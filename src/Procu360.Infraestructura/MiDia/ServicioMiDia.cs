using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.MiDia;

public sealed record ItemTarea(long Id, string Titulo, DateOnly Vence, long? ExpedienteId, int? NroOrden);

public sealed record ItemNovedad(long Id, DateOnly Fecha, string Texto, long? ExpedienteId);

public sealed record ItemPerencion(long ExpedienteId, int NroOrden, DateOnly UltimoCorte, DateOnly Vence);

public sealed record MiDiaResumen(
    int VencimientosHoy,
    int VencimientosVencidos,
    int NovedadesSinRevisar,
    int EtapasVencidas,
    int SinMovimiento,
    int PerencionProxima,
    int DiasAlertaPerencion,
    int DiasSinMovimiento,
    bool SinAlcance,
    IReadOnlyList<ItemTarea> Tareas,
    IReadOnlyList<ItemNovedad> Novedades,
    IReadOnlyList<ItemPerencion> Perenciones);

/// <summary>
/// Indicadores de Mi día (docs/01 §3.1). Todo sale del conjunto de expedientes en el alcance de la persona
/// (ámbito del rol en el Estudio activo), y los umbrales son parámetros del Estudio (conf.ConfiguracionEstudio).
/// </summary>
public sealed class ServicioMiDia(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    Procu360.Infraestructura.Expedientes.AlcanceExpedientes alcanceExpedientes)
{
    public const string ClaveDiasSinMovimiento = "DiasSinMovimientoAlerta";
    public const string ClaveDiasAlertaPerencion = "Perencion.DiasAlerta";
    public const string ClavePlazoPerencion = "Perencion.PlazoDias";

    private const int LimiteListas = 50;

    /// <summary>Fecha de Córdoba (UTC-3, sin horario de verano desde 2009).</summary>
    public static DateOnly HoyEnCordoba() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

    public async Task<MiDiaResumen> ResumenAsync(CancellationToken ct = default)
    {
        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var efectivos = await permisos.EfectivosAsync(ct);
        var todo = efectivos.Alcance.TodoEstudio;
        var hoy = HoyEnCordoba();

        var diasSin = await EnteroAsync(ClaveDiasSinMovimiento, 90, ct);
        var diasAlerta = await EnteroAsync(ClaveDiasAlertaPerencion, 30, ct);
        var plazo = await EnteroAsync(ClavePlazoPerencion, 1095, ct);

        var sinAlcance = !todo && efectivos.Alcance.Comitentes.Count == 0 && !efectivos.Alcance.Asignados;
        if (sinAlcance)
        {
            // Sin ningún alcance no se muestra nada del Estudio, ni siquiera las tareas visibles para todos.
            return new MiDiaResumen(0, 0, 0, 0, 0, 0, diasAlerta, diasSin, true, [], [], []);
        }
        var enAlcance = await alcanceExpedientes.IdsAsync(ct);

        // ---- tareas
        // Misma regla que ServicioTareas: visible para todos solo si no está vinculada a un expediente.
        var tareasVisibles = db.Tarea.Where(t => t.Estado == 1 && (
            t.ResponsableUsuarioId == usuario
            || db.TareaParticipante.Any(p => p.TareaId == t.Id && p.UsuarioId == usuario)
            || (t.ExpedienteId != null && enAlcance.Contains(t.ExpedienteId.Value))
            || (t.VisibleParaTodos && t.ExpedienteId == null)));

        var vencenHoy = await tareasVisibles.CountAsync(t => t.FechaVencimiento == hoy, ct);
        var vencidas = await tareasVisibles.CountAsync(t => t.FechaVencimiento < hoy, ct);
        var listaTareas = await tareasVisibles
            .Where(t => t.FechaVencimiento != null && t.FechaVencimiento <= hoy)
            .OrderBy(t => t.FechaVencimiento).Take(LimiteListas)
            .Select(t => new ItemTarea(t.Id, t.Titulo, t.FechaVencimiento!.Value, t.ExpedienteId,
                db.Expediente.Where(e => e.Id == t.ExpedienteId).Select(e => (int?)e.NroOrden).FirstOrDefault()))
            .ToListAsync(ct);

        // ---- novedades sin revisar (las sin expediente solo las ve quien tiene todo el Estudio)
        var novedades = db.NovedadJudicial.Where(n => !n.Leida && (
            n.ExpedienteId != null ? enAlcance.Contains(n.ExpedienteId.Value) : todo));
        var novedadesCantidad = await novedades.CountAsync(ct);
        var listaNovedades = await novedades.OrderByDescending(n => n.Fecha).Take(LimiteListas)
            .Select(n => new ItemNovedad(n.Id, n.Fecha, n.Texto, n.ExpedienteId))
            .ToListAsync(ct);

        // ---- etapas vencidas
        var etapasVencidas = await db.EtapaExpediente.CountAsync(x =>
            x.Estado == 1 && x.FechaVencimiento != null && x.FechaVencimiento < hoy
            && enAlcance.Contains(x.ExpedienteId), ct);

        // ---- sin movimiento
        var limite = hoy.AddDays(-diasSin);
        var sinMovimiento = await db.Expediente.CountAsync(e =>
            enAlcance.Contains(e.Id) && e.FechaUltimoMovimiento != null && e.FechaUltimoMovimiento < limite, ct);

        // ---- perención: expedientes en instancia que perime, con su último corte de perención
        var perenciones = await PerencionesAsync(enAlcance, hoy, plazo, diasAlerta, ct);

        return new MiDiaResumen(
            VencimientosHoy: vencenHoy,
            VencimientosVencidos: vencidas,
            NovedadesSinRevisar: novedadesCantidad,
            EtapasVencidas: etapasVencidas,
            SinMovimiento: sinMovimiento,
            PerencionProxima: perenciones.Count(p => p.Vence >= hoy),
            DiasAlertaPerencion: diasAlerta,
            DiasSinMovimiento: diasSin,
            SinAlcance: sinAlcance,
            Tareas: listaTareas,
            Novedades: listaNovedades,
            Perenciones: perenciones.OrderBy(p => p.Vence).Take(LimiteListas).ToList());
    }

    private async Task<List<ItemPerencion>> PerencionesAsync(
        IQueryable<long> enAlcance, DateOnly hoy, int plazo, int diasAlerta, CancellationToken ct)
    {
        // Expedientes en una instancia que perime (la actual del expediente).
        var enInstanciaQuePerime = await (
            from e in db.Expediente
            join i in db.Instancia on e.InstanciaActualId equals i.Id
            where i.Perime && enAlcance.Contains(e.Id)
            select new { e.Id, e.NroOrden }).ToListAsync(ct);
        if (enInstanciaQuePerime.Count == 0) return [];

        var ids = enInstanciaQuePerime.Select(x => x.Id).ToList();
        var cortes = await (
            from x in db.EtapaExpediente
            join et in db.Etapa on x.EtapaId equals et.Id
            where et.CortaPerencion && ids.Contains(x.ExpedienteId)
            group x by x.ExpedienteId into g
            select new { Expediente = g.Key, Ultimo = g.Max(v => v.FechaInicio) }).ToListAsync(ct);

        var nroPorExpediente = enInstanciaQuePerime.ToDictionary(x => x.Id, x => x.NroOrden);
        return cortes
            .Select(c => new ItemPerencion(c.Expediente, nroPorExpediente[c.Expediente], c.Ultimo, c.Ultimo.AddDays(plazo)))
            .Where(p => p.Vence <= hoy.AddDays(diasAlerta))
            .ToList();
    }

    private async Task<int> EnteroAsync(string clave, int defecto, CancellationToken ct)
    {
        var valor = await db.ConfiguracionEstudio.Where(c => c.Clave == clave).Select(c => c.Valor).FirstOrDefaultAsync(ct);
        return int.TryParse(valor, out var n) ? n : defecto;
    }
}

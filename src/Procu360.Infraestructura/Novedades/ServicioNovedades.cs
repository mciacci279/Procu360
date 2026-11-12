using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Novedades;

public sealed record FilaNovedad(long Id, DateOnly Fecha, string Texto, byte Fuente, bool Leida,
    long? ExpedienteId, int? NroOrden, bool TieneTarea);

public sealed record ResultadoNovedades(IReadOnlyList<FilaNovedad> Filas, int Total, int Pagina, int Paginas);

public enum PestanaNovedades { SinRevisar, Revisadas, SinVincular }

/// <summary>
/// Bandeja de novedades judiciales (docs/01 §3.4, docs/02 §4.7).
/// Una novedad vinculada se ve y se opera según el alcance de expedientes. Una novedad sin vincular
/// solo la ve (y vincula) quien tiene todo el Estudio.
/// </summary>
public sealed class ServicioNovedades(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance)
{
    private const int TamanioPagina = 25;

    public async Task<ResultadoNovedades> ListarAsync(PestanaNovedades pestana, int pagina, int tamanio = TamanioPagina, CancellationToken ct = default)
    {
        tamanio = Math.Clamp(tamanio, 1, 100);
        var (todo, enAlcance) = await AlcanceAsync(ct);
        var consulta = db.NovedadJudicial.AsQueryable();

        consulta = pestana switch
        {
            PestanaNovedades.SinRevisar => consulta.Where(n => !n.Leida),
            PestanaNovedades.Revisadas => consulta.Where(n => n.Leida),
            _ => consulta.Where(n => n.ExpedienteId == null),
        };
        consulta = consulta.Where(n => n.ExpedienteId != null
            ? enAlcance.Contains(n.ExpedienteId.Value)
            : todo);

        var total = await consulta.CountAsync(ct);
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanio));
        var numero = Math.Clamp(pagina, 1, paginas);

        var filas = await consulta.OrderByDescending(n => n.Fecha).ThenByDescending(n => n.Id)
            .Skip((numero - 1) * tamanio).Take(tamanio)
            .Select(n => new FilaNovedad(
                n.Id, n.Fecha, n.Texto, n.Fuente, n.Leida, n.ExpedienteId,
                db.Expediente.Where(e => e.Id == n.ExpedienteId).Select(e => (int?)e.NroOrden).FirstOrDefault(),
                n.TareaId != null))
            .ToListAsync(ct);

        return new ResultadoNovedades(filas, total, numero, paginas);
    }

    public async Task<ResultadoOperacion> MarcarRevisadaAsync(long novedadId, CancellationToken ct = default)
    {
        if (await SinPermisoAsync("novedades.editar") is { } denegado) return denegado;
        var novedad = await VisibleAsync(novedadId, ct);
        if (novedad is null) return ResultadoOperacion.Fallo("La novedad no existe o no está en tu alcance.");
        if (novedad.Leida) return ResultadoOperacion.Ok();

        novedad.Leida = true;
        novedad.LeidaUtc = DateTime.UtcNow;
        novedad.LeidaPor = contexto.UsuarioId;
        Auditar(2, novedadId, new { leida = new { antes = false, despues = true } });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> VincularAsync(long novedadId, long expedienteId, CancellationToken ct = default)
    {
        if (await SinPermisoAsync("novedades.editar") is { } denegado) return denegado;
        var novedad = await VisibleAsync(novedadId, ct);
        if (novedad is null) return ResultadoOperacion.Fallo("La novedad no existe o no está en tu alcance.");
        if (novedad.ExpedienteId != null) return ResultadoOperacion.Fallo("La novedad ya está vinculada a un expediente.");

        var (_, enAlcance) = await AlcanceAsync(ct);
        if (!await enAlcance.AnyAsync(x => x == expedienteId, ct))
            return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");

        novedad.ExpedienteId = expedienteId;
        Auditar(2, novedadId, new { expediente = new { antes = (long?)null, despues = expedienteId } });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> CrearTareaAsync(long novedadId, string titulo, DateOnly vence,
        CancellationToken ct = default)
    {
        if (await SinPermisoAsync("tareas.crear") is { } denegado) return denegado;
        var novedad = await VisibleAsync(novedadId, ct);
        if (novedad is null) return ResultadoOperacion.Fallo("La novedad no existe o no está en tu alcance.");
        if (novedad.ExpedienteId is null)
            return ResultadoOperacion.Fallo("Vinculá la novedad a un expediente antes de crear una tarea.");
        if (novedad.TareaId != null) return ResultadoOperacion.Fallo("La novedad ya tiene una tarea.");

        var tituloLimpio = string.IsNullOrWhiteSpace(titulo)
            ? novedad.Texto.Trim()
            : titulo.Trim();
        if (tituloLimpio.Length > 150) tituloLimpio = tituloLimpio[..150];

        var hoy = Procu360.Infraestructura.MiDia.ServicioMiDia.HoyEnCordoba();
        var tarea = new Tarea
        {
            EstudioId = contexto.EstudioId!.Value,
            ExpedienteId = novedad.ExpedienteId,
            Titulo = tituloLimpio,
            FechaInicio = hoy,
            FechaVencimiento = vence,
            ResponsableUsuarioId = contexto.UsuarioId,
            VisibleParaTodos = false,
            Estado = 1,
            Origen = 3,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Tarea.Add(tarea);
        await db.SaveChangesAsync(ct);

        novedad.TareaId = tarea.Id;
        Auditar(1, novedadId, new { tarea = new { antes = (long?)null, despues = tarea.Id } });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    // ------------------------------------------------------------ internos

    private async Task<(bool Todo, IQueryable<long> EnAlcance)> AlcanceAsync(CancellationToken ct)
    {
        var todo = (await permisos.EfectivosAsync(ct)).Alcance.TodoEstudio;
        return (todo, await alcance.IdsAsync(ct));
    }

    private async Task<NovedadJudicial?> VisibleAsync(long novedadId, CancellationToken ct)
    {
        var (todo, enAlcance) = await AlcanceAsync(ct);
        return await db.NovedadJudicial.FirstOrDefaultAsync(n => n.Id == novedadId && (
            n.ExpedienteId != null ? enAlcance.Contains(n.ExpedienteId.Value) : todo), ct);
    }

    private async Task<ResultadoOperacion?> SinPermisoAsync(string permiso) =>
        await permisos.TieneAsync(permiso, default)
            ? null
            : ResultadoOperacion.Fallo("No tenés permiso para esta acción.");

    private void Auditar(byte accion, long novedadId, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = "novedades",
            Entidad = "NovedadJudicial",
            EntidadId = novedadId.ToString(),
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

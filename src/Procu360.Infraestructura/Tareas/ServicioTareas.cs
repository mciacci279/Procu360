using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Tareas;

public enum FiltroTareas { Pendientes, Vencidas, Hoy, Semana, Cumplidas, Todas }

public sealed record FilaTarea(long Id, string Titulo, DateOnly? Vence, byte Estado, long? ExpedienteId,
    int? NroOrden, string? Responsable, bool VisibleParaTodos);

public sealed record ResultadoTareas(IReadOnlyList<FilaTarea> Filas, int Total, int Pagina, int Paginas);

/// <summary>
/// Tareas y agenda (docs/01 §3.5). Una tarea se ve si es visible para todos, si la persona es responsable
/// o participante, o si su expediente está dentro del alcance (la misma regla que Mi día).
/// </summary>
public sealed class ServicioTareas(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance)
{
    private const int TamanioPagina = 25;

    public async Task<ResultadoTareas> ListarAsync(FiltroTareas filtro, int pagina, int tamanio = TamanioPagina, CancellationToken ct = default)
    {
        tamanio = Math.Clamp(tamanio, 1, 100);
        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var hoy = ServicioMiDia.HoyEnCordoba();
        var enAlcance = await alcance.IdsAsync(ct);

        var consulta = VisiblesPara(usuario, enAlcance);
        consulta = filtro switch
        {
            FiltroTareas.Pendientes => consulta.Where(t => t.Estado == 1),
            FiltroTareas.Vencidas => consulta.Where(t => t.Estado == 1 && t.FechaVencimiento != null && t.FechaVencimiento < hoy),
            FiltroTareas.Hoy => consulta.Where(t => t.Estado == 1 && t.FechaVencimiento == hoy),
            FiltroTareas.Semana => consulta.Where(t => t.Estado == 1 && t.FechaVencimiento != null
                                                       && t.FechaVencimiento >= hoy && t.FechaVencimiento <= hoy.AddDays(7)),
            FiltroTareas.Cumplidas => consulta.Where(t => t.Estado == 2),
            _ => consulta,
        };

        var total = await consulta.CountAsync(ct);
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanio));
        var numero = Math.Clamp(pagina, 1, paginas);

        var filas = await consulta.OrderBy(t => t.FechaVencimiento == null).ThenBy(t => t.FechaVencimiento).ThenBy(t => t.Id)
            .Skip((numero - 1) * tamanio).Take(tamanio)
            .Select(t => new FilaTarea(
                t.Id, t.Titulo, t.FechaVencimiento, t.Estado, t.ExpedienteId,
                db.Expediente.Where(e => e.Id == t.ExpedienteId).Select(e => (int?)e.NroOrden).FirstOrDefault(),
                db.Usuario.Where(u => u.Id == t.ResponsableUsuarioId).Select(u => u.Nombre + " " + u.Apellido).FirstOrDefault(),
                t.VisibleParaTodos))
            .ToListAsync(ct);

        return new ResultadoTareas(filas, total, numero, paginas);
    }

    /// <summary>Tareas visibles con vencimiento en el rango, para la vista de calendario. Sin paginar (tope de seguridad).</summary>
    public async Task<IReadOnlyList<FilaTarea>> EntreAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var enAlcance = await alcance.IdsAsync(ct);
        return await VisiblesPara(usuario, enAlcance)
            .Where(t => t.FechaVencimiento != null && t.FechaVencimiento >= desde && t.FechaVencimiento <= hasta)
            .OrderBy(t => t.FechaVencimiento).ThenBy(t => t.Id).Take(1000)
            .Select(t => new FilaTarea(
                t.Id, t.Titulo, t.FechaVencimiento, t.Estado, t.ExpedienteId,
                db.Expediente.Where(e => e.Id == t.ExpedienteId).Select(e => (int?)e.NroOrden).FirstOrDefault(),
                db.Usuario.Where(u => u.Id == t.ResponsableUsuarioId).Select(u => u.Nombre + " " + u.Apellido).FirstOrDefault(),
                t.VisibleParaTodos))
            .ToListAsync(ct);
    }

    public async Task<ResultadoOperacion> CrearAsync(string titulo, DateOnly vence, long? expedienteId,
        bool visibleParaTodos, CancellationToken ct = default)
    {
        if (!await permisos.TieneAsync("tareas.crear", ct))
            return ResultadoOperacion.Fallo("No tenés permiso para crear tareas.");

        var tituloLimpio = titulo?.Trim() ?? string.Empty;
        if (tituloLimpio.Length == 0) return ResultadoOperacion.Fallo("La tarea necesita un título.");
        if (tituloLimpio.Length > 150) tituloLimpio = tituloLimpio[..150];

        if (expedienteId is long exp)
        {
            var enAlcance = await alcance.IdsAsync(ct);
            if (!await enAlcance.AnyAsync(x => x == exp, ct))
                return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");
        }

        var tarea = new Tarea
        {
            EstudioId = contexto.EstudioId!.Value,
            ExpedienteId = expedienteId,
            Titulo = tituloLimpio,
            FechaInicio = ServicioMiDia.HoyEnCordoba(),
            FechaVencimiento = vence,
            ResponsableUsuarioId = contexto.UsuarioId,
            VisibleParaTodos = visibleParaTodos,
            Estado = 1,
            Origen = 1,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Tarea.Add(tarea);
        await db.SaveChangesAsync(ct);
        Auditar(1, tarea.Id, new { titulo = tituloLimpio, vence, expediente = expedienteId });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> CumplirAsync(long tareaId, CancellationToken ct = default)
    {
        if (!await permisos.TieneAsync("tareas.editar", ct))
            return ResultadoOperacion.Fallo("No tenés permiso para editar tareas.");

        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var tarea = await VisiblesPara(usuario, await alcance.IdsAsync(ct)).FirstOrDefaultAsync(t => t.Id == tareaId, ct);
        if (tarea is null) return ResultadoOperacion.Fallo("La tarea no existe o no está en tu alcance.");
        if (tarea.Estado != 1) return ResultadoOperacion.Ok();

        tarea.Estado = 2;
        tarea.CumplidaUtc = DateTime.UtcNow;
        tarea.ModificadoUtc = DateTime.UtcNow;
        tarea.ModificadoPor = usuario;
        Auditar(2, tareaId, new { estado = new { antes = 1, despues = 2 } });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    /// <summary>
    /// Tareas que la persona puede ver: responsable o participante; o tareas de su alcance (expediente en alcance);
    /// o visibles para todos, pero solo si no están vinculadas a un expediente fuera del alcance.
    /// </summary>
    private IQueryable<Tarea> VisiblesPara(int usuario, IQueryable<long> enAlcance) =>
        db.Tarea.Where(t =>
            t.ResponsableUsuarioId == usuario
            || db.TareaParticipante.Any(p => p.TareaId == t.Id && p.UsuarioId == usuario)
            || (t.ExpedienteId != null && enAlcance.Contains(t.ExpedienteId.Value))
            || (t.VisibleParaTodos && t.ExpedienteId == null));

    private void Auditar(byte accion, long tareaId, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = "tareas",
            Entidad = "Tarea",
            EntidadId = tareaId.ToString(),
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

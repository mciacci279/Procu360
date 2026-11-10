using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Plazos;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Etapas;

/// <summary>
/// Registro de etapas y diligencias de un expediente (docs/01 §3.3). Una sola operación:
/// calcula el vencimiento (días hábiles con feriados), cierra la etapa abierta del mismo flujo,
/// actualiza la etapa actual del expediente, genera la tarea si la etapa lo indica y audita.
/// Solo sobre expedientes dentro del alcance de la persona.
/// </summary>
public sealed record FilaEtapa(long Id, string Nombre, byte Flujo, bool EsDiligencia, DateOnly Inicio,
    DateOnly? Vencimiento, byte Estado, decimal? Monto, string? Observaciones);

public sealed class ServicioEtapas(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance)
{
    public const byte FlujoPrincipal = 1;
    public const byte FlujoCautelar = 2;
    public const byte EstadoAlta = 1;
    public const byte EstadoCumplimentada = 2;
    public const byte EstadoSuspendida = 3;

    public async Task<ResultadoOperacion> RegistrarAsync(long expedienteId, int etapaId, DateOnly inicio,
        string? observaciones, decimal? monto, CancellationToken ct = default)
    {
        if (!await permisos.TieneAsync(Permisos.De(Modulos.Expedientes, Acciones.Editar), ct))
            return ResultadoOperacion.Fallo("No tenés permiso para registrar etapas.");

        if (!await (await alcance.IdsAsync(ct)).AnyAsync(x => x == expedienteId, ct))
            return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");

        var etapa = await db.Etapa.FirstOrDefaultAsync(e => e.Id == etapaId && e.Activa, ct);
        if (etapa is null) return ResultadoOperacion.Fallo("La etapa no existe o está inactiva.");

        var expediente = await db.Expediente.FirstAsync(e => e.Id == expedienteId, ct);
        var vence = CalculoPlazos.Vencimiento(inicio, etapa.Dias, etapa.DiasHabiles, await FeriadosAsync(ct));

        // La etapa abierta del mismo flujo pasa a cumplimentada (cerrada en la fecha de la nueva).
        var abiertas = await db.EtapaExpediente
            .Where(x => x.ExpedienteId == expedienteId && x.Estado == EstadoAlta
                        && db.Etapa.Any(e => e.Id == x.EtapaId && e.Flujo == etapa.Flujo))
            .ToListAsync(ct);
        foreach (var anterior in abiertas)
        {
            anterior.Estado = EstadoCumplimentada;
            anterior.FechaFin = inicio;
        }

        var nueva = new EtapaExpediente
        {
            EstudioId = contexto.EstudioId!.Value,
            ExpedienteId = expedienteId,
            EtapaId = etapa.Id,
            FechaInicio = inicio,
            FechaVencimiento = vence,
            Estado = EstadoAlta,
            Monto = monto,
            Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim(),
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.EtapaExpediente.Add(nueva);
        await db.SaveChangesAsync(ct);

        if (etapa.Flujo == FlujoCautelar) expediente.CautelarActualId = etapa.Id;
        else expediente.EtapaActualId = etapa.Id;
        if (expediente.FechaUltimoMovimiento is null || inicio > expediente.FechaUltimoMovimiento)
            expediente.FechaUltimoMovimiento = inicio;

        if (etapa.GeneraTarea)
        {
            db.Tarea.Add(new Tarea
            {
                EstudioId = contexto.EstudioId!.Value,
                ExpedienteId = expedienteId,
                EtapaExpedienteId = nueva.Id,
                Titulo = etapa.Nombre.Length > 150 ? etapa.Nombre[..150] : etapa.Nombre,
                FechaInicio = ServicioMiDia.HoyEnCordoba(),
                FechaVencimiento = vence,
                ResponsableUsuarioId = contexto.UsuarioId,
                VisibleParaTodos = false,
                Estado = 1,
                Origen = 2,
                CreadoUtc = DateTime.UtcNow,
                CreadoPor = contexto.UsuarioId
            });
        }

        Auditar(1, nueva.Id, new { etapa = etapa.Nombre, inicio, vence, flujo = etapa.Flujo });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    /// <summary>Marca una etapa como cumplimentada o suspendida. Requiere alcance sobre su expediente.</summary>
    public async Task<ResultadoOperacion> CambiarEstadoAsync(long etapaExpedienteId, byte nuevoEstado, CancellationToken ct = default)
    {
        if (!await permisos.TieneAsync(Permisos.De(Modulos.Expedientes, Acciones.Editar), ct))
            return ResultadoOperacion.Fallo("No tenés permiso para cambiar etapas.");
        if (nuevoEstado is not (EstadoCumplimentada or EstadoSuspendida))
            return ResultadoOperacion.Fallo("Estado no permitido.");

        var etapa = await db.EtapaExpediente.FirstOrDefaultAsync(x => x.Id == etapaExpedienteId, ct);
        if (etapa is null || !await (await alcance.IdsAsync(ct)).AnyAsync(x => x == etapa.ExpedienteId, ct))
            return ResultadoOperacion.Fallo("La etapa no existe o no está en tu alcance.");

        var anterior = etapa.Estado;
        etapa.Estado = nuevoEstado;
        if (nuevoEstado == EstadoCumplimentada) etapa.FechaFin ??= ServicioMiDia.HoyEnCordoba();
        Auditar(5, etapaExpedienteId, new { estado = new { antes = anterior, despues = nuevoEstado } });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    /// <summary>Historial de etapas del expediente (dentro del alcance), de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<FilaEtapa>> ListarAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await (await alcance.IdsIncluyendoBajasAsync(ct)).AnyAsync(x => x == expedienteId, ct))
            return [];

        return await db.EtapaExpediente.Where(x => x.ExpedienteId == expedienteId)
            .OrderByDescending(x => x.FechaInicio).ThenByDescending(x => x.Id)
            .Select(x => new FilaEtapa(x.Id, x.Etapa.Nombre, x.Etapa.Flujo, x.Etapa.EsDiligencia,
                x.FechaInicio, x.FechaVencimiento, x.Estado, x.Monto, x.Observaciones))
            .ToListAsync(ct);
    }

    /// <summary>Etapas activas del Estudio para el selector de registro.</summary>
    public async Task<IReadOnlyList<(int Id, string Nombre, byte Flujo)>> CatalogoAsync(CancellationToken ct = default)
    {
        var lista = await db.Etapa.Where(e => e.Activa).OrderBy(e => e.Flujo).ThenBy(e => e.Orden)
            .Select(e => new { e.Id, e.Nombre, e.Flujo }).ToListAsync(ct);
        return lista.Select(e => (e.Id, e.Nombre, e.Flujo)).ToList();
    }

    /// <summary>
    /// Feriados que cuentan como no hábiles: nacionales, de receso judicial, provinciales de la provincia
    /// configurada del Estudio (Feriados.ProvinciaId) y los propios del Estudio.
    /// </summary>
    public async Task<HashSet<DateOnly>> FeriadosParaAsync(CancellationToken ct = default)
    {
        var provinciaTexto = await db.ConfiguracionEstudio.Where(c => c.Clave == ClaveProvinciaFeriados)
            .Select(c => c.Valor).FirstOrDefaultAsync(ct);
        var provinciaId = int.TryParse(provinciaTexto, out var p) ? (int?)p : null;

        var nacionales = await db.Feriado.Where(f => f.Ambito == 1 || f.Ambito == 3).Select(f => f.Fecha).ToListAsync(ct);
        var provinciales = provinciaId is null
            ? new List<DateOnly>()
            : await db.Feriado.Where(f => f.Ambito == 2 && f.ProvinciaId == provinciaId).Select(f => f.Fecha).ToListAsync(ct);
        var delEstudio = await db.FeriadoEstudio.Select(f => f.Fecha).ToListAsync(ct);
        return nacionales.Concat(provinciales).Concat(delEstudio).ToHashSet();
    }

    private Task<HashSet<DateOnly>> FeriadosAsync(CancellationToken ct) => FeriadosParaAsync(ct);

    public const string ClaveProvinciaFeriados = "Feriados.ProvinciaId";

    private void Auditar(byte accion, long entidadId, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Expedientes,
            Entidad = "EtapaExpediente",
            EntidadId = entidadId.ToString(),
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

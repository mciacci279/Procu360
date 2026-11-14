using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Expedientes;

/// <summary>Un período adeudado del expediente, con su versión para el control de concurrencia.</summary>
public sealed record PeriodoEditable(long Id, short Anio, string? Cuota, string? Concepto, DateOnly? Vencimiento,
    decimal Tributo, decimal Recargo, string? Observaciones, byte[] Version)
{
    public decimal Total => Tributo + Recargo;
}

/// <summary>Datos con los que se da de alta o se edita un período adeudado.</summary>
public sealed record DatosPeriodo(short Anio, string? Cuota, string? Concepto, DateOnly? Vencimiento,
    decimal Tributo, decimal Recargo, string? Observaciones);

/// <summary>
/// ABM de los períodos adeudados de un expediente (dbo.PeriodoAdeudado).
///
/// <para>Son filas propias del expediente, así que acá sí se borran de verdad: no hay nada que las referencie.
/// Cada operación exige <c>expedientes.editar</c> y alcance sobre el expediente, y queda auditada.</para>
///
/// <para>Cada alta, edición o baja sincroniza el capital del título con la suma de los períodos, salvo que
/// alguien lo haya editado a mano: en ese caso el capital queda como está y la ficha avisa que perdió
/// correlación. El monto del título nunca se toca.</para>
/// </summary>
public sealed class ServicioPeriodosExpediente(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance,
    ServicioEdicionExpediente edicion)
{
    /// <summary>Año mínimo aceptado: evita cargar "19" o "199" por error de tipeo.</summary>
    private const short AnioMinimo = 1950;

    public async Task<IReadOnlyList<PeriodoEditable>> ListarAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return [];
        return await db.PeriodoAdeudado.Where(p => p.ExpedienteId == expedienteId)
            .OrderBy(p => p.Anio).ThenBy(p => p.Cuota)
            .Select(p => new PeriodoEditable(p.Id, p.Anio, p.Cuota, p.Concepto, p.Vencimiento,
                p.Tributo, p.Recargo, p.Observaciones, p.Version))
            .ToListAsync(ct);
    }

    public async Task<ResultadoOperacion> AgregarAsync(long expedienteId, DatosPeriodo datos,
        CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (Validar(limpio) is { } error) return ResultadoOperacion.Fallo(error);

        if (await db.PeriodoAdeudado.AnyAsync(p => p.ExpedienteId == expedienteId
                && p.Anio == limpio.Anio && p.Cuota == limpio.Cuota, ct))
            return ResultadoOperacion.Fallo("Ya hay un período cargado con ese año y esa cuota.");

        db.PeriodoAdeudado.Add(new PeriodoAdeudado
        {
            EstudioId = contexto.EstudioId!.Value,
            ExpedienteId = expedienteId,
            Anio = limpio.Anio,
            Cuota = limpio.Cuota,
            Concepto = limpio.Concepto,
            Vencimiento = limpio.Vencimiento,
            Tributo = limpio.Tributo,
            Recargo = limpio.Recargo,
            Observaciones = limpio.Observaciones,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        });
        Auditar(expedienteId, 1, new
        {
            antes = (object?)null,
            despues = new { limpio.Anio, limpio.Cuota, limpio.Tributo, limpio.Recargo }
        });
        await db.SaveChangesAsync(ct);
        await edicion.SincronizarCapitalAsync(expedienteId, ct: ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> EditarAsync(long expedienteId, long periodoId, DatosPeriodo datos,
        byte[] version, CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (Validar(limpio) is { } error) return ResultadoOperacion.Fallo(error);

        var periodo = await db.PeriodoAdeudado
            .FirstOrDefaultAsync(p => p.Id == periodoId && p.ExpedienteId == expedienteId, ct);
        if (periodo is null) return ResultadoOperacion.Fallo("El período no existe en este expediente.");
        if (!periodo.Version.SequenceEqual(version))
            return ResultadoOperacion.Fallo("El período cambió mientras lo editabas. Recargá la ficha y volvé a intentar.");

        if (await db.PeriodoAdeudado.AnyAsync(p => p.ExpedienteId == expedienteId && p.Id != periodoId
                && p.Anio == limpio.Anio && p.Cuota == limpio.Cuota, ct))
            return ResultadoOperacion.Fallo("Ya hay otro período cargado con ese año y esa cuota.");

        var cambios = new Dictionary<string, object?[]>();
        Cambiar(cambios, "anio", periodo.Anio, limpio.Anio, v => periodo.Anio = v);
        Cambiar(cambios, "cuota", periodo.Cuota, limpio.Cuota, v => periodo.Cuota = v);
        Cambiar(cambios, "concepto", periodo.Concepto, limpio.Concepto, v => periodo.Concepto = v);
        Cambiar(cambios, "vencimiento", periodo.Vencimiento, limpio.Vencimiento, v => periodo.Vencimiento = v);
        Cambiar(cambios, "tributo", periodo.Tributo, limpio.Tributo, v => periodo.Tributo = v);
        Cambiar(cambios, "recargo", periodo.Recargo, limpio.Recargo, v => periodo.Recargo = v);
        Cambiar(cambios, "observaciones", periodo.Observaciones, limpio.Observaciones, v => periodo.Observaciones = v);
        if (cambios.Count == 0) return ResultadoOperacion.Ok();

        periodo.ModificadoUtc = DateTime.UtcNow;
        periodo.ModificadoPor = contexto.UsuarioId;
        Auditar(expedienteId, 2, cambios.ToDictionary(c => c.Key, c => new { antes = c.Value[0], despues = c.Value[1] }));
        await db.SaveChangesAsync(ct);
        await edicion.SincronizarCapitalAsync(expedienteId, ct: ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> EliminarAsync(long expedienteId, long periodoId,
        CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var periodo = await db.PeriodoAdeudado
            .FirstOrDefaultAsync(p => p.Id == periodoId && p.ExpedienteId == expedienteId, ct);
        if (periodo is null) return ResultadoOperacion.Fallo("El período no existe en este expediente.");

        db.PeriodoAdeudado.Remove(periodo);
        Auditar(expedienteId, 3, new
        {
            antes = new { periodo.Anio, periodo.Cuota, periodo.Tributo, periodo.Recargo },
            despues = (object?)null
        });
        await db.SaveChangesAsync(ct);
        await edicion.SincronizarCapitalAsync(expedienteId, ct: ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- internos

    private async Task<ResultadoOperacion?> NoPuedeAsync(long expedienteId, CancellationToken ct)
    {
        if (!await permisos.TieneAsync(Permisos.De(Modulos.Expedientes, Acciones.Editar), ct))
            return ResultadoOperacion.Fallo("No tenés permiso para editar expedientes.");
        if (!await VisibleAsync(expedienteId, ct))
            return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");
        return null;
    }

    private async Task<bool> VisibleAsync(long expedienteId, CancellationToken ct) =>
        await (await alcance.IdsAsync(ct)).AnyAsync(x => x == expedienteId, ct);

    private static DatosPeriodo Normalizar(DatosPeriodo d) => d with
    {
        Cuota = Limpiar(d.Cuota, 50),
        Concepto = Limpiar(d.Concepto, 50),
        Observaciones = Limpiar(d.Observaciones, 2500),
        Tributo = decimal.Round(d.Tributo, 2),
        Recargo = decimal.Round(d.Recargo, 2),
    };

    private static string? Validar(DatosPeriodo d)
    {
        var anioTope = (short)(MiDia.ServicioMiDia.HoyEnCordoba().Year + 1);
        if (d.Anio < AnioMinimo || d.Anio > anioTope)
            return $"El año tiene que estar entre {AnioMinimo} y {anioTope}.";
        if (d.Tributo < 0) return "El tributo no puede ser negativo.";
        if (d.Recargo < 0) return "El recargo no puede ser negativo.";
        return null;
    }

    private static void Cambiar<T>(Dictionary<string, object?[]> cambios, string campo, T? antes, T? despues,
        Action<T?> asignar)
    {
        if (EqualityComparer<T>.Default.Equals(antes, despues)) return;
        cambios[campo] = [antes, despues];
        asignar(despues);
    }

    private static string? Limpiar(string? texto, int largo)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var t = texto.Trim();
        return t.Length > largo ? t[..largo] : t;
    }

    private void Auditar(long expedienteId, byte accion, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Expedientes,
            Entidad = "PeriodoAdeudado",
            EntidadId = expedienteId.ToString(),
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

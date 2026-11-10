using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Expedientes;

/// <summary>Datos del juicio, editables por sección desde la ficha.</summary>
public sealed record DatosJuicio(int? JuzgadoId, string? NroExpedienteJudicial, string? Caratula, string? Recaratulacion,
    string? Sorteo, DateOnly? FechaSorteo, byte[] Version);

/// <summary>Observaciones, editables por sección desde la ficha.</summary>
public sealed record DatosObservaciones(string? Observaciones, string? ObservacionesAdministrativas, byte[] Version);

/// <summary>Profesionales del expediente. Cada selector muestra solo los activos con el rol correspondiente.</summary>
public sealed record DatosProfesionales(int? ProcuradorId, int? PatrocinanteId, int? AbogadoInteriorId, byte[] Version);

/// <summary>
/// Edición de la ficha por secciones (docs/02-ux §4.3). Cada sección se guarda sola: valida, exige
/// <c>expedientes.editar</c> y alcance sobre el expediente, rechaza si otra persona lo cambió mientras
/// tanto (control de versión) y audita solo los campos que cambiaron.
/// </summary>
public sealed class ServicioEdicionExpediente(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance)
{
    public async Task<DatosJuicio?> JuicioAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosJuicio(e.JuzgadoId, e.NroExpedienteJudicial, e.Caratula, e.Recaratulacion,
                e.Sorteo, e.FechaSorteo, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DatosObservaciones?> ObservacionesAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosObservaciones(e.Observaciones, e.ObservacionesAdministrativas, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DatosProfesionales?> ProfesionalesAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosProfesionales(e.ProcuradorId, e.PatrocinanteId, e.AbogadoInteriorId, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>Juzgados activos del Estudio, para el selector.</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> JuzgadosAsync(CancellationToken ct = default) =>
        await db.Juzgado.Where(j => j.Activo).OrderBy(j => j.Orden).ThenBy(j => j.Nombre)
            .Select(j => new OpcionCatalogo(j.Id, j.Nombre)).ToListAsync(ct);

    /// <summary>Profesionales activos con un rol: "procurador", "patrocinante" o "abogadoInterior".</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> ProfesionalesDeRolAsync(string rol, CancellationToken ct = default)
    {
        var consulta = db.Profesional.Where(p => p.Activo);
        consulta = rol switch
        {
            "procurador" => consulta.Where(p => p.EsProcurador),
            "patrocinante" => consulta.Where(p => p.EsPatrocinante),
            "abogadoInterior" => consulta.Where(p => p.EsAbogadoInterior),
            _ => consulta.Where(p => false),
        };
        return await consulta.OrderBy(p => p.Nombre).Select(p => new OpcionCatalogo(p.Id, p.Nombre)).ToListAsync(ct);
    }

    public async Task<ResultadoOperacion> GuardarJuicioAsync(long expedienteId, DatosJuicio datos, CancellationToken ct = default)
    {
        var limpio = datos with
        {
            NroExpedienteJudicial = Limpiar(datos.NroExpedienteJudicial, 50),
            Caratula = Limpiar(datos.Caratula, 400),
            Recaratulacion = Limpiar(datos.Recaratulacion, 400),
            Sorteo = Limpiar(datos.Sorteo, 50),
        };

        if (limpio.JuzgadoId is int j && !await db.Juzgado.AnyAsync(x => x.Id == j && x.Activo, ct))
            return ResultadoOperacion.Fallo("El juzgado elegido no existe o está inactivo.");

        return await GuardarAsync(expedienteId, limpio.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "juzgadoId", e.JuzgadoId, limpio.JuzgadoId, v => e.JuzgadoId = v);
            Cambiar(cambios, "nroExpedienteJudicial", e.NroExpedienteJudicial, limpio.NroExpedienteJudicial, v => e.NroExpedienteJudicial = v);
            Cambiar(cambios, "caratula", e.Caratula, limpio.Caratula, v => e.Caratula = v);
            Cambiar(cambios, "recaratulacion", e.Recaratulacion, limpio.Recaratulacion, v => e.Recaratulacion = v);
            Cambiar(cambios, "sorteo", e.Sorteo, limpio.Sorteo, v => e.Sorteo = v);
            Cambiar(cambios, "fechaSorteo", e.FechaSorteo, limpio.FechaSorteo, v => e.FechaSorteo = v);
            return cambios;
        });
    }

    public async Task<ResultadoOperacion> GuardarObservacionesAsync(long expedienteId, DatosObservaciones datos, CancellationToken ct = default)
    {
        var obs = Limpiar(datos.Observaciones, 4000);
        var admin = Limpiar(datos.ObservacionesAdministrativas, 4000);
        return await GuardarAsync(expedienteId, datos.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "observaciones", e.Observaciones, obs, v => e.Observaciones = v);
            Cambiar(cambios, "observacionesAdministrativas", e.ObservacionesAdministrativas, admin, v => e.ObservacionesAdministrativas = v);
            return cambios;
        });
    }

    public async Task<ResultadoOperacion> GuardarProfesionalesAsync(long expedienteId, DatosProfesionales datos, CancellationToken ct = default)
    {
        if (!await ProfesionalValidoAsync(datos.ProcuradorId, "procurador", ct))
            return ResultadoOperacion.Fallo("El procurador elegido no existe, está inactivo o no tiene ese rol.");
        if (!await ProfesionalValidoAsync(datos.PatrocinanteId, "patrocinante", ct))
            return ResultadoOperacion.Fallo("El patrocinante elegido no existe, está inactivo o no tiene ese rol.");
        if (!await ProfesionalValidoAsync(datos.AbogadoInteriorId, "abogadoInterior", ct))
            return ResultadoOperacion.Fallo("El abogado del interior elegido no existe, está inactivo o no tiene ese rol.");

        return await GuardarAsync(expedienteId, datos.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "procuradorId", e.ProcuradorId, datos.ProcuradorId, v => e.ProcuradorId = v);
            Cambiar(cambios, "patrocinanteId", e.PatrocinanteId, datos.PatrocinanteId, v => e.PatrocinanteId = v);
            Cambiar(cambios, "abogadoInteriorId", e.AbogadoInteriorId, datos.AbogadoInteriorId, v => e.AbogadoInteriorId = v);
            return cambios;
        });
    }

    private async Task<bool> ProfesionalValidoAsync(int? id, string rol, CancellationToken ct)
    {
        if (id is null) return true;
        var opciones = await ProfesionalesDeRolAsync(rol, ct);
        return opciones.Any(o => o.Id == id.Value);
    }

    private async Task<ResultadoOperacion> GuardarAsync(long expedienteId, byte[] versionVista, CancellationToken ct,
        Func<Expediente, Dictionary<string, object?[]>> aplicar)
    {
        if (!await permisos.TieneAsync(Permisos.De(Modulos.Expedientes, Acciones.Editar), ct))
            return ResultadoOperacion.Fallo("No tenés permiso para editar expedientes.");
        if (!await VisibleAsync(expedienteId, ct))
            return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");

        var expediente = await db.Expediente.FirstAsync(e => e.Id == expedienteId, ct);
        if (!expediente.Version.SequenceEqual(versionVista))
            return ResultadoOperacion.Fallo("El expediente cambió mientras lo editabas. Recargá la ficha y volvé a intentar.");

        var cambios = aplicar(expediente);
        if (cambios.Count == 0) return ResultadoOperacion.Ok();

        expediente.ModificadoUtc = DateTime.UtcNow;
        expediente.ModificadoPor = contexto.UsuarioId;
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Expedientes,
            Entidad = "Expediente",
            EntidadId = expedienteId.ToString(),
            Accion = 2, // editar
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios.ToDictionary(c => c.Key, c => new { antes = c.Value[0], despues = c.Value[1] }))
        });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    private async Task<bool> VisibleAsync(long expedienteId, CancellationToken ct) =>
        await (await alcance.IdsAsync(ct)).AnyAsync(x => x == expedienteId, ct);

    /// <summary>Registra el cambio solo si el valor es distinto. Guarda antes y después para la auditoría.</summary>
    private static void Cambiar<T>(Dictionary<string, object?[]> cambios, string campo, T? antes, T? despues, Action<T?> asignar)
    {
        if (EqualityComparer<T>.Default.Equals(antes, despues)) return;
        cambios[campo] = [antes, despues];
        asignar(despues);
    }

    /// <summary>Texto sin espacios sobrantes; vacío pasa a null. Corta al largo de la columna.</summary>
    private static string? Limpiar(string? texto, int largo)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var t = texto.Trim();
        return t.Length > largo ? t[..largo] : t;
    }
}

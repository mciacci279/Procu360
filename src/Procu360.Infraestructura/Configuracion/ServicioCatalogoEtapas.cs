using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Configuracion;

/// <summary>A quién se le asigna la tarea que genera una etapa (conf.Etapa.TareaParaUsuarios).</summary>
public static class TareaPara
{
    public const byte Ninguno = 0;
    public const byte Responsable = 1;
    public const byte Seleccionados = 2;
    public const byte Todos = 3;

    public static string Nombre(byte valor) => valor switch
    {
        Responsable => "Al responsable del expediente",
        Seleccionados => "A usuarios seleccionados",
        Todos => "A todos",
        _ => "A nadie"
    };

    public static bool Valido(byte valor) => valor is Ninguno or Responsable or Seleccionados or Todos;
}

/// <summary>Una instancia del catálogo, con la cantidad de expedientes que la usan.</summary>
public sealed record FilaInstancia(int Id, string Nombre, string? Color, bool Perime, bool EsBaja, int? Orden,
    bool Activa, int Expedientes, byte[] Version);

/// <summary>Datos con los que se crea o edita una instancia.</summary>
public sealed record DatosInstancia(string Nombre, string? Color, bool Perime, bool EsBaja, int? Orden, bool Activa);

/// <summary>Una etapa del catálogo, con la cantidad de registros que la usan.</summary>
public sealed record FilaEtapaCatalogo(int Id, byte Flujo, string? Codigo, string Nombre, int? Orden, int? Dias,
    bool DiasHabiles, bool Interrumpe, bool CortaPerencion, bool EsMonitorio, bool EsEjecucion, bool EsDiligencia,
    int? EtapaAnteriorId, int? EtapaSiguienteId, bool PasaSiguienteAutomatica, bool GeneraTarea,
    byte TareaParaUsuarios, byte? HitoSistema, int? InstanciaId, string? Instancia, bool Activa, int Registros,
    byte[] Version)
{
    /// <summary>Las etapas con hito de sistema las usa el motor: su hito no se edita desde la pantalla.</summary>
    public bool EsDeSistema => HitoSistema is not null;
}

/// <summary>
/// Datos con los que se crea o edita una etapa. No incluye HitoSistema: lo usa el motor de vencimientos
/// y no se toca desde la UI.
/// </summary>
public sealed record DatosEtapa(byte Flujo, string? Codigo, string Nombre, int? Orden, int? Dias, bool DiasHabiles,
    bool Interrumpe, bool CortaPerencion, bool EsMonitorio, bool EsEjecucion, bool EsDiligencia,
    int? EtapaAnteriorId, int? EtapaSiguienteId, bool PasaSiguienteAutomatica, bool GeneraTarea,
    byte TareaParaUsuarios, int? InstanciaId, bool Activa);

/// <summary>
/// ABM de los catálogos de configuración del Estudio: instancias (macroestados del expediente) y etapas
/// (los pasos del trámite, con sus plazos y automatismos).
///
/// <para>Exige <c>configuracion.administrar</c>. Nada se borra: las filas se desactivan, porque los
/// expedientes y su historial las referencian (12 claves foráneas apuntan a estos catálogos).</para>
/// </summary>
public sealed class ServicioCatalogoEtapas(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos)
{
    public const byte FlujoPrincipal = 1;
    public const byte FlujoCautelar = 2;

    // ----------------------------------------------------------------- instancias

    public async Task<IReadOnlyList<FilaInstancia>> ListarInstanciasAsync(CancellationToken ct = default)
    {
        var instancias = await db.Instancia.OrderBy(i => i.Orden).ThenBy(i => i.Nombre)
            .Select(i => new { i.Id, i.Nombre, i.Color, i.Perime, i.EsBaja, i.Orden, i.Activa, i.Version })
            .ToListAsync(ct);

        // Cuántos expedientes tienen cada instancia como actual: es lo que impide desactivarla sin pensar.
        var uso = (await db.Expediente.Where(e => e.InstanciaActualId != null)
                .GroupBy(e => e.InstanciaActualId!.Value)
                .Select(g => new { InstanciaId = g.Key, Cantidad = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.InstanciaId, x => x.Cantidad);

        return instancias.Select(i => new FilaInstancia(i.Id, i.Nombre, i.Color, i.Perime, i.EsBaja, i.Orden,
            i.Activa, uso.GetValueOrDefault(i.Id), i.Version)).ToList();
    }

    public async Task<ResultadoOperacion> CrearInstanciaAsync(DatosInstancia datos, CancellationToken ct = default)
    {
        if (await SinPermisoAsync(ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (ValidarInstancia(limpio) is { } error) return ResultadoOperacion.Fallo(error);
        if (await db.Instancia.AnyAsync(i => i.Nombre == limpio.Nombre, ct))
            return ResultadoOperacion.Fallo("Ya existe una instancia con ese nombre.");

        var instancia = new Instancia
        {
            EstudioId = contexto.EstudioId!.Value,
            Nombre = limpio.Nombre,
            Color = limpio.Color,
            Perime = limpio.Perime,
            EsBaja = limpio.EsBaja,
            Orden = limpio.Orden,
            Activa = limpio.Activa,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Instancia.Add(instancia);
        await db.SaveChangesAsync(ct);
        Auditar(1, "Instancia", instancia.Id.ToString(), new { antes = (object?)null, despues = limpio });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> EditarInstanciaAsync(int id, DatosInstancia datos, byte[] version,
        CancellationToken ct = default)
    {
        if (await SinPermisoAsync(ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (ValidarInstancia(limpio) is { } error) return ResultadoOperacion.Fallo(error);

        var instancia = await db.Instancia.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (instancia is null) return ResultadoOperacion.Fallo("La instancia no existe en este Estudio.");
        if (!instancia.Version.SequenceEqual(version))
            return ResultadoOperacion.Fallo("La instancia cambió mientras la editabas. Recargá y volvé a intentar.");
        if (await db.Instancia.AnyAsync(i => i.Id != id && i.Nombre == limpio.Nombre, ct))
            return ResultadoOperacion.Fallo("Ya existe otra instancia con ese nombre.");

        // Desactivarla con expedientes en esa instancia dejaría a esos expedientes en un estado inválido.
        if (instancia.Activa && !limpio.Activa)
        {
            var enUso = await db.Expediente.CountAsync(e => e.InstanciaActualId == id, ct);
            if (enUso > 0)
                return ResultadoOperacion.Fallo(
                    $"No se puede desactivar: {enUso} expediente(s) están en esta instancia. Movelos primero.");
        }

        var cambios = new Dictionary<string, object?[]>();
        Cambiar(cambios, "nombre", instancia.Nombre, limpio.Nombre, v => instancia.Nombre = v!);
        Cambiar(cambios, "color", instancia.Color, limpio.Color, v => instancia.Color = v);
        Cambiar(cambios, "perime", instancia.Perime, limpio.Perime, v => instancia.Perime = v);
        Cambiar(cambios, "esBaja", instancia.EsBaja, limpio.EsBaja, v => instancia.EsBaja = v);
        Cambiar(cambios, "orden", instancia.Orden, limpio.Orden, v => instancia.Orden = v);
        Cambiar(cambios, "activa", instancia.Activa, limpio.Activa, v => instancia.Activa = v);
        if (cambios.Count == 0) return ResultadoOperacion.Ok();

        instancia.ModificadoUtc = DateTime.UtcNow;
        instancia.ModificadoPor = contexto.UsuarioId;
        Auditar(2, "Instancia", id.ToString(),
            cambios.ToDictionary(c => c.Key, c => new { antes = c.Value[0], despues = c.Value[1] }));
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- etapas

    public async Task<IReadOnlyList<FilaEtapaCatalogo>> ListarEtapasAsync(CancellationToken ct = default)
    {
        var etapas = await db.Etapa.OrderBy(e => e.Flujo).ThenBy(e => e.Orden).ThenBy(e => e.Nombre)
            .Select(e => new
            {
                e.Id, e.Flujo, e.Codigo, e.Nombre, e.Orden, e.Dias, e.DiasHabiles, e.Interrumpe, e.CortaPerencion,
                e.EsMonitorio, e.EsEjecucion, e.EsDiligencia, e.EtapaAnteriorId, e.EtapaSiguienteId,
                e.PasaSiguienteAutomatica, e.GeneraTarea, e.TareaParaUsuarios, e.HitoSistema, e.InstanciaId,
                Instancia = e.Instancia != null ? e.Instancia.Nombre : null,
                e.Activa, e.Version
            }).ToListAsync(ct);

        var uso = (await db.EtapaExpediente.GroupBy(x => x.EtapaId)
                .Select(g => new { EtapaId = g.Key, Cantidad = g.Count() }).ToListAsync(ct))
            .ToDictionary(x => x.EtapaId, x => x.Cantidad);

        return etapas.Select(e => new FilaEtapaCatalogo(e.Id, e.Flujo, e.Codigo, e.Nombre, e.Orden, e.Dias,
            e.DiasHabiles, e.Interrumpe, e.CortaPerencion, e.EsMonitorio, e.EsEjecucion, e.EsDiligencia,
            e.EtapaAnteriorId, e.EtapaSiguienteId, e.PasaSiguienteAutomatica, e.GeneraTarea, e.TareaParaUsuarios,
            e.HitoSistema, e.InstanciaId, e.Instancia, e.Activa, uso.GetValueOrDefault(e.Id), e.Version)).ToList();
    }

    /// <summary>Etapas para los selectores de encadenamiento (anterior y siguiente).</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> EtapasParaEncadenarAsync(byte flujo, CancellationToken ct = default) =>
        await db.Etapa.Where(e => e.Flujo == flujo).OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(ct);

    public async Task<ResultadoOperacion> CrearEtapaAsync(DatosEtapa datos, CancellationToken ct = default)
    {
        if (await SinPermisoAsync(ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (await ValidarEtapaAsync(limpio, null, ct) is { } error) return ResultadoOperacion.Fallo(error);

        var etapa = new Etapa
        {
            EstudioId = contexto.EstudioId!.Value,
            Flujo = limpio.Flujo,
            Codigo = limpio.Codigo,
            Nombre = limpio.Nombre,
            Orden = limpio.Orden,
            Dias = limpio.Dias,
            DiasHabiles = limpio.DiasHabiles,
            Interrumpe = limpio.Interrumpe,
            CortaPerencion = limpio.CortaPerencion,
            EsMonitorio = limpio.EsMonitorio,
            EsEjecucion = limpio.EsEjecucion,
            EsDiligencia = limpio.EsDiligencia,
            EtapaAnteriorId = limpio.EtapaAnteriorId,
            EtapaSiguienteId = limpio.EtapaSiguienteId,
            PasaSiguienteAutomatica = limpio.PasaSiguienteAutomatica,
            GeneraTarea = limpio.GeneraTarea,
            TareaParaUsuarios = limpio.TareaParaUsuarios,
            HitoSistema = null,   // lo maneja el motor, no la pantalla
            InstanciaId = limpio.InstanciaId,
            Activa = limpio.Activa,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Etapa.Add(etapa);
        await db.SaveChangesAsync(ct);
        Auditar(1, "Etapa", etapa.Id.ToString(), new { antes = (object?)null, despues = limpio });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> EditarEtapaAsync(int id, DatosEtapa datos, byte[] version,
        CancellationToken ct = default)
    {
        if (await SinPermisoAsync(ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (await ValidarEtapaAsync(limpio, id, ct) is { } error) return ResultadoOperacion.Fallo(error);

        var etapa = await db.Etapa.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (etapa is null) return ResultadoOperacion.Fallo("La etapa no existe en este Estudio.");
        if (!etapa.Version.SequenceEqual(version))
            return ResultadoOperacion.Fallo("La etapa cambió mientras la editabas. Recargá y volvé a intentar.");

        // Cambiarle el flujo rompería el historial y el encadenamiento ya registrado.
        if (etapa.Flujo != limpio.Flujo && await db.EtapaExpediente.AnyAsync(x => x.EtapaId == id, ct))
            return ResultadoOperacion.Fallo("No se puede cambiar el flujo: la etapa ya está registrada en expedientes.");

        // Una etapa que es hito del sistema no se puede desactivar: el motor la necesita.
        if (etapa.Activa && !limpio.Activa && etapa.HitoSistema is not null)
            return ResultadoOperacion.Fallo("Esta etapa es un hito del sistema y no se puede desactivar.");

        var cambios = new Dictionary<string, object?[]>();
        Cambiar(cambios, "flujo", etapa.Flujo, limpio.Flujo, v => etapa.Flujo = v);
        Cambiar(cambios, "codigo", etapa.Codigo, limpio.Codigo, v => etapa.Codigo = v);
        Cambiar(cambios, "nombre", etapa.Nombre, limpio.Nombre, v => etapa.Nombre = v!);
        Cambiar(cambios, "orden", etapa.Orden, limpio.Orden, v => etapa.Orden = v);
        Cambiar(cambios, "dias", etapa.Dias, limpio.Dias, v => etapa.Dias = v);
        Cambiar(cambios, "diasHabiles", etapa.DiasHabiles, limpio.DiasHabiles, v => etapa.DiasHabiles = v);
        Cambiar(cambios, "interrumpe", etapa.Interrumpe, limpio.Interrumpe, v => etapa.Interrumpe = v);
        Cambiar(cambios, "cortaPerencion", etapa.CortaPerencion, limpio.CortaPerencion, v => etapa.CortaPerencion = v);
        Cambiar(cambios, "esMonitorio", etapa.EsMonitorio, limpio.EsMonitorio, v => etapa.EsMonitorio = v);
        Cambiar(cambios, "esEjecucion", etapa.EsEjecucion, limpio.EsEjecucion, v => etapa.EsEjecucion = v);
        Cambiar(cambios, "esDiligencia", etapa.EsDiligencia, limpio.EsDiligencia, v => etapa.EsDiligencia = v);
        Cambiar(cambios, "etapaAnteriorId", etapa.EtapaAnteriorId, limpio.EtapaAnteriorId, v => etapa.EtapaAnteriorId = v);
        Cambiar(cambios, "etapaSiguienteId", etapa.EtapaSiguienteId, limpio.EtapaSiguienteId, v => etapa.EtapaSiguienteId = v);
        Cambiar(cambios, "pasaSiguienteAutomatica", etapa.PasaSiguienteAutomatica, limpio.PasaSiguienteAutomatica,
            v => etapa.PasaSiguienteAutomatica = v);
        Cambiar(cambios, "generaTarea", etapa.GeneraTarea, limpio.GeneraTarea, v => etapa.GeneraTarea = v);
        Cambiar(cambios, "tareaParaUsuarios", etapa.TareaParaUsuarios, limpio.TareaParaUsuarios,
            v => etapa.TareaParaUsuarios = v);
        Cambiar(cambios, "instanciaId", etapa.InstanciaId, limpio.InstanciaId, v => etapa.InstanciaId = v);
        Cambiar(cambios, "activa", etapa.Activa, limpio.Activa, v => etapa.Activa = v);
        if (cambios.Count == 0) return ResultadoOperacion.Ok();

        etapa.ModificadoUtc = DateTime.UtcNow;
        etapa.ModificadoPor = contexto.UsuarioId;
        Auditar(2, "Etapa", id.ToString(),
            cambios.ToDictionary(c => c.Key, c => new { antes = c.Value[0], despues = c.Value[1] }));
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- validación

    private static DatosInstancia Normalizar(DatosInstancia d) => d with
    {
        Nombre = (d.Nombre ?? string.Empty).Trim(),
        Color = Limpiar(d.Color, 7),
    };

    private static string? ValidarInstancia(DatosInstancia d)
    {
        if (string.IsNullOrWhiteSpace(d.Nombre)) return "El nombre es obligatorio.";
        if (d.Nombre.Length > 50) return "El nombre no puede tener más de 50 caracteres.";
        if (d.Color is not null && !EsColorHexadecimal(d.Color))
            return "El color tiene que ser hexadecimal, con el formato #1D4ED8.";
        if (d.Orden is < 0) return "El orden no puede ser negativo.";
        return null;
    }

    /// <summary>Acepta solo "#" más 6 dígitos hexadecimales, que es lo que admite la columna char(7).</summary>
    private static bool EsColorHexadecimal(string color) =>
        color.Length == 7 && color[0] == '#' && color[1..].All(Uri.IsHexDigit);

    private static DatosEtapa Normalizar(DatosEtapa d) => d with
    {
        Nombre = (d.Nombre ?? string.Empty).Trim(),
        Codigo = Limpiar(d.Codigo, 20),
        EtapaAnteriorId = d.EtapaAnteriorId is 0 ? null : d.EtapaAnteriorId,
        EtapaSiguienteId = d.EtapaSiguienteId is 0 ? null : d.EtapaSiguienteId,
        // Sin etapa siguiente no hay a dónde pasar automáticamente.
        PasaSiguienteAutomatica = d.PasaSiguienteAutomatica && d.EtapaSiguienteId is not (null or 0),
        // Si no genera tarea, a quién notificar no aplica.
        TareaParaUsuarios = d.GeneraTarea ? d.TareaParaUsuarios : TareaPara.Ninguno,
        // Solo el flujo principal mueve la instancia (lo exige CK_conf_Etapa_4).
        InstanciaId = d.Flujo == FlujoPrincipal && d.InstanciaId is not 0 ? d.InstanciaId : null,
    };

    private async Task<string?> ValidarEtapaAsync(DatosEtapa d, int? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(d.Nombre)) return "El nombre es obligatorio.";
        if (d.Nombre.Length > 100) return "El nombre no puede tener más de 100 caracteres.";
        if (d.Flujo is not (FlujoPrincipal or FlujoCautelar)) return "El flujo tiene que ser principal o cautelar.";
        if (!TareaPara.Valido(d.TareaParaUsuarios)) return "La opción de notificación no es válida.";
        if (d.Dias is < 0) return "Los días no pueden ser negativos.";
        if (d.Dias is > 3650) return "Los días no pueden ser más de 3650 (diez años).";
        if (d.Orden is < 0) return "El orden no puede ser negativo.";

        if (await db.Etapa.AnyAsync(e => e.Nombre == d.Nombre && e.Flujo == d.Flujo && (id == null || e.Id != id), ct))
            return "Ya existe otra etapa con ese nombre en el mismo flujo.";
        if (d.Codigo is not null
            && await db.Etapa.AnyAsync(e => e.Codigo == d.Codigo && (id == null || e.Id != id), ct))
            return "Ya existe otra etapa con ese código.";

        if (d.PasaSiguienteAutomatica && d.EtapaSiguienteId is null)
            return "Para pasar automáticamente hay que elegir la etapa siguiente.";

        // Cada etapa principal define a qué instancia pasa el expediente: sin eso, el macroestado no avanza.
        if (d.Flujo == FlujoPrincipal)
        {
            if (d.InstanciaId is null)
                return "Elegí la instancia a la que pasa el expediente con esta etapa.";
            if (!await db.Instancia.AnyAsync(i => i.Id == d.InstanciaId.Value, ct))
                return "La instancia elegida no existe.";
        }

        return await ValidarEncadenamientoAsync(d, id, ct);
    }

    /// <summary>
    /// El encadenamiento no puede apuntarse a sí mismo, cruzar flujos ni formar un ciclo
    /// (A → B → A dejaría al paso automático girando sin fin).
    /// </summary>
    private async Task<string?> ValidarEncadenamientoAsync(DatosEtapa d, int? id, CancellationToken ct)
    {
        foreach (var (vecinoId, rotulo) in new[] { (d.EtapaAnteriorId, "anterior"), (d.EtapaSiguienteId, "siguiente") })
        {
            if (vecinoId is null) continue;
            if (vecinoId == id) return $"La etapa {rotulo} no puede ser la etapa misma.";

            var vecino = await db.Etapa.Where(e => e.Id == vecinoId)
                .Select(e => new { e.Flujo }).FirstOrDefaultAsync(ct);
            if (vecino is null) return $"La etapa {rotulo} elegida no existe.";
            if (vecino.Flujo != d.Flujo) return $"La etapa {rotulo} tiene que ser del mismo flujo.";
        }

        if (id is null || d.EtapaSiguienteId is null) return null;

        // Recorre la cadena de siguientes desde el nuevo destino: si vuelve a esta etapa, hay ciclo.
        var enlaces = await db.Etapa.Where(e => e.EtapaSiguienteId != null)
            .Select(e => new { e.Id, Siguiente = e.EtapaSiguienteId!.Value }).ToListAsync(ct);
        var siguientePorEtapa = enlaces.ToDictionary(e => e.Id, e => e.Siguiente);
        siguientePorEtapa[id.Value] = d.EtapaSiguienteId.Value;

        var visitadas = new HashSet<int> { id.Value };
        var actual = d.EtapaSiguienteId.Value;
        while (siguientePorEtapa.TryGetValue(actual, out var proxima))
        {
            if (!visitadas.Add(actual)) break;   // ciclo que no incluye a esta etapa: ya existía
            if (proxima == id.Value)
                return "Ese encadenamiento forma un círculo: la etapa terminaría apuntándose a sí misma.";
            actual = proxima;
        }
        return null;
    }

    // ----------------------------------------------------------------- internos

    private async Task<ResultadoOperacion?> SinPermisoAsync(CancellationToken ct) =>
        await permisos.TieneAsync(Permisos.De(Modulos.Configuracion, Acciones.Administrar), ct)
            ? null
            : ResultadoOperacion.Fallo("No tenés permiso para administrar la configuración del Estudio.");

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

    private void Auditar(byte accion, string entidad, string entidadId, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Configuracion,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

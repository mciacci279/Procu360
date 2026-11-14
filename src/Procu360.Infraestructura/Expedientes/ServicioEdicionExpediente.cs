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
/// Objeto de la deuda: qué se le reclama (la cuenta o resolución y la identificación del inmueble o dominio).
/// Hoy el expediente tiene una sola cuenta; el modelo multi-cuenta está pendiente de definición.
/// </summary>
public sealed record DatosObjetoDeuda(string? NroCuenta, string? Nomenclatura, string? Dominio, string? Designacion,
    string? MatriculaRpi, string? DomicilioObjeto, byte[] Version);

/// <summary>
/// Título de deuda. <paramref name="SumaPeriodos"/> y <paramref name="CapitalManual"/> no se guardan:
/// se calculan para que la ficha pueda advertir cuando el capital dejó de seguir a los períodos.
/// </summary>
public sealed record DatosTituloDeuda(decimal? MontoTitulo, decimal? MontoCapital, DateOnly? FechaTitulo,
    string? NroLiquidacion, byte[] Version, decimal SumaPeriodos = 0m, bool CapitalManual = false)
{
    /// <summary>Diferencia entre el capital cargado y la suma de los períodos.</summary>
    public decimal Diferencia => (MontoCapital ?? 0m) - SumaPeriodos;

    /// <summary>True si el capital no coincide con la suma de los períodos (con tolerancia de un centavo).</summary>
    public bool Descorrelacionado => Math.Abs(Diferencia) >= 0.01m;
}

/// <summary>
/// Comparecencia del demandado y los datos de su abogado. Si no compareció, la fecha y el abogado no aplican.
/// </summary>
public sealed record DatosComparecencia(bool Comparecio, DateOnly? FechaComparecio, string? AbogadoContraparte,
    string? DomicilioAbogadoContraparte, byte[] Version);

/// <summary>Clasificación del expediente para listados y gestión: región, campaña y banderas del contribuyente.</summary>
public sealed record DatosClasificacion(int? RegionId, int? CampaniaId, bool EsGranContribuyente, bool EsFisca,
    byte[] Version);

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

    public async Task<DatosObjetoDeuda?> ObjetoDeudaAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosObjetoDeuda(e.NroCuenta, e.Nomenclatura, e.Dominio, e.Designacion,
                e.MatriculaRpi, e.DomicilioObjeto, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DatosTituloDeuda?> TituloDeudaAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        var datos = await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosTituloDeuda(e.MontoTitulo, e.MontoCapital, e.FechaTitulo, e.NroLiquidacion, e.Version))
            .SingleOrDefaultAsync(ct);
        if (datos is null) return null;

        return datos with
        {
            SumaPeriodos = await SumaPeriodosAsync(expedienteId, ct),
            CapitalManual = await CapitalEsManualAsync(expedienteId, ct)
        };
    }

    /// <summary>Suma de tributo y recargo de todos los períodos adeudados del expediente.</summary>
    public async Task<decimal> SumaPeriodosAsync(long expedienteId, CancellationToken ct = default) =>
        await db.PeriodoAdeudado.Where(p => p.ExpedienteId == expedienteId)
            .SumAsync(p => (decimal?)(p.Tributo + p.Recargo), ct) ?? 0m;

    /// <summary>
    /// True si alguien editó el capital a mano alguna vez. Se deduce de la auditoría —que ya registra
    /// cada cambio de campo— para no agregar una columna solo a efectos de esta marca.
    /// </summary>
    private Task<bool> CapitalEsManualAsync(long expedienteId, CancellationToken ct) =>
        db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Expediente"
            && a.EntidadId == expedienteId.ToString()
            && a.CambiosJson != null && a.CambiosJson.Contains(MarcaCapitalManual), ct);

    /// <summary>Marca que deja el guardado manual del capital en la auditoría.</summary>
    private const string MarcaCapitalManual = "montoCapitalManual";

    /// <summary>
    /// Pone el capital igual a la suma de los períodos. Lo usa el ABM de períodos al agregar, editar o
    /// eliminar, y el botón de resincronizar de la ficha. No hace nada si el capital es manual, salvo
    /// que <paramref name="forzar"/> sea true (el usuario pidió expresamente volver a sincronizarlo).
    /// </summary>
    public async Task<ResultadoOperacion> SincronizarCapitalAsync(long expedienteId, bool forzar = false,
        CancellationToken ct = default)
    {
        if (!await permisos.TieneAsync(Permisos.De(Modulos.Expedientes, Acciones.Editar), ct))
            return ResultadoOperacion.Fallo("No tenés permiso para editar expedientes.");
        if (!await VisibleAsync(expedienteId, ct))
            return ResultadoOperacion.Fallo("El expediente no existe o no está en tu alcance.");

        if (!forzar && await CapitalEsManualAsync(expedienteId, ct)) return ResultadoOperacion.Ok();

        var expediente = await db.Expediente.FirstAsync(e => e.Id == expedienteId, ct);
        var suma = await SumaPeriodosAsync(expedienteId, ct);
        if (expediente.MontoCapital == suma) return ResultadoOperacion.Ok();

        var antes = expediente.MontoCapital;
        expediente.MontoCapital = suma;
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
            Accion = 2,
            Resultado = 1,
            // Sin la marca de manual: este cambio lo hizo el sistema siguiendo los períodos.
            CambiosJson = JsonSerializer.Serialize(new
            {
                montoCapital = new { antes, despues = suma, origen = "períodos" }
            })
        });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<DatosComparecencia?> ComparecenciaAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosComparecencia(e.Comparecio, e.FechaComparecio, e.AbogadoContraparte,
                e.DomicilioAbogadoContraparte, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DatosClasificacion?> ClasificacionAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return null;
        return await db.Expediente.Where(e => e.Id == expedienteId)
            .Select(e => new DatosClasificacion(e.RegionId, e.CampaniaId, e.EsGranContribuyente, e.EsFisca, e.Version))
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>Regiones del Estudio, para el selector de clasificación.</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> RegionesAsync(CancellationToken ct = default) =>
        await db.Region.OrderBy(r => r.Nombre).Select(r => new OpcionCatalogo(r.Id, r.Nombre)).ToListAsync(ct);

    /// <summary>Campañas de gestión del Estudio, de la más reciente a la más vieja.</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> CampaniasAsync(CancellationToken ct = default) =>
        await db.Campania.OrderByDescending(c => c.Numero)
            .Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync(ct);

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

    public async Task<ResultadoOperacion> GuardarObjetoDeudaAsync(long expedienteId, DatosObjetoDeuda datos, CancellationToken ct = default)
    {
        // Los largos son los de dbo.Expediente (ver Procu360DbContext).
        var limpio = datos with
        {
            NroCuenta = Limpiar(datos.NroCuenta, 50),
            Nomenclatura = Limpiar(datos.Nomenclatura, 50),
            Dominio = Limpiar(datos.Dominio, 20),
            Designacion = Limpiar(datos.Designacion, 100),
            MatriculaRpi = Limpiar(datos.MatriculaRpi, 50),
            DomicilioObjeto = Limpiar(datos.DomicilioObjeto, 250),
        };

        return await GuardarAsync(expedienteId, limpio.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "nroCuenta", e.NroCuenta, limpio.NroCuenta, v => e.NroCuenta = v);
            Cambiar(cambios, "nomenclatura", e.Nomenclatura, limpio.Nomenclatura, v => e.Nomenclatura = v);
            Cambiar(cambios, "dominio", e.Dominio, limpio.Dominio, v => e.Dominio = v);
            Cambiar(cambios, "designacion", e.Designacion, limpio.Designacion, v => e.Designacion = v);
            Cambiar(cambios, "matriculaRpi", e.MatriculaRpi, limpio.MatriculaRpi, v => e.MatriculaRpi = v);
            Cambiar(cambios, "domicilioObjeto", e.DomicilioObjeto, limpio.DomicilioObjeto, v => e.DomicilioObjeto = v);
            return cambios;
        });
    }

    /// <summary>
    /// Guarda el título de deuda. Si el capital cambia, queda marcado como manual en la auditoría y deja
    /// de recalcularse solo con los períodos, hasta que se resincronice expresamente.
    /// </summary>
    public async Task<ResultadoOperacion> GuardarTituloDeudaAsync(long expedienteId, DatosTituloDeuda datos,
        CancellationToken ct = default)
    {
        var limpio = datos with
        {
            NroLiquidacion = Limpiar(datos.NroLiquidacion, 50),
            MontoTitulo = datos.MontoTitulo is decimal t ? decimal.Round(t, 2) : null,
            MontoCapital = datos.MontoCapital is decimal k ? decimal.Round(k, 2) : null,
        };

        if (limpio.MontoTitulo is < 0) return ResultadoOperacion.Fallo("El monto del título no puede ser negativo.");
        if (limpio.MontoCapital is < 0) return ResultadoOperacion.Fallo("El capital no puede ser negativo.");
        if (limpio.FechaTitulo is DateOnly f && f > MiDia.ServicioMiDia.HoyEnCordoba())
            return ResultadoOperacion.Fallo("La fecha del título no puede ser futura.");

        return await GuardarAsync(expedienteId, limpio.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "montoTitulo", e.MontoTitulo, limpio.MontoTitulo, v => e.MontoTitulo = v);
            Cambiar(cambios, "fechaTitulo", e.FechaTitulo, limpio.FechaTitulo, v => e.FechaTitulo = v);
            Cambiar(cambios, "nroLiquidacion", e.NroLiquidacion, limpio.NroLiquidacion, v => e.NroLiquidacion = v);
            // El nombre del campo marca el cambio como manual: lo distingue del que hace el sistema
            // al seguir los períodos, que audita "montoCapital" a secas.
            Cambiar(cambios, MarcaCapitalManual, e.MontoCapital, limpio.MontoCapital, v => e.MontoCapital = v);
            return cambios;
        });
    }

    public async Task<ResultadoOperacion> GuardarComparecenciaAsync(long expedienteId, DatosComparecencia datos,
        CancellationToken ct = default)
    {
        // Si no compareció, no hay fecha ni abogado que guardar.
        var limpio = datos.Comparecio
            ? datos with
            {
                AbogadoContraparte = Limpiar(datos.AbogadoContraparte, 250),
                DomicilioAbogadoContraparte = Limpiar(datos.DomicilioAbogadoContraparte, 250),
            }
            : datos with
            {
                FechaComparecio = null,
                AbogadoContraparte = null,
                DomicilioAbogadoContraparte = null,
            };

        if (limpio.Comparecio && limpio.FechaComparecio is null)
            return ResultadoOperacion.Fallo("Si compareció, indicá la fecha.");
        if (limpio.FechaComparecio is DateOnly f && f > MiDia.ServicioMiDia.HoyEnCordoba())
            return ResultadoOperacion.Fallo("La fecha de comparecencia no puede ser futura.");

        return await GuardarAsync(expedienteId, limpio.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "comparecio", e.Comparecio, limpio.Comparecio, v => e.Comparecio = v);
            Cambiar(cambios, "fechaComparecio", e.FechaComparecio, limpio.FechaComparecio, v => e.FechaComparecio = v);
            Cambiar(cambios, "abogadoContraparte", e.AbogadoContraparte, limpio.AbogadoContraparte,
                v => e.AbogadoContraparte = v);
            Cambiar(cambios, "domicilioAbogadoContraparte", e.DomicilioAbogadoContraparte,
                limpio.DomicilioAbogadoContraparte, v => e.DomicilioAbogadoContraparte = v);
            return cambios;
        });
    }

    public async Task<ResultadoOperacion> GuardarClasificacionAsync(long expedienteId, DatosClasificacion datos,
        CancellationToken ct = default)
    {
        var limpio = datos with
        {
            RegionId = datos.RegionId is 0 ? null : datos.RegionId,
            CampaniaId = datos.CampaniaId is 0 ? null : datos.CampaniaId,
        };

        if (limpio.RegionId is int r && !await db.Region.AnyAsync(x => x.Id == r, ct))
            return ResultadoOperacion.Fallo("La región elegida no existe.");
        if (limpio.CampaniaId is int c && !await db.Campania.AnyAsync(x => x.Id == c, ct))
            return ResultadoOperacion.Fallo("La campaña elegida no existe.");

        return await GuardarAsync(expedienteId, limpio.Version, ct, e =>
        {
            var cambios = new Dictionary<string, object?[]>();
            Cambiar(cambios, "regionId", e.RegionId, limpio.RegionId, v => e.RegionId = v);
            Cambiar(cambios, "campaniaId", e.CampaniaId, limpio.CampaniaId, v => e.CampaniaId = v);
            Cambiar(cambios, "esGranContribuyente", e.EsGranContribuyente, limpio.EsGranContribuyente,
                v => e.EsGranContribuyente = v);
            Cambiar(cambios, "esFisca", e.EsFisca, limpio.EsFisca, v => e.EsFisca = v);
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

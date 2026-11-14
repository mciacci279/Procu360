using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Expedientes;

/// <summary>Roles de una persona en el expediente. Coinciden con dbo.ExpedientePersona.Rol.</summary>
public static class RolesPersonaExpediente
{
    public const byte Demandado = 1;
    public const byte Codemandado = 2;
    public const byte ResponsableSolidario = 3;

    public static string Nombre(byte rol) => rol switch
    {
        Demandado => "Demandado",
        Codemandado => "Codemandado",
        ResponsableSolidario => "Responsable solidario",
        _ => "Otro"
    };

    public static bool Valido(byte rol) => rol is Demandado or Codemandado or ResponsableSolidario;
}

/// <summary>Una persona vinculada al expediente, con sus datos y el vínculo (rol y posición).</summary>
public sealed record VinculoPersona(
    long VinculoId,
    long PersonaId,
    byte Rol,
    byte Posicion,
    bool EsPersonaFisica,
    string? Apellido,
    string? Nombre,
    string? RazonSocial,
    int? TipoDocumentoId,
    string? NroDocumento,
    string? Cuit,
    string? Telefono1,
    string? Email,
    string? Domicilio,
    byte[] VersionVinculo,
    byte[] VersionPersona)
{
    /// <summary>Razón social para empresas; "Apellido, Nombre" para personas físicas.</summary>
    public string NombreMostrado => EsPersonaFisica
        ? string.Join(", ", new[] { Apellido, Nombre }.Where(p => !string.IsNullOrWhiteSpace(p)))
        : (RazonSocial ?? string.Empty);
}

/// <summary>Datos con los que se da de alta o se edita una persona del expediente.</summary>
public sealed record DatosPersona(
    bool EsPersonaFisica,
    string? Apellido,
    string? Nombre,
    string? RazonSocial,
    int? TipoDocumentoId,
    string? NroDocumento,
    string? Cuit,
    string? Telefono1,
    string? Email,
    byte Rol);

/// <summary>Resultado de buscar una persona ya existente en el Estudio, para no duplicarla.</summary>
public sealed record PersonaEncontrada(long PersonaId, string NombreMostrado, string? NroDocumento, string? Cuit,
    bool YaEnElExpediente);

/// <summary>
/// ABM de las personas de un expediente (demandados, codemandados y responsables solidarios).
///
/// <para>La <see cref="Persona"/> es compartida entre expedientes y el vínculo
/// <see cref="ExpedientePersona"/> es el que le da el rol en este expediente. Por eso "quitar" desvincula
/// y no borra la persona: puede estar en otros expedientes.</para>
///
/// <para>Cada operación exige <c>expedientes.editar</c> y alcance sobre el expediente, y queda auditada.</para>
/// </summary>
public sealed class ServicioPersonasExpediente(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos,
    AlcanceExpedientes alcance)
{
    // ----------------------------------------------------------------- lectura

    public async Task<IReadOnlyList<VinculoPersona>> ListarAsync(long expedienteId, CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return [];

        var filas = await db.ExpedientePersona.Where(ep => ep.ExpedienteId == expedienteId && !ep.Persona.Anulado)
            .OrderBy(ep => ep.Rol).ThenBy(ep => ep.Posicion)
            .Select(ep => new
            {
                ep.Id,
                ep.PersonaId,
                ep.Rol,
                ep.Posicion,
                ep.Persona.EsPersonaFisica,
                ep.Persona.Apellido,
                ep.Persona.Nombre,
                ep.Persona.RazonSocial,
                ep.Persona.TipoDocumentoId,
                ep.Persona.NroDocumento,
                ep.Persona.Cuit,
                ep.Persona.Telefono1,
                ep.Persona.Email,
                VersionVinculo = ep.Version,
                VersionPersona = ep.Persona.Version
            }).ToListAsync(ct);

        var personaIds = filas.Select(f => f.PersonaId).Distinct().ToList();
        var domicilios = await db.Domicilio.Where(d => d.Principal && personaIds.Contains(d.PersonaId))
            .Select(d => new { d.PersonaId, d.Calle, d.Numero }).ToListAsync(ct);

        return filas.Select(f => new VinculoPersona(
            f.Id, f.PersonaId, f.Rol, f.Posicion, f.EsPersonaFisica, f.Apellido, f.Nombre, f.RazonSocial,
            f.TipoDocumentoId, f.NroDocumento, f.Cuit, f.Telefono1, f.Email,
            domicilios.Where(d => d.PersonaId == f.PersonaId)
                .Select(d => $"{d.Calle} {d.Numero}".Trim()).FirstOrDefault(),
            f.VersionVinculo, f.VersionPersona)).ToList();
    }

    /// <summary>Tipos de documento del catálogo (DNI, LE, LC, Pasaporte, CUIT). Es global, no por Estudio.</summary>
    public async Task<IReadOnlyList<OpcionCatalogo>> TiposDocumentoAsync(CancellationToken ct = default) =>
        await db.TipoDocumento.OrderBy(t => t.Id).Select(t => new OpcionCatalogo(t.Id, t.Nombre)).ToListAsync(ct);

    /// <summary>
    /// Busca personas del Estudio por documento, CUIT o nombre, para reutilizarlas en vez de duplicarlas.
    /// Marca las que ya están en este expediente.
    /// </summary>
    public async Task<IReadOnlyList<PersonaEncontrada>> BuscarAsync(long expedienteId, string texto,
        CancellationToken ct = default)
    {
        if (!await VisibleAsync(expedienteId, ct)) return [];
        var t = (texto ?? string.Empty).Trim();
        if (t.Length < 3) return [];

        var yaEstan = await db.ExpedientePersona.Where(ep => ep.ExpedienteId == expedienteId)
            .Select(ep => ep.PersonaId).ToListAsync(ct);

        var filas = await db.Persona
            .Where(p => !p.Anulado && (
                (p.NroDocumento != null && p.NroDocumento.Contains(t))
                || (p.Cuit != null && p.Cuit.Contains(t))
                || (p.Apellido != null && p.Apellido.Contains(t))
                || (p.RazonSocial != null && p.RazonSocial.Contains(t))))
            .OrderBy(p => p.Apellido).ThenBy(p => p.RazonSocial)
            .Take(20)
            .Select(p => new { p.Id, p.EsPersonaFisica, p.Apellido, p.Nombre, p.RazonSocial, p.NroDocumento, p.Cuit })
            .ToListAsync(ct);

        return filas.Select(p => new PersonaEncontrada(
            p.Id,
            p.EsPersonaFisica
                ? string.Join(", ", new[] { p.Apellido, p.Nombre }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : (p.RazonSocial ?? string.Empty),
            p.NroDocumento, p.Cuit, yaEstan.Contains(p.Id))).ToList();
    }

    // ----------------------------------------------------------------- alta

    /// <summary>Crea una persona nueva y la vincula al expediente.</summary>
    public async Task<ResultadoOperacion> AgregarNuevaAsync(long expedienteId, DatosPersona datos,
        CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (Validar(limpio) is { } error) return ResultadoOperacion.Fallo(error);

        var persona = new Persona
        {
            EstudioId = contexto.EstudioId!.Value,
            EsPersonaFisica = limpio.EsPersonaFisica,
            Apellido = limpio.Apellido,
            Nombre = limpio.Nombre,
            RazonSocial = limpio.RazonSocial,
            TipoDocumentoId = limpio.TipoDocumentoId,
            NroDocumento = limpio.NroDocumento,
            Cuit = limpio.Cuit,
            Telefono1 = limpio.Telefono1,
            Email = limpio.Email,
            Anulado = false,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Persona.Add(persona);
        await db.SaveChangesAsync(ct);

        return await VincularAsync(expedienteId, persona.Id, limpio.Rol, ct);
    }

    /// <summary>Vincula una persona que ya existe en el Estudio.</summary>
    public async Task<ResultadoOperacion> VincularExistenteAsync(long expedienteId, long personaId, byte rol,
        CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;
        if (!RolesPersonaExpediente.Valido(rol)) return ResultadoOperacion.Fallo("El rol no es válido.");
        if (!await db.Persona.AnyAsync(p => p.Id == personaId && !p.Anulado, ct))
            return ResultadoOperacion.Fallo("La persona no existe en este Estudio o está anulada.");

        return await VincularAsync(expedienteId, personaId, rol, ct);
    }

    private async Task<ResultadoOperacion> VincularAsync(long expedienteId, long personaId, byte rol,
        CancellationToken ct)
    {
        if (await db.ExpedientePersona.AnyAsync(ep => ep.ExpedienteId == expedienteId && ep.PersonaId == personaId, ct))
            return ResultadoOperacion.Fallo("Esa persona ya está en el expediente.");

        // La posición ordena dentro del rol: la nueva va al final.
        var ultima = await db.ExpedientePersona
            .Where(ep => ep.ExpedienteId == expedienteId && ep.Rol == rol)
            .MaxAsync(ep => (byte?)ep.Posicion, ct) ?? 0;
        if (ultima >= byte.MaxValue)
            return ResultadoOperacion.Fallo("El expediente ya tiene demasiadas personas con ese rol.");

        db.ExpedientePersona.Add(new ExpedientePersona
        {
            EstudioId = contexto.EstudioId!.Value,
            ExpedienteId = expedienteId,
            PersonaId = personaId,
            Rol = rol,
            Posicion = (byte)(ultima + 1),
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        });
        Auditar(expedienteId, 1, "ExpedientePersona", new
        {
            antes = (object?)null,
            despues = new { personaId, rol = RolesPersonaExpediente.Nombre(rol) }
        });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- modificación

    /// <summary>Edita los datos de la persona y su rol en el expediente.</summary>
    public async Task<ResultadoOperacion> EditarAsync(long expedienteId, long vinculoId, DatosPersona datos,
        byte[] versionPersona, CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var limpio = Normalizar(datos);
        if (Validar(limpio) is { } error) return ResultadoOperacion.Fallo(error);

        var vinculo = await db.ExpedientePersona.Include(ep => ep.Persona)
            .FirstOrDefaultAsync(ep => ep.Id == vinculoId && ep.ExpedienteId == expedienteId, ct);
        if (vinculo is null) return ResultadoOperacion.Fallo("La persona no está en este expediente.");
        if (!vinculo.Persona.Version.SequenceEqual(versionPersona))
            return ResultadoOperacion.Fallo("La persona cambió mientras la editabas. Recargá la ficha y volvé a intentar.");

        if (vinculo.Rol == RolesPersonaExpediente.Demandado && limpio.Rol != RolesPersonaExpediente.Demandado
            && !await OtroDemandadoAsync(expedienteId, vinculoId, ct))
            return ResultadoOperacion.Fallo("El expediente quedaría sin demandado. Cargá otro antes de cambiarle el rol.");

        var p = vinculo.Persona;
        var cambios = new Dictionary<string, object?[]>();
        Cambiar(cambios, "esPersonaFisica", p.EsPersonaFisica, limpio.EsPersonaFisica, v => p.EsPersonaFisica = v);
        Cambiar(cambios, "apellido", p.Apellido, limpio.Apellido, v => p.Apellido = v);
        Cambiar(cambios, "nombre", p.Nombre, limpio.Nombre, v => p.Nombre = v);
        Cambiar(cambios, "razonSocial", p.RazonSocial, limpio.RazonSocial, v => p.RazonSocial = v);
        Cambiar(cambios, "tipoDocumentoId", p.TipoDocumentoId, limpio.TipoDocumentoId, v => p.TipoDocumentoId = v);
        Cambiar(cambios, "nroDocumento", p.NroDocumento, limpio.NroDocumento, v => p.NroDocumento = v);
        Cambiar(cambios, "cuit", p.Cuit, limpio.Cuit, v => p.Cuit = v);
        Cambiar(cambios, "telefono1", p.Telefono1, limpio.Telefono1, v => p.Telefono1 = v);
        Cambiar(cambios, "email", p.Email, limpio.Email, v => p.Email = v);
        Cambiar(cambios, "rol", RolesPersonaExpediente.Nombre(vinculo.Rol),
            RolesPersonaExpediente.Nombre(limpio.Rol), _ => vinculo.Rol = limpio.Rol);

        if (cambios.Count == 0) return ResultadoOperacion.Ok();

        p.ModificadoUtc = DateTime.UtcNow;
        p.ModificadoPor = contexto.UsuarioId;
        vinculo.ModificadoUtc = DateTime.UtcNow;
        vinculo.ModificadoPor = contexto.UsuarioId;
        Auditar(expedienteId, 2, "ExpedientePersona",
            cambios.ToDictionary(c => c.Key, c => new { antes = c.Value[0], despues = c.Value[1] }));
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- baja

    /// <summary>
    /// Desvincula la persona del expediente. No la borra: puede estar en otros expedientes.
    /// No deja el expediente sin demandado.
    /// </summary>
    public async Task<ResultadoOperacion> QuitarAsync(long expedienteId, long vinculoId, CancellationToken ct = default)
    {
        if (await NoPuedeAsync(expedienteId, ct) is { } denegado) return denegado;

        var vinculo = await db.ExpedientePersona
            .FirstOrDefaultAsync(ep => ep.Id == vinculoId && ep.ExpedienteId == expedienteId, ct);
        if (vinculo is null) return ResultadoOperacion.Fallo("La persona no está en este expediente.");

        if (vinculo.Rol == RolesPersonaExpediente.Demandado
            && !await OtroDemandadoAsync(expedienteId, vinculoId, ct))
            return ResultadoOperacion.Fallo("No se puede quitar: el expediente quedaría sin demandado.");

        db.ExpedientePersona.Remove(vinculo);
        Auditar(expedienteId, 3, "ExpedientePersona", new
        {
            antes = new { personaId = vinculo.PersonaId, rol = RolesPersonaExpediente.Nombre(vinculo.Rol) },
            despues = (object?)null
        });
        await db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok();
    }

    // ----------------------------------------------------------------- internos

    private async Task<bool> OtroDemandadoAsync(long expedienteId, long exceptoVinculoId, CancellationToken ct) =>
        await db.ExpedientePersona.AnyAsync(ep => ep.ExpedienteId == expedienteId
            && ep.Rol == RolesPersonaExpediente.Demandado && ep.Id != exceptoVinculoId, ct);

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

    /// <summary>Normaliza según el tipo: una empresa no lleva apellido ni nombre, y una persona física no lleva razón social.</summary>
    private static DatosPersona Normalizar(DatosPersona d) => d with
    {
        Apellido = d.EsPersonaFisica ? Limpiar(d.Apellido, 150) : null,
        Nombre = d.EsPersonaFisica ? Limpiar(d.Nombre, 150) : null,
        RazonSocial = d.EsPersonaFisica ? null : Limpiar(d.RazonSocial, 250),
        NroDocumento = Limpiar(d.NroDocumento, 20),
        Cuit = Limpiar(d.Cuit, 13),
        Telefono1 = Limpiar(d.Telefono1, 50),
        Email = Limpiar(d.Email, 150),
        TipoDocumentoId = d.TipoDocumentoId is 0 ? null : d.TipoDocumentoId,
    };

    private static string? Validar(DatosPersona d)
    {
        if (!RolesPersonaExpediente.Valido(d.Rol)) return "El rol no es válido.";
        if (d.EsPersonaFisica && string.IsNullOrWhiteSpace(d.Apellido))
            return "El apellido es obligatorio para una persona física.";
        if (!d.EsPersonaFisica && string.IsNullOrWhiteSpace(d.RazonSocial))
            return "La razón social es obligatoria para una empresa.";
        if (d.NroDocumento is not null && d.TipoDocumentoId is null)
            return "Elegí el tipo de documento.";
        if (d.Email is not null && !d.Email.Contains('@'))
            return "El email no es válido.";
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

    private void Auditar(long expedienteId, byte accion, string entidad, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Expedientes,
            Entidad = entidad,
            EntidadId = expedienteId.ToString(),
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

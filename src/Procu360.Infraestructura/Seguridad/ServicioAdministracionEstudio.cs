using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Seguridad;

public sealed record ResultadoOperacion(bool Exito, string? Error)
{
    public static ResultadoOperacion Ok() => new(true, null);
    public static ResultadoOperacion Fallo(string error) => new(false, error);
}

public sealed record VistaRol(int Id, string Nombre, string? Descripcion, byte Ambito, bool EsSistema,
    IReadOnlyList<string> Permisos, int Miembros);

public sealed record VistaAsignacion(int AsignacionId, int RolId, string NombreRol, byte Ambito, int? ComitenteId);

public sealed record VistaMembresia(int MembresiaId, int UsuarioId, string Email, string NombreCompleto,
    byte Estado, IReadOnlyList<VistaAsignacion> Roles);

/// <summary>
/// Administración de usuarios del Estudio, roles y matriz de permisos.
/// Cada operación exige usuarios.administrar en el Estudio activo, no permite escalar privilegios
/// y no deja al Estudio sin administradores. Los cambios se auditan (aud.AuditoriaEvento).
/// </summary>
public sealed class ServicioAdministracionEstudio(
    Procu360DbContext db,
    ContextoEstudio contexto,
    ServicioPermisos permisos)
{
    private const byte AccionCambiarPermisos = 12;
    private const byte AccionCambiarEstado = 5;

    // ----------------------------------------------------------------- lectura

    public async Task<IReadOnlyList<VistaRol>> ListarRolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Rol.OrderBy(r => r.Nombre)
            .Select(r => new { r.Id, r.Nombre, r.Descripcion, r.Ambito, r.EsSistema })
            .ToListAsync(ct);
        var permisosPorRol = (await db.RolPermiso.Select(rp => new { rp.RolId, rp.Permiso.Codigo }).ToListAsync(ct))
            .GroupBy(x => x.RolId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Codigo).OrderBy(c => c).ToList());
        var miembrosPorRol = (await db.EstudioUsuarioRol
                .Join(db.EstudioUsuario.Where(m => m.Estado == EstadosMembresia.Activo),
                    eur => eur.EstudioUsuarioId, m => m.Id, (eur, m) => new { eur.RolId, m.Id })
                .Distinct().ToListAsync(ct))
            .GroupBy(x => x.RolId).ToDictionary(g => g.Key, g => g.Count());

        return roles.Select(r => new VistaRol(
            r.Id, r.Nombre, r.Descripcion, r.Ambito, r.EsSistema,
            permisosPorRol.GetValueOrDefault(r.Id) ?? [],
            miembrosPorRol.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<IReadOnlyList<VistaMembresia>> ListarMembresiasAsync(CancellationToken ct = default)
    {
        var membresias = await db.EstudioUsuario
            .Join(db.Usuario, m => m.UsuarioId, u => u.Id,
                (m, u) => new { m.Id, m.UsuarioId, u.Email, u.Nombre, u.Apellido, m.Estado })
            .OrderBy(x => x.Apellido).ThenBy(x => x.Nombre)
            .ToListAsync(ct);
        var asignaciones = await (from eur in db.EstudioUsuarioRol
                                  join r in db.Rol on eur.RolId equals r.Id
                                  select new { eur.Id, eur.EstudioUsuarioId, eur.RolId, r.Nombre, r.Ambito, eur.ComitenteId })
            .ToListAsync(ct);
        var porMembresia = asignaciones.GroupBy(a => a.EstudioUsuarioId).ToDictionary(g => g.Key);

        return membresias.Select(m => new VistaMembresia(
            m.Id, m.UsuarioId, m.Email, $"{m.Apellido}, {m.Nombre}", m.Estado,
            porMembresia.TryGetValue(m.Id, out var grupo)
                ? grupo.Select(a => new VistaAsignacion(a.Id, a.RolId, a.Nombre, a.Ambito, a.ComitenteId)).ToList()
                : [])).ToList();
    }

    // ----------------------------------------------------------------- roles

    public async Task<ResultadoOperacion> CrearRolAsync(
        string nombre, string? descripcion, byte ambito, IReadOnlyList<string> permisosSeleccionados,
        CancellationToken ct = default)
    {
        if (await SinPermisoAsync() is { } denegado) return denegado;

        var nombreLimpio = nombre?.Trim() ?? string.Empty;
        var errores = ReglasRol.ValidarRol(nombreLimpio, permisosSeleccionados);
        if (errores.Count > 0) return ResultadoOperacion.Fallo(string.Join(" ", errores));
        if (ambito is < 1 or > 3) return ResultadoOperacion.Fallo("Ámbito inválido.");
        if (!await PuedeAsignarPermisosAsync(permisosSeleccionados))
            return ResultadoOperacion.Fallo("No podés crear un rol con permisos que vos no tenés.");
        if (await db.Rol.AnyAsync(r => r.Nombre == nombreLimpio, ct))
            return ResultadoOperacion.Fallo("Ya existe un rol con ese nombre en este Estudio.");

        var rol = new Rol
        {
            EstudioId = contexto.EstudioId!.Value,
            Nombre = nombreLimpio,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            Ambito = ambito,
            EsSistema = false,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = contexto.UsuarioId
        };
        db.Rol.Add(rol);
        await db.SaveChangesAsync(ct);

        await ReemplazarPermisosAsync(rol.Id, permisosSeleccionados, ct);
        Auditar(AccionCambiarPermisos, "Rol", rol.Id.ToString(), new
        {
            antes = (object?)null,
            despues = new { nombre = nombreLimpio, ambito, permisos = permisosSeleccionados.OrderBy(p => p).ToList() }
        });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> ActualizarPermisosAsync(
        int rolId, IReadOnlyList<string> permisosSeleccionados, CancellationToken ct = default)
    {
        if (await SinPermisoAsync() is { } denegado) return denegado;

        var rol = await db.Rol.FirstOrDefaultAsync(r => r.Id == rolId, ct);
        if (rol is null) return ResultadoOperacion.Fallo("El rol no existe en este Estudio.");

        var errores = ReglasRol.ValidarRol(rol.Nombre, permisosSeleccionados);
        if (errores.Count > 0) return ResultadoOperacion.Fallo(string.Join(" ", errores));
        if (!await PuedeAsignarPermisosAsync(permisosSeleccionados))
            return ResultadoOperacion.Fallo("No podés asignar permisos que vos no tenés.");

        var antes = await PermisosDeRolAsync(rolId, ct);
        var quitaAdministrar = antes.Contains(Permisos.UsuariosAdministrar)
            && !permisosSeleccionados.Contains(Permisos.UsuariosAdministrar);
        if (quitaAdministrar && await AdministradoresAsync(excluirRolId: rolId, ct: ct) == 0)
            return ResultadoOperacion.Fallo("No se puede quitar el permiso de administrar: el Estudio quedaría sin administradores.");

        await ReemplazarPermisosAsync(rolId, permisosSeleccionados, ct);
        Auditar(AccionCambiarPermisos, "Rol", rolId.ToString(), new
        {
            antes = antes.OrderBy(p => p).ToList(),
            despues = permisosSeleccionados.OrderBy(p => p).ToList()
        });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    // --------------------------------------------------------- asignaciones

    public async Task<ResultadoOperacion> AsignarRolAsync(
        int membresiaId, int rolId, int? comitenteId, CancellationToken ct = default)
    {
        if (await SinPermisoAsync() is { } denegado) return denegado;

        var membresia = await db.EstudioUsuario.FirstOrDefaultAsync(m => m.Id == membresiaId, ct);
        if (membresia is null || membresia.Estado != EstadosMembresia.Activo)
            return ResultadoOperacion.Fallo("Solo se pueden asignar roles a integrantes activos.");

        var rol = await db.Rol.FirstOrDefaultAsync(r => r.Id == rolId, ct);
        if (rol is null) return ResultadoOperacion.Fallo("El rol no existe en este Estudio.");

        if (rol.Ambito == (byte)Ambito.Comitente)
        {
            if (comitenteId is null) return ResultadoOperacion.Fallo("Este rol necesita un comitente.");
            if (!await db.Comitente.AnyAsync(c => c.Id == comitenteId.Value && c.Activo, ct))
                return ResultadoOperacion.Fallo("El comitente no existe o está inactivo.");
        }
        else if (comitenteId is not null)
        {
            return ResultadoOperacion.Fallo("Este rol no depende de un comitente.");
        }

        var permisosRol = await PermisosDeRolAsync(rolId, ct);
        if (!ReglasRol.PuedeAsignarRol(await permisos.EfectivosAsync(ct), permisosRol))
            return ResultadoOperacion.Fallo("No podés asignar un rol con permisos que vos no tenés.");

        if (await db.EstudioUsuarioRol.AnyAsync(eur => eur.EstudioUsuarioId == membresiaId
                && eur.RolId == rolId && eur.ComitenteId == comitenteId, ct))
            return ResultadoOperacion.Fallo("La persona ya tiene ese rol.");

        var asignacion = new EstudioUsuarioRol
        {
            EstudioId = contexto.EstudioId!.Value,
            EstudioUsuarioId = membresiaId,
            RolId = rolId,
            ComitenteId = comitenteId
        };
        db.EstudioUsuarioRol.Add(asignacion);
        Auditar(AccionCambiarPermisos, "EstudioUsuarioRol", membresiaId.ToString(), new
        {
            antes = (object?)null,
            despues = new { rol = rol.Nombre, comitente = comitenteId }
        });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> QuitarRolAsync(int asignacionId, CancellationToken ct = default)
    {
        if (await SinPermisoAsync() is { } denegado) return denegado;

        var asignacion = await db.EstudioUsuarioRol.Include(a => a.Rol)
            .FirstOrDefaultAsync(a => a.Id == asignacionId, ct);
        if (asignacion is null) return ResultadoOperacion.Fallo("La asignación no existe.");

        var permisosRol = await PermisosDeRolAsync(asignacion.RolId, ct);
        if (permisosRol.Contains(Permisos.UsuariosAdministrar)
            && await AdministradoresAsync(excluirAsignacionId: asignacionId, ct: ct) == 0)
            return ResultadoOperacion.Fallo("No se puede quitar: el Estudio quedaría sin administradores.");

        db.EstudioUsuarioRol.Remove(asignacion);
        Auditar(AccionCambiarPermisos, "EstudioUsuarioRol", asignacion.EstudioUsuarioId.ToString(), new
        {
            antes = new { rol = asignacion.Rol.Nombre, comitente = asignacion.ComitenteId },
            despues = (object?)null
        });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    // ---------------------------------------------------------- membresías

    public async Task<ResultadoOperacion> DesactivarMembresiaAsync(int membresiaId, CancellationToken ct = default)
    {
        if (await SinPermisoAsync() is { } denegado) return denegado;

        var membresia = await db.EstudioUsuario.FirstOrDefaultAsync(m => m.Id == membresiaId, ct);
        if (membresia is null) return ResultadoOperacion.Fallo("La membresía no existe en este Estudio.");
        if (membresia.Estado == EstadosMembresia.Desactivado) return ResultadoOperacion.Ok();

        if (membresia.Estado == EstadosMembresia.Activo
            && await EsAdministradorAsync(membresiaId, ct)
            && await AdministradoresAsync(excluirMembresiaId: membresiaId, ct: ct) == 0)
            return ResultadoOperacion.Fallo("No se puede desactivar: el Estudio quedaría sin administradores.");

        var estadoAnterior = membresia.Estado;
        membresia.Estado = EstadosMembresia.Desactivado;
        membresia.ModificadoUtc = DateTime.UtcNow;
        membresia.ModificadoPor = contexto.UsuarioId;
        Auditar(AccionCambiarEstado, "EstudioUsuario", membresiaId.ToString(), new
        {
            antes = estadoAnterior,
            despues = EstadosMembresia.Desactivado
        });
        await db.SaveChangesAsync(ct);
        permisos.Invalidar();
        return ResultadoOperacion.Ok();
    }

    // ------------------------------------------------------------- internos

    private async Task<ResultadoOperacion?> SinPermisoAsync() =>
        await permisos.TieneAsync(Permisos.UsuariosAdministrar)
            ? null
            : ResultadoOperacion.Fallo("No tenés permiso para administrar usuarios y roles.");

    private async Task<bool> PuedeAsignarPermisosAsync(IEnumerable<string> permisosSolicitados) =>
        ReglasRol.PuedeAsignarRol(await permisos.EfectivosAsync(), permisosSolicitados.ToList());

    private async Task<HashSet<string>> PermisosDeRolAsync(int rolId, CancellationToken ct) =>
        (await db.RolPermiso.Where(rp => rp.RolId == rolId).Select(rp => rp.Permiso.Codigo).ToListAsync(ct))
        .ToHashSet(StringComparer.Ordinal);

    private async Task ReemplazarPermisosAsync(int rolId, IEnumerable<string> codigos, CancellationToken ct)
    {
        await db.RolPermiso.Where(rp => rp.RolId == rolId).ExecuteDeleteAsync(ct);
        var ids = await db.Permiso.Where(p => codigos.Contains(p.Codigo)).Select(p => p.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            db.RolPermiso.Add(new RolPermiso { EstudioId = contexto.EstudioId!.Value, RolId = rolId, PermisoId = id });
        }
    }

    private async Task<bool> EsAdministradorAsync(int membresiaId, CancellationToken ct)
    {
        var adminPermiso = await db.Permiso.Where(p => p.Codigo == Permisos.UsuariosAdministrar).Select(p => p.Id).FirstAsync(ct);
        var rolesAdmin = db.RolPermiso.Where(rp => rp.PermisoId == adminPermiso).Select(rp => rp.RolId);
        return await db.EstudioUsuarioRol.AnyAsync(eur =>
            eur.EstudioUsuarioId == membresiaId && rolesAdmin.Contains(eur.RolId), ct);
    }

    /// <summary>
    /// Cuenta membresías activas con un rol que otorga usuarios.administrar, excluyendo lo que se está quitando.
    /// </summary>
    private async Task<int> AdministradoresAsync(
        int? excluirAsignacionId = null, int? excluirRolId = null, int? excluirMembresiaId = null,
        CancellationToken ct = default)
    {
        var adminPermiso = await db.Permiso.Where(p => p.Codigo == Permisos.UsuariosAdministrar).Select(p => p.Id).FirstAsync(ct);
        var rolesAdmin = db.RolPermiso.Where(rp => rp.PermisoId == adminPermiso).Select(rp => rp.RolId);

        return await (from eur in db.EstudioUsuarioRol
                      join m in db.EstudioUsuario.Where(x => x.Estado == EstadosMembresia.Activo) on eur.EstudioUsuarioId equals m.Id
                      where rolesAdmin.Contains(eur.RolId)
                            && (excluirAsignacionId == null || eur.Id != excluirAsignacionId)
                            && (excluirRolId == null || eur.RolId != excluirRolId)
                            && (excluirMembresiaId == null || eur.EstudioUsuarioId != excluirMembresiaId)
                      select m.Id).Distinct().CountAsync(ct);
    }

    private void Auditar(byte accion, string entidad, string entidadId, object cambios)
    {
        db.AuditoriaEvento.Add(new AuditoriaEvento
        {
            EstudioId = contexto.EstudioId,
            UsuarioId = contexto.UsuarioId,
            FechaUtc = DateTime.UtcNow,
            Modulo = Modulos.Usuarios,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Resultado = 1,
            CambiosJson = JsonSerializer.Serialize(cambios)
        });
    }
}

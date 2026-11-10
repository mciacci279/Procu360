using Microsoft.EntityFrameworkCore;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Seguridad;

/// <summary>
/// Permisos y alcance del usuario en el Estudio activo, calculados desde seg.EstudioUsuarioRol
/// y seg.RolPermiso. Se cachea por unidad de trabajo; tras un cambio de roles, usar Invalidar().
/// </summary>
public sealed class ServicioPermisos(Procu360DbContext db, ContextoEstudio contexto)
{
    private PermisosEfectivos? _cache;

    public async Task<PermisosEfectivos> EfectivosAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;

        var usuario = contexto.UsuarioId;
        var estudio = contexto.EstudioId;
        if (usuario is null || estudio is null)
        {
            return _cache = CalculoPermisos.Calcular([]);
        }

        var asignaciones = await (
            from eur in db.EstudioUsuarioRol
            join em in db.EstudioUsuario on eur.EstudioUsuarioId equals em.Id
            where em.UsuarioId == usuario.Value && em.Estado == EstadosMembresia.Activo
            select new AsignacionRol(eur.RolId, eur.Rol.Nombre, eur.Rol.Ambito, eur.ComitenteId))
            .ToListAsync(ct);

        var rolIds = asignaciones.Select(a => a.RolId).Distinct().ToList();
        var permisosPorRol = (await db.RolPermiso
                .Where(rp => rolIds.Contains(rp.RolId))
                .Select(rp => new { rp.RolId, rp.Permiso.Codigo })
                .ToListAsync(ct))
            .GroupBy(x => x.RolId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Codigo).ToList());

        var roles = asignaciones
            .Select(a => new RolAsignado(
                a.Nombre,
                (Ambito)a.Ambito,
                permisosPorRol.GetValueOrDefault(a.RolId) ?? [],
                a.ComitenteId))
            .ToList();

        return _cache = CalculoPermisos.Calcular(roles);
    }

    public async Task<bool> TieneAsync(string permiso, CancellationToken ct = default) =>
        (await EfectivosAsync(ct)).Tiene(permiso);

    public void Invalidar() => _cache = null;

    private sealed record AsignacionRol(int RolId, string Nombre, byte Ambito, int? ComitenteId);
}

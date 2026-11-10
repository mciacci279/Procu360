using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;

namespace Procu360.Infraestructura.Identidad;

public sealed record MembresiaActiva(int EstudioId, string NombreEstudio);

/// <summary>Datos de la sesión que muestra el pie de la barra lateral.</summary>
public sealed record PerfilSesion(string NombreCompleto, string NombreEstudio, IReadOnlyList<string> Roles);

/// <summary>
/// Membresías del usuario para el selector de Estudio y validación de la membresía de la sesión.
/// </summary>
public sealed class ServicioSesion
{
    private readonly Procu360DbContext _db;

    public ServicioSesion(Procu360DbContext db) => _db = db;

    /// <summary>
    /// Usar con contexto de solo usuario (antes de elegir Estudio). El RLS deja ver las membresías propias.
    /// </summary>
    public async Task<IReadOnlyList<MembresiaActiva>> MembresiasActivasAsync(int usuarioId, CancellationToken ct = default)
    {
        return await (from m in _db.EstudioUsuario.IgnoreQueryFilters()
                      join e in _db.Estudio on m.EstudioId equals e.Id
                      where m.UsuarioId == usuarioId && m.Estado == EstadosMembresia.Activo
                      orderby e.Nombre
                      select new MembresiaActiva(e.Id, e.Nombre))
            .ToListAsync(ct);
    }

    /// <summary>Usar con contexto de Estudio + usuario.</summary>
    public async Task<PerfilSesion?> PerfilAsync(int usuarioId, int estudioId, CancellationToken ct = default)
    {
        var persona = await _db.Usuario.Where(u => u.Id == usuarioId)
            .Select(u => new { u.Nombre, u.Apellido }).FirstOrDefaultAsync(ct);
        if (persona is null) return null;

        var nombreEstudio = await _db.Estudio.Where(e => e.Id == estudioId)
            .Select(e => e.Nombre).FirstOrDefaultAsync(ct) ?? string.Empty;

        var roles = await (from m in _db.EstudioUsuario
                           join eur in _db.EstudioUsuarioRol on m.Id equals eur.EstudioUsuarioId
                           join r in _db.Rol on eur.RolId equals r.Id
                           where m.UsuarioId == usuarioId && m.EstudioId == estudioId
                           select r.Nombre).Distinct().OrderBy(n => n).ToListAsync(ct);

        return new PerfilSesion($"{persona.Apellido}, {persona.Nombre}", nombreEstudio, roles);
    }

    /// <summary>
    /// Usar con contexto de Estudio + usuario: confirma que la membresía sigue activa
    /// (si la desactivaron, la sesión deja de servir en la siguiente request).
    /// </summary>
    public Task<bool> MembresiaActivaAsync(int usuarioId, int estudioId, CancellationToken ct = default) =>
        _db.EstudioUsuario.AnyAsync(
            m => m.UsuarioId == usuarioId && m.EstudioId == estudioId && m.Estado == EstadosMembresia.Activo,
            ct);
}

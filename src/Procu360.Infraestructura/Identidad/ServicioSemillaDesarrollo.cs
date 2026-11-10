using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Infraestructura.Identidad;

/// <summary>
/// Solo desarrollo: asigna una contraseña común a los usuarios de seed que todavía no tienen.
/// La contraseña viene de user-secrets (Seed:PasswordDesarrollo), nunca del código.
/// </summary>
public sealed class ServicioSemillaDesarrollo
{
    private readonly Procu360DbContext _db;
    private readonly UserManager<Usuario> _usuarios;

    public ServicioSemillaDesarrollo(Procu360DbContext db, UserManager<Usuario> usuarios)
    {
        _db = db;
        _usuarios = usuarios;
    }

    public async Task<int> AsignarContrasenaAsync(string contrasena, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("Falta la contraseña de desarrollo (Seed:PasswordDesarrollo).");

        var sinClave = await _db.Usuario.Where(u => u.PasswordHash == null && u.Activo).ToListAsync(ct);
        var asignadas = 0;
        foreach (var usuario in sinClave)
        {
            var resultado = await _usuarios.AddPasswordAsync(usuario, contrasena);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    $"La contraseña de desarrollo no cumple la política: {string.Join(" ", resultado.Errors.Select(e => e.Description))}");
            asignadas++;
        }
        return asignadas;
    }
}

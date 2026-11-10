using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Infraestructura.Identidad;

/// <summary>
/// Store de ASP.NET Core Identity sobre seg.Usuario (sin cambios de esquema).
/// El nombre de usuario es el email; EmailNormalizado es el nombre normalizado.
/// Los usuarios no se borran: se desactivan (Activo = 0).
/// </summary>
public sealed class UsuarioStore :
    IUserStore<Usuario>,
    IUserEmailStore<Usuario>,
    IUserPasswordStore<Usuario>,
    IUserSecurityStampStore<Usuario>,
    IUserLockoutStore<Usuario>
{
    private readonly Procu360DbContext _db;

    public UsuarioStore(Procu360DbContext db) => _db = db;

    public Task<string> GetUserIdAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task SetUserNameAsync(Usuario user, string? userName, CancellationToken cancellationToken)
    {
        user.Email = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.EmailNormalizado);

    public Task SetNormalizedUserNameAsync(Usuario user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.EmailNormalizado = normalizedName ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(Usuario user, CancellationToken cancellationToken)
    {
        _db.Usuario.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(Usuario user, CancellationToken cancellationToken)
    {
        try
        {
            user.ModificadoUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(DbUpdateConcurrencyException),
                Description = "El usuario fue modificado por otra operación. Intentá de nuevo."
            });
        }
    }

    public Task<IdentityResult> DeleteAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "NoBorrable",
            Description = "Los usuarios no se borran: se desactivan."
        }));

    public Task<Usuario?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var id))
        {
            return Task.FromResult<Usuario?>(null);
        }
        return _db.Usuario.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)!;
    }

    public Task<Usuario?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        _db.Usuario.FirstOrDefaultAsync(u => u.EmailNormalizado == normalizedUserName, cancellationToken)!;

    public Task<Usuario?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        _db.Usuario.FirstOrDefaultAsync(u => u.EmailNormalizado == normalizedEmail, cancellationToken)!;

    public Task SetEmailAsync(Usuario user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    // Sin confirmación de email en esta etapa: la activación la controla Activo (ver UsuarioActivoConfirmacion).
    public Task<bool> GetEmailConfirmedAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task SetEmailConfirmedAsync(Usuario user, bool confirmed, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<string?> GetNormalizedEmailAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.EmailNormalizado);

    public Task SetNormalizedEmailAsync(Usuario user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.EmailNormalizado = normalizedEmail ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(Usuario user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetSecurityStampAsync(Usuario user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(user.BloqueadoHastaUtc);

    public Task SetLockoutEndDateAsync(Usuario user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.BloqueadoHastaUtc = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(Usuario user, CancellationToken cancellationToken)
    {
        user.AccesosFallidos++;
        return Task.FromResult(user.AccesosFallidos);
    }

    public Task ResetAccessFailedCountAsync(Usuario user, CancellationToken cancellationToken)
    {
        user.AccesosFallidos = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccesosFallidos);

    public Task<bool> GetLockoutEnabledAsync(Usuario user, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task SetLockoutEnabledAsync(Usuario user, bool enabled, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public void Dispose() { }
}

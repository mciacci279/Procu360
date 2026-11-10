using Microsoft.AspNetCore.Identity;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Infraestructura.Identidad;

/// <summary>
/// Un usuario desactivado no puede iniciar sesión (SignInManager devuelve NotAllowed).
/// </summary>
public sealed class UsuarioActivoConfirmacion : IUserConfirmation<Usuario>
{
    public Task<bool> IsConfirmedAsync(UserManager<Usuario> manager, Usuario user) =>
        Task.FromResult(user.Activo);
}

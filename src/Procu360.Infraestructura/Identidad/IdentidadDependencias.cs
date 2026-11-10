using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Infraestructura.Identidad;

public static class IdentidadDependencias
{
    /// <summary>Identidad de Procu360 sobre seg.Usuario: contraseñas, bloqueo, invitaciones y sesión.</summary>
    public static IServiceCollection AgregarIdentidadProcu360(this IServiceCollection services)
    {
        // Se registran antes de AddIdentityCore: Identity usa TryAdd y así prevalecen estas implementaciones.
        services.AddScoped<IUserStore<Usuario>, UsuarioStore>();
        services.AddScoped<IUserConfirmation<Usuario>, UsuarioActivoConfirmacion>();
        services.AddScoped<ServicioInvitaciones>();
        services.AddScoped<ServicioSesion>();
        services.AddScoped<ServicioSemillaDesarrollo>();

        services.Configure<DataProtectionTokenProviderOptions>(opciones =>
            opciones.TokenLifespan = TimeSpan.FromDays(7));

        services.AddIdentityCore<Usuario>(opciones =>
            {
                opciones.User.RequireUniqueEmail = true;
                // Los emails pueden tener acentos y otros caracteres: la validación de formato la hace el alta.
                opciones.User.AllowedUserNameCharacters = string.Empty;

                opciones.Password.RequiredLength = 10;
                opciones.Password.RequireDigit = true;
                opciones.Password.RequireLowercase = true;
                opciones.Password.RequireUppercase = true;
                opciones.Password.RequireNonAlphanumeric = false;

                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                opciones.SignIn.RequireConfirmedAccount = true;   // NotAllowed si Activo = 0
            })
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}

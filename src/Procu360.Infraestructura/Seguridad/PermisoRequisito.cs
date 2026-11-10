using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Dominio.Seguridad;

namespace Procu360.Infraestructura.Seguridad;

/// <summary>Requisito de autorización: el usuario debe tener el permiso en el Estudio activo.</summary>
public sealed record PermisoRequisito(string Permiso) : IAuthorizationRequirement;

public sealed class PermisoHandler(ServicioPermisos permisos) : AuthorizationHandler<PermisoRequisito>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequisito requirement)
    {
        if (await permisos.TieneAsync(requirement.Permiso))
        {
            context.Succeed(requirement);
        }
    }
}

public static class PermisosDependencias
{
    /// <summary>
    /// Permisos, administración de Estudio y una política por cada permiso del catálogo
    /// ([Authorize(Policy = "usuarios.crear")]).
    /// </summary>
    public static IServiceCollection AgregarSeguridadProcu360(this IServiceCollection services)
    {
        services.AddScoped<ServicioPermisos>();
        services.AddScoped<ServicioAdministracionEstudio>();
        services.AddScoped<IAuthorizationHandler, PermisoHandler>();
        services.AddAuthorization(opciones =>
        {
            foreach (var permiso in Permisos.Catalogo())
            {
                opciones.AddPolicy(permiso, politica => politica.Requirements.Add(new PermisoRequisito(permiso)));
            }
        });
        return services;
    }
}

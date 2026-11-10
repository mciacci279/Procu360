using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura;

public static class InfraestructuraDependencias
{
    /// <summary>
    /// Registra el DbContext de Procu360 con el contexto de Estudio y el interceptor de SESSION_CONTEXT.
    /// La cadena de conexión la aporta el host (user-secrets en desarrollo).
    /// </summary>
    public static IServiceCollection AgregarPersistenciaProcu360(
        this IServiceCollection services,
        string cadenaConexion)
    {
        services.AddScoped<ContextoEstudio>();
        services.AddScoped<Procu360.Infraestructura.Expedientes.AlcanceExpedientes>();
        services.AddScoped<Procu360.Infraestructura.MiDia.ServicioMiDia>();
        services.AddScoped<Procu360.Infraestructura.Expedientes.ServicioBusquedaExpedientes>();
        services.AddScoped<Procu360.Infraestructura.Expedientes.FichaExpedienteServicio>();
        services.AddScoped<Procu360.Infraestructura.Novedades.ServicioNovedades>();
        services.AddScoped<Procu360.Infraestructura.Tareas.ServicioTareas>();
        services.AddScoped<Procu360.Infraestructura.Etapas.ServicioEtapas>();
        services.AddScoped<SesionEstudioInterceptor>();
        services.AddDbContext<Procu360DbContext>((sp, opciones) => opciones
            .UseSqlServer(cadenaConexion)
            .AddInterceptors(sp.GetRequiredService<SesionEstudioInterceptor>()));
        return services;
    }
}

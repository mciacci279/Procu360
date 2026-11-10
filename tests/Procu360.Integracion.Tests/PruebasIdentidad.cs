using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>Servicios de identidad y persistencia sobre Procu360_Test, con el login de la aplicación.</summary>
internal static class PruebasIdentidad
{
    public const string ClaveValida = "Clave-Prueba-2026";
    private const string PrefijoEmail = "prueba-";

    public static ServiceProvider CrearServicios(TimeSpan? vidaToken = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AgregarPersistenciaProcu360(ConfiguracionPruebas.CadenaAplicacionTest());
        services.AgregarIdentidadProcu360();
        services.AgregarSeguridadProcu360();
        if (vidaToken is { } vida)
        {
            services.Configure<Microsoft.AspNetCore.Identity.DataProtectionTokenProviderOptions>(o => o.TokenLifespan = vida);
        }
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static string NuevoEmail() => $"{PrefijoEmail}{Guid.NewGuid():N}@procu360.test";

    /// <summary>Crea un usuario activo con contraseña válida (contexto solo de usuario no hace falta: seg.Usuario no tiene RLS).</summary>
    public static async Task<int> CrearUsuarioConClaveAsync(IServiceProvider sp, string email, bool activo = true)
    {
        using var ambito = sp.CreateScope();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = new Usuario
        {
            Email = email,
            EmailNormalizado = usuarios.NormalizeEmail(email),
            Nombre = "Prueba",
            Apellido = "Usuario",
            Activo = activo,
            CreadoUtc = DateTime.UtcNow
        };
        var creado = await usuarios.CreateAsync(usuario);
        if (!creado.Succeeded) throw new InvalidOperationException(string.Join(" ", creado.Errors.Select(e => e.Description)));
        var clave = await usuarios.AddPasswordAsync(usuario, ClaveValida);
        if (!clave.Succeeded) throw new InvalidOperationException(string.Join(" ", clave.Errors.Select(e => e.Description)));
        return usuario.Id;
    }

    /// <summary>Inserta una membresía con el contexto del propio Estudio (el RLS exige ese contexto para escribir).</summary>
    public static async Task InsertarMembresiaAsync(IServiceProvider sp, int estudioId, int usuarioId, byte estado)
    {
        using var ambito = sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudioId, usuarioId);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        db.EstudioUsuario.Add(new EstudioUsuario
        {
            EstudioId = estudioId,
            UsuarioId = usuarioId,
            Estado = estado,
            CreadoUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Borra los usuarios de prueba y sus membresías en Procu360_Test. Usa la clave Procu360 (administración)
    /// solo para esta limpieza, con el contexto de cada Estudio (el RLS aplica también a esa conexión).
    /// </summary>
    public static void LimpiarUsuariosDePrueba()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets(typeof(PruebasIdentidad).Assembly)
            .Build();
        var builder = new SqlConnectionStringBuilder(configuracion["ConnectionStrings:Procu360"]
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Procu360 (ver user-secrets)."))
        {
            InitialCatalog = "Procu360_Test",
            Pooling = false
        };

        foreach (var estudio in new[] { 1, 2 })
        {
            using var cn = new SqlConnection(builder.ConnectionString);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"""
                EXEC sys.sp_set_session_context @key = N'EstudioId', @value = {estudio}, @read_only = 1;
                DELETE eur FROM seg.EstudioUsuarioRol eur
                  JOIN seg.EstudioUsuario m ON m.Id = eur.EstudioUsuarioId
                  JOIN seg.Usuario u ON u.Id = m.UsuarioId
                 WHERE u.EmailNormalizado LIKE N'PRUEBA-%';
                DELETE eur FROM seg.EstudioUsuarioRol eur
                  JOIN seg.Rol r ON r.Id = eur.RolId
                 WHERE r.Nombre LIKE N'PRUEBA-%';
                DELETE rp FROM seg.RolPermiso rp
                  JOIN seg.Rol r ON r.Id = rp.RolId
                 WHERE r.Nombre LIKE N'PRUEBA-%';
                DELETE FROM seg.Rol WHERE Nombre LIKE N'PRUEBA-%';
                UPDATE dbo.NovedadJudicial SET TareaId = NULL
                 WHERE TareaId IN (SELECT Id FROM dbo.Tarea WHERE Titulo LIKE N'PRUEBA-%');
                DELETE FROM dbo.Tarea WHERE Titulo LIKE N'PRUEBA-%';
                DELETE FROM dbo.NovedadJudicial WHERE Texto LIKE N'PRUEBA-%';
                -- Solo en Procu360_Test: la auditoría de los usuarios de prueba referencia sus Id.
                -- La aplicación no puede borrar auditoría (DENY DELETE); esta limpieza usa la clave de administración.
                DELETE e FROM aud.AuditoriaEvento e
                  JOIN seg.Usuario u ON u.Id = e.UsuarioId
                 WHERE u.EmailNormalizado LIKE N'PRUEBA-%';
                DELETE m FROM seg.EstudioUsuario m
                  JOIN seg.Usuario u ON u.Id = m.UsuarioId
                 WHERE u.EmailNormalizado LIKE N'PRUEBA-%';
                """;
            cmd.ExecuteNonQuery();
        }

        using (var cn = new SqlConnection(builder.ConnectionString))
        {
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM seg.Usuario WHERE EmailNormalizado LIKE N'PRUEBA-%';";
            cmd.ExecuteNonQuery();
        }
    }
}

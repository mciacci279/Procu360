using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Procu360.Aislamiento.Tests;

/// <summary>Cadena del login de la aplicación sobre Procu360_Test (user-secrets).</summary>
internal static class ConfiguracionPruebas
{
    public static string CadenaAplicacionTest()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ConfiguracionPruebas).Assembly)
            .Build();

        return configuracion["ConnectionStrings:ProcuAplicacionTest"]
            ?? throw new InvalidOperationException(
                "Falta el secreto ConnectionStrings:ProcuAplicacionTest (ver user-secrets).");
    }

    /// <summary>
    /// Solo para leer metadatos de seguridad (sys.security_predicates), que el login de la aplicación
    /// no puede ver porque no tiene VIEW DEFINITION. Usa la clave Procu360 de user-secrets sobre Procu360_Test.
    /// Las pruebas de datos no la usan.
    /// </summary>
    public static string CadenaMetadatosTest()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ConfiguracionPruebas).Assembly)
            .Build();

        var cadena = configuracion["ConnectionStrings:Procu360"]
            ?? throw new InvalidOperationException("Falta el secreto ConnectionStrings:Procu360 (ver user-secrets).");

        var builder = new SqlConnectionStringBuilder(cadena) { InitialCatalog = "Procu360_Test", Pooling = false };
        return builder.ConnectionString;
    }

    /// <summary>Sin pool: cada conexión de la prueba arranca con SESSION_CONTEXT limpio.</summary>
    public static string CadenaSinPool()
    {
        var builder = new SqlConnectionStringBuilder(CadenaAplicacionTest()) { Pooling = false };
        return builder.ConnectionString;
    }
}

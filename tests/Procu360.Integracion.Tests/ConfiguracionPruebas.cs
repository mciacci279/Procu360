using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Procu360.Integracion.Tests;

/// <summary>
/// Cadena de conexión de pruebas: login de la aplicación sobre Procu360_Test (user-secrets).
/// Max Pool Size=1 fuerza que las aperturas reutilicen la misma conexión física del pool.
/// </summary>
internal static class ConfiguracionPruebas
{
    public static string CadenaAplicacionTest()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ConfiguracionPruebas).Assembly)
            .Build();

        var cadena = configuracion["ConnectionStrings:ProcuAplicacionTest"]
            ?? throw new InvalidOperationException(
                "Falta el secreto ConnectionStrings:ProcuAplicacionTest (ver user-secrets).");

        var builder = new SqlConnectionStringBuilder(cadena) { MaxPoolSize = 1 };
        return builder.ConnectionString;
    }
}

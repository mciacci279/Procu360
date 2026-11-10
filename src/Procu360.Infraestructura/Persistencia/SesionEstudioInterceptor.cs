using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Persistencia;

/// <summary>
/// Fija EstudioId y UsuarioId en SESSION_CONTEXT cada vez que se abre una conexión.
/// Las claves son de solo lectura: SQL Server las limpia al reutilizar la conexión del pool
/// (sp_reset_connection), así que cada apertura empieza con el contexto de su propia unidad de trabajo.
/// </summary>
public sealed class SesionEstudioInterceptor : DbConnectionInterceptor
{
    private const string Sql =
        "EXEC sys.sp_set_session_context @key = N'EstudioId', @value = @estudio, @read_only = 1; " +
        "EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = @usuario, @read_only = 1;";

    private readonly ContextoEstudio _contexto;

    public SesionEstudioInterceptor(ContextoEstudio contexto) => _contexto = contexto;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var comando = CrearComando(connection);
        comando.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var comando = CrearComando(connection);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CrearComando(DbConnection connection)
    {
        var comando = connection.CreateCommand();
        comando.CommandText = Sql;
        AgregarParametro(comando, "@estudio", _contexto.EstudioId);
        AgregarParametro(comando, "@usuario", _contexto.UsuarioId);
        return comando;
    }

    private static void AgregarParametro(DbCommand comando, string nombre, int? valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = (object?)valor ?? DBNull.Value;
        comando.Parameters.Add(parametro);
    }
}

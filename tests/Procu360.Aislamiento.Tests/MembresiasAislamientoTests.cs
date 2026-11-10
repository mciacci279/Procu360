using Microsoft.Data.SqlClient;

namespace Procu360.Aislamiento.Tests;

/// <summary>
/// Membresías (seg.EstudioUsuario): cada persona ve las suyas, cada Estudio ve las propias,
/// y ninguna escritura cruza de Estudio. Usa datos de seed: Laura (UsuarioId 2) es miembro de Estudio 1.
/// Las escrituras se revierten.
/// </summary>
public class MembresiasAislamientoTests
{
    private const int UsuarioLaura = 2;
    private const int ErrorPredicadoBloqueo = 33504;

    private static SqlConnection Abrir(string contexto, int valor)
    {
        var cn = new SqlConnection(ConfiguracionPruebas.CadenaSinPool());
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"EXEC sys.sp_set_session_context @key = N'{contexto}', @value = {valor}, @read_only = 1;";
        cmd.ExecuteNonQuery();
        return cn;
    }

    private static long Contar(SqlConnection cn, string sql)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    [Fact]
    public void Un_usuario_sin_Estudio_ve_solo_sus_propias_membresias()
    {
        using var cn = Abrir("UsuarioId", UsuarioLaura);

        var ajenas = Contar(cn, $"SELECT COUNT_BIG(*) FROM seg.EstudioUsuario WHERE UsuarioId <> {UsuarioLaura};");
        var propias = Contar(cn, $"SELECT COUNT_BIG(*) FROM seg.EstudioUsuario WHERE UsuarioId = {UsuarioLaura};");

        Assert.Equal(0, ajenas);
        Assert.True(propias > 0, "La prueba es vacía: Laura debería tener membresías en el seed.");
    }

    [Fact]
    public void Un_Estudio_no_ve_membresias_de_otro_Estudio()
    {
        using var cn = Abrir("EstudioId", 1);

        var delOtro = Contar(cn, "SELECT COUNT_BIG(*) FROM seg.EstudioUsuario WHERE EstudioId = 2;");

        Assert.Equal(0, delOtro);
    }

    [Fact]
    public void Un_usuario_sin_Estudio_no_puede_modificar_su_membresia()
    {
        using var cn = Abrir("UsuarioId", UsuarioLaura);
        using var tx = cn.BeginTransaction();
        try
        {
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"UPDATE seg.EstudioUsuario SET Estado = Estado WHERE UsuarioId = {UsuarioLaura};";

            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());
            Assert.Equal(ErrorPredicadoBloqueo, ex.Number);
        }
        finally
        {
            tx.Rollback();
        }
    }
}

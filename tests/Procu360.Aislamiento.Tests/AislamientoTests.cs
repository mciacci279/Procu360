using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Aislamiento.Tests;

/// <summary>
/// Un Estudio nunca ve ni modifica datos de otro. Las escrituras se hacen dentro de una
/// transacción que siempre se revierte: la base de pruebas queda como estaba.
/// </summary>
public class AislamientoTests
{
    // Código de error de SQL Server cuando un predicado de bloqueo del RLS rechaza la operación.
    private const int ErrorPredicadoBloqueo = 33504;

    private static Procu360DbContext CrearPara(int estudioId)
    {
        var contexto = new ContextoEstudio();
        contexto.Establecer(estudioId, usuarioId: 2);
        var opciones = new DbContextOptionsBuilder<Procu360DbContext>()
            .UseSqlServer(ConfiguracionPruebas.CadenaAplicacionTest())
            .AddInterceptors(new SesionEstudioInterceptor(contexto))
            .Options;
        return new Procu360DbContext(opciones, contexto);
    }

    private static void Ejecutar(SqlConnection cn, SqlTransaction tx, string sql, params (string Nombre, object Valor)[] parametros)
    {
        using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (nombre, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nombre, valor);
        }
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void Estudio1_no_ve_expedientes_del_Estudio2()
    {
        using var db = CrearPara(1);

        // IgnoreQueryFilters: la consulta llega a SQL Server y la filtra solo el RLS (segunda barrera).
        var ajenos = db.Expediente.IgnoreQueryFilters().Count(e => e.EstudioId == 2);

        Assert.Equal(0, ajenos);
    }

    [Fact]
    public void Estudio1_no_puede_mover_un_registro_al_Estudio2()
    {
        using var cn = new SqlConnection(ConfiguracionPruebas.CadenaAplicacionTest());
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            Ejecutar(cn, tx, "EXEC sys.sp_set_session_context @key = N'EstudioId', @value = 1, @read_only = 1;");
            var nombre = "TEST_RLS_" + Guid.NewGuid().ToString("N")[..12];
            Ejecutar(cn, tx,
                "INSERT INTO seg.SecuenciaEstudio (EstudioId, Nombre, UltimoValor) VALUES (1, @n, 0);",
                ("@n", nombre));

            var ex = Assert.Throws<SqlException>(() => Ejecutar(cn, tx,
                "UPDATE seg.SecuenciaEstudio SET EstudioId = 2 WHERE EstudioId = 1 AND Nombre = @n;",
                ("@n", nombre)));

            Assert.Equal(ErrorPredicadoBloqueo, ex.Number);
        }
        finally
        {
            tx.Rollback();
        }
    }

    [Fact]
    public void Estudio1_no_puede_insertar_datos_en_el_Estudio2()
    {
        using var cn = new SqlConnection(ConfiguracionPruebas.CadenaAplicacionTest());
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            Ejecutar(cn, tx, "EXEC sys.sp_set_session_context @key = N'EstudioId', @value = 1, @read_only = 1;");

            var ex = Assert.Throws<SqlException>(() => Ejecutar(cn, tx,
                "INSERT INTO seg.SecuenciaEstudio (EstudioId, Nombre, UltimoValor) VALUES (2, @n, 0);",
                ("@n", "TEST_RLS_" + Guid.NewGuid().ToString("N")[..12])));

            Assert.Equal(ErrorPredicadoBloqueo, ex.Number);
        }
        finally
        {
            tx.Rollback();
        }
    }
}

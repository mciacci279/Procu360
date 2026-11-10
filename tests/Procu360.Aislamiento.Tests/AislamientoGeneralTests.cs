using Microsoft.Data.SqlClient;

namespace Procu360.Aislamiento.Tests;

/// <summary>
/// Cobertura de aislamiento sobre todas las tablas con EstudioId, leída desde la propia base
/// (no desde una lista fija): si se agrega una tabla sin RLS, estas pruebas fallan.
/// Todo se ejecuta con el login de la aplicación y las escrituras se revierten.
/// </summary>
public class AislamientoGeneralTests
{
    private const int ErrorPermisoDenegado = 229;

    private static SqlConnection Abrir(int? estudio)
    {
        var cn = new SqlConnection(ConfiguracionPruebas.CadenaSinPool());
        cn.Open();
        if (estudio is int e)
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"EXEC sys.sp_set_session_context @key = N'EstudioId', @value = {e}, @read_only = 1;";
            cmd.ExecuteNonQuery();
        }
        return cn;
    }

    private static List<(string Esquema, string Tabla)> TablasConEstudio()
    {
        using var cn = Abrir(null);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT s.name, t.name
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'EstudioId'
            ORDER BY s.name, t.name;
            """;
        var lista = new List<(string, string)>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            lista.Add((r.GetString(0), r.GetString(1)));
        }
        return lista;
    }

    private static string Nombre((string Esquema, string Tabla) t) => $"{t.Esquema}.{t.Tabla}";

    private static string Q((string Esquema, string Tabla) t) => $"[{t.Esquema}].[{t.Tabla}]";

    private static long Escalar(SqlConnection cn, string sql, SqlTransaction? tx = null)
    {
        using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static void Ejecutar(SqlConnection cn, SqlTransaction tx, string sql)
    {
        using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void Toda_tabla_con_EstudioId_tiene_filtro_y_bloqueo_de_RLS()
    {
        var tablas = TablasConEstudio();
        Assert.NotEmpty(tablas);

        var predicados = new Dictionary<string, HashSet<string>>();
        using (var cn = new SqlConnection(ConfiguracionPruebas.CadenaMetadatosTest()))
        {
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                SELECT OBJECT_SCHEMA_NAME(p.target_object_id) + '.' + OBJECT_NAME(p.target_object_id),
                       p.predicate_type_desc,
                       p.operation_desc
                FROM sys.security_predicates p;
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var tabla = r.GetString(0);
                var tipo = r.GetString(1);
                var operacion = r.IsDBNull(2) ? "" : r.GetString(2);
                if (!predicados.TryGetValue(tabla, out var set))
                {
                    predicados[tabla] = set = new HashSet<string>();
                }
                set.Add($"{tipo}:{operacion}");
            }
        }

        var faltantes = new List<string>();
        foreach (var t in tablas)
        {
            var nombre = Nombre(t);
            predicados.TryGetValue(nombre, out var set);
            set ??= new HashSet<string>();

            if (!set.Contains("FILTER:")) faltantes.Add($"{nombre}: sin FILTER");
            if (!set.Contains("BLOCK:AFTER INSERT")) faltantes.Add($"{nombre}: sin BLOCK AFTER INSERT");
            // aud.AuditoriaEvento es solo de inserción: no lleva bloqueo de UPDATE por diseño.
            if (nombre != "aud.AuditoriaEvento" && !set.Contains("BLOCK:AFTER UPDATE"))
                faltantes.Add($"{nombre}: sin BLOCK AFTER UPDATE");
        }

        Assert.Empty(faltantes);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void Un_Estudio_no_lee_filas_de_otro_Estudio_en_ninguna_tabla(int estudio, int otro)
    {
        var filtracion = new List<string>();
        var sinDatosPropios = new List<string>();

        foreach (var t in TablasConEstudio())
        {
            using var cn = Abrir(estudio);

            // Incluye IS NULL: los eventos de plataforma de la auditoría (EstudioId NULL) no deben verse.
            var ajenas = Escalar(cn, $"SELECT COUNT_BIG(*) FROM {Q(t)} WHERE EstudioId = {otro} OR EstudioId IS NULL");
            if (ajenas > 0) filtracion.Add($"{Nombre(t)}: {ajenas} filas ajenas visibles");

            // Sanidad: sin filas propias visibles la prueba sería vacía.
            var propias = Escalar(cn, $"SELECT COUNT_BIG(*) FROM {Q(t)} WHERE EstudioId = {estudio}");
            if (propias == 0) sinDatosPropios.Add(Nombre(t));
        }

        Assert.Empty(filtracion);
        Assert.Empty(sinDatosPropios);
    }

    [Fact]
    public void La_aplicacion_no_puede_modificar_ni_borrar_la_auditoria()
    {
        using var cn = Abrir(1);
        using var tx = cn.BeginTransaction();
        try
        {
            var update = Assert.Throws<SqlException>(() =>
                Ejecutar(cn, tx, "UPDATE aud.AuditoriaEvento SET Modulo = Modulo WHERE 1 = 0;"));
            Assert.Equal(ErrorPermisoDenegado, update.Number);

            var delete = Assert.Throws<SqlException>(() =>
                Ejecutar(cn, tx, "DELETE FROM aud.AuditoriaEvento WHERE 1 = 0;"));
            Assert.Equal(ErrorPermisoDenegado, delete.Number);
        }
        finally
        {
            tx.Rollback();
        }
    }
}

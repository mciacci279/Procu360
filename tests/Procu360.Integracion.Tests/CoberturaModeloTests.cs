using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// El modelo de EF y la base deben coincidir: toda tabla con EstudioId tiene entidad,
/// y toda entidad con EstudioId tiene filtro global. Si el scaffold agrega o quita una tabla,
/// estas pruebas lo detectan.
/// </summary>
public class CoberturaModeloTests
{
    private static Procu360DbContext Crear()
    {
        var contexto = new ContextoEstudio();
        var opciones = new DbContextOptionsBuilder<Procu360DbContext>()
            .UseSqlServer(ConfiguracionPruebas.CadenaAplicacionTest())
            .Options;
        return new Procu360DbContext(opciones, contexto);
    }

    [Fact]
    public void Cada_entidad_con_EstudioId_tiene_filtro_global()
    {
        using var db = Crear();

        var sinFiltro = db.Model.GetEntityTypes()
            .Where(e => e.FindProperty("EstudioId") is not null)
            .Where(e => e.GetQueryFilter() is null)
            .Select(e => $"{e.GetSchema() ?? "dbo"}.{e.GetTableName()}")
            .ToList();

        Assert.Empty(sinFiltro);
    }

    [Fact]
    public void Las_entidades_con_EstudioId_coinciden_con_las_tablas_de_la_base()
    {
        using var db = Crear();
        var entidades = db.Model.GetEntityTypes()
            .Where(e => e.FindProperty("EstudioId") is not null)
            .Select(e => $"{e.GetSchema() ?? "dbo"}.{e.GetTableName()}")
            .Order()
            .ToList();

        var tablasBase = new List<string>();
        using (var cn = new SqlConnection(ConfiguracionPruebas.CadenaAplicacionTest()))
        {
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                SELECT s.name, t.name
                FROM sys.tables t
                JOIN sys.schemas s ON s.schema_id = t.schema_id
                JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'EstudioId'
                ORDER BY s.name, t.name;
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                tablasBase.Add($"{r.GetString(0)}.{r.GetString(1)}");
            }
        }

        Assert.Equal(tablasBase, entidades);
    }
}

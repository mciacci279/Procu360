using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class FiltroYSesionTests
{
    private static Procu360DbContext Crear(ContextoEstudio contexto)
    {
        var opciones = new DbContextOptionsBuilder<Procu360DbContext>()
            .UseSqlServer(ConfiguracionPruebas.CadenaAplicacionTest())
            .AddInterceptors(new SesionEstudioInterceptor(contexto))
            .Options;
        return new Procu360DbContext(opciones, contexto);
    }

    private static ContextoEstudio ContextoDe(int estudioId)
    {
        var contexto = new ContextoEstudio();
        contexto.Establecer(estudioId, usuarioId: 2);
        return contexto;
    }

    [Fact]
    public void Filtro_devuelve_solo_los_expedientes_del_estudio_activo()
    {
        using (var db = Crear(ContextoDe(1)))
        {
            var estudios = db.Expediente.Select(e => e.EstudioId).ToList();
            Assert.Equal(13, estudios.Count);
            Assert.All(estudios, id => Assert.Equal(1, id));
        }

        using (var db = Crear(ContextoDe(2)))
        {
            var estudios = db.Expediente.Select(e => e.EstudioId).ToList();
            Assert.Equal(10, estudios.Count);
            Assert.All(estudios, id => Assert.Equal(2, id));
        }
    }

    [Fact]
    public void Sin_estudio_establecido_el_filtro_no_devuelve_filas()
    {
        using var db = Crear(new ContextoEstudio());

        Assert.Empty(db.Expediente.ToList());
    }

    [Fact]
    public void Cambiar_de_estudio_en_una_conexion_reutilizada_no_hereda_el_contexto_anterior()
    {
        // Con Max Pool Size=1 cada apertura reutiliza la misma conexión física.
        int[] secuencia = [1, 2, 1, 2];
        var esperados = new Dictionary<int, int> { [1] = 13, [2] = 10 };

        foreach (var estudio in secuencia)
        {
            using var db = Crear(ContextoDe(estudio));
            var estudios = db.Expediente.Select(e => e.EstudioId).ToList();

            Assert.Equal(esperados[estudio], estudios.Count);
            Assert.All(estudios, id => Assert.Equal(estudio, id));
        }
    }
}

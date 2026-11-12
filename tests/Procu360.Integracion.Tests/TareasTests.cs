using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tareas;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class TareasTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;
    private const string Diego = "DIEGO.LUNA@FERREYRA.TEST";

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public void Dispose()
    {
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    private IServiceScope Ambito(int usuario)
    {
        var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(Estudio1, usuario);
        return ambito;
    }

    private async Task<int> IdPorEmailAsync(string email)
    {
        using var ambito = _sp.CreateScope();
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == email).Select(u => u.Id).SingleAsync();
    }

    private async Task<long> IdExpedienteAsync(int nroOrden)
    {
        using var ambito = Ambito(Laura);
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente
            .Where(e => e.NroOrden == nroOrden).Select(e => e.Id).SingleAsync();
    }

    /// <summary>Recorre todas las páginas: la tarea puede no estar en la primera.</summary>
    private async Task<bool> EstaEnAsync(int usuario, FiltroTareas filtro, string titulo)
    {
        using var ambito = Ambito(usuario);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioTareas>();
        for (var pagina = 1; ; pagina++)
        {
            var r = await servicio.ListarAsync(filtro, pagina);
            if (r.Filas.Any(f => f.Titulo == titulo)) return true;
            if (pagina >= r.Paginas) return false;
        }
    }

    [Fact]
    public async Task Una_tarea_creada_aparece_en_pendientes_y_al_cumplirla_pasa_a_cumplidas()
    {
        var titulo = "PRUEBA-tarea-" + Guid.NewGuid().ToString("N")[..8];
        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioTareas>()
                .CrearAsync(titulo, ServicioMiDia.HoyEnCordoba().AddDays(3), null, false);
            Assert.True(r.Exito);
        }
        Assert.True(await EstaEnAsync(Laura, FiltroTareas.Pendientes, titulo));

        long id;
        using (var ambito = Ambito(Laura))
            id = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Tarea
                .Where(t => t.Titulo == titulo).Select(t => t.Id).SingleAsync();
        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioTareas>().CumplirAsync(id);
            Assert.True(r.Exito);
        }

        Assert.False(await EstaEnAsync(Laura, FiltroTareas.Pendientes, titulo));
        Assert.True(await EstaEnAsync(Laura, FiltroTareas.Cumplidas, titulo));
    }

    [Fact]
    public async Task El_tamanio_de_pagina_se_respeta_y_cambia_la_cantidad_de_paginas()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioTareas>();

        var todas = await servicio.ListarAsync(FiltroTareas.Todas, 1, 100);
        if (todas.Total < 2) return; // el seed siempre tiene tareas; si no, no hay qué paginar

        var deAUna = await servicio.ListarAsync(FiltroTareas.Todas, 1, 1);
        Assert.Single(deAUna.Filas);
        Assert.Equal(todas.Total, deAUna.Paginas);

        var segunda = await servicio.ListarAsync(FiltroTareas.Todas, 2, 1);
        Assert.Single(segunda.Filas);
        Assert.NotEqual(deAUna.Filas[0].Id, segunda.Filas[0].Id);
    }

    [Fact]
    public async Task No_se_puede_crear_una_tarea_en_un_expediente_fuera_del_alcance()
    {
        int diego = await IdPorEmailAsync(Diego);
        long fuera;
        using (var ambito = Ambito(Laura))
        {
            var lauraVe = await (await ambito.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            using var amb2 = Ambito(diego);
            var diegoVe = await (await amb2.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            fuera = lauraVe.First(x => !diegoVe.Contains(x));
        }

        using var ambitoDiego = Ambito(diego);
        var r = await ambitoDiego.ServiceProvider.GetRequiredService<ServicioTareas>()
            .CrearAsync("PRUEBA-fuera", ServicioMiDia.HoyEnCordoba(), fuera, false);

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Una_tarea_vinculada_a_un_expediente_fuera_del_alcance_no_se_ve_aunque_sea_para_todos()
    {
        int diego = await IdPorEmailAsync(Diego);
        var titulo = "PRUEBA-para-todos-" + Guid.NewGuid().ToString("N")[..6];
        long fuera;
        using (var ambito = Ambito(Laura))
        {
            var lauraVe = await (await ambito.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            using var amb2 = Ambito(diego);
            var diegoVe = await (await amb2.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            fuera = lauraVe.First(x => !diegoVe.Contains(x));

            // Lo crea Laura, visible para todos, pero vinculada a ese expediente.
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
            db.Tarea.Add(new Procu360.Infraestructura.Persistencia.Entidades.Tarea
            {
                EstudioId = Estudio1, ExpedienteId = fuera, Titulo = titulo, FechaInicio = ServicioMiDia.HoyEnCordoba(),
                FechaVencimiento = ServicioMiDia.HoyEnCordoba(), ResponsableUsuarioId = Laura, VisibleParaTodos = true,
                Estado = 1, Origen = 1, CreadoUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        Assert.False(await EstaEnAsync(diego, FiltroTareas.Todas, titulo));
        Assert.True(await EstaEnAsync(Laura, FiltroTareas.Todas, titulo));
    }

    [Fact]
    public async Task El_calendario_muestra_solo_las_tareas_del_rango_y_visibles_para_la_persona()
    {
        var titulo = "PRUEBA-cal-" + Guid.NewGuid().ToString("N")[..6];
        var dia = ServicioMiDia.HoyEnCordoba().AddDays(40);
        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioTareas>().CrearAsync(titulo, dia, null, true);
            Assert.True(r.Exito);
        }

        using (var ambito = Ambito(Laura))
        {
            var servicio = ambito.ServiceProvider.GetRequiredService<ServicioTareas>();
            var enRango = await servicio.EntreAsync(dia, dia);
            var fueraDeRango = await servicio.EntreAsync(dia.AddDays(1), dia.AddDays(10));
            Assert.Contains(enRango, f => f.Titulo == titulo);
            Assert.DoesNotContain(fueraDeRango, f => f.Titulo == titulo);
        }

        using (var ambito = Ambito(await IdPorEmailAsync(Diego)))
        {
            var deDiego = await ambito.ServiceProvider.GetRequiredService<ServicioTareas>().EntreAsync(dia, dia);
            Assert.Contains(deDiego, f => f.Titulo == titulo);
        }
    }

    [Fact]
    public async Task Una_tarea_sin_expediente_visible_para_todos_la_ven_todos_los_integrantes_con_alcance()
    {
        var titulo = "PRUEBA-agenda-" + Guid.NewGuid().ToString("N")[..6];
        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioTareas>()
                .CrearAsync(titulo, ServicioMiDia.HoyEnCordoba().AddDays(1), null, true);
            Assert.True(r.Exito);
        }

        Assert.True(await EstaEnAsync(await IdPorEmailAsync(Diego), FiltroTareas.Todas, titulo));
    }
}

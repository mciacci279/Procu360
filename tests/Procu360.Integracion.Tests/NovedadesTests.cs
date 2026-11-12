using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Novedades;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class NovedadesTests : IDisposable
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

    private IServiceScope Ambito(int estudio, int usuario)
    {
        var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudio, usuario);
        return ambito;
    }

    private async Task<long> IdExpedienteAsync(int nroOrden)
    {
        using var ambito = Ambito(Estudio1, Laura);
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente
            .Where(e => e.NroOrden == nroOrden).Select(e => e.Id).SingleAsync();
    }

    private async Task<long> CrearNovedadAsync(long? expediente)
    {
        using var ambito = Ambito(Estudio1, Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var n = new NovedadJudicial
        {
            EstudioId = Estudio1,
            ExpedienteId = expediente,
            Fecha = ServicioMiDia.HoyEnCordoba(),
            Texto = "PRUEBA-" + Guid.NewGuid().ToString("N")[..10] + " proveído de prueba",
            Fuente = 1,
            Leida = false,
            CreadoUtc = DateTime.UtcNow
        };
        db.NovedadJudicial.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    }

    private async Task<bool> EstaEnAsync(int usuario, PestanaNovedades pestana, long id)
    {
        using var ambito = Ambito(Estudio1, usuario);
        var r = await ambito.ServiceProvider.GetRequiredService<ServicioNovedades>().ListarAsync(pestana, 1);
        return r.Filas.Any(f => f.Id == id);
    }

    [Fact]
    public async Task El_tamanio_de_pagina_de_novedades_se_respeta()
    {
        await CrearNovedadAsync(await IdExpedienteAsync(641));
        await CrearNovedadAsync(await IdExpedienteAsync(641));

        using var ambito = Ambito(Estudio1, Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioNovedades>();
        var deAUna = await servicio.ListarAsync(PestanaNovedades.SinRevisar, 1, 1);

        Assert.Single(deAUna.Filas);
        Assert.Equal(deAUna.Total, deAUna.Paginas);
    }

    [Fact]
    public async Task Una_novedad_sin_revisar_aparece_en_la_bandeja_y_al_marcarla_pasa_a_revisadas()
    {
        var id = await CrearNovedadAsync(await IdExpedienteAsync(641));

        Assert.True(await EstaEnAsync(Laura, PestanaNovedades.SinRevisar, id));
        using (var ambito = Ambito(Estudio1, Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioNovedades>().MarcarRevisadaAsync(id);
            Assert.True(r.Exito);
        }

        Assert.False(await EstaEnAsync(Laura, PestanaNovedades.SinRevisar, id));
        Assert.True(await EstaEnAsync(Laura, PestanaNovedades.Revisadas, id));
    }

    [Fact]
    public async Task Vincular_una_novedad_sin_expediente_la_saca_de_sin_vincular()
    {
        var id = await CrearNovedadAsync(null);
        var expediente = await IdExpedienteAsync(641);
        Assert.True(await EstaEnAsync(Laura, PestanaNovedades.SinVincular, id));

        using (var ambito = Ambito(Estudio1, Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioNovedades>().VincularAsync(id, expediente);
            Assert.True(r.Exito);
        }

        Assert.False(await EstaEnAsync(Laura, PestanaNovedades.SinVincular, id));
    }

    [Fact]
    public async Task Crear_una_tarea_desde_la_novedad_la_deja_vinculada_y_no_se_repite()
    {
        var expediente = await IdExpedienteAsync(641);
        var id = await CrearNovedadAsync(expediente);
        using var ambito = Ambito(Estudio1, Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioNovedades>();

        var primera = await servicio.CrearTareaAsync(id, "PRUEBA-tarea", ServicioMiDia.HoyEnCordoba().AddDays(7));
        var segunda = await servicio.CrearTareaAsync(id, "PRUEBA-otra", ServicioMiDia.HoyEnCordoba().AddDays(7));

        Assert.True(primera.Exito);
        Assert.False(segunda.Exito);
        var tarea = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Tarea
            .Where(t => t.Titulo == "PRUEBA-tarea").SingleAsync();
        Assert.Equal(3, tarea.Origen);
        Assert.Equal(expediente, tarea.ExpedienteId);
    }

    [Fact]
    public async Task Un_gestor_no_ve_ni_opera_novedades_de_expedientes_fuera_de_su_alcance()
    {
        int diego;
        using (var ambito = _sp.CreateScope())
            diego = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
                .Where(u => u.EmailNormalizado == Diego).Select(u => u.Id).SingleAsync();

        // Un expediente que Laura ve y Diego no.
        long? fuera;
        using (var ambito = Ambito(Estudio1, Laura))
        {
            // Materializado: no se mezclan consultas de dos contextos en una misma sentencia.
            var lauraVe = await (await ambito.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            using var amb2 = Ambito(Estudio1, diego);
            var diegoVe = await (await amb2.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
            fuera = lauraVe.Cast<long?>().FirstOrDefault(x => !diegoVe.Contains(x!.Value));
        }
        Assert.NotNull(fuera);

        var id = await CrearNovedadAsync(fuera);

        Assert.False(await EstaEnAsync(diego, PestanaNovedades.SinRevisar, id));
        using var ambitoDiego = Ambito(Estudio1, diego);
        var r = await ambitoDiego.ServiceProvider.GetRequiredService<ServicioNovedades>().MarcarRevisadaAsync(id);
        Assert.False(r.Exito);
    }
}

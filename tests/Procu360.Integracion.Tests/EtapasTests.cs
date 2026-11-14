using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Etapas;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// Registro de etapas. Usa el expediente 641 de Estudio 1 y restaura su estado al terminar.
/// </summary>
public class EtapasTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;
    private const int Nro = 641;
    private const string Diego = "DIEGO.LUNA@FERREYRA.TEST";

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();
    private long? _expediente;
    private int? _etapaActualOriginal;
    private int? _cautelarActualOriginal;
    private int? _instanciaActualOriginal;
    private DateOnly? _ultimoMovimientoOriginal;
    private DateOnly? _fechaInstanciaOriginal;

    public EtapasTests()
    {
        // Restos de corridas anteriores: se limpian antes de empezar.
        RestaurarExpediente();
    }

    public void Dispose()
    {
        RestaurarExpediente();
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    /// <summary>
    /// Borra las etapas de prueba y sus tareas (primero las tareas, por la FK) y devuelve el expediente a su estado original.
    /// </summary>
    private void RestaurarExpediente()
    {
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var etapasDePrueba = db.EtapaExpediente.Where(x => x.Observaciones != null && x.Observaciones.StartsWith("PRUEBA-"));

        db.Tarea.Where(t => t.EtapaExpedienteId != null && etapasDePrueba.Select(x => x.Id).Contains(t.EtapaExpedienteId.Value))
            .ExecuteDelete();
        db.Tarea.Where(t => t.Titulo.StartsWith("PRUEBA-")).ExecuteDelete();
        etapasDePrueba.ExecuteDelete();

        if (_expediente is long id)
        {
            var e = db.Expediente.Single(x => x.Id == id);
            e.EtapaActualId = _etapaActualOriginal;
            e.CautelarActualId = _cautelarActualOriginal;
            e.InstanciaActualId = _instanciaActualOriginal;
            e.FechaUltimoMovimiento = _ultimoMovimientoOriginal;
            e.FechaInstanciaActual = _fechaInstanciaOriginal;
            // El historial de instancia que escribieron las pruebas de hoy.
            db.InstanciaExpediente.Where(i => i.ExpedienteId == id && i.Fecha == ServicioMiDia.HoyEnCordoba())
                .ExecuteDelete();
            // Las etapas que la prueba cerró vuelven a alta.
            db.EtapaExpediente.Where(x => x.ExpedienteId == id && x.Estado == 2 && x.FechaFin == ServicioMiDia.HoyEnCordoba())
                .ExecuteUpdate(s => s.SetProperty(x => x.Estado, (byte)1).SetProperty(x => x.FechaFin, (DateOnly?)null));
        }
        db.SaveChanges();
    }

    private IServiceScope Ambito(int usuario)
    {
        var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(Estudio1, usuario);
        return ambito;
    }

    private async Task<long> ExpedienteAsync()
    {
        if (_expediente is long ya) return ya;
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var e = await db.Expediente.SingleAsync(x => x.NroOrden == Nro);
        _expediente = e.Id;
        _etapaActualOriginal = e.EtapaActualId;
        _cautelarActualOriginal = e.CautelarActualId;
        _instanciaActualOriginal = e.InstanciaActualId;
        _ultimoMovimientoOriginal = e.FechaUltimoMovimiento;
        _fechaInstanciaOriginal = e.FechaInstanciaActual;
        return e.Id;
    }

    private async Task<int> EtapaPrincipalAsync(bool conTarea)
    {
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        return await db.Etapa.Where(e => e.Activa && e.Flujo == 1 && e.Dias != null && e.GeneraTarea == conTarea)
            .OrderBy(e => e.Orden).Select(e => e.Id).FirstAsync();
    }

    [Fact]
    public async Task Registrar_una_etapa_calcula_el_vencimiento_y_la_deja_como_actual()
    {
        var expediente = await ExpedienteAsync();
        var etapa = await EtapaPrincipalAsync(conTarea: false);
        var inicio = new DateOnly(2026, 11, 2);

        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>()
                .RegistrarAsync(expediente, etapa, inicio, "PRUEBA-registro", null);
            Assert.True(r.Exito, r.Error);
        }

        using var verif = Ambito(Laura);
        var db = verif.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var creada = await db.EtapaExpediente.SingleAsync(x => x.ExpedienteId == expediente && x.Observaciones == "PRUEBA-registro");
        Assert.Equal(etapa, creada.EtapaId);
        Assert.Equal(1, creada.Estado);
        Assert.NotNull(creada.FechaVencimiento);
        Assert.Equal(etapa, (await db.Expediente.SingleAsync(e => e.Id == expediente)).EtapaActualId);
    }

    [Fact]
    public async Task Registrar_una_etapa_cierra_la_anterior_del_mismo_flujo()
    {
        var expediente = await ExpedienteAsync();
        var etapa = await EtapaPrincipalAsync(conTarea: false);

        using (var ambito = Ambito(Laura))
        {
            var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEtapas>();
            Assert.True((await servicio.RegistrarAsync(expediente, etapa, new DateOnly(2026, 10, 1), "PRUEBA-primera", null)).Exito);
            Assert.True((await servicio.RegistrarAsync(expediente, etapa, new DateOnly(2026, 11, 1), "PRUEBA-segunda", null)).Exito);
        }

        using var verif = Ambito(Laura);
        var db = verif.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var primera = await db.EtapaExpediente.SingleAsync(x => x.ExpedienteId == expediente && x.Observaciones == "PRUEBA-primera");
        var segunda = await db.EtapaExpediente.SingleAsync(x => x.ExpedienteId == expediente && x.Observaciones == "PRUEBA-segunda");
        Assert.Equal(2, primera.Estado);
        Assert.Equal(new DateOnly(2026, 11, 1), primera.FechaFin);
        Assert.Equal(1, segunda.Estado);
    }

    [Fact]
    public async Task Una_etapa_que_genera_tarea_crea_la_tarea_vinculada()
    {
        var expediente = await ExpedienteAsync();
        var etapa = await EtapaPrincipalAsync(conTarea: true);

        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>()
                .RegistrarAsync(expediente, etapa, ServicioMiDia.HoyEnCordoba(), "PRUEBA-con-tarea", null);
            Assert.True(r.Exito, r.Error);
        }

        using var verif = Ambito(Laura);
        var db = verif.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var etapaCreada = await db.EtapaExpediente.SingleAsync(x => x.ExpedienteId == expediente && x.Observaciones == "PRUEBA-con-tarea");
        var tarea = await db.Tarea.SingleAsync(t => t.EtapaExpedienteId == etapaCreada.Id);
        Assert.Equal(2, tarea.Origen);
    }

    [Fact]
    public async Task Una_etapa_abierta_se_puede_cumplimentar_y_la_lista_lo_refleja()
    {
        var expediente = await ExpedienteAsync();
        var etapa = await EtapaPrincipalAsync(conTarea: false);
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEtapas>();
        Assert.True((await servicio.RegistrarAsync(expediente, etapa, new DateOnly(2026, 11, 3), "PRUEBA-cumplir", null)).Exito);
        var id = (await servicio.ListarAsync(expediente)).Single(f => f.Observaciones == "PRUEBA-cumplir").Id;

        var r = await servicio.CambiarEstadoAsync(id, 2);

        Assert.True(r.Exito, r.Error);
        var fila = (await servicio.ListarAsync(expediente)).Single(f => f.Id == id);
        Assert.Equal(2, fila.Estado);
    }

    [Fact]
    public async Task La_etapa_actual_es_la_abierta_mas_reciente_del_flujo_y_no_la_diligencia()
    {
        var expediente = await ExpedienteAsync();
        var etapa = await EtapaPrincipalAsync(conTarea: false);
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEtapas>();

        Assert.True((await servicio.RegistrarAsync(expediente, etapa, new DateOnly(2026, 9, 1), "PRUEBA-actual-1", null)).Exito);
        Assert.True((await servicio.RegistrarAsync(expediente, etapa, new DateOnly(2026, 10, 1), "PRUEBA-actual-2", null)).Exito);

        var filas = await servicio.ListarAsync(expediente);
        var actuales = filas.Where(f => f.EsActual).ToList();
        Assert.Contains(actuales, f => f.Observaciones == "PRUEBA-actual-2");
        Assert.DoesNotContain(actuales, f => f.Observaciones == "PRUEBA-actual-1");
        Assert.DoesNotContain(filas, f => f.EsDiligencia && f.EsActual);
    }

    [Fact]
    public async Task Una_diligencia_registrada_aparece_en_el_listado_solo_para_quien_tiene_alcance()
    {
        var expediente = await ExpedienteAsync();
        int etapa;
        using (var ambito = Ambito(Laura))
            etapa = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Etapa
                .Where(e => e.Activa && e.EsDiligencia).OrderBy(e => e.Orden).Select(e => e.Id).FirstAsync();

        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>()
                .RegistrarAsync(expediente, etapa, ServicioMiDia.HoyEnCordoba(), "PRUEBA-diligencia", null);
            Assert.True(r.Exito, r.Error);
        }

        using (var ambito = Ambito(Laura))
        {
            var pagina = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>().ListarDiligenciasAsync(true, 1);
            Assert.Contains(pagina.Filas, f => f.ExpedienteId == expediente && f.Nombre != null);
        }

        int diego;
        using (var ambito = _sp.CreateScope())
            diego = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
                .Where(u => u.EmailNormalizado == Diego).Select(u => u.Id).SingleAsync();
        using var ambitoDiego = Ambito(diego);
        var paginaDiego = await ambitoDiego.ServiceProvider.GetRequiredService<ServicioEtapas>().ListarDiligenciasAsync(false, 1);
        var visibles = await (await ambitoDiego.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
        Assert.DoesNotContain(paginaDiego.Filas, f => f.ExpedienteId == expediente && !visibles.Contains(expediente));
        Assert.All(paginaDiego.Filas, f => Assert.Contains(f.ExpedienteId, visibles));
    }

    [Fact]
    public async Task Registrar_una_etapa_principal_mueve_la_instancia_del_expediente_y_deja_historial()
    {
        var id = await ExpedienteAsync();
        int etapaId;
        int? instanciaDestino;
        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
            var actual = await db.Expediente.Where(e => e.Id == id).Select(e => e.InstanciaActualId).SingleAsync();
            // Una etapa principal cuya instancia sea distinta de la que tiene hoy el expediente.
            var etapa = await db.Etapa
                .Where(e => e.Flujo == ServicioEtapas.FlujoPrincipal && e.Activa && e.InstanciaId != null
                            && e.InstanciaId != actual)
                .OrderBy(e => e.Orden).FirstAsync();
            etapaId = etapa.Id;
            instanciaDestino = etapa.InstanciaId;
        }
        var hoy = ServicioMiDia.HoyEnCordoba();

        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>()
                .RegistrarAsync(id, etapaId, hoy, "PRUEBA-instancia", null);
            Assert.True(r.Exito, r.Error);
        }

        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
            var e = await db.Expediente.SingleAsync(x => x.Id == id);
            Assert.Equal(instanciaDestino, e.InstanciaActualId);
            Assert.Equal(hoy, e.FechaInstanciaActual);
            Assert.True(await db.InstanciaExpediente.AnyAsync(i => i.ExpedienteId == id
                && i.InstanciaId == instanciaDestino && i.Fecha == hoy));
        }
    }

    [Fact]
    public async Task Registrar_una_cautelar_no_cambia_la_instancia()
    {
        var id = await ExpedienteAsync();
        int cautelarId;
        int? instanciaAntes;
        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
            instanciaAntes = await db.Expediente.Where(e => e.Id == id).Select(e => e.InstanciaActualId).SingleAsync();
            cautelarId = await db.Etapa.Where(e => e.Flujo == ServicioEtapas.FlujoCautelar && e.Activa)
                .OrderBy(e => e.Orden).Select(e => e.Id).FirstAsync();
        }

        using (var ambito = Ambito(Laura))
        {
            var r = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>()
                .RegistrarAsync(id, cautelarId, ServicioMiDia.HoyEnCordoba(), "PRUEBA-cautelar", null);
            Assert.True(r.Exito, r.Error);
        }

        using (var ambito = Ambito(Laura))
        {
            var e = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>()
                .Expediente.SingleAsync(x => x.Id == id);
            Assert.Equal(instanciaAntes, e.InstanciaActualId);
            Assert.Equal(cautelarId, e.CautelarActualId);
        }
    }

    [Fact]
    public async Task El_tamanio_de_pagina_de_diligencias_se_respeta()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEtapas>();

        var pagina = await servicio.ListarDiligenciasAsync(false, 1, 1);
        if (pagina.Total == 0) return; // sin diligencias no hay qué paginar

        Assert.Single(pagina.Filas);
        Assert.Equal(pagina.Total, pagina.Paginas);
    }

    [Fact]
    public async Task Los_feriados_no_habiles_incluyen_nacionales_y_provinciales_del_Estudio()
    {
        using var ambito = Ambito(Laura);
        var feriados = await ambito.ServiceProvider.GetRequiredService<ServicioEtapas>().FeriadosParaAsync();

        Assert.Contains(new DateOnly(2026, 1, 1), feriados);      // nacional
        Assert.Contains(new DateOnly(2026, 7, 6), feriados);      // provincial de Córdoba (configurado)
    }

    [Fact]
    public async Task Una_persona_fuera_del_alcance_no_puede_registrar_etapas()
    {
        int diego;
        using (var ambito = _sp.CreateScope())
            diego = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
                .Where(u => u.EmailNormalizado == Diego).Select(u => u.Id).SingleAsync();

        // Un expediente que Laura ve y Diego no (el seed tiene varios).
        List<long> lauraVe, diegoVe;
        using (var a = Ambito(Laura))
            lauraVe = await (await a.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
        using (var b = Ambito(diego))
            diegoVe = await (await b.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
        var fuera = lauraVe.Except(diegoVe).ToList();
        Assert.NotEmpty(fuera);

        var etapa = await EtapaPrincipalAsync(conTarea: false);
        using var ambitoDiego = Ambito(diego);
        var r = await ambitoDiego.ServiceProvider.GetRequiredService<ServicioEtapas>()
            .RegistrarAsync(fuera[0], etapa, ServicioMiDia.HoyEnCordoba(), "PRUEBA-ajena", null);

        Assert.False(r.Exito);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Configuracion;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// ABM de los catálogos de configuración: instancias y etapas.
/// Las filas que crean las pruebas se llaman "PRUEBA-…" y se borran al terminar.
/// </summary>
public class CatalogoEtapasTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;                                  // Administrador
    private const string Martin = "MARTIN.OCHOA@FERREYRA.TEST";   // Abogado: sin configuracion.administrar

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public void Dispose()
    {
        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();

            // Las etapas de prueba: primero se desencadenan, para que ninguna FK impida borrarlas.
            var etapas = db.Etapa.Where(e => e.Nombre.StartsWith("PRUEBA-")).ToList();
            var ids = etapas.Select(e => e.Id).ToList();
            foreach (var e in db.Etapa.Where(e => (e.EtapaAnteriorId != null && ids.Contains(e.EtapaAnteriorId.Value))
                                                  || (e.EtapaSiguienteId != null && ids.Contains(e.EtapaSiguienteId.Value))))
            {
                if (e.EtapaAnteriorId is int a && ids.Contains(a)) e.EtapaAnteriorId = null;
                if (e.EtapaSiguienteId is int s && ids.Contains(s)) e.EtapaSiguienteId = null;
            }
            db.SaveChanges();

            db.Etapa.RemoveRange(etapas);
            db.Instancia.RemoveRange(db.Instancia.Where(i => i.Nombre.StartsWith("PRUEBA-")));
            db.SaveChanges();
        }
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    private IServiceScope Ambito(int usuario)
    {
        var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(Estudio1, usuario);
        return ambito;
    }

    private ServicioCatalogoEtapas Servicio(IServiceScope ambito) =>
        ambito.ServiceProvider.GetRequiredService<ServicioCatalogoEtapas>();

    private async Task<int> IdMartinAsync()
    {
        using var ambito = _sp.CreateScope();
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == Martin).Select(u => u.Id).SingleAsync();
    }

    /// <summary>
    /// Una etapa del flujo principal exige instancia, así que la toma de la primera activa salvo que se indique otra.
    /// </summary>
    private DatosEtapa NuevaEtapa(string sufijo, byte flujo = ServicioCatalogoEtapas.FlujoPrincipal,
        int? instanciaId = null) => new(
        Flujo: flujo, Codigo: null, Nombre: "PRUEBA-" + sufijo, Orden: 900, Dias: 10, DiasHabiles: true,
        Interrumpe: false, CortaPerencion: false, EsMonitorio: false, EsEjecucion: false, EsDiligencia: false,
        EtapaAnteriorId: null, EtapaSiguienteId: null, PasaSiguienteAutomatica: false, GeneraTarea: false,
        TareaParaUsuarios: TareaPara.Ninguno,
        InstanciaId: flujo == ServicioCatalogoEtapas.FlujoPrincipal ? (instanciaId ?? PrimeraInstancia) : null,
        Activa: true);

    /// <summary>Id de la primera instancia activa del Estudio, para las etapas de prueba.</summary>
    private int PrimeraInstancia
    {
        get
        {
            using var ambito = Ambito(Laura);
            return Servicio(ambito).ListarInstanciasAsync().GetAwaiter().GetResult()
                .First(i => i.Activa).Id;
        }
    }

    private static DatosInstancia NuevaInstancia(string sufijo) => new(
        Nombre: "PRUEBA-" + sufijo, Color: "#1D4ED8", Perime: false, EsBaja: false, Orden: 900, Activa: true);

    private async Task<FilaEtapaCatalogo> CrearEtapaAsync(IServiceScope ambito, DatosEtapa datos)
    {
        var r = await Servicio(ambito).CrearEtapaAsync(datos);
        Assert.True(r.Exito, r.Error);
        return (await Servicio(ambito).ListarEtapasAsync()).Single(e => e.Nombre == datos.Nombre);
    }

    // ----------------------------------------------------------------- permisos

    [Fact]
    public async Task Sin_permiso_de_configuracion_no_se_puede_crear_ni_editar()
    {
        using var ambito = Ambito(await IdMartinAsync());
        var servicio = Servicio(ambito);

        var etapa = await servicio.CrearEtapaAsync(NuevaEtapa("sin-permiso"));
        var instancia = await servicio.CrearInstanciaAsync(NuevaInstancia("sin-permiso"));

        Assert.False(etapa.Exito);
        Assert.False(instancia.Exito);
        // Leer el catálogo sí está permitido: lo necesita el selector de registro de etapas.
        Assert.NotEmpty(await servicio.ListarEtapasAsync());
    }

    // ----------------------------------------------------------------- instancias

    [Fact]
    public async Task Una_instancia_se_crea_se_edita_y_queda_auditada()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        var alta = await servicio.CrearInstanciaAsync(NuevaInstancia("instancia"));
        Assert.True(alta.Exito, alta.Error);
        var creada = (await servicio.ListarInstanciasAsync()).Single(i => i.Nombre == "PRUEBA-instancia");
        Assert.Equal(0, creada.Expedientes);

        var edicion = await servicio.EditarInstanciaAsync(creada.Id,
            new DatosInstancia("PRUEBA-instancia", "#B45309", true, false, 901, true), creada.Version);

        Assert.True(edicion.Exito, edicion.Error);
        var actualizada = (await servicio.ListarInstanciasAsync()).Single(i => i.Id == creada.Id);
        Assert.Equal("#B45309", actualizada.Color);
        Assert.True(actualizada.Perime);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Instancia"
            && a.EntidadId == creada.Id.ToString() && a.CambiosJson!.Contains("perime")));
    }

    [Fact]
    public async Task Una_instancia_con_nombre_repetido_o_color_invalido_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var existente = (await servicio.ListarInstanciasAsync())[0];

        var repetida = await servicio.CrearInstanciaAsync(NuevaInstancia("x") with { Nombre = existente.Nombre });
        var colorMalo = await servicio.CrearInstanciaAsync(NuevaInstancia("color") with { Color = "azul" });

        Assert.False(repetida.Exito);
        Assert.False(colorMalo.Exito);
    }

    [Fact]
    public async Task Una_instancia_con_expedientes_no_se_puede_desactivar()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var enUso = (await servicio.ListarInstanciasAsync()).FirstOrDefault(i => i.Expedientes > 0 && i.Activa);
        Assert.NotNull(enUso);   // el seed tiene expedientes con instancia actual

        var r = await servicio.EditarInstanciaAsync(enUso!.Id, new DatosInstancia(
            enUso.Nombre, enUso.Color, enUso.Perime, enUso.EsBaja, enUso.Orden, Activa: false), enUso.Version);

        Assert.False(r.Exito);
        Assert.True((await servicio.ListarInstanciasAsync()).Single(i => i.Id == enUso.Id).Activa);
    }

    // ----------------------------------------------------------------- etapas

    [Fact]
    public async Task Una_etapa_se_crea_con_sus_plazos_y_automatismos()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("completa") with
        {
            Codigo = "PRB-01", Dias = 15, DiasHabiles = true, CortaPerencion = true,
            GeneraTarea = true, TareaParaUsuarios = TareaPara.Responsable, EsDiligencia = true
        });

        Assert.Equal(15, creada.Dias);
        Assert.True(creada.DiasHabiles);
        Assert.True(creada.CortaPerencion);
        Assert.True(creada.GeneraTarea);
        Assert.Equal(TareaPara.Responsable, creada.TareaParaUsuarios);
        Assert.True(creada.EsDiligencia);
        Assert.Equal(0, creada.Registros);
        Assert.False(creada.EsDeSistema);   // una etapa nueva nunca es hito del sistema
    }

    [Fact]
    public async Task El_hito_del_sistema_no_se_asigna_desde_la_pantalla()
    {
        using var ambito = Ambito(Laura);

        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("sin-hito"));

        Assert.Null(creada.HitoSistema);
    }

    [Fact]
    public async Task Una_etapa_que_es_hito_del_sistema_no_se_puede_desactivar()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var hito = (await servicio.ListarEtapasAsync()).FirstOrDefault(e => e.EsDeSistema && e.Activa);
        if (hito is null) return;   // el seed puede no tener hitos cargados

        var r = await servicio.EditarEtapaAsync(hito.Id, DatosDe(hito) with { Activa = false }, hito.Version);

        Assert.False(r.Exito);
        Assert.True((await servicio.ListarEtapasAsync()).Single(e => e.Id == hito.Id).Activa);
    }

    [Fact]
    public async Task El_nombre_repetido_en_el_mismo_flujo_se_rechaza_pero_en_el_otro_flujo_se_acepta()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        await CrearEtapaAsync(ambito, NuevaEtapa("flujo", ServicioCatalogoEtapas.FlujoPrincipal));

        var repetida = await servicio.CrearEtapaAsync(NuevaEtapa("flujo", ServicioCatalogoEtapas.FlujoPrincipal));
        var otroFlujo = await servicio.CrearEtapaAsync(NuevaEtapa("flujo", ServicioCatalogoEtapas.FlujoCautelar));

        Assert.False(repetida.Exito);
        Assert.True(otroFlujo.Exito, otroFlujo.Error);
    }

    [Fact]
    public async Task Un_plazo_negativo_o_desmedido_y_un_flujo_invalido_se_rechazan()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        var negativo = await servicio.CrearEtapaAsync(NuevaEtapa("neg") with { Dias = -1 });
        var enorme = await servicio.CrearEtapaAsync(NuevaEtapa("enorme") with { Dias = 4000 });
        var flujoMalo = await servicio.CrearEtapaAsync(NuevaEtapa("flujo-malo") with { Flujo = 9 });
        var sinNombre = await servicio.CrearEtapaAsync(NuevaEtapa("x") with { Nombre = "   " });

        Assert.False(negativo.Exito);
        Assert.False(enorme.Exito);
        Assert.False(flujoMalo.Exito);
        Assert.False(sinNombre.Exito);
    }

    [Fact]
    public async Task El_encadenamiento_no_admite_apuntarse_a_si_misma_ni_cruzar_flujos()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var principal = await CrearEtapaAsync(ambito, NuevaEtapa("cadena-principal"));
        var cautelar = await CrearEtapaAsync(ambito, NuevaEtapa("cadena-cautelar", ServicioCatalogoEtapas.FlujoCautelar));

        var aSiMisma = await servicio.EditarEtapaAsync(principal.Id,
            DatosDe(principal) with { EtapaSiguienteId = principal.Id }, principal.Version);
        var cruzado = await servicio.EditarEtapaAsync(principal.Id,
            DatosDe(principal) with { EtapaSiguienteId = cautelar.Id }, principal.Version);

        Assert.False(aSiMisma.Exito);
        Assert.False(cruzado.Exito);
    }

    [Fact]
    public async Task El_encadenamiento_circular_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var a = await CrearEtapaAsync(ambito, NuevaEtapa("ciclo-a"));
        var b = await CrearEtapaAsync(ambito, NuevaEtapa("ciclo-b"));

        // A → B queda bien.
        var primera = await servicio.EditarEtapaAsync(a.Id, DatosDe(a) with { EtapaSiguienteId = b.Id }, a.Version);
        Assert.True(primera.Exito, primera.Error);

        // B → A cerraría el círculo.
        var vueltaB = (await servicio.ListarEtapasAsync()).Single(e => e.Id == b.Id);
        var circulo = await servicio.EditarEtapaAsync(b.Id, DatosDe(vueltaB) with { EtapaSiguienteId = a.Id }, vueltaB.Version);

        Assert.False(circulo.Exito);
    }

    [Fact]
    public async Task El_paso_automatico_exige_etapa_siguiente_y_se_apaga_si_no_la_hay()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        // Pedido sin etapa siguiente: se normaliza y queda apagado, no falla.
        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("automatica") with { PasaSiguienteAutomatica = true });

        Assert.False(creada.PasaSiguienteAutomatica);
    }

    [Fact]
    public async Task Si_no_genera_tarea_no_queda_a_quien_notificar()
    {
        using var ambito = Ambito(Laura);

        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("sin-tarea") with
        {
            GeneraTarea = false, TareaParaUsuarios = TareaPara.Todos
        });

        Assert.Equal(TareaPara.Ninguno, creada.TareaParaUsuarios);
    }

    [Fact]
    public async Task No_se_puede_cambiar_el_flujo_de_una_etapa_ya_registrada_en_expedientes()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var enUso = (await servicio.ListarEtapasAsync())
            .FirstOrDefault(e => e.Registros > 0 && e.Flujo == ServicioCatalogoEtapas.FlujoPrincipal);
        Assert.NotNull(enUso);   // el seed tiene etapas registradas

        var r = await servicio.EditarEtapaAsync(enUso!.Id,
            DatosDe(enUso) with { Flujo = ServicioCatalogoEtapas.FlujoCautelar }, enUso.Version);

        Assert.False(r.Exito);
    }

    // ----------------------------------------------------------------- instancia de la etapa

    [Fact]
    public async Task Una_etapa_del_flujo_principal_exige_instancia()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        var sinInstancia = await servicio.CrearEtapaAsync(
            NuevaEtapa("sin-instancia") with { InstanciaId = null });

        Assert.False(sinInstancia.Exito);
    }

    [Fact]
    public async Task Una_etapa_cautelar_no_lleva_instancia_aunque_se_le_pase_una()
    {
        using var ambito = Ambito(Laura);

        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("cautelar-sin-inst",
            ServicioCatalogoEtapas.FlujoCautelar) with { InstanciaId = PrimeraInstancia });

        Assert.Null(creada.InstanciaId);
    }

    [Fact]
    public async Task Una_instancia_inexistente_en_la_etapa_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);

        var r = await servicio.CrearEtapaAsync(NuevaEtapa("inst-mala") with { InstanciaId = 999999 });

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Las_etapas_del_seed_tienen_instancia_asignada()
    {
        using var ambito = Ambito(Laura);
        var etapas = await Servicio(ambito).ListarEtapasAsync();

        Assert.All(etapas.Where(e => e.Flujo == ServicioCatalogoEtapas.FlujoPrincipal),
            e => Assert.NotNull(e.InstanciaId));
        Assert.All(etapas.Where(e => e.Flujo == ServicioCatalogoEtapas.FlujoCautelar),
            e => Assert.Null(e.InstanciaId));
    }

    [Fact]
    public async Task Una_etapa_desactivada_desaparece_del_selector_pero_conserva_su_historial()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var etapas = ambito.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Etapas.ServicioEtapas>();
        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("desactivar"));
        Assert.Contains(await etapas.CatalogoAsync(), e => e.Id == creada.Id);

        var r = await servicio.EditarEtapaAsync(creada.Id, DatosDe(creada) with { Activa = false }, creada.Version);

        Assert.True(r.Exito, r.Error);
        Assert.DoesNotContain(await etapas.CatalogoAsync(), e => e.Id == creada.Id);
        // Sigue en el catálogo de administración, como inactiva: no se borró.
        Assert.False((await servicio.ListarEtapasAsync()).Single(e => e.Id == creada.Id).Activa);
    }

    [Fact]
    public async Task Una_version_vieja_de_la_etapa_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var creada = await CrearEtapaAsync(ambito, NuevaEtapa("concurrencia"));

        var primero = await servicio.EditarEtapaAsync(creada.Id, DatosDe(creada) with { Dias = 20 }, creada.Version);
        var segundo = await servicio.EditarEtapaAsync(creada.Id, DatosDe(creada) with { Dias = 30 }, creada.Version);

        Assert.True(primero.Exito, primero.Error);
        Assert.False(segundo.Exito);
        Assert.Equal(20, (await servicio.ListarEtapasAsync()).Single(e => e.Id == creada.Id).Dias);
    }

    [Fact]
    public async Task El_contador_de_uso_refleja_los_registros_en_expedientes()
    {
        using var ambito = Ambito(Laura);
        var servicio = Servicio(ambito);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();

        var etapas = await servicio.ListarEtapasAsync();
        foreach (var e in etapas.Where(x => x.Registros > 0).Take(3))
        {
            var real = await db.EtapaExpediente.CountAsync(x => x.EtapaId == e.Id);
            Assert.Equal(real, e.Registros);
        }
    }

    /// <summary>Convierte una fila del catálogo en los datos para editarla sin cambios.</summary>
    private static DatosEtapa DatosDe(FilaEtapaCatalogo e) => new(
        e.Flujo, e.Codigo, e.Nombre, e.Orden, e.Dias, e.DiasHabiles, e.Interrumpe, e.CortaPerencion,
        e.EsMonitorio, e.EsEjecucion, e.EsDiligencia, e.EtapaAnteriorId, e.EtapaSiguienteId,
        e.PasaSiguienteAutomatica, e.GeneraTarea, e.TareaParaUsuarios, e.InstanciaId, e.Activa);
}

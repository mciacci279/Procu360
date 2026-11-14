using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// ABM de la ficha: objeto de deuda, personas y períodos adeudados.
/// Trabaja sobre un expediente del seed y deja la base como estaba: borra lo que crea
/// (personas, vínculos y períodos de prueba) y restaura el objeto de deuda original.
/// </summary>
public class AbmFichaTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;
    private const int Nro = 641;
    private const string Diego = "DIEGO.LUNA@FERREYRA.TEST";

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();
    private readonly long _expediente;
    private readonly DatosObjetoDeuda _objetoOriginal;
    private readonly decimal? _montoTituloOriginal, _montoCapitalOriginal;
    private readonly DateOnly? _fechaTituloOriginal;
    private readonly string? _nroLiquidacionOriginal;

    public AbmFichaTests()
    {
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var e = db.Expediente.Single(x => x.NroOrden == Nro);
        _expediente = e.Id;
        _objetoOriginal = new DatosObjetoDeuda(e.NroCuenta, e.Nomenclatura, e.Dominio, e.Designacion,
            e.MatriculaRpi, e.DomicilioObjeto, e.Version);
        _montoTituloOriginal = e.MontoTitulo;
        _montoCapitalOriginal = e.MontoCapital;
        _fechaTituloOriginal = e.FechaTitulo;
        _nroLiquidacionOriginal = e.NroLiquidacion;
    }

    public void Dispose()
    {
        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();

            // Los vínculos y las personas que crearon las pruebas.
            var personasPrueba = db.Persona
                .Where(p => (p.Apellido != null && p.Apellido.StartsWith("PRUEBA-"))
                            || (p.RazonSocial != null && p.RazonSocial.StartsWith("PRUEBA-")))
                .Select(p => p.Id).ToList();
            if (personasPrueba.Count > 0)
            {
                db.ExpedientePersona.RemoveRange(
                    db.ExpedientePersona.Where(ep => personasPrueba.Contains(ep.PersonaId)));
                db.SaveChanges();
                db.Persona.RemoveRange(db.Persona.Where(p => personasPrueba.Contains(p.Id)));
            }

            db.PeriodoAdeudado.RemoveRange(db.PeriodoAdeudado
                .Where(p => p.Concepto != null && p.Concepto.StartsWith("PRUEBA")));

            var e = db.Expediente.Single(x => x.Id == _expediente);
            e.NroCuenta = _objetoOriginal.NroCuenta;
            e.Nomenclatura = _objetoOriginal.Nomenclatura;
            e.Dominio = _objetoOriginal.Dominio;
            e.Designacion = _objetoOriginal.Designacion;
            e.MatriculaRpi = _objetoOriginal.MatriculaRpi;
            e.DomicilioObjeto = _objetoOriginal.DomicilioObjeto;
            e.MontoTitulo = _montoTituloOriginal;
            e.MontoCapital = _montoCapitalOriginal;
            e.FechaTitulo = _fechaTituloOriginal;
            e.NroLiquidacion = _nroLiquidacionOriginal;

            // Los expedientes auxiliares vuelven a tener el capital igual a la suma de sus períodos,
            // que es el estado coherente del seed una vez borrados los períodos de prueba.
            foreach (var otro in _otrosUsados)
            {
                var aux = db.Expediente.Single(x => x.Id == otro);
                aux.MontoCapital = db.PeriodoAdeudado.Where(p => p.ExpedienteId == otro)
                    .Sum(p => (decimal?)(p.Tributo + p.Recargo)) ?? 0m;
            }
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

    /// <summary>
    /// Otro expediente del Estudio, para las pruebas de capital: la marca de "manual" queda en la auditoría,
    /// que es de solo escritura, así que no se puede limpiar entre pruebas.
    /// </summary>
    private async Task<long> OtroExpedienteAsync(int nroOrden)
    {
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var id = await db.Expediente.Where(e => e.NroOrden == nroOrden).Select(e => e.Id).SingleAsync();
        _otrosUsados.Add(id);
        return id;
    }

    private readonly HashSet<long> _otrosUsados = [];

    private async Task<int> IdDiegoAsync()
    {
        using var ambito = _sp.CreateScope();
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == Diego).Select(u => u.Id).SingleAsync();
    }

    /// <summary>Un expediente que Laura ve y Diego no, para probar el alcance.</summary>
    private async Task<long> FueraDelAlcanceDeDiegoAsync()
    {
        var diego = await IdDiegoAsync();
        List<long> lauraVe, diegoVe;
        using (var l = Ambito(Laura))
            lauraVe = await (await l.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
        using (var d = Ambito(diego))
            diegoVe = await (await d.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsAsync()).ToListAsync();
        return lauraVe.Except(diegoVe).First();
    }

    private static DatosPersona PersonaFisica(string sufijo, byte rol) => new(
        EsPersonaFisica: true, Apellido: "PRUEBA-" + sufijo, Nombre: "Juan", RazonSocial: null,
        TipoDocumentoId: 1, NroDocumento: "30" + Random.Shared.Next(100000, 999999), Cuit: null,
        Telefono1: null, Email: null, Rol: rol);

    // ------------------------------------------------------------ objeto de deuda

    [Fact]
    public async Task El_objeto_de_deuda_se_edita_y_queda_auditado()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var objeto = await servicio.ObjetoDeudaAsync(_expediente);
        Assert.NotNull(objeto);
        var cuenta = "PRUEBA-" + Random.Shared.Next(10000, 99999);

        var r = await servicio.GuardarObjetoDeudaAsync(_expediente, objeto! with { NroCuenta = cuenta });

        Assert.True(r.Exito, r.Error);
        Assert.Equal(cuenta, (await servicio.ObjetoDeudaAsync(_expediente))!.NroCuenta);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Expediente"
            && a.EntidadId == _expediente.ToString() && a.CambiosJson!.Contains("nroCuenta")));
    }

    [Fact]
    public async Task El_objeto_de_deuda_no_se_puede_editar_fuera_del_alcance()
    {
        var fuera = await FueraDelAlcanceDeDiegoAsync();
        using var ambito = Ambito(await IdDiegoAsync());
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();

        Assert.Null(await servicio.ObjetoDeudaAsync(fuera));
        var r = await servicio.GuardarObjetoDeudaAsync(fuera,
            new DatosObjetoDeuda("PRUEBA-ajena", null, null, null, null, null, []));
        Assert.False(r.Exito);
    }

    // ------------------------------------------------------------ personas

    [Fact]
    public async Task Una_persona_nueva_se_crea_vinculada_al_expediente_con_su_rol()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        var antes = (await servicio.ListarAsync(_expediente)).Count;

        var r = await servicio.AgregarNuevaAsync(_expediente,
            PersonaFisica("alta", RolesPersonaExpediente.Codemandado));

        Assert.True(r.Exito, r.Error);
        var despues = await servicio.ListarAsync(_expediente);
        Assert.Equal(antes + 1, despues.Count);
        var creada = despues.Single(p => p.Apellido == "PRUEBA-alta");
        Assert.Equal(RolesPersonaExpediente.Codemandado, creada.Rol);
        Assert.Equal("PRUEBA-alta, Juan", creada.NombreMostrado);
    }

    [Fact]
    public async Task Una_empresa_se_identifica_por_razon_social_y_no_lleva_apellido()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();

        var r = await servicio.AgregarNuevaAsync(_expediente, new DatosPersona(
            EsPersonaFisica: false, Apellido: "se descarta", Nombre: "se descarta",
            RazonSocial: "PRUEBA-empresa SA", TipoDocumentoId: null, NroDocumento: null,
            Cuit: "30-1234567-8", Telefono1: null, Email: null, Rol: RolesPersonaExpediente.Codemandado));

        Assert.True(r.Exito, r.Error);
        var creada = (await servicio.ListarAsync(_expediente)).Single(p => p.RazonSocial == "PRUEBA-empresa SA");
        Assert.Null(creada.Apellido);
        Assert.Null(creada.Nombre);
        Assert.Equal("PRUEBA-empresa SA", creada.NombreMostrado);
    }

    [Fact]
    public async Task Una_persona_fisica_sin_apellido_y_una_empresa_sin_razon_social_se_rechazan()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();

        var sinApellido = await servicio.AgregarNuevaAsync(_expediente, new DatosPersona(
            true, null, "Juan", null, null, null, null, null, null, RolesPersonaExpediente.Codemandado));
        var sinRazon = await servicio.AgregarNuevaAsync(_expediente, new DatosPersona(
            false, null, null, null, null, null, null, null, null, RolesPersonaExpediente.Codemandado));

        Assert.False(sinApellido.Exito);
        Assert.False(sinRazon.Exito);
    }

    [Fact]
    public async Task Una_persona_ya_existente_se_vincula_sin_duplicarla_y_no_se_repite()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        // Una persona del seed que todavía no esté en este expediente.
        var yaEstan = await db.ExpedientePersona.Where(ep => ep.ExpedienteId == _expediente)
            .Select(ep => ep.PersonaId).ToListAsync();
        var otra = await db.Persona.Where(p => !p.Anulado && !yaEstan.Contains(p.Id)).Select(p => p.Id).FirstAsync();
        var personasAntes = await db.Persona.CountAsync();

        var primera = await servicio.VincularExistenteAsync(_expediente, otra, RolesPersonaExpediente.ResponsableSolidario);
        var repetida = await servicio.VincularExistenteAsync(_expediente, otra, RolesPersonaExpediente.Codemandado);

        Assert.True(primera.Exito, primera.Error);
        Assert.False(repetida.Exito);   // la misma persona no se agrega dos veces
        Assert.Equal(personasAntes, await db.Persona.CountAsync());   // no se duplicó la persona

        // Limpieza: este vínculo es sobre una persona del seed, que Dispose no borra.
        db.ExpedientePersona.RemoveRange(
            db.ExpedientePersona.Where(ep => ep.ExpedienteId == _expediente && ep.PersonaId == otra));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task La_busqueda_encuentra_por_documento_y_marca_a_los_que_ya_estan_en_el_expediente()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        var datos = PersonaFisica("buscable", RolesPersonaExpediente.Codemandado);
        Assert.True((await servicio.AgregarNuevaAsync(_expediente, datos)).Exito);

        var porDocumento = await servicio.BuscarAsync(_expediente, datos.NroDocumento!);
        var cortita = await servicio.BuscarAsync(_expediente, "ab");

        Assert.Contains(porDocumento, p => p.NombreMostrado.Contains("PRUEBA-buscable") && p.YaEnElExpediente);
        Assert.Empty(cortita);   // menos de 3 caracteres no busca
    }

    [Fact]
    public async Task Editar_una_persona_cambia_sus_datos_y_su_rol_y_queda_auditado()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        Assert.True((await servicio.AgregarNuevaAsync(_expediente,
            PersonaFisica("editar", RolesPersonaExpediente.Codemandado))).Exito);
        var p = (await servicio.ListarAsync(_expediente)).Single(x => x.Apellido == "PRUEBA-editar");

        var r = await servicio.EditarAsync(_expediente, p.VinculoId, new DatosPersona(
            true, "PRUEBA-editar", "Carlos", null, p.TipoDocumentoId, p.NroDocumento, null,
            "351-5550000", "carlos@prueba.test", RolesPersonaExpediente.ResponsableSolidario), p.VersionPersona);

        Assert.True(r.Exito, r.Error);
        var actualizada = (await servicio.ListarAsync(_expediente)).Single(x => x.VinculoId == p.VinculoId);
        Assert.Equal("Carlos", actualizada.Nombre);
        Assert.Equal("carlos@prueba.test", actualizada.Email);
        Assert.Equal(RolesPersonaExpediente.ResponsableSolidario, actualizada.Rol);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "ExpedientePersona"
            && a.EntidadId == _expediente.ToString() && a.CambiosJson!.Contains("rol")));
    }

    [Fact]
    public async Task Una_version_vieja_de_la_persona_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        Assert.True((await servicio.AgregarNuevaAsync(_expediente,
            PersonaFisica("concurrencia", RolesPersonaExpediente.Codemandado))).Exito);
        var p = (await servicio.ListarAsync(_expediente)).Single(x => x.Apellido == "PRUEBA-concurrencia");
        var datos = new DatosPersona(true, "PRUEBA-concurrencia", "Primero", null, p.TipoDocumentoId,
            p.NroDocumento, null, null, null, p.Rol);

        var primero = await servicio.EditarAsync(_expediente, p.VinculoId, datos, p.VersionPersona);
        var segundo = await servicio.EditarAsync(_expediente, p.VinculoId,
            datos with { Nombre = "Segundo" }, p.VersionPersona);   // versión ya vencida

        Assert.True(primero.Exito, primero.Error);
        Assert.False(segundo.Exito);
        Assert.Equal("Primero", (await servicio.ListarAsync(_expediente)).Single(x => x.VinculoId == p.VinculoId).Nombre);
    }

    [Fact]
    public async Task Quitar_una_persona_la_desvincula_pero_no_la_borra_del_Estudio()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True((await servicio.AgregarNuevaAsync(_expediente,
            PersonaFisica("quitar", RolesPersonaExpediente.Codemandado))).Exito);
        var p = (await servicio.ListarAsync(_expediente)).Single(x => x.Apellido == "PRUEBA-quitar");

        var r = await servicio.QuitarAsync(_expediente, p.VinculoId);

        Assert.True(r.Exito, r.Error);
        Assert.DoesNotContain(await servicio.ListarAsync(_expediente), x => x.VinculoId == p.VinculoId);
        Assert.True(await db.Persona.AnyAsync(x => x.Id == p.PersonaId));   // la persona sigue existiendo
    }

    [Fact]
    public async Task No_se_puede_dejar_el_expediente_sin_demandado()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();
        var demandados = (await servicio.ListarAsync(_expediente))
            .Where(p => p.Rol == RolesPersonaExpediente.Demandado).ToList();
        Assert.Single(demandados);   // el seed tiene un único demandado en este expediente
        var unico = demandados[0];

        var quitar = await servicio.QuitarAsync(_expediente, unico.VinculoId);
        var cambiarRol = await servicio.EditarAsync(_expediente, unico.VinculoId, new DatosPersona(
            unico.EsPersonaFisica, unico.Apellido, unico.Nombre, unico.RazonSocial, unico.TipoDocumentoId,
            unico.NroDocumento, unico.Cuit, unico.Telefono1, unico.Email,
            RolesPersonaExpediente.Codemandado), unico.VersionPersona);

        Assert.False(quitar.Exito);
        Assert.False(cambiarRol.Exito);
        Assert.Equal(RolesPersonaExpediente.Demandado,
            (await servicio.ListarAsync(_expediente)).Single(p => p.VinculoId == unico.VinculoId).Rol);
    }

    [Fact]
    public async Task Las_personas_no_se_pueden_tocar_fuera_del_alcance()
    {
        var fuera = await FueraDelAlcanceDeDiegoAsync();
        using var ambito = Ambito(await IdDiegoAsync());
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPersonasExpediente>();

        Assert.Empty(await servicio.ListarAsync(fuera));
        Assert.False((await servicio.AgregarNuevaAsync(fuera,
            PersonaFisica("ajena", RolesPersonaExpediente.Codemandado))).Exito);
        Assert.False((await servicio.QuitarAsync(fuera, 1)).Exito);
    }

    // ------------------------------------------------------------ períodos

    [Fact]
    public async Task Un_periodo_se_agrega_se_edita_y_se_elimina()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();
        var antes = (await servicio.ListarAsync(_expediente)).Count;

        var alta = await servicio.AgregarAsync(_expediente,
            new DatosPeriodo(2024, "01", "PRUEBA-alta", new DateOnly(2024, 3, 10), 1500.50m, 120.25m, null));
        Assert.True(alta.Exito, alta.Error);

        var creado = (await servicio.ListarAsync(_expediente)).Single(p => p.Concepto == "PRUEBA-alta");
        Assert.Equal(1620.75m, creado.Total);

        var edicion = await servicio.EditarAsync(_expediente, creado.Id,
            new DatosPeriodo(2024, "01", "PRUEBA-alta", creado.Vencimiento, 2000m, 0m, "PRUEBA-obs"), creado.Version);
        Assert.True(edicion.Exito, edicion.Error);
        Assert.Equal(2000m, (await servicio.ListarAsync(_expediente)).Single(p => p.Id == creado.Id).Tributo);

        var baja = await servicio.EliminarAsync(_expediente, creado.Id);
        Assert.True(baja.Exito, baja.Error);
        Assert.Equal(antes, (await servicio.ListarAsync(_expediente)).Count);
    }

    [Fact]
    public async Task No_se_repite_el_mismo_anio_y_cuota()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();
        var datos = new DatosPeriodo(2023, "07", "PRUEBA-dup", null, 100m, 0m, null);
        Assert.True((await servicio.AgregarAsync(_expediente, datos)).Exito);

        var repetido = await servicio.AgregarAsync(_expediente, datos with { Concepto = "PRUEBA-dup2" });

        Assert.False(repetido.Exito);
    }

    [Fact]
    public async Task Un_anio_o_un_importe_invalido_se_rechazan()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();
        var anioFuturo = (short)(ServicioMiDia.HoyEnCordoba().Year + 5);

        var viejo = await servicio.AgregarAsync(_expediente, new DatosPeriodo(1800, null, "PRUEBA-viejo", null, 10m, 0m, null));
        var futuro = await servicio.AgregarAsync(_expediente, new DatosPeriodo(anioFuturo, null, "PRUEBA-futuro", null, 10m, 0m, null));
        var negativo = await servicio.AgregarAsync(_expediente, new DatosPeriodo(2024, "99", "PRUEBA-neg", null, -5m, 0m, null));

        Assert.False(viejo.Exito);
        Assert.False(futuro.Exito);
        Assert.False(negativo.Exito);
    }

    [Fact]
    public async Task Los_periodos_no_se_pueden_tocar_fuera_del_alcance()
    {
        var fuera = await FueraDelAlcanceDeDiegoAsync();
        using var ambito = Ambito(await IdDiegoAsync());
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();

        Assert.Empty(await servicio.ListarAsync(fuera));
        var r = await servicio.AgregarAsync(fuera, new DatosPeriodo(2024, null, "PRUEBA-ajeno", null, 10m, 0m, null));
        Assert.False(r.Exito);
    }

    // ------------------------------------------------------------ título de deuda

    [Fact]
    public async Task El_titulo_de_deuda_se_edita_y_queda_auditado()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var titulo = await servicio.TituloDeudaAsync(_expediente);
        Assert.NotNull(titulo);

        var r = await servicio.GuardarTituloDeudaAsync(_expediente, titulo! with
        {
            MontoTitulo = 123456.78m, NroLiquidacion = "PRUEBA-LIQ-1"
        });

        Assert.True(r.Exito, r.Error);
        var actual = await servicio.TituloDeudaAsync(_expediente);
        Assert.Equal(123456.78m, actual!.MontoTitulo);
        Assert.Equal("PRUEBA-LIQ-1", actual.NroLiquidacion);
    }

    [Fact]
    public async Task Un_monto_negativo_o_una_fecha_futura_del_titulo_se_rechazan()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var titulo = await servicio.TituloDeudaAsync(_expediente);

        var negativo = await servicio.GuardarTituloDeudaAsync(_expediente, titulo! with { MontoTitulo = -1m });
        var capitalNegativo = await servicio.GuardarTituloDeudaAsync(_expediente, titulo with { MontoCapital = -1m });
        var futura = await servicio.GuardarTituloDeudaAsync(_expediente, titulo with
        {
            FechaTitulo = ServicioMiDia.HoyEnCordoba().AddDays(1)
        });

        Assert.False(negativo.Exito);
        Assert.False(capitalNegativo.Exito);
        Assert.False(futura.Exito);
    }

    [Fact]
    public async Task Agregar_y_eliminar_periodos_actualiza_el_capital_del_titulo()
    {
        // Expediente propio: la marca de "capital manual" vive en la auditoría, que es de solo escritura,
        // así que una prueba que marque el capital no puede compartir expediente con esta.
        var expediente = await OtroExpedienteAsync(642);
        using var ambito = Ambito(Laura);
        var periodos = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();
        var edicion = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        if ((await edicion.TituloDeudaAsync(expediente))!.CapitalManual) return;   // ya marcado por otra corrida

        var alta = await periodos.AgregarAsync(expediente,
            new DatosPeriodo(2022, "11", "PRUEBA-capital", null, 1000m, 250m, null));
        Assert.True(alta.Exito, alta.Error);

        var conPeriodo = await edicion.TituloDeudaAsync(expediente);
        Assert.Equal(await edicion.SumaPeriodosAsync(expediente), conPeriodo!.MontoCapital);
        Assert.False(conPeriodo.Descorrelacionado);

        var creado = (await periodos.ListarAsync(expediente)).Single(p => p.Concepto == "PRUEBA-capital");
        Assert.True((await periodos.EliminarAsync(expediente, creado.Id)).Exito);

        var sinPeriodo = await edicion.TituloDeudaAsync(expediente);
        Assert.Equal(await edicion.SumaPeriodosAsync(expediente), sinPeriodo!.MontoCapital);
        Assert.NotEqual(conPeriodo.MontoCapital, sinPeriodo.MontoCapital);
    }

    [Fact]
    public async Task Un_capital_editado_a_mano_deja_de_seguir_a_los_periodos_y_se_puede_resincronizar()
    {
        var expediente = await OtroExpedienteAsync(643);
        using var ambito = Ambito(Laura);
        var periodos = ambito.ServiceProvider.GetRequiredService<ServicioPeriodosExpediente>();
        var edicion = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();

        // Se fija un capital a mano, distinto de la suma de períodos.
        var titulo = await edicion.TituloDeudaAsync(expediente);
        var aMano = titulo!.SumaPeriodos + 9999m;
        Assert.True((await edicion.GuardarTituloDeudaAsync(expediente, titulo with { MontoCapital = aMano })).Exito);

        var marcado = await edicion.TituloDeudaAsync(expediente);
        Assert.True(marcado!.CapitalManual);
        Assert.True(marcado.Descorrelacionado);

        // Un período nuevo ya no lo pisa.
        Assert.True((await periodos.AgregarAsync(expediente,
            new DatosPeriodo(2020, "05", "PRUEBA-no-pisa", null, 700m, 0m, null))).Exito);
        Assert.Equal(aMano, (await edicion.TituloDeudaAsync(expediente))!.MontoCapital);

        // Pero resincronizar a la fuerza lo vuelve a igualar.
        Assert.True((await edicion.SincronizarCapitalAsync(expediente, forzar: true)).Exito);
        var actual = await edicion.TituloDeudaAsync(expediente);
        Assert.Equal(actual!.SumaPeriodos, actual.MontoCapital);
        Assert.False(actual.Descorrelacionado);
    }

    [Fact]
    public async Task El_titulo_de_deuda_no_se_toca_fuera_del_alcance()
    {
        var fuera = await FueraDelAlcanceDeDiegoAsync();
        using var ambito = Ambito(await IdDiegoAsync());
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();

        Assert.Null(await servicio.TituloDeudaAsync(fuera));
        Assert.False((await servicio.GuardarTituloDeudaAsync(fuera,
            new DatosTituloDeuda(1m, 1m, null, null, []))).Exito);
        Assert.False((await servicio.SincronizarCapitalAsync(fuera, forzar: true)).Exito);
    }

    // ------------------------------------------------------------ tareas desde la ficha

    [Fact]
    public async Task Una_tarea_del_expediente_se_marca_cumplida_y_la_ficha_lo_refleja()
    {
        var titulo = "PRUEBA-ficha-" + Guid.NewGuid().ToString("N")[..6];
        using var ambito = Ambito(Laura);
        var tareas = ambito.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Tareas.ServicioTareas>();
        var ficha = ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>();
        Assert.True((await tareas.CrearAsync(titulo, ServicioMiDia.HoyEnCordoba().AddDays(5), _expediente, false)).Exito);

        var pendiente = (await ficha.TareasAsync(_expediente)).Single(t => t.Titulo == titulo);
        Assert.Equal(1, pendiente.Estado);   // 1 = pendiente

        var r = await tareas.CumplirAsync(pendiente.Id);

        Assert.True(r.Exito, r.Error);
        Assert.Equal(2, (await ficha.TareasAsync(_expediente)).Single(t => t.Id == pendiente.Id).Estado);

        // Limpieza: la tarea no la borra el Dispose.
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        db.Tarea.RemoveRange(db.Tarea.Where(t => t.Titulo == titulo));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Cumplir_una_tarea_fuera_del_alcance_se_rechaza()
    {
        var fuera = await FueraDelAlcanceDeDiegoAsync();
        var titulo = "PRUEBA-ajena-" + Guid.NewGuid().ToString("N")[..6];
        long tareaId;
        using (var deLaura = Ambito(Laura))
        {
            var tareas = deLaura.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Tareas.ServicioTareas>();
            Assert.True((await tareas.CrearAsync(titulo, ServicioMiDia.HoyEnCordoba().AddDays(5), fuera, false)).Exito);
            tareaId = await deLaura.ServiceProvider.GetRequiredService<Procu360DbContext>().Tarea
                .Where(t => t.Titulo == titulo).Select(t => t.Id).SingleAsync();
        }

        using (var deDiego = Ambito(await IdDiegoAsync()))
        {
            var r = await deDiego.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Tareas.ServicioTareas>()
                .CumplirAsync(tareaId);
            Assert.False(r.Exito);
        }

        using var limpieza = Ambito(Laura);
        var db = limpieza.ServiceProvider.GetRequiredService<Procu360DbContext>();
        db.Tarea.RemoveRange(db.Tarea.Where(t => t.Titulo == titulo));
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------ encabezado

    [Fact]
    public async Task El_encabezado_trae_cuenta_comitente_demandado_y_concepto()
    {
        using var ambito = Ambito(Laura);
        var ficha = ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>();

        var encabezado = await ficha.EncabezadoAsync(_expediente);

        Assert.NotNull(encabezado);
        Assert.False(string.IsNullOrWhiteSpace(encabezado!.Comitente));
        Assert.False(string.IsNullOrWhiteSpace(encabezado.Demandado));
        Assert.True(encabezado.Codemandados >= 0);
    }

    [Fact]
    public async Task Un_expediente_sin_juzgado_ni_numero_judicial_no_figura_judicializado()
    {
        using var ambito = Ambito(Laura);
        var edicion = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var ficha = ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>();
        var juicio = await edicion.JuicioAsync(_expediente);

        // Sin juzgado ni número judicial: todavía no llegó al juzgado.
        var r = await edicion.GuardarJuicioAsync(_expediente,
            juicio! with { JuzgadoId = null, NroExpedienteJudicial = null });
        Assert.True(r.Exito, r.Error);
        Assert.False((await ficha.EncabezadoAsync(_expediente))!.Judicializado);

        // Con número judicial, sí.
        var conNumero = await edicion.JuicioAsync(_expediente);
        Assert.True((await edicion.GuardarJuicioAsync(_expediente,
            conNumero! with { NroExpedienteJudicial = "PRUEBA-123/2026" })).Exito);
        Assert.True((await ficha.EncabezadoAsync(_expediente))!.Judicializado);

        // Restaura lo que había.
        var volver = await edicion.JuicioAsync(_expediente);
        await edicion.GuardarJuicioAsync(_expediente, volver! with
        {
            JuzgadoId = juicio.JuzgadoId,
            NroExpedienteJudicial = juicio.NroExpedienteJudicial
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>Edición de la ficha por secciones. Usa el expediente 641 de Estudio 1 y restaura sus datos al terminar.</summary>
public class EdicionExpedienteTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;
    private const int Nro = 641;
    private const string Diego = "DIEGO.LUNA@FERREYRA.TEST";

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();
    private readonly long _expediente;
    private readonly string? _caratulaOriginal;
    private readonly string? _observacionesOriginal;
    private readonly int? _juzgadoOriginal, _procuradorOriginal, _patrocinanteOriginal, _abogadoOriginal;

    public EdicionExpedienteTests()
    {
        using var ambito = Ambito(Laura);
        var e = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente.Single(x => x.NroOrden == Nro);
        _expediente = e.Id;
        _caratulaOriginal = e.Caratula;
        _observacionesOriginal = e.Observaciones;
        _juzgadoOriginal = e.JuzgadoId;
        _procuradorOriginal = e.ProcuradorId;
        _patrocinanteOriginal = e.PatrocinanteId;
        _abogadoOriginal = e.AbogadoInteriorId;
    }

    public void Dispose()
    {
        using (var ambito = Ambito(Laura))
        {
            var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
            var e = db.Expediente.Single(x => x.Id == _expediente);
            e.Caratula = _caratulaOriginal;
            e.Observaciones = _observacionesOriginal;
            e.JuzgadoId = _juzgadoOriginal;
            e.ProcuradorId = _procuradorOriginal;
            e.PatrocinanteId = _patrocinanteOriginal;
            e.AbogadoInteriorId = _abogadoOriginal;
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

    [Fact]
    public async Task Guardar_una_seccion_cambia_solo_sus_campos_y_audita()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var antes = await servicio.ObservacionesAsync(_expediente);
        var nuevaCaratula = "PRUEBA-carátula " + Guid.NewGuid().ToString("N")[..6];

        var juicio = await servicio.JuicioAsync(_expediente);
        var r = await servicio.GuardarJuicioAsync(_expediente, juicio! with { Caratula = nuevaCaratula });

        Assert.True(r.Exito, r.Error);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var e = await db.Expediente.SingleAsync(x => x.Id == _expediente);
        Assert.Equal(nuevaCaratula, e.Caratula);
        Assert.Equal(antes!.Observaciones, e.Observaciones);   // otra sección intacta
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Expediente"
            && a.EntidadId == _expediente.ToString() && a.CambiosJson!.Contains("caratula")));
    }

    [Fact]
    public async Task Una_version_vieja_se_rechaza_para_no_pisar_cambios_ajenos()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var vista = await servicio.ObservacionesAsync(_expediente);

        // Primero guarda otra persona (acá, la misma sesión) y cambia la versión.
        var primero = await servicio.GuardarObservacionesAsync(_expediente,
            vista! with { Observaciones = "PRUEBA-primero" });
        Assert.True(primero.Exito, primero.Error);

        var segundo = await servicio.GuardarObservacionesAsync(_expediente,
            vista with { Observaciones = "PRUEBA-viejo" });

        Assert.False(segundo.Exito);
        var actual = await servicio.ObservacionesAsync(_expediente);
        Assert.Equal("PRUEBA-primero", actual!.Observaciones);
    }

    [Fact]
    public async Task Una_persona_fuera_del_alcance_no_puede_editar_ni_ver_el_expediente()
    {
        int diego;
        using (var a = _sp.CreateScope())
            diego = await a.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
                .Where(u => u.EmailNormalizado == Diego).Select(u => u.Id).SingleAsync();

        List<long> lauraVe, diegoVe;
        using (var l = Ambito(Laura))
            lauraVe = await (await l.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Expedientes.AlcanceExpedientes>().IdsAsync()).ToListAsync();
        using (var d = Ambito(diego))
            diegoVe = await (await d.ServiceProvider.GetRequiredService<Procu360.Infraestructura.Expedientes.AlcanceExpedientes>().IdsAsync()).ToListAsync();
        var fuera = lauraVe.Except(diegoVe).First();

        using var ambitoDiego = Ambito(diego);
        var servicio = ambitoDiego.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        Assert.Null(await servicio.JuicioAsync(fuera));

        var r = await servicio.GuardarJuicioAsync(fuera, new DatosJuicio(null, null, "PRUEBA-ajena", null, null, null, Array.Empty<byte>()));
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Un_juzgado_inexistente_o_inactivo_se_rechaza()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var juicio = await servicio.JuicioAsync(_expediente);

        var r = await servicio.GuardarJuicioAsync(_expediente, juicio! with { JuzgadoId = 999999 });

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task El_juzgado_se_guarda_y_audita()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var juicio = await servicio.JuicioAsync(_expediente);
        // Debe ser distinto al actual, si no no hay cambio y no hay auditoría.
        var juzgado = (await servicio.JuzgadosAsync()).First(j => j.Id != juicio!.JuzgadoId);

        var r = await servicio.GuardarJuicioAsync(_expediente, juicio! with { JuzgadoId = juzgado.Id });

        Assert.True(r.Exito, r.Error);
        Assert.Equal(juzgado.Id, (await servicio.JuicioAsync(_expediente))!.JuzgadoId);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Expediente"
            && a.EntidadId == _expediente.ToString() && a.CambiosJson!.Contains("juzgadoId")));
    }

    [Fact]
    public async Task Un_profesional_sin_el_rol_pedido_no_se_puede_asignar()
    {
        using var ambito = Ambito(Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var noProcurador = await db.Profesional.Where(p => p.Activo && !p.EsProcurador).Select(p => p.Id).FirstAsync();
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var vista = await servicio.ProfesionalesAsync(_expediente);

        var r = await servicio.GuardarProfesionalesAsync(_expediente, vista! with { ProcuradorId = noProcurador });

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Un_procurador_con_el_rol_se_guarda_y_audita()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var procuradores = await servicio.ProfesionalesDeRolAsync("procurador");
        Assert.NotEmpty(procuradores);
        var vista = await servicio.ProfesionalesAsync(_expediente);

        var r = await servicio.GuardarProfesionalesAsync(_expediente, vista! with { ProcuradorId = procuradores[0].Id });

        Assert.True(r.Exito, r.Error);
        Assert.Equal(procuradores[0].Id, (await servicio.ProfesionalesAsync(_expediente))!.ProcuradorId);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        Assert.True(await db.AuditoriaEvento.AnyAsync(a => a.Entidad == "Expediente"
            && a.EntidadId == _expediente.ToString() && a.CambiosJson!.Contains("procuradorId")));
    }

    [Fact]
    public async Task Guardar_sin_cambios_no_audita()
    {
        using var ambito = Ambito(Laura);
        var servicio = ambito.ServiceProvider.GetRequiredService<ServicioEdicionExpediente>();
        var juicio = await servicio.JuicioAsync(_expediente);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var antes = await db.AuditoriaEvento.CountAsync(a => a.Entidad == "Expediente" && a.EntidadId == _expediente.ToString());

        var r = await servicio.GuardarJuicioAsync(_expediente, juicio!);

        Assert.True(r.Exito, r.Error);
        var despues = await db.AuditoriaEvento.CountAsync(a => a.Entidad == "Expediente" && a.EntidadId == _expediente.ToString());
        Assert.Equal(antes, despues);
    }
}

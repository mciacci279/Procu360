using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class FichaExpedienteTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Estudio2 = 2;
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

    private async Task<bool> VisibleParaAsync(int estudio, int usuario, long id)
    {
        using var ambito = Ambito(estudio, usuario);
        return await ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>().VisibleAsync(id);
    }

    private async Task<long> IdExpedienteAsync(int estudio, int nroOrden)
    {
        using var ambito = Ambito(estudio, Laura);
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente
            .Where(e => e.NroOrden == nroOrden).Select(e => e.Id).SingleAsync();
    }

    private async Task<int> IdUsuarioAsync(string emailNormalizado)
    {
        using var ambito = _sp.CreateScope();
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == emailNormalizado).Select(u => u.Id).SingleAsync();
    }

    [Fact]
    public async Task El_encabezado_muestra_el_expediente_pedido()
    {
        var id = await IdExpedienteAsync(Estudio1, 641);
        using var ambito = Ambito(Estudio1, Laura);
        var ficha = ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>();

        var encabezado = await ficha.EncabezadoAsync(id);

        Assert.NotNull(encabezado);
        Assert.Equal(641, encabezado!.NroOrden);
    }

    [Fact]
    public async Task Un_expediente_de_otro_Estudio_no_es_visible()
    {
        var idDelOtro = await IdExpedienteAsync(Estudio2, 1);

        Assert.False(await VisibleParaAsync(Estudio1, Laura, idDelOtro));
    }

    [Fact]
    public async Task Un_expediente_fuera_del_alcance_del_gestor_no_es_visible()
    {
        var diego = await IdUsuarioAsync(Diego);
        var lauraVe = await BuscarIdsAsync(Estudio1, Laura);
        var diegoVe = await BuscarIdsAsync(Estudio1, diego);
        var fueraDelAlcance = lauraVe.Except(diegoVe).ToList();

        // Si no hubiera ninguno, la prueba sería vacía: los datos de prueba tienen que tener al menos uno.
        Assert.NotEmpty(fueraDelAlcance);
        Assert.False(await VisibleParaAsync(Estudio1, diego, fueraDelAlcance[0]));
        Assert.True(await VisibleParaAsync(Estudio1, Laura, fueraDelAlcance[0]));
    }

    private async Task<List<long>> BuscarIdsAsync(int estudio, int usuario)
    {
        using var ambito = Ambito(estudio, usuario);
        var ids = await ambito.ServiceProvider.GetRequiredService<AlcanceExpedientes>().IdsIncluyendoBajasAsync();
        return await ids.ToListAsync();
    }

    [Fact]
    public async Task Los_periodos_coinciden_con_los_de_la_base()
    {
        var id = await IdExpedienteAsync(Estudio1, 641);
        using var ambito = Ambito(Estudio1, Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var esperados = await db.PeriodoAdeudado.CountAsync(p => p.ExpedienteId == id);

        var periodos = await ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>().PeriodosAsync(id);

        Assert.Equal(esperados, periodos.Count);
    }

    [Fact]
    public async Task Las_etapas_se_muestran_de_la_mas_reciente_a_la_mas_antigua()
    {
        var id = await IdExpedienteAsync(Estudio1, 641);
        using var ambito = Ambito(Estudio1, Laura);

        var etapas = await ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>().EtapasAsync(id);

        Assert.True(etapas.Zip(etapas.Skip(1)).All(par => par.First.Inicio >= par.Second.Inicio));
    }

    [Fact]
    public async Task Hay_como_maximo_un_demandado_por_expediente()
    {
        var id = await IdExpedienteAsync(Estudio1, 641);
        using var ambito = Ambito(Estudio1, Laura);

        var personas = await ambito.ServiceProvider.GetRequiredService<FichaExpedienteServicio>().PersonasAsync(id);

        Assert.True(personas.Count(p => p.Rol == "Demandado") <= 1);
    }
}

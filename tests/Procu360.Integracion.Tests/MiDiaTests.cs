using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class MiDiaTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Laura = 2;                                  // Administrador (todo el Estudio)
    private const string Diego = "diego.luna@ferreyra.test";      // Gestor, alcance asignados

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

    private async Task<MiDiaResumen> ResumenDeAsync(int usuario)
    {
        using var ambito = Ambito(Estudio1, usuario);
        return await ambito.ServiceProvider.GetRequiredService<ServicioMiDia>().ResumenAsync();
    }

    private int IdUsuario(string email)
    {
        using var ambito = _sp.CreateScope();
        var normalizado = email.ToUpperInvariant();
        return ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == normalizado).Select(u => u.Id).Single();
    }

    [Fact]
    public async Task Los_umbrales_salen_de_la_configuracion_del_Estudio()
    {
        var resumen = await ResumenDeAsync(Laura);

        Assert.Equal(90, resumen.DiasSinMovimiento);
        Assert.Equal(30, resumen.DiasAlertaPerencion);
        Assert.False(resumen.SinAlcance);
    }

    [Fact]
    public async Task Con_alcance_total_las_novedades_y_los_vencimientos_coinciden_con_la_base()
    {
        var resumen = await ResumenDeAsync(Laura);
        var hoy = ServicioMiDia.HoyEnCordoba();

        using var ambito = Ambito(Estudio1, Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        var novedadesSinLeer = await db.NovedadJudicial.CountAsync(n => !n.Leida);
        var sinMovimiento = await db.Expediente.CountAsync(e =>
            !e.Anulado && e.FechaUltimoMovimiento != null && e.FechaUltimoMovimiento < hoy.AddDays(-90));

        Assert.Equal(novedadesSinLeer, resumen.NovedadesSinRevisar);
        Assert.Equal(sinMovimiento, resumen.SinMovimiento);
    }

    [Fact]
    public async Task Un_gestor_por_asignados_nunca_ve_mas_que_el_administrador()
    {
        var todo = await ResumenDeAsync(Laura);
        var asignado = await ResumenDeAsync(IdUsuario(Diego));

        Assert.False(asignado.SinAlcance);
        Assert.True(asignado.SinMovimiento <= todo.SinMovimiento);
        Assert.True(asignado.NovedadesSinRevisar <= todo.NovedadesSinRevisar);
        Assert.True(asignado.EtapasVencidas <= todo.EtapasVencidas);
    }

    [Fact]
    public async Task Las_perenciones_listadas_vencen_dentro_del_margen_de_alerta()
    {
        var resumen = await ResumenDeAsync(Laura);
        var limite = ServicioMiDia.HoyEnCordoba().AddDays(resumen.DiasAlertaPerencion);

        Assert.All(resumen.Perenciones, p => Assert.True(p.Vence <= limite));
    }

    [Fact]
    public async Task Una_persona_sin_roles_recibe_el_aviso_de_sin_alcance()
    {
        var persona = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, PruebasIdentidad.NuevoEmail());
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio1, persona, EstadosMembresia.Activo);

        var resumen = await ResumenDeAsync(persona);

        Assert.True(resumen.SinAlcance);
        Assert.Equal(0, resumen.NovedadesSinRevisar);
        Assert.Empty(resumen.Tareas);
    }
}

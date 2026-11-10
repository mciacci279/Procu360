using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Expedientes;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

public class BusquedaExpedientesTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Estudio2 = 2;
    private const int Laura = 2;                                 // Administrador, Estudio 1

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

    private async Task<ResultadoBusqueda> BuscarAsync(int estudio, int usuario, CriteriosBusqueda criterios)
    {
        using var ambito = Ambito(estudio, usuario);
        return await ambito.ServiceProvider.GetRequiredService<ServicioBusquedaExpedientes>().BuscarAsync(criterios);
    }

    private static CriteriosBusqueda Criterio(string? texto = null, int pagina = 1, int tamanio = 25,
        int? comitente = null, bool sinMov = false, bool bajas = false) =>
        new(texto, comitente, null, sinMov, bajas, pagina, tamanio);

    private async Task<(int Nro, string? Caratula)> ExpedienteAsync(int estudio, int nroOrden)
    {
        using var ambito = Ambito(estudio, Laura);
        var e = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente
            .Where(x => x.NroOrden == nroOrden).Select(x => new { x.NroOrden, x.Caratula }).SingleAsync();
        return (e.NroOrden, e.Caratula);
    }

    [Fact]
    public async Task Buscar_641_encuentra_el_expediente_00641()
    {
        var resultado = await BuscarAsync(Estudio1, Laura, Criterio("641"));

        Assert.Contains(resultado.Filas, f => f.NroOrden == 641);
    }

    [Fact]
    public async Task Los_ceros_a_la_izquierda_no_cambian_el_resultado()
    {
        var conCeros = await BuscarAsync(Estudio1, Laura, Criterio("00641"));
        var sinCeros = await BuscarAsync(Estudio1, Laura, Criterio("641"));

        Assert.Equal(sinCeros.Total, conCeros.Total);
        Assert.Equal(sinCeros.Filas.Select(f => f.Id), conCeros.Filas.Select(f => f.Id));
    }

    [Fact]
    public async Task Busca_por_parte_de_la_caratula()
    {
        var (nro, caratula) = await ExpedienteAsync(Estudio1, 641);
        Assert.False(string.IsNullOrWhiteSpace(caratula));
        var fragmento = caratula![..Math.Min(8, caratula.Length)];

        var resultado = await BuscarAsync(Estudio1, Laura, Criterio(fragmento));

        Assert.Contains(resultado.Filas, f => f.NroOrden == nro);
    }

    [Fact]
    public async Task Un_gestor_por_asignados_no_ve_mas_expedientes_que_el_administrador()
    {
        using var ambito = Ambito(Estudio1, Laura);
        var diego = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().EstudioUsuario
            .Where(m => m.Usuario.EmailNormalizado == "DIEGO.LUNA@FERREYRA.TEST").Select(m => m.UsuarioId).SingleAsync();

        var todos = await BuscarAsync(Estudio1, Laura, Criterio(tamanio: 100));
        var delGestor = await BuscarAsync(Estudio1, diego, Criterio(tamanio: 100));

        Assert.True(delGestor.Total <= todos.Total);
        Assert.All(delGestor.Filas, f => Assert.Contains(todos.Filas, t => t.Id == f.Id));
    }

    [Fact]
    public async Task Un_Estudio_no_encuentra_expedientes_del_otro()
    {
        var valeria = IdPorEmail("valeria.lucero@lucero.test");
        var delUno = await BuscarAsync(Estudio1, Laura, Criterio(tamanio: 100));
        var delDos = await BuscarAsync(Estudio2, valeria, Criterio(tamanio: 100));

        Assert.Equal(await NoAnuladosAsync(Estudio1), delUno.Total);
        Assert.Equal(await NoAnuladosAsync(Estudio2), delDos.Total);
        Assert.Empty(delUno.Filas.Select(f => f.Id).Intersect(delDos.Filas.Select(f => f.Id)));
    }

    private int IdPorEmail(string email)
    {
        using var ambito = _sp.CreateScope();
        var normalizado = email.ToUpperInvariant();
        return ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == normalizado).Select(u => u.Id).Single();
    }

    private async Task<int> NoAnuladosAsync(int estudio)
    {
        using var ambito = Ambito(estudio, Laura);
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Expediente.CountAsync(e => !e.Anulado);
    }

    [Fact]
    public async Task La_paginacion_respeta_el_tamanio_de_pagina()
    {
        var total = await BuscarAsync(Estudio1, Laura, Criterio(tamanio: 100));
        var primera = await BuscarAsync(Estudio1, Laura, Criterio(pagina: 1, tamanio: 5));
        var ultima = await BuscarAsync(Estudio1, Laura, Criterio(pagina: primera.Paginas, tamanio: 5));

        Assert.Equal(total.Total, primera.Total);
        Assert.True(primera.Filas.Count <= 5);
        Assert.Equal((int)Math.Ceiling(total.Total / 5.0), primera.Paginas);
        Assert.Equal(total.Total - (primera.Paginas - 1) * 5, ultima.Filas.Count);
    }

    [Fact]
    public async Task El_filtro_sin_movimiento_solo_devuelve_expedientes_antiguos()
    {
        var resultado = await BuscarAsync(Estudio1, Laura, Criterio(sinMov: true, tamanio: 100));
        var limite = ServicioMiDia.HoyEnCordoba().AddDays(-90);

        Assert.All(resultado.Filas, f => Assert.True(f.FechaUltimoMovimiento != null && f.FechaUltimoMovimiento < limite));
    }

    [Fact]
    public async Task Los_dados_de_baja_solo_aparecen_si_se_piden()
    {
        var sinBajas = await BuscarAsync(Estudio1, Laura, Criterio(tamanio: 100));
        var conBajas = await BuscarAsync(Estudio1, Laura, Criterio(tamanio: 100, bajas: true));

        Assert.All(sinBajas.Filas, f => Assert.False(f.Anulado));
        Assert.True(conBajas.Total >= sinBajas.Total);
    }
}

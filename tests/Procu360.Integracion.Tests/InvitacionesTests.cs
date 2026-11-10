using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// Invitaciones, aceptación y selector de Estudio. Usa Estudio 1 como administrador
/// y el Estudio 2 solo para crear membresías previas.
/// </summary>
public class InvitacionesTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Estudio2 = 2;
    private const int AdministradorEstudio1 = 2; // Laura Ferreyra (seed)

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public void Dispose()
    {
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    /// <summary>Ejecuta una operación con el contexto de un Estudio y del administrador.</summary>
    private async Task<T> ComoAdministradorAsync<T>(int estudioId, Func<ServicioInvitaciones, Task<T>> operacion)
    {
        using var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudioId, AdministradorEstudio1);
        return await operacion(ambito.ServiceProvider.GetRequiredService<ServicioInvitaciones>());
    }

    private async Task<T> ComoUsuarioAsync<T>(int usuarioId, Func<ServicioInvitaciones, Task<T>> operacion)
    {
        using var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().EstablecerUsuario(usuarioId);
        return await operacion(ambito.ServiceProvider.GetRequiredService<ServicioInvitaciones>());
    }

    private async Task<IReadOnlyList<MembresiaActiva>> MembresiasDeAsync(int usuarioId)
    {
        using var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().EstablecerUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<ServicioSesion>().MembresiasActivasAsync(usuarioId);
    }

    private async Task<byte?> EstadoMembresiaAsync(int estudioId, int usuarioId)
    {
        using var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudioId, AdministradorEstudio1);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();
        return db.EstudioUsuario.Where(m => m.UsuarioId == usuarioId).Select(m => (byte?)m.Estado).FirstOrDefault();
    }

    private static DatosInvitacion Datos(string email) => new(email, "Nuevo", "Integrante");

    [Fact]
    public async Task Invitar_crea_la_membresia_en_estado_invitado_y_entrega_un_enlace()
    {
        var email = PruebasIdentidad.NuevoEmail();

        var resultado = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));

        Assert.Equal(TipoResultadoInvitacion.Creada, resultado.Tipo);
        Assert.False(string.IsNullOrEmpty(resultado.Token));
        Assert.Equal(EstadosMembresia.Invitado, await EstadoMembresiaAsync(Estudio1, resultado.UsuarioId!.Value));
    }

    [Fact]
    public async Task Aceptar_fija_la_contrasena_activa_la_membresia_y_el_enlace_es_de_un_solo_uso()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var invitacion = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));
        var usuarioId = invitacion.UsuarioId!.Value;

        var primera = await ComoUsuarioAsync(usuarioId, s =>
            s.AceptarAsync(usuarioId, invitacion.Token!, PruebasIdentidad.ClaveValida));
        var segunda = await ComoUsuarioAsync(usuarioId, s =>
            s.AceptarAsync(usuarioId, invitacion.Token!, PruebasIdentidad.ClaveValida));

        Assert.True(primera.Exito);
        Assert.Equal(1, primera.MembresiasActivadas);
        Assert.False(segunda.Exito);
        Assert.Equal(EstadosMembresia.Activo, await EstadoMembresiaAsync(Estudio1, usuarioId));
    }

    [Fact]
    public async Task Un_enlace_alterado_no_se_acepta()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var invitacion = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));
        var usuarioId = invitacion.UsuarioId!.Value;

        var resultado = await ComoUsuarioAsync(usuarioId, s =>
            s.AceptarAsync(usuarioId, invitacion.Token! + "x", PruebasIdentidad.ClaveValida));

        Assert.False(resultado.Exito);
        Assert.Equal(EstadosMembresia.Invitado, await EstadoMembresiaAsync(Estudio1, usuarioId));
    }

    [Fact]
    public async Task Un_enlace_vencido_no_se_acepta()
    {
        using var sp = PruebasIdentidad.CrearServicios(vidaToken: TimeSpan.FromSeconds(1));
        var email = PruebasIdentidad.NuevoEmail();
        ServicioInvitaciones Servicio(IServiceScope ambito) => ambito.ServiceProvider.GetRequiredService<ServicioInvitaciones>();

        ResultadoInvitacion invitacion;
        using (var ambito = sp.CreateScope())
        {
            ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(Estudio1, AdministradorEstudio1);
            invitacion = await Servicio(ambito).InvitarAsync(AdministradorEstudio1, Datos(email));
        }
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        using (var ambito = sp.CreateScope())
        {
            ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().EstablecerUsuario(invitacion.UsuarioId!.Value);
            var resultado = await Servicio(ambito).AceptarAsync(
                invitacion.UsuarioId!.Value, invitacion.Token!, PruebasIdentidad.ClaveValida);
            Assert.False(resultado.Exito);
        }
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    [Fact]
    public async Task Email_con_membresia_activa_en_otro_Estudio_queda_pendiente_sin_enlace()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var usuarioId = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio2, usuarioId, EstadosMembresia.Activo);

        var resultado = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));

        Assert.Equal(TipoResultadoInvitacion.Pendiente, resultado.Tipo);
        Assert.Null(resultado.Token);
        Assert.Equal(EstadosMembresia.Pendiente, await EstadoMembresiaAsync(Estudio1, usuarioId));
    }

    [Fact]
    public async Task No_se_invita_dos_veces_al_mismo_Estudio()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));

        var segunda = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));

        Assert.Equal(TipoResultadoInvitacion.Error, segunda.Tipo);
    }

    [Fact]
    public async Task Un_usuario_desactivado_no_se_puede_invitar()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email, activo: false);

        var resultado = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));

        Assert.Equal(TipoResultadoInvitacion.Error, resultado.Tipo);
    }

    [Fact]
    public async Task Aceptar_una_invitacion_no_cambia_las_membresias_de_otro_Estudio()
    {
        // Persona invitada a Estudio 1 que ya tiene otra membresía (pendiente) en Estudio 2.
        var email = PruebasIdentidad.NuevoEmail();
        var invitacion = await ComoAdministradorAsync(Estudio1, s => s.InvitarAsync(AdministradorEstudio1, Datos(email)));
        var id = invitacion.UsuarioId!.Value;
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio2, id, EstadosMembresia.Pendiente);

        var resultado = await ComoUsuarioAsync(id, s => s.AceptarAsync(id, invitacion.Token!, PruebasIdentidad.ClaveValida));

        Assert.True(resultado.Exito);
        Assert.Equal(EstadosMembresia.Activo, await EstadoMembresiaAsync(Estudio1, id));
        Assert.Equal(EstadosMembresia.Pendiente, await EstadoMembresiaAsync(Estudio2, id));
    }

    [Fact]
    public async Task El_selector_lista_solo_las_membresias_activas_del_usuario()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var usuarioId = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio1, usuarioId, EstadosMembresia.Activo);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio2, usuarioId, EstadosMembresia.Invitado);

        var membresias = await MembresiasDeAsync(usuarioId);

        var unica = Assert.Single(membresias);
        Assert.Equal(Estudio1, unica.EstudioId);
    }
}

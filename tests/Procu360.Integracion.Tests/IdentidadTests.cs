using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Integracion.Tests;

public class IdentidadTests : IDisposable
{
    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public void Dispose()
    {
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    private async Task<SignInResult> IngresarAsync(string email, string clave)
    {
        using var ambito = _sp.CreateScope();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var firmas = ambito.ServiceProvider.GetRequiredService<SignInManager<Usuario>>();
        var usuario = await usuarios.FindByEmailAsync(email);
        Assert.NotNull(usuario);
        return await firmas.CheckPasswordSignInAsync(usuario!, clave, lockoutOnFailure: true);
    }

    private async Task<Usuario> LeerAsync(string email)
    {
        using var ambito = _sp.CreateScope();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return (await usuarios.FindByEmailAsync(email))!;
    }

    [Fact]
    public async Task Ingresa_con_contrasena_correcta_y_usuario_activo()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);

        var resultado = await IngresarAsync(email, PruebasIdentidad.ClaveValida);

        Assert.True(resultado.Succeeded);
    }

    [Fact]
    public async Task Contrasena_incorrecta_suma_un_acceso_fallido()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);

        var resultado = await IngresarAsync(email, "Otra-Clave-2026");

        Assert.False(resultado.Succeeded);
        Assert.Equal(1, (await LeerAsync(email)).AccesosFallidos);
    }

    [Fact]
    public async Task Bloquea_la_cuenta_tras_cinco_intentos_fallidos_y_ni_la_clave_correcta_entra()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);

        for (var i = 0; i < 5; i++)
        {
            await IngresarAsync(email, "Otra-Clave-2026");
        }

        Assert.True((await IngresarAsync(email, "Otra-Clave-2026")).IsLockedOut);
        Assert.True((await IngresarAsync(email, PruebasIdentidad.ClaveValida)).IsLockedOut);
    }

    [Fact]
    public async Task Un_usuario_desactivado_no_puede_ingresar()
    {
        var email = PruebasIdentidad.NuevoEmail();
        await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email, activo: false);

        var resultado = await IngresarAsync(email, PruebasIdentidad.ClaveValida);

        Assert.True(resultado.IsNotAllowed);
    }

    [Fact]
    public async Task Un_usuario_sin_contrasena_no_puede_ingresar()
    {
        var email = PruebasIdentidad.NuevoEmail();
        using (var ambito = _sp.CreateScope())
        {
            var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var usuario = new Usuario
            {
                Email = email,
                EmailNormalizado = usuarios.NormalizeEmail(email),
                Nombre = "Sin",
                Apellido = "Clave",
                Activo = true,
                CreadoUtc = DateTime.UtcNow
            };
            Assert.True((await usuarios.CreateAsync(usuario)).Succeeded);
        }

        var resultado = await IngresarAsync(email, PruebasIdentidad.ClaveValida);

        Assert.False(resultado.Succeeded);
    }

    [Fact]
    public async Task La_clave_debe_cumplir_la_politica()
    {
        var email = PruebasIdentidad.NuevoEmail();
        using var ambito = _sp.CreateScope();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = new Usuario
        {
            Email = email,
            EmailNormalizado = usuarios.NormalizeEmail(email),
            Nombre = "Politica",
            Apellido = "Clave",
            Activo = true,
            CreadoUtc = DateTime.UtcNow
        };
        Assert.True((await usuarios.CreateAsync(usuario)).Succeeded);

        var corta = await usuarios.AddPasswordAsync(usuario, "corta1A");

        Assert.False(corta.Succeeded);
    }
}

using Microsoft.AspNetCore.Identity;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Web.Cuenta;

/// <summary>
/// Antes de cada request autenticada: fija el contexto (Estudio + usuario) desde la cookie
/// y comprueba que la membresía del Estudio siga activa. Si fue desactivada, cierra la sesión.
/// </summary>
public sealed class ContextoDeSesionMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(
        HttpContext http,
        ContextoEstudio contexto,
        ServicioSesion sesion,
        ServicioPermisos permisos,
        SignInManager<Infraestructura.Persistencia.Entidades.Usuario> firmas)
    {
        // Quien ya tiene sesión no ve el formulario de ingreso (evita enviarlo con un token ligado a otra identidad).
        if (http.User.Identity?.IsAuthenticated == true
            && HttpMethods.IsGet(http.Request.Method)
            && http.Request.Path.StartsWithSegments("/cuenta/ingresar"))
        {
            http.Response.Redirect("/");
            return;
        }

        if (SesionContexto.Aplicar(http.User, contexto))
        {
            var estudio = SesionContexto.EstudioId(http.User);
            var usuario = SesionContexto.UsuarioId(http.User)!.Value;
            if (estudio is int estudioId && !await sesion.MembresiaActivaAsync(usuario, estudioId))
            {
                await firmas.SignOutAsync();
                http.Response.Redirect("/cuenta/ingresar?error=sesion");
                return;
            }

            // Permisos cargados una vez por request, antes del render: el menú y las páginas los leen de caché.
            // Así no consultan el mismo DbContext en paralelo.
            if (estudio is not null)
            {
                await permisos.EfectivosAsync(http.RequestAborted);
            }
        }

        await siguiente(http);
    }
}

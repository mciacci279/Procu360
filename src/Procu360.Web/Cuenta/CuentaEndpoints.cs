using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Web.Cuenta;

/// <summary>
/// Acciones de cuenta por formulario (POST con antiforgery).
/// Son endpoints mínimos y no componentes: el inicio de sesión necesita escribir la cookie
/// antes de renderizar la respuesta.
/// </summary>
public static class CuentaEndpoints
{
    public static IEndpointRouteBuilder MapearCuenta(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/cuenta");

        // La validación antiforgery se hace en cada handler (ValidoAsync).
        grupo.MapPost("/ingresar", IngresarAsync);
        // Distinta de la página /cuenta/estudio para no coincidir con su ruta de Razor.
        grupo.MapPost("/elegir-estudio", ElegirEstudioAsync);
        grupo.MapPost("/salir", SalirAsync);
        grupo.MapPost("/invitacion", AceptarInvitacionAsync);

        return rutas;
    }

    /// <summary>Categoría del log técnico de cuenta (los intentos fallidos no van a seg.AuditoriaEvento: ver plan).</summary>
    private sealed class CuentaLog { }

    private static async Task<IResult> IngresarAsync(
        HttpContext http,
        [FromForm] string email,
        [FromForm] string clave,
        IAntiforgery antiforgery,
        UserManager<Usuario> usuarios,
        SignInManager<Usuario> firmas,
        ContextoEstudio contexto,
        ServicioSesion sesion,
        ILogger<CuentaLog> log)
    {
        if (!await ValidoAsync(http, antiforgery)) return Results.Redirect("/cuenta/ingresar?error=formulario");

        var emailLimpio = email?.Trim() ?? string.Empty;
        var usuario = await usuarios.FindByEmailAsync(emailLimpio);
        // Mismo mensaje para email inexistente y clave incorrecta: no se revela si la cuenta existe.
        if (usuario is null)
        {
            RegistrarFallo(log, http, "email inexistente", emailLimpio);
            return Results.Redirect("/cuenta/ingresar?error=credenciales");
        }

        var resultado = await firmas.CheckPasswordSignInAsync(usuario, clave ?? string.Empty, lockoutOnFailure: true);
        if (resultado.IsLockedOut)
        {
            RegistrarFallo(log, http, "cuenta bloqueada", emailLimpio);
            return Results.Redirect("/cuenta/ingresar?error=bloqueado");
        }
        if (!resultado.Succeeded)
        {
            RegistrarFallo(log, http, resultado.IsNotAllowed ? "cuenta desactivada" : "clave incorrecta", emailLimpio);
            return Results.Redirect("/cuenta/ingresar?error=credenciales");
        }

        contexto.EstablecerUsuario(usuario.Id);
        var membresias = await sesion.MembresiasActivasAsync(usuario.Id);

        if (membresias.Count == 1)
        {
            await firmas.SignInWithClaimsAsync(usuario, false, [new System.Security.Claims.Claim(ClaimesProcu.Estudio, membresias[0].EstudioId.ToString())]);
            return Results.Redirect("/");
        }

        // Sin membresías o con varias: sesión sin Estudio. El selector decide (o informa que no hay ninguno).
        await firmas.SignInWithClaimsAsync(usuario, false, Array.Empty<System.Security.Claims.Claim>());
        return Results.Redirect(membresias.Count == 0 ? "/cuenta/sin-estudio" : "/cuenta/estudio");
    }

    private static async Task<IResult> ElegirEstudioAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        UserManager<Usuario> usuarios,
        SignInManager<Usuario> firmas,
        ContextoEstudio contexto,
        ServicioSesion sesion)
    {
        // Sin parámetros [FromForm]: así la única validación antiforgery es la de ValidoAsync.
        if (!await ValidoAsync(http, antiforgery)) return Results.Redirect("/cuenta/estudio?error=formulario");
        var formulario = await http.Request.ReadFormAsync();
        if (!int.TryParse(formulario["estudioId"], out var estudioId)) return Results.Redirect("/cuenta/estudio?error=estudio");

        var usuarioId = SesionContexto.UsuarioId(http.User);
        if (usuarioId is null) return Results.Redirect("/cuenta/ingresar");

        contexto.Establecer(estudioId, usuarioId);
        if (!await sesion.MembresiaActivaAsync(usuarioId.Value, estudioId))
            return Results.Redirect("/cuenta/estudio?error=estudio");

        var usuario = await usuarios.FindByIdAsync(usuarioId.Value.ToString());
        if (usuario is null) return Results.Redirect("/cuenta/ingresar");

        await firmas.SignInWithClaimsAsync(usuario, false, [new System.Security.Claims.Claim(ClaimesProcu.Estudio, estudioId.ToString())]);
        return Results.Redirect("/");
    }

    private static async Task<IResult> SalirAsync(HttpContext http, IAntiforgery antiforgery, SignInManager<Usuario> firmas)
    {
        if (!await ValidoAsync(http, antiforgery)) return Results.Redirect("/");
        await firmas.SignOutAsync();
        return Results.Redirect("/cuenta/ingresar");
    }

    private static async Task<IResult> AceptarInvitacionAsync(
        HttpContext http,
        [FromForm] int usuarioId,
        [FromForm] string token,
        [FromForm] string? clave,
        IAntiforgery antiforgery,
        ContextoEstudio contexto,
        ServicioInvitaciones invitaciones)
    {
        if (!await ValidoAsync(http, antiforgery)) return Results.Redirect("/cuenta/ingresar?error=formulario");

        contexto.EstablecerUsuario(usuarioId);
        var resultado = await invitaciones.AceptarAsync(usuarioId, token ?? string.Empty, clave);
        if (!resultado.Exito)
        {
            var motivo = Uri.EscapeDataString(resultado.Errores.FirstOrDefault() ?? "No se pudo aceptar la invitación.");
            return Results.Redirect($"/cuenta/invitacion?u={usuarioId}&t={Uri.EscapeDataString(token ?? string.Empty)}&error={motivo}");
        }

        return Results.Redirect("/cuenta/ingresar?aceptada=1");
    }

    private static void RegistrarFallo(ILogger log, HttpContext http, string motivo, string email) =>
        log.LogWarning("Ingreso fallido ({Motivo}) para {Email} desde {Ip}",
            motivo, EnmascararEmail(email), http.Connection.RemoteIpAddress?.ToString() ?? "desconocida");

    /// <summary>"ana.perez@estudio.com" → "a***@estudio.com". Sin email completo en el log.</summary>
    private static string EnmascararEmail(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 0) return "***";
        return $"{email[0]}***{email[arroba..]}";
    }

    private static async Task<bool> ValidoAsync(HttpContext http, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(http);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}

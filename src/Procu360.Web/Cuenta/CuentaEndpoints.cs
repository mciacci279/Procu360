using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
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

        // La validación antiforgery se hace en cada handler (ValidoAsync). Los handlers NO declaran parámetros
        // [FromForm]: con ellos ASP.NET valida por su cuenta y un token inválido termina en un error 500
        // en lugar de una redirección con mensaje. El formulario se lee a mano con ReadFormAsync.
        // /ingresar e /invitacion comparten ruta con sus páginas Blazor, que también aceptan POST:
        // WithOrder(-1) le da prioridad al endpoint y evita la ambigüedad de rutas.
        grupo.MapPost("/ingresar", IngresarAsync).WithOrder(-1);
        // Distinta de la página /cuenta/estudio para no coincidir con su ruta de Razor.
        grupo.MapPost("/elegir-estudio", ElegirEstudioAsync);
        grupo.MapPost("/salir", SalirAsync);
        grupo.MapPost("/invitacion", AceptarInvitacionAsync).WithOrder(-1);

        return rutas;
    }

    /// <summary>Categoría del log técnico de cuenta (los intentos fallidos no van a seg.AuditoriaEvento: ver plan).</summary>
    private sealed class CuentaLog { }

    private static async Task<IResult> IngresarAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        UserManager<Usuario> usuarios,
        SignInManager<Usuario> firmas,
        ContextoEstudio contexto,
        ServicioSesion sesion,
        ILogger<CuentaLog> log)
    {
        if (!await ValidoAsync(http, antiforgery)) return Results.Redirect("/cuenta/ingresar?error=formulario");
        var formulario = await http.Request.ReadFormAsync();
        string? clave = formulario["clave"];

        var emailLimpio = formulario["email"].ToString().Trim();
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
        IAntiforgery antiforgery,
        SignInManager<Usuario> firmas,
        ContextoEstudio contexto,
        ServicioInvitaciones invitaciones)
    {
        var formulario = await http.Request.ReadFormAsync();
        if (!int.TryParse(formulario["usuarioId"], out var usuarioId)) return Results.Redirect("/cuenta/ingresar?error=formulario");
        var token = formulario["token"].ToString();
        string? clave = formulario["clave"];

        if (!await ValidoAsync(http, antiforgery))
        {
            // Caso típico: el enlace se abrió en un navegador con otra sesión iniciada y esa sesión cambió
            // (venció o se cerró) antes de enviar. Se cierra la sesión para que el reintento genere un
            // formulario sin usuario, que es el que corresponde a quien acepta una invitación.
            await CerrarSesionPreviaAsync(http, firmas);
            return RedirigirAInvitacion(usuarioId, token, "El formulario venció o cambió la sesión. Ingresá la contraseña de nuevo.");
        }

        contexto.EstablecerUsuario(usuarioId);
        var resultado = await invitaciones.AceptarAsync(usuarioId, token, clave);
        if (!resultado.Exito)
            return RedirigirAInvitacion(usuarioId, token, resultado.Errores.FirstOrDefault() ?? "No se pudo aceptar la invitación.");

        // Quien acepta entra luego con su propia cuenta: no queda abierta la sesión de otro usuario.
        await CerrarSesionPreviaAsync(http, firmas);
        return Results.Redirect("/cuenta/ingresar?aceptada=1");
    }

    private static IResult RedirigirAInvitacion(int usuarioId, string token, string error) =>
        Results.Redirect($"/cuenta/invitacion?u={usuarioId}&t={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(error)}");

    private static async Task CerrarSesionPreviaAsync(HttpContext http, SignInManager<Usuario> firmas)
    {
        if (http.User.Identity?.IsAuthenticated == true) await firmas.SignOutAsync();
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

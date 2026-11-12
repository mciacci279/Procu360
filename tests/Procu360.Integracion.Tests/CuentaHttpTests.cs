using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// Formularios de cuenta por HTTP real (WebApplicationFactory sobre Procu360_Test): un token antiforgery
/// inválido o de otra sesión debe terminar en una redirección con mensaje, nunca en un error 500.
/// </summary>
public partial class CuentaHttpTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int AdministradorEstudio1 = 2; // Laura Ferreyra (seed)
    private const string CookieAutenticacion = ".AspNetCore.Identity.Application";

    private readonly WebApplicationFactory<Program> _app;
    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public CuentaHttpTests()
    {
        // Pool normal: el límite de 1 conexión de las pruebas de servicios no sirve para requests concurrentes.
        var cadena = new SqlConnectionStringBuilder(ConfiguracionPruebas.CadenaAplicacionTest()) { MaxPoolSize = 100 }.ConnectionString;
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:ProcuAplicacion", cadena));
    }

    public void Dispose()
    {
        _app.Dispose();
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    [Fact]
    public async Task Invitacion_sin_token_antiforgery_redirige_a_la_invitacion_con_mensaje()
    {
        var navegador = new Navegador(_app);

        var respuesta = await navegador.PostAsync("/cuenta/invitacion",
            new() { ["usuarioId"] = "123", ["token"] = "abc", ["clave"] = PruebasIdentidad.ClaveValida });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var destino = respuesta.Headers.Location!.OriginalString;
        Assert.StartsWith("/cuenta/invitacion?u=123&t=abc&error=", destino);
    }

    [Fact]
    public async Task Ingresar_sin_token_antiforgery_redirige_con_error_de_formulario()
    {
        var navegador = new Navegador(_app);

        var respuesta = await navegador.PostAsync("/cuenta/ingresar",
            new() { ["email"] = "nadie@procu360.test", ["clave"] = "x" });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/cuenta/ingresar?error=formulario", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Invitacion_abierta_con_otra_sesion_que_luego_cambia_se_puede_reintentar_y_aceptar()
    {
        var (usuarioId, token) = await InvitarAsync();
        var navegador = new Navegador(_app);
        await IniciarSesionAsync(navegador, await UsuarioActivoAsync());

        // La página se carga con la sesión de otro usuario: el token queda ligado a esa sesión...
        var enlace = $"/cuenta/invitacion?u={usuarioId}&t={Uri.EscapeDataString(token)}";
        var tokenFormulario = await navegador.TokenDeFormularioAsync(enlace);
        Assert.Contains("Hay una sesión abierta", navegador.UltimoHtml);

        // ...y la sesión desaparece antes de enviar (venció o se cerró en otra pestaña).
        navegador.QuitarCookie(CookieAutenticacion);
        var fallida = await navegador.PostAsync("/cuenta/invitacion", Formulario(usuarioId, token, tokenFormulario));

        Assert.Equal(HttpStatusCode.Redirect, fallida.StatusCode);
        Assert.Contains("error=", fallida.Headers.Location!.OriginalString);

        // El reintento genera un formulario sin usuario y la invitación se acepta.
        var reintento = await navegador.TokenDeFormularioAsync(fallida.Headers.Location.OriginalString);
        var aceptada = await navegador.PostAsync("/cuenta/invitacion", Formulario(usuarioId, token, reintento));

        Assert.Equal(HttpStatusCode.Redirect, aceptada.StatusCode);
        Assert.Equal("/cuenta/ingresar?aceptada=1", aceptada.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Aceptar_una_invitacion_con_otra_sesion_abierta_la_cierra()
    {
        var (usuarioId, token) = await InvitarAsync();
        var navegador = new Navegador(_app);
        await IniciarSesionAsync(navegador, await UsuarioActivoAsync());

        var tokenFormulario = await navegador.TokenDeFormularioAsync($"/cuenta/invitacion?u={usuarioId}&t={Uri.EscapeDataString(token)}");
        var aceptada = await navegador.PostAsync("/cuenta/invitacion", Formulario(usuarioId, token, tokenFormulario));

        Assert.Equal("/cuenta/ingresar?aceptada=1", aceptada.Headers.Location!.OriginalString);
        Assert.False(navegador.TieneCookie(CookieAutenticacion));
    }

    private static Dictionary<string, string> Formulario(int usuarioId, string token, string tokenFormulario) => new()
    {
        ["__RequestVerificationToken"] = tokenFormulario,
        ["usuarioId"] = usuarioId.ToString(),
        ["token"] = token,
        ["clave"] = PruebasIdentidad.ClaveValida
    };

    /// <summary>Con los servicios de la app: el token del enlace se protege con sus claves de Data Protection.</summary>
    private async Task<(int UsuarioId, string Token)> InvitarAsync()
    {
        using var ambito = _app.Services.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(Estudio1, AdministradorEstudio1);
        var resultado = await ambito.ServiceProvider.GetRequiredService<ServicioInvitaciones>()
            .InvitarAsync(AdministradorEstudio1, new DatosInvitacion(PruebasIdentidad.NuevoEmail(), "Nuevo", "Integrante"));
        return (resultado.UsuarioId!.Value, resultado.Token!);
    }

    private async Task<string> UsuarioActivoAsync()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var id = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio1, id, EstadosMembresia.Activo);
        return email;
    }

    private static async Task IniciarSesionAsync(Navegador navegador, string email)
    {
        var token = await navegador.TokenDeFormularioAsync("/cuenta/ingresar");
        var respuesta = await navegador.PostAsync("/cuenta/ingresar", new()
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = email,
            ["clave"] = PruebasIdentidad.ClaveValida
        });
        Assert.Equal("/", respuesta.Headers.Location!.OriginalString);
        Assert.True(navegador.TieneCookie(CookieAutenticacion));
    }

    /// <summary>Cliente sin redirecciones automáticas y con cookies manejadas a mano, para poder quitarlas.</summary>
    private sealed partial class Navegador(WebApplicationFactory<Program> app)
    {
        private readonly HttpClient _cliente = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        private readonly Dictionary<string, string> _cookies = new();

        public string UltimoHtml { get; private set; } = string.Empty;

        public bool TieneCookie(string nombre) => _cookies.ContainsKey(nombre);

        public void QuitarCookie(string nombre) => _cookies.Remove(nombre);

        public async Task<string> TokenDeFormularioAsync(string ruta)
        {
            using var pedido = new HttpRequestMessage(HttpMethod.Get, ruta);
            var respuesta = await EnviarAsync(pedido);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            UltimoHtml = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
            return TokenRegex().Match(UltimoHtml) is { Success: true } m
                ? m.Groups[1].Value
                : throw new InvalidOperationException($"La página {ruta} no tiene token antiforgery.");
        }

        public async Task<HttpResponseMessage> PostAsync(string ruta, Dictionary<string, string> campos)
        {
            using var pedido = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(campos) };
            return await EnviarAsync(pedido);
        }

        private async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage pedido)
        {
            if (_cookies.Count > 0)
                pedido.Headers.Add("Cookie", string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}")));
            var respuesta = await _cliente.SendAsync(pedido);
            if (respuesta.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    var par = cookie.Split(';', 2)[0].Split('=', 2);
                    var borrada = string.IsNullOrEmpty(par[1]) || cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
                    if (borrada) _cookies.Remove(par[0]); else _cookies[par[0]] = par[1];
                }
            }
            return respuesta;
        }

        [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
        private static partial Regex TokenRegex();
    }
}

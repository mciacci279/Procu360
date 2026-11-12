using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Procu360.Infraestructura;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Seguridad;
using Procu360.Web.Components;
using Procu360.Web.Cuenta;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("ProcuAplicacion")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:ProcuAplicacion (ver user-secrets).");

builder.Services.AgregarPersistenciaProcu360(cadenaConexion);
builder.Services.AgregarIdentidadProcu360();
builder.Services.AgregarSeguridadProcu360();

// Claves de protección persistidas con nombre de aplicación fijo: los enlaces de invitación y las cookies
// validan aunque la app se reinicie o los tokens los haya creado otro proceso.
builder.Services.AddDataProtection()
    .SetApplicationName("Procu360")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Procu360", "claves")));

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/cuenta/ingresar";
    opciones.AccessDeniedPath = "/cuenta/ingresar";
    opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
    opciones.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Semilla de desarrollo: asigna la contraseña común (user-secrets) a los usuarios de seed sin clave.
if (args.Contains("--seed-desarrollo"))
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("La semilla de desarrollo solo corre con ASPNETCORE_ENVIRONMENT=Development.");
        return;
    }

    var clave = app.Configuration["Seed:PasswordDesarrollo"]
        ?? throw new InvalidOperationException("Falta Seed:PasswordDesarrollo (ver user-secrets).");

    using var ambito = app.Services.CreateScope();
    var asignadas = await ambito.ServiceProvider.GetRequiredService<ServicioSemillaDesarrollo>()
        .AsignarContrasenaAsync(clave);
    Console.WriteLine($"Contraseña de desarrollo asignada a {asignadas} usuario(s).");
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
// El contexto del Estudio va ANTES de la autorización: las políticas de permiso dependen de él.
app.UseMiddleware<ContextoDeSesionMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();
app.MapearCuenta();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>Visible para las pruebas HTTP (WebApplicationFactory).</summary>
public partial class Program;

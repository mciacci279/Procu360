using System.Security.Claims;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Identidad;

public static class ClaimesProcu
{
    /// <summary>Estudio activo elegido en la sesión.</summary>
    public const string Estudio = "procu360:estudio";
}

public static class SesionContexto
{
    /// <summary>
    /// Copia la identidad de la sesión al contexto de la unidad de trabajo.
    /// Devuelve false si no hay usuario autenticado.
    /// </summary>
    public static bool Aplicar(ClaimsPrincipal principal, ContextoEstudio contexto)
    {
        var usuario = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || !int.TryParse(usuario, out var usuarioId))
        {
            return false;
        }

        var estudio = principal.FindFirstValue(ClaimesProcu.Estudio);
        if (int.TryParse(estudio, out var estudioId))
        {
            contexto.Establecer(estudioId, usuarioId);
        }
        else
        {
            contexto.EstablecerUsuario(usuarioId);
        }
        return true;
    }

    public static int? UsuarioId(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static int? EstudioId(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimesProcu.Estudio), out var id) ? id : null;
}

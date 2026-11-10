namespace Procu360.Dominio.Seguridad;

/// <summary>Validación de roles personalizados y de reglas de administración (sin base de datos).</summary>
public static class ReglasRol
{
    public static IReadOnlyList<string> ValidarRol(string nombre, IEnumerable<string> permisos)
    {
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add("El rol necesita un nombre.");
        else if (nombre.Trim().Length > 50)
            errores.Add("El nombre del rol no puede superar 50 caracteres.");

        var conteo = permisos.Distinct(StringComparer.Ordinal).Count();
        if (conteo == 0)
            errores.Add("El rol necesita al menos un permiso.");

        var catalogo = new HashSet<string>(Permisos.Catalogo(), StringComparer.Ordinal);
        var desconocidos = permisos.Where(p => !catalogo.Contains(p)).Distinct().ToList();
        if (desconocidos.Count > 0)
            errores.Add($"Permisos inexistentes: {string.Join(", ", desconocidos)}.");

        return errores;
    }

    /// <summary>
    /// No se puede quitar el último administrador activo del Estudio. Un administrador es un rol
    /// con usuarios.administrar. Devuelve false si la operación dejaría al Estudio sin administradores.
    /// </summary>
    public static bool PuedeQuitarAdministrador(int administradoresActivosAntes)
        => administradoresActivosAntes > 1;

    /// <summary>
    /// Para cambiar roles hace falta usuarios.administrar. Además, asignar un rol con un permiso
    /// que el actor no tiene impide escalar privilegios.
    /// </summary>
    public static bool PuedeAsignarRol(PermisosEfectivos actor, IEnumerable<string> permisosDelRol)
        => actor.Tiene(Permisos.UsuariosAdministrar) && permisosDelRol.All(actor.Tiene);
}

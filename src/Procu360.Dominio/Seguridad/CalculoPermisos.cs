namespace Procu360.Dominio.Seguridad;

/// <summary>Rol asignado a una membresía, con su matriz y el comitente si el ámbito lo requiere.</summary>
public sealed record RolAsignado(
    string Nombre,
    Ambito Ambito,
    IReadOnlyCollection<string> Permisos,
    int? ComitenteId);

/// <summary>Ámbito de datos de un rol (seg.Rol.Ambito).</summary>
public enum Ambito
{
    Estudio = 1,
    Comitente = 2,
    Asignados = 3
}

/// <summary>
/// Alcance de datos efectivo: unión de los ámbitos de todos los roles de la membresía.
/// TodoEstudio domina: si algún rol da ámbito de Estudio, no hacen falta los demás.
/// </summary>
public sealed record AlcanceDatos(bool TodoEstudio, IReadOnlySet<int> Comitentes, bool Asignados)
{
    public bool Permite(int comitenteId) => TodoEstudio || Comitentes.Contains(comitenteId);
}

public sealed record PermisosEfectivos(IReadOnlySet<string> Permisos, AlcanceDatos Alcance)
{
    public bool Tiene(string permiso) => Permisos.Contains(permiso);
}

/// <summary>Cálculo puro de permisos y alcance a partir de los roles de una membresía activa.</summary>
public static class CalculoPermisos
{
    public static PermisosEfectivos Calcular(IEnumerable<RolAsignado> roles)
    {
        var permisos = new HashSet<string>(StringComparer.Ordinal);
        var comitentes = new HashSet<int>();
        var todoEstudio = false;
        var asignados = false;

        foreach (var rol in roles)
        {
            foreach (var permiso in rol.Permisos)
            {
                permisos.Add(permiso);
            }

            switch (rol.Ambito)
            {
                case Ambito.Estudio:
                    todoEstudio = true;
                    break;
                case Ambito.Comitente when rol.ComitenteId is int comitente:
                    comitentes.Add(comitente);
                    break;
                case Ambito.Asignados:
                    asignados = true;
                    break;
            }
        }

        return new PermisosEfectivos(permisos, new AlcanceDatos(todoEstudio, comitentes, asignados));
    }
}

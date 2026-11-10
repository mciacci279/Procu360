using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Global, definido por el código de la aplicación (modulo.accion). Reemplaza el esquema de menús del WinForms (MenuSistema/MenuPerfil), que se descarta. 
/// </summary>
public partial class Permiso
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Modulo { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<RolPermiso> RolPermiso { get; set; } = new List<RolPermiso>();
}

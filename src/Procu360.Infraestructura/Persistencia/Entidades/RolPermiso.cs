using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

public partial class RolPermiso
{
    public int EstudioId { get; set; }

    public int RolId { get; set; }

    public int PermisoId { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Permiso Permiso { get; set; } = null!;

    public virtual Rol Rol { get; set; } = null!;
}

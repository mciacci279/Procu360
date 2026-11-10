using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

public partial class EstudioUsuarioRol
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int EstudioUsuarioId { get; set; }

    public int RolId { get; set; }

    public int? ComitenteId { get; set; }

    public virtual Comitente? Comitente { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual EstudioUsuario EstudioUsuario { get; set; } = null!;

    public virtual Rol Rol { get; set; } = null!;
}

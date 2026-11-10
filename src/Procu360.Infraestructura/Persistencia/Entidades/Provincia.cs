using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Provincia.
/// </summary>
public partial class Provincia
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Codigo { get; set; }

    public Guid? LegadoId { get; set; }

    public virtual ICollection<Feriado> Feriado { get; set; } = new List<Feriado>();

    public virtual ICollection<Localidad> Localidad { get; set; } = new List<Localidad>();
}

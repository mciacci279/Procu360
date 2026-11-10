using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: TipoDoc.
/// </summary>
public partial class TipoDocumento
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public Guid? LegadoId { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();
}

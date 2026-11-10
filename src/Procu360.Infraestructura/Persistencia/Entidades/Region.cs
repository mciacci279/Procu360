using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Expediente.Region (valores distintos).
/// </summary>
public partial class Region
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();
}

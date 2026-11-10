using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: CodigoDevolucion.
/// </summary>
public partial class CodigoDevolucion
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<DevolucionCorreo> DevolucionCorreo { get; set; } = new List<DevolucionCorreo>();

    public virtual Estudio Estudio { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Expediente.NroCampania/NombreCampania, ExpedienteCodigoDevolucion.Campania.
/// </summary>
public partial class Campania
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Numero { get; set; } = null!;

    public string? Nombre { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<DevolucionCorreo> DevolucionCorreo { get; set; } = new List<DevolucionCorreo>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();
}

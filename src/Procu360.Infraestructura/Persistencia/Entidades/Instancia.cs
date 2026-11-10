using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Macroestado del expediente (pre judicial, judicial, con sentencia, archivado…). Origen Monitorio: Instancia.
/// </summary>
public partial class Instancia
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Color { get; set; }

    public bool Perime { get; set; }

    public bool EsBaja { get; set; }

    public int? Orden { get; set; }

    public bool Activa { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();

    public virtual ICollection<InstanciaExpediente> InstanciaExpediente { get; set; } = new List<InstanciaExpediente>();

    public virtual ICollection<PlantillaEscrito> PlantillaEscrito { get; set; } = new List<PlantillaEscrito>();
}

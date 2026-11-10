using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Registro.
/// </summary>
public partial class RegistroPublico
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Domicilio { get; set; }

    public string? Zona { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<Embargo> Embargo { get; set; } = new List<Embargo>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();
}

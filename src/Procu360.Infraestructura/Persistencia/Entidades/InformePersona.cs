using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Informe.
/// </summary>
public partial class InformePersona
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long PersonaId { get; set; }

    public DateOnly Fecha { get; set; }

    public string Texto { get; set; } = null!;

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Persona Persona { get; set; } = null!;
}

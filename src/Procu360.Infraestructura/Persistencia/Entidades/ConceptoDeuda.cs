using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Impuesto/tasa reclamada. TextoEscrito admite variables {{...}}. Origen Monitorio: Impuesto.
/// </summary>
public partial class ConceptoDeuda
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int? ComitenteId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Item { get; set; }

    public int? PeriodosDiferenciados { get; set; }

    public bool ConsideraPeriodos { get; set; }

    public bool PeriodosFormatoDGR { get; set; }

    public string? TextoEscrito { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Comitente? Comitente { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();
}

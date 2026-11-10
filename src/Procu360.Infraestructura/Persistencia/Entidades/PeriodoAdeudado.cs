using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Reemplaza también al texto Expediente.Periodos, que se arma a partir de esta tabla. Origen Monitorio: ExpedientePeriodo.
/// </summary>
public partial class PeriodoAdeudado
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public short Anio { get; set; }

    public string? Cuota { get; set; }

    public string? Concepto { get; set; }

    public DateOnly? Vencimiento { get; set; }

    public decimal Tributo { get; set; }

    public decimal Recargo { get; set; }

    public string? Observaciones { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual ICollection<LiquidacionLinea> LiquidacionLinea { get; set; } = new List<LiquidacionLinea>();
}

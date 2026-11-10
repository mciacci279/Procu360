using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

public partial class LiquidacionLinea
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long LiquidacionId { get; set; }

    public int Orden { get; set; }

    public int? RubroPlanillaId { get; set; }

    public int? ItemPlanillaId { get; set; }

    public string Concepto { get; set; } = null!;

    public long? PeriodoAdeudadoId { get; set; }

    public decimal? Base { get; set; }

    public decimal? Tasa { get; set; }

    public int? Dias { get; set; }

    public decimal Importe { get; set; }

    public bool EsManual { get; set; }

    public string? Detalle { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ItemPlanilla? ItemPlanilla { get; set; }

    public virtual Liquidacion Liquidacion { get; set; } = null!;

    public virtual PeriodoAdeudado? PeriodoAdeudado { get; set; }

    public virtual RubroPlanilla? RubroPlanilla { get; set; }
}

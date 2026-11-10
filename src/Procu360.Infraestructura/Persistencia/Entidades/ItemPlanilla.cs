using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// TipoCalculo reemplaza las 9 columnas Es*: 1 monto fijo, 2 porcentaje del capital, 3 capital, 4 interés simple, 5 tasa pasiva, 6 interés sobre capital, 7 honorarios, 8 tasa de justicia, 9 aportes, 10 gastos de diligencias, 11 subtotal. Origen Monitorio: ItemsPlanilla.
/// </summary>
public partial class ItemPlanilla
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int RubroPlanillaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public byte TipoCalculo { get; set; }

    public decimal? Valor { get; set; }

    public bool EsPorcentaje { get; set; }

    public decimal? MontoMinimo { get; set; }

    public bool CargaManual { get; set; }

    public bool EsSubtotal { get; set; }

    public bool EsDefault { get; set; }

    public int? OrdenDefault { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<ItemPlanillaTope> ItemPlanillaTope { get; set; } = new List<ItemPlanillaTope>();

    public virtual ICollection<LiquidacionLinea> LiquidacionLinea { get; set; } = new List<LiquidacionLinea>();

    public virtual ICollection<PlanillaJuzgado> PlanillaJuzgado { get; set; } = new List<PlanillaJuzgado>();

    public virtual RubroPlanilla RubroPlanilla { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: ItemsMontoMinMax (IemsMontoMinMax se descarta tras comparar contenido).
/// </summary>
public partial class ItemPlanillaTope
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int ItemPlanillaId { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public decimal? Minimo { get; set; }

    public decimal? Maximo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ItemPlanilla ItemPlanilla { get; set; } = null!;
}

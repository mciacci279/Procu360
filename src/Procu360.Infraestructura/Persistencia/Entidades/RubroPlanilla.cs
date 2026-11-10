using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: RubroPlanilla.
/// </summary>
public partial class RubroPlanilla
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? DescripcionTotal { get; set; }

    public int Orden { get; set; }

    public int? PadreId { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<RubroPlanilla> InverseRubroPlanillaNavigation { get; set; } = new List<RubroPlanilla>();

    public virtual ICollection<ItemPlanilla> ItemPlanilla { get; set; } = new List<ItemPlanilla>();

    public virtual ICollection<LiquidacionLinea> LiquidacionLinea { get; set; } = new List<LiquidacionLinea>();

    public virtual RubroPlanilla? RubroPlanillaNavigation { get; set; }
}

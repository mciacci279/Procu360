using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: JuzgadoItems.
/// </summary>
public partial class PlanillaJuzgado
{
    public int EstudioId { get; set; }

    public int JuzgadoId { get; set; }

    public int ItemPlanillaId { get; set; }

    public int Orden { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ItemPlanilla ItemPlanilla { get; set; } = null!;

    public virtual Juzgado Juzgado { get; set; } = null!;
}

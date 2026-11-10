using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: ExpedienteActividad + Expediente.ActividadIIBB/CodigoActividadIIBB.
/// </summary>
public partial class ExpedienteActividad
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int ActividadEconomicaId { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ActividadEconomica ActividadEconomica { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;
}

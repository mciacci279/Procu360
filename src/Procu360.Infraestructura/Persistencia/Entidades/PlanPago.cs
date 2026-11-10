using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Estado es texto libre, como EstadoPlan en Monitorio. Activo indica el plan vigente del expediente. Origen Monitorio: ExpedientePlanPago (+ Expediente.NroPlanPagos).
/// </summary>
public partial class PlanPago
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public string? NroPlan { get; set; }

    public DateOnly? FechaPlan { get; set; }

    public string? PeriodosIncluidos { get; set; }

    public int? CantCuotas { get; set; }

    public int? CuotasPagadas { get; set; }

    public int? CuotasVencidasImpagas { get; set; }

    public int? CuotasAVencer { get; set; }

    public DateOnly? FechaCaducidad { get; set; }

    public string? Estado { get; set; }

    public DateOnly? FechaConsulta { get; set; }

    public string? Decreto { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;
}

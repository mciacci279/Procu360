using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Historial de etapas de ambos flujos. Estado 1 alta, 2 cumplimentada, 3 suspendida, 4 baja (en el legado 0 = alta). FechaVencimiento se calcula con días hábiles y feriados. Origen Monitorio: EtapaExpediente + CautelarExpediente.
/// </summary>
public partial class EtapaExpediente
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int EtapaId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public byte Estado { get; set; }

    public decimal? Monto { get; set; }

    public string? Observaciones { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Etapa Etapa { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual ICollection<Tarea> Tarea { get; set; } = new List<Tarea>();
}

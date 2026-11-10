using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Estado 1 pendiente, 2 cumplida, 3 archivada, 4 anulada. Origen 1 manual, 2 generada por etapa, 3 desde novedad, 4 migrada de Agenda. Origen Monitorio: ActividadAdministrativaExpediente + Agenda.
/// </summary>
public partial class Tarea
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long? ExpedienteId { get; set; }

    public int? TipoTareaId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Lugar { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public TimeOnly? HoraInicio { get; set; }

    public TimeOnly? HoraFin { get; set; }

    public bool TodoElDia { get; set; }

    public bool Alerta { get; set; }

    public DateOnly? RepetirHasta { get; set; }

    public int? ResponsableUsuarioId { get; set; }

    public bool VisibleParaTodos { get; set; }

    public byte Estado { get; set; }

    public DateTime? CumplidaUtc { get; set; }

    public byte Origen { get; set; }

    public long? EtapaExpedienteId { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual EtapaExpediente? EtapaExpediente { get; set; }

    public virtual Expediente? Expediente { get; set; }

    public virtual ICollection<NovedadJudicial> NovedadJudicial { get; set; } = new List<NovedadJudicial>();

    public virtual Usuario? ResponsableUsuario { get; set; }

    public virtual ICollection<TareaParticipante> TareaParticipante { get; set; } = new List<TareaParticipante>();

    public virtual TipoTarea? TipoTarea { get; set; }
}

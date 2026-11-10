using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Proveídos, decretos y partes. Fuente 1 manual, 2 importación de archivo, 3 migrada. ExpedienteId NULL = pendiente de vincular. Origen Monitorio: ExpedienteParte.
/// </summary>
public partial class NovedadJudicial
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long? ExpedienteId { get; set; }

    public string? NroExpedienteInformado { get; set; }

    public DateOnly Fecha { get; set; }

    public string Texto { get; set; } = null!;

    public byte Fuente { get; set; }

    public bool Leida { get; set; }

    public DateTime? LeidaUtc { get; set; }

    public int? LeidaPor { get; set; }

    public long? TareaId { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente? Expediente { get; set; }

    public virtual Usuario? LeidaPorNavigation { get; set; }

    public virtual Tarea? Tarea { get; set; }
}

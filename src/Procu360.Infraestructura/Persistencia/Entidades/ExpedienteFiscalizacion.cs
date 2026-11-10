using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// 1:1 opcional. Solo para expedientes de fiscalización (EsFisca). Origen Monitorio: Expediente.Fisca*.
/// </summary>
public partial class ExpedienteFiscalizacion
{
    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int? TipoFisca { get; set; }

    public int? Tipo { get; set; }

    public string? NroExpediente { get; set; }

    public string? NroOrden { get; set; }

    public string? NroResolucion { get; set; }

    public string? NroRecursoReconsideracion { get; set; }

    public DateOnly? ResolFechaResolucion { get; set; }

    public DateOnly? ResolFechaNotificacion { get; set; }

    public DateOnly? ReconFechaResolucion { get; set; }

    public DateOnly? ReconFechaNotificacion { get; set; }

    public DateOnly? ContenFechaDemanda { get; set; }

    public DateOnly? ContenFechaApelacion { get; set; }

    public DateOnly? ContenFechaDemandaContenciosa { get; set; }

    public DateOnly? FechaReconocimientoDeuda { get; set; }

    public bool ExtiendeResponsabilidad { get; set; }

    public string? ResponsableNombre { get; set; }

    public string? ResponsableDomicilio { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;
}

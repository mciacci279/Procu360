using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: ExpedienteCodigoDevolucion.
/// </summary>
public partial class DevolucionCorreo
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int CodigoDevolucionId { get; set; }

    public DateOnly Fecha { get; set; }

    public int? CampaniaId { get; set; }

    public string? NroCaso { get; set; }

    public int? UsuarioId { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Campania? Campania { get; set; }

    public virtual CodigoDevolucion CodigoDevolucion { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }
}

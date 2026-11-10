using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Rol 1 demandado, 2 codemandado, 3 responsable solidario. Sin límite de codemandados. Origen Monitorio: ExpedientePersona (+ Expediente.PersonaID).
/// </summary>
public partial class ExpedientePersona
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public long PersonaId { get; set; }

    public byte Rol { get; set; }

    public byte Posicion { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual Persona Persona { get; set; } = null!;
}

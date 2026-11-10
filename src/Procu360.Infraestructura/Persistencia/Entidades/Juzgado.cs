using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Organización judicial por Estudio; al crear un Estudio puede copiarse de otro. Origen Monitorio: Juzgado (sin ItaxID ni MuniCordobaID).
/// </summary>
public partial class Juzgado
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Secretaria { get; set; }

    public string? Juez { get; set; }

    public string? Ubicacion { get; set; }

    public string? Telefono { get; set; }

    public bool EsCapital { get; set; }

    public int? LocalidadId { get; set; }

    public string? DomicilioParaEscritos { get; set; }

    public int? Orden { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();

    public virtual ICollection<Liquidacion> Liquidacion { get; set; } = new List<Liquidacion>();

    public virtual Localidad? Localidad { get; set; }

    public virtual ICollection<PlanillaJuzgado> PlanillaJuzgado { get; set; } = new List<PlanillaJuzgado>();
}

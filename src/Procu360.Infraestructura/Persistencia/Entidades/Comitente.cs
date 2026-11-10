using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Acreedor/organismo para el que se procura (municipio, DGR, ente). Las fórmulas de liquidación se parametrizan por comitente. Origen Monitorio: DomicilioDGR.
/// </summary>
public partial class Comitente
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Cuit { get; set; }

    public string? Domicilio { get; set; }

    public string? CodigoPostal { get; set; }

    public int? LocalidadId { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<ConceptoDeuda> ConceptoDeuda { get; set; } = new List<ConceptoDeuda>();

    public virtual ICollection<EscalaHonorarios> EscalaHonorarios { get; set; } = new List<EscalaHonorarios>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<EstudioUsuarioRol> EstudioUsuarioRol { get; set; } = new List<EstudioUsuarioRol>();

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();

    public virtual Localidad? Localidad { get; set; }

    public virtual ICollection<ParametroLiquidacion> ParametroLiquidacion { get; set; } = new List<ParametroLiquidacion>();

    public virtual ICollection<TasaInteres> TasaInteres { get; set; } = new List<TasaInteres>();
}

using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Persona.
/// </summary>
public partial class Persona
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public bool EsPersonaFisica { get; set; }

    public string? Apellido { get; set; }

    public string? Nombre { get; set; }

    public string? RazonSocial { get; set; }

    public int? TipoDocumentoId { get; set; }

    public string? NroDocumento { get; set; }

    public string? Cuit { get; set; }

    public string? Sexo { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? Telefono1 { get; set; }

    public string? Telefono2 { get; set; }

    public string? Email { get; set; }

    public string? Observaciones { get; set; }

    public Guid? LegadoId { get; set; }

    public bool Anulado { get; set; }

    public DateTime? AnuladoUtc { get; set; }

    public int? AnuladoPor { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Domicilio? Domicilio { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<ExpedientePersona> ExpedientePersona { get; set; } = new List<ExpedientePersona>();

    public virtual ICollection<InformePersona> InformePersona { get; set; } = new List<InformePersona>();

    public virtual TipoDocumento? TipoDocumento { get; set; }
}

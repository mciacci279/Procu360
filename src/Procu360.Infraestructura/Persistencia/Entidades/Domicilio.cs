using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Tipo 1 real, 2 fiscal, 3 constituido, 4 postal. Un solo principal por persona (índice filtrado). Origen Monitorio: Domicilio.
/// </summary>
public partial class Domicilio
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long PersonaId { get; set; }

    public byte Tipo { get; set; }

    public string? Calle { get; set; }

    public string? Numero { get; set; }

    public string? Piso { get; set; }

    public string? Depto { get; set; }

    public string? Barrio { get; set; }

    public string? CodigoPostal { get; set; }

    public int? LocalidadId { get; set; }

    public bool Principal { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Localidad? Localidad { get; set; }

    public virtual Persona Persona { get; set; } = null!;
}

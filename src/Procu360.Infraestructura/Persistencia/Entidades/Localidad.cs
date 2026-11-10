using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Localidad.
/// </summary>
public partial class Localidad
{
    public int Id { get; set; }

    public int ProvinciaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? CodigoPostal { get; set; }

    public Guid? LegadoId { get; set; }

    public virtual ICollection<Comitente> Comitente { get; set; } = new List<Comitente>();

    public virtual ICollection<Domicilio> Domicilio { get; set; } = new List<Domicilio>();

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();

    public virtual ICollection<Juzgado> Juzgado { get; set; } = new List<Juzgado>();

    public virtual ICollection<Profesional> Profesional { get; set; } = new List<Profesional>();

    public virtual Provincia Provincia { get; set; } = null!;
}

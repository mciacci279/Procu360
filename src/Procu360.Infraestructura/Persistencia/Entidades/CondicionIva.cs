using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: Iva.
/// </summary>
public partial class CondicionIva
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Porcentaje { get; set; }

    public string? LetraComprobante { get; set; }

    public bool EsResponsableInscripto { get; set; }

    public bool EsNoResponsable { get; set; }

    public bool EsExento { get; set; }

    public bool EsMonotributo { get; set; }

    public bool EsConsumidorFinal { get; set; }

    public Guid? LegadoId { get; set; }

    public virtual ICollection<Profesional> Profesional { get; set; } = new List<Profesional>();
}

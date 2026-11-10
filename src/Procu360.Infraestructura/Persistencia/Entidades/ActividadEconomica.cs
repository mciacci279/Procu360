using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Nomenclador público de actividades (IIBB). Origen Monitorio: Actividad.
/// </summary>
public partial class ActividadEconomica
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public Guid? LegadoId { get; set; }

    public virtual ICollection<ExpedienteActividad> ExpedienteActividad { get; set; } = new List<ExpedienteActividad>();
}

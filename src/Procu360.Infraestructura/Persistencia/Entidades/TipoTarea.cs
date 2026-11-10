using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: ActividadAdministrativa.
/// </summary>
public partial class TipoTarea
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public int? Dias { get; set; }

    public bool DiasHabiles { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<Tarea> Tarea { get; set; } = new List<Tarea>();
}

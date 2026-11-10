using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Cola de la pantalla Datos a revisar. Estado 1 pendiente, 2 resuelto, 3 descartado. 
/// </summary>
public partial class DatoARevisar
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Entidad { get; set; } = null!;

    public long? EntidadId { get; set; }

    public string? TablaOrigen { get; set; }

    public Guid? ClaveOrigen { get; set; }

    public string? Campo { get; set; }

    public string? ValorOriginal { get; set; }

    public string? ValorPropuesto { get; set; }

    public byte Estado { get; set; }

    public int? ResueltoPor { get; set; }

    public DateTime? ResueltoUtc { get; set; }

    public string? Comentario { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Usuario? ResueltoPorNavigation { get; set; }
}

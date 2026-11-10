using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Asuetos propios del Estudio o de su circunscripción. 
/// </summary>
public partial class FeriadoEstudio
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public DateOnly Fecha { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;
}

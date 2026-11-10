using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Tramos de la escala (9 % … 3 % en el legado, fijos en código). 
/// </summary>
public partial class EscalaHonorarios
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public int? ComitenteId { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public decimal BaseDesde { get; set; }

    public decimal? BaseHasta { get; set; }

    public decimal Porcentaje { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Comitente? Comitente { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Periodicidad 1 diaria, 2 mensual, 3 anual. Valor en tanto por uno (0.000830 = 0,083 %). Reemplaza 0,083 % y 0,1 % diarios fijos en código. 
/// </summary>
public partial class TasaInteres
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public int? ComitenteId { get; set; }

    public string Codigo { get; set; } = null!;

    public byte Periodicidad { get; set; }

    public decimal Valor { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Comitente? Comitente { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;
}

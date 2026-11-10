using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Parámetros con vigencia. ComitenteId NULL = valor general del Estudio; con ComitenteId = particular del comitente (tiene prioridad). Origen Monitorio: UnidadPlanilla + valores de App.config + constantes en frmPlanilla.cs.
/// </summary>
public partial class ParametroLiquidacion
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public int? ComitenteId { get; set; }

    public string Codigo { get; set; } = null!;

    public decimal? Valor { get; set; }

    public DateOnly? ValorFecha { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Comitente? Comitente { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;
}

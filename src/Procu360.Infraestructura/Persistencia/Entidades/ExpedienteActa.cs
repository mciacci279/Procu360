using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// 1:1 opcional. Actas de infracción (multas). Origen Monitorio: Expediente (FechaLabrado…FechaTramite).
/// </summary>
public partial class ExpedienteActa
{
    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public DateOnly? FechaLabrado { get; set; }

    public string? HoraLabrado { get; set; }

    public string? LugarLabrado { get; set; }

    public string? TipoYNroLicencia { get; set; }

    public string? DescripcionDominio { get; set; }

    public string? NroCaso { get; set; }

    public string? JuzgadoFaltas { get; set; }

    public string? NroTramite { get; set; }

    public DateOnly? FechaTramite { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;
}

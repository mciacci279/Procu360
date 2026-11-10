using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Nueva: el legado no guardaba planillas. Estado 1 borrador, 2 guardada, 3 presentada, 4 anulada. ParametrosJson copia los valores usados para que sea reproducible. 
/// </summary>
public partial class Liquidacion
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int NroVersion { get; set; }

    public DateOnly FechaCalculo { get; set; }

    public byte Estado { get; set; }

    public int? JuzgadoId { get; set; }

    public decimal TotalCapital { get; set; }

    public decimal TotalInteres { get; set; }

    public decimal TotalGastos { get; set; }

    public decimal? HonorariosJus { get; set; }

    public decimal HonorariosPesos { get; set; }

    public decimal Total { get; set; }

    public string ParametrosJson { get; set; } = null!;

    public long? DocumentoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Documento? Documento { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual Juzgado? Juzgado { get; set; }

    public virtual ICollection<LiquidacionLinea> LiquidacionLinea { get; set; } = new List<LiquidacionLinea>();
}

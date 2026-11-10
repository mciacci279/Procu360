using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Estado 1 solicitado, 2 trabado, 3 levantado, 4 sin efecto (nuevo: el legado no tenía estado; se migra como 2). DetalleJson guarda los datos propios de cada clase (marca, modelo, chasis, sticker; lote, manzana, PH; banco, CBU; etc.). Origen Monitorio: EmbargoExpediente.
/// </summary>
public partial class Embargo
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int TipoEmbargoId { get; set; }

    public byte Estado { get; set; }

    public DateOnly? FechaSolicitud { get; set; }

    public DateOnly? FechaTraba { get; set; }

    public DateOnly? FechaLevantamiento { get; set; }

    public decimal? Monto { get; set; }

    public decimal? Porcentaje { get; set; }

    public int? RegistroPublicoId { get; set; }

    public string? NroRegistro { get; set; }

    public string? DireccionRegistro { get; set; }

    public string? Secretaria { get; set; }

    public string? Autorizados { get; set; }

    public string? Dominio { get; set; }

    public string? Nomenclatura { get; set; }

    public int? MartilleroId { get; set; }

    public string? DetalleJson { get; set; }

    public string? Observaciones { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual Profesional? Profesional { get; set; }

    public virtual RegistroPublico? RegistroPublico { get; set; }

    public virtual TipoEmbargo TipoEmbargo { get; set; } = null!;
}

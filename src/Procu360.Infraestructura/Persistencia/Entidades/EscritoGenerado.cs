using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Estado 1 generado, 2 presentado, 3 anulado. Los migrados no tienen documento (el legado no lo guardaba). Origen Monitorio: ExpedienteEtapaEscrito + ExpedienteInstanciaEscrito.
/// </summary>
public partial class EscritoGenerado
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long ExpedienteId { get; set; }

    public int? PlantillaEscritoVersionId { get; set; }

    public int? EtapaId { get; set; }

    public int? InstanciaId { get; set; }

    public long? TrabajoProcesoId { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaUtc { get; set; }

    public long? DocumentoId { get; set; }

    public byte Estado { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Documento? Documento { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Etapa? Etapa { get; set; }

    public virtual Expediente Expediente { get; set; } = null!;

    public virtual Instancia? Instancia { get; set; }

    public virtual PlantillaEscritoVersion? PlantillaEscritoVersion { get; set; }

    public virtual TrabajoProceso? TrabajoProceso { get; set; }

    public virtual Usuario? Usuario { get; set; }
}

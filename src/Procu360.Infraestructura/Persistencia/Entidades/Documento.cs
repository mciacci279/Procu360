using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Tipo 1 adjunto, 2 escrito generado, 3 liquidación exportada, 4 plantilla, 5 importación. Contenido en almacenamiento de archivos con prefijo de Estudio; sin borrado automático: se conserva hasta que un usuario con permiso lo elimina. 
/// </summary>
public partial class Documento
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public long? ExpedienteId { get; set; }

    public byte Tipo { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long TamanioBytes { get; set; }

    public string Sha256 { get; set; } = null!;

    public string RutaAlmacenamiento { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Eliminado { get; set; }

    public DateTime? EliminadoUtc { get; set; }

    public int? EliminadoPor { get; set; }

    public string? MotivoEliminacion { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Usuario? EliminadoPorNavigation { get; set; }

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Expediente? Expediente { get; set; }

    public virtual ICollection<Liquidacion> Liquidacion { get; set; } = new List<Liquidacion>();

    public virtual ICollection<PlantillaEscritoVersion> PlantillaEscritoVersion { get; set; } = new List<PlantillaEscritoVersion>();
}

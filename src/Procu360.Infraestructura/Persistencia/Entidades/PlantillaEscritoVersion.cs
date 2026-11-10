using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

public partial class PlantillaEscritoVersion
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int PlantillaEscritoId { get; set; }

    public int NroVersion { get; set; }

    public long DocumentoId { get; set; }

    public string? Notas { get; set; }

    public bool Vigente { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Documento Documento { get; set; } = null!;

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual PlantillaEscrito PlantillaEscrito { get; set; } = null!;
}

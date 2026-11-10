using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Parámetros generales no modelados como columna. Tipo 1 texto, 2 entero, 3 decimal, 4 fecha, 5 booleano. Origen Monitorio: Configuracion.
/// </summary>
public partial class ConfiguracionEstudio
{
    public int EstudioId { get; set; }

    public string Clave { get; set; } = null!;

    public string? Valor { get; set; }

    public byte Tipo { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;
}

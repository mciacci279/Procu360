using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Procesos en segundo plano. Tipo 1 escritos masivos, 2 carga masiva, 3 modificación masiva, 4 importar novedades, 5 liquidación masiva, 6 purga de auditoría. Estado 1 en cola, 2 en curso, 3 finalizado, 4 finalizado con errores, 5 cancelado. 
/// </summary>
public partial class TrabajoProceso
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public byte Tipo { get; set; }

    public byte Estado { get; set; }

    public int Total { get; set; }

    public int Procesados { get; set; }

    public int Errores { get; set; }

    public string? ParametrosJson { get; set; }

    public string? ResultadoJson { get; set; }

    public DateTime? IniciadoUtc { get; set; }

    public DateTime? FinalizadoUtc { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;
}

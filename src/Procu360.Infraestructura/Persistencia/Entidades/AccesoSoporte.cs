using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Acceso temporal y auditado de un superadministrador a los datos de un Estudio. 
/// </summary>
public partial class AccesoSoporte
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int UsuarioId { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime DesdeUtc { get; set; }

    public DateTime HastaUtc { get; set; }

    public int OtorgadoPor { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Usuario OtorgadoPorNavigation { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}

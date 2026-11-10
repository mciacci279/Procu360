using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Membresía. Estado 1 invitado, 2 pendiente de aprobación (el email ya pertenece a otro Estudio), 3 activo, 4 desactivado, 5 rechazado. 
/// </summary>
public partial class EstudioUsuario
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public int UsuarioId { get; set; }

    public byte Estado { get; set; }

    public int? ProfesionalId { get; set; }

    public int? InvitadoPor { get; set; }

    public DateTime? InvitadoUtc { get; set; }

    public int? AprobadoPor { get; set; }

    public DateTime? AprobadoUtc { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? UltimoAccesoUtc { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Usuario? AprobadoPorNavigation { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<EstudioUsuarioRol> EstudioUsuarioRol { get; set; } = new List<EstudioUsuarioRol>();

    public virtual Usuario? InvitadoPorNavigation { get; set; }

    public virtual Profesional? Profesional { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}

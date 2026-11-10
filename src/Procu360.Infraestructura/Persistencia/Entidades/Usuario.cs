using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Identidad global (IdentityUser&lt;int&gt; de ASP.NET Core Identity con columnas propias). Sin EstudioId. Las contraseñas del legado NO se migran: PasswordHash queda NULL hasta que el usuario acepta la invitación. Origen Monitorio: Usuario (sin Pass).
/// </summary>
public partial class Usuario
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string EmailNormalizado { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public bool EsSuperAdmin { get; set; }

    public bool Activo { get; set; }

    public bool MfaHabilitado { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public DateTimeOffset? BloqueadoHastaUtc { get; set; }

    public int AccesosFallidos { get; set; }

    public DateTime? UltimoAccesoUtc { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<AccesoSoporte> AccesoSoporteOtorgadoPorNavigation { get; set; } = new List<AccesoSoporte>();

    public virtual ICollection<AccesoSoporte> AccesoSoporteUsuario { get; set; } = new List<AccesoSoporte>();

    public virtual ICollection<AuditoriaEvento> AuditoriaEvento { get; set; } = new List<AuditoriaEvento>();

    public virtual ICollection<DatoARevisar> DatoARevisar { get; set; } = new List<DatoARevisar>();

    public virtual ICollection<DevolucionCorreo> DevolucionCorreo { get; set; } = new List<DevolucionCorreo>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual ICollection<EstudioUsuario> EstudioUsuarioAprobadoPorNavigation { get; set; } = new List<EstudioUsuario>();

    public virtual ICollection<EstudioUsuario> EstudioUsuarioInvitadoPorNavigation { get; set; } = new List<EstudioUsuario>();

    public virtual ICollection<EstudioUsuario> EstudioUsuarioUsuario { get; set; } = new List<EstudioUsuario>();

    public virtual ICollection<EtapaUsuarioNotificado> EtapaUsuarioNotificado { get; set; } = new List<EtapaUsuarioNotificado>();

    public virtual ICollection<Expediente> ExpedienteDepuradorUsuario { get; set; } = new List<Expediente>();

    public virtual ICollection<Expediente> ExpedienteNegociadorUsuario { get; set; } = new List<Expediente>();

    public virtual ICollection<NovedadJudicial> NovedadJudicial { get; set; } = new List<NovedadJudicial>();

    public virtual ICollection<Tarea> Tarea { get; set; } = new List<Tarea>();

    public virtual ICollection<TareaParticipante> TareaParticipante { get; set; } = new List<TareaParticipante>();
}

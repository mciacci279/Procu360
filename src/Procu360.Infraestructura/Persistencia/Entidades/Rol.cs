using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Ámbito 1 todo el Estudio, 2 por comitente, 3 expedientes asignados. Origen Monitorio: Nueva (Perfil de Monitorio se descarta).
/// </summary>
public partial class Rol
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public byte Ambito { get; set; }

    public bool EsSistema { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<EstudioUsuarioRol> EstudioUsuarioRol { get; set; } = new List<EstudioUsuarioRol>();

    public virtual ICollection<RolPermiso> RolPermiso { get; set; } = new List<RolPermiso>();
}

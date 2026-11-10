using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: AgendaUsuario.
/// </summary>
public partial class TareaParticipante
{
    public int EstudioId { get; set; }

    public long TareaId { get; set; }

    public int UsuarioId { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Tarea Tarea { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}

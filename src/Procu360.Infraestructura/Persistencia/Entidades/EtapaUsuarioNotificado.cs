using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Origen Monitorio: EtapaAgendaUsuario.
/// </summary>
public partial class EtapaUsuarioNotificado
{
    public int EstudioId { get; set; }

    public int EtapaId { get; set; }

    public int UsuarioId { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Etapa Etapa { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}

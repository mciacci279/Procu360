using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Solo inserción. EstudioId NULL = evento de plataforma. Accion 1 crear, 2 editar, 3 anular, 4 eliminar, 5 cambiar estado, 6 calcular, 7 generar, 8 exportar, 9 login, 10 login fallido, 11 logout, 12 cambiar permisos, 13 aprobar membresía, 14 acceso de soporte. Resultado 1 ok, 2 rechazado, 3 error. Origen Monitorio: Nueva: reemplaza a LogSistema, que no se migra.
/// </summary>
public partial class AuditoriaEvento
{
    public long Id { get; set; }

    public int? EstudioId { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaUtc { get; set; }

    public string Modulo { get; set; } = null!;

    public string? Entidad { get; set; }

    public string? EntidadId { get; set; }

    public byte Accion { get; set; }

    public byte Resultado { get; set; }

    public string? CambiosJson { get; set; }

    public long? TrabajoProcesoId { get; set; }

    public Guid? CorrelacionId { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public virtual Estudio? Estudio { get; set; }

    public virtual Usuario? Usuario { get; set; }
}

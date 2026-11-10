using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Ámbito 1 nacional, 2 provincial, 3 judicial (receso). Origen Monitorio: Feriado.
/// </summary>
public partial class Feriado
{
    public int Id { get; set; }

    public DateOnly Fecha { get; set; }

    public string Descripcion { get; set; } = null!;

    public byte Ambito { get; set; }

    public int? ProvinciaId { get; set; }

    public Guid? LegadoId { get; set; }

    public virtual Provincia? Provincia { get; set; }
}

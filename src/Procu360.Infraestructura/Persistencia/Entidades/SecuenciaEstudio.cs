using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Numeración por Estudio (NroOrden). Se incrementa con UPDLOCK, HOLDLOCK dentro de la transacción de alta. Origen Monitorio: Secuencia.
/// </summary>
public partial class SecuenciaEstudio
{
    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public long UltimoValor { get; set; }

    public virtual Estudio Estudio { get; set; } = null!;
}

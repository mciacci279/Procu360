using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Coeficiente acumulado diario de tasa pasiva (BCRA). Lo mantiene el superadministrador. Origen Monitorio: AlicuotaPlanilla.
/// </summary>
public partial class IndiceTasaPasiva
{
    public DateOnly Fecha { get; set; }

    public decimal Coeficiente { get; set; }
}

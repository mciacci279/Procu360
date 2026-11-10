namespace Procu360.Dominio.Plazos;

/// <summary>
/// Vencimiento de una etapa (docs/04 §4.2): plazo en días corridos o hábiles, sin base de datos ni fecha del sistema.
/// Los días hábiles excluyen sábados, domingos y los feriados que reciba (nacionales, judiciales y del Estudio).
/// El día de inicio no cuenta: el plazo empieza el día siguiente.
/// </summary>
public static class CalculoPlazos
{
    public static DateOnly? Vencimiento(DateOnly inicio, int? dias, bool diasHabiles, IReadOnlySet<DateOnly> feriados)
    {
        if (dias is null or <= 0) return null;

        if (!diasHabiles) return inicio.AddDays(dias.Value);

        var fecha = inicio;
        var restantes = dias.Value;
        while (restantes > 0)
        {
            fecha = fecha.AddDays(1);
            if (EsHabil(fecha, feriados)) restantes--;
        }
        return fecha;
    }

    public static bool EsHabil(DateOnly fecha, IReadOnlySet<DateOnly> feriados) =>
        fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !feriados.Contains(fecha);
}

using Procu360.Dominio.Plazos;

namespace Procu360.Dominio.Tests;

public class PlazosTests
{
    private static readonly IReadOnlySet<DateOnly> SinFeriados = new HashSet<DateOnly>();

    [Fact]
    public void Dias_corridos_suman_al_dia_de_inicio()
    {
        var v = CalculoPlazos.Vencimiento(new DateOnly(2026, 11, 2), 10, false, SinFeriados);

        Assert.Equal(new DateOnly(2026, 11, 12), v);
    }

    [Fact]
    public void Dias_habiles_saltan_fines_de_semana()
    {
        // Viernes 6/11/2026 + 1 día hábil = lunes 9/11/2026.
        var v = CalculoPlazos.Vencimiento(new DateOnly(2026, 11, 6), 1, true, SinFeriados);

        Assert.Equal(new DateOnly(2026, 11, 9), v);
    }

    [Fact]
    public void Dias_habiles_saltan_feriados()
    {
        // Lunes 2/11/2026 + 2 días hábiles, con feriado el martes 3 → jueves 5.
        var feriados = new HashSet<DateOnly> { new(2026, 11, 3) };

        var v = CalculoPlazos.Vencimiento(new DateOnly(2026, 11, 2), 2, true, feriados);

        Assert.Equal(new DateOnly(2026, 11, 5), v);
    }

    [Fact]
    public void Sin_dias_no_hay_vencimiento()
    {
        Assert.Null(CalculoPlazos.Vencimiento(new DateOnly(2026, 11, 2), null, true, SinFeriados));
        Assert.Null(CalculoPlazos.Vencimiento(new DateOnly(2026, 11, 2), 0, false, SinFeriados));
    }
}

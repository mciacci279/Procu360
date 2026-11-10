using System.Reflection;

namespace Procu360.Liquidacion.Tests;

public class ReferenciaTests
{
    [Fact]
    public void Referencia_a_Dominio_resuelve()
    {
        Assert.NotNull(Assembly.Load("Procu360.Dominio"));
    }
}

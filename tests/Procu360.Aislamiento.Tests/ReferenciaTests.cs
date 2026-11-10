using System.Reflection;

namespace Procu360.Aislamiento.Tests;

public class ReferenciaTests
{
    [Fact]
    public void Referencia_a_Infraestructura_resuelve()
    {
        Assert.NotNull(Assembly.Load("Procu360.Infraestructura"));
    }
}

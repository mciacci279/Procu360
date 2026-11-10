using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Persistencia;

/// <summary>
/// Filtro global por Estudio (primera barrera de aislamiento en la aplicación).
/// La segunda barrera es el RLS de SQL Server, alimentado por <see cref="SesionEstudioInterceptor"/>.
/// </summary>
public partial class Procu360DbContext
{
    private readonly ContextoEstudio? _contexto;

    public Procu360DbContext(DbContextOptions<Procu360DbContext> options, ContextoEstudio contexto)
        : base(options)
    {
        _contexto = contexto;
    }

    /// <summary>Estudio activo; null cuando no hay contexto.</summary>
    internal int? EstudioActual => _contexto?.EstudioId;

    /// <summary>
    /// Valor que se compara en el filtro. Sin contexto vale -1, que nunca coincide con un Id real:
    /// comparar contra null haría que EF devuelva también las filas con EstudioId NULL (auditoría de plataforma).
    /// </summary>
    private int EstudioParaFiltro => EstudioActual ?? -1;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            var tipo = entidad.FindProperty("EstudioId")?.ClrType;
            if (tipo != typeof(int) && tipo != typeof(int?))
            {
                continue;
            }

            typeof(Procu360DbContext)
                .GetMethod(nameof(AplicarFiltroEstudio), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entidad.ClrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void AplicarFiltroEstudio<T>(ModelBuilder modelBuilder) where T : class =>
        modelBuilder.Entity<T>().HasQueryFilter(e => EF.Property<int?>(e, "EstudioId") == EstudioParaFiltro);
}

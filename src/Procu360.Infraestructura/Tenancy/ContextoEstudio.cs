namespace Procu360.Infraestructura.Tenancy;

/// <summary>
/// Contexto de la unidad de trabajo (una request o una prueba).
/// - Con EstudioId: operaciones de negocio; el filtro global y el RLS limitan a ese Estudio.
/// - Solo UsuarioId: antes de elegir Estudio (login, listar las propias membresías).
/// Sin EstudioId, el filtro global no devuelve filas de tablas por Estudio.
/// </summary>
public sealed class ContextoEstudio
{
    public int? EstudioId { get; private set; }

    public int? UsuarioId { get; private set; }

    public void Establecer(int estudioId, int? usuarioId = null)
    {
        EstudioId = estudioId;
        UsuarioId = usuarioId;
    }

    public void EstablecerUsuario(int usuarioId)
    {
        EstudioId = null;
        UsuarioId = usuarioId;
    }
}

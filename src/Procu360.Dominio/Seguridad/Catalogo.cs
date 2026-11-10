namespace Procu360.Dominio.Seguridad;

/// <summary>Módulos de la aplicación (docs/01 §5, seed de seg.Permiso). Cambiar aquí implica migrar seg.Permiso.</summary>
public static class Modulos
{
    public const string Expedientes = "expedientes";
    public const string Flujo = "flujo";
    public const string Tareas = "tareas";
    public const string Novedades = "novedades";
    public const string Escritos = "escritos";
    public const string Liquidaciones = "liquidaciones";
    public const string Embargos = "embargos";
    public const string PlanesPago = "planesPago";
    public const string Documentos = "documentos";
    public const string Listados = "listados";
    public const string DatosRevisar = "datosRevisar";
    public const string Configuracion = "configuracion";
    public const string Usuarios = "usuarios";
    public const string Auditoria = "auditoria";

    public static readonly IReadOnlyList<string> Todos =
    [
        Expedientes, Flujo, Tareas, Novedades, Escritos, Liquidaciones, Embargos,
        PlanesPago, Documentos, Listados, DatosRevisar, Configuracion, Usuarios, Auditoria
    ];
}

/// <summary>Acciones sobre cada módulo (docs/01 §5).</summary>
public static class Acciones
{
    public const string Consultar = "consultar";
    public const string Crear = "crear";
    public const string Editar = "editar";
    public const string Anular = "anular";
    public const string Exportar = "exportar";
    public const string Aprobar = "aprobar";
    public const string Administrar = "administrar";

    public static readonly IReadOnlyList<string> Todas =
    [
        Consultar, Crear, Editar, Anular, Exportar, Aprobar, Administrar
    ];
}

/// <summary>Códigos de permiso con formato "modulo.accion".</summary>
public static class Permisos
{
    public static string De(string modulo, string accion) => $"{modulo}.{accion}";

    /// <summary>Los 98 permisos del catálogo (14 módulos × 7 acciones).</summary>
    public static IEnumerable<string> Catalogo() =>
        from m in Modulos.Todos
        from a in Acciones.Todas
        select De(m, a);

    public static readonly string UsuariosCrear = De(Modulos.Usuarios, Acciones.Crear);
    public static readonly string UsuariosAdministrar = De(Modulos.Usuarios, Acciones.Administrar);
    public static readonly string ConfiguracionAdministrar = De(Modulos.Configuracion, Acciones.Administrar);
}

using Procu360.Dominio.Seguridad;

namespace Procu360.Dominio.Tests;

public class CatalogoTests
{
    [Fact]
    public void El_catalogo_tiene_98_permisos_unicos()
    {
        var permisos = Permisos.Catalogo().ToList();

        Assert.Equal(98, permisos.Count);
        Assert.Equal(98, permisos.Distinct().Count());
    }

    [Fact]
    public void Los_codigos_siguen_el_formato_modulo_punto_accion()
    {
        Assert.All(Permisos.Catalogo(), p =>
        {
            var partes = p.Split('.');
            Assert.Equal(2, partes.Length);
            Assert.Contains(partes[0], Modulos.Todos);
            Assert.Contains(partes[1], Acciones.Todas);
        });
    }

    [Fact]
    public void Los_permisos_que_usa_la_aplicacion_estan_en_el_catalogo()
    {
        var catalogo = Permisos.Catalogo().ToHashSet();

        Assert.Contains(Permisos.UsuariosCrear, catalogo);
        Assert.Contains(Permisos.UsuariosAdministrar, catalogo);
        Assert.Contains(Permisos.ConfiguracionAdministrar, catalogo);
    }
}

public class CalculoPermisosTests
{
    private static RolAsignado Rol(Ambito ambito, int? comitente = null, params string[] permisos) =>
        new("rol", ambito, permisos, comitente);

    [Fact]
    public void Sin_roles_no_hay_permisos_ni_alcance()
    {
        var efectivos = CalculoPermisos.Calcular([]);

        Assert.Empty(efectivos.Permisos);
        Assert.False(efectivos.Alcance.TodoEstudio);
        Assert.False(efectivos.Alcance.Permite(1));
    }

    [Fact]
    public void Los_permisos_de_varios_roles_se_unen()
    {
        var efectivos = CalculoPermisos.Calcular(
        [
            Rol(Ambito.Estudio, null, "expedientes.consultar"),
            Rol(Ambito.Estudio, null, "liquidaciones.aprobar")
        ]);

        Assert.True(efectivos.Tiene("expedientes.consultar"));
        Assert.True(efectivos.Tiene("liquidaciones.aprobar"));
        Assert.False(efectivos.Tiene("usuarios.administrar"));
    }

    [Fact]
    public void Un_rol_de_todo_el_Estudio_da_alcance_total_aunque_haya_otros_ambitos()
    {
        var efectivos = CalculoPermisos.Calcular(
        [
            Rol(Ambito.Comitente, 7, "expedientes.consultar"),
            Rol(Ambito.Estudio, null, "expedientes.consultar")
        ]);

        Assert.True(efectivos.Alcance.TodoEstudio);
        Assert.True(efectivos.Alcance.Permite(999));
    }

    [Fact]
    public void El_ambito_por_comitente_solo_permite_los_comitentes_asignados()
    {
        var efectivos = CalculoPermisos.Calcular(
        [
            Rol(Ambito.Comitente, 3, "expedientes.consultar"),
            Rol(Ambito.Comitente, 5, "expedientes.consultar")
        ]);

        Assert.False(efectivos.Alcance.TodoEstudio);
        Assert.True(efectivos.Alcance.Permite(3));
        Assert.True(efectivos.Alcance.Permite(5));
        Assert.False(efectivos.Alcance.Permite(4));
    }

    [Fact]
    public void El_ambito_asignados_se_registra_sin_dar_todo_el_Estudio()
    {
        var efectivos = CalculoPermisos.Calcular([Rol(Ambito.Asignados, null, "tareas.consultar")]);

        Assert.True(efectivos.Alcance.Asignados);
        Assert.False(efectivos.Alcance.TodoEstudio);
    }
}

public class ReglasRolTests
{
    [Fact]
    public void Un_rol_valido_no_tiene_errores()
    {
        var errores = ReglasRol.ValidarRol("Contador", ["liquidaciones.consultar"]);

        Assert.Empty(errores);
    }

    [Fact]
    public void Un_rol_sin_nombre_o_sin_permisos_se_rechaza()
    {
        var errores = ReglasRol.ValidarRol("  ", []);

        Assert.Equal(2, errores.Count);
    }

    [Fact]
    public void Un_permiso_inexistente_se_rechaza()
    {
        var errores = ReglasRol.ValidarRol("Rol", ["expedientes.volar"]);

        Assert.Single(errores);
        Assert.Contains("expedientes.volar", errores[0]);
    }

    [Fact]
    public void No_se_puede_quitar_el_ultimo_administrador()
    {
        Assert.False(ReglasRol.PuedeQuitarAdministrador(1));
        Assert.True(ReglasRol.PuedeQuitarAdministrador(2));
    }

    [Fact]
    public void Sin_usuarios_administrar_no_se_puede_asignar_ningun_rol()
    {
        var actor = CalculoPermisos.Calcular([new RolAsignado("r", Ambito.Estudio, ["expedientes.consultar"], null)]);

        Assert.False(ReglasRol.PuedeAsignarRol(actor, ["expedientes.consultar"]));
    }

    [Fact]
    public void No_se_puede_asignar_un_rol_con_permisos_que_el_actor_no_tiene()
    {
        var actor = CalculoPermisos.Calcular([new RolAsignado("r", Ambito.Estudio,
            ["usuarios.administrar", "expedientes.consultar"], null)]);

        Assert.False(ReglasRol.PuedeAsignarRol(actor, ["expedientes.consultar", "liquidaciones.aprobar"]));
        Assert.True(ReglasRol.PuedeAsignarRol(actor, ["expedientes.consultar"]));
    }
}

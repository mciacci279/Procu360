using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Identidad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Integracion.Tests;

/// <summary>
/// Roles, permisos y administración del Estudio. Las personas del seed se usan para leer permisos;
/// las pruebas que cambian roles o membresías usan usuarios de prueba (PRUEBA-…) y se limpian al final.
/// </summary>
public class SeguridadTests : IDisposable
{
    private const int Estudio1 = 1;
    private const int Estudio2 = 2;
    private const int Laura = 2;          // Administrador, Estudio 1 (seed)
    private const string Martin = "martin.ochoa@ferreyra.test";   // Abogado / Procurador
    private const string Pablo = "pablo.ruiz@ferreyra.test";      // Auditor + Solo lectura
    private const string Carla = "carla.quiroga@ferreyra.test";   // Liquidaciones
    private const string Diego = "diego.luna@ferreyra.test";      // Gestor (ámbito asignados)

    private readonly ServiceProvider _sp = PruebasIdentidad.CrearServicios();

    public void Dispose()
    {
        _sp.Dispose();
        PruebasIdentidad.LimpiarUsuariosDePrueba();
    }

    private IServiceScope Ambito(int estudio, int usuario)
    {
        var ambito = _sp.CreateScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudio, usuario);
        return ambito;
    }

    private int IdUsuario(string email)
    {
        using var ambito = _sp.CreateScope();
        var normalizado = email.ToUpperInvariant();
        return ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Usuario
            .Where(u => u.EmailNormalizado == normalizado).Select(u => u.Id).Single();
    }

    private int MembresiaDe(int estudio, int usuario)
    {
        using var ambito = Ambito(estudio, Laura);
        return ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().EstudioUsuario
            .Where(m => m.UsuarioId == usuario).Select(m => m.Id).Single();
    }

    private async Task<PermisosEfectivos> PermisosDeAsync(int estudio, int usuario)
    {
        using var ambito = Ambito(estudio, usuario);
        return await ambito.ServiceProvider.GetRequiredService<ServicioPermisos>().EfectivosAsync();
    }

    private async Task<T> ComoAsync<T>(int estudio, int usuario, Func<ServicioAdministracionEstudio, Task<T>> operacion)
    {
        using var ambito = Ambito(estudio, usuario);
        return await operacion(ambito.ServiceProvider.GetRequiredService<ServicioAdministracionEstudio>());
    }

    private async Task<int> RolIdAsync(int estudio, string nombre)
    {
        using var ambito = Ambito(estudio, Laura);
        return await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>().Rol
            .Where(r => r.Nombre == nombre).Select(r => r.Id).SingleAsync();
    }

    /// <summary>Crea un usuario de prueba con membresía activa en el Estudio 1 y devuelve su id.</summary>
    private async Task<int> UsuarioPruebaActivoAsync()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var id = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio1, id, EstadosMembresia.Activo);
        return id;
    }

    // ------------------------------------------------------------ permisos por rol

    [Fact]
    public async Task El_administrador_tiene_los_98_permisos_del_catalogo()
    {
        var permisos = await PermisosDeAsync(Estudio1, Laura);

        Assert.Equal(98, permisos.Permisos.Count);
        Assert.True(permisos.Alcance.TodoEstudio);
    }

    [Fact]
    public async Task El_abogado_consulta_y_opera_pero_no_administra_usuarios()
    {
        var permisos = await PermisosDeAsync(Estudio1, IdUsuario(Martin));

        Assert.True(permisos.Tiene("expedientes.consultar"));
        Assert.True(permisos.Tiene("expedientes.editar"));
        Assert.False(permisos.Tiene("usuarios.administrar"));
        Assert.False(permisos.Tiene("configuracion.administrar"));
    }

    [Fact]
    public async Task El_auditor_consulta_la_auditoria_y_no_edita()
    {
        var permisos = await PermisosDeAsync(Estudio1, IdUsuario(Pablo));

        Assert.True(permisos.Tiene("auditoria.consultar"));
        Assert.True(permisos.Tiene("auditoria.exportar"));
        Assert.False(permisos.Tiene("expedientes.editar"));
    }

    [Fact]
    public async Task Liquidaciones_puede_aprobar_y_no_edita_expedientes()
    {
        var permisos = await PermisosDeAsync(Estudio1, IdUsuario(Carla));

        Assert.True(permisos.Tiene("liquidaciones.aprobar"));
        Assert.False(permisos.Tiene("expedientes.editar"));
    }

    [Fact]
    public async Task El_gestor_tiene_alcance_de_expedientes_asignados_y_no_todo_el_Estudio()
    {
        var permisos = await PermisosDeAsync(Estudio1, IdUsuario(Diego));

        Assert.False(permisos.Alcance.TodoEstudio);
        Assert.True(permisos.Alcance.Asignados);
    }

    [Fact]
    public async Task Sin_usuarios_crear_no_se_puede_invitar()
    {
        var resultado = await ComoInvitarAsync(Estudio1, IdUsuario(Diego));

        Assert.Equal(TipoResultadoInvitacion.Error, resultado.Tipo);
        Assert.Contains("permiso", resultado.Error);
    }

    private async Task<ResultadoInvitacion> ComoInvitarAsync(int estudio, int usuario)
    {
        using var ambito = Ambito(estudio, usuario);
        return await ambito.ServiceProvider.GetRequiredService<ServicioInvitaciones>()
            .InvitarAsync(usuario, new DatosInvitacion(PruebasIdentidad.NuevoEmail(), "Nuevo", "Integrante"));
    }

    // ------------------------------------------------------------ administración

    [Fact]
    public async Task Sin_usuarios_administrar_no_se_pueden_crear_roles()
    {
        var resultado = await ComoAsync(Estudio1, IdUsuario(Martin), s =>
            s.CrearRolAsync("PRUEBA-ROL-INTRUSO", null, 1, ["expedientes.consultar"]));

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task No_se_puede_crear_un_rol_con_permisos_que_el_actor_no_tiene()
    {
        // Persona con usuarios.administrar y expedientes.consultar, pero sin liquidaciones.aprobar.
        var gerente = await UsuarioPruebaActivoAsync();
        var membresia = MembresiaDe(Estudio1, gerente);
        var rolGerente = await ComoAsync(Estudio1, Laura, s =>
            s.CrearRolAsync("PRUEBA-ROL-GERENTE", null, 1, ["usuarios.administrar", "expedientes.consultar"]));
        Assert.True(rolGerente.Exito);
        var rolGerenteId = await RolIdAsync(Estudio1, "PRUEBA-ROL-GERENTE");
        var asignado = await ComoAsync(Estudio1, Laura, s => s.AsignarRolAsync(membresia, rolGerenteId, null));
        Assert.True(asignado.Exito);

        var escalada = await ComoAsync(Estudio1, gerente, s =>
            s.CrearRolAsync("PRUEBA-ROL-ESCALADO", null, 1, ["liquidaciones.aprobar"]));
        var permitida = await ComoAsync(Estudio1, gerente, s =>
            s.CrearRolAsync("PRUEBA-ROL-PERMITIDO", null, 1, ["expedientes.consultar"]));

        Assert.False(escalada.Exito);
        Assert.True(permitida.Exito);
    }

    [Fact]
    public async Task No_se_puede_dejar_el_Estudio_sin_administradores()
    {
        var membresiaLaura = MembresiaDe(Estudio1, Laura);
        using (var ambito = Ambito(Estudio1, Laura))
        {
            var asignacionAdmin = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>()
                .EstudioUsuarioRol.Where(eur => eur.EstudioUsuarioId == membresiaLaura && eur.Rol.Nombre == "Administrador")
                .Select(eur => eur.Id).SingleAsync();

            var desactivar = await ComoAsync(Estudio1, Laura, s => s.DesactivarMembresiaAsync(membresiaLaura));
            var quitar = await ComoAsync(Estudio1, Laura, s => s.QuitarRolAsync(asignacionAdmin));

            Assert.False(desactivar.Exito);
            Assert.False(quitar.Exito);
        }
        Assert.Equal(98, (await PermisosDeAsync(Estudio1, Laura)).Permisos.Count);
    }

    [Fact]
    public async Task Crear_asignar_cambiar_y_quitar_un_rol_actualiza_los_permisos()
    {
        var persona = await UsuarioPruebaActivoAsync();
        var membresia = MembresiaDe(Estudio1, persona);

        var creado = await ComoAsync(Estudio1, Laura, s =>
            s.CrearRolAsync("PRUEBA-ROL-CONTADOR", "Rol de prueba", 1, ["liquidaciones.consultar"]));
        Assert.True(creado.Exito);
        var rolId = await RolIdAsync(Estudio1, "PRUEBA-ROL-CONTADOR");

        Assert.Empty((await PermisosDeAsync(Estudio1, persona)).Permisos);

        var asignado = await ComoAsync(Estudio1, Laura, s => s.AsignarRolAsync(membresia, rolId, null));
        Assert.True(asignado.Exito);
        Assert.True((await PermisosDeAsync(Estudio1, persona)).Tiene("liquidaciones.consultar"));

        var actualizado = await ComoAsync(Estudio1, Laura, s =>
            s.ActualizarPermisosAsync(rolId, ["liquidaciones.consultar", "liquidaciones.exportar"]));
        Assert.True(actualizado.Exito);
        Assert.True((await PermisosDeAsync(Estudio1, persona)).Tiene("liquidaciones.exportar"));

        using (var ambito = Ambito(Estudio1, Laura))
        {
            var asignacion = await ambito.ServiceProvider.GetRequiredService<Procu360DbContext>()
                .EstudioUsuarioRol.Where(eur => eur.EstudioUsuarioId == membresia && eur.RolId == rolId)
                .Select(eur => eur.Id).SingleAsync();
            var quitado = await ComoAsync(Estudio1, Laura, s => s.QuitarRolAsync(asignacion));
            Assert.True(quitado.Exito);
        }
        Assert.Empty((await PermisosDeAsync(Estudio1, persona)).Permisos);
    }

    [Fact]
    public async Task Un_rol_de_ambito_comitente_exige_un_comitente()
    {
        var persona = await UsuarioPruebaActivoAsync();
        var membresia = MembresiaDe(Estudio1, persona);
        await ComoAsync(Estudio1, Laura, s =>
            s.CrearRolAsync("PRUEBA-ROL-COMITENTE", null, 2, ["expedientes.consultar"]));
        var rolId = await RolIdAsync(Estudio1, "PRUEBA-ROL-COMITENTE");

        var sinComitente = await ComoAsync(Estudio1, Laura, s => s.AsignarRolAsync(membresia, rolId, null));

        Assert.False(sinComitente.Exito);
    }

    [Fact]
    public async Task El_perfil_de_la_sesion_muestra_el_Estudio_y_los_roles()
    {
        using var ambito = Ambito(Estudio1, Laura);

        var perfil = await ambito.ServiceProvider.GetRequiredService<ServicioSesion>()
            .PerfilAsync(Laura, Estudio1);

        Assert.NotNull(perfil);
        Assert.Equal("Ferreyra, Laura", perfil!.NombreCompleto);
        Assert.Equal("Estudio Ferreyra & Asociados", perfil.NombreEstudio);
        Assert.Equal(["Administrador"], perfil.Roles);
    }

    [Fact]
    public async Task Toda_asignacion_de_un_rol_por_comitente_tiene_comitente()
    {
        // Regresión: el seed dejaba a Pablo y a Nicolás con rol por comitente sin comitente.
        using var ambito = Ambito(Estudio1, Laura);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();

        var sinComitente = await db.EstudioUsuarioRol
            .CountAsync(eur => eur.Rol.Ambito == (byte)Procu360.Dominio.Seguridad.Ambito.Comitente && eur.ComitenteId == null);

        Assert.Equal(0, sinComitente);
    }

    [Fact]
    public async Task Desactivar_una_membresia_quita_sus_permisos()
    {
        var persona = await UsuarioPruebaActivoAsync();
        var membresia = MembresiaDe(Estudio1, persona);
        await ComoAsync(Estudio1, Laura, s => s.CrearRolAsync("PRUEBA-ROL-LECTOR", null, 1, ["expedientes.consultar"]));
        var lectorId = await RolIdAsync(Estudio1, "PRUEBA-ROL-LECTOR");
        await ComoAsync(Estudio1, Laura, s => s.AsignarRolAsync(membresia, lectorId, null));
        Assert.True((await PermisosDeAsync(Estudio1, persona)).Tiene("expedientes.consultar"));

        var desactivado = await ComoAsync(Estudio1, Laura, s => s.DesactivarMembresiaAsync(membresia));

        Assert.True(desactivado.Exito);
        Assert.Empty((await PermisosDeAsync(Estudio1, persona)).Permisos);
    }

    [Fact]
    public async Task Reactivar_una_membresia_desactivada_recupera_los_roles_que_tenia()
    {
        var persona = await UsuarioPruebaActivoAsync();
        var membresia = MembresiaDe(Estudio1, persona);
        await ComoAsync(Estudio1, Laura, s => s.CrearRolAsync("PRUEBA-ROL-REACTIVAR", null, 1, ["expedientes.consultar"]));
        var rolId = await RolIdAsync(Estudio1, "PRUEBA-ROL-REACTIVAR");
        await ComoAsync(Estudio1, Laura, s => s.AsignarRolAsync(membresia, rolId, null));
        await ComoAsync(Estudio1, Laura, s => s.DesactivarMembresiaAsync(membresia));
        Assert.Empty((await PermisosDeAsync(Estudio1, persona)).Permisos);

        var reactivado = await ComoAsync(Estudio1, Laura, s => s.ReactivarMembresiaAsync(membresia));

        Assert.True(reactivado.Exito);
        Assert.True((await PermisosDeAsync(Estudio1, persona)).Tiene("expedientes.consultar"));
    }

    [Fact]
    public async Task No_se_puede_reactivar_una_membresia_que_nunca_estuvo_activa()
    {
        var email = PruebasIdentidad.NuevoEmail();
        var persona = await PruebasIdentidad.CrearUsuarioConClaveAsync(_sp, email);
        await PruebasIdentidad.InsertarMembresiaAsync(_sp, Estudio1, persona, EstadosMembresia.Invitado);
        var membresia = MembresiaDe(Estudio1, persona);

        var resultado = await ComoAsync(Estudio1, Laura, s => s.ReactivarMembresiaAsync(membresia));

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task Los_cambios_de_permisos_quedan_auditados()
    {
        using var antes = Ambito(Estudio1, Laura);
        var cantidadAntes = await antes.ServiceProvider.GetRequiredService<Procu360DbContext>()
            .AuditoriaEvento.CountAsync(e => e.Modulo == "usuarios");

        await ComoAsync(Estudio1, Laura, s => s.CrearRolAsync("PRUEBA-ROL-AUDITADO", null, 1, ["tareas.consultar"]));

        using var despues = Ambito(Estudio1, Laura);
        var cantidadDespues = await despues.ServiceProvider.GetRequiredService<Procu360DbContext>()
            .AuditoriaEvento.CountAsync(e => e.Modulo == "usuarios");
        Assert.True(cantidadDespues > cantidadAntes);
    }

    [Fact]
    public async Task Los_comitentes_para_asignar_roles_son_los_activos_del_propio_Estudio()
    {
        var delEstudio1 = await ComoAsync(Estudio1, Laura, s => s.ListarComitentesAsync());

        using var ambito2 = Ambito(Estudio2, Laura);
        var idsEstudio2 = await ambito2.ServiceProvider.GetRequiredService<Procu360DbContext>()
            .Comitente.Select(c => c.Id).ToListAsync();
        using var ambito1 = Ambito(Estudio1, Laura);
        var activos1 = await ambito1.ServiceProvider.GetRequiredService<Procu360DbContext>()
            .Comitente.Where(c => c.Activo).Select(c => c.Id).ToListAsync();

        Assert.NotEmpty(delEstudio1);
        Assert.Equal(activos1.Order(), delEstudio1.Select(c => c.Id).Order());
        Assert.DoesNotContain(delEstudio1, c => idsEstudio2.Contains(c.Id));
    }
}

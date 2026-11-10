using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Procu360.Dominio.Seguridad;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Persistencia.Entidades;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Identidad;

public sealed record DatosInvitacion(string Email, string Nombre, string Apellido);

public enum TipoResultadoInvitacion { Creada, Pendiente, Error }

public sealed record ResultadoInvitacion(TipoResultadoInvitacion Tipo, int? UsuarioId, string? Token, string? Error)
{
    public static ResultadoInvitacion Creada(int usuarioId, string token) =>
        new(TipoResultadoInvitacion.Creada, usuarioId, token, null);

    /// <summary>Queda pendiente de aprobación de plataforma: no se genera enlace.</summary>
    public static ResultadoInvitacion Pendiente(int usuarioId) =>
        new(TipoResultadoInvitacion.Pendiente, usuarioId, null, null);

    public static ResultadoInvitacion Fallo(string error) =>
        new(TipoResultadoInvitacion.Error, null, null, error);
}

public sealed record ResultadoAceptacion(bool Exito, IReadOnlyList<string> Errores, int MembresiasActivadas)
{
    public static ResultadoAceptacion Fallo(params string[] errores) => new(false, errores, 0);
}

/// <summary>
/// Invitaciones a un Estudio y aceptación.
/// - InvitarAsync: el DbContext tiene el Estudio activo del administrador (EstudioId + UsuarioId).
/// - AceptarAsync: el DbContext solo tiene UsuarioId (la persona todavía no eligió Estudio).
/// La activación de cada membresía se hace en su propio contexto de Estudio, porque el RLS
/// no permite modificar membresías desde otro Estudio.
/// </summary>
public sealed class ServicioInvitaciones
{
    public const string PropositoInvitacion = "Invitacion";

    private readonly Procu360DbContext _db;
    private readonly UserManager<Usuario> _usuarios;
    private readonly IServiceScopeFactory _ambitos;
    private readonly ServicioPermisos _permisos;

    public ServicioInvitaciones(
        Procu360DbContext db, UserManager<Usuario> usuarios, IServiceScopeFactory ambitos, ServicioPermisos permisos)
    {
        _db = db;
        _usuarios = usuarios;
        _ambitos = ambitos;
        _permisos = permisos;
    }

    public async Task<ResultadoInvitacion> InvitarAsync(int invitadoPor, DatosInvitacion datos, CancellationToken ct = default)
    {
        if (!await _permisos.TieneAsync(Permisos.UsuariosCrear))
            return ResultadoInvitacion.Fallo("No tenés permiso para invitar integrantes.");

        var estudioId = _db.EstudioActual
            ?? throw new InvalidOperationException("No hay Estudio activo para invitar.");

        var email = datos.Email?.Trim() ?? string.Empty;
        if (email.Length == 0) return ResultadoInvitacion.Fallo("Ingresá un email.");
        if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Apellido))
            return ResultadoInvitacion.Fallo("Ingresá nombre y apellido.");

        var normalizado = _usuarios.NormalizeEmail(email);
        var usuario = await _db.Usuario.FirstOrDefaultAsync(u => u.EmailNormalizado == normalizado, ct);

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Email = email,
                EmailNormalizado = normalizado,
                Nombre = datos.Nombre.Trim(),
                Apellido = datos.Apellido.Trim(),
                Activo = true,
                CreadoUtc = DateTime.UtcNow,
                CreadoPor = invitadoPor
            };
            var creado = await _usuarios.CreateAsync(usuario);
            if (!creado.Succeeded)
                return ResultadoInvitacion.Fallo(string.Join(" ", creado.Errors.Select(e => e.Description)));
        }
        else if (!usuario.Activo)
        {
            return ResultadoInvitacion.Fallo("Ese usuario está desactivado en la plataforma.");
        }

        var yaTieneMembresia = await _db.EstudioUsuario.AnyAsync(m => m.UsuarioId == usuario.Id, ct);
        if (yaTieneMembresia)
            return ResultadoInvitacion.Fallo("Ese usuario ya es miembro de este Estudio o tiene una invitación pendiente.");

        var otroActivo = await TieneMembresiaActivaEnOtroEstudioAsync(usuario.Id, estudioId, ct);
        var estado = otroActivo ? EstadosMembresia.Pendiente : EstadosMembresia.Invitado;

        _db.EstudioUsuario.Add(new EstudioUsuario
        {
            EstudioId = estudioId,
            UsuarioId = usuario.Id,
            Estado = estado,
            InvitadoPor = invitadoPor,
            InvitadoUtc = DateTime.UtcNow,
            CreadoUtc = DateTime.UtcNow,
            CreadoPor = invitadoPor
        });
        await _db.SaveChangesAsync(ct);

        if (otroActivo) return ResultadoInvitacion.Pendiente(usuario.Id);

        var token = await _usuarios.GenerateUserTokenAsync(usuario, TokenOptions.DefaultProvider, PropositoInvitacion);
        return ResultadoInvitacion.Creada(usuario.Id, token);
    }

    public async Task<ResultadoAceptacion> AceptarAsync(
        int usuarioId, string token, string? nuevaContrasena, CancellationToken ct = default)
    {
        const string invalida = "El enlace de invitación no es válido o venció.";

        var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || !usuario.Activo)
            return ResultadoAceptacion.Fallo(invalida);

        if (!await _usuarios.VerifyUserTokenAsync(usuario, TokenOptions.DefaultProvider, PropositoInvitacion, token))
            return ResultadoAceptacion.Fallo(invalida);

        if (!await _usuarios.HasPasswordAsync(usuario))
        {
            if (string.IsNullOrEmpty(nuevaContrasena))
                return ResultadoAceptacion.Fallo("Ingresá una contraseña.");

            var clave = await _usuarios.AddPasswordAsync(usuario, nuevaContrasena);
            if (!clave.Succeeded)
                return ResultadoAceptacion.Fallo(clave.Errors.Select(e => e.Description).ToArray());

            // Cambiar el sello de seguridad invalida el token: el enlace es de un solo uso.
            await _usuarios.UpdateSecurityStampAsync(usuario);
        }

        var pendientes = await _db.EstudioUsuario.IgnoreQueryFilters()
            .Where(m => m.UsuarioId == usuarioId && m.Estado == EstadosMembresia.Invitado)
            .Select(m => m.EstudioId)
            .ToListAsync(ct);

        foreach (var estudioId in pendientes)
        {
            await ActivarMembresiaAsync(usuarioId, estudioId, ct);
        }

        return new ResultadoAceptacion(true, Array.Empty<string>(), pendientes.Count);
    }

    private async Task ActivarMembresiaAsync(int usuarioId, int estudioId, CancellationToken ct)
    {
        await using var ambito = _ambitos.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoEstudio>().Establecer(estudioId, usuarioId);
        var db = ambito.ServiceProvider.GetRequiredService<Procu360DbContext>();

        var membresia = await db.EstudioUsuario.FirstOrDefaultAsync(
            m => m.UsuarioId == usuarioId && m.Estado == EstadosMembresia.Invitado, ct);
        if (membresia is null) return;

        membresia.Estado = EstadosMembresia.Activo;
        membresia.ModificadoUtc = DateTime.UtcNow;
        membresia.ModificadoPor = usuarioId;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Consulta privilegiada (procedimiento con principal de plataforma): el RLS no deja al
    /// administrador ver membresías de otros Estudios. Devuelve solo un bit.
    /// </summary>
    private async Task<bool> TieneMembresiaActivaEnOtroEstudioAsync(int usuarioId, int estudioId, CancellationToken ct)
    {
        var conexion = _db.Database.GetDbConnection();
        var abrioAhora = conexion.State != ConnectionState.Open;
        if (abrioAhora) await _db.Database.OpenConnectionAsync(ct);
        try
        {
            using var comando = conexion.CreateCommand();
            comando.CommandText = "seg.usp_TieneMembresiaActivaEnOtroEstudio";
            comando.CommandType = CommandType.StoredProcedure;
            comando.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
            comando.Parameters.Add(new SqlParameter("@UsuarioId", usuarioId));
            comando.Parameters.Add(new SqlParameter("@EstudioId", estudioId));
            var salida = new SqlParameter("@Existe", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            comando.Parameters.Add(salida);
            await comando.ExecuteNonQueryAsync(ct);
            return salida.Value is bool existe && existe;
        }
        finally
        {
            if (abrioAhora) await _db.Database.CloseConnectionAsync();
        }
    }
}

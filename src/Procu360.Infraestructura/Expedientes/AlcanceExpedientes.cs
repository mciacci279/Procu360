using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;
using Procu360.Infraestructura.Seguridad;
using Procu360.Infraestructura.Tenancy;

namespace Procu360.Infraestructura.Expedientes;

/// <summary>
/// Conjunto de expedientes que la persona puede ver según su ámbito en el Estudio activo:
/// todo el Estudio, los comitentes asignados, o los expedientes asignados a ella
/// (negociador, depurador o profesional vinculado a su membresía). Lo usan Mi día y la búsqueda.
/// </summary>
public sealed class AlcanceExpedientes(Procu360DbContext db, ContextoEstudio contexto, ServicioPermisos permisos)
{
    /// <summary>Consulta de ids, sin materializar: se compone con otras consultas de EF.</summary>
    public async Task<IQueryable<long>> IdsAsync(CancellationToken ct = default)
    {
        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var alcance = (await permisos.EfectivosAsync(ct)).Alcance;
        var todo = alcance.TodoEstudio;
        var asignados = alcance.Asignados;
        var comitentes = alcance.Comitentes.ToList();

        var profesional = await db.EstudioUsuario.Where(m => m.UsuarioId == usuario)
            .Select(m => m.ProfesionalId).FirstOrDefaultAsync(ct);

        return db.Expediente
            .Where(e => !e.Anulado && (
                todo
                || (e.ComitenteId != null && comitentes.Contains(e.ComitenteId.Value))
                || (asignados && (
                    e.NegociadorUsuarioId == usuario
                    || e.DepuradorUsuarioId == usuario
                    || (profesional != null && (e.ProcuradorId == profesional || e.PatrocinanteId == profesional || e.AbogadoInteriorId == profesional))))))
            .Select(e => e.Id);
    }

    /// <summary>
    /// Para la búsqueda: incluye también los dados de baja cuando se pide. El alcance se aplica igual.
    /// </summary>
    public async Task<IQueryable<long>> IdsIncluyendoBajasAsync(CancellationToken ct = default)
    {
        var usuario = contexto.UsuarioId ?? throw new InvalidOperationException("No hay usuario en la sesión.");
        var alcance = (await permisos.EfectivosAsync(ct)).Alcance;
        var todo = alcance.TodoEstudio;
        var asignados = alcance.Asignados;
        var comitentes = alcance.Comitentes.ToList();

        var profesional = await db.EstudioUsuario.Where(m => m.UsuarioId == usuario)
            .Select(m => m.ProfesionalId).FirstOrDefaultAsync(ct);

        return db.Expediente
            .Where(e => todo
                || (e.ComitenteId != null && comitentes.Contains(e.ComitenteId.Value))
                || (asignados && (
                    e.NegociadorUsuarioId == usuario
                    || e.DepuradorUsuarioId == usuario
                    || (profesional != null && (e.ProcuradorId == profesional || e.PatrocinanteId == profesional || e.AbogadoInteriorId == profesional)))))
            .Select(e => e.Id);
    }
}

using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia;

namespace Procu360.Infraestructura.Expedientes;

public sealed record EncabezadoExpediente(
    long Id,
    int NroOrden,
    string? NroExpedienteJudicial,
    string? Caratula,
    string? Recaratulacion,
    string? Juzgado,
    string? Procurador,
    string? Instancia,
    string? EtapaActual,
    bool Anulado,
    string? MotivoAnulacion,
    string? NroCuenta,
    string? Comitente,
    string? Demandado,
    int Codemandados,
    string? Concepto,
    DateOnly? FechaSorteo,
    string? Sorteo)
{
    /// <summary>
    /// El expediente está judicializado cuando ya tiene número de expediente judicial o juzgado asignado.
    /// Los datos del juicio solo se muestran en ese caso.
    /// </summary>
    public bool Judicializado => NroExpedienteJudicial is not null || Juzgado is not null;
}

public sealed record ResumenExpediente(
    string? Demandado,
    int Codemandados,
    string? Comitente,
    string? Concepto,
    string? NroCuenta,
    string? Nomenclatura,
    string? Dominio,
    string? Designacion,
    string? MatriculaRpi,
    decimal? MontoTitulo,
    decimal? MontoCapital,
    DateOnly? FechaTitulo,
    string? NroLiquidacion,
    string? Observaciones);

public sealed record PersonaExpediente(string Nombre, string Rol, string? Documento, string? Cuit, string? Domicilio);

public sealed record PeriodoExpediente(short Anio, string? Cuota, string? Concepto, DateOnly? Vencimiento, decimal Tributo, decimal Recargo);

public sealed record EtapaHistorial(string Etapa, DateOnly Inicio, DateOnly? Vencimiento, byte Estado, decimal? Monto);

public sealed record TareaExpediente(long Id, string Titulo, DateOnly? Vence, byte Estado, string? Responsable);

/// <summary>
/// Datos de la ficha de un expediente (docs/02 §4.3), solo lectura.
/// Todo se consulta dentro del alcance de la persona: un expediente fuera de su alcance no existe para ella.
/// </summary>
public sealed class FichaExpedienteServicio(Procu360DbContext db, AlcanceExpedientes alcance)
{
    /// <summary>True si el expediente existe en el Estudio activo y está dentro del alcance de la persona.</summary>
    public async Task<bool> VisibleAsync(long id, CancellationToken ct = default)
    {
        var ids = await alcance.IdsIncluyendoBajasAsync(ct);
        return await ids.AnyAsync(x => x == id, ct);
    }

    public async Task<EncabezadoExpediente?> EncabezadoAsync(long id, CancellationToken ct = default)
    {
        var e = await db.Expediente.Where(x => x.Id == id).Select(x => new EncabezadoExpediente(
            x.Id, x.NroOrden, x.NroExpedienteJudicial, x.Caratula, x.Recaratulacion,
            x.Juzgado != null ? x.Juzgado.Nombre : null,
            db.Profesional.Where(p => p.Id == x.ProcuradorId).Select(p => p.Nombre).FirstOrDefault(),
            db.Instancia.Where(i => i.Id == x.InstanciaActualId).Select(i => i.Nombre).FirstOrDefault(),
            db.Etapa.Where(t => t.Id == x.EtapaActualId).Select(t => t.Nombre).FirstOrDefault(),
            x.Anulado, x.MotivoAnulacion,
            x.NroCuenta,
            db.Comitente.Where(c => c.Id == x.ComitenteId).Select(c => c.Nombre).FirstOrDefault(),
            null, 0,
            db.ConceptoDeuda.Where(c => c.Id == x.ConceptoDeudaId).Select(c => c.Nombre).FirstOrDefault(),
            x.FechaSorteo, x.Sorteo))
            .FirstOrDefaultAsync(ct);
        if (e is null) return null;

        return e with
        {
            Demandado = await DemandadoAsync(id, ct),
            Codemandados = await db.ExpedientePersona.CountAsync(
                ep => ep.ExpedienteId == id && ep.Rol == RolesPersonaExpediente.Codemandado, ct)
        };
    }

    /// <summary>Nombre del demandado principal: razón social para empresas, "Apellido, Nombre" para personas.</summary>
    private Task<string?> DemandadoAsync(long id, CancellationToken ct) =>
        db.ExpedientePersona.Where(ep => ep.ExpedienteId == id && ep.Rol == RolesPersonaExpediente.Demandado)
            .OrderBy(ep => ep.Posicion)
            .Select(ep => ep.Persona.RazonSocial
                          ?? ((ep.Persona.Apellido ?? "") + (ep.Persona.Nombre != null ? ", " + ep.Persona.Nombre : "")))
            .FirstOrDefaultAsync(ct);

    public async Task<ResumenExpediente?> ResumenAsync(long id, CancellationToken ct = default)
    {
        var e = await db.Expediente.Where(x => x.Id == id).Select(x => new
        {
            x.NroCuenta, x.Nomenclatura, x.Dominio, x.Designacion, x.MatriculaRpi, x.MontoTitulo, x.MontoCapital,
            x.FechaTitulo, x.NroLiquidacion, x.Observaciones, x.ComitenteId, x.ConceptoDeudaId
        }).FirstOrDefaultAsync(ct);
        if (e is null) return null;

        var demandado = await DemandadoAsync(id, ct);
        var codemandados = await db.ExpedientePersona.CountAsync(
            ep => ep.ExpedienteId == id && ep.Rol == RolesPersonaExpediente.Codemandado, ct);
        var comitente = e.ComitenteId is int c ? await db.Comitente.Where(x => x.Id == c).Select(x => x.Nombre).FirstOrDefaultAsync(ct) : null;
        var concepto = e.ConceptoDeudaId is int k ? await db.ConceptoDeuda.Where(x => x.Id == k).Select(x => x.Nombre).FirstOrDefaultAsync(ct) : null;

        return new ResumenExpediente(demandado, codemandados, comitente, concepto, e.NroCuenta, e.Nomenclatura,
            e.Dominio, e.Designacion, e.MatriculaRpi, e.MontoTitulo, e.MontoCapital, e.FechaTitulo,
            e.NroLiquidacion, e.Observaciones);
    }

    public async Task<IReadOnlyList<PersonaExpediente>> PersonasAsync(long id, CancellationToken ct = default)
    {
        var filas = await db.ExpedientePersona.Where(ep => ep.ExpedienteId == id)
            .OrderBy(ep => ep.Rol).ThenBy(ep => ep.Posicion)
            .Select(ep => new
            {
                Nombre = (ep.Persona.Apellido ?? "") + (ep.Persona.Nombre != null ? ", " + ep.Persona.Nombre : "")
                         + (ep.Persona.RazonSocial != null ? ep.Persona.RazonSocial : ""),
                ep.Rol,
                ep.Persona.NroDocumento,
                ep.Persona.Cuit,
                PersonaId = ep.PersonaId
            }).ToListAsync(ct);

        var personaIds = filas.Select(f => f.PersonaId).Distinct().ToList();
        var domicilios = await db.Domicilio.Where(d => d.Principal && personaIds.Contains(d.PersonaId))
            .Select(d => new { d.PersonaId, d.Calle, d.Numero }).ToListAsync(ct);

        return filas.Select(f => new PersonaExpediente(
            f.Nombre.Trim().TrimEnd(','),
            RolesPersonaExpediente.Nombre(f.Rol),
            f.NroDocumento,
            f.Cuit,
            domicilios.Where(d => d.PersonaId == f.PersonaId)
                .Select(d => $"{d.Calle} {d.Numero}".Trim()).FirstOrDefault()))
            .ToList();
    }

    public async Task<IReadOnlyList<PeriodoExpediente>> PeriodosAsync(long id, CancellationToken ct = default) =>
        await db.PeriodoAdeudado.Where(p => p.ExpedienteId == id)
            .OrderBy(p => p.Anio).ThenBy(p => p.Cuota)
            .Select(p => new PeriodoExpediente(p.Anio, p.Cuota, p.Concepto, p.Vencimiento, p.Tributo, p.Recargo))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EtapaHistorial>> EtapasAsync(long id, CancellationToken ct = default) =>
        await db.EtapaExpediente.Where(x => x.ExpedienteId == id)
            .OrderByDescending(x => x.FechaInicio)
            .Select(x => new EtapaHistorial(x.Etapa.Nombre, x.FechaInicio, x.FechaVencimiento, x.Estado, x.Monto))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TareaExpediente>> TareasAsync(long id, CancellationToken ct = default) =>
        await db.Tarea.Where(t => t.ExpedienteId == id)
            .OrderBy(t => t.FechaVencimiento)
            .Select(t => new TareaExpediente(t.Id, t.Titulo, t.FechaVencimiento, t.Estado,
                db.Usuario.Where(u => u.Id == t.ResponsableUsuarioId).Select(u => u.Nombre + " " + u.Apellido).FirstOrDefault()))
            .ToListAsync(ct);
}

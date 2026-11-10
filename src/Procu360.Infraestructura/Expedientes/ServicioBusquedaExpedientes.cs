using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.MiDia;
using Procu360.Infraestructura.Persistencia;

namespace Procu360.Infraestructura.Expedientes;

public sealed record CriteriosBusqueda(
    string? Texto,
    int? ComitenteId,
    int? InstanciaId,
    bool SinMovimiento,
    bool IncluirDadosDeBaja,
    int Pagina = 1,
    int TamanioPagina = 25);

public sealed record FilaExpediente(
    long Id,
    int NroOrden,
    string? NroExpedienteJudicial,
    string? Caratula,
    string? Demandado,
    string? Concepto,
    string? Instancia,
    string? UltimaEtapa,
    DateOnly? FechaUltimoMovimiento,
    bool Anulado);

public sealed record ResultadoBusqueda(IReadOnlyList<FilaExpediente> Filas, int Total, int Pagina, int Paginas);

public sealed record OpcionCatalogo(int Id, string Nombre);

/// <summary>
/// Búsqueda de expedientes (docs/01 §3.2). Solo ve lo que permite el alcance de la persona;
/// el texto se busca en varios campos a la vez y los ceros a la izquierda no importan ("641" = "00641").
/// </summary>
public sealed class ServicioBusquedaExpedientes(Procu360DbContext db, AlcanceExpedientes alcance)
{
    public const int TamanioMaximo = 100;

    public async Task<ResultadoBusqueda> BuscarAsync(CriteriosBusqueda criterios, CancellationToken ct = default)
    {
        var enAlcance = criterios.IncluirDadosDeBaja
            ? await alcance.IdsIncluyendoBajasAsync(ct)
            : await alcance.IdsAsync(ct);

        var consulta = db.Expediente.Where(e => enAlcance.Contains(e.Id));
        if (!criterios.IncluirDadosDeBaja) consulta = consulta.Where(e => !e.Anulado);

        if (criterios.ComitenteId is int comitente) consulta = consulta.Where(e => e.ComitenteId == comitente);
        if (criterios.InstanciaId is int instancia) consulta = consulta.Where(e => e.InstanciaActualId == instancia);

        if (criterios.SinMovimiento)
        {
            var diasSin = await DiasSinMovimientoAsync(ct);
            var limite = ServicioMiDia.HoyEnCordoba().AddDays(-diasSin);
            consulta = consulta.Where(e => e.FechaUltimoMovimiento != null && e.FechaUltimoMovimiento < limite);
        }

        var texto = criterios.Texto?.Trim();
        if (!string.IsNullOrEmpty(texto))
        {
            consulta = AplicarTexto(consulta, texto);
        }

        var tamanio = Math.Clamp(criterios.TamanioPagina, 1, TamanioMaximo);
        var total = await consulta.CountAsync(ct);
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanio));
        var pagina = Math.Clamp(criterios.Pagina, 1, paginas);

        var filas = await consulta
            .OrderByDescending(e => e.NroOrden)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .Select(e => new FilaExpediente(
                e.Id,
                e.NroOrden,
                e.NroExpedienteJudicial,
                e.Caratula,
                db.ExpedientePersona.Where(ep => ep.ExpedienteId == e.Id && ep.Rol == 1)
                    .OrderBy(ep => ep.Posicion)
                    .Select(ep => ep.Persona.Apellido + ", " + ep.Persona.Nombre)
                    .FirstOrDefault(),
                db.ConceptoDeuda.Where(c => c.Id == e.ConceptoDeudaId).Select(c => c.Nombre).FirstOrDefault(),
                db.Instancia.Where(i => i.Id == e.InstanciaActualId).Select(i => i.Nombre).FirstOrDefault(),
                db.Etapa.Where(x => x.Id == e.EtapaActualId).Select(x => x.Nombre).FirstOrDefault(),
                e.FechaUltimoMovimiento,
                e.Anulado))
            .ToListAsync(ct);

        return new ResultadoBusqueda(filas, total, pagina, paginas);
    }

    /// <summary>
    /// Una sola caja de texto: número de orden, liquidación, cuenta, expediente judicial, dominio,
    /// nomenclatura, carátula, o documento/CUIT/apellido de una persona del expediente.
    /// </summary>
    private IQueryable<Persistencia.Entidades.Expediente> AplicarTexto(
        IQueryable<Persistencia.Entidades.Expediente> consulta, string texto)
    {
        var nroOrden = 0;
        var esNumero = texto.All(char.IsDigit) && int.TryParse(texto, out nroOrden);
        var personas = db.ExpedientePersona
            .Where(ep => ep.Persona.NroDocumento == texto
                || ep.Persona.Cuit == texto
                || (ep.Persona.Apellido != null && ep.Persona.Apellido.Contains(texto))
                || (ep.Persona.RazonSocial != null && ep.Persona.RazonSocial.Contains(texto)))
            .Select(ep => ep.ExpedienteId);

        return consulta.Where(e =>
            (esNumero && e.NroOrden == nroOrden)
            || (e.NroLiquidacion != null && e.NroLiquidacion == texto)
            || (e.NroCuenta != null && e.NroCuenta == texto)
            || (e.Dominio != null && e.Dominio == texto)
            || (e.NroExpedienteJudicial != null && e.NroExpedienteJudicial.Contains(texto))
            || (e.Nomenclatura != null && e.Nomenclatura.Contains(texto))
            || (e.Caratula != null && e.Caratula.Contains(texto))
            || personas.Contains(e.Id));
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ComitentesAsync(CancellationToken ct = default) =>
        await db.Comitente.Where(c => c.Activo).OrderBy(c => c.Nombre)
            .Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync(ct);

    public async Task<IReadOnlyList<OpcionCatalogo>> InstanciasAsync(CancellationToken ct = default) =>
        await db.Instancia.Where(i => i.Activa).OrderBy(i => i.Orden)
            .Select(i => new OpcionCatalogo(i.Id, i.Nombre)).ToListAsync(ct);

    private async Task<int> DiasSinMovimientoAsync(CancellationToken ct)
    {
        var valor = await db.ConfiguracionEstudio.Where(c => c.Clave == ServicioMiDia.ClaveDiasSinMovimiento)
            .Select(c => c.Valor).FirstOrDefaultAsync(ct);
        return int.TryParse(valor, out var n) ? n : 90;
    }
}

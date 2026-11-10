using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Título de deuda + causa judicial. Las columnas de estado actual reemplazan las ~30 subconsultas TOP 1 de V_Expedientes. Origen Monitorio: Expediente (columnas generales).
/// </summary>
public partial class Expediente
{
    public long Id { get; set; }

    public int EstudioId { get; set; }

    public int NroOrden { get; set; }

    public int? ComitenteId { get; set; }

    public int? ConceptoDeudaId { get; set; }

    public string? NroLiquidacion { get; set; }

    public DateOnly? FechaTitulo { get; set; }

    public DateOnly FechaAlta { get; set; }

    public decimal? MontoTitulo { get; set; }

    public decimal? MontoCapital { get; set; }

    public string? NroCuenta { get; set; }

    public string? Nomenclatura { get; set; }

    public string? Dominio { get; set; }

    public string? Designacion { get; set; }

    public string? MatriculaRpi { get; set; }

    public string? DomicilioObjeto { get; set; }

    public int? LocalidadObjetoId { get; set; }

    public int? RegistroPublicoId { get; set; }

    public string? NroResolucionMulta { get; set; }

    public DateOnly? FechaResolucion { get; set; }

    public int? JuzgadoId { get; set; }

    public string? NroExpedienteJudicial { get; set; }

    public string? Caratula { get; set; }

    public string? Recaratulacion { get; set; }

    public string? Sorteo { get; set; }

    public DateOnly? FechaSorteo { get; set; }

    public int? Cedula { get; set; }

    public int? ProcuradorId { get; set; }

    public int? PatrocinanteId { get; set; }

    public int? AbogadoInteriorId { get; set; }

    public int? NegociadorUsuarioId { get; set; }

    public int? DepuradorUsuarioId { get; set; }

    public bool Comparecio { get; set; }

    public DateOnly? FechaComparecio { get; set; }

    public string? AbogadoContraparte { get; set; }

    public string? DomicilioAbogadoContraparte { get; set; }

    public int? RegionId { get; set; }

    public int? CampaniaId { get; set; }

    public bool EsGranContribuyente { get; set; }

    public bool EsFisca { get; set; }

    public string? ObservacionesTitulo { get; set; }

    public string? Observaciones { get; set; }

    public string? ObservacionesAdministrativas { get; set; }

    public int? EtapaActualId { get; set; }

    public DateOnly? FechaEtapaActual { get; set; }

    public int? CautelarActualId { get; set; }

    public DateOnly? FechaCautelarActual { get; set; }

    public int? InstanciaActualId { get; set; }

    public DateOnly? FechaInstanciaActual { get; set; }

    public int? UbicacionActualId { get; set; }

    public DateOnly? FechaUltimoMovimiento { get; set; }

    public Guid? LegadoId { get; set; }

    public bool Anulado { get; set; }

    public DateTime? AnuladoUtc { get; set; }

    public int? AnuladoPor { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Campania? Campania { get; set; }

    public virtual Comitente? Comitente { get; set; }

    public virtual ConceptoDeuda? ConceptoDeuda { get; set; }

    public virtual Usuario? DepuradorUsuario { get; set; }

    public virtual ICollection<DevolucionCorreo> DevolucionCorreo { get; set; } = new List<DevolucionCorreo>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual ICollection<Embargo> Embargo { get; set; } = new List<Embargo>();

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<EstudioTitulo> EstudioTitulo { get; set; } = new List<EstudioTitulo>();

    public virtual Etapa? Etapa { get; set; }

    public virtual ICollection<EtapaExpediente> EtapaExpediente { get; set; } = new List<EtapaExpediente>();

    public virtual Etapa? EtapaNavigation { get; set; }

    public virtual ExpedienteActa? ExpedienteActa { get; set; }

    public virtual ICollection<ExpedienteActividad> ExpedienteActividad { get; set; } = new List<ExpedienteActividad>();

    public virtual ExpedienteFiscalizacion? ExpedienteFiscalizacion { get; set; }

    public virtual ICollection<ExpedienteInfraccion> ExpedienteInfraccion { get; set; } = new List<ExpedienteInfraccion>();

    public virtual ICollection<ExpedientePersona> ExpedientePersona { get; set; } = new List<ExpedientePersona>();

    public virtual Instancia? Instancia { get; set; }

    public virtual ICollection<InstanciaExpediente> InstanciaExpediente { get; set; } = new List<InstanciaExpediente>();

    public virtual Juzgado? Juzgado { get; set; }

    public virtual ICollection<Liquidacion> Liquidacion { get; set; } = new List<Liquidacion>();

    public virtual Localidad? LocalidadObjeto { get; set; }

    public virtual Usuario? NegociadorUsuario { get; set; }

    public virtual ICollection<NovedadJudicial> NovedadJudicial { get; set; } = new List<NovedadJudicial>();

    public virtual ICollection<PeriodoAdeudado> PeriodoAdeudado { get; set; } = new List<PeriodoAdeudado>();

    public virtual ICollection<PlanPago> PlanPago { get; set; } = new List<PlanPago>();

    public virtual Profesional? Profesional { get; set; }

    public virtual Profesional? Profesional1 { get; set; }

    public virtual Profesional? ProfesionalNavigation { get; set; }

    public virtual Region? Region { get; set; }

    public virtual RegistroPublico? RegistroPublico { get; set; }

    public virtual ICollection<Requerimiento> Requerimiento { get; set; } = new List<Requerimiento>();

    public virtual ICollection<Tarea> Tarea { get; set; } = new List<Tarea>();

    public virtual Ubicacion? Ubicacion { get; set; }

    public virtual ICollection<UbicacionExpediente> UbicacionExpediente { get; set; } = new List<UbicacionExpediente>();
}

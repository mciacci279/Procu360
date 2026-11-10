using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Raíz del tenant. Estado 1 activo, 2 suspendido. CopiaCatalogosJson registra qué catálogos se copiaron del Estudio origen al crearlo. 
/// </summary>
public partial class Estudio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Cuit { get; set; }

    public byte Estado { get; set; }

    public string ZonaHoraria { get; set; } = null!;

    public int RetencionAuditoriaDias { get; set; }

    public int? EstudioOrigenCopiaId { get; set; }

    public string? CopiaCatalogosJson { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<AccesoSoporte> AccesoSoporte { get; set; } = new List<AccesoSoporte>();

    public virtual ICollection<AuditoriaEvento> AuditoriaEvento { get; set; } = new List<AuditoriaEvento>();

    public virtual ICollection<Campania> Campania { get; set; } = new List<Campania>();

    public virtual ICollection<CodigoDevolucion> CodigoDevolucion { get; set; } = new List<CodigoDevolucion>();

    public virtual ICollection<Comitente> Comitente { get; set; } = new List<Comitente>();

    public virtual ICollection<ConceptoDeuda> ConceptoDeuda { get; set; } = new List<ConceptoDeuda>();

    public virtual ICollection<ConfiguracionEstudio> ConfiguracionEstudio { get; set; } = new List<ConfiguracionEstudio>();

    public virtual ICollection<DatoARevisar> DatoARevisar { get; set; } = new List<DatoARevisar>();

    public virtual ICollection<DevolucionCorreo> DevolucionCorreo { get; set; } = new List<DevolucionCorreo>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual ICollection<Domicilio> Domicilio { get; set; } = new List<Domicilio>();

    public virtual ICollection<Embargo> Embargo { get; set; } = new List<Embargo>();

    public virtual ICollection<EscalaHonorarios> EscalaHonorarios { get; set; } = new List<EscalaHonorarios>();

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio? EstudioOrigenCopia { get; set; }

    public virtual ICollection<EstudioTitulo> EstudioTitulo { get; set; } = new List<EstudioTitulo>();

    public virtual ICollection<EstudioUsuario> EstudioUsuario { get; set; } = new List<EstudioUsuario>();

    public virtual ICollection<EstudioUsuarioRol> EstudioUsuarioRol { get; set; } = new List<EstudioUsuarioRol>();

    public virtual ICollection<Etapa> Etapa { get; set; } = new List<Etapa>();

    public virtual ICollection<EtapaExpediente> EtapaExpediente { get; set; } = new List<EtapaExpediente>();

    public virtual ICollection<EtapaUsuarioNotificado> EtapaUsuarioNotificado { get; set; } = new List<EtapaUsuarioNotificado>();

    public virtual ICollection<Expediente> Expediente { get; set; } = new List<Expediente>();

    public virtual ICollection<ExpedienteActa> ExpedienteActa { get; set; } = new List<ExpedienteActa>();

    public virtual ICollection<ExpedienteActividad> ExpedienteActividad { get; set; } = new List<ExpedienteActividad>();

    public virtual ICollection<ExpedienteFiscalizacion> ExpedienteFiscalizacion { get; set; } = new List<ExpedienteFiscalizacion>();

    public virtual ICollection<ExpedienteInfraccion> ExpedienteInfraccion { get; set; } = new List<ExpedienteInfraccion>();

    public virtual ICollection<ExpedientePersona> ExpedientePersona { get; set; } = new List<ExpedientePersona>();

    public virtual ICollection<FeriadoEstudio> FeriadoEstudio { get; set; } = new List<FeriadoEstudio>();

    public virtual ICollection<InformePersona> InformePersona { get; set; } = new List<InformePersona>();

    public virtual ICollection<Infraccion> Infraccion { get; set; } = new List<Infraccion>();

    public virtual ICollection<Instancia> Instancia { get; set; } = new List<Instancia>();

    public virtual ICollection<InstanciaExpediente> InstanciaExpediente { get; set; } = new List<InstanciaExpediente>();

    public virtual ICollection<Estudio> InverseEstudioOrigenCopia { get; set; } = new List<Estudio>();

    public virtual ICollection<ItemPlanilla> ItemPlanilla { get; set; } = new List<ItemPlanilla>();

    public virtual ICollection<ItemPlanillaTope> ItemPlanillaTope { get; set; } = new List<ItemPlanillaTope>();

    public virtual ICollection<Juzgado> Juzgado { get; set; } = new List<Juzgado>();

    public virtual ICollection<Liquidacion> Liquidacion { get; set; } = new List<Liquidacion>();

    public virtual ICollection<LiquidacionLinea> LiquidacionLinea { get; set; } = new List<LiquidacionLinea>();

    public virtual ICollection<NovedadJudicial> NovedadJudicial { get; set; } = new List<NovedadJudicial>();

    public virtual ICollection<ParametroLiquidacion> ParametroLiquidacion { get; set; } = new List<ParametroLiquidacion>();

    public virtual ICollection<PeriodoAdeudado> PeriodoAdeudado { get; set; } = new List<PeriodoAdeudado>();

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();

    public virtual ICollection<PlanPago> PlanPago { get; set; } = new List<PlanPago>();

    public virtual ICollection<PlanillaJuzgado> PlanillaJuzgado { get; set; } = new List<PlanillaJuzgado>();

    public virtual ICollection<PlantillaEscrito> PlantillaEscrito { get; set; } = new List<PlantillaEscrito>();

    public virtual ICollection<PlantillaEscritoVersion> PlantillaEscritoVersion { get; set; } = new List<PlantillaEscritoVersion>();

    public virtual ICollection<Profesional> Profesional { get; set; } = new List<Profesional>();

    public virtual ICollection<Region> Region { get; set; } = new List<Region>();

    public virtual ICollection<RegistroPublico> RegistroPublico { get; set; } = new List<RegistroPublico>();

    public virtual ICollection<Requerimiento> Requerimiento { get; set; } = new List<Requerimiento>();

    public virtual ICollection<Rol> Rol { get; set; } = new List<Rol>();

    public virtual ICollection<RolPermiso> RolPermiso { get; set; } = new List<RolPermiso>();

    public virtual ICollection<RubroPlanilla> RubroPlanilla { get; set; } = new List<RubroPlanilla>();

    public virtual ICollection<SecuenciaEstudio> SecuenciaEstudio { get; set; } = new List<SecuenciaEstudio>();

    public virtual ICollection<Tarea> Tarea { get; set; } = new List<Tarea>();

    public virtual ICollection<TareaParticipante> TareaParticipante { get; set; } = new List<TareaParticipante>();

    public virtual ICollection<TasaInteres> TasaInteres { get; set; } = new List<TasaInteres>();

    public virtual ICollection<TipoEmbargo> TipoEmbargo { get; set; } = new List<TipoEmbargo>();

    public virtual ICollection<TipoTarea> TipoTarea { get; set; } = new List<TipoTarea>();

    public virtual ICollection<TrabajoProceso> TrabajoProceso { get; set; } = new List<TrabajoProceso>();

    public virtual ICollection<Ubicacion> Ubicacion { get; set; } = new List<Ubicacion>();

    public virtual ICollection<UbicacionExpediente> UbicacionExpediente { get; set; } = new List<UbicacionExpediente>();
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Procu360.Infraestructura.Persistencia.Entidades;

namespace Procu360.Infraestructura.Persistencia;

public partial class Procu360DbContext : DbContext
{
    public Procu360DbContext(DbContextOptions<Procu360DbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccesoSoporte> AccesoSoporte { get; set; }

    public virtual DbSet<ActividadEconomica> ActividadEconomica { get; set; }

    public virtual DbSet<AuditoriaEvento> AuditoriaEvento { get; set; }

    public virtual DbSet<Campania> Campania { get; set; }

    public virtual DbSet<CodigoDevolucion> CodigoDevolucion { get; set; }

    public virtual DbSet<Comitente> Comitente { get; set; }

    public virtual DbSet<ConceptoDeuda> ConceptoDeuda { get; set; }

    public virtual DbSet<CondicionIva> CondicionIva { get; set; }

    public virtual DbSet<ConfiguracionEstudio> ConfiguracionEstudio { get; set; }

    public virtual DbSet<DatoARevisar> DatoARevisar { get; set; }

    public virtual DbSet<DevolucionCorreo> DevolucionCorreo { get; set; }

    public virtual DbSet<Documento> Documento { get; set; }

    public virtual DbSet<Domicilio> Domicilio { get; set; }

    public virtual DbSet<Embargo> Embargo { get; set; }

    public virtual DbSet<EscalaHonorarios> EscalaHonorarios { get; set; }

    public virtual DbSet<EscritoGenerado> EscritoGenerado { get; set; }

    public virtual DbSet<Estudio> Estudio { get; set; }

    public virtual DbSet<EstudioTitulo> EstudioTitulo { get; set; }

    public virtual DbSet<EstudioUsuario> EstudioUsuario { get; set; }

    public virtual DbSet<EstudioUsuarioRol> EstudioUsuarioRol { get; set; }

    public virtual DbSet<Etapa> Etapa { get; set; }

    public virtual DbSet<EtapaExpediente> EtapaExpediente { get; set; }

    public virtual DbSet<EtapaUsuarioNotificado> EtapaUsuarioNotificado { get; set; }

    public virtual DbSet<Expediente> Expediente { get; set; }

    public virtual DbSet<ExpedienteActa> ExpedienteActa { get; set; }

    public virtual DbSet<ExpedienteActividad> ExpedienteActividad { get; set; }

    public virtual DbSet<ExpedienteFiscalizacion> ExpedienteFiscalizacion { get; set; }

    public virtual DbSet<ExpedienteInfraccion> ExpedienteInfraccion { get; set; }

    public virtual DbSet<ExpedientePersona> ExpedientePersona { get; set; }

    public virtual DbSet<Feriado> Feriado { get; set; }

    public virtual DbSet<FeriadoEstudio> FeriadoEstudio { get; set; }

    public virtual DbSet<IndiceTasaPasiva> IndiceTasaPasiva { get; set; }

    public virtual DbSet<InformePersona> InformePersona { get; set; }

    public virtual DbSet<Infraccion> Infraccion { get; set; }

    public virtual DbSet<Instancia> Instancia { get; set; }

    public virtual DbSet<InstanciaExpediente> InstanciaExpediente { get; set; }

    public virtual DbSet<ItemPlanilla> ItemPlanilla { get; set; }

    public virtual DbSet<ItemPlanillaTope> ItemPlanillaTope { get; set; }

    public virtual DbSet<Juzgado> Juzgado { get; set; }

    public virtual DbSet<Liquidacion> Liquidacion { get; set; }

    public virtual DbSet<LiquidacionLinea> LiquidacionLinea { get; set; }

    public virtual DbSet<Localidad> Localidad { get; set; }

    public virtual DbSet<NovedadJudicial> NovedadJudicial { get; set; }

    public virtual DbSet<ParametroLiquidacion> ParametroLiquidacion { get; set; }

    public virtual DbSet<PeriodoAdeudado> PeriodoAdeudado { get; set; }

    public virtual DbSet<Permiso> Permiso { get; set; }

    public virtual DbSet<Persona> Persona { get; set; }

    public virtual DbSet<PlanPago> PlanPago { get; set; }

    public virtual DbSet<PlanillaJuzgado> PlanillaJuzgado { get; set; }

    public virtual DbSet<PlantillaEscrito> PlantillaEscrito { get; set; }

    public virtual DbSet<PlantillaEscritoVersion> PlantillaEscritoVersion { get; set; }

    public virtual DbSet<Profesional> Profesional { get; set; }

    public virtual DbSet<Provincia> Provincia { get; set; }

    public virtual DbSet<Region> Region { get; set; }

    public virtual DbSet<RegistroPublico> RegistroPublico { get; set; }

    public virtual DbSet<Requerimiento> Requerimiento { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<RolPermiso> RolPermiso { get; set; }

    public virtual DbSet<RubroPlanilla> RubroPlanilla { get; set; }

    public virtual DbSet<SecuenciaEstudio> SecuenciaEstudio { get; set; }

    public virtual DbSet<Tarea> Tarea { get; set; }

    public virtual DbSet<TareaParticipante> TareaParticipante { get; set; }

    public virtual DbSet<TasaInteres> TasaInteres { get; set; }

    public virtual DbSet<TipoDocumento> TipoDocumento { get; set; }

    public virtual DbSet<TipoEmbargo> TipoEmbargo { get; set; }

    public virtual DbSet<TipoTarea> TipoTarea { get; set; }

    public virtual DbSet<TrabajoProceso> TrabajoProceso { get; set; }

    public virtual DbSet<Ubicacion> Ubicacion { get; set; }

    public virtual DbSet<UbicacionExpediente> UbicacionExpediente { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccesoSoporte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_AccesoSoporte");

            entity.ToTable("AccesoSoporte", "seg", tb => tb.HasComment("Acceso temporal y auditado de un superadministrador a los datos de un Estudio. "));

            entity.HasIndex(e => e.OtorgadoPor, "IX_seg_AccesoSoporte_OtorgadoPor");

            entity.HasIndex(e => e.UsuarioId, "IX_seg_AccesoSoporte_UsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_seg_AccesoSoporte_EstudioId_Id").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_AccesoSoporte_CreadoUtc");
            entity.Property(e => e.DesdeUtc).HasPrecision(3);
            entity.Property(e => e.HastaUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.AccesoSoporte)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_AccesoSoporte_Estudio");

            entity.HasOne(d => d.OtorgadoPorNavigation).WithMany(p => p.AccesoSoporteOtorgadoPorNavigation)
                .HasForeignKey(d => d.OtorgadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_AccesoSoporte_OtorgadoPor");

            entity.HasOne(d => d.Usuario).WithMany(p => p.AccesoSoporteUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_AccesoSoporte_UsuarioId");
        });

        modelBuilder.Entity<ActividadEconomica>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_ActividadEconomica");

            entity.ToTable("ActividadEconomica", "cat", tb => tb.HasComment("Nomenclador público de actividades (IIBB). Origen Monitorio: Actividad."));

            entity.HasIndex(e => e.Codigo, "UQ_cat_ActividadEconomica_Codigo").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_ActividadEconomica_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(500);
        });

        modelBuilder.Entity<AuditoriaEvento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_aud_AuditoriaEvento");

            entity.ToTable("AuditoriaEvento", "aud", tb => tb.HasComment("Solo inserción. EstudioId NULL = evento de plataforma. Accion 1 crear, 2 editar, 3 anular, 4 eliminar, 5 cambiar estado, 6 calcular, 7 generar, 8 exportar, 9 login, 10 login fallido, 11 logout, 12 cambiar permisos, 13 aprobar membresía, 14 acceso de soporte. Resultado 1 ok, 2 rechazado, 3 error. Origen Monitorio: Nueva: reemplaza a LogSistema, que no se migra."));

            entity.HasIndex(e => new { e.EstudioId, e.Entidad, e.EntidadId }, "IX_aud_AuditoriaEvento_EstudioId_Entidad_EntidadId");

            entity.HasIndex(e => new { e.EstudioId, e.FechaUtc }, "IX_aud_AuditoriaEvento_EstudioId_FechaUtc");

            entity.HasIndex(e => new { e.EstudioId, e.UsuarioId, e.FechaUtc }, "IX_aud_AuditoriaEvento_EstudioId_UsuarioId_FechaUtc");

            entity.HasIndex(e => e.UsuarioId, "IX_aud_AuditoriaEvento_UsuarioId");

            entity.Property(e => e.Entidad).HasMaxLength(60);
            entity.Property(e => e.EntidadId).HasMaxLength(40);
            entity.Property(e => e.FechaUtc).HasPrecision(3);
            entity.Property(e => e.Ip)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Modulo).HasMaxLength(40);
            entity.Property(e => e.Resultado).HasDefaultValue((byte)1, "DF_aud_AuditoriaEvento_Resultado");
            entity.Property(e => e.UserAgent).HasMaxLength(300);

            entity.HasOne(d => d.Estudio).WithMany(p => p.AuditoriaEvento)
                .HasForeignKey(d => d.EstudioId)
                .HasConstraintName("FK_aud_AuditoriaEvento_EstudioId");

            entity.HasOne(d => d.Usuario).WithMany(p => p.AuditoriaEvento)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_aud_AuditoriaEvento_UsuarioId");
        });

        modelBuilder.Entity<Campania>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Campania");

            entity.ToTable("Campania", "conf", tb => tb.HasComment("Origen Monitorio: Expediente.NroCampania/NombreCampania, ExpedienteCodigoDevolucion.Campania."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Campania_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Numero }, "UQ_conf_Campania_EstudioId_Numero").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Campania_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Numero).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Campania)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Campania_Estudio");
        });

        modelBuilder.Entity<CodigoDevolucion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_CodigoDevolucion");

            entity.ToTable("CodigoDevolucion", "conf", tb => tb.HasComment("Origen Monitorio: CodigoDevolucion."));

            entity.HasIndex(e => new { e.EstudioId, e.Codigo }, "UQ_conf_CodigoDevolucion_EstudioId_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_CodigoDevolucion_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_CodigoDevolucion_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_CodigoDevolucion_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.CodigoDevolucion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_CodigoDevolucion_Estudio");
        });

        modelBuilder.Entity<Comitente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Comitente");

            entity.ToTable("Comitente", "conf", tb => tb.HasComment("Acreedor/organismo para el que se procura (municipio, DGR, ente). Las fórmulas de liquidación se parametrizan por comitente. Origen Monitorio: DomicilioDGR."));

            entity.HasIndex(e => e.LocalidadId, "IX_conf_Comitente_LocalidadId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Comitente_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_Comitente_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Comitente_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_conf_Comitente_Activo");
            entity.Property(e => e.CodigoPostal).HasMaxLength(10);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Comitente_CreadoUtc");
            entity.Property(e => e.Cuit).HasMaxLength(13);
            entity.Property(e => e.Domicilio).HasMaxLength(250);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Comitente)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Comitente_Estudio");

            entity.HasOne(d => d.Localidad).WithMany(p => p.Comitente)
                .HasForeignKey(d => d.LocalidadId)
                .HasConstraintName("FK_conf_Comitente_LocalidadId");
        });

        modelBuilder.Entity<ConceptoDeuda>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_ConceptoDeuda");

            entity.ToTable("ConceptoDeuda", "conf", tb => tb.HasComment("Impuesto/tasa reclamada. TextoEscrito admite variables {{...}}. Origen Monitorio: Impuesto."));

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId }, "IX_conf_ConceptoDeuda_EstudioId_ComitenteId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_ConceptoDeuda_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_ConceptoDeuda_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_ConceptoDeuda_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_conf_ConceptoDeuda_Activo");
            entity.Property(e => e.ConsideraPeriodos).HasDefaultValue(true, "DF_conf_ConceptoDeuda_ConsideraPeriodos");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_ConceptoDeuda_CreadoUtc");
            entity.Property(e => e.Item).HasMaxLength(50);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.TextoEscrito).HasMaxLength(4000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ConceptoDeuda)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_ConceptoDeuda_Estudio");

            entity.HasOne(d => d.Comitente).WithMany(p => p.ConceptoDeuda)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_conf_ConceptoDeuda_ComitenteId");
        });

        modelBuilder.Entity<CondicionIva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_CondicionIva");

            entity.ToTable("CondicionIva", "cat", tb => tb.HasComment("Origen Monitorio: Iva."));

            entity.HasIndex(e => e.Nombre, "UQ_cat_CondicionIva_Nombre").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_CondicionIva_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.LetraComprobante)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<ConfiguracionEstudio>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.Clave }).HasName("PK_conf_ConfiguracionEstudio");

            entity.ToTable("ConfiguracionEstudio", "conf", tb => tb.HasComment("Parámetros generales no modelados como columna. Tipo 1 texto, 2 entero, 3 decimal, 4 fecha, 5 booleano. Origen Monitorio: Configuracion."));

            entity.Property(e => e.Clave).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_ConfiguracionEstudio_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Valor).HasMaxLength(1000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ConfiguracionEstudio)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_ConfiguracionEstudio_Estudio");
        });

        modelBuilder.Entity<DatoARevisar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_mig_DatoARevisar");

            entity.ToTable("DatoARevisar", "mig", tb => tb.HasComment("Cola de la pantalla Datos a revisar. Estado 1 pendiente, 2 resuelto, 3 descartado. "));

            entity.HasIndex(e => new { e.EstudioId, e.Estado, e.Tipo }, "IX_mig_DatoARevisar_EstudioId_Estado_Tipo");

            entity.HasIndex(e => e.ResueltoPor, "IX_mig_DatoARevisar_ResueltoPor");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_mig_DatoARevisar_EstudioId_Id").IsUnique();

            entity.Property(e => e.Campo).HasMaxLength(60);
            entity.Property(e => e.Comentario).HasMaxLength(1000);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_mig_DatoARevisar_CreadoUtc");
            entity.Property(e => e.Entidad).HasMaxLength(60);
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_mig_DatoARevisar_Estado");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.ResueltoUtc).HasPrecision(3);
            entity.Property(e => e.TablaOrigen).HasMaxLength(60);
            entity.Property(e => e.Tipo).HasMaxLength(60);
            entity.Property(e => e.ValorOriginal).HasMaxLength(4000);
            entity.Property(e => e.ValorPropuesto).HasMaxLength(4000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.DatoARevisar)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mig_DatoARevisar_Estudio");

            entity.HasOne(d => d.ResueltoPorNavigation).WithMany(p => p.DatoARevisar)
                .HasForeignKey(d => d.ResueltoPor)
                .HasConstraintName("FK_mig_DatoARevisar_ResueltoPor");
        });

        modelBuilder.Entity<DevolucionCorreo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_DevolucionCorreo");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: ExpedienteCodigoDevolucion."));

            entity.HasIndex(e => new { e.EstudioId, e.CampaniaId }, "IX_dbo_DevolucionCorreo_EstudioId_CampaniaId");

            entity.HasIndex(e => new { e.EstudioId, e.CodigoDevolucionId }, "IX_dbo_DevolucionCorreo_EstudioId_CodigoDevolucionId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_DevolucionCorreo_EstudioId_ExpedienteId");

            entity.HasIndex(e => e.UsuarioId, "IX_dbo_DevolucionCorreo_UsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_DevolucionCorreo_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_DevolucionCorreo_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_DevolucionCorreo_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.NroCaso).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.DevolucionCorreo)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_DevolucionCorreo_Estudio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.DevolucionCorreo)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_dbo_DevolucionCorreo_UsuarioId");

            entity.HasOne(d => d.Campania).WithMany(p => p.DevolucionCorreo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.CampaniaId })
                .HasConstraintName("FK_dbo_DevolucionCorreo_CampaniaId");

            entity.HasOne(d => d.CodigoDevolucion).WithMany(p => p.DevolucionCorreo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.CodigoDevolucionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_DevolucionCorreo_CodigoDevolucionId");

            entity.HasOne(d => d.Expediente).WithMany(p => p.DevolucionCorreo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_DevolucionCorreo_ExpedienteId");
        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Documento");

            entity.ToTable(tb => tb.HasComment("Tipo 1 adjunto, 2 escrito generado, 3 liquidación exportada, 4 plantilla, 5 importación. Contenido en almacenamiento de archivos con prefijo de Estudio; sin borrado automático: se conserva hasta que un usuario con permiso lo elimina. "));

            entity.HasIndex(e => e.EliminadoPor, "IX_dbo_Documento_EliminadoPor");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_Documento_EstudioId_ExpedienteId").HasFilter("([Eliminado]=(0))");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Documento_EstudioId_Id").IsUnique();

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Documento_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.EliminadoUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.MotivoEliminacion).HasMaxLength(500);
            entity.Property(e => e.NombreArchivo).HasMaxLength(260);
            entity.Property(e => e.RutaAlmacenamiento).HasMaxLength(500);
            entity.Property(e => e.Sha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.EliminadoPorNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.EliminadoPor)
                .HasConstraintName("FK_dbo_Documento_EliminadoPor");

            entity.HasOne(d => d.Estudio).WithMany(p => p.Documento)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Documento_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.Documento)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .HasConstraintName("FK_dbo_Documento_ExpedienteId");
        });

        modelBuilder.Entity<Domicilio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Domicilio");

            entity.ToTable(tb => tb.HasComment("Tipo 1 real, 2 fiscal, 3 constituido, 4 postal. Un solo principal por persona (índice filtrado). Origen Monitorio: Domicilio."));

            entity.HasIndex(e => new { e.EstudioId, e.PersonaId }, "IX_dbo_Domicilio_EstudioId_PersonaId");

            entity.HasIndex(e => e.LocalidadId, "IX_dbo_Domicilio_LocalidadId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Domicilio_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.PersonaId }, "UX_dbo_Domicilio_EstudioId_PersonaId_Principal")
                .IsUnique()
                .HasFilter("([Principal]=(1))");

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Domicilio_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Barrio).HasMaxLength(100);
            entity.Property(e => e.Calle).HasMaxLength(150);
            entity.Property(e => e.CodigoPostal).HasMaxLength(10);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Domicilio_CreadoUtc");
            entity.Property(e => e.Depto).HasMaxLength(10);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Piso).HasMaxLength(10);
            entity.Property(e => e.Tipo).HasDefaultValue((byte)1, "DF_dbo_Domicilio_Tipo");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Domicilio)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Domicilio_Estudio");

            entity.HasOne(d => d.Localidad).WithMany(p => p.Domicilio)
                .HasForeignKey(d => d.LocalidadId)
                .HasConstraintName("FK_dbo_Domicilio_LocalidadId");

            entity.HasOne(d => d.Persona).WithOne(p => p.Domicilio)
                .HasPrincipalKey<Persona>(p => new { p.EstudioId, p.Id })
                .HasForeignKey<Domicilio>(d => new { d.EstudioId, d.PersonaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Domicilio_PersonaId");
        });

        modelBuilder.Entity<Embargo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Embargo");

            entity.ToTable(tb => tb.HasComment("Estado 1 solicitado, 2 trabado, 3 levantado, 4 sin efecto (nuevo: el legado no tenía estado; se migra como 2). DetalleJson guarda los datos propios de cada clase (marca, modelo, chasis, sticker; lote, manzana, PH; banco, CBU; etc.). Origen Monitorio: EmbargoExpediente."));

            entity.HasIndex(e => new { e.EstudioId, e.Dominio }, "IX_dbo_Embargo_EstudioId_Dominio").HasFilter("([Dominio] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_Embargo_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.MartilleroId }, "IX_dbo_Embargo_EstudioId_MartilleroId");

            entity.HasIndex(e => new { e.EstudioId, e.RegistroPublicoId }, "IX_dbo_Embargo_EstudioId_RegistroPublicoId");

            entity.HasIndex(e => new { e.EstudioId, e.TipoEmbargoId }, "IX_dbo_Embargo_EstudioId_TipoEmbargoId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Embargo_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Embargo_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Autorizados).HasMaxLength(500);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Embargo_CreadoUtc");
            entity.Property(e => e.DireccionRegistro).HasMaxLength(200);
            entity.Property(e => e.Dominio).HasMaxLength(20);
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_dbo_Embargo_Estado");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nomenclatura).HasMaxLength(200);
            entity.Property(e => e.NroRegistro).HasMaxLength(200);
            entity.Property(e => e.Observaciones).HasMaxLength(4000);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Secretaria).HasMaxLength(200);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Embargo)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Embargo_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.Embargo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Embargo_ExpedienteId");

            entity.HasOne(d => d.Profesional).WithMany(p => p.Embargo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.MartilleroId })
                .HasConstraintName("FK_dbo_Embargo_MartilleroId");

            entity.HasOne(d => d.RegistroPublico).WithMany(p => p.Embargo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RegistroPublicoId })
                .HasConstraintName("FK_dbo_Embargo_RegistroPublicoId");

            entity.HasOne(d => d.TipoEmbargo).WithMany(p => p.Embargo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.TipoEmbargoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Embargo_TipoEmbargoId");
        });

        modelBuilder.Entity<EscalaHonorarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_EscalaHonorarios");

            entity.ToTable("EscalaHonorarios", "liq", tb => tb.HasComment("Tramos de la escala (9 % … 3 % en el legado, fijos en código). "));

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId, e.VigenteDesde, e.BaseDesde }, "UQ_liq_EscalaHonorarios_EstudioId_ComitenteId_VigenteDesde_BaseDesde").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_EscalaHonorarios_EstudioId_Id").IsUnique();

            entity.Property(e => e.BaseDesde).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BaseHasta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_EscalaHonorarios_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(7, 4)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.EscalaHonorarios)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_EscalaHonorarios_Estudio");

            entity.HasOne(d => d.Comitente).WithMany(p => p.EscalaHonorarios)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_liq_EscalaHonorarios_ComitenteId");
        });

        modelBuilder.Entity<EscritoGenerado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_EscritoGenerado");

            entity.ToTable(tb => tb.HasComment("Estado 1 generado, 2 presentado, 3 anulado. Los migrados no tienen documento (el legado no lo guardaba). Origen Monitorio: ExpedienteEtapaEscrito + ExpedienteInstanciaEscrito."));

            entity.HasIndex(e => new { e.EstudioId, e.DocumentoId }, "IX_dbo_EscritoGenerado_EstudioId_DocumentoId");

            entity.HasIndex(e => new { e.EstudioId, e.EtapaId }, "IX_dbo_EscritoGenerado_EstudioId_EtapaId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.FechaUtc }, "IX_dbo_EscritoGenerado_EstudioId_ExpedienteId_FechaUtc");

            entity.HasIndex(e => new { e.EstudioId, e.InstanciaId }, "IX_dbo_EscritoGenerado_EstudioId_InstanciaId");

            entity.HasIndex(e => new { e.EstudioId, e.PlantillaEscritoVersionId }, "IX_dbo_EscritoGenerado_EstudioId_PlantillaEscritoVersionId");

            entity.HasIndex(e => new { e.EstudioId, e.TrabajoProcesoId }, "IX_dbo_EscritoGenerado_EstudioId_TrabajoProcesoId");

            entity.HasIndex(e => e.UsuarioId, "IX_dbo_EscritoGenerado_UsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_EscritoGenerado_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_EscritoGenerado_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_EscritoGenerado_CreadoUtc");
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_dbo_EscritoGenerado_Estado");
            entity.Property(e => e.FechaUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.EscritoGenerado)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EscritoGenerado_Estudio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EscritoGenerado)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_dbo_EscritoGenerado_UsuarioId");

            entity.HasOne(d => d.Documento).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.DocumentoId })
                .HasConstraintName("FK_dbo_EscritoGenerado_DocumentoId");

            entity.HasOne(d => d.Etapa).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaId })
                .HasConstraintName("FK_dbo_EscritoGenerado_EtapaId");

            entity.HasOne(d => d.Expediente).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EscritoGenerado_ExpedienteId");

            entity.HasOne(d => d.Instancia).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InstanciaId })
                .HasConstraintName("FK_dbo_EscritoGenerado_InstanciaId");

            entity.HasOne(d => d.PlantillaEscritoVersion).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PlantillaEscritoVersionId })
                .HasConstraintName("FK_dbo_EscritoGenerado_PlantillaEscritoVersionId");

            entity.HasOne(d => d.TrabajoProceso).WithMany(p => p.EscritoGenerado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.TrabajoProcesoId })
                .HasConstraintName("FK_dbo_EscritoGenerado_TrabajoProcesoId");
        });

        modelBuilder.Entity<Estudio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_Estudio");

            entity.ToTable("Estudio", "seg", tb => tb.HasComment("Raíz del tenant. Estado 1 activo, 2 suspendido. CopiaCatalogosJson registra qué catálogos se copiaron del Estudio origen al crearlo. "));

            entity.HasIndex(e => e.EstudioOrigenCopiaId, "IX_seg_Estudio_EstudioOrigenCopiaId");

            entity.HasIndex(e => e.Nombre, "UQ_seg_Estudio_Nombre").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_Estudio_CreadoUtc");
            entity.Property(e => e.Cuit).HasMaxLength(13);
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_seg_Estudio_Estado");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.RetencionAuditoriaDias).HasDefaultValue(365, "DF_seg_Estudio_RetencionAuditoriaDias");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ZonaHoraria)
                .HasMaxLength(50)
                .HasDefaultValue("America/Argentina/Cordoba", "DF_seg_Estudio_ZonaHoraria");

            entity.HasOne(d => d.EstudioOrigenCopia).WithMany(p => p.InverseEstudioOrigenCopia)
                .HasForeignKey(d => d.EstudioOrigenCopiaId)
                .HasConstraintName("FK_seg_Estudio_EstudioOrigenCopiaId");
        });

        modelBuilder.Entity<EstudioTitulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_EstudioTitulo");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: EstudioTitulo."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_EstudioTitulo_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_EstudioTitulo_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_EstudioTitulo_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_EstudioTitulo_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.EstudioTitulo)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EstudioTitulo_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.EstudioTitulo)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EstudioTitulo_ExpedienteId");
        });

        modelBuilder.Entity<EstudioUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_EstudioUsuario");

            entity.ToTable("EstudioUsuario", "seg", tb => tb.HasComment("Membresía. Estado 1 invitado, 2 pendiente de aprobación (el email ya pertenece a otro Estudio), 3 activo, 4 desactivado, 5 rechazado. "));

            entity.HasIndex(e => e.AprobadoPor, "IX_seg_EstudioUsuario_AprobadoPor");

            entity.HasIndex(e => new { e.EstudioId, e.ProfesionalId }, "IX_seg_EstudioUsuario_EstudioId_ProfesionalId");

            entity.HasIndex(e => e.InvitadoPor, "IX_seg_EstudioUsuario_InvitadoPor");

            entity.HasIndex(e => e.UsuarioId, "IX_seg_EstudioUsuario_UsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_seg_EstudioUsuario_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.UsuarioId }, "UQ_seg_EstudioUsuario_EstudioId_UsuarioId").IsUnique();

            entity.Property(e => e.AprobadoUtc).HasPrecision(3);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_EstudioUsuario_CreadoUtc");
            entity.Property(e => e.InvitadoUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(500);
            entity.Property(e => e.UltimoAccesoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.AprobadoPorNavigation).WithMany(p => p.EstudioUsuarioAprobadoPorNavigation)
                .HasForeignKey(d => d.AprobadoPor)
                .HasConstraintName("FK_seg_EstudioUsuario_AprobadoPor");

            entity.HasOne(d => d.Estudio).WithMany(p => p.EstudioUsuario)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_EstudioUsuario_Estudio");

            entity.HasOne(d => d.InvitadoPorNavigation).WithMany(p => p.EstudioUsuarioInvitadoPorNavigation)
                .HasForeignKey(d => d.InvitadoPor)
                .HasConstraintName("FK_seg_EstudioUsuario_InvitadoPor");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EstudioUsuarioUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_EstudioUsuario_UsuarioId");

            entity.HasOne(d => d.Profesional).WithMany(p => p.EstudioUsuario)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ProfesionalId })
                .HasConstraintName("FK_seg_EstudioUsuario_ProfesionalId");
        });

        modelBuilder.Entity<EstudioUsuarioRol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_EstudioUsuarioRol");

            entity.ToTable("EstudioUsuarioRol", "seg");

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId }, "IX_seg_EstudioUsuarioRol_EstudioId_ComitenteId");

            entity.HasIndex(e => new { e.EstudioId, e.RolId }, "IX_seg_EstudioUsuarioRol_EstudioId_RolId");

            entity.HasIndex(e => new { e.EstudioId, e.EstudioUsuarioId, e.RolId, e.ComitenteId }, "UQ_seg_EstudioUsuarioRol_EstudioId_EstudioUsuarioId_RolId_ComitenteId").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_seg_EstudioUsuarioRol_EstudioId_Id").IsUnique();

            entity.HasOne(d => d.Estudio).WithMany(p => p.EstudioUsuarioRol)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_EstudioUsuarioRol_Estudio");

            entity.HasOne(d => d.Comitente).WithMany(p => p.EstudioUsuarioRol)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_seg_EstudioUsuarioRol_ComitenteId");

            entity.HasOne(d => d.EstudioUsuario).WithMany(p => p.EstudioUsuarioRol)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EstudioUsuarioId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_EstudioUsuarioRol_EstudioUsuarioId");

            entity.HasOne(d => d.Rol).WithMany(p => p.EstudioUsuarioRol)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RolId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_EstudioUsuarioRol_RolId");
        });

        modelBuilder.Entity<Etapa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Etapa");

            entity.ToTable("Etapa", "conf", tb => tb.HasComment("Flujo 1 principal, 2 cautelar. HitoSistema 1 inicio de demanda, 2 demanda presentada (reemplaza el código fijo M100 y Configuracion.DemPresID), 3 sentencia, 4 ejecución. TareaParaUsuarios 0 ninguno, 1 responsable, 2 usuarios seleccionados, 3 todos. Origen Monitorio: Etapa + Cautelar."));

            entity.HasIndex(e => new { e.EstudioId, e.EtapaAnteriorId }, "IX_conf_Etapa_EstudioId_EtapaAnteriorId");

            entity.HasIndex(e => new { e.EstudioId, e.EtapaSiguienteId }, "IX_conf_Etapa_EstudioId_EtapaSiguienteId");

            entity.HasIndex(e => new { e.EstudioId, e.HitoSistema }, "IX_conf_Etapa_EstudioId_HitoSistema").HasFilter("([HitoSistema] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.InstanciaId }, "IX_conf_Etapa_EstudioId_InstanciaId").HasFilter("([InstanciaId] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.Flujo, e.Nombre }, "UQ_conf_Etapa_EstudioId_Flujo_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Etapa_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Etapa_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_conf_Etapa_Activa");
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Etapa_CreadoUtc");
            entity.Property(e => e.DiasHabiles).HasDefaultValue(true, "DF_conf_Etapa_DiasHabiles");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Etapa)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Etapa_Estudio");

            entity.HasOne(d => d.EtapaNavigation).WithMany(p => p.InverseEtapaNavigation)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaAnteriorId })
                .HasConstraintName("FK_conf_Etapa_EtapaAnteriorId");

            entity.HasOne(d => d.Etapa1).WithMany(p => p.InverseEtapa1)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaSiguienteId })
                .HasConstraintName("FK_conf_Etapa_EtapaSiguienteId");

            entity.HasOne(d => d.Instancia).WithMany(p => p.Etapa)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InstanciaId })
                .HasConstraintName("FK_conf_Etapa_InstanciaId");
        });

        modelBuilder.Entity<EtapaExpediente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_EtapaExpediente");

            entity.ToTable(tb => tb.HasComment("Historial de etapas de ambos flujos. Estado 1 alta, 2 cumplimentada, 3 suspendida, 4 baja (en el legado 0 = alta). FechaVencimiento se calcula con días hábiles y feriados. Origen Monitorio: EtapaExpediente + CautelarExpediente."));

            entity.HasIndex(e => new { e.EstudioId, e.Estado, e.FechaVencimiento }, "IX_dbo_EtapaExpediente_EstudioId_Estado_FechaVencimiento").HasFilter("([Estado] IN ((1), (3)))");

            entity.HasIndex(e => new { e.EstudioId, e.EtapaId }, "IX_dbo_EtapaExpediente_EstudioId_EtapaId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.FechaInicio }, "IX_dbo_EtapaExpediente_EstudioId_ExpedienteId_FechaInicio");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_EtapaExpediente_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_EtapaExpediente_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_EtapaExpediente_CreadoUtc");
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_dbo_EtapaExpediente_Estado");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.EtapaExpediente)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EtapaExpediente_Estudio");

            entity.HasOne(d => d.Etapa).WithMany(p => p.EtapaExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EtapaExpediente_EtapaId");

            entity.HasOne(d => d.Expediente).WithMany(p => p.EtapaExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_EtapaExpediente_ExpedienteId");
        });

        modelBuilder.Entity<EtapaUsuarioNotificado>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.EtapaId, e.UsuarioId }).HasName("PK_conf_EtapaUsuarioNotificado");

            entity.ToTable("EtapaUsuarioNotificado", "conf", tb => tb.HasComment("Origen Monitorio: EtapaAgendaUsuario."));

            entity.HasIndex(e => e.UsuarioId, "IX_conf_EtapaUsuarioNotificado_UsuarioId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.EtapaUsuarioNotificado)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_EtapaUsuarioNotificado_Estudio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EtapaUsuarioNotificado)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_EtapaUsuarioNotificado_UsuarioId");

            entity.HasOne(d => d.Etapa).WithMany(p => p.EtapaUsuarioNotificado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_EtapaUsuarioNotificado_EtapaId");
        });

        modelBuilder.Entity<Expediente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Expediente");

            entity.ToTable(tb => tb.HasComment("Título de deuda + causa judicial. Las columnas de estado actual reemplazan las ~30 subconsultas TOP 1 de V_Expedientes. Origen Monitorio: Expediente (columnas generales)."));

            entity.HasIndex(e => e.DepuradorUsuarioId, "IX_dbo_Expediente_DepuradorUsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.AbogadoInteriorId }, "IX_dbo_Expediente_EstudioId_AbogadoInteriorId");

            entity.HasIndex(e => new { e.EstudioId, e.CampaniaId }, "IX_dbo_Expediente_EstudioId_CampaniaId");

            entity.HasIndex(e => new { e.EstudioId, e.CautelarActualId }, "IX_dbo_Expediente_EstudioId_CautelarActualId");

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId }, "IX_dbo_Expediente_EstudioId_ComitenteId");

            entity.HasIndex(e => new { e.EstudioId, e.ConceptoDeudaId }, "IX_dbo_Expediente_EstudioId_ConceptoDeudaId");

            entity.HasIndex(e => new { e.EstudioId, e.Dominio }, "IX_dbo_Expediente_EstudioId_Dominio").HasFilter("([Dominio] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.EtapaActualId }, "IX_dbo_Expediente_EstudioId_EtapaActualId");

            entity.HasIndex(e => new { e.EstudioId, e.InstanciaActualId, e.FechaUltimoMovimiento }, "IX_dbo_Expediente_EstudioId_InstanciaActualId_FechaUltimoMovimiento").HasFilter("([Anulado]=(0))");

            entity.HasIndex(e => new { e.EstudioId, e.JuzgadoId }, "IX_dbo_Expediente_EstudioId_JuzgadoId");

            entity.HasIndex(e => new { e.EstudioId, e.Nomenclatura }, "IX_dbo_Expediente_EstudioId_Nomenclatura");

            entity.HasIndex(e => new { e.EstudioId, e.NroCuenta }, "IX_dbo_Expediente_EstudioId_NroCuenta");

            entity.HasIndex(e => new { e.EstudioId, e.NroExpedienteJudicial }, "IX_dbo_Expediente_EstudioId_NroExpedienteJudicial").HasFilter("([NroExpedienteJudicial] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.NroLiquidacion }, "IX_dbo_Expediente_EstudioId_NroLiquidacion");

            entity.HasIndex(e => new { e.EstudioId, e.PatrocinanteId }, "IX_dbo_Expediente_EstudioId_PatrocinanteId");

            entity.HasIndex(e => new { e.EstudioId, e.ProcuradorId }, "IX_dbo_Expediente_EstudioId_ProcuradorId");

            entity.HasIndex(e => new { e.EstudioId, e.RegionId }, "IX_dbo_Expediente_EstudioId_RegionId");

            entity.HasIndex(e => new { e.EstudioId, e.RegistroPublicoId }, "IX_dbo_Expediente_EstudioId_RegistroPublicoId");

            entity.HasIndex(e => new { e.EstudioId, e.UbicacionActualId }, "IX_dbo_Expediente_EstudioId_UbicacionActualId");

            entity.HasIndex(e => e.LocalidadObjetoId, "IX_dbo_Expediente_LocalidadObjetoId");

            entity.HasIndex(e => e.NegociadorUsuarioId, "IX_dbo_Expediente_NegociadorUsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Expediente_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.NroOrden }, "UQ_dbo_Expediente_EstudioId_NroOrden").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Expediente_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.AbogadoContraparte).HasMaxLength(250);
            entity.Property(e => e.AnuladoUtc).HasPrecision(3);
            entity.Property(e => e.Caratula).HasMaxLength(400);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Expediente_CreadoUtc");
            entity.Property(e => e.Designacion).HasMaxLength(100);
            entity.Property(e => e.DomicilioAbogadoContraparte).HasMaxLength(250);
            entity.Property(e => e.DomicilioObjeto).HasMaxLength(250);
            entity.Property(e => e.Dominio).HasMaxLength(20);
            entity.Property(e => e.MatriculaRpi).HasMaxLength(50);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.MontoCapital).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoTitulo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(500);
            entity.Property(e => e.Nomenclatura).HasMaxLength(50);
            entity.Property(e => e.NroCuenta).HasMaxLength(50);
            entity.Property(e => e.NroExpedienteJudicial).HasMaxLength(50);
            entity.Property(e => e.NroLiquidacion).HasMaxLength(50);
            entity.Property(e => e.NroResolucionMulta).HasMaxLength(50);
            entity.Property(e => e.Observaciones).HasMaxLength(4000);
            entity.Property(e => e.ObservacionesAdministrativas).HasMaxLength(4000);
            entity.Property(e => e.ObservacionesTitulo).HasMaxLength(4000);
            entity.Property(e => e.Recaratulacion).HasMaxLength(400);
            entity.Property(e => e.Sorteo).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.DepuradorUsuario).WithMany(p => p.ExpedienteDepuradorUsuario)
                .HasForeignKey(d => d.DepuradorUsuarioId)
                .HasConstraintName("FK_dbo_Expediente_DepuradorUsuarioId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.Expediente)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Expediente_Estudio");

            entity.HasOne(d => d.LocalidadObjeto).WithMany(p => p.Expediente)
                .HasForeignKey(d => d.LocalidadObjetoId)
                .HasConstraintName("FK_dbo_Expediente_LocalidadObjetoId");

            entity.HasOne(d => d.NegociadorUsuario).WithMany(p => p.ExpedienteNegociadorUsuario)
                .HasForeignKey(d => d.NegociadorUsuarioId)
                .HasConstraintName("FK_dbo_Expediente_NegociadorUsuarioId");

            entity.HasOne(d => d.Profesional).WithMany(p => p.ExpedienteProfesional)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.AbogadoInteriorId })
                .HasConstraintName("FK_dbo_Expediente_AbogadoInteriorId");

            entity.HasOne(d => d.Campania).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.CampaniaId })
                .HasConstraintName("FK_dbo_Expediente_CampaniaId");

            entity.HasOne(d => d.Etapa).WithMany(p => p.ExpedienteEtapa)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.CautelarActualId })
                .HasConstraintName("FK_dbo_Expediente_CautelarActualId");

            entity.HasOne(d => d.Comitente).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_dbo_Expediente_ComitenteId");

            entity.HasOne(d => d.ConceptoDeuda).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ConceptoDeudaId })
                .HasConstraintName("FK_dbo_Expediente_ConceptoDeudaId");

            entity.HasOne(d => d.EtapaNavigation).WithMany(p => p.ExpedienteEtapaNavigation)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaActualId })
                .HasConstraintName("FK_dbo_Expediente_EtapaActualId");

            entity.HasOne(d => d.Instancia).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InstanciaActualId })
                .HasConstraintName("FK_dbo_Expediente_InstanciaActualId");

            entity.HasOne(d => d.Juzgado).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.JuzgadoId })
                .HasConstraintName("FK_dbo_Expediente_JuzgadoId");

            entity.HasOne(d => d.ProfesionalNavigation).WithMany(p => p.ExpedienteProfesionalNavigation)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PatrocinanteId })
                .HasConstraintName("FK_dbo_Expediente_PatrocinanteId");

            entity.HasOne(d => d.Profesional1).WithMany(p => p.ExpedienteProfesional1)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ProcuradorId })
                .HasConstraintName("FK_dbo_Expediente_ProcuradorId");

            entity.HasOne(d => d.Region).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RegionId })
                .HasConstraintName("FK_dbo_Expediente_RegionId");

            entity.HasOne(d => d.RegistroPublico).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RegistroPublicoId })
                .HasConstraintName("FK_dbo_Expediente_RegistroPublicoId");

            entity.HasOne(d => d.Ubicacion).WithMany(p => p.Expediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.UbicacionActualId })
                .HasConstraintName("FK_dbo_Expediente_UbicacionActualId");
        });

        modelBuilder.Entity<ExpedienteActa>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.ExpedienteId }).HasName("PK_dbo_ExpedienteActa");

            entity.ToTable(tb => tb.HasComment("1:1 opcional. Actas de infracción (multas). Origen Monitorio: Expediente (FechaLabrado…FechaTramite)."));

            entity.HasIndex(e => new { e.EstudioId, e.NroCaso }, "IX_dbo_ExpedienteActa_EstudioId_NroCaso").HasFilter("([NroCaso] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.NroTramite }, "IX_dbo_ExpedienteActa_EstudioId_NroTramite").HasFilter("([NroTramite] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_ExpedienteActa_CreadoUtc");
            entity.Property(e => e.DescripcionDominio).HasMaxLength(250);
            entity.Property(e => e.HoraLabrado).HasMaxLength(20);
            entity.Property(e => e.JuzgadoFaltas).HasMaxLength(250);
            entity.Property(e => e.LugarLabrado).HasMaxLength(250);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.NroCaso).HasMaxLength(50);
            entity.Property(e => e.NroTramite).HasMaxLength(50);
            entity.Property(e => e.TipoYNroLicencia).HasMaxLength(250);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ExpedienteActa)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteActa_Estudio");

            entity.HasOne(d => d.Expediente).WithOne(p => p.ExpedienteActa)
                .HasPrincipalKey<Expediente>(p => new { p.EstudioId, p.Id })
                .HasForeignKey<ExpedienteActa>(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteActa_ExpedienteId");
        });

        modelBuilder.Entity<ExpedienteActividad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_ExpedienteActividad");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: ExpedienteActividad + Expediente.ActividadIIBB/CodigoActividadIIBB."));

            entity.HasIndex(e => e.ActividadEconomicaId, "IX_dbo_ExpedienteActividad_ActividadEconomicaId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.ActividadEconomicaId }, "UQ_dbo_ExpedienteActividad_EstudioId_ExpedienteId_ActividadEconomicaId").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_ExpedienteActividad_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_ExpedienteActividad_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_ExpedienteActividad_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.ActividadEconomica).WithMany(p => p.ExpedienteActividad)
                .HasForeignKey(d => d.ActividadEconomicaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteActividad_ActividadEconomicaId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.ExpedienteActividad)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteActividad_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.ExpedienteActividad)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteActividad_ExpedienteId");
        });

        modelBuilder.Entity<ExpedienteFiscalizacion>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.ExpedienteId }).HasName("PK_dbo_ExpedienteFiscalizacion");

            entity.ToTable(tb => tb.HasComment("1:1 opcional. Solo para expedientes de fiscalización (EsFisca). Origen Monitorio: Expediente.Fisca*."));

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_ExpedienteFiscalizacion_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.NroExpediente).HasMaxLength(50);
            entity.Property(e => e.NroOrden).HasMaxLength(50);
            entity.Property(e => e.NroRecursoReconsideracion).HasMaxLength(50);
            entity.Property(e => e.NroResolucion).HasMaxLength(50);
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.Property(e => e.ResponsableDomicilio).HasMaxLength(250);
            entity.Property(e => e.ResponsableNombre).HasMaxLength(250);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ExpedienteFiscalizacion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteFiscalizacion_Estudio");

            entity.HasOne(d => d.Expediente).WithOne(p => p.ExpedienteFiscalizacion)
                .HasPrincipalKey<Expediente>(p => new { p.EstudioId, p.Id })
                .HasForeignKey<ExpedienteFiscalizacion>(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteFiscalizacion_ExpedienteId");
        });

        modelBuilder.Entity<ExpedienteInfraccion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_ExpedienteInfraccion");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: InfraccionExpediente."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_ExpedienteInfraccion_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.InfraccionId }, "IX_dbo_ExpedienteInfraccion_EstudioId_InfraccionId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_ExpedienteInfraccion_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_ExpedienteInfraccion_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_ExpedienteInfraccion_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ExpedienteInfraccion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteInfraccion_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.ExpedienteInfraccion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteInfraccion_ExpedienteId");

            entity.HasOne(d => d.Infraccion).WithMany(p => p.ExpedienteInfraccion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InfraccionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedienteInfraccion_InfraccionId");
        });

        modelBuilder.Entity<ExpedientePersona>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_ExpedientePersona");

            entity.ToTable(tb => tb.HasComment("Rol 1 demandado, 2 codemandado, 3 responsable solidario. Sin límite de codemandados. Origen Monitorio: ExpedientePersona (+ Expediente.PersonaID)."));

            entity.HasIndex(e => new { e.EstudioId, e.PersonaId }, "IX_dbo_ExpedientePersona_EstudioId_PersonaId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.PersonaId, e.Rol }, "UQ_dbo_ExpedientePersona_EstudioId_ExpedienteId_PersonaId_Rol").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_ExpedientePersona_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_ExpedientePersona_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_ExpedientePersona_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Posicion).HasDefaultValue((byte)1, "DF_dbo_ExpedientePersona_Posicion");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ExpedientePersona)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedientePersona_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.ExpedientePersona)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedientePersona_ExpedienteId");

            entity.HasOne(d => d.Persona).WithMany(p => p.ExpedientePersona)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PersonaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_ExpedientePersona_PersonaId");
        });

        modelBuilder.Entity<Feriado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_Feriado");

            entity.ToTable("Feriado", "cat", tb => tb.HasComment("Ámbito 1 nacional, 2 provincial, 3 judicial (receso). Origen Monitorio: Feriado."));

            entity.HasIndex(e => e.ProvinciaId, "IX_cat_Feriado_ProvinciaId");

            entity.HasIndex(e => new { e.Fecha, e.Ambito, e.ProvinciaId }, "UQ_cat_Feriado_Fecha_Ambito_ProvinciaId").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_Feriado_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Descripcion).HasMaxLength(100);

            entity.HasOne(d => d.Provincia).WithMany(p => p.Feriado)
                .HasForeignKey(d => d.ProvinciaId)
                .HasConstraintName("FK_cat_Feriado_ProvinciaId");
        });

        modelBuilder.Entity<FeriadoEstudio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_FeriadoEstudio");

            entity.ToTable("FeriadoEstudio", "conf", tb => tb.HasComment("Asuetos propios del Estudio o de su circunscripción. "));

            entity.HasIndex(e => new { e.EstudioId, e.Fecha }, "UQ_conf_FeriadoEstudio_EstudioId_Fecha").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_FeriadoEstudio_EstudioId_Id").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_FeriadoEstudio_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(100);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.FeriadoEstudio)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_FeriadoEstudio_Estudio");
        });

        modelBuilder.Entity<IndiceTasaPasiva>(entity =>
        {
            entity.HasKey(e => e.Fecha).HasName("PK_cat_IndiceTasaPasiva");

            entity.ToTable("IndiceTasaPasiva", "cat", tb => tb.HasComment("Coeficiente acumulado diario de tasa pasiva (BCRA). Lo mantiene el superadministrador. Origen Monitorio: AlicuotaPlanilla."));

            entity.Property(e => e.Coeficiente).HasColumnType("decimal(12, 6)");
        });

        modelBuilder.Entity<InformePersona>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_InformePersona");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: Informe."));

            entity.HasIndex(e => new { e.EstudioId, e.PersonaId }, "IX_dbo_InformePersona_EstudioId_PersonaId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_InformePersona_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_InformePersona_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_InformePersona_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Texto).HasMaxLength(4000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.InformePersona)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_InformePersona_Estudio");

            entity.HasOne(d => d.Persona).WithMany(p => p.InformePersona)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PersonaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_InformePersona_PersonaId");
        });

        modelBuilder.Entity<Infraccion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Infraccion");

            entity.ToTable("Infraccion", "conf", tb => tb.HasComment("Origen Monitorio: Infraccion."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Infraccion_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Infraccion_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Infraccion_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(400);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Infraccion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Infraccion_Estudio");
        });

        modelBuilder.Entity<Instancia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Instancia");

            entity.ToTable("Instancia", "conf", tb => tb.HasComment("Macroestado del expediente (pre judicial, judicial, con sentencia, archivado…). Origen Monitorio: Instancia."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Instancia_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_Instancia_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Instancia_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_conf_Instancia_Activa");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Instancia_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Instancia)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Instancia_Estudio");
        });

        modelBuilder.Entity<InstanciaExpediente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_InstanciaExpediente");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: InstanciaExpediente."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.Fecha }, "IX_dbo_InstanciaExpediente_EstudioId_ExpedienteId_Fecha");

            entity.HasIndex(e => new { e.EstudioId, e.InstanciaId }, "IX_dbo_InstanciaExpediente_EstudioId_InstanciaId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_InstanciaExpediente_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_InstanciaExpediente_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_InstanciaExpediente_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.InstanciaExpediente)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_InstanciaExpediente_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.InstanciaExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_InstanciaExpediente_ExpedienteId");

            entity.HasOne(d => d.Instancia).WithMany(p => p.InstanciaExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InstanciaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_InstanciaExpediente_InstanciaId");
        });

        modelBuilder.Entity<ItemPlanilla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_ItemPlanilla");

            entity.ToTable("ItemPlanilla", "liq", tb => tb.HasComment("TipoCalculo reemplaza las 9 columnas Es*: 1 monto fijo, 2 porcentaje del capital, 3 capital, 4 interés simple, 5 tasa pasiva, 6 interés sobre capital, 7 honorarios, 8 tasa de justicia, 9 aportes, 10 gastos de diligencias, 11 subtotal. Origen Monitorio: ItemsPlanilla."));

            entity.HasIndex(e => new { e.EstudioId, e.RubroPlanillaId }, "IX_liq_ItemPlanilla_EstudioId_RubroPlanillaId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_ItemPlanilla_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_liq_ItemPlanilla_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_liq_ItemPlanilla_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_liq_ItemPlanilla_Activo");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_ItemPlanilla_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(4000);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.MontoMinimo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ItemPlanilla)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_ItemPlanilla_Estudio");

            entity.HasOne(d => d.RubroPlanilla).WithMany(p => p.ItemPlanilla)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RubroPlanillaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_ItemPlanilla_RubroPlanillaId");
        });

        modelBuilder.Entity<ItemPlanillaTope>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_ItemPlanillaTope");

            entity.ToTable("ItemPlanillaTope", "liq", tb => tb.HasComment("Origen Monitorio: ItemsMontoMinMax (IemsMontoMinMax se descarta tras comparar contenido)."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_ItemPlanillaTope_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.ItemPlanillaId, e.VigenteDesde }, "UQ_liq_ItemPlanillaTope_EstudioId_ItemPlanillaId_VigenteDesde").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_liq_ItemPlanillaTope_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_ItemPlanillaTope_CreadoUtc");
            entity.Property(e => e.Maximo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Minimo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ItemPlanillaTope)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_ItemPlanillaTope_Estudio");

            entity.HasOne(d => d.ItemPlanilla).WithMany(p => p.ItemPlanillaTope)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ItemPlanillaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_ItemPlanillaTope_ItemPlanillaId");
        });

        modelBuilder.Entity<Juzgado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Juzgado");

            entity.ToTable("Juzgado", "conf", tb => tb.HasComment("Organización judicial por Estudio; al crear un Estudio puede copiarse de otro. Origen Monitorio: Juzgado (sin ItaxID ni MuniCordobaID)."));

            entity.HasIndex(e => e.LocalidadId, "IX_conf_Juzgado_LocalidadId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Juzgado_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre, e.Secretaria }, "UQ_conf_Juzgado_EstudioId_Nombre_Secretaria").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Juzgado_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_conf_Juzgado_Activo");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Juzgado_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(100);
            entity.Property(e => e.DomicilioParaEscritos).HasMaxLength(500);
            entity.Property(e => e.Juez).HasMaxLength(150);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Secretaria).HasMaxLength(150);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.Property(e => e.Ubicacion).HasMaxLength(150);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Juzgado)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Juzgado_Estudio");

            entity.HasOne(d => d.Localidad).WithMany(p => p.Juzgado)
                .HasForeignKey(d => d.LocalidadId)
                .HasConstraintName("FK_conf_Juzgado_LocalidadId");
        });

        modelBuilder.Entity<Liquidacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_Liquidacion");

            entity.ToTable("Liquidacion", "liq", tb => tb.HasComment("Nueva: el legado no guardaba planillas. Estado 1 borrador, 2 guardada, 3 presentada, 4 anulada. ParametrosJson copia los valores usados para que sea reproducible. "));

            entity.HasIndex(e => new { e.EstudioId, e.DocumentoId }, "IX_liq_Liquidacion_EstudioId_DocumentoId");

            entity.HasIndex(e => new { e.EstudioId, e.JuzgadoId }, "IX_liq_Liquidacion_EstudioId_JuzgadoId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.NroVersion }, "UQ_liq_Liquidacion_EstudioId_ExpedienteId_NroVersion").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_Liquidacion_EstudioId_Id").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_Liquidacion_CreadoUtc");
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_liq_Liquidacion_Estado");
            entity.Property(e => e.HonorariosJus).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.HonorariosPesos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalCapital).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalGastos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalInteres).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Liquidacion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_Liquidacion_Estudio");

            entity.HasOne(d => d.Documento).WithMany(p => p.Liquidacion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.DocumentoId })
                .HasConstraintName("FK_liq_Liquidacion_DocumentoId");

            entity.HasOne(d => d.Expediente).WithMany(p => p.Liquidacion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_Liquidacion_ExpedienteId");

            entity.HasOne(d => d.Juzgado).WithMany(p => p.Liquidacion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.JuzgadoId })
                .HasConstraintName("FK_liq_Liquidacion_JuzgadoId");
        });

        modelBuilder.Entity<LiquidacionLinea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_LiquidacionLinea");

            entity.ToTable("LiquidacionLinea", "liq");

            entity.HasIndex(e => new { e.EstudioId, e.ItemPlanillaId }, "IX_liq_LiquidacionLinea_EstudioId_ItemPlanillaId");

            entity.HasIndex(e => new { e.EstudioId, e.PeriodoAdeudadoId }, "IX_liq_LiquidacionLinea_EstudioId_PeriodoAdeudadoId");

            entity.HasIndex(e => new { e.EstudioId, e.RubroPlanillaId }, "IX_liq_LiquidacionLinea_EstudioId_RubroPlanillaId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_LiquidacionLinea_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LiquidacionId, e.Orden }, "UQ_liq_LiquidacionLinea_EstudioId_LiquidacionId_Orden").IsUnique();

            entity.Property(e => e.Base).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Concepto).HasMaxLength(400);
            entity.Property(e => e.Detalle).HasMaxLength(1000);
            entity.Property(e => e.Importe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tasa).HasColumnType("decimal(12, 6)");

            entity.HasOne(d => d.Estudio).WithMany(p => p.LiquidacionLinea)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_LiquidacionLinea_Estudio");

            entity.HasOne(d => d.ItemPlanilla).WithMany(p => p.LiquidacionLinea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ItemPlanillaId })
                .HasConstraintName("FK_liq_LiquidacionLinea_ItemPlanillaId");

            entity.HasOne(d => d.Liquidacion).WithMany(p => p.LiquidacionLinea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.LiquidacionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_LiquidacionLinea_LiquidacionId");

            entity.HasOne(d => d.PeriodoAdeudado).WithMany(p => p.LiquidacionLinea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PeriodoAdeudadoId })
                .HasConstraintName("FK_liq_LiquidacionLinea_PeriodoAdeudadoId");

            entity.HasOne(d => d.RubroPlanilla).WithMany(p => p.LiquidacionLinea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RubroPlanillaId })
                .HasConstraintName("FK_liq_LiquidacionLinea_RubroPlanillaId");
        });

        modelBuilder.Entity<Localidad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_Localidad");

            entity.ToTable("Localidad", "cat", tb => tb.HasComment("Origen Monitorio: Localidad."));

            entity.HasIndex(e => new { e.ProvinciaId, e.Nombre }, "UQ_cat_Localidad_ProvinciaId_Nombre").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_Localidad_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CodigoPostal).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.Provincia).WithMany(p => p.Localidad)
                .HasForeignKey(d => d.ProvinciaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_cat_Localidad_ProvinciaId");
        });

        modelBuilder.Entity<NovedadJudicial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_NovedadJudicial");

            entity.ToTable(tb => tb.HasComment("Proveídos, decretos y partes. Fuente 1 manual, 2 importación de archivo, 3 migrada. ExpedienteId NULL = pendiente de vincular. Origen Monitorio: ExpedienteParte."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_NovedadJudicial_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.Leida, e.Fecha }, "IX_dbo_NovedadJudicial_EstudioId_Leida_Fecha");

            entity.HasIndex(e => new { e.EstudioId, e.TareaId }, "IX_dbo_NovedadJudicial_EstudioId_TareaId");

            entity.HasIndex(e => e.LeidaPor, "IX_dbo_NovedadJudicial_LeidaPor");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_NovedadJudicial_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_NovedadJudicial_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_NovedadJudicial_CreadoUtc");
            entity.Property(e => e.Fuente).HasDefaultValue((byte)1, "DF_dbo_NovedadJudicial_Fuente");
            entity.Property(e => e.LeidaUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.NroExpedienteInformado).HasMaxLength(50);
            entity.Property(e => e.Texto).HasMaxLength(4000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.NovedadJudicial)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_NovedadJudicial_Estudio");

            entity.HasOne(d => d.LeidaPorNavigation).WithMany(p => p.NovedadJudicial)
                .HasForeignKey(d => d.LeidaPor)
                .HasConstraintName("FK_dbo_NovedadJudicial_LeidaPor");

            entity.HasOne(d => d.Expediente).WithMany(p => p.NovedadJudicial)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .HasConstraintName("FK_dbo_NovedadJudicial_ExpedienteId");

            entity.HasOne(d => d.Tarea).WithMany(p => p.NovedadJudicial)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.TareaId })
                .HasConstraintName("FK_dbo_NovedadJudicial_TareaId");
        });

        modelBuilder.Entity<ParametroLiquidacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_ParametroLiquidacion");

            entity.ToTable("ParametroLiquidacion", "liq", tb => tb.HasComment("Parámetros con vigencia. ComitenteId NULL = valor general del Estudio; con ComitenteId = particular del comitente (tiene prioridad). Origen Monitorio: UnidadPlanilla + valores de App.config + constantes en frmPlanilla.cs."));

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId }, "IX_liq_ParametroLiquidacion_EstudioId_ComitenteId");

            entity.HasIndex(e => new { e.EstudioId, e.Codigo, e.ComitenteId, e.VigenteDesde }, "UQ_liq_ParametroLiquidacion_EstudioId_Codigo_ComitenteId_VigenteDesde").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_ParametroLiquidacion_EstudioId_Id").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_ParametroLiquidacion_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.ParametroLiquidacion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_ParametroLiquidacion_Estudio");

            entity.HasOne(d => d.Comitente).WithMany(p => p.ParametroLiquidacion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_liq_ParametroLiquidacion_ComitenteId");
        });

        modelBuilder.Entity<PeriodoAdeudado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_PeriodoAdeudado");

            entity.ToTable(tb => tb.HasComment("Reemplaza también al texto Expediente.Periodos, que se arma a partir de esta tabla. Origen Monitorio: ExpedientePeriodo."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.Anio }, "IX_dbo_PeriodoAdeudado_EstudioId_ExpedienteId_Anio");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_PeriodoAdeudado_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_PeriodoAdeudado_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Concepto).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_PeriodoAdeudado_CreadoUtc");
            entity.Property(e => e.Cuota).HasMaxLength(50);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Observaciones).HasMaxLength(2500);
            entity.Property(e => e.Recargo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tributo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.PeriodoAdeudado)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_PeriodoAdeudado_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.PeriodoAdeudado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_PeriodoAdeudado_ExpedienteId");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_Permiso");

            entity.ToTable("Permiso", "seg", tb => tb.HasComment("Global, definido por el código de la aplicación (modulo.accion). Reemplaza el esquema de menús del WinForms (MenuSistema/MenuPerfil), que se descarta. "));

            entity.HasIndex(e => e.Codigo, "UQ_seg_Permiso_Codigo").IsUnique();

            entity.Property(e => e.Accion).HasMaxLength(20);
            entity.Property(e => e.Codigo).HasMaxLength(80);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.Modulo).HasMaxLength(40);
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Persona");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: Persona."));

            entity.HasIndex(e => new { e.EstudioId, e.Cuit }, "IX_dbo_Persona_EstudioId_Cuit").HasFilter("([Cuit] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.NroDocumento }, "IX_dbo_Persona_EstudioId_NroDocumento");

            entity.HasIndex(e => e.TipoDocumentoId, "IX_dbo_Persona_TipoDocumentoId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Persona_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Persona_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.AnuladoUtc).HasPrecision(3);
            entity.Property(e => e.Apellido).HasMaxLength(150);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Persona_CreadoUtc");
            entity.Property(e => e.Cuit).HasMaxLength(13);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EsPersonaFisica).HasDefaultValue(true, "DF_dbo_Persona_EsPersonaFisica");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.NroDocumento).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(4000);
            entity.Property(e => e.RazonSocial).HasMaxLength(250);
            entity.Property(e => e.Sexo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Telefono1).HasMaxLength(50);
            entity.Property(e => e.Telefono2).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Persona)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Persona_Estudio");

            entity.HasOne(d => d.TipoDocumento).WithMany(p => p.Persona)
                .HasForeignKey(d => d.TipoDocumentoId)
                .HasConstraintName("FK_dbo_Persona_TipoDocumentoId");
        });

        modelBuilder.Entity<PlanPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_PlanPago");

            entity.ToTable(tb => tb.HasComment("Estado es texto libre, como EstadoPlan en Monitorio. Activo indica el plan vigente del expediente. Origen Monitorio: ExpedientePlanPago (+ Expediente.NroPlanPagos)."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_PlanPago_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_PlanPago_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_PlanPago_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_dbo_PlanPago_Activo");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_PlanPago_CreadoUtc");
            entity.Property(e => e.Decreto).HasMaxLength(4000);
            entity.Property(e => e.Estado).HasMaxLength(50);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.NroPlan).HasMaxLength(40);
            entity.Property(e => e.PeriodosIncluidos).HasMaxLength(4000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.PlanPago)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_PlanPago_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.PlanPago)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_PlanPago_ExpedienteId");
        });

        modelBuilder.Entity<PlanillaJuzgado>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.JuzgadoId, e.ItemPlanillaId }).HasName("PK_liq_PlanillaJuzgado");

            entity.ToTable("PlanillaJuzgado", "liq", tb => tb.HasComment("Origen Monitorio: JuzgadoItems."));

            entity.HasIndex(e => new { e.EstudioId, e.ItemPlanillaId }, "IX_liq_PlanillaJuzgado_EstudioId_ItemPlanillaId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.PlanillaJuzgado)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_PlanillaJuzgado_Estudio");

            entity.HasOne(d => d.ItemPlanilla).WithMany(p => p.PlanillaJuzgado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ItemPlanillaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_PlanillaJuzgado_ItemPlanillaId");

            entity.HasOne(d => d.Juzgado).WithMany(p => p.PlanillaJuzgado)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.JuzgadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_PlanillaJuzgado_JuzgadoId");
        });

        modelBuilder.Entity<PlantillaEscrito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_PlantillaEscrito");

            entity.ToTable("PlantillaEscrito", "conf", tb => tb.HasComment("Tipo 1 escrito, 2 cédula, 3 carta, 4 oficio. EsUnico: se genera una sola vez por expediente. Origen Monitorio: EtapaEscrito, Instancia.Path/EscritoNombre, Cautelar.Path/EscritoNombre."));

            entity.HasIndex(e => new { e.EstudioId, e.EtapaId }, "IX_conf_PlantillaEscrito_EstudioId_EtapaId");

            entity.HasIndex(e => new { e.EstudioId, e.InstanciaId }, "IX_conf_PlantillaEscrito_EstudioId_InstanciaId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_PlantillaEscrito_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_PlantillaEscrito_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_PlantillaEscrito_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_conf_PlantillaEscrito_Activa");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_PlantillaEscrito_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.PlantillaEscrito)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_PlantillaEscrito_Estudio");

            entity.HasOne(d => d.Etapa).WithMany(p => p.PlantillaEscrito)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaId })
                .HasConstraintName("FK_conf_PlantillaEscrito_EtapaId");

            entity.HasOne(d => d.Instancia).WithMany(p => p.PlantillaEscrito)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.InstanciaId })
                .HasConstraintName("FK_conf_PlantillaEscrito_InstanciaId");
        });

        modelBuilder.Entity<PlantillaEscritoVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_PlantillaEscritoVersion");

            entity.ToTable("PlantillaEscritoVersion", "conf");

            entity.HasIndex(e => new { e.EstudioId, e.DocumentoId }, "IX_conf_PlantillaEscritoVersion_EstudioId_DocumentoId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_PlantillaEscritoVersion_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.PlantillaEscritoId, e.NroVersion }, "UQ_conf_PlantillaEscritoVersion_EstudioId_PlantillaEscritoId_NroVersion").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_PlantillaEscritoVersion_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Notas).HasMaxLength(500);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Vigente).HasDefaultValue(true, "DF_conf_PlantillaEscritoVersion_Vigente");

            entity.HasOne(d => d.Estudio).WithMany(p => p.PlantillaEscritoVersion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_PlantillaEscritoVersion_Estudio");

            entity.HasOne(d => d.Documento).WithMany(p => p.PlantillaEscritoVersion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.DocumentoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_PlantillaEscritoVersion_DocumentoId");

            entity.HasOne(d => d.PlantillaEscrito).WithMany(p => p.PlantillaEscritoVersion)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PlantillaEscritoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_PlantillaEscritoVersion_PlantillaEscritoId");
        });

        modelBuilder.Entity<Profesional>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Profesional");

            entity.ToTable("Profesional", "conf", tb => tb.HasComment("Procuradores, patrocinantes, apoderados, abogados de interior y martilleros. Reemplaza al ProcuradorID que el legado usaba como tenant parcial. Origen Monitorio: Procurador (sin UsuarioGant/PasswordGant)."));

            entity.HasIndex(e => e.CondicionIvaId, "IX_conf_Profesional_CondicionIvaId");

            entity.HasIndex(e => e.LocalidadId, "IX_conf_Profesional_LocalidadId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Profesional_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Profesional_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_conf_Profesional_Activo");
            entity.Property(e => e.Barrio).HasMaxLength(100);
            entity.Property(e => e.Calle).HasMaxLength(100);
            entity.Property(e => e.Carpeta).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Profesional_CreadoUtc");
            entity.Property(e => e.Cuit).HasMaxLength(13);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Matricula).HasMaxLength(50);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(4000);
            entity.Property(e => e.TextoEscrito).HasMaxLength(4000);
            entity.Property(e => e.TituloApoderado).HasMaxLength(250);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CondicionIva).WithMany(p => p.Profesional)
                .HasForeignKey(d => d.CondicionIvaId)
                .HasConstraintName("FK_conf_Profesional_CondicionIvaId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.Profesional)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Profesional_Estudio");

            entity.HasOne(d => d.Localidad).WithMany(p => p.Profesional)
                .HasForeignKey(d => d.LocalidadId)
                .HasConstraintName("FK_conf_Profesional_LocalidadId");
        });

        modelBuilder.Entity<Provincia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_Provincia");

            entity.ToTable("Provincia", "cat", tb => tb.HasComment("Origen Monitorio: Provincia."));

            entity.HasIndex(e => e.Nombre, "UQ_cat_Provincia_Nombre").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_Provincia_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Codigo).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(60);
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Region");

            entity.ToTable("Region", "conf", tb => tb.HasComment("Origen Monitorio: Expediente.Region (valores distintos)."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Region_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_Region_EstudioId_Nombre").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Region_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Region)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Region_Estudio");
        });

        modelBuilder.Entity<RegistroPublico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_RegistroPublico");

            entity.ToTable("RegistroPublico", "conf", tb => tb.HasComment("Origen Monitorio: Registro."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_RegistroPublico_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_RegistroPublico_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_RegistroPublico_CreadoUtc");
            entity.Property(e => e.Domicilio).HasMaxLength(500);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Zona).HasMaxLength(250);

            entity.HasOne(d => d.Estudio).WithMany(p => p.RegistroPublico)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_RegistroPublico_Estudio");
        });

        modelBuilder.Entity<Requerimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Requerimiento");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: Requerimiento."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_Requerimiento_EstudioId_ExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Requerimiento_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Requerimiento_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Requerimiento_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Requerimiento)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Requerimiento_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.Requerimiento)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Requerimiento_ExpedienteId");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_Rol");

            entity.ToTable("Rol", "seg", tb => tb.HasComment("Ámbito 1 todo el Estudio, 2 por comitente, 3 expedientes asignados. Origen Monitorio: Nueva (Perfil de Monitorio se descarta)."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_seg_Rol_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_seg_Rol_EstudioId_Nombre").IsUnique();

            entity.Property(e => e.Ambito).HasDefaultValue((byte)1, "DF_seg_Rol_Ambito");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_Rol_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Rol)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_Rol_Estudio");
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.RolId, e.PermisoId }).HasName("PK_seg_RolPermiso");

            entity.ToTable("RolPermiso", "seg");

            entity.HasIndex(e => e.PermisoId, "IX_seg_RolPermiso_PermisoId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.RolPermiso)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_RolPermiso_Estudio");

            entity.HasOne(d => d.Permiso).WithMany(p => p.RolPermiso)
                .HasForeignKey(d => d.PermisoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_RolPermiso_PermisoId");

            entity.HasOne(d => d.Rol).WithMany(p => p.RolPermiso)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.RolId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_RolPermiso_RolId");
        });

        modelBuilder.Entity<RubroPlanilla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_RubroPlanilla");

            entity.ToTable("RubroPlanilla", "liq", tb => tb.HasComment("Origen Monitorio: RubroPlanilla."));

            entity.HasIndex(e => new { e.EstudioId, e.PadreId }, "IX_liq_RubroPlanilla_EstudioId_PadreId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_RubroPlanilla_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_liq_RubroPlanilla_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_liq_RubroPlanilla_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_RubroPlanilla_CreadoUtc");
            entity.Property(e => e.Descripcion).HasMaxLength(400);
            entity.Property(e => e.DescripcionTotal).HasMaxLength(100);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.RubroPlanilla)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_RubroPlanilla_Estudio");

            entity.HasOne(d => d.RubroPlanillaNavigation).WithMany(p => p.InverseRubroPlanillaNavigation)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.PadreId })
                .HasConstraintName("FK_liq_RubroPlanilla_PadreId");
        });

        modelBuilder.Entity<SecuenciaEstudio>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.Nombre }).HasName("PK_seg_SecuenciaEstudio");

            entity.ToTable("SecuenciaEstudio", "seg", tb => tb.HasComment("Numeración por Estudio (NroOrden). Se incrementa con UPDLOCK, HOLDLOCK dentro de la transacción de alta. Origen Monitorio: Secuencia."));

            entity.Property(e => e.Nombre).HasMaxLength(50);

            entity.HasOne(d => d.Estudio).WithMany(p => p.SecuenciaEstudio)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_SecuenciaEstudio_Estudio");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_Tarea");

            entity.ToTable(tb => tb.HasComment("Estado 1 pendiente, 2 cumplida, 3 archivada, 4 anulada. Origen 1 manual, 2 generada por etapa, 3 desde novedad, 4 migrada de Agenda. Origen Monitorio: ActividadAdministrativaExpediente + Agenda."));

            entity.HasIndex(e => new { e.EstudioId, e.Estado, e.FechaVencimiento }, "IX_dbo_Tarea_EstudioId_Estado_FechaVencimiento");

            entity.HasIndex(e => new { e.EstudioId, e.EtapaExpedienteId }, "IX_dbo_Tarea_EstudioId_EtapaExpedienteId");

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId }, "IX_dbo_Tarea_EstudioId_ExpedienteId").HasFilter("([ExpedienteId] IS NOT NULL)");

            entity.HasIndex(e => new { e.EstudioId, e.TipoTareaId }, "IX_dbo_Tarea_EstudioId_TipoTareaId");

            entity.HasIndex(e => e.ResponsableUsuarioId, "IX_dbo_Tarea_ResponsableUsuarioId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_Tarea_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_Tarea_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_Tarea_CreadoUtc");
            entity.Property(e => e.CumplidaUtc).HasPrecision(3);
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.Estado).HasDefaultValue((byte)1, "DF_dbo_Tarea_Estado");
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);
            entity.Property(e => e.Lugar).HasMaxLength(250);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Origen).HasDefaultValue((byte)1, "DF_dbo_Tarea_Origen");
            entity.Property(e => e.Titulo).HasMaxLength(250);
            entity.Property(e => e.TodoElDia).HasDefaultValue(true, "DF_dbo_Tarea_TodoElDia");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Tarea)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_Tarea_Estudio");

            entity.HasOne(d => d.ResponsableUsuario).WithMany(p => p.Tarea)
                .HasForeignKey(d => d.ResponsableUsuarioId)
                .HasConstraintName("FK_dbo_Tarea_ResponsableUsuarioId");

            entity.HasOne(d => d.EtapaExpediente).WithMany(p => p.Tarea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.EtapaExpedienteId })
                .HasConstraintName("FK_dbo_Tarea_EtapaExpedienteId");

            entity.HasOne(d => d.Expediente).WithMany(p => p.Tarea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .HasConstraintName("FK_dbo_Tarea_ExpedienteId");

            entity.HasOne(d => d.TipoTarea).WithMany(p => p.Tarea)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.TipoTareaId })
                .HasConstraintName("FK_dbo_Tarea_TipoTareaId");
        });

        modelBuilder.Entity<TareaParticipante>(entity =>
        {
            entity.HasKey(e => new { e.EstudioId, e.TareaId, e.UsuarioId }).HasName("PK_dbo_TareaParticipante");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: AgendaUsuario."));

            entity.HasIndex(e => e.UsuarioId, "IX_dbo_TareaParticipante_UsuarioId");

            entity.HasOne(d => d.Estudio).WithMany(p => p.TareaParticipante)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_TareaParticipante_Estudio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TareaParticipante)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_TareaParticipante_UsuarioId");

            entity.HasOne(d => d.Tarea).WithMany(p => p.TareaParticipante)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.TareaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_TareaParticipante_TareaId");
        });

        modelBuilder.Entity<TasaInteres>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_liq_TasaInteres");

            entity.ToTable("TasaInteres", "liq", tb => tb.HasComment("Periodicidad 1 diaria, 2 mensual, 3 anual. Valor en tanto por uno (0.000830 = 0,083 %). Reemplaza 0,083 % y 0,1 % diarios fijos en código. "));

            entity.HasIndex(e => new { e.EstudioId, e.ComitenteId }, "IX_liq_TasaInteres_EstudioId_ComitenteId");

            entity.HasIndex(e => new { e.EstudioId, e.Codigo, e.ComitenteId, e.VigenteDesde }, "UQ_liq_TasaInteres_EstudioId_Codigo_ComitenteId_VigenteDesde").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_liq_TasaInteres_EstudioId_Id").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_liq_TasaInteres_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Valor).HasColumnType("decimal(12, 6)");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.TasaInteres)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_liq_TasaInteres_Estudio");

            entity.HasOne(d => d.Comitente).WithMany(p => p.TasaInteres)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ComitenteId })
                .HasConstraintName("FK_liq_TasaInteres_ComitenteId");
        });

        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cat_TipoDocumento");

            entity.ToTable("TipoDocumento", "cat", tb => tb.HasComment("Origen Monitorio: TipoDoc."));

            entity.HasIndex(e => e.Codigo, "UQ_cat_TipoDocumento_Codigo").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_cat_TipoDocumento_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Codigo).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(30);
        });

        modelBuilder.Entity<TipoEmbargo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_TipoEmbargo");

            entity.ToTable("TipoEmbargo", "conf", tb => tb.HasComment("Clase 1 inmueble, 2 automotor, 3 embargo de cuentas corrientes, 4 inhibición, 5 intervención de caja, 6 reserva de cautelar. La clase define el esquema de DetalleJson del embargo. Origen Monitorio: TipoEmbargo."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_TipoEmbargo_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_TipoEmbargo_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_TipoEmbargo_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_TipoEmbargo_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.TipoEmbargo)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_TipoEmbargo_Estudio");
        });

        modelBuilder.Entity<TipoTarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_TipoTarea");

            entity.ToTable("TipoTarea", "conf", tb => tb.HasComment("Origen Monitorio: ActividadAdministrativa."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_TipoTarea_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_TipoTarea_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_TipoTarea_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_conf_TipoTarea_Activo");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_TipoTarea_CreadoUtc");
            entity.Property(e => e.DiasHabiles).HasDefaultValue(true, "DF_conf_TipoTarea_DiasHabiles");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.TipoTarea)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_TipoTarea_Estudio");
        });

        modelBuilder.Entity<TrabajoProceso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_TrabajoProceso");

            entity.ToTable("TrabajoProceso", "seg", tb => tb.HasComment("Procesos en segundo plano. Tipo 1 escritos masivos, 2 carga masiva, 3 modificación masiva, 4 importar novedades, 5 liquidación masiva, 6 purga de auditoría. Estado 1 en cola, 2 en curso, 3 finalizado, 4 finalizado con errores, 5 cancelado. "));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_seg_TrabajoProceso_EstudioId_Id").IsUnique();

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_TrabajoProceso_CreadoUtc");
            entity.Property(e => e.FinalizadoUtc).HasPrecision(3);
            entity.Property(e => e.IniciadoUtc).HasPrecision(3);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.TrabajoProceso)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seg_TrabajoProceso_Estudio");
        });

        modelBuilder.Entity<Ubicacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_conf_Ubicacion");

            entity.ToTable("Ubicacion", "conf", tb => tb.HasComment("Ubicación física del expediente. Origen Monitorio: Lugar."));

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_conf_Ubicacion_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.Nombre }, "UQ_conf_Ubicacion_EstudioId_Nombre").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_conf_Ubicacion_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_conf_Ubicacion_Activa");
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_conf_Ubicacion_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.Ubicacion)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_conf_Ubicacion_Estudio");
        });

        modelBuilder.Entity<UbicacionExpediente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo_UbicacionExpediente");

            entity.ToTable(tb => tb.HasComment("Origen Monitorio: ExpedienteLugar."));

            entity.HasIndex(e => new { e.EstudioId, e.ExpedienteId, e.Fecha }, "IX_dbo_UbicacionExpediente_EstudioId_ExpedienteId_Fecha");

            entity.HasIndex(e => new { e.EstudioId, e.UbicacionId }, "IX_dbo_UbicacionExpediente_EstudioId_UbicacionId");

            entity.HasIndex(e => new { e.EstudioId, e.Id }, "UQ_dbo_UbicacionExpediente_EstudioId_Id").IsUnique();

            entity.HasIndex(e => new { e.EstudioId, e.LegadoId }, "UX_dbo_UbicacionExpediente_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_dbo_UbicacionExpediente_CreadoUtc");
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Estudio).WithMany(p => p.UbicacionExpediente)
                .HasForeignKey(d => d.EstudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_UbicacionExpediente_Estudio");

            entity.HasOne(d => d.Expediente).WithMany(p => p.UbicacionExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.ExpedienteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_UbicacionExpediente_ExpedienteId");

            entity.HasOne(d => d.Ubicacion).WithMany(p => p.UbicacionExpediente)
                .HasPrincipalKey(p => new { p.EstudioId, p.Id })
                .HasForeignKey(d => new { d.EstudioId, d.UbicacionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo_UbicacionExpediente_UbicacionId");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_seg_Usuario");

            entity.ToTable("Usuario", "seg", tb => tb.HasComment("Identidad global (IdentityUser<int> de ASP.NET Core Identity con columnas propias). Sin EstudioId. Las contraseñas del legado NO se migran: PasswordHash queda NULL hasta que el usuario acepta la invitación. Origen Monitorio: Usuario (sin Pass)."));

            entity.HasIndex(e => e.EmailNormalizado, "UQ_seg_Usuario_EmailNormalizado").IsUnique();

            entity.HasIndex(e => e.LegadoId, "UX_seg_Usuario_LegadoId")
                .IsUnique()
                .HasFilter("([LegadoId] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_seg_Usuario_Activo");
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.BloqueadoHastaUtc).HasPrecision(3);
            entity.Property(e => e.CreadoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_seg_Usuario_CreadoUtc");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.EmailNormalizado).HasMaxLength(256);
            entity.Property(e => e.ModificadoUtc).HasPrecision(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.SecurityStamp).HasMaxLength(100);
            entity.Property(e => e.UltimoAccesoUtc).HasPrecision(3);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

-- Procu360 - modelo de datos (SQL Server 2019+ / Azure SQL)
-- Generado desde gen/esquema.py: no editar a mano.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'seg') IS NULL EXEC(N'CREATE SCHEMA seg');
GO
IF SCHEMA_ID(N'cat') IS NULL EXEC(N'CREATE SCHEMA cat');
GO
IF SCHEMA_ID(N'conf') IS NULL EXEC(N'CREATE SCHEMA conf');
GO
IF SCHEMA_ID(N'liq') IS NULL EXEC(N'CREATE SCHEMA liq');
GO
IF SCHEMA_ID(N'aud') IS NULL EXEC(N'CREATE SCHEMA aud');
GO
IF SCHEMA_ID(N'mig') IS NULL EXEC(N'CREATE SCHEMA mig');
GO

CREATE TABLE seg.Estudio (
    Id int IDENTITY(1,1) NOT NULL,
    Nombre nvarchar(200) NOT NULL,
    Cuit nvarchar(13) NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_seg_Estudio_Estado DEFAULT (1),
    ZonaHoraria nvarchar(50) NOT NULL CONSTRAINT DF_seg_Estudio_ZonaHoraria DEFAULT (N'America/Argentina/Cordoba'),
    RetencionAuditoriaDias int NOT NULL CONSTRAINT DF_seg_Estudio_RetencionAuditoriaDias DEFAULT (365),
    EstudioOrigenCopiaId int NULL,
    CopiaCatalogosJson nvarchar(max) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_Estudio_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_Estudio PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_Estudio_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_seg_Estudio_1 CHECK (Estado IN (1,2)),
    CONSTRAINT CK_seg_Estudio_2 CHECK (RetencionAuditoriaDias BETWEEN 30 AND 3650)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Raíz del tenant. Estado 1 activo, 2 suspendido. CopiaCatalogosJson registra qué catálogos se copiaron del Estudio origen al crearlo. ', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'Estudio';
GO
CREATE TABLE seg.Usuario (
    Id int IDENTITY(1,1) NOT NULL,
    Email nvarchar(256) NOT NULL,
    EmailNormalizado nvarchar(256) NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Apellido nvarchar(100) NOT NULL,
    EsSuperAdmin bit NOT NULL CONSTRAINT DF_seg_Usuario_EsSuperAdmin DEFAULT (0),
    Activo bit NOT NULL CONSTRAINT DF_seg_Usuario_Activo DEFAULT (1),
    MfaHabilitado bit NOT NULL CONSTRAINT DF_seg_Usuario_MfaHabilitado DEFAULT (0),
    PasswordHash nvarchar(max) NULL,
    SecurityStamp nvarchar(100) NULL,
    BloqueadoHastaUtc datetimeoffset(3) NULL,
    AccesosFallidos int NOT NULL CONSTRAINT DF_seg_Usuario_AccesosFallidos DEFAULT (0),
    UltimoAccesoUtc datetime2(3) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_Usuario_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_Usuario PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_Usuario_EmailNormalizado UNIQUE (EmailNormalizado)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identidad global (IdentityUser<int> de ASP.NET Core Identity con columnas propias). Sin EstudioId. Las contraseñas del legado NO se migran: PasswordHash queda NULL hasta que el usuario acepta la invitación. Origen Monitorio: Usuario (sin Pass).', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'Usuario';
GO
CREATE TABLE cat.Provincia (
    Id int IDENTITY(1,1) NOT NULL,
    Nombre nvarchar(60) NOT NULL,
    Codigo nvarchar(10) NULL,
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_Provincia PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_Provincia_Nombre UNIQUE (Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Provincia.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'Provincia';
GO
CREATE TABLE cat.Localidad (
    Id int IDENTITY(1,1) NOT NULL,
    ProvinciaId int NOT NULL,
    Nombre nvarchar(150) NOT NULL,
    CodigoPostal nvarchar(10) NULL,
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_Localidad PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_Localidad_ProvinciaId_Nombre UNIQUE (ProvinciaId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Localidad.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'Localidad';
GO
CREATE TABLE cat.TipoDocumento (
    Id int IDENTITY(1,1) NOT NULL,
    Nombre nvarchar(30) NOT NULL,
    Codigo nvarchar(10) NOT NULL,
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_TipoDocumento PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_TipoDocumento_Codigo UNIQUE (Codigo)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: TipoDoc.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'TipoDocumento';
GO
CREATE TABLE cat.CondicionIva (
    Id int IDENTITY(1,1) NOT NULL,
    Nombre nvarchar(60) NOT NULL,
    Porcentaje decimal(5,2) NOT NULL CONSTRAINT DF_cat_CondicionIva_Porcentaje DEFAULT (0),
    LetraComprobante char(1) NULL,
    EsResponsableInscripto bit NOT NULL CONSTRAINT DF_cat_CondicionIva_EsResponsableInscripto DEFAULT (0),
    EsNoResponsable bit NOT NULL CONSTRAINT DF_cat_CondicionIva_EsNoResponsable DEFAULT (0),
    EsExento bit NOT NULL CONSTRAINT DF_cat_CondicionIva_EsExento DEFAULT (0),
    EsMonotributo bit NOT NULL CONSTRAINT DF_cat_CondicionIva_EsMonotributo DEFAULT (0),
    EsConsumidorFinal bit NOT NULL CONSTRAINT DF_cat_CondicionIva_EsConsumidorFinal DEFAULT (0),
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_CondicionIva PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_CondicionIva_Nombre UNIQUE (Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Iva.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'CondicionIva';
GO
CREATE TABLE cat.Feriado (
    Id int IDENTITY(1,1) NOT NULL,
    Fecha date NOT NULL,
    Descripcion nvarchar(100) NOT NULL,
    Ambito tinyint NOT NULL,
    ProvinciaId int NULL,
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_Feriado PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_Feriado_Fecha_Ambito_ProvinciaId UNIQUE (Fecha, Ambito, ProvinciaId),
    CONSTRAINT CK_cat_Feriado_1 CHECK (Ambito IN (1,2,3))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Ámbito 1 nacional, 2 provincial, 3 judicial (receso). Origen Monitorio: Feriado.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'Feriado';
GO
CREATE TABLE cat.IndiceTasaPasiva (
    Fecha date NOT NULL,
    Coeficiente decimal(12,6) NOT NULL,
    CONSTRAINT PK_cat_IndiceTasaPasiva PRIMARY KEY CLUSTERED (Fecha)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Coeficiente acumulado diario de tasa pasiva (BCRA). Lo mantiene el superadministrador. Origen Monitorio: AlicuotaPlanilla.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'IndiceTasaPasiva';
GO
CREATE TABLE cat.ActividadEconomica (
    Id int IDENTITY(1,1) NOT NULL,
    Codigo nvarchar(20) NOT NULL,
    Nombre nvarchar(500) NOT NULL,
    LegadoId uniqueidentifier NULL,
    CONSTRAINT PK_cat_ActividadEconomica PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_cat_ActividadEconomica_Codigo UNIQUE (Codigo)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nomenclador público de actividades (IIBB). Origen Monitorio: Actividad.', @level0type = N'SCHEMA', @level0name = N'cat', @level1type = N'TABLE', @level1name = N'ActividadEconomica';
GO
CREATE TABLE conf.Comitente (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(200) NOT NULL,
    Cuit nvarchar(13) NULL,
    Domicilio nvarchar(250) NULL,
    CodigoPostal nvarchar(10) NULL,
    LocalidadId int NULL,
    Observaciones nvarchar(2000) NULL,
    Activo bit NOT NULL CONSTRAINT DF_conf_Comitente_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Comitente_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Comitente PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Comitente_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Comitente_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Acreedor/organismo para el que se procura (municipio, DGR, ente). Las fórmulas de liquidación se parametrizan por comitente. Origen Monitorio: DomicilioDGR.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Comitente';
GO
CREATE TABLE conf.ConceptoDeuda (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ComitenteId int NULL,
    Nombre nvarchar(150) NOT NULL,
    Item nvarchar(50) NULL,
    PeriodosDiferenciados int NULL,
    ConsideraPeriodos bit NOT NULL CONSTRAINT DF_conf_ConceptoDeuda_ConsideraPeriodos DEFAULT (1),
    PeriodosFormatoDGR bit NOT NULL CONSTRAINT DF_conf_ConceptoDeuda_PeriodosFormatoDGR DEFAULT (0),
    TextoEscrito nvarchar(4000) NULL,
    Activo bit NOT NULL CONSTRAINT DF_conf_ConceptoDeuda_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_ConceptoDeuda_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_ConceptoDeuda PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_ConceptoDeuda_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_ConceptoDeuda_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Impuesto/tasa reclamada. TextoEscrito admite variables {{...}}. Origen Monitorio: Impuesto.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'ConceptoDeuda';
GO
CREATE TABLE conf.Profesional (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(250) NOT NULL,
    Cuit nvarchar(13) NULL,
    Matricula nvarchar(50) NULL,
    CondicionIvaId int NULL,
    Calle nvarchar(100) NULL,
    Numero nvarchar(20) NULL,
    Barrio nvarchar(100) NULL,
    LocalidadId int NULL,
    Carpeta nvarchar(50) NULL,
    Descripcion nvarchar(250) NULL,
    EsProcurador bit NOT NULL CONSTRAINT DF_conf_Profesional_EsProcurador DEFAULT (0),
    EsPatrocinante bit NOT NULL CONSTRAINT DF_conf_Profesional_EsPatrocinante DEFAULT (0),
    EsApoderado bit NOT NULL CONSTRAINT DF_conf_Profesional_EsApoderado DEFAULT (0),
    EsAbogadoInterior bit NOT NULL CONSTRAINT DF_conf_Profesional_EsAbogadoInterior DEFAULT (0),
    EsMartillero bit NOT NULL CONSTRAINT DF_conf_Profesional_EsMartillero DEFAULT (0),
    TituloApoderado nvarchar(250) NULL,
    TextoEscrito nvarchar(4000) NULL,
    Observaciones nvarchar(4000) NULL,
    Activo bit NOT NULL CONSTRAINT DF_conf_Profesional_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Profesional_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Profesional PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Profesional_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Procuradores, patrocinantes, apoderados, abogados de interior y martilleros. Reemplaza al ProcuradorID que el legado usaba como tenant parcial. Origen Monitorio: Procurador (sin UsuarioGant/PasswordGant).', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Profesional';
GO
CREATE TABLE seg.EstudioUsuario (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    UsuarioId int NOT NULL,
    Estado tinyint NOT NULL,
    ProfesionalId int NULL,
    InvitadoPor int NULL,
    InvitadoUtc datetime2(3) NULL,
    AprobadoPor int NULL,
    AprobadoUtc datetime2(3) NULL,
    MotivoRechazo nvarchar(500) NULL,
    UltimoAccesoUtc datetime2(3) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_EstudioUsuario_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_EstudioUsuario PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_EstudioUsuario_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_seg_EstudioUsuario_EstudioId_UsuarioId UNIQUE (EstudioId, UsuarioId),
    CONSTRAINT CK_seg_EstudioUsuario_1 CHECK (Estado IN (1,2,3,4,5))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Membresía. Estado 1 invitado, 2 pendiente de aprobación (el email ya pertenece a otro Estudio), 3 activo, 4 desactivado, 5 rechazado. ', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'EstudioUsuario';
GO
CREATE TABLE seg.Rol (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Descripcion nvarchar(500) NULL,
    Ambito tinyint NOT NULL CONSTRAINT DF_seg_Rol_Ambito DEFAULT (1),
    EsSistema bit NOT NULL CONSTRAINT DF_seg_Rol_EsSistema DEFAULT (0),
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_Rol_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_Rol PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_Rol_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_seg_Rol_EstudioId_Nombre UNIQUE (EstudioId, Nombre),
    CONSTRAINT CK_seg_Rol_1 CHECK (Ambito IN (1,2,3))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Ámbito 1 todo el Estudio, 2 por comitente, 3 expedientes asignados. Origen Monitorio: Nueva (Perfil de Monitorio se descarta).', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'Rol';
GO
CREATE TABLE seg.Permiso (
    Id int IDENTITY(1,1) NOT NULL,
    Codigo nvarchar(80) NOT NULL,
    Modulo nvarchar(40) NOT NULL,
    Accion nvarchar(20) NOT NULL,
    Descripcion nvarchar(200) NULL,
    CONSTRAINT PK_seg_Permiso PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_Permiso_Codigo UNIQUE (Codigo)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Global, definido por el código de la aplicación (modulo.accion). Reemplaza el esquema de menús del WinForms (MenuSistema/MenuPerfil), que se descarta. ', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'Permiso';
GO
CREATE TABLE seg.RolPermiso (
    EstudioId int NOT NULL,
    RolId int NOT NULL,
    PermisoId int NOT NULL,
    CONSTRAINT PK_seg_RolPermiso PRIMARY KEY CLUSTERED (EstudioId, RolId, PermisoId)
);
GO
CREATE TABLE seg.EstudioUsuarioRol (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    EstudioUsuarioId int NOT NULL,
    RolId int NOT NULL,
    ComitenteId int NULL,
    CONSTRAINT PK_seg_EstudioUsuarioRol PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_EstudioUsuarioRol_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_seg_EstudioUsuarioRol_EstudioId_EstudioUsuarioId_RolId_ComitenteId UNIQUE (EstudioId, EstudioUsuarioId, RolId, ComitenteId)
);
GO
CREATE TABLE seg.SecuenciaEstudio (
    EstudioId int NOT NULL,
    Nombre nvarchar(50) NOT NULL,
    UltimoValor bigint NOT NULL,
    CONSTRAINT PK_seg_SecuenciaEstudio PRIMARY KEY CLUSTERED (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Numeración por Estudio (NroOrden). Se incrementa con UPDLOCK, HOLDLOCK dentro de la transacción de alta. Origen Monitorio: Secuencia.', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'SecuenciaEstudio';
GO
CREATE TABLE seg.AccesoSoporte (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    UsuarioId int NOT NULL,
    Motivo nvarchar(500) NOT NULL,
    DesdeUtc datetime2(3) NOT NULL,
    HastaUtc datetime2(3) NOT NULL,
    OtorgadoPor int NOT NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_AccesoSoporte_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_AccesoSoporte PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_AccesoSoporte_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_seg_AccesoSoporte_1 CHECK (HastaUtc > DesdeUtc)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Acceso temporal y auditado de un superadministrador a los datos de un Estudio. ', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'AccesoSoporte';
GO
CREATE TABLE seg.TrabajoProceso (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Tipo tinyint NOT NULL,
    Estado tinyint NOT NULL,
    Total int NOT NULL CONSTRAINT DF_seg_TrabajoProceso_Total DEFAULT (0),
    Procesados int NOT NULL CONSTRAINT DF_seg_TrabajoProceso_Procesados DEFAULT (0),
    Errores int NOT NULL CONSTRAINT DF_seg_TrabajoProceso_Errores DEFAULT (0),
    ParametrosJson nvarchar(max) NULL,
    ResultadoJson nvarchar(max) NULL,
    IniciadoUtc datetime2(3) NULL,
    FinalizadoUtc datetime2(3) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_seg_TrabajoProceso_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_seg_TrabajoProceso PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_seg_TrabajoProceso_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_seg_TrabajoProceso_1 CHECK (Tipo BETWEEN 1 AND 6),
    CONSTRAINT CK_seg_TrabajoProceso_2 CHECK (Estado BETWEEN 1 AND 5),
    CONSTRAINT CK_seg_TrabajoProceso_3 CHECK (ParametrosJson IS NULL OR ISJSON(ParametrosJson)=1)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Procesos en segundo plano. Tipo 1 escritos masivos, 2 carga masiva, 3 modificación masiva, 4 importar novedades, 5 liquidación masiva, 6 purga de auditoría. Estado 1 en cola, 2 en curso, 3 finalizado, 4 finalizado con errores, 5 cancelado. ', @level0type = N'SCHEMA', @level0name = N'seg', @level1type = N'TABLE', @level1name = N'TrabajoProceso';
GO
CREATE TABLE conf.ConfiguracionEstudio (
    EstudioId int NOT NULL,
    Clave nvarchar(50) NOT NULL,
    Valor nvarchar(1000) NULL,
    Tipo tinyint NOT NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_ConfiguracionEstudio_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_ConfiguracionEstudio PRIMARY KEY CLUSTERED (EstudioId, Clave),
    CONSTRAINT CK_conf_ConfiguracionEstudio_1 CHECK (Tipo BETWEEN 1 AND 5)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Parámetros generales no modelados como columna. Tipo 1 texto, 2 entero, 3 decimal, 4 fecha, 5 booleano. Origen Monitorio: Configuracion.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'ConfiguracionEstudio';
GO
CREATE TABLE conf.FeriadoEstudio (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Fecha date NOT NULL,
    Descripcion nvarchar(100) NOT NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_FeriadoEstudio_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_FeriadoEstudio PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_FeriadoEstudio_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_FeriadoEstudio_EstudioId_Fecha UNIQUE (EstudioId, Fecha)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Asuetos propios del Estudio o de su circunscripción. ', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'FeriadoEstudio';
GO
CREATE TABLE conf.Juzgado (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(150) NOT NULL,
    Descripcion nvarchar(100) NULL,
    Secretaria nvarchar(150) NULL,
    Juez nvarchar(150) NULL,
    Ubicacion nvarchar(150) NULL,
    Telefono nvarchar(50) NULL,
    EsCapital bit NOT NULL CONSTRAINT DF_conf_Juzgado_EsCapital DEFAULT (0),
    LocalidadId int NULL,
    DomicilioParaEscritos nvarchar(500) NULL,
    Orden int NULL,
    Activo bit NOT NULL CONSTRAINT DF_conf_Juzgado_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Juzgado_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Juzgado PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Juzgado_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Juzgado_EstudioId_Nombre_Secretaria UNIQUE (EstudioId, Nombre, Secretaria)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Organización judicial por Estudio; al crear un Estudio puede copiarse de otro. Origen Monitorio: Juzgado (sin ItaxID ni MuniCordobaID).', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Juzgado';
GO
CREATE TABLE conf.Instancia (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(50) NOT NULL,
    Color char(7) NULL,
    Perime bit NOT NULL CONSTRAINT DF_conf_Instancia_Perime DEFAULT (0),
    EsBaja bit NOT NULL CONSTRAINT DF_conf_Instancia_EsBaja DEFAULT (0),
    Orden int NULL,
    Activa bit NOT NULL CONSTRAINT DF_conf_Instancia_Activa DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Instancia_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Instancia PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Instancia_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Instancia_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Macroestado del expediente (pre judicial, judicial, con sentencia, archivado…). Origen Monitorio: Instancia.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Instancia';
GO
CREATE TABLE conf.Etapa (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Flujo tinyint NOT NULL,
    Codigo nvarchar(20) NULL,
    Nombre nvarchar(100) NOT NULL,
    Orden int NULL,
    Dias int NULL,
    DiasHabiles bit NOT NULL CONSTRAINT DF_conf_Etapa_DiasHabiles DEFAULT (1),
    Interrumpe bit NOT NULL CONSTRAINT DF_conf_Etapa_Interrumpe DEFAULT (0),
    CortaPerencion bit NOT NULL CONSTRAINT DF_conf_Etapa_CortaPerencion DEFAULT (0),
    EsMonitorio bit NOT NULL CONSTRAINT DF_conf_Etapa_EsMonitorio DEFAULT (0),
    EsEjecucion bit NOT NULL CONSTRAINT DF_conf_Etapa_EsEjecucion DEFAULT (0),
    EsDiligencia bit NOT NULL CONSTRAINT DF_conf_Etapa_EsDiligencia DEFAULT (0),
    EtapaAnteriorId int NULL,
    EtapaSiguienteId int NULL,
    PasaSiguienteAutomatica bit NOT NULL CONSTRAINT DF_conf_Etapa_PasaSiguienteAutomatica DEFAULT (0),
    GeneraTarea bit NOT NULL CONSTRAINT DF_conf_Etapa_GeneraTarea DEFAULT (0),
    TareaParaUsuarios tinyint NOT NULL CONSTRAINT DF_conf_Etapa_TareaParaUsuarios DEFAULT (0),
    HitoSistema tinyint NULL,
    InstanciaId int NULL,
    Activa bit NOT NULL CONSTRAINT DF_conf_Etapa_Activa DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Etapa_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Etapa PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Etapa_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Etapa_EstudioId_Flujo_Nombre UNIQUE (EstudioId, Flujo, Nombre),
    CONSTRAINT CK_conf_Etapa_1 CHECK (Flujo IN (1,2)),
    CONSTRAINT CK_conf_Etapa_2 CHECK (TareaParaUsuarios BETWEEN 0 AND 3),
    CONSTRAINT CK_conf_Etapa_3 CHECK (HitoSistema BETWEEN 1 AND 4),
    CONSTRAINT CK_conf_Etapa_4 CHECK (Flujo = 1 OR InstanciaId IS NULL)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Flujo 1 principal, 2 cautelar. HitoSistema 1 inicio de demanda, 2 demanda presentada (reemplaza el código fijo M100 y Configuracion.DemPresID), 3 sentencia, 4 ejecución. TareaParaUsuarios 0 ninguno, 1 responsable, 2 usuarios seleccionados, 3 todos. InstanciaId: la instancia a la que pasa el expediente al registrar esta etapa; solo para el flujo principal. Origen Monitorio: Etapa + Cautelar.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Etapa';
GO
CREATE TABLE conf.EtapaUsuarioNotificado (
    EstudioId int NOT NULL,
    EtapaId int NOT NULL,
    UsuarioId int NOT NULL,
    CONSTRAINT PK_conf_EtapaUsuarioNotificado PRIMARY KEY CLUSTERED (EstudioId, EtapaId, UsuarioId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: EtapaAgendaUsuario.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'EtapaUsuarioNotificado';
GO
CREATE TABLE conf.TipoTarea (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Dias int NULL,
    DiasHabiles bit NOT NULL CONSTRAINT DF_conf_TipoTarea_DiasHabiles DEFAULT (1),
    Activo bit NOT NULL CONSTRAINT DF_conf_TipoTarea_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_TipoTarea_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_TipoTarea PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_TipoTarea_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_TipoTarea_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: ActividadAdministrativa.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'TipoTarea';
GO
CREATE TABLE conf.Ubicacion (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Activa bit NOT NULL CONSTRAINT DF_conf_Ubicacion_Activa DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Ubicacion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Ubicacion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Ubicacion_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Ubicacion_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Ubicación física del expediente. Origen Monitorio: Lugar.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Ubicacion';
GO
CREATE TABLE conf.Region (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Region_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Region PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Region_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Region_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Expediente.Region (valores distintos).', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Region';
GO
CREATE TABLE conf.Campania (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Numero nvarchar(50) NOT NULL,
    Nombre nvarchar(100) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Campania_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Campania PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Campania_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_Campania_EstudioId_Numero UNIQUE (EstudioId, Numero)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Expediente.NroCampania/NombreCampania, ExpedienteCodigoDevolucion.Campania.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Campania';
GO
CREATE TABLE conf.TipoEmbargo (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Clase tinyint NOT NULL,
    Observaciones nvarchar(500) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_TipoEmbargo_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_TipoEmbargo PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_TipoEmbargo_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_TipoEmbargo_EstudioId_Nombre UNIQUE (EstudioId, Nombre),
    CONSTRAINT CK_conf_TipoEmbargo_1 CHECK (Clase BETWEEN 1 AND 6)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Clase 1 inmueble, 2 automotor, 3 embargo de cuentas corrientes, 4 inhibición, 5 intervención de caja, 6 reserva de cautelar. La clase define el esquema de DetalleJson del embargo. Origen Monitorio: TipoEmbargo.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'TipoEmbargo';
GO
CREATE TABLE conf.RegistroPublico (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(250) NOT NULL,
    Domicilio nvarchar(500) NULL,
    Zona nvarchar(250) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_RegistroPublico_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_RegistroPublico PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_RegistroPublico_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Registro.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'RegistroPublico';
GO
CREATE TABLE conf.Infraccion (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Codigo nvarchar(50) NULL,
    Nombre nvarchar(200) NOT NULL,
    Descripcion nvarchar(400) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_Infraccion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_Infraccion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_Infraccion_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Infraccion.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'Infraccion';
GO
CREATE TABLE conf.CodigoDevolucion (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Codigo nvarchar(20) NOT NULL,
    Descripcion nvarchar(250) NOT NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_CodigoDevolucion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_CodigoDevolucion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_CodigoDevolucion_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_CodigoDevolucion_EstudioId_Codigo UNIQUE (EstudioId, Codigo)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: CodigoDevolucion.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'CodigoDevolucion';
GO
CREATE TABLE conf.PlantillaEscrito (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(150) NOT NULL,
    Tipo tinyint NOT NULL,
    EtapaId int NULL,
    InstanciaId int NULL,
    EsUnico bit NOT NULL CONSTRAINT DF_conf_PlantillaEscrito_EsUnico DEFAULT (0),
    Activa bit NOT NULL CONSTRAINT DF_conf_PlantillaEscrito_Activa DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_PlantillaEscrito_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_PlantillaEscrito PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_PlantillaEscrito_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_PlantillaEscrito_EstudioId_Nombre UNIQUE (EstudioId, Nombre),
    CONSTRAINT CK_conf_PlantillaEscrito_1 CHECK (Tipo BETWEEN 1 AND 4)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo 1 escrito, 2 cédula, 3 carta, 4 oficio. EsUnico: se genera una sola vez por expediente. Origen Monitorio: EtapaEscrito, Instancia.Path/EscritoNombre, Cautelar.Path/EscritoNombre.', @level0type = N'SCHEMA', @level0name = N'conf', @level1type = N'TABLE', @level1name = N'PlantillaEscrito';
GO
CREATE TABLE dbo.Expediente (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    NroOrden int NOT NULL,
    ComitenteId int NULL,
    ConceptoDeudaId int NULL,
    NroLiquidacion nvarchar(50) NULL,
    FechaTitulo date NULL,
    FechaAlta date NOT NULL,
    MontoTitulo decimal(18,2) NULL,
    MontoCapital decimal(18,2) NULL,
    NroCuenta nvarchar(50) NULL,
    Nomenclatura nvarchar(50) NULL,
    Dominio nvarchar(20) NULL,
    Designacion nvarchar(100) NULL,
    MatriculaRpi nvarchar(50) NULL,
    DomicilioObjeto nvarchar(250) NULL,
    LocalidadObjetoId int NULL,
    RegistroPublicoId int NULL,
    NroResolucionMulta nvarchar(50) NULL,
    FechaResolucion date NULL,
    JuzgadoId int NULL,
    NroExpedienteJudicial nvarchar(50) NULL,
    Caratula nvarchar(400) NULL,
    Recaratulacion nvarchar(400) NULL,
    Sorteo nvarchar(50) NULL,
    FechaSorteo date NULL,
    Cedula int NULL,
    ProcuradorId int NULL,
    PatrocinanteId int NULL,
    AbogadoInteriorId int NULL,
    NegociadorUsuarioId int NULL,
    DepuradorUsuarioId int NULL,
    Comparecio bit NOT NULL CONSTRAINT DF_dbo_Expediente_Comparecio DEFAULT (0),
    FechaComparecio date NULL,
    AbogadoContraparte nvarchar(250) NULL,
    DomicilioAbogadoContraparte nvarchar(250) NULL,
    RegionId int NULL,
    CampaniaId int NULL,
    EsGranContribuyente bit NOT NULL CONSTRAINT DF_dbo_Expediente_EsGranContribuyente DEFAULT (0),
    EsFisca bit NOT NULL CONSTRAINT DF_dbo_Expediente_EsFisca DEFAULT (0),
    ObservacionesTitulo nvarchar(4000) NULL,
    Observaciones nvarchar(4000) NULL,
    ObservacionesAdministrativas nvarchar(4000) NULL,
    EtapaActualId int NULL,
    FechaEtapaActual date NULL,
    CautelarActualId int NULL,
    FechaCautelarActual date NULL,
    InstanciaActualId int NULL,
    FechaInstanciaActual date NULL,
    UbicacionActualId int NULL,
    FechaUltimoMovimiento date NULL,
    LegadoId uniqueidentifier NULL,
    Anulado bit NOT NULL CONSTRAINT DF_dbo_Expediente_Anulado DEFAULT (0),
    AnuladoUtc datetime2(3) NULL,
    AnuladoPor int NULL,
    MotivoAnulacion nvarchar(500) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Expediente_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Expediente PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Expediente_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_dbo_Expediente_EstudioId_NroOrden UNIQUE (EstudioId, NroOrden),
    CONSTRAINT CK_dbo_Expediente_1 CHECK (MontoTitulo IS NULL OR MontoTitulo >= 0),
    CONSTRAINT CK_dbo_Expediente_2 CHECK (NroOrden > 0)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Título de deuda + causa judicial. Las columnas de estado actual reemplazan las ~30 subconsultas TOP 1 de V_Expedientes. Origen Monitorio: Expediente (columnas generales).', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Expediente';
GO
CREATE TABLE dbo.ExpedienteFiscalizacion (
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    TipoFisca int NULL,
    Tipo int NULL,
    NroExpediente nvarchar(50) NULL,
    NroOrden nvarchar(50) NULL,
    NroResolucion nvarchar(50) NULL,
    NroRecursoReconsideracion nvarchar(50) NULL,
    ResolFechaResolucion date NULL,
    ResolFechaNotificacion date NULL,
    ReconFechaResolucion date NULL,
    ReconFechaNotificacion date NULL,
    ContenFechaDemanda date NULL,
    ContenFechaApelacion date NULL,
    ContenFechaDemandaContenciosa date NULL,
    FechaReconocimientoDeuda date NULL,
    ExtiendeResponsabilidad bit NOT NULL CONSTRAINT DF_dbo_ExpedienteFiscalizacion_ExtiendeResponsabilidad DEFAULT (0),
    ResponsableNombre nvarchar(250) NULL,
    ResponsableDomicilio nvarchar(250) NULL,
    Observaciones nvarchar(2000) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_ExpedienteFiscalizacion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_ExpedienteFiscalizacion PRIMARY KEY CLUSTERED (EstudioId, ExpedienteId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'1:1 opcional. Solo para expedientes de fiscalización (EsFisca). Origen Monitorio: Expediente.Fisca*.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ExpedienteFiscalizacion';
GO
CREATE TABLE dbo.ExpedienteActa (
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    FechaLabrado date NULL,
    HoraLabrado nvarchar(20) NULL,
    LugarLabrado nvarchar(250) NULL,
    TipoYNroLicencia nvarchar(250) NULL,
    DescripcionDominio nvarchar(250) NULL,
    NroCaso nvarchar(50) NULL,
    JuzgadoFaltas nvarchar(250) NULL,
    NroTramite nvarchar(50) NULL,
    FechaTramite date NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_ExpedienteActa_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_ExpedienteActa PRIMARY KEY CLUSTERED (EstudioId, ExpedienteId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'1:1 opcional. Actas de infracción (multas). Origen Monitorio: Expediente (FechaLabrado…FechaTramite).', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ExpedienteActa';
GO
CREATE TABLE dbo.ExpedienteInfraccion (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    InfraccionId int NOT NULL,
    Fecha date NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_ExpedienteInfraccion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_ExpedienteInfraccion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_ExpedienteInfraccion_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: InfraccionExpediente.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ExpedienteInfraccion';
GO
CREATE TABLE dbo.ExpedienteActividad (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    ActividadEconomicaId int NOT NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_ExpedienteActividad_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_ExpedienteActividad PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_ExpedienteActividad_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_dbo_ExpedienteActividad_EstudioId_ExpedienteId_ActividadEconomicaId UNIQUE (EstudioId, ExpedienteId, ActividadEconomicaId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: ExpedienteActividad + Expediente.ActividadIIBB/CodigoActividadIIBB.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ExpedienteActividad';
GO
CREATE TABLE dbo.PeriodoAdeudado (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    Anio smallint NOT NULL,
    Cuota nvarchar(50) NULL,
    Concepto nvarchar(50) NULL,
    Vencimiento date NULL,
    Tributo decimal(18,2) NOT NULL CONSTRAINT DF_dbo_PeriodoAdeudado_Tributo DEFAULT (0),
    Recargo decimal(18,2) NOT NULL CONSTRAINT DF_dbo_PeriodoAdeudado_Recargo DEFAULT (0),
    Observaciones nvarchar(2500) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_PeriodoAdeudado_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_PeriodoAdeudado PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_PeriodoAdeudado_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_PeriodoAdeudado_1 CHECK (Anio BETWEEN 1950 AND 2100)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Reemplaza también al texto Expediente.Periodos, que se arma a partir de esta tabla. Origen Monitorio: ExpedientePeriodo.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PeriodoAdeudado';
GO
CREATE TABLE dbo.Persona (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    EsPersonaFisica bit NOT NULL CONSTRAINT DF_dbo_Persona_EsPersonaFisica DEFAULT (1),
    Apellido nvarchar(150) NULL,
    Nombre nvarchar(150) NULL,
    RazonSocial nvarchar(250) NULL,
    TipoDocumentoId int NULL,
    NroDocumento nvarchar(20) NULL,
    Cuit nvarchar(13) NULL,
    Sexo char(1) NULL,
    FechaNacimiento date NULL,
    Telefono1 nvarchar(50) NULL,
    Telefono2 nvarchar(50) NULL,
    Email nvarchar(150) NULL,
    Observaciones nvarchar(4000) NULL,
    LegadoId uniqueidentifier NULL,
    Anulado bit NOT NULL CONSTRAINT DF_dbo_Persona_Anulado DEFAULT (0),
    AnuladoUtc datetime2(3) NULL,
    AnuladoPor int NULL,
    MotivoAnulacion nvarchar(500) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Persona_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Persona PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Persona_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_Persona_1 CHECK (Sexo IN ('F','M','X')),
    CONSTRAINT CK_dbo_Persona_2 CHECK ((EsPersonaFisica = 1 AND Apellido IS NOT NULL) OR (EsPersonaFisica = 0 AND RazonSocial IS NOT NULL))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Persona.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Persona';
GO
CREATE TABLE dbo.Domicilio (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    PersonaId bigint NOT NULL,
    Tipo tinyint NOT NULL CONSTRAINT DF_dbo_Domicilio_Tipo DEFAULT (1),
    Calle nvarchar(150) NULL,
    Numero nvarchar(20) NULL,
    Piso nvarchar(10) NULL,
    Depto nvarchar(10) NULL,
    Barrio nvarchar(100) NULL,
    CodigoPostal nvarchar(10) NULL,
    LocalidadId int NULL,
    Principal bit NOT NULL CONSTRAINT DF_dbo_Domicilio_Principal DEFAULT (0),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Domicilio_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Domicilio PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Domicilio_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_Domicilio_1 CHECK (Tipo BETWEEN 1 AND 4)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo 1 real, 2 fiscal, 3 constituido, 4 postal. Un solo principal por persona (índice filtrado). Origen Monitorio: Domicilio.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Domicilio';
GO
CREATE TABLE dbo.ExpedientePersona (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    PersonaId bigint NOT NULL,
    Rol tinyint NOT NULL,
    Posicion tinyint NOT NULL CONSTRAINT DF_dbo_ExpedientePersona_Posicion DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_ExpedientePersona_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_ExpedientePersona PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_ExpedientePersona_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_dbo_ExpedientePersona_EstudioId_ExpedienteId_PersonaId_Rol UNIQUE (EstudioId, ExpedienteId, PersonaId, Rol),
    CONSTRAINT CK_dbo_ExpedientePersona_1 CHECK (Rol BETWEEN 1 AND 3)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Rol 1 demandado, 2 codemandado, 3 responsable solidario. Sin límite de codemandados. Origen Monitorio: ExpedientePersona (+ Expediente.PersonaID).', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ExpedientePersona';
GO
CREATE TABLE dbo.InformePersona (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    PersonaId bigint NOT NULL,
    Fecha date NOT NULL,
    Texto nvarchar(4000) NOT NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_InformePersona_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_InformePersona PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_InformePersona_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Informe.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'InformePersona';
GO
CREATE TABLE dbo.EtapaExpediente (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    EtapaId int NOT NULL,
    FechaInicio date NOT NULL,
    FechaFin date NULL,
    FechaVencimiento date NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_dbo_EtapaExpediente_Estado DEFAULT (1),
    Monto decimal(18,2) NULL,
    Observaciones nvarchar(2000) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_EtapaExpediente_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_EtapaExpediente PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_EtapaExpediente_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_EtapaExpediente_1 CHECK (Estado BETWEEN 1 AND 4),
    CONSTRAINT CK_dbo_EtapaExpediente_2 CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Historial de etapas de ambos flujos. Estado 1 alta, 2 cumplimentada, 3 suspendida, 4 baja (en el legado 0 = alta). FechaVencimiento se calcula con días hábiles y feriados. Origen Monitorio: EtapaExpediente + CautelarExpediente.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'EtapaExpediente';
GO
CREATE TABLE dbo.InstanciaExpediente (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    InstanciaId int NOT NULL,
    Fecha date NOT NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_InstanciaExpediente_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_InstanciaExpediente PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_InstanciaExpediente_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: InstanciaExpediente.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'InstanciaExpediente';
GO
CREATE TABLE dbo.UbicacionExpediente (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    UbicacionId int NOT NULL,
    Fecha date NOT NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_UbicacionExpediente_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_UbicacionExpediente PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_UbicacionExpediente_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: ExpedienteLugar.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'UbicacionExpediente';
GO
CREATE TABLE dbo.Tarea (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NULL,
    TipoTareaId int NULL,
    Titulo nvarchar(250) NOT NULL,
    Descripcion nvarchar(2000) NULL,
    Lugar nvarchar(250) NULL,
    FechaInicio date NOT NULL,
    FechaVencimiento date NULL,
    HoraInicio time(0) NULL,
    HoraFin time(0) NULL,
    TodoElDia bit NOT NULL CONSTRAINT DF_dbo_Tarea_TodoElDia DEFAULT (1),
    Alerta bit NOT NULL CONSTRAINT DF_dbo_Tarea_Alerta DEFAULT (0),
    RepetirHasta date NULL,
    ResponsableUsuarioId int NULL,
    VisibleParaTodos bit NOT NULL CONSTRAINT DF_dbo_Tarea_VisibleParaTodos DEFAULT (0),
    Estado tinyint NOT NULL CONSTRAINT DF_dbo_Tarea_Estado DEFAULT (1),
    CumplidaUtc datetime2(3) NULL,
    Origen tinyint NOT NULL CONSTRAINT DF_dbo_Tarea_Origen DEFAULT (1),
    EtapaExpedienteId bigint NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Tarea_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Tarea PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Tarea_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_Tarea_1 CHECK (Estado BETWEEN 1 AND 4),
    CONSTRAINT CK_dbo_Tarea_2 CHECK (Origen BETWEEN 1 AND 4)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado 1 pendiente, 2 cumplida, 3 archivada, 4 anulada. Origen 1 manual, 2 generada por etapa, 3 desde novedad, 4 migrada de Agenda. Origen Monitorio: ActividadAdministrativaExpediente + Agenda.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Tarea';
GO
CREATE TABLE dbo.TareaParticipante (
    EstudioId int NOT NULL,
    TareaId bigint NOT NULL,
    UsuarioId int NOT NULL,
    CONSTRAINT PK_dbo_TareaParticipante PRIMARY KEY CLUSTERED (EstudioId, TareaId, UsuarioId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: AgendaUsuario.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'TareaParticipante';
GO
CREATE TABLE dbo.NovedadJudicial (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NULL,
    NroExpedienteInformado nvarchar(50) NULL,
    Fecha date NOT NULL,
    Texto nvarchar(4000) NOT NULL,
    Fuente tinyint NOT NULL CONSTRAINT DF_dbo_NovedadJudicial_Fuente DEFAULT (1),
    Leida bit NOT NULL CONSTRAINT DF_dbo_NovedadJudicial_Leida DEFAULT (0),
    LeidaUtc datetime2(3) NULL,
    LeidaPor int NULL,
    TareaId bigint NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_NovedadJudicial_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_NovedadJudicial PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_NovedadJudicial_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_NovedadJudicial_1 CHECK (Fuente BETWEEN 1 AND 3)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Proveídos, decretos y partes. Fuente 1 manual, 2 importación de archivo, 3 migrada. ExpedienteId NULL = pendiente de vincular. Origen Monitorio: ExpedienteParte.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'NovedadJudicial';
GO
CREATE TABLE dbo.Embargo (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    TipoEmbargoId int NOT NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_dbo_Embargo_Estado DEFAULT (1),
    FechaSolicitud date NULL,
    FechaTraba date NULL,
    FechaLevantamiento date NULL,
    Monto decimal(18,2) NULL,
    Porcentaje decimal(5,2) NULL,
    RegistroPublicoId int NULL,
    NroRegistro nvarchar(200) NULL,
    DireccionRegistro nvarchar(200) NULL,
    Secretaria nvarchar(200) NULL,
    Autorizados nvarchar(500) NULL,
    Dominio nvarchar(20) NULL,
    Nomenclatura nvarchar(200) NULL,
    MartilleroId int NULL,
    DetalleJson nvarchar(max) NULL,
    Observaciones nvarchar(4000) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Embargo_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Embargo PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Embargo_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_Embargo_1 CHECK (Estado BETWEEN 1 AND 4),
    CONSTRAINT CK_dbo_Embargo_2 CHECK (Porcentaje IS NULL OR Porcentaje BETWEEN 0 AND 100),
    CONSTRAINT CK_dbo_Embargo_3 CHECK (DetalleJson IS NULL OR ISJSON(DetalleJson) = 1)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado 1 solicitado, 2 trabado, 3 levantado, 4 sin efecto (nuevo: el legado no tenía estado; se migra como 2). DetalleJson guarda los datos propios de cada clase (marca, modelo, chasis, sticker; lote, manzana, PH; banco, CBU; etc.). Origen Monitorio: EmbargoExpediente.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Embargo';
GO
CREATE TABLE dbo.PlanPago (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    NroPlan nvarchar(40) NULL,
    FechaPlan date NULL,
    PeriodosIncluidos nvarchar(4000) NULL,
    CantCuotas int NULL,
    CuotasPagadas int NULL,
    CuotasVencidasImpagas int NULL,
    CuotasAVencer int NULL,
    FechaCaducidad date NULL,
    Estado nvarchar(50) NULL,
    FechaConsulta date NULL,
    Decreto nvarchar(4000) NULL,
    Activo bit NOT NULL CONSTRAINT DF_dbo_PlanPago_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_PlanPago_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_PlanPago PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_PlanPago_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado es texto libre, como EstadoPlan en Monitorio. Activo indica el plan vigente del expediente. Origen Monitorio: ExpedientePlanPago (+ Expediente.NroPlanPagos).', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PlanPago';
GO
CREATE TABLE dbo.Requerimiento (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    Fecha date NOT NULL,
    Descripcion nvarchar(2000) NULL,
    Enviado bit NOT NULL CONSTRAINT DF_dbo_Requerimiento_Enviado DEFAULT (0),
    EnFolio bit NOT NULL CONSTRAINT DF_dbo_Requerimiento_EnFolio DEFAULT (0),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Requerimiento_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Requerimiento PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Requerimiento_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: Requerimiento.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Requerimiento';
GO
CREATE TABLE dbo.EstudioTitulo (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    Fecha date NOT NULL,
    Descripcion nvarchar(2000) NULL,
    Pedido bit NOT NULL CONSTRAINT DF_dbo_EstudioTitulo_Pedido DEFAULT (0),
    EnFolio bit NOT NULL CONSTRAINT DF_dbo_EstudioTitulo_EnFolio DEFAULT (0),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_EstudioTitulo_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_EstudioTitulo PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_EstudioTitulo_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: EstudioTitulo.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'EstudioTitulo';
GO
CREATE TABLE dbo.DevolucionCorreo (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    CodigoDevolucionId int NOT NULL,
    Fecha date NOT NULL,
    CampaniaId int NULL,
    NroCaso nvarchar(50) NULL,
    UsuarioId int NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_DevolucionCorreo_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_DevolucionCorreo PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_DevolucionCorreo_EstudioId_Id UNIQUE (EstudioId, Id)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: ExpedienteCodigoDevolucion.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'DevolucionCorreo';
GO
CREATE TABLE dbo.Documento (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NULL,
    Tipo tinyint NOT NULL,
    NombreArchivo nvarchar(260) NOT NULL,
    ContentType nvarchar(100) NOT NULL,
    TamanioBytes bigint NOT NULL,
    Sha256 char(64) NOT NULL,
    RutaAlmacenamiento nvarchar(500) NOT NULL,
    Descripcion nvarchar(500) NULL,
    Eliminado bit NOT NULL CONSTRAINT DF_dbo_Documento_Eliminado DEFAULT (0),
    EliminadoUtc datetime2(3) NULL,
    EliminadoPor int NULL,
    MotivoEliminacion nvarchar(500) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_Documento_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_Documento PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_Documento_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_Documento_1 CHECK (Tipo BETWEEN 1 AND 5),
    CONSTRAINT CK_dbo_Documento_2 CHECK ((Eliminado = 0) OR (EliminadoUtc IS NOT NULL AND EliminadoPor IS NOT NULL AND MotivoEliminacion IS NOT NULL))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo 1 adjunto, 2 escrito generado, 3 liquidación exportada, 4 plantilla, 5 importación. Contenido en almacenamiento de archivos con prefijo de Estudio; sin borrado automático: se conserva hasta que un usuario con permiso lo elimina. ', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Documento';
GO
CREATE TABLE conf.PlantillaEscritoVersion (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    PlantillaEscritoId int NOT NULL,
    NroVersion int NOT NULL,
    DocumentoId bigint NOT NULL,
    Notas nvarchar(500) NULL,
    Vigente bit NOT NULL CONSTRAINT DF_conf_PlantillaEscritoVersion_Vigente DEFAULT (1),
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_conf_PlantillaEscritoVersion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_conf_PlantillaEscritoVersion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_conf_PlantillaEscritoVersion_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_conf_PlantillaEscritoVersion_EstudioId_PlantillaEscritoId_NroVersion UNIQUE (EstudioId, PlantillaEscritoId, NroVersion)
);
GO
CREATE TABLE dbo.EscritoGenerado (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    PlantillaEscritoVersionId int NULL,
    EtapaId int NULL,
    InstanciaId int NULL,
    TrabajoProcesoId bigint NULL,
    UsuarioId int NULL,
    FechaUtc datetime2(3) NOT NULL,
    DocumentoId bigint NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_dbo_EscritoGenerado_Estado DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_dbo_EscritoGenerado_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_dbo_EscritoGenerado PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_dbo_EscritoGenerado_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_dbo_EscritoGenerado_1 CHECK (Estado BETWEEN 1 AND 3)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado 1 generado, 2 presentado, 3 anulado. Los migrados no tienen documento (el legado no lo guardaba). Origen Monitorio: ExpedienteEtapaEscrito + ExpedienteInstanciaEscrito.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'EscritoGenerado';
GO
CREATE TABLE liq.ParametroLiquidacion (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ComitenteId int NULL,
    Codigo nvarchar(50) NOT NULL,
    Valor decimal(18,6) NULL,
    ValorFecha date NULL,
    VigenteDesde date NOT NULL,
    VigenteHasta date NULL,
    Observaciones nvarchar(500) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_ParametroLiquidacion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_ParametroLiquidacion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_ParametroLiquidacion_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_ParametroLiquidacion_EstudioId_Codigo_ComitenteId_VigenteDesde UNIQUE (EstudioId, Codigo, ComitenteId, VigenteDesde),
    CONSTRAINT CK_liq_ParametroLiquidacion_1 CHECK (Valor IS NOT NULL OR ValorFecha IS NOT NULL),
    CONSTRAINT CK_liq_ParametroLiquidacion_2 CHECK (VigenteHasta IS NULL OR VigenteHasta >= VigenteDesde)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Parámetros con vigencia. ComitenteId NULL = valor general del Estudio; con ComitenteId = particular del comitente (tiene prioridad). Origen Monitorio: UnidadPlanilla + valores de App.config + constantes en frmPlanilla.cs.', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'ParametroLiquidacion';
GO
CREATE TABLE liq.TasaInteres (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ComitenteId int NULL,
    Codigo nvarchar(50) NOT NULL,
    Periodicidad tinyint NOT NULL,
    Valor decimal(12,6) NOT NULL,
    VigenteDesde date NOT NULL,
    VigenteHasta date NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_TasaInteres_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_TasaInteres PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_TasaInteres_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_TasaInteres_EstudioId_Codigo_ComitenteId_VigenteDesde UNIQUE (EstudioId, Codigo, ComitenteId, VigenteDesde),
    CONSTRAINT CK_liq_TasaInteres_1 CHECK (Periodicidad IN (1,2,3))
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Periodicidad 1 diaria, 2 mensual, 3 anual. Valor en tanto por uno (0.000830 = 0,083 %). Reemplaza 0,083 % y 0,1 % diarios fijos en código. ', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'TasaInteres';
GO
CREATE TABLE liq.EscalaHonorarios (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ComitenteId int NULL,
    VigenteDesde date NOT NULL,
    BaseDesde decimal(18,2) NOT NULL,
    BaseHasta decimal(18,2) NULL,
    Porcentaje decimal(7,4) NOT NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_EscalaHonorarios_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_EscalaHonorarios PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_EscalaHonorarios_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_EscalaHonorarios_EstudioId_ComitenteId_VigenteDesde_BaseDesde UNIQUE (EstudioId, ComitenteId, VigenteDesde, BaseDesde)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tramos de la escala (9 % … 3 % en el legado, fijos en código). ', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'EscalaHonorarios';
GO
CREATE TABLE liq.RubroPlanilla (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Nombre nvarchar(50) NOT NULL,
    Descripcion nvarchar(400) NULL,
    DescripcionTotal nvarchar(100) NULL,
    Orden int NOT NULL,
    PadreId int NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_RubroPlanilla_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_RubroPlanilla PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_RubroPlanilla_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_RubroPlanilla_EstudioId_Nombre UNIQUE (EstudioId, Nombre)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: RubroPlanilla.', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'RubroPlanilla';
GO
CREATE TABLE liq.ItemPlanilla (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    RubroPlanillaId int NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Descripcion nvarchar(4000) NULL,
    TipoCalculo tinyint NOT NULL,
    Valor decimal(18,6) NULL,
    EsPorcentaje bit NOT NULL CONSTRAINT DF_liq_ItemPlanilla_EsPorcentaje DEFAULT (0),
    MontoMinimo decimal(18,2) NULL,
    CargaManual bit NOT NULL CONSTRAINT DF_liq_ItemPlanilla_CargaManual DEFAULT (0),
    EsSubtotal bit NOT NULL CONSTRAINT DF_liq_ItemPlanilla_EsSubtotal DEFAULT (0),
    EsDefault bit NOT NULL CONSTRAINT DF_liq_ItemPlanilla_EsDefault DEFAULT (0),
    OrdenDefault int NULL,
    Activo bit NOT NULL CONSTRAINT DF_liq_ItemPlanilla_Activo DEFAULT (1),
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_ItemPlanilla_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_ItemPlanilla PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_ItemPlanilla_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_ItemPlanilla_EstudioId_Nombre UNIQUE (EstudioId, Nombre),
    CONSTRAINT CK_liq_ItemPlanilla_1 CHECK (TipoCalculo BETWEEN 1 AND 11)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'TipoCalculo reemplaza las 9 columnas Es*: 1 monto fijo, 2 porcentaje del capital, 3 capital, 4 interés simple, 5 tasa pasiva, 6 interés sobre capital, 7 honorarios, 8 tasa de justicia, 9 aportes, 10 gastos de diligencias, 11 subtotal. Origen Monitorio: ItemsPlanilla.', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'ItemPlanilla';
GO
CREATE TABLE liq.ItemPlanillaTope (
    Id int IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ItemPlanillaId int NOT NULL,
    VigenteDesde date NOT NULL,
    VigenteHasta date NULL,
    Minimo decimal(18,2) NULL,
    Maximo decimal(18,2) NULL,
    LegadoId uniqueidentifier NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_ItemPlanillaTope_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_ItemPlanillaTope PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_ItemPlanillaTope_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_ItemPlanillaTope_EstudioId_ItemPlanillaId_VigenteDesde UNIQUE (EstudioId, ItemPlanillaId, VigenteDesde),
    CONSTRAINT CK_liq_ItemPlanillaTope_1 CHECK (Maximo IS NULL OR Minimo IS NULL OR Maximo >= Minimo)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: ItemsMontoMinMax (IemsMontoMinMax se descarta tras comparar contenido).', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'ItemPlanillaTope';
GO
CREATE TABLE liq.PlanillaJuzgado (
    EstudioId int NOT NULL,
    JuzgadoId int NOT NULL,
    ItemPlanillaId int NOT NULL,
    Orden int NOT NULL,
    CONSTRAINT PK_liq_PlanillaJuzgado PRIMARY KEY CLUSTERED (EstudioId, JuzgadoId, ItemPlanillaId)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Origen Monitorio: JuzgadoItems.', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'PlanillaJuzgado';
GO
CREATE TABLE liq.Liquidacion (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    ExpedienteId bigint NOT NULL,
    NroVersion int NOT NULL,
    FechaCalculo date NOT NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_liq_Liquidacion_Estado DEFAULT (1),
    JuzgadoId int NULL,
    TotalCapital decimal(18,2) NOT NULL,
    TotalInteres decimal(18,2) NOT NULL,
    TotalGastos decimal(18,2) NOT NULL,
    HonorariosJus decimal(9,2) NULL,
    HonorariosPesos decimal(18,2) NOT NULL,
    Total decimal(18,2) NOT NULL,
    ParametrosJson nvarchar(max) NOT NULL,
    DocumentoId bigint NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_liq_Liquidacion_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_liq_Liquidacion PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_Liquidacion_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_Liquidacion_EstudioId_ExpedienteId_NroVersion UNIQUE (EstudioId, ExpedienteId, NroVersion),
    CONSTRAINT CK_liq_Liquidacion_1 CHECK (Estado BETWEEN 1 AND 4),
    CONSTRAINT CK_liq_Liquidacion_2 CHECK (ISJSON(ParametrosJson) = 1),
    CONSTRAINT CK_liq_Liquidacion_3 CHECK (Total = TotalCapital + TotalInteres + TotalGastos + HonorariosPesos)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nueva: el legado no guardaba planillas. Estado 1 borrador, 2 guardada, 3 presentada, 4 anulada. ParametrosJson copia los valores usados para que sea reproducible. ', @level0type = N'SCHEMA', @level0name = N'liq', @level1type = N'TABLE', @level1name = N'Liquidacion';
GO
CREATE TABLE liq.LiquidacionLinea (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    LiquidacionId bigint NOT NULL,
    Orden int NOT NULL,
    RubroPlanillaId int NULL,
    ItemPlanillaId int NULL,
    Concepto nvarchar(400) NOT NULL,
    PeriodoAdeudadoId bigint NULL,
    Base decimal(18,2) NULL,
    Tasa decimal(12,6) NULL,
    Dias int NULL,
    Importe decimal(18,2) NOT NULL,
    EsManual bit NOT NULL CONSTRAINT DF_liq_LiquidacionLinea_EsManual DEFAULT (0),
    Detalle nvarchar(1000) NULL,
    CONSTRAINT PK_liq_LiquidacionLinea PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_liq_LiquidacionLinea_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT UQ_liq_LiquidacionLinea_EstudioId_LiquidacionId_Orden UNIQUE (EstudioId, LiquidacionId, Orden)
);
GO
CREATE TABLE aud.AuditoriaEvento (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NULL,
    UsuarioId int NULL,
    FechaUtc datetime2(3) NOT NULL,
    Modulo nvarchar(40) NOT NULL,
    Entidad nvarchar(60) NULL,
    EntidadId nvarchar(40) NULL,
    Accion tinyint NOT NULL,
    Resultado tinyint NOT NULL CONSTRAINT DF_aud_AuditoriaEvento_Resultado DEFAULT (1),
    CambiosJson nvarchar(max) NULL,
    TrabajoProcesoId bigint NULL,
    CorrelacionId uniqueidentifier NULL,
    Ip varchar(45) NULL,
    UserAgent nvarchar(300) NULL,
    CONSTRAINT PK_aud_AuditoriaEvento PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_aud_AuditoriaEvento_1 CHECK (Accion BETWEEN 1 AND 14),
    CONSTRAINT CK_aud_AuditoriaEvento_2 CHECK (Resultado BETWEEN 1 AND 3),
    CONSTRAINT CK_aud_AuditoriaEvento_3 CHECK (CambiosJson IS NULL OR ISJSON(CambiosJson) = 1)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Solo inserción. EstudioId NULL = evento de plataforma. Accion 1 crear, 2 editar, 3 anular, 4 eliminar, 5 cambiar estado, 6 calcular, 7 generar, 8 exportar, 9 login, 10 login fallido, 11 logout, 12 cambiar permisos, 13 aprobar membresía, 14 acceso de soporte. Resultado 1 ok, 2 rechazado, 3 error. Origen Monitorio: Nueva: reemplaza a LogSistema, que no se migra.', @level0type = N'SCHEMA', @level0name = N'aud', @level1type = N'TABLE', @level1name = N'AuditoriaEvento';
GO
CREATE TABLE mig.DatoARevisar (
    Id bigint IDENTITY(1,1) NOT NULL,
    EstudioId int NOT NULL,
    Tipo nvarchar(60) NOT NULL,
    Entidad nvarchar(60) NOT NULL,
    EntidadId bigint NULL,
    TablaOrigen nvarchar(60) NULL,
    ClaveOrigen uniqueidentifier NULL,
    Campo nvarchar(60) NULL,
    ValorOriginal nvarchar(4000) NULL,
    ValorPropuesto nvarchar(4000) NULL,
    Estado tinyint NOT NULL CONSTRAINT DF_mig_DatoARevisar_Estado DEFAULT (1),
    ResueltoPor int NULL,
    ResueltoUtc datetime2(3) NULL,
    Comentario nvarchar(1000) NULL,
    CreadoUtc datetime2(3) NOT NULL CONSTRAINT DF_mig_DatoARevisar_CreadoUtc DEFAULT (SYSUTCDATETIME()),
    CreadoPor int NULL,
    ModificadoUtc datetime2(3) NULL,
    ModificadoPor int NULL,
    Version rowversion,
    CONSTRAINT PK_mig_DatoARevisar PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_mig_DatoARevisar_EstudioId_Id UNIQUE (EstudioId, Id),
    CONSTRAINT CK_mig_DatoARevisar_1 CHECK (Estado BETWEEN 1 AND 3)
);
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Cola de la pantalla Datos a revisar. Estado 1 pendiente, 2 resuelto, 3 descartado. ', @level0type = N'SCHEMA', @level0name = N'mig', @level1type = N'TABLE', @level1name = N'DatoARevisar';
GO

-- ---------------------------------------------------------------- claves foráneas
ALTER TABLE seg.Estudio ADD CONSTRAINT FK_seg_Estudio_EstudioOrigenCopiaId FOREIGN KEY (EstudioOrigenCopiaId) REFERENCES seg.Estudio (Id);
ALTER TABLE cat.Localidad ADD CONSTRAINT FK_cat_Localidad_ProvinciaId FOREIGN KEY (ProvinciaId) REFERENCES cat.Provincia (Id);
ALTER TABLE cat.Feriado ADD CONSTRAINT FK_cat_Feriado_ProvinciaId FOREIGN KEY (ProvinciaId) REFERENCES cat.Provincia (Id);
ALTER TABLE conf.Comitente ADD CONSTRAINT FK_conf_Comitente_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Comitente ADD CONSTRAINT FK_conf_Comitente_LocalidadId FOREIGN KEY (LocalidadId) REFERENCES cat.Localidad (Id);
ALTER TABLE conf.ConceptoDeuda ADD CONSTRAINT FK_conf_ConceptoDeuda_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.ConceptoDeuda ADD CONSTRAINT FK_conf_ConceptoDeuda_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE conf.Profesional ADD CONSTRAINT FK_conf_Profesional_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Profesional ADD CONSTRAINT FK_conf_Profesional_CondicionIvaId FOREIGN KEY (CondicionIvaId) REFERENCES cat.CondicionIva (Id);
ALTER TABLE conf.Profesional ADD CONSTRAINT FK_conf_Profesional_LocalidadId FOREIGN KEY (LocalidadId) REFERENCES cat.Localidad (Id);
ALTER TABLE seg.EstudioUsuario ADD CONSTRAINT FK_seg_EstudioUsuario_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.EstudioUsuario ADD CONSTRAINT FK_seg_EstudioUsuario_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE seg.EstudioUsuario ADD CONSTRAINT FK_seg_EstudioUsuario_ProfesionalId FOREIGN KEY (EstudioId, ProfesionalId) REFERENCES conf.Profesional (EstudioId, Id);
ALTER TABLE seg.EstudioUsuario ADD CONSTRAINT FK_seg_EstudioUsuario_InvitadoPor FOREIGN KEY (InvitadoPor) REFERENCES seg.Usuario (Id);
ALTER TABLE seg.EstudioUsuario ADD CONSTRAINT FK_seg_EstudioUsuario_AprobadoPor FOREIGN KEY (AprobadoPor) REFERENCES seg.Usuario (Id);
ALTER TABLE seg.Rol ADD CONSTRAINT FK_seg_Rol_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.RolPermiso ADD CONSTRAINT FK_seg_RolPermiso_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.RolPermiso ADD CONSTRAINT FK_seg_RolPermiso_RolId FOREIGN KEY (EstudioId, RolId) REFERENCES seg.Rol (EstudioId, Id);
ALTER TABLE seg.RolPermiso ADD CONSTRAINT FK_seg_RolPermiso_PermisoId FOREIGN KEY (PermisoId) REFERENCES seg.Permiso (Id);
ALTER TABLE seg.EstudioUsuarioRol ADD CONSTRAINT FK_seg_EstudioUsuarioRol_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.EstudioUsuarioRol ADD CONSTRAINT FK_seg_EstudioUsuarioRol_EstudioUsuarioId FOREIGN KEY (EstudioId, EstudioUsuarioId) REFERENCES seg.EstudioUsuario (EstudioId, Id);
ALTER TABLE seg.EstudioUsuarioRol ADD CONSTRAINT FK_seg_EstudioUsuarioRol_RolId FOREIGN KEY (EstudioId, RolId) REFERENCES seg.Rol (EstudioId, Id);
ALTER TABLE seg.EstudioUsuarioRol ADD CONSTRAINT FK_seg_EstudioUsuarioRol_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE seg.SecuenciaEstudio ADD CONSTRAINT FK_seg_SecuenciaEstudio_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.AccesoSoporte ADD CONSTRAINT FK_seg_AccesoSoporte_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE seg.AccesoSoporte ADD CONSTRAINT FK_seg_AccesoSoporte_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE seg.AccesoSoporte ADD CONSTRAINT FK_seg_AccesoSoporte_OtorgadoPor FOREIGN KEY (OtorgadoPor) REFERENCES seg.Usuario (Id);
ALTER TABLE seg.TrabajoProceso ADD CONSTRAINT FK_seg_TrabajoProceso_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.ConfiguracionEstudio ADD CONSTRAINT FK_conf_ConfiguracionEstudio_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.FeriadoEstudio ADD CONSTRAINT FK_conf_FeriadoEstudio_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Juzgado ADD CONSTRAINT FK_conf_Juzgado_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Juzgado ADD CONSTRAINT FK_conf_Juzgado_LocalidadId FOREIGN KEY (LocalidadId) REFERENCES cat.Localidad (Id);
ALTER TABLE conf.Instancia ADD CONSTRAINT FK_conf_Instancia_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Etapa ADD CONSTRAINT FK_conf_Etapa_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Etapa ADD CONSTRAINT FK_conf_Etapa_EtapaAnteriorId FOREIGN KEY (EstudioId, EtapaAnteriorId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE conf.Etapa ADD CONSTRAINT FK_conf_Etapa_EtapaSiguienteId FOREIGN KEY (EstudioId, EtapaSiguienteId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE conf.Etapa ADD CONSTRAINT FK_conf_Etapa_InstanciaId FOREIGN KEY (EstudioId, InstanciaId) REFERENCES conf.Instancia (EstudioId, Id);
ALTER TABLE conf.EtapaUsuarioNotificado ADD CONSTRAINT FK_conf_EtapaUsuarioNotificado_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.EtapaUsuarioNotificado ADD CONSTRAINT FK_conf_EtapaUsuarioNotificado_EtapaId FOREIGN KEY (EstudioId, EtapaId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE conf.EtapaUsuarioNotificado ADD CONSTRAINT FK_conf_EtapaUsuarioNotificado_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE conf.TipoTarea ADD CONSTRAINT FK_conf_TipoTarea_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Ubicacion ADD CONSTRAINT FK_conf_Ubicacion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Region ADD CONSTRAINT FK_conf_Region_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Campania ADD CONSTRAINT FK_conf_Campania_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.TipoEmbargo ADD CONSTRAINT FK_conf_TipoEmbargo_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.RegistroPublico ADD CONSTRAINT FK_conf_RegistroPublico_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.Infraccion ADD CONSTRAINT FK_conf_Infraccion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.CodigoDevolucion ADD CONSTRAINT FK_conf_CodigoDevolucion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.PlantillaEscrito ADD CONSTRAINT FK_conf_PlantillaEscrito_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.PlantillaEscrito ADD CONSTRAINT FK_conf_PlantillaEscrito_EtapaId FOREIGN KEY (EstudioId, EtapaId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE conf.PlantillaEscrito ADD CONSTRAINT FK_conf_PlantillaEscrito_InstanciaId FOREIGN KEY (EstudioId, InstanciaId) REFERENCES conf.Instancia (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_ConceptoDeudaId FOREIGN KEY (EstudioId, ConceptoDeudaId) REFERENCES conf.ConceptoDeuda (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_LocalidadObjetoId FOREIGN KEY (LocalidadObjetoId) REFERENCES cat.Localidad (Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_RegistroPublicoId FOREIGN KEY (EstudioId, RegistroPublicoId) REFERENCES conf.RegistroPublico (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_JuzgadoId FOREIGN KEY (EstudioId, JuzgadoId) REFERENCES conf.Juzgado (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_ProcuradorId FOREIGN KEY (EstudioId, ProcuradorId) REFERENCES conf.Profesional (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_PatrocinanteId FOREIGN KEY (EstudioId, PatrocinanteId) REFERENCES conf.Profesional (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_AbogadoInteriorId FOREIGN KEY (EstudioId, AbogadoInteriorId) REFERENCES conf.Profesional (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_NegociadorUsuarioId FOREIGN KEY (NegociadorUsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_DepuradorUsuarioId FOREIGN KEY (DepuradorUsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_RegionId FOREIGN KEY (EstudioId, RegionId) REFERENCES conf.Region (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_CampaniaId FOREIGN KEY (EstudioId, CampaniaId) REFERENCES conf.Campania (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_EtapaActualId FOREIGN KEY (EstudioId, EtapaActualId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_CautelarActualId FOREIGN KEY (EstudioId, CautelarActualId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_InstanciaActualId FOREIGN KEY (EstudioId, InstanciaActualId) REFERENCES conf.Instancia (EstudioId, Id);
ALTER TABLE dbo.Expediente ADD CONSTRAINT FK_dbo_Expediente_UbicacionActualId FOREIGN KEY (EstudioId, UbicacionActualId) REFERENCES conf.Ubicacion (EstudioId, Id);
ALTER TABLE dbo.ExpedienteFiscalizacion ADD CONSTRAINT FK_dbo_ExpedienteFiscalizacion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.ExpedienteFiscalizacion ADD CONSTRAINT FK_dbo_ExpedienteFiscalizacion_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.ExpedienteActa ADD CONSTRAINT FK_dbo_ExpedienteActa_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.ExpedienteActa ADD CONSTRAINT FK_dbo_ExpedienteActa_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.ExpedienteInfraccion ADD CONSTRAINT FK_dbo_ExpedienteInfraccion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.ExpedienteInfraccion ADD CONSTRAINT FK_dbo_ExpedienteInfraccion_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.ExpedienteInfraccion ADD CONSTRAINT FK_dbo_ExpedienteInfraccion_InfraccionId FOREIGN KEY (EstudioId, InfraccionId) REFERENCES conf.Infraccion (EstudioId, Id);
ALTER TABLE dbo.ExpedienteActividad ADD CONSTRAINT FK_dbo_ExpedienteActividad_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.ExpedienteActividad ADD CONSTRAINT FK_dbo_ExpedienteActividad_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.ExpedienteActividad ADD CONSTRAINT FK_dbo_ExpedienteActividad_ActividadEconomicaId FOREIGN KEY (ActividadEconomicaId) REFERENCES cat.ActividadEconomica (Id);
ALTER TABLE dbo.PeriodoAdeudado ADD CONSTRAINT FK_dbo_PeriodoAdeudado_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.PeriodoAdeudado ADD CONSTRAINT FK_dbo_PeriodoAdeudado_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.Persona ADD CONSTRAINT FK_dbo_Persona_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Persona ADD CONSTRAINT FK_dbo_Persona_TipoDocumentoId FOREIGN KEY (TipoDocumentoId) REFERENCES cat.TipoDocumento (Id);
ALTER TABLE dbo.Domicilio ADD CONSTRAINT FK_dbo_Domicilio_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Domicilio ADD CONSTRAINT FK_dbo_Domicilio_PersonaId FOREIGN KEY (EstudioId, PersonaId) REFERENCES dbo.Persona (EstudioId, Id);
ALTER TABLE dbo.Domicilio ADD CONSTRAINT FK_dbo_Domicilio_LocalidadId FOREIGN KEY (LocalidadId) REFERENCES cat.Localidad (Id);
ALTER TABLE dbo.ExpedientePersona ADD CONSTRAINT FK_dbo_ExpedientePersona_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.ExpedientePersona ADD CONSTRAINT FK_dbo_ExpedientePersona_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.ExpedientePersona ADD CONSTRAINT FK_dbo_ExpedientePersona_PersonaId FOREIGN KEY (EstudioId, PersonaId) REFERENCES dbo.Persona (EstudioId, Id);
ALTER TABLE dbo.InformePersona ADD CONSTRAINT FK_dbo_InformePersona_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.InformePersona ADD CONSTRAINT FK_dbo_InformePersona_PersonaId FOREIGN KEY (EstudioId, PersonaId) REFERENCES dbo.Persona (EstudioId, Id);
ALTER TABLE dbo.EtapaExpediente ADD CONSTRAINT FK_dbo_EtapaExpediente_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.EtapaExpediente ADD CONSTRAINT FK_dbo_EtapaExpediente_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.EtapaExpediente ADD CONSTRAINT FK_dbo_EtapaExpediente_EtapaId FOREIGN KEY (EstudioId, EtapaId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE dbo.InstanciaExpediente ADD CONSTRAINT FK_dbo_InstanciaExpediente_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.InstanciaExpediente ADD CONSTRAINT FK_dbo_InstanciaExpediente_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.InstanciaExpediente ADD CONSTRAINT FK_dbo_InstanciaExpediente_InstanciaId FOREIGN KEY (EstudioId, InstanciaId) REFERENCES conf.Instancia (EstudioId, Id);
ALTER TABLE dbo.UbicacionExpediente ADD CONSTRAINT FK_dbo_UbicacionExpediente_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.UbicacionExpediente ADD CONSTRAINT FK_dbo_UbicacionExpediente_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.UbicacionExpediente ADD CONSTRAINT FK_dbo_UbicacionExpediente_UbicacionId FOREIGN KEY (EstudioId, UbicacionId) REFERENCES conf.Ubicacion (EstudioId, Id);
ALTER TABLE dbo.Tarea ADD CONSTRAINT FK_dbo_Tarea_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Tarea ADD CONSTRAINT FK_dbo_Tarea_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.Tarea ADD CONSTRAINT FK_dbo_Tarea_TipoTareaId FOREIGN KEY (EstudioId, TipoTareaId) REFERENCES conf.TipoTarea (EstudioId, Id);
ALTER TABLE dbo.Tarea ADD CONSTRAINT FK_dbo_Tarea_ResponsableUsuarioId FOREIGN KEY (ResponsableUsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.Tarea ADD CONSTRAINT FK_dbo_Tarea_EtapaExpedienteId FOREIGN KEY (EstudioId, EtapaExpedienteId) REFERENCES dbo.EtapaExpediente (EstudioId, Id);
ALTER TABLE dbo.TareaParticipante ADD CONSTRAINT FK_dbo_TareaParticipante_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.TareaParticipante ADD CONSTRAINT FK_dbo_TareaParticipante_TareaId FOREIGN KEY (EstudioId, TareaId) REFERENCES dbo.Tarea (EstudioId, Id);
ALTER TABLE dbo.TareaParticipante ADD CONSTRAINT FK_dbo_TareaParticipante_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.NovedadJudicial ADD CONSTRAINT FK_dbo_NovedadJudicial_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.NovedadJudicial ADD CONSTRAINT FK_dbo_NovedadJudicial_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.NovedadJudicial ADD CONSTRAINT FK_dbo_NovedadJudicial_LeidaPor FOREIGN KEY (LeidaPor) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.NovedadJudicial ADD CONSTRAINT FK_dbo_NovedadJudicial_TareaId FOREIGN KEY (EstudioId, TareaId) REFERENCES dbo.Tarea (EstudioId, Id);
ALTER TABLE dbo.Embargo ADD CONSTRAINT FK_dbo_Embargo_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Embargo ADD CONSTRAINT FK_dbo_Embargo_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.Embargo ADD CONSTRAINT FK_dbo_Embargo_TipoEmbargoId FOREIGN KEY (EstudioId, TipoEmbargoId) REFERENCES conf.TipoEmbargo (EstudioId, Id);
ALTER TABLE dbo.Embargo ADD CONSTRAINT FK_dbo_Embargo_RegistroPublicoId FOREIGN KEY (EstudioId, RegistroPublicoId) REFERENCES conf.RegistroPublico (EstudioId, Id);
ALTER TABLE dbo.Embargo ADD CONSTRAINT FK_dbo_Embargo_MartilleroId FOREIGN KEY (EstudioId, MartilleroId) REFERENCES conf.Profesional (EstudioId, Id);
ALTER TABLE dbo.PlanPago ADD CONSTRAINT FK_dbo_PlanPago_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.PlanPago ADD CONSTRAINT FK_dbo_PlanPago_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.Requerimiento ADD CONSTRAINT FK_dbo_Requerimiento_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Requerimiento ADD CONSTRAINT FK_dbo_Requerimiento_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.EstudioTitulo ADD CONSTRAINT FK_dbo_EstudioTitulo_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.EstudioTitulo ADD CONSTRAINT FK_dbo_EstudioTitulo_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.DevolucionCorreo ADD CONSTRAINT FK_dbo_DevolucionCorreo_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.DevolucionCorreo ADD CONSTRAINT FK_dbo_DevolucionCorreo_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.DevolucionCorreo ADD CONSTRAINT FK_dbo_DevolucionCorreo_CodigoDevolucionId FOREIGN KEY (EstudioId, CodigoDevolucionId) REFERENCES conf.CodigoDevolucion (EstudioId, Id);
ALTER TABLE dbo.DevolucionCorreo ADD CONSTRAINT FK_dbo_DevolucionCorreo_CampaniaId FOREIGN KEY (EstudioId, CampaniaId) REFERENCES conf.Campania (EstudioId, Id);
ALTER TABLE dbo.DevolucionCorreo ADD CONSTRAINT FK_dbo_DevolucionCorreo_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.Documento ADD CONSTRAINT FK_dbo_Documento_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.Documento ADD CONSTRAINT FK_dbo_Documento_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.Documento ADD CONSTRAINT FK_dbo_Documento_EliminadoPor FOREIGN KEY (EliminadoPor) REFERENCES seg.Usuario (Id);
ALTER TABLE conf.PlantillaEscritoVersion ADD CONSTRAINT FK_conf_PlantillaEscritoVersion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE conf.PlantillaEscritoVersion ADD CONSTRAINT FK_conf_PlantillaEscritoVersion_PlantillaEscritoId FOREIGN KEY (EstudioId, PlantillaEscritoId) REFERENCES conf.PlantillaEscrito (EstudioId, Id);
ALTER TABLE conf.PlantillaEscritoVersion ADD CONSTRAINT FK_conf_PlantillaEscritoVersion_DocumentoId FOREIGN KEY (EstudioId, DocumentoId) REFERENCES dbo.Documento (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_PlantillaEscritoVersionId FOREIGN KEY (EstudioId, PlantillaEscritoVersionId) REFERENCES conf.PlantillaEscritoVersion (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_EtapaId FOREIGN KEY (EstudioId, EtapaId) REFERENCES conf.Etapa (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_InstanciaId FOREIGN KEY (EstudioId, InstanciaId) REFERENCES conf.Instancia (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_TrabajoProcesoId FOREIGN KEY (EstudioId, TrabajoProcesoId) REFERENCES seg.TrabajoProceso (EstudioId, Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE dbo.EscritoGenerado ADD CONSTRAINT FK_dbo_EscritoGenerado_DocumentoId FOREIGN KEY (EstudioId, DocumentoId) REFERENCES dbo.Documento (EstudioId, Id);
ALTER TABLE liq.ParametroLiquidacion ADD CONSTRAINT FK_liq_ParametroLiquidacion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.ParametroLiquidacion ADD CONSTRAINT FK_liq_ParametroLiquidacion_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE liq.TasaInteres ADD CONSTRAINT FK_liq_TasaInteres_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.TasaInteres ADD CONSTRAINT FK_liq_TasaInteres_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE liq.EscalaHonorarios ADD CONSTRAINT FK_liq_EscalaHonorarios_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.EscalaHonorarios ADD CONSTRAINT FK_liq_EscalaHonorarios_ComitenteId FOREIGN KEY (EstudioId, ComitenteId) REFERENCES conf.Comitente (EstudioId, Id);
ALTER TABLE liq.RubroPlanilla ADD CONSTRAINT FK_liq_RubroPlanilla_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.RubroPlanilla ADD CONSTRAINT FK_liq_RubroPlanilla_PadreId FOREIGN KEY (EstudioId, PadreId) REFERENCES liq.RubroPlanilla (EstudioId, Id);
ALTER TABLE liq.ItemPlanilla ADD CONSTRAINT FK_liq_ItemPlanilla_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.ItemPlanilla ADD CONSTRAINT FK_liq_ItemPlanilla_RubroPlanillaId FOREIGN KEY (EstudioId, RubroPlanillaId) REFERENCES liq.RubroPlanilla (EstudioId, Id);
ALTER TABLE liq.ItemPlanillaTope ADD CONSTRAINT FK_liq_ItemPlanillaTope_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.ItemPlanillaTope ADD CONSTRAINT FK_liq_ItemPlanillaTope_ItemPlanillaId FOREIGN KEY (EstudioId, ItemPlanillaId) REFERENCES liq.ItemPlanilla (EstudioId, Id);
ALTER TABLE liq.PlanillaJuzgado ADD CONSTRAINT FK_liq_PlanillaJuzgado_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.PlanillaJuzgado ADD CONSTRAINT FK_liq_PlanillaJuzgado_JuzgadoId FOREIGN KEY (EstudioId, JuzgadoId) REFERENCES conf.Juzgado (EstudioId, Id);
ALTER TABLE liq.PlanillaJuzgado ADD CONSTRAINT FK_liq_PlanillaJuzgado_ItemPlanillaId FOREIGN KEY (EstudioId, ItemPlanillaId) REFERENCES liq.ItemPlanilla (EstudioId, Id);
ALTER TABLE liq.Liquidacion ADD CONSTRAINT FK_liq_Liquidacion_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.Liquidacion ADD CONSTRAINT FK_liq_Liquidacion_ExpedienteId FOREIGN KEY (EstudioId, ExpedienteId) REFERENCES dbo.Expediente (EstudioId, Id);
ALTER TABLE liq.Liquidacion ADD CONSTRAINT FK_liq_Liquidacion_JuzgadoId FOREIGN KEY (EstudioId, JuzgadoId) REFERENCES conf.Juzgado (EstudioId, Id);
ALTER TABLE liq.Liquidacion ADD CONSTRAINT FK_liq_Liquidacion_DocumentoId FOREIGN KEY (EstudioId, DocumentoId) REFERENCES dbo.Documento (EstudioId, Id);
ALTER TABLE liq.LiquidacionLinea ADD CONSTRAINT FK_liq_LiquidacionLinea_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE liq.LiquidacionLinea ADD CONSTRAINT FK_liq_LiquidacionLinea_LiquidacionId FOREIGN KEY (EstudioId, LiquidacionId) REFERENCES liq.Liquidacion (EstudioId, Id);
ALTER TABLE liq.LiquidacionLinea ADD CONSTRAINT FK_liq_LiquidacionLinea_RubroPlanillaId FOREIGN KEY (EstudioId, RubroPlanillaId) REFERENCES liq.RubroPlanilla (EstudioId, Id);
ALTER TABLE liq.LiquidacionLinea ADD CONSTRAINT FK_liq_LiquidacionLinea_ItemPlanillaId FOREIGN KEY (EstudioId, ItemPlanillaId) REFERENCES liq.ItemPlanilla (EstudioId, Id);
ALTER TABLE liq.LiquidacionLinea ADD CONSTRAINT FK_liq_LiquidacionLinea_PeriodoAdeudadoId FOREIGN KEY (EstudioId, PeriodoAdeudadoId) REFERENCES dbo.PeriodoAdeudado (EstudioId, Id);
ALTER TABLE aud.AuditoriaEvento ADD CONSTRAINT FK_aud_AuditoriaEvento_EstudioId FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE aud.AuditoriaEvento ADD CONSTRAINT FK_aud_AuditoriaEvento_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES seg.Usuario (Id);
ALTER TABLE mig.DatoARevisar ADD CONSTRAINT FK_mig_DatoARevisar_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);
ALTER TABLE mig.DatoARevisar ADD CONSTRAINT FK_mig_DatoARevisar_ResueltoPor FOREIGN KEY (ResueltoPor) REFERENCES seg.Usuario (Id);
GO

-- ---------------------------------------------------------------- índices
CREATE NONCLUSTERED INDEX IX_seg_Estudio_EstudioOrigenCopiaId ON seg.Estudio (EstudioOrigenCopiaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_seg_Usuario_LegadoId ON seg.Usuario (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_Provincia_LegadoId ON cat.Provincia (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_Localidad_LegadoId ON cat.Localidad (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_TipoDocumento_LegadoId ON cat.TipoDocumento (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_CondicionIva_LegadoId ON cat.CondicionIva (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_cat_Feriado_ProvinciaId ON cat.Feriado (ProvinciaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_Feriado_LegadoId ON cat.Feriado (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_cat_ActividadEconomica_LegadoId ON cat.ActividadEconomica (LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_Comitente_LocalidadId ON conf.Comitente (LocalidadId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Comitente_LegadoId ON conf.Comitente (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_ConceptoDeuda_EstudioId_ComitenteId ON conf.ConceptoDeuda (EstudioId, ComitenteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_ConceptoDeuda_LegadoId ON conf.ConceptoDeuda (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_Profesional_CondicionIvaId ON conf.Profesional (CondicionIvaId);
CREATE NONCLUSTERED INDEX IX_conf_Profesional_LocalidadId ON conf.Profesional (LocalidadId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Profesional_LegadoId ON conf.Profesional (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuario_UsuarioId ON seg.EstudioUsuario (UsuarioId);
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuario_EstudioId_ProfesionalId ON seg.EstudioUsuario (EstudioId, ProfesionalId);
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuario_InvitadoPor ON seg.EstudioUsuario (InvitadoPor);
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuario_AprobadoPor ON seg.EstudioUsuario (AprobadoPor);
CREATE NONCLUSTERED INDEX IX_seg_RolPermiso_PermisoId ON seg.RolPermiso (PermisoId);
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuarioRol_EstudioId_RolId ON seg.EstudioUsuarioRol (EstudioId, RolId);
CREATE NONCLUSTERED INDEX IX_seg_EstudioUsuarioRol_EstudioId_ComitenteId ON seg.EstudioUsuarioRol (EstudioId, ComitenteId);
CREATE NONCLUSTERED INDEX IX_seg_AccesoSoporte_UsuarioId ON seg.AccesoSoporte (UsuarioId);
CREATE NONCLUSTERED INDEX IX_seg_AccesoSoporte_OtorgadoPor ON seg.AccesoSoporte (OtorgadoPor);
CREATE NONCLUSTERED INDEX IX_conf_Juzgado_LocalidadId ON conf.Juzgado (LocalidadId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Juzgado_LegadoId ON conf.Juzgado (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Instancia_LegadoId ON conf.Instancia (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_Etapa_EstudioId_HitoSistema ON conf.Etapa (EstudioId, HitoSistema) WHERE HitoSistema IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_Etapa_EstudioId_InstanciaId ON conf.Etapa (EstudioId, InstanciaId) WHERE InstanciaId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_Etapa_EstudioId_EtapaAnteriorId ON conf.Etapa (EstudioId, EtapaAnteriorId);
CREATE NONCLUSTERED INDEX IX_conf_Etapa_EstudioId_EtapaSiguienteId ON conf.Etapa (EstudioId, EtapaSiguienteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Etapa_LegadoId ON conf.Etapa (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_EtapaUsuarioNotificado_UsuarioId ON conf.EtapaUsuarioNotificado (UsuarioId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_TipoTarea_LegadoId ON conf.TipoTarea (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Ubicacion_LegadoId ON conf.Ubicacion (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_TipoEmbargo_LegadoId ON conf.TipoEmbargo (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_RegistroPublico_LegadoId ON conf.RegistroPublico (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_Infraccion_LegadoId ON conf.Infraccion (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_CodigoDevolucion_LegadoId ON conf.CodigoDevolucion (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_conf_PlantillaEscrito_EstudioId_EtapaId ON conf.PlantillaEscrito (EstudioId, EtapaId);
CREATE NONCLUSTERED INDEX IX_conf_PlantillaEscrito_EstudioId_InstanciaId ON conf.PlantillaEscrito (EstudioId, InstanciaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_conf_PlantillaEscrito_LegadoId ON conf.PlantillaEscrito (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_NroExpedienteJudicial ON dbo.Expediente (EstudioId, NroExpedienteJudicial) WHERE NroExpedienteJudicial IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_NroLiquidacion ON dbo.Expediente (EstudioId, NroLiquidacion);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_NroCuenta ON dbo.Expediente (EstudioId, NroCuenta);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_Dominio ON dbo.Expediente (EstudioId, Dominio) WHERE Dominio IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_Nomenclatura ON dbo.Expediente (EstudioId, Nomenclatura);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_InstanciaActualId_FechaUltimoMovimiento ON dbo.Expediente (EstudioId, InstanciaActualId, FechaUltimoMovimiento) INCLUDE (NroOrden, Caratula) WHERE Anulado = 0;
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_ProcuradorId ON dbo.Expediente (EstudioId, ProcuradorId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_JuzgadoId ON dbo.Expediente (EstudioId, JuzgadoId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_ComitenteId ON dbo.Expediente (EstudioId, ComitenteId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_ConceptoDeudaId ON dbo.Expediente (EstudioId, ConceptoDeudaId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_LocalidadObjetoId ON dbo.Expediente (LocalidadObjetoId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_RegistroPublicoId ON dbo.Expediente (EstudioId, RegistroPublicoId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_PatrocinanteId ON dbo.Expediente (EstudioId, PatrocinanteId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_AbogadoInteriorId ON dbo.Expediente (EstudioId, AbogadoInteriorId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_NegociadorUsuarioId ON dbo.Expediente (NegociadorUsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_DepuradorUsuarioId ON dbo.Expediente (DepuradorUsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_RegionId ON dbo.Expediente (EstudioId, RegionId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_CampaniaId ON dbo.Expediente (EstudioId, CampaniaId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_EtapaActualId ON dbo.Expediente (EstudioId, EtapaActualId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_CautelarActualId ON dbo.Expediente (EstudioId, CautelarActualId);
CREATE NONCLUSTERED INDEX IX_dbo_Expediente_EstudioId_UbicacionActualId ON dbo.Expediente (EstudioId, UbicacionActualId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Expediente_LegadoId ON dbo.Expediente (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_ExpedienteActa_EstudioId_NroCaso ON dbo.ExpedienteActa (EstudioId, NroCaso) WHERE NroCaso IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_ExpedienteActa_EstudioId_NroTramite ON dbo.ExpedienteActa (EstudioId, NroTramite) WHERE NroTramite IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_ExpedienteInfraccion_EstudioId_ExpedienteId ON dbo.ExpedienteInfraccion (EstudioId, ExpedienteId);
CREATE NONCLUSTERED INDEX IX_dbo_ExpedienteInfraccion_EstudioId_InfraccionId ON dbo.ExpedienteInfraccion (EstudioId, InfraccionId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_ExpedienteInfraccion_LegadoId ON dbo.ExpedienteInfraccion (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_ExpedienteActividad_ActividadEconomicaId ON dbo.ExpedienteActividad (ActividadEconomicaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_ExpedienteActividad_LegadoId ON dbo.ExpedienteActividad (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_PeriodoAdeudado_EstudioId_ExpedienteId_Anio ON dbo.PeriodoAdeudado (EstudioId, ExpedienteId, Anio);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_PeriodoAdeudado_LegadoId ON dbo.PeriodoAdeudado (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Persona_EstudioId_NroDocumento ON dbo.Persona (EstudioId, NroDocumento);
CREATE NONCLUSTERED INDEX IX_dbo_Persona_EstudioId_Cuit ON dbo.Persona (EstudioId, Cuit) WHERE Cuit IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Persona_TipoDocumentoId ON dbo.Persona (TipoDocumentoId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Persona_LegadoId ON dbo.Persona (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Domicilio_EstudioId_PersonaId ON dbo.Domicilio (EstudioId, PersonaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Domicilio_EstudioId_PersonaId_Principal ON dbo.Domicilio (EstudioId, PersonaId) WHERE Principal = 1;
CREATE NONCLUSTERED INDEX IX_dbo_Domicilio_LocalidadId ON dbo.Domicilio (LocalidadId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Domicilio_LegadoId ON dbo.Domicilio (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_ExpedientePersona_EstudioId_PersonaId ON dbo.ExpedientePersona (EstudioId, PersonaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_ExpedientePersona_LegadoId ON dbo.ExpedientePersona (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_InformePersona_EstudioId_PersonaId ON dbo.InformePersona (EstudioId, PersonaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_InformePersona_LegadoId ON dbo.InformePersona (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_EtapaExpediente_EstudioId_ExpedienteId_FechaInicio ON dbo.EtapaExpediente (EstudioId, ExpedienteId, FechaInicio);
CREATE NONCLUSTERED INDEX IX_dbo_EtapaExpediente_EstudioId_Estado_FechaVencimiento ON dbo.EtapaExpediente (EstudioId, Estado, FechaVencimiento) INCLUDE (ExpedienteId) WHERE Estado IN (1,3);
CREATE NONCLUSTERED INDEX IX_dbo_EtapaExpediente_EstudioId_EtapaId ON dbo.EtapaExpediente (EstudioId, EtapaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_EtapaExpediente_LegadoId ON dbo.EtapaExpediente (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_InstanciaExpediente_EstudioId_ExpedienteId_Fecha ON dbo.InstanciaExpediente (EstudioId, ExpedienteId, Fecha);
CREATE NONCLUSTERED INDEX IX_dbo_InstanciaExpediente_EstudioId_InstanciaId ON dbo.InstanciaExpediente (EstudioId, InstanciaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_InstanciaExpediente_LegadoId ON dbo.InstanciaExpediente (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_UbicacionExpediente_EstudioId_ExpedienteId_Fecha ON dbo.UbicacionExpediente (EstudioId, ExpedienteId, Fecha);
CREATE NONCLUSTERED INDEX IX_dbo_UbicacionExpediente_EstudioId_UbicacionId ON dbo.UbicacionExpediente (EstudioId, UbicacionId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_UbicacionExpediente_LegadoId ON dbo.UbicacionExpediente (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Tarea_EstudioId_Estado_FechaVencimiento ON dbo.Tarea (EstudioId, Estado, FechaVencimiento) INCLUDE (ResponsableUsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_Tarea_EstudioId_ExpedienteId ON dbo.Tarea (EstudioId, ExpedienteId) WHERE ExpedienteId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Tarea_EstudioId_TipoTareaId ON dbo.Tarea (EstudioId, TipoTareaId);
CREATE NONCLUSTERED INDEX IX_dbo_Tarea_ResponsableUsuarioId ON dbo.Tarea (ResponsableUsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_Tarea_EstudioId_EtapaExpedienteId ON dbo.Tarea (EstudioId, EtapaExpedienteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Tarea_LegadoId ON dbo.Tarea (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_TareaParticipante_UsuarioId ON dbo.TareaParticipante (UsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_NovedadJudicial_EstudioId_Leida_Fecha ON dbo.NovedadJudicial (EstudioId, Leida, Fecha) INCLUDE (ExpedienteId);
CREATE NONCLUSTERED INDEX IX_dbo_NovedadJudicial_EstudioId_ExpedienteId ON dbo.NovedadJudicial (EstudioId, ExpedienteId);
CREATE NONCLUSTERED INDEX IX_dbo_NovedadJudicial_LeidaPor ON dbo.NovedadJudicial (LeidaPor);
CREATE NONCLUSTERED INDEX IX_dbo_NovedadJudicial_EstudioId_TareaId ON dbo.NovedadJudicial (EstudioId, TareaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_NovedadJudicial_LegadoId ON dbo.NovedadJudicial (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Embargo_EstudioId_ExpedienteId ON dbo.Embargo (EstudioId, ExpedienteId);
CREATE NONCLUSTERED INDEX IX_dbo_Embargo_EstudioId_Dominio ON dbo.Embargo (EstudioId, Dominio) WHERE Dominio IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Embargo_EstudioId_TipoEmbargoId ON dbo.Embargo (EstudioId, TipoEmbargoId);
CREATE NONCLUSTERED INDEX IX_dbo_Embargo_EstudioId_RegistroPublicoId ON dbo.Embargo (EstudioId, RegistroPublicoId);
CREATE NONCLUSTERED INDEX IX_dbo_Embargo_EstudioId_MartilleroId ON dbo.Embargo (EstudioId, MartilleroId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Embargo_LegadoId ON dbo.Embargo (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_PlanPago_EstudioId_ExpedienteId ON dbo.PlanPago (EstudioId, ExpedienteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_PlanPago_LegadoId ON dbo.PlanPago (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Requerimiento_EstudioId_ExpedienteId ON dbo.Requerimiento (EstudioId, ExpedienteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_Requerimiento_LegadoId ON dbo.Requerimiento (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_EstudioTitulo_EstudioId_ExpedienteId ON dbo.EstudioTitulo (EstudioId, ExpedienteId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_EstudioTitulo_LegadoId ON dbo.EstudioTitulo (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_DevolucionCorreo_EstudioId_ExpedienteId ON dbo.DevolucionCorreo (EstudioId, ExpedienteId);
CREATE NONCLUSTERED INDEX IX_dbo_DevolucionCorreo_EstudioId_CodigoDevolucionId ON dbo.DevolucionCorreo (EstudioId, CodigoDevolucionId);
CREATE NONCLUSTERED INDEX IX_dbo_DevolucionCorreo_EstudioId_CampaniaId ON dbo.DevolucionCorreo (EstudioId, CampaniaId);
CREATE NONCLUSTERED INDEX IX_dbo_DevolucionCorreo_UsuarioId ON dbo.DevolucionCorreo (UsuarioId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_DevolucionCorreo_LegadoId ON dbo.DevolucionCorreo (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_dbo_Documento_EstudioId_ExpedienteId ON dbo.Documento (EstudioId, ExpedienteId) WHERE Eliminado = 0;
CREATE NONCLUSTERED INDEX IX_dbo_Documento_EliminadoPor ON dbo.Documento (EliminadoPor);
CREATE NONCLUSTERED INDEX IX_conf_PlantillaEscritoVersion_EstudioId_DocumentoId ON conf.PlantillaEscritoVersion (EstudioId, DocumentoId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_ExpedienteId_FechaUtc ON dbo.EscritoGenerado (EstudioId, ExpedienteId, FechaUtc);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_PlantillaEscritoVersionId ON dbo.EscritoGenerado (EstudioId, PlantillaEscritoVersionId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_EtapaId ON dbo.EscritoGenerado (EstudioId, EtapaId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_InstanciaId ON dbo.EscritoGenerado (EstudioId, InstanciaId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_TrabajoProcesoId ON dbo.EscritoGenerado (EstudioId, TrabajoProcesoId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_UsuarioId ON dbo.EscritoGenerado (UsuarioId);
CREATE NONCLUSTERED INDEX IX_dbo_EscritoGenerado_EstudioId_DocumentoId ON dbo.EscritoGenerado (EstudioId, DocumentoId);
CREATE UNIQUE NONCLUSTERED INDEX UX_dbo_EscritoGenerado_LegadoId ON dbo.EscritoGenerado (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_liq_ParametroLiquidacion_EstudioId_ComitenteId ON liq.ParametroLiquidacion (EstudioId, ComitenteId);
CREATE NONCLUSTERED INDEX IX_liq_TasaInteres_EstudioId_ComitenteId ON liq.TasaInteres (EstudioId, ComitenteId);
CREATE NONCLUSTERED INDEX IX_liq_RubroPlanilla_EstudioId_PadreId ON liq.RubroPlanilla (EstudioId, PadreId);
CREATE UNIQUE NONCLUSTERED INDEX UX_liq_RubroPlanilla_LegadoId ON liq.RubroPlanilla (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_liq_ItemPlanilla_EstudioId_RubroPlanillaId ON liq.ItemPlanilla (EstudioId, RubroPlanillaId);
CREATE UNIQUE NONCLUSTERED INDEX UX_liq_ItemPlanilla_LegadoId ON liq.ItemPlanilla (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_liq_ItemPlanillaTope_LegadoId ON liq.ItemPlanillaTope (EstudioId, LegadoId) WHERE LegadoId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_liq_PlanillaJuzgado_EstudioId_ItemPlanillaId ON liq.PlanillaJuzgado (EstudioId, ItemPlanillaId);
CREATE NONCLUSTERED INDEX IX_liq_Liquidacion_EstudioId_JuzgadoId ON liq.Liquidacion (EstudioId, JuzgadoId);
CREATE NONCLUSTERED INDEX IX_liq_Liquidacion_EstudioId_DocumentoId ON liq.Liquidacion (EstudioId, DocumentoId);
CREATE NONCLUSTERED INDEX IX_liq_LiquidacionLinea_EstudioId_RubroPlanillaId ON liq.LiquidacionLinea (EstudioId, RubroPlanillaId);
CREATE NONCLUSTERED INDEX IX_liq_LiquidacionLinea_EstudioId_ItemPlanillaId ON liq.LiquidacionLinea (EstudioId, ItemPlanillaId);
CREATE NONCLUSTERED INDEX IX_liq_LiquidacionLinea_EstudioId_PeriodoAdeudadoId ON liq.LiquidacionLinea (EstudioId, PeriodoAdeudadoId);
CREATE NONCLUSTERED INDEX IX_aud_AuditoriaEvento_EstudioId_FechaUtc ON aud.AuditoriaEvento (EstudioId, FechaUtc);
CREATE NONCLUSTERED INDEX IX_aud_AuditoriaEvento_EstudioId_Entidad_EntidadId ON aud.AuditoriaEvento (EstudioId, Entidad, EntidadId);
CREATE NONCLUSTERED INDEX IX_aud_AuditoriaEvento_EstudioId_UsuarioId_FechaUtc ON aud.AuditoriaEvento (EstudioId, UsuarioId, FechaUtc);
CREATE NONCLUSTERED INDEX IX_aud_AuditoriaEvento_UsuarioId ON aud.AuditoriaEvento (UsuarioId);
CREATE NONCLUSTERED INDEX IX_mig_DatoARevisar_EstudioId_Estado_Tipo ON mig.DatoARevisar (EstudioId, Estado, Tipo);
CREATE NONCLUSTERED INDEX IX_mig_DatoARevisar_ResueltoPor ON mig.DatoARevisar (ResueltoPor);
GO

-- ---------------------------------------------------------------- roles de base de datos
IF DATABASE_PRINCIPAL_ID(N'rol_aplicacion') IS NULL CREATE ROLE rol_aplicacion;
IF DATABASE_PRINCIPAL_ID(N'rol_plataforma') IS NULL CREATE ROLE rol_plataforma;
GO
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo  TO rol_aplicacion;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::conf TO rol_aplicacion;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::liq  TO rol_aplicacion;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::seg  TO rol_aplicacion;
GRANT SELECT, INSERT, UPDATE         ON SCHEMA::mig  TO rol_aplicacion;
GRANT SELECT                         ON SCHEMA::cat  TO rol_aplicacion;
GRANT SELECT, INSERT                 ON SCHEMA::aud  TO rol_aplicacion;
DENY  UPDATE, DELETE                 ON SCHEMA::aud  TO rol_aplicacion;   -- auditoría: solo inserción
GRANT EXECUTE ON SCHEMA::seg TO rol_aplicacion;
GO

-- ---------------------------------------------------------------- numeración por Estudio
CREATE OR ALTER PROCEDURE seg.usp_SiguienteNumero
    @EstudioId int, @Nombre nvarchar(50), @Valor bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE seg.SecuenciaEstudio WITH (UPDLOCK, HOLDLOCK)
       SET @Valor = UltimoValor = UltimoValor + 1
     WHERE EstudioId = @EstudioId AND Nombre = @Nombre;
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT seg.SecuenciaEstudio (EstudioId, Nombre, UltimoValor) VALUES (@EstudioId, @Nombre, 1);
        SET @Valor = 1;
    END
END
GO

-- ---------------------------------------------------------------- retención de auditoría (job diario, ejecuta rol_plataforma)
CREATE OR ALTER PROCEDURE aud.usp_PurgarAuditoria
    @RetencionPlataformaDias int = 730, @Lote int = 5000
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @total int = 0, @n int = 1;
    WHILE @n > 0
    BEGIN
        DELETE TOP (@Lote) e
          FROM aud.AuditoriaEvento e
          LEFT JOIN seg.Estudio s ON s.Id = e.EstudioId
         WHERE e.FechaUtc < DATEADD(day, -ISNULL(s.RetencionAuditoriaDias, @RetencionPlataformaDias), SYSUTCDATETIME());
        SET @n = @@ROWCOUNT; SET @total += @n;
    END
    INSERT aud.AuditoriaEvento (EstudioId, UsuarioId, FechaUtc, Modulo, Entidad, Accion, Resultado, CambiosJson)
    VALUES (NULL, NULL, SYSUTCDATETIME(), N'plataforma', N'AuditoriaEvento', 4, 1,
            CONCAT(N'{"eventosEliminados":{"antes":null,"despues":', @total, N'}}'));
END
GO
GRANT EXECUTE ON aud.usp_PurgarAuditoria TO rol_plataforma;
GO

-- Búsqueda por texto (opcional, requiere Full-Text Search instalado):
-- CREATE FULLTEXT CATALOG ftProcu360;
-- CREATE FULLTEXT INDEX ON dbo.Expediente (Caratula, Recaratulacion) KEY INDEX PK_dbo_Expediente ON ftProcu360;
-- CREATE FULLTEXT INDEX ON dbo.Persona (Apellido, Nombre, RazonSocial) KEY INDEX PK_dbo_Persona ON ftProcu360;


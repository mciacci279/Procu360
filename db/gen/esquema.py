"""Procu360 - definición única del modelo de datos.

De esta especificación se generan:
  * el DDL de SQL Server (01-esquema.sql),
  * la política de Row-Level Security (03-seguridad-rls.sql),
  * una copia SQLite que se usa solo para validar los datos de prueba.

Convención de columnas:  "Nombre tipo [NN] [DEF:expresion]"
  NN  = NOT NULL
  DEF = valor por defecto (expresión T-SQL)
"""
import re

TABLAS = []          # en orden de dependencias
POR_NOMBRE = {}


class Tabla:
    def __init__(self, nombre, cols, *, tenant=True, base=True, anulable=False,
                 legado=False, pk="Id", identidad="bigint", fks=(), unicos=(),
                 indices=(), checks=(), desc="", origen=""):
        self.schema, self.nombre = nombre.split(".")
        self.full = nombre
        self.tenant = tenant
        self.desc = desc
        self.origen = origen
        self.cols = []
        self.pk = [pk] if isinstance(pk, str) else list(pk)
        self.identidad = identidad if self.pk == ["Id"] else None
        if self.identidad:
            self.cols.append(("Id", self.identidad + " IDENTITY(1,1)", False, None))
        if tenant:
            self.cols.append(("EstudioId", "int", False, None))
        for c in cols:
            self.cols.append(_parse(c))
        if legado:
            self.cols.append(("LegadoId", "uniqueidentifier", True, None))
        if anulable:
            self.cols += [("Anulado", "bit", False, "0"),
                          ("AnuladoUtc", "datetime2(3)", True, None),
                          ("AnuladoPor", "int", True, None),
                          ("MotivoAnulacion", "nvarchar(500)", True, None)]
        if base:
            self.cols += [("CreadoUtc", "datetime2(3)", False, "SYSUTCDATETIME()"),
                          ("CreadoPor", "int", True, None),
                          ("ModificadoUtc", "datetime2(3)", True, None),
                          ("ModificadoPor", "int", True, None),
                          ("Version", "rowversion", False, None)]
        self.legado = legado
        self.fks = list(fks)          # (col o tupla de cols, "esquema.Tabla" [, "cols destino"])
        self.unicos = list(unicos)    # tuplas de columnas (EstudioId se agrega solo si tenant)
        self.indices = list(indices)  # (cols, include, filtro)
        self.checks = list(checks)    # (expr, solo_tsql)
        TABLAS.append(self)
        POR_NOMBRE[nombre] = self

    def colnames(self, insertables=False):
        return [c[0] for c in self.cols
                if not (insertables and c[1] == "rowversion")]

    def col(self, n):
        return next(c for c in self.cols if c[0] == n)


def _parse(s):
    m = re.match(r"(\w+)\s+([\w]+(?:\([\d, max]+\))?)(.*)$", s.strip())
    if not m:
        raise ValueError(s)
    nombre, tipo, resto = m.groups()
    nn = " NN" in " " + resto
    dm = re.search(r"DEF:(\S+)", resto)
    return (nombre, tipo, not nn, dm.group(1) if dm else None)


T = Tabla
NOMBRE = "nvarchar(150) NN"

# ---------------------------------------------------------------- seg
T("seg.Estudio", [
    "Nombre nvarchar(200) NN", "Cuit nvarchar(13)", "Estado tinyint NN DEF:1",
    "ZonaHoraria nvarchar(50) NN DEF:N'America/Argentina/Cordoba'",
    "RetencionAuditoriaDias int NN DEF:365",
    "EstudioOrigenCopiaId int", "CopiaCatalogosJson nvarchar(max)",
], tenant=False, identidad="int",
  fks=[("EstudioOrigenCopiaId", "seg.Estudio")],
  unicos=[("Nombre",)],
  checks=[("Estado IN (1,2)", False), ("RetencionAuditoriaDias BETWEEN 30 AND 3650", False)],
  desc="Raíz del tenant. Estado 1 activo, 2 suspendido. CopiaCatalogosJson registra qué catálogos se copiaron del Estudio origen al crearlo.")

T("seg.Usuario", [
    "Email nvarchar(256) NN", "EmailNormalizado nvarchar(256) NN", "Nombre nvarchar(100) NN",
    "Apellido nvarchar(100) NN", "EsSuperAdmin bit NN DEF:0", "Activo bit NN DEF:1",
    "MfaHabilitado bit NN DEF:0", "PasswordHash nvarchar(max)", "SecurityStamp nvarchar(100)",
    "BloqueadoHastaUtc datetimeoffset(3)", "AccesosFallidos int NN DEF:0", "UltimoAccesoUtc datetime2(3)",
], tenant=False, identidad="int", legado=True,
  unicos=[("EmailNormalizado",)],
  desc="Identidad global (IdentityUser<int> de ASP.NET Core Identity con columnas propias). Sin EstudioId. Las contraseñas del legado NO se migran: PasswordHash queda NULL hasta que el usuario acepta la invitación.",
  origen="Usuario (sin Pass)")

T("cat.Provincia", ["Nombre nvarchar(60) NN", "Codigo nvarchar(10)"],
  tenant=False, base=False, identidad="int", legado=True, unicos=[("Nombre",)], origen="Provincia")
T("cat.Localidad", ["ProvinciaId int NN", "Nombre nvarchar(150) NN", "CodigoPostal nvarchar(10)"],
  tenant=False, base=False, identidad="int", legado=True, fks=[("ProvinciaId", "cat.Provincia")],
  unicos=[("ProvinciaId", "Nombre")], origen="Localidad")
T("cat.TipoDocumento", ["Nombre nvarchar(30) NN", "Codigo nvarchar(10) NN"],
  tenant=False, base=False, identidad="int", legado=True, unicos=[("Codigo",)], origen="TipoDoc")
T("cat.CondicionIva", [
    "Nombre nvarchar(60) NN", "Porcentaje decimal(5,2) NN DEF:0", "LetraComprobante char(1)",
    "EsResponsableInscripto bit NN DEF:0", "EsNoResponsable bit NN DEF:0", "EsExento bit NN DEF:0",
    "EsMonotributo bit NN DEF:0", "EsConsumidorFinal bit NN DEF:0"],
  tenant=False, base=False, identidad="int", legado=True, unicos=[("Nombre",)], origen="Iva")
T("cat.Feriado", ["Fecha date NN", "Descripcion nvarchar(100) NN", "Ambito tinyint NN", "ProvinciaId int"],
  tenant=False, base=False, identidad="int", legado=True, fks=[("ProvinciaId", "cat.Provincia")],
  unicos=[("Fecha", "Ambito", "ProvinciaId")], checks=[("Ambito IN (1,2,3)", False)],
  desc="Ámbito 1 nacional, 2 provincial, 3 judicial (receso).", origen="Feriado")
T("cat.IndiceTasaPasiva", ["Fecha date NN", "Coeficiente decimal(12,6) NN"],
  tenant=False, base=False, pk="Fecha", origen="AlicuotaPlanilla",
  desc="Coeficiente acumulado diario de tasa pasiva (BCRA). Lo mantiene el superadministrador.")
T("cat.ActividadEconomica", ["Codigo nvarchar(20) NN", "Nombre nvarchar(500) NN"],
  tenant=False, base=False, identidad="int", legado=True, unicos=[("Codigo",)], origen="Actividad",
  desc="Nomenclador público de actividades (IIBB).")

# ---------------------------------------------------------------- conf (por Estudio)
T("conf.Comitente", [
    "Nombre nvarchar(200) NN", "Cuit nvarchar(13)", "Domicilio nvarchar(250)", "CodigoPostal nvarchar(10)",
    "LocalidadId int", "Observaciones nvarchar(2000)", "Activo bit NN DEF:1"],
  legado=True, identidad="int", fks=[("LocalidadId", "cat.Localidad")], unicos=[("Nombre",)],
  origen="DomicilioDGR", desc="Acreedor/organismo para el que se procura (municipio, DGR, ente). Las fórmulas de liquidación se parametrizan por comitente.")
T("conf.ConceptoDeuda", [
    "ComitenteId int", "Nombre nvarchar(150) NN", "Item nvarchar(50)", "PeriodosDiferenciados int",
    "ConsideraPeriodos bit NN DEF:1", "PeriodosFormatoDGR bit NN DEF:0", "TextoEscrito nvarchar(4000)",
    "Activo bit NN DEF:1"],
  legado=True, identidad="int", fks=[("ComitenteId", "conf.Comitente")], unicos=[("Nombre",)],
  origen="Impuesto", desc="Impuesto/tasa reclamada. TextoEscrito admite variables {{...}}.")
T("conf.Profesional", [
    "Nombre nvarchar(250) NN", "Cuit nvarchar(13)", "Matricula nvarchar(50)", "CondicionIvaId int",
    "Calle nvarchar(100)", "Numero nvarchar(20)", "Barrio nvarchar(100)", "LocalidadId int",
    "Carpeta nvarchar(50)", "Descripcion nvarchar(250)",
    "EsProcurador bit NN DEF:0", "EsPatrocinante bit NN DEF:0", "EsApoderado bit NN DEF:0",
    "EsAbogadoInterior bit NN DEF:0", "EsMartillero bit NN DEF:0",
    "TituloApoderado nvarchar(250)", "TextoEscrito nvarchar(4000)", "Observaciones nvarchar(4000)",
    "Activo bit NN DEF:1"],
  legado=True, identidad="int",
  fks=[("CondicionIvaId", "cat.CondicionIva"), ("LocalidadId", "cat.Localidad")],
  origen="Procurador (sin UsuarioGant/PasswordGant)",
  desc="Procuradores, patrocinantes, apoderados, abogados de interior y martilleros. Reemplaza al ProcuradorID que el legado usaba como tenant parcial.")

T("seg.EstudioUsuario", [
    "UsuarioId int NN", "Estado tinyint NN", "ProfesionalId int", "InvitadoPor int",
    "InvitadoUtc datetime2(3)", "AprobadoPor int", "AprobadoUtc datetime2(3)",
    "MotivoRechazo nvarchar(500)", "UltimoAccesoUtc datetime2(3)"],
  identidad="int", fks=[("UsuarioId", "seg.Usuario"), ("ProfesionalId", "conf.Profesional"),
                        ("InvitadoPor", "seg.Usuario"), ("AprobadoPor", "seg.Usuario")],
  unicos=[("UsuarioId",)],
  checks=[("Estado IN (1,2,3,4,5)", False)],
  desc="Membresía. Estado 1 invitado, 2 pendiente de aprobación (el email ya pertenece a otro Estudio), 3 activo, 4 desactivado, 5 rechazado.")
T("seg.Rol", ["Nombre nvarchar(100) NN", "Descripcion nvarchar(500)", "Ambito tinyint NN DEF:1",
              "EsSistema bit NN DEF:0"],
  identidad="int", unicos=[("Nombre",)], checks=[("Ambito IN (1,2,3)", False)],
  origen="Nueva (Perfil de Monitorio se descarta)", desc="Ámbito 1 todo el Estudio, 2 por comitente, 3 expedientes asignados.")
T("seg.Permiso", ["Codigo nvarchar(80) NN", "Modulo nvarchar(40) NN", "Accion nvarchar(20) NN",
                  "Descripcion nvarchar(200)"],
  tenant=False, base=False, identidad="int", unicos=[("Codigo",)],
  desc="Global, definido por el código de la aplicación (modulo.accion). Reemplaza el esquema de menús del WinForms (MenuSistema/MenuPerfil), que se descarta.")
T("seg.RolPermiso", ["RolId int NN", "PermisoId int NN"], pk=("EstudioId", "RolId", "PermisoId"),
  base=False, fks=[("RolId", "seg.Rol"), ("PermisoId", "seg.Permiso")])
T("seg.EstudioUsuarioRol", ["EstudioUsuarioId int NN", "RolId int NN", "ComitenteId int"],
  identidad="int", base=False,
  fks=[("EstudioUsuarioId", "seg.EstudioUsuario"), ("RolId", "seg.Rol"), ("ComitenteId", "conf.Comitente")],
  unicos=[("EstudioUsuarioId", "RolId", "ComitenteId")])
T("seg.SecuenciaEstudio", ["Nombre nvarchar(50) NN", "UltimoValor bigint NN"],
  pk=("EstudioId", "Nombre"), base=False, origen="Secuencia",
  desc="Numeración por Estudio (NroOrden). Se incrementa con UPDLOCK, HOLDLOCK dentro de la transacción de alta.")
T("seg.AccesoSoporte", ["UsuarioId int NN", "Motivo nvarchar(500) NN", "DesdeUtc datetime2(3) NN",
                        "HastaUtc datetime2(3) NN", "OtorgadoPor int NN"],
  identidad="int", fks=[("UsuarioId", "seg.Usuario"), ("OtorgadoPor", "seg.Usuario")],
  checks=[("HastaUtc > DesdeUtc", False)],
  desc="Acceso temporal y auditado de un superadministrador a los datos de un Estudio.")
T("seg.TrabajoProceso", [
    "Tipo tinyint NN", "Estado tinyint NN", "Total int NN DEF:0", "Procesados int NN DEF:0",
    "Errores int NN DEF:0", "ParametrosJson nvarchar(max)", "ResultadoJson nvarchar(max)",
    "IniciadoUtc datetime2(3)", "FinalizadoUtc datetime2(3)"],
  checks=[("Tipo BETWEEN 1 AND 6", False), ("Estado BETWEEN 1 AND 5", False),
          ("ParametrosJson IS NULL OR ISJSON(ParametrosJson)=1", True)],
  desc="Procesos en segundo plano. Tipo 1 escritos masivos, 2 carga masiva, 3 modificación masiva, 4 importar novedades, 5 liquidación masiva, 6 purga de auditoría. Estado 1 en cola, 2 en curso, 3 finalizado, 4 finalizado con errores, 5 cancelado.")

T("conf.ConfiguracionEstudio", ["Clave nvarchar(50) NN", "Valor nvarchar(1000)", "Tipo tinyint NN"],
  pk=("EstudioId", "Clave"), origen="Configuracion",
  checks=[("Tipo BETWEEN 1 AND 5", False)],
  desc="Parámetros generales no modelados como columna. Tipo 1 texto, 2 entero, 3 decimal, 4 fecha, 5 booleano.")
T("conf.FeriadoEstudio", ["Fecha date NN", "Descripcion nvarchar(100) NN"], identidad="int",
  unicos=[("Fecha",)], desc="Asuetos propios del Estudio o de su circunscripción.")
T("conf.Juzgado", [
    "Nombre nvarchar(150) NN", "Descripcion nvarchar(100)", "Secretaria nvarchar(150)",
    "Juez nvarchar(150)", "Ubicacion nvarchar(150)", "Telefono nvarchar(50)", "EsCapital bit NN DEF:0",
    "LocalidadId int", "DomicilioParaEscritos nvarchar(500)", "Orden int", "Activo bit NN DEF:1"],
  legado=True, identidad="int", fks=[("LocalidadId", "cat.Localidad")],
  unicos=[("Nombre", "Secretaria")], origen="Juzgado (sin ItaxID ni MuniCordobaID)",
  desc="Organización judicial por Estudio; al crear un Estudio puede copiarse de otro.")
T("conf.Instancia", ["Nombre nvarchar(50) NN", "Color char(7)", "Perime bit NN DEF:0",
                     "EsBaja bit NN DEF:0", "Orden int", "Activa bit NN DEF:1"],
  legado=True, identidad="int", unicos=[("Nombre",)], origen="Instancia",
  desc="Macroestado del expediente (pre judicial, judicial, con sentencia, archivado…).")
T("conf.Etapa", [
    "Flujo tinyint NN", "Codigo nvarchar(20)", "Nombre nvarchar(100) NN", "Orden int",
    "Dias int", "DiasHabiles bit NN DEF:1", "Interrumpe bit NN DEF:0", "CortaPerencion bit NN DEF:0",
    "EsMonitorio bit NN DEF:0", "EsEjecucion bit NN DEF:0", "EsDiligencia bit NN DEF:0",
    "EtapaAnteriorId int", "EtapaSiguienteId int", "PasaSiguienteAutomatica bit NN DEF:0",
    "GeneraTarea bit NN DEF:0", "TareaParaUsuarios tinyint NN DEF:0", "HitoSistema tinyint",
    "InstanciaId int", "Activa bit NN DEF:1"],
  legado=True, identidad="int",
  fks=[("EtapaAnteriorId", "conf.Etapa"), ("EtapaSiguienteId", "conf.Etapa"),
       ("InstanciaId", "conf.Instancia")],
  unicos=[("Flujo", "Nombre")],
  indices=[(("EstudioId", "HitoSistema"), (), "HitoSistema IS NOT NULL"),
           (("EstudioId", "InstanciaId"), (), "InstanciaId IS NOT NULL")],
  checks=[("Flujo IN (1,2)", False), ("TareaParaUsuarios BETWEEN 0 AND 3", False),
          ("HitoSistema BETWEEN 1 AND 4", False),
          # El flujo cautelar (embargos, inhibiciones) no mueve el macroestado del expediente.
          ("Flujo = 1 OR InstanciaId IS NULL", False)],
  origen="Etapa + Cautelar",
  desc="Flujo 1 principal, 2 cautelar. HitoSistema 1 inicio de demanda, 2 demanda presentada (reemplaza el código fijo M100 y Configuracion.DemPresID), 3 sentencia, 4 ejecución. TareaParaUsuarios 0 ninguno, 1 responsable, 2 usuarios seleccionados, 3 todos. InstanciaId: la instancia a la que pasa el expediente al registrar esta etapa; solo para el flujo principal.")
T("conf.EtapaUsuarioNotificado", ["EtapaId int NN", "UsuarioId int NN"],
  pk=("EstudioId", "EtapaId", "UsuarioId"), base=False,
  fks=[("EtapaId", "conf.Etapa"), ("UsuarioId", "seg.Usuario")], origen="EtapaAgendaUsuario")
T("conf.TipoTarea", ["Nombre nvarchar(100) NN", "Dias int", "DiasHabiles bit NN DEF:1", "Activo bit NN DEF:1"],
  legado=True, identidad="int", unicos=[("Nombre",)], origen="ActividadAdministrativa")
T("conf.Ubicacion", ["Nombre nvarchar(100) NN", "Activa bit NN DEF:1"], legado=True, identidad="int",
  unicos=[("Nombre",)], origen="Lugar", desc="Ubicación física del expediente.")
T("conf.Region", ["Nombre nvarchar(100) NN"], identidad="int", unicos=[("Nombre",)],
  origen="Expediente.Region (valores distintos)")
T("conf.Campania", ["Numero nvarchar(50) NN", "Nombre nvarchar(100)"], identidad="int",
  unicos=[("Numero",)], origen="Expediente.NroCampania/NombreCampania, ExpedienteCodigoDevolucion.Campania")
T("conf.TipoEmbargo", ["Nombre nvarchar(100) NN", "Clase tinyint NN", "Observaciones nvarchar(500)"],
  legado=True, identidad="int", unicos=[("Nombre",)], checks=[("Clase BETWEEN 1 AND 6", False)],
  origen="TipoEmbargo",
  desc="Clase 1 inmueble, 2 automotor, 3 embargo de cuentas corrientes, 4 inhibición, 5 intervención de caja, 6 reserva de cautelar. La clase define el esquema de DetalleJson del embargo.")
T("conf.RegistroPublico", ["Nombre nvarchar(250) NN", "Domicilio nvarchar(500)", "Zona nvarchar(250)"],
  legado=True, identidad="int", origen="Registro")
T("conf.Infraccion", ["Codigo nvarchar(50)", "Nombre nvarchar(200) NN", "Descripcion nvarchar(400)"],
  legado=True, identidad="int", origen="Infraccion")
T("conf.CodigoDevolucion", ["Codigo nvarchar(20) NN", "Descripcion nvarchar(250) NN"],
  legado=True, identidad="int", unicos=[("Codigo",)], origen="CodigoDevolucion")
T("conf.PlantillaEscrito", ["Nombre nvarchar(150) NN", "Tipo tinyint NN", "EtapaId int", "InstanciaId int",
                            "EsUnico bit NN DEF:0", "Activa bit NN DEF:1"],
  legado=True, identidad="int", fks=[("EtapaId", "conf.Etapa"), ("InstanciaId", "conf.Instancia")],
  unicos=[("Nombre",)], checks=[("Tipo BETWEEN 1 AND 4", False)],
  origen="EtapaEscrito, Instancia.Path/EscritoNombre, Cautelar.Path/EscritoNombre",
  desc="Tipo 1 escrito, 2 cédula, 3 carta, 4 oficio. EsUnico: se genera una sola vez por expediente.")

# ---------------------------------------------------------------- dbo (núcleo)
T("dbo.Expediente", [
    "NroOrden int NN", "ComitenteId int", "ConceptoDeudaId int", "NroLiquidacion nvarchar(50)",
    "FechaTitulo date", "FechaAlta date NN", "MontoTitulo decimal(18,2)", "MontoCapital decimal(18,2)",
    # objeto de la deuda
    "NroCuenta nvarchar(50)", "Nomenclatura nvarchar(50)", "Dominio nvarchar(20)", "Designacion nvarchar(100)",
    "MatriculaRpi nvarchar(50)", "DomicilioObjeto nvarchar(250)", "LocalidadObjetoId int", "RegistroPublicoId int",
    "NroResolucionMulta nvarchar(50)", "FechaResolucion date",
    # judicial
    "JuzgadoId int", "NroExpedienteJudicial nvarchar(50)", "Caratula nvarchar(400)", "Recaratulacion nvarchar(400)",
    "Sorteo nvarchar(50)", "FechaSorteo date", "Cedula int",
    # responsables
    "ProcuradorId int", "PatrocinanteId int", "AbogadoInteriorId int",
    "NegociadorUsuarioId int", "DepuradorUsuarioId int",
    # contraparte
    "Comparecio bit NN DEF:0", "FechaComparecio date", "AbogadoContraparte nvarchar(250)",
    "DomicilioAbogadoContraparte nvarchar(250)",
    # gestión
    "RegionId int", "CampaniaId int", "EsGranContribuyente bit NN DEF:0", "EsFisca bit NN DEF:0",
    "ObservacionesTitulo nvarchar(4000)", "Observaciones nvarchar(4000)", "ObservacionesAdministrativas nvarchar(4000)",
    # estado desnormalizado (lo mantiene la aplicación en la misma transacción)
    "EtapaActualId int", "FechaEtapaActual date", "CautelarActualId int", "FechaCautelarActual date",
    "InstanciaActualId int", "FechaInstanciaActual date", "UbicacionActualId int", "FechaUltimoMovimiento date",
], anulable=True, legado=True,
  fks=[("ComitenteId", "conf.Comitente"), ("ConceptoDeudaId", "conf.ConceptoDeuda"),
       ("LocalidadObjetoId", "cat.Localidad"), ("RegistroPublicoId", "conf.RegistroPublico"),
       ("JuzgadoId", "conf.Juzgado"), ("ProcuradorId", "conf.Profesional"),
       ("PatrocinanteId", "conf.Profesional"), ("AbogadoInteriorId", "conf.Profesional"),
       ("NegociadorUsuarioId", "seg.Usuario"), ("DepuradorUsuarioId", "seg.Usuario"),
       ("RegionId", "conf.Region"), ("CampaniaId", "conf.Campania"),
       ("EtapaActualId", "conf.Etapa"), ("CautelarActualId", "conf.Etapa"),
       ("InstanciaActualId", "conf.Instancia"), ("UbicacionActualId", "conf.Ubicacion")],
  unicos=[("NroOrden",)],
  indices=[(("EstudioId", "NroExpedienteJudicial"), (), "NroExpedienteJudicial IS NOT NULL"),
           (("EstudioId", "NroLiquidacion"), (), None), (("EstudioId", "NroCuenta"), (), None),
           (("EstudioId", "Dominio"), (), "Dominio IS NOT NULL"), (("EstudioId", "Nomenclatura"), (), None),
           (("EstudioId", "InstanciaActualId", "FechaUltimoMovimiento"), ("NroOrden", "Caratula"), "Anulado = 0"),
           (("EstudioId", "ProcuradorId"), (), None), (("EstudioId", "JuzgadoId"), (), None)],
  checks=[("MontoTitulo IS NULL OR MontoTitulo >= 0", False), ("NroOrden > 0", False)],
  origen="Expediente (columnas generales)",
  desc="Título de deuda + causa judicial. Las columnas de estado actual reemplazan las ~30 subconsultas TOP 1 de V_Expedientes.")
T("dbo.ExpedienteFiscalizacion", [
    "ExpedienteId bigint NN", "TipoFisca int", "Tipo int", "NroExpediente nvarchar(50)", "NroOrden nvarchar(50)",
    "NroResolucion nvarchar(50)", "NroRecursoReconsideracion nvarchar(50)",
    "ResolFechaResolucion date", "ResolFechaNotificacion date", "ReconFechaResolucion date",
    "ReconFechaNotificacion date", "ContenFechaDemanda date", "ContenFechaApelacion date",
    "ContenFechaDemandaContenciosa date", "FechaReconocimientoDeuda date",
    "ExtiendeResponsabilidad bit NN DEF:0", "ResponsableNombre nvarchar(250)",
    "ResponsableDomicilio nvarchar(250)", "Observaciones nvarchar(2000)"],
  pk=("EstudioId", "ExpedienteId"), fks=[("ExpedienteId", "dbo.Expediente")],
  origen="Expediente.Fisca*", desc="1:1 opcional. Solo para expedientes de fiscalización (EsFisca).")
T("dbo.ExpedienteActa", [
    "ExpedienteId bigint NN", "FechaLabrado date", "HoraLabrado nvarchar(20)", "LugarLabrado nvarchar(250)",
    "TipoYNroLicencia nvarchar(250)", "DescripcionDominio nvarchar(250)", "NroCaso nvarchar(50)",
    "JuzgadoFaltas nvarchar(250)", "NroTramite nvarchar(50)", "FechaTramite date"],
  pk=("EstudioId", "ExpedienteId"), fks=[("ExpedienteId", "dbo.Expediente")],
  indices=[(("EstudioId", "NroCaso"), (), "NroCaso IS NOT NULL"), (("EstudioId", "NroTramite"), (), "NroTramite IS NOT NULL")],
  origen="Expediente (FechaLabrado…FechaTramite)", desc="1:1 opcional. Actas de infracción (multas).")
T("dbo.ExpedienteInfraccion", ["ExpedienteId bigint NN", "InfraccionId int NN", "Fecha date"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("InfraccionId", "conf.Infraccion")],
  origen="InfraccionExpediente")
T("dbo.ExpedienteActividad", ["ExpedienteId bigint NN", "ActividadEconomicaId int NN"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("ActividadEconomicaId", "cat.ActividadEconomica")],
  unicos=[("ExpedienteId", "ActividadEconomicaId")],
  origen="ExpedienteActividad + Expediente.ActividadIIBB/CodigoActividadIIBB")
T("dbo.PeriodoAdeudado", [
    "ExpedienteId bigint NN", "Anio smallint NN", "Cuota nvarchar(50)", "Concepto nvarchar(50)",
    "Vencimiento date", "Tributo decimal(18,2) NN DEF:0", "Recargo decimal(18,2) NN DEF:0",
    "Observaciones nvarchar(2500)"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente")],
  indices=[(("EstudioId", "ExpedienteId", "Anio"), (), None)],
  checks=[("Anio BETWEEN 1950 AND 2100", False)],
  origen="ExpedientePeriodo", desc="Reemplaza también al texto Expediente.Periodos, que se arma a partir de esta tabla.")
T("dbo.Persona", [
    "EsPersonaFisica bit NN DEF:1", "Apellido nvarchar(150)", "Nombre nvarchar(150)", "RazonSocial nvarchar(250)",
    "TipoDocumentoId int", "NroDocumento nvarchar(20)", "Cuit nvarchar(13)", "Sexo char(1)",
    "FechaNacimiento date", "Telefono1 nvarchar(50)", "Telefono2 nvarchar(50)", "Email nvarchar(150)",
    "Observaciones nvarchar(4000)"],
  anulable=True, legado=True, fks=[("TipoDocumentoId", "cat.TipoDocumento")],
  indices=[(("EstudioId", "NroDocumento"), (), None), (("EstudioId", "Cuit"), (), "Cuit IS NOT NULL")],
  checks=[("Sexo IN ('F','M','X')", False),
          ("(EsPersonaFisica = 1 AND Apellido IS NOT NULL) OR (EsPersonaFisica = 0 AND RazonSocial IS NOT NULL)", False)],
  origen="Persona")
T("dbo.Domicilio", ["PersonaId bigint NN", "Tipo tinyint NN DEF:1", "Calle nvarchar(150)", "Numero nvarchar(20)",
                    "Piso nvarchar(10)", "Depto nvarchar(10)", "Barrio nvarchar(100)", "CodigoPostal nvarchar(10)",
                    "LocalidadId int", "Principal bit NN DEF:0"],
  legado=True, fks=[("PersonaId", "dbo.Persona"), ("LocalidadId", "cat.Localidad")],
  indices=[(("EstudioId", "PersonaId"), (), None),
           (("EstudioId", "PersonaId"), (), "Principal = 1", True)],
  checks=[("Tipo BETWEEN 1 AND 4", False)],
  origen="Domicilio", desc="Tipo 1 real, 2 fiscal, 3 constituido, 4 postal. Un solo principal por persona (índice filtrado).")
T("dbo.ExpedientePersona", ["ExpedienteId bigint NN", "PersonaId bigint NN", "Rol tinyint NN", "Posicion tinyint NN DEF:1"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("PersonaId", "dbo.Persona")],
  unicos=[("ExpedienteId", "PersonaId", "Rol")],
  indices=[(("EstudioId", "PersonaId"), (), None)],
  checks=[("Rol BETWEEN 1 AND 3", False)],
  origen="ExpedientePersona (+ Expediente.PersonaID)",
  desc="Rol 1 demandado, 2 codemandado, 3 responsable solidario. Sin límite de codemandados.")
T("dbo.InformePersona", ["PersonaId bigint NN", "Fecha date NN", "Texto nvarchar(4000) NN"],
  legado=True, fks=[("PersonaId", "dbo.Persona")], origen="Informe")
T("dbo.EtapaExpediente", ["ExpedienteId bigint NN", "EtapaId int NN", "FechaInicio date NN", "FechaFin date",
                          "FechaVencimiento date", "Estado tinyint NN DEF:1", "Monto decimal(18,2)",
                          "Observaciones nvarchar(2000)"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("EtapaId", "conf.Etapa")],
  indices=[(("EstudioId", "ExpedienteId", "FechaInicio"), (), None),
           (("EstudioId", "Estado", "FechaVencimiento"), ("ExpedienteId",), "Estado IN (1,3)")],
  checks=[("Estado BETWEEN 1 AND 4", False), ("FechaFin IS NULL OR FechaFin >= FechaInicio", False)],
  origen="EtapaExpediente + CautelarExpediente",
  desc="Historial de etapas de ambos flujos. Estado 1 alta, 2 cumplimentada, 3 suspendida, 4 baja (en el legado 0 = alta). FechaVencimiento se calcula con días hábiles y feriados.")
T("dbo.InstanciaExpediente", ["ExpedienteId bigint NN", "InstanciaId int NN", "Fecha date NN"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("InstanciaId", "conf.Instancia")],
  indices=[(("EstudioId", "ExpedienteId", "Fecha"), (), None)], origen="InstanciaExpediente")
T("dbo.UbicacionExpediente", ["ExpedienteId bigint NN", "UbicacionId int NN", "Fecha date NN"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("UbicacionId", "conf.Ubicacion")],
  indices=[(("EstudioId", "ExpedienteId", "Fecha"), (), None)], origen="ExpedienteLugar")
T("dbo.Tarea", ["ExpedienteId bigint", "TipoTareaId int", "Titulo nvarchar(250) NN", "Descripcion nvarchar(2000)",
                "Lugar nvarchar(250)", "FechaInicio date NN", "FechaVencimiento date", "HoraInicio time(0)",
                "HoraFin time(0)", "TodoElDia bit NN DEF:1", "Alerta bit NN DEF:0", "RepetirHasta date",
                "ResponsableUsuarioId int", "VisibleParaTodos bit NN DEF:0", "Estado tinyint NN DEF:1",
                "CumplidaUtc datetime2(3)", "Origen tinyint NN DEF:1", "EtapaExpedienteId bigint"],
  legado=True,
  fks=[("ExpedienteId", "dbo.Expediente"), ("TipoTareaId", "conf.TipoTarea"),
       ("ResponsableUsuarioId", "seg.Usuario"), ("EtapaExpedienteId", "dbo.EtapaExpediente")],
  indices=[(("EstudioId", "Estado", "FechaVencimiento"), ("ResponsableUsuarioId",), None),
           (("EstudioId", "ExpedienteId"), (), "ExpedienteId IS NOT NULL")],
  checks=[("Estado BETWEEN 1 AND 4", False), ("Origen BETWEEN 1 AND 4", False)],
  origen="ActividadAdministrativaExpediente + Agenda",
  desc="Estado 1 pendiente, 2 cumplida, 3 archivada, 4 anulada. Origen 1 manual, 2 generada por etapa, 3 desde novedad, 4 migrada de Agenda.")
T("dbo.TareaParticipante", ["TareaId bigint NN", "UsuarioId int NN"], pk=("EstudioId", "TareaId", "UsuarioId"),
  base=False, fks=[("TareaId", "dbo.Tarea"), ("UsuarioId", "seg.Usuario")], origen="AgendaUsuario")
T("dbo.NovedadJudicial", ["ExpedienteId bigint", "NroExpedienteInformado nvarchar(50)", "Fecha date NN",
                          "Texto nvarchar(4000) NN", "Fuente tinyint NN DEF:1", "Leida bit NN DEF:0",
                          "LeidaUtc datetime2(3)", "LeidaPor int", "TareaId bigint"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("LeidaPor", "seg.Usuario"), ("TareaId", "dbo.Tarea")],
  indices=[(("EstudioId", "Leida", "Fecha"), ("ExpedienteId",), None)],
  checks=[("Fuente BETWEEN 1 AND 3", False)],
  origen="ExpedienteParte",
  desc="Proveídos, decretos y partes. Fuente 1 manual, 2 importación de archivo, 3 migrada. ExpedienteId NULL = pendiente de vincular.")
T("dbo.Embargo", [
    "ExpedienteId bigint NN", "TipoEmbargoId int NN", "Estado tinyint NN DEF:1", "FechaSolicitud date",
    "FechaTraba date", "FechaLevantamiento date", "Monto decimal(18,2)", "Porcentaje decimal(5,2)",
    "RegistroPublicoId int", "NroRegistro nvarchar(200)", "DireccionRegistro nvarchar(200)",
    "Secretaria nvarchar(200)", "Autorizados nvarchar(500)", "Dominio nvarchar(20)",
    "Nomenclatura nvarchar(200)", "MartilleroId int", "DetalleJson nvarchar(max)", "Observaciones nvarchar(4000)"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("TipoEmbargoId", "conf.TipoEmbargo"),
                    ("RegistroPublicoId", "conf.RegistroPublico"), ("MartilleroId", "conf.Profesional")],
  indices=[(("EstudioId", "ExpedienteId"), (), None), (("EstudioId", "Dominio"), (), "Dominio IS NOT NULL")],
  checks=[("Estado BETWEEN 1 AND 4", False), ("Porcentaje IS NULL OR Porcentaje BETWEEN 0 AND 100", False),
          ("DetalleJson IS NULL OR ISJSON(DetalleJson) = 1", True)],
  origen="EmbargoExpediente",
  desc="Estado 1 solicitado, 2 trabado, 3 levantado, 4 sin efecto (nuevo: el legado no tenía estado; se migra como 2). DetalleJson guarda los datos propios de cada clase (marca, modelo, chasis, sticker; lote, manzana, PH; banco, CBU; etc.).")
T("dbo.PlanPago", ["ExpedienteId bigint NN", "NroPlan nvarchar(40)", "FechaPlan date", "PeriodosIncluidos nvarchar(4000)",
                   "CantCuotas int", "CuotasPagadas int", "CuotasVencidasImpagas int", "CuotasAVencer int",
                   "FechaCaducidad date", "Estado nvarchar(50)", "FechaConsulta date", "Decreto nvarchar(4000)",
                   "Activo bit NN DEF:1"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente")],
  indices=[(("EstudioId", "ExpedienteId"), (), None)],
  origen="ExpedientePlanPago (+ Expediente.NroPlanPagos)",
  desc="Estado es texto libre, como EstadoPlan en Monitorio. Activo indica el plan vigente del expediente.")
T("dbo.Requerimiento", ["ExpedienteId bigint NN", "Fecha date NN", "Descripcion nvarchar(2000)",
                        "Enviado bit NN DEF:0", "EnFolio bit NN DEF:0"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente")], origen="Requerimiento")
T("dbo.EstudioTitulo", ["ExpedienteId bigint NN", "Fecha date NN", "Descripcion nvarchar(2000)",
                        "Pedido bit NN DEF:0", "EnFolio bit NN DEF:0"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente")], origen="EstudioTitulo")
T("dbo.DevolucionCorreo", ["ExpedienteId bigint NN", "CodigoDevolucionId int NN", "Fecha date NN",
                           "CampaniaId int", "NroCaso nvarchar(50)", "UsuarioId int"],
  legado=True, fks=[("ExpedienteId", "dbo.Expediente"), ("CodigoDevolucionId", "conf.CodigoDevolucion"),
                    ("CampaniaId", "conf.Campania"), ("UsuarioId", "seg.Usuario")],
  origen="ExpedienteCodigoDevolucion")
T("dbo.Documento", ["ExpedienteId bigint", "Tipo tinyint NN", "NombreArchivo nvarchar(260) NN",
                    "ContentType nvarchar(100) NN", "TamanioBytes bigint NN", "Sha256 char(64) NN",
                    "RutaAlmacenamiento nvarchar(500) NN", "Descripcion nvarchar(500)",
                    "Eliminado bit NN DEF:0", "EliminadoUtc datetime2(3)", "EliminadoPor int",
                    "MotivoEliminacion nvarchar(500)"],
  fks=[("ExpedienteId", "dbo.Expediente"), ("EliminadoPor", "seg.Usuario")],
  indices=[(("EstudioId", "ExpedienteId"), (), "Eliminado = 0")],
  checks=[("Tipo BETWEEN 1 AND 5", False),
          ("(Eliminado = 0) OR (EliminadoUtc IS NOT NULL AND EliminadoPor IS NOT NULL AND MotivoEliminacion IS NOT NULL)", False)],
  desc="Tipo 1 adjunto, 2 escrito generado, 3 liquidación exportada, 4 plantilla, 5 importación. Contenido en almacenamiento de archivos con prefijo de Estudio; sin borrado automático: se conserva hasta que un usuario con permiso lo elimina.")
T("conf.PlantillaEscritoVersion", ["PlantillaEscritoId int NN", "NroVersion int NN", "DocumentoId bigint NN",
                                   "Notas nvarchar(500)", "Vigente bit NN DEF:1"],
  identidad="int", fks=[("PlantillaEscritoId", "conf.PlantillaEscrito"), ("DocumentoId", "dbo.Documento")],
  unicos=[("PlantillaEscritoId", "NroVersion")])
T("dbo.EscritoGenerado", ["ExpedienteId bigint NN", "PlantillaEscritoVersionId int", "EtapaId int", "InstanciaId int",
                          "TrabajoProcesoId bigint", "UsuarioId int", "FechaUtc datetime2(3) NN",
                          "DocumentoId bigint", "Estado tinyint NN DEF:1"],
  legado=True,
  fks=[("ExpedienteId", "dbo.Expediente"), ("PlantillaEscritoVersionId", "conf.PlantillaEscritoVersion"),
       ("EtapaId", "conf.Etapa"), ("InstanciaId", "conf.Instancia"), ("TrabajoProcesoId", "seg.TrabajoProceso"),
       ("UsuarioId", "seg.Usuario"), ("DocumentoId", "dbo.Documento")],
  indices=[(("EstudioId", "ExpedienteId", "FechaUtc"), (), None)],
  checks=[("Estado BETWEEN 1 AND 3", False)],
  origen="ExpedienteEtapaEscrito + ExpedienteInstanciaEscrito",
  desc="Estado 1 generado, 2 presentado, 3 anulado. Los migrados no tienen documento (el legado no lo guardaba).")

# ---------------------------------------------------------------- liq
T("liq.ParametroLiquidacion", ["ComitenteId int", "Codigo nvarchar(50) NN", "Valor decimal(18,6)", "ValorFecha date",
                               "VigenteDesde date NN", "VigenteHasta date", "Observaciones nvarchar(500)"],
  fks=[("ComitenteId", "conf.Comitente")],
  unicos=[("Codigo", "ComitenteId", "VigenteDesde")],
  checks=[("Valor IS NOT NULL OR ValorFecha IS NOT NULL", False),
          ("VigenteHasta IS NULL OR VigenteHasta >= VigenteDesde", False)],
  origen="UnidadPlanilla + valores de App.config + constantes en frmPlanilla.cs",
  desc="Parámetros con vigencia. ComitenteId NULL = valor general del Estudio; con ComitenteId = particular del comitente (tiene prioridad).")
T("liq.TasaInteres", ["ComitenteId int", "Codigo nvarchar(50) NN", "Periodicidad tinyint NN", "Valor decimal(12,6) NN",
                      "VigenteDesde date NN", "VigenteHasta date"],
  fks=[("ComitenteId", "conf.Comitente")], unicos=[("Codigo", "ComitenteId", "VigenteDesde")],
  checks=[("Periodicidad IN (1,2,3)", False)],
  desc="Periodicidad 1 diaria, 2 mensual, 3 anual. Valor en tanto por uno (0.000830 = 0,083 %). Reemplaza 0,083 % y 0,1 % diarios fijos en código.")
T("liq.EscalaHonorarios", ["ComitenteId int", "VigenteDesde date NN", "BaseDesde decimal(18,2) NN",
                           "BaseHasta decimal(18,2)", "Porcentaje decimal(7,4) NN"],
  fks=[("ComitenteId", "conf.Comitente")], unicos=[("ComitenteId", "VigenteDesde", "BaseDesde")],
  desc="Tramos de la escala (9 % … 3 % en el legado, fijos en código).")
T("liq.RubroPlanilla", ["Nombre nvarchar(50) NN", "Descripcion nvarchar(400)", "DescripcionTotal nvarchar(100)",
                        "Orden int NN", "PadreId int"],
  legado=True, identidad="int", fks=[("PadreId", "liq.RubroPlanilla")], unicos=[("Nombre",)],
  origen="RubroPlanilla")
T("liq.ItemPlanilla", ["RubroPlanillaId int NN", "Nombre nvarchar(100) NN", "Descripcion nvarchar(4000)",
                       "TipoCalculo tinyint NN", "Valor decimal(18,6)", "EsPorcentaje bit NN DEF:0",
                       "MontoMinimo decimal(18,2)", "CargaManual bit NN DEF:0", "EsSubtotal bit NN DEF:0",
                       "EsDefault bit NN DEF:0", "OrdenDefault int", "Activo bit NN DEF:1"],
  legado=True, identidad="int", fks=[("RubroPlanillaId", "liq.RubroPlanilla")], unicos=[("Nombre",)],
  checks=[("TipoCalculo BETWEEN 1 AND 11", False)],
  origen="ItemsPlanilla",
  desc="TipoCalculo reemplaza las 9 columnas Es*: 1 monto fijo, 2 porcentaje del capital, 3 capital, 4 interés simple, 5 tasa pasiva, 6 interés sobre capital, 7 honorarios, 8 tasa de justicia, 9 aportes, 10 gastos de diligencias, 11 subtotal.")
T("liq.ItemPlanillaTope", ["ItemPlanillaId int NN", "VigenteDesde date NN", "VigenteHasta date",
                           "Minimo decimal(18,2)", "Maximo decimal(18,2)"],
  legado=True, identidad="int", fks=[("ItemPlanillaId", "liq.ItemPlanilla")],
  unicos=[("ItemPlanillaId", "VigenteDesde")],
  checks=[("Maximo IS NULL OR Minimo IS NULL OR Maximo >= Minimo", False)],
  origen="ItemsMontoMinMax (IemsMontoMinMax se descarta tras comparar contenido)")
T("liq.PlanillaJuzgado", ["JuzgadoId int NN", "ItemPlanillaId int NN", "Orden int NN"],
  pk=("EstudioId", "JuzgadoId", "ItemPlanillaId"), base=False,
  fks=[("JuzgadoId", "conf.Juzgado"), ("ItemPlanillaId", "liq.ItemPlanilla")], origen="JuzgadoItems")
T("liq.Liquidacion", ["ExpedienteId bigint NN", "NroVersion int NN", "FechaCalculo date NN", "Estado tinyint NN DEF:1",
                      "JuzgadoId int", "TotalCapital decimal(18,2) NN", "TotalInteres decimal(18,2) NN",
                      "TotalGastos decimal(18,2) NN", "HonorariosJus decimal(9,2)", "HonorariosPesos decimal(18,2) NN",
                      "Total decimal(18,2) NN", "ParametrosJson nvarchar(max) NN", "DocumentoId bigint"],
  fks=[("ExpedienteId", "dbo.Expediente"), ("JuzgadoId", "conf.Juzgado"), ("DocumentoId", "dbo.Documento")],
  unicos=[("ExpedienteId", "NroVersion")],
  checks=[("Estado BETWEEN 1 AND 4", False), ("ISJSON(ParametrosJson) = 1", True),
          ("Total = TotalCapital + TotalInteres + TotalGastos + HonorariosPesos", True)],
  desc="Nueva: el legado no guardaba planillas. Estado 1 borrador, 2 guardada, 3 presentada, 4 anulada. ParametrosJson copia los valores usados para que sea reproducible.")
T("liq.LiquidacionLinea", ["LiquidacionId bigint NN", "Orden int NN", "RubroPlanillaId int", "ItemPlanillaId int",
                           "Concepto nvarchar(400) NN", "PeriodoAdeudadoId bigint", "Base decimal(18,2)",
                           "Tasa decimal(12,6)", "Dias int", "Importe decimal(18,2) NN", "EsManual bit NN DEF:0",
                           "Detalle nvarchar(1000)"],
  base=False,
  fks=[("LiquidacionId", "liq.Liquidacion"), ("RubroPlanillaId", "liq.RubroPlanilla"),
       ("ItemPlanillaId", "liq.ItemPlanilla"), ("PeriodoAdeudadoId", "dbo.PeriodoAdeudado")],
  unicos=[("LiquidacionId", "Orden")])

# ---------------------------------------------------------------- aud / mig
T("aud.AuditoriaEvento", ["EstudioId int", "UsuarioId int", "FechaUtc datetime2(3) NN", "Modulo nvarchar(40) NN",
                          "Entidad nvarchar(60)", "EntidadId nvarchar(40)", "Accion tinyint NN", "Resultado tinyint NN DEF:1",
                          "CambiosJson nvarchar(max)", "TrabajoProcesoId bigint", "CorrelacionId uniqueidentifier",
                          "Ip varchar(45)", "UserAgent nvarchar(300)"],
  tenant=False, base=False,
  fks=[("EstudioId", "seg.Estudio"), ("UsuarioId", "seg.Usuario")],
  indices=[(("EstudioId", "FechaUtc"), (), None), (("EstudioId", "Entidad", "EntidadId"), (), None),
           (("EstudioId", "UsuarioId", "FechaUtc"), (), None)],
  checks=[("Accion BETWEEN 1 AND 14", False), ("Resultado BETWEEN 1 AND 3", False),
          ("CambiosJson IS NULL OR ISJSON(CambiosJson) = 1", True)],
  origen="Nueva: reemplaza a LogSistema, que no se migra",
  desc="Solo inserción. EstudioId NULL = evento de plataforma. Accion 1 crear, 2 editar, 3 anular, 4 eliminar, 5 cambiar estado, 6 calcular, 7 generar, 8 exportar, 9 login, 10 login fallido, 11 logout, 12 cambiar permisos, 13 aprobar membresía, 14 acceso de soporte. Resultado 1 ok, 2 rechazado, 3 error.")
T("mig.DatoARevisar", ["Tipo nvarchar(60) NN", "Entidad nvarchar(60) NN", "EntidadId bigint",
                       "TablaOrigen nvarchar(60)", "ClaveOrigen uniqueidentifier", "Campo nvarchar(60)",
                       "ValorOriginal nvarchar(4000)", "ValorPropuesto nvarchar(4000)", "Estado tinyint NN DEF:1",
                       "ResueltoPor int", "ResueltoUtc datetime2(3)", "Comentario nvarchar(1000)"],
  fks=[("ResueltoPor", "seg.Usuario")],
  indices=[(("EstudioId", "Estado", "Tipo"), (), None)],
  checks=[("Estado BETWEEN 1 AND 3", False)],
  desc="Cola de la pantalla Datos a revisar. Estado 1 pendiente, 2 resuelto, 3 descartado.")

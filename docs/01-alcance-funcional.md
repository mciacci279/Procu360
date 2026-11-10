# Procu360 — Alcance funcional

> Versión 0.3 · 09/11/2026 · Incorpora las decisiones del 09/11/2026 (§8). Modelo y datos de prueba: doc. 03 y carpeta `db/`.
> Fuentes: script `LegisProcessDBMonitorio` (SQL Server), código WinForms LegisProcess Monitorio (como referencia del objetivo, no como especificación literal), prototipo Procu360.

## 1. Objetivo

Procu360 es una aplicación web para gestionar la procuración fiscal de deudas vencidas: títulos de deuda, expedientes pre judiciales y judiciales, etapas y diligencias (cédulas, oficios), cautelares y embargos, generación individual y masiva de escritos, actividades administrativas, liquidación de deuda (planillas), planes de pago y documentación.

Reemplaza a LegisProcess Monitorio. **Se migran todos los datos de negocio** de su base SQL Server (ver doc. 03 §6). No se migran contraseñas ni datos propios de integraciones externas.

## 2. Principio rector: el Estudio como tenant

**Estudio** es la entidad raíz. Todo dato de negocio pertenece a exactamente un Estudio y solo es visible para usuarios con membresía activa en ese Estudio.

### 2.1 Qué pertenece a un Estudio

- Membresías de usuarios, roles y permisos.
- Profesionales: procuradores, patrocinantes, apoderados, abogados de interior, martilleros.
- Comitentes/acreedores y sus conceptos de deuda.
- **Organización judicial**: juzgados con secretaría, juez, ubicación y domicilio.
- Expedientes, personas, domicilios, períodos, etapas, embargos, planes de pago, tareas, novedades, escritos, liquidaciones, documentos.
- Configuración: etapas y flujos, instancias, plantillas de escritos, parámetros de liquidación por comitente, feriados propios, numeración, retención de auditoría.
- Auditoría.

### 2.2 Qué es compartido (catálogos de plataforma)

Solo datos públicos y estables: provincias, localidades, tipos de documento, condiciones de IVA, feriados nacionales/provinciales, el índice de tasa pasiva y el nomenclador de actividades económicas. Los mantiene el superadministrador.

### 2.3 Alta de un Estudio y copia de catálogos

Al crear un Estudio, el superadministrador elige un **Estudio origen** (o la plantilla base de la plataforma) y marca qué copiar:

- juzgados;
- etapas, flujos e instancias;
- tipos de tarea, tipos de embargo, registros públicos, ubicaciones, infracciones, códigos de devolución de correo;
- rubros e ítems de planilla y planillas por juzgado;
- plantillas de escritos;
- roles y su matriz de permisos.

La copia es independiente: cambios posteriores en un Estudio no afectan al otro. Nunca se copian expedientes, personas, comitentes, profesionales ni usuarios. Se registra el origen de la copia y queda auditada.

### 2.4 Usuarios y membresías en varios Estudios

Una identidad (email) puede pertenecer a varios Estudios. Al iniciar sesión elige el Estudio activo y puede cambiarlo sin reautenticarse; los permisos se evalúan siempre en el Estudio activo.

Regla (confirmada):

| Situación | Quién la resuelve |
|---|---|
| Invitar a un email **que no pertenece a ningún Estudio** | El administrador del Estudio (el usuario acepta la invitación) |
| Invitar a un email **que ya pertenece a otro Estudio** | Queda *pendiente de aprobación* hasta que un **superadministrador** la aprueba o rechaza |
| Desactivar una membresía | El administrador del Estudio (solo afecta a ese Estudio) |

Motivo: la membresía múltiple es la que puede exponer información entre Estudios (un mismo usuario con acceso a dos carteras), por eso requiere aprobación de plataforma; el alta común no debe depender del superadministrador para no frenar la operación diaria. El administrador de un Estudio nunca ve en qué otros Estudios está el usuario.

### 2.5 Superadministrador (plataforma)

- Crea y suspende Estudios; aprueba membresías múltiples; mantiene catálogos compartidos.
- No ve datos de un Estudio salvo **acceso de soporte** explícito, temporal y auditado.
- MFA obligatorio.

### 2.6 Ámbito de datos dentro del Estudio

Además del permiso módulo/acción, cada rol define un ámbito: *todo el Estudio*, *por comitente* o *asignados* (expedientes donde el usuario es responsable, negociador, depurador o abogado de interior).

### 2.7 Estrategia multi-tenant

Base compartida con `EstudioId` en toda tabla de negocio:

1. Claves únicas y foráneas compuestas con `EstudioId` (imposible referenciar un registro de otro Estudio).
2. Filtro global en EF Core por el Estudio activo.
3. Row-Level Security de SQL Server con `SESSION_CONTEXT('EstudioId')` como segunda barrera.
4. Archivos separados por prefijo de Estudio.
5. Pruebas automatizadas de aislamiento obligatorias.
6. Resolvedor de conexión abstraído para poder mover un Estudio a base propia sin reescribir.

> En Monitorio, `ProcuradorID` cumplía parcialmente este rol, de forma inconsistente. Procu360 lo formaliza en `Estudio`.

## 3. Módulos y funcionalidades

**MVP** = imprescindible para reemplazar Monitorio; **F2** = segunda fase.

### 3.1 Mi día (MVP)
Indicadores (vencimientos hoy, novedades sin revisar, sin movimiento +90 días, etapas por vencer/vencidas, perención próxima); lista de vencimientos y tareas; novedades sin revisar; datos a revisar de la migración. Filtrado por ámbito.

### 3.2 Expedientes (MVP)
- **Búsqueda unificada** en un campo: nro de orden, nro de expediente judicial, DNI/CUIT, nro de liquidación, nro de cuenta/resolución, dominio, nomenclatura, nro de caso, nro de trámite o carátula; normaliza ceros a la izquierda. Filtros equivalentes a los ~25 de Monitorio (procurador, juzgado, solo Capital/Interior, instancia, concepto, monitorio/ejecución, negociador, depurador, abogado de interior, región, campaña, ubicación, registro, tipo de fisca, gran contribuyente, actividad, incluir dados de baja).
- Alta individual, **alta rápida** y **carga masiva** desde planilla con validación previa y confirmación.
- Ficha: título (comitente, concepto, nro de liquidación, fecha de título, monto, capital), objeto de la deuda (cuenta/resolución, nomenclatura, dominio, designación, matrícula RPI), períodos, personas (demandado y codemandados sin límite) con domicilios, datos judiciales (juzgado, nro de expediente, carátula y recaratulación, sorteo), responsables, contraparte, fiscalización, acta/infracciones, actividades económicas, observaciones.
- **Modificación masiva** con confirmación previa y auditoría.

### 3.3 Flujo procesal (MVP)
- Etapas por Estudio con plazo (hábiles/corridos), corta perención, interrumpe, monitorio/ejecución, etapa siguiente automática, generación de tarea y usuarios notificados, hito de sistema (inicio de demanda, demanda presentada). Las **diligencias** (cédulas, oficios) se registran como etapas del flujo; Procu360 agrega la marca "diligencia" para distinguirlas en la línea de tiempo y los listados.
- Flujo cautelar con su propio catálogo de etapas.
- Historial de etapas con estado (alta, cumplimentada, suspendida, baja) y monto.
- Instancias (macroestado, perime, baja) y ubicación física.
- Alertas de etapa por vencer y vencida; control de perención.

### 3.4 Novedades (MVP)
Registro manual o por importación de archivo de proveídos, decretos y "partes", con estado leída/revisada; desde una novedad se crea tarea. Novedades sin expediente quedan en bandeja para vincular.

### 3.5 Tareas y agenda (MVP)
Tareas con vencimiento, responsable, participantes, estado; tipos con plazo; agenda personal y del Estudio; tareas generadas por etapas.

### 3.6 Requerimientos y estudios de título (MVP)
Registro de requerimientos (enviado, en folio, fecha) y pedidos de estudio de título por expediente (existen en Monitorio).

### 3.7 Escritos (MVP)
Plantillas DOCX por Estudio (escrito, cédula, carta, oficio) asociables a etapa o instancia; catálogo de variables; generación individual y **masiva** en segundo plano con validación de datos faltantes, descarga ZIP y registro de cada escrito generado; opción de avanzar etapa.

### 3.8 Correspondencia (MVP)
Registro de códigos de devolución de correo por expediente y campaña (existe en Monitorio). Sin intercambio de archivos con el correo.

### 3.9 Embargos y cautelares (MVP registro, F2 escritos masivos de embargo)
Tipos: Automotor, Inmueble, Inhibición, Intervención de Caja, Embargo de cuentas corrientes y Reserva de cautelar, cada uno con sus datos propios, monto, porcentaje, registro y martillero.

### 3.10 Liquidación (MVP)
- **Fórmulas parametrizadas por comitente**, con vigencia: capital, % de capital con mínimo, interés simple, tasa pasiva, interés sobre capital (tasas por tramo de fecha), tasa de justicia y su interés, aportes, honorarios (escala por tramos, mínimo, máximo, reparto procurador/patrocinante, IVA según condición), ítems fijos y manuales.
- Planillas por juzgado (ítems y orden), rubros con subtotales, monto en letras.
- Cada liquidación se guarda como instantánea versionada con los parámetros usados.
- Liquidación masiva: F2.

### 3.11 Planes de pago (MVP)
Registro y seguimiento (el estado del plan es texto libre, como en Monitorio); uso en escritos y reportes.

### 3.12 Listados y tableros (MVP listados, F2 tableros)
Exportación de búsquedas a Excel (con permiso), informe de policía fiscal, estadísticas por negociador, alertas, embargos, planes de pago.

### 3.13 Documentos (MVP)
Adjuntos por expediente. **Se conservan hasta que un usuario con permiso los elimine**; la eliminación pide confirmación, registra motivo y queda auditada. No hay borrado automático por antigüedad.

### 3.14 Datos a revisar (MVP)
Cola de problemas de la migración y de calidad: montos no numéricos, fechas inválidas, nros de expediente duplicados, personas duplicadas, estados inconsistentes, etc.

## 4. Fuera de alcance

- **Integraciones externas** (OTAX/KLK, ITAX, Poder Judicial, ROL/AFC, archivos de correo, importadores específicos de caminera y municipios): descartadas. Sus tablas de log y exportación (`KLKWSLog`, `LogExportacion`, `Exportacion`, `AFC`, `PJ_*`, `EstandarEtapa`, códigos `ItaxID`/`MuniCordobaID`) no se migran.
- Licencias y trials, ejecución de scripts SQL desde la aplicación, formularios de prueba, Crystal Reports.
- Facturación de honorarios y cobranzas.

## 5. Administración

- Usuarios por invitación; desactivación sin borrado; MFA opcional (obligatorio para administradores y superadministradores).
- Roles por Estudio (sugeridos: Administrador, Abogado/Procurador, Gestor, Liquidaciones, Solo lectura, Auditor) con **matriz módulo × acción**: consultar, crear, editar, anular/eliminar, exportar, aprobar, administrar.
- Catálogos y parámetros del Estudio; configuración (retención de auditoría, numeración, inactividad de sesión).

## 6. Auditoría y permisos

- Permisos granulares por módulo y acción, aplicados en backend en cada endpoint y operación masiva.
- Auditoría de altas, cambios, bajas/anulaciones, cambios de estado, cálculos, generación de escritos, exportaciones, eliminación de documentos, sesiones, intentos fallidos, cambios de permisos y aprobaciones de membresía.
- Por evento: usuario, Estudio, fecha/hora UTC, entidad, identificador, acción, resultado, IP, id de correlación y diferencias anterior/nuevo.
- Operaciones masivas: evento cabecera más detalle por registro.
- Vista filtrable por usuario, fecha, módulo, acción, registro y resultado, con permiso específico.
- Solo inserción; nadie la edita desde la aplicación.
- Reemplaza a `LogSistema` de Monitorio, que no se migra (su historial queda en el respaldo de Monitorio).
- **Retención configurable** en *Administración › Configuración del Estudio*, **por defecto 365 días**. Un proceso diario elimina los eventos vencidos y registra cuántos eliminó. Los eventos de plataforma (superadministrador) tienen su propia retención.
- Baja lógica en datos de negocio; eliminación física solo con permiso "administrar" y sin dependencias.

La auditoría funcional no sustituye logs técnicos de errores y diagnóstico.

## 7. Requisitos no funcionales

- Web moderna, optimizada para escritorio y usable en tablet.
- Backend ASP.NET Core y SQL Server; entornos separados (desarrollo, homologación con datos anonimizados, producción).
- Validación en cliente y servidor.
- Fechas `date`/`datetime2` UTC, importes `decimal(18,2)`, tasas `decimal(12,6)`; API ISO 8601 y punto decimal.
- Backups verificados y restauración documentada y probada.
- Protección de credenciales, datos personales y documentos (Ley 25.326).
- Pruebas automatizadas, de aislamiento entre Estudios y de aceptación.
- Concurrencia optimista; procesos masivos en segundo plano con progreso y reporte.

### 7.1 Recomendación de arquitectura (más ágil)

1. **Monolito modular** ASP.NET Core (.NET LTS vigente), un solo despliegue.
2. UI servida por el mismo host con **cookie de sesión** (BFF): **Blazor Web App** si el equipo es .NET y chico; **React + TypeScript** si hay perfil frontend.
3. **ASP.NET Core Identity** para autenticación, bloqueo, recuperación y MFA.
4. **Auditoría con interceptor de EF Core** en una sola tabla con diff JSON.
5. Procesos masivos con worker en el mismo proceso y tabla de trabajos.
6. Escritos DOCX con **OpenXML** en servidor (nunca Office Interop).
7. Hosting gestionado con backups automáticos, o IIS + SQL Server con plan de backup y restauración de prueba trimestral.
8. Logging estructurado con id de correlación.

## 8. Decisiones registradas (09/11/2026)

| # | Tema | Decisión |
|---|---|---|
| 1 | Origen de datos | Se migra todo LegisProcess Monitorio; la BD Access no se considera |
| 2 | Organización judicial | Por Estudio, con copia desde otro Estudio o plantilla al crear un Estudio |
| 3 | Fórmulas de liquidación | Parametrizadas por comitente y con vigencia (confirmado) |
| 4 | Integraciones | Descartadas |
| 5 | Retención | Auditoría configurable, 365 días por defecto; documentos hasta eliminación por usuario autorizado |
| 6 | Multi-Estudio | Permitido; aprobación de superadministrador para membresías en un segundo Estudio (§2.4, confirmado) |
| 7 | Cuentas por expediente | Un solo número de cuenta / objeto de deuda por expediente |
| 8 | `EtapaDeuda` | Nombre anterior de `EtapaExpediente`; no es otra tabla |
| 9 | Procedimientos almacenados | Referencia secundaria: el script recibido es el único disponible; se toma la lógica que convenga y ante diferencias manda el código |
| 10 | Control de acceso del WinForms | `MenuSistema`, `MenuPerfil`, `Perfil` y `PerfilUsuario` se descartan; Procu360 usa sus propios roles y permisos |
| 11 | Auditoría | `LogSistema` se reemplaza por la auditoría nueva y no se migra |

## 9. Pendientes

Ninguno de definición. Próximo paso: ejecutar los scripts de `db/` en SQL Server (doc. 03 §10).

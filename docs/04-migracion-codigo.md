# Procu360 — El código de Monitorio como referencia

> Versión 0.3 · 09/11/2026
> Código analizado: `D:\Google Drive\Proyectos\LegisProcess\Monitorio\BACKUPS\LegisProcessMonitorio` (WinForms, última modificación 2020).
> Leídos en detalle: `frmPlanilla.cs`, `frmEscritos.cs`, `frmLogin.cs`, `LegisProcess.WinForms/App.config` e historial de versiones. El resto se inventarió por nombre, tamaño y estructura.

## 1. Enfoque

Procu360 **no es una migración del código**. Monitorio se usa como base para entender el objetivo: qué hace el estudio todos los días, qué reglas aplica, qué datos usa y qué resultados espera (planillas y escritos). Lo que se lleva de Monitorio es:

1. **Reglas de negocio** (§4), escritas como especificación y probadas con casos reales.
2. **Datos**: se migran todos los de la base (doc. 03 §7).
3. **Plantillas de escritos**, convertidas a DOCX.

Todo lo demás (pantallas, acceso a datos, reportes, licenciamiento) se diseña de nuevo. El código de Monitorio no es una buena base técnica: ~90 % es generado (.netTiers), la lógica está mezclada con controles de pantalla en formularios de 100–340 KB, depende de Office Interop, Crystal Reports y .NET Framework, y tiene defectos de seguridad estructurales (§5).

## 2. Proyectos de la solución Monitorio

| Proyecto | Uso en Procu360 |
|---|---|
| `LegisProcess.WinForms` | **Referencia funcional**: formularios (qué hace cada pantalla), `Tools/Funciones.cs` (búsqueda, días hábiles) |
| `LegisProcess.SQL` | **Referencia secundaria**: `procedures.sql` (alertas, perención, informes). Es el único script disponible y puede diferir del código: se toma la lógica que convenga y ante diferencias manda el código |
| `LegisProcess.Services` | Referencia puntual: `ExpedienteService`, `DomicilioService`, `ExpedientePersonaService` |
| `LegisProcess.Entities`, `LegisProcess.Data`, `LegisProcess.Data.SqlClient` | Sin uso (código generado; lo reemplaza EF Core) |
| `LegisProcess.ImportadorMonitorio` | **Descartado** |
| `LegisProcess.WebService` | **Descartado** |
| `LegisProcessWCService` | **Descartado** |
| `LegisProcess.GeneradorLicencias`, `GeneradorTrials`, `GeneradorDate` | **Descartados** |
| `LegisProcess.UnitTests` | **Descartado** |

## 3. Formularios de referencia por módulo

| Módulo Procu360 | Formularios de Monitorio que muestran el objetivo |
|---|---|
| Expedientes | `frmExpedienteEdicion`, `frmExpedienteConsultar`, `frmExpedienteListado`, `frmExpedienteAltaRapida`, `frmExpedienteCargaMasiva`, `frmExpedientePeriodoEdicion`, `frmPeriodos`, `frmExpedienteRegistro`, `frmExpedienteLugarEdicion`, `frmExpedienteActividad`, `frmPersonaEdicion/Listado`, `frmDomicilioEdicion/Listado` |
| Novedades | `frmExpedienteParte` |
| Flujo procesal | `frmEtapaEdicion/Listado`, `frmInstancia*`, `frmCautelar*`, `frmCautelarExpedienteEdicion`, `frmInstanciaExpedienteEdicion`, `frmActividad*`, `frmActividadExpedienteEdicion` |
| Tareas y agenda | `frmAgendaEdicion/Listado`, `frmFeriado*`, `frmFeriadoCalcularDias` |
| Escritos | `frmEscritos`, `frmEscritosByEtapas`, `frmEscritosCautelares`, `frmEtapaEscritoEdicion/Listado`, `frmIntimacionHistoricoListado` |
| Correspondencia | `frmCodigoDevolucion*` |
| Liquidación | `frmPlanilla`, `frmPlanillasMasivas`, `frmItemsPlanilla*`, `frmItemsPlanillaMinMax`, `frmRubroPlanilla*`, `frmUnidadPlanilla*`, `frmAlicuotaImportador`, `frmAlicuotaPlanilla*`, `frmJuzgadoItems*` |
| Embargos y planes | `frmEmbargoExpedienteEdicion`, `frmTipoEmbargo*`, `frmRegistro*`, `frmExpedientePlanPago`, `frmPlanPagosListado` |
| Listados | `frmListados`, `frmListadoPoliciaFiscal`, `frmEstadisticas` |
| Administración | `frmUsuario*`, `frmProcurador*`, `frmConfiguracion*`, catálogos (`frmJuzgado*`, `frmLocalidad*`, `frmProvincia*`, `frmIva*`, `frmImpuesto*`, `frmLugar*`, `frmInfraccion*`, `frmDomicilioDGR*`) |
| **Sin equivalente** | Integraciones (`frmWS*`, `frmExportacion*`, `frmExportadosITAX`, `frmROL*`, `frmAFC*`, `frmImportaMuni*`, `frmImportarCausasCaminera`, `frmCorreoGenerarArchivo`, `frmImportarCorreo`), `frmLicencia`, `frmRunScripts`, `frmPrueba`, `frmXMLTest`, `frmTestWS*`, `frmProcuradorSeleccionar` (lo reemplaza el selector de Estudio), `frmLogin` (lo reemplaza Identity), `frmPerfil*`, `frmPerfilUsuarioEdicion`, `frmMenu*` (control de acceso del WinForms, se descarta), `frmLogSistema` (lo reemplaza la auditoría nueva) |

## 4. Reglas de negocio a conservar

### 4.1 Planilla de liquidación (`frmPlanilla.cs`)

La planilla se arma con los ítems configurados para el juzgado del expediente (`liq.PlanillaJuzgado`), separados en ítems de carga manual y preconfigurados. Todos los valores numéricos pasan a parámetros con vigencia por comitente (`liq.ParametroLiquidacion`, `liq.TasaInteres`, `liq.EscalaHonorarios`, `liq.ItemPlanillaTope`); en el código de Procu360 no queda ninguno fijo.

| `TipoCalculo` | Cálculo en Monitorio | Parámetros en Procu360 |
|---|---|---|
| 2 Porcentaje del capital | `MontoCapital × % / 100`, con mínimo | `ItemPlanilla.Valor`, `MontoMinimo` |
| 3 Capital | `MontoCapital`, rotulado "al {fecha de demanda presentada}" | Etapa con `HitoSistema = 2` |
| 4 Interés simple | `MontoCapital × (tasa mensual / 30,4166…) × (días desde FechaAlta / 100)` | `TasaInteres INTERES_SIMPLE` |
| 5 Tasa pasiva | Alícuota = `((100 + coef. del día de cálculo) / (100 + coef. del día anterior a la demanda presentada) − 1) × 100`; monto = `Capital × alícuota / 100` | `cat.IndiceTasaPasiva` |
| 6 Interés sobre capital | Por período: interés diario = `Tributo × 0,083 %`; capitalizable = `días(demanda − vencimiento) × interés diario`; interés = `(Tributo + capitalizable) × 0,083 % × días(31/03/2014 − demanda) + (Tributo + capitalizable) × 0,1 % × días(fecha planilla − 31/03/2014) + capitalizable` | `TasaInteres INTERES_CAPITAL` (dos vigencias), `FECHA_CORTE_INTERES` |
| 7 Honorarios | Base = `Capital + interés sobre capital`; % por escala (≤ 25.870 → 9 %; ≤ 77.611 → 7,8 %; ≤ 155.222 → 6,6 %; ≤ 258.703 → 5,4 %; ≤ 517.406 → 4,2 %; más → 3 %); mínimo = mitad del mínimo del ítem (13 → 6,5 JUS); máximo = 50 % de la base; con procurador y patrocinante 60 % / 40 %; IVA en línea aparte si el profesional es Responsable Inscripto | `EscalaHonorarios`, `HONORARIOS_MINIMO_JUS`, `HONORARIOS_MAXIMO_PCT_BASE`, `REPARTO_*`, `IVA_PCT`, `VALOR_JUS` |
| 8 Tasa de justicia | `2 % × monto del título`, con mínimo y máximo vigentes a la fecha de la demanda; interés = `TJ × 0,0657534 × días(fecha planilla − demanda) / 100` en línea aparte | `TASA_JUSTICIA_PCT`, `ItemPlanillaTope`, `INTERES_TASA_JUSTICIA_ANUAL_PCT` |
| 9 Aportes | Igual que tasa de justicia, con sus topes | `APORTES_PCT`, `ItemPlanillaTope` |
| 1 Monto fijo | Monto del ítem | `ItemPlanilla.Valor` |

Además: subtotales por rubro, total, monto en letras ("Asciende la presente planilla a la suma de PESOS …"), título con la carátula, y variables en las descripciones (`<<FechaInicio>>`, `<<FechaHoy>>`, `<<Procurador>>`, `<<Patrocinante>>`, `<<CARATULA>>`).

**Defectos que no se replican** (el estudio confirma el resultado correcto con los casos de §6.1):

1. Interés simple: `días / 100` es división entera en C#; trunca los días a centenas (99 días → 0).
2. Algunos cálculos usan la fecha de la planilla y otros `DateTime.Now`.
3. Interés sobre capital: si la demanda es posterior al 31/03/2014 el primer tramo da días negativos y resta interés.
4. Honorarios con un solo profesional: busca la condición de IVA del patrocinante aunque no exista.
5. Escalas, tasas, porcentajes y fecha de corte fijos en código o en `App.config`.
6. Montos convertidos desde texto según la cultura del equipo.
7. Si falta un coeficiente o la etapa de demanda presentada, el ítem queda en 0 sin bloquear la planilla. En Procu360 el ítem se bloquea con el motivo.

### 4.2 Escritos (`frmEscritos.cs` y derivados)

Monitorio arma una planilla Excel temporal con una fila por expediente y combina con Word la plantilla `.doc/.dot` elegida. Procu360 genera DOCX en el servidor con OpenXML, sin Office.

Campos combinables (~110) que el catálogo de variables de Procu360 debe cubrir:

- **Expediente**: nro de orden (y versión código de barras), nro de liquidación, nro de resolución (= nro de cuenta), nro de expediente judicial, carátula, fecha del título, designación, dominio, región, nro y fecha de trámite, matrícula RPI, actividades, períodos, monto y monto en letras, vencimiento de la etapa.
- **Concepto de deuda**: nombre y texto para escritos con variables internas (nro de resolución, liquidación, expediente, períodos, dominio, monto, monto en letras).
- **Demandado y codemandados** (Monitorio: hasta 3; Procu360: sin límite con bloque repetible): nombre, documento, CUIT, domicilio, domicilio fiscal, CP, localidad, provincia.
- **Juzgado**: tribunal, secretaría, juez, ubicación, juzgado de faltas.
- **Profesionales**: procurador (nombre, matrícula, CUIT, IVA, domicilio), apoderado elegido al generar (nombre, título, CUIT, domicilio, texto), abogado de interior.
- **Comitente**: domicilio, localidad, provincia.
- **Acta**: infracciones, fecha/hora/lugar de labrado, licencia, descripción del dominio, nro de caso.
- **Embargo** (Monitorio: el primero; Procu360: todos, en bloque repetible): tipo, monto, secretaría, autorizados y datos de su clase.
- **Planes de pago** (Monitorio: hasta 5): "plan de pagos de fecha …, Nº … que incluyó los períodos … caducado de oficio con fecha … por incumplimiento;".

Reglas de armado: la recaratulación reemplaza a la carátula; en juzgados de Capital el nro de expediente lleva `/36` si no lo tiene; períodos `año/cuota` ordenados y separados por `; `; actividades "A (cód), B (cód) y C (cód)"; vencimiento de etapa en días hábiles con feriados; persona física "Apellido, Nombre"; domicilio principal; aviso previo si hay novedades sin leer. El corte de textos en 255 caracteres era un límite del driver de Excel y no aplica.

### 4.3 Otras reglas a relevar al construir cada módulo

| Regla | Dónde está en Monitorio |
|---|---|
| Días hábiles y feriados | `Funciones.GetDiasHabiles`, `frmFeriadoCalcularDias` |
| Alertas de etapa por vencer y vencida | `sp_Expediente_GetAlertaEtapaPorVencer` / `...Vencida` (estado alta o suspendida, `FechaInicio + Dias`) |
| Perención | `V_ExpedientesPerencion`, `sp_Expediente_GetByFechaDesde(Hasta)` (última etapa que corta perención, instancia que perime) |
| Búsqueda con ~25 filtros | `Funciones.GetExpedientes` |
| Carga y modificación masiva | `frmExpedienteCargaMasiva` (confirmación previa, versión 2014) |
| Etapa siguiente automática y agenda | `frmEtapaEdicion`, `EtapaAgendaUsuario` |
| Informe Policía Fiscal y estadísticas por negociador | `sp_V_Expedientes_GetInformePF`, `sp_V_Expedientes_GetEstadisticasByNegociadorId` |

## 5. Defectos de seguridad encontrados en Monitorio

| # | Hallazgo | Acción |
|---|---|---|
| 1 | `App.config` con usuario y contraseña de administrador en texto plano, sincronizado en Google Drive | **Inmediata**: cambiar esa contraseña donde exista y quitarla del archivo y de las copias |
| 2 | Contraseñas con cifrado reversible | No se migran; reactivación por invitación |
| 3 | Login con SQL concatenado (comilla faltante) y `catch` vacío | ASP.NET Core Identity |
| 4 | `GetPaged` y `GetExpedientes` con SQL dinámico concatenado | Consultas parametrizadas |
| 5 | `frmRunScripts` ejecuta SQL arbitrario | Sin equivalente |
| 6 | Cadenas de conexión con servidores internos; credenciales de terceros en la base | Secretos en almacén seguro |
| 7 | `Expediente → Procurador` con `ON DELETE CASCADE` | Sin cascadas; baja lógica |
| 8 | Nombres de funcionarios y textos de firma en `App.config` | En `conf.Profesional` y plantillas |
| 9 | Office Interop | OpenXML |

## 6. Cómo se usa Monitorio en el proyecto

### 6.1 Casos de referencia (antes de programar el motor de liquidación)

1. Elegir con el estudio ~50 expedientes por comitente/concepto (con y sin tasa pasiva, uno o dos profesionales, períodos antes y después de 2014, Capital e Interior).
2. Generar sus planillas en Monitorio y guardarlas como casos de referencia.
3. El motor de Procu360 los reproduce al centavo, salvo los defectos de §4.1, que el estudio aprueba caso por caso.
4. Igual para escritos: 20 por plantilla clave, comparados lado a lado.

### 6.2 Migración de datos

1. Obtener un backup actual de la base Monitorio.
2. Cargar en staging sin transformar.
3. Transformar con el mapeo del doc. 03 §7 (proyecto `Procu360.Migracion`), guardando `LegadoId` y registrando problemas en `mig.DatoARevisar`.
4. Ensayo 1 en homologación → revisión → ensayo 2 con conteos y totales conciliados.
5. Corte: Monitorio en solo lectura, migración final, conciliación, invitación de usuarios.
6. Monitorio disponible en solo lectura por un período acordado (sugerido: 6 meses).

### 6.3 Plantillas de escritos

Inventariar las carpetas de plantillas, descartar las que no se usan, convertir a DOCX y renombrar los campos con la tabla de equivalencias. Planificarlo como tarea propia del estudio con desarrollo.

## 7. Estructura de la nueva solución

```
Procu360.sln
├─ src/
│  ├─ Procu360.Web              Host ASP.NET Core: API + UI, autenticación, autorización
│  ├─ Procu360.Dominio          Entidades, reglas y casos de uso por módulo
│  ├─ Procu360.Infraestructura  EF Core + RLS, archivos, email, jobs, OpenXML
│  └─ Procu360.Migracion        ETL Monitorio → Procu360 (consola)
├─ db/
│  ├─ sql/                      01-esquema, 02-datos-prueba, 03-seguridad-rls
│  └─ gen/                      Definición del modelo y generador de datos de prueba
└─ tests/
   ├─ Procu360.Liquidacion.Tests   Casos de referencia de Monitorio (§6.1)
   ├─ Procu360.Integracion.Tests   SQL Server real en contenedor, con 02-datos-prueba
   ├─ Procu360.Aislamiento.Tests   Un Estudio nunca ve ni modifica datos de otro
   └─ Procu360.E2E                 Flujos principales en navegador (Playwright)
```

El motor de liquidación vive en `Dominio`, sin dependencias de base de datos ni de la fecha del sistema: recibe datos y parámetros, devuelve líneas. Los datos de prueba (doc. 03 §9) sirven de base para las pruebas de integración, de aislamiento y E2E (los usuarios cubren todos los roles y estados de membresía).

## 8. Plan por fases

| Fase | Contenido | Criterio de salida |
|---|---|---|
| 0. Preparación | Acción de seguridad §5.1; backup de la base Monitorio; casos de referencia (§6.1) | Casos aprobados por el estudio |
| 1. Base | Esquema y datos de prueba en SQL Server, tenancy, Identity, roles y permisos, auditoría con retención, catálogos y copia entre Estudios, entornos y pipeline | Pruebas de aislamiento en verde |
| 2. Expedientes | Ficha, búsqueda unificada, períodos, personas, etapas, tareas, novedades, Mi día, ETL ensayo 1 | Ensayo 1 conciliado |
| 3. Liquidación y escritos | Motor parametrizado, cálculo individual, plantillas DOCX, escritos individuales y masivos, Datos a revisar | Casos de referencia al centavo |
| 4. Puesta en producción | Ensayo 2, capacitación, corte | Conciliación final firmada |
| 5. Segunda fase | Liquidación masiva, tableros, mejoras de embargos y planes de pago | — |

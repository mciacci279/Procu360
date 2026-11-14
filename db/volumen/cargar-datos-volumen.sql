/*
  Datos de volumen para probar paginación, filtros e indicadores de carga.

  SOLO para Procu360_Dev. No tocar Procu360_Test: las pruebas de integración esperan
  el conjunto chico y conocido del seed.

  Todo lo que crea queda marcado con Observaciones = 'VOLUMEN' (expedientes) o el prefijo
  'VOLUMEN-' (personas y tareas), así que se borra con borrar-datos-volumen.sql.
  Es idempotente: si ya hay datos de volumen, no agrega más.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @estudio int = 1;
DECLARE @cantidad int = 2000;   -- expedientes a generar

EXEC sys.sp_set_session_context @key = N'EstudioId', @value = @estudio;

IF EXISTS (SELECT 1 FROM dbo.Expediente WHERE EstudioId = @estudio AND Observaciones = 'VOLUMEN')
BEGIN
    PRINT 'Ya hay datos de volumen cargados. Ejecutá antes borrar-datos-volumen.sql.';
    RETURN;
END

-- Catálogos existentes, para que los datos sean coherentes con el Estudio.
DECLARE @comitentes TABLE (fila int IDENTITY(0,1), Id int);
INSERT INTO @comitentes (Id) SELECT Id FROM conf.Comitente WHERE EstudioId = @estudio AND Activo = 1 ORDER BY Id;

DECLARE @conceptos TABLE (fila int IDENTITY(0,1), Id int);
INSERT INTO @conceptos (Id) SELECT Id FROM conf.ConceptoDeuda WHERE EstudioId = @estudio ORDER BY Id;

DECLARE @juzgados TABLE (fila int IDENTITY(0,1), Id int);
INSERT INTO @juzgados (Id) SELECT Id FROM conf.Juzgado WHERE EstudioId = @estudio AND Activo = 1 ORDER BY Id;

DECLARE @procuradores TABLE (fila int IDENTITY(0,1), Id int);
INSERT INTO @procuradores (Id) SELECT Id FROM conf.Profesional WHERE EstudioId = @estudio AND EsProcurador = 1 ORDER BY Id;

-- Etapas del flujo principal, en orden: definen también la instancia del expediente.
DECLARE @etapas TABLE (fila int IDENTITY(0,1), Id int, InstanciaId int, Dias int, DiasHabiles bit);
INSERT INTO @etapas (Id, InstanciaId, Dias, DiasHabiles)
SELECT Id, InstanciaId, Dias, DiasHabiles FROM conf.Etapa
WHERE EstudioId = @estudio AND Flujo = 1 AND Activa = 1 ORDER BY Orden;

DECLARE @usuarios TABLE (fila int IDENTITY(0,1), Id int);
INSERT INTO @usuarios (Id) SELECT UsuarioId FROM seg.EstudioUsuario
WHERE EstudioId = @estudio AND Estado = 3 ORDER BY UsuarioId;

DECLARE @nComit int = (SELECT COUNT(*) FROM @comitentes);
DECLARE @nConc  int = (SELECT COUNT(*) FROM @conceptos);
DECLARE @nJuz   int = (SELECT COUNT(*) FROM @juzgados);
DECLARE @nProc  int = (SELECT COUNT(*) FROM @procuradores);
DECLARE @nEtapa int = (SELECT COUNT(*) FROM @etapas);
DECLARE @nUsu   int = (SELECT COUNT(*) FROM @usuarios);

IF @nComit = 0 OR @nConc = 0 OR @nEtapa = 0 OR @nUsu = 0
BEGIN
    RAISERROR('Faltan catálogos en el Estudio %d: cargá primero los datos de prueba.', 16, 1, @estudio);
    RETURN;
END

-- Apellidos y calles para armar nombres y domicilios verosímiles.
DECLARE @apellidos TABLE (fila int IDENTITY(0,1), Texto nvarchar(50));
INSERT INTO @apellidos (Texto) VALUES
 (N'Gómez'),(N'Rodríguez'),(N'Fernández'),(N'López'),(N'Martínez'),(N'Pérez'),(N'Sánchez'),
 (N'Romero'),(N'Sosa'),(N'Torres'),(N'Álvarez'),(N'Ruiz'),(N'Benítez'),(N'Acosta'),(N'Medina'),
 (N'Herrera'),(N'Aguirre'),(N'Pereyra'),(N'Gutiérrez'),(N'Molina'),(N'Castro'),(N'Ortiz'),
 (N'Silva'),(N'Núñez'),(N'Luna'),(N'Juárez'),(N'Cabrera'),(N'Ríos'),(N'Morales'),(N'Vega');

DECLARE @nombres TABLE (fila int IDENTITY(0,1), Texto nvarchar(50));
INSERT INTO @nombres (Texto) VALUES
 (N'María'),(N'Juan'),(N'Ana'),(N'Carlos'),(N'Laura'),(N'Diego'),(N'Sofía'),(N'Martín'),
 (N'Valentina'),(N'Pablo'),(N'Lucía'),(N'Jorge'),(N'Camila'),(N'Gabriel'),(N'Florencia');

DECLARE @calles TABLE (fila int IDENTITY(0,1), Texto nvarchar(60));
INSERT INTO @calles (Texto) VALUES
 (N'San Martín'),(N'Belgrano'),(N'Rivadavia'),(N'Sarmiento'),(N'Colón'),(N'Independencia'),
 (N'25 de Mayo'),(N'Entre Ríos'),(N'Bv. Illia'),(N'Av. Vélez Sarsfield');

DECLARE @nApe int = (SELECT COUNT(*) FROM @apellidos);
DECLARE @nNom int = (SELECT COUNT(*) FROM @nombres);
DECLARE @nCal int = (SELECT COUNT(*) FROM @calles);

DECLARE @hoy date = CAST(SYSDATETIME() AS date);
DECLARE @desde int = ISNULL((SELECT MAX(NroOrden) FROM dbo.Expediente WHERE EstudioId = @estudio), 0);

BEGIN TRANSACTION;

-- Serie 1..@cantidad sin bucles: más rápido que insertar de a uno.
WITH numeros AS (
    SELECT TOP (@cantidad) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO dbo.Expediente (
    EstudioId, NroOrden, ComitenteId, ConceptoDeudaId, NroLiquidacion, FechaTitulo, FechaAlta,
    MontoTitulo, MontoCapital, NroCuenta, Nomenclatura, Dominio, DomicilioObjeto,
    JuzgadoId, NroExpedienteJudicial, Caratula, ProcuradorId,
    EtapaActualId, FechaEtapaActual, InstanciaActualId, FechaInstanciaActual,
    FechaUltimoMovimiento, Comparecio, EsGranContribuyente, EsFisca,
    Observaciones, Anulado, CreadoUtc, CreadoPor)
SELECT
    @estudio,
    @desde + n,
    (SELECT Id FROM @comitentes WHERE fila = n % @nComit),
    (SELECT Id FROM @conceptos  WHERE fila = n % @nConc),
    'LIQ-' + RIGHT('000000' + CAST(@desde + n AS varchar(10)), 6),
    DATEADD(day, -(n % 900) - 30, @hoy),
    DATEADD(day, -(n % 900), @hoy),
    CAST(15000 + (n * 137) % 900000 AS decimal(18,2)),
    CAST(12000 + (n * 113) % 700000 AS decimal(18,2)),
    CAST(100000 + (n * 7) AS varchar(20)),
    '11-01-' + RIGHT('0000' + CAST(n % 9999 AS varchar(4)), 4) + '-001',
    CASE WHEN n % 11 = 0 THEN 'AAA' + RIGHT('000' + CAST(n % 999 AS varchar(3)), 3) END,
    (SELECT Texto FROM @calles WHERE fila = n % @nCal) + ' ' + CAST(100 + (n * 3) % 3800 AS varchar(10)),
    -- Dos tercios judicializados: el resto queda en etapa pre judicial.
    CASE WHEN n % 3 <> 0 AND @nJuz > 0 THEN (SELECT Id FROM @juzgados WHERE fila = n % @nJuz) END,
    CASE WHEN n % 3 <> 0 THEN CAST(1000000 + n AS varchar(12)) + '/' + CAST(2020 + (n % 6) AS varchar(4)) END,
    CASE WHEN n % 3 <> 0 THEN
        (SELECT Texto FROM @comitentes c JOIN conf.Comitente cc ON cc.Id = c.Id
         CROSS APPLY (SELECT cc.Nombre AS Texto) x WHERE c.fila = n % @nComit)
        + ' c/ ' + (SELECT Texto FROM @apellidos WHERE fila = n % @nApe)
        + ', ' + (SELECT Texto FROM @nombres WHERE fila = n % @nNom)
        + ' - Ejecución fiscal' END,
    CASE WHEN @nProc > 0 THEN (SELECT Id FROM @procuradores WHERE fila = n % @nProc) END,
    (SELECT Id FROM @etapas WHERE fila = n % @nEtapa),
    DATEADD(day, -(n % 200), @hoy),
    (SELECT InstanciaId FROM @etapas WHERE fila = n % @nEtapa),
    DATEADD(day, -(n % 200), @hoy),
    -- Uno de cada siete queda sin movimiento reciente, para probar ese filtro.
    CASE WHEN n % 7 = 0 THEN DATEADD(day, -120 - (n % 240), @hoy) ELSE DATEADD(day, -(n % 85), @hoy) END,
    CASE WHEN n % 9 = 0 THEN 1 ELSE 0 END,
    CASE WHEN n % 23 = 0 THEN 1 ELSE 0 END,
    CASE WHEN n % 31 = 0 THEN 1 ELSE 0 END,
    'VOLUMEN',
    CASE WHEN n % 47 = 0 THEN 1 ELSE 0 END,   -- algunos dados de baja
    SYSUTCDATETIME(),
    1
FROM numeros;

PRINT 'Expedientes creados: ' + CAST(@@ROWCOUNT AS varchar(10));

-- Una persona demandada por expediente.
INSERT INTO dbo.Persona (EstudioId, EsPersonaFisica, Apellido, Nombre, RazonSocial,
    TipoDocumentoId, NroDocumento, Cuit, Telefono1, Email, Observaciones, Anulado, CreadoUtc, CreadoPor)
SELECT
    @estudio,
    CASE WHEN e.NroOrden % 13 = 0 THEN 0 ELSE 1 END,
    CASE WHEN e.NroOrden % 13 <> 0 THEN (SELECT Texto FROM @apellidos WHERE fila = e.NroOrden % @nApe) END,
    CASE WHEN e.NroOrden % 13 <> 0 THEN (SELECT Texto FROM @nombres WHERE fila = e.NroOrden % @nNom) END,
    CASE WHEN e.NroOrden % 13 = 0 THEN
        (SELECT Texto FROM @apellidos WHERE fila = e.NroOrden % @nApe) + ' e Hijos S.R.L.' END,
    1,
    CAST(10000000 + (e.NroOrden * 997) % 40000000 AS varchar(20)),
    '20-' + CAST(10000000 + (e.NroOrden * 997) % 40000000 AS varchar(20)) + '-3',
    '351' + CAST(4000000 + (e.NroOrden * 31) % 999999 AS varchar(10)),
    'contacto' + CAST(e.NroOrden AS varchar(10)) + '@ejemplo.test',
    'VOLUMEN',
    0, SYSUTCDATETIME(), 1
FROM dbo.Expediente e
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN';

PRINT 'Personas creadas: ' + CAST(@@ROWCOUNT AS varchar(10));

-- Vincula cada persona a su expediente como demandada (rol 1).
INSERT INTO dbo.ExpedientePersona (EstudioId, ExpedienteId, PersonaId, Rol, Posicion, CreadoUtc, CreadoPor)
SELECT @estudio, e.Id, p.Id, 1, 1, SYSUTCDATETIME(), 1
FROM dbo.Expediente e
CROSS APPLY (
    SELECT TOP 1 pe.Id FROM dbo.Persona pe
    WHERE pe.EstudioId = @estudio AND pe.Observaciones = 'VOLUMEN'
      AND pe.Email = 'contacto' + CAST(e.NroOrden AS varchar(10)) + '@ejemplo.test'
) p
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN';

PRINT 'Vínculos persona-expediente: ' + CAST(@@ROWCOUNT AS varchar(10));

-- Entre 2 y 6 períodos adeudados por expediente.
WITH cuotas AS (SELECT * FROM (VALUES (1),(2),(3),(4),(5),(6)) AS c(n))
INSERT INTO dbo.PeriodoAdeudado (EstudioId, ExpedienteId, Anio, Cuota, Concepto, Vencimiento,
    Tributo, Recargo, Observaciones, CreadoUtc, CreadoPor)
SELECT
    @estudio, e.Id,
    CAST(2019 + ((e.NroOrden + c.n) % 6) AS smallint),
    RIGHT('0' + CAST(c.n AS varchar(2)), 2),
    'VOLUMEN',
    DATEADD(day, -(e.NroOrden % 700) - c.n * 30, @hoy),
    CAST(1500 + ((e.NroOrden * c.n * 37) % 45000) AS decimal(18,2)),
    CAST(((e.NroOrden * c.n * 11) % 9000) AS decimal(18,2)),
    NULL, SYSUTCDATETIME(), 1
FROM dbo.Expediente e
JOIN cuotas c ON c.n <= 2 + (e.NroOrden % 5)
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN';

PRINT 'Períodos creados: ' + CAST(@@ROWCOUNT AS varchar(10));

-- El capital del título sigue a la suma de períodos, como hace la aplicación.
UPDATE e SET e.MontoCapital = s.Suma
FROM dbo.Expediente e
JOIN (SELECT ExpedienteId, SUM(Tributo + Recargo) AS Suma FROM dbo.PeriodoAdeudado
      WHERE EstudioId = @estudio GROUP BY ExpedienteId) s ON s.ExpedienteId = e.Id
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN';

-- Historial de etapas: todas las anteriores a la actual quedan cumplidas, y la actual en alta.
INSERT INTO dbo.EtapaExpediente (EstudioId, ExpedienteId, EtapaId, FechaInicio, FechaFin,
    FechaVencimiento, Estado, Observaciones, CreadoUtc, CreadoPor)
SELECT
    @estudio, e.Id, et.Id,
    DATEADD(day, -(e.NroOrden % 200) - (@nEtapa - et.fila) * 25, @hoy),
    CASE WHEN et.fila < e.NroOrden % @nEtapa
         THEN DATEADD(day, -(e.NroOrden % 200) - (@nEtapa - et.fila - 1) * 25, @hoy) END,
    DATEADD(day, -(e.NroOrden % 200) - (@nEtapa - et.fila) * 25 + ISNULL(et.Dias, 15), @hoy),
    CASE WHEN et.fila < e.NroOrden % @nEtapa THEN 2 ELSE 1 END,
    'VOLUMEN',
    SYSUTCDATETIME(), 1
FROM dbo.Expediente e
JOIN @etapas et ON et.fila <= e.NroOrden % @nEtapa
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN';

PRINT 'Etapas de expediente creadas: ' + CAST(@@ROWCOUNT AS varchar(10));

-- Historial de instancias, coherente con la etapa actual.
INSERT INTO dbo.InstanciaExpediente (EstudioId, ExpedienteId, InstanciaId, Fecha, CreadoUtc, CreadoPor)
SELECT @estudio, e.Id, e.InstanciaActualId, e.FechaInstanciaActual, SYSUTCDATETIME(), 1
FROM dbo.Expediente e
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN'
  AND e.InstanciaActualId IS NOT NULL AND e.FechaInstanciaActual IS NOT NULL;

-- Tareas: una cada dos expedientes, repartidas entre pendientes, vencidas y cumplidas.
INSERT INTO dbo.Tarea (EstudioId, ExpedienteId, Titulo, Descripcion, FechaInicio, FechaVencimiento,
    ResponsableUsuarioId, VisibleParaTodos, Estado, Origen, CumplidaUtc, CreadoUtc, CreadoPor)
SELECT
    @estudio, e.Id,
    'VOLUMEN-' + CASE e.NroOrden % 5
        WHEN 0 THEN 'Revisar cédula'
        WHEN 1 THEN 'Presentar planilla'
        WHEN 2 THEN 'Controlar vencimiento'
        WHEN 3 THEN 'Pedir informe de dominio'
        ELSE 'Llamar al comitente' END
      + ' (exp. ' + CAST(e.NroOrden AS varchar(10)) + ')',
    NULL,
    DATEADD(day, -(e.NroOrden % 60), @hoy),
    -- Un tercio ya vencidas, para que se vean en Mi día.
    CASE WHEN e.NroOrden % 3 = 0 THEN DATEADD(day, -(e.NroOrden % 25) - 1, @hoy)
         ELSE DATEADD(day, (e.NroOrden % 40) + 1, @hoy) END,
    (SELECT Id FROM @usuarios WHERE fila = e.NroOrden % @nUsu),
    CASE WHEN e.NroOrden % 4 = 0 THEN 1 ELSE 0 END,
    CASE WHEN e.NroOrden % 6 = 0 THEN 2 ELSE 1 END,   -- 2 cumplida, 1 pendiente
    1,
    CASE WHEN e.NroOrden % 6 = 0 THEN SYSUTCDATETIME() END,
    SYSUTCDATETIME(), 1
FROM dbo.Expediente e
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN' AND e.NroOrden % 2 = 0;

PRINT 'Tareas creadas: ' + CAST(@@ROWCOUNT AS varchar(10));

-- Reparte los vencimientos por Id de la tarea: NroOrden no sirve porque las tareas son de
-- expedientes pares y dejaría casi todas en el mismo grupo.
UPDATE dbo.Tarea SET FechaVencimiento =
    CASE (Id % 3)
        WHEN 0 THEN DATEADD(day, -(CAST(Id % 30 AS int)) - 1, @hoy)   -- vencidas
        WHEN 1 THEN DATEADD(day, CAST(Id % 7 AS int), @hoy)            -- esta semana
        ELSE DATEADD(day, CAST(Id % 45 AS int) + 8, @hoy)              -- más adelante
    END
WHERE EstudioId = @estudio AND Titulo LIKE 'VOLUMEN-%' AND Estado = 1;

-- Novedades judiciales: una cada cinco expedientes.
INSERT INTO dbo.NovedadJudicial (EstudioId, ExpedienteId, Fecha, Texto, Fuente, Leida, CreadoUtc, CreadoPor)
SELECT
    @estudio, e.Id, DATEADD(day, -(e.NroOrden % 30), @hoy),
    'VOLUMEN - Proveído del ' + CONVERT(varchar(10), DATEADD(day, -(e.NroOrden % 30), @hoy), 103)
      + ': se provee lo solicitado en autos ' + ISNULL(e.NroExpedienteJudicial, CAST(e.NroOrden AS varchar(10))) + '.',
    1,
    CASE WHEN e.NroOrden % 3 = 0 THEN 1 ELSE 0 END,
    SYSUTCDATETIME(), 1
FROM dbo.Expediente e
WHERE e.EstudioId = @estudio AND e.Observaciones = 'VOLUMEN' AND e.NroOrden % 5 = 0;

PRINT 'Novedades creadas: ' + CAST(@@ROWCOUNT AS varchar(10));

COMMIT;

-- Resumen final.
SELECT 'Expedientes' AS Tabla, COUNT(*) AS Total FROM dbo.Expediente WHERE EstudioId = @estudio
UNION ALL SELECT 'Personas',   COUNT(*) FROM dbo.Persona           WHERE EstudioId = @estudio
UNION ALL SELECT 'Períodos',   COUNT(*) FROM dbo.PeriodoAdeudado   WHERE EstudioId = @estudio
UNION ALL SELECT 'Etapas',     COUNT(*) FROM dbo.EtapaExpediente   WHERE EstudioId = @estudio
UNION ALL SELECT 'Tareas',     COUNT(*) FROM dbo.Tarea             WHERE EstudioId = @estudio
UNION ALL SELECT 'Novedades',  COUNT(*) FROM dbo.NovedadJudicial   WHERE EstudioId = @estudio;

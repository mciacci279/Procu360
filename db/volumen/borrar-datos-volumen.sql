/*
  Borra los datos de volumen y deja Procu360_Dev con el seed original.

  Solo toca lo que creó cargar-datos-volumen.sql: expedientes con Observaciones = 'VOLUMEN',
  sus personas, períodos, etapas, tareas y novedades. Los 13 expedientes del seed no se tocan.
  La auditoría no se borra: es de solo escritura.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @estudio int = 1;
EXEC sys.sp_set_session_context @key = N'EstudioId', @value = @estudio;

DECLARE @expedientes TABLE (Id bigint PRIMARY KEY);
INSERT INTO @expedientes (Id)
SELECT Id FROM dbo.Expediente WHERE EstudioId = @estudio AND Observaciones = 'VOLUMEN';

IF NOT EXISTS (SELECT 1 FROM @expedientes)
BEGIN
    PRINT 'No hay datos de volumen para borrar.';
    RETURN;
END

DECLARE @personas TABLE (Id bigint PRIMARY KEY);
INSERT INTO @personas (Id)
SELECT Id FROM dbo.Persona WHERE EstudioId = @estudio AND Observaciones = 'VOLUMEN';

BEGIN TRANSACTION;

-- En orden de dependencia: primero lo que apunta al expediente.
DELETE FROM dbo.NovedadJudicial  WHERE ExpedienteId IN (SELECT Id FROM @expedientes);
DELETE FROM dbo.Tarea            WHERE ExpedienteId IN (SELECT Id FROM @expedientes);
DELETE FROM dbo.InstanciaExpediente WHERE ExpedienteId IN (SELECT Id FROM @expedientes);
DELETE FROM dbo.EtapaExpediente  WHERE ExpedienteId IN (SELECT Id FROM @expedientes);
DELETE FROM dbo.PeriodoAdeudado  WHERE ExpedienteId IN (SELECT Id FROM @expedientes);
DELETE FROM dbo.ExpedientePersona WHERE ExpedienteId IN (SELECT Id FROM @expedientes);

-- La etapa y la instancia actuales apuntan a catálogos, no hace falta limpiarlas antes.
DELETE FROM dbo.Expediente WHERE Id IN (SELECT Id FROM @expedientes);

-- Las personas de volumen, ya sin vínculos.
DELETE FROM dbo.Domicilio WHERE PersonaId IN (SELECT Id FROM @personas);
DELETE FROM dbo.Persona   WHERE Id IN (SELECT Id FROM @personas);

COMMIT;

PRINT 'Datos de volumen borrados.';

SELECT 'Expedientes' AS Tabla, COUNT(*) AS Total FROM dbo.Expediente WHERE EstudioId = @estudio
UNION ALL SELECT 'Personas',  COUNT(*) FROM dbo.Persona         WHERE EstudioId = @estudio
UNION ALL SELECT 'Períodos',  COUNT(*) FROM dbo.PeriodoAdeudado WHERE EstudioId = @estudio
UNION ALL SELECT 'Tareas',    COUNT(*) FROM dbo.Tarea           WHERE EstudioId = @estudio;

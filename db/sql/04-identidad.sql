-- Procu360 - Identidad: consulta de membresías en otros Estudios para invitaciones.
-- Idempotente: se puede ejecutar sobre una base ya cargada con 01 a 03.
-- La aplicación no puede leer membresías de otros Estudios (RLS). Para la regla
-- "el email ya pertenece a otro Estudio" se usa un procedimiento acotado que corre con un
-- principal sin login, miembro de rol_plataforma, y devuelve solo un bit.
IF DATABASE_PRINCIPAL_ID(N'usuario_plataforma') IS NULL CREATE USER usuario_plataforma WITHOUT LOGIN;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members m
                 JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
                 JOIN sys.database_principals u ON u.principal_id = m.member_principal_id
                WHERE r.name = N'rol_plataforma' AND u.name = N'usuario_plataforma')
    ALTER ROLE rol_plataforma ADD MEMBER usuario_plataforma;
GO
GRANT SELECT ON seg.EstudioUsuario TO rol_plataforma;
GO
CREATE OR ALTER PROCEDURE seg.usp_TieneMembresiaActivaEnOtroEstudio
    @UsuarioId int, @EstudioId int, @Existe bit OUTPUT
WITH EXECUTE AS 'usuario_plataforma'
AS
BEGIN
    SET NOCOUNT ON;
    SET @Existe = CASE WHEN EXISTS (
        SELECT 1 FROM seg.EstudioUsuario
         WHERE UsuarioId = @UsuarioId AND Estado = 3 AND EstudioId <> @EstudioId)
        THEN 1 ELSE 0 END;
END
GO
GRANT EXECUTE ON seg.usp_TieneMembresiaActivaEnOtroEstudio TO rol_aplicacion;
GO

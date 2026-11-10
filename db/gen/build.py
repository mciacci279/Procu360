"""Genera los scripts SQL de Procu360 y valida los datos de prueba en una copia SQLite.

Salida (carpeta ../sql):
  01-esquema.sql          esquemas, tablas, claves, índices, roles, procedimientos
  02-datos-prueba.sql     datos de prueba de dos Estudios (todas las tablas)
  03-seguridad-rls.sql    Row-Level Security por Estudio (ejecutar después de cargar datos)
"""
import sqlite3
import sys
from collections import Counter, defaultdict
from datetime import date, datetime, time
from decimal import Decimal
from pathlib import Path

import esquema as S
import datos as Dt

OUT = Path(__file__).resolve().parent.parent / "sql"
OUT.mkdir(exist_ok=True)
FECHA_DEFAULT = "2026-11-09T12:00:00"


# ------------------------------------------------------------------ utilidades
def fk_target(t, col, ref):
    """Devuelve (cols origen, tabla destino, cols destino)."""
    r = S.POR_NOMBRE[ref]
    if t.tenant and r.tenant and col != "EstudioId":
        return ("EstudioId", col), r, ("EstudioId", r.pk[-1])
    return (col,), r, (r.pk[-1],)


def self_ref_cols(t):
    return [c for c, ref in t.fks if ref == t.full]


def nombre_obj(prefijo, t, cols):
    return f"{prefijo}_{t.schema}_{t.nombre}_{'_'.join(cols)}"[:128]


def default_literal(expr):
    if expr is None:
        return None
    if expr == "SYSUTCDATETIME()":
        return FECHA_DEFAULT
    if expr.startswith("N'"):
        return expr[2:-1]
    return Decimal(expr) if "." in expr else int(expr)


# ------------------------------------------------------------------ DDL SQL Server
def ddl_tsql():
    o = ["-- Procu360 - modelo de datos (SQL Server 2019+ / Azure SQL)",
         "-- Generado desde gen/esquema.py: no editar a mano.",
         "SET ANSI_NULLS ON;", "SET QUOTED_IDENTIFIER ON;", "GO", ""]
    for sc in ["seg", "cat", "conf", "liq", "aud", "mig"]:
        o += [f"IF SCHEMA_ID(N'{sc}') IS NULL EXEC(N'CREATE SCHEMA {sc}');", "GO"]
    o.append("")
    for t in S.TABLAS:
        o.append(f"CREATE TABLE {t.full} (")
        lines = []
        for n, tipo, nul, de in t.cols:
            s = f"    {n} {tipo}"
            if tipo != "rowversion":
                s += " NULL" if nul else " NOT NULL"
            if de:
                s += f" CONSTRAINT DF_{t.schema}_{t.nombre}_{n} DEFAULT ({de})"
            lines.append(s)
        lines.append(f"    CONSTRAINT PK_{t.schema}_{t.nombre} PRIMARY KEY CLUSTERED ({', '.join(t.pk)})")
        if t.tenant and t.pk == ["Id"]:
            lines.append(f"    CONSTRAINT UQ_{t.schema}_{t.nombre}_EstudioId_Id UNIQUE (EstudioId, Id)")
        for u in t.unicos:
            cols = (("EstudioId",) if t.tenant else ()) + tuple(u)
            lines.append(f"    CONSTRAINT {nombre_obj('UQ', t, cols)} UNIQUE ({', '.join(cols)})")
        for i, (expr, _) in enumerate(t.checks):
            lines.append(f"    CONSTRAINT CK_{t.schema}_{t.nombre}_{i + 1} CHECK ({expr})")
        o.append(",\n".join(lines))
        o += [");", "GO"]
        if t.desc or t.origen:
            txt = (t.desc + (" " if t.desc else "") + (f"Origen Monitorio: {t.origen}." if t.origen else "")).replace("'", "''")
            o += [f"EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'{txt}', "
                  f"@level0type = N'SCHEMA', @level0name = N'{t.schema}', @level1type = N'TABLE', @level1name = N'{t.nombre}';", "GO"]
    o += ["", "-- ---------------------------------------------------------------- claves foráneas"]
    for t in S.TABLAS:
        if t.tenant:
            o.append(f"ALTER TABLE {t.full} ADD CONSTRAINT FK_{t.schema}_{t.nombre}_Estudio FOREIGN KEY (EstudioId) REFERENCES seg.Estudio (Id);")
        for col, ref in t.fks:
            src, r, dst = fk_target(t, col, ref)
            o.append(f"ALTER TABLE {t.full} ADD CONSTRAINT FK_{t.schema}_{t.nombre}_{col} FOREIGN KEY ({', '.join(src)}) "
                     f"REFERENCES {r.full} ({', '.join(dst)});")
    o += ["GO", "", "-- ---------------------------------------------------------------- índices"]
    for t in S.TABLAS:
        for ix in t.indices:
            cols, inc, filtro = ix[:3]
            unico = len(ix) > 3 and ix[3]
            nombre = nombre_obj("UX" if unico else "IX", t, cols) + ("_Principal" if unico else "")
            s = f"CREATE {'UNIQUE ' if unico else ''}NONCLUSTERED INDEX {nombre} ON {t.full} ({', '.join(cols)})"
            if inc:
                s += f" INCLUDE ({', '.join(inc)})"
            if filtro:
                s += f" WHERE {filtro}"
            o.append(s + ";")
        for col, ref in t.fks:            # índice de apoyo a cada FK que no esté cubierta
            src, _, _ = fk_target(t, col, ref)
            cubiertas = [tuple(ix[0][:len(src)]) for ix in t.indices] + [tuple(t.pk[:len(src)])] + \
                        [(("EstudioId",) if t.tenant else ()) + tuple(u)[:len(src) - (1 if t.tenant else 0)] for u in t.unicos]
            if tuple(src) not in cubiertas:
                o.append(f"CREATE NONCLUSTERED INDEX {nombre_obj('IX', t, src)} ON {t.full} ({', '.join(src)});")
        if t.legado:
            cols = ("EstudioId", "LegadoId") if t.tenant else ("LegadoId",)
            o.append(f"CREATE UNIQUE NONCLUSTERED INDEX UX_{t.schema}_{t.nombre}_LegadoId ON {t.full} ({', '.join(cols)}) "
                     f"WHERE LegadoId IS NOT NULL;")
    o += ["GO", "", PROCS]
    return "\n".join(o) + "\n"


PROCS = r"""-- ---------------------------------------------------------------- roles de base de datos
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
"""


IDENTIDAD = r"""-- Procu360 - Identidad: consulta de membresías en otros Estudios para invitaciones.
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
"""


def identidad_tsql():
    return IDENTIDAD


def rls_tsql():
    tablas = [t for t in S.TABLAS if t.tenant] + [S.POR_NOMBRE["aud.AuditoriaEvento"]]
    o = ["-- Procu360 - Row-Level Security por Estudio. Ejecutar DESPUÉS de cargar datos.",
         "-- La aplicación fija el Estudio activo al abrir cada conexión:",
         "--   EXEC sp_set_session_context @key = N'EstudioId', @value = @id, @read_only = 1;",
         "-- Los procesos de plataforma (migración, purga, alta de Estudios) usan un usuario miembro de rol_plataforma.",
         "GO",
         "CREATE OR ALTER FUNCTION seg.fn_FiltroEstudio (@EstudioId int)",
         "RETURNS TABLE WITH SCHEMABINDING AS",
         "RETURN SELECT 1 AS Permitido",
         "       WHERE @EstudioId = CAST(SESSION_CONTEXT(N'EstudioId') AS int)",
         "          OR IS_ROLEMEMBER(N'rol_plataforma') = 1;",
         "GO",
         "-- Membresías: antes de elegir Estudio, el usuario necesita ver las suyas en todos los Estudios.",
         "-- La aplicación fija también SESSION_CONTEXT('UsuarioId') al autenticar.",
         "CREATE OR ALTER FUNCTION seg.fn_FiltroMembresia (@EstudioId int, @UsuarioId int)",
         "RETURNS TABLE WITH SCHEMABINDING AS",
         "RETURN SELECT 1 AS Permitido",
         "       WHERE @EstudioId = CAST(SESSION_CONTEXT(N'EstudioId') AS int)",
         "          OR @UsuarioId = CAST(SESSION_CONTEXT(N'UsuarioId') AS int)",
         "          OR IS_ROLEMEMBER(N'rol_plataforma') = 1;",
         "GO",
         "IF OBJECT_ID(N'seg.PoliticaEstudio') IS NOT NULL DROP SECURITY POLICY seg.PoliticaEstudio;",
         "GO",
         "CREATE SECURITY POLICY seg.PoliticaEstudio"]
    preds = []
    for t in tablas:
        if t.full == "seg.EstudioUsuario":
            preds.append(f"    ADD FILTER PREDICATE seg.fn_FiltroMembresia(EstudioId, UsuarioId) ON {t.full}")
        else:
            preds.append(f"    ADD FILTER PREDICATE seg.fn_FiltroEstudio(EstudioId) ON {t.full}")
        preds.append(f"    ADD BLOCK PREDICATE seg.fn_FiltroEstudio(EstudioId) ON {t.full} AFTER INSERT")
        if t.full != "aud.AuditoriaEvento":
            preds.append(f"    ADD BLOCK PREDICATE seg.fn_FiltroEstudio(EstudioId) ON {t.full} AFTER UPDATE")
    o.append(",\n".join(preds))
    o += ["WITH (STATE = ON, SCHEMABINDING = ON);", "GO"]
    return "\n".join(o) + "\n"


# ------------------------------------------------------------------ datos
def filas_completas(t):
    cols = [c for c in t.cols if c[1] != "rowversion"]
    nombres = [c[0] for c in cols]
    extra = set()
    filas = []
    for r in Dt.D.get(t.full, []):
        extra |= set(r) - set(nombres)
        fila = {}
        for n, tipo, nul, de in cols:
            if n in r and not (r[n] is None and not nul and de):
                fila[n] = r[n]
            elif not nul and de:
                fila[n] = default_literal(de)
            else:
                fila[n] = None
        filas.append(fila)
    if extra:
        raise SystemExit(f"{t.full}: columnas inexistentes en el esquema: {sorted(extra)}")
    return nombres, filas


def lit_tsql(v, tipo):
    if v is None:
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, Decimal)):
        return str(v)
    if isinstance(v, datetime):
        return f"'{v:%Y-%m-%dT%H:%M:%S}'"
    if isinstance(v, date):
        return f"'{v:%Y-%m-%d}'"
    s = str(v).replace("'", "''")
    if tipo.startswith(("nvarchar", "nchar")):
        return f"N'{s}'"
    return f"'{s}'"


def lit_sqlite(v):
    if v is None:
        return None
    if isinstance(v, bool):
        return int(v)
    if isinstance(v, Decimal):
        return str(v)
    if isinstance(v, datetime):
        return v.strftime("%Y-%m-%dT%H:%M:%S")
    if isinstance(v, date):
        return v.isoformat()
    return v


def datos_tsql():
    o = ["-- Procu360 - datos de prueba: 2 Estudios, todas las tablas. Datos ficticios.",
         "-- Ejecutar sobre una base creada con 01-esquema.sql y ANTES de 03-seguridad-rls.sql",
         "-- (o con un usuario de rol_plataforma).",
         "SET NOCOUNT ON;", "SET XACT_ABORT ON;", "BEGIN TRANSACTION;", ""]
    updates = []
    for t in S.TABLAS:
        nombres, filas = filas_completas(t)
        if not filas:
            continue
        auto = self_ref_cols(t)
        tipos = {c[0]: c[1] for c in t.cols}
        o.append(f"-- {t.full} ({len(filas)} filas)")
        ident = t.identidad is not None
        if ident:
            o.append(f"SET IDENTITY_INSERT {t.full} ON;")
        for i in range(0, len(filas), 500):
            o.append(f"INSERT INTO {t.full} ({', '.join(nombres)}) VALUES")
            vals = []
            for f in filas[i:i + 500]:
                vals.append("(" + ", ".join("NULL" if n in auto else lit_tsql(f[n], tipos[n]) for n in nombres) + ")")
            o.append(",\n".join(vals) + ";")
        if ident:
            o.append(f"SET IDENTITY_INSERT {t.full} OFF;")
        for f in filas:
            sets = [f"{n} = {lit_tsql(f[n], tipos[n])}" for n in auto if f[n] is not None]
            if sets:
                updates.append(f"UPDATE {t.full} SET {', '.join(sets)} WHERE Id = {f['Id']};")
        o.append("")
    o += ["-- referencias a la misma tabla (se cargan después para no depender del orden)"] + updates
    o += ["", "COMMIT TRANSACTION;", "",
          "-- Reajustar identidades",
          *[f"DBCC CHECKIDENT ('{t.full}', RESEED);" for t in S.TABLAS if t.identidad],
          "GO"]
    return "\n".join(o) + "\n"


# ------------------------------------------------------------------ validación en SQLite
def sqlite_tipo(tipo):
    t = tipo.lower()
    if t.startswith(("bit", "tinyint", "smallint", "int", "bigint")):
        return "INTEGER"
    if t.startswith("decimal"):
        return "NUMERIC"
    return "TEXT"


def validar_sqlite():
    db = sqlite3.connect(":memory:")
    db.execute("PRAGMA foreign_keys = ON")
    def q(t):
        return f'"{t.schema}_{t.nombre}"'
    for t in S.TABLAS:
        cols = []
        for n, tipo, nul, de in t.cols:
            if tipo == "rowversion":
                continue
            cols.append(f'{n} {sqlite_tipo(tipo)}{"" if nul else " NOT NULL"}')
        cols.append(f"PRIMARY KEY ({', '.join(t.pk)})")
        if t.tenant and t.pk == ["Id"]:
            cols.append("UNIQUE (EstudioId, Id)")
        for u in t.unicos:
            cols.append(f"UNIQUE ({', '.join((('EstudioId',) if t.tenant else ()) + tuple(u))})")
        for expr, solo_tsql in t.checks:
            if not solo_tsql and "ISJSON" not in expr:
                cols.append(f"CHECK ({expr})")
        if t.tenant:
            cols.append(f'FOREIGN KEY (EstudioId) REFERENCES "seg_Estudio" (Id)')
        for col, ref in t.fks:
            src, r, dst = fk_target(t, col, ref)
            cols.append(f"FOREIGN KEY ({', '.join(src)}) REFERENCES {q(r)} ({', '.join(dst)})")
        db.execute(f"CREATE TABLE {q(t)} ({', '.join(cols)})")
        for ix in t.indices:
            if len(ix) > 3 and ix[3]:
                db.execute(f"CREATE UNIQUE INDEX {nombre_obj('UX', t, ix[0])} ON {q(t)} ({', '.join(ix[0])}) WHERE {ix[2]}")
        if t.legado:
            cols_l = ("EstudioId", "LegadoId") if t.tenant else ("LegadoId",)
            db.execute(f"CREATE UNIQUE INDEX UX_{t.schema}_{t.nombre}_Leg ON {q(t)} ({', '.join(cols_l)}) WHERE LegadoId IS NOT NULL")
    conteo = {}
    for t in S.TABLAS:
        nombres, filas = filas_completas(t)
        auto = self_ref_cols(t)
        ph = ", ".join("?" * len(nombres))
        try:
            db.executemany(f"INSERT INTO {q(t)} ({', '.join(nombres)}) VALUES ({ph})",
                           [[None if n in auto else lit_sqlite(f[n]) for n in nombres] for f in filas])
            for f in filas:
                for n in auto:
                    if f[n] is not None:
                        db.execute(f"UPDATE {q(t)} SET {n} = ? WHERE Id = ?", (f[n], f["Id"]))
        except sqlite3.Error as e:
            raise SystemExit(f"Error al cargar {t.full}: {e}")
        conteo[t.full] = len(filas)
    fk = db.execute("PRAGMA foreign_key_check").fetchall()
    if fk:
        raise SystemExit(f"Violaciones de FK: {fk[:10]}")
    return db, conteo


def validar_negocio(db):
    errores = []
    def one(sql, *a):
        return db.execute(sql, a).fetchall()
    # 1. totales de liquidación = suma de líneas por rubro y total = suma de componentes
    for lid, tc, ti, tg, th, tot in one('SELECT Id, TotalCapital, TotalInteres, TotalGastos, HonorariosPesos, Total FROM "liq_Liquidacion"'):
        lineas = one('SELECT r.Nombre, l.Importe FROM "liq_LiquidacionLinea" l JOIN "liq_RubroPlanilla" r ON r.Id = l.RubroPlanillaId WHERE l.LiquidacionId = ?', lid)
        suma = defaultdict(Decimal)
        for rub, imp in lineas:
            suma[rub] += Decimal(str(imp))
        esp = {"Capital": tc, "Intereses": ti, "Gastos": tg, "Honorarios": th}
        for k, v in esp.items():
            if abs(suma[k] - Decimal(str(v))) > Decimal("0.005"):
                errores.append(f"Liquidación {lid}: rubro {k} {suma[k]} ≠ {v}")
        if abs(sum(Decimal(str(x)) for x in (tc, ti, tg, th)) - Decimal(str(tot))) > Decimal("0.005"):
            errores.append(f"Liquidación {lid}: total inconsistente")
    # 2. MontoCapital = suma de tributos
    for eid, cap, s in one('SELECT e.Id, e.MontoCapital, SUM(p.Tributo) FROM "dbo_Expediente" e JOIN "dbo_PeriodoAdeudado" p ON p.ExpedienteId = e.Id GROUP BY e.Id, e.MontoCapital'):
        if abs(Decimal(str(cap)) - Decimal(str(s))) > Decimal("0.01"):
            errores.append(f"Expediente {eid}: capital {cap} ≠ períodos {s}")
    # 3. etapa actual = última etapa del flujo principal
    for eid, act, ult in one('''SELECT e.Id, e.EtapaActualId,
            (SELECT ee.EtapaId FROM "dbo_EtapaExpediente" ee JOIN "conf_Etapa" t ON t.Id = ee.EtapaId
              WHERE ee.ExpedienteId = e.Id AND t.Flujo = 1 ORDER BY ee.FechaInicio DESC, ee.Id DESC LIMIT 1)
            FROM "dbo_Expediente" e'''):
        if act != ult:
            errores.append(f"Expediente {eid}: EtapaActualId {act} ≠ última etapa {ult}")
    # 4. un expediente tiene exactamente un demandado
    for eid, n in one('SELECT ExpedienteId, COUNT(*) FROM "dbo_ExpedientePersona" WHERE Rol = 1 GROUP BY ExpedienteId HAVING COUNT(*) <> 1'):
        errores.append(f"Expediente {eid}: {n} demandados")
    # 5. una sola etapa abierta por flujo
    for eid, fl, n in one('''SELECT ee.ExpedienteId, t.Flujo, COUNT(*) FROM "dbo_EtapaExpediente" ee JOIN "conf_Etapa" t ON t.Id = ee.EtapaId
                              WHERE ee.Estado = 1 GROUP BY ee.ExpedienteId, t.Flujo HAVING COUNT(*) > 1'''):
        errores.append(f"Expediente {eid}: {n} etapas abiertas en flujo {fl}")
    # 6. secuencia >= máximo NroOrden
    for e, ult, mx in one('SELECT s.EstudioId, s.UltimoValor, (SELECT MAX(NroOrden) FROM "dbo_Expediente" x WHERE x.EstudioId = s.EstudioId) FROM "seg_SecuenciaEstudio" s'):
        if ult < mx:
            errores.append(f"Secuencia del Estudio {e} atrasada")
    # 7. Estado de membresía pendiente solo si el usuario tiene otra membresía activa
    for e, u in one('SELECT EstudioId, UsuarioId FROM "seg_EstudioUsuario" WHERE Estado = 2'):
        if not one('SELECT 1 FROM "seg_EstudioUsuario" WHERE UsuarioId = ? AND EstudioId <> ? AND Estado = 3', u, e):
            errores.append(f"Membresía pendiente sin otra activa (usuario {u})")
    # 8. cobertura: todas las tablas tienen datos de ambos Estudios cuando son por Estudio
    for t in S.TABLAS:
        n = db.execute(f'SELECT COUNT(*) FROM "{t.schema}_{t.nombre}"').fetchone()[0]
        if n == 0:
            errores.append(f"{t.full} sin datos")
        elif t.tenant:
            es = {r[0] for r in db.execute(f'SELECT DISTINCT EstudioId FROM "{t.schema}_{t.nombre}"')}
            if es != {1, 2} and t.full not in ("conf.FeriadoEstudio",):
                errores.append(f"{t.full}: datos solo de Estudios {sorted(es)}")
    # 9. ninguna referencia cruzada (doble control además de las FK compuestas)
    for t in S.TABLAS:
        if not t.tenant:
            continue
        for col, ref in t.fks:
            r = S.POR_NOMBRE[ref]
            if r.tenant:
                bad = db.execute(f'SELECT COUNT(*) FROM "{t.schema}_{t.nombre}" a JOIN "{r.schema}_{r.nombre}" b '
                                 f'ON b.{r.pk[-1]} = a.{col} WHERE a.EstudioId <> b.EstudioId').fetchone()[0]
                if bad:
                    errores.append(f"{t.full}.{col}: {bad} referencias a otro Estudio")
    return errores


def resumen(db):
    filas = []
    for t in S.TABLAS:
        q = f'"{t.schema}_{t.nombre}"'
        tot = db.execute(f"SELECT COUNT(*) FROM {q}").fetchone()[0]
        if t.tenant or t.full == "aud.AuditoriaEvento":
            c = dict(db.execute(f"SELECT IFNULL(EstudioId, 0), COUNT(*) FROM {q} GROUP BY IFNULL(EstudioId, 0)").fetchall())
            filas.append((t.full, tot, c.get(1, 0), c.get(2, 0), c.get(0, 0)))
        else:
            filas.append((t.full, tot, None, None, tot))
    return filas


if __name__ == "__main__":
    # Siempre LF (no depende del sistema operativo): salida reproducible.
    for nombre, contenido in [
        ("01-esquema.sql", ddl_tsql()),
        ("02-datos-prueba.sql", datos_tsql()),
        ("03-seguridad-rls.sql", rls_tsql()),
        ("04-identidad.sql", identidad_tsql()),
    ]:
        (OUT / nombre).write_bytes(contenido.encode("utf-8"))
    db, conteo = validar_sqlite()
    err = validar_negocio(db)
    for e in err:
        print("ERROR:", e)
    print(f"Tablas: {len(S.TABLAS)} · filas: {sum(conteo.values())} · errores: {len(err)}")
    import json
    (OUT.parent / "gen" / "resumen.json").write_text(json.dumps(resumen(db), ensure_ascii=False))
    sys.exit(1 if err else 0)

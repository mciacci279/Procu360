# Procu360 · base de datos

1. Crear una base vacía en SQL Server 2019+ (o Azure SQL).
2. Ejecutar `sql/01-esquema.sql` (tablas, claves, índices, roles, procedimientos).
3. Ejecutar `sql/02-datos-prueba.sql` (2 Estudios, todas las tablas, datos ficticios).
4. Ejecutar `sql/03-seguridad-rls.sql` (Row-Level Security por Estudio).

Los tres scripts se generan con `python gen/build.py` (Python 3.10+, sin dependencias), que además valida
los datos de prueba (claves, restricciones y reglas de negocio) en una base SQLite en memoria.
Para cambiar el modelo, editar `gen/esquema.py`; para cambiar los datos, `gen/datos.py`. No editar los .sql a mano.

Detalle del modelo, mapeo desde Monitorio y escenario de prueba: `03-modelo-datos.md`.

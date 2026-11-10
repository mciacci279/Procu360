# Procu360
Definición en docs/01..04. El modelo de datos manda: db/gen/esquema.py y db/sql/.
- Stack: ASP.NET Core (.NET LTS), monolito modular, EF Core + SQL Server, Identity con cookie, Blazor Web App.
- Multi-tenant: EstudioId en todo; filtro global EF + SESSION_CONTEXT para RLS; FK compuestas.
- Nunca: valores de liquidación fijos en código, Office Interop, SQL concatenado, credenciales en archivos.
- Textos de UI en español (Argentina); fechas dd/MM/yyyy; importes $ 1.234,56.
- Cada cambio con pruebas; correr `dotnet test` antes de terminar.
- Conexión local: user-secrets, no appsettings.
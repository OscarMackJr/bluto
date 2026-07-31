# Bluto Repository Validation Tooling

WP-001 implements local validation for repository governance gates `GATE-01` through `GATE-06`.

## Local Execution

From the repository root:

```powershell
dotnet test implementation\Bluto.Validation.sln
dotnet run --project implementation\src\Bluto.Validation.Cli -- --root .
```

The validator writes:

- `implementation/validation/report.json`
- `repository/evidence/rvec/implementation-wp-001-gate-01.json`
- `repository/evidence/rvec/implementation-wp-001-gate-02.json`
- `repository/evidence/rvec/implementation-wp-001-gate-03.json`
- `repository/evidence/rvec/implementation-wp-001-gate-04.json`
- `repository/evidence/rvec/implementation-wp-001-gate-05.json`
- `repository/evidence/rvec/implementation-wp-001-gate-06.json`

A mandatory finding exits with status code `1`. Clean validation exits with status code `0`.

## Flyway Migrations

Flyway is configured through `implementation/tools/flyway/flyway.local.conf`. The file stores migration locations and schema settings only; set connection details in environment variables or pass them to the wrapper.

```powershell
$env:BLUTO_FLYWAY_URL = "jdbc:postgresql://localhost:15432/bluto"
$env:BLUTO_FLYWAY_USER = "postgres"
$env:BLUTO_FLYWAY_PASSWORD = "postgres"
.\scripts\windows\Invoke-BlutoFlyway.ps1 -Command validate
.\scripts\windows\Invoke-BlutoFlyway.ps1 -Command migrate
```

If Flyway is not installed at `C:\tools\flyway-13.1.0\flyway.cmd`, pass `-FlywayExecutable` with the local path.

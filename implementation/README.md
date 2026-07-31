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

# WP-003 Contract Tooling And Compatibility Harness

Governing Document IDs: `BLUTO-ADR-0006`, `BLUTO-ADR-0007`, `BLUTO-CONTRACT-ARCH-001`, `BLUTO-CONTRACT-API-001`, `BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-CONTRACT-COMPAT-001`, `BLUTO-TEST-CONTRACT-001`.

This package establishes contract validation commands only. It does not authorize new endpoint semantics, schema field additions or removals, production code, or generated clients.

## Local Commands

From the repository root:

```powershell
npm install --prefix implementation/tools/contracts
npm exec --prefix implementation/tools/contracts -- spectral lint docs/04-contracts/schemas/bluto-v1.openapi.yaml --ruleset implementation/tools/contracts/.spectral.yaml
npm exec --prefix implementation/tools/contracts -- ajv validate --spec=draft2020 -c ajv-formats -s implementation/contracts/schemas/source-link-established.v1.schema.json -d implementation/contracts/examples/source-link-established.v1.example.json
npm exec --prefix implementation/tools/contracts -- ajv validate --spec=draft2020 -c ajv-formats -s implementation/contracts/schemas/party-created.v1.schema.json -d implementation/contracts/examples/party-created.v1.example.json
dotnet test implementation\tests\Contracts\Bluto.Contracts.Tests.csproj --configuration Release
```

## Compatibility Baseline Procedure

The compatibility baseline is the last reviewed copy of each controlled contract under `implementation/contracts/baselines`. For WP-003, the compatibility baseline is `implementation/contracts/baselines/bluto-v1.openapi.yaml`, copied from the current controlled OpenAPI projection without semantic edits.

Before a future contract change is accepted:

1. Validate the candidate contract with Spectral and Ajv.
2. Compare candidate OpenAPI against the compatibility baseline.
3. Classify changes under `BLUTO-CONTRACT-COMPAT-001` as patch, minor, or major.
4. Reject unapproved breaking changes: removals, renames, type changes, requiredness changes, authorization weakening, ordering guarantee changes, or identifier changes.
5. Update the baseline only after the contract lifecycle state and review evidence authorize publication.

## Fail-Closed Checks

The harness reserves explicit checks for raw strong identifier field names, externally exposed HMAC token fields, missing operation security metadata, missing correlation metadata, and missing tenant scope on mapping results. Malformed-example tests also prove schema rejection for omitted tenant/correlation fields and prohibited extra fields.

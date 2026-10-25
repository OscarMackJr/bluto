# External Integrations

**Analysis Date:** 2026-10-24

All executable code lives in the nested git checkout `publish-bluto/`. The top-level project root contains only governance documents. Paths below are relative to the repository root (`publish-bluto/` locally, `OscarMackJr/bluto` on GitHub).

## APIs & External Services

**Program sibling systems (contracted, not live):**
- **olive** - owns the program's single shared review queue. bluto submits `identity_review` items to it and polls for decisions.
  - Contract: IR-1, vendored with a pinned hash at `contracts/vendored/olive/QUEUE_ITEM_CONTRACT_identity-review_v0.1.md` (also `contracts/vendored/olive/`). Pin record: `contracts/vendored/VENDORED.md` (SHA-256 `c993c35e...cff5cf`, source `olive/contracts/queue/identity-review/`, ratified 2026-10-21). olive's copy is authoritative. Never edit the vendored file. Re-vendor it and update the hash instead.
  - Endpoints (on olive): `POST /v1/queue/items` (create), `POST /v1/queue/items/{item_id}/withdraw`, `GET /v1/queue/items?kind=identity_review&state=decided&decided_after=<cursor>` (poll, once per tenant token at batch start)
  - Rules: the create body must **not** contain `tenant_id` or `producer`, because olive sets both from the credential. Payloads are references only (`payload_ref = bluto:review-evidence/<candidate_id>`). Idempotency key = (`producer`, `tenant_id`, `producer_ref = candidate_id`).
  - Status: **v1 sends nothing.** Submission, withdrawal, and polling start in v1.1. CI must use an olive queue stub. No client code exists in `implementation/src/`.
  - Spec: `specs/BLUTO_IDENTITY_SPINE_v0.1.1.md` sections 9, 9.1, and 9.2 (before every olive call, check that the tenant in the token matches the tenant of the data)
- **popeye** - issues tenant-scoped service tokens (gateway and credentials) for bluto's application identity when it calls olive
  - Auth: popeye service token. Tenant is read from the token's claims or by introspection, never from config.
  - Status: **stub-only**. popeye has not created bluto's registry entry.
- **hometown** - owns semantic models, Cube, and the Answer Trace Envelope. bluto has no integration with it (`specs/BLUTO_IDENTITY_SPINE_v0.1.1.md`, ownership section).

**Event transport:**
- PostgreSQL transactional outbox (schema `integration_outbox`) is the required v1 boundary (ADR-0017, `docs/01-foundation-governance/adrs/ADR-0017-PROPOSED-EVENT-TRANSPORT.md`)
  - Publisher: `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs`, behind the `IOutboxTransport` port. The only implementation is `OutboxTransport.Disabled`.
  - Config: `outbox.broker_mode` = `disabled` | `adapter`, `max_attempts` 1-10 (`implementation/deploy/configuration/bluto-runtime-config.schema.json`)
  - Optional future adapter: Azure Service Bus topics fed from the outbox. Not implemented, and no SDK is referenced.
  - Event schemas: `implementation/contracts/schemas/{outbox-fact,party-created,source-link-established}.v1.schema.json`, with examples in `implementation/contracts/examples/events/`

**Bluto's own inbound HTTP API:**
- Contract: `docs/04-contracts/schemas/bluto-v1.openapi.yaml` (OpenAPI 3.0.3, v1.1.0). The server URL is the placeholder `https://api.bluto.example/v1`. Baseline copy for compatibility checks: `implementation/contracts/baselines/bluto-v1.openapi.yaml`.
  - Paths: `/v1/mappings/{source_system}/{source_key}`, `/v1/mappings/{source_system}/{source_key}/at/{timestamp}`, `/v1/review-evidence/{candidate_id}`
  - Implemented: only `GET /v1/mappings/{source_system}/{source_key}` (`implementation/src/Bluto.Api/MappingApi.cs`)
  - Required headers: `x-correlation-id` (uuid, required), `x-request-id`
  - Lint gates in `implementation/tools/contracts/.spectral.yaml`: every operation declares `parameters` and `security`, `MappingResponse` requires `tenant_id`, and no field may be named like a raw strong identifier or HMAC token (`ssn`, `tax_id`, `hmac_token`, ...)
- Health and smoke contract: `implementation/deploy/contracts/operational-health.contract.json` (`GET /healthz`, mapping read, worker dry run, telemetry export, Key Vault fault returns 503, cross-tenant read returns 404). `/healthz` is **not mapped** in `Program.cs`.

## Data Storage

**Databases:**
- PostgreSQL (managed: Azure Database for PostgreSQL Flexible Server; local and test: `postgres:16-alpine` through Testcontainers)
  - Connection (runtime): Key Vault reference `kvref://<vault>/postgres-connection` (`vault.connection_reference`), host `kvref://<vault>/postgres-host`, database `bluto_identity`, roles `bluto_runtime` (runtime) and `bluto_migration` (migrations). See `implementation/deploy/configuration/nonproduction.sample.json`.
  - Connection (migrations): env vars `BLUTO_FLYWAY_URL`, `BLUTO_FLYWAY_USER`, `BLUTO_FLYWAY_PASSWORD` (`scripts/windows/Invoke-BlutoFlyway.ps1`)
  - Client: Npgsql `NpgsqlDataSource` plus Dapper with explicit SQL and explicit transactions (`implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`). No ORM.
  - Schemas: `identity_resolution` (parties, party_source_links, idempotency_records, review_cases, ...), `integration_outbox`, `flyway_history`. Extension `btree_gist`.
  - Tenant isolation: row-level security is enabled and **forced** on the tenant tables (`implementation/db/migrations/V002__wp013_privacy_tenant_lifecycle.sql`). Repositories must call `set_config('bluto.tenant_id', @tenantId, false)` on each connection before querying (`SetTenantContextAsync` in `PostgresIdentityResolutionSql.cs`).
  - Migrations: Flyway, files named `V###__description.sql` in `implementation/db/migrations/`, callbacks in `implementation/db/callbacks/`. Checksum evidence: `repository/evidence/rvec/implementation-wp-008-migration-checksums.json`.
- In-memory: `implementation/src/Bluto.Application/Identity/InMemoryCurrentMappingRepository.cs` is used only by tests. `src/Bluto.Api/Program.cs` registers no `ICurrentMappingRepository` at all (verified 2026-10-24), so the API cannot serve real requests, and it is not wired to Postgres.

**File Storage:**
- Local filesystem only. Contracts, fixtures, and evidence JSON are committed in the repo.

**Caching:**
- None

## Authentication & Identity

**Auth Provider:**
- Inbound API: JWT bearer (`components.securitySchemes.bearerAuth` in `bluto-v1.openapi.yaml`). The token issuer is not specified in code. The olive contract names Microsoft Entra ID as the reviewer identity provider (Entra group behind `role:identity_steward`, not yet named).
  - Implementation: `app.UseAuthorization()` plus the policy `resolve-current-party` (`RequireAuthenticatedUser`). `ClaimsBlutoAuthorizationPolicy` reads the `tenant_id` and `authorized_tenant_id` claims (`implementation/src/Bluto.Api/MappingApi.cs`). **No `AddAuthentication`/JwtBearer handler is registered** in `implementation/src/Bluto.Api/Program.cs`.
  - A cross-tenant read returns 404 with existence concealed.
- Outbound (olive): popeye tenant-scoped service token, application principal, no delegated agent. Stub-only.
- Workload identities (Azure managed identity): `bluto-<env>-api-identity`, `-worker-identity`, `-migration-identity` (`implementation/infra/terraform/main.tf`)
- CI to cloud: GitHub OIDC is planned (ADR-0013). No deploy job exists.

**Cryptography:**
- HMAC-SHA-256 tokenization of strong identifiers, with the key held in Azure Key Vault (`vault.token_key_reference`). The key owner is defined by `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` (REQ-ID-10), which is not in this repo. Config must never contain the key.
- SHA-256 digests (`SHA256.HashData`) in `PostgresIdentityResolutionSql.cs` and `ScheduledResolutionWorker.cs`

## Monitoring & Observability

**Error Tracking:**
- None (no Sentry, App Insights SDK, or similar)

**Telemetry:**
- OpenTelemetry-compatible through `ActivitySource`/`Meter` (ADR-0016). Planned export: OTLP to a Collector or Azure Monitor. Endpoint reference: `kvref://<vault>/otlp-endpoint`. Service namespace: `Bluto`.
  - Metrics: `bluto.outbox.publish_attempts`, `bluto.outbox.duplicate_suppressions`, `bluto.outbox.dead_letters`, `bluto.outbox.publish_latency_ms`, `bluto.outbox.lag_ms`, `bluto.identity_resolution.candidates`, `bluto.identity_resolution.failed_partitions`, `bluto.identity_resolution.outbox_lag_ms`
  - Spans: `outbox_publish_batch`, `scheduled_resolution_batch`
  - Allowed dimensions only: `environment`, `tenant_scope`, `operation`, `outcome`, `correlation_id`, `batch_id`, `rule_version`
  - No exporter or SDK pipeline is wired. `OpenTelemetry.Extensions.Hosting` is declared but unused.

**Logs:**
- `ILogger` (Microsoft.Extensions.Logging) with structured templates, for example `logger.LogInformation("{BlutoMappingOutcome}", message)` in `MappingApi.cs`. Logs go to Log Analytics workspace `bluto-<env>-logs` (planned).

## CI/CD & Deployment

**Hosting:**
- Azure Container Apps environment `bluto-<env>-apps` (API app plus worker container job), images in ACR (`bluto<env>acr`). Image digests are supplied by CI (`api_image_digest`, `worker_image_digest` variables). Defined only as a contract. Nothing is provisioned.

**CI Pipeline:**
- GitHub Actions (`.github/workflows/`)
  - `wp001-validation.yml`: `dotnet test` and the repository validator CLI (`dotnet run --project implementation/src/Bluto.Validation.Cli -- --root .`)
  - `wp002-ci.yml` jobs:
    - `validation`: restore, build, test with trx output, `dotnet format --verify-no-changes`, `dotnet list package --vulnerable --include-transitive`, validator, JSON checks, evidence manifest upload
    - `codeql`: `github/codeql-action@v4.37.3`, C#, `security-and-quality`, config `.github/codeql/codeql-config.yml`, `upload: false`
    - `trivy`: `aquasecurity/trivy-action@v0.36.0` fs scan to SARIF plus a CycloneDX SBOM, `exit-code: '0'` (does not block)
    - `runtime-smoke`: Terraform fmt, init with `-backend=false`, validate; smoke tests; `dotnet publish` of the API and worker
  - Triggers: `pull_request`, and push to `main`/`master`. The working branch `implementation/bootstrap` is covered only through PRs.
  - Artifacts are uploaded with `actions/upload-artifact@v7`, retention 30 days
- Dependabot (`.github/dependabot.yml`): `nuget` and `github-actions`, weekly on Monday, grouped
- CODEOWNERS (`.github/CODEOWNERS`): `@OscarMackJr/twg-global-appdev`. The file itself says this team slug is a placeholder.
- No release or deploy workflow. Nothing pushes container images.

## Environment Configuration

**Required env vars:**
- `BLUTO_FLYWAY_URL`, `BLUTO_FLYWAY_USER`, `BLUTO_FLYWAY_PASSWORD` - Flyway migrations
- GitHub-provided variables used in CI evidence: `GITHUB_SHA`, `GITHUB_WORKFLOW`, `GITHUB_WORKFLOW_REF`, `GITHUB_RUN_ID`, `GITHUB_RUN_ATTEMPT`
- The API and worker read no custom environment variables.

**Secrets location:**
- Azure Key Vault `bluto-<env>-vault`, referenced as `kvref://` URIs: `postgres-host`, `postgres-connection`, `token-key-version`, `otlp-endpoint`, `runtime-configuration`
- No secrets are committed. `.gitignore` excludes `implementation/validation/*.local.json`. No `.env` files exist.

## Webhooks & Callbacks

**Incoming:**
- None. bluto exposes no decision callback. The `POST /v1/review-cases/{id}/decisions` endpoint was removed by spec v0.1.1, and decisions arrive only by bluto polling olive.

**Outgoing:**
- None live. Planned for v1.1: outbound calls to olive's queue API (create, withdraw, poll), plus an optional Azure Service Bus outbox adapter.

---

*Integration audit: 2026-10-24*

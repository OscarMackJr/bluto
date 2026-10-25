# Codebase Concerns

**Analysis Date:** 2026-10-24

**Scope note:** The workspace root `C:\Users\OMack\bluto` is not a git repository. It holds an older, documentation-only copy of the "Enterprise Starter Kit v2.0" (`docs/`, `00-baseline/`, `repository/`, `specs/`, `releases/`). The live, version-controlled codebase is the nested git repository `publish-bluto/` (remote `https://github.com/OscarMackJr/bluto.git`, branch `implementation/bootstrap`). All code paths below are relative to `publish-bluto/` unless they start with the workspace root.

## Tech Debt

**Two divergent copies of the governed document set:**
- Issue: The workspace root and `publish-bluto/` both contain `docs/`, `00-baseline/`, `repository/`, `specs/`, `contracts/`, `SHA256SUMS.txt`, and `release-manifest.json`. Both declare version `2.0.0`, but the contents differ. Files such as `docs/01-foundation-governance/VISION.md` and `00-baseline/README.md` differ. ADR-0001..0017 exist only in `publish-bluto/`. `docs/01-Foundation/Vision.md` and `specs/BLUTO-ARCH-PRINCIPLES-00.md` exist only at the root, and all `implementation-wp-*` RVEC evidence exists only in `repository/evidence/rvec/`.
- Files: `C:\Users\OMack\bluto\docs\**`, `C:\Users\OMack\bluto\00-baseline\**`, `C:\Users\OMack\bluto\repository\**`, `C:\Users\OMack\bluto\SHA256SUMS.txt`, `docs/**`
- Impact: Agents and humans can read or edit the wrong copy. Running `Bluto.Validation.Cli --root C:\Users\OMack\bluto` would scan both trees and report duplicate Document IDs.
- Fix approach: Treat `publish-bluto/` as the single source of truth. Archive or delete the root copy, or move it under a clearly named `archive/` folder. Point all GSD planning at `publish-bluto/`.

**API host is not wired for a real deployment:**
- Issue: `Program.cs` calls `AddBlutoMappingApi()` and `UseAuthorization()`. It never registers an authentication scheme (`AddAuthentication`/`AddJwtBearer`) and never registers an `ICurrentMappingRepository`. No Postgres `ICurrentMappingRepository` exists anywhere; the only implementation is `InMemoryCurrentMappingRepository`. Tests get past this by registering stubs (`tests/Api/MappingApiRouteContractTests.cs:73`).
- Files: `implementation/src/Bluto.Api/Program.cs`, `implementation/src/Bluto.Api/MappingApi.cs:107-117`, `implementation/src/Bluto.Application/Identity/InMemoryCurrentMappingRepository.cs`
- Impact: The published `bluto-api` container fails at request time. DI cannot resolve `CurrentMappingQueryService`, and the authorization challenge has no default scheme. CI publishes this artifact (`.github/workflows/wp002-ci.yml`, "Publish API artifact") without ever starting it.
- Fix approach: Add a `PostgresCurrentMappingRepository` in `implementation/src/Bluto.Infrastructure.Postgres/Identity/`, a composition-root extension that registers `NpgsqlDataSource`, and a JWT bearer scheme that matches `bearerAuth` in `implementation/contracts/baselines/bluto-v1.openapi.yaml`. Add a `WebApplicationFactory<Program>` smoke test that uses the real `Program` without overriding DI.

**Worker is a library with no host:**
- Issue: `Bluto.Worker.csproj` enables container support (`ContainerRepository=bluto-worker`) but has no `Program.cs`, no `OutputType=Exe`, no hosted service, no `IOutboxStore` implementation, and no `IOutboxTransport` other than `OutboxTransport.Disabled`.
- Files: `implementation/src/Bluto.Worker/Bluto.Worker.csproj`, `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs`, `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs`
- Impact: Neither scheduled resolution nor outbox draining can run outside unit tests. The outbox table fills with `pending` rows forever.
- Fix approach: Add a Generic Host entry point with `BackgroundService` wrappers, and add a Postgres `IOutboxStore` (claim using `FOR UPDATE SKIP LOCKED`). The broker transport stays blocked on BLK-007 (`implementation/intake/IMPLEMENTATION-BLOCKERS.md`).

**Outbox schema does not support the publisher's state machine:**
- Issue: `IOutboxStore` exposes `MarkPendingRetryAsync(attemptCount, nextAttemptAt, errorCode)` and `MarkDeadLetteredAsync`, and `OutboxStoredFact` carries `AttemptCount`. The table `integration_outbox.outbox_facts` has only `publish_status in ('pending','published','failed')`. It has no `attempt_count`, `next_attempt_at`, `last_error_code`, `published_at`, or dead-letter state.
- Files: `implementation/db/migrations/V001__identity_resolution_minimal_slice.sql` (outbox_facts DDL), `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs:24-35`
- Impact: A Postgres `IOutboxStore` cannot be written without a new migration.
- Fix approach: Add forward-only migration `V003__outbox_publish_state.sql` with the columns above, a `dead_lettered` status, and an index on `(publish_status, next_attempt_at)`.

**Persisted identity token stored under a "digest" name:**
- Issue: `MatchIdentityDigest(string identityToken) => identityToken;` is an identity function. The column `match_identity_digest` stores the caller-supplied HMAC token verbatim, and so does `review_cases.match_identity_digest`. `releases/BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` section 2 accepts this, but the name implies a second hash.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`MatchIdentityDigest`), `implementation/db/migrations/V001__identity_resolution_minimal_slice.sql`
- Impact: Readers may assume tokens are re-hashed at rest. A future token-version rotation (`v1.` to `v2.`) has no column for the token version or a dual-accept path.
- Fix approach: Rename to `MatchIdentityKey`, or document the accepted decision inline. When rotation is designed, add a `token_version` column and widen `ApprovedIdentityTokenPattern` (`IdentityResolutionService.cs:117`) in step with `SyntheticTokenPattern` (`ScheduledResolutionWorker.cs`).

**Duplicated validation and pattern logic:**
- Issue: The raw-strong-identifier rejection runs twice: once in `ResolveAsync` and again inside `Validate`. The token regex `^v1\.[A-Za-z0-9_-]{43}$` is defined in two places. `DeterministicGuid` (SHA-256 truncated to 16 bytes) is copy-pasted three times.
- Files: `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs:130-134,238-241,547-551`, `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs` (`SyntheticTokenPattern`, `DeterministicGuid`), `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`DeterministicGuid`)
- Impact: A token-version change must be made in several places, and drift is likely.
- Fix approach: Move the token pattern and deterministic-GUID helper into `implementation/src/Bluto.Domain/Identity/`, for example an `IdentityToken` value object and a `DeterministicId` static class.

**Application project holds in-memory infrastructure and contracts:**
- Issue: `IdentityResolutionService.cs` (537 lines) mixes commands, the service, `IPartyRepository`, `InMemoryPartyRepository`, `OutboxFact`, and event payload records.
- Files: `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs`
- Impact: Test doubles ship in production assemblies, and the file is hard to navigate.
- Fix approach: Split into `Commands.cs`, `IPartyRepository.cs`, `OutboxFact.cs`, and `EventPayloads.cs`. Move `InMemoryPartyRepository` to a test-support project.

**Terraform is a contract stub, not infrastructure:**
- Issue: `main.tf` contains only `locals`. There are no providers or resources, and `versions.tf` pins only `required_version`. The image digests are placeholders (`sha256:aaaa…`, `sha256:bbbb…`).
- Files: `implementation/infra/terraform/main.tf`, `implementation/infra/terraform/variables.tf`, `implementation/infra/terraform/versions.tf`
- Impact: `terraform validate` passes in CI, but nothing can be provisioned. This is gated by BLK-005.
- Fix approach: When BLK-005 closes, add the `azurerm` provider pin and resources, and remove the placeholder digest defaults so CI must supply them.

**Naming drift between Terraform and runtime configuration:**
- Issue: Terraform derives names as `bluto-${environment}-…`, giving `bluto-nonproduction-vault` and `bluto-nonproduction-api-identity`. The sample config hard-codes `bluto-nonprod-vault` and `bluto-api-identity`. The config also names `runtime_role: bluto_runtime` and `migration_role: bluto_migration`, but no migration creates or grants these roles.
- Files: `implementation/infra/terraform/main.tf`, `implementation/deploy/configuration/nonproduction.sample.json`, `implementation/db/migrations/*.sql`
- Impact: Key Vault references will not resolve, and the app may end up connecting as a privileged role.
- Fix approach: Generate the sample config from the Terraform outputs, or add a test asserting the names match. Add a role/grant migration or a documented provisioning script.

## Known Bugs

**Rehydrated source links lose persisted status and identity:**
- Symptoms: `HydratePartyAsync` selects `status` but `Party.LoadSourceLink` hard-codes `PartySourceLink.Active` and assigns a fresh `SourceLinkId.New()`. `source_link_id` is never selected. `causation_id` is set to `null`, and `match_method`/`confidence` are hard-coded to `"deterministic_exact_token"`/`"1.0"`.
- Files: `implementation/src/Bluto.Domain/Identity/IdentityModel.cs` (`LoadSourceLink`), `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`HydratePartyAsync`)
- Trigger: Hydrate a Party that has a `superseded` or `rejected` link that is still open-ended or not closed. `IdentityResolutionService` (the `same_source_identity_conflict` check) and `Party.EstablishSourceLink` then treat it as active. This produces false conflicts or `InvalidOperationException("An active source link already exists…")`.
- Workaround: None. WP-014 tracks it as review finding S3 ("persisted-hydration-state-tests-for-s3") and S4 (match method/confidence persistence) in `implementation/plan/WORK-PACKAGE-INDEX.yaml`.

**Outbox publisher silently drops retried events:**
- Symptoms: `publishedEventIds.Add(record.Fact.EventId)` runs before the publish attempt and is never rolled back on failure. When the same `OutboxPublisher` instance claims that fact again for retry, it is counted as a "duplicate" and `MarkPublishedAsync` is called without ever reaching the transport.
- Files: `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs` (`DrainAsync`)
- Trigger: A transport failure on attempt 1, followed by a later `DrainAsync` on the same instance.
- Workaround: Create a new publisher per drain. Fix by adding to the set only after a successful publish, or by relying on the store's `publish_status` instead of in-process memory.

**Publish latency and lag metrics are always zero:**
- Symptoms: `started = batch.Now` and latency is computed as `batch.Now - started`, which is always 0. `ScheduledResolutionWorker` records `OutboxLagMs.Record(0, …)` and hard-codes `failed_partitions = 0`, `outbox_lag_ms = 0`.
- Files: `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs`, `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs`
- Trigger: Every run.
- Workaround: None. Use a `TimeProvider`/`Stopwatch` for latency and real counters for failures.

**Worker misclassifies review outcomes and aborts batches on any exception:**
- Symptoms: Any `IdentityResolutionResult` with `Created == false` counts as "replayed". That includes `cross_tenant_collision` and `same_source_identity_conflict`, which are deferrals. An exception from `ResolveAsync` (for example `DBConcurrencyException`) propagates and aborts the remaining candidates. `RequestedAt` is set to `candidate.EffectiveFrom` rather than the processing time.
- Files: `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs` (`ProcessAsync`, `ToCommand`)
- Trigger: Batches with conflicts, or a transient database error mid-batch.
- Workaround: None.

**Party creation collides on shared correlation IDs:**
- Symptoms: `parties` has `unique (tenant_id, correlation_id)`, and the insert uses `on conflict (tenant_id, correlation_id) do nothing`. The worker passes `batch.CorrelationId` to every candidate, so the second new Party in one batch inserts 0 rows and the repository throws `DBConcurrencyException("…not inserted atomically.")`.
- Files: `implementation/db/migrations/V001__identity_resolution_minimal_slice.sql` (`parties` unique constraint), `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`AtomicCreatePartyWithInitialLinkAndOutboxFacts`), `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs` (`ToCommand`)
- Trigger: A batch with two or more `deterministic_no_match` candidates resolved against `PostgresPartyRepository`.
- Workaround: Unit tests use `InMemoryPartyRepository`, which has no such constraint, so the bug is masked. Drop the unique constraint, or key it on the idempotency key or command ID instead.

**In-memory repository mutates state before a failed "atomic" commit:**
- Symptoms: `EstablishLink` mutates the shared `Party` object (`sourceLinks.Add`, `Version++`) before `CommitSourceLinkWithOutboxAsync`. When `failBeforeOutboxCommit` throws, the Party held in the dictionary already has the new link.
- Files: `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs` (`InMemoryPartyRepository`, `EstablishLink`)
- Trigger: Atomicity tests on the "link existing party" path.
- Workaround: None. Clone the Party before mutating, or apply the link inside the commit.

## Security Considerations

**Cross-tenant existence oracle via SECURITY DEFINER function:**
- Risk: `identity_resolution.has_cross_tenant_identity_digest` is `security definer`, bypasses RLS, and is executable by `PUBLIC` because there is no `REVOKE`. Any role with a connection can ask whether a given token exists in another tenant.
- Files: `implementation/db/migrations/V002__wp013_privacy_tenant_lifecycle.sql`
- Current mitigation: Tokens are HMAC outputs and the API conceals collisions. The function returns only a boolean.
- Recommendations: Add `revoke all on function … from public` and grant execute only to the runtime role. Record the accepted residual risk against BLK-010.

**Outbox table has no row-level security:**
- Risk: RLS is enabled and forced on the four `identity_resolution` tables, but not on `integration_outbox.outbox_facts`, which holds tenant-scoped `Restricted` payloads.
- Files: `implementation/db/migrations/V002__wp013_privacy_tenant_lifecycle.sql`
- Current mitigation: Repository code always filters by `tenant_id`.
- Recommendations: Add an RLS policy for `outbox_facts`. The publisher's per-tenant claim already sets `bluto.tenant_id`.

**Tenant context set at session scope on pooled connections:**
- Risk: `SetTenantContextAsync` uses `set_config('bluto.tenant_id', …, false)`, which is session scope. It depends on Npgsql's reset-on-close (`DISCARD ALL`) to stop one tenant's context leaking to the next connection user. Turning on `No Reset On Close` or using a pooler such as PgBouncer in transaction mode would leak tenant context.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`SetTenantContextAsync`)
- Current mitigation: Npgsql defaults.
- Recommendations: Run every tenant-scoped operation inside a transaction with `set_config(…, true)` (local scope). Add a test that reuses a pooled connection across tenants.

**RLS proven only for reads; write paths are tested as superuser:**
- Risk: Testcontainers connects as `postgres`, a superuser that bypasses RLS even with `FORCE`. Only one test (`Row_level_security_blocks_cross_tenant_reads_when_application_predicate_is_omitted`) creates `bluto_rls_app`, and it grants only `SELECT`. The insert `WITH CHECK` policies are never exercised.
- Files: `implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs:108-140`
- Current mitigation: None for writes.
- Recommendations: Run the whole live suite as a non-superuser runtime role with production-equivalent grants.

**Tenant authorization check is tautological:**
- Risk: `ClaimsBlutoAuthorizationPolicy` puts the first `tenant_id` claim into `AuthorizedTenantIds`, so `!AuthorizedTenantIds.Contains(TenantId)` can never be true for a token-derived context. The effective tenant is always the first claim, and callers cannot choose among authorized tenants.
- Files: `implementation/src/Bluto.Api/MappingApi.cs` (`CreateRequestContext`, `ResolveCurrentPartyAsync`)
- Current mitigation: Unit tests build `MappingRequestContext` directly to reach the concealed branch.
- Recommendations: Take the requested tenant from an explicit request input (header or route) and validate it against `authorized_tenant_id` claims only. This depends on BLK-010.

**Substring-based sensitive-term filter:**
- Risk: `OutboxContractValidator.ContainsProhibitedTerm` runs case-insensitive `Contains` over the serialized fact. Terms like `ssn` match unrelated text and dead-letter legitimate events. The filter also cannot detect real PII that uses other field names.
- Files: `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs` (`ProhibitedTerms`, `Validate`)
- Current mitigation: Payloads are typed records whose fields are known.
- Recommendations: Validate against the JSON Schemas in `implementation/contracts/schemas/` with `additionalProperties: false` instead of keyword scanning.

**CI security scans are non-gating:**
- Risk: Trivy runs with `exit-code: '0'`. CodeQL runs with `upload: false`, so results never reach code scanning and never fail the build. `dotnet list package --vulnerable` exits 0 even when vulnerabilities exist. Actions are pinned by tag rather than commit SHA.
- Files: `.github/workflows/wp002-ci.yml`
- Current mitigation: Artifacts are uploaded for manual review.
- Recommendations: Set Trivy `exit-code: '1'`, enable CodeQL upload with a severity gate, parse `--vulnerable --format json` and fail on findings, and pin actions to SHAs.

**Flyway helper passes the password on the command line:**
- Risk: `-password=$Password` shows up in process listings. The script also assigns to PowerShell's automatic `$args` variable and hard-codes `C:\tools\flyway-13.1.0\flyway.cmd`.
- Files: `scripts/windows/Invoke-BlutoFlyway.ps1`
- Current mitigation: Credentials come from environment variables.
- Recommendations: Pass the password through the `FLYWAY_PASSWORD` environment variable and rename `$args` to `$flywayArgs`.

**Tokenization key owner is unassigned:**
- Risk: No component or repository owns HMAC token computation or key custody. Joint ATE review and key-owner assignment are both `PENDING`.
- Files: `implementation/plan/STATE.md`, `releases/BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` section 2
- Current mitigation: Bluto enforces the token shape and its config tests forbid `hmac` keys.
- Recommendations: Close REQ-ID-10 before any production data flows.

## Performance Bottlenecks

**One connection per repository call; no shared transaction for resolution:**
- Problem: A single `ResolveAsync` opens up to five separate connections: idempotency lookup, active-link lookup, cross-tenant check, token lookup, and commit. `HydratePartyAsync` adds two queries per hit.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`, `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs` (`ResolveAsync`)
- Cause: The `IPartyRepository` interface has no unit-of-work concept.
- Improvement path: Introduce a unit-of-work/session that holds one connection and transaction per command. Batch hydration with a single join.

**In-memory scans in the worker and in-memory repository:**
- Problem: `InMemorySourceTokenCollisionRegistry` and `OutboxPublisher.publishedEventIds` grow without bound for the life of the process. `InMemoryPartyRepository.HasCrossTenantIdentityTokenAsync` scans every key.
- Files: `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs`, `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs`, `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs`
- Cause: These are POC test doubles used as defaults.
- Improvement path: Make collision detection rely on the database (`has_cross_tenant_identity_digest`) and drop the in-process sets.

**Serializable isolation without retry:**
- Problem: Commits use `IsolationLevel.Serializable`, but nothing retries on serialization failures (SQLSTATE `40001`) or `DBConcurrencyException`.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`CommitNewPartyWithLinkAndOutboxAsync`, `CommitSourceLinkWithOutboxAsync`)
- Cause: The retry policy is not implemented.
- Improvement path: Add bounded retry with re-read on `40001`/`23505`. The API maps `InvalidOperationException` to 503, but `DBConcurrencyException` is not mapped at all.

## Fragile Areas

**Hand-rolled YAML parsing in the repository validator:**
- Files: `implementation/src/Bluto.Validation.Cli/RepositoryValidator.cs` (`LoadGates`, `LoadSimpleYamlMap`, `ReadRegistryYaml`)
- Why fragile: The parser relies on exact two-space indentation, `- id:` / `name:` line prefixes, and a `release_ids:` sentinel. Valid YAML reformatting (quotes, flow style, comments, a different indent) silently drops entries or falls back to `DefaultGates()`.
- Safe modification: Keep the formatting of `repository/validation/*.yaml`, `repository/lir/logical-identity-registry.yaml`, and `repository/rgtm/repository-governance-traceability.yaml` byte-compatible. Better, adopt a real YAML parser such as YamlDotNet through `Directory.Packages.props`.
- Test coverage: `implementation/tests/Bluto.Validation.Tests/RepositoryValidatorTests.cs` uses fixtures that follow the same narrow format.

**Validator writes evidence into the source tree with fixed names and timestamps:**
- Files: `implementation/src/Bluto.Validation.Cli/RepositoryValidator.cs` (`WriteEvidence`, `RvecTimestamp = "1970-01-01T00:00:00Z"`)
- Why fragile: Every run overwrites `repository/evidence/rvec/implementation-wp-001-gate-0N.json` and `implementation/validation/report.json`, whatever the current work package is. Timestamps are constant, so the evidence cannot show when validation ran.
- Safe modification: Run with `--evidence-dir`/`--report` pointing at a scratch path when you do not mean to regenerate committed evidence.
- Test coverage: The output format is covered. Work-package parameterization is not.

**Baselined artifacts drift from `SHA256SUMS.txt`:**
- Files: `SHA256SUMS.txt`, `docs/00-authoritative/BLUTO_IDENTITY_SPINE_v0.1.md`, `docs/03-architecture/LOGICAL-ARCHITECTURE.md`, `docs/04-contracts/COMMAND-QUERY-CONTRACTS.md`, `docs/04-contracts/schemas/bluto-v1.openapi.yaml`, `docs/04-contracts/schemas/source-link-established.v1.schema.json`, `repository/lir/document-index.json`, `repository/rgtm/dependency-graph.json`
- Why fragile: At `HEAD`, 7 of 273 committed blobs no longer match their recorded SHA-256, even though the README calls these documents "immutable in this release". Separately, `core.autocrlf=true` on this Windows checkout converts working files to CRLF, so `sha256sum -c SHA256SUMS.txt` fails for every file locally. `.gitattributes` sets `eol` only for `*.cs`.
- Safe modification: Regenerate `SHA256SUMS.txt` through the change-control process after intentional edits. Add `* text eol=lf` (or `-text` for baselined docs) to `.gitattributes`, and add a CI step that verifies checksums.
- Test coverage: No automated checksum verification.

**Duplicate contract artifacts:**
- Files: `docs/04-contracts/schemas/bluto-v1.openapi.yaml` and `implementation/contracts/baselines/bluto-v1.openapi.yaml` (these differ); `docs/04-contracts/schemas/source-link-established.v1.schema.json` and `implementation/contracts/schemas/source-link-established.v1.schema.json` (these differ); `implementation/contracts/examples/party-created.v1.example.json` and `implementation/contracts/examples/events/party-created.v1.example.json` (these differ)
- Why fragile: `implementation/tools/contracts/package.json` lints the `docs/` OpenAPI copy but validates `implementation/contracts/` schemas, so it is unclear which copy is authoritative. No CI job runs the npm contract tooling, and Dependabot does not cover `implementation/tools/contracts`.
- Safe modification: Edit both copies together, or pick one and generate the other. Add a test that asserts equality or an explicit diff allowlist.
- Test coverage: `implementation/tests/Contracts/*` exercise some examples. The cross-copy consistency check called for by WP-014 ("contract-example-consistency-tests") is not present.

**CODEOWNERS paths do not match the repository layout:**
- Files: `.github/CODEOWNERS`
- Why fragile: It lists `/terraform/`, `/infra/`, `/sql/`, and `/migrations/`, which do not exist. The real paths are `implementation/infra/terraform/` and `implementation/db/migrations/`. The comment says the team slug `twg-global-appdev` is a placeholder.
- Safe modification: Change the paths to `/implementation/infra/`, `/implementation/db/`, and `/implementation/contracts/`, and confirm the team exists.

**CI does not run on the active branch:**
- Files: `.github/workflows/wp001-validation.yml`, `.github/workflows/wp002-ci.yml`
- Why fragile: The `push` triggers cover only `main`/`master`. Work is merged into `implementation/bootstrap`, so post-merge pushes there are validated only through their PRs. The two workflows also repeat the same build, test, and validator steps.

## Scaling Limits

**Single-tenant-per-call worker and publisher:**
- Current capacity: `OutboxPublisherBatch` and `SyntheticCandidateBatch` each carry one `TenantId`, and candidates are processed sequentially.
- Limit: Throughput is one tenant at a time with no partitioning. Per-tenant metric tags (`tenant_id` on every counter and histogram in `OutboxPublisher.Telemetry`) create high-cardinality series.
- Scaling path: Partition by tenant behind a scheduler, and replace raw `tenant_id` metric dimensions with the `tenant_scope` dimension listed in `implementation/deploy/configuration/nonproduction.sample.json` (`safe_dimensions`).

## Dependencies at Risk

**Bleeding-edge toolchain pins:**
- Risk: `net10.0` with SDK `10.0.204` (`global.json`), `Npgsql 10.0.0`, `xunit.v3 3.2.0`, `Microsoft.NET.Test.Sdk 18.0.0`, and `actions/checkout@v7`/`setup-dotnet@v6`/`upload-artifact@v7`. Container base images are `10.0-noble-chiseled-extra`.
- Impact: `TreatWarningsAsErrors=true` plus `AnalysisLevel=latest` (`Directory.Build.props`) means an SDK roll-forward (`rollForward: latestFeature`) can break the build when new analyzers ship.
- Migration plan: Pin `AnalysisLevel` to a specific version, for example `10.0`, and let Dependabot (`.github/dependabot.yml`) carry upgrades.

**Unimplemented OpenTelemetry export:**
- Risk: Only `OpenTelemetry.Api` is referenced. `OpenTelemetry.Extensions.Hosting` is declared centrally but unused, and no exporter is configured. This is gated by BLK-008.
- Impact: The `ActivitySource`/`Meter` instrumentation emits nothing at runtime.

## Missing Critical Features

**Unimplemented contract surface:**
- Problem: The OpenAPI baseline defines `/v1/mappings/{source_system}/{source_key}/at/{timestamp}` (point-in-time) and `/v1/review-evidence/{candidate_id}`. Only the current-mapping route is implemented. Merge, split, and retire exist as domain methods (`IdentityModel.cs`) with no service or persistence path. The point-in-time test performs the merge with a raw SQL fixture and carries `TODO(BLUTO_IDENTITY_SPINE_v0.1 section 7)`.
- Files: `implementation/contracts/baselines/bluto-v1.openapi.yaml`, `implementation/src/Bluto.Api/MappingApi.cs`, `implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs:174`
- Blocks: REQ-ID-09 (pre-merge trace resolution) in `releases/BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md`; BLK-013/014/015.

**Optimistic concurrency on Party aggregate:**
- Problem: `parties.version` is incremented in the domain, but no `UPDATE … WHERE version = @expected` path exists. The existing-party link path inserts the link and never bumps the persisted version.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` (`AtomicCreateSourceLinkForExistingPartyWithOutboxFact`)
- Blocks: WP-014 finding S2 ("optimistic-concurrency-or-explicit-deferral-tests-for-s2").

**Missing WP-014 source input:**
- Problem: `implementation/plan/WORK-PACKAGE-INDEX.yaml` (WP-014 `source_inputs` and `allowed_files`) references `releases/BLUTO_IMPLEMENTATION_REVIEW_v0.1.md`, which is not in the repository. `releases/` contains only `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` and `ENTERPRISE-RELEASE-CERTIFICATION.md`.
- Blocks: Closing review findings S2–S4 and issuing a POC go/no-go.

**Open governance blockers:**
- Problem: 22 blockers (BLK-001..BLK-022) are recorded. Several remain unresolved in code: broker (BLK-007), observability backend (BLK-008), cross-tenant authorization (BLK-010), merge/split/retire semantics (BLK-013..015), ruleset activation (BLK-016), and evidence retention (BLK-017).
- Files: `implementation/intake/IMPLEMENTATION-BLOCKERS.md`
- Blocks: Production deployment and lifecycle operations.

**Local Trivy evidence blocked:**
- Problem: `repository/evidence/rvec/implementation-wp-011-local-trivy-condition.json` records `"result": "BLOCKED_LOCALLY"`.
- Blocks: Full local/CI parity, which is a WP-014 acceptance criterion.

## Test Coverage Gaps

**Production composition root:**
- What's not tested: The real `Program` pipeline with no DI overrides (authentication scheme, repository registration, startup).
- Files: `implementation/src/Bluto.Api/Program.cs`, `implementation/tests/Api/*`
- Risk: The deployed API fails on its first request.
- Priority: High

**Concurrency and race conditions:**
- What's not tested: Two concurrent `ResolveAsync` calls for the same source key or token, serialization failures, and the exclusion constraint `ex_party_source_links_active_interval_no_overlap` under contention. No test uses `Task.WhenAll` or parallel execution.
- Files: `implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs`
- Risk: Unhandled `DBConcurrencyException`/`23505` in production, and duplicate-Party creation windows between the separate lookup connections.
- Priority: High

**Hydration fidelity:**
- What's not tested: Round-tripping non-active link statuses, `source_link_id`, and `causation_id` through `HydratePartyAsync`.
- Files: `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`
- Risk: The Known Bug above goes undetected (S3).
- Priority: High

**Worker against Postgres:**
- What's not tested: `ScheduledResolutionWorker` with `PostgresPartyRepository`. All worker tests use `InMemoryPartyRepository`, which masks the correlation-ID uniqueness bug.
- Files: `implementation/tests/Worker/ScheduledResolutionWorkerTests.cs`
- Risk: Multi-candidate batches fail in any real environment.
- Priority: High

**Outbox retry across drains:**
- What's not tested: A failure followed by a retry on the same `OutboxPublisher` instance.
- Files: `implementation/tests/Outbox/OutboxPublisherTests.cs`
- Risk: Events are marked published without ever being sent.
- Priority: High

**RLS on write paths and the outbox schema:**
- What's not tested: `WITH CHECK` enforcement on inserts as a non-superuser, and RLS on `integration_outbox.outbox_facts`, which currently has none.
- Files: `implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs`
- Risk: Tenant isolation regressions on writes go unnoticed.
- Priority: Medium

**Checksum and contract-copy consistency:**
- What's not tested: `SHA256SUMS.txt` against committed files; equality between the `docs/04-contracts/schemas/*` and `implementation/contracts/*` copies; the npm Spectral/AJV harness in CI.
- Files: `SHA256SUMS.txt`, `implementation/tools/contracts/package.json`, `.github/workflows/wp002-ci.yml`
- Risk: Baselined "immutable" artifacts and contracts drift silently (7 checksum mismatches exist today).
- Priority: Medium

---

*Concerns audit: 2026-10-24*

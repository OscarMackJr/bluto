# Testing Patterns

**Analysis Date:** 2026-10-24

**Scope note:** All tests live in the .NET solution `implementation/Bluto.Validation.sln`. Paths below are relative to `publish-bluto/` unless stated otherwise. Governing test standards are in `docs/07-testing/` (`TEST-ARCHITECTURE.md`, `DOMAIN-UNIT-TESTING.md`, `CONTRACT-TESTING.md`, `SECURITY-TESTING.md`, `COVERAGE-QUALITY-GATES.md`); read them before adding tests.

## Test Framework

**Runner:**
- xUnit v3 `3.2.0` (`xunit.v3`) with `xunit.runner.visualstudio` `3.1.5` and `Microsoft.NET.Test.Sdk` `18.0.0`
- Versions pinned centrally in `Directory.Packages.props`; target `net10.0` via `Directory.Build.props` (SDK `10.0.204` in `global.json`)
- Config: no `xunit.runner.json`; each test csproj sets `<IsPackable>false</IsPackable>`, `<IsTestProject>true</IsTestProject>`, `<NoWarn>$(NoWarn);CS1591</NoWarn>`

**Assertion Library:**
- Built-in xUnit `Assert` only. No FluentAssertions, Shouldly, or similar.

**Supporting libraries:**
- `Testcontainers.PostgreSql` `4.8.0` - live PostgreSQL 16 for persistence tests (`tests/Bluto.Persistence.Tests/`)
- `JsonSchema.Net` `7.4.0` - JSON Schema evaluation in `tests/Contracts/` and `tests/Smoke/`
- `Microsoft.AspNetCore.App` framework reference - route/metadata tests in `tests/Api/`
- `Microsoft.AspNetCore.Mvc.Testing` is declared in `Directory.Packages.props` but not referenced by any test project (no `WebApplicationFactory` usage)
- Node contract tooling in `implementation/tools/contracts/package.json` (Spectral `^6.15.0`, Ajv `^8.17.1`, ajv-formats); scripts `lint:openapi`, `validate:source-link`, `validate:party-created`

**Run Commands:**
```powershell
dotnet test implementation\Bluto.Validation.sln                                     # Run all tests
dotnet test implementation\Bluto.Validation.sln --configuration Release             # CI-equivalent
dotnet test implementation\tests\Smoke\Bluto.Smoke.Tests.csproj --filter Smoke      # Smoke subset (CI wp012 job)
dotnet test implementation\Bluto.Validation.sln --filter "Category=Security"        # By trait
dotnet test implementation\Bluto.Validation.sln --filter "Category!=Integration"    # Skip Docker-backed tests
dotnet format implementation\Bluto.Validation.sln --verify-no-changes               # Required in CI
dotnet run --project implementation\src\Bluto.Validation.Cli -- --root .            # Governance gates GATE-01..06
npm exec --prefix implementation/tools/contracts -- spectral lint docs/04-contracts/schemas/bluto-v1.openapi.yaml --ruleset implementation/tools/contracts/.spectral.yaml
```
- Watch mode: `dotnet watch test --project <test csproj>` (not configured explicitly)
- Coverage: no collector configured (see Coverage)
- CI (`.github/workflows/wp002-ci.yml`) runs `dotnet test ... --configuration Release --no-build --logger "trx;LogFileName=bluto-tests.trx" --results-directory implementation/validation/test-results` and uploads the TRX as evidence.

## Test File Organization

**Location:**
- Separate `implementation/tests/` tree, one project per concern. Not co-located with source.

**Naming:**
- Project: `Bluto.<Area>.Tests.csproj`; root namespace `Bluto.<Area>.Tests`
- Class/file: `<Subject>Tests` - `OutboxPublisherTests`, `MigrationContractTests`, `PostgresPartyRepositoryLiveTests` (Live = needs Docker)
- Work-package specification tests: `Wp<NNN><Area>SpecificationTests` (`Wp006IdentityResolutionDomainSpecificationTests`)
- Test methods: sentence-style `Snake_case_with_leading_capital` describing the behavior/invariant, no `Should`/`Given` prefixes:
  - `Party_id_is_never_reused`
  - `Raw_strong_identifiers_fail_closed_without_persistence_logs_or_events`
  - `Publisher_claims_only_the_requested_tenant_partition`

**Structure:**
```
implementation/tests/
├── Api/                        # Bluto.Api.Tests - MappingApi responses, route registration, claims policy
├── Bluto.Identity.Tests/       # Domain + application tests over InMemoryPartyRepository; SliceHarness.cs
├── Bluto.Persistence.Tests/    # Migration text contracts, SQL script contracts, Testcontainers live tests
├── Bluto.Validation.Tests/     # RepositoryValidator gates (RepositoryFixture.cs), CI workflow assertions
├── Contracts/                  # OpenAPI/JSON Schema/example contract tests (no project references)
├── Outbox/                     # OutboxPublisher with in-file fake store/transports
├── Smoke/                      # Runtime config schema, container publish settings, smoke plan
└── Worker/                     # ScheduledResolutionWorker with in-file WorkerHarness
```
- Every test project must be added to `implementation/Bluto.Validation.sln` (solution folders `Api`, `Contracts`, `Worker`, `Outbox`, `Smoke`, `tests`) or CI will not run it.

## Test Structure

**Suite Organization:**
```csharp
using Bluto.Application.Identity;
using Bluto.Worker.Outbox;
using Xunit;

namespace Bluto.Outbox.Tests;

public sealed class OutboxPublisherTests
{
    private static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Security")]
    public async Task Publisher_claims_only_the_requested_tenant_partition()
    {
        var tenantB = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var store = new InMemoryOutboxStore([Fact("aaaaaaaa-...-020", "PartyCreated", tenantId: TenantA),
                                             Fact("aaaaaaaa-...-021", "PartyCreated", tenantId: tenantB)]);
        var transport = new RecordingTransport();
        var publisher = Publisher(store, transport: transport, brokerEnabled: true);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(1, result.PublishedCount);
        Assert.DoesNotContain(store.PublishedFacts, fact => fact.TenantId == tenantB);
    }

    private static OutboxPublisher Publisher(...) => ...;      // private factory helpers at bottom
    private sealed class RecordingTransport : IOutboxTransport { ... } // fakes nested at bottom
}
```

**Patterns:**
- Test classes are `public sealed class`; one blank line separates Arrange / Act / Assert blocks (no `// Arrange` comments)
- Every test carries one or more `[Trait("Category", "...")]` attributes. Used categories: `Security`, `Integration`, `Contract`, `Outbox`, `Worker`, `Domain`, `Api`, `Specification`, `Migration`, `Application`, `Smoke`, `Acceptance`, `Property`, `Observability`. Exception: `tests/Bluto.Validation.Tests/*` have no traits. Tag new tests with the layer and with `Security` whenever tenant isolation or raw-identifier handling is asserted.
- `[Theory]` + `[InlineData]` for value variations; `[Trait]` attributes follow `[InlineData]`
- Setup: static factory harnesses (`SliceHarness.Create(failBeforeOutboxCommit: true)`, `WorkerHarness.Create(authorizedTenantIds)`), not constructors or `IClassFixture`
- Teardown: `IDisposable` fixtures with `using var fixture = RepositoryFixture.CreateValid();`; `IAsyncLifetime` (`InitializeAsync`/`DisposeAsync` returning `ValueTask`) for containers
- Cancellation: pass `CancellationToken.None` in unit tests; use `TestContext.Current.CancellationToken` in xUnit v3 live tests
- Fixed deterministic GUIDs/timestamps (`10000000-0000-0000-0000-000000000001` = TenantA, `20000000-...-0002` = TenantB, `dddddddd-...` = correlation ID, `2026-07-30T12:00:00Z`)
- Use `Assert.Single`, `Assert.IsType<T>` return values to continue asserting: `var link = Assert.Single(result.Party.SourceLinks);`

## Mocking

**Framework:** None. No Moq/NSubstitute. Hand-written fakes implementing the port interfaces.

**Patterns:**
```csharp
// Recording fake nested in the test class (tests/Outbox/OutboxPublisherTests.cs)
private sealed class RecordingTransport : IOutboxTransport
{
    public List<OutboxFact> PublishedFacts { get; } = [];

    public Task<OutboxPublishAck> PublishAsync(OutboxFact fact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PublishedFacts.Add(fact);
        return Task.FromResult(new OutboxPublishAck(fact.EventId));
    }
}

// Failure-injecting fake
private sealed class FailingTransport : IOutboxTransport
{
    private readonly string code;
    public FailingTransport(string code) { this.code = code; }
    public Task<OutboxPublishAck> PublishAsync(OutboxFact fact, CancellationToken cancellationToken)
    {
        _ = fact;
        cancellationToken.ThrowIfCancellationRequested();
        throw new OutboxTransportException(code);
    }
}
```
- Production in-memory adapters double as test doubles: `InMemoryPartyRepository` (with `failBeforeOutboxCommit` switch and inspection helpers `Parties()`, `OutboxFacts()`, `ReviewCases()`, `ActiveLinks(...)`, `Snapshot()`) in `src/Bluto.Application/Identity/IdentityResolutionService.cs`; `InMemoryCurrentMappingRepository`; `InMemorySourceTokenCollisionRegistry`
- Built-in strategy objects for configuration: `OutboxContractValidator.Permissive` vs `OutboxContractValidator.Governed`, `OutboxTransport.Disabled`
- Logging is captured by passing `logs.Add` (a `List<string>`) as the `Action<string> log` delegate; pass `_ => { }` when logs are irrelevant
- Call-recording repositories assert that authorization short-circuits happen before data access: `Assert.Empty(repository.Calls);` (`RecordingCurrentMappingRepository` in `tests/Api/ResolveCurrentPartyApiTests.cs`)

**What to Mock:**
- Ports only: `IPartyRepository`, `ICurrentMappingRepository`, `IOutboxStore`, `IOutboxTransport`, `IOutboxContractValidator`, `ISourceTokenCollisionRegistry`
- External brokers and transports (always faked; no broker exists)

**What NOT to Mock:**
- Domain types (`Party`, `EffectiveInterval`, `PartyId`) - use them directly
- Application services (`IdentityResolutionService`, `CurrentMappingQueryService`) - construct real instances over fakes
- PostgreSQL in live persistence tests - use Testcontainers and real migrations
- Contract files - read the real files from the repo

## Fixtures and Factories

**Test Data:**
```csharp
// tests/Bluto.Identity.Tests/SliceHarness.cs - optional-parameter command factory
public ResolveSourceCandidateCommand Command(
    Guid? tenantId = null,
    string sourceSystem = "nexus",
    string sourceKey = "SRC-001",
    string identityToken = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
    ...
    string? rawStrongIdentifier = null) =>
    new(
        CommandId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        TenantId: tenantId ?? TenantA,
        ...);

// Usage: override only what the test varies
await harness.Service.ResolveAsync(harness.Command(sourceKey: "SRC-002", identityToken: "v1.BBBB..."), CancellationToken.None);
```
- Identity tokens are synthetic and must match `^v1\.[A-Za-z0-9_-]{43}$`
- Idempotency keys are six `|`-separated parts ending with the operation: `tenant|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link`
- Negative raw-identifier samples (`"123-45-6789"`, `"123456789"`) are used only to prove they never reach `error.Message`, logs, persisted snapshot, or outbox
- `RepositoryFixture` (`tests/Bluto.Validation.Tests/RepositoryFixture.cs`) builds throwaway governed repos in `Path.GetTempPath()/bluto-validator-tests/<guid>` via named variants (`CreateValid`, `CreateWithMissingDependency`, `CreateWithDuplicateIdentity`, `CreateWithAuthorityInversion`, ...) and deletes them on `Dispose`

**Location:**
- Harnesses/factories: `tests/Bluto.Identity.Tests/SliceHarness.cs`, `tests/Bluto.Validation.Tests/RepositoryFixture.cs`; others are private nested classes/methods at the bottom of the test file (`WorkerHarness`, `Batch(...)`, `Candidate(...)` in `tests/Worker/ScheduledResolutionWorkerTests.cs`; `Fact(...)`, `InMemoryOutboxStore` in `tests/Outbox/OutboxPublisherTests.cs`)
- File fixtures: `implementation/testdata/` (synthetic candidate batch + schema), `implementation/contracts/examples/` and `implementation/contracts/fixtures/`, `implementation/deploy/configuration/nonproduction.sample.json`
- Repo-root discovery is duplicated per test class as a private helper walking up from `AppContext.BaseDirectory` until an `implementation/` folder is found:
  ```csharp
  private static string RepositoryRoot()
  {
      var directory = AppContext.BaseDirectory;
      while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
      {
          directory = Directory.GetParent(directory)?.FullName;
      }
      return directory ?? throw new InvalidOperationException("Repository root not found.");
  }
  ```
  Copy this helper (or `RepoPath(params string[] parts)`) when a new test reads repo files.

## Coverage

**Requirements:** No line-coverage threshold or collector (no coverlet, no `--collect`). `docs/07-testing/COVERAGE-QUALITY-GATES.md` defines coverage as behavioral: every requirement, invariant, transition, contract, threat, and operational scenario needs a test; release gates require zero cross-tenant and raw-identifier violations. `docs/07-testing/DOMAIN-UNIT-TESTING.md` requires positive, negative, boundary, and property-style tests per invariant.

**View Coverage:**
```powershell
dotnet test implementation\Bluto.Validation.sln --collect:"XPlat Code Coverage"   # requires adding coverlet.collector; not currently referenced
```

## Test Types

**Unit Tests (Domain/Application):**
- `tests/Bluto.Identity.Tests/` - real `IdentityResolutionService` over `InMemoryPartyRepository`; no DB, network, or real clock (fixed timestamps)
- `tests/Outbox/`, `tests/Worker/` - publisher/worker logic over fakes
- `tests/Api/ResolveCurrentPartyApiTests.cs` - calls static `MappingApi.ResolveCurrentPartyAsync(...)` directly with a `MappingRequestContext`, asserting status code, headers, and envelope (both typed and via `JsonSerializer.SerializeToElement(response.Body).GetProperty("tenant_id")`)

**Specification tests (reflection):**
- `Wp006*SpecificationTests` assert required members exist by reflection (`type.GetMethods(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(m => m.Name == "MergeInto")`). Use these to pin work-package API surface; pair them with behavioral tests.

**Contract / static-artifact tests:**
- Read files and assert content: OpenAPI text (`tests/Contracts/OpenApiContractTests.cs`), migration SQL (`tests/Bluto.Persistence.Tests/MigrationContractTests.cs`), csproj settings (`tests/Smoke/RuntimeConfigurationContractTests.cs`), CI workflow YAML (`tests/Bluto.Validation.Tests/CiConfigurationTests.cs`), SQL transaction scripts (`PostgresIdentityResolutionSql.CreatePartyWithInitialLinkAndOutboxFacts()`)
- JSON Schema validation of examples:
  ```csharp
  var schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
  var example = JsonNode.Parse(File.ReadAllText(examplePath)) ?? throw new InvalidOperationException("Example JSON could not be parsed.");
  var result = schema.Evaluate(example, new EvaluationOptions { OutputFormat = OutputFormat.List });
  Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(d => d.EvaluationPath.ToString())));
  ```
- Prohibited-term scans (`ssn`, `tax_id`, `raw_identifier`, `hmac_token`, `identity_token`) with `Assert.DoesNotContain(term, text, StringComparison.OrdinalIgnoreCase)` on every new contract, migration, and config artifact
- When changing CI workflow, csproj container settings, or `.gitattributes`, update the matching string assertions in these tests

**Integration Tests:**
- `tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs` - `IAsyncLifetime` starts `postgres:16-alpine` via `PostgreSqlBuilder`, applies all `implementation/db/migrations/V*.sql` in ordinal order, then exercises `PostgresPartyRepository` through `IdentityResolutionService`, asserting row counts with `select count(*)` and RLS cross-tenant blocking. Requires Docker. Tagged `Integration`.
- Many in-memory tests also carry `Category=Integration` (outbox/worker), so filtering out `Integration` skips more than Docker tests.

**E2E Tests:**
- Not used. `tests/Smoke/` validates deployment config/plans statically (`implementation/deploy/smoke/nonproduction-smoke-plan.json`); runbook in `implementation/runbooks/wp-012-operational-smoke.md`.

## Common Patterns

**Async Testing:**
```csharp
[Fact]
[Trait("Category", "Domain")]
public async Task One_permitted_active_link_exists_for_canonical_source_identity_scope()
{
    var harness = SliceHarness.Create();
    var command = harness.Command();

    var result = await harness.Service.ResolveAsync(command, CancellationToken.None);
    var replay = await harness.Service.ResolveAsync(command, CancellationToken.None);

    Assert.Equal(result.Party.PartyId, replay.Party.PartyId);
    Assert.Single(harness.Repository.ActiveLinks(command.TenantId, command.SourceSystem, command.SourceKey));
}
```

**Error Testing:**
```csharp
// Fail-closed: assert exception type, then prove no leakage and no side effects
var error = await Assert.ThrowsAsync<ArgumentException>(() =>
    harness.Service.ResolveAsync(harness.Command(rawStrongIdentifier: "123-45-6789"), CancellationToken.None));

Assert.DoesNotContain("123-45-6789", error.Message);
Assert.DoesNotContain("123-45-6789", JsonSerializer.Serialize(harness.Repository.Snapshot()));
Assert.DoesNotContain("123-45-6789", string.Join(Environment.NewLine, harness.Logs));
Assert.Empty(harness.Repository.Parties());
Assert.Empty(harness.Repository.OutboxFacts());

// Synchronous domain invariant
Assert.Throws<InvalidOperationException>(() => result.Party.ReplaceId(PartyId.New()));

// API boundary: status code + no repository access
Assert.Equal(403, response.StatusCode);
Assert.Empty(repository.Calls);
```

**Idempotency / determinism testing:**
- Run the same command or batch twice and assert identical IDs, unchanged counts, and no duplicate outbox facts (`Worker_rerun_is_idempotent_and_does_not_duplicate_state_or_outbox`)
- Run the validator twice and compare finding order (`Mandatory_findings_return_non_zero_and_are_sorted_deterministically`)
- Supply unordered input and assert ordered output (`Assert.Equal(["SRC-001", "SRC-002"], result.ProcessedSourceKeys)`)

**Known gaps to be aware of:**
- `tests/Smoke/Bluto.Smoke.Tests.csproj` omits `IsTestProject`/`IsPackable` and `PrivateAssets="all"` on the runner, unlike the other test projects
- `implementation/tools/contracts/package.json` declares `bin`/scripts pointing at `./bin/ajv-validate.mjs`, which is not present in `implementation/tools/contracts/`

---

*Testing analysis: 2026-10-24*

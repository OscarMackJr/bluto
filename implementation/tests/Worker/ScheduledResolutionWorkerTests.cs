using System.Text.Json;
using Bluto.Api.Mapping;
using Bluto.Application.Identity;
using Bluto.Worker.Resolution;
using Xunit;

namespace Bluto.Worker.Tests;

public sealed class ScheduledResolutionWorkerTests
{
    private static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid CorrelationId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Worker")]
    public void Candidate_input_fixture_contract_requires_synthetic_tokens_and_tenant_scope()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(TestDataPath("candidate-batch.v1.schema.json")));
        var root = schema.RootElement;

        Assert.Equal("SyntheticCandidateBatch.v1", root.GetProperty("title").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());

        var serialized = File.ReadAllText(TestDataPath("candidate-batch.v1.schema.json"));
        Assert.Contains("synthetic_identity_token", serialized);
        Assert.Contains("tenant_id", serialized);
        Assert.Contains("^sha256:[a-f0-9]{64}$", serialized);
        Assert.DoesNotContain("ssn", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_id", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_identifier", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Worker")]
    public async Task Worker_processes_synthetic_batch_in_deterministic_source_key_order()
    {
        var harness = WorkerHarness.Create();
        var batch = Batch([
            Candidate("SRC-002", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            Candidate("SRC-001", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
        ]);

        var result = await harness.Worker.ProcessAsync(batch, CancellationToken.None);

        Assert.Equal(["SRC-001", "SRC-002"], result.ProcessedSourceKeys);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(4, harness.Repository.OutboxFacts().Count);
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Integration")]
    public async Task Worker_rerun_is_idempotent_and_does_not_duplicate_state_or_outbox()
    {
        var harness = WorkerHarness.Create();
        var batch = Batch([Candidate("SRC-001")]);

        var first = await harness.Worker.ProcessAsync(batch, CancellationToken.None);
        var second = await harness.Worker.ProcessAsync(batch, CancellationToken.None);

        Assert.Equal(1, first.CreatedCount);
        Assert.Equal(0, second.CreatedCount);
        Assert.Single(harness.Repository.Parties());
        Assert.Equal(2, harness.Repository.OutboxFacts().Count);
        Assert.Single(harness.Repository.ActiveLinks(TenantA, "nexus", "SRC-001"));
    }

    [Theory]
    [InlineData("ambiguous")]
    [InlineData("conflicting")]
    [Trait("Category", "Worker")]
    [Trait("Category", "Security")]
    public async Task Ambiguous_or_conflicting_candidates_are_deferred_without_active_links(string outcome)
    {
        var harness = WorkerHarness.Create();

        var result = await harness.Worker.ProcessAsync(Batch([Candidate("SRC-001", outcome: outcome)]), CancellationToken.None);

        Assert.Equal(1, result.DeferredCount);
        Assert.Empty(harness.Repository.Parties());
        Assert.Empty(harness.Repository.ActiveLinks(TenantA, "nexus", "SRC-001"));
        Assert.Empty(harness.Repository.OutboxFacts());
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Security")]
    public async Task Tenant_scope_failure_happens_before_repository_access()
    {
        var repository = new InMemoryPartyRepository();
        var worker = new ScheduledResolutionWorker(
            new IdentityResolutionService(repository, _ => { }),
            new ScheduledResolutionWorkerOptions(new HashSet<Guid> { TenantA }, "worker:identity-resolution"),
            _ => { });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            worker.ProcessAsync(Batch([Candidate("SRC-001")], tenantId: TenantB), CancellationToken.None));

        Assert.Empty(repository.Parties());
        Assert.Empty(repository.OutboxFacts());
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Integration")]
    public async Task Failure_leaves_no_partial_authoritative_state_or_outbox()
    {
        var repository = new InMemoryPartyRepository(failBeforeOutboxCommit: true);
        var worker = new ScheduledResolutionWorker(
            new IdentityResolutionService(repository, _ => { }),
            new ScheduledResolutionWorkerOptions(new HashSet<Guid> { TenantA }, "worker:identity-resolution"),
            _ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            worker.ProcessAsync(Batch([Candidate("SRC-001")]), CancellationToken.None));

        Assert.Empty(repository.Parties());
        Assert.Empty(repository.OutboxFacts());
    }

    [Fact]
    [Trait("Category", "Worker")]
    public async Task Duplicate_page_entries_are_processed_once_by_idempotency_key()
    {
        var harness = WorkerHarness.Create();
        var candidate = Candidate("SRC-001");

        var result = await harness.Worker.ProcessAsync(Batch([candidate, candidate]), CancellationToken.None);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Single(harness.Repository.Parties());
        Assert.Equal(2, harness.Repository.OutboxFacts().Count);
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Security")]
    public async Task Safe_logs_and_metrics_exclude_raw_or_token_values()
    {
        var harness = WorkerHarness.Create();
        const string token = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        var result = await harness.Worker.ProcessAsync(Batch([Candidate("SRC-001", token)]), CancellationToken.None);
        var logs = string.Join(Environment.NewLine, harness.Logs);
        var serializedMetrics = JsonSerializer.Serialize(result.Metrics);

        Assert.Equal(1, result.Metrics["created"]);
        Assert.DoesNotContain(token, logs);
        Assert.DoesNotContain(token, serializedMetrics);
        Assert.DoesNotContain("123-45-6789", logs);
        Assert.DoesNotContain("raw", logs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Integration")]
    public async Task Api_can_read_mapping_created_by_worker()
    {
        var harness = WorkerHarness.Create();
        await harness.Worker.ProcessAsync(Batch([Candidate("SRC-001")]), CancellationToken.None);
        var query = new CurrentMappingQueryService(new InMemoryCurrentMappingRepository(harness.Repository));

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            new MappingRequestContext(true, TenantA, new HashSet<Guid> { TenantA }, CorrelationId),
            query,
            _ => { },
            CancellationToken.None);

        Assert.Equal(200, response.StatusCode);
        var body = Assert.IsType<MappingApiResponse>(response.Body);
        Assert.Equal(TenantA, body.TenantId);
        Assert.Equal("active", body.Status);
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Security")]
    public async Task Cross_tenant_candidate_collision_is_deferred_without_link()
    {
        var harness = WorkerHarness.Create();

        var result = await harness.Worker.ProcessAsync(
            Batch([
                Candidate("SRC-001"),
                Candidate("SRC-001", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", tenantId: TenantB)
            ]),
            CancellationToken.None);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.DeferredCount);
        Assert.Empty(harness.Repository.ActiveLinks(TenantB, "nexus", "SRC-001"));
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Security")]
    public async Task Same_token_in_different_authorized_tenant_batches_is_deferred_without_link()
    {
        var harness = WorkerHarness.Create(new HashSet<Guid> { TenantA, TenantB });
        await harness.Worker.ProcessAsync(Batch([Candidate("SRC-001")], tenantId: TenantA), CancellationToken.None);

        var result = await harness.Worker.ProcessAsync(Batch([Candidate("SRC-999")], tenantId: TenantB), CancellationToken.None);

        Assert.Equal(1, result.DeferredCount);
        Assert.Empty(harness.Repository.ActiveLinks(TenantB, "nexus", "SRC-999"));
        Assert.Contains(harness.Logs, log => log.Contains("outcome=cross_tenant_token_collision", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Integration")]
    public async Task Idempotency_key_requires_operation_component()
    {
        var harness = WorkerHarness.Create();

        var result = await harness.Worker.ProcessAsync(
            Batch([Candidate("SRC-001", idempotencyKey: $"{TenantA}|nexus|SRC-001|source-page-v1|rule-version-2026-07-30")]),
            CancellationToken.None);

        Assert.Equal(1, result.DeferredCount);
        Assert.Empty(harness.Repository.Parties());
    }

    [Fact]
    [Trait("Category", "Worker")]
    [Trait("Category", "Observability")]
    public void Worker_exposes_approved_telemetry_sources_and_safe_metric_names()
    {
        Assert.Equal("Bluto.Worker.Resolution", ScheduledResolutionWorker.Telemetry.ActivitySource.Name);
        Assert.Equal("Bluto.Worker.Resolution", ScheduledResolutionWorker.Telemetry.Meter.Name);
        Assert.Contains("bluto.identity_resolution.candidates", ScheduledResolutionWorker.Telemetry.MetricNames);
        Assert.Contains("bluto.identity_resolution.failed_partitions", ScheduledResolutionWorker.Telemetry.MetricNames);
        Assert.Contains("bluto.identity_resolution.outbox_lag_ms", ScheduledResolutionWorker.Telemetry.MetricNames);
    }
    private static SyntheticCandidateBatch Batch(IReadOnlyList<SyntheticSourceCandidate> candidates, Guid? tenantId = null) =>
        new(
            "batch-2026-07-30-001",
            tenantId ?? TenantA,
            "nexus",
            CorrelationId,
            "ruleset-1.0.0",
            candidates);

    private static SyntheticSourceCandidate Candidate(
        string sourceKey,
        string token = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        string outcome = "deterministic_no_match",
        Guid? tenantId = null,
        string? idempotencyKey = null) =>
        new(
            tenantId,
            sourceKey,
            token,
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            "source-page-v1",
            "rule-exact-token",
            "rule-version-2026-07-30",
            outcome,
            idempotencyKey ?? $"{tenantId ?? TenantA}|nexus|{sourceKey}|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link");

    private static string TestDataPath(string fileName)
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return Path.Combine(directory ?? throw new InvalidOperationException("Repository root not found."), "implementation", "testdata", fileName);
    }

    private sealed class WorkerHarness
    {
        private WorkerHarness(InMemoryPartyRepository repository, ScheduledResolutionWorker worker, List<string> logs)
        {
            Repository = repository;
            Worker = worker;
            Logs = logs;
        }

        public InMemoryPartyRepository Repository { get; }

        public ScheduledResolutionWorker Worker { get; }

        public List<string> Logs { get; }

        public static WorkerHarness Create(IReadOnlySet<Guid>? authorizedTenantIds = null)
        {
            var logs = new List<string>();
            var repository = new InMemoryPartyRepository();
            var worker = new ScheduledResolutionWorker(
                new IdentityResolutionService(repository, logs.Add),
                new ScheduledResolutionWorkerOptions(authorizedTenantIds ?? new HashSet<Guid> { TenantA }, "worker:identity-resolution"),
                logs.Add);
            return new WorkerHarness(repository, worker, logs);
        }
    }

}





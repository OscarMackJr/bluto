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
    [Trait("Category", "Integration")]
    public async Task Broker_disabled_publisher_drains_pending_facts_without_external_transport()
    {
        var fact = Fact("aaaaaaaa-0000-0000-0000-000000000001", "PartyCreated", createdAt: Now.AddMinutes(-3));
        var store = new InMemoryOutboxStore([fact]);
        var publisher = Publisher(store, brokerEnabled: false);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(1, result.PublishedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.DeadLetteredCount);
        Assert.Single(store.PublishedFacts);
        Assert.Equal(fact.EventId, store.PublishedFacts[0].EventId);
        Assert.Equal(180_000, result.Metrics["outbox_lag_ms"]);
    }

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Integration")]
    public async Task Duplicate_event_ids_are_suppressed_by_the_publisher()
    {
        var eventId = "aaaaaaaa-0000-0000-0000-000000000010";
        var store = new InMemoryOutboxStore([Fact(eventId, "PartyCreated"), Fact(eventId, "PartyCreated")]);
        var transport = new RecordingTransport();
        var publisher = Publisher(store, transport: transport, brokerEnabled: true);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(1, result.PublishedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Single(transport.PublishedFacts);
        Assert.Equal(2, store.PublishedFacts.Count);
        Assert.Equal(1, result.Metrics["duplicate_suppressions"]);
    }

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Security")]
    public async Task Publisher_claims_only_the_requested_tenant_partition()
    {
        var tenantB = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var tenantAFact = Fact("aaaaaaaa-0000-0000-0000-000000000020", "PartyCreated", tenantId: TenantA);
        var tenantBFact = Fact("aaaaaaaa-0000-0000-0000-000000000021", "PartyCreated", tenantId: tenantB);
        var store = new InMemoryOutboxStore([tenantAFact, tenantBFact]);
        var transport = new RecordingTransport();
        var publisher = Publisher(store, transport: transport, brokerEnabled: true);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(1, result.PublishedCount);
        Assert.Single(transport.PublishedFacts);
        Assert.Equal(TenantA, transport.PublishedFacts[0].TenantId);
        Assert.DoesNotContain(store.PublishedFacts, fact => fact.TenantId == tenantB);
    }

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Contract")]
    [Trait("Category", "Security")]
    public async Task Publisher_validates_contract_before_transport_and_dead_letters_invalid_payloads()
    {
        var invalid = Fact(
            "aaaaaaaa-0000-0000-0000-000000000030",
            "PartyCreated",
            payload: new { raw_identifier = "123-45-6789", party_id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") });
        var store = new InMemoryOutboxStore([invalid]);
        var transport = new RecordingTransport();
        var logs = new List<string>();
        var publisher = new OutboxPublisher(
            store,
            transport,
            OutboxContractValidator.Governed,
            new OutboxPublisherOptions(BrokerEnabled: true, MaxAttempts: 1),
            logs.Add);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(0, result.PublishedCount);
        Assert.Equal(1, result.DeadLetteredCount);
        Assert.Empty(transport.PublishedFacts);
        Assert.Single(store.DeadLetters);
        Assert.DoesNotContain("123-45-6789", string.Join(Environment.NewLine, logs), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_identifier", string.Join(Environment.NewLine, logs), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Integration")]
    public async Task Retryable_transport_failure_keeps_fact_pending_with_sanitized_error_code()
    {
        var store = new InMemoryOutboxStore([Fact("aaaaaaaa-0000-0000-0000-000000000040", "PartyCreated")]);
        var logs = new List<string>();
        var publisher = new OutboxPublisher(
            store,
            new FailingTransport("service_bus_unavailable"),
            OutboxContractValidator.Permissive,
            new OutboxPublisherOptions(BrokerEnabled: true, MaxAttempts: 3),
            logs.Add);

        var result = await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.Single(store.Retries);
        Assert.Equal("service_bus_unavailable", store.Retries[0].ErrorCode);
        Assert.Contains(logs, log => log.Contains("outcome=retry_scheduled", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Security")]
    public async Task Transport_error_codes_are_sanitized_before_state_or_logs()
    {
        var store = new InMemoryOutboxStore([Fact("aaaaaaaa-0000-0000-0000-000000000050", "PartyCreated")]);
        var logs = new List<string>();
        var publisher = new OutboxPublisher(
            store,
            new FailingTransport("raw_identifier:123-45-6789"),
            OutboxContractValidator.Permissive,
            new OutboxPublisherOptions(BrokerEnabled: true, MaxAttempts: 3),
            logs.Add);

        await publisher.DrainAsync(new OutboxPublisherBatch(TenantA, MaxFacts: 10, Now), CancellationToken.None);

        Assert.Equal("publish_failed", store.Retries[0].ErrorCode);
        Assert.DoesNotContain("raw_identifier", string.Join(Environment.NewLine, logs), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("123-45-6789", string.Join(Environment.NewLine, logs), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Observability")]
    public void Publisher_exposes_approved_telemetry_source_and_metric_names()
    {
        Assert.Equal("Bluto.Worker.Outbox", OutboxPublisher.Telemetry.ActivitySource.Name);
        Assert.Equal("Bluto.Worker.Outbox", OutboxPublisher.Telemetry.Meter.Name);
        Assert.Contains("bluto.outbox.lag_ms", OutboxPublisher.Telemetry.MetricNames);
        Assert.Contains("bluto.outbox.publish_attempts", OutboxPublisher.Telemetry.MetricNames);
        Assert.Contains("bluto.outbox.duplicate_suppressions", OutboxPublisher.Telemetry.MetricNames);
        Assert.Contains("bluto.outbox.dead_letters", OutboxPublisher.Telemetry.MetricNames);
        Assert.Contains("bluto.outbox.publish_latency_ms", OutboxPublisher.Telemetry.MetricNames);
    }

    private static OutboxFact Fact(string eventId, string eventType, DateTimeOffset? createdAt = null, Guid? tenantId = null, object? payload = null) =>
        new(
            Guid.Parse(eventId),
            eventType,
            "1.0.0",
            createdAt ?? Now,
            createdAt ?? Now,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            1,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            null,
            tenantId ?? TenantA,
            "Bluto.IdentityResolution",
            "0.1.0",
            "Party",
            $"{eventType}.v1",
            "Restricted",
            payload ?? new { party_id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), tenant_id = tenantId ?? TenantA });

    private sealed class InMemoryOutboxStore : IOutboxStore
    {
        private readonly List<OutboxStoredFact> pending;
        private readonly List<(OutboxStoredFact Fact, int AttemptCount, string ErrorCode)> retries = [];
        private readonly List<(OutboxStoredFact Fact, int AttemptCount, string ErrorCode)> deadLetters = [];

        public InMemoryOutboxStore(IEnumerable<OutboxFact> facts)
        {
            pending = facts.Select(fact => new OutboxStoredFact(fact, AttemptCount: 0, PublishState: "pending", CreatedAt: fact.OccurredAt)).ToList();
        }

        public List<OutboxFact> PublishedFacts { get; } = [];

        public IReadOnlyList<(OutboxStoredFact Fact, int AttemptCount, string ErrorCode)> Retries => retries;

        public IReadOnlyList<(OutboxStoredFact Fact, int AttemptCount, string ErrorCode)> DeadLetters => deadLetters;

        public Task<IReadOnlyList<OutboxStoredFact>> ClaimPendingAsync(Guid tenantId, int maxFacts, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<OutboxStoredFact>>(
                pending
                    .Where(fact => fact.Fact.TenantId == tenantId && fact.PublishState.Equals("pending", StringComparison.Ordinal))
                    .OrderBy(fact => fact.CreatedAt)
                    .ThenBy(fact => fact.Fact.EventId)
                    .Take(maxFacts)
                    .ToArray());
        }

        public Task MarkPublishedAsync(OutboxStoredFact fact, DateTimeOffset publishedAt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = publishedAt;
            PublishedFacts.Add(fact.Fact);
            pending.Remove(fact);
            return Task.CompletedTask;
        }

        public Task MarkPendingRetryAsync(OutboxStoredFact fact, int attemptCount, DateTimeOffset nextAttemptAt, string errorCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = nextAttemptAt;
            retries.Add((fact, attemptCount, errorCode));
            pending.Remove(fact);
            pending.Add(fact with { AttemptCount = attemptCount });
            return Task.CompletedTask;
        }

        public Task MarkDeadLetteredAsync(OutboxStoredFact fact, int attemptCount, string errorCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            deadLetters.Add((fact, attemptCount, errorCode));
            pending.Remove(fact);
            return Task.CompletedTask;
        }
    }
    private static OutboxPublisher Publisher(InMemoryOutboxStore store, IOutboxTransport? transport = null, bool brokerEnabled = false) =>
        new(
            store,
            transport ?? OutboxTransport.Disabled,
            OutboxContractValidator.Permissive,
            new OutboxPublisherOptions(BrokerEnabled: brokerEnabled, MaxAttempts: 3),
            _ => { });

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

    private sealed class FailingTransport : IOutboxTransport
    {
        private readonly string code;

        public FailingTransport(string code)
        {
            this.code = code;
        }

        public Task<OutboxPublishAck> PublishAsync(OutboxFact fact, CancellationToken cancellationToken)
        {
            _ = fact;
            cancellationToken.ThrowIfCancellationRequested();
            throw new OutboxTransportException(code);
        }
    }
}

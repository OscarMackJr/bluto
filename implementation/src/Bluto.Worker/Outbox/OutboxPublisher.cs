using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bluto.Application.Identity;

namespace Bluto.Worker.Outbox;

public sealed record OutboxPublisherBatch(Guid TenantId, int MaxFacts, DateTimeOffset Now);

public sealed record OutboxPublisherOptions(bool BrokerEnabled, int MaxAttempts);

public sealed record OutboxPublishResult(
    Guid TenantId,
    int PublishedCount,
    int DuplicateCount,
    int FailedCount,
    int DeadLetteredCount,
    IReadOnlyDictionary<string, int> Metrics);

public sealed record OutboxStoredFact(OutboxFact Fact, int AttemptCount, string PublishState, DateTimeOffset CreatedAt);

public sealed record OutboxPublishAck(Guid EventId);

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxStoredFact>> ClaimPendingAsync(Guid tenantId, int maxFacts, CancellationToken cancellationToken);

    Task MarkPublishedAsync(OutboxStoredFact fact, DateTimeOffset publishedAt, CancellationToken cancellationToken);

    Task MarkPendingRetryAsync(OutboxStoredFact fact, int attemptCount, DateTimeOffset nextAttemptAt, string errorCode, CancellationToken cancellationToken);

    Task MarkDeadLetteredAsync(OutboxStoredFact fact, int attemptCount, string errorCode, CancellationToken cancellationToken);
}

public interface IOutboxTransport
{
    Task<OutboxPublishAck> PublishAsync(OutboxFact fact, CancellationToken cancellationToken);
}

public interface IOutboxContractValidator
{
    void Validate(OutboxFact fact);
}

public sealed class OutboxPublisher
{
    public static class Telemetry
    {
        public const string Name = "Bluto.Worker.Outbox";
        public static readonly ActivitySource ActivitySource = new(Name);
        public static readonly Meter Meter = new(Name);
        public static readonly IReadOnlySet<string> MetricNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "bluto.outbox.publish_attempts",
            "bluto.outbox.duplicate_suppressions",
            "bluto.outbox.dead_letters",
            "bluto.outbox.publish_latency_ms",
            "bluto.outbox.lag_ms"
        };

        internal static readonly Counter<long> PublishAttemptCounter = Meter.CreateCounter<long>("bluto.outbox.publish_attempts");
        internal static readonly Counter<long> DuplicateSuppressionCounter = Meter.CreateCounter<long>("bluto.outbox.duplicate_suppressions");
        internal static readonly Counter<long> DeadLetterCounter = Meter.CreateCounter<long>("bluto.outbox.dead_letters");
        internal static readonly Histogram<double> PublishLatencyMs = Meter.CreateHistogram<double>("bluto.outbox.publish_latency_ms");
        internal static readonly Histogram<double> LagMs = Meter.CreateHistogram<double>("bluto.outbox.lag_ms");
    }

    private readonly IOutboxStore store;
    private readonly IOutboxTransport transport;
    private readonly IOutboxContractValidator validator;
    private readonly OutboxPublisherOptions options;
    private readonly Action<string> log;
    private readonly HashSet<Guid> publishedEventIds = [];

    public OutboxPublisher(
        IOutboxStore store,
        IOutboxTransport transport,
        IOutboxContractValidator validator,
        OutboxPublisherOptions options,
        Action<string> log)
    {
        this.store = store;
        this.transport = transport;
        this.validator = validator;
        this.options = options;
        this.log = log;
    }

    public async Task<OutboxPublishResult> DrainAsync(OutboxPublisherBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batch.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant scope is required.", nameof(batch));
        }

        using var activity = Telemetry.ActivitySource.StartActivity("outbox_publish_batch");
        activity?.SetTag("tenant.id", batch.TenantId);
        activity?.SetTag("broker.enabled", options.BrokerEnabled);

        var records = await store.ClaimPendingAsync(batch.TenantId, batch.MaxFacts, cancellationToken);
        var published = 0;
        var duplicates = 0;
        var failed = 0;
        var deadLettered = 0;
        var maxLagMs = 0;

        foreach (var record in records.OrderBy(record => record.CreatedAt).ThenBy(record => record.Fact.EventId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lagMs = Math.Max(0, (int)(batch.Now - record.CreatedAt).TotalMilliseconds);
            maxLagMs = Math.Max(maxLagMs, lagMs);
            Telemetry.LagMs.Record(lagMs, new KeyValuePair<string, object?>("tenant_id", batch.TenantId));

            if (!publishedEventIds.Add(record.Fact.EventId))
            {
                duplicates++;
                Telemetry.DuplicateSuppressionCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", batch.TenantId));
                await store.MarkPublishedAsync(record, batch.Now, cancellationToken);
                log($"outbox_publish outcome=duplicate_suppressed tenant_id={batch.TenantId:D} event_id={record.Fact.EventId:D}");
                continue;
            }

            try
            {
                validator.Validate(record.Fact);
                Telemetry.PublishAttemptCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", batch.TenantId));
                var started = batch.Now;
                _ = options.BrokerEnabled ? await transport.PublishAsync(record.Fact, cancellationToken) : await OutboxTransport.Disabled.PublishAsync(record.Fact, cancellationToken);
                Telemetry.PublishLatencyMs.Record(Math.Max(0, (batch.Now - started).TotalMilliseconds), new KeyValuePair<string, object?>("tenant_id", batch.TenantId));
                await store.MarkPublishedAsync(record, batch.Now, cancellationToken);
                published++;
                log($"outbox_publish outcome=published tenant_id={batch.TenantId:D} event_id={record.Fact.EventId:D}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var attemptCount = record.AttemptCount + 1;
                var errorCode = SafeErrorCode(ex);
                if (attemptCount >= options.MaxAttempts)
                {
                    await store.MarkDeadLetteredAsync(record, attemptCount, errorCode, cancellationToken);
                    Telemetry.DeadLetterCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", batch.TenantId));
                    deadLettered++;
                    log($"outbox_publish outcome=dead_lettered tenant_id={batch.TenantId:D} event_id={record.Fact.EventId:D} error_code={errorCode}");
                }
                else
                {
                    await store.MarkPendingRetryAsync(record, attemptCount, batch.Now.AddSeconds(30 * attemptCount), errorCode, cancellationToken);
                    failed++;
                    log($"outbox_publish outcome=retry_scheduled tenant_id={batch.TenantId:D} event_id={record.Fact.EventId:D} error_code={errorCode}");
                }
            }
        }

        var metrics = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["published"] = published,
            ["duplicate_suppressions"] = duplicates,
            ["failed"] = failed,
            ["dead_letters"] = deadLettered,
            ["outbox_lag_ms"] = maxLagMs
        };

        return new OutboxPublishResult(batch.TenantId, published, duplicates, failed, deadLettered, metrics);
    }

    private static string SafeErrorCode(Exception exception) =>
        exception switch
        {
            OutboxContractValidationException => "contract_validation_failed",
            OutboxTransportException transportException => SanitizeErrorCode(transportException.Code),
            _ => "publish_failed"
        };

    private static string SanitizeErrorCode(string code)
    {
        if (!SafeErrorCodePattern.IsMatch(code))
        {
            return "publish_failed";
        }

        return OutboxContractValidator.ContainsProhibitedTerm(code) ? "publish_failed" : code;
    }

    private static readonly Regex SafeErrorCodePattern = new("^[a-z0-9_]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
}

public sealed class OutboxTransportException : Exception
{
    public OutboxTransportException(string code)
        : base("Outbox publish failed.")
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class OutboxContractValidationException : Exception
{
    public OutboxContractValidationException(string code)
        : base("Outbox contract validation failed.")
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class OutboxContractValidator : IOutboxContractValidator
{
    private static readonly HashSet<string> EventTypes = new(StringComparer.Ordinal)
    {
        "PartyCreated",
        "SourceLinkEstablished"
    };

    internal static readonly string[] ProhibitedTerms =
    [
        "raw_identifier",
        "rawStrongIdentifier",
        "identity_token",
        "identityToken",
        "ssn",
        "tax_id",
        "taxId",
        "source_payload"
    ];

    public static readonly IOutboxContractValidator Permissive = new PermissiveOutboxContractValidator();

    public static readonly IOutboxContractValidator Governed = new OutboxContractValidator();

    public void Validate(OutboxFact fact)
    {
        if (!EventTypes.Contains(fact.EventType) || !fact.EventVersion.Equals("1.0.0", StringComparison.Ordinal))
        {
            throw new OutboxContractValidationException("unsupported_event_contract");
        }

        var serialized = JsonSerializer.Serialize(fact);
        if (ContainsProhibitedTerm(serialized))
        {
            throw new OutboxContractValidationException("sensitive_payload_rejected");
        }
    }

    internal static bool ContainsProhibitedTerm(string value) =>
        ProhibitedTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private sealed class PermissiveOutboxContractValidator : IOutboxContractValidator
    {
        public void Validate(OutboxFact fact)
        {
            _ = fact;
        }
    }
}

public sealed class OutboxTransport : IOutboxTransport
{
    public static readonly IOutboxTransport Disabled = new OutboxTransport();

    private OutboxTransport()
    {
    }

    public Task<OutboxPublishAck> PublishAsync(OutboxFact fact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new OutboxPublishAck(fact.EventId));
    }
}

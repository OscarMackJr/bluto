using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Bluto.Application.Identity;

namespace Bluto.Worker.Resolution;

public sealed record SyntheticCandidateBatch(
    string BatchId,
    Guid TenantId,
    string SourceSystem,
    Guid CorrelationId,
    string RulesetVersion,
    IReadOnlyList<SyntheticSourceCandidate> Candidates);

public sealed record SyntheticSourceCandidate(
    Guid? TenantId,
    string SourceKey,
    string SyntheticIdentityToken,
    DateTimeOffset EffectiveFrom,
    string SourceVersion,
    string MatchRuleId,
    string RuleVersionId,
    string Outcome,
    string IdempotencyKey);

public sealed record ScheduledResolutionWorkerOptions(
    IReadOnlySet<Guid> AuthorizedTenantIds,
    string ActorId);

public sealed record ScheduledResolutionBatchResult(
    string BatchId,
    IReadOnlyList<string> ProcessedSourceKeys,
    int CreatedCount,
    int ReplayedCount,
    int DeferredCount,
    int DuplicateCount,
    IReadOnlyDictionary<string, int> Metrics);

public interface ISourceTokenCollisionRegistry
{
    bool TryRegister(Guid tenantId, string sourceSystem, string syntheticIdentityToken, string sourceKey);
}

public sealed class InMemorySourceTokenCollisionRegistry : ISourceTokenCollisionRegistry
{
    private readonly Dictionary<string, (Guid TenantId, string SourceKey)> tokens = new(StringComparer.Ordinal);

    public bool TryRegister(Guid tenantId, string sourceSystem, string syntheticIdentityToken, string sourceKey)
    {
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{sourceSystem}|{syntheticIdentityToken}")));
        if (tokens.TryGetValue(digest, out var existing))
        {
            return existing.TenantId == tenantId && existing.SourceKey.Equals(sourceKey, StringComparison.Ordinal);
        }

        tokens.Add(digest, (tenantId, sourceKey));
        return true;
    }
}

public sealed class ScheduledResolutionWorker
{
    public static class Telemetry
    {
        public const string Name = "Bluto.Worker.Resolution";
        public static readonly ActivitySource ActivitySource = new(Name);
        public static readonly Meter Meter = new(Name);
        public static readonly IReadOnlySet<string> MetricNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "bluto.identity_resolution.candidates",
            "bluto.identity_resolution.failed_partitions",
            "bluto.identity_resolution.outbox_lag_ms"
        };

        internal static readonly Counter<long> CandidateCounter = Meter.CreateCounter<long>("bluto.identity_resolution.candidates");
        internal static readonly Counter<long> FailedPartitionCounter = Meter.CreateCounter<long>("bluto.identity_resolution.failed_partitions");
        internal static readonly Histogram<double> OutboxLagMs = Meter.CreateHistogram<double>("bluto.identity_resolution.outbox_lag_ms");
    }

    private const string CreatePartyOperation = "create_party_with_initial_link";
    private static readonly Regex SyntheticTokenPattern = new("^sha256:[a-f0-9]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly IdentityResolutionService resolutionService;
    private readonly ScheduledResolutionWorkerOptions options;
    private readonly Action<string> log;
    private readonly ISourceTokenCollisionRegistry collisionRegistry;

    public ScheduledResolutionWorker(
        IdentityResolutionService resolutionService,
        ScheduledResolutionWorkerOptions options,
        Action<string> log,
        ISourceTokenCollisionRegistry? collisionRegistry = null)
    {
        this.resolutionService = resolutionService;
        this.options = options;
        this.log = log;
        this.collisionRegistry = collisionRegistry ?? new InMemorySourceTokenCollisionRegistry();
    }

    public async Task<ScheduledResolutionBatchResult> ProcessAsync(SyntheticCandidateBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var activity = Telemetry.ActivitySource.StartActivity("scheduled_resolution_batch");
        activity?.SetTag("batch.id", batch.BatchId);
        activity?.SetTag("tenant.id", batch.TenantId);
        activity?.SetTag("ruleset.version", batch.RulesetVersion);

        if (batch.TenantId == Guid.Empty || !options.AuthorizedTenantIds.Contains(batch.TenantId))
        {
            Telemetry.FailedPartitionCounter.Add(1, new KeyValuePair<string, object?>("reason", "tenant_scope_forbidden"));
            log($"scheduled_resolution_batch outcome=tenant_scope_forbidden batch_id={batch.BatchId} correlation_id={batch.CorrelationId:D}");
            throw new UnauthorizedAccessException("Batch tenant scope is not authorized.");
        }

        var processed = new List<string>();
        var seenIdempotencyKeys = new HashSet<string>(StringComparer.Ordinal);
        var created = 0;
        var replayed = 0;
        var deferred = 0;
        var duplicates = 0;

        foreach (var candidate in batch.Candidates.OrderBy(candidate => candidate.SourceKey, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!seenIdempotencyKeys.Add(candidate.IdempotencyKey))
            {
                duplicates++;
                Telemetry.CandidateCounter.Add(1, new KeyValuePair<string, object?>("outcome", "duplicate"));
                log($"scheduled_resolution_candidate outcome=duplicate batch_id={batch.BatchId} correlation_id={batch.CorrelationId:D}");
                continue;
            }

            var deferReason = DeferReason(batch, candidate);
            if (deferReason is not null)
            {
                deferred++;
                Telemetry.CandidateCounter.Add(1, new KeyValuePair<string, object?>("outcome", deferReason));
                log($"scheduled_resolution_candidate outcome={deferReason} batch_id={batch.BatchId} correlation_id={batch.CorrelationId:D}");
                continue;
            }

            var result = await resolutionService.ResolveAsync(ToCommand(batch, candidate), cancellationToken);
            processed.Add(candidate.SourceKey);
            Telemetry.CandidateCounter.Add(1, new KeyValuePair<string, object?>("outcome", result.Created ? "created" : "replayed"));
            Telemetry.OutboxLagMs.Record(0, new KeyValuePair<string, object?>("source_system", batch.SourceSystem));

            if (result.Created)
            {
                created++;
            }
            else
            {
                replayed++;
            }
        }

        var metrics = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["created"] = created,
            ["replayed"] = replayed,
            ["deferred"] = deferred,
            ["duplicates"] = duplicates,
            ["processed"] = processed.Count,
            ["failed_partitions"] = 0,
            ["outbox_lag_ms"] = 0
        };

        log($"scheduled_resolution_batch outcome=completed batch_id={batch.BatchId} created={created} replayed={replayed} deferred={deferred} duplicates={duplicates} correlation_id={batch.CorrelationId:D}");
        return new ScheduledResolutionBatchResult(batch.BatchId, processed, created, replayed, deferred, duplicates, metrics);
    }

    private string? DeferReason(SyntheticCandidateBatch batch, SyntheticSourceCandidate candidate)
    {
        if (candidate.TenantId is not null && candidate.TenantId != batch.TenantId)
        {
            return "tenant_scope_conflict";
        }

        if (!candidate.Outcome.Equals("deterministic_no_match", StringComparison.Ordinal))
        {
            return "deferred";
        }

        if (!SyntheticTokenPattern.IsMatch(candidate.SyntheticIdentityToken))
        {
            return "invalid_synthetic_token";
        }

        if (!candidate.IdempotencyKey.EndsWith($"|{CreatePartyOperation}", StringComparison.Ordinal))
        {
            return "invalid_idempotency_key";
        }

        if (!collisionRegistry.TryRegister(batch.TenantId, batch.SourceSystem, candidate.SyntheticIdentityToken, candidate.SourceKey))
        {
            return "cross_tenant_token_collision";
        }

        return null;
    }

    private ResolveSourceCandidateCommand ToCommand(SyntheticCandidateBatch batch, SyntheticSourceCandidate candidate) =>
        new(
            DeterministicGuid($"{batch.BatchId}|{candidate.IdempotencyKey}|command"),
            batch.TenantId,
            options.AuthorizedTenantIds,
            batch.SourceSystem,
            candidate.SourceKey,
            candidate.SyntheticIdentityToken,
            candidate.EffectiveFrom,
            candidate.EffectiveFrom,
            candidate.IdempotencyKey,
            batch.CorrelationId,
            null,
            options.ActorId,
            candidate.MatchRuleId,
            candidate.RuleVersionId,
            batch.RulesetVersion,
            candidate.SourceVersion,
            $"evidence://synthetic/{batch.BatchId}/{candidate.SourceKey}",
            RawStrongIdentifier: null);

    private static Guid DeterministicGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}

using System.Text.Json.Serialization;
using Bluto.Domain.Identity;

namespace Bluto.Application.Identity;

public sealed record ResolveSourceCandidateCommand(
    Guid CommandId,
    Guid TenantId,
    IReadOnlySet<Guid> AuthorizedTenantIds,
    string SourceSystem,
    string SourceKey,
    string IdentityToken,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset RequestedAt,
    string IdempotencyKey,
    Guid CorrelationId,
    Guid? CausationId,
    string ActorId,
    string MatchRuleId,
    string RuleVersionId,
    string RulesetVersion,
    string SourceVersion,
    string EvidenceReference,
    string? RawStrongIdentifier);

public sealed record IdentityResolutionResult(Party Party, bool Created);

public interface IPartyRepository
{
    Task<Party?> FindIdempotentResultAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken);

    Task RecordIdempotencyAsync(string idempotencyKey, PartyId partyId, CancellationToken cancellationToken);

    Task CommitNewPartyWithLinkAndOutboxAsync(Party party, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken);
}

public sealed class IdentityResolutionService
{
    private readonly IPartyRepository repository;
    private readonly Action<string> log;

    public IdentityResolutionService(IPartyRepository repository, Action<string> log)
    {
        this.repository = repository;
        this.log = log;
    }

    public async Task<IdentityResolutionResult> ResolveAsync(ResolveSourceCandidateCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(command);

        if (!command.AuthorizedTenantIds.Contains(command.TenantId))
        {
            log("identity_resolution_denied tenant_scope=unauthorized");
            throw new UnauthorizedAccessException("Tenant scope is not authorized.");
        }

        var replay = await repository.FindIdempotentResultAsync(command.IdempotencyKey, cancellationToken);
        if (replay is not null)
        {
            log("identity_resolution_replayed outcome=idempotent");
            return new IdentityResolutionResult(replay, Created: false);
        }

        var existing = await repository.FindActivePartyAsync(command.TenantId, command.SourceSystem, command.SourceKey, cancellationToken);
        if (existing is not null)
        {
            await repository.RecordIdempotencyAsync(command.IdempotencyKey, existing.PartyId, cancellationToken);
            return new IdentityResolutionResult(existing, Created: false);
        }

        var party = new Party(PartyId.New(), command.TenantId, command.RequestedAt);
        var provenance = new LinkProvenance(
            command.MatchRuleId,
            command.RuleVersionId,
            command.RulesetVersion,
            "deterministic_exact_token",
            "1.0",
            command.SourceVersion,
            command.EvidenceReference,
            command.CorrelationId,
            command.CausationId,
            command.ActorId,
            command.RequestedAt);
        var link = party.EstablishSourceLink(
            command.TenantId,
            command.SourceSystem,
            command.SourceKey,
            EffectiveInterval.HalfOpen(command.EffectiveFrom, effectiveTo: null),
            provenance);

        var facts = new[]
        {
            OutboxFact.PartyCreated(party, command),
            OutboxFact.SourceLinkEstablished(party, link, command)
        };

        await repository.CommitNewPartyWithLinkAndOutboxAsync(party, command.IdempotencyKey, facts, cancellationToken);
        log("identity_resolution_completed outcome=created");
        return new IdentityResolutionResult(party, Created: true);
    }

    private static void Validate(ResolveSourceCandidateCommand command)
    {
        if (command.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant scope is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.RawStrongIdentifier))
        {
            throw new ArgumentException("Raw strong identifiers are prohibited; provide only approved canonical tokens.");
        }

        if (string.IsNullOrWhiteSpace(command.SourceSystem)
            || string.IsNullOrWhiteSpace(command.SourceKey)
            || string.IsNullOrWhiteSpace(command.IdentityToken)
            || string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || string.IsNullOrWhiteSpace(command.MatchRuleId)
            || string.IsNullOrWhiteSpace(command.RuleVersionId)
            || string.IsNullOrWhiteSpace(command.RulesetVersion))
        {
            throw new ArgumentException("Command is missing required canonical identity fields.");
        }
    }
}

public sealed class InMemoryPartyRepository : IPartyRepository
{
    private readonly bool failBeforeOutboxCommit;
    private readonly Dictionary<PartyId, Party> parties = [];
    private readonly Dictionary<string, PartyId> idempotency = new(StringComparer.Ordinal);
    private readonly List<OutboxFact> outbox = [];

    public InMemoryPartyRepository(bool failBeforeOutboxCommit = false)
    {
        this.failBeforeOutboxCommit = failBeforeOutboxCommit;
    }

    public Task<Party?> FindIdempotentResultAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindIdempotentResult(idempotencyKey));
    }

    public Party? FindIdempotentResult(string idempotencyKey) =>
        idempotency.TryGetValue(idempotencyKey, out var partyId) && parties.TryGetValue(partyId, out var party)
            ? party
            : null;

    public Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindActiveParty(tenantId, sourceSystem, sourceKey));
    }

    public Party? FindActiveParty(Guid tenantId, string sourceSystem, string sourceKey) =>
        parties.Values.SingleOrDefault(party =>
            party.TenantId == tenantId
            && party.SourceLinks.Any(link =>
                link.TenantId == tenantId
                && link.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
                && link.SourceKey.Equals(sourceKey, StringComparison.Ordinal)
                && link.Status.Equals("active", StringComparison.Ordinal)
                && link.EffectiveInterval.EffectiveTo is null));

    public Task RecordIdempotencyAsync(string idempotencyKey, PartyId partyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RecordIdempotency(idempotencyKey, partyId);
        return Task.CompletedTask;
    }

    public void RecordIdempotency(string idempotencyKey, PartyId partyId)
    {
        idempotency.TryAdd(idempotencyKey, partyId);
    }

    public Task CommitNewPartyWithLinkAndOutboxAsync(Party party, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommitNewPartyWithLinkAndOutbox(party, idempotencyKey, facts);
        return Task.CompletedTask;
    }

    public void CommitNewPartyWithLinkAndOutbox(Party party, string idempotencyKey, IReadOnlyList<OutboxFact> facts)
    {
        if (failBeforeOutboxCommit)
        {
            throw new InvalidOperationException("Simulated atomic transaction failure.");
        }

        var partiesCopy = new Dictionary<PartyId, Party>(parties);
        var idempotencyCopy = new Dictionary<string, PartyId>(idempotency, StringComparer.Ordinal);
        var outboxCopy = new List<OutboxFact>(outbox);

        if (!partiesCopy.TryAdd(party.PartyId, party))
        {
            throw new InvalidOperationException("Party ID reuse is prohibited.");
        }

        idempotencyCopy.Add(idempotencyKey, party.PartyId);
        outboxCopy.AddRange(facts);

        parties.Clear();
        foreach (var item in partiesCopy)
        {
            parties.Add(item.Key, item.Value);
        }

        idempotency.Clear();
        foreach (var item in idempotencyCopy)
        {
            idempotency.Add(item.Key, item.Value);
        }

        outbox.Clear();
        outbox.AddRange(outboxCopy);
    }

    public IReadOnlyList<PartySourceLink> ActiveLinks(Guid tenantId, string sourceSystem, string sourceKey) =>
        parties.Values
            .SelectMany(party => party.SourceLinks)
            .Where(link =>
                link.TenantId == tenantId
                && link.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
                && link.SourceKey.Equals(sourceKey, StringComparison.Ordinal)
                && link.Status.Equals("active", StringComparison.Ordinal)
                && link.EffectiveInterval.EffectiveTo is null)
            .ToArray();

    public IReadOnlyList<Party> Parties() => parties.Values.ToArray();

    public IReadOnlyList<OutboxFact> OutboxFacts() => outbox.ToArray();

    public object Snapshot() => new
    {
        parties = parties.Values.Select(party => new
        {
            party_id = party.PartyId.ToString(),
            tenant_id = party.TenantId,
            status = party.Status,
            links = party.SourceLinks.Select(link => new
            {
                link_id = link.LinkId.ToString(),
                party_id = link.PartyId.ToString(),
                tenant_id = link.TenantId,
                source_system = link.SourceSystem,
                source_key = link.SourceKey,
                effective_from = link.EffectiveInterval.EffectiveFrom,
                effective_to = link.EffectiveInterval.EffectiveTo,
                status = link.Status,
                provenance = link.Provenance
            })
        })
    };
}

public sealed record OutboxFact(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTimeOffset OccurredAt,
    DateTimeOffset PublishedAt,
    Guid AggregateId,
    int AggregateVersion,
    Guid CorrelationId,
    Guid? CausationId,
    Guid TenantId,
    string Producer,
    string DataClassification,
    object Payload)
{
    public static OutboxFact PartyCreated(Party party, ResolveSourceCandidateCommand command) =>
        new(
            DeterministicEventId(command.IdempotencyKey, "PartyCreated"),
            "PartyCreated",
            "1.0.0",
            command.RequestedAt,
            command.RequestedAt,
            party.PartyId.Value,
            party.Version,
            command.CorrelationId,
            command.CausationId,
            command.TenantId,
            "Bluto.IdentityResolution",
            "Restricted",
            new PartyCreatedPayload(
                party.PartyId.Value,
                "active",
                command.RequestedAt,
                command.ActorId,
                command.CorrelationId,
                ProvenancePayload.From(command)));

    public static OutboxFact SourceLinkEstablished(Party party, PartySourceLink link, ResolveSourceCandidateCommand command) =>
        new(
            DeterministicEventId(command.IdempotencyKey, "SourceLinkEstablished"),
            "SourceLinkEstablished",
            "1.0.0",
            command.RequestedAt,
            command.RequestedAt,
            party.PartyId.Value,
            party.Version,
            command.CorrelationId,
            command.CausationId,
            command.TenantId,
            "Bluto.IdentityResolution",
            "Restricted",
            new SourceLinkEstablishedPayload(
                party.PartyId.Value,
                link.LinkId.Value,
                link.SourceSystem,
                link.SourceKey,
                link.EffectiveInterval.EffectiveFrom,
                link.EffectiveInterval.EffectiveTo,
                "active",
                command.RequestedAt,
                command.ActorId,
                command.CorrelationId,
                ProvenancePayload.From(command)));

    private static Guid DeterministicEventId(string idempotencyKey, string eventType)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{eventType}|{idempotencyKey}"));
        return new Guid(bytes[..16]);
    }
}

public sealed record PartyCreatedPayload(
    [property: JsonPropertyName("party_id")] Guid PartyId,
    [property: JsonPropertyName("party_status")] string PartyStatus,
    [property: JsonPropertyName("asserted_at")] DateTimeOffset AssertedAt,
    [property: JsonPropertyName("asserted_by")] string AssertedBy,
    [property: JsonPropertyName("correlation_id")] Guid CorrelationId,
    [property: JsonPropertyName("provenance")] ProvenancePayload Provenance);

public sealed record SourceLinkEstablishedPayload(
    [property: JsonPropertyName("party_id")] Guid PartyId,
    [property: JsonPropertyName("link_id")] Guid LinkId,
    [property: JsonPropertyName("source_system")] string SourceSystem,
    [property: JsonPropertyName("source_key")] string SourceKey,
    [property: JsonPropertyName("effective_from")] DateTimeOffset EffectiveFrom,
    [property: JsonPropertyName("effective_to")] DateTimeOffset? EffectiveTo,
    [property: JsonPropertyName("link_status")] string LinkStatus,
    [property: JsonPropertyName("asserted_at")] DateTimeOffset AssertedAt,
    [property: JsonPropertyName("asserted_by")] string AssertedBy,
    [property: JsonPropertyName("correlation_id")] Guid CorrelationId,
    [property: JsonPropertyName("provenance")] ProvenancePayload Provenance);

public sealed record ProvenancePayload(
    [property: JsonPropertyName("match_rule_id")] string MatchRuleId,
    [property: JsonPropertyName("rule_version_id")] string RuleVersionId,
    [property: JsonPropertyName("ruleset_version")] string RulesetVersion,
    [property: JsonPropertyName("match_method")] string MatchMethod,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("source_version")] string SourceVersion,
    [property: JsonPropertyName("evidence_reference")] string EvidenceReference)
{
    public static ProvenancePayload From(ResolveSourceCandidateCommand command) =>
        new(
            command.MatchRuleId,
            command.RuleVersionId,
            command.RulesetVersion,
            "deterministic_exact_token",
            "1.0",
            command.SourceVersion,
            command.EvidenceReference);
}

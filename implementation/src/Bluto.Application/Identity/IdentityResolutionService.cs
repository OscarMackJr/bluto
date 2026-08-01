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

public sealed record ReviewCase(
    Guid TenantId,
    string SourceSystem,
    string SourceKey,
    string IdentityToken,
    string Reason,
    Guid CorrelationId,
    DateTimeOffset RecordedAt);

public interface IPartyRepository
{
    Task<Party?> FindIdempotentResultAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken);

    Task<Party?> FindActivePartyByIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken);

    Task<bool> HasCrossTenantIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken);

    Task RecordIdempotencyAsync(string idempotencyKey, PartyId partyId, CancellationToken cancellationToken);

    Task RecordReviewCaseAsync(ReviewCase reviewCase, CancellationToken cancellationToken);

    Task CommitNewPartyWithLinkAndOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken);

    Task CommitSourceLinkWithOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(command.RawStrongIdentifier))
        {
            log($"identity_resolution_rejected outcome=raw_strong_identifier_rejected correlation_id={command.CorrelationId:D}");
            throw new ArgumentException("raw strong identifier rejected; provide only approved canonical tokens.");
        }

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

        if (await repository.HasCrossTenantIdentityTokenAsync(command.TenantId, command.IdentityToken, cancellationToken))
        {
            await RecordReviewCaseAsync(command, "cross_tenant_collision", cancellationToken);
            log($"identity_resolution_rejected outcome=cross_tenant_collision tenant_id={command.TenantId:D} correlation_id={command.CorrelationId:D}");
            return new IdentityResolutionResult(new Party(PartyId.New(), command.TenantId, command.RequestedAt), Created: false);
        }

        var tokenMatch = await repository.FindActivePartyByIdentityTokenAsync(command.TenantId, command.IdentityToken, cancellationToken);
        if (tokenMatch is not null)
        {
            if (tokenMatch.SourceLinks.Any(link =>
                    link.TenantId == command.TenantId
                    && link.SourceSystem.Equals(command.SourceSystem, StringComparison.Ordinal)
                    && !link.SourceKey.Equals(command.SourceKey, StringComparison.Ordinal)
                    && link.Status.Equals(PartySourceLink.Active, StringComparison.Ordinal)
                    && link.EffectiveInterval.EffectiveTo is null))
            {
                await RecordReviewCaseAsync(command, "same_source_identity_conflict", cancellationToken);
                log($"identity_resolution_deferred outcome=same_source_identity_conflict tenant_id={command.TenantId:D} correlation_id={command.CorrelationId:D}");
                return new IdentityResolutionResult(tokenMatch, Created: false);
            }

            var link = EstablishLink(tokenMatch, command);
            var facts = new[] { OutboxFact.SourceLinkEstablished(tokenMatch, link, command) };
            await repository.CommitSourceLinkWithOutboxAsync(tokenMatch, command.IdentityToken, command.IdempotencyKey, facts, cancellationToken);
            log("identity_resolution_completed outcome=linked_existing_party");
            return new IdentityResolutionResult(tokenMatch, Created: false);
        }

        var party = new Party(PartyId.New(), command.TenantId, command.RequestedAt);
        var newLink = EstablishLink(party, command);
        var newFacts = new[]
        {
            OutboxFact.PartyCreated(party, command),
            OutboxFact.SourceLinkEstablished(party, newLink, command)
        };

        await repository.CommitNewPartyWithLinkAndOutboxAsync(party, command.IdentityToken, command.IdempotencyKey, newFacts, cancellationToken);
        log("identity_resolution_completed outcome=created");
        return new IdentityResolutionResult(party, Created: true);
    }

    private static PartySourceLink EstablishLink(Party party, ResolveSourceCandidateCommand command) =>
        party.EstablishSourceLink(
            command.TenantId,
            command.SourceSystem,
            command.SourceKey,
            EffectiveInterval.HalfOpen(command.EffectiveFrom, effectiveTo: null),
            new LinkProvenance(
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
                command.RequestedAt));

    private async Task RecordReviewCaseAsync(ResolveSourceCandidateCommand command, string reason, CancellationToken cancellationToken) =>
        await repository.RecordReviewCaseAsync(
            new ReviewCase(
                command.TenantId,
                command.SourceSystem,
                command.SourceKey,
                command.IdentityToken,
                reason,
                command.CorrelationId,
                command.RequestedAt),
            cancellationToken);

    private static void Validate(ResolveSourceCandidateCommand command)
    {
        if (command.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant scope is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.RawStrongIdentifier))
        {
            throw new ArgumentException("raw strong identifier rejected; provide only approved canonical tokens.");
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

        ValidateIdempotencyKey(command);
    }

    private static void ValidateIdempotencyKey(ResolveSourceCandidateCommand command)
    {
        var parts = command.IdempotencyKey.Split('|', StringSplitOptions.None);
        if (parts.Length != 6 || string.IsNullOrWhiteSpace(parts[5]))
        {
            throw new ArgumentException("Idempotency key must include the operation component.");
        }
    }
}

public sealed class InMemoryPartyRepository : IPartyRepository
{
    private readonly bool failBeforeOutboxCommit;
    private readonly Dictionary<PartyId, Party> parties = [];
    private readonly Dictionary<string, PartyId> idempotency = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PartyId> identityTokens = new(StringComparer.Ordinal);
    private readonly List<ReviewCase> reviewCases = [];
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
                && link.Status.Equals(PartySourceLink.Active, StringComparison.Ordinal)
                && link.EffectiveInterval.EffectiveTo is null));

    public Task<Party?> FindActivePartyByIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            identityTokens.TryGetValue(TokenScope(tenantId, identityToken), out var partyId) && parties.TryGetValue(partyId, out var party)
                ? party
                : null);
    }

    public Task<bool> HasCrossTenantIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(identityTokens.Keys.Any(key => !key.StartsWith($"{tenantId:D}|", StringComparison.Ordinal) && key.EndsWith($"|{identityToken}", StringComparison.Ordinal)));
    }

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

    public Task RecordReviewCaseAsync(ReviewCase reviewCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        reviewCases.Add(reviewCase);
        return Task.CompletedTask;
    }

    public Task CommitNewPartyWithLinkAndOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommitNewPartyWithLinkAndOutbox(party, identityToken, idempotencyKey, facts);
        return Task.CompletedTask;
    }

    public void CommitNewPartyWithLinkAndOutbox(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts)
    {
        if (failBeforeOutboxCommit)
        {
            throw new InvalidOperationException("Simulated atomic transaction failure.");
        }

        var partiesCopy = new Dictionary<PartyId, Party>(parties);
        var idempotencyCopy = new Dictionary<string, PartyId>(idempotency, StringComparer.Ordinal);
        var tokenCopy = new Dictionary<string, PartyId>(identityTokens, StringComparer.Ordinal);
        var outboxCopy = new List<OutboxFact>(outbox);

        if (!partiesCopy.TryAdd(party.PartyId, party))
        {
            throw new InvalidOperationException("Party ID reuse is prohibited.");
        }

        idempotencyCopy.Add(idempotencyKey, party.PartyId);
        tokenCopy.Add(TokenScope(party.TenantId, identityToken), party.PartyId);
        outboxCopy.AddRange(facts);

        ReplaceState(partiesCopy, idempotencyCopy, tokenCopy, outboxCopy);
    }

    public Task CommitSourceLinkWithOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (failBeforeOutboxCommit)
        {
            throw new InvalidOperationException("Simulated atomic transaction failure.");
        }

        var idempotencyCopy = new Dictionary<string, PartyId>(idempotency, StringComparer.Ordinal);
        var tokenCopy = new Dictionary<string, PartyId>(identityTokens, StringComparer.Ordinal);
        var outboxCopy = new List<OutboxFact>(outbox);

        idempotencyCopy.Add(idempotencyKey, party.PartyId);
        tokenCopy.TryAdd(TokenScope(party.TenantId, identityToken), party.PartyId);
        outboxCopy.AddRange(facts);
        ReplaceState(new Dictionary<PartyId, Party>(parties), idempotencyCopy, tokenCopy, outboxCopy);
        return Task.CompletedTask;
    }

    public IReadOnlyList<PartySourceLink> ActiveLinks(Guid tenantId, string sourceSystem, string sourceKey) =>
        parties.Values
            .SelectMany(party => party.SourceLinks)
            .Where(link =>
                link.TenantId == tenantId
                && link.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
                && link.SourceKey.Equals(sourceKey, StringComparison.Ordinal)
                && link.Status.Equals(PartySourceLink.Active, StringComparison.Ordinal)
                && link.EffectiveInterval.EffectiveTo is null)
            .ToArray();

    public IReadOnlyList<Party> Parties() => parties.Values.ToArray();

    public IReadOnlyList<ReviewCase> ReviewCases() => reviewCases.ToArray();

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

    private void ReplaceState(
        Dictionary<PartyId, Party> partiesCopy,
        Dictionary<string, PartyId> idempotencyCopy,
        Dictionary<string, PartyId> tokenCopy,
        List<OutboxFact> outboxCopy)
    {
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

        identityTokens.Clear();
        foreach (var item in tokenCopy)
        {
            identityTokens.Add(item.Key, item.Value);
        }

        outbox.Clear();
        outbox.AddRange(outboxCopy);
    }

    private static string TokenScope(Guid tenantId, string identityToken) => $"{tenantId:D}|{identityToken}";
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
    string ProducerVersion,
    string AggregateType,
    string PayloadSchema,
    string DataClassification,
    object Payload)
{
    [JsonPropertyName("aggregate_type")]
    public string AggregateTypeJson => AggregateType;

    [JsonPropertyName("producer_version")]
    public string ProducerVersionJson => ProducerVersion;

    [JsonPropertyName("payload_schema")]
    public string PayloadSchemaJson => PayloadSchema;

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
            "0.1.0",
            "Party",
            "PartyCreated.v1",
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
            "0.1.0",
            "Party",
            "SourceLinkEstablished.v1",
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

using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bluto.Application.Identity;
using Bluto.Domain.Identity;
using Dapper;
using Npgsql;

namespace Bluto.Infrastructure.Postgres.Identity;

public sealed record PostgresTransactionScript(IReadOnlyList<string> Statements);

public sealed class PostgresPartyRepository : IPartyRepository
{
    private readonly NpgsqlDataSource dataSource;

    public PostgresPartyRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<Party?> FindIdempotentResultAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PartyRow>(new CommandDefinition(
            """
            select p.tenant_id as TenantId, p.party_id as PartyId, p.created_at as CreatedAt
            from identity_resolution.idempotency_records i
            join identity_resolution.parties p on p.tenant_id = i.tenant_id and p.party_id = i.party_id
            where i.idempotency_key = @idempotencyKey
            limit 1
            """,
            new { idempotencyKey },
            cancellationToken: cancellationToken));
        return row is null ? null : await HydratePartyAsync(connection, row.TenantId, row.PartyId, cancellationToken);
    }

    public async Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PartyRow>(new CommandDefinition(
            """
            select p.tenant_id as TenantId, p.party_id as PartyId, p.created_at as CreatedAt
            from identity_resolution.party_source_links l
            join identity_resolution.parties p on p.tenant_id = l.tenant_id and p.party_id = l.party_id
            where l.tenant_id = @tenantId
              and l.source_system = @sourceSystem
              and l.source_key = @sourceKey
              and l.status = 'active'
              and l.effective_to is null
            limit 1
            """,
            new { tenantId, sourceSystem, sourceKey },
            cancellationToken: cancellationToken));
        return row is null ? null : await HydratePartyAsync(connection, row.TenantId, row.PartyId, cancellationToken);
    }

    public async Task<Party?> FindActivePartyByIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var digest = MatchIdentityDigest(identityToken);
        var row = await connection.QuerySingleOrDefaultAsync<PartyRow>(new CommandDefinition(
            """
            select p.tenant_id as TenantId, p.party_id as PartyId, p.created_at as CreatedAt
            from identity_resolution.party_source_links l
            join identity_resolution.parties p on p.tenant_id = l.tenant_id and p.party_id = l.party_id
            where l.tenant_id = @tenantId
              and l.match_identity_digest = @digest
              and l.status = 'active'
              and l.effective_to is null
            order by l.created_at, l.source_link_id
            limit 1
            """,
            new { tenantId, digest },
            cancellationToken: cancellationToken));
        return row is null ? null : await HydratePartyAsync(connection, row.TenantId, row.PartyId, cancellationToken);
    }

    public async Task<bool> HasCrossTenantIdentityTokenAsync(Guid tenantId, string identityToken, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var digest = MatchIdentityDigest(identityToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            select exists (
                select 1
                from identity_resolution.party_source_links
                where tenant_id <> @tenantId
                  and match_identity_digest = @digest
                  and status = 'active'
                  and effective_to is null)
            """,
            new { tenantId, digest },
            cancellationToken: cancellationToken));
    }

    public async Task RecordIdempotencyAsync(string idempotencyKey, PartyId partyId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into identity_resolution.idempotency_records (tenant_id, idempotency_key, party_id, created_at)
            select tenant_id, @idempotencyKey, party_id, now()
            from identity_resolution.parties
            where party_id = @partyId
            on conflict (tenant_id, idempotency_key) do nothing
            """,
            new { idempotencyKey, partyId = partyId.Value },
            cancellationToken: cancellationToken));
    }

    public async Task RecordReviewCaseAsync(ReviewCase reviewCase, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into identity_resolution.review_cases (
                tenant_id,
                review_case_id,
                source_system,
                source_key,
                match_identity_digest,
                reason,
                correlation_id,
                created_at)
            values (
                @tenantId,
                @reviewCaseId,
                @sourceSystem,
                @sourceKey,
                @digest,
                @reason,
                @correlationId,
                @createdAt)
            on conflict (tenant_id, review_case_id) do nothing
            """,
            new
            {
                tenantId = reviewCase.TenantId,
                reviewCaseId = DeterministicGuid($"review|{reviewCase.TenantId:D}|{reviewCase.SourceSystem}|{reviewCase.SourceKey}|{reviewCase.Reason}|{reviewCase.CorrelationId:D}"),
                sourceSystem = reviewCase.SourceSystem,
                sourceKey = reviewCase.SourceKey,
                digest = MatchIdentityDigest(reviewCase.IdentityToken),
                reason = reviewCase.Reason,
                correlationId = reviewCase.CorrelationId,
                createdAt = reviewCase.RecordedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task CommitNewPartyWithLinkAndOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken)
    {
        var link = party.SourceLinks.Single();
        var partyCreated = facts.Single(fact => fact.EventType.Equals("PartyCreated", StringComparison.Ordinal));
        var sourceLinkEstablished = facts.Single(fact => fact.EventType.Equals("SourceLinkEstablished", StringComparison.Ordinal));

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var insertedFacts = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            PostgresIdentityResolutionSql.AtomicCreatePartyWithInitialLinkAndOutboxFacts,
            NewPartyParameters(party, link, identityToken, idempotencyKey, partyCreated, sourceLinkEstablished),
            transaction,
            cancellationToken: cancellationToken));

        if (insertedFacts != 2)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DBConcurrencyException("Authoritative state and outbox facts were not inserted atomically.");
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CommitSourceLinkWithOutboxAsync(Party party, string identityToken, string idempotencyKey, IReadOnlyList<OutboxFact> facts, CancellationToken cancellationToken)
    {
        var link = party.SourceLinks.Last();
        var sourceLinkEstablished = facts.Single(fact => fact.EventType.Equals("SourceLinkEstablished", StringComparison.Ordinal));

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var insertedFacts = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            PostgresIdentityResolutionSql.AtomicCreateSourceLinkForExistingPartyWithOutboxFact,
            ExistingPartyLinkParameters(party, link, identityToken, idempotencyKey, sourceLinkEstablished),
            transaction,
            cancellationToken: cancellationToken));

        if (insertedFacts != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DBConcurrencyException("Source link and outbox fact were not inserted atomically.");
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static object NewPartyParameters(Party party, PartySourceLink link, string identityToken, string idempotencyKey, OutboxFact partyCreated, OutboxFact sourceLinkEstablished) =>
        new
        {
            tenant_id = party.TenantId,
            party_id = party.PartyId.Value,
            created_at = party.CreatedAt,
            created_by = link.Provenance.AssertedBy,
            correlation_id = link.Provenance.CorrelationId,
            version = party.Version,
            source_link_id = link.LinkId.Value,
            source_system = link.SourceSystem,
            source_key = link.SourceKey,
            match_identity_digest = MatchIdentityDigest(identityToken),
            effective_from = link.EffectiveInterval.EffectiveFrom,
            effective_to = link.EffectiveInterval.EffectiveTo,
            status = link.Status,
            rule_id = link.Provenance.MatchRuleId,
            rule_version_id = link.Provenance.RuleVersionId,
            ruleset_version = link.Provenance.RulesetVersion,
            source_version = link.Provenance.SourceVersion,
            evidence_reference = link.Provenance.EvidenceReference,
            idempotency_key = idempotencyKey,
            aggregate_type = partyCreated.AggregateType,
            event_version = partyCreated.EventVersion,
            occurred_at = partyCreated.OccurredAt,
            causation_id = partyCreated.CausationId,
            party_created_outbox_fact_id = partyCreated.EventId,
            party_created_event_type = partyCreated.EventType,
            party_created_payload = JsonSerializer.Serialize(partyCreated.Payload),
            party_created_idempotency_key = $"{idempotencyKey}|PartyCreated",
            source_link_established_outbox_fact_id = sourceLinkEstablished.EventId,
            source_link_established_event_type = sourceLinkEstablished.EventType,
            source_link_established_payload = JsonSerializer.Serialize(sourceLinkEstablished.Payload),
            source_link_established_idempotency_key = $"{idempotencyKey}|SourceLinkEstablished"
        };

    private static object ExistingPartyLinkParameters(Party party, PartySourceLink link, string identityToken, string idempotencyKey, OutboxFact sourceLinkEstablished) =>
        new
        {
            tenant_id = party.TenantId,
            party_id = party.PartyId.Value,
            source_link_id = link.LinkId.Value,
            source_system = link.SourceSystem,
            source_key = link.SourceKey,
            match_identity_digest = MatchIdentityDigest(identityToken),
            effective_from = link.EffectiveInterval.EffectiveFrom,
            effective_to = link.EffectiveInterval.EffectiveTo,
            status = link.Status,
            rule_id = link.Provenance.MatchRuleId,
            rule_version_id = link.Provenance.RuleVersionId,
            ruleset_version = link.Provenance.RulesetVersion,
            source_version = link.Provenance.SourceVersion,
            evidence_reference = link.Provenance.EvidenceReference,
            created_at = link.Provenance.AssertedAt,
            created_by = link.Provenance.AssertedBy,
            correlation_id = link.Provenance.CorrelationId,
            idempotency_key = idempotencyKey,
            aggregate_type = sourceLinkEstablished.AggregateType,
            event_version = sourceLinkEstablished.EventVersion,
            occurred_at = sourceLinkEstablished.OccurredAt,
            causation_id = sourceLinkEstablished.CausationId,
            source_link_established_outbox_fact_id = sourceLinkEstablished.EventId,
            source_link_established_event_type = sourceLinkEstablished.EventType,
            source_link_established_payload = JsonSerializer.Serialize(sourceLinkEstablished.Payload),
            source_link_established_idempotency_key = $"{idempotencyKey}|SourceLinkEstablished"
        };

    private async Task<Party?> HydratePartyAsync(NpgsqlConnection connection, Guid tenantId, Guid partyId, CancellationToken cancellationToken)
    {
        var partyRow = await connection.QuerySingleOrDefaultAsync<PartyRow>(new CommandDefinition(
            """
            select tenant_id as TenantId, party_id as PartyId, created_at as CreatedAt
            from identity_resolution.parties
            where tenant_id = @tenantId and party_id = @partyId
            """,
            new { tenantId, partyId },
            cancellationToken: cancellationToken));
        if (partyRow is null)
        {
            return null;
        }

        var party = new Party(new PartyId(partyRow.PartyId), partyRow.TenantId, new DateTimeOffset(DateTime.SpecifyKind(partyRow.CreatedAt, DateTimeKind.Utc)));
        var links = await connection.QueryAsync<LinkRow>(new CommandDefinition(
            """
            select
                tenant_id as TenantId,
                party_id as PartyId,
                source_system as SourceSystem,
                source_key as SourceKey,
                effective_from as EffectiveFrom,
                effective_to as EffectiveTo,
                status as Status,
                rule_id as RuleId,
                rule_version_id as RuleVersionId,
                ruleset_version as RulesetVersion,
                source_version as SourceVersion,
                evidence_reference as EvidenceReference,
                created_at as CreatedAt,
                created_by as CreatedBy,
                correlation_id as CorrelationId
            from identity_resolution.party_source_links
            where tenant_id = @tenantId and party_id = @partyId
            order by created_at, source_link_id
            """,
            new { tenantId, partyId },
            cancellationToken: cancellationToken));

        foreach (var link in links)
        {
            party.EstablishSourceLink(
                link.TenantId,
                link.SourceSystem,
                link.SourceKey,
                EffectiveInterval.HalfOpen(new DateTimeOffset(DateTime.SpecifyKind(link.EffectiveFrom, DateTimeKind.Utc)), link.EffectiveTo is null ? null : new DateTimeOffset(DateTime.SpecifyKind(link.EffectiveTo.Value, DateTimeKind.Utc))),
                new LinkProvenance(
                    link.RuleId,
                    link.RuleVersionId,
                    link.RulesetVersion,
                    "deterministic_exact_token",
                    "1.0",
                    link.SourceVersion,
                    link.EvidenceReference,
                    link.CorrelationId,
                    null,
                    link.CreatedBy,
                    new DateTimeOffset(DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc))));
        }

        return party;
    }

    private static string MatchIdentityDigest(string identityToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityToken))).ToLowerInvariant();

    private static Guid DeterministicGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }

    private sealed class PartyRow
    {
        public Guid TenantId { get; set; }

        public Guid PartyId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    private sealed class LinkRow
    {
        public Guid TenantId { get; set; }

        public Guid PartyId { get; set; }

        public string SourceSystem { get; set; } = string.Empty;

        public string SourceKey { get; set; } = string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public string Status { get; set; } = string.Empty;

        public string RuleId { get; set; } = string.Empty;

        public string RuleVersionId { get; set; } = string.Empty;

        public string RulesetVersion { get; set; } = string.Empty;

        public string SourceVersion { get; set; } = string.Empty;

        public string EvidenceReference { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public Guid CorrelationId { get; set; }
    }
}

public static class PostgresIdentityResolutionSql
{
    public const string RollbackStatement = "rollback";

    public static PostgresTransactionScript CreatePartyWithInitialLinkAndOutboxFacts() =>
        new(
            [
                "begin",
                AtomicCreatePartyWithInitialLinkAndOutboxFacts,
                "commit"
            ]);

    public const string AtomicCreatePartyWithInitialLinkAndOutboxFacts = """
        with inserted_party as (
            insert into identity_resolution.parties (
                tenant_id,
                party_id,
                created_at,
                created_by,
                correlation_id,
                version)
            values (
                @tenant_id,
                @party_id,
                @created_at,
                @created_by,
                @correlation_id,
                @version)
            on conflict (tenant_id, correlation_id) do nothing
            returning tenant_id, party_id
        ), inserted_idempotency as (
            insert into identity_resolution.idempotency_records (
                tenant_id,
                idempotency_key,
                party_id,
                created_at)
            select tenant_id, @idempotency_key, party_id, @created_at
            from inserted_party
            on conflict (tenant_id, idempotency_key) do nothing
            returning tenant_id, party_id
        ), inserted_link as (
            insert into identity_resolution.party_source_links (
                tenant_id,
                source_link_id,
                party_id,
                source_system,
                source_key,
                match_identity_digest,
                effective_from,
                effective_to,
                status,
                rule_id,
                rule_version_id,
                ruleset_version,
                source_version,
                evidence_reference,
                created_at,
                created_by,
                correlation_id,
                idempotency_key)
            select
                @tenant_id,
                @source_link_id,
                @party_id,
                @source_system,
                @source_key,
                @match_identity_digest,
                @effective_from,
                @effective_to,
                @status,
                @rule_id,
                @rule_version_id,
                @ruleset_version,
                @source_version,
                @evidence_reference,
                @created_at,
                @created_by,
                @correlation_id,
                @idempotency_key
            from inserted_idempotency
            returning tenant_id, party_id
        ), inserted_party_created_outbox as (
            insert into integration_outbox.outbox_facts (
                tenant_id,
                outbox_fact_id,
                aggregate_type,
                aggregate_id,
                event_type,
                event_version,
                payload,
                occurred_at,
                correlation_id,
                causation_id,
                idempotency_key,
                created_at)
            select
                @tenant_id,
                @party_created_outbox_fact_id,
                @aggregate_type,
                @party_id,
                @party_created_event_type,
                @event_version,
                cast(@party_created_payload as jsonb),
                @occurred_at,
                @correlation_id,
                @causation_id,
                @party_created_idempotency_key,
                @created_at
            from inserted_link
            on conflict (tenant_id, idempotency_key, event_type) do nothing
            returning outbox_fact_id
        ), inserted_source_link_established_outbox as (
            insert into integration_outbox.outbox_facts (
                tenant_id,
                outbox_fact_id,
                aggregate_type,
                aggregate_id,
                event_type,
                event_version,
                payload,
                occurred_at,
                correlation_id,
                causation_id,
                idempotency_key,
                created_at)
            select
                @tenant_id,
                @source_link_established_outbox_fact_id,
                @aggregate_type,
                @party_id,
                @source_link_established_event_type,
                @event_version,
                cast(@source_link_established_payload as jsonb),
                @occurred_at,
                @correlation_id,
                @causation_id,
                @source_link_established_idempotency_key,
                @created_at
            from inserted_link
            where exists (select 1 from inserted_party_created_outbox)
            on conflict (tenant_id, idempotency_key, event_type) do nothing
            returning outbox_fact_id
        )
        select
            (select count(*) from inserted_party_created_outbox)
            + (select count(*) from inserted_source_link_established_outbox)
        """;

    public const string AtomicCreateSourceLinkForExistingPartyWithOutboxFact = """
        with selected_party as (
            select tenant_id, party_id
            from identity_resolution.parties
            where tenant_id = @tenant_id and party_id = @party_id
        ), inserted_idempotency as (
            insert into identity_resolution.idempotency_records (
                tenant_id,
                idempotency_key,
                party_id,
                created_at)
            select tenant_id, @idempotency_key, party_id, @created_at
            from selected_party
            on conflict (tenant_id, idempotency_key) do nothing
            returning tenant_id, party_id
        ), inserted_link as (
            insert into identity_resolution.party_source_links (
                tenant_id,
                source_link_id,
                party_id,
                source_system,
                source_key,
                match_identity_digest,
                effective_from,
                effective_to,
                status,
                rule_id,
                rule_version_id,
                ruleset_version,
                source_version,
                evidence_reference,
                created_at,
                created_by,
                correlation_id,
                idempotency_key)
            select
                @tenant_id,
                @source_link_id,
                @party_id,
                @source_system,
                @source_key,
                @match_identity_digest,
                @effective_from,
                @effective_to,
                @status,
                @rule_id,
                @rule_version_id,
                @ruleset_version,
                @source_version,
                @evidence_reference,
                @created_at,
                @created_by,
                @correlation_id,
                @idempotency_key
            from inserted_idempotency
            returning tenant_id, party_id
        ), inserted_source_link_established_outbox as (
            insert into integration_outbox.outbox_facts (
                tenant_id,
                outbox_fact_id,
                aggregate_type,
                aggregate_id,
                event_type,
                event_version,
                payload,
                occurred_at,
                correlation_id,
                causation_id,
                idempotency_key,
                created_at)
            select
                @tenant_id,
                @source_link_established_outbox_fact_id,
                @aggregate_type,
                @party_id,
                @source_link_established_event_type,
                @event_version,
                cast(@source_link_established_payload as jsonb),
                @occurred_at,
                @correlation_id,
                @causation_id,
                @source_link_established_idempotency_key,
                @created_at
            from inserted_link
            on conflict (tenant_id, idempotency_key, event_type) do nothing
            returning outbox_fact_id
        )
        select count(*) from inserted_source_link_established_outbox
        """;
}

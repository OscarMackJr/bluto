using System.Text.Json;
using Bluto.Domain.Identity;
using Xunit;

namespace Bluto.Identity.Tests;

public sealed class IdentityResolutionSliceTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public async Task Party_id_is_immutable()
    {
        var harness = SliceHarness.Create();

        var result = await harness.Service.ResolveAsync(harness.Command(), CancellationToken.None);

        Assert.Throws<InvalidOperationException>(() => result.Party.ReplaceId(PartyId.New()));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task Party_id_is_never_reused()
    {
        var harness = SliceHarness.Create();

        var first = await harness.Service.ResolveAsync(harness.Command(sourceKey: "SRC-001"), CancellationToken.None);
        var second = await harness.Service.ResolveAsync(harness.Command(sourceKey: "SRC-002", identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", idempotencyKey: "tenant-a|nexus|SRC-002|v1|rule-version-2026-07-30|ResolveSourceCandidate"), CancellationToken.None);

        Assert.NotEqual(first.Party.PartyId, second.Party.PartyId);
    }

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

    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Property")]
    public async Task Effective_interval_uses_approved_half_open_semantics()
    {
        var harness = SliceHarness.Create();
        var effectiveFrom = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

        var result = await harness.Service.ResolveAsync(harness.Command(effectiveFrom: effectiveFrom), CancellationToken.None);
        var link = Assert.Single(result.Party.SourceLinks);

        Assert.True(link.EffectiveInterval.Contains(effectiveFrom));
        Assert.True(link.EffectiveInterval.Contains(effectiveFrom.AddTicks(1)));
        Assert.Null(link.EffectiveInterval.EffectiveTo);
        Assert.Throws<ArgumentException>(() => EffectiveInterval.HalfOpen(effectiveFrom, effectiveFrom));
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Application")]
    public async Task Tenant_scope_is_explicit_and_authorized()
    {
        var harness = SliceHarness.Create();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.ResolveAsync(harness.Command(authorizedTenantIds: new HashSet<Guid>()), CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Application")]
    public async Task Raw_strong_identifiers_fail_closed_without_persistence_logs_or_events()
    {
        var harness = SliceHarness.Create();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.ResolveAsync(harness.Command(rawStrongIdentifier: "123-45-6789"), CancellationToken.None));
        var persisted = JsonSerializer.Serialize(harness.Repository.Snapshot());
        var logs = string.Join(Environment.NewLine, harness.Logs);
        var outbox = JsonSerializer.Serialize(harness.Repository.OutboxFacts());

        Assert.DoesNotContain("123-45-6789", error.Message);
        Assert.DoesNotContain("123-45-6789", persisted);
        Assert.DoesNotContain("123-45-6789", logs);
        Assert.DoesNotContain("123-45-6789", outbox);
        Assert.Empty(harness.Repository.Parties());
        Assert.Empty(harness.Repository.OutboxFacts());
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("123-45-6789")]
    [Trait("Category", "Security")]
    [Trait("Category", "Application")]
    public async Task Raw_looking_identity_tokens_fail_closed_without_persistence_logs_or_events(string rawLookingToken)
    {
        var harness = SliceHarness.Create();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.ResolveAsync(harness.Command(identityToken: rawLookingToken), CancellationToken.None));
        var persisted = JsonSerializer.Serialize(harness.Repository.Snapshot());
        var logs = string.Join(Environment.NewLine, harness.Logs);
        var outbox = JsonSerializer.Serialize(harness.Repository.OutboxFacts());

        Assert.DoesNotContain(rawLookingToken, error.Message);
        Assert.DoesNotContain(rawLookingToken, persisted);
        Assert.DoesNotContain(rawLookingToken, logs);
        Assert.DoesNotContain(rawLookingToken, outbox);
        Assert.Empty(harness.Repository.Parties());
        Assert.Empty(harness.Repository.OutboxFacts());
    }
    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Application")]
    public async Task Provenance_includes_rule_ruleset_version_and_correlation()
    {
        var harness = SliceHarness.Create();
        var correlationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await harness.Service.ResolveAsync(harness.Command(correlationId: correlationId), CancellationToken.None);
        var provenance = Assert.Single(result.Party.SourceLinks).Provenance;

        Assert.Equal("rule-exact-token", provenance.MatchRuleId);
        Assert.Equal("rule-version-2026-07-30", provenance.RuleVersionId);
        Assert.Equal("ruleset-1.0.0", provenance.RulesetVersion);
        Assert.Equal(correlationId, provenance.CorrelationId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Outbox")]
    public async Task State_and_outbox_facts_commit_atomically()
    {
        var harness = SliceHarness.Create();

        await harness.Service.ResolveAsync(harness.Command(), CancellationToken.None);

        Assert.Single(harness.Repository.Parties());
        Assert.Equal(["PartyCreated", "SourceLinkEstablished"], harness.Repository.OutboxFacts().Select(fact => fact.EventType));
    }

    [Fact]
    [Trait("Category", "Application")]
    [Trait("Category", "Property")]
    public async Task Replaying_same_command_is_idempotent()
    {
        var harness = SliceHarness.Create();
        var command = harness.Command(idempotencyKey: "tenant-a|nexus|SRC-001|v1|rule-version-2026-07-30|ResolveSourceCandidate");

        var first = await harness.Service.ResolveAsync(command, CancellationToken.None);
        var second = await harness.Service.ResolveAsync(command, CancellationToken.None);

        Assert.Equal(first.Party.PartyId, second.Party.PartyId);
        Assert.Single(harness.Repository.Parties());
        Assert.Equal(2, harness.Repository.OutboxFacts().Count);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Integration")]
    public async Task Cross_tenant_collision_does_not_create_a_link()
    {
        var harness = SliceHarness.Create();
        await harness.Service.ResolveAsync(harness.Command(tenantId: SliceHarness.TenantA), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.ResolveAsync(
                harness.Command(
                    tenantId: SliceHarness.TenantB,
                    authorizedTenantIds: new HashSet<Guid> { SliceHarness.TenantA },
                    idempotencyKey: "tenant-b|nexus|SRC-001|v1|rule-version-2026-07-30|ResolveSourceCandidate"),
                CancellationToken.None));

        Assert.Empty(harness.Repository.ActiveLinks(SliceHarness.TenantB, "nexus", "SRC-001"));
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Application")]
    public async Task Cross_tenant_collision_returns_deferred_result_without_phantom_party()
    {
        var harness = SliceHarness.Create();
        await harness.Service.ResolveAsync(harness.Command(tenantId: SliceHarness.TenantA), CancellationToken.None);

        var result = await harness.Service.ResolveAsync(
            harness.Command(
                tenantId: SliceHarness.TenantB,
                sourceKey: "SRC-999",
                authorizedTenantIds: new HashSet<Guid> { SliceHarness.TenantB },
                idempotencyKey: "tenant-b|nexus|SRC-999|v1|rule-version-2026-07-30|ResolveSourceCandidate"),
            CancellationToken.None);

        Assert.False(result.Created);
        Assert.Null(result.ResolvedParty);
        Assert.DoesNotContain(harness.Repository.Parties(), party => party.TenantId == SliceHarness.TenantB);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Acceptance")]
    public async Task Failure_leaves_no_partial_authoritative_state()
    {
        var harness = SliceHarness.Create(failBeforeOutboxCommit: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.ResolveAsync(harness.Command(), CancellationToken.None));

        Assert.Empty(harness.Repository.Parties());
        Assert.Empty(harness.Repository.OutboxFacts());
    }
}

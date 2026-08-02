using System.Text.Json;
using Xunit;

namespace Bluto.Identity.Tests;

public sealed class Wp006IdentityResolutionAcceptanceSpecificationTests
{
    [Fact]
    [Trait("Category", "Acceptance")]
    [Trait("Category", "Specification")]
    public async Task Seeded_crm_ledger_and_intrepid_records_resolve_to_one_stable_party()
    {
        var harness = SliceHarness.Create();

        var crm = await harness.Service.ResolveAsync(harness.Command(sourceSystem: "crm", sourceKey: "CRM-001"), CancellationToken.None);
        var ledger = await harness.Service.ResolveAsync(harness.Command(sourceSystem: "ledger", sourceKey: "LEDGER-001", idempotencyKey: "tenant-a|ledger|LEDGER-001|v1|rule-version-2026-07-30|ResolveSourceCandidate"), CancellationToken.None);
        var intrepid = await harness.Service.ResolveAsync(harness.Command(sourceSystem: "intrepid", sourceKey: "INTREPID-001", idempotencyKey: "tenant-a|intrepid|INTREPID-001|v1|rule-version-2026-07-30|ResolveSourceCandidate"), CancellationToken.None);

        Assert.Equal(crm.Party.PartyId, ledger.Party.PartyId);
        Assert.Equal(crm.Party.PartyId, intrepid.Party.PartyId);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    [Trait("Category", "Security")]
    [Trait("Category", "Specification")]
    public async Task Cross_tenant_strong_identifier_collision_is_audited_with_zero_cross_tenant_links()
    {
        var harness = SliceHarness.Create();

        await harness.Service.ResolveAsync(harness.Command(tenantId: SliceHarness.TenantA), CancellationToken.None);
        await harness.Service.ResolveAsync(
            harness.Command(
                tenantId: SliceHarness.TenantB,
                identityToken: "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                idempotencyKey: "tenant-b|nexus|SRC-001|v1|rule-version-2026-07-30|ResolveSourceCandidate"),
            CancellationToken.None);

        Assert.Empty(harness.Repository.ActiveLinks(SliceHarness.TenantB, "nexus", "SRC-001"));
        Assert.Contains(harness.Logs, log => log.Contains("cross_tenant_collision", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    [Trait("Category", "Security")]
    [Trait("Category", "Specification")]
    public async Task No_readable_identifier_audit_covers_state_events_logs_and_errors()
    {
        var harness = SliceHarness.Create();
        const string readableIdentifier = "123-45-6789";

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.ResolveAsync(harness.Command(rawStrongIdentifier: readableIdentifier), CancellationToken.None));

        var combinedSurface = string.Join(
            Environment.NewLine,
            error.Message,
            string.Join(Environment.NewLine, harness.Logs),
            JsonSerializer.Serialize(harness.Repository.Snapshot()),
            JsonSerializer.Serialize(harness.Repository.OutboxFacts()));

        Assert.DoesNotContain(readableIdentifier, combinedSurface, StringComparison.Ordinal);
        Assert.Contains("raw strong identifier rejected", combinedSurface, StringComparison.OrdinalIgnoreCase);
    }
}

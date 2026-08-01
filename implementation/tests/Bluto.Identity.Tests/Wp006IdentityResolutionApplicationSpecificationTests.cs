using System.Text.Json;
using Bluto.Application.Identity;
using Xunit;
using TargetInvocationException = System.Reflection.TargetInvocationException;

namespace Bluto.Identity.Tests;

public sealed class Wp006IdentityResolutionApplicationSpecificationTests
{
    [Fact]
    [Trait("Category", "Application")]
    [Trait("Category", "Specification")]
    public void Resolve_command_requires_explicit_operation_component_in_idempotency_key()
    {
        var harness = SliceHarness.Create();
        var command = harness.Command(idempotencyKey: "tenant-a|nexus|SRC-001|v1|rule-version-2026-07-30");

        var validation = typeof(IdentityResolutionService)
            .GetMethod("ValidateIdempotencyKey", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(validation);
        var error = Assert.ThrowsAny<TargetInvocationException>(() => validation.Invoke(null, [command]));
        Assert.Contains("operation", error.InnerException?.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Application")]
    [Trait("Category", "Security")]
    [Trait("Category", "Specification")]
    public async Task Ambiguous_or_conflicting_candidate_creates_review_case_and_no_active_link()
    {
        var harness = SliceHarness.Create();
        await harness.Service.ResolveAsync(harness.Command(), CancellationToken.None);

        var ambiguous = harness.Command(
            sourceKey: "SRC-AMBIGUOUS",
            identityToken: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            idempotencyKey: "tenant-a|nexus|SRC-AMBIGUOUS|v1|rule-version-2026-07-30|ResolveSourceCandidate");

        var result = await harness.Service.ResolveAsync(ambiguous, CancellationToken.None);

        Assert.False(result.Created);
        Assert.Empty(harness.Repository.ActiveLinks(ambiguous.TenantId, ambiguous.SourceSystem, ambiguous.SourceKey));
        var reviewCasesMethod = harness.Repository.GetType().GetMethod("ReviewCases");
        Assert.NotNull(reviewCasesMethod);
        var reviewCases = reviewCasesMethod.Invoke(harness.Repository, []);
        Assert.NotNull(reviewCases);
    }

    [Fact]
    [Trait("Category", "Application")]
    [Trait("Category", "Outbox")]
    [Trait("Category", "Specification")]
    public async Task Outbox_facts_conform_to_wp005_contract_envelope()
    {
        var harness = SliceHarness.Create();

        await harness.Service.ResolveAsync(harness.Command(), CancellationToken.None);
        var serializedOutbox = JsonSerializer.Serialize(harness.Repository.OutboxFacts());

        Assert.Contains("aggregate_type", serializedOutbox, StringComparison.Ordinal);
        Assert.Contains("producer_version", serializedOutbox, StringComparison.Ordinal);
        Assert.Contains("payload_schema", serializedOutbox, StringComparison.Ordinal);
    }
}


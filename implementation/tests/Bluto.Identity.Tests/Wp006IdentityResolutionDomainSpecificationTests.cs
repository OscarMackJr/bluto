using System.Reflection;
using Bluto.Domain.Identity;
using Xunit;

namespace Bluto.Identity.Tests;

public sealed class Wp006IdentityResolutionDomainSpecificationTests
{
    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Specification")]
    public void Party_aggregate_exposes_governed_merge_retire_and_split_transitions()
    {
        var partyType = typeof(Party);

        Assert.NotNull(FindInstanceMethod(partyType, "MergeInto"));
        Assert.NotNull(FindInstanceMethod(partyType, "Retire"));
        Assert.NotNull(FindInstanceMethod(partyType, "Split"));
    }

    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Specification")]
    public void Party_source_link_exposes_candidate_rejected_and_superseded_lifecycle_states()
    {
        var linkType = typeof(PartySourceLink);

        Assert.NotNull(FindInstanceMethod(linkType, "Reject"));
        Assert.NotNull(FindInstanceMethod(linkType, "Supersede"));
        Assert.Contains("rejected", LifecycleVocabularyFor(linkType), StringComparer.Ordinal);
        Assert.Contains("superseded", LifecycleVocabularyFor(linkType), StringComparer.Ordinal);
    }

    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Property")]
    [Trait("Category", "Specification")]
    public void Effective_interval_exposes_overlap_detection_for_closed_historical_links()
    {
        var intervalType = typeof(EffectiveInterval);

        Assert.NotNull(FindInstanceMethod(intervalType, "Overlaps"));
    }

    [Fact]
    [Trait("Category", "Domain")]
    [Trait("Category", "Specification")]
    public void Party_identifier_registry_prevents_reuse_after_merge_split_or_retirement()
    {
        var domainAssembly = typeof(Party).Assembly;

        Assert.NotNull(domainAssembly.GetType("Bluto.Domain.Identity.PartyIdentifierRegistry"));
    }

    private static MethodInfo? FindInstanceMethod(Type type, string name) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method => method.Name.Equals(name, StringComparison.Ordinal));

    private static IEnumerable<string> LifecycleVocabularyFor(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string?)field.GetValue(null))
            .OfType<string>();
}

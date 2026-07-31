namespace Bluto.Domain.Identity;

public readonly record struct PartyId(Guid Value)
{
    public static PartyId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct SourceLinkId(Guid Value)
{
    public static SourceLinkId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public sealed record EffectiveInterval(DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo)
{
    public static EffectiveInterval HalfOpen(DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo)
    {
        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new ArgumentException("Effective interval must be half-open with effective_to greater than effective_from.");
        }

        return new EffectiveInterval(effectiveFrom, effectiveTo);
    }

    public bool Contains(DateTimeOffset instant) =>
        instant >= EffectiveFrom && (EffectiveTo is null || instant < EffectiveTo);
}

public sealed record LinkProvenance(
    string MatchRuleId,
    string RuleVersionId,
    string RulesetVersion,
    string MatchMethod,
    string Confidence,
    string SourceVersion,
    string EvidenceReference,
    Guid CorrelationId,
    Guid? CausationId,
    string AssertedBy,
    DateTimeOffset AssertedAt);

public sealed record PartySourceLink(
    SourceLinkId LinkId,
    PartyId PartyId,
    Guid TenantId,
    string SourceSystem,
    string SourceKey,
    EffectiveInterval EffectiveInterval,
    string Status,
    LinkProvenance Provenance);

public sealed class Party
{
    private readonly List<PartySourceLink> sourceLinks = [];

    public Party(PartyId partyId, Guid tenantId, DateTimeOffset createdAt)
    {
        PartyId = partyId;
        TenantId = tenantId;
        CreatedAt = createdAt;
        Status = "active";
        Version = 1;
    }

    public PartyId PartyId { get; }

    public Guid TenantId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Status { get; }

    public int Version { get; private set; }

    public IReadOnlyList<PartySourceLink> SourceLinks => sourceLinks;

    public void ReplaceId(PartyId _)
    {
        throw new InvalidOperationException("Party ID is immutable.");
    }

    public PartySourceLink EstablishSourceLink(
        Guid tenantId,
        string sourceSystem,
        string sourceKey,
        EffectiveInterval effectiveInterval,
        LinkProvenance provenance)
    {
        if (tenantId != TenantId)
        {
            throw new UnauthorizedAccessException("Tenant scope is not authorized for this Party.");
        }

        if (sourceLinks.Any(link =>
                link.TenantId == tenantId
                && link.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
                && link.SourceKey.Equals(sourceKey, StringComparison.Ordinal)
                && link.Status.Equals("active", StringComparison.Ordinal)
                && link.EffectiveInterval.EffectiveTo is null))
        {
            throw new InvalidOperationException("An active source link already exists for this canonical source identity scope.");
        }

        var link = new PartySourceLink(
            SourceLinkId.New(),
            PartyId,
            tenantId,
            sourceSystem,
            sourceKey,
            effectiveInterval,
            "active",
            provenance);
        sourceLinks.Add(link);
        Version++;
        return link;
    }
}

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

    public bool Overlaps(EffectiveInterval other)
    {
        var thisTo = EffectiveTo ?? DateTimeOffset.MaxValue;
        var otherTo = other.EffectiveTo ?? DateTimeOffset.MaxValue;
        return EffectiveFrom < otherTo && other.EffectiveFrom < thisTo;
    }
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
    LinkProvenance Provenance)
{
    public const string Active = "active";
    public const string Rejected = "rejected";
    public const string Superseded = "superseded";

    public PartySourceLink Reject() => this with { Status = Rejected };

    public PartySourceLink Supersede() => this with { Status = Superseded };
}

public sealed class PartyIdentifierRegistry
{
    private readonly HashSet<PartyId> issued = [];
    private readonly HashSet<PartyId> retired = [];

    public void RegisterIssued(PartyId partyId)
    {
        if (retired.Contains(partyId) || !issued.Add(partyId))
        {
            throw new InvalidOperationException("Party ID reuse is prohibited.");
        }
    }

    public void Retire(PartyId partyId)
    {
        if (!issued.Contains(partyId))
        {
            throw new InvalidOperationException("Only issued Party IDs can be retired.");
        }

        retired.Add(partyId);
    }
}

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

    public string Status { get; private set; }

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
                && link.Status.Equals(PartySourceLink.Active, StringComparison.Ordinal)
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
            PartySourceLink.Active,
            provenance);
        sourceLinks.Add(link);
        Version++;
        return link;
    }

    public void MergeInto(PartyId survivingPartyId)
    {
        if (survivingPartyId == PartyId)
        {
            throw new InvalidOperationException("A Party cannot be merged into itself.");
        }

        Status = "merged";
        Version++;
    }

    public void Retire()
    {
        Status = "retired";
        Version++;
    }

    public Party Split(PartyId newPartyId, DateTimeOffset createdAt)
    {
        if (newPartyId == PartyId)
        {
            throw new InvalidOperationException("Split Party ID must be newly issued.");
        }

        Version++;
        return new Party(newPartyId, TenantId, createdAt);
    }
}

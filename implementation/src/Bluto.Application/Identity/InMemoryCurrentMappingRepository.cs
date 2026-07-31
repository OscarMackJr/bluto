namespace Bluto.Application.Identity;

public sealed class InMemoryCurrentMappingRepository : ICurrentMappingRepository
{
    private readonly InMemoryPartyRepository repository;

    public InMemoryCurrentMappingRepository(InMemoryPartyRepository repository)
    {
        this.repository = repository;
    }

    public Task<CurrentMapping?> ResolveCurrentAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var party = repository.FindActiveParty(tenantId, sourceSystem, sourceKey);
        var link = party?.SourceLinks.SingleOrDefault(sourceLink =>
            sourceLink.TenantId == tenantId
            && sourceLink.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
            && sourceLink.SourceKey.Equals(sourceKey, StringComparison.Ordinal)
            && sourceLink.Status.Equals("active", StringComparison.Ordinal)
            && sourceLink.EffectiveInterval.EffectiveTo is null);

        return Task.FromResult(link is null || party is null
            ? null
            : new CurrentMapping(
                tenantId,
                party.PartyId.Value,
                link.SourceSystem,
                link.SourceKey,
                link.EffectiveInterval.EffectiveFrom,
                link.EffectiveInterval.EffectiveTo,
                link.Status,
                link.Provenance.RuleVersionId,
                link.Provenance.EvidenceReference));
    }
}

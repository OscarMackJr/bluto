namespace Bluto.Application.Identity;

public sealed record CurrentMapping(
    Guid TenantId,
    Guid PartyId,
    string SourceSystem,
    string SourceKey,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string Status,
    string RuleVersion,
    string ProvenanceReference);

public interface ICurrentMappingRepository
{
    Task<CurrentMapping?> ResolveCurrentAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken);
}

public sealed record ResolveCurrentPartyQuery(
    Guid TenantId,
    IReadOnlySet<Guid> AuthorizedTenantIds,
    string SourceSystem,
    string SourceKey,
    Guid CorrelationId);

public sealed class CurrentMappingQueryService
{
    private readonly ICurrentMappingRepository repository;

    public CurrentMappingQueryService(ICurrentMappingRepository repository)
    {
        this.repository = repository;
    }

    public Task<CurrentMapping?> ResolveAsync(ResolveCurrentPartyQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (query.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant scope is required.");
        }

        if (!query.AuthorizedTenantIds.Contains(query.TenantId))
        {
            throw new UnauthorizedAccessException("Tenant scope is not authorized.");
        }

        if (string.IsNullOrWhiteSpace(query.SourceSystem) || string.IsNullOrWhiteSpace(query.SourceKey))
        {
            throw new ArgumentException("Current mapping query is missing canonical source identity fields.");
        }

        return repository.ResolveCurrentAsync(query.TenantId, query.SourceSystem, query.SourceKey, cancellationToken);
    }
}

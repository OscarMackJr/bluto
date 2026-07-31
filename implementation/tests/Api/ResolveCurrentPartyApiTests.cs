using System.Text.Json;
using Bluto.Api.Mapping;
using Bluto.Application.Identity;
using Xunit;

namespace Bluto.Api.Tests;

public sealed class ResolveCurrentPartyApiTests
{
    private static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid PartyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CorrelationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Contract")]
    public async Task Current_mapping_query_returns_governed_mapping_contract()
    {
        var repository = new RecordingCurrentMappingRepository(CurrentMapping());
        var logs = new List<string>();

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            AuthenticatedContext(TenantA),
            new CurrentMappingQueryService(repository),
            logs.Add,
            CancellationToken.None);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(CorrelationId.ToString("D"), response.Headers["x-correlation-id"]);
        var body = Assert.IsType<MappingApiResponse>(response.Body);
        Assert.Equal(PartyId, body.PartyId);
        Assert.Equal(TenantA, body.TenantId);
        Assert.Equal("nexus", body.SourceSystem);
        Assert.Equal("SRC-001", body.SourceKey);
        Assert.Equal("active", body.Status);
        Assert.Equal("rule-version-2026-07-30", body.RuleVersion);
        Assert.Equal("evidence://synthetic/candidate/SRC-001", body.ProvenanceReference);
        Assert.True(body.EffectiveFrom <= new DateTimeOffset(2026, 7, 30, 12, 30, 0, TimeSpan.Zero));
        Assert.Null(body.EffectiveTo);
        Assert.Single(repository.Calls);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Api")]
    public async Task Tenant_claim_is_required_before_repository_access()
    {
        var repository = new RecordingCurrentMappingRepository(CurrentMapping());

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            new MappingRequestContext(IsAuthenticated: true, TenantId: null, AuthorizedTenantIds: new HashSet<Guid>(), CorrelationId),
            new CurrentMappingQueryService(repository),
            _ => { },
            CancellationToken.None);

        Assert.Equal(403, response.StatusCode);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Api")]
    public async Task Unauthenticated_call_has_no_local_bypass()
    {
        var repository = new RecordingCurrentMappingRepository(CurrentMapping());

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            new MappingRequestContext(IsAuthenticated: false, TenantId: TenantA, AuthorizedTenantIds: new HashSet<Guid> { TenantA }, CorrelationId),
            new CurrentMappingQueryService(repository),
            _ => { },
            CancellationToken.None);

        Assert.Equal(401, response.StatusCode);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Category", "Integration")]
    public async Task Cross_tenant_mapping_is_concealed_and_does_not_query_repository()
    {
        var repository = new RecordingCurrentMappingRepository(CurrentMapping(tenantId: TenantB));

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            new MappingRequestContext(IsAuthenticated: true, TenantId: TenantB, AuthorizedTenantIds: new HashSet<Guid> { TenantA }, CorrelationId),
            new CurrentMappingQueryService(repository),
            _ => { },
            CancellationToken.None);

        Assert.Equal(404, response.StatusCode);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    [Trait("Category", "Api")]
    public async Task Absent_mapping_returns_documented_not_found_outcome()
    {
        var repository = new RecordingCurrentMappingRepository(null);

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-404",
            AuthenticatedContext(TenantA),
            new CurrentMappingQueryService(repository),
            _ => { },
            CancellationToken.None);

        Assert.Equal(404, response.StatusCode);
        var error = Assert.IsType<MappingApiError>(response.Body);
        Assert.Equal("mapping_not_found", error.Code);
        Assert.Equal(CorrelationId, error.CorrelationId);
    }

    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Integration")]
    public async Task Degraded_dependency_returns_structured_503()
    {
        var repository = new RecordingCurrentMappingRepository(null, fail: true);

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            "SRC-001",
            AuthenticatedContext(TenantA),
            new CurrentMappingQueryService(repository),
            _ => { },
            CancellationToken.None);

        Assert.Equal(503, response.StatusCode);
        var error = Assert.IsType<MappingApiError>(response.Body);
        Assert.Equal("mapping_dependency_degraded", error.Code);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Raw_source_identifier_is_absent_from_logs_errors_and_response()
    {
        var repository = new RecordingCurrentMappingRepository(null);
        var logs = new List<string>();
        const string sensitive = "123-45-6789";

        var response = await MappingApi.ResolveCurrentPartyAsync(
            "nexus",
            sensitive,
            AuthenticatedContext(TenantA),
            new CurrentMappingQueryService(repository),
            logs.Add,
            CancellationToken.None);

        var serialized = JsonSerializer.Serialize(response);
        Assert.DoesNotContain(sensitive, serialized);
        Assert.DoesNotContain(sensitive, string.Join(Environment.NewLine, logs));
    }

    private static MappingRequestContext AuthenticatedContext(Guid tenantId) =>
        new(IsAuthenticated: true, TenantId: tenantId, AuthorizedTenantIds: new HashSet<Guid> { tenantId }, CorrelationId);

    private static CurrentMapping CurrentMapping(Guid? tenantId = null) =>
        new(
            tenantId ?? TenantA,
            PartyId,
            "nexus",
            "SRC-001",
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            EffectiveTo: null,
            "active",
            "rule-version-2026-07-30",
            "evidence://synthetic/candidate/SRC-001");

    private sealed class RecordingCurrentMappingRepository : ICurrentMappingRepository
    {
        private readonly CurrentMapping? mapping;
        private readonly bool fail;

        public RecordingCurrentMappingRepository(CurrentMapping? mapping, bool fail = false)
        {
            this.mapping = mapping;
            this.fail = fail;
        }

        public List<(Guid TenantId, string SourceSystem, string SourceKey)> Calls { get; } = [];

        public Task<CurrentMapping?> ResolveCurrentAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)
        {
            if (fail)
            {
                throw new InvalidOperationException("projection unavailable");
            }

            Calls.Add((tenantId, sourceSystem, sourceKey));
            return Task.FromResult(mapping);
        }
    }
}

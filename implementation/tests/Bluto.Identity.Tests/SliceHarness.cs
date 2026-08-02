using Bluto.Application.Identity;

namespace Bluto.Identity.Tests;

internal sealed class SliceHarness
{
    public static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid TenantB = Guid.Parse("20000000-0000-0000-0000-000000000002");

    private SliceHarness(InMemoryPartyRepository repository, IdentityResolutionService service, List<string> logs)
    {
        Repository = repository;
        Service = service;
        Logs = logs;
    }

    public InMemoryPartyRepository Repository { get; }

    public IdentityResolutionService Service { get; }

    public List<string> Logs { get; }

    public static SliceHarness Create(bool failBeforeOutboxCommit = false)
    {
        var logs = new List<string>();
        var repository = new InMemoryPartyRepository(failBeforeOutboxCommit);
        var service = new IdentityResolutionService(repository, message => logs.Add(message));
        return new SliceHarness(repository, service, logs);
    }

    public ResolveSourceCandidateCommand Command(
        Guid? tenantId = null,
        string sourceSystem = "nexus",
        string sourceKey = "SRC-001",
        string identityToken = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
        DateTimeOffset? effectiveFrom = null,
        Guid? correlationId = null,
        string idempotencyKey = "tenant-a|nexus|SRC-001|v1|rule-version-2026-07-30|ResolveSourceCandidate",
        IReadOnlySet<Guid>? authorizedTenantIds = null,
        string? rawStrongIdentifier = null) =>
        new(
            CommandId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            TenantId: tenantId ?? TenantA,
            AuthorizedTenantIds: authorizedTenantIds ?? new HashSet<Guid> { tenantId ?? TenantA },
            SourceSystem: sourceSystem,
            SourceKey: sourceKey,
            IdentityToken: identityToken,
            EffectiveFrom: effectiveFrom ?? new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            RequestedAt: new DateTimeOffset(2026, 7, 30, 12, 1, 0, TimeSpan.Zero),
            IdempotencyKey: idempotencyKey,
            CorrelationId: correlationId ?? Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            CausationId: null,
            ActorId: "worker:identity-resolution",
            MatchRuleId: "rule-exact-token",
            RuleVersionId: "rule-version-2026-07-30",
            RulesetVersion: "ruleset-1.0.0",
            SourceVersion: "source-page-v1",
            EvidenceReference: "evidence://synthetic/candidate/SRC-001",
            RawStrongIdentifier: rawStrongIdentifier);
}

using Bluto.Application.Identity;
using Bluto.Infrastructure.Postgres.Identity;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bluto.Persistence.Tests;

public sealed class PostgresPartyRepositoryLiveTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("bluto")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(MigrationSql(), connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Outbox")]
    public async Task Repository_commits_party_link_idempotency_and_outbox_facts_atomically_in_postgresql()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(dataSource), _ => { });
        var command = ResolveCommand();

        var result = await service.ResolveAsync(command, TestContext.Current.CancellationToken);
        var replay = await service.ResolveAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.Created);
        Assert.False(replay.Created);
        Assert.Equal(result.Party.PartyId, replay.Party.PartyId);
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.parties", TestContext.Current.CancellationToken));
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.party_source_links", TestContext.Current.CancellationToken));
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.idempotency_records", TestContext.Current.CancellationToken));
        Assert.Equal(2L, await ScalarAsync(connection, "select count(*) from integration_outbox.outbox_facts", TestContext.Current.CancellationToken));
    }


    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Outbox")]
    public async Task Repository_links_same_tenant_cross_source_candidate_to_existing_party()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(dataSource), _ => { });

        var first = await service.ResolveAsync(ResolveCommand(), TestContext.Current.CancellationToken);
        var second = await service.ResolveAsync(
            ResolveCommand(
                sourceSystem: "ledger",
                sourceKey: "LEDGER-001",
                idempotencyKey: "10000000-0000-0000-0000-000000000001|ledger|LEDGER-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link"),
            TestContext.Current.CancellationToken);

        Assert.False(second.Created);
        Assert.Equal(first.Party.PartyId, second.Party.PartyId);
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.parties", TestContext.Current.CancellationToken));
        Assert.Equal(2L, await ScalarAsync(connection, "select count(*) from identity_resolution.party_source_links", TestContext.Current.CancellationToken));
        Assert.Equal(3L, await ScalarAsync(connection, "select count(*) from integration_outbox.outbox_facts", TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public async Task Same_idempotency_key_in_two_tenants_replays_only_within_requested_tenant()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(dataSource), _ => { });
        const string sharedIdempotencyKey = "shared|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link";

        var tenantA = await service.ResolveAsync(
            ResolveCommand(idempotencyKey: sharedIdempotencyKey),
            TestContext.Current.CancellationToken);
        var tenantB = await service.ResolveAsync(
            ResolveCommand(
                tenantId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                sourceKey: "SRC-002",
                idempotencyKey: sharedIdempotencyKey,
                identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                correlationId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            TestContext.Current.CancellationToken);

        Assert.True(tenantA.Created);
        Assert.True(tenantB.Created);
        Assert.NotEqual(tenantA.Party?.PartyId, tenantB.Party?.PartyId);
        Assert.Equal(Guid.Parse("20000000-0000-0000-0000-000000000002"), tenantB.Party?.TenantId);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public async Task Row_level_security_blocks_cross_tenant_reads_when_application_predicate_is_omitted()
    {
        await using var adminDataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(adminDataSource), _ => { });

        var tenantA = await service.ResolveAsync(ResolveCommand(), TestContext.Current.CancellationToken);
        var tenantB = await service.ResolveAsync(
            ResolveCommand(
                tenantId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                sourceKey: "SRC-002",
                idempotencyKey: "20000000-0000-0000-0000-000000000002|nexus|SRC-002|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link",
                identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                correlationId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            TestContext.Current.CancellationToken);

        await using (var adminConnection = await adminDataSource.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
            await ExecuteAsync(adminConnection, "create role bluto_rls_app login password 'postgres' nosuperuser nocreatedb nocreaterole noinherit", TestContext.Current.CancellationToken);
            await ExecuteAsync(adminConnection, "grant usage on schema identity_resolution to bluto_rls_app", TestContext.Current.CancellationToken);
            await ExecuteAsync(adminConnection, "grant select on all tables in schema identity_resolution to bluto_rls_app", TestContext.Current.CancellationToken);
        }

        var builder = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Username = "bluto_rls_app",
            Password = "postgres"
        };
        await using var appConnection = new NpgsqlConnection(builder.ConnectionString);
        await appConnection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0L, await ScalarAsync(appConnection, "select count(*) from identity_resolution.parties", TestContext.Current.CancellationToken));
        await ExecuteAsync(appConnection, "select set_config('bluto.tenant_id', '10000000-0000-0000-0000-000000000001', false)", TestContext.Current.CancellationToken);

        Assert.Equal(1L, await ScalarAsync(appConnection, "select count(*) from identity_resolution.parties", TestContext.Current.CancellationToken));
        Assert.Equal(tenantA.Party?.PartyId.Value, await GuidScalarAsync(appConnection, "select party_id from identity_resolution.parties", TestContext.Current.CancellationToken));
        Assert.NotEqual(tenantB.Party?.PartyId.Value, await GuidScalarAsync(appConnection, "select party_id from identity_resolution.parties", TestContext.Current.CancellationToken));
    }
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Domain")]
    public async Task Point_in_time_resolution_preserves_pre_merge_party_after_current_link_moves_to_survivor()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var repository = new PostgresPartyRepository(dataSource);
        var service = new IdentityResolutionService(repository, _ => { });
        var tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var traceTimestamp = new DateTimeOffset(2026, 3, 14, 12, 30, 0, TimeSpan.Zero);
        var mergeInstant = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);

        var partyA = await service.ResolveAsync(
            ResolveCommand(
                sourceKey: "SRC-A",
                idempotencyKey: "10000000-0000-0000-0000-000000000001|nexus|SRC-A|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link",
                effectiveFrom: new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero),
                requestedAt: new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero)),
            TestContext.Current.CancellationToken);
        var partyB = await service.ResolveAsync(
            ResolveCommand(
                sourceSystem: "ledger",
                sourceKey: "LEDGER-B",
                idempotencyKey: "10000000-0000-0000-0000-000000000001|ledger|LEDGER-B|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link",
                identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                correlationId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            TestContext.Current.CancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        // TODO(BLUTO_IDENTITY_SPINE_v0.1 section 7): replace this SQL fixture with the chartered service-level merge path when that workflow is implemented.
        await ExecuteAsync(
            connection,
            """
            update identity_resolution.parties
            set status = 'merged', merged_into_party_id = @survivorPartyId, version = version + 1
            where tenant_id = @tenantId and party_id = @absorbedPartyId;

            update identity_resolution.party_source_links
            set status = 'superseded', effective_to = @mergeInstant
            where tenant_id = @tenantId and party_id = @absorbedPartyId and status = 'active' and effective_to is null;

            insert into identity_resolution.party_source_links (
                tenant_id, source_link_id, party_id, source_system, source_key, match_identity_digest,
                effective_from, effective_to, status, rule_id, rule_version_id, ruleset_version, source_version,
                evidence_reference, created_at, created_by, correlation_id, idempotency_key)
            values (
                @tenantId, '99999999-9999-4999-8999-999999999999', @survivorPartyId, 'nexus', 'SRC-A', 'v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA',
                @mergeInstant, null, 'active', 'rule-exact-token', 'rule-version-2026-07-30', 'ruleset-1.0.0', 'source-page-v1',
                'evidence://synthetic/merge/SRC-A', @mergeInstant, 'worker:identity-resolution', 'dddddddd-dddd-dddd-dddd-dddddddddddd',
                '10000000-0000-0000-0000-000000000001|nexus|SRC-A|merge|rule-version-2026-07-30|merge_party_source_link');
            """,
            TestContext.Current.CancellationToken,
            new NpgsqlParameter("tenantId", tenantId),
            new NpgsqlParameter("absorbedPartyId", partyA.Party.PartyId.Value),
            new NpgsqlParameter("survivorPartyId", partyB.Party.PartyId.Value),
            new NpgsqlParameter("mergeInstant", mergeInstant));

        var historicalPartyId = await GuidScalarAsync(
            connection,
            """
            select party_id
            from identity_resolution.party_source_links
            where tenant_id = @tenantId
              and source_system = 'nexus'
              and source_key = 'SRC-A'
              and effective_from <= @traceTimestamp
              and (effective_to is null or effective_to > @traceTimestamp)
            """,
            TestContext.Current.CancellationToken,
            new NpgsqlParameter("tenantId", tenantId),
            new NpgsqlParameter("traceTimestamp", traceTimestamp));
        var current = await repository.FindActivePartyAsync(tenantId, "nexus", "SRC-A", TestContext.Current.CancellationToken);
        var absorbedStatus = await StringScalarAsync(
            connection,
            "select status from identity_resolution.parties where tenant_id = @tenantId and party_id = @absorbedPartyId",
            TestContext.Current.CancellationToken,
            new NpgsqlParameter("tenantId", tenantId),
            new NpgsqlParameter("absorbedPartyId", partyA.Party.PartyId.Value));
        var mergedIntoPartyId = await GuidScalarAsync(
            connection,
            "select merged_into_party_id from identity_resolution.parties where tenant_id = @tenantId and party_id = @absorbedPartyId",
            TestContext.Current.CancellationToken,
            new NpgsqlParameter("tenantId", tenantId),
            new NpgsqlParameter("absorbedPartyId", partyA.Party.PartyId.Value));

        Assert.Equal(partyA.Party.PartyId.Value, historicalPartyId);
        Assert.Equal(partyB.Party.PartyId, current?.PartyId);
        Assert.Equal("merged", absorbedStatus);
        Assert.Equal(partyB.Party.PartyId.Value, mergedIntoPartyId);
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.parties where tenant_id = '10000000-0000-0000-0000-000000000001' and party_id = '" + partyA.Party.PartyId.Value.ToString("D") + "'", TestContext.Current.CancellationToken));
    }
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public async Task Repository_records_same_source_conflict_review_case_without_link()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(dataSource), _ => { });

        await service.ResolveAsync(ResolveCommand(), TestContext.Current.CancellationToken);
        var result = await service.ResolveAsync(
            ResolveCommand(
                sourceKey: "SRC-AMBIGUOUS",
                idempotencyKey: "10000000-0000-0000-0000-000000000001|nexus|SRC-AMBIGUOUS|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link"),
            TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.party_source_links", TestContext.Current.CancellationToken));
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.review_cases where reason = 'same_source_identity_conflict'", TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public async Task Repository_detects_cross_tenant_collision_without_creating_link()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.GetConnectionString());
        var service = new IdentityResolutionService(new PostgresPartyRepository(dataSource), _ => { });

        await service.ResolveAsync(ResolveCommand(), TestContext.Current.CancellationToken);
        var result = await service.ResolveAsync(
            ResolveCommand(
                tenantId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                sourceKey: "SRC-002",
                idempotencyKey: "20000000-0000-0000-0000-000000000002|nexus|SRC-002|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link"),
            TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0L, await ScalarAsync(connection, "select count(*) from identity_resolution.party_source_links where tenant_id = '20000000-0000-0000-0000-000000000002'", TestContext.Current.CancellationToken));
        Assert.Equal(1L, await ScalarAsync(connection, "select count(*) from identity_resolution.review_cases where tenant_id = '20000000-0000-0000-0000-000000000002' and reason = 'cross_tenant_collision'", TestContext.Current.CancellationToken));
    }
    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No scalar result."));
    }
    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> GuidScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No scalar result."));
    }
    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> GuidScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No scalar result."));
    }

    private static async Task<string> StringScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (string)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No scalar result."));
    }

    private static ResolveSourceCandidateCommand ResolveCommand(Guid? tenantId = null, string sourceSystem = "nexus", string sourceKey = "SRC-001", string idempotencyKey = "10000000-0000-0000-0000-000000000001|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link", string identityToken = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", Guid? correlationId = null, DateTimeOffset? effectiveFrom = null, DateTimeOffset? requestedAt = null) =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            tenantId ?? Guid.Parse("10000000-0000-0000-0000-000000000001"),
            new HashSet<Guid> { tenantId ?? Guid.Parse("10000000-0000-0000-0000-000000000001") },
            sourceSystem,
            sourceKey,
            identityToken,
            effectiveFrom ?? new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            requestedAt ?? new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            idempotencyKey,
            correlationId ?? Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            null,
            "worker:identity-resolution",
            "rule-exact-token",
            "rule-version-2026-07-30",
            "ruleset-1.0.0",
            "source-page-v1",
            "evidence://synthetic/batch-2026-07-30-001/SRC-001",
            RawStrongIdentifier: null);

    private static string MigrationSql() => string.Join(Environment.NewLine, Directory.GetFiles(Path.Combine(RepositoryRoot(), "implementation", "db", "migrations"), "V*.sql").Order(StringComparer.Ordinal).Select(File.ReadAllText));

    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return directory ?? throw new InvalidOperationException("Repository root not found.");
    }
}

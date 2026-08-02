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
                identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"),
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
                identityToken: "v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"),
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

    private static ResolveSourceCandidateCommand ResolveCommand(Guid? tenantId = null, string sourceSystem = "nexus", string sourceKey = "SRC-001", string idempotencyKey = "10000000-0000-0000-0000-000000000001|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link", string identityToken = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA") =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            tenantId ?? Guid.Parse("10000000-0000-0000-0000-000000000001"),
            new HashSet<Guid> { tenantId ?? Guid.Parse("10000000-0000-0000-0000-000000000001") },
            sourceSystem,
            sourceKey,
            identityToken,
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            idempotencyKey,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
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

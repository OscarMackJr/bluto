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

    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No scalar result."));
    }

    private static ResolveSourceCandidateCommand ResolveCommand() =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            new HashSet<Guid> { Guid.Parse("10000000-0000-0000-0000-000000000001") },
            "nexus",
            "SRC-001",
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            "10000000-0000-0000-0000-000000000001|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            null,
            "worker:identity-resolution",
            "rule-exact-token",
            "rule-version-2026-07-30",
            "ruleset-1.0.0",
            "source-page-v1",
            "evidence://synthetic/batch-2026-07-30-001/SRC-001",
            RawStrongIdentifier: null);

    private static string MigrationSql() => File.ReadAllText(Path.Combine(RepositoryRoot(), "implementation", "db", "migrations", "V001__identity_resolution_minimal_slice.sql"));

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




using System.Text.RegularExpressions;
using Bluto.Application.Identity;
using Bluto.Infrastructure.Postgres.Identity;
using Xunit;

namespace Bluto.Persistence.Tests;

public sealed class PostgresRepositoryContractTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void Postgres_repository_implements_party_repository_contract()
    {
        Assert.True(typeof(IPartyRepository).IsAssignableFrom(typeof(PostgresPartyRepository)));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Postgres_project_uses_approved_npgsql_dapper_dependencies()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot(), "implementation", "src", "Bluto.Infrastructure.Postgres", "Bluto.Infrastructure.Postgres.csproj"));

        Assert.Contains("Npgsql", project);
        Assert.Contains("Dapper", project);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Insert_script_commits_party_link_and_outbox_facts_in_one_transaction()
    {
        var script = PostgresIdentityResolutionSql.CreatePartyWithInitialLinkAndOutboxFacts();

        Assert.Equal("begin", script.Statements.First());
        Assert.Equal("commit", script.Statements.Last());
        Assert.Contains(script.Statements, statement => statement.Contains("insert into identity_resolution.parties", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(script.Statements, statement => statement.Contains("insert into identity_resolution.party_source_links", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, Regex.Matches(string.Join(Environment.NewLine, script.Statements), "insert into integration_outbox.outbox_facts", RegexOptions.IgnoreCase).Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public void Insert_script_is_tenant_scoped_and_parameterized()
    {
        var combined = string.Join(Environment.NewLine, PostgresIdentityResolutionSql.CreatePartyWithInitialLinkAndOutboxFacts().Statements);

        Assert.Contains("@tenant_id", combined);
        Assert.Contains("@party_id", combined);
        Assert.Contains("@source_system", combined);
        Assert.Contains("@source_key", combined);
        Assert.Contains("@idempotency_key", combined);
        Assert.DoesNotContain("$\"", combined);
        Assert.DoesNotContain("string.format", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Security")]
    public void Insert_script_never_references_raw_strong_identifiers()
    {
        var combined = string.Join(Environment.NewLine, PostgresIdentityResolutionSql.CreatePartyWithInitialLinkAndOutboxFacts().Statements);

        Assert.DoesNotContain("ssn", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_id", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac_token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identity_token", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Outbox")]
    public void Outbox_facts_are_conditioned_on_successful_authoritative_state_insert()
    {
        var combined = string.Join(Environment.NewLine, PostgresIdentityResolutionSql.CreatePartyWithInitialLinkAndOutboxFacts().Statements);

        Assert.Contains("inserted_link", combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from inserted_link", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"party_source_links[\\s\\S]*on conflict \\(tenant_id, idempotency_key\\) do nothing", combined);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Outbox")]
    public void Rollback_statement_is_available_for_failed_transaction_boundary()
    {
        Assert.Equal("rollback", PostgresIdentityResolutionSql.RollbackStatement);
    }

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



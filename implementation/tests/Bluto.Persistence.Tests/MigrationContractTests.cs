using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Bluto.Persistence.Tests;

public sealed class MigrationContractTests
{
    [Fact]
    [Trait("Category", "Migration")]
    public void Minimal_slice_migration_creates_owned_identity_resolution_and_outbox_tables()
    {
        var sql = MigrationSql();

        Assert.Contains("create schema if not exists identity_resolution", sql);
        Assert.Contains("create schema if not exists integration_outbox", sql);
        Assert.Contains("create table if not exists identity_resolution.parties", sql);
        Assert.Contains("create table if not exists identity_resolution.party_source_links", sql);
        Assert.Contains("create table if not exists integration_outbox.outbox_facts", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Migration_persists_only_non_raw_match_identity_digest()
    {
        var sql = MigrationSql();

        Assert.Contains("match_identity_digest text not null", sql);
        Assert.DoesNotContain("identity_token", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac_token", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Migration_creates_tenant_scoped_review_cases_for_conflicts()
    {
        var sql = MigrationSql();

        Assert.Contains("create table if not exists identity_resolution.review_cases", sql);
        Assert.Contains("reason text not null", sql);
        Assert.Contains("correlation_id uuid not null", sql);
        Assert.Matches(@"primary\s+key\s*\(\s*tenant_id\s*,\s*review_case_id\s*\)", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Migration_never_persists_raw_strong_identifier_columns()
    {
        var sql = MigrationSql();

        Assert.DoesNotContain("ssn", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("social_security", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_identifier", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac_token", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identity_token", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Tenant_scope_is_part_of_authoritative_keys_and_outbox_facts()
    {
        var sql = MigrationSql();

        Assert.Matches(@"primary\s+key\s*\(\s*tenant_id\s*,\s*party_id\s*\)", sql);
        Assert.Matches(@"foreign\s+key\s*\(\s*tenant_id\s*,\s*party_id\s*\)", sql);
        Assert.Matches(@"unique\s*\(\s*tenant_id\s*,\s*idempotency_key\s*\)", sql);
        Assert.Contains("tenant_id uuid not null", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Integration")]
    public void Source_link_constraints_enforce_one_active_half_open_interval_per_canonical_scope()
    {
        var sql = MigrationSql();

        Assert.Contains("effective_from timestamptz not null", sql);
        Assert.Contains("effective_to timestamptz null", sql);
        Assert.Matches(@"check\s*\(\s*effective_to\s+is\s+null\s+or\s+effective_to\s*>\s*effective_from\s*\)", sql);
        Assert.Matches(@"create\s+unique\s+index\s+if\s+not\s+exists\s+ux_party_source_links_active_scope", sql);
        Assert.Matches(@"on\s+identity_resolution\.party_source_links\s*\(\s*tenant_id\s*,\s*source_system\s*,\s*source_key\s*\)", sql);
        Assert.Matches(@"where\s+status\s*=\s*'active'\s+and\s+effective_to\s+is\s+null", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Tenant_scoped_tables_enable_and_force_row_level_security()
    {
        var sql = MigrationSql();

        foreach (var table in new[] { "parties", "party_source_links", "review_cases", "idempotency_records" })
        {
            Assert.Contains($"alter table identity_resolution.{table} enable row level security", sql);
            Assert.Contains($"alter table identity_resolution.{table} force row level security", sql);
            Assert.Contains("create policy", sql);
            Assert.Contains($"on identity_resolution.{table}", sql);
        }

        Assert.Contains("current_setting('bluto.tenant_id'", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Security")]
    public void Cross_tenant_identity_probe_is_boolean_security_definer_exception()
    {
        var sql = MigrationSql();

        Assert.Contains("create or replace function identity_resolution.has_cross_tenant_identity_digest", sql);
        Assert.Contains("returns boolean", sql);
        Assert.Contains("security definer", sql);
        Assert.DoesNotContain("returns table", sql);
    }

    [Fact]
    [Trait("Category", "Migration")]
    [Trait("Category", "Domain")]
    public void Party_lifecycle_state_is_persisted_with_contract_fields()
    {
        var sql = MigrationSql();

        Assert.Contains("status text not null default 'active'", sql);
        Assert.Contains("party_type text not null default 'person'", sql);
        Assert.Contains("merged_into_party_id uuid null", sql);
        Assert.Contains("check (status in ('active', 'merged', 'retired'))", sql);
        Assert.Contains("foreign key (tenant_id, merged_into_party_id)", sql);
    }
    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Outbox")]
    public void Outbox_fact_schema_is_executable_and_closed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(SchemaPath("outbox-fact.v1.schema.json")));
        var root = document.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal("OutboxFact.v1", root.GetProperty("title").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());

        var required = root.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToHashSet();
        Assert.Contains("tenant_id", required);
        Assert.Contains("aggregate_id", required);
        Assert.Contains("event_type", required);
        Assert.Contains("event_version", required);
        Assert.Contains("payload", required);
        Assert.Contains("correlation_id", required);
    }

    private static string MigrationSql() => Regex.Replace(
        string.Join(Environment.NewLine, Directory.GetFiles(Path.Combine(RepositoryRoot(), "implementation", "db", "migrations"), "V*.sql").Order(StringComparer.Ordinal).Select(File.ReadAllText)).ToLowerInvariant(),
        @"\s+",
        " ");

    private static string SchemaPath(string fileName) =>
        Path.Combine(RepositoryRoot(), "implementation", "contracts", "schemas", fileName);

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

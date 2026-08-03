using System.Text.Json;
using Bluto.Application.Identity;
using Xunit;

namespace Bluto.Identity.Tests;

public sealed class RulesetVersionHandshakeTests
{
    [Fact]
    [Trait("Category", "Contract")]
    public async Task Resolution_outbox_ruleset_version_matches_fixture_and_party_created_schema_constraint()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(RepoPath("implementation", "contracts", "fixtures", "ruleset-version.fixture.json")));
        var canonicalValue = fixture.RootElement.GetProperty("canonical_value").GetString() ?? throw new InvalidOperationException("Fixture canonical_value is missing.");
        var formatRegex = fixture.RootElement.GetProperty("format_regex").GetString() ?? throw new InvalidOperationException("Fixture format_regex is missing.");

        using var schema = JsonDocument.Parse(File.ReadAllText(RepoPath("implementation", "contracts", "schemas", "party-created.v1.schema.json")));
        var rulesetSchema = schema.RootElement
            .GetProperty("properties")
            .GetProperty("payload")
            .GetProperty("properties")
            .GetProperty("provenance")
            .GetProperty("properties")
            .GetProperty("ruleset_version");

        Assert.Equal("string", rulesetSchema.GetProperty("type").GetString());
        Assert.Equal(1, rulesetSchema.GetProperty("minLength").GetInt32());
        Assert.Matches(formatRegex, canonicalValue);

        var repository = new InMemoryPartyRepository();
        var service = new IdentityResolutionService(repository, _ => { });
        await service.ResolveAsync(Command(canonicalValue), CancellationToken.None);

        var partyCreated = Assert.Single(repository.OutboxFacts(), fact => fact.EventType == "PartyCreated");
        var payload = Assert.IsType<PartyCreatedPayload>(partyCreated.Payload);
        Assert.Equal(canonicalValue, payload.Provenance.RulesetVersion);
        Assert.Matches(formatRegex, payload.Provenance.RulesetVersion);
    }

    private static ResolveSourceCandidateCommand Command(string rulesetVersion) =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            new HashSet<Guid> { Guid.Parse("10000000-0000-0000-0000-000000000001") },
            "nexus",
            "SRC-001",
            "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            "10000000-0000-0000-0000-000000000001|nexus|SRC-001|source-page-v1|rule-version-2026-07-30|create_party_with_initial_link",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            null,
            "worker:identity-resolution",
            "rule-exact-token",
            "rule-version-2026-07-30",
            rulesetVersion,
            "source-page-v1",
            "evidence://synthetic/batch-2026-07-30-001/SRC-001",
            RawStrongIdentifier: null);

    private static string RepoPath(params string[] parts)
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return Path.Combine(new[] { directory ?? throw new InvalidOperationException("Repository root not found.") }.Concat(parts).ToArray());
    }
}



using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace Bluto.Contracts.Tests;

public sealed class MappingQueryContractCompletionTests
{
    private static readonly string[] ExpectedExampleFiles =
    [
        "resolve-current-party.success.v1.example.json",
        "resolve-current-party.absent.v1.example.json",
        "resolve-current-party.forbidden-concealed.v1.example.json",
        "resolve-current-party.stale.v1.example.json",
        "resolve-current-party.merged.v1.example.json",
        "resolve-current-party.retired.v1.example.json"
    ];

    [Fact]
    [Trait("Category", "Contract")]
    public void Resolve_current_party_openapi_declares_completed_query_outcomes()
    {
        var openApi = File.ReadAllText(OpenApiPath());

        Assert.Contains("ResolveCurrentPartySuccess", openApi, StringComparison.Ordinal);
        Assert.Contains("ResolveCurrentPartyError", openApi, StringComparison.Ordinal);
        Assert.Contains("MappingStatus", openApi, StringComparison.Ordinal);
        Assert.Contains("No active mapping", openApi, StringComparison.Ordinal);
        Assert.Contains("Stale mapping projection", openApi, StringComparison.Ordinal);
        Assert.Contains("Merged identity", openApi, StringComparison.Ordinal);
        Assert.Contains("Retired identity", openApi, StringComparison.Ordinal);
        Assert.Contains("x-request-id", openApi, StringComparison.Ordinal);
        Assert.Contains("x-tenant-scope", openApi, StringComparison.Ordinal);
        Assert.Contains("x-bluto-data-classification", openApi, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Security")]
    public void Resolve_current_party_examples_are_tenant_scoped_and_do_not_expose_prohibited_fields()
    {
        foreach (var exampleFile in ExpectedExampleFiles)
        {
            var text = File.ReadAllText(Path.Combine(ExamplesPath(), exampleFile));
            Assert.Contains("tenant_id", text, StringComparison.Ordinal);
            Assert.Contains("correlation_id", text, StringComparison.Ordinal);
            Assert.Contains("request_id", text, StringComparison.Ordinal);
            Assert.DoesNotContain("raw_identifier", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hmac_token", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("identity_token", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("first_name", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("last_name", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("address", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("balance", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("resolve-current-party.success.v1.example.json", "ResolveCurrentPartySuccess")]
    [InlineData("resolve-current-party.absent.v1.example.json", "ResolveCurrentPartyError")]
    [InlineData("resolve-current-party.forbidden-concealed.v1.example.json", "ResolveCurrentPartyError")]
    [InlineData("resolve-current-party.stale.v1.example.json", "ResolveCurrentPartyError")]
    [InlineData("resolve-current-party.merged.v1.example.json", "ResolveCurrentPartySuccess")]
    [InlineData("resolve-current-party.retired.v1.example.json", "ResolveCurrentPartySuccess")]
    [Trait("Category", "Contract")]
    public void Resolve_current_party_examples_validate_against_projected_schemas(string exampleFile, string schemaName)
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "04-contracts", "schemas", $"{schemaName}.schema.json")));
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(ExamplesPath(), exampleFile)))
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        var result = schema.Evaluate(example, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(detail => detail.EvaluationPath.ToString())));
    }

    private static string OpenApiPath() =>
        Path.Combine(RepositoryRoot(), "docs", "04-contracts", "schemas", "bluto-v1.openapi.yaml");

    private static string ExamplesPath() =>
        Path.Combine(RepositoryRoot(), "implementation", "contracts", "examples");

    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "docs")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return directory ?? throw new InvalidOperationException("Repository root not found.");
    }
}

using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace Bluto.Contracts.Tests;

public sealed class OpenApiContractTests
{
    [Fact]
    [Trait("Category", "Contract")]
    public void Resolve_current_party_openapi_contract_is_present()
    {
        var openApi = File.ReadAllText(OpenApiPath());

        Assert.Contains("/v1/mappings/{source_system}/{source_key}", openApi);
        Assert.Contains("operationId: resolveCurrentParty", openApi);
        Assert.Contains("MappingResponse", openApi);
        foreach (var status in new[] { "'200':", "'400':", "'401':", "'403':", "'404':", "'409':", "'422':", "'429':", "'503':" })
        {
            Assert.Contains(status, openApi);
        }

        Assert.Contains("x-correlation-id", openApi);
        Assert.Contains("security:", openApi);
        Assert.Contains("MappingError", openApi);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Security")]
    public void Resolve_current_party_contract_does_not_expose_raw_identifier_fields()
    {
        var openApi = File.ReadAllText(OpenApiPath());

        Assert.DoesNotContain("ssn", openApi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_id", openApi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_identifier", openApi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac_token", openApi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identity_token", openApi, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("party-created.v1.schema.json", "party-created.v1.example.json")]
    [InlineData("source-link-established.v1.schema.json", "source-link-established.v1.example.json")]
    [Trait("Category", "Contract")]
    public void Event_contract_examples_validate_against_json_schemas(string schemaFile, string exampleFile)
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(RepositoryRoot(), "implementation", "contracts", "schemas", schemaFile)));
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "implementation", "contracts", "examples", exampleFile)))
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        var result = schema.Evaluate(example, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(detail => detail.EvaluationPath.ToString())));
    }

    private static string OpenApiPath() =>
        Path.Combine(RepositoryRoot(), "docs", "04-contracts", "schemas", "bluto-v1.openapi.yaml");

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

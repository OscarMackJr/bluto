using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace Bluto.Contracts.Tests;

public sealed class ContractToolingHarnessTests
{
    [Fact]
    [Trait("Category", "Contract")]
    public void Wp003_contract_tooling_files_are_present_and_governed()
    {
        var root = RepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "implementation", "tools", "contracts", "package.json")));
        Assert.True(File.Exists(Path.Combine(root, "implementation", "tools", "contracts", "package-lock.json")));
        Assert.True(File.Exists(Path.Combine(root, "implementation", "tools", "contracts", ".spectral.yaml")));
        Assert.True(File.Exists(Path.Combine(root, "implementation", "contracts", "README.md")));
        Assert.True(File.Exists(Path.Combine(root, "implementation", "contracts", "baselines", "bluto-v1.openapi.yaml")));

        var readme = File.ReadAllText(Path.Combine(root, "implementation", "contracts", "README.md"));
        foreach (var documentId in new[]
        {
            "BLUTO-ADR-0006",
            "BLUTO-ADR-0007",
            "BLUTO-CONTRACT-ARCH-001",
            "BLUTO-CONTRACT-API-001",
            "BLUTO-CONTRACT-SCHEMA-001",
            "BLUTO-CONTRACT-COMPAT-001",
            "BLUTO-TEST-CONTRACT-001"
        })
        {
            Assert.Contains(documentId, readme, StringComparison.Ordinal);
        }

        Assert.Contains("compatibility baseline", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm exec --prefix implementation/tools/contracts -- spectral", readme, StringComparison.Ordinal);
        Assert.Contains("npm exec --prefix implementation/tools/contracts -- ajv", readme, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Security")]
    public void Wp003_spectral_rules_fail_closed_for_sensitive_and_tenant_contract_metadata()
    {
        var root = RepositoryRoot();
        var spectral = File.ReadAllText(Path.Combine(root, "implementation", "tools", "contracts", ".spectral.yaml"));

        Assert.Contains("bluto-no-raw-strong-identifiers", spectral, StringComparison.Ordinal);
        Assert.Contains("raw_identifier", spectral, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hmac_token", spectral, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bluto-require-correlation-parameter", spectral, StringComparison.Ordinal);
        Assert.Contains("bluto-require-operation-security", spectral, StringComparison.Ordinal);
        Assert.Contains("bluto-require-tenant-scope", spectral, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void Source_link_schema_rejects_malformed_examples_and_prohibited_fields()
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "implementation",
            "contracts",
            "schemas",
            "source-link-established.v1.schema.json")));
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "implementation",
            "contracts",
            "examples",
            "source-link-established.v1.example.json")))
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        AssertInvalid(schema, RemoveRequiredField(example, "tenant_id"));
        AssertInvalid(schema, RemoveRequiredField(example, "correlation_id"));
        AssertInvalid(schema, AddPayloadField(example, "raw_identifier", "123-45-6789"));
        AssertInvalid(schema, AddPayloadField(example, "hmac_token", "token"));
    }

    private static void AssertInvalid(JsonSchema schema, JsonNode candidate)
    {
        var result = schema.Evaluate(candidate, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.False(result.IsValid);
    }

    private static JsonNode RemoveRequiredField(JsonNode source, string fieldName)
    {
        var clone = JsonNode.Parse(source.ToJsonString()) ?? throw new InvalidOperationException("Clone failed.");
        clone.AsObject().Remove(fieldName);
        return clone;
    }

    private static JsonNode AddPayloadField(JsonNode source, string fieldName, string value)
    {
        var clone = JsonNode.Parse(source.ToJsonString()) ?? throw new InvalidOperationException("Clone failed.");
        clone["payload"]![fieldName] = value;
        return clone;
    }

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

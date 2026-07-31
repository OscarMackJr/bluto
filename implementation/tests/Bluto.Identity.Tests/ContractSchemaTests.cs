using System.Text.Json;
using Xunit;

namespace Bluto.Identity.Tests;

public sealed class ContractSchemaTests
{
    [Theory]
    [InlineData("party-created.v1.schema.json", "PartyCreated.v1")]
    [InlineData("source-link-established.v1.schema.json", "SourceLinkEstablished.v1")]
    [Trait("Category", "Contract")]
    public void Event_contract_schema_is_closed_and_contains_required_envelope(string fileName, string title)
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath(fileName)));
        var root = schema.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());

        var required = root.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToHashSet();
        Assert.Contains("event_id", required);
        Assert.Contains("tenant_id", required);
        Assert.Contains("correlation_id", required);
        Assert.Contains("payload", required);
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void Event_contract_schemas_do_not_expose_raw_identifiers_or_hmac_tokens()
    {
        var combined = string.Join(
            Environment.NewLine,
            Directory.GetFiles(SchemaDirectory(), "*.schema.json").Select(File.ReadAllText));

        Assert.DoesNotContain("ssn", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_id", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac_token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identity_token", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string SchemaPath(string fileName) => Path.Combine(SchemaDirectory(), fileName);

    private static string SchemaDirectory()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return Path.Combine(directory ?? throw new InvalidOperationException("Repository root not found."), "implementation", "contracts", "schemas");
    }
}

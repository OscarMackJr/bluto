using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace Bluto.Contracts.Tests;

public sealed class EventOutboxContractCompletionTests
{
    private static readonly (string Schema, string Example)[] EventExamples =
    [
        ("party-created.v1.schema.json", "party-created.v1.example.json"),
        ("source-link-established.v1.schema.json", "source-link-established.v1.example.json")
    ];

    private static readonly string[] OutboxExamples =
    [
        "outbox-party-created.v1.example.json",
        "outbox-source-link-established.v1.example.json"
    ];

    private static readonly string[] ProhibitedTerms =
    [
        "raw_identifier",
        "rawStrongIdentifier",
        "hmac_token",
        "hmacToken",
        "identity_token",
        "identityToken",
        "ssn",
        "tax_id",
        "taxId",
        "first_name",
        "last_name",
        "address",
        "balance",
        "source_payload"
    ];

    [Theory]
    [MemberData(nameof(EventExampleData))]
    [Trait("Category", "Contract")]
    public void Integration_event_examples_validate_against_governed_schemas(string schemaFile, string exampleFile)
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(SchemasPath(), schemaFile)));
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(EventExamplesPath(), exampleFile)))
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        var result = schema.Evaluate(example, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(detail => detail.EvaluationPath.ToString())));
    }

    [Theory]
    [MemberData(nameof(OutboxExampleData))]
    [Trait("Category", "Contract")]
    [Trait("Category", "Outbox")]
    public void Outbox_fact_examples_validate_against_governed_schema(string exampleFile)
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(SchemasPath(), "outbox-fact.v1.schema.json")));
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(EventExamplesPath(), exampleFile)))
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        var result = schema.Evaluate(example, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(detail => detail.EvaluationPath.ToString())));
    }

    [Theory]
    [MemberData(nameof(AllExampleData))]
    [Trait("Category", "Contract")]
    [Trait("Category", "Security")]
    public void Event_and_outbox_examples_exclude_raw_identifiers_hmac_tokens_and_source_owned_payloads(string exampleFile)
    {
        var text = File.ReadAllText(Path.Combine(EventExamplesPath(), exampleFile));

        foreach (var prohibitedTerm in ProhibitedTerms)
        {
            Assert.DoesNotContain(prohibitedTerm, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(EventExampleData))]
    [Trait("Category", "Contract")]
    [Trait("Category", "Outbox")]
    public void Integration_events_have_complete_broker_neutral_envelope(string schemaFile, string exampleFile)
    {
        _ = schemaFile;
        var example = JsonNode.Parse(File.ReadAllText(Path.Combine(EventExamplesPath(), exampleFile)))?.AsObject()
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");

        foreach (var requiredField in new[]
                 {
                     "event_id",
                     "event_type",
                     "event_version",
                     "occurred_at",
                     "published_at",
                     "aggregate_type",
                     "aggregate_id",
                     "aggregate_version",
                     "correlation_id",
                     "causation_id",
                     "tenant_id",
                     "producer",
                     "producer_version",
                     "payload_schema",
                     "data_classification",
                     "payload"
                 })
        {
            Assert.True(example.ContainsKey(requiredField), $"Missing event envelope field {requiredField}.");
        }
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Category", "Outbox")]
    public void Event_ids_are_unique_across_wp005_examples_for_consumer_deduplication()
    {
        var eventIds = EventExamples
            .Select(tuple => JsonNode.Parse(File.ReadAllText(Path.Combine(EventExamplesPath(), tuple.Example)))?["event_id"]?.GetValue<string>())
            .ToArray();

        Assert.Equal(eventIds.Length, eventIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(OutboxExampleData))]
    [Trait("Category", "Contract")]
    [Trait("Category", "Outbox")]
    public void Outbox_facts_embed_schema_valid_integration_events_without_requiring_a_broker(string exampleFile)
    {
        var outbox = JsonNode.Parse(File.ReadAllText(Path.Combine(EventExamplesPath(), exampleFile)))?.AsObject()
            ?? throw new InvalidOperationException("Example JSON could not be parsed.");
        var payload = outbox["payload"]?.AsObject() ?? throw new InvalidOperationException("Outbox payload is missing.");

        Assert.Equal("pending", outbox["publish_state"]?.GetValue<string>());
        Assert.Equal("none", outbox["broker_transport"]?.GetValue<string>());
        Assert.Equal(outbox["tenant_id"]?.GetValue<string>(), payload["tenant_id"]?.GetValue<string>());
        Assert.Equal(outbox["event_id"]?.GetValue<string>(), payload["event_id"]?.GetValue<string>());
    }

    public static TheoryData<string, string> EventExampleData()
    {
        var data = new TheoryData<string, string>();
        foreach (var (schema, example) in EventExamples)
        {
            data.Add(schema, example);
        }

        return data;
    }

    public static TheoryData<string> OutboxExampleData()
    {
        var data = new TheoryData<string>();
        foreach (var example in OutboxExamples)
        {
            data.Add(example);
        }

        return data;
    }

    public static TheoryData<string> AllExampleData()
    {
        var data = new TheoryData<string>();
        foreach (var (_, example) in EventExamples)
        {
            data.Add(example);
        }

        foreach (var example in OutboxExamples)
        {
            data.Add(example);
        }

        return data;
    }

    private static string SchemasPath() =>
        Path.Combine(RepositoryRoot(), "docs", "04-contracts", "schemas");

    private static string EventExamplesPath() =>
        Path.Combine(RepositoryRoot(), "implementation", "contracts", "examples", "events");

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

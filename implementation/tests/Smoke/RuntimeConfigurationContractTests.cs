using System.Text.Json;
using Json.Schema;
using Xunit;

namespace Bluto.Smoke.Tests;

public sealed class RuntimeConfigurationContractTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    [Trait("Category", "Contract")]
    public void Nonproduction_runtime_configuration_matches_schema_and_contains_no_secrets()
    {
        var schema = JsonSchema.FromText(File.ReadAllText(RepoPath("implementation", "deploy", "configuration", "bluto-runtime-config.schema.json")));
        using var sample = JsonDocument.Parse(File.ReadAllText(RepoPath("implementation", "deploy", "configuration", "nonproduction.sample.json")));

        var result = schema.Evaluate(sample.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Details.Select(detail => $"{detail.InstanceLocation}: {detail.Errors}")));
        AssertNoProhibitedTerms(sample.RootElement.ToString());
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Api_and_worker_projects_are_configured_for_sdk_container_publish()
    {
        var api = File.ReadAllText(RepoPath("implementation", "src", "Bluto.Api", "Bluto.Api.csproj"));
        var worker = File.ReadAllText(RepoPath("implementation", "src", "Bluto.Worker", "Bluto.Worker.csproj"));

        Assert.Contains("<EnableSdkContainerSupport>true</EnableSdkContainerSupport>", api, StringComparison.Ordinal);
        Assert.Contains("<ContainerRepository>bluto-api</ContainerRepository>", api, StringComparison.Ordinal);
        Assert.Contains("<ContainerUser>64198</ContainerUser>", api, StringComparison.Ordinal);
        Assert.Contains("<EnableSdkContainerSupport>true</EnableSdkContainerSupport>", worker, StringComparison.Ordinal);
        Assert.Contains("<ContainerRepository>bluto-worker</ContainerRepository>", worker, StringComparison.Ordinal);
        Assert.Contains("<ContainerUser>64198</ContainerUser>", worker, StringComparison.Ordinal);
    }

    private static void AssertNoProhibitedTerms(string value)
    {
        var prohibited = new[] { "password", "secret", "client_secret", "raw_identifier", "hmac", "ssn", "tax_id" };
        foreach (var term in prohibited)
        {
            Assert.DoesNotContain(term, value, StringComparison.OrdinalIgnoreCase);
        }
    }

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

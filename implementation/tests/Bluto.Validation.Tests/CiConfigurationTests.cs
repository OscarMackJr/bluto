using Xunit;

namespace Bluto.Validation.Tests;

public sealed class CiConfigurationTests
{
    [Fact]
    public void Wp002_workflow_enforces_supply_chain_and_evidence_checks()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "wp002-ci.yml"));
        var gitAttributes = File.ReadAllText(Path.Combine(root, ".gitattributes"));

        Assert.Contains("permissions:", workflow, StringComparison.Ordinal);
        Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
        Assert.Contains("actions: read", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("id-token: write", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("security-events: write", workflow, StringComparison.Ordinal);
        Assert.Contains("*.cs text eol=crlf", gitAttributes, StringComparison.Ordinal);

        Assert.Contains("dotnet build implementation/Bluto.Validation.sln --configuration Release --no-restore", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test implementation/Bluto.Validation.sln --configuration Release --no-build", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet format implementation/Bluto.Validation.sln --verify-no-changes", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet list implementation/Bluto.Validation.sln package --vulnerable --include-transitive", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project implementation/src/Bluto.Validation.Cli --configuration Release -- --root .", workflow, StringComparison.Ordinal);
        Assert.Contains("python -m json.tool", workflow, StringComparison.Ordinal);

        Assert.Contains("github/codeql-action/init@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("github/codeql-action/analyze@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("config-file: ./.github/codeql/codeql-config.yml", workflow, StringComparison.Ordinal);
        Assert.Contains("upload: false", workflow, StringComparison.Ordinal);
        Assert.Contains("implementation/validation/codeql-results", workflow, StringComparison.Ordinal);

        Assert.Contains("aquasecurity/trivy-action@v0.36.0", workflow, StringComparison.Ordinal);
        Assert.Contains("format: 'sarif'", workflow, StringComparison.Ordinal);
        Assert.Contains("format: 'cyclonedx'", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/upload-artifact@v6", workflow, StringComparison.Ordinal);

        Assert.Contains("GITHUB_SHA", workflow, StringComparison.Ordinal);
        Assert.Contains("GITHUB_RUN_ID", workflow, StringComparison.Ordinal);
        Assert.Contains("implementation/validation/ci-evidence-manifest.json", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Wp002_dependabot_and_codeql_configuration_are_present()
    {
        var root = FindRepositoryRoot();
        var dependabot = File.ReadAllText(Path.Combine(root, ".github", "dependabot.yml"));
        var codeql = File.ReadAllText(Path.Combine(root, ".github", "codeql", "codeql-config.yml"));

        Assert.Contains("package-ecosystem: \"nuget\"", dependabot, StringComparison.Ordinal);
        Assert.Contains("package-ecosystem: \"github-actions\"", dependabot, StringComparison.Ordinal);
        Assert.Contains("groups:", dependabot, StringComparison.Ordinal);

        Assert.Contains("security-and-quality", codeql, StringComparison.Ordinal);
        Assert.Contains("implementation/src", codeql, StringComparison.Ordinal);
        Assert.Contains("implementation/tests", codeql, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "implementation", "Bluto.Validation.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}

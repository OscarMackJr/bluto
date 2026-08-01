using System.Text.Json;
using Xunit;

namespace Bluto.Smoke.Tests;

public sealed class OperationalSmokeContractTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    [Trait("Category", "Contract")]
    public void Operational_health_contract_declares_required_safe_checks()
    {
        using var contract = JsonDocument.Parse(File.ReadAllText(RepoPath("implementation", "deploy", "contracts", "operational-health.contract.json")));
        var root = contract.RootElement;

        Assert.Equal("BLUTO-WP-012-OPERATIONAL-HEALTH-CONTRACT", root.GetProperty("document_id").GetString());
        Assert.Contains("BLUTO-OPS-OBS-001", root.GetProperty("governing_document_ids").EnumerateArray().Select(id => id.GetString()));

        var checkNames = root.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("api_health", checkNames);
        Assert.Contains("mapping_read", checkNames);
        Assert.Contains("worker_dry_run", checkNames);
        Assert.Contains("telemetry_export", checkNames);
        Assert.Contains("key_vault_reference_failure", checkNames);
        Assert.Contains("cross_tenant_negative", checkNames);

        var serialized = root.ToString();
        Assert.DoesNotContain("raw_identifier", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmac", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Terraform_contract_enforces_nonproduction_and_managed_identity_controls()
    {
        var terraform = string.Join(Environment.NewLine,
            File.ReadAllText(RepoPath("implementation", "infra", "terraform", "variables.tf")),
            File.ReadAllText(RepoPath("implementation", "infra", "terraform", "main.tf")),
            File.ReadAllText(RepoPath("implementation", "infra", "terraform", "outputs.tf")));

        Assert.Contains("allowed_environments", terraform, StringComparison.Ordinal);
        Assert.Contains("nonproduction", terraform, StringComparison.Ordinal);
        Assert.Contains("api_workload_identity_name", terraform, StringComparison.Ordinal);
        Assert.Contains("worker_workload_identity_name", terraform, StringComparison.Ordinal);
        Assert.Contains("public_network_access_enabled", terraform, StringComparison.Ordinal);
        Assert.Contains("key_vault_reference", terraform, StringComparison.Ordinal);
        Assert.DoesNotContain("default     = \"prod", terraform, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Smoke_plan_uses_two_tenants_and_cross_tenant_negative_check()
    {
        using var plan = JsonDocument.Parse(File.ReadAllText(RepoPath("implementation", "deploy", "smoke", "nonproduction-smoke-plan.json")));
        var root = plan.RootElement;

        Assert.Equal("BLUTO-WP-012-NONPROD-SMOKE-PLAN", root.GetProperty("document_id").GetString());
        Assert.Equal(2, root.GetProperty("synthetic_tenants").GetArrayLength());
        Assert.Contains("cross_tenant_negative", root.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString()));
        Assert.Contains("rollback_previous_revision", root.GetProperty("rollback_exercise").GetProperty("required_steps").EnumerateArray().Select(step => step.GetString()));
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

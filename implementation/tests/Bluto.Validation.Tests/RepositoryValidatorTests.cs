using System.Text.Json;
using Xunit;

namespace Bluto.Validation.Tests;

public sealed class RepositoryValidatorTests
{
    [Fact]
    public void Valid_repository_fixture_passes_all_applicable_wp001_gates_and_emits_rvec()
    {
        using var fixture = RepositoryFixture.CreateValid();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(0, result.ExitCode);
        Assert.All(result.Gates, gate => Assert.Equal(GateResult.Pass, gate.Result));
        Assert.Equal(6, Directory.GetFiles(fixture.EvidenceDirectory, "implementation-wp-001-gate-*.json").Length);

        using var evidence = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture.EvidenceDirectory, "implementation-wp-001-gate-01.json")));
        Assert.Equal("1.0", evidence.RootElement.GetProperty("rvec_version").GetString());
        Assert.Equal("BLUTO-BASE-VAL-001", evidence.RootElement.GetProperty("validation_standard_id").GetString());
        Assert.Equal("GATE-01", evidence.RootElement.GetProperty("quality_gate").GetString());
        Assert.Equal("PASS", evidence.RootElement.GetProperty("result").GetString());
        Assert.True(evidence.RootElement.TryGetProperty("evidence_digest", out _));
    }

    [Fact]
    public void Adr_ids_generated_registry_external_dependencies_and_release_composition_are_valid()
    {
        using var fixture = RepositoryFixture.CreateWithAdrAndGeneratedProjectionShapes();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Mandatory_findings_return_non_zero_and_are_sorted_deterministically()
    {
        using var fixture = RepositoryFixture.CreateWithMissingDependency();

        var first = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));
        var second = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(1, first.ExitCode);
        Assert.Equal(1, second.ExitCode);
        Assert.Equal(first.Findings.Select(f => f.Code), second.Findings.Select(f => f.Code));
        Assert.Contains(first.Findings, f => f.Gate == "GATE-04" && f.Code == "DEPENDENCY_MISSING");
    }

    [Fact]
    public void Duplicate_document_and_artifact_ids_are_identity_failures()
    {
        using var fixture = RepositoryFixture.CreateWithDuplicateIdentity();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Findings, f => f.Gate == "GATE-02" && f.Code == "DOCUMENT_ID_DUPLICATE");
        Assert.Contains(result.Findings, f => f.Gate == "GATE-02" && f.Code == "ARTIFACT_ID_DUPLICATE");
    }

    [Fact]
    public void Markdown_cross_references_must_resolve_to_known_document_ids()
    {
        using var fixture = RepositoryFixture.CreateWithUnresolvedReference();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Findings, f => f.Gate == "GATE-03" && f.Code == "CROSS_REFERENCE_UNRESOLVED");
    }

    [Fact]
    public void Authority_dependencies_cannot_point_to_lower_authority_documents()
    {
        using var fixture = RepositoryFixture.CreateWithAuthorityInversion();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Findings, f => f.Gate == "GATE-05" && f.Code == "AUTHORITY_INVERSION");
    }

    [Fact]
    public void Rgtm_gate_mappings_must_match_repository_validation_mappings()
    {
        using var fixture = RepositoryFixture.CreateWithTraceabilityMismatch();

        var result = RepositoryValidator.Validate(new ValidationOptions(
            fixture.Root,
            fixture.EvidenceDirectory,
            fixture.ReportPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Findings, f => f.Gate == "GATE-06" && f.Code == "RGTM_VALIDATION_MAPPING_MISMATCH");
    }
}

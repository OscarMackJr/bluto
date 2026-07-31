namespace Bluto.Validation.Tests;

internal sealed class RepositoryFixture : IDisposable
{
    private RepositoryFixture(string root)
    {
        Root = root;
        EvidenceDirectory = Path.Combine(root, "repository", "evidence", "rvec");
        ReportPath = Path.Combine(root, "implementation", "validation", "report.json");
        Directory.CreateDirectory(EvidenceDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
    }

    public string Root { get; }

    public string EvidenceDirectory { get; }

    public string ReportPath { get; }

    public static RepositoryFixture CreateValid()
    {
        var fixture = CreateBase();
        return fixture;
    }

    public static RepositoryFixture CreateWithMissingDependency()
    {
        var fixture = CreateBase(extraDependency: "BLUTO-MISSING-001");
        return fixture;
    }

    public static RepositoryFixture CreateWithDuplicateIdentity()
    {
        var fixture = CreateBase();
        WriteControlledDocument(
            fixture.Root,
            "docs/duplicate.md",
            "BLUTO-BASE-GOV-002",
            "ART-BLUTO-BASE-GOV-002-v1.0.0",
            "1.0.0",
            "Baselined",
            "RAH-1",
            []);
        WriteRegistries(fixture.Root);
        return fixture;
    }

    public static RepositoryFixture CreateWithUnresolvedReference()
    {
        var fixture = CreateBase(body: "This references `BLUTO-UNKNOWN-001`.");
        return fixture;
    }

    public static RepositoryFixture CreateWithAuthorityInversion()
    {
        var fixture = CreateBase(authorityDependency: true);
        return fixture;
    }

    public static RepositoryFixture CreateWithTraceabilityMismatch()
    {
        var fixture = CreateBase(traceabilityMismatch: true);
        return fixture;
    }

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
    }

    private static RepositoryFixture CreateBase(
        string? extraDependency = null,
        string body = "",
        bool authorityDependency = false,
        bool traceabilityMismatch = false)
    {
        var fixture = new RepositoryFixture(Path.Combine(Path.GetTempPath(), "bluto-validator-tests", Guid.NewGuid().ToString("N")));

        WriteControlledDocument(
            fixture.Root,
            "00-baseline/README.md",
            "BLUTO-BASE-GOV-001",
            "ART-BLUTO-BASE-GOV-001-v1.0.0",
            "1.0.0",
            "Baselined",
            "RAH-1",
            [],
            body);
        WriteControlledDocument(
            fixture.Root,
            "docs/foundation.md",
            "BLUTO-BASE-GOV-002",
            "ART-BLUTO-BASE-GOV-002-v1.0.0",
            "1.0.0",
            "Baselined",
            "RAH-2",
            authorityDependency ? ["BLUTO-IMPL-DOC-001"] : ["BLUTO-BASE-GOV-001"]);
        WriteControlledDocument(
            fixture.Root,
            "implementation/doc.md",
            "BLUTO-IMPL-DOC-001",
            "ART-BLUTO-IMPL-DOC-001-v1.0.0",
            "1.0.0",
            "Approved",
            "RAH-12",
            extraDependency is null ? ["BLUTO-BASE-GOV-002"] : ["BLUTO-BASE-GOV-002", extraDependency]);

        WriteRegistries(fixture.Root, traceabilityMismatch);
        return fixture;
    }

    private static void WriteControlledDocument(
        string root,
        string relativePath,
        string documentId,
        string artifactId,
        string version,
        string status,
        string authorityLevel,
        IReadOnlyList<string> dependencies,
        string body = "")
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var dependsOn = dependencies.Count == 0 ? "None" : string.Join(", ", dependencies);
        File.WriteAllText(path, $$"""
            # {{documentId}}

            | Metadata | Value |
            |---|---|
            | Document ID | {{documentId}} |
            | Artifact ID | {{artifactId}} |
            | Version | {{version}} |
            | Status | {{status}} |
            | Owner | Test Owner |
            | Baseline | Test Baseline |
            | Stream | Test |
            | Authority Level | {{authorityLevel}} |
            | Depends On | {{dependsOn}} |
            | Supersedes | None |
            | Approval Date | 2026-07-30 |
            | Effective Date | 2026-07-30 |
            | Review Date | 2027-07-30 |
            | Classification | Internal |

            {{body}}
            """);
    }

    private static void WriteRegistries(string root, bool traceabilityMismatch = false)
    {
        Directory.CreateDirectory(Path.Combine(root, "repository", "lir"));
        Directory.CreateDirectory(Path.Combine(root, "repository", "rgtm"));
        Directory.CreateDirectory(Path.Combine(root, "repository", "validation"));
        Directory.CreateDirectory(Path.Combine(root, "repository", "schemas"));

        File.WriteAllText(Path.Combine(root, "repository", "validation", "quality-gates.yaml"), """
            quality_gates:
            - id: GATE-01
              name: Metadata Validation
            - id: GATE-02
              name: Identity Validation
            - id: GATE-03
              name: Cross-Reference Validation
            - id: GATE-04
              name: Dependency Validation
            - id: GATE-05
              name: Authority Validation
            - id: GATE-06
              name: Traceability Validation
            """);
        File.WriteAllText(Path.Combine(root, "repository", "validation", "validation-standards.yaml"), """
            mappings:
              GATE-01: BLUTO-BASE-VAL-001
              GATE-02: BLUTO-BASE-VAL-002
              GATE-03: BLUTO-BASE-VAL-003
              GATE-04: BLUTO-BASE-VAL-004
              GATE-05: BLUTO-BASE-VAL-005
              GATE-06: BLUTO-BASE-VAL-006
            """);
        File.WriteAllText(Path.Combine(root, "repository", "rgtm", "repository-governance-traceability.yaml"), $$"""
            schema_version: 1.0.0
            principles:
            - CP-01
            control_objectives:
            - RCO-01
            quality_gates:
            - id: GATE-01
              name: Metadata Validation
            - id: GATE-02
              name: Identity Validation
            - id: GATE-03
              name: Cross-Reference Validation
            - id: GATE-04
              name: Dependency Validation
            - id: GATE-05
              name: Authority Validation
            - id: GATE-06
              name: Traceability Validation
            validation_mappings:
              GATE-01: BLUTO-BASE-VAL-001
              GATE-02: BLUTO-BASE-VAL-002
              GATE-03: BLUTO-BASE-VAL-003
              GATE-04: BLUTO-BASE-VAL-004
              GATE-05: {{(traceabilityMismatch ? "BLUTO-WRONG-001" : "BLUTO-BASE-VAL-005")}}
              GATE-06: BLUTO-BASE-VAL-006
            documents:
            - document_id: BLUTO-BASE-GOV-001
              dependencies: []
            - document_id: BLUTO-BASE-GOV-002
              dependencies:
              - BLUTO-BASE-GOV-001
            - document_id: BLUTO-IMPL-DOC-001
              dependencies:
              - BLUTO-BASE-GOV-002
            """);
        File.WriteAllText(Path.Combine(root, "repository", "schemas", "rvec.schema.json"), """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "required": [
                "rvec_version",
                "validation_standard_id",
                "quality_gate",
                "repository_version",
                "artifact_ids",
                "tool",
                "tool_version",
                "execution_mode",
                "start_timestamp",
                "end_timestamp",
                "result",
                "evidence_digest"
              ]
            }
            """);
    }
}

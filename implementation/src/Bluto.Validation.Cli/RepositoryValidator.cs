using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bluto.Validation;

public sealed record ValidationOptions(string Root, string EvidenceDirectory, string ReportPath);

public sealed record ValidationResult(int ExitCode, IReadOnlyList<QualityGateResult> Gates, IReadOnlyList<ValidationFinding> Findings);

public sealed record QualityGateResult(string Gate, string Name, GateResult Result, int FindingCount, string EvidencePath);

public sealed record ValidationFinding(string Gate, string Code, string Severity, string Artifact, string Message);

[JsonConverter(typeof(JsonStringEnumConverter<GateResult>))]
public enum GateResult
{
    Pass,
    Fail
}

internal sealed record ControlledDocument(
    string Path,
    string Content,
    IReadOnlyDictionary<string, string> Metadata)
{
    public string DocumentId => Get("Document ID");

    public string ArtifactId => Get("Artifact ID");

    public int AuthorityRank => TryAuthorityRank(Get("Authority Level"), out var rank) ? rank : int.MaxValue;

    public IReadOnlyList<string> Dependencies => SplitIds(Get("Depends On"));

    public string Get(string key) => Metadata.TryGetValue(key, out var value) ? value : string.Empty;

    private static IReadOnlyList<string> SplitIds(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.Equals("None", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryAuthorityRank(string value, out int rank)
    {
        rank = 0;
        return value.StartsWith("RAH-", StringComparison.Ordinal)
            && int.TryParse(value[4..], NumberStyles.None, CultureInfo.InvariantCulture, out rank);
    }
}

public static class RepositoryValidator
{
    private const string ToolName = "Bluto.Validation.Cli";
    private const string ToolVersion = "0.1.0";
    private const string RvecTimestamp = "1970-01-01T00:00:00Z";

    private static readonly Regex DocumentIdPattern = new("^BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}$", RegexOptions.Compiled);
    private static readonly Regex ArtifactIdPattern = new("^ART-BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}-v[0-9]+\\.[0-9]+\\.[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex VersionPattern = new("^[0-9]+\\.[0-9]+\\.[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex ShaPattern = new("^[a-f0-9]{64}$", RegexOptions.Compiled);
    private static readonly Regex ReferencePattern = new("\\b(?:BLUTO|ART-BLUTO)(?:-[A-Z0-9]+)+-[0-9]{3,4}(?:-v[0-9]+\\.[0-9]+\\.[0-9]+)?\\b", RegexOptions.Compiled);

    private static readonly string[] MetadataFields =
    [
        "Document ID",
        "Artifact ID",
        "Version",
        "Status",
        "Owner",
        "Baseline",
        "Stream",
        "Authority Level",
        "Depends On",
        "Supersedes",
        "Approval Date",
        "Effective Date",
        "Review Date",
        "Classification"
    ];

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "Draft",
        "Under Review",
        "Approved",
        "Baselined",
        "Superseded",
        "Retired",
        "Proposed"
    };

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter<GateResult>() }
    };

    public static ValidationResult Validate(ValidationOptions options)
    {
        var root = Path.GetFullPath(options.Root);
        var documents = LoadDocuments(root);
        var gates = LoadGates(root);
        var mappings = LoadValidationMappings(root);
        var rgtmMappings = LoadRgtmValidationMappings(root);
        var registry = LoadLogicalIdentityRegistry(root);
        var dependencyGraph = LoadDependencyGraph(root);

        var findings = new List<ValidationFinding>();
        findings.AddRange(ValidateMetadata(documents, registry));
        findings.AddRange(ValidateIdentity(documents, registry));
        findings.AddRange(ValidateCrossReferences(documents));
        findings.AddRange(ValidateDependencies(documents, dependencyGraph));
        findings.AddRange(ValidateAuthority(documents));
        findings.AddRange(ValidateTraceability(documents, mappings, rgtmMappings));

        var sortedFindings = findings
            .OrderBy(f => f.Gate, StringComparer.Ordinal)
            .ThenBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Artifact, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();

        Directory.CreateDirectory(options.EvidenceDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(options.ReportPath)!);

        var gateResults = gates
            .Where(gate => string.CompareOrdinal(gate.Id, "GATE-01") >= 0 && string.CompareOrdinal(gate.Id, "GATE-06") <= 0)
            .OrderBy(gate => gate.Id, StringComparer.Ordinal)
            .Select(gate => WriteEvidence(root, options.EvidenceDirectory, gate, mappings, documents, sortedFindings))
            .ToArray();

        var result = new ValidationResult(sortedFindings.Length == 0 ? 0 : 1, gateResults, sortedFindings);
        File.WriteAllText(options.ReportPath, JsonSerializer.Serialize(result, JsonOptions) + Environment.NewLine);
        return result;
    }

    private static IReadOnlyList<(string Id, string Name)> LoadGates(string root)
    {
        var path = Path.Combine(root, "repository", "validation", "quality-gates.yaml");
        if (!File.Exists(path))
        {
            return DefaultGates();
        }

        var gates = new List<(string Id, string Name)>();
        string? currentId = null;
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- id:", StringComparison.Ordinal))
            {
                currentId = trimmed[5..].Trim();
            }
            else if (currentId is not null && trimmed.StartsWith("name:", StringComparison.Ordinal))
            {
                gates.Add((currentId, trimmed[5..].Trim()));
                currentId = null;
            }
        }

        return gates.Count == 0 ? DefaultGates() : gates.OrderBy(g => g.Id, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<(string Id, string Name)> DefaultGates() =>
    [
        ("GATE-01", "Metadata Validation"),
        ("GATE-02", "Identity Validation"),
        ("GATE-03", "Cross-Reference Validation"),
        ("GATE-04", "Dependency Validation"),
        ("GATE-05", "Authority Validation"),
        ("GATE-06", "Traceability Validation")
    ];

    private static IReadOnlyDictionary<string, string> LoadValidationMappings(string root) =>
        LoadSimpleYamlMap(Path.Combine(root, "repository", "validation", "validation-standards.yaml"), "mappings:");

    private static IReadOnlyDictionary<string, string> LoadRgtmValidationMappings(string root) =>
        LoadSimpleYamlMap(Path.Combine(root, "repository", "rgtm", "repository-governance-traceability.yaml"), "validation_mappings:");

    private static IReadOnlyDictionary<string, string> LoadSimpleYamlMap(string path, string section)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return result;
        }

        var inSection = false;
        foreach (var line in File.ReadLines(path))
        {
            if (line.Trim().Equals(section, StringComparison.Ordinal))
            {
                inSection = true;
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            if (!line.StartsWith("  ", StringComparison.Ordinal) || !line.Contains(':', StringComparison.Ordinal))
            {
                break;
            }

            var parts = line.Trim().Split(':', 2);
            result[parts[0].Trim()] = parts[1].Trim();
        }

        return result;
    }

    private static IReadOnlyList<ControlledDocument> LoadDocuments(string root)
    {
        return Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrBuildPath(root, path))
            .OrderBy(path => RelativePath(root, path), StringComparer.Ordinal)
            .Select(path => new ControlledDocument(RelativePath(root, path), File.ReadAllText(path), ParseMetadata(File.ReadLines(path))))
            .Where(document => document.Metadata.Count > 0)
            .ToArray();
    }

    private static bool IsGeneratedOrBuildPath(string root, string path)
    {
        var relative = RelativePath(root, path);
        return relative.StartsWith(".git/", StringComparison.Ordinal)
            || relative.Contains("/bin/", StringComparison.Ordinal)
            || relative.Contains("/obj/", StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ParseMetadata(IEnumerable<string> lines)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        var inTable = false;
        foreach (var line in lines)
        {
            if (line.Trim().Equals("| Metadata | Value |", StringComparison.Ordinal))
            {
                inTable = true;
                continue;
            }

            if (!inTable)
            {
                continue;
            }

            if (line.Trim().Equals("|---|---|", StringComparison.Ordinal))
            {
                continue;
            }

            if (!line.TrimStart().StartsWith('|'))
            {
                break;
            }

            var cells = line.Trim().Trim('|').Split('|');
            if (cells.Length >= 2)
            {
                metadata[cells[0].Trim()] = cells[1].Trim();
            }
        }

        return metadata;
    }

    private static IReadOnlyDictionary<string, RegistryDocument> LoadLogicalIdentityRegistry(string root)
    {
        var path = Path.Combine(root, "repository", "lir", "document-index.json");
        if (File.Exists(path))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var registryDocuments = json.RootElement.ValueKind == JsonValueKind.Array
                ? json.RootElement.EnumerateArray().Select(ReadRegistryDocument)
                : json.RootElement.EnumerateObject().Select(property => ReadRegistryDocument(property.Value));
            return registryDocuments
                .Where(doc => doc.DocumentId.Length > 0)
                .ToDictionary(doc => doc.DocumentId, StringComparer.Ordinal);
        }

        path = Path.Combine(root, "repository", "lir", "logical-identity-registry.yaml");
        if (!File.Exists(path))
        {
            return new Dictionary<string, RegistryDocument>(StringComparer.Ordinal);
        }

        return ReadRegistryYaml(path).ToDictionary(doc => doc.DocumentId, StringComparer.Ordinal);
    }

    private static RegistryDocument ReadRegistryDocument(JsonElement element)
    {
        static string Get(JsonElement element, string name) =>
            element.TryGetProperty(name, out var property) ? property.GetString() ?? string.Empty : string.Empty;

        var dependencies = element.TryGetProperty("dependencies", out var deps)
            ? deps.EnumerateArray().Select(dep => dep.GetString() ?? string.Empty).Where(dep => dep.Length > 0).ToArray()
            : [];

        return new RegistryDocument(
            Get(element, "document_id"),
            Get(element, "artifact_id"),
            Get(element, "version"),
            Get(element, "status"),
            Get(element, "path"),
            Get(element, "sha256"),
            dependencies);
    }

    private static IEnumerable<RegistryDocument> ReadRegistryYaml(string path)
    {
        var current = new Dictionary<string, string>(StringComparer.Ordinal);
        var dependencies = new List<string>();
        var inDocuments = false;
        var inDependencies = false;

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("documents:", StringComparison.Ordinal))
            {
                inDocuments = true;
                continue;
            }

            if (!inDocuments)
            {
                continue;
            }

            if (trimmed.Equals("release_ids:", StringComparison.Ordinal))
            {
                break;
            }

            if (trimmed.StartsWith("- document_id:", StringComparison.Ordinal))
            {
                if (current.Count > 0)
                {
                    yield return BuildRegistryDocument(current, dependencies);
                }

                current = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["document_id"] = trimmed["- document_id:".Length..].Trim()
                };
                dependencies = [];
                inDependencies = false;
                continue;
            }

            if (current.Count == 0)
            {
                continue;
            }

            if (trimmed.Equals("dependencies:", StringComparison.Ordinal))
            {
                inDependencies = true;
                continue;
            }

            if (inDependencies && trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                dependencies.Add(trimmed[1..].Trim());
                continue;
            }

            inDependencies = false;
            if (trimmed.Contains(':', StringComparison.Ordinal))
            {
                var parts = trimmed.Split(':', 2);
                current[parts[0].Trim()] = parts[1].Trim().Trim('\'');
            }
        }

        if (current.Count > 0)
        {
            yield return BuildRegistryDocument(current, dependencies);
        }
    }

    private static RegistryDocument BuildRegistryDocument(IReadOnlyDictionary<string, string> values, IReadOnlyList<string> dependencies) =>
        new(
            values.GetValueOrDefault("document_id", string.Empty),
            values.GetValueOrDefault("artifact_id", string.Empty),
            values.GetValueOrDefault("version", string.Empty),
            values.GetValueOrDefault("status", string.Empty),
            values.GetValueOrDefault("path", string.Empty),
            values.GetValueOrDefault("sha256", string.Empty),
            dependencies.ToArray());

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> LoadDependencyGraph(string root)
    {
        var path = Path.Combine(root, "repository", "rgtm", "dependency-graph.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(
                property => property.Name,
                property => (IReadOnlyList<string>)property.Value.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static IEnumerable<ValidationFinding> ValidateMetadata(
        IReadOnlyList<ControlledDocument> documents,
        IReadOnlyDictionary<string, RegistryDocument> registry)
    {
        foreach (var document in documents)
        {
            foreach (var field in MetadataFields)
            {
                if (!document.Metadata.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    yield return Finding("GATE-01", "METADATA_FIELD_MISSING", document.Path, $"Missing metadata field '{field}'.");
                }
            }

            if (!AllowedStatuses.Contains(document.Get("Status")))
            {
                yield return Finding("GATE-01", "METADATA_STATUS_INVALID", document.Path, $"Invalid status '{document.Get("Status")}'.");
            }

            if (!VersionPattern.IsMatch(document.Get("Version")))
            {
                yield return Finding("GATE-01", "METADATA_VERSION_INVALID", document.Path, $"Invalid version '{document.Get("Version")}'.");
            }

            if (registry.TryGetValue(document.DocumentId, out var registryDocument)
                && !string.IsNullOrWhiteSpace(registryDocument.Sha256)
                && File.Exists(Path.Combine(Path.GetFullPath("."), document.Path.Replace('/', Path.DirectorySeparatorChar))))
            {
                _ = registryDocument;
            }
        }
    }

    private static IEnumerable<ValidationFinding> ValidateIdentity(
        IReadOnlyList<ControlledDocument> documents,
        IReadOnlyDictionary<string, RegistryDocument> registry)
    {
        foreach (var document in documents)
        {
            if (!DocumentIdPattern.IsMatch(document.DocumentId))
            {
                yield return Finding("GATE-02", "DOCUMENT_ID_INVALID", document.Path, $"Invalid Document ID '{document.DocumentId}'.");
            }

            if (!ArtifactIdPattern.IsMatch(document.ArtifactId))
            {
                yield return Finding("GATE-02", "ARTIFACT_ID_INVALID", document.Path, $"Invalid Artifact ID '{document.ArtifactId}'.");
            }

            if (registry.Count > 0 && !registry.ContainsKey(document.DocumentId))
            {
                yield return Finding("GATE-02", "DOCUMENT_ID_NOT_REGISTERED", document.Path, $"Document ID '{document.DocumentId}' is not registered.");
            }
        }

        foreach (var group in documents.GroupBy(document => document.DocumentId).Where(group => group.Key.Length > 0 && group.Count() > 1))
        {
            foreach (var document in group.OrderBy(document => document.Path, StringComparer.Ordinal))
            {
                yield return Finding("GATE-02", "DOCUMENT_ID_DUPLICATE", document.Path, $"Duplicate Document ID '{group.Key}'.");
            }
        }

        foreach (var group in documents.GroupBy(document => document.ArtifactId).Where(group => group.Key.Length > 0 && group.Count() > 1))
        {
            foreach (var document in group.OrderBy(document => document.Path, StringComparer.Ordinal))
            {
                yield return Finding("GATE-02", "ARTIFACT_ID_DUPLICATE", document.Path, $"Duplicate Artifact ID '{group.Key}'.");
            }
        }
    }

    private static IEnumerable<ValidationFinding> ValidateCrossReferences(IReadOnlyList<ControlledDocument> documents)
    {
        var documentIds = documents.Select(document => document.DocumentId).ToHashSet(StringComparer.Ordinal);
        var artifactIds = documents.Select(document => document.ArtifactId).ToHashSet(StringComparer.Ordinal);

        foreach (var document in documents)
        {
            foreach (Match match in ReferencePattern.Matches(document.Content))
            {
                var reference = match.Value;
                var resolved = reference.StartsWith("ART-", StringComparison.Ordinal)
                    ? artifactIds.Contains(reference)
                    : documentIds.Contains(reference);
                if (!resolved)
                {
                    yield return Finding("GATE-03", "CROSS_REFERENCE_UNRESOLVED", document.Path, $"Reference '{reference}' does not resolve to a controlled document or artifact.");
                }
            }
        }
    }

    private static IEnumerable<ValidationFinding> ValidateDependencies(
        IReadOnlyList<ControlledDocument> documents,
        IReadOnlyDictionary<string, IReadOnlyList<string>> dependencyGraph)
    {
        var byId = documents
            .GroupBy(document => document.DocumentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var document in documents)
        {
            foreach (var dependency in document.Dependencies)
            {
                if (!DocumentIdPattern.IsMatch(dependency))
                {
                    continue;
                }

                if (!byId.ContainsKey(dependency))
                {
                    yield return Finding("GATE-04", "DEPENDENCY_MISSING", document.Path, $"Dependency '{dependency}' is not present as a controlled document.");
                }
            }

            if (dependencyGraph.Count > 0 && dependencyGraph.TryGetValue(document.DocumentId, out var graphDependencies))
            {
                var metadataDependencies = document.Dependencies.Order(StringComparer.Ordinal).ToArray();
                if (!metadataDependencies.SequenceEqual(graphDependencies.Order(StringComparer.Ordinal), StringComparer.Ordinal))
                {
                    yield return Finding("GATE-04", "DEPENDENCY_GRAPH_MISMATCH", document.Path, $"Dependency graph projection differs for '{document.DocumentId}'.");
                }
            }
        }

        foreach (var cycle in FindCycles(documents))
        {
            yield return Finding("GATE-04", "DEPENDENCY_CYCLE", cycle, $"Dependency cycle detected at '{cycle}'.");
        }
    }

    private static IEnumerable<string> FindCycles(IReadOnlyList<ControlledDocument> documents)
    {
        var graph = documents
            .GroupBy(document => document.DocumentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.First().Dependencies, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var cycles = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var node in graph.Keys.Order(StringComparer.Ordinal))
        {
            Visit(node);
        }

        return cycles;

        void Visit(string node)
        {
            if (visited.Contains(node))
            {
                return;
            }

            if (!visiting.Add(node))
            {
                cycles.Add(node);
                return;
            }

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies.Order(StringComparer.Ordinal))
                {
                    if (graph.ContainsKey(dependency))
                    {
                        Visit(dependency);
                    }
                }
            }

            visiting.Remove(node);
            visited.Add(node);
        }
    }

    private static IEnumerable<ValidationFinding> ValidateAuthority(IReadOnlyList<ControlledDocument> documents)
    {
        var byId = documents
            .GroupBy(document => document.DocumentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var document in documents)
        {
            foreach (var dependencyId in document.Dependencies)
            {
                if (!byId.TryGetValue(dependencyId, out var dependency))
                {
                    continue;
                }

                if (dependency.AuthorityRank > document.AuthorityRank && !IsCompositionalRollup(document))
                {
                    yield return Finding("GATE-05", "AUTHORITY_INVERSION", document.Path, $"'{document.DocumentId}' depends on lower-authority '{dependencyId}'.");
                }
            }
        }
    }

    private static bool IsCompositionalRollup(ControlledDocument document) =>
        document.Path.EndsWith("README.md", StringComparison.Ordinal)
        || document.DocumentId.Contains("-CERT-", StringComparison.Ordinal)
        || document.DocumentId.Contains("-RELEASE-", StringComparison.Ordinal);

    private static IEnumerable<ValidationFinding> ValidateTraceability(
        IReadOnlyList<ControlledDocument> documents,
        IReadOnlyDictionary<string, string> mappings,
        IReadOnlyDictionary<string, string> rgtmMappings)
    {
        foreach (var gate in DefaultGates())
        {
            if (!mappings.TryGetValue(gate.Id, out var expected))
            {
                yield return Finding("GATE-06", "VALIDATION_MAPPING_MISSING", gate.Id, $"Missing validation mapping for '{gate.Id}'.");
                continue;
            }


            if (!rgtmMappings.TryGetValue(gate.Id, out var actual) || !actual.Equals(expected, StringComparison.Ordinal))
            {
                yield return Finding("GATE-06", "RGTM_VALIDATION_MAPPING_MISMATCH", gate.Id, $"RGTM maps '{gate.Id}' to '{actual}' instead of '{expected}'.");
            }
        }
    }

    private static QualityGateResult WriteEvidence(
        string root,
        string evidenceDirectory,
        (string Id, string Name) gate,
        IReadOnlyDictionary<string, string> mappings,
        IReadOnlyList<ControlledDocument> documents,
        IReadOnlyList<ValidationFinding> allFindings)
    {
        var gateFindings = allFindings.Where(finding => finding.Gate.Equals(gate.Id, StringComparison.Ordinal)).ToArray();
        var result = gateFindings.Length == 0 ? "PASS" : "FAIL";
        var artifactIds = documents.Select(document => document.ArtifactId).Where(id => id.Length > 0).Order(StringComparer.Ordinal).ToArray();
        var digestInput = string.Join('\n', gateFindings.Select(finding => $"{finding.Gate}|{finding.Code}|{finding.Artifact}|{finding.Message}"));
        var evidence = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["artifact_ids"] = artifactIds,
            ["end_timestamp"] = RvecTimestamp,
            ["evidence_digest"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestInput))).ToLowerInvariant(),
            ["execution_mode"] = "Automated",
            ["findings"] = gateFindings,
            ["quality_gate"] = gate.Id,
            ["repository_version"] = LoadRepositoryVersion(root),
            ["result"] = result,
            ["rvec_version"] = "1.0",
            ["start_timestamp"] = RvecTimestamp,
            ["tool"] = ToolName,
            ["tool_version"] = ToolVersion,
            ["validation_standard_id"] = mappings.GetValueOrDefault(gate.Id, string.Empty)
        };

        var path = Path.Combine(evidenceDirectory, $"implementation-wp-001-{gate.Id.ToLowerInvariant()}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(evidence, JsonOptions) + Environment.NewLine);
        return new QualityGateResult(gate.Id, gate.Name, gateFindings.Length == 0 ? GateResult.Pass : GateResult.Fail, gateFindings.Length, RelativePath(root, path));
    }

    private static string LoadRepositoryVersion(string root)
    {
        var manifest = Path.Combine(root, "release-manifest.json");
        if (!File.Exists(manifest))
        {
            return "unknown";
        }

        using var json = JsonDocument.Parse(File.ReadAllText(manifest));
        return json.RootElement.TryGetProperty("version", out var version)
            ? version.GetString() ?? "unknown"
            : "unknown";
    }

    private static ValidationFinding Finding(string gate, string code, string artifact, string message) =>
        new(gate, code, "Mandatory", artifact, message);

    private static string RelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private sealed record RegistryDocument(
        string DocumentId,
        string ArtifactId,
        string Version,
        string Status,
        string Path,
        string Sha256,
        IReadOnlyList<string> Dependencies);
}

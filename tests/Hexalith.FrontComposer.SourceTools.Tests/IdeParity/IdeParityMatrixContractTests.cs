using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Conformance;
using Hexalith.FrontComposer.SourceTools.Conformance;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

[Trait("MatrixRowId", "IDE-MUST-001")]
[Trait("MatrixRowId", "IDE-VERSION-001")]
public sealed class IdeParityMatrixContractTests
{
    private static readonly Regex _commitShaPattern = new("^[0-9a-f]{40}$", RegexOptions.Compiled);
    private static readonly Regex _artifactHashPattern = new("^sha256:[0-9a-f]{64}$", RegexOptions.Compiled);

    [Fact]
    public void MatrixJson_HasFailClosedSchemaForEveryRow()
    {
        using JsonDocument matrix = LoadMatrix();
        JsonElement root = matrix.RootElement;
        ValidateMatrixDocument(root).ShouldBeEmpty("IDE parity matrix JSON must reject unknown fields with a named category.");

        JsonElement metadata = root.GetProperty("metadata");
        RequiredString(metadata, "dotnetSdk").ShouldBe("10.0.103");
        RequiredString(metadata, "visualStudio").ShouldContain("Visual Studio 2022 17.13");
        RequiredString(metadata, "rider").ShouldContain("Rider 2026.1");
        RequiredString(metadata, "vsCode").ShouldContain("C# Dev Kit");
        RequiredString(metadata, "generatedOutputPathContract").ShouldBe(GeneratedOutputPathContract.Template);
        RequiredString(metadata, "generatedOutputPathContractVersion").ShouldBe(GeneratedOutputPathContract.Version);
        RequiredString(metadata, "fixturePath").ShouldBe("samples/IdeParityCounter");

        JsonElement.ArrayEnumerator rows = root.GetProperty("rows").EnumerateArray();
        HashSet<string> rowIds = new(StringComparer.Ordinal);
        HashSet<string> allowedTiers = ["Must", "Should", "Out-of-scope"];
        HashSet<string> allowedEvidenceTypes = ["ci-automated", "manual-release", "scheduled-manual", "out-of-scope"];

        foreach (JsonElement row in rows)
        {
            string rowId = RequiredString(row, "rowId");
            rowId.ShouldNotBeNullOrWhiteSpace("rowId must not be empty.");
            rowIds.Add(rowId).ShouldBeTrue($"Duplicate matrix row ID '{rowId}' must fail closed.");
            allowedTiers.ShouldContain(RequiredString(row, "tier"));
            allowedEvidenceTypes.ShouldContain(RequiredString(row, "evidenceType"));
            RequiredString(row, "capability").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "fixture").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "validationMethod").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "expectedResult").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "owner").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "lastVerified").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "knownLimitation").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "releaseGate").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "revalidationTrigger").ShouldNotBeNullOrWhiteSpace();
            ShouldBeIsoDate(RequiredString(row, "lastVerified"), "lastVerified", rowId);

            JsonElement ideEvidence = row.GetProperty("ideEvidence");
            RequiredString(ideEvidence.GetProperty("visualStudio"), "version").ShouldContain("17.13");
            RequiredString(ideEvidence.GetProperty("rider"), "version").ShouldContain("2026.1");
            RequiredString(ideEvidence.GetProperty("vsCodeDevKit"), "version").ShouldContain("C# Dev Kit");

            string tier = RequiredString(row, "tier");
            string evidenceType = RequiredString(row, "evidenceType");
            if (tier == "Out-of-scope" || evidenceType == "out-of-scope")
            {
                row.TryGetProperty("evidenceArtifact", out _).ShouldBeFalse(
                    $"Out-of-scope row '{rowId}' must not advertise an evidenceArtifact path; the schema fails closed for unresolved evidence references.");
                continue;
            }

            string evidenceArtifact = RequiredString(row, "evidenceArtifact");
            evidenceArtifact.ShouldStartWith("artifacts/ide-parity/");
            evidenceArtifact.Contains('\\', StringComparison.Ordinal).ShouldBeFalse(
                $"Evidence path '{evidenceArtifact}' for '{rowId}' must use forward slashes.");
        }

        rowIds.ShouldContain("IDE-MUST-001");
        rowIds.ShouldContain("IDE-MUST-006");
        rowIds.ShouldContain("IDE-SHOULD-004");
        rowIds.ShouldContain("IDE-REMOTE-001");
        rowIds.ShouldContain("IDE-VERSION-001");
        rowIds.Count.ShouldBeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public void MatrixMarkdown_IsAuthoritativeAndNamesRequiredLimitations()
    {
        string markdown = File.ReadAllText(Path.Combine(IdeParityRepositoryRoot.Value, "docs", "ide-parity-matrix.md"));

        markdown.ShouldContain("authoritative parity contract");
        markdown.ShouldContain("Visual Studio is the calibration IDE");
        markdown.ShouldContain("C# Dev Kit is required");
        markdown.ShouldContain("OmniSharp-only VS Code is unsupported in v1");
        markdown.ShouldContain("direct generated-file rename is unsupported");
        markdown.ShouldContain("Generated files remain read-only by design");
        markdown.ShouldContain(GeneratedOutputPathContract.Template);
        markdown.ShouldContain("Evidence Manifest Schema");
        markdown.ShouldContain("samples/IdeParityCounter");
        markdown.ShouldContain("IDE-MUST-001");
        markdown.ShouldContain("IDE-REMOTE-001");
    }

    [Fact]
    public void EvidenceManifests_AreBoundToRowsCommitFixtureHashAndSafePaths()
    {
        using JsonDocument matrix = LoadMatrix();
        string fixtureHash = IdeParityConformanceUtilityTests.ComputeIdeParityCounterFixtureHash();

        Dictionary<string, string> evidenceByRow = matrix.RootElement
            .GetProperty("rows")
            .EnumerateArray()
            .Where(row => row.GetProperty("evidenceType").GetString() != "out-of-scope")
            .ToDictionary(
                row => row.GetProperty("rowId").GetString()!,
                row => row.GetProperty("evidenceArtifact").GetString()!,
                StringComparer.Ordinal);

        foreach ((string rowId, string artifactPath) in evidenceByRow)
        {
            artifactPath.Contains('\\', StringComparison.Ordinal).ShouldBeFalse(
                $"evidenceArtifact for '{rowId}' must use forward slashes.");
            string fullPath = Path.Combine(IdeParityRepositoryRoot.Value, artifactPath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(fullPath).ShouldBeTrue($"Evidence artifact '{artifactPath}' for '{rowId}' must exist.");

            using JsonDocument manifest = LoadStrictJsonDocument(fullPath);
            JsonElement root = manifest.RootElement;
            ValidateEvidenceManifest(root).ShouldBeEmpty($"Evidence manifest '{artifactPath}' must reject unknown fields with a named category.");
            root.GetProperty("rowId").GetString().ShouldBe(rowId);
            RequiredString(root, "fixtureName").ShouldBe("samples/IdeParityCounter");
            string commitSha = RequiredString(root, "repositoryCommitSha");
            _commitShaPattern.IsMatch(commitSha).ShouldBeTrue($"repositoryCommitSha '{commitSha}' for '{rowId}' must be a 40-char lowercase hex SHA-1.");
            RequiredString(root, "generatedOutputPathContractVersion").ShouldBe(GeneratedOutputPathContract.Version);

            string artifactHash = RequiredString(root, "artifactHash");
            _artifactHashPattern.IsMatch(artifactHash).ShouldBeTrue($"artifactHash '{artifactHash}' for '{rowId}' must match ^sha256:[0-9a-f]{{64}}$.");

            RequiredString(root, "fixtureContentHash").ShouldBe(fixtureHash);
            RequiredString(root, "owner").ShouldNotBeNullOrWhiteSpace();
            ShouldBeIsoDate(RequiredString(root, "lastVerified"), "lastVerified", rowId);
            ShouldBeIsoDate(RequiredString(root, "expiresOn"), "expiresOn", rowId);
            DateOnly lastVerified = ParseIsoDate(RequiredString(root, "lastVerified"));
            DateOnly expiresOn = ParseIsoDate(RequiredString(root, "expiresOn"));
            (expiresOn > lastVerified).ShouldBeTrue($"expiresOn for '{rowId}' must be after lastVerified.");
            RequiredString(root, "revalidationTrigger").ShouldNotBeNullOrWhiteSpace();

            string sanitizedArtifact = RequiredString(root, "sanitizedArtifact");
            IdeParityEvidencePath.TryNormalizeProjectRelativePath(IdeParityRepositoryRoot.Value, sanitizedArtifact, caseSensitive: false, out string normalized)
                .ShouldBeTrue($"Evidence path '{sanitizedArtifact}' must normalize inside the repository.");
            normalized.ShouldBe(sanitizedArtifact);
        }
    }

    [Fact]
    public void StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas()
    {
        Should.Throw<JsonException>(() => AssertNoDuplicateJsonProperties("""{ "metadata": {}, "metadata": {}, "rows": [] }"""))
            .Message.ShouldContain("Duplicate JSON property");

        JsonObject matrix = JsonNode.Parse(File.ReadAllText(Path.Combine(IdeParityRepositoryRoot.Value, "docs", "ide-parity-matrix.json")))!.AsObject();
        matrix["unexpected"] = "tampered";
        using JsonDocument tamperedMatrix = JsonDocument.Parse(matrix.ToJsonString());
        ValidateMatrixDocument(tamperedMatrix.RootElement).ShouldContain("matrix-unknown-property:unexpected");

        JsonObject manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(IdeParityRepositoryRoot.Value, "artifacts", "ide-parity", "evidence", "IDE-MUST-001.json")))!.AsObject();
        manifest["unexpected"] = "tampered";
        using JsonDocument tamperedManifest = JsonDocument.Parse(manifest.ToJsonString());
        ValidateEvidenceManifest(tamperedManifest.RootElement).ShouldContain("evidence-unknown-property:unexpected");

        Should.Throw<JsonException>(() => LoadStrictJsonDocumentFromString("""{ "rows": [], }"""))
            .Message.ShouldNotBeNullOrWhiteSpace("trailing commas must stay fail-closed through System.Text.Json strict parsing.");
    }

    [Fact]
    public void ProductionSource_ForbidsUnconditionalDebuggerLaunch()
    {
        string[] matches = Directory.EnumerateFiles(Path.Combine(IdeParityRepositoryRoot.Value, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("Debugger.Launch(", StringComparison.Ordinal))
            .Select(path => path[IdeParityRepositoryRoot.Value.Length..].TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        matches.ShouldBeEmpty("CONTRIBUTING.md permits Debugger.Launch() only in local investigation branches; production source must stay free of unconditional generator-host launch prompts.");
    }

    private static JsonDocument LoadMatrix()
        => LoadStrictJsonDocument(Path.Combine(IdeParityRepositoryRoot.Value, "docs", "ide-parity-matrix.json"));

    private static JsonDocument LoadStrictJsonDocument(string path)
        => LoadStrictJsonDocumentFromString(File.ReadAllText(path));

    private static JsonDocument LoadStrictJsonDocumentFromString(string json)
    {
        AssertNoDuplicateJsonProperties(json);
        return JsonDocument.Parse(json);
    }

    private static void AssertNoDuplicateJsonProperties(string json)
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
        Stack<HashSet<string>> scopes = new();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    scopes.Push(new HashSet<string>(StringComparer.Ordinal));
                    break;
                case JsonTokenType.EndObject:
                    _ = scopes.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    string propertyName = reader.GetString()!;
                    if (scopes.Count > 0 && !scopes.Peek().Add(propertyName))
                    {
                        throw new JsonException($"Duplicate JSON property '{propertyName}' is not allowed in IDE parity manifests.");
                    }

                    break;
            }
        }
    }

    private static IEnumerable<string> ValidateMatrixDocument(JsonElement root)
    {
        HashSet<string> rootAllowed = ["metadata", "manifestSchema", "rows"];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!rootAllowed.Contains(property.Name))
            {
                yield return "matrix-unknown-property:" + property.Name;
            }
        }

        if (root.TryGetProperty("metadata", out JsonElement metadata))
        {
            HashSet<string> metadataAllowed = [
                "schemaVersion",
                "matrixName",
                "lastValidated",
                "dotnetSdk",
                "sourceToolsPackage",
                "generatedOutputPathContractVersion",
                "generatedOutputPathContract",
                "visualStudio",
                "rider",
                "vsCode",
                "calibrationIde",
                "fixturePath",
                "fixtureContentHashScope",
            ];
            foreach (JsonProperty property in metadata.EnumerateObject())
            {
                if (!metadataAllowed.Contains(property.Name))
                {
                    yield return "matrix-metadata-unknown-property:" + property.Name;
                }
            }
        }

        if (root.TryGetProperty("manifestSchema", out JsonElement manifestSchema))
        {
            HashSet<string> manifestSchemaAllowed = ["requiredFields", "artifactRoot"];
            foreach (JsonProperty property in manifestSchema.EnumerateObject())
            {
                if (!manifestSchemaAllowed.Contains(property.Name))
                {
                    yield return "matrix-manifest-schema-unknown-property:" + property.Name;
                }
            }
        }

        if (!root.TryGetProperty("rows", out JsonElement rows) || rows.ValueKind != JsonValueKind.Array)
        {
            yield return "matrix-missing-rows";
            yield break;
        }

        HashSet<string> rowAllowed = [
            "rowId",
            "capability",
            "tier",
            "fixture",
            "validationMethod",
            "expectedResult",
            "evidenceType",
            "evidenceArtifact",
            "owner",
            "lastVerified",
            "knownLimitation",
            "releaseGate",
            "revalidationTrigger",
            "ideEvidence",
        ];
        HashSet<string> ideAllowed = ["visualStudio", "rider", "vsCodeDevKit"];
        HashSet<string> ideDetailAllowed = ["version", "platform", "status"];
        foreach (JsonElement row in rows.EnumerateArray())
        {
            foreach (JsonProperty property in row.EnumerateObject())
            {
                if (!rowAllowed.Contains(property.Name))
                {
                    yield return "matrix-row-unknown-property:" + property.Name;
                }
            }

            if (!row.TryGetProperty("ideEvidence", out JsonElement ideEvidence))
            {
                continue;
            }

            foreach (JsonProperty ide in ideEvidence.EnumerateObject())
            {
                if (!ideAllowed.Contains(ide.Name))
                {
                    yield return "matrix-ide-unknown-property:" + ide.Name;
                    continue;
                }

                foreach (JsonProperty detail in ide.Value.EnumerateObject())
                {
                    if (!ideDetailAllowed.Contains(detail.Name))
                    {
                        yield return "matrix-ide-detail-unknown-property:" + detail.Name;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> ValidateEvidenceManifest(JsonElement root)
    {
        HashSet<string> allowed = [
            "rowId",
            "fixtureName",
            "fixtureContentHash",
            "repositoryCommitSha",
            "generatedOutputPathContractVersion",
            "ideVersions",
            "osOrContainerImage",
            "validationCommandOrManualSteps",
            "artifactHash",
            "owner",
            "lastVerified",
            "expiresOn",
            "revalidationTrigger",
            "sanitizedArtifact",
        ];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                yield return "evidence-unknown-property:" + property.Name;
            }
        }

        if (root.TryGetProperty("ideVersions", out JsonElement ideVersions))
        {
            HashSet<string> versionAllowed = ["visualStudio", "rider", "vsCodeDevKit", "dotnetSdk"];
            foreach (JsonProperty version in ideVersions.EnumerateObject())
            {
                if (!versionAllowed.Contains(version.Name))
                {
                    yield return "evidence-ide-version-unknown-property:" + version.Name;
                }
            }
        }
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        element.TryGetProperty(propertyName, out JsonElement property).ShouldBeTrue($"Missing '{propertyName}'.");
        property.ValueKind.ShouldBe(JsonValueKind.String);
        return property.GetString()!;
    }

    private static void ShouldBeIsoDate(string value, string field, string rowId)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            .ShouldBeTrue($"{field} for '{rowId}' must be ISO yyyy-MM-dd; got '{value}'.");

    private static DateOnly ParseIsoDate(string value)
        => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}

using System.Globalization;
using System.Text.Json;
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

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(fullPath));
            JsonElement root = manifest.RootElement;
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

    private static JsonDocument LoadMatrix()
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(IdeParityRepositoryRoot.Value, "docs", "ide-parity-matrix.json")));

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

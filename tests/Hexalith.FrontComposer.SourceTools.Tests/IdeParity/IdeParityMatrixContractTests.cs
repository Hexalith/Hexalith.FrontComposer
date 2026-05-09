using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Conformance;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

public sealed class IdeParityMatrixContractTests
{
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
        RequiredString(metadata, "generatedOutputPathContract")
            .ShouldBe(GeneratedOutputPathContract.Template);

        JsonElement.ArrayEnumerator rows = root.GetProperty("rows").EnumerateArray();
        HashSet<string> rowIds = new(StringComparer.Ordinal);
        HashSet<string> allowedTiers = ["Must", "Should", "Out-of-scope"];
        HashSet<string> allowedEvidenceTypes = ["ci-automated", "manual-release", "scheduled-manual", "out-of-scope"];

        foreach (JsonElement row in rows)
        {
            string rowId = RequiredString(row, "rowId");
            rowIds.Add(rowId).ShouldBeTrue($"Duplicate matrix row ID '{rowId}' must fail closed.");
            allowedTiers.ShouldContain(RequiredString(row, "tier"));
            allowedEvidenceTypes.ShouldContain(RequiredString(row, "evidenceType"));
            RequiredString(row, "capability").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "fixture").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "validationMethod").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "expectedResult").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "evidenceArtifact").ShouldStartWith("artifacts/ide-parity/");
            RequiredString(row, "owner").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "lastVerified").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "knownLimitation").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "releaseGate").ShouldNotBeNullOrWhiteSpace();
            RequiredString(row, "revalidationTrigger").ShouldNotBeNullOrWhiteSpace();

            JsonElement ideEvidence = row.GetProperty("ideEvidence");
            RequiredString(ideEvidence.GetProperty("visualStudio"), "version").ShouldContain("17.13");
            RequiredString(ideEvidence.GetProperty("rider"), "version").ShouldContain("2026.1");
            RequiredString(ideEvidence.GetProperty("vsCodeDevKit"), "version").ShouldContain("C# Dev Kit");
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
        string markdown = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "ide-parity-matrix.md"));

        markdown.ShouldContain("authoritative parity contract");
        markdown.ShouldContain("Visual Studio is the calibration IDE");
        markdown.ShouldContain("C# Dev Kit is required");
        markdown.ShouldContain("OmniSharp-only VS Code is unsupported in v1");
        markdown.ShouldContain("direct generated-file rename is unsupported");
        markdown.ShouldContain("Generated files remain read-only by design");
        markdown.ShouldContain("obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs");
        markdown.ShouldContain("Evidence Manifest Schema");
        markdown.ShouldContain("IDE-MUST-001");
        markdown.ShouldContain("IDE-REMOTE-001");
    }

    [Fact]
    public void EvidenceManifests_AreBoundToRowsCommitFixtureHashAndSafePaths()
    {
        using JsonDocument matrix = LoadMatrix();
        Dictionary<string, string> evidenceByRow = matrix.RootElement
            .GetProperty("rows")
            .EnumerateArray()
            .Where(row => row.GetProperty("evidenceType").GetString() != "out-of-scope")
            .ToDictionary(row => row.GetProperty("rowId").GetString()!, row => row.GetProperty("evidenceArtifact").GetString()!, StringComparer.Ordinal);

        foreach ((string rowId, string artifactPath) in evidenceByRow)
        {
            string fullPath = Path.Combine(RepositoryRoot, artifactPath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(fullPath).ShouldBeTrue($"Evidence artifact '{artifactPath}' for '{rowId}' must exist.");

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(fullPath));
            JsonElement root = manifest.RootElement;
            root.GetProperty("rowId").GetString().ShouldBe(rowId);
            RequiredString(root, "fixtureName").ShouldBe("IdeParityCounterFixture");
            RequiredString(root, "repositoryCommitSha").Length.ShouldBe(40);
            RequiredString(root, "generatedOutputPathContractVersion").ShouldBe("v1");
            RequiredString(root, "artifactHash").ShouldStartWith("sha256:");
            RequiredString(root, "owner").ShouldNotBeNullOrWhiteSpace();
            RequiredString(root, "lastVerified").ShouldNotBeNullOrWhiteSpace();
            RequiredString(root, "expiresOn").ShouldNotBeNullOrWhiteSpace();
            RequiredString(root, "revalidationTrigger").ShouldNotBeNullOrWhiteSpace();

            string sanitizedArtifact = RequiredString(root, "sanitizedArtifact");
            IdeParityEvidencePath.TryNormalizeProjectRelativePath(RepositoryRoot, sanitizedArtifact, caseSensitive: false, out string normalized)
                .ShouldBeTrue($"Evidence path '{sanitizedArtifact}' must normalize inside the repository.");
            normalized.ShouldBe(sanitizedArtifact);
        }
    }

    private static JsonDocument LoadMatrix()
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "ide-parity-matrix.json")));

    private static string RequiredString(JsonElement element, string propertyName)
    {
        element.TryGetProperty(propertyName, out JsonElement property).ShouldBeTrue($"Missing '{propertyName}'.");
        property.ValueKind.ShouldBe(JsonValueKind.String);
        return property.GetString()!;
    }

    private static string RepositoryRoot
    {
        get
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.sln")))
            {
                directory = directory.Parent;
            }

            directory.ShouldNotBeNull("Tests must run under the repository checkout.");
            return directory.FullName;
        }
    }
}

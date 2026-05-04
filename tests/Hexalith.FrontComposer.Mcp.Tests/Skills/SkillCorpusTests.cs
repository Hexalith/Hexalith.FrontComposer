using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

public sealed class SkillCorpusTests {
    [Fact]
    public void EmbeddedCorpus_LoadsDeterministicFrameworkResources() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();

        snapshot.Diagnostics.ShouldBeEmpty();
        snapshot.Resources.Count.ShouldBeGreaterThanOrEqualTo(11);
        snapshot.Resources.Select(r => r.ResourceUri).ShouldBe(snapshot.Resources
            .OrderBy(r => r.Order)
            .ThenBy(r => r.ResourceUri, StringComparer.Ordinal)
            .Select(r => r.ResourceUri));
        snapshot.Resources.ShouldAllBe(r => r.ResourceUri.StartsWith("frontcomposer://skills/", StringComparison.Ordinal));
        snapshot.Resources.ShouldAllBe(r => r.ContentType == "text/markdown");
        snapshot.Resources.ShouldAllBe(r => r.Fingerprint != null);
        snapshot.Resources.Select(r => r.ResourceUri).ShouldContain("frontcomposer://skills/index");
    }

    [Fact]
    public void Extraction_ExposesOnlyAgentReferenceSections() {
        const string Source = """
            ---
            id: example
            title: Example
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/example
            order: 1
            sourceDoc: docs/skills/frontcomposer/example.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section narrative -->
            Human-only concept text.
            <!-- /frontcomposer:section -->
            <!-- frontcomposer:section agent-reference -->
            Agent-safe reference text.
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/example.md", Source),
        ]);

        snapshot.Diagnostics.ShouldBeEmpty();
        snapshot.Resources.Single().Markdown.ShouldContain("Agent-safe reference text.");
        snapshot.Resources.Single().Markdown.ShouldNotContain("Human-only concept text.");
        snapshot.Resources.Single().Fingerprint!.AlgorithmId.ShouldBe("frontcomposer.schema.sha256.canonical-json.v1");
        snapshot.Resources.Single().Fingerprint!.Value.Length.ShouldBe(64);
    }

    [Fact]
    public void Extraction_FailsClosedOnUnknownNestedOrUnterminatedMarkers() {
        string BuildSource(string body) => $$"""
            ---
            id: bad
            title: Bad
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/bad
            order: 1
            sourceDoc: docs/skills/frontcomposer/bad.md
            narrative: true
            references: true
            ---
            {{body}}
            """;

        string[] bodies = [
            "<!-- frontcomposer:section hidden -->\nNope.\n<!-- /frontcomposer:section -->",
            "<!-- frontcomposer:section agent-reference -->\n<!-- frontcomposer:section narrative -->\nNope.\n<!-- /frontcomposer:section -->\n<!-- /frontcomposer:section -->",
            "<!-- frontcomposer:section agent-reference -->\nNope.",
        ];

        foreach (string body in bodies) {
            SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
                new SkillCorpusSource("docs/skills/frontcomposer/bad.md", BuildSource(body)),
            ]);

            snapshot.Diagnostics.ShouldNotBeEmpty();
            snapshot.Resources.ShouldBeEmpty();
        }
    }

    [Fact]
    public void ManifestValidation_RejectsDuplicateResourceSlugsCaseInsensitively() {
        const string One = """
            ---
            id: one
            title: One
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/Duplicate
            order: 1
            sourceDoc: docs/skills/frontcomposer/one.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            One
            <!-- /frontcomposer:section -->
            """;
        const string Two = """
            ---
            id: two
            title: Two
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/duplicate
            order: 2
            sourceDoc: docs/skills/frontcomposer/two.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            Two
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/one.md", One),
            new SkillCorpusSource("docs/skills/frontcomposer/two.md", Two),
        ]);

        snapshot.Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.DuplicateResource);
        snapshot.Resources.ShouldBeEmpty();
    }

    [Fact]
    public void ReferenceValidator_FindsMissingPublicApiReferences() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();

        SkillCorpusValidationResult result = SkillCorpusReferenceValidator.Validate(snapshot, [
            typeof(Contracts.Attributes.CommandAttribute).Assembly,
            typeof(FrontComposerMcpOptions).Assembly,
        ]);

        result.Diagnostics.ShouldBeEmpty();

        SkillCorpusValidationResult broken = SkillCorpusReferenceValidator.Validate(
            snapshot with {
                Resources = [
                    snapshot.Resources[0] with {
                        PublicApiReferences = ["Hexalith.FrontComposer.Contracts.Attributes.DoesNotExistAttribute"],
                    },
                ],
            },
            [typeof(Contracts.Attributes.CommandAttribute).Assembly]);

        broken.Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.MissingPublicApiReference);
    }

    [Fact]
    public void ReleaseGuard_RequiresMigrationOwnerForBreakingCorpusChanges() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        SkillCorpusResource resource = snapshot.Resources.Single(r => r.ResourceUri == "frontcomposer://skills/index");

        SkillCorpusReleaseGuard.ValidateBreakingChangesRequireMigration([resource])
            .Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.MigrationGuideMissing);

        SkillCorpusReleaseGuard.ValidateBreakingChangesRequireMigration([
            resource with { MigrationOwner = "Story 9-5" },
        ]).Diagnostics.ShouldBeEmpty();
    }
}

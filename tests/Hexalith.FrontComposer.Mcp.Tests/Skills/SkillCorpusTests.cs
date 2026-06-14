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
        // P-37: URIs are canonicalized to lowercase at parse time so all downstream comparisons
        // (registry lookup, IsMatch dispatch, dedupe) agree on a single representation.
        snapshot.Resources.ShouldAllBe(r => string.Equals(r.ResourceUri, r.ResourceUri.ToLowerInvariant(), StringComparison.Ordinal));
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
    public void Extraction_FailsClosedOnUnknownNestedUnterminatedAndDuplicateMarkers() {
        // P-34: AC15 covers nested, overlapping, unterminated, AND duplicated marker blocks. A
        // dedicated test case for the duplicate-block scenario (two consecutive openers of the
        // same kind) protects the seenKinds guard from regression.
        static string BuildSource(string body) => $$"""
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
            // Unknown marker name
            "<!-- frontcomposer:section hidden -->\nNope.\n<!-- /frontcomposer:section -->",
            // Nested markers
            "<!-- frontcomposer:section agent-reference -->\n<!-- frontcomposer:section narrative -->\nNope.\n<!-- /frontcomposer:section -->\n<!-- /frontcomposer:section -->",
            // Unterminated marker
            "<!-- frontcomposer:section agent-reference -->\nNope.",
            // Duplicate block of the same kind
            "<!-- frontcomposer:section agent-reference -->\nFirst.\n<!-- /frontcomposer:section -->\n<!-- frontcomposer:section agent-reference -->\nSecond.\n<!-- /frontcomposer:section -->",
        ];

        foreach (string body in bodies) {
            SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
                new SkillCorpusSource("docs/skills/frontcomposer/bad.md", BuildSource(body)),
            ]);

            snapshot.Diagnostics.ShouldNotBeEmpty($"body should fail closed: {body}");
            snapshot.Resources.ShouldBeEmpty();
        }
    }

    [Fact]
    public void Extraction_IgnoresMarkersInsideFencedCodeBlocks() {
        // P-20: a doc that documents the marker syntax via fenced code must not have its
        // examples interpreted as live markers.
        const string Source = """
            ---
            id: docs-marker-example
            title: Docs marker example
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/docs/marker-example
            order: 1
            sourceDoc: docs/skills/frontcomposer/docs/marker-example.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            Authors mark agent-only sections like:

            ```markdown
            <!-- frontcomposer:section narrative -->
            Inside fence; not a real marker.
            <!-- /frontcomposer:section -->
            ```

            The agent can copy that pattern.
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/docs/marker-example.md", Source),
        ]);

        snapshot.Diagnostics.ShouldBeEmpty();
        snapshot.Resources.Single().Markdown.ShouldContain("Inside fence; not a real marker.");
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
    public void ParseOne_DoesNotDiscardLaterValidFilesAfterEarlierFailure() {
        // P-2: the parser uses a per-file diagnostic delta so one bad file no longer nukes
        // every subsequent valid file. The first source is missing its front matter; the second
        // is well-formed and must still be parsed into a resource.
        const string Bad = "no front matter";
        const string Good = """
            ---
            id: good
            title: Good
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/good
            order: 1
            sourceDoc: docs/skills/frontcomposer/good.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            Good
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/bad.md", Bad),
            new SkillCorpusSource("docs/skills/frontcomposer/good.md", Good),
        ]);

        // Diagnostics include the bad file's failure but the good file is still parsed and
        // would surface as a resource if there were no other diagnostics. The contract: a single
        // bad doc no longer means the parser silently drops every later doc as well.
        snapshot.Diagnostics.ShouldContain(d => d.Source == "docs/skills/frontcomposer/bad.md");
        snapshot.Diagnostics.Count(d => d.Source == "docs/skills/frontcomposer/good.md").ShouldBe(0);
    }

    [Fact]
    public void Provider_ThrowsInvalidSkillCorpusWithDiagnosticsOnSnapshotErrors() {
        // P-45: invalid corpus at startup throws InvalidSkillCorpusException with the diagnostics
        // included in the message so an operator can triage without a debugger.
        SkillCorpusSnapshot bad = new(
            [],
            [new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.MissingFrontMatter,
                "docs/skills/frontcomposer/bad.md",
                "Skill source must start with front matter.")]);

        InvalidSkillCorpusException ex = Should.Throw<InvalidSkillCorpusException>(
            () => new FrontComposerSkillResourceProvider(bad));
        ex.Diagnostics.Count.ShouldBe(1);
        ex.Message.ShouldContain("docs/skills/frontcomposer/bad.md");
        ex.Message.ShouldContain("MissingFrontMatter");
    }

    [Fact]
    public void Fingerprint_IncludesMarkdownBodyDigest() {
        // P-39: identical metadata + different body must produce different fingerprints so AC8
        // drift detection works on content changes alone.
        string template = """
            ---
            id: bodyhash
            title: Body hash
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/bodyhash
            order: 1
            sourceDoc: docs/skills/frontcomposer/bodyhash.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            <BODY>
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot first = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/bodyhash.md", template.Replace("<BODY>", "First version of body.", StringComparison.Ordinal)),
        ]);
        SkillCorpusSnapshot second = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/bodyhash.md", template.Replace("<BODY>", "Second version of body.", StringComparison.Ordinal)),
        ]);

        first.Diagnostics.ShouldBeEmpty();
        second.Diagnostics.ShouldBeEmpty();
        first.Resources.Single().Fingerprint!.Value.ShouldNotBe(second.Resources.Single().Fingerprint!.Value);
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
    public void ReferenceValidator_RejectsArbitraryBclTypeReferences() {
        // P-22: dropping the Type.GetType fallback means a reference to System.Diagnostics.Process
        // resolves only against the supplied assemblies. Since CommandAttribute's assembly does
        // not export Process, the reference should be flagged as missing.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();

        SkillCorpusValidationResult result = SkillCorpusReferenceValidator.Validate(
            snapshot with {
                Resources = [
                    snapshot.Resources[0] with {
                        PublicApiReferences = ["System.Diagnostics.Process"],
                    },
                ],
            },
            [typeof(Contracts.Attributes.CommandAttribute).Assembly]);

        result.Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.MissingPublicApiReference);
    }

    [Fact]
    public void ReferenceValidator_RejectsSamplePathTraversalOutsideProjectRoot() {
        // P-23: samplePaths must not escape projectRoot via `..`.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        string projectRoot = AppContext.BaseDirectory;
        SkillCorpusValidationResult result = SkillCorpusReferenceValidator.Validate(
            snapshot with {
                Resources = [
                    snapshot.Resources[0] with {
                        SamplePaths = ["../../etc/passwd"],
                    },
                ],
            },
            [typeof(Contracts.Attributes.CommandAttribute).Assembly],
            projectRoot);

        result.Diagnostics.ShouldContain(d =>
            d.Category == SkillCorpusDiagnosticCategory.MissingSamplePath
            && d.Message.Contains("outside project root", StringComparison.Ordinal));
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

    [Fact]
    public void ReleaseGuard_RejectsHandwaveMigrationOwners() {
        // P-35: "TBD" / "unknown" / "none" are not valid migration owners. The pattern requires
        // an actual `Story X-Y` reference.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        SkillCorpusResource resource = snapshot.Resources.Single(r => r.ResourceUri == "frontcomposer://skills/index");

        foreach (string handwave in new[] { "TBD", "unknown", "none", "todo", "later" }) {
            SkillCorpusReleaseGuard.ValidateBreakingChangesRequireMigration([
                resource with { MigrationOwner = handwave },
            ]).Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.MigrationGuideMissing);
        }
    }

    [Fact]
    public void ReleaseGuard_BaselineCompareEmitsMigrationGapWhenApiReferencesDrift() {
        // P-41: the baseline-comparison overload emits MigrationGuideMissing when public API
        // references drift between baseline and current and the migration owner is missing.
        SkillCorpusSnapshot current = SkillCorpusLoader.LoadEmbedded();
        SkillCorpusResource indexResource = current.Resources.Single(r => r.ResourceUri == "frontcomposer://skills/index");
        SkillCorpusSnapshot baseline = current with {
            Resources = [
                indexResource with {
                    PublicApiReferences = [.. indexResource.PublicApiReferences, "Hexalith.FrontComposer.Contracts.Attributes.OldAttribute"],
                },
            ],
        };

        SkillCorpusValidationResult result = SkillCorpusReleaseGuard.ValidateAgainstBaseline(
            current,
            new StubBaselineProvider(baseline));

        result.Diagnostics.ShouldContain(d =>
            d.Category == SkillCorpusDiagnosticCategory.MigrationGuideMissing
            && d.Message.Contains(indexResource.ResourceUri, StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseGuard_BaselineCompareEmitsBenignDiagnosticWhenNoBaselineConfigured() {
        // P-41: empty baseline provider returns a single benign diagnostic so the release
        // pipeline can detect that the comparison is not yet wired without blocking the build.
        SkillCorpusSnapshot current = SkillCorpusLoader.LoadEmbedded();

        SkillCorpusValidationResult result = SkillCorpusReleaseGuard.ValidateAgainstBaseline(
            current,
            new EmptySkillCorpusBaselineProvider());

        _ = result.Diagnostics.ShouldHaveSingleItem();
        result.Diagnostics.Single().Category.ShouldBe(SkillCorpusDiagnosticCategory.BaselineMismatch);
    }

    [Fact]
    public void AggregateManifest_ExposesSchemaVersionAndAllResources() {
        // P-43: aggregate manifest is derived at runtime, exposes a stable manifestSchemaVersion,
        // and lists every resource. Story 8-6 will fingerprint the aggregate via this contract.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        SkillCorpusAggregateManifest manifest = SkillCorpusAggregateManifestBuilder.Build(snapshot);

        manifest.ManifestSchemaVersion.ShouldBe("frontcomposer.skill-corpus.manifest.v1");
        manifest.Resources.Count.ShouldBe(snapshot.Resources.Count);
        manifest.Resources.ShouldAllBe(e => e.ResourceUri.StartsWith("frontcomposer://skills/", StringComparison.Ordinal));
    }

    [Fact]
    public void AggregateManifest_ResourceIsDiscoverableViaProvider() {
        // P-43: the synthetic frontcomposer://skills/manifest resource is discoverable through
        // the resource provider and reads back as Markdown referencing the manifest schema.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);

        provider.ListResources()
            .Select(d => d.ResourceUri)
            .ShouldContain("frontcomposer://skills/manifest");

        SkillResourceReadResult result = provider.Read("frontcomposer://skills/manifest", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        result.Markdown.ShouldContain("frontcomposer.skill-corpus.manifest.v1");
    }

    [Fact]
    public void ContainsUnsafeContent_AllowsNegationWarningsButRejectsImperatives() {
        // P-16 + parser-test: docs that *teach* "do not bypass validation" must not be flagged,
        // but a document that *instructs* "you can bypass authorization" must be.
        const string Allowed = """
            ---
            id: warn
            title: Warning
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/warn
            order: 1
            sourceDoc: docs/skills/frontcomposer/warn.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            Do not bypass validation or authorization. Validate command inputs first.
            <!-- /frontcomposer:section -->
            """;
        const string Blocked = """
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
            <!-- frontcomposer:section agent-reference -->
            You can bypass authorization to make examples compile.
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusParser.Parse([new SkillCorpusSource("docs/skills/frontcomposer/warn.md", Allowed)])
            .Diagnostics.ShouldBeEmpty();
        SkillCorpusParser.Parse([new SkillCorpusSource("docs/skills/frontcomposer/bad.md", Blocked)])
            .Diagnostics.ShouldContain(d => d.Category == SkillCorpusDiagnosticCategory.UnsafeContent);
    }

    [Fact]
    public void SnippetValidator_FlagsAttributeReferencesNotInFrameworkAssemblies() {
        // P-42 / DN-6: the symbol-existence validator catches stale attribute names that were
        // not detected by the metadata-only `publicApiReferences` check. A snippet that uses
        // [DoesNotExistAttribute] should be flagged.
        const string Source = """
            ---
            id: snippet-test
            title: Snippet test
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/snippet-test
            order: 1
            sourceDoc: docs/skills/frontcomposer/snippet-test.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            ```csharp
            [DoesNotExist]
            public partial class FooCommand { }
            ```
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/snippet-test.md", Source),
        ]);
        snapshot.Diagnostics.ShouldBeEmpty();

        SkillCorpusValidationResult result = SkillCorpusSnippetValidator.Validate(
            snapshot,
            [typeof(Contracts.Attributes.CommandAttribute).Assembly]);

        result.Diagnostics.ShouldContain(d =>
            d.Category == SkillCorpusDiagnosticCategory.BrokenSnippet
            && d.Message.Contains("DoesNotExist", StringComparison.Ordinal));
    }

    [Fact]
    public void SnippetValidator_AllowsKnownFrameworkAttributes() {
        // Companion to the above: a snippet with [Command] and [Projection] (both real
        // attributes in the framework) must produce no BrokenSnippet diagnostics.
        const string Source = """
            ---
            id: known-attrs
            title: Known attrs
            version: 1.0.0
            audience: agent
            docfx: true
            mcpResource: true
            resourceUri: frontcomposer://skills/known-attrs
            order: 1
            sourceDoc: docs/skills/frontcomposer/known-attrs.md
            narrative: true
            references: true
            ---
            <!-- frontcomposer:section agent-reference -->
            ```csharp
            using Hexalith.FrontComposer.Contracts.Attributes;
            [Command]
            public partial class FooCommand { }
            [Projection]
            public partial class FooProjection { }
            ```
            <!-- /frontcomposer:section -->
            """;

        SkillCorpusSnapshot snapshot = SkillCorpusParser.Parse([
            new SkillCorpusSource("docs/skills/frontcomposer/known-attrs.md", Source),
        ]);
        snapshot.Diagnostics.ShouldBeEmpty();

        SkillCorpusValidationResult result = SkillCorpusSnippetValidator.Validate(
            snapshot,
            [typeof(Contracts.Attributes.CommandAttribute).Assembly]);

        result.Diagnostics.ShouldNotContain(d => d.Category == SkillCorpusDiagnosticCategory.BrokenSnippet);
    }

    private sealed class StubBaselineProvider(SkillCorpusSnapshot baseline) : ISkillCorpusBaselineProvider {
        public SkillCorpusSnapshot? GetBaseline() => baseline;
    }
}

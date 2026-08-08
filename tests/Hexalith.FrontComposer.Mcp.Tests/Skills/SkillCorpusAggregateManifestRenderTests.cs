using System.Globalization;

using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

/// <summary>
/// Story 11.21 CA1305 regression cover for <see cref="SkillCorpusAggregateManifestBuilder.Render"/>.
/// The eleven interpolated <c>AppendLine</c> calls now take <see cref="CultureInfo.InvariantCulture"/>
/// explicitly.
/// <para>
/// The primary guard is the golden-text assertion: it pins the rendered manifest byte-for-byte, so
/// the culture argument cannot have changed the emitted document. The culture theory below is a
/// forward guard, not a reproduction of a live defect — every interpolation hole in the current
/// template holds a <see langword="string"/> or a non-negative <see langword="int"/>, and standard
/// .NET integer formatting emits ASCII digits under every culture. It exists so that adding a
/// <c>double</c>, <c>decimal</c>, or <c>DateTimeOffset</c> to this agent-visible manifest fails here
/// instead of drifting per-host.
/// </para>
/// <para>
/// <see cref="RenderUnder"/> mutates process-wide culture, so the class joins the serialised
/// <see cref="McpCultureTestGroup"/> collection: without it the mutation would be visible to every
/// test running concurrently in this assembly.
/// </para>
/// </summary>
[Collection(McpCultureTestGroup.Name)]
public sealed class SkillCorpusAggregateManifestRenderTests {
    [Fact]
    public void Render_TwoResourceManifest_ProducesExactExpectedMarkdown() {
        string rendered = Render(Manifest());

        rendered.ShouldBe(ExpectedMarkdown);
    }

    [Fact]
    public void Render_OmitsOptionalLinesWhenTheEntryLeavesThemUnset() {
        // The beta entry has no owning story, migration owner, public API references, or sample
        // paths; each of those lines is conditionally emitted and must be absent from its section.
        const string BetaDelimiter = "### `frontcomposer://skills/beta`";
        string[] parts = Render(Manifest()).Split(BetaDelimiter);
        parts.Length.ShouldBeGreaterThan(
            1,
            $"rendered markdown must contain the beta section delimiter {BetaDelimiter}");
        string betaSection = parts[1];

        betaSection.ShouldNotContain("- owningStory:");
        betaSection.ShouldNotContain("- migrationOwner:");
        betaSection.ShouldNotContain("- publicApiReferences:");
        betaSection.ShouldNotContain("- samplePaths:");
        betaSection.ShouldContain("- version: `2.1.0`");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    [InlineData("hi-IN")]
    public void Render_UnderNonInvariantCulture_MatchesInvariantOutputAndKeepsAsciiDigits(string cultureName) {
        SkillCorpusAggregateManifest manifest = Manifest();
        string invariant = RenderUnder(CultureInfo.InvariantCulture, manifest);
        string hostile = RenderUnder(new CultureInfo(cultureName), manifest);

        hostile.ShouldBe(invariant);
        hostile.ShouldContain("- resourceCount: `2`", Case.Sensitive);
    }

    private static string ExpectedMarkdown =>
        "# FrontComposer Skill Corpus Manifest" + Environment.NewLine
        + Environment.NewLine
        + "- manifestSchemaVersion: `frontcomposer.skill-corpus.manifest.v1`" + Environment.NewLine
        + "- corpusVersion: `2.1.0`" + Environment.NewLine
        + "- resourceCount: `2`" + Environment.NewLine
        + Environment.NewLine
        + "## Resources" + Environment.NewLine
        + Environment.NewLine
        + "### `frontcomposer://skills/alpha`" + Environment.NewLine
        + Environment.NewLine
        + "- id: `alpha`" + Environment.NewLine
        + "- sourceDoc: `docs/alpha.md`" + Environment.NewLine
        + "- version: `1.0.0`" + Environment.NewLine
        + "- owningStory: `11.21`" + Environment.NewLine
        + "- migrationOwner: `framework`" + Environment.NewLine
        + "- publicApiReferences: `Alpha.One`, `Alpha.Two`" + Environment.NewLine
        + "- samplePaths: `samples/alpha`" + Environment.NewLine
        + Environment.NewLine
        + "### `frontcomposer://skills/beta`" + Environment.NewLine
        + Environment.NewLine
        + "- id: `beta`" + Environment.NewLine
        + "- sourceDoc: `docs/beta.md`" + Environment.NewLine
        + "- version: `2.1.0`" + Environment.NewLine
        + Environment.NewLine;

    private static SkillCorpusAggregateManifest Manifest() => new(
        SkillCorpusAggregateManifestBuilder.ManifestSchemaVersion,
        "2.1.0",
        [
            new SkillCorpusManifestEntry(
                "alpha",
                "frontcomposer://skills/alpha",
                "docs/alpha.md",
                "1.0.0",
                "11.21",
                "framework",
                ["Alpha.One", "Alpha.Two"],
                ["samples/alpha"]),
            new SkillCorpusManifestEntry(
                "beta",
                "frontcomposer://skills/beta",
                "docs/beta.md",
                "2.1.0",
                OwningStory: null,
                MigrationOwner: null,
                [],
                []),
        ]);

    private static string Render(SkillCorpusAggregateManifest manifest)
        => SkillCorpusAggregateManifestBuilder.Render(manifest);

    private static string RenderUnder(CultureInfo culture, SkillCorpusAggregateManifest manifest) {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return Render(manifest);
        }
        finally {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}

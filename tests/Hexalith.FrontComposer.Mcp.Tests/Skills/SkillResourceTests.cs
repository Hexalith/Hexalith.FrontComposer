using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

public sealed class SkillResourceTests {
    [Fact]
    public void Provider_ListsAndReadsSkillResourcesWithoutTenantContext() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);

        SkillResourceDescriptor[] descriptors = [.. provider.ListResources()];

        descriptors.ShouldNotBeEmpty();
        // Aggregate manifest resource is included alongside per-file descriptors.
        descriptors.Select(d => d.ResourceUri).ShouldContain("frontcomposer://skills/manifest");

        SkillResourceReadResult result = provider.Read("frontcomposer://skills/index", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.ContentType.ShouldBe("text/markdown");
        result.Markdown.ShouldContain("FrontComposer");
    }

    [Fact]
    public void Provider_ReturnsUnknownForMissingResourceAndHonorsCancellation() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);

        provider.Read("frontcomposer://skills/missing", CancellationToken.None).Category
            .ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        provider.Read("frontcomposer://skills/index", cts.Token).Category
            .ShouldBe(FrontComposerMcpFailureCategory.Canceled);
    }

    [Fact]
    public void ProtocolAdapter_MapsSkillResourcesAtTheMcpSdkEdge() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);
        FrontComposerSkillMcpResource resource = provider.CreateMcpResources().Single(r => r.Descriptor.ResourceUri == "frontcomposer://skills/index");

        resource.ProtocolResource.Uri.ShouldBe("frontcomposer://skills/index");
        resource.ProtocolResource.MimeType.ShouldBe("text/markdown");
        resource.Metadata.ShouldContain(resource.Descriptor);
    }

    [Fact]
    public void Provider_ReturnsResponseTooLargeWhenMarkdownExceedsCap() {
        // P-27 / P-31: the bounded-response policy returns SkillResourceTooLarge instead of
        // truncating, because partial fenced code samples mislead agents.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot, new SkillResourceReadOptions(MaxCharacters: 10));

        SkillResourceReadResult result = provider.Read("frontcomposer://skills/index", CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.SkillResourceTooLarge);
    }

    [Fact]
    public void Provider_FailureBodyExposesStableOpaqueTokensNotEnumNames() {
        // P-13: failure responses use stable opaque category tokens so the wire format does not
        // depend on internal enum naming and so hidden-equivalent failures (Story 8-4a DN-2)
        // remain indistinguishable.
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);

        SkillResourceReadResult missing = provider.Read("frontcomposer://skills/does-not-exist", CancellationToken.None);
        missing.Markdown.ShouldBe("unknown_resource");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        SkillResourceReadResult canceled = provider.Read("frontcomposer://skills/index", cts.Token);
        canceled.Markdown.ShouldBe("canceled");
    }

    [Fact]
    public void ReadResultFactories_EnforceValidStateMatrix() {
        SkillResourceReadResult success = SkillResourceReadResult.Success("# Safe markdown");
        success.IsSuccess.ShouldBeTrue();
        success.Category.ShouldBe(FrontComposerMcpFailureCategory.None);
        success.ContentType.ShouldBe("text/markdown");
        success.Markdown.ShouldBe("# Safe markdown");

        SkillResourceReadResult missing = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.UnknownResource);
        missing.IsSuccess.ShouldBeFalse();
        missing.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        missing.ContentType.ShouldBe("text/plain");
        missing.Markdown.ShouldBe("unknown_resource");

        SkillResourceReadResult denied = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.AuthFailed);
        denied.IsSuccess.ShouldBeFalse();
        // 11-5 review P4: AuthFailed and UnknownResource intentionally project the same opaque
        // "unknown_resource" markdown body (Story 8-4a DN-2 hidden-equivalence) so an unauth'd
        // probe cannot distinguish "exists but forbidden" from "does not exist". The Category
        // field must remain distinct, however, so internal correlation and telemetry can
        // separate them — a regression that collapses both factories to the same Category would
        // weaken the audit surface without strengthening the hidden-equivalence guarantee.
        denied.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
        denied.Category.ShouldNotBe(missing.Category);
        denied.Markdown.ShouldBe("unknown_resource");

        SkillResourceReadResult invalid = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest);
        invalid.IsSuccess.ShouldBeFalse();
        invalid.Category.ShouldBe(FrontComposerMcpFailureCategory.MalformedRequest);
        invalid.Markdown.ShouldBe("malformed_request");
    }

    [Fact]
    public void PackagingFootprint_EmbedsAllSkillCorpusMarkdownAndPromptSet() {
        // P-30: the .Mcp assembly carries every skill corpus markdown file as an embedded
        // resource plus the v1 benchmark prompt-set JSON. A regression that drops the embed
        // (e.g., csproj edit) would cause the loader to silently see fewer resources, so we
        // assert the count + presence here. This stands in for a full `dotnet pack` content
        // assertion which is heavier to execute in test.
        System.Reflection.Assembly mcpAssembly = typeof(FrontComposerSkillResourceProvider).Assembly;
        string[] embeddedNames = mcpAssembly.GetManifestResourceNames();

        embeddedNames.Where(n => n.StartsWith("Hexalith.FrontComposer.Mcp.Skills.", StringComparison.Ordinal)
                && n.EndsWith(".md", StringComparison.Ordinal))
            .Count()
            .ShouldBeGreaterThanOrEqualTo(11);
        embeddedNames.ShouldContain("Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json");
    }
}

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
        descriptors.ShouldAllBe(d => d.ContentType == "text/markdown");
        descriptors.Select(d => d.ResourceUri).ShouldBe(descriptors
            .OrderBy(d => d.Order)
            .ThenBy(d => d.ResourceUri, StringComparer.Ordinal)
            .Select(d => d.ResourceUri));

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
}

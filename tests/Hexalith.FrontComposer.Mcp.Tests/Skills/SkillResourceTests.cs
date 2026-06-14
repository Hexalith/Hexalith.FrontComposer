using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Skills;

using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using NSubstitute;

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
        resource.IsMatch("frontcomposer://skills/index").ShouldBeTrue();
        resource.IsMatch("frontcomposer://skills/Index").ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => _ = resource.ProtocolResourceTemplate)
            .Message.ShouldContain("URI templates");
    }

    [Fact]
    public async Task ProtocolAdapter_ReadsOnlyRequestedSkillResourceAndEchoesRequestedUri() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);
        FrontComposerSkillMcpResource resource = provider.CreateMcpResources().Single(r => r.Descriptor.ResourceUri == "frontcomposer://skills/index");

        ReadResourceResult result = await resource.ReadAsync(
            Request("frontcomposer://skills/index"),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe("frontcomposer://skills/index");
        text.MimeType.ShouldBe("text/markdown");
        text.Text.ShouldBe(snapshot.Resources.Single(r => r.ResourceUri == "frontcomposer://skills/index").Markdown);
        text.Text.ShouldNotContain("frontcomposer:section narrative");
    }

    [Fact]
    public async Task ProtocolAdapter_SkillReadBypassesProjectionVisibilityGate() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);
        FrontComposerSkillMcpResource resource = provider.CreateMcpResources().Single(r => r.Descriptor.ResourceUri == "frontcomposer://skills/index");
        ServiceCollection services = [];
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, ThrowingVisibilityGate>();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        ReadResourceResult result = await resource.ReadAsync(
            Request("frontcomposer://skills/index", serviceProvider),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.MimeType.ShouldBe("text/markdown");
        text.Text.ShouldBe(snapshot.Resources.Single(r => r.ResourceUri == "frontcomposer://skills/index").Markdown);
    }

    [Fact]
    public async Task ProtocolAdapter_MissingUriUsesDescriptorUriAndMalformedToken() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);
        FrontComposerSkillMcpResource resource = provider.CreateMcpResources().Single(r => r.Descriptor.ResourceUri == "frontcomposer://skills/index");

        ReadResourceResult result = await resource.ReadAsync(
            Request(null),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe("frontcomposer://skills/index");
        text.MimeType.ShouldBe("text/plain");
        text.Text.ShouldBe("malformed_request");
    }

    [Fact]
    public async Task ProtocolAdapter_CancellationReturnsStableTokenWithoutEscaping() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);
        FrontComposerSkillMcpResource resource = provider.CreateMcpResources().Single(r => r.Descriptor.ResourceUri == "frontcomposer://skills/index");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ReadResourceResult result = await resource.ReadAsync(
            Request("frontcomposer://skills/index"),
            cts.Token);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe("frontcomposer://skills/index");
        text.MimeType.ShouldBe("text/plain");
        text.Text.ShouldBe("canceled");
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
        result.Markdown.ShouldBe("response_too_large");
    }

    [Fact]
    public void Provider_ManifestReadIsDeterministicAndIncludesResourceMetadata() {
        SkillCorpusSnapshot snapshot = SkillCorpusLoader.LoadEmbedded();
        var provider = new FrontComposerSkillResourceProvider(snapshot);

        SkillResourceReadResult first = provider.Read("frontcomposer://skills/manifest", CancellationToken.None);
        SkillResourceReadResult second = provider.Read("frontcomposer://skills/manifest", CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        first.ContentType.ShouldBe("text/markdown");
        first.Markdown.ShouldBe(second.Markdown);
        first.Markdown.ShouldContain("manifestSchemaVersion");
        first.Markdown.ShouldContain("corpusVersion");
        first.Markdown.ShouldContain("resourceCount");
        first.Markdown.ShouldContain("publicApiReferences");
        first.Markdown.ShouldContain("samplePaths");
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
        var success = SkillResourceReadResult.Success("# Safe markdown");
        success.IsSuccess.ShouldBeTrue();
        success.Category.ShouldBe(FrontComposerMcpFailureCategory.None);
        success.ContentType.ShouldBe("text/markdown");
        success.Markdown.ShouldBe("# Safe markdown");

        var missing = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.UnknownResource);
        missing.IsSuccess.ShouldBeFalse();
        missing.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        missing.ContentType.ShouldBe("text/plain");
        missing.Markdown.ShouldBe("unknown_resource");

        var denied = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.AuthFailed);
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

        var invalid = SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest);
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

    private static RequestContext<ReadResourceRequestParams> Request(
        string? uri,
        IServiceProvider? services = null) {
        var request = new JsonRpcRequest {
            Id = new RequestId("test-skill-read"),
            Method = RequestMethods.ResourcesRead,
        };
        return new RequestContext<ReadResourceRequestParams>(
            Substitute.For<McpServer>(),
            request,
            new ReadResourceRequestParams {
                Uri = uri!,
            }) {
            Services = services,
        };
    }

    private sealed class ThrowingVisibilityGate : IFrontComposerMcpResourceVisibilityGate {
        public ValueTask<bool> IsVisibleAsync(
            McpResourceDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("skill resource must not call projection visibility gate");
    }
}

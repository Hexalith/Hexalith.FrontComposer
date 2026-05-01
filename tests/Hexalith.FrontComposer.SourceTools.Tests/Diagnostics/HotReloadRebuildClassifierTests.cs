using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class HotReloadRebuildClassifierTests {
    [Theory]
    [InlineData(CustomizationHotReloadChangeKind.RazorBodyEdit)]
    [InlineData(CustomizationHotReloadChangeKind.CssOnlyEdit)]
    public void SupportedBodyAndCssEdits_DoNotRequireRebuild(CustomizationHotReloadChangeKind changeKind) {
        HotReloadRebuildClassification classification = CustomizationHotReloadClassifier.Classify(changeKind);

        classification.RequiresRebuild.ShouldBeFalse();
        classification.DiagnosticId.ShouldBeNull();
    }

    [Theory]
    [InlineData(CustomizationHotReloadChangeKind.MarkerMetadataChanged)]
    [InlineData(CustomizationHotReloadChangeKind.ExpectedContractVersionChanged)]
    [InlineData(CustomizationHotReloadChangeKind.GenericContextTypeChanged)]
    [InlineData(CustomizationHotReloadChangeKind.DescriptorSchemaChanged)]
    [InlineData(CustomizationHotReloadChangeKind.RegistrationAddedOrRemoved)]
    [InlineData(CustomizationHotReloadChangeKind.DuplicateRegistrationIntroduced)]
    [InlineData(CustomizationHotReloadChangeKind.GeneratedManifestVersionMismatch)]
    public void MetadataAndDescriptorChanges_RequireFullRebuild(CustomizationHotReloadChangeKind changeKind) {
        HotReloadRebuildClassification classification = CustomizationHotReloadClassifier.Classify(changeKind);

        classification.RequiresRebuild.ShouldBeTrue();
        classification.DiagnosticId.ShouldBe("HFC1010");
        classification.Message.ShouldContain("What:");
        classification.Message.ShouldContain("Expected:");
        classification.Message.ShouldContain("Got:");
        classification.Message.ShouldContain("Fix:");
        classification.Message.ShouldContain("DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1010");
    }
}

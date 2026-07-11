using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class TypographyConstantsTests {
    [Fact]
    public void TypographyMappingVersion_KernelMetadata_IsPinned()
        => ContractsMetadata.TypographyMappingVersion.ShouldBe("3.1.0");
}

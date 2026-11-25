using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

public sealed class ProjectionTemplateVersionParityTests
{
    [Fact]
    public void CompilerAndRuntimeSupportTheSameTemplateContract()
    {
        CustomizationContractVersion compilerVersion = new(
            ProjectionTemplateMarkerParser.CurrentContractMajor,
            ProjectionTemplateMarkerParser.CurrentContractMinor,
            ProjectionTemplateMarkerParser.CurrentContractBuild);

        compilerVersion.ShouldBe(CustomizationContractVersion.Unpack(ProjectionTemplateContractVersion.Current));
        ProjectionTemplateMarkerParser.CurrentContractPacked.ShouldBe(compilerVersion.Packed);
    }
}

using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-2 RF5 + review follow-up — regression coverage for HFC1008 across both supported
/// call sites: command [Flags] properties and projection [Flags] enums carrying
/// <c>[ProjectionBadge]</c> annotations.
/// </summary>
public class Hfc1008DiagnosticTests {
    private const string CommandFlagsSource = @"
namespace TestDomain
{
    using System;
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Flags]
    public enum OrderTraits
    {
        None = 0,
        Rush = 1,
        Priority = 2,
    }

    [Command]
    public partial class SubmitOrderCommand
    {
        public OrderTraits Traits { get; set; }
    }
}
";

    private const string ProjectionFlagsWithBadgeSource = @"
namespace TestDomain
{
    using System;
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Flags]
    public enum OrderTraits
    {
        None = 0,
        [ProjectionBadge(BadgeSlot.Warning)] Rush = 1,
        Priority = 2,
    }

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public OrderTraits Traits { get; set; }
    }
}
";

    private const string ProjectionFlagsWithoutBadgeSource = @"
namespace TestDomain
{
    using System;
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Flags]
    public enum OrderTraits
    {
        None = 0,
        Rush = 1,
        Priority = 2,
    }

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public OrderTraits Traits { get; set; }
    }
}
";

    [Fact]
    public void FiresForCommandFlagsEnumProperty() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(CommandFlagsSource);

        Diagnostic diagnostic = diagnostics.Single(d => d.Id == "HFC1008");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().ShouldContain("SubmitOrderCommand");
        diagnostic.GetMessage().ShouldContain("Traits");
        diagnostic.GetMessage().ShouldContain("FcFieldPlaceholder");
    }

    [Fact]
    public void FiresForProjectionFlagsEnumWithProjectionBadgeAnnotations() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionFlagsWithBadgeSource);

        Diagnostic diagnostic = diagnostics.Single(d => d.Id == "HFC1008");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().ShouldContain("OrderTraits");
        diagnostic.GetMessage().ShouldContain("[ProjectionBadge]");
        diagnostic.GetMessage().ShouldContain("ignored");
    }

    [Fact]
    public void DoesNotFireForProjectionFlagsEnumWithoutProjectionBadgeAnnotations() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionFlagsWithoutBadgeSource);

        diagnostics.Any(d => d.Id == "HFC1008").ShouldBeFalse();
    }

    [Fact]
    public void AnalyzerReleasesPublishesWidenedHfc1008Title() {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        string analyzerReleasesPath = Path.Combine(repositoryRoot, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md");

        File.ReadAllText(analyzerReleasesPath)
            .ShouldContain("HFC1008 | HexalithFrontComposer | Warning | [Flags] enum in a single-value UI context");
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}

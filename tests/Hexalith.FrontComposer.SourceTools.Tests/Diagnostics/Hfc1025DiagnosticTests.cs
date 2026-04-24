using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-2 T3 / D6 / AC3 — generator-level assertions for the HFC1025 partial-coverage
/// diagnostic. Validates that the diagnostic fires only for partial enum coverage, names the
/// offending property + unannotated members, and stays silent for zero / full coverage.
/// </summary>
public class Hfc1025DiagnosticTests {
    private const string FullyAnnotatedSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    public enum OrderStatus
    {
        [ProjectionBadge(BadgeSlot.Neutral)] Pending,
        [ProjectionBadge(BadgeSlot.Success)] Approved,
        [ProjectionBadge(BadgeSlot.Danger)] Rejected,
    }

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public OrderStatus Status { get; set; }
    }
}
";

    private const string PartiallyAnnotatedSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    public enum OrderStatus
    {
        [ProjectionBadge(BadgeSlot.Warning)] Pending,
        Approved,
        Rejected,
    }

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public OrderStatus Status { get; set; }
    }
}
";

    private const string UnannotatedSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    public enum OrderStatus
    {
        Pending,
        Approved,
        Rejected,
    }

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public OrderStatus Status { get; set; }
    }
}
";

    private const string FlagsEnumWithPartialBadgesSource = @"
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

    [Fact]
    public void FiresOncePerProjection_ForPartialCoverage() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(PartiallyAnnotatedSource);

        Diagnostic[] hfc1025 = [.. diagnostics.Where(d => d.Id == "HFC1025")];
        hfc1025.Length.ShouldBe(1);
        hfc1025[0].Severity.ShouldBe(DiagnosticSeverity.Info);
        string message = hfc1025[0].GetMessage();
        message.ShouldContain("Status");
        message.ShouldContain("OrderProjection");
        message.ShouldContain("1 of 3");
        message.ShouldContain("Approved");
        message.ShouldContain("Rejected");
    }

    [Fact]
    public void DoesNotFire_WhenAllMembersAnnotated() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(FullyAnnotatedSource);

        diagnostics.Any(d => d.Id == "HFC1025").ShouldBeFalse();
    }

    [Fact]
    public void DoesNotFire_WhenNoMembersAnnotated() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(UnannotatedSource);

        diagnostics.Any(d => d.Id == "HFC1025").ShouldBeFalse();
    }

    [Fact]
    public void DoesNotFire_ForFlagsEnum_EvenWithPartialAnnotations() {
        // Story 4-2 D10 — [Flags] enums short-circuit to empty BadgeMappings in Parse; the
        // column takes the Story 1-5 text path, and HFC1025 (partial coverage) never fires
        // because the Transform sees zero mappings.
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(FlagsEnumWithPartialBadgesSource);

        diagnostics.Any(d => d.Id == "HFC1025").ShouldBeFalse();
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

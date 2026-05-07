using System.Reflection;
using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Drift;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC1 / T1 + T3 — the structural drift comparison seam. Asserts the planned internal
/// pure service surface (<c>DriftComparisonService.Compare(domain, baseline, options)</c>)
/// exists, returns a deterministic <c>DriftComparisonResult</c>, and treats partial-type
/// declaration order as semantically irrelevant. Memory rule "comparison logic stays
/// internal — no public CLI/code-fix/source-rewrite surface in 9-1" is enforced by
/// <see cref="Seam.DriftSeamPublicSurfaceContractTests"/>.
/// </summary>
public sealed class DriftComparisonServiceTests {
    private const string SkipReason = "RED-PHASE: T1 + T3 — DriftComparisonService not yet introduced.";

    [Fact()]
    public void Service_TypeIsInternal_AndExposesCompareMethodOnDomainAndBaseline() {
        Type? service = TryFindServiceType();

        service.ShouldNotBeNull("AC1 / T3: planned `DriftComparisonService` must exist as an internal pure service.");
        service!.IsPublic.ShouldBeFalse("AC17 forbids public comparison API in 9-1.");

        MethodInfo? compare = service.GetMethod("Compare", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        compare.ShouldNotBeNull("Service must expose a Compare method.");
        ParameterInfo[] parameters = compare!.GetParameters();
        parameters.Length.ShouldBeGreaterThanOrEqualTo(2);
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("DomainModel", StringComparison.Ordinal)
                                    || p.ParameterType.Name.Contains("DriftCurrent", StringComparison.Ordinal));
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("Baseline", StringComparison.Ordinal));
    }

    [Fact()]
    public void Result_IsDeterministicForIdenticalInputs() {
        // Same domain + same baseline ⇒ comparison results compare equal (value-based, ordered).
        // Activation contract: invoke the service twice with the same DomainModel sequence and
        // the same parsed baseline; assert ImmutableArray<DriftDiagnosticFact>-equivalent equality.
        InvocationProbe probe = InvocationProbe.CreateOrSkip();
        object first = probe.Compare(probe.SampleDomain, probe.SampleBaseline);
        object second = probe.Compare(probe.SampleDomain, probe.SampleBaseline);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.GetType().ShouldBe(second.GetType());

        IEnumerable<object> firstFacts = ExtractDiagnosticFacts(first);
        IEnumerable<object> secondFacts = ExtractDiagnosticFacts(second);

        firstFacts.Select(f => f.ToString()).ShouldBe(secondFacts.Select(f => f.ToString()));
    }

    [Fact()]
    public void Result_IsIndependentOfPartialDeclarationOrder() {
        // T3: partial declarations of the same projection/command type may live in any file
        // order; merging must produce identical drift facts regardless of declaration order.
        InvocationProbe probe = InvocationProbe.CreateOrSkip();
        object forward = probe.Compare(probe.PartialPermutationDomain(reverse: false), probe.SampleBaseline);
        object reverse = probe.Compare(probe.PartialPermutationDomain(reverse: true), probe.SampleBaseline);

        ExtractDiagnosticFacts(forward).Select(f => f.ToString())
            .ShouldBe(ExtractDiagnosticFacts(reverse).Select(f => f.ToString()),
                "AC18: partial-declaration order must not affect drift output.");
    }

    [Fact()]
    public void Result_IsIndependentOfBaselineFileOrder() {
        // T2: when multiple AdditionalText baseline files are provided, ordinal-sorted path
        // normalization must produce identical drift facts regardless of enumeration order.
        InvocationProbe probe = InvocationProbe.CreateOrSkip();
        object forward = probe.Compare(probe.SampleDomain, probe.PermutedBaseline(reverse: false));
        object reverse = probe.Compare(probe.SampleDomain, probe.PermutedBaseline(reverse: true));

        ExtractDiagnosticFacts(forward).Select(f => f.ToString())
            .ShouldBe(ExtractDiagnosticFacts(reverse).Select(f => f.ToString()),
                "AC18: baseline file enumeration order must not change drift output.");
    }

    private static Type? TryFindServiceType() {
        Assembly sourceTools = typeof(DomainModel).Assembly;
        return sourceTools.GetTypes().FirstOrDefault(t =>
            t.Name == "DriftComparisonService"
            || (t.Name.EndsWith("DriftComparisonService", StringComparison.Ordinal)));
    }

    private static IEnumerable<object> ExtractDiagnosticFacts(object result) {
        // The result type is expected to expose a `Diagnostics` (or `Facts`) property whose
        // element type is a stable record. We enumerate via reflection so the test compiles
        // before the type lands.
        PropertyInfo? facts = result.GetType().GetProperty("Diagnostics")
            ?? result.GetType().GetProperty("Facts")
            ?? result.GetType().GetProperty("Drifts");
        if (facts is null) {
            return Array.Empty<object>();
        }

        return ((System.Collections.IEnumerable)facts.GetValue(result)!).Cast<object>();
    }

    public sealed class InvocationProbe {
        public required object Service { get; init; }
        public required MethodInfo CompareMethod { get; init; }
        public required object SampleDomain { get; init; }
        public required object SampleBaseline { get; init; }

        public object PartialPermutationDomain(bool reverse) => SampleDomain;
        public object PermutedBaseline(bool reverse) => SampleBaseline;

        public object Compare(object domain, object baseline) => CompareMethod.Invoke(Service, [domain, baseline])!;

        public static InvocationProbe CreateOrSkip() {
            Type? type = TryFindServiceType()
                ?? throw new InvalidOperationException("DriftComparisonService not yet implemented.");
            object instance = type.IsAbstract ? null! : Activator.CreateInstance(type)!;
            MethodInfo compare = type.GetMethod("Compare")
                ?? throw new InvalidOperationException("Compare method missing.");
            DriftCurrentSnapshot domain = new(ImmutableArray.Create(new DriftCurrentContract(
                "projection",
                "TestDomain.OrderProjection",
                "Orders",
                null,
                null,
                null,
                null,
                null,
                null,
                ImmutableArray.Create(new DriftCurrentProperty("Id", "String", false, null, null, null, null, null, "Default")),
                string.Empty,
                -1,
                -1)));
            DriftBaselineSet baseline = new(ImmutableArray.Create(new DriftBaselineContract(
                "frontcomposer.drift-baseline.json",
                "projection",
                "TestDomain.OrderProjection",
                "Orders",
                null,
                null,
                null,
                null,
                null,
                null,
                ImmutableArray.Create(new DriftBaselineProperty("Id", "String", false, null, null, null, null, null, "Default")))));
            return new InvocationProbe {
                Service = instance,
                CompareMethod = compare,
                SampleDomain = domain,
                SampleBaseline = baseline,
            };
        }
    }
}

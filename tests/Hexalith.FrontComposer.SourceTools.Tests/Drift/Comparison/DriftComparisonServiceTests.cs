using System.Reflection;
using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Drift;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC1 / T1 + T3 — the structural drift comparison seam. Asserts the planned internal
/// pure service surface (<c>DriftComparisonService.Compare(snapshot, baseline)</c>)
/// exists, returns a deterministic <c>DriftComparisonResult</c>, and treats partial-type
/// declaration order and baseline file order as semantically irrelevant. Memory rule
/// "comparison logic stays internal — no public CLI/code-fix/source-rewrite surface in 9-1"
/// is enforced by <see cref="Seam.DriftSeamPublicSurfaceContractTests"/>.
/// </summary>
public sealed class DriftComparisonServiceTests {
    private const string SkipReason = "RED-PHASE: T1 + T3 — DriftComparisonService not yet introduced.";

    [Fact()]
    public void Service_TypeIsInternal_AndExposesCompareMethodOnDomainAndBaseline() {
        Type? service = TryFindServiceType();

        service.ShouldNotBeNull("AC1 / T3: planned `DriftComparisonService` must exist as an internal pure service.");
        service!.IsPublic.ShouldBeFalse("AC17 forbids public comparison API in 9-1.");

        // Story 9-1 review CB-1: pin the lookup to the canonical 2-arg overload by exact
        // parameter types. The earlier `OrderBy(parameters.Length).FirstOrDefault()` was
        // brittle: it would silently bind to a different (or null) method if a future overload
        // shifted positions, instead of asserting the contract. Production exposes:
        //   internal static DriftComparisonResult Compare(DriftCurrentSnapshot, DriftBaselineSet)
        // and a 4-arg variant with options; here we pin to the 2-arg one explicitly.
        MethodInfo? compare = service.GetMethod(
            "Compare",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
            binder: null,
            types: [typeof(DriftCurrentSnapshot), typeof(DriftBaselineSet)],
            modifiers: null);
        compare.ShouldNotBeNull("Service must expose Compare(DriftCurrentSnapshot, DriftBaselineSet).");
        ParameterInfo[] parameters = compare!.GetParameters();
        parameters[0].ParameterType.ShouldBe(typeof(DriftCurrentSnapshot));
        parameters[1].ParameterType.ShouldBe(typeof(DriftBaselineSet));
    }

    [Fact()]
    public void Result_IsDeterministicForIdenticalInputs() {
        // Same domain + same baseline ⇒ comparison results compare equal (value-based, ordered).
        InvocationProbe probe = InvocationProbe.CreateOrSkip();
        DriftComparisonResult first = probe.Compare(probe.SampleDomain, probe.SampleBaseline);
        DriftComparisonResult second = probe.Compare(probe.SampleDomain, probe.SampleBaseline);

        // Story 9-1 review CB-38: compare facts structurally (Id + Severity + Message) rather
        // than via `f.ToString()` — DriftDiagnosticFact does not override ToString, so the prior
        // assertion compared `Type.FullName` for every element and passed trivially.
        first.Diagnostics.Length.ShouldBe(second.Diagnostics.Length);
        for (int i = 0; i < first.Diagnostics.Length; i++) {
            DriftDiagnosticFact a = first.Diagnostics[i];
            DriftDiagnosticFact b = second.Diagnostics[i];
            a.Id.ShouldBe(b.Id, $"AC18 determinism — fact[{i}].Id differs.");
            a.Severity.ShouldBe(b.Severity, $"AC18 determinism — fact[{i}].Severity differs.");
            a.Message.ShouldBe(b.Message, $"AC18 determinism — fact[{i}].Message differs.");
        }
    }

    [Fact()]
    public void Result_IsIndependentOfPartialDeclarationOrder() {
        // T3: partial declarations of the same projection/command type may live in any file
        // order; merging must produce identical drift facts regardless of contract enumeration
        // order in the snapshot.
        // Story 9-1 review CB-2: actually permute. The earlier helpers returned the same
        // instance regardless of `reverse:`, so the test was a tautology.
        InvocationProbe probe = InvocationProbe.CreateOrSkip(twoContracts: true);
        DriftComparisonResult forward = probe.Compare(probe.SampleDomain, probe.SampleBaseline);
        DriftComparisonResult reverse = probe.Compare(probe.ReversedDomain, probe.SampleBaseline);

        AssertSameFacts(forward, reverse, "AC18 partial-declaration order must not affect drift output.");
    }

    [Fact()]
    public void Result_IsIndependentOfBaselineFileOrder() {
        // T2: when multiple AdditionalText baseline files are provided, ordinal-sorted path
        // normalization must produce identical drift facts regardless of enumeration order.
        // Story 9-1 review CB-2: actually permute the baseline contract array.
        InvocationProbe probe = InvocationProbe.CreateOrSkip(twoContracts: true);
        DriftComparisonResult forward = probe.Compare(probe.SampleDomain, probe.SampleBaseline);
        DriftComparisonResult reverse = probe.Compare(probe.SampleDomain, probe.ReversedBaseline);

        AssertSameFacts(forward, reverse, "AC18 baseline file enumeration order must not change drift output.");
    }

    private static void AssertSameFacts(DriftComparisonResult a, DriftComparisonResult b, string reason) {
        // Sort by Id+Message so we compare set equality. The comparison service guarantees
        // ordering, but the goal here is to assert that order independence holds at the *fact*
        // level — the production sort is exercised by DriftDiagnosticOrderingAndTruncationTests.
        IOrderedEnumerable<string> aKeys = a.Diagnostics.Select(f => f.Id + "|" + f.Message).OrderBy(s => s, StringComparer.Ordinal);
        IOrderedEnumerable<string> bKeys = b.Diagnostics.Select(f => f.Id + "|" + f.Message).OrderBy(s => s, StringComparer.Ordinal);
        aKeys.ShouldBe(bKeys, reason);
    }

    private static Type? TryFindServiceType() {
        Assembly sourceTools = typeof(DomainModel).Assembly;
        return sourceTools.GetTypes().FirstOrDefault(t =>
            t.Name == "DriftComparisonService"
            || (t.Name.EndsWith("DriftComparisonService", StringComparison.Ordinal)));
    }

    internal sealed class InvocationProbe {
        public required MethodInfo CompareMethod { get; init; }
        public required DriftCurrentSnapshot SampleDomain { get; init; }
        public required DriftBaselineSet SampleBaseline { get; init; }
        public required DriftCurrentSnapshot ReversedDomain { get; init; }
        public required DriftBaselineSet ReversedBaseline { get; init; }

        public DriftComparisonResult Compare(DriftCurrentSnapshot domain, DriftBaselineSet baseline)
            => (DriftComparisonResult)CompareMethod.Invoke(null, [domain, baseline])!;

        internal static InvocationProbe CreateOrSkip(bool twoContracts = false) {
            Type? type = TryFindServiceType()
                ?? throw new InvalidOperationException("DriftComparisonService not yet implemented.");
            // Story 9-1 review CB-1: pin to the exact 2-arg signature.
            MethodInfo compare = type.GetMethod(
                "Compare",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                binder: null,
                types: [typeof(DriftCurrentSnapshot), typeof(DriftBaselineSet)],
                modifiers: null)
                ?? throw new InvalidOperationException("Compare(DriftCurrentSnapshot, DriftBaselineSet) missing.");

            DriftCurrentContract orderContract = MakeCurrentContract("TestDomain.OrderProjection", "Orders", "Id");
            DriftBaselineContract orderBaseline = MakeBaselineContract("TestDomain.OrderProjection", "Orders", "Id");

            ImmutableArray<DriftCurrentContract> currentContracts;
            ImmutableArray<DriftCurrentContract> currentReversed;
            ImmutableArray<DriftBaselineContract> baselineContracts;
            ImmutableArray<DriftBaselineContract> baselineReversed;

            if (twoContracts) {
                // Story 9-1 review CB-2: real permutation requires ≥2 contracts (or members).
                // Use two distinct projections; reversing the array proves order independence.
                DriftCurrentContract shipContract = MakeCurrentContract("TestDomain.ShipmentProjection", "Shipping", "Priority");
                DriftBaselineContract shipBaseline = MakeBaselineContract("TestDomain.ShipmentProjection", "Shipping", "Priority");

                currentContracts = ImmutableArray.Create(orderContract, shipContract);
                currentReversed = ImmutableArray.Create(shipContract, orderContract);
                baselineContracts = ImmutableArray.Create(orderBaseline, shipBaseline);
                baselineReversed = ImmutableArray.Create(shipBaseline, orderBaseline);
            } else {
                currentContracts = ImmutableArray.Create(orderContract);
                currentReversed = currentContracts;
                baselineContracts = ImmutableArray.Create(orderBaseline);
                baselineReversed = baselineContracts;
            }

            return new InvocationProbe {
                CompareMethod = compare,
                SampleDomain = new DriftCurrentSnapshot(currentContracts),
                SampleBaseline = new DriftBaselineSet(baselineContracts),
                ReversedDomain = new DriftCurrentSnapshot(currentReversed),
                ReversedBaseline = new DriftBaselineSet(baselineReversed),
            };
        }

        private static DriftCurrentContract MakeCurrentContract(string type, string boundedContext, string propertyName)
            => new(
                "projection",
                type,
                boundedContext,
                displayName: null,
                displayGroupName: null,
                role: null,
                icon: null,
                destructive: null,
                requiresPolicy: null,
                emptyStateCtaCommandTypeName: null,
                ImmutableArray.Create(new DriftCurrentProperty(
                    propertyName,
                    "String",
                    false,
                    derivable: null,
                    displayName: null,
                    description: null,
                    columnPriority: null,
                    fieldGroup: null,
                    displayFormat: "Default",
                    relativeTimeWindowDays: null,
                    badgeSignature: null)),
                string.Empty,
                -1,
                -1);

        private static DriftBaselineContract MakeBaselineContract(string type, string boundedContext, string propertyName)
            => new(
                "frontcomposer.drift-baseline.json",
                "projection",
                type,
                boundedContext,
                displayName: null,
                displayGroupName: null,
                role: null,
                icon: null,
                destructive: null,
                requiresPolicy: null,
                emptyStateCtaCommandTypeName: null,
                ImmutableArray.Create(new DriftBaselineProperty(
                    propertyName,
                    "String",
                    false,
                    derivable: null,
                    displayName: null,
                    description: null,
                    columnPriority: null,
                    fieldGroup: null,
                    displayFormat: "Default",
                    relativeTimeWindowDays: null,
                    badgeSignature: null)));
    }
}

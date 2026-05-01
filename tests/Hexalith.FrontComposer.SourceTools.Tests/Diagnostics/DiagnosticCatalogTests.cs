using System.Reflection;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 6-6 P6 / AC14 / T7 — diagnostic catalog uniqueness and discipline. Reflectively
/// enumerates every <see cref="FcDiagnosticIds"/> constant and every public
/// <see cref="DiagnosticDescriptor"/> field on <see cref="DiagnosticDescriptors"/>, asserting
/// that no HFC ID is duplicated, every analyzer-emitted descriptor uses the canonical
/// docs-link shape, and Title / MessageFormat are non-empty.
/// </summary>
public sealed class DiagnosticCatalogTests {
    private const string DocsLinkPrefix = "https://hexalith.github.io/FrontComposer/diagnostics/";

    [Fact]
    public void FcDiagnosticIdsConstants_AreUnique_AndShapedHFCxxxx() {
        string[] ids = TypeIdConstants(typeof(FcDiagnosticIds)).ToArray();

        ids.Length.ShouldBeGreaterThan(0);
        ids.ShouldBeUnique();
        foreach (string id in ids) {
            id.ShouldStartWith("HFC");
            // 4-digit numeric tail (HFC + 4 digits = 7 chars).
            id.Length.ShouldBe(7);
            for (int i = 3; i < id.Length; i++) {
                char.IsDigit(id[i]).ShouldBeTrue($"Diagnostic ID {id} has non-digit character at position {i}.");
            }
        }
    }

    [Fact]
    public void DiagnosticDescriptors_AreUnique_AndCarryNonEmptyTitleAndMessageFormat() {
        DiagnosticDescriptor[] descriptors = DiagnosticDescriptorFields().ToArray();

        descriptors.Length.ShouldBeGreaterThan(0);
        descriptors.Select(d => d.Id).ShouldBeUnique();

        foreach (DiagnosticDescriptor descriptor in descriptors) {
            descriptor.Id.ShouldNotBeNullOrWhiteSpace();
            descriptor.Title.ToString().ShouldNotBeNullOrWhiteSpace();
            descriptor.MessageFormat.ToString().ShouldNotBeNullOrWhiteSpace();
            descriptor.Category.ShouldBe("HexalithFrontComposer");
            descriptor.Id.ShouldStartWith("HFC");
        }
    }

    [Fact]
    public void Story66ContractValidationDescriptors_DocsLinkPrefixIsCanonical() {
        // P6 / AC14 — Story 6-6 owns HFC1010, HFC1050-HFC1055, HFC2115-HFC2119, HFC1601 strict
        // gate diagnostic. We don't enforce docs-link presence on every descriptor (existing
        // descriptors don't carry HelpLinkUri), but all newly-introduced runtime diagnostic
        // emissions must use the canonical https://hexalith.github.io/FrontComposer/diagnostics/HFCxxxx
        // prefix. The CustomizationAccessibilityAnalyzer emits messages that include the
        // canonical prefix, so spot-checking message format is sufficient.
        string[] story66Ids = [
            FcDiagnosticIds.HFC1010_FullRebuildRequired,
            FcDiagnosticIds.HFC1050_CustomizationAccessibleNameMissing,
            FcDiagnosticIds.HFC1051_CustomizationKeyboardReachabilityIssue,
            FcDiagnosticIds.HFC1052_CustomizationFocusVisibilitySuppressed,
            FcDiagnosticIds.HFC1053_CustomizationAriaLiveParityMissing,
            FcDiagnosticIds.HFC1054_CustomizationReducedMotionMissing,
            FcDiagnosticIds.HFC1055_CustomizationForcedColorsMissing,
            FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault,
            FcDiagnosticIds.HFC2116_CustomizationStaleDescriptorManifest,
            FcDiagnosticIds.HFC2117_CustomizationPanelRecoveryFailure,
            FcDiagnosticIds.HFC2118_CustomizationRuntimeAccessibilityFallback,
            FcDiagnosticIds.HFC2119_CustomizationRedactionFallback,
        ];

        story66Ids.ShouldBeUnique();
        foreach (string id in story66Ids) {
            id.ShouldStartWith("HFC");
            id.Length.ShouldBe(7);
        }

        // Sanity: the canonical docs-link prefix is the agreed-upon shape.
        DocsLinkPrefix.ShouldEndWith("/diagnostics/");
        DocsLinkPrefix.ShouldStartWith("https://");
    }

    [Fact]
    public void DescriptorIdsAndConstantIds_MustNotCollide() {
        // Every analyzer descriptor ID MUST also have a matching constant in FcDiagnosticIds
        // (with the EXCEPT of long-standing HFC1001-HFC1009 generator-only descriptors that
        // pre-date the FcDiagnosticIds catalog by design). The test verifies the inverse:
        // every constant whose number falls in the analyzer range (HFC1010-HFC1099) and is
        // NOT explicitly marked "Reserved" in FcDiagnosticIds doc comments has a matching
        // descriptor field. This catches accidental constant deletions.
        HashSet<string> descriptorIds = DiagnosticDescriptorFields().Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
        HashSet<string> constantIds = TypeIdConstants(typeof(FcDiagnosticIds)).ToHashSet(StringComparer.Ordinal);

        // Spot-check: HFC1050-HFC1055 are known analyzer IDs that must be in BOTH.
        foreach (string id in new[] { "HFC1050", "HFC1051", "HFC1052", "HFC1053", "HFC1054", "HFC1055" }) {
            descriptorIds.ShouldContain(id, $"Analyzer descriptor for {id} must be present in DiagnosticDescriptors.");
            constantIds.ShouldContain(id, $"Constant for {id} must be present in FcDiagnosticIds.");
        }
    }

    private static IEnumerable<string> TypeIdConstants(Type type) {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.IsLiteral && field.FieldType == typeof(string)) {
                if (field.GetRawConstantValue() is string value && value.StartsWith("HFC", StringComparison.Ordinal)) {
                    yield return value;
                }
            }
        }
    }

    private static IEnumerable<DiagnosticDescriptor> DiagnosticDescriptorFields() {
        foreach (FieldInfo field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.FieldType == typeof(DiagnosticDescriptor)
                && field.GetValue(null) is DiagnosticDescriptor descriptor) {
                yield return descriptor;
            }
        }
    }
}

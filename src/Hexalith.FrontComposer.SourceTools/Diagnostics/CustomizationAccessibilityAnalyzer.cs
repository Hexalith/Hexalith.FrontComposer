using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hexalith.FrontComposer.SourceTools.Diagnostics;

/// <summary>
/// Conservative analyzer for statically provable accessibility issues on customization components.
/// </summary>
/// <remarks>
/// <para>
/// <b>P8 — perf.</b> Source size is bounded per type (<see cref="MaxAnalyzedSourceBytes"/>) so
/// large partial classes do not cause analyzer OOM or quadratic IDE typing latency.
/// </para>
/// <para>
/// <b>P9 — false positives.</b> Single-line and block comments are stripped from the analyzed
/// source before substring matching so commented-out code does not produce spurious warnings.
/// String literal contents are intentionally preserved because the analyzer's detection pattern
/// is the call-site shape <c>builder.AddAttribute(_, "onclick", _)</c> — the attribute name lives
/// inside a string literal, and stripping it would defeat the analyzer.
/// </para>
/// <para>
/// <b>P16 — L3/L4 scope.</b> A compilation-start syntax walk collects component types referenced
/// from <c>AddProjectionTemplate&lt;&gt;</c>, <c>AddSlotOverride&lt;,&gt;</c>, and
/// <c>AddViewOverride&lt;,&gt;</c> registration calls so Level 3 slot and Level 4 view override
/// components receive the same six accessibility checks as Level 2 templates.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomizationAccessibilityAnalyzer : DiagnosticAnalyzer {
    private const string ProjectionTemplateAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionTemplateAttribute";
    private const int MaxAnalyzedSourceBytes = 256 * 1024;

    // P16 — registration-call method names that introduce a customization component as a
    // generic argument. Matched on simple name (the underlying generic-method call).
    private static readonly HashSet<string> RegistrationCallNames = new(StringComparer.Ordinal) {
        "AddProjectionTemplate",
        "AddSlotOverride",
        "AddViewOverride",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.CustomizationAccessibleNameMissing,
        DiagnosticDescriptors.CustomizationKeyboardReachabilityIssue,
        DiagnosticDescriptors.CustomizationFocusVisibilitySuppressed,
        DiagnosticDescriptors.CustomizationAriaLiveParityMissing,
        DiagnosticDescriptors.CustomizationReducedMotionMissing,
        DiagnosticDescriptors.CustomizationForcedColorsMissing);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext compilationContext) {
        // P16 — collect component types referenced from registration calls. The set is
        // mutated by SyntaxNodeAction passes (one per InvocationExpression) and read by
        // the SymbolAction pass below. ConcurrentDictionary keyed by SymbolEqualityComparer
        // prevents key conflation across nested generic instantiations.
        ConcurrentDictionary<INamedTypeSymbol, byte> referencedComponents =
            new(SymbolEqualityComparer.Default);

        compilationContext.RegisterSyntaxNodeAction(
            ctx => CollectRegistrationReferences(ctx, referencedComponents),
            SyntaxKind.InvocationExpression);

        compilationContext.RegisterSymbolAction(
            ctx => AnalyzeType(ctx, referencedComponents),
            SymbolKind.NamedType);
    }

    private static void CollectRegistrationReferences(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, byte> referencedComponents) {
        if (context.Node is not InvocationExpressionSyntax invocation) {
            return;
        }

        // Identify the simple method name without resolving the symbol when the name does
        // not match — this short-circuits the vast majority of invocations cheaply.
        string? methodName = ExtractSimpleName(invocation.Expression);
        if (methodName is null || !RegistrationCallNames.Contains(methodName)) {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method) {
            return;
        }

        // The component type is the LAST type argument: AddProjectionTemplate<TComponent>,
        // AddSlotOverride<TProjection, TField, TComponent>, AddViewOverride<TProjection, TComponent>.
        if (method.TypeArguments.Length == 0) {
            return;
        }

        if (method.TypeArguments[method.TypeArguments.Length - 1] is INamedTypeSymbol componentType) {
            referencedComponents.TryAdd(componentType, 0);
        }
    }

    private static string? ExtractSimpleName(ExpressionSyntax expression) => expression switch {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => null,
    };

    private static void AnalyzeType(
        SymbolAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, byte> referencedComponents) {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
        if (!IsCustomizationComponent(type, referencedComponents)) {
            return;
        }

        string? source = GetDeclaredSource(type, context.CancellationToken);
        if (source is null || source.Length == 0) {
            return;
        }

        Location location = type.Locations.Length > 0 && type.Locations[0] != Location.None
            ? type.Locations[0]
            : Location.None;
        bool hasClick = Contains(source, "\"onclick\"") || Contains(source, "'onclick'");
        bool hasAccessibleName = Contains(source, "\"aria-label\"")
            || Contains(source, "\"aria-labelledby\"")
            || Contains(source, "\"title\"")
            || Contains(source, ".AddContent(");

        if (hasClick && !hasAccessibleName) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationAccessibleNameMissing,
                location,
                what: "A customization component renders an interactive element without an accessible name.",
                expected: "Interactive override roots expose visible text, aria-label, aria-labelledby, or an equivalent static name.",
                got: "The statically inspectable render tree adds an onclick handler but no name-bearing attribute or content.",
                fix: "Add visible text or an aria-label/aria-labelledby attribute to the interactive element.",
                docsId: "HFC1050");
        }

        if (Contains(source, "\"tabindex\", -1") || Contains(source, "\"tabindex\", \"-1\"")) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationKeyboardReachabilityIssue,
                location,
                what: "A customization component removes the only obvious keyboard path from an interactive element.",
                expected: "Clickable custom override elements stay keyboard reachable.",
                got: "The statically inspectable render tree sets tabindex=-1.",
                fix: "Remove tabindex=-1 or provide a reachable keyboard control.",
                docsId: "HFC1051");
        }

        if ((Contains(source, "outline: none") || Contains(source, "box-shadow: none")) && !Contains(source, "focus-visible")) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationFocusVisibilitySuppressed,
                location,
                what: "A customization component suppresses visible focus styling.",
                expected: "Custom override CSS preserves or replaces visible keyboard focus.",
                got: "The statically inspectable source disables outline or box-shadow without a replacement focus-visible rule.",
                fix: "Restore the framework focus style or add an explicit :focus-visible replacement.",
                docsId: "HFC1052");
        }

        if ((Contains(source, "data-fc-lifecycle") || Contains(source, "data-fc-status")) && !Contains(source, "\"aria-live\"")) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationAriaLiveParityMissing,
                location,
                what: "A customization component replaces lifecycle/status UI without aria-live parity.",
                expected: "Lifecycle, loading, empty, and status override surfaces preserve the framework polite/assertive announcement category.",
                got: "The statically inspectable source identifies a lifecycle/status surface but no aria-live attribute.",
                fix: "Add the same aria-live category used by the generated framework surface.",
                docsId: "HFC1053");
        }

        if ((Contains(source, "transition:") || Contains(source, "@keyframes") || Contains(source, "animation:"))
            && !Contains(source, "prefers-reduced-motion")) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationReducedMotionMissing,
                location,
                what: "A customization component defines motion without a reduced-motion fallback.",
                expected: "Custom override animations and transitions include @media (prefers-reduced-motion: reduce).",
                got: "The statically inspectable source contains motion CSS without the reduced-motion media query.",
                fix: "Add a reduced-motion media query that disables or reduces the animation/transition.",
                docsId: "HFC1054");
        }

        if ((Contains(source, "color:") || Contains(source, "background:") || Contains(source, "border-color:") || Contains(source, "fill:"))
            && !Contains(source, "forced-colors")) {
            Report(
                context,
                DiagnosticDescriptors.CustomizationForcedColorsMissing,
                location,
                what: "A customization component defines custom color styling without forced-colors evidence.",
                expected: "Override-owned color, border, and fill CSS includes a forced-colors path using system color keywords.",
                got: "The statically inspectable source contains custom color CSS without @media (forced-colors: active).",
                fix: "Add a forced-colors media query or move the color pair to framework tokens verified elsewhere.",
                docsId: "HFC1055");
        }
    }

    private static bool IsCustomizationComponent(
        INamedTypeSymbol type,
        ConcurrentDictionary<INamedTypeSymbol, byte> referencedComponents) {
        // Level 2 — class-level attribute.
        foreach (AttributeData attribute in type.GetAttributes()) {
            if (attribute.AttributeClass?.ToDisplayString() == ProjectionTemplateAttributeName) {
                return true;
            }
        }

        // Levels 3 / 4 — type referenced from an Add{Slot,View,ProjectionTemplate}Override call.
        // P16 — extends analyzer reach without requiring a class-level attribute on slot/view
        // override components.
        return referencedComponents.ContainsKey(type);
    }

    private static string? GetDeclaredSource(INamedTypeSymbol type, CancellationToken cancellationToken) {
        // P8 — bound the source-text scan size. Quadratic scans over partial classes that
        // sprawl across many files are the documented Roslyn analyzer perf trap.
        StringBuilder builder = new(capacity: 4 * 1024);
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences) {
            string fragment = reference.GetSyntax(cancellationToken).ToFullString();
            if (builder.Length + fragment.Length > MaxAnalyzedSourceBytes) {
                // Truncate at the cap; subsequent fragments are dropped silently. Adopters
                // with classes this large likely have bigger problems than partial coverage.
                int remaining = MaxAnalyzedSourceBytes - builder.Length;
                if (remaining > 0) {
                    _ = builder.Append(fragment, 0, remaining);
                }

                break;
            }

            _ = builder.Append(fragment).Append('\n');
        }

        // P9 — strip C# comments before the substring scans so commented-out code does not
        // produce false positives. String literals are preserved intentionally — the analyzer
        // matches call-site shape (`builder.AddAttribute(_, "onclick", _)`) where the attribute
        // name lives inside a string literal. CSS rules in companion .razor.css files are not
        // part of DeclaringSyntaxReferences and are unaffected.
        return StripComments(builder.ToString());
    }

    private static string StripComments(string source) {
        if (source.Length == 0) {
            return source;
        }

        StringBuilder result = new(source.Length);
        int i = 0;
        while (i < source.Length) {
            char c = source[i];

            // Single-line comment.
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/') {
                int end = source.IndexOf('\n', i + 2);
                if (end < 0) {
                    break;
                }

                _ = result.Append('\n');
                i = end + 1;
                continue;
            }

            // Block comment.
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*') {
                int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) {
                    break;
                }

                _ = result.Append(' ');
                i = end + 2;
                continue;
            }

            // String literal — copy through unchanged so analyzer detection on call-site
            // string-literal arguments still works.
            if (c == '"') {
                _ = result.Append('"');
                int j = i + 1;
                while (j < source.Length) {
                    char k = source[j];
                    _ = result.Append(k);
                    if (k == '\\' && j + 1 < source.Length) {
                        j++;
                        if (j < source.Length) {
                            _ = result.Append(source[j]);
                        }

                        j++;
                        continue;
                    }

                    if (k == '"') {
                        break;
                    }

                    if (k == '\n') {
                        break;
                    }

                    j++;
                }

                i = j < source.Length ? j + 1 : j;
                continue;
            }

            // Verbatim string @"..." — copy through unchanged.
            if (c == '@' && i + 1 < source.Length && source[i + 1] == '"') {
                _ = result.Append('@').Append('"');
                int j = i + 2;
                while (j < source.Length) {
                    char k = source[j];
                    _ = result.Append(k);
                    if (k == '"') {
                        if (j + 1 < source.Length && source[j + 1] == '"') {
                            _ = result.Append('"');
                            j += 2;
                            continue;
                        }

                        break;
                    }

                    j++;
                }

                i = j < source.Length ? j + 1 : j;
                continue;
            }

            _ = result.Append(c);
            i++;
        }

        return result.ToString();
    }

    private static bool Contains(string source, string value)
        => source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        string what,
        string expected,
        string got,
        string fix,
        string docsId) {
        string message =
            $"What: {what}\n"
            + $"Expected: {expected}\n"
            + $"Got: {got}\n"
            + $"Fix: {fix}\n"
            + "Fallback: The generated framework path remains available when the override is not selected.\n"
            + $"DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/{docsId}";

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            location,
            message));
    }
}

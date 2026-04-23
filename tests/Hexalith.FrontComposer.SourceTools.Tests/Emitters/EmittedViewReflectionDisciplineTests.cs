using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-1 T5.6 (round 4 honesty rename per Murat — was <c>GeneratorAotSmokeTests</c>) —
/// asserts the source generator's emitted views contain none of the AOT/trim-hostile
/// reflection patterns that D8 worried about (generic-state reflection, late-binding
/// `Activator.CreateInstance(Type)` without `[DynamicallyAccessedMembers]`, etc.).
/// </summary>
/// <remarks>
/// <para><strong>Scope honesty:</strong> this is NOT a real AOT certification gate.
/// MSBuild <c>PublishAot=true</c> + <c>TrimMode=full</c> on a stand-alone
/// <see cref="CSharpCompilation"/> are just metadata flags; the IL Linker
/// (<c>Microsoft.NET.ILLink.Tasks</c>) and AOT compiler are entirely separate
/// toolchains that a Roslyn compilation does not invoke. End-to-end AOT
/// certification ships in Epic 11's <c>dotnet publish</c> surface against an
/// adopter app.</para>
/// <para><strong>What this test DOES verify:</strong> the generator's output
/// discipline — no emitted view contains reflection patterns known to break
/// trim/AOT (the regression class D8 actually worried about). A future false
/// negative would justify wiring <c>Microsoft.NET.ILLink.Tasks</c> analyzers
/// into the compilation's <c>AnalyzerReferences</c> and switching to trim-warning
/// based assertions; not worth the Roslyn-version coupling cost for v1.</para>
/// </remarks>
public sealed class EmittedViewReflectionDisciplineTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static readonly EquatableArray<string> _noWhenStates =
        new(ImmutableArray<string>.Empty);

    /// <summary>
    /// Forbidden pattern → human-readable rationale. The test fails LOUDLY with the
    /// rationale so the offending PR knows which AOT/trim regression class is
    /// implicated.
    /// </summary>
    private static readonly (string ForbiddenPattern, string Rationale)[] _aotHostilePatterns = {
        ("MakeGenericMethod",
            "Runtime generic method specialization breaks AOT and trim. Use compile-time generics in emitted output."),
        ("MakeGenericType",
            "Runtime generic type specialization breaks AOT and trim. Generated views must close generics at emit time."),
        ("Activator.CreateInstance",
            "Late-bound `Activator.CreateInstance(Type)` requires `[DynamicallyAccessedMembers]` annotations the emitter does not produce. Use `new T()` with the static type known at emit time."),
        (".GetTypes()",
            "`Assembly.GetTypes()` requires the trimmer to preserve every type in the assembly. Emit explicit `typeof(T)` constants instead."),
        (".GetMembers(",
            "`Type.GetMembers(...)` requires `[DynamicallyAccessedMembers]` annotations the emitter does not produce. Use compile-time bound members."),
        (".GetMethod(",
            "`Type.GetMethod(name)` requires `[DynamicallyAccessedMembers(All)]` for trim-safe behavior. Emit direct method references instead."),
        (".GetProperty(",
            "`Type.GetProperty(name)` is reflection over generic state — exactly the pattern D8 forbade. Use compile-time `Expression<Func<T, TProp>>` accessors."),
    };

    [Theory]
    [InlineData(ProjectionRenderStrategy.Default)]
    [InlineData(ProjectionRenderStrategy.ActionQueue)]
    [InlineData(ProjectionRenderStrategy.StatusOverview)]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Timeline)]
    [InlineData(ProjectionRenderStrategy.Dashboard)]
    public void EmittedViewsHaveNoAotHostilePatterns(ProjectionRenderStrategy strategy) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        RazorModel model = BuildModel(strategy);
        string emitted = RazorEmitter.Emit(model);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(emitted, cancellationToken: ct);
        SyntaxNode root = tree.GetRoot(ct);

        // First, ensure the emitted code parses without diagnostics — a parse failure
        // would mask the AOT scan, since unparsed text could hide the forbidden tokens.
        IEnumerable<Diagnostic> parseDiagnostics = tree
            .GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error);
        parseDiagnostics.ShouldBeEmpty(
            $"Strategy {strategy} emitted code that does not parse as valid C#; AOT scan cannot proceed.");

        List<string> findings = new();
        string sourceText = root.ToFullString();
        foreach ((string pattern, string rationale) in _aotHostilePatterns) {
            int index = sourceText.IndexOf(pattern, StringComparison.Ordinal);
            if (index < 0) {
                continue;
            }

            // Suppress false positives — `state.Items.GetType()` style scans aren't applicable
            // because the emitter does not produce them, but if a future ColumnEmitter does,
            // we want the scan to flag them. Currently no exclusions are required.
            findings.Add(
                $"Strategy {strategy}: forbidden AOT-hostile pattern `{pattern}` found at offset {index}. {rationale}");
        }

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void EmittedViewBodyHasNoReflectionInvocationExpressions() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        // Companion AST walk: enumerate every InvocationExpression in the Default
        // emitted view and assert none of them target System.Type / System.Reflection
        // surfaces. Catches refactors that introduce `typeof(T).GetProperty(...)`.
        RazorModel model = BuildModel(ProjectionRenderStrategy.Default);
        string emitted = RazorEmitter.Emit(model);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(emitted, cancellationToken: ct);
        SyntaxNode root = tree.GetRoot(ct);

        InvocationExpressionSyntax[] reflectiveCalls = root
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsReflectiveInvocation)
            .ToArray();

        reflectiveCalls.ShouldBeEmpty(
            "Emitted view contains reflective invocations — see D8. Replace with compile-time-bound member access.");
    }

    private static bool IsReflectiveInvocation(InvocationExpressionSyntax invocation) {
        if (invocation.Expression is not MemberAccessExpressionSyntax member) {
            return false;
        }

        string memberName = member.Name.Identifier.ValueText;
        return memberName is
            "GetMethod"
            or "GetMethods"
            or "GetProperty"
            or "GetProperties"
            or "GetField"
            or "GetFields"
            or "GetMember"
            or "GetMembers"
            or "GetTypes"
            or "MakeGenericType"
            or "MakeGenericMethod";
    }

    private static RazorModel BuildModel(ProjectionRenderStrategy strategy) =>
        new RazorModel(
            typeName: "OrderProjection",
            @namespace: "TestDomain",
            boundedContext: "Orders",
            columns: new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Id", "Id", TypeCategory.Text, null, false, _emptyBadges),
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false,
                    new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(
                        new BadgeMappingEntry("Pending", "Warning")))),
                new ColumnModel("Count", "Count", TypeCategory.Numeric, "N0", false, _emptyBadges),
                new ColumnModel("CreatedAt", "Created At", TypeCategory.DateTime, "d", false, _emptyBadges))),
            strategy: strategy,
            whenStates: _noWhenStates);
}

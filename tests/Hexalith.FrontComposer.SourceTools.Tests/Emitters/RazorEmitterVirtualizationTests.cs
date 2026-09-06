using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-4 T5.1 — verifies the generator emits the virtualization attributes
/// (<c>Virtualize</c> / <c>DisplayMode</c> / <c>ItemSize</c> / <c>OverscanCount</c> /
/// <c>ItemKey</c>) and the density-driven <c>SetKey</c> on every grid-rendering strategy.
/// Also pins <c>_itemKeyAccessor</c> resolution precedence per D13 revised:
/// <c>AggregateId</c> &gt; <c>Id</c> &gt; <c>Key</c> &gt; <c>(object)x</c> fallback.
/// </summary>
public sealed class RazorEmitterVirtualizationTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static ColumnModel Col(string name, string? header = null, TypeCategory cat = TypeCategory.Text)
        => new(name, header ?? name, cat, null, false, _emptyBadges);

    private static ColumnModel BadgeCol()
        => new(
            "Status",
            "Status",
            TypeCategory.Enum,
            null,
            false,
            new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(
                new BadgeMappingEntry("Ready", "Success"),
                new BadgeMappingEntry("NeedsReview", "Warning"))),
            new EquatableArray<string>(ImmutableArray.Create("Ready", "NeedsReview")));

    private static RazorModel Model(params ColumnModel[] cols)
        => new("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(cols)));

    [Fact]
    public void EmitsVirtualizeAndDisplayModeAndOverscan() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("Name")));
        src.ShouldContain("\"Virtualize\", true");
        src.ShouldContain("DataGridDisplayMode.Table");
        src.ShouldContain("\"OverscanCount\", 3");
    }

    [Fact]
    public void EmitsItemSizeFromDensityMetricsAndSetKeyOnDensity() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("Name")));
        src.ShouldContain("DataGridDensityMetrics.ResolveRowHeightPx(_density)");
        src.ShouldContain("builder.SetKey(_density);");
        src.ShouldContain("_density = RenderContext?.DensityLevel");
    }

    [Fact]
    public void EmitsProjectionGridClassStickyHeaderItemSizeAndDensityKeyTogether() {
        string src = GeneratedRenderTreeText.MaskSequenceArguments(RazorEmitter.Emit(Model(Col("Id"), Col("Name"))));
        int gridIndex = src.IndexOf("builder.OpenComponent<FluentDataGrid<OrderProjection>>(#);", StringComparison.Ordinal);
        gridIndex.ShouldBeGreaterThanOrEqualTo(0);

        string gridBlock = src[gridIndex..src.IndexOf("builder.CloseComponent();", gridIndex, StringComparison.Ordinal)];
        gridBlock.ShouldContain("builder.SetKey(_density);");
        gridBlock.ShouldContain("\"Class\", \"fc-projection-grid\"");
        gridBlock.ShouldContain("\"GenerateHeader\", Microsoft.FluentUI.AspNetCore.Components.DataGridGeneratedHeaderType.Sticky");
        gridBlock.ShouldContain("\"ItemSize\", Hexalith.FrontComposer.Shell.Components.Rendering.DataGridDensityMetrics.ResolveRowHeightPx(_density)");
        src.ShouldContain("return \"fc-datagrid-host fc-projection-grid\";");
    }

    [Fact]
    public void EmitsStatusFilterChipsForBadgeMappedProjection() {
        string src = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));

        src.ShouldContain("private static readonly System.Collections.Generic.IReadOnlyList<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> _statusFilterSlots");
        src.ShouldContain("global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot.Success");
        src.ShouldContain("global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot.Warning");
        src.ShouldContain("FcStatusFilterChips");
        src.ShouldContain("\"AvailableSlots\", _statusFilterSlots");
        src.ShouldContain("\"ActiveSlots\", ActiveStatusSlots(gridSnapshot)");
        src.ShouldContain("ReservedFilterKeys.StatusKey");
        src.ShouldContain("private static System.Collections.Generic.IReadOnlyList<OrderProjection> TemplateItems(System.Collections.Generic.IReadOnlyList<OrderProjection>? items, global::Hexalith.FrontComposer.Contracts.Rendering.GridViewSnapshot? snapshot)");
        src.ShouldContain("items: TemplateItems(state.Items, gridSnapshot)");
        src.ShouldContain("var __detailItems = TemplateItems(state.Items, CurrentGridSnapshot());");
    }

    [Theory]
    [InlineData("AggregateId")]
    [InlineData("Id")]
    [InlineData("Key")]
    public void ItemKeyAccessor_PrecedenceOverFallback(string propertyName) {
        string src = RazorEmitter.Emit(Model(Col(propertyName), Col("Other")));
        src.ShouldContain("static x => (object)x." + propertyName + "!");
        src.ShouldContain("\"ItemKey\", (System.Func<OrderProjection, object>)_itemKeyAccessor");
    }

    [Fact]
    public void ItemKeyAccessor_AggregateIdWinsOverId() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("AggregateId"), Col("Name")));
        src.ShouldContain("static x => (object)x.AggregateId!");
        src.ShouldNotContain("static x => (object)x.Id!");
    }

    [Fact]
    public void ItemKeyAccessor_FallsBackToIdentityWhenNoMatchingProperty() {
        string src = RazorEmitter.Emit(Model(Col("Name"), Col("Other")));
        src.ShouldContain("static x => (object)x;");
    }

    [Fact]
    public void Emit_UsesLiteralRenderTreeSequencesInsteadOfRuntimeCounters() {
        RenderTreeSequenceRewriterTests.ShouldUseLiteralRenderTreeSequences(
            RazorEmitter.Emit(Model(Col("Id"), Col("Name"), BadgeCol())));
    }

    [Fact]
    public void Emit_PicksTheConcreteHiddenColumnAndBadgeSetTypes() {
        string source = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));

        // Story 11.21 CA1859 — concrete return/parameter types; both call sites already hand in a
        // HashSet, and every ResolveHiddenColumns path already produced a string[].
        source.ShouldContain("private static string[] ResolveHiddenColumns(");
        source.ShouldContain("System.Collections.Generic.HashSet<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> activeSlots)");
        source.ShouldNotContain("System.Collections.Generic.ISet<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> activeSlots)");
    }

    [Fact]
    public void Emit_TruncateUsesSpanBasedConcatWithIdenticalOutput() {
        string source = RazorEmitter.Emit(Model(Col("Id")));

        source.ShouldContain("=> maxLength <= 0 ? string.Empty : value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 1), \"\\u2026\");");
        source.ShouldNotContain("value.Substring(0, maxLength - 1) + ");
    }

    [Fact]
    public void Emit_TruncateFailsSoftAtNonPositiveAndPositiveBounds() {
        MethodInfo truncate = CompileTruncateMethod(RazorEmitter.Emit(Model(Col("Id"))));

        truncate.Invoke(null, ["abcdef", -1]).ShouldBe(string.Empty);
        truncate.Invoke(null, ["abcdef", 0]).ShouldBe(string.Empty);
        truncate.Invoke(null, ["abcdef", 1]).ShouldBe("\u2026");
        truncate.Invoke(null, ["abcdef", 4]).ShouldBe("abc\u2026");
        truncate.Invoke(null, ["abcdef", 6]).ShouldBe("abcdef");
    }

    [Fact]
    public void Emit_DefaultFieldRendererIsStaticOnlyWhenItReadsNoInstanceState() {
        // Text columns render from the row alone.
        RazorEmitter.Emit(Model(Col("Id"), Col("Name")))
            .ShouldContain("private static global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");

        // A badge column falls back to the injected shell localizer for unmapped members.
        string withBadge = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));
        withBadge.ShouldContain("private global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");
        withBadge.ShouldNotContain("private static global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");
    }

    [Fact]
    public void Emit_ProjectionTeardownSuppressesFinalization() {
        RazorEmitter.Emit(Model(Col("Id"))).ShouldContain("System.GC.SuppressFinalize(this);");
    }

    [Fact]
    public void Emit_ProjectionTeardownUsesAtomicOnceOnlyGuardsForGridAndNonGridViews() {
        const string Guard = "if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)";
        string grid = RazorEmitter.Emit(Model(Col("Id")));
        RazorModel nonGridModel = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(Col("Id"))),
            ProjectionRenderStrategy.DetailRecord);
        string nonGrid = RazorEmitter.Emit(nonGridModel);

        grid.ShouldContain("public async ValueTask DisposeAsync()");
        grid.ShouldContain(Guard);
        grid.IndexOf(Guard, StringComparison.Ordinal)
            .ShouldBeLessThan(grid.IndexOf("_newItemIndicatorSubscription?.Dispose();", StringComparison.Ordinal));
        grid.IndexOf(Guard, StringComparison.Ordinal)
            .ShouldBeLessThan(grid.IndexOf("System.GC.SuppressFinalize(this);", StringComparison.Ordinal));
        nonGrid.ShouldContain("public void Dispose()");
        nonGrid.ShouldContain(Guard);
        nonGrid.IndexOf(Guard, StringComparison.Ordinal)
            .ShouldBeLessThan(nonGrid.IndexOf("State.StateChanged -= OnStateChanged;", StringComparison.Ordinal));
        nonGrid.IndexOf(Guard, StringComparison.Ordinal)
            .ShouldBeLessThan(nonGrid.IndexOf("System.GC.SuppressFinalize(this);", StringComparison.Ordinal));
    }

    [Fact]
    public void Emit_NonGridDisposeUnsubscribesStateHandlerOnlyOnceAtRuntime() {
        RazorModel nonGridModel = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(Col("Id"))),
            ProjectionRenderStrategy.DetailRecord);
        Type fixtureType = CompileNonGridDisposeFixture(RazorEmitter.Emit(nonGridModel));
        object fixture = Activator.CreateInstance(fixtureType)!;
        MethodInfo dispose = fixtureType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)!;

        _ = dispose.Invoke(fixture, null);
        _ = dispose.Invoke(fixture, null);

        fixtureType.GetProperty("StateRemoveCount")!.GetValue(fixture).ShouldBe(1);
    }

    private static MethodInfo CompileTruncateMethod(string generatedSource) {
        MethodDeclarationSyntax truncate = CSharpSyntaxTree.ParseText(generatedSource)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "Truncate");
        string fixture = "using System;\r\n\r\npublic static class TruncateFixture\r\n{\r\n"
            + truncate.ToFullString()
            + "    public static string Invoke(string value, int maxLength) => Truncate(value, maxLength);\r\n}\r\n";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            fixture,
            assemblyName: "TruncateFixture_" + Guid.NewGuid().ToString("N"));
        Assembly assembly = CompileFixture(compilation);

        return assembly.GetType("TruncateFixture")!
            .GetMethod("Invoke", BindingFlags.Public | BindingFlags.Static)!;
    }

    private static Type CompileNonGridDisposeFixture(string generatedSource) {
        SyntaxNode root = CSharpSyntaxTree.ParseText(generatedSource).GetRoot();
        FieldDeclarationSyntax disposedField = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Single(field => field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "_disposed"));
        MethodDeclarationSyntax disposeMethod = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "Dispose" && method.ParameterList.Parameters.Count == 0);
        string fixture = "using System;\r\n\r\npublic sealed class NonGridDisposeFixture\r\n{\r\n"
            + disposedField.ToFullString()
            + "    public RecordingState OrderProjectionState { get; } = new();\r\n"
            + "    public int StateRemoveCount => OrderProjectionState.RemoveCount;\r\n\r\n"
            + "    public NonGridDisposeFixture() => OrderProjectionState.StateChanged += OnStateChanged;\r\n\r\n"
            + "    private void OnStateChanged(object? sender, EventArgs e) { }\r\n\r\n"
            + disposeMethod.ToFullString()
            + "}\r\n\r\npublic sealed class RecordingState\r\n{\r\n"
            + "    public int RemoveCount { get; private set; }\r\n\r\n"
            + "    public event EventHandler? StateChanged\r\n    {\r\n"
            + "        add { }\r\n        remove { RemoveCount++; }\r\n    }\r\n}\r\n";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            fixture,
            assemblyName: "NonGridDisposeFixture_" + Guid.NewGuid().ToString("N"));

        return CompileFixture(compilation).GetType("NonGridDisposeFixture")!;
    }

    private static Assembly CompileFixture(CSharpCompilation compilation) {
        using MemoryStream stream = new();
        EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(stream);
    }

}

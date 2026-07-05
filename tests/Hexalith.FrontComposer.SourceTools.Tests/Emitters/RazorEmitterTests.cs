using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class RazorEmitterTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public Task BasicProjection_Snapshot() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Name", "Name", TypeCategory.Text),
                Col("Count", "Count", TypeCategory.Numeric, "N0"),
                Col("IsActive", "Is Active", TypeCategory.Boolean, "Yes/No"),
                Col("CreatedAt", "Created At", TypeCategory.DateTime, "d"))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public Task DisplayNameOverrides_Snapshot() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("OrderDate", "Date Ordered", TypeCategory.DateTime, "d"))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public void EmittedCode_HumanizesEnumLabels_AndPreservesThirtyCharacterLimit() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Status", "Status", TypeCategory.Enum, "Humanize:30"))));
        string source = RazorEmitter.Emit(model);
        source.ShouldContain("HumanizeEnumLabel");
        source.ShouldContain("maxLength - 1");
        source.ShouldContain("Truncate(HumanizeEnumLabel");
    }

    [Fact]
    public void EmittedCode_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Name", "Name", TypeCategory.Text),
                Col("Count", "Count", TypeCategory.Numeric, "N0"),
                Col("IsActive", "Is Active", TypeCategory.Boolean, "Yes/No"),
                Col("CreatedAt", "Created At", TypeCategory.DateTime, "d"),
                Col("Id", "Id", TypeCategory.Text, "Truncate:8"),
                Col("Status", "Status", TypeCategory.Enum, "Humanize:30"),
                Col("Items", "Items", TypeCategory.Collection, "Count"))));

        string source = RazorEmitter.Emit(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Razor code should parse without syntax errors");
    }

    [Fact]
    public void EmittedCode_PassesEntityPluralOverrideToEmptyPlaceholder() {
        var model = new RazorModel(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(Col("Name", "Name", TypeCategory.Text))),
            entityPluralLabel: "Purchase orders");

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("builder.AddAttribute(seq++, \"EntityPluralOverride\", \"Purchase orders\");");
    }

    [Fact]
    public void EmittedGrid_RendersNewItemIndicatorsFromStateSnapshot() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Name", "Name", TypeCategory.Text))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("private global::Hexalith.FrontComposer.Shell.State.PendingCommands.INewItemIndicatorStateService NewItemIndicators { get; set; } = default!;");
        source.ShouldContain("private void RenderNewItemIndicators(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, ref int seq)");
        source.ShouldContain("foreach (var entry in NewItemIndicators.Snapshot(_viewKey))");
        source.ShouldContain("builder.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.DataGrid.FcNewItemIndicator>(seq++);");
        source.ShouldContain("builder.SetKey(entry.EntityKey);");
        source.ShouldContain("RenderNewItemIndicators(builder, ref seq);");
    }

    [Fact]
    public void EmittedGrid_DismissesIndicatorsOnMaterializedRowsAndLaneChanges() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Name", "Name", TypeCategory.Text))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("DismissMaterializedIndicators(OrderProjectionState.Value.Items);");
        source.ShouldContain("DismissMaterializedIndicators(typed);");
        source.ShouldContain("NewItemIndicators.DismissMaterialized(_viewKey, entityKey);");
        source.ShouldContain("if (_registeredProjectionFallbackLaneKey is not null)");
        source.ShouldContain("NewItemIndicators.DismissForFilterChange(_viewKey);");
    }

    [Fact]
    public void EmittedGrid_CascadesPendingCommandRowIdentityThroughFieldSlots() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Status", "Status", TypeCategory.Enum))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("private static global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity? PendingCommandRowIdentityFor(OrderProjection row)");
        source.ShouldContain("return new global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity(");
        source.ShouldContain("ProjectionTypeFromViewKey(),");
        source.ShouldContain("_viewKey,");
        source.ShouldContain("ExpectedStatusSlotFromItem(row));");
        source.ShouldContain("var __pendingCommandRowIdentity = PendingCommandRowIdentityFor(row);");
        source.ShouldContain("builder.OpenComponent<global::Microsoft.AspNetCore.Components.CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>>(0);");
        source.ShouldContain("__cascadeBuilder.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.Rendering.FcFieldSlotHost<OrderProjection, TField>>(0);");
    }

    [Fact]
    public void EmittedCode_EscapesStatusOverviewRouteLiteral() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        var model = new RazorModel(
            "OrderProjection",
            "TestDomain",
            "Orders\"North",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Status", "Status", TypeCategory.Enum))),
            ProjectionRenderStrategy.StatusOverview);

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("FcProjectionRoutes.StatusFilter(\"/Orders\\\"North\", status)");
        CSharpSyntaxTree.ParseText(source, cancellationToken: ct).GetDiagnostics(ct).ShouldBeEmpty();
    }

    [Fact]
    public void EmittedCode_UsesCorrectNumericFormats() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Count", "Count", TypeCategory.Numeric, "N0"),
                Col("Price", "Price", TypeCategory.Numeric, "N2"))));
        string source = RazorEmitter.Emit(model);
        source.ShouldContain("\"N0\"");
        source.ShouldContain("\"N2\"");
    }

    [Fact]
    public void EmittedCode_UsesEmDash_NotHyphen() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Name", "Name", TypeCategory.Text))));
        string source = RazorEmitter.Emit(model);
        source.ShouldContain("\\u2014");
    }

    [Fact]
    public void EmittedCode_UsesShortDateFormat_LowercaseD() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("CreatedAt", "Created At", TypeCategory.DateTime, "d"))));
        string source = RazorEmitter.Emit(model);
        // The date format specifier "d" (short date) should be lowercase, not "D" (long date)
        source.ShouldContain("ToString(\"d\"");
    }

    [Fact]
    public Task EnumAndBadgeMappings_Snapshot() {
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Active", "Success"),
            new BadgeMappingEntry("Inactive", "Neutral")));

        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false, badges))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public Task GuidTruncation_Snapshot() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text, "Truncate:8"))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public Task NullableProperties_Snapshot() {
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Name", "Name", TypeCategory.Text, isNullable: true),
                Col("Count", "Count", TypeCategory.Numeric, "N0", isNullable: true),
                Col("IsActive", "Is Active", TypeCategory.Boolean, "Yes/No", isNullable: true))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public Task DescriptionWithEscapeEdgeCases_Snapshot() {
        // GD-P1 (review of Story 6-3 Group D, 2026-05-01) — Lock down the slot-line description
        // literalizer (RazorEmitter.SlotStringLiteral) for non-null values containing escape
        // edge cases that prior snapshots never exercised: embedded quotes, backslash, and a
        // newline. The verified.txt snapshot proves SlotStringLiteral routes through the same
        // EscapeString helper as DescriptionTooltipEmissionTests.
        const string EdgeCaseDescription = "Quoted \"value\", \\ backslash, and \n newline.";
        var model = new RazorModel("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Name", "Name", TypeCategory.Text, formatHint: null, isNullable: false, badgeMappings: _emptyBadges, description: EdgeCaseDescription))));

        string result = RazorEmitter.Emit(model);
        return Verify(result);
    }

    private static ColumnModel Col(string name, string header, TypeCategory cat, string? formatHint = null, bool isNullable = false)
                                                => new(name, header, cat, formatHint, isNullable, _emptyBadges);

    // Semantic spot-checks
}

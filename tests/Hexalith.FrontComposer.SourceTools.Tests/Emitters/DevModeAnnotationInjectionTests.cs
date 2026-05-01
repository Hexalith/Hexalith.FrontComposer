using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class DevModeAnnotationInjectionTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void ProjectionView_EmitsDebugOnlyDevModeAnnotation() {
        string source = RazorEmitter.Emit(Model(Col("Name", TypeCategory.Text)));

        source.ShouldContain("#if DEBUG");
        source.ShouldContain("IServiceProvider DevModeServices");
        source.ShouldContain("DevModeServices.GetService(typeof(global::Hexalith.FrontComposer.Shell.Services.DevMode.IDevModeOverlayController))");
        source.ShouldContain("FcDevModeAnnotation");
        source.ShouldContain("ComponentTreeNode");
        source.ShouldContain("ComponentTreeContractVersion.Current");
    }

    [Fact]
    public void UnsupportedPlaceholder_EmitsUnsupportedDevModeAnnotationSibling() {
        string source = RazorEmitter.Emit(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        source.ShouldContain("FcFieldPlaceholder");
        source.ShouldContain("fc-devmode-unsupported");
        source.ShouldContain("IsUnsupported: true");
        source.ShouldContain("Use a Level 3 slot override.");
    }

    [Fact]
    public void DevModeAnnotationInjection_ParsesAsValidCSharp() {
        string source = RazorEmitter.Emit(Model(Col("Name", TypeCategory.Text)));

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken)
            .GetDiagnostics(cancellationToken)
            .ShouldBeEmpty();
    }

    private static RazorModel Model(params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(columns.ToImmutableArray()));

    private static ColumnModel Col(string name, TypeCategory category)
        => new(name, name, category, formatHint: null, isNullable: false, EmptyBadges);

    private static ColumnModel Unsupported(string name, string typeName)
        => new(
            name,
            name,
            TypeCategory.Unsupported,
            formatHint: null,
            isNullable: false,
            EmptyBadges,
            unsupportedTypeFullyQualifiedName: typeName);
}

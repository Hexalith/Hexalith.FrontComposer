using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hexalith.FrontComposer.SourceTools.Tests;

internal static class CompilationHelper {
    private static MetadataReference[] GetBaseReferences() {
        List<MetadataReference> refs =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
        ];

        // Add runtime assemblies needed for netcoreapp compilation
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string[] additionalDlls = ["System.Runtime.dll", "netstandard.dll", "System.Collections.dll", "System.Collections.Concurrent.dll", "System.Collections.Immutable.dll", "System.Linq.dll", "System.Linq.Queryable.dll", "System.Linq.Expressions.dll", "System.Private.Uri.dll"];
        foreach (string dll in additionalDlls) {
            string path = Path.Combine(runtimeDir, dll);
            if (File.Exists(path)) {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // Add Fluxor, FluentUI, and ASP.NET Components for generated code compilation
        TryAddAssemblyRef(refs, typeof(Fluxor.IState<>));                                  // Fluxor
        TryAddAssemblyRef(refs, typeof(Fluxor.Feature<>));                                 // Fluxor
        TryAddAssemblyRef(refs, typeof(Fluxor.ReducerMethodAttribute));                    // Fluxor
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.ComponentBase));     // ASP.NET Components
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.InjectAttribute));   // ASP.NET Components
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder)); // ASP.NET Rendering
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.Forms.EditContext)); // ASP.NET Forms
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.Forms.EditForm));    // ASP.NET Components.Web
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider)); // ASP.NET Components.Authorization (Story 7-3 Pass 2 DN1)
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.Localization.IStringLocalizer<>)); // Localization
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.Logging.ILogger<>));            // Logging
        TryAddAssemblyRef(refs, typeof(Microsoft.FluentUI.AspNetCore.Components.FluentDataGrid<>));   // FluentUI
        // Generated rendering contexts live in the explicit net10-only UI contracts assembly.
        TryAddAssemblyRef(refs, typeof(Hexalith.FrontComposer.Contracts.Rendering.FieldSlotContext<,>));
        // Story 2-4 — emitter wraps generated EditForm in FcLifecycleWrapper, so the Shell
        // assembly must be resolvable by the test compilation.
        TryAddAssemblyRef(refs, typeof(Hexalith.FrontComposer.Shell.Components.Lifecycle.FcLifecycleWrapper));
        // Story 2-2: generated renderer + subscriber pull in these ASP.NET / Extensions types.
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.Options.IOptions<>));            // Options
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)); // DI
        TryAddAssemblyRef(refs, typeof(Microsoft.JSInterop.IJSRuntime));                     // JSInterop
        TryAddAssemblyRef(refs, typeof(Microsoft.AspNetCore.Components.RouteAttribute));     // Routing
        // System.Collections.Concurrent explicit ref (ConcurrentDictionary<,>).
        TryAddAssemblyRef(refs, typeof(System.Collections.Concurrent.ConcurrentDictionary<,>));
        TryAddAssemblyRef(refs, typeof(System.Collections.Immutable.ImmutableDictionary<,>)); // Immutable collections

        return refs.ToArray();
    }

    private static void TryAddAssemblyRef(List<MetadataReference> refs, Type type) {
        string location = type.Assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location)) {
            refs.Add(MetadataReference.CreateFromFile(location));
        }
    }

    internal static CSharpCompilation CreateCompilation(string source, bool enableNullable = true) {
        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(enableNullable ? NullableContextOptions.Enable : NullableContextOptions.Disable);

        return CSharpCompilation.Create(
            "TestAssembly",
            [CreateSyntaxTree(source, "Test0.cs")],
            GetBaseReferences(),
            options);
    }

    internal static CSharpCompilation CreateCompilation(string[] sources, bool enableNullable = true) {
        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(enableNullable ? NullableContextOptions.Enable : NullableContextOptions.Disable);

        return CSharpCompilation.Create(
            "TestAssembly",
            sources.Select((source, index) => CreateSyntaxTree(source, $"Test{index}.cs")),
            GetBaseReferences(),
            options);
    }

    internal static ParseResult ParseProjection(string source, string metadataName, bool enableNullable = true)
        => ParseProjection(CreateCompilation(source, enableNullable), metadataName);

    internal static ParseResult ParseProjection(string[] sources, string metadataName, bool enableNullable = true)
        => ParseProjection(CreateCompilation(sources, enableNullable), metadataName);

    internal static ParseResult ParseProjection(CSharpCompilation compilation, string metadataName) {
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"Could not find projection type '{metadataName}' in the test compilation.");

        SyntaxReference syntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()
            ?? throw new InvalidOperationException($"Projection type '{metadataName}' has no declaring syntax reference.");

        SyntaxNode targetNode = syntaxReference.GetSyntax(TestContext.Current.CancellationToken);
        return AttributeParser.Parse(typeSymbol, targetNode, TestContext.Current.CancellationToken);
    }

    internal static CommandParseResult ParseCommand(string source, string metadataName, bool enableNullable = true)
        => ParseCommand(CreateCompilation(source, enableNullable), metadataName);

    internal static CommandParseResult ParseCommand(CSharpCompilation compilation, string metadataName) {
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"Could not find command type '{metadataName}' in the test compilation.");

        SyntaxReference syntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()
            ?? throw new InvalidOperationException($"Command type '{metadataName}' has no declaring syntax reference.");

        SyntaxNode targetNode = syntaxReference.GetSyntax(TestContext.Current.CancellationToken);
        return CommandParser.Parse(typeSymbol, targetNode, TestContext.Current.CancellationToken);
    }

    private static SyntaxTree CreateSyntaxTree(string source, string filePath)
        => CSharpSyntaxTree.ParseText(source, path: filePath);

    /// <summary>
    /// Story 9-1 review P4: drift detection is opt-in. Tests that exercise the drift pipeline
    /// must pass an <see cref="AnalyzerConfigOptionsProvider"/> with
    /// <c>build_property.HfcDriftDetectionEnabled=true</c>; this helper builds one and lets
    /// callers add additional options for narrower scenarios (path overrides, severity, caps).
    /// </summary>
    internal static AnalyzerConfigOptionsProvider DriftEnabledOptions(IReadOnlyDictionary<string, string>? extra = null) {
        Dictionary<string, string> values = extra is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(extra, StringComparer.OrdinalIgnoreCase);
        values["build_property.HfcDriftDetectionEnabled"] = "true";
        return new InMemoryDriftOptionsProvider(values);
    }

    private sealed class InMemoryDriftOptionsProvider(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemory(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemory(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
            // Story 9-1 review CB-27: Roslyn's real `AnalyzerConfigOptions.TryGetValue` leaves
            // `out value` at its default (`null`) on miss. Returning `string.Empty` here would
            // diverge from production and let consumers that ignore the bool see a present-but-
            // empty string instead of "absent". Suppress the nullable warning explicitly.
            public override bool TryGetValue(string key, out string value) {
                if (values.TryGetValue(key, out string? v)) {
                    value = v;
                    return true;
                }

                value = null!;
                return false;
            }
        }
    }
}

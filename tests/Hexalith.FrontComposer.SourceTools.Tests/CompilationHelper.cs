using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hexalith.FrontComposer.SourceTools.Tests;

internal static class CompilationHelper {
    private static MetadataReference[] GetBaseReferences() {
        List<MetadataReference> refs =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location),
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
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.Localization.IStringLocalizer<>)); // Localization
        TryAddAssemblyRef(refs, typeof(Microsoft.Extensions.Logging.ILogger<>));            // Logging
        TryAddAssemblyRef(refs, typeof(Microsoft.FluentUI.AspNetCore.Components.FluentDataGrid<>));   // FluentUI
        // Story 4-5 T2.1 — generated views reference the Icons.Regular.Size20.ChevronRight icon
        // type for the row-action chevron button. Lives in a separate assembly from the main
        // FluentUI package, so the test compilation needs an explicit reference.
        TryAddAssemblyRef(refs, typeof(Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.Size20.ChevronRight));
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
}

using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class CommandFormValidationCompilationTests
{
    private static readonly string[] Net11Symbols = ["NET11_0_OR_GREATER"];
    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public void EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(int frameworkMajor)
    {
        CommandFormModel form = new("CreateCommand", "Consumer", null, "Consumer.CreateCommand", "Create",
            new EquatableArray<FormFieldModel>(ImmutableArray<FormFieldModel>.Empty));
        CommandFluxorModel fluxor = new("CreateCommand", "Consumer", "CreateState", "CreateFeature", "CreateActions",
            "CreateReducers", "Consumer.CreateCommand", "Consumer.CreateState");
        string emitted = CommandFormEmitter.Emit(form, fluxor);
        int start = emitted.IndexOf("        if (validateBeforeDispatch && _editContext is { } editContext", StringComparison.Ordinal);
        int end = emitted.IndexOf("        if (HasClientParseErrors())", start, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        end.ShouldBeGreaterThan(start);
        string source = $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components.Forms;
            public sealed class Consumer
            {
                private long _modelRevision = 0;
                public async Task SubmitAsync(bool validateBeforeDispatch, EditContext? _editContext, bool _disposed, CancellationToken cancellationToken)
                {
            {{emitted[start..end]}}
                    await Task.CompletedTask;
                }
                private static bool IsAdmissionScopeCurrent() => true;
                private static Task ShowValidationSummaryAsync(FcValidationSummaryKind kind) => Task.CompletedTask;
            }
            public enum FcValidationSummaryKind { ClientValidation }
            """;
        string runtimeRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "..", ".."));
        string coreRuntime = FindRuntime(runtimeRoot, "Microsoft.NETCore.App", frameworkMajor);
        string aspNetRuntime = FindRuntime(runtimeRoot, "Microsoft.AspNetCore.App", frameworkMajor);
        IEnumerable<MetadataReference> references = Directory.EnumerateFiles(coreRuntime, "*.dll")
            .Append(Path.Combine(aspNetRuntime, "Microsoft.AspNetCore.Components.Forms.dll"))
            .Select(path => MetadataReference.CreateFromFile(path));
        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(
            frameworkMajor >= 11 ? Net11Symbols : Array.Empty<string>());
        CSharpCompilation compilation = CSharpCompilation.Create("GeneratedValidationConsumer",
            [CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: TestContext.Current.CancellationToken)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                generalDiagnosticOption: ReportDiagnostic.Error));

        using MemoryStream assembly = new();
        var result = compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
            .ShouldBeEmpty();
        result.Success.ShouldBeTrue();
    }

    private static string FindRuntime(string runtimeRoot, string framework, int frameworkMajor)
        => Directory.EnumerateDirectories(Path.Combine(runtimeRoot, framework))
            .Where(path => Path.GetFileName(path).StartsWith(frameworkMajor + ".", StringComparison.Ordinal))
            .OrderByDescending(path => Version.Parse(Path.GetFileName(path)))
            .First();
}

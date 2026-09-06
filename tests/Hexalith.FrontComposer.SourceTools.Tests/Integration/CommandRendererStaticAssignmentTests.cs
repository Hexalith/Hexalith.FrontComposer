using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

public class CommandRendererStaticAssignmentTests {
    [Fact]
    public async Task GeneratedRenderer_ExecutesConversionMatrixAndContinuesProviderChain() {
        const string source = """
            using System;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace RuntimeFixtures;

            public enum MatrixStatus { Pending, Approved }
            public readonly struct CustomValue { }

            [Command]
            public sealed class MatrixCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)] public int Count { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public int? Optional { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public MatrixStatus Status { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public Guid RequestId { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public DateTimeOffset OccurredAt { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public CustomValue Custom { get; set; }
                [Obsolete("removed", true)]
                [DerivedFrom(DerivedFromSource.Context)] public int Fatal { get; set; }
            }
            """;
        Assembly assembly = CompileGeneratedAssembly(source);
        Type rendererType = assembly.GetType("RuntimeFixtures.MatrixCommandRenderer").ShouldNotBeNull();
        object renderer = Activator.CreateInstance(rendererType).ShouldNotBeNull();
        MethodInfo trySet = rendererType.GetMethod("TrySetPropertyValue", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        FieldInfo modelField = rendererType.GetField("_prefilledModel", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        object model = modelField.GetValue(renderer).ShouldNotBeNull();

        InvokeTrySet(trySet, renderer, "MessageId", "exact").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Count", "42").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Optional", "7").ShouldBeTrue();
        object approved = Enum.Parse(assembly.GetType("RuntimeFixtures.MatrixStatus").ShouldNotBeNull(), "Approved");
        InvokeTrySet(trySet, renderer, "Status", approved).ShouldBeTrue();
        Guid requestId = Guid.NewGuid();
        InvokeTrySet(trySet, renderer, "RequestId", requestId.ToString("D")).ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "OccurredAt", "2026-09-06T08:30:00+00:00").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Optional", null).ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Custom", new ConversionProbe(returnNull: true)).ShouldBeTrue();

        model.GetType().GetProperty("MessageId").ShouldNotBeNull().GetValue(model).ShouldBe("exact");
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(42);
        model.GetType().GetProperty("Optional").ShouldNotBeNull().GetValue(model).ShouldBeNull();
        model.GetType().GetProperty("Status").ShouldNotBeNull().GetValue(model).ShouldBe(approved);
        model.GetType().GetProperty("RequestId").ShouldNotBeNull().GetValue(model).ShouldBe(requestId);

        InvokeTrySet(trySet, renderer, "Count", "not-an-integer").ShouldBeFalse();
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(42);
        InvokeTrySet(trySet, renderer, "Custom", new ConversionProbe(returnNull: false)).ShouldBeFalse();
        InvokeTrySet(trySet, renderer, "Unknown", 1).ShouldBeFalse();
        InvokeTrySet(trySet, renderer, "Fatal", 1).ShouldBeFalse();

        ResolvedDerivedValueProvider invalid = new("bad");
        ResolvedDerivedValueProvider valid = new("41");
        SetProviders(rendererType, renderer, invalid, valid);
        await InvokePrefillAsync(rendererType, renderer, "Count").ConfigureAwait(true);
        invalid.Calls.ShouldBe(1);
        valid.Calls.ShouldBe(1);
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(41);

        ResolvedDerivedValueProvider unsafeFirst = new(1);
        ResolvedDerivedValueProvider unsafeSecond = new(2);
        SetProviders(rendererType, renderer, unsafeFirst, unsafeSecond);
        await InvokePrefillAsync(rendererType, renderer, "Fatal").ConfigureAwait(true);
        unsafeFirst.Calls.ShouldBe(1);
        unsafeSecond.Calls.ShouldBe(1);
    }

    [Fact]
    public void GeneratedRenderer_UnsafeTypesUseSoftFailArmsAndKeywordMemberCompiles() {
        const string source = """
            using System;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace UnsafeFixtures;

            [Command]
            public unsafe sealed class UnsafeCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)] public Span<int> RefLike { get => default; set { } }
                [DerivedFrom(DerivedFromSource.Context)] public int* Pointer { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public delegate*<void> Callback { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public int*[] PointerArray { get; set; } = [];
                [DerivedFrom(DerivedFromSource.Context)] public delegate*<void>[] CallbackArray { get; set; } = [];
                [Obsolete("removed", true)]
                [DerivedFrom(DerivedFromSource.Context)] public int Fatal { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public string @event { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, allowUnsafe: true);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "UnsafeFixtures.UnsafeCommand.CommandRenderer.g.razor.cs");
        foreach (string property in (string[])["RefLike", "Pointer", "Callback", "PointerArray", "CallbackArray", "Fatal"]) {
            string arm = SliceCase(renderer, property);
            arm.Trim().ShouldBe($"case \"{property}\":\n                return false;");
        }

        renderer.ShouldContain("_prefilledModel.@event =");
        renderer.ShouldNotContain("GetProperty(");
        renderer.ShouldNotContain("SetValue(");
        renderer.ShouldNotContain("unsafe ");
    }

    [Fact]
    public void GeneratedRenderer_AliasOnlyTypeEmitsRequiredExternAliasAndCompiles() {
        const string aliasedSource = "namespace AliasLibrary; public sealed class Token { }";
        CSharpCompilation aliasedCompilation = CompilationHelper.CreateCompilation(aliasedSource, assemblyName: "AliasLibrary");
        using MemoryStream aliasStream = new();
        aliasedCompilation.Emit(aliasStream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        MetadataReference aliasedReference = MetadataReference.CreateFromImage(
            aliasStream.ToArray(),
            properties: MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("OnlyAlias")));
        const string source = """
            extern alias OnlyAlias;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace AliasFixtures;

            [Command]
            public sealed class AliasCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)]
                public OnlyAlias::AliasLibrary.Token Token { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: [aliasedReference]);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "AliasFixtures.AliasCommand.CommandRenderer.g.razor.cs");
        renderer.ShouldContain("extern alias OnlyAlias;");
        renderer.ShouldContain("TryConvertPropertyValue<OnlyAlias::AliasLibrary.Token>");
    }

    [Fact]
    public void SuppressedHfc1016_RemainsInvalidAndProducesNoCommandArtifacts() {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            [assembly: SuppressMessage("HexalithFrontComposer", "HFC1016", Justification = "test")]

            namespace SuppressionFixtures;

            [Command]
            public sealed class InvalidCommand
            {
                public string MessageId { get; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        result.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1016").IsSuppressed.ShouldBeTrue();
        result.GeneratedTrees.ShouldNotContain(tree => Path.GetFileName(tree.FilePath).Contains("InvalidCommand.Command", StringComparison.Ordinal));
        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
    }

    private static Assembly CompileGeneratedAssembly(string source) {
        string assemblyName = "RendererRuntime_" + Guid.NewGuid().ToString("N");
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, assemblyName: assemblyName);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        using MemoryStream stream = new();
        EmitResult emitResult = outputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        emitResult.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitResult.Diagnostics));
        stream.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(stream);
    }

    private static GeneratorDriverRunResult RunGenerator(CSharpCompilation compilation, out CSharpCompilation outputCompilation) {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees.ToArray());
        return result;
    }

    private static string GetRenderer(GeneratorDriverRunResult result, string hintName)
        => result.GeneratedTrees.Single(tree => Path.GetFileName(tree.FilePath) == hintName)
            .GetText(TestContext.Current.CancellationToken)
            .ToString();

    private static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty(string.Join(Environment.NewLine, diagnostics));

    private static bool InvokeTrySet(MethodInfo method, object renderer, string propertyName, object? value)
        => (bool)method.Invoke(renderer, [propertyName, value]).ShouldNotBeNull();

    private static void SetProviders(Type rendererType, object renderer, params IDerivedValueProvider[] providers)
        => rendererType.GetProperty("DerivedValueProviders", BindingFlags.Instance | BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .SetValue(renderer, providers);

    private static async Task InvokePrefillAsync(Type rendererType, object renderer, string propertyName) {
        MethodInfo method = rendererType.GetMethod("TryPrefillPropertyAsync", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        Task task = (Task)method.Invoke(renderer, [propertyName]).ShouldNotBeNull();
        await task.ConfigureAwait(true);
    }

    private static string SliceCase(string source, string propertyName) {
        string marker = $"case \"{propertyName}\":";
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        int nextCase = source.IndexOf("            case \"", start + marker.Length, StringComparison.Ordinal);
        int defaultCase = source.IndexOf("            default:", start + marker.Length, StringComparison.Ordinal);
        int end = nextCase >= 0 && nextCase < defaultCase ? nextCase : defaultCase;
        end.ShouldBeGreaterThan(start);
        return source[start..end];
    }
}

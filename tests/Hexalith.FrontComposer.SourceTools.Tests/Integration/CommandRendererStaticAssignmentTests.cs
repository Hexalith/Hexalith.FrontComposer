using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

public partial class GeneratorDriverTests {
    [Fact]
    public async Task GeneratedRenderer_ExecutesConversionMatrixAndContinuesProviderChain() {
        const string source = """
            using System;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace RuntimeFixtures;

            public enum MatrixStatus { Pending, Approved }
            public readonly struct CustomValue
            {
                public CustomValue(int value) => Value = value;
                public int Value { get; }
            }

            [Command]
            public sealed class MatrixCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)] public int Count { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public int? Optional { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public decimal Amount { get; set; }
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

        InvokeTrySet(trySet, renderer, "MessageId", null).ShouldBeTrue();
        model.GetType().GetProperty("MessageId").ShouldNotBeNull().GetValue(model).ShouldBeNull();
        InvokeTrySet(trySet, renderer, "MessageId", "exact").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Count", "42").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Count", null).ShouldBeTrue();
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(0);
        InvokeTrySet(trySet, renderer, "Count", "42").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Optional", "7").ShouldBeTrue();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            InvokeTrySet(trySet, renderer, "Amount", "12,5").ShouldBeTrue();
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
        }

        object approved = Enum.Parse(assembly.GetType("RuntimeFixtures.MatrixStatus").ShouldNotBeNull(), "Approved");
        object pending = Enum.Parse(assembly.GetType("RuntimeFixtures.MatrixStatus").ShouldNotBeNull(), "Pending");
        InvokeTrySet(trySet, renderer, "Status", 0).ShouldBeTrue();
        model.GetType().GetProperty("Status").ShouldNotBeNull().GetValue(model).ShouldBe(pending);
        InvokeTrySet(trySet, renderer, "Status", "approved").ShouldBeTrue();
        Guid requestId = Guid.NewGuid();
        InvokeTrySet(trySet, renderer, "RequestId", requestId.ToString("D")).ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "OccurredAt", "2026-09-06T08:30:00+00:00").ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Optional", null).ShouldBeTrue();
        InvokeTrySet(trySet, renderer, "Custom", new ConversionProbe(returnNull: true)).ShouldBeTrue();

        model.GetType().GetProperty("MessageId").ShouldNotBeNull().GetValue(model).ShouldBe("exact");
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(42);
        model.GetType().GetProperty("Optional").ShouldNotBeNull().GetValue(model).ShouldBeNull();
        model.GetType().GetProperty("Amount").ShouldNotBeNull().GetValue(model).ShouldBe(12.5m);
        model.GetType().GetProperty("Status").ShouldNotBeNull().GetValue(model).ShouldBe(approved);
        model.GetType().GetProperty("RequestId").ShouldNotBeNull().GetValue(model).ShouldBe(requestId);
        model.GetType().GetProperty("OccurredAt").ShouldNotBeNull().GetValue(model)
            .ShouldBe(new DateTimeOffset(2026, 9, 6, 8, 30, 0, TimeSpan.Zero));
        PropertyInfo customProperty = model.GetType().GetProperty("Custom").ShouldNotBeNull();
        object defaultCustom = customProperty.GetValue(model).ShouldNotBeNull();
        defaultCustom.GetType().GetProperty("Value").ShouldNotBeNull().GetValue(defaultCustom).ShouldBe(0);

        object customSeven = Activator.CreateInstance(defaultCustom.GetType(), [7]).ShouldNotBeNull();
        InvokeTrySet(trySet, renderer, "Custom", customSeven).ShouldBeTrue();

        InvokeTrySet(trySet, renderer, "Count", "not-an-integer").ShouldBeFalse();
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(42);
        InvokeTrySet(trySet, renderer, "Count", long.MaxValue).ShouldBeFalse();
        InvokeTrySet(trySet, renderer, "Count", new object()).ShouldBeFalse();
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(42);
        InvokeTrySet(trySet, renderer, "Status", "not-a-status").ShouldBeFalse();
        model.GetType().GetProperty("Status").ShouldNotBeNull().GetValue(model).ShouldBe(approved);
        InvokeTrySet(trySet, renderer, "RequestId", "not-a-guid").ShouldBeFalse();
        model.GetType().GetProperty("RequestId").ShouldNotBeNull().GetValue(model).ShouldBe(requestId);
        InvokeTrySet(trySet, renderer, "OccurredAt", "not-a-date").ShouldBeFalse();
        model.GetType().GetProperty("OccurredAt").ShouldNotBeNull().GetValue(model)
            .ShouldBe(new DateTimeOffset(2026, 9, 6, 8, 30, 0, TimeSpan.Zero));
        InvokeTrySet(trySet, renderer, "Custom", new ConversionProbe(returnNull: false)).ShouldBeFalse();
        object unchangedCustom = customProperty.GetValue(model).ShouldNotBeNull();
        unchangedCustom.GetType().GetProperty("Value").ShouldNotBeNull().GetValue(unchangedCustom).ShouldBe(7);
        InvokeTrySet(trySet, renderer, "Unknown", 1).ShouldBeFalse();
        InvokeTrySet(trySet, renderer, "Fatal", 1).ShouldBeFalse();

        ResolvedDerivedValueProvider overflow = new(long.MaxValue);
        ResolvedDerivedValueProvider invalidCast = new(new object());
        ResolvedDerivedValueProvider valid = new("41");
        ResolvedDerivedValueProvider afterValid = new("99");
        SetProviders(rendererType, renderer, overflow, invalidCast, valid, afterValid);
        await InvokePrefillAsync(rendererType, renderer, "Count").ConfigureAwait(true);
        overflow.Calls.ShouldBe(1);
        invalidCast.Calls.ShouldBe(1);
        valid.Calls.ShouldBe(1);
        afterValid.Calls.ShouldBe(0);
        model.GetType().GetProperty("Count").ShouldNotBeNull().GetValue(model).ShouldBe(41);

        ResolvedDerivedValueProvider unsafeFirst = new(1);
        ResolvedDerivedValueProvider unsafeSecond = new(2);
        SetProviders(rendererType, renderer, unsafeFirst, unsafeSecond);
        await InvokePrefillAsync(rendererType, renderer, "Fatal").ConfigureAwait(true);
        unsafeFirst.Calls.ShouldBe(1);
        unsafeSecond.Calls.ShouldBe(1);
    }

    [Fact]
    public void GeneratedRenderer_PreservesNestedNullabilityAndWarningObsoleteTypedAssignmentUnderWarningsAsErrors() {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace WarningFixtures;

            [Command]
            public sealed class WarningCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)] public List<string?> Values { get; set; } = [];
                [DerivedFrom(DerivedFromSource.Context)] public string?[]? Names { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public string?[,][]? Matrix { get; set; }
                [Obsolete("warning only")]
                [DerivedFrom(DerivedFromSource.Context)] public int Legacy { get; set; }
                [Obsolete]
                [DerivedFrom(DerivedFromSource.Context)] public int LegacyWithoutMessage { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public int SetterFatal { get; [Obsolete("removed", true)] set; }
                [Experimental("EXP001")]
                [DerivedFrom(DerivedFromSource.Context)] public int ExperimentalProperty { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public int ExperimentalSetter { get; [Experimental("EXP002")] set; }
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        compilation = compilation.WithOptions(
            compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "WarningFixtures.WarningCommand.CommandRenderer.g.razor.cs");
        renderer.ShouldContain("#pragma warning disable CS0612, CS0618");
        renderer.ShouldContain("#pragma warning restore CS0612, CS0618");
        renderer.ShouldContain("TryConvertPropertyValue<global::System.Collections.Generic.List<global::System.String?>>");
        renderer.ShouldContain("TryConvertPropertyValue<global::System.String?[]?>");
        renderer.ShouldContain("TryConvertPropertyValue<global::System.String?[,][]?>");
        SliceCase(renderer, "Legacy").ShouldContain("_prefilledModel.@Legacy =");
        SliceCase(renderer, "LegacyWithoutMessage").ShouldContain("_prefilledModel.@LegacyWithoutMessage =");
        foreach (string propertyName in (string[])["SetterFatal", "ExperimentalProperty", "ExperimentalSetter"]) {
            SliceCase(renderer, propertyName).Trim().ShouldBe(
                $"case \"{propertyName}\":\n                return false;");
        }
    }

    [Fact]
    public void GeneratedRenderer_MetadataOnlyUnnameablePropertiesUseSoftFailArmsUnderWarningsAsErrors() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using MetadataFixtures;

            namespace MetadataFixtures;

            [Command]
            public sealed class MetadataCommand : MetadataCommandBase
            {
                public string Payload { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            source,
            additionalReferences: [MetadataAssignmentFixture.CreateReference()]);
        compilation = compilation.WithOptions(
            compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "MetadataFixtures.MetadataCommand.CommandRenderer.g.razor.cs");
        foreach (string propertyName in (string[])[
            MetadataAssignmentFixture.InvalidPropertyName,
            MetadataAssignmentFixture.InvalidTypePropertyName,
            MetadataAssignmentFixture.InvalidNamespacePropertyName,
            MetadataAssignmentFixture.NonSzArrayPropertyName,
        ]) {
            SliceCase(renderer, propertyName).Trim().ShouldBe(
                $"case \"{propertyName}\":\n                return false;");
        }
    }

    [Fact]
    public async Task GeneratedRenderer_UnsafeTypesUseSoftFailArmsAndKeywordMemberCompiles() {
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
                [DerivedFrom(DerivedFromSource.Context)] public int*[][] NestedPointerArray { get; set; } = [];
                [DerivedFrom(DerivedFromSource.Context)] public delegate*<void>[,][] NestedCallbackArray { get; set; } = null!;
                [Obsolete("removed", true)]
                [DerivedFrom(DerivedFromSource.Context)] public int Fatal { get; set; }
                [DerivedFrom(DerivedFromSource.Context)] public string @event { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, allowUnsafe: true);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "UnsafeFixtures.UnsafeCommand.CommandRenderer.g.razor.cs");
        foreach (string property in (string[])[
            "RefLike",
            "Pointer",
            "Callback",
            "PointerArray",
            "CallbackArray",
            "NestedPointerArray",
            "NestedCallbackArray",
            "Fatal",
        ]) {
            string arm = SliceCase(renderer, property);
            arm.Trim().ShouldBe($"case \"{property}\":\n                return false;");
        }

        renderer.ShouldContain("_prefilledModel.@event =");
        renderer.ShouldNotContain("GetProperty(");
        renderer.ShouldNotContain("SetValue(");
        renderer.ShouldNotContain("unsafe ");

        Assembly assembly = LoadAssembly(outputCompilation);
        Type rendererType = assembly.GetType("UnsafeFixtures.UnsafeCommandRenderer").ShouldNotBeNull();
        object rendererInstance = Activator.CreateInstance(rendererType).ShouldNotBeNull();
        MethodInfo trySet = rendererType.GetMethod("TrySetPropertyValue", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        object logger = Activator.CreateInstance(typeof(RecordingLogger<>).MakeGenericType(rendererType)).ShouldNotBeNull();
        rendererType.GetProperty("Logger", BindingFlags.Instance | BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .SetValue(rendererInstance, logger);
        IRecordingLogger recordingLogger = (IRecordingLogger)logger;

        string[] unsafeProperties = [
            "RefLike",
            "Pointer",
            "Callback",
            "PointerArray",
            "CallbackArray",
            "NestedPointerArray",
            "NestedCallbackArray",
            "Fatal",
        ];
        foreach (string property in unsafeProperties) {
            InvokeTrySet(trySet, rendererInstance, property, null).ShouldBeFalse(property);
            InvokeTrySet(trySet, rendererInstance, property, 1).ShouldBeFalse(property);

            foreach (object? value in (object?[])[null, 1]) {
                ResolvedDerivedValueProvider first = new(value);
                ResolvedDerivedValueProvider second = new(value);
                SetProviders(rendererType, rendererInstance, first, second);
                await InvokePrefillAsync(rendererType, rendererInstance, property).ConfigureAwait(true);
                first.Calls.ShouldBe(1, property);
                second.Calls.ShouldBe(1, property);
            }
        }

        recordingLogger.Warnings.Count.ShouldBe(unsafeProperties.Length * 4);
        recordingLogger.Warnings.ShouldAllBe(message => message.Contains("could not be assigned", StringComparison.Ordinal));
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
                public OnlyAlias::AliasLibrary.Token[,][] Tokens { get; set; } = null!;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: [aliasedReference]);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);

        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        string renderer = GetRenderer(result, "AliasFixtures.AliasCommand.CommandRenderer.g.razor.cs");
        renderer.ShouldContain("extern alias OnlyAlias;");
        renderer.ShouldContain("TryConvertPropertyValue<OnlyAlias::AliasLibrary.Token[,][]>");
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

    private static Assembly CompileGeneratedAssembly(string source, bool allowUnsafe = false) {
        string assemblyName = "RendererRuntime_" + Guid.NewGuid().ToString("N");
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            source,
            allowUnsafe: allowUnsafe,
            assemblyName: assemblyName);
        GeneratorDriverRunResult result = RunGenerator(compilation, out CSharpCompilation outputCompilation);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
        return LoadAssembly(outputCompilation);
    }

    private static Assembly LoadAssembly(CSharpCompilation outputCompilation) {
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

    private interface IRecordingLogger {
        IReadOnlyList<string> Warnings { get; }
    }

    private sealed class RecordingLogger<T> : ILogger<T>, IRecordingLogger {
        private readonly List<string> _warnings = [];

        public IReadOnlyList<string> Warnings => _warnings;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel == LogLevel.Warning) {
                _warnings.Add(formatter(state, exception));
            }
        }
    }
}

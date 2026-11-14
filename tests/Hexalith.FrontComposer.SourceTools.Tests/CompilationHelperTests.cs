using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests;

public sealed class CompilationHelperTests
{
    [Fact]
    public void BothOverloadsPreserveOrderedDuplicateReferencesAndSourcePaths()
    {
        CSharpCompilation single = CompilationHelper.CreateCompilation("public class First { }");
        CSharpCompilation multiple = CompilationHelper.CreateCompilation(["public class First { }", "public class Second { }"]);
        string[] first = Describe(single);
        Describe(multiple).ShouldBe(first);
        single.SyntaxTrees.Select(tree => tree.FilePath).ToArray().ShouldBe(["Test0.cs"]);
        multiple.SyntaxTrees.Select(tree => tree.FilePath).ToArray().ShouldBe(["Test0.cs", "Test1.cs"]);
        first.Length.ShouldBeGreaterThan(first.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AdditionalAliasedReferenceDoesNotLeakIntoLaterCompilations()
    {
        CSharpCompilation library = CompilationHelper.CreateCompilation("namespace AliasLibrary; public sealed class Token { }", assemblyName: "AliasLibrary");
        using MemoryStream bytes = new();
        library.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        PortableExecutableReference alias = MetadataReference.CreateFromImage(bytes.ToArray(),
            MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("OnlyAlias")));
        const string source = "extern alias OnlyAlias; public class Consumer { public OnlyAlias::AliasLibrary.Token? Value { get; set; } }";
        CSharpCompilation withAlias = CompilationHelper.CreateCompilation(source, additionalReferences: [alias]);
        withAlias.References.Last().ShouldBeSameAs(alias);
        withAlias.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        CSharpCompilation withAliasArray = CompilationHelper.CreateCompilation([source], additionalReferences: [alias]);
        withAliasArray.References.Last().ShouldBeSameAs(alias);
        Describe(withAliasArray).ShouldBe(Describe(withAlias));
        CSharpCompilation withoutAlias = CompilationHelper.CreateCompilation(source);
        withoutAlias.References.Any(reference => reference.Properties.Aliases.Contains("OnlyAlias")).ShouldBeFalse();
        withoutAlias.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(d => d.Id == "CS0430");
        withAlias.References.Count().ShouldBe(withoutAlias.References.Count() + 1);
    }

    [Fact]
    public void PerCompilationNullableUnsafeAndAssemblyOptionsRemainIndependent()
    {
        const string source = "public unsafe class Value { public int* Pointer; public string? Text; }";
        CSharpCompilation enabled = CompilationHelper.CreateCompilation(source, enableNullable: true, allowUnsafe: true, assemblyName: "Enabled");
        CSharpCompilation disabled = CompilationHelper.CreateCompilation([source], enableNullable: false, allowUnsafe: false, assemblyName: "Disabled");
        enabled.AssemblyName.ShouldBe("Enabled");
        disabled.AssemblyName.ShouldBe("Disabled");
        enabled.Options.NullableContextOptions.ShouldBe(NullableContextOptions.Enable);
        disabled.Options.NullableContextOptions.ShouldBe(NullableContextOptions.Disable);
        enabled.Options.AllowUnsafe.ShouldBeTrue();
        disabled.Options.AllowUnsafe.ShouldBeFalse();
        enabled.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        disabled.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(d => d.Id == "CS0227");
        disabled.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(d => d.Id == "CS8632");
    }

    private static string[] Describe(CSharpCompilation compilation) => compilation.References
        .Select(reference => string.Join("|", reference.Display, reference.Properties.Kind, reference.Properties.EmbedInteropTypes, string.Join(",", reference.Properties.Aliases))).ToArray();
}

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public class AttributeParserTests {
    [Fact]
    public async Task Parse_BasicProjection_ProducesCorrectIR()
        => await VerifyProjectionAsync(TestSources.BasicProjection, "TestDomain.CounterProjection");

    [Fact]
    public async Task Parse_AllFieldTypesProjection_Covers29Types()
        => await VerifyProjectionAsync(TestSources.AllFieldTypesProjection, "TestDomain.AllFieldTypesProjection");

    [Fact]
    public async Task Parse_RecordProjection_ProducesCorrectIR()
        => await VerifyProjectionAsync(TestSources.RecordProjection, "TestDomain.RecordProjection");

    [Fact]
    public async Task Parse_BadgeMappingProjection_ExtractsBadgeSlots()
        => await VerifyProjectionAsync(TestSources.BadgeMappingProjection, "TestDomain.BadgeMappingProjection");

    [Fact]
    public async Task Parse_MultiAttributeProjection_ExtractsBoundedContextAndRole()
        => await VerifyProjectionAsync(TestSources.MultiAttributeProjection, "TestDomain.MultiAttributeProjection");

    [Fact]
    public async Task Parse_GlobalNamespaceProjection_HandlesEmptyNamespace()
        => await VerifyProjectionAsync(TestSources.GlobalNamespaceProjection, "GlobalProjection");

    [Fact]
    public void Parse_DisplayLabelProjection_ExtractsDisplayLabel() {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.DisplayLabelProjection, "TestDomain.DisplayLabelProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.BoundedContext.ShouldBe("Orders");
        result.Model.BoundedContextDisplayLabel.ShouldBe("Commandes");
    }

    [Fact]
    public void Parse_BasicProjection_DisplayLabelIsNull() {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.BasicProjection, "TestDomain.CounterProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.BoundedContextDisplayLabel.ShouldBeNull();
    }

    // Diagnostic negative-path tests

    [Fact]
    public void Parse_UnsupportedFieldType_EmitsHFC1002() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.UnsupportedFieldProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic[] hfc1002Diagnostics = result.Diagnostics.Where(d => d.Id == "HFC1002").ToArray();
        hfc1002Diagnostics.Length.ShouldBeGreaterThanOrEqualTo(4,
            "Expected HFC1002 for byte[], Dictionary, object, and tuple");
    }

    [Fact]
    public void Parse_PropertySuppressMessage_SuppressesOnlyTargetedHFC1002() {
        const string source = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            public partial class SuppressedFixtureProjection
            {
                [SuppressMessage(
                    "HexalithFrontComposer",
                    "HFC1002:Unsupported field type",
                    Justification = "Approved exact fixture property.")]
                public Dictionary<string, string> ApprovedFixture { get; set; } = new();

                public object UnapprovedFixture { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic[] diagnostics = driver.GetRunResult().Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1002").ToArray();

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("UnapprovedFixture");
        diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldNotContain("'ApprovedFixture'");
    }

    [Fact]
    public void Parse_PropertySuppressMessageForOtherCheckId_StillReportsHFC1002() {
        // Pins the checkId comparison in HasSuppressMessage. Without a property that carries a
        // real [SuppressMessage] for a *different* rule, deleting that comparison so any
        // SuppressMessageAttribute suppresses HFC1002 would leave every other test green.
        const string source = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            public partial class OtherCheckIdFixtureProjection
            {
                [SuppressMessage(
                    "Style",
                    "IDE0057:Use range operator",
                    Justification = "Unrelated rule; must not suppress HFC1002.")]
                public Dictionary<string, string> OtherRuleSuppressed { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic[] diagnostics = driver.GetRunResult().Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1002").ToArray();

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("OtherRuleSuppressed");
    }

    [Fact]
    public void Parse_PropertyUnconditionalSuppressMessageForHfc1002_SuppressesDiagnostic() {
        const string source = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            public partial class UnconditionalHfc1002FixtureProjection
            {
                [UnconditionalSuppressMessage(
                    "HexalithFrontComposer",
                    "HFC1002:Unsupported field type",
                    Justification = "Approved unconditional fixture suppression.")]
                public Dictionary<string, string> UnconditionallySuppressed { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic[] diagnostics = driver.GetRunResult().Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1002").ToArray();

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_PropertyUnconditionalSuppressMessageForOtherCheckId_StillReportsHFC1002() {
        // Mirrors Parse_PropertySuppressMessageForOtherCheckId_StillReportsHFC1002 for the
        // UnconditionalSuppressMessage branch of HasSuppressMessage checkId comparison.
        const string source = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            public partial class UnconditionalOtherCheckIdFixtureProjection
            {
                [UnconditionalSuppressMessage(
                    "Style",
                    "IDE0057:Use range operator",
                    Justification = "Unrelated rule; must not suppress HFC1002.")]
                public Dictionary<string, string> OtherRuleUnconditionallySuppressed { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic[] diagnostics = driver.GetRunResult().Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1002").ToArray();

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .ShouldContain("OtherRuleUnconditionallySuppressed");
    }

    [Fact]
    public void Parse_TypeLevelSuppressMessageForHfc1002_StillReportsHFC1002() {
        const string source = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            [SuppressMessage(
                "HexalithFrontComposer",
                "HFC1002:Unsupported field type",
                Justification = "Type-level suppression must not silence property HFC1002.")]
            public partial class TypeLevelHfc1002FixtureProjection
            {
                public Dictionary<string, string> StillReported { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic[] diagnostics = driver.GetRunResult().Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1002").ToArray();

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("StillReported");
    }

    [Fact]
    public void Parse_NonPartialProjection_EmitsHFC1003() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonPartialProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1003");
    }

    [Fact]
    public void Parse_StructProjection_EmitsHFC1004() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.StructProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_RecordStructProjection_EmitsHFC1004() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.RecordStructProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_GenericProjection_EmitsHFC1004() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.GenericProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_AbstractProjection_EmitsHFC1004() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.AbstractProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_NestedInNonPartialProjection_EmitsHFC1004() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NestedInNonPartialProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_NullBoundedContext_EmitsHFC1005() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NullBoundedContextProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1005");
    }

    [Fact]
    public void Parse_InvalidProjectionRole_EmitsHFC1024() {
        // Story 4-1 D15 / AC7 — an unsafe cast of an out-of-range value to ProjectionRole
        // now emits HFC1024 (Unknown ProjectionRole value) — split from the old HFC1005
        // trash-can code per H6. Renderer falls back to Default rendering.
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.InvalidProjectionRoleProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1024");
    }

    [Fact]
    public void Parse_InvalidBadgeSlot_EmitsHFC1005() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.InvalidBadgeSlotProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1005");
    }

    [Fact]
    public void Parse_NullableContextDisabled_TreatsReferenceTypesAsNullable() {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.NullableContextDisabledProjection,
            "TestDomain.NullableContextDisabledProjection",
            enableNullable: false);

        PropertyModel nameProperty = result.Model!.Properties.AsImmutableArray().Single(p => p.Name == "Name");
        nameProperty.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnsupportedFieldProjection_ProducesPartialIR() {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.UnsupportedFieldProjection, "TestDomain.UnsupportedFieldProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.Properties.AsImmutableArray().Any(p => p.IsUnsupported).ShouldBeTrue();
        result.Diagnostics.AsImmutableArray().Any(d => d.Id == "HFC1002").ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnsupportedFieldType_DiagnosticsHaveSourceLocation() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.UnsupportedFieldProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic diagnostic = result.Diagnostics.First(d => d.Id == "HFC1002");
        diagnostic.Location.ShouldNotBe(Location.None);
        diagnostic.Location.GetLineSpan().Path.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Parse_NonIntEnumProjection_EmitsHFC1002() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonIntEnumProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1002");
    }

    [Fact]
    public void Parse_CompoundUnsupportedTypes_EmitsHFC1002PerField() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = @"
using System;
using System.Collections.Generic;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial class CompoundTypeProjection
{
    public byte[] ByteArray { get; set; } = Array.Empty<byte>();
    public (int X, int Y) TupleField { get; set; }
    public Dictionary<string, int> DictField { get; set; } = new();
    public object ObjectField { get; set; } = new();
}";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic[] hfc1002 = result.Diagnostics.Where(d => d.Id == "HFC1002").ToArray();
        hfc1002.Length.ShouldBe(4, "Expected one HFC1002 per unsupported field (byte[], tuple, Dictionary, object)");
    }

    [Fact]
    public void Parse_NestedGenericArray_PreservesSourceReadyTypeSyntax() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TypeFixtures;

            public sealed class Outer<T>
            {
                public sealed class Inner<TValue> { }
            }

            [Projection]
            public sealed class TypeSyntaxProjection
            {
                public Outer<int>.Inner<string>[,][] Values { get; set; } = [];
            }
            """;

        PropertyModel property = CompilationHelper
            .ParseProjection(source, "TypeFixtures.TypeSyntaxProjection")
            .Model.ShouldNotBeNull()
            .Properties.Single();

        property.SourceTypeName.ShouldBe(
            "global::TypeFixtures.Outer<global::System.Int32>.Inner<global::System.String>[,][]");
        property.RequiredExternAliases.Count.ShouldBe(0);
        property.SupportsStaticAssignment.ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnsafeAndErrorObsoleteProperties_ClassifiesStaticReferenceSafety() {
        const string source = """
            using System;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TypeFixtures;

            [Projection]
            public unsafe sealed class StaticSafetyProjection
            {
                public Span<int> RefLike { get => default; set { } }
                public int* Pointer { get; set; }
                public delegate*<int, int> FunctionPointer { get; set; }
                public int*[] PointerArray { get; set; } = [];
                public delegate*<int, int>[] FunctionPointerArray { get; set; } = [];

                [Obsolete("removed", true)]
                public int ErrorObsolete { get; set; }

                [Obsolete("warning only")]
                public int WarningObsolete { get; set; }
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, allowUnsafe: true);

        ParseResult result = CompilationHelper.ParseProjection(compilation, "TypeFixtures.StaticSafetyProjection");
        DomainModel model = result.Model.ShouldNotBeNull();

        foreach (string propertyName in new[] {
            "RefLike",
            "Pointer",
            "FunctionPointer",
            "PointerArray",
            "FunctionPointerArray",
            "ErrorObsolete",
        }) {
            model.Properties.Single(property => property.Name == propertyName)
                .SupportsStaticAssignment.ShouldBeFalse(propertyName);
        }

        model.Properties.Single(property => property.Name == "WarningObsolete")
            .SupportsStaticAssignment.ShouldBeTrue();
        model.Properties.Single(property => property.Name == "PointerArray")
            .SourceTypeName.ShouldBe("global::System.Int32*[]");
        model.Properties.Single(property => property.Name == "FunctionPointer")
            .SourceTypeName.ShouldContain("delegate*");
    }

    [Fact]
    public void Parse_AliasOnlyProperty_PreservesAliasQualifiedSyntaxAndProvenance() {
        const string aliasSource = "namespace AliasLibrary; public sealed class Token { }";
        CSharpCompilation aliasCompilation = CompilationHelper.CreateCompilation(aliasSource, assemblyName: "AliasLibrary");
        using MemoryStream stream = new();
        aliasCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        MetadataReference aliasReference = MetadataReference.CreateFromImage(
            stream.ToArray(),
            properties: MetadataReferenceProperties.Assembly.WithAliases(
                System.Collections.Immutable.ImmutableArray.Create("OnlyAlias")));
        const string source = """
            extern alias OnlyAlias;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace AliasFixtures;

            [Projection]
            public sealed class AliasProjection
            {
                public OnlyAlias::AliasLibrary.Token Value { get; set; } = new();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            source,
            additionalReferences: [aliasReference]);

        PropertyModel property = CompilationHelper.ParseProjection(compilation, "AliasFixtures.AliasProjection")
            .Model.ShouldNotBeNull()
            .Properties.Single();

        property.SourceTypeName.ShouldBe("OnlyAlias::AliasLibrary.Token");
        property.RequiredExternAliases.ToArray().ShouldBe(["OnlyAlias"]);
    }

    [Fact]
    public void Parse_NestedNullableReferences_PreserveSourceAnnotations() {
        const string source = """
            using System.Collections.Generic;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TypeFixtures;

            [Projection]
            public sealed class NullableSyntaxProjection
            {
                public List<string?> Values { get; set; } = [];
                public string?[]? Names { get; set; }
                public string?[,][]? Matrix { get; set; }
            }
            """;

        DomainModel model = CompilationHelper.ParseProjection(source, "TypeFixtures.NullableSyntaxProjection")
            .Model.ShouldNotBeNull();

        model.Properties.Single(property => property.Name == "Values").SourceTypeName
            .ShouldBe("global::System.Collections.Generic.List<global::System.String?>");
        model.Properties.Single(property => property.Name == "Names").SourceTypeName
            .ShouldBe("global::System.String?[]?");
        model.Properties.Single(property => property.Name == "Matrix").SourceTypeName
            .ShouldBe("global::System.String?[,][]?");
    }

    [Fact]
    public void Parse_BlockingMemberAttributes_ClassifyOnlyCompilerUnreferenceableMembersAsUnsafe() {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TypeFixtures;

            [Projection]
            public sealed class BlockingAttributeProjection
            {
                [Obsolete("warning only")]
                public int WarningObsolete { get; set; }

                public int SetterErrorObsolete { get; [Obsolete("removed", true)] set; }

                [Experimental("EXP001")]
                public int ExperimentalProperty { get; set; }

                public int ExperimentalSetter { get; [Experimental("EXP002")] set; }
            }
            """;

        DomainModel model = CompilationHelper.ParseProjection(source, "TypeFixtures.BlockingAttributeProjection")
            .Model.ShouldNotBeNull();

        model.Properties.Single(property => property.Name == "WarningObsolete")
            .SupportsStaticAssignment.ShouldBeTrue();
        foreach (string propertyName in (string[])["SetterErrorObsolete", "ExperimentalProperty", "ExperimentalSetter"]) {
            model.Properties.Single(property => property.Name == propertyName)
                .SupportsStaticAssignment.ShouldBeFalse(propertyName);
        }
    }

    [Fact]
    public void Parse_MetadataOnlyUnnameableMembers_ClassifyStaticAssignmentAsUnsafe() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using MetadataFixtures;

            namespace TypeFixtures;

            [Command]
            public sealed class MetadataCommand : MetadataCommandBase
            {
                public string Payload { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            source,
            additionalReferences: [MetadataAssignmentFixture.CreateReference()]);

        CommandModel model = CompilationHelper.ParseCommand(compilation, "TypeFixtures.MetadataCommand")
            .Model.ShouldNotBeNull();

        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty();
        IPropertySymbol nonSzArrayProperty = compilation.GetTypeByMetadataName("MetadataFixtures.MetadataCommandBase")
            .ShouldNotBeNull()
            .GetMembers(MetadataAssignmentFixture.NonSzArrayPropertyName)
            .OfType<IPropertySymbol>()
            .Single();
        ((IArrayTypeSymbol)nonSzArrayProperty.Type).IsSZArray.ShouldBeFalse();

        foreach (string propertyName in (string[])[
            MetadataAssignmentFixture.InvalidPropertyName,
            MetadataAssignmentFixture.InvalidTypePropertyName,
            MetadataAssignmentFixture.InvalidNamespacePropertyName,
            MetadataAssignmentFixture.NonSzArrayPropertyName,
        ]) {
            model.DerivableProperties.Single(property => property.Name == propertyName)
                .SupportsStaticAssignment.ShouldBeFalse(propertyName);
        }
    }

    private static async Task VerifyProjectionAsync(string source, string metadataName) {
        ParseResult result = CompilationHelper.ParseProjection(source, metadataName);
        result.Diagnostics.Count.ShouldBe(0);
        _ = result.Model.ShouldNotBeNull();

        _ = await Verifier.Verify(result.Model)
            .UseDirectory("../Snapshots");
    }
}

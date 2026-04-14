namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

using System.Linq;
using System.Threading;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using VerifyXunit;

using Xunit;

public class AttributeParserTests
{
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
    public void Parse_DisplayLabelProjection_ExtractsDisplayLabel()
    {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.DisplayLabelProjection, "TestDomain.DisplayLabelProjection");

        result.Model.ShouldNotBeNull();
        result.Model.BoundedContext.ShouldBe("Orders");
        result.Model.BoundedContextDisplayLabel.ShouldBe("Commandes");
    }

    [Fact]
    public void Parse_BasicProjection_DisplayLabelIsNull()
    {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.BasicProjection, "TestDomain.CounterProjection");

        result.Model.ShouldNotBeNull();
        result.Model.BoundedContextDisplayLabel.ShouldBeNull();
    }

    // Diagnostic negative-path tests

    [Fact]
    public void Parse_UnsupportedFieldType_EmitsHFC1002()
    {
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
    public void Parse_NonPartialProjection_EmitsHFC1003()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonPartialProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1003");
    }

    [Fact]
    public void Parse_StructProjection_EmitsHFC1004()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.StructProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_RecordStructProjection_EmitsHFC1004()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.RecordStructProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_GenericProjection_EmitsHFC1004()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.GenericProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_AbstractProjection_EmitsHFC1004()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.AbstractProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_NestedInNonPartialProjection_EmitsHFC1004()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NestedInNonPartialProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1004");
    }

    [Fact]
    public void Parse_NullBoundedContext_EmitsHFC1005()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NullBoundedContextProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1005");
    }

    [Fact]
    public void Parse_InvalidProjectionRole_EmitsHFC1005()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.InvalidProjectionRoleProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1005");
    }

    [Fact]
    public void Parse_InvalidBadgeSlot_EmitsHFC1005()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.InvalidBadgeSlotProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1005");
    }

    [Fact]
    public void Parse_NullableContextDisabled_TreatsReferenceTypesAsNullable()
    {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.NullableContextDisabledProjection,
            "TestDomain.NullableContextDisabledProjection",
            enableNullable: false);

        PropertyModel nameProperty = result.Model!.Properties.AsImmutableArray().Single(p => p.Name == "Name");
        nameProperty.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnsupportedFieldProjection_ProducesPartialIR()
    {
        ParseResult result = CompilationHelper.ParseProjection(TestSources.UnsupportedFieldProjection, "TestDomain.UnsupportedFieldProjection");

        result.Model.ShouldNotBeNull();
        result.Model.Properties.AsImmutableArray().Any(p => p.IsUnsupported).ShouldBeTrue();
        result.Diagnostics.AsImmutableArray().Any(d => d.Id == "HFC1002").ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnsupportedFieldType_DiagnosticsHaveSourceLocation()
    {
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
    public void Parse_NonIntEnumProjection_EmitsHFC1002()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonIntEnumProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.ShouldContain(d => d.Id == "HFC1002");
    }

    [Fact]
    public void Parse_CompoundUnsupportedTypes_EmitsHFC1002PerField()
    {
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

    private static async Task VerifyProjectionAsync(string source, string metadataName)
    {
        ParseResult result = CompilationHelper.ParseProjection(source, metadataName);
        result.Diagnostics.Count.ShouldBe(0);
        result.Model.ShouldNotBeNull();

        await Verifier.Verify(result.Model)
            .UseDirectory("../Snapshots");
    }
}

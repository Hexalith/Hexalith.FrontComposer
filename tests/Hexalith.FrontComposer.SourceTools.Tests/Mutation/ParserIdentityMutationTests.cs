using System.Reflection;
using System.Reflection.Emit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks renderer-reference and imported property identity decisions.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ParserIdentityMutationTests
{
    /// <summary>Checks the compiler's dynamic symbol remains available for typed assignment.</summary>
    [Fact]
    public void DynamicCommandPropertyRetainsItsObjectAssignmentType()
    {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            [Command]
            public sealed class UpdateCommand
            {
                public string MessageId { get; set; } = string.Empty;
                [DerivedFrom(DerivedFromSource.Context)]
                public dynamic Payload { get; set; } = new object();
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        INamedTypeSymbol symbol = compilation.GetTypeByMetadataName("Fixtures.UpdateCommand").ShouldNotBeNull();
        symbol.GetMembers("Payload").OfType<IPropertySymbol>().Single().Type.TypeKind.ShouldBe(TypeKind.Dynamic);

        CommandParseResult result = CompilationHelper.ParseCommand(compilation, "Fixtures.UpdateCommand");
        PropertyModel property = result.Model.ShouldNotBeNull().DerivableProperties.Single(item => item.Name == "Payload");

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "HFC1002");
        property.SupportsStaticAssignment.ShouldBeTrue();
        property.SourceTypeName.ShouldBe("global::System.Object");
        string renderer = GenerateRenderer(compilation);
        renderer.ShouldContain("TryConvertPropertyValue<global::System.Object>");
        renderer.ShouldContain("_prefilledModel.@Payload = convertedValue");
    }

    /// <summary>Checks that a metadata override takes precedence over an intervening shadow.</summary>
    [Fact]
    public void ImportedExplicitOverridePrecedesTheShadowedProperty()
    {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            [Command]
            public sealed class LastCommand : ImportedFixtures.Leaf
            {
                public string MessageId { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: [CreateRouteReference()]);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        INamedTypeSymbol leaf = compilation.GetTypeByMetadataName("ImportedFixtures.Leaf").ShouldNotBeNull();
        IPropertySymbol declared = leaf.GetMembers("Route").OfType<IPropertySymbol>().Single();
        declared.OverriddenProperty.ShouldNotBeNull().ContainingType.Name.ShouldBe("Root");
        leaf.BaseType.ShouldNotBeNull().GetMembers("Route").OfType<IPropertySymbol>().Single()
            .GetAttributes().ShouldContain(attribute => attribute.AttributeClass!.Name == nameof(DerivedFromAttribute));

        CommandModel model = CompilationHelper.ParseCommand(compilation, "Fixtures.LastCommand").Model.ShouldNotBeNull();
        model.DerivableProperties.ShouldNotContain(property => property.Name == "Route");
        model.NonDerivableProperties.Single(property => property.Name == "Route").TypeName.ShouldBe("String");
        GenerateRenderer(compilation).ShouldNotContain("_prefilledModel.@Route = ");

        CSharpCompilation rootHasAttribute = CompilationHelper.CreateCompilation(source, additionalReferences: [CreateRouteReference(rootIsDerived: true)]);
        CompilationHelper.ParseCommand(rootHasAttribute, "Fixtures.LastCommand").Model.ShouldNotBeNull()
            .DerivableProperties.ShouldContain(property => property.Name == "Route");
        GenerateRenderer(rootHasAttribute).ShouldContain("_prefilledModel.@Route = convertedValue");
    }

    private static string GenerateRenderer(CSharpCompilation compilation)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _, cancellationToken);
        output.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return driver.GetRunResult().Results.Single().GeneratedSources
            .Single(item => item.HintName.EndsWith("Renderer.g.razor.cs", StringComparison.Ordinal)).SourceText.ToString();
    }

    private static PortableExecutableReference CreateRouteReference(bool rootIsDerived = false)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName("RouteOverrideFixtures"), typeof(object).Assembly);
        ModuleBuilder module = assembly.DefineDynamicModule("Routes");
        TypeBuilder root = module.DefineType("ImportedFixtures.Root", TypeAttributes.Public);
        (MethodBuilder rootGetter, MethodBuilder rootSetter) = DefineRoute(root, typeof(string), derived: rootIsDerived);
        TypeBuilder middle = module.DefineType("ImportedFixtures.Middle", TypeAttributes.Public, root.CreateType());
        _ = DefineRoute(middle, typeof(int), derived: true);
        TypeBuilder leaf = module.DefineType("ImportedFixtures.Leaf", TypeAttributes.Public, middle.CreateType());
        (MethodBuilder leafGetter, MethodBuilder leafSetter) = DefineRoute(leaf, typeof(string));

        // MethodImpl metadata can name a base slot past the intervening shadow.
        leaf.DefineMethodOverride(leafGetter, rootGetter);
        leaf.DefineMethodOverride(leafSetter, rootSetter);
        _ = leaf.CreateType();

        using MemoryStream image = new();
        assembly.Save(image);
        return MetadataReference.CreateFromImage(image.ToArray());
    }

    private static (MethodBuilder Getter, MethodBuilder Setter) DefineRoute(TypeBuilder type, Type propertyType, bool derived = false)
    {
        const MethodAttributes flags = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
            | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        PropertyBuilder property = type.DefineProperty("Route", PropertyAttributes.None, propertyType, Type.EmptyTypes);
        MethodBuilder getter = type.DefineMethod("get_Route", flags, propertyType, Type.EmptyTypes);
        ILGenerator getterBody = getter.GetILGenerator();
        getterBody.Emit(propertyType == typeof(int) ? OpCodes.Ldc_I4_0 : OpCodes.Ldnull);
        getterBody.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        MethodBuilder setter = type.DefineMethod("set_Route", flags, typeof(void), [propertyType]);
        setter.GetILGenerator().Emit(OpCodes.Ret);
        property.SetSetMethod(setter);

        if (derived)
        {
            property.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(DerivedFromAttribute).GetConstructor([typeof(DerivedFromSource)])!, [DerivedFromSource.Context]));
        }

        return (getter, setter);
    }
}

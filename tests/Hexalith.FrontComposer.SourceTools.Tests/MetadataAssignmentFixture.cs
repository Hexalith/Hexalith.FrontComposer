using System.Reflection;
using System.Reflection.Emit;

using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Tests;

internal static class MetadataAssignmentFixture {
    internal const string InvalidPropertyName = "Bad-Property";
    internal const string InvalidTypePropertyName = "InvalidType";
    internal const string NonSzArrayPropertyName = "NonSzArray";

    internal static PortableExecutableReference CreateReference() {
        PersistedAssemblyBuilder assembly = new(
            new AssemblyName("CompilerUnreferenceableMetadata"),
            typeof(object).Assembly);
        ModuleBuilder module = assembly.DefineDynamicModule("CompilerUnreferenceableMetadata");
        TypeBuilder invalidTypeBuilder = module.DefineType(
            "Bad-Namespace.Bad-Type",
            TypeAttributes.Public | TypeAttributes.Class);
        _ = invalidTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        Type invalidType = invalidTypeBuilder.CreateType();

        TypeBuilder baseTypeBuilder = module.DefineType(
            "MetadataFixtures.MetadataCommandBase",
            TypeAttributes.Public | TypeAttributes.Class);
        _ = baseTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        DefineProperty(baseTypeBuilder, "MessageId", typeof(string), isDerivable: false);
        DefineProperty(baseTypeBuilder, InvalidPropertyName, typeof(int), isDerivable: true);
        DefineProperty(baseTypeBuilder, InvalidTypePropertyName, invalidType, isDerivable: true);
        DefineProperty(baseTypeBuilder, NonSzArrayPropertyName, typeof(int).MakeArrayType(1), isDerivable: true);
        _ = baseTypeBuilder.CreateType();

        using MemoryStream stream = new();
        assembly.Save(stream);
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static void DefineProperty(
        TypeBuilder containingType,
        string name,
        Type propertyType,
        bool isDerivable) {
        const MethodAttributes AccessorAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
        PropertyBuilder property = containingType.DefineProperty(name, PropertyAttributes.None, propertyType, null);
        MethodBuilder getter = containingType.DefineMethod("get_" + name, AccessorAttributes, propertyType, Type.EmptyTypes);
        ILGenerator getterIl = getter.GetILGenerator();
        if (propertyType == typeof(int)) {
            getterIl.Emit(OpCodes.Ldc_I4_0);
        }
        else {
            getterIl.Emit(OpCodes.Ldnull);
        }

        getterIl.Emit(OpCodes.Ret);
        MethodBuilder setter = containingType.DefineMethod("set_" + name, AccessorAttributes, null, [propertyType]);
        setter.GetILGenerator().Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        property.SetSetMethod(setter);

        if (isDerivable) {
            ConstructorInfo constructor = typeof(DerivedFromAttribute).GetConstructor([typeof(DerivedFromSource)])!;
            property.SetCustomAttribute(new CustomAttributeBuilder(constructor, [DerivedFromSource.Context]));
        }
    }
}

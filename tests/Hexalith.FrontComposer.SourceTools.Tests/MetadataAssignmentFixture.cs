using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Tests;

internal static class MetadataAssignmentFixture {
    internal const string InvalidPropertyName = "Bad-Property";
    internal const string InvalidTypePropertyName = "InvalidType";
    internal const string InvalidNamespacePropertyName = "InvalidNamespace";
    internal const string NonSzArrayPropertyName = "NonSzArray";

    internal static PortableExecutableReference CreateReference() {
        MetadataBuilder metadata = new();
        BlobBuilder ilStream = new();
        MethodBodyStreamEncoder methodBodies = new(ilStream);

        _ = metadata.AddModule(
            0,
            metadata.GetOrAddString("CompilerUnreferenceableMetadata.dll"),
            metadata.GetOrAddGuid(Guid.NewGuid()),
            default,
            default);
        _ = metadata.AddAssembly(
            metadata.GetOrAddString("CompilerUnreferenceableMetadata"),
            new Version(1, 0, 0, 0),
            default,
            default,
            default,
            AssemblyHashAlgorithm.None);

        AssemblyReferenceHandle coreAssembly = AddAssemblyReference(metadata, typeof(object).Assembly.GetName());
        AssemblyReferenceHandle contractsAssembly = AddAssemblyReference(metadata, typeof(DerivedFromAttribute).Assembly.GetName());
        TypeReferenceHandle objectType = metadata.AddTypeReference(
            coreAssembly,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Object"));
        TypeReferenceHandle derivedFromAttributeType = metadata.AddTypeReference(
            contractsAssembly,
            metadata.GetOrAddString("Hexalith.FrontComposer.Contracts.Attributes"),
            metadata.GetOrAddString(nameof(DerivedFromAttribute)));
        TypeReferenceHandle derivedFromSourceType = metadata.AddTypeReference(
            contractsAssembly,
            metadata.GetOrAddString("Hexalith.FrontComposer.Contracts.Attributes"),
            metadata.GetOrAddString(nameof(DerivedFromSource)));

        MemberReferenceHandle objectConstructor = metadata.AddMemberReference(
            objectType,
            metadata.GetOrAddString(".ctor"),
            AddMethodSignature(metadata, 0, static returnType => returnType.Void(), static _ => { }));
        MemberReferenceHandle derivedFromConstructor = metadata.AddMemberReference(
            derivedFromAttributeType,
            metadata.GetOrAddString(".ctor"),
            AddMethodSignature(
                metadata,
                1,
                static returnType => returnType.Void(),
                parameters => parameters.AddParameter().Type(isByRef: false).Type(derivedFromSourceType, isValueType: true)));

        TypeDefinitionHandle invalidTypeName = MetadataTokens.TypeDefinitionHandle(2);
        TypeDefinitionHandle invalidNamespaceType = MetadataTokens.TypeDefinitionHandle(3);
        TypeDefinitionHandle baseType = MetadataTokens.TypeDefinitionHandle(4);
        MethodDefinitionHandle firstMethod = MetadataTokens.MethodDefinitionHandle(1);
        FieldDefinitionHandle firstField = MetadataTokens.FieldDefinitionHandle(1);
        _ = metadata.AddTypeDefinition(
            TypeAttributes.NotPublic,
            default,
            metadata.GetOrAddString("<Module>"),
            default,
            firstField,
            firstMethod);
        _ = metadata.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString("MetadataFixtures"),
            metadata.GetOrAddString("Bad-Type"),
            objectType,
            firstField,
            firstMethod);
        _ = metadata.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString("Bad-Namespace"),
            metadata.GetOrAddString("ValidType"),
            objectType,
            firstField,
            firstMethod);
        _ = metadata.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString("MetadataFixtures"),
            metadata.GetOrAddString("MetadataCommandBase"),
            objectType,
            firstField,
            firstMethod);

        InstructionEncoder constructorIl = new(new BlobBuilder());
        constructorIl.LoadArgument(0);
        constructorIl.Call(objectConstructor);
        constructorIl.OpCode(ILOpCode.Ret);
        _ = metadata.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            MethodImplAttributes.IL,
            metadata.GetOrAddString(".ctor"),
            AddMethodSignature(metadata, 0, static returnType => returnType.Void(), static _ => { }),
            methodBodies.AddMethodBody(constructorIl),
            MetadataTokens.ParameterHandle(1));

        PropertyDefinitionHandle firstProperty = default;
        AddProperty(metadata, methodBodies, "MessageId", static type => type.String(), false, derivedFromConstructor, ref firstProperty);
        AddProperty(metadata, methodBodies, InvalidPropertyName, static type => type.Int32(), true, derivedFromConstructor, ref firstProperty);
        AddProperty(metadata, methodBodies, InvalidTypePropertyName, type => type.Type(invalidTypeName, isValueType: false), true, derivedFromConstructor, ref firstProperty);
        AddProperty(metadata, methodBodies, InvalidNamespacePropertyName, type => type.Type(invalidNamespaceType, isValueType: false), true, derivedFromConstructor, ref firstProperty);
        AddProperty(
            metadata,
            methodBodies,
            NonSzArrayPropertyName,
            static type => type.Array(
                static elementType => elementType.Int32(),
                static shape => shape.Shape(1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty)),
            true,
            derivedFromConstructor,
            ref firstProperty);
        metadata.AddPropertyMap(baseType, firstProperty);

        ManagedPEBuilder peBuilder = new(
            PEHeaderBuilder.CreateLibraryHeader(),
            new MetadataRootBuilder(metadata),
            ilStream);
        BlobBuilder peImage = new();
        _ = peBuilder.Serialize(peImage);
        return MetadataReference.CreateFromImage(peImage.ToImmutableArray());
    }

    private static AssemblyReferenceHandle AddAssemblyReference(MetadataBuilder metadata, AssemblyName assemblyName)
        => metadata.AddAssemblyReference(
            metadata.GetOrAddString(assemblyName.Name!),
            assemblyName.Version ?? new Version(0, 0, 0, 0),
            string.IsNullOrEmpty(assemblyName.CultureName) ? default : metadata.GetOrAddString(assemblyName.CultureName),
            metadata.GetOrAddBlob(assemblyName.GetPublicKeyToken() ?? []),
            default,
            default);

    private static BlobHandle AddMethodSignature(
        MetadataBuilder metadata,
        int parameterCount,
        Action<ReturnTypeEncoder> encodeReturnType,
        Action<ParametersEncoder> encodeParameters) {
        BlobBuilder signature = new();
        new BlobEncoder(signature)
            .MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: true)
            .Parameters(parameterCount, encodeReturnType, encodeParameters);
        return metadata.GetOrAddBlob(signature);
    }

    private static void AddProperty(
        MetadataBuilder metadata,
        MethodBodyStreamEncoder methodBodies,
        string name,
        Action<SignatureTypeEncoder> encodeType,
        bool isDerivable,
        MemberReferenceHandle derivedFromConstructor,
        ref PropertyDefinitionHandle firstProperty) {
        BlobBuilder propertySignature = new();
        new BlobEncoder(propertySignature)
            .PropertySignature(isInstanceProperty: true)
            .Parameters(0, returnType => encodeType(returnType.Type(isByRef: false)), static _ => { });
        PropertyDefinitionHandle property = metadata.AddProperty(
            PropertyAttributes.None,
            metadata.GetOrAddString(name),
            metadata.GetOrAddBlob(propertySignature));
        if (firstProperty.IsNil) {
            firstProperty = property;
        }

        InstructionEncoder getterIl = new(new BlobBuilder());
        if (string.Equals(name, InvalidPropertyName, StringComparison.Ordinal)) {
            getterIl.LoadConstantI4(0);
        }
        else {
            getterIl.OpCode(ILOpCode.Ldnull);
        }

        getterIl.OpCode(ILOpCode.Ret);
        MethodDefinitionHandle getter = metadata.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            MethodImplAttributes.IL,
            metadata.GetOrAddString("get_" + name),
            AddMethodSignature(metadata, 0, returnType => encodeType(returnType.Type(isByRef: false)), static _ => { }),
            methodBodies.AddMethodBody(getterIl),
            MetadataTokens.ParameterHandle(1));

        InstructionEncoder setterIl = new(new BlobBuilder());
        setterIl.OpCode(ILOpCode.Ret);
        MethodDefinitionHandle setter = metadata.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            MethodImplAttributes.IL,
            metadata.GetOrAddString("set_" + name),
            AddMethodSignature(
                metadata,
                1,
                static returnType => returnType.Void(),
                parameters => encodeType(parameters.AddParameter().Type(isByRef: false))),
            methodBodies.AddMethodBody(setterIl),
            MetadataTokens.ParameterHandle(1));
        metadata.AddMethodSemantics(property, MethodSemanticsAttributes.Getter, getter);
        metadata.AddMethodSemantics(property, MethodSemanticsAttributes.Setter, setter);

        if (isDerivable) {
            BlobBuilder attributeValue = new();
            attributeValue.WriteUInt16(1);
            attributeValue.WriteInt32((int)DerivedFromSource.Context);
            attributeValue.WriteUInt16(0);
            _ = metadata.AddCustomAttribute(property, derivedFromConstructor, metadata.GetOrAddBlob(attributeValue));
        }
    }
}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.SourceTools.Parsing;
/// <summary>
/// Parses [Projection]-annotated types into DomainModel IR.
/// Pure function: no side effects, no Compilation references in output.
/// </summary>
public static class AttributeParser {
    private const string BoundedContextAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.BoundedContextAttribute";
    private const string ProjectionRoleEnumName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole";
    private const string ProjectionRoleAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionRoleAttribute";
    private const string BadgeSlotEnumName = "Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot";
    private const string ProjectionBadgeAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionBadgeAttribute";
    private const string DisplayAttributeName = "System.ComponentModel.DataAnnotations.DisplayAttribute";
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadgeMappings = new(ImmutableArray<BadgeMappingEntry>.Empty);
    private static readonly EquatableArray<DiagnosticInfo> EmptyDiagnostics = new(ImmutableArray<DiagnosticInfo>.Empty);
    private static readonly ParseResult EmptyParseResult = new(null, EmptyDiagnostics);

    /// <summary>
    /// Parses a [Projection]-annotated type into a ParseResult containing IR and diagnostics.
    /// </summary>
    public static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct) {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) {
            return EmptyParseResult;
        }

        return Parse(typeSymbol, context.TargetNode, ct);
    }

    /// <summary>
    /// Parses a single projection type into a ParseResult containing IR and diagnostics.
    /// </summary>
    /// <param name="typeSymbol">The projection type to parse.</param>
    /// <param name="targetNode">The syntax node declaring the projection type.</param>
    /// <param name="ct">Cancellation token for generator responsiveness.</param>
    /// <returns>The parsed IR and any collected diagnostics.</returns>
    public static ParseResult Parse(INamedTypeSymbol typeSymbol, SyntaxNode targetNode, CancellationToken ct) {
        if (ct.IsCancellationRequested) {
            return EmptyParseResult;
        }

        List<DiagnosticInfo> diagnostics = [];
        string filePath = GetFilePath(targetNode);
        Microsoft.CodeAnalysis.Text.LinePosition linePos = GetLinePosition(targetNode);

        // Validate type kind
        ValidateTypeKind(typeSymbol, targetNode, diagnostics, filePath, linePos);

        if (ct.IsCancellationRequested) {
            return EmptyParseResult;
        }

        // Extract type info
        string typeName = typeSymbol.Name;
        string ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        // Parse attributes on the type
        string? boundedContext = ParseBoundedContext(typeSymbol, diagnostics, filePath, linePos, out string? boundedContextDisplayLabel);
        string? projectionRole = ParseProjectionRole(typeSymbol, diagnostics, filePath, linePos);

        if (ct.IsCancellationRequested) {
            return EmptyParseResult;
        }

        // Parse properties
        ImmutableArray<PropertyModel>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<PropertyModel>();

        ImmutableArray<ISymbol> members = typeSymbol.GetMembers();
        for (int i = 0; i < members.Length; i++) {
            if (ct.IsCancellationRequested) {
                return EmptyParseResult;
            }

            if (members[i] is IPropertySymbol propertySymbol
                && propertySymbol.DeclaredAccessibility == Accessibility.Public
                && !propertySymbol.IsStatic
                && !propertySymbol.IsIndexer) {
                PropertyModel property = ParseProperty(propertySymbol, typeName, diagnostics, filePath);
                propertiesBuilder.Add(property);
            }
        }

        var model = new DomainModel(
            typeName,
            ns,
            boundedContext,
            boundedContextDisplayLabel,
            projectionRole,
            new EquatableArray<PropertyModel>(propertiesBuilder.ToImmutable()));

        return new ParseResult(
            model,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static void ValidateTypeKind(
        INamedTypeSymbol typeSymbol,
        SyntaxNode targetNode,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        // Check partial keyword
        bool isPartial = targetNode is TypeDeclarationSyntax tds
            && tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1003",
                string.Format("[Projection] type '{0}' should be declared as partial for source generator to emit code", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // Check for struct types
        if (typeSymbol.TypeKind == TypeKind.Struct) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Projection] attribute on '{0}' is not supported: structs are not supported. Only non-abstract, non-generic classes and records are supported", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // Check for abstract types
        if (typeSymbol.IsAbstract) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Projection] attribute on '{0}' is not supported: abstract types are not supported. Only non-abstract, non-generic classes and records are supported", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // Check for generic types
        if (typeSymbol.IsGenericType) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Projection] attribute on '{0}' is not supported: generic types are not supported. Only non-abstract, non-generic classes and records are supported", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // Check for nested class in non-partial outer
        if (typeSymbol.ContainingType is INamedTypeSymbol containingType) {
            SyntaxReference? outerRef = containingType.DeclaringSyntaxReferences.FirstOrDefault();
            bool outerIsPartial = outerRef?.GetSyntax() is TypeDeclarationSyntax outerTds
                && outerTds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

            if (!outerIsPartial) {
                diagnostics.Add(new DiagnosticInfo(
                    "HFC1004",
                    string.Format("[Projection] attribute on '{0}' is not supported: nested in non-partial type '{1}'. Only non-abstract, non-generic classes and records are supported", typeSymbol.Name, containingType.Name),
                    "Warning",
                    filePath,
                    linePos.Line,
                    linePos.Character));
            }
        }

    }

    private static string? ParseBoundedContext(
        INamedTypeSymbol typeSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos,
        out string? displayLabel) {
        displayLabel = null;
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == BoundedContextAttributeName) {
                // Extract DisplayLabel from named arguments
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                    if (namedArg.Key == "DisplayLabel" && namedArg.Value.Value is string label
                        && !string.IsNullOrWhiteSpace(label)) {
                        displayLabel = label;
                    }
                }

                if (attr.ConstructorArguments.Length > 0) {
                    TypedConstant arg = attr.ConstructorArguments[0];
                    if (arg.Value is string name && !string.IsNullOrWhiteSpace(name)) {
                        return name.Trim();
                    }

                    // BoundedContext name is invalid — clear any captured DisplayLabel
                    displayLabel = null;
                    diagnostics.Add(new DiagnosticInfo(
                        "HFC1005",
                        string.Format("Invalid argument for attribute 'BoundedContext' on type '{0}': name must be a non-empty string", typeSymbol.Name),
                        "Warning",
                        filePath,
                        linePos.Line,
                        linePos.Character));
                }

                return null;
            }
        }

        return null;
    }

    private static string? ParseProjectionRole(
        INamedTypeSymbol typeSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == ProjectionRoleAttributeName) {
                if (attr.ConstructorArguments.Length > 0) {
                    TypedConstant arg = attr.ConstructorArguments[0];
                    if (arg.Value is int enumValue) {
                        if (TryResolveEnumMemberName(attr.AttributeClass!.ContainingAssembly, ProjectionRoleEnumName, enumValue, out string roleName)) {
                            return roleName;
                        }

                        diagnostics.Add(new DiagnosticInfo(
                            "HFC1005",
                            string.Format("Invalid argument for attribute 'ProjectionRole' on type '{0}': role must be a defined ProjectionRole value", typeSymbol.Name),
                            "Warning",
                            filePath,
                            linePos.Line,
                            linePos.Character));
                    }
                }

                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a single property symbol into a <see cref="PropertyModel"/>. Called by
    /// <see cref="CommandParser"/> as well so unsupported-type diagnostics stay consistent.
    /// </summary>
    internal static PropertyModel ParsePropertyForCommand(
        IPropertySymbol propertySymbol,
        string containingTypeName,
        List<DiagnosticInfo> diagnostics,
        string filePath) => ParseProperty(propertySymbol, containingTypeName, diagnostics, filePath);

    private static PropertyModel ParseProperty(
        IPropertySymbol propertySymbol,
        string containingTypeName,
        List<DiagnosticInfo> diagnostics,
        string filePath) {
        ITypeSymbol propertyType = propertySymbol.Type;
        bool isNullable = false;
        string fullyQualifiedTypeName;

        // Handle Nullable<T> for value types
        if (propertyType is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1) {
            isNullable = true;
            propertyType = namedType.TypeArguments[0];
        }

        // Handle reference type nullability via annotation
        if (!isNullable && propertySymbol.NullableAnnotation == NullableAnnotation.Annotated) {
            isNullable = true;
        }

        // Handle nullable context disabled: treat reference types as nullable by default
        if (!isNullable
            && propertySymbol.NullableAnnotation == NullableAnnotation.None
            && propertyType.IsReferenceType) {
            isNullable = true;
        }

        bool isEnumType = propertyType.TypeKind == TypeKind.Enum;
        bool isEnum = IsSupportedEnumType(propertyType);

        // Get fully qualified type name for mapping
        if (propertyType is INamedTypeSymbol namedPropertyType && namedPropertyType.IsGenericType) {
            // Build constructed name for display
            fullyQualifiedTypeName = namedPropertyType.OriginalDefinition.ContainingNamespace?.ToDisplayString() + "." + namedPropertyType.OriginalDefinition.Name;
        }
        else {
            fullyQualifiedTypeName = propertyType.ToDisplayString(
                new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        }

        // Map the type
        string? irType = FieldTypeMapper.MapType(fullyQualifiedTypeName, isEnum);
        bool isUnsupported = irType is null;
        string? enumFqn = isEnum ? fullyQualifiedTypeName : null;
        string? unsupportedFqn = isUnsupported ? fullyQualifiedTypeName : null;

        if (isUnsupported) {
            Location location = propertySymbol.Locations.FirstOrDefault() ?? Location.None;
            Microsoft.CodeAnalysis.Text.LinePosition propLinePos = location.GetLineSpan().StartLinePosition;
            string propFilePath = location.SourceTree?.FilePath ?? filePath;
            string unsupportedType = DescribeUnsupportedType(propertyType, isEnumType);

            string newline = "\n";

            diagnostics.Add(new DiagnosticInfo(
                "HFC1002",
                string.Format(
                    "What: Property '{0}' on type '{1}' is not supported for auto-generation.{3}Expected: One of: string, int, long, decimal, double, float, bool, DateTime, DateTimeOffset, DateOnly, TimeOnly, enum (backed by int), Guid, or nullable/collection variants.{3}Got: {2}{3}Fix: Use a supported type, or override rendering with [ProjectionFieldSlot] (Story 6.3).{3}DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002",
                    propertySymbol.Name,
                    containingTypeName,
                    unsupportedType,
                    newline),
                "Warning",
                propFilePath,
                propLinePos.Line,
                propLinePos.Character));

            irType = propertyType.ToDisplayString();
        }

        string resolvedTypeName = irType ?? propertyType.ToDisplayString();

        // Parse [Display] attribute
        string? displayName = ParseDisplayAttribute(propertySymbol);

        // Parse [ProjectionBadge] from enum fields (if this property is an enum type)
        EquatableArray<BadgeMappingEntry> badgeMappings = ParseBadgeMappings(propertyType, isEnum, diagnostics, filePath);

        return new PropertyModel(
            propertySymbol.Name,
            resolvedTypeName,
            isNullable,
            isUnsupported,
            displayName,
            badgeMappings,
            enumFqn,
            unsupportedFqn);
    }

    private static string? ParseDisplayAttribute(IPropertySymbol propertySymbol) {
        foreach (AttributeData attr in propertySymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == DisplayAttributeName) {
                // DisplayAttribute uses named arguments: Name="...", Description="..."
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                    if (namedArg.Key == "Name" && namedArg.Value.Value is string name) {
                        return name;
                    }
                }
            }
        }

        return null;
    }

    private static EquatableArray<BadgeMappingEntry> ParseBadgeMappings(
        ITypeSymbol propertyType,
        bool isEnum,
        List<DiagnosticInfo> diagnostics,
        string filePath) {
        if (!isEnum) {
            return EmptyBadgeMappings;
        }

        if (propertyType is not INamedTypeSymbol enumType) {
            return EmptyBadgeMappings;
        }

        ImmutableArray<BadgeMappingEntry>.Builder builder = ImmutableArray.CreateBuilder<BadgeMappingEntry>();

        foreach (ISymbol member in enumType.GetMembers()) {
            if (member is IFieldSymbol field && field.HasConstantValue) {
                foreach (AttributeData attr in field.GetAttributes()) {
                    if (attr.AttributeClass?.ToDisplayString() == ProjectionBadgeAttributeName
                        && attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is int slotValue) {
                        if (TryResolveEnumMemberName(attr.AttributeClass!.ContainingAssembly, BadgeSlotEnumName, slotValue, out string slotName)) {
                            builder.Add(new BadgeMappingEntry(field.Name, slotName));
                        }
                        else {
                            Location location = field.Locations.FirstOrDefault() ?? Location.None;
                            Microsoft.CodeAnalysis.Text.LinePosition linePos = location.GetLineSpan().StartLinePosition;
                            string fieldFilePath = location.SourceTree?.FilePath ?? filePath;

                            diagnostics.Add(new DiagnosticInfo(
                                "HFC1005",
                                string.Format("Invalid argument for attribute 'ProjectionBadge' on enum member '{0}': slot must be a defined BadgeSlot value", field.Name),
                                "Warning",
                                fieldFilePath,
                                linePos.Line,
                                linePos.Character));
                        }
                    }
                }
            }
        }

        return new EquatableArray<BadgeMappingEntry>(builder.ToImmutable());
    }

    private static bool IsSupportedEnumType(ITypeSymbol propertyType) => propertyType is INamedTypeSymbol enumType
            && enumType.TypeKind == TypeKind.Enum
            && enumType.EnumUnderlyingType?.SpecialType == SpecialType.System_Int32;

    private static string DescribeUnsupportedType(ITypeSymbol propertyType, bool isEnumType) {
        if (isEnumType
            && propertyType is INamedTypeSymbol enumType
            && enumType.EnumUnderlyingType is ITypeSymbol underlyingType) {
            return string.Format("{0} (enum underlying type {1})", propertyType.ToDisplayString(), underlyingType.ToDisplayString());
        }

        return propertyType.ToDisplayString();
    }

    private static bool TryResolveEnumMemberName(IAssemblySymbol assembly, string metadataName, int value, out string memberName) {
        INamedTypeSymbol? enumType = assembly.GetTypeByMetadataName(metadataName);
        if (enumType is not null) {
            foreach (ISymbol member in enumType.GetMembers()) {
                if (member is IFieldSymbol field
                    && field.HasConstantValue
                    && field.ConstantValue is int fieldValue
                    && fieldValue == value) {
                    memberName = field.Name;
                    return true;
                }
            }
        }

        memberName = string.Empty;
        return false;
    }

    private static string GetFilePath(SyntaxNode node)
        => node.SyntaxTree.FilePath ?? string.Empty;

    private static Microsoft.CodeAnalysis.Text.LinePosition GetLinePosition(SyntaxNode node)
        => node.GetLocation().GetLineSpan().StartLinePosition;
}

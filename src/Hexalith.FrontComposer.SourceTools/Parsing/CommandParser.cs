using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Parses [Command]-annotated types into <see cref="CommandModel"/> IR.
/// Pure function: no side effects, no Compilation references in output.
/// </summary>
public static class CommandParser {
    /// <summary>
    /// Property names that are classified as derivable by convention (filled by infrastructure).
    /// </summary>
    private static readonly HashSet<string> WellKnownDerivablePropertyNames = new(StringComparer.Ordinal) {
        "MessageId",
        "CommandId",
        "CorrelationId",
        "TenantId",
        "UserId",
        "Timestamp",
        "CreatedAt",
        "ModifiedAt",
    };

    private const string BoundedContextAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.BoundedContextAttribute";
    private const string DerivedFromAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.DerivedFromAttribute";
    private const string DisplayAttributeName = "System.ComponentModel.DataAnnotations.DisplayAttribute";
    private const string IconAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.IconAttribute";
    private const string DefaultValueAttributeName = "System.ComponentModel.DefaultValueAttribute";

    /// <summary>Warning threshold for non-derivable property count.</summary>
    public const int NonDerivableWarningThreshold = 30;

    /// <summary>Hard error threshold for non-derivable property count (DoS mitigation).</summary>
    public const int NonDerivableErrorThreshold = 100;

    /// <summary>Hard error threshold for TOTAL property count — Story 2-2 HFC1011 (DoS mitigation).</summary>
    public const int TotalPropertyHardLimit = 200;

    private static readonly EquatableArray<DiagnosticInfo> EmptyDiagnostics = new(ImmutableArray<DiagnosticInfo>.Empty);
    private static readonly CommandParseResult EmptyResult = new(null, EmptyDiagnostics);

    /// <summary>
    /// Parses a <see cref="GeneratorAttributeSyntaxContext"/> target symbol into a command IR.
    /// </summary>
    public static CommandParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct) {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) {
            return EmptyResult;
        }

        return Parse(typeSymbol, context.TargetNode, ct);
    }

    /// <summary>
    /// Parses a [Command]-annotated type symbol into a command IR plus any diagnostics.
    /// </summary>
    public static CommandParseResult Parse(INamedTypeSymbol typeSymbol, SyntaxNode targetNode, CancellationToken ct) {
        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        List<DiagnosticInfo> diagnostics = [];
        string filePath = targetNode.SyntaxTree.FilePath ?? string.Empty;
        Microsoft.CodeAnalysis.Text.LinePosition linePos = targetNode.GetLocation().GetLineSpan().StartLinePosition;

        ValidateTypeKind(typeSymbol, targetNode, diagnostics, filePath, linePos);

        // HFC1014: nested [Command] types are unsupported (Story 2-2 Task 1.3b). A command's ContainingSymbol
        // must be a namespace, not a class/struct/record. Emit early and halt parsing — nested generation would
        // break the route-emission invariant {namespace.{CommandTypeName}Page} if the class lived inside another.
        if (typeSymbol.ContainingType is not null) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1014",
                string.Format(
                    "[Command] type '{0}' is nested inside '{1}'. Command types must be top-level within a namespace.",
                    typeSymbol.Name,
                    typeSymbol.ContainingType.ToDisplayString()),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // HFC1009: emitted form body uses `_model = new()`; commands without a public parameterless
        // ctor (positional records, classes with only parameterised ctors) would generate uncompilable
        // code. Surface a build error so the adopter fixes the command shape. (Patch P1.)
        if (!HasPublicParameterlessConstructor(typeSymbol)) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1009",
                string.Format(
                    "[Command] type '{0}' has no public parameterless constructor. The generated form requires 'new {0}()' to build the form model. Add a parameterless constructor, or give every positional parameter a default value.",
                    typeSymbol.Name),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        string typeName = typeSymbol.Name;
        string ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        string? boundedContext = ParseBoundedContext(typeSymbol, out string? boundedContextDisplayLabel);
        string? displayName = ParseDisplayAttribute(typeSymbol);
        string? iconName = ParseIconAttribute(typeSymbol);

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // Collect public, non-static, non-indexer properties, including inherited ones (e.g., MessageId on a base record).
        ImmutableArray<PropertyModel>.Builder allBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder derivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder nonDerivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();

        // Case-insensitive dedup + MessageId lookup (patch 2026-04-16 P-02): a property named
        // `messageId` satisfies the runtime MessageId contract, so HFC1006 must not fire on casing alone.
        HashSet<string> seenNames = new(StringComparer.Ordinal);
        List<IPropertySymbol> nonDerivableSymbols = new();
        INamedTypeSymbol? currentType = typeSymbol;
        while (currentType is not null && currentType.SpecialType != SpecialType.System_Object) {
            foreach (ISymbol member in currentType.GetMembers()) {
                if (ct.IsCancellationRequested) {
                    return EmptyResult;
                }

                if (member is not IPropertySymbol propertySymbol
                    || propertySymbol.DeclaredAccessibility != Accessibility.Public
                    || propertySymbol.IsStatic
                    || propertySymbol.IsIndexer) {
                    continue;
                }

                // Skip record synthetic EqualityContract property.
                if (propertySymbol.Name == "EqualityContract") {
                    continue;
                }

                // Inherited override: keep the most-derived declaration only.
                if (!seenNames.Add(propertySymbol.Name)) {
                    continue;
                }

                PropertyModel property = AttributeParser.ParsePropertyForCommand(propertySymbol, typeName, diagnostics, filePath);
                ValidateDefaultValueType(propertySymbol, diagnostics, filePath);
                allBuilder.Add(property);

                bool isDerivable = IsDerivableProperty(propertySymbol);
                if (isDerivable) {
                    derivableBuilder.Add(property);
                }
                else {
                    nonDerivableBuilder.Add(property);
                    nonDerivableSymbols.Add(propertySymbol);
                }
            }

            currentType = currentType.BaseType;
        }

        // Check MessageId presence (HFC1006). Also walk AllInterfaces so an interface-declared
        // `MessageId` (potentially with a default implementation) satisfies the contract (patch 2026-04-16 P-03).
        bool hasMessageId = seenNames.Contains("MessageId");
        if (!hasMessageId) {
            foreach (INamedTypeSymbol iface in typeSymbol.AllInterfaces) {
                foreach (ISymbol interfaceMember in iface.GetMembers("MessageId")) {
                    if (interfaceMember is IPropertySymbol { IsStatic: false, IsIndexer: false }) {
                        hasMessageId = true;
                        break;
                    }
                }

                if (hasMessageId) {
                    break;
                }
            }
        }

        if (!hasMessageId) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1006",
                string.Format("[Command] type '{0}' is missing a 'MessageId' property. Add a string MessageId property (or inherit from a base record that provides it) so commands can be correlated end-to-end.", typeName),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // HFC1016: non-derivable property must be writable via `_model.X = v`. Read-only or
        // init-only setters fail to compile at adopter side even when HFC1009 passes (init-only
        // records still have an implicit parameterless ctor). Patch 2026-04-16 P-01.
        foreach (IPropertySymbol prop in nonDerivableSymbols) {
            IMethodSymbol? setter = prop.SetMethod;
            bool isWritable = setter is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
            if (!isWritable) {
                string kind = setter is null
                    ? "has no public setter"
                    : setter.IsInitOnly
                        ? "is declared with an 'init' accessor"
                        : "has a non-public setter";

                diagnostics.Add(new DiagnosticInfo(
                    "HFC1016",
                    string.Format(
                        "[Command] type '{0}' property '{1}' {2}. The generated form binds input controls via '_model.{1} = value', which requires a public writable setter. Change the property to '{{ get; set; }}' or mark it with [DerivedFrom].",
                        typeName,
                        prop.Name,
                        kind),
                    "Error",
                    filePath,
                    linePos.Line,
                    linePos.Character));
            }
        }

        // HFC1011: hard error when total (derivable + non-derivable) public property count exceeds 200 — Story 2-2 Task 1.3a, Red-team RT-5.
        int totalCount = allBuilder.Count;
        if (totalCount > TotalPropertyHardLimit) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1011",
                string.Format(
                    "[Command] type '{0}' has {1} total public properties, exceeding the hard limit of {2}. Split the command into smaller aggregates.",
                    typeName,
                    totalCount,
                    TotalPropertyHardLimit),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        int nonDerivableCount = nonDerivableBuilder.Count;
        if (nonDerivableCount > NonDerivableErrorThreshold) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1007",
                string.Format("[Command] type '{0}' has {1} non-derivable properties, exceeding the hard limit of {2}. Split the command into smaller aggregates.", typeName, nonDerivableCount, NonDerivableErrorThreshold),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
        }
        else if (nonDerivableCount > NonDerivableWarningThreshold) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1007",
                string.Format("[Command] type '{0}' has {1} non-derivable properties, above the recommended limit of {2}. Consider splitting the command for better form ergonomics.", typeName, nonDerivableCount, NonDerivableWarningThreshold),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        var model = new CommandModel(
            typeName,
            ns,
            boundedContext,
            boundedContextDisplayLabel,
            displayName,
            new EquatableArray<PropertyModel>(allBuilder.ToImmutable()),
            new EquatableArray<PropertyModel>(derivableBuilder.ToImmutable()),
            new EquatableArray<PropertyModel>(nonDerivableBuilder.ToImmutable()),
            iconName);

        return new CommandParseResult(
            model,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol typeSymbol) {
        foreach (IMethodSymbol ctor in typeSymbol.InstanceConstructors) {
            if (ctor.DeclaredAccessibility != Accessibility.Public) {
                continue;
            }

            if (ctor.Parameters.Length == 0) {
                return true;
            }

            // A ctor whose parameters all have default values (including positional record
            // primary ctors like `record Foo(string A = "", int B = 0)`) is callable as `new Foo()`.
            bool allOptional = true;
            foreach (IParameterSymbol parameter in ctor.Parameters) {
                if (!parameter.HasExplicitDefaultValue) {
                    allOptional = false;
                    break;
                }
            }

            if (allOptional) {
                return true;
            }
        }

        return false;
    }

    private static bool IsDerivableProperty(IPropertySymbol propertySymbol) {
        // Walk the property's inheritance chain (override / shadow / base positional params):
        // attributes placed on a base declaration must still be discoverable when a derived
        // record shadows the property (see code-review 2026-04-15, patch P15).
        IPropertySymbol? current = propertySymbol;
        while (current is not null) {
            foreach (AttributeData attr in current.GetAttributes()) {
                if (attr.AttributeClass?.ToDisplayString() == DerivedFromAttributeName) {
                    return true;
                }
            }

            current = current.OverriddenProperty ?? FindShadowedProperty(current);
        }

        return WellKnownDerivablePropertyNames.Contains(propertySymbol.Name);
    }

    private static IPropertySymbol? FindShadowedProperty(IPropertySymbol propertySymbol) {
        INamedTypeSymbol? baseType = propertySymbol.ContainingType?.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object) {
            foreach (ISymbol member in baseType.GetMembers(propertySymbol.Name)) {
                if (member is IPropertySymbol basePropertySymbol
                    && basePropertySymbol.DeclaredAccessibility == Accessibility.Public
                    && !basePropertySymbol.IsStatic
                    && !basePropertySymbol.IsIndexer) {
                    return basePropertySymbol;
                }
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static void ValidateTypeKind(
        INamedTypeSymbol typeSymbol,
        SyntaxNode targetNode,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        if (typeSymbol.TypeKind == TypeKind.Struct) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: structs are not supported. Only non-abstract, non-generic classes and records are supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.IsAbstract) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: abstract types are not supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.IsGenericType) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: generic types are not supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.ContainingNamespace?.ToDisplayString().StartsWith("System", StringComparison.Ordinal) == true) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: command types must not live in the 'System' namespace.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }
    }

    private static string? ParseBoundedContext(INamedTypeSymbol typeSymbol, out string? displayLabel) {
        displayLabel = null;
        INamedTypeSymbol? current = typeSymbol;
        while (current is not null) {
            foreach (AttributeData attr in current.GetAttributes()) {
                if (attr.AttributeClass?.ToDisplayString() == BoundedContextAttributeName) {
                    foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                        if (namedArg.Key == "DisplayLabel"
                            && namedArg.Value.Value is string label
                            && !string.IsNullOrWhiteSpace(label)) {
                            displayLabel = label;
                        }
                    }

                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is string name
                        && !string.IsNullOrWhiteSpace(name)) {
                        return name.Trim();
                    }

                    return null;
                }
            }

            current = current.ContainingType;
        }

        return null;
    }

    private static string? ParseDisplayAttribute(INamedTypeSymbol typeSymbol) {
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == DisplayAttributeName) {
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                    if (namedArg.Key == "Name" && namedArg.Value.Value is string name) {
                        return name;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the <c>[Icon(IconName)]</c> attribute value when declared on the command type.
    /// Format validation is deferred to runtime (Story 2-2 Decision D34).
    /// </summary>
    private static string? ParseIconAttribute(INamedTypeSymbol typeSymbol) {
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == IconAttributeName
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string iconName
                && !string.IsNullOrWhiteSpace(iconName)) {
                return iconName;
            }
        }

        return null;
    }

    /// <summary>
    /// HFC1012: emits a parse-time error when a property's <c>[DefaultValue(x)]</c> argument's runtime type is not
    /// assignable to the decorated property's declared type (Story 2-2 Task 1.3c, Chaos CM-1).
    /// Uses Roslyn's <see cref="TypedConstant"/> type to compare against the property type; nullable wrappers unwrapped.
    /// </summary>
    private static void ValidateDefaultValueType(IPropertySymbol propertySymbol, List<DiagnosticInfo> diagnostics, string filePath) {
        foreach (AttributeData attr in propertySymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() != DefaultValueAttributeName) {
                continue;
            }

            if (attr.ConstructorArguments.Length == 0) {
                continue;
            }

            ITypeSymbol propertyType = propertySymbol.Type;
            // Unwrap Nullable<T> for comparison.
            if (propertyType is INamedTypeSymbol named && named.IsGenericType && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) {
                propertyType = named.TypeArguments[0];
            }

            bool assignable;
            string actualTypeDisplay;
            if (attr.AttributeConstructor?.Parameters is { Length: 2 } parameters
                && parameters[0].Type.ToDisplayString() == "System.Type"
                && parameters[1].Type.SpecialType == SpecialType.System_String
                && attr.ConstructorArguments[0].Value is ITypeSymbol declaredDefaultType) {
                assignable = IsDefaultValueTypeAssignable(declaredDefaultType, propertyType);
                actualTypeDisplay = declaredDefaultType.ToDisplayString();
            }
            else {
                TypedConstant arg = attr.ConstructorArguments[0];

                // Null is always assignable (reference / nullable types); skip the check.
                if (arg.IsNull) {
                    continue;
                }

                ITypeSymbol? argType = arg.Type;
                if (argType is null) {
                    continue;
                }

                assignable = IsDefaultValueTypeAssignable(argType, propertyType);
                actualTypeDisplay = argType.ToDisplayString();
            }

            if (!assignable) {
                AttributeSyntax? attrSyntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
                Microsoft.CodeAnalysis.Text.LinePosition linePos = attrSyntax?.GetLocation().GetLineSpan().StartLinePosition ?? default;
                diagnostics.Add(new DiagnosticInfo(
                    "HFC1012",
                    string.Format(
                        "Property '{0}.{1}' has [DefaultValue] of type '{2}' which is not assignable to the property type '{3}'.",
                        propertySymbol.ContainingType?.Name ?? "?",
                        propertySymbol.Name,
                        actualTypeDisplay,
                        propertySymbol.Type.ToDisplayString()),
                    "Error",
                    filePath,
                    linePos.Line,
                    linePos.Character));
            }
        }
    }

    // ECMA-334 §10.2.3 implicit numeric conversion coverage (Story 2-2 code-review P26).
    private static bool IsDefaultValueTypeAssignable(ITypeSymbol candidateType, ITypeSymbol propertyType) {
        if (SymbolEqualityComparer.Default.Equals(candidateType, propertyType)) {
            return true;
        }

        SpecialType from = candidateType.SpecialType;
        SpecialType to = propertyType.SpecialType;
        return (from, to) switch {
            (SpecialType.System_SByte, SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Byte, SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int16, SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_UInt16, SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int32, SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_UInt32, SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int64 or SpecialType.System_UInt64, SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Char, SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Single, SpecialType.System_Double) => true,
            _ => false,
        };
    }
}

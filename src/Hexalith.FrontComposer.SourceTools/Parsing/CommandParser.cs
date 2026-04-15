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

    /// <summary>Warning threshold for non-derivable property count.</summary>
    public const int NonDerivableWarningThreshold = 30;

    /// <summary>Hard error threshold for non-derivable property count (DoS mitigation).</summary>
    public const int NonDerivableErrorThreshold = 100;

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

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        string typeName = typeSymbol.Name;
        string ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        string? boundedContext = ParseBoundedContext(typeSymbol, out string? boundedContextDisplayLabel);
        string? displayName = ParseDisplayAttribute(typeSymbol);

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // Collect public, non-static, non-indexer properties, including inherited ones (e.g., MessageId on a base record).
        ImmutableArray<PropertyModel>.Builder allBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder derivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder nonDerivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();

        HashSet<string> seenNames = new(StringComparer.Ordinal);
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
                allBuilder.Add(property);

                bool isDerivable = IsDerivableProperty(propertySymbol);
                if (isDerivable) {
                    derivableBuilder.Add(property);
                }
                else {
                    nonDerivableBuilder.Add(property);
                }
            }

            currentType = currentType.BaseType;
        }

        // Check MessageId presence (HFC1006).
        if (!seenNames.Contains("MessageId")) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1006",
                string.Format("[Command] type '{0}' is missing a 'MessageId' property. Add a string MessageId property (or inherit from a base record that provides it) so commands can be correlated end-to-end.", typeName),
                "Warning",
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
            new EquatableArray<PropertyModel>(nonDerivableBuilder.ToImmutable()));

        return new CommandParseResult(
            model,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static bool IsDerivableProperty(IPropertySymbol propertySymbol) {
        foreach (AttributeData attr in propertySymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == DerivedFromAttributeName) {
                return true;
            }
        }

        return WellKnownDerivablePropertyNames.Contains(propertySymbol.Name);
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
}

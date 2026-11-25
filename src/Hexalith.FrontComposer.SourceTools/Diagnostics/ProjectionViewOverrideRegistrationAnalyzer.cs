using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Hexalith.FrontComposer.SourceTools.Diagnostics;

/// <summary>Checks the component and Context contract in typed Level 4 view registrations.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProjectionViewOverrideRegistrationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.ProjectionViewOverrideComponentInvalid];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilation =>
        {
            INamedTypeSymbol? extensions = compilation.Compilation.GetTypeByMetadataName(
                "Hexalith.FrontComposer.Shell.Extensions.ProjectionViewOverrideServiceCollectionExtensions");
            INamedTypeSymbol? viewContext = compilation.Compilation.GetTypeByMetadataName(
                "Hexalith.FrontComposer.Contracts.Rendering.ProjectionViewContext`1");
            INamedTypeSymbol? parameter = compilation.Compilation.GetTypeByMetadataName(
                "Microsoft.AspNetCore.Components.ParameterAttribute");
            INamedTypeSymbol? cascadingParameter = compilation.Compilation.GetTypeByMetadataName(
                "Microsoft.AspNetCore.Components.CascadingParameterAttributeBase");
            if (extensions is not null && viewContext is not null && parameter is not null && cascadingParameter is not null)
            {
                compilation.RegisterOperationAction(
                    action => AnalyzeRegistration(action, extensions, viewContext, parameter, cascadingParameter),
                    OperationKind.Invocation, OperationKind.MethodReference);
            }
        });
    }

    private static void AnalyzeRegistration(
        OperationAnalysisContext context,
        INamedTypeSymbol extensions,
        INamedTypeSymbol viewContext,
        INamedTypeSymbol parameter,
        INamedTypeSymbol cascadingParameter)
    {
        IMethodSymbol method = context.Operation is IInvocationOperation invocation
            ? invocation.TargetMethod
            : ((IMethodReferenceOperation)context.Operation).Method;
        if (method.Name != "AddViewOverride"
            || method.TypeArguments.Length != 2
            || !SymbolEqualityComparer.Default.Equals((method.ReducedFrom ?? method).ContainingType, extensions))
        {
            return;
        }

        ITypeSymbol projection = method.TypeArguments[0];
        if (method.TypeArguments[1] is not INamedTypeSymbol { TypeKind: not TypeKind.Error } component
            || projection.TypeKind == TypeKind.Error)
        {
            return;
        }

        string? reason = GetIncompatibility(
            context.Compilation, component, viewContext, projection, parameter, cascadingParameter);
        if (reason is null)
        {
            return;
        }

        string componentName = component.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        string projectionName = projection.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ProjectionViewOverrideComponentInvalid,
            context.Operation.Syntax.GetLocation(),
            $"What: `{componentName}` cannot replace the view for `{projectionName}`. "
            + $"Expected: a concrete component with a public settable [Parameter] ProjectionViewContext<{projectionName}> Context. "
            + $"Got: {reason} Fix: update the Context property or register a compatible component. "
            + "DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1043"));
    }

    private static string? GetIncompatibility(
        Compilation compilation,
        INamedTypeSymbol component,
        INamedTypeSymbol viewContext,
        ITypeSymbol projection,
        INamedTypeSymbol parameter,
        INamedTypeSymbol cascadingParameter)
    {
        if (component.IsAbstract || component.TypeKind == TypeKind.Interface)
        {
            return "The component is abstract or an interface.";
        }

        IPropertySymbol? found = null;
        for (INamedTypeSymbol? type = component; type is not null; type = type.BaseType)
        {
            foreach (ISymbol member in type.GetMembers("Context"))
            {
                if (member is not IPropertySymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } property)
                {
                    continue;
                }

                if (found is not null && IsKnown(found.Type) && IsKnown(property.Type)
                    && !compilation.ClassifyCommonConversion(found.Type, property.Type).IsIdentity)
                {
                    return "The component exposes ambiguous Context properties.";
                }

                found ??= property;
            }
        }

        if (found?.Type is not INamedTypeSymbol suppliedContext
            || !SymbolEqualityComparer.Default.Equals(suppliedContext.OriginalDefinition, viewContext)
            || (IsKnown(projection) && IsKnown(suppliedContext)
                && !compilation.ClassifyCommonConversion(suppliedContext, viewContext.Construct(projection)).IsIdentity))
        {
            return "The component has no public Context property of the required type.";
        }

        if (found.SetMethod?.DeclaredAccessibility != Accessibility.Public)
        {
            return "The Context property has no public setter.";
        }

        AttributeData? directParameter = null;
        for (IPropertySymbol? property = found; property is not null; property = property.OverriddenProperty)
        {
            foreach (AttributeData attribute in property.GetAttributes())
            {
                if (RequiresCascadingValue(attribute.AttributeClass, cascadingParameter))
                {
                    return "The Context property accepts cascading values instead of the supplied parameter.";
                }

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, parameter))
                {
                    directParameter ??= attribute;
                }
            }
        }

        if (directParameter is null)
        {
            return "The Context property is missing [Parameter].";
        }

        return directParameter.NamedArguments.Any(static argument =>
            argument.Key == "CaptureUnmatchedValues" && argument.Value.Value is true)
            ? "The Context parameter cannot capture unmatched values."
            : null;
    }

    private static bool RequiresCascadingValue(INamedTypeSymbol? attribute, INamedTypeSymbol cascadingParameter)
    {
        if (attribute?.ToDisplayString() == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute")
        {
            return false;
        }

        for (INamedTypeSymbol? type = attribute; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, cascadingParameter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKnown(ITypeSymbol type) => type switch
    {
        { TypeKind: TypeKind.Error or TypeKind.TypeParameter } => false,
        IArrayTypeSymbol array => IsKnown(array.ElementType),
        INamedTypeSymbol named => (named.ContainingType is null || IsKnown(named.ContainingType))
            && named.TypeArguments.All(IsKnown),
        _ => true,
    };
}

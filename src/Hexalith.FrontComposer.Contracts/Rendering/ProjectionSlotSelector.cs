using System.Linq.Expressions;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Diagnostics;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Parses refactor-safe Level 3 slot selector expressions.
/// </summary>
public static class ProjectionSlotSelector {
    private const string DocsLink = "https://hexalith.dev/frontcomposer/diagnostics/HFC1038";

    /// <summary>
    /// Parses a direct property selector.
    /// </summary>
    /// <typeparam name="TProjection">Projection type owning the selected field.</typeparam>
    /// <typeparam name="TField">Selected field type.</typeparam>
    /// <param name="field">Direct property selector such as <c>x =&gt; x.Priority</c>.</param>
    /// <returns>The canonical selected field identity.</returns>
    /// <exception cref="ProjectionSlotSelectorException">Thrown for nested, computed, captured,
    /// indexer, method-call, or otherwise non-direct selectors.</exception>
    public static ProjectionSlotFieldIdentity Parse<TProjection, TField>(
        Expression<Func<TProjection, TField>> field)
        => ParseCore(field);

    /// <summary>
    /// Parses a direct property selector whose return type is object.
    /// </summary>
    /// <typeparam name="TProjection">Projection type owning the selected field.</typeparam>
    /// <param name="field">Direct property selector such as <c>x =&gt; x.Priority</c>.</param>
    /// <returns>The canonical selected field identity.</returns>
    public static ProjectionSlotFieldIdentity Parse<TProjection>(
        Expression<Func<TProjection, object?>> field)
        => ParseCore(field);

    private static ProjectionSlotFieldIdentity ParseCore(LambdaExpression lambda) {
        if (lambda is null) {
            throw new ArgumentNullException(nameof(lambda));
        }

        Expression body = StripConversions(lambda.Body);
        if (body is not MemberExpression memberExpression
            || memberExpression.Member is not PropertyInfo property
            || memberExpression.Expression is not ParameterExpression parameter
            || parameter != lambda.Parameters[0]
            || property.GetIndexParameters().Length != 0) {
            throw Invalid(lambda);
        }

        return new ProjectionSlotFieldIdentity(property.Name, property.PropertyType);
    }

    private static Expression StripConversions(Expression expression) {
        // Repeatedly unwrap compiler-emitted Convert/ConvertChecked nodes that wrap a
        // direct property selector. Two cases must be tolerated:
        //   - boxing conversions to System.Object (Expression&lt;Func&lt;T, object?&gt;&gt; selectors);
        //   - lifted nullable conversions (Convert(member, typeof(T?)) when the property is T,
        //     or Convert(Convert(member, typeof(T?)), typeof(object))).
        // Anything else (user method-call, Convert to an unrelated type, etc.) is left
        // for ParseCore to reject.
        while (expression is UnaryExpression unary
            && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)
            && IsPassThroughConversion(unary)) {
            expression = unary.Operand;
        }

        return expression;
    }

    private static bool IsPassThroughConversion(UnaryExpression unary) {
        if (unary.Type == typeof(object)) {
            return true;
        }

        Type? operandUnderlying = Nullable.GetUnderlyingType(unary.Operand.Type);
        Type? targetUnderlying = Nullable.GetUnderlyingType(unary.Type);
        Type operandRoot = operandUnderlying ?? unary.Operand.Type;
        Type targetRoot = targetUnderlying ?? unary.Type;
        return operandRoot == targetRoot;
    }

    private static ProjectionSlotSelectorException Invalid(LambdaExpression lambda)
        => new(
            $"{FcDiagnosticIds.HFC1038_ProjectionSlotSelectorInvalid}: Invalid Level 3 slot selector '{lambda}'. "
            + "Expected: direct projection property expression like x => x.Priority. "
            + "Got: nested member, method call, captured value, indexer, computed expression, or unsupported conversion. "
            + "Fix: select one public projection property directly and register a separate slot for each field. "
            + "Docs: " + DocsLink + ".");
}

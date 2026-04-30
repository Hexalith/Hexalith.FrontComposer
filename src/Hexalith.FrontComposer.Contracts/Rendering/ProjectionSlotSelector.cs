using System.Linq.Expressions;
using System.Reflection;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Parses refactor-safe Level 3 slot selector expressions.
/// </summary>
public static class ProjectionSlotSelector {
    private const string DiagnosticId = "HFC1038";
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

        Expression body = StripBoxingConversion(lambda.Body);
        if (body is not MemberExpression memberExpression
            || memberExpression.Member is not PropertyInfo property
            || memberExpression.Expression is not ParameterExpression parameter
            || parameter != lambda.Parameters[0]
            || property.GetIndexParameters().Length != 0) {
            throw Invalid(lambda);
        }

        return new ProjectionSlotFieldIdentity(property.Name, property.PropertyType);
    }

    private static Expression StripBoxingConversion(Expression expression) {
        if (expression is UnaryExpression unary
            && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)
            && unary.Type == typeof(object)) {
            return unary.Operand;
        }

        return expression;
    }

    private static ProjectionSlotSelectorException Invalid(LambdaExpression lambda)
        => new(
            $"{DiagnosticId}: Invalid Level 3 slot selector '{lambda}'. "
            + "Expected: direct projection property expression like x => x.Priority. "
            + "Got: nested member, method call, captured value, indexer, computed expression, or unsupported conversion. "
            + "Fix: select one public projection property directly and register a separate slot for each field. "
            + "Docs: " + DocsLink + ".");
}

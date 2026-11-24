using System.Linq.Expressions;
using System.Reflection;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Validates a direct property assignment for a test data builder.</summary>
internal static class TestDataPropertySelector
{
    /// <summary>Returns the selected property after validating that it can store the value.</summary>
    internal static PropertyInfo GetWritableProperty<TModel, TValue>(
        Expression<Func<TModel, TValue>> property,
        TValue value)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (StripSafeConversion(property.Body) is not MemberExpression
            {
                Member: PropertyInfo info,
                Expression: { } receiver,
            }
            || StripSafeConversion(receiver) != property.Parameters[0])
        {
            throw new ArgumentException("Use a direct property selector such as x => x.Id.", nameof(property));
        }

        if (!info.CanWrite)
        {
            throw new ArgumentException($"Property '{info.Name}' must be writable.", nameof(property));
        }

        if (value is not null && !info.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"The value is incompatible with property '{info.Name}'.", nameof(value));
        }

        if (value is null && info.PropertyType.IsValueType && Nullable.GetUnderlyingType(info.PropertyType) is null)
        {
            throw new ArgumentNullException(nameof(value), $"Property '{info.Name}' does not accept null.");
        }

        return info;
    }

    private static Expression StripSafeConversion(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert, Method: null } conversion
            && conversion.Type.IsAssignableFrom(conversion.Operand.Type)
            ? conversion.Operand
            : expression;
}

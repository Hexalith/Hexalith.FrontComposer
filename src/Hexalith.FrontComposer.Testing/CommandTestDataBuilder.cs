using System.Linq.Expressions;
using System.Reflection;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Small deterministic builder for command models used in generated command component tests.
/// </summary>
/// <typeparam name="TCommand">Command model type.</typeparam>
public sealed class CommandTestDataBuilder<TCommand>
    where TCommand : class, new() {
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <summary>Sets a command property value.</summary>
    public CommandTestDataBuilder<TCommand> With<TValue>(
        Expression<Func<TCommand, TValue>> property,
        TValue value) {
        ArgumentNullException.ThrowIfNull(property);
        _values[GetProperty(property).Name] = value;
        return this;
    }

    /// <summary>Builds a command instance with configured property values.</summary>
    public TCommand Build() {
        TCommand command = new();
        foreach ((string name, object? value) in _values) {
            PropertyInfo property = typeof(TCommand).GetProperty(name)
                ?? throw new InvalidOperationException($"Command property '{name}' was not found.");
            property.SetValue(command, value);
        }

        return command;
    }

    private static PropertyInfo GetProperty<TValue>(Expression<Func<TCommand, TValue>> property)
        => property.Body is MemberExpression { Member: PropertyInfo info }
            ? info
            : throw new ArgumentException("Use a direct property selector such as x => x.Id.", nameof(property));
}

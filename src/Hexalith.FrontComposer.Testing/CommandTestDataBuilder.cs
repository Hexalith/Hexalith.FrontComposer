using System.Linq.Expressions;
using System.Reflection;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Small deterministic builder for command models used in generated command component tests.
/// </summary>
/// <typeparam name="TCommand">Command model type.</typeparam>
public sealed class CommandTestDataBuilder<TCommand>
    where TCommand : class, new() {
    private readonly Dictionary<string, (PropertyInfo Property, object? Value)> _values = new(StringComparer.Ordinal);

    /// <summary>Sets a command property value.</summary>
    /// <typeparam name="TValue">The value type exposed by the selector.</typeparam>
    /// <param name="property">A direct selector for a writable property.</param>
    /// <param name="value">The value to assign.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">The selector is null, or the property does not accept a null value.</exception>
    /// <exception cref="ArgumentException">The selector or value is incompatible with the property assignment.</exception>
    public CommandTestDataBuilder<TCommand> With<TValue>(
        Expression<Func<TCommand, TValue>> property,
        TValue value) {
        PropertyInfo selected = TestDataPropertySelector.GetWritableProperty(property, value);
        _values[selected.Name] = (selected, value);
        return this;
    }

    /// <summary>Builds a command instance with configured property values.</summary>
    public TCommand Build() {
        TCommand command = new();
        foreach ((PropertyInfo property, object? value) in _values.Values) {
            property.SetValue(command, value);
        }

        return command;
    }
}

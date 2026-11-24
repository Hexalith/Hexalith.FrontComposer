using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Small deterministic builder for projection models used in adopter component tests.
/// </summary>
/// <typeparam name="TProjection">Projection model type.</typeparam>
public sealed class ProjectionTestDataBuilder<TProjection>
    where TProjection : class, new() {
    private readonly Dictionary<string, (PropertyInfo Property, object? Value)> _values = new(StringComparer.Ordinal);

    /// <summary>Sets a projection property value.</summary>
    /// <typeparam name="TValue">The value type exposed by the selector.</typeparam>
    /// <param name="property">A direct selector for a writable property.</param>
    /// <param name="value">The value to assign.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">The selector is null, or the property does not accept a null value.</exception>
    /// <exception cref="ArgumentException">The selector or value is incompatible with the property assignment.</exception>
    public ProjectionTestDataBuilder<TProjection> With<TValue>(
        Expression<Func<TProjection, TValue>> property,
        TValue value) {
        PropertyInfo selected = TestDataPropertySelector.GetWritableProperty(property, value);
        _values[selected.Name] = (selected, value);
        return this;
    }

    /// <summary>Builds a projection instance with configured property values.</summary>
    public TProjection Build() {
        TProjection projection = new();
        foreach ((PropertyInfo property, object? value) in _values.Values) {
            property.SetValue(projection, value);
        }

        return projection;
    }

    /// <summary>Builds several projection instances from a sequence of configure callbacks.</summary>
    /// <param name="configure">One non-null callback for each projection to create.</param>
    /// <returns>The projection instances in callback order.</returns>
    /// <exception cref="ArgumentNullException">The callback array is null.</exception>
    /// <exception cref="ArgumentException">The callback array contains a null entry.</exception>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Shipped 2.x public factory. Relocating it to a non-generic companion type would delete a published member; approved as design-ca1000-generic-static-compatibility in the analyzer-policy exception ledger and revisited at the 3.0 compatibility boundary.")]
    public static IReadOnlyList<TProjection> BuildMany(params Action<ProjectionTestDataBuilder<TProjection>>[] configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Action<ProjectionTestDataBuilder<TProjection>>[] callbacks = [.. configure];
        if (Array.Exists(callbacks, static action => action is null)) {
            throw new ArgumentException("Every projection configuration callback must be non-null.", nameof(configure));
        }

        return callbacks.Select(action => {
            ProjectionTestDataBuilder<TProjection> builder = new();
            action(builder);
            return builder.Build();
        }).ToArray();
    }
}

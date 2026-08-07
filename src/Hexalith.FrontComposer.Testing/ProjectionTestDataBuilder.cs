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
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <summary>Sets a projection property value.</summary>
    public ProjectionTestDataBuilder<TProjection> With<TValue>(
        Expression<Func<TProjection, TValue>> property,
        TValue value) {
        ArgumentNullException.ThrowIfNull(property);
        _values[GetProperty(property).Name] = value;
        return this;
    }

    /// <summary>Builds a projection instance with configured property values.</summary>
    public TProjection Build() {
        TProjection projection = new();
        foreach ((string name, object? value) in _values) {
            PropertyInfo property = typeof(TProjection).GetProperty(name)
                ?? throw new InvalidOperationException($"Projection property '{name}' was not found.");
            property.SetValue(projection, value);
        }

        return projection;
    }

    /// <summary>Builds several projection instances from a sequence of configure callbacks.</summary>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Shipped 2.x public factory. Relocating it to a non-generic companion type would delete a published member; approved as design-ca1000-generic-static-compatibility in the analyzer-policy exception ledger and revisited at the 3.0 compatibility boundary.")]
    public static IReadOnlyList<TProjection> BuildMany(params Action<ProjectionTestDataBuilder<TProjection>>[] configure)
        => configure.Select(action => {
            ProjectionTestDataBuilder<TProjection> builder = new();
            action(builder);
            return builder.Build();
        }).ToArray();

    private static PropertyInfo GetProperty<TValue>(Expression<Func<TProjection, TValue>> property)
        => property.Body is MemberExpression { Member: PropertyInfo info }
            ? info
            : throw new ArgumentException("Use a direct property selector such as x => x.Id.", nameof(property));
}

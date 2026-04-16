using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #2 in the chain (Story 2-2 Decision D24): reads values from the cascading
/// <see cref="ProjectionContext"/>. Null-tolerant (Decision D27) — when the cascade is absent the
/// provider declines and the chain continues.
/// </summary>
/// <remarks>
/// Matching rules:
/// <list type="number">
///   <item>Exact key match on <see cref="ProjectionContext.Fields"/>.</item>
///   <item><c>{ProjectionName}Id</c> convention — when the property name ends with <c>"Id"</c> and the stem matches
///     the projection's short type name, the provider returns <see cref="ProjectionContext.AggregateId"/>.</item>
/// </list>
/// </remarks>
public sealed class ProjectionContextProvider : IDerivedValueProvider {
    /// <inheritdoc/>
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (projectionContext is null) {
            return Task.FromResult(DerivedValueResult.None);
        }

        if (projectionContext.Fields.TryGetValue(propertyName, out object? value)) {
            return Task.FromResult(new DerivedValueResult(true, value));
        }

        if (!string.IsNullOrEmpty(projectionContext.AggregateId)
            && propertyName.EndsWith("Id", StringComparison.Ordinal)) {
            string stem = propertyName.Substring(0, propertyName.Length - 2);
            string shortName = ShortName(projectionContext.ProjectionTypeFqn);
            if (string.Equals(stem, shortName, StringComparison.Ordinal)) {
                return Task.FromResult(new DerivedValueResult(true, projectionContext.AggregateId));
            }
        }

        return Task.FromResult(DerivedValueResult.None);
    }

    private static string ShortName(string fqn) {
        int dot = fqn.LastIndexOf('.');
        return dot < 0 ? fqn : fqn.Substring(dot + 1);
    }
}

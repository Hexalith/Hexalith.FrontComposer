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
        ct.ThrowIfCancellationRequested();

        if (projectionContext is null) {
            return Task.FromResult(DerivedValueResult.None);
        }

        if (projectionContext.Fields.TryGetValue(propertyName, out object? value)) {
            return Task.FromResult(new DerivedValueResult(true, value));
        }

        if (!string.IsNullOrWhiteSpace(projectionContext.AggregateId)
            && propertyName.EndsWith("Id", StringComparison.Ordinal)) {
            string stem = propertyName.Substring(0, propertyName.Length - 2);
            string shortName = ShortName(projectionContext.ProjectionTypeFqn);
            if (string.Equals(stem, shortName, StringComparison.Ordinal)) {
                return Task.FromResult(new DerivedValueResult(true, projectionContext.AggregateId));
            }
        }

        return Task.FromResult(DerivedValueResult.None);
    }

    /// <summary>
    /// Extracts the short (unqualified) type name from a <see cref="Type.FullName"/>-style string,
    /// stripping generic arity markers (<c>`N[[...]]</c>) and treating both <c>.</c> and nested-type
    /// <c>+</c> separators as scope boundaries.
    /// </summary>
    private static string ShortName(string fqn) {
        int arity = fqn.IndexOf('`');
        ReadOnlySpan<char> head = arity < 0 ? fqn.AsSpan() : fqn.AsSpan(0, arity);

        int lastDot = head.LastIndexOf('.');
        int lastPlus = head.LastIndexOf('+');
        int cut = Math.Max(lastDot, lastPlus);
        return cut < 0 ? head.ToString() : head.Slice(cut + 1).ToString();
    }
}

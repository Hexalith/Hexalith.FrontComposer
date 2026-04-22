namespace Hexalith.FrontComposer.Shell.Badges;

/// <summary>
/// Builds stable seen-set <c>capabilityId</c> strings for Story 3-5 D11.
/// </summary>
/// <remarks>
/// <para>
/// Two reserved prefixes:
/// <list type="bullet">
///   <item><term><c>bc:</c></term><description>identifies a bounded context (<c>bc:Counter</c>).</description></item>
///   <item><term><c>proj:</c></term><description>identifies a projection within a bounded context
///   (<c>proj:Counter:Hexalith.Samples.Counter.Projections.CounterProjection</c>).</description></item>
/// </list>
/// </para>
/// <para>
/// Adopters extending the seen-set with their own capability shapes (v1.x extensibility) MUST
/// choose a prefix outside the framework reserved list.
/// </para>
/// </remarks>
public static class CapabilityIds {
    /// <summary>The reserved prefix for bounded-context capability ids.</summary>
    public const string BoundedContextPrefix = "bc:";

    /// <summary>The reserved prefix for projection capability ids.</summary>
    public const string ProjectionPrefix = "proj:";

    /// <summary>
    /// Builds the seen-set id for a bounded context.
    /// </summary>
    /// <param name="boundedContext">The bounded-context name (e.g. <c>"Counter"</c>).</param>
    /// <returns>The capability id, e.g. <c>"bc:Counter"</c>.</returns>
    public static string ForBoundedContext(string boundedContext) {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundedContext);
        return BoundedContextPrefix + boundedContext;
    }

    /// <summary>
    /// Builds the seen-set id for a projection within a bounded context.
    /// </summary>
    /// <param name="boundedContext">The bounded-context name.</param>
    /// <param name="projectionTypeFullName">The projection's fully-qualified type name.</param>
    /// <returns>The capability id, e.g. <c>"proj:Counter:Hexalith.Samples.Counter.Projections.CounterProjection"</c>.</returns>
    public static string ForProjection(string boundedContext, string projectionTypeFullName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundedContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFullName);
        return ProjectionPrefix + boundedContext + ":" + projectionTypeFullName;
    }
}

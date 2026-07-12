namespace Hexalith.FrontComposer.Shell.Routing;

/// <summary>Pure helpers for canonical projection routes and display labels.</summary>
internal static class ProjectionRouteBuilder {
    /// <summary>
    /// Builds <c>/{bounded-context-lowercase}/{simple-projection-type-kebab}</c>.
    /// </summary>
    /// <param name="boundedContext">The bounded-context name.</param>
    /// <param name="projectionFqn">The fully qualified or simple projection type name.</param>
    /// <returns>The canonical projection route.</returns>
    public static string BuildRoute(string boundedContext, string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(boundedContext);
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);

        string label = ProjectionLabel(projectionFqn);
        string slug = string.IsNullOrWhiteSpace(label) ? label : CommandRouteBuilder.KebabCase(label);
        return $"/{boundedContext.ToLowerInvariant()}/{slug}";
    }

    /// <summary>Returns the projection's simple type name with its original casing.</summary>
    /// <param name="projectionFqn">The fully qualified or simple projection type name.</param>
    /// <returns>The segment after the final namespace separator.</returns>
    public static string ProjectionLabel(string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);
        int lastDot = projectionFqn.LastIndexOf('.');
        return lastDot < 0 ? projectionFqn : projectionFqn[(lastDot + 1)..];
    }
}

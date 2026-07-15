namespace Hexalith.FrontComposer.Shell.Components.Home;

/// <summary>
/// Per-projection row for the card body.
/// </summary>
/// <param name="ProjectionFqn">The fully-qualified projection type name.</param>
/// <param name="Count">The current actionable-item count for the projection, or <see langword="null"/> while still pending.</param>
public sealed record HomeProjectionRow(string ProjectionFqn, int? Count);

namespace Hexalith.FrontComposer.Testing;

/// <summary>Measured summary content, separate from its accessible label.</summary>
/// <param name="Semantics">Measured role/name/focus/channel attributes.</param>
/// <param name="Text">Measured localized summary description.</param>
/// <param name="ErrorCount">Measured complete summary error count.</param>
public sealed record UxValidationSummarySnapshot(UxAccessibilitySnapshot Semantics, string Text, int ErrorCount);

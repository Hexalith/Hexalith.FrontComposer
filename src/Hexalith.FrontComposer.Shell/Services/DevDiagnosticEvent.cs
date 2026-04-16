namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Dev-mode diagnostic event surfaced by the shell — consumed by <c>FcDiagnosticsPanel</c> and
/// forwarded to <see cref="Microsoft.Extensions.Logging.ILogger"/>. Story 2-2 Tasks 3.5/3.5a,
/// surfacing Decision D31 fail-closed conditions (tenant/user missing) in a way that is visible
/// to developers but never in production.
/// </summary>
/// <param name="Code">Short diagnostic code (e.g. <c>"D31"</c>, <c>"D38"</c>).</param>
/// <param name="Category">Dev-facing category label (e.g. <c>"LastUsed"</c>).</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="CapturedAt">UTC timestamp (for the panel's ordering / rate-limit window).</param>
public sealed record DevDiagnosticEvent(
    string Code,
    string Category,
    string Message,
    DateTimeOffset CapturedAt);

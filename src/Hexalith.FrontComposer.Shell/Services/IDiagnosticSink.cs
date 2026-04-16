namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Publishes <see cref="DevDiagnosticEvent"/>s to any subscribed surface (dev panel + logger).
/// </summary>
public interface IDiagnosticSink {
    /// <summary>Publishes a single event.</summary>
    void Publish(DevDiagnosticEvent evt);

    /// <summary>Returns the most-recent retained events (newest-first).</summary>
    IReadOnlyList<DevDiagnosticEvent> RecentEvents { get; }
}

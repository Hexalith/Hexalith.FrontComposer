using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Represents diagnostic logger metadata normalized by an independent observer.
/// </summary>
/// <param name="EventId">The logger event identifier.</param>
/// <param name="EventName">The logger event name.</param>
/// <param name="Level">The declared logger level, or <see langword="null"/> when unresolved.</param>
/// <param name="HasExceptionParameter">Whether the attributed method accepts an exception parameter.</param>
/// <param name="Location">The source or reflection location that produced the entry.</param>
internal sealed record FrontComposerDiagnosticLogInventoryEntry(
    int EventId,
    string? EventName,
    LogLevel? Level,
    bool HasExceptionParameter,
    string Location);

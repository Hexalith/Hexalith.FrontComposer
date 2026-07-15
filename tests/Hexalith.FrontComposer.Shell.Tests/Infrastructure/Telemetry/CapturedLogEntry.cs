using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

internal sealed record CapturedLogEntry(
    LogLevel Level,
    EventId EventId,
    IReadOnlyDictionary<string, object?> State,
    string Message,
    Exception? Exception);

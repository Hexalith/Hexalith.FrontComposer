using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

internal sealed record CapturedLogEntry(
    LogLevel Level,
    EventId EventId,
    IReadOnlyDictionary<string, object?> State,
    string Message,
    Exception? Exception);

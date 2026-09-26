using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

internal sealed class McpAuditLogger(string categoryName, ConcurrentQueue<string> entries) : ILogger {
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        entries.Enqueue(categoryName + ": " + formatter(state, exception) + (exception?.ToString() ?? string.Empty));
    }
}

using System.Collections;
using System.Collections.Concurrent;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Captures every log entry, its level, its structured state values, and every scope state, so leak assertions
/// cover what a structured sink would export and not only the rendered message.
/// </summary>
/// <param name="categoryName">The logger category.</param>
/// <param name="entries">The shared entry queue.</param>
internal sealed class McpAuditLogger(string categoryName, ConcurrentQueue<string> entries) : ILogger {
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull {
        entries.Enqueue(categoryName + " [Scope]: " + state + Describe(state));
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        entries.Enqueue(categoryName + " [" + logLevel + "]: " + formatter(state, exception) + Describe(state) + (exception?.ToString() ?? string.Empty));
    }

    private static string Describe(object? state) {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values) {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach (KeyValuePair<string, object?> value in values) {
            _ = builder.Append(" {").Append(value.Key).Append('=').Append(Render(value.Value)).Append('}');
        }

        return builder.ToString();
    }

    // A collection value would otherwise render as its type name and hide its elements from leak assertions.
    private static string? Render(object? value)
        => value is IEnumerable sequence and not string
            ? "[" + string.Join(", ", sequence.Cast<object?>()) + "]"
            : value?.ToString();
}

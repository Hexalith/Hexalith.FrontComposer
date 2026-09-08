using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

/// <summary>
/// Captures log entries while enabling exactly one <see cref="LogLevel"/>, so a wrapper whose
/// enablement guard checks the wrong level is observable.
/// </summary>
/// <param name="enabledLevel">The single level the logger reports as enabled.</param>
internal sealed class SingleLevelLogger(LogLevel enabledLevel) : ILogger
{
    /// <summary>
    /// Gets the entries the logger recorded.
    /// </summary>
    public List<CapturedLogEntry> Entries { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logLevel == enabledLevel;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        IReadOnlyDictionary<string, object?> structuredState = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.ToDictionary(static value => value.Key, static value => value.Value, StringComparer.Ordinal)
            : new Dictionary<string, object?>(StringComparer.Ordinal);
        Entries.Add(new(logLevel, eventId, structuredState, formatter(state, exception), exception));
    }
}

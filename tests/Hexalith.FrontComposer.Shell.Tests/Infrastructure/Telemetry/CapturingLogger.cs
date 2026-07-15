using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<CapturedLogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        IReadOnlyDictionary<string, object?> structuredState = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.ToDictionary(static value => value.Key, static value => value.Value, StringComparer.Ordinal)
            : new Dictionary<string, object?>(StringComparer.Ordinal);
        Entries.Add(new(logLevel, eventId, structuredState, formatter(state, exception), exception));
    }
}

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

internal sealed class CapturingLogger<T> : ILogger<T> {
    /// <summary>Backwards-compatible message-only view (kept for tests using `Messages.ShouldContain`).</summary>
    public List<string> Messages { get; } = [];

    /// <summary>P13 — full (level, message, exception) capture so tests can verify HFC2012/HFC2013 are logged at the expected severity.</summary>
    public List<CapturedLogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        string message = formatter(state, exception);
        Messages.Add(message);
        Entries.Add(new CapturedLogEntry(logLevel, message, exception));
    }
}

internal sealed record CapturedLogEntry(LogLevel Level, string Message, Exception? Exception);

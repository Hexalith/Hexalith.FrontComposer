namespace Hexalith.FrontComposer.Shell.Infrastructure.Storage;

/// <summary>
/// A queued write awaiting the drain worker.
/// </summary>
/// <param name="Key">The localStorage key (or <see cref="LocalStorageService.SentinelKey"/> for a flush marker).</param>
/// <param name="SerializedValue">The JSON payload, or <see langword="null"/> to signal a remove.</param>
/// <param name="FlushSignal">When non-null, the drain worker completes this TCS once the record is observed (used by <see cref="LocalStorageService.FlushAsync"/>).</param>
internal readonly record struct PendingWrite(
    string Key,
    string? SerializedValue,
    TaskCompletionSource? FlushSignal);

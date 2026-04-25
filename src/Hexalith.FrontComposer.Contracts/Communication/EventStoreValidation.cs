namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Shared validation helpers for FrontComposer's EventStore communication seam.
/// </summary>
public static class EventStoreValidation {
    /// <summary>
    /// Maximum number of ETag validators accepted by the EventStore query endpoint.
    /// </summary>
    public const int MaxETagValidators = 10;

    /// <summary>
    /// Default maximum serialized UTF-8 request-body size accepted by EventStore.
    /// </summary>
    public const int DefaultMaxRequestBytes = 1_048_576;

    /// <summary>
    /// Validates a required EventStore routing segment.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="paramName">The parameter name used for exceptions.</param>
    /// <returns>The original value when valid.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is missing or contains a colon.</exception>
    public static string RequireNonColonSegment(string? value, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("EventStore routing values must not be empty.", paramName);
        }

        string result = value!;
        if (result.Contains(":")) {
            throw new ArgumentException("EventStore routing values must not contain ':'.", paramName);
        }

        return result;
    }

    /// <summary>
    /// Validates ETag validator count before an HTTP request is sent.
    /// </summary>
    /// <param name="etags">The validator set.</param>
    /// <param name="maxCount">The configured maximum count.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is invalid.</exception>
    /// <exception cref="ArgumentException">Thrown when too many validators are supplied.</exception>
    public static void ValidateETagCount(IReadOnlyCollection<string>? etags, int maxCount = MaxETagValidators) {
        if (maxCount <= 0 || maxCount > MaxETagValidators) {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "The ETag validator maximum must be between 1 and 10.");
        }

        if (etags is not null && etags.Count > maxCount) {
            throw new ArgumentException(
                $"At most {maxCount} ETag validators can be sent to EventStore.",
                nameof(etags));
        }
    }
}

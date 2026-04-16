using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.Services.Lifecycle;

/// <summary>
/// Default <see cref="IUlidFactory"/> that wraps <c>NUlid.Ulid.NewUlid()</c> — 128-bit
/// Crockford Base32 identifier with millisecond-precision timestamp prefix + 80-bit cryptographic
/// entropy (Decision D3 / ADR-018).
/// </summary>
/// <remarks>
/// <para>
/// Falls back to <see cref="Guid.NewGuid()"/> on <see cref="System.Security.Cryptography.CryptographicException"/>
/// (Chaos Monkey CM2 — NUlid reads <see cref="System.Security.Cryptography.RandomNumberGenerator"/>; exotic
/// Windows security policies can throw). FR36 deterministic dedup still works because Guids are unique;
/// time-sortability is lost — an Epic 5 concern.
/// </para>
/// </remarks>
public sealed class UlidFactory : IUlidFactory {
    private readonly ILogger<UlidFactory> _logger;

    public UlidFactory(ILogger<UlidFactory>? logger = null) {
        _logger = logger ?? NullLogger<UlidFactory>.Instance;
    }

    /// <inheritdoc/>
    public string NewUlid() {
        try {
            return NUlid.Ulid.NewUlid().ToString();
        }
        catch (System.Security.Cryptography.CryptographicException ex) {
            _logger.LogWarning(
                ex,
                "NUlid generation failed; falling back to Guid. Time-sortable MessageId lost for this submission.");
            return Guid.NewGuid().ToString("N");
        }
    }
}

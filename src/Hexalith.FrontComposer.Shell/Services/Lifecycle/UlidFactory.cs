using Hexalith.FrontComposer.Contracts.Lifecycle;

using NUlid.Rng;

namespace Hexalith.FrontComposer.Shell.Services.Lifecycle;

/// <summary>
/// Default <see cref="IUlidFactory"/> that wraps <c>NUlid.Ulid.NewUlid()</c> — 128-bit
/// Crockford Base32 identifier with millisecond-precision timestamp prefix + 80-bit cryptographic
/// entropy (Decision D3 / ADR-018).
/// </summary>
public sealed class UlidFactory : IUlidFactory {
    internal static IUlidRng EntropySource { get; } = new CSUlidRng();

    /// <inheritdoc/>
    public string NewUlid() => NUlid.Ulid.NewUlid(EntropySource).ToString();
}

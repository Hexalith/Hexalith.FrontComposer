namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Produces deterministic canonical ULIDs for one test-host dispatch sequence.
/// </summary>
internal static class DeterministicTestUlid {
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Creates the message identifier for a dispatch sequence.
    /// </summary>
    /// <param name="sequence">The positive per-host dispatch sequence.</param>
    /// <returns>A canonical ULID unique to the message side of the sequence.</returns>
    internal static string CreateMessageId(long sequence) => Create(sequence, discriminator: 0);

    /// <summary>
    /// Creates the correlation identifier for a dispatch sequence.
    /// </summary>
    /// <param name="sequence">The positive per-host dispatch sequence.</param>
    /// <returns>A canonical ULID unique to the correlation side of the sequence.</returns>
    internal static string CreateCorrelationId(long sequence) => Create(sequence, discriminator: 1);

    private static string Create(long sequence, uint discriminator) {
        if (sequence <= 0) {
            throw new InvalidOperationException("The deterministic test ULID sequence is exhausted.");
        }

        UInt128 value = ((UInt128)(ulong)sequence << 1) | discriminator;
        Span<char> encoded = stackalloc char[26];
        for (int index = encoded.Length - 1; index >= 0; index--) {
            encoded[index] = CrockfordBase32[(int)(value & 31)];
            value >>= 5;
        }

        return new string(encoded);
    }
}

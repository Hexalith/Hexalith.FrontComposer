using System.Security.Cryptography;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>
/// Creates deterministic log join tokens for identifiers and diagnostic material written to Shell logs.
/// </summary>
/// <remarks>
/// These truncated, unsalted digest tokens are operational join aids. They do not guarantee anonymity
/// or uniqueness, prove integrity, or convey authorization semantics.
/// </remarks>
internal static class FrontComposerLogPseudonymizer
{
    private const int Utf8BufferSize = 1024;

    /// <summary>
    /// Trims and converts an identifier into a deterministic log join token, mapping missing values to
    /// <c>absent</c>.
    /// </summary>
    /// <param name="value">The identifier to pseudonymize.</param>
    /// <returns>The canonical log join token.</returns>
    internal static string Pseudonymize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "absent";
        }

        return Hash(value.AsSpan().Trim());
    }

    /// <summary>
    /// Hashes the complete supplied character span without applying normalization or a length suffix.
    /// </summary>
    /// <param name="value">The characters to hash with deterministic .NET UTF-8 encoding.</param>
    /// <returns>A token containing the lowercase first eight bytes of the SHA-256 digest.</returns>
    internal static string Hash(ReadOnlySpan<char> value)
    {
        Span<byte> utf8Buffer = stackalloc byte[Utf8BufferSize];
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];

        try
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Encoder encoder = Encoding.UTF8.GetEncoder();
            bool completed = false;

            while (!completed)
            {
                encoder.Convert(
                    value,
                    utf8Buffer,
                    flush: true,
                    out int charactersUsed,
                    out int bytesUsed,
                    out completed);
                hash.AppendData(utf8Buffer[..bytesUsed]);
                CryptographicOperations.ZeroMemory(utf8Buffer[..bytesUsed]);
                value = value[charactersUsed..];
            }

            if (!hash.TryGetHashAndReset(digest, out int bytesWritten)
                || bytesWritten != SHA256.HashSizeInBytes)
            {
                throw new CryptographicException("SHA-256 did not produce the expected digest length.");
            }

            return "sha256:" + Convert.ToHexStringLower(digest[..8]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8Buffer);
            CryptographicOperations.ZeroMemory(digest);
        }
    }
}

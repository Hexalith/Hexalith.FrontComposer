using System.Buffers.Binary;
using System.Security.Cryptography;

using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class FrontComposerMcpUlidFactory : IUlidFactory {
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private readonly object _gate = new();
    private long _lastTimestampMs;
    private ulong _lastRandomHi;
    private ulong _lastRandomLo;

    public string NewUlid() {
        Span<byte> bytes = stackalloc byte[16];
        long milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ulong hi;
        ulong lo;
        lock (_gate) {
            // P51: defend against backwards clock movement (NTP step / VM migration / container
            // skew). Lexicographic monotonicity must be preserved across the issued sequence; we
            // hold the timestamp at its prior maximum until wall-clock catches up.
            if (milliseconds < _lastTimestampMs) {
                milliseconds = _lastTimestampMs;
            }

            // Monotonicity per the canonical ULID spec section 4: when two IDs share the same
            // timestamp segment, increment the random component instead of regenerating, so the
            // sequence remains lexicographically sortable.
            if (milliseconds == _lastTimestampMs) {
                ulong newLo = _lastRandomLo + 1;
                ulong nextHi = _lastRandomHi;
                if (newLo == 0) {
                    // D9 / ULID spec section 4: reject monotonic overflow within the same
                    // millisecond instead of silently wrapping the masked 16-bit hi field.
                    if (_lastRandomHi == 0xFFFFUL) {
                        throw new InvalidOperationException(
                            "ULID monotonic overflow within the same millisecond.");
                    }

                    nextHi = _lastRandomHi + 1;
                }

                _lastRandomHi = nextHi;
                _lastRandomLo = newLo;
            }
            else {
                _lastTimestampMs = milliseconds;
                Span<byte> rand = stackalloc byte[10];
                RandomNumberGenerator.Fill(rand);
                _lastRandomHi = (ulong)((rand[0] << 8) | rand[1]);
                _lastRandomLo = BinaryPrimitives.ReadUInt64BigEndian(rand[2..]);
            }

            hi = _lastRandomHi;
            lo = _lastRandomLo;
        }

        bytes[0] = (byte)(milliseconds >> 40);
        bytes[1] = (byte)(milliseconds >> 32);
        bytes[2] = (byte)(milliseconds >> 24);
        bytes[3] = (byte)(milliseconds >> 16);
        bytes[4] = (byte)(milliseconds >> 8);
        bytes[5] = (byte)milliseconds;
        bytes[6] = (byte)(hi >> 8);
        bytes[7] = (byte)hi;
        BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], lo);

        Span<char> chars = stackalloc char[26];
        WriteCrockford(bytes, chars);
        return new string(chars);
    }

    private static void WriteCrockford(ReadOnlySpan<byte> bytes, Span<char> chars) {
        ulong hi = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        ulong lo = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);

        chars[0] = Alphabet[(int)((hi >> 61) & 0x1F)];
        chars[1] = Alphabet[(int)((hi >> 56) & 0x1F)];
        chars[2] = Alphabet[(int)((hi >> 51) & 0x1F)];
        chars[3] = Alphabet[(int)((hi >> 46) & 0x1F)];
        chars[4] = Alphabet[(int)((hi >> 41) & 0x1F)];
        chars[5] = Alphabet[(int)((hi >> 36) & 0x1F)];
        chars[6] = Alphabet[(int)((hi >> 31) & 0x1F)];
        chars[7] = Alphabet[(int)((hi >> 26) & 0x1F)];
        chars[8] = Alphabet[(int)((hi >> 21) & 0x1F)];
        chars[9] = Alphabet[(int)((hi >> 16) & 0x1F)];
        chars[10] = Alphabet[(int)((hi >> 11) & 0x1F)];
        chars[11] = Alphabet[(int)((hi >> 6) & 0x1F)];
        chars[12] = Alphabet[(int)((hi >> 1) & 0x1F)];
        chars[13] = Alphabet[(int)(((hi & 0x1UL) << 4) | ((lo >> 60) & 0xFUL))];
        chars[14] = Alphabet[(int)((lo >> 55) & 0x1F)];
        chars[15] = Alphabet[(int)((lo >> 50) & 0x1F)];
        chars[16] = Alphabet[(int)((lo >> 45) & 0x1F)];
        chars[17] = Alphabet[(int)((lo >> 40) & 0x1F)];
        chars[18] = Alphabet[(int)((lo >> 35) & 0x1F)];
        chars[19] = Alphabet[(int)((lo >> 30) & 0x1F)];
        chars[20] = Alphabet[(int)((lo >> 25) & 0x1F)];
        chars[21] = Alphabet[(int)((lo >> 20) & 0x1F)];
        chars[22] = Alphabet[(int)((lo >> 15) & 0x1F)];
        chars[23] = Alphabet[(int)((lo >> 10) & 0x1F)];
        chars[24] = Alphabet[(int)((lo >> 5) & 0x1F)];
        chars[25] = Alphabet[(int)(lo & 0x1F)];
    }
}

using Hexalith.FrontComposer.Contracts.Lifecycle;

using NUlid.Rng;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>
/// Deterministic <see cref="IUlidFactory"/> for tests — emits a predictable sequence derived from a seed.
/// Uses <c>NUlid.Ulid.NewUlid(DateTimeOffset, IUlidRng)</c> so the ULID spec (Crockford Base32, 26 chars,
/// time-prefix) is preserved while entropy is deterministic (Task 10.1).
/// </summary>
public sealed class TestUlidFactory : IUlidFactory {
    private readonly object _gate = new();
    private long _counter;
    private readonly DateTimeOffset _baseTime;

    public TestUlidFactory(int seed = 0, DateTimeOffset? baseTime = null) {
        _counter = seed;
        _baseTime = baseTime ?? new DateTimeOffset(2026, 4, 16, 0, 0, 0, TimeSpan.Zero);
    }

    public string NewUlid() {
        long n;
        DateTimeOffset time;
        lock (_gate) {
            n = ++_counter;
            time = _baseTime.AddMilliseconds(n);
        }

        return NUlid.Ulid.NewUlid(time, new DeterministicRng(n)).ToString();
    }

    private sealed class DeterministicRng : IUlidRng {
        private readonly long _seed;
        public DeterministicRng(long seed) => _seed = seed;
        public byte[] GetRandomBytes(DateTimeOffset dateTime) {
            byte[] bytes = new byte[10];
            BitConverter.GetBytes(_seed).CopyTo(bytes, 0);
            return bytes;
        }
        public void GetRandomBytes(Span<byte> buffer, DateTimeOffset dateTime) {
            Span<byte> src = BitConverter.GetBytes(_seed);
            int copy = Math.Min(src.Length, buffer.Length);
            src[..copy].CopyTo(buffer[..copy]);
            buffer[copy..].Clear();
        }
    }
}

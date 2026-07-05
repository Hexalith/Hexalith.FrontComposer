using Hexalith.FrontComposer.Shell.Services.Auth;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerUserTokenStoreTests {
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
    private const string SensitiveToken = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ1c2VyLTEifQ.signature";

    [Fact]
    public void TryGet_ReturnsTokenBeforeExpiry_WithoutLoggingTokenMaterial() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);

        store.Set("user-1", SensitiveToken, FixedNow.AddMinutes(5));

        bool found = store.TryGet("user-1", out string token);

        found.ShouldBeTrue();
        (token == SensitiveToken).ShouldBeTrue("the stored value should be returned before expiry");
    }

    [Fact]
    public void TryGet_EvictsExpiredToken() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set("user-1", SensitiveToken, FixedNow.AddMinutes(5));

        time.Advance(TimeSpan.FromMinutes(6));

        store.TryGet("user-1", out _).ShouldBeFalse();
        store.TryGet("user-1", out _).ShouldBeFalse();
    }

    [Fact]
    public void Set_RejectsAlreadyExpiredToken_WithoutRemovingUnexpiredEntry() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set("user-1", SensitiveToken, FixedNow.AddMinutes(5));

        store.Set("user-1", "expired-token", FixedNow.AddTicks(-1));

        store.TryGet("user-1", out string token).ShouldBeTrue();
        (token == SensitiveToken).ShouldBeTrue("an already-expired incoming token should not evict a valid stored token");
    }

    [Fact]
    public void TryGet_ExpiredCleanup_DoesNotRemoveFreshReplacementToken() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set("user-1", SensitiveToken, FixedNow.AddMinutes(5));
        time.Advance(TimeSpan.FromMinutes(6));
        time.BeforeNextRead = () => store.Set("user-1", "fresh-token", FixedNow.AddMinutes(10));

        store.TryGet("user-1", out _).ShouldBeFalse();

        store.TryGet("user-1", out string token).ShouldBeTrue();
        (token == "fresh-token").ShouldBeTrue("expired cleanup should remove only the stale entry it observed");
    }

    [Fact]
    public void Remove_IsIdempotent() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);

        store.Remove("user-1");
        store.Set("user-1", SensitiveToken, FixedNow.AddMinutes(5));
        store.Remove("user-1");
        store.Remove("user-1");

        store.TryGet("user-1", out _).ShouldBeFalse();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        private DateTimeOffset _utcNow = utcNow;
        public Action? BeforeNextRead { get; set; }

        public override DateTimeOffset GetUtcNow() {
            Action? before = BeforeNextRead;
            BeforeNextRead = null;
            before?.Invoke();
            return _utcNow;
        }

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }
}

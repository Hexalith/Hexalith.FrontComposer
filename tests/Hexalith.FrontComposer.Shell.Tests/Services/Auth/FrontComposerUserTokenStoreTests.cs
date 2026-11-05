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

        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), SensitiveToken, FixedNow.AddMinutes(5));

        bool found = store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out string token);

        found.ShouldBeTrue();
        (token == SensitiveToken).ShouldBeTrue("the stored value should be returned before expiry");
    }

    [Fact]
    public void TryGet_EvictsExpiredToken() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), SensitiveToken, FixedNow.AddMinutes(5));

        time.Advance(TimeSpan.FromMinutes(6));

        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out _).ShouldBeFalse();
        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out _).ShouldBeFalse();
    }

    [Fact]
    public void Set_RejectsAlreadyExpiredToken_WithoutRemovingUnexpiredEntry() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), SensitiveToken, FixedNow.AddMinutes(5));

        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), "expired-token", FixedNow.AddTicks(-1));

        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out string token).ShouldBeTrue();
        (token == SensitiveToken).ShouldBeTrue("an already-expired incoming token should not evict a valid stored token");
    }

    [Fact]
    public void TryGet_ExpiredCleanup_DoesNotRemoveFreshReplacementToken() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), SensitiveToken, FixedNow.AddMinutes(5));
        time.Advance(TimeSpan.FromMinutes(6));
        time.BeforeNextRead = () => store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), "fresh-token", FixedNow.AddMinutes(10));

        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out _).ShouldBeFalse();

        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out string token).ShouldBeTrue();
        (token == "fresh-token").ShouldBeTrue("expired cleanup should remove only the stale entry it observed");
    }

    [Fact]
    public void Remove_IsIdempotent() {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);

        store.Remove(new FrontComposerTokenSession("session-1", "user-1", "acme"));
        store.Set(new FrontComposerTokenSession("session-1", "user-1", "acme"), SensitiveToken, FixedNow.AddMinutes(5));
        store.Remove(new FrontComposerTokenSession("session-1", "user-1", "acme"));
        store.Remove(new FrontComposerTokenSession("session-1", "user-1", "acme"));

        store.TryGet(new FrontComposerTokenSession("session-1", "user-1", "acme"), out _).ShouldBeFalse();
    }

    [Fact]
    public void Set_PurgesExpiredAbandonedSessions_WhenAnotherSessionSignsIn()
    {
        FakeTimeProvider time = new(FixedNow);
        FrontComposerUserTokenStore store = new(time);
        store.Set(new("abandoned", "shared", "acme"), SensitiveToken, FixedNow.AddMinutes(1));
        time.Advance(TimeSpan.FromMinutes(1));
        store.Set(new("new-session", "shared", "acme"), "new-token", FixedNow.AddMinutes(5));
        // Verify retention without looking up the expired key (which would itself perform cleanup).
        object entries = typeof(FrontComposerUserTokenStore).GetField("_tokens",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(store)!;
        ((System.Collections.ICollection)entries).Count.ShouldBe(1);
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

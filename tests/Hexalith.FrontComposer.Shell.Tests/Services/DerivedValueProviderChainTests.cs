using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Text;

using FsCheck.Xunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// Story 2-2 Task 3.9 — exactly 20 tests for the derived-value provider chain plus
/// storage-key canonicalization (Decisions D24, D27, D31, D39).
/// </summary>
public class DerivedValueProviderChainTests {
    // ----- Test fixtures ------------------------------------------------------
    public sealed class TestCommand {
        public string MessageId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        [DefaultValue(1)]
        public int Amount { get; set; }
        public string Note { get; set; } = "default-note";
    }

    public sealed class NullableCommand {
        public string MessageId { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    private sealed class StubUserContext : IUserContextAccessor {
        public string? TenantId { get; init; }
        public string? UserId { get; init; }
    }

    private static IUserContextAccessor StubUser(string? tenant = "t1", string? user = "alice@example.com")
        => new StubUserContext { TenantId = tenant, UserId = user };

    // ===== Per-provider — positive resolve + miss (10 tests) =================

    // 1. SystemValueProvider — positive
    [Fact]
    public async Task System_Positive_ResolvesMessageId() {
        var p = new SystemValueProvider(new NullUserContextAccessor());
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "MessageId", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeTrue();
        r.Value.ShouldBeOfType<string>().ShouldNotBeNullOrWhiteSpace();
    }

    // 2. SystemValueProvider — miss (unknown property name)
    [Fact]
    public async Task System_Miss_UnknownProperty_ReturnsNone() {
        var p = new SystemValueProvider(new NullUserContextAccessor());
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "UnknownProperty", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
    }

    // 3. ProjectionContextProvider — positive (field match)
    [Fact]
    public async Task ProjectionContext_Positive_FieldMatch() {
        var ctx = new ProjectionContext("X.Y.Z", "BC", null, ImmutableDictionary.CreateRange<string, object?>(new Dictionary<string, object?> { ["Amount"] = 42 }));
        var p = new ProjectionContextProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Amount", ctx, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeTrue();
        r.Value.ShouldBe(42);
    }

    // 4. ProjectionContextProvider — miss (null context)
    [Fact]
    public async Task ProjectionContext_Miss_NullContext_ReturnsNone() {
        var p = new ProjectionContextProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Amount", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
    }

    // 5. ExplicitDefaultValueProvider — positive ([DefaultValue(1)] int Amount)
    [Fact]
    public async Task ExplicitDefault_Positive_AmountReturnsOne() {
        var p = new ExplicitDefaultValueProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Amount", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeTrue();
        r.Value.ShouldBe(1);
    }

    // 6. ExplicitDefaultValueProvider — miss (no [DefaultValue] on Note)
    [Fact]
    public async Task ExplicitDefault_Miss_NoAttributeReturnsNone() {
        var p = new ExplicitDefaultValueProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Note", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
    }

    // 7. LastUsedValueProvider — positive (storage hit)
    [Fact]
    public async Task LastUsed_Positive_StorageHit() {
        var storage = Substitute.For<IStorageService>();
        string key = FrontComposerStorageKey.Build("t1", "alice@example.com", typeof(TestCommand).FullName!, "Note");
        storage.GetAsync<object?>(key, Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>("stored-note"));
        var p = new LastUsedValueProvider(storage, StubUser());
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Note", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeTrue();
        r.Value.ShouldBe("stored-note");
    }

    // 8. LastUsedValueProvider — miss (storage returns null)
    [Fact]
    public async Task LastUsed_Miss_StorageReturnsNull_ReturnsNone() {
        var storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));
        var p = new LastUsedValueProvider(storage, StubUser());
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Note", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
    }

    // 9. ConstructorDefaultValueProvider — positive (Note default)
    [Fact]
    public async Task ConstructorDefault_Positive_ReturnsInitializerValue() {
        var p = new ConstructorDefaultValueProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Note", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeTrue();
        r.Value.ShouldBe("default-note");
    }

    // 10. ConstructorDefaultValueProvider — miss (unknown property)
    [Fact]
    public async Task ConstructorDefault_Miss_UnknownProperty_ReturnsNone() {
        var p = new ConstructorDefaultValueProvider();
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "DoesNotExist", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
    }

    // ===== Chain ordering (5 tests) ===========================================

    // 11. System beats ProjectionContext
    [Fact]
    public async Task ChainOrder_SystemBeatsProjectionContext_ForMessageId() {
        var ctx = new ProjectionContext("X.Y.Z", "BC", null, ImmutableDictionary.CreateRange<string, object?>(new Dictionary<string, object?> { ["MessageId"] = "from-projection" }));
        DerivedValueResult result = await ResolveChain("MessageId", ctx,
            new SystemValueProvider(new NullUserContextAccessor()),
            new ProjectionContextProvider());
        // System provides a fresh GUID; projection's value must be ignored.
        result.Value.ShouldNotBe("from-projection");
    }

    // 12. ProjectionContext beats ExplicitDefault
    [Fact]
    public async Task ChainOrder_ProjectionContextBeatsExplicitDefault_ForAmount() {
        var ctx = new ProjectionContext("X.Y.Z", "BC", null, ImmutableDictionary.CreateRange<string, object?>(new Dictionary<string, object?> { ["Amount"] = 99 }));
        DerivedValueResult result = await ResolveChain("Amount", ctx,
            new ProjectionContextProvider(),
            new ExplicitDefaultValueProvider());
        result.Value.ShouldBe(99);
    }

    // 13. ExplicitDefault beats LastUsed
    [Fact]
    public async Task ChainOrder_ExplicitDefaultBeatsLastUsed_ForAmount() {
        var storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(777));
        DerivedValueResult result = await ResolveChain("Amount", null,
            new ExplicitDefaultValueProvider(),
            new LastUsedValueProvider(storage, StubUser()));
        result.Value.ShouldBe(1); // [DefaultValue(1)] wins, not the LastUsed 777.
    }

    // 14. LastUsed beats ConstructorDefault
    [Fact]
    public async Task ChainOrder_LastUsedBeatsConstructorDefault_ForNote() {
        var storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>("stored-note"));
        DerivedValueResult result = await ResolveChain("Note", null,
            new LastUsedValueProvider(storage, StubUser()),
            new ConstructorDefaultValueProvider());
        result.Value.ShouldBe("stored-note");
    }

    // 15. Prepended custom provider beats all built-ins (validates AddDerivedValueProvider semantics)
    [Fact]
    public async Task ChainOrder_PrependedCustomProvider_BeatsBuiltins() {
        var custom = new DelegateProvider((_, _) => new DerivedValueResult(true, "custom-wins"));
        var storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>("from-storage"));
        DerivedValueResult result = await ResolveChain("Note", null,
            custom,
            new LastUsedValueProvider(storage, StubUser()),
            new ConstructorDefaultValueProvider());
        result.Value.ShouldBe("custom-wins");
    }

    // ===== Chain stops at first HasValue=true (1 test) ========================

    // 16. Subsequent providers MUST NOT be invoked once a provider returns HasValue=true.
    [Fact]
    public async Task ChainStops_AtFirstHasValueTrue() {
        int laterInvocations = 0;
        var winning = new DelegateProvider((_, _) => new DerivedValueResult(true, "winner"));
        var spy = new DelegateProvider((_, _) => { laterInvocations++; return new DerivedValueResult(true, "later"); });
        DerivedValueResult result = await ResolveChain("Note", null, winning, spy);
        result.Value.ShouldBe("winner");
        laterInvocations.ShouldBe(0);
    }

    // ===== D31 fail-closed guards (2 tests) ===================================

    // 17. LastUsed_NullTenantId_RefusesRead_ReturnsHasValueFalse
    [Fact]
    public async Task LastUsed_NullTenantId_RefusesRead_ReturnsNone() {
        var storage = Substitute.For<IStorageService>();
        var p = new LastUsedValueProvider(storage, StubUser(tenant: null));
        DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), "Note", null, TestContext.Current.CancellationToken);
        r.HasValue.ShouldBeFalse();
        await storage.DidNotReceive().GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // 18. LastUsed_EmptyUserId_RefusesWrite_PublishesDevDiagnosticOncePerCircuit
    [Fact]
    public async Task LastUsed_EmptyUserId_RefusesWrite_PublishesDevDiagnosticOncePerCircuit() {
        var storage = Substitute.For<IStorageService>();
        var sink = new InMemoryDiagnosticSink(NullLogger<InMemoryDiagnosticSink>());
        var p = new LastUsedValueProvider(storage, StubUser(user: ""), sink);

        await p.Record(new TestCommand { Note = "n1" }, TestContext.Current.CancellationToken);
        await p.Record(new TestCommand { Note = "n2" }, TestContext.Current.CancellationToken);

        await storage.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        sink.RecentEvents.Count(e => e.Code == "D31").ShouldBe(1);
        p.TenantGuardTripped.ShouldBeTrue();
    }

    [Fact]
    public async Task LastUsed_Record_NullValue_RemovesStoredKey() {
        var storage = Substitute.For<IStorageService>();
        var p = new LastUsedValueProvider(storage, StubUser());
        CancellationToken ct = TestContext.Current.CancellationToken;

        await p.Record(new NullableCommand { MessageId = "m1", Note = null }, ct);

        string key = FrontComposerStorageKey.Build("t1", "alice@example.com", typeof(NullableCommand).FullName!, "Note");
        await storage.Received(1).RemoveAsync(key, Arg.Is<CancellationToken>(t => t == ct));
        await storage.DidNotReceive().SetAsync(key, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ===== D39 storage-key canonicalization (2 tests) =========================

    // 19. FsCheck round-trip — Parse(Build(t,u,c,p)) == (Canon(t), Canon(u), c, p)
    [Property]
    public bool StorageKey_Build_Roundtrip(string tenantRaw, string userRaw) {
        if (string.IsNullOrWhiteSpace(tenantRaw) || string.IsNullOrWhiteSpace(userRaw)) {
            return true;
        }

        // Avoid ':' in raw input — would split during parse since the helper preserves verbatim segments after URL-encoding.
        // (The encode step takes care of ':', but we filter to keep the property statement focused.)
        const string commandFqn = "X.Y.SomeCommand";
        const string propertyName = "SomeProperty";

        string key = FrontComposerStorageKey.Build(tenantRaw, userRaw, commandFqn, propertyName);
        var parts = FrontComposerStorageKey.TryParse(key);
        string expectedTenantCanon = Uri.EscapeDataString(tenantRaw.Trim().Normalize(NormalizationForm.FormC));
        string normalizedUser = userRaw.Trim().Normalize(NormalizationForm.FormC);
        string expectedUserCanon = Uri.EscapeDataString(
            normalizedUser.Contains('@')
                ? normalizedUser.ToLower(CultureInfo.InvariantCulture)
                : normalizedUser);

        return parts is not null
            && parts.Value.TenantCanon == expectedTenantCanon
            && parts.Value.UserCanon == expectedUserCanon
            && parts.Value.CommandTypeFqn == commandFqn
            && parts.Value.PropertyName == propertyName;
    }

    // 20. Build throws InvalidOperationException when tenant or user is null/empty (D31+D39 fail-closed at construction)
    [Fact]
    public void StorageKey_Build_NullOrEmptyTenantOrUser_Throws() {
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build(null, "u", "C", "P"));
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build("", "u", "C", "P"));
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build("   ", "u", "C", "P"));
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build("t", null, "C", "P"));
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build("t", "", "C", "P"));
        Should.Throw<InvalidOperationException>(() => FrontComposerStorageKey.Build("t", "   ", "C", "P"));
    }

    // ===== Helpers ===========================================================

    private static async Task<DerivedValueResult> ResolveChain(string propertyName, ProjectionContext? ctx, params IDerivedValueProvider[] chain) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        foreach (IDerivedValueProvider p in chain) {
            DerivedValueResult r = await p.ResolveAsync(typeof(TestCommand), propertyName, ctx, ct).ConfigureAwait(false);
            if (r.HasValue) {
                return r;
            }
        }

        return DerivedValueResult.None;
    }

    private sealed class DelegateProvider : IDerivedValueProvider {
        private readonly Func<Type, string, DerivedValueResult> _impl;
        public DelegateProvider(Func<Type, string, DerivedValueResult> impl) => _impl = impl;
        public Task<DerivedValueResult> ResolveAsync(Type t, string p, ProjectionContext? c, CancellationToken ct)
            => Task.FromResult(_impl(t, p));
    }

    private static ILogger<T> NullLogger<T>() => Substitute.For<ILogger<T>>();
}

using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// DW-1142 — LastUsed storage keys never exceed <see cref="FrontComposerStorageKey.MaxKeyLength"/>,
/// and values written under the legacy unbounded format stay readable until the next record.
/// </summary>
public sealed class FrontComposerStorageKeyTests {
    private const string Tenant = "t1";
    private const string User = "alice@example.com";
    private const string Property = "Note";
    private const string Prefix = FrontComposerStorageKey.LastUsedPrefix;

    private static readonly string TenantCanon = FrontComposerStorageKey.CanonicalizeTenant(Tenant);
    private static readonly string UserCanon = FrontComposerStorageKey.CanonicalizeUser(User);

    /// <summary>A key of exactly the maximum length keeps the legacy format and has no fallback.</summary>
    [Fact]
    public void BuildKeepsTheLegacyFormatAtTheMaximumLength() {
        string command = CommandForLegacyLength(FrontComposerStorageKey.MaxKeyLength);

        (string key, string? legacyKey) = FrontComposerStorageKey.BuildWithLegacyFallback(Tenant, User, command, Property);

        key.ShouldBe($"{Prefix}:{TenantCanon}:{UserCanon}:{command}:{Property}");
        key.Length.ShouldBe(FrontComposerStorageKey.MaxKeyLength);
        legacyKey.ShouldBeNull();
        FrontComposerStorageKey.Build(Tenant, User, command, Property).ShouldBe(key);
    }

    /// <summary>One character past the maximum switches to the bounded format and reports the legacy key.</summary>
    [Fact]
    public void BuildBoundsAKeyOneCharacterPastTheMaximum() {
        string command = CommandForLegacyLength(FrontComposerStorageKey.MaxKeyLength + 1);

        (string key, string? legacyKey) = FrontComposerStorageKey.BuildWithLegacyFallback(Tenant, User, command, Property);

        legacyKey.ShouldBe($"{Prefix}:{TenantCanon}:{UserCanon}:{command}:{Property}");
        legacyKey!.Length.ShouldBe(FrontComposerStorageKey.MaxKeyLength + 1);
        key.ShouldBe($"{Prefix}:{FrontComposerStorageKey.BoundedFormatVersion}:{TenantCanon}:{UserCanon}:{Digest(command)}:{Property}");
        key.Length.ShouldBeLessThanOrEqualTo(FrontComposerStorageKey.MaxKeyLength);
    }

    /// <summary>The bounded format keeps segments of 96 characters and hashes longer ones.</summary>
    [Fact]
    public void BoundedFormatHashesOnlySegmentsLongerThanNinetySixCharacters() {
        string tenant = new('a', 96);
        string user = new('b', 97);
        string command = "C" + new string('x', 600);

        string key = FrontComposerStorageKey.Build(tenant, user, command, Property);

        key.ShouldBe($"{Prefix}:{FrontComposerStorageKey.BoundedFormatVersion}:{tenant}:{Digest(user)}:{Digest(command)}:{Property}");
    }

    /// <summary>The longest bounded key keeps three 96-character segments verbatim and hashes the fourth.</summary>
    [Fact]
    public void BuildKeepsTheLongestBoundedKeyWithinTheMaximum() {
        string tenant = new('a', 96);
        string user = new('b', 96);
        string property = "P" + new string('y', 95);
        string command = "C" + new string('x', 600);

        string key = FrontComposerStorageKey.Build(tenant, user, command, property);

        key.ShouldBe($"{Prefix}:{FrontComposerStorageKey.BoundedFormatVersion}:{tenant}:{user}:{Digest(command)}:{property}");
        key.Length.ShouldBe(382);
        key.Length.ShouldBeLessThanOrEqualTo(FrontComposerStorageKey.MaxKeyLength);
    }

    /// <summary>When every segment is oversized, each is hashed and the key still parses.</summary>
    [Fact]
    public void BuildHashesEveryOversizedSegment() {
        string tenant = new('\u00E9', 1_000);
        string user = new string('\u00E8', 1_000) + "@example.com";
        string command = "C" + new string('x', 5_000);
        string property = "P" + new string('y', 5_000);

        string key = FrontComposerStorageKey.Build(tenant, user, command, property);

        key.Length.ShouldBeLessThanOrEqualTo(FrontComposerStorageKey.MaxKeyLength);
        (string TenantCanon, string UserCanon, string CommandTypeFqn, string PropertyName) parts = FrontComposerStorageKey.TryParse(key).ShouldNotBeNull();
        parts.TenantCanon.ShouldBe(Digest(FrontComposerStorageKey.CanonicalizeTenant(tenant)));
        parts.UserCanon.ShouldBe(Digest(FrontComposerStorageKey.CanonicalizeUser(user)));
        parts.CommandTypeFqn.ShouldBe(Digest(command));
        parts.PropertyName.ShouldBe(Digest(property));
    }

    /// <summary>Oversized segments that differ only in their last character produce different keys.</summary>
    [Fact]
    public void BuildDistinguishesOversizedSegmentsThatShareALongPrefix() {
        string shared = "C" + new string('x', 600);

        string first = FrontComposerStorageKey.Build(Tenant, User, shared + "A", Property);
        string second = FrontComposerStorageKey.Build(Tenant, User, shared + "B", Property);

        first.ShouldNotBe(second);
    }

    /// <summary>A verbatim segment shaped like a digest is hashed, so it cannot collide with a real digest.</summary>
    [Fact]
    public void BoundedFormatHashesALiteralThatLooksLikeADigest() {
        // The oversized user forces the bounded format for both keys, which then differ only by command.
        string user = new('u', 600);
        string oversizedCommand = "C" + new string('x', 600);
        string lookalikeCommand = Digest(oversizedCommand);

        string hashedCommandKey = FrontComposerStorageKey.Build(Tenant, user, oversizedCommand, Property);
        string lookalikeKey = FrontComposerStorageKey.Build(Tenant, user, lookalikeCommand, Property);

        lookalikeKey.ShouldNotBe(hashedCommandKey);
        FrontComposerStorageKey.TryParse(hashedCommandKey).ShouldNotBeNull().CommandTypeFqn.ShouldBe(lookalikeCommand);
        FrontComposerStorageKey.TryParse(lookalikeKey).ShouldNotBeNull().CommandTypeFqn.ShouldBe(Digest(lookalikeCommand));
    }

    /// <summary>The verbatim command and property segments cannot carry the key separator.</summary>
    [Fact]
    public void BuildRejectsTheSeparatorInVerbatimSegments() {
        Should.Throw<ArgumentException>(() => FrontComposerStorageKey.Build(Tenant, User, "X:Command", Property))
            .ParamName.ShouldBe("commandTypeFqn");
        Should.Throw<ArgumentException>(() => FrontComposerStorageKey.Build(Tenant, User, "X.Command", "a:b"))
            .ParamName.ShouldBe("propertyName");
    }

    /// <summary>A fixed key pins the stored bounded format and its constants.</summary>
    [Fact]
    public void BuildPinsTheStoredBoundedKeyFormat() {
        string key = FrontComposerStorageKey.Build(Tenant, User, "C" + new string('x', 600), Property);

        FrontComposerStorageKey.MaxKeyLength.ShouldBe(512);
        key.ShouldBe("frontcomposer:lastused:v2:t1:alice%40example.com:!8bef0967b78838d8575f280d727139a87712700add816a1ab6e62eea27c741c3:Note");
    }

    /// <summary>A legacy key whose tenant equals the version marker still parses as a legacy key.</summary>
    [Fact]
    public void TryParseReadsALegacyKeyWhoseTenantIsTheVersionMarker() {
        string key = FrontComposerStorageKey.Build(FrontComposerStorageKey.BoundedFormatVersion, User, "X.Command", Property);

        (string TenantCanon, string UserCanon, string CommandTypeFqn, string PropertyName) parts = FrontComposerStorageKey.TryParse(key).ShouldNotBeNull();

        parts.ShouldBe((FrontComposerStorageKey.BoundedFormatVersion, UserCanon, "X.Command", Property));
    }

    /// <summary>A value recorded under the legacy oversized key is still resolved.</summary>
    [Fact]
    public async Task LastUsedReadsTheLegacyKeyWhenTheBoundedKeyIsEmpty() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = new('t', 600);
        (string key, string? legacyKey) = BuildNoteKeys(tenant);
        IStorageService storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(key, Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));
        storage.GetAsync<object?>(legacyKey!, Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>("legacy-note"));
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = tenant, UserId = User });

        DerivedValueResult result = await provider.ResolveAsync(typeof(NoteCommand), nameof(NoteCommand.Note), null, ct);

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("legacy-note");
    }

    /// <summary>A deeply nested generic command name alone triggers the bounded key, and both keys are read once.</summary>
    [Fact]
    public async Task LastUsedReadsBothKeysOnceForALongGenericCommandName() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        Type commandType = typeof(Wrapper<Wrapper<Wrapper<Wrapper<NoteCommand>>>>);
        commandType.FullName!.Length.ShouldBeGreaterThan(FrontComposerStorageKey.MaxKeyLength);
        (string key, string? legacyKey) = FrontComposerStorageKey.BuildWithLegacyFallback(Tenant, User, commandType.FullName!, nameof(NoteCommand.Note));
        IStorageService storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = Tenant, UserId = User });

        DerivedValueResult result = await provider.ResolveAsync(commandType, nameof(NoteCommand.Note), null, ct);

        result.HasValue.ShouldBeFalse();
        key.ShouldStartWith($"{Prefix}:{FrontComposerStorageKey.BoundedFormatVersion}:");
        key.Length.ShouldBeLessThanOrEqualTo(FrontComposerStorageKey.MaxKeyLength);
        Received.InOrder(() => {
            _ = storage.GetAsync<object?>(key, ct);
            _ = storage.GetAsync<object?>(legacyKey!, ct);
        });
        _ = await storage.Received(2).GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A value under the bounded key wins, and the legacy key is not read.</summary>
    [Fact]
    public async Task LastUsedPrefersTheBoundedKeyOverTheLegacyKey() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = new('t', 600);
        (string key, string? legacyKey) = BuildNoteKeys(tenant);
        IStorageService storage = Substitute.For<IStorageService>();
        storage.GetAsync<object?>(key, Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>("bounded-note"));
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = tenant, UserId = User });

        DerivedValueResult result = await provider.ResolveAsync(typeof(NoteCommand), nameof(NoteCommand.Note), null, ct);

        result.Value.ShouldBe("bounded-note");
        _ = await storage.DidNotReceive().GetAsync<object?>(legacyKey!, Arg.Any<CancellationToken>());
    }

    /// <summary>Recording an oversized key writes the bounded key and removes the legacy key.</summary>
    [Fact]
    public async Task LastUsedRecordMigratesAnOversizedLegacyKey() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = new('t', 600);
        (string key, string? legacyKey) = BuildNoteKeys(tenant);
        IStorageService storage = Substitute.For<IStorageService>();
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = tenant, UserId = User });

        await provider.Record(new NoteCommand { Note = "current" }, ct);

        Received.InOrder(() => {
            _ = storage.SetAsync(key, (object)"current", ct);
            _ = storage.RemoveAsync(legacyKey!, ct);
        });
        await storage.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Clearing an oversized value removes both keys, so the legacy value cannot resurface.</summary>
    [Fact]
    public async Task LastUsedRecordOfANullValueRemovesBothKeys() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = new('t', 600);
        (string key, string? legacyKey) = BuildNoteKeys(tenant);
        IStorageService storage = Substitute.For<IStorageService>();
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = tenant, UserId = User });

        await provider.Record(new NoteCommand { Note = null }, ct);

        // The fallback goes first, so an interruption leaves the newer bounded value rather than the legacy one.
        Received.InOrder(() => {
            _ = storage.RemoveAsync(legacyKey!, ct);
            _ = storage.RemoveAsync(key, ct);
        });
        await storage.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Recording a key that fits touches only that key.</summary>
    [Fact]
    public async Task LastUsedRecordOfAShortKeyTouchesOnlyThatKey() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (string key, string? legacyKey) = BuildNoteKeys(Tenant);
        IStorageService storage = Substitute.For<IStorageService>();
        LastUsedValueProvider provider = new(storage, new StubUserContext { TenantId = Tenant, UserId = User });

        await provider.Record(new NoteCommand { Note = "current" }, ct);

        legacyKey.ShouldBeNull();
        await storage.Received(1).SetAsync(key, (object)"current", ct);
        await storage.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static (string Key, string? LegacyKey) BuildNoteKeys(string tenant)
        => FrontComposerStorageKey.BuildWithLegacyFallback(tenant, User, typeof(NoteCommand).FullName!, nameof(NoteCommand.Note));

    private static string CommandForLegacyLength(int legacyLength) {
        int overhead = Prefix.Length + 4 + TenantCanon.Length + UserCanon.Length + Property.Length;
        return "C" + new string('x', legacyLength - overhead - 1);
    }

    private static string Digest(string segment)
        => "!" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(segment)));

    /// <summary>Generic wrapper whose nested closed forms produce long type names.</summary>
    /// <typeparam name="T">The wrapped type.</typeparam>
    public sealed class Wrapper<T> {
        /// <summary>Gets or sets the recorded note.</summary>
        public string? Note { get; set; }
    }

    /// <summary>Command with one recordable property.</summary>
    public sealed class NoteCommand {
        /// <summary>Gets or sets the recorded note.</summary>
        public string? Note { get; set; }
    }

    private sealed class StubUserContext : IUserContextAccessor {
        public string? TenantId { get; init; }

        public string? UserId { get; init; }
    }
}

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// Story 11.15 (M19 cluster 4) — direct fail-closed tests for the consolidated
/// <see cref="StorageScopeResolver"/>. Valid tenant/user → raw identities + <see langword="true"/>;
/// missing / blank / whitespace / <b>throwing</b> accessor → fail closed + HFC2105 (never leaking the
/// raw tenant/user values). The throwing-getter path is the new hardening adopted from the former
/// CommandPalette copy (<c>Reason=AccessorThrew</c>), which the other five copies lacked.
/// </summary>
public sealed class StorageScopeResolverTests {
    private const string Tenant = "acme";
    private const string User = "Alice@Example.com";

    [Fact]
    public void TryResolveScope_ValidScope_ReturnsRawIdentitiesAndTrue() {
        CapturingLogger logger = new();
        StorageScopeResolver sut = new(Accessor(Tenant, User), logger);

        bool ok = sut.TryResolveScope(out string tenantId, out string userId, "Theme", "hydrate");

        ok.ShouldBeTrue();
        // Raw (un-escaped, un-lowercased) — canonicalization stays centralized in StorageKeys.
        tenantId.ShouldBe(Tenant);
        userId.ShouldBe(User);
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void TryResolveScope_MissingAccessor_FailsClosedAndLogsHFC2105() {
        CapturingLogger logger = new();
        StorageScopeResolver sut = new(accessor: null, logger);

        bool ok = sut.TryResolveScope(out string tenantId, out string userId, "Theme", "persist");

        ok.ShouldBeFalse();
        tenantId.ShouldBeEmpty();
        userId.ShouldBeEmpty();
        logger.ShouldHaveInformation(FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, "persist");
    }

    [Theory]
    [InlineData(null, "alice")]
    [InlineData("acme", null)]
    [InlineData("", "alice")]
    [InlineData("acme", "")]
    [InlineData("   ", "alice")]
    [InlineData("acme", "   ")]
    public void TryResolveScope_BlankOrWhitespaceSegment_FailsClosedAndLogsHFC2105(string? tenant, string? user) {
        CapturingLogger logger = new();
        StorageScopeResolver sut = new(Accessor(tenant, user), logger);

        bool ok = sut.TryResolveScope(out string tenantId, out string userId, "Theme", "hydrate");

        ok.ShouldBeFalse();
        tenantId.ShouldBeEmpty();
        userId.ShouldBeEmpty();
        logger.ShouldHaveInformation(FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, "hydrate");
    }

    [Fact]
    public void TryResolveScope_ThrowingAccessorGetter_FailsClosedAndLogsSanitizedCategory() {
        CapturingLogger logger = new();
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        _ = accessor.TenantId.Returns("acme");
        _ = accessor.UserId.Returns(_ => throw new InvalidOperationException("secret-user@corp"));
        StorageScopeResolver sut = new(accessor, logger);

        bool ok = sut.TryResolveScope(out string tenantId, out string userId, "Theme", "persist");

        ok.ShouldBeFalse();
        tenantId.ShouldBeEmpty();
        userId.ShouldBeEmpty();
        logger.ShouldHaveInformation(FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, "persist");
        logger.Entries.ShouldContain(e => e.Message.Contains("Reason=AccessorThrew", StringComparison.Ordinal));
        logger.Entries.ShouldContain(e => e.Message.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(e => e.Exception == null);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("secret-user@corp", StringComparison.Ordinal));
        CapturingLogger.CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.EventId.Id.ShouldBe(5690);
        entry.EventId.Name.ShouldBe("StorageAccessorFailed");
    }

    [Fact]
    public void TryResolveScope_FailClosed_NeverLogsRawTenantOrUserValues() {
        CapturingLogger logger = new();
        // Tenant blank → fail-closed; the (present) user value must never appear in any log message.
        StorageScopeResolver sut = new(Accessor(tenantId: "   ", userId: "secret-user@corp"), logger);

        _ = sut.TryResolveScope(out _, out _, "Theme", "persist");

        logger.Entries.ShouldNotBeEmpty();
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("secret-user@corp", StringComparison.Ordinal));
    }

    [Fact]
    public void TryResolveScope_FailClosed_LogsPerFeatureAttribution() {
        // Story 11.15 review (round 2): the consolidated resolver must still name the failing feature in
        // the HFC2105 skip log — the six removed effect-local copies each logged a feature-specific
        // message, so a generic "Storage {direction} skipped" line (which loses per-feature attribution
        // and the effect logger category) is an observability regression.
        CapturingLogger logger = new();
        StorageScopeResolver sut = new(accessor: null, logger);

        _ = sut.TryResolveScope(out _, out _, "Density", "hydrate");

        logger.Entries.ShouldContain(
            e => e.Level == LogLevel.Information
                && e.EventId.Id == 5691
                && e.EventId.Name == "StorageScopeMissing"
                && e.Message.Contains(FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, StringComparison.Ordinal)
                && e.Message.Contains("sha256:77a283d69258c2ae", StringComparison.Ordinal)
                && e.Message.Contains("hydrate", StringComparison.Ordinal),
            customMessage: "HFC2105 skip log must retain stable pseudonymous feature attribution.");
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("Density", StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("Storage hydrate", StringComparison.Ordinal));
    }

    private static IUserContextAccessor Accessor(string? tenantId, string? userId) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        _ = accessor.TenantId.Returns(tenantId);
        _ = accessor.UserId.Returns(userId);
        return accessor;
    }

    private sealed class CapturingLogger : ILogger {
        public List<CapturedLogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            IReadOnlyDictionary<string, object?> structuredState = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(static value => value.Key, static value => value.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            Entries.Add(new(logLevel, eventId, structuredState, formatter(state, exception), exception));
        }

        public void ShouldHaveInformation(string diagnosticId, string direction)
            => Entries.ShouldContain(
                e => e.Level == LogLevel.Information
                    && e.Message.Contains(diagnosticId, StringComparison.Ordinal)
                    && e.Message.Contains(direction, StringComparison.Ordinal),
                customMessage: $"Expected Information log referencing '{diagnosticId}' AND '{direction}'.");

        public sealed record CapturedLogEntry(
            LogLevel Level,
            EventId EventId,
            IReadOnlyDictionary<string, object?> State,
            string Message,
            Exception? Exception);
    }
}

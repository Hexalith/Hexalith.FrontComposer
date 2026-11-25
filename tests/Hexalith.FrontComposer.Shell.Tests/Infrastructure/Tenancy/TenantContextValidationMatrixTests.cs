using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Microsoft.Extensions.Logging;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

public sealed class TenantContextValidationMatrixTests {
    [Fact]
    public void TryGetContext_ValidTenantAndUser_PreservesCaseAndReturnsSnapshot() {
        FrontComposerTenantContextAccessor sut = NewAccessor("Acme_Corp", "User-A");

        TenantContextResult result = sut.TryGetContext("Acme_Corp", "test");

        result.Succeeded.ShouldBeTrue();
        result.Context.ShouldNotBeNull();
        result.Context!.TenantId.ShouldBe("Acme_Corp");
        result.Context.UserId.ShouldBe("User-A");
        result.Context.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void TryGetContextAcceptsSegmentsAtTheirLengthLimits() {
        string tenant = new('t', 64);
        string user = new('u', 1024);

        TenantContextSnapshot snapshot = NewAccessor(tenant, user).TryGetContext(tenant).EnsureSuccess();

        snapshot.TenantId.ShouldBe(tenant);
        snapshot.UserId.ShouldBe(user);
    }

    [Theory]
    [InlineData(65, 8, 8, 't')]
    [InlineData(8, 1025, 8, 't')]
    [InlineData(8, 8, 65, 't')]
    [InlineData(8, 8, 65, ' ')]
    public void TryGetContextRejectsAnyOversizedSegment(
        int tenantLength, int userLength, int requestedLength, char requestedCharacter) {
        FrontComposerTenantContextAccessor sut = NewAccessor(new string('t', tenantLength), new string('u', userLength));

        TenantContextResult result = sut.TryGetContext(new string(requestedCharacter, requestedLength));

        result.FailureCategory.ShouldBe(TenantContextFailureCategory.MalformedSegment);
        result.Context.ShouldBeNull();
    }

    [Fact]
    public void TryGetContext_MixedCaseTenantMismatch_FailsOrdinally() {
        FrontComposerTenantContextAccessor sut = NewAccessor("Acme", "alice");

        TenantContextResult result = sut.TryGetContext("acme", "test");

        result.Succeeded.ShouldBeFalse();
        result.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMismatch);
    }

    [Fact]
    public void TryGetContextIssues128BitHexCorrelationIdsForSuccessAndFailure() {
        FrontComposerTenantContextAccessor sut = NewAccessor("tenant", "user");
        TenantContextResult success = sut.TryGetContext("tenant");
        TenantContextResult failure = sut.TryGetContext("other-tenant");

        success.CorrelationId.ShouldMatch("^[0-9A-F]{32}$");
        failure.CorrelationId.ShouldMatch("^[0-9A-F]{32}$");
        success.EnsureSuccess().CorrelationId.ShouldBe(success.CorrelationId);
        Should.Throw<TenantContextException>(() => failure.EnsureSuccess()).CorrelationId.ShouldBe(failure.CorrelationId);
    }

    [Theory]
    [InlineData(null, "user", TenantContextFailureCategory.TenantMissing)]
    [InlineData("   ", "user", TenantContextFailureCategory.TenantMissing)]
    [InlineData("tenant", null, TenantContextFailureCategory.UserMissing)]
    [InlineData("tenant", " ", TenantContextFailureCategory.UserMissing)]
    [InlineData("tenant:evil", "user", TenantContextFailureCategory.MalformedSegment)]
    [InlineData("tenant", "user\u0001evil", TenantContextFailureCategory.MalformedSegment)]
    public void TryGetContext_InvalidTenantOrUser_FailsClosed(
        string? tenant,
        string? user,
        TenantContextFailureCategory expected) {
        FrontComposerTenantContextAccessor sut = NewAccessor(tenant, user);

        TenantContextResult result = sut.TryGetContext(operationKind: "test");

        result.Succeeded.ShouldBeFalse();
        result.FailureCategory.ShouldBe(expected);
    }

    [Fact]
    public void TryGetContext_SyntheticTenantRejectedUnlessExplicitlyAllowed() {
        FrontComposerTenantContextAccessor production = NewAccessor("counter-demo", "demo-user");
        FrontComposerTenantContextAccessor demo = NewAccessor("counter-demo", "demo-user", allowDemo: true);

        production.TryGetContext(operationKind: "test").FailureCategory
            .ShouldBe(TenantContextFailureCategory.SyntheticTenantRejected);
        demo.TryGetContext(operationKind: "test").Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void TryGetContext_FailureLogOmitsRawTenantAndUserValues() {
        CapturingLogger<FrontComposerTenantContextAccessor> logger = new();
        FrontComposerTenantContextAccessor sut = NewAccessor(
            "tenant-secret",
            "user-secret",
            logger: logger);

        TenantContextResult result = sut.TryGetContext("tenant-other-secret", "command-dispatch");

        result.Succeeded.ShouldBeFalse();
        logger.Entries.ShouldNotBeEmpty();
        foreach (string message in logger.Entries) {
            message.ShouldNotContain("tenant-secret");
            message.ShouldNotContain("tenant-other-secret");
            message.ShouldNotContain("user-secret");
            message.ShouldContain("TenantMismatch");
            message.ShouldContain("CorrelationId=");
        }
    }

    // DW-892 — the failure categories TryGetContext reports and their diagnostic ids, with distinctive
    // sentinel inputs for the redaction check. StaleTenantContext comes only from Revalidate.
    private static readonly (string? Tenant, string? User, string? RequestedTenant, string DiagnosticId, TenantContextFailureCategory Category)[] FailureCases =
    [
        (null, "user-sentinel", null, "HFC2015", TenantContextFailureCategory.TenantMissing),
        ("tenant-sentinel", null, null, "HFC2015", TenantContextFailureCategory.UserMissing),
        ("tenant-sentinel:evil", "user-sentinel", null, "HFC2016", TenantContextFailureCategory.MalformedSegment),
        ("tenant-sentinel", "user-sentinel", "other-sentinel", "HFC2017", TenantContextFailureCategory.TenantMismatch),
        ("counter-demo", "demo-user", null, "HFC2018", TenantContextFailureCategory.SyntheticTenantRejected),
    ];

    /// <summary>Gets the indexes of <see cref="FailureCases"/> for the theory below.</summary>
    public static TheoryData<int> FailureCaseIndexes => new(Enumerable.Range(0, FailureCases.Length));

    /// <summary>
    /// DW-892 — a TryGetContext failure logs one Warning (EventId 5650) under its category's diagnostic id,
    /// carries the returned correlation id, and omits the raw tenant and user values.
    /// </summary>
    /// <param name="index">The index into <see cref="FailureCases"/>.</param>
    [Theory]
    [MemberData(nameof(FailureCaseIndexes))]
    public void TryGetContextLogsTheDiagnosticIdOfEachFailureCategory(int index) {
        (string? tenant, string? user, string? requestedTenant, string diagnosticId, TenantContextFailureCategory category) = FailureCases[index];
        CapturingLogger<FrontComposerTenantContextAccessor> logger = new();
        FrontComposerTenantContextAccessor sut = NewAccessor(tenant, user, logger: logger);

        TenantContextResult result = sut.TryGetContext(requestedTenant, "test");

        result.FailureCategory.ShouldBe(category);
        AssertSingleBlockedEntry(logger, diagnosticId, category, result.CorrelationId);
        foreach (string? raw in new[] { tenant, user, requestedTenant }) {
            if (raw is not null) {
                logger.Entries[0].ShouldNotContain(raw, Case.Sensitive);
            }
        }
    }

    /// <summary>DW-892 — the failure cases cover every category TryGetContext can report.</summary>
    [Fact]
    public void FailureCasesCoverEveryCategoryTryGetContextReports() {
        HashSet<TenantContextFailureCategory> expected = [.. Enum.GetValues<TenantContextFailureCategory>()];
        _ = expected.Remove(TenantContextFailureCategory.None);
        _ = expected.Remove(TenantContextFailureCategory.StaleTenantContext);

        FailureCases.Select(failure => failure.Category).ToHashSet().SetEquals(expected).ShouldBeTrue();
    }

    /// <summary>A tenant or user change logs one HFC2019 warning with a fresh correlation id.</summary>
    [Theory]
    [InlineData("tenant", "alice")]
    [InlineData("other-tenant", "bob")]
    public void RevalidateLogsTheStaleContextDiagnosticId(string tenant, string user) {
        CapturingLogger<FrontComposerTenantContextAccessor> logger = new();
        FrontComposerTenantContextAccessor sut = NewAccessor(tenant, user, logger: logger);
        TenantContextSnapshot snapshot = new(
            "tenant",
            "bob",
            IsAuthenticated: true,
            CorrelationId: "earlier");

        TenantContextResult result = sut.Revalidate(snapshot, "test");

        result.FailureCategory.ShouldBe(TenantContextFailureCategory.StaleTenantContext);
        result.CorrelationId.ShouldNotBe("earlier");
        AssertSingleBlockedEntry(logger, "HFC2019", TenantContextFailureCategory.StaleTenantContext, result.CorrelationId);
    }

    /// <summary>DW-892 — a successful resolution, including an allowed demo tenant, logs nothing.</summary>
    /// <param name="tenant">The authenticated tenant.</param>
    /// <param name="user">The authenticated user.</param>
    /// <param name="allowDemo">Whether demo tenant contexts are allowed.</param>
    [Theory]
    [InlineData("tenant-sentinel", "user-sentinel", false)]
    [InlineData("counter-demo", "demo-user", true)]
    public void TryGetContextLogsNothingOnSuccess(string tenant, string user, bool allowDemo) {
        CapturingLogger<FrontComposerTenantContextAccessor> logger = new();

        NewAccessor(tenant, user, allowDemo, logger).TryGetContext(operationKind: "test").Succeeded.ShouldBeTrue();

        logger.Entries.ShouldBeEmpty();
    }

    private static void AssertSingleBlockedEntry(
        CapturingLogger<FrontComposerTenantContextAccessor> logger,
        string diagnosticId,
        TenantContextFailureCategory category,
        string correlationId) {
        string entry = logger.Entries.ShouldHaveSingleItem();
        entry.ShouldStartWith(diagnosticId + ": ", Case.Sensitive);
        entry.ShouldContain("FailureCategory=" + category, Case.Sensitive);
        entry.ShouldContain("CorrelationId=" + correlationId, Case.Sensitive);
        LogRecord record = logger.Records.ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.EventId.Id.ShouldBe(5650);
        record.State["DiagnosticId"].ShouldBe(diagnosticId);
        record.State["FailureCategory"].ShouldBe(category.ToString());
    }

    [Fact]
    public void ManifestGate_ReturnsNoContext_WhenTenantContextInvalid() {
        TenantScopedManifestGate gate = new TenantScopedManifestGate(NewAccessor(null, "user"));

        TenantContextResult result = gate.TryAuthorizeEnumeration();

        result.Succeeded.ShouldBeFalse();
        result.Context.ShouldBeNull();
    }

    private static FrontComposerTenantContextAccessor NewAccessor(
        string? tenant,
        string? user,
        bool allowDemo = false,
        CapturingLogger<FrontComposerTenantContextAccessor>? logger = null)
        => new(
            new TestUserContextAccessor(tenant, user),
            MsOptions.Create(new FcShellOptions { AllowDemoTenantContext = allowDemo }),
            logger ?? new CapturingLogger<FrontComposerTenantContextAccessor>());

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed record LogRecord(LogLevel Level, EventId EventId, IReadOnlyDictionary<string, object?> State);

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<string> Entries { get; } = [];

        public List<LogRecord> Records { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Entries.Add(formatter(state, exception));
            Dictionary<string, object?> values = [];
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs) {
                foreach (KeyValuePair<string, object?> pair in pairs) {
                    values[pair.Key] = pair.Value;
                }
            }

            Records.Add(new LogRecord(logLevel, eventId, values));
        }
    }
}

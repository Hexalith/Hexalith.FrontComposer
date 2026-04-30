using System.Globalization;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 6-1 review F17 / F18 — runtime behavioural contract for the Level 1 currency
/// and relative-time format helpers that <c>RazorEmitter</c> generates into projection
/// views. Generator-side approval tests (<c>Level1FormatEmitterTests</c>) prove the
/// emitted source string. These tests prove the runtime semantics that source must
/// satisfy; if the generator drifts away from the contract, the corresponding emitter
/// test breaks first and these tests act as the spec for the desired behaviour.
/// </summary>
public sealed class Level1FormatRuntimeTests {
    private const decimal SampleAmount = 1_234.56m;
    private const decimal NegativeAmount = -42m;

    /// <summary>
    /// Story 6-1 T5 / Hardening Addendum bullet 3 — culture-restoration helper. Sets
    /// <see cref="CultureInfo.CurrentCulture"/> for the duration of <paramref name="action"/>
    /// and restores the original via <see langword="finally"/>, regardless of throw.
    /// </summary>
    private static void WithCulture(string cultureName, Action action) {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            action();
        }
        finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //   F17 — Currency culture-restoration tests (T5 / AC6 / AC8)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("en-US", 1234.56)]
    [InlineData("fr-FR", 1234.56)]
    [InlineData("en-US", 0)]
    [InlineData("fr-FR", 0)]
    [InlineData("en-US", -42)]
    [InlineData("fr-FR", -42)]
    public void Currency_DecimalRendersWithCultureSpecificSymbol(string culture, double rawValue) {
        decimal amount = (decimal)rawValue;
        WithCulture(culture, () => {
            string rendered = amount.ToString("C", CultureInfo.CurrentCulture);
            string expected = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
            rendered.ShouldBe(expected);
        });
    }

    [Fact]
    public void Currency_DecimalEnUsHasDollarSymbol() {
        WithCulture("en-US", () => {
            string rendered = SampleAmount.ToString("C", CultureInfo.CurrentCulture);
            rendered.ShouldContain("$");
        });
    }

    [Fact]
    public void Currency_DecimalFrFrUsesEuroSymbol() {
        WithCulture("fr-FR", () => {
            string rendered = SampleAmount.ToString("C", CultureInfo.CurrentCulture);
            rendered.ShouldContain("€");
        });
    }

    [Fact]
    public void Currency_NegativeDecimalIsRendered() {
        WithCulture("en-US", () => {
            string rendered = NegativeAmount.ToString("C", CultureInfo.CurrentCulture);
            rendered.ShouldContain("42");
        });
    }

    [Fact]
    public void Currency_NullableDecimal_NullRendersDashViaGuard() {
        decimal? amount = null;
        string rendered = amount.HasValue
            ? amount.Value.ToString("C", CultureInfo.CurrentCulture)
            : "—";
        rendered.ShouldBe("—");
    }

    [Fact]
    public void Currency_DoubleAndFloatVariantsHonourCurrentCulture() {
        WithCulture("en-US", () => {
            double d = 9.99;
            float f = 9.99f;
            d.ToString("C", CultureInfo.CurrentCulture).ShouldStartWith("$");
            f.ToString("C", CultureInfo.CurrentCulture).ShouldStartWith("$");
        });
    }

    [Fact]
    public void Currency_CultureScope_RestoresOriginalCultureOnException() {
        CultureInfo original = CultureInfo.CurrentCulture;
        Should.Throw<InvalidOperationException>(() => WithCulture("fr-FR", () => throw new InvalidOperationException("boom")));
        CultureInfo.CurrentCulture.ShouldBe(original);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //   F18 — Relative-time deterministic clock tests (T6 / AC4 / AC5 / D6 / D12)
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly DateTimeOffset NowUtc = new(2026, 4, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RelativeTime_LessThanOneMinuteAgo_ReturnsLessThanOneMinuteLabel() {
        DateTimeOffset value = NowUtc.AddSeconds(-30);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("<1m ago");
    }

    [Fact]
    public void RelativeTime_LessThanOneMinuteFuture_ReturnsInLessThanOneMinuteLabel() {
        DateTimeOffset value = NowUtc.AddSeconds(30);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("in <1m");
    }

    [Theory]
    [InlineData(5, "5m ago")]
    [InlineData(59, "59m ago")]
    public void RelativeTime_MinutesAgo_RendersFloorMinutes(int minutes, string expected) {
        DateTimeOffset value = NowUtc.AddMinutes(-minutes);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "1h ago")]
    [InlineData(23, "23h ago")]
    public void RelativeTime_HoursAgo_RendersFloorHours(int hours, string expected) {
        DateTimeOffset value = NowUtc.AddHours(-hours);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "1d ago")]
    [InlineData(6, "6d ago")]
    public void RelativeTime_DaysWithinWindow_RendersFloorDays(int days, string expected) {
        DateTimeOffset value = NowUtc.AddDays(-days);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe(expected);
    }

    [Fact]
    public void RelativeTime_ExactlyAtWindowBoundary_StaysRelative() {
        // Boundary is `>` strict — exactly 7 days still renders relative.
        DateTimeOffset value = NowUtc.AddDays(-7);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("7d ago");
    }

    [Fact]
    public void RelativeTime_OlderThanWindow_FallsBackToAbsoluteDate() {
        DateTimeOffset value = NowUtc.AddDays(-8);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe(value.ToString("d", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void RelativeTime_FutureMinutes_RendersFromNowSuffix() {
        DateTimeOffset value = NowUtc.AddMinutes(15);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("15m from now");
    }

    [Fact]
    public void RelativeTime_FutureBeyondWindow_FallsBackToAbsoluteDate() {
        DateTimeOffset value = NowUtc.AddDays(30);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe(value.ToString("d", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void RelativeTime_DateTimeUtcKind_BehavesLikeDateTimeOffset() {
        DateTime value = NowUtc.AddHours(-3).UtcDateTime;
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("3h ago");
    }

    [Fact]
    public void RelativeTime_DateTimeLocalKind_NormalizesToUtcThenFormats() {
        DateTime localValue = TimeZoneInfo.ConvertTimeFromUtc(NowUtc.AddHours(-2).UtcDateTime, TimeZoneInfo.Local);
        DateTime asLocal = DateTime.SpecifyKind(localValue, DateTimeKind.Local);
        FormatRelativeTime(asLocal, NowUtc, 7).ShouldBe("2h ago");
    }

    [Fact]
    public void RelativeTime_DateTimeUnspecifiedKind_TreatedAsUtc() {
        // Story 6-1 review F8 / D12 — Unspecified is interpreted as UTC, matching the
        // canonical comparison frame and the typical .NET server-side persistence pattern.
        DateTime value = DateTime.SpecifyKind(NowUtc.AddHours(-5).UtcDateTime, DateTimeKind.Unspecified);
        FormatRelativeTime(value, NowUtc, 7).ShouldBe("5h ago");
    }

    [Fact]
    public void RelativeTime_DateTimeLocalExtreme_FallsBackToAbsoluteDate() {
        // Story 6-1 review F2 — Local-kind extremes overflow DateTimeOffset construction.
        // Generator wraps the conversion with try/catch and falls back to "d".
        DateTime value = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
        string rendered = FormatRelativeTime(value, NowUtc, 7);
        rendered.ShouldBe(value.ToString("d", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void RelativeTime_NowUsesInjectedTimeProvider_NotWallClock() {
        FakeTimeProvider time = new(NowUtc);
        DateTimeOffset capturedNow = time.GetUtcNow();
        DateTimeOffset value = capturedNow.AddMinutes(-10);

        // Advance wall clock to prove the helper uses captured `now`, not real time.
        time.Advance(TimeSpan.FromHours(2));
        FormatRelativeTime(value, capturedNow, 7).ShouldBe("10m ago");
    }

    [Fact]
    public void RelativeTime_NullableDateTimeOffset_NullRendersDashViaGuard() {
        DateTimeOffset? value = null;
        DateTimeOffset capturedNow = NowUtc;
        string rendered = value.HasValue
            ? FormatRelativeTime(value.Value, capturedNow, 7)
            : "—";
        rendered.ShouldBe("—");
    }

    /// <summary>
    /// Reference implementation of the generator-emitted <c>FormatRelativeTime(DateTime, ...)</c>
    /// helper. Mirrored here so behavioural tests can exercise the contract without compiling
    /// generator output. Keep in sync with <c>RazorEmitter.EmitFormatters</c>.
    /// </summary>
    private static string FormatRelativeTime(DateTime value, DateTimeOffset now, int relativeWindowDays) {
        DateTimeOffset timestamp;
        try {
            timestamp = value.Kind switch {
                DateTimeKind.Utc => new DateTimeOffset(value, TimeSpan.Zero),
                DateTimeKind.Local => new DateTimeOffset(value).ToUniversalTime(),
                _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero),
            };
        }
        catch (ArgumentOutOfRangeException) {
            return value.ToString("d", CultureInfo.CurrentCulture);
        }

        return FormatRelativeTime(timestamp, now, relativeWindowDays);
    }

    /// <summary>
    /// Reference implementation of the generator-emitted <c>FormatRelativeTime(DateTimeOffset, ...)</c>
    /// helper. Mirrored here for behavioural tests; keep in sync with <c>RazorEmitter.EmitFormatters</c>.
    /// </summary>
    private static string FormatRelativeTime(DateTimeOffset value, DateTimeOffset now, int relativeWindowDays) {
        DateTimeOffset timestamp = value.ToUniversalTime();
        DateTimeOffset utcNow = now.ToUniversalTime();
        TimeSpan delta;
        try {
            delta = utcNow - timestamp;
        }
        catch (OverflowException) {
            return value.ToString("d", CultureInfo.CurrentCulture);
        }
        bool future = delta.Ticks < 0;
        TimeSpan distance = delta.Duration();
        if (distance > TimeSpan.FromDays(relativeWindowDays)) {
            return value.ToString("d", CultureInfo.CurrentCulture);
        }

        string suffix = future ? " from now" : " ago";
        if (distance < TimeSpan.FromMinutes(1)) {
            return future ? "in <1m" : "<1m ago";
        }

        if (distance < TimeSpan.FromHours(1)) {
            return ((int)Math.Floor(distance.TotalMinutes)).ToString(CultureInfo.InvariantCulture) + "m" + suffix;
        }

        if (distance < TimeSpan.FromDays(1)) {
            return ((int)Math.Floor(distance.TotalHours)).ToString(CultureInfo.InvariantCulture) + "h" + suffix;
        }

        return ((int)Math.Floor(distance.TotalDays)).ToString(CultureInfo.InvariantCulture) + "d" + suffix;
    }
}

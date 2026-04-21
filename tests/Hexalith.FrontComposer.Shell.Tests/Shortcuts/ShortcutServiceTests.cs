// Story 3-4 Task 10.1 / 10.1c / 10.1d / 10.1g (D1-D4, D22 — AC1).
using Hexalith.FrontComposer.Contracts.Diagnostics;

using Hexalith.FrontComposer.Shell.Shortcuts;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Shortcuts;

public class ShortcutServiceTests
{
    private static ShortcutService BuildService(out FakeTimeProvider time, out ILogger<ShortcutService> logger)
    {
        time = new FakeTimeProvider();
        logger = Substitute.For<ILogger<ShortcutService>>();
        return new ShortcutService(time, logger);
    }

    [Fact]
    public async Task RegisterThenInvoke_RunsHandler()
    {
        ShortcutService sut = BuildService(out _, out _);
        int hits = 0;
        sut.Register("ctrl+k", "PaletteShortcutDescription", () => { hits++; return Task.CompletedTask; });

        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "K", CtrlKey = true });

        fired.ShouldBeTrue();
        hits.ShouldBe(1);
    }

    [Fact]
    public async Task DuplicateRegister_LastWriterWins_AndLogsHFC2108()
    {
        ShortcutService sut = BuildService(out _, out ILogger<ShortcutService> logger);
        int firstHits = 0, secondHits = 0;
        sut.Register("ctrl+k", "FirstDescription", () => { firstHits++; return Task.CompletedTask; });
        sut.Register("ctrl+k", "SecondDescription", () => { secondHits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });

        firstHits.ShouldBe(0);
        secondHits.ShouldBe(1);

        LoggedEntry entry = GetLogEntries(logger)
            .Single(e => e.Level == LogLevel.Information && e.Message.Contains(FcDiagnosticIds.HFC2108_ShortcutConflict, StringComparison.Ordinal));

        entry.State["Binding"].ShouldBe("ctrl+k");
        entry.State["PreviousDescriptionKey"].ShouldBe("FirstDescription");
        entry.State["NewDescriptionKey"].ShouldBe("SecondDescription");
        entry.State["PreviousCallSiteFile"].ShouldNotBeNull();
        entry.State["NewCallSiteFile"].ShouldNotBeNull();
        entry.State["PreviousCallSiteLine"].ShouldBeOfType<int>().ShouldBeGreaterThan(0);
        entry.State["NewCallSiteLine"].ShouldBeOfType<int>().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DisposedRegistration_DoesNotFire_WhenNoReplacement()
    {
        ShortcutService sut = BuildService(out _, out _);
        int hits = 0;
        IDisposable handle = sut.Register("ctrl+k", "Desc", () => { hits++; return Task.CompletedTask; });
        handle.Dispose();

        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });

        fired.ShouldBeFalse();
        hits.ShouldBe(0);
    }

    [Fact]
    public async Task DisposedOriginal_DoesNotRemoveReplacement()
    {
        ShortcutService sut = BuildService(out _, out _);
        int firstHits = 0, secondHits = 0;
        IDisposable handle = sut.Register("ctrl+k", "First", () => { firstHits++; return Task.CompletedTask; });
        sut.Register("ctrl+k", "Second", () => { secondHits++; return Task.CompletedTask; });
        handle.Dispose();

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });

        secondHits.ShouldBe(1);
    }

    [Fact]
    public async Task Chord_GH_FiresWhenSecondKeyArrivesAt1499ms()
    {
        ShortcutService sut = BuildService(out FakeTimeProvider time, out _);
        int hits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        time.Advance(TimeSpan.FromMilliseconds(1499));
        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        fired.ShouldBeTrue();
        hits.ShouldBe(1);
    }

    [Fact]
    public async Task Chord_GH_DoesNotFireAfter1500ms()
    {
        ShortcutService sut = BuildService(out FakeTimeProvider time, out _);
        int hits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        time.Advance(TimeSpan.FromMilliseconds(1501));
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        hits.ShouldBe(0);
    }

    [Fact]
    public async Task Chord_GH_DoesNotFireExactlyAt1500ms()
    {
        // P20 (2026-04-21 pass-4): boundary companion for Chord_GH_FiresWhenSecondKeyArrivesAt1499ms.
        // The 1499ms test proves sync lookup succeeds when the timer has NOT yet fired; this test
        // pins the inclusive/exclusive boundary by advancing to exactly 1500ms so the FakeTimeProvider
        // drains the scheduled callback. Together the three tests (1499 fires / 1500 clears / 1501
        // no-fire) bracket the timeout semantics deterministically and would fail if the timeout
        // constant were mis-tuned.
        ShortcutService sut = BuildService(out FakeTimeProvider time, out _);
        int hits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        time.Advance(TimeSpan.FromMilliseconds(1500));
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        hits.ShouldBe(0);
    }

    [Fact]
    public async Task Chord_NonMatchingSecondKey_ClearsPending_AndDoesNotFire()
    {
        ShortcutService sut = BuildService(out _, out _);
        int hits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "x" });
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        hits.ShouldBe(0);
    }

    [Fact]
    public async Task Chord_RepeatPrefix_OverwritesPending_NewWindowStarts()
    {
        ShortcutService sut = BuildService(out FakeTimeProvider time, out _);
        int hits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        time.Advance(TimeSpan.FromMilliseconds(800));
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        time.Advance(TimeSpan.FromMilliseconds(800));
        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        fired.ShouldBeTrue();
        hits.ShouldBe(1);
    }

    [Fact]
    public async Task ModifierBindingDuringPending_FiresAndClearsPending()
    {
        ShortcutService sut = BuildService(out _, out _);
        int chordHits = 0, modifierHits = 0;
        sut.Register("g h", "HomeShortcutDescription", () => { chordHits++; return Task.CompletedTask; });
        sut.Register("ctrl+k", "PaletteShortcutDescription", () => { modifierHits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "K", CtrlKey = true });
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        modifierHits.ShouldBe(1);
        chordHits.ShouldBe(0);
    }

    [Fact]
    public async Task TryInvokeAsync_WhenHandlerThrows_LogsHFC2109_AndReturnsTrue()
    {
        ShortcutService sut = BuildService(out _, out ILogger<ShortcutService> logger);
        sut.Register("ctrl+k", "Desc", () => throw new InvalidOperationException("boom"));

        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });

        fired.ShouldBeTrue();

        LoggedEntry entry = GetLogEntries(logger)
            .Single(e => e.Level == LogLevel.Warning && e.Message.Contains(FcDiagnosticIds.HFC2109_ShortcutHandlerFault, StringComparison.Ordinal));

        entry.Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("boom");
        entry.State["Binding"].ShouldBe("ctrl+k");
        entry.State["DescriptionKey"].ShouldBe("Desc");
        entry.State["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
        entry.State["ExceptionMessage"].ShouldBe("boom");
    }

    [Fact]
    public async Task TryInvokeAsync_WhenHandlerThrowsOperationCanceledException_LogsHFC2109_AndReturnsTrue()
    {
        ShortcutService sut = BuildService(out _, out ILogger<ShortcutService> logger);
        sut.Register("ctrl+k", "Desc", () => throw new OperationCanceledException("cancelled"));

        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });

        fired.ShouldBeTrue();

        LoggedEntry entry = GetLogEntries(logger)
            .Single(e => e.Level == LogLevel.Warning && e.Message.Contains(FcDiagnosticIds.HFC2109_ShortcutHandlerFault, StringComparison.Ordinal));

        entry.Exception.ShouldBeOfType<OperationCanceledException>().Message.ShouldBe("cancelled");
        entry.State["Binding"].ShouldBe("ctrl+k");
        entry.State["DescriptionKey"].ShouldBe("Desc");
        entry.State["ExceptionType"].ShouldBe(typeof(OperationCanceledException).FullName);
        entry.State["ExceptionMessage"].ShouldBe("cancelled");
    }

    [Theory]
    [InlineData(null, "Desc")]
    [InlineData("", "Desc")]
    [InlineData("   ", "Desc")]
    [InlineData("ctrl+k", null)]
    [InlineData("ctrl+k", "")]
    [InlineData("ctrl+k", "  ")]
    public void Register_ThrowsArgumentException_OnNullOrEmptyArguments(string? binding, string? descriptionKey)
    {
        ShortcutService sut = BuildService(out _, out _);
        Should.Throw<ArgumentException>(() => sut.Register(binding!, descriptionKey!, () => Task.CompletedTask));
    }

    [Fact]
    public void Register_ThrowsArgumentNullException_OnNullHandler()
    {
        ShortcutService sut = BuildService(out _, out _);
        Should.Throw<ArgumentNullException>(() => sut.Register("ctrl+k", "Desc", null!));
    }

    [Fact]
    public void GetRegistrations_ReturnsAllCurrentlyRegistered()
    {
        ShortcutService sut = BuildService(out _, out _);
        sut.Register("ctrl+k", "PaletteShortcutDescription", () => Task.CompletedTask);
        sut.Register("ctrl+,", "SettingsShortcutDescription", () => Task.CompletedTask);

        IReadOnlyList<Hexalith.FrontComposer.Contracts.Shortcuts.ShortcutRegistration> regs = sut.GetRegistrations();

        regs.Count.ShouldBe(2);
        regs.ShouldContain(r => r.Binding == "ctrl+k" && r.NormalisedLabel == "Ctrl+K");
        regs.ShouldContain(r => r.Binding == "ctrl+,");
    }

    private static IReadOnlyList<LoggedEntry> GetLogEntries(ILogger<ShortcutService> logger)
    {
        List<LoggedEntry> entries = [];
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls())
        {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            {
                continue;
            }

            object?[] args = call.GetArguments();
            if (args.Length < 5 || args[0] is not LogLevel level)
            {
                continue;
            }

            Dictionary<string, object?> state = new(StringComparer.Ordinal);
            if (args[2] is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                foreach (KeyValuePair<string, object?> kvp in kvps)
                {
                    state[kvp.Key] = kvp.Value;
                }
            }

            entries.Add(new LoggedEntry(level, args[2]?.ToString() ?? string.Empty, args[3] as Exception, state));
        }

        return entries;
    }

    [Fact]
    public async Task Chord_RepeatPrefixBeforeTimeout_OverwritesPendingField()
    {
        // D4 sub-decision (c): pressing the same prefix twice before timeout keeps only the latest
        // prefix pending — the second press resets the generation counter.
        ShortcutService sut = BuildService(out _, out _);
        int hits = 0;
        sut.Register("g h", "Home", () => { hits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        fired.ShouldBeTrue();
        hits.ShouldBe(1);
    }

    [Fact]
    public async Task Chord_ModifierBindingDuringPending_FiresAndClearsPending()
    {
        // D4 sub-decision (d): a modifier-bearing shortcut pressed during a pending chord must
        // both fire AND clear the pending first-key (chord does not complete later).
        ShortcutService sut = BuildService(out _, out _);
        int gHomeHits = 0;
        int ctrlKHits = 0;
        sut.Register("g h", "Home", () => { gHomeHits++; return Task.CompletedTask; });
        sut.Register("ctrl+k", "Palette", () => { ctrlKHits++; return Task.CompletedTask; });

        await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });
        bool modifierFired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "k", CtrlKey = true });
        bool secondKeyFired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "h" });

        modifierFired.ShouldBeTrue();
        ctrlKHits.ShouldBe(1);
        secondKeyFired.ShouldBeFalse();
        gHomeHits.ShouldBe(0);
    }

    [Fact]
    public async Task TryInvokeAsync_AfterDispose_ReturnsFalse_WithoutAllocatingTimer()
    {
        ShortcutService sut = BuildService(out _, out _);
        sut.Register("g h", "Home", () => Task.CompletedTask);
        sut.Dispose();

        bool fired = await sut.TryInvokeAsync(new KeyboardEventArgs { Key = "g" });

        fired.ShouldBeFalse();
    }

    private sealed record LoggedEntry(
        LogLevel Level,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> State);
}

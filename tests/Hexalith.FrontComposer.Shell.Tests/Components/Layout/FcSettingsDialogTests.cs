// ATDD RED PHASE — Story 3-3 Task 10.8 (D13, D14, D15, D17; AC2, AC4, AC5; ADR-040)
// Fails at compile until Task 6.1 (FcSettingsDialog) + Task 2.3 (UserPreferenceChangedAction etc.) land.

using System.Collections.Concurrent;
using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-3 Task 10.8 — settings-dialog DOM, action-dispatch, and viewport-forced-note tests.
/// D13 (no Apply/Cancel — live changes), D14 (preview panel parameter-driven),
/// D15 (FcThemeToggle embedded verbatim), D17 (radio labels resolved from resx),
/// ADR-040 (forced-by-viewport informational note).
/// </summary>
public sealed class FcSettingsDialogTests : LayoutComponentTestBase {
    public FcSettingsDialogTests() => EnsureStoreInitialized();

    [Fact]
    public void RendersDensityRadioThemeAndPreview() {
        IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

        cut.WaitForAssertion(() => {
            // Density radio group (3 options) — D13 + D17.
            cut.Markup.ShouldContain("Compact", Case.Sensitive);
            cut.Markup.ShouldContain("Comfortable", Case.Sensitive);
            cut.Markup.ShouldContain("Roomy", Case.Sensitive);

            // Theme section embeds FcThemeToggle (D15) — find it as a child component.
            _ = cut.FindComponent<FcThemeToggle>();

            // Preview panel renders (D14).
            _ = cut.FindComponent<FcDensityPreviewPanel>();
        });
    }

    [Fact]
    public void AccordionSectionsBindTitlesToV5HeaderParam() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

            // The three titled sections (Density / Theme / Preview, §4.2) must bind their titles to the
            // Fluent v5 `Header` parameter. With the removed v4 `Heading` attribute the value lands in
            // AdditionalAttributes and `Header` stays null, so each chevron renders with no visible title
            // (the blank-accordion-header regression fixed by correct-course 2026-06-24).
            string?[] headers = cut.FindComponents<FluentAccordionItem>()
                .Select(item => item.Instance.Header)
                .ToArray();

            headers.ShouldContain("Display density");
            headers.ShouldContain("Theme");
            headers.ShouldContain("Preview");
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void DensityRadioSelectionDispatchesAction() {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

        // Drive the SelectedDensity setter — the dialog dispatches UserPreferenceChangedAction
        // with NewEffective pre-resolved per D3.
        cut.Instance.SelectedDensity = DensityLevel.Compact;

        cut.WaitForAssertion(() => {
            IState<FrontComposerDensityState> state =
                Services.GetRequiredService<IState<FrontComposerDensityState>>();
            state.Value.UserPreference.ShouldBe(DensityLevel.Compact);
            state.Value.EffectiveDensity.ShouldBe(DensityLevel.Compact); // Desktop default; tier override inactive.
        });
    }

    [Fact]
    public void ResetToDefaultsDispatchesClearedAndThemeSystem() {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        // Pre-set a non-default state so reset has visible effect.
        dispatcher.Dispatch(new UserPreferenceChangedAction("seed-1", DensityLevel.Roomy, DensityLevel.Roomy));
        dispatcher.Dispatch(new ThemeChangedAction("seed-2", ThemeValue.Dark));

        IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

        // Click "Reset to defaults" footer button (FluentButton renders as <fluent-button>).
        cut.Find("[data-testid=\"fc-settings-reset\"]").Click();

        cut.WaitForAssertion(() => {
            IState<FrontComposerDensityState> density =
                Services.GetRequiredService<IState<FrontComposerDensityState>>();
            IState<FrontComposerThemeState> theme =
                Services.GetRequiredService<IState<FrontComposerThemeState>>();

            density.Value.UserPreference.ShouldBeNull("Reset must clear the user density preference (D13).");
            density.Value.EffectiveDensity.ShouldBe(DensityLevel.Compact, "Reset must fall through to the Desktop factory default.");
            theme.Value.CurrentTheme.ShouldBe(ThemeValue.System, "Reset must restore System theme (D13).");
        });
    }

    [Fact]
    public void RendersForcedDensityNoteAtTabletWhenFactoryDefaultWouldBeCompact() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Tablet));
            dispatcher.Dispatch(new UserPreferenceClearedAction("c1", DensityLevel.Comfortable));

            IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

            cut.WaitForAssertion(() =>
                cut.Markup.ShouldContain(
                    "Your device size is forcing Comfortable density",
                    Case.Sensitive,
                    "ADR-040 — settings dialog must compare against the resolver's factory default, not a duplicated Comfortable fallback."));
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task DoneButtonClosesDialog() {
        IDialogInstance dialog = CreateRegisteredDialogInstance();

        IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();
        cut.Instance.Dialog = dialog;

        await cut.InvokeAsync(() => cut.Find("[data-testid=\"fc-settings-done\"]").Click());

        DialogResult result = await dialog.Result
            .WaitAsync(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        result.Cancelled.ShouldBeFalse();
    }

    [Fact]
    public void RendersForcedDensityNoteAtTabletWhenUserPrefersCompact() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

            // Seed Tablet viewport so DensityEffects.HandleViewportTierChanged forces Comfortable.
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Tablet));
            // User prefers Compact — but forcing is active.
            dispatcher.Dispatch(new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Comfortable));

            IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

            cut.WaitForAssertion(() =>
                cut.Markup.ShouldContain(
                    "Your device size is forcing Comfortable density",
                    Case.Sensitive,
                    "ADR-040 — settings dialog must surface the forcing-by-viewport reason."));
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void NoForcedNoteAtDesktop() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
            dispatcher.Dispatch(new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Compact));

            IRenderedComponent<FcSettingsDialog> cut = Render<FcSettingsDialog>();

            cut.WaitForAssertion(() =>
                cut.Markup.ShouldNotContain(
                    "Your device size is forcing",
                    Case.Sensitive,
                    "Forced-note must be absent when EffectiveDensity matches the user's choice."));
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    private IDialogInstance CreateRegisteredDialogInstance() {
        IDialogService dialogService = Services.GetRequiredService<IDialogService>();
        ConstructorInfo constructor = typeof(DialogInstance).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(IDialogService), typeof(Type), typeof(DialogOptions)],
            modifiers: null) ?? throw new InvalidOperationException("Fluent UI DialogInstance constructor was not found.");

        var dialog = (IDialogInstance)constructor.Invoke(
            [dialogService, typeof(FcSettingsDialog), new DialogOptions()]);

        FieldInfo itemsField = typeof(DialogService).BaseType?.GetField(
            "_list",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Fluent UI dialog registry was not found.");

        var items =
            (ConcurrentDictionary<string, IDialogInstance>)itemsField.GetValue(dialogService)!;
        items.TryAdd(dialog.Id, dialog).ShouldBeTrue();
        return dialog;
    }
}

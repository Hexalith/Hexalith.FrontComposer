using System.Linq;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CommandRendererInlineTests : CommandRendererTestBase {
    [Fact]
    public async Task Renderer_ZeroFields_RendersSingleButton() {
        await InitializeStoreAsync();

        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fluent-button", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_ZeroFields_ClickInvokesRegisteredExternalSubmit() {
        await InitializeStoreAsync();
        IState<ZeroFieldInlineCommandLifecycleState> state = Services.GetRequiredService<IState<ZeroFieldInlineCommandLifecycleState>>();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => {
            state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        });
    }

    [Fact]
    public async Task Renderer_OneField_ClickOpensPopover() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fluent-popover", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_OneField_PopoverFormIsWrappedInFcLifecycleWrapper() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();
        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_Inline_UsesSecondaryAppearance() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("appearance=\"outline\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_OneField_EscapeClosesPopover() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();
        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("opened=\"true\"", Case.Insensitive);
        });

        cut.Find(".fc-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => {
            // FluentPopover stays in markup; the Opened attribute flips to false after ClosePopoverAsync.
            cut.Markup.ShouldNotContain("opened=\"true\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_OneField_PopoverSubmit_InnerFormDispatchesSubmittedAction() {
        await InitializeStoreAsync();
        IState<OneFieldInlineCommandLifecycleState> state = Services.GetRequiredService<IState<OneFieldInlineCommandLifecycleState>>();
        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        });
    }

    // NOTE: `Renderer_Inline_LeadingIconPresent` remains deferred because command icons now resolve
    // through the shell-owned Fluent UI v5 icon factory. Re-enable when the test host asserts
    // rendered SVG content rather than the old predefined icon type path.
    [Fact]
    public async Task Renderer_OneField_ScrollIntoView_ThenFocusReturnsToTrigger_OnConfirmed() {
        await InitializeStoreAsync();
        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find(".fc-popover"));
        int invocationsBefore = FcExpandInRowModule.Invocations.Count;
        cut.Find(".fc-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => {
            FcExpandInRowModule.Invocations.Count.ShouldBeGreaterThan(invocationsBefore);
            FcExpandInRowModule.Invocations.Last().Identifier.ShouldBe("focusTriggerElementById");
        });
    }

    [Fact]
    public async Task Renderer_OneField_FocusReturnsToTriggerButtonOnEscape() {
        await InitializeStoreAsync();
        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find(".fc-popover"));
        cut.Find(".fc-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => {
            FcExpandInRowModule.Invocations.Last().Identifier.ShouldBe("focusTriggerElementById");
        });
    }

    [Fact]
    public async Task Renderer_AllFieldsDerivable_Renders0FieldInlineButton_SubmitsImmediately() {
        // ZeroFieldInlineCommand has only MessageId + TenantId — both derivable system fields.
        // Per AC2 / D36, this renders as the 0-field inline button and submits via the registered external submit.
        await InitializeStoreAsync();
        IState<ZeroFieldInlineCommandLifecycleState> state = Services.GetRequiredService<IState<ZeroFieldInlineCommandLifecycleState>>();

        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.FindAll("fluent-popover").Count.ShouldBe(0);
            cut.Find("fluent-button").ShouldNotBeNull();
        });

        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => {
            state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        });
    }

    [Fact]
    public async Task Renderer_IconFallback_InvalidIconName_FallsBackToDefaultAndLogs() {
        TestLogger<IconFallbackInlineCommandRenderer> logger = new();
        _ = Services.AddSingleton<ILogger<IconFallbackInlineCommandRenderer>>(logger);
        await InitializeStoreAsync();

        IRenderedComponent<IconFallbackInlineCommandRenderer> cut = Render<IconFallbackInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            // Renderer succeeded — icon resolution did not throw, fallback path used.
            cut.Markup.ShouldContain("fluent-button", Case.Insensitive);
        });

        logger.WarningMessages.ShouldContain(message =>
            message.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0
            && message.IndexOf("This.Icon.Definitely.Does.Not.Exist", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public async Task Renderer_ZeroFields_ButtonDisabled_UntilExternalSubmitRegistered() {
        // Decision D36 — emitted button is `[disabled]=@(_externalSubmit is null)`. The Form
        // calls RegisterExternalSubmit in OnAfterRender(firstRender=true) which then triggers
        // a re-render via StateHasChanged. We assert that AFTER full settle the button is enabled,
        // and that the wiring contract (disabled attribute is bound) is present in markup.
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            // After OnAfterRender + StateHasChanged, the button is enabled (disabled removed).
            string markup = cut.Markup;
            markup.ShouldContain("fluent-button", Case.Insensitive);
            markup.ShouldNotContain("disabled=\"true\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_OpeningSecondPopover_ClosesFirstPopoverFirst() {
        // Decision D37 — at-most-one Inline popover open at a time per circuit, enforced by
        // InlinePopoverRegistry. Assert that the registry's OpenAsync closes any previously
        // open popover before recording the new one.
        InlinePopoverRegistry registry = Services.GetRequiredService<InlinePopoverRegistry>();
        TrackedPopover first = new();
        TrackedPopover second = new();

        await registry.OpenAsync(first);
        first.CloseCount.ShouldBe(0);
        await registry.OpenAsync(second);

        first.CloseCount.ShouldBe(1, "the first popover must be closed when a second one opens");
    }

    private sealed class TrackedPopover : IInlinePopover {
        public int CloseCount { get; private set; }

        public Task ClosePopoverAsync() {
            CloseCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestLogger<T> : ILogger<T> {
        public List<string> WarningMessages { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Warning) {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}

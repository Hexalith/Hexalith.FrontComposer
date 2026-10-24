using System.Globalization;
using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Visible, single-owner failure feedback for the hidden zero-field form.</summary>
public sealed class ZeroFieldFailureAccessibilityTests : CommandRendererTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HiddenServerValidationIncludesSafeFieldMessagesAsPlainUnlinkedText(bool includeGlobal)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandValidationException(ProblemDetailsPayload.Empty with {
                ValidationErrors = new Dictionary<string, IReadOnlyList<string>> { ["TenantId"] = ["Safe field-only correction."] },
                GlobalErrors = includeGlobal ? ["Safe form correction."] : [],
            })));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("Safe field-only correction."));
        if (includeGlobal) cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("Safe form correction.");
        cut.Find("[data-testid='fc-zero-field-outcome']").QuerySelectorAll("a").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CommandWarningKind.NotFound, null, "en")]
    [InlineData(CommandWarningKind.NotFound, "  ", "fr")]
    [InlineData(CommandWarningKind.RateLimited, null, "en")]
    [InlineData(CommandWarningKind.RateLimited, "  ", "fr")]
    public async Task BlankWarningUsesNonblankFallbackAndRetainsRetryGuidance(CommandWarningKind kind, string? text, string culture)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(kind,
                ProblemDetailsPayload.Empty with { Detail = text, Title = text, EntityLabel = text },
                kind == CommandWarningKind.RateLimited ? TimeSpan.FromSeconds(17) : null)));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain(culture == "fr" ? "Veuillez lire l’avertissement et réessayer." : "Please review the warning and try again."));
        if (kind == CommandWarningKind.RateLimited) cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain(culture == "fr" ? "Réessayez dans 17 secondes." : "Retry after 17 seconds.");
        cut.FindComponents<Microsoft.FluentUI.AspNetCore.Components.FluentMessageBar>().Single(component => component.Markup.Contains("fc-zero-field-outcome", StringComparison.Ordinal)).Instance.AllowDismiss.ShouldBeFalse();
        _ = service.Received(1).DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnedRejectionHasVisibleSafeCopyAndOneLifecycleSpeechOwner()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult("rejected", CommandResultStatus.Rejected)));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        List<string> speech = [];
        using IDisposable subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Subscribe("command-renderer:" + cut.Find("fluent-button").Id, speech.Add);
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("rejected", Case.Insensitive));
        cut.WaitForAssertion(() => speech.Count(message => message.Contains("rejected", StringComparison.OrdinalIgnoreCase)).ShouldBe(1));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposingHiddenOutcomeCallbackCannotContinueAdmissionOrClaimSpeech(bool disposeOnClear)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(CommandWarningKind.NotFound,
                ProblemDetailsPayload.Empty with { Detail = "Safe hidden failure." })));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        bool disposed = false, leaseHeldDuringClear = false;
        IRenderedComponent<ZeroFieldInlineCommandForm>? cut = null;
        List<string> speech = [];
        using IDisposable subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Subscribe("dispose-hidden", speech.Add);
        ICommandExecutionAdmissionGate admissionGate = Services.GetRequiredService<ICommandExecutionAdmissionGate>();
        cut = Render<ZeroFieldInlineCommandForm>(p => p
            .Add(c => c.AnnouncementSurface, "dispose-hidden")
            .Add(c => c.OnHiddenOutcome, async (string? message) => {
                if (disposeOnClear == (message is null))
                {
                    // Negative control: the admitted clear runs while this attempt still holds the lease.
                    if (message is null)
                    {
                        using CommandExecutionAdmission held = admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("held-probe"));
                        held.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.AdmissionAlreadyInProgress);
                        leaseHeldDuringClear = !held.IsAdmitted;
                    }
                    cut!.Instance.Dispose();
                    disposed = true;
                }
                await Task.Yield();
            }));
        await cut.InvokeAsync(() => (Task)typeof(ZeroFieldInlineCommandForm)
            .GetMethod("SubmitAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, [false])!);
        cut.WaitForAssertion(() => disposed.ShouldBeTrue());
        speech.Where(message => message == "Safe hidden failure.").ShouldBeEmpty();
        if (disposeOnClear)
        {
            _ = service.DidNotReceive().DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
            // The disposed exit follows admission, so it must still release the circuit-scoped lease.
            leaseHeldDuringClear.ShouldBeTrue();
            SpinWait.SpinUntil(() => {
                using CommandExecutionAdmission probe = admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("settlement-probe"));
                return probe.IsAdmitted;
            }, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("admission-busy")]
    [InlineData("in-flight")]
    [InlineData("scope-unavailable")]
    public async Task BlockedAttemptDisposedDuringItsCallbackCannotContinuePresentationOrFocus(string blocked)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<CommandFeedbackWarning> published = [];
        using IDisposable feedback = Services.GetRequiredService<ICommandFeedbackPublisher>().Subscribe(published.Add);
        int blockedPresentations = 0, publishedBeforeDisposal = 0;
        bool disposed = false, noLeaseDuringCallback = false;
        ICommandExecutionAdmissionGate admissionGate = Services.GetRequiredService<ICommandExecutionAdmissionGate>();
        CommandExecutionAdmission? holder = null;
        IRenderedComponent<ZeroFieldInlineCommandForm>? cut = null;
        cut = Render<ZeroFieldInlineCommandForm>(p => p
            .Add(c => c.OnHiddenOutcome, async (string? message) => {
                if (message is null || blocked == "scope-unavailable")
                {
                    // Withdraw the external blocker, then prove this attempt owns no admission lease.
                    // The probe needs a scope, so the scope row lends one and always withdraws it.
                    holder?.Dispose();
                    string? tenant = UserContext.TenantId;
                    if (blocked == "scope-unavailable") UserContext.TenantId = "test-tenant";
                    try
                    {
                        using CommandExecutionAdmission probe = admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("blocked-probe"));
                        noLeaseDuringCallback = probe.IsAdmitted;
                    }
                    finally
                    {
                        UserContext.TenantId = tenant;
                    }
                    publishedBeforeDisposal = published.Count;
                    cut!.Instance.Dispose();
                    disposed = true;
                }
                await Task.Yield();
            })
            .Add(c => c.OnBlockedSubmission, (bool _) => { blockedPresentations++; return Task.CompletedTask; }));
        using CommandExecutionAdmission? heldAdmission = holder = blocked == "admission-busy"
            ? admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("holder"))
            : null;
        if (blocked == "scope-unavailable") UserContext.TenantId = null;
        holder?.IsAdmitted.ShouldBeTrue();
        if (blocked == "in-flight")
        {
            Services.GetRequiredService<Fluxor.IDispatcher>().Dispatch(
                new ZeroFieldInlineCommandActions.SubmittedAction("01ERZ3NDEKTSV4RRFFQ69G5FAV", new ZeroFieldInlineCommand()));
        }
        await cut.InvokeAsync(() => (Task)typeof(ZeroFieldInlineCommandForm)
            .GetMethod("SubmitAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, [false])!);

        // A parent that disposes the blocked form during its hidden-outcome callback (the clear, or the
        // scope row's warning) receives no blocked outcome, focus or dispatch afterwards.
        cut.WaitForAssertion(() => disposed.ShouldBeTrue());
        noLeaseDuringCallback.ShouldBeTrue();
        published.Count.ShouldBe(publishedBeforeDisposal);
        published.Count.ShouldBe(blocked == "scope-unavailable" ? 1 : 0);
        blockedPresentations.ShouldBe(0);
        FcFocusModule.VerifyNotInvoke("focusAttemptedControl");
        FcFocusModule.VerifyNotInvoke("focusElementById");
        FcFocusModule.VerifyNotInvoke("focusValidationOutcome");
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlockedRendererAttemptWithdrawsTheEarlierVisibleOutcomeWithoutDispatch()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(CommandWarningKind.RateLimited,
                ProblemDetailsPayload.Empty with { Detail = "Too many requests." }, TimeSpan.FromSeconds(17))));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("Retry after 17 seconds."));
        using CommandExecutionAdmission holder = Services.GetRequiredService<ICommandExecutionAdmissionGate>().TryAcquire(new CommandExecutionAdmissionRequest("holder"));
        holder.IsAdmitted.ShouldBeTrue();
        string triggerId = cut.Find("fluent-button").Id!;
        List<string> speech = [];
        using IDisposable subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Subscribe("command-renderer:" + triggerId, speech.Add);
        speech.Clear(); // Subscribe can replay the previous warning; count only this blocked activation.
        int earlierFocusCalls = FcFocusModule.Invocations.Count;

        cut.Find("fluent-button").Click();

        // Decision 2 / R4-A1 — the blocked click is the next attempt: the stale retry guidance leaves while
        // the blocked outcome is presented beside the trigger, and nothing more is dispatched.
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-testid='fc-zero-field-outcome']").ShouldBeEmpty();
            cut.Find("[data-testid='fc-command-blocked-outcome']").GetAttribute("data-fc-blocked").ShouldBe("true");
            speech.Where(message => !string.IsNullOrWhiteSpace(message)).ShouldHaveSingleItem()
                .ShouldBe("This command did not run. Another command is already in progress.");
        });
        cut.Render();
        speech.Where(message => !string.IsNullOrWhiteSpace(message)).ShouldHaveSingleItem();
        cut.Find("fluent-button").Id.ShouldBe(triggerId);
        // bUnit records the attempted-control interop contract; no heading or replacement focus occurs.
        var focus = FcFocusModule.Invocations.Skip(earlierFocusCalls).Where(call => call.Identifier.StartsWith("focus", StringComparison.Ordinal)).ShouldHaveSingleItem();
        focus.Identifier.ShouldBe("focusAttemptedControl");
        focus.Arguments.ShouldBe([triggerId, false]);
        FcFocusModule.VerifyNotInvoke("focusElementById");
        FcFocusModule.VerifyNotInvoke("focusValidationOutcome");
        FcFocusModule.VerifyNotInvoke("focusReplacementHeading");
        _ = service.Received(1).DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("admitted")]
    [InlineData("admission-busy")]
    [InlineData("scope-unavailable")]
    public async Task EveryAttemptWithdrawsTheEarlierHiddenOutcomeButOnlyAdmissionDispatches(string attempt)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult("accepted", CommandResultStatus.Accepted)));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<string?> hiddenOutcomes = [];
        IRenderedComponent<ZeroFieldInlineCommandForm> cut = Render<ZeroFieldInlineCommandForm>(p => p
            .Add(c => c.OnHiddenOutcome, (string? message) => {
                hiddenOutcomes.Add(message);
                return Task.CompletedTask;
            }));
        if (attempt == "scope-unavailable") UserContext.TenantId = null;
        using CommandExecutionAdmission? holder = attempt == "admission-busy"
            ? Services.GetRequiredService<ICommandExecutionAdmissionGate>().TryAcquire(new CommandExecutionAdmissionRequest("holder"))
            : null;
        holder?.IsAdmitted.ShouldBeTrue();
        await cut.InvokeAsync(() => (Task)typeof(ZeroFieldInlineCommandForm)
            .GetMethod("SubmitAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, [false])!);

        // Decision 2 — a blocked attempt is the next attempt too: it withdraws the earlier outcome
        // (scope loss replaces it with its own copy), while only an admitted attempt dispatches.
        if (attempt == "scope-unavailable") hiddenOutcomes.ShouldHaveSingleItem().ShouldNotBeNullOrWhiteSpace();
        else hiddenOutcomes.ShouldBe([null]);
        _ = service.Received(attempt == "admitted" ? 1 : 0).DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InFlightStateGuardedAttemptWithdrawsTheEarlierHiddenOutcomeWithoutDispatch()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<string?> hiddenOutcomes = [];
        IRenderedComponent<ZeroFieldInlineCommandForm> cut = Render<ZeroFieldInlineCommandForm>(p => p
            .Add(c => c.OnHiddenOutcome, (string? message) => {
                hiddenOutcomes.Add(message);
                return Task.CompletedTask;
            }));
        Services.GetRequiredService<Fluxor.IDispatcher>().Dispatch(
            new ZeroFieldInlineCommandActions.SubmittedAction("01ERZ3NDEKTSV4RRFFQ69G5FAV", new ZeroFieldInlineCommand()));
        await cut.InvokeAsync(() => (Task)typeof(ZeroFieldInlineCommandForm)
            .GetMethod("SubmitAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, [false])!);

        // A rapid second activation while a command is in flight is blocked: it withdraws the earlier
        // outcome after the state guard has decided, so the awaited clear cannot admit it.
        hiddenOutcomes.ShouldBe([null]);
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScopeUnavailableIsVisibleWithoutDispatchOrHiddenFocusAndEachAttemptSpeaksOnce()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        string triggerId = cut.Find("fluent-button").Id!;
        List<string> speech = [];
        using IDisposable subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Subscribe("command-renderer:" + triggerId, speech.Add);
        UserContext.TenantId = null;
        List<JSRuntimeInvocation> AttemptedControlFocus() => [.. FcFocusModule.Invocations.Where(call => call.Identifier == "focusAttemptedControl")];
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("workspace identity"));
        cut.WaitForAssertion(() => speech.Count(message => message.Contains("workspace identity", StringComparison.Ordinal)).ShouldBe(1));
        // The live form still applies the attempted-control rule once per attempt: the positive control
        // for the disposed exit that skips it.
        cut.WaitForAssertion(() => AttemptedControlFocus().ShouldHaveSingleItem());
        cut.Render();
        speech.Count(message => message.Contains("workspace identity", StringComparison.Ordinal)).ShouldBe(1);
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => speech.Count(message => message.Contains("workspace identity", StringComparison.Ordinal)).ShouldBe(2));
        cut.WaitForAssertion(() => AttemptedControlFocus().Count.ShouldBe(2));
        string attemptedControlId = AttemptedControlFocus()[0].Arguments[0].ShouldBeOfType<string>();
        attemptedControlId.ShouldStartWith("fc-command-form-");
        attemptedControlId.ShouldEndWith("-submit");
        AttemptedControlFocus().ShouldAllBe(call => call.Arguments.SequenceEqual(new object?[] { attemptedControlId, false }));
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        FcFocusModule.VerifyNotInvoke("focusValidationOutcome");
        FcFocusModule.VerifyNotInvoke("focusElementById");
    }

    [Fact]
    public async Task SubmitTimePolicyDenialUsesVisibleOwnerWithoutDispatchOrHiddenHeadingFocus()
    {
        bool denied = false;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        _ = evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(denied ? CommandAuthorizationDecision.Denied("denied") : CommandAuthorizationDecision.Allowed("allowed")));
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        _ = authentication.GetAuthenticationStateAsync().Returns(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        Services.Replace(ServiceDescriptor.Singleton(authentication));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ProtectedZeroFieldInlineCommandRenderer> cut = Render<ProtectedZeroFieldInlineCommandRenderer>();
        string triggerId = cut.Find("fluent-button").Id!;
        List<string> speech = [];
        using IDisposable subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Subscribe("command-renderer:" + triggerId, speech.Add);
        denied = true;
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("permission"));
        cut.WaitForAssertion(() => speech.Count(message => message.Contains("permission", StringComparison.Ordinal)).ShouldBe(1));
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<ProtectedZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        FcFocusModule.VerifyNotInvoke("focusElementById");
        FcFocusModule.VerifyNotInvoke("focusValidationOutcome");
    }

    [Fact]
    public async Task TransportFailureRemainsBesideTriggerAndSpeaksOnceWithoutRetry()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(
                CommandWarningKind.RetryableDispatchFailed,
                ProblemDetailsPayload.Empty with { Detail = "Raw transport detail must not be displayed" })));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        string triggerId = cut.Find("fluent-button").Id!;
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> speech = [];
        using IDisposable subscription = announcements.Subscribe("command-renderer:" + triggerId, speech.Add);

        cut.FindComponent<ZeroFieldInlineCommandForm>().Instance.OnHiddenOutcome.HasDelegate.ShouldBeTrue();
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-zero-field-outcome']").Count.ShouldBe(1, cut.Markup));
        string feedback = cut.Find("[data-testid='fc-zero-field-outcome']").TextContent;
        feedback.ShouldNotContain("Raw transport");
        cut.Find("[data-testid='fc-zero-field-outcome']").Closest("[style='display:none']").ShouldBeNull();
        cut.Find("[data-testid='fc-zero-field-outcome']").GetAttribute("aria-live").ShouldBe("off");
        cut.Render();
        speech.Count(message => message == feedback.Trim()).ShouldBe(1);
        _ = service.Received(1).DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        FcFocusModule.VerifyNotInvoke("focusValidationOutcome");
        FcFocusModule.VerifyNotInvoke("focusElementById");
    }

    [Fact]
    public async Task NextAttemptClearsFailureWhileAcceptedCommandUsesExistingSummary()
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        TaskCompletionSource<CommandResult> nextAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = service.DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<CommandResult>(new CommandWarningException(CommandWarningKind.NotFound,
                ProblemDetailsPayload.Empty with { Detail = "This item is unavailable." })), nextAttempt.Task);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-zero-field-outcome']").TextContent.ShouldContain("This item is unavailable."));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-zero-field-outcome']").ShouldBeEmpty());
        _ = service.Received(2).DispatchAsync(Arg.Any<ZeroFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }
}

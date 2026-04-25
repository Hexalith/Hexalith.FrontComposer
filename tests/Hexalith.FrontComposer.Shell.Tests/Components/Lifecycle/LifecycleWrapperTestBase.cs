using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Shared bUnit base for the Story 2-4 <see cref="FcLifecycleWrapper"/> test surfaces.
/// Registers FluentUI, localization, a deterministic <see cref="FakeTimeProvider"/>, a fake
/// <see cref="NavigationManager"/>, and exposes helpers for pushing transitions.
/// </summary>
public abstract class LifecycleWrapperTestBase : BunitContext {
    protected const string DefaultCorrelationId = "corr-test-001";

    protected LifecycleWrapperTestBase() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        _ = Services.AddLogging();
        _ = Services.AddOptions<FcShellOptions>();
        Services.TryAddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();
        _ = Services.AddSingleton<NavigationManager>(_ => new TestNavigationManager());
        FakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 16, 12, 0, 0, TimeSpan.Zero));
        _ = Services.AddSingleton<TimeProvider>(FakeTime);
        _ = Services.AddSingleton<IProjectionConnectionState, ProjectionConnectionStateService>();
    }

    /// <summary>The fake clock that drives both <c>GetUtcNow()</c> and registered <c>ITimer</c> callbacks.</summary>
    protected FakeTimeProvider FakeTime { get; }

    protected void RegisterLifecycleService(ILifecycleStateService service) {
        Services.RemoveAll<ILifecycleStateService>();
        _ = Services.AddSingleton(service);
    }

    protected CommandLifecycleTransition Transition(CommandLifecycleState previous, CommandLifecycleState next, string? messageId = "01HXXXXXXXXXXXXXXXXXXXXXXX") {
        DateTimeOffset now = FakeTime.GetUtcNow();
        return new CommandLifecycleTransition(
            DefaultCorrelationId,
            previous,
            next,
            messageId,
            TimestampUtc: now,
            LastTransitionAt: now,
            IdempotencyResolved: false);
    }

    protected static CommandLifecycleTransition TransitionAt(CommandLifecycleState previous, CommandLifecycleState next, DateTimeOffset anchor, string? messageId = "01HXXXXXXXXXXXXXXXXXXXXXXX", bool idempotencyResolved = false)
        => new(
            DefaultCorrelationId,
            previous,
            next,
            messageId,
            TimestampUtc: anchor,
            LastTransitionAt: anchor,
            IdempotencyResolved: idempotencyResolved);

    protected IRenderedComponent<FcLifecycleWrapper> RenderWrapperWithStubService() {
        ILifecycleStateService stub = Substitute.For<ILifecycleStateService>();
        _ = stub.Subscribe(Arg.Any<string>(), Arg.Any<Action<CommandLifecycleTransition>>())
            .Returns(Substitute.For<IDisposable>());
        RegisterLifecycleService(stub);
        return Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId)
            .AddChildContent("<span class='child-content-marker'>child</span>"));
    }

    protected (IRenderedComponent<FcLifecycleWrapper> Cut, Action<CommandLifecycleTransition> Push) RenderWrapperWithLiveService(
        string? rejectionMessage = null,
        string? rejectionTitle = null,
        string? idempotentInfoMessage = null) {
        ILifecycleStateService service = Substitute.For<ILifecycleStateService>();
        Action<CommandLifecycleTransition>? captured = null;
        _ = service.Subscribe(Arg.Any<string>(), Arg.Do<Action<CommandLifecycleTransition>>(cb => captured = cb))
            .Returns(Substitute.For<IDisposable>());
        RegisterLifecycleService(service);

        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId)
            .Add(c => c.RejectionMessage, rejectionMessage)
            .Add(c => c.RejectionTitle, rejectionTitle)
            .Add(c => c.IdempotentInfoMessage, idempotentInfoMessage)
            .AddChildContent("<span class='child-content-marker'>child</span>"));

        void Push(CommandLifecycleTransition transition) {
            if (captured is null) {
                throw new InvalidOperationException("Wrapper did not subscribe — implementation incomplete (Task 2.5).");
            }
            cut.InvokeAsync(() => captured(transition)).GetAwaiter().GetResult();
        }

        return (cut, Push);
    }

    protected (IRenderedComponent<FcLifecycleWrapper> Cut, Action<CommandLifecycleTransition> Push, FakeTimeProvider Time) RenderWrapperWithFakeTime() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        return (cut, push, FakeTime);
    }

    protected sealed class TestNavigationManager : NavigationManager {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/test");

        public (string Uri, bool ForceLoad)? LastNavigateCall { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad) => LastNavigateCall = (uri, forceLoad);

        protected override void NavigateToCore(string uri, NavigationOptions options) => LastNavigateCall = (uri, options.ForceLoad);
    }
}

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public abstract class CommandRendererTestBase : BunitContext {
    private readonly TestUserContextAccessor _userContext = new();
    private readonly TestCommandPageContext _pageContext = new();
    private bool _storeInitialized;

    /// <summary>JS module for <c>fc-expandinrow.js</c> (expand-in-row init + inline popover focus restore).</summary>
    protected BunitJSModuleInterop FcExpandInRowModule { get; }

    protected CommandRendererTestBase() {
        JSInterop.Mode = JSRuntimeMode.Strict;
        JSInterop.SetupVoid("Microsoft.FluentUI.Blazor.Utilities.Attributes.observeAttributeChange", _ => true)
            .SetVoidResult();
        JSInterop.SetupVoid("Microsoft.FluentUI.Blazor.Utilities.Attributes.disposeAttributeObserver", _ => true)
            .SetVoidResult();
        FcExpandInRowModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        _ = FcExpandInRowModule.SetupVoid("initializeExpandInRow", _ => true).SetVoidResult();
        _ = FcExpandInRowModule.SetupVoid("focusTriggerElementById", _ => true).SetVoidResult();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        _ = Services.AddLogging();
        _ = Services.AddFluxor(o => o.ScanAssemblies(typeof(CommandRendererTestBase).Assembly));
        _ = Services.AddHexalithDomain<CommandRendererTestBase>();
        _ = Services.AddScoped<ICommandService, StubCommandService>();
        _ = Services.Configure<StubCommandServiceOptions>(o => {
            o.AcknowledgeDelayMs = 0;
            o.SyncingDelayMs = 0;
            o.ConfirmDelayMs = 0;
        });
        _ = Services.AddOptions<FcShellOptions>();
        _ = Services.AddScoped<InlinePopoverRegistry>();
        _ = Services.AddScoped<LastUsedSubscriberRegistry>();
        _ = Services.AddScoped<ILastUsedSubscriberRegistry>(sp => sp.GetRequiredService<LastUsedSubscriberRegistry>());
        _ = Services.AddScoped<ILastUsedRecorder, NullLastUsedRecorder>();

        // Story 2-3 — generated forms resolve ILifecycleBridgeRegistry + IUlidFactory; full wire-up so
        // submits flow through the real service and tests can inspect lifecycle state when needed.
        _ = Services.AddOptions<LifecycleOptions>();
        _ = Services.AddSingleton<IUlidFactory, UlidFactory>();
        _ = Services.AddScoped<ILifecycleStateService, LifecycleStateService>();
        _ = Services.AddScoped<LifecycleBridgeRegistry>();
        _ = Services.AddScoped<ILifecycleBridgeRegistry>(sp => sp.GetRequiredService<LifecycleBridgeRegistry>());

        // Story 2-4 — FcLifecycleWrapper (wrapping every generated form) injects TimeProvider.
        _ = Services.AddSingleton(TimeProvider.System);
        _ = Services.AddScoped<IProjectionConnectionState, ProjectionConnectionStateService>();

        // Story 5-2 — generated forms inject the warning publisher + auth-redirect seam.
        _ = Services.AddScoped<ICommandFeedbackPublisher, CommandFeedbackPublisher>();
        _ = Services.AddScoped<IAuthRedirector, NoOpAuthRedirector>();
        _ = Services.AddScoped<IPendingCommandStateService, PendingCommandStateService>();

        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => _userContext));
        Services.Replace(ServiceDescriptor.Scoped<ICommandPageContext>(_ => _pageContext));
    }

    protected TestUserContextAccessor UserContext => _userContext;

    protected TestCommandPageContext PageContext => _pageContext;

    protected async Task InitializeStoreAsync() {
        if (_storeInitialized) {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }

    protected sealed class TestUserContextAccessor : IUserContextAccessor {
        public string? TenantId { get; set; } = "counter-demo";

        public string? UserId { get; set; } = "demo-user";
    }

    protected sealed class TestCommandPageContext : ICommandPageContext {
        public string CommandName { get; set; } = "Configure Counter";

        public string BoundedContext { get; set; } = "Counter";

        public string? ReturnPath { get; set; }
    }

    private sealed class NullLastUsedRecorder : ILastUsedRecorder {
        public Task RecordAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : class => Task.CompletedTask;
    }
}

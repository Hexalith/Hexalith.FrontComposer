---
title: "Test generated components"
description: "Use Hexalith.FrontComposer.Testing to render generated views and customization overrides without a running app host."
genre: how-to
audience: adopter
ownerStory: 10-1-adopter-test-host-and-component-testing-utilities
status: published
reviewed: 2026-06-05
uid: frontcomposer.how-to.test-generated-components
slug: how-to/test-generated-components/
---

# Test generated components

Configure failure paths through `host.CommandService`, request-sensitive query/page results through callback overloads, and policy decisions through `host.AuthorizationEvaluator`. Unknown policies fail closed. `host.FaultRecorder` records evidence only and does not emulate a transport.

For setup-time Fluxor initialization, await `AddFrontComposerTestHostAsync(...)` with `StoreInitializationMode.DuringHostSetup`; synchronous composition intentionally rejects that mode.

Add `Hexalith.FrontComposer.Testing` to an xUnit v3 + bUnit test project. The package configures FrontComposer Shell services, in-memory storage, deterministic user context, fake command/query/page-loader providers, loose JS interop, and assertion helpers.

## Inherit from the test base

```csharp no-compile reason="Generated Counter sample types are produced by the sample domain project, not the standalone docs snippet harness."
using Counter.Domain;
using Bunit;
using Fluxor;
using Hexalith.FrontComposer.Testing;
using Hexalith.FrontComposer.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class CounterViewTests : FrontComposerTestBase
{
    public CounterViewTests()
        : base(options =>
        {
            options.TestTenantId = "sample-tenant";
            options.TestUserId = "sample-user";
            options.DomainAssemblies.Add(typeof(CounterDomain).Assembly);
        })
    {
        Services.AddHexalithDomain<CounterDomain>();
    }

    [Fact]
    public async Task CounterProjectionView_LoadedState_RendersExpectedCells()
    {
        await InitializeStoreAsync();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [new CounterProjection { Id = "counter-1", Count = 12 }]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        cut.WaitForAssertion(() =>
        {
            GeneratedProjectionAssertions.AssertDataGridEnvelope(cut, "Counter");
            GeneratedProjectionAssertions.AssertCellValues(cut, "counter-1", "12");
        });
    }
}
```

Generated projection reducers accept a direct `LoadedAction` or `LoadFailedAction` only before any load request has been dispatched, as in the example above. **Breaking change:** once a request exists, a mismatched correlation is dropped, including push-style direct `LoadedAction` and `LoadFailedAction` values. After `ScopeChangedAction`, stamp `OriginScopeGeneration` from `state.ScopeGeneration`. Stamp `ScopeTenantId` and `ScopeUserId` from the resolved tenant context (`ProjectionStateScopeOwnership.ResolveCurrentScope`), the same source generated Retry uses. A scope change clears tenant and user on projection state, so those two fields cannot come from that state. Then dispatch a new `LoadRequestedAction` before `LoadedAction` or `LoadFailedAction`. Every result must carry that request's correlation. A result without a matching request is discarded so a late response from the prior tenant cannot refill the view. The adopter owns the request and its result; a scope change does not start a load automatically.

Non-grid Retry, and grid Retry when the only failure is `state.Error`, dispatches `{Type}LoadRequestedAction` with `OperatorInitiated: true`. No framework effect fetches on that action. An adopter whose projection can fail must answer the operator-initiated request: reload, then dispatch `Loaded` or `LoadFailed` with the same correlation, `OriginScopeGeneration` from `state.ScopeGeneration`, and tenant and user from the resolved tenant context.

## Compose the host directly

```csharp compile
using Bunit;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FrontComposer.Snippets;

public sealed class CommandDispatchTests
{
    public static async Task CommandServiceDispatchCapturesRedactedEvidence()
    {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);

        ICommandService commandService = context.Services.GetRequiredService<ICommandService>();
        await commandService.DispatchAsync(
            new ConfigureCounterCommand { Count = 7 },
            CancellationToken.None).ConfigureAwait(false);

        CommandDispatchEvidence evidence = host.CommandService.Evidence.Single();
        CommandEvidenceAssertions.AssertLifecycleContains(evidence, CommandLifecycleState.Confirmed);
        CommandEvidenceAssertions.AssertRedacted(evidence, "test-tenant", "test-user");
    }

    private sealed class ConfigureCounterCommand
    {
        public int Count { get; init; }
    }
}
```

The fake providers keep evidence per test context. They do not open EventStore, SignalR, DAPR, browser storage, or a running app host. Use `TestProjectionPageLoader.SucceedWith(...)` or `TestProjectionPageLoader.NotModified(...)` for server-side virtualization paths, `TestQueryService.SucceedWith<T>(...)` or `TestQueryService.NotModifiedWith<T>(...)` for query seams, and `TestFaultEvidenceRecorder` to record deterministic drop, delay, partial delivery, reorder, and reconnect-nudge evidence. Evidence formatting redacts configured tenant/user values and token, secret, or password keyed values before assertion output.

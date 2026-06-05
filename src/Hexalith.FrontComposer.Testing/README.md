# Hexalith.FrontComposer.Testing

Adopter-facing test utilities for rendering generated FrontComposer components with bUnit.

Use `FrontComposerTestBase` for inheritance-based tests, or call `Services.AddFrontComposerTestHost(...)` from a `BunitContext` for composition-based setup. Dispose the returned `FrontComposerTestHostBuilder` when composing directly so culture settings are restored. The package registers in-memory storage, deterministic tenant and user context, Shell services, command/query/page-loader fakes, loose JS interop defaults, and component assertion helpers without requiring a running app host.

The deterministic fakes are per test host instance. `TestCommandService` captures redacted dispatch evidence and lifecycle states, `TestQueryService` supports success and not-modified paths through `SucceedWith<T>()` and `NotModifiedWith<T>()`, and `TestProjectionPageLoader` supports configured pages plus not-modified evidence. `TestFaultInjectionProvider` records deterministic Drop, Delay, PartialDelivery, Reorder, and ReconnectNudge scenarios without opening a live SignalR connection.

Evidence formatting redacts configured tenant/user values and token, secret, or password keyed values before assertion output. The package public surface is pinned by `PublicAPI.Shipped.txt`; update that baseline intentionally when adding adopter-facing APIs.

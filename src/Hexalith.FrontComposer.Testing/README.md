# Hexalith.FrontComposer.Testing

Adopter-facing test utilities for rendering generated FrontComposer components with bUnit.

Commands support deterministic success, rejection, immediate timeout, and stall-at-`Syncing`. Queries and pages accept per-request callbacks with last-write-wins configuration. `TestAuthorizationEvaluator` is registered as the exact evaluator exposed by the host and fails closed for unknown policies.

`TestFaultEvidenceRecorder` records bounded, redacted scenario evidence only; it does not inject transport behavior. Use `AddFrontComposerTestHostAsync(...)` for `DuringHostSetup` store initialization and pass its optional cancellation token when test setup is cancelable. The synchronous API rejects that mode instead of blocking on async work.

Use `FrontComposerTestBase` for inheritance-based tests, or call `Services.AddFrontComposerTestHost(...)` from a `BunitContext` for composition-based setup. Dispose the returned `FrontComposerTestHostBuilder` when composing directly so culture settings are restored. The package registers in-memory storage, deterministic tenant and user context, Shell services, command/query/page-loader fakes, loose JS interop defaults, and component assertion helpers without requiring a running app host.

The deterministic fakes are per test host instance. `TestCommandService` captures redacted dispatch evidence and lifecycle states, `TestQueryService` supports success and not-modified paths through `SucceedWith<T>()` and `NotModifiedWith<T>()`, and `TestProjectionPageLoader` supports configured pages plus not-modified evidence.

Evidence formatting redacts configured tenant/user values and token, secret, or password keyed values before assertion output. The package public surface is pinned by `PublicAPI.Shipped.txt`; update that baseline intentionally when adding adopter-facing APIs.

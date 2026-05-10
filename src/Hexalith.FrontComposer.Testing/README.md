# Hexalith.FrontComposer.Testing

Adopter-facing test utilities for rendering generated FrontComposer components with bUnit.

Use `FrontComposerTestBase` for inheritance-based tests, or call `Services.AddFrontComposerTestHost(...)` from a `BunitContext` for composition-based setup. The package registers in-memory storage, deterministic tenant and user context, Shell services, command/query/page-loader fakes, loose JS interop defaults, and component assertion helpers without requiring a running app host.

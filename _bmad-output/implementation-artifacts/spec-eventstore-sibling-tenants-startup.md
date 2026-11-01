---
title: 'Resolve sibling Tenants hosts during EventStore Aspire startup'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore Aspire startup fails because its runtime Tenants resolver requires nested host projects even though the workspace provides the root-declared sibling Tenants checkout and build-time dependency resolution already supports it.

**Approach:** Prefer complete repository-local Tenants hosts for standalone behavior, then fall back to a complete sibling checkout. Keep both hosts in the same checkout. Preserve source/package switches, dependency versions, authentication, Dapr access controls, and existing changes. Never initialize nested submodules. Pull the latest EventStore main as explicitly requested, run focused path/model/build checks, start its AppHost, verify resource health, and report remaining blockers.

</frozen-after-approval>

## Implementation Notes

- User explicitly authorized preserving existing changes and clarified sibling fallback and prohibition on nested initialization; unrelated dirty root artifacts remain untouched. No staging, commit, or push is authorized.
- EventStore fast-forwarded from c02bc703a0348a2cf43c56d6fe04cb2d174fcb99 to f704401ff9033919b1e9925564d814d7410bfe3d with submodule recursion disabled. No submodule initialization/update was run.
- Reproduced exact EventStore Aspire startup failure after a successful Debug build: exit 2, missing both nested Tenants hosts at TenantsProjectPaths.Resolve. Build-time Directory.Build.props already resolves the sibling source checkout.
- Small reversible change with no intent gaps, public API, dependency upgrade, or migration. Limit implementation to EventStore TenantsProjectPaths, focused path/layout tests, and existing tests that hard-code the nested location. Leave Program.cs topology/security unchanged.

- Implemented repository-local-first, sibling-second resolution using repository-relative paths. Both runnable hosts must exist in one candidate checkout; no cross-checkout mixing. Startup diagnostics list missing candidates and prohibit nested initialization.
- Existing launch-profile tests now use the selected checkout; eight isolated layout cases cover standalone priority, sibling fallback (including partial local checkouts), absent hosts, and split-host rejection.
- `dotnet msbuild src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=true -getProperty:HexalithTenantsBasePath,HexalithTenantsFromSource -getItem:ProjectReference` confirms existing build-time source selection points both Tenants hosts at the workspace root-declared sibling. No dependency configuration or version change was required.
- From the EventStore repository: `dotnet build tests/Hexalith.EventStore.AppHost.Tests/Hexalith.EventStore.AppHost.Tests.csproj -c Debug -m:1 --verbosity minimal` passed with zero warnings/errors. Focused built xUnit v3 assembly run with classes `*TenantsProjectPathsTests`, `*TenantsApiLaunchSettingsTests`, `*AppHostProjectConfigurationTests`, `*AppHostAuthenticationModelTests`, `*AppHostTrustBoundaryModelTests` and `-parallel none`: 30 passed, zero failed/skipped. Logs: `/tmp/eventstore-apphost-tests-build.log`, `/tmp/eventstore-apphost-focused-tests.log`.
- Exact-target `aspire start --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj --non-interactive --format Json` passed. `aspire wait RESOURCE --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj --status healthy --timeout 60 --non-interactive` passed for security, eventstore, tenants; tenants-api used 15 seconds, and eventstore-admin, eventstore-admin-ui, sample, sample-api, sample-blazor-ui used 30 seconds. Final `aspire describe --include-hidden --format Json` showed all nine project/container resources and all eight Dapr sidecars Running/Healthy.
- Remaining functional issues observed through `aspire otel logs --search 'severity:error'` and resource console logs: sample-blazor-ui SignalR negotiation returns HTTP 401, and Keycloak logs invalid_client_credentials for EventStore workload token requests. These do not prevent startup/readiness. Authentication and access-control configuration remain unchanged; functional authorization remediation is separate from host path resolution. Certificate tooling also reported partial local trust during baseline startup; no certificate configuration was changed.

- Independent review reported four maintenance/documentation findings; all were addressed locally. Removed the unused Validate method and moved its two missing-host checks onto the production resolver; clarified that the API launch-profile contract test intentionally requires a complete runtime checkout; removed the overbroad ban on all initialization-command text from the diagnostic test; updated quickstart, development guide, and package-reference documentation with complete-checkout precedence and workspace-root-only dependency initialization. No deferred code findings remain.
- After review corrections, the focused Debug test-project build passed again with zero warnings/errors. Ten layout cases now exercise the production resolver directly. Final test logs: `/tmp/eventstore-apphost-tests-build-final.log`, `/tmp/eventstore-apphost-focused-tests-final.log`.

## Review Triage Log

- R1 — low, patch: Validate had become test-only duplication; removed it and exercised incomplete-checkout diagnostics through Resolve instead.
- R2 — false as a runtime defect: a complete pair is an intentional startup prerequisite; clarified the launch-profile test's documentation to state it checks the complete selected runtime checkout.
- R3 — low, patch: removed the diagnostic assertion rejecting all git submodule update text; retained the explicit prohibition on nested initialization.
- R4 — medium, patch: corrected all three setup references that claimed repository-local-only discovery and unsafe unconditional initialization advice.

- Final reviewed binary: `aspire start --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj --no-build --non-interactive --format Json` passed after the final zero-warning Debug build and 30/30 focused tests. Repeated security, gateway, Tenants hosts, and sample UI health waits passed. `/tmp/eventstore-resource-health-reviewed.json` confirms all nine project/container resources and all eight sidecars Running/Healthy.

- Final exact-target `aspire stop` succeeded; `aspire describe` confirms no EventStore AppHost remains running. `git diff --check` passed in both owning EventStore and workspace repositories. EventStore nested submodules remain uninitialized (`git submodule status` entries all start with `-`). Existing root edits remain untouched; no staging, commits, pushes, dependency-version edits, or security changes were made. Startup is repaired; the reported SignalR/workload-credential functional issues remain outside this path-resolution fix.

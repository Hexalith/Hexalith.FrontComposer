# Hexalith.FrontComposer — Architecture & Engineering Quality Review

Date: 2026-07-04 · Scope: all 9 `src/` projects + 7 test projects + `tests/ci-governance` + `tests/e2e` (~66k LOC src, ~80k LOC tests). Analysis only — no code was modified. Produced from five parallel deep-dive reviews (Contracts kernel; Shell services/state/infrastructure; Shell components/UI host; Mcp/Schema/SourceTools/Cli/Testing/AppHost/samples; cross-cutting duplication/build/tests).

## Executive summary

This is an unusually well-governed codebase: zero TODO/HACK/FIXME in src, zero skipped tests, central package management with zero inline versions, CI-workflow-pinning governance tests, PublicAPI baselines, Pact contracts, FsCheck properties, and exemplary fail-closed tenancy and MCP security engineering. **No Critical findings** — nothing observed corrupts data or crosses tenants.

The problems are concentrated in four themes:

1. **A handful of real runtime bugs** that slipped past a strong test suite because they live in the classes of defect bUnit/compilers can't see: silent Blazor parameter splats, unlinked/dead CSS, generated-code emission for untested type shapes, and a DI lifetime mismatch.
2. **Contracts kernel scope creep** — the "netstandard2.0 wire kernel" is in fact a dual-TFM assembly leaking Fluent UI and Shell internals to every consumer, which is the main architectural risk for reuse by Hexalith.Tenants.
3. **Convention drift at scale** — the one-type-per-file and source-generated-LoggerMessage rules from the Hexalith guidelines are honored more in the breach (~60 multi-type files; 1 compliant logging file vs 206 direct logger call sites in the Shell alone).
4. **Consolidation debt from story-by-story development** — the same security-adjacent helpers (string escaping for codegen, storage-key canonicalization, kebab-case slugs, tenant-scope resolution, snapshot pub/sub) exist in 2–7 divergent copies, with hardening fixes applied unevenly across copies.

## 1. Architecture assessment

**Actual shape.** `Contracts` (dual-TFM netstandard2.0/net10.0) is the kernel consumed by everything. `Shell` (Blazor library: Fluxor state, EventStore HTTP/SignalR clients, registries, layout/rendering components) references only Contracts. `SourceTools` (Roslyn incremental generator + analyzer, netstandard2.0) emits views/state/forms/manifests from `[Projection]`/`[Command]` attributes. `Mcp` (real MCP server on the official SDK) exposes generated command/projection manifests to agents. `Schema` is a tiny neutral diff kernel between SourceTools and Mcp. `Cli` is a dotnet tool (inspect/migrate). `Testing` is the adopter-facing bUnit harness. `UI` + `AppHost` are the host/Aspire composition roots; `samples/Counter` demonstrates the full Level 1→4 customization ladder through public APIs only.

**What holds.** The project-level dependency rules are verified honored (Mcp→Contracts+Schema, SourceTools→Contracts, Testing→Contracts+Shell); boundaries between SourceTools/Schema/Mcp are crisp with no meaningful overlap; DI lifetimes in the Shell are explicitly reasoned and even guarded at runtime; the samples teach the right consumption pattern; the docs' "attributes are the source model, everything is generated from it" story matches the code.

**What doesn't.**
- *Contracts purity* (F1/F4/F8/F9 below): Fluent UI types, `RenderFragment` contexts, Shell option grab-bags, Fluxor action records (one holding a live `TaskCompletionSource`), and two stateful runtime services live in the "contracts" package.
- *Shell internal layering*: Infrastructure↔State are mutually dependent, and State effects call static helpers on a Razor component (`FrontComposerNavigation.BuildRoute/ProjectionLabel`) — routing logic hosted on the render layer.
- *Change notification*: four coexisting idioms (Fluxor, hand-rolled subscribe/snapshot containers, plain .NET events, Rx `Subject<T>` — the only consumer of the System.Reactive dependency).

**Reusability by Hexalith.Tenants.** Mostly ready today: `DomainManifest`/`FrontComposerNavEntry` are exemplary UI-neutral contracts (string icon keys, resource-marker localization), `FcAggregateListPage`/`FcAggregateDetailPage` are cleanly slot-based and fail-closed, nothing hardcodes tenant routes. The real frictions: (a) referencing Contracts drags in the pinned Fluent RC package on net10; (b) the Testing harness can only simulate success — a Tenants developer cannot test command rejection/timeout UX; (c) `TryResolveScope` and the persisted-feature skeleton must be copy-pasted (7th copy) rather than reused; (d) sample tenant names (`"counter"`, `"counter-demo"`) are baked into framework tenancy code.

## 2. Findings by severity

### Critical

None.

### High

**H1. UI host pages pass nonexistent parameters to `FcPageHeader` — pages render with no visible text.**
`src/Hexalith.FrontComposer.UI/Components/Pages/AdminLanding.razor:6-7`, `Pages/NoPartyBinding.razor:5-6`, `Components/Routes.razor:16-17` pass `Title=`/`Subtitle=`, but the component's parameters are `PageTitle`/`Heading`/`Description` (`FcPageHeader.razor.cs:40-49`). Because the component has `CaptureUnmatchedValues=true`, the values are silently splatted as HTML attributes, `Heading` stays blank, and the blank-heading fail-safe suppresses the `<h1>` — the Forbidden page and no-party-binding dead-end show nothing, and `FocusOnNavigate Selector="h1"` has no target. Fix the parameter names; add a host-side parameter-surface test mirroring `FrontComposerShellParameterSurfaceTests`.

**H2. Singleton token store never evicts; sign-out removal is dead code.**
`src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:17-28` — `FrontComposerUserTokenStore` holds raw bearer tokens in a process-wide `ConcurrentDictionary` keyed by user id. `Remove` has zero callers (only `Set` from the OIDC `OnTokenValidated` hook). Tokens of signed-out users persist for process lifetime, stale tokens are relayed downstream (401s with no refresh path), and growth is unbounded. Store token+expiry, evict on expiry, and wire `Remove` into the sign-out endpoint.

**H3. Generated command forms do not compile for nullable numeric command properties.**
`src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:941` emits `_model.{Prop}.ToString(CultureInfo.CurrentCulture)` regardless of `IsNullable`; `int?`/`decimal?` have no such overload → CS1501 in the adopter's project. The parser explicitly supports nullable numerics; no test fixture declares one, so the compile-the-output tests never hit it. Branch on `IsNullable` and add the fixture.

**H4. MCP lifecycle tracker registered Scoped but holds cross-request state — lifecycle polling inert as shipped.**
`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:41`. Follow-up `lifecycle.subscribe` calls resolve a fresh tracker; scope disposal kills its timers. Tests mask this by re-registering it Singleton (`CommandLifecycleTests.cs:754`). The class is already thread-safe — register Singleton and add a cross-scope hosting test.

**H5. `fc-empty-state.scoped.css` is never linked — projection empty states and field placeholders ship unstyled.**
The file defines `.fc-empty-state-body*` and the `.fc-field-placeholder` dashed-card affordance; `FrontComposerShell.razor:24-28` links only the other three stylesheets, and no `<link>` to it exists repo-wide. Merge into `fc-projection.css` or add to `HeadContent`; add a governance test asserting every `wwwroot/css` file is referenced.

**H6. SignalR reconnect gives up permanently after the default retry ladder.**
`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs:20` uses default `WithAutomaticReconnect()` (4 attempts, ~42 s). After that the connection is `Closed` forever; nothing restarts existing subscriptions, so a long-lived circuit that survives a >42 s outage silently degrades to 15 s fallback polling for its remaining lifetime. Supply an unbounded jittered `IRetryPolicy` or restart on `Closed` gated by the fallback driver.

**H7. `ReturnPathValidator` — security-critical open-redirect validator with zero direct tests.**
`src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` (121 lines: protocol-relative prefixes, percent-decode re-check, `..` traversal, BiDi/zero-width spoofing, Unix `file`-scheme carve-out) is documented as the single security funnel for return paths and is delegated to by generated code (`CommandRendererEmitter.cs:544-546`). Only two indirect cases exercise it. An unnoticed regression is an open-redirect vulnerability. Add an exhaustive theory per documented attack class — the doc comments already read as the test list.

**H8. Five `EscapeString` implementations for generated C# literals; one is demonstrably weaker.**
`RegistrationEmitter.cs:88-95` uses a naive `.Replace()` chain missing ` `/` ` (C# line terminators), `\b`, `\f`, `\v` and other control chars; `RoleBodyHelpers.cs:179` hand-rolls a stronger variant; three emitters correctly use `SymbolDisplay.FormatLiteral`. An adopter label containing a line separator flows through RegistrationEmitter into uncompilable or meaning-changed generated code — the exact bug class already fixed per-emitter in review passes P38/P40. Extract one `GeneratedLiteral.Escape` on `SymbolDisplay.FormatLiteral`; delete the other four.

**H9. Two storage-key builders with conflicting canonicalization — security-adjacent.**
`Shell/State/StorageKeys.cs:21-48` (raw segments, throws on `:`) vs `Shell/Services/FrontComposerStorageKey.cs:34-105` (trims, NFC-normalizes, lowercases emails, URL-encodes `:`). The same `(tenant, user)` yields different key spaces depending on the feature (Fluxor persistence vs LastUsed); both files carry cross-tenant-bleed rationale, so divergence undermines the guarantee. Converge on the canonicalizing variant; add an equivalence property test.

**H10. Two divergent kebab-case slug algorithms feed user-facing routes.**
`Routing/CommandRouteBuilder.cs:26-61` is boundary-aware (`XMLParser` → `xml-parser`, the documented D21 contract); `FrontComposerNavigation.razor.cs:519-535` `ToKebab` hyphenates every uppercase (`XMLParser` → `x-m-l-parser`) and feeds nav rendering, home directory, and palette route URLs. Any acronym-bearing projection gets different slugs from different subsystems, and nothing pins the resolving page to either. Make `ToKebab` delegate to `CommandRouteBuilder.KebabCase` (or freeze the divergence with acronym pinning tests).

**H11. The Contracts "kernel" is a dual-personality assembly with Blazor/Fluent leakage.**
`Hexalith.FrontComposer.Contracts.csproj:16-22` multi-targets net10.0/netstandard2.0; the net10 surface references `Microsoft.FluentUI.AspNetCore.Components` (pinned RC) for `Rendering/Typography.cs`, and `RenderFragment` appears in `ProjectionViewContext`/`ProjectionTemplateContext`/`FieldSlotContext`; `IShortcutService` takes `KeyboardEventArgs`. Every net10 consumer — including Tenants UI — transitively inherits the Fluent RC pin, coupling all modules' Fluent version to the kernel; generators and hosts see different public APIs of the same package. Split the Blazor surface into a net10-only `Contracts.Rendering.Blazor` (or `.UI`) assembly.

**H12. AppHost blanket-suppresses all NuGet vulnerability audit warnings.**
`Hexalith.FrontComposer.AppHost.csproj:10` NoWarns NU1902/NU1903/NU1904 — a permanent blindfold for future high/critical advisories. Replace with per-advisory `NuGetAuditSuppress` items, or keep NU1903/NU1904 fatal.

### Medium

**Correctness / robustness**
- **M1. Access-token provider throws during normal circuit activity.** `Services/Auth/FrontComposerAccessTokenProvider.cs:73-81` reads only `IHttpContextAccessor` and throws HFC2013 when `HttpContext` is null — the normal state during Blazor Server interactive work. Sibling seams fall back to `CircuitServicesAccessor`; this one doesn't, so live-circuit EventStore auth silently depends on adopters wiring `TokenRelay.HostAccessTokenProvider`. Add the circuit-safe fallback or fail fast at registration.
- **M2. Singleton `FrontComposerRegistry` mutates unsynchronized `List<T>`s.** `Registration/FrontComposerRegistry.cs:85,115,164` — `TryGetCommandPolicy` snapshots specifically to dodge a startup race, but `GetManifests()` returns the live list and `HasFullPageRoute` enumerates it unlocked. Guard with a lock or `ImmutableList` swap.
- **M3. `ProjectionSubscriptionService.DisposeAsync` can hang** (`Infrastructure/EventStore/ProjectionSubscriptionService.cs:174` — gate wait with no timeout while a wedged `StartAsync` holds it). Also: dead `catch { throw; }` at lines 151-156; two polling drivers with inconsistent disposal (one awaits its loop, the other disposes the CTS under an in-flight poll).
- **M4. `ETagCacheService` seeding race** (`State/ETagCache/ETagCacheService.cs:302-316`): seeded flag set regardless of completion — concurrent writes run against a partially seeded LRU and a failed seed permanently exempts persisted entries from eviction. Use `Lazy<Task>`/semaphore and reset on failure.
- **M5. `LocalStorageService.GetKeysAsync` uses JS `eval`** (`Infrastructure/Storage/LocalStorageService.cs:145-148`) — breaks under CSP without `unsafe-eval`, forcing hosts to weaken CSP. Ship a small JS module export instead.
- **M6. Scoped-CSS dead rules in 7 component stylesheets** — the project's own documented gotcha (class on a Fluent component gets no scope attribute): `FcProjectionConnectionStatus.razor.css` (all rules dead, incl. the reconnect pulse animation), `FcColumnPrioritizer` (gear pinning), `FcSettingsDialog` (mobile full-width Done), `FcDensityPreviewPanel` (the very affordance it demos), `FcDevModeToggleButton`/`FcDevModeAnnotation`/`FcDevModeOverlay`. Wrap in a raw scoped root + `::deep` or inline Style; add a governance test for the pattern.
- **M7. `FcSystemThemeWatcher` lacks the disposal-race hardening of its sibling** (`FcSystemThemeWatcher.razor.cs:55-88`: catches only `JSException`, no `_disposed` re-check after awaits → leaked matchMedia listener holding a disposed `DotNetObjectReference`). Port the `FcLayoutBreakpointWatcher` pattern. Related: `fc-prefers-color-scheme.js:19-27` lacks `.catch(() => {})` on invokes.
- **M8. Missing `@key` on data-driven loops that reorder at runtime** — `FcHomeDirectory.razor:81-137` (cards live-resort as counts arrive), `FrontComposerNavigation.razor:70,143,179` (menu/tooltip anchored by id — positional reuse risks mispaired flyout state), `FcPaletteResultList.razor:26-63`.
- **M9. MCP fail-closed with zero operator signal** — `FrontComposerMcpProjectionReader.cs:118-120` bare-catches to `downstream_failed` with no `ILogger` at all; same in the tools-list and lifecycle paths of `FrontComposerMcpServiceCollectionExtensions.cs:134-175`. Agent-facing redaction is right; add sanitized `[LoggerMessage]` logging.
- **M10. `BuildServiceProvider()` inside `AddFrontComposerMcp`** (`FrontComposerMcpServiceCollectionExtensions.cs:81`) — second container at registration (ASP0000); skill corpus parsed/fingerprinted twice; later options mutations ignored.
- **M11. Wire DTOs pinned by convention only.** `ProjectionChangedDetail`, `CommandResult`, `ProblemDetailsPayload`, `QueryRequest` carry no `[JsonPropertyName]` and no serialized-shape test, while cross-repo wire compatibility with EventStore is maintained by convention. Add golden-JSON round-trip tests or property-name pins. Also `CommandResult.Status` is stringly-typed with no constants.
- **M12. MCP API keys are plaintext config values keyed by the secret itself** (`FrontComposerMcpOptions.cs:20`); comparison is constant-time (good) but keys sit raw in config/options dumps. Store hashes or document dev-only.
- **M13. AppHost fragility**: repo-root computed as `BaseDirectory + "../../../../.."` (`ProjectMetadataPaths.cs:14-15` — breaks under publish/artifacts layouts); hard-coded dev OIDC secrets in `Program.cs:170,181`; conditional cross-repo references silently skipped when submodules are missing (failure surfaces only at `aspire run`).

**Conventions / structure**
- **M14. One-type-per-file violated at scale (~60 files).** Worst: `Cli/MigrationCommand.cs` (1,463 lines, 23 types), `Mcp/Skills/SkillCorpus.cs` (2,034 lines, ~45 types incl. an LLM benchmark harness that arguably shouldn't ship in the runtime package), `SourceTools/Drift/DriftDetection.cs` (1,665 lines, 17 types), `Cli/InspectCommand.cs` (14), `Contracts/Schema/SchemaFingerprintContracts.cs` (13), `Shell/State/CommandPalette/CommandPaletteActions.cs` (11), `Shell/Options/FrontComposerAuthenticationOptions.cs` (9 public classes), plus 14 Contracts files, 31 Shell files, 4 component code-behinds, 5 Testing files. Split at minimum files mixing public interfaces + implementations + DTOs; if Fluxor action groups are a deliberate exception, document it.
- **M15. LoggerMessage convention adopted in exactly two files repo-wide.** Shell: 206 direct `_logger.LogX` sites in 50 files vs one compliant file (`FrontComposerLog.cs`); Mcp likewise (only the lifecycle tracker complies), including hot per-event paths. Migrate Warning-and-above and hot paths; enforce with an analyzer.
- **M16. XML-doc enforcement is dead configuration.** `src/Directory.Build.props:7` NoWarns 1591 solution-wide, silently defeating `.editorconfig:73-85`'s scoped CS1591=warning blocks for Contracts public folders. Concrete gaps: the schema/fingerprint surface, `FrontComposerRenderContract.cs` (all 4 types), `FrontComposerMcpFailureCategory` (26 undocumented members that are the public error contract), the entire Cli project. Remove 1591 from NoWarn at least for Contracts, or delete the dead config.
- **M17. Thin analyzer posture for a framework repo** — no `AnalysisMode`/StyleCop/Roslynator anywhere; `TreatWarningsAsErrors` currently guards mostly compiler warnings. Set `AnalysisMode Recommended` and burn down.
- **M18. Layering tangle in Shell** — Infrastructure imports State (`ProjectionSubscriptionService.cs:9-11`, `EventStoreQueryClient.cs:11`) while State imports Infrastructure.Telemetry; several "State" types are background workers/caches. `CommandPaletteEffects.cs:11,364,818` and NavigationEffects call statics on the `FrontComposerNavigation` Razor component. Declare the real layering (Telemetry cross-cutting; pollers/connection into Infrastructure), move `BuildRoute`/`ProjectionLabel` next to `CommandRouteBuilder`.
- **M19. Four change-notification idioms** (Fluxor; hand-rolled snapshot containers; .NET events; Rx `Subject<T>` in `BadgeCountService` — sole consumer of System.Reactive). Converge on one non-Fluxor primitive; dropping Rx sheds a dependency from every adopter.
- **M20. Infrastructure bypasses the tenant-accessor seam via static calls** (`EventStoreQueryClient.cs:46-53`, `ProjectionSubscriptionService.cs:514,532` call the concrete static `FrontComposerTenantContextAccessor.Resolve` while `IFrontComposerTenantContextAccessor` is a replaceable registration) — custom accessors change some call sites but not query/subscription validation. Inject the interface or document the static path as non-replaceable policy.
- **M21. Testing harness is happy-path only.** `TestCommandService.cs:43-70` can only succeed (no rejection/timeout/stall); `TestFaultInjectionProvider` records evidence but injects nothing; `TestQueryService`/`TestProjectionPageLoader` ignore paging/filter/sort, so generated-grid virtualization isn't genuinely testable; sync-over-async in host setup (`FrontComposerTestBase.cs:30`). This is the single biggest gap for Tenants adoption.
- **M22. SourceTools model-equality inconsistency** — projection models include source location in equality (any line shift re-runs five emitters: incremental-cache churn) while command models exclude it (stale drift diagnostic locations). Pick one contract. Also the trim/AOT advisory walks the full compilation semantically per IDE keystroke when `PublishTrimmed/PublishAot` is set (`FrontComposerGenerator.cs:108-131,305-339`) — pre-filter or move to an analyzer.
- **M23. CLI duplicates SourceTools artifact-naming knowledge with no shared contract** (`InspectCommand.cs:430-444` suffix table vs generator hint names — commands without a "Command"-suffixed type name are misclassified). Extract shared naming constants (a `GeneratedOutputPathContract` already exists in Contracts.Conformance). Also within the CLI: two near-identical diagnostics-sidecar readers and three `IsSameOrUnder` copies.
- **M24. Contracts contains runtime services and Shell tunables** — `Storage/InMemoryStorageService.cs` (belongs in Testing), `Rendering/InlinePopoverRegistry.cs` (stateful scoped service that swallows close failures because it can't take an `ILogger` in the kernel — a smell caused by wrong placement), `FcShellOptions.cs` (344 lines of Shell runtime knobs whose every change revs the Contracts package), and Fluxor action records incl. `LoadPageAction` carrying a live `TaskCompletionSource` (broken value semantics, non-serializable — an in-process coordination object wearing a DTO costume). Relocate to Shell/Testing.
- **M25. `QueryRequest` fuses two abstraction layers in a 19-positional-parameter record** (`Communication/QueryRequest.cs:25-49`: grid concerns + EventStore transport/caching concerns + one deprecated member). Split into a UI-facing `ProjectionQuery` + transport envelope before v1.0; the HFC0001 deprecation machinery already exists to do it cleanly.
- **M26. Samples NoWarn the framework's own diagnostics with a stale justification** (`Counter.Domain.csproj`, `IdeParityCounter.csproj`: `ASP0006;HFC1002` — "will be fixed in generator Story 1.8"; repo is at Story 9-x). Samples are the reference pattern; fix or re-justify.
- **M27. Diagnostic-ID semantic drift** — `DensityEffects.cs:103,115` logs density hydration under `HFC2106_ThemeHydrationEmpty`; rename the constant (ID string unchanged). Fatal-exception filters exist in 3 variants across ~25 sites — unify a `IsFatal` helper.
- **M28. Two `IUlidFactory` implementations with different semantics** — Shell wraps non-monotonic `NUlid`; Mcp hand-rolls a monotonic, clock-skew-defended generator with no dedicated spec test for its overflow/regression branches. Keep one (property-tested) implementation beside the contract.

### Low (selected)

- `CommandAuthorizationEvaluator.cs:27` falls back to `Guid.NewGuid()` for correlation ids unconditionally (never consults `IUlidFactory`); three other sites fall back only on unresolvable factory — against the ULID convention.
- `BadgeCountService` (`.cs:304,320`, `:271`): the repo's single `async void` handler races `_lifetimeCts` disposal; `GetOrAdd` factory race leaks a lane registration.
- Hardcoded English aria-label in `FcHomeCard.razor:19-21` (rest of Shell is localized, French satellite ships); UI host hardcodes `lang="en"` and English strings while enabling request localization.
- `FcStatusFilterChips.razor:22` puts `@onclick` on `FluentBadge` — same shape as the known FluentMenuItem runtime fault, and the chips aren't keyboard-activatable (a11y gap). Needs an e2e smoke.
- Undefined/legacy CSS tokens escaping the governance regex: `--error-foreground-rest` (FAST-era) in `FcFieldPlaceholder.razor.css:8`, undefined `--error` in `FcDevModeAnnotation.razor.css:19`; extend the `LegacyFluentToken` guard.
- `FcAccountMenu` reads auth state once and never observes `AuthenticationStateChanged` (minimal impact — sign-in/out use forceLoad).
- Effects construction inconsistent (service-locator in palette/navigation vs constructor injection elsewhere); primary-constructor and `Async`-suffix conventions mixed across older/newer Shell files.
- CLI: cancellation exits with code 4 ("apply/write failure") even for read-only inspect; migration scanner matches any identifier named `AddFrontComposerDebugOverlay` without symbol resolution; misleading message when `dotnet` is missing.
- SourceTools: cancellation swallowed into synthetic empty parse results instead of `ThrowIfCancellationRequested`; HFC1007 has two descriptor identities; emitted identifiers not keyword-escaped (`@if` property → CS1001); drift redaction heuristic can fail-closed on identifiers containing `eyJ`.
- Mcp low items: projection `RequestId` uses `Guid.NewGuid()` beside a shipped ULID factory; `ResolveType` copy-pasted with uncached AppDomain scans; tools/list rebuilds every input schema per request; K&R braces contradict the Allman `.editorconfig` rule.
- Hygiene: `*.lscache` files neither ignored nor intentionally tracked (several already committed under samples); stray tracked `evtest/run-metadata.json`; `Directory.Packages.props` comment says SDK 10.0.300, `global.json` pins 10.0.301; two unrelated classes both named `RazorEmitter` (Shell DevMode vs SourceTools).

## 3. Performance

The hot paths are largely healthy by design: JS-side 30 Hz scroll throttling + .NET-side semantic debounce, `IStateSelection` projections instead of whole-state subscriptions, bounded caches/queues everywhere (LRU caps, response-body byte caps, log-suppression buckets), `TimeProvider` throughout, and a benchmarked palette scorer with a CI-enforced <200 μs p95 budget. Remaining risks, in priority order:

1. **Command palette authorization churn** (`CommandPaletteEffects.cs:830-869`): each debounced keystroke enumerates all manifests × commands and runs an async `IAuthorizationService` evaluation per protected command with no decision cache. Fine today; will degrade with large domain catalogs. Cache decisions per (policy, circuit), invalidate on auth-state change.
2. **SourceTools trim/AOT advisory** does a full semantic walk of every type declaration per IDE keystroke when trimming is enabled (M22) — the only generator-side scalability issue in an otherwise textbook incremental pipeline; projection-model equality including source location also defeats incremental caching.
3. **Render-path recomputation** (bounded, low): `FrontComposerNavigation` enumerates the registry 3× per render; `FcPendingCommandSummary` re-sorts the same list ~6× per render in property getters; `FcProjectionSubtitle` does `GetCustomAttribute` reflection in getters per render (cache per type); `FcHomeDirectory` runs OrderBy/Where chains in markup per Fluxor tick. Memoize if domain counts grow; add the missing `@key`s (M8) to avoid churn amplification.
4. **Mcp**: uncached AppDomain type scans per invocation and per-request tools/list schema rebuilds — cache both.
5. **Testing evidence**: `Evidence` snapshots the queue on every access; `DispatchAsync` reads `Count` twice — O(n) copies per dispatch in adopter test suites.

No sync-over-async in the Shell (the single guarded `.Result` is documented); CA2007/`ConfigureAwait(false)` compiler-enforced; zero unsupervised fire-and-forget found.

## 4. Factorization and reuse

| # | Cluster | Sites | Verdict | Where it should live |
|---|---------|-------|---------|----------------------|
| 1 | C#-literal escaping for codegen (H8) | 5 SourceTools emitters | **Useful — do first** | `SourceTools/Emitters/GeneratedLiteral.cs` on `SymbolDisplay.FormatLiteral` |
| 2 | Storage-key canonicalization (H9) | 2 Shell builders | **Useful — security-adjacent** | Single canonicalizing builder in Shell; Contracts documents the format |
| 3 | Kebab-case slug (H10) | Shell routing vs nav component (+ a third convention in generated command pages) | **Useful** | `CommandRouteBuilder.KebabCase` as the only slugger |
| 4 | `TryResolveScope` tenant/user fail-closed guard | 6 Fluxor effects + 2 near-variants | **Useful** — also unblocks Tenants writing persisted features without a 7th copy | Shell-internal `StorageScopeResolver` |
| 5 | Snapshot pub/sub (`lock` + handler list + `Subscription`) | ~7 hand-rolled implementations, hardening applied unevenly | **Useful** | Shell-internal `SnapshotPublisher<T>`; or standardize on Rx and make the hand-rolled ones the anomaly |
| 6 | Registry component/contract validation | 3 Projection* registries (~60 duplicated lines each) | **Useful for the two helpers; PREMATURE for a registry base** — duplicate-handling policies deliberately differ | `Services/Customization/` |
| 7 | ULID generation (M28) | Shell (non-monotonic) vs Mcp (monotonic hand-rolled) | **Useful** | Beside the Contracts interface (TFM-guarded) or one shared internal file |
| 8 | Fatal-exception filter | 3 variants, ~25 sites | Useful (tiny) | Shell internal `ExceptionGuard.IsFatal` |
| 9 | Hydration enums (`Idle/Hydrating/Hydrated`) | 5 identical enums | Useful (trivial) | `Shell/State/HydrationState.cs` |
| 10 | `JsonSerializerOptions` configs | 3 src sites | Useful (trivial) | Shell internal `FcJson` (leave the test-side copies — they intentionally pin wire shape) |
| 11 | Generated-artifact naming (M23) | CLI classifier vs generator hint names | **Useful** | Contracts.Conformance (extend `GeneratedOutputPathContract`) |
| 12 | Polling drivers | 2 drivers, different timer models | **Premature** — only dispose scaffolding matches; align the disposal pattern, don't unify |
| 13 | Fluxor effects hydrate/persist skeleton | 5 feature effects | **Premature as a base class** — per-feature semantics dominate; extract clusters 4/9/10 and the residue is small |
| 14 | AppHost `IProjectMetadata` micro-classes | 7 files | **Premature** — idiomatic Aspire; path table already centralized |

Placement note: nothing here belongs in `Hexalith.Commons` (treated as an external pinned submodule); extractions are Shell- or SourceTools-internal, except ULID (Contracts-adjacent) and artifact naming (Contracts.Conformance).

## 5. Prioritized refactoring roadmap

**Phase 1 — Quick wins (hours each, low risk, no API impact):**
1. Fix `FcPageHeader` parameter names in the three UI-host pages (H1) + host parameter-surface test.
2. Link/merge `fc-empty-state.scoped.css` (H5) + stylesheet-reference governance test.
3. Branch on `IsNullable` in `CommandFormEmitter` numeric binding (H3) + nullable-numeric fixture.
4. Register the MCP lifecycle tracker as Singleton (H4) + cross-scope test; remove the test-side re-registration masks.
5. Extract `GeneratedLiteral.Escape` and delete the four other escapers (H8) + FsCheck escaping property test.
6. Make `FrontComposerNavigation.ToKebab` delegate to `CommandRouteBuilder.KebabCase` (H10) + acronym pinning tests.
7. Replace AppHost blanket `NU1902-04` NoWarn with per-advisory suppressions (H12).
8. Add sanitized `[LoggerMessage]` logging to the MCP swallow sites (M9).
9. Add `@key` to the three dynamic loops (M8); port breakpoint-watcher disposal hardening to `FcSystemThemeWatcher` (M7); add `.catch` to `fc-prefers-color-scheme.js`.
10. Hygiene: gitignore `*.lscache`, delete the dead `catch { throw; }`, rename `HFC2106` constant, localize the `FcHomeCard` aria-label, fix the SDK-pin comment, remove `evtest/`.

**Phase 2 — Medium-risk improvements (days each, behavioral surface):**
1. Token store: expiry-based eviction + wire `Remove` into sign-out (H2); circuit-safe fallback in `FrontComposerAccessTokenProvider` (M1).
2. Unbounded jittered `IRetryPolicy` for the projection hub + restart-on-Closed (H6).
3. Converge the two storage-key builders on the canonicalizing variant (H9) + equivalence property test.
4. `ReturnPathValidator` exhaustive test theory (H7).
5. Scoped-CSS dead-rule fixes across the 7 files (M6) + a governance test for the class-on-Fluent-component pattern; fix the undefined `--error*` tokens and extend the legacy-token regex.
6. Lock `FrontComposerRegistry` (M2); fix ETag seeding race (M4); bound `ProjectionSubscriptionService` disposal (M3); align the two polling drivers' disposal.
7. Testing harness failure modes: rejection/timeout/stall on `TestCommandService`, per-request query/paging callbacks, real fault injection or rename (M21) — the key unblock for Tenants adoption.
8. Replace `eval` in `LocalStorageService` with a JS module export (M5).
9. Extract duplication clusters 4, 5, 8, 9, 10 (scope resolver, snapshot publisher, fatal filter, hydration enum, FcJson).
10. LoggerMessage migration for Warning-and-above and hot paths (M15), enforced by analyzer; set `AnalysisMode Recommended` and burn down (M17); fix the dead CS1591 config for Contracts (M16).
11. Remove `BuildServiceProvider` from `AddFrontComposerMcp` (M10); hash MCP API keys (M12); marker-file-based repo-root resolution + secret parameters in AppHost (M13).

**Phase 3 — Larger architectural changes (releases, API-breaking, do before v1.0):**
1. **Split the Contracts kernel** (H11 + M24): net10/Blazor surface (`Typography`, `RenderFragment` contexts, keyboard-event members) into a `Contracts.UI` assembly; `InMemoryStorageService` → Testing; `InlinePopoverRegistry` implementation → Shell (interface stays); `FcShellOptions` → Shell/Options package; Fluxor action records (incl. the TCS-bearing `LoadPageAction`) → Shell. Result: Contracts becomes the genuine wire/DTO/attribute kernel that Tenants can reference without inheriting a Fluent RC pin.
2. **Decompose `QueryRequest`** into UI-facing query + transport/caching envelope; introduce `CommandResultStatus` constants (M25) — use the existing HFC deprecation pipeline.
3. **Declare and enforce Shell layering** (M18): Telemetry as cross-cutting; connection/polling workers into Infrastructure; route/label derivation off the nav component into Routing; then add an architecture test pinning the folder dependency directions.
4. **Converge change notification** on one idiom and drop System.Reactive (M19).
5. **Split the god files** (M14): `MigrationCommand.cs`, `SkillCorpus.cs` (move the LLM benchmark harness out of the runtime package), `DriftDetection.cs`, `InspectCommand.cs`, `SchemaFingerprintContracts.cs`, and the interface+impl+DTO bundles in Shell; document the Fluxor-action-group exception if kept.
6. **Shared generated-artifact naming contract** consumed by SourceTools and the CLI (M23); single ULID implementation beside the contract (M28).
7. Wire-shape pinning for the SignalR/EventStore boundary: `[JsonPropertyName]` or golden-JSON tests for the cross-repo DTOs (M11) + hub method-name pinning tests.

## 6. Missing tests and validation steps

1. `ReturnPathValidator` exhaustive theory (per documented attack class: protocol-relative, backslash variants, percent-encoded bypass, traversal, BiDi/zero-width, non-root base href).
2. Golden-JSON round-trip tests for `ProjectionChangedDetail`, `CommandResult`, `ProblemDetailsPayload` (cross-repo wire contract).
3. `SignalRProjectionHubConnectionFactory` unit tests + pinning test for the wire method-name literals (`ProjectionChanged`, `JoinGroupScoped`, …).
4. Storage-key equivalence FsCheck property (whitespace/colon/NFD-NFC/mixed-case-email inputs) across both builders.
5. Acronym slug pinning tests (`CommandRouteBuilder.KebabCase` vs nav `BuildRoute`).
6. Emitter escaping property test (control chars, ` / `) asserting generated source parses clean.
7. `FrontComposerMcpUlidFactory` monotonicity/clock-regression/overflow spec tests.
8. Nullable-numeric command fixture through the compile-the-output generator tests.
9. MCP lifecycle cross-scope hosting test (would have caught H4).
10. Testing-package surface tests for `Builders`/`Assertions`/fakes (currently 2 test files for 11 shipped source files).
11. `CommandFeedbackPublisher` subscribe/dispose/fault-isolation tests; `InlinePopoverRegistry` open/close race test.
12. Governance additions: every `wwwroot/css` file referenced; scoped-CSS class-on-Fluent-component detector; `--error*` in the legacy-token regex; assert release lane sets `EnableFrontComposerPackageValidation` (currently default-false and apparently never flipped).
13. E2E smoke for `FcStatusFilterChips` badge activation (the `@onclick`-on-Fluent-component risk class bUnit can't catch).

## 7. What is done well

- **Fail-closed security engineering**: tenancy resolution with distinct diagnostic categories and post-await revalidation; MCP gates with startup probes, no existence oracles, bounded everything, constant-time key comparison; open-redirect and CSS-injection guards; sanitized logging with hashed identifiers.
- **DI lifetime discipline**: every registration carries a rationale; runtime lifetime guards (`InlinePopoverRegistry` throws on wrong lifetime; `LifecycleStateService` detects singleton mis-registration).
- **Async correctness**: zero sync-over-async, CA2007 compiler-enforced, all fire-and-forget supervised, cancellation threaded end-to-end in the tooling.
- **Textbook incremental generator**: pure-data IR, `EquatableArray`, `ForAttributeWithMetadataName`, cache-hit tracking tests, byte-stability and culture-invariance regressions, integration tests that compile the generated output.
- **Rare packaging rigor**: PublicAPI baselines with reflection enforcement, pack-and-consume-from-nupkg clean-consumer tests, append-only wire contracts guarded by compat tests, `[Obsolete]` with `DiagnosticId`/`UrlFormat` and versioned removal plans.
- **Governance-as-tests**: Fluent v5 conformance (raw-element denylist, legacy-token regex with shrink-only backlog, package pins, emitter string literals), CI-workflow structure pinning, provider-package deny lists, hostile-output redaction fixtures, empty quarantine state.
- **Decision traceability**: story/decision IDs on nearly every non-obvious choice, making intent auditable years later.
- **Accessibility depth**: landmark discipline, aria-live choreography with anti-regression comments, focus management that fails loudly, reduced-motion support, localized aria templates.

The overarching pattern across the High findings: the defects cluster precisely in the blind spots of the existing (excellent) test machinery — silent Blazor parameter splats, CSS that compiles but never applies, codegen paths without a fixture, DI lifetimes that only misbehave across requests. The highest-leverage investment is extending the team's own governance-test approach to those four classes.

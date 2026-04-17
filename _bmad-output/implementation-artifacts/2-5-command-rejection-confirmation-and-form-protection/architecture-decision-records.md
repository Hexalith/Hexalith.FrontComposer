# Architecture Decision Records

## ADR-024: Destructive Confirmation Is Renderer-Level, Pre-Submit — Lifecycle Wrapper Remains Post-Submit Only

- **Status:** Accepted
- **Context:** Destructive-action confirmation dialog (epics §1042-1048, UX-DR37, UX-DR58) must intercept submit BEFORE the command dispatches. Three plausible ownership splits:
  1. **Wrapper-owned** — `FcLifecycleWrapper` intercepts its own `ChildContent`'s submit event.
  2. **Renderer-owned** — `CommandRendererEmitter` gates the submit trigger (the button's `OnClick` / `_externalSubmit` invocation) behind dialog confirmation.
  3. **Form-owned** — `CommandFormEmitter` inserts a dialog before `OnValidSubmitAsync`.
- **Decision:** Take option 2 (renderer-owned). `CommandRendererEmitter` emits the destructive-dialog open call, gates `_externalSubmit` on the dialog's `OnConfirm`, and leaves the form + wrapper untouched.
- **Consequences:** (+) `FcLifecycleWrapper`'s append-only parameter surface (2-4 D1) is preserved — no new intercept-hooks, no pre-submit logic. (+) Renderer is already the owner of submit-trigger chrome (button, popover, breadcrumb — ADR-016); extending it to "gate submit behind a dialog" is a natural expansion of that ownership. **Decisive argument (post-elicitation Red Team Attack-1 reinforcement):** the renderer/form split ADR-016 paid real cost to establish is the load-bearing reason — wrapper-owned or form-owned would collapse it. (+) Non-destructive commands emit zero confirmation plumbing — flat runtime cost for the 99 %. (-) Adds a renderer-level code path gated on `model.IsDestructive`; snapshot tests gain 3 new approved baselines (Task 8.6). (-) Inline (0-field) destructive commands CANNOT render — blocked at parse time via HFC1021 (D19). (-) **Future coupling**: adopters who want a typed data-preview slot in the dialog (G7) will migrate the dialog to a lean `Hexalith.FrontComposer.Components` sibling package in Story 9-x (G12 from Story 2-4 applies here too — v0.1 Shell placement is fine, entrenches "domain → Shell" reverse-reference one component deeper). **XSS inheritance constraint**: when Story 6-3 (Level 3 slot replacement) lands the customization-gradient path for the dialog body, 6-3 MUST inherit D14's plain-text invariant — any adopter-provided Razor fragment that reaches `ConfirmationBody` requires a sanitizer (e.g., HtmlSanitizer) as an explicit 6-3 opt-in contract. Surfacing this here so future-you doesn't open the XSS vector the moment rich formatting is legalized.
- **Rejected alternatives:**
  - **Wrapper-owned (option 1)** — would require `FcLifecycleWrapper` to introspect submit intent (new intercept param + dialog callback). Breaks 2-4 D1 append-only contract (we'd need a NON-optional new parameter to distinguish destructive vs non-destructive).
  - **Form-owned (option 3)** — `CommandFormEmitter` already knows `NonDerivableProperties` but not destructive intent (it receives a `CommandFormModel`, not a `CommandRendererModel`). Widening the form model to carry destructive metadata couples two emitters that Story 2-2 ADR-016 deliberately decoupled.
  - **Shell-level middleware (cascade)** — service-locator hack, same anti-pattern Story 2-4 ADR-020 rejected for wrapper placement.

## ADR-025: `NavigationLock` + `LocationChanging` For Form Abandonment — NOT `IJSRuntime.beforeunload`

- **Status:** Accepted
- **Context:** Full-page form abandonment protection (epics §1050-1055, UX-DR38) can be implemented at two layers:
  1. **Browser-native `beforeunload`** via `IJSRuntime.InvokeAsync("eval", "window.onbeforeunload = ...")`.
  2. **Blazor 8+ `NavigationLock` + `LocationChanging` handler** — built-in component + event from `Microsoft.AspNetCore.Components.Routing`.
- **Decision:** Take option 2. `FcFormAbandonmentGuard` renders a `<NavigationLock OnBeforeInternalNavigation="OnNavigationChanging" />` and handles the `LocationChangingContext` to show the warning bar and call `ctx.PreventNavigation()` when the threshold has elapsed. On "Leave anyway" the guard sets `_isLeaving = true` and re-invokes `NavigationManager.NavigateTo(_pendingTarget)` which, because the lock is no longer active (or it checks the flag), completes the navigation.
- **Consequences:** (+) SSR-safe, no JS interop, no CSP/script-src policy wrinkle. (+) Works identically on Blazor Server, WebAssembly, Auto. (+) Copy is framework-controlled (UX-DR38 + UX-DR57 zero-override invariants satisfied). (+) Testable with bUnit's `TestContext.Services.AddSingleton<NavigationManager>` and simulated `LocationChangingContext`. (-) Only intercepts INTERNAL navigation (SPA nav). External `window.close` / browser-back IS NOT blocked (browser owns that). Acceptable: epics §1051 scopes "breadcrumb, sidebar, command palette" — all internal. Browser-close is out of framework control. (-) Requires `FcFormAbandonmentGuard` to coordinate with `IDisposable` on Blazor circuit teardown to release the lock.
- **Rejected alternatives:**
  - **beforeunload (option 1)** — browser-native dialog is non-customizable ("Leave site? Changes you made may not be saved"), violates UX-DR38 (must show framework-controlled copy with "Stay on form" / "Leave anyway"). Also requires JS interop which is not available during pre-render (SSR phase).
  - **Custom JS + CSP-safe inline handler** — moves copy-control to JS, loses localization parity with other framework UI, and introduces a JS module surface that Epic 6 customization gradient would have to overlay.

## ADR-026: Destructive Is Opt-In Via `[Destructive]` Attribute — NOT Name Heuristic

- **Status:** Accepted
- **Context:** epics §1042 says commands are "annotated OR identified as destructive (Delete, Remove, Purge)". Two interpretation axes:
  1. **Name heuristic** — `DeleteOrderCommand` is destructive because its name starts with `Delete`.
  2. **Attribute opt-in** — only `[Destructive]`-annotated commands trigger confirmation, regardless of name.
  3. **Hybrid** — name heuristic flags + opt-in required via attribute; analyzer warning bridges them.
- **Decision:** Take option 3 (hybrid). `[Destructive]` attribute is the ONLY runtime-honored classification signal. Name heuristic fires an analyzer warning (`HFC1020`) at build time on `Delete*`/`Remove*`/`Purge*` commands missing the attribute — a guardrail, not a gate.
- **Consequences:** (+) Explicit adopter intent (no false-positive `RemoveOutlierCommand` false-confirmation noise). (+) Guardrail catches the common miss (developer adds `DeletePartyCommand` and forgets the attribute). (+) Under `TreatWarningsAsErrors` (Story 1-7), the warning IS effectively blocking — adopter must consciously annotate or rename. (-) Two-step authoring (attribute + class name) for the happy path. Mitigation: `[Destructive]` is a one-liner above the record declaration. (-) Analyzer consumes 2 diagnostic IDs (HFC1020 Warning + HFC1021 Error).
- **Rejected alternatives:**
  - **Name-only (option 1)** — false-positive risk is non-trivial (`RemoveFilterCommand` is non-destructive). Adding a name-heuristic escape hatch (`[NotDestructive]` attribute) inverts the opt-in/opt-out semantics and reads badly.
  - **Attribute-only, no heuristic (option 2)** — common dev mistake (forgetting the attribute on a clearly-destructive command) ships as a production bug with no compile-time signal. Unacceptable for a framework that invests heavily in build-time analyzers (Story 9 is the Diagnostic ID System).
  - **Runtime command-name inspection** — reflection in hot path, AOT-hostile, L05 violation.

---

# FC-CUST Override Accessibility Diagnostics Contract

Date: 2026-06-05
Status: confirmed-build-and-dev-runtime
Scope: FC-CUST Level 2/3/4 override accessibility diagnostics and development-only contract-mismatch display.
Owner: FrontComposer Epic 6, Story 6.4.

## Diagnostic Mapping

| Diagnostic | FC-A11Y primitive | WCAG / accessibility intent | Current disposition |
|---|---|---|---|
| HFC1050 | Accessible name | WCAG 4.1.2 Name, Role, Value | Build-time SourceTools warning for statically inspectable custom override components with `onclick` but no visible/static name evidence. |
| HFC1051 | Keyboard reachability | WCAG 2.1.1 Keyboard | Build-time SourceTools warning for statically inspectable custom override components that set `tabindex=-1`. |
| HFC1052 | Focus visibility | WCAG 2.4.7 Focus Visible | Build-time SourceTools warning for statically inspectable custom override source that suppresses `outline` or `box-shadow` without `focus-visible` replacement evidence. |
| HFC1053 | Status/live-region parity | WCAG 4.1.3 Status Messages | Build-time SourceTools warning for lifecycle/status override source with `data-fc-lifecycle` or `data-fc-status` but no `aria-live`. |
| HFC1054 | Reduced motion | WCAG 2.3.3 Animation from Interactions | Build-time SourceTools warning for transition/animation/keyframes evidence without `prefers-reduced-motion`. |
| HFC1055 | Forced-colors / high contrast | Forced-colors/high-contrast fallback | Build-time SourceTools warning for custom color/border/fill evidence without `forced-colors`. |

All six descriptors are `Warning`, enabled by default, and use the canonical help-link shape `https://hexalith.github.io/FrontComposer/diagnostics/HFC105x`. Build-breaking behavior comes from repository/adopter `TreatWarningsAsErrors=true`, not from Error severity.

## Analyzer Scope

Status: confirmed and pinned.

- Level 2 components are discovered by `[ProjectionTemplate]`.
- Registration-discovered customization components are analyzed when they are the last type argument of `AddProjectionTemplate`, `AddSlotOverride`, or `AddViewOverride`.
- Analyzer messages use the teaching shape `What / Expected / Got / Fix / Fallback / DocsLink`.
- Analysis remains conservative: generated code is excluded, concurrency stays enabled, source scanning is bounded to 256 KiB per type, and comments are stripped before substring checks.
- The analyzer does not use `CompilationProvider`, generated output paths, runtime registries, tenant/user state, localized strings, or a broad DOM/CSS parser.
- Negative controls are pinned for non-custom components, accessible named controls, commented-out violations, and supported reduced-motion/forced-colors fallback evidence.

Known static-analysis limits:

- Only statically inspectable generated or handwritten C# source in `DeclaringSyntaxReferences` is scanned.
- Companion `.razor.css` files are not inspected unless their relevant CSS evidence is surfaced in source text.
- CSS and render-tree detection is substring-based and intentionally conservative.
- The bounded scan can truncate very large partial classes.
- String literal contents are preserved while C# comments are stripped because render-tree attribute names live inside string literals.

Catalog/front-matter drift:

- `FcDiagnosticIds`, `DiagnosticDescriptors`, `AnalyzerReleases.Unshipped.md`, `docs/diagnostics/diagnostic-registry.json`, and `docs/diagnostics/HFC1050.md` through `HFC1055.md` agree on IDs, warning severity, help links, and SourceTools ownership.
- Published docs and registry entries still use generic stub wording such as "Follow the FrontComposer diagnostic contract" instead of the richer live analyzer teaching sections. This story records that wording drift rather than rewriting CI-gated docs.
- `AnalyzerReleases.Unshipped.md` still labels these entries with historical Story 6-6. Source comments also contain historical labels; this artifact maps the behavior to current Story 6.4 without cosmetic churn.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs`
- Source: `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- Source: `docs/diagnostics/diagnostic-registry.json`
- Source: `docs/diagnostics/HFC1050.md` through `docs/diagnostics/HFC1055.md`
- Test: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`

## Contract-Mismatch Panel

Status: confirmed development-only runtime.

- Registry rejection records stay in `ICustomizationContractRejectionLog`.
- `CustomizationContractMismatchDiagnosticProvider` converts those sanitized rejection records into `CustomizationDiagnostic` instances.
- `FcCustomizationDiagnosticPanel` remains the only panel UI used for these diagnostics.
- Panel diagnostics include projection, component, role, field when applicable, expected/actual contract versions, decision, fix guidance, generated fallback, and an HTTPS diagnostic docs link.
- The provider is registered only through `AddFrontComposerDevMode` when both gates are true: `#if DEBUG` and `IHostEnvironment.IsDevelopment()`.
- `FrontComposerShell` renders mismatch diagnostics only when `IsDevModeBuild` is true and the optional provider exists.
- Production/Staging and Release builds do not register or render the mismatch panel path.
- Existing registry semantics are unchanged: `LogAndSkip` remains log-and-skip, and `FailClosedOnMajorMismatch` remains startup fail-closed through `CustomizationContractValidationGate`.

Redaction requirements:

- Diagnostics must not include tenant/user IDs, item payloads, field values, exception objects, render fragments, scoped services, access tokens, bearer tokens, or localized runtime strings.
- Docs links render as anchors only for absolute HTTPS URLs; unsafe or malformed links render as text in the existing panel.
- Contract mismatch records already carry type/role/field/version metadata only; the provider adds only version and decision properties.

Evidence:

- Source: `src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractRejection.cs`
- Source: `src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs`
- Source: `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/CustomizationContractMismatchDiagnosticProvider.cs`
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`
- Source: `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- Source: `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor`
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/Diagnostics/CustomizationContractMismatchDiagnosticProviderTests.cs`
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs`
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`

## Non-Goals

- No new analyzer package, DOM parser, CSS parser, browser-only enforcement layer, or second diagnostic panel UI.
- No changes to FC-CUST render precedence, registry resolution semantics, or contract-version comparison directionality.
- No changes to HFC1033-HFC1046 behavior except boundary documentation in this contract.
- No changes to `CanonicalSchemaMaterial`, schema fingerprints, generated-output paths, MCP URI/security behavior, EventStore boundaries, package versions, pacts, `.verified.txt`, or public API baselines.
- No published `docs/` rewrite in this story.

## Open Items

| Owner | Reason | Risk | Follow-up |
|---|---|---|---|
| Story 9-4/9-5 diagnostic catalog owner, with Story 6.4 accessibility owner | HFC1050-HFC1055 docs/registry wording is generic relative to live analyzer teaching text. | Adopters may see less specific published guidance than the emitted warning text. | Reconcile published docs and registry message templates if/when the diagnostic catalog refresh story owns `docs/` changes. |
| Story 9-4/9-5 diagnostic catalog owner | Historical Story 6-6 labels remain in analyzer release/source comments. | Future agents can confuse story numbering, though behavior is now pinned. | Rename labels only in a story that owns diagnostic catalog/comment cleanup. |

## Changed-File Reconciliation

Story-owned changed files at completion:

- `_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/6-4-override-accessibility-safety-diagnostics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/CustomizationContractMismatchDiagnosticProvider.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/ICustomizationContractMismatchDiagnosticProvider.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Diagnostics/CustomizationContractMismatchDiagnosticProviderTests.cs`

Pre-existing unrelated workspace change observed and not owned by this story:

- `_bmad-output/story-automator/orchestration-1-20260604-140358.md`

Validation summary:

- Release build of `tests/Hexalith.FrontComposer.SourceTools.Tests` passed 0/0.
- SourceTools focused in-process analyzer lane passed 12/12.
- Release build of `tests/Hexalith.FrontComposer.Shell.Tests` passed 0/0.
- Release focused Shell in-process lane passed 3/3.
- Debug build of `tests/Hexalith.FrontComposer.Shell.Tests` passed 0/0.
- Debug focused Shell in-process lane passed 4/4.
- Focused `dotnet test` / VSTest was attempted and aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied`; CI remains authoritative for the solution-level VSTest lane.

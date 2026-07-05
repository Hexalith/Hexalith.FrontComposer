---
title: 'FC-L10N — Shell-string ownership contract'
date: '2026-06-03'
story: '1.4'
status: 'confirmed'   # two-localizer split, density-preview sample scope, and domain-label ownership are confirmed
owner: 'FrontComposer + Tenants author'
supersedes: ''
---

# FC-L10N — Shell-string ownership contract

> **Decision deliverable for Story 1.4.** Unlike a build-from-scratch story, **the localization
> machinery named below already ships and is already tested** in the shell at baseline `df37313`:
> `FcShellResources.resx` (EN default) + `FcShellResources.fr.resx` (French satellite) at **157
> keys each, full parity**; every shell-chrome string resolves through
> `IStringLocalizer<FcShellResources>`; `AddHexalithShellLocalization(...)` advertises the shell's
> cultures into the adopter's request-localization pipeline; `FcShellOptions.DefaultCulture` /
> `SupportedCultures` (`["en","fr"]`, BCP-47-lite, must-include-default) are the culture surface;
> and `FcShellResourcesTests` pins EN↔FR parity, culture round-trip, per-key values, NBSP-before-colon
> byte guards, and placeholder-count parity. This note therefore **(1) names the shell-vs-host/domain
> string-ownership boundary** as the single FC-L10N contract every later story points at, **(2) records
> the resolution mechanism** (resx BaseName convention + `IStringLocalizer<FcShellResources>` + the
> `services.Replace` swap seam) and the supported-culture surface, and **(3) confirms the two prior
> boundary questions** so no unowned FC-L10N wording remains. Adopting FC-L10N introduces **zero behaviour
> change**: the contract confirms and pins what the shell already does; it re-implements nothing.

## The contract

The FC-L10N ownership boundary is a **two-localizer split, and naming that split *is* the contract.**
Shell chrome resolves through the shell's own embedded resource; domain/host text resolves through
the host's per-type localizer; application/tenant text stays entirely host-owned. The split is made
concrete in code by the generated command form, which injects **both** localizers side by side
(`CommandFormEmitter.cs:98,105`) — the shell's `IStringLocalizer<FcShellResources>` for the
framework-owned authorization warning, and the host's `IStringLocalizer<TCommand>` for the
domain-owned field labels.

| String category | Owner | Source | Resolves via |
|---|---|---|---|
| **Shell chrome** — nav, settings, status, palette, lifecycle, filters, badges, authorization warnings, density, shortcuts, footer, skip-links | **FrontComposer Shell** | `FcShellResources.resx` (+ `.fr.resx`), 157 keys at parity | `IStringLocalizer<FcShellResources>` |
| **Dev-mode overlay strings** (DEBUG / specimen-authoring aid) | **FrontComposer Shell** (dev-only) | `Resources/DevMode/DevModeStrings.resx` (+ `.fr.resx`) | `IStringLocalizer<DevModeStrings>` — a **separate** resource, not user-facing chrome |
| **Domain field / label text** — projection column titles, command-form field labels | **Host / domain** | `[Display(Name=…)]` on the host's annotated types **or** the host's own `IStringLocalizer<T>` (`T` = the command/projection type) | runtime `IStringLocalizer<TCommand>`; `[Display(Name)]` wins, else falls through to the per-type localizer (`FormFieldModel.cs:39,62`, `CommandFormEmitter.cs:98`) |
| **Application / tenant text** — app-name override, host nav items, host pages | **Host / Tenants layer** | host-owned resources | the host's own localization registration |

## The mechanism (AC1)

- **Resx BaseName convention.** The `FcShellResources` marker class lives in
  `Hexalith.FrontComposer.Shell.Resources`; ASP.NET Core resolves satellites by
  `typeof(FcShellResources).FullName` as the resx BaseName, so the embedded
  `Resources/FcShellResources.resx` / `.fr.resx` match by namespace + class name with **no**
  `ResourcesPath` override (`FcShellResources.cs:9-13`).
- **`AddHexalithShellLocalization(services, configure?)`** registers `ShellRequestLocalizationOptionsSetup`
  — a `TryAddEnumerable` singleton `IConfigureOptions<RequestLocalizationOptions>` that reads
  `FcShellOptions` and sets `DefaultRequestCulture` / `SupportedCultures` / `SupportedUICultures`
  (`ServiceCollectionExtensions.cs:452-465,645-662`). It **deliberately does not** call
  `AddLocalization()` — the adopter keeps authoritative control over `LocalizationOptions`. The
  granular three-call path (`AddLocalization().AddHexalithShellLocalization().AddHexalithFrontComposer()`)
  is the primary API; `AddHexalithFrontComposerQuickstart()` is the one-line sugar that chains
  `AddLocalization()` + `AddHexalithShellLocalization()` for first-time adopters
  (`ServiceCollectionExtensions.cs:491-511`).
- **The swap seam.** A whitelabel / DB-backed source is the supported extensibility path:
  `services.Replace(ServiceDescriptor.Scoped(typeof(IStringLocalizer<FcShellResources>), …))`
  swaps the shell's localizer without touching the resx convention (documented at
  `ServiceCollectionExtensions.cs:432-438`).

### Supported-culture surface

- `FcShellOptions.DefaultCulture` — default `"en"`, BCP-47-lite validated (`^[a-z]{2}(-[A-Z]{2})?$`).
- `FcShellOptions.SupportedCultures` — default `["en","fr"]`, with the cross-property invariant that
  it **must include** `DefaultCulture` (enforced by `FcShellOptionsValidationTests`).
- **Extending coverage** is a two-step, non-code change for the shell: add the culture to
  `SupportedCultures` **and** ship the matching `FcShellResources.<culture>.resx` satellite (or
  swap in a custom localizer). No new public API is required.
- **RTL forward-compatibility.** FC-LYT (1.2) and FC-A11Y (1.3) already use logical CSS properties
  (`max-inline-size`, `margin-inline`, `inset-inline`), so adding an RTL culture (e.g. `ar`) is a
  resx-satellite + `SupportedCultures` addition; the layout/a11y CSS is already logical-property-clean
  for it. No RTL satellite ships in this story.

## The "chrome" scope boundary (confirmed 2026-07-05)

The `FcDensityPreviewPanel` density preview uses **illustrative sample-data column titles**
(`"Order"` / `"Customer"` / `"Status"`, hard-coded at `FcDensityPreviewPanel.razor:26-28`). These are
demo/sample data — the Lorem-ipsum of a density preview — not framework chrome.

**Confirmed: out of scope.** Sample-data strings inside the settings density preview are
intentionally *not* localized, the same way Lorem-ipsum body text in a layout demo is not localized;
they exist only to show row spacing at each density. They are **not** user-facing application chrome
and resolve through no localizer by design.

## Confirmation

**Status: CONFIRMED (2026-07-05).** The two-localizer split, resolution mechanism, supported-culture
surface, and prior boundary questions are confirmed:

1. Density-preview sample strings (`"Order"` / `"Customer"` / `"Status"`) are out of scope for
   FC-L10N because they are illustrative sample data inside a visual density preview, not shell
   chrome or adopter-facing application text.
2. Domain labels are host-owned through `[Display(Name)]` or the host's own `IStringLocalizer<T>`.
   The shell intentionally does not ship a fallback localizer for domain types.

The sprint action **"Drive residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions to
confirmed or dated owned follow-up"** is closed for FC-L10N by this disposition. No dated follow-up is
required.

## FC-DOC linkage (deferred to Story 1.5)

The cross-link from the **published component docs** to this contract is owned by **Story 1.5
(FC-DOC)**, which owns the CI-gated `docs/` DocFX site. This story does **not** scratch-write
`docs/`. For 1.5 to link it, this contract lives at:

```
_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md
```

Story 1.5 should add the published-docs cross-reference from the `FrontComposerShell` /
localization section (and from any adopter "customize the shell strings" guide) pointing here, so the
shell-vs-host string-ownership boundary and the `services.Replace` swap seam are discoverable from the
component docs.

## Surface confirmed / pinned by Story 1.4

- **No new `src/` surface.** The resx files, the `IStringLocalizer<FcShellResources>` wiring,
  `AddHexalithShellLocalization`, the `FcShellOptions` culture surface, and the parity/round-trip
  tests all pre-exist and are unchanged.
- **New pin:** `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs`
  — a consolidated shell-frame **culture-render** ready-gate: under `CurrentUICulture=fr` the rendered
  chrome accessible-names resolve to their **FR** resx values and are **not** the EN literals (the
  "no hard-coded English chrome" guard), distinct from 1.3's accessible-name-*presence* gate.
- **Referenced, not modified:** `FcShellResources.resx` / `.fr.resx`, `FcShellResources` BaseName
  identity, `AddHexalithShellLocalization` / `ShellRequestLocalizationOptionsSetup` / `FcShellOptions`,
  the locked 7-parameter shell surface, and the standing `FcShellResourcesTests` /
  `FcShellOptionsValidationTests` evidence.

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4: Establish FC-L10N shell-string ownership] (story + 3 ACs; AR3, FR9/FR10/FR15)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-L10N ask + owner: FrontComposer + Tenants author)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT contract — structure/tone mirrored; escalate-with-owner precedent)
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] (FC-A11Y contract — most recent same-shape precedent)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.cs:9-13] (marker class + resx BaseName convention)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx + FcShellResources.fr.resx] (157 EN + 157 FR keys, at parity)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:432-465] (`AddHexalithShellLocalization`, deliberate no-`AddLocalization`, `services.Replace` swap seam)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:491-511] (`AddHexalithFrontComposerQuickstart` chains `AddLocalization` + shell localization)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:645-662] (`ShellRequestLocalizationOptionsSetup` → DefaultRequestCulture/SupportedCultures from `FcShellOptions`)
- [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs:134-150] (`DefaultCulture` `"en"`, `SupportedCultures` `["en","fr"]`, BCP-47-lite + must-include-default invariant)
- [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/FormFieldModel.cs:39,62] (`[Display(Name)]` wins, else runtime per-type `IStringLocalizer` — the host/domain side of the boundary)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:98,105] (generated form injects `IStringLocalizer<TCommand>` for domain labels + `IStringLocalizer<FcShellResources>` for shell-owned auth warnings — the boundary in code)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx] (separate dev-only shell resource, consumed by `FcDevModeOverlay.razor:10`)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor:26-28] (illustrative sample-data column titles — the open scope question for AC3)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs:18-51,365-371] (parity + culture round-trip + `BuildLocalizedProvider` pattern; standing evidence)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs] (culture-config invariant evidence)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs] (most recent same-shape story; localizer-assertion + render-and-query style)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules + #Testing Rules] (localization wiring order, IStringLocalizer, test discipline, DiffEngine_Disabled, solution-level test + trait filter)

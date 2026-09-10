# FrontComposer / Fluent UI Blazor V5 Conformance Review — frontcomposer

## Overall verdict

**Adequate, with one high-impact API correction required.** The UX contract consistently inherits FrontComposer and Fluent UI Blazor V5, bans raw interactive controls and theme redefinition, uses Fluent 2 roles, and adopts the repository's accordion rule. Two component names in the visual supplement do not exist in the pinned Fluent UI package, so the current handoff can send downstream implementers toward APIs they cannot call.

## Evidence basis

- Repository UX baseline: `references/Hexalith.AI.Tools/hexalith-ux-instructions.md`.
- Pinned package: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` (`references/Hexalith.Builds/Props/Directory.Packages.props:226`).
- Package surface checked with `dotnet-inspect`: `FluentPopover`, `FluentMenuButton`, `FluentGrid`, `FluentTabs`, `FluentTooltip`, `FluentNav`, `FluentDataGrid`, `FluentTextInput`, `FluentAccordion`, `FluentAccordionItem`, `FluentLayout`, and `FluentStack` resolve; `FluentToolbar` and `FluentSearch` do not.

## Strengths

- The contract explicitly requires FrontComposer/Fluent UI Blazor V5 and forbids raw interactive controls and custom theme recreation (`ux-design.md:88-95`; `ux-design-detailed-2026-07-05.md:78-94,142-151`).
- Typography, colors, radii, and layout generally inherit from Fluent component parameters or Fluent 2 roles rather than duplicating a theme (`ux-design-detailed-2026-07-05.md:19-50,96-130`).
- The multi-section rule is correctly behavioral and deterministic in EXPERIENCE: one `FluentAccordion`, primary section expanded, only-primary-content not hidden (`ux-experience-2026-07-05.md:78`).
- Fluent enum/API names such as `Color.Lightweight`, `TextSize.Size200/500/700`, and the other resolved components match the pinned package surface.

## Findings

- **[high] The visual contract names two unavailable Fluent components.** The `page-toolbar` token/body says `FcPageToolbar` uses `FluentToolbar` and `FluentSearch`, but neither type exists in the pinned V5 package (`ux-design-detailed-2026-07-05.md:63-64,137`). The repository's actual composition uses `FluentStack role="toolbar"` plus `FluentTextInput TextInputType.Search` (`src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor:4-27`). *Fix:* replace the nonexistent API names with the exact FrontComposer composition contract, or name only `FcPageToolbar` and explicitly make its internal Fluent composition implementation-owned.
- **[medium] The navigation token uses a vague “FluentButton / FluentNav defaults” mapping.** The canonical contract names `FrontComposerNavigation`, while the token neither identifies the exact FrontComposer component nor states which Fluent components own labeled, icon-only, flyout, current-item, and badge rendering (`ux-design.md:45-47`; `ux-design-detailed-2026-07-05.md:56-59,134-136`). *Fix:* make `FrontComposerNavigation` the contract component and list only its supported Fluent primitives/roles where consumers must know them.
- **[medium] The accent alias is not tied to one exact Fluent V5 role.** `accent-thread` is described as “Fluent theme accent / --fc-color-accent,” which can be read as either inheritance or a custom theme primitive (`ux-design-detailed-2026-07-05.md:27,55,82,91`). *Fix:* document `--fc-color-accent` as an implementation alias of the active Fluent V5 accent role, including ownership and fallback, and explicitly prohibit setting an independent seed/palette through it.
- **[medium] The DESIGN component table is too sparse to enforce reuse-first behavior.** Several canonical FrontComposer components are absent, leaving downstream teams to choose raw markup or Fluent primitives independently despite the repository's reuse rule (`ux-design.md:52-62`; `ux-design-detailed-2026-07-05.md:51-71`). *Fix:* add every reusable FrontComposer component to the visual registry and state “use this component; inherited visual defaults” where no custom tokens are needed.

## Summary

- Critical: 0
- High: 1
- Medium: 3
- Low: 0

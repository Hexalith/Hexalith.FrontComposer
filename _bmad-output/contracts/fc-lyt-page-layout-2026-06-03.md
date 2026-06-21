---
title: 'FC-LYT — Page-layout contract'
date: '2026-06-03'
story: '1.2'
status: 'confirmed'   # FullWidth default + 75rem max-measure signed off 2026-06-21 (sprint-change-proposal-2026-06-21)
owner: 'FrontComposer + Product/UX (Administrator, 2026-06-21)'
supersedes: ''
---

# FC-LYT — Page-layout contract

> **Decision deliverable for Story 1.2.** Before this story there was **no `<PageLayout>`
> component, no layout-mode enum, and no max-measure wrapper** in the repo. `<PageLayout>` lived
> only in the planning docs as the *name of a contract to confirm*. This note **(1) confirms the
> two-mode contract**, **(2) records the default + opt-in mechanism actually shipped**, and
> **(3) escalates the two open Product/UX inputs** (default choice and the exact max-measure
> value) with a named owner per AC2. The mechanism is shipped behind these decisions and is
> regression-safe: with no opt-in, every existing page renders exactly as it did at baseline
> `f40dece`.

## The contract

FrontComposer pages render at one of **two measures**, selected per page:

| Mode | Behaviour | When to use |
|---|---|---|
| **`FullWidth`** *(default)* | Content spans the full content area edge-to-edge — **exactly today's behaviour**. The shell's `#fc-main-content` keeps its `Padding.All3` and no width constraint. | DataGrid-dense, read-only projection pages (the Epic 2 MVP); anything that benefits from horizontal room. |
| **`Constrained`** *(opt-in)* | Content is capped at a readable max measure and centred within the content area: `max-inline-size: var(--fc-page-max-inline-size, 75rem); margin-inline: auto;`. | Prose, forms, and detail pages where an unbounded line length hurts readability. |

### Default

**`FullWidth` is the default.** Rationale:

- It is the **zero / first value** of `FcPageLayoutMode`, so a page that declares nothing gets it.
- It is **exactly the pre-`f40dece` behaviour**, so adopting FC-LYT introduces **zero regression**
  for any existing page.
- It is the right default for the **DataGrid-heavy read-only MVP** (Epic 2), where horizontal room
  is valuable.
- The AC wording — *"when a page **declares** constrained … full-width pages span the content
  area"* — treats *constrained* as the explicit opt-in action, consistent with FullWidth-as-default.

### Opt-in mechanism

A page declares **constrained** measure by dropping the **`<FcPageLayout>`** component into its
content:

```razor
<FcPageLayout Mode="FcPageLayoutMode.Constrained">
    @* prose / form / detail content here *@
</FcPageLayout>
```

`<FcPageLayout>` reads a **cascaded `FcPageLayoutCoordinator`** (an instance-per-shell value the
`FrontComposerShell` cascades with `IsFixed="true"`), sets the active mode on first render, and
resets it to `FullWidth` on dispose. The shell's `#fc-main-content` div binds
`data-fc-page-layout="full-width|constrained"` and toggles a `fc-page-layout--constrained` class
off the coordinator's current mode.

**Why a cascaded coordinator + child component, not a shell `[Parameter]`:** a page lives **inside**
`@ChildContent`, *below* the shell, and cannot set a shell parameter. The cascaded coordinator is the
repo's existing, proven answer to "a child wants to influence the shell's layout" — it mirrors
`LayoutHamburgerCoordinator` exactly (`FrontComposerShell.razor:35`, register-on-first-render /
clear-on-dispose like `FcHamburgerToggle`). Reusing it keeps the **locked 7-parameter surface
untouched** (`FrontComposerShellParameterSurfaceTests`), and honours ADR-030 scoped-lifetime /
single-writer (the coordinator is a field, never a DI singleton).

### Max-measure value

The constrained cap is a **themeable CSS custom property**, not a magic number:

- Custom property: **`--fc-page-max-inline-size`**
- Shipped default: **`75rem`** (≈ 1200px at the default root font size)
- Logical properties (`max-inline-size`, `margin-inline`) are used — **not** `max-width` /
  `margin-left/right` — for RTL-awareness consistent with FluentUI v5 and forward-compatible with
  FC-L10N (Story 1.4).

The exact default value (`75rem`) is a **Product/UX input** — see Confirmation below.

## Confirmation

**Status: CONFIRMED (Product/UX — Administrator, 2026-06-21).** The two open inputs are signed off via
the 2026-06-21 correct-course pass (`sprint-change-proposal-2026-06-21.md`):

1. **Default mode = `FullWidth`?** — ✅ **Confirmed.** Ships as the default (zero-regression; right for the DataGrid-heavy MVP).
2. **Constrained max-measure default = `75rem`?** — ✅ **Confirmed** as the `--fc-page-max-inline-size`
   default (themeable, so a future change remains a one-line CSS-variable override, not a code change).

Owner column per the readiness request (`frontcomposer-readiness-request-2026-06-03.md:22`, 🔴
FC-LYT row): **FrontComposer + Product/UX**. Resolution does not block Story 1.2 — the AC explicitly
permits escalate-with-owner.

## FC-DOC linkage (deferred to Story 1.5)

The cross-link from the **published component docs** to this contract is owned by **Story 1.5
(FC-DOC)**, which owns the CI-gated `docs/` DocFX site. This story does **not** scratch-write
`docs/`. For 1.5 to link it, this contract lives at:

```
_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md
```

Story 1.5 should add the published-docs cross-reference to the `FrontComposerShell` /
`FcPageLayout` component documentation pointing here.

## Surface shipped by Story 1.2

- `FcPageLayoutMode { FullWidth, Constrained }` — `Hexalith.FrontComposer.Contracts.Rendering`.
- `FcPageLayout` component — `Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor`.
- `FcPageLayoutCoordinator` (internal, instance-per-shell) — beside `LayoutHamburgerCoordinator`.
- `#fc-main-content[data-fc-page-layout]` + `.fc-page-layout--constrained` (scoped
  `FrontComposerShell.razor.css`) → `--fc-page-max-inline-size`.

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2] (story + ACs, FR9 / AR1 / UX-DR7)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:22] (🔴 FC-LYT ask + owners)
- [Source: _bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md] (FC-LYT body renders cleanly — confirmed by Story 1.0 spike)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:35,96-106] (cascade precedent + content area)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/LayoutHamburgerCoordinator.cs] (cascaded instance-per-shell pattern mirrored)
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-density.css] (data-attr + CSS-var precedent)

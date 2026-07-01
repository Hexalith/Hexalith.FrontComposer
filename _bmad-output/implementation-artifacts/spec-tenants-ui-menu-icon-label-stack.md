---
title: 'Tenants UI menu icon label stack'
type: 'bugfix'
created: '2026-07-01'
status: 'done'
route: 'one-shot'
---

# Tenants UI menu icon label stack

## Intent

**Problem:** The Tenants UI navigation rail inherited the FrontComposerShell menu tile layout, where the label could render beside the icon instead of below it. This did not match the Aspire dashboard menu pattern the shell is intended to follow.

**Approach:** Wrap each rail tile icon and label in an explicit vertical FluentStack so the visible text sits below the icon. Keep count/New badges outside that stack as an overlay row to avoid cramped rail tiles.

## Suggested Review Order

**Tile Layout**

- Manifest tiles now stack icon over label through a Fluent layout primitive.
  [`FrontComposerNavigation.razor:95`](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor#L95)

- Badge indicators stay separate from icon/label stacking to preserve tile density.
  [`FrontComposerNavigation.razor:109`](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor#L109)

- Orphan navigation contexts use the same icon-over-label structure.
  [`FrontComposerNavigation.razor:197`](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor#L197)

**Layout CSS**

- Tile CSS keeps layout-only constraints and avoids Fluent theme redefinition.
  [`FrontComposerNavigation.razor.css:21`](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.css#L21)

- Badge row positioning keeps indicators out of the vertical label stack.
  [`FrontComposerNavigation.razor.css:25`](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.css#L25)

**Tests**

- Existing desktop rail test now pins vertical stack rendering.
  [`FrontComposerNavigationTests.cs:65`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs#L65)

- Regression test proves badges are not stacked under the label.
  [`FrontComposerNavigationTests.cs:81`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs#L81)

---
title: "Page Tabs"
description: "Body-level Fluent tabs whose child components own their associated page-panel content."
genre: reference
audience: adopter
ownerStory: frontcomposer-tab-contract
status: published
reviewed: 2026-08-28
uid: frontcomposer.reference.components.pagetabs
slug: reference/components/page-tabs/
---

# Page Tabs

## Overview

`FcPageTabs` is the FrontComposer contract for switching complete page-body surfaces. Each
`FcPageTab` renders an actual Fluent UI v5 `FluentTab` and carries the content for that tab's panel.
Fluent therefore owns tab semantics, selection, roving focus, keyboard behavior, visibility, and the
tab-to-panel association.

Place page tabs after the page header, usually in a page wrapper's body or child-content slot. Do
not put grids, surface states, pagers, or command flows in `FcPageHeader.Actions`, and do not render
external sibling panels for toolbar-only tabs.

## Usage

```razor
<FcAggregateListPage TItem="OrderRow"
                     Heading="Orders">
    <Body>
        <FcPageTabs ActiveTabId="@activeTabId"
                    ActiveTabIdChanged="@OnActiveTabChangedAsync"
                    AriaLabel="Order sections"
                    TestId="orders-page-tabs">
            <FcPageTab Id="summary" Header="Summary" DeferredLoading="true">
                <OrderSummary />
            </FcPageTab>
            <FcPageTab Id="activity" Header="Activity" DeferredLoading="true">
                <OrderActivity />
            </FcPageTab>
        </FcPageTabs>
    </Body>
</FcAggregateListPage>

@code {
    private string activeTabId = "summary";

    private Task OnActiveTabChangedAsync(string? value)
    {
        activeTabId = value ?? "summary";
        return Task.CompletedTask;
    }
}
```

Every tab must have a stable, non-blank `Id`, a non-blank `Header`, and non-empty child content.
With `Id="summary"`, the pinned Fluent component derives `aria-controls="summary-panel"` and renders
the owned content in `id="summary-panel" role="tabpanel"`.

## Parameters / slots

### `FcPageTabs`

| Parameter | Type | Purpose |
|---|---|---|
| `ActiveTabId` / `ActiveTabIdChanged` | `string?` / `EventCallback<string?>` | Caller-owned active-tab state and selection callback. |
| `AriaLabel` | `string` | Accessible name for the page tab list. |
| `TestId` | `string` | Stable selector on the Fluent tabs root. Defaults to `fc-page-tabs`. |
| `Appearance` | `TabsAppearance?` | Fluent appearance. Defaults to `Subtle`. |
| `Disabled` | `bool` | Disables the complete tab set. |
| `Orientation` | `Orientation?` | Fluent orientation. Defaults to horizontal. |
| `Width` | `string?` | Width of the Fluent tabs root. Defaults to `100%`. |
| `ChildContent` | `RenderFragment?` | Caller-owned `FcPageTab` children. |

### `FcPageTab`

| Parameter | Type | Purpose |
|---|---|---|
| `Id` | `string` | Stable tab id and source for Fluent's derived `${Id}-panel` id. |
| `Header` | `string` | Visible label and accessible tab name. |
| `Disabled` | `bool` | Disables the tab; Fluent excludes it from keyboard selection. |
| `IconStart` | `Icon?` | Optional Fluent icon before the label. |
| `DeferredLoading` | `bool` | Defers panel content until Fluent first activates the tab. |
| `Tooltip` | `string?` | Optional Fluent tooltip for the tab header. |
| `ChildContent` | `RenderFragment?` | The real caller-owned panel content. |

## Layout (FC-LYT)

`FcPageTabs` fills the available width by default. It does not add a second page layout, page header,
or spacing wrapper; adopters place it in the body of `FcPageLayout`, `FcAggregateListPage`, or
`FcAggregateDetailPage` and use Fluent layout components inside each panel.

Use `DeferredLoading="true"` when a panel starts gateway work or renders an expensive surface. An
inactive panel must not issue requests merely because its tab header exists.

## Accessibility (FC-A11Y)

Fluent UI v5 owns the `tablist`, `tab`, and `tabpanel` roles, roving focus, ArrowLeft/ArrowRight,
Home/End, disabled-tab skipping, selection, and panel visibility. `FcPageTabs` forwards an accessible
tab-list name through `AriaLabel`; every `FcPageTab.Header` supplies that tab's accessible name.

Do not override `aria-controls`, panel ids, or `aria-labelledby` through `FluentTab` additional
attributes. The pinned Fluent component applies additional attributes to both its header and panel,
so overrides can create duplicate or contradictory associations. Keep content inside `FcPageTab` and
use the derived `${Id}-panel` contract.

Because tabs expose caller-owned panel content, custom surfaces placed inside tabs must preserve accessible names and keyboard reachability. The closest published checks are [HFC1050](../../diagnostics/HFC1050.md) for missing accessible names and [HFC1051](../../diagnostics/HFC1051.md) for blocked keyboard access.

## Localization (FC-L10N)

The components do not inject a localizer. Adopters pass localized `AriaLabel`, `Header`, and
`Tooltip` values from their resource owner. Keep each label a complete localized string.

## Related

- [Page Toolbar](page-toolbar.md) — header actions and the compatibility header-only tab API.
- [FrontComposerShell](front-composer-shell.md) — the route shell around page content.
- [Components](index.md) — the component reference index.

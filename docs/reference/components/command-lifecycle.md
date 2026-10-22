---
title: "Command lifecycle"
description: "Public command lifecycle contracts, states, accessibility and customization ownership."
genre: reference
audience: adopter
ownerStory: 15-2-i-doc-a-reconcile-component-and-page-pattern-documentation
status: published
reviewed: 2026-10-21
uid: frontcomposer.reference.components.command-lifecycle
slug: reference/components/command-lifecycle/
---

# Command lifecycle

## Overview

`FcLifecycleWrapper` observes one command correlation. Generated forms, pending summaries, blocked outcomes, validation and abandonment guards compose around it. Transport acknowledgement never implies confirmation: only an authoritative projection/status observation establishes a confirmed terminal result.

## Usage

```razor
<FcLifecycleWrapper CorrelationId="@operationCorrelation" DisplayLabel="Save order">
    @CommandForm
</FcLifecycleWrapper>
```

Use a deterministic synthetic correlation in tests. Production correlation is owned by the command pipeline. Keep one status owner when a mapped validation summary takes focused error ownership.

## Parameters / slots

The tables describe the public component parameters. Injected services and internal Fluent composition are implementation details.

### `FcLifecycleWrapper`

| Parameter | Type | Contract |
|---|---|---|
| `CorrelationId` | `string` | Gets or sets the correlation identifier this wrapper tracks. Use a command correlation string. |
| `ChildContent` | `RenderFragment?` | Gets or sets the wrapped content (typically a generated EditForm). |
| `StatusSurface` | `string?` | Gets or sets the shared status surface; defaults to the bound lifecycle operation. |
| `RenderStatus` | `bool` | Gets or sets whether this wrapper owns the persistent polite status node. |
| `CommandId` | `string?` | Gets or sets the stable command id used by browser E2E selectors. |
| `RejectionMessage` | `string?` | Gets or sets the optional domain-specific rejection copy. Null uses generic localized rejection copy; always rendered as encoded plain text. |
| `RejectionTitle` | `string?` | Optional domain-language rejection title (e.g., "Approval failed"). Null → falls back to the localized "Submission rejected". Rendered as encoded plain text. |
| `RejectionDetails` | `CommandRejectionDetails?` | Gets or sets optional typed rejection metadata. Plain text only; rendered with normal Blazor encoding. |
| `MappedRejection` | `bool` | Gets or sets whether the rejection owns a focused mapped validation summary. When true, the lifecycle region remains visible but suppresses its independent live announcement. |
| `DisplayLabel` | `string?` | Gets or sets the localized command label used by the active lifecycle heading. |
| `IdempotentInfoMessage` | `string?` | Optional adopter-supplied idempotent Info copy override. Null → framework default "This was already confirmed — no action needed." Rendered as encoded plain text. |

### `FcPendingCommandSummary`

| Parameter | Type | Contract |
|---|---|---|
| `Entries` | `IReadOnlyList<PendingCommandEntry>` | Pending-command entries; an empty list uses the registered scoped service snapshot when available. |
| `MaxDetails` | `int` | Maximum visible details; defaults to 5 and nonpositive values fall back to 5. |

### `FcAuthorizedCommandRegion`

| Parameter | Type | Contract |
|---|---|---|
| `CommandType` | `Type` | Command type whose authorization is evaluated; required. |
| `PolicyName` | `string` | Required authorization policy; missing or invalid evaluation fails closed. |
| `BoundedContext` | `string?` | Optional bounded context included in the authorization request. |
| `DisplayLabel` | `string` | Localized command label used in authorization context. |
| `Authorized` | `RenderFragment?` | Content rendered only when the current authorization decision allows access. |
| `Pending` | `RenderFragment?` | Content rendered while the authorization decision is pending. |
| `NotAuthorized` | `RenderFragment?` | Content rendered for blocked or failed authorization evaluation. |

### `FcFormAbandonmentGuard`

| Parameter | Type | Contract |
|---|---|---|
| `ChildContent` | `RenderFragment?` | Gets or sets the form children wrapped by the guard. |
| `EditContext` | `EditContext?` | Gets or sets the edit context exposed by the generated form from `OnEditContextReady`. Required to anchor the first edit; when null the guard is inert. |
| `CorrelationId` | `string` | Command correlation used to suppress the abandonment warning while that operation is Submitting. |

The guard also consumes a boolean cascade named `WrapperInitiatedNavigation`. `FcLifecycleWrapper` already supplies it. In custom compositions, provide it through `CascadingValue` with `Name="WrapperInitiatedNavigation"`, render the true value before navigation starts so the guard receives it, and reset the flag afterward. The guard yields without prompting while the received value is true. This is a named cascading value, separate from the explicit component parameters above.

### `FcDestructiveConfirmationDialog`

Direct callers must open the component through `IDialogService` with the generated forms' dialog options: `Modal = true`, `PreventDismissOnEscape = false`, `Id = "fc-destructive-confirmation"`, and `AdditionalAttributes` containing `aria-labelledby = "fc-destructive-dialog-title"` and `aria-describedby = "fc-destructive-dialog-description"`. These attributes connect the dialog to its title and explanation.

| Parameter | Type | Contract |
|---|---|---|
| `Title` | `string?` | Dialog title. Generated forms pass `[Destructive(ConfirmationTitle = …)]` text literally; without configured text, they use the shell-localized title template with the command display label. Adopters own localization of configured attribute text; direct callers must supply their own localized title. |
| `Body` | `string?` | Dialog explanation. Generated forms pass `[Destructive(ConfirmationBody = …)]` text literally; without configured text, they use the shell-localized default body. Adopters own localization of configured attribute text; direct callers must supply any localized body text. |
| `DestructiveLabel` | `string?` | Destructive action label. Generated forms use the command display label. Direct callers own localization; a null label displays the fixed English literal "Confirm". |
| `OnConfirm` | `EventCallback` | Gets or sets the confirm callback invoked when the user clicks the destructive button. Optional in tests; in production the caller relies on `IDialogService`'s result. |
| `OnCancel` | `EventCallback` | Gets or sets the cancel callback invoked when the user clicks Cancel or presses Escape. Optional in tests; in production the caller relies on `IDialogService`'s result. |

### `FcCommandBlockedOutcome`

| Parameter | Type | Contract |
|---|---|---|
| `AttemptedControlId` | `string` | Gets or sets the DOM id of the control whose activation was blocked. |
| `Inline` | `bool` | Gets or sets a value indicating whether the outcome renders inline, beside a trigger in the text flow (a zero-field inline command), instead of as its own row under a form. |
| `AnnouncementSurface` | `string?` | Gets or sets the shared surface used by a generated command form. |
| `OwnsSurfaceStatus` | `bool` | Gets or sets whether this visible control owns the shared status node for a hidden form. |

Direct callers capture the component with `@ref` and call `PresentAsync()` for each blocked attempt; setting parameters alone does not present the outcome. `ClearAsync()` withdraws it, for example when a later attempt is admitted. Rebinding either `AttemptedControlId` or `AnnouncementSurface` also withdraws the presented outcome and cancels its announcement. The read-only `IsBlocked` property reports whether an outcome is currently presented.

### `FcValidationSummary`

| Parameter | Type | Contract |
|---|---|---|
| `EditContext` | `EditContext?` | Gets or sets the edit context whose messages are summarized. |
| `Fields` | `IReadOnlyList<FcValidationFieldDescriptor>` | Gets or sets fields in their declared DOM order. |
| `FormLevelErrors` | `IReadOnlyList<string>` | Gets or sets support-safe form-level errors that have no field target. |
| `SummaryId` | `string` | Gets or sets the stable summary DOM identifier. |

Direct callers capture the component with `@ref` and call `ShowAndFocusAsync(FcValidationSummaryKind)` to show the current errors and schedule summary focus; setting parameters alone leaves it hidden. The two supported kinds are `ClientValidation` for a client validation attempt blocked before dispatch and `MappedServerRejection` for an authoritative rejection with mapped field errors. `HideAsync()` hides the summary without clearing the underlying validation messages.

## Layout (FC-LYT)

The wrapper owns progressive pending feedback, rejection and recovery; the form owns inputs and validation. `FcPendingCommandSummary` bounds visible details with `MaxDetails`. The abandonment guard warns only after the first edit, while the EditContext remains modified and `FcShellOptions.FormAbandonmentThresholdSeconds` has elapsed (default 30 seconds, allowed range 5–600). Startup validation also requires `FormAbandonmentThresholdSeconds * 1000 > StillSyncingThresholdMs`; equality or a smaller value fails startup even within that range. No edit, unmodified input, time below the threshold, Submitting, and wrapper-initiated navigation suppress the warning. Syncing does not suppress it; modified input past the threshold remains protected. Authorization pending/denied and blocked-before-dispatch outcomes must remain distinct from submitted lifecycle entries.

### `CommandLifecycleState`

| State | Meaning |
|---|---|
| `Idle` | No command in flight. |
| `Submitting` | Dispatch underway, awaiting acknowledgement. |
| `Acknowledged` | Transport accepted; projection/status confirmation still pending. |
| `Syncing` | Projection update detected and being applied. |
| `Confirmed` | Authoritative result confirmed. |
| `Rejected` | Domain rejection; preserve useful input and explain recovery. |
| `IdempotentConfirmed` | Result already confirmed; no repeated side effect implied. |
| `NeedsReview` | Human review required. |
| `Warning` | Command completed with a warning. |
| `Degraded` | Confirmation delayed while polling remains active. |
| `DegradedExhausted` | Confirmation budget exhausted; local lifecycle closed without claiming success. |

These are command outcomes, not `FcAggregateDetailState`, transport connectivity or view `Loading/Empty/Loaded` summaries. Use the existing measured UX timing and announcement helpers rather than sleeps.

### `ProjectionConnectionStatus`

Transport state observed by the lifecycle wrapper remains separate from command confirmation; see [Projection status](projection-status.md).

| State | Meaning |
|---|---|
| `Connected` | Hub transport connected; command confirmation still needs an authoritative result. |
| `Reconnecting` | Hub reconnection active; this is not a command outcome. |
| `Disconnected` | Hub transport unavailable; do not infer rejection or confirmation. |

## Accessibility (FC-A11Y)

Persistent polite status reports lifecycle progress. A mapped rejection summary owns focused validation instead of a second live announcement; blocked submissions retain attempted-control focus and create no dispatch or lifecycle entry. Destructive confirmation needs named controls and restoration to its invoker. Validate linked summaries, input preservation and focus in browser measurements. Keep caller content named and keyboard reachable; see [HFC1050](../../diagnostics/HFC1050.md) and [HFC1051](../../diagnostics/HFC1051.md).

## Localization (FC-L10N)

Built-in progress, support-reference and recovery copy is shell-localized. `DisplayLabel`, rejection overrides, confirmation labels and domain validation messages are adopter-owned complete strings. Rejection copy is plain encoded text; never place payloads, tokens or backend exceptions in operator guidance.

## Related

- [Page patterns](page-patterns.md)
- [Projection status](projection-status.md)
- [Customization](customization.md)
- [Components](index.md)

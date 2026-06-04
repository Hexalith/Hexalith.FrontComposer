# FC-CNC One-at-a-Time Execution Policy Contract

Date: 2026-06-04

## Decision

FC-CNC v1 command execution is one-at-a-time per Shell circuit/user scope.

The approved v1 fallback is to block and reject the later local submit. The framework must not create a client-side queue, batch commands, automatically retry the blocked command, or hide a background dispatch. A blocked local submit produces operator-visible warning feedback and no command side effects.

Batching and queued execution are fast-follow scope and are not part of this v1 contract.

## Lock Lifetime

Generated command forms acquire a short pre-dispatch admission lock after client validation, authorization checks, and `BeforeSubmit`, but before lifecycle submission actions, command dispatch, pending registration, or EventStore HTTP send.

The pre-dispatch admission lock is held until the command service dispatch completes and any accepted result has been registered with `IPendingCommandStateService`. The lock is released from `finally` paths so validation, authorization, dispatch, cancellation, or registration exceptions cannot permanently block the circuit.

After an accepted command is registered, `PendingCommandStateService` is the source of truth. A command remains in-flight while `IPendingCommandStateService.Snapshot()` contains any entry with `PendingCommandStatus.Pending`. Terminal statuses do not block later command admission:

- `Confirmed`
- `Rejected`
- `IdempotentConfirmed`
- `NeedsReview`

## Side-Effect Boundary

If FC-CNC denies admission, generated forms must return before:

- generated `SubmittedAction`
- `ICommandService.DispatchAsync`
- `IPendingCommandStateService.Register`
- EventStore HTTP dispatch
- command lifecycle mutation

The original admitted or pending command continues normally.

## Operator Feedback

Blocked submits must show a non-modal warning using the existing command feedback/message-bar pattern. The form input remains intact and correctable, focus is not stolen, and the copy must not claim the command was queued, retried, or submitted.

## Related Architecture

This contract closes AR7 FC-CNC for v1 by choosing one-at-a-time local admission with block/reject fallback. AR8 command-status polling budgets, retry/degraded handling, queued execution, batching, and MCP-facing command policy are separate follow-up scope.


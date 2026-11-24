---
title: 'DW-886 Quote command labels in French authorization copy'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the French authorization messages that embed the command label read correctly whether the label is a verb phrase or a noun, matching how the English copy reads with verb labels.

</frozen-after-approval>

## Implementation Notes

- The three French messages that embed the command label (`UnauthorizedCommandWarningMessage`, `UnauthenticatedCommandWarningMessage`, `AuthorizationCheckingPermissionMessage`) now name the label as an action in guillemets, with U+00A0 inside, so verb and noun labels both read correctly. Each carries a comment explaining this. Placeholders are unchanged.
- English stays unchanged because it reads naturally with verb labels; its comments now tell translators they may quote the label. English with noun-shaped default labels is deferred (see triage).
- Tests: the French denial-copy test now expects the quoted label, and `FrenchAuthorizationCopyQuotesTheCommandLabel` pins all three French values, including the no-break spaces.
- Validation: `FcShellResourcesTests`, `LocalizationGovernanceTests` and `CommandRendererWrapperIntegrationTests` 170/170.

## Review Triage Log

- medium, patched — the French sign-in message had the same verb-or-noun problem; it now quotes the label the same way.
- low, patched — the new French pending-check copy is pinned by a resource test.
- low, patched — the same resource test pins the no-break spaces inside the guillemets.
- low, patched — the changed French entries carry comments, and the English comments tell translators they may quote the label.
- medium, deferred — English and the generated English fallbacks still read awkwardly with noun-shaped default labels (type names split into words); changing the English copy is a broader user-visible decision.
- false — the ledger entry is closed when the change is finalized; its old line references are historical.
- low, deferred with the English item — the generated fallbacks follow the English copy.

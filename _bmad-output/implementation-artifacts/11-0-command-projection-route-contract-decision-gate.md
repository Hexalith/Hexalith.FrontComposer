---
created: 2026-07-05
completed: 2026-07-05
owner: Architect + Product
contract: _bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md
---

# Story 11.0: Command/projection route-contract decision gate

Status: done

## Story

As a Product Owner and Architect,
I want the command route family selected before Epic 11 implementation starts,
so that command activation from the palette and empty-state CTA targets real generated pages.

## Decision

The canonical generated command route family is:

```text
/commands/{BoundedContext}/{CommandTypeName}
```

Projection routes remain unchanged. The historical `/domain/{bounded-context-kebab}/{command-type-kebab}`
command activation family is not canonical and must not be emitted by new framework-owned command
activation paths.

The decision is recorded in `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`.

## Acceptance Criteria

1. Given the current route families - projection links `/{bc-lower}/{proj-kebab}`, palette/CTA
   command links `/domain/{kebab}/{kebab}`, and generated command pages
   `/commands/{BC}/{TypeName}` - when Architect + Product review the route contract, then they
   select one canonical command route family and record the decision in a contract artifact.

   Result: done. The selected family is `/commands/{BoundedContext}/{CommandTypeName}`.

2. Given the route decision is recorded, when Story 11.7 is created, then it implements only the
   selected route contract and adds the e2e route-activation pin.

   Result: done at decision level. Story 11.7 remains the implementation story.

3. Given Story 11.0 is not done, when any Story 11.1+ `create-story` request is made, then the
   request is blocked with the dated owner and decision status.

   Result: done. Story 11.0 no longer blocks Story 11.1+ `create-story` work. Story 11.7 remains
   blocked from ready-for-dev only until it cites and implements the recorded contract.

## Implementation Handoff

Story 11.7 must update palette command entries, projection empty-state CTAs, and any other
framework-owned command activation surfaces to target `/commands/{BoundedContext}/{CommandTypeName}`.
It must also add an e2e pin proving command activation lands on a generated command page that exists.

Compatibility for old `/domain/...` recent routes is optional and transitional. If Story 11.7 adds it,
the compatibility route must redirect or alias to `/commands/...` and must not become the advertised
route family.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Completion Notes

- Recorded the FC-ROUTE generated command route contract.
- Updated planning and sprint-status artifacts so the Story 11.0 decision gate is no longer open.
- No source code was changed; implementation remains owned by Story 11.7.

### File List

- `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`
- `_bmad-output/implementation-artifacts/11-0-command-projection-route-contract-decision-gate.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e11-route-contract-decision.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Test Evidence

Not run. This story records a planning/contract decision only and does not change source code.
Story 11.7 must provide focused unit/e2e verification when it implements the route change.

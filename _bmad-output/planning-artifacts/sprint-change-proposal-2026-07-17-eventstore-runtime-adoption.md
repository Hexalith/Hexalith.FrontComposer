# Sprint Change Proposal: EventStore Runtime Identity Adoption

Date: 2026-07-17  
Approval: Administrator directed application of the detailed blocker/story plan.  
Scope: Minor direct backlog adjustment; Product Owner / Developer handoff.

## 1. Issue Summary

EventStore Story 1.20 now has implementation corrections for its lifecycle and AD-11 blockers, but it
still authorizes no consumer migration. FrontComposer consumes EventStore in both source and package
modes, while its current EventStore gitlink and Builds-owned package version are different identities.
Completed Stories 11.1, 11.2, and 11.19b do not own a future approved-runtime adoption, and reopening
them would corrupt completed history.

## 2. Impact Analysis

- Epic impact: add one independent child to Epic 11's runtime reliability/security workstream.
- Story impact: register Story 11.24 as backlog; do not create its implementation file yet.
- PRD/MVP impact: none. EventStore remains the backend integration model and existing product scope is
  unchanged.
- Architecture/UX impact: no UX change and no adapter/topology redesign. The change adds an identity
  and verification gate around existing source/package integration.
- Release impact: package adoption requires the exact approved EventStore version/hashes through an
  already-landed Builds commit; source adoption requires exact gitlink/checkout equality.

## 3. Recommended Approach

Use a direct adjustment. Register Story 11.24 in Epic 11 and sprint status as blocked backlog. Keep it
independent of Stories 11.20–11.23. Activation requires the durable Story 1.20 decision, exact source
and package identities, and named approval. Rollback and PRD/MVP reduction are not justified.

Effort is medium once activated because package/source validation, Pact provider verification, and a
live Aspire smoke are required. Pre-activation risk is low because no dependency pointer or runtime
code changes.

## 4. Detailed Changes

- `epics.md`: add Story 11.24 with fail-closed activation, source/package identity, Pact, build, and
  Aspire acceptance criteria.
- `sprint-status.yaml`: register `11-24-adopt-owner-approved-eventstore-runtime: backlog` and record
  that story-file creation is prohibited before authorization.
- `epic-11-context.md`: add the activation and non-redesign boundaries.
- No Story 11.24 implementation file is created by this proposal.

## 5. Checklist And Handoff

- [x] Trigger and evidence identified: EventStore Story 1.20 is non-authorizing and consumer identities
  are not converged.
- [x] Epic, PRD, architecture, UX, testing, release, and submodule impacts assessed.
- [x] Direct adjustment selected; rollback and MVP reduction rejected.
- [x] Administrator approval recorded by the `apply` directive.
- [x] Sprint registration updated.
- [!] Developer: create Story 11.24 only after the activation gate passes, then implement and verify.
- [!] Product/Release owners: provide the durable Story 1.20 authority and approved identities.

Success means the backlog preserves completed history, cannot start on an unapproved EventStore
identity, and contains enough verification scope for source and package modes to converge later.

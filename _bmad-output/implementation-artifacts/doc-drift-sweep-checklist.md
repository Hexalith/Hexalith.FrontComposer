# Doc Drift Sweep Checklist

Source: E8-AI-2, Epic 8 retrospective follow-through; E10R-AI-2, Epic 10 retrospective follow-through.

Use this checklist after any story, review fix, or post-epic sweep changes a public component surface,
route contract, CLI output, diagnostic metadata, generated-output shape, MCP descriptor, or adopter-facing
behavior. Also use it whenever a review fix changes implementation behavior governed by a contract document,
even if the original story tasks already updated documentation before review.

## Required Sweep

1. Identify the public terms that changed: component names, parameters, routes, menu entries, deleted
   components, metadata fields, command names, diagnostic IDs, or generated artifact names.
2. Search current public docs and generated planning docs for stale terms.
   - Start with `docs/reference/**`, `docs/how-to/**`, `docs/tutorials/**`, `docs/skills/frontcomposer/**`,
     and `_bmad-output/project-docs/**` when present.
   - Include story/proposal artifacts only when the change amends current planning guidance; leave
     completed story records as provenance unless they make a current-state claim.
3. For every affected public page, verify body text against the implemented source and latest reviewed
   story evidence.
4. If a review fix changed implementation behavior, verify contract documents explicitly:
   - Search `_bmad-output/contracts/**` first for the affected contract family.
   - Also check `_bmad-output/project-docs/api-contracts.md`, `docs/reference/**`, and package README files
     when they make current contract claims.
   - Update any contract document that still describes the pre-review design.
   - If no contract document exists or no update is needed, record the no-update decision and rationale.
5. Verify front matter:
   - `ownerStory` points to the story that owns the current public contract or the latest material public
     surface change.
   - `reviewed` reflects the review or synchronization date.
   - `status`, `uid`, and `slug` remain stable unless the page is intentionally moved or unpublished.
6. Record no-update decisions for searched documents that looked relevant but were already current.
7. Run docs validation where feasible:
   - `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
   - If full DocFX or snippet validation is required by the story, run it separately and record blockers
     explicitly.
8. Add the sweep result to the story, retrospective, or sprint-change proposal evidence before promoting
   the work to done.

## Evidence Record Format

```md
### Documentation Drift Sweep

- Source change:
- Search terms:
- Contract docs checked:
- Documents updated:
- No-update decisions:
- Validation:
- Residual risk:
```

## 2026-07-05 Closure Records

### E8-AI-2

- Source change: E8-AI-2 shell/navigation metadata synchronization after Story 8.3 and Story 8.5.
- Documents updated:
  - `docs/reference/components/front-composer-shell.md` owner metadata now points to
    `8-3-brand-logo-cell-in-header-start`; `reviewed` was already `2026-06-25`.
  - `docs/reference/components/navigation.md` owner metadata now points to
    `8-5-icon-label-navigation-rail-and-projection-flyout`; `reviewed` was already `2026-06-25`.
- Future sweep record: this checklist and the story-review reconciliation checklist now require an explicit
  documentation sweep when public surfaces change.
- Sprint status: E8-AI-2 marked done.

### E10R-AI-2

- Source change: Epic 10 retrospective found that the Testing host contract could still describe
  pre-review redaction behavior after Story 10.5 review fixes changed the implementation.
- Documents updated:
  - `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md` now requires explicit
    `_bmad-output/contracts/**` verification when review fixes change implementation behavior.
  - `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` now makes
    behavior-changing review fixes a doc-drift trigger and requires contract-doc update/no-update evidence.
  - `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e10r-ai-2-contract-doc-verification.md`
    records the correct-course decision and handoff.
- Future sweep record: post-story and post-epic sweeps must record contract docs checked, documents updated,
  and no-update decisions whenever review fixes materially change behavior.
- Sprint status: E10R-AI-2 marked done.

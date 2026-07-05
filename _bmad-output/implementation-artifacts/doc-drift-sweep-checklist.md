# Doc Drift Sweep Checklist

Source: E8-AI-2, Epic 8 retrospective follow-through.

Use this checklist after any story, review fix, or post-epic sweep changes a public component surface,
route contract, CLI output, diagnostic metadata, generated-output shape, MCP descriptor, or adopter-facing
behavior.

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
4. Verify front matter:
   - `ownerStory` points to the story that owns the current public contract or the latest material public
     surface change.
   - `reviewed` reflects the review or synchronization date.
   - `status`, `uid`, and `slug` remain stable unless the page is intentionally moved or unpublished.
5. Record no-update decisions for searched documents that looked relevant but were already current.
6. Run docs validation where feasible:
   - `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
   - If full DocFX or snippet validation is required by the story, run it separately and record blockers
     explicitly.
7. Add the sweep result to the story, retrospective, or sprint-change proposal evidence before promoting
   the work to done.

## Evidence Record Format

```md
### Documentation Drift Sweep

- Source change:
- Search terms:
- Documents updated:
- No-update decisions:
- Validation:
- Residual risk:
```

## 2026-07-05 Closure Record

- Source change: E8-AI-2 shell/navigation metadata synchronization after Story 8.3 and Story 8.5.
- Documents updated:
  - `docs/reference/components/front-composer-shell.md` owner metadata now points to
    `8-3-brand-logo-cell-in-header-start`; `reviewed` was already `2026-06-25`.
  - `docs/reference/components/navigation.md` owner metadata now points to
    `8-5-icon-label-navigation-rail-and-projection-flyout`; `reviewed` was already `2026-06-25`.
- Future sweep record: this checklist and the story-review reconciliation checklist now require an explicit
  documentation sweep when public surfaces change.
- Sprint status: E8-AI-2 marked done.

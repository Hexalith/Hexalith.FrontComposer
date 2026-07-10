---
title: 'Fix accessibility-visual CI specimen baselines'
type: 'bugfix'
created: '2026-07-09T00:00:00+02:00'
status: 'done'
route: 'one-shot'
---

## Intent

Fix the `accessibility-visual` CI failure from GitHub Actions run `29023685860` job `86137533044`: the Windows Chromium FrontComposer type specimen screenshots were stale after the Counter specimen host began loading the Shell scoped CSS bundle. Refresh the visual baselines with explicit rationale, keep generated command contrast readable in the dark specimen, and add a browser guard for the dark generated-command contrast regression found during review.

## Suggested Review Order

- Start with the browser assertion and Windows-only visual tolerance.
  [`specimen-accessibility.spec.ts`](../../tests/e2e/specs/specimen-accessibility.spec.ts)

- Review the specimen-scoped CSS contrast fix for generated command components.
  [`FrontComposerTypeSpecimen.razor.css`](../../samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor.css)

- Check the required baseline rationale for before/after evidence.
  [`baseline-change-rationale.md`](../../docs/accessibility-verification/baseline-change-rationale.md)

- Compare the refreshed Light/Dark by Compact/Comfortable/Roomy Chromium snapshots.
  [`specimen-accessibility.spec.ts-snapshots`](../../tests/e2e/specs/specimen-accessibility.spec.ts-snapshots)

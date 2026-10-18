The review corrections reject native-plane circular coverage when effective CSS zoom occurs anywhere in composed ancestry or when a supported corner shape is nonround. Both `round` and its computed `superellipse(1)` form retain existing valid coverage. Regressions establish an unmodified positive first, then change only direct zoom, composed-ancestor zoom, or supported bevel paint. Firefox records an explicit unsupported bevel capability skip.

The runner now refuses existing record or legacy/new log destinations before writing evidence. Each lane's candidate/run identity, variables and both commands are saved before launch. JSON and JUnit paths are explicitly owned by the lane. Launch errors, signals, unavailable roots and unavailable/malformed reports persist incomplete dispositions and allow later engines to run. A completed failed test/validator gate remains `complete: true`, with `gatesPassed: false` and process exit 1. New captures use nonignored `.log.txt` destinations.

The relocation audit verifies `../runtime-inputs.json` before executing the historical replay, binding the imported state validator, full artifact validator and specimen manifest separately from the immutable historical candidate. Exactly one Chromium, Firefox and WebKit lane must match candidate, retained mapping, report run metadata, report engine and recorded outcomes. Every retained replay row must be accounted for, with exactly one complete row per engine.

Verification performed only the focused native-plane browser cases and the packet scripts' targeted regressions:

- `browser-red-final.txt`: saved reviewed helper, 8 failures and 1 capability skip. Six zoom assertions and both supported bevel assertions rejected the old resolved 21:1 result.
- `browser-green-final.txt`: corrected helper, 14 passed and 1 capability skip across three engines. Includes existing contained/exposed rounded-plane checks and opaque native text coverage.
- `tooling-green.txt`: 25 targeted Node tests passed. Covers overwrite refusal, prelaunch metadata, caller report-path isolation, missing/invalid reports, missing launches, signals, completed failed gates, runtime mutation rejection, duplicate lane/mapping/replay rows, and candidate/environment/mapping/report/outcome identities.
- `tooling-red.txt`: the same new contract suite exits 1 against the saved prior tools. This is a contract regression run; individual failures can reflect newly required metadata as well as former unsafe acceptance.
- `relocation-audit-green.json`: hardened audit exits 0 with three runtime hashes verified, all original 7,310 retained files and 709 full-gate JUnit markers verified. Underlying replay/artifact gates still exit 1 and retain the identical 18 visual-baseline failures. Overall classification remains blocked.

`before/` plus `before-source.json` preserve the reviewed source/tool bytes. `focused-browser-captures.json` inventories every byte and attachment path in all four focused browser attempts, including the initial alias-serialization mistake, with lossless archives. Extract an archive to any new directory and use its `files` and `attachmentPaths` entries to verify/relocate paths. `retain-focused-captures.py` records the capture recipe and refuses archive replacement.

Run the focused checks from the repository root:

```sh
node --test _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser/review-fixes/tooling.test.mjs
```

Run the browser selection from `tests/e2e`, with fresh output roots and run identity:

```sh
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_A11Y_EVIDENCE_RUN_ID=unique-review-run FC_E2E_OUTPUT_DIR=/tmp/unique-review-run/results FC_E2E_HTML_REPORT_DIR=/tmp/unique-review-run/html npx playwright test specs/responsive-accessibility.spec.ts --grep 'review native-plane|repeatable rounded native planes|review opaque native top-layer' --workers=3 --retries=0
```

Run the historical packet's integrity audit from the repository root:

```sh
node _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser/audit-relocation.mjs
```

The original candidate, full browser records/reports, attachment map, archives and original failed outcomes are unchanged. These scoped corrections require a fresh candidate and complete gates before being represented as final full-run evidence; the parent workflow owns those runs and result refresh.

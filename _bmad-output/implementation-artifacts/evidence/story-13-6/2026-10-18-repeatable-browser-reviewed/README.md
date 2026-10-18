# Reviewed Story 13.6 browser evidence

This final packet binds root `1f8c7b2b4424b6232121a7cc567f7129e46577d3` and source patch `b6b5b7a761b4927d21ee0fe9ba5daf78ce31ea87928f28b3e2a3b842e8fe5fc3`. It follows the three-layer review and its seven private corrections. Story/sprint remain in review; accessibility classification is blocked.

All three existing accessibility specs ran completely per engine: **774 total / 752 passed / 18 visual failures / 4 capability skips / 0 flaky**. Every executed nonvisual case passes. Each results/HTML root meets the unchanged 52,428,800-byte limit. Full artifact validators still exit 1 for the six visual failures in each engine.

`candidate.json`, `candidate.patch`, `browser-runs.json`, `runtime-inputs.json` and `verification.json` retain exact source/tool identities, commands and dispositions. The three engine archives contain every raw report, capture, external expected image and complete HTML tree. `attachment-path-map.json` verifies every archived byte. Six nonignored validator/browser logs have their own `support-logs.json` hashes.

Run the read-only integrity replay from the repository root:

```sh
node _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/audit-relocation.mjs
```

Expected: structural integrity exits 0 while explicitly retaining blocked accessibility and the original underlying validator exit 1. It verifies 2,079 files, 709 JSON attachment paths and 709 JUnit markers, all 116 required state rows per engine, runtime-validator hashes, unique engine/run identities and unchanged outcomes. It extracts into a fresh temporary directory; raw reports remain unchanged. Open the restored HTML tree through Playwright's report server.

`git-visible-copy-proof.json` and `git-visible-copy-replay.json` retain the independent replay using only Git-visible inputs and a fresh installation from the existing lockfile. No ignored logs or original-checkout support files were used.

Earlier causal failures, intermediate/incomplete attempts, review regressions and the original full packet remain under [the prior packet](../2026-10-18-repeatable-browser/README.md). Review fixtures passed 14 cases with one explicit Firefox bevel-capability skip; all 25 tooling regressions passed. Build/.NET evidence is reused from the prior packet because all 37 assemblies and C# source match; its four existing snapshot/catalog failures stay open. These captures provide no manual AT, actual browser zoom, physical-device, Windows, actual WebKit forced-palette or owner approval.

The owned Aspire topology is stopped; `aspire-stopped.txt` confirms no AppHost running. Nothing was staged, committed or pushed.

# Repeatable Story 13.6 browser evidence

This packet records one final candidate, all complete engine gates, and every causal/intermediate attempt. Story and sprint remain in review. All eighteen visual comparisons and four existing .NET snapshot/catalog tests remain failed; manual and owner gates remain open.

- `candidate.json` and `candidate.patch` identify the exact application/test/configuration candidate. `initial-source.json`, `before/`, `palette-red-source.json` and intermediate manifests retain causality inputs.
- `support-logs.json` hash-binds all 29 root support logs, retained byte-for-byte as nonignored `*.log.txt` files. Replay never reads the ignored writer output names.
- `browser-runs.json` records all 765 final outcomes, exact commands, unique run IDs and original logical root sizes. The three `*-artifacts.log.txt` files preserve full validator failures.
- `chromium.tar.gz`, `firefox.tar.gz` and `webkit.tar.gz` contain complete raw results and HTML trees. External expected PNG attachments are captured under `external/`. Seven causal archives retain complete failed/passing attempts. `interrupted-full-chromium.tar.gz` retains all raw fragments from the interrupted, oversized attempt; it has no completion reports and supplies no gate proof.
- `attachment-path-map.json` binds every archive, retained file, raw report and JSON attachment path to SHA-256. Identical members use ordinary tar hard links; extraction restores all paths and original bytes. `archive-deduplication.json` records the lossless container transformation. The original 52,428,800-byte gate counts every logical file, including hard links separately.

Run the read-only portability audit from any checkout location with the repository's existing Node dependencies and `tar` installed:

```bash
node _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser/audit-relocation.mjs > /tmp/story-13-6-relocation-audit.json
```

The [Git-visible-only replay proof](git-visible-copy-proof.json) uses a fresh workspace populated exclusively from tracked and untracked nonignored packet/runtime inputs. It copies zero ignored logs, installs dependencies independently from the copied pinned lockfile, and [passes replay](git-visible-copy-replay.json) with all 29 retained support logs. No original checkout node_modules or support files are used. `verify-git-visible-copy.py` retains the reproduction recipe.

The audit extracts every archive into a fresh temporary directory and checks every file hash and attachment owner. It calls the original fail-closed artifact validator on path-relocated JSON, checks all 116 state rows per engine, verifies JUnit marker paths, and emits `relocated-report.json` / `relocated-junit.xml` beside the original results. Raw JSON/JUnit bytes remain unchanged. All HTML files and assets are retained; open `<restored>/<engine>/html/index.html` through Playwright's report server. The output names the fresh `restored` directory.

**Expected distinction:** the portability audit exits 0 only when integrity/ownership checks pass and all original visual gate failures remain identical. Its output explicitly retains `overallAccessibilityClassification: blocked`. Running `validate-state-packet.mjs` directly exits 1 with those unchanged full-gate failures. Neither result approves the visual baselines or supplies manual AT/device/owner evidence.

`run-browser-lanes.mjs`, `capture-packets.mjs`, `deduplicate-archives.py` and `retain-support-logs.mjs` are the construction recipes for this dated packet. Use a new evidence directory, candidate manifest, run IDs and separate output/HTML roots for a new execution. The exact per-engine commands and Test settings are recorded in `browser-runs.json`; snapshot updates and retries were disabled. No dependencies or approved PNG/text baselines were changed.

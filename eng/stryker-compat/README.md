# Private Stryker MTP compatibility build

`definition.json` identifies `5.1.0-hexalith.compat.1`, a private build of upstream
[7052fa6c646a6bdfb143608e695d11542e2be815](https://github.com/stryker-mutator/stryker-net/tree/7052fa6c646a6bdfb143608e695d11542e2be815).
The separate official base pin remains 5.1.0. The build keeps the upstream
Apache-2.0 [license](LICENSE), target framework and locked dependency versions.
It is built locally in job-private storage and is never published as a tool.

`static-reset-compatibility.patch` changes one production file: the MTP runner
resets its host before `IsStaticValue` or `MustBeTestedInIsolation` execution,
then clears the control ID and resets in `finally`. Ordinary reuse is preserved;
unexpected multiple-mutant batches fail before changing runner state. The patch
also adds 15 lifecycle controls and strengthens the two prior batch controls.
The reset approach is described in upstream [PR 3695](https://github.com/stryker-mutator/stryker-net/pull/3695).
Core mutator generation, eligibility, filters, score calculation and timeouts are
unchanged by this compatibility patch.

The official archive SHA, exact patch/path hashes, SDK 11.0.100, original license
and HTML resource are authenticated before use. Checkout CRLF is normalized only
for authenticating/applying the approved LF patch. Preparation resolves locked
packages from NuGet.org, inspects actual CLI/Core/MTP PE metadata, and inventories
all runtime bytes. Preparation subprocesses have bounded waits, disabled compiler
and MSBuild servers, and owned session cleanup/reaping. Actual mutation execution
uses `exec` in its original process group and retains the tool's exit/cancellation.

`python3 eng/stryker_compat.py` accepts only `prepare`, `run <segment>` and
`verify <segment>`. Its scratch directory is computed from authenticated inputs
under the current job's `RUNNER_TEMP` or the local wrapper's `TMPDIR`; there is no
external executable/receipt path or mutation-option override. A segment requires
clean tracked source, no nonignored untracked input, and an empty output directory.
Its pre-execution receipt binds the current commit, exact config and output to the
verified DLL/build. It does not assert completion or a passing mutation score.

Finite lifecycle/activation controls admit the runner; complete current-source
Happy 80 and Error 60 runs, original target roots and strict report/triage checks
remain required. Follow the [local replay guide](../../docs/how-to/mutation-and-property-quality-gates.md).

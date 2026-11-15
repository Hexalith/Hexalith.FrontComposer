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

## Error timeout allowance

The Error config sets `additional-timeout` to 60,000 milliseconds. This adds
50 seconds to every Error mutant deadline compared with the former 10-second
default allowance; it also affects coverage capture and is not capture-only.
Happy retains its 170,000-millisecond allowance. Thresholds, roots, filters,
per-test coverage and two test servers remain unchanged.

A bounded native control used the retained a7 injected binaries and exact 26
cases associated with the historical 15.942-second deadline. Under two CPUs,
10 GiB and no swap, the no-active 26-case process passed in 24.266 seconds while
a concurrent no-active 224-case process passed in 35.579 seconds. A separate
arm with a pre-created control ID of 15019 passed those same 26 cases in
24.820 seconds while its concurrent no-active cohort passed in 36.506 seconds.
The retained control receipt has SHA-256
`79c792923af621daa4e1264eb8b273d264b9dee6a482ea4b24001f974f3572ee`.

Fresh native processes differ from reused Stryker RPC sessions. The control
shows that ordinary work under paired load exceeded the former budget; it does
not establish historical RPC causality or prove activation of a particular
injected branch. Historical report statuses are preserved. Complete reports
from the new exact source still determine qualification.

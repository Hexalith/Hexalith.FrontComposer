import assert from 'node:assert/strict';
import { access, chmod, mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
import test from 'node:test';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const PROOF_SCRIPT = join(REPOSITORY_ROOT, 'eng', 'run-epic9-live-proof.sh');
const APPHOST = join(
  REPOSITORY_ROOT,
  'src',
  'Hexalith.FrontComposer.AppHost',
  'Hexalith.FrontComposer.AppHost.csproj',
);
const EXPECTED_SOURCE_GRAPH_BUILD = `build ${APPHOST} --configuration Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
const CANDIDATE = '1234567890abcdef1234567890abcdef12345678';
const OTHER_CANDIDATE = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';

const writeExecutable = async (path, content) => {
  await writeFile(path, content);
  await chmod(path, 0o755);
};

const createHarness = async (t, options = {}) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-epic9-runner-'));
  const bin = join(root, 'bin');
  const artifactRoot = join(root, 'artifacts');
  const aspireLog = join(root, 'aspire.log');
  const npmLog = join(root, 'npm.log');
  const dotnetLog = join(root, 'dotnet.log');
  const stateFile = join(root, 'aspire-state');
  await mkdir(bin, { recursive: true });
  await writeFile(stateFile, 'stopped\n');
  await writeFile(`${stateFile}.pid`, '4321\n');
  t.after(() => rm(root, { recursive: true, force: true }));

  await writeExecutable(join(bin, 'git'), `#!/usr/bin/env bash
set -euo pipefail
next_count() {
  local count_file="$1"
  local count=0
  if [[ -f "$count_file" ]]; then read -r count < "$count_file"; fi
  count=$((count + 1))
  printf '%s\\n' "$count" > "$count_file"
  printf '%s' "$count"
}
if [[ "$1" == "rev-parse" && "$2" == "--show-toplevel" ]]; then
  if [[ "\${FC_EPIC9_FAKE_ROOT_FAIL:-false}" == "true" ]]; then exit 97; fi
  printf '%s\\n' "$FC_EPIC9_FAKE_REPOSITORY_ROOT"
elif [[ "$1" == "-C" && "$3" == "rev-parse" && "$4" == "HEAD" ]]; then
  count="$(next_count "$FC_EPIC9_FAKE_HEAD_COUNT")"
  if [[ "\${FC_EPIC9_FAKE_HEAD_FAIL_CALL:-0}" == "$count" ]]; then exit 97; fi
  if [[ "$count" -eq 1 ]]; then
    printf '%s\\n' "$FC_EPIC9_FAKE_INITIAL_HEAD"
  else
    printf '%s\\n' "$FC_EPIC9_FAKE_FINAL_HEAD"
  fi
elif [[ "$1" == "-C" && "$3" == "status" && "$4" == "--porcelain" ]]; then
  count="$(next_count "$FC_EPIC9_FAKE_STATUS_COUNT")"
  if [[ "\${FC_EPIC9_FAKE_STATUS_FAIL_CALL:-0}" == "$count" ]]; then exit 97; fi
  if [[ "$count" -eq 1 ]]; then
    printf '%s' "$FC_EPIC9_FAKE_INITIAL_STATUS"
  else
    printf '%s' "$FC_EPIC9_FAKE_FINAL_STATUS"
  fi
else
  printf 'Unexpected fake git invocation: %s\\n' "$*" >&2
  exit 98
fi
`);

  await writeExecutable(join(bin, 'aspire'), `#!/usr/bin/env bash
set -euo pipefail
if [[ "\${HexalithFrontComposerFromSource:-}" != "true" ]]; then
  printf 'Epic 9 Aspire lifecycle requires FrontComposer source routing.\n' >&2
  exit 91
fi
next_count() {
  local count_file="$1"
  local count=0
  if [[ -f "$count_file" ]]; then read -r count < "$count_file"; fi
  count=$((count + 1))
  printf '%s\\n' "$count" > "$count_file"
  printf '%s' "$count"
}
printf '%s\\n' "$*" >> "$FC_EPIC9_FAKE_ASPIRE_LOG"
case "$1" in
  ps)
    count="$(next_count "$FC_EPIC9_FAKE_PS_COUNT")"
    case ",\${FC_EPIC9_FAKE_PS_FAIL_CALLS:-}," in
      *",$count,"*) exit 96 ;;
    esac
    state="$(tr -d '\\r\\n' < "$FC_EPIC9_FAKE_STATE")"
    if [[ "$count" -eq "$FC_EPIC9_FAKE_UNRELATED_PS_CALL" ]]; then
      printf '[{"appHostPath":"/unrelated/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj","appHostPid":9999}]\\n'
    elif [[ "$state" == "running" ]]; then
      pid="\${FC_EPIC9_FAKE_PS_PID:-$(cat "$FC_EPIC9_FAKE_STATE.pid")}"
      if [[ "\${FC_EPIC9_FAKE_PS_PID_CHANGE_CALL:-0}" -gt 0 \
        && "$count" -ge "$FC_EPIC9_FAKE_PS_PID_CHANGE_CALL" ]]; then
        pid="$FC_EPIC9_FAKE_PS_PID_AFTER_CHANGE"
      fi
      printf '[{"appHostPath":"%s","appHostPid":%s}]\\n' "$FC_EPIC9_FAKE_APPHOST" "$pid"
    else
      printf '[]\\n'
    fi
    ;;
  start)
    parent_log=""
    previous=""
    for argument in "$@"; do
      if [[ "$previous" == "--log-file" ]]; then parent_log="$argument"; fi
      previous="$argument"
    done
    child_log="$FC_EPIC9_FAKE_STATE"_detach-child_fixture.log
    printf 'Spawning child CLI: aspire run --apphost %s --log-file %s --isolated --no-build\\n' "$FC_EPIC9_FAKE_APPHOST" "$child_log" > "$parent_log"
    for ((line=1; line<=200; line++)); do printf 'child-line-%s\\n' "$line"; done > "$child_log"
    printf 'AppHost exited before readiness with code 0\\n' >> "$child_log"
    printf 'password=fixture-password\\nauthorization=fixture-authorization\\nhttps://localhost/login?t=child-secret\\n' >> "$child_log"
    count="$(next_count "$FC_EPIC9_FAKE_START_COUNT")"
    pid="$FC_EPIC9_FAKE_FIRST_START_PID"
    if [[ "$count" -gt 1 ]]; then pid="$FC_EPIC9_FAKE_SECOND_START_PID"; fi
    printf '%s\\n' "$pid" > "$FC_EPIC9_FAKE_STATE.pid"
    if [[ "$count" -eq 1 && "\${FC_EPIC9_FAKE_FIRST_START_FAILS:-false}" == "true" ]]; then
      if [[ "\${FC_EPIC9_FAKE_PARTIAL_START:-false}" == "true" ]]; then
        printf 'running\\n' > "$FC_EPIC9_FAKE_STATE"
      fi
      printf 'start failed at https://localhost/login?t=secret\\n' >&2
      exit "$FC_EPIC9_FAKE_FIRST_START_EXIT_CODE"
    fi
    case ",\${FC_EPIC9_FAKE_INVALID_START_CALLS:-}," in
      *",$count,"*)
        if [[ "\${FC_EPIC9_FAKE_PARTIAL_START:-false}" == "true" ]]; then
          printf 'running\\n' > "$FC_EPIC9_FAKE_STATE"
        fi
        if [[ "$FC_EPIC9_FAKE_PROGRESS_ONLY" == "true" ]]; then
          printf 'Starting Aspire AppHost in the background...\\n'
        else
          printf 'Build failed on start call %s at https://localhost/login?t=secret\\n' "$count"
        fi
        exit 0
        ;;
    esac
    printf 'running\\n' > "$FC_EPIC9_FAKE_STATE"
    if [[ "$count" -eq 1 && "$FC_EPIC9_FAKE_FIRST_START_ABSENT" == "true" ]]; then
      printf 'stopped\\n' > "$FC_EPIC9_FAKE_STATE"
    fi
    printf '{"appHostPath":"%s","appHostPid":%s,"dashboardUrl":"https://localhost/login?t=secret"}\\n' "$FC_EPIC9_FAKE_APPHOST" "$pid"
    ;;
  stop)
    count="$(next_count "$FC_EPIC9_FAKE_STOP_COUNT")"
    if [[ "$count" -le "\${FC_EPIC9_FAKE_STOP_FAILURES:-0}" ]]; then exit 94; fi
    if [[ "$count" -gt "\${FC_EPIC9_FAKE_STOP_LEAVES_RUNNING_CALLS:-0}" ]]; then
      printf 'stopped\\n' > "$FC_EPIC9_FAKE_STATE"
    fi
    ;;
  wait)
    printf 'counter-web is up\\n'
    ;;
  describe)
    printf '{"resources":[{"name":"counter-web-proof","urls":[{"name":"https","url":"https://localhost:43210"}]}]}\\n'
    ;;
  logs)
    printf '{"logs":[{"resourceName":"counter-web","content":"Now listening on: https://localhost:43210","isError":false}]}\\n'
    ;;
  --version)
    printf '13.4.6\\n'
    ;;
  *)
    printf 'Unexpected lifecycle invocation: %s\\n' "$*" >&2
    exit 99
    ;;
esac
`);

  await writeExecutable(join(bin, 'dotnet'), `#!/usr/bin/env bash
set -euo pipefail
next_count() {
  local count_file="$1"
  local count=0
  if [[ -f "$count_file" ]]; then read -r count < "$count_file"; fi
  count=$((count + 1))
  printf '%s\\n' "$count" > "$count_file"
  printf '%s' "$count"
}
printf '%s\\n' "$*" >> "$FC_EPIC9_FAKE_DOTNET_LOG"
if [[ "$1" == "--version" ]]; then
  printf '10.0.302\\n'
elif [[ "$1" == "build" ]]; then
  count="$(next_count "$FC_EPIC9_FAKE_DOTNET_BUILD_COUNT")"
  if [[ "\${FC_EPIC9_FAKE_DOTNET_FAIL_BUILD_CALL:-0}" == "$count" ]]; then
    printf 'Serialized build failed on call %s.\\n' "$count" >&2
    exit 92
  fi
  printf 'Serialized build succeeded.\\n'
else
  exit 99
fi
`);

  await writeExecutable(join(bin, 'npm'), `#!/usr/bin/env bash
set -euo pipefail
printf '%s\\n' "$*" >> "$FC_EPIC9_FAKE_NPM_LOG"
if [[ "$1" != "run" ]]; then exit 99; fi
if [[ "$2" == "test:epic-9" ]]; then
  mkdir -p "$FC_E2E_OUTPUT_DIR/canonical-success"
  printf '{}\\n' > "$FC_E2E_OUTPUT_DIR/canonical-success/epic-9-command-evidence.json"
  printf 'PK\\003\\004canonical-trace\\n' > "$FC_E2E_OUTPUT_DIR/canonical-success/trace.zip"
  if [[ "\${FC_EPIC9_FAKE_RETRY_ARTIFACTS:-false}" == "true" ]]; then
    mkdir -p "$FC_E2E_OUTPUT_DIR/failed-attempt"
    printf 'PK\\003\\004failed-attempt-trace\\n' > "$FC_E2E_OUTPUT_DIR/failed-attempt/trace.zip"
  fi
fi
if [[ "$2" == "validate:epic-9-artifacts" && ! -s "$FC_EPIC9_ARTIFACT_ROOT/checksums.sha256" ]]; then
  printf 'validator was invoked before checksums.sha256 existed\\n' >&2
  exit 93
fi
`);

  const initialStatus = options.initialStatus ?? (options.dirty ? ' M story-owned-file\n' : '');
  const environment = {
    ...process.env,
    PATH: `${bin}:${process.env.PATH}`,
    FC_EPIC9_ARTIFACT_ROOT: artifactRoot,
    FC_EPIC9_EXPECTED_COMMIT: options.expectedCandidate ?? CANDIDATE,
    FC_EPIC9_REQUIRE_CLEAN: options.requireClean === false ? 'false' : 'true',
    FC_EPIC9_LOCK_ROOT: root,
    FC_EPIC9_FAKE_REPOSITORY_ROOT: REPOSITORY_ROOT,
    FC_EPIC9_FAKE_APPHOST: APPHOST,
    FC_EPIC9_FAKE_INITIAL_HEAD: options.initialHead ?? CANDIDATE,
    FC_EPIC9_FAKE_FINAL_HEAD: options.finalHead ?? options.initialHead ?? CANDIDATE,
    FC_EPIC9_FAKE_INITIAL_STATUS: initialStatus,
    FC_EPIC9_FAKE_FINAL_STATUS: options.finalStatus ?? initialStatus,
    FC_EPIC9_FAKE_HEAD_FAIL_CALL: String(options.headFailCall ?? 0),
    FC_EPIC9_FAKE_STATUS_FAIL_CALL: String(options.statusFailCall ?? 0),
    FC_EPIC9_FAKE_HEAD_COUNT: join(root, 'git-head-count'),
    FC_EPIC9_FAKE_STATUS_COUNT: join(root, 'git-status-count'),
    FC_EPIC9_FAKE_ASPIRE_LOG: aspireLog,
    FC_EPIC9_FAKE_STATE: stateFile,
    FC_EPIC9_FAKE_PS_COUNT: join(root, 'aspire-ps-count'),
    FC_EPIC9_FAKE_START_COUNT: join(root, 'aspire-start-count'),
    FC_EPIC9_FAKE_STOP_COUNT: join(root, 'aspire-stop-count'),
    FC_EPIC9_FAKE_FIRST_START_FAILS: String(options.firstStartFails ?? false),
    FC_EPIC9_FAKE_FIRST_START_EXIT_CODE: String(options.firstStartExitCode ?? 95),
    FC_EPIC9_FAKE_INVALID_START_CALLS: options.invalidStartCalls?.join(',') ?? '',
    FC_EPIC9_FAKE_PROGRESS_ONLY: String(options.progressOnly ?? false),
    FC_EPIC9_FAKE_PARTIAL_START: String(options.partialStart ?? false),
    FC_EPIC9_FAKE_UNRELATED_PS_CALL: String(options.unrelatedPsCall ?? (options.unrelated ? 1 : 0)),
    FC_EPIC9_FAKE_PS_FAIL_CALLS: options.psFailCalls?.join(',') ?? '',
    FC_EPIC9_FAKE_PS_PID: options.psPid === undefined ? '' : String(options.psPid),
    FC_EPIC9_FAKE_FIRST_START_PID: String(options.startPids?.[0] ?? 4321),
    FC_EPIC9_FAKE_SECOND_START_PID: String(options.startPids?.[1] ?? 4321),
    FC_EPIC9_FAKE_FIRST_START_ABSENT: String(options.firstStartAbsent ?? false),
    FC_EPIC9_FAKE_PS_PID_CHANGE_CALL: String(options.psPidChangeCall ?? 0),
    FC_EPIC9_FAKE_PS_PID_AFTER_CHANGE: String(options.psPidAfterChange ?? 9999),
    FC_EPIC9_FAKE_STOP_FAILURES: String(options.stopFailures ?? 0),
    FC_EPIC9_FAKE_STOP_LEAVES_RUNNING_CALLS: String(options.stopLeavesRunningCalls ?? 0),
    FC_EPIC9_FAKE_DOTNET_LOG: dotnetLog,
    FC_EPIC9_FAKE_DOTNET_BUILD_COUNT: join(root, 'dotnet-build-count'),
    FC_EPIC9_FAKE_DOTNET_FAIL_BUILD_CALL: String(options.dotnetFailBuildCall ?? 0),
    FC_EPIC9_FAKE_NPM_LOG: npmLog,
    FC_EPIC9_FAKE_RETRY_ARTIFACTS: String(options.retryArtifacts ?? false),
  };
  return { artifactRoot, aspireLog, dotnetLog, environment, npmLog };
};

const runProof = async (environment) => new Promise((resolvePromise, rejectPromise) => {
  const child = spawn(PROOF_SCRIPT, [], {
    cwd: REPOSITORY_ROOT,
    env: environment,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let stdout = '';
  let stderr = '';
  child.stdout.setEncoding('utf8');
  child.stderr.setEncoding('utf8');
  child.stdout.on('data', (chunk) => { stdout += chunk; });
  child.stderr.on('data', (chunk) => { stderr += chunk; });
  child.once('error', rejectPromise);
  child.once('close', (exitCode) => resolvePromise({ exitCode, stdout, stderr }));
});

const readInvocations = async (path) => {
  try {
    const content = await readFile(path, 'utf8');
    return content.trim().length === 0 ? [] : content.trim().split('\n');
  } catch (error) {
    if (error?.code === 'ENOENT') return [];
    throw error;
  }
};

const lifecycleNames = (invocations) => invocations.map((invocation) => invocation.split(' ')[0]);

test('Epic 9 strict proof rejects a dirty candidate before Aspire is invoked', async (t) => {
  const harness = await createHarness(t, { dirty: true });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /candidate preflight failed/u);
  assert.deepEqual(await readInvocations(harness.aspireLog), []);
  const failure = JSON.parse(await readFile(join(harness.artifactRoot, 'candidate-preflight.failed.json'), 'utf8'));
  assert.equal(failure.workingTreeDirty, true);
  assert.equal(failure.evidenceMode, 'final');
});

test('Epic 9 proof rejects an expected-SHA mismatch before Aspire is invoked', async (t) => {
  const harness = await createHarness(t, { expectedCandidate: OTHER_CANDIDATE });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /candidate preflight failed/u);
  assert.deepEqual(await readInvocations(harness.aspireLog), []);
});

test('Epic 9 proof fails closed when Git status fails before Aspire is invoked', async (t) => {
  const harness = await createHarness(t, { statusFailCall: 1 });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /working-tree discovery failed closed/u);
  assert.deepEqual(await readInvocations(harness.aspireLog), []);
});

test('Epic 9 proof leaves an unrelated FrontComposer AppHost untouched', async (t) => {
  const harness = await createHarness(t, { unrelated: true });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /already running/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps']);
});

test('Epic 9 proof completes a correlated lifecycle and validates only after checksums exist', async (t) => {
  const harness = await createHarness(t);
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 0, result.stderr);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), [
    'ps', 'ps', 'start', 'ps', 'wait', 'describe', '--version', 'logs', 'ps', 'stop', 'ps',
  ]);
  assert.deepEqual(await readInvocations(harness.npmLog), [
    'run test:epic-9',
    `run validate:epic-9-artifacts -- ${harness.artifactRoot} --candidate ${CANDIDATE}`,
  ]);
  assert.deepEqual(
    (await readInvocations(harness.dotnetLog)).filter((invocation) => invocation.startsWith('build ')),
    [EXPECTED_SOURCE_GRAPH_BUILD],
  );
  await access(join(harness.artifactRoot, 'checksums.sha256'));
});

test('Epic 9 proof removes failed retry artifacts after a successful attempt', async (t) => {
  const harness = await createHarness(t, { retryArtifacts: true });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 0, result.stderr);
  await access(join(harness.artifactRoot, 'playwright-results', 'canonical-success', 'trace.zip'));
  await assert.rejects(
    access(join(harness.artifactRoot, 'playwright-results', 'failed-attempt')),
    (error) => error?.code === 'ENOENT',
  );
});

for (const [name, mutation] of [
  ['HEAD', { finalHead: OTHER_CANDIDATE }],
  ['working tree', { finalStatus: ' M changed-during-proof\n' }],
]) {
  test(`Epic 9 proof rejects a mid-run ${name} mutation and cleans its owned AppHost`, async (t) => {
    const harness = await createHarness(t, mutation);
    const result = await runProof(harness.environment);
    const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

    assert.equal(result.exitCode, 2);
    assert.match(result.stderr, /candidate integrity check failed/u);
    assert.deepEqual(invocations.slice(-2), ['ps', 'stop']);
    assert.equal(invocations.filter((name_) => name_ === 'stop').length, 1);
    assert.deepEqual(await readInvocations(harness.npmLog), ['run test:epic-9']);
  });
}

test('Epic 9 proof cleans and refuses a partial AppHost left by a failed first start', async (t) => {
  const harness = await createHarness(t, { firstStartFails: true, partialStart: true });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /partial FrontComposer AppHost/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), [
    'ps', 'ps', 'start', 'ps', 'stop', 'ps',
  ]);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_SOURCE_GRAPH_BUILD]);
});

test('Epic 9 proof stops before Aspire when the complete source-graph prebuild fails', async (t) => {
  const harness = await createHarness(t, { dotnetFailBuildCall: 1 });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /Complete AppHost source-graph prebuild failed/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps']);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_SOURCE_GRAPH_BUILD]);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
  await access(join(harness.artifactRoot, 'apphost-source-graph-build.log'));
});

test('Epic 9 proof records the exact serialized complete-graph build and isolated no-build start', async (t) => {
  const harness = await createHarness(t);
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 0, result.stderr);
  const starts = (await readInvocations(harness.aspireLog)).filter((invocation) => invocation.startsWith('start '));
  assert.equal(starts.length, 1);
  assert.match(starts[0], /--isolated --no-build --non-interactive --format Json --nologo --log-file /u);
  assert.deepEqual((await readInvocations(harness.dotnetLog)).filter((invocation) => invocation.startsWith('build ')), [EXPECTED_SOURCE_GRAPH_BUILD]);
  const metadata = JSON.parse(await readFile(join(harness.artifactRoot, 'runtime-metadata.json'), 'utf8'));
  assert.equal(metadata.startMode, 'isolated-no-build-after-source-graph-build');
  assert.equal(metadata.commands[1], EXPECTED_SOURCE_GRAPH_BUILD.replace(APPHOST, 'src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj').replace(/^build /u, 'dotnet build '));
  assert.equal(metadata.commands[3], 'aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --no-build --non-interactive --format Json --nologo --log-file <temporary-cli-log>');
  assert.doesNotMatch(metadata.commands.join(' '), /BuildProjectReferences=false/u);
});

for (const exitCode of [3, 95]) {
  test(`Epic 9 proof fails closed after CLI exit ${exitCode} without relaunching`, async (t) => {
    const harness = await createHarness(t, { firstStartFails: true, firstStartExitCode: exitCode });
    const result = await runProof(harness.environment);
    assert.equal(result.exitCode, 2);
    assert.match(result.stderr, /Prebuilt AppHost start failed/u);
    assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps', 'start', 'ps']);
    assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_SOURCE_GRAPH_BUILD]);
    assert.deepEqual(await readInvocations(harness.npmLog), []);
  });
}

test('Epic 9 proof retains bounded redacted child diagnostics for the hosted zero-exit progress-only failure', async (t) => {
  const harness = await createHarness(t, { invalidStartCalls: [1], progressOnly: true });
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /valid JSON object/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps', 'start', 'ps']);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
  const failure = await readFile(join(harness.artifactRoot, 'apphost-start.failed.json'), 'utf8');
  assert.match(failure, /Starting Aspire AppHost in the background/u);
  assert.match(failure, /Detached child startup log/u);
  assert.match(failure, /AppHost exited before readiness with code 0/u);
  assert.match(failure, /login\?t=\[REDACTED\]/u);
  assert.doesNotMatch(failure, /child-secret|fixture-password|fixture-authorization|child-line-1\b/u);
  assert.ok(failure.split('\n').length <= 164, 'Only bounded parent/child excerpts may be retained');
  const start = (await readInvocations(harness.aspireLog)).find((invocation) => invocation.startsWith('start '));
  const rawLog = start.split('--log-file ')[1];
  await assert.rejects(access(rawLog), (error) => error?.code === 'ENOENT');
});

test('Epic 9 proof cleans a partial AppHost after an invalid zero-exit start without relaunching', async (t) => {
  const harness = await createHarness(t, { invalidStartCalls: [1], partialStart: true });
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /partial FrontComposer AppHost/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps', 'start', 'ps', 'stop', 'ps']);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_SOURCE_GRAPH_BUILD]);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
});

test('Epic 9 proof retries Aspire stop from EXIT when normal cleanup fails', async (t) => {
  const harness = await createHarness(t, { stopFailures: 1 });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /EXIT trap will recheck/u);
  assert.equal(invocations.filter((name) => name === 'stop').length, 2);
  assert.deepEqual(invocations.slice(-3), ['stop', 'ps', 'stop']);
});

test('Epic 9 proof retries Aspire stop from EXIT when postflight discovery fails', async (t) => {
  const harness = await createHarness(t, { psFailCalls: [5], stopLeavesRunningCalls: 1 });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /postflight failed/u);
  assert.equal(invocations.filter((name) => name === 'stop').length, 2);
  assert.deepEqual(invocations.slice(-4), ['stop', 'ps', 'ps', 'stop']);
});

test('Epic 9 proof does not stop a replacement PID after postflight discovery fails', async (t) => {
  const harness = await createHarness(t, {
    psFailCalls: [5],
    stopLeavesRunningCalls: 1,
    psPidChangeCall: 6,
    psPidAfterChange: 9999,
  });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /postflight failed/u);
  assert.equal(invocations.filter((name) => name === 'stop').length, 1);
  assert.deepEqual(invocations.slice(-3), ['stop', 'ps', 'ps']);
});

test('Epic 9 proof refuses to proceed or stop when process ownership does not match start JSON', async (t) => {
  const harness = await createHarness(t, { psPid: 9999 });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /did not uniquely correlate/u);
  assert.equal(invocations.filter((name) => name === 'stop').length, 0);
});

test('Epic 9 proof rechecks ownership after failed discovery and never acquires another start PID', async (t) => {
  const harness = await createHarness(t, { startPids: [4321, 8765], firstStartAbsent: true, psFailCalls: [3] });
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 2);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps', 'start', 'ps', 'ps']);
  assert.equal((await readFile(`${harness.environment.FC_EPIC9_FAKE_STATE}.pid`, 'utf8')).trim(), '4321');
  assert.deepEqual(await readInvocations(harness.npmLog), []);
});

test('Epic 9 proof refuses an unrelated run that appears during the source-graph build', async (t) => {
  const harness = await createHarness(t, { unrelatedPsCall: 2 });
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /appeared during the source-graph build/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps']);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_SOURCE_GRAPH_BUILD]);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
});

test('Epic 9 proof fails closed when post-build discovery fails before startup', async (t) => {
  const harness = await createHarness(t, { psFailCalls: [2] });
  const result = await runProof(harness.environment);
  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /process discovery failed closed/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'ps']);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
});

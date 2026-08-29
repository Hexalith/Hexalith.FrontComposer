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
const EVENTSTORE_ASPIRE = join(
  REPOSITORY_ROOT,
  'references',
  'Hexalith.EventStore',
  'src',
  'Hexalith.EventStore.Aspire',
  'Hexalith.EventStore.Aspire.csproj',
);
const EXPECTED_DEPENDENCY_BUILD = `build ${EVENTSTORE_ASPIRE} --configuration Debug -m:1 -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
const EXPECTED_APPHOST_BUILD = `build ${APPHOST} --configuration Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
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
    if [[ "$count" -eq 1 && "\${FC_EPIC9_FAKE_UNRELATED:-false}" == "true" ]]; then
      printf '[{"appHostPath":"/unrelated/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj","appHostPid":9999}]\\n'
    elif [[ "$state" == "running" ]]; then
      pid="\${FC_EPIC9_FAKE_PS_PID:-4321}"
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
    count="$(next_count "$FC_EPIC9_FAKE_START_COUNT")"
    if [[ "$count" -eq 1 && "\${FC_EPIC9_FAKE_FIRST_START_FAILS:-false}" == "true" ]]; then
      if [[ "\${FC_EPIC9_FAKE_PARTIAL_START:-false}" == "true" ]]; then
        printf 'running\\n' > "$FC_EPIC9_FAKE_STATE"
      fi
      printf 'start failed at https://localhost/login?t=secret\\n' >&2
      exit 95
    fi
    printf 'running\\n' > "$FC_EPIC9_FAKE_STATE"
    printf '{"appHostPath":"%s","appHostPid":4321,"dashboardUrl":"https://localhost/login?t=secret"}\\n' "$FC_EPIC9_FAKE_APPHOST"
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
    FC_EPIC9_FAKE_PARTIAL_START: String(options.partialStart ?? false),
    FC_EPIC9_FAKE_UNRELATED: String(options.unrelated ?? false),
    FC_EPIC9_FAKE_PS_FAIL_CALLS: options.psFailCalls?.join(',') ?? '',
    FC_EPIC9_FAKE_PS_PID: String(options.psPid ?? 4321),
    FC_EPIC9_FAKE_PS_PID_CHANGE_CALL: String(options.psPidChangeCall ?? 0),
    FC_EPIC9_FAKE_PS_PID_AFTER_CHANGE: String(options.psPidAfterChange ?? 9999),
    FC_EPIC9_FAKE_STOP_FAILURES: String(options.stopFailures ?? 0),
    FC_EPIC9_FAKE_STOP_LEAVES_RUNNING_CALLS: String(options.stopLeavesRunningCalls ?? 0),
    FC_EPIC9_FAKE_DOTNET_LOG: dotnetLog,
    FC_EPIC9_FAKE_DOTNET_BUILD_COUNT: join(root, 'dotnet-build-count'),
    FC_EPIC9_FAKE_DOTNET_FAIL_BUILD_CALL: String(options.dotnetFailBuildCall ?? 0),
    FC_EPIC9_FAKE_NPM_LOG: npmLog,
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
    'ps', 'start', 'ps', 'wait', 'describe', '--version', 'logs', 'ps', 'stop', 'ps',
  ]);
  assert.deepEqual(await readInvocations(harness.npmLog), [
    'run test:epic-9',
    `run validate:epic-9-artifacts -- ${harness.artifactRoot} --candidate ${CANDIDATE}`,
  ]);
  await access(join(harness.artifactRoot, 'checksums.sha256'));
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
    'ps', 'start', 'ps', 'stop', 'ps',
  ]);
  assert.deepEqual(await readInvocations(harness.dotnetLog), []);
});

test('Epic 9 proof uses the serialized-build fallback only after failed-start postflight is absent', async (t) => {
  const harness = await createHarness(t, { firstStartFails: true });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 0, result.stderr);
  assert.deepEqual(invocations.slice(0, 5), ['ps', 'start', 'ps', 'start', 'ps']);
  assert.equal(invocations.filter((name) => name === 'start').length, 2);
  const fallbackBuilds = (await readInvocations(harness.dotnetLog))
    .filter((invocation) => invocation.startsWith('build '));
  assert.deepEqual(fallbackBuilds, [EXPECTED_DEPENDENCY_BUILD, EXPECTED_APPHOST_BUILD]);
  assert.match(invocations.join(' '), /stop/u);
});

test('Epic 9 proof stops before the AppHost build and fallback start when the dependency build fails', async (t) => {
  const harness = await createHarness(t, { firstStartFails: true, dotnetFailBuildCall: 1 });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /Serialized AppHost fallback build failed/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'start', 'ps']);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [EXPECTED_DEPENDENCY_BUILD]);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
  await access(join(harness.artifactRoot, 'apphost-start.failed.json'));
  assert.match(
    await readFile(join(harness.artifactRoot, 'apphost-serialized-build.log'), 'utf8'),
    /Serialized build failed on call 1\./u,
  );
  await assert.rejects(
    access(join(harness.artifactRoot, 'apphost-start.json')),
    (error) => error?.code === 'ENOENT',
  );
});

test('Epic 9 proof stops before the fallback start when the AppHost build fails', async (t) => {
  const harness = await createHarness(t, { firstStartFails: true, dotnetFailBuildCall: 2 });
  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /Serialized AppHost fallback build failed/u);
  assert.deepEqual(lifecycleNames(await readInvocations(harness.aspireLog)), ['ps', 'start', 'ps']);
  assert.deepEqual(await readInvocations(harness.dotnetLog), [
    EXPECTED_DEPENDENCY_BUILD,
    EXPECTED_APPHOST_BUILD,
  ]);
  assert.deepEqual(await readInvocations(harness.npmLog), []);
  await access(join(harness.artifactRoot, 'apphost-start.failed.json'));
  assert.match(
    await readFile(join(harness.artifactRoot, 'apphost-serialized-build.log'), 'utf8'),
    /Serialized build failed on call 2\./u,
  );
  await assert.rejects(
    access(join(harness.artifactRoot, 'apphost-start.json')),
    (error) => error?.code === 'ENOENT',
  );
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
  const harness = await createHarness(t, { psFailCalls: [4], stopLeavesRunningCalls: 1 });
  const result = await runProof(harness.environment);
  const invocations = lifecycleNames(await readInvocations(harness.aspireLog));

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /postflight failed/u);
  assert.equal(invocations.filter((name) => name === 'stop').length, 2);
  assert.deepEqual(invocations.slice(-4), ['stop', 'ps', 'ps', 'stop']);
});

test('Epic 9 proof does not stop a replacement PID after postflight discovery fails', async (t) => {
  const harness = await createHarness(t, {
    psFailCalls: [4],
    stopLeavesRunningCalls: 1,
    psPidChangeCall: 5,
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

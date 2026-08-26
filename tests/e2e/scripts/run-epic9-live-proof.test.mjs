import assert from 'node:assert/strict';
import { access, chmod, mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
import test from 'node:test';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const PROOF_SCRIPT = join(REPOSITORY_ROOT, 'eng', 'run-epic9-live-proof.sh');
const CANDIDATE = '1234567890abcdef1234567890abcdef12345678';

const writeExecutable = async (path, content) => {
  await writeFile(path, content);
  await chmod(path, 0o755);
};

const createHarness = async (t, { dirty, processList }) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-epic9-runner-'));
  const bin = join(root, 'bin');
  const artifactRoot = join(root, 'artifacts');
  const aspireLog = join(root, 'aspire.log');
  await mkdir(bin, { recursive: true });
  t.after(() => rm(root, { recursive: true, force: true }));

  await writeExecutable(join(bin, 'git'), `#!/usr/bin/env bash
set -euo pipefail
if [[ "$1" == "rev-parse" && "$2" == "--show-toplevel" ]]; then
  printf '%s\\n' "$FC_EPIC9_FAKE_REPOSITORY_ROOT"
elif [[ "$1" == "-C" && "$3" == "rev-parse" && "$4" == "HEAD" ]]; then
  printf '%s\\n' "$FC_EPIC9_FAKE_CANDIDATE"
elif [[ "$1" == "-C" && "$3" == "status" && "$4" == "--porcelain" ]]; then
  printf '%s' "$FC_EPIC9_FAKE_STATUS"
else
  printf 'Unexpected fake git invocation: %s\\n' "$*" >&2
  exit 98
fi
`);
  await writeExecutable(join(bin, 'aspire'), `#!/usr/bin/env bash
set -euo pipefail
printf '%s\\n' "$*" >> "$FC_EPIC9_FAKE_ASPIRE_LOG"
if [[ "$1" == "ps" ]]; then
  printf '%s\\n' "$FC_EPIC9_FAKE_PROCESS_LIST"
else
  printf 'Unexpected lifecycle invocation: %s\\n' "$*" >&2
  exit 99
fi
`);

  const environment = {
    ...process.env,
    PATH: `${bin}:${process.env.PATH}`,
    FC_EPIC9_ARTIFACT_ROOT: artifactRoot,
    FC_EPIC9_EXPECTED_COMMIT: CANDIDATE,
    FC_EPIC9_FAKE_REPOSITORY_ROOT: REPOSITORY_ROOT,
    FC_EPIC9_FAKE_CANDIDATE: CANDIDATE,
    FC_EPIC9_FAKE_STATUS: dirty ? ' M story-owned-file\n' : '',
    FC_EPIC9_FAKE_ASPIRE_LOG: aspireLog,
    FC_EPIC9_FAKE_PROCESS_LIST: JSON.stringify(processList),
  };
  return { artifactRoot, aspireLog, environment };
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

test('Epic 9 strict proof rejects a dirty candidate before Aspire is invoked', async (t) => {
  const harness = await createHarness(t, { dirty: true, processList: [] });
  harness.environment.FC_EPIC9_REQUIRE_CLEAN = 'true';

  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /candidate preflight failed/u);
  await assert.rejects(access(harness.aspireLog));
  const failure = JSON.parse(await readFile(join(harness.artifactRoot, 'candidate-preflight.failed.json'), 'utf8'));
  assert.equal(failure.candidateCommit, CANDIDATE);
  assert.equal(failure.expectedCandidate, CANDIDATE);
  assert.equal(failure.workingTreeDirty, true);
  assert.equal(failure.evidenceMode, 'final');
});

test('Epic 9 proof leaves an unrelated FrontComposer AppHost untouched', async (t) => {
  const harness = await createHarness(t, {
    dirty: false,
    processList: [{ appHostPath: '/unrelated/Hexalith.FrontComposer.AppHost.csproj' }],
  });
  harness.environment.FC_EPIC9_REQUIRE_CLEAN = 'false';

  const result = await runProof(harness.environment);

  assert.equal(result.exitCode, 2);
  assert.match(result.stderr, /already running/u);
  const lifecycleInvocations = (await readFile(harness.aspireLog, 'utf8')).trim().split('\n');
  assert.equal(lifecycleInvocations.length, 1);
  assert.match(lifecycleInvocations[0], /^ps /u);
  assert.doesNotMatch(lifecycleInvocations[0], /\b(?:start|stop)\b/u);
});

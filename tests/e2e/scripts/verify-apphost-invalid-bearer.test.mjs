import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { chmod, mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
import test from 'node:test';

const repository = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const script = join(repository, 'eng/verify-apphost-invalid-bearer.sh');
const apphost = join(repository, 'src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj');
const paths = [
  '/api/v1/commands',
  '/api/v1/commands/status/01K8AAAAAAAAAAAAAAAAAAAAAA',
  '/api/v1/queries',
  '/hubs/projection-changes/negotiate?negotiateVersion=1',
];


const bounded = async (promise, milliseconds, label) => {
  let timer;
  try {
    return await Promise.race([promise, new Promise((_, reject) => {
      timer = setTimeout(() => reject(new Error(`${label} timed out`)), milliseconds);
    })]);
  } finally {
    clearTimeout(timer);
  }
};

const lockAvailable = async (path) => {
  const child = spawn('flock', ['-n', path, 'true'], { stdio: 'ignore' });
  try {
    return await bounded(new Promise((resolveExit, reject) => {
      child.once('error', reject);
      child.once('close', code => resolveExit(code === 0));
    }), 5_000, 'Lock availability probe');
  } finally {
    if (child.exitCode === null && child.signalCode === null) child.kill('SIGKILL');
  }
};

const holdLock = async (t, path) => {
  // --no-fork leaves one owned process to terminate if readiness or teardown fails.
  const child = spawn('flock', ['--no-fork', '-x', path, 'bash', '-c', 'printf ready; read -r -t 15 release'], {
    stdio: ['pipe', 'pipe', 'pipe'],
  });
  child.stdin.on('error', () => {});
  const exited = new Promise(resolveExit => child.once('close', resolveExit));
  t.after(async () => {
    child.stdin.end('release\n');
    try {
      await bounded(exited, 2_000, 'Lock holder teardown');
    } catch (error) {
      child.kill('SIGKILL');
      await bounded(exited, 2_000, 'Lock holder termination');
      throw error;
    }
  });
  await bounded(new Promise((ready, reject) => {
    child.once('error', reject);
    child.stdout.once('data', chunk => {
      if (chunk.toString() === 'ready') ready();
      else reject(new Error('Unexpected lock holder readiness output'));
    });
    child.once('exit', code => reject(new Error(`Lock holder exited before release: ${code}`)));
  }), 5_000, 'Lock holder readiness');
};

async function run(t, options = {}) {
  const root = await mkdtemp(join(tmpdir(), 'fc-bearer-test-'));
  const survivorPidFile = join(root, 'survivor.pid');
  const flockLog = join(root, 'flock.log');
  const cleanupLockLog = join(root, 'cleanup-lock.log');
  const lockFile = join(root, `hexalith-epic9-${createHash('sha256').update(apphost).digest('hex')}.lock`);
  t.after(async () => {
    try {
      process.kill(Number(await readFile(survivorPidFile, 'utf8')), 'SIGTERM');
    } catch (error) {
      if (!['ENOENT', 'ESRCH'].includes(error.code)) throw error;
    }
    await rm(root, { recursive: true, force: true });
  });
  const bin = join(root, 'bin');
  const scratch = join(root, 'scratch');
  await mkdir(bin);
  await mkdir(scratch);
  const state = join(root, 'state.json');
  await writeFile(state, JSON.stringify({ running: options.existing ?? false, pid: 4321, calls: [], ps: 0 }));
  const executable = async (name, source) => {
    await writeFile(join(bin, `${name}.mjs`), source);
    const survivor = name === 'aspire' && options.survivingChild ? `
if [[ "$1" == start ]]; then
  sleep 60 </dev/null >/dev/null 2>&1 &
  printf '%s\\n' "$!" > "$BEARER_TEST_SURVIVOR_PID_FILE"
fi
` : '';
    await writeFile(join(bin, name), '#!/usr/bin/env bash\n' + survivor + 'exec node "${BASH_SOURCE[0]}.mjs" "$@"\n');
    await chmod(join(bin, name), 0o755);
  };
  await writeFile(join(bin, 'flock'), `#!/usr/bin/env bash
printf '%s\\n' "$*" >> "$BEARER_TEST_FLOCK_LOG"
if [[ "$1" == -u && "$BEARER_TEST_REJECT_UNLOCK" == true ]]; then exit 77; fi
PATH="$BEARER_TEST_ORIGINAL_PATH" exec flock "$@"
`);
  await chmod(join(bin, 'flock'), 0o755);
  const prelude = `
import { readFileSync, writeFileSync, appendFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
const options = JSON.parse(process.env.BEARER_TEST_OPTIONS);
const statePath = process.env.BEARER_TEST_STATE;
const state = JSON.parse(readFileSync(statePath, 'utf8'));
const apphost = process.env.BEARER_TEST_APPHOST;
const args = process.argv.slice(2);
const save = () => writeFileSync(statePath, JSON.stringify(state));
const privateText = 'authorization=private-bearer password=private-password https://localhost/login?t=private-token';
`;
  await executable('aspire', prelude + `
state.calls.push(['aspire', ...args]);
if (process.env.HexalithFrontComposerFromSource !== 'true') process.exit(91);
const identity = () => ({ appHostPath: apphost, appHostPid: state.pid });
if (options.survivingChild && ['ps', 'stop'].includes(args[0])) {
  const probe = spawnSync('flock', ['-n', process.env.BEARER_TEST_LOCK_FILE, 'true'], {
    env: { ...process.env, PATH: process.env.BEARER_TEST_ORIGINAL_PATH }, stdio: 'ignore', timeout: 2_000,
  });
  appendFileSync(process.env.BEARER_TEST_CLEANUP_LOCK_LOG, args[0] + ' ' + probe.status + '\\n');
  if (probe.status !== 1) { save(); process.exit(97); }
}
switch (args[0]) {
  case 'ps':
    state.ps++;
    if (options.pidChanged && state.ps >= 3) state.pid = 9999;
    save();
    if (options.psFailure === state.ps) { console.error(privateText); process.exit(9); }
    console.log(JSON.stringify(state.running ? [identity()] : []));
    break;
  case 'start':
    state.running = true;
    save();
    if (options.startFailure) { console.error(privateText); process.exit(8); }
    if (options.startMalformed) { console.log(privateText); break; }
    console.log('Starting Aspire AppHost in the background...');
    console.log(JSON.stringify({ ...identity(), ...(options.identityMismatch ? { appHostPid: 9999 } : {}), dashboardUrl: privateText }));
    break;
  case 'wait':
    save();
    if (options.signal) process.kill(process.ppid, options.signal);
    if (options.waitFailure) { console.error(privateText); process.exit(7); }
    break;
  case 'describe':
    save();
    if (options.discovery === 'failure') { console.error(privateText); process.exit(6); }
    if (options.discovery === 'malformed') { console.log(privateText); break; }
    console.log(JSON.stringify({ resources: [{ name: 'eventstore-owned', displayName: 'eventstore', urls: options.discovery === 'empty' ? [] : [
      { name: 'https', url: 'https://localhost:5443' },
      { name: 'http', url: 'http://127.0.0.1:5080' },
      { name: 'management', url: 'http://localhost:9990' },
      { name: 'private', url: 'https://private:password@example.invalid/?token=private-token' },
    ], environment: { secret: privateText } }] }));
    break;
  case 'stop':
    if (!options.stopFailure) state.running = false;
    save();
    if (options.stopFailure) { console.error(privateText); process.exit(5); }
    break;
  default: save(); process.exit(92);
}
`);
  await executable('curl', prelude + `
state.calls.push(['curl', ...args]); save();
if (args[0] !== '--disable' || args[args.indexOf('--noproxy') + 1] !== '*' || args.includes('--location') || args.includes('-L')) process.exit(93);
if (!args.includes('Authorization: Bearer invalid-ci-token')) process.exit(94);
const url = args.at(-1);
if (url.includes(':9990') || url.includes('example.invalid')) process.exit(95);
const transportFailure = options.unreachable || (options.fallback && url.startsWith('https:'));
if (transportFailure) { console.error(privateText); process.stdout.write('000'); process.exit(28); }
process.stdout.write(String(options.httpStatus ?? 401));
if (options.partialResponse) { console.error(privateText); process.exit(56); }
`);
  await executable('python3', prelude + `
state.calls.push(['python3', ...args]); save();
if (args[1] !== 'check') process.exit(96);
if (options.readinessFailure) { console.error(privateText); process.exit(1); }
`);
  await options.beforeRun?.(lockFile);
  const child = spawn('bash', [script], {
    cwd: repository,
    env: {
      ...process.env,
      PATH: `${bin}:${process.env.PATH}`,
      TMPDIR: scratch,
      FC_EPIC9_LOCK_ROOT: root,
      BEARER_TEST_FLOCK_LOG: flockLog,
      BEARER_TEST_CLEANUP_LOCK_LOG: cleanupLockLog,
      BEARER_TEST_LOCK_FILE: lockFile,
      BEARER_TEST_REJECT_UNLOCK: String(options.rejectUnlock ?? false),
      BEARER_TEST_ORIGINAL_PATH: process.env.PATH,
      BEARER_TEST_SURVIVOR_PID_FILE: survivorPidFile,
      BEARER_TEST_STATE: state,
      BEARER_TEST_OPTIONS: JSON.stringify(options),
      BEARER_TEST_APPHOST: apphost,
      HTTPS_PROXY: 'http://proxy.invalid:8888',
      HTTP_PROXY: 'http://proxy.invalid:8888',
      NO_PROXY: '',
      FrontComposerVerification__Enabled: options.verification ? 'true' : 'false',
      FrontComposerVerification__Directory: options.verification ? root : '',

    },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let output = '';
  child.stdout.on('data', chunk => { output += chunk; });
  child.stderr.on('data', chunk => { output += chunk; });
  const timer = setTimeout(() => child.kill('SIGKILL'), 20_000);
  const code = await new Promise((resolveExit, reject) => {
    child.on('error', reject);
    child.on('close', resolveExit);
  });
  clearTimeout(timer);
  const final = JSON.parse(await readFile(state, 'utf8'));
  assert.deepEqual(await readdir(scratch), [], 'private scratch files must be removed');
  for (const secret of ['private-bearer', 'private-password', 'private-token', 'example.invalid']) {
    assert.ok(!output.includes(secret), `diagnostics leaked ${secret}`);
  }
  return { code, output, lockFile, flockLog, survivorPidFile, cleanupLockLog, ...final, probes: final.calls.filter(call => call[0] === 'curl') };
}

for (const httpStatus of [401, 403]) {
  test(`all four endpoints require completed HTTP ${httpStatus}, readiness and owned cleanup`, async t => {
    const result = await run(t, { httpStatus });
    assert.equal(result.code, 0, result.output);
    assert.equal(result.probes.length, 4);
    assert.deepEqual(result.probes.map(probe => new URL(probe.at(-1)).pathname + new URL(probe.at(-1)).search), paths);
    assert.deepEqual(result.probes.map(probe => probe[probe.indexOf('--request') + 1]), ['POST', 'GET', 'POST', 'POST']);
    assert.deepEqual(result.calls.filter(call => call[1] === 'wait').map(call => call[2]), ['security', 'eventstore']);
    assert.ok(result.calls.filter(call => call[1] === 'wait').every(call => call.includes('healthy')));
    assert.equal(result.calls.filter(call => call[1] === 'stop').length, 1);
    assert.equal(result.running, false);
  });
}

test('only transport failure permits trying the next advertised endpoint', async t => {
  const result = await run(t, { fallback: true });
  assert.equal(result.code, 0, result.output);
  assert.equal(result.probes.length, 8);
  assert.match(result.output, /curl exit 28 \(request timed out\); no HTTP response/);
  assert.equal(result.running, false);
});

test('all-unreachable endpoints fail every path without an authentication result', async t => {
  const result = await run(t, { unreachable: true });
  assert.equal(result.code, 1);
  assert.equal(result.probes.length, 8);
  assert.equal(result.output.match(/all advertised endpoints were unreachable/g)?.length, 4);
  assert.equal(result.running, false);
});

for (const httpStatus of [200, 302, 404, 500]) {
  test(`HTTP ${httpStatus} fails without fallback or following redirects`, async t => {
    const result = await run(t, { httpStatus });
    assert.equal(result.code, 1);
    assert.equal(result.probes.length, 4);
    assert.match(result.output, new RegExp(`returned HTTP ${httpStatus}`));
    assert.equal(result.running, false);
  });
}

test('a 401 header with an incomplete transport does not count as rejection', async t => {
  const result = await run(t, { partialResponse: true });
  assert.equal(result.code, 1);
  assert.equal(result.probes.length, 4);
  assert.match(result.output, /HTTP 401 with curl exit 56/);
});

for (const discovery of ['failure', 'malformed', 'empty']) {
  test(`endpoint discovery ${discovery} fails before probing and cleans up`, async t => {
    const result = await run(t, { discovery });
    assert.equal(result.code, 2);
    assert.equal(result.probes.length, 0);
    assert.equal(result.running, false);
    assert.match(result.output, /endpoint discovery/);
  });
}

for (const options of [{ startFailure: true }, { startMalformed: true }, { waitFailure: true }, { psFailure: 2 }]) {
  test(`partial startup failure cleans only the owned AppHost: ${JSON.stringify(options)}`, async t => {
    const result = await run(t, options);
    assert.equal(result.code, 2);
    assert.equal(result.probes.length, 0);
    assert.equal(result.running, false);
    assert.equal(result.calls.filter(call => call[1] === 'stop').length, 1);
  });
}

for (const options of [{ existing: true }, { psFailure: 1 }]) {
  test(`preflight refuses lifecycle work: ${JSON.stringify(options)}`, async t => {
    const result = await run(t, options);
    assert.equal(result.code, 2);
    assert.equal(result.calls.filter(call => call[1] === 'start' || call[1] === 'stop').length, 0);
  });
}

for (const options of [{ pidChanged: true }, { identityMismatch: true }, { psFailure: 3 }]) {
  test(`unproven or changed ownership is never stopped: ${JSON.stringify(options)}`, async t => {
    const result = await run(t, options);
    assert.notEqual(result.code, 0);
    assert.equal(result.calls.filter(call => call[1] === 'stop').length, 0);
    assert.equal(result.running, true);
  });
}

test('cleanup failure makes otherwise passing verification fail', async t => {
  const result = await run(t, { stopFailure: true });
  assert.equal(result.code, 1);
  assert.equal(result.probes.length, 4);
  assert.match(result.output, /cleanup failed/);
});

test('Quality executes the helper and its regression suite as blocking steps', async () => {
  const workflow = await readFile(join(repository, '.github/workflows/quality.yml'), 'utf8');
  const step = /- name: AppHost rejects invalid bearer tokens\r?\n([\s\S]*?)(?=      - name:)/.exec(workflow)?.[1];
  assert.ok(step);
  assert.match(step, /run: \.\/eng\/verify-apphost-invalid-bearer\.sh/);
  assert.doesNotMatch(step, /continue-on-error|if:/);
  const packageJson = JSON.parse(await readFile(join(repository, 'tests/e2e/package.json'), 'utf8'));
  assert.ok(packageJson.scripts['test:epic-9-evidence'].includes('scripts/verify-apphost-invalid-bearer.test.mjs'));
});

for (const readinessFailure of [false, true]) {
  test(`disposable health gate precedes probes and preserves cleanup: failure=${readinessFailure}`, async t => {
    const result = await run(t, { verification: true, readinessFailure });
    assert.equal(result.code, readinessFailure ? 2 : 0, result.output);
    assert.equal(result.probes.length, readinessFailure ? 0 : 4);
    assert.equal(result.calls.filter(call => call[0] === 'python3').length, 1);
    assert.equal(result.running, false);
    if (readinessFailure) assert.match(result.output, /application health\/readiness verification failed/);
  });
}


test('invalid-bearer helper releases its owned flock after cleanup while an inherited child survives', async t => {
  const result = await run(t, { survivingChild: true });
  assert.equal(result.code, 0, result.output);
  assert.equal(result.running, false);
  assert.equal(result.probes.length, 4);
  const checks = (await readFile(result.cleanupLockLog, 'utf8')).trim().split('\n');
  assert.ok(checks.includes('stop 1'));
  assert.ok(checks.every(check => check.endsWith(' 1')), checks.join('\n'));
  process.kill(Number(await readFile(result.survivorPidFile, 'utf8')), 0);
  assert.equal(await lockAvailable(result.lockFile), true, 'a surviving child must not retain the completed lifecycle lock');
  assert.equal((await readFile(result.flockLog, 'utf8')).split('\n').filter(call => call.startsWith('-u ')).length, 1);
});

test('invalid-bearer helper never unlocks a flock it failed to acquire or starts lifecycle work under contention', async t => {
  const result = await run(t, { beforeRun: async path => {
    await holdLock(t, path);
    assert.equal(await lockAvailable(path), false);
  } });
  assert.equal(result.code, 2, result.output);
  assert.match(result.output, /Another proof owns the AppHost lifecycle lock/);
  assert.deepEqual(result.calls, []);
  assert.equal((await readFile(result.flockLog, 'utf8')).split('\n').filter(call => call.startsWith('-u ')).length, 0);
  assert.equal(await lockAvailable(result.lockFile), false, 'the active owner must retain its lock');
});


for (const [name, options, expectedExit] of [
  ['readiness failure', { waitFailure: true }, 2],
  ['SIGINT', { signal: 'SIGINT' }, 130],
  ['SIGTERM', { signal: 'SIGTERM' }, 143],
]) {
  test(`invalid-bearer ${name} retains flock through EXIT discovery and stop, then releases its surviving child descriptor`, async t => {
    const result = await run(t, { survivingChild: true, ...options });
    assert.equal(result.code, expectedExit, result.output);
    assert.equal(result.running, false);
    assert.equal(result.probes.length, 0);
    process.kill(Number(await readFile(result.survivorPidFile, 'utf8')), 0);
    const checks = (await readFile(result.cleanupLockLog, 'utf8')).trim().split('\n');
    assert.deepEqual(checks.slice(-3), ['ps 1', 'stop 1', 'ps 1'], 'EXIT discovery, shutdown and postflight must each contend on a separate real flock');
    assert.ok(checks.every(check => check.endsWith(' 1')), checks.join('\n'));
    assert.equal(await lockAvailable(result.lockFile), true);
    assert.equal((await readFile(result.flockLog, 'utf8')).split('\n').filter(call => call.startsWith('-u ')).length, 1);
  });
}

test('invalid-bearer helper fails with a fixed diagnostic if its owned flock cannot be unlocked', async t => {
  const result = await run(t, { survivingChild: true, rejectUnlock: true });
  assert.equal(result.code, 1, result.output);
  assert.match(result.output, /^::error::Could not release the owned AppHost lifecycle lock\.$/m);
  assert.equal(result.running, false);
  assert.equal(result.probes.length, 4);
  process.kill(Number(await readFile(result.survivorPidFile, 'utf8')), 0);
  const checks = (await readFile(result.cleanupLockLog, 'utf8')).trim().split('\n');
  assert.ok(checks.includes('stop 1'));
  assert.ok(checks.every(check => check.endsWith(' 1')), checks.join('\n'));
  assert.equal(await lockAvailable(result.lockFile), false, 'the rejected unlock leaves the inherited descriptor locked until owned fixture teardown');
  assert.equal((await readFile(result.flockLog, 'utf8')).split('\n').filter(call => call.startsWith('-u ')).length, 1);
});

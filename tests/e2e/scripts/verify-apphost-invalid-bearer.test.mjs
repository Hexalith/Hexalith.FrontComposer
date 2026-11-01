import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { chmod, mkdir, mkdtemp, readFile, readdir, rm, stat, writeFile } from 'node:fs/promises';
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
  const diagnostics = join(root, 'diagnostics');
  await mkdir(bin);
  await mkdir(scratch);
  if (options.previousEvidence) {
    await mkdir(diagnostics);
    await writeFile(join(diagnostics, 'diagnostics.previous.json'), '{"historical":true}\n');
  }
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
  if (options.artifactCopyFailure || options.artifactPublishFailure) {
    const name = options.artifactCopyFailure ? 'cat' : 'ln';
    await writeFile(join(bin, name), `#!/usr/bin/env bash
if [[ "$1" == */sanitized.json || "$1" == -- ]]; then
  [[ "$1" != */sanitized.json ]] || printf '{'
  printf 'private-password\\n' >&2
  exit 73
fi
PATH="$BEARER_TEST_ORIGINAL_PATH" exec ${name} "$@"
`);
    await chmod(join(bin, name), 0o755);
  }
  const prelude = `
import { readFileSync, writeFileSync, appendFileSync, chmodSync, symlinkSync, utimesSync } from 'node:fs';
import { dirname, join } from 'node:path';
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
const identity = () => ({ appHostPath: apphost, ...(options.missingPid && state.ps >= 3 ? {} : { appHostPid: state.pid }) });
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
    if (options.pidChangedDuringDiagnostics && state.calls.some(call => call[1] === 'logs')) state.pid = 9999;
    const finalRefresh = state.calls.filter(call => call[1] === 'logs').length === 2;
    if (finalRefresh && options.finalOwnerCheck === 'changed') state.pid = 9999;
    save();
    if (finalRefresh && options.finalOwnerCheck === 'hang') { setTimeout(() => {}, 60_000); break; }
    if (options.psFailure === state.ps) { console.error(privateText); process.exit(9); }
    console.log(JSON.stringify(state.running ? [identity()] : []));
    break;
  case 'start':
    state.running = !options.startAlreadyExited;
    save();
    const parentLog = args[args.indexOf('--log-file') + 1];
    const logFile = join(dirname(parentLog), 'cli_20261101T121314123_detach-child_' + 'a'.repeat(32) + '.log');
    if (options.hostileDetachedLog === 'symlink') {
      writeFileSync(statePath + '.unowned', 'Address already in use ' + privateText);
      symlinkSync(statePath + '.unowned', logFile);
    } else if (options.hostileDetachedLog !== 'missing') {
      writeFileSync(logFile, 'Unhandled exception ' + privateText + '\\nFailed to bind ' + privateText + '\\n');
      if (options.hostileDetachedLog === 'public') chmodSync(logFile, 0o644);
      if (options.hostileDetachedLog === 'stale') utimesSync(logFile, 1, 1);
    }
    writeFileSync(parentLog, 'Spawning child CLI: aspire run --apphost ' + apphost + ' --log-file ' + logFile + (options.hostileDetachedLog === 'unbound' ? '.other' : '') + ' --isolated --no-build ' + privateText);
    console.log('Connection refused ' + privateText);
    console.log('Failed to bind ' + privateText);
    if (options.startFailure && !options.startFailureRecord) { console.error(privateText); process.exit(8); }
    if (options.startMalformed) { console.log(privateText); break; }
    console.log('Starting Aspire AppHost in the background...');
    console.log(JSON.stringify({ ...identity(), ...(options.identityMismatch ? { appHostPid: 9999 } : {}), logFile, dashboardUrl: privateText }));
    if (options.startFailure) process.exit(8);
    break;
  case 'wait':
    save();
    if (options.signal) process.kill(process.ppid, options.signal);
    if (options.waitFailure) { console.error(privateText); process.exit(7); }
    break;
  case 'describe':
    save();
    if (state.calls.filter(call => call[1] === 'describe').length > 1 || args[1] === 'security') {
      if (options.diagnosticFailure) { console.error(privateText); process.exit(18); }
      if (options.diagnosticMalformed) { console.log(privateText); break; }
    }
    if (options.discovery === 'failure') { console.error(privateText); process.exit(6); }
    if (options.discovery === 'malformed') { console.log(privateText); break; }
    console.log(JSON.stringify({ resources: [{ name: args[1] + '-owned', displayName: args[1], state: options.hostileResourceState ? privateText : 'Running', healthStatus: 'Healthy', exitCode: options.resourceExitCode ?? 17, urls: options.discovery === 'empty' ? [] : [
      { name: 'https', url: 'https://localhost:5443' },
      { name: 'http', url: 'http://127.0.0.1:5080' },
      { name: 'management', url: 'http://localhost:9990' },
      { name: 'private', url: 'https://private:password@example.invalid/?token=private-token' },
    ], environment: { secret: privateText } }] }));
    break;
  case 'logs':
    save();
    if (options.diagnosticFailure) { console.error(privateText); process.exit(19); }
    if (options.diagnosticMalformed) { console.log(privateText); break; }
    if (options.diagnosticTimeout && args[1] === 'security') { setTimeout(() => {}, 60_000); break; }
    if (options.diagnosticOversize) { console.log(privateText.repeat(4000)); break; }
    console.log(JSON.stringify(options.extraErrorRecords
      ? Array.from({ length: 45 }, (_, index) => ({ isError: index === 0 ? 'private-password' : true, content: 'Connection refused ' + privateText }))
      : [{ isError: true, level: 'Error', content: 'Connection refused ' + privateText }, { isError: false, level: privateText, message: 'Now listening on https://private.invalid/?password=private-password' }]));
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
const commandSubmit = new URL(url).pathname === '/api/v1/commands';
const attempts = state.calls.filter(call => call[0] === 'curl' && new URL(call.at(-1)).pathname === new URL(url).pathname).length;
const transportFailure = options.unreachable || (options.fallback && url.startsWith('https:')) ||
  (options.coldStart && commandSubmit && attempts <= 2);
const metrics = options.metrics ?? ['0.000019', '0.001500', url.startsWith('https:') ? '0.003500' : '0.000000', '0.000000', '10.000100', '127.0.0.1', '5080', '::1', '41000', '1', '0'];
const emit = status => process.stdout.write(options.rawCurlOutput ?? [String(status), metrics.join('\\t'), ''].join('\\n'));
if (transportFailure) { console.error(privateText); emit('000'); process.exit(options.transportExit ?? 28); }
emit(options.coldStart && commandSubmit ? options.coldStatus ?? 401 : options.httpStatus ?? 401);
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
      FC_INVALID_BEARER_DIAGNOSTICS_DIR: diagnostics,
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
  if (options.previousEvidence) assert.equal(await readFile(join(diagnostics, 'diagnostics.previous.json'), 'utf8'), '{"historical":true}\n');
  const retainedFiles = (await readdir(diagnostics)).filter(name => name !== 'diagnostics.previous.json');
  const artifactFailure = options.artifactCopyFailure || options.artifactPublishFailure;
  assert.equal(retainedFiles.length, artifactFailure ? 0 : 1, 'retain complete records only, with no pending files');
  let artifact = '', evidence = null;
  if (!artifactFailure) {
    assert.match(retainedFiles[0], /^diagnostics\.[A-Za-z0-9]+\.json$/);
    artifact = await readFile(join(diagnostics, retainedFiles[0]), 'utf8');
    assert.equal((await stat(join(diagnostics, retainedFiles[0]))).mode & 0o777, 0o600);
    evidence = JSON.parse(artifact);
    if (final.calls.some(call => call[1] === 'start')) {
      assert.equal(evidence.startup.launchOutputStatus, 'available');
      assert.deepEqual(evidence.startup.consoleSignals, {
        connectionRefused: 1, addressInUse: 0, listening: 0, unhandledException: 0, failedToBind: 1,
      });
    }
  }
  for (const secret of ['private-bearer', 'private-password', 'private-token', 'example.invalid', 'private.invalid', 'authorization=', 'dashboardUrl', 'invalid-ci-token', 'https://localhost', 'http://127.0.0.1', 'cli_20261101T121314123_detach-child_']) {
    assert.ok(!output.includes(secret), `diagnostics leaked ${secret}`);
    assert.ok(!artifact.includes(secret), `artifact leaked ${secret}`);
  }
  return { code, output, evidence, artifact, diagnostics, lockFile, flockLog, survivorPidFile, cleanupLockLog, ...final, probes: final.calls.filter(call => call[0] === 'curl') };
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

test('all-unreachable endpoints get exactly one additional transport-only pass and still fail every path', async t => {
  const result = await run(t, { unreachable: true });
  assert.equal(result.code, 1);
  assert.equal(result.probes.length, 16);
  for (const path of paths) {
    const probes = result.probes.filter(probe => new URL(probe.at(-1)).pathname + new URL(probe.at(-1)).search === path);
    assert.deepEqual(probes.map(probe => new URL(probe.at(-1)).protocol), ['https:', 'http:', 'https:', 'http:']);
  }
  assert.equal(result.output.match(/all advertised endpoints were unreachable/g)?.length, 4);
  assert.equal(result.running, false);
  assert.equal(result.evidence.verificationExitCode, 1);
  assert.equal(result.evidence.exitCode, 1);
  assert.equal(result.evidence.diagnosticsOwnership, 'verified');
  assert.deepEqual(result.evidence.cleanup, { appHost: 'verified-stopped', lock: 'released' });
  assert.equal(result.evidence.startup.ownershipConfirmed, true);
  assert.equal(result.evidence.startup.exitCode, 0);
  assert.deepEqual(result.evidence.startup.waits, [{ resource: 'security', exitCode: 0 }, { resource: 'eventstore', exitCode: 0 }]);
  assert.equal(result.evidence.probes.length, 16);
  assert.deepEqual(result.evidence.probes[0], {
    route: 1, pass: 1, endpoint: 1, scheme: 'https', advertisedPort: 5443, curlExitCode: 28, httpCode: '000',
    seconds: { nameLookup: 0.000019, connect: 0.0015, tls: 0.0035, firstByte: 0, total: 10.0001 },
    remotePeer: 'loopback-ipv4', remotePort: 5080, localPeer: 'loopback-ipv6', localPort: 41000, connections: 1, tlsVerifyResult: 0,
  });
  assert.equal(result.evidence.resources.length, 4);
  for (const summary of result.evidence.resources.filter(item => item.source === 'describe')) {
    assert.equal(summary.state, 'Running');
    assert.equal(summary.health, 'Healthy');
    assert.equal(summary.exitCode, 0, 'CLI success is separate from resource process exit');
    assert.equal(summary.resourceExitCode, 17);
    assert.deepEqual(summary.endpoints, [
      { scheme: 'https', host: 'loopback-name', port: 5443 }, { scheme: 'http', host: 'loopback-ipv4', port: 5080 },
    ]);
  }
  for (const summary of result.evidence.resources.filter(item => item.source === 'logs')) {
    assert.equal(summary.records, 2);
    assert.equal(summary.stderrRecords, 1);
    assert.equal(summary.signals.connectionRefused, 1);
    assert.equal(summary.signals.listening, 1);
  }
  const diagnosticCalls = result.calls.filter(call => call[1] === 'logs' || (call[1] === 'describe' && call[2] === 'security'));
  assert.ok(diagnosticCalls.every(call => call.includes('--apphost') && call[call.indexOf('--apphost') + 1] === apphost));
  assert.ok(diagnosticCalls.every(call => result.calls.indexOf(call) < result.calls.findIndex(item => item[1] === 'stop')));
  assert.ok(result.calls.filter(call => call[1] === 'logs').every(call => call[call.indexOf('--tail') + 1] === '40'));
  assert.equal(result.calls.filter(call => call[1] === 'start').length, 1);
  assert.ok(result.probes.every(call => call[call.indexOf('--connect-timeout') + 1] === '5' && call[call.indexOf('--max-time') + 1] === '10'));
});

for (const transportExit of [7, 28, 35, 56]) for (const coldStatus of [401, 200]) {
  test(`cold-start command submit retries both ${transportExit}/000 endpoints once and requires completed rejection: HTTP ${coldStatus}`, async t => {
    const result = await run(t, { coldStart: true, coldStatus, transportExit });
    assert.equal(result.code, coldStatus === 401 ? 0 : 1, result.output);
    assert.equal(result.probes.length, 6);
    const submits = result.probes.filter(probe => new URL(probe.at(-1)).pathname === '/api/v1/commands');
    assert.deepEqual(submits.map(probe => new URL(probe.at(-1)).protocol), ['https:', 'http:', 'https:']);
    assert.equal(result.output.split(`curl exit ${transportExit} (`).length - 1, 2);
    assert.equal(result.output.match(/rejected the invalid bearer with 401/g)?.length, coldStatus === 401 ? 4 : 3);
    if (coldStatus === 200) assert.match(result.output, /returned HTTP 200 with curl exit 0/);
    assert.equal(result.running, false);
  });
}

for (const transportExit of [0, 9]) {
  test(`HTTP 000 with unrecognized curl outcome ${transportExit} fails without endpoint fallback or retry`, async t => {
    const result = await run(t, { unreachable: true, transportExit });
    assert.equal(result.code, 1, result.output);
    assert.equal(result.probes.length, 4);
    assert.match(result.output, new RegExp(`returned HTTP invalid with curl exit ${transportExit}`));
    assert.equal(result.running, false);
  });
}

test('a transport fallback followed by HTTP 200 never permits another pass', async t => {
  const result = await run(t, { fallback: true, httpStatus: 200 });
  assert.equal(result.code, 1, result.output);
  assert.equal(result.probes.length, 8);
  assert.equal(result.output.match(/returned HTTP 200/g)?.length, 4);
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

test('successful verification retains numeric evidence and cleanup without extra resource diagnostics', async t => {
  const result = await run(t);
  assert.equal(result.code, 0, result.output);
  assert.equal(result.evidence.verificationExitCode, 0);
  assert.equal(result.evidence.exitCode, 0);
  assert.deepEqual(result.evidence.cleanup, { appHost: 'verified-stopped', lock: 'released' });
  assert.equal(result.evidence.probes.length, 4);
  assert.ok(result.evidence.probes.every(probe => probe.httpCode === '401' && probe.curlExitCode === 0));
  assert.deepEqual(result.evidence.resources, []);
  assert.equal(result.calls.filter(call => call[1] === 'describe').length, 1);
  assert.equal(result.calls.filter(call => call[1] === 'logs').length, 0);
});

test('hostile timing, peer and resource strings are omitted while transport failure still fails', async t => {
  const result = await run(t, {
    unreachable: true, hostileResourceState: true,
    metrics: ['private-bearer', '-1', 'NaN', 'https://private.invalid/?token=private-token', '99999999', 'private-password', 'private-token', '198.51.100.5', '70000', 'authorization=private-bearer', 'private-password'],
  });
  assert.equal(result.code, 1, result.output);
  assert.equal(result.probes.length, 16);
  for (const probe of result.evidence.probes) {
    assert.deepEqual(probe.seconds, { nameLookup: null, connect: null, tls: null, firstByte: null, total: null });
    assert.equal(probe.remotePeer, 'other');
    assert.equal(probe.remotePort, null);
    assert.equal(probe.localPeer, 'other');
    assert.equal(probe.localPort, null);
    assert.equal(probe.connections, null);
    assert.equal(probe.tlsVerifyResult, null);
  }
  assert.ok(!result.artifact.includes('198.51.100.5'));
  assert.ok(result.evidence.resources.filter(item => item.source === 'describe').every(item => item.state === 'unknown'));
});

for (const rawCurlOutput of ['401\nprivate-bearer', '401 private-password\n0\t0\t0\t0\t0\t\t0\t\t0\t0\t0\n']) {
  test('malformed curl output cannot turn a header into completed authentication proof', async t => {
    const result = await run(t, { rawCurlOutput });
    assert.equal(result.code, 1, result.output);
    assert.equal(result.probes.length, 4);
    assert.ok(result.evidence.probes.every(probe => probe.httpCode === 'invalid'));
    assert.match(result.output, /returned HTTP invalid/);
  });
}

for (const [option, status] of [['diagnosticFailure', 'command-failed'], ['diagnosticMalformed', 'invalid-json'], ['diagnosticOversize', 'output-limit']]) {
  test(`unavailable diagnostic source ${option} records its limitation without changing the failed proof`, async t => {
    const result = await run(t, { unreachable: true, [option]: true });
    assert.equal(result.code, 1, result.output);
    assert.equal(result.evidence.verificationExitCode, 1);
    assert.equal(result.evidence.resources.length, 4);
    const sources = result.evidence.resources.filter(item => option !== 'diagnosticOversize' || item.source === 'logs');
    assert.ok(sources.every(item => item.status === status), JSON.stringify(sources));
    assert.equal(result.evidence.cleanup.appHost, 'verified-stopped');
    assert.equal(result.running, false);
  });
}

test('a hanging diagnostic command is bounded and still permits verified cleanup', async t => {
  const started = Date.now();
  const result = await run(t, { unreachable: true, diagnosticTimeout: true });
  assert.equal(result.code, 1, result.output);
  assert.ok(Date.now() - started < 18_000, 'diagnostic deadline must precede the test process deadline');
  assert.equal(result.evidence.resources.find(item => item.resource === 'security' && item.source === 'logs').status, 'timeout');
  assert.equal(result.evidence.cleanup.appHost, 'verified-stopped');
  assert.equal(result.running, false);
});

test('an ownership change during diagnostics prevents further resource reads and refuses stop', async t => {
  const result = await run(t, { unreachable: true, pidChangedDuringDiagnostics: true });
  assert.equal(result.code, 1, result.output);
  assert.equal(result.evidence.diagnosticsOwnership, 'rejected');
  assert.equal(result.evidence.cleanup.appHost, 'ownership-changed');
  assert.equal(result.calls.filter(call => call[1] === 'logs').length, 1);
  assert.equal(result.evidence.resources.length, 2);
  assert.equal(result.calls.filter(call => call[1] === 'stop').length, 0);
  assert.equal(result.running, true);
});

test('new sanitized invocation evidence preserves historical evidence', async t => {
  const result = await run(t, { unreachable: true, previousEvidence: true });
  assert.equal(result.code, 1, result.output);
  assert.equal((await readdir(result.diagnostics)).length, 2);
});

for (const finalOwnerCheck of ['changed', 'hang']) {
  test(`final ownership refresh ${finalOwnerCheck} preserves captured evidence and refuses an unverified stop`, async t => {
    const result = await run(t, { unreachable: true, finalOwnerCheck });
    assert.equal(result.code, 1, result.output);
    assert.equal(result.evidence.diagnosticsOwnership, finalOwnerCheck === 'hang' ? 'unavailable' : 'rejected');
    assert.equal(result.evidence.cleanup.appHost, finalOwnerCheck === 'hang' ? 'ownership-unavailable' : 'ownership-changed');
    assert.equal(result.evidence.resources.length, 4, 'capture-time summaries remain valid');
    assert.equal(result.calls.filter(call => call[1] === 'stop').length, 0);
    assert.equal(result.evidence.cleanup.lock, 'released');
    assert.equal(result.running, true);
  });
}

for (const options of [
  { startAlreadyExited: true },
  { startFailure: true, startAlreadyExited: true },
  { startFailure: true, startFailureRecord: true, startAlreadyExited: true },
]) {
  test(`startup summaries survive failed/already-exited launch: ${JSON.stringify(options)}`, async t => {
    const result = await run(t, options);
    assert.equal(result.code, 2, result.output);
    assert.equal(result.evidence.startup.consoleSignals.connectionRefused, 1);
    assert.equal(result.evidence.startup.consoleSignals.failedToBind, 1);
    const log = result.evidence.startup.detachedLog;
    if (!options.startFailure || options.startFailureRecord) {
      assert.equal(log.status, 'available');
      assert.deepEqual(log.consoleSignals, {
        connectionRefused: 0, addressInUse: 0, listening: 0, unhandledException: 1, failedToBind: 1,
      });
    } else assert.equal(log.status, 'record-unavailable');
    assert.equal(result.evidence.resources.length, 0);
    assert.equal(result.calls.filter(call => call[1] === 'describe' || call[1] === 'logs' || call[1] === 'stop').length, 0);
  });
}

for (const hostileDetachedLog of ['unbound', 'symlink', 'public', 'stale', 'missing']) {
  test(`detached startup source ${hostileDetachedLog} is omitted safely`, async t => {
    const result = await run(t, { startAlreadyExited: true, hostileDetachedLog });
    assert.equal(result.code, 2, result.output);
    assert.deepEqual(result.evidence.startup.detachedLog, { status: hostileDetachedLog === 'missing' ? 'unavailable' : 'unsafe-path' });
  });
}

test('resource process exit codes require a bounded integer', async t => {
  for (const resourceExitCode of ['private-password', 1.5, 4294967296]) {
    const result = await run(t, { unreachable: true, resourceExitCode });
    assert.equal(result.code, 1, result.output);
    assert.ok(result.evidence.resources.filter(item => item.source === 'describe').every(item => item.resourceExitCode === null));
  }
});

test('log stderr counts require a boolean and remain bounded to forty records', async t => {
  const result = await run(t, { unreachable: true, extraErrorRecords: true });
  assert.equal(result.code, 1, result.output);
  for (const summary of result.evidence.resources.filter(item => item.source === 'logs')) {
    assert.equal(summary.records, 40);
    assert.equal(summary.stderrRecords, 39);
    assert.equal(summary.signals.connectionRefused, 40);
  }
});

for (const option of ['artifactCopyFailure', 'artifactPublishFailure']) {
  test(`artifact ${option} removes pending output and publishes no partial JSON`, async t => {
    const result = await run(t, { [option]: true, previousEvidence: true });
    assert.equal(result.code, 1, result.output);
    assert.match(result.output, /Could not retain sanitized invalid-bearer diagnostics/);
    assert.deepEqual(await readdir(result.diagnostics), ['diagnostics.previous.json']);
    assert.equal(result.running, false);
  });
}

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

for (const options of [{ pidChanged: true }, { missingPid: true }, { identityMismatch: true }, { psFailure: 3 }]) {
  test(`unproven or changed ownership is never stopped: ${JSON.stringify(options)}`, async t => {
    const result = await run(t, options);
    assert.notEqual(result.code, 0);
    assert.equal(result.calls.filter(call => call[1] === 'stop').length, 0);
    assert.equal(result.running, true);
    assert.equal(result.evidence.resources.length, 0);
    assert.equal(result.calls.filter(call => call[1] === 'logs').length, 0);
  });
}

test('cleanup failure makes otherwise passing verification fail', async t => {
  const result = await run(t, { stopFailure: true });
  assert.equal(result.code, 1);
  assert.equal(result.probes.length, 4);
  assert.match(result.output, /cleanup failed/);
  assert.equal(result.evidence.verificationExitCode, 0);
  assert.equal(result.evidence.exitCode, 1);
  assert.equal(result.evidence.cleanup.appHost, 'failed');
});

test('Quality executes the helper and its regression suite as blocking steps', async () => {
  const workflow = await readFile(join(repository, '.github/workflows/quality.yml'), 'utf8');
  const step = /- name: AppHost rejects invalid bearer tokens\r?\n([\s\S]*?)(?=      - name:)/.exec(workflow)?.[1];
  assert.ok(step);
  assert.match(step, /run: \.\/eng\/verify-apphost-invalid-bearer\.sh/);
  assert.match(step, /FC_INVALID_BEARER_DIAGNOSTICS_DIR:.*artifacts\/invalid-bearer/);
  assert.doesNotMatch(step, /continue-on-error|if:/);
  const packageJson = JSON.parse(await readFile(join(repository, 'tests/e2e/package.json'), 'utf8'));
  assert.ok(packageJson.scripts['test:epic-9-evidence'].includes('scripts/verify-apphost-invalid-bearer.test.mjs'));
  const upload = /- name: Upload Epic 9 acceptance artifacts\r?\n([\s\S]*?)(?=\r?\n  #)/.exec(workflow)?.[1];
  assert.ok(upload);
  assert.match(upload, /if: always\(\)/);
  assert.match(upload, /artifacts\/epic-9\/\*\*/);
  assert.match(upload, /artifacts\/invalid-bearer\/diagnostics\.\*\.json/);
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
  assert.deepEqual(result.evidence.cleanup, { appHost: 'not-required', lock: 'not-held' });
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
    assert.equal(result.evidence.verificationExitCode, expectedExit);
    assert.equal(result.evidence.exitCode, expectedExit);
    assert.deepEqual(result.evidence.cleanup, { appHost: 'verified-stopped', lock: 'released' });
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
  assert.deepEqual(result.evidence.cleanup, { appHost: 'verified-stopped', lock: 'release-failed' });
  assert.equal(result.running, false);
  assert.equal(result.probes.length, 4);
  process.kill(Number(await readFile(result.survivorPidFile, 'utf8')), 0);
  const checks = (await readFile(result.cleanupLockLog, 'utf8')).trim().split('\n');
  assert.ok(checks.includes('stop 1'));
  assert.ok(checks.every(check => check.endsWith(' 1')), checks.join('\n'));
  assert.equal(await lockAvailable(result.lockFile), false, 'the rejected unlock leaves the inherited descriptor locked until owned fixture teardown');
  assert.equal((await readFile(result.flockLog, 'utf8')).split('\n').filter(call => call.startsWith('-u ')).length, 1);
});

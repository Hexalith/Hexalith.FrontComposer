import assert from 'node:assert/strict';
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

async function run(t, options = {}) {
  const root = await mkdtemp(join(tmpdir(), 'fc-bearer-test-'));
  t.after(() => rm(root, { recursive: true, force: true }));
  const bin = join(root, 'bin');
  const scratch = join(root, 'scratch');
  await mkdir(bin);
  await mkdir(scratch);
  const state = join(root, 'state.json');
  await writeFile(state, JSON.stringify({ running: options.existing ?? false, pid: 4321, calls: [], ps: 0 }));
  const executable = async (name, source) => {
    await writeFile(join(bin, `${name}.mjs`), source);
    await writeFile(join(bin, name), '#!/usr/bin/env bash\nexec node "${BASH_SOURCE[0]}.mjs" "$@"\n');
    await chmod(join(bin, name), 0o755);
  };
  const prelude = `
import { readFileSync, writeFileSync } from 'node:fs';
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
  const child = spawn('bash', [script], {
    cwd: repository,
    env: {
      ...process.env,
      PATH: `${bin}:${process.env.PATH}`,
      TMPDIR: scratch,
      FC_EPIC9_LOCK_ROOT: root,
      BEARER_TEST_STATE: state,
      BEARER_TEST_OPTIONS: JSON.stringify(options),
      BEARER_TEST_APPHOST: apphost,
      HTTPS_PROXY: 'http://proxy.invalid:8888',
      HTTP_PROXY: 'http://proxy.invalid:8888',
      NO_PROXY: '',
    },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let output = '';
  child.stdout.on('data', chunk => { output += chunk; });
  child.stderr.on('data', chunk => { output += chunk; });
  const timer = setTimeout(() => child.kill('SIGTERM'), 20_000);
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
  return { code, output, ...final, probes: final.calls.filter(call => call[0] === 'curl') };
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

import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer } from '../helpers/owned-process.ts';

const waitUntil = async (probe, timeoutMs = 5_000) => {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await probe()) return;
    await new Promise((resolve) => setTimeout(resolve, 25));
  }
  assert.fail('Focused process probe did not complete before its deadline');
};

const isAlive = async (pid) => {
  try {
    process.kill(pid, 0);
    if (process.platform === 'linux') {
      // A terminated orphan can briefly remain as a zombie until its new parent reaps it.
      const stat = await readFile(`/proc/${pid}/stat`, 'utf8');
      return !/\) Z /u.test(stat);
    }
    return true;
  } catch (error) {
    if (error.code === 'ESRCH' || error.code === 'ENOENT') return false;
    throw error;
  }
};

test('listening URLs wait for complete stdout chunks and tolerate malformed diagnostics', async (t) => {
  const owned = spawnOwnedProcess(process.execPath, ['-e', 'setInterval(() => {}, 1000);'], { env: process.env });
  t.after(() => stopOwnedProcess(owned));

  owned.child.stdout.emit('data', Buffer.from('Now listening on: http://127.0.0.1:5'));
  assert.equal(owned.listeningOrigins.size, 0);
  owned.child.stdout.emit('data', Buffer.from('070\r\nNow listening on: http://['));
  assert.deepEqual([...owned.listeningOrigins], ['http://127.0.0.1:5070']);
  owned.child.stdout.emit('data', Buffer.from('::1]:5080\nNow listening on: http://[invalid]\n'));
  assert.deepEqual([...owned.listeningOrigins], ['http://127.0.0.1:5070', 'http://[::1]:5080']);
});

test('forced Debug build timeout terminates its owned MSBuild Exec descendant', async (t) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-owned-msbuild-'));
  const childPidPath = join(root, 'descendant.pid');
  const childPath = join(root, 'descendant.cjs');
  await writeFile(childPath, `require('node:fs').writeFileSync(${JSON.stringify(childPidPath)}, String(process.pid)); process.on('SIGTERM', () => {}); setInterval(() => {}, 1000);`);
  const escapeXml = (value) => value.replaceAll('&', '&amp;').replaceAll('"', '&quot;');
  await writeFile(join(root, 'Probe.csproj'), `<Project DefaultTargets="Build"><Target Name="Restore"/><Target Name="Build"><Exec Command="${escapeXml(`"${process.execPath}" "${childPath}"`)}"/></Target></Project>`);
  const owned = spawnOwnedProcess('dotnet', [
    'build', 'Probe.csproj', '--configuration', 'Debug', '--disable-build-servers', '-m:1',
  ], { cwd: root, env: process.env }, 5_000);
  t.after(async () => { await stopOwnedProcess(owned); await rm(root, { recursive: true, force: true }); });
  let childPid;
  await waitUntil(async () => {
    try { childPid = Number(await readFile(childPidPath, 'utf8')); return true; }
    catch (error) { if (error.code === 'ENOENT') return false; throw error; }
  });
  assert.equal(await isAlive(childPid), true);
  await waitUntil(() => owned.timedOut, 6_000);
  await stopOwnedProcess(owned);
  await waitUntil(async () => !await isAlive(childPid));
  assert.notEqual(owned.child.exitCode ?? owned.child.signalCode, null);
});

test('another listener cannot establish readiness before the owned startup confirmation', async (t) => {
  const server = createServer((_request, response) => response.end('another server'));
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}`;
  const owned = spawnOwnedProcess(process.execPath, ['-e', 'setInterval(() => {}, 1000)'], { env: process.env });
  t.after(async () => { await stopOwnedProcess(owned); server.close(); });
  await assert.rejects(waitForOwnedServer(owned, url, Date.now() + 250), /did not confirm startup/u);
});

test('owned server exit during an HTTP response cannot establish readiness', async (t) => {
  let owned;
  const server = createServer(async (_request, response) => {
    await stopOwnedProcess(owned);
    response.end('another server');
  });
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}`;
  owned = spawnOwnedProcess(process.execPath, ['-e', `console.log('Now listening on: ${url}'); setInterval(() => {}, 1000);`], { env: process.env });
  t.after(async () => { await stopOwnedProcess(owned); server.close(); });
  await assert.rejects(waitForOwnedServer(owned, url, Date.now() + 2_000), /exited before readiness/u);
});

// POSIX process groups survive their launcher; Windows descendant tracking is separately deferred.
test('startup failure cleans descendants after the owned parent exits', { skip: process.platform === 'win32' }, async (t) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-owned-parent-exit-'));
  const childPidPath = join(root, 'descendant.pid');
  const childScript = `require('node:fs').writeFileSync(${JSON.stringify(childPidPath)}, String(process.pid)); process.on('SIGTERM', () => {}); setInterval(() => {}, 1000);`;
  const parentScript = `require('node:child_process').spawn(process.execPath, ['-e', ${JSON.stringify(childScript)}], { stdio: 'ignore' }).unref();`;
  const owned = spawnOwnedProcess(process.execPath, ['-e', parentScript], { env: process.env });
  t.after(async () => { await stopOwnedProcess(owned); await rm(root, { recursive: true, force: true }); });
  let childPid;
  await waitUntil(async () => {
    try { childPid = Number(await readFile(childPidPath, 'utf8')); return true; }
    catch (error) { if (error.code === 'ENOENT') return false; throw error; }
  });
  await waitUntil(() => owned.child.exitCode !== null);
  assert.equal(await isAlive(childPid), true);
  await assert.rejects(waitForOwnedServer(owned, 'http://127.0.0.1:1', Date.now() + 1_000), /exited before readiness/u);
  await stopOwnedProcess(owned);
  await waitUntil(async () => !await isAlive(childPid));
});

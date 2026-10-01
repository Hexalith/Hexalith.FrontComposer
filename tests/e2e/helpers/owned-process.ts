import { execFile, spawn, type ChildProcessWithoutNullStreams, type SpawnOptionsWithoutStdio } from 'node:child_process';

export interface OwnedProcess {
  child: ChildProcessWithoutNullStreams;
  output: string;
  listeningOrigins: Set<string>;
  error?: Error;
  timedOut: boolean;
  timer?: NodeJS.Timeout;
  cleanup?: Promise<void>;
}

export const spawnOwnedProcess = (
  command: string,
  args: string[],
  options: Omit<SpawnOptionsWithoutStdio, 'detached' | 'timeout'>,
  timeoutMs?: number,
): OwnedProcess => {
  // A private POSIX process group owns descendants even after the parent exits.
  const child = spawn(command, args, { ...options, detached: process.platform !== 'win32' });
  const owned: OwnedProcess = { child, output: '', listeningOrigins: new Set(), timedOut: false };
  const append = (chunk: Buffer): void => {
    owned.output = `${owned.output}${chunk.toString('utf8')}`.slice(-8_000);
    for (const match of owned.output.matchAll(/Now listening on:\s+(https?:\/\/[^\s]+)/gu)) {
      owned.listeningOrigins.add(new URL(match[1]).origin);
    }
  };
  child.stdout.on('data', append);
  child.stderr.on('data', append);
  child.on('error', (error: Error) => { owned.error = error; });
  child.once('exit', () => clearTimeout(owned.timer));
  if (timeoutMs !== undefined) {
    // Do not let spawn's timeout kill only the parent before Windows taskkill can discover its tree.
    owned.timer = setTimeout(() => {
      owned.timedOut = true;
      void stopOwnedProcess(owned).catch((error: Error) => { owned.error = error; });
    }, Math.max(1, timeoutMs));
  }
  return owned;
};

const running = (owned: OwnedProcess): boolean => owned.child.exitCode === null && owned.child.signalCode === null;

export const stopOwnedProcess = (owned: OwnedProcess): Promise<void> => {
  owned.cleanup ??= (async () => {
    clearTimeout(owned.timer);
    const pid = owned.child.pid;
    if (pid === undefined) return;
    if (process.platform === 'win32') {
      if (running(owned)) {
        await new Promise<void>((resolve, reject) => {
          execFile('taskkill', ['/PID', String(pid), '/T', '/F'], { timeout: 5_000 }, (error) => {
            if (error && running(owned)) reject(error);
            else resolve();
          });
        });
      }
    } else {
      const signalGroup = (signal: NodeJS.Signals): void => {
        try { process.kill(-pid, signal); }
        catch (error) { if ((error as NodeJS.ErrnoException).code !== 'ESRCH') throw error; }
      };
      signalGroup('SIGTERM');
      await waitForExit(owned.child);
      // The parent may exit first while an Exec child ignores TERM. Kill the owned group regardless.
      signalGroup('SIGKILL');
    }
    await waitForExit(owned.child);
    if (!owned.error && running(owned)) throw new Error(`Owned process ${pid} did not stop.\n${owned.output}`);
  })();
  return owned.cleanup;
};

const waitForExit = async (child: ChildProcessWithoutNullStreams): Promise<void> => {
  if (child.exitCode !== null || child.signalCode !== null) return;
  await new Promise<void>((resolve) => {
    const complete = (): void => {
      clearTimeout(timer);
      child.off('exit', complete);
      resolve();
    };
    const timer = setTimeout(complete, 5_000);
    child.once('exit', complete);
  });
};

export const waitForOwnedServer = async (owned: OwnedProcess, baseUrl: string, deadline: number): Promise<void> => {
  const origin = new URL(baseUrl).origin;
  let lastError: unknown;
  const requireRunning = (): void => {
    if (owned.error || owned.timedOut || !running(owned)) {
      throw new Error(`Owned server exited before readiness: ${String(owned.error ?? owned.child.exitCode ?? owned.child.signalCode)}.\n${owned.output}`);
    }
  };
  while (Date.now() < deadline) {
    requireRunning();
    if (owned.listeningOrigins.has(origin)) {
      let response: Response | undefined;
      try {
        response = await fetch(baseUrl, { signal: AbortSignal.timeout(Math.max(1, Math.min(2_000, deadline - Date.now()))) });
      } catch (error) { lastError = error; }
      // A different listener cannot turn the owned server's concurrent exit into readiness.
      requireRunning();
      if (response?.ok) return;
    }
    await new Promise((resolve) => setTimeout(resolve, Math.min(100, Math.max(0, deadline - Date.now()))));
  }
  throw new Error(`Owned server did not confirm startup and HTTP readiness at ${baseUrl}: ${String(lastError)}.\n${owned.output}`);
};

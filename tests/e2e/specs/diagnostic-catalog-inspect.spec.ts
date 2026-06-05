import { execFile } from 'node:child_process';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';

import { expect, test } from '@playwright/test';

const execFileAsync = promisify(execFile);
const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const CLI_PROJECT = path.join(REPO_ROOT, 'src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj');
const DOTNET_HOME = path.join(tmpdir(), 'frontcomposer-e2e-dotnet-home');

test.describe('Story 7.3: diagnostic catalog inspect CLI', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'CLI E2E coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });

  test('filters HFC sidecars by severity threshold in JSON and text output', async () => {
    const workspace = await createInspectWorkspace('severity-threshold', {
      'FrontComposer.diagnostics.json': JSON.stringify(
        {
          diagnostics: [
            { id: 'CS1001', severity: 'Error', relatedType: 'Acme.Shipping.ShipmentProjection', what: 'ignored' },
            { id: 'HFC1001', severity: 'Hidden', relatedType: 'Acme.Shipping.ShipmentProjection', what: 'hidden' },
            { id: 'HFC1002', severity: 'Info', relatedType: 'Acme.Shipping.ShipmentProjection', what: 'info' },
            { id: 'HFC1003', severity: 'Warning', relatedType: 'Acme.Shipping.ShipmentProjection', what: 'warn' },
            { id: 'HFC1004', severity: 'Error', relatedType: 'Acme.Shipping.ShipmentProjection', what: 'err' },
          ],
        },
        null,
        2,
      ),
    });

    try {
      await expectDiagnosticIds(workspace.projectPath, 'hidden', ['HFC1001', 'HFC1002', 'HFC1003', 'HFC1004']);
      await expectDiagnosticIds(workspace.projectPath, 'info', ['HFC1002', 'HFC1003', 'HFC1004']);
      await expectDiagnosticIds(workspace.projectPath, 'warning', ['HFC1003', 'HFC1004']);
      await expectDiagnosticIds(workspace.projectPath, 'error', ['HFC1004']);

      const warningText = await runInspect([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
        '--severity',
        'warning',
      ]);

      expect(warningText.exitCode).toBe(0);
      expect(warningText.stderr).toBe('');
      expect(warningText.stdout).not.toContain('HFC1002');
      expect(warningText.stdout).toContain('HFC1003 Warning');
      expect(warningText.stdout).toContain('HFC1004 Error');
      expect(warningText.stdout).toContain('Warnings: 1');
      expect(warningText.stdout).toContain('Errors: 1');
    } finally {
      await workspace.dispose();
    }
  });

  test('rejects invalid severity before rendering inspect output', async () => {
    const workspace = await createInspectWorkspace('invalid-severity', {});

    try {
      const result = await runInspect([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
        '--severity',
        'verbose',
      ]);

      expect(result.exitCode).toBe(2);
      expect(result.stderr).toContain('--severity must be one of hidden, info, warning, or error.');
      expect(result.stdout).toBe('');
    } finally {
      await workspace.dispose();
    }
  });

  test('emits malformed-sidecar sentinel without leaking absolute paths', async () => {
    const workspace = await createInspectWorkspace('malformed-sidecar', {
      'Broken.diagnostics.json': '{not-json',
    });

    try {
      const result = await runInspect([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
        '--format',
        'json',
      ]);

      expect(result.exitCode).toBe(0);
      expect(result.stderr).toBe('');
      expect(result.stdout).not.toContain(workspace.root);

      const report = JSON.parse(result.stdout) as InspectJsonReport;
      expect(report.schemaVersion).toBe('frontcomposer.cli.inspect.v1');
      expect(report.summary.warnings).toBe(1);
      expect(report.summary.errors).toBe(0);
      expect(report.diagnostics).toEqual([
        expect.objectContaining({
          id: 'HFCM0002',
          severity: 'Warning',
          path: 'obj/Debug/net10.0/generated/HexalithFrontComposer/Broken.diagnostics.json',
        }),
      ]);
    } finally {
      await workspace.dispose();
    }
  });
});

type InspectWorkspace = {
  root: string;
  projectPath: string;
  dispose: () => Promise<void>;
};

type InspectResult = {
  exitCode: number;
  stdout: string;
  stderr: string;
};

type InspectJsonReport = {
  schemaVersion: string;
  summary: {
    warnings: number;
    errors: number;
  };
  diagnostics: Array<{
    id: string;
    severity: string;
    path: string;
  }>;
};

const createInspectWorkspace = async (
  name: string,
  sidecars: Record<string, string>,
): Promise<InspectWorkspace> => {
  const root = await mkdtemp(path.join(tmpdir(), `frontcomposer-${name}-`));
  const projectDirectory = path.join(root, 'Acme.App');
  const generatedDirectory = path.join(
    projectDirectory,
    'obj/Debug/net10.0/generated/HexalithFrontComposer',
  );
  const projectPath = path.join(projectDirectory, 'Acme.App.csproj');

  await mkdir(generatedDirectory, { recursive: true });

  await writeFile(
    projectPath,
    [
      '<Project Sdk="Microsoft.NET.Sdk">',
      '  <PropertyGroup>',
      '    <TargetFramework>net10.0</TargetFramework>',
      '  </PropertyGroup>',
      '</Project>',
      '',
    ].join('\n'),
  );

  await writeFile(
    path.join(generatedDirectory, 'Acme.Shipping.ShipmentProjection.g.razor.cs'),
    'namespace Acme.Shipping;\n',
  );

  for (const [fileName, content] of Object.entries(sidecars)) {
    await writeFile(path.join(generatedDirectory, fileName), content);
  }

  return {
    root,
    projectPath,
    dispose: async () => {
      await rm(root, { recursive: true, force: true });
    },
  };
};

const expectDiagnosticIds = async (
  projectPath: string,
  severity: 'hidden' | 'info' | 'warning' | 'error',
  expectedIds: string[],
): Promise<void> => {
  const result = await runInspect([
    'inspect',
    '--project',
    projectPath,
    '--configuration',
    'Debug',
    '--framework',
    'net10.0',
    '--severity',
    severity,
    '--format',
    'json',
  ]);

  expect(result.exitCode).toBe(0);
  expect(result.stderr).toBe('');

  const report = JSON.parse(result.stdout) as InspectJsonReport;
  expect(report.diagnostics.map((diagnostic) => diagnostic.id)).toEqual(expectedIds);
};

const runInspect = async (args: string[]): Promise<InspectResult> => {
  try {
    const { stdout, stderr } = await execFileAsync(
      'dotnet',
      ['run', '--project', CLI_PROJECT, '--configuration', 'Release', '--no-restore', '--', ...args],
      {
        cwd: REPO_ROOT,
        env: {
          ...process.env,
          DOTNET_CLI_HOME: DOTNET_HOME,
          DOTNET_CLI_TELEMETRY_OPTOUT: '1',
          DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE: 'true',
          DOTNET_NOLOGO: '1',
          DOTNET_SKIP_FIRST_TIME_EXPERIENCE: '1',
          NUGET_XMLDOC_MODE: 'skip',
        },
        timeout: 120_000,
      },
    );

    return { exitCode: 0, stdout, stderr };
  } catch (error) {
    if (isExecError(error)) {
      return {
        exitCode: typeof error.code === 'number' ? error.code : 1,
        stdout: error.stdout,
        stderr: error.stderr,
      };
    }

    throw error;
  }
};

const isExecError = (
  error: unknown,
): error is Error & { code?: number; stdout: string; stderr: string } =>
  error instanceof Error &&
  'stdout' in error &&
  typeof (error as { stdout?: unknown }).stdout === 'string' &&
  'stderr' in error &&
  typeof (error as { stderr?: unknown }).stderr === 'string';

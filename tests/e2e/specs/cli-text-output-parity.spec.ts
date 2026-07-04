import { execFile } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';

import { expect, test } from '@playwright/test';

const execFileAsync = promisify(execFile);
const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const CLI_PROJECT = path.join(REPO_ROOT, 'src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj');
const CLI_BINARY = path.join(REPO_ROOT, 'src/Hexalith.FrontComposer.Cli/bin/Release/net10.0/Hexalith.FrontComposer.Cli');
const DOTNET_HOME = path.join(tmpdir(), 'frontcomposer-e2e-dotnet-home');
const ACTIONABLE_FINDINGS = 1;

test.describe('Story 10.3: CLI text-output parity guard', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'CLI E2E coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });

  test('keeps inspect text summary and filtered fail flags aligned with JSON behavior', async () => {
    const workspace = await createCliWorkspace('inspect-text-parity');

    try {
      for (const fileName of [
        'Acme.Shipping.ShipmentProjection.g.razor.cs',
        'Acme.Shipping.ShipmentProjectionRegistration.g.cs',
        'Acme.Shipping.CreateShipment.CommandForm.g.razor.cs',
        'Acme.Shipping.CreateShipment.CommandRenderer.g.razor.cs',
        'FrontComposerMcpManifest.g.cs',
      ]) {
        await workspace.writeGenerated(fileName, 'namespace Acme.Shipping;\n');
      }

      await workspace.writeGenerated(
        'FrontComposer.diagnostics.json',
        JSON.stringify(
          {
            diagnostics: [
              {
                id: 'HFC1002',
                severity: 'Warning',
                relatedType: 'Acme.Shipping.ShipmentProjection',
                what: 'warn',
              },
              {
                id: 'HFC1003',
                severity: 'Error',
                relatedType: 'Acme.Billing.InvoiceProjection',
                what: 'err',
              },
            ],
          },
          null,
          2,
        ),
      );

      const json = await runCli([
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
      const text = await runCli([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
      ]);
      const typeFiltered = await runCli([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
        '--type',
        'Acme.Shipping.ShipmentProjection',
        '--fail-on-error',
      ]);
      const warningFiltered = await runCli([
        'inspect',
        '--project',
        workspace.projectPath,
        '--configuration',
        'Debug',
        '--framework',
        'net10.0',
        '--type',
        'Acme.Shipping.ShipmentProjection',
        '--fail-on-warning',
      ]);

      expect(json.exitCode).toBe(0);
      expect(json.stderr).toBe('');
      expect(text.exitCode).toBe(0);
      expect(text.stderr).toBe('');

      const report = JSON.parse(json.stdout) as InspectReport;
      expect(text.stdout).toContain(`Generated files: ${report.summary.generatedFiles}`);
      expect(text.stdout).toContain(
        `Forms: ${report.summary.forms}; Grids: ${report.summary.grids}; Registrations: ${report.summary.registrations}; MCP manifests: ${report.summary.mcpManifestEntries}; Warnings: ${report.summary.warnings}; Errors: ${report.summary.errors}`,
      );
      expect(text.stdout).toContain(
        '- CommandForm: obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.CreateShipment.CommandForm.g.razor.cs',
      );
      expect(text.stdout).toContain(
        '- McpManifest: obj/Debug/net10.0/generated/HexalithFrontComposer/FrontComposerMcpManifest.g.cs',
      );
      expect(text.stdout).toContain('! HFC1002 Warning');
      expect(text.stdout).toContain('! HFC1003 Error');
      expect(text.stdout).not.toContain(workspace.root);

      expect(typeFiltered.exitCode).toBe(0);
      expect(typeFiltered.stderr).toBe('');
      expect(typeFiltered.stdout).toContain('Warnings: 1');
      expect(typeFiltered.stdout).toContain('Errors: 0');
      expect(typeFiltered.stdout).toContain('HFC1002 Warning');
      expect(typeFiltered.stdout).not.toContain('HFC1003');

      expect(warningFiltered.exitCode).toBe(ACTIONABLE_FINDINGS);
      expect(warningFiltered.stderr).toBe('');
      expect(warningFiltered.stdout).toContain('HFC1002 Warning');
    } finally {
      await workspace.dispose();
    }
  });

  test('keeps migrate text summary and fail-on-findings aligned with JSON behavior', async () => {
    const changedWorkspace = await createCliWorkspace('migrate-text-changed');
    const manualWorkspace = await createCliWorkspace('migrate-text-manual');
    const unchangedWorkspace = await createCliWorkspace('migrate-text-unchanged');

    try {
      await changedWorkspace.writeSource('Program.cs', 'services.AddFrontComposerDebugOverlay();\n');
      await manualWorkspace.writeSource('Program.cs', 'namespace Acme.App;\n');
      await manualWorkspace.writeGenerated(
        'frontcomposer.migration.diagnostics.json',
        JSON.stringify(
          {
            diagnostics: [
              {
                id: 'HFCM9002',
                severity: 'Warning',
                path: 'Program.cs',
                what: 'Custom FrontComposer migration requires manual review',
              },
            ],
          },
          null,
          2,
        ),
      );
      await unchangedWorkspace.writeSource('Program.cs', 'namespace Acme.App;\n');

      const changedJson = await runMigration(changedWorkspace.projectPath, ['--format', 'json']);
      const changedText = await runMigration(changedWorkspace.projectPath, []);
      const changedFail = await runMigration(changedWorkspace.projectPath, ['--fail-on-findings']);
      const manualFail = await runMigration(manualWorkspace.projectPath, ['--fail-on-findings']);
      const unchangedFail = await runMigration(unchangedWorkspace.projectPath, ['--fail-on-findings']);

      expect(changedJson.exitCode).toBe(0);
      expect(changedJson.stderr).toBe('');
      expect(changedText.exitCode).toBe(0);
      expect(changedText.stderr).toBe('');

      const changedReport = JSON.parse(changedJson.stdout) as MigrateReport;
      expect(changedText.stdout).toContain(summaryLine(changedReport.summary));
      expect(changedText.stdout).toContain('- safe-fix HFCM9001 Program.cs:');
      expect(changedText.stdout).toContain('AddFrontComposerDevMode');
      expect(changedText.stdout).not.toContain(changedWorkspace.root);

      expect(changedFail.exitCode).toBe(ACTIONABLE_FINDINGS);
      expect(changedFail.stderr).toBe('');
      expect(changedFail.stdout).toContain('Changed: 1');
      expect(changedFail.stdout).toContain('Manual-only: 0');
      expect(changedFail.stdout).toContain('Conflicts: 0');

      expect(manualFail.exitCode).toBe(ACTIONABLE_FINDINGS);
      expect(manualFail.stderr).toBe('');
      expect(manualFail.stdout).toContain('Changed: 0');
      expect(manualFail.stdout).toContain('Manual-only: 1');
      expect(manualFail.stdout).toContain('- manual-only HFCM9002 Program.cs:');
      expect(manualFail.stdout).not.toContain(manualWorkspace.root);

      expect(unchangedFail.exitCode).toBe(0);
      expect(unchangedFail.stderr).toBe('');
      expect(unchangedFail.stdout).toContain('Changed: 0');
      expect(unchangedFail.stdout).toContain('Unchanged: 1');
      expect(unchangedFail.stdout).toContain('Manual-only: 0');
      expect(unchangedFail.stdout).toContain('Conflicts: 0');
    } finally {
      await changedWorkspace.dispose();
      await manualWorkspace.dispose();
      await unchangedWorkspace.dispose();
    }
  });
});

type CliWorkspace = {
  root: string;
  projectPath: string;
  dispose: () => Promise<void>;
  writeGenerated: (fileName: string, content: string) => Promise<void>;
  writeSource: (relativePath: string, content: string) => Promise<void>;
};

type CliResult = {
  exitCode: number;
  stdout: string;
  stderr: string;
};

type InspectReport = {
  summary: {
    generatedFiles: number;
    forms: number;
    grids: number;
    registrations: number;
    mcpManifestEntries: number;
    warnings: number;
    errors: number;
  };
};

type MigrateReport = {
  summary: MigrationSummary;
};

type MigrationSummary = {
  changed: number;
  unchanged: number;
  skipped: number;
  failed: number;
  manualOnly: number;
  conflicts: number;
};

const createCliWorkspace = async (name: string): Promise<CliWorkspace> => {
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

  return {
    root,
    projectPath,
    dispose: async () => {
      await rm(root, { recursive: true, force: true });
    },
    writeGenerated: async (fileName: string, content: string) => {
      await writeFile(path.join(generatedDirectory, fileName), content);
    },
    writeSource: async (relativePath: string, content: string) => {
      const fullPath = path.join(projectDirectory, relativePath);
      await mkdir(path.dirname(fullPath), { recursive: true });
      await writeFile(fullPath, content);
    },
  };
};

const runMigration = async (projectPath: string, additionalArgs: string[]): Promise<CliResult> =>
  runCli([
    'migrate',
    '--project',
    projectPath,
    '--from',
    '9.1.0',
    '--to',
    '9.2.0',
    ...additionalArgs,
  ]);

const summaryLine = (summary: MigrationSummary): string =>
  `Changed: ${summary.changed}; Unchanged: ${summary.unchanged}; Skipped: ${summary.skipped}; Failed: ${summary.failed}; Manual-only: ${summary.manualOnly}; Conflicts: ${summary.conflicts}`;

const runCli = async (args: string[]): Promise<CliResult> => {
  const executable = existsSync(CLI_BINARY) ? CLI_BINARY : 'dotnet';
  const executableArgs = existsSync(CLI_BINARY)
    ? args
    : ['run', '--project', CLI_PROJECT, '--configuration', 'Release', '--no-restore', '--', ...args];

  try {
    const { stdout, stderr } = await execFileAsync(
      executable,
      executableArgs,
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

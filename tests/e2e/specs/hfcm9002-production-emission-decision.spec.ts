import { execFile } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
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

const ADOPTER_BOUNDARY_FILES = [
  'src/Hexalith.FrontComposer.Cli/README.md',
  '_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md',
  '_bmad-output/project-docs/api-contracts.md',
  'docs/migrations/9.1-to-9.2.md',
  'docs/diagnostics/migration-findings.json',
];

const PRODUCTION_PROMISE_PATTERNS = [
  /\badopter builds\s+(?:now\s+)?(?:emit|generate|produce)\b.{0,160}\bHFCM9002\b/is,
  /\bnormal builds\s+(?:now\s+)?(?:emit|generate|produce)\b.{0,160}\bHFCM9002\b/is,
  /\bSourceTools\s+(?:now\s+)?(?:emits|generates|produces)\b.{0,160}\bHFCM9002\b/is,
];

test.describe('Story 10.4: HFCM9002 production-emission decision', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Repository governance E2E coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });

  test('keeps the recorded decision on the not-approved production-emission path', async () => {
    const decision = await readRepoFile('_bmad-output/contracts/hfcm9002-production-emission-decision-2026-07-05.md');

    expect(decision).toContain('Decision: production emission not approved');
    expect(decision).toContain('Owners: Architect + Product Owner');
    expect(decision).toContain('Reviewed source documents');
    expect(decision).toContain('src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs');

    const productionApproved = /^Decision:\s+production emission approved\s*$/im.test(decision);
    expect(productionApproved).toBe(false);
  });

  test('guards adopter-facing documents from normal-build HFCM9002 sidecar promises', async () => {
    const cliReadme = await readRepoFile('src/Hexalith.FrontComposer.Cli/README.md');
    const migrateContract = await readRepoFile('_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md');

    expect(cliReadme).toContain('adopter builds do not yet produce production');
    expect(cliReadme).toContain('not a promise that normal builds');
    expect(migrateContract).toContain('There is no');
    expect(migrateContract).toContain('production SourceTools `HFCM9002` sidecar emitter');
    expect(migrateContract).toContain('No new production SourceTools `HFCM9002` sidecar emitter');

    for (const relativePath of ADOPTER_BOUNDARY_FILES) {
      const content = normalize(await readRepoFile(relativePath));

      for (const pattern of PRODUCTION_PROMISE_PATTERNS) {
        expect(content, `${relativePath} must not promise production HFCM9002 sidecars`).not.toMatch(pattern);
      }
    }
  });

  test('preserves synthetic sidecar consumption with text and JSON redaction', async () => {
    const workspace = await createCliWorkspace('hfcm9002-synthetic-boundary');

    try {
      await workspace.writeSource('Program.cs', 'namespace Acme.App;\n');
      await workspace.writeGenerated(
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

      const json = await runMigration(workspace.projectPath, ['--format', 'json']);
      const textFail = await runMigration(workspace.projectPath, ['--fail-on-findings']);

      expect(json.exitCode).toBe(0);
      expect(json.stderr).toBe('');
      expect(json.stdout).not.toContain(workspace.root);

      const report = JSON.parse(json.stdout) as MigrateReport;
      expect(report.summary.manualOnly).toBe(1);
      expect(report.entries).toHaveLength(1);
      expect(report.entries[0]).toMatchObject({
        diagnosticId: 'HFCM9002',
        kind: 'manual-only',
        path: 'Program.cs',
      });

      expect(textFail.exitCode).toBe(ACTIONABLE_FINDINGS);
      expect(textFail.stderr).toBe('');
      expect(textFail.stdout).toContain('Manual-only: 1');
      expect(textFail.stdout).toContain('- manual-only HFCM9002 Program.cs:');
      expect(textFail.stdout).not.toContain(workspace.root);
    } finally {
      await workspace.dispose();
    }
  });

  test('redacts hostile sidecar paths through the sidecar sentinel', async () => {
    const workspace = await createCliWorkspace('hfcm9002-hostile-sidecar');

    try {
      await workspace.writeSource('Program.cs', 'namespace Acme.App;\n');
      await workspace.writeGenerated(
        'frontcomposer.migration.diagnostics.json',
        JSON.stringify(
          {
            diagnostics: [
              {
                id: 'HFCM9002',
                severity: 'Warning',
                path: '../Program.cs',
                what: 'Unsafe sidecar path should not be trusted',
              },
            ],
          },
          null,
          2,
        ),
      );

      const json = await runMigration(workspace.projectPath, ['--format', 'json']);

      expect(json.exitCode).toBe(0);
      expect(json.stderr).toBe('');
      expect(json.stdout).not.toContain('../Program.cs');
      expect(json.stdout).not.toContain(workspace.root);

      const report = JSON.parse(json.stdout) as MigrateReport;
      expect(report.summary.manualOnly).toBe(1);
      expect(report.entries[0].kind).toBe('manual-only');
      expect(report.entries[0].path).toMatch(/^__sidecar__\//);
    } finally {
      await workspace.dispose();
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

type MigrationEntry = {
  diagnosticId: string;
  kind: string;
  path: string;
};

type MigrationSummary = {
  manualOnly: number;
};

type MigrateReport = {
  summary: MigrationSummary;
  entries: MigrationEntry[];
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

const readRepoFile = async (relativePath: string): Promise<string> =>
  readFile(path.join(REPO_ROOT, relativePath), 'utf8');

const normalize = (value: string): string => value.replace(/\s+/g, ' ').trim();

const isExecError = (
  error: unknown,
): error is Error & { code?: number; stdout: string; stderr: string } =>
  error instanceof Error &&
  'stdout' in error &&
  typeof (error as { stdout?: unknown }).stdout === 'string' &&
  'stderr' in error &&
  typeof (error as { stderr?: unknown }).stderr === 'string';

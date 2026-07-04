import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { expect, test } from '@playwright/test';

const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));

type AdopterPage = {
  relativePath: string;
  expectedTerms: string[];
};

const ADOPTER_PAGES: AdopterPage[] = [
  {
    relativePath: 'docs/how-to/migration-guides.md',
    expectedTerms: [
      'CLI migration contract',
      'HFC diagnostic governance contract',
      'migration stubs',
    ],
  },
  {
    relativePath: 'docs/migrations/9.1-to-9.2.md',
    expectedTerms: [
      'versioned CLI migration contract',
      'developer-mode overlay',
      'AddFrontComposerDevMode',
    ],
  },
  {
    relativePath: 'docs/reference/cli.md',
    expectedTerms: [
      'dry-run and apply modes',
      'versioned migration stubs',
      'docs/migrations',
    ],
  },
  {
    relativePath: 'docs/reference/diagnostics/index.md',
    expectedTerms: [
      'canonical diagnostic registry',
      'published diagnostics reference',
      'registry entries',
    ],
  },
  {
    relativePath: 'docs/concepts/source-generation-and-mcp-split.md',
    expectedTerms: [
      'single-source docs model',
      'narrative/reference markers',
      'MCP slices',
    ],
  },
];

const STALE_VISIBLE_OWNERSHIP_PATTERNS = [
  /Story\s+9-2/i,
  /Story\s+9-4/i,
  /Story\s+9-5/i,
  /Story\s+9\s+ownership/i,
  /9-2-cli-inspection-and-migration-tools/i,
  /9-4-diagnostic-id-system-and-deprecation-policy/i,
  /9-5-diataxis-documentation-site/i,
];

test.describe('Story 10.2: adopter-facing historical-label cleanup', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Documentation E2E coverage runs once in Chromium.');

  test('keeps current adopter-facing docs on contract wording instead of stale Story 9 ownership', async () => {
    for (const page of ADOPTER_PAGES) {
      const markdown = await readMarkdownPage(page.relativePath);
      const body = normalize(stripHtmlComments(markdown.body));

      expect(markdown.frontMatter, `${page.relativePath} must remain an adopter page`).toContain('audience: adopter');
      expect(markdown.frontMatter, `${page.relativePath} must remain published`).toContain('status: published');

      for (const expectedTerm of page.expectedTerms) {
        expect(body, `${page.relativePath} lost current-contract wording: ${expectedTerm}`).toContain(expectedTerm);
      }

      for (const pattern of STALE_VISIBLE_OWNERSHIP_PATTERNS) {
        expect(body, `${page.relativePath} still exposes stale historical ownership: ${pattern}`).not.toMatch(pattern);
      }
    }
  });

  test('preserves product version labels and migration API facts while removing ownership labels', async () => {
    const migrationHowTo = await readMarkdownPage('docs/how-to/migration-guides.md');
    const versionedGuide = await readMarkdownPage('docs/migrations/9.1-to-9.2.md');
    const combined = normalize(`${migrationHowTo.raw}\n${versionedGuide.raw}`);

    for (const requiredVersionFact of [
      'fromVersion: "9.1.0"',
      'toVersion: "9.2.0"',
      '9.1-to-9.2',
      'AddFrontComposerDebugOverlay',
      'AddFrontComposerDevMode',
      'HFCM9001',
      'HFCM9002',
    ]) {
      expect(combined, `Story 10.2 must not erase version/API fact: ${requiredVersionFact}`).toContain(
        requiredVersionFact,
      );
    }

    const visibleBody = normalize(`${migrationHowTo.body}\n${versionedGuide.body}`);
    expect(visibleBody).not.toContain('Story 9-2');
    expect(visibleBody).not.toContain('Story 9-4');
    expect(visibleBody).not.toContain('Epic 7');
  });

  test('retains Story 9 provenance only in metadata and internal registry surfaces', async () => {
    const migrationHowTo = await readMarkdownPage('docs/how-to/migration-guides.md');
    const diagnosticsIndex = await readMarkdownPage('docs/reference/diagnostics/index.md');
    const registry = await readJson<DiagnosticRegistry>('docs/diagnostics/diagnostic-registry.json');
    const migrationFindings = await readJson<MigrationFindings>('docs/diagnostics/migration-findings.json');
    const registryReadme = await readRepoFile('docs/diagnostics/README.md');

    expect(migrationHowTo.frontMatter).toContain('ownerStory: 9-5-diataxis-documentation-site');
    expect(diagnosticsIndex.frontMatter).toContain('ownerStory: 9-5-diataxis-documentation-site');

    expect(registry.messageTemplatePolicy).toContain('Story 9-5');
    expect(registry.diagnostics.some((diagnostic) => diagnostic.ownerStory === '9-4-diagnostic-id-system-and-deprecation-policy')).toBe(
      true,
    );
    expect(migrationFindings.ownerStory).toBe('9-2-cli-inspection-and-migration-tools');
    expect(migrationFindings.findings.some((finding) => finding.ownerStory === migrationFindings.ownerStory)).toBe(
      true,
    );
    expect(registryReadme).toContain('Story 9-4 makes `diagnostic-registry.json` the authoritative source');
  });
});

type MarkdownPage = {
  raw: string;
  frontMatter: string;
  body: string;
};

type DiagnosticRegistry = {
  messageTemplatePolicy: string;
  diagnostics: Array<{
    ownerStory: string;
  }>;
};

type MigrationFindings = {
  ownerStory: string;
  findings: Array<{
    ownerStory: string;
  }>;
};

const readMarkdownPage = async (relativePath: string): Promise<MarkdownPage> => {
  const raw = await readRepoFile(relativePath);
  const match = /^---\r?\n(?<frontMatter>[\s\S]*?)\r?\n---\r?\n(?<body>[\s\S]*)$/u.exec(raw);

  if (!match?.groups) {
    throw new Error(`${relativePath} is missing strict YAML front matter.`);
  }

  return {
    raw,
    frontMatter: match.groups.frontMatter,
    body: match.groups.body,
  };
};

const readJson = async <T>(relativePath: string): Promise<T> => {
  const raw = await readRepoFile(relativePath);
  return JSON.parse(raw) as T;
};

const readRepoFile = async (relativePath: string): Promise<string> =>
  readFile(path.join(REPO_ROOT, relativePath), 'utf8');

const normalize = (value: string): string => value.replace(/\s+/g, ' ').trim();

const stripHtmlComments = (value: string): string => value.replace(/<!--[\s\S]*?-->/gu, ' ');

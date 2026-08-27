import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { expect, test } from '@playwright/test';

const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const MANIFEST_PATH = 'tests/contract-fixtures/fc-nip-command-target-identity-contract.json';
const STORY_9_2 =
  '_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md';

type ContractDocument = { path: string; contains: string[]; notContains: string[] };
type ContractTable = { path: string; heading: string; rows: string[][] };
type ContractManifest = { schemaVersion: 1; documents: ContractDocument[]; tables: ContractTable[] };

// These checks read the filesystem only. The npm script pins one browser project and skips its
// web server, so no browserName condition may silently skip this governance suite.
test.describe('FC-NIP explicit command-target identity contract', () => {
  test('applies the shared manifest to normalized documents and exact tables', async () => {
    const manifest = validateManifest(JSON.parse(await readRawRepoFile(MANIFEST_PATH)) as unknown);

    for (const document of manifest.documents) {
      const content = await readRepoFile(document.path);
      for (const fragment of document.contains) {
        expect(content, `${document.path} is missing: ${fragment}`).toContain(fragment);
      }
      for (const fragment of document.notContains) {
        expect(content, `${document.path} unexpectedly contains: ${fragment}`).not.toContain(fragment);
      }
    }

    for (const table of manifest.tables) {
      expect(parseTableRows(await readRawRepoFile(table.path), table.heading)).toEqual(table.rows);
    }
  });

  test('rejects unsafe paths, duplicate table identities, and inconsistent row widths', () => {
    const document = (manifestPath: string): ContractDocument => ({
      path: manifestPath,
      contains: [],
      notContains: [],
    });
    const table = (rows: string[][]): ContractTable => ({
      path: 'docs/example.md',
      heading: '## Table',
      rows,
    });

    expect(() => validateManifest({ schemaVersion: 1, documents: [document('docs\\escape.md')], tables: [] })).toThrow();
    expect(() =>
      validateManifest({
        schemaVersion: 1,
        documents: [document('docs/example.md')],
        tables: [table([['a', 'b']]), table([['a', 'b']])],
      }),
    ).toThrow();
    expect(() =>
      validateManifest({
        schemaVersion: 1,
        documents: [document('docs/example.md')],
        tables: [table([['a', 'b'], ['a']])],
      }),
    ).toThrow();
  });

  test('pins TypeScript-specific implementation and ordering evidence', async () => {
    const rowIdentity = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs',
    );
    const eventStoreStatusQuery = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs',
    );
    const commandFormEmitter = await readRepoFile(
      'src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs',
    );
    const razorEmitter = await readRepoFile(
      'src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs',
    );
    const pendingState = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs',
    );
    const pollingCoordinator = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs',
    );
    const outcomeResolver = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs',
    );
    const storyNineTwo = await readRepoFile(STORY_9_2);
    const indicatorState = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs',
    );

    assertContainsAll(rowIdentity, [
      'projection row identity cascaded to generated command forms',
      'It must not be populated from raw',
      'command payloads or user-editable form values',
    ]);
    assertContainsAll(eventStoreStatusQuery, ['MessageId: pendingCommand.MessageId', 'string? AggregateId', 'int? EventCount']);
    for (const forbidden of ['EntityKey:', 'ProjectionTypeName:', 'LaneKey:', 'ExpectedStatusSlot:', 'PriorStatusSlot:']) {
      expect(eventStoreStatusQuery).not.toContain(forbidden);
    }

    assertContainsAll(commandFormEmitter, [
      'CascadingParameter',
      'CommandTypeName: typeof(',
      'form.CommandTarget?.ResolutionMode == CommandTargetResolutionMode.SameAsSource',
      'ResolveCommandTargetAsync(_model, cts.Token)',
      'PendingCommandOutcomeResolver.AssociateAccepted',
      'PendingCommandOutcomeResolver.Resolve',
    ]);
    for (const forbidden of ['PendingCommandState.ResolveTerminal', 'PendingCommandState.Register', 'EntityKey: status.AggregateId', 'ResultPayload']) {
      expect(commandFormEmitter).not.toContain(forbidden);
    }

    assertContainsAll(pendingState, [
      '_lifecycleConvergenceDeadlines',
      'TryConvergeLifecycle(canonicalMessageId)',
      'ConvergeLifecycle(int maximumAttempts)',
      'LifecycleMatches(terminal, lifecycleState)',
      'catch (Exception ex) when (ex is not OperationCanceledException)',
    ]);
    assertContainsAll(outcomeResolver, [
      '_indicatorDecisions.Contains(messageId!)',
      'RecordIndicatorDecision(messageId!)',
      '_ = _indicatorDecisions.Add(messageId)',
      'TryPublishIndicator(publication)',
      'TryGetCommittedRegistration(registration)',
      '!string.Equals(delegatedMessageId, canonicalMessageId, StringComparison.Ordinal)',
    ]);
    expect(outcomeResolver).not.toContain('_indicatorDecisions.Remove');
    expect((outcomeResolver.match(/_indicatorDecisions\.Clear\(\)/g) ?? []).length).toBe(1);

    const reservationSnapshotIndex = pollingCoordinator.indexOf('_pendingCommands.Snapshot()');
    const convergenceIndex = pollingCoordinator.indexOf(
      'concreteState.ConvergeLifecycle(convergenceBudget)',
      reservationSnapshotIndex,
    );
    const pollingSnapshotIndex = pollingCoordinator.indexOf('_pendingCommands.Snapshot()', convergenceIndex);
    const transportIndex = pollingCoordinator.indexOf('_statusQuery', pollingSnapshotIndex);
    expect(reservationSnapshotIndex).toBeGreaterThanOrEqual(0);
    expect(convergenceIndex).toBeGreaterThan(reservationSnapshotIndex);
    expect(pollingSnapshotIndex).toBeGreaterThan(convergenceIndex);
    expect(transportIndex).toBeGreaterThan(pollingSnapshotIndex);
    expect(pollingCoordinator).toContain('hasPendingTransport ? Math.Max(0, budget - 1) : budget');

    assertContainsAll(razorEmitter, [
      'PendingCommandRowIdentityFor(row)',
      'CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>',
    ]);
    assertContainsAll(storyNineTwo, [
      'Status: done',
      'FrontComposer-owned pending-command row metadata',
      'Source-level wiring was proven',
      'Do not hide FC-NIP row identity in optional EventStore/domain-defined `ResultPayload`',
    ]);
    expect(indicatorState).toContain('DefaultLifetime = TimeSpan.FromSeconds(10)');
  });
});

const validateManifest = (candidate: unknown): ContractManifest => {
  requireRecord(candidate, ['schemaVersion', 'documents', 'tables']);
  requireCondition(candidate.schemaVersion === 1, 'schemaVersion must be 1');
  requireCondition(Array.isArray(candidate.documents) && candidate.documents.length > 0, 'documents must be non-empty');
  requireCondition(Array.isArray(candidate.tables), 'tables must be an array');

  const documents: ContractDocument[] = [];
  const documentPaths = new Set<string>();
  for (const value of candidate.documents) {
    requireRecord(value, ['contains', 'notContains', 'path']);
    const manifestPath = requireSafePath(value.path);
    requireCondition(!documentPaths.has(manifestPath), `duplicate document path: ${manifestPath}`);
    documentPaths.add(manifestPath);
    documents.push({
      path: manifestPath,
      contains: requireStringArray(value.contains, 'contains'),
      notContains: requireStringArray(value.notContains, 'notContains'),
    });
  }

  const tables: ContractTable[] = [];
  const tableIdentities = new Set<string>();
  for (const value of candidate.tables) {
    requireRecord(value, ['heading', 'path', 'rows']);
    const manifestPath = requireSafePath(value.path);
    requireCondition(documentPaths.has(manifestPath), `table path is not declared in documents: ${manifestPath}`);
    const heading = requireNormalizedString(value.heading, 'table heading');
    const identity = `${manifestPath}\n${heading}`;
    requireCondition(!tableIdentities.has(identity), `duplicate table identity: ${identity}`);
    tableIdentities.add(identity);
    requireCondition(Array.isArray(value.rows) && value.rows.length > 0, 'table rows must be non-empty');
    const rows = value.rows.map((row: unknown) => requireStringArray(row, 'table row'));
    requireCondition(rows.every((row) => row.length > 0 && row.length === rows[0].length), 'inconsistent table row widths');
    tables.push({ path: manifestPath, heading, rows });
  }

  return { schemaVersion: 1, documents, tables };
};

function requireRecord(value: unknown, properties: string[]): asserts value is Record<string, unknown> {
  requireCondition(typeof value === 'object' && value !== null && !Array.isArray(value), 'manifest node must be an object');
  requireCondition(
    JSON.stringify(Object.keys(value).sort()) === JSON.stringify([...properties].sort()),
    `manifest properties must be exactly: ${properties.join(', ')}`,
  );
}

function requireSafePath(value: unknown): string {
  const manifestPath = requireNormalizedString(value, 'path');
  requireCondition(!manifestPath.includes('\\') && !path.isAbsolute(manifestPath), `unsafe manifest path: ${manifestPath}`);
  requireCondition(manifestPath.split('/').every((part) => part.length > 0 && part !== '.' && part !== '..'), `traversal in manifest path: ${manifestPath}`);
  return manifestPath;
}

function requireStringArray(value: unknown, name: string): string[] {
  requireCondition(Array.isArray(value), `${name} must be an array`);
  const result = value.map((entry) => requireNormalizedString(entry, name));
  requireCondition(new Set(result).size === result.length, `${name} contains duplicates`);
  return result;
}

function requireNormalizedString(value: unknown, name: string): string {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string`);
  requireCondition(value === normalizeWhitespace(value), `${name} must be whitespace-normalized`);
  return value;
}

function requireCondition(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const parseTableRows = (document: string, heading: string): string[][] => {
  const lines = document.replace(/\r\n/g, '\n').split('\n');
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  expect(headingIndex, `${heading} heading is missing`).toBeGreaterThanOrEqual(0);
  const sectionEndOffset = lines.slice(headingIndex + 1).findIndex((line) => line.trimStart().startsWith('#'));
  const sectionEnd = sectionEndOffset < 0 ? lines.length : headingIndex + 1 + sectionEndOffset;
  const headerOffset = lines.slice(headingIndex + 1, sectionEnd).findIndex((line) => line.trimStart().startsWith('|'));
  expect(headerOffset, `${heading} section contains no table`).toBeGreaterThanOrEqual(0);
  const headerIndex = headingIndex + 1 + headerOffset;
  expect(headerIndex + 2, `${heading} table is truncated`).toBeLessThanOrEqual(sectionEnd);
  expect(/^\|[\s:|-]+\|$/.test(lines[headerIndex + 1].trim()), `${heading} separator is missing`).toBe(true);
  const width = splitTableRow(lines[headerIndex]).length;
  const rows: string[][] = [];
  for (let index = headerIndex + 2; index < sectionEnd && lines[index].trimStart().startsWith('|'); index += 1) {
    const row = splitTableRow(lines[index]);
    expect(row.length, `${heading} row width differs from its heading`).toBe(width);
    rows.push(row);
  }
  return rows;
};

const splitTableRow = (line: string): string[] =>
  line.trim().replace(/^\||\|$/g, '').split('|').map((cell) => cell.trim());

const assertContainsAll = (document: string, fragments: string[]): void => {
  for (const fragment of fragments) expect(document, `Missing source fragment: ${fragment}`).toContain(fragment);
};

const readRepoFile = async (relativePath: string): Promise<string> =>
  normalizeWhitespace(await readRawRepoFile(relativePath));

const readRawRepoFile = async (relativePath: string): Promise<string> =>
  readFile(path.join(REPO_ROOT, relativePath), 'utf8');

const normalizeWhitespace = (value: string): string => value.replace(/\s+/g, ' ');

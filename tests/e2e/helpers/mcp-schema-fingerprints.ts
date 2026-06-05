import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const sourceToolsAlgorithm = 'frontcomposer.schema.sha256.v1.sourcetools-blob';
const repoRoot = resolve(fileURLToPath(new URL('../../..', import.meta.url)));
const generatedManifestRelativePaths = [
  'samples/Counter/Counter.Domain/obj/Release/net10.0/generated/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/FrontComposerMcpManifest.g.cs',
  'samples/Counter/Counter.Domain/obj/Debug/net10.0/generated/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/FrontComposerMcpManifest.g.cs',
  'samples/Counter/Counter.Specimens.Domain/obj/Release/net10.0/generated/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/FrontComposerMcpManifest.g.cs',
  'samples/Counter/Counter.Specimens.Domain/obj/Debug/net10.0/generated/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/FrontComposerMcpManifest.g.cs',
];

export const staleSourceToolsFingerprintHeader = `${sourceToolsAlgorithm}:${'0'.repeat(64)}`;

export const getGeneratedSchemaFingerprintHeader = (descriptorName: string): string => {
  for (const relativePath of generatedManifestRelativePaths) {
    const manifestPath = resolve(repoRoot, relativePath);
    if (!existsSync(manifestPath)) {
      continue;
    }

    const source = readFileSync(manifestPath, 'utf8');
    const descriptorIndex = source.indexOf(`"${descriptorName}"`);
    if (descriptorIndex < 0) {
      continue;
    }

    const descriptorBlock = source.slice(descriptorIndex, descriptorIndex + 8_000);
    const fingerprint = /new SchemaFingerprint\("([^"]+)", "([a-f0-9]{64})"/u.exec(descriptorBlock);
    if (!fingerprint) {
      throw new Error(`Descriptor ${descriptorName} in ${relativePath} does not declare a schema fingerprint.`);
    }

    return `${fingerprint[1]}:${fingerprint[2]}`;
  }

  throw new Error(
    `Generated MCP descriptor ${descriptorName} was not found. Build samples/Counter/Counter.Web before running MCP schema E2E tests.`,
  );
};

// Preserve historical writer output byte-for-byte under Git-visible names.
// The repository ignores *.log; replay uses only these .log.txt copies.
import { createHash } from 'node:crypto';
import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const files = readdirSync(root).filter(name => name.endsWith('.log')).sort().map(name => {
  const bytes = readFileSync(join(root, name));
  const path = `${name}.txt`;
  writeFileSync(join(root, path), bytes);
  return { path, bytes: bytes.length, sha256: createHash('sha256').update(bytes).digest('hex') };
});
writeFileSync(join(root, 'support-logs.json'), JSON.stringify({ files }, null, 2) + '\n');
console.log(`Retained ${files.length} byte-identical support logs under nonignored .log.txt names.`);

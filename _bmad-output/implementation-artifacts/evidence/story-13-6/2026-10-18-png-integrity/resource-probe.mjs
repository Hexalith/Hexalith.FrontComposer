// Exercise the reviewed decoder's real expanded-data guard with a valid PNG.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { crc32, deflateSync, inflateSync } from 'node:zlib';

const repo = resolve(import.meta.dirname, '../../../../..');
const { PNG } = createRequire(resolve(repo, 'tests/e2e/package.json'))('playwright-core/lib/utilsBundle');
const source = readFileSync(resolve(repo, 'tests/e2e/scripts/a11y-state-evidence.mjs'), 'utf8');
let inflations = 0, decodes = 0;
const decode = new Function('PNG', 'crc32', 'inflateSync',
  `return (${source.slice(source.indexOf('function decodeSystemSnapshot'))});`)(
  { sync: { read(...args) { decodes++; return PNG.sync.read(...args); } } },
  crc32, (...args) => { inflations++; return inflateSync(...args); });
function chunk(type, data = Buffer.alloc(0)) {
  const bytes = Buffer.alloc(data.length + 12);
  bytes.writeUInt32BE(data.length); bytes.write(type, 4, 'ascii'); data.copy(bytes, 8);
  bytes.writeUInt32BE(crc32(bytes.subarray(4, -4)), bytes.length - 4);
  return bytes;
}
const header = Buffer.alloc(13);
header.writeUInt32BE(4096); header.writeUInt32BE(4096, 4); header[8] = 16; header[9] = 6;
const expandedBytes = 4096 * (1 + 4096 * 8);
const png = (() => Buffer.concat([
  Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', header),
  chunk('IDAT', deflateSync(Buffer.alloc(expandedBytes))), chunk('IEND'),
]))();
assert.equal(typeof global.gc, 'function', 'Run this probe with node --expose-gc');
global.gc();
const beforeRss = process.memoryUsage().rss, started = performance.now();
assert.throws(() => decode(png), /expanded image data exceeds the 33554432-byte decoding limit/);
const elapsedMs = performance.now() - started, afterRss = process.memoryUsage().rss;
assert.equal(inflations, 0); assert.equal(decodes, 0);
console.log(JSON.stringify({
  sourceSha256: createHash('sha256').update(Buffer.from(source)).digest('hex'),
  pngSha256: createHash('sha256').update(png).digest('hex'), encodedBytes: png.length,
  width: 4096, height: 4096, bitDepth: 16, expandedBytes,
  inflations, decodes, elapsedMs, beforeRss, afterRss,
}, null, 2));

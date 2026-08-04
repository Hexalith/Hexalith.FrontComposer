#!/usr/bin/env node
// Compute the semantic-release decision without running verify/prepare/publish plugins.
import semanticRelease from "semantic-release";

const result = await semanticRelease(
  {
    branches: ["main"],
    tagFormat: "v${version}",
    dryRun: true,
    ci: false,
    plugins: [
      ["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }],
      ["@semantic-release/release-notes-generator", { preset: "conventionalcommits" }],
    ],
  },
  {
    cwd: process.cwd(),
    env: process.env,
    stdout: process.stderr,
    stderr: process.stderr,
  },
);

const next = result === false ? null : result.nextRelease;
process.stdout.write(`${JSON.stringify({
  release_required: next !== null,
  version: next?.version ?? null,
})}\n`);

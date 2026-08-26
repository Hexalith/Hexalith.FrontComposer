#!/usr/bin/env bash

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
apphost="$repo_root/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
e2e_root="$repo_root/tests/e2e"
artifact_root="${FC_EPIC9_ARTIFACT_ROOT:-$repo_root/artifacts/epic-9}"
require_clean="${FC_EPIC9_REQUIRE_CLEAN:-false}"
playwright_results="$artifact_root/playwright-results"
html_report="$artifact_root/playwright-report"
raw_logs=""
raw_start=""
raw_describe=""
started=0

redact_json() {
  local source_file="$1"
  local destination_file="$2"
  jq '
    walk(
      if type == "object" then
        with_entries(
          if (.key | test("authorization|cookie|password|secret|token|headers|dashboard.*url"; "i")) then
            .value = "[REDACTED]"
          else
            .
          end)
      else
        .
      end)
  ' "$source_file" > "$destination_file"
}

cleanup() {
  local exit_code=$?
  for temporary_file in "$raw_logs" "$raw_start" "$raw_describe"; do
    if [[ -n "$temporary_file" && -f "$temporary_file" ]]; then
      rm -f -- "$temporary_file"
    fi
  done
  if [[ $started -eq 1 ]]; then
    aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null 2>&1 || true
  fi
  exit "$exit_code"
}
trap cleanup EXIT INT TERM

mkdir -p "$artifact_root"
if find "$artifact_root" -mindepth 1 -print -quit | grep -q .; then
  echo "Epic 9 artifact directory must be empty: $artifact_root" >&2
  exit 2
fi

case "$require_clean" in
  true)
    evidence_mode="final"
    ;;
  false)
    evidence_mode="development"
    ;;
  *)
    echo "FC_EPIC9_REQUIRE_CLEAN must be either true or false." >&2
    exit 2
    ;;
esac

candidate_commit="$(git -C "$repo_root" rev-parse HEAD)"
expected_candidate="${FC_EPIC9_EXPECTED_COMMIT:-$candidate_commit}"
if [[ ! "$expected_candidate" =~ ^[0-9a-f]{40}$ ]]; then
  echo "FC_EPIC9_EXPECTED_COMMIT must be a full lowercase commit SHA." >&2
  exit 2
fi
working_tree_dirty="false"
if [[ -n "$(git -C "$repo_root" status --porcelain)" ]]; then
  working_tree_dirty="true"
fi
if [[ "$candidate_commit" != "$expected_candidate" || ( "$require_clean" == "true" && "$working_tree_dirty" == "true" ) ]]; then
  jq -n \
    --arg candidateCommit "$candidate_commit" \
    --arg expectedCandidate "$expected_candidate" \
    --argjson workingTreeDirty "$working_tree_dirty" \
    --arg evidenceMode "$evidence_mode" \
    '{
      schemaVersion: 1,
      story: "9.8",
      candidateCommit: $candidateCommit,
      expectedCandidate: $expectedCandidate,
      workingTreeDirty: $workingTreeDirty,
      evidenceMode: $evidenceMode,
      failure: "Candidate preflight rejected the requested evidence run."
    }' > "$artifact_root/candidate-preflight.failed.json"
  echo "Epic 9 candidate preflight failed: HEAD=$candidate_commit expected=$expected_candidate dirty=$working_tree_dirty mode=$evidence_mode" >&2
  exit 2
fi
if [[ "$evidence_mode" == "development" ]]; then
  echo "Running explicit Epic 9 development evidence mode; dirty diagnostics are permitted and are not final acceptance." >&2
fi

aspire ps --format Json --non-interactive --nologo \
  | redact_json /dev/stdin "$artifact_root/apphost-preflight.json"
if grep -Fq 'Hexalith.FrontComposer.AppHost' "$artifact_root/apphost-preflight.json"; then
  echo "The FrontComposer AppHost is already running; refusing to stop or reuse an unrelated run." >&2
  exit 2
fi

start_mode="isolated-build"
raw_start="$(mktemp)"
if ! aspire start \
  --apphost "$apphost" \
  --isolated \
  --non-interactive \
  --format Json \
  --nologo > "$raw_start" 2>&1; then
  sed -E 's/(login\?t=)[^[:space:]"&]+/\1[REDACTED]/g' \
    "$raw_start" > "$artifact_root/apphost-start.failed.json"
  rm -f -- "$raw_start"
  raw_start="$(mktemp)"
  dotnet build "$apphost" \
    --configuration Debug \
    -m:1 \
    -p:BuildProjectReferences=false \
    -p:NuGetAudit=false \
    -p:CentralPackageTransitivePinningEnabled=false \
    > "$artifact_root/apphost-serialized-build.log" 2>&1
  start_mode="isolated-no-build-after-serialized-build"
  aspire start \
    --apphost "$apphost" \
    --isolated \
    --no-build \
    --non-interactive \
    --format Json \
    --nologo > "$raw_start" 2>&1
fi
started=1
sed -n '/^{/,$p' "$raw_start" | redact_json /dev/stdin "$artifact_root/apphost-start.json"
rm -f -- "$raw_start"
raw_start=""

aspire wait counter-web \
  --status up \
  --timeout 180 \
  --apphost "$apphost" \
  --non-interactive \
  --nologo > "$artifact_root/counter-web-wait.log" 2>&1
raw_describe="$(mktemp)"
aspire describe counter-web \
  --apphost "$apphost" \
  --format Json \
  --non-interactive \
  --nologo > "$raw_describe"
redact_json "$raw_describe" "$artifact_root/counter-web-describe.json"
rm -f -- "$raw_describe"
raw_describe=""

resource_name="$(jq -r '.resources[0].name // empty' "$artifact_root/counter-web-describe.json")"
base_url="$(jq -r '
  [.resources[0].urls[]?.url | select(test("^https?://"))]
  | (map(select(startswith("https://"))) + map(select(startswith("http://"))))
  | first // empty
' "$artifact_root/counter-web-describe.json")"
if [[ -z "$resource_name" || -z "$base_url" ]]; then
  echo "Could not discover the counter-web resource name and HTTP endpoint from Aspire describe output." >&2
  exit 2
fi

jq -n \
  --arg candidateCommit "$candidate_commit" \
  --arg baseUrl "$base_url" \
  --arg counterWebResource "$resource_name" \
  --arg startedAtUtc "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
  --arg startMode "$start_mode" \
  --arg evidenceMode "$evidence_mode" \
  --arg aspireVersion "$(aspire --version)" \
  --arg dotnetVersion "$(dotnet --version)" \
  --arg nodeVersion "$(node --version)" \
  --argjson workingTreeDirty "$working_tree_dirty" \
  '{
    schemaVersion: 1,
    story: "9.8",
    candidateCommit: $candidateCommit,
    workingTreeDirty: $workingTreeDirty,
    evidenceMode: $evidenceMode,
    baseUrl: $baseUrl,
    counterWebResource: $counterWebResource,
    startedAtUtc: $startedAtUtc,
    startMode: $startMode,
    toolVersions: {
      aspire: $aspireVersion,
      dotnet: $dotnetVersion,
      node: $nodeVersion
    },
    commands: [
      "aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --non-interactive --format Json --nologo",
      "aspire wait counter-web --status up --timeout 180 --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo",
      "aspire describe counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --non-interactive --nologo",
      "npm run test:epic-9",
      "aspire logs counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --tail 1000 --non-interactive --nologo",
      "aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo",
      "npm run validate:epic-9-artifacts -- <artifact-root>"
    ]
  }' > "$artifact_root/runtime-metadata.json"

set +e
(
  cd "$e2e_root"
  BASE_URL="$base_url" \
  PLAYWRIGHT_SKIP_WEBSERVER=1 \
  FC_E2E_CANDIDATE_COMMIT="$candidate_commit" \
  FC_E2E_TRACE=on \
  FC_E2E_OUTPUT_DIR="$playwright_results" \
  FC_E2E_HTML_REPORT_DIR="$html_report" \
  FC_E2E_JUNIT_PATH="$artifact_root/junit.xml" \
  npm run test:epic-9
)
playwright_exit=$?
set -e

raw_logs="$(mktemp)"
aspire logs counter-web \
  --apphost "$apphost" \
  --format Json \
  --tail 1000 \
  --non-interactive \
  --nologo > "$raw_logs"
redact_json "$raw_logs" "$artifact_root/counter-web-logs.redacted.json"

if [[ $playwright_exit -ne 0 ]]; then
  echo "Epic 9 Playwright proof failed; diagnostic artifacts were retained in $artifact_root" >&2
  exit "$playwright_exit"
fi

final_candidate_commit="$(git -C "$repo_root" rev-parse HEAD)"
final_working_tree_dirty="false"
if [[ -n "$(git -C "$repo_root" status --porcelain)" ]]; then
  final_working_tree_dirty="true"
fi
if [[ "$final_candidate_commit" != "$candidate_commit" \
  || ( "$require_clean" == "true" && "$final_working_tree_dirty" == "true" ) ]]; then
  jq -n \
    --arg candidateCommit "$candidate_commit" \
    --arg finalCandidateCommit "$final_candidate_commit" \
    --argjson finalWorkingTreeDirty "$final_working_tree_dirty" \
    --arg evidenceMode "$evidence_mode" \
    '{
      schemaVersion: 1,
      story: "9.8",
      candidateCommit: $candidateCommit,
      finalCandidateCommit: $finalCandidateCommit,
      finalWorkingTreeDirty: $finalWorkingTreeDirty,
      evidenceMode: $evidenceMode,
      failure: "Candidate integrity changed while the evidence run was active."
    }' > "$artifact_root/candidate-integrity.failed.json"
  echo "Epic 9 candidate integrity check failed: initial=$candidate_commit final=$final_candidate_commit dirty=$final_working_tree_dirty mode=$evidence_mode" >&2
  exit 2
fi

aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null
started=0
aspire ps --format Json --non-interactive --nologo \
  | redact_json /dev/stdin "$artifact_root/apphost-postflight.json"
if grep -Fq 'Hexalith.FrontComposer.AppHost' "$artifact_root/apphost-postflight.json"; then
  echo "The exact FrontComposer AppHost started by the proof is still running after cleanup." >&2
  exit 2
fi

(
  cd "$e2e_root"
  validator_arguments=("$artifact_root" --candidate "$candidate_commit")
  if [[ "$evidence_mode" == "development" ]]; then
    validator_arguments+=(--allow-dirty)
  fi
  npm run validate:epic-9-artifacts -- "${validator_arguments[@]}"
)
(
  cd "$artifact_root"
  find . -type f ! -name checksums.sha256 -print0 \
    | sort -z \
    | xargs -0 sha256sum > checksums.sha256
)

echo "Epic 9 live proof passed: $artifact_root"
echo "Discovered counter-web endpoint: $base_url"

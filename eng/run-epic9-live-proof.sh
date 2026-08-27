#!/usr/bin/env bash

set -euo pipefail

if ! repo_root="$(git rev-parse --show-toplevel)" || [[ -z "$repo_root" ]]; then
  echo "Could not resolve the repository root." >&2
  exit 2
fi

apphost="$repo_root/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
e2e_root="$repo_root/tests/e2e"
artifact_root="${FC_EPIC9_ARTIFACT_ROOT:-$repo_root/artifacts/epic-9}"
require_clean="${FC_EPIC9_REQUIRE_CLEAN:-false}"
playwright_results="$artifact_root/playwright-results"
html_report="$artifact_root/playwright-report"
raw_logs=""
raw_start=""
raw_describe=""
raw_process_list=""
started_apphost_path=""
started_apphost_pid=""
cleanup_required=0
proof_lock_held=0
preflight_apphost_absent=0
unknown_pid_cleanup_allowed=0

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

redact_lifecycle_text() {
  local source_file="$1"
  local destination_file="$2"
  sed -E 's/(login\?t=)[^[:space:]"&]+/\1[REDACTED]/g' "$source_file" > "$destination_file"
  if [[ ! -s "$destination_file" ]]; then
    printf '%s\n' 'Aspire command failed without console output.' > "$destination_file"
  fi
}

capture_process_list() {
  local destination_file="$1"
  if ! aspire ps --format Json --non-interactive --nologo > "$destination_file"; then
    echo "Aspire process discovery failed closed." >&2
    return 1
  fi
  if ! jq -e 'type == "array"' "$destination_file" >/dev/null; then
    echo "Aspire process discovery returned invalid JSON." >&2
    return 1
  fi
}

exact_apphost_count() {
  local process_list_file="$1"
  jq -r --arg apphostPath "$apphost" \
    '[.[] | select((.appHostPath // "") == $apphostPath)] | length' \
    "$process_list_file"
}

frontcomposer_apphost_count() {
  local process_list_file="$1"
  jq -r \
    '[.[] | select((.appHostPath // "") | endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"))] | length' \
    "$process_list_file"
}

owned_apphost_count() {
  local process_list_file="$1"
  jq -r \
    --arg apphostPath "$started_apphost_path" \
    --argjson apphostPid "$started_apphost_pid" \
    '[.[] | select((.appHostPath // "") == $apphostPath and (.appHostPid // 0) == $apphostPid)] | length' \
    "$process_list_file"
}

remove_temporary_files() {
  local temporary_file
  for temporary_file in "$raw_logs" "$raw_start" "$raw_describe" "$raw_process_list"; do
    if [[ -n "$temporary_file" && -f "$temporary_file" ]]; then
      rm -f -- "$temporary_file"
    fi
  done
}

cleanup() {
  local exit_code=$?
  local cleanup_process_list=""
  trap - EXIT INT TERM

  if [[ $cleanup_required -eq 1 ]]; then
    cleanup_process_list="$(mktemp)"
    if capture_process_list "$cleanup_process_list" >/dev/null 2>&1; then
      if [[ -n "$started_apphost_pid" \
        && "$(owned_apphost_count "$cleanup_process_list")" -eq 1 \
        && "$(exact_apphost_count "$cleanup_process_list")" -eq 1 ]]; then
        aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null 2>&1 || true
      elif [[ -z "$started_apphost_pid" \
        && $unknown_pid_cleanup_allowed -eq 1 \
        && $proof_lock_held -eq 1 \
        && $preflight_apphost_absent -eq 1 \
        && "$(exact_apphost_count "$cleanup_process_list")" -eq 1 ]]; then
        aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null 2>&1 || true
      fi
    fi
    rm -f -- "$cleanup_process_list"
  fi

  remove_temporary_files
  exit "$exit_code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

read_git_head() {
  local head
  if ! head="$(git -C "$repo_root" rev-parse HEAD)"; then
    echo "Git HEAD discovery failed closed." >&2
    return 1
  fi
  if [[ ! "$head" =~ ^[0-9a-f]{40}$ ]]; then
    echo "Git HEAD discovery did not return a full lowercase commit SHA." >&2
    return 1
  fi
  printf '%s' "$head"
}

read_git_status() {
  local status
  if ! status="$(git -C "$repo_root" status --porcelain)"; then
    echo "Git working-tree discovery failed closed." >&2
    return 1
  fi
  printf '%s' "$status"
}

inspect_failed_start() {
  local phase="$1"
  local exact_count
  local frontcomposer_count

  raw_process_list="$(mktemp)"
  if ! capture_process_list "$raw_process_list"; then
    cleanup_required=1
    if [[ -z "$started_apphost_pid" \
      && $proof_lock_held -eq 1 \
      && $preflight_apphost_absent -eq 1 ]]; then
      unknown_pid_cleanup_allowed=1
    fi
    echo "Could not determine whether the failed $phase start left a partial AppHost; the EXIT trap will recheck ownership and fallback is refused." >&2
    return 1
  fi
  exact_count="$(exact_apphost_count "$raw_process_list")"
  frontcomposer_count="$(frontcomposer_apphost_count "$raw_process_list")"
  if [[ "$exact_count" -gt 0 ]]; then
    if [[ -n "$started_apphost_pid" && "$(owned_apphost_count "$raw_process_list")" -ne 1 ]]; then
      cleanup_required=0
      unknown_pid_cleanup_allowed=0
      echo "The failed $phase start inspection found a different AppHost PID; refusing to stop it." >&2
      return 1
    fi
    if [[ -z "$started_apphost_pid" \
      && ( $proof_lock_held -ne 1 \
        || $preflight_apphost_absent -ne 1 \
        || "$exact_count" -ne 1 ) ]]; then
      cleanup_required=0
      unknown_pid_cleanup_allowed=0
      echo "The failed $phase start cannot prove exclusive unknown-PID ownership; refusing to stop it." >&2
      return 1
    fi
    cleanup_required=1
    started_apphost_path="$apphost"
    if [[ -z "$started_apphost_pid" ]]; then
      unknown_pid_cleanup_allowed=1
    else
      unknown_pid_cleanup_allowed=0
    fi
    echo "The failed $phase start left a partial FrontComposer AppHost; stopping it and refusing fallback." >&2
    if ! aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null; then
      return 1
    fi
    rm -f -- "$raw_process_list"
    raw_process_list="$(mktemp)"
    if ! capture_process_list "$raw_process_list"; then
      return 1
    fi
    if [[ "$(exact_apphost_count "$raw_process_list")" -ne 0 ]]; then
      return 1
    fi
    cleanup_required=0
    unknown_pid_cleanup_allowed=0
    return 1
  fi
  if [[ "$frontcomposer_count" -gt 0 ]]; then
    echo "An unrelated FrontComposer AppHost appeared after the failed $phase start; refusing fallback without stopping it." >&2
    return 1
  fi
  rm -f -- "$raw_process_list"
  raw_process_list=""
  return 0
}

parse_and_own_started_apphost() {
  local parsed_start
  parsed_start="$(mktemp)"
  sed -n '/^[[:space:]]*{/,$p' "$raw_start" > "$parsed_start"
  if ! jq -e 'type == "object"' "$parsed_start" >/dev/null; then
    rm -f -- "$parsed_start"
    echo "Aspire start did not return a valid JSON object." >&2
    return 1
  fi
  if ! started_apphost_path="$(jq -er '.appHostPath | strings | select(length > 0)' "$parsed_start")" \
    || ! started_apphost_pid="$(jq -er '.appHostPid | numbers | select(. > 0 and floor == .)' "$parsed_start")"; then
    rm -f -- "$parsed_start"
    echo "Aspire start JSON did not identify the started AppHost path and PID." >&2
    return 1
  fi
  if [[ "$started_apphost_path" != "$apphost" ]]; then
    rm -f -- "$parsed_start"
    echo "Aspire start returned an unexpected AppHost path; refusing ownership." >&2
    return 3
  fi
  redact_json "$parsed_start" "$artifact_root/apphost-start.json"
  rm -f -- "$parsed_start"

  cleanup_required=1
  unknown_pid_cleanup_allowed=0
  raw_process_list="$(mktemp)"
  if ! capture_process_list "$raw_process_list"; then
    return 1
  fi
  if [[ "$(exact_apphost_count "$raw_process_list")" -ne 1 \
    || "$(owned_apphost_count "$raw_process_list")" -ne 1 ]]; then
    cleanup_required=0
    unknown_pid_cleanup_allowed=0
    echo "Aspire process discovery did not uniquely correlate the AppHost path and PID returned by start; refusing reuse or cleanup." >&2
    return 3
  fi
  rm -f -- "$raw_process_list"
  raw_process_list=""
}

mkdir -p "$artifact_root"
if find "$artifact_root" -mindepth 1 -print -quit | grep -q .; then
  echo "Epic 9 artifact directory must be empty: $artifact_root" >&2
  exit 2
fi

case "$require_clean" in
  true) evidence_mode="final" ;;
  false) evidence_mode="development" ;;
  *)
    echo "FC_EPIC9_REQUIRE_CLEAN must be either true or false." >&2
    exit 2
    ;;
esac

if ! candidate_commit="$(read_git_head)"; then
  exit 2
fi
expected_candidate="${FC_EPIC9_EXPECTED_COMMIT:-$candidate_commit}"
if [[ ! "$expected_candidate" =~ ^[0-9a-f]{40}$ ]]; then
  echo "FC_EPIC9_EXPECTED_COMMIT must be a full lowercase commit SHA." >&2
  exit 2
fi
if ! initial_git_status="$(read_git_status)"; then
  exit 2
fi
working_tree_dirty="false"
if [[ -n "$initial_git_status" ]]; then
  working_tree_dirty="true"
fi
if [[ "$candidate_commit" != "$expected_candidate" || ( "$require_clean" == "true" && "$working_tree_dirty" == "true" ) ]]; then
  jq -n \
    --arg candidateCommit "$candidate_commit" \
    --arg expectedCandidate "$expected_candidate" \
    --argjson workingTreeDirty "$working_tree_dirty" \
    --arg evidenceMode "$evidence_mode" \
    '{schemaVersion: 1, story: "9.8", candidateCommit: $candidateCommit,
      expectedCandidate: $expectedCandidate, workingTreeDirty: $workingTreeDirty,
      evidenceMode: $evidenceMode,
      failure: "Candidate preflight rejected the requested evidence run."}' \
    > "$artifact_root/candidate-preflight.failed.json"
  echo "Epic 9 candidate preflight failed: HEAD=$candidate_commit expected=$expected_candidate dirty=$working_tree_dirty mode=$evidence_mode" >&2
  exit 2
fi
if [[ "$evidence_mode" == "development" ]]; then
  echo "Running explicit Epic 9 development evidence mode; dirty diagnostics are permitted and are not final acceptance." >&2
fi

# Serialize proof ownership per AppHost before lifecycle preflight when flock exists.
if command -v flock >/dev/null 2>&1; then
  proof_lock_root="${FC_EPIC9_LOCK_ROOT:-/tmp}"
  if ! apphost_lock_id="$(printf '%s' "$apphost" | sha256sum | cut -d' ' -f1)" \
    || [[ ! "$apphost_lock_id" =~ ^[0-9a-f]{64}$ ]]; then
    echo "Could not derive the Epic 9 AppHost proof lock identifier." >&2
    exit 2
  fi
  exec {proof_lock_fd}>"$proof_lock_root/hexalith-epic9-$apphost_lock_id.lock"
  if ! flock -n "$proof_lock_fd"; then
    echo "Another Epic 9 proof owns the FrontComposer AppHost lock; refusing concurrent lifecycle work." >&2
    exit 2
  fi
  proof_lock_held=1
fi

raw_process_list="$(mktemp)"
if ! capture_process_list "$raw_process_list"; then
  exit 2
fi
redact_json "$raw_process_list" "$artifact_root/apphost-preflight.json"
if [[ "$(frontcomposer_apphost_count "$raw_process_list")" -ne 0 ]]; then
  echo "A FrontComposer AppHost is already running; refusing to stop or reuse an unrelated run." >&2
  exit 2
fi
preflight_apphost_absent=1
rm -f -- "$raw_process_list"
raw_process_list=""

start_mode="isolated-build"
raw_start="$(mktemp)"
if ! aspire start \
  --apphost "$apphost" \
  --isolated \
  --non-interactive \
  --format Json \
  --nologo > "$raw_start" 2>&1; then
  redact_lifecycle_text "$raw_start" "$artifact_root/apphost-start.failed.json"
  if ! inspect_failed_start "initial"; then
    exit 2
  fi
  rm -f -- "$raw_start"
  raw_start=""
  if ! dotnet build "$apphost" \
    --configuration Debug \
    -m:1 \
    -p:BuildProjectReferences=false \
    -p:NuGetAudit=false \
    -p:CentralPackageTransitivePinningEnabled=false \
    > "$artifact_root/apphost-serialized-build.log" 2>&1; then
    echo "Serialized AppHost fallback build failed." >&2
    exit 2
  fi
  if [[ ! -s "$artifact_root/apphost-serialized-build.log" ]]; then
    printf '%s\n' 'Serialized AppHost fallback build completed without console output.' \
      > "$artifact_root/apphost-serialized-build.log"
  fi
  start_mode="isolated-no-build-after-serialized-build"
  raw_start="$(mktemp)"
  if ! aspire start \
    --apphost "$apphost" \
    --isolated \
    --no-build \
    --non-interactive \
    --format Json \
    --nologo > "$raw_start" 2>&1; then
    {
      printf '\n%s\n' 'Fallback --no-build start output:'
      sed -E 's/(login\?t=)[^[:space:]"&]+/\1[REDACTED]/g' "$raw_start"
    } >> "$artifact_root/apphost-start.failed.json"
    if ! inspect_failed_start "fallback"; then
      exit 2
    fi
    echo "Fallback AppHost start failed without leaving an owned partial run." >&2
    exit 2
  fi
fi

parse_result=0
parse_and_own_started_apphost || parse_result=$?
if [[ $parse_result -ne 0 ]]; then
  if [[ $parse_result -eq 3 ]]; then
    exit 2
  fi
  if ! inspect_failed_start "successful-but-unparseable"; then
    exit 2
  fi
  exit 2
fi
rm -f -- "$raw_start"
raw_start=""

aspire wait counter-web --status up --timeout 180 --apphost "$apphost" \
  --non-interactive --nologo > "$artifact_root/counter-web-wait.log" 2>&1
raw_describe="$(mktemp)"
aspire describe counter-web --apphost "$apphost" --format Json \
  --non-interactive --nologo > "$raw_describe"
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

commands=(
  "aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --non-interactive --format Json --nologo"
)
if [[ "$start_mode" == "isolated-no-build-after-serialized-build" ]]; then
  commands+=(
    "dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false"
    "aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --no-build --non-interactive --format Json --nologo"
  )
fi
commands+=(
  "aspire wait counter-web --status up --timeout 180 --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo"
  "aspire describe counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --non-interactive --nologo"
  "npm run test:epic-9"
  "aspire logs counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --tail 1000 --non-interactive --nologo"
  "aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo"
  "generate complete sorted checksums.sha256"
)
if [[ "$evidence_mode" == "development" ]]; then
  commands+=("npm run validate:epic-9-artifacts -- <artifact-root> --candidate <candidate> --allow-dirty")
else
  commands+=("npm run validate:epic-9-artifacts -- <artifact-root> --candidate <candidate>")
fi
commands_json="$(printf '%s\n' "${commands[@]}" | jq -R -s 'split("\n")[:-1]')"

jq -n \
  --arg candidateCommit "$candidate_commit" \
  --arg baseUrl "$base_url" \
  --arg counterWebResource "$resource_name" \
  --arg counterWebLogResource "counter-web" \
  --arg startedAtUtc "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
  --arg startMode "$start_mode" \
  --arg evidenceMode "$evidence_mode" \
  --arg aspireVersion "$(aspire --version)" \
  --arg dotnetVersion "$(dotnet --version)" \
  --arg nodeVersion "$(node --version)" \
  --argjson workingTreeDirty "$working_tree_dirty" \
  --argjson commands "$commands_json" \
  '{schemaVersion: 1, story: "9.8", candidateCommit: $candidateCommit,
    workingTreeDirty: $workingTreeDirty, evidenceMode: $evidenceMode, baseUrl: $baseUrl,
    counterWebResource: $counterWebResource, counterWebLogResource: $counterWebLogResource,
    startedAtUtc: $startedAtUtc, startMode: $startMode,
    toolVersions: {aspire: $aspireVersion, dotnet: $dotnetVersion, node: $nodeVersion},
    commands: $commands}' > "$artifact_root/runtime-metadata.json"

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
aspire logs counter-web --apphost "$apphost" --format Json --tail 1000 \
  --non-interactive --nologo > "$raw_logs"
redact_json "$raw_logs" "$artifact_root/counter-web-logs.redacted.json"
rm -f -- "$raw_logs"
raw_logs=""

if [[ $playwright_exit -ne 0 ]]; then
  echo "Epic 9 Playwright proof failed; diagnostic artifacts were retained in $artifact_root" >&2
  exit "$playwright_exit"
fi

if ! final_candidate_commit="$(read_git_head)" || ! final_git_status="$(read_git_status)"; then
  exit 2
fi
final_working_tree_dirty="false"
if [[ -n "$final_git_status" ]]; then
  final_working_tree_dirty="true"
fi
if [[ "$final_candidate_commit" != "$candidate_commit" || "$final_git_status" != "$initial_git_status" ]]; then
  jq -n \
    --arg candidateCommit "$candidate_commit" \
    --arg finalCandidateCommit "$final_candidate_commit" \
    --argjson finalWorkingTreeDirty "$final_working_tree_dirty" \
    --arg evidenceMode "$evidence_mode" \
    '{schemaVersion: 1, story: "9.8", candidateCommit: $candidateCommit,
      finalCandidateCommit: $finalCandidateCommit,
      finalWorkingTreeDirty: $finalWorkingTreeDirty, evidenceMode: $evidenceMode,
      failure: "Candidate integrity changed while the evidence run was active."}' \
    > "$artifact_root/candidate-integrity.failed.json"
  echo "Epic 9 candidate integrity check failed: initial=$candidate_commit final=$final_candidate_commit dirty=$final_working_tree_dirty mode=$evidence_mode" >&2
  exit 2
fi

raw_process_list="$(mktemp)"
if ! capture_process_list "$raw_process_list"; then
  exit 2
fi
if [[ "$(exact_apphost_count "$raw_process_list")" -ne 1 \
  || "$(owned_apphost_count "$raw_process_list")" -ne 1 ]]; then
  cleanup_required=0
  echo "The running AppHost no longer matches the path and PID started by this proof; refusing to stop an unrelated run." >&2
  exit 2
fi
rm -f -- "$raw_process_list"
raw_process_list=""

if ! aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null; then
  echo "Normal Aspire cleanup failed; the EXIT trap will recheck exact PID ownership before retrying Aspire stop." >&2
  exit 2
fi
raw_process_list="$(mktemp)"
if ! capture_process_list "$raw_process_list"; then
  echo "Aspire postflight failed; the EXIT trap will recheck exact PID ownership before any Aspire stop retry." >&2
  exit 2
fi
redact_json "$raw_process_list" "$artifact_root/apphost-postflight.json"
remaining_exact_count="$(exact_apphost_count "$raw_process_list")"
remaining_owned_count="$(owned_apphost_count "$raw_process_list")"
remaining_frontcomposer_count="$(frontcomposer_apphost_count "$raw_process_list")"
if [[ "$remaining_owned_count" -ne 0 ]]; then
  echo "The exact FrontComposer AppHost started by the proof is still running after cleanup; the EXIT trap will retry Aspire stop." >&2
  exit 2
fi
if [[ "$remaining_exact_count" -ne 0 ]]; then
  cleanup_required=0
  unknown_pid_cleanup_allowed=0
  echo "A different FrontComposer AppHost now owns the proof AppHost path; refusing to stop it." >&2
  exit 2
fi
cleanup_required=0
unknown_pid_cleanup_allowed=0
if [[ "$remaining_frontcomposer_count" -ne 0 ]]; then
  echo "An unrelated FrontComposer AppHost appeared during the proof; it was not stopped." >&2
  exit 2
fi
rm -f -- "$raw_process_list"
raw_process_list=""

if ! symlink_path="$(find "$artifact_root" -type l -print -quit)"; then
  echo "Could not enumerate artifact symlinks." >&2
  exit 2
fi
if [[ -n "$symlink_path" ]]; then
  echo "Epic 9 artifacts must not contain symlinks: $symlink_path" >&2
  exit 2
fi
(
  cd "$artifact_root"
  if ! find . -type f -print >/dev/null; then
    echo "Could not enumerate regular Epic 9 artifacts." >&2
    exit 2
  fi
  mapfile -d '' checksum_files < <(
    find . -type f ! -path './checksums.sha256' -printf '%P\0' | LC_ALL=C sort -z
  )
  if [[ ${#checksum_files[@]} -eq 0 ]]; then
    echo "Epic 9 proof produced no files to checksum." >&2
    exit 2
  fi
  for checksum_file in "${checksum_files[@]}"; do
    sha256sum -- "$checksum_file"
  done > checksums.sha256
)

(
  cd "$e2e_root"
  validator_arguments=("$artifact_root" --candidate "$candidate_commit")
  if [[ "$evidence_mode" == "development" ]]; then
    validator_arguments+=(--allow-dirty)
  fi
  npm run validate:epic-9-artifacts -- "${validator_arguments[@]}"
)

echo "Epic 9 live proof passed: $artifact_root"
echo "Discovered counter-web endpoint: $base_url"

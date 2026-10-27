#!/usr/bin/env bash
set -euo pipefail
umask 077

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
apphost="$repo_root/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
aspire_args=(--apphost "$apphost" --non-interactive --nologo)
export HexalithFrontComposerFromSource=true

fail() { printf '::error::%s\n' "$1" >&2; }
for prerequisite in aspire jq curl flock sha256sum; do
  if ! command -v "$prerequisite" >/dev/null 2>&1; then
    fail "Invalid-bearer verification requires $prerequisite."
    exit 2
  fi
done
scratch="$(mktemp -d)"
started_pid=""
start_attempted=0
ownership_rejected=0
proof_lock_held=0

capture_processes() {
  aspire ps --format Json --non-interactive --nologo > "$scratch/processes.json" 2> "$scratch/processes.err" &&
    jq -e 'type == "array"' "$scratch/processes.json" >/dev/null 2>&1
}
exact_count() {
  jq --arg apphost "$apphost" '[.[] | select(.appHostPath == $apphost)] | length' "$scratch/processes.json"
}
owned_count() {
  jq --arg apphost "$apphost" --argjson pid "$started_pid" \
    '[.[] | select(.appHostPath == $apphost and .appHostPid == $pid)] | length' "$scratch/processes.json"
}
cleanup() {
  local result=$?
  trap - EXIT INT TERM
  if [[ $start_attempted -eq 1 && $ownership_rejected -eq 0 ]]; then
    if ! capture_processes; then
      fail 'Cleanup cannot establish AppHost ownership; no unverified process was stopped.'
      result=1
    elif [[ "$(exact_count)" -gt 0 ]]; then
      # A failed start may leave a partial process without JSON. The shared lock and
      # absent preflight permit cleanup only when exactly one target appeared.
      if [[ -z "$started_pid" && "$(exact_count)" -eq 1 ]]; then
        started_pid="$(jq -r --arg apphost "$apphost" '.[] | select(.appHostPath == $apphost) | .appHostPid' "$scratch/processes.json")"
      fi
      if [[ "$started_pid" =~ ^[1-9][0-9]*$ && "$(exact_count)" -eq 1 && "$(owned_count)" -eq 1 ]]; then
        if ! aspire stop "${aspire_args[@]}" > "$scratch/stop.log" 2>&1 ||
          ! capture_processes || [[ "$(exact_count)" -ne 0 ]]; then
          fail 'Owned AppHost cleanup failed or could not be verified.'
          result=1
        fi
      else
        fail 'AppHost ownership changed; refusing to stop another process.'
        result=1
      fi
    fi
  fi
  rm -rf -- "$scratch"
  if [[ $proof_lock_held -eq 1 ]]; then
    # Detached children may retain the descriptor; release only our acquired flock
    # after the owned AppHost cleanup, rather than relying on process exit.
    if ! flock -u "$proof_lock_fd"; then
      fail 'Could not release the owned AppHost lifecycle lock.'
      result=1
    fi
    exec {proof_lock_fd}>&-
    proof_lock_held=0
  fi
  exit "$result"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

# Share the existing live-proof lifecycle lock without sourcing its executable.
apphost_lock_id="$(printf '%s' "$apphost" | sha256sum | cut -d' ' -f1)"
exec {proof_lock_fd}>"${FC_EPIC9_LOCK_ROOT:-/tmp}/hexalith-epic9-$apphost_lock_id.lock"
if ! flock -n "$proof_lock_fd"; then
  fail 'Another proof owns the AppHost lifecycle lock.'
  exit 2
fi
proof_lock_held=1
if ! capture_processes; then
  fail 'Aspire process discovery failed before startup.'
  exit 2
fi
if [[ "$(jq '[.[] | select((.appHostPath // "") | endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"))] | length' "$scratch/processes.json")" -ne 0 ]]; then
  fail 'A FrontComposer AppHost is already running; verification requires an owned start.'
  exit 2
fi

start_attempted=1
if ! aspire start "${aspire_args[@]}" --isolated --no-build --format Json > "$scratch/start.log" 2>&1; then
  fail 'Aspire start failed; invalid-bearer verification did not execute.'
  exit 2
fi
sed -n '/^[[:space:]]*{/,$p' "$scratch/start.log" > "$scratch/start.json"
if ! jq -e -s 'length == 1 and (.[0] | type == "object")' "$scratch/start.json" >/dev/null 2>&1; then
  fail 'Aspire start returned no valid ownership record.'
  exit 2
fi
if ! jq -e --arg apphost "$apphost" \
  '.appHostPath == $apphost and (.appHostPid | type == "number" and . > 0 and floor == .)' "$scratch/start.json" >/dev/null 2>&1; then
  ownership_rejected=1
  fail 'Aspire start returned an unexpected AppHost identity; ownership was rejected.'
  exit 2
fi
started_pid="$(jq -r '.appHostPid' "$scratch/start.json")"
if ! capture_processes; then
  fail 'Aspire process discovery failed after startup.'
  exit 2
fi
if [[ "$(exact_count)" -ne 1 || "$(owned_count)" -ne 1 ]]; then
  ownership_rejected=1
  fail 'Aspire process discovery did not confirm the started AppHost identity.'
  exit 2
fi
for resource in security eventstore; do
  if ! aspire wait "$resource" --status healthy --timeout 300 "${aspire_args[@]}" > "$scratch/wait.log" 2>&1; then
    fail "Aspire readiness failed for $resource; invalid-bearer verification did not execute."
    exit 2
  fi
done
if [[ "${FrontComposerVerification__Enabled:-}" == true ]]; then
  if [[ -z "${FrontComposerVerification__Directory:-}" ]] ||
    ! python3 "$repo_root/eng/verify-isolated-readiness.py" check "$FrontComposerVerification__Directory" > "$scratch/application-health.log" 2>&1; then
    fail 'Disposable application health/readiness verification failed.'
    exit 2
  fi
  printf 'Disposable application health/readiness and ordinary gateway workers verified.\n'
fi
if ! aspire describe eventstore "${aspire_args[@]}" --format Json > "$scratch/describe.json" 2> "$scratch/describe.err"; then
  fail 'Aspire endpoint discovery failed.'
  exit 2
fi
# Only advertised, non-management loopback application endpoints are eligible. Never
# echo arbitrary discovery data, response bodies, curl stderr, or credential fields.
if ! jq -er '
  [.resources[] | select(.displayName == "eventstore" or .name == "eventstore" or (.name | startswith("eventstore-"))) ]
  | select(length == 1) | .[0]
  | [.urls[]? | select(.name != "management") | .url
      | select(test("^https?://(localhost|127\\.0\\.0\\.1|\\[::1\\]):[0-9]+/?$"))]
  | unique | (map(select(startswith("https://"))) + map(select(startswith("http://"))))
  | select(length > 0) | .[]' "$scratch/describe.json" > "$scratch/endpoints" 2> "$scratch/describe.err"; then
  fail 'Aspire endpoint discovery returned no unambiguous loopback application endpoints.'
  exit 2
fi
mapfile -t bases < "$scratch/endpoints"
failed=0
probe() {
  local method="$1" path="$2" base code curl_exit endpoint=0 reason
  local request=(--request "$method" --header 'Authorization: Bearer invalid-ci-token')
  if [[ "$method" == POST ]]; then request+=(--header 'Content-Type: application/json' --data '{}'); fi
  for base in "${bases[@]}"; do
    endpoint=$((endpoint + 1))
    curl_exit=0
    # Disable inherited curl configuration/proxies for Aspire's local endpoints.
    # Do not follow redirects: a redirect is an HTTP failure, never a fallback cue.
    code="$(curl --disable --noproxy '*' --silent --show-error --insecure --connect-timeout 5 --max-time 10 \
      --output /dev/null --write-out '%{http_code}' "${request[@]}" "${base%/}$path" 2> "$scratch/curl.err")" || curl_exit=$?
    if [[ $curl_exit -eq 0 && ( "$code" == 401 || "$code" == 403 ) ]]; then
      printf '%s %s rejected the invalid bearer with %s (endpoint %d).\n' "$method" "$path" "$code" "$endpoint"
      return
    fi
    if [[ $curl_exit -ne 0 && "$code" == 000 ]]; then
      case "$curl_exit" in
        5|6) reason='name resolution failed' ;;
        7) reason='connection failed' ;;
        28) reason='request timed out' ;;
        35|60) reason='TLS transport failed' ;;
        52|55|56) reason='connection ended without a complete response' ;;
        *) reason='curl execution failed' ;;
      esac
      printf '%s %s endpoint %d: curl exit %d (%s); no HTTP response.\n' "$method" "$path" "$endpoint" "$curl_exit" "$reason" >&2
      case "$curl_exit" in 5|6|7|28|35|52|55|56|60) continue ;; esac
    fi
    # Fixed status vocabulary prevents arbitrary stdout from entering diagnostics.
    [[ "$code" =~ ^[1-5][0-9][0-9]$ ]] || code='invalid'
    fail "$method $path endpoint $endpoint returned HTTP $code with curl exit $curl_exit; expected completed 401 or 403."
    failed=1
    return
  done
  fail "$method $path: all advertised endpoints were unreachable; no authentication result."
  failed=1
}
probe POST /api/v1/commands
probe GET /api/v1/commands/status/01K8AAAAAAAAAAAAAAAAAAAAAA
probe POST /api/v1/queries
probe POST '/hubs/projection-changes/negotiate?negotiateVersion=1'
exit "$failed"

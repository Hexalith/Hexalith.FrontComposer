#!/usr/bin/env bash
set -euo pipefail
umask 077

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
apphost="$repo_root/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
aspire_args=(--apphost "$apphost" --non-interactive --nologo)
sanitizer="$repo_root/eng/sanitize-invalid-bearer-diagnostics.jq"
diagnostics_dir="${FC_INVALID_BEARER_DIAGNOSTICS_DIR:-$repo_root/artifacts/invalid-bearer}"
export HexalithFrontComposerFromSource=true

fail() { printf '::error::%s\n' "$1" >&2; }
for prerequisite in aspire jq curl flock sha256sum timeout; do
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
startup_confirmed=0
start_exit=null
phase=preflight
diagnostics_ownership=not-requested
cleanup_outcome=not-required
lock_outcome=not-held
printf '[]\n' > "$scratch/waits.json"
: > "$scratch/probes.jsonl"
: > "$scratch/resources.jsonl"
printf '{"launchOutputStatus":"not-started","consoleSignals":{},"detachedLog":{"status":"record-unavailable"}}\n' > "$scratch/startup-summary.json"

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
# Read-only diagnostics get a separate time and output bound. Raw data stays in
# the private scratch directory and only an allowlisted projection can survive.
bounded_aspire() {
  local destination="$1" command_exit=0
  shift
  (ulimit -f 128; timeout --signal=TERM --kill-after=2s 10s aspire "$@") \
    > "$destination" 2> "$scratch/diagnostic.err" || command_exit=$?
  # Some tools swallow a failed stdout write and exit zero. A full capped file
  # remains incomplete evidence, regardless of the command's reported status.
  if [[ "$(wc -c < "$destination")" -ge 131072 || "$(wc -c < "$scratch/diagnostic.err")" -ge 131072 ]]; then
    return 125
  fi
  return "$command_exit"
}
diagnostic_owner() {
  if ! bounded_aspire "$scratch/processes.json" ps --format Json --non-interactive --nologo ||
    ! jq -e 'type == "array"' "$scratch/processes.json" >/dev/null 2>&1; then
    diagnostics_ownership=unavailable
    return 1
  fi
  if [[ "$(exact_count)" -ne 1 || "$(owned_count)" -ne 1 ]]; then
    diagnostics_ownership=rejected
    return 1
  fi
  diagnostics_ownership=verified
}
capture_startup_diagnostics() {
  local child_log parent child_name child_mode parent_mode
  [[ $start_attempted -eq 1 ]] || return 0
  # Launch stdout is ours even when the child has already exited. Never gate its
  # summary, or this invocation's static log, on live resource discovery.
  if timeout --signal=TERM --kill-after=2s 5s tail -c 131072 -- "$scratch/start.log" \
    > "$scratch/startup-tail" 2> "$scratch/diagnostic.err"; then
    tail -120 "$scratch/startup-tail" | jq -Rrs --arg mode startup -f "$sanitizer" \
      > "$scratch/startup-summary.json" 2> "$scratch/sanitize.err" || return 1
    jq '. + {launchOutputStatus: "available", detachedLog: {status: "record-unavailable"}}' \
      "$scratch/startup-summary.json" > "$scratch/startup.next" || return 1
    mv "$scratch/startup.next" "$scratch/startup-summary.json"
  else
    printf '{"launchOutputStatus":"unavailable","consoleSignals":{},"detachedLog":{"status":"record-unavailable"}}\n' > "$scratch/startup-summary.json"
    return 0
  fi
  sed -n '/^[[:space:]]*{/,$p' "$scratch/startup-tail" > "$scratch/startup-record.json"
  if ! jq -e -s --arg apphost "$apphost" 'length == 1 and (.[0] | .appHostPath == $apphost
    and (.appHostPid | type == "number" and . > 0 and floor == .) and (.logFile | type == "string"))' \
    "$scratch/startup-record.json" >/dev/null 2>&1; then return 0; fi
  child_log="$(jq -r '.logFile' "$scratch/startup-record.json")"
  child_name="${child_log##*/}"
  parent="${child_log%/*}"
  child_mode="$(stat -c %a -- "$child_log" 2> "$scratch/diagnostic.err")" || child_mode=""
  parent_mode="$(stat -c %a -- "$parent" 2> "$scratch/diagnostic.err")" || parent_mode=""
  # Aspire 13.6 generates this child filename and records its exact path in the
  # private launcher log. A launch-record path alone is never permission to read.
  if ! timeout --signal=TERM --kill-after=2s 5s tail -c 131072 -- "$scratch/launch-cli.log" \
    > "$scratch/launcher-tail" 2> "$scratch/diagnostic.err"; then
    printf '{"status":"unavailable"}\n' > "$scratch/detached-summary.json"
  elif [[ "$child_log" != /* || ! "$child_name" =~ ^cli_[0-9]{8}T[0-9]{9}_detach-child_[0-9a-f]{32}\.log$ ]] ||
    ! sed -n '/Spawning child CLI:/p' "$scratch/launcher-tail" | grep -F -- "--log-file $child_log " >/dev/null; then
    printf '{"status":"unsafe-path"}\n' > "$scratch/detached-summary.json"
  elif [[ ! -e "$child_log" && ! -L "$child_log" ]]; then
    printf '{"status":"unavailable"}\n' > "$scratch/detached-summary.json"
  elif [[ ! -f "$child_log" || ! -O "$child_log" || -L "$child_log" || ! "$child_log" -nt "$scratch/launch-started" ]] ||
    [[ "$(realpath -e -- "$child_log" 2> "$scratch/diagnostic.err")" != "$child_log" ]] ||
    [[ ! "$child_mode" =~ ^[0-7]{3,4}$ || ! "$parent_mode" =~ ^[0-7]{3,4}$ || ! -O "$parent" ]] ||
    (( (8#$child_mode & 077) != 0 || (8#$parent_mode & 022) != 0 )); then
    printf '{"status":"unsafe-path"}\n' > "$scratch/detached-summary.json"
  else
    parent="${child_log%/*}"
    while [[ "$parent" != / ]]; do
      [[ ! -L "$parent" ]] || break
      parent="${parent%/*}"
      [[ -n "$parent" ]] || parent=/
    done
    if [[ "$parent" != / ]]; then
      printf '{"status":"unsafe-path"}\n' > "$scratch/detached-summary.json"
    elif timeout --signal=TERM --kill-after=2s 5s tail -c 131072 -- "$child_log" \
      > "$scratch/detached-tail" 2> "$scratch/diagnostic.err"; then
      tail -120 "$scratch/detached-tail" | jq -Rrs --arg mode startup -f "$sanitizer" \
        | jq '. + {status: "available"}' > "$scratch/detached-summary.json" || return 1
    else
      printf '{"status":"unavailable"}\n' > "$scratch/detached-summary.json"
    fi
  fi
  jq --slurpfile detached "$scratch/detached-summary.json" '. + {detachedLog: $detached[0]}' \
    "$scratch/startup-summary.json" > "$scratch/startup.next" || return 1
  mv "$scratch/startup.next" "$scratch/startup-summary.json"
}
capture_owned_diagnostics() {
  local resource source command_exit
  diagnostic_owner || return 0
  for resource in security eventstore; do
    for source in describe logs; do
      # Recheck immediately before every diagnostic command, and again before stop.
      diagnostic_owner || return 0
      command_exit=0
      if [[ "$source" == describe ]]; then
        bounded_aspire "$scratch/diagnostic.raw" describe "$resource" "${aspire_args[@]}" --format Json || command_exit=$?
      else
        bounded_aspire "$scratch/diagnostic.raw" logs "$resource" "${aspire_args[@]}" --format Json --tail 40 || command_exit=$?
      fi
      if ! jq -Rrs --arg mode "$source" --arg resource "$resource" --argjson exitCode "$command_exit" \
        -f "$sanitizer" "$scratch/diagnostic.raw" > "$scratch/resource-summary.json" 2> "$scratch/sanitize.err"; then
        jq -n --arg resource "$resource" --arg source "$source" --argjson exitCode "$command_exit" \
          '{resource: $resource, source: $source, exitCode: $exitCode, status: "unsupported-shape"}' > "$scratch/resource-summary.json"
      fi
      cat "$scratch/resource-summary.json" >> "$scratch/resources.jsonl"
    done
  done
}
retain_diagnostics() {
  local verification_exit="$1" final_exit="$2" pending destination
  jq -n --argjson verificationExitCode "$verification_exit" --argjson exitCode "$final_exit" \
    --arg phase "$phase" --argjson startAttempted "$start_attempted" --argjson startExit "$start_exit" \
    --argjson startupConfirmed "$startup_confirmed" --arg ownership "$diagnostics_ownership" \
    --arg cleanup "$cleanup_outcome" --arg lock "$lock_outcome" \
    --slurpfile waits "$scratch/waits.json" --slurpfile startup "$scratch/startup-summary.json" \
    --slurpfile probes "$scratch/probes.jsonl" --slurpfile resources "$scratch/resources.jsonl" \
    '{schemaVersion: 1, verificationExitCode: $verificationExitCode, exitCode: $exitCode, phase: $phase,
      startup: ({attempted: ($startAttempted == 1), exitCode: $startExit, ownershipConfirmed: ($startupConfirmed == 1), waits: $waits[0]} + $startup[0]),
      diagnosticsOwnership: $ownership, resources: $resources, probes: $probes,
      cleanup: {appHost: $cleanup, lock: $lock}}' \
    > "$scratch/sanitized.json" 2> "$scratch/sanitize.err" || return 1
  # A unique file preserves previous invocation evidence, including the earlier
  # Epic 9 proof. This destination contains sanitized JSON only.
  mkdir -p -- "$diagnostics_dir" 2> "$scratch/artifact.err" || return 1
  pending="$(mktemp "$diagnostics_dir/.pending.XXXXXX" 2> "$scratch/artifact.err")" || return 1
  destination="$diagnostics_dir/diagnostics.${pending##*.}.json"
  # Hard-link creation atomically publishes complete bytes and fails if that name
  # already exists. Pending or partial output never enters the upload glob.
  if ! { cat "$scratch/sanitized.json" > "$pending" && jq -e 'type == "object"' "$pending" >/dev/null &&
    ln -- "$pending" "$destination"; } 2> "$scratch/artifact.err"; then
    rm -f -- "$pending"
    return 1
  fi
  rm -f -- "$pending"
}
cleanup() {
  local result=$? verification_exit
  verification_exit=$result
  trap - EXIT INT TERM
  if ! capture_startup_diagnostics; then
    fail 'Startup diagnostic capture was incomplete; raw data was discarded.'
  fi
  if [[ $start_attempted -eq 1 && $ownership_rejected -eq 0 ]]; then
    if ! capture_processes; then
      cleanup_outcome=ownership-unavailable
      fail 'Cleanup cannot establish AppHost ownership; no unverified process was stopped.'
      result=1
    elif [[ "$(exact_count)" -gt 0 ]]; then
      # A failed start may leave a partial process without JSON. The shared lock and
      # absent preflight permit cleanup only when exactly one target appeared.
      if [[ -z "$started_pid" && "$(exact_count)" -eq 1 ]]; then
        started_pid="$(jq -r --arg apphost "$apphost" '.[] | select(.appHostPath == $apphost) | .appHostPid' "$scratch/processes.json")"
      fi
      if [[ "$started_pid" =~ ^[1-9][0-9]*$ && "$(exact_count)" -eq 1 && "$(owned_count)" -eq 1 ]]; then
        if [[ $result -ne 0 ]]; then
          if ! capture_owned_diagnostics; then
            fail 'Owned diagnostic capture was incomplete; raw data was discarded.'
          fi
        fi
        # Diagnostic CLI calls must never make an earlier ownership check stale.
        if [[ $diagnostics_ownership != not-requested ]] && ! diagnostic_owner; then
          cleanup_outcome=ownership-changed
          [[ $diagnostics_ownership != unavailable ]] || cleanup_outcome=ownership-unavailable
          fail 'AppHost ownership changed or became unavailable; refusing an unverified stop.'
          result=1
        elif ! aspire stop "${aspire_args[@]}" > "$scratch/stop.log" 2>&1 ||
          ! capture_processes || [[ "$(exact_count)" -ne 0 ]]; then
          cleanup_outcome=failed
          fail 'Owned AppHost cleanup failed or could not be verified.'
          result=1
        else
          cleanup_outcome=verified-stopped
        fi
      else
        cleanup_outcome=ownership-changed
        fail 'AppHost ownership changed; refusing to stop another process.'
        result=1
      fi
    else
      cleanup_outcome=already-absent
    fi
  elif [[ $ownership_rejected -eq 1 ]]; then
    cleanup_outcome=ownership-rejected
  fi
  if [[ $proof_lock_held -eq 1 ]]; then
    # Detached children may retain the descriptor; release only our acquired flock
    # after the owned AppHost cleanup, rather than relying on process exit.
    if ! flock -u "$proof_lock_fd"; then
      lock_outcome=release-failed
      fail 'Could not release the owned AppHost lifecycle lock.'
      result=1
    else
      lock_outcome=released
    fi
    exec {proof_lock_fd}>&-
    proof_lock_held=0
  fi
  if ! retain_diagnostics "$verification_exit" "$result"; then
    fail 'Could not retain sanitized invalid-bearer diagnostics.'
    [[ $result -ne 0 ]] || result=1
  fi
  rm -rf -- "$scratch"
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
lock_outcome=held
if ! capture_processes; then
  fail 'Aspire process discovery failed before startup.'
  exit 2
fi
if [[ "$(jq '[.[] | select((.appHostPath // "") | endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"))] | length' "$scratch/processes.json")" -ne 0 ]]; then
  fail 'A FrontComposer AppHost is already running; verification requires an owned start.'
  exit 2
fi

start_attempted=1
phase=start
start_exit=0
touch "$scratch/launch-started"
aspire start "${aspire_args[@]}" --isolated --no-build --format Json --log-file "$scratch/launch-cli.log" > "$scratch/start.log" 2>&1 || start_exit=$?
if [[ $start_exit -ne 0 ]]; then
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
phase=ownership
if ! capture_processes; then
  fail 'Aspire process discovery failed after startup.'
  exit 2
fi
if [[ "$(exact_count)" -ne 1 || "$(owned_count)" -ne 1 ]]; then
  ownership_rejected=1
  fail 'Aspire process discovery did not confirm the started AppHost identity.'
  exit 2
fi
startup_confirmed=1
for resource in security eventstore; do
  phase="wait-$resource"
  wait_exit=0
  aspire wait "$resource" --status healthy --timeout 300 "${aspire_args[@]}" > "$scratch/wait.log" 2>&1 || wait_exit=$?
  jq --arg resource "$resource" --argjson exitCode "$wait_exit" \
    '. + [{resource: $resource, exitCode: $exitCode}]' "$scratch/waits.json" > "$scratch/waits.next"
  mv "$scratch/waits.next" "$scratch/waits.json"
  if [[ $wait_exit -ne 0 ]]; then
    fail "Aspire readiness failed for $resource; invalid-bearer verification did not execute."
    exit 2
  fi
done
if [[ "${FrontComposerVerification__Enabled:-}" == true ]]; then
  phase=application-readiness
  if [[ -z "${FrontComposerVerification__Directory:-}" ]] ||
    ! python3 "$repo_root/eng/verify-isolated-readiness.py" check "$FrontComposerVerification__Directory" > "$scratch/application-health.log" 2>&1; then
    fail 'Disposable application health/readiness verification failed.'
    exit 2
  fi
  printf 'Disposable application health/readiness and ordinary gateway workers verified.\n'
fi
phase=discovery
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
probe_index=0
phase=probes
probe() {
  local method="$1" path="$2" base code curl_exit endpoint pass reason scheme port
  probe_index=$((probe_index + 1))
  local request=(--request "$method" --header 'Authorization: Bearer invalid-ci-token')
  if [[ "$method" == POST ]]; then request+=(--header 'Content-Type: application/json' --data '{}'); fi
  # A cold gateway may exhaust both transports before it can respond. Only an
  # entirely transport-only pass reaches the next iteration; any HTTP outcome or
  # unrecognized curl error returns immediately through the existing failure path.
  for pass in 1 2; do
    endpoint=0
    for base in "${bases[@]}"; do
      endpoint=$((endpoint + 1))
      curl_exit=0
      # Disable inherited curl configuration/proxies for Aspire's local endpoints.
      # Do not follow redirects: a redirect is an HTTP failure, never a fallback cue.
      curl --disable --noproxy '*' --silent --show-error --insecure --connect-timeout 5 --max-time 10 \
        --output /dev/null --write-out '%{http_code}\n%{time_namelookup}\t%{time_connect}\t%{time_appconnect}\t%{time_starttransfer}\t%{time_total}\t%{remote_ip}\t%{remote_port}\t%{local_ip}\t%{local_port}\t%{num_connects}\t%{ssl_verify_result}\n' \
        "${request[@]}" "${base%/}$path" > "$scratch/curl.out" 2> "$scratch/curl.err" || curl_exit=$?
      scheme=http
      [[ "$base" != https:* ]] || scheme=https
      port="${base%/}"
      port="${port##*:}"
      jq -Rrs --arg mode probe --argjson route "$probe_index" --argjson pass "$pass" --argjson endpoint "$endpoint" \
        --arg scheme "$scheme" --arg port "$port" --argjson exitCode "$curl_exit" -f "$sanitizer" "$scratch/curl.out" \
        > "$scratch/probe.json" 2> "$scratch/sanitize.err"
      cat "$scratch/probe.json" >> "$scratch/probes.jsonl"
      code="$(jq -r '.httpCode' "$scratch/probe.json")"
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
    if [[ $pass -eq 1 ]]; then
      printf '%s %s: retrying advertised endpoints once after transport-only failures.\n' "$method" "$path" >&2
    fi
  done
  fail "$method $path: all advertised endpoints were unreachable; no authentication result."
  failed=1
}
probe POST /api/v1/commands
probe GET /api/v1/commands/status/01K8AAAAAAAAAAAAAAAAAAAAAA
probe POST /api/v1/queries
probe POST '/hubs/projection-changes/negotiate?negotiateVersion=1'
phase=complete
exit "$failed"

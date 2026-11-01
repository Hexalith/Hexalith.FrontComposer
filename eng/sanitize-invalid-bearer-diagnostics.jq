# All retained strings are fixed vocabulary. Never copy resource names, messages,
# URLs, environment values, headers, or stderr into the public diagnostic record.
def choice($values):
  . as $value | if type == "string" and ($values | index($value)) != null then $value else "unknown" end;
def numeric($maximum):
  if type == "string" and test("^[0-9]{1,8}(\\.[0-9]{1,9})?$") then
    tonumber | if . <= $maximum then . else null end
  else null end;
def integer($maximum):
  numeric($maximum) | if . != null and floor == . then . else null end;
def peer:
  if . == "" then "not-connected"
  elif test("^127\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}$") then "loopback-ipv4"
  elif . == "::1" or . == "0:0:0:0:0:0:0:1" then "loopback-ipv6"
  else "other" end;
def signals:
  {
    connectionRefused: ([scan("connection refused"; "i")] | length),
    addressInUse: ([scan("address already in use"; "i")] | length),
    listening: ([scan("now listening on"; "i")] | length),
    unhandledException: ([scan("unhandled exception"; "i")] | length),
    failedToBind: ([scan("failed to bind"; "i")] | length)
  };

if $mode == "probe" then
  split("\n") as $lines
  | (if ($lines | length) == 3 and $lines[2] == "" and ($lines[1] | split("\t") | length) == 11
      then {code: $lines[0], fields: ($lines[1] | split("\t"))}
      else {code: "invalid", fields: []} end) as $probe
  | {
      route: $ARGS.named.route,
      pass: $ARGS.named.pass,
      endpoint: $ARGS.named.endpoint,
      scheme: ($ARGS.named.scheme | choice(["http", "https"])),
      advertisedPort: ($ARGS.named.port | integer(65535)),
      curlExitCode: $ARGS.named.exitCode,
      httpCode: ($probe.code | if test("^(000|[1-5][0-9][0-9])$") then . else "invalid" end),
      seconds: {
        nameLookup: (($probe.fields[0] // "") | numeric(600)),
        connect: (($probe.fields[1] // "") | numeric(600)),
        tls: (($probe.fields[2] // "") | numeric(600)),
        firstByte: (($probe.fields[3] // "") | numeric(600)),
        total: (($probe.fields[4] // "") | numeric(600))
      },
      remotePeer: (($probe.fields[5] // "") | peer),
      remotePort: (($probe.fields[6] // "") | integer(65535)),
      localPeer: (($probe.fields[7] // "") | peer),
      localPort: (($probe.fields[8] // "") | integer(65535)),
      connections: (($probe.fields[9] // "") | integer(100)),
      tlsVerifyResult: (($probe.fields[10] // "") | integer(100000))
    }
elif $mode == "startup" then
  {consoleSignals: signals}
else
  (try fromjson catch null) as $data
  | {
      resource: ($ARGS.named.resource | choice(["security", "eventstore"])),
      source: ($mode | choice(["describe", "logs"])),
      exitCode: $ARGS.named.exitCode,
      status: (if $ARGS.named.exitCode == 124 or $ARGS.named.exitCode == 137 then "timeout"
        elif $ARGS.named.exitCode == 125 then "output-limit"
        elif $ARGS.named.exitCode != 0 then "command-failed"
        elif $data == null then "invalid-json" else "available" end)
    }
  + (if $ARGS.named.exitCode != 0 or $data == null then {} elif $mode == "describe" then
      ([$data.resources[]? | select(.displayName == $ARGS.named.resource or .name == $ARGS.named.resource
        or ((.name // "") | startswith($ARGS.named.resource + "-")))]) as $resources
      | if ($resources | length) != 1 then {status: "ambiguous-resource"} else
          $resources[0] | {
            state: ((.state // "") | choice(["Running", "Starting", "Stopping", "Stopped", "FailedToStart", "Finished", "Exited", "Waiting", "NotStarted", "Building"])),
            health: ((.healthStatus // "") | choice(["Healthy", "Unhealthy", "Degraded"])),
            resourceExitCode: (.exitCode | if type == "number" and . >= -2147483648 and . <= 4294967295 and floor == . then . else null end),
            endpoints: ([.urls[]? | select(.name != "management") | .url | strings
              | select(test("^https?://(localhost|127\\.0\\.0\\.1|\\[::1\\]):[0-9]+/?$"))
              | capture("^(?<scheme>https?)://(?<host>localhost|127\\.0\\.0\\.1|\\[::1\\]):(?<port>[0-9]+)/?$")
              | {scheme, host: (if .host == "localhost" then "loopback-name" elif .host == "[::1]" then "loopback-ipv6" else "loopback-ipv4" end), port: (.port | integer(65535))}][:16])
          } end
    elif $mode == "logs" then
      (if ($data | type) == "array" then $data elif ($data.logs? | type) == "array" then $data.logs else null end) as $logs
      | if $logs == null then {status: "unsupported-shape"} else
          {records: ($logs[:40] | length), stderrRecords: ([$logs[:40][] | select(.isError? == true)] | length),
            signals: ([$logs[:40][] | (.message? // .content? // .text? // "") | strings] | join("\n") | signals)} end
    else {} end)
end

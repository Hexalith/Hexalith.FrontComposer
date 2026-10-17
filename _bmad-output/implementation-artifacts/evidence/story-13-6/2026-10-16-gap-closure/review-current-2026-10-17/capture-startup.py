"""Capture only allowed runtime identity fields; never retain dashboard or environment secrets."""
from pathlib import Path
import hashlib
import json
import ssl
import subprocess
import urllib.request

packet = Path(__file__).resolve().parent
command = ['aspire', 'describe', '--apphost',
           'src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj',
           '--format', 'Json', '--non-interactive']
result = subprocess.run(command, text=True, capture_output=True, check=True)
resources = json.loads(result.stdout)['resources']
resource = next(item for item in resources if item['name'].startswith('counter-web-'))
pid = str(resource['properties']['executable.pid'])
allowed = {'ASPNETCORE_ENVIRONMENT', 'DOTNET_ENVIRONMENT',
           'Hexalith__Shell__ProjectionReconnectedNoticeDurationMs'}
environment = {}
for item in Path('/proc', pid, 'environ').read_bytes().split(b'\0'):
    key, _, value = item.partition(b'=')
    if key.decode() in allowed:
        environment[key.decode()] = value.decode()
if environment != {'ASPNETCORE_ENVIRONMENT': 'Test', 'DOTNET_ENVIRONMENT': 'Test',
                   'Hexalith__Shell__ProjectionReconnectedNoticeDurationMs': '30000'}:
    raise ValueError('Executed Test host settings differ')

base = 'https://localhost:7201'
context = ssl._create_unverified_context()
def fetch(path):
    return urllib.request.urlopen(base + path, context=context).read()
def sha(data):
    return hashlib.sha256(data).hexdigest()

record = {'resource': {key: resource[key] for key in
                      ['name', 'state', 'healthStatus', 'source', 'startTimestamp']},
          'executablePid': pid, 'actualEnvironment': environment, 'resourceEndpoint': base}
for name in ['fc-focus.js', 'fc-datagrid.js']:
    served = fetch('/_content/Hexalith.FrontComposer.Shell/js/' + name)
    disk = Path('src/Hexalith.FrontComposer.Shell/wwwroot/js', name).read_bytes()
    if served != disk:
        raise ValueError('Served module differs: ' + name)
    record[name] = {'servedSha256': sha(served), 'diskSha256': sha(disk)}
served = fetch('/_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.bundle.scp.css')
disk = Path('src/Hexalith.FrontComposer.Shell/obj/Debug/net10.0/scopedcss/projectbundle/'
            'Hexalith.FrontComposer.Shell.bundle.scp.css').read_bytes()
if served != disk or b'grid-template-columns: auto minmax(0, 1fr) auto' not in served:
    raise ValueError('Served scoped CSS differs or lacks the native narrow grid')
record['scopedCss'] = {'servedSha256': sha(served), 'diskSha256': sha(disk),
                       'nativeGridCorrectionPresent': True}
(packet / 'current-startup.json').write_text(json.dumps(record, indent=2) + '\n')
print(json.dumps(record, indent=2))

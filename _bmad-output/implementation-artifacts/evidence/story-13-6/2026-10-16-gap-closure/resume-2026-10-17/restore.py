"""Restore the byte-exact evidence trees next to a selected destination directory."""
import hashlib,io,json,sys,tarfile
from pathlib import Path
packet=Path(__file__).resolve().parent
manifest=json.loads((packet/'raw-artifacts.json').read_text())
archive=b''
for part in manifest['parts']:
 data=(packet/part['file']).read_bytes()
 assert hashlib.sha256(data).hexdigest()==part['sha256']
 archive+=data
assert hashlib.sha256(archive).hexdigest()==manifest['archiveSha256']
destination=Path(sys.argv[1]).resolve()
with tarfile.open(fileobj=io.BytesIO(archive),mode='r:gz') as tar:
 for entry in manifest['entries']:
  path=destination/entry['tree']/entry['path']
  assert path.resolve().is_relative_to(destination)
  data=tar.extractfile('sha256/'+entry['sha256']).read()
  assert len(data)==entry['bytes'] and hashlib.sha256(data).hexdigest()==entry['sha256']
  path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
print(f"Restored {len(manifest['entries'])} files under {destination}")

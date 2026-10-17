"""Restore byte-exact evidence, enforcing integrity even under python -O."""
import argparse
import hashlib
import io
import json
import tarfile
from pathlib import Path


def contained(root, *members):
    path = root.joinpath(*members).resolve()
    if not path.is_relative_to(root):
        raise ValueError(f'Evidence path escapes destination: {path}')
    return path


def restore(packet, destination):
    packet, destination = Path(packet).resolve(), Path(destination).resolve()
    manifest = json.loads((packet / 'raw-artifacts.json').read_text())
    chunks = []
    for part in manifest['parts']:
        data = contained(packet, part['file']).read_bytes()
        if len(data) != part['bytes'] or hashlib.sha256(data).hexdigest() != part['sha256']:
            raise ValueError(f"Archive part size/hash mismatch: {part['file']}")
        chunks.append(data)
    archive = b''.join(chunks)
    if hashlib.sha256(archive).hexdigest() != manifest['archiveSha256']:
        raise ValueError('Archive hash mismatch')
    paths = [contained(destination, entry['tree'], entry['path']) for entry in manifest['entries']]
    # Read each blob once; duplicate entries need not repeatedly seek through gzip.
    blobs = {}
    with tarfile.open(fileobj=io.BytesIO(archive), mode='r|gz') as tar:
        for member in tar:
            if not member.isfile() or not member.name.startswith('sha256/'):
                raise ValueError(f'Unexpected archive member: {member.name}')
            data = tar.extractfile(member).read()
            digest = member.name.removeprefix('sha256/')
            if hashlib.sha256(data).hexdigest() != digest:
                raise ValueError(f'Archive blob hash mismatch: {member.name}')
            blobs[digest] = data
    for entry in manifest['entries']:
        data = blobs.get(entry['sha256'])
        if data is None or len(data) != entry['bytes'] or hashlib.sha256(data).hexdigest() != entry['sha256']:
            raise ValueError(f"Evidence entry size/hash mismatch: {entry['tree']}/{entry['path']}")
    for entry, path in zip(manifest['entries'], paths):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(blobs[entry['sha256']])
    print(f"Restored {len(manifest['entries'])} files under {destination}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--packet', type=Path, default=Path(__file__).resolve().parent)
    args = parser.parse_args()
    restore(args.packet, args.destination)


if __name__ == '__main__':
    main()

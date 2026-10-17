"""Verify retained archive parts, blobs and every declared path without extraction."""
from pathlib import Path
import hashlib
import json
import re
import tarfile
import tempfile


def verify(manifest_path):
    manifest = json.loads(manifest_path.read_text())
    digest = hashlib.sha256()
    total = 0
    with tempfile.TemporaryFile() as archive:
        for part in manifest['parts']:
            name = Path(part['file'])
            if name.is_absolute() or '..' in name.parts:
                raise ValueError('Unsafe archive part path')
            part_digest = hashlib.sha256()
            size = 0
            with (manifest_path.parent / name).open('rb') as stream:
                while block := stream.read(1024 * 1024):
                    size += len(block)
                    part_digest.update(block)
                    digest.update(block)
                    archive.write(block)
            if size != part['bytes'] or part_digest.hexdigest() != part['sha256']:
                raise ValueError(f'Part integrity failure: {name}')
            total += size
        if digest.hexdigest() != manifest['archiveSha256']:
            raise ValueError('Combined archive hash differs')
        archive.seek(0)
        blobs = {}
        with tarfile.open(fileobj=archive, mode='r|gz') as tar:
            for member in tar:
                if not member.isfile() or not re.fullmatch(r'sha256/[a-f0-9]{64}', member.name):
                    raise ValueError(f'Unexpected archive member: {member.name}')
                member_digest = hashlib.sha256()
                size = 0
                with tar.extractfile(member) as stream:
                    while block := stream.read(1024 * 1024):
                        size += len(block)
                        member_digest.update(block)
                key = member.name.removeprefix('sha256/')
                if key in blobs or member_digest.hexdigest() != key or size != member.size:
                    raise ValueError(f'Blob integrity failure: {member.name}')
                blobs[key] = size
        paths = set()
        for entry in manifest['entries']:
            tree, path = Path(entry['tree']), Path(entry['path'])
            if tree.is_absolute() or path.is_absolute() or '..' in tree.parts or '..' in path.parts:
                raise ValueError('Unsafe evidence path')
            key = (entry['tree'], entry['path'])
            if key in paths or blobs.get(entry['sha256']) != entry['bytes']:
                raise ValueError(f'Evidence entry integrity failure: {key}')
            paths.add(key)
        if len(blobs) != manifest['uniqueBlobCount']:
            raise ValueError('Blob count differs')
    return {'manifest': str(manifest_path), 'archiveSha256': digest.hexdigest(),
            'bytes': total, 'parts': len(manifest['parts']),
            'entries': len(paths), 'uniqueBlobs': len(blobs), 'verified': True}


if __name__ == '__main__':
    packet = Path(__file__).resolve().parent
    results = [verify(path) for path in sorted(packet.rglob('raw-artifacts.json'))]
    (packet / 'archive-integrity.json').write_text(json.dumps({'results': results}, indent=2) + '\n')
    print(json.dumps([{'manifest': Path(row['manifest']).relative_to(packet).as_posix(),
                       'entries': row['entries'], 'parts': row['parts'], 'verified': row['verified']}
                      for row in results], indent=2))

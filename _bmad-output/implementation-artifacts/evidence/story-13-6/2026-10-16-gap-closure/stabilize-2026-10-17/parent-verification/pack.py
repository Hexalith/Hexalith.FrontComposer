"""Publish a complete content-addressed evidence archive after all inputs validate."""
import argparse
import hashlib
import io
import json
import os
import tarfile
import tempfile
from pathlib import Path


def pack(roots, destination):
    inputs = []
    for label, value in roots.items():
        root = Path(value)
        if not root.is_dir():
            raise ValueError(f"Required evidence root is missing: {label}: {root}")
        files = sorted(path for path in root.rglob('*') if path.is_file())
        if not files:
            raise ValueError(f"Required evidence root is empty: {label}: {root}")
        for path in files:
            if not os.access(path, os.R_OK):
                raise ValueError(f"Required evidence input is unreadable: {path}")
        inputs.append((label, root, files))
    if not inputs:
        raise ValueError('No required evidence roots declared')

    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    # Build every byte before publishing. Generation-specific part names leave the
    # previous archive intact until the new manifest is atomically replaced.
    with tempfile.TemporaryDirectory(dir=destination, prefix='.pack-') as staging:
        staging = Path(staging)
        archive = staging / 'archive.tar.gz'
        seen, entries = set(), []
        with tarfile.open(archive, 'w:gz', compresslevel=6) as tar:
            for label, root, files in inputs:
                for path in files:
                    data = path.read_bytes()
                    digest = hashlib.sha256(data).hexdigest()
                    entries.append({'tree': label, 'path': str(path.relative_to(root)), 'sha256': digest, 'bytes': len(data)})
                    if digest in seen:
                        continue
                    seen.add(digest)
                    info = tarfile.TarInfo('sha256/' + digest)
                    info.size, info.mtime = len(data), 0
                    tar.addfile(info, io.BytesIO(data))
        with archive.open('rb') as stream:
            archive_digest = hashlib.file_digest(stream, 'sha256').hexdigest()
        parts = []
        with archive.open('rb') as stream:
            while data := stream.read(32 * 1024 * 1024):
                name = f'raw-artifacts.{archive_digest}.tar.gz.part-{len(parts) + 1:02}'
                (staging / name).write_bytes(data)
                parts.append({'file': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
        manifest = {'format': 'content-addressed-tar-gzip-v1', 'archiveSha256': archive_digest, 'parts': parts,
                    'roots': {label: str(root) for label, root, _ in inputs}, 'entries': entries, 'uniqueBlobCount': len(seen),
                    'note': 'All declared roots validated before publication. Every byte is retained; failed and interrupted runs remain separate trees.'}
        (staging / 'raw-artifacts.json').write_text(json.dumps(manifest, indent=2) + '\n')
        for part in parts:
            os.replace(staging / part['file'], destination / part['file'])
        os.replace(staging / 'raw-artifacts.json', destination / 'raw-artifacts.json')
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--roots-json', type=Path, help='Required label-to-directory mapping; defaults to the captured manifest roots')
    parser.add_argument('--evidence-root', type=Path, help='Restored tree containing each required root label')
    parser.add_argument('--output-dir', type=Path, default=Path(__file__).resolve().parent)
    args = parser.parse_args()
    packet = Path(__file__).resolve().parent
    roots = json.loads(args.roots_json.read_text()) if args.roots_json else json.loads((packet / 'raw-artifacts.json').read_text())['roots']
    if args.evidence_root:
        roots = {label: str(args.evidence_root / label) for label in roots}
    manifest = pack(roots, args.output_dir)
    print(json.dumps({'files': len(manifest['entries']), 'uniqueBlobs': manifest['uniqueBlobCount'], 'parts': len(manifest['parts'])}))


if __name__ == '__main__':
    main()

from pathlib import Path
import tarfile,hashlib,json,io
base=Path('_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/resume-2026-10-17')
roots={
 'visual':'/tmp/story-13-6-resume-20261017',
 'first-green':'/tmp/story-13-6-review-green-20261017',
 'green':'/tmp/story-13-6-review-green2-20261017',
 'home-first':'/tmp/story-13-6-review-home-20261017',
 'home':'/tmp/story-13-6-review-home2-20261017',
 'scroll':'/tmp/story-13-6-review-scroll-20261017',
 'interrupted-first':'/tmp/story-13-6-resume-final-20261017',
 'interrupted-second':'/tmp/story-13-6-resume-final2-20261017',
 'before-slot-browser':'/tmp/story-13-6-resume-final3-20261017',
 'before-slot-report':'/tmp/story-13-6-resume-final3-20261017-report',
 'browser':'/tmp/story-13-6-resume-final4-20261017',
 'slot':'/tmp/story-13-6-review-slot-20261017',
 'slot-others':'/tmp/story-13-6-review-slot-others-20261017',
 'browser-report':'/tmp/story-13-6-resume-final4-20261017-report',
 'settings-probe':'/tmp/story13-settings-debug',
}
archive=Path('/tmp/story-13-6-resume-raw.tar.gz');seen=set();entries=[]
with tarfile.open(archive,'w:gz',compresslevel=6) as tar:
 for label,root in roots.items():
  root=Path(root)
  if not root.exists():continue
  for path in sorted(root.rglob('*')):
   if not path.is_file():continue
   data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
   entries.append({'tree':label,'path':str(path.relative_to(root)),'sha256':digest,'bytes':len(data)})
   if digest in seen:continue
   seen.add(digest);info=tarfile.TarInfo('sha256/'+digest);info.size=len(data);info.mtime=0;tar.addfile(info,io.BytesIO(data))
parts=[]
with archive.open('rb') as stream:
 index=0
 while data:=stream.read(32*1024*1024):
  index+=1;name=f'raw-artifacts.tar.gz.part-{index:02}'
  (base/name).write_bytes(data);parts.append({'file':name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
manifest={'format':'content-addressed-tar-gzip-v1','archiveSha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'parts':parts,'roots':roots,'entries':entries,'uniqueBlobCount':len(seen),'note':'Every byte is retained; identical files are stored once. Failed and interrupted runs remain separate trees. Join parts in order, extract sha256 blobs, then restore each entry at <destination>/<tree>/<path> using its digest.'}
(base/'raw-artifacts.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'files':len(entries),'uniqueBlobs':len(seen),'archiveBytes':archive.stat().st_size,'parts':len(parts)}))

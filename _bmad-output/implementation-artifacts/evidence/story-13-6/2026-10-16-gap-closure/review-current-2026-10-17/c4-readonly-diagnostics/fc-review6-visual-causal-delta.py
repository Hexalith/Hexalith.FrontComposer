from pathlib import Path
from PIL import Image,ImageChops
import json
root=Path('/tmp/story-13-6-review4-final-browser/test-results')
rows=[]
for case in sorted(root.glob('specimen-accessibility-*chromium')):
 actual=list(case.glob('*-actual.png'));expected=list(case.glob('*-expected.png'))
 if not actual or not expected: continue
 a=Image.open(actual[0]).convert('RGB');e=Image.open(expected[0]).convert('RGB');w=min(a.width,e.width);h=min(a.height,e.height)
 count=lambda x,y:sum(1 for p in ImageChops.difference(x,y).getdata() if p!=(0,0,0))
 before=count(a.crop((0,64,w,h)),e.crop((0,64,w,h)))
 shifted=count(a.crop((0,65,w,h+1)),e.crop((0,64,w,h))) if a.height==e.height+1 else None
 rows.append({'case':case.name,'actual':str(actual[0]),'expected':str(expected[0]),'actualSize':a.size,'expectedSize':e.size,'bodyMismatchedPixelsBefore':before,'bodyMismatchedPixelsAfterRemovingActualHeaderExtraRow':shifted})
Path('/tmp/fc-review6-visual-causal-delta.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps(rows,indent=2))

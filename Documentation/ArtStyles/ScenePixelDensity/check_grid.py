"""Read-only screenshot measurement of world-grid pixel cells; no image edits."""
from pathlib import Path
import json
from PIL import Image
import numpy as np

root = Path(__file__).parent
out = {}
for name in ['before.png', 'scene-unified.png', 'crate-scaled-150.png', 'rotated-23.png']:
    p = np.asarray(Image.open(root / name).convert('RGB'))
    h,w,_=p.shape
    blocks=p[:h//3*3,:w//3*3].reshape(h//3,3,w//3,3,3)
    delta = blocks.max(axis=(1,3)).astype(np.int16)-blocks.min(axis=(1,3)).astype(np.int16)
    constant=(delta.max(axis=2)<=2)
    yy,xx=np.mgrid[0:h//3,0:w//3]*3+1.5
    # Main map only. Exclude character footprints and circle/mask silhouette cells.
    distance=(xx-960)**2+(yy-562)**2
    interior=(distance<385**2) & (yy<825) & ~((xx>740)&(xx<880)&(yy<305))
    opaque_ring=(distance>436**2)&(distance<450**2)&(yy<850)&(xx>560)&(xx<1360)
    out[name]={'interior_cells':int(interior.sum()), 'constant_3x3_interior_percent':round(float(constant[interior].mean()*100),2),
       'ring_strip_cells':int(opaque_ring.sum()),'constant_3x3_ring_percent':round(float(constant[opaque_ring].mean()*100),2)}
print(json.dumps(out,indent=2))
(root/'pixel-grid-measurement.json').write_text(json.dumps(out,indent=2),encoding='utf-8')

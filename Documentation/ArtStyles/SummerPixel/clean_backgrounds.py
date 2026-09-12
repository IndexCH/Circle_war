"""Authorized alpha/background cleanup and empty-margin cropping only. Visible RGB is preserved."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

def label_regions(mask):
    labels=np.zeros(mask.shape,dtype=np.int32)
    parents=[0]
    previous=[]
    def find(n):
        while parents[n]!=n:
            parents[n]=parents[parents[n]]
            n=parents[n]
        return n
    for y,row in enumerate(mask):
        edges=np.flatnonzero(np.diff(np.r_[False,row,False]))
        runs=[]
        cursor=0
        for start,end in zip(edges[::2],edges[1::2]):
            k=len(parents);parents.append(k)
            while cursor<len(previous) and previous[cursor][1]<start: cursor+=1
            q=cursor
            while q<len(previous) and previous[q][0]<=end:
                a=find(k);b=find(previous[q][2]);parents[a]=b;q+=1
            labels[y,start:end]=k
            runs.append((start,end,k))
        previous=runs
    roots=np.array([find(i) for i in range(len(parents))],dtype=np.int32)
    return roots[labels],len(parents)-1

def dilate(mask):
    p=np.pad(mask,1)
    result=np.zeros_like(mask)
    for dy in range(3):
        for dx in range(3):result|=p[dy:dy+mask.shape[0],dx:dx+mask.shape[1]]
    return result

root=Path(__file__).parent
out=root/'ready'
out.mkdir(exist_ok=True)
records=json.loads((root/'generated-files.json').read_text(encoding='utf-8'))
report=[]
for record in records:
    key=record['key']
    source=Image.open(record['path']).convert('RGBA')
    pixels=np.array(source)
    if key=='summer-upward-dusk':
        source.save(out/(key+'.png'))
        continue
    rgb=pixels[:,:,:3].astype(np.int16)
    if np.mean(pixels[:,:,3]<9)<0.05:
        # Baked checkerboard is light neutral gray. The outlined subjects are colored/dark.
        neutral=(rgb.max(2)-rgb.min(2)<=14)&(rgb.min(2)>=95)
        labels,count=label_regions(neutral)
        areas=np.bincount(labels.ravel())
        means=rgb.mean(2)
        remove=np.zeros(count+1,dtype=bool)
        border_ids=np.unique(np.r_[labels[0,:],labels[-1,:],labels[:,0],labels[:,-1]])
        remove[border_ids]=True
        for region in np.flatnonzero(areas>=40):
            if region==0: continue
            values=means[labels==region]
            # Also remove enclosed checkered holes; retain flat pale material highlights.
            if areas[region] > source.width*source.height*.01 or np.std(values)>7:
                remove[region]=True
        remove[0]=False
        background=remove[labels]
        fringe=dilate(background)&(rgb.max(2)-rgb.min(2)<=22)&(rgb.min(2)>=95)
        pixels[background|fringe,3]=0
    pixels[pixels[:,:,3]<=8,3]=0
    alpha=pixels[:,:,3]
    visible=np.argwhere(alpha>8)
    if not len(visible): raise RuntimeError('Empty '+key)
    top,left=visible.min(0);bottom,right=visible.max(0)+1
    # Keep a circular ring's full centered canvas so its geometry remains concentric.
    crop=(0,0,source.width,source.height) if key=='summer-moss-ring' else (max(0,left-4),max(0,top-4),min(source.width,right+4),min(source.height,bottom+4))
    cleaned=Image.fromarray(pixels).crop(crop)
    cleaned.save(out/(key+'.png'))
    record.update(size=list(cleaned.size),crop=list(map(int,crop)),transparentFraction=float(np.mean(alpha==0)))
    report.append(record)
    print(key, cleaned.size, 'transparent',round(record['transparentFraction'],3))
(root/'cleanup-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')

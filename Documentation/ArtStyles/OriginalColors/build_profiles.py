"""Measure sprite colors and build numeric Unity Texture3D color tables.

Source and pixel artwork are read only. No PNG is edited or resampled.
"""
from pathlib import Path
import csv
import json
import re
import numpy as np
from PIL import Image

ROOT = Path(r'D:\Unity Projects\Circle_War')
OUT = Path(__file__).parent
SIZE = 32
M = np.array([[.4124564,.3575761,.1804375],[.2126729,.7151522,.072175],[.0193339,.119192,.9503041]])
WHITE = np.array([.95047,1,1.08883])

def linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)

def gamma(rgb):
    rgb = np.maximum(rgb,0)
    return np.where(rgb <= .0031308, 12.92*rgb, 1.055*rgb**(1/2.4)-.055)

def lab(rgb):
    xyz = linear(rgb) @ M.T / WHITE
    f = np.where(xyz > (6/29)**3, np.cbrt(xyz), xyz/(3*(6/29)**2)+4/29)
    return np.stack([116*f[:,1]-16,500*(f[:,0]-f[:,1]),200*(f[:,1]-f[:,2])],axis=1)

def from_lab(values):
    fy=(values[:,0]+16)/116
    f=np.stack([fy+values[:,1]/500,fy,fy-values[:,2]/200],axis=1)
    xyz=np.where(f>6/29,f**3,3*(6/29)**2*(f-4/29))*WHITE
    return np.clip(gamma(xyz @ np.linalg.inv(M).T),0,1)

def groups(rgb):
    r,g,b=rgb.T
    high=rgb.max(1);low=rgb.min(1);delta=np.maximum(high-low,1e-6)
    hue=np.where(high==r,(g-b)/delta,np.where(high==g,2+(b-r)/delta,4+(r-g)/delta))/6 % 1
    saturation=(high-low)/np.maximum(high,1e-6)
    # Keep mineral/wood neutral tones separate from actual colored accents.
    chromatic=(saturation>.35)&(high>.18)
    return np.where(chromatic&(hue>=.18)&(hue<.50),1,
        np.where(chromatic&(hue>=.50)&(hue<.72),2,
        np.where(chromatic&(hue>=.72)&(hue<.94),3,0)))

def read_visible(path, rect):
    image=Image.open(ROOT/path).convert('RGBA')
    x,y,w,h=map(lambda v:int(float(v)),re.findall(r':([0-9.]+)',rect))
    pixels=np.asarray(image.crop((x,image.height-y-h,x+w,image.height-y)))
    rgb=pixels[:,:,:3][pixels[:,:,3]>230].astype(np.float64)/255
    return rgb[::max(1,len(rgb)//60000)]

def make_transfer(source, pixel):
    source_group=groups(source);pixel_group=groups(pixel)
    active=[n for n in range(1,4) if (source_group==n).sum()>max(100,len(source)*.005)
        and (pixel_group==n).sum()>max(100,len(pixel)*.005)]
    source_group=np.where(np.isin(source_group,active),source_group,0)
    pixel_group=np.where(np.isin(pixel_group,active),pixel_group,0)
    source_lab=lab(source);pixel_lab=lab(pixel)
    models={}
    for n in [0]+active:
        a=source_lab[source_group==n];b=pixel_lab[pixel_group==n]
        if not len(a) or not len(b):continue
        q=np.linspace(0,1,33)
        models[n]=(np.quantile(b[:,0],q),np.quantile(a[:,0],q),
            a[:,1:].mean(0),b[:,1:].mean(0),np.clip(a[:,1:].std(0)/np.maximum(b[:,1:].std(0),1),.65,1.35))
    def transfer(rgb):
        ids=groups(rgb);ids=np.where(np.isin(ids,active),ids,0)
        values=lab(rgb)
        for n,(old_l,new_l,new_ab,old_ab,scale) in models.items():
            mask=ids==n
            if not mask.any():continue
            # Preserve true black/white endpoints outside the measured shade range.
            x=np.r_[0,old_l,100];y=np.r_[0,new_l,100]
            mapped=np.interp(values[mask,0],x,y)
            values[mask,0]=mapped
            values[mask,1:]=(values[mask,1:]-old_ab)*scale+new_ab
        return from_lab(values)
    return transfer, active

z,y,x=np.meshgrid(np.arange(SIZE),np.arange(SIZE),np.arange(SIZE),indexing='ij')
grid=np.stack([x,y,z],axis=-1).reshape(-1,3)/(SIZE-1)
manifest=[]
seen=set()
mapping = OUT/'pixel-color-mappings.tsv'
if not mapping.exists():
    mapping = OUT.parent/'pixel-color-mappings.tsv'
with mapping.open(encoding='utf-8-sig') as f:
    for row in csv.DictReader(f,delimiter='\t'):
        name=Path(row['Pixel']).stem
        if name in seen:continue
        seen.add(name)
        source=read_visible(row['Original'],row['OriginalRect'])
        pixel=read_visible(row['Pixel'],row['PixelRect'])
        transfer,active=make_transfer(source,pixel)
        mapped=transfer(grid)
        # Tables contain linear RGB numbers, not an sRGB image texture.
        rgba=np.concatenate([linear(mapped),np.ones((len(mapped),1))],axis=1)
        (OUT/(name+'.rgba16')).write_bytes(rgba.astype('<f2').tobytes())
        before=lab(pixel).mean(0);after=lab(transfer(pixel)).mean(0);reference=lab(source).mean(0)
        item={'name':name,'pixel':row['Pixel'],'original':row['Original'],'size':SIZE,
            'sourceMeanLab':reference.round(3).tolist(),'beforeMeanLab':before.round(3).tolist(),
            'correctedMeanLab':after.round(3).tolist(),'accentGroups':active}
        manifest.append(item)
        print(name, 'mean Lab', np.round(before,1),'->',np.round(after,1),'reference',np.round(reference,1))
(OUT/'profiles.json').write_text(json.dumps({'profiles':manifest},ensure_ascii=False,indent=2),encoding='utf-8')

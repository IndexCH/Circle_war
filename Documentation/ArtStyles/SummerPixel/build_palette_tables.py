"""Read original/generated pixels and generate numeric color tables; never recolor PNGs."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

project=Path(r'D:\Unity Projects\Circle_War')
stage=Path(__file__).parent
namespace={}
existing=(project/'Documentation/ArtStyles/OriginalColors/build_profiles.py').read_text(encoding='utf-8')
namespace['__file__']=str(stage/'build_palette_tables.py')
exec(existing.split('z,y,x=np.meshgrid')[0],namespace)
transfer_function=namespace['make_transfer'];linear=namespace['linear'];lab=namespace['lab']
records=json.loads((stage/'generated-files.json').read_text(encoding='utf-8'))
inventory=[]
for line in (stage/'inventory.txt').read_text(encoding='utf-8-sig').splitlines():
    kind,path,guid,file_id,x,y,w,h,sx,sy,pixel=line.split('|')
    inventory.append(dict(kind=kind,path=path,guid=guid,fileId=int(file_id),rect=list(map(float,[x,y,w,h])),pixel=pixel))

def normalize(path):return str(path).replace('\\','/')
output_by_source={normalize(r['reference']):r['key'] for r in records if r['key'] not in ['summer-upward-dusk','summer-moss-ground-contact']}
aliases={'Assets/Resources/Scence/沼泽地/图层 31.png':'summer-dandelion','Assets/Resources/GameData/RoadSegments/ChatGPT Image 2026年8月28日 15_11_29.png':'summer-water-purifier'}
mappings=[]
for item in inventory:
    if '/NpcRoadSprites/' in item['path']:continue
    if item['kind']=='background': key='summer-upward-dusk'
    elif item['pixel']: key=None
    else:key=output_by_source.get(normalize(project/item['path']),aliases.get(item['path']))
    if not key and not item['pixel']:raise ValueError('Missing '+item['path'])
    item['key']=key
    if key:item['pixel']='Assets/ArtStyles/SummerPixel/'+key+'.png'
    item['contact']=item['pixel'].endswith(('upright-supply-crate.png','wide-supply-chest.png','summer-storage-pod.png'))
    mappings.append(item)
(stage/'mappings.json').write_text(json.dumps(mappings,ensure_ascii=False,indent=2),encoding='utf-8')

size=32
z,y,x=np.meshgrid(np.arange(size),np.arange(size),np.arange(size),indexing='ij')
grid=np.stack([x,y,z],axis=-1).reshape(-1,3)/(size-1)
output=stage/'tables';output.mkdir(exist_ok=True)
profiles=[];seen=set()
for item in mappings:
    key=item['key']
    if not key or key=='summer-upward-dusk' or key in seen:continue
    seen.add(key)
    source_image=Image.open(project/item['path']).convert('RGBA')
    x,y,w,h=map(int,item['rect'])
    source=np.asarray(source_image.crop((x,source_image.height-y-h,x+w,source_image.height-y)))
    image=np.asarray(Image.open(stage/'ready'/(key+'.png')).convert('RGBA'))
    def visible(a):
        values=a[:,:,:3][a[:,:,3]>230].astype(np.float64)/255
        return values[::max(1,len(values)//60000)]
    src=visible(source);pix=visible(image)
    transfer,active=transfer_function(src,pix)
    mapped=transfer(grid)
    rgba=np.concatenate([linear(mapped),np.ones((len(mapped),1))],axis=1)
    (output/(key+'.rgba16')).write_bytes(rgba.astype('<f2').tobytes())
    profile=dict(key=key,sourceMeanLab=lab(src).mean(0).round(2).tolist(),beforeMeanLab=lab(pix).mean(0).round(2).tolist(),correctedMeanLab=lab(transfer(pix)).mean(0).round(2).tolist())
    profiles.append(profile);print(key,profile['beforeMeanLab'],'->',profile['correctedMeanLab'],'source',profile['sourceMeanLab'])
(stage/'profiles.json').write_text(json.dumps(profiles,ensure_ascii=False,indent=2),encoding='utf-8')
print('Mappings',len(mappings),'new color tables',len(profiles))

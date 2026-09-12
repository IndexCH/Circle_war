from pathlib import Path
import json
import numpy as np
from PIL import Image
root=Path(__file__).parent
for r in json.loads((root/'cleanup-report.json').read_text(encoding='utf-8')):
    source=np.array(Image.open(r['path']).convert('RGBA').crop(r['crop']))
    result=np.array(Image.open(root/'ready'/(r['key']+'.png')).convert('RGBA'))
    assert np.array_equal(source[:,:,:3],result[:,:,:3]), 'Unexpected RGB edit'
    assert (result[:,:,3]==0).mean()>.15, 'Missing transparency'
    print(r['key'], 'visible RGB unchanged; alpha transparent fraction',round(float((result[:,:,3]==0).mean()),3))

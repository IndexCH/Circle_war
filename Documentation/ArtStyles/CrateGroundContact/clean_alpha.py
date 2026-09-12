from pathlib import Path
from PIL import Image
import numpy as np

source = Path(r'C:\Users\Administrator\.codex\generated_images\01a09080-1e30-7e81-a8cf-b6224a5c1bff\exec-9746b279-eb8b-4e43-a1c6-83702877ae71.png')
image = Image.open(source).convert('RGBA')
pixels = np.array(image)
before_rgb = pixels[:, :, :3].copy()
# Remove only near-transparent extraction noise, then trim empty margins.
# Background cleanup and empty-edge cropping were explicitly authorized by the user.
pixels[pixels[:, :, 3] <= 8, 3] = 0
assert np.array_equal(before_rgb, pixels[:, :, :3])
clean = Image.fromarray(pixels)
box = clean.getchannel('A').getbbox()
assert box is not None
box = (max(0, box[0]-4), max(0, box[1]-4), min(clean.width, box[2]+4), min(clean.height, box[3]+4))
clean.crop(box).save(Path(__file__).parent / 'crate-salt-ground-contact-v1.png')
print({'source_size': image.size, 'crop': box, 'output_size': (box[2]-box[0], box[3]-box[1]), 'rgb_modified': False})

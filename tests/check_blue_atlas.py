"""Regenerate original world maps and check channel/import contracts without Unity."""
from pathlib import Path
import sys
import struct
import zlib
import numpy as np
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root/'tools'))
import generate_blue_world as blue

def decode(path):
    blob=path.read_bytes(); chunks=[]; pos=8
    while pos < len(blob):
        size=struct.unpack('>I',blob[pos:pos+4])[0]
        if blob[pos+4:pos+8] == b'IDAT': chunks.append(blob[pos+8:pos+8+size])
        pos+=size+12
    raw=np.frombuffer(zlib.decompress(b''.join(chunks)),dtype=np.uint8).reshape(blue.HEIGHT,blue.WIDTH*4+1)
    assert np.all(raw[:,0] == 0)
    return raw[:,1:].reshape(blue.HEIGHT,blue.WIDTH,4)

surface, clouds = blue.generate()
for name, generated, srgb in [('QW_BlueSurface.png',surface,1),('QW_BlueClouds.png',clouds,0)]:
    path=root/'Assets/Art/QuietWatch/Textures'/name
    saved=decode(path)
    assert np.max(np.abs(saved.astype(int)-generated.astype(int))) <= 1, name+' differs from generator'
    assert f'sRGBTexture: {srgb}' in Path(str(path)+'.meta').read_text()
    # Longitude wrap must be no worse than neighboring texels, not a hard seam.
    seam=np.abs(generated[:,0].astype(float)-generated[:,-1]).mean()
    adjacent=np.abs(np.diff(generated.astype(float),axis=1)).mean()
    assert seam < adjacent*3+1, (name,seam,adjacent)
assert .15 < np.mean(surface[...,3]>128) < .85, 'Missing land/ocean contrast'
assert np.ptp(clouds[...,0]) > 150 and np.count_nonzero(clouds[...,2]>40) > 1000
assert clouds[...,2][surface[...,3]==0].mean() < 3, 'Settlements leak into open ocean'
repeat=blue.generate_rows(256,320)
assert np.array_equal(surface[256:320],repeat[0]) and np.array_equal(clouds[256:320],repeat[1])
print('PASS: terrain/cloud/city channels, wrap continuity, import color space and deterministic checked-in maps')

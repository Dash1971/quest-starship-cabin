"""Check original relief asset, channel range, seam and deterministic derivation."""
import sys,struct,zlib
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'tools'))
import generate_weather_relief as relief
image=relief.generate_relief()
path=root/'Assets/Art/QuietWatch/Textures/QW_WeatherRelief.png'
blob=path.read_bytes();pos=8;chunks=[]
while pos<len(blob):
    size=struct.unpack('>I',blob[pos:pos+4])[0]
    if blob[pos+4:pos+8]==b'IDAT':chunks.append(blob[pos+8:pos+8+size])
    pos+=size+12
raw=np.frombuffer(zlib.decompress(b''.join(chunks)),dtype=np.uint8).reshape(relief.HEIGHT,relief.WIDTH*4+1)
assert np.all(raw[:,0]==0)
saved=raw[:,1:].reshape(image.shape)
assert np.max(np.abs(saved.astype(int)-image.astype(int)))<=1
assert image.shape==(1024,2048,4)
assert np.ptp(image[...,0])>20 and np.ptp(image[...,1])>20, 'No cloud slopes'
assert image[...,2].max()>180 and np.ptp(image[...,3])>150
seam=np.abs(image[:,0].astype(float)-image[:,-1]).mean()
adjacent=np.abs(np.diff(image.astype(float),axis=1)).mean()
assert seam<adjacent*3+1
meta=Path(str(path)+'.meta').read_text()
assert 'sRGBTexture: 0' in meta and 'maxTextureSize: 2048' in meta
assert 'format = TextureImporterFormat.ASTC_6x6' in (root/'Assets/Editor/QuietWatchExteriorBuilder.cs').read_text()
print('PASS: 2K relief matches deterministic source; valid slopes, cloud coverage/height, longitude wrap and linear import')

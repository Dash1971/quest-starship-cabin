"""Regenerate the actual atlas twice; allow platform math rounding of one byte."""
import importlib.util
from pathlib import Path
import struct
import zlib
import numpy as np

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('weather', root/'tools/generate_great_weather.py')
weather = importlib.util.module_from_spec(spec)
spec.loader.exec_module(weather)
a, b = weather.generate(), weather.generate()
assert np.array_equal(a, b), 'Atlas generation is not deterministic'
# This generator writes PNG scanlines with filter 0. Decode without Pillow.
blob = (root/'Assets/Art/QuietWatch/Textures/QW_GreatWeather.png').read_bytes()
assert blob[:8] == b'\x89PNG\r\n\x1a\n'
chunks, pos = [], 8
while pos < len(blob):
    size = struct.unpack('>I', blob[pos:pos+4])[0]
    kind = blob[pos+4:pos+8]
    if kind == b'IDAT': chunks.append(blob[pos+8:pos+8+size])
    pos += size + 12
raw = np.frombuffer(zlib.decompress(b''.join(chunks)), dtype=np.uint8).reshape(weather.HEIGHT, weather.WIDTH*4+1)
assert np.all(raw[:,0] == 0)
saved = raw[:,1:].reshape(a.shape)
assert np.max(np.abs(saved.astype(int)-a.astype(int))) <= 1, 'Checked-in atlas differs from generator'
assert a.shape == (1024,2048,4)
assert np.ptp(a[...,3]) > 200, 'Missing storm mask'
print('PASS: original RGBA atlas repeats deterministically and matches checked-in asset within one-byte rounding')

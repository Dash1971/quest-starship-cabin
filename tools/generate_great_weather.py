"""Original, deterministic cloud atlas. Requires NumPy; no external imagery.

Run from any directory: python3 tools/generate_great_weather.py
RGBA: display-referred cloud colour plus storm mask. Longitude is periodic.
The checked-in PNG lets Unity build without Python or Blender installed.
"""
from pathlib import Path
import struct
import zlib
import numpy as np

WIDTH, HEIGHT = 2048, 1024


def turbulence(x, y):
    value = np.zeros_like(x)
    # Periodic longitudinal harmonics: seamless at u=0/1, including derivatives.
    for octave, amplitude in enumerate((0.50, 0.25, 0.125, 0.0625, 0.03125)):
        frequency = 2 ** octave
        value += amplitude * np.sin(x * frequency + 1.7 * np.sin(y * frequency * .73 + octave))
        value += amplitude * .35 * np.cos(x * (frequency + 3) - y * frequency * 1.13)
    return value


def generate():
    u, v = np.meshgrid(np.arange(WIDTH) / WIDTH, np.arange(HEIGHT) / (HEIGHT - 1))
    x, y = u * 2 * np.pi, v * 2 * np.pi
    storm_mask = np.zeros_like(u)
    # Flow displacement embeds each storm in the surrounding belts.
    for cx, cy, rx, ry, turns in ((.08, .43, .085, .045, 3.8),
                                   (.22, .59, .037, .022, -2.5),
                                   (.86, .65, .048, .026, 2.1),
                                   (.40, .28, .030, .018, -2.7)):
        dx = ((u - cx + .5) % 1 - .5) / rx
        dy = (v - cy) / ry
        radius = np.sqrt(dx*dx + dy*dy)
        falloff = np.exp(-radius*radius*1.4)
        angle = turns * falloff
        displaced_y = (dx * np.sin(angle) + dy * np.cos(angle) - dy) * ry * 2*np.pi
        y += displaced_y
        storm_mask = np.maximum(storm_mask, falloff)
    broad = turbulence(x*3, y*4.0)
    detail = turbulence(x*13, y*17.0)
    shear = y + .055*broad + .014*detail
    belt = .5 + .5*np.sin(shear*13 + .5*turbulence(x*2, y*6))
    filaments = turbulence(x*31, shear*71)
    tone = np.clip(.18 + .63*belt + .10*detail + .065*filaments, 0, 1)
    dark = np.array([.24, .15, .105])
    pale = np.array([.86, .77, .59])
    rgb = dark + (pale-dark)*tone[..., None]
    warm = np.array([.62, .31, .15])
    rgb = rgb*(1-storm_mask[..., None]*.55)+warm*storm_mask[..., None]*.55
    rgb *= (1+.065*filaments[..., None])
    pole = np.clip((np.abs(v-.5)-.34)/.16, 0, 1)
    rgb = rgb*(1-pole[..., None]*.6)+np.array([.34,.37,.38])*pole[..., None]*.6
    rgba = np.dstack((np.clip(rgb,0,1), np.clip(storm_mask,0,1)))
    return (rgba*255+.5).astype(np.uint8)


def save_png(path, pixels):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind+data))
    raw = b''.join(b'\x00'+row.tobytes() for row in pixels)
    data = b'\x89PNG\r\n\x1a\n'
    data += chunk(b'IHDR', struct.pack('>IIBBBBB', WIDTH, HEIGHT, 8, 6, 0, 0, 0))
    data += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


if __name__ == '__main__':
    destination = Path(__file__).resolve().parents[1] / 'Assets/Art/QuietWatch/Textures/QW_GreatWeather.png'
    save_png(destination, generate())
    print(destination)

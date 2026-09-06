"""Independent geometry reference checks; does not compile or run the GPU shader."""
import math
import re
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parents[1]
def rotation(axis, degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return np.array(([[1,0,0],[0,c,-s],[0,s,c]], [[c,0,s],[0,1,0],[-s,0,c]], [[c,-s,0],[s,c,0],[0,0,1]])[axis])
def unit(v): return v / np.linalg.norm(v)
source = (root/'Assets/Editor/QuietWatchExteriorBuilder.cs').read_text()
def vector(name):
    return np.array([float(x) for x in re.search(name+r' = new Vector3\(([^)]+)\)',source)[1].replace('f','').split(',')])
def number(name): return float(re.search(r'\b'+name+r' = ([\d.]+)f',source)[1])
center = vector('WeatherCenter')
radius, inner, outer = (number(n) for n in ('WeatherRadius','RingInner','RingOuter'))
start_radius, travel, moon_radius = number('MoonShadowRadius'), number('MoonTravel'), number('MoonDiameter')/2
origin = np.array([-1.6,1.1,-1.42])
angles = vector('WeatherRingAngles')
turn = rotation(1,angles[1]) @ rotation(0,angles[0]) @ rotation(2,angles[2]) # Unity Z, X, Y order
normal = turn @ [0,0,1]
radial = turn @ [-1,0,0]
sun = unit(vector('WeatherSun'))
start = center + radial*start_radius - sun*8
finish = start + radial*travel
for point, expected in ((start,start_radius),(finish,start_radius+travel)):
    distance = np.dot(center-point,normal)/np.dot(sun,normal)
    assert distance > 0
    assert abs(np.linalg.norm(point+sun*distance-center)-expected) < 1e-10
    # Observer's segment to the moon must miss the opaque giant, including moon radius.
    direction = unit(point-origin)
    nearest = origin + direction*np.clip(np.dot(center-origin,direction),0,np.linalg.norm(point-origin))
    assert np.linalg.norm(nearest-center) > radius+moon_radius
print('PASS: moon starts behind ring and clears its outer shadow, visible outside giant silhouette')
# Compare the shader projection reference with direct, astronomical world-space rays.
scale = 1_000_000
for point in (center,start,finish,center+[radius,0,0]):
    for eye in (origin+[-.032,0,0], origin+[.032,0,0], origin+[.15,0,0], np.array([2.05,.95,-1.])):
        physical = origin + (point-origin)*scale
        proxy_displaced = point + (eye-origin)*(1-1/scale)
        assert np.linalg.norm(unit(physical-eye)-unit(proxy_displaced-eye)) < 1e-12
print('PASS: each-eye, 15 cm sway and reclining-seat projection matches physical-space rays')
# Scale and composition must be visibly different from the earlier 42 degree globe.
assert scale*2*radius/1000 == 140000
angle = math.degrees(2*math.asin(radius/np.linalg.norm(center-origin)))
assert 70 < angle < 90
assert start_radius+travel > outer+moon_radius
# The initial shadow ray must hit a dense band, not the division at t=.64.
t = (start_radius-inner)/(outer-inner)
density = (.68+.18*math.sin(start_radius*2.9)+.10*math.sin(start_radius*9.7))*(1-.92*math.exp(-((t-.64)/.027)**2))
assert 1-density*.92 < .65
print(f'PASS: 140,000 km giant spans {angle:.1f} degrees; moon starts in a dense band and clears it fully')

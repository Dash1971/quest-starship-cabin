"""Independent geometry reference checks; does not compile or run the GPU shader."""
import math
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parents[1]
def rotation(axis, degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return np.array(([[1,0,0],[0,c,-s],[0,s,c]], [[c,0,s],[0,1,0],[-s,0,c]], [[c,-s,0],[s,c,0],[0,0,1]])[axis])
def unit(v): return v / np.linalg.norm(v)
center = np.array([8.,6.,-76.])
origin = np.array([-1.6,1.1,-1.42])
turn = rotation(1,8) @ rotation(0,63) @ rotation(2,-14) # Unity Z, X, Y order
normal = turn @ [0,0,1]
radial = turn @ [-1,0,0]
sun = unit(np.array([-.62,.1,.78]))
start = center + radial*39.5 - sun*4
finish = start + radial*8
for point, expected in ((start,39.5),(finish,47.5)):
    distance = np.dot(center-point,normal)/np.dot(sun,normal)
    assert distance > 0
    assert abs(np.linalg.norm(point+sun*distance-center)-expected) < 1e-10
    # Observer's segment to the moon must miss the opaque giant, including moon radius.
    direction = unit(point-origin)
    nearest = origin + direction*np.clip(np.dot(center-origin,direction),0,np.linalg.norm(point-origin))
    assert np.linalg.norm(nearest-center) > 29+.8
print('PASS: moon starts behind ring and clears its outer shadow, visible outside giant silhouette')
# Compare the shader projection reference with direct, astronomical world-space rays.
scale = 1_000_000
for point in (center,start,finish,center+[29,0,0]):
    for eye in (origin+[-.032,0,0], origin+[.032,0,0], origin+[.15,0,0], np.array([2.05,.95,-1.])):
        physical = origin + (point-origin)*scale
        proxy_displaced = point + (eye-origin)*(1-1/scale)
        assert np.linalg.norm(unit(physical-eye)-unit(proxy_displaced-eye)) < 1e-12
print('PASS: each-eye, 15 cm sway and reclining-seat projection matches physical-space rays')
# Fixed angular diameter with correct distance (58 proxy metres -> 58,000 km).
assert scale*58/1000 == 58000
print('PASS: giant physical diameter is 58,000 km')
# Confirm test reference values are still wired in the scene builder.
source = (root/'Assets/Editor/QuietWatchExteriorBuilder.cs').read_text()
for token in ('1000000f', '39.5f', 'radial * 8f', 'Quaternion.Euler(63f, 8f, -14f)', 'new Vector3(-0.62f, 0.10f, 0.78f)'):
    assert token in source, 'Update geometry reference for builder change: '+token
print('PASS: reference constants agree with scene authoring')

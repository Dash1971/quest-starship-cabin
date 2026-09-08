"""Check the actual C# stellar catalogue through all cabin seats. Not a GPU/headset approval."""
import json,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from preview_first_question import positions, project, unit, SEATS, glazing, views
model=json.loads(Path(sys.argv[1]).read_text())
# Check this projection tool against values emitted by the ACTUAL runtime model,
# including both cell periods. It is not an independent stand-in catalogue.
for checkpoint in model['checkpoints']:
 p,fade=positions(model,checkpoint['seconds'])
 actual=np.array([[v[k] for k in ('X','Y','Z')] for v in checkpoint['positions']])
 actual_fade=np.array([v['Visibility'] for v in checkpoint['positions']])
 assert np.allclose(p,actual,rtol=0,atol=.012),'Python projection disagrees with C# runtime positions'
 assert np.allclose(fade,actual_fade,rtol=0,atol=5e-7),'Recycling fade differs from runtime'
print('PASS: projection agrees with exported runtime coordinates across near/mid/far cell rollovers')

# Sample the WHOLE sphere, including zenith/nadir and both ends of the flight axis.
# The M12 +/-36-degree slab fails this immediately, even if its seat centre looks fine.
for seconds in (0,600,3600,7200):
 p,fade=positions(model,seconds);directions=unit(p);minimum=100000
 for elevation in range(-80,81,20):
  for azimuth in range(0,360,30):
   el,az=np.radians([elevation,azimuth])
   ray=np.array([np.cos(el)*np.sin(az),np.sin(el),-np.cos(el)*np.cos(az)])
   count=np.sum((directions@ray>np.cos(np.radians(15)))&(fade>.1))
   minimum=min(minimum,count)
   assert count>=40,f'Empty sky cone: {seconds}s, az={azimuth}, el={elevation}: {count}'
 print(f'PASS: full sphere at {seconds}s: every sampled 15-degree cone has at least {minimum} stars')

# Intersect rays with the actual sloped glazing, independently of head pitch/FOV.
# Inspect sky neighbourhoods behind the bottom/middle/top of every pane, from seats and standing.
def pane_coordinates(p,eye):
 base=np.array([0,.75,-2.6]);up=unit(np.array([0,1.75,1.2]));normal=np.cross([1,0,0],up)
 delta=p-eye;fraction=((base-eye)@normal)/(delta@normal)
 hit=eye+delta*fraction[:,None];v=(hit-base)@up
 return hit[:,0],v,(fraction>0)&(fraction<1)
extra_eyes=[('standing', [0,1.7,-.8]),('left-window',[-1.25,1.65,-1.1]),('right-window',[1.4,1.65,-.8])]
for name,base in [(s[0],s[1]) for s in SEATS]+extra_eyes:
 cone_minimum=100000
 for seconds in (0,600,3600,7200):
  p,fade=positions(model,seconds);directions=unit(p)
  for offset in ([0,0,0],[-.12,.12,.10],[.12,-.12,-.1]):
   eye=np.array(base)+offset;u,v,in_front=pane_coordinates(p,eye)
   visible=in_front&(fade>.1)
   for lo,hi in [(-2.88,-1.92),(-1.56,-.60),(-.24,.72),(1.42,2.78)]:
    for bottom,top in [(.36,.86),(.86,1.36),(1.36,1.74)]:
     # Grazing panes can subtend less than one expected star per tile.
     # Test a fixed angular neighbourhood as well as the complete pane.
     centre=np.array([(lo+hi)/2,.75,-2.6])+unit(np.array([0,1.75,1.2]))*((bottom+top)/2)
     cone=np.sum((directions@unit(centre-eye)>np.cos(np.radians(6)))&(fade>.1))
     cone_minimum=min(cone_minimum,cone)
     assert cone>=3,f'Empty sky behind pane: {name}/{seconds}s pane {lo}, height {bottom}'
    assert np.sum(visible&(u>lo+.04)&(u<hi-.04)&(v>.36)&(v<1.74))>=4,f'Unpopulated pane: {name}/{lo}'
 print(f'PASS: {name}, including head movement: every pane populated; bottom/middle/top sky cones contain >= {cone_minimum} stars')
for name,eye,target in SEATS:
 eye=np.array(eye,float)
 for seconds in (0,600,3600,7200):
  p,fade=positions(model,seconds);q,later=positions(model,seconds+12)
  xy=project(p,eye,target,1536,1024);after=project(q,eye,target,1536,1024)
  visible=(fade>.99)&(later>.99)&(xy[:,0]>30)&(xy[:,0]<1506)&(xy[:,1]>30)&(xy[:,1]<994)
  visible&=np.array([glazing(point,eye) for point in p])
  assert visible.sum()>150, f'Sparse/uncovered starfield: {name}/{seconds}'
  assert np.all(after[visible,0]<xy[visible,0]-.5), f'Stationary or right-moving stars: {name}/{seconds}'
  assert np.allclose(p[visible,1:],q[visible,1:]), 'Stars approach the ship or bob vertically'
  scales=np.array([s['Scale'] for s in model['stars']]);shift=xy[:,0]-after[:,0]
  foreground=visible&(scales==.25);background=visible&(scales==1)
  assert foreground.sum()>=20,f'No foreground travel references at {name}/{seconds}'
  assert np.median(shift[foreground])>2*np.median(shift[background]),f'Flat motion at {name}/{seconds}'
 print(f'PASS: {name}: >150 points move LEFT; >=20 near references with >2x background travel at 0/10/60/120 minutes')
# The real GPU audit must be able to observe every depth tier on both sides of
# oblique/upward cameras. This caught premature near-star fade in diagonal views.
smallest=100000;samples=0
for seconds in (0,float(np.floor(model['period']/model['speed']))):
 p,fade=positions(model,seconds);q,later=positions(model,seconds+20)
 for name,eye,target in views():
  a=project(p,np.array(eye),target,512,512)/512;b=project(q,np.array(eye),target,512,512)/512
  delta=(b[:,0]-a[:,0])*512
  for scale in (.25,.5,1):
   for left in (True,False):
    ok=(scales==scale)&(fade>.99)&(later>.99)&((a[:,0]<.5)==left)&(a[:,0]>.2)&(a[:,0]<.8)&(a[:,1]>.2)&(a[:,1]<.8)
    ok&=(b[:,0]>.15)&(b[:,0]<.85)&(b[:,1]>.15)&(b[:,1]<.85)&(np.abs(delta)>.5)
    if 'upward' not in name and 'standing' not in name:ok&=delta<0
    assert ok.any(),f'Missing persistent travel reference: {seconds}/{name}/{scale}/{left}'
    smallest=min(smallest,int(ok.sum()));samples+=1
print(f'PASS: {samples} GPU-audit selections have persistent references; smallest candidate pool = {smallest}')
# Full six-degree comet tail, including width, stays in the reference panes at hold-B's readable phase.
p=np.array([model['comet'][k] for k in ('X','Y','Z')]);anti=unit(np.array([-1,.24,-.08]))
for name,base_eye,target in SEATS:
 for head in (np.zeros(3),np.array([-.08,0,0]),np.array([.08,0,0])):
  eye=np.array(base_eye)+head;ray=unit(p-eye);along=unit(anti-ray*np.dot(anti,ray));across=unit(np.cross(ray,along))
  for u in (-.1,.2,.5,.8,1):
   for v in (-1,1):
    direction=unit(ray+along*u*np.radians(model['cometTailDegrees'])+across*v*np.radians(model['cometHalfWidthDegrees']))
    assert glazing(eye+direction*36000,eye),f'Distant comet hidden behind a frame: {name}'
print('PASS: readable comet tail clears glazing from all seats, including +/-8 cm head offsets')
shared=(ROOT/'Assets/Shaders/QuietWatchStarWindow.shader').read_text()
field=(ROOT/'Assets/Shaders/QuietWatchCruiseStars.shader').read_text()
assert shared.index('if (_FirstQuestionField > .5) return')<shared.index('float band = galacticMask(sky)')
assert 'firstQuestionComet(' not in shared, 'Oversized UV-space meteor returned'
assert '_Time' not in field and '_ObservationTime' not in field, 'Independent stellar animation returned'
assert 'stellarPosition.x=a.p.x+_Travel-localCycle*period' in field, 'Shader translation differs from the audited model'
print('PASS: unified field bypasses stationary sky layers, dust and the old UV-space meteor')

assert 'float3 data:TEXCOORD1' in field and '_Sector/cellScale+localCycle' in field,'Per-depth shader recycling metadata missing'

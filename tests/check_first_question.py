"""Check the actual C# stellar catalogue through all cabin seats. Not a GPU/headset approval."""
import json,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from preview_first_question import positions, project, unit, SEATS, glazing
model=json.loads(Path(sys.argv[1]).read_text())
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
 print(f'PASS: {name}: every visible source projects LEFT at 0/10/60/120 minutes; >150 sources inside glazing')
# Full comet tail, including width, stays in the reference panes at hold-B's readable phase.
p=np.array([model['comet'][k] for k in ('X','Y','Z')]);anti=unit(np.array([-1,.24,-.08]))
for name,base_eye,target in SEATS:
 for head in (np.zeros(3),np.array([-.08,0,0]),np.array([.08,0,0])):
  eye=np.array(base_eye)+head;ray=unit(p-eye);along=unit(anti-ray*np.dot(anti,ray));across=unit(np.cross(ray,along))
  for u in (0,1):
   for v in (-1,1):
    direction=unit(ray+along*u*np.radians(model['cometTailDegrees'])+across*v*np.radians(model['cometHalfWidthDegrees']))
    assert glazing(eye+direction*36000,eye),f'Distant comet hidden behind a frame: {name}'
print('PASS: small comet tail clears glazing from all seats, including +/-8 cm head offsets')
shared=(ROOT/'Assets/Shaders/QuietWatchStarWindow.shader').read_text()
field=(ROOT/'Assets/Shaders/QuietWatchCruiseStars.shader').read_text()
assert shared.index('if (_FirstQuestionField > .5) return')<shared.index('float band = galacticMask(sky)')
assert 'firstQuestionComet(' not in shared, 'Oversized UV-space meteor returned'
assert '_Time' not in field and '_ObservationTime' not in field, 'Independent stellar animation returned'
assert 'stellarPosition.x=a.p.x+_Travel-localCycle*_WrapWidth' in field, 'Shader translation differs from the audited model'
print('PASS: unified field bypasses stationary sky layers, dust and the old UV-space meteor')

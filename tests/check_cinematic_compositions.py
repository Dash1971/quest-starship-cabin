"""Source-space framing/depth checks; not Unity renders or headset approval."""
import sys,re,struct,zlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from check_harbour_layout import vector,scalar,rotation,glazing
import generate_galactic_sky as galaxy

def unit(v):return v/np.linalg.norm(v,axis=-1,keepdims=True)
origin=np.array([-1.6,1.1,-1.42])
seats=[(origin,[0,1.45,-7]),(np.array([1.42,1.22,-.1]),[.4,1.55,-7]),
       (np.array([2.05,.95,-1]),[.6,1.55,-7]),(np.array([-2.2,1.18,2]),[-.5,1.4,-7])]
center=vector('BlueCenter');radius=scalar('BlueRadius');sun=unit(vector('BlueSun'));a=vector('BlueAngles')
turn=rotation(1,a[1])@rotation(0,a[0])@rotation(2,a[2])
assert np.linalg.norm(center-origin)>radius*1.04,'Eye enters an orbital atmosphere'
source=(ROOT/'Assets/Shaders/QuietWatchCloudDeck.shader').read_text()
assert '(.94+wave)' in source # Keep the geometry model tied to the material's authored oval.
for eye,target in seats:
    f=unit(np.array(target)-eye);right=unit(np.cross(f,[0,1,0]));up=unit(np.cross(right,f))
    xx,yy=np.meshgrid(np.linspace(-1,1,120),np.linspace(-1,1,80));ray=unit(f+xx[...,None]*.866*right+yy[...,None]*.577*up)
    rel=origin-center;b=ray@rel;disc=b*b-np.dot(rel,rel)+(radius*1.024)**2
    t=-b-np.sqrt(np.maximum(0,disc));point=origin+ray*t[...,None];n=unit(point-center);globe=n@turn
    latitude=np.abs(globe[...,1]);night=1-np.clip(((n@sun)+.15)/.33,0,1)
    band=(np.abs(latitude-.94)<.007)&(night>.2)&(disc>0)&(t>0)
    pane=np.array([glazing(eye+d*100,eye) for d in ray.reshape(-1,3)]).reshape(xx.shape)
    assert np.count_nonzero(band&pane)>100,'Aurora misses the visible night-side glazing'
print('PASS: Blue Morning auroral band crosses visible night-side glazing from all four seats')
for name in ('Harbour','Formation'):
    c=vector(name+'WorldCenter');r=scalar(name+'WorldRadius')
    assert np.linalg.norm(c-origin)-r>1800,'Planet proxy can occlude the foreground fleet'
    assert np.linalg.norm(c-origin)+r*1.04<20000,'Camera clips distant atmosphere geometry'
print('PASS: enlarged fleet worlds retain foreground depth ordering and fit the camera clip range')
moon=vector('WeatherCompanion');mr=scalar('WeatherCompanionDiameter')/2
assert np.linalg.norm(moon-vector('WeatherCenter'))>scalar('WeatherRadius')+mr
assert np.linalg.norm(moon-origin)>mr
# The whole companion disc must be clear of the opaque planet along its sightline.
direction=unit(moon-origin);toPlanet=vector('WeatherCenter')-origin
nearest=origin+direction*np.clip(np.dot(toPlanet,direction),0,np.linalg.norm(moon-origin))
assert np.linalg.norm(nearest-vector('WeatherCenter'))>scalar('WeatherRadius')+mr
ringAngles=vector('WeatherRingAngles')
ringNormal=rotation(1,ringAngles[1])@rotation(0,ringAngles[0])@rotation(2,ringAngles[2])@np.array([0,0,1])
assert np.abs(np.dot(moon-vector('WeatherCenter'),unit(ringNormal)))>mr

assert np.linalg.norm(vector('FlagshipPosition'))>scalar('FlagshipScale')*14.5+3
print('PASS: companion moon clears the giant and close flagship clears the observer')
# Galaxy PNG is original deterministic RGBA, with poles and longitude continuity.
expected=galaxy.generate();blob=(ROOT/'Assets/Art/QuietWatch/Textures/QW_GalacticSky.png').read_bytes();parts=[];i=8
while i<len(blob):
    length=struct.unpack('>I',blob[i:i+4])[0]
    if blob[i+4:i+8]==b'IDAT':parts.append(blob[i+8:i+8+length])
    i+=length+12
raw=np.frombuffer(zlib.decompress(b''.join(parts)),np.uint8).reshape(galaxy.HEIGHT,galaxy.WIDTH*4+1)
assert np.all(raw[:,0]==0);saved=raw[:,1:].reshape(expected.shape)
assert np.max(np.abs(saved.astype(int)-expected.astype(int)))<=1
assert not np.any(expected[[0,-1]])
assert np.abs(expected[:,0].astype(float)-expected[:,-1]).mean()<2
assert np.percentile(expected[...,:3],95)>20 and np.mean(expected[...,:3]<3)>.5
print('PASS: deterministic galactic panorama, seamless longitude, convergent poles and preserved black space')

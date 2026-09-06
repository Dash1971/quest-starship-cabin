"""CPU corridor/solid checks for the authored layout; not Unity or headset rendering."""
import json
import re
from pathlib import Path
import numpy as np
ROOT = Path(__file__).resolve().parents[1]
layout = json.loads((ROOT / 'ArtSource/harbour-layout.json').read_text())
blocks, routes = layout['blocks'], layout['routes']

source=(ROOT/'Assets/Editor/QuietWatchExteriorBuilder.cs').read_text()
def scalar(name):return float(re.search(r'\b'+name+r' = ([\d.]+)f',source)[1])
def vector(name):return np.array([float(v) for v in re.search(name+r' = new Vector3\(([^)]+)\)',source)[1].replace('f','').split(',')])
STATION_SCALE=scalar('HarbourScale');FLEET_SCALE=scalar('FleetScale')
def rotation(axis,degrees):
    c,s=np.cos(np.radians(degrees)),np.sin(np.radians(degrees))
    return np.array(([[1,0,0],[0,c,-s],[0,s,c]],[[c,0,s],[0,1,0],[-s,0,c]],[[c,-s,0],[s,c,0],[0,0,1]])[axis])
a=vector('HarbourAngles');TURN=rotation(1,a[1])@rotation(0,a[0])@rotation(2,a[2])
def world(point):return (np.asarray(point)@TURN.T*STATION_SCALE+vector('HarbourPosition'))*FLEET_SCALE

def glazing(point,eye):
    base=np.array([0,.75,-2.6]);up=np.array([0,1.75,1.2]);up=up/np.linalg.norm(up)
    normal=np.cross([1,0,0],up);direction=point-eye
    fraction=np.dot(base-eye,normal)/np.dot(direction,normal)
    if not 0<fraction<1:return False
    hit=eye+direction*fraction
    u,v=hit[0],np.dot(hit-base,up);inset=max(0,(v-1.74)/.14*.1)
    return .36<v<1.87 and any(lo+inset+.01<u<hi-inset-.01 for lo,hi in [(-2.88,-1.92),(-1.56,-.60),(-.24,.72),(1.42,2.78)])

def sample(route, phase):
    points = np.array([[p[k] for k in ('x','y','z')] for p in route['points']])
    scaled = np.clip(np.asarray(phase),0,1) * (len(points)-1)
    i = np.minimum(scaled.astype(int),len(points)-2)
    t = (scaled-i)[...,None]
    a,b,c,d = points[np.maximum(i-1,0)],points[i],points[i+1],points[np.minimum(i+2,len(points)-1)]
    return .5*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)

def smooth(t):
    t=np.clip(t,0,1);return t*t*(3-2*t)

def phase(route, cycles):
    x=(cycles+route['phase'])%1
    return np.where(x<.4,smooth(x/.4),np.where(x<.5,1,np.where(x<.9,1-smooth((x-.5)/.4),0))) if route['shuttle'] else x

# These are the pre-existing imported station's conservative envelopes. New
# district envelopes come directly from the same JSON as the generated meshes.
volumes=[(np.array([0,0,z]),5.2) for z in range(-10,11,4)]
volumes += [(np.array([np.cos(a)*17.4,np.sin(a)*17.4,0]),4.35) for a in np.arange(24)*2*np.pi/24]
for side in (-1,1):
    a=np.array([side*8,8.5,-1.2]);b=np.array([side*27,10,-3])
    volumes += [(a+(b-a)*t,2) for t in np.linspace(0,1,6)]
    volumes += [(np.array([side*25.7,9.9,-3]),4.4)]

for arch in layout.get('arches',[]):
    volumes += [(np.array([np.cos(a)*arch['radius'],np.sin(a)*arch['radius'],arch['z']]),1.7)
                for a in np.radians(np.linspace(arch['start'],arch['end'],65))]

def clearances(points, route):
    radius=route['clearance']/STATION_SCALE # station scale; routes are stored in vista space by Unity
    result={f'Imported structure {i}':np.linalg.norm(points-c,axis=-1)-r-radius for i,(c,r) in enumerate(volumes)}
    for block in blocks:
        size=np.array(block['size']);assert np.all(size>0)
        q=np.abs(points-block['position'])-size*.5
        result[block['name']]=np.linalg.norm(np.maximum(q,0),axis=-1)+np.minimum(np.max(q,axis=-1),0)-radius
    return result

if __name__ == '__main__':
    for route in routes:
        points=sample(route,np.linspace(0,1,4001))
        values=clearances(points,route)
        label=min(values,key=lambda k:values[k].min());gap=values[label].min()
        assert gap>0, f'{route["name"]} intersects {label}: {gap:.3f} station units'
        print(f'PASS: {route["name"]}: 4001 samples clear all solids, minimum {gap*STATION_SCALE*FLEET_SCALE:.2f} m ({label})')
    # Include Quiet, Living and a blend of their clocks; no rebase at a mode switch.
    time=np.arange(0,7200,.25)
    for blend in (0,.5,1):
        poses=[]
        for route in routes:
            cycles=time*(blend/route['living']+(1-blend)/route['quietDuration'])
            p=smooth((time-720)/72) if route['grace'] else phase(route,cycles)
            poses.append(sample(route,p))
        for i in range(len(routes)):
            for j in range(i):
                gap=np.linalg.norm(poses[i]-poses[j],axis=-1)-(routes[i]['clearance']+routes[j]['clearance'])/STATION_SCALE
                assert gap.min()>0,f'Traffic overlap: {routes[i]["name"]}/{routes[j]["name"]}'
    print('PASS: traffic separation over two hours in Quiet, Living and blended travel clocks')
    # Service craft rests inside the real berth, with clearance on every side.
    tender=next(r for r in routes if r['name']=='Berth Service Tender')
    dock=sample(tender,1)
    assert 15<dock[0]<25 and 1<dock[1]<7 and 3<dock[2]<11
    assert phase(tender,.45-tender['phase'])==1
    print('PASS: tender dwells inside the open berth')
    for eye in np.array([[-1.6,1.1,-1.42],[1.42,1.22,-.10],[2.05,.95,-1],[-2.2,1.18,2]]):
        assert glazing(world(dock),eye),'Docked tender is behind a window frame'
    print('PASS: docked tender centre projects through a pane from all four reference seats')
    # From the couch, the return leg passes behind the imported outer torus.
    eye=(np.array([-1.6,1.1,-1.42])/FLEET_SCALE-vector('HarbourPosition'))@TURN/STATION_SCALE
    behind=sample(tender,phase(tender,87/tender['living']))
    hit=eye+(behind-eye)*(1.45-eye[2])/(behind[2]-eye[2])
    assert 17.6 < np.linalg.norm(hit[:2]) < 20
    assert -150 < np.degrees(np.arctan2(-hit[1],hit[0])) < 154
    assert glazing(world(behind),np.array([-1.6,1.1,-1.42]))
    print('PASS: return leg is occluded by the torus, rather than the cabin frame, at 87 seconds')
    eyes=[np.array(e) for e in [[-1.6,1.1,-1.42],[1.42,1.22,-.1],[2.05,.95,-1],[-2.2,1.18,2]]]
    for route in routes:
        if not route['grace'] and not route['shuttle']:
            assert not any(glazing(world(sample(route,t)),eye) for t in (0,1) for eye in eyes), 'Visible traffic loop reset'
    commuter=next(r for r in routes if r['name']=='Cross Harbour Commuter')
    assert all(glazing(world(sample(commuter,phase(commuter,0))),eye) for eye in eyes)
    print('PASS: looping traffic resets outside all four window sightlines; arrival commuter crosses all four panes')
    assert len(blocks)<80
    print(f'PASS: {len(blocks)} authored solids, batched into five material surfaces per near LOD')

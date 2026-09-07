"""Source projection study, NOT a Unity render. Uses the actual C# catalogue export.

Run .NET checks with a second argument /tmp/catalogue.json, then run this tool
with that JSON and an output directory. Models point filtering, Unity camera
handedness and cabin glazing. Omits glass, URP bloom, MSAA, headset optics and room.
"""
import json,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tests'))
from check_harbour_layout import glazing
SEATS=[('couch',[-1.6,1.1,-1.42],[0,1.45,-7]),('bed-sitting',[1.42,1.22,-.1],[.4,1.55,-7]),
       ('bed-reclining',[2.05,.95,-1],[.6,1.55,-7]),('desk',[-2.2,1.18,2],[-.5,1.4,-7])]

def unit(v):return v/np.linalg.norm(v,axis=-1,keepdims=True)
def mix(v):
 v=np.asarray(v,dtype=np.uint32);v=v^(v>>16);v=v*np.uint32(0x7feb352d)
 v=v^(v>>15);v=v*np.uint32(0x846ca68b);return v^(v>>16)
def noise(v):return (mix(v)&0xffffff)/16777216

def positions(model,seconds):
 stars=model['stars'];p=np.array([[s[k] for k in ('X','Y','Z')] for s in stars],float)
 eased=seconds-2*(1-np.exp(-seconds/2));distance=eased*model['speed'];period=model['period']
 cycles=np.floor((p[:,0]+distance+period*.5)/period).astype(np.uint32)
 p[:,0]+=distance-cycles.astype(float)*period
 seed=np.arange(1,len(stars)+1,dtype=np.uint32)^(cycles*np.uint32(0x9e3779b9))
 depth=8000+36000*noise(seed)
 p[:,1]=np.where(cycles==0,p[:,1],(-.72+1.44*noise(seed+np.uint32(1)))*depth)
 p[:,2]=np.where(cycles==0,p[:,2],-depth)
 t=np.clip((np.abs(p[:,0])-period*.4)/(period*.1),0,1);fade=1-t*t*(3-2*t)
 return p,fade

def project(p,eye,target,w,h):
 forward=unit(np.asarray(target)-eye)
 # Unity left-handed view: +Z forward; camera right = UP cross FORWARD.
 right=unit(np.cross([0,1,0],forward));up=unit(np.cross(forward,right))
 delta=p-eye;z=delta@forward;scale=h/(2*np.tan(np.radians(30)))
 return np.stack([w*.5+(delta@right)*scale/z,h*.5-(delta@up)*scale/z],axis=-1)

def render(model,seconds,seat,output):
 from PIL import Image,ImageDraw
 w,h=1536,1024;name,eye,target=seat;eye=np.array(eye,float)
 p,fade=positions(model,seconds);xy=project(p,eye,target,w,h)
 rgb=np.zeros((h,w,3),float);count=0
 for i,(x,y) in enumerate(xy):
  if not (4<x<w-4 and 4<y<h-4) or fade[i]<.1 or not glazing(p[i],eye):continue
  star=model['stars'][i];sigma=np.sqrt(star['Sigma']**2+.18);extent=sigma*3.5
  x0,x1=max(0,int(x-extent)),min(w,int(x+extent)+2);y0,y1=max(0,int(y-extent)),min(h,int(y+extent)+2)
  gy,gx=np.mgrid[y0:y1,x0:x1];core=np.exp(-.5*((gx+.5-x)**2+(gy+.5-y)**2)/sigma**2)
  energy=np.array([star[k] for k in ('R','G','B')])*star['Flux']*fade[i]/sigma**2
  rgb[y0:y1,x0:x1]+=1-np.exp(-core[...,None]*energy);count+=1
 image=Image.fromarray(np.uint8(np.clip(rgb,0,1)**(1/2.2)*255))
 draw=ImageDraw.Draw(image);draw.text((24,22),f'SOURCE PROJECTION ONLY / {name.upper()} / CRUISE {seconds}s / {count} POINTS IN GLAZING',fill=(115,123,132))
 draw.text((24,h-30),'Actual C# catalogue. Unity camera handedness. No room, glass, bloom or headset validation.',fill=(100,108,115))
 image.save(output);return count

if __name__=='__main__':
 model=json.loads(Path(sys.argv[1]).read_text());folder=Path(sys.argv[2]);folder.mkdir(parents=True,exist_ok=True)
 counts={}
 for seat in SEATS:
  counts[seat[0]]={s:render(model,s,seat,folder/f'{seat[0]}-{s:04d}.png') for s in (0,12,45,600,3600)}
 print(json.dumps(counts,indent=2))
 # Structural framing check for the small comet (actual endpoint, before glass/occlusion).
 p=np.array([model['comet'][k] for k in ('X','Y','Z')]);print('Comet nucleus in panes:',{s[0]:glazing(p,np.array(s[1],float)) for s in SEATS})

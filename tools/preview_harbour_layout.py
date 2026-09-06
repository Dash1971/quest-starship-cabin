"""CPU massing/framing study, NOT a Unity render or visual approval.
Approximate the existing ring, draw authored districts, and mask the cabin panes.
Omits imported asset detail, URP lighting, compression, stereo, and ship hulls.
"""
from pathlib import Path
import sys
import numpy as np
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tests'))
from check_harbour_layout import layout, sample, phase, world, TURN

def unit(v):return v/np.linalg.norm(v,axis=-1,keepdims=True)

def render(seconds):
 w,h=1200,800;eye=np.array([-1.6,1.1,-1.42]);forward=unit(np.array([0,1.45,-7])-eye)
 right=unit(np.cross(forward,[0,1,0]));up=unit(np.cross(right,forward));scale=h/(2*np.tan(np.radians(30)))
 image=Image.new('RGB',(w,h),(3,6,12));draw=ImageDraw.Draw(image)
 faces=[]
 def polygon(p,color):
  q=world(p)-eye;z=q@forward
  if np.any(z<=0):return
  xy=np.stack([w/2+(q@right)*scale/z,h/2-(q@up)*scale/z],-1)
  faces.append((z.mean(),list(map(tuple,xy)),tuple(int(np.clip(c,0,255)) for c in color)))
 def box(center,size,color):
  for n in np.vstack((np.eye(3),-np.eye(3))):
   u=np.array([1,0,0]) if abs(n[1])>.5 else np.array([0,1,0]);v=np.cross(n,u)
   p=np.array(center)+n*np.array(size)*.5;u=u*size*.5;v=v*size*.5
   lighting=.3+.7*max(0,np.dot(TURN@n,unit(np.array([-.55,.55,.5]))))
   polygon([p-u-v,p+u-v,p+u+v,p-u+v],np.array(color)*lighting)
 # Approximate imported ring, axial core and piers, solely to read massing.
 for inner,outer,z,color in [(17.6,20,1.45,[135,145,160]),(15.6,17.25,1.9,[72,86,98])]:
  for a in np.linspace(-150,150,100):
   b=a+3;dirs=np.array([[np.cos(np.radians(k)),-np.sin(np.radians(k)),0] for k in (a,b)])
   polygon([dirs[0]*inner+[0,0,z],dirs[1]*inner+[0,0,z],dirs[1]*outer+[0,0,z],dirs[0]*outer+[0,0,z]],color)
 box([0,0,0],[7,7,18],[60,75,88])
 for side in (-1,1):box([side*25.7,9.9,-3],[8,3,5],[112,123,134])
 for arch in layout.get('arches',[]):
  for a,b in zip(np.linspace(arch['start'],arch['end'],65)[:-1],np.linspace(arch['start'],arch['end'],65)[1:]):
   def pt(deg,r,z):return [np.cos(np.radians(deg))*r,np.sin(np.radians(deg))*r,z]
   r0,r1=arch['radius']-arch['width']/2,arch['radius']+arch['width']/2;z=arch['z']+arch['depth']/2
   polygon([pt(a,r0,z),pt(b,r0,z),pt(b,r1,z),pt(a,r1,z)],[155,169,175])
   polygon([pt(a,r0+.20,z+.015),pt(b,r0+.20,z+.015),pt(b,r0+.32,z+.015),pt(a,r0+.32,z+.015)],[100,195,220])
 colors={'hull':[138,153,164],'dark':[38,49,58],'ochre':[137,85,42]}
 for b in layout['blocks']:
  box(b['position'],b['size'],colors[b['material']])
  if b['windows']:
   c=np.array(b['position']);s=np.array(b['size'])
   for deck in range(int(s[1]/.36)):
    for bay in range(int((s[0]-.4)/.25)):
     if (bay*17+deck*11)%13<5:continue
     p=c+[-s[0]/2+.25+bay*.25,-s[1]/2+.22+deck*.36,s[2]/2+.015]
     polygon([p+[-.045,-.032,0],p+[.045,-.032,0],p+[.045,.032,0],p+[-.045,.032,0]],[255,191,115])
 for r in layout['routes']:
  p=0 if r['grace'] else phase(r,seconds/r['living'])
  size=[1, .5, 1.5] if r['shuttle'] else [7,2,15] if r['family']=='CommandShip' else [3,1,4]
  box(sample(r,p),size,[180,181,170])
 for _,xy,color in sorted(faces,reverse=True,key=lambda f:f[0]):draw.polygon(xy,fill=color)
 # Cabin's four sloped pane openings, using the same coordinates as source checks.
 xx,yy=np.meshgrid((np.arange(w)+.5-w/2)/scale,(h/2-np.arange(h)-.5)/scale)
 ray=forward+xx[...,None]*right+yy[...,None]*up
 base=np.array([0,.75,-2.6]);sloped=unit(np.array([0,1.75,1.2]));normal=np.cross([1,0,0],sloped)
 distance=np.dot(base-eye,normal)/(ray@normal);hit=eye+ray*distance[...,None]
 u=hit[...,0];v=(hit-base)@sloped;inset=np.maximum(0,(v-1.74)/.14*.1)
 mask=np.zeros((h,w),bool)
 for a,b in [(-2.88,-1.92),(-1.56,-.60),(-.24,.72),(1.42,2.78)]:mask|=(u>a+inset)&(u<b-inset)
 mask&=(v>.35)&(v<1.88)&(distance>0)
 pixels=np.array(image);pixels[~mask]=[32,30,29];image=Image.fromarray(pixels)
 draw=ImageDraw.Draw(image);draw.rectangle((0,0,w,50),fill=(12,14,18))
 draw.text((15,15),f'CPU MASSING STUDY / {seconds}s / NOT UNITY OR HEADSET CAPTURE',fill='white')
 path=ROOT/f'Builds/ArtReview/harbour-layout-{seconds:03}s.png';path.parent.mkdir(parents=True,exist_ok=True);image.save(path);print(path)
if __name__=='__main__':
 for seconds in (0,10,32,87):render(seconds)

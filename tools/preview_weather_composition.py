"""CPU geometry/material study from authored values. NOT a Unity render.

Used to check framing, shadow geometry and texture placement before handing the
branch to Unity. Does not simulate the room, URP, XR, MSAA, bloom or tone mapping.
Requires NumPy and Pillow; writes only ignored Builds/ArtReview output.
"""
from pathlib import Path
import re
import numpy as np
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
SOURCE=(ROOT/'Assets/Editor/QuietWatchExteriorBuilder.cs').read_text()
def vector(name):
    return np.array([float(x) for x in re.search(name+r' = new Vector3\(([^)]+)\)',SOURCE)[1].replace('f','').split(',')])
def number(name): return float(re.search(r'\b'+name+r' = ([\d.]+)f',SOURCE)[1])
def unit(x): return x/np.maximum(1e-10,np.linalg.norm(x,axis=-1,keepdims=True))
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1); return t*t*(3-2*t)
def rotation(axis,d):
    c,s=np.cos(np.radians(d)),np.sin(np.radians(d))
    return np.array(([[1,0,0],[0,c,-s],[0,s,c]],[[c,0,s],[0,1,0],[-s,0,c]],[[c,-s,0],[s,c,0],[0,0,1]])[axis])
def euler(angles): return rotation(1,angles[1])@rotation(0,angles[0])@rotation(2,angles[2])
def density(r,inner,outer):
    t=np.clip((r-inner)/(outer-inner),0,1)
    return np.clip((.68+.18*np.sin(r*2.9)+.10*np.sin(r*9.7))*smooth(0,.025,t)*(1-smooth(.965,1,t))*(1-.92*np.exp(-((t-.64)/.027)**2)),0,1)
def sample(texture,normal,rotation):
    globe=normal@rotation
    u=np.arctan2(globe[...,0],-globe[...,2])/(2*np.pi)+.5
    v=np.arcsin(np.clip(globe[...,1],-1,1))/np.pi+.5
    x=(u%1)*(texture.shape[1]-1); y=(1-v)*(texture.shape[0]-1)
    x0=x.astype(int); y0=y.astype(int);fx=(x-x0)[...,None];fy=(y-y0)[...,None]
    a=texture[y0,x0]*(1-fx)+texture[y0,(x0+1)%texture.shape[1]]*fx
    b=texture[np.minimum(y0+1,texture.shape[0]-1),x0]*(1-fx)+texture[np.minimum(y0+1,texture.shape[0]-1),(x0+1)%texture.shape[1]]*fx
    return a*(1-fy)+b*fy

def render(center,radius,inner,outer,moonStart,moonTravel,moonDiameter,sun,ringRotation,texture,event=0):
    w,h=1200,640
    eye=np.array([-1.6,1.1,-1.42]);forward=unit(np.array([0,1.45,-7])-eye)
    right=unit(np.cross(forward,[0,1,0]));up=unit(np.cross(right,forward))
    x,y=np.meshgrid((np.arange(w)+.5)/w*2-1,1-(np.arange(h)+.5)/h*2)
    ray=unit(forward+x[...,None]*np.tan(np.radians(55))*right+y[...,None]*np.tan(np.radians(55))*h/w*up)
    color=np.zeros((h,w,3))+.001; depth=np.full((h,w),np.inf)
    normal=ringRotation@[0,0,1]
    def ring_shadow(point):
        distance=np.sum((center-point)*normal,axis=-1)/np.dot(sun,normal)
        intersection=point+sun*distance[...,None]
        return np.where(distance>0,1-density(np.linalg.norm(intersection-center,axis=-1),inner,outer)*.92,1)
    def sphere(c,r,giant):
        oc=eye-c; b=np.sum(ray*oc,axis=-1); disc=b*b-np.dot(oc,oc)+r*r
        t=-b-np.sqrt(np.maximum(0,disc)); hit=(disc>0)&(t>0)&(t<depth)
        point=eye+ray*t[...,None];n=unit(point-c);sd=np.sum(n*sun,axis=-1)
        light=smooth(-.04,.085,sd)*ring_shadow(point)
        if giant:
            albedo=sample(texture,n,euler([0,-22,-8]))[...,:3]
            linear=np.where(albedo<=.04045,albedo/12.92,((albedo+.055)/1.055)**2.4)
            shade=linear*(.018+light*(.16+1.12*np.clip(sd,0,1)))[...,None]
            rim=(1-np.clip(np.sum(n*-ray,axis=-1),0,1))**2.35
            shade+=np.array([.62,.52,.39])*rim[...,None]*smooth(-.18,.42,sd)[...,None]*.20
            shade=1-np.exp(-shade*1.58)
        else:
            shade=np.array([.38,.35,.31])*(.035+light*1.08)[...,None]
        color[hit]=shade[hit];depth[hit]=t[hit]
    sphere(center,radius,True)
    radial=ringRotation@[-1,0,0]
    sphere(center+radial*(moonStart+moonTravel*event)-sun*8,moonDiameter*.5,False)
    # The shared analytic ring plane and planet shadow.
    denominator=np.sum(ray*normal,axis=-1)
    t=np.dot(center-eye,normal)/np.where(np.abs(denominator)>.00001,denominator,.00001)
    point=eye+ray*t[...,None];rad=np.linalg.norm(point-center,axis=-1)
    mask=(t>0)&(t<depth)&(rad>inner)&(rad<outer)
    d=density(rad,inner,outer);toCenter=center-point;along=np.sum(toCenter*sun,axis=-1)
    separation=np.sqrt(np.maximum(0,np.sum(toCenter**2,axis=-1)-along**2))
    transmission=np.where(along>0,smooth(radius-.35,radius+.35,separation),1)
    ringColor=(np.array([.11,.09,.075])+(np.array([.71,.62,.48])-np.array([.11,.09,.075]))*d[...,None])
    ringColor*=(.12+.88*transmission*(.3+.7*abs(np.dot(normal,sun))))[...,None]
    alpha=d*.68*mask
    color=color*(1-alpha[...,None])+ringColor*alpha[...,None]
    # Atmosphere study outside the opaque silhouette.
    toCenter=center-eye;tangent=ray*np.sum(ray*toCenter,axis=-1)[...,None]-toCenter
    altitude=(np.linalg.norm(tangent,axis=-1)-radius)/radius
    incidence=np.sum(unit(tangent)*sun,axis=-1);day=smooth(-.12,.28,incidence)
    twilight=np.exp(-((incidence+.015)/.10)**2)
    a=np.exp(-np.maximum(0,altitude)/.006)*(altitude>0)*(.012+day*.58+twilight*.22)
    glow=np.array([.46,.64,.82])+(np.array([1,.27,.055])-np.array([.46,.64,.82]))*twilight[...,None]*.68
    color=color*(1-a[...,None])+glow*a[...,None]*1.55
    encoded=np.where(color<=.0031308,color*12.92,1.055*np.maximum(color,0)**(1/2.4)-.055)
    return Image.fromarray((np.clip(encoded,0,1)*255+.5).astype('uint8'))

if __name__=='__main__':
    texture=np.array(Image.open(ROOT/'Assets/Art/QuietWatch/Textures/QW_GreatWeather.png'))/255
    image=render(vector('WeatherCenter'),number('WeatherRadius'),number('RingInner'),number('RingOuter'),
        number('MoonShadowRadius'),number('MoonTravel'),number('MoonDiameter'),unit(vector('WeatherSun')),euler(vector('WeatherRingAngles')),texture)
    draw=ImageDraw.Draw(image)
    draw.rectangle((0,0,1200,46),fill=(15,18,24))
    draw.text((18,12),'GREAT WEATHER | CPU composition study - not a Unity or headset capture',fill=(225,230,238))
    destination=ROOT/'Builds/ArtReview/weather-composition-study.png'
    destination.parent.mkdir(parents=True,exist_ok=True);image.save(destination);print(destination)

"""Original cloud relief derived from the authored atlas; requires NumPy.

2K linear RGBA: east/north surface slopes, upper-cloud coverage, height.
Slopes correspond to 0.0012 planet radii of authored height variation.
This is a shallow shading approximation, not volumetric cloud geometry.
"""
from pathlib import Path
import numpy as np
from generate_great_weather import generate, save_png

WIDTH, HEIGHT = 2048, 1024
LAYER_HEIGHT = .0012
SLOPE_RANGE = .125

def generate_relief():
    source=generate().astype(np.float32)/255
    small=source.reshape(HEIGHT,2,WIDTH,2,4).mean(axis=(1,3))
    tone=small[...,:3]@np.array([.24,.56,.20],dtype=np.float32)
    height=np.clip((tone-.22)/.64 + small[...,3]*.10,0,1)
    # Suppress sub-texel normals while retaining large sheared storm ridges.
    height=(height*4+np.roll(height,1,1)+np.roll(height,-1,1)
            +np.concatenate((height[:1],height[:-1]))+np.concatenate((height[1:],height[-1:])))/8
    latitude=np.linspace(np.pi/2,-np.pi/2,HEIGHT)[:,None]
    east=(np.roll(height,-1,1)-np.roll(height,1,1))*WIDTH*.5*LAYER_HEIGHT/(2*np.pi*np.maximum(.08,np.cos(latitude)))
    north=-np.gradient(height,axis=0)*(HEIGHT-1)*LAYER_HEIGHT/np.pi
    pole=np.clip(np.cos(latitude)/.12,0,1)
    east*=pole; north*=pole
    upper=np.clip((height-.50)/.35,0,1)
    packed=np.dstack((np.clip(east/SLOPE_RANGE,-1,1)*.5+.5,np.clip(north/SLOPE_RANGE,-1,1)*.5+.5,upper,height))
    return (packed*255+.5).astype(np.uint8)

if __name__=='__main__':
    path=Path(__file__).resolve().parents[1]/'Assets/Art/QuietWatch/Textures/QW_WeatherRelief.png'
    save_png(path,generate_relief());print(path)

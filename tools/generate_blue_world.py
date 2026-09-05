"""Bake original fictional terrain, cloud systems and night settlements.

No downloaded imagery. RGBA terrain: sRGB albedo + linear land mask.
RGBA weather: linear cloud coverage, cloud relief, settlement radiance, opaque.
Generate in strips to bound memory. Requires NumPy.
"""
from pathlib import Path
import numpy as np
from generate_great_weather import save_png

WIDTH, HEIGHT = 2048, 1024

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)

def noise(p):
    cell=np.floor(p).astype(np.int64)
    f=p-cell
    f=f*f*(3-2*f)
    result=np.zeros(p.shape[:-1],np.float64)
    for x in (0,1):
        for y in (0,1):
            for z in (0,1):
                h=((cell[...,0]+x)*73856093)^((cell[...,1]+y)*19349663)^((cell[...,2]+z)*83492791)
                h=(h^(h>>13))*1274126177
                value=(h&0xffffffff)/4294967295
                weight=(f[...,0] if x else 1-f[...,0])*(f[...,1] if y else 1-f[...,1])*(f[...,2] if z else 1-f[...,2])
                result+=value*weight
    return result

def fbm(p, octaves=5):
    value=np.zeros(p.shape[:-1])
    weight,total=1.,0.
    for octave in range(octaves):
        value+=noise(p)*weight
        total+=weight
        weight*=.51
        p=p*2.03+np.array([13.7,4.3,-8.2])
    return value/total

def generate_rows(first,last):
    u,v=np.meshgrid(np.arange(WIDTH)/WIDTH,1-np.arange(first,last)/(HEIGHT-1))
    lon=(u-.5)*2*np.pi; lat=(v-.5)*np.pi
    p=np.stack([np.sin(lon)*np.cos(lat),np.sin(lat),-np.cos(lon)*np.cos(lat)],-1)
    warp=np.stack([noise(p*3+7),noise(p*3-13),noise(p*3+31)],-1)-.5
    terrain=fbm(p*2.7+warp*.9+np.array([4,8,1]),6)
    land=smooth(.475,.492,terrain)
    height=smooth(.49,.66,terrain)
    moisture=fbm(p*7+19,4)
    desert=smooth(.48,.62,moisture)*(1-smooth(.55,.8,np.abs(p[...,1])))
    green=np.array([.14,.25,.12]); sand=np.array([.59,.46,.28])
    albedo=green+(sand-green)*desert[...,None]
    ridge=1-np.abs(noise(p*54+17)*2-1)
    albedo*= (.70+height*.42+ridge*.13)[...,None]
    ice=smooth(.84,.98,np.abs(p[...,1])+height*.12)
    albedo=albedo*(1-ice[...,None])+np.array([.82,.88,.91])*ice[...,None]
    shelf=np.exp(-np.abs(terrain-.481)*125)*(1-land)
    ocean=np.zeros_like(albedo)+np.array([.019,.065,.13])
    ocean+=np.array([.025,.22,.19])*shelf[...,None]
    albedo=ocean*(1-land[...,None])+albedo*land[...,None]
    # Curl the spherical weather coordinates into large midlatitude cyclones.
    q=p.copy()
    storm=np.zeros_like(u)
    for cx,cy,strength in ((.12,.66,4.2),(.75,.32,-3.6),(.49,.70,3.7),(.88,.58,3.4)):
        dx=((u-cx+.5)%1-.5)/.085; dy=(v-cy)/.075
        radius=np.sqrt(dx*dx+dy*dy)
        angle=strength*np.exp(-radius*radius*.7)
        cl=lon+(dx*np.cos(angle)-dy*np.sin(angle)-dx)*.085*2*np.pi
        ct=lat+(dx*np.sin(angle)+dy*np.cos(angle)-dy)*.075*np.pi
        blend=np.exp(-radius*radius*.4)
        curled=np.stack([np.sin(cl)*np.cos(ct),np.sin(ct),-np.cos(cl)*np.cos(ct)],-1)
        q=q*(1-blend[...,None])+curled*blend[...,None]
        storm=np.maximum(storm,np.exp(-radius*radius*6))
    cloudBase=fbm(q*np.array([7.,16.,7.])+41,5)
    detail=fbm(q*65-5,3)
    cloud=smooth(.46,.62,cloudBase+detail*.08)
    cloud*= .72+.28*detail
    cloud=np.clip(cloud-storm*.55,0,1)
    # Settlement networks favor coasts and temperate land, with broad dark gaps.
    coast=np.exp(-np.abs(terrain-.495)*50)*land
    population=smooth(.50,.61,fbm(p*14-16,3))*(1-ice)*(1-desert*.7)
    grid=noise(p*220+7)
    fine=noise(p*530-4)
    cities=smooth(.60,.80,grid)*smooth(.43,.70,fine)*population*(.2+.8*coast)
    surface=np.dstack([np.clip(albedo,0,1),land])
    clouds=np.dstack([cloud,detail,np.clip(cities*4,0,1),np.ones_like(u)])
    return (surface*255+.5).astype(np.uint8),(clouds*255+.5).astype(np.uint8)

def generate():
    strips=[generate_rows(first,min(HEIGHT,first+64)) for first in range(0,HEIGHT,64)]
    return tuple(np.concatenate([pair[index] for pair in strips]) for index in (0,1))

if __name__=='__main__':
    target=Path(__file__).resolve().parents[1]/'Assets/Art/QuietWatch/Textures'
    for name,pixels in zip(('QW_BlueSurface.png','QW_BlueClouds.png'),generate()):
        save_png(target/name,pixels)
        print(target/name)

"""Original galactic panorama: multiscale stellar bulge, filamentary extinction and emission.
No external images. Uses periodic noise; pole rows converge to one colour.
"""
from pathlib import Path
import numpy as np
from generate_great_weather import save_png
WIDTH,HEIGHT=2048,1024

def noise(x,y,seed=0):
    xi=np.floor(x).astype(np.int64);yi=np.floor(y).astype(np.int64)
    fx=x-xi;fy=y-yi;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
    def h(i,j):
        n=(i%256)*374761393+(j%256)*668265263+seed*1442695041
        n=(n^(n>>13))*1274126177
        return ((n^(n>>16))&0xffffff)/0xffffff
    return (h(xi,yi)*(1-fx)+h(xi+1,yi)*fx)*(1-fy)+(h(xi,yi+1)*(1-fx)+h(xi+1,yi+1)*fx)*fy

def field(x,y,seed):
    total=np.zeros_like(x)
    for i in range(6):total+=noise(x*2**i,y*2**i,seed+i)*(.5**(i+1))
    return total

def generate():
    rows=[]
    for start in range(0,HEIGHT,64):
        u,v=np.meshgrid(np.arange(WIDTH)/WIDTH,1-np.arange(start,min(start+64,HEIGHT))/(HEIGHT-1))
        lon=u*2*np.pi
        # Periodic sky coordinates; the forward core sits at +20 degrees azimuth.
        spine=.5+.09*np.sin(lon-.4)+.018*np.sin(lon*3+.7)
        d=v-spine
        x=np.cos(lon)*3+8;y=np.sin(lon)*3+v*10+8
        warp=field(x,y,31)-.5
        fine=field(x*8+warp*2,y*8,61)
        cloud=field(x*3+warp,y*3,7)
        band=np.exp(-(d/(.031+cloud*.032))**2)
        bulge=np.exp(-(((u-.555)/.075)**2+((v-.543)/.041)**2)*.5)
        split=d+.012*np.sin(lon*9+warp*6)
        dust=np.exp(-(split/.012)**2)*(0.55+cloud*.65)
        fingers=np.clip((field(x*5,y*5,97)-.40)*3,0,1)*band
        extinction=np.clip(dust+fingers*.75,0,.98)
        stars=(band*(.18+fine*.40)+bulge*.68)*(1-extinction*.94)
        rgb=stars[...,None]*np.array([.95,.80,.63])
        rgb+=(band*cloud*(1-extinction)*.14)[...,None]*np.array([.22,.39,.80])
        knots=np.maximum(0,fine-.64)**1.2*band*3
        rgb+=knots[...,None]*np.array([.65,.13,.20])
        polar=np.clip(np.sin(v*np.pi)*5,0,1)
        rgba=np.dstack((np.clip(rgb*polar[...,None],0,1),extinction*polar))
        rows.append((rgba*255+.5).astype(np.uint8))
    return np.concatenate(rows)
if __name__=='__main__':
    path=Path(__file__).resolve().parents[1]/'Assets/Art/QuietWatch/Textures/QW_GalacticSky.png'
    save_png(path,generate());print(path)

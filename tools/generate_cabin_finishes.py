"""Original restrained wood, woven cloth and paper; tileable at physical metre UVs."""
from pathlib import Path
import numpy as np
from generate_great_weather import save_png
ROOT=Path(__file__).resolve().parents[1]
def generate(size=512):
 y,x=np.mgrid[:size,:size]/size
 grain=np.sin(2*np.pi*(x*23+.28*np.sin(2*np.pi*y*2)+.08*np.sin(2*np.pi*y*7)))
 pores=np.sin(2*np.pi*x*137+.4*np.sin(2*np.pi*y*11))
 wood=.83+.055*grain+.016*pores+.024*np.cos(2*np.pi*x*5)
 weave=.91+.022*np.sin(2*np.pi*x*128)*np.cos(2*np.pi*y*128)+.014*np.cos(2*np.pi*x*7)*np.cos(2*np.pi*y*9)
 paper=.94+.009*np.sin(2*np.pi*x*51)*np.cos(2*np.pi*y*17)
 return {'QW_CabinWood':np.stack([wood,wood*.975,wood*.93],-1),
         'QW_CabinWeave':np.repeat(weave[...,None],3,-1),
         'QW_BookPaper':np.stack([paper,paper*.972,paper*.905],-1)}
if __name__=='__main__':
 for name,rgb in generate().items():
  rgba=np.concatenate([rgb,np.ones((*rgb.shape[:2],1))],-1)
  path=ROOT/'Assets/Art/QuietWatch/Textures'/f'{name}.png'
  save_png(path,np.uint8(np.clip(rgba,0,1)*255+.5));print(path)

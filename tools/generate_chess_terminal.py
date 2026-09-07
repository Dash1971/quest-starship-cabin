"""Original static phosphor chess display. NumPy only; no font/image downloads.

The historical position is recorded separately in ArtSource/chess-terminal.json.
The checked-in texture is all the runtime needs: no UI canvas, font atlas or chess engine.
"""
import json
from pathlib import Path
import numpy as np
from generate_great_weather import save_png
ROOT = Path(__file__).resolve().parents[1]
# Original hand-set 5x7 bitmap lettering. Kept here for platform-independent output.
FONT = dict(zip('ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 /.-:', [
'01110 10001 10001 11111 10001 10001 10001','11110 10001 10001 11110 10001 10001 11110',
'01111 10000 10000 10000 10000 10000 01111','11110 10001 10001 10001 10001 10001 11110',
'11111 10000 10000 11110 10000 10000 11111','11111 10000 10000 11110 10000 10000 10000',
'01111 10000 10000 10111 10001 10001 01111','10001 10001 10001 11111 10001 10001 10001',
'11111 00100 00100 00100 00100 00100 11111','00111 00010 00010 00010 10010 10010 01100',
'10001 10010 10100 11000 10100 10010 10001','10000 10000 10000 10000 10000 10000 11111',
'10001 11011 10101 10101 10001 10001 10001','10001 11001 10101 10011 10001 10001 10001',
'01110 10001 10001 10001 10001 10001 01110','11110 10001 10001 11110 10000 10000 10000',
'01110 10001 10001 10001 10101 10010 01101','11110 10001 10001 11110 10100 10010 10001',
'01111 10000 10000 01110 00001 00001 11110','11111 00100 00100 00100 00100 00100 00100',
'10001 10001 10001 10001 10001 10001 01110','10001 10001 10001 10001 01010 01010 00100',
'10001 10001 10001 10101 10101 11011 10001','10001 10001 01010 00100 01010 10001 10001',
'10001 10001 01010 00100 00100 00100 00100','11111 00001 00010 00100 01000 10000 11111',
'01110 10001 10011 10101 11001 10001 01110','00100 01100 00100 00100 00100 00100 01110',
'01110 10001 00001 00010 00100 01000 11111','11110 00001 00001 01110 00001 00001 11110',
'00010 00110 01010 10010 11111 00010 00010','11111 10000 10000 11110 00001 00001 11110',
'01110 10000 10000 11110 10001 10001 01110','11111 00001 00010 00100 01000 01000 01000',
'01110 10001 10001 01110 10001 10001 01110','01110 10001 10001 01111 00001 00001 01110',
'00000 00000 00000 00000 00000 00000 00000','00001 00010 00010 00100 01000 01000 10000',
'00000 00000 00000 00000 00000 00110 00110','00000 00000 00000 11111 00000 00000 00000',
'00000 00100 00100 00000 00100 00100 00000']))


def position(fen):
    rows=[]
    for row in fen.split()[0].split('/'):
        squares=[]
        for c in row:squares.extend(['']*int(c) if c.isdigit() else [c])
        assert len(squares)==8
        rows.append(squares)
    assert len(rows)==8
    return rows


def piece_mask(kind):
    y,x=np.mgrid[:20,:16]
    mask=((y>=17)&(y<=18)&(x>=2)&(x<=13)) | ((y==16)&(x>=3)&(x<=12))
    mask|=(y>=11)&(y<=15)&(abs(x-7.5)<=(y-8)*.65)
    if kind=='p':mask|=((x-7.5)**2+(y-7)**2<=10)
    if kind=='r':
        mask|=(y>=5)&(y<=11)&(x>=4)&(x<=11)
        mask|=(y>=3)&(y<=5)&(x>=3)&(x<=12)&((x<=5)|(x>=10)|(x==7)|(x==8))
    if kind=='n':
        mask|=(y>=4)&(y<=12)&(x>=4)&(x<=11)&(x+y>=12)
        mask|=(y>=7)&(y<=9)&(x>=1)&(x<=9)
        mask|=(y>=2)&(y<=5)&(x>=8)&(x<=10)
        mask[(y==6)&(x==6)]=False
    if kind=='b':
        mask|=(abs(x-7.5)/4+abs(y-6)/5<=1)
        mask[(x+y==14)&(y>=3)&(y<=7)]=False
        mask|=(y==11)&(x>=4)&(x<=11)
    if kind=='q':
        mask|=(y>=6)&(y<=10)&(abs(x-7.5)<=6-(y-6)*.7)
        for cx,cy in ((2,4),(7.5,2),(13,4)):
            mask|=((x-cx)**2+(y-cy)**2<=2.5)
        mask|=(y>=3)&(y<=7)&(x>=7)&(x<=8)
    if kind=='k':
        mask|=(y>=1)&(y<=6)&(x>=7)&(x<=8)
        mask|=(y>=3)&(y<=4)&(x>=5)&(x<=10)
        mask|=((x-7.5)/5)**2+((y-8)/3)**2<=1
    return np.repeat(np.repeat(mask,2,0),2,1)


def generate():
    image=np.full((640,1024,4),(10,18,17,255),dtype=np.uint8)
    green=(147,194,164,255);muted=(84,127,108,255);line=(42,72,60,255)
    def text(value,x,y,scale=3,color=green):
        for c in value:
            glyph=np.array([[v=='1' for v in row] for row in FONT[c].split()])
            glyph=np.repeat(np.repeat(glyph,scale,0),scale,1)
            region=image[y:y+7*scale,x:x+5*scale]
            assert region.shape[:2]==glyph.shape,(value,c,x,y)
            region[glyph]=color;x+=6*scale
    text('CHESS ARCHIVE / 006',32,24,3,muted)
    image[62:64,32:992]=line
    board=position(json.loads((ROOT/'ArtSource/chess-terminal.json').read_text())['fen'])
    for row in range(8):
        text(str(8-row),31,108+56*row,2,muted)
        for col in range(8):
            left=64+col*56;top=92+row*56
            image[top:top+56,left:left+56]=(25,42,34,255) if (col+row)%2==0 else (12,24,20,255)
            p=board[row][col]
            if not p:continue
            mask=piece_mask(p.lower());region=image[top+8:top+48,left+12:left+44]
            if p.isupper():region[mask]=green
            else:
                padded=np.pad(mask,1);outline=np.zeros_like(mask)
                for dy in range(3):
                    for dx in range(3):outline|=padded[dy:dy+40,dx:dx+32]
                # A three-pixel rim survives minification better than a hairline outline.
                interior=mask.copy()
                for dy in range(3):
                    for dx in range(3):interior &= padded[dy:dy+40,dx:dx+32]
                region[outline]=green;region[interior]=(14,27,22,255)
    for col in range(8):text('ABCDEFGH'[col],85+56*col,554,2,muted)
    image[92:572,543:545]=line
    text('FISCHER',578,99,5);text('SPASSKY',578,149,5)
    text('REYKJAVIK 1972',580,226,3,muted)
    text('GAME 6',580,260,3,muted)
    image[304:306,580:980]=line
    text('21. F4',580,339,5)
    text('BLACK TO MOVE',580,404,3,muted)
    text('POSITION STUDY',580,461,3,muted)
    text('WHITE / FILLED',580,519,2,muted)
    text('BLACK / OUTLINE',580,545,2,muted)
    image[591:593,32:992]=line
    text('WORLD CHAMPIONSHIP',32,611,2,muted)
    return image


if __name__=='__main__':
    path=ROOT/'Assets/Art/QuietWatch/Textures/QW_ChessTerminal.png'
    save_png(path,generate());print(path)

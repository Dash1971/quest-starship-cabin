"""Replay the sourced game, verify board orientation and deterministic shipped artwork."""
import json
import struct
import sys
import zlib
from pathlib import Path
import chess
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from generate_chess_terminal import generate, position, piece_mask
record=json.loads((ROOT/'ArtSource/chess-terminal.json').read_text())
board=chess.Board()
for san in record['san']:board.push_san(san)
assert board.is_valid() and board.fen()==record['fen']
assert len(board.move_stack)==41 and board.peek()==chess.Move.from_uci('f2f4')
assert board.turn==chess.BLACK
rows=position(record['fen'])
for row in range(8):
    for col in range(8):
        actual=board.piece_at(chess.square(col,7-row))
        assert rows[row][col]==(actual.symbol() if actual else ''),'Mirrored/flipped chess diagram'
assert len({piece_mask(kind).tobytes() for kind in 'pnbrqk'})==6,'Indistinguishable piece silhouettes'
print('PASS: Fischer-Spassky game 6 legally replayed through 21.f4; black to move; White at bottom')
a=generate();assert a.shape==(640,1024,4) and np.array_equal(a,generate())
blob=(ROOT/'Assets/Art/QuietWatch/Textures/QW_ChessTerminal.png').read_bytes()
assert blob[:8]==b'\x89PNG\r\n\x1a\n'
chunks=[];pos=8
while pos<len(blob):
    size=struct.unpack('>I',blob[pos:pos+4])[0]
    if blob[pos+4:pos+8]==b'IDAT':chunks.append(blob[pos+8:pos+8+size])
    pos+=size+12
raw=np.frombuffer(zlib.decompress(b''.join(chunks)),dtype=np.uint8).reshape(640,4097)
assert np.all(raw[:,0]==0) and np.array_equal(raw[:,1:].reshape(a.shape),a),'Shipped display differs from source'
assert np.max(a[...,:3])<=194 and np.all(a[...,3]==255),'Terminal must stay subdued and opaque'
print('PASS: original static chess bitmap reproduces exactly; bounded luminance; opaque background')
source=(ROOT/'Assets/Editor/QuietWatchChessTerminal.cs').read_text()
compact=''.join(source.split())
assert 'mesh.uv=new[]{Vector2.right,Vector2.zero,Vector2.up,Vector2.one};' in compact, \
    'Terminal quad must map image right to world +Z for the +X-facing display'
assert all(check in compact for check in (
    'mesh.uv[0]!=Vector2.right','mesh.uv[1]!=Vector2.zero',
    'mesh.uv[2]!=Vector2.up','mesh.uv[3]!=Vector2.one')), \
    'Unity validation must reject a mirrored terminal UV layout'
print('PASS: +X-facing terminal UVs preserve left-to-right artwork orientation')

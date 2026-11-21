# -*- coding: utf-8 -*-
import re, math
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# build transform world-ish positions by local pos only (approx, ignores parent chain for quick scan)
go_pos={}
go_name={}
for iid,(t,b) in blocks.items():
    if t==1:
        m=re.search(r'm_Name: (.*)', b)
        go_name[iid]=m.group(1).strip().strip('"') if m else '?'
for iid,(t,b) in blocks.items():
    if t==4:
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        pm=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        if gm and pm:
            go_pos[gm.group(1)]=(float(pm.group(1)),float(pm.group(2)))
# find colliders (BoxCollider2D 61, TilemapCollider2D 19719996, CompositeCollider2D 66, Polygon 60)
spawn=(-1.7,-30.44)
for iid,(t,b) in blocks.items():
    if t not in (61,60,58,70,19719996,66): continue
    gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
    if not gm: continue
    go=gm.group(1)
    pos=go_pos.get(go,(0,0))
    name=go_name.get(go,'?')
    # rough: only report if near spawn in local coords
    if abs(pos[0]-spawn[0])<20 and abs(pos[1]-spawn[1])<20:
        trig=re.search(r'm_IsTrigger: (\d)', b)
        is_trig=trig.group(1)=='1' if trig else False
        off=re.search(r'm_Offset: \{x: ([^,]+), y: ([^}]+)\}', b)
        sz=re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)\}', b)
        o=(float(off.group(1)),float(off.group(2))) if off else (0,0)
        ssz=(float(sz.group(1)),float(sz.group(2))) if sz else None
        print(f'{t} {name} go={go} pos={pos} trigger={is_trig} off={o} size={ssz}')

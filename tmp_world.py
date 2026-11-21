# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])

go_name={}
go_comp={}
for iid,(t,b) in blocks.items():
    if t==1:
        m=re.search(r'm_Name: (.*)', b)
        go_name[iid]=m.group(1).strip().strip('"') if m else '?'
        go_comp[iid]=[]
for iid,(t,b) in blocks.items():
    gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
    if gm and gm.group(1) in go_comp:
        go_comp[gm.group(1)].append((t,iid,b))

# transform for go
trans={}
for iid,(t,b) in blocks.items():
    if t==4:
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        pm=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        fm=re.search(r'm_Father: \{fileID: (-?\d+)\}', b)
        sm=re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        if gm:
            trans[gm.group(1)]={
                'tid':iid,
                'pos':(float(pm.group(1)),float(pm.group(2))) if pm else (0,0),
                'father':fm.group(1) if fm else '0',
                'scale':(float(sm.group(1)),float(sm.group(2))) if sm else (1,1)
            }

# world pos (ignoring rotation, good enough for axis-aligned)
from functools import lru_cache
@lru_cache(None)
def world(go):
    if go not in trans: return (0,0),(1,1)
    t=trans[go]
    f=t['father']
    if f=='0' or f not in trans:
        # find GO of father transform
        pass
    # resolve father transform -> go
    fgo=None
    for g,td in trans.items():
        if td['tid']==f:
            fgo=g; break
    if fgo is None:
        return t['pos'], t['scale']
    fp,fs=world(fgo)
    lp=t['pos']; ls=t['scale']
    return (fp[0]+lp[0]*fs[0], fp[1]+lp[1]*fs[1]), (fs[0]*ls[0], fs[1]*ls[1])

spawn=(-1.7,-30.44)
# player collider: offset (0,0.15) size (0.4,0.25) relative to player transform
px,py=spawn
pmin=(px-0.2, py+0.025)
pmax=(px+0.2, py+0.275)
print('player box',pmin,pmax)

def overlaps(a_min,a_max,b_min,b_max):
    return a_min[0] < b_max[0] and a_max[0] > b_min[0] and a_min[1] < b_max[1] and a_max[1] > b_min[1]

for go,comps in go_comp.items():
    name=go_name.get(go,'?')
    wp,ws=world(go)
    for t,iid,b in comps:
        if t not in (61,60,58,70): continue
        trig=re.search(r'm_IsTrigger: (\d)', b)
        if trig and trig.group(1)=='1': continue
        offm=re.search(r'm_Offset: \{x: ([^,]+), y: ([^}]+)\}', b)
        szm=re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)\}', b)
        if not offm or not szm: continue
        off=(float(offm.group(1)),float(offm.group(2)))
        sz=(float(szm.group(1)),float(szm.group(2)))
        # world box
        cx=wp[0]+off[0]*ws[0]; cy=wp[1]+off[1]*ws[1]
        hx=sz[0]*abs(ws[0])/2; hy=sz[1]*abs(ws[1])/2
        bmin=(cx-hx,cy-hy); bmax=(cx+hx,cy+hy)
        # report if close
        if bmax[0] < spawn[0]-5 or bmin[0] > spawn[0]+5: continue
        if bmax[1] < spawn[1]-5 or bmin[1] > spawn[1]+5: continue
        ov=overlaps(pmin,pmax,bmin,bmax)
        print(('OVERLAP' if ov else 'near'), t,name,'go',go,'wpos',wp,'box',bmin,bmax)

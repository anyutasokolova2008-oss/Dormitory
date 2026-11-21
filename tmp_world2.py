# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
go_name={}
for iid,(t,b) in blocks.items():
    if t==1:
        m=re.search(r'm_Name: (.*)', b)
        go_name[iid]=m.group(1).strip().strip('"') if m else '?'
trans={}
for iid,(t,b) in blocks.items():
    if t==4:
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        pm=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        fm=re.search(r'm_Father: \{fileID: (-?\d+)\}', b)
        sm=re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        if gm:
            trans[gm.group(1)]={'tid':iid,'pos':(float(pm.group(1)),float(pm.group(2))) if pm else (0,0),'father':fm.group(1) if fm else '0','scale':(float(sm.group(1)),float(sm.group(2))) if sm else (1,1)}
from functools import lru_cache
@lru_cache(None)
def world(go):
    if go not in trans: return (0,0),(1,1)
    t=trans[go]; f=t['father']
    fgo=None
    for g,td in trans.items():
        if td['tid']==f: fgo=g; break
    if fgo is None: return t['pos'], t['scale']
    fp,fs=world(fgo); lp=t['pos']; ls=t['scale']
    return (fp[0]+lp[0]*fs[0], fp[1]+lp[1]*fs[1]), (fs[0]*ls[0], fs[1]*ls[1])
spawn=(-1.7,-30.44)
pmin=(-1.9,-30.415); pmax=(-1.5,-30.165)
def overlaps(a_min,a_max,b_min,b_max):
    return a_min[0] < b_max[0] and a_max[0] > b_min[0] and a_min[1] < b_max[1] and a_max[1] > b_min[1]
# all collider types
for go, in_comps in [(g,[]) for g in go_name]:
    pass
go_comp={}
for iid,(t,b) in blocks.items():
    gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
    if gm: go_comp.setdefault(gm.group(1),[]).append((t,iid,b))
for go,comps in go_comp.items():
    name=go_name.get(go,'?')
    wp,ws=world(go)
    for t,iid,b in comps:
        if t not in (61,66,19719996,60,58,70): continue
        trig=re.search(r'm_IsTrigger: (\d)', b)
        if trig and trig.group(1)=='1': continue
        if t==61:
            offm=re.search(r'm_Offset: \{x: ([^,]+), y: ([^}]+)\}', b)
            szm=re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)\}', b)
            if not offm or not szm: continue
            off=(float(offm.group(1)),float(offm.group(2))); sz=(float(szm.group(1)),float(szm.group(2)))
            cx=wp[0]+off[0]*ws[0]; cy=wp[1]+off[1]*ws[1]
            hx=sz[0]*abs(ws[0])/2; hy=sz[1]*abs(ws[1])/2
            bmin=(cx-hx,cy-hy); bmax=(cx+hx,cy+hy)
            if bmax[0] < spawn[0]-5 or bmin[0] > spawn[0]+5 or bmax[1] < spawn[1]-5 or bmin[1] > spawn[1]+5: continue
            ov=overlaps(pmin,pmax,bmin,bmax)
            print(('OVERLAP' if ov else 'near'), 'Box', name, 'go',go,'wpos',wp,'box',bmin,bmax)
        elif t in (19719996,66):
            # just report presence near spawn by transform
            if abs(wp[0]-spawn[0])<30 and abs(wp[1]-spawn[1])<30:
                print('MAPCOL',t,name,'go',go,'wpos',wp)

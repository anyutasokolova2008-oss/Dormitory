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
for go,name in go_name.items():
    nl=name.lower()
    if any(x in nl for x in ['vaxta','vospet','medpunkt','sklad','lectnica','floor1','first','1fl','вахт','воспит','медпункт','склад','лестниц']):
        wp,ws=world(go)
        print(name,'go',go,'world',wp)

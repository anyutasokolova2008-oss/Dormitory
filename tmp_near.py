# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# find transforms near -1.7,-30.44
for iid,(t,b) in blocks.items():
    if t==4:
        pm=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', b)
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        if not pm or not gm: continue
        x=float(pm.group(1)); y=float(pm.group(2))
        if -6 < x < 4 and -35 < y < -26:
            go=gm.group(1)
            gon='?'
            if go in blocks:
                nm=re.search(r'm_Name: (.*)', blocks[go][1])
                if nm: gon=nm.group(1).strip()
            # has collider?
            cols=[]
            for iid2,(t2,b2) in blocks.items():
                if re.search(r'm_GameObject: \{fileID: '+re.escape(go)+r'\}', b2) and t2 in (61,60,70,58,66,64):
                    cols.append(str(t2))
            print(f'{gon} pos={x},{y} cols={cols} go={go}')

# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# list all transforms whose father is 684963121
for iid,(t,b) in blocks.items():
    if t==4 and re.search(r'm_Father: \{fileID: 684963121\}', b):
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        go=gm.group(1) if gm else '?'
        name='?'
        if go in blocks:
            nm=re.search(r'm_Name: (.*)', blocks[go][1])
            if nm: name=nm.group(1).strip()
        print(go,name,iid)

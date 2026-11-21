# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# Spawn_To_Floor is go 1432859533, its transform is 1432859534, father is {fileID: 0}? we saw Fainter? Wait look up
for iid,(t,b) in blocks.items():
    if t==4 and re.search(r'm_GameObject: \{fileID: 1432859533\}', b):
        print(b)
# find parent chain
def get_parent(tid):
    if tid not in blocks: return None
    t,b=blocks[tid]
    if t!=4: return None
    m=re.search(r'm_Father: \{fileID: (-?\d+)\}', b)
    return m.group(1) if m else None
cur='1432859534'
for _ in range(10):
    p=get_parent(cur)
    print('parent',p)
    if not p or p=='0': break
    cur=p
    # get go name
    t,b=blocks[cur]
    gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
    if gm and gm.group(1) in blocks:
        nm=re.search(r'm_Name: (.*)', blocks[gm.group(1)][1])
        print(' name', nm.group(1).strip() if nm else '?')

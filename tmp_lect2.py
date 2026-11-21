# -*- coding: utf-8 -*-
import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# any transform with father 684963120 transform id?
# get Lectnica transform id
for iid,(t,b) in blocks.items():
    if t==4 and 'm_GameObject: {fileID: 684963120}' in b:
        print('lect tid',iid)
        print(b[:400])

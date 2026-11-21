import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3):
    blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
for iid,(t,b) in blocks.items():
    if t==114 and 'DoorTrigger' in b:
        dm=re.search(r'destinationName: (.*)', b)
        tm=re.search(r'target: \{fileID: (-?\d+)\}', b)
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        dest=dm.group(1).strip().strip('"') if dm else ''
        tgt=tm.group(1) if tm else ''
        go=gm.group(1) if gm else ''
        gon='?'
        if go in blocks:
            nm=re.search(r'm_Name: (.*)', blocks[go][1])
            gon=nm.group(1).strip().strip('"') if nm else '?'
        pos='?'
        if tgt in blocks:
            tt,tb=blocks[tgt]
            if tt==4:
                pm=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', tb)
                if pm: pos=f'{pm.group(1)},{pm.group(2)}'
        print(gon,'=>',dest,'target=',tgt,'pos=',pos)

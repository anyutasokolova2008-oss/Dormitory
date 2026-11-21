import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# find GO named Komendant / Player
for iid,(t,b) in blocks.items():
    if t==1:
        m=re.search(r'm_Name: (.*)', b)
        name=m.group(1).strip().strip('"')
        if 'komendant' in name.lower() or name.lower()=='player' or 'комендант' in name.lower():
            print('OBJ',iid,name)
            # list components
            for iid2,(t2,b2) in blocks.items():
                if re.search(r'm_GameObject: \{fileID: '+re.escape(iid)+r'\}', b2):
                    print('  comp',t2,iid2)
                    if t2==114:
                        for line in b2.splitlines():
                            if 'm_EditorClassIdentifier' in line: print('   ',line.strip())
# also check any PlayerController on other GO
for iid,(t,b) in blocks.items():
    if t==114 and 'PlayerController' in b:
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
        go=gm.group(1) if gm else '?'
        gon='?'
        if go in blocks:
            nm=re.search(r'm_Name: (.*)', blocks[go][1]); gon=nm.group(1).strip() if nm else '?'
        print('PC ON',go,gon)

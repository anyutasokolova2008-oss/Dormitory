import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3):
    blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
# show transform for Door_To_Floor1 target and also Door_To_Floor1 door object
for name in ['Door_To_Floor1','SpawnFloor1','Floor1','Spawn Floor1']:
    pass
# find Door_To_Floor1 GO and its collider/transform
for iid,(t,b) in blocks.items():
    if t==1 and 'Door_To_Floor1' in b:
        print('GO',iid)
        print(b)
for iid,(t,b) in blocks.items():
    if re.search(r'm_GameObject: \{fileID: 1775629812\}', b):
        print('---',t,iid)
        print('\n'.join(b.splitlines()[:120]))
# target transform 1432859534
if '1432859534' in blocks:
    print('--- target', blocks['1432859534'][0])
    print(blocks['1432859534'][1][:800])

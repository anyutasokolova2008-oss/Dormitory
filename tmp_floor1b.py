import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3):
    blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
for iid,(t,b) in blocks.items():
    if re.search(r'm_GameObject: \{fileID: 855490126\}', b):
        print('---',t,iid)
        print('\n'.join(b.splitlines()[:140]))
        print('...')
# also find which GO has transform 1432859533
if '1432859533' in blocks:
    print('--- targetGO')
    print(blocks['1432859533'][1])

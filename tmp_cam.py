import re
path='Assets/Scenes/PlayTest.unity'
s=open(path,encoding='utf-8',errors='replace').read()
docs=re.split(r'^--- !u!([0-9]+) &(-?[0-9]+)', s, flags=re.M)
blocks={}
for i in range(1,len(docs),3): blocks[docs[i+1]]=(int(docs[i]), docs[i+2])
for iid in ['519420030','519420033']:
    if iid in blocks:
        print('---',blocks[iid][0],iid)
        print('\n'.join(blocks[iid][1].splitlines()[:120]))
        print('...')

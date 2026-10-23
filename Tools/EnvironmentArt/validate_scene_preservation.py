"""Compare M7 saved scene against a Git baseline without opening or changing Unity assets."""
import hashlib
import re
import subprocess
import sys
from pathlib import Path

SCENE = 'Assets/Scenes/Demo/SalvageIntake.unity'


def blocks(text):
    return {(int(kind), int(identity)): body for kind,identity,body in
            re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)', text, re.M|re.S)}


def collision_maps(objects):
    names = {identity: re.search(r'^  m_Name: (.*)$',body,re.M).group(1)
             for (kind,identity),body in objects.items() if kind == 1}
    for (kind,identity),body in objects.items():
        if kind != 1839735485: continue
        owner = int(re.search(r'm_GameObject: \{fileID: (\d+)\}',body).group(1))
        name = names[owner]
        if name not in ('Ground Collision - Hidden','Maintenance Crouch Collision'): continue
        cells = re.findall(r'first: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}', body.split('  m_TileAssetArray:')[0])
        ordered = sorted(tuple(map(int,c)) for c in cells)
        signature = ''.join(f'{x},{y},{z};' for x,y,z in ordered)
        print(name, len(cells), hashlib.sha256(signature.encode()).hexdigest())
        yield (kind,identity),body


if __name__ == '__main__':
    baseline = subprocess.run(['git','show',(sys.argv[1] if len(sys.argv)>1 else 'ca14060')+':'+SCENE],
                              check=True,capture_output=True,text=True).stdout
    original,current=blocks(baseline),blocks(Path(SCENE).read_text())
    # All existing MonoBehaviours and physics components must be byte-identical. This
    # includes player tuning, encounters, checkpoints, menu integration and breaching.
    types = (50,61,66,114,58,60,70,19719996)
    protected = {k:v for k,v in original.items() if k[0] in types}
    assert set(protected) == {k for k in current if k[0] in types}, 'Gameplay/physics components added or removed'
    protected.update(dict(collision_maps(original)))
    assert all(current.get(k)==v for k,v in protected.items()), 'Protected gameplay/collision data changed'
    list(collision_maps(current))
    print(f'PASS: {len(protected)} protected component/map blocks unchanged from baseline')

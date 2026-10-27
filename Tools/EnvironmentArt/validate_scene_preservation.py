"""Compare M7 saved scene against a Git baseline without opening or changing Unity assets."""
import hashlib
import argparse
import re
import subprocess
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
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('baseline', nargs='?', default='ca14060')
    parser.add_argument('--allow-enemy-health-bars', action='store_true')
    parser.add_argument('--allow-industrial-backgrounds', action='store_true')
    args = parser.parse_args()
    baseline = subprocess.run(['git','show',args.baseline+':'+SCENE],
                              check=True,capture_output=True,text=True).stdout
    original,current=blocks(baseline),blocks(Path(SCENE).read_text())
    # All existing MonoBehaviours and physics components must be byte-identical. This
    # includes player tuning, encounters, checkpoints, menu integration and breaching.
    types = (50,61,66,114,58,60,70,19719996)
    protected = {k:v for k,v in original.items() if k[0] in types}
    added = {k for k in current if k[0] in types} - set(protected)
    allowed = set()
    if args.allow_enemy_health_bars:
        guid = re.search(r'^guid: (\w+)', Path('Assets/Scripts/Presentation/EnemyHealthBar2D.cs.meta').read_text(), re.M).group(1)
        bars = {k for k in added if k[0] == 114 and f'guid: {guid},' in current[k]}
        assert len(bars) == 14, 'Exactly fourteen new enemy bar presenters required'
        allowed.update(bars)
    if args.allow_industrial_backgrounds:
        guid = re.search(r'^guid: (\w+)', Path('Assets/Scripts/Presentation/PixelSnappedBackgroundPlane2D.cs.meta').read_text(), re.M).group(1)
        planes = {k for k in added if k[0] == 114 and f'guid: {guid},' in current[k]}
        assert len(planes) == 2, 'Exactly two new industrial background planes required'
        allowed.update(planes)
    assert added == allowed, 'Unauthorized gameplay/physics components added'
    assert set(protected) <= set(current), 'Gameplay/physics components removed'
    protected.update(dict(collision_maps(original)))
    assert all(current.get(k)==v for k,v in protected.items()), 'Protected gameplay/collision data changed'
    list(collision_maps(current))
    print(f'PASS: {len(protected)} protected component/map blocks unchanged from baseline')

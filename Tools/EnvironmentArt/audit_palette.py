"""Read-only exact Canonical 28 / binary-alpha audit of production environment PNGs.

Run from any directory: python Tools/EnvironmentArt/audit_palette.py
Optional --report Logs/M71/palette-audit.json writes pixel counts and source hashes.
Source/reference images under Assets/Art/Source are deliberately outside production.
Requires Pillow, as does the existing environment generation pipeline.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

from PIL import Image


def audit(root):
    palette = {tuple(bytes.fromhex(value)) for value in
               re.findall(r'`#([0-9A-F]{6})`', (root / 'docs/PALETTE.md').read_text())[:28]}
    assert len(palette) == 28, 'Canonical palette could not be read exactly'
    results = []
    for path in sorted((root / 'Assets/Art/Environment').rglob('*.png')):
        with Image.open(path) as source:
            rgba = source.convert('RGBA')
            pixels = Counter(rgba.get_flattened_data() if hasattr(rgba, 'get_flattened_data') else rgba.getdata())
            result = {
                'path': path.relative_to(root).as_posix(),
                'size': list(source.size),
                'opaque_pixels': sum(n for p, n in pixels.items() if p[3] == 255),
                'transparent_pixels': sum(n for p, n in pixels.items() if p[3] == 0),
                'partial_alpha_pixels': sum(n for p, n in pixels.items() if p[3] not in (0, 255)),
                'opaque_colors': len({p[:3] for p in pixels if p[3] == 255}),
                'off_palette_opaque_pixels': sum(n for p, n in pixels.items() if p[3] == 255 and p[:3] not in palette),
                'off_palette_rgb': sorted({p[:3] for p in pixels if p[3] == 255 and p[:3] not in palette}),
                'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
            }
        results.append(result)
    assert results, 'No production environment PNGs found'
    return results


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    results = audit(Path(__file__).resolve().parents[2])
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(results, indent=2) + '\n')
    print('| M7 production asset | Dimensions | Opaque pixels | RGB colors | Off-palette | Partial alpha |')
    print('| --- | --- | ---: | ---: | ---: | ---: |')
    for row in results:
        if '/Hardening/' in row['path']:
            print(f"| `{Path(row['path']).name}` | {'x'.join(map(str,row['size']))} | {row['opaque_pixels']} | "
                  f"{row['opaque_colors']} | {row['off_palette_opaque_pixels']} | {row['partial_alpha_pixels']} |")
    off = sum(r['off_palette_opaque_pixels'] for r in results)
    partial = sum(r['partial_alpha_pixels'] for r in results)
    print(f'\nAudited {len(results)} production PNGs: {off} off-palette opaque pixels; {partial} partial-alpha pixels.')
    raise SystemExit(1 if off or partial else 0)

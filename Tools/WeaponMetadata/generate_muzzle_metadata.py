#!/usr/bin/env python3
"""Generate deterministic per-frame muzzle metadata from production weapon PNGs."""

from __future__ import annotations

import argparse
import json
import math
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image


SCHEMA_VERSION = 1
GENERATOR_VERSION = 2
CELL_WIDTH = 80
CELL_HEIGHT = 96
PIVOT_X = 24
PIVOT_Y = 8
BLUE_MARKER = (0, 0, 255, 255)
RED_MARKER = (255, 0, 0, 255)
TRANSPARENT = (0, 0, 0, 0)
OUT_OF_BOUNDS = object()
SIGNATURE_SIZES = (5, 7, 9, 11, 13)
DIRECTION_SUFFIXES = (
    "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10",
    "0",
    "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90",
)
DIRECTION_ANGLES = (
    90, 80, 70, 60, 50, 40, 30, 20, 10, 0,
    -10, -20, -30, -40, -50, -60, -70, -80, -90,
)


@dataclass(frozen=True)
class StateSpec:
    name: str
    directory: str
    filename_token: str
    frame_count: int

    @property
    def sheet_width(self) -> int:
        return CELL_WIDTH * self.frame_count


STATE_SPECS = (
    StateSpec("Idle", "Idle", "idle", 2),
    StateSpec("Run", "Run", "run", 6),
    StateSpec("Backpedal", "Backpedal", "backpedal", 4),
    StateSpec("Crouch", "Crouch", "crouch", 6),
    StateSpec("Fall", "Fall", "fall", 1),
)


@dataclass(frozen=True)
class WeaponSpec:
    weapon_id: str
    display_name: str
    filename_token: str
    reference_relative_path: Path
    output_relative_path: Path
    production_relative_root: Path
    unsupported_directions: tuple[tuple[str, tuple[str, ...]], ...] = ()

    def is_supported(self, state_name: str, suffix: str) -> bool:
        for unsupported_state, suffixes in self.unsupported_directions:
            if unsupported_state == state_name and suffix in suffixes:
                return False
        return True


LONGWATCH_SPEC = WeaponSpec(
    weapon_id="longwatch_dmr",
    display_name="Longwatch DMR",
    filename_token="longwatch_dmr",
    reference_relative_path=Path(
        "ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png"
    ),
    output_relative_path=Path(
        "ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json"
    ),
    production_relative_root=Path(
        "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim"
    ),
    unsupported_directions=(("Crouch", ("m70", "m80", "m90")),),
)

LATCH9_SPEC = WeaponSpec(
    weapon_id="latch_9",
    display_name="Latch-9",
    filename_token="latch_9",
    reference_relative_path=Path(
        "ArtSource/Metadata/Weapons/latch_9/Muzzle/latch_9_muzzle_reference.png"
    ),
    output_relative_path=Path(
        "ArtSource/Metadata/Weapons/latch_9/Generated/latch_9_muzzle_metadata.json"
    ),
    production_relative_root=Path(
        "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim"
    ),
)

WEAPON_SPECS = (LONGWATCH_SPEC, LATCH9_SPEC)
WEAPON_SPECS_BY_ID = {spec.weapon_id: spec for spec in WEAPON_SPECS}

# Backward-compatible Longwatch aliases retained for existing callers/tests.
WEAPON_ID = LONGWATCH_SPEC.weapon_id
UNSUPPORTED_CROUCH_DIRECTIONS = frozenset(("m70", "m80", "m90"))
REFERENCE_RELATIVE_PATH = LONGWATCH_SPEC.reference_relative_path
OUTPUT_RELATIVE_PATH = LONGWATCH_SPEC.output_relative_path
PRODUCTION_RELATIVE_ROOT = LONGWATCH_SPEC.production_relative_root

Pixel = tuple[int, int, int, int]
Point = tuple[int, int]


class MetadataError(RuntimeError):
    """An actionable validation or exact-matching failure."""


@dataclass(frozen=True)
class NormalizedImage:
    width: int
    height: int
    pixels: tuple[Pixel, ...]

    def pixel(self, x: int, y: int) -> Pixel | object:
        if x < 0 or y < 0 or x >= self.width or y >= self.height:
            return OUT_OF_BOUNDS
        return self.pixels[y * self.width + x]


@dataclass(frozen=True)
class Signature:
    size: int
    pixels: tuple[Pixel | object, ...]
    comparison_order: tuple[int, ...]


@dataclass(frozen=True)
class GenerationResult:
    metadata: dict[str, object]
    serialized: str
    signature_sizes: dict[str, int]
    marker_angles: dict[str, float]


def normalize_pixel(pixel: Sequence[int]) -> Pixel:
    """Normalize hidden RGB so every fully transparent pixel compares equally."""
    rgba = tuple(int(channel) for channel in pixel)
    if len(rgba) != 4:
        raise ValueError(f"Expected RGBA pixel, got {rgba!r}")
    if rgba[3] == 0:
        return TRANSPARENT
    return rgba  # type: ignore[return-value]


def normalized_image_from_pixels(
    width: int, height: int, pixels: Iterable[Sequence[int]]
) -> NormalizedImage:
    normalized = tuple(normalize_pixel(pixel) for pixel in pixels)
    if len(normalized) != width * height:
        raise ValueError(
            f"Expected {width * height} pixels for {width}x{height}, got {len(normalized)}"
        )
    return NormalizedImage(width, height, normalized)


def rgba_pixels(image: Image.Image) -> tuple[tuple[int, int, int, int], ...]:
    """Read flattened pixels without depending on Pillow's deprecated getdata API."""
    get_flattened_data = getattr(image, "get_flattened_data", None)
    if get_flattened_data is not None:
        return tuple(get_flattened_data())
    return tuple(image.getdata())


def load_normalized_image(path: Path) -> NormalizedImage:
    with Image.open(path) as source:
        rgba = source.convert("RGBA")
        return normalized_image_from_pixels(rgba.width, rgba.height, rgba_pixels(rgba))


def extract_frame(sheet: NormalizedImage, frame: int) -> NormalizedImage:
    if sheet.height != CELL_HEIGHT or sheet.width % CELL_WIDTH != 0:
        raise ValueError(f"Sheet is not composed of {CELL_WIDTH}x{CELL_HEIGHT} cells")
    frame_count = sheet.width // CELL_WIDTH
    if frame < 0 or frame >= frame_count:
        raise IndexError(f"Frame {frame} is outside sheet frame count {frame_count}")
    start_x = frame * CELL_WIDTH
    pixels = tuple(
        sheet.pixels[y * sheet.width + start_x + x]
        for y in range(CELL_HEIGHT)
        for x in range(CELL_WIDTH)
    )
    return NormalizedImage(CELL_WIDTH, CELL_HEIGHT, pixels)


def extract_signature(image: NormalizedImage, center: Point, size: int) -> Signature:
    if size < 1 or size % 2 == 0:
        raise ValueError(f"Signature size must be a positive odd number, got {size}")
    radius = size // 2
    values = tuple(
        image.pixel(center[0] + dx, center[1] + dy)
        for dy in range(-radius, radius + 1)
        for dx in range(-radius, radius + 1)
    )
    order = tuple(sorted(
        range(len(values)),
        key=lambda index: 0 if values[index] is OUT_OF_BOUNDS
        else (1 if values[index] != TRANSPARENT else 2),
    ))
    return Signature(size, values, order)


def comparison_pixels_equal(left: Pixel | object, right: Pixel | object) -> bool:
    """Compare exact pixels with transparent padding outside an isolated sprite cell."""
    if left is OUT_OF_BOUNDS:
        return right is OUT_OF_BOUNDS or right == TRANSPARENT
    if right is OUT_OF_BOUNDS:
        return left == TRANSPARENT
    return left == right


def find_signature_matches(image: NormalizedImage, signature: Signature) -> list[Point]:
    radius = signature.size // 2
    matches: list[Point] = []
    for center_y in range(image.height):
        for center_x in range(image.width):
            matched = True
            for index in signature.comparison_order:
                dx = index % signature.size - radius
                dy = index // signature.size - radius
                if not comparison_pixels_equal(
                    image.pixel(center_x + dx, center_y + dy), signature.pixels[index]
                ):
                    matched = False
                    break
            if matched:
                matches.append((center_x, center_y))
    return matches


def pixel_to_pivot_offset(point: Point) -> list[float]:
    image_x, image_y = point
    center_x = image_x + 0.5
    center_y_from_bottom = CELL_HEIGHT - (image_y + 0.5)
    return [center_x - PIVOT_X, center_y_from_bottom - PIVOT_Y]


def serialize_metadata(metadata: dict[str, object]) -> str:
    return json.dumps(metadata, indent=2, ensure_ascii=False) + "\n"


def assign_markers_to_directions(
    red: Point, blue_points: Sequence[Point]
) -> tuple[dict[str, Point], dict[str, float]]:
    if len(set(blue_points)) != len(blue_points):
        raise MetadataError("Reference contains duplicate blue marker coordinates")

    polar: list[tuple[float, float, Point]] = []
    for point in blue_points:
        dx = point[0] - red[0]
        dy = red[1] - point[1]
        radius = math.hypot(dx, dy)
        if radius < 5.0 or radius > math.hypot(CELL_WIDTH, CELL_HEIGHT):
            raise MetadataError(
                f"Structurally unreasonable blue marker {point}: red-to-blue radius {radius:.3f}"
            )
        polar.append((math.degrees(math.atan2(dy, dx)), radius, point))

    polar.sort(key=lambda entry: entry[0], reverse=True)
    if len(polar) != len(DIRECTION_SUFFIXES):
        raise MetadataError(
            f"Reference blue marker count is {len(polar)}; expected {len(DIRECTION_SUFFIXES)}"
        )
    for previous, current in zip(polar, polar[1:]):
        if previous[0] - current[0] <= 1e-6:
            raise MetadataError(
                "Reference radial ordering is degenerate/non-unique: "
                f"angles {previous[0]:.6f} and {current[0]:.6f}"
            )

    marker_by_suffix: dict[str, Point] = {}
    angle_by_suffix: dict[str, float] = {}
    for suffix, expected_angle, entry in zip(DIRECTION_SUFFIXES, DIRECTION_ANGLES, polar):
        measured_angle, _radius, point = entry
        if abs(measured_angle - expected_angle) > 15.0:
            raise MetadataError(
                f"Reference marker {point} assigned to {suffix} has unreasonable angle "
                f"{measured_angle:.3f} degrees; expected near {expected_angle}"
            )
        marker_by_suffix[suffix] = point
        angle_by_suffix[suffix] = measured_angle
    return marker_by_suffix, angle_by_suffix


def read_reference(
    path: Path,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> tuple[dict[str, Point], dict[str, float]]:
    if not path.is_file():
        raise MetadataError(f"Missing {weapon.display_name} muzzle reference: {path}")
    with Image.open(path) as source:
        rgba = source.convert("RGBA")
        if rgba.size != (CELL_WIDTH, CELL_HEIGHT):
            raise MetadataError(
                f"Reference dimensions are {rgba.width}x{rgba.height}; "
                f"expected {CELL_WIDTH}x{CELL_HEIGHT}"
            )
        pixels = rgba_pixels(rgba)
        red_indices = [index for index, pixel in enumerate(pixels) if pixel == RED_MARKER]
        blue_indices = [index for index, pixel in enumerate(pixels) if pixel == BLUE_MARKER]

    if len(red_indices) != 1:
        raise MetadataError(f"Reference red marker count is {len(red_indices)}; expected 1")
    if len(blue_indices) != 19:
        raise MetadataError(f"Reference blue marker count is {len(blue_indices)}; expected 19")
    red = (red_indices[0] % CELL_WIDTH, red_indices[0] // CELL_WIDTH)
    blue = [(index % CELL_WIDTH, index // CELL_WIDTH) for index in blue_indices]
    return assign_markers_to_directions(red, blue)


def canonical_filename(
    spec: StateSpec,
    suffix: str,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> str:
    return f"player_salvager_{weapon.filename_token}_{spec.filename_token}_aim_{suffix}.png"


def load_production_sheets(
    repo_root: Path,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> dict[tuple[str, str], NormalizedImage]:
    production_root = repo_root / weapon.production_relative_root
    sheets: dict[tuple[str, str], NormalizedImage] = {}
    for spec in STATE_SPECS:
        directory = production_root / spec.directory
        if not directory.is_dir():
            raise MetadataError(f"Missing production state directory: {directory}")
        actual_paths = list(directory.glob("*.png"))
        actual_names = [path.name for path in actual_paths]
        casefolded = [name.casefold() for name in actual_names]
        if len(casefolded) != len(set(casefolded)):
            raise MetadataError(f"Duplicate direction PNG filenames in {directory}")
        expected_names = {
            canonical_filename(spec, suffix, weapon) for suffix in DIRECTION_SUFFIXES
        }
        actual_name_set = set(actual_names)
        missing = sorted(expected_names - actual_name_set)
        unexpected = sorted(actual_name_set - expected_names)
        if missing or unexpected:
            raise MetadataError(
                f"Invalid {weapon.display_name} {spec.name} direction sheets in {directory}; "
                f"missing={missing or 'none'}, unexpected={unexpected or 'none'}"
            )

        for suffix in DIRECTION_SUFFIXES:
            path = directory / canonical_filename(spec, suffix, weapon)
            with Image.open(path) as source:
                if source.size != (spec.sheet_width, CELL_HEIGHT):
                    raise MetadataError(
                        f"Unexpected dimensions for {path}: {source.width}x{source.height}; "
                        f"expected {spec.sheet_width}x{CELL_HEIGHT} "
                        f"({spec.frame_count} horizontal frames)"
                    )
                rgba = source.convert("RGBA")
                raw_pixels = rgba_pixels(rgba)
            marker_colors = sum(
                pixel == BLUE_MARKER or pixel == RED_MARKER for pixel in raw_pixels
            )
            if marker_colors:
                raise MetadataError(
                    f"Production artwork contains {marker_colors} opaque authoring marker color(s): {path}"
                )
            sheets[(spec.name, suffix)] = normalized_image_from_pixels(
                spec.sheet_width, CELL_HEIGHT, raw_pixels
            )
    return sheets


def required_frames_for_direction(
    sheets: dict[tuple[str, str], NormalizedImage],
    suffix: str,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> list[tuple[str, int, NormalizedImage]]:
    frames: list[tuple[str, int, NormalizedImage]] = []
    for spec in STATE_SPECS:
        if not weapon.is_supported(spec.name, suffix):
            continue
        sheet = sheets[(spec.name, suffix)]
        for frame in range(spec.frame_count):
            frames.append((spec.name, frame, extract_frame(sheet, frame)))
    return frames


def select_adaptive_signature(
    source: NormalizedImage,
    seed: Point,
    required: Sequence[tuple[str, int, NormalizedImage]],
    direction: str,
    signature_sizes: Sequence[int] = SIGNATURE_SIZES,
    weapon_id: str = WEAPON_ID,
) -> tuple[int, list[tuple[str, int, Point]]]:
    attempted: list[tuple[int, list[tuple[str, int, int]]]] = []
    for size in signature_sizes:
        signature = extract_signature(source, seed, size)
        resolved: list[tuple[str, int, Point]] = []
        counts: list[tuple[str, int, int]] = []
        all_unique = True
        for state_name, frame, image in required:
            matches = find_signature_matches(image, signature)
            counts.append((state_name, frame, len(matches)))
            if len(matches) == 1:
                resolved.append((state_name, frame, matches[0]))
            else:
                all_unique = False
        attempted.append((size, counts))
        if all_unique:
            return size, resolved

    lines = [
        f"Exact muzzle signature failed for weapon={weapon_id} direction={direction} seed={seed}."
    ]
    for size, counts in attempted:
        failures = [
            f"{state}/frame {frame}: candidates={count}"
            for state, frame, count in counts if count != 1
        ]
        lines.append(f"  signature {size}x{size}: " + "; ".join(failures))
    raise MetadataError("\n".join(lines))


def resolve_direction(
    suffix: str,
    seed: Point,
    sheets: dict[tuple[str, str], NormalizedImage],
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> tuple[int, dict[str, list[Point]]]:
    source = extract_frame(sheets[("Idle", suffix)], 0)
    # A marker may sit in transparent space just beyond the barrel or overwrite the
    # actual muzzle pixel. It is only a coordinate seed; the clean signature always
    # comes from isolated production art, so both authoring conventions are valid.
    required = required_frames_for_direction(sheets, suffix, weapon)
    size, frame_matches = select_adaptive_signature(
        source,
        seed,
        required,
        suffix,
        weapon_id=weapon.weapon_id,
    )
    resolved: dict[str, list[Point]] = {
        spec.name: [] for spec in STATE_SPECS if weapon.is_supported(spec.name, suffix)
    }
    for state_name, _frame, point in frame_matches:
        resolved[state_name].append(point)
    return size, resolved


def build_metadata(
    matches_by_suffix: dict[str, dict[str, list[Point]]],
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> dict[str, object]:
    states: list[dict[str, object]] = []
    for spec in STATE_SPECS:
        directions: list[dict[str, object]] = []
        for suffix, angle in zip(DIRECTION_SUFFIXES, DIRECTION_ANGLES):
            supported = weapon.is_supported(spec.name, suffix)
            frames = []
            if supported:
                points = matches_by_suffix[suffix][spec.name]
                if len(points) != spec.frame_count:
                    raise MetadataError(
                        f"Internal error: {weapon.weapon_id}/{spec.name}/{suffix} resolved "
                        f"{len(points)} frames; expected {spec.frame_count}"
                    )
                frames = [
                    {"frame": frame, "muzzleOffsetPixels": pixel_to_pivot_offset(point)}
                    for frame, point in enumerate(points)
                ]
            directions.append({
                "suffix": suffix,
                "angleDegrees": angle,
                "supported": supported,
                "frames": frames,
            })
        states.append({
            "name": spec.name,
            "frameCount": spec.frame_count,
            "directions": directions,
        })

    return {
        "schemaVersion": SCHEMA_VERSION,
        "generatorVersion": GENERATOR_VERSION,
        "weaponId": weapon.weapon_id,
        "cellSizePixels": [CELL_WIDTH, CELL_HEIGHT],
        "pivotPixels": [PIVOT_X, PIVOT_Y],
        "reference": {
            "state": "Idle",
            "frame": 0,
            "path": weapon.reference_relative_path.as_posix(),
        },
        "states": states,
    }


def generate(
    repo_root: Path,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> GenerationResult:
    marker_by_suffix, marker_angles = read_reference(
        repo_root / weapon.reference_relative_path,
        weapon,
    )
    sheets = load_production_sheets(repo_root, weapon)
    signature_sizes: dict[str, int] = {}
    matches_by_suffix: dict[str, dict[str, list[Point]]] = {}
    for suffix in DIRECTION_SUFFIXES:
        size, matches = resolve_direction(
            suffix,
            marker_by_suffix[suffix],
            sheets,
            weapon,
        )
        signature_sizes[suffix] = size
        matches_by_suffix[suffix] = matches
    metadata = build_metadata(matches_by_suffix, weapon)
    return GenerationResult(
        metadata=metadata,
        serialized=serialize_metadata(metadata),
        signature_sizes=signature_sizes,
        marker_angles=marker_angles,
    )


def print_summary(
    result: GenerationResult,
    output_path: Path,
    check: bool,
    weapon: WeaponSpec = LONGWATCH_SPEC,
) -> None:
    print(f"{weapon.display_name} muzzle metadata")
    print("reference: OK (19 blue, 1 red)")
    for suffix in DIRECTION_SUFFIXES:
        state_summary = []
        for state in STATE_SPECS:
            state_summary.append(
                f"{state.name} {state.frame_count}/{state.frame_count}"
                if weapon.is_supported(state.name, suffix)
                else f"{state.name} unsupported"
            )
        print(
            f"{suffix:>3}  signature {result.signature_sizes[suffix]}x{result.signature_sizes[suffix]}  "
            + " ".join(state_summary)
        )
    verb = "checked" if check else "generated"
    print(f"{verb}: {output_path}")


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate deterministic per-frame muzzle metadata for configured weapons."
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail if any selected checked-in JSON differs from fresh in-memory output.",
    )
    parser.add_argument(
        "--weapon",
        choices=("all", *WEAPON_SPECS_BY_ID.keys()),
        default="all",
        help="Generate/check all configured weapons (default) or one weapon id.",
    )
    return parser.parse_args(argv)


def selected_weapon_specs(weapon_arg: str) -> tuple[WeaponSpec, ...]:
    if weapon_arg == "all":
        return WEAPON_SPECS
    return (WEAPON_SPECS_BY_ID[weapon_arg],)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)
    repo_root = Path(__file__).resolve().parents[2]
    specs = selected_weapon_specs(args.weapon)
    try:
        # Resolve every selected corpus before touching any generated file. This prevents
        # a failure in one weapon from leaving another weapon's metadata half-updated.
        generated = [(spec, generate(repo_root, spec)) for spec in specs]

        if args.check:
            for spec, result in generated:
                output_path = repo_root / spec.output_relative_path
                if not output_path.is_file():
                    raise MetadataError(f"Generated metadata is missing: {output_path}")
                existing = output_path.read_bytes()
                if existing != result.serialized.encode("utf-8"):
                    raise MetadataError(
                        f"Generated metadata is stale: {output_path}\n"
                        "Run this tool without --check and review the resulting diff."
                    )
        else:
            for spec, result in generated:
                output_path = repo_root / spec.output_relative_path
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_bytes(result.serialized.encode("utf-8"))

        for index, (spec, result) in enumerate(generated):
            if index:
                print()
            print_summary(
                result,
                repo_root / spec.output_relative_path,
                args.check,
                spec,
            )
        return 0
    except (MetadataError, OSError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

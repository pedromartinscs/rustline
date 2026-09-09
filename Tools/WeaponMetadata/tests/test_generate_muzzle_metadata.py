from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOL_ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(TOOL_ROOT))

import generate_muzzle_metadata as muzzle  # noqa: E402


def make_image(
    width: int,
    height: int,
    colored_pixels: dict[tuple[int, int], tuple[int, int, int, int]] | None = None,
) -> muzzle.NormalizedImage:
    pixels = [muzzle.TRANSPARENT] * (width * height)
    for (x, y), color in (colored_pixels or {}).items():
        pixels[y * width + x] = color
    return muzzle.normalized_image_from_pixels(width, height, pixels)


class MuzzleMetadataUnitTests(unittest.TestCase):
    def test_transparent_rgb_is_normalized(self) -> None:
        self.assertEqual(muzzle.TRANSPARENT, muzzle.normalize_pixel((12, 34, 56, 0)))
        self.assertEqual((12, 34, 56, 1), muzzle.normalize_pixel((12, 34, 56, 1)))

    def test_exact_signature_matching(self) -> None:
        red = (200, 10, 20, 255)
        blue = (20, 30, 200, 255)
        source = make_image(15, 15, {(7, 7): red, (8, 7): blue, (7, 8): blue})
        target = make_image(15, 15, {(4, 10): red, (5, 10): blue, (4, 11): blue})
        signature = muzzle.extract_signature(source, (7, 7), 5)
        self.assertEqual([(4, 10)], muzzle.find_signature_matches(target, signature))

    def test_zero_match_is_a_failure(self) -> None:
        source = make_image(15, 15, {(7, 7): (255, 255, 255, 255)})
        target = make_image(15, 15)
        with self.assertRaisesRegex(
            muzzle.MetadataError, r"TestState/frame 3: candidates=0"
        ):
            muzzle.select_adaptive_signature(
                source, (7, 7), (("TestState", 3, target),), "p90", (5,)
            )

    def test_multiple_matches_are_an_ambiguity_failure(self) -> None:
        red = (255, 0, 0, 255)
        source = make_image(15, 15, {(7, 7): red})
        target = make_image(31, 15, {(8, 7): red, (22, 7): red})
        with self.assertRaisesRegex(
            muzzle.MetadataError, r"TestState/frame 1: candidates=2"
        ):
            muzzle.select_adaptive_signature(
                source, (7, 7), (("TestState", 1, target),), "0", (5,)
            )

    def test_adaptive_signature_uses_smallest_unique_size(self) -> None:
        red = (255, 0, 0, 255)
        blue = (0, 0, 255, 255)
        image = make_image(31, 15, {(6, 7): red, (9, 7): blue, (23, 7): red})
        size, matches = muzzle.select_adaptive_signature(
            image, (6, 7), (("TestState", 0, image),), "0", (5, 7, 9)
        )
        self.assertEqual(7, size)
        self.assertEqual([("TestState", 0, (6, 7))], matches)

    def test_boundary_uses_explicit_transparent_padding(self) -> None:
        dark = (13, 23, 44, 255)
        light = (66, 89, 112, 255)
        source = make_image(9, 9, {(4, 3): dark, (4, 4): light})
        target = make_image(9, 9, {(4, 1): dark, (4, 2): light})
        signature = muzzle.extract_signature(source, (4, 2), 5)

        self.assertIs(source.pixel(0, -1), muzzle.OUT_OF_BOUNDS)
        self.assertTrue(
            muzzle.comparison_pixels_equal(muzzle.OUT_OF_BOUNDS, muzzle.TRANSPARENT)
        )
        self.assertFalse(muzzle.comparison_pixels_equal(muzzle.OUT_OF_BOUNDS, dark))
        self.assertEqual([(4, 0)], muzzle.find_signature_matches(target, signature))

    def test_radial_sort_assigns_canonical_direction_order(self) -> None:
        red = (17, 45)
        ordered_points = [
            (18, 3), (26, 2), (34, 6), (40, 8), (45, 13), (51, 17),
            (55, 22), (58, 30), (61, 37), (64, 46), (63, 53), (60, 60),
            (59, 68), (54, 73), (49, 80), (44, 84), (36, 86), (29, 89),
            (21, 93),
        ]
        shuffled = ordered_points[::2] + ordered_points[1::2]
        assigned, angles = muzzle.assign_markers_to_directions(red, shuffled)
        self.assertEqual(ordered_points, [assigned[s] for s in muzzle.DIRECTION_SUFFIXES])
        measured = [angles[suffix] for suffix in muzzle.DIRECTION_SUFFIXES]
        self.assertTrue(all(a > b for a, b in zip(measured, measured[1:])))

    def test_reference_requires_exact_marker_counts(self) -> None:
        red = (17, 45)
        blue = [
            (18, 3), (26, 2), (34, 6), (40, 8), (45, 13), (51, 17),
            (55, 22), (58, 30), (61, 37), (64, 46), (63, 53), (60, 60),
            (59, 68), (54, 73), (49, 80), (44, 84), (36, 86), (29, 89),
            (21, 93),
        ]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "reference.png"
            image = Image.new("RGBA", (80, 96), muzzle.TRANSPARENT)
            image.putpixel(red, muzzle.RED_MARKER)
            for point in blue:
                image.putpixel(point, muzzle.BLUE_MARKER)
            image.save(path)
            assigned, _angles = muzzle.read_reference(path)
            self.assertEqual(19, len(assigned))

            image.putpixel(blue[-1], muzzle.TRANSPARENT)
            image.save(path)
            with self.assertRaisesRegex(muzzle.MetadataError, "blue marker count is 18"):
                muzzle.read_reference(path)

            image.putpixel(blue[-1], muzzle.BLUE_MARKER)
            image.putpixel((0, 0), muzzle.RED_MARKER)
            image.save(path)
            with self.assertRaisesRegex(muzzle.MetadataError, "red marker count is 2"):
                muzzle.read_reference(path)

    def test_pixel_center_converts_to_pivot_relative_offset(self) -> None:
        self.assertEqual([0.5, 0.5], muzzle.pixel_to_pivot_offset((24, 87)))
        self.assertEqual([-0.5, -0.5], muzzle.pixel_to_pivot_offset((23, 88)))

    def test_json_serialization_is_deterministic(self) -> None:
        metadata = {"schemaVersion": 1, "states": [{"name": "Idle", "frames": []}]}
        first = muzzle.serialize_metadata(metadata)
        second = muzzle.serialize_metadata(metadata)
        self.assertEqual(first, second)
        self.assertTrue(first.endswith("\n"))
        self.assertEqual(metadata, json.loads(first))


class LongwatchRealArtIntegrationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.result = muzzle.generate(REPO_ROOT)

    def test_real_reference_and_sheet_contract(self) -> None:
        reference_path = REPO_ROOT / muzzle.REFERENCE_RELATIVE_PATH
        with Image.open(reference_path) as reference:
            rgba = reference.convert("RGBA")
            pixels = muzzle.rgba_pixels(rgba)
            self.assertEqual((80, 96), rgba.size)
            self.assertEqual(19, pixels.count(muzzle.BLUE_MARKER))
            self.assertEqual(1, pixels.count(muzzle.RED_MARKER))

        sheets = muzzle.load_production_sheets(REPO_ROOT)
        self.assertEqual(19 * 5, len(sheets))
        for spec in muzzle.STATE_SPECS:
            state_sheets = [
                sheets[(spec.name, suffix)] for suffix in muzzle.DIRECTION_SUFFIXES
            ]
            self.assertEqual(19, len(state_sheets))
            self.assertTrue(
                all(
                    sheet.width == spec.frame_count * 80 and sheet.height == 96
                    for sheet in state_sheets
                )
            )

    def test_real_corpus_resolves_all_required_frames(self) -> None:
        expected_supported_points = {
            "Idle": 38,
            "Run": 114,
            "Backpedal": 76,
            "Crouch": 96,
            "Fall": 19,
        }
        states = self.result.metadata["states"]
        self.assertEqual(list(expected_supported_points), [state["name"] for state in states])
        for state in states:
            self.assertEqual(
                list(muzzle.DIRECTION_SUFFIXES),
                [direction["suffix"] for direction in state["directions"]],
            )
            supported_points = sum(
                len(direction["frames"])
                for direction in state["directions"]
                if direction["supported"]
            )
            self.assertEqual(expected_supported_points[state["name"]], supported_points)
            unsupported = [
                direction["suffix"]
                for direction in state["directions"]
                if not direction["supported"]
            ]
            expected_unsupported = (
                ["m70", "m80", "m90"] if state["name"] == "Crouch" else []
            )
            self.assertEqual(expected_unsupported, unsupported)
            for direction in state["directions"]:
                expected_frames = (
                    [] if not direction["supported"] else list(range(state["frameCount"]))
                )
                self.assertEqual(
                    expected_frames,
                    [frame["frame"] for frame in direction["frames"]],
                )
        self.assertEqual(343, sum(expected_supported_points.values()))
        self.assertEqual(
            {suffix: 5 for suffix in muzzle.DIRECTION_SUFFIXES},
            self.result.signature_sizes,
        )

    def test_checked_in_json_is_byte_deterministic(self) -> None:
        generated_path = REPO_ROOT / muzzle.OUTPUT_RELATIVE_PATH
        self.assertTrue(generated_path.is_file())
        self.assertEqual(
            self.result.serialized.encode("utf-8"),
            generated_path.read_bytes(),
        )
        second = muzzle.generate(REPO_ROOT)
        self.assertEqual(self.result.serialized.encode(), second.serialized.encode())


if __name__ == "__main__":
    unittest.main()

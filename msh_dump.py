#!/usr/bin/env python3
"""Dump the Earth 2150 MSH layout emitted by Aod2Msh.exe."""

from __future__ import annotations

import argparse
import math
import struct
from pathlib import Path


EFFECT_TYPES = {
    0: "Unspecified/Group",
    1: "Explosion",
    2: "Track",
    3: "ScaleableObject",
    4: "MappedExplosion",
    5: "FlatExplosion",
    6: "Laser",
    7: "LaserWall",
    8: "Shockwave",
    9: "Line",
    10: "Sphere",
    11: "ElectricalCannon",
    12: "Lighting",
    13: "Smoke",
    14: "Kilwater",
}

LIGHT_TYPES = {0: "Const", 1: "Pyramid", 2: "Trapezium", 3: "Random"}

FOOTPRINT_ANCHORS = ((0, 3), (0, 0), (3, 0), (3, 3))
FOOTPRINT_FLAG_PERMUTATIONS = (
    (1, 0, 3, 2),
    (0, 3, 2, 1),
    (3, 2, 1, 0),
    (2, 1, 0, 3),
)


def rotate_footprint_slot(quarter_turn: int, row: int, column: int) -> int:
    return (
        4 * (3 - column) + row,
        4 * row + column,
        4 * column + (3 - row),
        15 - (4 * row + column),
    )[quarter_turn]


class Reader:
    def __init__(self, data: bytes) -> None:
        self.data = data
        self.offset = 0

    def read(self, size: int) -> bytes:
        end = self.offset + size
        if end > len(self.data):
            raise ValueError(
                f"read past EOF at 0x{self.offset:x}: need 0x{size:x} bytes, "
                f"file ends at 0x{len(self.data):x}"
            )
        value = self.data[self.offset:end]
        self.offset = end
        return value

    def unpack(self, fmt: str):
        size = struct.calcsize(fmt)
        return struct.unpack(fmt, self.read(size))

    def u8(self) -> int:
        return self.unpack("<B")[0]

    def u32(self) -> int:
        return self.unpack("<I")[0]

    def i32(self) -> int:
        return self.unpack("<i")[0]

    def f32x3(self) -> tuple[float, float, float]:
        return self.unpack("<3f")

    def sized_ascii(self) -> str:
        return self.read(self.u32()).decode("ascii")


def validate_rotated_footprints(
    fixed_payload: bytes, box_mask: int, header_start: int
) -> None:
    descriptors = struct.unpack_from("<4I", fixed_payload, 0x1A0)
    flag_maps = struct.unpack_from("<4Q", fixed_payload, 0x1B0)
    physical_flags = fixed_payload[0x190:0x1A0]

    if box_mask == 0:
        if any(descriptors) or any(flag_maps):
            raise ValueError(
                f"empty footprint has nonzero derivatives at 0x{header_start:x}"
            )
        return

    for quarter_turn in range(4):
        destination_slots = []
        expected_flag_map = (1 << 64) - 1

        for source_slot in range(16):
            if box_mask & (1 << (15 - source_slot)) == 0:
                continue

            row, column = divmod(source_slot, 4)
            destination_slot = rotate_footprint_slot(quarter_turn, row, column)
            destination_slots.append(destination_slot)
            destination_bit = 15 - destination_slot

            source_flags = physical_flags[source_slot]
            permutation = FOOTPRINT_FLAG_PERMUTATIONS[quarter_turn]
            destination_flags = sum(
                ((source_flags >> source_bit) & 1) << destination_bit_index
                for destination_bit_index, source_bit in enumerate(permutation)
            )
            shift = 4 * destination_bit
            expected_flag_map &= ~(0xF << shift)
            expected_flag_map |= destination_flags << shift

        rows = [slot // 4 for slot in destination_slots]
        columns = [slot % 4 for slot in destination_slots]
        min_row, max_row = min(rows), max(rows)
        min_column, max_column = min(columns), max(columns)
        unused_midpoint_bias_a = min_row + math.trunc(
            (max_column + 1 - min_row) / 2
        )
        unused_midpoint_bias_b = min_column + math.trunc(
            (max_row + 1 - min_column) / 2
        )
        anchor_x, anchor_y = FOOTPRINT_ANCHORS[quarter_turn]
        occupancy = sum(1 << (15 - slot) for slot in destination_slots)
        expected_descriptor = (
            occupancy
            | anchor_x << 30
            | anchor_y << 28
            | unused_midpoint_bias_a << 26
            | unused_midpoint_bias_b << 24
        )

        if descriptors[quarter_turn] != expected_descriptor:
            raise ValueError(
                f"footprint descriptor {quarter_turn} at 0x{header_start:x} is "
                f"0x{descriptors[quarter_turn]:08x}, expected "
                f"0x{expected_descriptor:08x}"
            )
        if flag_maps[quarter_turn] != expected_flag_map:
            raise ValueError(
                f"footprint flag map {quarter_turn} at 0x{header_start:x} is "
                f"0x{flag_maps[quarter_turn]:016x}, expected "
                f"0x{expected_flag_map:016x}"
            )


def dump_object(
    reader: Reader, source_depth: int = 0, details: bool = False
) -> int:
    start = reader.offset
    vertex_count = reader.u32()
    block_count = reader.u32()
    blocks_start = reader.offset
    blocks = reader.read(block_count * 0xA0)
    for block_index in range(block_count):
        for lane_index in range(4):
            texture_w_offset = block_index * 0xA0 + 0x80 + lane_index * 4
            texture_w_bits = struct.unpack_from("<I", blocks, texture_w_offset)[0]
            if texture_w_bits != 0:
                raise ValueError(
                    f"vertex texture W/reserved lane at "
                    f"0x{blocks_start + texture_w_offset:x} is "
                    f"0x{texture_w_bits:08x}, expected zero"
                )
    flags_offset = reader.offset
    flags = reader.u32()
    name_length = reader.u32()
    texture = reader.read(name_length).decode("ascii")
    index_count = reader.u32()
    indices = [reader.unpack("<4H") for _ in range(index_count)]
    vertex_positions = [
        tuple(
            struct.unpack_from(
                "<f",
                blocks,
                (vertex_index // 4) * 0xA0
                + channel_offset
                + (vertex_index % 4) * 4,
            )[0]
            for channel_offset in (0x00, 0x10, 0x20)
        )
        for vertex_index in range(vertex_count)
    ]
    face_normal_zs: list[float | None] = []
    for triangle_index, (i0, i1, i2, triangle_flags) in enumerate(indices):
        if max(i0, i1, i2) >= vertex_count:
            raise ValueError(
                f"triangle {triangle_index} references vertex outside "
                f"0..{vertex_count - 1}"
            )
        p0, p1, p2 = (vertex_positions[index] for index in (i0, i1, i2))
        ax, ay, az = (p1[index] - p0[index] for index in range(3))
        bx, by, bz = (p2[index] - p1[index] for index in range(3))
        nx = az * by - ay * bz
        ny = bz * ax - az * bx
        nz = ay * bx - by * ax
        length = math.sqrt(nx * nx + ny * ny + nz * nz)
        face_normal_z = nz / length if length > 0.0 else None
        face_normal_zs.append(face_normal_z)
        expected_flags = 3 if face_normal_z is not None and face_normal_z > 0.5 else 1
        if triangle_flags != expected_flags:
            raise ValueError(
                f"triangle {triangle_index} flags 0x{triangle_flags:04x} do not "
                f"match computed flags 0x{expected_flags:04x}"
            )
    scale_count = reader.u32()
    scales = [reader.f32x3() for _ in range(scale_count)]
    translation_count = reader.u32()
    translations = [reader.f32x3() for _ in range(translation_count)]
    matrix_count = reader.u32()
    matrices = [reader.unpack("<16f") for _ in range(matrix_count)]
    animation_type = reader.u32()
    position = reader.f32x3()
    barrel = reader.u8()
    next_marker_offset = reader.offset
    next_marker = reader.u32()

    nested_source = bool(flags & 0x800)
    unwind_count = flags & 0xFF
    source_depth += int(nested_source) - unwind_count
    if source_depth < 0:
        raise ValueError(
            f"invalid hierarchy unwind {unwind_count} at 0x{flags_offset:x}"
        )
    indent = "  " * source_depth

    print(
        f"{indent}object @ 0x{start:08x}..0x{reader.offset:08x}: "
        f"vertices={vertex_count}, blocks={block_count}, "
        f"flags=0x{flags:08x}@0x{flags_offset:x}, texture={texture!r}, "
        f"indices={index_count}, scales={scale_count}, "
        f"translations={translation_count}, matrices={matrix_count}, "
        f"animation_type={animation_type}, position={position}, barrel={barrel}, "
        f"source_depth={source_depth}, nested_source={nested_source}, "
        f"unwind={unwind_count}, "
        f"next=0x{next_marker:08x}@0x{next_marker_offset:x}"
    )
    if details:
        for vertex_index in range(vertex_count):
            block_offset = (vertex_index // 4) * 0xA0
            lane_offset = (vertex_index % 4) * 4
            channels = tuple(
                struct.unpack_from("<f", blocks, block_offset + channel + lane_offset)[0]
                for channel in range(0, 0x90, 0x10)
            )
            lane_u16 = (vertex_index % 4) * 2
            same_normal = struct.unpack_from(
                "<H", blocks, block_offset + 0x90 + lane_u16
            )[0]
            same_position = struct.unpack_from(
                "<H", blocks, block_offset + 0x98 + lane_u16
            )[0]
            print(
                f"{indent}  v{vertex_index}: pos={channels[0:3]}, "
                f"normal={channels[3:6]}, uv={channels[6:8]}, "
                f"texture_w_reserved={channels[8]}, "
                f"previous_same_normal=0x{same_normal:04x}, "
                f"previous_same_position=0x{same_position:04x}"
            )
        for index, triangle in enumerate(indices):
            face_normal_z = face_normal_zs[index]
            normal_text = (
                "degenerate" if face_normal_z is None else f"{face_normal_z:.9g}"
            )
            print(
                f"{indent}  triangle{index}: indices={triangle[:3]}, "
                f"flags=0x{triangle[3]:04x}, face_normal_z={normal_text}"
            )
        for index, value in enumerate(scales):
            print(f"{indent}  scale{index}: {value}")
        for index, value in enumerate(translations):
            print(f"{indent}  translation{index}: {value}")
        for index, value in enumerate(matrices):
            print(f"{indent}  matrix{index}: {value}")
    if next_marker:
        return dump_object(reader, source_depth, details)
    return source_depth


def read_base_header(
    reader: Reader, indent: str = "  ", details: bool = False
) -> bytes:
    header_start = reader.offset
    magic = reader.read(4)
    version = reader.u32()
    if magic != b"MESH":
        raise ValueError(f"bad magic {magic!r} at 0x{header_start:x}")
    fixed_payload = reader.read(0x360)
    mesh_kind, box_mask, animation_lengths, animation_frames = struct.unpack_from(
        "<4I", fixed_payload
    )
    validate_rotated_footprints(fixed_payload, box_mask, header_start)
    animation_lengths_by_type = tuple(
        (animation_lengths >> (8 * (3 - animation_type))) & 0xFF
        for animation_type in range(4)
    )
    animation_frames_by_type = tuple(
        (animation_frames >> (8 * (3 - animation_type))) & 0xFF
        for animation_type in range(4)
    )
    boxes = [
        (
            index,
            struct.unpack_from("<H", fixed_payload, 0x18E - 2 * index)[0],
            fixed_payload[0x19F - index],
        )
        for index in range(16)
        if box_mask & (1 << index)
    ]
    cannon_positions = [
        struct.unpack_from("<3f", fixed_payload, 0x10 + 12 * index)
        for index in range(4)
    ]
    spot_lights = []
    for index in range(1, 5):
        record_offset = 0x10 + 0x30 * index
        record = fixed_payload[record_offset : record_offset + 0x30]
        if any(record[0x0C:]):
            spot_lights.append(
                (
                    index,
                    struct.unpack_from("<3f", record, 0x00),
                    struct.unpack_from("<3f", record, 0x0C),
                    struct.unpack_from("<f", record, 0x18)[0],
                    record[0x1C],
                    struct.unpack_from("<4f", record, 0x20),
                )
            )
    attachments = [
        (index, *struct.unpack_from("<hhhBB", fixed_payload, 0x1C8 + 8 * index))
        for index in range(1, 50)
        if struct.unpack_from("<3h", fixed_payload, 0x1C8 + 8 * index)
        != (-0x8000, -0x8000, -0x8000)
    ]
    extents = struct.unpack_from("<4H", fixed_payload, 0x358)
    print(
        f"{indent}base header @ 0x{header_start:08x}..0x{reader.offset:08x}: "
        f"version={version}, mesh_kind={mesh_kind}, box_mask=0x{box_mask:08x}, "
        f"animation_lengths(type0..3)={animation_lengths_by_type}, "
        f"animation_frames(type0..3)={animation_frames_by_type}, "
        f"boxes={len(boxes)}, "
        f"attachments={len(attachments)}, "
        f"extents(+Y,-Y,+X,-X)={extents}"
    )
    if details:
        if any(any(value != 0 for value in position) for position in cannon_positions):
            for index, position in enumerate(cannon_positions, 1):
                print(f"{indent}  cannon{index}: position={position}")
        for index, position, parameters, distance, heading, derived in spot_lights:
            print(
                f"{indent}  spot{index}: position={position}, "
                f"parameters={parameters}, distance={distance:g}, "
                f"heading={heading}/256 turn, derived={derived}"
            )
        for index, height, box_flags in boxes:
            print(
                f"{indent}  box{index}: height={height}/256, "
                f"flags=0x{box_flags:02x}"
            )
        for index, x, y, z, heading, yaw_half_range in attachments:
            print(
                f"{indent}  attachment{index}: "
                f"position=({x}/256,{y}/256,{z}/256), "
                f"heading={heading}/256 turn, "
                f"yaw_half_range=0x{yaw_half_range:02x}"
            )
    return fixed_payload


def dump_dynamic_record(reader: Reader, depth: int = 0) -> None:
    indent = "  " * (depth + 1)
    start = reader.offset
    read_base_header(reader, indent)

    effect_type = reader.u32()
    light_type = reader.u32()
    first_frame = reader.i32()
    frame_count = reader.i32()
    frame_columns = reader.i32()
    frame_rows = reader.i32()
    frame_period = reader.i32()
    reciprocal_columns, reciprocal_rows = reader.unpack("<2f")
    start_rectangle = reader.unpack("<4f")
    end_rectangle = reader.unpack("<4f")
    effect_z_or_depth_offset, ribbon_half_width = reader.unpack("<2f")
    reserved_3b4_offset = reader.offset
    reserved_3b4 = reader.u32()
    if reserved_3b4 != 0:
        raise ValueError(
            f"reserved dynamic word at 0x{reserved_3b4_offset:x} is "
            f"0x{reserved_3b4:08x}, expected zero"
        )
    additive = reader.u32()
    terrain_light_rgb = reader.unpack("<3f")
    color = reader.unpack("<3f")
    terrain_light_gain = reader.unpack("<f")[0]
    alpha_mode = reader.u32()
    alpha_end, alpha_start = reader.unpack("<2f")
    scale_end, scale_start = reader.unpack("<2f")
    child_translation_start = reader.f32x3()
    child_translation_end = reader.f32x3()
    mesh_name = reader.sized_ascii()
    texture_name = reader.sized_ascii()
    child_count = reader.u32()

    print(
        f"{indent}dynamic @ 0x{start:08x}..0x{reader.offset:08x}: "
        f"type={EFFECT_TYPES.get(effect_type, 'unknown')}({effect_type}), "
        f"light_type={LIGHT_TYPES.get(light_type, 'unknown')}({light_type}), "
        f"frames=({first_frame}, count={frame_count}, columns={frame_columns}, "
        f"rows={frame_rows}, period={frame_period}, "
        f"reciprocals=({reciprocal_columns:g},{reciprocal_rows:g})), "
        f"start_rectangle={start_rectangle}, end_rectangle={end_rectangle}, "
        f"effect_z_or_depth_offset={effect_z_or_depth_offset:g}, "
        f"ribbon_half_width={ribbon_half_width:g}, "
        f"reserved_3b4={reserved_3b4}, additive={additive}, "
        f"terrain_light_rgb={terrain_light_rgb}, color={color}, "
        f"terrain_light_gain={terrain_light_gain:g}, alpha_mode={alpha_mode}, "
        f"alpha=(start={alpha_start:g}, end={alpha_end:g}), "
        f"scale=(start={scale_start:g}, end={scale_end:g}), "
        f"child_translation=(start={child_translation_start}, "
        f"end={child_translation_end}), mesh={mesh_name!r}, "
        f"texture={texture_name!r}, children={child_count}"
    )
    for _ in range(child_count):
        dump_dynamic_record(reader, depth + 1)


def dump(path: Path, details: bool = False) -> None:
    reader = Reader(path.read_bytes())
    framing = reader.u32()
    print(path)
    if framing == 0x20D0A1FF:
        guid = reader.read(16)
        print(f"  framing=static(0x{framing:08x}), guid={guid.hex()}")
        read_base_header(reader, details=details)
        trailing_unwind_offset = reader.offset
        trailing_unwind_count = reader.u32()
        print(
            f"  trailing_unwind_count={trailing_unwind_count}"
            f"@0x{trailing_unwind_offset:x}"
        )
        final_source_depth = dump_object(reader, details=details)
        expected_unwind_count = final_source_depth + 1
        if trailing_unwind_count != expected_unwind_count:
            raise ValueError(
                f"trailing unwind count {trailing_unwind_count} does not match "
                f"final source depth {final_source_depth} + 1"
            )
    elif framing == 0x30D0A1FF:
        archive_type = reader.u32()
        guid = reader.read(16)
        print(
            f"  framing=dynamic(0x{framing:08x}), archive_type={archive_type}, "
            f"guid={guid.hex()}"
        )
        dump_dynamic_record(reader)
    else:
        raise ValueError(f"unknown archive framing 0x{framing:08x}")

    if reader.offset != len(reader.data):
        raise ValueError(
            f"unparsed trailing data: 0x{reader.offset:x}..0x{len(reader.data):x}"
        )
    print(f"  EOF matched at 0x{reader.offset:x} ({reader.offset} bytes)")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("files", nargs="+", type=Path)
    parser.add_argument(
        "--details", action="store_true", help="dump vertices, triangles, and tracks"
    )
    args = parser.parse_args()
    for path in args.files:
        dump(path, args.details)


if __name__ == "__main__":
    main()

"""
Lecture et écriture du format .aseprite (spécification : github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md).

Écriture : sprite RGBA, un calque par entrée de `layer_names`, une frame par liste de calques, palette de la charte
jointe (fenêtre Palette d'Aseprite). Lecture : aplatit les calques visibles de chaque frame (cels bruts, liés ou
compressés, opacité de calque et de cel, mode de fusion normal), ce qui permet de réexporter les retouches en PNG
sans dépendre de l'exécutable Aseprite.
"""
from __future__ import annotations

import struct
import zlib
from pathlib import Path
from typing import Sequence

import numpy as np
from PIL import Image

MAGIC_FILE = 0xA5E0
MAGIC_FRAME = 0xF1FA
CHUNK_LAYER = 0x2004
CHUNK_CEL = 0x2005
CHUNK_PALETTE = 0x2019
CEL_RAW, CEL_LINKED, CEL_COMPRESSED = 0, 1, 2
LAYER_VISIBLE, LAYER_EDITABLE = 1, 2
LAYER_TYPE_GROUP = 1


def _string(text: str) -> bytes:
    data = text.encode("utf-8")
    return struct.pack("<H", len(data)) + data


def _chunk(kind: int, payload: bytes) -> bytes:
    return struct.pack("<IH", len(payload) + 6, kind) + payload


def write_aseprite(path: Path, frames: Sequence[Sequence[Image.Image]], layer_names: Sequence[str],
                   palette: Sequence[tuple[int, int, int]] = (), duration_ms: int = 100) -> None:
    """`frames[i][j]` : calque j de la frame i, toutes les images à la même taille (RGBA)."""
    width, height = frames[0][0].size
    frame_blobs = []
    for index, layers in enumerate(frames):
        chunks = []
        if index == 0:
            for name in layer_names:
                payload = struct.pack("<HHHHHHB3x", LAYER_VISIBLE | LAYER_EDITABLE, 0, 0, 0, 0, 0, 255) + _string(name)
                chunks.append(_chunk(CHUNK_LAYER, payload))
            if palette:
                entries = b"".join(struct.pack("<HBBBB", 0, r, g, b, 255) for r, g, b in palette)
                chunks.append(_chunk(CHUNK_PALETTE, struct.pack("<III8x", len(palette), 0, len(palette) - 1) + entries))
        for layer_index, image in enumerate(layers):
            rgba = image.convert("RGBA")
            bbox = rgba.getbbox()
            if bbox is None:
                continue
            cel = rgba.crop(bbox)
            payload = struct.pack("<HhhBHh5x", layer_index, bbox[0], bbox[1], 255, CEL_COMPRESSED, 0)
            payload += struct.pack("<HH", cel.width, cel.height) + zlib.compress(cel.tobytes(), 9)
            chunks.append(_chunk(CHUNK_CEL, payload))
        body = b"".join(chunks)
        header = struct.pack("<IHHH2xI", 16 + len(body), MAGIC_FRAME, min(len(chunks), 0xFFFF), duration_ms, len(chunks))
        frame_blobs.append(header + body)

    frames_data = b"".join(frame_blobs)
    header = struct.pack("<IHHHHHIH8xB3xHBBhhHH84x", 128 + len(frames_data), MAGIC_FILE, len(frames), width, height,
                         32, 1, duration_ms, 0, len(palette) if 0 < len(palette) <= 256 else 0, 1, 1, 0, 0, 16, 16)
    assert len(header) == 128
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(header + frames_data)


def read_aseprite(path: Path) -> list[Image.Image]:
    """Frames aplaties (calques visibles, du bas vers le haut). Sprites RGBA uniquement."""
    data = path.read_bytes()
    _, magic, frame_count, width, height, depth = struct.unpack_from("<IHHHHH", data, 0)
    if magic != MAGIC_FILE:
        raise ValueError(f"{path} n'est pas un fichier Aseprite")
    if depth != 32:
        raise ValueError(f"{path} : mode couleur {depth} bits non géré, repasser le sprite en RGBA (Sprite › Color Mode)")

    layers: list[dict] = []
    frames_cels: list[dict[int, tuple[int, int, int, Image.Image]]] = []
    offset = 128
    for _ in range(frame_count):
        frame_size, frame_magic, old_chunks, _duration = struct.unpack_from("<IHHH", data, offset)
        if frame_magic != MAGIC_FRAME:
            raise ValueError(f"{path} : frame corrompue")
        new_chunks = struct.unpack_from("<I", data, offset + 12)[0]
        chunk_count = new_chunks if new_chunks else old_chunks
        cursor = offset + 16
        cels: dict[int, tuple[int, int, int, Image.Image]] = {}
        for _ in range(chunk_count):
            chunk_size, kind = struct.unpack_from("<IH", data, cursor)
            payload = data[cursor + 6:cursor + chunk_size]
            if kind == CHUNK_LAYER:
                flags, layer_type, child_level, _, _, blend, opacity = struct.unpack_from("<HHHHHHB", payload, 0)
                layers.append({"visible": bool(flags & LAYER_VISIBLE), "type": layer_type, "level": child_level,
                               "blend": blend, "opacity": opacity})
            elif kind == CHUNK_CEL:
                layer_index, x, y, opacity, cel_type = struct.unpack_from("<HhhBH", payload, 0)
                body = payload[16:]
                if cel_type == CEL_LINKED:
                    linked_frame = struct.unpack_from("<H", body, 0)[0]
                    cels[layer_index] = frames_cels[linked_frame][layer_index]
                elif cel_type in (CEL_RAW, CEL_COMPRESSED):
                    cel_width, cel_height = struct.unpack_from("<HH", body, 0)
                    pixels = body[4:] if cel_type == CEL_RAW else zlib.decompress(body[4:])
                    image = Image.frombytes("RGBA", (cel_width, cel_height), pixels[:cel_width * cel_height * 4])
                    cels[layer_index] = (x, y, opacity, image)
            cursor += chunk_size
        frames_cels.append(cels)
        offset += frame_size

    visible = _effective_visibility(layers)
    result = []
    for cels in frames_cels:
        canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        for layer_index, layer in enumerate(layers):
            if layer["type"] == LAYER_TYPE_GROUP or not visible[layer_index] or layer_index not in cels:
                continue
            if layer["blend"] != 0:
                raise ValueError(f"{path} : calque {layer_index} en mode de fusion non normal, non géré")
            x, y, cel_opacity, image = cels[layer_index]
            alpha = layer["opacity"] * cel_opacity / (255 * 255)
            if alpha < 1.0:
                pixels = np.asarray(image).copy()
                pixels[..., 3] = (pixels[..., 3] * alpha).round().astype(np.uint8)
                image = Image.fromarray(pixels, "RGBA")
            layer_canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
            layer_canvas.paste(image, (x, y))
            canvas.alpha_composite(layer_canvas)
        result.append(canvas)
    return result


def _effective_visibility(layers: list[dict]) -> list[bool]:
    """Un calque est visible si lui et tous les groupes qui le contiennent le sont."""
    result = []
    parents: list[bool] = []
    for layer in layers:
        level = layer["level"]
        parents = parents[:level]
        shown = layer["visible"] and all(parents)
        result.append(shown)
        if layer["type"] == LAYER_TYPE_GROUP:
            parents.append(layer["visible"] and all(parents))
    return result

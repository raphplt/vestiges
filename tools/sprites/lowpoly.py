"""
Modèles low poly tirés des volumes SDF du pipeline (plan 60, lot 60.0a).

Le volume d'une pose est échantillonné sur une grille grossière, puis maillé par « surface nets » : un sommet par
cellule traversée par la surface, une face par arête de la grille qui la traverse. Chaque triangle prend la couleur du
matériau qu'il recouvre ; les sommets sont partagés, et ne se doublent qu'à la frontière de deux matériaux. Une animation devient une suite de
maillages, un par pose, comme les sprites : on ne réécrit aucun modèle.

Sortie glTF 2.0 binaire (.glb) : un nœud par pose, nommé `<animation>_<NN>`, sous un nœud racine ; un manifeste JSON à
côté donne les animations, leurs poses et la cadence. Unités : le mètre, Y vers le haut, l'avant du modèle vers +Z.

Format compact (plan 60 §18, lot C3a), en glTF de base seulement : couleurs sur 8 bits, indices sur 16 bits quand le
maillage le permet, et pas de normale en low poly. L'ombrage par faces se calcule dans le shader du jeu, depuis les
dérivées de la position (`lowpoly_flat.gdshader`) ; le manifeste le dit par `flat_normals`. Une pose identique à une
précédente de la même animation réutilise son maillage.
"""
from __future__ import annotations

import hashlib
import json
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Sequence

import numpy as np

from .palette import Material
from .render import Part, _evaluate, _evaluate_with_part

# Les 8 coins d'une cellule et ses 12 arêtes (indices de coins).
CORNERS = np.array([[0, 0, 0], [1, 0, 0], [0, 1, 0], [1, 1, 0], [0, 0, 1], [1, 0, 1], [0, 1, 1], [1, 1, 1]])
EDGES = [(0, 1), (2, 3), (4, 5), (6, 7), (0, 2), (1, 3), (4, 6), (5, 7), (0, 4), (1, 5), (2, 6), (3, 7)]


@dataclass
class Mesh:
    """
    Sommets partagés, une couleur (sRGB) par sommet, triangles en indices. Normales par sommet en mid poly ; aucune en
    low poly (`normals` vaut None), où l'ombrage par faces vient du shader. `emissive` dit, triangle par triangle, s'il
    brille.
    """
    positions: np.ndarray
    normals: np.ndarray | None
    colors: np.ndarray
    emissive: np.ndarray
    indices: np.ndarray

    @property
    def triangles(self) -> int:
        return len(self.indices)


def material_colors(materials: Sequence[Material]) -> tuple[np.ndarray, np.ndarray]:
    """
    Couleur de face de chaque matériau (son ton de base, troisième de la rampe) et son caractère émissif. Écrite en sRGB
    et non en linéaire comme le veut glTF : Godot en GL Compatibility relit COLOR_0 comme du sRGB, quel que soit le
    drapeau du matériau (mesuré : une couleur linéaire s'affiche deux fois assombrie).
    """
    colors = np.array([np.array(m.ramp[2][:3], dtype=np.float64) / 255.0 for m in materials])
    emissive = np.array([m.emissive for m in materials], dtype=bool)
    return colors, emissive


def find_bounds(parts: Sequence[Part], box_min, box_max, step: float) -> tuple[np.ndarray, np.ndarray] | None:
    """Boîte de la matière, cherchée à pas grossier dans une boîte large, agrandie d'un pas."""
    axes = [np.arange(lo, hi + step, step) for lo, hi in zip(box_min, box_max)]
    grid = np.stack(np.meshgrid(*axes, indexing="ij"), axis=-1).reshape(-1, 3)
    d, _ = _evaluate(parts, grid)
    inside = grid[d < step * 0.5]
    if len(inside) == 0:
        return None
    return inside.min(axis=0) - step, inside.max(axis=0) + step


def _surface_nets(parts: Sequence[Part], bounds_min, bounds_max, step: float) -> tuple[np.ndarray, np.ndarray]:
    """Sommets de la surface (un par cellule traversée) et quadrilatères qui les relient (indices de sommets)."""
    lo = np.asarray(bounds_min, dtype=np.float64) - step
    hi = np.asarray(bounds_max, dtype=np.float64) + step
    shape = np.ceil((hi - lo) / step).astype(int) + 1
    axes = [lo[a] + np.arange(shape[a]) * step for a in range(3)]
    grid = np.stack(np.meshgrid(*axes, indexing="ij"), axis=-1)
    d, _ = _evaluate(parts, grid.reshape(-1, 3))
    d = d.reshape(shape)
    inside = d < 0.0
    nx, ny, nz = shape

    # Cellules traversées par la surface, et leur sommet : moyenne des passages à zéro sur leurs arêtes.
    corner_d = [d[cx:nx - 1 + cx, cy:ny - 1 + cy, cz:nz - 1 + cz] for cx, cy, cz in CORNERS]
    corner_in = [c < 0.0 for c in corner_d]
    any_in = np.logical_or.reduce(corner_in)
    all_in = np.logical_and.reduce(corner_in)
    active = any_in & ~all_in
    accum = np.zeros(active.shape + (3,))
    count = np.zeros(active.shape)
    for a, b in EDGES:
        da, db = corner_d[a], corner_d[b]
        cross = (da < 0.0) != (db < 0.0)
        t = np.where(cross, da / np.where(cross, da - db, 1.0), 0.0)
        point = CORNERS[a] + t[..., None] * (CORNERS[b] - CORNERS[a])
        accum += np.where(cross[..., None], point, 0.0)
        count += cross
    index = np.full(active.shape, -1, dtype=np.int64)
    cells = np.argwhere(active)
    index[active] = np.arange(len(cells))
    local = accum[active] / np.maximum(count[active], 1)[:, None]
    vertices = lo + (cells + local) * step

    # Une face par arête de grille traversée : les quatre cellules qui la partagent.
    quads = []
    for axis in range(3):
        u, v = [a for a in range(3) if a != axis]
        sl_a = [slice(None)] * 3
        sl_b = [slice(None)] * 3
        sl_a[axis] = slice(0, -1)
        sl_b[axis] = slice(1, None)
        crossing = inside[tuple(sl_a)] != inside[tuple(sl_b)]
        for p in np.argwhere(crossing):
            if p[u] == 0 or p[v] == 0 or p[u] >= shape[u] - 1 or p[v] >= shape[v] - 1:
                continue
            ring = []
            for du, dv in ((-1, -1), (0, -1), (0, 0), (-1, 0)):
                c = p.copy()
                c[u] += du
                c[v] += dv
                ring.append(index[tuple(c)])
            if min(ring) >= 0:
                quads.append(ring)
    return vertices, np.array(quads, dtype=np.int64).reshape(-1, 4)


def _split_quads(vertices: np.ndarray, quads: np.ndarray) -> np.ndarray:
    """Coupe chaque quadrilatère selon sa diagonale la plus courte : triangles en indices de sommets."""
    a, b, c, e = (vertices[quads[:, i]] for i in range(4))
    short_ac = np.linalg.norm(a - c, axis=1) <= np.linalg.norm(b - e, axis=1)
    t1 = np.where(short_ac[:, None], quads[:, [0, 1, 2]], quads[:, [0, 1, 3]])
    t2 = np.where(short_ac[:, None], quads[:, [0, 2, 3]], quads[:, [1, 2, 3]])
    return np.concatenate([t1, t2])


def _empty_mesh() -> Mesh:
    return Mesh(np.zeros((0, 3)), None, np.zeros((0, 3)), np.zeros(0, dtype=bool), np.zeros((0, 3), dtype=np.int64))


def mesh_parts(parts: Sequence[Part], materials: Sequence[Material], bounds_min, bounds_max, step: float) -> Mesh:
    """
    Maille l'union des volumes dans la boîte, au pas `step` (unités du modèle), pour un ombrage par faces. Chaque face
    prend le matériau juste sous elle ; un sommet sert à toutes ses faces d'un même matériau.
    """
    vertices, quads = _surface_nets(parts, bounds_min, bounds_max, step)
    if not len(quads):
        return _empty_mesh()
    tris = _split_quads(vertices, quads)
    corners = vertices[tris]
    normal = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    length = np.linalg.norm(normal, axis=1)
    keep = length > 1e-9
    tris, corners, normal = tris[keep], corners[keep], normal[keep] / length[keep, None]

    # Oriente chaque face vers l'extérieur, puis prend le matériau juste sous elle.
    centroid = corners.mean(axis=1)
    out_d, _ = _evaluate(parts, centroid + normal * step * 0.5)
    in_d, _ = _evaluate(parts, centroid - normal * step * 0.5)
    flip = out_d < in_d
    tris[flip] = tris[flip][:, ::-1]
    normal[flip] = -normal[flip]
    _, mat, _ = _evaluate_with_part(parts, centroid - normal * step * 0.3)
    colors, emissive = material_colors(materials)
    mat = np.where(mat < 0, 0, mat)

    # Un sommet par couple (sommet de la grille, matériau) : la couleur reste franche d'une face à l'autre.
    keys = tris * len(materials) + mat[:, None]
    unique, remap = np.unique(keys.reshape(-1), return_inverse=True)
    return Mesh(vertices[unique // len(materials)], None, colors[unique % len(materials)], emissive[mat],
                remap.reshape(-1, 3))


def mesh_parts_smooth(parts: Sequence[Part], materials: Sequence[Material], bounds_min, bounds_max, step: float) -> Mesh:
    """
    Même surface, sommets partagés et ombrage lissé (« mid poly ») : la normale d'un sommet est le gradient du volume,
    sa couleur celle du matériau juste sous lui. Un triangle est émissif si l'un de ses sommets l'est.
    """
    vertices, quads = _surface_nets(parts, bounds_min, bounds_max, step)
    if not len(quads):
        return _empty_mesh()
    tris = _split_quads(vertices, quads)
    corners = vertices[tris]
    normal = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    keep = np.linalg.norm(normal, axis=1) > 1e-9
    tris, corners, normal = tris[keep], corners[keep], normal[keep]
    normal /= np.linalg.norm(normal, axis=1)[:, None]
    centroid = corners.mean(axis=1)
    out_d, _ = _evaluate(parts, centroid + normal * step * 0.5)
    in_d, _ = _evaluate(parts, centroid - normal * step * 0.5)
    flip = out_d < in_d
    tris[flip] = tris[flip][:, ::-1]

    h = step * 0.25
    gradient = np.zeros_like(vertices)
    for axis in range(3):
        offset = np.zeros(3)
        offset[axis] = h
        plus, _ = _evaluate(parts, vertices + offset)
        minus, _ = _evaluate(parts, vertices - offset)
        gradient[:, axis] = plus - minus
    normals = gradient / np.maximum(np.linalg.norm(gradient, axis=1), 1e-9)[:, None]
    _, mat, _ = _evaluate_with_part(parts, vertices - normals * step * 0.3)
    colors, emissive = material_colors(materials)
    mat = np.where(mat < 0, 0, mat)
    tri_emissive = emissive[mat][tris].any(axis=1)
    return Mesh(vertices, normals, colors[mat], tri_emissive, tris)


def merged(pieces: Sequence[Mesh]) -> Mesh:
    """Un maillage fait de plusieurs, sans les souder."""
    offsets = np.cumsum([0] + [len(p.positions) for p in pieces[:-1]])
    normals = None if any(p.normals is None for p in pieces) else np.concatenate([p.normals for p in pieces])
    return Mesh(np.concatenate([p.positions for p in pieces]), normals, np.concatenate([p.colors for p in pieces]),
                np.concatenate([p.emissive for p in pieces]),
                np.concatenate([p.indices + o for p, o in zip(pieces, offsets)]))


def scaled(mesh: Mesh, factor: float) -> Mesh:
    return Mesh(mesh.positions * factor, mesh.normals, mesh.colors, mesh.emissive, mesh.indices)


@dataclass
class ModelExport:
    model_id: str
    # Poses par animation, dans l'ordre : chaque pose est un maillage.
    animations: dict[str, list[Mesh]]
    fps: float = 8.0
    kind: str = "creature"


COMPONENT_TYPES = {5121: np.uint8, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}


def _mesh_key(mesh: Mesh) -> bytes:
    """Empreinte d'un maillage, pour n'écrire qu'une fois deux poses identiques."""
    digest = hashlib.blake2b(digest_size=16)
    for array in (mesh.positions.astype(np.float32), mesh.colors.astype(np.float32), mesh.indices.astype(np.int64),
                  mesh.emissive.astype(np.uint8)):
        digest.update(array.tobytes())
    return digest.digest()


def write_glb(export: ModelExport, path: Path) -> dict:
    """Écrit le .glb et son manifeste ; renvoie le manifeste."""
    chunks: list[bytes] = []
    buffer_views: list[dict] = []
    accessors: list[dict] = []
    offset = 0

    def add(data: np.ndarray, kind: str, target: int, component: int = 5126, with_bounds: bool = False) -> int:
        """Un accesseur : flottants (5126), octets normalisés (5121) ou indices sur 16 (5123) ou 32 bits (5125)."""
        nonlocal offset
        raw = data.astype(COMPONENT_TYPES[component]).tobytes()
        pad = (-len(raw)) % 4
        chunks.append(raw + b"\0" * pad)
        buffer_views.append({"buffer": 0, "byteOffset": offset, "byteLength": len(raw), "target": target})
        offset += len(raw) + pad
        accessor = {"bufferView": len(buffer_views) - 1, "componentType": component, "count": int(len(data)), "type": kind}
        if component == 5121:
            accessor["normalized"] = True
        if with_bounds:
            accessor["min"] = data.min(axis=0).astype(float).tolist()
            accessor["max"] = data.max(axis=0).astype(float).tolist()
        accessors.append(accessor)
        return len(accessors) - 1

    def add_mesh(name: str, mesh: Mesh) -> int:
        if not mesh.triangles:
            # Un décor plat sort sans volume (plan 60 §17) : il garde son nœud, sans surface.
            meshes.append({"name": name, "primitives": []})
            return len(meshes) - 1
        # Sommets partagés par les deux surfaces ; seuls les indices se partagent entre l'éteinte et l'émissive.
        attributes = {"POSITION": add(mesh.positions, "VEC3", 34962, with_bounds=True)}
        if mesh.normals is not None:
            attributes["NORMAL"] = add(mesh.normals, "VEC3", 34962)
        # Un attribut de sommet s'aligne sur 4 octets : la couleur prend un alpha opaque.
        rgba = np.concatenate([np.round(np.clip(mesh.colors, 0.0, 1.0) * 255.0), np.full((len(mesh.colors), 1), 255.0)], axis=1)
        attributes["COLOR_0"] = add(rgba, "VEC4", 34962, 5121)
        index_type = 5123 if len(mesh.positions) <= 0xFFFF else 5125
        primitives = []
        for emissive in (False, True):
            chosen = mesh.indices[mesh.emissive == emissive]
            if len(chosen):
                primitives.append({"attributes": attributes, "indices": add(chosen.reshape(-1), "SCALAR", 34963, index_type),
                                   "material": 1 if emissive else 0})
        meshes.append({"name": name, "primitives": primitives})
        return len(meshes) - 1

    meshes: list[dict] = []
    nodes: list[dict] = [{"name": export.model_id, "children": []}]
    manifest_animations = {}
    triangles = 0
    flat = False
    by_content: dict[bytes, int] = {}
    for name, poses in export.animations.items():
        frame_names = []
        for i, mesh in enumerate(poses):
            node_name = f"{name}_{i:02d}"
            flat |= mesh.normals is None
            key = _mesh_key(mesh)
            if key not in by_content:
                by_content[key] = add_mesh(node_name, mesh)
            nodes.append({"name": node_name, "mesh": by_content[key]})
            nodes[0]["children"].append(len(nodes) - 1)
            frame_names.append(node_name)
            triangles = max(triangles, mesh.triangles)
        manifest_animations[name] = frame_names

    gltf = {
        "asset": {"version": "2.0", "generator": "vestiges tools/sprites/lowpoly.py"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": nodes,
        "meshes": meshes,
        "materials": [
            {"name": "lowpoly", "pbrMetallicRoughness": {"baseColorFactor": [1, 1, 1, 1], "metallicFactor": 0.0, "roughnessFactor": 1.0}},
            {"name": "lowpoly_emissive", "emissiveFactor": [1.0, 1.0, 1.0],
             "pbrMetallicRoughness": {"baseColorFactor": [1, 1, 1, 1], "metallicFactor": 0.0, "roughnessFactor": 1.0}},
        ],
        "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": offset}],
    }
    body = json.dumps(gltf, separators=(",", ":")).encode()
    body += b" " * ((-len(body)) % 4)
    binary = b"".join(chunks)
    total = 12 + 8 + len(body) + 8 + len(binary)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, total))
        f.write(struct.pack("<II", len(body), 0x4E4F534A))
        f.write(body)
        f.write(struct.pack("<II", len(binary), 0x004E4942))
        f.write(binary)

    manifest = {"model": export.model_id, "kind": export.kind, "fps": export.fps, "max_triangles": triangles,
                "flat_normals": flat, "meshes": len(meshes), "animations": manifest_animations}
    path.with_suffix(".json").write_text(json.dumps(manifest, indent="\t", ensure_ascii=False) + "\n")
    return manifest

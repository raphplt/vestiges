#!/usr/bin/env python3
"""
Exporte des modèles du pipeline SDF en low poly (plan 60, lot 60.0a) : un .glb et son manifeste par modèle, dans
assets/models/. Les volumes et les poses sont ceux des sprites ; rien n'est redessiné.

    python3 tools/export_lowpoly.py                       # le jeu d'essai de 60.0a
    python3 tools/export_lowpoly.py --character facteur --creature rodeur --prop forest:prop_rock_mossy
    python3 tools/export_lowpoly.py --step 3.0            # pas de grille plus grossier : moins de faces
Pas par défaut : 1,6 unité pour un personnage (le visage reste lisible), 2,0 pour une créature, un quarantième de la
taille pour un décor.
"""
from __future__ import annotations

import argparse
import importlib
import sys
import time
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.sprites import models  # noqa: E402
from tools.sprites.lowpoly import Mesh, ModelExport, find_bounds, mesh_parts, scaled, write_glb  # noqa: E402
from tools.sprites.props._kit import M  # noqa: E402
from tools.sprites.render import CHARACTER_MODEL_SCALE, MODEL_SCALE, PITCH  # noqa: E402

OUT = ROOT / "assets" / "models"
# Les personnages sont modélisés plus grands que les créatures et rendus à une échelle plus petite : ramenés au
# mètre, ils gardent à l'écran la taille relative des sprites.
CHARACTER_METRES = CHARACTER_MODEL_SCALE / MODEL_SCALE / M
CREATURE_METRES = 1.0 / M

DEFAULT_SET = {
    "character": ["facteur"],
    "creature": ["rampant", "rodeur", "charognard"],
    "prop": ["forest:prop_tree_medium_base+prop_tree_medium_canopy", "urban_buildings:prop_bld_house_w3_a",
             "forest:prop_rock_mossy"],
}
PROP_MODULES = {"forest": "forest", "urban_buildings": "buildings", "quarry": "quarry", "urban": "urban",
                "swamp": "swamp", "fields": "fields", "fields_farm": "farm"}


def _frame_box(frame_size, scale):
    width = frame_size[0] / scale
    height = frame_size[1] / (scale * np.cos(PITCH))
    return (-width * 0.7, -height * 0.2, -width * 0.7), (width * 0.7, height * 1.1, width * 0.7)


def _mesh_pose(parts, materials, box, step) -> Mesh:
    bounds = find_bounds(parts, box[0], box[1], max(step * 1.6, 3.0))
    if bounds is None:
        raise ValueError("volume vide")
    return mesh_parts(parts, materials, bounds[0], bounds[1], step)


def export_animated(model: models.SpriteModel, kind: str, metres: float, step: float) -> ModelExport:
    box = model.bounds if model.bounds is not None else _frame_box(model.frame_size, model.scale)
    animations = {}
    for name, poses in model.animations.items():
        animations[name] = [scaled(_mesh_pose(model.parts(pose), model.materials, box, step), metres) for pose in poses]
    return ModelExport(model.model_id, animations, kind=kind)


def export_prop(spec: str, step: float | None) -> ModelExport:
    biome, stems = spec.split(":", 1)
    catalog = {m.stem: m for m in importlib.import_module(f"tools.sprites.props.{PROP_MODULES[biome]}").catalog()}
    pieces = []
    for stem in stems.split("+"):
        prop = catalog[stem]
        box = prop.bounds if prop.bounds is not None else _frame_box(prop.canvas, MODEL_SCALE)
        extent = float(np.max(np.asarray(box[1]) - np.asarray(box[0])))
        prop_step = step if step is not None else max(2.4, extent / 40.0)
        pieces.append(scaled(_mesh_pose(prop.parts(), prop.materials, box, prop_step), CREATURE_METRES))
    merged = Mesh(np.concatenate([p.positions for p in pieces]), np.concatenate([p.normals for p in pieces]),
                  np.concatenate([p.colors for p in pieces]), np.concatenate([p.emissive for p in pieces]))
    model_id = stems.split("+")[0].removesuffix("_base")
    return ModelExport(model_id, {"idle": [merged]}, kind="prop")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--character", action="append", default=[])
    parser.add_argument("--creature", action="append", default=[])
    parser.add_argument("--prop", action="append", default=[], help="biome:stem, ou biome:stem+stem pour fondre deux pièces")
    parser.add_argument("--step", type=float, default=None, help="pas de grille en unités du modèle, pour tous les modèles")
    args = parser.parse_args()
    if not (args.character or args.creature or args.prop):
        args.character, args.creature, args.prop = DEFAULT_SET["character"], DEFAULT_SET["creature"], DEFAULT_SET["prop"]
    character_step = args.step if args.step is not None else 1.6
    creature_step = args.step if args.step is not None else 2.0

    jobs = [("characters", lambda i=i: export_animated(models.character(i), "character", CHARACTER_METRES, character_step))
            for i in args.character]
    jobs += [("creatures", lambda i=i: export_animated(models.creature(i), "creature", CREATURE_METRES, creature_step))
             for i in args.creature]
    jobs += [("props", lambda s=s: export_prop(s, args.step)) for s in args.prop]
    for folder, job in jobs:
        start = time.time()
        export = job()
        manifest = write_glb(export, OUT / folder / f"{export.model_id}.glb")
        poses = sum(len(v) for v in manifest["animations"].values())
        size = export.animations["idle"][0].positions
        height = float(size[:, 1].max() - size[:, 1].min()) if len(size) else 0.0
        print(f"[lowpoly] {folder}/{export.model_id}.glb : {poses} poses, {manifest['max_triangles']} triangles au plus, "
              f"{height:.2f} m de haut, {time.time() - start:.1f} s")


if __name__ == "__main__":
    main()

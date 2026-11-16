#!/usr/bin/env python3
"""
Exporte des modèles du pipeline SDF en low poly (plan 60, lot 60.0a) : un .glb et son manifeste par modèle. Les volumes
et les poses sont ceux des sprites ; rien n'est redessiné.

Modèles choisis un à un, dans assets/models/ (ce que lit le bac à sable) :
    python3 tools/export_lowpoly.py                       # le jeu d'essai de 60.0a
    python3 tools/export_lowpoly.py --character facteur --creature rodeur --prop forest:prop_rock_mossy
    python3 tools/export_lowpoly.py --step 3.0            # pas de grille plus grossier : moins de faces
    python3 tools/export_lowpoly.py --detail mid          # mid poly lissé, dans assets/models/mid/ (non versionné)

Export de masse (plan 60 §17, lot C1), dans la bibliothèque build/models/ (non versionnée, ignorée de Godot) :
    python3 tools/export_lowpoly.py --all --jobs 8        # personnages, créatures, faune et les douze catalogues de décors
    python3 tools/export_lowpoly.py --catalog forest --catalog creatures --missing
    python3 tools/export_lowpoly.py --list                # ce que --all exporterait, sans rien écrire
Catalogues : characters, creatures, fauna, et ceux de tools/generate_props.py (forest, swamp, urban, chests, ...). Dans
un catalogue de décors, le tronc et la canopée d'un arbre (`_base`, `_canopy`) se fondent en un modèle, et des variantes
orientées (`_x`/`_y`, `_h`/`_v`, `_e`/`_w`/`_n`/`_s`, et la forme sans suffixe) on ne garde que la première : la caméra tourne, le jeu tourne le modèle. Le
fichier index.json de la bibliothèque dit, modèle par modèle, ses sources, ses variantes écartées, son poids et sa durée.

Pas par défaut en low poly : 1,6 unité pour un personnage (le visage reste lisible), 2,0 pour une créature, un
quarantième de la taille pour un décor, affiné jusqu'à 1,2 pour un décor long et mince (clôture, grillage) que la grille
traverserait ; en mid poly, deux fois plus fin. Un processus tué en route (mémoire) n'arrête pas l'export : ce qu'il
restait à faire repasse un par un.

Budgets de triangles (plan 60 §18, lot C3c), dans tools/sprites/lowpoly_budgets.json : par sorte de modèle, des paliers
[taille minimale en mètres, budget], la taille étant la plus grande dimension du modèle. Une créature ou un personnage
au-dessus de son budget reprend un pas plus grossier, cherché par dichotomie, jusqu'à `max_coarsening` fois le pas
d'origine ; au-delà, il garde son pas et l'index note le dépassement. Un décor (`coarsen` faux) ne se grossit pas : ses
portes, fenêtres, parois et chaînes se défont dès 1,2 fois le pas, et ses pièces minces font l'essentiel de son poids ;
l'index note seulement son dépassement. Le mid poly a quatre fois le budget. `--step` impose le pas et ignore les
budgets.
"""
from __future__ import annotations

import argparse
import importlib
import json
import math
import os
import re
import sys
import time
import traceback
from concurrent.futures import ProcessPoolExecutor, as_completed
from concurrent.futures.process import BrokenProcessPool
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.generate_fauna import model as fauna_model  # noqa: E402
from tools.generate_props import BIOMES  # noqa: E402
from tools.sprites import models  # noqa: E402
from tools.sprites.lowpoly import (Mesh, ModelExport, find_bounds, merged, mesh_parts, mesh_parts_adaptive,  # noqa: E402
                                   mesh_parts_smooth, scaled, write_glb)  # noqa: E402
from tools.sprites.props._kit import M  # noqa: E402
from tools.sprites.render import MODEL_SCALE, PITCH  # noqa: E402

OUT = ROOT / "assets" / "models"
LIBRARY = ROOT / "build" / "models"
SPRITES = ROOT / "tools" / "sprites"
BUDGETS = SPRITES / "lowpoly_budgets.json"
# Itérations de la dichotomie du pas, une fois le budget encadré : un huitième de l'écart suffit.
BISECTIONS = 3
# Un modèle se ramène au mètre par son échelle de rendu : les personnages, modélisés plus grands et rendus plus petits,
# gardent à l'écran la taille relative des sprites, et la faune son agrandissement propre.
CREATURE_METRES = 1.0 / M

DEFAULT_SET = {
    "character": ["facteur"],
    "creature": ["rampant", "rodeur", "charognard"],
    "prop": ["forest:prop_tree_medium_base+prop_tree_medium_canopy", "urban_buildings:prop_bld_house_w3_a",
             "forest:prop_rock_mossy"],
}
ANIMATED = ("characters", "creatures", "fauna")
ORIENTED = re.compile(r"^(.*)_(?:x|y|e|w|n|s|h|v)(_v\d+)?$")
# Pas le plus fin de l'affinage des pièces minces (unités du modèle, 1 m ≈ 27,5) : 4 cm, même quand le décor reprend un
# pas plus fin partout.
FINE_FLOOR = 1.2


def _metres(model: models.SpriteModel) -> float:
    return model.scale / MODEL_SCALE * CREATURE_METRES


def _frame_box(frame_size, scale):
    width = frame_size[0] / scale
    height = frame_size[1] / (scale * np.cos(PITCH))
    return (-width * 0.7, -height * 0.2, -width * 0.7), (width * 0.7, height * 1.1, width * 0.7)


def _bounds(parts, box, step):
    bounds = find_bounds(parts, box[0], box[1], max(step * 1.6, 3.0))
    if bounds is None:
        raise ValueError("volume vide")
    return bounds


def _mesh_pose(parts, materials, box, step, smooth: bool) -> Mesh:
    bounds = _bounds(parts, box, step)
    mesher = mesh_parts_smooth if smooth else mesh_parts
    return mesher(parts, materials, bounds[0], bounds[1], step)


def export_animated(model: models.SpriteModel, kind: str, step: float, smooth: bool) -> ModelExport:
    box = model.bounds if model.bounds is not None else _frame_box(model.frame_size, model.scale)
    metres = _metres(model)
    animations = {}
    for name, poses in model.animations.items():
        animations[name] = [scaled(_mesh_pose(model.parts(pose), model.materials, box, step, smooth), metres)
                            for pose in poses]
    return ModelExport(model.model_id, animations, kind=kind)


def _props_catalog(catalog: str) -> dict:
    return {m.stem: m for m in importlib.import_module(BIOMES[catalog][0]).catalog()}


def _mesh_prop(prop, box, prop_step: float, smooth: bool, fineness: float, fine_base: float) -> tuple[Mesh, int]:
    """
    Le pas d'un décor suit la taille de son cadre, et ses pièces minces (fût, flèche, branche, grillage) passent entre
    les points de la grille : la grille s'affine localement autour d'elles (plan 60 §18, lot C3b), à la moitié du pas
    sans descendre sous `FINE_FLOOR`. Un pas fin calé sur la taille réelle de la matière triplait les triangles des décors
    pour un gain que la planche ne montre pas. `fine_base` est le pas d'avant le budget (lot C3c) : un décor grossi pour
    tenir son budget garde ses pièces minces telles quelles. En dernier recours, un décor qui sort encore trop bas
    (clôture couchée) reprend un pas plus fin partout. Renvoie aussi les triangles de l'affinage.
    """
    parts = prop.parts()
    coarse_step = max(prop_step * 1.6, 3.0)
    bounds = find_bounds(parts, box[0], box[1], coarse_step)
    if bounds is None:
        raise ValueError("volume vide")
    # La boîte grossière déborde de la matière d'un pas et demi de chaque côté.
    expected = float(bounds[1][1] - bounds[0][1]) - 3.0 * coarse_step
    while True:
        mesh_bounds = _bounds(parts, box, prop_step)
        fine_step = max(FINE_FLOOR / fineness, min(prop_step, fine_base) / 2.0)
        mesh, detail = mesh_parts_adaptive(parts, prop.materials, mesh_bounds[0], mesh_bounds[1], prop_step, fine_step, smooth)
        height = float(np.ptp(mesh.positions[:, 1])) if len(mesh.positions) else 0.0
        if height >= 0.6 * expected or prop_step <= 1.2:
            return mesh, detail
        prop_step = max(1.2, prop_step * 0.5)


def export_prop(catalog: str, stems: list[str], model_id: str, step: float | None, smooth: bool, fineness: float,
                coarsening: float = 1.0) -> ModelExport:
    props = _props_catalog(catalog)
    pieces = []
    detail = 0
    for stem in stems:
        prop = props[stem]
        box = prop.bounds if prop.bounds is not None else _frame_box(prop.canvas, MODEL_SCALE)
        extent = float(np.max(np.asarray(box[1]) - np.asarray(box[0])))
        if step is not None:
            mesh = _mesh_pose(prop.parts(), prop.materials, box, step, smooth)
        else:
            prop_step = max(2.4 / fineness, extent / (40.0 * fineness))
            mesh, added = _mesh_prop(prop, box, prop_step * coarsening, smooth, fineness, prop_step)
            detail += added
        pieces.append(scaled(mesh, CREATURE_METRES))
    return ModelExport(model_id, {"idle": [merged(pieces)]}, kind="prop", detail_triangles=detail)


def _triangles(export: ModelExport) -> int:
    return max((mesh.triangles for poses in export.animations.values() for mesh in poses), default=0)


def _size_m(export: ModelExport) -> float:
    positions = export.animations[next(iter(export.animations))][0].positions
    return float(np.ptp(positions, axis=0).max()) if len(positions) else 0.0


def budget_for(kind: str, size_m: float, budgets: dict) -> int:
    """Le budget du plus haut palier que la taille atteint (paliers rangés par taille croissante)."""
    return [budget for size, budget in budgets[kind]["tiers"] if size_m >= size][-1]


def fit_budget(build: Callable[[float], ModelExport], kind: str, budgets: dict, scale: float) -> tuple[ModelExport, dict]:
    """
    Exporte au pas d'origine (`build(1.0)`), puis, au-dessus du budget, au pas multiplié par le plus petit facteur qui y
    rentre. Les triangles baissent comme le carré du pas : la première tentative vise ce facteur, la dichotomie affine.
    Renvoie le modèle et ce que l'index en retient.
    """
    export = build(1.0)
    triangles = _triangles(export)
    budget = int(budget_for(kind, _size_m(export), budgets) * scale)
    info = {"budget": budget, "coarsening": 1.0}
    if triangles <= budget:
        return export, info
    if not budgets[kind]["coarsen"]:
        info["over_budget"] = True
        return export, info
    limit = float(budgets["max_coarsening"])
    low, high = 1.0, min(limit, math.sqrt(triangles / budget) * 1.05)
    while True:
        candidate = build(high)
        if _triangles(candidate) <= budget:
            best = (high, candidate)
            break
        if high >= limit:
            # Hors de portée : le grossir l'abîmerait sans le faire tenir. Il garde son pas, le dépassement est noté.
            info["over_budget"] = True
            return export, info
        low, high = high, min(limit, high * 1.2)
    for _ in range(BISECTIONS):
        middle = (low + high) / 2.0
        candidate = build(middle)
        if _triangles(candidate) <= budget:
            high, best = middle, (middle, candidate)
        else:
            low = middle
    info["coarsening"] = round(best[0], 3)
    return best[1], info


@dataclass
class Job:
    """Un modèle à exporter : d'où il vient, où il va, et ce qu'on en a écarté."""
    catalog: str
    model_id: str
    sources: list[str]
    folder: str
    dropped: list[str] = field(default_factory=list)


def _modules(package: str) -> list[str]:
    return sorted(p.stem for p in (SPRITES / package).glob("*.py") if not p.stem.startswith("_"))


def catalog_jobs(catalog: str) -> list[Job]:
    """Les modèles d'un catalogue, troncs et canopées fondus, variantes orientées réduites à la première."""
    if catalog in ANIMATED:
        return [Job(catalog, i, [i], catalog) for i in _modules(catalog)]
    stems = list(_props_catalog(catalog))
    present = set(stems)
    jobs: list[Job] = []
    families: dict[str, Job] = {}
    for stem in stems:
        if stem.endswith("_canopy") and stem.removesuffix("_canopy") + "_base" in present:
            continue
        if stem.endswith("_base") and stem.removesuffix("_base") + "_canopy" in present:
            jobs.append(Job(catalog, stem.removesuffix("_base"), [stem, stem.removesuffix("_base") + "_canopy"], f"props/{catalog}"))
            continue
        oriented = ORIENTED.match(stem)
        family = oriented.group(1) + (oriented.group(2) or "") if oriented else stem
        if family in families:
            families[family].dropped.append(stem)
            continue
        job = Job(catalog, family, [stem], f"props/{catalog}")
        families[family] = job
        jobs.append(job)
    return jobs


def run_job(job: Job, out: Path, step: float | None, smooth: bool, fineness: float) -> dict:
    """Exporte un modèle ; renvoie sa ligne d'index. Une erreur est rapportée, pas levée : l'export de masse continue."""
    start = time.time()
    entry = {"id": job.model_id, "catalog": job.catalog, "sources": job.sources, "path": f"{job.folder}/{job.model_id}.glb"}
    if job.dropped:
        entry["dropped_variants"] = job.dropped
    try:
        if job.catalog in ANIMATED:
            kind = "character" if job.catalog == "characters" else "creature"
            model = (models.character(job.model_id) if job.catalog == "characters"
                     else models.creature(job.model_id) if job.catalog == "creatures" else fauna_model(job.model_id))
            base = (1.6 if kind == "character" else 2.0) / fineness
            build = lambda c: export_animated(model, kind, (step or base) * c, smooth)  # noqa: E731
        else:
            kind = "prop"
            build = lambda c: export_prop(job.catalog, job.sources, job.model_id, step, smooth, fineness, c)  # noqa: E731
        if step is None:
            export, budget = fit_budget(build, kind, json.loads(BUDGETS.read_text()), fineness ** 2)
            entry.update(budget)
        else:
            export = build(1.0)
        path = out / job.folder / f"{job.model_id}.glb"
        manifest = write_glb(export, path)
        idle = export.animations[next(iter(export.animations))][0].positions
        entry.update({
            "poses": sum(len(v) for v in manifest["animations"].values()),
            "meshes": manifest["meshes"],
            "max_triangles": manifest["max_triangles"],
            "detail_triangles": export.detail_triangles,
            "height_m": round(float(idle[:, 1].max() - idle[:, 1].min()), 2) if len(idle) else 0.0,
            "bytes": path.stat().st_size,
        })
    except Exception as error:  # noqa: BLE001 : un modèle en échec ne doit pas arrêter les deux cents autres
        entry["error"] = f"{type(error).__name__}: {error}"
        entry["trace"] = traceback.format_exc(limit=3)
    entry["seconds"] = round(time.time() - start, 1)
    return entry


def _print_entry(entry: dict) -> None:
    if "error" in entry:
        print(f"[lowpoly] ÉCHEC {entry['path']} : {entry['error']}", flush=True)
        return
    budget = ""
    if "budget" in entry:
        budget = f" (budget {entry['budget']}"
        budget += f", pas ×{entry['coarsening']:.2f}" if entry["coarsening"] > 1.0 else ""
        budget += ", DÉPASSÉ)" if entry.get("over_budget") else ")"
    print(f"[lowpoly] {entry['path']} : {entry['poses']} poses, {entry['max_triangles']} triangles au plus{budget}, "
          f"{entry['height_m']:.2f} m de haut, {entry['bytes'] / 1e6:.1f} Mo, {entry['seconds']:.1f} s", flush=True)


def write_index(out: Path, entries: list[dict]) -> None:
    """Index de la bibliothèque : les lignes déjà écrites restent, celles de cet export les remplacent."""
    path = out / "index.json"
    existing = {e["path"]: e for e in json.loads(path.read_text())["models"]} if path.exists() else {}
    for entry in entries:
        existing[entry["path"]] = {k: v for k, v in entry.items() if k != "trace"}
    models_list = sorted(existing.values(), key=lambda e: e["path"])
    ok = [e for e in models_list if "error" not in e]
    summary = {
        "models": len(models_list),
        "failures": len(models_list) - len(ok),
        "megabytes": round(sum(e["bytes"] for e in ok) / 1e6, 1),
        "dropped_variants": sum(len(e.get("dropped_variants", [])) for e in models_list),
        "coarsened": sum(1 for e in ok if e.get("coarsening", 1.0) > 1.0),
        "over_budget": [e["id"] for e in ok if e.get("over_budget")],
    }
    path.write_text(json.dumps({"summary": summary, "models": models_list}, indent=1, ensure_ascii=False) + "\n")
    print(f"[lowpoly] index : {path} — {summary['models']} modèles, {summary['failures']} échecs, "
          f"{summary['megabytes']} Mo, {summary['dropped_variants']} variantes orientées écartées, "
          f"{summary['coarsened']} pas grossis pour le budget, dépassements : {', '.join(summary['over_budget']) or 'aucun'}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--character", action="append", default=[])
    parser.add_argument("--creature", action="append", default=[])
    parser.add_argument("--prop", action="append", default=[], help="biome:stem, ou biome:stem+stem pour fondre deux pièces")
    parser.add_argument("--all", action="store_true", help="tous les catalogues, dans la bibliothèque")
    parser.add_argument("--catalog", action="append", default=[], help="un catalogue entier, dans la bibliothèque")
    parser.add_argument("--list", action="store_true", help="liste les modèles des catalogues choisis, sans exporter")
    parser.add_argument("--missing", action="store_true", help="n'exporte que les modèles absents de la sortie")
    parser.add_argument("--jobs", type=int, default=max(1, (os.cpu_count() or 2) // 2), help="processus en parallèle")
    parser.add_argument("--out", type=Path, default=None, help="dossier de sortie")
    parser.add_argument("--step", type=float, default=None, help="pas de grille en unités du modèle, pour tous les modèles")
    parser.add_argument("--detail", choices=("low", "mid"), default="low",
                        help="low : faces non lissées ; mid : deux fois plus fin, lissé (dans mid/ de la sortie)")
    args = parser.parse_args()
    smooth = args.detail == "mid"
    fineness = 2.0 if smooth else 1.0

    catalogs = list(ANIMATED) + sorted(BIOMES) if (args.all or args.list and not args.catalog) else args.catalog
    unknown = [c for c in catalogs if c not in ANIMATED and c not in BIOMES]
    if unknown:
        parser.error(f"catalogue inconnu : {', '.join(unknown)} (connus : {', '.join(list(ANIMATED) + sorted(BIOMES))})")
    if catalogs:
        out = args.out or LIBRARY
        jobs = [job for catalog in catalogs for job in catalog_jobs(catalog)]
    else:
        out = args.out or OUT
        if not (args.character or args.creature or args.prop):
            args.character, args.creature, args.prop = DEFAULT_SET["character"], DEFAULT_SET["creature"], DEFAULT_SET["prop"]
        jobs = [Job("characters", i, [i], "characters") for i in args.character]
        jobs += [Job("creatures", i, [i], "creatures") for i in args.creature]
        for spec in args.prop:
            catalog, stems = spec.split(":", 1)
            jobs.append(Job(catalog, stems.split("+")[0].removesuffix("_base"), stems.split("+"), "props"))
    if smooth:
        out = out / "mid"

    if args.list:
        for job in jobs:
            extra = f" (fond {' + '.join(job.sources)})" if len(job.sources) > 1 else ""
            extra += f" (écarte {', '.join(job.dropped)})" if job.dropped else ""
            print(f"{job.folder}/{job.model_id}{extra}")
        print(f"[lowpoly] {len(jobs)} modèles, {sum(len(j.dropped) for j in jobs)} variantes orientées écartées")
        return
    if args.missing:
        jobs = [j for j in jobs if not (out / j.folder / f"{j.model_id}.glb").exists()]
    if catalogs:
        out.mkdir(parents=True, exist_ok=True)
        # La bibliothèque vit dans le projet : Godot ne doit pas importer ses centaines de .glb.
        (out / ".gdignore").touch()

    started = time.time()
    entries = []
    work = [(job, out, args.step, smooth, fineness) for job in jobs]
    if args.jobs <= 1 or len(work) <= 1:
        for item in work:
            entries.append(run_job(*item))
            _print_entry(entries[-1])
    else:
        # Un processus tué (mémoire, machine chargée par d'autres builds) casse tout le pool : les modèles qu'il n'a pas
        # rendus repassent un par un plutôt que de perdre l'export.
        pending = {}
        try:
            with ProcessPoolExecutor(max_workers=args.jobs) as pool:
                pending = {pool.submit(run_job, *item): item for item in work}
                for future in as_completed(pending):
                    entries.append(future.result())
                    _print_entry(entries[-1])
                    del pending[future]
        except BrokenProcessPool:
            print(f"[lowpoly] pool interrompu, {len(pending)} modèles repassent un par un", flush=True)
            for item in pending.values():
                entries.append(run_job(*item))
                _print_entry(entries[-1])
    for entry in entries:
        if "trace" in entry:
            print(entry["trace"], file=sys.stderr)
    if catalogs:
        write_index(out, entries)
    failures = sum("error" in e for e in entries)
    print(f"[lowpoly] {len(entries)} modèles en {time.time() - started:.0f} s, {failures} échecs")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()

"""
Génère les décors d'un biome avec le pipeline procédural commun (plan 08, lots P0–P6).
Biomes disponibles : urban, urban_buildings, forest, swamp, quarry, fields, fields_farm ; « chests » produit les coffres (plan 17, lot 0A), « landmarks » le Mémorial et la Faille (vague 3), « barrier » la Barrière (plan 07 B2) et sa disposition (barrier_layout.json), « indicible » les mains de l'Indicible (B3).

Usage :
    python3 tools/generate_props.py urban                        # écrit assets/props/urban_ruins/
    python3 tools/generate_props.py urban --sheet out.png        # planche de contrôle ×3 sur le sol du biome
    python3 tools/generate_props.py urban --only prop_dumpster --dry-run --sheet out.png
    python3 tools/generate_props.py forest --only prop_stump --editable   # retouche Aseprite (voir tools/sprites/retouch.py)
    python3 tools/generate_props.py swamp --jobs 4               # rendu des décors en parallèle (même résultat)

Seuls les fichiers du catalogue sont réécrits ; les autres décors du dossier (immeubles, lot P2) restent en place.
Godot réimporte de lui-même un PNG modifié : les .import existants (et leurs uid) sont conservés.
"""
from __future__ import annotations

import argparse
import importlib
import json
import random
import sys
import time
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.props._kit import render_prop  # noqa: E402
from tools.sprites.retouch import create_source, save_unless_locked  # noqa: E402

URBAN_TILES = ["assets/tiles/ruines/tile_ruines_sol_base.png", "assets/tiles/ruines/tile_ruines_sol_v2.png",
               "assets/tiles/ruines/tile_ruines_carrelage_base.png"]
FOREST_TILES = ["assets/tiles/foret/tile_foret_sol_base.png", "assets/tiles/foret/tile_foret_sol_v2.png",
                "assets/tiles/foret/tile_foret_sousbois_base.png"]
FIELDS_TILES = ["assets/tiles/champs/tile_champs_herbe_base.png", "assets/tiles/champs/tile_champs_herbe_v2.png",
                "assets/tiles/champs/tile_champs_ble_base.png"]
SWAMP_TILES = ["assets/tiles/marecages/tile_marecages_sol_base.png", "assets/tiles/marecages/tile_marecages_sol_humide.png",
               "assets/tiles/marecages/tile_marecages_eau_base.png"]
QUARRY_TILES = ["assets/tiles/carriere/tile_carriere_sol_base.png", "assets/tiles/carriere/tile_carriere_roche_base.png",
                "assets/tiles/carriere/tile_carriere_industriel_base.png"]
BIOMES = {
    "chests": ("tools.sprites.props.chests", "assets/chests", FOREST_TILES),
    "barrier": ("tools.sprites.props.barrier", "assets/bosses/barrier", URBAN_TILES),
    "indicible": ("tools.sprites.props.indicible", "assets/bosses/indicible", FOREST_TILES),
    "landmarks": ("tools.sprites.props.landmarks", "assets/landmarks", FOREST_TILES),
    "fields": ("tools.sprites.props.fields", "assets/props/wild_fields", FIELDS_TILES),
    "fields_farm": ("tools.sprites.props.farm", "assets/props/wild_fields", FIELDS_TILES),
    "forest": ("tools.sprites.props.forest", "assets/props/forest", FOREST_TILES),
    "swamp": ("tools.sprites.props.swamp", "assets/props/swamp", SWAMP_TILES),
    "quarry": ("tools.sprites.props.quarry", "assets/props/collapsed_quarry", QUARRY_TILES),
    "urban_buildings": ("tools.sprites.props.buildings", "assets/props/urban_ruins", URBAN_TILES),
    "urban": ("tools.sprites.props.urban", "assets/props/urban_ruins",
              ["assets/tiles/ruines/tile_ruines_sol_base.png", "assets/tiles/ruines/tile_ruines_sol_v2.png",
               "assets/tiles/ruines/tile_ruines_carrelage_base.png"]),
}
MANIFEST_NAME = "props_manifest.json"
SCALE_REFERENCE = "assets/characters/vagabond/char_vagabond_SE_idle_01.png"


def _render_indexed(job: tuple[str, int]):
    """Rendu dans un processus séparé : les modèles portent des fermetures, on les reconstruit depuis le catalogue."""
    module_name, index = job
    started = time.time()
    rendered = render_prop(importlib.import_module(module_name).catalog()[index])
    return rendered, time.time() - started


def _renders(module_name: str, models, indices: list[int], jobs: int):
    """Rendus dans l'ordre du catalogue, en parallèle si demandé : le résultat est identique pixel pour pixel."""
    if jobs <= 1:
        for model in models:
            started = time.time()
            rendered = render_prop(model)
            yield model, rendered, time.time() - started
        return
    with ProcessPoolExecutor(max_workers=jobs) as pool:
        for model, (rendered, elapsed) in zip(models, pool.map(_render_indexed, [(module_name, i) for i in indices])):
            yield model, rendered, elapsed


def generate(biome: str, only: set[str], output: Path | None, sheet: Path | None, scale: int, editable: bool,
             jobs: int = 1) -> None:
    module_name, folder, tiles = BIOMES[biome]
    module = importlib.import_module(module_name)
    catalog = module.catalog()
    indices = [i for i, m in enumerate(catalog) if not only or m.stem in only]
    models = [catalog[i] for i in indices]
    # Un ensemble posé pièce à pièce (la Barrière) écrit aussi les pas qui alignent ses pièces.
    if output is not None and hasattr(module, "layout"):
        output.mkdir(parents=True, exist_ok=True)
        layout_path = output / f"{biome}_layout.json"
        layout_path.write_text(json.dumps(module.layout(), indent=2) + "\n")
        print(f"[generate_props] disposition : {layout_path}")
    images: list[tuple[str, Image.Image]] = []
    manifest_entries: dict[str, dict] = {}
    for model, rendered, elapsed in _renders(module_name, models, indices, jobs):
        images.append((model.stem, rendered.image))
        entry = {"pivot": [round(rendered.pivot[0], 2), round(rendered.pivot[1], 2)]}
        if rendered.footprint is not None:
            entry["footprint"] = [list(point) for point in rendered.footprint]
        if output is not None:
            target = output / f"{model.stem}.png"
            # Une retouche garde son cadrage : son entrée de manifeste reste celle du rendu d'origine.
            if save_unless_locked(rendered.image, target, "generate_props"):
                manifest_entries[model.stem] = entry
            if editable:
                create_source(target.with_suffix("").as_posix(), [rendered.layers], [target], "generate_props")
        print(f"[generate_props] {model.stem} {rendered.image.width}×{rendered.image.height} ({elapsed:.1f} s)", flush=True)
    if output is not None:
        write_manifest(output / MANIFEST_NAME, manifest_entries)
    if sheet is not None:
        write_sheet(images, tiles, sheet, scale)


def write_manifest(path: Path, entries: dict[str, dict]) -> None:
    """Pivot au sol et emprise projetée de chaque décor, lus par le jeu (PropManifest) pour collision et tri."""
    existing = json.loads(path.read_text()) if path.exists() else {}
    existing.update(entries)
    lines = [f'  "{stem}": {json.dumps(entry, separators=(", ", ": "))}' for stem, entry in sorted(existing.items())]
    path.write_text("{\n" + ",\n".join(lines) + "\n}\n")
    print(f"[generate_props] manifeste : {path} ({len(existing)} décors)")


def write_sheet(images: list[tuple[str, Image.Image]], tiles: list[str], path: Path, scale: int) -> None:
    """Décors posés sur le sol réel du biome, à côté du personnage, puis agrandis sans lissage."""
    columns = 5 if max(image.width for _, image in images) <= 120 else 3
    cell = (max(128, max(image.width for _, image in images) + 40), max(112, max(image.height for _, image in images) + 30))
    rows = (len(images) + columns - 1) // columns
    width, height = columns * cell[0], rows * cell[1]
    ground = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    tile_images = [Image.open(t).convert("RGBA") for t in tiles]
    rng = random.Random(7)
    # Tuiles iso 64×32 en quinconce ; coller hors du cadre est permis par paste (alpha_composite l'interdit).
    for row in range(-2, height // 16 + 2):
        for col in range(-1, width // 64 + 2):
            tile = rng.choice(tile_images)
            ground.paste(tile, (col * 64 + (32 if row % 2 else 0) - 32, row * 16 - 16), tile)
    reference = Image.open(SCALE_REFERENCE).convert("RGBA")
    draw = ImageDraw.Draw(ground)
    for index, (stem, image) in enumerate(images):
        cx = (index % columns) * cell[0] + cell[0] // 2
        cy = (index // columns) * cell[1] + cell[1] - 18
        ground.alpha_composite(image, (cx - image.width // 2 + 8, cy + 4 - image.height))
        ground.alpha_composite(reference, (cx - image.width // 2 - reference.width + 4, cy + 4 - reference.height))
        draw.text((index % columns * cell[0] + 3, index // columns * cell[1] + cell[1] - 12), stem.replace("prop_", ""),
                  fill=(236, 228, 210, 255))
    big = ground.resize((width * scale, height * scale), Image.NEAREST)
    path.parent.mkdir(parents=True, exist_ok=True)
    big.save(path)
    print(f"[generate_props] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("biome", choices=sorted(BIOMES))
    parser.add_argument("--only", nargs="*", default=[])
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=3)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    parser.add_argument("--editable", action="store_true",
                        help="crée la retouche Aseprite (art/retouches/) des décors générés ; à combiner avec --only")
    parser.add_argument("--jobs", type=int, default=1, help="processus de rendu en parallèle")
    args = parser.parse_args()
    output = None if args.dry_run else Path(BIOMES[args.biome][1])
    generate(args.biome, set(args.only), output, args.sheet, args.scale, args.editable, args.jobs)


if __name__ == "__main__":
    main()

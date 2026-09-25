---
paths:
  - "scripts/World/**"
  - "data/world/**"
  - "data/props/**"
  - "data/biomes/**"
  - "scenes/Main.tscn"
  - "tools/sprites/props/**"
---

# Monde, biomes et décors

- Biomes : mosaïque de régions (`WorldGenerator.CreateBiomeRegions`), réglée par `biome_layout` dans `data/world/world_gen.json`. Tout consommateur interroge le biome **par cellule** (`GetBiome`, `GetBiomeId`) : ne pas supposer une région unique par biome.
- Mesurer une modification de génération avec `tools/capture_run.sh` et `CAPTURE_EXTRA_ARGS="--capture-map"` (statistiques sur 40 seeds + images) avant et après.
- Décors : `EnvironmentProp.Initialize(texture, canopée, offset, blocking, footprintScale)`. `blocking` exprime une intention ; les seuils de `props` dans `world_gen.json` empêchent les petits décors de bloquer. Ne pas réintroduire de rayon de collision en dur.
- Décor procédural : son emprise exacte et son pivot viennent de `props_manifest.json` (écrit par `tools/generate_props.py`). Ajouter un décor = l'ajouter au catalogue Python, régénérer, puis le référencer dans un placeur ou un JSON de `data/props/`.
- Immeubles urbains : `UrbanBuildingPlacer` lit les modules disponibles dans le manifeste (`prop_bld_<style>_w<largeur>_<a|b>`) ; leur hauteur doit tenir dans la profondeur d'un îlot à l'écran (~190 px), sinon ils masquent la rue au nord.
- Profondeur : `Main` et les conteneurs d'entités sont triés en Y ; respecter les couches `z_index` listées dans `AGENTS.md`. Transparence derrière un décor : `PropOcclusion` (index spatial, ne pas parcourir tous les décors par frame).
- Vérifier collisions et profondeur par `CAPTURE_EXTRA_ARGS="--capture-props"` (formes de collision affichées) et `--capture-props --hide-collisions` (rendu propre).

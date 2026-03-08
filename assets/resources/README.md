# Resource Sprites

Grounded 32x32 isometric resource drops for VESTIGES.

These sprites were rebuilt to better match the game's tone:
- darker post-collapse silhouettes
- stronger material read at gameplay scale
- soft grounded shadows instead of floating icon shapes
- clearer distinction between organic memory, rusted debris, and hostile essence

## Variants

### Wood
- `resource_bois_1.png` - Mossy stacked log bundle
- `resource_bois_2.png` - Bound rotting logs with exposed cuts
- `resource_bois_3.png` - Decayed timber pile with roots and moss

### Stone
- `resource_pierre_1.png` - Jagged shard cluster with moss in the seams
- `resource_pierre_2.png` - Heavy fractured boulder
- `resource_pierre_3.png` - Layered slate-like rubble

### Metal
- `resource_metal_1.png` - Twisted plate pile with rusted rivets
- `resource_metal_2.png` - Bent girder and industrial scrap
- `resource_metal_3.png` - Crushed rusted debris chunk

### Fiber
- `resource_fibre_1.png` - Thorny vine nest
- `resource_fibre_2.png` - Root bundle with hooked fibers
- `resource_fibre_3.png` - Dense bramble clump

### Essence
- `resource_essence_1.png` - Acid-green crystal cluster on blackened rock
- `resource_essence_2.png` - Pulsing essence core with toxic mist

## Direction

- View: 2.5D isometric, bottom-grounded
- Light: top-left
- Outline: sel-out only, never pure black
- Background: transparent
- Canvas: `32x32`

## Preview Files

- `PREVIEW_RESOURCES.png` - full sheet overview
- `SHOWCASE_DETAIL.png` - large presentation strip
- `ZOOMED_DETAILS.png` - zoomed sprite pass

## Regeneration

Run:

```bash
python3 scripts/generate_resources.py
```

Dependency:

```bash
python3 -m pip install Pillow
```

# Forêt Reconquise Tile Assets

Complete isometric pixel art tileset for the Reclaimed Forest biome.

## Files Generated

### Ground Tiles (32×16 isometric diamond shape)
- `tile_foret_herbe_1.png` - Grass/moss main ground (green mix)
- `tile_foret_herbe_2.png` - Moss-dominant variant (darker greens)
- `tile_foret_herbe_3.png` - Grass with wildflowers (yellow/purple accents)
- `tile_foret_terre_1.png` - Dirt with roots (brown with green patches)
- `tile_foret_terre_2.png` - Packed earth (tan/brown)
- `tile_foret_chemin_droit.png` - Straight path (light brown trail)
- `tile_foret_chemin_courbe.png` - Curved path
- `tile_foret_autoroute_1.png` - Cracked highway (gray with vegetation)
- `tile_foret_autoroute_2.png` - Overgrown highway (more green)

### Large Trees (32×48)
- `tile_foret_arbre_grand.png` - Thick trunk with dense organic canopy
- `tile_foret_arbre_grand_2.png` - Asymmetric canopy variant

### Medium Trees (16×32)
- `tile_foret_arbre_moyen_1.png` - Compact canopy
- `tile_foret_arbre_moyen_2.png` - Different leaf distribution
- `tile_foret_arbre_moyen_3.png` - Mixed autumn colors

### Bushes (16×12)
- `tile_foret_buisson_1.png` - Rounded moss clump
- `tile_foret_buisson_2.png` - Bush with red berries
- `tile_foret_buisson_3.png` - Wide, flat bush

### Mossy Rocks (16×12)
- `tile_foret_rocher_mousse_1.png` - Gray stone with green moss
- `tile_foret_rocher_mousse_2.png` - Different moss distribution

### Structures (32×32)
- `tile_foret_mur_ruine.png` - Ruined concrete wall with dark ivy

### Preview & Documentation
- `PREVIEW_FORET_TILES.png` - Contact sheet of all tiles
- `GENERATION_SUMMARY.md` - Technical generation details
- `README.md` - This file

## Technical Specifications

### Ground Tiles
- Format: PNG with RGBA transparency
- Dimensions: 32×16 pixels
- Shape: Isometric diamond (2:1 aspect ratio)
- Colors: 12-16 per tile (Forêt Reconquise palette)
- Rendering: Pixel-perfect, no anti-aliasing

### Tall Sprites
- Format: PNG with RGBA transparency
- Dimensions: 16×32 (medium trees), 32×48 (large trees), 16×12 (bushes/rocks), 32×32 (walls)
- Shape: Free-standing with transparent background
- Colors: 8-14 per tile
- Rendering: Pixel-perfect, no anti-aliasing

## Color Palette (Forêt Reconquise)
```
Primary Greens:
  - #2D5A27 (Vert canopée sombre) - darkest shadows, sel-out
  - #4A8C3F (Vert mousse) - main vegetation
  - #7BC558 (Vert clair) - lit leaves, new growth
  - #A4D65E (Vert-jaune) - highlights, filtered light
  - #1E3A1A (Lierre sombre) - deep undergrowth

Browns:
  - #4A3728 (Brun tronc foncé) - trunks, roots
  - #7A5C42 (Brun terreux) - soil, bark
  - #A68B6B (Brun clair) - dry wood, paths

Accents:
  - #C49B3E (Ocre automne) - autumn leaves, mushrooms
  - #8B6BAE (Fleur violette) - rare wildflowers
  - #E0C84A (Fleur jaune) - secondary wildflowers

Concrete & Stones:
  - #7A7A70 (Gris béton) - highways, foundations
  - #6B6161 (Gris chaud) - aged concrete
  - #3A3535 (Gris chaud foncé) - dark cracks

Shadows & Outlines:
  - #1A1A2E (Noir profond) - darkest shadows
  - #E8E0D4 (Blanc cassé) - highlights
```

## Pixel Art Techniques Used

1. **Sel-Out (Colored Outlines)**: All outlines use darker shades of adjacent colors, never pure black (#000000)
2. **Intentional Placement**: Every pixel is deliberately placed, no random noise or dithering
3. **Organic Clusters**: Leaf clusters are non-uniform, hand-placed for natural appearance
4. **Material Transitions**: Dithering with checkerboard patterns for smooth color transitions
5. **Subtlety**: Ground textures have subtle variation, avoiding repetitive patterns

## Usage in Godot

Add tiles to your tilemap:
1. Import all PNG files into Godot (GL Compatibility renderer recommended)
2. Create AtlasTexture resources for each tile
3. Add to TileSet for use in TileMap nodes
4. Isometric diamond ground tiles: 32×16 base layer
5. Tall sprites: Position above ground tiles with proper Y-ordering

## Generation Details

All tiles were generated using Python/Pillow with intentional pixel patterns. No procedural noise or randomization was used - each pixel represents a deliberate artistic choice. The generation script (`generate_foret_tiles.py`) is available in `/sessions/epic-inspiring-hamilton/mnt/vestiges/scripts/`.

The tiles follow the specifications in:
- `doc/CHARTE-GRAPHIQUE.md` - Visual style guide
- `doc/VESTIGES-GDD.md` - Game design specifications
- `doc/STRATEGIE-BIOMES-ET-WORKFLOW.md` - Biome workflow

## Quality Assurance

- All ground tiles conform to 32×16 isometric diamond shape
- All tall sprites have transparent backgrounds
- All colors strictly follow Forêt Reconquise palette
- Pixel-perfect rendering verified (no anti-aliasing detected)
- PNG files optimized and valid
- Preview sheet confirms visual quality

# Resource Node Sprites

Pixel-perfect harvestable resource sprites for VESTIGES, generated with cel-out technique and isometric light direction (top-left).

## Generated Assets

### 1. BOIS (Wood) - 24×32px
- `resource_bois_1.png` - Small tree with prominent trunk (choppable)
- `resource_bois_2.png` - Leaning tree variant
- `resource_bois_3.png` - Dead tree with sparse foliage

**Palette:** Brun tronc foncé, Brun terreux, Vert canopée sombre, Vert mousse, Vert clair

### 2. PIERRE (Stone) - 20×16px
- `resource_pierre_1.png` - Stacked rock formation with moss
- `resource_pierre_2.png` - Single large rounded boulder
- `resource_pierre_3.png` - Flat layered rock slab

**Palette:** Gris chaud foncé, Gris chaud, Gris clair, Blanc cassé, Vert mousse

### 3. MÉTAL (Metal) - 20×18px
- `resource_metal_1.png` - Twisted rusted wreckage
- `resource_metal_2.png` - Metal pipe/beam sticking from ground
- `resource_metal_3.png` - Crushed appliance remains

**Palette:** Métal industriel, Métal clair, Rouille foncée, Rouille orange, Gris chaud

### 4. FIBRE (Fiber) - 18×16px
- `resource_fibre_1.png` - Bushy plant with fibrous leaves
- `resource_fibre_2.png` - Tall reeds/grasses
- `resource_fibre_3.png` - Vine-covered shrub

**Palette:** Vert canopée sombre, Vert mousse, Vert clair, Vert-jaune

### 5. ESSENCE (Crystal) - 16×20px
- `resource_essence_1.png` - Crystal cluster with glow (2-3 spires)
- `resource_essence_2.png` - Single large crystal with cracks and glow

**Palette:** Gris chaud foncé, Cyan Essence, lighter variations

## Technical Specifications

- **Format:** PNG with transparency (RGBA)
- **Anti-aliasing:** None (pixel-perfect)
- **Outlines:** Cel-out technique using darker color variants (never pure black #000000)
- **Light Direction:** Top-left (highlights on upper-left, shadows on lower-right)
- **Appearance:** Interactive and distinct from decorative scenery

## Preview Assets

- `PREVIEW_RESOURCES.png` - All variants at original scale
- `ZOOMED_DETAILS.png` - 4× zoomed view showing pixel detail

## Usage in Godot

Each sprite is sized to fit standard isometric grid:
- Trees (24×32) - Taller resources
- Stones (20×16) - Medium resources  
- Metal (20×18) - Medium resources with height
- Fiber (18×16) - Small bushy plants
- Essence (16×20) - Special tall crystals

Import as Texture2D with Filter: Nearest (to maintain pixel-perfect appearance).

## Color Reference

All colors follow the Forêt Reconquise palette + universal colors defined in CHARTE-GRAPHIQUE.md:

| Color | Hex | RGB | Usage |
|-------|-----|-----|-------|
| Vert canopée sombre | #2D5A27 | (45, 90, 39) | Dark foliage |
| Vert mousse | #4A8C3F | (74, 140, 63) | Moss/green shade |
| Vert clair | #7BC558 | (123, 197, 88) | Bright green |
| Vert-jaune | #A4D65E | (164, 214, 94) | Yellow-green highlights |
| Brun tronc foncé | #4A3728 | (74, 55, 40) | Dark wood |
| Brun terreux | #7A5C42 | (122, 92, 66) | Earthy brown |
| Brun clair | #A68B6B | (166, 139, 107) | Light wood |
| Gris chaud foncé | #3A3535 | (58, 53, 53) | Deep shadow |
| Gris chaud | #6B6161 | (107, 97, 97) | Medium gray |
| Gris clair | #9E9494 | (158, 148, 148) | Light gray |
| Blanc cassé | #E8E0D4 | (232, 224, 212) | Off-white highlight |
| Noir profond | #1A1A2E | (26, 26, 46) | Cel-out outlines |
| Rouille foncée | #6B3A24 | (107, 58, 36) | Dark rust |
| Rouille orange | #A85C30 | (168, 92, 48) | Orange rust |
| Métal industriel | #5A6A7A | (90, 106, 122) | Metal base |
| Métal clair | #8A9AAA | (138, 154, 170) | Metal highlight |
| Cyan Essence | #5EC4C4 | (94, 196, 196) | Crystal glow |

Generated: 2026-03-06

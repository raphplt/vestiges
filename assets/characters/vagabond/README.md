# LE VAGABOND - Sprite Sheet Assets

## Quick Start

**Character:** Le Vagabond (P0 protagonist)
**Frame Size:** 16×24 pixels
**Total Animations:** 68 frames across 4 directions
**Format:** PNG with RGBA transparency
**Color Palette:** VESTIGES Charte Graphique (browns, oranges, earth tones)

## Files in This Directory

### Animation Frames (68 total)
- `char_vagabond_SE_*.png` — Front-facing (South-East, right)
- `char_vagabond_SW_*.png` — Front-facing (South-West, left)
- `char_vagabond_NE_*.png` — Back view (North-East, right)
- `char_vagabond_NW_*.png` — Back view (North-West, left)

Each direction has:
- `idle_01.png` to `idle_04.png` — Standing still (4 frames)
- `walk_01.png` to `walk_04.png` — Walking cycle (4 frames)
- `dash_01.png` to `dash_03.png` — Quick dodge (3 frames)
- `hurt_01.png` to `hurt_02.png` — Damage reaction (2 frames)
- `death_01.png` to `death_04.png` — Defeat sequence (4 frames)

### Reference & Documentation
- `PREVIEW_VAGABOND.png` — Visual reference showing all animations
- `GENERATION_MANIFEST.txt` — Detailed technical specifications
- `README.md` — This file

## Animation Frame Counts

| Animation | Frames | Speed | Use |
|-----------|--------|-------|-----|
| idle | 4 | 8 FPS | Standing still, breathing |
| walk | 4 | 12 FPS | Movement, exploration |
| dash | 3 | 20 FPS | Sprint, quick escape |
| hurt | 2 | 15 FPS | Take damage, pain reaction |
| death | 4 | 10 FPS | Defeat sequence, fall animation |

## Color Palette

| Part | Color | Hex |
|------|-------|-----|
| Cloak | Brun terreux | #7A5C42 |
| Tool Belt | Orange outil | #D4853A |
| Skin | Light warm | #D4A880 |
| Hair | Dark brown | #4A3728 |
| Boots | Very dark | #3A2E22 |
| Shirt | Off-white | #C8B8A0 |

## Usage in Godot

1. **Import frames** into Godot as TextureRect2D resources
2. **Create AnimatedSprite2D nodes** for each direction
3. **Set animation speeds** per frame type (see table above)
4. **Switch directions** based on player velocity
5. **Ensure z-index** above tilemap, below UI

## Regenerating Sprites

If you need to modify the sprites, the Python generation script is available:

```bash
python3 /sessions/epic-inspiring-hamilton/generate_vagabond.py
```

This will regenerate all frames in this directory (overwrites existing files).

## Technical Details

- **Pixel Art:** Hand-drawn, pixel-perfect placement
- **Lighting:** Top-left directional (highlights on upper-left)
- **Outline Style:** #3A2E22 sel-out (NOT pure black)
- **Center:** Character horizontally centered, ~8-10px width in 16px frame
- **Transparency:** Full RGBA support for clean integration

## Character Description

**Le Vagabond** — A practical wanderer dressed in worn earth tones. His brown cloak and orange tool belt reflect a life of resourcefulness. Despite hardship, his balanced stance and calm demeanor speak to experience in harsh environments. A survivor, not a warrior.

**Personality in Motion:**
- Idle: Patient, observant breathing
- Walk: Deliberate, steady pace
- Dash: Quick, survival instinct
- Hurt: Resilient, stands through pain
- Death: Graceful collapse, fades with dignity

## File Manifest

```
vagabond/
├── README.md (this file)
├── GENERATION_MANIFEST.txt
├── PREVIEW_VAGABOND.png
├── char_vagabond_SE_idle_01.png
├── char_vagabond_SE_idle_02.png
├── ... (68 total animation frames)
└── char_vagabond_NW_death_04.png
```

Total: 69 files
Directory Size: ~572 KB

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-03-06 | 1.0 | Initial generation - all 68 frames, 4 directions, 5 animation types |

## Support

For detailed technical information, see `GENERATION_MANIFEST.txt`.
For integration guide and Godot setup, see the main project documentation.

---

**Generated:** 2026-03-06
**Part of:** VESTIGES - Roguelike 2D Isométrique
**Engine:** Godot 4.6
**Script:** `/sessions/epic-inspiring-hamilton/generate_vagabond.py`

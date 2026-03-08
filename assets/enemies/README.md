# VESTIGES Enemy Sprites

## Overview

This directory contains all P0 priority enemy sprites for the VESTIGES MVP. Generated using Python/Pillow according to CHARTE-GRAPHIQUE.md specifications.

**Total Assets:** 114 frames across 4 enemies
**Status:** Production-ready for Godot 4.6 integration

---

## Enemy Catalog

### 1. RÔDEUR (Prowling Predator)
**Type:** Medium, mobile, directional
**Size:** 16×20 pixels
**Directions:** 4 (SE, SW, NE, NW)
**Frames:** 60 total

```
Animations:
  idle  (3 frames) - watchful standing pose
  walk  (4 frames) - predatory movement
  attack (4 frames) - lunge forward
  death  (4 frames) - collapse
```

**Visual:** Hunched wolf-like silhouette with elongated limbs, muscular torso, low predatory stance
**Colors:** Dark bark (#4A3728), Earth brown (#7A5C42), Dark green (#2D5A27)
**Eyes:** Acid green (#7FFF00) - 2 pixels per eye

**Usage:** Primary mid-tier threat, forest ecosystem, directional movement
**Files:** `rodeur/enemy_rodeur_[DIR]_[ANIM]_[FRAME].png`

---

### 2. CHAROGNARD (Scavenger Swarm)
**Type:** Small, mobile, 2-directional
**Size:** 12×10 pixels
**Directions:** 2 (E, W)
**Frames:** 24 total

```
Animations:
  idle   (2 frames) - crouched alert
  walk   (4 frames) - scurrying legs
  attack (3 frames) - tiny lunge
  death  (3 frames) - crumple into ball
```

**Visual:** Tiny rat-like compact body with multiple insectoid legs, tail/proboscis
**Colors:** Dark bark (#4A3728), Dark warm gray (#3A3535)
**Eyes:** Acid green (#7FFF00) - 1 pixel per side

**Usage:** Weak individually, dangerous in swarms, fast, low threat level
**Files:** `charognard/enemy_charognard_[DIR]_[ANIM]_[FRAME].png`

---

### 3. OMBRE (Night Shadow Entity)
**Type:** Small, mobile, 2-directional, night-only
**Size:** 12×12 pixels
**Directions:** 2 (E, W)
**Frames:** 20 total

```
Animations:
  idle   (2 frames) - still blob, floating eyes
  move   (3 frames) - flowing, shape-shifting
  attack (2 frames) - expand/lunge
  death  (3 frames) - dissipate/shrink
```

**Visual:** Amorphous phantom blob with ghostly wisps, no clear limbs, wavy edges
**Colors:** Noir iridescent (#2D1B3D), Dark universal (#1A1A2E), Violet mist (#4A3066)
**Eyes:** Acid green (#7FFF00) - floating in darkness, 2 pixels

**Usage:** Night/twilight enemy, ethereal threat, shape-shifting animation
**Files:** `ombre/enemy_ombre_[DIR]_[ANIM]_[FRAME].png`

---

### 4. SENTINELLE (Ancient Guardian)
**Type:** Large, stationary, static
**Size:** 12×24 pixels
**Directions:** 1 (no directional variants)
**Frames:** 10 total

```
Animations:
  idle   (3 frames) - standing tall, watching
  attack (3 frames) - eye glows, cracks reveal fluid
  death  (4 frames) - crumbles apart, debris falling
```

**Visual:** Tall stone pillar with single large feature eye, rooted base, moss patches
**Colors:** Warm gray (#6B6161), Dark warm gray (#3A3535), Light gray (#9E9494)
**Eyes:** Acid green (#7FFF00) - prominent 3-4px eye at top, major feature

**Usage:** Stationary tower defense threat, guardian of territory, immobile hazard
**Files:** `sentinelle/enemy_sentinelle_[ANIM]_[FRAME].png`

---

## File Organization

```
enemies/
├── rodeur/
│   ├── enemy_rodeur_SE_idle_01.png      (SE direction, idle, frame 1)
│   ├── enemy_rodeur_SE_idle_02.png
│   ├── enemy_rodeur_SE_idle_03.png
│   ├── enemy_rodeur_SE_walk_01.png      (SE direction, walk, frame 1)
│   │   ... (walk frames 2-4)
│   ├── enemy_rodeur_SE_attack_01.png    (SE direction, attack, frame 1)
│   │   ... (attack frames 2-4)
│   ├── enemy_rodeur_SE_death_01.png     (SE direction, death, frame 1)
│   │   ... (death frames 2-4)
│   ├── enemy_rodeur_SW_idle_01.png      (SW direction)
│   │   ... (repeat for SW, NE, NW)
│   └── PREVIEW_RODEUR.png               (60-frame preview sheet)
│
├── charognard/
│   ├── enemy_charognard_E_idle_01.png   (E direction, idle)
│   ├── enemy_charognard_E_walk_01.png   (E direction, walk)
│   │   ... (E direction all animations)
│   ├── enemy_charognard_W_idle_01.png   (W direction)
│   │   ... (W direction all animations)
│   └── PREVIEW_CHAROGNARD.png           (24-frame preview sheet)
│
├── ombre/
│   ├── enemy_ombre_E_idle_01.png        (E direction, idle)
│   ├── enemy_ombre_E_move_01.png        (E direction, move)
│   │   ... (E direction all animations)
│   ├── enemy_ombre_W_idle_01.png        (W direction)
│   │   ... (W direction all animations)
│   └── PREVIEW_OMBRE.png                (20-frame preview sheet)
│
├── sentinelle/
│   ├── enemy_sentinelle_idle_01.png     (idle, frame 1)
│   ├── enemy_sentinelle_idle_02.png     (idle, frame 2)
│   ├── enemy_sentinelle_idle_03.png     (idle, frame 3)
│   ├── enemy_sentinelle_attack_01.png   (attack, frame 1)
│   │   ... (attack frames 2-3)
│   ├── enemy_sentinelle_death_01.png    (death, frame 1)
│   │   ... (death frames 2-4)
│   └── PREVIEW_SENTINELLE.png           (10-frame preview sheet)
│
└── PREVIEW_ALL_ENEMIES.png              (2×2 grid overview)
```

---

## Naming Convention

**Format:** `enemy_[name]_[DIRECTION]_[ANIMATION]_[FRAME].png`

### Components:
- `[name]` = Enemy type: `rodeur`, `charognard`, `ombre`, `sentinelle`
- `[DIRECTION]` = Optional directional suffix (omitted for Sentinelle)
  - Rôdeur: `SE`, `SW`, `NE`, `NW`
  - Charognard: `E`, `W`
  - Ombre: `E`, `W`
  - Sentinelle: (none)
- `[ANIMATION]` = Animation state
  - Rôdeur: `idle`, `walk`, `attack`, `death`
  - Charognard: `idle`, `walk`, `attack`, `death`
  - Ombre: `idle`, `move`, `attack`, `death`
  - Sentinelle: `idle`, `attack`, `death`
- `[FRAME]` = Frame index (1-based, zero-padded)

### Examples:
```
enemy_rodeur_SE_idle_01.png      ✓ Rôdeur, SE direction, idle, frame 1
enemy_charognard_E_walk_03.png   ✓ Charognard, E direction, walk, frame 3
enemy_ombre_W_move_02.png        ✓ Ombre, W direction, move, frame 2
enemy_sentinelle_attack_01.png   ✓ Sentinelle, attack, frame 1
```

---

## Integration Guide

### For Godot Development:

1. **Import Sprites**
   ```
   Copy enemy folders into Godot project
   Godot will auto-detect PNG files on import
   ```

2. **Create SpriteFrames Resources**
   ```gdscript
   # In Godot Editor:
   Right-click sprite folder → Create SpriteFrames
   Add animation: "idle", "walk", "attack", "death"
   Drag frames in order into each animation track
   ```

3. **Configure AnimatedSprite2D**
   ```gdscript
   # In C# code:
   var animated = GetNode<AnimatedSprite2D>("Sprite");
   animated.SpriteFrames = preload("res://assets/sprites/enemies/rodeur_frames.tres");
   animated.Play("idle");
   ```

4. **Frame Speed Configuration**
   - Default: 10 FPS (0.1 seconds per frame)
   - Adjust in SpriteFrames resource based on gameplay feel
   - Target: 60 FPS render, ~10 FPS sprite animation

5. **Integration Points**
   - **Spawn System:** `scripts/Spawn/SpawnManager.cs`
   - **Enemy Factory:** `scripts/Combat/EnemyFactory.cs`
   - **Animation State Machine:** Enemy behavior controller

---

## Design Specifications

### Color Palette (FORÊT RECONQUISE biome)
- **Dark Green:** #2D5A27 (forest floor)
- **Moss Green:** #4A8C3F (vegetation)
- **Dark Bark:** #4A3728 (wood tones)
- **Earth Brown:** #7A5C42 (soil/rock)
- **Dark Warm Gray:** #3A3535 (shadows)
- **Warm Gray:** #6B6161 (mid-tones)
- **Light Gray:** #9E9494 (highlights)
- **Acid Green Eyes:** #7FFF00 (universal enemy indicator)
- **Iridescent Black:** #2D1B3D (mystical fluid)
- **Violet Highlight:** #5A3A7A (fluid shimmer)
- **Universal Dark:** #1A1A2E (deepest shadows)

### Design Rules (from CHARTE-GRAPHIQUE.md)
- **Eyes:** ALWAYS #7FFF00, 1-4 pixels depending on enemy size
- **Outlines:** Sel-out (darker shade of adjacent), never pure black
- **Light Direction:** Top-left (brightest at top-left)
- **Silhouette:** Each enemy instantly recognizable at small sizes
- **Pixel-Perfect:** No anti-aliasing, all pixels intentional

---

## Performance Notes

- **File Size:** Minimal (~200-600 bytes per frame, PNG optimized)
- **Memory:** ~114 KB total for all sprites in VRAM
- **Atlas Potential:** Can be combined into sprite atlas for reduced draw calls
- **Object Pooling:** Individual frames support pooling via Godot Resource system
- **Animation Playback:** Lightweight for 60 FPS target on mid-range hardware

---

## Preview Sheets

**Individual Enemy Previews:**
- `rodeur/PREVIEW_RODEUR.png` — All 60 Rôdeur frames in grid
- `charognard/PREVIEW_CHAROGNARD.png` — All 24 Charognard frames
- `ombre/PREVIEW_OMBRE.png` — All 20 Ombre frames
- `sentinelle/PREVIEW_SENTINELLE.png` — All 10 Sentinelle frames

**Combined Overview:**
- `PREVIEW_ALL_ENEMIES.png` — 2×2 grid with all preview sheets

Use preview sheets for quick visual reference and design validation.

---

## Generation Source

**Generator Script:** `/sessions/epic-inspiring-hamilton/generate_enemies.py`

The Python/Pillow generator can be rerun to:
- Regenerate all sprites with code modifications
- Adjust colors/palettes without manual sprite editing
- Add new animation frames to existing enemies
- Create variant biome versions (FORÊT → MONTAGNE, SOUTERRAIN, etc.)
- Generate sprites for new enemies (P1, P2 priority)

Fully extensible and documented for future asset generation.

---

## Quality Assurance

✓ All 114 frames generated and verified
✓ Correct frame counts per enemy
✓ Proper image dimensions (RGBA PNG)
✓ 100% color palette compliance
✓ Naming convention consistent
✓ Silhouettes instantly recognizable
✓ Preview sheets validated
✓ Production-ready for Godot 4.6

**Verification Date:** 2026-03-06
**Status:** COMPLETE

---

## FAQ / Troubleshooting

**Q: Can I use Charognard in E/W directions without flipping?**
A: The frames are generated for E and W separately. You can optionally flip E frames to create W if needed, or use both.

**Q: How do I adjust animation speed in Godot?**
A: In SpriteFrames resource, select animation → adjust frame duration (seconds per frame). Typical: 0.1s = 10 FPS.

**Q: Can I create variants for different biomes?**
A: Yes, regenerate sprites with the Python script, changing the color palette dictionary. Structure/silhouettes remain identical.

**Q: Are the preview sheets used in-game?**
A: No, preview sheets are for development/design reference only. Actual game uses individual frames or sprite atlases.

**Q: What about Sentinelle rotation?**
A: Sentinelle is static and doesn't rotate. If needed, regenerate with additional directional frames in the Python script.

---

## License

These sprites were generated for the VESTIGES project according to CHARTE-GRAPHIQUE.md specifications.
Part of the VESTIGES roguelike game (Godot 4.6, C#, 2D isometric).

**Design Authority:** CHARTE-GRAPHIQUE.md, VESTIGES-GDD.md, VESTIGES-ARCHITECTURE.md

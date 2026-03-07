# PROMPT — Génération IA de Pixel Art pour VESTIGES

> **Usage :** Prompt à utiliser dans un outil de génération d'images IA (Midjourney, DALL-E, Stable Diffusion, etc.)
> **Objectif :** Produire des assets pixel art de qualité professionnelle pour le jeu VESTIGES
> **Dernière mise à jour :** 7 mars 2026

---

## INSTRUCTIONS GÉNÉRALES (à inclure dans CHAQUE prompt)

```
You are a professional pixel art artist creating assets for VESTIGES, an isometric 2D roguelike survival game set in a world that wasn't destroyed — it was FORGOTTEN. Nature has reclaimed civilization. The aesthetic is beautiful, melancholic, and haunting — NOT a typical brown/gray post-apocalypse. Think Hyper Light Drifter meets Eastward.

ABSOLUTE TECHNICAL REQUIREMENTS:
- Pure pixel art. Every pixel is hand-placed. No anti-aliasing, no smooth gradients, no blur.
- Nearest-neighbor scaling ONLY. Pixels must remain crisp, square, and perfectly aligned.
- Transparent PNG background (alpha channel).
- Light source: top-left (standard isometric convention). Highlights top-left of volumes, shadows bottom-right.
- NO pure black (#000000) outlines. Use "sel-out" technique: outlines are darker, color-tinted versions of the adjacent surface color.
- NO pillow shading (light from all sides). Consistent top-left directionality.
- Color transitions use dithering (checkerboard pattern, 1px on 2px off) or flat color zones. NEVER smooth gradients.
- Each sprite must be readable at 100% zoom in a 480×270 viewport. Silhouette must be identifiable without detail.
- Maximum 12-16 colors per biome palette (listed below). No colors outside the specified palette + universal colors.
- Internal resolution: 480×270, upscaled ×4 to 1920×1080.
```

---

## PALETTES DE RÉFÉRENCE (UNIVERSELLES — toujours autorisées)

```
UNIVERSAL COLORS (allowed in ALL biomes):
- #1A1A2E  Deep black (darkest shadows, night, dark sel-out)
- #16213E  Blue-black (night, erased zones)
- #3A3535  Dark warm gray (secondary shadows, dark stone)
- #6B6161  Warm gray (stone, concrete, oxidized metal)
- #9E9494  Light gray (light stone, ash, dust)
- #E8E0D4  Off-white (highlights, bright anchored zones)
- #F5F0EB  Erasure white (fog of war, map edges, dissolution)
- #D4A843  Foyer gold (Foyer light, golden particles, score)
- #E07B39  Flame orange (fire, torches, explosions)
- #C4432B  Player blood red (player damage, low HP, danger)
- #7FFF00  Creature acid-green (creature eyes, hostile light — ALWAYS exactly this color for creature eyes, 1-2 pixels per eye)
- #5EC4C4  Essence cyan (Essence, magic, active abilities)
- #2D1B3D  Iridescent black (creature fluid, enemy death)
- #4A3066  Mist violet (erasure mist, night)
```

---

## FORMAT TILES ISOMÉTRIQUES

```
ISOMETRIC TILE FORMAT — CRITICAL:
- Tile size: 64×32 pixels (detailed tiles) or 32×16 pixels (base tiles)
- Shape: perfect isometric diamond (rhombus), ratio 2:1
- Diamond geometry (64×32): row 0 is 4px wide centered, each subsequent row adds 4px (2px each side), rows 15-16 are 64px full width, then symmetric decrease back to 4px
- Everything outside the diamond shape is TRANSPARENT
- Technique: use a dominant color (~70% of pixels) with scattered individual accent pixels randomly placed. NOT smooth noise or gradients — individual pixel accents on a flat dominant color.
- Each tile type needs 3 variants minimum (base, v2, v3) for visual variety when tiling. Variants should be subtly different (different scatter pattern, slight color variation) but clearly the same terrain type.
```

---

## FORMAT PROPS / DÉCORS

```
PROP SPRITE FORMAT:
- Transparent PNG background
- Anchored at bottom center (the "foot" of the sprite sits on the ground plane)
- Sizes vary by prop type (see per-biome specs below)
- Props with collision need a clear base area
- Multi-layer props: base sprite (trunk, rock) + optional canopy sprite (foliage, roof) rendered above the player
- Canopy sprites are semi-transparent (85% opacity) with slight color desaturation
- Props must feel ALIVE in the world — slight asymmetry, organic shapes, nothing perfectly geometric
```

---

## BIOME 1 — FORÊT RECONQUISE (Forest Reclaimed)

### Contexte narratif
Nature has devoured a suburban zone. Cracked highways under giant canopies. Houses swallowed by ivy. Rusted cars turned into planters by moss. Deceptively calm — birds sing, golden light filters through leaves. You could almost forget the world is unraveling. Almost.

### Palette Forêt
```
FOREST PALETTE (12 colors):
- #2D5A27  Dark canopy green (dense foliage, canopy shadows)
- #4A8C3F  Moss green (grass, moss, main vegetation)
- #7BC558  Light green (lit leaves, new growth)
- #A4D65E  Yellow-green (vegetation highlights, filtered light)
- #4A3728  Dark trunk brown (trunks, roots, dark wood)
- #7A5C42  Earth brown (soil, earth, bark)
- #A68B6B  Light brown (dry wood, paths, dead branches)
- #C49B3E  Autumn ochre (autumn leaves, mushrooms, warm accents)
- #7A7A70  Overgrown concrete gray (cracked highways, visible foundations)
- #1E3A1A  Dark ivy (ivy on walls, deep undergrowth)
- #8B6BAE  Purple flower (wild flowers — rare color accent)
- #E0C84A  Yellow flower (secondary wild flowers)
```

### Assets à produire — Tiles sol (64×32 chacun, format losange iso)
```
FOREST FLOOR TILES (already done — reference style):
These tiles exist and define the visual standard. New biome tiles must match this quality level and technique: dominant color with scattered individual accent pixels, NO smooth gradients.
```

### Assets à produire — Props
```
FOREST PROPS TO CREATE:

1. Grand arbre (Tree Large)
   - Base sprite: 24×48px — thick trunk with moss patches, broken branches, ivy climbing
   - Canopy sprite: 48×40px — dense leaf mass, light filtering through gaps, some autumn-colored leaves
   - Collision radius: 6px
   - Style: ancient oak/chestnut that has grown through concrete. Roots visible, cracking pavement.

2. Arbre moyen (Tree Medium)
   - Base: 16×36px — thinner trunk, more vertical
   - Canopy: 32×28px — lighter foliage, more sky visible
   - 3 variants with different silhouettes

3. Buisson (Bush)
   - 16×12px — rounded, dense, some berries visible (ochre dots)
   - No collision, 3 variants

4. Souche d'arbre (Tree Stump)
   - 12×8px — cut trunk with moss ring, mushrooms growing on side
   - 2 variants

5. Rocher moussu (Mossy Rock)
   - 16×12px — gray stone with green moss patches, some lichen
   - 2 variants

6. Tronc tombé (Fallen Log)
   - 28×10px — horizontal log, partially decomposed, mushrooms, moss carpet
   - Collision radius: 8px

7. Voiture envahie (Overgrown Car)
   - 32×20px — rusted car shell, vegetation bursting through windows and hood
   - Collision radius: 12px — clearly recognizable as a car despite nature's takeover

8. Lampadaire cassé (Broken Lamppost)
   - 8×24px — bent metal pole, covered in ivy, light fixture hanging
   - Collision radius: 3px

9. Panneau routier rouillé (Rusty Road Sign)
   - 8×16px — bent post, sign barely readable, rust orange dominant
   - No collision

10. Mur ruiné couvert de lierre (Ruined Wall with Ivy)
    - 32×24px — concrete wall fragment, cracked, ivy covering 60%+
    - Collision radius: 10px

11. Champignons (Mushroom Cluster)
    - 10×8px — 3-5 small mushrooms, some with subtle glow (ochre/green)
    - No collision

12. Fleurs sauvages (Wild Flowers)
    - 12×8px — scattered colorful pixels: purple (#8B6BAE), yellow (#E0C84A), rare red
    - No collision

13. Fougères (Ferns)
    - 14×10px — curved fronds, 2-3 shades of green
    - No collision

14. Balançoire rouillée (Rusted Swing)
    - 16×20px — metal A-frame, one chain broken, seat hanging at angle
    - Collision radius: 5px — eerie childhood relic

15. Autoroute surélevée effondrée (Collapsed Highway Fragment)
    - 48×24px — chunk of elevated highway fallen to ground, rebar exposed, plants growing
    - Collision radius: 15px — landmark prop, rare
```

---

## BIOME 2 — RUINES URBAINES (Urban Ruins)

### Contexte narratif
A medium-sized downtown. 5-8 story buildings partially collapsed. Streets covered in debris. Exposed interiors like dollhouses — a living room with a sofa suspended in the void on the 4th floor. Oppressive silence. No birds. Wind whistles between buildings. Distant cracking sounds — another building giving way. The weight of what was here and isn't anymore.

### Palette Ruines
```
URBAN RUINS PALETTE (12 colors):
- #4A4A4A  Dark concrete gray (slabs, foundations, roads)
- #737373  Medium concrete gray (walls, standing structures)
- #A0A0A0  Light concrete gray (lit concrete, dust)
- #6B3A24  Dark rust (rusted metal, beams)
- #A85C30  Orange rust (newer rust, pipes, scrap)
- #5A9A8A  Oxidized copper (copper roofs, turquoise accents)
- #8A5A42  Faded brick (brick walls, chimneys)
- #5A7A9A  Peeling blue paint (remnants of wall paint, doors)
- #C4A830  Signage yellow (panels, faded road markings)
- #4A7A3A  Reclaiming nature green (grass in cracks, isolated shrubs)
- #5A4A38  Rotting wood (furniture, window frames)
- #8AB8C4  Broken glass (glass reflections, shattered windows)
```

### Assets à produire — Tiles sol (64×32 chacun)
```
URBAN RUINS TILES:
1. Sol béton fissuré (Cracked Concrete) — 3 variants
   Dominant gray (#4A4A4A to #737373), crack lines in dark gray, occasional weed pixel in green

2. Sol intérieur carrelage cassé (Broken Interior Tile) — 2 variants
   Checkered pattern partially destroyed, exposed concrete underneath

3. Trottoir défoncé (Broken Sidewalk) — 2 variants
   Lighter gray than road, curb edge visible, cracks, some grass
```

### Assets à produire — Props
```
URBAN RUINS PROPS:

1. Immeuble effondré fragment (Collapsed Building Fragment)
   - 48×36px — partial wall with exposed rooms, rebar, hanging wallpaper
   - Collision radius: 16px — major landmark prop

2. Mur béton debout (Standing Concrete Wall)
   - 32×32px — concrete wall section, cracks, rebar poking through
   - 3 variants: intact, cracked, half-collapsed
   - Collision radius: 10px

3. Mur brique exposé (Exposed Brick Wall)
   - 24×28px — partially demolished brick wall, mortar visible
   - Collision radius: 8px

4. Poutrelle acier (Steel Beam)
   - 32×8px — I-beam, rusted, 2 variants (horizontal, diagonal)
   - Collision radius: 6px

5. Débris de béton au sol (Concrete Debris)
   - 16×8px — scattered chunks, rebar bits, dust
   - 3 variants, no collision

6. Bureau renversé (Overturned Desk)
   - 16×12px — office desk on its side, drawers open, papers scattered (pixel dots)
   - No collision

7. Voiture abandonnée urbaine (Abandoned Urban Car)
   - 32×16px — more intact than forest variant, dusty, flat tires, cracked windshield
   - Collision radius: 12px

8. Feu de signalisation tombé (Fallen Traffic Light)
   - 8×16px — pole bent, lights dark, one cracked
   - Collision radius: 3px

9. Poubelle/Conteneur (Dumpster)
   - 12×12px — metal container, dented, lid open, rust
   - 2 variants, collision radius: 4px

10. Clôture grillagée déformée (Deformed Chain-Link Fence)
    - 24×16px — bent metal mesh, torn sections, posts leaning
    - Collision radius: 6px

11. Escalier effondré (Collapsed Stairs)
    - 24×24px — concrete stairway leading to nothing, steps crumbling
    - Collision radius: 8px

12. Étagères vides de supermarché (Empty Supermarket Shelves)
    - 24×20px — metal shelving, completely empty — not looted, the products just ceased to exist
    - Collision radius: 8px

13. Cabine téléphonique (Phone Booth)
    - 10×20px — glass panels cracked/missing, phone dangling off hook
    - Collision radius: 4px

14. Boîte aux lettres (Mailbox)
    - 8×12px — leaning, stuffed with yellowed papers nobody will read
    - No collision

15. Graffiti mur (Graffiti Wall Fragment)
    - 24×16px — concrete chunk with spray paint: "NE PAS OUBLIER" in deteriorating letters
    - Collision radius: 6px — narrative prop

16. Affiche publicitaire déchirée (Torn Billboard)
    - 20×28px — rusted frame, advertisement for a product that no longer exists, colors faded
    - Collision radius: 5px
```

---

## BIOME 3 — CARRIÈRE EFFONDRÉE (Collapsed Quarry)

### Contexte narratif
An industrial mine/quarry that collapsed on itself. Unstable tunnels, gigantic rusted machines, exposed ore veins. Vertical biome — ramps, levels, chasms. Claustrophobic despite the isometric view. Sounds echo. Danger comes from everywhere — including below. Glowing Essence crystal veins in the rock walls are the only light.

### Palette Carrière
```
COLLAPSED QUARRY PALETTE (12 colors):
- #3A3030  Dark rock (mine walls, base rock)
- #5A5050  Gray rock (exposed rock, rubble)
- #8A7A6A  Light rock (lit rock, sediment)
- #6A3A28  Red earth (iron ore, rich soil)
- #5A6A7A  Industrial metal (machines, rails, tools)
- #5A2A18  Deep rust (abandoned machines)
- #4ABAE0  Essence crystal blue (glowing crystal veins)
- #7AE0F0  Bright Essence crystal (lit crystals, highlight)
- #6A5038  Mine timber (pit props, support planks)
- #2A2222  Coal (coal zones, soot)
- #B4A080  Ochre dust (suspended dust, sediment)
- #1A1218  Deep shadow (deep tunnels, chasms — darker than black)
```

### Assets à produire — Tiles sol (64×32)
```
QUARRY TILES:
1. Sol roche brute (Raw Rock Floor) — 3 variants
   Dominant dark gray-brown (#3A3030), scattered lighter rock pixels (#5A5050, #8A7A6A)

2. Sol terre rouge (Red Earth) — 2 variants
   Iron-rich earth, reddish-brown dominant (#6A3A28), some rock fragments

3. Rails de mine (Mine Rails) — 2 variants (straight, curved)
   Metal rails (#5A6A7A) on wooden sleepers (#6A5038) over rock floor
```

### Assets à produire — Props
```
QUARRY PROPS:

1. Paroi rocheuse (Rock Wall)
   - 32×48px — imposing mine wall, layered sediment visible, possibly a crystal vein
   - Collision radius: 14px

2. Veine de cristal Essence (Essence Crystal Vein)
   - 16×20px — crystals jutting from rock, glowing blue (#4ABAE0, #7AE0F0)
   - 3 variants (small: 8×10, medium: 16×16, large: 16×20)
   - No collision on small, 4px on medium, 6px on large
   - MUST have internal glow: brighter center pixel, darker edges

3. Wagon de mine renversé (Overturned Mine Cart)
   - 24×14px — wooden cart on its side, rusted wheels, ore spilled
   - Collision radius: 8px

4. Étai en bois (Wooden Pit Prop)
   - 12×24px — vertical timber support beam
   - 2 variants: intact (straight), broken (splintered, sagging)
   - Collision radius: 3px

5. Machine rouillée massive (Massive Rusted Machine)
   - 36×32px — giant extraction machine, gears frozen, rust covering, pipes
   - Collision radius: 14px — major landmark

6. Éboulis / pierres tombées (Rubble / Fallen Rocks)
   - 16×10px — scattered rock chunks, dust cloud suggestion
   - 3 variants, no collision

7. Gouffre sombre (Dark Chasm)
   - 32×16px — hole in ground, pitch black center (#1A1218), crumbling edges
   - Collision radius: 12px (danger zone)

8. Casier d'ouvrier ouvert (Open Worker Locker)
   - 10×16px — metal locker, dented, door ajar, personal effects visible (photo pixel, thermos)
   - No collision — narrative prop

9. Chariot à bras (Hand Cart)
   - 16×12px — wooden wheelbarrow with rusted wheel, empty or with rock chunks
   - Collision radius: 5px

10. Câble métallique tombé (Fallen Cable)
    - 24×6px — coiled metal cable on ground, rust-orange
    - No collision

11. Lampe de mine (Mine Lamp)
    - 6×8px — small lamp on ground or hanging, still faintly orange
    - No collision — atmospheric prop

12. Poutre de soutènement effondrée (Collapsed Support Beam)
    - 28×12px — timber beam fallen diagonally, splintered end
    - Collision radius: 8px

13. Pioche abandonnée (Abandoned Pickaxe)
    - 8×10px — rusted pickaxe stuck in rock or lying on ground
    - No collision — narrative detail

14. Tas de charbon (Coal Pile)
    - 12×8px — black mound, some pieces scattered
    - No collision
```

---

## BIOME 4 — CHAMPS SAUVAGES (Wild Fields)

### Contexte narratif
Open spaces, wind, maximum visibility. Vulnerable but beautiful. Former farmland gone wild — wheat fields grown feral, stone walls crumbling, rusted fences marking boundaries nobody remembers. The wind is visible — tall grass ripples in waves. You can see threats coming from far away, but there's nowhere to hide.

### Palette Champs
```
WILD FIELDS PALETTE (12 colors):
- #3A6A30  Dark grass (shaded grass, dense clumps)
- #5AA848  Vivid grass (main prairie)
- #A8B458  Golden grass (dry grass, wild wheat)
- #C8D480  Pale grass (highlights, sunlit grass)
- #8A7058  Dry earth (dirt paths, bare soil)
- #B8A080  Light earth (lit soil, sand)
- #7A7A6A  Field stone (isolated rocks, low walls)
- #C44A3A  Red flower (poppies, wild red flowers)
- #5A7ACA  Blue flower (cornflowers, lavender)
- #8AB0D0  Sky reflection (puddles, sky reflections)
- #6A5A42  Wood fence (fences, posts, farm vestiges)
- #D0D8D0  Visible wind (wind lines as particles, dust)
```

### Assets à produire — Tiles sol (64×32)
```
WILD FIELDS TILES:
1. Sol prairie herbeuse (Grassy Prairie) — 3 variants
   Dominant vivid green (#5AA848), scattered darker (#3A6A30) and lighter (#C8D480) grass pixels, occasional tiny flower dot

2. Sol terre sèche (Dry Earth) — 2 variants
   Warm brown (#8A7058) dominant, lighter patches, small pebble accents

3. Blé sauvage (Wild Wheat) — 2 variants
   Golden (#A8B458) dominant, tall grass texture, wind direction suggested by pixel pattern
```

### Assets à produire — Props
```
WILD FIELDS PROPS:

1. Arbre solitaire (Solitary Tree)
   - Base: 14×32px — lone tree standing in open field, wind-shaped lean
   - Canopy: 28×24px — wind-swept foliage, asymmetric crown
   - Collision radius: 5px — landmark visible from afar

2. Herbes hautes (Tall Grass / Wheat)
   - 16×16px — swaying tall grass cluster, golden and green mix
   - 3 variants + should look like it moves in wind
   - No collision

3. Coquelicots (Poppies)
   - 8×6px — small cluster of red (#C44A3A) dots on green stems
   - 3 color variants (red, blue #5A7ACA, mixed)
   - No collision

4. Muret de pierre bas (Low Stone Wall)
   - 32×10px — old dry stone wall, partially collapsed
   - 2 variants: mostly intact, crumbled
   - Collision radius: 6px

5. Clôture bois (Wooden Fence)
   - 32×12px — weathered posts and rails, gaps where wood rotted
   - 2 variants: standing, broken
   - Collision radius: 4px

6. Meule de foin décomposée (Decomposed Hay Bale)
   - 12×10px — cylindrical hay bale, dark with rot, mushrooms growing
   - 2 variants, collision radius: 4px

7. Flaque d'eau (Puddle)
   - 16×8px — irregular water puddle reflecting sky blue (#8AB0D0)
   - 2 variants, no collision

8. Épouvantail brisé (Broken Scarecrow)
   - 10×20px — wooden cross with tattered cloth, leaning, eerie
   - Collision radius: 3px — unsettling prop

9. Puits abandonné (Abandoned Well)
   - 14×16px — circular stone well, wooden roof collapsed, dark water below
   - Collision radius: 6px — explorable?

10. Charrue rouillée (Rusted Plow)
    - 20×10px — farm equipment partially buried in earth, orange rust
    - Collision radius: 6px

11. Poteau de clôture isolé (Lone Fence Post)
    - 4×12px — single weathered post, barbed wire remnant
    - No collision

12. Nid d'oiseau au sol (Ground Bird Nest)
    - 6×4px — small nest with eggs, hidden in grass
    - No collision — easter egg detail

13. Pierre levée / Menhir (Standing Stone)
    - 8×20px — tall gray stone, possibly carved symbols barely visible
    - Collision radius: 4px — mysterious, ancient

14. Trace de tracteur (Tractor Remains)
    - 28×16px — rusted tractor skeleton, tires flat, vegetation growing through
    - Collision radius: 10px — major prop
```

---

## BIOME 5 — SANCTUAIRE (Sanctuary — Rare POI)

### Contexte narratif
A place that REFUSES to be erased. A building — church, library, museum, school — that has remained INTACT amid chaos. Not a ruin. Not invaded by nature. Clean, silent, suspended in time. Deeply unsettling — because nothing else in the world is like this. Colors are MORE SATURATED than everywhere else, as if reality is "denser" here. A violent contrast with the outside.

### Palette Sanctuaire
```
SANCTUARY PALETTE (12 colors):
- #C8B898  Light golden stone (intact sanctuary walls)
- #5A4A3A  Dark stone (base, structure shadows)
- #3A7ACE  Stained glass blue (stained glass fragments, reflections)
- #CE3A3A  Stained glass red (stained glass fragments, accents)
- #E0C040  Stained glass gold (stained glass, light)
- #7A5A3A  Polished wood (intact furniture, bookshelves)
- #8A6A4A  Worn book (books, documents, paper)
- #F0E0C0  Interior light (warm light, strongly anchored zones)
- #4A8A4A  Sacred moss (respectful vegetation that coexists)
- #9A8A7A  Tiled floor (period tiling, mosaics)
- #6A4AAE  Guardian aura (sanctuary guardian aura)
- #F0C030  Saturated gold (golden details, symbols, concentrated light)
```

### Assets à produire — Tiles sol (64×32)
```
SANCTUARY TILES:
1. Sol carrelé/mosaïque (Tiled/Mosaic Floor) — 3 variants
   Warm stone (#9A8A7A) with geometric pattern in darker/lighter stone, INTACT and CLEAN
   Unlike all other biomes, these tiles look maintained, perfect, unsettling in their preservation

2. Sol pierre dorée (Golden Stone Floor) — 2 variants
   Warm (#C8B898) dominant, polished appearance, slight golden shimmer pixels
```

### Assets à produire — Props
```
SANCTUARY PROPS:

1. Mur de pierre intact (Intact Stone Wall)
   - 32×32px — clean, well-maintained stone wall, NO CRACKS, no vegetation damage
   - 2 variants — this is disturbing because EVERYTHING else in the world is ruined
   - Collision radius: 12px

2. Vitrail fragment (Stained Glass Fragment)
   - 16×24px — intact stained glass panel in stone frame, colored light (#3A7ACE, #CE3A3A, #E0C040)
   - 3 color variants — each tells a different forgotten story
   - Collision radius: 4px

3. Bibliothèque intacte (Intact Bookshelf)
   - 24×28px — wooden shelving full of books, spines visible in earth tones
   - All books present, organized — impossibly preserved
   - Collision radius: 10px

4. Autel / Piédestal (Altar/Pedestal)
   - 16×16px — stone pedestal, golden (#F0C030) symbols carved, faint glow
   - Collision radius: 6px — central narrative prop

5. Arche d'entrée (Entry Arch)
   - 48×48px — grand stone archway, intact carvings, golden light emanating from within
   - Collision radius: 16px — the threshold between broken and preserved reality

6. Chandelier suspendu (Hanging Chandelier)
   - 16×12px — ornate metal chandelier, candles still burning (???)
   - No collision (rendered above player)

7. Banc en bois poli (Polished Wooden Bench)
   - 20×8px — intact church/library bench, warm wood tones
   - Collision radius: 5px

8. Horloge fonctionnelle (Working Clock)
   - 10×10px — wall clock, STILL TICKING, hands moving
   - No collision — deeply unsettling detail

9. Statue de gardien (Guardian Statue)
   - 16×24px — humanoid stone figure, aura (#6A4AAE), eyes might glow
   - Collision radius: 6px — is it a statue or something else?

10. Tapis intact (Intact Carpet/Rug)
    - 24×12px — woven rug, rich colors still vivid, geometric patterns
    - No collision — floor decoration, surreally pristine
```

---

## PROMPTS PRÊTS À L'EMPLOI

### Template générique (copier-coller et adapter)

```
Create a [CATEGORY] pixel art sprite for an isometric 2D game.

Subject: [DESCRIPTION DÉTAILLÉE]
Size: [WxH] pixels
Biome: [NOM DU BIOME]
Palette: [LISTER 6-8 COULEURS HEX PRINCIPALES]

Style requirements:
- Pure pixel art, every pixel intentional, no anti-aliasing
- Sel-out outlines (tinted, never pure black)
- Light from top-left
- Transparent background
- Isometric perspective (2:1 ratio)
- Must be readable at 480×270 viewport resolution
- Melancholic, post-civilization atmosphere — beautiful but haunting
- [DÉTAILS SPÉCIFIQUES AU BIOME]

The world of VESTIGES wasn't destroyed — it was FORGOTTEN. This asset should convey that sense of fading memory, of things that persist because they were deeply remembered.
```

### Exemple concret — Props Ruines Urbaines

```
Create a prop pixel art sprite for an isometric 2D survival game.

Subject: A collapsed stairway in an urban ruin. Concrete stairs leading up to nothing — the building above has been erased. Steps are crumbling, rebar exposed. Dust and small debris at the base. One step has a child's shoe on it (2-3 pixels, heartbreaking detail). The stairway goes up 3-4 visible steps then breaks off into empty air.

Size: 24×24 pixels
Biome: Urban Ruins
Palette: #4A4A4A (dark concrete), #737373 (medium concrete), #A0A0A0 (light concrete), #6B3A24 (dark rust/rebar), #A85C30 (rust), #4A7A3A (crack vegetation), #3A3535 (shadows)

Style: Pure pixel art, sel-out outlines in dark concrete tones (#3A3535), light from top-left, transparent background. Isometric view. The stairway should feel lonely and purposeless — it goes nowhere because where it went no longer exists. A vestige of daily life frozen in place.
```

### Exemple concret — Tile Carrière

```
Create an isometric floor tile in pixel art for a mining/quarry biome.

Subject: Raw rock floor of a collapsed quarry. Dark stone with visible sediment layers. Small ore fragments scattered. The rock surface is uneven and rough.

Size: 64×32 pixels (isometric diamond shape)
Palette: #3A3030 (dominant dark rock), #5A5050 (gray rock accent), #8A7A6A (light sediment), #6A3A28 (red earth/iron trace), #2A2222 (coal/dark accent)

Style: Pure pixel art. The tile is a perfect isometric diamond (rhombus). Use scattered individual accent pixels on the dominant color — NOT smooth gradients or noise. ~70% of pixels should be the dominant rock color, with random single-pixel accents in the other colors. Must tile seamlessly with variants. Transparent background outside the diamond. Each pixel is intentional.
```

### Exemple concret — Prop Sanctuaire

```
Create a pixel art prop for an isometric 2D game's rare sacred location.

Subject: An intact bookshelf inside a Sanctuary — a place that refuses to be erased. The bookshelf is PERFECT — no dust, no damage, no decay. Wooden shelves full of leather-bound books with visible spines in earth tones. This is deeply unsettling because everything else in the world is ruined. The bookshelf exists because too many people remembered reading here.

Size: 24×28 pixels
Palette: #7A5A3A (polished wood), #8A6A4A (book leather), #5A4A3A (dark wood shadow), #C8B898 (light wood highlight), #F0E0C0 (warm interior light), #CE3A3A (rare red book spine), #3A7ACE (rare blue book spine)

Style: Pure pixel art, sel-out outlines in dark wood tones, light from top-left, transparent background. Isometric perspective. The preservation of this object should feel WRONG — too clean, too intact. Colors are slightly more saturated than they should be, as if reality is denser here. A vestige of knowledge that refused to be forgotten.
```

---

## CHECKLIST QUALITÉ (valider chaque asset)

```
For EVERY generated asset, verify:
□ Readable at 100% zoom (480×270 viewport) — recognizable shape without zooming
□ Readable in silhouette alone (fill with solid color — still identifiable?)
□ Palette respected (no colors outside biome palette + universals)
□ No pillow shading (consistent top-left light source)
□ Sel-out outlines (no pure black #000000 anywhere)
□ Size conforms to specs
□ Fits isometric grid without offset
□ Transparent background
□ PNG export, nearest-neighbor compatible
□ Emotionally resonant — conveys the "forgotten world" atmosphere
□ For tiles: diamond geometry correct (4px start, +4px per row)
□ For tiles: tiles seamlessly with its variants
□ For props: clear base anchor point at bottom center
□ For props: appropriate collision area is visually obvious
```

---

## NOTES FINALES

### Ce qui fait un BON asset VESTIGES
- Il raconte une micro-histoire. Un détail qui fait réfléchir : pourquoi cette chaussure est là ? Pourquoi cette horloge fonctionne encore ?
- Il est beau malgré la désolation. La nature qui reprend ses droits est magnifique, pas triste.
- Il respecte l'échelle. Un humain fait 16×24px. Tout doit être proportionné à ça.
- Il fonctionne en contexte. Isolé il est joli, mais posé sur sa tile de biome il VIT.

### Ce qui fait un MAUVAIS asset VESTIGES
- Trop générique. "Un arbre" sans personnalité. "Un rocher" sans histoire.
- Trop détaillé pour la résolution. Si tu as besoin de zoomer pour voir le détail, il est trop petit.
- Mauvaise palette. Des couleurs hors palette cassent l'harmonie du biome instantanément.
- Smooth gradients ou AA visible. Ça tue le style pixel art.
- Symétrie parfaite. La nature n'est pas symétrique. Les ruines non plus.

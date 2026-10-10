# PROMPT — Illustrations des personnages de VESTIGES

> **Usage :** prompts pour un générateur d'images qui accepte une image jointe (ChatGPT, Gemini, Midjourney avec référence), à copier-coller.
> **Objet :** illustrations des onze sprites de héros : les dix personnages du lore et l'Enfant du Bas-Port, version alternative du Vagabond (Miroir et page Personnages au Hub, fiches, visuels Steam). **Pas les sprites en jeu**, qui restent produits par le pipeline procédural (plan 32).
> **Sources :** sprites `assets/characters/<id>/`, [lore §5](VESTIGES-LORE.md#5-les-personnages-jouables), [plan 32 §3](plans/32-heros.md#3-designs), [charte §4](CHARTE-GRAPHIQUE.md).
> **Version :** 0.3, 10 octobre 2026. Retour de Raphaël sur la v0.1 : rendu trop détaillé ; il faut plus stylisé, un décor, et partir des sprites du jeu.

Prompts en anglais, le reste en français.

---

## 1. Principe

- **Le sprite fait foi.** Chaque prompt part de la planche de référence du personnage (`doc/illustrations/references/ref_<id>.png` : sprite au repos ×12, cinq vues, sur blanc cassé), jointe à la demande. L'illustration redessine ce personnage : même costume, mêmes couleurs, mêmes proportions trapues, même signature, rien d'ajouté.
- **Stylisé, pas réaliste.** Grands aplats, ombrage en deux ou trois valeurs à bord net, silhouette propre, aucun petit détail (pas de couture, de trame, de mèche, de pli).
- **Un décor**, tiré du lieu du lore propre au personnage, construit en quelques plans d'aplats, plus sombre et moins saturé que le personnage, qui s'enfonce dans l'ombre vers les bords.
- **Carré, sombre, serré.** Format 1:1, image basse en valeurs (crépuscule, ombres bleu-violet), personnage qui occupe presque toute la hauteur, visage en partie dans l'ombre.

Si un sprite change, refaire sa planche : `char_<id>_<dir>_idle_01.png` des vues S, SE, E, NE et N, agrandies ×12 en plus proche voisin, côte à côte, pieds alignés, sur fond `#E8E0D4`.

---

## 2. En-tête de style (à coller en tête de CHAQUE prompt)

v0.3, après l'essai du Vagabond : format carré, image sombre, visage en partie caché, cadrage serré, et interdiction de recopier les pixels (le premier essai avait rendu le sprite en cubes).

```
Square 1:1 image.
Stylized 2D character illustration for VESTIGES, a dark, melancholic roguelite set in a small French coastal town that was not destroyed but forgotten.
The attached image is the in-game pixel art sprite of this character, seen from five angles. Use it only as a design reference: same costume, same colors, same proportions, same signature objects, nothing added. Do NOT reproduce the pixels: no blocky, cubic or voxel shapes; translate the design into smooth painted shapes.
Character: chunky game proportions (slightly large head, short sturdy body, oversized signature object). Face partly hidden, in the shadow of the headwear or behind a raised collar or scarf, painted with the same shading as the rest of the body. Eyes are small plain black vertical slits, as in the sprite, placed in the eye sockets and following the turn of the head (only one visible in profile): no iris, no white, no eyelids. Head and body turned toward the viewer, three-quarter front view, full body, idle pose with attitude. Close framing: the character fills most of the frame height, slightly low camera.
Style: graphic and simplified, not realistic. Large flat color shapes, hard-edged cel shading, two or three values per color, crisp silhouette, minimal texture, no fine detail. Matte gouache look.
Light and color: dark, low-key image. Dusk or overcast, deep blue-violet shadows (#1A1A2E, #4A3066), muted desaturated colors. A single warm light from the upper left rims the character; the character's accent color is the only saturated note. Most of the image sits in the darker half of the value range.
Background: a simplified scene of a few flat planes, darker and less saturated than the character, sinking into shadow toward the edges and corners. No sea or ocean anywhere: the sea has been forgotten. No white border, no frame. No text.
```

**À exclure** (champ négatif, `--no` sous Midjourney, ou phrase « Avoid: … » dans ChatGPT) :

```
bright, high-key, pastel, washed out, white background, white border, frame, pixel art, voxel, blocky, cubic shapes, Minecraft, low poly, wide landscape, tiny character, fully lit face, highly detailed, intricate, photorealistic, realistic anatomy, fine texture, detailed wrinkles, individual hair strands, brush splatter, watercolor bleed, 3D render, glossy, anime, manga, chibi, text, letters, logo, watermark, signature, black outline, acid green, moon, stars
```

---

## 3. Blocs par personnage

Joindre `ref_<id>.png`, coller l'en-tête, puis le bloc. Aucun nom propre : seulement la fonction (lore §5).

### Le Vagabond — `ref_vagabond.png`
```
The Wanderer. As in the sprite: brown road hat pulled low so its brim shadows his eyes, short reddish beard, burnt-orange scarf knotted at the neck, long patched brown coat, dark boots, and an enormous tall brown backpack rising above his head, topped with a rolled green blanket and an orange bundle, a tent peg and a tin can sticking out. Accent: burnt orange. Mood: stubborn, quiet, always walking.
Background: an overgrown country road climbing between ruined stone houses, tall wild grass and ivy, the church bell tower far away on the hill, autumn dusk.
```

### Le Traqueur — `ref_traqueur.png`
```
The Tracker. As in the sprite: tall pointed moss-green hood with two pale almond eyes glowing in its shadow, long green coat, beige strap across the chest, a tall wooden bow on his back rising above his head, dark boots. The tallest, thinnest figure, leaning forward. Accent: pale beige on moss green. Mood: nervous, obsessive, unwilling to let go.
Background: a steep winding path at dusk, a closed iron barrier at the top, a line of footprints in the dust that stops abruptly.
```

### La Forgeuse — `ref_forgeuse.png`
```
The Smith. As in the sprite: short and very broad woman, an oversized dark steel forge hammer resting on her shoulder, red-lensed goggles pushed up on top of her head, grey shirt, brown leather apron, thick brown gloves, dark trousers. The widest figure. Accent: forge red. Mood: stubborn, warm, ready to hammer the world straight.
Background: a ruined stone street of the upper town, doors with their locks torn out, a heavy broken chain lying across the cobblestones, warm ember light from an old forge doorway.
```

### L'Éveillée — `ref_eveillee.png`
```
The Awakened. As in the sprite: dark violet hair in a high bun, long pale blue-white dress flaring at the hem as if in a wind that does not exist, an operator headset, a dark red telephone cord looping around her, a cyan glow in her hand. A faint offset echo of her silhouette just behind her. Accent: Essence cyan. Mood: calm, absent, listening to voices nobody else hears.
Background: the ruined switchboard room of the town hall, rows of brass sockets and hanging cords, dust floating in the light of a tall window.
```

### Le Facteur sans destination — `ref_facteur.png`
```
The Postman with no destination. As in the sprite: navy postal cap with a visor, moustache, faded postal-blue uniform, a big mustard-yellow leather satchel overflowing with letters, white envelopes flying off behind him, leaning forward like a skater on makeshift skates. Accent: mustard yellow. Mood: tender, obstinate, cheerful in spite of everything.
Background: a quiet street of the upper town with a ruined post office, overflowing letterboxes, letters scattered by the wind along the pavement.
```

### La Scaphandrière sans mer — `ref_scaphandriere.png`
```
The Diver without a sea. As in the sprite: round copper diving helmet with a dark green porthole, grey-beige canvas diving suit, brown harness and weight belt, an air hose, heavy dark boots. The only round head of the cast. Accent: copper and brass. Mood: silent, methodical, searching for a coastline that no longer exists.
Background: a dried salt marsh where the harbour used to be, an old stone quay bordering nothing, a fishing boat lying on its side among reeds, cracked dry ground.
```

### Le Sonneur — `ref_sonneur.png`
```
The Bell-ringer. As in the sprite: black beret, dark beard, long dark navy coat, a beige bell rope worn across the chest like a bandolier with its frayed cut end hanging low, a bronze bell strapped on his back above his shoulder. Accent: bronze. Mood: grave, patient, waiting for the right hour.
Background: the church belfry at dusk, heavy wooden beams, a great bronze bell, the roofs of the town far below.
```

### L'Écolière — `ref_ecoliere.png`
```
The Schoolgirl. As in the sprite: a nine-year-old girl, the smallest of the cast after the boy, brown hair with pink ribbons, navy school smock, a big red school satchel on her back wider than her shoulders, a stick of pink chalk in her hand. Accent: chalk pink. Mood: patient, hopeful, lonely, brave.
Background: an empty schoolyard in front of an old village school, chalk hopscotch drawings on the ground, a small bell above the door, late afternoon light.
```

### La Photographe — `ref_photographe.png`
```
The Photographer. As in the sprite: short dark hair, belted burgundy coat with the collar up, a camera on her chest with a white strap, a flash gun with a large round silver reflector rising above her shoulder. Accent: silver flash. Mood: determined, guarded, keeping a truth she refused to destroy.
Background: the view from the bell tower over the grey roofs of the upper town, an overcast sky, the white glow of a flash.
```

### La Veilleuse — `ref_veilleuse.png` *(personnage caché, à ne pas diffuser)*
```
The Watcher. As in the sprite: a very old hunched woman, bottle-green wool shawl worn as a rounded hood, black widow's dress, grey apron, a lit oil lamp hanging from her hand, a cane. Accent: bottle green and warm lamplight. Mood: kind, tired, unafraid.
Background: night at the top of a steep path, the last house before an iron barrier, a green front door ajar with warm light inside.
```

### L'Enfant du Bas-Port — `ref_enfant_bas_port.png` *(débloqué après la vraie fin, à ne pas diffuser)*
```
The Child of the Lower Port. As in the sprite: a six-year-old boy in an oversized yellow oilskin coat and sou'wester hat, rubber boots, an orange scarf far too long for him, a small red bobble hat clutched in his hand. Accent: oilskin yellow. Mood: frightened, small, holding on.
Background: the lower port at night in a storm, heavy rain, dark waves breaking over the quay, wet cobblestones reflecting a single lamp. This is the only image where the sea exists: ignore the "no sea" rule.
```

---

## 4. Méthode

1. **Pilote : le Vagabond.** Joindre `ref_vagabond.png`, en-tête + bloc. Plusieurs essais, en garder un.
2. **Corriger par la conversation** plutôt que de relancer à zéro :
   - trop détaillé : `Simplify: fewer and bigger shapes, flatter colors, remove every small detail.`
   - s'écarte du sprite : `Stick to the attached sprite: same colors and proportions, remove anything that is not in it.`
   - personnage noyé dans le décor : `Make the background flatter, lighter and less saturated so the character stands out.`
3. **Verrouiller le style.** L'image retenue devient la seconde pièce jointe des neuf autres : `The second attached image is the style reference: match its rendering, palette treatment and level of simplification exactly; take the character from the first image.`
4. **Contrôle en aplat noir** : chaque silhouette doit se reconnaître seule et ne ressembler à aucune autre.

---

## 5. Checklist par image

- [ ] Fidèle au sprite : costume, couleurs, proportions trapues, signature ; rien d'ajouté.
- [ ] Stylisé : grands aplats, deux ou trois valeurs par couleur, aucun petit détail.
- [ ] Carré, image sombre, personnage qui remplit presque toute la hauteur, visage en partie dans l'ombre.
- [ ] Peint, pas pixelisé : aucune forme en cubes.
- [ ] Décor du lieu du personnage, plus sombre et moins saturé que lui.
- [ ] Une seule couleur saturée, l'accent ; aucun vert-acide.
- [ ] Lumière haut-gauche, cohérente avec les autres images.
- [ ] Aucun texte, nom, logo ni signature ; mains et sangles correctes à 100 %.
- [ ] Veilleuse, Enfant du Bas-Port : jamais en communication.

# Prompt pour génération de tiles isométriques — VESTIGES

> Coller ce prompt dans une nouvelle session Claude pour continuer la production de tiles.

---

## Contexte

Tu travailles sur **VESTIGES**, un roguelike isométrique 2D en Godot 4.6 / C#. Tu dois générer des tiles de sol isométriques en pixel art pour chaque biome du jeu, en utilisant Python + Pillow.

## Spécifications techniques des tiles

- **Taille** : 64×32 pixels (losange isométrique, ratio 2:1)
- **Format** : PNG avec transparence (RGBA)
- **Pointes du losange** : 4 pixels de large en haut (y=0) et en bas (y=31), 64 pixels de large au milieu (y=15 et y=16)
- **Progression** : chaque row ajoute 4 pixels de largeur (2 de chaque côté)
- **Nombre de couleurs par tile** : 5-8 max (style pixel art contraint)
- **Palette** : strictement issue de `doc/CHARTE-GRAPHIQUE.md` — ne jamais inventer de couleurs

## Formule du losange (pixel-perfect, OBLIGATOIRE)

```python
W, H = 64, 32

def is_inside_diamond(x, y):
    """Losange iso pixel-perfect 64x32, 4px aux pointes."""
    if y < 0 or y >= H:
        return False
    if y <= 15:
        half_w = 2 + y * 2  # y=0: 4px, y=15: 32+2=34... non, 2+15*2=32 -> range 0..63
        left = 32 - half_w
        right = 31 + half_w
    else:
        mirror_y = 31 - y
        half_w = 2 + mirror_y * 2
        left = 32 - half_w
        right = 31 + half_w
    return left <= x <= right
```

**Vérification obligatoire** avant de sauvegarder :
- Row 0 : 4 pixels [30-33]
- Row 15 : 64 pixels [0-63]
- Row 16 : 64 pixels [0-63]
- Row 31 : 4 pixels [30-33]
- Total : 1088 pixels opaques

## Méthode de génération

1. **Créer le canvas** : `Image.new("RGBA", (64, 32), (0,0,0,0))`
2. **Remplir le losange** avec la couleur de base du biome (la plus sombre/dominante)
3. **Appliquer des patterns** pixel par pixel avec `random.random()` pour créer de la variation :
   - **Texture de base** : 15-25% des pixels en couleur secondaire
   - **Patches organiques** : zones localisées avec `abs(x-cx)<r and abs(y-cy)<r`
   - **Veines/fissures** : lignes diagonales avec `abs(x - a*y - b) < threshold`
   - **Stripes** : motifs répétitifs avec `(x + y*k) % n < threshold`
   - **Highlights** : 2-5% des pixels en couleur claire (lumière)
   - **Accents** : 0.5-1% en couleur vive (fleurs, cristaux, rouille)
4. **Détails ponctuels** : placer manuellement 3-5 pixels d'accent à des positions fixes
5. **Sauvegarder** : `img.save(path)`

## Structure des fichiers

```
assets/tiles/
├── foret/          tile_foret_sol_base.png, _v2.png, _v3.png      ✅ FAIT
├── ruines/         tile_ruines_sol_base.png, _v2.png, _v3.png     ✅ FAIT
├── marecages/      tile_marecages_sol_base.png, _v2.png, _v3.png  ✅ FAIT
├── carriere/       tile_carriere_sol_base.png, _v2.png, _v3.png   ✅ FAIT
├── champs/         tile_champs_sol_base.png, _v2.png, _v3.png     ✅ FAIT
├── sanctuaire/     tile_sanctuaire_sol_base.png, _v2.png, _v3.png ✅ FAIT
```

## Palettes par biome (extraites de CHARTE-GRAPHIQUE.md)

### Forêt Reconquise
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Lierre sombre | `#1E3A1A` | Base dominante (60-75%) |
| Vert canopée | `#2D5A27` | Texture secondaire (15-20%) |
| Vert mousse | `#4A8C3F` | Patches de mousse (5-10%) |
| Vert clair | `#7BC558` | Highlights lumière (1-2%) |
| Brun terreux | `#7A5C42` | Terre visible |
| Brun tronc | `#4A3728` | Terre sombre |
| Gris béton envahi | `#7A7A70` | Fragments de route |
| Ocre automne | `#C49B3E` | Feuilles tombées (accents) |

### Ruines Urbaines
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Gris béton foncé | `#4A4A4A` | Base dominante |
| Gris béton moyen | `#737373` | Texture secondaire |
| Gris béton clair | `#A0A0A0` | Highlights |
| Rouille foncée | `#6B3A24` | Métal rouillé |
| Rouille orange | `#A85C30` | Rouille récente |
| Cuivre oxydé | `#5A9A8A` | Accents turquoise |
| Brique ternie | `#8A5A42` | Zones brique |
| Bleu peinture | `#5A7A9A` | Peinture écaillée |
| Jaune signalisation | `#C4A830` | Marquages routiers |
| Vert reconquérant | `#4A7A3A` | Herbe dans fissures |
| Bois pourri | `#5A4A38` | Débris bois |
| Verre brisé | `#8AB8C4` | Éclats de verre |

### Marécages
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Eau trouble sombre | `#4A6A5E` | Base dominante |
| Eau trouble claire | `#8AA89A` | Eau éclairée |
| Eau laiteuse | `#B8C8BE` | Reflets |
| Mousse humide | `#3A6A38` | Végétation |
| Tourbe | `#3A2E22` | Sol boueux |
| Bois blanchi | `#9A8E7A` | Souches |
| Lichen jaune-vert | `#8A9A4A` | Lichen |
| Spore verte | `#6ACA5A` | Lueur bio |
| Brume bleu-gris | `#7A8A9A` | Brume |
| Violet profond | `#4A2A5A` | Champignons toxiques |
| Racine sombre | `#2A1E16` | Racines |
| Reflet argenté | `#C4D0D4` | Reflets fantômes |

### Carrière Effondrée
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Roche sombre | `#3A3030` | Base dominante |
| Roche grise | `#5A5050` | Texture secondaire |
| Roche claire | `#8A7A6A` | Highlights |
| Terre rouge | `#6A3A28` | Minerai |
| Métal industriel | `#5A6A7A` | Rails, machines |
| Rouille profonde | `#5A2A18` | Machines |
| Cristal Essence bleu | `#4ABAE0` | Cristaux |
| Cristal Essence vif | `#7AE0F0` | Cristaux highlight |
| Bois de mine | `#6A5038` | Étais |
| Charbon | `#2A2222` | Zones charbon |
| Poussière ocre | `#B4A080` | Poussière |
| Ombre profonde | `#1A1218` | Gouffres |

### Champs Sauvages
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Herbe sombre | `#3A6A30` | Ombre |
| Herbe vive | `#5AA848` | Base dominante |
| Herbe dorée | `#A8B458` | Blé sauvage |
| Herbe pâle | `#C8D480` | Highlights |
| Terre sèche | `#8A7058` | Chemins |
| Terre claire | `#B8A080` | Sol éclairé |
| Pierre de champ | `#7A7A6A` | Rochers |
| Fleur rouge | `#C44A3A` | Coquelicots |
| Fleur bleue | `#5A7ACA` | Bleuets |
| Ciel reflet | `#8AB0D0` | Flaques |
| Clôture bois | `#6A5A42` | Barrières |
| Vent visible | `#D0D8D0` | Particules vent |

### Sanctuaire
| Couleur | Hex | Usage tile |
|---------|-----|-----------|
| Pierre claire dorée | `#C8B898` | Structure principale |
| Pierre sombre | `#5A4A3A` | Ombres |
| Vitrail bleu | `#3A7ACE` | Reflets vitraux |
| Vitrail rouge | `#CE3A3A` | Reflets vitraux |
| Vitrail or | `#E0C040` | Reflets vitraux |
| Bois poli | `#7A5A3A` | Mobilier |
| Livre usé | `#8A6A4A` | Livres |
| Lumière intérieure | `#F0E0C0` | Zones ancrées |
| Mousse sacrée | `#4A8A4A` | Végétation |
| Sol carrelé | `#9A8A7A` | Carrelage (base) |
| Gardien aura | `#6A4AAE` | Aura violette |
| Or saturé | `#F0C030` | Détails dorés |

## Règles de design

- **Chaque variant doit être visuellement distinct** : pas juste du bruit différent, mais des features différentes (v1 = subtil, v2 = feature forte, v3 = mix/autre angle)
- **Garder les bords nets** : le losange doit être propre, pas de pixels orphelins hors du diamond
- **Pas de dégradé lissé** : c'est du pixel art, pas de blending
- **Limiter le contraste** : chaque tile doit être un sol, pas un point d'intérêt. Les variantes doivent pouvoir se placer côte à côte sans qu'une saute aux yeux
- **Ajouter un détail "unique" discret** sur une seule variante (ça crée un motif repérable en boucle)

## Tiles restantes à produire (P1 selon TILES-PROMPTS.md)

Consulter `doc/TILES-PROMPTS.md` pour la liste complète. Les prochaines priorités :
- Tiles de chemin/sentier (toutes directions)
- Tiles d'eau (rivière, mare)
- Tiles de transition entre biomes
- Tiles de fog of war / bord de map

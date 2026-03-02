# VESTIGES — Charte Graphique Pixel Art

> **Version :** 1.0
> **Auteur :** Raphaël Plassart
> **Dernière mise à jour :** 2 mars 2026
> **Docs liés :** Bible Artistique (sections 4, 7, 11), Guide Pixel Art, Asset List

---

## 1. RÉSOLUTION ET GRILLE

### Canvas de jeu
- **Résolution interne :** 480×270 pixels (16:9)
- **Affichage :** Upscale ×4 → 1920×1080 (nearest-neighbor, jamais bilinéaire)
- **Le joueur voit** ~480×270 vrais pixels à l'écran à tout moment

### Grille isométrique
- **Tile de base :** 32×16 pixels (format losange iso standard)
- **Tile détaillé (optionnel) :** 64×32 pixels (pour biomes nécessitant plus de détail)
- **Ratio iso :** 2:1 (pour chaque 2px horizontal, 1px vertical)
- **Hauteur d'un "étage" :** 16px (permet d'empiler des tiles pour la fausse 3D)

### Tailles des sprites

| Catégorie | Bounding box | Notes |
|-----------|-------------|-------|
| **Tiles sol** | 32×16 | Losange iso de base |
| **Tiles avec hauteur** | 32×32 à 32×48 | Sol + mur/élévation |
| **Personnage joueur** | 16×24 ou 24×32 | Doit être lisible sur un tile |
| **Ennemis petits** (Ombre, Charognard) | 12×12 | |
| **Ennemis moyens** (Rôdeur, Sentinelle) | 16×20 | |
| **Ennemis gros** (Brute, Tisseuse) | 24×24 | |
| **Mini-boss** (Colosse) | 48×48 | |
| **Boss** (L'Indicible) | 96×64+ | Dépasse l'écran |
| **Décor petit** (buissons, débris) | 8×8 à 16×16 | |
| **Décor moyen** (arbres, ruines) | 16×24 à 32×48 | |
| **Structures joueur** (murs, pièges) | 32×16 à 32×32 | S'alignent sur la grille |
| **Icônes UI** (items, perks) | 16×16 | |
| **Coffres** | 16×12 | 4 variants de rareté |

---

## 2. PALETTE MASTER

### Philosophie
Chaque biome utilise 12-16 couleurs max (hors effets de lumière Godot). La palette master définit toutes les couleurs autorisées dans le jeu. Les palettes biomes sont des sous-ensembles de la master + 2-3 couleurs exclusives.

### Couleurs universelles (présentes dans TOUS les biomes)

Ces couleurs sont utilisées partout, elles forment le socle commun :

| Nom | Hex | Usage |
|-----|-----|-------|
| **Noir profond** | `#1A1A2E` | Ombres les plus sombres, nuit, contours sel-out sombres |
| **Noir bleuté** | `#16213E` | Nuit, zones effacées, ciel nocturne |
| **Gris chaud foncé** | `#3A3535` | Ombres secondaires, pierres sombres |
| **Gris chaud** | `#6B6161` | Pierre, béton, métal oxydé |
| **Gris clair** | `#9E9494` | Pierre claire, cendres, poussière |
| **Blanc cassé** | `#E8E0D4` | Highlights, zones "ancrées" très éclairées |
| **Blanc effacement** | `#F5F0EB` | Fog of war, bords de map, dissolution |
| **Or Foyer** | `#D4A843` | Lumière du Foyer, particules dorées, score |
| **Orange flamme** | `#E07B39` | Feu, torches, explosion |
| **Rouge sang joueur** | `#C4432B` | Dégâts joueur, HP bas, danger |
| **Vert-acide créatures** | `#7FFF00` | Yeux des créatures, lumière hostile |
| **Cyan Essence** | `#5EC4C4` | Essence, magie, capacités actives |
| **Noir iridescent** | `#2D1B3D` | Fluide des créatures, mort d'ennemi |
| **Violet brume** | `#4A3066` | Brume d'effacement, nuit |

### Couleurs narratives (transitions jour/nuit)

| Phase | Dominante | Couleur clé |
|-------|-----------|-------------|
| **Jour** | Chaud, saturé | Or diffus `#D4A843` via CanvasModulate |
| **Crépuscule** | Désaturation progressive | Bleu-violet `#4A3066` monte |
| **Nuit** | Froid, sombre | Noir bleuté `#16213E` domine, seules les sources de lumière colorent |
| **Aube** | Retour progressif de la chaleur | Or pâle `#D4A843` revient du bord de l'écran |

---

## 3. PALETTES PAR BIOME

### Forêt Reconquise

**Ambiance :** Nature luxuriante qui a dévoré la civilisation. Calme trompeur, lumière filtrée par la canopée.

| Nom | Hex | Usage |
|-----|-----|-------|
| Vert canopée sombre | `#2D5A27` | Feuillages denses, ombres de canopée |
| Vert mousse | `#4A8C3F` | Herbe, mousse, végétation principale |
| Vert clair | `#7BC558` | Feuilles éclairées, nouvelles pousses |
| Vert-jaune | `#A4D65E` | Highlights végétation, lumière filtrée |
| Brun tronc foncé | `#4A3728` | Troncs, racines, bois sombre |
| Brun terreux | `#7A5C42` | Sol, terre, écorce |
| Brun clair | `#A68B6B` | Bois sec, chemins, branches mortes |
| Ocre automne | `#C49B3E` | Feuilles d'automne, champignons, accents chauds |
| Gris béton envahi | `#7A7A70` | Autoroutes fissurées, fondations visibles |
| Lierre sombre | `#1E3A1A` | Lierre sur les murs, sous-bois profond |
| Fleur violette | `#8B6BAE` | Fleurs sauvages (touche de couleur, rare) |
| Fleur jaune | `#E0C84A` | Fleurs sauvages secondaires |

### Ruines Urbaines

**Ambiance :** Centre-ville effondré, intérieurs exposés. Silence oppressant, craquements lointains.

| Nom | Hex | Usage |
|-----|-----|-------|
| Gris béton foncé | `#4A4A4A` | Dalles, fondations, routes |
| Gris béton moyen | `#737373` | Murs, structures debout |
| Gris béton clair | `#A0A0A0` | Béton éclairé, poussière |
| Rouille foncée | `#6B3A24` | Métal rouillé, poutrelles |
| Rouille orange | `#A85C30` | Rouille plus récente, tuyaux, ferraille |
| Cuivre oxydé | `#5A9A8A` | Toits en cuivre, accents turquoise |
| Brique ternie | `#8A5A42` | Murs en brique, cheminées |
| Bleu peinture écaillée | `#5A7A9A` | Restes de peinture murale, portes |
| Jaune signalisation | `#C4A830` | Panneaux, marquages routiers fanés |
| Vert nature reconquérante | `#4A7A3A` | Herbe dans les fissures, arbustes isolés |
| Bois pourri | `#5A4A38` | Meubles, cadres de fenêtres |
| Verre brisé | `#8AB8C4` | Reflets de verre, fenêtres cassées |

### Marécages

**Ambiance :** Réalité particulièrement fragile. Eau laiteuse, brume permanente, reflets trompeurs.

| Nom | Hex | Usage |
|-----|-----|-------|
| Eau trouble claire | `#8AA89A` | Surface d'eau éclairée |
| Eau trouble sombre | `#4A6A5E` | Eau profonde, zones sombres |
| Eau laiteuse | `#B8C8BE` | Reflets, eau peu profonde |
| Mousse humide | `#3A6A38` | Végétation de marécage |
| Tourbe | `#3A2E22` | Sol boueux, terre humide |
| Bois blanchi | `#9A8E7A` | Arbres morts, souches blanchies |
| Lichen jaune-vert | `#8A9A4A` | Lichen sur les pierres, mousse |
| Spore verte | `#6ACA5A` | Particules de spore, lueur biologique |
| Brume bleu-gris | `#7A8A9A` | Brume permanente |
| Violet profond | `#4A2A5A` | Fleurs vénéneuses, champignons toxiques |
| Racine sombre | `#2A1E16` | Racines aériennes, bois submergé |
| Reflet argenté | `#C4D0D4` | Reflets dans l'eau (choses qui ne sont plus là) |

### Carrière Effondrée

**Ambiance :** Mine industrielle écroulée. Claustrophobe, veines de cristaux lumineux.

| Nom | Hex | Usage |
|-----|-----|-------|
| Roche sombre | `#3A3030` | Parois de mine, roche de base |
| Roche grise | `#5A5050` | Roche exposée, éboulis |
| Roche claire | `#8A7A6A` | Roche éclairée, sédiments |
| Terre rouge | `#6A3A28` | Minerai de fer, terre riche |
| Métal industriel | `#5A6A7A` | Machines, rails, outils |
| Rouille profonde | `#5A2A18` | Machines abandonnées |
| Cristal Essence bleu | `#4ABAE0` | Veines de cristaux lumineux |
| Cristal Essence vif | `#7AE0F0` | Cristaux éclairés, highlight |
| Bois de mine | `#6A5038` | Étais, planches de soutènement |
| Charbon | `#2A2222` | Zones de charbon, suie |
| Poussière ocre | `#B4A080` | Poussière en suspension, sédiments |
| Ombre profonde | `#1A1218` | Tunnels profonds, gouffres |

### Champs Sauvages

**Ambiance :** Espaces ouverts, vent, visibilité maximale. Vulnérable mais beau.

| Nom | Hex | Usage |
|-----|-----|-------|
| Herbe sombre | `#3A6A30` | Herbe à l'ombre, touffes denses |
| Herbe vive | `#5AA848` | Prairie principale |
| Herbe dorée | `#A8B458` | Herbe sèche, blé sauvage |
| Herbe pale | `#C8D480` | Highlights, herbe au soleil |
| Terre sèche | `#8A7058` | Chemins de terre, sol nu |
| Terre claire | `#B8A080` | Sol éclairé, sable |
| Pierre de champ | `#7A7A6A` | Rochers isolés, murets |
| Fleur rouge | `#C44A3A` | Coquelicots, fleurs sauvages |
| Fleur bleue | `#5A7ACA` | Bleuets, lavande |
| Ciel reflet | `#8AB0D0` | Flaques, reflets de ciel |
| Clôture bois | `#6A5A42` | Barrières, poteaux, vestiges agricoles |
| Vent visible | `#D0D8D0` | Lignes de vent (particules), poussière |

### Sanctuaire (POI rare)

**Ambiance :** Réalité plus "dense". Couleurs plus saturées, plus vivantes que partout ailleurs.

| Nom | Hex | Usage |
|-----|-----|-------|
| Pierre claire dorée | `#C8B898` | Murs intacts du sanctuaire |
| Pierre sombre | `#5A4A3A` | Base, ombres de la structure |
| Vitrail bleu | `#3A7ACE` | Fragments de vitraux, reflets |
| Vitrail rouge | `#CE3A3A` | Fragments de vitraux, accents |
| Vitrail or | `#E0C040` | Fragments de vitraux, lumière |
| Bois poli | `#7A5A3A` | Mobilier intact, bibliothèques |
| Livre usé | `#8A6A4A` | Livres, documents, papier |
| Lumière intérieure | `#F0E0C0` | Lumière chaude, zones très ancrées |
| Mousse sacrée | `#4A8A4A` | Végétation respectueuse qui coexiste |
| Sol carrelé | `#9A8A7A` | Carrelage d'époque, mosaïques |
| Gardien aura | `#6A4AAE` | Aura du gardien du sanctuaire |
| Or saturé | `#F0C030` | Détails dorés, symboles, lumière concentrée |

---

## 4. PALETTE DES ENTITÉS

### Personnages joueurs — couleurs identitaires

Chaque personnage a 2-3 couleurs identitaires qui le rendent reconnaissable instantanément, même à distance.

| Personnage | Couleur dominante | Accent | Hex dominant | Hex accent |
|-----------|------------------|--------|-------------|------------|
| **Le Vagabond** | Brun terreux | Orange outil | `#7A5C42` | `#D4853A` |
| **La Forgeuse** | Gris acier | Rouge forge | `#6A6A7A` | `#C44A3A` |
| **Le Traqueur** | Vert forêt | Beige clair | `#3A5A30` | `#C4B490` |
| **L'Éveillée** | Blanc-bleu | Cyan Essence | `#B8C0D0` | `#5EC4C4` |
| **Le Colosse** | Brun foncé | Métal sombre | `#4A3A2A` | `#5A6A7A` |
| **L'Ombre** | Noir-violet | Pas d'accent (discrétion) | `#2A1A2E` | — |
| **???** | ??? | ??? | — | — |

### Créatures — palette commune

Toutes les créatures partagent ces règles chromatiques :

| Élément | Couleur | Hex | Règle |
|---------|---------|-----|-------|
| **Yeux** | Vert-acide | `#7FFF00` | TOUJOURS cette couleur exacte, 1-2 pixels par œil |
| **Chair/corps** | Variable | — | Mélange organique-minéral, désaturé, tiré du biome d'origine |
| **Fluide** | Noir iridescent | `#2D1B3D` + highlight `#5A3A7A` | 2-3 pixels sombres + 1 pixel violet pour l'iridescence |
| **Contour** | Sel-out du biome | — | Jamais noir pur, toujours teinté de la couleur du biome |

---

## 5. RÈGLES DE CONTOUR ET OMBRAGE

### Sel-out (contour coloré)
- **Jamais** de contour noir pur `#000000`
- Le contour est **plus sombre** que la surface adjacente, mais **teinté** de sa couleur
- Exemple : un sprite sur fond de forêt → contour vert très sombre `#1E3A1A`, pas noir
- Les contours intérieurs (séparations de zones) sont **plus clairs** que les contours extérieurs

### Direction de la lumière
- **Source principale :** Haut-gauche (cohérent avec l'isométrie standard)
- **Conséquence :** Highlights en haut-gauche des volumes, ombres en bas-droite
- **La nuit :** La lumière vient du Foyer (source ponctuelle) → les ombres rayonnent depuis le Foyer. Géré par Godot PointLight2D, pas par les sprites.

### Anti-aliasing manuel
- Uniquement sur les courbes et diagonales qui paraissent trop crénelées
- 1 pixel intermédiaire maximum (pas 2)
- Jamais d'AA sur les lignes horizontales ou verticales

### Dithering
- **Usage :** Transitions entre deux zones de couleur, ombres graduelles, effets d'effacement
- **Pattern :** Checkerboard classique (1 pixel sur 2) ou pattern 2×2
- **Dithering narratif :** Aux bords de map et pour le fog of war, le dithering EST l'effacement — les pixels se raréfient progressivement
- **Ne PAS ditherer** les surfaces planes unies (pillow shading interdit)

---

## 6. CONVENTIONS D'ANIMATION

### Frames par animation

| Animation | Frames | Notes |
|-----------|--------|-------|
| Idle | 2-4 | Sub-pixel shift suffit pour un idle vivant |
| Walk | 4 | 4 directions iso |
| Dash | 2-3 | Rapide, squash & stretch |
| Hurt | 2 | Flash blanc frame 1, pose de recul frame 2 |
| Death joueur | 4 | + effet dissolution Godot |
| Attack ennemi | 3-4 | Anticipation → frappe → recovery |
| Death ennemi | 3-4 | + GPUParticles2D pour compléter la dissolution |

### Directions
- **Personnages joueurs :** 4 directions (haut-droite, bas-droite, bas-gauche, haut-gauche)
- **Ennemis moyens/gros :** 4 directions
- **Ennemis petits** (Ombre, Charognard) : 2 directions (droite, gauche) + flip si acceptable
- **Décor animé** (feu, eau, végétation) : 1 direction, 2-4 frames

### Principes d'animation pixel art
- **Chaque frame est une pose clé.** Pas de tweening — chaque frame doit être lisible isolée
- **Sub-pixel shifting** pour les mouvements subtils (idle, respiration)
- **Squash & stretch** de 1-2 pixels pour le dynamisme (dash, attaque, saut)
- **Smear frames** (1 frame intermédiaire floue/étirée) pour les mouvements rapides
- **Flash blanc** (1 frame, sprite entier blanc) pour le hit confirm

---

## 7. CONVENTIONS DE NOMMAGE DES FICHIERS

### Sprites
```
[categorie]_[nom]_[direction]_[animation]_[frame].png
```

Exemples :
```
char_vagabond_SE_walk_01.png
char_vagabond_SE_walk_02.png
enemy_rodeur_SW_attack_01.png
tile_foret_sol_base.png
tile_foret_sol_effiloche.png
decor_foret_arbre_01.png
decor_foret_arbre_01_detruit.png
item_epee_bois.png
ui_icon_perk_vampirisme.png
```

### Directions iso
- `NE` = haut-droite (Nord-Est)
- `SE` = bas-droite (Sud-Est)
- `SW` = bas-gauche (Sud-Ouest)
- `NW` = haut-gauche (Nord-Ouest)

### Spritesheets
Pour l'export vers Godot, chaque entité animée est exportée en spritesheet horizontale :
```
char_vagabond_SE_walk_sheet.png  (4 frames côte à côte)
```

---

## 8. EXPORT ET INTÉGRATION GODOT

### Paramètres Godot 4

**Project Settings :**
- `display/window/size/viewport_width` = 480
- `display/window/size/viewport_height` = 270
- `display/window/size/window_width_override` = 1920
- `display/window/size/window_height_override` = 1080
- `display/window/stretch/mode` = "viewport"
- `display/window/stretch/aspect` = "keep"
- `rendering/textures/canvas_textures/default_texture_filter` = "Nearest"

**Import de sprites :**
- Filter = OFF (Nearest)
- Compression = Lossless
- Mipmaps = OFF

### Lumière Godot (ne pas résoudre dans les sprites)
- Les sprites sont dessinés **sans ombrage dynamique** — c'est Godot qui gère la lumière via PointLight2D et CanvasModulate
- Les sprites ont un ombrage "de forme" (volumes de base) mais pas d'ombre projetée
- Le cycle jour/nuit est géré par CanvasModulate, pas par des variants de sprites

---

## 9. CHECKLIST QUALITÉ PAR SPRITE

Avant de valider un sprite, vérifier :

- [ ] Lisible à 100% de zoom (480×270) — forme reconnaissable sans zoomer
- [ ] Lisible en silhouette seule (remplir le sprite d'une couleur unie, est-ce que c'est identifiable ?)
- [ ] Palette respectée (aucune couleur hors palette du biome + universelles)
- [ ] Pas de pillow shading (source de lumière cohérente haut-gauche)
- [ ] Contour en sel-out (pas de noir pur #000000)
- [ ] Taille conforme aux specs (section 1)
- [ ] S'intègre dans la grille iso sans décalage
- [ ] État "ancré" et "effiloché" (si c'est un tile)
- [ ] Nommage de fichier conforme (section 7)
- [ ] Export en PNG transparence, nearest-neighbor

---

> **Ce document est la loi chromatique de VESTIGES.** Tout sprite, tout tile, tout pixel doit respecter ces palettes et ces conventions. Si un asset ne passe pas la checklist, il retourne en production.

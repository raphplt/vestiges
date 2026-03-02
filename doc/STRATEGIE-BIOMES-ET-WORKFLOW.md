# VESTIGES — Stratégie Biomes Isométriques & Workflow de Production

> **Version :** 1.0
> **Dernière mise à jour :** 2 mars 2026
> **Docs liés :** Charte Graphique, Asset List, Guide Pixel Art, Bible (section 5)

---

## PARTIE I — BIOMES ISOMÉTRIQUES : COMMENT FAIRE DE LA FAUSSE 3D

### Le problème

Des tiles iso 2D plates donnent un résultat "jeu Flash 2005". Pour VESTIGES, on veut un monde qui a de la profondeur, du volume, de l'atmosphère — pas juste un sol avec des sprites posés dessus.

### La solution : le Layered Isometric

L'idée : au lieu d'un seul plan de tiles, on empile **plusieurs couches** qui se superposent pour créer une illusion de profondeur 3D. Combiné avec la lumière dynamique Godot et les effets de particules, le résultat est visuellement riche malgré des sprites simples.

---

### 1. Architecture en couches (le cœur du système)

Chaque "cellule" du monde est composée de couches empilées :

```
Couche 5 — CANOPÉE / PLAFOND (optionnel)
            Feuillage au-dessus du joueur, semi-transparent
            Le joueur passe DESSOUS

Couche 4 — DÉCOR HAUT
            Cimes d'arbres, toits de bâtiments, piliers hauts
            Occluent partiellement la vue

Couche 3 — ENTITÉS
            Joueur, ennemis, NPCs, coffres
            Même Z-sort que le décor moyen

Couche 2 — DÉCOR MOYEN (le gros du travail visuel)
            Arbres (troncs), murs, rochers, meubles
            Sprites avec "pied" au sol et hauteur variable
            C'est ICI que la fausse 3D opère

Couche 1 — SOL + DÉTAILS AU SOL
            Tiles iso de base (32×16)
            + herbe, flaques, fissures, rails, débris plats

Couche 0 — SOUS-SOL / OMBRES
            Ombres portées (sprites sombres semi-transparents)
            Trous, gouffres, eau sous la surface
```

**Dans Godot 4 :** Utiliser des TileMapLayer séparés pour chaque couche, ou un système de YSort (CanvasItem → y_sort_enabled) pour le tri automatique des entités et du décor.

### 2. Les tiles avec hauteur : le secret de la fausse 3D

Un tile "plat" fait 32×16. Mais un tile avec hauteur (mur, falaise, bloc de roche) fait 32×32, 32×48 ou plus. La partie basse du sprite est le "pied" qui se pose sur la grille, et le reste monte visuellement.

```
┌──────────────┐
│   FACE HAUTE │ ← Sommet visible (éclairé)
│              │
│   FACE CÔTÉ  │ ← Flanc visible (ombré)
│              │
└──────┬───────┘
       │ Tile de base 32×16 (pied au sol, invisible sous le sprite)
```

**Règles :**
- Chaque tile surélevé a **2 faces visibles** en iso : le dessus et un côté (généralement bas-droite)
- Le dessus est plus clair (source de lumière haut-gauche)
- Le côté est plus sombre (ombre)
- La hauteur se mesure en multiples de **8px** (demi-tile) pour rester cohérent

**Exemples concrets :**
- Sol plat : 32×16
- Trottoir surélevé : 32×20 (4px de hauteur)
- Mur bas / muret : 32×24 (8px de hauteur = 1 demi-étage)
- Mur standard : 32×32 (16px = 1 étage complet)
- Falaise / bâtiment : 32×48 (32px = 2 étages)

### 3. Les éléments de décor volumétriques

C'est le décor moyen (couche 2) qui donne 90% de l'impression de 3D. Ces sprites ne sont PAS des tiles — ce sont des objets individuels posés SUR les tiles, avec un Y-sort correct.

**Technique pour un arbre :**
```
        🌿🌿🌿          ← Feuillage (cluster de pixels verts, irrégulier)
       🌿🌿🌿🌿        ← Variations de teinte pour le volume
        🌿🌿🌿
          ║║             ← Tronc (2-3px de large, brun, ombre à droite)
          ║║
    ──────╨╨──────       ← Le "pied" du sprite s'aligne sur la grille iso
```

Le sprite fait peut-être 16×32 pixels, mais visuellement il "habite" l'espace au-dessus du tile de sol. Le YSort de Godot s'occupe de trier : si le joueur est "devant" l'arbre (Y plus grand), l'arbre est derrière. Si le joueur est "derrière", l'arbre est devant.

**Technique pour des ruines :**
Les murs de bâtiments cassés sont les meilleurs créateurs de profondeur. Un mur qui fait 32×48 avec une face avant, un dessus cassé irrégulier, et une face latérale crée instantanément un espace architectural 3D.

### 4. Overlap et occultation

**L'overlap est ton ami.** Les éléments se chevauchent visuellement :
- Des feuilles de canopée passent DEVANT le joueur quand il est sous un arbre (couche 5, semi-transparente)
- Les murs cachent partiellement ce qui est derrière eux
- Les rochers, la végétation, les débris créent des plans de profondeur

**Dans Godot :** Utiliser `z_index` pour les couches fixes et `y_sort_enabled` pour le tri dynamique entités/décor.

### 5. Profondeur par la couleur (perspective atmosphérique pixel art)

Même en pixel art, on peut utiliser la perspective atmosphérique :
- **Premier plan (proche du joueur) :** Couleurs vives, contrastes forts, détails nets
- **Plan moyen :** Légèrement désaturé, contrastes réduits
- **Arrière-plan (bords de map) :** Très désaturé, tend vers le bleu-gris, moins de détails

Pour VESTIGES c'est doublement pertinent : les bords de la carte sont littéralement "en train de s'effacer", donc la désaturation progressive est narrativement justifiée.

**Implémentation :** Un shader CanvasItem qui désature progressivement les sprites en fonction de leur distance au Foyer.

### 6. Les sols intéressants

Le sol ne doit pas être un aplat monotone. Techniques :

- **Variation de tiles :** 3 variants minimum du même type de sol (randomisés). Même palette, légers changements de texture (un pixel déplacé, une tache différente)
- **Tiles de détail au sol** (couche 1) : herbe éparse, petits cailloux, fissures, racines, feuilles mortes, flaques d'eau. Posés aléatoirement PAR-DESSUS le tile de base
- **Transitions de sol :** Tiles de bord entre deux types de sol (herbe→béton, terre→eau). Utiliser le dithering entre les deux couleurs ou des tiles de bordure dédiés
- **Auto-tiling Godot 4 :** Configurer les terrains dans le TileMap pour que les transitions se fassent automatiquement

---

## PARTIE II — STRATÉGIE PAR BIOME

### Forêt Reconquise — Comment la rendre vivante

**L'illusion 3D ici = la canopée multi-couche**

```
Architecture verticale :
- Couche 5 : Canopée dense (grandes taches de vert, semi-transparent, filtre la lumière)
- Couche 4 : Cimes d'arbres (feuillage haut)
- Couche 2 : Troncs d'arbres + murs envahis de lierre
- Couche 1 : Sol herbe/mousse + feuilles mortes + racines
- Couche 0 : Ombres des arbres (formes irrégulières, vert très sombre)
```

**Éléments clés :**
- Les troncs d'arbres sont fins (2-4px) mais montent haut (24-32px), créant des "colonnes" naturelles
- La végétation envahit les structures artificielles : lierre sur les murs (quelques pixels verts ajoutés sur le gris du béton), herbe dans les fissures
- Les voitures abandonnées couvertes de mousse donnent un excellent contraste organique/artificiel
- Lumière filtrée : les PointLight2D à travers la canopée créent des taches de lumière au sol

**Prompt IA (pour Nano Banana) :**
> "isometric pixel art forest tile 32x16, post-apocalyptic nature reclaimed, lush green moss and vines covering cracked asphalt, warm golden light filtering through canopy, palette restricted to 12 colors, game asset"

### Ruines Urbaines — Comment la rendre oppressante

**L'illusion 3D ici = les murs brisés à différentes hauteurs**

```
Architecture verticale :
- Couche 4 : Poutrelles qui dépassent des murs écroulés
- Couche 2 : Murs de béton à hauteurs variables (16px, 32px, 48px)
                 Intérieurs exposés (meubles visibles "à l'intérieur")
                 Fenêtres cassées (trous dans les murs = profondeur)
- Couche 1 : Sol béton fissuré + débris + poussière
- Couche 0 : Ombres dures des bâtiments (formes géométriques)
```

**Éléments clés :**
- Les murs à différentes hauteurs créent un skyline urbain irrégulier
- Les "intérieurs exposés" sont un hack génial : un mur cassé qui révèle un sol carrelé, un meuble, une étagère à l'intérieur → ça donne l'impression de vraies pièces en 3D
- Les fenêtres cassées sont des trous noirs dans les murs (2-3 pixels noirs) qui donnent de la profondeur
- La poussière en particules (Godot GPUParticles2D) dans les rayons de lumière qui percent entre les bâtiments

### Marécages — Comment les rendre inquiétants

**L'illusion 3D ici = l'eau + la brume sur plusieurs niveaux**

```
Architecture verticale :
- Couche 5 : Brume haute (particules, semi-transparent)
- Couche 2 : Arbres morts blanchis (fins, hauts, fantomatiques)
                 Racines aériennes (traversent plusieurs couches)
- Couche 1 : Sol boueux + nénuphars + mousse humide
- Couche 0.5 : Surface de l'eau (semi-transparent, animation lente)
- Couche 0 : Fond de l'eau (sombre, flou → les reflets sont DESSUS l'eau)
```

**Éléments clés :**
- L'eau est LA star. Elle est semi-transparente : on voit le fond (sombre) à travers la surface (plus claire). 2 couches de tiles superposées avec alpha
- Les reflets dans l'eau montrent "des choses qui ne sont plus là" → c'est narratif ET visuellement intrigant. Quelques pixels de couleur inhabituelle dans l'eau, comme un reflet de bâtiment qui n'existe pas
- La brume (particules Godot) à mi-hauteur masque partiellement les bases des arbres, créant de la profondeur
- Les racines aériennes traversent l'espace entre le sol et la canopée

### Carrière Effondrée — Comment la rendre claustrophobe

**L'illusion 3D ici = les parois rocheuses hautes + l'éclairage limité**

```
Architecture verticale :
- Couche 4 : Plafond de roche (dans les tunnels, opaque, masque le dessus)
- Couche 2 : Parois rocheuses HAUTES (48-64px) des deux côtés
                 Étais en bois (cadre de soutènement)
                 Machines rouillées (gros sprites)
- Couche 1 : Sol roche + rails + gravas + charbon
- Couche 0 : Ombres TRÈS sombres (la Carrière est le biome le plus sombre de jour)
- Couche spéciale : Cristaux d'Essence (PointLight2D cyan intégré)
```

**Éléments clés :**
- Les parois hautes des deux côtés créent un "couloir" naturel
- Les cristaux d'Essence sont les seules sources de lumière colorée → le cyan sur fond de roche grise-brune est saisissant
- Les machines rouillées (sprites 32×32) sont des "monuments" qui occupent l'espace
- Le plafond (couche 4 opaque) dans certaines zones crée un vrai effet de tunnel : le joueur passe SOUS la roche

### Champs Sauvages — Comment les rendre mélancoliques

**L'illusion 3D ici = horizon dégagé + herbe animée au vent**

```
Architecture verticale (plus plat) :
- Couche 2 : Arbre solitaire (rare, donc impactant)
                 Clôtures, poteaux, épouvantails
- Couche 1.5 : Herbes hautes (graminées, blé sauvage) — animation vent
- Couche 1 : Sol prairie + fleurs + terre + flaques
- Couche 0 : Ombres douces (nuages qui passent → ombre mobile)
```

**Éléments clés :**
- C'est le biome le plus PLAT, et c'est voulu. L'absence de couvert est un choix de gameplay (vulnérabilité) et de mood (solitude, espace)
- L'herbe animée (2-3 frames, oscillation latérale de 1px) donne une sensation de vent et de vie
- Les rares éléments verticaux (un arbre solitaire, un poteau) sont d'autant plus remarquables
- La profondeur vient de la perspective atmosphérique : les champs au loin sont plus pâles, plus dorés

---

## PARTIE III — WORKFLOW DE PRODUCTION : NANO BANANA → ASEPRITE → GODOT

### Vue d'ensemble du pipeline

```
ÉTAPE 1 — CONCEPT (IA)
    Nano Banana / outil IA génère des concepts
    ↓ Images HD en quelques secondes

ÉTAPE 2 — RÉFÉRENCE & ÉTUDE
    Utiliser le concept comme référence visuelle
    PAS comme asset final
    ↓ Comprendre les formes, les couleurs, la composition

ÉTAPE 3 — TRADUCTION PIXEL ART (Aseprite)
    Recréer l'asset à la main en pixel art
    Respecter la palette de la Charte Graphique
    Respecter les tailles de la spec
    ↓ Sprite pixel art propre

ÉTAPE 4 — ANIMATION (Aseprite)
    Animer frame par frame
    ↓ Spritesheet exportée

ÉTAPE 5 — INTÉGRATION (Godot 4)
    Import, configuration, mise en scène
    ↓ Asset fonctionnel en jeu
```

### ÉTAPE 1 — Génération IA (Nano Banana ou alternatives)

**Outils recommandés :**
- **Nano Banana** (nano-banana.com) — Gratuit, rapide, orienté concepts
- **Leonardo.ai** — Free tier généreux, bon pour les concepts de jeu
- **Stable Diffusion (local)** — Gratuit, illimité, nécessite un GPU correct
- **Bing Image Creator** (DALL-E 3) — Gratuit, qualité excellente pour les concepts
- **Pixellab.ai** — Spécialisé pixel art, génère directement en pixel style

**Stratégie de prompting pour VESTIGES :**

Les prompts doivent être précis. Structure recommandée :

```
[style] + [sujet] + [détails VESTIGES] + [contraintes techniques]
```

**Exemples de prompts par catégorie :**

**Pour les tiles/environnements :**
```
"isometric pixel art tile, 32x16 diamond shape, [biome description],
post-apocalyptic nature reclaimed world, muted earthy colors with
green overgrowth, [specific details], game asset, transparent background"
```

**Pour les personnages :**
```
"pixel art character sprite, isometric view, [character description from Bible],
16x24 pixels, limited color palette, dark fantasy survival style,
post-apocalyptic wanderer, clear silhouette, game sprite"
```

**Pour les créatures :**
```
"pixel art creature sprite, isometric view, [creature description from Bible],
asymmetric organic-mineral hybrid, glowing green acid eyes,
no blood, dark fluid, eldritch but not demonic, game enemy sprite"
```

**Pour les structures :**
```
"pixel art isometric building/structure, [description],
ruined and overgrown with nature, cracked concrete and rust,
warm golden light from a nearby fire, game asset"
```

**IMPORTANT : L'IA ne produit PAS l'asset final.** Elle produit un concept/référence que tu retranscris ensuite en pixel art dans Aseprite. Les raisons :
1. L'IA ne respecte pas les contraintes de palette exactes
2. L'IA ne produit pas en taille pixel-parfaite (32×16, 16×24, etc.)
3. L'IA ne gère pas la transparence pour les sprites
4. L'IA ne produit pas d'animations
5. La cohérence entre assets générés n'est jamais parfaite

### ÉTAPE 2 — De la référence IA à la compréhension des formes

**Workflow concret :**

1. Génère 3-5 variations avec l'IA pour le même asset
2. Ouvre-les côte à côte (ou dans PureRef si tu veux un moodboard)
3. Identifie les éléments récurrents que tu aimes :
   - Quelles formes principales ?
   - Quelles proportions ?
   - Quels détails narratifs ?
   - Quel mood/ambiance ?
4. Esquisse mentalement (ou griffonne vite) la version pixel art simplifiée
5. Passe à Aseprite

### ÉTAPE 3 — Traduction en pixel art (Aseprite)

**Processus par type d'asset :**

#### Tiles isométriques

1. **Nouveau fichier :** 32×16 pixels, transparent
2. **Grille iso :** Dessine le losange de base (diagonales 2:1)
3. **Remplissage :** Couleur de base de la palette biome
4. **Volumes :** Ajoute les ombres (source haut-gauche) et highlights
5. **Détails :** 2-3 pixels de variation de texture (caillou, touffe d'herbe, fissure)
6. **Sel-out :** Contour coloré (pas noir pur)
7. **Test :** Duplique le tile en grille 3×3 pour vérifier que le tiling fonctionne sans couture visible
8. **Variants :** Crée 2 variants en déplaçant/modifiant quelques pixels de détail

#### Sprites personnage/ennemi

1. **Nouveau fichier :** Taille spécifiée dans l'Asset List
2. **Silhouette d'abord :** Remplis la forme en couleur unie. Est-ce reconnaissable ?
3. **Aplats de couleur :** 3-4 zones de couleur principales
4. **Ombres :** Assombrir les zones éloignées de la lumière (haut-gauche)
5. **Highlights :** 1-2 pixels clairs sur les arêtes exposées
6. **Détails identitaires :** Les 2-3 éléments qui rendent CE personnage unique
7. **Yeux :** Pour les créatures, poser les pixels vert-acide `#7FFF00`
8. **Sel-out :** Contour teinté
9. **Test silhouette :** Remplis d'une couleur unie → toujours reconnaissable ?

#### Éléments de décor volumétriques

1. **Visualise en 3D mentalement :** Un arbre a un tronc cylindrique et un feuillage sphérique
2. **Dessine la base iso :** Le "pied" du sprite qui touche le sol
3. **Monte progressivement :** Le tronc/mur/roche monte pixel par pixel
4. **Face éclairée vs face ombrée :** Toujours 2 tons minimum par surface
5. **Sommet :** Irrégulier (nature) ou géométrique (architecture)
6. **Ombre portée :** Sprite séparé, noir à 30-50% opacité, décalé bas-droite

### ÉTAPE 4 — Animation dans Aseprite

1. **Créer les frames :** Via le timeline Aseprite (Frame > New Frame)
2. **Onion skin ON :** Pour voir les frames précédentes/suivantes
3. **Poses clés d'abord :** Frame 1 (début), frame 3 (extrême), frame 5 (retour)
4. **Intervalles ensuite :** Remplir les frames entre les poses clés
5. **Loop test :** Jouer l'animation en boucle (Enter ou Play)
6. **Sub-pixel shifting :** Déplacer des éléments de 1 pixel entre frames pour le mouvement subtil
7. **Export spritesheet :** File > Export Sprite Sheet (horizontal, padding 0)

### ÉTAPE 5 — Intégration Godot 4

**Import du sprite/spritesheet :**
1. Glisser le .png dans le FileSystem de Godot
2. Sélectionner le fichier → Inspector → Import :
   - Filter: `Nearest`
   - Mipmaps: `Disabled`
   - Compress: `Lossless`
3. Cliquer "Reimport"

**Pour un sprite animé (personnage/ennemi) :**
1. Créer un `AnimatedSprite2D` (ou `Sprite2D` + `AnimationPlayer`)
2. Configurer le SpriteFrames avec les spritesheets exportées
3. Nommer les animations : `idle_SE`, `walk_SE`, `attack_SE`, etc.

**Pour les tiles (TileMap) :**
1. Créer un `TileMapLayer` par couche (sol, décor, canopée)
2. Importer le tileset (atlas ou tiles individuels)
3. Configurer les terrains (auto-tiling) pour les transitions
4. Taille de cellule : 32×16 (iso)
5. TileMap > Tile Shape : "Isometric"

**Pour la lumière :**
1. `CanvasModulate` sur la scène principale pour le cycle jour/nuit
2. `PointLight2D` sur chaque source de lumière (Foyer, torches, cristaux)
3. `LightOccluder2D` sur les murs/structures pour bloquer la lumière
4. Les sprites n'ont PAS besoin d'ombrage dynamique intégré — Godot le fait

---

## PARTIE IV — PLAN DE PRODUCTION RÉALISTE

### Phase 1 : Proof of Concept (Semaines 1-2)

**Objectif :** Un écran iso jouable avec le Vagabond qui se déplace dans un biome Forêt.

| Jour | Tâche |
|------|-------|
| 1-2 | Générer 20+ concepts IA pour la Forêt Reconquise. Choisir les meilleurs. |
| 3-4 | Dessiner 5 tiles de sol Forêt (32×16) + 3 tiles avec hauteur (murs, rochers) |
| 5-6 | Dessiner le Vagabond — idle + walk (4 directions) |
| 7-8 | Dessiner 2-3 éléments de décor (arbre, buisson, rocher) |
| 9-10 | Assembler dans Godot : TileMap + personnage qui se déplace + 1 PointLight2D |
| 11-12 | Itérer : ajuster les couleurs, le Y-sort, la sensation de profondeur |
| 13-14 | Ajouter le Foyer (basique) + transition lumière jour/nuit (CanvasModulate) |

### Phase 2 : Core Loop Visuel (Semaines 3-4)

**Objectif :** Combat jouable avec 2-3 ennemis, effets de base.

| Jour | Tâche |
|------|-------|
| 1-3 | Dessiner 3 ennemis (Rôdeur, Charognard, Ombre) — idle + walk + attack + death |
| 4-5 | Effets : flash de hit, orbes XP, impact, dissolution basique |
| 6-7 | Armes de base (épée + arc) — icônes + effets d'auto-attaque |
| 8-9 | HUD : barre de vie, XP, timer, score |
| 10-12 | Structures : mur bois, barricade, torche, feu de camp |
| 13-14 | Tests gameplay : est-ce que c'est FUN avec ces assets ? |

### Phase 3+ : Contenu (au rythme de la roadmap)

Suivre l'Asset List et les priorités P0 → P1 → P2 → P3 → P4.

---

## PARTIE V — TECHNIQUES AVANCÉES POUR BIOMES RICHES

### 1. Tiles modulaires (le système LEGO)

Au lieu de dessiner des tiles complets pour chaque situation, décompose :

- **Tile sol** (32×16) — le sol nu
- **Overlay détail** (32×16, transparent) — herbe, fissures, feuilles, débris
- **Objet posé** (taille variable) — arbre, rocher, meuble, voiture
- **Overlay haut** (32×16, transparent) — canopée, brume, ombres

En combinant aléatoirement ces couches, tu obtiens une variété quasi infinie avec un nombre d'assets limité. Un sol forêt + 3 overlays de détails + 5 objets posés = 15 combinaisons visuelles.

### 2. Shader d'effacement progressif

Un shader CanvasItem uniforme pour tout le jeu :

```
Paramètre : dissolution (0.0 = intact → 1.0 = effacé)
- À 0.0-0.3 : sprite normal
- À 0.3-0.6 : dithering progressif (pixels commencent à disparaître)
- À 0.6-0.9 : la plupart des pixels sont transparents, reste la silhouette
- À 0.9-1.0 : complètement transparent
```

Utilisé pour : bords de map (dissolution spatiale), fog of war (dissolution temporelle), mort d'ennemis (dissolution rapide), mort du joueur (dissolution depuis les bords de l'écran).

### 3. Parallax léger

Même en isométrique, un léger parallax entre les couches donne de la vie :

- Couche 5 (canopée) : bouge de 0.5px quand la caméra bouge de 1px
- Couche 0 (ombres) : bouge de 1.2px quand la caméra bouge de 1px

L'écart est minime mais le cerveau perçoit la profondeur. Attention : garder les valeurs très faibles en pixel art (< 0.5px d'écart) sinon ça détruit la lisibilité.

### 4. Palette shifting dynamique

Un shader qui modifie la palette en temps réel selon l'heure :
- **Jour :** Palette normale
- **Crépuscule :** Shift vers le bleu-violet (mélanger 30% de `#4A3066`)
- **Nuit :** Shift vers le noir-bleuté, seules les lumières gardent leur couleur originale
- **Aube :** Shift vers le doré-rosé pendant 30 secondes

Ça évite de créer des variants jour/nuit pour chaque tile.

---

> **Ce document est la stratégie de production visuelle de VESTIGES.** Il répond à "comment faire des biomes qui ne soient pas plats" et "comment produire les assets efficacement". Le workflow IA → Aseprite → Godot est conçu pour un dev solo qui veut un résultat pro avec un budget temps limité.

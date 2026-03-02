# Guide de démarrage express — Pixel Art pour VESTIGES

> **Pour :** Raphaël — développeur solo, débutant en pixel art
> **Objectif :** Être capable de produire les premiers assets jouables en 2-3 semaines
> **Dernière mise à jour :** 2 mars 2026

---

## 1. L'OUTIL : ASEPRITE

### Pourquoi Aseprite

C'est le standard de l'industrie pour le pixel art de jeu. Tout le monde l'utilise (Celeste, Eastward, Dead Cells, Hyper Light Drifter). Il fait exactement ce dont tu as besoin et rien de plus : dessin pixel, animation frame par frame, gestion de palettes, export de spritesheets.

### Comment l'obtenir gratuitement

Aseprite est open source (licence GPL). Le binaire compilé coûte ~20€ sur Steam ou itch.io (et ça les vaut), mais tu peux le compiler toi-même gratuitement :

1. Clone le repo : `git clone --recursive https://github.com/aseprite/aseprite.git`
2. Suis les instructions de compilation : https://github.com/aseprite/aseprite/blob/main/INSTALL.md
3. Tu as besoin de CMake, Ninja, et Skia (les instructions détaillent tout)

Alternative gratuite sans compilation : **LibreSprite** (fork communautaire, même base de code, 100% gratuit, disponible sur Flathub/GitHub).

Alternative web : **Piskel** (piskelapp.com) — dans le navigateur, gratuit, suffisant pour démarrer si tu veux tester avant d'installer quoi que ce soit.

### Setup initial Aseprite pour VESTIGES

Dès que tu lances Aseprite :

- **Edit > Preferences > General** : mets le zoom par défaut à 400-800% (tu travailles pixel par pixel)
- **View > Grid > Grid Settings** : configure une grille iso si tu travailles sur les tiles (voir section tiles)
- **View > Pixel Grid** : active-le — tu veux voir chaque pixel

---

## 2. LES FONDAMENTAUX EN 5 JOURS

### Jour 1 — Les formes de base et la palette

**Exercice :** Dessine 5 carrés de 16×16 pixels. Dans chaque carré, représente un objet avec seulement 4 couleurs : un arbre, une pierre, une maison, une épée, un personnage stick.

**Ce que tu apprends :** En pixel art, tu ne "dessines" pas — tu PLACES des pixels. Chaque pixel est une décision. Avec seulement 4 couleurs (une pour la base, une pour l'ombre, une pour le highlight, une pour le contour), tu peux représenter n'importe quoi de lisible.

**Ressources :**
- Lospec Palette List (lospec.com/palette-list) — browse les palettes existantes, télécharge-les directement dans Aseprite
- Pour VESTIGES, commence par chercher des palettes de 12-16 couleurs avec des tons naturels (verts, bruns, gris) et un accent chaud (orange/or)

### Jour 2 — La tile isométrique

**Exercice :** Crée ton premier tile iso. Dans Aseprite, nouveau fichier de 32×16 pixels. Le diamant isométrique de base :

```
      ################
    ##                ##
  ##                    ##
##                        ##
  ##                    ##
    ##                ##
      ################
```

Remplis-le avec une texture d'herbe (3-4 nuances de vert, placement semi-aléatoire des pixels). Puis crée un tile de terre. Puis un tile de pierre. Place-les côte à côte dans un nouveau canvas plus grand pour vérifier qu'ils s'assemblent.

**Ce que tu apprends :** Le ratio iso standard est 2:1 (largeur = 2× hauteur). Les bords du diamant doivent suivre un pattern strict (2 pixels horizontaux pour 1 pixel vertical) pour que les tiles s'emboîtent parfaitement.

**Ressources :**
- "Pixel Art Isometric Tiles Tutorial" par Saultoons sur YouTube (~15 min, couvre exactement ça)
- Le wiki d'OpenGameArt a un guide iso tile excellent

### Jour 3 — Le premier personnage

**Exercice :** Dessine le Vagabond en 16×24 pixels, de face (direction bas-droite). Commence par la silhouette en une seule couleur. Puis ajoute 2-3 couleurs pour les vêtements. Puis le visage/capuche. Puis l'ombre et le highlight.

**Méthode pour un non-dessinateur :**

1. Cherche des refs de personnages pixel art isométriques sur Pinterest/Google (tape "isometric pixel art character 16x24")
2. Étudie comment les pros résolvent les mêmes problèmes que toi (comment représenter une capuche en 4 pixels ?)
3. NE copie PAS, mais utilise les mêmes "solutions pixel" (la forme d'une épaule en 3 pixels, la façon de placer les yeux)

**Ce que tu apprends :** À cette taille, un personnage c'est ~50-80 pixels colorés. Chaque pixel a un impact visuel énorme. La capuche du Vagabond = 3-4 pixels de plus sur la tête. Son sac à dos = un bloc de 3×4 pixels sur le dos.

### Jour 4 — La première animation

**Exercice :** Anime la marche du Vagabond en 4 frames (direction bas-droite). La méthode :

1. Frame 1 : pose neutre (les deux pieds au sol)
2. Frame 2 : pied droit en avant (le corps monte de 1 pixel — c'est le "bounce")
3. Frame 3 : pose neutre (miroir de frame 1, ou identique)
4. Frame 4 : pied gauche en avant (le corps monte de 1 pixel)

Teste l'animation en loop dans Aseprite (appuie sur Enter pour play/pause). Ajuste la vitesse (100-150ms par frame pour la marche).

**Astuce critique :** Le "bounce" de 1 pixel vers le haut sur les frames 2 et 4 fait TOUTE la différence. Sans ça, le personnage glisse. Avec, il marche.

**Ce que tu apprends :** L'animation pixel art est plus simple que tu ne le penses. 4 frames + 1 pixel de bounce = marche crédible. Le cerveau du joueur fait le reste.

### Jour 5 — Le premier ennemi + export

**Exercice :** Dessine un Rôdeur (16×20) et un Charognard (12×10). Le Rôdeur : silhouette humanoïde voûtée, bras trop longs, 2 points verts pour les yeux. Le Charognard : quadrupède aplati, trop de pattes, 1 point vert pour l'œil.

Puis exporte ta spritesheet : **File > Export Sprite Sheet** dans Aseprite. Configure pour un strip horizontal (une ligne, toutes les frames côte à côte). Format PNG.

**Ce que tu apprends :** L'export spritesheet est ce que tu importeras dans Godot. Habitue-toi au workflow Aseprite → PNG → Godot dès maintenant.

---

## 3. LE WORKFLOW ASEPRITE → GODOT 4

### Import des sprites

1. Exporte depuis Aseprite en spritesheet PNG (horizontal strip)
2. Dans Godot, importe le PNG : dans l'Inspector de la ressource importée, vérifie que **Filter** est sur `Nearest` (pas Linear — sinon tes pixels seront flous)
3. Pour les AnimatedSprite2D : crée un SpriteFrames, ajoute les frames depuis la spritesheet
4. Pour les TileMap : importe le tileset PNG et configure les tiles dans l'éditeur TileMap de Godot 4

### Settings Godot critiques pour le pixel art

Dans **Project > Project Settings** :

- `display/window/size/viewport_width` = 480
- `display/window/size/viewport_height` = 270
- `display/window/size/window_width_override` = 1920
- `display/window/size/window_height_override` = 1080
- `display/window/stretch/mode` = `viewport`
- `display/window/stretch/aspect` = `keep`
- `rendering/textures/canvas_textures/default_texture_filter` = `Nearest`

Ce dernier réglage est LE plus important — il garantit que tous les sprites restent nets au scaling.

### Structure de fichiers assets

```
assets/
├── palettes/              # Fichiers .pal ou .png de palettes Lospec
├── sprites/
│   ├── player/            # Vagabond.png, Forgeuse.png, etc.
│   ├── enemies/           # Rodeur.png, Charognard.png, etc.
│   ├── structures/        # Mur.png, Foyer.png, etc.
│   └── props/             # Arbres, rochers, débris par biome
├── tiles/
│   ├── foret/             # Tileset forêt reconquise
│   ├── ruines/            # Tileset ruines urbaines
│   ├── marecages/         # Tileset marécages
│   └── carriere/          # Tileset carrière effondrée
├── ui/
│   ├── icons/             # Icônes 16×16 des items/perks
│   └── panels/            # Bordures et fonds de panneau
├── vfx/                   # Sprites de particules si nécessaire
└── fonts/                 # Polices bitmap pixel art
```

---

## 4. LES TECHNIQUES ESSENTIELLES

### La palette restreinte — ta meilleure amie

En pixel art, la cohérence vient de la palette, pas du talent. Si tous tes sprites partagent les mêmes 12-16 couleurs, l'ensemble aura l'air cohérent même si chaque sprite individuel est imparfait.

**Pour VESTIGES :**
- Crée UNE palette maître de ~24 couleurs (toutes les couleurs du jeu)
- Chaque biome utilise un sous-ensemble de 12-16 couleurs
- Les couleurs partagées entre biomes (vert des yeux de créatures, orange du Foyer, couleurs du joueur) créent la cohérence globale

**Outil :** Lospec Palette List. Cherche "nature", "dark", "16 colors". Quelques palettes à explorer comme point de départ :
- **Endesga 32** (32 couleurs, très polyvalente, tons naturels + accents)
- **Resurrect 64** (64 couleurs si tu veux plus de marge)
- **Pear36** (36 couleurs, bons verts et bruns pour la forêt)

Ou crée ta propre palette en partant des couleurs UI définies dans la Bible (#E8E0D4, #D4A843, #C4432B, #7A9E7E, #5EC4C4, #8B6BAE).

### Le sel-out (outline coloré)

Au lieu d'un contour noir #000000 autour de tes sprites (qui aplatit tout), utilise un contour d'une couleur plus sombre que la zone adjacente. Par exemple :
- Contour du Vagabond côté ombre : brun foncé
- Contour du Vagabond côté lumière : brun moyen
- Contour des arbres : vert très foncé

Ça donne du volume et de la douceur. Compare avec un outline noir pur et tu verras immédiatement la différence.

### L'anti-aliasing manuel

Le pixel art n'a pas d'anti-aliasing automatique. Pour adoucir les courbes et diagonales, tu places manuellement des pixels de couleur intermédiaire aux "marches d'escalier". Ne le fais que sur les sprites importants (personnage joueur, Foyer) — pour les petits ennemis et les tiles, ce n'est pas nécessaire.

### Le dithering

Le dithering (motif de pixels alternés pour simuler un mélange de couleurs) est un outil puissant pour VESTIGES :
- **Transitions de biomes** : dithering entre les palettes des deux biomes
- **Effacement** : dithering qui se disperse vers le blanc = l'effacement parfait
- **Ombres douces** : dithering entre la couleur de base et l'ombre

Aseprite a un outil de dithering intégré (Ink > Pattern > Ordered Dithering).

### Le sub-pixel animation

La technique secrète du pixel art fluide. Au lieu de déplacer un sprite de 1 pixel entier (ce qui est un gros mouvement à 16 pixels de large), tu changes la distribution des couleurs à l'intérieur du sprite pour créer l'ILLUSION d'un mouvement de moins d'un pixel. Utilisé pour : les idles (le personnage "respire"), les mouvements subtils, les transitions douces.

**Tutoriel de référence :** "Sub-pixel animation" par Pedro Medeiros (saint11.org ou @saint11 sur Twitter). C'est LE maître du pixel art de jeu, ses mini-tutos sont la référence absolue.

---

## 5. LES RACCOURCIS POUR ALLER VITE

### Méthode "silhouette d'abord"

Pour chaque nouveau sprite :

1. Remplis la silhouette en UNE couleur (le gris moyen)
2. Vérifie que la silhouette est lisible et distincte des autres entités
3. Seulement ENSUITE, ajoute les couleurs, l'ombre, les détails

Si la silhouette ne marche pas, aucune quantité de détail ne sauvera le sprite.

### Le mirroring pour les directions

Tu as 4 directions à faire par personnage. Astuce : les directions gauche-droite sont des miroirs horizontaux. Donc tu ne dessines réellement que 2 directions (bas-droite et haut-droite), et tu flip pour les 2 autres. Ça divise le travail par 2.

Note : si ton personnage a un élément asymétrique (une arme dans une main), le mirror ne fonctionnera pas parfaitement. Dans ce cas, ajuste manuellement après le flip.

### Les recolors/palette swaps

Pour les élites (Aberrations dans la Bible), les variantes d'ennemis, et les variations de biome : ne redessine pas. Utilise les palette swaps. Dans Aseprite : **Sprite > Color Mode > Indexed**, puis modifie la palette. Un Rôdeur avec une palette plus sombre + yeux plus intenses = une Aberration.

### La génération IA comme assistant de concept

L'IA ne produit pas du bon pixel art directement, mais elle est excellente pour :

1. **Concept art de créatures :** Décris la Brute ou le Tisseuse à Midjourney/Stable Diffusion en style réaliste, puis utilise le résultat comme référence visuelle pour ta version pixel
2. **Exploration de palettes :** Demande des ambiances de forêt post-apo, de marécage brumeux, et extrais les couleurs dominantes
3. **Variations rapides :** Génère 10 versions d'un concept de créature, garde les éléments qui marchent, traduis-les en pixels

**Prompt type pour Stable Diffusion :** "isometric game creature, asymmetric organic horror, glowing green eyes, dark fluid, mossy stone texture, concept art, white background, detailed"

### Les assets communautaires comme base

Sur itch.io, cherche "isometric pixel art tileset" — tu trouveras des packs gratuits ou à 1-5€ qui peuvent servir de :
- **Étude** : démonte-les pixel par pixel pour comprendre comment les pros construisent des tiles iso
- **Placeholders** : utilise-les temporairement pendant que tu développes le gameplay
- **Base** : certains packs sont sous licence CC0/MIT et peuvent être modifiés (recoloring avec ta palette VESTIGES)

---

## 6. LE PLAN DE PRODUCTION PIXEL ART POUR VESTIGES

### Semaine 1-2 : Les fondamentaux

- [ ] Installer Aseprite, faire les exercices du Jour 1-5 ci-dessus
- [ ] Définir la palette maître (24 couleurs) et la palette Forêt (12-16 couleurs)
- [ ] Créer 5 tiles iso de base pour la Forêt Reconquise (herbe, terre, pierre, eau, béton fissuré)
- [ ] Créer le Vagabond (idle + walk, 1 direction)

### Semaine 3-4 : Le set jouable minimum

- [ ] Vagabond complet (idle + walk + dash + hurt, 4 directions)
- [ ] 3 ennemis de base (Rôdeur idle+walk+attack, Charognard idle+walk, Ombre idle+walk)
- [ ] Foyer (sprite statique + 2-3 frames de flamme animée)
- [ ] 10-15 tiles Forêt (sol + transitions + élévation)
- [ ] 5-6 props Forêt (arbre ×2, rocher ×2, buisson, voiture rouillée)

### Semaine 5-6 : Le polish minimum et l'UI

- [ ] Animations de mort/désintégration ennemis (3-4 frames + particules Godot)
- [ ] Tiles "effilochés" pour les bords de map (versions dithered des tiles de base)
- [ ] Icônes d'items (16×16) : bois, pierre, métal, fibre, essence (×5 minimum)
- [ ] Barres HP/XP en pixel art
- [ ] Icônes de perks placeholder (peut être des formes simples + couleur de rareté)

### Après : itérer biome par biome

Chaque nouveau biome = 1-2 semaines de travail pixel art :
- Nouvelle palette (sous-ensemble de la palette maître + 2-3 couleurs spécifiques)
- 10-15 tiles de sol
- 5-6 props
- 2-3 ennemis spécifiques (ou recolors d'existants)

---

## 7. LES RESSOURCES INDISPENSABLES

### Tutoriels (par ordre de priorité)

1. **Pedro Medeiros / Saint11** (saint11.org) — Mini-tutos pixel art en GIF. LA référence. Couvre tout : animation, couleur, formes, techniques. Gratuit.
2. **AdamCYounis** (YouTube) — Excellentes vidéos sur le pixel art de jeu, spécifiquement orientées gamedev solo. Couvre le workflow Aseprite → moteur de jeu.
3. **Brandon James Greer** (YouTube) — Pixel art de haute qualité, tutos détaillés, très bon sur la couleur et les palettes.
4. **MortMort** (YouTube) — Tutoriels accessibles pour débutants, bonne énergie, beaucoup de contenu.
5. **Saultoons** (YouTube) — Spécifiquement bon sur les tiles isométriques.

### Outils gratuits

| Outil | Usage | URL |
|-------|-------|-----|
| **Aseprite** (compilé) | Création pixel art + animation | github.com/aseprite/aseprite |
| **LibreSprite** | Alternative gratuite à Aseprite | libresprite.github.io |
| **Lospec** | Palettes, tutos, outils en ligne | lospec.com |
| **Piskel** | Pixel art dans le navigateur | piskelapp.com |
| **Pixelorama** | Éditeur pixel art open source (Godot-based !) | orama-interactive.itch.io/pixelorama |
| **Tilesetter** | Génération d'auto-tiles | led.itch.io/tilesetter |
| **Stable Diffusion** | IA locale pour concept art | stability.ai |
| **Krita** | Retouche et concept art (pas pixel art) | krita.org |
| **Photopea** | Photoshop dans le navigateur, gratuit | photopea.com |

### Communautés

- **r/PixelArt** — Feedback, inspiration, entraide
- **Lospec Discord** — Communauté pixel art dédiée
- **Pixel Joint** (pixeljoint.com) — Galerie + forums pixel art historique
- **Game Dev Alliance** (Discord FR) — Communauté gamedev francophone, section pixel art

---

## 8. ERREURS CLASSIQUES DU DÉBUTANT

**Le pillow shading :** Mettre l'ombre sur tous les bords et le highlight au centre. Ça donne un aspect "coussin" moche. À la place, choisis UNE direction de lumière (haut-gauche est le standard) et ombre de façon cohérente.

**Trop de couleurs :** L'envie d'ajouter des couleurs pour "plus de détail". En réalité, plus de couleurs = moins de cohérence. Force-toi à rester dans ta palette.

**Les banding (lignes parallèles) :** Des lignes de pixels de même longueur qui se suivent. Ça crée un pattern artificiel qui casse l'illusion. Varie les longueurs.

**Mélanger les résolutions :** Des sprites à des échelles différentes dans le même jeu. Si ton personnage est en 16×24, tes tiles en 32×16, et tes props en multiples de 8 — garde cette cohérence partout.

**Zoomer trop :** Tu travailles à 400-800% de zoom, mais tu dois CONSTAMMENT vérifier à 100% (la taille réelle en jeu). Un sprite magnifique à 800% peut être illisible à 100%.

---

> **Le meilleur conseil qu'on puisse donner à un dev qui se lance dans le pixel art : fais un sprite moche, mets-le en jeu, et itère.** La boucle "dessiner → voir en jeu → corriger" est 10× plus productive que "dessiner → perfectionner dans l'éditeur → ne jamais l'importer". Ton premier Vagabond sera moche. Ton dixième sera correct. Ton vingtième sera bien. Et ton centième sprite sera bon. C'est le volume qui fait le skill, pas le talent inné.

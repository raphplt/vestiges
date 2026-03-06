# VESTIGES — Asset List Exhaustive

> **Version :** 1.0
> **Dernière mise à jour :** 6 mars 2026
> **Source :** GDD v1.3, Bible v1.1, Charte Graphique v1.0
> **Convention :** ✅ = fait, 🔲 = à faire, ⏳ = en cours, 🔁 = placeholder en place

---

## STATISTIQUES GLOBALES

| Catégorie | Nombre d'assets estimé |
|-----------|----------------------|
| Tiles biomes | ~180 |
| Personnages joueurs | ~7 × 56-68 frames = ~420 frames |
| Ennemis | ~10 types × 48-60 frames = ~550 frames |
| Armes & outils | ~25 |
| Items & ressources | ~35 |
| Structures & défenses | ~30 |
| Coffres & loot | ~12 |
| Effets visuels (sprites) | ~25 |
| UI / HUD | ~60 |
| Hub | ~20 |
| Décor & props par biome | ~60 |
| **TOTAL ESTIMÉ** | **~1400+ sprites/frames individuels** |

---

## 1. TILES — BIOMES

### 1.1 Tiles communs (tous biomes)

Chaque tile existe en version "ancré" (normal) et "effiloché" (bords de map).

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 1 | Sol de base (herbe/terre/béton selon biome) | 32×16 | 3 variants + effiloché | P0 | 🔲 |
| 2 | Sol de transition biome A→B | 32×16 | 2 par combo de biomes | P1 | 🔲 |
| 3 | Chemin/route | 32×16 | 4 (droit, courbe, T, croix) | P0 | 🔲 |
| 4 | Eau peu profonde | 32×16 | 2 + animation 2 frames | P1 | 🔲 |
| 5 | Eau profonde | 32×16 | 2 + animation 2 frames | P1 | 🔲 |
| 6 | Bord de map — dissolution (dithering → blanc) | 32×16 | 3 niveaux de dissolution | P1 | 🔲 |
| 7 | Fog of war — voile blanc-bleuté | 32×16 | Animation 3 frames | P1 | 🔲 |

### 1.2 Forêt Reconquise (~30 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 8 | Sol forêt — herbe + mousse | 32×16 | 3 | P0 | 🔲 |
| 9 | Sol forêt — terre + racines | 32×16 | 2 | P0 | 🔲 |
| 10 | Autoroute fissurée (envahie de végétation) | 32×16 | 4 (droit, fissure, herbe, effondré) | P1 | 🔲 |
| 11 | Fondation béton envahie | 32×16 | 2 | P1 | 🔲 |
| 12 | Sous-bois dense (mousse + feuilles mortes) | 32×16 | 2 | P1 | 🔲 |
| 13 | Clairière (herbe + fleurs) | 32×16 | 2 | P2 | 🔲 |
| 14 | Mur ruiné couvert de lierre (tile avec hauteur) | 32×32 | 3 | P1 | 🔲 |
| 15 | Arbre géant (canopée) — base | 32×48 | 2 | P0 | 🔲 |
| 16 | Arbre moyen | 16×32 | 3 | P0 | 🔲 |
| 17 | Buisson | 16×12 | 3 | P1 | 🔲 |
| 18 | Souche | 12×8 | 2 | P2 | 🔲 |
| 19 | Rocher couvert de mousse | 16×12 | 2 | P1 | 🔲 |
| 20 | Voiture envahie de végétation | 32×20 | 2 | P2 | 🔲 |
| 21 | Maison effondrée (base) | 64×48 | 2 | P2 | 🔲 |
| 22 | Panneau routier rouillé | 8×16 | 3 | P2 | 🔲 |
| 23 | Lampadaire cassé couvert de lierre | 8×24 | 2 | P2 | 🔲 |
| 24 | Pont fissuré (pièce) | 32×24 | 2 | P2 | 🔲 |

### 1.3 Ruines Urbaines (~30 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 25 | Sol béton fissuré | 32×16 | 3 | P0 | 🔲 |
| 26 | Sol intérieur (carrelage cassé) | 32×16 | 2 | P1 | 🔲 |
| 27 | Trottoir défoncé | 32×16 | 2 | P1 | 🔲 |
| 28 | Mur béton debout (avec hauteur) | 32×32 | 3 (intact, fissuré, effondré) | P0 | 🔲 |
| 29 | Mur brique exposé | 32×32 | 2 | P1 | 🔲 |
| 30 | Fenêtre cassée (intégré au mur) | Dans le mur | 3 | P1 | 🔲 |
| 31 | Escalier effondré | 32×32 | 2 | P2 | 🔲 |
| 32 | Poutrelle acier | 32×8 | 2 (horizontale, diagonale) | P1 | 🔲 |
| 33 | Débris de béton au sol | 16×8 | 3 | P1 | 🔲 |
| 34 | Bureau/meuble renversé | 16×12 | 3 | P2 | 🔲 |
| 35 | Poubelle/conteneur | 12×12 | 2 | P2 | 🔲 |
| 36 | Voiture abandonnée (urbain, plus intacte) | 32×16 | 2 | P2 | 🔲 |
| 37 | Feu de signalisation tombé | 8×16 | 1 | P2 | 🔲 |
| 38 | Clôture grillagée déformée | 16×16 | 2 | P2 | 🔲 |

### 1.4 Marécages (~25 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 39 | Sol boueux | 32×16 | 3 | P0 | 🔲 |
| 40 | Sol mousse humide | 32×16 | 2 | P1 | 🔲 |
| 41 | Eau marécageuse (laiteuse) | 32×16 | 2 + anim 2 frames | P0 | 🔲 |
| 42 | Nénuphar / plante flottante | 12×8 | 3 | P2 | 🔲 |
| 43 | Arbre mort blanchi | 16×32 | 3 | P1 | 🔲 |
| 44 | Racine aérienne | 16×16 | 2 | P1 | 🔲 |
| 45 | Souche dans l'eau | 12×10 | 2 | P2 | 🔲 |
| 46 | Champignon toxique | 8×8 | 3 (couleurs) | P2 | 🔲 |
| 47 | Brume au sol (anim) | 32×8 | Animation 3 frames | P1 | 🔲 |
| 48 | Pierre affleurante dans l'eau | 16×8 | 2 | P2 | 🔲 |

### 1.5 Carrière Effondrée (~25 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 49 | Sol roche brute | 32×16 | 3 | P0 | 🔲 |
| 50 | Sol terre rouge | 32×16 | 2 | P1 | 🔲 |
| 51 | Paroi rocheuse (avec hauteur) | 32×48 | 3 | P0 | 🔲 |
| 52 | Rails de mine | 32×16 | 2 (droit, courbe) | P1 | 🔲 |
| 53 | Wagon de mine renversé | 24×16 | 1 | P2 | 🔲 |
| 54 | Étai en bois (soutènement) | 16×24 | 2 (intact, cassé) | P1 | 🔲 |
| 55 | Veine de cristal Essence | 16×16 | 3 (petit, moyen, gros) + lueur anim | P1 | 🔲 |
| 56 | Machine rouillée (gros) | 32×32 | 2 | P2 | 🔲 |
| 57 | Éboulis/pierres tombées | 16×12 | 3 | P1 | 🔲 |
| 58 | Gouffre sombre (trou dans le sol) | 32×16 | 2 | P2 | 🔲 |

### 1.6 Champs Sauvages (~20 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 59 | Sol prairie herbeuse | 32×16 | 3 | P0 | 🔲 |
| 60 | Sol terre sèche | 32×16 | 2 | P1 | 🔲 |
| 61 | Herbes hautes (blé/graminées) | 16×16 | 3 + anim vent 2 frames | P1 | 🔲 |
| 62 | Coquelicots/fleurs | 8×8 | 3 (couleurs) | P2 | 🔲 |
| 63 | Muret de pierre bas | 32×12 | 2 (intact, écroulé) | P1 | 🔲 |
| 64 | Clôture bois | 32×12 | 2 (intacte, cassée) | P2 | 🔲 |
| 65 | Arbre solitaire | 16×32 | 2 | P1 | 🔲 |
| 66 | Meule de foin décomposée | 12×12 | 2 | P2 | 🔲 |
| 67 | Flaque d'eau | 16×8 | 2 | P2 | 🔲 |

### 1.7 Sanctuaire (POI rare, ~10 tiles)

| # | Asset | Taille | Variants | Priorité | Statut |
|---|-------|--------|----------|----------|--------|
| 68 | Sol carrelé/mosaïque | 32×16 | 2 | P3 | 🔲 |
| 69 | Mur de pierre intact (exceptionnel) | 32×32 | 2 | P3 | 🔲 |
| 70 | Vitrail (fragment mural) | 16×24 | 3 couleurs | P3 | 🔲 |
| 71 | Bibliothèque intacte | 32×32 | 1 | P3 | 🔲 |
| 72 | Autel/piédestal | 16×16 | 1 | P3 | 🔲 |
| 73 | Arche d'entrée | 48×48 | 1 | P3 | 🔲 |

---

## 2. PERSONNAGES JOUEURS

### Par personnage : ~56-68 frames minimum (4 directions × 4-6 animations × 2-4 frames)

| # | Personnage | Taille | Animations | Frames total | Priorité | Statut |
|---|-----------|--------|-----------|-------------|----------|--------|
| 74 | **Le Vagabond** | 16×24 | idle(4f), walk(4f), dash(3f), hurt(2f), death(4f) × 4 dir | ~68 | P0 | 🔲 |
| 75 | **La Forgeuse** | 16×24 | idem | ~68 | P2 | 🔲 |
| 76 | **Le Traqueur** | 16×24 | idem | ~68 | P2 | 🔲 |
| 77 | **L'Éveillée** | 16×24 | idem | ~68 | P2 | 🔲 |
| 78 | **Le Colosse** | 20×28 | idem | ~68 | P2 | 🔲 |
| 79 | **L'Ombre** | 14×20 | idem | ~68 | P3 | 🔲 |
| 80 | **???** | ??? | idem | ~68 | P4 | 🔲 |

---

## 3. ENNEMIS

### 3.1 Créatures diurnes

| # | Créature | Taille | Directions | Animations | Frames | Priorité | Statut |
|---|---------|--------|-----------|-----------|--------|----------|--------|
| 81 | **Rôdeur** | 16×20 | 4 | idle(3f), walk(4f), attack(4f), death(4f) | ~60 | P0 | 🔲 |
| 82 | **Charognard** (individu) | 12×10 | 2 | idle(2f), walk(4f), attack(3f), death(3f) | ~24 | P0 | 🔲 |
| 83 | **Charognard Chef** (plus gros, œil en +) | 14×12 | 2 | idem | ~24 | P1 | 🔲 |
| 84 | **Sentinelle** | 12×24 | 1 (fixe) | idle(3f), attack(3f), death(4f) | ~10 | P0 | 🔲 |
| 85 | **Tréant Corrompu** | 24×32 | 4 | idle(2f), walk(4f), attack(4f), death(4f) | ~56 | P1 | 🔲 |

### 3.2 Créatures nocturnes

| # | Créature | Taille | Directions | Animations | Frames | Priorité | Statut |
|---|---------|--------|-----------|-----------|--------|----------|--------|
| 86 | **Ombre** | 12×12 | 2 | idle(2f), move(3f), attack(2f), death(3f) | ~20 | P0 | 🔲 |
| 87 | **Brute** | 24×24 | 4 | idle(2f), walk(4f), charge(3f), attack(4f), death(4f) | ~68 | P1 | 🔲 |
| 88 | **Tisseuse** | 20×20 | 4 | idle(3f), move(4f), attack_tisse(4f), death(4f) | ~60 | P1 | 🔲 |
| 89 | **Hurleur** | 12×20 | 1 (fixe) | idle(2f), cri(4f), death(4f) | ~10 | P1 | 🔲 |
| 90 | **Rampant** | 16×8 | 2 | underground(3f), emerge(4f), attack(3f), death(3f) | ~26 | P2 | 🔲 |

### 3.3 Mini-boss & Boss

| # | Créature | Taille | Directions | Animations | Frames | Priorité | Statut |
|---|---------|--------|-----------|-----------|--------|----------|--------|
| 91 | **Colosse Forêt** | 48×48 | 4 | idle(3f), walk(4f), attack1(4f), attack2(4f), death(6f) | ~84 | P2 | 🔲 |
| 92 | **Colosse Urbain** | 48×48 | 4 | idem | ~84 | P3 | 🔲 |
| 93 | **Colosse Marécage** | 48×48 | 4 | idem | ~84 | P3 | 🔲 |
| 94 | **Colosse Carrière** | 48×48 | 4 | idem | ~84 | P3 | 🔲 |
| 95 | **L'Indicible** (boss rare) | 96×64+ | 1 (partiel) | idle(4f), tentacle_attack(4f), beam(3f), death(8f) | ~19 | P4 | 🔲 |

### 3.4 Élites (Aberrations)
Les Aberrations sont des recolors/modifications des sprites de base. Pas de nouveaux sprites from scratch.

| # | Base | Modification | Priorité | Statut |
|---|------|-------------|----------|--------|
| 96 | Chaque type d'ennemi | Palette shift + 1-2 pixels excroissance + aura shader | P3 | 🔲 |

---

## 4. ARMES

Chaque arme a un sprite d'icône inventaire (16×16) + sprite in-world optionnel (équipé au personnage).

### 4.1 Corps à corps

| # | Arme | Niveaux | Icône 16×16 | Priorité | Statut |
|---|------|---------|------------|----------|--------|
| 97 | **Épée** | Bois → Pierre → Métal → Essence | 4 icônes | P0 | ✅ |
| 98 | **Masse** | Bois → Pierre → Métal → Essence | 4 icônes | P1 | ✅ |
| 99 | **Lance** | Bois → Pierre → Métal → Essence | 4 icônes | P1 | ✅ |

### 4.2 Distance

| # | Arme | Niveaux | Icône 16×16 | Priorité | Statut |
|---|------|---------|------------|----------|--------|
| 100 | **Arc** | Bois → Pierre → Métal → Essence | 4 icônes | P0 | ✅ |
| 101 | **Arbalète** | Bois → Métal → Essence | 3 icônes | P1 | ✅ |
| 102 | **Lance-pierres** | Bois → Pierre → Métal | 3 icônes | P2 | ✅ (1 tier) |

### 4.3 Spéciales (débloquables méta)

| # | Arme | Icône 16×16 | Priorité | Statut |
|---|------|------------|----------|--------|
| 103 | **Bâton d'Essence** | 1 icône | P3 | ✅ |
| 104 | **Fouet** | 1 icône | P3 | ✅ |

### 4.4 Effets d'auto-attaque (VFX)

| # | Effet | Frames | Priorité | Statut |
|---|-------|--------|----------|--------|
| 105 | Slash épée (arc) | 3 frames | P0 | ✅ |
| 106 | Impact masse (AoE cercle) | 3 frames | P1 | ✅ |
| 107 | Thrust lance (ligne) | 3 frames | P1 | ✅ |
| 108 | Projectile flèche | 1 sprite + trail | P0 | ✅ |
| 109 | Projectile carreau arbalète | 1 sprite + trail | P1 | ✅ |
| 110 | Projectile pierre | 1 sprite | P2 | ✅ |
| 111 | Orbe Essence (tête chercheuse) | 3 frames loop | P3 | 🔲 |
| 112 | Frappe circulaire fouet | 4 frames | P3 | 🔲 |

---

## 5. OUTILS

| # | Outil | Niveaux | Icône 16×16 | Priorité | Statut |
|---|-------|---------|------------|----------|--------|
| 113 | **Hache** | Bois → Pierre → Métal → Essence | 4 icônes | P0 | ✅ |
| 114 | **Pioche** | Bois → Pierre → Métal → Essence | 4 icônes | P0 | ✅ |
| 115 | **Couteau** | Bois → Pierre → Métal → Essence | 4 icônes | P1 | ✅ |

---

## 6. RESSOURCES

| # | Ressource | Icône 16×16 | In-world (nœud de récolte) | Priorité | Statut |
|---|-----------|------------|---------------------------|----------|--------|
| 116 | **Bois** | 1 icône | Intégré aux arbres/décor | P0 | ✅ |
| 117 | **Pierre** | 1 icône | Rochers interactifs 16×12 | P0 | ✅ |
| 118 | **Métal** | 1 icône | Ferraille/ruines interactives | P0 | ✅ |
| 119 | **Fibre** | 1 icône | Plantes interactives | P1 | ✅ |
| 120 | **Nourriture** (générique) | 1 icône | Buissons à baies, etc. | P1 | ✅ |
| 121 | **Combustible** | 1 icône | Charbon, bidons | P1 | ✅ |
| 122 | **Composants** | 1 icône | Épaves tech, tiroirs | P2 | ✅ |
| 123 | **Essence** (cristal) | 1 icône | Veines cristallines (voir tiles) | P0 | ✅ |

---

## 7. ITEMS CONSOMMABLES

| # | Item | Icône 16×16 | Priorité | Statut |
|---|------|------------|----------|--------|
| 124 | **Bandage** | 1 | P0 | ✅ |
| 125 | **Potion de soin** | 1 | P1 | ✅ |
| 126 | **Antidote** | 1 | P2 | ✅ |
| 127 | **Nourriture — baies** | 1 | P1 | ✅ |
| 128 | **Nourriture — viande** | 1 | P2 | ✅ |
| 129 | **Nourriture — pain** (craft four) | 1 | P2 | ✅ |
| 130 | **Bombe** | 1 | P2 | ✅ |
| 131 | **Torche portable** | 1 | P0 | ✅ |

---

## 8. STRUCTURES & DÉFENSES

### 8.1 Murs

| # | Structure | Niveaux | Taille | Priorité | Statut |
|---|-----------|---------|--------|----------|--------|
| 132 | **Mur** | Bois → Pierre → Métal → Renforcé | 32×24 chacun | P0 | ✅ (3 niveaux) |
| 133 | **Porte** (mur avec passage) | Bois → Pierre → Métal | 32×24 chacun | P1 | ✅ |
| — | Variants : intact, endommagé, détruit | ×3 par niveau | — | P1 | 🔲 |

### 8.2 Pièges

| # | Structure | Taille | Priorité | Statut |
|---|-----------|--------|----------|--------|
| 134 | **Piques** | 32×16 (au sol) | P1 | ✅ |
| 135 | **Collets** | 16×8 | P2 | 🔲 |
| 136 | **Mine** | 8×8 | P2 | 🔲 |
| 137 | **Piège à feu** | 16×16 + anim activation | P2 | 🔲 |

### 8.3 Tourelles

| # | Structure | Taille | Priorité | Statut |
|---|-----------|--------|----------|--------|
| 138 | **Tourelle arbalète** | 16×24 | P2 | 🔲 |
| 139 | **Lance-flammes** | 16×24 | P3 | 🔲 |

### 8.4 Autres

| # | Structure | Taille | Priorité | Statut |
|---|-----------|--------|----------|--------|
| 140 | **Barricade** (rapide, temporaire) | 32×12 | P0 | ✅ |
| 141 | **Feu de camp** | 16×16 + anim feu 3f | P0 | ✅ (sprite, anim TODO) |
| 142 | **Torche plantée** | 8×16 + anim flamme 3f | P0 | ✅ (sprite + 3f flamme) |
| 143 | **Lanterne** | 8×12 + anim 2f | P1 | ✅ |
| 144 | **Station de craft** | 24×20 | P0 | ✅ |
| 145 | **Four** (débloquable) | 20×20 | P2 | ✅ |

---

## 9. LE FOYER

| # | Asset | Taille | Variants/Animations | Priorité | Statut |
|---|-------|--------|-------------------|----------|--------|
| 146 | **Foyer — niveau 1** (base) | 24×32 | Anim flamme 4 frames (orange-doré) | P0 | ✅ (sprite, anim TODO) |
| 147 | **Foyer — niveau 2** | 24×32 | Flamme plus grande, symboles lumineux | P2 | 🔲 |
| 148 | **Foyer — niveau 3** | 28×36 | Flamme bleutée au cœur + particules or | P3 | 🔲 |
| 149 | **Transition rayon de sécurité** | Shader/overlay | Dégradé tiles nets → effilochés | P1 | 🔲 |

---

## 10. COFFRES

| # | Asset | Taille | Animations | Priorité | Statut |
|---|-------|--------|-----------|----------|--------|
| 150 | **Coffre commun** (bois) | 16×12 | Fermé + ouverture 3f | P0 | ✅ (fermé, anim TODO) |
| 151 | **Coffre rare** (métal) | 16×12 | Fermé + ouverture 3f + lueur | P1 | ✅ (fermé, anim TODO) |
| 152 | **Coffre épique** (essence) | 16×14 | Fermé + ouverture 4f + particules | P2 | 🔲 |
| 153 | **Coffre de lore** (ancien) | 16×14 | Fermé + ouverture 3f, aspect unique | P2 | 🔲 |

---

## 11. EFFETS VISUELS (SPRITES)

Les effets sont complétés par GPUParticles2D dans Godot, mais les sprites de base sont nécessaires.

| # | Effet | Taille | Frames | Priorité | Statut |
|---|-------|--------|--------|----------|--------|
| 154 | **Flash de hit** (blanc) | 1 frame overlay | 1 | P0 | ✅ |
| 155 | **Nombres de dégâts** (pop) | Font pixel | — | P0 | 🔲 |
| 156 | **Orbes XP** (blanc-doré) | 4×4 | 2 frames | P0 | ✅ |
| 157 | **Dissolution ennemi** (particules noires) | Shader + | 4 frames | P1 | ✅ |
| 158 | **Impact projectile** | 8×8 | 3 frames | P0 | ✅ |
| 159 | **Explosion** (bombe/mine) | 24×24 | 5 frames | P2 | ✅ |
| 160 | **Flamme de torche/feu** | 8×12 | 3 frames loop | P0 | ✅ |
| 161 | **Étincelle de craft** | 4×4 | 2 frames | P1 | ✅ (1 frame) |
| 162 | **Dash trail** | Overlay | 3 frames fade | P1 | ✅ |
| 163 | **Aura Essence** (joueur) | Overlay | 3 frames loop | P2 | ✅ |
| 164 | **Pulse de lumière** (capacité) | 32×32 → 64×64 | 4 frames | P2 | 🔲 |
| 165 | **Bouclier d'Essence** | Overlay joueur | 3 frames loop | P2 | 🔲 |

---

## 12. UI / HUD

### 12.1 HUD in-game

| # | Élément | Style | Priorité | Statut |
|---|---------|-------|----------|--------|
| 166 | **Barre de vie** | Pixel art, 3-5px hauteur, dégradé rouge→orange→vert | P0 | 🔲 |
| 167 | **Barre d'XP / niveau** | Pixel art, 2-3px hauteur, blanc-doré | P0 | 🔲 |
| 168 | **Timer jour/nuit** (barre pleine largeur) | Gradient soleil→crépuscule→nuit | P0 | 🔲 |
| 169 | **Compteur de score** | Font pixel, style compteur mécanique | P0 | 🔲 |
| 170 | **Barre rapide** (3-4 slots capacités) | Cadre pixel art, bordures métal oxydé | P0 | 🔲 |
| 171 | **Indicateur de sélection** (slot actif) | Bordure dorée | P0 | 🔲 |
| 172 | **Popup +points** (kill) | Font pixel, blanc→doré | P0 | 🔲 |
| 173 | **Indicateurs de statut** (icônes) | 8×8 chacun (saignement, poison, etc.) | P1 | ✅ (8 icônes) |
| 174 | **Mini-map cadre** | Bordure pixel art | P2 | 🔲 |
| 175 | **Touche d'interaction contextuelle** | "E" ou icône | P1 | 🔲 |

### 12.2 Menus

| # | Élément | Priorité | Statut |
|---|---------|----------|--------|
| 176 | **Panneau inventaire** (fond, bordures, slots) | P1 | 🔲 |
| 177 | **Panneau craft** (fond, bordures, recettes) | P1 | 🔲 |
| 178 | **Écran de sélection de perk** (3 cartes) | P0 | 🔲 |
| 179 | **Écran de mort / score final** | P1 | 🔲 |
| 180 | **Écran titre** | P3 | 🔲 |
| 181 | **Menu pause** | P2 | 🔲 |
| 182 | **Panneau carte** (fond, icônes de POI) | P2 | 🔲 |

### 12.3 Icônes de perks (16×16 chacune)

| # | Perk | Priorité | Statut |
|---|------|----------|--------|
| 183 | Écho | P1 | ✅ |
| 184 | Vampirisme | P1 | ✅ |
| 185 | Berserker | P1 | ✅ |
| 186 | Barrage | P1 | ✅ |
| 187 | Architecte | P1 | ✅ |
| 188 | Récupérateur | P2 | ✅ |
| 189 | Torche vivante | P1 | ✅ |
| 190 | Canalisation | P2 | ✅ |
| 191 | Siphon | P2 | ✅ |
| 192 | Instabilité | P2 | ✅ |
| 193 | Deuxième souffle | P2 | ✅ |
| 194 | Maître du temps | P3 | 🔲 |
| 195 | Éveillé | P3 | 🔲 |
| 196 | +HP / +Dégâts / +Vitesse / +Armure (×4) | P0 | ✅ |

---

## 13. HUB

| # | Asset | Taille | Animations | Priorité | Statut |
|---|-------|--------|-----------|----------|--------|
| 197 | **Fond Hub** (vide blanc-bleuté) | Plein écran | Particules lentes | P3 | 🔲 |
| 198 | **Plateformes de pierre** (sol du Hub) | 32×16 | — | P3 | 🔲 |
| 199 | **Arbre de Souvenirs — petit** (début) | 24×32 | Lueur douce 2f | P3 | 🔲 |
| 200 | **Arbre de Souvenirs — grand** (fin) | 64×96 | Lueur + orbes 3f | P4 | 🔲 |
| 201 | **Les Miroirs** (sélection perso) | 16×24 chacun | Intact/brisé + reflet | P3 | 🔲 |
| 202 | **L'Établi** | 24×20 | — | P3 | 🔲 |
| 203 | **L'Obélisque** | 12×28 | Symboles lumineux + vibration | P3 | 🔲 |
| 204 | **Les Chroniques** (mur de scores) | 48×32 | Inscriptions qui apparaissent | P3 | 🔲 |
| 205 | **Le Vide** (bord du Hub) | Tiles dissolution | Anim 3f | P3 | 🔲 |

---

## 14. RÉSUMÉ PAR PRIORITÉ

### P0 — MVP jouable (premiers tests fun)
Placeholder OK pour l'esthétique, mais proportions et lisibilité finales.

- 1 biome de tiles (Forêt) : ~15 tiles
- Vagabond : toutes animations
- 3 ennemis : Rôdeur, Charognard, Ombre + Sentinelle
- Foyer niveau 1
- Armes de base : épée bois, arc bois
- Outils : hache bois, pioche bois
- Ressources : bois, pierre, métal, essence (icônes)
- Structures : mur bois, barricade, feu de camp, torche
- Coffre commun
- HUD : barre de vie, barre XP, timer, score, barre rapide, popup dégâts
- Sélection de perk (3 cartes)
- Effets : flash hit, orbes XP, impact, flamme torche

**Total P0 : ~120-150 assets/frames**

### P1 — Core loop complet
- Biomes 2 et 3 tiles
- Ennemis restants (Brute, Tisseuse, Hurleur, Tréant)
- Armes et outils niveaux supérieurs
- Structures : porte, pièges (piques), lanterne
- Coffre rare
- Effets : dissolution, dash trail
- UI : inventaire, craft, indicateurs de statut
- Icônes de perks principaux

**Total P1 : ~200-250 assets/frames supplémentaires**

### P2-P4 — Polish, contenu complet, Hub
Le reste (voir les lignes marquées P2/P3/P4 ci-dessus).

---

> **Ce document est la production bible.** Chaque asset y est listé, numéroté, dimensionné et priorisé. Cocher les statuts au fur et à mesure de la production. L'objectif P0 est atteignable en 2-4 semaines avec le workflow AI + Aseprite.

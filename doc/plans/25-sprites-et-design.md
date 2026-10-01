# Plan 25 — Sprites et design : objets, projectiles ennemis, raretés, menus

1er octobre 2026 · Demandé par Raphaël ([DECISIONS §40](DECISIONS.md)) : « un prompt/plan global pour avancer sur les sprites (faire tous les objets, revoir certains projectiles ennemis pour les rendre plus jolis/impactants ; et les petits détails comme les raretés et les menus) ». Ce plan est écrit pour **un agent qui travaille seul**. Le code de jeu (HUD, écrans, bonus lâchés) est fait en parallèle par un autre agent au [plan 24](24-retours-du-1er-octobre.md) : ce plan livre les **images** et les branche là où le code les attend.

## 0. Prompt à donner à l'agent

> Tu travailles sur Vestiges (Godot 4.7.2, C#). Lis `AGENTS.md`, `CLAUDE.md`, `.claude/rules/pixel-art.md`, `doc/CHARTE-GRAPHIQUE.md`, puis ce plan (`doc/plans/25-sprites-et-design.md`) en entier, et `doc/plans/DECISIONS.md` §17 (icônes d'armes validées), §29-30 (icônes de perks : « fragments teintés ») et §39-40.
>
> Ta mission : produire les sprites listés au §2, **lot par lot dans l'ordre du §3**. Pour chaque lot : un générateur procédural dans `tools/` (pipeline `tools/sprites`, pas de dessin isolé, graines fixes), une **planche** dans `doc/plans/planches/` que tu regardes toi-même (Read sur le PNG) et que tu corriges jusqu'à ce qu'elle soit lisible à taille réelle, puis le branchement dans le jeu, une capture en vraie run (`/capture`) regardée, et `/close-lot`. Les lots marqués « planche à valider » s'arrêtent après la planche : tu la présentes à Raphaël avec ses questions et tu passes au lot suivant en attendant.
>
> Règles : jamais de `.import` supprimé ni écrit à la main ; `tools/smoke_test.sh` crée ceux des nouveaux PNG, à committer avec eux ; une image régénérée sans changement de modèle doit être identique octet pour octet. Pas de glyphes Unicode décoratifs dans l'UI (la police Saira ne les a pas). Commits en français, `type: sujet`. Ne touche pas au gameplay : si une image demande un changement de règle, note-le dans le plan et passe.

## 1. Direction commune

- **« Mode oubli » :** objets du quotidien abîmés, couleurs un peu passées, bords qui s'effilochent en pixels violets (`#6B4FA0` et voisins de la palette d'Effacement) quand l'objet est « oublié » ou rare. Le doré (`#F0C85C`) est réservé à ce qui se souvient : légendaire, Mémorial, XP.
- **Lumière haut-gauche, contour sel-out**, 4 tons par matériau, comme les icônes d'armes validées (`tools/sprites/weapons/icons.py`). Les icônes d'objets doivent pouvoir se poser à côté des icônes d'armes sans rupture de style.
- **Lisibilité d'abord :** une icône se reconnaît en silhouette à 32 × 32, et en 16 × 16 dans l'inventaire du HUD. Planche toujours aux deux tailles, sur fond sombre et sur le cadre de carte réel.
- **Pixel réel :** tout ce qui est animé dans l'UI (fonds, rayons, reflets) se dessine à la résolution du pixel art et s'agrandit sans lissage (filtre *nearest*). Jamais de dégradé lisse ni de polygone vectoriel à bords nets à la résolution de l'écran.

## 2. Inventaire de ce qui manque

### 2.1 Objets — 34 icônes (le gros du travail)

Aujourd'hui **aucun objet n'a d'icône propre** : `PerkIconResolver` prête une ancienne icône de perk selon la stat, et plusieurs objets partagent la même. Cible : une icône 32 × 32 par objet, champ `icon` dans `data/progression/passive_souvenirs.json`, `PerkIconResolver` retiré.

| Objet | Sujet de l'icône |
|---|---|
| Ressort de sommier | Ressort hélicoïdal rouillé, une spire détendue |
| Papier carbone | Feuille bleu nuit pliée en deux, double trace d'écriture |
| Reflet brisé | Éclat de miroir triangulaire, reflet clair |
| Rondelle de cuivre | Rondelle cuivrée, oxydée vert-de-gris par endroits |
| Mètre pliant | Mètre jaune en zigzag, chiffres noirs |
| Pince à linge | Pince en bois, ressort métal |
| Lunettes de lecture | Demi-lunes à monture fine, un verre fendu |
| Bouton de manteau | Gros bouton à quatre trous, fil qui pend |
| Bobine de fil | Bobine en bois, fil rouge, aiguille plantée |
| Genouillère | Genouillère de sport rembourrée, scratch usé |
| Écusson de pompier | Écusson brodé rouge et or |
| Lacet rouge | Lacet noué en boucle, embouts blancs |
| Aimant de frigo | Aimant en forme de fruit, écaillé |
| Photo de classe | Photo cornée, rangées de petites silhouettes, un visage effacé |
| Jeton de fête foraine | Jeton de laiton troué, étoile frappée |
| Allumette humide | Allumette au bout noirci, goutte d'eau |
| Glaçon dans un mouchoir | Mouchoir à carreaux noué autour d'un glaçon bleu |
| Thermomètre | Thermomètre à mercure, colonne rouge haute |
| Épingle à nourrice | Épingle ouverte, reflet |
| Pétard mouillé | Pétard rouge, mèche tordue, gouttes |
| Dé à coudre | Dé métal piqueté, vu de trois quarts |
| Semelle usée | Semelle de chaussure, trou au talon |
| Boîte de pansements | Boîte métal blanche à croix rouge, couvercle entrouvert |
| Loupe de philatéliste | Petite loupe pliante sur un timbre dentelé |
| Stylo à quatre couleurs | Stylo épais, quatre poussoirs colorés |
| Tabouret de camping | Tabouret pliant à trois pieds, toile verte |
| Chewing-gum | Plaquette entamée, papier argent froissé |
| Gilet réfléchissant | Gilet jaune fluo, bandes grises |
| Thermos | Thermos écossais, bouchon-tasse |
| Médaille cabossée | Médaille sur ruban tricolore, bosselée |
| Porte-monnaie usé | Porte-monnaie à fermoir, cuir craquelé |
| Presse-papier en verre (monde) | Dôme de verre, fleur figée dedans |
| Calendrier arraché (monde) | Bloc éphéméride, feuilles arrachées en biais |
| Médaillon ouvrant (monde) | Médaillon ouvert, portrait effacé |

Les trois objets « monde » ont leur icône dès ce plan, même s'ils attendent le Reliquaire.

### 2.2 Raretés — 5 icônes + cadres

- **Éclats de verre** qui gagnent une facette par rang (validé, plan 24 §3 B4) : commune terne (1 facette), inhabituelle verte (2), rare bleue (3), épique violette (4), légendaire dorée (5, reflet animé sur 4 frames). Tailles 12 × 12 et 24 × 24. Couleurs : `data/ui/rarities.json`.
- **Cadres de carte** par rareté (nine-patch, à partir de `ui_card_normal.png`) : coin et liseré à la couleur de la rareté ; légendaire avec une frise dorée qui scintille.
- **Saut de rareté** (la Chance a monté la carte, plan 24 D2) : petite animation de 6 frames, l'éclat qui se fend et révèle le suivant, avec un trèfle à 4 feuilles 8 × 8.
- Remplace partout `ChoiceStyle.RarityGlyph` (◆, ★) : level-up, Mémorial, Faille, coffre, pause, Collection.

### 2.3 Projectiles ennemis — à rendre plus beaux et plus percutants

Projectiles en jeu : `bile` (Cracheur effacé), `web` (Tisseuse), `howl` (Sentinelle hurlante), et `spit` par défaut (Hurleur, Présage). Constats : petits, peu lisibles dans la foule, et les sorts paraissent « verticaux » alors que la vue est en 2.5D (retour du 26 septembre, [DECISIONS §1](DECISIONS.md)).

| Projectile | Direction |
|---|---|
| `spit` (défaut) | Boule de vide violette avec cœur clair, traînée de 3 pixels qui s'efface, aplatie en 2:1 au sol |
| `bile` | Goutte verdâtre qui tremble, éclaboussure au sol à l'impact |
| `web` | Toile qui se déplie en vol (3 frames), laisse une flaque collante au sol |
| `howl` | Onde en arc, aplatie en 2:1, qui s'élargit et pâlit |
| Présage (nouveau, `omen`) | Œil fermé qui s'ouvre juste avant l'impact |

Pour chacun :
- **Télégraphe** : 2 frames d'apparition (gonflement) avant de partir, pour que le joueur le voie venir.
- **Impact** : 4 frames d'éclatement au sol, en 2:1 (vue iso), réutilisables par le pool d'effets (`CombatPools`).
- **Ombre** au sol sous le projectile (ellipse 2:1, `ZIndex = -1`) pour lire sa trajectoire en 2.5D.
- Lisibilité : contour clair d'un pixel, contraste suffisant sur les cinq sols de biome. Planche sur chaque sol.

Générateur : étendre `tools/sprites/projectiles.py` et `tools/generate_projectiles.py` (déjà multi-directions), manifeste `projectiles_manifest.json`.

### 2.4 Bonus lâchés — 5 sprites (plan 24 C4)

16 × 16, avec 4 frames de flottement et un reflet : **Gourde** (soin), **Fer à cheval aimanté** (aimant d'XP), **Couverture de survie** dorée (bouclier), **Café froid** en gobelet (cadence), **Pétard** rouge (onde). Plus une lueur au sol 2:1 de couleur propre à chaque bonus, et 3 frames de disparition (l'objet s'effiloche en pixels violets). L'agent du plan 24 les attend dans `assets/vfx/pickups/` ; tant qu'ils manquent, le jeu dessine un repli.

### 2.5 HUD et menus

| Élément | Demande |
|---|---|
| **Barre d'XP** (pleine largeur, en bas) | Cadre nine-patch fin en métal patiné ; remplissage doré qui ondule (texture 4 frames), reflet qui la parcourt ; pointe lumineuse au bout du remplissage ; éclat au passage de niveau. L'agent du plan 24 la code avec un repli dessiné ; les textures se branchent ensuite |
| Compteur de kills | Tête de mort 10 × 10 en pixel art, style os jauni |
| Sceaux de quêtes (plan 24 A2) | Sceau de cire 16 × 16, trois couleurs, anneau de progression en 8 crans, 5 frames de bris doré |
| Pictogrammes de la minimap | 5 × 5 : coffre, stèle du Mémorial, faille, petit lieu, joueur (flèche) |
| Fond animé des écrans de choix (plan 24 B1) | Tuile de poussière d'oubli et rayons en marches, 480 × 270, en 4 teintes (or, cyan, violet, neutre) |
| Écran de chargement (plan 24 B5) | Bande de sol iso à dérouler, sol de chacun des cinq biomes ; le personnage marche (animations existantes) |
| Icônes des Réminiscences | 14 « fragments teintés » par famille (combat, survie, collecte, récompense), direction validée DECISIONS §30 ; motifs à valider |
| Boutons et cadres | Revoir `ui_button_*`, `ui_panel_frame*`, `ui_card_*` : coins effilochés, bord qui s'efface en pixels, cohérents avec les cadres de rareté |

## 3. Lots

| Lot | Contenu | Arrêt | Branchement |
|---|---|---|---|
| **S1** Raretés | 2.2 : éclats, cadres, saut de rareté | Planche à valider | `ChoiceStyle` : icône au lieu du glyphe, cadre par rareté |
| **S2** Objets, 1ʳᵉ moitié | 2.1 : les 15 objets de propriété | Planche à valider | Champ `icon`, `PerkIconResolver` en repli seulement |
| **S3** Objets, 2ᵉ moitié | 2.1 : 16 objets de déclencheur + 3 « monde » | Planche à valider | Idem, puis `PerkIconResolver` supprimé |
| **S4** Projectiles ennemis | 2.3 | Planche puis capture en run | Manifeste, `EnemyProjectile` (télégraphe, ombre, impact) |
| **S5** Bonus lâchés | 2.4 | Capture en run | `assets/vfx/pickups/` |
| **S6** HUD | Barre d'XP, kills, sceaux, pictos minimap | Capture en run | Textures à la place des replis dessinés |
| **S7** Écrans | Fond animé, cadres et boutons, chargement | Planche à valider | Composant de fond, thème UI |
| **S8** Réminiscences | 14 icônes | Planche à valider | Champ `icon` de `perk_specializations.json` |

**Ordre :** S1 → S2 → S4 → S3 → S6 → S5 → S7 → S8. Les raretés d'abord, parce qu'elles se voient à chaque level-up ; les projectiles avant la fin des objets, parce que leur lisibilité touche le combat.

## 4. Vérification

- Planche à taille réelle et ×4, sur fond sombre et sur le cadre réel ; l'agent la regarde avant de la montrer.
- Capture en vraie run pour tout ce qui s'affiche en jeu (`tools/capture_run.sh`, modes `--capture-levelup`, `--capture-chests`, `--capture-memorial`) ; les PNG sont ouverts et regardés.
- Les icônes d'objets à côté des icônes d'armes sur une même planche : même style, même densité.
- `dotnet build` sans avertissement et `tools/smoke_test.sh` vert après chaque branchement.

## 5. Questions pour Raphaël, à poser avec les planches

1. Raretés : les éclats de verre à facettes conviennent-ils, ou préfère-t-il une autre forme (pierres, sceaux, étoiles dessinées) ?
2. Objets : style « objet posé » de trois quarts comme les armes, ou à plat, de face ?
3. Projectiles ennemis : quelle place pour le télégraphe (2 frames, environ 0,1 s) sans rendre les tireurs trop faciles ?

## 6. Comptes rendus

### S1 — planche proposée, 1er octobre 2026

`python3 tools/generate_rarities.py` produit la [planche S1](planches/25-s1-raretes.png) :
cinq éclats (12 et 24 px, 1 à 5 facettes), cadres dérivés de `ui_card_normal.png`
(marges nine-patch 3 px), reflet légendaire en 4 frames, quatre transitions de
6 frames et trèfle de 8 px. Palette lue dans `data/ui/rarities.json`.
Les textures sont calculées à leur taille native dans `tools/sprites/ui/`.
`--export` prépare les fichiers et leur manifeste après validation.

Planche inspectée aux tailles natives et ×4, sur fond sombre et cadre réel.
Deux générations ont donné le même SHA-256 :
`e477393dca7ea999b0ee41799e7788d25dd172bb58afdd276470a95808a2985e`.
Build : 0 avertissement, 0 erreur ; smoke 600 frames vert.
**Validation visuelle demandée à Raphaël ; aucun branchement ni case roadmap
cochée pour cette proposition.** Suite : S2, conformément au §3.

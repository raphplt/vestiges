# Plan 25 — Sprites et design : objets, projectiles ennemis, raretés, menus

**Reprise du 1er octobre — DECISIONS §41 :** Raphaël demande de tout brancher
sur `main`. Les arrêts de validation ci-dessous sont levés. Lots d'intégration :
I1 raretés et cadres ; I2 objets et Réminiscences ; I3 sceaux et bonus sur les
consommateurs du plan 24 ; I4 fonds, thème et chargement ; I5 captures, régressions
et clôture. Les anciens comptes rendus gardent l'état au moment de leur écriture.

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

### S2 — planche proposée, 1er octobre 2026

`python3 tools/generate_item_icons.py` produit la [planche S2](planches/25-s2-objets.png) :
quinze objets de propriété, modélisés en SDF dans `tools/sprites/items/`,
rendus par le pipeline des armes. Chaque modèle est rendu directement en
32 et 16 px (pas de réduction lissée). Armes de référence sur la même planche,
tailles natives et ×4, fond sombre et cadre réel. Modèles sans tirage aléatoire.

Planche inspectée. Deux générations identiques : SHA-256
`807613692f6acae87be399e94d14a96fc294236a1b1f71c197ca2cf007a1e42d`.
Build et import Godot verts lors de la préparation de la capture S4.
**Vue de trois quarts soumise à Raphaël ; branchement en attente, aucune
case roadmap cochée.** Suite : S4.

### S4 — rendu branché et vérifié, 1er octobre 2026

- `tools/generate_projectiles.py --enemies --biome-sheet doc/plans/planches/25-s4-projectiles.png` :
  cinq familles, apparitions 2 frames à 20 i/s, impacts 4 frames à 14 i/s ;
  spit/bile/howl en 16 directions. Contour clair, volumes couchés, toile en
  3 poses, onde qui s'élargit et pâlit, impacts qui se défont en pixels.
- Métadonnées du manifeste : liens `appearance`/`impact`, `shadow_width`,
  `loop`. Atlas découpés au chargement, références mises en cache.
- Apparition pendant les dernières 0,1 s de la **visée déjà existante** :
  aucun délai de tir ajouté. Ombre au sol 2:1, largeur 16 px, z −1.
- Impacts au sol par `NodePool<ProjectileImpact>` dans `CombatPools`,
  quatre poses, réinitialisation à chaque usage ; aucune collision ajoutée.
- **Écart à l'inventaire :** le Présage utilise déjà `OmenStrikeAbility`.
  L'œil s'ouvre dans sa zone annoncée et éclate au terme du délai existant.
  Le transformer en tireur changerait les règles : ce changement n'est pas fait.
- La flaque de toile est uniquement le dessin de l'impact ; son effet reste
  le ralentissement existant à la collision, sans nouvelle zone ralentissante.

Vérifications : build 0 avertissement/0 erreur, smoke 600 frames vert,
`test_enemy_abilities.sh` à zéro échec, dont cinq contrôles ajoutés sur
l'apparition avant le départ et le recyclage des impacts. Relecture locale
des changements C#/Godot (travail seul demandé au §0). PNG, manifeste et
planche identiques après régénération. Planche sur les cinq sols inspectée.

Captures seed 1002 inspectées : `/tmp/vestiges-plan25-s4-reference/`,
`/tmp/vestiges-plan25-s4-after/` et `/tmp/vestiges-plan25-s4-final/` ; la
dernière contient le pâlissement de l'onde. Le mode `--capture-abilities`
écrit bien ses PNG mais le script retourne 1 car son `rg` final attend une
ligne `RESULT` absente de ce mode. Pas d'exception de jeu dans ces captures.
`--shot-frames` permet désormais de saisir les animations brèves.

La première capture exploratoire utilisait deux identifiants erronés
(`cracheur`, `sentinelle`) ; elle a été remplacée par la référence avec
`fading_spitter`, `wailing_sentinel`, `tisseuse`, `hurleur`, `presage`.

Coût : comparaison A/B lancée contre `a8858ff0` dans
`/tmp/vestiges-plan25-s4-ab/`. La passe de base a rencontré un délai dépassé
lors de la capture après mesure (pas de nouvelle image depuis 5 s) :
**aucune conclusion de performance sur cette comparaison incomplète**.
Le banc exploratoire précédent chevauchait une capture et n'est pas retenu.
Les mesures brutes de nœuds/allocations restent disponibles dans les JSON.

### S3 — planche proposée, 1er octobre 2026

`python3 tools/generate_item_icons.py --lot s3` produit la
[planche S3](planches/25-s3-objets.png) : seize objets de déclencheur et
trois objets du monde, avec leurs identifiants du catalogue existant.
Même pipeline SDF et rendu natif 32/16 px que S2. Les 34 identifiants S2+S3
sont uniques ; régénération de S3 identique octet pour octet, planche
inspectée à côté des armes. Build sans avertissement et smoke verts.

**Planche soumise à validation ; les règles des objets du monde ne sont
pas créées. `PerkIconResolver` reste présent tant que S2/S3 ne sont pas
validés et branchés.** Suite : S6.

### S6 — HUD branché, sceaux préparés, 1er octobre 2026

`tools/generate_hud_sprites.py` livre 23 textures dans
`assets/ui/hud/plan25/` et `hud_manifest.json` (tailles, marges, poses).
[Planche S6](planches/25-s6-hud.png) inspectée, génération identique octet
pour octet. Barre d'XP : rail métal patiné en nine-patch, quatre remplissages
dorés, pointe lumineuse, reflet en marches et éclats au niveau gagné.
`XpBar`, `PixelSkull` et les cinq pictogrammes de `Minimap` utilisent les
textures en nearest, chargées une fois.

Sceaux : trois couleurs, neuf états de progression (0/8 à 8/8), bris doré
en cinq poses. **Le code de quêtes repliées du plan 24 L4 est absent de
cette branche.** Le manifeste est prêt ; aucune logique de quête ajoutée.
Le banc `--capture-hud-art` présente les sceaux en galerie explicitement
distincte du HUD fonctionnel, dans la vraie scène Main.

Build sans avertissement, smoke vert, captures 1080p seed 1002 inspectées
dans `/tmp/vestiges-plan25-s6-run/` (remplissage, éclat de niveau, aperçu
des sceaux). Relecture locale des caches et chemins de textures. Nouvelle
comparaison A/B des changements runtime S4+S6 dans
`/tmp/vestiges-plan25-runtime-ab/` ; résultats consignés ci-dessous.

### Mesure S4 + S6 — 1er octobre 2026

Seconde comparaison A/B terminée, toutes les passes valides :
`BENCH_SECONDS=15 tools/bench_ab.sh a8858ff0 /tmp/vestiges-plan25-runtime-ab 1`.
Même banc dense, 120 ennemis, versions alternées ; Ryzen 7 5700X,
Radeon RX 6950 XT, Godot 4.7.2 GL. Charge en fin de mesure : 2,69/16 cœurs.
Les générateurs et captures étaient arrêtés pendant la mesure.

| Résolution | FPS avant → après | p99 avant → après | Nœuds/s avant → après (arrondis) |
|---|---|---|---|
| 1280 × 720 | 186,8 → 178,1 | 10,7 → 12,8 ms | 1 → 1 |
| 1920 × 1080 | 177,4 → 191,5 | 11,7 → 10,5 ms | 2 → 1 |

Une seule passe par version : les écarts opposés selon la résolution ne
permettent pas d'affirmer un gain de FPS. Le test de réutilisation d'impact
prouve séparément l'absence de création d'un nouveau nœud au second usage.
La première comparaison incomplète reste conservée pour diagnostic.

### S5 — images livrées, branchement au plan 24 en attente, 1er octobre 2026

`tools/generate_pickups.py` livre les cinq bonus dans `assets/vfx/pickups/` :
quatre poses 16 × 16 de flottement/reflet, trois poses d'effilochage violet,
une lueur 16 × 8 par bonus. Modèles SDF des objets, graines fixes 2505–2509.
`pickups_manifest.json` donne les chemins, tailles, cadences et clés
`heal`, `magnet`, `shield`, `haste`, `blast` à raccorder aux IDs du plan 24.

[Planche S5](planches/25-s5-bonus.png) inspectée et corrigée : objets portés
à 14 px d'emprise dans le cadre de 16 px, pli de couverture renforcé pour
rester lisible. Régénération des PNG et du manifeste identique octet pour
octet. Build sans avertissement, smoke vert, aperçu dans Main inspecté
(`/tmp/vestiges-plan25-s5-run/`, quatre flottements et trois disparitions).

**Le système de bonus lâchés (plan 24 C4) n'existe pas encore dans cette
branche.** Le banc `--capture-pickup-art` est une galerie de sprites au sol,
sans collecte ni effet ; le ramassage en run ne peut donc pas être déclaré
vérifié. Aucun taux de loot ni bonus ajouté ici. Suite : S7.

### S7 — trois planches proposées, 1er octobre 2026

`python3 tools/generate_screen_sprites.py` prépare les [fonds de choix](planches/25-s7-ecrans.png),
les [cadres et boutons](planches/25-s7-cadres.png) et les [sols de chargement](planches/25-s7-chargement.png).
Quatre teintes, huit poses de poussière/rayons en pixels natifs 480 × 270,
graine 2507 ; neuf habillages dérivés des textures réelles ; cinq bandes
de sol 128 × 32 utilisant les tuiles de biome existantes. Personnage de
référence sur la planche de chargement, animation existante à réutiliser.

Planches inspectées ; les anciennes bordures dorées des boutons ont été
remplacées par du métal patiné (cyan au focus), pour réserver le doré aux
récompenses mémorielles. Bords effilochés violets, marges nine-patch 4 px.
Cadres et sols montrés en natif et ×4 ; fonds entiers en natif avec détails
de quatre poses agrandis ×4. Régénération des trois PNG identique octet
pour octet. Build sans avertissement et smoke 600 frames verts.
**Validation demandée avant tout export ou branchement au thème.**
Suite : S8.

### S8 — quatorze motifs proposés, 1er octobre 2026

`python3 tools/generate_reminiscence_icons.py` produit la
[planche S8](planches/25-s8-reminiscences.png) : quatorze fragments teintés
par famille, en 32 et 16 px rendus directement par le pipeline SDF des
armes, agrandissements ×4, fond sombre et cadre de carte réel. Neuf motifs
reprennent les prototypes du plan 05 ; cinq complètent le catalogue du
plan 21 §5. Aucun tirage aléatoire. Planche inspectée, couleur de Braise
renforcée pour la distinguer du cristal rouge.

**Écart de données :** `perk_specializations.json` ne définit que neuf
Réminiscences. Les cinq autres modèles réservent les identifiants d'art
`backlash` (Contrecoup), `ember_transfer` (Braise), `double_impact` (Mémoire
vive), `erasure_edge` (Lisière) et `dead_weight` (Poids mort). Ils ne créent
aucune règle de jeu. Leur futur branchement devra confirmer ces IDs.
Le manifeste exportable signale ces cinq entrées par `proposed_id`.

**Motifs soumis à Raphaël ; pas d'export dans `assets/` ni de champ `icon`
ajouté avant validation.** Le générateur historique des neuf prototypes
reste disponible et inchangé.

Régénération identique octet pour octet : SHA-256
`c33fe47463b03f6ee0a6959b8e8057f6280a593326562f864c6f829a63f8c60d`.
Build : 0 avertissement, 0 erreur ; smoke final 600 frames vert. Les 34 IDs
d'objets et les 14 IDs de Réminiscences sont uniques ; les neuf IDs de
Réminiscences existantes correspondent exactement au fichier de données.

### État après intégration sur main

Les lots ont été produits seul sur `sprites-plan25`, puis fusionnés.
L'intégration I1–I5 sur `main` suit la demande explicite de Raphaël (§41).
Les consommateurs du plan 24 ont été réunis par le merge `2788b208`.

| Lot | Branchement livré |
|---|---|
| S1 | Éclats, cadres et reflets ; Chance avec transitions et trèfle ; tous les écrans de choix |
| S2/S3 | 31 objets actifs dans les interfaces ; les trois objets du monde visibles « À venir » en Collection |
| S4 | Cinq familles de projectiles/attaques, apparitions, ombres et impacts recyclés |
| S5 | Cinq bonus du plan 24 : apparition au sol, flottement, lueur, ramassage et disparition |
| S6 | XP, éliminations, minimap, progression et complétion des sceaux |
| S7 | Quatre fonds animés, neuf habillages, cinq sols et personnage au chargement |
| S8 | 14 motifs en Collection et neuf définitions liées aux écrans ; sept effets actifs inchangés |

Les objets du monde attendent le Reliquaire. Délestage, Habitude et les cinq
Réminiscences nouvelles ne deviennent pas des offres jouables par la seule
présence d'une icône. Leurs règles relèvent des plans 05/22 ; aucune image
n'attend désormais une validation pour être branchée à un écran existant.

### I1 — raretés branchées sur main, 1er octobre 2026

Éclats 12/24 px et cadres nine-patch exportés ; reflet légendaire et frise
animés avec atlas mis en cache. Les rangs gagnés par la Chance se révèlent
successivement avec les six poses et le trèfle. `RarityGlyph` supprimé :
level-up, Mémorial, Faille et coffre utilisent les textures ; les services
sans rareté gardent un cadre neutre. Pause et Collection n'attribuent pas
de rareté aux objets/armes, conformément à la rareté portée par les améliorations.

Build sans avertissement, smoke 600 frames vert, capture des cinq raretés
dans Main (`/tmp/vestiges-plan25-i1-levelup/`) inspectée. Le mode de capture
historique retourne 1 faute de ligne `RESULT`, mais écrit toutes les images
sans exception de jeu ; ce défaut de compte rendu sera corrigé avec I5.

### I2 — objets et Réminiscences branchés, 1er octobre 2026

31 objets actifs : champs `icon`/`icon_small` lus dans le JSON et employés
dans le level-up, la Faille, le HUD, la pause, l'inventaire, le butin et le
bilan. `PerkIconResolver` et son UID supprimés ; aucune teinte de stat
n'altère les nouvelles images du HUD. Les neuf Réminiscences définies ont
leurs chemins d'icônes, employés dans les choix, la pause et l'inventaire.

Collection : 34 objets et 14 Réminiscences visibles, onglet Réminiscences
ajouté. Les trois objets du monde et les Réminiscences sans effet actif
portent « À venir » ; leur affichage lit les manifestes visuels et ne crée
aucune offre inactive. Les règles du Reliquaire et des cinq Réminiscences
absentes du catalogue ne sont pas inventées pour afficher une image.

Build sans avertissement, smoke vert, tous les chemins 32/16 px vérifiés.
Captures Main (`/tmp/vestiges-plan25-i2-final/`) et Collection
(`/tmp/vestiges-plan25-i2-collection-perks/`, `…-items/`) inspectées.
Le mode `--capture-levelup` couvre désormais les Réminiscences et retourne
une ligne `RESULT`, corrigeant son ancien faux échec de script.

### I3 — bonus et sceaux branchés, 1er octobre 2026

Les cinq IDs de bonus du plan 24 pointent vers les sprites S5 par un champ
`sprite` ; flottement en quatre poses, lueur 2:1 au sol et disparition en
trois poses. Le ramassage désactive immédiatement le bonus ; le nœud reste
hors des quatre places au sol pendant sa disparition, puis retourne au pool.
Réinitialisation des textures, opacité et position à chaque réutilisation.
Les effets, chances de butin et durées restent ceux du plan 24.

Les sceaux utilisent leurs neuf états de progression, leurs cinq poses de
bris, puis une texture dorée de complétion ajoutée au générateur S6.
Liaisons existantes à `QuestManager`/`EventBus` conservées.

Build sans avertissement, smoke vert, `test_field_bonuses.sh` : zéro échec,
dont dissolution et reprise du même nœud avec un autre bonus. Capture des
cinq ramassages et d'une quête complétée par `QuestManager` dans Main,
inspectée dans `/tmp/vestiges-plan25-i3-final/`. Le premier parcours était
masqué par le level-up accordé en récompense ; le bot le résout désormais.

### I4 — menus et chargement branchés, 1er octobre 2026

Les quatre atlas 480 × 270 remplacent le shader de repli : or pour le
level-up/coffre, cyan au Mémorial, violet à la Faille, neutre pour les
Réminiscences et le chargement. Huit poses à 3 i/s, cache partagé et filtre
nearest. Les neuf habillages passent par `UITheme` : boutons, panneaux,
cartes neutres et tuiles de Collection, focus compris. Les fichiers source
des anciens menus restent intacts pour préserver la génération déterministe.

Le chargement déroule les cinq sols natifs avec le personnage choisi, à
échelle entière. Les particules de repli du chargement sont remplacées par
le fond natif ; l'éclat de révélation du coffre réutilise les quatre poses
d'XP. Les textures de saut de rareté restent à leurs 32 px natifs pendant
la révélation, au lieu d'être réduites à 24 px.

Les premiers parcours Mémorial/Faille/coffre capturaient l'entrée avant
l'apparition des cartes. Les délais des outils attendent maintenant la fin
des animations. Le parcours level-up couvre aussi trois rangs de Chance
gagnés successivement et capture le véritable chargement de Main.

Build : zéro avertissement ; smoke 600 frames vert. Captures inspectées :
`/tmp/vestiges-plan25-i4-levelup/` (cinq raretés, Réminiscences, Chance,
chargement), `…-final-memorial/`, `…-final-rift/`, `…-final-loot/`,
`…-pause/` et `…-collection/`. Les écrans finaux montrent leurs cartes,
boutons et icônes ; les petites facettes communes du coffre sont bien des
textures, sans caractère décoratif dans les libellés.

### I5 — vérification de l'intégration, 1er octobre 2026

`test_ui_art.sh` : 122 assertions passent. Il charge les PNG par les vrais
lecteurs de données, contrôle les deux tailles d'icône, les 34/14 entrées de
Collection, les atlas, les cinq bonus et les neuf habillages. Il vérifie
également le départ et l'arrivée d'une révélation de trois rangs de Chance,
sans réduction des poses de 32 px. Aucun catalogue de gameplay n'est étendu.

Régressions : contrats de perks (32 assertions), acquisition (22), objets
(70), bonus (8), capacités ennemies (60) et six scénarios de profils dev
verts. Nouvelle capture du level-up et de la Chance inspectée dans
`/tmp/vestiges-plan25-i5-levelup/`. Régénération des cinq familles exportées
(rarités, objets, Réminiscences, HUD, écrans) : **158 PNG inchangés octet pour
octet**. Les `.import` et `.cs.uid` viennent de Godot.

Travail et revue effectués seul conformément au §0 ; aucun agent parallèle.
Les captures et journaux restent dans `/tmp`, les planches demandées et
les générateurs sont versionnés. La recette de goût en jeu reste à Raphaël ;
elle n'empêche plus les branchements demandés au §41.

Banc dense A/B : base `2788b208` (mêmes règles du plan 24), seed 221092026,
120 ennemis, 15 s après 5 s de chauffe, Godot 4.7.2 GL, Ryzen 7 5700X /
RX 6950 XT. Une passe complète dans `/tmp/vestiges-plan25-integration-ab/` :

| Résolution | FPS avant → après | p99 avant → après | Nœuds créés/s |
|---|---:|---:|---:|
| 720p | 192,2 → 156,6 | 11,2 → 15,8 ms | 1 → 1 |
| 1080p | 190,5 → 180,2 | 10,6 → 11,5 ms | 1 → 1 |

Écart défavorable observé, surtout à 720p ; une passe ne permet pas de
l'attribuer au branchement. Les allocations et appels de dessin n'augmentent
pas. Un autre processus utilisait fortement le CPU pendant cette série.
La seconde comparaison a été interrompue pour livrer sans prolonger les
vérifications supplémentaires, après le retour de Raphaël. Aucune conclusion
de gain ni d'absence de régression n'est revendiquée. Build sans avertissement,
smoke et vérifications fonctionnelles terminés ; arbre livré sur `main`.

### Correctif I6 — fond de coffre saccadé

Retour de Raphaël : le fond d'ouverture donne la nausée avec sa faible
cadence. Cause : huit poses de rotation à 3 images/s, soit 5,625° par saut.
Lot ciblé : rayons stables et poussières qui scintillent en continu ; même
composant corrigé dans les autres écrans de choix. Conserver les pixels
natifs et vérifier une ouverture réelle, le build et le smoke.

Livré : quatre textures natives fixes remplacent les huit poses de chaque
teinte. Le shader module seulement les pixels des poussières avec une
sinusoïde continue ; aucune rotation, aucun changement d'image en C#.
Le fondu d'ouverture et les révélations du butin sont conservés.

Build sans avertissement et smoke 600 frames verts. Capture de 32 images
successives d'un vrai coffre inspectée (`/tmp/vestiges-plan25-i6-loot/`).
Dans une zone du fond hors HUD/cartes, seuls 12 pixels sur 28 600 changent
de luminosité entre les images : les rayons restent en place. Les 21 PNG
réexportés (textures et planches) sont identiques lors d'une seconde génération.

### Correctif I7 — rotation continue, DECISIONS §43

I6 est corrigé : Raphaël veut conserver la rotation. Le shader calculera
l'angle à chaque image rendue, à 8°/s, avec les couleurs et la grille native
du générateur. Aucun défilement d'atlas à basse cadence. Vérifier les images
successives d'un coffre, le build, le smoke et les contrats UI.

Livré : angle piloté par `TIME` dans le shader, vitesse issue du manifeste,
couleurs issues de la palette commune. À 60 images/s, l'avance est de 0,133°
par image au lieu des anciens sauts de 5,625° à 3 images/s. La grille spatiale
reste en 480 × 270 avec nearest, sans plafond de cadence de l'animation.

Build : zéro avertissement ; smoke 600 frames et 122 contrôles UI verts.
32 captures horodatées du coffre sur 2,039 s inspectées dans
`/tmp/vestiges-plan25-i7-loot/` : les rayons se déplacent sur chacune des
31 paires successives (928 à 1 336 pixels changés dans la zone observée),
au lieu des seuls 12 pixels de poussières du fond fixe I6. Les captures ne
mesurent pas les FPS du jeu ; la rotation est calculée à chaque rendu.
Régénération des 21 PNG de textures/planches identique octet pour octet.

## 7. Retours après intégration — 1er octobre 2026

[DECISIONS §44](DECISIONS.md), coordination au [plan 24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026). Le travail livré est globalement apprécié. Restent de nouveaux compléments graphiques à traiter :

- **R2 :** icônes identifiant les gains des coffres (Essence, dégâts critiques, PV, etc.). Les éclats déjà branchés indiquent la rareté ; ils ne suffisent pas à identifier la nature du gain. Faire l'inventaire des icônes réutilisables, puis produire les manquantes par le pipeline et les vérifier dans la roulette et le résultat.
- **R1/R6 :** appui au plan 04 pour une pause mieux composée, des survols sans empilement de contours et une carte mieux dessinée/colorée.
- **R5 :** refonte des trois personnages jouables portée par le plan 08, distincte des lots d'objets déjà livrés.

La pression des tirs relève du plan 07 ; leur embellissement S4 ne résout pas à lui seul le retour de gameplay. La rotation continue du fond des coffres (I7) reste à préserver. Cette note n'ajoute aucun asset et ne clôt aucun nouveau lot.


### R1 — boutons sobres intégrés

Le générateur `tools/sprites/ui/screens.py` produit désormais quatre états par
aplats, avec un repère latéral cyan au survol/appui ; `UITheme` place le même
repère au focus, sans cadre de panneau superposé. Planches de contrôle dans
`/tmp/vestiges-r1-sheets`, captures pause/paramètres au plan 04 R1. Seuls les
quatre PNG de boutons changent : textures des fonds, manifeste de rotation et
shader I7 inchangés.


### R2 — icônes de récompense intégrées

Les icônes de statistiques natives 16 px, les orbes d'Essence/XP, le signe
mémoriel et les sprites propres aux objets sont réutilisés. Rareté déplacée
au bord droit des lignes ; nature du gain à gauche, pendant la roulette et
après révélation. Table `data/ui/loot_icons.json`, 169 contrôles UI verts,
captures de tous les gains à 100/130 % inspectées ; compte rendu au plan 04 R2.
Le shader, la vitesse de rotation et les sons des coffres restent ceux d'I7.

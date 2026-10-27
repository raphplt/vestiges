# Plan 57 — Le chemin du gardien se lit, et chaque terre a son gardien

25 octobre 2026 · [DECISIONS §198](DECISIONS.md) · passe avant la suite du [LE-JEU.md](../LE-JEU.md).

Retours de Raphaël après une première run sur la version du plan 56 :
- « les boss à la fin de chaque niveau ont été faits ? je ne les ai pas vus », puis « ça c'est un problème majeur. Il faut vraiment corriger ça » ;
- « il faut que les boss soient des entités à part, pas des versions grossies de mobs existants ».

## 1. Pourquoi on ne voit pas le gardien

Le chemin d'une terre tient en trois gestes : raviver trois Mémoriaux, revenir au repaire, réveiller le gardien. Rien ne le dit au bon endroit :
- **L'objectif** est une ligne en 10 px au-dessus de la minimap, en bas à droite, hors du regard. Le regard reste au centre et en haut au centre (temps, Série).
- **Le carton d'arrivée** donne le lieu, pas ce qu'il faut y faire.
- **Les Mémoriaux** ont une colonne de lumière, mais rien ne dit que ce sont les clés. La minimap ne les montre qu'une fois explorés.
- **Le repaire** est près du départ, que le joueur quitte et ne revoit plus. Quand la troisième lumière s'allume, rien ne se passe à l'écran : seule la petite ligne change.
- **Le gardien qui se lève seul**, au bout de 12 min, n'est pas annoncé non plus.

## 2. Chantier A : le chemin se lit

| Lot | Contenu | Effort |
|---|---|---|
| **C1** | **Le bon endroit et le bon moment.** L'objectif passe en haut au centre, sous la Série, plus gros, toujours là. Une flèche d'objectif dorée au bord de l'écran mène au Mémorial endormi le plus proche, puis au repaire ; à l'écran, un chevron flotte au-dessus. Le carton d'arrivée dit la règle (« Ravive 3 Mémoriaux, puis réveille le gardien »). À la 3e lumière, un carton annonce « Le gardien t'attend », avec un son ; si le gardien se lève seul, un carton le dit aussi | Moyen |
| **C2** | **Ce qui reste flou.** Sur la minimap, les Mémoriaux de la terre se voient dès l'arrivée, ravivés en or. Le repaire bat quand il est prêt. Une minute avant que le gardien se lève seul, l'objectif passe au rouge (« Le gardien se lève dans 1:00 »). Le bot des terres rejoint le cœur avant la rage, pour mesurer enfin la 3e terre (§8) | Faible |

**Tranché par défaut, réversible :**
- **La flèche montre les Mémoriaux même dans le brouillard.** La découverte y perd un peu, la clarté y gagne beaucoup. Leurs colonnes se voient déjà de loin.
- **Une seule flèche d'objectif**, vers la cible la plus proche. Les flèches des coffres restent, plus petites et à la couleur de leur rareté.
- **Pas de flèche pendant le combat** : le gardien traque le joueur, et la barre de boss le nomme.

## 3. Chantier B : un gardien par terre

Aujourd'hui, le gardien est le Souverain du biome (une créature ordinaire en variante champion), aux PV gonflés, avec les trois coups de référence (charge, frappe qui ouvre le vide, onde). La charte du [plan 51 §10](51-objectifs-de-run.md) demandait déjà « un vrai corps, un gros personnage, différent pour chaque terre ».

**La forme :** chaque gardien est une créature à part dans `data/enemies/`, de rang `guardian`, que rien d'autre ne fait naître. Il a son nom, son corps (environ trois fois une créature), ses animations, sa barre de boss. Il prend deux des trois coups de référence et ajoute **un coup à lui**. Les coups se règlent dans ses données. Chaque biome nomme son gardien.

**Les cinq gardiens** (proposition de l'agent, réversible). Ce que la ville a refusé de regarder, lieu par lieu (lore §7 : les créatures sont la peur que Vaulme avait des gens du port) :

| Terre | Gardien | Corps | Coups de référence | Coup à lui |
|---|---|---|---|---|
| Les bois de la colline | **Le Grand Cerf** | Cerf dressé, bois et écorce, ramure de branches mortes | Charge, frappe | **Charges en chaîne** : trois charges de suite, chacune annoncée, la dernière finit dans le vide qu'ouvre sa frappe |
| Les fermes du plateau | **L'Épouvantail** | Grand épouvantail en loques, faux, corbeaux dans la paille | Frappe, onde | **Fauchage** : un arc de faux annoncé, sur les trois quarts du tour, avec une ouverture où se glisser |
| La Haute-Ville | **Le Tocsin** | Corps de pierre portant la cloche qui n'a pas sonné | Charge, onde | **Volée** : anneaux concentriques à une brèche chacun, à traverser par la brèche ou en dash |
| Le Bas-Port | **Le Pilleur** | Masse voûtée, sac sur le dos, gaffe de pêcheur : l'image que la Haute-Ville se faisait des gens du port | Charge, frappe | **Gaffe** : il lance sa gaffe sur une ligne annoncée et tire le joueur vers lui s'il touche |
| La carrière des Aubiers | **Le Carrier** | Géant de pierre de taille, pic, poussière | Frappe, onde | **Éboulement** : des blocs tombent sur des disques annoncés et restent quelques secondes comme obstacles |

La Barrière (mi-pente de la Montée) et l'Indicible (au sommet) restent hors de ce tableau : ils viennent avec la Montée, étape 6.

| Lot | Contenu | Effort |
|---|---|---|
| **G0** | **Le gardien devient une entité.** Données de gardien (`tier: guardian`), coups choisis dans ses données, champ `guardian` des biomes, `GuardianDirector` pose le gardien de la terre au lieu d'un Souverain. Les échos du late game reprennent les gardiens vaincus. Plus de variante champion pour les gardiens | Moyen |
| **G1** | **Le Grand Cerf, gardien de référence fini** : corps procédural, animations (attente, marche, élan, frappe, mort), charges en chaîne, sons, mort mise en scène. Combat de 1 à 2 min au bot, profil neuf. Tant que les autres manquent, **toutes les terres le prennent** : plus aucun Souverain grossi | Élevé |
| **G2 à G5** | L'Épouvantail, le Tocsin, le Pilleur, le Carrier, un par lot, sur le modèle du Cerf | Moyen chacun |

**Tranché par défaut, réversible :**
- **Corps procéduraux d'abord**, par le pipeline des créatures (`tools/sprites/creatures/`). L'ami artiste les reprend ensuite : retouche Aseprite, ou planche qui remplace la génération.
- **Le Cerf d'abord** : sa charge en chaîne est le coup le plus lisible, et il pousse à sauter le vide, la deuxième signature du jeu.
- **Le repaire montre le gardien qui dort** dès G1 : son corps couché, au lieu de la stèle violette.

## 4. Ce qu'on retire

- La variante champion des Souverains comme gardien (G0).
- La stèle violette du repaire (G1).
- La ligne d'objectif au-dessus de la minimap (C1).

## 5. L'avocat du diable

- **Une flèche, c'est tenir la main.** Les survivors-likes montrent leurs objectifs sans gêne (Megabonk, Deep Rock Galactic: Survivor), et ici le chemin est la boucle elle-même. Si la flèche lasse, un réglage la limitera aux premières runs.
- **Cinq gardiens, c'est beaucoup avant les terres complètes.** C'est pour ça que G1 sert à toutes les terres : un vrai gardien dès le premier lot, les quatre autres suivent.
- **Des corps procéduraux risquent de rester des placeholders.** Le pipeline sort aujourd'hui des créatures que la charte accepte. Le gardien est trois fois plus grand : G1 dira si la méthode tient à cette taille, capture à l'appui.

## 6. Compte rendu C1, le bon endroit et le bon moment — 25 octobre 2026

- **La ligne d'objectif** passe en haut au centre, sous la Série, à la place du nom de biome de la grande carte (`LandObjective`). Elle s'éclaire un instant à chaque changement et s'efface sous le carton, qui occupe la même place.
- **La flèche d'objectif** (`ObjectivePointer`) : un losange au bord de l'écran avec l'icône des Mémoriaux et une pointe qui bat, dorée vers le Mémorial endormi le plus proche, violette vers le repaire quand il est prêt. Si le but est à l'écran, c'est un chevron qui flotte au-dessus. Rien pendant un cercle tenu, à moins de 64 px du but (l'invite prend le relais), pendant le combat ni au passage.
- **Le carton d'arrivée** ajoute la règle sous le lieu : « Ravive 3 Mémoriaux, puis réveille le gardien » (sur la terre du late game : « Les échos des gardiens te traquent : tiens bon »).
- **Le carton du gardien prêt**, à la dernière lumière : « 3 Mémoriaux ravivés », « Le gardien t'attend », « Rejoins le cœur, et réveille-le », avec le son du réveil de la Barrière, plus grave et plus bas (`ready_audio`). Il attend la fin de l'écran des bénédictions, et s'efface aussitôt si le combat commence.
- **Les bandeaux de quête** descendent sous le carton (`StreakHud.Bottom + 100`) : ils recouvraient sa nouvelle ligne au passage d'une terre.
- **Pas de carton quand le gardien se lève seul**, contrairement au §2 : la barre de boss paraît au même endroit au même instant. La ligne d'objectif passe au rouge (« Le gardien s'est levé seul »), avec la secousse et le son du réveil. L'alerte d'une minute avant vient avec C2.

**Vérifié** : build sans avertissement, smoke test vert. Captures en vraie run (`--lands-bot --nomad`, graine 7, deux terres) :
- carton d'arrivée avec sa règle ;
- ligne d'objectif en haut au centre ;
- flèche dorée vers les Mémoriaux, violette vers le repaire ;
- carton du gardien prêt, puis barre de boss ;
- carton de la terre 2 dégagé.

Trace headless sur deux terres : la flèche trouve sa cible sur chaque terre et se tait pendant les cercles. Le chevron à l'écran n'a pas été vu en capture, parce que le bot entre aussitôt dans le cercle : à regarder en jouant.

## 7. Compte rendu G0, le gardien devient une entité — 25 octobre 2026

- **Un rang à part** : `tier: guardian` dans `data/enemies/`, avec un bloc `guardian` obligatoire pour ce rang et refusé ailleurs. Le bloc contient `moves` (les coups de référence qu'il prend) et `body_scale`.
- **Chaque biome nomme son gardien** (champ `guardian`, obligatoire). Une créature de rang guardian est refusée dans les listes de créatures d'un biome : seul son repaire la fait naître.
- **`GuardianDirector` pose le gardien du biome du repaire**, sans variante ni affixe. Son escorte vient des créatures de la terre.
  - **PV** : ceux de sa fiche × `hp_rank_growth` par terre franchie, puis × la force (lumières en plus, rage, écho). Le plancher et le plafond du Souverain disparaissent (`hp_multiplier`, `min_hp`, `hp_cap_floor_ratio`) : le plafond jouait à chaque réveil mesuré, donc les PV restent les mêmes (1 950, 2 632, 4 442 enragé à la 3e terre).
  - **Coffres** : une liste (`chests`). L'épique du gardien, plus le rare que lâchait le Souverain.
- **Les coups se filtrent selon la fiche** (`GuardianMoves`). L'onde n'arrive qu'à la deuxième phase, et seulement si le gardien la prend.
- **Rang lourd** (`EnemyGrammar.IsHeavy`), comme les mini-boss et les boss. Le gardien ne subit ni recul, ni percussion, ni poussée de la foule, ni leurre, et un gel ne coupe pas ses coups. Sa mort secoue l'écran comme celle d'un mini-boss.
- **Il prend la suite du Souverain**. Sa mort :
  - compte pour les quêtes de Souverains (§196) ;
  - lâche un objet et un bonus au sol ;
  - compte au bilan.

  Il reste une cible prioritaire : l'onde d'un bonus ne lui ôte qu'un quart.
- **Les échos du late game reprennent les gardiens vaincus**, chacun son tour.
- **Le Grand Cerf** (`grand_cerf.json`) sert à toutes les terres jusqu'aux lots G2 à G5. Il prend la charge et la frappe. Son corps est provisoire : celui du Tréant, à l'échelle 2, en attendant G1.

**Vérifié** :
- build sans avertissement ;
- suites `smoke`, `catalogs`, `enemy_abilities`, `lands`, `quests`, `item_loot`, `field_bonuses` et `music` vertes ;
- nouveaux cas de refus : gardien sans bloc ou hors rang, coup inconnu ou en double, échelle absente, gardien dans une foule, gardien de biome absent, ordinaire ou inconnu ;
- capture en vraie run (graine 7) : barre de boss « Le Grand Cerf », corps à l'échelle 2, frappes au sol.

**Mesure** : bot des terres, profil neuf, 30 min, graines 7 et 42, même protocole que B1 (plan 56 §10). Durée du combat, du réveil à la mort :

| Cas | Terre 1 | Terre 2 | Terre 3 | B1 (Souverain), terres 1 / 2 / 3 |
|---|---|---|---|---|
| Traqueur, graine 7 | 72 s | 125 s | enragé, non vaincu après 11 min | 73 / 105 / enragé, non vaincu |
| Traqueur, graine 42 | enragé, 190 s | 346 s | — | enragé 90 / 409 / — |
| Forgeuse, graine 7 | 83 s | 121 s | enragé, 572 s, **run gagnée vers 29:20** | 82 / 112 / enragé 60, gagnée à 19:22 |
| Forgeuse, graine 42 | enragé, 79 s | 150 s | — | enragé 212 / 91 / enragé à 29:48 |
| Vagabond, graine 7 | 94 s | 130 s | enragé, non vaincu après 10 min | 110 / 264 / 324, gagnée à 18:54 |
| Vagabond, graine 42 | enragé, 132 s | 83 s | enragé à 29:46 | enragé 139 / 87 / enragé à 28:27 |

Lecture :
- **Terres 1 et 2** : 10 combats sur 12 entre 72 et 150 s, comme au B1. Les deux plus longs (190 et 346 s) font écho à ceux du B1 (212 et 409 s).
- **La 3e terre reste le point faible du bot**, comme au B1 : il n'atteint pas le repaire à temps et le gardien se lève enragé, à 4 442 PV, au milieu de 200 à 400 créatures. Une seule run gagnée, contre deux au B1. Les PV et l'arène sont les mêmes. Ce qui a changé :
  - le gardien ne recule plus et le gel ne coupe plus ses coups ;
  - sa charge l'emporte à 440 px du bot.

  G1 règle le combat du Cerf sur son vrai corps et ses charges en chaîne (« 1 à 2 min au bot », §3), et le revérifie sur la 3e terre.

## 8. Compte rendu G1, le Grand Cerf fini — 27 octobre 2026

- **Son corps** (`tools/sprites/creatures/grand_cerf.py`) est procédural, par le pipeline des créatures :
  - cadre de 176 × 160, environ trois fois une créature ;
  - masse d'écorce sombre, crâne blanc, ramure de bois mort avec un andouiller cassé, chiffon rouge noué à un bois, yeux violets ;
  - cinq animations en 8 directions : attente, marche, élan tête basse, frappe cabrée puis retombée, chute sur le flanc.

  Le pipeline gagne deux réglages par modèle : la portée des rayons et la boîte qui les borne (`RAY_RANGE`, `BOUNDS`). L'élan est une action facultative du générateur et du chargeur (`charge`), que seul un modèle qui la définit produit.
- **Dessiné à sa taille** : `body_scale` disparaît au profit de `body_radius`, rayon de son corps pour les collisions et les coups. Le gardien n'est plus agrandi.
- **Son coup à lui, `chain_charge`** (réglages dans `data/world/guardian.json`) :
  - dès sa deuxième phase, il enchaîne trois charges ;
  - chaque charge est annoncée par sa ligne et visée sur le joueur au début de l'annonce. La première s'annonce en 1 s, les suivantes en 0,6 s ;
  - la dernière montre aussi le disque où elle finit. Il y frappe, et le vide s'y ouvre 12 s. Le joueur qui s'y trouve est touché ;
  - puis 1,8 s de récupération pour riposter.

  Prête, la chaîne passe avant la frappe et la charge simple. Sans joueur à viser, elle finit tout de suite par sa frappe.
- **Une mort mise en scène** : un arrêt sur image plus long, le corps couché qui reste 1,1 s, puis une dissolution lente. Le son de chute vient de sa fiche (`death_audio`, obligatoire), à sa mort comme à celle de ses échos.
- **Sons** : ceux de la banque, sans nouveau fichier. La charge de créature annonce la chaîne, le poing de la Barrière frappe, et la défaite de la Barrière fait sa chute.
- **Le repaire montre le gardien qui dort** : son corps couché, vu de profil (dernière image de sa chute vers l'ouest), teinté de violet sous la colonne de lumière, à la place de la stèle. La stèle reste en repli pour un gardien sans sprite.

**Vérifié** :
- build sans avertissement ;
- suites `smoke`, `catalogs`, `enemy_abilities`, `lands`, `quests`, `item_loot`, `field_bonuses` et `music` vertes. Nouveaux contrôles :
  - le repaire montre le gardien couché ;
  - le gardien a le rayon de sa fiche et n'est plus agrandi ;
  - en deuxième phase, la chaîne part et finit en ouvrant le vide ;
  - cas de refus : rayon absent ou nul, son de chute inconnu, coup inconnu ;
- planche des 168 images regardée ;
- capture en vraie run (graine 7) : le Cerf couché au repaire sous sa colonne, puis debout en combat, avec sa barre de boss, ses frappes et l'élan de ses charges. Sa silhouette se lit au milieu de la foule.

**Mesure** : même protocole qu'au G0 (§7). Durée du combat, du réveil à la mort :

| Cas | Terre 1 | Terre 2 | Terre 3 | G0, terres 1 / 2 / 3 |
|---|---|---|---|---|
| Traqueur, graine 7 | 74 s | 106 s | enragé, non vaincu après 676 s | 72 / 125 / non vaincu |
| Traqueur, graine 42 | enragé, 182 s | 233 s | — | enragé 190 / 346 / — |
| Forgeuse, graine 7 | 92 s | 142 s | enragé, non vaincu après 586 s | 83 / 121 / 572, gagnée |
| Forgeuse, graine 42 | enragé, 145 s | 124 s | — | enragé 79 / 150 / — |
| Vagabond, graine 7 | 98 s | 177 s | enragé, non vaincu après 558 s | 94 / 130 / non vaincu |
| Vagabond, graine 42 | enragé, 228 s | 130 s | — | enragé 132 / 83 / — |

Lecture :
- **Terres 1 et 2** : 9 combats sur 12 entre 74 et 177 s, en moyenne 144 s contre 134 au G0. Les trois plus longs (182 à 233 s) suivent un réveil enragé, ou la chaîne qui emporte le Cerf loin du bot.
- **La 3e terre ne se mesure pas avec ce bot.** Il n'atteint pas sa 3e lumière avant la rage, et la terre se réduit à son cœur. Dans les trois runs de la graine 7, le bot reste ensuite sur place : 7 m parcourus en cinq minutes. Le gardien ne passe même pas en deuxième phase : le bot ne l'atteint pas. C'est un défaut de l'outil, déjà présent au G0 : invincible, le bot survit hors du cœur au lieu d'y revenir. Le corriger rejoint le lot C2, qui traite la rage, pour revoir la 3e terre.

**Avocat du diable (§5) :** la méthode procédurale tient à trois fois la taille d'une créature, capture à l'appui. Le corps reste simple de près ; l'ami artiste le reprend à l'étape 7.

## 9. Compte rendu C2, ce qui restait flou — 27 octobre 2026

- **Les Mémoriaux sur la minimap dès l'arrivée**, même dans le brouillard : ceux à raviver en pierre pâle, ceux ravivés en or. Loin, un Mémorial à raviver s'épingle au bord du radar, du côté où le chercher. La légende de la carte entière distingue les deux. Hors des terres, rien ne change.
- **Le repaire** s'épingle aussi au bord du radar. Prêt à réveiller, il s'éclaircit et bat : une onde violette qui s'élargit chaque seconde.
- **L'alerte d'une minute.** Dans la dernière minute avant que le gardien se lève seul, l'objectif passe au rouge et compte à rebours : « Terre 1 · Le gardien se lève seul dans 0:44 ». Le temps suit le rythme de l'oubli du moment (`ErasureManager.FrontRate`). Un son marque l'entrée dans l'alerte, une fois par terre ; chaque seconde fait battre la ligne. Réglages : `rage_warning_seconds` (60) et `rage_warning_audio` dans `data/world/guardian.json`. Nouveau signal `GuardianRageCountdown`.
- **L'arène s'ouvre au plus à 40 créatures.** Averti, le joueur rejoint le cœur, et sa horde avec lui : au bot, 150 à 450 créatures. Elles faisaient au gardien un rempart que les armes ne traversaient pas. Au réveil, celles en trop, les plus loin du joueur, s'en vont sans butin, comme celles du sol qui s'effondre. Le plafond `fight_crowd_cap` ne valait jusque-là que pour les renforts.
- **Le bot des terres** rejoint le cœur pendant l'alerte, et relève son combat toutes les 10 s (`[GuardianFight]` : distance, PV, sol sous lui, foule, dernier coup reçu).

**Vérifié** :
- build sans avertissement ; suites `smoke`, `catalogs`, `lands`, `enemy_abilities` et `quests` vertes. Nouveaux contrôles : l'objectif compte à rebours dans la dernière minute et cesse au réveil ; 60 créatures amenées au cœur, l'arène s'ouvre à 44 (40 et l'escorte) ;
- capture en vraie run (graine 7, `--capture-lands`) : l'objectif rouge sous la Série, lisible ; la minimap montre quatre Mémoriaux pâles et le repaire épinglés au bord. Le battement du repaire prêt n'est pas vu en image (un écran de niveau couvrait la capture) ;
- relecture `godot-reviewer` : rien de bloquant ; le son d'alerte pouvait se rejouer si l'oubli ralentissait, corrigé (une fois par terre, l'alerte ne s'éteint plus avant le réveil) ; les Mémoriaux s'affichaient aussi hors des terres, corrigé.

**Mesure** : même protocole qu'au G0 et au G1, profil neuf, 30 min. Durée du combat, du réveil à la mort :

| Cas | Terre 1 | Terre 2 | Terre 3 |
|---|---|---|---|
| Traqueur, graine 7 | 60 s | 153 s | enragé, non vaincu (bot figé, ci-dessous) |
| Traqueur, graine 42 | enragé, 681 s dont 350 figé | 266 s | — |
| Forgeuse, graine 7 | 103 s | 62 s | **enragé, 40 s, run gagnée à 19 min 44** |
| Forgeuse, graine 42 | enragé, 221 s | 101 s | — |
| Vagabond, graine 7 | 97 s | 169 s | enragé, non vaincu (bot figé) |
| Vagabond, graine 42 | enragé, 182 s | 94 s | — |

Deux runs de mise au point, avant le plafond de l'arène, ont aussi vaincu la 3e terre : Vagabond graine 7, enragé, 194 s ; Traqueur graine 7, trois lumières, 256 s, run gagnée à 13 min 45.

Lecture :
- **La 3e terre se mesure enfin** : trois fois vaincue sur cinq essais à la graine 7, de 40 à 256 s. Au G1, jamais.
- **Terres 1 et 2** : 12 combats, de 60 à 266 s hors le cas figé. Les plus longs suivent toujours un réveil enragé.
- **Deux limites du bot, pas du jeu** :
  - l'arc du Traqueur tire droit devant, dans le sens de la marche ; le bot, qui tourne autour du gardien pour garder sa place, tire souvent à côté ;
  - trois fois, le bot reste figé sur une case de vide au bord du cœur, le gardien figé à côté de lui, 5 à 10 min. **À comprendre en premier au lot G2** : un vrai joueur peut-il s'y coincer ?
- **Graine 42** : le bot n'a pas ses trois lumières avant la rage sur la 1re terre (premier Mémorial à 2 min 40), comme au G0 et au G1.


## 10. Retours de Raphaël après la visite des cinq terres — 27 octobre 2026

> « Il ne faut pas afficher en permanence "terre 1 - mémoriaux ravivés 0/3" sur l'écran […] ça bouffe trop de place. Le "terre 1" déjà il faut afficher le nom de la terre, et le mettre qu'au tout début, et ensuite le compteur de mémoriaux, mets-le dans un coin ou un autre endroit plus discret. Ensuite la grosse flèche moche, elle sert à quoi ? Pour moi elle n'a pas lieu d'être (j'ai compris qu'elle affiche les mémoriaux). Autre truc : raviver les mémoriaux, y'a aucun suspense, et on comprend rien (combien de temps il faut rester, etc.). Ensuite, en effet, les cartes font très vides / linéaires pour la plupart, même s'il y a de bonnes bases (j'ai regardé les 5 existantes). »

C1 avait mis l'objectif en haut au centre et une flèche au bord de l'écran. Les deux sont de trop une fois le chemin compris : la minimap (C2) suffit à trouver les Mémoriaux et le repaire.

| Lot | Contenu | Effort |
|---|---|---|
| **C3** | **Le HUD se tait.** Plus de ligne d'objectif permanente. Le carton d'arrivée donne le nom du lieu (« Les bois de la colline ») et la règle, puis s'efface. Le compteur devient trois petites icônes de Mémorial collées à la minimap, éteintes puis dorées ; à trois, l'icône du repaire s'y allume et bat. Le haut de l'écran ne parle qu'aux changements (Mémorial ravivé, gardien prêt, gardien levé seul) et dans la dernière minute avant la rage, qui reste en rouge. **La flèche d'objectif disparaît** ; les flèches des coffres restent | Faible |
| **C4** | **Raviver tient en haleine.** Lisible : l'anneau se remplit comme une horloge autour du Mémorial, les secondes restantes au-dessus ; hors du cercle, l'anneau rougit et se vide, avec un son. Du suspense, en trois temps : à un tiers puis à deux tiers, une vague monte du bord du cercle, annoncée au sol ; au dernier tiers, le cercle se resserre de moitié et le son monte ; au bout, une onde de lumière repousse et blesse la foule autour, le Mémorial s'allume. Réglages dans `landmarks.json`, mesure au bot (durée réelle, coups reçus) | Moyen |

Les cartes « vides et linéaires » vont à l'étape 5 (terres complètes), qui part de ce retour.

## 11. Compte rendu C3, le HUD se tait — 27 octobre 2026

- **Plus de ligne d'objectif permanente.** La ligne en haut au centre ne parle plus qu'aux changements, 3,5 s puis elle s'efface : « Mémorial ravivé · 2/3 », « Le gardien s'est levé seul », « La terre suivante se recoud », et au late game « Un écho de gardien te traque », « Échos vaincus : 1 ». Le carton d'arrivée donne déjà le lieu et la règle, celui du gardien prêt l'annonce, et la barre de boss nomme le combat : pas de ligne pour eux. « Terre 1 · » disparaît des textes.
- **Seule l'alerte de rage reste**, en rouge, la dernière minute (« Le gardien se lève seul dans 0:44 »).
- **Le compteur passe sur la minimap** (`LandMemorialPips`) : une icône par Mémorial à raviver, pâle puis dorée, avec un petit saut quand elle s'allume ; à droite, le repaire en violet éteint, qui s'allume et bat quand le gardien attend. Caché au passage et au late game.
- **La flèche d'objectif est retirée** (`ObjectivePointer` supprimé). Les flèches des coffres restent.

**Vérifié** : build sans avertissement ; suites `smoke`, `lands` et `development_tools` vertes, avec de nouveaux contrôles (compteur affiché et plus de flèche ; compte à rebours seul en haut ; au late game, pas de compteur, puis l'écho et les échos vaincus annoncés) ; capture en vraie run (graine 7, `--capture-lands`) : le haut de l'écran est libre pendant le cercle ; l'alerte rouge seule pendant la dernière minute ; les quatre icônes se lisent sur le bord de la minimap sans la couvrir.

## 12. Compte rendu C4, raviver tient en haleine — 27 octobre 2026

- **On sait combien de temps il reste.** Un anneau-horloge de perles fait le tour du cercle : il part du haut et se remplit dans le sens des aiguilles. Au-dessus du Mémorial, « Tiens le cercle · 12 s ». Hors du cercle, le bord et l'anneau rougissent, le texte devient « Reviens dans le cercle » et un signal sonore retentit (au plus toutes les 1,5 s).
- **Deux vagues.** Au tiers et aux deux tiers, des marques au sol s'annoncent sur le bord pendant 1,2 s, puis 8 créatures du lieu en sortent.
- **Le cercle se resserre** au dernier tiers, jusqu'à 55 % de son rayon.
- **L'onde.** Au bout, le Mémorial ravivé repousse les créatures à 380 px et leur ôte 60 % de leurs PV, avec un grand éclat doré ; les gardiens n'y sont pas sensibles.
- Tout se règle dans `memorial.circle` (`data/world/landmarks.json` : `waves`, `closing`, `burst`, `leave_audio`), avec 20 s de tenue.

**Vérifié :**
- build sans avertissement ;
- suite `lands` verte, avec de nouveaux contrôles : texte du temps restant et du retour, deux vagues sur deux, témoin blessé ou repoussé par l'onde, puis Mémorial ravivé et bénédiction choisie ;
- capture en vraie run (graine 7, `--capture-lands`) : l'horloge et le texte se lisent ; l'anneau rougit hors du cercle ; la deuxième vague s'annonce par des marques pleines de 20 px (les anneaux étaient trop pâles) ; l'onde éclate en or.

**Mesuré au bot** (Traqueur, graines 7 et 42, `--lands-bot --nomad`, comparé à C2) :

| | avant C4 | après C4 |
|---|---|---|
| Mémoriaux ravivés, graine 7 | 29, 54, 97 s | 29, 54, 97 s |
| Mémoriaux ravivés, graine 42 | 162, 200 s | 162, 200 s |
| Coups reçus par minute (4 premières minutes), graine 7 | 1 080 | 3 240 |
| Coups reçus par minute (4 premières minutes), graine 42 | 4 192 | 4 895 |
| Éliminations, graines 7 et 42 | 394, 411 | 525, 498 |

- Chaque vague fait sortir 7 à 8 créatures, et l'onde en touche de 31 à 195.
- Le ravivage n'est pas plus lent, mais il est plus disputé.
- Le dosage est à régler à la séance de jeu de Raphaël.

**Relecture :**
- corrigé :
  - `closing.from` à 1 donnait une division par zéro, il est maintenant refusé au chargement ;
  - le signal de sortie se répétait à chaque image ;
  - les marques de vague étaient créées à chaque tentative, elles le sont maintenant à la première vague.
- non corrigé : un niveau gagné pendant la tenue ouvre son écran, comme avant C4, car l'XP des vagues tombe en orbes.

## 13. Compte rendu G2, l'Épouvantail — 27 octobre 2026

- **Son corps** (`tools/sprites/creatures/epouvantail.py`), procédural par le pipeline des créatures :
  - grand épouvantail planté sur un pieu, dans un cadre de 208 × 176 ;
  - traverse pour épaules, manteau de laine bleu délavé rapiécé de rouge, paille qui sort des manches et du col ;
  - tête en sac de toile cousu où deux fentes brûlent de violet, chapeau de paille écrasé, faux rouillée ;
  - deux corbeaux, l'un sur la traverse, l'autre sur le chapeau.

  Quatre animations en 8 directions (136 images) :
  - l'attente, où il oscille sur son pieu ;
  - la marche, par bonds ;
  - la frappe, où il arme la faux d'un tour de buste ;
  - la mort.

  À la mort, il s'affaisse le long de son pieu en penchant, son chapeau tombe et les corbeaux s'envolent. Une première chute sur le côté sortait du cadre vers la caméra : elle a été remplacée par l'affaissement.
- **Ses coups** : la frappe et l'onde de référence, plus **le fauchage** (`moves.sweep` dans `data/world/guardian.json`) :
  - dès la première phase, 4 s après le réveil puis toutes les 9 s, si le joueur est à moins de 280 px ;
  - il s'arrête et annonce en 1,3 s un disque de 230 px autour de lui, ouvert d'une brèche de 90° ;
  - la brèche est à 120° du joueur, d'un côté puis de l'autre : il faut tourner autour de lui pour s'y glisser, ou traverser en dash ;
  - la faux balaie tout le disque sauf la brèche (1,8 × ses dégâts), puis il récupère 1,2 s.
- **L'annonce en secteur** : `GroundTelegraph.ShowSector`, avec un paramètre `gap_half` du shader `pixel_fx`. La brèche dessinée et la zone épargnée se mesurent au même angle au sol.
- **Les champs le prennent** (`data/biomes/wild_fields.json`). Les autres terres gardent le Grand Cerf jusqu'à leur lot.
- **Sons de la banque** : la chaîne de la Barrière annonce et frappe le fauchage, la défaite de la Barrière fait sa chute.
- **Le bot de mesure se bat mieux** (reste du §201) :
  - arrivé à sa place, il marche vers le gardien jusqu'au contact, pour que l'arc et la lame visent ;
  - immobile plus de 3 s sans toucher, il fait un détour de l'autre côté du gardien.

**Vérifié :**
- build sans avertissement ;
- suites `smoke`, `catalogs`, `lands`, `enemy_abilities` et `quests` vertes. Nouveaux contrôles :
  - le fauchage est annoncé, touche le joueur dans la faux, épargne la brèche et le dehors ;
  - la brèche suivante s'ouvre de l'autre côté ;
  - les échos du late game reprennent les gardiens vaincus dans l'ordre (premier écho : le gardien de la première terre) ;
- capture en vraie run (graine 7, `--capture-lands --land-order wild_fields,...`) :
  - l'Épouvantail couché au repaire, puis debout dans la foule avec sa barre de boss ;
  - l'annonce du fauchage, disque pâle ouvert de sa brèche ;
  - la frappe en violet plein, brèche nette ;
  - la capture gagne les images `lands-4d-sweep.png` et `lands-4e-sweep-strike.png`, et choisit les bénédictions au passage.

**Mesuré au bot** (Forgeuse, graines 7 et 42, `--lands-bot --nomad`, champs en première terre ; durées à 10 s près) :

| | Champs (Épouvantail) | Terre 2 (Cerf) | Terre 3 (Cerf, enragé) |
|---|---|---|---|
| Graine 7 | 90 s | 50 s | non vaincu après 610 s |
| Graine 42 | 130 s | 150 s | 300 s, run gagnée |

Lecture :
- les deux combats de l'Épouvantail tiennent dans la cible du Cerf (1 à 2 min) ;
- la 3e terre reste hors de portée du bot à la graine 7, comme au G1 ;
- au late game, le premier écho est l'Épouvantail.

**Relecture :** rien de bloquant. Restent quatre finitions, laissées en l'état :
- la marque du fauchage est créée pour tout gardien, comme les autres marques ;
- deux animations d'attaque s'enchaînent quand la faux touche ;
- le côté de la première brèche n'est pas remis à zéro d'un combat à l'autre ;
- le test lit des champs privés par réflexion.

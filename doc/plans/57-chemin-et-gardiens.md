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
| **C2** | **Ce qui reste flou.** Sur la minimap, les Mémoriaux de la terre se voient dès l'arrivée, ravivés en or. Le repaire bat quand il est prêt. Une minute avant que le gardien se lève seul, l'objectif passe au rouge (« Le gardien se lève dans 1:00 ») | Faible |

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

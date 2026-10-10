# Plan 34 — Le late game : après l'Indicible

10 octobre 2026, nuit. Étude demandée par Raphaël au [§108](DECISIONS.md) : « pour moi le late game c'est ce qui se passe après l'Indicible […] à partir de 20 min on est en mid-game et à 30 on est déjà au début du late game. Mais je pensais plus au late game genre qu'est-ce qu'on prévoit, quoi ? ». **Rien n'est implémenté.** Ce plan dit ce qui existe, ce que les documents promettaient, et propose un contenu à trancher (§5).

## 0. En bref

- **Découpage de la run retenu** (calendrier du [plan 33 §8](33-equilibrage-ouverture.md)) :
  - début de 0 à 10 min environ (ouverture, premier Souverain, première Résurgence) ;
  - milieu de partie de 10 à 30 min (Barrière à 15:00, trois Résurgences, trois Souverains) ;
  - **late game à partir de l'Indicible, à 30:00**, sans fin imposée.
- **Aujourd'hui, le late game n'a aucun contenu propre.** Après la mort de l'Indicible :
  - les multiplicateurs montent (créatures ×1,85, PV ×1,55, dégâts ×1,32, un tiers d'affixes) ;
  - les Résurgences reviennent environ toutes les 4 min 45 (de début à début) au lieu de 6 min, et durent 80 s ;
  - l'Effacement continue jusqu'au Néant.

  Ni créature, ni événement, ni récompense, ni décor nouveau : c'est la même run, plus dure.
- **Ce que la stratégie V2 (§14) promettait :**
  - un monde « de mémoire résiduelle », fait de fragments ;
  - plus de temps calme ;
  - un meilleur butin et de l'Essence en abondance ;
  - un score qui s'accélère ;
  - une fin seulement à la mort.

  Raphaël a ajouté depuis : une excellente run vise les niveaux 300 à 400 en 45 min (§72), avec des foules de plusieurs centaines de créatures (§72, §83).
- **Proposition** : un late game en trois couches, toutes construites sur des systèmes existants.
  1. **La Profondeur**, une escalade lisible minute par minute, qui fait la course au classement.
  2. **Les Marées**, un rythme en vagues et respirations ponctué d'Échos-Souverains.
  3. **Le monde d'après** : la carte change de nature, les Échos des héros deviennent hostiles.

  Plus tard, en option : **le Choix**, une sortie volontaire qui clôt la run.

## 1. Ce qui existe (code du 10 octobre)

| Élément | Où | Effet après l'Indicible |
|---|---|---|
| Phase `Endgame` | `EndgameManager`, `GameManager.ReportEndgame` | Bascule à la mort de l'Indicible, pour toute la suite |
| Tempo des Résurgences | `crises.json` : `endgame_interval_multiplier` 0,7, `endgame_duration_multiplier` 1,15 | Une Résurgence environ toutes les 4 min 45 de début à début (au lieu de 6 min), 80 s chacune |
| Foule et solidité | `spawn_flow.json` : `endgame_spawn_multiplier` 1,85, PV ×1,55, dégâts ×1,32 | Plus de tout, sur la pente des PV (+13 %/min) |
| Affixes | `_variants.json` : `affix_chance_endgame` 0,34 | Un tiers des créatures portent un affixe |
| Péril du temps | `spawn_flow.json` : `time_peril_per_minute` 0 | Prévu (plan 28 P4), **éteint** |
| Plafond de créatures | `peril.json` : 500 actives au plus | Les foules de late game sont possibles (plan 29) |
| Score | `score.json` : éliminations seules | Plus de créatures donne plus de points, sans bonus propre au late game |
| Musique | `MusicDirector` | Une intention « endgame » existe (plan 15) |
| Quête | `beyond_climax` | « Survivre à une Résurgence après la chute de l'Indicible » |
| Échos | `erasure.json` → `echoes` | Silhouettes des trois héros et d'habitants qui murmurent en zone oubliée, sans effet de jeu |

Le reste (pools de créatures, butin, coffres, lieux, événements) est identique avant et après.

## 2. Ce qui manque pour que le late game tienne

1. **Une raison de rester.** Sans fin et sans récompense propre, survivre une minute de plus ne vaut que des éliminations. Le classement (plan 09) compte les éliminations, ce qui est un bon début, mais le joueur ne voit rien monter.
2. **Une forme.** Une rampe sans relief lasse vite : c'était le constat du plan 30 sur les 15 premières minutes, et il vaut encore plus ici.
3. **Une identité.** Le joueur vient de tuer le boss final ; le monde devrait le montrer.
4. **La puissance qui s'exprime.** Le plan 20 vise des cascades de niveaux et des builds qui « cassent » le jeu en fin de run. Il faut des cibles à la hauteur : grandes foules, créatures solides, élites en nombre.

## 3. Repères dans le genre

- **Vampire Survivors** : une fin dure à 30 min (la Faucheuse), le late game est la course jusqu'à elle ; des modes à part (Endless, Hurry) changent la durée.
- **Megabonk** : des étapes courtes rythmées par des mini-boss, des nuées et un boss (plan 30 §4), avec une fin connue.
- **Brotato** : un nombre de vagues fixé, puis des niveaux de difficulté pour la rejouabilité.

Vestiges a choisi une fin seulement à la mort (stratégie §7, « just one more run »). Le late game doit donc rendre chaque minute **différente de la précédente** et **mesurable** par le joueur.

## 4. Principes proposés

- **Tout est visible et compté** : le joueur sait à quelle profondeur il est et ce que la minute suivante lui coûtera.
- **Des systèmes existants avant du neuf** : Péril, affixes, Souverains, Échos, Effacement, plafond de 500 créatures.
- **Le late game est l'endroit des builds extrêmes** : niveaux qui cascadent, armes plafonnées, objets empilés. On lui donne des foules à faucher, pas seulement des PV.
- **Une fin choisie plutôt qu'imposée**, en option : clore la run sur une décision.

## 5. Propositions

### L1 — La Profondeur : l'escalade lisible (recommandé, priorité 1)

Après l'Indicible, chaque minute ajoute **un point de Péril du temps** (`time_peril_per_minute`, déjà en données). Ce point pèse sur le nombre de créatures d'abord, puis sur PV et dégâts, et **multiplie le score**, contrairement au Péril du temps d'aujourd'hui. Le HUD montre un compteur « Profondeur N » à la place de l'horloge, ou à côté. Les paliers de 5 (5, 10, 15…) sont annoncés : un son, une ligne de lore courte (plan 19 C, « moments-seuils »).

- **Pourquoi** : la course au classement devient lisible (« j'ai tenu jusqu'à la Profondeur 14 ») et chaque minute est différente de la précédente.
- **Coût** : faible. Compteur, annonce, branchement du Péril du temps sur le score.

### L2 — Les Marées : un rythme en vagues (recommandé, priorité 1)

Les Résurgences de late game deviennent des **Marées** : 60 s de déferlement à plafond relevé, puis 20 à 30 s de reflux où la foule recule ou se dissout, avec un coffre au point le plus calme. Toutes les 4 à 5 min, un **Écho-Souverain** se lève : un Souverain déjà vaincu revient avec deux affixes, une barre de PV, et laisse un coffre épique.

- **Pourquoi** : la forme que demandait le plan 30 (pics et creux), en réutilisant Résurgences, rendez-vous fixes et affixes.
- **Coût** : moyen. Reflux (retrait ou dissolution d'une part de la foule), rendez-vous en boucle, variante d'Écho.

### L3 — Le monde d'après (recommandé, priorité 2)

La mort de l'Indicible change la carte :
- la lumière tombe, puis le monde alterne entre éclats de couleur (souvenirs) et blanc du Néant (stratégie §14) ;
- le Néant ne blesse plus, mais tout s'y oublie : pas de coffre, pas de lieu ;
- des **îlots de mémoire** surgissent devant le joueur, à rejoindre. Ils portent les coffres, l'Essence abondante et les Autels restants.
- **les Échos se retournent** : les silhouettes des trois héros et des habitants (déjà dessinées et animées pour les échos du plan 16) deviennent des élites hostiles, au moveset de leur arme. Ce sont des miroirs du joueur.

- **Pourquoi** : une identité forte et un sens à l'exploration en late game (aller d'îlot en îlot). Les Échos ennemis sont une première créature propre au late game.
- **Coût** : élevé. Effet visuel global, génération d'îlots, IA des Échos. À découper.

### L4 — Le butin du late game (priorité 2)

- les coffres d'îlots et d'Échos-Souverains montent d'une rareté ;
- l'Essence tombe en abondance pour les derniers Autels (stratégie §14) ;
- quand toutes les armes sont plafonnées, les niveaux versent en « surplus » (plan 05) : petits gains de stats sans fin, pour que la cascade continue (§72).
- **Coût** : faible à moyen, surtout de l'équilibrage.

### L5 — Le Choix : une sortie volontaire (option, plus tard)

À la Profondeur 10, puis toutes les 10, un Mémorial d'or apparaît. Le toucher **clôt la run** : bilan de victoire, bonus de score, quête propre. Rester, c'est continuer à descendre. C'est l'option C de la stratégie §14.

- **Pourquoi** : une fin satisfaisante pour qui ne vise pas le record, sans imposer de fin.
- **Coût** : moyen. Bilan de victoire, règles de classement à préciser.

### L6 — Créatures propres au late game (plus tard)

En plus des Échos (L3), deux ou trois créatures nées du Néant (silhouettes blanches, morceaux de décors effacés qui marchent), introduites par paliers de Profondeur. Bestiaire à proposer au plan 07.

## 6. Ordre proposé

| Lot | Contenu | Vérification |
|---|---|---|
| **G1** | L1 Profondeur (Péril du temps branché sur le score, compteur, paliers annoncés) | Mesure 45 min au build de référence : créatures, éliminations, score par minute |
| **G2** | L2 Marées et Échos-Souverains | Chronologie du plan 30 prolongée à 45 min : pics et creux visibles |
| **G3** | L4 Butin | Niveaux et rareté par tranche de 5 min après 30 min |
| **G4** | L3 Monde d'après, en deux temps : visuel et Néant inoffensif, puis îlots et Échos hostiles | Captures, banc dense à 500 créatures |
| G5 | L5 Choix, L6 créatures | À trancher après G1–G4 |

## 7. Questions pour Raphaël

1. **Profondeur** : remplace-t-elle l'horloge en late game, ou s'y ajoute-t-elle ? Doit-elle multiplier le score ?
2. **Marées** : un reflux qui retire une part de la foule, ou seulement un plafond qui baisse et une foule qui s'use d'elle-même, comme aux Résurgences d'aujourd'hui ?
3. **Monde d'après** : le Néant inoffensif en late game te va-t-il ? Les Échos des héros en ennemis respectent-ils le lore (plan 19) ?
4. **Fin volontaire (L5)** : à garder dans le plan ou à écarter ?

# Plan 55 — Le rythme du départ

18 octobre 2026 · étape 3 du [LE-JEU.md](../LE-JEU.md) · [DECISIONS §193](DECISIONS.md) · effort faible.

## 1. Constat

Mesure au bot du §189 (profil neuf, joueur moyen, graines 7 et 42, 7 min) et du plan 52 S5 :
- 3,5 créatures à l'écran en moyenne la première minute, 17 à 25 % d'images vides ;
- premier niveau entre 23 s et 3 min 05 ;
- premier rang de la Série (×10) entre 2 min 36 et 4 min 28 : sans créatures à portée, la fenêtre de 5 s se vide avant l'élimination suivante.

Causes, dans les réglages :

| Réglage | Valeur | Effet à 1 min |
|---|---|---|
| Ouverture (`spawn_flow.json`) : répit 8 s, cible de départ 4, intervalle ×2, rampe de 300 s | Introduite au §154 contre un départ « trop nerveux » | Cible de créatures proches à 6,8 au lieu de 20 |
| Densité du début (`early_density_multiplier` 0,57 jusqu'à 10 min) | §171 et §184, « spammé d'ennemis » | La cible tombe à 3,9 ; une apparition toutes les 6 s |
| Premiers niveaux ×1,8 décroissant à ×1 au niveau 10 (`progression.json`) | §154, niveau 10 vers 4–5 min | 27 XP pour le premier niveau, 410 XP pour le niveau 5 |
| Aimant 80 px sans dérive | §172 | Les orbes tombent à portée d'arme, hors d'attraction |

Le §171 et le §184 visaient les minutes 1 à 10 (12 à 20 créatures visibles en moyenne, jugé correct par Raphaël). C'est l'ouverture, cumulée à cette densité, qui vide les trois premières minutes.

## 2. Ce qui change (lot R1)

**Écran plein tôt** (`data/scaling/spawn_flow.json`, et une ligne de `SpawnManager`) :
- l'ouverture peut partir **au-dessus** de la courbe normale : le code ne la borne plus par la cible normale ;
- répit 8 → 3 s ; cible de départ 4 → 26 ; intervalle ×2 → ×1 ; rampe 300 → 180 s ;
- la densité du début (×0,57) et la courbe d'après 3 min ne changent pas.

Créatures proches visées (rayon 700 px, Normal, sans Péril) :

| Temps | Avant | Après |
|---|---|---|
| 3 s | 0 (répit) | 15 |
| 1 min | 4 | 14 |
| 2 min | 7 | 15 |
| 3 min | 11 | 18 |
| 5 min et après | 25 | 25 (inchangé) |

**Premier niveau vite** (`data/scaling/progression.json`) : le facteur des dix premiers niveaux part de 0,4 au lieu de 1,8 et monte jusqu'à 1,15 au niveau 10 (au lieu de 1,0).

| | Avant | Après |
|---|---|---|
| Premier niveau | 27 XP (2 à 3 éliminations) | 6 XP (la première élimination) |
| Niveau 5, cumulé | 410 XP | 149 XP |
| Niveau 10, cumulé | 2 144 XP | 1 467 XP |
| Coût du niveau 10 → 11 | 474 XP | 545 XP |

La courbe rejoint l'ancienne vers le niveau 9 : l'écran plus plein rapporte plus d'XP, la visée du §154 (niveau 10 vers 4–5 min) doit tenir. Le coût reste croissant d'un niveau au suivant (545 au niveau 10, 547 au niveau 11).

**Aimant revu** : 80 → 100 px, toujours sans dérive. Les orbes tombent à portée d'arme, et la Série pousse à avancer.

## 3. Lots suivants

| Lot | Contenu | Effort |
|---|---|---|
| R2 | Mesure au bot, même protocole que le §189 et le plan 52 S5 : créatures visibles à 1, 2 et 3 min, premier niveau, niveau 5 et 10, premier rang de la Série, meilleure Série. Visées : premier niveau avant 15 s, ×10 avant 1 min, niveau 10 entre 4 et 5 min. Réglage d'un cran si une visée est manquée | Faible |
| R3 | Régressions (`tools/test_movement.sh`, `tools/test_enemy_abilities.sh`) et capture d'une vraie run à 30 s et à 1 min 30 | Faible |
| R4 | Séance de Raphaël : le départ est-il plein sans être « spammé » (§171) ? | Ton temps |

R1 est fait sans test, à la demande de Raphaël. R2 et R3 viennent avant le commit.

## 4. Compte rendu R2, mesure au bot — 19 octobre 2026

Profil neuf (`MEASURE_PROFILE=normal`), 7 min, graines 7 et 42, Vagabond, Forgeuse et Traqueur. Avant et après mesurés au même commit et avec le même outil, chacun dans sa copie de travail.

| | Avant | Après |
|---|---|---|
| Créatures visibles, 0 à 1 min (images vides) | 1,9 à 4,0 (27 à 43 %) | 10,9 à 13,2 (5 à 13 %) |
| Créatures visibles, 1 à 2 min | 6,8 à 11,0 | 12,3 à 30,4 |
| Créatures visibles, 2 à 3 min | 7,5 à 19,2 | 13,1 à 24,6 |
| Créatures visibles, 3 à 5 min | 13,3 à 39,4 | 18,3 à 33,8 |
| Premier niveau | 85 à 150 s | 29 à 83 s |
| Niveau 5 | 234 à 341 s | 92 à 252 s |
| Niveau 10 | atteint une fois sur six (353 s) | 283 à 330 s, cinq fois sur six |
| Premier rang de la Série (×10) | 126 à 337 s | 32 à 54 s à la graine 7, 183 à 272 s à la graine 42 |
| Meilleure Série | ×23 à ×92 | ×15 à ×189 |

Lecture :
- **écran plein tôt : atteint.** Environ 12 créatures visibles dès la première minute, au lieu de 3 ; après 3 min, la densité reste celle d'avant ;
- **niveau 10 vers 4–5 min : atteint**, entre 4 min 43 et 5 min 30, alors que le bot ne l'atteignait presque jamais en 7 min ;
- **premier niveau avant 15 s : manqué au bot, mais le bot ne juge pas cette visée.** La première élimination tombe entre 9 et 12 s à la graine 7, et 6 XP la paient. Or le bot de mesure ne va jamais chercher les orbes : il les ramasse quand il repasse dessus (18 s d'attente au Vagabond). Un joueur qui marche vers l'orbe monte au niveau 2 à la première élimination ;
- **×10 avant 1 min : atteint à la graine 7**, 32 à 54 s au lieu de 126 à 250 s ;
- **la graine 42 retarde tout, avant comme après** : sa première élimination tombe à 80 s (40 s au Traqueur). Le bot recule devant les Rampants d'Ombre et n'entre pas à portée de lame. C'est une limite du bot, pas du réglage.

**Décision par défaut (§193) : aucun cran de réglage.** Les visées que le bot peut juger sont atteintes ; la seule manquée dépend du ramassage, que le bot ne fait pas. La séance de Raphaël (R4) tranche la question du « spammé » (§171).

## 5. Compte rendu R3, régressions et capture — 19 octobre 2026

- `tools/test_movement.sh` et `tools/test_enemy_abilities.sh` verts.
- Capture d'une vraie run (Forgeuse, graine 7) : à 30 s, niveau 2, 5 éliminations, une douzaine de créatures autour du joueur ; à 1 min, niveau 5, Série ×25 FERVEUR, 42 éliminations. L'écran est occupé sans être noyé.

Reste R4, la séance de Raphaël.

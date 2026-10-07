# Plan 28 — Péril et difficulté

7 octobre 2026. Chantier ouvert après le plan 27, à la demande de Raphaël ([DECISIONS §74](DECISIONS.md), [§78](DECISIONS.md)).

## 1. Le problème

Avec un bon build (quatre armes quelconques, projectiles, Chance, XP, critique), la run devient trop facile vers 10 minutes. Raphaël veut surtout des ennemis plus forts, pas un joueur affaibli. Le levier principal est le **Péril** : une stat que le joueur monte lui-même pour durcir sa run. Doser sa difficulté devient une compétence.

Ce que le lot H du plan 21 a appris le 3 octobre (§17) : les PV des ennemis sont un levier faible tant que le build tient, puis brutal ; les dégâts suivent déjà (5 à 10 coups pour mourir toute la run) ; le levier qui reste est **le nombre d'ennemis qui atteignent le joueur**.

## 2. Ce qui existe

- Péril de 0 à 10 (`data/scaling/peril.json`). Par point : +6 % d'ennemis, +10 % de PV, +6 % de dégâts, +12 % d'XP, +12 % de score, un demi-cran de rareté.
- Sources : Failles (une carte épique ou mieux acceptée = +1 Péril et un Oubli), bannissements payants.
- Le « nombre d'ennemis » ne monte que la densité locale visée ; le plafond global d'ennemis actifs (110, +4 par minute) ne bouge pas avec le Péril.
- Le Péril n'est affiché que dans la fiche du joueur (menu pause).

## 3. Ce qui est décidé (§78)

- **Sans plafond.**
- **Le Péril ne paie pas directement.** Sa contrepartie, c'est plus d'ennemis, donc plus d'XP et d'Essence par les éliminations. Les bonus directs d'XP et de rareté par point sont retirés. Le score reste multiplié (classement).
- **Stèles** sur la carte : on maintient la touche, +1 Péril définitif. Une dizaine par carte.
- **Objet du Péril** : un emplacement d'objet qui ne fait que monter le Péril.
- En plus du Péril : ennemis plus forts, plus résistants et plus nombreux au fil de la run, Résurgences plus dures (§74).

## 4. Lots

### P0 — Mesure de référence

Protocole du plan 21 H0 : `MEASURE_EXTRA_ARGS="--nomad --visit --mortal --prefer souffle_du_neant,reflet_brise,memoire_vive,photo_de_classe,jeton_de_fete,oeil_critique"`, seeds 42, 1002 et 7, 25 min, à Péril 0 et à Péril 10. Relevés par tranche de 5 min (`tools/summarize_power.py`) : temps pour tuer, coups pour mourir, éliminations, niveau, Essence, ennemis proches.

**Mesuré le 7 octobre (Péril 0, code d'avant le plan 28)** :

| Tranche | Éliminations | Temps pour tuer (moyenne, étendue) | PV moyen d'une créature | Dégâts infligés /s | Niveau en fin de tranche |
|---|---|---|---|---|---|
| 0–5 min | 189 | 2,07 s (0,81–3,97) | 71 | 53 | 10 |
| 5–10 min | 558 | 0,62 s (0,39–0,99) | 196 | 367 | 19 |
| 10–15 min | 908 | 0,35 s (0,21–0,46) | 398 | 1 264 | 29 |
| 15–20 min | 1 055 | 0,34 s (0,23–0,44) | 1 009 | 3 141 | 39 |
| 20–25 min | 1 246 | 0,31 s (0,18–0,47) | 2 136 | 7 815 | 47 |

- **Le constat de Raphaël est reproduit** : à partir de 10 min, une créature meurt six fois plus vite qu'au début. Les PV des créatures sont multipliés par 30 sur la run, les dégâts du build par 150.
- Coups pour mourir : 5 à 15 selon la seed et la tranche ; le bot mortel « meurt » 12 à 95 fois par tranche (il n'esquive rien, survie non représentative, plan 21 H0).

### P1 — Péril sans plafond, recentré sur les ennemis

- `peril.json` : plus de `max` ; `xp` et `rarity_steps` retirés ; `score` gardé.
- **Nombre d'ennemis par point** : la densité locale visée **et** le plafond d'ennemis actifs montent ensemble, jusqu'à un plafond de coût fixé par le banc (`active_enemies_ceiling`). Au-delà, les points de Péril ne pèsent plus que sur les PV et les dégâts.
- **Affichage** : le Péril au HUD dès qu'il dépasse 0, près du score, qui s'éclaire quand il monte.
- Fiche du joueur : lignes du Péril alignées sur les nouveaux effets.
- Vérification : contrôles du chargeur, mesure P0 rejouée à Péril 0, 10 et 20, banc A/B au plafond de coût.


**Livré le 7 octobre.** Le Péril n'a plus de maximum (`PerilManager` borne seulement à 0). `peril.json` : `enemy_count_bonus_max` 1,5 (le nombre de créatures cesse de monter vers 25 points), `active_enemies_ceiling` 240 (provisoire, à fixer au banc de coût, machine calme), par point +6 % de créatures, +10 % de PV, +6 % de dégâts, +12 % de score. Le multiplicateur d'XP disparaît du signal `DifficultyModifierChanged` et de `PlayerProgression` ; `UpgradeRoller.BumpSteps` ne prend plus le Péril. `SpawnManager` relève le plafond de créatures actives avec le Péril, sans dépasser le plafond de coût ni descendre sous celui de la run. HUD : « Péril N » en rouge à droite du chrono, dès 1, qui s'éclaire à chaque point. Fiche de pause : créatures, PV, dégâts, puis score seul. Contrôles du chargeur réécrits (plafond, bonus de nombre, clé `xp` refusée) : 191 contrôles verts.

Point ouvert : le plafond de base atteint 210 créatures à 25 min, donc à 240 le Péril n'en ajoute plus qu'une trentaine en fin de run et pèse alors surtout sur la densité locale, les PV et les dégâts. Le plafond se fixe au banc (`/bench` avec `--peril`), machine calme.

### P2 — Stèles du Péril

- Nouveau lieu, placé comme les Failles (couronnes de distance au départ), une dizaine par carte ; maintenir la touche, +1 Péril définitif, la stèle s'éteint.
- Sprite procédural, icône de minimap, invite, son d'activation repris d'un lieu existant.
- Pas de Repère (premier usage sans gain), pour rester fidèle au §78.
- Vérification : capture de la stèle avant/après, mesure de stèles croisées par run.


**Livré le 7 octobre.** `PerilStele` (sur le modèle de la Faille) : obélisque sombre et penché, fente rouge qui palpite ; invite « Réveiller le Péril » (la police des invites n'a pas de « É » majuscule), maintien de 0,8 s, +1 Péril, « Péril +1 » qui monte au-dessus, son de cloche grave repris du Chronomètre (à revoir avec l'audio), puis la stèle s'éteint, grise. Section `peril_stele` de `landmarks.json` : une stèle entre 0,08 et 0,16 du rayon, neuf entre 0,2 et 0,92, placées après les Ateliers pour que les lieux existants ne bougent pas. Minimap : triangle rouge, légende « Stèle du Péril », masqué une fois la stèle éteinte. Mode `--capture-steles` : sur la seed 221092026, 10 stèles placées, Péril 1 puis 2, stèle éteinte refusée ; images regardées (marais, HUD, fiche de pause). La mesure compte les stèles vues (`peril_stele_seen`) sans que le bot les vise.

### P3 — Objet du Péril

- Un objet de plus dans `passive_souvenirs.json`, 30 niveaux, **+1 Péril par niveau**, aucun autre effet ; palier du niveau 15 sans gain.
- Icône générée, texte localisé, offert comme les autres objets.
- Vérification : contrat des objets, test des objets, capture de la carte.


**Livré le 7 octobre.** **Sifflet d'arbitre** (« Les créatures t'entendent de loin ») : stat `peril`, +1 par niveau, sans palier. Une carte rare donne une fraction de plus ; `Player.AddObjectPeril` cumule les fractions et verse les points entiers. Icône : sifflet en laiton percé, anneau et cordon rouge. Le bot de mesure ne le prend que si `--prefer` le nomme. `ObjectsRegression` : 33 objets proposés, Sifflet contrôlé (1, 3 au niveau 3, 4 puis 6 après deux cartes à × 1,5).

### P4 — Montée naturelle et Résurgences

- Relever les curseurs existants (`spawn_flow.json`) : densité locale et plafond d'ennemis après 8 min d'abord, vitesse et agression ensuite, PV en dernier.
- Résurgences plus dures (`crises.json`, multiplicateurs d'apparition de crise) : plus d'ennemis et d'élites.
- Cible mesurée au protocole P0, à Péril 0 : le temps pour tuer entre 5 et 25 min ne descend plus sous celui de 0–5 min ; les coups pour mourir restent dans l'étendue actuelle (5 à 10).
- Vérification : mesure avant/après, banc de coût.

### P5 — À jouer

Raphaël joue une run avec le build de §74, en montant le Péril aux stèles. Ses retours vont au registre.

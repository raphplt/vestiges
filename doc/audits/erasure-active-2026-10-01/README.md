# Effacement : cellules actives — 1er octobre 2026

Lot 6C du plan 10, continuation hors audio, seul. Référence de production :
`a75780ca`. Le banc et le scénario de capture ont été ajoutés avant le correctif,
puis exécutés à l'identique après. La révélation Mémorial/Faille reste un lot indépendant : son commit
`36a318f3`, arrivé pendant cette validation, est conservé dans la version
réunie avec `main`. Il ne fait pas partie de la référence du comparatif 6C.

## Changement et contrat

La mémoire et la phase de chaque zone restent conservées, y compris à zéro.
Un index séparé marque les zones positives, par blocs de 64 bits, et garde
l'ordre de leur première apparition. Les zones nulles ne refont plus distance,
déclin, écriture de mémoire et recherche de phase à chaque demi-seconde.
Une stabilisation (Mémorial, coffre, lieu, Souvenir) ou une surcharge de capture
réactive le bit. Les coefficients JSON, les horloges et l'upload du sol restent
identiques. Le dernier passage à zéro émet toujours sa transition.

Cet index demande une table d'indices et une liste de coordonnées supplémentaires.
Il parcourt encore les blocs de bits : 106 blocs pour les 6 753 cellules de fin
de scénario. Ce n'est ni un gain mémoire ni la suppression de tout travail sur
le passé. Une copie de toutes les clés n'est plus nécessaire à chaque pas.
L'énumérateur est une valeur sans allocation ; les collections grandissent lors
de la découverte de nouvelles cellules.

Coût retenu de ces seules structures, mesuré dans un processus .NET isolé
après chauffe et GC, quatre passes alternées : ancien instantané de clés
**107 680 octets**, nouvel index **268 912 octets**, soit **+161 232 octets
(157,5 Kio)** pour 6 753 cellules. La croissance des capacités reprend les
3 600 tailles de la trace ; les dictionnaires métier communs sont exclus.
Ce n'est pas la mémoire RSS de Godot. Sources du microbanc archivées en
`index-memory.cs.txt`/`index-memory.csproj.txt`, résultats dans
[index-memory.json](index-memory.json). Le projet temporaire lie le vrai
`ErasureCellIndex.cs`, sans remplacer sa logique ni démarrer Godot.

L'ordre compte : un abonné peut utiliser les transitions pour tirer une Faille.
Une réactivation synchrone pendant un signal traite une ancienne zone encore
à venir au même pas ; une zone déjà passée ou nouvellement créée attend le
suivant. Les quatre contrôles réentrants vérifient ce contrat historique.

## Comparaison reproductible

```sh
tools/test_erasure_active.sh /tmp/vestiges-erasure-check \
  "$PWD/doc/audits/erasure-active-2026-10-01/before-result.json.gz"
```

Parcours sinusoïdal fini de 30 minutes : x ±5 000, y ±2 200, pas de 0,5 s,
seed 221092026. Crises, application/retrait de l'Oubli du chemin et
stabilisations de coffres/Mémoriaux aux mêmes pas des deux côtés. Le trajet
synthétique ignore obstacles et combat ; la vraie run est traitée séparément.

**20 assertions passent avant et après.** Les 3 600 empreintes de mémoire
(valeurs float brutes), phases, texture R8 et déclin global sont identiques.
Les **25 514 signaux de phase ordonnés** sont comparés intégralement ; ce sont
les entrées utilisées par les Failles, pas un nouveau test de leur RNG.

| Travail | Avant | Après |
|---|---:|---:|
| Recalculs de cellule sur les 30 minutes | 21 195 630 | 16 152 536 (−23,793 %) |
| Dernier pas : zones mémorisées | 6 753 | 6 753 |
| Dernier pas : zones encore positives | 174 | 174 |
| Dernier pas : recalculs | 6 753 | 174 (−97,423 %) |
| 6 000 zones nulles : recalculs par pas | 6 000 | 0 |
| 100 pas chauds, 841 ou 6 000 zones nulles : allocations directes | 25 600 octets | 25 600 octets |

Les allocations incluent les publications du gestionnaire, pas l'observateur ;
aucune réduction d'allocations affirmée. Le tableau ne mesure pas les FPS.
Charge observée : 5,70 puis 7,38 pour 16 cœurs logiques (seuil du banc : 4),
avec un autre jeu actif. Aucune comparaison de débit fiable ni validation
60 FPS. Les gains de parcours apparaissent lorsque les zones atteignent zéro,
pas au début de la partie.

Les anciens compteurs de l'audit de septembre sont adaptés : les visites
proviennent maintenant du nombre réellement calculé, les zones nulles après
un pas ne sont plus appelées « visites nulles ». Les surcharges de ce banc
passent par `OverrideMemory`, pour ne pas contourner l'index.

## Régressions et captures

- 26 contrôles temporels du lot 6A : dette, jitter, pause, hitstop simulé,
  transitions, dégâts du Néant et statuts lointains ; tous passent.
- 26 contrôles de liaison à Main, traversée, eau, Néant, Mémorial, crise,
  score et sauvegarde ; tous passent.
- Build C# : zéro avertissement et erreur. Smoke : 600 frames, vert.
- Huit captures avant et huit après : cinq phases, dégradé, éclats et retour
  depuis le Néant après stabilisation. Planches inspectées, dernière image
  ouverte séparément. La zone ravivée redevient colorée, son bord rejoint le
  Néant ; aucun changement visuel recherché ni constaté sur ces cas.
  ViewSonic seul : écran 1, (3840, 0), taille réelle 3840 × 2160.

Le scénario de capture attend désormais la disparition de l'overlay de
chargement et publie son `RESULT`, attendu par le lanceur. Il ajoute la reprise
après stabilisation. Ce même scénario est appliqué des deux côtés. Les images
restent hors dépôt ; chemins et SHA-256 dans [captures.json](captures.json).

## Run prolongée

Main réelle headless de **1 800 secondes**, seed 221092026, bot nomade
invincible qui visite les lieux, spawn naturel : terminée sans erreur moteur
inattendue. 20 189 éliminations, 32 ouvertures de coffre confirmées par signal,
six débuts et cinq fins de Résurgence. Les trois Mémoriaux indiqués « visités »
par le compteur de proximité n'ont émis aucun réveil dans cette run ; leur
réactivation est couverte par les régressions et la capture dédiées.

`--measure-erasure` échantillonne les compteurs une fois par seconde et trace
les crises et stabilisations, sans intervenir sur le jeu. Les 1 800 échantillons
et signaux sont archivés (`long-run-*.csv.gz`), synthèse dans
[long-run-summary.json](long-run-summary.json). Dernier relevé à 1 799,017 s :
17 087 cellules connues, 591 encore positives, **603 recalculs au dernier pas**
(12 cellules sont arrivées à zéro pendant ce pas), 267 blocs d'index, Effacement
global à 100 %. Aucun chronométrage comparatif ni conclusion de difficulté :
c'est une run d'intégration invincible, distincte du rejeu exact avant/après.

```sh
MEASURE_EXTRA_ARGS='--nomad --visit --measure-erasure' MEASURE_JOBS=1 \
  tools/measure_run.sh /tmp/vestiges-erasure-long 1800 '221092026'
CAPTURE_EXTRA_ARGS='--capture-erasure' VESTIGES_SCREEN=1 \
  tools/capture_run.sh /tmp/vestiges-erasure-images 10 0 1920x1080 221092026
```

Observation pour les lots suivants : le journal compte **19 782 orbes d'XP
encore au sol** à la fin. Leur durée de vie en zone oubliée reste à auditer
(plans 16/22) ; leur coût n'a pas été isolé dans ce lot.


## Réunion avec le lot indépendant de révélation des lieux

La branche de travail a repris `36a318f3` sans conflit ; aucun fichier de
Mémorial, Faille, révélation ou audio n'a été modifié par 6C. Après cette réunion,
la comparaison des 3 600 états/25 514 signaux est toujours identique, les
20 contrôles 6C passent, ainsi que les 26 contrôles Main et les **31 contrôles
des choix/révélations**. Journaux `integrated-*.log.gz` et
[integrated-comparison.json](integrated-comparison.json). Build : zéro warning.

# Pression des projectiles — 1er octobre 2026

Diagnostic du [plan 07 R3](../../plans/07-bestiaire-et-rencontres.md), suite au retour de Raphaël : esquiver continuellement empêche de combattre la foule.

## Inventaire du comportement actuel

| Tireur | Portée de déclenchement | Annonce | Intervalle minimal de base entre tirs | Particularité |
|---|---:|---:|---:|---|
| Cracheur Pâli | 275 px | 0,30 s | 1,40 s | Bile, présent en exploration urbaine/marais |
| Sentinelle Hurlante | 250 px au sol isométrique | 0,45 s | 1,55 s | Immobile, cercle de portée visible |
| Tisseuse | 180 px | 0,35 s | 1,45 s | Ralentissement ×0,4 pendant 2 s |
| Hurleur | 300 px | 0,40 s | 1,50 s | Tir en plus du cri qui appelle des renforts |

Le cooldown commun de 1,1 s se réduit avec l'agressivité ; l'annonce s'ajoute ensuite. Les intervalles ci-dessus supposent une cible à portée, aucune autre capacité occupée et une agressivité de 1. Le premier délai aléatoire de 0,3–1 s désynchronise les apparitions. La direction se verrouille au début de l'annonce ; tuer le tireur pendant celle-ci interrompt l'attaque.

**Les quatre tirs voyagent à 185 px/s et expirent à 4 s, soit environ 740 px maximum**, bien au-delà des 180–300 px qui déclenchent le tir. Le masque ne touche que le joueur : les décors ne les arrêtent pas. Un projectile déjà parti survit à son tireur ; ni sa sortie de l'écran ni une limite commune de tirs simultanés ne l'arrêtent. Les traînées/VFX ne sont pas comptées comme des projectiles.

Le **Présage** utilise une frappe au sol avec anticipation, annonce de 1 s, rayon 42 px et maximum de deux zones simultanées : il ne participe pas à ces compteurs. Charge, surgissement et ondes locales sont également distincts.

| Biome | Tireurs du pool exploration | Tireurs du pool Résurgence |
|---|---|---|
| Ruines urbaines | Cracheur | Cracheur, Sentinelle, Hurleur |
| Marécages | Cracheur | Sentinelle, Tisseuse, Hurleur |
| Forêt / Champs | Aucun | Hurleur |
| Carrière | Aucun | Sentinelle, Hurleur |

Ces listes ne sont pas des proportions garanties : sélection pondérée, temps, renforts et déplacements entre biomes changent les rencontres. Le Hurleur existe dans tous les pools de Résurgence.

Sources : `data/enemies/*.json`, `data/biomes/*.json`, `data/scaling/spawn_flow.json`, `Enemy.cs`, `Abilities/AimedShotAbility.cs`, `EnemyProjectile.cs` et sa scène.

## Protocole

Même build, seeds 221092026 et 1002, 320 s de jeu, bot Traqueur nomade invincible, Péril 0, première Résurgence de 240 à 310 s. Simulation headless à pas fixe de 60 Hz ; aucune fenêtre, aucune mesure de FPS. Vue de base 960×540 px monde, élargie par la caméra en présence de foule ; le cadre courant sert à chaque mesure.

- Référence : durée de vie 4 s issue de la scène.
- Essai : surcharge de 2 s dans `ProjectilePressureProbe` seulement (370 px maximum), restaurée à la sortie. Aucune donnée de production changée.
- Échantillonnage à 10 Hz : tirs actifs, tirs dont le point au sol est dans le cadre, âge >2 s, espèce source, nombre de tireurs vivants visibles. Un tireur visible n'est pas forcément celui d'un tir observé ; la sonde ne relie pas les tirs à un propriétaire individuel.
- Densité et dégâts : compteurs du banc existant. Les dégâts reçus sont des valeurs brutes sur bot invincible, pas des PV perdus ; le journal distingue aussi une estimation avec invulnérabilité. Une source « Hurleur » ne suffit pas à isoler son projectile des autres dégâts de cette espèce.
- L'aléatoire et les choix de niveau du bot font diverger les runs, même avec la même seed. L'essai explore un levier ; il ne prouve ni un gain causal précis ni le plaisir de jeu.

```sh
MEASURE_EXTRA_ARGS='--nomad --measure-projectiles' MEASURE_JOBS=1 tools/measure_run.sh /tmp/vestiges-r3-base 320 '221092026 1002'
MEASURE_EXTRA_ARGS='--nomad --measure-projectiles --projectile-lifetime 2' MEASURE_JOBS=1 tools/measure_run.sh /tmp/vestiges-r3-short 320 '221092026 1002'
python3 tools/summarize_projectile_pressure.py /tmp/vestiges-r3-base /tmp/vestiges-r3-short
```

Les CSV et journaux compressés conservent les observations, `summary.json` les agrégats. P95 = nombre non dépassé pendant 95 % des échantillons ; les moyennes ne comptent pas le temps passé dans les écrans de niveau.

## Résultats de l'essai

| Seed | Durée de vie | Projectiles visibles en crise, moyenne / P95 / pic | Crise avec au moins un tir visible | Tireurs visibles en crise, moyenne | Foule visible sur 320 s, moyenne | Dégâts bruts/min en crise |
|---|---:|---:|---:|---:|---:|---:|
| 221092026 | 4 s | 14,78 / 44 / 60 | 93,00 % | 10,53 | 21,36 | 3 855 |
| 221092026 | 2 s | 16,77 / 47 / 59 | 98,71 % | 19,64 | 23,41 | 4 042 |
| 1002 | 4 s | 7,20 / 21 / 35 | 94,43 % | 10,19 | 20,80 | 805 |
| 1002 | 2 s | 4,03 / 13 / 25 | 76,29 % | 7,88 | 23,32 | 683 |

À 4 s, le Hurleur représente **72,91 % / 62,63 %** du temps cumulé de projectiles visibles pendant la crise (seeds dans l'ordre du tableau) ; le Cracheur 20,86 % / 29,03 %. Les projectiles de plus de 2 s représentent 28,14 % / 21,19 % de cette occupation. Avant la crise, les tirs viennent uniquement des Cracheurs sur ces trajets, avec 1,21 / 1,12 projectile visible en moyenne. La transition vers les compositions de Résurgence est donc le principal point observé.

**Essai non concluant pour adopter 2 s.** La seconde seed s'allège ; la première rencontre presque deux fois plus de tireurs et reste saturée. Les armes/niveaux du bot et les populations divergent. Les tirs qui s'éloignent du joueur comptent aussi : occupation visuelle ne signifie pas danger imminent. Aucun pourcentage global d'amélioration n'est déduit de ces runs.

**Suite recommandée :** une rencontre à composition et trajectoire fixes, centrée sur le Hurleur, avec la même foule de mêlée. Comparer un seul levier : espacer son tir en données, tout en conservant son cri/renforts et son télégraphe. Puis reprendre les mêmes seeds et jouer réellement la séquence pour juger les fenêtres de combat. Un budget simultané de tireurs reste une piste si l'espacement ne suffit pas ; ne pas cumuler les changements lors du premier essai.

Le jeu normal conserve 4 s et tous ses réglages. Ni le fun, ni les Résurgences suivantes, ni les performances ne sont validés ici. La vérification à l'écran/partie jouée de l'essai reste à faire ; ces mesures sans rendu ne valent pas recette visuelle.

## Contrôles

Build C# : 0 avertissement, 0 erreur. Même SHA-256 d'assembly avant/après (dans `metadata.json`). Quatre runs terminées, **12 800 échantillons**, 700 de crise par run ; sources entièrement identifiées, somme des espèces égale aux tirs visibles et aucun tir de plus de 2 s dans l'essai. Les journaux ne signalent pas d'exception de run ; avertissements Steam/audio Dummy et ressources à la fermeture conservés dans les archives. Synthèse Python compilée et exécutée sur les quatre résultats. Sonde optionnelle uniquement, aucun fichier de gameplay modifié.

# Plan 42 — La signature de Vestiges

Statut : **conception, ouverte le 13 octobre 2026** ([DECISIONS §151](DECISIONS.md)). Rien ne se code avant validation, sauf l'outillage de mesure (§5), qui ne change pas le jeu.

## 1. Le retour

Raphaël, le 13 octobre : Vestiges appartient au roguelite, plus précisément au bullet heaven, dont les grands succès sont Vampire Survivors, Megabonk et, selon lui, Risk of Rain 2. Il veut un jeu aussi complet et poussé, **avec des mécaniques uniques** : c'est ce qui le différenciera. L'histoire ne suffit pas sans un feeling propre, une boucle très satisfaisante et addictive. Megabonk est celui qui l'a le plus happé, par « un mélange du côté gambling, des scores explosifs, des quêtes et déblocages, et qu'on ne s'ennuie jamais en run ».

## 2. Diagnostic partagé

| Constat | Accord de Raphaël |
|---|---|
| L'Effacement pousse en arrière-plan, il change peu les décisions de chaque seconde. Ses règles existent (présence qui ralentit l'oubli, coffre qui ravive, zones effacées plus peuplées, Néant qui blesse), mais graduelles et invisibles | Oui. Elles se sentent au bout d'un moment, pas en début de partie |
| Pas d'emballement de puissance : aucun moment où le build bascule et casse le jeu (plan 30 : temps forts sans amplitude, Souverains abattus en 4 à 7 s) | « 1000 % » : c'est ce qui rend Megabonk addictif. Ça ne doit pas arriver à chaque run, il faut connaître le jeu et avoir un build qui fonctionne en synergie. Le travail est dans l'équilibrage, surtout la montée en puissance des ennemis face à celle du joueur |
| Le mouvement n'est pas une compétence (dash seul) | Oui, à renforcer, à la manière d'un jeu 2D isométrique et non de Megabonk |
| Trop de systèmes minces | À moitié : Megabonk en a autant (4 armes, 4 tomes, objets cumulables, coffres payants ou gratuits, statues, micro-ondes, pots, aimants, POI à combattre). Les nôtres sont plutôt mal expliqués, organisés et équilibrés |
| Les derniers lots ont surtout porté sur l'habillage | Oui : l'atmosphère est en pause |
| Les gestes de base sont ceux du genre | Gardés : le tir automatique lui plaît. La différence doit venir d'ailleurs |
| « Une course en ligne » | Erreur de l'agent, retirée : la carte est finie, se retraverse en une minute et s'ouvre dans toutes les directions |

## 3. Acquis du 13 octobre

**Les deux leviers à construire :**
- **La pression** : l'Effacement prend quelque chose, de plus en plus instable au fil de la run.
- **La gourmandise** : du butin qui s'efface dans la carte et qu'on court sauver.

L'idée d'« élan » (la vitesse rend plus fort) n'est pas retenue. La formule « la distance comme moteur » non plus.

**Ce que l'Effacement prend :**
- **Retenu à étudier** : dans le Néant, une ou plusieurs stats tirées au hasard baissent pour un temps, d'autant plus nombreuses que la run avance. Raphaël y voit un côté aléatoire « plus marrant ».
- **Écartés** :
  - l'Essence non dépensée, parce que des objets comptent l'Essence ramassée (plafond `essence_damage_cap`, Siphon d'Essence) ;
  - les objets du sac qui s'effacent ;
  - les niveaux d'arme oubliés, moins bien que les stats.
- Les PV perdus par seconde (6 % des PV max dans le Néant aujourd'hui) peuvent rester, avec le risque d'une zone de battle royale.

**Instabilité** : elle croît au fil de la partie. Calme au début, elle devient imprévisible ensuite.

**Butin à sauver** (idée B du [plan 11](11-mecaniques-originales.md)) : jugé « excellent ». Des coffres et des bonus de la carte s'effacent peu à peu, ce qui force à explorer vite. Une fois le jeu compris, on sait quoi faire et on se dépêche de récupérer le plus possible : le jeu devient plus vif.

**Contrer l'Effacement :**
- Pas de nouveau geste.
- Pas de halo de couleur au sol, qui rendrait mal.
- L'ancrage passe par ce qui existe :
  - ouvrir un coffre ;
  - utiliser un point d'intérêt ;
  - marcher.

  Ces trois gestes ralentissent l'oubli de la zone. Il reste à trouver comment le rendre lisible.

**Méthode** : mesurer d'abord, écrire la promesse en une phrase, puis **tout concevoir avant de coder**.

## 4. Question ouverte : le Campement

Doute de Raphaël : certaines améliorations permanentes lui conviennent (taille du sac, Vestiges gagnés). D'autres le gênent : vitesse, dégâts et autres stats brutes. « Quand on a joué 20 heures, le début de partie n'est plus le même qu'au tout début du jeu. »

Les 14 améliorations actuelles (`data/progression/camp_upgrades.json`) se rangent en deux familles :

| Famille | Améliorations |
|---|---|
| Options et ampleur | emplacements d'armes, poches (sac), relances, bannissements, aimant, Essence de départ |
| Puissance brute | PV max, dégâts, cadence, vitesse, régénération, armure, XP, Chance |

Deux écoles dans le genre :
- **Puissance permanente** : Vampire Survivors vend des bonus de stats, remboursables.
- **Déblocages seulement** : Risk of Rain 2 ne vend aucune stat permanente, seulement des déblocages.

Proposition de l'agent : garder la famille « options et ampleur », retirer ou transformer la famille « puissance brute ».
- Les Vestiges déjà dépensés seraient remboursés.
- XP et Chance sont à trancher : elles touchent au gambling.

**Tranché le 13 octobre ([§152](DECISIONS.md))** : la puissance brute part (PV max, dégâts, cadence, vitesse, régénération, armure), sans remboursement (personne n'a de copie du jeu) ; XP et Chance à trancher avec le gambling. L'état « complet » se mesure sans elles.

## 5. Campagne de mesures

But : savoir où en sont, minute par minute, la puissance du joueur et celle des ennemis, l'exposition à l'Effacement et les creux sans décision. On compare personnages, états de progression, builds et niveaux de joueur. Le tout se fait **en simulation headless, en temps accéléré**, sans lancer le jeu à l'écran.

### Ce qui existe

Inventaire du 13 octobre :
- `tools/measure_run.sh` avec `MEASURE_EXTRA_ARGS` et `MEASURE_JOBS` ;
- `--character`, `--nomad`, `--visit`, `--mortal`, `--damage-probe`, `--prefer`, `--peril`, `--scaling`, `--camp-full`, `--timeline`, `--measure-erasure` ;
- profils neuf (`MEASURE_PROFILE=normal`) et tout débloqué (`--dev`) ;
- synthèses `summarize_power.py` (temps pour tuer par tranche) et `summarize_timeline.py` (creux entre temps forts).

### Ce qui manque

- **Niveaux de joueur.** Le bot ne dashe pas, n'esquive pas, ne relance pas et prend la première carte offerte. Les profils « débutant / fort » n'existent pas ; seuls des multiplicateurs hypothétiques existent dans `progression_model.py`.
- **Builds imposés** en run mesurée. Aujourd'hui, seul `--prefer` existe, et il ne marche que si la carte est offerte.
- **État méta intermédiaire** : déblocages partiels, Campement à moitié.
- **Métriques absentes** :
  - distance parcourue ;
  - temps par phase d'Effacement ;
  - PV du joueur à chaque seconde ;
  - dégâts par arme en vraie run ;
  - durée sans décision.

### Lots d'outillage (ne changent pas le jeu)

- **M0a — Métriques.**
  - Ajouter à la sonde de run :
    - distance parcourue ;
    - phase d'Effacement sous le joueur à chaque seconde ;
    - PV et PV max du joueur ;
    - dégâts par source (arme, objet, Écusson) ;
    - « secondes depuis la dernière décision ».
  - Définition proposée d'une décision : un écran de choix, un coffre ou un lieu utilisé, un micro-événement, une Résurgence, un élite ou un Souverain à portée.
  - Synthèse Python par tranche de 1 min, avec, pour chaque minute :
    - le temps pour tuer un ennemi ordinaire et un élite ;
    - le rapport entre le DPS du joueur et les PV des créatures apparues par seconde ;
    - les morts ;
    - la part du temps dans chaque phase d'oubli ;
    - le plus long creux.
- **M0b — Niveaux de joueur**, trois profils du bot :
  - **Débutant** : erre, ne visite que ce qu'il croise, prend une carte au hasard, n'esquive pas, ne dashe pas.
  - **Moyen** : garde un cap, visite les lieux proches, choisit les cartes de ses armes et de ses objets déjà possédés.
  - **Fort** :
    - visite et détours ;
    - choix de cartes par score de synergie ;
    - relances et bannissements utilisés ;
    - esquive simple : s'éloigne de la densité et des projectiles annoncés ;
    - dash pour sortir d'un encerclement.

  Tous mortels (`--mortal`) : une mort est comptée et la run continue, pour garder une courbe complète.
- **M0c — Builds et méta.**
  - Option `--start-build` : armes, niveaux, objets et Écussons imposés au départ.
  - Trois états méta :
    - neuf ;
    - milieu (environ la moitié des quêtes accomplies, Campement à moitié) ;
    - complet.
  - Le « milieu » se fait par fixture de sauvegarde, pas par `--dev`.

### Livré le 13 octobre (M0a à M0c)

Tout passe par `tools/measure_run.sh` (`MEASURE_EXTRA_ARGS`), sans rien changer au jeu.

- **M0a — rythme**, relevé à chaque mesure (`tools/tests/RunPaceProbe.cs`) :
  - `pace-<seed>.csv`, chaque seconde : chemin parcouru, éloignement du départ, phase d'oubli sous le joueur, PV et PV max, morts comptées, secondes depuis la dernière décision, élite à portée ;
  - `damage-sources-<seed>.csv` : dégâts infligés par minute, arme et nature (coup, secondaire, sur la durée, objets) ;
  - `kills-<seed>.csv` : temps pour tuer chaque créature, du premier coup du joueur à sa mort, élite ou non ;
  - `decisions-<seed>.csv` : journal des décisions (choix, coffre, lieu, monument, micro-événement, Résurgence, Souverain, élite à portée ; un élite proche suspend le creux tant qu'il est là) ;
  - synthèse : `python3 tools/summarize_pace.py <dossier> [autre dossier…]`, un tableau par minute et la part des dégâts par source.
  - Limite : les dégâts d'un objet sont rangés sous l'arme dont l'impact l'a déclenché, en nature « objets » ; le nom de l'objet n'est pas connu du combat.
- **M0b — profils**, `--player-profile debutant|moyen|fort` (`RunObservation.Profiles.cs`) ; sans l'option, le bot historique reste inchangé.
  - Débutant : erre autour du départ, détour de 220 px, carte au hasard.
  - Moyen : garde un cap, détour de 700 px, prend d'abord ce qu'il possède (montées, ascensions, spécialisations).
  - Fort : garde un cap, détour de 1 100 px, choisit par score (arme principale poussée, objets qui partagent une stat déjà portée, rareté), relance une offre sans intérêt, bannit gratuitement une nouveauté inutile ; s'écarte de la foule ; il dashe quand trois créatures l'entourent à moins de 110 px, ou pour fuir sous 40 % des PV. Son écart à la foule marche si bien que quatre créatures au contact n'arrivent que quelques images par run : d'où ce seuil bas (16 dashs en 5 min sur la graine 7). Avec un build fermé, une montée d'arme est presque toujours offerte : il ne relance donc presque jamais.
  - L'esquive des tirs et des annonces au sol vient en M0d (ci-dessous).
  - Ajouter `--mortal` pour compter les morts.
- **M0c — builds et méta** (`RunObservation.Builds.cs`) :
  - `--start-build <id>` : cinq builds dans `tools/tests/measure_builds.json` (une arme poussée, synergie d'objets autour du feu, zone, critique, tank). Armes au départ, cartes du build prises d'abord, objets posés dans le sac à heure fixe (le butin d'un coffre ne se choisit pas), nouvelles armes refusées si le build est fermé.
  - `--meta neuf|milieu|complet`, avec `MEASURE_PROFILE=normal` : milieu = une quête sur deux du catalogue (chaque famille à moitié) et la moitié des rangs du Campement ; complet = tout. Les rangs de puissance brute ne sont jamais achetés (§4).
  - Les niveaux d'arme ne s'imposent pas : le build grandit par les cartes, ce qui garde une courbe de puissance réaliste.

### M0d et M0e — esquive et contrôle (demandés le 13 octobre)

Retour de Raphaël : un bot invincible gâche la mesure, un bot qui meurt par bêtise aussi. Il faut un bot qui esquive comme un joueur, sans séance de jeu dédiée.

- **M0d — esquive** (`RunObservation.Dodge.cs`). Le bot ne lit que ce qu'un joueur voit à l'écran :
  - les tirs ennemis en vol : il s'écarte de leur trajectoire ;
  - les annonces de coup au sol, disques et couloirs : il en sort.

  Chaque menace est remarquée ou non, puis seulement après un temps de réaction. Si marcher ne suffit plus, il dashe.

  | Profil | Réaction | Menaces remarquées | Dash pour esquiver |
  |---|---|---|---|
  | Débutant | 0,5 s | une sur trois | jamais |
  | Moyen | 0,35 s | deux sur trois | sous 40 % des PV |
  | Fort | 0,22 s | 95 % | dès que marcher ne suffit plus |

  Ce sont des ordres de grandeur humains, pas un calage sur un joueur. Côté jeu, seul un accès interne en lecture est ajouté (`EnemyProjectile.InFlight`, `GroundTelegraph.Shown`), sans changer aucun comportement.
- **M0e — contrôle sur les parties de Raphaël.** Un enregistreur, actif en mode dev seulement, note pendant ses parties de test habituelles :
  - les PV perdus par minute et par source ;
  - les morts et les dashs.

  Quand quelques runs se sont accumulées, on compare au profil fort ; on ne recale que si l'écart est grand. Pas de séance dédiée, et la matrice n'attend pas ce contrôle.
  - Côté jeu : `PlayTraceRecorder`, présent seulement dans les builds d'édition et jamais dans les bancs. Il écrit `traces/<date>-<personnage>.csv` et `.txt` dans le profil Godot à la fin de chaque run de plus de 30 s.
  - Côté bot : le même compte (`PlayTrace`), écrit à chaque mesure (`trace-<seed>.csv`).
  - Comparaison : `python3 tools/compare_play_traces.py [--character traqueur] <dossiers de mesure>`.
  - Vérification : `--check-play-trace` (le bot joue 40 s avec l'enregistreur branché, puis la trace doit être écrite).
- **Effet mesuré de l'esquive** (graine 7, build zone, méta complet, 5 min) :

  | Bot | PV perdus / min | Morts | Dégâts du Présage reçus |
  |---|---|---|---|
  | Fort sans esquive des tirs | 78 | 4 | 434 |
  | Fort | 34 | 2 | 196 |
  | Moyen | 48 | 0 | 66 |
  | Débutant | 363 | 21 | 1 888 |

  L'ordre attendu est là, sur une graine.
- **Dispersion** : une même graine ne rejoue pas la même run, car le monde se génère en parallèle et le combat dépend du rythme des images. Sur la graine 7, deux runs identiques du profil fort ont encaissé 744 puis 66 dégâts du Présage (8 puis 0 coups fatals). Une configuration ne se lit donc qu'en moyenne et en étendue sur plusieurs runs : trois graines au moins par case de la matrice, avec l'étendue min–max à côté de chaque moyenne.
- Bornes gardées : **invincible** pour la pression brute (dégâts si on encaisse tout), **mortel** pour la survie (un coup fatal compte une mort et la run continue).

### Run de référence (13 octobre)

Traqueur, profil dev, bot historique `--nomad --visit --mortal`, graine 221092026 :
- **30 min de jeu en 19 min de calcul** (1131 s). Une run n'occupe qu'un cœur (le thread principal de Godot) et environ 1 Go de mémoire ; la machine a 16 cœurs, et cette session n'a pas de quota. La matrice entière (93 runs) prendrait environ 29 h en série, et environ 5 h avec six runs à la fois (`MEASURE_JOBS=3 MATRIX_JOBS=2`).
- Premiers signaux, à confirmer par la matrice :
  - de la minute 6 à la minute 27, le bot ne perd pas un PV ;
  - le temps pour tuer reste sous 0,4 s jusqu'à 25 min ;
  - le rapport dégâts / PV apparus tourne autour de 1, la marée est absorbée ;
  - l'oubli sous le joueur passe Fragile vers 8 min, Effiloché vers 19 min, Effacé vers 28 min, et jamais Néant ;
  - le bot reste dans 800 m du départ (la carte) ;
  - 82 % des dégâts viennent de la Cloche, dont 35 % par les objets qu'elle déclenche ;
  - le plus long creux sans décision monte de 10–20 s à 30–40 s après 20 min.

### Lot D1 — départ graduel (avant la matrice, DECISIONS §154)

Retour de jeu : niveau 10 vers 2 min, cinq premières minutes trop nerveuses ; cible : **niveau 10 vers 4–5 min**, pression ennemie plus basse d'autant au début. Fait avant M1 pour que la matrice mesure le jeu corrigé.

1. **Mesure de base**, 10 min de jeu :
   - les cinq personnages, profils moyen et fort, méta milieu, deux graines ;
   - relevés par minute : niveau, temps pour tuer, PV perdus, créatures apparues.
2. **Leviers**, tous en données :
   - la courbe d'XP (`data/scaling/progression.json`) : majoration des premiers niveaux, aujourd'hui neutre ;
   - le flux d'apparition des premières minutes (`data/scaling/spawn_flow.json`).

   Moins d'ennemis, c'est aussi moins d'XP : les deux leviers se règlent ensemble.
3. **Mesure après réglage**, avec les mêmes configurations : niveau 10 entre 4 et 5 min pour le profil moyen, sans que le profil débutant reste bloqué sous le niveau 5.
4. **Contrôle de non-régression** : le niveau à 10, 20 et 30 min, pour ne pas déplacer tout le reste de la run sans le vouloir.

Outil : `tools/measure_matrix.sh <dossier> <plan>` enchaîne des configurations en une seule compilation (une ligne par configuration : nom, secondes, graines, profil de sauvegarde, arguments).

**Livré le 13 octobre.** Réglages :
- les dix premiers niveaux coûtent ×1,8, en décroissant jusqu'à ×1 au niveau 10 (`early_levels` 10, `early_multiplier_start` 1,8) ;
- la rampe d'ouverture des apparitions passe de 120 à 300 s.

Mesure : méta milieu, deux graines, 10 min.

| Personnage | Niveau 10 atteint, moyen (avant → après) | Fort (avant → après) | Créatures à moins de 600 px à 3 min (avant → après) |
|---|---|---|---|
| Traqueur | 3,0 → 4,2 min | 3,9 → 5,1 min | 34 → 23 (moyen) |
| Vagabond | 3,0 → 4,4 min | 2,9 → 4,0 min | 35 → 26 |
| Forgeuse | 3,1 → 4,5 min | 3,5 → 4,0 min | 37 → 23 |
| Éveillée | 4,1 → 5,4 min | 5,0 → 5,1 min | 36 → 25 |
| Facteur | 4,7 → 7,0 min | 5,8 → 5,9 min | 40 → 32 |

- Au profil moyen, le niveau 10 tombe vers 4–5 min, sauf pour le Facteur, plus faible que les autres de toute façon (niveau 14 à 20 à 10 min). Son cas relève de l'équilibrage des personnages (matrice, bloc A).
- **Bot fort au corps à corps corrigé en route.** Il fuyait la foule à 170 px : le Vagabond restait au niveau 3,5 à 5 min. Désormais, un build de corps à corps ne s'écarte que de ce qui le colle (60 px) et ne dashe qu'à partir de sept créatures au contact.
- **Écart avec Raphaël.** Le bot fort encaisse encore bien plus que lui sur les cinq premières minutes : 33 à 470 PV par minute selon le personnage, contre 10 à 66 dans ses traces. Il dashe aussi beaucoup plus (10 à 14 par minute au corps à corps, contre 0,7). À recaler avec plus de traces (M0e).
- Le niveau à 20 et à 30 minutes sera vérifié par la matrice M1, qui mesure le jeu ainsi corrigé.

### Matrice (lot M1)

Une run de référence est d'abord chronométrée, pour connaître le rapport entre temps de jeu et temps machine. Durée : 30 min de jeu, jusqu'à l'Indicible. Ordre (§152) : après le lot 1 des retours du 12 octobre au soir. Budget visé : une nuit de calcul, `MEASURE_JOBS=2`.

| Bloc | Personnages | Méta | Profils | Builds | Graines | Runs |
|---|---|---|---|---|---|---|
| A — personnages | 5 | milieu | 3 | naturels | 3 | 45 |
| B — progression | Traqueur | neuf, complet | 3 | naturels | 3 | 18 |
| C — builds | Traqueur | complet | moyen, fort | 5 builds de référence (une arme poussée, synergie d'objets, zone, critique, tank) | 3 | 30 |

Builds de référence validés le 13 octobre (§152), tank ajouté ; leur composition exacte est fixée en M0c. L'état « complet » se mesure sans les stats brutes du Campement (§4).

### Résultats de M1 (13 octobre au soir)

Matrice complète : 31 configurations × 3 graines, 30 min de jeu, bot mortel qui se relève (`tools/measure_plans/m1-matrice.txt`, synthèse `tools/summarize_matrix.py`). Le bloc C a été remesuré à part : sa première passe avait lu des images non importées d'une autre session. Les écarts entre graines restent larges (Éveillée fort : 1 400 à 23 000 éliminations) : lire les moyennes avec leurs étendues.

- **Personnages** (méta milieu, profil moyen ; éliminations, niveau à 29 min) : Traqueur 19 200 (117), Forgeuse 14 500 (99), Vagabond 13 100 (94), Éveillée 9 200 (67), **Facteur 2 400 (37)**. Le corps à corps n'est pas en retard sur le bot ; le Facteur l'est très nettement, l'Éveillée un peu.
- **Progression** (Traqueur moyen) : neuf 13 500 éliminations, niveau 26 à 10 min ; complet 22 000, niveau 50, et plus aucun dégât subi après 15 min (10 PV/min). Avec la méta complète, la seconde moitié de run ne menace plus.
- **Builds** (Traqueur, méta complet) : arme poussée, critique et zone 20 000 à 24 000 éliminations, 0 à 2 PV perdus par minute après 15 min ; synergie d'objets 17 000 à 18 000, encore touchée ; tank 12 000, survit sans tuer. Aucun build ne se détache des autres, mais tous rendent la fin de run sans danger. La Pelle à neige (run cassée du §154) n'est pas dans ces builds.
- **Effacement** : 3 à 7 % du temps passé Effacé ou dans le Néant, quel que soit le profil. Il ne presse presque jamais.
- **Départ** (D1) : niveau 10 à 5 min tenu pour la méta milieu (niveau 9 à 15 à 5 min).
- **Bot** : le profil fort fait parfois moins bien que le moyen (Traqueur 11 800 contre 19 200, Facteur 1 100 contre 2 400) : son esquive le garde trop loin pour les armes de moyenne portée. Le profil moyen sert de référence en attendant le recalage du bot sur les traces de Raphaël.

**Run cassée à la Pelle à neige** (§154), remesurée le 13 octobre au soir. Dans l'historique, c'est le Traqueur de 14 h 49 : 834 s, niveau 66, 7 710 éliminations, **9,1 millions de dégâts** (23 fois l'autre Traqueur du jour). Après 7 min, il ne perdait plus de PV que contre les Présages, qui tirent de loin. Le build `pelle` (Pelle seule, Traqueur, 15 min, méta milieu et complète, 3 graines) reproduit la façon de survivre : la Pelle repousse les créatures au corps à corps (Déblayer : tout autour, recul ×2), et après 7 min les PV perdus viennent à 75–90 % des Présages. Il ne reproduit pas les dégâts : 1,3 à 1,5 million, autant que l'arc poussé (1,5 à 1,8). Ce qui a multiplié les dégâts venait donc du reste du build, que l'historique ne garde pas. Depuis, la trace des parties écrit aussi le build final et les dégâts par arme (`traces/<date>-<personnage>-build.txt`).

### Lot S1 — Souverains calés sur le niveau du joueur (DECISIONS §160), en pause

Commencé le 13 octobre au soir, arrêté à la demande de Raphaël avant la fin du calage. Code sur la branche `souverains-niveau` (commit `474b303e`), **pas fusionné** :
- `level_scaling` du Souverain dans `data/enemies/_variants.json` : PV = durée × coefficient × niveau^exposant (× Péril et affixes), dégâts = base + pente × niveau, à la place du temps ; `SpawnManager` suit le niveau par `EventBus.LevelUp` ;
- la sonde de rythme relève chaque combat de Souverain (`sovereigns-<graine>.csv` : niveau, PV, durée, secondes où il prend des coups).

Mesures (5 personnages, profils débutant et moyen, méta milieu, 3 graines, 11 min 30) :
- avant : 1er Souverain ≈ 2 400 PV ; combat médian 67 s, 7 sur 30 en 40 s ou moins, 5 jamais tués. 2e : médiane 33 s, 4 jamais tués ;
- premier réglage (30 s × 1,8 × niveau^1,45) : 1er à 450–2 500 PV selon le niveau ; médiane 50 s, 9 sur 30 en 40 s ou moins, 3 jamais tués. 2e : médiane 50 s, 7 jamais tués, à revoir au-dessus du niveau 20.
- Limite : le bot ne va pas chercher le Souverain (Facteur : 557 PV, vivant après 7 min 30) ; la durée du premier coup à la mort compte surtout ce temps-là. Reprise : caler sur les secondes de combat réel, puis fusionner.

Un Souverain jamais tué bloque les événements suivants (§157), ce qui pèse surtout sur les builds faibles.

### Le Facteur remis à niveau (13 octobre au soir)

M1 le montrait très en retard (2 400 éliminations en 30 min contre 13 000 à 19 000 pour les autres). À niveau égal, il faisait pourtant autant de dégâts que le Traqueur : c'est sa montée en niveau qui calait. Deux causes :
- **frappe faible** : attaque 8 (×0,8 sur ses armes) et Sacoche à 7, soit 5,6 au premier coup contre 9,6 pour l'arc du Traqueur ; les rebonds étalent le reste sur la foule, si bien qu'une créature mettait 12 à 19 s à mourir (médiane) contre moins d'une seconde avec les autres armes : peu d'XP et une foule qui s'accumule ;
- **croissance qui s'étale** : l'arc gagne des flèches en montant de niveau, la Sacoche ne gagnait que des rebonds.

Correctif (données seules) : attaque du Facteur 8 → 10, dégâts de la Sacoche 7 → 9, et la Sacoche peut gagner des lettres (`projectile_count` 0,6 dans sa croissance ; 1,25 comme l'arc donnait niveau 54 à 10 min au profil fort, chaque lettre rebondissant aussi). La Lettre recommandée garde sa lettre unique, plus lourde.

Mesure avant/après (Facteur, méta milieu, 12 min, 3 graines) : niveau à 10 min 17 → 30 (moyen), 19 → 30 (débutant), 25 → 46 (fort) ; durée de vie médiane d'une créature 18,7 → 6,2 s (moyen). Traqueur dans M1 : 38, 30 et 28. Le profil fort du bot n'est pas fiable (M1).

### Lecture (lot M2)

- Une page de résultats : courbes de puissance joueur / ennemis par profil, morts, exposition à l'oubli, creux.
- Puis discussion avec Raphaël.
- Les chiffres posent les **courbes cibles** de la conception :
  - temps pour tuer qu'on vise à chaque minute, pour un build moyen et un build en synergie ;
  - fenêtre où un bon build doit pouvoir casser le jeu.

## 6. Conception, après les mesures

Chaque point devient une section de ce plan, validée avant tout lot de code.

1. **La promesse en une phrase.** Brouillons :
   - « Le monde s'oublie autour de toi : ce que tu veux garder, va le prendre avant qu'il disparaisse. »
   - « Plus tu t'enfonces dans l'oubli, plus tu gagnes, et plus tu risques de perdre. »
2. **L'Effacement qui prend** : stats tirées au hasard dans le Néant, nombre, durée, lisibilité, montée de l'instabilité.
3. **Le butin à sauver** : quels coffres et bonus s'effacent, à quel rythme, comment on les voit venir.
4. **L'ancrage lisible** : coffres, lieux et pas qui ralentissent l'oubli, rendus visibles sans halo au sol.
5. **Courbes de puissance et emballement** : objets qui définissent un build, chaînes d'effets visibles, scores qui explosent, ennemis qui suivent.
6. **Le gambling** : coffres payants, raretés, Chance, Failles. Ce qui fait parier.
7. **Le mouvement en 2D isométrique.**
8. **Lisibilité et organisation des systèmes.**
9. **Quêtes et déblocages** : jamais d'ennui, toujours une raison de relancer.
10. **Le Campement** (§4).

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

### Matrice (lot M1)

Une run de référence est d'abord chronométrée, pour connaître le rapport entre temps de jeu et temps machine. Durée : 30 min de jeu, jusqu'à l'Indicible. Ordre (§152) : après le lot 1 des retours du 12 octobre au soir. Budget visé : une nuit de calcul, `MEASURE_JOBS=2`.

| Bloc | Personnages | Méta | Profils | Builds | Graines | Runs |
|---|---|---|---|---|---|---|
| A — personnages | 5 | milieu | 3 | naturels | 3 | 45 |
| B — progression | Traqueur | neuf, complet | 3 | naturels | 3 | 18 |
| C — builds | Traqueur | complet | moyen, fort | 5 builds de référence (une arme poussée, synergie d'objets, zone, critique, tank) | 3 | 30 |

Builds de référence validés le 13 octobre (§152), tank ajouté ; leur composition exacte est fixée en M0c. L'état « complet » se mesure sans les stats brutes du Campement (§4).

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

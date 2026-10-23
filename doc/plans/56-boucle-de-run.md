# Plan 56 — La boucle de run, revue et version finie

20 octobre 2026 · étape 2 du [LE-JEU.md](../LE-JEU.md) · [DECISIONS §194](DECISIONS.md) · revue demandée au [§191](DECISIONS.md) : « voir si c'est toujours ça qu'on prend ».

Même exercice que la revue de la Série ([plan 52 §15](52-elan.md)) : ce qui tourne, ce que la mesure montre, une version finie en quelques règles, ce qu'on retire, l'avocat du diable, les lots.

## 1. Ce qui tourne aujourd'hui (code et données du 19 octobre)

- **Trois terres, puis une 4e sans fin.** Chaque terre est une carte neuve d'un seul biome, tirée sans remise parmi cinq. Le `danger_level` des biomes est lu mais ne sert à rien. Le danger monte par le rang (+3 Péril par terre) et par l'horloge de run, qui ne repart jamais à zéro.
- **Une terre** : raviver trois Mémoriaux (tenir un cercle 20 s, foule ×1,6), toucher le repaire, battre le gardien. Le front ronge la terre des bords au cœur en 12 min. S'il atteint le cœur, le gardien se lève seul, enragé (PV ×1,5, dégâts ×1,3). Au réveil, le sol non ancré s'effondre.
- **Le gardien** est un Souverain du biome grossi (variante champion), avec les trois coups du gardien de référence (charge, frappe qui ouvre le vide, onde). PV : 2 500 × 1,8 par rang, plafonnés à 1,6 fois ce plancher. Pas de corps à lui, pas de lore.
- **La fin** : le 3e gardien vaincu, la run est gagnée. Puis une 4e terre où des échos de gardien se lèvent toutes les 2 min 30, jusqu'à la mort. **La Montée n'existe pas dans le code. L'Indicible existe, mais seulement sur la grande carte (`--no-lands`).**
- **Des systèmes calés sur l'horloge de run, aveugles aux terres** :
  - Résurgences : la première à 6 min, puis toutes les 290 s environ ;
  - chasses de Souverains à 4:00, 9:30, 21:00 et 27:00 ;
  - micro-événements ;
  - Failles, Ateliers, Puits et stèles (×0,34 par terre).

  Le [LE-JEU.md](../LE-JEU.md) les met en réserve depuis le §189. **Dans les données, ils sont toujours actifs.**
- **Au HUD** : une ligne d'objectif au-dessus de la minimap (« Terre 1 · Mémoriaux ravivés 0/3 ») et l'anneau de terre restante. Rien ne montre où l'on en est dans la run : terre 2 sur 3, ce qui vient ensuite.

## 2. Ce que la mesure montre

Bot des terres (`--lands-bot --nomad`), **profil neuf** (`MEASURE_PROFILE=normal`), invincible, graines 7 et 42 :
- 45 min avec le Vagabond ;
- 30 min avec le Traqueur et la Forgeuse ;
- le Traqueur aussi au commit du plan 51 (`efe91888`), pour vérifier s'il s'agit d'une régression.

| Cas | Terre 1 : réveil → vaincu (combat) | Terre 2 : réveil → vaincu (combat) |
|---|---|---|
| Vagabond, graine 7 | 2:02 → 7:19 (317 s) | 9:19 → 38:56 (**1 777 s**) |
| Vagabond, graine 42 | enragé à 11:25 → 35:14 (**1 429 s**) | 37:25 → 40:28 (183 s) |
| Traqueur, graine 7 | 1:50 → 5:12 (202 s) | 7:06 → non vaincu à 30:00 (**> 23 min**) |
| Traqueur, graine 42 | enragé à 11:25 → non vaincu à 30:00 | — |
| Forgeuse, graine 7 | 2:09 → 8:27 (378 s) | 10:32 → 16:57 (385 s) ; terre 3 : enragé à 27:50, non vaincu à 30:00 |
| Forgeuse, graine 42 | enragé à 11:25 → non vaincu à 30:00 | — |
| Traqueur au plan 51, graine 7 | 1:52 → 4:19 (147 s) | 6:14 → non vaincu à 30:00 |
| Traqueur au plan 51, graine 42 | enragé à 11:25 → non vaincu à 30:00 | — |

Lecture :
- **Le combat de gardien n'a pas de fin.** La charte vise 1 à 2 min ([plan 51 §11.4](51-objectifs-de-run.md)).
  - Le premier combat dure déjà de 2 min 30 à 6 min.
  - Dès la 2e terre, ou contre un gardien enragé, le bot met de 6 à 30 min, ou ne finit pas.
  - Aucune des huit runs n'est gagnée en 30 à 45 min.
  - Pendant le combat, l'horloge de run continue de faire monter la foule : de 65 à 543 créatures pour le Traqueur, de 60 à 520 pour le Vagabond.
  - Quatre Résurgences tombent en plein combat.
  - Les armes automatiques visent la créature la plus proche, donc la foule protège le gardien : le Présage Souverain n'atteint jamais sa phase 2, et le niveau du bot stagne (13 à 18 en 18 min).
  - Un joueur mortel y meurt. Le bot ne mourant pas, il donne la limite basse de la durée.
- **Ce n'est pas une régression.** Le commit du plan 51 donne le même mur. Le plan 51 annonçait des combats de 110 à 138 s et une run gagnée à 21 min 48 : ces chiffres venaient du profil dev, où tout est débloqué (profil par défaut de `measure_run.sh`). Ils ne valent pas pour un joueur qui débute.
- **La terre avant le gardien dure 2 min.** Les trois lumières s'allument entre 1:50 et 2:08 (graine 7). L'exploration est un court prélude au combat, et le front de 12 min ne se fait sentir que si le bot se bloque (graine 42 : il n'allume que deux Mémoriaux, le gardien se lève seul à 11:25).
- **Le départ est bon** : écran plein et Série ×10 en 19 à 46 s ([plan 55](55-rythme-du-depart.md)) ; niveau 10 entre 2 et 4 min sur les terres, où les cercles des Mémoriaux attirent la foule.
- **L'économie déborde en fin de run**, au bot invincible qui ne coupe jamais sa Série : 9 600 à 20 000 Essence en 45 min, dont la moitié vient de la Série. Un joueur réel coupe sa Série ; à regarder après la séance de Raphaël.

## 3. Diagnostic

1. **La run butera sur le 2e gardien** pour un joueur en profil neuf. C'est le défaut qui fera abandonner : il casse le pic que devait être le combat.
2. **Deux horloges se battent.** L'horloge de run (foule, Résurgences, Souverains, événements) ignore les terres, et l'horloge de la terre (le front) ignore la run. Le joueur subit l'une pendant qu'il lit l'autre.
3. **Trop de notions.** Une première terre montre la horde, les niveaux, la Série, les coffres, les Mémoriaux, le front, le vide et le gardien, plus les Résurgences, les Souverains, les événements, les Failles, les Ateliers, les Puits et les stèles. L'étape « clarté » vise cinq notions au plus.
4. **On ne sait pas où l'on en est.** Ni « terre 2 sur 3 », ni ce qui vient après, ni le nom du lieu. Raphaël, au §189 : le jeu « manque de dynamisme et de clarté sur le déroulement d'une partie ».
5. **La fin n'en est pas une.** Le 3e gardien gagne la run, puis une terre de plus continue sans le dire. La Montée et l'Indicible, actés au §190, n'existent pas.

## 4. La proposition : la run en trois règles

Affichées au joueur, une fois, dans cet ordre :

1. **Ravive trois Mémoriaux, puis réveille le gardien.** Inchangé. Le repaire s'ouvre à trois lumières. Chaque lumière de plus durcit le gardien et enrichit sa récompense.
2. **La terre s'efface derrière toi.** Inchangé : le front ronge des bords au cœur. S'il arrive au cœur, le gardien se lève seul, enragé.
3. **Trois terres, puis la Montée.** Au bout de la Montée, l'Indicible. Le vaincre gagne la run.

**Ce qui change pour que ces règles tiennent :**

- **Le combat de gardien est une arène fermée.** Au réveil, l'horloge de difficulté s'arrête jusqu'à la mort du gardien : la foule ne grossit plus, rien d'autre n'arrive (ni Résurgence, ni événement, ni Souverain), et la foule est plafonnée à un nombre fixe. Visée : 1 à 2 min au profil neuf, mesurées au bot sur trois personnages. C'est la condition des séances de jeu des étapes 1 et 4.
- **Une seule horloge, celle de la terre.** On retire de la run ce qui vit sur l'horloge de run : Résurgences, chasses de Souverains, micro-événements, Ateliers, Puits et stèles. Ils passent en réserve, éteints dans les données, comme le dit le [LE-JEU.md](../LE-JEU.md). Les pics d'une terre sont les cercles des Mémoriaux (foule ×1,6) et le gardien ; ses creux sont la marche entre les deux. Restent : coffres, Failles (le gambling) et Mémoriaux.
- **La run se lit d'un coup d'œil.**
  - Chaque terre porte le nom d'un lieu de Vaulme : les bois de la colline, les fermes du plateau, la Haute-Ville, le Bas-Port, la carrière des Aubiers.
  - Un carton d'arrivée annonce « Terre 2 · Le Bas-Port ».
  - Une piste de run, sous le chrono, montre quatre marques (trois terres, puis la Montée), celle en cours allumée.
- **La Montée, finale fixe** (étape 6 du LE-JEU, définie ici, codée plus tard) :
  - une carte faite main, la même à chaque run : le chemin en lacets qui monte du Bas-Port à la Haute-Ville, étroit, bordé de vide. C'est le lieu idéal pour pousser la horde dans le vide ;
  - le front monte du bas : on avance ou on tombe. Environ 4 à 5 min ;
  - à mi-pente, la barrière que le maire a fait fermer, tenue comme un Mémorial (un cercle, la foule qui converge) ;
  - en haut, l'Indicible ([lore](../VESTIGES-LORE.md) : « la nuit du 14 elle-même »), avec le code qui existe déjà sur la grande carte.
- **La victoire laisse choisir** (le modèle de Brotato). L'Indicible vaincu, la run est gagnée et comptée. Deux choix :
  - **redescendre** : fin de run, bilan de victoire ;
  - **rester dans la nuit** : la fin sans fin, sur le sommet. Les échos des gardiens vaincus se lèvent l'un après l'autre (le mécanisme L2 existe), le Péril monte, et le classement compte le temps tenu.

  La 4e terre actuelle disparaît.
- **Durée visée** pour un joueur moyen : 6 à 8 min par terre, 4 à 5 min de Montée, **25 à 30 min pour gagner**, soit le « 20 à 30 min » de Raphaël au §185.

**Leviers de maîtrise**, découverts en jouant :
- réveiller le gardien à trois lumières, ou chercher une 4e et une 5e pendant que le sol s'en va (le pari de Megabonk, où le boss appelé tard est plus fort et rapporte plus) ;
- choisir les Mémoriaux à raviver : le sol qu'on ancre dessine l'arène du gardien ;
- la Série et le vide pendant le combat : l'arène fermée donne enfin le temps de jouer les coups du gardien ;
- dans la Montée, pousser la horde dans le vide plutôt que la tuer.

## 5. Deux options pour la règle 1

**A (recommandée) : les Mémoriaux comme clés**, comme aujourd'hui. On va dans les lieux, on les tient, le gardien s'ouvre à trois. Le chantier des terres (étape 5) posera ses pièces faites main autour des Mémoriaux : « on se souvient des lieux, pas des chemins ».

**B : le gardien quand tu veux** (le portail de Megabonk). Le repaire est ouvert dès l'arrivée et le gardien se renforce avec le temps. Les Mémoriaux ne donnent plus que leur puissance et leur ancre. Une règle de moins, et un pari plus net (tôt : plus faible, moins de butin).

Pourquoi A : avec B, le meilleur coup est de foncer au repaire, et les terres redeviennent des couloirs, ce que Raphaël reproche déjà aux terres actuelles (« très linéaires, plats et vides »). A garde aussi le code et le tutoriel en trois étapes déjà écrit (raviver, réveiller, passer).

## 6. Retiré

- La 4e terre sans fin (remplacée par la fin sans fin au sommet de la Montée, par choix).
- Dans la run : Résurgences, chasses de Souverains, micro-événements, Ateliers, Puits et stèles. Ils passent en réserve, sans être supprimés.
- L'horloge de difficulté pendant le combat de gardien.

## 7. Avocat du diable

- **L'arène fermée retire de la pression.** Un combat de gardien sans foule montante peut devenir un duel trop calme. Réponse : la foule reste, plafonnée, et les coups du gardien ouvrent le vide sous le joueur. Si c'est trop calme, on remonte le plafond, pas l'horloge.
- **Retirer les Résurgences retire un pic.** Elles étaient le seul événement « tout l'écran » des runs d'avant. Réponse : chaque cercle de Mémorial en est un petit, trois par terre, et c'est le joueur qui choisit quand.
- **La Montée est une carte faite main, donc un vrai chantier.** C'est l'étape 6 du LE-JEU, déjà prévue ; la revue ne fait que la définir.
- **Trois terres, puis la finale, puis le sans fin : c'est la forme de Megabonk.** Raphaël l'a dit au §185 : « ça ressemble trop à Megabonk […] malheureusement, ça marche très bien ». L'identité vient de la Série, du vide et de Vaulme, pas du squelette.

## 8. Lots

| Lot | Contenu | Effort |
|---|---|---|
| B1 | **L'arène du gardien** : horloge de difficulté arrêtée pendant le combat, rien d'autre ne se déclenche, foule plafonnée ; même chose pour un écho. Mesure au bot, profil neuf, trois personnages, graines 7 et 42 : combat de 1 à 2 min, run gagnée | Faible à moyen |
| B2 | **Une seule horloge** : Résurgences, chasses de Souverains, micro-événements, Ateliers, Puits et stèles éteints en run par interrupteur de données (réserve du LE-JEU). Régressions qui en dépendent rendues indépendantes de l'interrupteur | Faible |
| B3 | **La run se lit** : nom de lieu de Vaulme par terre, carton d'arrivée, piste de run sous le chrono (trois terres puis la Montée), textes traduits ; captures | Moyen |
| B4 | **La Montée et la fin au choix** : étape 6 du LE-JEU, à découper dans son plan quand on y arrive | Moyen à élevé |

B1 à B3 viennent avant la séance de Raphaël : une vraie run ne se juge pas avec un 2e gardien de 20 min. B4 garde sa place à l'étape 6, après le chantier des terres.

## 9. Points d'accroche (inventaire du 19 octobre)

- **Gardien** : `scripts/World/GuardianDirector.cs` (phases `Sleeping`, `Fighting`, `Defeated`, `Passage`, `Holding`, `Echo` ; réveil l. 172-210 ; Péril de rang l. 415) ; `data/world/guardian.json` (`fight_crowd_density` 0,5).
- **Horloge de run** : `SpawnManager.ElapsedSeconds` (`scripts/Spawn/SpawnManager.cs:389`) porte la montée de la foule ; `CrisisManager` ne connaît pas `LandChanging`.
- **Événements** : `data/events/run_events.json` (`fixed_events` des Souverains), `RunEventDirector` (l'événement en cours échoue au changement de terre, l. 283).
- **Fin de run** : `EndgameManager.cs` (`ShouldForceBoss` faux sur les terres, l. 109 ; victoire l. 168-185) ; Indicible : `scripts/Combat/Indicible*.cs`, `data/scaling/indicible.json`.
- **HUD** : `scripts/UI/LandObjective.cs`, `Minimap.cs` (anneau, l. 354-362), chrono `HUD.cs` (l. 531-543).
- **Mesure** : `--lands-bot --nomad` ; `tools/summarize_run_measurements.py` ne lit pas les lignes `LAND`. Toujours mesurer en `MEASURE_PROFILE=normal` : le profil dev par défaut fausse les durées de combat.

Sources (recherche du 19 octobre) : [Megabonk, minuteur et étapes](https://megabonk.org/guides/mechanics/timer/), [Megabonk, boss appelé tôt ou tard](https://gamerblurb.com/articles/megabonk-difficulty-and-duration-guide), [Brotato, vagues et mode sans fin](https://brotato.wiki.spellsandguns.com/Waves), [Brotato, abandon après la vague 20 compté comme victoire](https://brotato.wiki.spellsandguns.com/Patch_1.1.*.*), [Deep Rock Galactic: Survivor, étapes et élites](https://deeprockgalactic.wiki.gg/wiki/Survivor:Elimination), [Hades, salles et gardiens](https://hades.fandom.com/wiki/Chambers_and_Encounters), [Halls of Torment, runs jugées longues](https://www.pushsquare.com/reviews/ps5/halls-of-torment), [Death Must Die, boss « dps check »](https://steamcommunity.com/app/2334730/discussions/0/4515507392492350728).

## 10. Compte rendu B1, l'arène du gardien — 23 octobre 2026

**Ce qui change :**
- **L'horloge de difficulté s'arrête pendant le combat.** `RunClock` porte deux dates : le temps joué (chrono, bilan, quêtes, Effacement) et le temps de difficulté (foule, Péril du temps, élites, Résurgences, micro-événements). Le second s'arrête du réveil du gardien, ou d'un écho, jusqu'à sa mort ou au changement de terre (signal `GuardianArenaChanged`).
- **Rien d'autre ne se déclenche.** Une Résurgence en cours au réveil s'arrête. Un micro-événement en cours finit échoué. Le calendrier des suivants reprend à la mort du gardien.
- **La foule est plafonnée** à 40 créatures actives (`fight_crowd_cap`), et toujours deux fois moins dense. Les renforts appelés par un Hurleur tiennent désormais le plafond de la run, et celui de l'arène : un Hurleur hors d'atteinte appelait 2 Ombres toutes les 8 s, sans limite (1 208 Ombres en un combat, foule de 935).
- **Le gardien passe devant la foule dans le choix des cibles** (`target_bias` 100 px), comme les parties de boss depuis le 10 octobre. Sans ce biais, les armes visaient toujours la créature la plus proche : 420 dégâts par seconde infligés, presque aucun au gardien.
- **Le gardien naît sans affixe tiré au sort.** Un gardien Cuirassé encaissait 1,7 fois ses PV.
- **PV du gardien revus** : plancher 1 500 au lieu de 2 500, ×1,35 par terre au lieu de ×1,8, plafond à 1,3 fois le plancher au lieu de 1,6. Enragé, ×1,25 au lieu de ×1,5. En profil neuf, le bot met 15 à 30 dégâts par seconde sur le gardien, et ne gagne que 4 à 5 niveaux entre deux terres. Avec ×1,8 par terre (2 500, 4 500, 8 100), le gardien grandissait plus vite que le joueur.
- **Bot des terres** : il se bat sur le sol, à portée du gardien. Avant, la frappe du gardien ouvrait le vide sous lui. Invincible, il y restait, et ses armes de mêlée ne touchaient plus rien, puisque les créatures n'entrent pas dans le vide. Il laisse aussi un Mémorial hors d'atteinte au bout de 60 s, au lieu de s'y coincer toute la terre.

**Mesure** : bot des terres, profil neuf (`MEASURE_PROFILE=normal`), 30 min, graines 7 et 42, Traqueur, Vagabond et Forgeuse. Durée du combat, du réveil à la mort du gardien :

| Cas | Terre 1 | Terre 2 | Terre 3 | Avant B1 (terre 1 / terre 2) |
|---|---|---|---|---|
| Traqueur, graine 7 | 73 s | 105 s | enragé, non vaincu après 12 min | 202 s / non vaincu après 23 min |
| Traqueur, graine 42 | enragé, 90 s | 409 s | — | enragé, non vaincu |
| Vagabond, graine 7 | 110 s | 264 s | 324 s, **run gagnée à 18:54** | 317 s / 1 777 s |
| Vagabond, graine 42 | enragé, 139 s | 87 s | enragé à 28:27 | enragé, 1 429 s / 183 s |
| Forgeuse, graine 7 | 82 s | 112 s | enragé, 60 s, **run gagnée à 19:22** | 378 s / 385 s |
| Forgeuse, graine 42 | enragé, 212 s | 91 s | enragé à 29:48 | enragé, non vaincu |

Lecture :
- **Visée de 1 à 2 min, atteinte dans 7 combats sur 10** hors gardien enragé. Les écarts : 264, 324 et 409 s. Avant B1, aucun combat de 2e terre ne finissait en moins de 6 min.
- **Deux runs gagnées sur six, vers 19 min.** Avant B1, aucune ne l'était en 30 à 45 min.
- **Les quatre autres runs butent sur la 3e terre (ou la 2e), sans être perdues au combat.** Le bot n'y rallume qu'un ou deux Mémoriaux : il laisse trois à cinq Mémoriaux hors d'atteinte par run, faute de contourner l'eau et les immeubles. Le gardien finit par se lever enragé, au cœur seul (2 % de la terre), avec 4 442 PV. Le Traqueur graine 7 ne l'abat pas en 12 min. C'est le pathfinding du bot qui bloque, pas le combat : un joueur trouve son chemin.
- **Étapes intermédiaires** (même outil, même protocole) :
  - arène seule, foule à 60 : le Vagabond graine 42 restait figé dans le vide ;
  - bot corrigé : 87 à 586 s ;
  - sans affixe : 133 à 955 s ;
  - nouveaux PV : 81 à 474 s, mais foule de 935 par le Hurleur ;
  - renforts plafonnés et biais de visée : le tableau ci-dessus.
- **Plafond à 25 essayé**, puis écarté. Les combats enragés s'allongent : 230 à 302 s contre 90 à 212 s, et la médiane n'est pas meilleure. Un combat à 25 créatures serait aussi plus calme (avocat du diable, §7).

**Vérifications** :
- `dotnet build` : 0 avertissement ;
- smoke test vert ;
- `tools/test_lands.sh` : 0 échec. Il vérifie que l'arène se ferme au réveil, que le chrono avance sans la difficulté, et que l'arène se rouvre à la mort du gardien ;
- `tools/test_enemy_abilities.sh` et `tools/test_movement.sh` : 0 échec ; `tools/test_dev_mode.sh` vert ;
- relecture `godot-reviewer` : aucun chemin où l'arène resterait fermée. Un double commentaire de documentation a été corrigé.

La vérification « à mi-PV, deuxième phase » échouait déjà sur `main`, parce que les 50 % de PV infligés ne passaient pas l'armure d'un gardien Cuirassé. Elle frappe désormais jusqu'à mi-PV.

Pas de capture : rien de visible ne change à l'écran, hors la foule. Pas de banc de performance : le coût ajouté est un calcul de plafond par image.

**Points ouverts** :
- le bot ne contourne pas les obstacles pour rejoindre un Mémorial : la run gagnée ne se mesure qu'aux graines où les Mémoriaux sont dégagés ;
- un gardien enragé de 3e terre sur le seul cœur reste long au bot ;
- les deux se jugent à la séance de Raphaël, après B2 et B3.

## 11. Compte rendu B2, une seule horloge — 23 octobre 2026

**Ce qui est éteint en run**, par un champ `enabled` de leurs données, sans suppression :
- **Résurgences** : `data/scaling/crises.json`. `CrisisManager` ne programme plus rien, et une crise lancée par le mode dev va à son terme ;
- **micro-événements et chasses de Souverains** : `schedule.enabled` de `data/events/run_events.json`, ni rendez-vous fixes ni tirage. Un événement forcé (mode dev, banc) va à son terme ;
- **Ateliers, Puits et stèles de Péril** : `enabled` de chaque section de `data/world/landmarks.json`. `LandmarkSpawner` ne les place plus.

Les clés absentes valent « allumé » pour les crises et les événements, « éteint » pour les trois lieux. Seule la terre donne le temps : Mémoriaux, gardien, Effacement.

**`--reserve`** (outils seulement, comme `--no-lands`) rallume le tout pour un banc ou une régression (`ReserveSwitch`). Une version exportée l'ignore.

**Quêtes** : cinq dépendent des systèmes éteints. Elles passent en réserve (`"reserve": true`) : cachées du carnet, non suivies, avec « En réserve pour l'instant. » sur leur pièce.
- Développer : arme Instantané ;
- Compter les Résurgences : arme Lampe à pétrole ;
- Dernière émission : arme Transistor ;
- Les gestes connus : Réminiscence Habitude, déjà en attente de son effet ;
- Une trace de moins : objet Portrait effacé.

Leurs pièces restent verrouillées : trois armes, une Réminiscence et un objet sortent des tirages tant que la réserve dure. C'est le choix le plus réversible. L'autre choix, libérer les pièces, les donnerait sans quête, et rallumer la réserve les reprendrait.

Les quêtes de Souverains restent ouvertes : un gardien est un champion, et son élimination compte.

**Bilan** : la ligne « Résurgences » ne s'affiche plus quand elle vaut 0.

**Effet de bord** : sans Résurgence, la phase de fin de partie (`EndgameManager`) ne vient plus que de l'Effacement global ou du dernier gardien vaincu. Avant, elle venait aussi de la n-ième Résurgence. En terres, l'Indicible n'est pas forcé au temps : rien d'autre ne change.

**Régressions** : la suite `quests` rallume la réserve, par réflexion, le temps du contrôle d'une quête en réserve. Elle ajoute un contrôle : 5 quêtes en réserve, toutes cachées sans `--reserve`. Les autres suites (Résurgences, micro-événements, lieux, terres) restent vertes sans changement.

**Vérifications** :
- `dotnet build` : 0 avertissement ;
- smoke test vert ;
- `tools/validate.sh` : 39 suites sur 40 vertes au premier passage, puis `quests` verte après correction de son décompte (325 contrôles, 0 échec). Après la relecture, `run_phase`, `run_trace`, `indicible`, `lands`, `quests` et `dev_mode` : 6 suites sur 6 ;
- relecture `godot-reviewer` : aucun bug bloquant. L'effet de bord sur la fin de partie est documenté ci-dessus, et l'état initial de l'interrupteur des crises est harmonisé (éteint jusqu'à la lecture des données).

Pas de capture ni de banc : il n'y a rien de neuf à l'écran, et le coût baisse (moins de systèmes en run).

**Point ouvert** : les Failles restent en run. Ce sont un pari, pas une horloge.

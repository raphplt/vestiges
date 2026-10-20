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

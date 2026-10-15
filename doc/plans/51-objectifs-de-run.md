# Plan 51 — Objectifs dans la run : la Descente

15 octobre 2026. Étude demandée au [§185](DECISIONS.md), sur le manque relevé par le premier testeur ([§184](DECISIONS.md), [plan 50 §8](50-retours-premier-testeur.md)). **Rien n'est implémenté ; propositions à trancher (§8).**

Ce que Raphaël a fixé :
- l'Indicible ne vient plus à heure fixe, le joueur l'appelle (Mémoriaux ou portail) ;
- ce qui l'y pousse, c'est une difficulté qui monte sans cesse jusqu'à devenir intenable ;
- piste A (Mémoriaux) presque acquise, piste C (terres successives) à étudier, éventuellement ensemble ;
- le ressenti visé est une course contre l'Effacement, avec des choix serrés ;
- la piste narrative et les contrats sont écartés ;
- le lore peut être retouché en profondeur, s'il reste cohérent.

## 0. En bref

- **Proposition : la Descente.** Une run descend de la colline vers la mer, en deux paliers séparés par la Barrière.
  - **Palier haut** : forêt, champs, Haute-Ville. On y ravive des Mémoriaux. Avec deux voix, la Barrière s'éveille quand le joueur s'en approche ; chaque voix de plus ajoute un battant et un coffre.
  - **Palier bas** : Bas-Port et carrière. Trois voix de plus rendent la nuit dicible, et le joueur appelle l'Indicible sur le quai.
  - Le palier quitté s'effondre dans le Néant derrière lui. L'horloge de difficulté ne s'arrête jamais.
- **Le lore le porte presque tel quel.**
  - La Barrière est déjà la barrière de la Montée, que la Haute-Ville a fermée devant le Bas-Port la nuit du 14.
  - « Une mort sans témoin n'a pas lieu » : raviver les Mémoriaux du port, c'est donner des témoins aux noyés, et la nuit se lève.
- **Faisabilité : tout tient dans une seule carte, sans régénération.** C'est un « C léger », estimé à environ 3 à 4 semaines, piste A comprise.
  - Les vraies terres régénérées (une carte neuve par palier) coûtent 2 à 3 semaines de plus. Le code suppose partout une carte unique.
  - Elles restent possibles ensuite, puisque le palier sera déjà l'unité de découpage.
- **Prérequis technique : une horloge de run unique.** Aujourd'hui, sept compteurs de temps indépendants pilotent la run.
- **Ordre proposé** :
  1. l'horloge ;
  2. l'Indicible appelé, testable sur la carte actuelle ;
  3. la carte en paliers et la Barrière fixe ;
  4. en option, la régénération.

## 1. Ce que font les autres jeux (recherche du 15 octobre)

| Jeu | Ce qui fait passer à la suite | Ce qui pousse à partir |
|---|---|---|
| Risk of Rain 2 | Trouver le téléporteur, le charger 90 s dans sa zone et tuer le boss. 5 étapes, puis la boucle recommence | Difficulté = (1 + 0,0506 × minutes) × 1,15^étapes, alors que le butin de chaque carte est fini. Le conseil courant est de ne pas dépasser 3 min avant d'activer. L'autel de la Montagne double le boss contre le double de butin |
| Megabonk | Porte du boss à trouver ; chrono de 10 min par étape ; 1 à 3 étapes selon le palier | À la fin du chrono, une nuée infinie de fantômes. Elle a été détournée pour farmer, puis corrigée par patch |
| DRG: Survivor | Barre d'objectif qui avance seule, élite, puis la capsule de départ. 5 étapes | 30 s pour rejoindre la capsule, sinon la run est finie |
| Halls of Torment, HoloCure, 20 Minutes Till Dawn | Boss à l'heure, survivre au chrono | Chrono fixe : notre situation actuelle |
| Soulstone Survivors | Un quota d'éliminations fait venir le boss | Le joueur fixe le rythme |

Sources : [wiki RoR2, difficulté](https://riskofrain2.wiki.gg/wiki/Difficulty), [téléporteur](https://riskofrain2.wiki.gg/wiki/Teleporter), [autel de la Montagne](https://riskofrain2.wiki.gg/wiki/Shrine_of_the_Mountain), [PC Gamer sur Megabonk](https://www.pcgamer.com/games/roguelike/megabonk-review/), [Destructoid](https://www.destructoid.com/how-to-go-to-the-next-stage-in-megabonk/), [wiki DRG: Survivor](https://deeprockgalactic.wiki.gg/wiki/Survivor:Elimination).

**Leçons :**
- **La pression la plus saine** naît d'une ressource finie face à une menace qui monte avec le temps (RoR2). Un temps illimité pousse à farmer, et les correctifs arrivent après coup (la nuée de Megabonk).
- **Chercher l'objectif lasse** sur une grande carte : c'est une plainte récurrente sur RoR2 (Scorched Acres). Il faut le voir de loin.
- **Les étapes lassent quand il y a peu de cartes** (IGN sur Megabonk, Ars Technica sur Halls of Torment). C'est l'argument contre des terres qui se ressembleraient.
- **Le pari « boss plus dur, butin plus riche »** de l'autel de la Montagne existe déjà chez nous : un Mémorial ravivé ajoute un battant à la Barrière, et chaque battant rend un coffre.

## 2. Ce qui existe

### Calendrier

Tout arrive à heure fixe :
- micro-événements de 1:10 à la fin ;
- chasses de Souverains à 4:00, 9:30, 21:00 et 27:00 ;
- Résurgences toutes les 6 min environ ;
- **Barrière à 15:00** ;
- **Indicible à la 5e Résurgence, ou au plus tard à 30:00**.

Pendant ce temps, l'Effacement monte avec le temps, la distance et l'absence du joueur.

**Le temps n'a pas de source unique.** Sept compteurs avancent chacun de leur côté, chacun avec sa propre garde :
- `SpawnManager._elapsedTime` sert déjà d'horloge de difficulté : PV, dégâts, densité, élites, créatures débloquées, Péril du temps. Le mode dev la règle, et la Barrière et le contenu des micro-événements la lisent ;
- `CrisisManager`, `RunEventDirector`, `EndgameManager` et `ErasureManager` ont chacun leur compteur ;
- `RunTracker` et `QuestTracker` comptent le temps réel, ce qui est juste pour eux.

### Mémoriaux

- **Nombre et placement** : 9 par carte, dont 1 près du départ, à 900 px au moins de tout autre lieu.
- **Raviver** : 3 éclats à ramasser en 20 s, mais un seul suffit. On reçoit 3 bénédictions au choix, plus des services (soin, lever un Oubli).
- **Contre l'Effacement** : stabilisation ponctuelle de la zone.
- **Perte** : un Mémorial est perdu quand sa zone passe au Néant, même déjà ravivé.
- **Ce qui manque** :
  - pas de pointeur vers les Mémoriaux ;
  - pas de compteur au HUD ;
  - même icône sur la minimap quel que soit l'état.

### Barrière

- Elle se lève à 15:00, à 150 px devant le joueur, avec des ailes de chaînes de 3 000 px pour empêcher de la contourner.
- Battants : 1 + Mémoriaux ravivés, 5 au plus. Elle rend un coffre par battant.
- Elle se passe dès qu'un battant est brisé.
- Mesurée entre 2 et 9 min, pour 60 à 90 s visées.

### Indicible

- Il apparaît autour du joueur, pendant la 5e Résurgence, dans la nuit tombée.
- Trois phases : tempête, marée, vagues.
- PV ×3,2, sans croissance avec le temps.
- Sa mort fait passer en Endgame : multiplicateurs relevés, Résurgences plus serrées, difficulté suivante ouverte.

### Carte

- Une ellipse d'environ 200 × 400 cellules, avec les **5 biomes sur 5** en mosaïque d'environ 87 régions.
- 11 000 décors, 39 coffres et 39 grands lieux.
- Génération en 3,6 s, jusqu'à 7 s sur une machine chargée.

## 3. La proposition : la Descente

### Le trajet

La vallée descend de la colline vers la mer. Cela suit la géographie du lore ([VESTIGES-LORE.md §2](../VESTIGES-LORE.md)) et le gradient de danger des biomes (Stratégie V2 §9) :

```
PALIER HAUT : la colline et la ville           PALIER BAS : le port
forêt, champs (danger 1) → Haute-Ville (2)  ║  Bas-Port, carrière (danger 3)
départ ···· Mémoriaux (voix) ···· Barrière ═╬═ ···· Mémoriaux ···· le quai : Indicible
                                            ║
                       la barrière de la Montée, en travers de la carte
```

1. **Départ** sur le plateau. La Barrière est **déjà là**, en travers de la carte : un mur de chaînes visible sur la minimap dès le début. Le but se voit, il n'y a rien à chercher.
2. **Palier haut.**
   - On ravive des Mémoriaux, qui comptent comme des voix. Avec **2 voix**, la Barrière s'éveille quand le joueur s'en approche ; avant cela, c'est un mur inerte.
   - Chaque voix de plus ajoute un battant et un coffre, jusqu'à 5 : « plus on se souvient, plus elle résiste », le pari de l'autel de la Montagne, à notre façon.
   - Le joueur choisit quand y aller.
3. **Le passage.** Dès qu'un battant tombe, on passe.
   - Le palier haut se détache derrière le joueur et s'effondre dans le Néant en 45 à 60 s : ce qu'on n'a pas pris est perdu.
   - C'est le moment « jusqu'où » rendu littéral.
4. **Palier bas.** Le Bas-Port et la carrière, avec les créatures les plus dures.
   - Les Mémoriaux du port sont les noyés et leurs témoins. **3 voix** rendent la nuit dicible.
   - Le joueur appelle alors l'Indicible au quai. C'est un lieu fixe, visible et protégé de l'Effacement.
5. **Après l'Indicible** : late game sur place, selon le plan 34 (Profondeur, Marées), jusqu'à la mort. En option plus tard : la boucle (§6).

### Ce qui pousse à avancer

- **L'horloge de difficulté** monte sans pause, comme aujourd'hui (PV ×1,13 par minute après 8 min). Elle ne s'arrête pas au passage d'un palier.
- **Le Péril du temps**, déjà codé mais éteint (`time_peril_per_minute` à 0 avant 15 min), devient l'aiguille visible de cette montée. Par exemple : +1 Péril toutes les 2 à 3 min, affiché au HUD.
  - Attendre trop longtemps ne déclenche rien de forcé. Simplement, au bout d'un moment, le joueur ne tient plus : c'est ce que Raphaël demande.
- **L'Effacement mange le palier** : il emporte les Mémoriaux et les services pas encore pris. La ressource est finie et s'amenuise, comme chez RoR2.
- **Les Résurgences restent le pouls de la run** : la nuit qui remonte, sur l'horloge. Elles ne déclenchent plus l'Indicible.
- **Les chasses de Souverains** suivent la même horloge.

### Garde-fous

- **Éviter le blocage** :
  - le seuil baisse si des Mémoriaux sont perdus (« on ne peut pas appeler une voix qui n'est plus ») ;
  - le quai et la Barrière ne s'effacent jamais ;
  - le palier haut compte 5 Mémoriaux, le palier bas 4 à 5.
- **Pas de farm infini** : le butin est fini (coffres et lieux du palier), l'horloge tourne et le Péril monte. Il n'y a pas de source sans fin à exploiter, contrairement à la nuée de Megabonk.
- **Jouer vite reste un vrai choix** : on peut forcer la Barrière dès 2 voix et sauter des coffres.
- **Mesure à fixer en jeu** : passage vers 12–15 min et Indicible vers 25–30 min pour un joueur moyen, selon la cible du plan 47.

## 4. Le lore

[VESTIGES-LORE.md](../VESTIGES-LORE.md) fait foi ; la Bible §1–3 est remplacée.

**Ce qui soutient déjà la proposition :**
- §7 : « Chaque Mémorial ravivé est quelqu'un de plus qui frappe à la barrière » et « plus on se souvient, plus elle résiste ». La Barrière **est** la barrière de la Montée.
- §1 : « Une mort sans témoin n'a pas lieu ». Raviver les Mémoriaux du port donne des témoins aux 31 noyés, et la nuit du 14 devient dicible : l'Indicible se lève.
- §7 bis : « Toute marche mène à la nuit du 14. Plus on marche, plus on s'en approche ». §9.2 : la vraie fin demande « l'Indicible atteint », c'est un verbe de destination.
- §1 : « On se souvient des lieux, pas des chemins ». Une fois la barrière passée, plus personne ne tient le chemin ni le palier d'en haut, qui se détache.
- La pression qui monte prend un sens : attendre, c'est faire ce qu'a fait Vaulme, « rester chez soi, portes closes ».

**À réécrire si c'est retenu :**
- §7 bis « Ce qu'il a vu reste à sa place tant qu'il marche » : ajouter la règle du palier quitté.
- §7 bis, la difficulté : elle monte « par la marche », ajouter « et par l'attente ».
- §8.2 : la cadence des Résurgences.
- §7 bis : la Montée « dans chaque carte » devient la run entière.
- §2 et §11 : le Néant au bord et l'au-delà inconnu ne bougent pas. La Descente reste dans la vallée.
- Stratégie V2 §6, §9, §11 et §14 ; plans 07, 33 §8 et 34 (calendriers à 15:00 et 30:00).

**Variante écartée en recommandation : la montée** (du port vers la Haute-Ville). Elle fait partir le joueur dans le marais, de danger 3, et contredit la Stratégie V2 §9.

## 5. Faisabilité

### L'horloge unique (prérequis)

- Un service `RunClock` porte le temps réel, le temps de difficulté et un éventuel facteur de vitesse.
- `SpawnManager`, `CrisisManager`, `RunEventDirector`, `EndgameManager` et `ErasureManager` le lisent au lieu de leur compteur.
- Le temps réel reste pour le bilan, le HUD et les quêtes.
- **À trancher** : l'ouverture (`opening_*`, `early_*`) se refait-elle à l'arrivée sur un palier ? Je propose non : le palier bas n'a pas d'ouverture.
- 2 à 3 jours, sans changement de jeu : mesure avant/après identique.

### L'Indicible appelé

- `EndgameManager` : retirer le forçage à la 5e Résurgence ou à 1 800 s ; ajouter l'appel et un lieu-quai interactif.
- Un compteur de voix unique : il y en a deux aujourd'hui, dans `BarrierDirector` et `QuestTracker`.
- Interface :
  - compteur de voix au HUD ;
  - états des Mémoriaux sur la minimap ;
  - pointeurs vers les Mémoriaux et le lieu d'appel.
- PV de l'Indicible : ils doivent suivre l'horloge. Aujourd'hui ils sont fixes, alors l'appeler tard le rendrait trop facile.
- Le bot de mesure (`RunObservation`) doit apprendre à raviver et à appeler.
- 6 à 8 jours. **Testable sur la carte actuelle** : la Barrière y reste à l'heure en attendant.

### La carte en paliers (C léger)

- **Génération** : biomes par bande au lieu d'une mosaïque libre (`WorldGenerator.AssignBiomes` et `CreateBiomeRegions`). Il faut une ligne de seuil traversant la carte, le départ au bout du plateau, et des lieux répartis par palier (`SitePlacer`, `landmarks.json`).
- **Barrière fixe** : posée à la génération sur le seuil, au lieu d'apparaître devant le joueur. Elle s'éveille au contact avec assez de voix. Les ailes et la gestion des bords existent déjà.
- **Effondrement du palier quitté** : un mode « palier » dans `ErasureManager`, qui pousse les cellules du palier haut vers le Néant. Les lieux perdus le sont déjà par ce chemin.
- 8 à 12 jours, réglage et mesures compris.

### Les vraies terres régénérées (option)

- Tout l'état de run vit dans des nœuds de `Main` : joueur, inventaire, Essence, Péril, score, quêtes. Il n'existe pas de sauvegarde instantanée.
- **Recharger la scène** coûte 3 à 5 semaines, à haut risque : une quinzaine de systèmes à transporter.
- **Régénérer en place** coûte 2 à 3 semaines :
  - isoler ce que la carte produit, puis vider et reconstruire ;
  - remettre à zéro une vingtaine de systèmes sur un signal ;
  - transition avec l'écran de chargement existant ;
  - ajouter un test qui enchaîne trois terres.
- Le calcul de la terre suivante peut tourner en tâche de fond. Il reste 1,5 à 2,5 s de chargement à cacher, par exemple derrière le passage.
- **Ce que ça apporterait** :
  - des terres tirées parmi des variantes, comme les rangs de RoR2 ;
  - une boucle infinie à la place du late game sur place ;
  - des cartes plus petites et plus denses.
- **Le risque** : avec 5 biomes seulement, il faudrait plusieurs terres par palier pour éviter la monotonie, et c'est la critique faite à Megabonk.

## 6. Plus tard : la boucle (si les terres régénérées sont retenues)

Après l'Indicible, au lieu de rester sur place, le joueur « remonte » : une nouvelle Descente commence, avec une difficulté multipliée et des terres différentes. Le joueur boucle déjà, selon le lore : ce sont des oubliés « qui savent qu'ils bouclent » (§7 bis).

Le late game du plan 34 deviendrait la deuxième boucle et les suivantes. C'est cohérent, mais ça dépend entièrement de la régénération.

## 7. Lots proposés

| Lot | Contenu | Coût estimé | Jouable à la fin |
|---|---|---|---|
| **O0 Horloge** | `RunClock` unique, compteurs remplacés, mesure avant/après identique | 2–3 j | Rien de visible |
| **O1 Voix et Indicible appelé** | Compteur de voix, lieu d'appel, fin du forçage à 30:00, Péril du temps visible, PV de l'Indicible sur l'horloge, pointeurs et minimap, bot | 6–8 j | Piste A sur la carte actuelle |
| **O2 La Descente** | Carte en deux paliers, Barrière fixe éveillée par les voix, effondrement du palier quitté, lieux par palier | 8–12 j | La proposition complète |
| **O3 Lore et docs** | Script (§4), Stratégie V2, plans 07, 33, 34 ; première run guidée (plan 50) qui mène aux Mémoriaux puis à la Barrière | 2–3 j | Onboarding |
| O4 Terres régénérées (option) | Voir §5 | 2–3 semaines | Boucle, variantes de terres |

O1 peut se juger en jouant avant d'engager O2.

## 8. Questions à Raphaël

1. **Le sens** : descendre vers la mer (recommandé, cohérent avec le lore et le danger) ou monter vers la Haute-Ville ?
2. **Le nombre de paliers** : deux, avec la Barrière comme seule couture (recommandé), ou trois, avec un nouveau gardien à concevoir ?
3. **L'ampleur** : la carte en paliers d'abord (recommandé), ou directement les terres régénérées ?
4. **Le lieu d'appel de l'Indicible** : le quai du Bas-Port (recommandé), le clocher (sonner le tocsin qu'on a empêché de sonner à 23 h 10), ou le dernier Mémorial ravivé ?
5. **La pression visible** : le Péril du temps affiché, qui monte seul (recommandé), ou une autre aiguille, par exemple l'heure de la nuit du 14, de 17 h à 23 h 40 ?
6. **Les seuils de départ** : 2 voix pour la Barrière et 3 pour l'Indicible ?

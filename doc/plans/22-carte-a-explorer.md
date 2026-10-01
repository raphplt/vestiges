# Plan 22 — La carte à explorer

30 septembre 2026 · **Direction validée par Raphaël ([DECISIONS §35](DECISIONS.md)) : feu vert, avec les arbitrages du §0.** Ouvert à sa demande ([DECISIONS §34](DECISIONS.md)). Conçu avec les [douze principes](../PRINCIPES-BUILD.md) et le [plan 21](21-systeme-de-jeu.md). Aucun code modifié par ce document.

**Ce plan regroupe au lieu d'ajouter.** Il absorbe le plan 13 (butin), l'idée B du plan 11 (butin à sauver de l'Effacement) et la remise en service des POI prévue au plan 18. Ces documents restent comme historique ; les décisions à prendre sont ici.

## 0. Arbitrages du 30 septembre

- **Feu vert** sur la direction : les décors deviennent des lieux.
- **Carte agrandie, surtout en hauteur.** Elle mesure aujourd'hui environ 25 600 × 6 400 px, soit quatre fois plus large que haute. Cible de départ : doubler la hauteur (12 800 px), largeur inchangée. À mesurer : coût de génération, nombre de décors, temps de traversée.
- **Un peu plus de lieux** que proposé : viser **85 à 90** au lieu de 70. Avec la carte agrandie, l'espacement moyen reste voisin de celui d'aujourd'hui.
- **Le Mémorial reste la pièce centrale** des lieux de choix : un autel à raviver, qui protège sa zone de l'Effacement et donne un bonus. Il est déjà en jeu (plan 17, vague 3).
- **Minimap : oui**, au lieu d'un simple écran de carte en dernier lot.
- Les ex-perks s'appellent les **Réminiscences** ; le système de build est fixé dans la [référence unique](21-systeme-de-jeu.md).

## 1. La demande et le constat

Raphaël veut une carte qui donne envie d'explorer et de ratisser, avec plus de lieux et de récompenses qu'aujourd'hui, mais **moins dense que Megabonk** pour garder l'immersion, et des lieux intégrés au lore. Les objets sont limités à 6 : l'intérêt d'explorer sur la durée doit donc venir **d'autre chose que l'accumulation d'objets** (Essence, améliorations d'armes, ou un système nouveau).

État mesuré dans le code et les données :

| Élément | Aujourd'hui |
|---|---|
| Carte | Environ 25 600 × 6 400 px ; à 200 px/s, 128 s d'est en ouest |
| Lieux fixes | 31 : 23 coffres, 5 Mémoriaux, 3 Failles |
| Événements | 5 micro-événements, un toutes les 60 à 90 s |
| POI | 6 types, désactivés (formes V1) |
| Essence | Sources : éliminations, coffres, événements, quêtes. **Un seul usage : les quatre services du Mémorial** |
| Décors reconnaissables | Fermes (puits, épouvantail, tracteur, pique-nique, corde à linge), carrières (wagonnets, veine de cristal, baraque), bâtiments urbains : **tous décoratifs** |
| Carte du joueur | Aucune minimap ni écran de carte ; pointeur vers 3 coffres au plus |

Deux manques : il y a peu de raisons de faire un détour, et l'Essence ne mène presque nulle part.

## 2. Principes du plan

1. **Le monde montre déjà ses lieux.** Un lieu est un décor du monde d'avant qui se souvient encore : un puits, une boîte aux lettres, un wagonnet. Pas d'icône flottante partout, pas de sanctuaire abstrait.
2. **Trois tailles, trois rythmes.** Des petits lieux fréquents (un geste, une petite récompense), des lieux de choix plus rares (une décision de build), des épreuves (un combat, une grosse récompense).
3. **Moins dense que Megabonk.** Cible de départ : un petit lieu en vue toutes les 20 à 30 s de marche, un lieu de choix toutes les 2 à 3 min. À mesurer et à baisser si la carte paraît encombrée.
4. **L'Effacement choisit ce qu'on perd.** Un lieu est plus riche près du front, et perdu quand sa zone s'efface. On ne peut pas tout prendre.
5. **Explorer nourrit les armes, pas un inventaire.** Voir §5.

## 3. Petits lieux : donner un rôle aux décors

Un geste court (maintien de 0,5 à 1 s), une récompense immédiate, aucune interface. Chaque lieu n'agit qu'une fois. Non visité, il porte un signe discret à moins de 400 px environ (une lueur, quelques particules d'Essence).

| Lieu | Décor | Ce qu'il donne | Lien au lore |
|---|---|---|---|
| Boîte aux lettres | Urbain, fermes | XP (une lettre jamais lue) | L'obsession du Facteur |
| Puits | Fermes (déjà généré) | Soin de 15 % | L'eau se souvient de la soif |
| Veine de cristal | Carrières (déjà générée) | Essence, à briser | Les « veines cristallines » de la Stratégie V2 §12 |
| Wagonnet | Carrières (déjà généré) | Essence, ou une arme au sol | Ce qu'on n'a pas eu le temps de remonter |
| Épouvantail | Fermes (déjà généré) | Petite embuscade, puis Essence | Il garde encore le champ |
| Voiture abandonnée | Routes | Fouille : Essence ou soin | Un départ interrompu |
| Cabine téléphonique | Urbain | Révèle les lieux proches (pointeurs de bord d'écran) | Une voix qui donne encore une adresse |
| Abribus | Routes | 20 s de vitesse | On y attendait ; on repart |
| Table de pique-nique | Fermes (déjà générée) | Soin léger et une ligne de lore | Un repas jamais fini |

Six sont des décors déjà générés ou faciles à placer sur les routes existantes. Cible : environ 25 par carte.

## 4. Lieux de choix et épreuves

| Lieu | Statut | Rôle |
|---|---|---|
| Coffre | Existe (23) | Essence, XP, arme. Les anciens Dons en sortent (plan 21) |
| Mémorial | Existe (5) | **La mémoire** : bénédiction, soin, lever un Oubli |
| Faille | Existe (3, plus celles des zones effacées) | Amélioration forte contre un Oubli et du Péril |
| **Reliquaire** | Nouveau (6 à 8) | **Les objets** : trois alcôves, une prise, deux effacées. Une fois les 6 objets pris, il propose des niveaux d'objets ou un remplacement (plan 21 §18) |
| **Atelier** | Nouveau (4) | **Les armes** : voir §5 |
| Épreuves | Existent (5 micro-événements, Souverains) | Combat, puis coffre, Essence, parfois une Réminiscence (nom provisoire des ex-perks) |

Total visé : environ 70 lieux au lieu de 31, dont 25 petits. La carte fait 164 millions de px² : cela laisse environ 1 500 px entre deux lieux en moyenne, soit 7 à 8 s de marche. Un écran en montre rarement plus de deux.

## 5. Ce qui garde l'exploration intéressante jusqu'au bout

Trois moteurs, tous tournés vers les armes et l'Essence plutôt que vers les objets.

### A. L'Atelier : l'Essence devient de la puissance d'arme

Un établi ou une forge abandonnée, dans un garage, une grange, une baraque de carrière. C'est le seul lieu qui travaille les armes :

- **Niveau d'arme** contre de l'Essence : le service quitte le Mémorial pour l'Atelier.
- **Retrempe** : relancer les stats de la dernière amélioration d'une arme, à rareté égale ou supérieure.
- **Trempe**, gratuite à la première visite : les 5 prochaines améliorations d'armes touchent **une stat de plus**. C'est l'idée du marteau de Megabonk, mais portée par un lieu et limitée dans le temps, pas par un objet permanent.

Les armes ont 196 niveaux à gagner sur une run. L'Essence et la Trempe restent donc utiles jusqu'à la fin, là où les objets plafonnent à 6. Explorer maintenant améliore les niveaux à venir (principe 6).

### B. Les Repères : la variété améliore tout le reste

Chaque **type** de lieu reconnu pour la première fois dans la run donne un Repère : un peu de Chance, donc de meilleures raretés sur toutes les améliorations. Douze types, douze Repères. Ce qui est récompensé, c'est de varier, pas d'enchaîner le même lieu.

### C. L'Atlas : ce qu'on découvre reste, d'une run à l'autre

Un onglet de la Collection. Chaque type de lieu, puis chaque lieu remarquable, y inscrit une ligne de lore la première fois. Compléter une page débloque une arme ou un objet : c'est une des voies de déblocage de la référence (plan 21 §10) ([DECISIONS §29](DECISIONS.md)). Une partie ratée laisse quand même une page avancée (principe 12).

## 6. Lien avec l'Effacement

- **Plus riche près du front.** La quantité et la rareté d'un lieu montent avec l'oubli de sa zone, comme pour les améliorations aujourd'hui.
- **Perdu avec sa zone.** Un lieu englouti disparaît. Il laisse une **trace** pâle, visible un moment : le joueur sait ce qu'il a laissé oublier (principe 12).
- **Stabiliser.** Ouvrir un coffre stabilise déjà sa zone ; les lieux de choix font de même. Visiter, c'est ralentir un peu l'oubli là où l'on passe.

## 7. Lisibilité sans casser l'immersion

- Les lieux sont des décors. Leur signe d'activité ne se voit que de près.
- De loin, seuls les lieux de choix ont un repère : la colonne de lumière des coffres, à la couleur de leur famille.
- Pointeurs de bord d'écran limités à 3, comme aujourd'hui ; la Cabine téléphonique en ajoute pour un temps.
- **Écran de carte** à la demande, montrant les lieux découverts et l'avancée de l'Effacement : proposé en dernier lot, à juger après essai.

## 8. Vérification par les principes

| Principe | Où il est tenu |
|---|---|
| 1. Fonction claire | Mémorial = mémoire, Atelier = armes, Reliquaire = objets, petits lieux = récolte |
| 2 et 4. Valeur selon le build, renforcement | La Trempe vaut plus pour une arme à nombreuses stats ; l'Essence vaut plus avec un Porte-monnaie |
| 5. Régime | Atelier et Reliquaire changent la suite de la run |
| 6. Immédiat contre futur | Détour vers un Atelier contre fuite ; Trempe pour plus tard contre soin maintenant |
| 7. Hasard orienté | Placement aléatoire, types reconnaissables, Cabine qui révèle |
| 8. Contraintes | L'Effacement : on ne peut pas tout visiter |
| 9. Conséquences visibles | Récompense immédiate, stat en plus visible sur les cartes d'amélioration |
| 10. Croissance sous tension | Lieux riches près du front |
| 11. Plusieurs chemins | Builds d'Essence, de Chance (Repères), d'armes (Trempe), de survie (puits, Mémorial) |
| 12. Hypothèses | Traces des lieux perdus, pages de l'Atlas |

## 9. Lots

| Lot | Contenu | Vérification |
|---|---|---|
| C0 | **Mesure de départ** : lieux rencontrés et visités par minute, Essence gagnée et dépensée, avec `tools/measure_run.sh` étendu | **Livré le 30 septembre** ([§12](#12-compte-rendu-c0--mesure-de-départ-30-septembre)) |
| C1 | Trois petits lieux sur des décors déjà générés (Puits, Veine de cristal, Épouvantail) ; socle commun des petits lieux | **Livré le 30 septembre** ([§14](#14-compte-rendu-c1--trois-petits-lieux-30-septembre)) |
| C2 | Atelier : niveau d'arme, Retrempe, Trempe | Banc, captures, mesure de l'Essence dépensée |
| C3 | Reliquaire, avec les objets du plan 21 (lot G2) | Dépend du catalogue d'objets |
| C4 | Les six autres petits lieux, Repères | **Six lieux livrés le 1er octobre** (plan 23 R7, [§15](#15-compte-rendu-c4-et-c6--six-petits-lieux-carte-agrandie-et-minimap-1er-octobre)) ; **Repères livrés le 1er octobre** (plan 23 R9) |
| C5 | Traces des lieux effacés, Atlas | Captures, essai de Raphaël |
| C6 | Carte agrandie en hauteur ; **minimap** (lieux découverts, front de l'Effacement) | **Livré le 1er octobre** (plan 23 R7, [§15](#15-compte-rendu-c4-et-c6--six-petits-lieux-carte-agrandie-et-minimap-1er-octobre)) |

## 10. Idées reprises d'autres plans, à placer

Pour qu'elles ne se perdent pas ([tableau de bord §4](TABLEAU-DE-BORD.md)) :

- **Vestige figé** (plan 13 §4 A) : un objet flottant à ramasser sans menu. Peut devenir la forme d'un niveau d'objet trouvé.
- **Pacte d'oubli** (plan 13 §4 C) : oublier un objet contre un meilleur. Peut devenir le « remplacement » du Reliquaire.
- **Source de soin** (plan 13 §8) : couverte par le Puits, la Table de pique-nique et le Mémorial.
- **Marchand ambulant, escorte, événements de biome** (plan 12 §6) : épreuves à ajouter après C4.
- **Fragment de lore près des scènes-récits** (plan 08) : c'est la Table de pique-nique et l'Atlas.
- **Établi de grand-père** (plan 21 §16) : remplacé par la Trempe de l'Atelier, si Raphaël valide.

## 11. Questions pour Raphaël

1. Donner un rôle aux décors existants plutôt que créer des sanctuaires : est-ce la bonne direction pour l'immersion ?
2. L'Atelier et la Trempe (une stat de plus sur les 5 prochaines améliorations) comme moteur d'exploration tourné vers les armes : ça te convient ?
3. Déplacer le service « niveau d'arme » du Mémorial vers l'Atelier ?
4. Environ 70 lieux au lieu de 31 : la bonne échelle, ou plus prudent pour commencer ?
5. Les Repères et l'Atlas : on les garde, ou on simplifie ?

## 12. Compte rendu C0 : mesure de départ (30 septembre)

**Outil.** `tools/measure_run.sh` donne maintenant, en plus de la densité :
- les lieux croisés, c'est-à-dire entrés dans le cadre (coffres, Mémoriaux, Failles) ;
- les lieux visités, c'est-à-dire un lieu encore utile atteint à portée d'interaction ;
- les micro-événements lancés ;
- l'Essence gagnée et dépensée. Un achat raté puis remboursé dans la même frame ne compte pas.

L'option `--visit` (`MEASURE_EXTRA_ARGS="--nomad --visit"`) fait jouer un bot qui ratisse. Il se détourne vers le lieu vu le plus proche (700 px au plus), ouvre les coffres, ravive les Mémoriaux et achète un service s'il recroise un Mémorial éveillé. Il n'ouvre pas les Failles, qui changeraient le Péril de la mesure. Code : `tools/tests/RunObservation.Places.cs`.

**Mesure.** 5 seeds × 10 min de jeu, bot nomade, Péril 0, au commit du lot.

| Par run de 10 min (moyenne, min–max) | Nomade qui ne se détourne pas | Nomade qui ratisse (`--visit`) |
|---|---|---|
| Lieux croisés | 14,6 (13–17), soit 1,5 par minute | 14,0 (12–19), soit 1,4 par minute |
| Lieux visités | 3,4 (1–6) | 13,8 (11–19) |
| dont coffres croisés / visités | 12,0 / 3,0 | 10,6 / 10,4 |
| dont Mémoriaux croisés / visités | 1,4 / 0,2 | 2,8 / 2,8 |
| dont Failles croisées / visitées | 1,2 / 0,2 | 0,6 / 0,6 |
| Premier lieu en vue | 33 s (0–137) | 24 s (0–89) |
| Micro-événements | 4,0 (0,4 par minute) | 3,8 |
| Essence gagnée | 1 190 (800–1 560), soit 119 par minute | 1 544 (1 355–1 764), soit 154 par minute |
| Essence dépensée | 0 | 0 |

**Ce que ça dit, face aux cibles du §2 :**
- Un lieu en vue toutes les **41 à 43 s**, contre une cible de 20 à 30 s pour les seuls petits lieux. Il faut à peu près doubler la densité de lieux croisés.
- Un Mémorial toutes les **3,5 à 7 min**, contre une cible d'un lieu de choix toutes les 2 à 3 min.
- Ratisser rapporte : +30 % d'Essence (surtout par les éliminations autour des lieux) et trois fois plus de coffres ouverts.
- **Aucune Essence dépensée**, même par le bot qui ratisse : il ravive les Mémoriaux mais ne revient pas une fois qu'ils sont éveillés. Un joueur nomade n'a presque jamais l'occasion de dépenser. Cela confirme le constat du §1 : l'Essence ne mène presque nulle part. C'est la mesure de référence pour l'Atelier (C2).

**Limites :** le bot suit un cap et ne revient jamais en arrière, et son écran a le cadrage de la capture (967 × 544 px de monde). Un joueur qui explore reviendrait parfois sur ses pas.

## 13. Lot C1 — trois petits lieux sur des décors déjà générés : découpage (30 septembre)

Principes du §2 et du §3 : un geste court, une récompense immédiate, un seul usage, un signe discret à moins de 400 px, perdu quand sa zone s'efface.

- **Socle commun :** `data/world/small_places.json` décrit chaque type (décors qui le portent, maintien, récompense, nombre par carte). Après la génération, un directeur parcourt une fois les décors posés et en retient une partie :
  - au plus N par type ;
  - au moins 600 px entre deux lieux, chaque type posant à son tour pour qu'un type rare ne soit pas évincé ;
  - tirés avec la graine de la carte.

  Sur chacun, il pose un lieu activable (`SmallPlace`), sans nouveau sprite : c'est le décor lui-même qui se souvient.
- **Signe :** une lueur au sol, légère (halo `InteractableAura`), visible seulement quand le joueur passe à moins de 400 px et tant que le lieu n'a pas servi. Le directeur la met à jour quatre fois par seconde, sans boucle par frame sur les décors.
- **Perte :** un lieu dont la zone passe au Néant s'éteint pour de bon.
- **Les trois lieux** (valeurs de départ) :

| Lieu | Décor | Maintien | Récompense | Par carte |
|---|---|---|---|---|
| Puits | `prop_abandoned_well` (fermes, champs) | 0,6 s | Soin de 15 % des PV max | 6 au plus |
| Veine de cristal | `prop_crystal_vein` (carrières) | 1 s, « briser » | 6 à 10 Essence, qui volent vers le compteur | 8 au plus |
| Épouvantail | `prop_scarecrow_broken`, `prop_scene_crow_scarecrow` | 0,5 s | Trois créatures du biome sortent autour ; si elles tombent toutes en 20 s, 15 Essence. Fuir ne rapporte rien | 6 au plus |

- **Vérification :**
  - `tools/measure_run.sh` avant et après, avec les mêmes seeds que C0. Les nouveaux lieux entrent dans les lieux croisés et visités ; le bot qui ratisse les utilise.
  - Capture d'un lieu, avec son signe, avant et après usage.
  - Banc : placement (quota, écart, graine), récompenses, perte au Néant.

## 14. Compte rendu C1 : trois petits lieux (30 septembre)

Livré comme découpé au §13. Code : `SmallPlaceDirector`, `SmallPlace`, `SmallPlaceDataLoader` ; données : `data/world/small_places.json`.

- **Placement :** 13 à 17 lieux par carte selon la graine (6 puits, 4 à 8 veines, 2 à 3 épouvantails). Les décors reconnus sont limités par ce que la carte génère : la carte en porte 6 à 13 puits, 5 à 12 veines et 2 à 5 épouvantails.
- **Signe :** une lueur au sol de la couleur du lieu, plus large que le pied du décor, et deux étincelles qui montent de temps en temps. Le tout seulement à moins de 400 px, et éteint dès l'usage.
- **Embuscade :** trois créatures du biome, jamais posées dans l'eau. Elle ne paie que gagnée, en 20 s. Fuir ne rapporte rien.

**Mesure (C0 → C1)**, 5 seeds × 10 min, bot nomade. Même outil qu'au §12 ; le trajet du bot varie d'un passage à l'autre, les écarts de coffres et de Mémoriaux sont du bruit.

| Par run de 10 min | C0, sans se détourner | C1, sans se détourner | C0, qui ratisse | C1, qui ratisse |
|---|---|---|---|---|
| Lieux croisés | 14,6, soit 1 toutes les 41 s | **25,4, soit 1 toutes les 24 s** | 14,0 | 19,4 (15–24) |
| Lieux visités | 3,4 | 3,6 | 13,8 | 18,4 (13–22) |
| dont petits lieux croisés (puits, veines, épouvantails) | — | 7,2 | — | 6,4 |
| Essence gagnée par minute | 119 | 113 | 154 | **184** |
| Essence dépensée | 0 | 0 | 0 | 0 |

- La cible du §2, un petit lieu en vue toutes les 20 à 30 s, est atteinte en comptant tous les lieux : un toutes les 24 s. Les petits lieux seuls, eux, reviennent toutes les 80 s environ ; les six autres types (C4) doivent combler l'écart.
- Ratisser rapporte 20 % d'Essence de plus qu'en C0.
- L'Essence n'est toujours **jamais dépensée**. C'est le travail de l'Atelier (C2).

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_small_places.sh` (nouveau) : 5 assertions, zéro échec. Il couvre le quota, l'écart, la graine, le soin unique du puits, l'Essence de la veine et le signe éteint après usage.
- Objets, armes, déplacements : zéro échec. Smoke vert.
- Capture `--capture-places` (nouvelle), regardée :
  - « [E] Boire », « [E] Briser », « [E] Secouer » au-dessus du décor ;
  - la lueur sous la veine ;
  - après usage, le signe éteint, et les créatures de l'épouvantail sortent et attaquent.

Relecture par sous-agent. Corrigé :
- décors reconnus par leur texture, sans chaîne par décor ;
- les petits lieux ne comptent plus pour les quêtes d'exploration de POI ;
- créatures d'embuscade jamais dans l'eau ;
- l'embuscade ne paie que gagnée ;
- tirage du placement séparé de celui des récompenses.

**Reste :**
- la lueur au sol est un losange translucide lisse, qui tranche avec le pixel art : à reprendre avec la direction artistique ;
- sur l'herbe, la lueur de l'épouvantail se voit mal ;
- la veine brisée garde son sprite intact ;
- la perte au Néant n'est vérifiée que par lecture du code.

## 15. Compte rendu C4 et C6 : six petits lieux, carte agrandie et minimap (1er octobre)

Fait au plan 23, lot R7.

**C4, six petits lieux** sur le socle de C1 (`SmallPlaceDirector`, `data/world/small_places.json`), tous sur des décors déjà générés :

| Lieu | Décors reconnus | Effet | Action |
|---|---|---|---|
| Boîte aux lettres | `prop_mailbox` | la moitié de l'XP du niveau suivant | Lire le courrier |
| Wagonnet | wagonnets de la carrière | une chance sur quatre d'une arme au sol, que le joueur ne porte pas ; sinon 8 à 14 Essence | Vider |
| Voiture abandonnée | les huit voitures de la ville | 40 % de soin (12 % des PV max), sinon 6 à 11 Essence | Fouiller |
| Cabine téléphonique | `prop_phone_booth` | les lieux encore utiles à 2 400 px reçoivent une flèche de bord d'écran, 45 s | Décrocher |
| Abribus | `prop_bus_shelter` (**nouveau décor**, généré par `tools/sprites/props/urban.py`, posé le long des trottoirs) | vitesse +20 % pendant 20 s | Lire l'horaire |
| Table de pique-nique | `prop_scene_picnic` | soin de 8 % et une ligne de lore parmi quatre | S'asseoir |

**C6, carte agrandie en hauteur :** la carte devient une ellipse de 401 × 801 cellules, soit 25 600 × 12 800 px (`map_radius_y` dans `world_gen.json`). Tout ce qui supposait un disque passe par `WorldGenerator.EllipseDistance` ou par deux rayons (biomes, villes, champs, marais, chemins, décors, sites, son d'ambiance, shader du sol). Les comptes de lieux suivent la surface : coffres 23 → 39, Mémoriaux 5 → 9, Failles 3 → 6, plafonds des petits lieux relevés là où la carte porte assez de décors (de 55 à environ 110 lieux posés).

**Minimap** (`scripts/UI/Minimap.cs`), coin bas droit du HUD : un pixel par cellule d'Effacement, révélé à 12 cellules autour du chemin et teinté par sa phase ; le front de l'Effacement avance aussi sur les cellules déjà vues (`ZonePhaseChanged`). Lieux découverts en points de leur couleur (grisés une fois servis), coffres à la couleur de leur rareté, Mémoriaux, Failles, joueur. Repeinte quatre fois par seconde, sans boucle sur les décors. Sous l'Oubli des repères, les coffres disparaissent de la carte comme les flèches.

**Mesure**, 5 seeds × 10 min, mêmes seeds qu'au §14, bot nomade (`tools/measure_run.sh`) :

| Par run de 10 min | C1 | C4 seul (carte d'avant) | C4 + C6, comptes d'avant | **C4 + C6, comptes réglés** |
|---|---|---|---|---|
| Petits lieux croisés | 7,2, un toutes les 80 s | 16,6, un toutes les 38 s | 15,2, un toutes les 40 s | **24,0, un toutes les 26 s (21–32)** |
| Lieux croisés, tous types | 25,4 | 31,4 | 25,6 | 37,2 |
| Coffres croisés | — | 11,6 | 9,4 | 10,2 |
| Mémoriaux croisés | — | 2,0 | 0,6 | 1,2 |
| Failles croisées | — | 1,2 | 0,4 | 1,8 |
| Essence gagnée | 1 130 | 1 617 | 1 226 | 1 267 |

- La cible du §2, **un petit lieu en vue toutes les 20 à 30 s à lui seul**, est atteinte : 26 s en moyenne.
- Sans réglage, la carte deux fois plus grande diluait tout : un Mémorial croisé sur trois runs. Une première version des comptes doublés donnait un petit lieu toutes les 19 s et 15 coffres : coffres communs et lieux les plus fréquents ramenés en dessous.
- Nomade qui ratisse (`--visit`), comptes réglés : 35,2 lieux croisés, **32,8 visités** (18,4 en C1), 1 571 Essence, toujours aucune dépensée.
- Les colonnes C4 + C6 tournent avec la montée des PV de R5 (moins d'éliminations, d'où moins d'Essence).

**Coût :**
- Décors par carte : 10 694 → 21 505. Les décors restent hors de toute boucle par frame : index d'occlusion et tronçons comme avant.
- Génération du terrain (`--capture-map`, 40 graines) : 203 → 423 ms par carte. Chargement complet en rendu logiciel, machine chargée : 21,6 → 26,8 s ; chiffre indicatif seulement.
- Banc de combat dense A/B, `tools/bench_ab.sh`, 2 passes, même commit, seule la hauteur change : 8,1 → 7,2 FPS en 720p et 4,4 → 4,1 en 1080p (−7 à −11 %), nœuds créés par seconde négligeables des deux côtés (1 à 7). Rendu logiciel du conteneur, sans carte graphique : l'écart dit qu'il y a un coût, pas sa taille sur la machine de Raphaël. **À refaire avec `/bench` sur une vraie carte graphique** ; si l'écart tient, la piste est le tri en Y du conteneur de décors, qui parcourt deux fois plus d'enfants.

**Vérifications :**
- `dotnet build` : zéro avertissement. Smoke vert.
- Bancs à `RESULT failures=0` : petits lieux (six contrôles C4 nouveaux, quota lu dans les données), déplacements, objets, capacités ennemies, armes, mode dev.
- Captures regardées : `--capture-map` (ellipse de biomes, vues dézoomées) ; run nomade à 100 s avec la minimap (chemin révélé, lieux, coffres, Faille, joueur).
- Relecture `godot-reviewer` : pas de bug bloquant, tous les restes de rayon unique corrigés. Corrigé ensuite : coffres masqués sous l'Oubli des repères sur la minimap, arme du Wagonnet tirée hors des armes portées et `LootReceived` émis au seul ramassage, petites allocations de la minimap, Boîte aux lettres signalée si la progression manque.

**Reste :**
- coût de la carte haute à mesurer sur une vraie carte graphique (voir Coût) ;
- la minimap montre les coffres jusqu'à 12 cellules (environ 1 500 px) autour du chemin, plus loin que les flèches (1 200 px) : à juger en jeu ;
- Repères (C4) : livrés au plan 23 R9, +1 % de Chance par type de lieu ;
- la carte reste centrée sur le départ : un joueur qui file droit vers le nord ou le sud a maintenant deux fois plus de chemin avant le bord.


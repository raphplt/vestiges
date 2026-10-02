# Plan 21 — Le système de jeu : référence unique

30 septembre 2026 · **Ce document fait foi.** Il fixe comment un build se construit dans Vestiges : armes, objets, Réminiscences, personnages, niveaux, hasard. Demande de Raphaël ([DECISIONS §35](DECISIONS.md)) : une seule version, détaillée, gardée à un seul endroit, puis appliquée.

> **Mise à jour du 30 septembre au soir ([DECISIONS §36 et §37](DECISIONS.md)) :**
> - objets à 30 niveaux et gains francs, dont la rareté renforce le gain ;
> - stats entières fractionnaires ;
> - projectiles en plus au lieu des copies ;
> - plus de bouclier de départ, invulnérabilité réduite ;
> - cartes de niveau à la Megabonk.
>
> Les sections ci-dessous sont à jour de ces décisions. Leur application est planifiée dans [23-plan-agent.md](23-plan-agent.md).

**Règles de tenue du document :**
- Toute décision qui touche au build se reporte ici avant d'être codée. Aucun autre plan ne redéfinit ce qui est écrit ici.
- La **structure** est fixée. Les **chiffres** sont des valeurs de départ, à régler par mesure ; ils se changent ici et dans `data/`.
- Conçu avec les [douze principes](../PRINCIPES-BUILD.md). Raisonnement et versions précédentes : [21-historique.md](21-historique.md).
- La carte, ses lieux et ce qu'ils donnent : [plan 22](22-carte-a-explorer.md).

## 1. Vue d'ensemble

| Famille | Question | Emplacements | Niveaux | D'où ça vient |
|---|---|---|---|---|
| **Armes** | Quoi : comment j'attaque | 4 | 1 à 50, puis ascension | Niveau ; au sol |
| **Objets** | Combien et quand : l'efficacité et ce qui se déclenche | 6 | 1 à 30, palier au niveau 15 | Niveau ; Reliquaire |
| **Réminiscences** | Comment : une règle réécrite | 4 | Aucun | Après chaque Résurgence survécue |
| **Personnage** | Pourquoi : l'orientation | 1 | — | Choix au Hub |

Trois canaux :
- le **niveau** récompense le combat ;
- le **monde** récompense l'exploration ;
- les **Résurgences** récompensent la survie.

Il n'existe pas d'autre famille : ni traits, ni passifs, ni Dons de coffre.

## 2. La montée de niveau

- **Courbe :** 300 à 400 niveaux dans une excellente run de 45 min. Coût par niveau en données (`data/scaling/progression.json`), plafonné à 3 000 XP.
- **Cascades :** en milieu et fin de partie, plusieurs niveaux se gagnent d'un coup. La réserve automatique retient l'écran quelques secondes quand les niveaux affluent ou que la foule est dense, puis enchaîne tous les choix dans un écran qui reste ouvert.
- **Un niveau = un choix de trois cartes.** Quatre types de cartes :

| Carte | Quand elle peut apparaître | Effet |
|---|---|---|
| Nouvelle arme | Un emplacement d'arme est libre | Ajoute l'arme au niveau 1 |
| Amélioration d'arme | L'arme n'est pas au niveau 50 | +1 niveau, des stats tirées au hasard (§3) |
| Nouvel objet | Un emplacement d'objet est libre | Ajoute l'objet au niveau 1 |
| Amélioration d'objet | L'objet n'est pas au niveau 30 | +1 niveau, dont le gain dépend de la rareté (§4) |

- **Composition :** si c'est possible, au moins une nouveauté et au moins une amélioration. Tant que le joueur n'a aucun objet de survie, une des trois cartes en propose un.
- **Rareté d'une amélioration :** commune, inhabituelle, rare, épique, légendaire, de poids 60 / 25 / 11 / 3,5 / 0,5. La **Chance**, l'**oubli de la zone** où se tient le joueur et le **Péril** ajoutent des crans de montée.
- **Gains francs :** une carte commune doit se sentir en jeu. Les totaux peuvent devenir très grands, façon Megabonk. La difficulté et la puissance des ennemis montent en conséquence (DECISIONS §37).
- **Relancer :** 3 par run.
- **Bannir, c'est oublier :** 3 gratuits par run, puis le n-ième payant coûte n tiers de point de Péril. Une carte bannie ne revient plus de la run.
- **Passer :** toujours possible.

## 3. Armes

- **4 emplacements.** L'arme de départ vient du personnage. Une arme trouvée au sol s'équipe si un emplacement est libre, sinon elle s'échange contre celle du premier emplacement.
- **24 armes**, toutes de même rang : il n'y a pas d'arme « rare ». La rareté porte sur les améliorations.
- **Niveau 1 à 50.** Chaque amélioration tire au hasard une ou plusieurs stats parmi celles que l'arme peut monter, pondérées par arme :

| Rareté | Stats touchées | Ampleur par stat | Stat entière (projectile, perforation, saut, orbe) |
|---|---|---|---|
| Commune | 1 | × 1 | +0,5 |
| Inhabituelle | 1 | × 1,5 | +0,75 |
| Rare | 2 | × 1,25 | +1 |
| Épique | 1 | × 1,75 | +2 |
| Légendaire | 3 | × 2 | +3 |

- **Stats entières fractionnaires :** une stat entière peut être tirée dès la rareté commune, et monte par fractions. La partie entière s'applique toujours ; la partie décimale est la chance, à chaque attaque, d'en avoir une de plus. Exemple : 2,5 projectiles, c'est 2 projectiles et une chance sur deux d'en tirer un troisième.

- **Pas par stat**, à l'ampleur × 1 (valeurs de départ, relevées de moitié pour des gains francs) : dégâts +18 %, cadence +12 %, portée +9 %, taille d'arc ou de cône +12 %, recul +20 %, vitesse de projectile ou d'orbite +15 %.
- **La carte montre la valeur avant → après** (§11).
- **Ascension au niveau 50 :** la carte suivante de cette arme propose **deux voies**, au choix et pour de bon. Une voie transforme l'arme et ouvre un changement de régime. Exemples :

| Arme | Voie A | Voie B |
|---|---|---|
| Arc du gymnase | **Volée** : éventail, le Nombre compte double | **Transpercer** : une seule flèche, qui traverse tout |
| Faucille | **Moisson** : cercle complet, plus lent | **Estoc** : frappe droite deux fois plus longue |
| Cloche d'école | **Glas** : ralentit deux fois plus longtemps, sans recul | **Tocsin** : repousse fort et désoriente |
| Boîte à musique | **Ronde** : les orbes s'éloignent et reviennent | **Berceuse** : orbes proches, qui endorment un instant |

Les deux voies de chacune des 24 armes sont en jeu (G3, étapes 1 et 2 ; DECISIONS §40). Celles des 20 autres armes sont celles du [21-historique.md §28](21-historique.md#28-lot-g3--ascensions-darmes--découpage-et-proposition-pour-20-armes-30-septembre), aux leviers détaillés au [plan 24 §11, L12](24-retours-du-1er-octobre.md).

## 4. Objets

- **6 emplacements.** Un objet ne se cumule pas en exemplaires : il **monte de niveau**, de 1 à **30**.
- **Un niveau = un gain franc.** Chaque carte donne un niveau. Son gain est le pas de l'objet multiplié par la rareté : commune × 1, inhabituelle × 1,5, rare × 2, épique × 2,5, légendaire × 3. La valeur d'un objet est la somme des gains de ses cartes.
- **Deux sortes**, dans les mêmes emplacements :
  - objets de **propriété** : montent une propriété commune (§7) ;
  - objets de **déclencheur** : font quelque chose à un moment précis (impact, élimination, critique, blessure, dash, niveau gagné, zone oubliée).
- **Obtenir un objet neuf :**
  - par une carte « nouvel objet » au niveau, tant qu'un emplacement est libre ;
  - par un **Reliquaire** (plan 22) : trois alcôves, une prise, les deux autres s'effacent. Certains objets ne se trouvent que là (« monde »).
- **Monter un objet :**
  - par une carte d'amélioration au niveau : un niveau, gain selon la rareté ;
  - par le monde : Reliquaire une fois les 6 emplacements pris, coffres.
- **Palier au niveau 15 :** chaque objet y gagne un effet propre. C'est là qu'un objet change la manière de jouer. Les effets de palier écrits ci-dessous, auparavant au niveau 25, passent au niveau 15.
- **Remplacer :** un Reliquaire peut proposer de remplacer un objet. Le nouveau démarre à la moitié du niveau de l'ancien.
- **Accès :** D = disponible dès le départ ; Q = débloqué par une quête ; V = acheté en Vestiges ; M = monde seulement (Reliquaire).

### Objets de propriété (16)

Pas = gain d'une carte commune. Au niveau 30 en cartes communes seules, l'objet vaut 30 pas. Valeurs de départ, à régler par mesure.

| Objet | ID | Pas (carte commune) | Palier 15 | Accès |
|---|---|---|---|---|
| Ressort de sommier | `memoire_vive` | Fréquence +8 % | Chaque 10ᵉ attaque d'une arme part deux fois | D |
| Papier carbone | `souffle_du_neant` | **+0,5 projectile** à toutes les armes à projectiles ou à frappes, à pleins dégâts | Les projectiles en plus visent chacun une cible différente | Q |
| Reflet brisé | `reflet_brise` | +0,5 perforation aux tirs | Un projectile qui perfore gagne +10 % de dégâts par ennemi traversé | Q |
| Rondelle de cuivre | `resonance` | Taille +8 % | Les zones frappent une seconde fois, à 30 % | D |
| Mètre pliant | `portee_etendue` | Portée +6 % | Un projectile en bout de course éclate en petite zone | D |
| Pince à linge | `persistance` | Durée +8 % (statuts, zones au sol) | Un statut qui expire a 25 % de chance de se renouveler | D |
| Lunettes de lecture | `oeil_critique` | Critique +2 %, dégâts critiques +10 % | Un critique sur une cible à PV pleins compte double | Q |
| Bouton de manteau | `ancrage` | PV max +15 | Les coups inférieurs à 3 % des PV max sont ignorés | D |
| Bobine de fil | `regeneration` | +0,4 PV/s | La régénération double pendant 3 s après une blessure | D |
| Genouillère | `peau_dure` | Armure +2 | L'armure compte double pendant le dash et 1 s après | V |
| Écusson de pompier | `carapace` | Bouclier +5 | Quand le bouclier casse, une onde repousse les ennemis proches | V |
| Lacet rouge | `instinct` | Vitesse +3 %, recharge du dash +5 % | Le dash va 30 % plus loin | D |
| Aimant de frigo | `siphon_essence` | Aimant +15 % | Chaque orbe ramassée rend 0,2 PV | D |
| Photo de classe | `photo_de_classe` | XP +5 % | Chaque niveau gagné donne 3 Essence | D |
| Jeton de fête foraine | `jeton_de_fete` | Chance +3 % | +1 relance tous les 15 niveaux du joueur | D |
| Paille tordue | `paille_tordue` | Vol de vie +0,5 % des dégâts (soin plafonné à 5 % des PV max par seconde) | Sous la moitié des PV, le plafond double | D |

Les **copies d'attaque** (projectiles à dégâts réduits) sont supprimées (DECISIONS §37). Les projectiles en plus du Papier carbone sont pleins. Ils suivent la règle des stats fractionnaires (§3) : 2,5 projectiles en plus, c'est 2 projectiles et une chance sur deux d'un troisième, à chaque attaque. Une arme de mêlée les reçoit en frappes, en éventail. Sans le palier, les projectiles en plus partent en éventail vers la cible de l'arme ; au palier, chacun vise sa propre cible.

**Voies d'ascension et projectiles en plus :** Volée double le total (projectiles de l'arme et projectiles en plus) ; Transpercer tire une seule flèche, et les projectiles en plus ne s'y appliquent pas.

Un palier se décrit en données avec l'objet (`milestones` : niveau, effet, texte, paramètres). Il n'est annoncé que si son effet est codé.

Il n'existe **pas** d'objet « +dégâts » universel : les dégâts viennent des niveaux d'armes.

Les ID des anciens passifs sont conservés pour ceux qui migrent. `flamme_interieure` n'est plus proposé (désactivé, ID gardé) ; `reflet_brise` est réactivé en perforation fractionnaire (plan 23, R3).

### Objets de déclencheur (19)

Pas écrits pour 30 niveaux (plan 23, R3) : au niveau 30 en cartes communes seules, chaque objet vaut au moins ce qu'il valait au niveau 50 de l'ancienne formule, et le premier niveau reste perceptible. Il n'y a plus de valeur de départ : le premier niveau vaut un pas. Seules les durées de base d'un effet (traînée du Chewing-gum, invulnérabilité du Médaillon) sont des réglages fixes de l'objet, auxquels le pas s'ajoute.

| Objet | Déclencheur | Pas (carte commune) | Niv 30 en communes (ancien niv 50) | Palier 15 | Accès |
|---|---|---|---|---|---|
| Allumette humide | Impact | Chance d'enflammer (Brûlure) +3 % | 90 % (26 %) | La Brûlure passe au voisin quand la cible meurt | D |
| Glaçon dans un mouchoir | Impact | Chance de ralentir +3 % | 90 % (26 %) | Un ennemi ralenti deux fois est figé 0,5 s | D |
| Pétard mouillé | Élimination | La victime explose : +10 % des dégâts du coup | 300 % (100 %) | L'explosion se produit deux fois | Q |
| Dé à coudre | Élimination | +0,1 PV | 3 PV (1,1) | Une élite tuée rend 5 % des PV max | D |
| Loupe de philatéliste | Critique | La cible devient Fragile 2 s : +5 % de dégâts subis | 150 % (30 %) | Fragile dure 4 s | Q |
| Stylo à quatre couleurs | Critique | L'attaque repart sur une autre cible : +6 % de ses dégâts | 180 % (80 %) | Elle repart sur deux cibles | Q |
| Semelle usée | Déplacement continu de 2 s | La prochaine attaque : +6 % | 180 % (65 %) | Vaut pour les deux prochaines attaques | D |
| Tabouret de camping | Immobile depuis 1 s | Fréquence +3 % | 90 % (50 %) | Et armure +10 | Q |
| Chewing-gum | Dash | Traînée qui ralentit : 1 s, +0,1 s | 4 s (3 s) | La traînée rend aussi Fragile | D |
| Gilet réfléchissant | Ennemis à moins de 120 px | Dégâts +0,2 % par ennemi, 10 au plus | 6 % par ennemi, 60 % au plus (3 %, 30 %) | 20 ennemis au plus | Q |
| Thermos | PV au-dessus de 90 % | Dégâts +2 % | 60 % (30 %) | Seuil abaissé à 75 % | V |
| Médaille cabossée | PV sous 35 % | Dégâts +3 % | 90 % (38 %) | Et vitesse +15 % | Q |
| Boîte de pansements | Niveau gagné | Soin +0,3 % des PV max | 9 % (4 %) | Une cascade de 3 niveaux ou plus donne 1 s d'invulnérabilité | V |
| Thermomètre | Cible brûlée | Dégâts contre elle +4 % | 120 % (50 %) | Tes Brûlures ralentissent aussi de 15 % | D |
| Épingle à nourrice | Cible ralentie | Dégâts contre elle +4 % | 120 % (50 %) | Un ennemi ralenti tué prolonge de 1 s le ralentissement de ses voisins | D |
| Porte-monnaie usé | Essence gardée | +1 % de dégâts par tranche de 10 Essence ; plafond +3 % | plafond 90 % (60 %) | 20 % de l'Essence dépensée est rendue | Q |
| Presse-papier en verre | En zone oubliée | Dégâts +2 % | 60 % (30 %) | Les malus de vitesse des zones oubliées ne s'appliquent plus | M |
| Calendrier arraché | Permanent | L'Effacement avance 10 % plus vite ; XP et Essence +2 % | 60 % (31 %) | Chaque Résurgence survécue laisse un coffre de plus | M |
| Médaillon ouvrant | Coup fatal | Une fois par run : reste à 1 PV, invulnérable 1 s, +0,1 s | 4 s (3 s) | Se recharge à chaque Résurgence survécue | M |

Une chance au-delà de 100 % part toujours, et l'excédent renforce l'effet (§7).

Deux objets d'exploration (Carte routière, Pied-de-biche) sont décrits au plan 22, car ils dépendent des lieux.

## 5. Réminiscences

- **4 emplacements.** Une Réminiscence réécrit une règle. Elle n'a ni niveau ni rareté, et ne se remplace pas.
- **Après chaque Résurgence survécue**, choix parmi trois. La première offre contient, si possible, une Réminiscence de survie, une de combat et une de collecte ou de récompense.
- Un **Souverain** vaincu en offre une avec 25 % de chance, si un emplacement est libre.
- Une Réminiscence passée, ou sans candidate, revient au niveau gagné suivant.
- Une Réminiscence n'est proposée que si l'arsenal lui donne prise. Sans arme compatible, elle est marquée « inactive » dans la pause, et l'échange d'arme au sol prévient.

| Réminiscence | Famille | Règle | État |
|---|---|---|---|
| Convergence | Combat | Les armes qui visent préfèrent élites et Souverains à portée | Livrée |
| Débordement | Combat | 50 % du surplus d'un coup fatal va au prochain impact de la même arme (3 s) | Livrée |
| Propagation | Combat | À sa mort, un ennemi passe son ralentissement et sa désorientation au voisin le plus proche (120 px) | Livrée |
| Prévoyance | Survie | Les soins en trop remplissent une réserve (20 % des PV max) qui rend les PV d'un coup non fatal | Livrée |
| Reprise | Survie | Après une blessure, 4 s pour récupérer 40 % de la perte en tuant (2 % des PV max par élimination) | Livrée |
| Sillage | Collecte | Le trajet des 6 dernières secondes attire l'XP | Livrée |
| Seconde lecture | Récompense | La plus rare des améliorations d'arme laissées revient au choix suivant | Livrée |
| Délestage | Récompense | Une carte d'objet peut être convertie en XP | À faire, avec les objets |
| Habitude | Récompense | Une carte « nouvel objet » peut être remplacée par un niveau d'un objet possédé | À faire, avec les objets |
| Contrecoup | Combat | Le dash déclenche les objets « à l'impact » autour du joueur | À faire |
| Braise | Combat | La Brûlure qui s'épuise saute au voisin | À faire |
| Mémoire vive | Combat | Chaque 10ᵉ impact déclenche deux fois les objets | À faire |
| Lisière | Survie | En zone oubliée, les statuts infligés durent deux fois plus | À faire |
| Poids mort | Combat | Les ennemis ralentis encaissent 25 % de plus | À faire |

## 6. Personnages

Chaque personnage apporte une signature, une arme de départ, une mobilité (plan 06) et **deux affinités** : les cartes qui portent ces propriétés sont deux fois plus probables dans ses offres. L'affinité oriente, elle n'interdit rien.

**Défense commune (DECISIONS §37) :** aucun personnage n'a de bouclier de départ ; le bouclier ne vient que des objets (Écusson de pompier). L'invulnérabilité après un coup passe de 0,5 s à 0,25 s (valeur de départ).

| Personnage | Signature | Affinités |
|---|---|---|
| Vagabond | Carnet de route : chaque biome traversé renforce | Élan, Taille |
| Traqueur | Empreinte : une marque qui se transmet | Précision, Portée |
| Forgeuse | Redresser : chaque 4ᵉ coup est une onde | Taille, Force |
| Éveillée | Double : la rémanence répète l'attaque suivante | Nombre, Durée |
| Facteur | Tournée : l'élan monte tant qu'il avance | Fréquence, Élan |
| Scaphandrière | Réserve d'air : tient dans l'Effacement | Durée, zone oubliée |

## 7. Grammaire commune

Toutes les pièces parlent la même langue. Une pièce nouvelle n'a jamais besoin d'une règle écrite pour une autre pièce.

**Propriétés**

| Propriété | Ce qu'elle change |
|---|---|
| Force | Dégâts de base d'une arme. Niveaux d'armes seulement |
| Fréquence | Attaques par seconde |
| Nombre | Projectiles, frappes, orbes, sauts de chaîne |
| Taille | Zone, arc, cône, rayon d'onde |
| Portée | Distance d'acquisition et de trajet |
| Durée | Statuts, zones au sol, orbites |
| Précision | Chance et puissance de critique |
| Élan | Vitesse et dash |

**Déclencheurs :** impact, élimination, critique, blessure subie, soin, dash, niveau gagné, entrée en zone oubliée, fin de Résurgence.

**Statuts :** Brûlure (dégâts dans le temps), Saignement (dégâts dans le temps, arme), Ralentissement, Désorientation, Fragilité (dégâts subis augmentés). Un statut a une règle de cumul unique : une nouvelle application rafraîchit la durée et garde la plus forte intensité.

**Garde-fous :**
- **Coefficient de déclenchement par arme :** un impact d'arme rapide ou multiple pèse moins pour les objets qu'un impact lent et lourd.
- **Pas de récursion :** un effet déclenché ne redéclenche pas les objets.
- **Une chance plafonne à 100 % ;** au-delà, l'excédent renforce l'effet.

## 8. Chance, Péril, Essence

- **Chance :** monte la rareté des améliorations et du butin. Elle n'agit ni sur les critiques ni sur les déclencheurs.
- **Péril** (0 à 10) : chaque point renforce les créatures et majore XP, score et rareté. Il vient des Failles et des bannissements payants.
- **Essence :** seule ressource de run. Elle se gagne en tuant et en explorant, et se dépense dans les lieux (plan 22). Elle ne sort pas de la run.
- **Vestiges :** monnaie entre les runs, pour les déblocages.

## 9. Fin de progression

Quand une carte ne peut plus rien offrir (4 armes ascensionnées, 6 objets au niveau 50), le niveau donne un **surplus** concret, au choix parmi trois : soin, Essence, relance, bannissement gratuit, score. Les règles détaillées sont au plan 20.

## 10. Déblocages

- **Réserve de départ limitée** pour les armes et les objets ; le reste se débloque.
- **Par quête :** la condition décrit un style à essayer (par exemple 300 éliminations sans bouger pour le Tabouret de camping).
- **Par achat en Vestiges**, au Hub.
- **Par l'Atlas** (plan 22) : ce qu'on découvre sur la carte.
- La Collection montre chaque pièce verrouillée avec sa condition. Le mode dev ouvre tout.

## 11. Lisibilité

- **Carte de niveau** (forme Megabonk, validée en DECISIONS §37) :
  - rareté en petit ; nom ; niveau à droite (« Niv 3 → 4 », ou « NOUVEAU ») ;
  - **une ligne de gain en valeur** (« Cadence 1,2 → 1,4 /s »), deux au plus ;
  - ni propriété nommée, ni « Pour : … », ni texte de palier. Un palier que la carte fait atteindre se signale par un badge doré.
- **Pendant le choix :** l'inventaire à gauche (armes, objets, Réminiscences, avec leurs niveaux) et les stats du joueur à droite.
- **Pause :** armes, objets avec leur niveau et leur effet du moment, Réminiscences avec leur état.
- **Combat :** chaque déclencheur d'objet et chaque Réminiscence a un retour visuel propre.
- **Bilan :** dégâts par source (armes, objets, Réminiscences) et la chaîne la plus rentable de la run.

## 12. État d'application

| Élément | État au 30 septembre | Lot |
|---|---|---|
| Armes : 4 emplacements, niveau 50, stats aléatoires à rareté | **En jeu** | — |
| Offre de level-up : une arme nouvelle garantie sous 3 armes, composition en données | **En jeu** | Plan 24, L3 |
| Relances limitées, bannissements gratuits puis Péril | **En jeu** | G1 |
| Réminiscences après les Résurgences, 7 sur 14, libellées « Réminiscence » | **En jeu** | G1, G2a |
| Objets : 6 emplacements, 30 niveaux, une carte = un niveau, gain du pas × 1 à × 3 selon la rareté, effets multiples | **En jeu** | Plan 23, R3 |
| 15 objets de propriété à pas francs, dont Papier carbone (projectiles en plus, pleins, fractionnaires) et Reflet brisé (perforation fractionnaire) | **En jeu** | Plan 23, R3 |
| Paliers de tous les objets au niveau 15, dont les deux nouveaux (Papier carbone, Reflet brisé) | **En jeu** | Plan 23, R3 |
| Stats entières fractionnaires (dès la commune, 0,5 à 3), pas d'armes relevés de moitié | **En jeu** | Plan 23, R4 |
| Montée des PV des créatures recalée sur cette puissance (×1,25 d'emblée, ×1,04 puis ×1,075 par minute après 6 min) | **En jeu** | Plan 23, R5 |
| Bouclier de départ retiré, invulnérabilité réduite (0,25 s, à confirmer en jeu) | **En jeu** | Plan 23, R1 |
| Cartes de niveau à la Megabonk, inventaire et stats à côté | **En jeu** | Plan 23, R2 |
| Statut Fragilité, coefficient de déclenchement par arme ; Allumette humide, Glaçon, Thermomètre, Épingle à nourrice | **En jeu** | G2b, étape 2 |
| Pétard mouillé, Dé à coudre, Semelle usée, Boîte de pansements | **En jeu** | G2b, étape 3 |
| Huit objets de déclencheur de G2c : Loupe, Stylo, Tabouret, Chewing-gum, Gilet, Thermos, Médaille, Porte-monnaie (31 objets proposés) | **En jeu** | Plan 23, R6 |
| Trois objets « monde » : Presse-papier, Calendrier, Médaillon | À faire | Avec le Reliquaire (plan 22, C3) |
| Reliquaire, objets « monde », remplacement | À faire | Plan 22, C3 |
| Propriétés nommées sur les cartes d'armes et d'objets, armes concernées par un objet | **Retirées des cartes** ; les armes concernées s'allument dans l'inventaire au focus d'une carte d'objet, la règle reste pour les affinités | G0, plan 23 R2 |
| Ascensions d'armes : mécanique et quatre armes (Arc, Faucille, Cloche, Boîte à musique) | **En jeu** | G3, étape 1 |
| Ascensions des 20 autres armes | **En jeu** (validées DECISIONS §40) | G3, étape 2, plan 24 L12 |
| Affinités des personnages, Souverain à 25 % | À faire | G4 |
| Sept Réminiscences restantes | À faire | G5 |
| Surplus, déblocages | À faire | Plans 20, 22 |
| Anciens Dons des coffres et leurs synergies | **Retirés** ; les coffres donnent des niveaux d'objet (un niveau = une carte commune) | G2b, étape 1, plan 23 R3 |
| Bonus d'une stat au hasard à chaque coffre, en plus du butin (un niveau d'objet commun, ×2 rare, ×3 épique) | **En jeu** | Plan 23, R8 |
| Repères : au premier usage de chaque type de lieu, un petit gain propre au lieu (stat, relance ou bannissement), douze types | **En jeu** | Plan 23 R9, plan 24 L9 |

## 13. Ce que ce document remplace

- [21-historique.md](21-historique.md) : raisonnement et comptes rendus des lots G1 et G2a.
- [05-perks-specialisations.md](05-perks-specialisations.md) : reste la fiche technique détaillée des sept effets livrés (cas limites, contrats). Son calendrier d'acquisition et ses lots B3–B4 sont périmés.
- [05-objets-catalogue-v1.md](05-objets-catalogue-v1.md), [05-catalogue-objets-perks.md](05-catalogue-objets-perks.md), [05-armes-objets-builds.md](05-armes-objets-builds.md) : périmés pour tout ce qui concerne les objets, les passifs et les perks.
- Plan 17, vague 4 et plafond d'armes ; plan 20, mentions d'un plafond à 70 : périmés.

## 14. Lots G6 — retours du 1er octobre soir (DECISIONS §48, §49)

Découpage du 2 octobre, après les réponses de Raphaël (§49). Un lot à la fois, un commit par lot.

**Constats de départ (données au 2 octobre) :** 6 armes sur 24 ont `projectile_count` dans leur croissance, au poids 0,5 sur un total d'environ 9,5 (≈ 5 % des tirages de stat) ; la mêlée ne reçoit des frappes en plus que du Papier carbone ; la Boîte à musique (`orbital_count`) et la Chaîne de noms (`chain_targets`) ont leur nombre au même poids 0,5. Les 31 objets proposés pèsent tous 1 dans les offres. Chance : Jeton de fête seul (+0,03 par pas) ; XP : Photo de classe seule (+5 %) ; pas de vol de vie.

| Lot | Contenu | Vérification |
|---|---|---|
| **G6a — Le nombre pour toutes les armes** | Les 24 armes peuvent monter leur nombre : `projectile_count` (tirs, ou frappes en éventail pour la mêlée, en plus de celles du Papier carbone), `orbital_count` pour la Boîte à musique, `chain_targets` pour la Chaîne de noms ; poids de tirage 0,5 → 1,5 (≈ 13 %). Les voies qui fixent le nombre (Transpercer, Bille d'acier, Agrafeuse, Balayage, Clé unique) gardent leur valeur. Poids d'offre par objet en données (`offer_weight`, 1 par défaut) ; Papier carbone à 2. Libellé « Frappes » au lieu de « Projectiles » sur les armes de mêlée. | Build, `tools/test_weapons.sh`, tirage simulé des stats, capture d'une arme de mêlée à 2 frappes |
| **G6b — Vol de vie** | Nouvel objet de propriété « Paille tordue » (`paille_tordue`) : +0,5 % des dégâts infligés rendus en PV par pas ; soin plafonné à 2 % des PV max par seconde ; palier 15 : le soin au-delà des PV max remplit le bouclier, jusqu'à 10. Stat `lifesteal` au tirage des bonus de coffre. Icône produite par le pipeline des objets (plan 25). | Build, smoke, contrôle chiffré du plafond, capture de la carte et de la fiche |
| **G6c — Chance et XP renforcés** | Pas relevés : Jeton de fête +0,03 → +0,05 de Chance, Photo de classe +5 % → +8 % d'XP, Aimant de frigo +15 % → +20 % ; bonus de coffre alignés (un niveau d'objet commun). | Build, chiffres des cartes |
| **G6d — Rythme des niveaux mesuré** | `tools/measure_run.sh` sur trois seeds : intervalle entre deux niveaux par tranche de 5 min, sans et avec Photo de classe forcée. Proposition chiffrée de courbe à Raphaël, **sans toucher** `progression.json` avant sa réponse. | Rapport au plan 20 |
| **G6e — Sprites d'armes : taille** | Les visuels d'attaque (arcs, projectiles, orbes) suivent la stat de taille et le nombre, avec un plafond de lisibilité en foule. | Captures avant/après |
| **G6f — Sprites d'armes : variantes par nombre** | Variantes visuelles quand le nombre de projectiles franchit des seuils (planche à valider par Raphaël avant intégration, plans 08/25). | Planche, captures |

Ensuite : C2, l'Atelier ([plan 22](22-carte-a-explorer.md), validé §49).

### G6a — livré le 2 octobre

- **Données :** les 24 armes ont leur nombre dans `growth`, au poids 1,25 (13 à 16 % des tirages de stat selon l'arme, contre environ 5 % pour 6 armes auparavant) : `projectile_count` pour les tirs, les frappes de mêlée et les ondes du Transistor, `orbital_count` pour la Boîte à musique, `chain_targets` pour la Chaîne de noms. Les armes de mêlée portent `"projectile_count": 1` explicite. `count_name_key` nomme ce nombre « Frappes » (11 armes de mêlée) ou « Ondes » (Transistor) sur les cartes et dans la pause.
- **Mêlée :** une frappe en plus part en éventail, à pleins dégâts, comme celles du Papier carbone, qui s'y ajoutent. Sur une onde circulaire (Fouet, Cloche), chaque frappe touche à nouveau tout le cercle.
- **Transistor :** le cône ignorait tout nombre. Chaque onde en plus (stat de l'arme ou Papier carbone) repasse à pleins dégâts : les dégâts de base du cône sont multipliés par le nombre d'ondes tiré à l'activation. Le visuel reste celui d'un cône ; les variantes viennent avec G6e–f.
- **Voies d'ascension :** celles qui fixent le nombre (Transpercer, Bille d'acier, Agrafeuse, Balayage, Clé unique) gardent leur valeur ; Volée et les autres multiplicateurs s'appliquent au nombre monté.
- **Offres :** `offer_weight` par objet (`passive_souvenirs.json`, 1 par défaut), appliqué aux cartes « nouvel objet » et « amélioration d'objet » ; Papier carbone à 2.
- **Vérification :** build sans avertissement, smoke vert, `tools/test_weapons.sh` 30/30, dont quatre contrôles nouveaux : toutes les armes ont leur nombre (part minimale 13 %), Lame ébréchée à 2 frappes (12 puis 24 dégâts, libellé « Frappes »), Transistor à 2 ondes (9,6 puis 19,2 de base), poids d'offre du Papier carbone. Pas de capture : le rendu des frappes multiples est le chemin existant du Papier carbone, inchangé.
- **Relecture (`godot-reviewer`) :** deux constats sur les armes ascendées écartés : une arme ne s'ascende qu'au niveau 50 et ne reçoit plus d'amélioration ensuite (`CanAscend` exige `!CanLevelUp`). **À surveiller en jeu :** sur une onde circulaire (Fouet, Cloche, et les voies Moisson, Séisme, Déblayer, Ratisser, Mie de pain), chaque frappe en plus retouche tout le cercle : c'est un multiplicateur de dégâts pur, comme le Papier carbone l'était déjà sur ces armes.

### G6b — livré le 2 octobre

- **Paille tordue** (`paille_tordue`, objet de propriété, de survie) : +0,5 % des dégâts infligés rendus en PV par pas. Le soin s'accumule à chaque coup et revient en un seul soin toutes les 0,25 s, plafonné à **5 % des PV max par seconde** (`data/characters/defense.json`) ; l'excédent est perdu. Palier 15 : sous la moitié des PV, plafond doublé.
- **Plafond relevé de 2 % à 5 % :** mesuré au banc, 2 % ne rendaient que 1,4 PV/s avec les 70 PV de départ, moins qu'un seul niveau de Bobine de fil (0,6 PV/s) après quelques cartes. À régler en jeu.
- **Coffres :** `lifesteal` (+0,5 %, ×2 rare, ×3 épique) entre au tirage du bonus de stat ; icône de butin : celle de la Paille. Fiche du joueur : ligne « Vol de vie » dès qu'il existe.
- **Relecture (`godot-reviewer`) :** rien à corriger (pas de soin après la mort ni pendant un écran de choix, aucune allocation). Choix assumé : seul le coup direct d'une arme nourrit le vol de vie, pas les dégâts de statut (saignement, brûlure) ni les échos différés.
- **Icône :** paille coudée rayée, modèle SDF `straw` (`tools/sprites/items/icons.py`), exportée par `generate_item_icons.py --ids paille_tordue --export`.
- **Vérification :** build sans avertissement, `tools/test_objects.sh` 72/72 (deux contrôles nouveaux : 20 dégâts rendent 0,1 PV, 5 000 dégâts plafonnés à 0,88 PV en 0,25 s, palier qui double à 1,75 PV ; catalogue et offre à 32 objets ; 14 stats de coffre), `tools/test_weapons.sh` 30/30. Capture du level-up (`--capture-levelup --levelup-weapon chipped_blade --levelup-new paille_tordue`, options ajoutées) inspectée : carte « Paille tordue · Vol de vie +0,5 % » et carte de Faucille « Frappes +3 ».

### G6c — livré le 2 octobre

- **Pas relevés** (une carte commune) : Jeton de fête +0,03 → **+0,05** de Chance, Photo de classe +5 % → **+8 %** d'XP, Aimant de frigo +15 % → **+20 %** d'aimant. Au niveau 10 en communes, la Photo donne +80 % d'XP au lieu de +50 %.
- **Alignés sur un niveau commun :** bonus de coffre (`chest_stat_bonus.json` : Chance 0,05, XP 0,08, aimant 0,2) et Repères de la Boîte aux lettres (XP 0,08) et du Wagonnet (aimant 0,2) ; ceux de Chance valaient déjà 0,05. Les bénédictions des Mémoriaux ne changent pas.
- **Vérification :** `tools/test_objects.sh` 72/72 (Photo niveau 10 : +80 % ; Jeton : +0,05), `tools/test_small_places.sh` vert.

### G6d — rythme des niveaux mesuré le 2 octobre (rien de changé)

`tools/measure_run.sh`, 3 seeds (221092026, 1002, 42) × 15 min de jeu, après G6a–c. Le bot prend la première carte ; avec `--prefer photo_de_classe,jeton_de_fete,siphon_essence` (option ajoutée à RunObservation), il prend d'abord ces objets quand ils sont offerts.

| Scénario | Niveau à 5 / 10 / 15 min | Un niveau toutes les… (1–5 / 5–10 / 10–15 min) | Orbes laissées au sol à 15 min |
|---|---|---|---|
| Bot sur place | 13–14 / 24–25 / 34–37 | 20–22 s / 27 s / 25–30 s | 430 à 860 |
| Bot sur place, build XP | 12–14 / 24–31 / 35–53 | 20–24 s / 16–27 s / 14–27 s | 40 à 550 |
| Bot nomade | 11–14 / 20–30 / 28–45 | 18–24 s / 19–33 s / 20–38 s | 640 à 1 100 |
| Bot nomade, build XP | 12–16 / 23–31 / 31–58 | 16–22 s / 18–27 s / 11–38 s | 30 à 950 |

**Lecture :**
- Les bots montent d'un niveau toutes les 20 à 35 s, sans accélération en fin de quart d'heure. Ils sont loin de la cible du plan 20 (300 à 400 niveaux en 45 min pour une excellente run) et laissent des centaines d'orbes au sol : un joueur qui ramasse, comme Raphaël, monte nettement plus vite. **La mesure ne peut pas trancher son ressenti** ; elle situe seulement le plancher.
- Quand le build XP se forme (seed 221092026 dans les deux modes), il accélère franchement la fin : 53 à 58 niveaux à 15 min au lieu de 37 et 28, et un niveau toutes les 11 à 14 s. Il ramasse aussi presque tout (aimant). Sans les objets offerts, il ne change rien. G6c (pas relevés) rend désormais ce build plus marqué.

**Propositions à Raphaël** (aucune appliquée, `data/scaling/progression.json` inchangé) :
1. **Recommandé : ne rien changer à la courbe pour l'instant.** G6c creuse l'écart entre une run avec build XP et une run sans. Rejouer, puis trancher sur ton ressenti.
2. Ralentir tout le monde un peu : `base_xp` 20 → 24 (chaque niveau coûte 20 % de plus jusqu'au plafond de 3 000 XP, environ 15 % de niveaux en moins à 15 min).
3. Ralentir surtout sans build : XP des orbes −15 %, le build XP compense largement (+8 % par carte commune).

### G6e — livré le 2 octobre

- **Règle :** échelle visuelle = 1 + (taille − 1), bornée entre 1 et 2 (`data/weapons/weapon_visuals.json`, `WeaponVisualConfig`). La taille est la stat de zone du joueur (`aoe_radius` : Rondelle de cuivre, coffres, bénédictions).
- **Projectiles :** sprite **et zone de contact** grandissent ensemble, remis à chaque tir sortant du pool. Changement de jeu assumé : un projectile qui paraît plus gros doit toucher plus large ; auparavant la taille ne jouait pas sur les projectiles. **À confirmer par Raphaël en jeu.**
- **Mêlée :** l'allonge dessinée n'est plus bridée à 64 px (80 px pour l'estoc) : le plafond suit l'échelle. L'ouverture de l'arc suivait déjà la taille.
- **Boîte à musique :** les notes grandissent avec la taille ; les orbes sont recréées quand la taille change (avant, leur contact restait celui de leur création).
- **Hors lot :** l'arme tenue en main (option désactivée par défaut, grille de 16 px) ne change pas ; le cône du Transistor suivait déjà la portée et la taille. Le nombre (ondes, projectiles) relève de G6f.
- **Vérification :** build sans avertissement, smoke vert, `tools/test_weapons.sh` 31/31 (contrôle d'échelle ajouté). Captures `--capture-weapons` sans et avec Rondelle de cuivre niveau 15 (taille ×2,2, échelle plafonnée à 2) inspectées : notes, flèches de l'arc, orbe du Bâton et arc de la Faucille nettement plus grands, lisibles. Pas de banc FPS : une échelle par tir, sans nœud ni allocation en plus.

### G6f — planche proposée le 2 octobre, en attente de Raphaël

[Planche](planches/21-g6f-variantes.png) : trois paliers lus sur le nombre de projectiles de l'arme (stat et Papier carbone). Palier I (1 à 2) inchangé ; palier II (3 à 5) liseré lumineux de la couleur de la famille ; palier III (6 et plus) liseré et rémanence de deux échos. Mêlée : second trait intérieur au-delà de 3 frappes, étincelles au bout de l'arc à 6 et plus. Transistor : une onde concentrique par onde en plus (4 au plus). Rien n'est intégré : seuils, liseré et rémanence sont à valider.

## 15. Lots E — équilibrage après la partie du 2 octobre (DECISIONS §50)

### E0 — mesure de référence (2 octobre, après G6 et C2)

`tools/measure_run.sh`, `--nomad --visit` (le bot avance et ratisse les lieux vus, donc ramasse presque toute l'XP : 87 à 99 % de l'XP lâchée), 3 seeds × 15 min, sans et avec `--prefer` sur Photo, Jeton et Aimant. Relevés ajoutés à RunObservation : XP lâchée (`xp_dropped`), apparitions par espèce (`spawned_by`), et à chaque écran de niveau la Chance, les crans de montée et la rareté des cartes offertes, par tranche de 5 min.

| | 5 min | 10 min | 15 min |
|---|---|---|---|
| Niveau, sans build | 15 à 26 | 27 à 62 | 54 à 219 |
| Niveau, build XP/Chance | 20 | 38 à 63 | 62 à 239 |
| Rares ou mieux parmi les améliorations offertes | 4 à 26 % | 27 à 50 % | 38 à 67 % (légendaires jusqu'à 15 %) |
| Crans de montée moyens (Chance ×10, zone, Péril) | 0,5 à 1,3 | 1,7 à 3,7 | 2,3 à 4,6 |
| PV moyen d'un ennemi apparu | 48 à 60 | 62 à 74 | 85 à 95 |
| DPS du joueur | 78 à 184 | 227 à 1 025 | 1 181 à 9 165 |
| Temps pour tuer un ennemi moyen | 0,26 à 0,65 s | 0,06 à 0,32 s | 0,01 à 0,08 s |

- **XP :** les niveaux de Raphaël (60 à 10 min) sont reproduits. Après le plafond de 3 000 XP par niveau (niveau 41 environ), les niveaux s'enchaînent : jusqu'à 219 à 15 min.
- **Raretés :** chaque cran donne 30 % de monter d'un rang ; avec 3 à 5 crans, la moitié des cartes sont rares ou mieux.
- **Ennemis :** leurs PV font ×1,8 en 15 min quand le DPS du joueur fait ×10 à ×80 : ils fondent.
- **Tirs ennemis :** les tireurs font 21 à 26 % des apparitions mais 36 à 82 % des dégâts reçus. En exploration, Champs, Forêt et Carrière n'ont que le Présage (demi-poids, après 3 min).

### Découpage

| Lot | Contenu | Vérification |
|---|---|---|
| **E1 — Cartes** | Ligne pointillée corrigée (tuiles ajustées du cadre) ; fond de carte teinté de la rareté, plus franc au survol ; titre à la couleur claire de la rareté. **Livré.** | Captures de level-up, rare et légendaire |
| **E2 — XP** | G6c annulé (Jeton +0,03, Photo +5 %, Aimant +15 %, coffres et Repères alignés) ; courbe `base_xp` 20 → 30, exposant 1,35 → 1,5, plafond 3 000 → 15 000 XP par niveau. Modèle sur le revenu mesuré : 18 à 30 niveaux à 10 min, 34 à 48 à 15 min (avant multiplicateurs d'XP). | Mesure E0 refaite |
| **E3 — Raretés** | Montée par cran 0,3 → 0,15 : aux crans mesurés, 25 à 37 % de rares ou mieux et 2 à 5 % de légendaires en fin de quart d'heure. | Même mesure |
| **E4 — Ennemis** | Après E2–E3 (le joueur sera moins fort), relever la montée des PV pour viser un temps pour tuer de 0,3 à 0,5 s stable ; plus de tireurs en exploration (environ 35 % des apparitions) dans les biomes qui n'en ont presque pas. | Mesure, captures de combat |

### E2 et E3 — livrés le 2 octobre

- **E2 :** G6c annulé (Jeton +0,03 de Chance, Photo +5 % d'XP, Aimant +15 %, coffres et Repères alignés) ; courbe d'XP `base_xp` 30, exposant 1,5, plafond 15 000 XP par niveau (atteint vers le niveau 63).
- **E3 :** montée de rareté par cran 0,3 → 0,15.
- **Mesure** (même protocole qu'E0) :

| | 5 min | 10 min | 15 min |
|---|---|---|---|
| Niveau, sans build (E0 → E2) | 15–26 → 11–14 | 27–62 → 20–34 | 54–219 → 27–47 |
| Niveau, build XP/Chance | 20 → 10–12 | 38–63 → 19–23 | 62–239 → 25–34 |
| Rares ou mieux, sans build, 10–15 min | | | 38–67 % → 20–31 % (légendaires 0 à 3 %) |
| Rares ou mieux, build Chance (Chance 0,26 à 0,37), 10–15 min | | | 40–52 % → 21–50 % |

- Bancs armes, objets et petits lieux verts.

### E4 — livré le 2 octobre

- **PV des créatures :** ×1,07 par minute jusqu'à 6 min (au lieu de 1,04), ×1,17 ensuite (au lieu de 1,075), toujours ×1,25 d'emblée (`spawn_flow.json`).
- **Tireurs :** le Cracheur (`fading_spitter`) rejoint l'exploration des Champs, de la Forêt et de la Carrière, qui n'avaient que le Présage.
- **Mesure** (`--nomad --visit`, 3 seeds × **25 min**, après E2–E3) :

| Tranche | 1–5 min | 5–10 | 10–15 | 15–20 | 20–25 |
|---|---|---|---|---|---|
| Temps pour tuer un ennemi moyen (E2–E3 → E4) | 0,45–0,57 → 0,55–0,73 s | 0,19–0,40 → 0,32–0,49 s | 0,12–0,26 → 0,22–0,50 s | → 0,15–0,54 s | → 0,18–0,62 s |
| PV moyen d'un ennemi apparu | 51 | 85 | 180 | 430 | 950 |
| Niveau atteint en fin de tranche | 10–11 | 19–20 | 26–27 | 30–37 | 33–43 |

- Les tireurs font 31 à 34 % des apparitions (23 à 28 % avant).
- **À juger en jeu par Raphaël :** les ennemis plus solides réduisent les éliminations, donc l'XP : 19–20 niveaux à 10 min pour le bot (Raphaël en avait 60 avant E2). Si c'est trop lent, `base_xp` 30 → 25 rend environ 15 % de niveaux. La pression en fin de partie (dégâts reçus ×4 à ×10 entre 10 et 25 min) n'est mesurée que sur un bot invincible : la survie se juge en jouant.

### E5 — niveaux : un entre-deux (DECISIONS §51), livré le 2 octobre

Trois réglages mesurés avec les ennemis d'E4 (`--nomad --visit`, 3 seeds × 25 min) :

| Réglage (base / exposant / plafond, XP par victime) | Niveau à 10 min | 15 min | 25 min | Temps pour tuer, 15–25 min |
|---|---|---|---|---|
| E4 : 30 / 1,5 / 15 000, +2 %/min | 19–20 | 26–27 | 33–43 | 0,15–0,62 s |
| 20 / 1,4 / 8 000, +5 %/min | 27–42 | 37–63 | 55–124 | 0,04–0,31 s |
| 25 / 1,45 / 10 000, +3 %/min | 20–28 | 24–38 | 35–72 | 0,07–0,68 s |
| **Retenu : 20 / 1,4 / 20 000, +2 %/min** | **24–37** | **33–52** | **55–77** | 0,08–0,20 s |

- Avant tout l'équilibrage (E0) : 27 à 62 à 10 min, jusqu'à 239 à 15 min.
- L'XP par victime qui croît plus vite emballe la fin de partie : la croissance reste à +2 % par minute ; le plafond à 20 000 n'est atteint que vers le niveau 139.
- **Revers :** plus de niveaux, plus de puissance : après 15 min, le temps pour tuer redescend à 0,08–0,2 s sur deux seeds sur trois (0,01–0,08 s avant E4). Si les ennemis paraissent de nouveau faibles en fin de partie, relever `late_hp_scaling_per_minute` (1,17) plutôt que ralentir les niveaux.
- **À surveiller :** sur une seed, la Chance atteint 0,67 à 0,86 entre 15 et 25 min (Repères, bonus de coffre), soit 8 à 11 crans et la moitié des cartes rares ou mieux.

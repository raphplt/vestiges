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

Les deux voies de chacune des 24 armes s'écrivent au lot G3. Les quatre exemples ci-dessus sont en jeu. Les voies des 20 autres armes sont proposées dans [21-historique.md §28](21-historique.md#28-lot-g3--ascensions-darmes--découpage-et-proposition-pour-20-armes-30-septembre), à valider.

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

### Objets de propriété (15)

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
| Ascensions des 20 autres armes | **Proposées, à valider par Raphaël** | G3, étape 2 |
| Affinités des personnages, Souverain à 25 % | À faire | G4 |
| Sept Réminiscences restantes | À faire | G5 |
| Surplus, déblocages | À faire | Plans 20, 22 |
| Anciens Dons des coffres et leurs synergies | **Retirés** ; les coffres donnent des niveaux d'objet (un niveau = une carte commune) | G2b, étape 1, plan 23 R3 |
| Bonus d'une stat au hasard à chaque coffre, en plus du butin (un niveau d'objet commun, ×2 rare, ×3 épique) | **En jeu** | Plan 23, R8 |
| Repères : +1 % de Chance au premier usage de chaque type de lieu (douze types) | **En jeu** | Plan 23, R9 |

## 13. Ce que ce document remplace

- [21-historique.md](21-historique.md) : raisonnement et comptes rendus des lots G1 et G2a.
- [05-perks-specialisations.md](05-perks-specialisations.md) : reste la fiche technique détaillée des sept effets livrés (cas limites, contrats). Son calendrier d'acquisition et ses lots B3–B4 sont périmés.
- [05-objets-catalogue-v1.md](05-objets-catalogue-v1.md), [05-catalogue-objets-perks.md](05-catalogue-objets-perks.md), [05-armes-objets-builds.md](05-armes-objets-builds.md) : périmés pour tout ce qui concerne les objets, les passifs et les perks.
- Plan 17, vague 4 et plafond d'armes ; plan 20, mentions d'un plafond à 70 : périmés.

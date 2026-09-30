# Plan 21 — Le système de jeu : référence unique

30 septembre 2026 · **Ce document fait foi.** Il fixe comment un build se construit dans Vestiges : armes, objets, Réminiscences, personnages, niveaux, hasard. Demande de Raphaël ([DECISIONS §35](DECISIONS.md)) : une seule version, détaillée, gardée à un seul endroit, puis appliquée.

**Règles de tenue du document :**
- Toute décision qui touche au build se reporte ici avant d'être codée. Aucun autre plan ne redéfinit ce qui est écrit ici.
- La **structure** est fixée. Les **chiffres** sont des valeurs de départ, à régler par mesure ; ils se changent ici et dans `data/`.
- Conçu avec les [douze principes](../PRINCIPES-BUILD.md). Raisonnement et versions précédentes : [21-historique.md](21-historique.md).
- La carte, ses lieux et ce qu'ils donnent : [plan 22](22-carte-a-explorer.md).

## 1. Vue d'ensemble

| Famille | Question | Emplacements | Niveaux | D'où ça vient |
|---|---|---|---|---|
| **Armes** | Quoi : comment j'attaque | 4 | 1 à 50, puis ascension | Niveau ; au sol |
| **Objets** | Combien et quand : l'efficacité et ce qui se déclenche | 6 | 1 à 50, palier au niveau 25 | Niveau ; Reliquaire |
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
| Amélioration d'objet | L'objet n'est pas au niveau 50 | +1 à +5 niveaux selon la rareté (§4) |

- **Composition :** si c'est possible, au moins une nouveauté et au moins une amélioration. Tant que le joueur n'a aucun objet de survie, une des trois cartes en propose un.
- **Rareté d'une amélioration :** commune, inhabituelle, rare, épique, légendaire, de poids 60 / 25 / 11 / 3,5 / 0,5. La **Chance**, l'**oubli de la zone** où se tient le joueur et le **Péril** ajoutent des crans de montée.
- **Relancer :** 3 par run.
- **Bannir, c'est oublier :** 3 gratuits par run, puis le n-ième payant coûte n tiers de point de Péril. Une carte bannie ne revient plus de la run.
- **Passer :** toujours possible.

## 3. Armes

- **4 emplacements.** L'arme de départ vient du personnage. Une arme trouvée au sol s'équipe si un emplacement est libre, sinon elle s'échange contre celle du premier emplacement.
- **24 armes**, toutes de même rang : il n'y a pas d'arme « rare ». La rareté porte sur les améliorations.
- **Niveau 1 à 50.** Chaque amélioration tire au hasard une ou plusieurs stats parmi celles que l'arme peut monter, pondérées par arme :

| Rareté | Stats touchées | Ampleur par stat | Palier (stat entière : projectile, perforation, saut, orbe) |
|---|---|---|---|
| Commune | 1 | × 1 | — |
| Inhabituelle | 1 | × 1,5 | — |
| Rare | 2 | × 1,25 | — |
| Épique | 1 | × 1,75 | +1, sinon 2 stats |
| Légendaire | 3 | × 2 | +1, sinon 3 stats |

- **Pas par stat**, à l'ampleur × 1 : dégâts +12 %, cadence +8 %, portée +6 %, taille d'arc ou de cône +8 %, recul +15 %, vitesse de projectile ou d'orbite +10 %.
- **La carte nomme la propriété touchée** (§7) et montre la valeur avant → après.
- **Ascension au niveau 50 :** la carte suivante de cette arme propose **deux voies**, au choix et pour de bon. Une voie transforme l'arme et ouvre un changement de régime. Exemples :

| Arme | Voie A | Voie B |
|---|---|---|
| Arc du gymnase | **Volée** : éventail, le Nombre compte double | **Transpercer** : une seule flèche, qui traverse tout |
| Faucille | **Moisson** : cercle complet, plus lent | **Estoc** : frappe droite deux fois plus longue |
| Cloche d'école | **Glas** : ralentit deux fois plus longtemps, sans recul | **Tocsin** : repousse fort et désoriente |
| Boîte à musique | **Ronde** : les orbes s'éloignent et reviennent | **Berceuse** : orbes proches, qui endorment un instant |

Les deux voies de chacune des 24 armes s'écrivent au lot G3.

## 4. Objets

- **6 emplacements.** Un objet ne se cumule pas en exemplaires : il **monte de niveau**, de 1 à 50.
- **Deux sortes**, dans les mêmes emplacements :
  - objets de **propriété** : montent une propriété commune (§7) ;
  - objets de **déclencheur** : font quelque chose à un moment précis (impact, élimination, critique, blessure, dash, niveau gagné, zone oubliée).
- **Obtenir un objet neuf :**
  - par une carte « nouvel objet » au niveau, tant qu'un emplacement est libre ;
  - par un **Reliquaire** (plan 22) : trois alcôves, une prise, les deux autres s'effacent. Certains objets ne se trouvent que là (« monde »).
- **Monter un objet :**
  - par une carte d'amélioration au niveau : commune +1 niveau, inhabituelle +2, rare +3, épique +4, légendaire +5 ;
  - par le monde : Reliquaire une fois les 6 emplacements pris, coffres.
- **Palier au niveau 25 :** chaque objet y gagne un effet propre. C'est là qu'un objet change la manière de jouer.
- **Remplacer :** un Reliquaire peut proposer de remplacer un objet. Le nouveau démarre à la moitié du niveau de l'ancien.
- **Accès :** D = disponible dès le départ ; Q = débloqué par une quête ; V = acheté en Vestiges ; M = monde seulement (Reliquaire).

### Objets de propriété (14)

`n` = niveau de l'objet.

| Objet | ID | Effet au niveau n | Palier 25 | Accès |
|---|---|---|---|---|
| Ressort de sommier | `memoire_vive` | Fréquence +1,2 % × n | Chaque 10ᵉ attaque d'une arme part deux fois | D |
| Papier carbone | `souffle_du_neant` | +1 copie d'attaque, qui inflige 30 % + 1,4 % × n des dégâts | +1 copie de plus (et une autre au niveau 50) | Q |
| Rondelle de cuivre | `resonance` | Taille +1,2 % × n | Les zones frappent une seconde fois, à 30 % | D |
| Mètre pliant | `portee_etendue` | Portée +1 % × n | Un projectile en bout de course éclate en petite zone | D |
| Pince à linge | `persistance` | Durée +1,5 % × n (statuts, zones au sol ; orbites quand elles auront une durée) | Un statut qui expire a 25 % de chance de se renouveler | D |
| Lunettes de lecture | `oeil_critique` | Chance de critique +0,6 % × n, dégâts critiques +1 % × n | Un critique sur une cible à PV pleins compte double | Q |
| Bouton de manteau | `ancrage` | PV max +4 × n | Les coups inférieurs à 3 % des PV max sont ignorés | D |
| Bobine de fil | `regeneration` | +0,06 PV/s × n | La régénération double pendant 3 s après une blessure | D |
| Genouillère | `peau_dure` | Armure +0,8 × n | L'armure compte double pendant le dash et 1 s après | V |
| Écusson de pompier | `carapace` | Bouclier max +1,5 × n | Quand le bouclier casse, une onde repousse les ennemis proches | V |
| Lacet rouge | `instinct` | Vitesse +0,6 % × n, recharge du dash +1 % × n | Le dash va 30 % plus loin | D |
| Aimant de frigo | `siphon_essence` | Rayon d'attraction de l'XP +3 % × n | Chaque orbe ramassée rend 0,2 PV | D |
| Photo de classe | `photo_de_classe` | XP +1 % × n | Chaque niveau gagné donne 3 Essence | D |
| Jeton de fête foraine | `jeton_de_fete` | Chance +0,01 × n | +1 relance tous les 15 niveaux du joueur | D |

Un palier se décrit en données avec l'objet (`milestones` : niveau, effet, texte, paramètres). Il n'est annoncé sur une carte ou dans la pause que si son effet est codé. Les copies du Papier carbone sont des crans de la formule de l'effet (`step`, `step_levels`).

Il n'existe **pas** d'objet « +dégâts » universel : les dégâts viennent des niveaux d'armes.

Les ID des anciens passifs sont conservés pour ceux qui migrent. `flamme_interieure` et `reflet_brise` ne sont plus proposés (désactivés, ID gardés).

### Objets de déclencheur (19)

| Objet | Déclencheur | Effet au niveau n | Palier 25 | Accès |
|---|---|---|---|---|
| Allumette humide | Impact | 6 % + 0,4 % × n d'enflammer (Brûlure) | La Brûlure passe au voisin quand la cible meurt | D |
| Glaçon dans un mouchoir | Impact | 6 % + 0,4 % × n de ralentir | Un ennemi ralenti deux fois est figé 0,5 s | D |
| Pétard mouillé | Élimination | La victime explose : 20 % + 1,6 % × n des dégâts du coup | L'explosion se produit deux fois | Q |
| Dé à coudre | Élimination | Rend 0,1 + 0,02 × n PV | Une élite tuée rend 5 % des PV max | D |
| Loupe de philatéliste | Critique | La cible devient Fragile 2 s : +10 % + 0,4 % × n de dégâts subis | Fragile dure 4 s | Q |
| Stylo à quatre couleurs | Critique | L'attaque repart sur une autre cible, à 20 % + 1,2 % × n | Elle repart sur deux cibles | Q |
| Semelle usée | Déplacement continu de 2 s | La prochaine attaque fait +15 % + 1 % × n | Vaut pour les deux prochaines attaques | D |
| Tabouret de camping | Immobile depuis 1 s | Fréquence +10 % + 0,8 % × n | Et armure +10 | Q |
| Chewing-gum | Dash | Traînée qui ralentit, 1 s + 0,04 s × n | La traînée rend aussi Fragile | D |
| Gilet réfléchissant | Ennemis à moins de 120 px | +0,5 % + 0,05 % × n de dégâts par ennemi, 10 au plus | 20 ennemis au plus | Q |
| Thermos | PV au-dessus de 90 % | Dégâts +5 % + 0,5 % × n | Seuil abaissé à 75 % | V |
| Médaille cabossée | PV sous 35 % | Dégâts +8 % + 0,6 % × n | Et vitesse +15 % | Q |
| Boîte de pansements | Niveau gagné | Soigne 1 % + 0,06 % × n des PV max | Une cascade de 3 niveaux ou plus donne 1 s d'invulnérabilité | V |
| Thermomètre | Cible brûlée | Dégâts +10 % + 0,8 % × n contre elle | Les ennemis brûlés sont aussi ralentis de 15 % | D |
| Épingle à nourrice | Cible ralentie | Dégâts +10 % + 0,8 % × n contre elle | Un ennemi ralenti tué prolonge de 1 s le ralentissement de ses voisins | D |
| Porte-monnaie usé | Essence gardée | +1 % de dégâts par tranche de 10 Essence, plafond 10 % + 1 % × n | 20 % de l'Essence dépensée est rendue | Q |
| Presse-papier en verre | En zone oubliée | Dégâts +5 % + 0,5 % × n | Les malus de vitesse des zones oubliées ne s'appliquent plus | M |
| Calendrier arraché | Permanent | L'Effacement avance 10 % plus vite ; XP et Essence +6 % + 0,5 % × n | Chaque Résurgence survécue laisse un coffre de plus | M |
| Médaillon ouvrant | Coup fatal | Une fois par run : reste à 1 PV, invulnérable 1 s + 0,04 s × n | Se recharge à chaque Résurgence survécue | M |

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

- **Carte de niveau :** la propriété touchée, la valeur avant → après, les armes concernées.
- **Pause :** armes, objets avec leur niveau et leur effet du moment, Réminiscences avec leur état.
- **Combat :** chaque déclencheur d'objet et chaque Réminiscence a un retour visuel propre.
- **Bilan :** dégâts par source (armes, objets, Réminiscences) et la chaîne la plus rentable de la run.

## 12. État d'application

| Élément | État au 30 septembre | Lot |
|---|---|---|
| Armes : 4 emplacements, niveau 50, stats aléatoires à rareté | **En jeu** | — |
| Relances limitées, bannissements gratuits puis Péril | **En jeu** | G1 |
| Réminiscences après les Résurgences, 7 sur 14, libellées « Réminiscence » | **En jeu** | G1, G2a |
| Objets : 6 emplacements, 50 niveaux par formule, effets multiples, rareté qui donne 1 à 5 niveaux | **En jeu** | G2a |
| 14 objets de propriété, dont Papier carbone (copies à dégâts réduits) et Pince à linge (Durée) | **En jeu** | G2a, G2a-2 |
| Paliers des 14 objets de propriété (niveau 25, et 50 pour Papier carbone) | **En jeu** | G2a-2 |
| Objets de déclencheur, statut Fragilité, coefficients | À faire | **G2b, suivant** |
| Reliquaire, objets « monde », remplacement | À faire | Plan 22, C3 |
| Propriétés nommées sur les cartes d'armes | À faire | G0 |
| Ascensions d'armes | À faire | G3 |
| Affinités des personnages, Souverain à 25 % | À faire | G4 |
| Sept Réminiscences restantes | À faire | G5 |
| Surplus, déblocages | À faire | Plans 20, 22 |
| Anciens Dons des coffres et leurs synergies | **Retirés** ; les coffres donnent des niveaux d'objet | G2b, étape 1 |

## 13. Ce que ce document remplace

- [21-historique.md](21-historique.md) : raisonnement et comptes rendus des lots G1 et G2a.
- [05-perks-specialisations.md](05-perks-specialisations.md) : reste la fiche technique détaillée des sept effets livrés (cas limites, contrats). Son calendrier d'acquisition et ses lots B3–B4 sont périmés.
- [05-objets-catalogue-v1.md](05-objets-catalogue-v1.md), [05-catalogue-objets-perks.md](05-catalogue-objets-perks.md), [05-armes-objets-builds.md](05-armes-objets-builds.md) : périmés pour tout ce qui concerne les objets, les passifs et les perks.
- Plan 17, vague 4 et plafond d'armes ; plan 20, mentions d'un plafond à 70 : périmés.

# Plan 05 — Perks de spécialisation, catalogue B V1

29 septembre 2026 · **Catalogue V1 de neuf perks validé et extensible ; B0, B1, B2 et la partie sans objets de B3 livrés et vérifiés : sept perks actifs en run. Délestage et Habitude attendent les objets (B4). Coefficients à éprouver, plafond d’armes non arbitré.**

Références : [Stratégie V2](../VESTIGES-STRATEGIE-V2.md), [audit et étude A/B/C](05-catalogue-objets-perks.md), [personnages](06-fiches-casting.md), [progression](20-recompense-et-puissance.md). Ce document devient le contrat courant des perks. Les seize perks à dix niveaux et les sept exemples de l’étude sont historiques. La V1 est validée ; elle n’interdit pas de futurs ajouts. Sept effets sont actifs en run (Prévoyance, Reprise, Débordement, Convergence, Propagation, Sillage, Seconde lecture) ; Délestage et Habitude restent hors des offres jusqu'aux objets.

## 1. Contrat retenu et portée de cette version

Raphaël choisit définitivement B et demande une version aboutie des perks. **Quatre emplacements, quatre règles acquises une seule fois, aucun niveau de perk, aucune rareté de perk, aucun doublon.** Les armes évoluent ; les objets s'accumulent sans limite de slots ni d'exemplaires. Le personnage garde son pouvoir inné hors inventaire.

Cette proposition fixe **neuf perks** et leur fonctionnement. Elle fixe aussi les cas limites nécessaires à une implémentation cohérente. « Version complète » ne signifie pas équilibrage prouvé : coefficients, calendrier et sensations seront testés. Les anciens noms/effets n'ont aucun droit acquis dans ce catalogue ; les droits de déblocage du profil doivent en revanche être conservés lors de la migration.

| Famille | Perks | Intention |
|---|---|---|
| Combat | Convergence, Débordement, Propagation | Choisir une menace, exploiter la surpuissance, prolonger un contrôle déjà produit |
| Survie | Prévoyance, Reprise | Préparer un affrontement ou récupérer une blessure récente |
| Déplacement et collecte | Sillage | Récolter en continuant sa route |
| Récompenses | Délestage, Seconde lecture, Habitude | Arbitrer XP/objets, conserver une occasion, concentrer ses piles |

Les familles servent à composer des offres variées ; elles n'imposent aucun emplacement réservé. Les neuf perks sont accessibles initialement dans cette proposition. Les armes/objets et leurs déblocages apportent déjà une progression de contenu : pas de quêtes artificielles supplémentaires pour remplir quatre slots.

## 2. Acquisition, lisibilité et durée de vie

- Choix aux **niveaux joueur 2, 6, 12 et 20**, parmi trois perks, à la place du choix ordinaire. Calendrier de départ à mesurer ; aucun écran supplémentaire pour ce même niveau.
- Les autres niveaux proposent armes et améliorations d'armes, avec leur rareté. Le cumul des niveaux et la réserve automatique R1-G restent communs. Une cascade franchissant un palier conserve ce choix à sa place dans la file.
- Première offre : au moins un défensif ; si les candidats le permettent, un combat et un déplacement/récompense. Ne pas forcer Prévoyance dans toutes les runs : alterner les deux défenses. En l'absence d'un candidat compatible dans une famille, compléter avec un autre perk éligible.
- Éligibilité selon l'équipement et les services réellement disponibles, précisée dans les fiches. Aucun doublon. Une famille n'est pas une garantie que chaque candidat sera proposé.
- Passer un choix consomme ce niveau et conserve le droit au slot. Nouvelle occasion au prochain niveau ordinaire ; pas de réouverture immédiate. Au plus un droit servi par niveau, quatre droits au total. En l'absence de trois candidats, présenter les un/deux disponibles et passer ; sans candidat, choix d'arme normal et report du droit.
- Relances et bannissements gardent leurs ressources communes. Un bannissement ne doit pas rendre impossible le nombre de slots restant à remplir avec le catalogue déverrouillé ; conserver le refus existant, adapté à ce contrat. Une relance peut produire une offre courte sans inventer un perk incompatible.
- Pas de remplacement libre des perks pendant la run. Un choix économique précoce reste un engagement. Changer d'arme peut rendre un perk inactif ; afficher la conséquence avant l'échange, puis conserver le choix du joueur.
- Chaque carte : une phrase d'effet, sa condition éventuelle, et une précision courte. La pause détaille les chiffres et états utiles. Aucune barre d'XP de perk ou amélioration de rareté.
- Certains perks d'économie perdent leur utilité une fois l'arsenal complet. L'annoncer dans leur fiche : le surplus est une autre phase, pas une raison de leur inventer un second pouvoir ou une conversion automatique.

## 3. Les neuf fiches

Tous les coefficients ci-dessous sont des valeurs initiales de réglage à mettre en données. Les événements de combat, de soin et d'acquisition doivent porter leur origine ; les propositions de contrats techniques sont en §7.

### 3.1 Convergence — `priority_targeting`

**Texte joueur :** « Tes armes à ciblage privilégient les élites et les Souverains à portée. »

Dans chaque recherche de cible compatible avec l'arme, préférer les élites/Souverains aux créatures ordinaires. Dans cette catégorie, conserver la cible valide déjà suivie ; sinon choisir la plus proche. Une cible ordinaire laisse la place lorsqu'une menace prioritaire devient éligible. Portée, obstacles et restrictions de l'arme continuent de s'appliquer. Aucun gain de dégâts ni capacité de guidage ajouté.

Les règles impératives d'une arme ou d'un pouvoir de personnage priment sur ce classement : ne pas remplacer le rebond lointain du Facteur ou la marque du Traqueur. Une onde, une orbite et un motif sans recherche de cible restent inchangés.

**Offre :** au moins une arme équipée possède un ciblage compatible. **Synergies :** allonge, cadence, armes lourdes, Débordement. **Coût :** l'arsenal peut négliger la foule ; placement nécessaire pour tenir la bonne cible à portée. **Feedback :** discret indicateur sur la cible prioritaire effectivement suivie, sans nouvelle marque de dégâts.

### 3.2 Débordement — `overflow`

**Texte joueur :** « Une partie des dégâts inutilisés d'un coup fatal renforce le prochain impact de cette arme. »

Reporter **50 % de l'excédent natif** d'un impact direct fatal dans une réserve par arme, valable **3 s**, plafonnée au dommage natif de référence d'une attaque de cette arme. Le prochain impact direct consomme la réserve sur sa première cible. Une arme retirée perd sa réserve.

Le dommage natif comprend les modificateurs du coup, mais exclut un report précédent et les dégâts secondaires. Calculer l'excédent à partir des PV avant impact et de ce dommage résolu : si seul le report rend le coup fatal, ce coup ne crée pas de nouvel excédent natif. Le report ne critique pas une seconde fois et ne produit aucun proc indépendant. Les morts de feu/saignement, objets, ondes de surplus et répliques secondaires ne chargent pas la réserve.

La réserve est un montant de dégâts, non un pourcentage à réappliquer à la prochaine attaque. À sa consommation, seuls les modificateurs défensifs de la nouvelle cible s'appliquent à ce montant ; ne pas réappliquer les bonus offensifs du joueur. Le plafond est calculé et figé sur la contribution de l'attaque qui charge, pas amplifié rétroactivement par une amélioration d'arme.

Une attaque à plusieurs impacts peut agréger leurs excédents, sous un plafond unique. Elle ne consomme pas une réserve qu'elle vient elle-même de créer : identifier le lancement d'attaque. Une réserve existante se consomme au premier impact d'un lancement ultérieur ; durée rafraîchie seulement par un nouvel excédent natif réel. Référence d'attaque et plafond doivent traiter explicitement les salves et les impacts continus, sans compter chaque projectile comme une attaque complète.

**Offre :** au moins une arme cause des impacts directs attribués. **Synergies :** dégâts, critique, Convergence. **Coût :** dépend des surkills, de la cadence et des cibles ; ne remplace pas une attaque de zone. **Feedback :** réserve prête sur l'icône d'arme, consommation visible sur le prochain impact.

### 3.3 Propagation — `carry_control`

**Texte joueur :** « À leur mort, les ennemis transmettent leur ralentissement et leur désorientation à un voisin. »

À une élimination attribuée au joueur, transmettre les contrôles admissibles encore actifs à **un ennemi vivant dans un rayon de 120 px**, le plus proche de la victime. Copier leur intensité et leur durée restante, sans prolongation. Les deux contrôles peuvent atteindre le même voisin. Les immunités/résistances de la cible restent applicables ; ne pas affaiblir ou raccourcir un effet plus fort déjà présent.

Seuls **ralentissement et désorientation appliqués directement par une arme du joueur** sont admissibles. Aucun feu, saignement, recul, marque de personnage, effet de boss ou état générique. Un effet reçu par Propagation ne se transmet pas à son tour ; une application native ultérieure peut à nouveau être transmissible. Conserver cette provenance par effet, pas seulement par ennemi.

**Offre :** au moins une arme équipée produit un de ces deux contrôles. **Synergies :** Cloche, Chronomètre, Polaroïd ; les autres armes peuvent terminer une cible préparée. **Coût :** un contrôle doit déjà exister et survivre jusqu'à la mort ; aucun effet contre une foule entièrement hors rayon. **Feedback :** bref lien vers le receveur, puis l'indicateur de contrôle existant. Le pouvoir de transmission de marque du Traqueur reste séparé et exclusif au personnage.

### 3.4 Prévoyance — `overheal_reserve`

**Texte joueur :** « Tes soins excédentaires sont conservés pour te rendre des PV après une blessure. »

Réserve maximale : **20 % des PV max**, remplie à l'acquisition. Ensuite, chaque soin éligible remplit d'abord les PV ; seul son excédent alimente la réserve. La régénération doit fonctionner à PV pleins pour ce calcul. Les changements de PV maximum et les remboursements produits par ces perks ne constituent pas des soins excédentaires.

Après un coup ennemi non fatal ayant réellement retiré des PV, restituer au plus le dommage subi et le stock disponible. Consommer exactement ce qui est rendu. Aucun effet sur un coup absorbé par le bouclier, esquivé ou ignoré. Le Néant ne déclenche pas cette restitution. Aucun sauvetage d'un coup fatal, aucune recharge de bouclier, aucun déclenchement d'un nouveau soin par la restitution.

Si les PV max augmentent, la capacité grandit mais la réserve ne se remplit pas ; si les PV max baissent, tronquer l'excédent. L'augmentation de capacité n'est pas un soin.

**Offre :** toujours ; la charge initiale assure un usage immédiat, le monde possède des soins. **Synergies :** régénération, soins, PV max, Scalpel. **Coût :** reconstitution dépendante de vrais soins excédentaires ; ne protège pas d'un coup fatal ni de l'Effacement. **Feedback :** réserve distincte des PV et du bouclier, avec stock lisible.

### 3.5 Reprise — `rally`

**Texte joueur :** « Après une blessure, tuer rapidement peut te rendre une partie des PV perdus. »

Un coup ennemi non fatal ouvre une fenêtre de **4 s**. **40 % de la perte effective restant après Prévoyance** deviennent récupérables, dans la limite de **20 % des PV max** et des PV actuellement manquants. Chaque élimination attribuée au joueur pendant la fenêtre restitue jusqu'à **2 % des PV max**, sans dépasser le budget ni les PV manquants.

Un nouveau coup dans cette fenêtre ajoute son budget admissible sous le même plafond mais **ne repousse pas l'expiration**. Après expiration, tout reste est perdu ; un nouveau coup peut ouvrir une nouvelle fenêtre. Les soins effectivement reçus d'une autre source réduisent le budget récupérable point pour point, pour ne pas récupérer deux fois une même blessure. La restitution de Reprise diminue directement le budget, sans second débit.

Exclure les dégâts du Néant, sacrifices, variations de PV max, coups absorbés/ignorés et morts sans attribution au joueur. Inclure les éliminations normales de ses armes et effets persistants attribués ; un ennemi donne un seul crédit. Les restitutions ne créent pas d'excédent pour Prévoyance et ne remplissent pas le bouclier. Une mort du joueur reste immédiate.

**Offre :** toujours. **Synergies :** couverture offensive, contrôle, PV max. **Coût :** exige de rester capable de tuer après la blessure ; ne soigne jamais une vieille blessure et ne sert pas face à un boss isolé sans victime disponible. **Feedback :** portion récupérable de la barre de vie et expiration visible.

### 3.6 Sillage — `xp_trail`

**Texte joueur :** « Ton trajet récent continue de ramasser l'XP derrière toi. »

Conserver les **6 dernières secondes de déplacement réel** comme couloir de collecte. Une orbe dans ce couloir peut rejoindre le joueur, même créée après son passage. Demi-largeur du couloir égale au rayon de collecte actuel. La collecte normale autour du joueur reste active ; chaque orbe ne peut être créditée qu'une fois.

Le trajet vieillit au temps de jeu, même à l'arrêt ; la pause le suspend. Un dash traversé physiquement trace le trajet, une téléportation coupe le segment. Aucun tracé entre deux positions de réapparition ni collecte le long d'un téléport. Les portions de monde réellement supprimées/invalidées cessent d'être collectables ; Sillage ne maintient pas le terrain et ne recrée pas une orbe déjà détruite.

**Offre :** toujours. **Synergies :** attraction, armes qui frappent derrière, déplacement nomade. **Coût :** rayon et durée limités ; ne récupère ni objets ni Essence, ne crée pas d'XP. **Feedback :** attraction depuis le trajet visible au moment utile, sans ruban permanent sur le sol. Le reflux de Résurgence garde son propre rôle.

### 3.7 Délestage — `salvage_xp`

**Texte joueur :** « Dans les récompenses à choix, tu peux abandonner un objet contre de l'XP. »

Avant acquisition d'un objet révélé dans un coffre ou une récompense de Souverain avec écran, proposer sa conversion en XP. Aucun inventaire existant n'est débité. Les butins ramassés automatiquement et les récompenses narratives n'ouvrent pas de nouvel écran et ne sont pas convertibles.

Valeur initiale = coût normal du prochain niveau joueur au moment de la révélation × coefficient de rareté de l'objet : **commun 0,20 ; inhabituel 0,40 ; rare 0,75 ; épique 1,25**. Puis appliquer une seule fois le bonus général d'XP du build. Afficher le gain final, figé avec l'offre. Aucun multiplicateur de Péril/zone/récompense d'ennemi supplémentaire. Le coût normal sert aussi en surplus, pour éviter un changement caché de tarif à l'intérieur d'une même offre ; cette XP suit ensuite la règle de progression en vigueur.

Une unité abandonnée produit un crédit unique ; sur un paquet de plusieurs unités, afficher quantité et total. Pas de revente après acquisition, conversion d'arme, conversion de niveau garanti ou transformation d'Essence. Changer de carte ne résout pas à nouveau la récompense.

**Offre :** le catalogue d'objets et ces récompenses doivent être actifs dans la run. **Synergies :** objets d'XP, croissance précoce, Sillage. **Coût :** perte définitive de l'objet et de sa contribution future ; intérêt moindre au surplus. **Feedback :** choix « Prendre » / « Convertir : X XP » dans la même récompense.

### 3.8 Seconde lecture — `carried_choice`

**Texte joueur :** « Une amélioration laissée de côté peut revenir au prochain choix de niveau. »

Après sélection d'une carte, conserver **la plus rare des améliorations d'arme neuves non choisies**, une seule, avec ses gains concrets. À égalité, prendre celle de gauche. Au prochain choix ordinaire d'arme, cette carte remplace une des trois nouvelles cartes. Elle garde ses valeurs et sa rareté ; ce n'est pas un nouveau tirage. Les deux autres places restent neuves et ne proposent pas une autre amélioration de la même instance d'arme. Si le pool ne suffit pas, présenter moins de trois cartes selon les règles communes.

Une carte reportée n'est valable que pour cette prochaine offre : si elle n'est pas sélectionnée, elle expire. Elle ne peut jamais être reportée deux fois. Les autres cartes neuves de cette offre peuvent préparer le report suivant. Les choix de perks ne consomment pas le report et n'en créent pas.

Revalider propriété et possibilité d'appliquer les gains, notamment après une amélioration du monde ou un changement d'arme. Une carte devenue invalide est libérée et remplacée par une carte normale. Passer une offre ordinaire efface le report et n'en crée pas ; bannir la carte reportée l'efface. Relancer une offre renouvelle les cartes neuves, avec la charge habituelle, et conserve le report déjà engagé ; aucune nouvelle carte n'est mise en réserve avant une sélection effective.

**Offre :** au moins deux armes équipées et améliorables, pour proposer immédiatement de vraies alternatives. **Synergies :** Chance, cascades, arsenal varié. **Coût :** une place de nouveautés occupée ; le joueur doit choisir au prochain niveau ou perdre l'occasion. **Feedback :** carte marquée « Reportée », sans compteur ni monnaie supplémentaires. **Cette version remplace celle limitée aux trois relances de la run.**

### 3.9 Habitude — `familiar_loot`

**Texte joueur :** « Dans les récompenses à choix, tu peux préférer une copie d'un objet que tu possèdes déjà. »

Pour chaque objet révélé dans une récompense admissible à Délestage, proposer **un autre objet déjà possédé de même rareté**, tiré uniformément parmi les IDs éligibles distincts. Les quantités possédées ne pondèrent pas ce tirage. Le joueur choisit l'original ou l'alternative ; quantité et rareté restent identiques. Sans candidat, acquisition normale.

Les deux possibilités sont résolues et figées à l'ouverture. Aucune relance gratuite en fermant/réouvrant, aucun échange d'objet déjà acquis, aucun deuxième traitement de l'alternative comme une nouvelle récompense. Sur une offre qui proposait déjà plusieurs objets, remplacer cette règle par une alternative clairement liée à chaque carte, sans multiplier les gains ni ajouter un second écran.

**Offre :** au moins un objet déjà possédé et une récompense admissible possible. **Synergies :** concentration d'objets d'XP/Chance/soin, choix de build volontaire. **Coût :** alternative parfois peu intéressante, même rareté obligatoire ; aucune garantie de l'objet préféré. **Feedback :** objet original et alternative avec quantité possédée/effet total après gain.

Avec Délestage, une seule décision : **original, alternative, ou XP**. La conversion utilise la récompense d'origine et ne s'applique qu'une fois ; mêmes quantité/rareté, donc aucune différence de prix exploitable. Les objets exclusifs de quête ne sont ni remplacés ni convertis.

## 4. Cohérence du catalogue et exemples de builds

| Build | Quatre perks | Intention et limite |
|---|---|---|
| Chasseur d'élites | Convergence, Débordement, Prévoyance, Sillage | Préparer des frappes lourdes, soutenir l'approche et continuer à récolter ; la foule peut déborder le joueur |
| Contrôle offensif | Propagation, Reprise, Débordement, Sillage | Transformer les premières victimes contrôlées en ouverture pour avancer et récupérer ; exige les armes compatibles |
| Croissance choisie | Délestage, Seconde lecture, Habitude, Sillage | Arbitrer piles utiles/XP et améliorer plusieurs armes ; aucun perk de défense, perte d'intérêt partielle au surplus |
| Endurance | Prévoyance, Reprise, Propagation, Habitude | Préparer les soins, contrôler la foule, concentrer les objets utiles ; dégâts dépendants de l'arsenal et du butin |

Ce sont des intentions testables, pas des builds gagnants établis. Aucun bonus de set supplémentaire. Le choix de quatre perks n'oblige pas à prendre un exemplaire de chaque famille.

**Traversée sort de cette version.** Son effet dépendait de contributions locales d'XP et de rareté encore en refonte, et risquait de devenir un bonus conditionnel difficile à lire. Les objets et les récompenses de zone continuent de porter l'axe oubli. Ce retrait n'interdit pas de réexaminer ultérieurement un vrai changement de règle lié à l'Effacement.

**Objets : travail restant impératif.** Le catalogue historique de 24 objets n'est pas automatiquement adopté avec B. En particulier : conserver un accès concret à la chance critique (l'ancien catalogue ne propose que les dégâts critiques), XP, Chance, soins, portée et taille ; revoir miroir/ondes/rebonds globaux avec les signatures des armes/personnages. Prévoyance ne justifie pas la suppression du soin, Débordement ne supprime pas les dégâts, Propagation ne distribue pas le contrôle à un arsenal qui n'en produit pas. Finir ce catalogue conjoint avant une migration définitive en production.

## 5. Niveaux d'armes : 70 proposé, 99 comparé

**Recommandation de départ : armes au niveau maximum 70, courbe joueur actuelle conservée pour le premier essai complet B.** Le niveau 70 n'est pas validé par Raphaël ; il a proposé de comparer 70, 99 et la courbe. Le choix vise un arsenal qui se construit longtemps, puis une phase de surplus avant la fin d'une excellente run. La cible de 300–400 niveaux en 45 min reste une cible et ne prouve pas que B l'atteindra.

Un personnage commence niveau joueur 1 avec une arme niveau 1. Pour quatre armes et quatre perks : `3 acquisitions + 4 × (max_arme − 1) + 4 perks = 4 × max_arme + 3 choix`.

| Maximum par arme | Améliorations par arme | Choix théoriques | Niveau joueur au build complet | XP cumulée, courbe actuelle arrondie |
|---|---:|---:|---:|---:|
| 50 actuel | 49 | 203 | 204 | 540 138 |
| **70 proposé** | **69** | **283** | **284** | **780 138** |
| 99 | 98 | 399 | 400 | 1 128 138 |

Calculs hors améliorations du monde, niveaux passés, échanges d'armes et indisponibilités. Les Mémoriaux/butins peuvent remplir l'arsenal plus tôt ; un échange peut rouvrir des choix. Le surplus se déduit des améliorations réellement possibles, jamais du niveau joueur 284 ou 400.

**70 :** 80 améliorations de plus sur l'arsenal par rapport à 50, soit +40,8 % d'améliorations par arme. Une excellente trajectoire allant de niveau 190 à 30 min à 300 à 40 min pourrait terminer dans cette période, hors gains du monde. C'est une lecture des cibles, pas une mesure ni une promesse de minute précise.

**99 :** 196 améliorations supplémentaires sur l'arsenal par rapport à 50, exactement deux fois les améliorations par arme. Le budget couvre presque intégralement la cible d'une excellente run à 45 min. Pertinent si l'on veut des décisions jusqu'au bout et un arsenal rarement complet ; il laisse moins de place au surplus sur cette durée et impose davantage de sélection de cartes.

Augmenter le maximum n'est pas un réglage d'affichage : les gains ordinaires, rares et les paliers entiers (projectiles, orbites, rebonds…) changent la puissance et le coût du combat. Ne pas doubler les choix puis diviser tous les gains par deux pour conserver artificiellement le même résultat. Vérifier le temps pour tuer, la couverture et les coûts réels ; aucun pourcentage universel ne décrit toutes les armes.

Le tirage courant ne retire pas toutes les stats déjà plafonnées (arc à 360°, cône à 180°). Corriger l'éligibilité des gains avant d'étendre le plafond : une amélioration doit produire un effet réel. Si une arme ne possède plus de gain possible, son niveau nominal ne suffit pas à la rendre améliorable.

Les centaines de choix ont aussi un coût de lecture. À titre arithmétique, 80 décisions supplémentaires à une seconde chacune représentent 80 s d'écran ; 196 représentent 196 s. Ce n'est pas une durée utilisateur mesurée. La réserve R1-G regroupe les décisions, elle ne supprime pas ce coût. Ne pas la transformer ici en montée automatique ou banque manuelle : Raphaël a demandé des cascades de choix enchaînés.

### Courbe joueur : ne pas confondre quantité de contenu et rythme

Le JSON courant utilise `20 × niveau^1,35`, avec majoration des cinq premiers niveaux et plafond à **3 000 XP par niveau dès environ 41**. Allonger cette courbe ne crée aucun nouveau choix : cela retarde les mêmes choix. B déplace l'XP des anciens perks chiffrés vers les objets, Délestage et la collecte ; il faut d'abord mesurer ce nouveau revenu.

Premier essai : garder les paramètres actuels et mesurer B/70. Ensuite, si la trajectoire excellente dépasse ou manque nettement la cible, ajuster les sources de récompenses et/ou la courbe tardive avec les mêmes seeds. Préserver les cibles de départ et les vagues de boss/Résurgences. Éviter de ralentir tout le début pour corriger seulement la saturation après trente minutes.

Si 70 donne trop tôt un arsenal complet malgré une cadence plaisante, 99 devient le candidat suivant. Si 99 maintient surtout des choix répétitifs et coûteux en temps, il ne résout pas le problème. Si l'XP n'atteint pas la cible avec les nouveaux objets, monter le plafond d'armes ne peut pas y remédier.

**Comparaison mathématique d'une courbe retravaillée**, exécutée avec `Curve` et `level_after` de `tools/progression_model.py`, en conservant artificiellement **1 068 138 XP reçue** (volume menant au niveau 380 dans la courbe actuelle) et les autres paramètres :

| Coût maximum d'un niveau | Niveau atteint avec ce même volume d'XP |
|---|---:|
| 3 000 actuel | 380 |
| 3 500 | 331 |
| 4 000 | 296 |
| 4 200 | 284 |

Le coût maximal modifie aussi le niveau à partir duquel la courbe plafonne. Cette comparaison ne simule ni puissance, ni morts d'ennemis, ni butin, ni bonus de niveaux garantis, ni tarif spécifique du surplus : dans une vraie run, tous ces éléments changent le revenu et le niveau final. Elle montre qu'un couple **70 / courbe visant environ 300 niveaux** est une autre direction cohérente à essayer si l'on souhaite moins de choix, mais ce n'est pas une nouvelle cible validée. La première comparaison recommandée reste B/70 avec la courbe actuelle ; tester ensuite 3 500 ou 4 000 selon le problème mesuré.

## 6. Vérification du modèle de progression

Sources lues : `data/weapons/weapon_upgrades.json`, `data/scaling/progression.json`, `tools/progression_model.py`, `scripts/Progression/PlayerProgression.cs`, `scripts/Spawn/SpawnManager.cs`, `scripts/Progression/UpgradeRoller.cs`, `scripts/Combat/WeaponInstance.cs` ; modèles et mesures historiques du plan 20.

Le modèle lit la vraie courbe, mais son scénario excellent contient un **bonus de build XP de +60 % à 20 min**. Ce n'est pas une propriété du catalogue B ni une mesure d'objets déjà acquis. Recalculer l'hypothèse à partir du contenu effectivement proposé, des quantités de butin et des conversions.

Constat avant B0, corrigé au §10 : `simulate()` appliquait les leviers de source R1-A à l'XP de sa mesure de base ; le code courant `SpawnManager.ComputeXpMultiplier` en applique déjà dans les nouvelles captures. Une mesure post-R1-A ne peut pas être réinjectée telle quelle puis recevoir ces mêmes multiplicateurs une seconde fois. Identifier la version des mesures, distinguer scénario contrefactuel et revenu réellement observé, puis seulement annoncer une trajectoire simulée de B.

Les budgets ci-dessus sont de l'arithmétique de contenu et de courbe ; aucune nouvelle run B n'a été mesurée. Les anciens chiffres 358/380 ne sont pas des résultats de cette proposition.

## 7. Découverte technique et contrats à construire

Découverte avant B0, conservée pour comprendre les dépendances. Les APIs de dégâts, soins, statuts et événements ont depuis été complétées : voir le compte rendu §11. Les points acquisition/récompenses restent à intégrer dans les lots suivants.

| Existant vérifié | À utiliser / compléter | Garde-fou |
|---|---|---|
| `FragmentManager.SelectFragment`, `EnsureSurvivalChoice`, `Reroll`, `BanishFragment` | File commune, paliers, droits de slots et exclusions | Ne pas réintroduire dix niveaux de perk ; ne pas vider les offres via bannissement |
| `UpgradeRoller.RollGains`, `FragmentOption`, `WeaponInstance.CanLevelUp` | Carte concrète reportée et revalidation | Ne pas relancer ses gains ; une place totale sur trois, pas une quatrième carte gratuite |
| `LootRewards.Resolve` / `Apply` | Habitude/Délestage avant crédit | Résolution unique, transaction unique ; pas de popup supplémentaire sur loot automatique |
| `Player.OnAttackHit(Enemy, float, bool, bool, WeaponInstance, int, bool)` | Source et nombre d'impacts déjà transmis | Un booléen de ricochet ne suffit pas à distinguer tous les dommages secondaires |
| `Enemy.TakeDamage` retourne `void` ; `EnemyKilled(string, Vector2)` | Résultat de dégâts avec PV avant, dégâts natifs, report, source et identité d'attaque à concevoir | Ne pas déduire l'excédent du bilan ou d'un DPS affiché ; un crédit par vie d'ennemi |
| `Enemy.ApplySlow` / `ApplyDisorient` | Durée restante, source native/transmise et règles de fusion | Pas d'API de stun supposée ; la désorientation actuelle peut écraser sa durée, adapter explicitement la fusion |
| `Player.Heal`, `ApplyRegen`, `PlayerDefense.Outcome`, `TakeErasureDamage` | Résultat de soin, dommage réel et origine | `PlayerDamaged` sert aussi aux soins/UI ; jamais le traiter comme preuve de blessure |
| `XpOrb._collected`, `IsAsleep`, `SleepToken` | Requêtes spatiales bornées pour Sillage, réveil/collecte corrects | Aucun scan global par frame, aucun double crédit ni accès après recyclage |
| Cibles du pool d'ennemis | Identification par vie et catégorie d'ennemi à exposer | Un nœud réutilisé n'est pas la même cible ; aucun test sur nom affiché |

Ordre des soins/récupérations : résoudre bouclier/armure → perte réelle et mort éventuelle → Prévoyance si vivant et source admissible → calcul du budget Reprise sur la perte restante. Les soins normaux sont ensuite ventilés PV/excédent et réduisent le budget Reprise à hauteur des PV réellement restaurés. Une restitution de perk est identifiée comme telle et ne réentre pas dans le pipeline de proc.

État borné : au plus quatre réserves Débordement, un budget et une échéance Reprise, une réserve Prévoyance, une carte reportée, un trajet de durée bornée. Les contrôles transmettent leur provenance avec le statut existant. Aucun nœud/abonnement par objet empilé ; aucune création d'effet fréquent hors pool. Préserver l'attribution par joueur pour une future coopération.

Les Failles perdent les améliorations de perks ; si aucun gain d'arme n'est admissible, prévoir un autre gain réel conformément au plan de butin ou rendre l'offre indisponible sans coût. Ne jamais payer un Oubli contre une récompense impossible. Même contrôle pour tous les lieux offrant actuellement des passifs.

## 8. Lots proposés après arbitrage du catalogue

Raphaël valide le catalogue V1 et demande de commencer l’implémentation. **B0 est livré et vérifié en premier**, puis chaque lot se termine et se vérifie avant le suivant ; les anciens lots du catalogue à seize perks ne sont plus la séquence de B. Le plafond 70 demeure une proposition : aucune modification de niveau maximal ou de courbe dans B0.

| Lot | Travail concret et références | Validation |
|---|---|---|
| B0 — Données et contrats, migration préparée | Charger les neuf règles sans les activer ; définir les contrats de dégâts/soins/statuts ; corriger la provenance du modèle XP ; documenter les destinations et dépendances des anciens passifs (§9) | Contrats testés, catalogue extensible, arithmétique 50/70/99 reproductible, pas de double multiplicateur XP. La migration effective des objets reste B4 ; B0 ne la déclare pas terminée |
| B1 — Acquisition | Reprendre la file `FragmentManager`, les données et le contrat `FragmentOption` pour quatre perks uniques, neuf définitions et éligibilité ; même écran et réserve automatique | Choix isolés/cascades/passés/bannis ; moins de trois candidats ; changements d'armes ; droits et profils conservés ; aucune carte annonçant un effet encore inactif en run normale |
| B2 — Effets de combat et survie | Contrats §7, puis Convergence/Débordement/Propagation/Prévoyance/Reprise et feedback | Surkills multi-impacts, attributs des morts, pools, contrôles transmis, coups fatals, Néant, bouclier et soins croisés ; build et régressions ciblées |
| B3 — Collecte et récompenses | Sillage via collecte existante ; cartes concrètes pour Seconde lecture ; résolution avant application pour Délestage/Habitude | Orbes endormies/recyclées, expiration trajet, loot unique, relance/réouverture, éligibilité d'une carte après amélioration du monde ; captures en run |
| B4 — Catalogue entier et puissance | Catalogue d'objets révisé branché, remplacement de l'ancien circuit, UI/Collection/bilan ; essayer plafond 70 et comparer selon §5–6 | Runs mêmes seeds et mesure XP/temps pour tuer/objets/interruptions ; banc dense machine calme ; absence de coût par pile ; critères humains ci-dessous |

Les lots intermédiaires restent sous contrôle dev tant que les effets annoncés ne sont pas livrés. Vérification de fin : `dotnet build` sans warning ; smoke si scènes/initialisation concernées ; régressions utiles ; captures pour changements visibles ; cases de roadmap uniquement pour ce qui est réellement implémenté et vérifié.

**Recette humaine :** reconnaître l'effet sans ouvrir la pause ; pouvoir raconter un choix différent grâce à lui ; trouver une raison de préférer chacun des deux défensifs ; vérifier que Habitude/Délestage apportent une décision sans alourdir chaque ramassage ; éprouver Seconde lecture dans une vraie cascade. Mesurer aussi les perks proposés/choisis/inactifs, pas seulement les dégâts produits. Un candidat systématiquement inerte doit être revu, même si sa fiche semble originale.

**État courant :** catalogue V1 de neuf règles validé et ouvert aux ajouts futurs ; réglages initiaux à mesurer. B0 livre les données et contrats vérifiés, sans activer les nouveaux perks en run. Plafond 70 recommandé mais non arbitré ; courbe inchangée.


### 8.1 Découpage de B0 engagé — 28 septembre 2026

1. **Données et lecture** : neuf définitions indépendantes dans `data/progression/perk_specializations.json`, paramètres d’acquisition et coefficients déclaratifs, chargement par `Godot.Json`. Aucun ajout au pool normal tant que les effets correspondants ne fonctionnent pas. Le nombre de définitions n’est pas fixé dans le code.
2. **Contrats** : résultats explicites de dégâts et soins, identité d’attaque/vie/source et provenance des contrôles ; préserver le comportement actuel et l’attribution du bilan. Tests ciblés sur les données réellement nécessaires aux effets.
3. **Modèle XP** : exiger une provenance de mesure pré/post R1-A avant simulation ; une mesure post n’est pas multipliée une seconde fois par les sources déjà présentes. Les mesures inconnues restent affichables sans projection.
4. **Migration préparée** : documenter les quatorze passifs de statistiques et leurs destinations dans les objets, sans retirer leurs sources jouables avant remplacement. Les objets restent un lot conjoint nécessaire à la migration complète, sans bloquer la construction des contrats indépendants.
5. **Validation de B0** : build sans avertissement, tests ciblés de contrats/chargement/provenance XP ; smoke si branchement dans l’initialisation. Ni activation de neuf cartes sans effets, ni changement de plafond d’armes, ni suppression d’anciens Dons.

L’état de validation est consigné au §11. B1/B2/B3/B4 ne sont pas livrés par ce socle.

### 8.2 Découpage de B1 engagé — 29 septembre 2026

1. **Activation par effet** : un registre côté code liste les effets réellement branchés. En B1 il est vide : aucune carte de perk n’apparaît en run normale, le calendrier reporte simplement son droit. Un aperçu réservé aux bancs et captures (compilé seulement avec `TOOLS`) permet d’éprouver l’acquisition sans prétendre à un effet. Chaque lot suivant active ses effets au fur et à mesure.
2. **État de run** : les perks équipés vivent sur le joueur (ordre d’acquisition, aucun doublon, quatre au plus), un signal `EventBus` annonce chaque acquisition pour les effets et l’interface futurs.
3. **Éligibilité** : traits d’arme dérivés des données existantes (recherche de cible hors onde/orbite, impacts directs, ralentissement/désorientation natifs, armes encore améliorables). Conditions d’objets fausses tant que B4 n’existe pas.
4. **Calendrier dans la file existante** : droits gagnés aux paliers 2/6/12/20, un droit servi au plus par niveau, report au niveau suivant après passage ou sans candidat, place conservée dans une cascade. Composition de la première offre en données (survie, combat, collecte/récompenses). Relance et bannissement communs ; bannissement refusé s’il empêcherait de remplir les emplacements restants.
5. **Interface** : carte de perk distincte (famille, phrase d’effet), titre indiquant l’emplacement servi, liste des perks dans la pause.
6. **Validation** : nouveau banc de régression d’acquisition, capture d’une cascade en aperçu, régressions armes/contrats, smoke. Hors B1 : état « inactif » d’un perk après changement d’arme et avertissement avant échange, qui n’ont de sens qu’avec des effets actifs (B2).


### 8.3 Découpage de B2 engagé — 29 septembre 2026

Chaque étape active son effet dans `PerkSpecializationEffects` seulement une fois vérifiée ; sa carte apparaît alors en run normale. Un seul composant par joueur porte l'état des perks et ordonne les réactions (Prévoyance avant Reprise sur une même blessure), abonné aux résultats B0 de l'`EventBus` et sans travail par frame hors fenêtre ou réserve active.

1. **Survie — Prévoyance et Reprise** : réserve remplie à l'acquisition puis par les excédents de soin/régénération, restitution après un coup non fatal ; fenêtre de Reprise, budget plafonné, crédit par élimination attribuée, soins extérieurs déduits. Retours : réserve en liseré sous la barre de PV, part récupérable sur la barre avec l'expiration visible.
2. **Débordement** : réserve par arme alimentée par l'excédent natif d'un coup fatal direct, plafonnée à la référence de l'attaque qui charge, consommée par le premier impact direct d'un lancement ultérieur, durée 3 s, perdue si l'arme quitte l'inventaire. Retour : case d'arme marquée tant que la réserve est prête, éclat à la consommation.
3. **Convergence** : les recherches de cible des armes qui visent préfèrent élites et Souverains à portée, en gardant la cible prioritaire déjà suivie. Retour : repère discret sur la cible suivie.
4. **Propagation** : à une élimination attribuée, les ralentissements/désorientations natifs encore actifs passent au plus proche voisin vivant à 120 px, sans prolongation ni affaiblissement d'un contrôle plus fort. Retour : bref lien entre la victime et le receveur.
5. **État inactif et pause** : un perk sans arme support est signalé inactif dans la pause, qui affiche aussi réserves et budgets ; l'échange d'arme au sol prévient avant de rendre un perk inactif.

Validation : banc de régression par effet sur le vrai joueur et les vrais ennemis (cas limites des fiches §3), captures en run pour les retours visuels, banc dense avant/après pour vérifier l'absence de coût mesurable, smoke.

## 9. Migration conjointe des statistiques — préparation B0

Le fichier `data/progression/passive_souvenirs.json` contient **14 définitions**, dont Fragment d’Éternité désactivé, donc 13 dans le pool actif. Elles sont distinctes des anciens Dons de `data/perks/perks.json`. Les noms ci-dessous désignent les IDs existants, pas un catalogue de nouveaux objets implicitement adopté.

| Ancien passif | Stat effective | Destination proposée dans les objets / dépendance |
|---|---|---|
| `flamme_interieure` | `damage` | Base existante `damage_up` ; bonus de dégâts global, sans nouvelle forme d’attaque |
| `memoire_vive` | `attack_speed` | `attack_speed_up` ; cadence globale, plafonds physiques et coûts à mesurer |
| `ancrage` | `max_hp` | `hp_up` ; gain additif de capacité, aucun déclenchement artificiel de soin excédentaire |
| `instinct` | `speed` | `speed_up` ; vitesse de déplacement, à agréger sans multiplication exponentielle des piles |
| `resonance` | `aoe_radius` | `aoe_up` ; taille d’impact séparée de l’allonge, cohérence visuelle obligatoire |
| `siphon_essence` | `xp_magnet_radius` | `xp_magnet` ; corriger la description historique parlant de drop d’Essence : la stat réelle est l’attraction d’XP |
| `peau_dure` | `armor` | `armor_up` ; armure additive, conserver le calcul de défense commun |
| `oeil_critique` | `crit_chance` | `crit_chance` existe comme Don complexe ; conserver une source explicite de chance critique, distincte de `crit_damage` |
| `regeneration` | `regen_rate` | `regen_up` ; soin par seconde ; le futur calcul d’excédent doit fonctionner à PV pleins |
| `portee_etendue` | `attack_range` | `range_up` ; allonge/portée sans modifier le motif signature de l’arme |
| `souffle_du_neant` | `projectile_count` | `extra_projectile` existe ; migration à régler par paliers et compatibilité d’arme, pas +1 projectile illimité par pile sans mesure |
| `fragment_deternite` (désactivé) | `cooldown_reduction` | Préserver l’ID et les droits historiques, garder hors offres ; aucun rétablissement d’un effet non branché ni doublon implicite de cadence |
| `reflet_brise` | `projectile_pierce` | `piercing_shot` / `traqueur_piercing` existent ; préserver la source jusqu’à décision sur le perçage global, en protégeant l’intérêt de la Cloueuse |
| `carapace` | `shield` | Pas d’objet générique de bouclier équivalent dans les Dons actuels ; source d’objet à spécifier et brancher avant retrait du passif |

**Compléments nécessaires, au-delà des 14 passifs :** `lucky` fournit déjà de la Chance, et `crit_damage` les dégâts critiques. Aucun des quatorze passifs n’accorde de multiplicateur général d’XP ; ni `xp_magnet_radius` ni `essence_drop_multiplier` n’en sont un. L’objet d’XP et son point d’application unique restent à définir et implémenter conjointement avec Délestage. Ne pas annoncer un build +60 % XP comme déjà disponible. L’axe oubli reste une dépendance au catalogue révisé, sans réintroduire Traversée.

**Transition minimale à préparer pour B4 :** conserver les effets génériques existants comme candidats d’objets, leur donner les nouvelles données d’inventaire et les mêmes droits débloqués, puis retirer les passifs de niveau uniquement une fois leurs sources remplacées et accessibles. Les anciens Dons complexes et de personnage ne sont ni supprimés ni renommés globalement en B0. Les noms, raretés et coefficients finaux des objets restent à discuter dans ce catalogue conjoint ; l’ancienne proposition de 24 objets n’est pas adoptée par l’accord sur les neuf perks.

Enlever `MaxStacks` dans `PerkManager` ne suffit pas : les multiplicateurs répétés deviendraient exponentiels, certains effets sont enregistrés à chaque acquisition, d’autres écrasent un état unique (seconde chance), et le butin `perk` résout actuellement une unité sans modèle d’objet. Prévoir un compteur par ID, une agrégation par stat/effet et une quantité explicite dans la récompense ; aucun abonnement, nœud ni traitement par frame et par exemplaire. Les pourcentages globaux peuvent s’additionner dans une même catégorie avant application ; les probabilités et paliers discrets demandent chacun une règle dédiée. Ces règles seront chiffrées au lot objets, pas devinées dans B0.

**Délestage et Habitude restent dépendants des objets :** `item_choice_rewards` sera faux tant que les objets, les récompenses admissibles et leur transaction unique ne sont pas branchés. `owned_item` exige en plus une possession effective. Le loader décrit ces conditions sans les décider. Leurs offres ne doivent pas être activées pour remplir artificiellement quatre slots avec des cartes sans effet.

## 10. API et vérification du socle de données

`PerkSpecializationDataLoader` lit `data/progression/perk_specializations.json` avec `Godot.Json` ; `Load()` indique la réussite, `Get(id)` et `GetAll()` donnent des définitions en lecture seule, `Config` porte les emplacements/taille d’offre/paliers. `TryParse` permet la validation sans toucher au cache. Une erreur interdit la publication partielle. `Effect` est un identifiant stable indépendant du nom traduit ; `Eligibility` décrit les conditions et `Parameters` les coefficients. Le nombre de définitions ne pilote pas le nombre d’emplacements. Ajouter un contenu n’exige pas de changer une constante neuf ; un nouvel effet exige cependant son implémentation et sa validation avant toute offre.

Le modèle Python exige maintenant `xp_provenance="pre-r1-a"` ou `"post-r1-a"` pour `simulate`. En CLI, `--xp-provenance` vaut pour l’ensemble des dossiers de l’appel : ne pas mélanger les versions. Les commandes historiques sans option affichent les mesures et les courbes, mais n’inventent plus la provenance nécessaire à une projection. Les agrégats post-R1-A gardent leurs sources observées ; temps/oubli/Résurgence ne sont pas appliqués deux fois. Les variations PLAY, Péril et bonus de boss restent des scénarios explicitement hypothétiques ; ne pas ajouter un bonus garanti déjà inclus dans la mesure. Cette correction ne constitue aucune nouvelle mesure de B.

Vérification exécutée : `python3 -m unittest discover -s tools/tests -p test_progression_model.py -v` — six tests verts (sources pré/post, provenance manquante, données vides/désalignées, ancienne CLI, budgets 50/70/99). `dotnet build --no-restore` : zéro avertissement, zéro erreur. Le banc Godot des contrats et du loader est consigné au §11.


## 11. Compte rendu B0 — socle livré et vérifié le 28 septembre 2026

Le catalogue V1 et ses conditions sont chargés indépendamment des anciens Dons. Les neuf effets ne sont pas encore proposés en run. Le socle suivant est branché sur les attaques, blessures et soins existants ; il conserve les signaux et l’attribution du bilan historiques.

| API livrée | Données disponibles et usage futur |
|---|---|
| `AttackContext`, `Player.BeginAttack` | Joueur propriétaire, instance d’arme, identifiant de lancement, origine du dommage et référence native. Salve/arc/chaîne partagent un lancement ; un cône entier aussi, chaque contact orbital constitue une attaque autonome. Répliques et DOT gardent leur lancement d’origine. |
| `EnemyLife`, `Enemy.Life`, `IsPriorityTarget` | Identité de nœud et génération renouvelée à l’initialisation ; catégorie prioritaire sans dépendance au nom affiché. Prépare les cibles persistantes et le contrôle du recyclage. |
| `Enemy.TakeDamage`, `DamageResult` | PV avant impact, dégâts natifs/reportés après défense, perte effective, fatalité et application réelle ; excédent natif distinct. Le report qui achève seul une cible ne fabrique pas de surkill natif. |
| `ControlState`, `SlowControl`, `DisorientationControl`, `EnemyKillResult` | Source et durée propres de chaque contribution native admissible. Un contrôle propagé ne prête ni sa puissance ni sa durée à une contribution native ; il ne devient pas transmissible à son tour. L’instantané fatal utilise les contrôles déjà présents avant `OnAttackHit`, sans application rétroactive. |
| `HealingResult`, `Player.Heal(amount, HealingKind)` | PV réellement restaurés et excédent, origine normale/régénération/restitution/résurrection. La régénération à PV pleins expose l’excédent sans ajouter de signal UI historique. |
| `PlayerDamageResult`, `Player.TakeDamage`, `TakeErasureDamage` | Perte réelle, absorption et fatalité avant une éventuelle résurrection ; une blessure récupérable exclut Néant, absorption et coup fatal. |
| Événements C# d’`EventBus` | `EnemyDamageResolved`, `EnemyKillResolved`, `PlayerHealingResolved`, `PlayerDamageResolved` transmettent ces structures sans convertir chaque résultat en `Variant`. Les signaux historiques restent disponibles. |

La provenance passe par projectiles, ricochets, mêlée, orbites, cône, chaînes, saignement, embrasement, échos, formes, feu, épines et exécution. Les explosions ennemies ne sont pas attribuées au joueur. Les ricochets issus d’un effet passif ne deviennent pas des applications natives de contrôle.

**Limites de livraison :** B2 construira les réserves, restitutions, priorités de ciblage actives, transmissions et feedback à partir de ces résultats. B1 reste responsable des offres et droits d’acquisition ; B3 des collectes et récompenses ; B4 des objets et de la migration des sources. Il n’existe pas encore de nouvelle run B mesurable. Les plafonds d’armes et la courbe d’XP n’ont pas changé dans B0.

Vérifications déjà exécutées sur ce socle :

- `dotnet build --no-restore` : zéro avertissement, zéro erreur.
- `tools/test_perk_contracts.sh` : 32 assertions, zéro échec ; catalogue extensible, données invalides, vrais impacts et morts, recyclage, provenance des contrôles, soins/excédents et blessures.
- `tools/test_weapons.sh` : 13 assertions, zéro échec.
- `tools/test_enemy_abilities.sh` : 55 assertions, zéro échec.
- `tools/smoke_test.sh 600` : boot headless de 600 frames avec profil isolé réussi, aucune erreur inattendue.
- `python3 -m unittest discover -s tools/tests -p test_progression_model.py -v` : six tests verts.

`git diff --check` vert ; tous les nouveaux scripts C# ont leur UID. La case B0 est cochée dans la roadmap V2, aucune autre case de cette verticale. Ce socle n’introduit pas de nouveau rendu de perks : la capture de recette en run accompagne les futurs effets et leur interface. Aucune affirmation de performance ou d’équilibrage n’est déduite de ces régressions.


## 12. Compte rendu B1 — acquisition livrée et vérifiée le 29 septembre 2026

L'acquisition des quatre perks est branchée dans la file de niveaux existante. **Elle reste en sommeil en run normale** : aucun effet n'étant encore actif, aucun perk n'est proposable et chaque droit se reporte sans rien changer au jeu. B2 activera ses effets un à un dans `PerkSpecializationEffects` ; les cartes correspondantes apparaîtront alors d'elles-mêmes.

| Élément livré | Comportement |
|---|---|
| `PerkSpecializationEffects` | Liste des effets branchés, vide en B1. `PreviewInactive` (compilé seulement avec `TOOLS`, activé par `--preview-perks` dans les captures et par le banc) propose aussi les effets non branchés ; leurs cartes portent alors « Aperçu : effet pas encore branché ». |
| `Player.Specializations`, `AcquireSpecialization` | Perks de la run dans l'ordre d'acquisition, sans doublon, quatre au plus ; signal `EventBus.SpecializationAcquired`. |
| `WeaponTraits` | Recherche de cible (ni onde, ni orbite, ni cône orienté par le regard), impacts directs, ralentissement/désorientation natifs (Cloche, Polaroïd, Chronomètre). |
| `PerkSpecializationOffers` | Droits aux paliers 2/6/12/20 ; un droit servi au plus par niveau ; passé ou sans candidat, il revient au niveau suivant. Première offre composée d'après `first_offer_families` (survie, combat, collecte/récompenses), défensif tiré uniformément, ordre mélangé. Seconde lecture exige deux armes améliorables non bannies. Délestage et Habitude restent exclus tant que les objets à choix n'existent pas (B4). |
| `FragmentManager` | Au niveau dû, l'offre de perks remplace le choix ordinaire et garde sa place dans une cascade ; la relance et le bannissement communs s'appliquent. Un bannissement de perk est refusé, sans être consommé, s'il empêcherait de remplir les emplacements restants avec le catalogue, ou s'il viderait l'offre affichée. |
| Interface | Carte à cadre parchemin, bandeau « PERK · famille », règle en une phrase ; titre « PERK · EMPLACEMENT n/4 » avec le compteur de cascade ; section « Perks » dans la pause. |

Vérifications :

- `dotnet build` : zéro avertissement, zéro erreur.
- `tools/test_perk_acquisition.sh` (nouveau) : 21 assertions (chiffre corrigé le 29 septembre, 24 annoncé par erreur), zéro échec. Run normale sans aucune carte de perk sur les niveaux 2 à 30 ; droit reporté puis servi au niveau suivant ; soixante premières offres toutes composées survie/combat/collecte, Prévoyance et Reprise vues toutes deux ; relance consommée ; paliers 2/6/12/20 ; passage reporté ; cascade 11→13 ; offres de 3, 2 puis 1 carte après bannissements et dernier bannissement refusé ; limite du catalogue ; éligibilité selon l'arsenal (arc, Cloche, arme bannie) ; cinquième perk et doublon refusés.
- `tools/test_perk_contracts.sh`, `tools/test_weapons.sh`, `tools/test_enemy_abilities.sh` : zéro échec.
- `tools/smoke_test.sh 600` : vert.
- Capture `CAPTURE_EXTRA_ARGS="--capture-cascade --preview-perks"` : cascade de cinq niveaux, perks aux niveaux 2 et 6 dans le même écran ouvert, règles de la réserve toutes PASS ; cartes et titre lisibles à 1080p.

**Reporté à B2 :** état « inactif » d'un perk dont l'arme support a été échangée, avertissement avant l'échange au sol, chiffres utiles dans la pause. Ils n'ont de sens qu'avec des effets actifs. Aucune règle de profil ou de déblocage n'a changé : les neuf perks sont accessibles d'emblée, comme prévu au §1.


## 13. Compte rendu B2 — effets de combat et de survie livrés le 29 septembre 2026

Les cinq perks de combat et de survie sont actifs en run normale. Ils sont proposés aux paliers 2/6/12/20 selon l'arsenal. Sillage, Délestage, Seconde lecture et Habitude restent hors des offres (B3/B4) : une run compte donc au plus cinq candidats, et la première offre ne peut pas encore présenter de perk de collecte.

| Perk | Comportement livré | Retour visuel |
|---|---|---|
| Prévoyance | Réserve de 20 % des PV max, pleine à l'acquisition, remplie par les seuls excédents de soin et de régénération (y compris à PV pleins). Restitution après un coup non fatal, au plus la perte et le stock ; ni Néant, ni coup fatal, ni recharge par une restitution. Tronquée pour de bon si les PV max baissent. | Liseré doré au bas de la barre de PV |
| Reprise | Fenêtre de 4 s après un coup non fatal ; 40 % de la perte restant après Prévoyance deviennent récupérables, plafonnés à 20 % des PV max. Chaque élimination attribuée rend jusqu'à 2 % des PV max. Un second coup n'allonge pas l'échéance ; les soins extérieurs réduisent le budget. | Part récupérable en clair après les PV, qui pâlit avec la fenêtre et bat dans sa dernière seconde et demie |
| Débordement | La moitié de l'excédent natif d'un coup direct fatal va en réserve pour l'arme, plafonnée à la référence de l'attaque qui charge, pendant 3 s. Le premier impact direct d'un lancement ultérieur de cette arme l'emporte. Pas de charge par un report seul, un DOT ou un coup secondaire ; réserve perdue si l'arme quitte l'inventaire, effacée avec le joueur. | Case d'arme cernée de bleu pâle ; chiffre de dégâts « »N » bleu pâle au coup renforcé |
| Convergence | Les recherches de cible des armes qui visent (projectiles, arcs, chaîne) placent en tête l'élite ou le Souverain à portée déjà suivi, sinon le plus proche. Portée et motif inchangés ; onde, orbite et cône non concernés. | Quatre coins dorés au sol autour de la cible suivie |
| Propagation | À une élimination attribuée, le ralentissement et la désorientation natifs encore actifs passent au plus proche voisin vivant à 120 px, avec intensité et durée restante. Pas de retransmission, et un contrôle plus fort déjà présent n'est pas affaibli. | Trait pâle de la victime au receveur |

S'y ajoutent l'état **inactif** d'un perk sans arme support (pause) et l'avertissement de l'invite d'échange au sol (« Échanger : X · rend inactif : Convergence »). La pause affiche pour chaque perk sa règle et son état du moment : réserve, part récupérable et temps restant, réserves prêtes par arme, élite suivie.

Architecture : un composant `SpecializationRuntime` par joueur, créé au premier perk, écoute les résultats B0 de l'`EventBus` et ordonne les réactions. Seul Débordement lit chaque impact, et seulement s'il est acquis. Le reste du temps, un impact paie une lecture de liste vide (`OverflowLedger.Take`) et une recherche de cible un test de nullité. Le composant ne tourne par frame que pendant une fenêtre ou une réserve. Les jauges passent par `EventBus.SpecializationGaugeChanged`.

Vérifications :

- `dotnet build` : zéro avertissement, zéro erreur.
- `tools/test_perk_effects.sh` (nouveau) : 50 assertions, zéro échec, sur le vrai joueur et de vrais ennemis. Il couvre les cas limites des fiches listés ci-dessus, ainsi que l'échange d'arme et l'état inactif.
- `tools/test_perk_acquisition.sh` : 21 assertions, adapté aux effets désormais actifs (seuls des perks branchés sont proposés ; report sans candidat vérifié sans arme).
- Contrats (32), armes, capacités ennemies, déplacements : zéro échec ; `tools/smoke_test.sh 600` vert.
- Captures `CAPTURE_EXTRA_ARGS="--capture-perks --perk-scene <survival|overflow|priority|carry|status>"`, regardées : liseré et part récupérable, case d'arme et chiffre renforcé, repère sur une élite plus lointaine que les rôdeurs, trait de Propagation, pause et invite d'échange lisibles en 1080p.
- Banc dense A/B `b2420397` (B1) contre B2 : FPS médians 306,8 → 315,9 (720p) et 294,8 → 287,3 (1080p), p99 6,6 → 6,3 / 6,6 → 6,7 ms, 1 nœud créé/s des deux côtés. Écarts dans le bruit ; charge de 1,25 à 2,10 sur 2 cœurs, donc aucune conclusion fine sur les FPS. **Limite :** ce banc tourne sans perk et sans élimination ; il établit l'absence de coût pour un joueur sans perk, pas le coût des effets actifs en foule.

Relecture de code (sous-agent) sans bug bloquant. Deux points corrigés avant clôture : l'invite d'échange gardée en cache jusqu'au prochain changement d'arme ou de perk (elle était recalculée à chaque pas physique), et l'état actif d'un perk qui ignore désormais le nombre d'armes améliorables (condition d'offre seulement). Limites connues, laissées en l'état :

- Débordement sur l'orbite : chaque contact d'orbe est une attaque autonome (contrat B0), donc une réserve chargée par un contact est souvent emportée par le suivant, presque aussitôt. Le cône garde un seul lancement pour toute son émission.
- Propagation lors d'une mort groupée : le voisin choisi peut mourir dans la même frappe de zone, et le contrôle transmis est alors perdu.
- Un échange au sol qui échouerait après le retrait de l'arme ferait perdre sa réserve de Débordement ; cas presque inatteignable, les doublons étant refusés avant.

**Constat de réglage à trancher — Propagation avec la Cloche seule.** Dans la scène `carry` (Cloche seule, rôdeurs de début de partie, 15 s), sur 14 éliminations, aucune n'est faite d'un seul coup, et pourtant aucune victime n'est encore ralentie au coup fatal. L'écart entre le dernier coup et le coup fatal va de 2,1 à 3,2 s (temps réel de capture), au-delà des 2 s de ralentissement, vraisemblablement parce que le recul de 60 px sort la cible de portée. La règle fonctionne (banc, transmission contrôlée), mais la synergie « Cloche + Propagation » citée par la fiche ne se produit presque pas sans une autre arme qui achève. Pistes, non appliquées : allonger le ralentissement de la Cloche, réduire son recul, ou admettre un contrôle expiré depuis moins d'une seconde. Décision de Raphaël.


## 14. Compte rendu B3 (partie sans objets) — Sillage et Seconde lecture, 29 septembre 2026

Raphaël laisse le réglage de Propagation à l'agent et demande les perks manquants ([DECISIONS §27](DECISIONS.md)).

### Propagation : mesure sur les trois armes de contrôle, règle conservée

Scène `carry` étendue par `--perk-weapons`, 15 s de rôdeurs de début de partie, éliminations dont la victime portait un contrôle transmissible :

| Arsenal | Éliminations | Transmissibles |
|---|---:|---:|
| Cloche seule | 14 | 0 |
| Polaroïd seul | 9 | 9 |
| Chronomètre seul | 13 | 10 |
| Cloche + arc | 14 | 2 |
| Polaroïd + arc | 9 | 8 |
| Chronomètre + arc | 14 | 7 |

Le Polaroïd désoriente avec le premier projectile d'une salve et achève avec les suivants. Le Chronomètre ralentit toute une zone, donc les voisins de sa cible. Seule la Cloche échoue, à cause de son recul et de l'intervalle entre ses coups. **Décision de l'agent : Propagation reste telle quelle.** Le cas de la Cloche relève du réglage de l'arme (plan 17), pas du perk.

### Sillage (`xp_trail`)

`XpTrail` échantillonne toutes les 0,1 s (temps de jeu) le déplacement réel du joueur et garde 6 s de trajet. Un saut de plus de 200 px entre deux échantillons coupe le couloir (téléportation) ; un dash (≈ 58 px par échantillon) le trace. Ces deux valeurs sont en données. Demi-largeur du couloir : le rayon d'attraction courant des orbes (150 px × aimant). Une orbe hors d'attraction consulte le couloir toutes les 0,15 s, jamais à chaque pas physique. Une orbe endormie loin du joueur est réveillée par la ronde de `CombatPools` si elle est dans le couloir. Une orbe prise dans le Sillage rejoint le joueur jusqu'au bout, sans double crédit. Immobile, le joueur ne trace rien et le trajet expire ; la pause le fige.

Capture `--perk-scene trail` : après 3 s de marche, 8 orbes posées sur le trajet (jusqu'à ≈ 650 px derrière) et 8 à l'écart. En 3,4 s, 35 XP sur 40 rejoignent le joueur ; celles à l'écart restent au sol. L'orbe manquante n'est pas expliquée à ce stade : peut-être encore en route, ou posée juste hors du couloir par le décalage vertical de ±40 px.

### Seconde lecture (`carried_choice`)

`CarriedChoice` dans la file des niveaux :

- **Report :** après une sélection ordinaire, la plus rare des améliorations d'arme non choisies (la plus à gauche à égalité) est reportée avec ses gains figés.
- **Offre suivante :** au prochain choix ordinaire, elle prend une place marquée « REPORTÉE ». Les autres cartes sont neuves et n'améliorent pas la même arme.
- **Expiration :** la carte expire si elle n'est pas prise et n'est jamais reportée deux fois.
- **Effacement :** passer l'offre l'efface, bannir son arme l'efface, et une arme partie ou au maximum la libère.
- **Conservation :** une relance la garde, et un choix de perk ne la consomme ni ne la crée.

Capture `--perk-scene carried` : carte « ✶ ÉPIQUE · REPORTÉE » en tête de l'offre, gains conservés.

Observation hors lot : cette amélioration épique de la Cloche affiche « Zone +0 % », un gain trop petit pour l'arrondi. Cela vient du tirage des améliorations existant (plan 17), pas de ce lot.

### Délestage et Habitude : bloqués par l'absence d'objets

Tous deux agissent sur « un objet révélé dans une récompense à choix » (coffre ou Souverain avec écran). Constat du code au 29 septembre :

- **Aucun inventaire d'objets cumulables.** Le joueur n'a que ses emplacements d'armes et de Souvenirs passifs. Le seul compteur par identifiant est celui des anciens Dons (`PerkManager._activeStacks`).
- **Aucune récompense à choix d'objet.**
  - Le coffre, et donc la récompense du Souverain (un `chest_rare` garanti), passe par une roulette qui applique tout le butin tiré (`ChestLootScreen`, `LootRewards.Resolve/Apply`).
  - Un Don de coffre est tiré et appliqué d'office (`PerkManager.PickLootPerk/OnLootReceived`).
  - Les seuls écrans de choix du monde, Mémorial et Faille, proposent des bénédictions, des services et des améliorations d'arme, pas des objets.
- **Aucune transaction unique de butin :** chaque entrée est appliquée à part.

Les implémenter sur les anciens Dons reviendrait à trancher le catalogue d'objets à la place de Raphaël, alors que la fiche le réserve au catalogue révisé (§4, §9). **Rien n'est livré pour ces deux perks.** Ils restent hors des offres (`item_choice_rewards` et `owned_item` faux). Prérequis à décider pour B4 :

1. le catalogue d'objets V1 (l'ancienne proposition de 24 objets n'est pas adoptée) ;
2. la forme de la récompense à choix dans les coffres et chez les Souverains (écran, nombre d'objets révélés, rareté) ;
3. l'inventaire cumulable avec agrégation par statistique, sans coût par pile.

### Vérifications

- `dotnet build` : zéro avertissement.
- `tools/test_perk_effects.sh` : 66 assertions, zéro échec. Il couvre le couloir, la téléportation, l'expiration, une orbe endormie rappelée, une orbe hors couloir restée endormie, et les neuf cas de Seconde lecture.
- `tools/test_perk_acquisition.sh` : 21 assertions, zéro échec.
- Contrats, armes, capacités ennemies : zéro échec. `tools/smoke_test.sh 600` : vert.
- Incident de procédure : un commit (`98661434`) est passé avec un échec du banc d'acquisition, dû au banc lui-même (joueur sans arme en aperçu). Il est corrigé par `a7ea44d1` ; le jeu n'était pas en cause.

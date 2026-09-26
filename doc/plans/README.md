# VESTIGES — Dossier de plans à valider

Date : 25 septembre 2026 · Version : 0.7 · Statut : **socle de déplacement validé par Raphaël ; dash commun validé ; priorité au casting et à la refonte des sprites avant les mobilités spécifiques**.

Ce dossier transforme les retours de Raphaël en lots réalisables. Il couvre le contrôle, les sensations, la boucle, les interfaces, les builds, la progression, les ennemis, l'identité et la compétition. Les décisions acquises et les propositions restantes sont consignées dans le [registre de décisions](DECISIONS.md). Le 22 septembre, Raphaël valide explicitement les déplacements de base refaits et demande de poursuivre le plan 01. Son [compte rendu](01-deplacements.md#7-première-implémentation--21-septembre-2026) distingue mesures automatisées, validation du socle et recette complète restante. Le [compte rendu du lot D](01-deplacements.md#8-prototype-de-mobilité--22-septembre-2026) décrit le dash livré, ses vérifications et les variantes à comparer ; Raphaël valide ensuite le dash livré. Il demande de redessiner les personnages, de valider un catalogue d’au moins cinq à six identités avec de nouveaux sprites, puis seulement de reprendre le lot E. Le [mode dev](../DEV-MODE.md) permet de tester le contenu disponible sans progression préalable.

## 1. Direction commune

**Promesse : avancer dans un monde qui s'oublie, construire une puissance singulière et revenir avec une raison précise de repartir.**

Trois échelles de plaisir :
- À la seconde : mouvement précis, attaque lisible, impact, élimination et collecte satisfaisants.
- À la minute : choix de trajet et de build, risque assumé, récompense, tension puis respiration.
- D'une run à l'autre : maîtrise, nouveaux styles de jeu, fragments de lore, records.

La [Stratégie V2](../VESTIGES-STRATEGIE-V2.md) reste l'autorité gameplay/roadmap. La [Charte](../CHARTE-GRAPHIQUE.md) reste l'autorité visuelle. Les décisions explicites de Raphaël consignées dans le registre précisent ces références ; un amendement de la V2 les signale. Les nouvelles variantes proposées restent à valider. Aucun craft, cycle jour/nuit, matériau ou Foyer in-run ne revient.

## 2. Documents et décisions

| Plan | Priorité | Décision principale à valider | Dépendances |
|---|---|---|---|
| [00 — État des lieux et sources](00-etat-des-lieux.md) | Référence | Constats et limites de l'audit | Aucune |
| [01 — Déplacements](01-deplacements.md) | P0 | Socle B/C et dash D validés ; E après nouveau casting et sprites (06/08) | 03 pour calibrer la mobilité active |
| [02 — Juiciness, score et récompenses](02-juiciness-score.md) | P0 | Direction validée ; score en run, record au bilan seulement ; refonte mort | 01 pour le ressenti ; cadrage 08 |
| [03 — Boucle et rythme](03-boucle-et-rythme.md) | P0 | Direction validée ; début plus menaçant et XP moins rapide | Première passe 01/02 |
| [04 — Interfaces et Hub](04-interfaces-et-hub.md) | P1 | Navigation, sélection, typographie et affichage adaptatif | Cadrage 08 ; contrats 05/06 |
| [05 — Armes, objets et builds](05-armes-objets-builds.md) | P1 | 4 armes + 4 passifs validés ; objets distincts cumulables sans limite | Hypothèses de tempo 03 |
| [06 — Personnages, quêtes et défis](06-personnages-quetes-defis.md) | P0 casting ; P2 progression | Casting initial, récompenses, progression et défis | 05 ; présentation 04 |
| [07 — Bestiaire et rencontres](07-bestiaire-et-rencontres.md) | P1 début de run ; P2 reste | Menaces à distance originales, rôles, compositions | 01/03 ; cadrage 08 |
| [08 — Direction artistique, sprites et lore](08-direction-artistique.md) | Transversal | Référence visuelle, personnages mémorables, pipeline | Cadrage immédiat, production après validation gameplay |
| [09 — Classement hebdomadaire](09-classement-hebdomadaire.md) | Plus tard | Périodes, règles comparables et intégrité des scores | Score 02 ; équilibre 03/05/06 |
| [10 — Terrain et tiles](10-terrain-et-tiles.md) | P1 transversal | Cohérence, transitions, circulation et coût du terrain | Cadrage 08 ; déplacements 01 |
| [11 — Mécaniques originales](11-mecaniques-originales.md) | Prototypes | Sélection et critères d'abandon des innovations | 01/03/05/10 selon proposition |
| [12 — Micro-événements et variantes](12-micro-evenements.md) | P0 | Cadence, cinq événements, élites et Souverains (v1 livrée) | 03/07 |
| [13 — Butin](13-butin.md) | P0 après recette 12 | Trois formes de butin (Vestige figé, Triptyque, Pacte d'oubli), raretés, sources lisibles | Objets 05, idée B 11 |
| [14 — Anomalies du monde](14-anomalies-du-monde.md) | P1 | Anomalies rares liées au joueur et à l'oubli, jamais mortelles | 12, 13 |
| [15 — Audio](15-audio.md) | Choix A2 consignés | Six candidats retenus, révélation du coffre actuelle conservée ; 3 refus à retravailler et 102 autres besoins à rechercher | [Retours A2](../audio/lot-a2/RETOURS.md) ; dissolution complète préparée, intégration à faire |
| [16 — L'oubli rendu sensible](16-oubli-sensible.md) | P1 | Lots O1–O6 : sol qui oublie, choses qui se défont, frontière visible, coût et récompense de l'oubli | 10 (shader du sol), 02, 13, 15 |

Les numéros servent à identifier les plans, pas à imposer leur exécution intégrale dans cet ordre. Le [registre](DECISIONS.md) fait foi pour leur statut de validation.

## 3. Séquence recommandée

1. Utiliser le mode dev pour les essais. Prioriser ensemble 06 (refonte et validation d’au moins cinq à six personnages) et 08 (tous leurs sprites à refaire, échelle et style communs). Valider les identités et les nouveaux sprites en jeu avant les mobilités spécifiques du lot 01 E ; le dash commun reste la référence validée.
2. Construire une première référence avec le contenu existant et les lots A–C de 03 : contrôle, menace dès le départ, cadence XP, exploration et Résurgence. Réévaluer la difficulté après introduction de mobilité active.
3. Définir les contrats de contenu de 05 et 06, sélectionner les rôles de 07 et produire les prototypes nécessaires. Réaliser la lisibilité prioritaire de 04, puis assembler l'expérience complète décrite ci-dessous. Les écrans finaux dépendent de ces catégories.
4. Approfondir les objets cumulables, livrer les déblocages directs, la Collection et le Hub épuré. Ajouter les créatures de 07, les assets de 08 et les améliorations terrain retenues de 10. Un prototype de 11 à la fois, selon ses dépendances.
5. Tester plusieurs runs complètes, finaliser le late game/endgame et les critères de sortie.
6. Traiter 09 seulement après stabilisation des règles de score.

Un seul lot d'implémentation ouvert à la fois. Un échec de lisibilité, de performance ou de fun ramène au lot concerné avant expansion.

**Mise à jour du 26 septembre (après-midi) :** Raphaël valide l'accueil et les effets d'attaque, trouve l'ouverture « un tout petit trop peu adoucie », valide l'ordre des chantiers et délègue les autres arbitrages ([DECISIONS §7](DECISIONS.md#7-arbitrages-délégués-du-26-septembre), provisoires).
- Nouvelle machine, un Mac Apple Silicon. Steam y levait une exception à chaque frame ; les outils de test écrivaient dans le vrai profil. Les deux sont corrigés.
- Ouverture adoucie d'un cran ([03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)).
- Jonctions tramées entre biomes ([10 T2](10-terrain-et-tiles.md#lot-t2-livré--26-septembre-2026)) et sol qui oublie ([16 O1](16-oubli-sensible.md#6-arbitrages-et-lot-o1--26-septembre-2026)), par un même shader du sol.
- Suite prévue : T1 (sol refait), O3 (frontière de l'oubli), 07 §7 (perception des créatures).

**Mise à jour du 26 septembre (retours de jeu) :** début de run adouci (6 s de répit, montée sur une minute, [03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)) ; sons d'attaque ennemis branchés, provisoires ([15](15-audio.md)) ; décors découpés en tronçons, ≈ ×2 FPS en combat dense ([10 §10](10-terrain-et-tiles.md#10-investigation-performance--26-septembre-2026)). Propositions à arbitrer : composition des champs (08 P4b), tiles et jonctions (10 §9), [plan 16](16-oubli-sensible.md) sur l'oubli, perception des créatures (07 §7).

**Point d'arrêt du 26 septembre (retours de jeu) :** livrés et vérifiés (build, smoke, régressions, bancs, captures) : ouverture de run, sons d'attaque provisoires, tronçons de décors. Non fait : relecture `godot-reviewer` du diff lancée mais pas intégrée ; recette en jeu de ces trois points ; lot suivant de l'investigation perf (physique interne du moteur, 10 §10) ; aucun des lots proposés (08 P4b, 10 §9, 16, 07 §7) n'est commencé, en attente d'arbitrage.

**Mise à jour du 26 septembre :** l'écran d'accueil est refait sur demande de Raphaël (« trop classique », pas de stats mais le sprite, « surprends-moi »). Le Hub devient le camp du Foyer, vivant ; les personnages veillent autour du feu ; le menu est textuel. Compte rendu et limites : [plan 04](04-interfaces-et-hub.md#accueil-refait--26-septembre-2026). Recette attendue.

**Mise à jour du 25 septembre :**
- Audio : [retours A2](../audio/lot-a2/RETOURS.md) reçus le 26 septembre. Six candidats retenus depuis A : critique, ouverture physique du coffre, dash, dissolution B complète (1,40 s), perk A et danger A. Révélation du coffre actuelle conservée ; impacts ennemi/joueur et level-up à retravailler ; XP en attente. Le [catalogue](../audio/COUVERTURE.md) suit 113 besoins, 51 propositions sur 11 besoins et 102 autres besoins à rechercher ou arbitrer ; aucun nouveau son intégré. Sources CC0 ou CC-BY documentées. Contrôle : `python3 tools/audio/build_catalogue.py --check` ; pages historiques [A](../audio/lot-a/index.html) et [A2](../audio/lot-a2/index.html) préservées.
- Recette de Raphaël : cadence des micro-événements validée en l'état, élites bien dosées, micro-événements appréciés, HUD « bien mieux ». Le soin, peut-être trop rare, est noté pour les plans 13 et 03.
- Nouveau chantier prioritaire : **visuel et juiciness de tout le jeu**.
  - Régression des biomes : cause trouvée dans l'historique (14 mars), mesurée, corrigée ([10 §6](10-terrain-et-tiles.md#6-régression-un-seul-biome-autour-du-départ--25-septembre-2026)).
  - Audit des décors ([10 §7](10-terrain-et-tiles.md#7-audit-des-décors--25-septembre-2026)) : pas de sprite manquant, mais des petits décors camouflés qui bloquent, des collisions décalées devant les décors, des immeubles traversables et aucun tri en profondeur. Lots D1–D3.
  - Refonte procédurale des décors, urbain d'abord : lots P0–P6 du [plan 08](08-direction-artistique.md#décors-procéduraux--chantier-du-25-septembre-2026).
  - Juiciness : liste priorisée J0–J6 du [plan 02 §7](02-juiciness-score.md#7-chantier-prioritaire-du-25-septembre--juiciness-de-tout-le-jeu).
  - Effets d'attaque du joueur et des ennemis refaits en pixel art dans les couleurs de la charte, avec un onglet de réglages (activation, opacité, secousses) : lots V0–V3 du [plan 08](08-direction-artistique.md#effets-dattaque--chantier-du-25-septembre-2026), recette attendue.
- Les plans 13 et 14 restent non arbitrés.

**Mise à jour du 24 septembre :**
- Raphaël valide le design des nouveaux ennemis. Le personnage joué passe à environ 30 px avec un idle animé (08). Son redessin reste reporté.
- HUD refait : plaques contrastées, jauge de PV sous le héros, boussole retirée. Nouvelle police Saira Semi Condensed (04).
- Nouveau [plan 12 — micro-événements et variantes renforcées](12-micro-evenements.md) : cinq événements toutes les 2 à 3 minutes autour des Résurgences, élites naturelles et Souverains. v1 implémentée, recette humaine attendue.
- Demandes suivantes consignées sans implémentation : [plan 13 — butin](13-butin.md) (objets aléatoires à raretés, trois formes de présentation, sources visibles de loin) et [plan 14 — anomalies du monde](14-anomalies-du-monde.md) (événements rares liés au joueur et à l'oubli). Le socle d'objets du plan 05 est leur prérequis.

**Mise à jour du 23 septembre :**
- Les saccades périodiques en combat dense sont corrigées (01 §10). Raphaël trouve le jeu « beaucoup plus fluide » ; la cible 1080p reste ouverte.
- Le début de run reste trop facile. Son hypothèse porte sur le bestiaire : trop passif, trop de corps à corps, pas assez d'attaques à distance originales. Elle est intégrée en 03 A2 et 07, où des menaces à distance originales sont proposées.
- Six [fiches de casting](06-fiches-casting.md) et l'audit des sprites (08 §2) attendent sa validation.

**Suite immédiate du plan 01 (historique du 22 septembre) :** la [mesure en combat dense](01-deplacements.md#9-mesure-de-mobilité-en-combat-dense--22-septembre-2026) est réalisée avec 120 ennemis en 720p/1080p ; elle révèle que les 60 FPS constants ne sont pas tenus. Le diagnostic de ces saccades reste un travail technique à mener avant de conclure la recette. Les variantes clavier/manette et le tempo de début de run restent à évaluer ; aucun nouveau mouvement spécifique ne précède le casting. Le prochain lot de conception prioritaire reste 06 A / 08 A : six fiches et une planche commune à présenter, puis une référence visuelle jouable. Les identités et les choix de dimensions/orientations restent à valider ; le numéro 02 n’impose pas de passer avant cette priorité.

## 4. Expérience de référence proposée

Périmètre de validation, pas réduction du catalogue définitif :
- Un personnage abouti, dans un biome, avec contrôle fluide et une mobilité active retenue après essai.
- Trois armes déjà présentes : Lame Ébréchée, Arc de Fortune et Marteau Lourd, sous réserve de leurs noms/données vérifiés dans le plan 05.
- Trois objets prototypes cumulables, leurs raretés et deux combinaisons aux effets visibles.
- Quatre rôles ennemis, un ennemi fort et une Résurgence.
- Un coffre, un Autel, un fragment de lore rapide.
- Score animé sans record pendant la partie, HUD lisible, bilan de mort refondu et un déblocage direct présenté dans la Collection.

Première session : 8 à 12 minutes pour isoler les sensations ; ensuite runs complètes aux durées de la V2. Ce test court n'impose pas de fin artificielle au jeu final.

**Passage au lot suivant :** commandes comprises immédiatement ; au moins deux styles de combat perçus ; joueur capable d'expliquer un détour, une amélioration et sa mort ; prochaine tentative choisie pour une raison précise.

## 5. Protocole de validation

Pour chaque plan, Raphaël peut répondre : **validé**, **validé avec modifications**, **à retravailler** ou **reporté**, en citant son numéro et les décisions concernées. La validation porte sur le périmètre et les choix explicités, pas sur toutes les extensions futures mentionnées.

Chaque implémentation devra fournir :
- Le lot réalisé et les différences par rapport au plan approuvé.
- Les captures ou séquences avant/après adaptées au changement.
- Les résultats techniques et les observations de playtest, distingués.
- Les décisions encore ouvertes et l'impact sur les lots suivants.

Les réglages numériques proposés sont des points de départ expérimentaux, jamais des résultats mesurés. Il n'y a pas d'estimation de durée ferme avant diagnostic du premier lot.

## 6. Vérifications communes

- C# : `dotnet build` avec zéro warning.
- Scènes, shaders, projet ou initialisation : `tools/smoke_test.sh`, puis essai visuel du parcours touché. Un démarrage headless du Hub ne prouve pas une run correcte.
- Performance : même machine, résolution et scène avant/après ; combat dense avec au moins 100 ennemis ; cible 60 FPS. Noter modèle matériel, moyenne, pics, mémoire et coût des effets.
- État : nouvelle sauvegarde et ancienne sauvegarde de test ; sortie/retour Hub ; événements non dupliqués ; aucune perte de déblocage.
- Accessibilité : texte lisible à 1280×720 et 1920×1080, grand texte, navigation clavier/manette, effets réduits, information indépendante de la seule couleur.
- Architecture : valeurs d'équilibrage JSON, ressources/cache/pools, pas d'allocation ni recherche de nœuds par frame ajoutée, signaux/EventBus entre systèmes.
- Après validation réelle d'un lot : cocher uniquement les items concernés dans la [roadmap V2 §25](../VESTIGES-STRATEGIE-V2.md#25-phases-de-développement). La création de ces plans ne valide aucune fonctionnalité.

## 7. Traçabilité des retours

| Retour | Plans responsables |
|---|---|
| Accueil, menus, onglet Exploration, police | 04 + 08 |
| Vrai choix, davantage de personnages, meilleurs sprites | 06 + 04 + 08 |
| Armes par défaut et à débloquer, tri du catalogue | 05 + 06 |
| Objets variés, effets intéressants, inspiration Megabonk | 05 + 06 |
| HUD intuitif et bien dimensionné | 04 + 02 |
| Quêtes/défis utiles et complets | 06 |
| Bestiaire augmenté | 07 |
| Boucle plaisante, rejouabilité, score/lore/quêtes | 03 + 02 + 05 + 06 + 08 |
| Score permanent, animations, sentiment de récompense | 02 |
| Déplacements en diagonale | 01 |
| Âme, DA, armes/personnages/lore mémorables | 08 et critères de contenu 05/06/07 |
| Classement mondial avec remise à zéro hebdomadaire | 09 |

## 8. Nouveaux retours intégrés

Dash commun validé ; casting d’au moins cinq à six personnages et refonte de tous les sprites avant 01 E : 06/08 ; mode dev tout débloqué : [guide](../DEV-MODE.md) ; mobilité expressive clavier/manette : 01/06 ; record uniquement au bilan et bilan majeur : 02/04 ; XP et menace initiale : 03 ; Hub peu textuel et Collection directe : 04 ; objets illimités et quêtes indépendantes du lore : 05/06 ; casting décalé cohérent : 06/08 ; nouveaux mobs et boss de famille : 07 ; terrain : 10 ; innovations : 11 ; fabrication du pixel art homogène : 08.

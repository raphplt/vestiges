# VESTIGES — Dossier de plans à valider

Date : 21 septembre 2026 · Version : 0.2 · Statut : **directions partiellement validées, extensions proposées**.

Ce dossier transforme les retours de Raphaël en lots réalisables. Il couvre le contrôle, les sensations, la boucle, les interfaces, les builds, la progression, les ennemis, l'identité et la compétition. Les décisions acquises et les propositions restantes sont consignées dans le [registre de décisions](DECISIONS.md). Cette révision prépare la suite sans démarrer le développement.

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
| [01 — Déplacements](01-deplacements.md) | P0 | Socle validé ; fluidité, dash/saut et variantes à préciser | Aucune pour le socle ; 03 pour calibrer la mobilité active |
| [02 — Juiciness, score et récompenses](02-juiciness-score.md) | P0 | Direction validée ; score en run, record au bilan seulement ; refonte mort | 01 pour le ressenti ; cadrage 08 |
| [03 — Boucle et rythme](03-boucle-et-rythme.md) | P0 | Direction validée ; début plus menaçant et XP moins rapide | Première passe 01/02 |
| [04 — Interfaces et Hub](04-interfaces-et-hub.md) | P1 | Navigation, sélection, typographie et affichage adaptatif | Cadrage 08 ; contrats 05/06 |
| [05 — Armes, objets et builds](05-armes-objets-builds.md) | P1 | 4 armes + 4 passifs validés ; objets distincts cumulables sans limite | Hypothèses de tempo 03 |
| [06 — Personnages, quêtes et défis](06-personnages-quetes-defis.md) | P2 | Casting initial, récompenses, progression et défis | 05 ; présentation 04 |
| [07 — Bestiaire et rencontres](07-bestiaire-et-rencontres.md) | P2 | Rôles, compositions et nouveaux comportements utiles | 01/03 ; cadrage 08 |
| [08 — Direction artistique, sprites et lore](08-direction-artistique.md) | Transversal | Référence visuelle, personnages mémorables, pipeline | Cadrage immédiat, production après validation gameplay |
| [09 — Classement hebdomadaire](09-classement-hebdomadaire.md) | Plus tard | Périodes, règles comparables et intégrité des scores | Score 02 ; équilibre 03/05/06 |
| [10 — Terrain et tiles](10-terrain-et-tiles.md) | P1 transversal | Cohérence, transitions, circulation et coût du terrain | Cadrage 08 ; déplacements 01 |
| [11 — Mécaniques originales](11-mecaniques-originales.md) | Prototypes | Sélection et critères d'abandon des innovations | 01/03/05/10 selon proposition |

Les numéros servent à identifier les plans, pas à imposer leur exécution intégrale dans cet ordre. Le [registre](DECISIONS.md) fait foi pour leur statut de validation.

## 3. Séquence recommandée

1. Partir du socle de contrôle validé de 01 et des décisions de 02. Choisir le cadrage de 08 ; définir ensuite la variante de mobilité à prototyper. Obtenir un joueur lisible et des impacts/score/récompenses expressifs.
2. Construire une première référence avec le contenu existant et les lots A–C de 03 : contrôle, menace dès le départ, cadence XP, exploration et Résurgence. Réévaluer la difficulté après introduction de mobilité active.
3. Définir les contrats de contenu de 05 et 06, sélectionner les rôles de 07 et produire les prototypes nécessaires. Réaliser la lisibilité prioritaire de 04, puis assembler l'expérience complète décrite ci-dessous. Les écrans finaux dépendent de ces catégories.
4. Approfondir les objets cumulables, livrer les déblocages directs, la Collection et le Hub épuré. Ajouter les créatures de 07, les assets de 08 et les améliorations terrain retenues de 10. Un prototype de 11 à la fois, selon ses dépendances.
5. Tester plusieurs runs complètes, finaliser le late game/endgame et les critères de sortie.
6. Traiter 09 seulement après stabilisation des règles de score.

Un seul lot d'implémentation ouvert à la fois. Un échec de lisibilité, de performance ou de fun ramène au lot concerné avant expansion.

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

Mobilité expressive clavier/manette : 01/06 ; record uniquement au bilan et bilan majeur : 02/04 ; XP et menace initiale : 03 ; Hub peu textuel et Collection directe : 04 ; objets illimités et quêtes indépendantes du lore : 05/06 ; casting décalé cohérent : 06/08 ; nouveaux mobs et boss de famille : 07 ; terrain : 10 ; innovations : 11 ; fabrication du pixel art homogène : 08.

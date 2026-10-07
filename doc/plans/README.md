# VESTIGES — Dossier de plans à valider

**7 octobre — tout se voit, V1a (plan 27) :** les statuts se lisent sur le sprite des créatures : figé (pose tenue, givre), ralenti (teinte froide, pas ralentis), brûlure (bords bas chauds), Fragile (fêlures). Réglages en données, 13 contrôles, banc A/B sans coût après avoir ramené les boucles du shader à 4 pixels (−5 % en 1080p avant). **V1b :** étoiles qui tournent (désorienté), larmes (saignement), braises (brûlure) autour du sprite, à sa hauteur réelle, jamais coupées par les réglages d'effets. **V1c :** vérifié sur forêt, ville et carrière, en foule de soixante et au banc avec Cloche, Lampe, Berceuse et Polaroïd ; les stries du ralenti, un appel de dessin par créature, sont retirées. **V2a :** brûlure et saignement ont leur chiffre, regroupé toutes les 0,5 s, orange ou rouge, jamais fondu avec ceux des coups ; reliquat affiché à la mort. **V2b :** les Craies dessinent leur forme (étoile, cercle, maison, soleil) à la taille réelle de la zone, l'écho des Gants a son vrai rayon, coups secondaires et objets ont leur gerbe colorée ; banc sans coût. **V2c :** champ du Chronomètre tramé à sa taille réelle, premier maillon du Trousseau depuis le joueur, petit éclat d'un projectile en bout de course. **V2d :** icônes des paliers d'objets quand ils agissent, trait de la brûlure transmise, poussière du recul (plafonnée pour laisser le budget aux impacts). V2 terminé. **V3a :** le joueur touché s'éclaire en rouge, un liseré rouge borde l'écran du côté du coup, « −12 » s'affiche au-dessus de lui, la caméra secoue enfin de façon lisible. **V3b :** la toile de la Tisseuse ne colle plus à un coup annulé (bouclier, invulnérabilité, dash) et se voit (fils sur le personnage, icône près de la jauge) ; le joueur que l'Effacement ralentit pâlit. **V3c :** le bouclier qui casse vole en éclats, revient plein avec un éclat ; l'armure qui pare se voit ; les soins éclairent la jauge ; plus d'icône de vol de vie à vie pleine. **V3d :** le Néant ne rejoue plus le paquet d'une blessure deux fois par seconde : liseré pâle qui pulse, pas de secousse ni de son de coup, dash libre. V3 terminé, à essayer en jeu. Suite : V4, les annonces ennemies.

**6 octobre, nuit — tout se voit, V0 (plan 27) :** le shader des créatures et du joueur applique de nouveau Modulate (lu dans `vertex()` : en fragment, `COLOR` contient déjà la texture). Reviennent le flash des coups, les teintes d'annonce, l'estompage du Rampant, l'éclair et le clignotement d'invulnérabilité du joueur. Même défaut réparé sur les projectiles du joueur : liseré et halo du §53 enfin visibles (§73). Deux modes de capture (`--capture-statuses`, `--capture-player-hit`), planches avant/après regardées, banc A/B sans coût mesurable. Suite : V1, les statuts sur les créatures.

**6 octobre, soir — synergies de règles (planche 05, lot S1, §70) :** les trois planches en attente sont validées. S1a : écho, forme, saignement et feu comptent comme l'arme pour les objets ; ce qu'un objet produit ne déclenche jamais d'objet. S1b : le feu de la Lampe brûle (mêmes dégâts à un PV près), Fragile vaut pour les tics, l'Épingle lit les entravés, une créature figée n'attaque plus (mini-boss exceptés), cumul des Brûlures sans emprunt d'intensité ni de durée. S1c : offre de la Propagation, cap des désorientés, compteur du Scalpel. 20 contrôles ajoutés, validations vertes à chaque sous-lot, trois relectures. Suite : pouvoirs des personnages (plan 06), puis Barrière et Indicible (B1–B4).

**6 octobre — biomes (plan 26 Q7c-4a) :** les cinq fiches sont contrôlées ensemble avant publication, leurs définitions sont en lecture seule ; un refus affiche le diagnostic et empêche la génération. 590 valeurs avant/après identiques, 190 contrôles de catalogues, validations 9/9 puis 5/5 et fiche réellement invalide dans une copie isolée vérifiés. [Preuves](../audits/catalogues-biomes-2026-10-06/README.md). Suite : paramètres du monde, fermes et carrières (Q7c-4b/c), puis événements et Résurgences (Q7d). Planches de lore, synergies et boss toujours en attente de relecture.

**5 octobre — corrections de run et catalogues (plan 26), sans décision de design (§68) :** Q8a tirs guidés verrouillés sur une seule vie de créature ; Q8b remapping sans perte de WASD au redémarrage ; Q8c seed effective publiée et tirages de gameplay dans des flux dérivés de la seed (même seed, mêmes apparitions) ; Q8d un seul propriétaire de la phase de run (late game gardé après la quatrième Résurgence) ; Q7a courbe d'XP, score et Péril contrôlés ; Q7b objets, raretés et offre ; Q7c-1 à Q7c-3 lieux, bénédictions, Oublis, coffres, bonus lâchés, petits lieux et Repères (135 fixtures, valeurs identiques avant/après). Chaque lot : contre-épreuve, valeurs identiques avant/après quand elles devaient l'être, relecture, suites vertes. Galerie de captures d'armes corrigée (plus d'image prise pendant le fondu).

**4 octobre, nuit — chargement récupérable (plan 26 Q4) :** une panne pendant le chargement affiche l'erreur et un retour au camp au lieu de laisser le jeu en pause ; une scène quittée en route ne laisse ni pause ni décors orphelins. 7 scénarios, contre-épreuves par mutation, 7/7 suites. Planches proposées : synergies de règles ([05-planche-synergies.md](05-planche-synergies.md)), Barrière et Indicible ([plan 07](07-bestiaire-et-rencontres.md)), lore L1 ([19-planche-L1.md](19-planche-L1.md)).

**4 octobre, nuit — nettoyage (plan 26 N1) :** meute sans bonus de dégâts, arme en main abandonnée, réglage « Taille du texte » retiré, `score_points` des lieux et coffres retiré (score aux seules éliminations) ; mêmes valeurs, 7/7 suites. Marche à suivre Steam écrite ([STEAM-MISE-EN-PLACE.md](../STEAM-MISE-EN-PLACE.md)). Réponses de reprise : [DECISIONS §67](DECISIONS.md).

**4 octobre — sauvegardes protégées (plan 26 Q2a) :** méta, historique, record et analytics écrits par temporaire vérifié avec copie de secours ; fichier illisible mis de côté et repris sur la copie ; version future jamais réécrite. 39 contrôles, 6/6 suites. **Q2b :** fin de run réglée une seule fois, méta en une écriture, historique réparé au camp, échec affiché au bilan et au camp ; 58 contrôles, 6/6 suites. **Q6a :** contrat des armes en données, effets typés, arme fautive refusée avec un message précis ; valeurs effectives identiques, 13/13 suites. **Q6b :** réglages de l'Indicible en données, même combat ; constat : le boss ne se combat pas vraiment (question au tableau de bord). **Q3 :** classements Steam par une file unique (une opération à la fois, délais, relances, réponses tardives ignorées), rappels pompés pendant la pause, Weekly par semaine ISO ; 56 contrôles avec un faux service, 8/8 suites ; Steam réel non vérifié. **Q6c :** créatures typées et contrôlées par un contrat (stats, capacités, réglages, sons, renforts), meute par famille, groupes d'apparition contrôlés au chargement ; mêmes valeurs, 163 contrôles, 10/10 suites ; question : `pack_bonus_damage` jamais appliqué. **Indicible mesuré** ([plan 07](07-bestiaire-et-rencontres.md#lindicible-mesuré--4-octobre-2026)) : il ne peut pas être blessé (bords hors de portée, ciblage au centre) ; reprise à concevoir avec la Barrière. Questions de reprise consignées ([DECISIONS §62](DECISIONS.md)) : lore v1.1 validé comme base, personnages, G6g, Ateliers, Mémorial, mort et bilan validés. [Plan 26](26-qualite-et-dette-technique.md).

**2 octobre — qualité du code et dette technique :** le [plan 26](26-qualite-et-dette-technique.md) couvre les 18 constats et les noms/règles en dur. **Q0 livré et vérifié** : lanceurs stricts, bancs réparés, validation globale 21/21 ; [preuves](../audits/qualite-2026-10-02/q0/README.md) et [commande commune](../VALIDATION.md). Prochain lot recommandé : Q1 ; nettoyage du combat aux Q5–Q7. [DECISIONS §54–55](DECISIONS.md).

**1er octobre — Effacement, cellules actives :** les zones au Néant gardent leur état sans être recalculées ; stabilisations et ordre des signaux préservés. Rejeu de 30 min identique, recalculs −23,8 %, 72 contrôles, captures et run Main de 30 min vérifiés. [Lot 10 6C et preuves](../audits/erasure-active-2026-10-01/README.md).

**1er octobre (soir) — réveil des lieux dans le monde :** au dernier éclat, le monde se fige, se pixelise, les éclats tournent autour de la stèle et s'y fondent, puis l'oubli recule jusqu'aux bords avant l'écran ; même traitement, plus court, pour la Faille. Passable d'un appui ; 7 contrôles et captures ViewSonic vérifiés. [Plan 24 L6b](24-retours-du-1er-octobre.md#l6b--mémorial-et-faille-mis-en-scène-dans-le-monde-livré-le-1er-octobre-soir).

**1er octobre — recette fiabilisée :** l’intégration Main atteint ses 26 assertions, au lieu d’expirer à 1 500 frames ; captures rejetées sur erreur moteur inattendue. [Vérifications](../audits/verification-tools-2026-10-01/README.md).

**1er octobre — préchauffage rendu :** le chargement soumet réellement 16 shaders et les particules d’XP au GPU, puis libère son viewport ; 17 échantillons vérifiés et captures Main, [plan 10 5B1](10-terrain-et-tiles.md#5b1--soumettre-réellement-les-shaders-au-rendu--découpage-du-1er-octobre). Coût à froid/chaud encore à mesurer sur machine calme.

**1er octobre — robustesse des choix :** relances et entrées interrompues corrigées, 24 contrôles (12 échecs → zéro), captures Main vérifiées ; [plan 04 R7](04-interfaces-et-hub.md#r7--entrées-des-écrans-de-choix--lot-engagé-le-1er-octobre).

**1er octobre (soir) — audio A0/A1 livrés :** enregistrement audible de référence (Movie Maker de Godot, mixage du moteur, trace et rapport), puis pilotage de la musique stabilisé : intention séparée de la phase, annonce tenue jusqu'au début, accalmie, endgame, première run enfin musicale, fondus à puissance constante ; 22 contrôles verts. Avant/après et planche A2 (points d'entrée des morceaux d'annonce et de crise) hors dépôt, **à écouter par Raphaël** : quatre questions au plan 15. Le rôle exploration/combat reste à choisir (foule permanente). [Plan 15](15-audio.md#a0-et-a1--livrés-le-1er-octobre-soir).

**1er octobre — retours visuels et combat :** R1/R2 (pause, survols et icônes de butin), R3b (cadence du Hurleur), R5a–c (trois personnages, 456 PNG) et R6 (carte par biome) livrés et vérifiés. Rotation des coffres préservée. Restent le ressenti humain, l’approbation artistique et le banc FPS de la carte sur machine calme. [État courant au plan 24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026). Cette continuation exclut l’audio (DECISIONS §47) ; son suivi indépendant reste au plan 15.

**1er octobre — correctif du fond des coffres :** rotation continue des rayons à 8°/s, calculée à chaque rendu, avec scintillement des poussières ; ouverture réelle capturée et inspectée (plan 25 I7, remplace le fond fixe I6 refusé par Raphaël).

**1er octobre — [plan 25](25-sprites-et-design.md) intégré sur `main` :** à la demande de Raphaël ([DECISIONS §41](DECISIONS.md)), les huit lots sont raccordés aux écrans et systèmes du plan 24. Raretés et Chance, 34 objets, 14 motifs de Réminiscences, projectiles, cinq bonus, XP, sceaux, minimap, menus et chargement : captures inspectées, build sans avertissement, smoke vert. Les trois objets du monde et les Réminiscences sans effet actif restent « À venir » dans la Collection ; leurs images sont prêtes sans activer de nouvelles règles.

**1er octobre :** retours de la partie du matin, diagnostic et lots au [plan 24](24-retours-du-1er-octobre.md) ([DECISIONS §39](DECISIONS.md)).

**Où en est chaque chose : [tableau de bord](TABLEAU-DE-BORD.md)** (une page, tenue à jour à chaque lot et à chaque décision). Priorité du 30 septembre : le [système de jeu, référence unique](21-systeme-de-jeu.md) (4 armes, 6 objets à niveaux, 4 Réminiscences après les Résurgences) et la [carte à explorer](22-carte-a-explorer.md). Les paragraphes ci-dessous gardent l'historique ; là où ils parlent d'objets sans plafond, de traits ou de quatre passifs conservés, ils sont périmés ([DECISIONS §32 à §34](DECISIONS.md)).

**Priorité actuelle — 28 septembre : objets et perks.** Raphaël demande leur refonte conjointe : objets cumulables sans plafond, quatre perks équipés au maximum. Le [catalogue de neuf spécialisations](05-perks-specialisations.md) est validé pour une V1 extensible ; B0 à B2 et la partie sans objets de B3 sont livrés et vérifiés : sept perks sur neuf sont proposés en run. Délestage et Habitude attendent les objets (B4), dont le catalogue, la récompense à choix et l'inventaire restent à décider par Raphaël. Le [catalogue commun historique](05-catalogue-objets-perks.md) garde l’audit et la proposition d’objets à réviser. Ce chantier coordonne les plans 05/13/17/20, sans ajouter un nouveau plan numéroté. Les autres priorités ci-dessous constituent l'historique et les dépendances des chantiers concernés.

**Catalogue B V1 validé :** quatre perks qualitatifs sans niveaux parmi neuf, avec ajouts futurs possibles. Le [catalogue V1 de neuf perks](05-perks-specialisations.md) précise leurs règles, acquisition et interactions, et compare les plafonds d'armes 70/99 à la courbe d'XP. Le contenu V1 est validé ; coefficients à mesurer, nouveau plafond non arbitré. L'étude A/B/C est conservée au §12 du catalogue historique.

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
| [02 — Juiciness, score et récompenses](02-juiciness-score.md) | P0 | Direction validée ; score en run, record au bilan seulement ; refonte de la mort : M1 à M4 livrés, recette en jeu attendue | 01 pour le ressenti ; cadrage 08 |
| [03 — Boucle et rythme](03-boucle-et-rythme.md) | P0 | Direction validée ; début plus menaçant et XP moins rapide | Première passe 01/02 |
| [04 — Interfaces et Hub](04-interfaces-et-hub.md) | P1 | Navigation, sélection, typographie et affichage adaptatif | Cadrage 08 ; contrats 05/06 |
| [05 — Armes, objets et builds](05-armes-objets-builds.md) | **P0, priorité actuelle** | [Neuf perks V1](05-perks-specialisations.md) validés et extensibles ; B0 à B3 sans objets livrés (sept perks actifs) ; [catalogue d'objets V1](05-objets-catalogue-v1.md) proposé, à valider | 13/17, XP 20 |
| [06 — Personnages, quêtes et défis](06-personnages-quetes-defis.md) | P0 casting ; P2 progression | Casting initial, récompenses, progression et défis | 05 ; présentation 04 |
| [07 — Bestiaire et rencontres](07-bestiaire-et-rencontres.md) | P1 début de run ; P2 reste | Menaces à distance originales, rôles, compositions | 01/03 ; cadrage 08 |
| [08 — Direction artistique, sprites et lore](08-direction-artistique.md) | Transversal | Référence visuelle, personnages mémorables, pipeline | Cadrage immédiat, production après validation gameplay |
| [09 — Classement hebdomadaire](09-classement-hebdomadaire.md) | Plus tard | Périodes, règles comparables et intégrité des scores | Score 02 ; équilibre 03/05/06 |
| [10 — Terrain et tiles](10-terrain-et-tiles.md) | P1 transversal | Cohérence, transitions, circulation et coût du terrain | Cadrage 08 ; déplacements 01 |
| [11 — Mécaniques originales](11-mecaniques-originales.md) | Prototypes | Sélection et critères d'abandon des innovations | 01/03/05/10 selon proposition |
| [12 — Micro-événements et variantes](12-micro-evenements.md) | P0 | Cadence, cinq événements, élites et Souverains (v1 livrée) | 03/07 |
| [13 — Butin](13-butin.md) | P0 après recette 12 | Trois formes de butin (Vestige figé, Triptyque, Pacte d'oubli), raretés, sources lisibles | Objets 05, idée B 11 | **Absorbé par le plan 22.**
| [14 — Anomalies du monde](14-anomalies-du-monde.md) | P1 | Anomalies rares liées au joueur et à l'oubli, jamais mortelles | 12, 13 |
| [15 — Audio](15-audio.md) | Prochain gros chantier (1er octobre) | 50 choix branchés ; nouvelle passe musique, ambiances, effets et mixage en run, avec identité des Résurgences | 24 §12, DECISIONS §44 ; inventaire restant à réauditer |
| [16 — L'oubli rendu sensible](16-oubli-sensible.md) | P1 | Lots O1–O6 : sol qui oublie, choses qui se défont, frontière visible, coût et récompense de l'oubli | 10 (shader du sol), 02, 13, 15 |
| [17 — Armes, coffres et modificateurs](17-armes-coffres-modificateurs.md) | P0 | Vagues 0–5 ; rareté sur les améliorations ; noms d'armes ; Mémoriaux et Failles | 05, 08, 13, 16, 18 |
| [18 — Inventaire des restes V1](18-inventaire-restes-v1.md) | P0 | Éléments de lore et POI vectoriels, Colosses, identifiants Steam « nuits » | 17 (lots 0B/0C) |
| [19 — Lore](19-lore.md) | Transversal | Étape A : incohérences, textes trop directs, 14 questions à trancher, pistes de révélation | 06 (casting), 16 (échos), 17 (noms d'armes) |
| [20 — Récompense et puissance](20-recompense-et-puissance.md) | P0 (R1 capital) | Retours du 28 septembre ; modèle de progression révisé pour 300 à 400 niveaux en 45 min (§6.6), cibles à valider ; paliers validés ; lots D1, R1-0 et R1-T livrés ; R1-F proposé | 03, 05, 06, 13, 16, 17 |
| [21 — Système de jeu](21-systeme-de-jeu.md) | **P0, référence unique** | Fixe le build : 4 armes (niveau 50, ascension), 6 objets à 50 niveaux, 4 Réminiscences après les Résurgences, personnages à affinités. [Historique](21-historique.md) ; lot G1 livré, G2a en cours | 05, 06, 17, 20, 22 |
| [22 — Carte à explorer](22-carte-a-explorer.md) | **P0** | Décors qui deviennent des lieux, Atelier et Trempe, Reliquaire, Repères, Atlas ; absorbe 13, 11 B et les POI de 18 ; à valider | 21, 12, 16, 17 |

Les numéros servent à identifier les plans, pas à imposer leur exécution intégrale dans cet ordre. Le [registre](DECISIONS.md) fait foi pour leur statut de validation.

## 3. Séquence recommandée

1. Utiliser le mode dev pour les essais. Prioriser ensemble 06 (refonte et validation d’au moins cinq à six personnages) et 08 (tous leurs sprites à refaire, échelle et style communs). Valider les identités et les nouveaux sprites en jeu avant les mobilités spécifiques du lot 01 E ; le dash commun reste la référence validée.
2. Construire une première référence avec le contenu existant et les lots A–C de 03 : contrôle, menace dès le départ, cadence XP, exploration et Résurgence. Réévaluer la difficulté après introduction de mobilité active.
3. Définir les contrats de contenu de 05 et 06, sélectionner les rôles de 07 et produire les prototypes nécessaires. Réaliser la lisibilité prioritaire de 04, puis assembler l'expérience complète décrite ci-dessous. Les écrans finaux dépendent de ces catégories.
4. Approfondir les objets cumulables, livrer les déblocages directs, la Collection et le Hub épuré. Ajouter les créatures de 07, les assets de 08 et les améliorations terrain retenues de 10. Un prototype de 11 à la fois, selon ses dépendances.
5. Tester plusieurs runs complètes, finaliser le late game/endgame et les critères de sortie.
6. Traiter 09 seulement après stabilisation des règles de score.

Un seul lot d'implémentation ouvert à la fois. Un échec de lisibilité, de performance ou de fun ramène au lot concerné avant expansion.

**Mise à jour du 29 septembre, nuit — plan 21 version 2 et lot G1 livré :** Raphaël tranche le plan 21 ([DECISIONS §32](DECISIONS.md)) :
- armes au niveau 50 par stats aléatoires, avec ascension à deux voies au niveau max ;
- objets en 6 emplacements jusqu'au niveau 50 ;
- fragments après les Résurgences ;
- bannissements gratuits puis payés en Péril ;
- courbe de 300 à 400 niveaux conservée.

Le [lot G1](21-historique.md) est livré. Question ouverte : les traits, dont l'agent recommande la fusion dans les objets.

**Mise à jour du 29 septembre, soir — refonte du build depuis les principes :** Raphaël fixe [douze principes de conception des builds](../PRINCIPES-BUILD.md) et demande une reconception neutre. Le [plan 21](21-systeme-de-jeu.md) propose cinq familles aux rôles nets et trois canaux d'acquisition :
- armes (voies au niveau 5) et traits sur propriétés communes, par le level-up ;
- objets à déclencheurs communs, par le monde ;
- fragments après chaque Résurgence.

Il propose aussi une courbe de 80 à 110 niveaux au lieu de 300 à 400. Les catalogues d'objets et de perks précédents deviennent des matériaux pour ce plan. À valider avant tout code.

**Mise à jour du 29 septembre, suite — proposition 2 des objets et des icônes, à valider :** le [catalogue d'objets](05-objets-catalogue-v1.md) compte 30 objets, dont 19 à condition, contrepartie ou effet sur le monde et 11 multiplicateurs. Il ajoute les déblocages : 13 objets au départ, les autres par quêtes ou achats en Vestiges au Hub. Les [icônes de perks](planches/perks-icones-proposition-2.png) deviennent des fragments teintés par famille, aux pictogrammes repris des retours visuels du jeu ([plan 05 §15](05-perks-specialisations.md)).

**Mise à jour du 29 septembre — catalogue d'objets révisé et planche d'icônes de perks, à valider :** le [catalogue d'objets V1](05-objets-catalogue-v1.md) propose 23 objets. Il retire les cinq qui copiaient une signature d'arme, donne une destination aux treize passifs et une source à part pour la récompense à choix (le Reliquaire, Triptyque du plan 13). La [planche d'icônes de perks](planches/perks-icones-proposition-1.png) propose des pin's émaillés, en deux variantes d'émail. Rien n'est créé en jeu avant validation.

**Mise à jour du 29 septembre — B3 sans objets livré :** [plan 05 §14](05-perks-specialisations.md#14-compte-rendu-b3-partie-sans-objets--sillage-et-seconde-lecture-29-septembre-2026). Sillage et Seconde lecture sont actifs. Propagation, mesurée sur les trois armes de contrôle, reste inchangée : seule la Cloche échoue, c'est un réglage d'arme. Délestage et Habitude sont bloqués : il n'existe ni inventaire d'objets ni récompense à choix. Banc d'effets de 66 assertions.

**Mise à jour du 29 septembre — lot B2 livré (effets de combat et de survie) :** [plan 05 §13](05-perks-specialisations.md#13-compte-rendu-b2--effets-de-combat-et-de-survie-livrés-le-29-septembre-2026). Prévoyance, Reprise, Débordement, Convergence et Propagation sont actifs en run, avec leurs retours visuels. S'y ajoutent l'état inactif, la pause chiffrée et l'avertissement avant un échange d'arme. Banc d'effets de 50 assertions, captures regardées, banc dense A/B sans écart hors bruit (sans perk actif dans le banc). Réglage à trancher : Propagation ne se déclenche presque pas avec la Cloche seule. Suite proposée : B3.

**Mise à jour du 29 septembre — lot B1 livré (acquisition des perks) :** [plan 05 §12](05-perks-specialisations.md#12-compte-rendu-b1--acquisition-livrée-et-vérifiée-le-29-septembre-2026). Quatre droits aux paliers 2/6/12/20 dans la file de niveaux, report, cascade, relance, bannissement protégé, première offre composée, éligibilité selon l'arsenal, carte et pause. En sommeil en run normale tant qu'aucun effet n'est branché ; vérifié par un banc de 21 assertions et une capture en aperçu. Suite proposée : B2.

**Mise à jour du 28 septembre — mort refondue, lot M1 livré :** [plan 02 lot D, seconde passe](02-juiciness-score.md). La mort se joue dans le monde : impact et ralenti, le joueur se défait, l'Effacement part de sa place et décolore l'écran, le tueur reste en couleur. Le bilan suit sans titre explicite. Direction validée ([DECISIONS §22](DECISIONS.md)) : une seule page de bilan dense, distance mise en avant. Suite proposée : M2 (données de run).

**Mise à jour du 28 septembre — lot M2 livré :** [plan 02 lot D](02-juiciness-score.md). La run relève sa distance (au plus loin et parcourue), une frise, les éliminations par arme, les élites, Souverains et boss. L'historique passe en version 3 sans perdre les runs V2 ; une montée de version les aurait effacées, c'est corrigé. Suite proposée : M3 (bilan dense).

**Mise à jour du 28 septembre — lot M3 livré :** [plan 02 lot D](02-juiciness-score.md). Le bilan tient en une page dense. En tête : distance, score et durée. Au centre : le tableau des armes (part, DPS, éliminations), le tueur et les faits. Dessous : la frise de la run. Suite proposée : M4 (gains animés).

**Mise à jour du 28 septembre — lot M4 livré, refonte de la mort terminée :** [plan 02 lot D](02-juiciness-score.md). Les cartes de gain se retournent une à une et les Vestiges montent dans un compteur. Passer la révélation donne le même état final. Reste à juger en jeu : durée de la séquence, densité de la page, échelle des distances.

**Mise à jour du 28 septembre — lot 6B livré :** le feedback du cône du Transistor est cadencé, les dégâts et procs restent à chaque tick. À 50 cibles sur quatre secondes : allocations directes **2,66 → 1,41 Mo (−47 %)** et feedback **12 000 → 2 000 (−83 %)**, mêmes dégâts et attribution. 38 assertions et régressions armes/capacités vertes, smoke et captures avec chaîne/homing vérifiés. [Preuves, scripts et limites](../audits/performance-2026-09-28/lot-6b/README.md). Aucun gain FPS revendiqué. Suite proposée : 5B (préchauffage).

**Mise à jour du 28 septembre — lot 6A livré après autorisation :** les statuts expirent au loin et l’Effacement conserve ses pas manqués (rattrapage limité à quatre par image). Même banc : 13 échecs avant, zéro après sur 26 assertions ; à 1 Hz, 30,80 % d’oubli en dix minutes au lieu de 15,40 %. Build sans avertissement, capacités/déplacement, smoke et capture de run vérifiés. L’intégration Main headless expire avant comme après et reste signalée. [Preuves et limites](../audits/performance-2026-09-28/lot-6a/README.md). Prochain lot : 6B (cône).

**Mise à jour du 28 septembre — seconde passe de performances et robustesse (diagnostic uniquement) :** [rapport](../AUDIT-PERFORMANCES-2026-09-28.md), [preuves et scripts](../audits/performance-2026-09-28/README.md), [lots proposés au plan 10 §11](10-terrain-et-tiles.md#11-audit-de-performances-du-27-septembre--lots).
- Cône : 12 000 impacts et 3,05–3,18 Mo alloués dans quatre secondes d’émission sur 50 cibles, après chauffe.
- Deux défauts temporels reproduits : statuts lointains qui n’expirent pas ; temps excédentaire perdu par l’Effacement.
- Préchauffage : zéro dessin hors champ contre huit dans le champ. Physique : 10 694 corps de décor, mais seulement 2 318 formes ; potentiel mémoire estimé des corps inutiles ≈5,88 Mio.
- Vingt cycles techniques Hub/run/Hub : 148 nœuds et zéro orphelin à chaque retour. Profils managés de foule archivés. A/B final au calme : 2,28–2,49 ms/image à 60 ennemis, 7,42–7,58 ms à 240 ; inspection du banc trop faible pour expliquer cette pente. Pic initial de 108–137 ms à isoler de la mesure du combat. Aucune conclusion de FPS sous charge.
- Suites proposées : 6A horloges, 6B impacts continus, 5B préchauffage, 3B attribution foule, 6C cellules actives, 6D corps optionnels. **Aucun correctif de production, aucune case de roadmap cochée ; travail de feu préexistant préservé.**

**Mise à jour du 28 septembre, nuit :** [plan 20 §6.7–§6.8](20-recompense-et-puissance.md). Paliers validés ; réserve de niveaux automatique ; montée plus forte en endgame.
- Lot R1-0 livré : courbe d'XP en données (`data/scaling/progression.json`), plafonnée à 3 000 XP par niveau.
- Lot R1-T livré : temps pour tuer mesuré. Sur 45 min, il baisse de moitié même pour le bot ; 2 300 à 2 600 orbes au sol à 45 min.
- L'endgame n'est jamais atteint, car l'Indicible n'est pas combattable : R1-F proposé, avec des PV ×1,07 par minute après 22 min.

**Mise à jour du 28 septembre, soir :** [plan 20](20-recompense-et-puissance.md) après les réponses de Raphaël.
- §6.6 : une excellente run vise 300 à 400 niveaux en 45 min. Coût de niveau plafonné à 3 000 XP au-delà du niveau 41 (simulé : 358 niveaux à 45 min) ; réserve de niveaux pour enchaîner les choix ; niveaux de surplus au-delà d'un build complet (≈ 219 montées).
- Lot D1 livré (§7.2) : Présage moins fréquent et absent avant 3 min, dégâts à distance à ×1,02 par minute, tir du Hurleur annoncé. Zones et tirs : 46,6 → 34,2 % des dégâts filtrés.

**Mise à jour du 28 septembre, après-midi :** [plan 20 §6–§9](20-recompense-et-puissance.md#6-modèle-de-progression--28-septembre-2026-proposition-cibles-à-valider). Mesure de 30 min (bot nomade, deux seeds) et `tools/progression_model.py`.
- Niveaux cibles par archétype à valider, leviers d'XP chiffrés et simulés (temps, oubli, Résurgences, Péril, boss intermédiaire à rang choisi, Indicible, build).
- Constats : l'XP par PV à abattre est divisée par 4 à 30 min ; les orbes laissées pèsent au plus 15 % de l'XP (750 à 920 au sol), alors que 73 % des créatures apparues ne meurent jamais.
- Dégâts à distance (`tools/damage_sources.py`) : c'est le Présage qui pèse (24 % des coups, présent dans tous les biomes), pas les tireurs, qui blessent en proportion de leur présence.
- Densité vers 1 000 créatures : environ trois fois moins de coût par créature à trouver.
- Rien n'est implémenté côté jeu.

**Mise à jour du 27 septembre, nuit (session locale, sans Raphaël) :** machine enfin calme, [audit de performances](../AUDIT-PERFORMANCES-2026-09-27.md) repris en lots ([10 §11](10-terrain-et-tiles.md)).
- Lot 1 : le pool d'ennemis ne laisse plus d'ennemis orphelins en fin de run (20 par run avant), abonnement de la montée de niveau désabonné. Le brouillard, invisible depuis mars, est retiré ; **à arbitrer** : l'Oubli du regard n'avait donc aucun effet visible.
- [17 3D](17-armes-coffres-modificateurs.md) : un coffre posé après un Oubli des repères naît avec sa colonne raccourcie.
- Lot 2 : le sol et les routes se dessinent depuis un seul atlas, à l'image identique. À 1 080p, 156 → 255 FPS et 1 894 → 331 appels de dessin par image sur le banc de combat dense.
- Banc des chemins T3 fait : pas de coût mesurable.
- [02 lot D](02-juiciness-score.md) et [04 C2](04-interfaces-et-hub.md) : une arme gagnée pendant la run a sa carte au bilan, avec « Voir dans la Collection », qui ouvre la Collection sur elle.
- [04 lot C](04-interfaces-et-hub.md) : les colonnes de la pause défilent au stick droit et à Page haut/bas.
- Crash à la fermeture du jeu lancé depuis Godot (préexistant) corrigé par une sortie propre ([10](10-terrain-et-tiles.md)).
- Audit, lot 4 : les orbes d'XP laissées loin s'endorment. Un nomade en laisse environ 300 en 10 minutes ; avec 400 orbes, 720p passe de 244 à 289 FPS.
- [08](08-direction-artistique.md) : auras d'affixe en pixel art, sous les pieds.
- [03 lot E](03-boucle-et-rythme.md) : **l'Indicible n'est pas combattable** (aucune arme ne le vise, 0 PV perdu en 40 s de combat). Sa mort et le passage en endgame marchent. Refonte à arbitrer avec le lore.
- [17](17-armes-coffres-modificateurs.md) et [02 J3](02-juiciness-score.md) : arme au sol refaite (anneau pixel, invite commune « Échanger », plus de stats ni de texte au sol), icône qui vole vers le HUD.
- Audit, lot 5 : les animations des créatures se chargent sous l'écran de chargement. Avant, chaque nouvelle espèce faisait sauter une image en pleine partie.
- [10 lot E](10-terrain-et-tiles.md) : les herbes plient aussi au passage des huit créatures les plus proches.
- Explosion des créatures instables en effets pixel recyclés, qui montrent la zone réelle des dégâts, mesurée au sol ([10](10-terrain-et-tiles.md)).
- [06](06-personnages-quetes-defis.md) : un profil neuf commence avec le Vagabond (décision du 23 septembre) ; les profils existants gardent leurs personnages.

**Mise à jour du 27 septembre, soir (session locale) :** deux bugs signalés par Raphaël.
- [Son des coffres](15-audio.md) : la mélodie de révélation ne part plus pendant le défilement et s'éteint avec l'écran.
- [Chargement](10-terrain-et-tiles.md) : plus de frame de carte vide au départ, et chargement ramené de 6,0 à 3,5 s (génération sur un thread, décors construits hors de l'arbre).
- [07 lot B](07-bestiaire-et-rencontres.md), fin : tirs du Cracheur, de la Sentinelle et de la Tisseuse annoncés par un couloir de visée ; portée de la Sentinelle dessinée au sol.
- [04 lot C](04-interfaces-et-hub.md) : paramètres entièrement navigables au clavier et à la manette (ils étaient à la souris seule), pause ouverte sur « Reprendre », cadre doré sur le focus.
- [03 lot C](03-boucle-et-rythme.md) : présage des Résurgences, les bords de l'écran se ternissent pendant l'avertissement et les créatures s'agitent.
- [02 lot D](02-juiciness-score.md) : le bilan montre les dégâts de chaque arme, la principale en or.
- [17 lot 3B](17-armes-coffres-modificateurs.md) : au Mémorial ravivé, relancer les bénédictions contre de l'Essence.
- [10 lot D](10-terrain-et-tiles.md) : contrôle automatique de connectivité ; sur 13 seeds, les 31 coffres, Mémoriaux et Failles de chaque carte sont accessibles à pied depuis le départ.
- [10 lot E](10-terrain-et-tiles.md) : herbes, fleurs et roseaux plient au passage du joueur (shader, aucune boucle sur les décors).
- [16 O6](16-oubli-sensible.md) : les échos de l'oubli montrent de vrais habitants (écolière, vieil homme, ouvrière) au lieu des personnages jouables.
- [08, ennemis](08-direction-artistique.md) : les huit ennemis de mars (Brute du Vide, Ombre, Hurleur, Sentinelle, Cracheur Pâli, Rampant, Rampant d'Ombre, Tréant) refaits dans le pipeline, à l'échelle du joueur. Toutes les créatures en jeu sont désormais procédurales.

**Mise à jour du 27 septembre, suite (session locale, sans Raphaël) :**
- Bancs de A-VERIFIER toujours pas faits : charge entre 4 et 7,7 toute la session (seuil fixé à 2).
- [07 lot B](07-bestiaire-et-rencontres.md), étapes 1 et 3 : la charge de la Brute, le surgissement du Rampant et le cri du Hurleur s'annoncent au sol et laissent une fenêtre ; le Rampant ne blesse plus enfoui ; tuer un Hurleur qui crie coupe l'appel. Sons et animations branchés.
- [04 lot B](04-interfaces-et-hub.md), étapes 1 à 4 : échelle typographique commune (rien sous 14 px en base 1080p, lisible en 720p), réglage « Taille du texte » 100/115/130 %, colonnes de la pause et onglets des paramètres qui défilent.
- [08 P6b](08-direction-artistique.md) : un chantier par région de la carrière, sur le modèle des fermes (entrée de galerie étayée, voie et wagonnets, baraque, machines groupées). Le placement des fermes et des chantiers passe par un même `SiteComposer`.

**Mise à jour du 27 septembre (session locale, sans Raphaël) :**
- Passage de vérification sur le Mac de ce que la session cloud avait livré sans GPU ([A-VERIFIER](A-VERIFIER.md)) : bilan, level-up, chemins, fermes, repères et échos regardés en capture. Corrigés : murmure des échos flou, brèche du toit de l'église trop régulière.
- [17 2B](17-armes-coffres-modificateurs.md) : chaque arme reprend la couleur de son icône dans ses effets d'attaque.
- [17 2C](17-armes-coffres-modificateurs.md) : arme en main en essai, derrière une option désactivée par défaut.
- [07 lot B](07-bestiaire-et-rencontres.md) : **deux mécaniques qui n'avaient jamais marché** réparées. La charge de la Brute du Vide avançait à 35 px/s (vitesse écrasée par la poursuite), et le recul des armes ne repoussait rien. Réglages des comportements passés en données ; recyclage du pool vérifié par un test.
- [04 lot B](04-interfaces-et-hub.md) : une quarantaine de textes français sans accents corrigés.
- [05](05-armes-objets-builds.md) : les six synergies de perks s'annoncent sans aucun effet ; à arbitrer.
- [08 P5 et P6](08-direction-artistique.md) : décors du marais refaits à l'échelle du personnage, décors propres à la carrière (roche, cristaux d'Essence, machines figées, traces des ouvriers). P3 à P6 sont livrés.
- Non fait : bancs de performance (machine chargée toute la session).

**Mise à jour des 26 et 27 septembre (nuit, session cloud sans Raphaël) :**
- [10 T3](10-terrain-et-tiles.md#lot-t3-livré--chemins-et-routes-26-septembre-2026) : chemins de terre entre les régions, raccordés aux rues, avec un style par biome. Les rues verticales ne sont plus coupées en deux bandes, et les trottoirs ont des bordures usées.
- [16 O6](16-oubli-sensible.md) : échos de l'oubli. Dans les zones Fragiles, la silhouette pâle d'un habitant apparaît parfois, puis se dissout à l'approche en laissant un murmure.
- [08 P4b-1/2](08-direction-artistique.md) : une ferme par région des champs, reliée au réseau de chemins par un embranchement (maison, grange, silo, hangar, enclos, haies).
- [02 J0](02-juiciness-score.md) terminé : les effets de mort et la collecte d'XP sont recyclés, et le panneau des quêtes ne recrée plus ses lignes quatre fois par seconde. Banc de combat dense : 284 → 8 nœuds créés en 15 s.
- [07 §7](07-bestiaire-et-rencontres.md) : perception et laisse propres à cinq créatures (valeurs provisoires, mesure à refaire).
- [10 T2](10-terrain-et-tiles.md) : décors de transition mêlés le long des frontières de biomes. [08 I3](08-direction-artistique.md) : hauteur de vol unique des projectiles.
- [16 O2](16-oubli-sensible.md) : en zone Effacée, des éclats du Néant s'élèvent des décors proches.
- [08 P4b-3](08-direction-artistique.md) : haies, murets et clôtures aux bords des parcelles, vergers en rangs.
- [16 O5](16-oubli-sensible.md) : un Autel qui sert rappelle sa zone à l'existence. Depuis la fusion de main, c'est le réveil d'un Mémorial qui le fait (les Autels n'existent plus).
- [08 P4b-4](08-direction-artistique.md) : scènes-récits dans les champs (pique-nique abandonné, linge étendu, épouvantail aux corbeaux, tracteur embourbé).
- [02 J1](02-juiciness-score.md) : retour de coup sans tween (recul sur le visuel seul), chiffres de dégâts en Saira, cumulés par cible, critique distinct par la forme.
- [08 P2](08-direction-artistique.md) : l'église et le pylône de télécommunication reviennent comme repères rares des Ruines Urbaines, refaits dans le pipeline.
- [02 J2](02-juiciness-score.md) : morts orientées par le dernier coup (dissolution, éclats, nuage), orbes d'XP qui jaillissent du corps, onde et éclair à la mort des élites.
- [02 J3](02-juiciness-score.md) : orbes qui s'étirent et laissent une traînée, son de ramassage qui monte le long d'une chaîne, barre d'XP qui pulse.
- [02 J4](02-juiciness-score.md) : montée de niveau avec onde dorée, colonne de lumière, créatures proches repoussées (en apparence), barre d'XP qui éclate, écran de choix qui entre avec du punch.
- [02 J5](02-juiciness-score.md) : compteur de morts en rafale (« ×24 »), léger recul de caméra quand l'écran se remplit, voix sonores coupées de la plus ancienne à la plus récente.
- [02 J6](02-juiciness-score.md) : traces de pas selon le sol (ronds dans l'eau, poussière), coffres qui frémissent à l'approche.
- [02 lot A](02-juiciness-score.md) : score exact. Horloge de jeu qui exclut les pauses, score qui monte au HUD entre deux kills, « nouveau record » enfin affiché au bilan, barème en JSON.
- [02 lot D](02-juiciness-score.md) : bilan de fin de run refait en trois zones (score et record, personnage et build complet, gains), révélé en trois temps après que le monde a pâli.
- [04 C2](04-interfaces-et-hub.md) : Collection dans le menu de l'accueil. Armes et souvenirs de run en grille, détail et condition de déblocage à la demande, même règle de disponibilité que le loot.
- [07 lot A](07-bestiaire-et-rencontres.md) : audit des données du bestiaire (`tools/audit_bestiary.py`). La Tisseuse ne sort qu'en crise dans les Marécages ; propositions à arbitrer.
- [07 lot C](07-bestiaire-et-rencontres.md) : un Colosse par crise, livré puis **retiré** à la fusion de main, qui a supprimé les Colosses (plan 17 lot 0C).
- [03 lot C](03-boucle-et-rythme.md) : pendant une Résurgence l'oubli s'accélère ; à l'accalmie, Essence doublée 30 s et coffre rare posé à portée devant le joueur (V2 §8).
- **À arbitrer en priorité** : la Tisseuse hors des Marécages ; le barème des points d'intérêt (valeurs de `pois.json` inutilisées) ; la recette du bilan de fin de run et de la Collection (premières passes sans maquette validée).
- 27 septembre : main (plans 17 et 18) fusionnée dans la branche de la session. Les apports de la nuit sont reportés sur les coffres, le level-up et les Mémoriaux de main ; ce qui dépendait des Autels, des Colosses ou des raretés d'armes a été retiré.
- Travail fait dans un conteneur sans GPU : les points à revérifier sur la machine de Raphaël sont listés dans [A-VERIFIER.md](A-VERIFIER.md).

**Mise à jour du 26 septembre (plan 17, vague 0 livrée) :** coffres réintégrés et visibles ([17](17-armes-coffres-modificateurs.md#lot-0a-livré--26-septembre)).
- 23 coffres sur toute la carte au lieu de 10 à 15 autour du départ, dégagés des décors.
- Sprites à l'échelle des personnages, colonne de lumière à la couleur de la rareté, invite « Ouvrir », flèches de bord d'écran.
- Palette de rareté unique ; butin montré tel qu'obtenu, sans perks V1 ni malédictions.
- Mesure de densité désormais headless en temps accéléré (`tools/measure_run.sh`) : 5 seeds × 3 min en ≈ 1 min.
- Enchaînés le même jour, à la demande de Raphaël : 0B (restes V1 visibles retirés, POI et éléments de lore désactivés), 0C (code, données et 306 PNG morts, Colosses), 0D (références au jeu du genre ramenées à une section de garde-fous).
- Deux correctifs trouvés en route : blocage de partie au level-up, durée de run comptée en temps réel.
- Vague 1, sans attendre la décision 4.3 : les quatre bugs de build corrigés (effets de l'arme qui frappe, notes de la Boîte à musique, niveau unique, bannissement), banc `tools/test_weapons.sh` ; pilote de trois icônes d'armes 32×32 à juger.
- Soir : décision 4.3 prise (rareté seulement sur les améliorations) ; coffres et icônes v2 ; vague 1 livrée : croissance des armes en données, level-up à raretés (Chance et oubli), cartes « avant → après », pause avec équipement et fiche. Recette groupée attendue.
- Nuit : vague 3 livrée ([17](17-armes-coffres-modificateurs.md#vague-3-détaillée--26-septembre-soir)). Péril à la place de l'Appel du Vide et des malédictions (3A) ; Mémoriaux à raviver à la place des Autels (3B) ; Failles et Oublis (3C). Écran de choix commun et lieux interactifs partagés avec les coffres. Question ouverte : ces écrans figent la run (V2 §11 voulait l'Autel sans pause). Vague 2 en attente des noms d'armes et du style d'icônes ; vague 4 dépend du plan 13.
- Nuit (suite), après les retours de Raphaël (pause gardée, noms et icônes validés, Oublis de carte validés) : lots 2A (noms, histoire des armes dans la pause), 2B (24 icônes 32×32, HUD agrandi) et 3D (neuf Oublis de carte) livrés. Restent : Craies et Chronomètre à confirmer ; 2C (arme en main) non commencé ; vague 4 dépend du plan 13.

**Mise à jour du 26 septembre (armes, coffres, modificateurs) :** nouvelle priorité de Raphaël, auditée puis planifiée, rien d'implémenté.
- [Plan 17](17-armes-coffres-modificateurs.md) : armes (sprites, noms, présentation, level-up à raretés, pause), coffres, modificateurs de run (Chance, Péril, Mémoriaux et Failles), objets ensuite, north star.
- Coffres : ils apparaissent toujours (13 et 14 dans les logs du jour) mais ne sont jamais ouverts. Sprites restés à 16×12 face aux décors agrandis, placement confiné à 7,6 % de la carte depuis le passage au rayon 200, aucun signal.
- [Plan 18](18-inventaire-restes-v1.md) : inventaire des restes V1. Les visuels « lisses » viennent du rendu procédural (polygones, dégradés, lumières texturées par le logo Godot), pas des PNG.
- Décisions attendues : plan 17 §6 et plan 18 §5.

**Mise à jour du 26 septembre (soir) :**
- Raphaël valide les tuiles de la forêt (« mille fois mieux »).
- Le lot T1 couvre quatre biomes sur cinq (forêt, carrière, champs, marais) en tuiles de Wang ; la ville reste à faire ([10](10-terrain-et-tiles.md)).
- Vue isométrique respectée pour les sorts et effets, lots I1–I4 : projectiles orientés, annonces au sol, ombres de contact, finitions ([08](08-direction-artistique.md#respect-de-la-vue-isométrique--chantier-du-26-septembre-2026)).
- Bord du monde infranchissable.
- Créatures distancées qui perdent la trace ([07 §7](07-bestiaire-et-rencontres.md)).
- Suite : la ville en T1, O3 (frontière de l'oubli), T3 (chemins).

**Mise à jour du 26 septembre (après-midi) :** Raphaël valide l'accueil et les effets d'attaque, trouve l'ouverture « un tout petit trop peu adoucie », valide l'ordre des chantiers et délègue les autres arbitrages ([DECISIONS §7](DECISIONS.md#7-arbitrages-délégués-du-26-septembre), provisoires).
- Nouvelle machine, un Mac Apple Silicon. Steam y levait une exception à chaque frame ; les outils de test écrivaient dans le vrai profil. Les deux sont corrigés.
- Ouverture adoucie d'un cran ([03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)).
- Jonctions tramées entre biomes ([10 T2](10-terrain-et-tiles.md#lot-t2-livré--26-septembre-2026)) et sol qui oublie ([16 O1](16-oubli-sensible.md#6-arbitrages-et-lot-o1--26-septembre-2026)), par un même shader du sol.
- Suite prévue : T1 (sol refait), O3 (frontière de l'oubli), 07 §7 (perception des créatures).

**Mise à jour du 26 septembre (retours de jeu) :** début de run adouci (6 s de répit, montée sur une minute, [03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)) ; sons d'attaque ennemis branchés, provisoires ([15](15-audio.md)) ; décors découpés en tronçons, ≈ ×2 FPS en combat dense ([10 §10](10-terrain-et-tiles.md#10-investigation-performance--26-septembre-2026)). Propositions à arbitrer : composition des champs (08 P4b), tiles et jonctions (10 §9), [plan 16](16-oubli-sensible.md) sur l'oubli, perception des créatures (07 §7).

**Point d'arrêt du 26 septembre (retours de jeu) :** livrés et vérifiés (build, smoke, régressions, bancs, captures) : ouverture de run, sons d'attaque provisoires, tronçons de décors. Non fait : relecture `godot-reviewer` du diff lancée mais pas intégrée ; recette en jeu de ces trois points ; lot suivant de l'investigation perf (physique interne du moteur, 10 §10) ; aucun des lots proposés (08 P4b, 10 §9, 16, 07 §7) n'est commencé, en attente d'arbitrage.

**Mise à jour du 26 septembre :** l'écran d'accueil est refait sur demande de Raphaël (« trop classique », pas de stats mais le sprite, « surprends-moi »). Le Hub devient le camp du Foyer, vivant ; les personnages veillent autour du feu ; le menu est textuel. Compte rendu et limites : [plan 04](04-interfaces-et-hub.md#accueil-refait--26-septembre-2026). Recette attendue.

**Mise à jour du 25 septembre :**
- Audio : [50 choix branchés, nettoyage et suite](15-audio.md) ; [archives hors dépôt](../audio/README.md).
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


**1er octobre, continuation hors audio — R3b :** cadence du Hurleur ×2,5, contrôles et captures vérifiés ; [rapport du banc fixe et des runs](../audits/projectile-cadence-2026-10-01/README.md). R6 carte et R5 personnages suivent, conformément à DECISIONS §47.

**1er octobre, préchargement :** le boot Main charge désormais le shader actuel du fond de coffre ; référence supprimée retirée, smoke et ouverture réelle vérifiés (plan 25).

**1er octobre, R6 livré :** radar/carte par biome, routes/eau, danger et légende ;
17 contrôles, build/smoke et captures ViewSonic vérifiés ([plan 04](04-interfaces-et-hub.md#r6-livré--1er-octobre-2026)). R5 personnages suit hors audio (DECISIONS §47).

**1er octobre, R5a livré :** Traqueur repris, 152 PNG déterministes, planches et animations Main vérifiées sur ViewSonic. Vagabond puis Forgeuse suivent (plan 08).

**1er octobre, R5b livré :** Vagabond repris, 152 PNG déterministes, planches et Main vérifiées. Forgeuse suit (plan 08).

**1er octobre, R5c livré :** Forgeuse reprise et vérifiée. Les trois personnages sont produits (456 PNG déterministes), captures Main sur ViewSonic et build/smoke verts ; recette artistique humaine ouverte (plan 08).

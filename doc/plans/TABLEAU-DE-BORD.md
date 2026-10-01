# Tableau de bord des plans

1er octobre 2026 · **Une page pour ne rien perdre.** Établi par recensement des 23 plans, du README et du registre. Pour les plans 02, 04, 07, 08, 10 et 19, le recensement a lu les en-têtes, les lots et les comptes rendus, pas le texte entier. À tenir à jour à chaque clôture de lot et à chaque décision : une ligne change ici avant tout autre document.

Le [registre](DECISIONS.md) garde les mots de Raphaël. Les plans gardent le détail. Ce tableau dit seulement **où en est chaque chose et qui doit agir**.

## 1. En cours

| Chantier | Plan | Prochaine action |
|---|---|---|
| **Retours du 1er octobre** (écran allégé, écrans de choix animés en pixel art, début de run plus tenable, coffres sans armes, minimap radar, bonus lâchés, icônes d'objets) | [24](24-retours-du-1er-octobre.md) | Validé ([DECISIONS §40](DECISIONS.md)) et **livré** : L1, L2 (avec la barre d'XP et le score aux éliminations), L3, L4, L7, L8, L9, L11, L12 (ascensions des 24 armes), L5/L6 côté code. Reste : la mise en scène du Mémorial dans le monde ; les images, au plan 25. À tester en jeu par Raphaël |
| **Sprites et design** (34 icônes d'objets, raretés, projectiles ennemis, bonus lâchés, HUD, menus) | [25](25-sprites-et-design.md) | Sur `sprites-plan25` : S4 et XP/crâne/minimap branchés et vérifiés. Valider les planches S1/S2/S3/S7/S8 avant intégration ; brancher bonus et sceaux aux systèmes du plan 24 encore absents de cette branche |
| **Gains qui se sentent** (objets à 30 niveaux, projectiles fractionnaires, cartes à la Megabonk, défense, difficulté, objets manquants, carte moins vide) | [23, plan d'exécution](23-plan-agent.md) | Décidé le 30 septembre ([DECISIONS §36–37](DECISIONS.md)). R0 à R4 livrés (mesure de référence ; bouclier de départ retiré, invulnérabilité 0,25 s ; cartes à la Megabonk ; objets à 30 niveaux, projectiles en plus au lieu des copies, paliers à 15 ; stats entières fractionnaires, pas d'armes relevés) ; R5 livré (PV ×1,25 d'emblée, pente 1,04 puis 1,075 après 6 min : temps pour tuer de R0 à ±20 %) ; R6 livré (huit objets de déclencheur) ; R7 livré (six petits lieux, carte de 12 800 px de haut, minimap : un petit lieu toutes les 26 s). R8 révisé livré (bonus d'une stat au hasard à chaque coffre, DECISIONS §38) ; R9 livré (Porte-monnaie hors quête, Repères). Plan 23 terminé ; travail sur `main` |
| Système de jeu | [21, référence unique](21-systeme-de-jeu.md) | G1, G2a et G2a-2 livrés, refaits au plan 23 R3 (30 niveaux, paliers à 15, 15 objets de propriété). G2b livré : anciens Dons retirés, Fragilité, coefficient, 8 objets de déclencheur. G2c livré au plan 23 R6 (8 de plus, 31 objets proposés) ; les 3 objets « monde » attendent le Reliquaire. G0 et G3 livrés : les 24 armes ont leurs deux voies d'ascension (étape 2 au plan 24 L12) |
| Carte à explorer | [22](22-carte-a-explorer.md) | C0, C1, C4 (six lieux) et C6 livrés : neuf petits lieux, un toutes les 26 s à eux seuls ; carte de 12 800 px de haut, minimap ; aucune Essence dépensée. Agent : C2 (Atelier), après confirmation de Raphaël (§11) |

## 2. Décisions attendues de Raphaël

| Sujet | Plan | Question |
|---|---|---|
| Carte à explorer | 22 §11 | Atelier et Trempe, service « niveau d'arme » déplacé du Mémorial, Atlas : pas encore confirmés un par un (Repères livrés, DECISIONS §38) |
| Catalogue d'objets | 21 §4 | 33 objets écrits avec effet par niveau et palier 25 : à relire, chiffres à régler en jeu. 22 sont en jeu |
| Invulnérabilité après un coup | 23 R1 | 0,25 s proposé au lieu de 0,5 s : à confirmer en jeu |
| Raretés, objets et menus | 25 S1/S2/S3/S7 | Planches inspectées disponibles : facettes des éclats, objets de trois quarts, fonds de choix et cadres patinés à valider avant branchement |
| Icônes des Réminiscences | 25 S8, 05 §15 | Direction « fragments teintés » retenue ; planche des 14 motifs prête, à valider avant branchement |
| Recette de la mort et du bilan | 02 | Durée de la séquence, densité de la page, échelle des distances |
| Déplacements | 01 | Inertie, invulnérabilité du dash, recette manette |
| Bestiaire | 07 | Tisseuse hors Marécages ; « mobs successifs » |
| Direction artistique | 08 | Traqueur peu lisible, double contour |
| Anomalies | 14 | Trois décisions, jamais arbitrées |
| Lore | 19 | Relecture du script v1.1 ; questions P1, P6 à P10 ; fin |
| Audio | 15 | Écoute en run ; quatre sons à reprendre |
| Récompense et puissance | 20 | Reprise après la pause ; à réaligner sur le plan 21 |
| Classement | 09 | Toutes les décisions, plus tard |
| Points à vérifier en jeu | `A-VERIFIER.md` | 54 cases non cochées |

## 3. Validé, pas encore fait

| Quoi | Plan |
|---|---|
| Objets : 6 emplacements, 30 niveaux (plan 23 R3), migration des passifs (lot G2) — **livré** | 21, 23 |
| Grammaire commune sur les cartes d'armes (G0), ascensions au niveau max (G3), affinités des personnages (G4) | 21 |
| Bannissements payés en Péril, fragments après les Résurgences | 21 — **livrés (G1)** |
| Déblocages par quêtes et achats en Vestiges | 21, 22 §5 C |
| Mobilités par personnage (lot E) | 01 |
| Trois personnages à intégrer, leurs sprites, leurs armes de départ (Sacoche de lettres, Fusil-harpon) | 06, 08 |
| Réécriture des textes de chargement, trop directs | 02, 19 |
| Butin qui disparaît avec sa zone | 16, repris par 22 §6 |
| Préchauffage des effets (5B) | 10 |
| Essai XP, niveaux de surplus, réserve automatique | 20 |

## 4. Idées en suspens, rattachées pour ne pas les perdre

| Idée | D'où elle vient | Rattachée à |
|---|---|---|
| Vestige figé, Pacte d'oubli, source de soin | 13 §4, §8 | 22 (lieux et Reliquaire) |
| Marchand ambulant, escorte, événements de biome | 12 §6 | 22 (épreuves) |
| Fragment de lore près des scènes-récits | 08 | 22 §3 (Table de pique-nique) et Atlas |
| Rémanence offensive, chemins rémanents | 11 A et C | 21 (personnages) |
| Éveil des armes | 17 vague 5 | 21 G3 : même idée que les ascensions |
| Établi de grand-père (une stat de plus par amélioration) | 21 §16 | 22 §5 A : devient la Trempe de l'Atelier |
| Pouvoir par personnage, taille des projectiles | 20 R7, R8 | 21 §3 et §8 |
| Délestage et Habitude | 05 perks §14 | 21 §7 : s'appliquent au Reliquaire |
| Comptoir du Hub, prix en Vestiges | 05 objets §3 | 22 §5 C (Atlas) et plan 04 |
| Porte-Nom, Rémanent, Glaneur | 07 | Bestiaire, plus tard |
| Défi hebdomadaire | 09 | Plus tard |

## 5. Plans remplacés ou absorbés

| Plan | Devenu |
|---|---|
| 05, perks B et catalogues d'objets | Plan 21. Les effets livrés restent ; les fiches restent la référence de chaque effet |
| 13, butin | Plan 22 (Reliquaire, lieux) |
| 11, idée B | Plan 22 §6 |
| 18, refonte des POI | Plan 22 §3 |
| 17, vague 4 et plafond d'armes | Plan 21 : niveau max 50, confirmé ([DECISIONS §32](DECISIONS.md)). Les mentions « 70 recommandé » des plans 05 et 20 sont périmées |
| Objets illimités, quatre passifs conservés ([DECISIONS §1](DECISIONS.md), 05 armes) | Périmés : 6 objets, traits supprimés ([DECISIONS §33](DECISIONS.md)) |

## 6. Plans sans action en attente

00 (référence), 03, 04, 10, 12, 16 : lots livrés, recettes à faire en jeu par Raphaël quand il le souhaite.

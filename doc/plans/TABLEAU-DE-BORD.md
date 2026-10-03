# Tableau de bord des plans

3 octobre 2026 (soir) · **Une page pour ne rien perdre.** Établi par recensement des 23 plans, du README et du registre. Pour les plans 02, 04, 07, 08, 10 et 19, le recensement a lu les en-têtes, les lots et les comptes rendus, pas le texte entier. À tenir à jour à chaque clôture de lot et à chaque décision : une ligne change ici avant tout autre document.

Le [registre](DECISIONS.md) garde les mots de Raphaël. Les plans gardent le détail. Ce tableau dit seulement **où en est chaque chose et qui doit agir**.

## 1. En cours

| Chantier | Plan | Prochaine action |
|---|---|---|
| **Sprites un cran en dessous** (personnages, projectiles du joueur trop semblables) | [25](25-sprites-et-design.md), [DECISIONS §60](DECISIONS.md) | Méthode choisie : pousser le générateur ; seuls les personnages joués et leurs animations sont visés (§60). Lots S1–S4. **S1 : planche des projectiles à valider** (`doc/plans/planches/25-s7-projectiles-joueur.png`). **S2 : direction des personnages à choisir** sur le Traqueur (`doc/plans/planches/25-s8-traqueur-directions.png`, C recommandé). |
| **Difficulté, coffres, raretés, écran de niveau, projectiles, objets** | [21 §16–17](21-systeme-de-jeu.md), [DECISIONS §60](DECISIONS.md) | **Validés en jeu le 3 octobre.** H3 et H1 gardés tels quels. |
| **Qualité du code et dette technique** | [26](26-qualite-et-dette-technique.md), [DECISIONS §54–56, §58](DECISIONS.md) | **Q0 et Q1 livrés et vérifiés** : lanceurs stricts, bancs réparés ; F1/F4 réservés au dev, outils et hooks absents des exports Debug/Release, provenance des essais (5/5 suites, [preuves Q1](../audits/qualite-2026-10-02/q1/)). **Q5 livré et vérifié** le 3 octobre : motifs d'attaque et familles d'armes typés, clé inconnue refusée au chargement ([preuves](../audits/qualite-2026-10-02/q5/)). Suite proposée : Q6a (effets d'armes). |
| **Retours du 2 octobre après-midi** (rythme, ennemis, raretés, objets, projectiles, impacts) | [21 §16](21-systeme-de-jeu.md), [DECISIONS §53](DECISIONS.md) | F1–F6 livrés : niveaux un peu ralentis, ennemis plus solides, raretés hautes liées à la Chance, règles chiffrées des objets, icônes des objets qui agissent, projectiles lisérés et agrandis, impacts plus marqués. **Captures à refaire écran allumé** (F3 à F6) et banc FPS en foule |
| **Équilibrage du 2 octobre** (XP trop rapide, raretés, cartes, ennemis) | [21 §15](21-systeme-de-jeu.md), [DECISIONS §50](DECISIONS.md) | E0 à E4 livrés et mesurés : bug de carte corrigé et rareté teintée sur toute la carte ; XP ralentie et G6c annulé ; raretés hautes divisées par deux ; PV des ennemis relevés (temps pour tuer stable vers 0,45 s jusqu'à 25 min) et tireurs à un tiers. **À jouer par Raphaël** |
| **Retours du 1er octobre, soir** (projectiles, vol de vie, level-up, XP/Chance, sprites d'armes adaptatifs) | [21 §14](21-systeme-de-jeu.md), [DECISIONS §48–49](DECISIONS.md) | Décidé le 2 octobre (§49). Lots G6a à G6f découpés au plan 21 §14 ; G6a, G6b et G6c livrés (nombre pour les 24 armes, vol de vie, Chance et XP renforcés) ; G6d mesuré (courbe inchangée, trois options au plan 21) ; G6e livré (visuels à la taille) ; planche G6f (paliers) refusée le 3 octobre (§59) ; **G6g livré** : un projectile de plus se voit à l'écran (rafale sur une même cible, ondes successives en cercle, fronts au Transistor). À juger en jeu. |
| **Nouvelle recette après intégration des sprites** | [24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026), [DECISIONS §44/47](DECISIONS.md) | **R1/R2, R3b, R5a–c et R6 livrés et vérifiés** : interfaces, cadence du Hurleur, trois personnages et carte par biome. Recette humaine du combat et du visuel à faire ; banc FPS de la carte sur machine calme. Rotation des coffres conservée. Suivi audio indépendant sur la ligne dédiée. |
| **Effacement : cellules actives** | [10 6C](10-terrain-et-tiles.md) | **Livré et vérifié** : états et signaux identiques, recalculs −23,8 % sur le scénario de 30 min ; 72 contrôles, captures ViewSonic et run réelle de 30 min. Index +157,5 Kio pour 6 753 cellules. FPS à mesurer au calme. |
| **Préchauffage rendu** | [10 5B1](10-terrain-et-tiles.md) | **Livré et vérifié** : 16 shaders + lueur d’XP soumis puis viewport libéré ; banc Main réparé, 26 contrôles verts. 5B2 : cache froid/chaud et premiers effets, machine calme requise. |
| **Écrans de choix : relances et entrées** | [04 R7](04-interfaces-et-hub.md) | **Livré et vérifié** : titre/actions/fond restaurés à la réouverture, premier appui sans récompense ; 24 contrôles et captures Main sur ViewSonic. |
| **Audio et identité des Résurgences** | [15](15-audio.md), 24 §12 | **A0 et A1 livrés**, banque inchangée : enregistrement de référence et rapport (`tools/record_run_audio.sh`), musique pilotée par intention (annonce tenue, accalmie, endgame, première run), fondus corrigés, `tools/test_music.sh` vert. **À écouter** : avant/après et planche A2 (points d'entrée des morceaux) hors dépôt. À décider : rôle exploration/combat avec une foule permanente. Puis A2 (identité) et A3 (mix : 12 sons/s, 64 voix coupées/min). |
| **Retours du 1er octobre** (écran allégé, écrans de choix animés en pixel art, début de run plus tenable, coffres sans armes, minimap radar, bonus lâchés, icônes d'objets) | [24](24-retours-du-1er-octobre.md) | Validé ([DECISIONS §40](DECISIONS.md)) et **livré** : L1, L2 (avec la barre d'XP et le score aux éliminations), L3, L4, L7, L8, L9, L11, L12 (ascensions des 24 armes), L5/L6 côté code, L6b (réveil du Mémorial et de la Faille dans le monde). Images du plan 25 intégrées sur `main`. Plan 24 terminé côté code ; à tester en jeu par Raphaël, son de la fusion à juger à l'écoute |
| **Sprites et design** (34 icônes d'objets, raretés, projectiles ennemis, bonus lâchés, HUD, menus) | [25](25-sprites-et-design.md) | **Intégré sur `main`** à la demande de Raphaël (DECISIONS §41) : raretés, objets, Réminiscences, projectiles, bonus, HUD, menus et chargement. Captures inspectées. Les images des contenus futurs sont visibles dans la Collection avec « À venir » ; leurs règles restent aux plans 05/22. |
| **Gains qui se sentent** (objets à 30 niveaux, projectiles fractionnaires, cartes à la Megabonk, défense, difficulté, objets manquants, carte moins vide) | [23, plan d'exécution](23-plan-agent.md) | Décidé le 30 septembre ([DECISIONS §36–37](DECISIONS.md)). R0 à R4 livrés (mesure de référence ; bouclier de départ retiré, invulnérabilité 0,25 s ; cartes à la Megabonk ; objets à 30 niveaux, projectiles en plus au lieu des copies, paliers à 15 ; stats entières fractionnaires, pas d'armes relevés) ; R5 livré (PV ×1,25 d'emblée, pente 1,04 puis 1,075 après 6 min : temps pour tuer de R0 à ±20 %) ; R6 livré (huit objets de déclencheur) ; R7 livré (six petits lieux, carte de 12 800 px de haut, minimap : un petit lieu toutes les 26 s). R8 révisé livré (bonus d'une stat au hasard à chaque coffre, DECISIONS §38) ; R9 livré (Porte-monnaie hors quête, Repères). Plan 23 terminé ; travail sur `main` |
| Système de jeu | [21, référence unique](21-systeme-de-jeu.md) | G1, G2a et G2a-2 livrés, refaits au plan 23 R3 (30 niveaux, paliers à 15, 15 objets de propriété). G2b livré : anciens Dons retirés, Fragilité, coefficient, 8 objets de déclencheur. G2c livré au plan 23 R6 (8 de plus, 31 objets proposés) ; les 3 objets « monde » attendent le Reliquaire. G0 et G3 livrés : les 24 armes ont leurs deux voies d'ascension (étape 2 au plan 24 L12) |
| Carte à explorer | [22](22-carte-a-explorer.md) | C0, C1, C4, C6 livrés ; **C2a–b livrés le 2 octobre** : Atelier (4 par carte), Trempe, forge d'arme, Retrempe. C2c mesuré puis réglé le 3 octobre : 8 Ateliers, forge 150, Retrempe 100 ; 1 à 3 Ateliers croisés en 15 min, 0 à 450 Essence dépensée sur 1 500 à 3 600. Le Mémorial ne garde que soin et levée d'Oubli : à juger en jeu |

## 2. Décisions attendues de Raphaël

| Sujet | Plan | Question |
|---|---|---|
| Recette de la mort et du bilan | 02 | Durée de la séquence, densité de la page, échelle des distances |
| Bestiaire | 07 | Recette du Hurleur validée (§49). Tisseuse hors Marécages, rare : livrée le 3 octobre (Forêt et Champs, 8 % des Résurgences). « Mobs successifs » ouvert. |
| Direction artistique | 08 | Traqueur, Vagabond et Forgeuse repris et vérifiés (R5a–c) ; appréciation artistique de Raphaël encore ouverte. |
| Lore | 19 | Relecture du script v1.1 ; questions P1, P6 à P10 ; fin |
| Audio | 15 | Écoute en run et choix restants ; chantier élargi à toute l'identité sonore, avec les Résurgences comme premier cas proposé (§44). |
| Réveil du Mémorial et de la Faille | 24 L6b, 15 | Durée gardée ; son à changer (§49), à traiter avec l'audio |
| Classement | 09 | Toutes les décisions, plus tard |
| Mémorial réduit au soin | 22 C2c | À juger en jeu (§57). Ateliers à prix ×5 et 8 par carte livrés le 3 octobre (1 à 3 croisés en 15 min) : à juger en jeu aussi. |
| Audio, mort, lore, classement | 15, 02, 19, 09 | L'audio reprend après H et Q1 (§57) ; les autres ensuite |
| Points à vérifier en jeu | `A-VERIFIER.md` | 55 cases non cochées |

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
| Coût à froid/chaud des effets (5B2), après la soumission au rendu livrée en 5B1 | 10 |
| Essai XP, niveaux de surplus, réserve automatique | 21 (absorbé du plan 20, §57) |
| Anomalies : Écho de ta dernière run, Oubli de soi, Effondrement ; pénalité ≤ 25 % des PV max ; une ligne de texte (§49) | 14 |
| Atelier et Trempe, niveau d'arme déplacé, Atlas (§49) | 22 C2 |

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
| 20, récompense et puissance | Plan 21 (DECISIONS §57) : essai d'XP, niveaux de surplus et réserve automatique y sont rattachés ; le reste est livré ou dépassé par les lots E, F et H |
| 05, perks B et catalogues d'objets | Plan 21. Les effets livrés restent ; les fiches restent la référence de chaque effet |
| 13, butin | Plan 22 (Reliquaire, lieux) |
| 11, idée B | Plan 22 §6 |
| 18, refonte des POI | Plan 22 §3 |
| 17, vague 4 et plafond d'armes | Plan 21 : niveau max 50, confirmé ([DECISIONS §32](DECISIONS.md)). Les mentions « 70 recommandé » des plans 05 et 20 sont périmées |
| Objets illimités, quatre passifs conservés ([DECISIONS §1](DECISIONS.md), 05 armes) | Périmés : 6 objets, traits supprimés ([DECISIONS §33](DECISIONS.md)) |

## 6. Plans sans action en attente

00 (référence), 03, 12 : lots livrés, recettes à faire en jeu par Raphaël quand il le souhaite. Les lots de reprise du plan 04 sont livrés ; recettes humaines et banc FPS de la carte restent ouverts. Le plan 10 garde ses suites mesurées de performance (5B2, 3B, 6D). Au plan 16, les effets du monde sont livrés ; la durée de vie du butin dans les zones oubliées reste au plan 22 §6 (19 782 orbes au sol dans la run de 30 min du lot 6C).


**Continuation hors audio (DECISIONS §47) :** R3b livré au plan 07 : cadence du Hurleur réglée, 61 contrôles et captures vérifiés, ressenti humain ouvert. Suite : préchargement à réparer, R6 carte, R5 trois personnages. Le chantier audio est exclu de cette continuation.

**1er octobre, R6 livré :** radar/carte par biome, routes/eau, danger et légende ;
17 contrôles, build/smoke et captures ViewSonic vérifiés ([plan 04](04-interfaces-et-hub.md#r6-livré--1er-octobre-2026)). R5 personnages suit hors audio (DECISIONS §47).

**1er octobre, R5a livré :** Traqueur repris, 152 PNG déterministes, planches et animations Main vérifiées sur ViewSonic. Vagabond puis Forgeuse suivent (plan 08).

**1er octobre, R5b livré :** Vagabond repris, 152 PNG déterministes, planches et Main vérifiées. Forgeuse suit (plan 08).

**1er octobre, R5c livré :** Forgeuse reprise et vérifiée. Les trois personnages sont produits (456 PNG déterministes), captures Main sur ViewSonic et build/smoke verts ; recette artistique humaine ouverte (plan 08).

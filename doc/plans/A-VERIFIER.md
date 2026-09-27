# À revérifier sur la machine de Raphaël

Fichier temporaire. Il liste ce qu'une session cloud a livré sans pouvoir le vérifier complètement. Le conteneur avait Godot 4.7.2 et .NET 10, mais pas de GPU : rendu logiciel à environ 5 FPS, et aucune mesure de FPS n'y a de sens. Le build, le smoke test, les régressions et les captures ont bien été faits. Chaque point ci-dessous est à cocher par le Claude local, ou par Raphaël en jeu, puis à retirer. Supprimer le fichier quand il est vide.

## Plan 10 T3 — chemins et routes

- [ ] **Coût GPU des chemins.** Environ 80 rubans maillés avec un shader par pixel (bruit, tramage, oubli). Lancer `tools/bench_ab.sh <commit d'avant T3> <dossier>` machine calme ; le banc se déroule dans les champs et la carrière, donc sur des chemins.
- [ ] **Temps de génération** : 257 ms de calcul CPU au chargement dans le conteneur (`[PathNetwork]` dans le log). Vérifier sur le Mac que l'écran de chargement ne s'allonge pas de façon sensible.
- [ ] **Recette visuelle en jeu** : largeur des chemins (champs 46 px au sol, forêt et marais 26 px, carrière 36 px), contraste des ornières, fréquence des chemins (un arbre couvrant plus 30 % de boucles). Les captures sont dézoomées ou au zoom ×2 ; le rendu à la taille réelle de l'écran de Raphaël peut différer.
- [ ] **Rues verticales** : une seule chaussée désormais (une tile par parité de rang). Vérifier les carrefours en T et en croix, et la position des voitures garées sur les rues verticales (recentrées de ±16 px).
- [ ] **Bordures de trottoir** : lisibles ou trop discrètes ?

## Plan 16 O6 — échos

- [ ] **Fréquence et lisibilité en vraie partie** : premier écho à 60 s, puis toutes les 45 à 90 s. Il n'apparaît qu'en zone Fragile ou Effilochée, donc pas avant que l'Effacement ait progressé. Trop rare, trop discret ?

## Plan 08 P4b — fermes

- [ ] **Recette visuelle** : 16 fermes sur la seed de capture, toutes sur le même plan retourné. La répétition se voit-elle en jeu ? Faut-il des plans variés ou moins de fermes ?
- [ ] **Collisions** : maison, grange, silo, hangar, puits, tracteur et remorque bloquent (emprise du manifeste) ; clôtures, haies et portail ne bloquent pas. Vérifier avec `--capture-props` et en jeu qu'on ne reste pas coincé dans la cour ou derrière le hangar.
- [ ] **Coût** : environ 30 décors de plus par ferme, dont 3 à 4 grands sprites. Mesure `--measure-props` dans les champs à refaire machine calme. Placement : 273 ms au chargement dans le conteneur, à vérifier sur le Mac.

## Plan 02 J0 — effets de mort recyclés

- [ ] **Rendu des morts en jeu** : les éclats de désintégration passent de particules GPU (dérive lente vers le haut) aux `PixelSparks` (famille `Void`, trajets droits). À comparer en vraie partie : la mort reste-t-elle aussi lisible ? Nuage et flaque doivent être identiques à avant.
- [ ] **Gerbe de collecte d'XP** : 4 éclats `Essence` au lieu de la gerbe bleu clair. Couleur à vérifier.

## Plan 07 §7 — perception par créature

- [ ] **Mesure de densité avant/après** (`tools/measure_density.sh`, deux seeds) sur le Mac : celle du conteneur n'est pas concluante, le bot y est irrégulier à 5 FPS. Commit de référence : le parent de « perception et laisse par créature ».
- [ ] **Ressenti** : les Ombres et Charognards doivent coller davantage, les créatures lentes décrocher plus vite.

## Plan 10 T2 — décors de transition

- [ ] **Densité** : 2 292 décors de plus par carte (7 % des cellules proches d'une frontière). Coût à vérifier avec `--measure-props`, et lecture : les frontières paraissent-elles plus naturelles ou plus encombrées ?

## Plan 16 O2 — éclats des décors en zone Effacée

- [ ] **Densité et lisibilité** : deux décors par tick de 0,15 s, trois éclats violets chacun. Dans le conteneur à 5 FPS, la capture n'en montre que quelques-uns. À juger à 60 FPS : trop discret, ou assez pour dire « ici, tout s'en va » ?

## Plan 08 P4b-3 — bords de parcelles et vergers

- [ ] **Lecture des parcelles** : les haies (70 % des bords échantillonnés) forment-elles des limites lisibles, ou des morceaux épars ? Réglage dans `WildFieldsLayoutGenerator` (`HedgeChance`, échantillonnage par axe).
- [ ] **Vergers** : les arbres bloquent (tronc), avec deux colonnes d'écart. Vérifier qu'on circule sans accrocher en combat.

## Plan 02 J1 — impact

- [ ] **Chiffres de dégâts** : Saira cernée, cumul par cible sur 0,25 s. À 60 FPS, avec une arme rapide (fouet, haches), le cumul se lit-il comme « ça monte » ou masque-t-il le rythme des coups ? Réglages en tête de `DamageNumber` (`MergeWindowSec`, `MaxHoldSec`, tailles).
- [ ] **Recul** : désormais sur le sprite seul (3 px). Le corps ne bouge plus au coup ; vérifier que l'impact reste aussi senti qu'avant.


## Plan 02 J0 — budget d'effets par frame

- [ ] **Plafonds** (`data/scaling/fx_budget.json`) fixés sans mesure à 60 FPS : dans une grosse vague fauchée (J5), vérifier qu'on ne voit pas de « trous » (morts sans nuage) et que les FPS tiennent mieux. Le banc écrit `fx_dropped`.

## Plan 08 P4b-4 — tracteur embourbé

- [ ] **Collision** : la scène bloque selon l'emprise du tracteur non incliné. Vérifier avec `--capture-props` qu'elle couvre la carrosserie sans déborder sur la mare.
- [ ] **Ornières** : elles se lisent comme une traînée sombre derrière le tracteur. Garder, allonger, ou retirer ?

## Plan 08 P2 — église et pylône

- [ ] **Rareté** : au plus deux de chaque par carte (8 % et 10 % des îlots éligibles). Assez pour s'orienter ? Trop peu pour être remarqués ?
- [ ] **Pylône** : planté dans la cour, il bloque selon son emprise (1,1 m autour des pieds, plus le local technique hors emprise). Vérifier avec `--capture-props`.

## Plan 02 J2 — morts

- [ ] **Dissolution orientée** : dans le conteneur, la dissolution ne se voit qu'au ralenti. À 60 FPS, le sens du coup se lit-il ? Réglage : `vertical_bias` du shader d'entité (0,35) pèse le balayage face au bruit.
- [ ] **Saut des orbes** : 0,35 s sans attraction. Gêne-t-il le ramassage en pleine vague ?

## Plan 02 J3 — collecte

- [ ] **Chaîne sonore** : à écouter. La montée (+3,5 % par orbe, 14 crans) est-elle agréable, ou trop aiguë en fin de chaîne ? Réglages en tête de `XpOrb`.
- [ ] **Traînée des orbes** : lisible, ou brouillon quand vingt orbes convergent ?

## Plan 02 J4 — montée de niveau

- [ ] **Moment de l'effet** : l'écran de choix met en pause dès la montée de niveau, donc onde, colonne et poussée se jouent au retour dans la run. Est-ce le bon moment, ou faut-il qu'elles se voient derrière le voile, animées pendant la pause ?

## Plan 02 J5 — rouler sur la game

- [ ] **Recul de caméra** : 8 % au plus. Perceptible sans gêner ? Il élargit aussi la zone où les créatures sont visibles, donc le rythme d'apparition hors écran (le `SpawnManager` lit le zoom courant).
- [ ] **Compteur de rafale** : légende « EN RAFALE ». Position sous la plaque de vie à valider, ainsi que le seuil (5 morts, 1,5 s).

## Plan 02 J6 — micro-interactions

- [ ] **Poussière de pas** : à regarder en jeu (herbe, béton, eau). Trop discrète ? Réglages dans `FootstepFx`.
- [ ] **Coffres qui frémissent** : 3,4° au plus, par saccades. Assez pour attirer l'œil sans agacer ?

## Plan 02 lot A — score

- [ ] **Scores comparés** : l'horloge n'avance plus pendant les pauses. Une run d'aujourd'hui marque donc un peu moins de points de survie qu'avant à durée murale égale, et les records anciens restent légèrement avantagés.
- [ ] **Barème des points d'intérêt** : `pois.json` prévoit 25 à 300 points selon le type, mais le jeu en donne 50 partout. Faut-il brancher les valeurs des données ?

## Plan 02 lot D — bilan de fin de run

- [ ] **Recette du bilan** : composition, textes (« LA ROUTE S'EFFACE », « Dernier coup »), rythme de la révélation (2,2 s après 1,1 s de pâleur) et délai des boutons. Captures : `CAPTURE_EXTRA_ARGS="--capture-death" tools/capture_run.sh <dossier> 8 30`.
- [ ] **Manette** : focus sur « Rejouer », gauche/droite entre les deux boutons. Non testé au clavier réel dans le conteneur.

## Plan 04 C2 — Collection

- [ ] **Recette** : place de l'entrée dans le menu (après « Partir »), lisibilité des silhouettes verrouillées, textes du panneau. Capture : `tools/capture_hub.sh <dossier> ui_down,ui_accept`.

## Plan 03 lot C — Résurgence et accalmie

- [ ] **Rythme** : oubli ×2,5 pendant une crise de 70 s. La fin de run arrive-t-elle trop vite ? Le late game se déclenche à 68 % d'oubli global.
- [ ] **Coffre d'accalmie** : trouvé naturellement devant soi, ou manqué faute de repère ?

## Fusion de main (plans 17 et 18) — 27 septembre

Apports de la nuit reportés sur les versions de main ; build, smoke test et régressions (mouvement avec intégration, capacités ennemies, mode dev, armes) verts, rien regardé en rendu.
- [ ] **Coffre qui frémit** (J6) : désormais sur le sprite de main, qui pivote sur sa base au sol. Code intact (plafond 3,4°, soit 2° à 45 px) ; la capture `--capture-micro` n'échantillonne que 12 frames et relève 0,72°, trop peu pour juger une saccade : à regarder en jeu.
- [ ] **O5** : c'est maintenant le réveil d'un Mémorial qui rappelle sa zone (vérifié par le banc, pas en jeu).

## Passage du 27 septembre sur le Mac (Claude local)

Captures en vraie run regardées : bilan, level-up (cartes et effet), coffre, pas, chemins, fermes, repères, échos.
- Vérifiés et retirés de la liste : bilan après la fusion, entrée du level-up, capture des repères, murmure en français.
- Corrigés : murmure des échos flou (texte de 9 px agrandi par la caméra, désormais en Saira rastérisée au double puis réduite) ; brèche du toit de l'église (bord en tuiles cassées, chevrons visibles).
- Vu, à juger par Raphaël : raccord ville-champs de la seed de capture, où un bout de rue verticale s'arrête dans la terre (`path-raccord-ville.png`).
- Non fait : mesures de coût (bancs A/B, `--measure-props`), machine chargée au moment du passage (charge 7 à 16).


## Session locale du 27 septembre — à juger en jeu par Raphaël

- [ ] **Mesures de coût, machine calme** (charge restée entre 3 et 16 toute la session) : `tools/bench_ab.sh 2e50f59^ <dossier>` lancé depuis un worktree au commit `2e50f59` pour les chemins T3 ; `--measure-props` pour les décors du marais (1 062 → 1 754 sur la seed de capture) et de la carrière (1 145 → 1 232).
- [ ] **Brute du Vide** : sa charge avance enfin (200 px/s pendant 0,8 s, à moins de 200 px), désormais annoncée 0,6 s par un couloir violet et suivie de 0,9 s de récupération (07 lot B étape 1). Trop punitive, ou trop facile à éviter ? Réglages dans le bloc `abilities.charge` de `data/enemies/void_brute.json`.
- [ ] **Rampant et Hurleur** (07 lot B étape 1) : surgissement annoncé par un cercle rouille (0,6 s), plus de dégâts enfoui ; cri du Hurleur annoncé 0,8 s, interrompu si on le tue. Lisible en pleine mêlée ?
- [ ] **Recul des armes** : Parcmètre, Cloche, Râteau, Chronomètre repoussent vraiment (valeur `knockback` = pixels). Plaisant, ou le début de run devient-il trop facile ?
- [ ] **Couleurs signature des armes** (plan 17 lot 2B) : chaque arme a la couleur de son icône dans ses effets.
- [ ] **Arme en main** (plan 17 lot 2C) : Paramètres › Graphismes › « Arme en main (essai) », désactivée par défaut. Garder, régler ou abandonner ?
- [ ] **Marais et carrière refaits** (plan 08 P5, P6) : échelle, lisibilité, densité ; les grandes machines de la carrière restent rares.
- [ ] **Synergies de perks** annoncées sans effet (plan 05, fin) : les retirer ou les implémenter ?
- [ ] **Typographie de l'interface** (04 lot B) : tailles légèrement relevées (lore de la pause 12 → 14 px, corps 15 → 16…). Plus lisible en 720p ; trop gros en 1080p ? Réglage « Taille du texte » à 115 et 130 % : utile, ou à retirer ?
- [ ] **Chantiers de la carrière** (08 P6b) : un par région (6 sur la seed de capture). Se lisent-ils comme une mine abandonnée ? Trop vides au sud, trop répétitifs ? Captures : `CAPTURE_EXTRA_ARGS="--capture-quarries" tools/capture_run.sh <dossier>`.
- [ ] **Tirs annoncés** (07 lot B) : couloir de visée avant chaque tir du Cracheur, de la Sentinelle et de la Tisseuse ; contour de portée de la Sentinelle. Trop d'indications à l'écran en pleine vague ? Tireurs devenus trop faciles ?
- [ ] **Présage des Résurgences** (03 lot C) : bords ternis en trame pendant l'avertissement et la crise, créatures agitées. Assez fort, trop fort ? Coût de la passe plein écran en combat dense de crise à mesurer (`/bench`).

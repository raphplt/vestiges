# Plan 37 — Retours du 11 octobre : Hub, interface de run, visuels du monde

Statut : **décidé le 11 octobre ([DECISIONS §117](DECISIONS.md)), lot H1 engagé** · Priorité : P1 · Dépendances : plan 04 (refonte des menus), 35 (inventaire), 36 (Campement), 06 (quêtes).

Raphaël a joué les livraisons du 9 au 11 octobre (refonte des menus, Campement, inventaire, audio, difficulté). Il juge l'ensemble bon (« le son c'est beaucoup mieux », « la courbe de difficulté elle est mieux », menus « dans la globalité, c'est cool ») et liste ce qu'il reprendrait. Ce plan regroupe ces retours en trois chantiers, dans l'ordre qu'il a choisi : **Hub, puis run, puis monde**.

## 1. Constats, vérifiés dans le code et en capture

Captures du Hub regardées le 11 octobre (`tools/capture_hub.sh`, profil dev, 1080p).

| Retour | Constat |
|---|---|
| Onglets Quêtes/Collection sur la sélection | Chaque page du Hub reçoit la même barre de trois onglets (`HubScreen.AddPage`). Le Campement n'y figure même pas : sur sa page, la barre s'affiche sans onglet actif. |
| Bouton Partir peu visible | Bouton primaire de 68 px de haut, perdu au milieu du panneau de fiche, rien ne le distingue de « Détails du personnage » sinon une arête dorée. |
| Liste des personnages | `MenuSkin.StyleLink` : aucun cadre ; survol et focus = fond doré à 7 % et trait gauche de 3 px ; sélection marquée par une simple teinte grise des autres. |
| Quêtes | Trois filtres Toutes / En cours / Accomplies (alors que §98 demandait déjà de ne plus séparer) ; cartes de 236 × 244 px, quatre par rangée, deux rangées visibles. |
| Collection | Bouton « Valeurs et règles » qui déplie une ou deux lignes. |
| Réminiscences toutes ouvertes | Aucun déblocage méta : `UnlockKind` ne connaît que personnage, arme, objet, Écusson. Les 7 Réminiscences dont l'effet est codé sont proposées d'office ; 7 autres sont « À venir » (5 sans données). |
| Icône « Mémoire vive » | `perk_icon_double_impact.png` : éclat rouge générique et deux petits éclairs, rien qui dise « le coup se répète ». |
| Campement incompréhensible | Chaque ligne montre « +0 % → +5 % » sans dire de quelle stat il s'agit ; la phrase d'explication n'apparaît qu'au survol, posée sur le mur de pierre du bas, presque illisible. |
| Pas de curseur custom | Aucun `SetCustomMouseCursor` ; seule une forme « main » sur les boutons. |
| Pages qui mènent partout | Conséquence de la barre d'onglets commune. |
| Chargement qui saccade | Deux étapes synchrones de ~0,8 s sans image (`SpawnEnvironmentProps` 807 ms, `BuildPropOcclusion` 747 ms) et le préchauffage des shaders (883 ms). |
| Minimap basique | Radar carré de 104 unités HUD, 26 cellules, brouillard découpé en disque net de 12 cellules. |
| Menus de run plein écran | Depuis la refonte, `ChoiceStyle.OverlayColor` est opaque (avant : voile à 75–80 %) et la pause pose un fond opaque (avant : 84 %). Le jeu ne se voit plus. |
| HUD du haut | « Péril 0 » collé au chronomètre ; score et éliminations dans une plaque, éliminations en taille 10 ; plaque PV/niveau en encadré. |
| Barre du bas | Armes 72 px, Écussons 36 px, sac 26 px, tout au même endroit. |
| Cercle doré de la Veillée | `RunEventMarker` trace ses anneaux en `DrawLine` à la résolution de l'écran, épaisseurs 1,5 à 3 et pulsation continue : rendu vectoriel lisse, hors grille pixel art. Le faisceau, lui, est déjà en pixel art. `PixelGroundRing` existe et fait ce qu'il faut. |
| Transitions de biomes | `ground.gdshader` tire chaque amas de 2 × 2 pixels au hasard près de la frontière : des pixels épars dans tous les sens. |
| Routes trop droites | `RoadTileGenerator` dessine des bandes alignées sur l'écran ; le bord haut d'une rue horizontale est une droite répétée de tuile en tuile. |
| Ennemis en retrait | Rôdeur, Charognard, Présage : pilote du 24 septembre ; huit autres repris le 27 septembre ; Tisseuse encore issue de l'ancien générateur de mars. Héros, décors et tuiles ont été repris depuis. |

## 2. Décisions de Raphaël (§117)

- **Réminiscences** : 3 ouvertes au départ, 4 par des quêtes ; **préparer aussi les quêtes des 7 « À venir »**, actives quand leur effet sera codé.
- **Quêtes** : une seule liste en **lignes compactes** (icône, nom, étoiles, objectif court, progression) ; accomplies en doré pâle avec la médaille, en fin de liste ; fiche détaillée à droite.
- **Curseur** : flèche pixel art pierre et or, main au survol ; même curseur au Hub et dans les menus de run, caché en combat.
- **Collection** : plus de bouton, règles et chiffres affichés sous la description, la fiche défile si besoin.
- **Fond des menus de run** : jeu **flouté et assombri**, bords rongés par l'Effacement ; panneaux en pierre inchangés.
- **HUD du haut** : plus d'encadrés, **texte posé avec un contour sombre** ; Péril à droite près du score ; éliminations plus grandes ; barre de PV fine, niveau en grand chiffre.
- **Barre du bas** : Écussons en grandes cartes au-dessus des armes ; objets du sac en **colonne au bord gauche**, sans cadre.
- **Minimap** : **ronde, façon boussole** (repères cardinaux), zoom ×1,5, brouillard tramé et irrégulier.
- **Routes** : mêmes axes, **bords dessinés en perspective iso** (trottoir avec épaisseur, bordure cassée, raccords en diagonale aux croisements) ; planche avant intégration.
- **Transitions** : **lisière organique en amas** (plaques franches qui suivent un bruit doux, bande plus large), même shader.
- **Ennemis** : Rôdeur, Charognard, Présage et Tisseuse d'abord, planche avant/après sur le sol réel, puis les huit autres.
- **Ordre** : Hub, puis run, puis monde. Le chargement est corrigé avec le chantier « run ».

Retenu sans question (demandes explicites) : sélection des personnages seule après Partir ; pages Campement, Quêtes, Collection séparées, sans onglets vers les autres ; liste des personnages encadrée, survol et sélection habillés ; bouton Partir plus visible ; sous-texte explicatif sur chaque ligne du Campement ; icône de Mémoire vive refaite ; menus de pause, coffre et level-up gardés tels quels au centre (« très bien fait, assez stylé »).

## 3. Lots

Un lot à la fois, capture regardée avant de conclure.

### Chantier Hub

| Lot | Contenu | Vérification |
|---|---|---|
| **H1** Pages séparées et sélection | Barre d'onglets retirée de toutes les pages ; Retour ramène à l'accueil. Liste des personnages en cartes encadrées (pierre, portrait dans un médaillon), survol éclairé et sélection marquée par une arête dorée et une plaque plus claire ; fond de liste détaché du mur. Bouton Partir agrandi, en bas de la fiche, doré plein. | Captures sélection/Campement/Quêtes/Collection ; suite `menu_navigation`. |
| **H2** Quêtes en liste unique | Filtres retirés ; lignes compactes, accomplies en fin de liste en doré pâle ; fiche à droite inchangée. | Capture profil neuf et profil dev ; suite `quests`. |
| **H3** Collection et Campement lisibles | Collection : règles et chiffres affichés directement, défilement de la fiche. Campement : sous-texte d'une ligne sous chaque nom (ce que fait l'amélioration), ligne du bas retirée. | Captures des deux pages. |
| **H4** Réminiscences par quêtes | `UnlockKind.Reminiscence`, réserve de départ de 3, 4 quêtes, quêtes préparées des 7 à venir (masquées tant que l'effet n'est pas codé) ; offres en run filtrées ; icône de Mémoire vive refaite. | Suites `quests`, `perk_acquisition`, `perk_contracts` ; Collection en profil neuf. |
| **H5** Curseur | Deux curseurs pixel art (flèche, main), appliqués au Hub et aux menus de run, cachés en combat. | Capture du Hub au survol. |

### Chantier run

| Lot | Contenu |
|---|---|
| **R1** Fond flouté | Un fond commun (copie de l'écran, flou, assombrissement, bords rongés) pour level-up, coffre, pause, Mémorial, Faille, Atelier, échange. Shader ajouté au préchauffage. Banc : pas de coût hors menu. |
| **R2** HUD du haut | Texte posé avec contour ; Péril à droite près du score ; éliminations agrandies ; plaque PV/niveau refaite. |
| **R3** Barre du bas | Écussons en grandes cartes, objets en colonne à gauche. |
| **R4** Minimap boussole | Ronde, cadre de pierre, repères cardinaux, zoom ×1,5, brouillard tramé. |
| **R5** Chargement fluide | Pose des décors et des tronçons d'occlusion par tranches, comme les tuiles ; mesure de la pire image avant/après. |

### Chantier monde

| Lot | Contenu |
|---|---|
| **M1** Cercles pixel art | `RunEventMarker` passe par `PixelGroundRing` (ou sa logique) ; revue des autres tracés lisses (anneau de dash, marqueur de cible, fosse et éclair de l'Indicible). |
| **M2** Transitions | Lisière en plaques organiques dans `ground.gdshader` ; captures des cinq frontières. |
| **M3** Routes | Planche des bords en iso, puis `RoadTileGenerator`. |
| **M4** Ennemis | Planche avant/après : Rôdeur, Charognard, Présage, Tisseuse ; puis les huit autres. |

## 4. Lot H1 livré : pages séparées, sélection des personnages — 11 octobre 2026

- **Pages séparées** : la barre d'onglets Personnages / Quêtes / Collection est retirée de toutes les pages (`HubScreen.AddPage`). Chaque page porte un titre doré (« Choisir un personnage », « Campement », « Quêtes », « Collection ») et son Retour. Les liens contextuels restent (quête d'un personnage verrouillé, récompense d'une quête), avec leur retour à l'origine.
- **Liste des personnages** dans son propre cadre de pierre. Chaque personnage est une carte de pierre ; son sprite repose dans un médaillon sombre cerclé. La carte examinée (survol ou focus) prend la tranche dorée des dalles de choix, et son médaillon un cercle doré. Fini le voile à 7 % sans contour.
- **Partir** : dalle de bronze à arête dorée, 92 px de haut, en texte de titre (`MenuSkin.StyleCallToAction`). Il est posé au pied de la fiche, hors du défilement, toujours au même endroit. Son focus l'entoure sans le remplacer.
- **Vérifié** : build sans avertissement ; captures de la sélection et des pages regardées ; `tools/validate.sh` smoke et `menu_navigation` 2/2.

## 5. Lot H2 livré : Quêtes en liste unique — 11 octobre 2026

- **Plus de filtres** Toutes / En cours / Accomplies. Une seule liste de lignes de 64 px (contre des cartes de 236 × 244) : icône de la récompense, nom, étoiles, objectif sur une ligne (coupé par « … », entier dans la fiche), jauge de progression. Dix quêtes visibles d'un coup, contre huit cartes.
- **Ordre** : quêtes en cours d'abord, puis un séparateur « Accomplies · N » et les accomplies. Une accomplie prend une dalle de pierre chaude à arête dorée (`MenuSkin.AcquiredCard`), son nom en doré pâle et sa médaille à la place de la jauge. Ligne examinée : tranche cyan (en cours) ou dorée (accomplie).
- **Fiche de droite** inchangée : condition, avancée chiffrée, liens vers les récompenses.
- **Outil** : `HubCapture --open-page <Characters|Camp|Quests|Collection>` ouvre une page sans dépendre du focus de l'accueil.
- **Vérifié** : captures en profil neuf avec quatre quêtes accomplies et deux avancées (haut et bas de liste) ; `tools/validate.sh` smoke, `menu_navigation`, `quests` 3/3.

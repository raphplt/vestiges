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

## 6. Lot H3 livré : Collection et Campement lisibles — 11 octobre 2026

- **Collection** : le bouton « Valeurs et règles » est retiré. La règle d'un Écusson ou d'un objet et les chiffres d'une arme s'affichent sous la description ; la fiche défile si elle déborde.
- **Campement** : sous chaque nom, une phrase dit ce que fait l'amélioration (« Toutes tes armes frappent plus fort. », « Chaque coup reçu fait moins mal. », « Écarter une carte pour toute la run. »…), quatorze clés `CAMP_*_HINT` en français et en anglais. À droite de la ligne : rang, prix, et l'effet actuel puis suivant, en cyan. La phrase posée au survol sur le mur du bas, illisible, est retirée avec sa clé `UI_CAMP_NEXT_PRICE` : le prix est déjà sur la ligne. Les rayons défilent si une langue allonge les lignes.
- **Vérifié** : captures du Campement et de la Collection (onglet Écussons) regardées, les neuf lignes de Statistiques tiennent en 1080p ; `tools/validate.sh` smoke, `menu_navigation`, `camp`, `ui_art` 4/4.

## 7. Lot H4 livré : Réminiscences par quêtes, icône de Mémoire vive — 11 octobre 2026

- **Nouveau déblocage** `reminiscence` dans `data/quests/quests.json` (`UnlockKind.Reminiscence`). Une Réminiscence existe dès qu'elle a son image au catalogue de la Collection, même sans effet codé. La règle d'accès reste unique : ce qu'aucune quête ne garde est ouvert d'office. `MetaSaveManager.IsReminiscenceUnlocked` filtre les offres de fragments en run (`PerkSpecializationOffers.IsAvailable`).
- **Réserve de départ, une par famille de la première offre** : Prévoyance (survie), Débordement (combat), Sillage (collecte).
- **Quatre quêtes actives** :

| Quête | Condition | Débloque |
|---|---|---|
| Se relever ★ | 200 éliminations sous la moitié des PV, cumul | Reprise |
| Viser haut ★ | 3 Souverains, cumul | Convergence |
| Contagion ★★ | 1 000 éliminations de créatures ralenties, cumul | Propagation |
| Trois retours ★★ | 3 Résurgences dans la même run | Seconde lecture |

- **Sept quêtes prêtes, « en attente »** (`QuestBook.IsDormant`) : elles ne débloquent que des Réminiscences sans effet codé. Ni affichées au Hub ni suivies en run, elles s'ouvrent seules le jour où l'effet entre dans `PerkSpecializationEffects`. Vider les greniers (120 coffres, cumul) → Délestage ; Les gestes connus (20 micro-événements, cumul) → Habitude ; Coude à coude (1 500 éliminations à moins de 80 px, cumul) → Contrecoup ; Garder la braise (1 000 éliminations en feu, cumul) → Braise ; Le même geste ★★★ (40 éliminations en 2 s) → Mémoire vive ; Au bord de l'oubli (1 000 éliminations en zone effilochée ou effacée, cumul) → Lisière ; Lester ★★★ (600 ralentis tués dans une run) → Poids mort.
- **Collection** : l'onglet Réminiscences dit « 3 / 14 disponibles » en profil neuf ; une Réminiscence verrouillée montre sa quête et le lien « Voir la quête ». Les « À venir » gardent leur mention. Le tri par état est désormais stable dans les trois onglets (`List.Sort` mélangeait l'ordre du catalogue).
- **Bannissement** : avec 3 Réminiscences pour 4 emplacements, l'ancienne garde (« assez de perks pour remplir les emplacements ») refusait tout bannissement. Une réserve déjà plus petite que les emplacements ne garde que la seconde garde : l'offre en cours ne doit pas rester vide. Relevé par `godot-reviewer`.
- **Conséquence à connaître** : en profil neuf, la première offre de Réminiscences montre toujours les trois mêmes cartes, et le 4ᵉ emplacement ne se remplit qu'une fois une quête accomplie. C'est l'effet voulu d'une réserve de 3 (§117).
- **Mémoire vive** (« chaque 10ᵉ impact déclenche deux fois les objets », plan 21 §5) : les deux éclairs sont remplacés par un impact blanc et son écho cyan, la même étoile en plus petit, reliée par deux points de trajectoire (`tools/sprites/perks/plan25.py`). Seules ses deux images changent à l'export ; planche `doc/plans/planches/25-s8-reminiscences.png` régénérée.
- **Vérifié** : build sans avertissement ; capture de la Collection en profil neuf ; `QuestRegression` : 49 quêtes, 11 Réminiscences dont 7 en attente, réserve de départ, Réminiscence inconnue refusée, quêtes en attente jamais accomplies en run ; `tools/validate.sh` smoke, `quests`, `perk_acquisition`, `perk_contracts`, `perk_effects`, `menu_navigation`, `ui_art`, `choice_screen`, `inventory`, `objects`, `saves` verts.
- **Non couvert par un test** : le bannissement avec la seule réserve de départ (`PerkAcquisitionRegression` ouvre toutes les Réminiscences pour éprouver les règles d'offre).

## 8. Lot H5 livré : curseur pixel art — 11 octobre 2026

- **Deux curseurs** dessinés au pixel (`tools/sprites/ui/cursor.py`, `tools/generate_cursors.py`, [planche](planches/37-h5-curseurs.png)) : une flèche de pierre claire à arête gauche dorée et contour sombre, 11 × 18 ; une main qui pointe de l'index, doigts repliés séparés d'un trait d'ombre, manchette dorée, 16 × 18. Points actifs dans `assets/ui/cursor/cursor_manifest.json`.
- **Service** `Infrastructure/MouseCursor`, installé par `GameManager` : la flèche remplace le curseur du système, la main remplace la forme « main » que portent déjà les boutons des menus. Le curseur est agrandi d'un facteur entier selon la hauteur de la fenêtre (×2 en 1080p, ×4 en 2160p) et recalculé au redimensionnement. En run, il est caché tant que le jeu tourne et revient dès qu'un écran met le jeu en pause (montée de niveau, coffre, pause, rencontres, bilan). La souris ne sert pas à viser.
- **Sortie** : `GameExit` rend les curseurs du système avant de quitter, sinon leurs deux textures fuyaient à la fermeture.
- **Vérifié** : journal d'une fenêtre réelle (×4 sur l'écran 4K, puis ×2 à 1341 px de haut), sortie sans fuite ; `tools/validate.sh` smoke, `menu_navigation`, `ui_art`, `choice_screen`, `dev_mode` 5/5. **À juger par Raphaël** : le curseur à l'écran, qu'aucune capture du moteur ne montre.

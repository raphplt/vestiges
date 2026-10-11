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

## 9. Lot R1 livré : le jeu flouté derrière les menus de run — 11 octobre 2026

- **Nouveau shader** `menu_game_veil.gdshader` : il lit l'image du jeu (HUD compris), la floute sur la grille de 480 × 270 du pixel art (treize échantillons sur deux anneaux), la ramène à 55 % de luminosité et 60 % de saturation, assombrit les bords et les ronge par éclats comme la pierre des menus, avec quelques poussières teintées selon l'écran.
- **Écrans concernés** : montée de niveau, coffre, rencontres (Mémorial, Faille, Atelier, échange), pause et journal. Les panneaux de pierre ne changent pas. Le voile de ces écrans devient transparent (`ChoiceStyle.InRunOverlayColor`) et ne sert plus qu'à bloquer les clics. Le fond monte en 0,3 s, du jeu net au jeu flouté.
- **Inchangés** : le bilan (fin de run) garde son voile opaque ; l'écran de chargement garde sa pierre (`PixelBackdrop(…, overGame: false)`) ; les paramètres gardent leur fond de pierre, partagé avec l'accueil.
- **Coût** : le flou ne tourne que pendant un menu, jeu en pause ; aucun coût en combat. Shader ajouté au préchauffage, ainsi que `menu_erasure` et `menu_portrait` (le portrait du bilan manquait au préchauffage depuis la refonte des menus, relevé par l'audit du préchauffage).
- **Curseur** : `GameManager` rend aussi les curseurs à sa sortie, ce qui couvre les fermetures sans `GameExit` (bancs).
- **Vérifié** : captures de la montée de niveau, du coffre et de la pause en vraie run, regardées. Un premier réglage (luminosité 45 %, flou 1,6 case) cachait trop le jeu : ramené à 55 % et 1,1 case. `tools/validate.sh` smoke, `choice_screen`, `menu_navigation`, `ui_art`, `loading` 5/5 ; `tools/test_shader_warmup.sh` valide, sans fuite à la fermeture.

## 10. Lots R2 et R3 livrés : HUD sans encadrés, Écussons au-dessus des armes, objets à gauche — 11 octobre 2026

- **Haut gauche** (`VitalsDisplay`) : la plaque de métal est retirée. Le niveau s'écrit en grand chiffre cyan (22 au lieu de 13) sous la légende « NIV. ». La jauge de PV est fine (7 unités), cerclée d'encre et d'un liseré de métal doré, avec ses quarts gravés et ses PV écrits au-dessus. Chaque texte porte un contour sombre de 4. Couleurs de la jauge, éclat de perte, pulsation à PV bas et bouclier inchangés ; l'overlay des Réminiscences de survie garde son contrat autour de la piste.
- **Haut centre** : le chronomètre est seul, le biome passe dessous.
- **Haut droit** (`HUD.BuildScoreArea`) : plus de plaque. Le score s'écrit en or, taille 22 (au lieu de 18), précédé d'une petite légende « SCORE ». Dessous, une ligne collée au bord droit : crâne et éliminations en taille 14 (au lieu de 10), Essence, puis **Péril**, toujours affiché. Le gain « +120 » apparaît juste à gauche du score.
- **Bas** : les armes descendent juste au-dessus de la barre d'XP. Les quatre **Écussons** passent en cartes de 28 unités (au lieu de 18) au-dessus des armes, avec leur cadre doré, leur niveau et leur fil d'usage. Les **objets du sac** quittent le bas : une colonne au bord gauche, centrée en hauteur, sans cadre, une icône de 24 unités par objet, avec « ×2 » pour les doublons et le compte « 7/10 » en tête. Au-delà de douze objets différents, la colonne se dédouble (`HudInventoryBar`). L'envol d'un objet ramassé vise sa ligne.
- **Vérifié** : captures en vraie run (HUD seul, et inventaire garni : 4 armes, 4 Écussons, 7 objets dont des doublons) regardées. Deux retouches après regard : le gain flottait loin du score, les icônes du sac à 16 unités étaient trop petites. `tools/validate.sh` smoke, `inventory`, `ui_art`, `camp`, `choice_screen`, `objects` 6/6.

## 11. Lot R4 livré : minimap boussole — 11 octobre 2026

- **Radar rond**, 112 unités de diamètre, en trois calques (`MinimapLayer`) :
  - le **brouillard**, un disque au pixel près en deux tons sombres tramés avec un grain fixe ;
  - le **terrain et les repères**, découpés au disque par un petit shader (`minimap_disc.gdshader`) ;
  - l'**anneau de pierre**, au pixel près : filets d'encre, pierre éclairée en haut à gauche, quatre encoches dorées aux points cardinaux et le nord plus large et plus clair.
  
  La carte entière (« show_map ») garde son rectangle, sa légende et son filet doré.
- **Zoom ×1,5** : 17 cellules de côté au lieu de 26, environ deux écrans.
- **Brouillard moins simple** :
  - le rayon de découverte varie d'une cellule à l'autre (8 cellules, jusqu'à 1,6 de moins), donc le front est irrégulier au lieu d'être un disque net ;
  - le bord d'une cellule connue s'effiloche en trame d'un pixel sur deux contre une voisine inconnue, et se referme quand celle-ci est découverte ;
  - le bord du monde reste net.
  
  Le rayon tombe juste en deçà du bord du radar : le front se voit avancer en marchant. Il est sans lien avec la quête « Dessiner la carte », qui compte le brouillard du monde.
- **Coût** : l'anneau et le brouillard sont des textures générées une fois par session et ne se redessinent qu'au passage radar ↔ carte entière ; seul le terrain se repeint, quatre fois par seconde, comme avant. Un premier montage par masque d'enfants (`ClipChildren`) copiait l'écran à chaque image : remplacé par le shader après relecture `godot-reviewer`, qui l'a relevé avec le rayon de découverte invisible dans le radar et le bord du monde tramé.
- **Vérifié** : captures du radar (en marchant, au bord du Néant, poche inconnue tramée) et de la carte entière, regardées ; test `CartographyRegression` adapté (intérieur de cellule intact, bord tramé contre l'inconnu, refermé par la voisine) ; `tools/validate.sh` smoke, `cartography`, `ui_art` 3/3 ; préchauffage des shaders valide.

## 12. Lot R5 livré : chargement fluide — 11 octobre 2026

**Constat mesuré.** Une nouvelle mesure dans `RunObservation` relève la pire image entre le lancement et le début de la run. Mesures avant, même graine, deux passes : 6,8 à 7,2 s de chargement, **pire image de 1 074 à 1 144 ms**. Les décors des biomes entraient d'un bloc (991 ms), les tronçons de décors aussi (904 ms), et le préchauffage dessinait tous ses shaders dans la même image.

**Découpage, sans rien changer à ce qui est posé.**
- **Décors des biomes** (`SpawnEnvironmentPropsAsync`) : reprise de main entre chaque placeur (fermes, carrières, champs, scènes, carrefours). `PropSpawner.SpawnPropsAsync` rend la main dès que sa tranche de 12 ms est épuisée, vérifiée à chaque cellule, puisque le premier chargement d'une texture de décor coûte cher. Les cinq grands décors des champs se posent un par image.
- **Tronçons** (`PropChunks`) : le rangement reste d'un bloc (145 ms) ; l'entrée des tronçons dans l'arbre se fait par tranches de 12 ms (`AttachPending`). Une scène quittée en plein chargement libère les tronçons pas encore entrés. Reprise de main aussi autour de l'index d'occlusion.
- **Préchauffage des shaders** : quatre échantillons par image au lieu de tous dans la même.
- **Fin du chargement** : une reprise de main après les directeurs et les petits lieux, jeu toujours en pause.
- **Code mort retiré** : le chemin synchrone `InitializeWorldSync` (et `ApplyTerrain`, `ApplyRoadOverlay` synchrones) n'avait plus d'appelant.

**Après**, mêmes conditions, deux passes : 6,9 s de chargement (inchangé), **pire image de 250 ms** (4 à 4,5 fois moins). Monde identique : 11 086 décors semés, 21 355 rangés en 468 tronçons, 12 456 décors occultants, avant comme après.

**Reste** : l'image du démarrage de la run (~250 ms : câblage des systèmes et ajouts différés), et une dizaine d'images de 60 à 170 ms (préparation des tuiles, matériau du sol, décors de la ville, rangement des tronçons). Les couper toucherait l'ordre d'initialisation de la run : pas fait sans nouvelle demande.

**Vérifié** : `tools/validate.sh` smoke, `loading`, `movement-integration`, `run_trace`, `small_places`, `dev_mode` 6/6 ; préchauffage des shaders valide ; relecture `godot-reviewer` (sortie de scène, déterminisme, pause pendant les reprises de main) sans bloquant, trois points mineurs corrigés. Mesures prises machine chargée (3 à 6 de charge, une autre session au travail) : seul l'avant/après relatif compte.

## 13. Lot M1 livré : repères au sol en pixel art — 11 octobre 2026

- **Repère des micro-événements** (`RunEventMarker`) : redessiné au pixel du monde, comme les anneaux d'affixe.
  - La Veillée a désormais un fond tramé d'un pixel sur deux, un bord plein, un arc de progression fait des pixels du bord (en 64 pas, dans le sens des aiguilles depuis le haut) et un anneau intérieur qui respire par poses de 0,2 s.
  - Le vestige tombé est un losange au pixel : contour sombre, cœur clair, reflet ; sa traînée et sa lueur au sol sont tramées.
  - Le repère est calé sur un pixel entier et ne se redessine qu'au changement de pose ou de progression ; il ne repeint plus à chaque image.
- **Ellipses cuites** (`PixelEllipseArt`) : bord ou trame d'une ellipse au sol, dans une texture blanche par rayon, gardée pour la session. Une zone de 96 px se dessine en une texture au lieu de milliers de rectangles. Même forme que `PixelGroundRing`, désormais partagée (`PixelGroundRing.Ellipse`).
- **Autres tracés lisses repris** : l'anneau « dash prêt » sous le joueur (ellipse cuite) et les coins du marqueur de cible de la Convergence (traits d'un pixel qui s'écartent par paliers, position calée au pixel).
- **Restent lisses** : les effets de l'Indicible (fosse, éclairs) et les fissures de la Barrière, à reprendre avec le plan 07 si Raphaël les remarque.
- **Vérifié** : captures de la Veillée (zoom sur l'anneau : marches pixel, trame) et du vestige tombé, regardées. L'ellipse blanche saisie à l'impact du vestige est l'éclair existant de `GroundTelegraph`, déjà en pixel art. `tools/validate.sh` smoke, `movement-integration`, `perk_effects`, `small_places` 4/4.

## 14. Lot M2 livré : transitions entre biomes en plaques — 11 octobre 2026

- **Cause des « pixels dans tous les sens »** : près d'une frontière, chaque carré de 2 × 2 pixels tirait au hasard s'il prenait la matière voisine.
- **Premier essai, écarté** : un seuil de bruit cohérent donnait la même décision des deux côtés de la frontière. Les matières s'échangeaient sur la même bande et le bord droit des losanges restait visible, simplement inversé.
- **Retenu** (`ground.gdshader`) :
  - un bruit **déplace la frontière** vers l'un ou l'autre biome, d'une bande au plus, avec un signe fixé par l'ordre des deux biomes, pour que les deux côtés voient la même frontière déplacée ;
  - de grandes plaques (bruit au 1/16) rongées d'un grain plus fin, au pixel double ;
  - la distance à la frontière est sondée sur six anneaux au lieu de trois, depuis un point déformé par un bruit, pour que la lisière ne suive plus les losanges de la grille.
  
  Bande élargie de 34 à 46 px (`data/world/world_gen.json`). S'applique aussi aux matières d'un même biome qui se fondent (boue, eau et herbe du marais).
- **Résultat** : mares bordées de boue aux contours organiques, champs et prairies qui avancent par plaques, plus de pixels isolés ni de losanges francs ([avant/après](planches/37-m2-transitions.png), même graine, même instant).
- **Coût** : seules les cellules proches d'une frontière sondent ; elles font jusqu'à 48 lectures au lieu de 24, plus cinq bruits. La capture en 4K reste au plafond de 144 FPS ; **banc A/B à faire machine calme** (une autre session tournait).
- **Vérifié** : captures avant/après regardées ; `tools/validate.sh` smoke, `erasure_active`, `catalogs` 3/3 ; préchauffage valide.

## 15. Lot M3 livré : bords de routes en perspective iso — 11 octobre 2026

Raphaël a demandé d'enchaîner sans attendre la validation de la planche : la planche sert d'avant/après, à juger en jeu.

- **Cause** : chaque tuile de route ne connaissait que ses quatre voisines et ne dessinait que sa tranche de 16 px. Les bords étaient des traits droits d'un pixel, la bordure nord identique à la sud, les croisements à angle droit.
- **Retenu** (`RoadTileGenerator`, réécrit) :
  - la géométrie de la chaussée est calculée sur le réseau autour de chaque cellule (voisinage 5 × 5 codé en clé), et non plus tuile par tuile. Les angles des croisements sont **abattus en diagonale 2:1**, la pente du sol iso, et débordent sur les tranches voisines, trottoirs compris ;
  - vue du sud-est, comme les immeubles : la bordure au nord d'une rue et celle à l'ouest d'une rue verticale **montrent leur face** (2 px, joints tous les 8 px, pied sombre). Au sud et à l'est, seul le dessus de la bordure dépasse, au-dessus d'un trait sombre ;
  - la ligne droite est cassée par des **tronçons de bordure effondrés** (gravats, sol qui perce), des **morsures** du bord de chaussée que la bordure suit, et de rares nids-de-poule en ellipse 2:1 ;
  - les tuiles sont dédoublonnées par l'empreinte de leurs surfaces : **60 motifs × 4 variantes d'usure** pour 7 966 cellules sur la graine de capture. Les morsures et les tronçons restent hors des pixels que lisent les tuiles voisines, donc les raccords sont exacts.
- **Chargement** : le calcul se fait sur le thread de génération (environ 130 ms, en parallèle du préchauffage des shaders) ; le thread principal ne crée que les textures. Pire image : 256 à 283 ms selon la passe, contre 251 avant, dans le bruit de la machine chargée.
- **Gameplay inchangé** : mêmes cellules de route, même emprise à un pixel près ; les décors ne bougent pas.
- **Résultat** : [avant/après](planches/37-m3-routes.png), même graine.
- **Vérifié** : captures regardées ; `tools/validate.sh` smoke, `loading`, `cartography`, `small_places`, `movement` 5/5 ; relecture `godot-reviewer` (points mineurs corrigés : type `RoadTiles` dans son fichier, verrou sur les caches, cellules hors grille ignorées, références libérées).

## 16. Lot M4, première vague livrée : Rôdeur, Charognard, Présage, Tisseuse — 11 octobre 2026

- **Constat** sur le sol réel (ville, forêt, marais), à côté du Traqueur :
  - les trois créatures du pilote du 24 septembre se fondaient dans les sols : bruns sur terre et pavés, silhouettes molles ;
  - le Rôdeur de profil était un bâton penché : ses bras suivaient la pente du dos ;
  - le Présage était une gélule lilas ;
  - la Tisseuse venait encore de l'ancien générateur en voxels, quatre directions, d'un autre style.
- **Retenu** (pipeline commun, `tools/sprites/creatures/`) :
  - **Rôdeur** : chair cendrée et membres noircis, tête noircie où trois yeux verts se détachent, bras plus longs qui pendent à la verticale devant le corps voûté, griffes d'os et poing de pierre agrandis ;
  - **Charognard** : fourrure rouille, échine d'os à nu, pattes sombres d'insecte, corps un peu plus grand ;
  - **Présage** : voile évasé en cloche, capuche pointue penchée en avant qui montre son vide et sa colonne d'yeux, franges plus longues, couronne de pierres claires écartée ;
  - **Tisseuse** : nouveau modèle (`tisseuse.py`), petit corps sombre fendu d'un cœur vert qui pulse, huit pattes pâles inégales aux genoux relevés, filière. Huit directions, cadre 48 × 48, `sprite_feet_offset` 16. Elle se cabre à l'attaque et se replie à la mort. `scripts/generate_tisseuse.py` est retiré.
- **Gameplay inchangé** : mêmes cadres et mêmes pieds pour les trois premiers ; la Tisseuse est plus petite à l'écran que l'ancienne (environ 30 px de large contre 40), son emprise de jeu ne change pas.
- **Résultat** : [avant/après](planches/37-m4-ennemis.png), aperçus sur trois sols et capture en jeu.
- **Vérifié** : aperçus regardés sur trois sols, capture du bestiaire en jeu ; `tools/validate.sh` smoke, `enemy_abilities`, `catalogs`, `ui_art` 4/4 ; régénération identique octet pour octet.
- **Suite** : Raphaël valide la direction et demande les huit autres, « qualité max » (§17).

## 17. Lot M4, seconde vague livrée : les huit autres créatures — 11 octobre 2026

Raphaël : « ouais. essaie de revoir les autres ennemis qui ont besoin d'un upgrade visuel. fait la qualité max. »

- **Constat**, même aperçu sur trois sols :
  - le Hurleur, petit tube vert-de-gris, disparaissait dans l'herbe ;
  - le Rampant se lisait comme une bûche brune ;
  - la Sentinelle était une tige grêle ;
  - le Tréant, une souche coiffée d'une boule verte ;
  - la Brute, un ours violet ;
  - le Cracheur, l'Ombre et le Rampant d'ombre restaient lisibles mais plats.
- **Retenu** :
  - **Hurleur** : chair livide veinée de lie-de-vin, un tiers plus grand, trois plis, lambeaux pendus au bord de la bouche verte, trois yeux. La cible prioritaire se voit de loin ;
  - **Rampant** : larve blême entre des plaques d'ardoise plus étroites que les anneaux, mandibules d'os plus grandes, un peu plus grand ;
  - **Sentinelle** : réverbère lisible. Socle de béton clair, fût de fonte rouillée envahi de bourrelets de chair, chapeau de lanterne en fonte sous lequel pend le bulbe de chair. Paupières entrouvertes au repos, les yeux se devinent. Plus haute qu'un humain ;
  - **Tréant** : bois mort grisé fendu de noir, bouche creuse sous les deux nœuds verts, couronne de feuilles rousses en touffes avec de la mousse et des branches nues, tronc plus haut, bras plus longs et plus clairs ;
  - **Brute** : chair à vif couturée au lieu du violet uniforme, dalle de béton pâle, cinq fers à béton rouillés plus épais, portière rouge, pied de chaise clair ;
  - **Cracheur** : lambeaux d'une chemise délavée (il a été quelqu'un), poche de bile plus grosse avec un reflet humide, bras qui pendent devant le corps penché, tête et yeux plus grands ;
  - **Ombre** : reflets irisés violet et cyan qui la détachent des sols sombres, yeux plus hauts sur leurs tiges, flaque un peu plus grande ;
  - **Rampant d'ombre** : masque agrandi, traînées rouges plus vives (trois), membres un ton au-dessus du corps.
- **Gameplay inchangé** : mêmes cadres, mêmes pivots, mêmes `sprite_feet_offset` ; seules les images changent.
- **Hors champ** : l'Indicible et la Barrière, refaits le 10 octobre (plan 07 B3–B5) sur planches validées, ne sont pas touchés.
- **Résultat** : [avant/après](planches/37-m4-ennemis-2.png), aperçus et les douze créatures en jeu.
- **Vérifié** : aperçus regardés sur trois sols, planches de contrôle des quatre animations en huit directions, deux captures du bestiaire en jeu ; `tools/validate.sh` smoke, `enemy_abilities`, `catalogs`, `ui_art`.

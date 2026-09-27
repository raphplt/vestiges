# Plan 04 — Interfaces lisibles et vraie préparation d'exploration

Statut : **clarté, sobriété et Collection validées ; maquettes proposées** · Priorité : P1 · Dépendances : cadrage 08, catégories 05/06.
Références : V2 §15–18 ; Bible §9 ; [dossier](README.md).

## 1. Résultat attendu

Le joueur comprend où commencer, qui il incarne, ce qu'il peut débloquer, ce qu'il gagne et pourquoi il meurt. Le texte est net et confortable. **Less is more : portraits, icônes, espaces et états clairs ; une phrase utile plutôt que des paragraphes sur chaque carte.** Le Hub donne envie de choisir un personnage et une intention de run.

## 2. Phase 0 — Faits et composants réutilisables

- [HubScreen.cs](../../scripts/UI/HubScreen.cs), `CreateCharacterCard(HFlowContainer parent, CharacterData character)` et sélection vers 779–899 : cartes textuelles, statistiques/passif/arme/condition déjà présents, sans aperçu animé ; interactions souris explicites.
- Marges fixes et boutons sans focus limitent l'adaptation et la navigation. À observer à plusieurs résolutions avant refonte.
- [HUD.cs](../../scripts/UI/HUD.cs) : base logique 960×540, scaling de racine, `SetHudScale(float)`, score et barre XP déjà existants.
- Le projet utilise 1920×1080 et `canvas_items` ; la charte décrit 480×270. C'est un arbitrage à traiter en 08.
- PixelOperator est présent dans les assets et le générateur du logo, mais aucune affectation globale de police au HUD n'a été identifiée : diagnostiquer police effective, bitmap du logo et scaling séparément.
- [UITheme.cs](../../scripts/UI/UITheme.cs) fournit `ApplyButtonStyle`, `ApplyAnimatedButtonStyle`, `ApplyTabStyle` et `WireButtonAudio`. Reprendre ces points de mutualisation.
- `CharacterSpriteLoader.LoadOrGet(string charId, string folder)` peut fournir les animations de prévisualisation ; `QuestManager.GetProgressionSnapshots` fournit la progression des objectifs.

Les esquisses ci-dessous sont des propositions de contenu et de hiérarchie ; les maquettes visuelles seront produites dans le premier lot après validation.

## 3. Architecture d'information proposée

| Écran | Question à laquelle il répond | Contenu prioritaire |
|---|---|---|
| Accueil | Où commencer ? | Explorer, **Collection**, quêtes/défis, paramètres, quitter |
| Exploration / Miroirs | Avec qui et pour quoi partir ? | Grille des personnages, aperçu, style, arme/passif, objectif, départ |
| Collection | Que puis-je trouver et essayer ? | Accès direct du menu principal ; onglets Armes/Objets, grille d’icônes, raretés, états ; détails au focus |
| Quêtes / Journal | Qu’est-ce que je progresse ou découvre ? | Quêtes/défis et récompenses d’un côté ; lore dans un espace distinct |
| Écho | Quels sont mes résultats ? | Historique et records locaux ; classement distant plus tard |
| Pause | Quel est mon build ? | Armes, objets, stats, synergies, quêtes actives, paramètres |
| Mort | Qu'ai-je accompli ? | Score, cause, build, progression, déblocages, prochaine tentative |

Les noms Miroirs/Chroniques/Écho viennent de V2 §18 ; conserver un sous-titre fonctionnel pour leur compréhension. L’accès direct à la **Collection** est demandé par Raphaël. Les libellés fonctionnels dominent ; les noms poétiques peuvent être secondaires. La Collection affiche armes et objets débloqués dès l’accueil, sans passage obligé par Exploration ni une run.

### Contenu du panneau personnage

Grille : portrait/animation, nom, état. Panneau sélectionné : promesse en une phrase, arme et mobilité sous forme d’icônes, passif résumé. Statistiques, formule et lore uniquement à la demande. Si verrouillé, une condition et sa progression. Aucun paragraphe répété sur toutes les cartes.

Ne pas supposer que le joueur peut choisir librement son arme initiale : cette possibilité exige une décision de 05/06. Le premier lot montre l'arme liée au personnage.

### Hiérarchie du HUD

Santé/danger immédiat ; score vivant ; XP/niveau ; Essence ; armes/objets ; Effacement et Résurgence ; suivi compact des quêtes de run. Les détails chiffrés restent dans la pause ou les choix. Le score reste visible pendant le jeu et ne dispute pas la priorité aux alertes de survie. Aucun record ou objectif de dépassement dans le HUD ni dans la pause. Les objets sont regroupés en piles ; la liste exhaustive est consultable en pause, pas étalée sur le combat.

## 4. Décisions à valider

- Navigation proposée et place d'Exploration comme écran central.
- Corps de texte net, typographie utilitaire pour HUD/titres et ton distinct pour lore, suivant Bible §9.1.
- Redimensionnement par disposition et échelle UI dédiée, après décision de résolution en 08.
- Support du parcours complet au clavier et à la manette.
- Comparaison explicite au loot : gain/perte, comportement, rareté, remplacement.
- Limiter le suivi in-run aux objectifs utiles ; catalogue détaillé au Hub.

## 5. Lots d'action

### Lot A — Inventaire et maquettes

1. Capturer accueil, Exploration, cartes verrouillées, Chroniques, HUD, pause, loot, niveau, mort, paramètres.
2. Identifier pour chaque écran l'action principale et les trois informations essentielles.
3. Produire les maquettes accueil/Exploration/HUD/choix de loot/mort à 720p et 1080p.
4. Établir tokens de texte, espacements, panneaux, focus, icônes, états verrouillé/sélectionné.
5. Montrer un personnage verrouillé, un score long et une description longue dans les maquettes.

**Vérification :** Raphaël valide le parcours et la hiérarchie ; aucun écran n'exige de lire toute une colonne avant l'action principale.
**Garde-fou :** pas de décoration détaillée avant validation de lisibilité.

### Lot B — Typographie et mise en page communes

1. Reprendre `UITheme` pour définir le système partagé plutôt que dupliquer couleurs/tailles.
2. Identifier les fontes réellement utilisées ; comparer deux variantes typographiques sur les maquettes.
3. Définir rendu du texte, tailles minimales et agrandissement sans forcer le filtrage des sprites.
4. Remplacer les marges rigides problématiques par conteneurs, retours à la ligne et défilement.
5. Vérifier accents français, chiffres, noms longs et variation de langue.

**Vérification :** 1280×720, 1920×1080, 2560×1440 et écran large ; aucun texte tronqué critique.
**Garde-fou :** la police du logo n'impose pas celle des descriptions ; ne pas changer globalement le filtrage pixel art pour lisser du texte.

**Lot B, étape 5 (accents) — 27 septembre 2026 :** une quarantaine de textes français sans accents corrigés (la police Saira les gère) : traduction (`PARAMÈTRES`, Contrôles, Plein écran, Réduites, Désactivé, Réinitialiser, pause, accueil, Journal, chargement, « [Échap] »), écran des paramètres, comparaison d'arme au sol (« Déjà équipée », « Portée », « [E] Échanger »), Chroniques. Recherche outillée : mots des textes du code et de la traduction comparés aux formes accentuées de la doc. Quatre clés V1 inutilisées retirées de la traduction (réparer, récolter, métal, « mort avant la première crise »). Les étapes 1 à 4 restent à faire.

**Lot B, étapes 1 à 4 : découpage — 27 septembre 2026 (session locale, sans Raphaël) :**

*Constat de départ :*
- 89 tailles de police écrites en dur, 18 valeurs différentes (de 7 à 120) ;
- sept fonctions `MakeLabel` quasi identiques (pause, level-up, écrans de choix, accueil, bilan, HUD, événements) ;
- les polices Saira chargées fichier par fichier dans six écrans ;
- les couleurs de texte dupliquées entre `UITheme` et `ChoiceStyle`.

L'étirement `canvas_items` en mode `keep` garde la même mise en page à toutes les résolutions 16:9 (bandes noires ailleurs). Le vrai risque de 720p n'est donc pas la mise en page mais la **taille physique** : un texte de 12 px en base 1080p fait 8 px à l'écran. Les lignes de lore de la pause, les détails des quêtes de run et les étiquettes du level-up sont illisibles en 720p (captures regardées, réduites en 1280×720).

| Étape | Contenu |
|---|---|
| B1 Système commun | `UITheme` porte les polices (chargées une fois), une échelle typographique par rôle (légende, petit, corps, accent, titre de section, titre, affichage), les couleurs de texte, et un seul `MakeLabel`. `ChoiceStyle` et les écrans s'y rattachent ; plus aucune taille en dur dans les écrans en base 1080p. Le HUD (base 960×540) et les textes posés dans le monde gardent leurs tailles, mais passent par le même calcul. |
| B2 Polices et variantes | Inventaire des polices réellement utilisées. Comparaison de deux échelles sur la pause et le level-up : l'échelle actuelle et une échelle « confort » où rien ne descend sous 14 px en base 1080p. |
| B3 Rendu et agrandissement | Taille minimale par rôle ; réglage « Taille du texte » (100, 115, 130 %) dans les paramètres, appliqué par l'échelle commune, sans toucher au filtrage des sprites. |
| B4 Mises en page souples | Là où le texte agrandi déborde : défilement (colonnes de la pause, liste des quêtes), retours à la ligne (descriptions), largeurs minimales plutôt que fixes. Vérification en « Taille du texte » 130 % avec noms longs. |

### Lot C — Exploration et sélection

1. Reprendre les données de cartes existantes et le cache d'animations.
2. Ajouter grille navigable, focus visible, panneau détaillé, verrouillage expliqué et lancement.
3. Relier la condition à la progression réelle de 06 ; permettre d'aller à la quête correspondante.
4. Conserver sélection au retour, gérer personnage indisponible et sauvegarde ancienne.
5. Ajouter transitions brèves et audio de navigation via `UITheme`.

**Vérification :** depuis une sauvegarde vierge, choisir et lancer sans aide ; parcourir sans souris ; aperçu conforme au personnage réellement chargé.
**Garde-fou :** pas de déblocage local simulé par l'interface ; aucune dépendance à un futur service distant.

### Lot C2 — Collection directement accessible et texte à la demande

1. Ajouter « Collection » comme entrée visible du menu principal, accessible souris/clavier/manette. Deux onglets explicites Armes/Objets ; conserver le choix de filtre au retour.
2. Montrer une grille visuelle avec rareté et statut disponible/verrouillé ; distinguer découverte en run et déblocage sans multiplier les badges illisibles.
3. Dans le seul panneau sélectionné : nom, phrase d’effet, règle de cumul d’objet, condition directe de quête avec lien. Rareté fixe par objet au premier lot de 05 ; compteur possédé seulement dans les écrans de run/bilan.
4. Employer la même politique de disponibilité que le loot et les quêtes, pour éviter les contradictions d’affichage. Filtrer débloqués/non débloqués ; pagination ou liste virtualisée si nécessaire.
5. Vérifier l’entrée depuis la fin de run et le retour à l’accueil sans perdre le focus. Pas de bouton d’achat ajouté sans décision spécifique.

**Vérification :** le joueur trouve ses armes/objets disponibles en une action depuis l’accueil, explique un déblocage sans lire un mur de texte et parcourt une grande collection à la manette.
**Garde-fou :** un résumé court ne doit pas masquer la contrepartie d’une malédiction ou l’effet réel d’une pile.

### Lot D — HUD, choix, pause et bilan

1. Intégrer le compteur de 02 et réserver sa place avec chiffres longs.
2. Reprendre la barre XP et afficher les catégories de build approuvées en 05.
3. Unifier descriptions d'effets, comparaison d'armes et états d'inventaire plein.
4. Afficher clairement les choix de niveau, récompenses et boutons de fermeture/reprise.
5. Réaliser la refonte majeure du bilan définie en 02 : zones visuelles, build complet/piles, récompenses, record uniquement après la mort, animation accélérable et relance claire.

**Vérification :** le joueur retrouve son build, comprend un remplacement et revient au combat ; overlays simultanés ordonnés.
**Garde-fou :** la UI ne calcule ni score, ni coût réel, ni récompense ; elle présente l'état autoritaire.

## 6. Recette finale

Parcours : nouveau profil → Exploration → run → loot/niveau → pause → mort → déblocage → nouveau départ. Répéter sur profil avancé avec nombreux contenus.

Tester focus clavier/manette, souris, paramètres persistants, texte agrandi, filtres daltoniens et effets réduits. Captures avant/après, build et smoke si applicable.

Roadmap D/E/F/G. La création d'un écran agréable ne suffit pas à cocher onboarding avant un essai sans explication.

## Retour de Raphaël et HUD de run — 24 septembre 2026

**Retour :**
- La flèche qui pointe vers le centre de la carte n'a plus d'intérêt.
- Les PV et les autres informations en haut à gauche sont très peu visibles.
- La police est trop pixelisée et dure à lire. Il semble y avoir plusieurs polices, aucune ne convient. Il en faut une qui soit dans la DA et vraiment lisible.

**Diagnostic de la police :** une seule fonte était en place, PixelOperator (grille de 16 px), affectée par le thème global. Le HUD l'affichait à 7–12 px, sur une racine mise à l'échelle ×2 : les glyphes, ré-échantillonnés hors de leur grille, changeaient d'aspect selon la taille. D'où l'impression de plusieurs polices.

**Police retenue : Saira Semi Condensed** (OFL, `assets/fonts/saira/`), en Medium pour le thème et SemiBold pour les plaques d'élites. Critères :
- Bible §9.1 : sans-serif condensée, industrielle et utilitaire (lettrage de caisses, pochoir), ni serif fantaisie ni gothique.
- Lisibilité : chasse ouverte, 1/I/l et 0/O distincts.
- Accents français complets.

Comparées sur les mêmes textes : Barlow Semi Condensed, Chakra Petch, Big Shoulders et Pixelify Sans. Barlow est le second choix ; Big Shoulders ne tient pas aux petites tailles ; Pixelify reste un pixel font. Godot 4.7 rastérise le texte à la taille finale malgré l'échelle ×2 du HUD : le rendu est net en 1080p comme en 4K (capture 3840×2160 vérifiée). La police secondaire « livresque » du lore (Bible §9.1) reste à choisir.

**HUD refait (`scripts/UI/HUD.cs`) :**
- Trois plaques sombres à 82 % d'opacité, liseré or : lisibles sur tous les sols.
- Haut-gauche : pastille de niveau, barre de PV de 156×17 avec valeur, trace claire des PV perdus, bord qui bat sous 30 % de vie, XP juste dessous.
- Haut-centre : phase, biome, temps de run, Effacement en barre et en %, annonce de Résurgence avec décompte.
- Haut-droite : score défilant et Essence.
- **Jauge de PV sous le héros** (`PlayerHealthGauge`) : discrète à pleine vie, opaque après un coup, battante quand la vie est basse. La recharge du dash passe juste dessous.
- Boussole vers le centre supprimée, avec le code mort hérité de la V1 (inventaire, résumé d'aube, noms de ressources). La barre d'XP pleine largeur du bas rejoint la plaque de vie, à côté du niveau.
- Libellés en clés de traduction `UI_HUD_*`. Accents rétablis dans les quêtes de run.

**À valider en jeu :** taille des plaques en 720p, présence de la jauge sous le héros en combat dense, et intérêt de garder ou non une barre d'XP pleine largeur.

## Accueil refait — 26 septembre 2026

**Demande de Raphaël :** l'accueil est « trop classique ». Ne plus afficher les statistiques du personnage choisi (PV, ATK, VIT) mais montrer son sprite. Rendre l'ensemble « jeu fini » et fidèle à la DA. « Surprends-moi. »

**Parti pris : le Hub est le Foyer (V2 §18), un camp vivant autour du feu.** Plus de panneau central ni de cadres de boutons. La peinture du menu (480×270 natifs affichés ×4) devient la scène, et tout ce qui s'y ajoute respecte ce grain de 4 px.
- **Camp (`HubCamp`)** : les personnages veillent autour du feu, à l'échelle ×4 de la peinture, avec leur idle animé. Le personnage choisi se tourne vers le joueur (face S), avec un liseré doré (`outline.gdshader`) et un halo au sol. Les autres regardent le feu, assombris. Les personnages verrouillés sont des silhouettes blanchies dont les pixels disparaissent et reviennent (`hub_forgotten.gdshader`) : ils « reviennent » au camp quand on les débloque, première forme du Hub qui se remplit (V2 §18). Changement : ◀ ▶ (clavier, manette), clic sur un personnage ou flèches de la plaque.
- **Plaque** : nom et phrase de description, rien d'autre. Personnage verrouillé : « ??? » et sa condition, et « Partir » est désactivé.
- **Décor vivant (`HubBackdrop`)** : lumière du feu qui vacille, en anneaux francs et fondu additif ; braises qui montent ; Effacement qui arrache des pixels au bord droit ; paires d'yeux de créatures qui s'ouvrent, clignent et se referment dans les recoins sombres ; dégradés d'ombre à gauche et en bas pour la lecture.
- **Titre** : nouveau « VESTIGES » en glyphes dessinés (`tools/generate_hub_title.py`, 115×30 natifs, ×4), biseau doré. Les trois dernières lettres s'effacent et continuent de s'effriter en jeu (particules).
- **Menu (`HubMenuButton`)** : Partir, Chroniques, Paramètres, Quitter, en texte seul. Au focus, une braise en losange s'allume et décale le texte ; survol et focus ne font qu'un, et le menu se joue entièrement au clavier et à la manette. La graine et la bascule dev sont discrètes en bas à gauche, les Vestiges en haut à droite.
- **Départ** : le personnage quitte le feu en marchant vers le nord-est, le menu s'éteint, puis `VoidTransition` prend le relais.
- **Chroniques** : extraites dans `HubChroniquesPanel`, affichées sur un voile sombre au-dessus du camp, avec des onglets textuels soulignés. L'écran intermédiaire « Exploration/Miroirs » disparaît : le choix du personnage se fait sur l'accueil.
- **Texte** : Saira Semi Condensed (retour du 24 septembre sur la police pixel) ; seul le titre est en pixels.

**Vérifications :** `dotnet build` sans warning ; `tools/smoke_test.sh` vert ; `tools/test_dev_mode.sh` et `tools/test_dev_release.sh` verts. Captures 1080p avec le nouvel outil `tools/capture_hub.sh <dossier> [actions]`, qui rejoue des actions d'input et capture après chacune : accueil en dev et en profil neuf (personnages verrouillés), changement de personnage, aller-retour dans les Chroniques et navigation des onglets à la manette, départ vers la run.

**Limites et suites :**
- Le Traqueur, capuche baissée, se lit presque de dos même de face (sprite, plan 08).
- La Collection demandée (lot C2) n'existe pas encore. Elle prendra une entrée du menu.
- Le Hub qui évolue avec les Souvenirs se limite pour l'instant au retour des personnages débloqués autour du feu.
- Six places sont prévues autour du feu ; au-delà, il faudra un second cercle.

## Collection, première passe — 27 septembre 2026 (session cloud)

Lot C2, décision de Raphaël : Collection directement accessible depuis le menu principal.
- **Entrée « Collection »** dans le menu de l'accueil, juste après « Partir », accessible à la souris, au clavier et à la manette. Même voile, même en-tête et même « Retour » que les Chroniques ; `ui_cancel` ramène au menu, et le focus revient sur « Collection ».
- **Deux onglets** : « Armes » (24) et « Souvenirs de run » (13, les passifs). Les objets du plan 05 n'existent pas encore ; ils prendront un troisième onglet. L'onglet choisi est conservé d'une ouverture à l'autre.
- **Grille d'icônes**, disponibles d'abord. Une arme verrouillée n'est qu'une silhouette sombre, sans badge. Le panneau de droite, seul à porter du texte, affiche pour la case survolée ou sélectionnée son nom, « Disponible en run » ou « Pas encore disponible », son effet, ses valeurs (mêlée ou distance, forme, dégâts, cadence, portée) et, si elle est verrouillée, sa condition directe (« Se débloque en retrouvant le Souvenir « Flamme de mémoire » »). Un compteur indique « 20 / 24 disponibles ».
- **Même règle que le loot** : `MetaSaveManager.IsWeaponUnlocked` est désormais la seule règle de disponibilité d'une arme, partagée par le tirage d'armes du joueur, les fragments de niveau et la Collection.
- **Manette** : du premier rang, « haut » remonte aux onglets et « bas » redescend ; les deux onglets sont liés explicitement, car la recherche géométrique de Godot plongeait dans la grille.
- **Vérifié** : `tools/capture_hub.sh` en profil dev (24/24) et en profil neuf (20/24, quatre silhouettes, condition affichée), avec navigation à la manette dans la grille, vers les onglets et retour au menu. Images regardées.
- **Non fait** : lien vers la quête qui débloque (les armes se débloquent encore par Souvenir, en attente du plan 05 lot A et du plan 06), entrée depuis le bilan, filtre disponibles et non disponibles (inutile à 24 armes).


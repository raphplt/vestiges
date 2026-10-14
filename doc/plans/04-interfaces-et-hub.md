# Plan 04 — Interfaces lisibles et vraie préparation d'exploration

Statut : **refonte P1–P7 livrée et vérifiée sur `codex/refonte-menus` (§8–9), recette artistique en jeu ouverte** · Priorité : P1 · Dépendances : cadrage 08, catégories 05/06.
Références : V2 §15–18 ; Bible §9 ; [dossier](README.md).

**Menus du Hub, 10 octobre 2026 :** la page Chroniques et son entrée sont supprimées à la demande de Raphaël. Menu à cette étape : Partir, Personnages, Quêtes, Collection, Paramètres, Quitter. Le parcours de production du §8 le remplace ensuite. Les quêtes accomplies utilisent une médaille pixel art originale ; le premier sceau est remplacé après le retour de Raphaël sur sa lisibilité, au [plan 06 Q3d](06-personnages-quetes-defis.md#q3d--lisibilité-de-licône-de-complétion--10-octobre-2026), décision §100.

**Nouvelle demande du 10 octobre, précisée après les esquisses :** reprendre l'intérieur de Personnages, Quêtes, Collection et Paramètres, ainsi que la pause ; garder l'accueil actuel, apprécié, avec Paramètres et Quitter regroupés en bas. Les deux premières esquisses sont rejetées pour leur rendu trop web. Pierre et Effacement est une préférence de recherche, pas un visuel approuvé ([§7](#7-refonte-des-menus--étude-du-10-octobre-2026), [DECISIONS §106](DECISIONS.md)). Ce cadrage remplace les anciennes propositions de parcours du §3 pour le prochain chantier. Les comptes rendus B/C/R1–R8 restent l'historique de ce qui a été livré.

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

**Lot B, étapes 1 à 4 livrées — 27 septembre 2026 (session locale) :**
- **B1, système commun.** `UITheme` porte désormais :
  - les trois graisses de Saira, chargées une fois ;
  - neuf rôles de texte (`TextRole`) : légende 14, petit 15, corps 16, accent 18, sous-titre 20, titre de section 24, titre 30, bannière 36, affichage 46, en base 1080p ;
  - un `MakeLabel` commun, et `TextLight` avec les autres couleurs.
  
  Seize écrans et panneaux en base 1080p n'ont plus aucune taille écrite en dur (pause, level-up, écrans de choix, coffres, paramètres, journal, accueil, Collection, Chroniques, bilan, chargement, quêtes de run…). Les sept `MakeLabel` passent par le rôle ; `ChoiceStyle` reprend les couleurs de `UITheme`. Correspondance : 13 px et moins → légende, 14 → petit, 15-16 → corps, 17-18 → accent, 20 → sous-titre, 22-24 → titre de section, 26-30 → titre, 34-36 → bannière, 44-48 → affichage.
- **Hors du système, volontairement :**
  - le HUD (base 960×540, recetté « bien mieux »), la rafale et le bandeau d'événement qui vivent dans sa racine ;
  - les textes posés dans le monde (invite « Ouvrir », état des Mémoriaux, arme au sol, murmure des échos) ;
  - le score géant du bilan (120 px, placé au pixel près) ;
  - l'overlay de debug.
- **B2, polices.** Saira Semi Condensed (Medium, SemiBold, Bold) partout dans l'interface ; PixelOperator ne reste que sur deux textes posés dans le monde (invite d'interaction, état d'un Mémorial), en cohérence avec les sprites. Comparaison de deux échelles sur la pause et le level-up, en 1280×720 : l'échelle d'avant (lore de la pause à 12 px, soit 8 px à l'écran) et l'échelle « confort » retenue, où rien ne descend sous 14 px. Captures regardées : les lignes de lore, les « dégâts infligés » et les étiquettes de rareté deviennent lisibles en 720p.
- **B3, taille du texte.** Réglage Paramètres › Graphismes › « Taille du texte » : 100, 115 ou 130 %, enregistré dans `display_settings.cfg`.
  - Il s'applique sur-le-champ : chaque texte retient sa taille de base, et `UITheme.RefreshTextScale` la réapplique. Ce parcours ne descend pas dans le monde : sous un `Node2D`, seuls les `CanvasLayer` sont visités, pas les milliers de décors (relevé par `godot-reviewer`).
  - Rendu inchangé : les polices sont rastérisées à leur taille finale (suréchantillonnage de Godot), le filtrage « nearest » des sprites n'est pas touché.
- **B4, mises en page souples.** Captures à 130 % (`--text-step 2`, nouvelle option des scènes de capture) :
  - les colonnes Équipement et Fiche de la pause défilent, avec une gouttière pour la barre ;
  - les onglets des paramètres défilent ;
  - le score du bilan chevauchait « Nouveau record » à 130 % : il reste à sa taille ;
  - level-up, quêtes de run, Chroniques et Collection tiennent sans retouche.
- **B3 retiré (4 octobre, [DECISIONS §65](DECISIONS.md)).** Le réglage « Taille du texte », `TextSettings`, `UITheme.RefreshTextScale` et l'option `--text-step` des captures sont retirés (plan 26 N1). L'échelle par rôle et les mises en page souples de B4 restent.
- **Vérifié :**
  - build sans avertissement, smoke test ;
  - captures de l'accueil, de la Collection, des Chroniques, des paramètres, de la pause, du level-up, des coffres et du bilan, à 100 et 130 %, réduites en 1280×720 et regardées.
  
  Relecture `godot-reviewer` intégrée : parcours limité à l'interface, palier « bannière » ajouté pour ne pas rapetisser les titres du bilan et de la pause (34-36 px), hauteur des boutons de l'accueil rendue.
- **Limites :**
  - les onglets des paramètres ne prenaient pas le focus clavier : corrigé au lot C, plus bas ;
  - le défilement de la pause se fait à la molette seulement (stick droit et Page haut/bas ajoutés au lot C) ;
  - les tailles au-dessus de 14 px ont bougé d'un ou deux pixels (15 → 16, 17 → 18, 22 → 24) : à juger en recette.

### Lot C — Exploration et sélection

1. Reprendre les données de cartes existantes et le cache d'animations.
2. Ajouter grille navigable, focus visible, panneau détaillé, verrouillage expliqué et lancement.
3. Relier la condition à la progression réelle de 06 ; permettre d'aller à la quête correspondante.
4. Conserver sélection au retour, gérer personnage indisponible et sauvegarde ancienne.
5. Ajouter transitions brèves et audio de navigation via `UITheme`.

**Vérification :** depuis une sauvegarde vierge, choisir et lancer sans aide ; parcourir sans souris ; aperçu conforme au personnage réellement chargé.
**Garde-fou :** pas de déblocage local simulé par l'interface ; aucune dépendance à un futur service distant.

**Lot C, navigation clavier et manette — 27 septembre 2026 (session locale) :** l'étape 2 (« parcourir sans souris »), hors écran de sélection, déjà navigable depuis l'accueil refait.
- **Paramètres**, jusqu'ici entièrement à la souris :
  - aucun contrôle ne prenait le focus (`FocusMode.None` partout) ; onglets, boutons, curseurs, remappage et bouton de fermeture le prennent désormais ;
  - l'écran s'ouvre sur l'onglet actif. Gauche et droite parcourent les onglets et les ouvrent ; bas mène au premier réglage ; Échap ou B ferme et rend le focus au bouton qui l'avait ouvert (pause ou accueil) ;
  - les curseurs avancent par pas de 5 % au lieu de 1 %, sinon il fallait cent appuis.
- **Pause** : elle s'ouvre sur « Reprendre », avec le focus.
- **Repère commun** : `UITheme.ApplyFocusStyle`, un cadre doré sur le contrôle qui a le focus, appliqué aux boutons, onglets et curseurs stylés par `UITheme`.
- **Vérifié :** capture de l'accueil rejouant bas, bas, bas, entrée, puis droite et bas dans les paramètres (`tools/capture_hub.sh`) ; images regardées : onglet Graphismes ouvert au clavier, focus visible de réglage en réglage. Smoke test vert.
- **Non fait :**
  - lien entre la condition d'un personnage verrouillé et sa quête (étape 3, dépend du plan 06) ;
  - le remappage d'une touche commence à l'appui sur Entrée ou A : à essayer au clavier réel.
- **Défilement de la pause sans souris, 27 septembre au soir** : les colonnes Équipement et Fiche n'ont aucun contrôle focalisable. Elles défilent donc au stick droit ou à Page haut / Page bas (actions `scroll_up` et `scroll_down`), les deux ensemble. Vérifié par `--capture-pause --text-step 2` : à 130 %, la colonne descend jusqu'au dernier passif. À 100 % en 1080p, tout tient sans défiler.

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
- **Non fait** : lien vers la quête qui débloque (les armes se débloquent encore par Souvenir, en attente du plan 05 lot A et du plan 06), filtre disponibles et non disponibles (inutile à 24 armes).
- **Entrée depuis le bilan, 27 septembre au soir** : voir le [plan 02, lot D](02-juiciness-score.md).

## Reprise de la pause, des boutons et de la carte — 1er octobre 2026

Plan rouvert après recette ([DECISIONS §44](DECISIONS.md)). Lots coordonnés au [plan 24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026), **R1/R2 livrés ci-dessous, R6 à entreprendre** :

- **R1 :** retirer les descriptions des armes dans l'équipement, « HALTE DANS LE VIDE », le sous-titre « Le monde se fige, mais ta mémoire reste éveillée. » et l'explication sous Chance. Conserver les valeurs utiles au build. Reprendre la composition visuelle de la pause, pas seulement ses textes.
- **R1 :** simplifier les contours superposés au survol des boutons ; vérifier aussi le focus clavier/manette et les écrans partageant ces styles.
- **R2 :** montrer la nature de chaque récompense de coffre (Essence, PV, dégâts critiques, etc.) par une icône appropriée ; la rareté reste une information séparée. Produire les images manquantes avec le plan 25.
- **R6 :** revoir le rendu gris et trop grossier de la carte. Examiner radar et carte agrandie avant de fixer le périmètre ; ne pas attribuer d'office le retour à tout le terrain du monde.

Points d'entrée : `PauseMenu`, `PlayerSheet`, `UITheme`, `ChestLootScreen`, `Minimap`. Validation par captures à taille d'usage et navigation, sans cocher les lots sur une simple maquette.

### R1 — lot engagé le 1er octobre

Travail seul, selon la demande de reprise. Capture de référence avant modification,
puis pause moins haute : navigation à gauche, équipement au centre, statistiques à
droite. Retrait des quatre familles de textes demandées ; noms, niveaux, valeurs
effectives, dégâts infligés et effets des objets/Réminiscences restent consultables.
Les colonnes conservent leur défilement et la taille du texte réglable.

Boutons communs : aplats sobres et un seul repère latéral pour le survol/focus,
sans superposer le cadre sélectionné d'un panneau au cadre d'un bouton. Vérifier
la pause à 100/130 %, un aller-retour dans les paramètres au clavier/manette,
et le survol avec le focus déjà présent. Les fonds de choix et leur rotation
ne font pas partie de ce lot.


**R1 livré et vérifié.** Navigation à gauche, équipement élargi au centre et
statistiques à droite ; hauteur ramenée de 900 à 780 unités d'interface.
Descriptions narratives d'armes et trois textes de pause retirés. Les valeurs
et les paliers restent disponibles, avec retour à la ligne et défilement.
Boutons : aplat, texte clair, repère cyan latéral unique au survol/focus ; aucun
cadre supplémentaire. Les autres écrans utilisant `UITheme` reprennent ce style.

La vérification a révélé l'absence des boutons A/B dans les actions natives
`ui_accept`/`ui_cancel` de Godot 4.7.2 : mappings explicités dans `project.godot`,
avec conservation d'Entrée, Entrée numérique, Espace et Échap. Le banc de capture
injecte les événements clavier/manette dans le viewport, sans prendre la main
sur le clavier de Raphaël : bas → Paramètres, A → ouverture, B → retour du focus,
puis survol/focus simultanés et Page bas. Ces étapes sont contrôlées, pas seulement
photographiées. La manette physique reste à recetter par Raphaël.

Captures regardées : `/tmp/vestiges-r1-before`, `…-after` (texte 100 %),
`/tmp/vestiges-r1-verified` (texte 130 %, navigation, survol et défilement).
La sortie de rendu locale est en 3840 × 2160 malgré la résolution demandée par
le script ; une réduction à 1280 × 720 a aussi été inspectée. Ce n'est pas un test
d'une fenêtre native 720p. Build sans avertissement, smoke 600 frames vert.

### R2 — lot engagé : icônes de nature du butin

Inventaire du chemin `LootRewards.Resolve` et de `chest_stat_bonus.json` : Essence,
XP, niveaux d'objet, Souvenir narratif, plus treize statistiques. Réutiliser les
icônes natives de stats (16 px), les orbes et le sprite propre à l'objet ; le
Souvenir emploie son signe mémoriel. Table visuelle en JSON, textures chargées
avant le défilement. Icône de nature à gauche, texte au centre, petit éclat de
rareté à droite ; les leurres changent de texte et d'icône ensemble.

Vérifier une ouverture réelle et une galerie de tous les gains dans Main,
dont Essence, PV et dégâts critiques côte à côte. Contrôle de couverture des
treize stats, rareté indépendante, aucun réglage de récompense ou d'audio changé.


**R2 livré et vérifié.** Les quatre familles de récompenses et les treize
statistiques ont une icône de nature distincte de la rareté. Table dans
`data/ui/loot_icons.json`, résolveur visuel partagé, sprite propre de chaque
objet. Les PNG existants suffisent ; aucun nouvel asset ni règle de butin.
Les icônes et libellés défilent ensemble ; résultat avec icône à gauche et
éclat de rareté 12 px à droite. Chargement des textures hors animation.

`test_ui_art.sh` : **169 contrôles passent**, dont couverture des treize stats,
ressources et sprites des 31 objets. Build sans avertissement, smoke vert.
Ouverture réelle puis galerie des treize stats, Essence, XP, objet et Souvenir
regardées dans `/tmp/vestiges-r2-loot` (100 %) et `/tmp/vestiges-r2-verified`
(130 %). La galerie emploie un Souvenir explicite, car le profil dev connaît
tout et convertit un tirage aléatoire en Essence. Elle présente les récompenses
sans les attribuer au joueur. Écran de rendu vérifié par le journal : X11,
écran 1, position (3840, 0), donc ViewSonic. Rotation I7 et audio inchangés.

### R6 — carte conservée au programme, premier périmètre préparé

Après les corrections courtes et le premier cycle audio, reprendre **radar et
carte agrandie**. Le code actuel de `Minimap` utilise un pixel par cellule
d'Effacement, agrandi en nearest ; le radar montre 26 cellules sur 104 unités.
La couleur de fond vient de la phase d'Effacement, sans dessin des biomes ou
chemins. Cela donne une piste concrète pour le caractère grossier et peu varié
signalé ; ce constat de code reste à confronter aux captures de référence.

Premier lot à borner : finesse du fond cartographique distincte de la grille
logique d'Effacement, palette, puis hiérarchie joueur/lieux/coffres et légende.
Conserver découverte et information de danger ; ne pas changer les règles de
l'Effacement pour améliorer le dessin. Capturer radar et carte entière aux
mêmes positions/seeds, avec et sans foule, à leur taille d'usage et avec grand
texte. Étendre au terrain seulement si les images confirment ce périmètre,
au plan 10. Aucun rendu de carte modifié dans cette reprise R1–R4.

### R6 — cartographie, lot engagé

Conserver le radar de proximité et la carte entière, mais dessiner le terrain
à quatre échantillons par axe de cellule d'Effacement (32 px monde au lieu de
128). Palette de charte par biome, eau et rues reconnaissables ; danger teinté
par phase, avec légende. Rasteriser seulement les nouvelles cellules explorées,
mettre en cache le fond et recolorer les cellules concernées par un changement
de phase. Ni règles d'Effacement, ni génération du monde modifiées.

Validation : référence avant/après aux mêmes positions, carte ouverte/fermée,
limites, changement de phase, légende et grand texte. Contrôles de découverte,
limites, cache et coût borné ; build/smoke, captures sur ViewSonic. Ne pas
confondre une image de carte améliorée avec une validation du terrain du monde.

### R6 livré — 1er octobre 2026

Fond cartographique quatre fois plus fin par axe, couleurs par biome, eau,
rues et chemins ; teintes de danger sur le cache, Néant connu hachuré. La
légende et la carte partagent un fond sombre et restent centrées ensemble,
y compris avec le texte à 130 %. Les petits repères ont un liseré de contraste.
La génération du monde, la découverte et les règles d’Effacement sont conservées.

**17 contrôles passent**, build sans warning et smoke 600 frames vert. Captures
Main avant/après regardées sur ViewSonic (radar, carte, danger injecté, bord).
[Protocole, logs et coût CPU](../audits/cartography-2026-10-01/README.md) :
0,0191 → 1,2404 ms pour le premier disque dans le micro-banc ; cache géré 2,66 Mo.
Machine chargée : comparaison FPS reportée, pas de garantie 60 FPS déduite.
Sortie réelle 4K, réduction de lecture 720p inspectée, pas de test natif 720p.
Le caractère gris et uniforme du fond est traité ; l’appréciation de Raphaël
et une éventuelle reprise du terrain du monde restent ouvertes.

### R7 — entrées des écrans de choix : lot engagé le 1er octobre

La reprise de L6b étant travaillée dans un autre checkout, isoler ici le
contrat de `ChoiceScreen`, commun aux Mémoriaux et aux Failles. Défauts lus :
une réouverture sans animation conserve le titre ou les actions transparents
si l'entrée précédente a été interrompue ; `Activate` peut sélectionner avant
la fin de l'entrée ; l'animation précédente peut viser des cartes supprimées.

1. Faire passer tous les chemins de validation par la même garde : premier
   appui = terminer l'entrée, suivant = choix. Conserver la navigation et les
   cartes désactivées ; ne modifier aucun son, prix, gain ni durée d'animation.
2. Arrêter l'ancien tween à chaque ouverture et remettre le titre, les actions
   et le fond dans leur état final avant une éventuelle nouvelle entrée.
   Détacher immédiatement les anciennes cartes lors d'une relance.
3. Vérifier ces cas avant/après avec un banc Godot headless, puis capturer les
   réouvertures dans Main sur ViewSonic. Vérifier pause continue, annulation
   autorisée seulement aux services/Failles et absence de double récompense.


**R7 livré et vérifié — 1er octobre.** La réouverture coupe l'entrée précédente
et restaure titre, sous-titre, actions, voile et fond. Les anciennes cartes
quittent immédiatement leur conteneur. Validation directe, bouton et input
partagent la garde d'entrée ; un premier appui ne choisit pas de bénédiction.
Le fondu termine aussi quand on passe l'animation. Rotation à 8°/s conservée.

[Preuves et limites](../audits/choice-screen-2026-10-01/README.md) : 24 contrôles,
12 échecs avant → zéro après ; 169 contrôles des assets/UI verts, build sans
warning, smoke 600 frames vert. Quatre captures Main regardées sur ViewSonic.
Prix, récompenses et audio inchangés. Test clavier/manette par événements
injectés ; appréciation en jeu toujours distincte de ces vérifications.

### R8 — plaque PV et niveau : lot du 7 octobre 2026

Demande de Raphaël : embellir le bloc PV/niveau montré en capture et corriger
ses décalages, dans la direction artistique du jeu.

1. Recomposer la plaque : badge cyan du niveau, métal sombre patiné et accents
   d'or discrets ; valeur de PV sur une ligne dédiée, jauge verte en relief.
   Utiliser les métriques réelles de la police et des conteneurs pour éviter
   que les chiffres débordent ou descendent sous leur cadre.
2. Conserver les états de combat : trace des dégâts, seuils orange/rouge,
   bouclier, Prévoyance et Reprise. Borner les remplissages, y compris à zéro.
3. Vérifier par captures de Main (PV pleins/bas/nuls, grands nombres, montée
   de niveau et bouclier), en 1080p et 720p ; build sans warning et smoke.

Lot unique, limité à la présentation de ce bloc et au placement de la rafale
sous celui-ci. Aucun changement des statistiques ou règles de combat.

**R8 livré et vérifié — 7 octobre 2026.** `VitalsDisplay` porte la présentation
PV/niveau : double filet sombre, coins coupés, accents d'or patiné, badge cyan,
signe de soin crème, rail vert avec lumière haute, ombre basse et graduations.
Les PV sont au-dessus du rail ; hauteur calculée depuis les métriques de Saira
(plaque 214 × 49 unités), chiffre de niveau sans contour épais. L'éclat de
niveau remplace le grossissement qui pouvait déborder ; les éclats successifs
interrompent proprement le précédent. La rafale d'éliminations suit la hauteur
de la plaque. Les seuils de couleur, la trace des dégâts et les jauges de
survie sont conservés ; à zéro, aucun remplissage résiduel après la trace.

Build **0 warning, 0 erreur**, smoke **600 frames vert**
(`/tmp/vestiges-smoke.SgDDfb`). Huit états capturés dans Main, seed 221092026,
aux tailles de fichier vérifiées **1920 × 1080** et **1280 × 720** : PV pleins,
trace de coup, blessure, danger, zéro, éclat au niveau 100, valeurs 1234/5678,
bouclier + Prévoyance + Reprise. Captures inspectées dans
`/tmp/vestiges-vitals-final-1080` et `/tmp/vestiges-vitals-final-720` ; contrôle
automatique des limites et de la séparation des lignes passé. Le scénario
`--capture-vitals [--vitals-resolution 1280x720]` injecte seulement des états
visuels, sans changer les statistiques du joueur. Une première série a aussi
été rendue en 4K avant les derniers détails de métal. Recette esthétique par
Raphaël encore ouverte.

**Retour de Raphaël — R8 compact, 7 octobre 2026.** « mieux mais ptet pas
besoin den faire un cadre aussi gros ? ». Resserrer le même dessin : largeur,
hauteur et marges du badge réduites, ligne des PV à la même taille de police,
rail légèrement aminci. Vérifier les huit états en 1080p/720p avant de clore.

**R8 compact vérifié.** Plaque ramenée de **214 × 49 à 190 × 38** unités
(−31 % de surface), badge resserré et rail de 9 unités au lieu de 11. Chiffres
des PV toujours à 11, niveau à 13 ; ombre plus courte et graduations plus fines.
Les huit états passent sans débordement aux tailles réelles 1920 × 1080 et
1280 × 720, même seed : `/tmp/vestiges-vitals-compact-1080` et
`/tmp/vestiges-vitals-compact-720`. Build sans warning.
Smoke 600 frames vert : `/tmp/vestiges-smoke.sJ507s`.

## 7. Refonte des menus — étude du 10 octobre 2026

**Périmètre après le retour sur les esquisses :** améliorer les parcours et la direction artistique de l'intérieur de Personnages, Quêtes, Collection, Paramètres et de la pause. **L'accueil actuel est conservé** : fond, logo, ambiance et composition générale. Raphaël apprécie le regroupement Paramètres / Quitter en bas ; c'est le seul ajustement de l'accueil retenu pour le plan. **Livrable : diagnostic, nouvelle étude illustrée et plan ; pas implémentation.** Les détails UX restent des propositions : Raphaël précise ne pas avoir lu tout le premier plan.

Les premières esquisses accueil/pause, métal et pierre, sont **rejetées**, même si Pierre et Effacement est la piste préférée entre les deux. Leur vérification en navigateur ne constitue pas une validation de leur design. Le présent §7.3 remplace leur direction artistique ; le §7.5 remplace le découpage initial M0–M5.

### 7.1 Diagnostic à partir du jeu actuel

Lecture de la Stratégie V2, de la Charte, de la Bible §9–10, des décisions récentes et des scripts. Captures réellement ouvertes : accueil et quatre pages en profil dev à 1920×1080 ; profil neuf à 1280×720 ; pause chargée et paramètres depuis Main. Le scénario de pause injecte quatre armes et six objets après vingt secondes de combat : état de lecture dense, pas mesure d'équilibrage. Ses images sont sorties en **3840×2160**, malgré la résolution 1920×1080 demandée au lanceur ; elles ne constituent donc pas une recette native 1080p de la pause.

Références hors dépôt : `/tmp/vestiges-menu-audit-20261010-hub`, `/tmp/vestiges-menu-audit-20261010-neuf`, `/tmp/vestiges-menu-audit-20261010-pause`. Ces chemins sont temporaires ; les constats et commandes sont consignés ici pour rester reproductibles.

| Constat vérifié | Conséquence pour le joueur | Traitement proposé |
|---|---|---|
| L'accueil choisit le héros par le camp et gauche/droite ; `HubCharactersPanel` présente les fiches au focus/survol, sans action de sélection. | Le joueur étudie un héros puis doit revenir au camp pour le retrouver et le choisir. Le problème est une fonction répartie sur deux écrans, pas deux catalogues identiques. | Ajouter la confirmation du choix et le départ depuis Personnages ; synchroniser le héros avec le camp, dont le fonctionnement est conservé. |
| `HubScreen.SetState` masque le menu principal sur chaque page ; chaque retour conduit à l'accueil. | Passer de Quêtes à Collection exige Retour puis Collection. | Navigation commune visible sur les pages secondaires ; une action pour changer de rubrique. |
| Les conditions de déblocage sont du texte dans les fiches de personnages et de collection ; les récompenses sont montrées dans Quêtes sans lien vers leur fiche. | Le joueur mémorise un nom et recherche la même entrée ailleurs. | Liens ciblés « Voir la quête » et « Voir le personnage / l'arme / l'objet », avec retour à l'origine. |
| Le fond de l'accueil comporte déjà feu, braises, dérive d'Effacement, yeux ponctuels et logo qui s'effrite. | Ajouter encore des particules ne donnera pas à lui seul une identité aux panneaux. | Construire la profondeur dans la matière des contrôles, les bords, la lumière et la composition. |
| L'accueil a des entrées textuelles avec une braise ; les pages ont un voile sombre ; les fiches ont des cadres fins ; pause et paramètres utilisent les textures communes, visuellement presque plates. | Plusieurs traitements sans hiérarchie matérielle commune ; impression d'interface générique sur une illustration. | Une même famille de surfaces, avec trois intensités adaptées à leur rôle. |
| La pause affiche une liste d'équipement longue et une fiche de stats séparée ; le stick droit / Page haut-bas défile les deux colonnes ensemble. | Même les six objets ne sont pas visibles en même temps dans la capture dense ; retrouver une règle exige du défilement. | Vue compacte du build, détail d'un élément sélectionné, statistiques regroupées ; défilement de la zone consultée seulement. |
| Paramètres est déjà le même composant depuis le Hub et la pause, avec rendu du focus au bouton d'origine. | Cette réutilisation est utile ; deux accès contextuels ne sont pas un doublon à supprimer. | Conserver ce contrat ; revoir le cadre et l'organisation de ses catégories. |
| « Retour au Hub » change directement de scène ; « Quitter » ferme le jeu. | Le libellé de retour masque le fait que la run est abandonnée. | « Abandonner la run » et « Quitter le jeu » ; confirmation expliquant les conséquences réelles, focus initial sur Annuler. |
| Le solde de Vestiges est visible à l'accueil ; `SpendVestiges` n'a aucun appel dans les scripts actuels. | Une monnaie attire l'œil sans destination utilisable dans ce Hub. | Constat conservé pour mémoire ; sa suppression est reportée hors périmètre, puisque l'accueil est conservé. |

Les quêtes et la collection ont un recouvrement légitime : **Quêtes = ce que je dois faire ; Collection = ce que fait le contenu.** Une même récompense peut apparaître dans les deux, à partir de la même source de disponibilité. Il faut relier ces deux vues, pas fusionner leurs rôles.

La page Chroniques est déjà supprimée et ne revient pas. Le Journal des Souvenirs existe séparément en run (touche J) : il porte le lore, pas les objectifs de déblocage. Son accessibilité et ses retours entrent dans la recette ; un nouveau menu de lore au Hub demande un cadrage distinct et n'est pas ajouté ici.

### 7.2 Parcours recommandé

**Accueil :** garder son fond, son logo, ses effets, le camp et la sélection actuelle. Partir reste le départ rapide. Prévoir seulement Paramètres et Quitter côte à côte en bas, séparés des quatre entrées principales. Ni nouvelle composition du camp, ni remplacement du fond, ni nouvel écran de préparation obligatoire.

**Pages secondaires :** navigation stable Personnages / Quêtes / Collection, accès à l'accueil et départ avec le héros confirmé. Titre, marges, pied de commandes et placement de l'action principale communs. Ne pas répéter la grande liste de l'accueil à côté d'une seconde barre de catégories : une seule navigation globale par écran.

| Page | Responsabilité | Action principale |
|---|---|---|
| Personnages | Comparer les styles, comprendre passif/arme/mobilité, choisir le héros. Portrait dominant ; résumé court ; chiffres secondaires à la demande. | « Choisir » confirme sans quitter la page ; « Partir » est directement accessible après le choix. Héros verrouillé : « Voir la quête ». |
| Quêtes | Toutes les quêtes visibles, progression et récompenses, filtres Toutes / En cours / Accomplies déjà demandés. | Consulter l'objectif ; ouvrir la fiche de chaque récompense. Pas de sélection de quête à équiper. |
| Collection | Catalogue Armes / Objets / Réminiscences, comportement, valeurs de base, disponibilité et conditions. | Consulter ; ouvrir la quête de déblocage si elle existe. Aucun achat fictif. |
| Paramètres | Audio / Affichage / Effets / Commandes ; langue et filtre daltonien groupés explicitement. | Modifier et revenir au contexte d'origine. L'emplacement exact de langue/accessibilité est à maquetter, sans ajouter de réglage. |
| Pause | Reprendre, comprendre le build actuel, régler le jeu, abandonner ou quitter. | Reprendre. Le build effectif n'est pas le catalogue de la Collection. |

**Contrat de navigation :** survol/focus = élément examiné ; validation = choix confirmé. Un héros examiné ne change jamais silencieusement le prochain départ. Les intitulés « Choisi », « Jouable », « À débloquer » distinguent l'état réel. Ne pas afficher « ??? » à la place du nom d'un héros déjà présenté dans le catalogue de déblocage.

L'accueil conserve le dernier héros confirmé disponible ; un simple passage sur un héros verrouillé ne désactive pas Partir avec ce héros confirmé. Une fiche verrouillée a un objectif et un lien utiles, sans faux bouton de départ.

Chaque page conserve élément sélectionné, filtre/onglet et position de défilement. Un lien Quêtes → Collection → Retour restaure la quête d'origine. Échap/B ferme d'abord le détail ou les paramètres, puis revient à l'origine ; dans la pause, il reprend seulement après fermeture du sous-écran. Afficher des indications correspondant au périphérique actif.

**Cibles vérifiables de parcours :**
- Accueil → run avec le héros confirmé : **1 action** d'activation.
- Page Quêtes → Collection : **2 actions actuellement** (Retour, Collection) → **1 action** proposée.
- Condition d'un contenu → quête correspondante : **1 lien ciblé**, sans recherche manuelle.
- Fiche Personnages → confirmer ce héros → lancer : **2 actions**, sans retour au camp.
- Paramètres → fermer : retour au même écran, même élément et même focus.

La proposition initiale de déplacer la graine et de masquer le solde est **reportée hors de cette passe**, comme le remplacement du fond. Le mode dev ne dicte pas le design des menus distribués.

### 7.3 Direction à explorer : pierre urbaine et traces effacées

**Base narrative relue :** [VESTIGES-LORE v1.1](../VESTIGES-LORE.md), validée au §62 et remplaçant la cosmologie de la partie I de la Bible. Le monde garde les lieux et perd leurs chemins ; les oubliés ont des métiers, des vêtements et des objets ordinaires. La pierre vient des rues et des maisons. Le blanc marque ce qui ne tient plus. Cela donne un vocabulaire concret : enduit usé, arêtes de maçonnerie, empreintes, inscriptions partielles et contours qui se raréfient. Le menu reste une convention d'interface ; il ne transforme pas l'accueil en lieu canonique.

Le lore doit se sentir dans la matière. Ne pas afficher les noms réels des personnages, les révélations ou de nouveaux slogans expliquant la mémoire. Les textes d'ambiance restent soumis au §10 du lore. Les libellés utiles restent simples : Personnages, Quêtes, Collection, Paramètres.

**Références effectivement regardées :**
- [Blasphemous, Collection](https://interfaceingame.com/screenshots/blasphemous-collectibles/) : illustration d'objet dominante, grille compacte, cadre périphérique et catégories communes. À retenir : une composition reconnaissable et des ornements dessinés ; son iconographie religieuse ne fait pas partie de Vestiges.
- [Dead Cells, équipement — capture officielle](https://steamcommunity.com/games/588650/announcements/detail/3383909822377684032) : icônes nettes, coins de cadres spécifiques, repères de catégories et commandes en pied. À retenir : la précision des états et des assets ; sa densité de descriptions n'est pas la cible de la pause de Vestiges.

Il s'agit d'une lecture graphique de ces captures, pas d'un constat mesuré de leur ergonomie. La proposition pour Vestiges est originale et reste à juger.

**Construction des écrans :**
1. **Un sujet dominant.** Le personnage, l'objet examiné ou le build donne sa composition à l'écran. Le portrait sort du rectangle de fiche ; l'objet est assez grand pour être reconnu. La navigation reste secondaire.
2. **Des supports dessinés.** Quelques morceaux de mur et de pierre, une tranche éclairée en haut à gauche, une ombre franche en dessous, des angles réellement cassés. Les formes irrégulières entourent une zone intérieure calme. Ne pas mettre un bloc de pierre autour de chaque ligne.
3. **Une profondeur par plans.** Fond éloigné, contenu posé au premier plan, quelques recouvrements et bords qui dépassent. Le décor ne concurrence pas les informations. Les pages intérieures peuvent avoir leur propre fond illustré sans changer l'image de lancement.
4. **Un Effacement localisé.** Contours périphériques qui perdent des pixels et traces incomplètes dans la matière ; une brève recomposition au changement de fiche. Aucun effacement des noms utiles, chiffres, icônes de commande ou indicateurs de progression. Éviter le bruit uniforme sur toute la surface.
5. **Des contrôles de jeu lisibles.** Repère de focus dessiné, petite encoche ou pointeur, tranche qui s'enfonce à la pression ; la marque du choix confirmé reste distincte du focus. Pas de pilules, de vitres floues ou de contour cyan générique appliqué partout. La surface cliquable reste régulière et assez grande même si le bord dessiné est cassé.

Palette issue de la Charte : pierre gris chaud, ombres indigo, textes ivoire ; or et orange ponctuels sur le choix actif, blanc d'Effacement en bordure ; cyan réservé à son rôle d'Essence. Les valeurs de rareté gardent leur palette existante. Pas de temple médiéval, de runes ni de dorures décoratives systématiques.

**Pixel art et texte :** dessiner les matériaux, coins, repères et icônes sur une grille cohérente ; agrandissement entier et contours nets. Préserver les sprites existants. Le texte reste rendu par Godot, avec Saira Semi Condensed pour les descriptions et les valeurs : il ne doit pas être intégré dans une image ni usé pour faire ancien. Une nouvelle forme de titre éventuelle doit être éprouvée avec les accents et les traductions. La planche illustrée n'autorise pas une modification du contrat de résolution de la Charte.

**Déclinaison envisagée, à examiner :**

| Écran | Composition propre | Matière et mouvement |
|---|---|---|
| Personnages | Galerie de petites silhouettes, grand personnage mis en scène, une fiche courte avec passif / arme / mobilité ; valeurs détaillées accessibles. | Fragment de rue et de mur ; bord du portrait qui se reconstitue au focus ; petite marque persistante sur le héros choisi. |
| Quêtes | Conserver la grille demandée, ses filtres, la progression et les récompenses. Un détail lisible pour la quête examinée. | Un support collectif patiné, repères de progression et médailles dessinés ; quelques traces et attaches. Éviter une forêt de cartes toutes encadrées d'or et les faux documents narratifs qui dévoilent l'histoire. |
| Collection | Grille dense d'icônes, grand objet examiné, une fiche ; Armes / Objets / Réminiscences clairement séparés. | Alvéoles discrètes dans un seul support, tranche et ombre de l'objet ; silhouette verrouillée reconnaissable, condition lisible et lien vers la quête. |
| Paramètres | Catégories stables, lignes alignées, valeurs visibles et retour au contexte d'origine. | Un seul plan de matière sombre ; curseurs encochés et poignées dessinées, cases gravées. Pas de transformation des réglages en énigme diégétique. |
| Pause | Actions courtes, build compact, détail examiné et statistiques regroupées (§7.4). | Même famille d'arêtes et de repères, décor beaucoup plus sobre ; conserver le lien visuel avec la run figée, sans rejouer l'illustration de Personnages. |

**Effets à prototyper après accord sur l'image :** apparition courte de la tranche, lumière locale au focus, enfoncement à la pression, quelques pixels qui se détachent des marges. Durées d'essai 140–200 ms ; réponse à l'input immédiate, sans attendre l'animation. Effets réduits : garder matière, contraste et repères, réduire le mouvement décoratif. Respecter les sons UI déjà choisis.

**Nouvelle étude illustrée :** [Personnages — pierre et Effacement, v2](planches/04-menus-personnages-pierre-v2.png), produite avec l'outil image_gen intégré à partir du portrait du Vagabond et du fond actuel comme références ([prompt exact](planches/04-menus-personnages-pierre-v2.prompt.json)). Elle montre la composition et les matières, **pas un écran Godot ni une direction validée**. L'accueil n'est pas redessiné.

Limites de cette planche : petites vignettes secondaires générées à remplacer par les portraits réels ; dessin encore illustratif à reprendre sur la grille pixel des assets ; typographie indicative. Les valeurs visibles viennent du catalogue actuel, mais la fiche complète devra conserver ses statistiques secondaires. Le bouton « Choisir » ne doit plus solliciter de confirmation quand le héros affiché est déjà choisi ; le libellé et le focus de cet état restent à détailler. Aucun asset de production ne sera obtenu en découpant les textes de cette image.

### 7.4 Pause : un build que l'on peut lire d'un regard

Garder une colonne d'actions courte à gauche. Au centre, montrer simultanément **quatre armes, six objets et quatre emplacements de Réminiscences**, avec icône, nom court, niveau/état ; les emplacements vides restent discrets. Cette vue reprend les limites actuelles, elle n'en crée pas de nouvelles.

Sous le build, un seul panneau de détail pour l'élément examiné : statistiques effectives de l'arme et dégâts infligés ; règle chiffrée et palier de l'objet ; effet et état actuel de la Réminiscence. Aucune description d'arme retirée au §44 n'est réintroduite. Le détail est lisible aussi au focus clavier/manette ; le survol seul ne suffit pas.

À droite, statistiques du héros regroupées par survie, attaque et exploration, avec Oublis dans une section identifiée. Valeurs actuelles et bonus distingués ; pas de record ni de comparaison à une ancienne run. Déplier les groupes secondaires si la résolution l'exige, sans retirer les statistiques utiles. Le temps de run et le héros peuvent figurer dans l'en-tête ; éviter de répéter le HUD complet derrière le menu.

Navigation explicite entre actions, grille et fiche ; stick droit/Page haut-bas agit sur la zone consultée. Reprendre toujours facile à retrouver. Les Paramètres remplacent temporairement le contenu de consultation ou en occupent un seul plan opaque : éviter deux cadres lisibles et deux voiles cumulés. L'état de pause est conservé à leur fermeture.

Le traitement des gains lors d'un abandon doit être lu dans le contrat de fin de run avant rédaction de la confirmation : ne pas annoncer une sauvegarde ou une perte qui ne correspond pas au code, ni modifier score/récompenses comme effet caché de cette refonte.

### 7.5 Lots proposés, un à la fois

| Lot | Livrable limité et concret | Critère de sortie |
|---|---|---|
| **M0 — Recherche réorientée** | Captures actuelles, références de jeux, présent cadrage et étude illustrée Personnages. Après retour, préciser le kit de boutons / cadres et montrer une déclinaison Collection ou Paramètres avec ses vrais contenus. | Parti pris visuel et organisation examinés. Les premières esquisses rejetées ne servent plus de cible ; aucun lot de code engagé par cette étude. |
| **M1 — Personnages, écran de référence** | Après accord sur la direction : produire les assets dessinés et leurs états, intégrer la composition, le choix confirmé et le départ depuis la fiche ; conserver la sélection du camp. | Écran abouti avec un héros choisi, un autre examiné et un verrouillé ; souris/clavier/manette, 720p/1080p/1440p, libellés longs ; aucun changement graphique de l'accueil. |
| **M2 — Quêtes et Collection, parcours reliés** | Décliner la matière selon le rôle des pages ; navigation directe entre les trois rubriques, liens quête/récompense, retour d'origine et conservation des filtres/focus. | Parcours du §7.2 rejoués en profils neuf et avancé ; état verrouillé clair et récompense retrouvée sans recherche manuelle ; grilles et informations utiles conservées. |
| **M3 — Paramètres et groupe utilitaire** | Décliner le kit sur les vrais contrôles et catégories ; composant commun accueil/pause ; regrouper Paramètres et Quitter côte à côte en bas de l'accueil. | Valeurs, focus et fermeture explicites ; retour au même contexte ; fond, logo, camp et autres commandes d'accueil conservés. |
| **M4 — Pause** | Build compact sélectionnable, détail unique et statistiques regroupées ; matières sobres, paramètres sur un seul plan, abandon/quitter explicites. | Build dense visible sans défiler sa grille ; chaque détail atteignable à la manette ; fermer les paramètres ne reprend pas la run ; Annuler ne perd rien. Captures natives en run obligatoires. |
| **M5 — Recette du périmètre** | Contrôler cohérence, traductions, focus et retours des cinq écrans ciblés ; nettoyer seulement les styles devenus inutilisés. | Accueil conservé → fiche héros → quête → récompense → retour → run → pause → paramètres → reprise → mort. Souris/clavier/manette, 720p à 1440p et écran large, effets réduits. |

**Ordre : M0 → M1 → M2 → M3 → M4 → M5.** La priorité est de juger une vraie composition de menu intérieur, puis de finir un écran de référence avant de décliner le kit. M4 garde sa recette en run séparée du smoke Hub. Bilan, Journal, écrans de choix, graine et solde ne sont pas refondus automatiquement : leur éventuelle harmonisation est un chantier ultérieur. Les contrats existants doivent continuer à fonctionner.

### 7.6 Cadre technique et validation

Réutiliser `UITheme` pour polices, états et styles, les résolveurs de disponibilité pour quêtes/catalogue, et `SettingsScreen` pour les deux contextes. La navigation du Hub garde un état explicite et un contexte de retour ; un lien ciblé transporte un identifiant, sans faire dépendre directement un panneau de l'autre. Le kit de surfaces peut être un petit composant réutilisable : pas de refonte générale de l'architecture pour une peau de menu.

Textures de matières chargées et partagées ; animation uniquement pour le contrôle actif et les éléments ambiants bornés. Aucun bruit procédural, chargement de ressource ou création de nœuds par frame. Le fond et ses particules doivent être suspendus quand entièrement cachés. L'abandon, le changement de scène et les écrans imbriqués doivent conserver un propriétaire cohérent de la pause.

Pour chaque lot de code : `dotnet build` **zéro warning**, smoke si scènes/initialisation/rendu touchés, contrôles de focus et de retour adaptés au changement. Comparer les captures avec les mêmes profils, héros, résolution effective et état du build. Vérifier les dimensions des PNG, car le lanceur de run n'a pas garanti la résolution demandée pendant cette étude. Pour toute affirmation de coût ou d'optimisation, banc avant/après sur machine calme ; aucune conclusion de FPS tirée ici.

**État après le retour :** accueil conservé et groupe Paramètres / Quitter en bas retenus pour le plan ; préférence de recherche pour Pierre et Effacement ; premières esquisses rejetées. Nouvelle planche intérieure produite, à examiner, et lots réorientés. Les détails UX restent proposés. Aucun menu de jeu modifié, aucune case de roadmap cochée. Les cases déjà cochées restent le constat des livraisons précédentes.

**Vérifications de l'étude initiale :** les trois lanceurs de capture aboutissent, dont les inputs injectés du scénario pause ; `dotnet build --nologo` : **0 warning, 0 erreur**. Les contrôles navigateur des anciennes esquisses portaient sur leur fonctionnement, pas sur leur qualité artistique ; elles restent rejetées. La nouvelle planche est une image fixe examinée visuellement : elle ne vérifie ni les transitions, ni le focus réel, ni une manette physique. Aucun gain de performance revendiqué.

**Vérification de la reprise :** `dotnet build --nologo` relancé, **0 warning, 0 erreur** ; `git diff --check` propre. Planche enregistrée en 1672×941 : format d'étude, pas capture à une résolution du jeu. Aucun script, scène, shader ou asset de production changé dans ce chantier ; smoke non relancé pour ces seules modifications documentaires.

## 8. Production autorisée — 10 octobre 2026

Raphaël donne carte blanche pour refaire les menus sur une branche distincte de main. La planche Personnages lui plaît, mais le nom doit être discret et les statistiques d'arme/mobilité quittent la présentation principale. Il envisage Partir / Recommencer → choix du personnage → run ; l'agent retient ce parcours pour sa lisibilité et sa capacité à accueillir dix héros. Le camp reste le décor de l'accueil et présente les personnages débloqués, sans les sélectionner. Aucune sélection de carte fictive n'est ajoutée.

Branche : `codex/refonte-menus`, worktree isolé. Les modifications des autres chantiers sur main ne sont pas embarquées. Les lots ci-dessous remplacent M0–M5 pour cette production autorisée, sans demande d'accord supplémentaire.

| Lot | Périmètre | Validation |
|---|---|---|
| P1 — Départ et personnages | Camp d'ambiance ; Partir et Recommencer ouvrent la sélection ; portrait, nom discret, fiche secondaire ; navigation entre pages et retours explicites. | Build sans warning, démarrage et héros verrouillé, focus. |
| P2 — Matières et catalogues | Décor intérieur original, cadres et états dessinés ; Quêtes et Collection, conservation de la consultation et liens ciblés. | Captures et navigation en profil neuf/dev, 720p et 1080p. |
| P3 — Pause et réglages | Build compact, détail ciblé, abandon confirmé ; Paramètres sur un seul plan, utilitaires en bas ; cohérence des contrôles partagés. | Build, smoke, régressions concernées, captures de pause et réglages en run. |

Le HUD de combat, les mécaniques, les récompenses et le lore ne sont pas rééquilibrés. Les menus de choix/bilan gardent leurs contrats ; les contrôles partagés profitent de la nouvelle matière. Pas de fusion vers main dans ce chantier.

### 8.1 Implémentation P1–P3

- **Départ.** L’accueil conserve sa peinture, son titre et son animation. Le camp montre les héros débloqués ; les flèches n’y changent plus le choix. Partir ouvre Personnages. Le choix n’est confirmé qu’au départ depuis la fiche. Recommencer au bilan revient directement à cette sélection, puis lance une nouvelle run. L’état global revient à Hub entre les deux runs.
- **Personnages.** Galerie verticale défilante, illustration dominante avec tranche dissoute en pixels, nom 30 px, description et passif courts, arme liée à la Collection. Les chiffres d’arme et de mobilité sont repliés sous Détails du personnage. Les héros verrouillés exposent leur quête et ne peuvent pas partir.
- **Catalogues.** Navigation directe Personnages / Quêtes / Collection. Une quête ouvre sa récompense ; un contenu verrouillé mène à sa quête. Retour restaure l’entrée consultée, la catégorie de Collection et le filtre de quêtes quand il contient la cible. Historique borné aux huit dernières visites. Les personnages futurs sans définition jouable restent « À venir ».
- **Matières.** Fond intérieur et cadre originaux, pierre urbaine et Effacement ivoire, surfaces de lecture opaques. Boutons ébréchés, tranches éclairées, focus doré. Le portrait garde sa couleur d’origine (modulation appliquée une seule fois). Le camp caché est suspendu. Les textures sont partagées, sans génération de texture par frame. [Assets et consignes de génération](../../assets/ui/menus/stone/GENERATION.md).
- **Pause.** Quatre armes, six objets et les emplacements de Réminiscences visibles ensemble ; souris, focus ou validation changent la fiche de détail. Les règles, niveaux, paliers et dégâts infligés restent consultables. La colonne des statistiques défile indépendamment, Oublis compris. Abandonner et Quitter demandent confirmation ; le choix initial conserve la partie, Échap annule sans reprendre la run.
- **Paramètres.** Écran opaque commun à l’accueil et à la pause, quatre onglets, curseurs dessinés, commandes de remappage agrandies. Fermer retrouve le contrôle d’origine et conserve la pause. Paramètres et Quitter sont côte à côte en bas de l’accueil.

La langue des nouveaux libellés suit le français déjà utilisé par ces menus ; la localisation intégrale des menus reste un travail distinct. Le HUD de combat et les règles de progression ne changent pas. Pas de promesse de performance : aucune mesure FPS sur cette machine chargée.

### 8.2 Validation et captures — P1–P3 livrés

- `tools/validate.sh /tmp/vestiges-menus-validation-finale smoke menu_navigation choice_screen dev_mode ui_art input_remap music` : **7/7**, build **0 warning, 0 erreur**, aucune source modifiée pendant les suites.
- Dernier verrou ajouté au départ : la transition absorbe les entrées avant les contrôles. Validation ciblée `/tmp/vestiges-menus-verrou-depart` : **smoke et menu_navigation 2/2**, **16 contrôles de parcours**, build toujours sans warning. Aucun rendu changé par ce dernier verrou.
- Profil neuf, 1280×720 : Partir → Vagabond, héros verrouillé → quête précise → retour au héros ; retour accueil → Paramètres, quatre onglets. Captures et logs : `/tmp/vestiges-menus-neuf-720`.
- Profil dev, 1920×1080 : camp, personnages et quêtes (`/tmp/vestiges-menus-final-hub`) ; Collection et paramètres 720p (`/tmp/vestiges-menus-final-720-v2`). Le portrait assombri de la première capture a été corrigé et recapturé ci-dessous.
- Vraie pause : quatre armes, six objets, détail du Papier carbone niveau 30, neuf Oublis ; paramètres ouverts/fermés par événements manette A/B, focus restitué, abandon annulé sans reprise, défilement limité à la colonne sélectionnée. Scénario **vert** à 1080p (`/tmp/vestiges-menus-final-pause-v2`) puis à 720p avec le fond opaque et les statistiques agrandies (`/tmp/vestiges-menus-pause-720`).
- Mort réelle → Recommencer → sélection du Vagabond à la place du Traqueur → Partir → Main initialisé et jouable : scénario **vert**, `/tmp/vestiges-menus-restart-final`. La capture finale montre la nouvelle run après son chargement, pas seulement la transition.
- Deux défauts des bancs corrigés : l’ancienne ouverture privée de Collection avait été retirée ; le délai du test de relance comptait des frames de rendu alors que la transition avance en physique. Les bancs utilisent maintenant le nouveau parcours et attendent l’état réel du monde. Le test de défilement ajoute les Oublis pour exercer un contenu effectivement plus haut que la colonne.

Captures conservées dans la branche : [Personnages en jeu, 1080p](planches/04-menus-personnages-integre.png) · [Pause en jeu, 720p](planches/04-menus-pause-integree.png).

Les dimensions des PNG ont été contrôlées. Les parcours clavier/manette utilisent les événements du moteur ; aucun essai sur manette physique n’est revendiqué. La direction artistique reste à juger en jouant par Raphaël. Les tests de cette livraison ne constituent ni un banc FPS ni une recette de traduction complète. Branche locale isolée, aucune fusion ni publication.

## 9. Achèvement de tous les menus — 11 octobre 2026

Raphaël demande de terminer la refonte sur `codex/refonte-menus`, y compris montée de niveau, butin, bilan et journal, puis de tester l'ensemble. Direction P1–P3 conservée : pierre sombre ébréchée, Effacement, pixel art, or discret ; accueil et camp conservés.

Lots prévus avant code, réalisés et vérifiés dans cet ordre :

- **P4 — choix et butin.** Composition large et bornée à l'écran, mêmes matières que les menus intérieurs ; inventaire/statistiques consultables sans masquer le choix. Inclure les choix de Mémorial/Faille/Autel qui partagent `ChoiceScreen`. Préserver raretés, gains, bannissement, relance, file de niveaux et garde d'entrée. Butin : révélation puis bouton explicite pour repartir, pas de fermeture avant lecture.
- **P5 — journal.** Constellations, fragments et lecture séparés ; états inconnus explicites, défilement, sélection conservée, fermeture et navigation clavier/manette. Vérifier le retour à la run et l'absence de conflit avec les autres pauses.
- **P6 — bilan.** Résumé de l'expédition, record réservé au bilan, équipement/faits et gains organisés sans débordement ; détails consultables, tous les gains accessibles. Garder révélation, sauvegarde, absence de double attribution et Recommencer → choix du personnage.
- **P7 — recette commune.** Build sans warning, smoke, régressions pertinentes et nouveaux contrôles des parcours ; captures réelles en 1080p/720p, texte agrandi, profils neuf/dev. Examiner les images et corriger avant livraison ; consigner les preuves durables et les limites exactes.

Aucune modification d'équilibrage ni de règles de progression. Le HUD reste un affichage de combat ; les écrans interactifs de la run entrent dans cette reprise.

### 9.1 Implémentation P4–P6

- **Choix de niveau.** Grandes dalles opaques, icônes 96 px, gain au premier plan, rareté portée par l'éclat et la tranche. Inventaire et statistiques séparés, noms longs renvoyés à la ligne, défilement des offres et retour du focus après renouvellement. Relancer, bannir et passer gardent leurs compteurs ; Échap annule le mode bannissement. La file de niveaux garde la run suspendue jusqu'au dernier choix.
- **Rencontres.** Mémorial, Faille et Atelier partagent la même pierre et le même fond d'Effacement. Fenêtre large, hauteur adaptée au nombre de cartes et plafonnée ; les offres longues défilent, le bouton de sortie reste en place. Les prix et contreparties restent visibles. Les contrôles de première validation, de choix obligatoire et de récompense unique sont conservés.
- **Butin.** Coffre illustré, nature du gain et rareté séparées. Révéler puis Reprendre la route : le résultat reste affiché jusqu'à la confirmation. La fermeture automatique est supprimée ; l'audio termine sa révélation pendant la lecture. Une confirmation ne peut attribuer le gain qu'une fois. Hauteur adaptée à la quantité de butin, défilement au-delà.
- **Journal.** Trois espaces : constellations, index, lecture. Constellation gravée indiquant ce qui est retrouvé, texte plus grand, emplacements inconnus sans spoiler, fragment consulté mémorisé. J et Échap/B ferment ; Retour fonctionne à la souris. Entrée Journal ajoutée à la pause et liée dans l'ordre de focus. Fermer rend le contexte d'origine, sans reprendre la run lorsqu'on venait de la pause.
- **Bilan.** Score, distance et durée en tête ; nom discret sous le portrait. Deux vues sur place : Expédition (cause, faits, frise) et Équipement (armes, dégâts, objets et Réminiscences). Les gains occupent une colonne indépendante, avec défilement pour les listes longues. Révélation et garde de validation conservées ; Rejouer ouvre le choix du personnage. Les enregistrements de run et l'attribution des gains restent dans leurs services existants.
- **Cohérence.** La notification de Souvenir reprend la pierre. Les anciennes frises de carte devenues inutilisées sont retirées ; les éclats de rareté restent animés. La matière du fond garde ses couleurs d'origine, sans multiplier deux fois la texture dans le shader.

La recette a détecté et corrigé l'entrée Journal absente de la boucle de focus de la pause, ainsi que le défilement qui pouvait suivre le pointeur au lieu de la colonne focalisée. Les grands textes de l'inventaire se renvoient à la ligne. Le banc de bilan envoie désormais le relâchement du bouton A, nécessaire aux boutons natifs.

### 9.2 Validation — P4–P7 livrés

Validation générale **15/15 suites**, build **0 warning, 0 erreur** ; après les derniers ajustements de hauteur, couleur et focus, validation finale **5/5** (smoke, menu_navigation, choice_screen, ui_art, input_remap). Les deux passes constatent zéro source modifiée pendant les tests. La suite de choix couvre désormais **41 contrôles**, et la navigation du Hub conserve ses **16 contrôles**.

Huit scénarios dans Main puis cinq reprises visuelles ont passé : pause, cascade de niveaux, Mémorial, Faille, Atelier, butin, journal et bilan. Captures 1280×720 et 1920×1080 inspectées, dont texte agrandi à 130 %, profil neuf et longues listes de gains. La Faille a été recapturée après le dernier ajustement pour montrer les trois offres et leurs contreparties en entier. Le parcours mort réelle → bilan → Rejouer → autre personnage → nouvelle run jouable a passé.

[Rapport, captures et logs conservés](../audits/menus-2026-10-11/README.md). Les états denses sont des fixtures explicites ; les neuf annonces de quête du bilan ne représentent pas neuf quêtes réellement accomplies. Entrées manette injectées, pas d'essai sur manette physique. Pas de conclusion de FPS ni de validation artistique au nom de Raphaël. Les lots demandés sont terminés sur la même branche, sans fusion vers main.

### 9.3 Préparation de la PR — 11 octobre 2026

À la demande de Raphaël, PR préparée depuis la même branche. Synchronisation avec `origin/main` (`6a353705`) : conflits limités à quatre documents, autres chantiers conservés et décisions des menus renumérotées §114–115. Après synchronisation : **16/16 suites** (quinze suites générales et audio), build **0 warning, 0 erreur**, aucune source modifiée pendant les tests. [Preuves](../audits/menus-2026-10-11/README.md#vérification-avant-pr-après-synchronisation-avec-main). Cette préparation n'intègre pas la refonte dans main.

## 10. Paramètres : curseurs et bouton fermer — 14 octobre 2026

Demande de Raphaël ([DECISIONS §179](DECISIONS.md)) : des curseurs plus beaux et dans la DA, un bouton fermer carré avec une vraie croix.

- **Curseurs** (`MenuSkin.SliderTrack`, `SliderFill`, `SliderThumb`) : rainure creusée dans la pierre (lèvre haute dans l'ombre, paroi basse éclairée), incrustation de bronze jusqu'à la poignée, taquet de bronze à pans coupés modelé en quatre tons et gravé d'un trait, plus clair au survol et au focus. Pixels doublés comme les dalles, contour `1a1a2e`, lumière en haut à gauche. Hauteur de la rainure 18 px, poignée 26 × 38 px.
- **Fermer** : dalle carrée de 48 px qui ne s'étire plus avec sa rangée, croix dessinée (`MenuSkin.CloseCross`) teintée par l'état du bouton (texte au repos, or au survol et au focus) ; l'aide du bas devient « [Échap] Fermer ».
- Vérifié par capture (`HUB_EXTRA_ARGS="--open-settings" tools/capture_hub.sh`, option ajoutée à `HubCapture`) : avant, piste d'un trait à peine visible et bouton « X » étroit ; après, images regardées à 1080p et agrandies.

## 11. Level-up au fil — 14 octobre 2026

Demande de Raphaël ([DECISIONS §182](DECISIONS.md)) : l'écran de level-up ressemblait trop à Megabonk ; maquette A « Le fil » retenue parmi trois.

**Lot F1, l'écran au fil (un seul lot, l'écran entier change d'un coup) :**
- Fragments : papier vertical (380 px de large, hauteur selon le texte), teinte du papier par la rareté, photo de l'objet (icône ×4) au bord blanc et scotchée, type et rareté, nom, libellé du gain puis valeur en grand, ligne « et … » et règle ou ambiance en petit, tampon NOUVEAU / Niv / Palier. Pinces sur un fil courbé entre deux clous ; fragments un peu de travers, le fragment survolé se redresse, monte et prend un liseré doré.
- Ceinture en bas : poches d'armes, d'Écussons, du sac (sur deux rangs au-delà de 12 places) et des Réminiscences. La poche concernée s'allume ; une nouveauté montre son icône en fantôme dans la première poche libre.
- Étiquette de ce qui change sous le fragment survolé : valeur avant → après pour une amélioration, valeur actuelle du joueur pour un nouvel objet de stat, emplacement visé pour une arme ou une Réminiscence.
- Étiquettes d'actions aux clous (Relancer, Bannir, Passer) ; navigation gauche/droite entre fragments et étiquettes, bas vers les actions, haut vers les fragments.
- Toutes les stats en surimpression avec la touche du journal.
- Animations : le fil descend à l'ouverture avec un léger balancement ; un fragment banni ou relancé tombe et s'efface.
- Textures procédurales en C# (comme les curseurs des Paramètres, `MenuSkin`), aucun PNG nouveau ; noms des méthodes appelées par réflexion par les bancs conservés (`SetFocus`, `Activate`, `Skip`, `ToggleBanish`, `OnCardChosen`, `_fragmentManager`).
- Vérification : build, `tools/test_choice_screen.sh`, `tools/test_inventory.sh`, captures `--capture-levelup` regardées.

**F1 livré :** `LevelUpScreen` réécrit, `PaperFragment` (un fragment), `ClothesLine` (fil, clous, ficelles), `InventoryBelt` et `BeltPocket` (ceinture), `PaperArt` (matières au pixel), `UpgradeParts` (texte découpé : `UpgradeText.Describe` en est recomposé, mêmes lignes pour la Faille). Au-delà de trois fragments (Trèfle plastifié), ils rapetissent et penchent moitié moins ; ceux des bords penchent vers l'extérieur. Le fragment survolé passe devant (ZIndex 1), pinces et étiquettes restent devant lui. Un fragment relancé ou banni tombe derrière les nouveaux ; le banni s'éteint vers le sombre. Le panneau des stats a un voile qui retient les clics ; `_Process` ne tourne que panneau ouvert.

**F2, plus de détails (retour du soir) :** tirage photo de 304 × 176 px par teinte et quatre cadrages : bord blanc jauni aux coins, ciel de crépuscule qui s'allume vers l'horizon, immeubles en ombre chinoise aux fenêtres parfois allumées, halo derrière l'objet, sol pavé et ombre portée, vignettage, grain, rayures, poussière, fuite de lumière orangée ; cinq tons tramés par Bayer, sépia pour un nouvel objet, turquoise des Mémoriaux pour une Réminiscence, or pour une voie. L'objet a son ombre portée sur le tirage. Papier uni, ligné ou quadrillé (lignes calées sur la zone répétée), fibres, ombre sous le rabat corné, rond de tasse et pliure tirés au hasard, ombre portée de la feuille. Scotch froissé, pince veinée à ressort enroulé, clous rouillés, ficelle torsadée, ceinture grainée et éraflée, poches cousues teintées par la rareté, rivets de cuivre entre les groupes.

**Vérification :** build sans avertissement ; captures `--capture-levelup` (cinq raretés, ascension, Trèfle à quatre fragments, Réminiscences, Chance, focus actions et objet, stats, mode bannir, chute) et `--capture-cascade` regardées à 1080p ; cascade (11 contrôles, borne d'écran de la ceinture et des fragments au lieu de l'ancien `_layout`), `test_objects`, `test_inventory`, `test_choice_screen` et smoke verts. Relecture `godot-reviewer` : aucun bloquant, quatre points faibles corrigés (repère de l'étiquette pendant l'entrée, clics sous le panneau des stats, `_Process` permanent, espacement de la flèche des objets dans la Faille).

**Reste à juger en jouant :** lisibilité du texte sur papier clair en mouvement, rythme de la descente et des chutes, place de l'étiquette sous les fragments longs.

**F3, actions regroupées et raccourcis :** les trois actions en colonne au clou de droite (l'étiquette s'écarte du bord si elle est trop large, sa ficelle part en biais), « Toutes les stats » cliquable au clou de gauche à la place de l'aide en haut à droite. Actions `levelup_reroll` (R, Y), `levelup_banish` (B, X), `levelup_skip` (P, LB) déclarées dans `project.godot`, non remappables (raccourcis de menu) ; la touche s'affiche sous l'œillet, au clavier ou à la manette branchée. La cascade relance et bannit désormais par ces actions ; cascade, écrans de choix, inventaire et smoke verts, capture regardée.

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

# VESTIGES — Roadmap & Milestones

> **Version :** 1.2
> **Dernière mise à jour :** 21 février 2026
> **Objectif final :** Early Access Steam
> **Docs liés :** GDD v1.3, Bible Artistique & Narrative v1.0, Architecture v2.1

---

## PHILOSOPHIE

### Règles de la roadmap

- **Chaque milestone produit quelque chose de jouable.** Pas de milestone "infrastructure invisible". Même le setup Godot se termine par un personnage qui bouge.
- **Itérer, pas perfectionner.** Carrés gris, placeholders, sons gratuits — on s'en fout. Le fun se teste avec du moche. Le polish vient à la fin.
- **Tester le fun le plus tôt possible.** Si la boucle de base (bouger + auto-attaque + ennemis) n'est pas fun au milestone 2, il faut pivoter AVANT de construire 15 systèmes par-dessus.
- **Un système à la fois.** On ne code pas le craft ET le level-up ET la construction en parallèle. Un système, on le teste, on valide, on passe au suivant.
- **Le graphique vient en dernier.** Le graphic designer intervient quand le jeu est fun en carrés gris. Pas avant.

### Référentiel de complexité

| Niveau | Définition |
|--------|------------|
| C1 — Faible | Tâche localisée, peu de dépendances, risque technique faible. |
| C2 — Moyenne | Tâche claire avec quelques interactions système, validation simple. |
| C3 — Élevée | Tâche multi-systèmes, équilibrage nécessaire, risque de régression. |
| C4 — Très élevée | Tâche structurante, impact large, forte incertitude technique/game design. |

Les niveaux de complexité servent à prioriser l'effort et le risque. Ils n'impliquent aucun engagement calendaire.

---

## PHASE 0 — FONDATIONS

> **Objectif :** Être à l'aise avec Godot et avoir un personnage qui se déplace sur une map isométrique.
> **Livrable :** Un personnage (carré coloré) qui se déplace au ZQSD sur une grille isométrique avec une caméra qui suit.

### Tâches

**Lot 0.1 — Apprendre Godot (`Complexité : C2`)**
- [x] Installer Godot 4 avec le support C# (version stable.mono).
- [x] Moteur de rendu : **Compatibilité** (OpenGL, optimal pour la 2D, meilleure compatibilité hardware).
- [ ] Suivre 2-3 tutoriels officiels Godot (le "Getting Started" + un tuto isométrique).
- [ ] Comprendre : Scenes, Nodes, Signals, TileMaps, le système de coordonnées iso, PointLight2D, CanvasModulate.
- [x] Résultat : un projet vide qui compile avec une scène de test.

**Lot 0.2 — Mouvement isométrique (`Complexité : C3`)**
- [x] Créer un TileMap isométrique basique (tiles placeholder carrés/colorés).
- [x] Implémenter un personnage (carré) avec mouvement ZQSD en coordonnées isométriques.
- [x] Caméra qui suit le joueur avec un léger smoothing.
- [x] Résultat : le joueur se déplace fluidement sur une map iso.

**Lot 0.3 — Structure du projet (`Complexité : C2`)**
- [x] Mettre en place la structure de dossiers du projet (cf. Architecture v2.1 pour les couches logiques).
- [x] Mettre en place Git + .gitignore Godot. Branches : `main` ← `dev` ← `feature/xxx`.
- [x] Créer les premiers fichiers de données JSON (template pour les stats, les ennemis, les perks).
- [x] Documenter les conventions de code (nommage, architecture).
- [x] Mettre en place les autoloads essentiels : GameManager (machine d'états), EventBus (signaux typés).
- [x] Résultat : un projet propre, versionné, prêt à scaler.

### Critère de validation
- [x] Je peux lancer le jeu, déplacer un carré sur une map iso, et la caméra suit. Le projet est sur Git avec la bonne structure. Les autoloads de base (GameManager, EventBus) existent.

---

## PHASE 1 — LE PROTOTYPE DE COMBAT

> **Objectif :** Valider que la boucle auto-attaque + ennemis + déplacement est FUN.
> **Livrable :** Un mini Vampire Survivors en iso — le joueur se déplace, auto-attaque les ennemis qui spawn, gagne de l'XP, et level up avec choix de perks.

### Tâches

**Lot 1.1 — Auto-attaque (`Complexité : C2`)**
- [x] Le personnage a une auto-attaque (projectile simple ou frappe en arc selon l'arme).
- [x] L'attaque touche les ennemis et leur retire des HP.
- [x] Feedback visuel : flash de hit, nombre de dégâts qui pop.
- [x] Résultat : le joueur se déplace et son personnage attaque automatiquement.

**Lot 1.2 — Ennemis basiques (`Complexité : C3`)**
- [x] Créer 2 types d'ennemis (carré rouge = mêlée, triangle rouge = distance).
- [x] Les ennemis spawn autour du joueur en continu depuis un **object pool** (pas d'instanciation/destruction à chaque spawn).
- [x] Les ennemis se déplacent vers le joueur (pathfinding basique).
- [x] Les ennemis ont des HP, meurent (effet de désintégration en particules sombres — dès le prototype, même simple), droppent de l'XP (orbes dorées attirées vers le joueur).
- [x] Stats d'ennemis chargées depuis JSON (data-driven dès le départ).
- [x] Résultat : une arène infinie avec des ennemis qui spawn et se désagrègent.

**Lot 1.3 — XP et Level Up (`Complexité : C3`)**
- [x] Le joueur ramasse l'XP automatiquement (rayon d'attraction, comme VS/Megabonk).
- [x] Barre d'XP visible. Au level up → écran de choix de 3 perks.
- [x] Implémenter 6-8 perks basiques (dégâts+, vitesse+, HP+, vitesse d'attaque+, projectile supplémentaire, AoE).
- [x] Les perks s'appliquent immédiatement et se stackent.
- [x] Résultat : le joueur level up et sent la montée en puissance.

**Lot 1.4 — Polish et test du fun (`Complexité : C2`)**
- [x] Score basique (kills).
- [x] HP du joueur + mort + écran de game over avec score.
- [x] Équilibrage basique (spawn rate, HP ennemis, scaling).
- [x] **TEST CRITIQUE : est-ce que c'est fun ?** Faire tester à 2-3 personnes.
- [x] Résultat : un mini-jeu complet et jouable. La boucle combat est validée (ou pas → itérer).

### Critère de validation
- [x] Un ami peut jouer 15 minutes et avoir envie de relancer pour battre son score. Si non → itérer sur cette phase AVANT de continuer.

---

## PHASE 2 — LE CYCLE JOUR/NUIT

> **Objectif :** Transformer l'arène infinie en boucle jour/nuit avec deux dynamiques distinctes.
> **Livrable :** Le joueur explore de jour (ennemis dispersés, réalité stable) et défend le Foyer la nuit (vagues convergentes, réalité qui s'effiloche). Survie de plusieurs cycles avec scaling.

### Tâches

**Lot 2.1 — Timer et cycle visuel (`Complexité : C2`)**
- [x] Implémenter le timer jour/nuit avec barre de progression visible (soleil qui s'éteint — pas de lune, la nuit est du vide).
- [x] Transition visuelle via CanvasModulate : palette vivante (jour doré) → crépuscule (désaturation, bleu-violet) → palette corrompue (noir vrai, seules les lumières existent).
- [x] Sources de lumière basiques avec PointLight2D (le Foyer = carré jaune avec halo orange-doré).
- [x] Résultat : la map change visuellement, un timer défile, la nuit est SOMBRE.

**Lot 2.2 — Comportement jour vs nuit (`Complexité : C3`)**
- [x] **Jour :** Les ennemis spawnent de manière dispersée sur la map (hors rayon du Foyer). Le joueur va vers eux.
- [x] **Nuit :** Les ennemis spawnent en vagues depuis les bords et convergent vers le Foyer (ils cherchent à consommer la lumière/mémoire).
- [x] Le Foyer a un rayon de sécurité visuel : les tiles à l'intérieur sont plus nets/colorés que l'extérieur (même en placeholder, le contraste doit exister).
- [x] Résultat : le jeu a deux phases distinctes et ça se ressent. Le Foyer EST la base.

**Lot 2.3 — Scaling et nuits multiples (`Complexité : C3`)**
- [x] À l'aube, résumé basique (kills, score de nuit).
- [x] La difficulté des nuits augmente (nombre d'ennemis, HP).
- [x] Ajouter 2-3 types d'ennemis supplémentaires (lent+résistant, rapide+fragile, à distance).
- [x] Résultat : le joueur peut survivre plusieurs nuits avec un scaling qui monte.

**Lot 2.4 — Score et mort (`Complexité : C2`)**
- [x] Système de score complet (survie + kills + bonus nuit sans dégât).
- [x] Effet de mort : le monde se décompose visuellement autour du joueur (même en placeholder : tiles qui disparaissent, fade to white). Transition vers écran de score.
- [x] Écran de mort avec score détaillé + record personnel.
- [x] Bouton "Relancer" immédiat — zéro friction.
- [x] Sauvegarde locale du meilleur score.
- [x] Résultat : la boucle complète fonctionne — jouer → mourir → score → relancer.

### Critère de validation
- [ ] Le cycle jour/nuit change vraiment la dynamique. Le joueur se dit "merde la nuit arrive" et change de comportement. Le score donne envie de faire mieux.

---

## PHASE 3 — LA BASE

> **Objectif :** Ajouter le layer construction/craft qui différencie VESTIGES d'un Vampire Survivors.
> **Livrable :** Le joueur récolte des ressources, retourne à la base, craft des murs/pièges, et défend sa base la nuit.

### Tâches

**Lot 3.1 — Récolte de ressources (`Complexité : C2`)**
- [ ] Nœuds de ressources sur la map (arbres = bois, rochers = pierre, débris = métal).
- [ ] Interaction : le joueur s'approche, touche une touche, animation courte, ressources dans l'inventaire.
- [ ] Inventaire basique (liste de ressources avec quantités).
- [ ] Résultat : le joueur peut récolter en explorant.

**Lot 3.2 — Système de craft (`Complexité : C3`)**
- [ ] Menu de craft (panneau latéral, pas plein écran).
- [ ] 8-10 recettes de base : mur bois, mur pierre, piège à pointes, barricade, torche, bandage, arme T2.
- [ ] Craft en temps réel (timer court, le joueur est vulnérable... mais il est dans la zone safe du Foyer).
- [ ] Résultat : le joueur craft des objets à partir des ressources récoltées.

**Lot 3.3 — Placement de structures (`Complexité : C4`)**
- [ ] Système de placement sur grille iso (prévisualisation fantôme vert/rouge).
- [ ] Murs avec HP (les ennemis les attaquent la nuit).
- [ ] Pièges qui infligent des dégâts aux ennemis qui marchent dessus.
- [ ] Le Foyer comme ancrage central fixe autour duquel on construit. Son rayon de sécurité définit la zone constructible.
- [ ] Résultat : le joueur peut construire une base fonctionnelle.

**Lot 3.4 — Défenses actives (`Complexité : C4`)**
- [ ] Tourelle basique (auto-attaque sur les ennemis proches, consomme des ressources).
- [ ] Réparation des structures endommagées (coût en ressources réduit).
- [ ] Les ennemis ciblent intelligemment : murs d'abord si ils bloquent, ou contournement.
- [ ] Résultat : la nuit devient un vrai siège — les défenses travaillent avec le joueur.

**Lot 3.5 — Intégration et équilibrage (`Complexité : C3`)**
- [ ] Équilibrer le coût des structures vs la puissance des ennemis.
- [ ] S'assurer que la boucle récolte → craft → placement est fluide et pas tedious.
- [ ] Tester que le jeu est toujours fun AVEC la base (pas juste un obstacle entre les combats).
- [ ] Résultat : la base ajoute de la profondeur stratégique sans casser le rythme.

### Critère de validation
- [ ] Le joueur fait des choix stratégiques sur sa base ("je mets le mur ici pour canaliser les ennemis vers les pièges"). La base a un impact réel sur la survie. Construire est satisfaisant, pas une corvée.

---

## PHASE 4 — PERSONNAGES & META

> **Objectif :** Ajouter la rejouabilité — personnages multiples, méta-progression, raisons de relancer.
> **Livrable :** 3-4 personnages jouables avec des kits distincts, un Hub entre les runs, des déblocages permanents.

### Tâches

**Lot 4.1 — Système de personnages (`Complexité : C4`)**
- [ ] Refactorer le code joueur pour supporter des personnages avec des stats et perks différents.
- [ ] Implémenter 3 personnages : Le Vagabond (équilibré), La Forgeuse (craft/défense), Le Traqueur (distance/agilité).
- [ ] Chaque perso a un perk passif unique + pool de perks de level-up modifiée.
- [ ] Résultat : jouer La Forgeuse se sent différent de jouer Le Traqueur.

**Lot 4.2 — Le Hub (`Complexité : C3`)**
- [ ] Scène Hub entre les runs — espace onirique minimaliste (plateformes flottantes, vide blanc-bleuté, cf. Bible section 10).
- [ ] En placeholder : fond simple + UI fonctionnelle. L'aspect visuel évolutif (Hub qui grandit avec les Souvenirs) est repoussé à la Phase 7.
- [ ] Éléments fonctionnels : sélection de personnage (Miroirs), Établi (kits de départ), Chroniques (historique/scores), accès au Vide (lancer une run).
- [ ] Résultat : entre deux runs, le joueur passe par le Hub et choisit son perso.

**Lot 4.3 — Vestiges et déblocages (`Complexité : C3`)**
- [ ] Monnaie "Vestiges" gagnée proportionnellement au score.
- [ ] Arbre de déblocages basique : kits de départ (commencer avec une arme T2, +10 bois, etc.).
- [ ] Déblocage de personnages via accomplissements (survivre 3 nuits → La Forgeuse, etc.).
- [ ] Résultat : le joueur accumule des Vestiges et débloque des trucs entre les runs.

**Lot 4.4 — Pool de perks étendue (`Complexité : C4`)**
- [ ] Étendre la pool de perks à 25-30 (stats, combat, survie, Essence, rares).
- [ ] Implémenter les synergies/combos entre perks.
- [ ] Perks spécifiques par personnage (5-6 par perso).
- [ ] Résultat : chaque run produit un build différent. Les combos sont excitants.

**Lot 4.5 — Équilibrage méta (`Complexité : C3`)**
- [ ] Équilibrer les personnages (win rate, score moyen par perso).
- [ ] Équilibrer les perks (aucun perk ne doit être "always pick" ou "never pick").
- [ ] S'assurer que la méta-progression donne de la variété, pas de la puissance brute.
- [ ] Résultat : chaque personnage est viable, chaque run est différente.

### Critère de validation
- [ ] Le joueur dit "je veux essayer Le Traqueur maintenant" après avoir joué Le Vagabond. Les Vestiges donnent une raison de relancer même après une run ratée. Les perks créent des moments "oh ce combo est CASSÉ" qui sont satisfaisants.

---

## PHASE 5 — LE MONDE

> **Objectif :** Remplacer les placeholders par un vrai monde procédural intéressant à explorer.
> **Livrable :** Monde procédural avec biomes, POI, coffres, lore, fog of war. Chaque run est un monde unique.

### Tâches

**Lot 5.1 — Génération procédurale basique (`Complexité : C4`)**
- [ ] Remplacer la map fixe par une génération procédurale (Cellular Automata ou WFC simple).
- [ ] Foyer au centre, zones concentriques (proche/médiane/lointaine).
- [ ] Tiles variés : herbe, béton, eau (infranchissable), forêt dense.
- [ ] Seed reproductible (même seed = même monde).
- [ ] Résultat : chaque run a une map différente.

**Lot 5.2 — Biomes (`Complexité : C3`)**
- [ ] Implémenter 3 biomes pour l'EA : Forêt reconquise, Ruines urbaines, Marécages (cf. Bible section 5 pour palettes, ambiances, détails de décor).
- [ ] 4ème biome (Carrière effondrée) en bonus si la capacité le permet.
- [ ] Chaque biome a ses propres tiles, ambiance, types de ressources, et ennemis dominants.
- [ ] La map est composée de 2-3 biomes par run.
- [ ] Le Sanctuaire comme POI rare inter-biomes (lieu intact, plus saturé que le reste — cf. Bible section 5.5).
- [ ] Résultat : la variété visuelle et gameplay entre les zones est visible.

**Lot 5.3 — Points d'intérêt (POI) (`Complexité : C3`)**
- [ ] Système de placement de POI procédural dans les biomes.
- [ ] 5-6 POI types : bâtiment fouillable, cache de ressources, coffre gardé, ruine avec lore, NPC marchand, anomalie.
- [ ] Les POI sont des "scènes" hand-crafted placées procéduralement.
- [ ] Résultat : l'exploration a des objectifs concrets ("je vois un bâtiment là-bas").

**Lot 5.4 — Coffres et loot (`Complexité : C2`)**
- [ ] Implémenter les 4 types de coffres (commun, rare, épique, lore).
- [ ] Loot tables en JSON (facile à équilibrer).
- [ ] Coffres épiques gardés par des ennemis élites.
- [ ] Résultat : trouver un coffre est un moment de dopamine.

**Lot 5.5 — Fog of war et extension de map (`Complexité : C3`)**
- [ ] Fog of war : les zones non explorées ne sont pas "sombres" mais "pas encore réelles" — voile blanc-bleuté animé (cf. Bible section 7.1). Les tiles se matérialisent quand le joueur explore.
- [ ] Bords de map : les tiles se dégradent visuellement (couleurs qui fuient, formes floues) au lieu d'un mur invisible. La réalité s'arrête.
- [ ] À chaque aube, la map s'étend en périphérie (nouveaux chunks, nouveaux POI).
- [ ] Résultat : l'exploration est progressive et le monde a une limite organique.

**Lot 5.6 — Lore (`Complexité : C2`)**
- [ ] Implémenter les Souvenirs comme objets trouvables dans les coffres de lore et POI.
- [ ] Interface journal : fragments collectés, classés par constellation (L'Avant, Les Signes, L'Effacement, Les Créatures, Le Foyer, Le Joueur — cf. Bible section 3).
- [ ] 10-15 premiers fragments de lore écrits. Ton : humain, court (2-5 phrases), ambigu. Pas d'exposition directe.
- [ ] Les Souvenirs débloquent des recettes/perks dans la méta-progression (se souvenir = rendre possible).
- [ ] Résultat : le joueur qui explore trouve des indices sur l'histoire du monde.

### Critère de validation
- [ ] Chaque run se sent comme un nouveau monde. L'exploration est récompensante (coffres, lore, ressources). Le joueur a des décisions à prendre ("je vais explorer cette ruine loin ou je reste safe près de la base ?").

---

## PHASE 6 — POLISH GAMEPLAY

> **Objectif :** Transformer le prototype en quelque chose qui RESSEMBLE à un jeu.
> **Livrable :** Événements aléatoires, bestiaire complet, multiplicateurs de score, mutateurs, seed de défi fixe.

### Tâches

**Lot 6.1 — Bestiaire complet (`Complexité : C4`)**
- [ ] Implémenter tous les types d'ennemis du GDD : Rôdeurs, Charognards, Sentinelles, Tréants corrompus (jour) + Ombres, Brutes, Tisseuses, Hurleurs, Rampants (nuit).
- [ ] Mini-boss (Colosses) visuellement uniques par biome (cf. Bible section 6.2 : faits de ce qui a été effacé dans leur zone).
- [ ] Élites nocturnes (Aberrations) : versions corrompues avec excroissances, aura de particules sombres, propriétés aléatoires.
- [ ] L'Indicible (boss rare nuit 10+) : trop grand pour l'écran, son infrasonore.
- [ ] Tous les ennemis se désagrègent en particules noires à la mort (pas de cadavre — retour au néant).
- [ ] Résultat : la variété d'ennemis rend chaque nuit différente.

**Lot 6.2 — Événements aléatoires (`Complexité : C3`)**
- [ ] Implémenter 5 événements de jour (Caravane, Tempête, Tremblement, Signal de fumée, Migration).
- [ ] Implémenter 3 événements de nuit (Brume épaisse, Résurgence, L'Appel).
- [ ] Résultat : des surprises cassent la routine et forcent l'adaptation.

**Lot 6.3 — Score avancé et leaderboard (`Complexité : C3`)**
- [ ] Score complet : survie + combat + exploration + multiplicateurs (personnage, mutateurs).
- [ ] Leaderboard local complet (global, par personnage, par record de nuits).
- [ ] Seed de défi fixe (en local, même seed pour tous).
- [ ] Résultat : la compétition de score fonctionne.

**Lot 6.4 — Mutateurs de difficulté (`Complexité : C3`)**
- [ ] Implémenter 5-6 mutateurs dans le Hub (ennemis +HP, pas de Foyer safe, nuit plus longue, etc.).
- [ ] Chaque mutateur augmente le multiplicateur de score.
- [ ] Résultat : les joueurs avancés ont des défis supplémentaires.

### Critère de validation
- [ ] Le jeu a assez de variété pour que 10 runs d'affilée se sentent toutes différentes. Le score pousse à relancer. Les mutateurs donnent un challenge aux joueurs qui maîtrisent le jeu.

---

## PHASE 7 — ART & AUDIO

> **Objectif :** Remplacer tous les placeholders par de l'art et du son de qualité. C'est là que le graphic designer entre en jeu.
> **Livrable :** Le jeu est beau et a une identité visuelle forte conforme à la Bible Artistique.
> **Référence absolue pour cette phase :** Bible Artistique & Narrative (palettes, styles, specs techniques des assets, guide de production).

### Tâches

**Lot 7.1 — Art des tiles et environnements (`Complexité : C3`)**
- [ ] Graphic designer : tiles isométriques 128×64 pour chaque biome (3 biomes × 15-20 tiles). Chaque tile a un état "ancré" (normal) et un état "effiloché" (pour les bords de map).
- [ ] Deux palettes : vivante (jour — verts, dorés, blancs cassés) et corrompue (nuit — noirs vrais, orange Foyer, bleu-violet). Cf. Bible section 4.2.
- [ ] Intégrer les tiles dans la génération procédurale + transitions entre biomes.
- [ ] Éléments de décor narratifs (cf. Bible section 5 : jouet d'enfant, panneau routier avalé par un arbre, empreintes qui s'arrêtent net).
- [ ] Résultat : le monde est beau ET raconte une histoire.

**Lot 7.2 — Art des personnages et ennemis (`Complexité : C4`)**
- [ ] Sprites des 3-4 personnages (64×64 bounding box, 4 directions, idle/walk/dash/hurt/death — cf. Bible section 11.2 pour specs complètes). Silhouettes distinctes obligatoires (Vagabond ≠ Forgeuse ≠ Traqueur).
- [ ] Sprites de tous les ennemis — créatures asymétriques, mêlant organique et minéral, yeux vert-acide. Pas de zombies, pas d'animaux. Cf. Bible section 6.2.
- [ ] Effet de désintégration à la mort (particules noires iridescentes, pas de cadavre).
- [ ] Effets visuels de combat (projectiles, impacts, orbes XP dorées).
- [ ] Résultat : le combat est lisible et les créatures sont dérangeantes — belles et fausses.

**Lot 7.3 — Art du UI et du Hub (`Complexité : C3`)**
- [ ] HUD semi-diégétique in-game (textures parchemin usé, bordures métal oxydé, polices pochoir — cf. Bible section 9).
- [ ] Palette UI : fond noir 85%, texte blanc cassé, accent or pâle, accents danger/positif/Essence/rareté.
- [ ] Écran de level-up / choix de perks. Écran de mort / score (compteur mécanique).
- [ ] Hub : Arbre de Souvenirs (évolutif), Miroirs de sélection personnage, Obélisque, Chroniques, le Vide.
- [ ] Hub évolutif : visuellement vide au début, s'enrichit avec les Souvenirs retrouvés (cf. Bible section 10).
- [ ] Résultat : l'UI est propre, lisible, et appartient au monde.

**Lot 7.4 — Audio (`Complexité : C3`)**
- [ ] Musique : 5-6 tracks minimum. Pas d'orchestre, pas d'épique — intime et étrange (piano désaccordé, guitare avec delay, percussions d'objets du quotidien). Cf. Bible section 8 pour les références (Ólafur Arnalds, Disasterpeace, Ben Frost).
- [ ] Tracks : jour exploration (ambient), jour combat (densification), crépuscule (distorsion), nuit (percussif croissant), aube (release — une note de piano), Hub (onirique).
- [ ] Le silence est un outil — pas de fond sonore permanent.
- [ ] Sound design : créatures qui émettent des sons du quotidien déformés (pas de rugissements classiques). Foyer = crépitement doux tonal. Craft = son "complet" satisfaisant. Level up = cristallin ascendant.
- [ ] Musique adaptative basique (intensité liée au nombre d'ennemis, battement de cœur à HP bas).
- [ ] Résultat : le son EST l'atmosphère. La nuit fait peur, l'aube soulage, le Hub flotte.

> **Note :** Cette phase peut être menée en parallèle de la production gameplay. L'objectif est de brancher progressivement les assets validés, sans bloquer le développement des systèmes.

### Critère de validation
- [ ] Quelqu'un qui voit le jeu pour la première fois dit "c'est beau" et "ça a du style". Le contraste jour/nuit est saisissant. Le son renforce la tension nuit et la mélancolie jour. Le monde est beau ET dangereux — pas marron-gris-déprimant.

---

## PHASE 8 — PRÉPARATION EARLY ACCESS

> **Objectif :** Le jeu est prêt à être vendu.
> **Livrable :** Build stable, page Steam, trailer, lancement EA.

### Tâches

**Lot 8.1 — Contenu final EA (`Complexité : C3`)**
- [ ] Vérifier le scope EA : 3 biomes, 3-4 personnages, 6-8 types d'ennemis, 30 perks, 15 Souvenirs, 5 événements.
- [ ] Écrire les 15 fragments de lore pour l'EA (répartis sur les 6 constellations, cf. Bible section 3. Ton et exemples définis dans la Bible).
- [ ] Ajouter le 4ème ou 5ème personnage si la capacité le permet.
- [ ] Résultat : le contenu EA est complet.

**Lot 8.2 — Qualité et stabilité (`Complexité : C4`)**
- [ ] Bug fixing intensif.
- [ ] Test de performance (60 FPS sur mid-range).
- [ ] Test sur Mac (build export).
- [ ] Accessibilité : remapping, taille de texte, screen shake toggle.
- [ ] Résultat : le jeu ne crash pas et tourne bien.

**Lot 8.3 — Steam et distribution (`Complexité : C2`)**
- [ ] Créer la page Steam (Steamworks).
- [ ] Screenshots de qualité (5-10) : capturer le contraste jour/nuit, la beauté du monde, le chaos nocturne.
- [ ] Description de la page : pitcher l'angle unique ("un monde en train d'être oublié" ≠ post-apo classique). Tags, catégories.
- [ ] Système de feedback joueur intégré (bouton in-game "donner un avis").
- [ ] Résultat : la page Steam est en ligne et donne envie.

**Lot 8.4 — Trailer et marketing (`Complexité : C2`)**
- [ ] Trailer gameplay de 60-90 secondes.
- [ ] GIFs pour les réseaux sociaux.
- [ ] Post d'annonce sur les communautés pertinentes (Reddit r/roguelikes, r/survivalgames, r/indiegaming, Steam forums, Discord indie dev).
- [ ] Résultat : le jeu a du matériel de communication.

**Lot 8.5 — Lancement 🚀 (`Complexité : C3`)**
- [ ] Build finale testée.
- [ ] Upload sur Steam.
- [ ] Prix EA fixé (12-15€).
- [ ] Lancement + monitoring des premiers retours.
- [ ] Résultat : VESTIGES est en Early Access sur Steam.

### Critère de validation
- [ ] Des joueurs inconnus achètent le jeu, y jouent, et laissent des avis. Idéalement "Mostly Positive" minimum.

---

## APRÈS L'EARLY ACCESS

La roadmap post-EA dépend des retours joueurs, mais les grandes lignes :

- [ ] **Axe A — Stabilisation (`Complexité : C3`) :** Bug fixes, équilibrage basé sur les données, QoL demandées par la communauté.
- [ ] **Axe B — Contenu additionnel (`Complexité : C4`) :** 2 nouveaux personnages, 2 nouveaux biomes, nouveaux ennemis et perks.
- [ ] **Axe C — Compétition & social (`Complexité : C4`) :** Leaderboard en ligne (serveur), seed de défi automatisée, social features.
- [ ] **Axe D — Progression vers v1.0 (`Complexité : C4`) :** contenu complet (6-8 persos, 5+ biomes, 50+ perks, 30+ Souvenirs, lore complet), localisation additionnelle, polish final, sortie de l'Early Access.
- [ ] **Axe E — v2+ (`Complexité : C4`) :** Coop 2 joueurs, DLC, Steam Workshop, console.

---

## VUE COMPLEXITÉ (APERÇU)

| Phase | Complexité dominante | Risque principal |
|-------|-----------------------|------------------|
| Phase 0 | C2-C3 | Prise en main moteur / architecture initiale |
| Phase 1 | C2-C3 | Qualité du game feel combat |
| Phase 2 | C2-C3 | Lisibilité des transitions jour/nuit |
| Phase 3 | C3-C4 | Intégration combat + base + IA |
| Phase 4 | C3-C4 | Équilibrage méta et diversité des builds |
| Phase 5 | C3-C4 | Qualité de génération procédurale |
| Phase 6 | C3-C4 | Variété réelle sur plusieurs runs |
| Phase 7 | C3-C4 | Cohérence visuelle/audio sans bloquer le dev |
| Phase 8 | C2-C4 | Stabilité build + readiness commerciale |

---

## RAPPELS IMPORTANTS

- [ ] **Si la Phase 1 n'est pas fun → STOP.** Itérer sur le combat et le feel avant de continuer. C'est la fondation de tout le jeu. Un beau monde avec un combat moyen = un jeu moyen.
- [ ] **Tester tôt, tester souvent.** À chaque fin de phase, faire tester par 2-3 personnes extérieures. Leurs retours valent plus que 10h de réflexion solo.
- [ ] **Le scope EA est volontairement réduit.** 3 biomes, 4 personnages, c'est suffisant pour valider le marché. Le contenu se rajoute en EA — c'est littéralement le principe.
- [ ] **L'art peut être parallélisé.** Dès que le graphic designer est embarqué (Phase 4-5), lui donner la Bible Artistique & Narrative comme brief. Il peut commencer à produire les sprites pendant que le dev continue sur le code. Les priorités de production sont définies dans la Bible section 11.1.
- [ ] **À chaque revue de progression :** relire les lots clôturés, noter les blocages et ajuster les prochains lots selon la complexité réelle observée.

---

> **Ce document est vivant.** Les niveaux de complexité VONT changer. Ce qui compte, c'est l'ordre des phases, les critères de validation, et la maîtrise du risque technique.

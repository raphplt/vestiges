# VESTIGES — Game Design Document

> **Version :** 1.3
> **Dernière mise à jour :** 21 février 2026
> **Auteur :** Raphaël
> **Statut :** Document fondateur — vision complète
> **Docs liés :** Bible Artistique & Narrative, Architecture Technique, Roadmap & Milestones

---

## 1. VISION

### Le pitch

Un roguelike de survie et construction en vue isométrique 2D, dans un monde qui est en train d'**être oublié**. La réalité s'efface. Des créatures naissent du vide laissé par l'oubli. Le joueur maintient le monde en existence par la lumière de son Foyer — un ancrage de mémoire — et explore, construit, combat, jusqu'à ce que la nuit finisse par le submerger. La question n'est pas "est-ce que tu vas mourir" mais "combien de nuits tu vas tenir".

### L'émotion cible

Le joueur doit ressentir un cycle émotionnel à chaque run :

1. **Émerveillement et curiosité** — en découvrant un nouveau monde, en sentant la montée en puissance dès les premières secondes.
2. **Flow et puissance** — quand le build se construit, que les perks s'empilent, que le personnage devient une machine.
3. **Adrénaline et tension** — quand la nuit tombe, que la base tremble, que les vagues deviennent écrasantes.
4. **Fierté et frustration productive** — "j'ai tenu 7 nuits, je SAIS que je peux faire 8 avec ce perso et ce build".

### Le "just one more run"

Le joueur relance parce que :
- Il veut battre son record (nuits survivées, score).
- Il a débloqué un nouveau personnage et veut tester son kit.
- Il a trouvé un combo de perks brisé et veut le reproduire.
- Il a vu un pote le dépasser au leaderboard.
- Il a aperçu un fragment de lore qu'il n'a pas eu le temps de lire.
- Il pense avoir compris un meilleur build order.

### Références clés

| Jeu | Ce qu'on prend |
|-----|---------------|
| **Megabonk** | Boucle addictive, montée en puissance in-run, personnages multiples, système de niveaux/perks, coffres, "just one more run", score compétitif |
| **Hades** | Lisibilité, fluidité du combat, méta-progression entre les runs |
| **Project Zomboid** | Survie sans fin possible — la mort est inévitable. Construction de base, gestion des ressources |
| **Hollow Knight** | Un monde "mort" rendu visuellement vivant et poétique. Narration environnementale. |
| **Stalker / Metro** | Atmosphère, sentiment d'isolement, beauté mélancolique des lieux abandonnés |
| **The Last of Us** | Nature vs ruines, mélancolie, narration environnementale |
| **Don't Starve** | Cycle jour/nuit comme mécanique centrale, survie stylisée, scaling organique de la difficulté |
| **Vampire Survivors** | Montée en puissance dopaminergique, auto-attaque, simplicité de la boucle, rejouabilité infinie |
| **Gris** | Usage de la couleur comme mécanique narrative. La beauté au service de l'émotion. |

---

## 2. INFORMATIONS PROJET

| Élément | Détail |
|---------|--------|
| **Nom** | VESTIGES |
| **Genre** | Roguelike / Survie / Construction |
| **Moteur** | Godot 4 |
| **Langage** | C# |
| **Vue** | Isométrique 2D |
| **Plateforme** | PC / Mac (Steam) |
| **Modèle** | Early Access → Release complète |
| **Multijoueur** | Non en v1 (architecture pensée pour v2 coop) |
| **Équipe** | 1 développeur + 1 graphic designer |
| **Rythme** | ~10h/semaine, side project |

---

## 3. CORE LOOP

### Philosophie fondamentale

**La mort est inévitable. La question c'est "combien de nuits ?".**

Il n'y a pas de "fin" au jeu. Pas de boss final qui conclut la run. Le monde scale indéfiniment — chaque nuit est plus dure que la précédente. Le joueur finit toujours par être submergé. C'est ce qui crée la rejouabilité infinie : il y a TOUJOURS un record à battre, un nouveau build à essayer, un score à dépasser.

**La montée en puissance est la drogue.**

Inspiré de Megabonk : le joueur commence faible et en quelques minutes devient puissant. Level-ups in-run, choix de perks, drops de coffres, combos qui se construisent. Le joueur ne grind pas — il MONTE EN PUISSANCE en temps réel. Chaque décision le rend plus fort, et cette sensation de puissance croissante est ce qui rend la boucle addictive.

**L'univers justifie la mécanique.**

Ce n'est pas un monde détruit par une bombe ou un virus — c'est un monde en train d'**être oublié**. La réalité elle-même se défait. Le Foyer n'est pas un simple feu de camp : c'est un ancrage de mémoire qui maintient le monde en existence. Les créatures ne sont pas des monstres : elles sont ce qui **pousse dans le vide** laissé par l'oubli. Et retrouver des Souvenirs rend le joueur plus fort parce que se souvenir, c'est littéralement renforcer la réalité. Chaque mécanique a une raison d'être dans l'univers.

> **Détails complets de la cosmologie :** voir Bible Artistique & Narrative, sections 1-3.

### Vue d'ensemble

```
┌──────────────────────────────────────────────────────────┐
│                     UNE RUN                               │
│                                                           │
│  JOUR 1 ──► NUIT 1 ──► JOUR 2 ──► NUIT 2 ──► ... ──► ☠ │
│                                                           │
│  Explorer     Défendre    Explorer    Défendre     MORT   │
│  Récolter     Survivre    Récolter    Survivre     SCORE  │
│  Level up     Gagner XP   Level up    Gagner XP           │
│  Crafter      Loot        Crafter     Loot                │
│                                                           │
│  Difficulté constante ──────────────────► Scaling infini  │
│  Puissance joueur ──────────────────────► Scaling aussi   │
│                                                           │
│  Score final = f(nuits, kills, exploration, bonus)        │
└──────────────────────────────────────────────────────────┘
```

### Le Jour (≈ 8-10 minutes réelles par jour)

**Objectif du joueur :** Monter en puissance et préparer la nuit à venir.

**Ce qui se passe :**
- Le joueur explore un monde procédural composé de biomes et de ruines.
- **Des créatures sont présentes en PERMANENCE hors de la base.** Le jour n'est jamais safe. Dès que le joueur quitte le rayon de sécurité du Foyer, il est en combat. C'est un Vampire Survivors à ciel ouvert.
- L'auto-attaque génère un flux constant d'XP et de loot → le joueur monte en puissance en temps réel.
- Il récolte des ressources (bois, pierre, métal) en interagissant avec l'environnement entre les combats.
- Il ouvre des coffres contenant des objets, perks, et ressources rares.
- Il level up et choisit des perks/améliorations de stats.
- Il retourne à la base (zone safe) pour crafter et construire des défenses.
- Il découvre optionnellement des fragments de lore dans les ruines.
- Des événements aléatoires surgissent (caravane, tempête, cri lointain).

**La base = zone safe.** Le Foyer est un **ancrage de mémoire** — sa lumière n'est pas thermique, elle est ontologique. Elle rappelle au monde qu'il existe, et les créatures (incarnations de l'oubli) ne peuvent pas supporter sa présence. C'est le seul endroit où le joueur peut crafter, construire, et souffler. Ça crée une boucle micro : sortir → farmer → rentrer crafter → ressortir.

**Timer visible :** Une barre de progression "soleil" constamment visible. Le joueur sait EXACTEMENT combien de temps il lui reste avant la nuit. La durée du jour reste constante — c'est la difficulté de la nuit qui scale, pas le temps disponible.

**Différence jour vs nuit :**
- **Jour :** Le joueur VA VERS les ennemis. Il choisit où aller, quels risques prendre, quelle direction explorer. Les ennemis sont dispersés dans le monde.
- **Nuit :** Les ennemis VIENNENT AU JOUEUR. Ils convergent vers le Foyer. Le joueur défend une position fixe.

**Règle de design critique :** Le jour ne doit JAMAIS être une corvée. L'auto-attaque + ennemis permanents garantissent que chaque seconde passée dehors est du gameplay actif — combat, loot, level up, exploration.

### Le Crépuscule (≈ 1-2 minutes réelles)

**Objectif du joueur :** Derniers préparatifs, choix tactiques de dernière seconde.

**Ce qui se passe :**
- Le monde commence à perdre sa cohérence — les couleurs se délavent, les verts deviennent bleus, les ombres s'allongent et prennent de la substance. La brume d'effacement monte depuis le sol.
- La musique change — montée de tension progressive, dissonances.
- Les créatures diurnes fuient ou deviennent agressives.
- Dernière chance de poser des pièges, allumer des feux, se repositionner.
- Le joueur doit choisir : rester dehors pour un dernier loot risqué, ou se replier dans sa base.

### La Nuit (≈ 5-8 minutes réelles, scaling)

**Objectif du joueur :** Survivre jusqu'à l'aube.

**Ce qui se passe :**
- Le monde "s'endort" — la mémoire collective faiblit. La nuit n'a **pas de ciel** : pas de lune, pas d'étoiles, juste le vide au-dessus. Seules les sources de lumière existent.
- Les créatures — incarnations du néant qui comble le vide de l'oubli — convergent en vagues vers le Foyer.
- Les vagues sont de plus en plus intenses et variées.
- Le joueur utilise ses craft (armes, pièges, tourelles, murs) pour défendre.
- Tuer des créatures = XP + drops d'Essence + parfois du loot. Les créatures ne laissent pas de cadavre : elles se désagrègent en particules sombres.
- La base subit des dégâts — réparations en temps réel entre les vagues.
- Des pauses courtes entre les vagues pour souffler et réorganiser.

**Mécanique lumière/discrétion :** Le Foyer est un ancrage de mémoire — sa lumière maintient la réalité et repousse les créatures à courte portée, mais les attire de loin (elles cherchent à **consommer** la mémoire). Le joueur peut éteindre certaines sources de lumière pour réduire l'afflux, mais s'expose à des attaques surprises dans le noir — et dans VESTIGES, le noir n'est pas juste l'absence de lumière, c'est l'absence de *réalité*.

### L'Aube (transition)

**Si le joueur survit :**
- Le monde "se réveille" — un sursaut de mémoire. Les créatures reculent et se désagrègent dans la lumière naissante.
- Moment de respiration — la musique s'adoucit. Les couleurs reviennent progressivement.
- Résumé de la nuit (score partiel, dégâts subis, créatures tuées).
- **La map se renouvelle partiellement** — les zones explorées restent, mais de nouvelles zones apparaissent en périphérie (nouveaux biomes, nouvelles ruines). La base et les constructions persistent.
- Mais chaque cycle use un peu la réalité. La difficulté de la prochaine nuit augmente parce que le monde tient de moins en moins bien (plus de créatures, nouveaux types, vagues plus longues).
- Le cycle recommence — nouveau jour, nouvelles opportunités.

### Scaling de la difficulté

**Le scaling est organique, pas artificiel — il est ancré dans l'univers :**
- Le jour ne raccourcit PAS. Le joueur a toujours le même temps de préparation.
- Ce qui scale : le nombre de créatures nocturnes, leurs HP, leur variété, la durée des vagues, l'apparition de types élites et mini-boss.
- **Justification narrative :** chaque cycle jour/nuit use un peu plus la réalité. Le monde se souvient de moins en moins bien de lui-même. Les fissures s'élargissent. Plus de néant s'infiltre. Plus de créatures en émergent.
- Le scaling est infini mais suit une courbe logarithmique douce — pas de spike brutal, mais une pression qui monte inexorablement.
- Le joueur scale aussi (perks, gear, base fortifiée) — le jeu est dans l'écart entre sa puissance et celle des ennemis. Cet écart se réduit lentement jusqu'à ce que les ennemis submergent.

**Paliers de difficulté (approximatifs) :**

| Nuit | Ce qui change |
|------|--------------|
| 1-2 | Introduction douce. Ombres et créatures basiques. Le joueur apprend. |
| 3-4 | Nouveaux types d'ennemis. Premières combinaisons dangereuses. |
| 5-6 | Mini-boss occasionnels. Les vagues deviennent longues. La base est mise à l'épreuve. |
| 7-9 | Élites réguliers. Le joueur doit avoir un build solide pour tenir. |
| 10+ | Territoire inconnu. Scaling libre. Seuls les meilleurs builds et les meilleurs joueurs dépassent ce seuil. Chaque nuit supplémentaire est un exploit. |

### La Mort

**La mort est permanente et définitive pour la run.**

Le joueur ne "meurt" pas au sens classique. Sa conscience **se décroche** de cette instance de réalité et se retrouve dans le Hub — un espace entre les mondes, la conscience nue du joueur. Visuellement : le monde autour du joueur se décompose, les tiles se désagrègent, les couleurs fuient vers le blanc. La dernière chose qu'on voit est le Foyer qui s'éteint. Puis le Hub.

**Ce que le joueur perd :**
- Le monde entier (map, base, inventaire, level, perks, tout). Ce fragment de réalité est définitivement effacé.
- La run est terminée.

**Ce que le joueur gagne :**
- Un **score final** calculé et enregistré.
- Des **Vestiges** (monnaie méta) proportionnels à la performance.
- Des **Souvenirs** éventuellement trouvés pendant la run (lore + déblocages permanents). Les Souvenirs voyagent avec la conscience du joueur — ce qu'il apprend dans un monde, il le sait dans le suivant.
- Son propre skill de joueur (connaissance des patterns, stratégies de build).

**Écran de mort :**
- Score final affiché avec détail des bonus.
- Comparaison avec le record personnel.
- Position dans le leaderboard (amis + global).
- Bouton "Relancer" IMMÉDIAT — zéro friction entre la mort et la prochaine run.

---

## 3.5 SYSTÈME DE SCORE

### Philosophie

Le score est la colonne vertébrale de la rejouabilité. C'est ce qui transforme "j'ai perdu" en "j'ai fait 47 832 points, je peux faire mieux". C'est aussi ce qui crée la compétition entre joueurs.

### Calcul du score

**Score = Score de survie + Score de combat + Score d'exploration + Multiplicateurs**

**Score de survie :**
- Points par nuit survivée (scaling exponentiel : nuit 1 = 100, nuit 2 = 250, nuit 5 = 1500, nuit 10 = 8000...).
- Bonus "base intacte" si la base a encore >75% HP à l'aube.
- Bonus "sans dégât" pour chaque nuit sans prendre de dégât personnel (très rare, très récompensé).

**Score de combat :**
- Points par créature tuée (scalés par type et difficulté).
- Bonus combos (kills rapides enchaînés).
- Bonus "multi-kill" (5+ kills en une action : piège, bombe, AoE).
- Points bonus pour les mini-boss et élites.

**Score d'exploration :**
- Points par zone découverte.
- Points par coffre ouvert.
- Points par POI exploré.
- Points par fragment de lore trouvé.

**Multiplicateurs :**
- **Multiplicateur de personnage :** Certains persos ont un multiplicateur de score différent (un perso facile = x0.8, un perso difficile = x1.3).
- **Multiplicateur de mutateurs :** Activer des mutateurs de difficulté dans le Hub augmente le multiplicateur de score.
- **Multiplicateur de streak :** Bonus si le joueur enchaîne les runs sans mourir avant la nuit 3 (récompense la régularité).

### Leaderboards

**Leaderboards multiples :**
- **Global** — Tous les joueurs, tous les personnages. Le classement principal.
- **Par personnage** — Meilleur score avec chaque personnage. Encourage à tous les essayer.
- **Amis** — Classement entre amis Steam. Le plus addictif.
- **Hebdomadaire** — Reset chaque semaine avec une seed imposée. Conditions égales pour tous.
- **Nuits survivées** — Classement pur endurance, sans considération de score.

**Seed hebdomadaire :**
- Chaque semaine, une seed mondiale est partagée par tous les joueurs.
- Même monde, mêmes conditions, même point de départ.
- Leaderboard dédié — compétition pure sur le skill et les choix.
- Crée un événement communautaire récurrent.

**Anti-triche :**
- Validation côté serveur des scores (hash de la run).
- Détection d'anomalies statistiques.
- Leaderboard modéré.

### Affichage in-game

- Le score courant est visible en permanence (petit compteur en haut à droite, discret mais accessible).
- À chaque kill, +points affiché en popup (comme Megabonk).
- À chaque aube, résumé du score de la nuit avec détail des bonus.
- Le record personnel est affiché comme objectif à battre.

---

## 4. SYSTÈMES DE GAMEPLAY

### 4.1 Personnages

**Philosophie :** Comme Megabonk, chaque personnage offre une expérience de jeu radicalement différente. Changer de perso = changer de stratégie = rejouabilité massive.

**Personnage de départ :** Le Vagabond — équilibré, polyvalent, parfait pour apprendre.

**Personnages débloquables (via la méta-progression) :**

| Personnage | Archétype | Spécificité | Déblocage |
|-----------|-----------|-------------|-----------|
| **Le Vagabond** | Équilibré | Pas de faiblesse, pas de force majeure. Bonus de récolte. | Disponible de base |
| **La Forgeuse** | Craft/Défense | Craft plus rapide, structures plus solides, dégâts corps à corps réduits. | Survivre 3 nuits |
| **Le Traqueur** | Agilité/Distance | Esquive améliorée, dégâts distance bonus, HP réduits. | Tuer 200 créatures en une run |
| **L'Éveillée** | Magie/Essence | Capacités d'Essence puissantes, régénération d'Essence, HP très bas. | Trouver 10 Souvenirs |
| **Le Colosse** | Tank/Corps à corps | HP massifs, dégâts CaC bonus, lent, ne peut pas esquiver. | Survivre 5 nuits |
| **L'Ombre** | Furtivité/Risque | Invisible la nuit si immobile, dégâts critiques x3, meurt en 2 coups. | Survivre une nuit sans prendre de dégât |
| **???** | ??? | Personnage secret. Lié au lore profond. | Condition cachée |

**Chaque personnage a :**
- Des **stats de base** différentes (HP, vitesse, dégâts, vitesse de craft, capacité d'inventaire).
- Un **perk unique passif** actif dès le début de la run.
- Un **arbre de perks in-run** spécifique (les choix de level-up diffèrent selon le perso).
- Un **multiplicateur de score** (perso difficile = meilleur multiplicateur).
- Une **silhouette distincte** immédiatement reconnaissable (cf. Bible Artistique, section 6.1).
- Un **skin de base** + cosmétiques débloquables.

### 4.2 Progression In-Run (Level Up)

**Philosophie :** C'est le cœur du système Megabonk adapté à VESTIGES. Le joueur doit sentir qu'il monte en puissance CONSTAMMENT. Chaque minute il est plus fort qu'avant.

**Gain d'XP :**
- Tuer des créatures (source principale).
- Ouvrir des coffres.
- Explorer de nouvelles zones.
- Compléter des événements aléatoires.
- Survivre une nuit (bonus XP massif).

**Level up :**
- À chaque level up, le joueur choisit 1 perk parmi 3 proposés (tirés de la pool du personnage + pool commune).
- Le jeu se "pause" brièvement (1-2 secondes) pour le choix — ou le joueur peut différer le choix.
- Les perks se combinent et se synergisent → émergence de "builds" uniques.
- Le level up est rapide au début (dopamine immédiate) et ralentit progressivement.

**Types de perks :**

**Perks de stats :**
- +HP, +Dégâts, +Vitesse, +Armure, +Vitesse de craft.
- Simples mais efficaces. Le "pain quotidien" des level-ups.

**Perks de combat :**
- "Écho" — Chaque 5ème attaque frappe deux fois.
- "Vampirisme" — Soigne 2% des dégâts infligés.
- "Berserker" — +30% dégâts quand <30% HP.
- "Barrage" — Les projectiles percent un ennemi supplémentaire.

**Perks de survie :**
- "Architecte" — Les murs ont +50% HP.
- "Récupérateur" — Les structures détruites rendent 75% des matériaux (au lieu de 50%).
- "Torche vivante" — Le joueur émet de la lumière (de la mémoire), renforçant la réalité autour de lui et repoussant les créatures proches.

**Perks d'Essence :**
- "Canalisation" — Les capacités d'Essence coûtent 20% de moins.
- "Siphon" — Les créatures tuées droppent 2x plus d'Essence.
- "Instabilité" — Les capacités d'Essence ont 30% de chance de ne rien coûter mais 10% de chance de backfire.

**Perks rares (apparaissent rarement, très puissants) :**
- "Deuxième souffle" — Revenir à 50% HP une fois après être tombé à 0 (une seule fois par run).
- "Maître du temps" — Le jour dure 20% plus longtemps.
- "Éveillé" — Voir les créatures nocturnes à travers les murs.

**Combos et synergies :**
- "Vampirisme" + "Berserker" = mode berserker viable (rester bas en HP, se soigner en tapant).
- "Architecte" + "Récupérateur" = base ultra résiliente et économique.
- "Siphon" + "Canalisation" = spam de capacités d'Essence.
- Les combos émergents sont la source principale du "just one more run" — le joueur veut retrouver CE combo.

### 4.3 Coffres & Loot

**Philosophie :** Les coffres sont des moments de dopamine. Trouver un coffre = excitation. L'ouvrir = récompense.

**Types de coffres :**
- **Coffre commun** (bois) — Ressources basiques, parfois un outil. Fréquent.
- **Coffre rare** (métal) — Arme ou outil de qualité, ressources rares. Souvent dans des zones dangereuses.
- **Coffre épique** (essence) — Perk bonus gratuit OU arme unique avec propriété spéciale. Rare, gardé par un mini-boss ou un piège.
- **Coffre de lore** (ancien) — Fragment de Souvenir + récompense. Visuel distinct, le joueur qui cherche le lore les repère.

**Drops de créatures :**
- Les créatures normales droppent des ressources basiques et de l'XP.
- Les élites droppent des ressources rares, de l'Essence, et parfois des objets.
- Les mini-boss droppent un coffre garanti.

### 4.4 Exploration

**Monde procédural :** Chaque run génère un nouveau monde composé de tuiles isométriques assemblées procéduralement. À chaque aube, la map s'étend en périphérie avec de nouvelles zones.

**Structure du monde :**
- **Le Foyer** (centre) — Le point de départ et de défense. Toujours au centre de la map.
- **Zone proche** (rayon 1) — Ressources basiques, dangers faibles, ruines simples. Accessible en quelques secondes.
- **Zone médiane** (rayon 2) — Ressources intermédiaires, dangers modérés, ruines complexes avec du lore.
- **Zone lointaine** (rayon 3+) — Ressources rares, dangers élevés, structures majeures, secrets. Aller-retour risqué par rapport au timer.

**Biomes :** Chaque monde contient 3-5 biomes. Chaque biome est un type de lieu que le monde "se souvient" encore. Plus le biome est loin du Foyer, plus la mémoire est faible, plus il est dégradé et étrange.

- **Forêt reconquise** — La nature a dévoré une zone suburbaine. Autoroutes fissurées sous des canopées géantes, maisons englouties par le lierre. Calme trompeur, oiseaux qui chantent. Abondance de bois et fibre. Ennemis : meutes de charognards, Tréants corrompus.
- **Ruines urbaines** — Centre-ville partiellement effondré. Intérieurs exposés comme des maisons de poupée. Silence oppressant, craquements lointains. Riche en métal et composants. Ennemis : Rôdeurs (humanoïdes déformés), Sentinelles.
- **Marécages** — Zone basse où la réalité est particulièrement fragile. L'eau est trouble, laiteuse, et absorbe la lumière. Brume permanente même de jour. Les reflets montrent des choses qui ne sont plus là. Riche en Essence et plantes rares. Ennemis : Tisseuses, Rampants.
- **Carrière effondrée** — Mine industrielle écroulée. Tunnels instables, machines rouillées figées en plein mouvement, veines de cristaux lumineux (Essence brute). Biome claustrophobe et vertical. Riche en pierre, métal, Essence. Ennemis : Brutes (armure de roche), Rôdeurs.
- **Champs sauvages** — Espaces ouverts, peu de couvert, visibilité maximale. Vulnérable mais riche en ressources dispersées. Le vent y est plus fort que partout ailleurs.
- **Sanctuaire** (POI rare) — Un lieu qui refuse d'être effacé. Bâtiment (église, bibliothèque, école) resté INTACT. Couleurs plus saturées, réalité plus "dense" qu'ailleurs. Gardien puissant, lore majeur, Souvenir garanti.

> **Détails complets des biomes (palette, ambiance, détails environnementaux) :** voir Bible Artistique & Narrative, section 5.

**Fog of war :** Le monde n'est pas "caché dans le brouillard" — il n'est "pas encore réel". Les zones non explorées sont un voile blanc-bleuté animé. Les tiles se matérialisent quand le joueur explore. La nuit, les zones non éclairées redeviennent incertaines.

**Points d'intérêt (POI) :** Chaque biome contient des POI procéduraux :
- Bâtiments fouillables (loot, lore, pièges).
- Caches de ressources.
- NPCs non-hostiles occasionnels (marchands, survivants avec quêtes courtes).
- Anomalies (événements spéciaux, portails vers des zones bonus).

### 4.5 Ressources & Récolte

**Ressources primaires :**

| Ressource | Source | Usage principal |
|-----------|--------|----------------|
| **Bois** | Arbres, débris | Construction, feux, outils basiques |
| **Pierre** | Rochers, ruines | Construction solide, armes contondantes |
| **Métal** | Ruines, épaves | Outils avancés, armes, pièges |
| **Fibre** | Plantes, tissus | Cordes, vêtements, bandages |
| **Nourriture** | Cueillette, loot, chasse | Soin, buffs temporaires |
| **Combustible** | Charbon, huile, résine | Feux, torches, explosifs |
| **Composants** | Ruines tech, épaves | Craft avancé, pièges mécaniques |
| **Essence** | Créatures nocturnes, anomalies, veines cristallines | Craft magique, capacités actives, améliorations du Foyer. Résidu de mémoire concentrée — manipuler l'Essence c'est manipuler la substance de la réalité. |

**Règles de récolte :**
- La récolte prend du temps (animation courte mais pas instantanée) → tension avec le timer.
- Certaines ressources nécessitent des outils (hache pour le bois, pioche pour la pierre).
- Les ressources rares sont souvent dans des zones dangereuses.
- Le joueur a un inventaire limité → choix constants sur quoi ramener.

### 4.6 Craft

**Philosophie :** Le craft est au cœur du gameplay. Chaque objet crafté doit avoir un impact perceptible sur la survie.

**Catégories de craft :**

**Outils**
- Hache, pioche, couteau — récolte plus rapide, dégâts bonus selon le type.
- Améliorables (bois → pierre → métal → essence).

**Armes (déterminent le type d'auto-attaque)**
- **Corps à corps :** Épée (arc large, rapide), Masse (lent, AoE), Lance (longue portée, linéaire).
- **Distance :** Arc (projectile rapide, mono-cible), Arbalète (lent, perçant), Lance-pierres (rapide, faible dégâts, multi-cible).
- **Spéciales :** Bâton d'Essence (orbes à tête chercheuse), Fouet (frappe circulaire) — débloqués via méta-progression.
- Chaque arme a un pattern d'auto-attaque distinct → changer d'arme change le feel du combat.
- Améliorables (bois → pierre → métal → essence).

**Défenses**
- **Murs** : Bois → Pierre → Métal → Renforcé. Chacun avec HP et résistances croissantes.
- **Pièges** : Piques, collets, mines, pièges à feu — à placer stratégiquement autour de la base.
- **Tourelles** : Artisanales (arbalète auto, lance-flammes) — consomment des ressources pour fonctionner.
- **Barricades** : Rapides à poser, faibles mais utiles en urgence.

**Survie**
- Bandages, potions de soin, antidotes.
- Nourriture trouvée (cueillette, loot) — buffs temporaires (vitesse, résistance, régénération).
- Torches, lanternes, feux de camp — sources de lumière mobiles ou fixes.

**Avancé (débloqué via méta-progression)**
- Objets infusés d'Essence — armes et outils avec propriétés spéciales.
- Améliorations du Foyer — portée de lumière, aura de soin, bouclier temporaire.
- Constructions avancées — murs enchantés, tourelles à essence, pièges dimensionnels.

**Interface de craft :**
- Menu accessible rapidement (pas de menu plein écran qui casse le rythme).
- Recettes organisées par catégorie avec filtre par ressources disponibles.
- Craft en temps réel — le joueur est vulnérable pendant qu'il craft (pas de pause).
- File d'attente de craft possible (craft 5 flèches d'un coup).

### 4.7 Construction

**Philosophie :** La base n'est pas un exercice de créativité libre (pas Minecraft). C'est un outil de survie tactique. Chaque mur, chaque piège est un choix stratégique.

**Le Foyer :**
- **Ancrage de mémoire** au centre de la map, indestructible mais améliorable. Ce n'est pas un feu — c'est un point où la conscience humaine est suffisamment concentrée pour maintenir la réalité en place. Sa "flamme" est orange-dorée mais ne brûle rien et ne produit pas de fumée.
- Source de lumière principale — sa lumière n'est pas thermique mais ontologique : elle rappelle au monde qu'il existe. Visuellement, le sol autour du Foyer est plus net, plus coloré, plus "réel" que le reste de la map.
- Point de respawn si le joueur meurt pendant le jour (avec pénalité de temps).
- Station de craft principale.
- Peut être amélioré pour : augmenter la portée de lumière (= rayon de réalité stable), soigner le joueur à proximité, stocker plus de ressources. Au niveau maximum, la flamme prend une nuance bleutée au cœur et des particules d'or flottent autour.

**Système de placement :**
- Grille isométrique — les structures se placent sur des cases.
- Prévisualisation avant placement (fantôme vert/rouge).
- Rotation possible.
- Certaines structures nécessitent un sol stable (pas de mur dans le marécage sans fondation).

**Dégâts et réparation :**
- Les structures ont des HP. Les créatures les attaquent.
- Les réparations coûtent des ressources (mais moins que la construction initiale).
- Les structures détruites laissent des débris récupérables (50% des ressources).

### 4.8 Combat

**Philosophie :** Le combat est en AUTO-ATTAQUE. Le joueur ne spamme pas de bouton — son personnage attaque automatiquement les ennemis à portée. Le skill du joueur réside dans le positionnement, le timing des capacités actives, et les choix de build (perks + gear).

**Inspirations directes :** Vampire Survivors (auto-attaque, montée en puissance), Megabonk (rythme, power fantasy).

**Auto-attaque :**
- Le personnage attaque automatiquement l'ennemi le plus proche à intervalle régulier.
- Le type d'attaque dépend de l'arme équipée (mêlée = frappe en arc, distance = projectile, magie = orbe).
- La vitesse d'attaque, les dégâts, la portée et le pattern sont améliorables via les perks et le gear.
- Les perks ajoutent des effets à l'auto-attaque (projectiles supplémentaires, ricochets, AoE, DoT, etc.).

**Capacités actives (input joueur) :**
- En plus de l'auto-attaque, le joueur a 2-3 slots de capacités actives qu'il déclenche manuellement.
- Capacités d'Essence (sort de feu, bouclier, pulse de lumière), consommables (bombe, potion), dash/esquive.
- Cooldowns courts — le joueur les utilise fréquemment mais doit choisir le bon moment.

**Contrôles :**
- Déplacement (ZQSD ou clic) — c'est l'input principal. Le positionnement EST le gameplay.
- Capacité active 1, 2, 3 (touches dédiées ou clic droit).
- Dash / esquive (Espace) — déplacement rapide avec courte invincibilité, cooldown.
- Pas de bouton d'attaque. L'attaque est automatique.

**Système de dégâts :**
- HP du joueur — pas de régénération naturelle, soin par craft/nourriture/perks uniquement.
- Armure — réduit les dégâts, se dégrade avec le temps.
- Statuts : Saignement, Poison, Brûlure, Gel, Terreur (réduit les stats).

**Feedback de combat :**
- Nombres de dégâts qui pop (comme Megabonk/VS).
- Flash de hit, particules d'impact.
- Score +points à chaque kill.
- Screen shake léger (toggle dans les settings).
- Le joueur VOIT sa puissance augmenter : les nombres grossissent, les effets sont plus spectaculaires, les ennemis meurent plus vite.

**Combat de jour vs de nuit :**
- **Jour :** Le joueur se déplace dans le monde, son auto-attaque nettoie les ennemis autour de lui. Il choisit sa trajectoire pour optimiser XP/loot/exploration. Gameplay offensif et mobile.
- **Nuit :** Les ennemis convergent vers la base. L'auto-attaque + les défenses (pièges, tourelles, murs) travaillent ensemble. Gameplay défensif et positionnel. Le joueur gère les brèches et utilise ses capacités actives aux moments critiques.

### 4.9 Bestiaire

**Principe de design :** Les créatures de VESTIGES ne sont PAS des animaux, des zombies, ou des démons. Elles sont de la **matière qui n'aurait pas dû exister** — ce qui pousse dans le vide laissé par l'oubli, comme de la mauvaise herbe dans une fissure mais à l'échelle de l'existence. Elles sont organiques mais FAUSSES : asymétriques, mêlant chair et matière minérale, faites du monde qu'elles remplacent. Elles ne saignent pas rouge — elles suintent un fluide noir iridescent. Elles ne meurent pas — elles se désagrègent en particules sombres et retournent au néant. Leurs yeux émettent une lumière vert-acide : la lumière inverse du Foyer.

> **Détails complets du design des créatures :** voir Bible Artistique & Narrative, section 6.2.

**Créatures diurnes (faibles à modérées) :**
- **Rôdeurs** — Ce qui pousse là où un humain a été effacé. Vaguement bipèdes, bras trop longs, visage qui est un amas de traits n'arrivant pas à former une expression. Lents, frappent fort.
- **Meute de charognards** — Comme des chiens mais avec trop de pattes et pas assez de tête. Rapides, nerveux, fuient si le chef (le plus gros, un œil de plus) est tué.
- **Sentinelle** — Pilier organique ancré dans les ruines, immobile. Des "yeux" s'ouvrent quand le joueur approche et projettent des projectiles verts. Comme un réverbère devenu hostile.
- **Tréant corrompu** — On croit que c'est un arbre. Puis il bouge. Branches = bras, racines = pieds, tronc avec des excroissances qui ressemblent à des visages. Lent, très résistant.

**Créatures nocturnes (modérées à létales) :**
- **Ombres** — Presque bidimensionnelles. Taches sombres qui glissent sur le sol, seuls les yeux vert-acide sont "solides". Fragiles mais terrifiantes en masse.
- **Brutes** — Amas de chair, de pierre et de métal fusionnés. On voit des morceaux de voiture, de béton, de meubles dans leur chair. Lentes mais dévastatrices — elles chargent les murs.
- **Tisseuses** — Arachnéennes, longues "pattes" filandreuses, corps central pulsant. Tissent des fils d'effacement qui immobilisent. Se déplacent sur les murs.
- **Hurleurs** — Tube de chair avec une ouverture béante en haut. Le "cri" appelle des renforts et la lumière verte de l'intérieur éclaire les environs. Priorité absolue à éliminer.
- **Rampants** — Invisibles en surface. Le joueur ne voit qu'une perturbation — les tiles tremblent, la terre se fissure. Surgissent à l'intérieur de la base en ignorant les murs.
- **Colosses** (mini-boss, nuit 3+) — Chaque Colosse est visuellement unique car fait de ce qui a été effacé dans sa zone. Un Colosse de forêt = amas d'arbres et d'animaux fusionnés. Un Colosse urbain = sculpture de béton, d'acier et de verre brisé. Patterns uniques, loot rare garanti.
- **Aberrations** (élites, nuit 7+) — Versions corrompues des types de base : excroissances supplémentaires, taille accrue, aura de particules sombres. Propriétés spéciales aléatoires.
- **L'Indicible** (boss rare, nuit 10+) — Trop grand pour l'écran. Le joueur ne le voit jamais en entier. Tentacules, yeux, formes changeantes. Son infrasonore mêlé à ce qui ressemble à une voix humaine. Vaincre = score massif.

> **Détails complets du design visuel des créatures :** voir Bible Artistique & Narrative, section 6.2.

**Scaling infini :**
- Chaque nuit successive augmente : le nombre de créatures, leur HP, leur variété, et la durée de la nuit.
- Les combinaisons d'ennemis deviennent plus dangereuses (Brutes + Tisseuses = cauchemar).
- À partir de la nuit 7+, des modificateurs aléatoires s'appliquent aux vagues (ennemis enragés, régénérants, explosifs à la mort).
- Le scaling n'a pas de cap — il continue indéfiniment. La question est juste : quand le joueur craque ?

### 4.10 Rythme d'une Run

**Durée cible par cycle jour/nuit :**

| Phase | Durée réelle | Notes |
|-------|-------------|-------|
| Jour | ~8-10 min | Constant. Le joueur a toujours le même temps de préparation. |
| Crépuscule | ~1-2 min | Transition tendue. |
| Nuit | ~5-8 min | La durée et l'intensité des vagues augmentent à chaque nuit. |

**Un cycle complet ≈ 15-20 minutes.**

**Run typique ≈ 30-90 minutes** selon le skill du joueur (2-5+ nuits survivées).

**Record runs ≈ 2-3 heures** pour les joueurs experts qui dépassent la nuit 10.

**Run ratée (mort nuit 1) ≈ 15-20 minutes.** Assez court pour relancer immédiatement.

### 4.11 Magie / Capacités Spéciales

**Source :** L'Essence — le résidu de mémoire concentrée, récoltée sur les créatures nocturnes (qui l'absorbent en consommant la réalité) et dans les anomalies (fissures dans le tissu du monde).

**Philosophie :** La magie n'est pas séparée du craft — c'est du craft avancé. Utiliser l'Essence, c'est manipuler la substance même de la mémoire pour renforcer la réalité autour de soi. Les capacités spéciales sont des recettes qui nécessitent des Souvenirs (méta-progression) pour être débloquées — parce que se souvenir d'une capacité c'est la rendre à nouveau possible.

**Types de capacités :**
- **Actives :** Sort de feu (AoE dégâts), Bouclier d'essence (absorption), Pulse de lumière (repousse les ennemis), Téléportation courte.
- **Passives :** Régénération lente, Vision nocturne, Récolte accélérée, Résistance au froid.
- **Infusions :** Appliquer de l'Essence à une arme/outil pour lui donner des propriétés temporaires (arme enflammée, pioche qui révèle les minerais).

**Limitation :** L'Essence est rare et volatile. Les capacités ont un coût en Essence à chaque utilisation. Le joueur doit choisir entre utiliser l'Essence pour le combat ou pour améliorer le Foyer / crafter des objets avancés.

### 4.12 Événements Aléatoires

**But :** Casser la routine, forcer l'adaptation, créer des histoires émergentes.

**Événements de jour :**
- **Caravane de passage** — Marchand temporaire avec des objets rares. Disparaît après 2 minutes.
- **Tempête** — Réduit la visibilité, éteint les feux extérieurs, réduit le timer de jour.
- **Tremblement** — Révèle une grotte cachée ou effondre un bâtiment (nouveau loot ou chemin bloqué).
- **Signal de fumée** — Un survivant en détresse. Le sauver donne une récompense ; l'ignorer, rien. Piège possible.
- **Migration** — Un troupeau de créatures traverse la zone. Éviter ou chasser pour du loot massif.
- **Éclipse** — Le jour se raccourcit drastiquement. Mini-nuit surprise.

**Événements de nuit :**
- **Brume épaisse** — Visibilité quasi nulle. Les créatures arrivent sans prévenir.
- **Horde silencieuse** — Pas de son. Les créatures se déplacent sans bruit.
- **Résurgence** — La réalité se fissure profondément. Les créatures sont renforcées mais droppent plus d'Essence.
- **Trêve** — Une vague est annulée. Moment de respiration... ou piège ?
- **L'Appel** — Une créature spéciale apparaît et tente d'attirer le joueur hors de sa base.

---

## 5. MÉTA-PROGRESSION

### Philosophie

La méta-progression doit créer deux boucles de rétention :
1. **Court terme :** "Je viens de débloquer un truc, je veux le tester." → relance immédiate.
2. **Long terme :** "Il me manque encore 3 personnages et 15 Souvenirs." → engagement sur des semaines.

La méta-progression ne doit JAMAIS rendre le jeu "facile". Elle doit donner de la VARIÉTÉ (nouveaux persos, nouvelles recettes, nouveaux outils), pas de la puissance brute.

### Le Hub

**Entre les runs**, la conscience du joueur se retrouve dans le Hub — un espace entre les mondes. Ce n'est pas un lieu physique : c'est l'intérieur de la conscience du joueur, un espace onirique qui **grandit et se peuple au fur et à mesure** que le joueur retrouve des Souvenirs.

**Évolution visuelle du Hub :**
- Au début (0 Souvenirs) : presque vide. Plateformes de pierre brute flottant dans un vide blanc-bleuté. Silence total. Le Foyer, minuscule, au centre.
- Au fur et à mesure : chaque Souvenir ajoute un élément. Un arbre pousse. Des lucioles apparaissent. Des sons émergent. Le Hub GRANDIT avec le joueur.
- En fin de progression : un jardin onirique complet. L'Arbre de Souvenirs est immense. Des fragments du monde flottent dans l'air. C'est la récompense silencieuse de la méta-progression.

**Fonctions du Hub :**
- **Arbre de Souvenirs** — Arbre lumineux dont les branches portent des orbes dorés. Consulter le lore et les déblocages.
- **Les Miroirs** — Miroirs ovales flottants pour la sélection de personnage. Les persos non débloqués sont des miroirs brisés.
- **L'Établi** — Préparer un "kit de départ" pour la prochaine run (limité par les Vestiges dépensés).
- **L'Obélisque** — Pierre noire gravée de symboles. Choisir des mutateurs de difficulté (augmente le multiplicateur de score). Plus on active de mutateurs, plus la pierre vibre.
- **Les Chroniques** — Mur de pierre où les résultats de chaque run s'inscrivent. Historique, scores, leaderboard.
- **Le Vide** — Le bord du Hub. Le joueur marche vers le vide et se laisse tomber. Transition vers le nouveau monde. Chaque run est un nouveau fragment de réalité en train d'être oublié.

### Les Souvenirs

**Les Souvenirs sont le puzzle narratif du jeu.**

- Ils se trouvent dans les ruines (journaux, graffitis, objets), sur les mini-boss, dans les coffres de lore, et via des événements spéciaux.
- Chaque Souvenir a deux faces :
  - **Narrative :** Un fragment de lore organisé en 6 constellations thématiques (L'Avant, Les Signes, L'Effacement, Les Créatures, Le Foyer, Le Joueur). Le joueur reconstitue le puzzle à son rythme, dans n'importe quel ordre.
  - **Gameplay :** Débloque une recette, un personnage, un cosmétique, ou un perk ajouté à la pool. **Justification narrative :** se souvenir d'un objet ou d'une capacité, c'est littéralement le rendre à nouveau possible dans le monde.

**Le lore est optionnel mais récompensé.** Le joueur qui ignore le lore a un jeu complet et fun. Le joueur qui le cherche a une couche de profondeur supplémentaire ET des déblocages gameplay.

> **Détails des 6 constellations et exemples de fragments :** voir Bible Artistique & Narrative, section 3.

**Exemples :**
- *"Journal d'un ingénieur — Page 12"* → Débloque la recette de la tourelle à arbalète.
- *"Souvenir : Le goût du pain"* → Débloque la recette du four (buff de nourriture amélioré).
- *"Fragment de mémoire : La flamme intérieure"* → Ajoute le perk "Pyromane" à la pool de level-up.
- *"Écho : Le dernier signal"* → Débloque le personnage secret ???.

### Les Vestiges (monnaie méta)

- Obtenus proportionnellement au score de la run.
- Dépensés dans le Hub pour : kits de départ, cosmétiques, mutateurs.
- Non perdus — accumulation permanente.
- Les kits de départ ne sont PAS des avantages massifs — juste un léger coup de pouce (commencer avec une hache en pierre au lieu de rien, ou 5 planches de bois). Ça accélère les premières secondes sans trivialiser la run.

### Déblocages permanents

**Arbres de déblocage :**

| Arbre | Ce qui se débloque | Source |
|-------|-------------------|--------|
| **Personnages** | Nouveaux personnages jouables | Accomplissements spécifiques |
| **Recettes** | Nouvelles recettes de craft disponibles en run | Souvenirs |
| **Pool de perks** | Nouveaux perks ajoutés aux choix de level-up | Souvenirs |
| **Cosmétiques** | Skins, effets visuels, emotes | Vestiges, accomplissements |
| **Mutateurs** | Options de difficulté modifiées (+ multiplicateur de score) | Nuits survivées (seuils) |
| **Lore** | Fragments de l'histoire du monde | Exploration, coffres de lore |

---

## 6. NARRATION & LORE

### Philosophie

**Le lore est un puzzle optionnel, pas une quête principale.**

Il n'y a pas de "fin" narrative. L'histoire du monde se reconstitue fragment par fragment, run après run, comme un puzzle dont on ne connaît pas l'image finale. L'ambiguïté est une feature, pas un bug — les fans reconstitueront les théories sur Reddit et Discord.

### La thèse fondatrice (structure sous-jacente)

**Le monde n'a pas été détruit. Il a été OUBLIÉ.**

L'Effacement n'est pas une bombe, un virus ou une guerre. La réalité elle-même a commencé à se défaire — comme un rêve qu'on oublie au réveil. Les lois physiques se sont distordues. Les souvenirs collectifs se sont évaporés. Les choses elles-mêmes ont cessé d'être. Ce qui reste — les ruines, la végétation, les fragments d'objets — ce sont les **vestiges** : les choses suffisamment ancrées dans la mémoire collective pour ne pas disparaître.

Cette thèse n'est JAMAIS expliquée au joueur. C'est la structure qui donne sa cohérence à tout, mais le joueur doit la déduire.

**Ce que ça implique :**
- La lumière = la mémoire. Le Foyer rappelle au monde qu'il existe.
- Les créatures = l'oubli incarné. Ce qui pousse pour combler le vide.
- Se souvenir (Souvenirs) = renforcer la réalité = devenir plus fort.
- La nuit = le monde qui "s'endort". La conscience collective faiblit.
- Chaque run = un fragment de réalité différent en train d'être oublié.

### Les trois états de la réalité (impact gameplay & visuel)

| État | Manifestation | Où |
|------|--------------|-----|
| **Ancré** | Le monde est stable, coloré, tangible. Gameplay normal. | Près du Foyer. En plein jour. |
| **Effiloché** | Les textures se dégradent, les couleurs se délavent. Zones plus dangereuses. | Zones éloignées. Crépuscule. |
| **Effacé** | Le néant. Pas du noir — de l'absence. Les créatures en émergent. | Bords de la map. Nuit sans lumière. |

**Implication gameplay :** Les bords de la map ne sont pas un mur invisible. Le monde se dégrade visuellement : les tiles perdent leurs couleurs, les textures deviennent floues, des glitchs apparaissent. Si le joueur va trop loin, la réalité cesse.

### Structure narrative — 6 constellations

Le lore est organisé en constellations thématiques (8-12 fragments chacune) que le joueur explore dans n'importe quel ordre :

- **L'Avant** — Ce que le monde était avant l'Effacement. Un monde qui ressemble au nôtre — presque. Des détails subtils montrent que ce n'est pas exactement notre réalité.
- **Les Signes** — Les premiers symptômes. Des choses objectives qui disparaissent. Un immeuble entier, un mot, des proches — sans que personne ne ressente de manque.
- **L'Effacement** — La catastrophe elle-même. Progressif, sur des semaines. D'abord les abstractions (lois, institutions), puis le concret (bâtiments, routes), puis les gens.
- **Les Créatures** — Des observations de survivants. Les créatures poussent là où les choses disparaissent. Elles essaient de copier des formes vivantes sans comprendre la vie.
- **Le Foyer** — Les Foyers ne sont pas construits, ils sont apparus. Aux endroits où la mémoire humaine était la plus concentrée. Certains survivants pensent qu'ils sont vivants.
- **Le Joueur** — Qui suis-je ? Fragments contradictoires et volontairement ambigus. Trois interprétations valides : survivant ordinaire, lié à l'Effacement, ou manifestation du Foyer lui-même. Le jeu ne tranche jamais.

### Méthode de narration

**Aucun dialogue explicatif. Aucun narrateur. Aucune cutscene.**

Le lore se découvre exclusivement par : journaux et notes trouvés dans les ruines, graffitis et inscriptions, objets significatifs avec descriptions évocatrices, l'environnement lui-même (la disposition des ruines raconte une histoire), et les Souvenirs dans le Hub (flashs de mémoire courts et ambigus).

**Ton d'écriture :** Humain, pas épique. Des gens réels face à l'incompréhensible. Chaque fragment fonctionne seul. 2-5 phrases maximum. Le joueur est en run — il ne va pas lire un roman.

### Thèmes narratifs

- **L'identité** — Peut-on exister sans mémoire ?
- **La mémoire** — Peut-on reconstruire une identité à partir de fragments ?
- **La lumière et l'obscurité** — Littérale (gameplay) et métaphorique (espoir vs désespoir).
- **La nature et la civilisation** — Le monde se porte-t-il mieux sans nous ?
- **Le cycle** — La répétition comme mécanique narrative ET gameplay.

> **Détails complets du lore, exemples de fragments, ton d'écriture :** voir Bible Artistique & Narrative, sections 1-3.

---

## 7. DIRECTION ARTISTIQUE

> **Détails complets :** voir Bible Artistique & Narrative, sections 4-7 et 9-11.

### Proposition unique

**VESTIGES ne ressemble PAS à un jeu post-apocalyptique classique.** Pas de palette marron-gris-déprimante. Le monde est **beau** — étrangement, tragiquement beau. La nature a gagné, et elle est magnifique. Le contraste émotionnel central : **le monde le plus beau que tu aies vu va essayer de te tuer cette nuit.**

### Style visuel

- **Vue isométrique 2D** avec sprites qui ont un rendu "peinture numérique" — des formes lisibles avec des textures riches.
- **Références :** Hades (lisibilité), Darkest Dungeon (atmosphère), Hollow Knight (monde mort mais poétique), Gris (couleur comme narration).
- **Règle :** Chaque entité est identifiable par sa silhouette seule, même à petite taille.

### Les deux palettes

Le jeu a deux identités visuelles qui coexistent et se battent :

**La Palette Vivante (le jour, la mémoire) :**
- Bleus profonds du ciel. Verts intenses de la mousse et du lierre. Blancs cassés de la pierre ancienne.
- Touches vives : fleurs sauvages (violet, jaune), rouille orange, cuivre oxydé turquoise.
- Lumière diffuse dorée, éternel début d'automne.

**La Palette Corrompue (la nuit, l'oubli) :**
- Noir VRAI d'encre — l'absence de réalité, pas juste l'absence de lumière.
- Orange brûlant du Foyer et des torches — seule couleur chaude, violente dans l'obscurité.
- Bleu-violet de la brume d'effacement.
- Vert-acide de la bioluminescence des créatures — lumière inverse du Foyer.
- **Pas de lune. Pas d'étoiles.** La nuit de VESTIGES n'a pas de ciel — juste du vide.

**Le crépuscule :** Les deux palettes se mélangent. Les verts deviennent bleus. Les ombres prennent de la substance. L'orange du Foyer perce alors que tout s'éteint.

### Effets visuels clés

- **Bords de map :** Les tiles se décomposent — pixels qui se dispersent, couleurs qui fuient vers le blanc, comme une aquarelle inachevée. Le monde se termine, pas avec un mur mais avec l'absence.
- **Fog of war :** Voile blanc-bleuté animé. Les zones non explorées ne sont pas sombres — elles ne sont "pas encore réelles". Les tiles se matérialisent quand le joueur explore.
- **Mort d'ennemi :** Désintégration en particules noires iridescentes (1-2s). Pas de cadavre — retour au néant.
- **Mort du joueur :** Le monde se décompose autour de lui. Tiles qui se désagrègent, couleurs qui fuient. Le Foyer s'éteint. Puis le Hub.
- **Rayon du Foyer :** Transition visible entre "réalité stable" (tiles nets et colorés) et "réalité effilochée" (tiles dégradés).

### Lisibilité

**Règle absolue :** Le joueur doit toujours pouvoir distinguer son personnage, les ennemis et leurs attaques, les éléments interactifs, et le terrain navigable. Même dans le chaos de la nuit avec 30+ ennemis, la lisibilité prime sur l'esthétique.

### Animation

- Personnage : mouvement fluide, dash, récolte. Pas d'animation d'attaque visible (auto-attaque = effets visuels sur les ennemis).
- Ennemis : silhouette ET animation distinctes par type → reconnaissable à distance, même dans le chaos.
- Environnement : brume d'effacement, poussière, cendres, végétation qui bouge, feux animés.
- Combat : flash de hit, nombres de dégâts qui pop, particules d'impact, screen shake léger (toggle).

---

## 8. DIRECTION AUDIO

> **Détails complets :** voir Bible Artistique & Narrative, sections 8.1-8.4.

### Philosophie

**Le silence est le son le plus important de VESTIGES.** Le jeu n'a pas de fond sonore constant. Quand la musique s'arrête, quand il n'y a que le vent et les pas du joueur — c'est un moment.

**Pas d'orchestre. Pas d'épique. Pas de synthwave.** La musique est intime et étrange : piano désaccordé, guitare avec delay long, nappes synthétiques qui ressemblent à des voix, percussions d'objets du quotidien (métal qui résonne, bois qui craque, eau qui goutte en rythme).

**Références :** Ólafur Arnalds (piano + électronique), Ben Frost (drones abrasifs pour la nuit), Disasterpeace/Hyper Light Drifter (mélancolie de jeu), Gustavo Santaolalla/TLOU (guitare, intimité), Hildur Guðnadóttir (violoncelle, tension).

### Musique par phase

| Phase | Style | Émotion |
|-------|-------|---------|
| **Jour — exploration** | Piano notes espacées, guitare acoustique, field recordings (vent, oiseaux) | Mélancolie, émerveillement, solitude |
| **Jour — combat** | Le track se densifie : percussions subtiles, basse sourde | Tension contenue |
| **Crépuscule** | Les mélodies se distordent, dissonances, drone basse-fréquence | Appréhension |
| **Nuit — premières vagues** | Percussions sèches (métal, bois), basses profondes, cordes staccato | Adrénaline, peur contrôlée |
| **Nuit — chaos** | Tout s'empile : percussions lourdes, drones saturés, cordes agressives | Panique, power fantasy |
| **Aube** | Tout s'éteint. Silence. Puis une note de piano, simple, claire. | Soulagement, repos |
| **Hub** | Nappes synthétiques réverbérées, piano avec delay long, sons inversés | Flottement, introspection |
| **Mort** | La musique en cours se "casse" — ralentit, se distord. Silence. | Du vide. |

### Son adaptatif

La musique réagit au gameplay en temps réel : nombre d'ennemis proches → intensité musicale. HP bas → battement de cœur subtil. Dernières 2 minutes du jour → dissonance légère. Entrer dans la zone du Foyer → sons hostiles amortis, crépitement enveloppant. Multi-kills → stinger musical satisfaisant.

### Sound design

- **Le silence comme outil :** Ne pas surcharger. Les sons sont des événements, pas du bruit de fond.
- **Les créatures ne rugissent pas comme des animaux.** Elles émettent des sons du quotidien déformés : murmure étiré en grondement, rire d'enfant ralenti, grincement de porte amplifié. Chaque type a un son signature reconnaissable.
- **Foyer :** Un crépitement doux, presque tonal. Comme un ronronnement. Le son de la sécurité.
- **Craft terminé :** Son "complet", comme un puzzle qui s'emboîte. Court, satisfaisant, Pavlovien.
- **Level up :** Son ascendant cristallin qui coupe tout pendant une demi-seconde.
- **Pas du joueur :** Changent selon la surface (béton, herbe, eau, bois). Toujours présents, jamais trop forts.
- **Indicateurs audio :** Ennemi derrière, structure qui craque, timer qui approche de la fin.

---

## 9. UI / UX

### Principes

1. **Semi-diégétique :** Les éléments de HUD appartiennent au monde — textures de parchemin usé, bordures en métal oxydé, polices pochoir. L'UI a une esthétique de survivaliste pragmatique.
2. **Minimalisme informé :** Peu d'éléments à l'écran, mais chacun est dense en information (la barre de vie change de couleur ET de texture quand elle baisse).
3. **Le monde reste visible :** Aucun menu ne couvre plus de 40% de l'écran pendant une run.
4. **Feedback visuel > texte :** La barre de vie change de couleur plutôt qu'afficher un nombre. Le timer est une barre, pas un chrono.
5. **Animation subtile :** Les panneaux glissent, ne poppent pas. Les chiffres de score roulent comme un compteur mécanique.

### HUD en jeu

- **Barre de vie** (en haut à gauche) — avec indicateurs de statuts.
- **Barre de timer jour/nuit** (en haut, pleine largeur) — position du soleil qui s'éteint progressivement. Pas de lune — la nuit est du vide.
- **Score courant** (en haut à droite) — compteur discret, +points popup à chaque kill.
- **Barre d'XP / niveau** (sous la vie) — progression vers le prochain level-up et choix de perk.
- **Barre rapide** (en bas) — 3-4 slots pour capacités actives et consommables. Pas de bouton d'attaque (auto-attaque).
- **Mini-map** (en bas à droite, optionnelle) — fog of war, position du Foyer, POI découverts.
- **Indicateurs contextuels** — touche d'interaction quand proche d'un objet interactif, quantité de ressources quand proche d'un nœud de récolte.

### Menus

- **Inventaire** — panneau latéral droit, catégorisé, filtrable.
- **Craft** — panneau latéral gauche, recettes connues, filtre "craftable maintenant".
- **Carte** — plein écran, vue de la map explorée, marqueurs placés par le joueur.
- **Journal** — fragments de lore trouvés, classés par thème.
- **Paramètres** — accessibilité complète (remapping, taille de texte, screen shake toggle, daltonisme).

---

## 10. CONTRAINTES TECHNIQUES

### Architecture (principes)

- **Modularité :** Chaque système (craft, combat, construction, génération procédurale, scoring) est un module indépendant.
- **Data-driven :** Les perks, recettes, stats d'ennemis, courbes de scaling, et loot tables sont dans des fichiers de données externes (JSON/YAML). Ça permet d'itérer sur le balancing sans recompiler, et facilite le modding.
- **Prêt pour le coop :** L'architecture réseau n'est pas implémentée en v1, mais le code est structuré pour qu'un deuxième joueur puisse être ajouté (pas de singleton monolithique, séparation logique/affichage).
- **Save system :** Sauvegarde automatique à chaque aube (une run peut être reprise). Les données méta sont sauvegardées indépendamment.
- **Score & Leaderboard :** Les scores sont calculés localement et validés côté serveur. Un backend léger (Steamworks ou solution custom) gère les leaderboards. La seed hebdomadaire est distribuée par le même backend.
- **Performance :** Objectif 60 FPS constant sur hardware mid-range (2020+). L'isométrique 2D aide beaucoup.

### Génération procédurale

- **Approche :** Wave Function Collapse (WFC) ou Cellular Automata pour le terrain, placement procédural de POI par-dessus.
- **Seed :** Chaque monde a une seed partageable. Le joueur peut entrer une seed manuellement.
- **Validation :** Le générateur vérifie que le monde est "jouable" (pas de Foyer encerclé de murs infranchissables, ressources minimales accessibles, au moins 1 POI par biome).

### Modding (futur)

- Prévoir une structure de données externalisée (JSON/YAML) pour les recettes, les stats d'ennemis, les loot tables → facilite le modding ultérieur.

---

## 11. ACCESSIBILITÉ

- Remapping complet des touches.
- Sous-titres pour tous les éléments audio narratifs.
- Mode daltonien (3 types).
- Taille de texte ajustable.
- Screen shake désactivable.
- Indicateurs visuels en complément des indicateurs audio.
- Difficulté ajustable via les mutateurs (pas de mode "facile" stigmatisant, mais des leviers : timer plus long, ennemis moins résistants, ressources plus abondantes).

---

## 12. MONÉTISATION & BUSINESS

| Élément | Détail |
|---------|--------|
| **Prix Early Access** | 12-15€ |
| **Prix Release** | 18-22€ |
| **DLC envisagé** | Extensions de biomes, nouvelles storylines, mode coop (v2) |
| **Micro-transactions** | Non. Jamais. |
| **Modding** | Encouragé, Steam Workshop à terme |

---

## 13. SCOPE & PRIORISATION

### Ce qui est IN (v1.0 — release Steam)

- Core loop complet (jour/nuit en boucle infinie, mort inévitable)
- 6-8 personnages jouables avec arbres de perks distincts
- 5+ biomes
- 10+ types de créatures + élites + mini-boss
- 50+ recettes de craft
- Système de construction complet
- Progression in-run (level-up, perks, coffres)
- 30+ Souvenirs (lore puzzle + déblocages)
- Événements aléatoires (8-10)
- Hub complet avec sélection de personnage et méta-progression
- Système de score complet avec leaderboards (global, amis, par personnage, hebdomadaire)
- Seed hebdomadaire
- Mutateurs de difficulté
- Musique et sound design complets
- Tutoriel intégré (apprentissage par le jeu)
- Accessibilité de base
- Localisation FR / EN

### Ce qui est IN (Early Access)

- Core loop complet mais réduit (3 biomes, 6-8 créatures, 3-4 personnages)
- Progression in-run fonctionnelle (level-up + 30 perks)
- Système de score + leaderboard basique (global + amis)
- Hub simplifié
- 15 Souvenirs
- 5 événements aléatoires
- Localisation FR / EN
- Système de feedback joueur intégré

### Ce qui est OUT (v1)

- Multijoueur / coop
- Mode PvP
- Monde persistant entre les runs (la base est TOUJOURS perdue à la mort)
- Véhicules
- Système de faction / diplomatie
- Crafting de vêtements cosmétiques
- Mode créatif / sandbox
- "Fin" du jeu / boss final / victoire narrative
- Console / mobile

### Ce qui pourrait être en v2+

- Coop 2 joueurs
- Nouveaux biomes, personnages et storylines (DLC)
- Mode défi quotidien (en plus de l'hebdomadaire)
- Tournois communautaires avec seeds imposées
- Steam Workshop / modding officiel
- Console (Switch, PS, Xbox)

---

## 14. RISQUES IDENTIFIÉS

| Risque | Impact | Mitigation |
|--------|--------|------------|
| Le jour est ennuyeux | Critique | Auto-attaque + ennemis permanents hors base = gameplay actif à chaque seconde. Coffres, événements, montée en puissance immédiate, lore à découvrir. |
| Scope creep | Critique | Ce document. Respecter les priorités. Chaque feature passe le test : "Est-ce que ça sert le core loop ?" |
| Le scoring est déséquilibré / exploitable | Élevé | Playtesting, analytics, itération. Les mutateurs et personnages permettent d'ajuster sans tout casser. |
| Fatigue de solo dev | Élevé | Milestones courts et jouables. Célébrer chaque étape. Varier les tâches. |
| Perks déséquilibrés / combo brisé | Élevé | Pool de perks externalisée (data-driven). Facile à ajuster sans toucher au code. Analytics sur les win rates par build. |
| Génération procédurale trop générique | Élevé | Hand-crafted POIs combinés au procédural. Le procédural pour le terrain, le hand-craft pour le contenu. |
| Performance en nuit (beaucoup d'ennemis) | Moyen | Object pooling, frustum culling, LOD simplifié, tests de perf réguliers. |
| Le leaderboard attire les tricheurs | Moyen | Validation serveur, détection d'anomalies, modération. Pas critique en EA. |
| Courbe de scaling trop brutale ou trop lente | Moyen | Playtesting fréquent, courbe paramétrable, données de run. |

---

## 15. GLOSSAIRE

| Terme | Définition |
|-------|-----------|
| **Run** | Une partie complète, du réveil à la mort. Chaque run est un fragment de réalité différent en train d'être oublié. Il n'y a pas de "victoire" — seulement un score. |
| **Foyer** | Ancrage de mémoire au centre de la base. Sa lumière n'est pas thermique — elle est ontologique : elle rappelle au monde qu'il existe. Repousse les créatures, stabilise la réalité autour de lui. |
| **Effacement** | La catastrophe qui a défait la réalité. Pas une destruction — un oubli. Le monde a cessé de se souvenir de lui-même. |
| **Créatures** | Ce qui pousse dans le vide laissé par l'oubli. Pas des monstres au sens classique — de la matière qui comble les trous de la réalité. Attirées par la lumière (la mémoire) qu'elles cherchent à consommer. |
| **Souvenirs** | Fragments de lore (puzzle optionnel en 6 constellations) qui débloquent aussi du gameplay. Se souvenir = renforcer la réalité = devenir plus fort. |
| **Vestiges** | Monnaie méta accumulée entre les runs, proportionnelle au score. Également le titre du jeu — les choses suffisamment ancrées dans la mémoire pour ne pas disparaître. |
| **Essence** | Résidu de mémoire concentrée, récoltée sur les créatures et les anomalies. Manipuler l'Essence c'est manipuler la substance de la réalité. |
| **Hub** | L'intérieur de la conscience du joueur entre les runs. Espace onirique qui grandit avec les Souvenirs retrouvés. |
| **Perks** | Améliorations choisies au level-up pendant une run. Perdus à la mort. |
| **Build** | La combinaison personnage + perks + gear d'une run. Émerge des choix du joueur. |
| **Ancré / Effiloché / Effacé** | Les trois états de la réalité. Ancré = stable. Effiloché = dégradé. Effacé = le néant. |
| **Scaling** | L'augmentation progressive et infinie de la difficulté. Narrativement : le monde se souvient de moins en moins bien de lui-même à chaque cycle. |
| **Seed** | Graine de génération procédurale. Même seed = même monde. |
| **Mutateurs** | Options modifiant la difficulté d'une run (augmentent le multiplicateur de score). |
| **POI** | Point of Interest — lieu notable sur la map. |

---

> **Ce document est vivant.** Il évolue avec le projet. Chaque modification majeure doit être documentée avec une date et une raison.
>
> **Règle d'or gameplay :** Si une feature ne peut pas être justifiée par "ça rend le core loop plus addictif", "ça donne envie de relancer", ou "ça rend la compétition de score plus intéressante", elle n'a pas sa place dans VESTIGES.
>
> **Règle d'or univers :** Chaque pixel, chaque son, chaque mot doit passer le test : "Est-ce que c'est cohérent avec un monde qui est en train d'être OUBLIÉ, et avec un joueur qui essaie de SE SOUVENIR ?" (cf. Bible Artistique & Narrative)
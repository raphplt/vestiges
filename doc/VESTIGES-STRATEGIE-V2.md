# VESTIGES — Stratégie V2 : Plan Complet

> **Version :** 1.1
> **Date initiale :** 12 mars 2026
> **Amendement :** 22 septembre 2026
> **Statut :** Document stratégique — remplace la roadmap V1 et les sections obsolètes du GDD/Bible
> **Auteur :** Raphaël + Claude (design partner)

---

## Amendement de direction — retours validés des 21 et 22 septembre 2026

Les [décisions de Raphaël](plans/DECISIONS.md) et le [dossier de plans v0.6](plans/README.md) précisent les sections historiques ci-dessous. Le socle de déplacement est implémenté, vérifié techniquement et explicitement validé par Raphaël le 22 septembre ([compte rendu](plans/01-deplacements.md#7-première-implémentation--21-septembre-2026)). Le [prototype D est livré](plans/01-deplacements.md#8-prototype-de-mobilité--22-septembre-2026) ; Raphaël valide le dash par défaut ; les variantes alternatives et la recette exhaustive restent ouvertes. La priorité suivante est la refonte du casting et de tous ses sprites avant 01 E.

- Déplacements de base dans les axes de l’écran et amplitude analogique préservée validés ; viser une mobilité très fluide clavier/manette. Le dash commun du prototype D est jouable ; ses variantes restent à comparer. Le dash livré est validé par Raphaël. Saut, glissade et mobilités spécifiques attendent la refonte et la validation d’au moins cinq à six personnages avec tous leurs sprites refaits (06/08).
- Score visible et animé pendant la run ; **aucun record à battre, progression vers un record ou annonce de dépassement en jeu ou en pause**. Record et célébration uniquement au bilan final, qui doit être largement refondu.
- Début actuellement jugé trop facile : menace à renforcer et niveaux à espacer. La prescription historique de level-up rapide ne bloque pas cette révision ; calibrer par playtests.
- Quatre armes et quatre passifs conservés ; ajouter un système distinct d’objets avec raretés, sans limite de slots ou d’exemplaires fixée par le design, avec cumul explicite et déblocage partiel initial.
- **Priorité du 28 septembre :** refondre ensemble les objets et les **perks**. Après comparaison des formules, Raphaël choisit **B : quatre perks qualitatifs maximum, acquis une fois, sans niveaux ni raretés** ; les armes s'améliorent et les objets se cumulent sans plafond de slots/exemplaires. Le [catalogue de spécialisation](plans/05-perks-specialisations.md) fixe neuf règles validées pour une V1 extensible ; B0 (données, contrats), B1 (acquisition), B2 (cinq effets de combat et de survie) et B3 sans objets (Sillage, Seconde lecture) sont livrés et vérifiés ; Délestage, Habitude et les objets restent à implémenter. Les coefficients et paliers initiaux restent à éprouver ; le plafond 70/99 et la courbe ne sont pas arbitrés. Le [catalogue commun initial](plans/05-catalogue-objets-perks.md) conserve l'audit, pas une cible acquise de seize perks à dix niveaux.
- Retour du 22 septembre : portée de mêlée trop faible, à augmenter dès les armes de base ; accroître la proportion d’armes à distance ; prévoir des objets/bonus de portée et de diamètre d’impact. Le [plan 05](plans/05-armes-objets-builds.md) distingue allonge et taille de zone et prévoit leur cohérence avec les dégâts et les visuels. Valeurs et ratio final restent à calibrer.
- Quêtes variées et accès aux armes/objets/personnages **indépendants des Souvenirs**. Migrer les anciens droits sans les retirer. Le lore conserve sa progression narrative, ses constellations et les transformations du Hub ; les anciennes descriptions « Souvenir → accès gameplay » sont à remplacer.
- Hub très clair et peu textuel ; **Collection armes/objets accessible directement depuis le menu principal**.
- Casting plus diversifié et potentiellement décalé dans un univers cohérent ; nouvelles créatures demandées. Boss de familles, terrain amélioré et mécaniques originales sont détaillés dans les plans, avec variantes encore proposées.
- Pixel art conservé, suffisamment détaillé et uniforme. Le contrat numérique et la méthode de production recommandés en plan 08 restent à valider sur un ensemble de référence avant modification de la Charte.

Les propositions nouvelles (kits exacts, seuils, chiffres, règle d’invulnérabilité, boss généralisés, innovations) ne sont pas validées implicitement. Les cases §25 ne seront cochées qu’après implémentation et vérification. Les systèmes retirés par le pivot restent retirés.

---

# PARTIE I — LE PIVOT

## 1. Pourquoi pivoter

Le prototype V1 (Phases 0-6 complétées) a mis en lumière trois frictions fondamentales :

**Le confinement nocturne tue le flow.** Le cœur d'un roguelite c'est le mouvement perpétuel — explorer, tuer, looter, monter en puissance. Forcer le joueur à défendre un point fixe pendant 5-8 minutes casse ce flow. La nuit devenait un simulateur d'esquive de projectiles sans profondeur tactique.

**Le craft n'a pas prouvé sa valeur.** L'UI n'était pas assez fluide, les assets pas au niveau (Polygon2D), le rapport effort/récompense illisible. Le joueur pouvait survivre sans crafter. Un système que le joueur ignore est un système mort.

**Les runs étaient trop longues.** 15 min/cycle × 3-4 cycles = 45min-1h. Pour un roguelite où la mort est permanente, un investissement de 45 min avant de mourir est frustrant, pas motivant. Les jeux du genre visent 15 à 30 min par run.

## 2. La nouvelle identité

VESTIGES passe d'un **hybride roguelite/survie/craft** à un **roguelite d'exploration-combat nomade** dans un monde post-apocalyptique en cours d'Effacement.

**Le pitch V2 :**

> Un roguelite isométrique dans un monde déjà en ruines, en train d'être oublié. Le joueur est un nomade qui avance toujours — derrière lui, la réalité s'efface. Devant, les vestiges d'un monde qui se souvient à peine de lui-même. La question n'est pas "combien de nuits tu vas tenir" mais "jusqu'où tu vas aller".

**Les piliers de design :**

1. **Mouvement permanent.** Le joueur ne s'arrête jamais. Pas de base, pas de zone safe, pas de phase statique.
2. **L'Effacement est le jeu.** Ce n'est pas un décor — c'est LA mécanique de pression. Le monde se réduit activement autour du joueur.
3. **Montée en puissance addictive.** Perks, loot d'armes avec raretés, upgrades aux Autels. Le joueur doit se sentir plus fort chaque minute.
4. **Endgame ouvert.** Pas de mort forcée à 30 min. Les bons joueurs peuvent aller très loin. Un boss/événement majeur marque la fin du "late game" classique, puis c'est l'endgame infini.
5. **Lore intégré au gameplay.** L'Effacement, les Souvenirs, les Autels — chaque mécanique a un sens dans l'univers.

### Références et garde-fous

Section unique pour les jeux cités (plan 17, lot 0D). Ailleurs dans la doc, on renvoie ici.

**Conventions du genre, communes à tous et assumées :** auto-attaque, level-up à trois choix, raretés colorées, montée en puissance continue, runs de 15 à 30 min, score, « encore une partie ».

**Citations de Raphaël** (registre des décisions) :
- *Megabonk* : inspiration pour le bilan de mort, et pour la variété du butin, avec une présentation « différente de Megabonk » (24 septembre).
- *Outer Wilds*, *Celeste*, *Megabonk* : inspirations pour la bande-son (26 septembre, plan 15).

**Garde-fous :**
- Aucune mécanique signature d'un jeu précis n'est reprise telle quelle. Chaque système propre part de l'Effacement ou de la mémoire (plan 17, principe 5).
- Aucun nombre de contenus ni réglage d'un autre jeu n'est une cible. Les chiffres cités (densités, durées) sont des ordres de grandeur, jamais des objectifs.
- Un écran inspiré d'un autre jeu s'adapte à Vestiges ; il ne se copie pas.

**Atmosphère et lecture** (GDD §1) : *Hades* (lisibilité), *Hollow Knight*, *Stalker*, *The Last of Us*, *Gris* (monde mort rendu vivant, mélancolie, couleur au service de l'émotion).

## 3. Ce qui disparaît

| Élément | Raison |
|---------|--------|
| Cycle jour/nuit binaire | Remplacé par l'Effacement progressif continu |
| Phase de nuit défensive | Le joueur est toujours en mouvement |
| Le Foyer (in-run) | Plus de base fixe. Le joueur est nomade |
| Le système de craft | La montée en puissance passe par perks + loot + Autels |
| La construction (murs, pièges, tourelles) | Plus de défense statique |
| Les ressources de craft (bois, pierre, métal, fibre, combustible, composants) | Seule l'Essence reste |
| L'inventaire de matériaux | Simplifié à XP + Essence |

## 4. Ce qui reste et se renforce

| Élément | Rôle dans V2 |
|---------|-------------|
| Combat en auto-attaque | Inchangé — le cœur |
| Armes + upgrades | Renforcé — rareté, loot dès le début, Autels |
| Perks de level-up | Inchangé — moteur de la montée en puissance |
| Essence | Seule ressource in-run. Upgrades aux Autels |
| Coffres et loot | Renforcés — plus fréquents, armes avec raretés |
| Exploration procédurale | Renforcée — map plus grande, biomes distincts |
| Personnages (6+) | Inchangés — rejouabilité |
| Score et leaderboards | Inchangé |
| Vestiges (monnaie méta) | Inchangé |
| Souvenirs (lore + déblocages) | Inchangé mais pickups rapides (pas d'exploration longue de ruines) |
| Le Hub (entre les runs) | Le Hub DEVIENT le "Foyer" narrativement |
| Le bestiaire complet | Renforcé — plus agressif, plus dense, types de Crise |
| Les biomes (3+) | Renforcés — map plus grande, biomes non-adjacents au centre |
| Les événements aléatoires | Adaptés au nouveau flow |

---

# PARTIE II — LA NOUVELLE BOUCLE

## 5. L'état initial du monde

**Le monde est déjà en ruines.** Le joueur n'arrive PAS dans un monde intact qui va s'effacer. Il arrive dans un monde post-apocalyptique déjà partiellement oublié — la nature a reconquis les villes, les structures tiennent par la force des souvenirs, la réalité est fragile partout. C'est beau, tragique, et dangereux dès la première seconde.

Conformément à la Bible : "La proposition visuelle est claire : VESTIGES n'est PAS un post-apo gris-brun. C'est un monde tragiquement, douloureusement beau." La nature a gagné. Les autoroutes sont sous les canopées. Les maisons sont englouties par le lierre. L'eau est claire. Les fleurs sont vives. Mais les murs se fissurent, les panneaux s'effacent, et les choses disparaissent quand personne ne les regarde.

**Visuellement au spawn :**
- Le monde est détaillé, coloré (palette "vivante" de la Bible), mais on voit déjà des signes d'Effacement : zones floues en périphérie, structures partiellement transparentes, poussière de particules blanches dans l'air.
- Les ennemis sont déjà là, dispersés. Le combat commence dans les 10 premières secondes.
- L'ambiance est celle de la "Forêt Reconquise" : calme trompeur, oiseaux, lumière dorée filtrée — mais des ombres bougent entre les arbres.

## 6. Le flow d'une run

```
SPAWN
│  Le joueur apparaît dans un monde post-apo déjà partiellement effacé.
│  Beau, dangereux, fragile. Ennemis présents immédiatement.
│  ↓
│
EXPLORATION & COMBAT (continu, pas de phases)
│  Le joueur avance, explore, combat, loote.
│  Coffres fréquents → armes avec raretés, Essence, perks bonus.
│  Autels d'Essence dispersés → upgrades d'armes.
│  Level-ups via XP → choix de perks.
│  Souvenirs trouvés au sol → lore + déblocages.
│  ↓ (au fil du temps, l'Effacement s'intensifie)
│
PRESSION CROISSANTE
│  Les zones déjà visitées s'effacent progressivement.
│  Les ennemis se densifient. De nouveaux types apparaissent.
│  Le joueur est poussé vers l'AVANT, vers les zones intactes.
│  Des Résurgences (crises courtes 60-90s) ponctuent la run.
│  ↓
│
LATE GAME (~20 min pour les bons joueurs)
│  Le monde est majoritairement effacé.
│  Seules les zones devant le joueur subsistent.
│  Un ÉVÉNEMENT MAJEUR se déclenche (boss, anomalie, Résurgence finale).
│  Si le joueur survit → passage en ENDGAME.
│  ↓
│
ENDGAME (infini, pour les meilleurs)
│  L'Effacement est maximal. Le monde ne tient presque plus.
│  Vagues continues d'ennemis. Scaling infini.
│  Le joueur survit aussi longtemps que son build le permet.
│  Chaque seconde supplémentaire = score massif.
│  → Après le boss, la seule question : jusqu'où tient le build.
│  ↓
│
MORT
│  Le monde se décompose autour du joueur.
│  Le dernier vestige s'efface.
│  Score → Hub → Nouvelle run.
```

## 7. Tempo et durée des runs

| Profil joueur | Durée de run typique | Ce qui se passe |
|--------------|---------------------|-----------------|
| Débutant (premières runs) | 5-10 min | Apprend les bases, meurt avant le late game |
| Joueur régulier | 15-25 min | Atteint le late game, meurt au boss ou peu après |
| Bon joueur | 25-40 min | Bat le boss, entre en endgame, survit un moment |
| Expert / record | 40+ min | Endgame prolongé, chaque minute est un exploit |

**Il n'y a PAS de mort forcée.** Le scaling de l'Effacement et des ennemis finit par submerger le joueur, mais un build suffisamment puissant avec un bon joueur peut théoriquement tenir très longtemps. C'est ce qui crée le "just one more run" et la compétition au leaderboard.

---

# PARTIE III — LES SYSTÈMES DE JEU

## 8. L'Effacement — Mécanique centrale

### Concept

L'Effacement est le battement de cœur du monde. Ce n'est plus un événement cosmétique (la nuit tombe) — c'est un système de jeu actif qui façonne chaque seconde de la run.

Le monde a une "mémoire" qui s'épuise en temps réel. La présence du joueur ralentit l'Effacement dans sa zone immédiate (il "rappelle" le monde à l'existence par sa conscience), mais ne l'arrête pas. Les zones qu'il quitte commencent à s'effacer derrière lui.

### Fonctionnement

**Chaque chunk/zone de la map a un compteur de mémoire (0-100%).**

Facteurs d'Effacement :
- **Le temps (base)** : toutes les zones perdent de la mémoire à un rythme constant qui accélère au fil de la run.
- **La distance au joueur** : les zones proches du joueur s'effacent beaucoup plus lentement. Les zones lointaines s'effacent rapidement.
- **Le scaling global** : le rythme d'Effacement accélère globalement. Au début, c'est lent. À 20 min, c'est rapide. À 30 min, c'est brutal.

**Phases visuelles d'une zone :**

| Mémoire | État visuel | Gameplay |
|---------|------------|---------|
| 100-75% | **Ancrée.** Couleurs vives (palette vivante de la Bible). Monde détaillé, beau, post-apo reconquis par la nature. | Ennemis normaux, loot normal |
| 75-50% | **Fragile.** Couleurs qui commencent à se délaver. Brume légère au sol. Détails qui perdent de la netteté. Sons plus étouffés. | Ennemis légèrement plus denses |
| 50-25% | **Effilochée.** Couleurs pâles, structures transparentes par endroits. Brume visible. Craquements, sons distordus. Le sol se fissure visuellement. | Ennemis plus durs et plus nombreux. Débuffs légers au joueur (vitesse -10%, dégâts -10%) |
| 25-1% | **Effacée.** Quasi monochrome blanc-bleuté. Les structures se désagrègent en particules. Le sol est instable. Sons presque absents. | Ennemis élites. Débuffs lourds (vitesse -25%, dégâts -25%, prise de dégâts lente) |
| 0% | **Néant.** Vide blanc animé. Traversable mais extrêmement dangereux. | Dégâts continus. Ennemis de type "Néant" exclusifs. Sortir est la seule option |

**Le joueur ne peut pas empêcher l'Effacement.** La tendance est irréversible. Le monde meurt. Le joueur fuit vers l'avant.

### Implications sur la direction du joueur

L'Effacement crée un mouvement naturel vers l'avant. Le joueur ne décide pas consciemment "je dois avancer" — il le fait parce que rester c'est mourir lentement. C'est comme la zone qui rétrécit dans un battle royale, mais organique et narratif.

**Le joueur a quand même des choix de direction :** la map s'étend dans plusieurs directions. Certaines zones ont de meilleurs coffres, des Autels, des Souvenirs. Le joueur choisit OÙ avancer, pas SI il avance.

### Les Résurgences (crises)

Toutes les 3-5 minutes, l'Effacement **pulse** — une Résurgence.

**Déroulement d'une Résurgence :**

1. **Signal (30 sec avant) :** La musique change. Les bords de l'écran se désaturent. Un son grave, sourd, comme le monde qui retient son souffle. Les créatures normales fuient ou s'agitent.

2. **La Crise (60-90 sec) :** L'Effacement s'accélère brutalement partout. Des vagues de créatures surgissent de toutes les directions — pas d'un seul point. La lumière ambiante chute. Des types de créatures exclusifs aux Résurgences apparaissent (les Résurgents — créatures à mi-chemin entre l'ancré et l'effacé, semi-transparentes, imprévisibles). Le joueur survit EN MOUVEMENT.

3. **L'accalmie (après) :** Le monde se stabilise. L'Effacement reprend son rythme normal. Drop d'Essence augmenté pendant 30 sec. Coffre rare garanti à proximité. La prochaine Résurgence sera plus intense.

**Scaling des Résurgences :**

| Résurgence | Timing approximatif | Intensité |
|-----------|-------------------|-----------|
| 1ère | ~4 min | Introduction douce. Quelques ennemis en plus. Le joueur apprend le concept. |
| 2ème | ~8 min | Nettement plus intense. Premiers Résurgents. |
| 3ème | ~13 min | Dangereuse. Le joueur doit avoir un build solide. |
| 4ème | ~18 min | Très dangereuse. Prélude au late game. |
| 5ème+ | ~22 min+ | Élites et Résurgents en masse. Territore de l'endgame. |

## 9. La map — Plus grande, mieux structurée

### Problème actuel

La map actuelle est trop petite et les biomes convergent tous vers le centre, créant un patchwork disgracieux. Il faut une map qui donne envie d'explorer et qui soit cohérente visuellement.

### Nouvelle structure

**Génération par biomes contigus, pas concentriques.**

Au lieu de : Foyer au centre → cercles concentriques de biomes mélangés autour
On passe à : Le joueur spawn dans un biome principal → les autres biomes sont des régions adjacentes séparées.

```
Exemple de layout (vue macro) :

    ┌──────────────┐
    │   CARRIÈRE    │
    │   EFFONDRÉE   │
    └──────┬───────┘
           │ transition naturelle
    ┌──────┴───────────────────┐
    │                          │
    │    RUINES URBAINES       │
    │                          │
    │    [spawn possible]      │
    └──────┬───────────────────┘
           │
    ┌──────┴──────────────────────────┐
    │                                  │
    │      FORÊT RECONQUISE           │
    │                                  │
    │      [spawn possible]           │
    │                                  │
    └──────┬──────────┬───────────────┘
           │          │
    ┌──────┴───┐  ┌───┴──────────┐
    │ MARÉCAGES│  │   CHAMPS     │
    │          │  │   SAUVAGES   │
    └──────────┘  └──────────────┘
```

**Principes :**
- Chaque biome est une **zone contiguë** avec sa propre palette, ses props, ses ennemis, ses ressources.
- Les transitions entre biomes sont **graduelles** (pas de coupure nette) : la forêt devient progressivement plus marécageuse, les arbres deviennent des souches, le sol devient boueux, puis c'est le marécage.
- Le joueur spawn toujours dans un biome "d'introduction" (Forêt ou Champs — relativement calme) et découvre les biomes plus dangereux en s'éloignant.
- **La map est GRANDE.** Le joueur ne voit jamais les bords pendant une run normale. Il y a toujours du monde à explorer devant lui.
- Les biomes sont générés procéduralement mais avec des contraintes de voisinage (la carrière jouxte les ruines, les marécages jouxtent la forêt, etc.) pour que la géographie soit cohérente.
- Le **Sanctuaire** (POI rare) peut apparaître dans n'importe quel biome.

### Taille cible

La map explorable doit être au moins **4-5x plus grande** que la map actuelle. Le joueur ne devrait pouvoir explorer qu'une fraction de la map totale pendant une run, ce qui garantit que chaque run se déroule dans une partie différente du monde.

### Fog of War

Inchangé par rapport au GDD V1 : les zones non explorées ne sont pas "cachées dans le brouillard" — elles ne sont pas encore "réelles". Voile blanc-bleuté animé. Les tiles se matérialisent quand le joueur explore. C'est cohérent avec le lore et visuellement distinctif.

## 10. Le combat repensé

### Les ennemis doivent être une menace

Le prototype V1 avait des ennemis trop passifs et trop loin. En V2 :

- **Spawn plus proche** : les ennemis apparaissent juste hors écran, pas à 200m. Le joueur les rencontre immédiatement en avançant.
- **Densité augmentée** : toujours des ennemis visibles à l'écran. Le monde n'est jamais "vide" hors de la zone du joueur.
- **Agressivité accrue** : les ennemis poursuivent plus longtemps, chargent plus vite, ont des patterns qui punissent l'esquive passive (AoE, ennemis rapides qui coupent les trajectoires, charges).
- **Ennemis de mêlée dangereux** : pas juste des tireurs à distance. Des ennemis rapides au corps à corps qui forcent le positionnement.

### Diversité progressive

| Phase de la run | Types d'ennemis | Comportement |
|----------------|----------------|-------------|
| 0-5 min | Ombres, Charognards | Basiques, faibles, apprennent le rythme au joueur |
| 5-10 min | + Rôdeurs, Sentinelles | Plus variés, premiers patterns à esquiver |
| 10-15 min | + Tisseuses, Brutes | Ennemis qui changent le positionnement (immobilisation, charge) |
| 15-20 min | + Élites (Aberrations) | Propriétés aléatoires, dangereux, drops de qualité |
| 20+ min | + Mini-boss (Colosses) | Un par biome, loot garanti, patterns complexes |
| Résurgences | Résurgents (exclusifs) | Semi-transparents, imprévisibles, apparaissent/disparaissent |
| Endgame | L'Indicible | Boss massif. Le test ultime. |

### Armes avec raretés

> **Amendement du 26 septembre 2026 (décision de Raphaël, plan 17 §4.3)** : la rareté n'est plus portée par l'arme. Une arme n'a qu'un niveau et les gains accumulés de ses améliorations ; chaque amélioration du level-up tire une rareté (Commun à Légendaire) qui fixe l'ampleur du gain et le nombre de stats touchées. L'Autel donne une amélioration Rare au moins ; le reforgeage disparaît. Le tableau ci-dessous décrit l'ancienne intention.

**Chaque personnage démarre avec une arme de base (tier 1, commune).** Les autres armes sont trouvées en jeu.

| Rareté | Couleur bordure | Fréquence | Caractéristiques |
|--------|----------------|-----------|-----------------|
| **Commun** | Gris/Blanc | Très fréquent | Stats de base, pas d'effet spécial |
| **Inhabituel** | Vert | Fréquent | Stats +20%, 1 effet mineur |
| **Rare** | Bleu | Régulier | Stats +40%, 1 effet notable |
| **Épique** | Violet | Peu fréquent | Stats +70%, 1 effet majeur |
| **Légendaire** | Or | Très rare | Stats max, effet unique puissant, visuel distinct |

**Sources d'armes :**
- Coffres (toutes raretés, pondérées).
- Drops de miniboss (Rare+ garanti).
- Récompense de Résurgence (coffre post-crise).
- Autels (reforge/upgrade).

**Upgrades aux Autels :** dépenser de l'Essence pour monter la rareté d'une arme (Commun → Inhabituel coûte 10, Inhabituel → Rare coûte 25, etc.). Chaque upgrade ajoute ou améliore un effet.

## 11. Les Autels d'Essence

> **Amendement du 26 septembre 2026 (plan 17, vague 3, direction validée par Raphaël)** : l'Autel devient le **Mémorial**. On le ravive en rassemblant ses trois éclats (20 s) ; il stabilise sa zone et offre une bénédiction à rareté, puis des services contre de l'Essence (raviver une arme au choix, Rare au moins ; soin ; lever un Oubli). Son miroir est la **Faille** : une amélioration Épique ou Légendaire contre un **Oubli** (malus durable) et un point de **Péril** (créatures plus fortes, score, XP et raretés majorés), toujours refusable. L'Appel du Vide et les malédictions disparaissent. Écart avec le paragraphe « Interaction » ci-dessous, validé par Raphaël : le choix se fait sur un écran de trois cartes qui fige la run, comme le level-up.
>
> **Amendement du 7 octobre 2026 (plan 28, DECISIONS §78)** : le Péril n'a plus de plafond et ne paie plus directement. Il renforce les créatures (nombre, PV, dégâts) et multiplie le score ; XP et Essence viennent seulement des éliminations en plus. Il monte aussi aux stèles du Péril (une dizaine par carte) et avec le Sifflet d'arbitre (+1 par niveau).

### Concept

Les Autels sont des points de mémoire concentrée — des lieux qui résistent plus longtemps à l'Effacement. Ce sont les dernières "stations" du monde oublié. Visuellement : piliers de cristal avec une aura dorée, sol plus net autour, particules d'Essence flottantes.

### Fonctions

| Action à l'Autel | Coût | Effet |
|------------------|------|-------|
| **Upgrade d'arme** | Essence (scaling) | Monte la rareté de l'arme +1 |
| **Reforge** | Essence (fixe) | Re-roll les effets de l'arme (même rareté) |
| **Soin** | Gratuit (1x) | Restaure 30% HP à la première visite |
| **Choix de perk bonus** | Essence (élevé) | Un perk supplémentaire hors level-up |

### Placement dans le monde

- 4-6 Autels par map, répartis dans les différents biomes.
- Certains Autels sont dans des zones dangereuses (gardés par des élites, en zone d'Effacement avancé) → risk/reward.
- Les Autels résistent à l'Effacement plus longtemps que leur zone, mais finissent par disparaître aussi → urgence d'y aller.
- Un Autel utilisé a un cooldown (pas réutilisable immédiatement, mais un même Autel peut servir plusieurs fois si le joueur revient et qu'il existe encore).

### Interaction

Approcher un Autel → menu contextuel rapide (pas de menu plein écran). Le joueur voit ses options, fait son choix en 2-3 secondes, et repart. **Le monde ne se met PAS en pause** pendant l'interaction — les ennemis continuent de venir.

## 12. Économie simplifiée

| Ressource | Source | Usage in-run | Usage méta |
|-----------|--------|-------------|-----------|
| **XP** | Kills, coffres, exploration | Level-up → perks | — |
| **Essence** | Kills (créatures fortes), coffres, veines cristallines | Upgrades aux Autels | — |
| **Vestiges** | Score de fin de run | — | Déblocages permanents dans le Hub |

Trois ressources au total. Pas d'inventaire de matériaux. Pas de gestion de stock. Le joueur se concentre sur le combat et les choix de build.

## 13. La progression in-run

### Level-up et perks (inchangé)

- Tuer → XP → level-up → choix 1 perk parmi 3.
- Pool commune + pool spécifique au personnage.
- Les perks se combinent pour créer des builds émergents.
- Level-up rapide au début (dopamine immédiate), ralentit progressivement.

### Perks à adapter pour V2

**Perks à retirer** (liés au craft/base) :
- "Architecte" (murs +50% HP) → retiré.
- "Récupérateur" (structures rendent 75% matériaux) → retiré.
- Tous les perks liés aux tourelles, pièges, craft speed → retirés.
- "Torche vivante" (lumière qui repousse) → à adapter (pourrait devenir "Mémoire vive" — ralentit l'Effacement autour du joueur).

**Perks à ajouter** (liés au nouveau flow) :
- "Nomade" — +15% vitesse de déplacement. Simple mais crucial quand le monde s'efface.
- "Mémoire vive" — L'Effacement ralentit de 20% dans un rayon autour du joueur. Le joueur "ancre" le monde un peu mieux.
- "Résonance" — Les Autels offrent un choix de perk supplémentaire gratuit.
- "Marcheur du vide" — Les débuffs de zone effacée sont réduits de 50%. Permet d'explorer les zones dangereuses.
- "Second souffle" — Inchangé (revenir à 50% HP une fois par run).

### Les coffres (renforcés)

Le joueur doit tomber sur un coffre toutes les **60-90 secondes** d'exploration active. Les coffres sont la dopamine du jeu.

| Type | Fréquence | Contenu |
|------|-----------|---------|
| **Commun** (bois) | Très fréquent | Essence, parfois arme commune |
| **Rare** (métal) | Régulier | Arme (Inhabituel+), Essence, parfois perk bonus |
| **Épique** (cristal) | Peu fréquent | Arme (Rare+), perk garanti, Essence abondante. Gardé par un élite. |
| **Lore** (ancien) | Rare | Souvenir + récompense. Visuel distinct pour les chasseurs de lore. |

---

# PARTIE IV — LE LATE GAME ET L'ENDGAME

## 14. Structure du late game

Le late game n'est PAS "la même chose mais plus dur". C'est une escalade narrative et mécanique.

### Phase 1 : L'Éveil (~15-20 min)

Le monde commence à montrer des signes que "quelque chose de plus grand" se passe. L'Effacement n'est plus seulement la décomposition passive du monde — il y a une INTENTION derrière.

**Signaux :**
- Des structures apparaissent qui n'existaient pas avant — des formes impossibles, géométriques, qui n'appartiennent à aucun biome.
- Les Résurgences deviennent plus fréquentes et plus violentes.
- Des messages de lore trouvés à ce stade deviennent plus urgents, plus cryptiques.
- La musique change de caractère — plus intense, plus dissonante.

### Phase 2 : Le Boss / Événement Majeur (~20-25 min)

Un événement unique par run. Pas nécessairement un "boss" au sens classique — plutôt une rencontre qui marque un point de non-retour.

**Options de design (à tester) :**

**Option A — L'Indicible.** Le boss rare de la Phase 6 (trop grand pour l'écran) devient le boss de late game récurrent. Chaque run qui atteint ce stade l'affronte. Il est l'incarnation de l'Effacement — la chose qui EFFACE.

**Option B — La Convergence.** Pas un boss unique mais un événement : toutes les zones s'effacent simultanément sauf un petit îlot. Le joueur est entouré de néant, et les ennemis convergent en une dernière vague massive. Survivre = passer en endgame.

**Option C — Le Choix.** Le joueur trouve un Souvenir majeur qui lui offre un choix : "Se souvenir" (passer en endgame, scaling infini) ou "Oublier" (terminer la run avec un bonus de score). Ça donne au joueur le contrôle sur la fin de sa run.

### Phase 3 : L'Endgame (post-boss, infini)

**Si le joueur survit au boss / événement majeur :**

Le monde entre dans un état de "mémoire résiduelle". Les biomes n'existent plus vraiment — le paysage est un mélange surréaliste de fragments de réalité. L'Effacement est maximal et constant. Les ennemis spawnent en continu, avec un scaling infini.

**Ce qui change en endgame :**
- Plus de temps calme. Les ennemis sont TOUJOURS là, en masse.
- Le loot est de meilleure qualité (plus de Légendaires).
- L'Essence drop en abondance → le joueur peut encore upgrader aux Autels restants.
- Le score par seconde est multiplié → chaque minute supplémentaire vaut énormément.
- Visuellement : le monde oscille entre des flashs de couleur (souvenirs fugaces) et le blanc du néant. C'est beau et mélancolique.

**L'endgame se termine quand le joueur meurt.** Pas de timer. Pas de fin forcée. Les meilleurs joueurs avec les meilleurs builds tiennent le plus longtemps.

**C'est ÇA qui crée la compétition au leaderboard** : pas juste "combien de temps tu survis" mais "est-ce que tu atteins l'endgame, et combien de temps tu tiens après".

---

# PARTIE V — LA LISIBILITÉ ET L'UX

## 15. Onboarding implicite

**Règle d'or : si le joueur ne comprend pas en 10 secondes de jeu, c'est mal conçu.**

| Seconde | Ce que le joueur apprend | Comment |
|---------|------------------------|---------|
| 0-10 | Se déplacer | Il est entouré d'ennemis, il bouge instinctivement |
| 10-20 | L'auto-attaque existe | Son personnage tape les ennemis proches tout seul |
| 20-40 | XP et Essence | Des orbes volent vers lui, une barre en bas se remplit |
| 40-60 | Premier level-up | Choix 1 parmi 3, descriptions courtes et claires |
| 1-2 min | Premier coffre | Coffre visible, ouverture, Essence ou bonus |
| 3-5 min | L'Effacement existe | Zone derrière lui qui se décolore visiblement |
| 4-6 min | Premier Autel | L'Autel pulse quand le joueur approche, menu simple |
| 4-5 min | Première Résurgence | Signal sonore + visuel 30 sec avant, vague intense |

### Affichage des armes

- **En jeu :** l'arme équipée est visible sur le sprite. Nom + rareté affichés brièvement au changement.
- **Menu pause :** armes possédées avec stats, rareté (bordure colorée), effets, tier.
- **Comparaison au loot :** quand le joueur trouve une arme, comparaison côte à côte (vert = mieux, rouge = pire).

### Le menu pause

- **Compact.** Ne prend pas toute la hauteur de l'écran.
- **Affiche :** armes équipées + stats, perks actifs, quêtes de run en cours, score actuel. Le record est réservé au bilan final.
- **Pas de :** inventaire de matériaux (il n'y en a plus), menu de craft (il n'y en a plus).

### La barre d'XP

**En bas de l'écran, fullwidth.** Le joueur voit sa progression vers le prochain level-up en permanence. C'est la barre de dopamine — elle se remplit en continu tant qu'il tue.

## 16. L'écran de mort

L'écran de mort est le **dernier souvenir** avant la décision de relancer. Il doit être impactant et motivant.

**Transition :**
1. Le monde se fige.
2. Les tiles autour du joueur se désagrègent en particules blanches, en spirale, depuis les bords vers le centre.
3. Le personnage se fige, sa silhouette se désature.
4. Le dernier son du monde s'éteint. Silence.
5. Fondu vers l'écran de score.

**Écran de score :**
- Score total avec compteur qui monte (effet satisfaisant).
- Détail : score de combat, score d'exploration, bonus de Résurgences survivées, bonus d'endgame.
- Nuits → remplacé par "Distance parcourue" + "Temps survécu" + "Résurgences survivées".
- Comparaison avec le record personnel.
- Position dans le leaderboard (amis + global).
- Vestiges gagnés.
- Quêtes complétées pendant la run.
- Bouton "Relancer" GROS et proéminent.

---

# PARTIE VI — LES QUÊTES

## 17. Structure des quêtes

### Quêtes de run (in-game, par run)

Générées au début de chaque run. 3-5 par run. Récompenses en Essence et bonus de score.

**Exemples :**
- "Tuer 50 créatures" → +100 Essence
- "Trouver 3 coffres rares" → perk bonus
- "Survivre à 2 Résurgences" → +500 score
- "Visiter un Autel en zone effacée" → arme Rare garantie
- "Tuer un Colosse" → +200 Essence
- "Explorer 3 biomes différents" → +300 score
- "Atteindre l'endgame" → +1000 score

**Affichage :** widget compact dans le HUD. Toast non-intrusif quand une quête est complétée (son satisfaisant + flash visuel).

### Quêtes de progression (permanentes, cross-run)

Milestones de déblocage. Trackées dans le Hub.

**Déblocages de personnages :**

| Personnage | Condition de déblocage |
|-----------|----------------------|
| Le Vagabond | Disponible de base |
| La Forgeuse | Survivre 15 minutes |
| Le Traqueur | Tuer 200 créatures en une run |
| L'Éveillée | Trouver 10 Souvenirs (cross-run) |
| Le Colosse | Survivre à 4 Résurgences en une run |
| L'Ombre | Atteindre l'endgame sans prendre de dégât pendant une Résurgence |

**Déblocages d'armes :**
- Certaines armes ne sont pas dans le loot pool de base. Elles se débloquent via des quêtes de progression.
- Exemple : "Tuer 500 créatures au total" → débloque le Bâton d'Essence dans le loot pool.
- Exemple : "Compléter 10 quêtes de run" → débloque le Fouet dans le loot pool.

**Autres déblocages :**
- Cosmétiques de personnage.
- Mutateurs de difficulté.
- Backgrounds du Hub.

### Quêtes de lore (optionnelles, cross-run)

Liées aux 6 Constellations de la Bible. Trouver des Souvenirs spécifiques dans le monde pour compléter chaque constellation. Récompenses thématiques uniques.

### Menu quêtes

- **Hub :** panneau "Chroniques" avec toutes les quêtes (run, progression, lore), leur avancement, leurs récompenses.
- **In-game :** widget HUD avec les quêtes de run actives. Pas les quêtes de progression (trop de bruit).

---

# PARTIE VII — LE HUB

## 18. Le Hub comme "Foyer narratif"

Le Foyer disparaît du gameplay in-run. Narrativement, le Hub DEVIENT le Foyer — l'espace de conscience du joueur entre les fragments de réalité. C'est le seul lieu "stable" dans un multivers qui s'oublie.

**Ce que le Hub contient :**
- **Le Miroir** : sélection de personnage. Chaque personnage débloqué est visible.
- **Le Tableau** : choix de mutateurs de difficulté (multiplicateurs de score).
- **Les Chroniques** : quêtes (progression + lore), journal de Souvenirs (organisé par constellation).
- **L'Écho** : leaderboards, historique des runs, records.
- **Le Passage** : lancer la run.

**Visuellement :** le Hub évolue avec la progression du joueur. Au début, c'est un espace presque vide — juste de la lumière dans le noir. Au fil des Souvenirs trouvés et des personnages débloqués, l'espace se "remplit" de détails, de couleurs, d'objets. C'est le joueur qui crée la réalité du Hub par ses souvenirs.

**Musicalement :** synthwave réverbérée, piano avec long delay, sons inversés. Flottement, introspection, entre-deux-mondes (cf. Bible section 8).

---

# PARTIE VIII — ART, SON, ET ASSETS

## 19. Les Polygon2D à remplacer

34 fichiers utilisent encore Polygon2D. Voici la priorisation par visibilité :

### Priorité 1 — Visibles en permanence
- `Player.tscn` / `Player.cs` — le joueur est à l'écran 100% du temps
- `Enemy.tscn` / `Enemy.cs` — les ennemis sont visibles en permanence
- `Projectile.cs` / `EnemyProjectile.tscn` — vus constamment

### Priorité 2 — Visibles régulièrement
- `Chest.tscn` / `Chest.cs` — coffres fréquents
- `WeaponPickup.cs` — loot d'armes (plus fréquent en V2)
- `ResourceNode.tscn` / `ResourceNode.cs` — à transformer en sources d'Essence ou retirer
- `XpOrb.cs` — visible à chaque kill

### Priorité 3 — Visibles ponctuellement
- `PointOfInterest.tscn` — POIs dans le monde
- `Indicible.cs` — boss rare
- Éléments de lore (`WaterMirror.cs`, `SwallowedSign.cs`, etc.) — 11 fichiers
- `InteractableAura.cs` — aura d'interaction
- `Foyer.cs` — à retirer (in-run) ou transformer (Hub)

### Priorité 4 — À retirer ou transformer
- `Structure.cs`, `Turret.cs` — systèmes retirés, les fichiers disparaissent
- `StructurePlacer.cs` — retiré

## 20. Pipeline d'assets

### Le problème

Tu es débutant en pixel art, sans graphic designer, et la solution actuelle (Pillow/Python) montre ses limites pour les sprites complexes (personnages animés, ennemis, environnements détaillés).

### La solution recommandée : pipeline hybride

**Pour les environnements et props (tiles, arbres, rochers, ruines, etc.) :**
1. Génération via IA (Stable Diffusion / FLUX avec LoRA pixel art isométrique).
2. Post-processing automatique : normalisation de palette (palette master de la Charte Graphique), nettoyage d'artefacts, uniformisation de l'angle iso.
3. Retouche manuelle dans Aseprite si nécessaire (ajuster quelques pixels, nettoyer des contours).

**Pour les sprites animés (personnages, ennemis) :**
C'est le point dur. Les options réalistes :
1. **Freelance pixel artist** pour les 10-15 sprites critiques (4 personnages × 5 animations + 6-8 ennemis principaux). Budget estimé : 500-1500€ selon le niveau du freelance.
2. **Sprite sheets IA + retouche Aseprite** : générer chaque frame individuellement, puis assembler et nettoyer manuellement. Plus long mais moins cher.
3. **Asset packs adaptés** (itch.io, OpenGameArt) : acheter des packs pixel art iso existants et les adapter à ta palette. Limité par ce qui existe.

**Recommandation :** option 1 (freelance) pour les personnages jouables, option 2 (IA + retouche) pour les ennemis et props.

### Angle isométrique

**TOUT doit avoir le même angle.** L'angle iso de VESTIGES doit être défini précisément (typiquement 2:1, soit ~26.57°) et tous les assets doivent respecter cet angle. C'est le critère de qualité n°1 pour la cohérence visuelle.

Le script de post-processing doit inclure une vérification/correction de l'angle iso.

## 21. Sound design V2

### Ce qui change

- **Les sons d'ennemis répétitifs sont retirés.** Pas de sons "idle" ou "alerte" en boucle. Le silence est un outil (cf. Bible).
- **Les sons d'impact restent.** Hit ennemi, hit joueur, mort d'ennemi (désintégration en particules).
- **Le son d'XP doit se fondre.** Quand les orbes arrivent en rafale, le son doit se superposer sans devenir désagréable. Technique : même son mais pitch randomisé légèrement, volume qui diminue quand beaucoup d'orbes arrivent simultanément.
- **Les musiques** s'adaptent au nouveau flow (pas de phase jour/nuit distincte mais une montée progressive) :
  - **Direction validée par Raphaël le 25 septembre 2026 :** musique de jeu vidéo avec mélodie identifiable, rythme et progression. La distorsion et l'Effacement peuvent transformer cette matière musicale. Les anciennes consignes de drones abstraits sont remplacées par le [guide audio V2](AUDIO-GUIDE.md). Raphaël choisit les candidats à l'écoute ; les agents assurent la recherche et la préparation.
  - Début de run : thème mélodique mélancolique, pulsation qui accompagne l'exploration, arrangement aéré.
  - Combat dense : la musique s'intensifie (percussion, basse).
  - Résurgence : distorsion, urgence, percussion agressive.
  - Post-Résurgence : retour au calme, piano.
  - Endgame : mélange de tout — intense mais avec une beauté mélancolique. Le joueur sait qu'il va mourir, et la musique accompagne cette acceptation.

---

# PARTIE IX — IMPACT SUR LE CODE

## 22. Systèmes à retirer / désactiver

| Système | Fichiers | Action |
|---------|----------|--------|
| CraftManager | `scripts/Base/CraftManager.cs` | Supprimer |
| Inventory (ressources) | `scripts/Base/Inventory.cs` | Simplifier → EssenceTracker |
| StructureManager | `scripts/Base/StructureManager.cs` | Supprimer |
| StructurePlacer | `scripts/Base/StructurePlacer.cs` | Supprimer |
| Structure, Wall, Trap, Turret, Torch | `scripts/Base/*.cs` | Supprimer |
| ResourceNode | `scripts/Base/ResourceNode.cs` | Transformer → EssenceNode (veine de cristal) |
| Foyer | `scripts/World/Foyer.cs` | Supprimer (in-run). Hub garde le concept. |
| RecipeDataLoader | `scripts/Infrastructure/RecipeDataLoader.cs` | Supprimer |
| ResourceDataLoader | `scripts/Infrastructure/ResourceDataLoader.cs` | Simplifier |
| Cycle DayNight (GameManager) | `scripts/Core/GameManager.cs` | Retirer, remplacer par ErasureManager |
| Recettes JSON | `data/recipes/` | Supprimer |
| Ressources JSON (hors Essence) | `data/resources/` | Simplifier |

## 23. Systèmes à créer

| Système | Responsabilité | Dépendances |
|---------|---------------|-------------|
| **ErasureManager** | Compteur mémoire par zone, rythme global, phases visuelles, scaling | GameManager (orchestration) |
| **CrisisManager** | Timing et intensité des Résurgences, spawns spéciaux, signaux précurseurs | ErasureManager, SpawnManager |
| **AltarSystem** | Spawn d'Autels, interaction, upgrades, reforge, soin, cooldowns | WeaponInstance, EssenceTracker |
| **WeaponRaritySystem** | Raretés des armes, génération de stats, effets, comparaison | WeaponDataLoader |
| **EssenceTracker** | Remplace l'inventaire complexe. Track l'Essence du joueur. | EventBus |
| **QuestManager** | Quêtes de run (génération, tracking, complétion) et de progression | EventBus, MetaSaveManager |
| **QuestDataLoader** | Charge les définitions de quêtes depuis JSON | — |
| **EndgameManager** | Détecte la transition vers l'endgame, gère le boss/événement, scaling infini | ErasureManager, CrisisManager |

## 24. Systèmes à modifier

| Système | Modification |
|---------|-------------|
| **GameManager** | Retirer cycle jour/nuit. Intégrer ErasureManager comme driver. Nouvelle machine d'états : Exploration → Crisis → Exploration → LateGame → Endgame → Death |
| **SpawnManager** | Spawns liés à l'Effacement (densité proportionnelle à la mémoire perdue). Types progressifs. Spawns de Résurgence. |
| **Player** | Retirer interactions craft/base. Ajouter interaction Autels. |
| **Enemy** | Spawn plus proche. Agressivité augmentée. Variantes de Résurgence. |
| **HUD** | Retirer UI craft/inventaire. Ajouter : barre XP fullwidth en bas, quêtes, indicateur d'Effacement, comparaison d'armes. |
| **WeaponInstance** | Ajouter raretés, effets par rareté, comparaison. |
| **WeaponPickup** | Afficher rareté (couleur de bordure), comparaison au ramassage. |
| **Chest** | Fréquence augmentée. Loot d'armes avec raretés. |
| **PerkManager** | Retirer perks craft/base. Ajouter perks V2 (Nomade, Mémoire vive, etc.). |
| **World generation** | Map plus grande (4-5x). Biomes contigus, pas concentriques. Transitions graduelles. |
| **Score** | Adapter les catégories (plus de "nuits survivées", remplacer par distance/temps/Résurgences). |

---

# PARTIE X — PLAN D'EXÉCUTION

## 25. Phases de développement

### Phase A — Le nouveau cœur (2-3 semaines)

**Objectif :** la nouvelle boucle est jouable. Pas de polish, juste le flow.

- [x] Retirer/désactiver les systèmes obsolètes (craft, base, Foyer in-run, cycle jour/nuit).
- [x] Implémenter ErasureManager (mémoire par zone, rythme global, phases visuelles en placeholder — même juste un changement de teinte sur les tiles).
- [x] Fiabiliser les horloges : expiration des statuts des ennemis au loin, reste temporel et rattrapage borné de l’Effacement, phases et dégâts conservés ; 26 assertions automatisées (plan 10, lot 6A, 28 septembre 2026).
- [x] Réduire le coût des impacts continus du Transistor sans modifier la cadence des dégâts/procs : feedback cadencé, signal sans tableau temporaire, chiffres mis à jour à valeur affichée changée ; avant/après et 38 assertions vérifiés (plan 10, lot 6B, 28 septembre 2026).
- [x] Calcul de l’Effacement limité aux cellules actives, état du Néant conservé et réactivation par les lieux : ordre des phases identique, recalculs −23,8 % sur le rejeu de 30 min, régressions/captures et Main prolongée vérifiées (plan 10, lot 6C, 1er octobre 2026).
- [x] Préchauffage réellement rendu pendant le chargement : 16 shaders de run et particules d’XP, viewport libéré, audits GPU et Main vérifiés, banc d’intégration fiabilisé (26 assertions) (plan 10 5B1, 1er octobre 2026). Compilation froide et à-coups restent à mesurer en 5B2.
- [x] Implémenter CrisisManager (Résurgences toutes les 3-5 min, spawns en burst).
- [x] Résurgences qui débordent le plafond de run (×1,4 pendant la crise, sans reflux) : la 2e crise passe de +2 % à +45–64 % de foule proche, mesuré sur 3 profils × 4 graines ([plan 30 T1](plans/30-temps-forts.md#t1-livré--9-octobre-2026)).
- [x] Deux Souverains à heure fixe (2:30 et 7:00) entre les Résurgences, PV ×4, sans Tenace : combat de 5 à 58 s (médiane 24,5 s) avec le build de référence ([plan 30 T2](plans/30-temps-forts.md#t2-livré--9-octobre-2026)).
- [x] Adapter SpawnManager (spawns liés à l'Effacement, plus proches, plus denses).
- [x] Agrandir la map (doubler la taille pour tester, objectif final 4-5x).
- [x] Revoir la génération : biomes contigus, pas concentriques.
- [x] Biomes mélangés dès le départ : mosaïque de régions (chaque biome revient plusieurs fois), frontière visible dès le premier écran ; régression du 14 mars mesurée sur 40 seeds et corrigée ([plan 10 §6](plans/10-terrain-et-tiles.md#6-régression-un-seul-biome-autour-du-départ--25-septembre-2026)).
- [x] Socle de déplacement écran : amplitude analogique, vitesse diagonale bornée, animation/pas sur mouvement réel et protections hors run ; vérification Godot headless du [plan 01](plans/01-deplacements.md#7-première-implémentation--21-septembre-2026).
- [x] Validation par Raphaël des déplacements de base refaits (22 septembre 2026, plan 01 B/C).
- [ ] Recette humaine du socle de déplacement : clavier/manette, terrain et interactions en run, ressenti et caméra (plan 01 A–C).
- [x] Prototype technique de mobilité commune (plan 01 D) : dash remappable Espace/X, module composé et paramètres JSON, distance stable au stick, recharge/buffer, variantes directe/brève et protection nulle/courte ; régressions automatisées.
- [x] Protections de mobilité vérifiées : murs/coins, hurt/mort, pause sans buffer résiduel, interruptions coffre/POI et ralentissements ; intégration vraie Main pour liaison automatique Effacement, Néant et eau générée (plan 01 D).
- [x] Feedback du prototype de mobilité : animation, traînées recyclées selon intensité, jauge de recharge et sons existants ; parcours GL 1280×720 jusqu’au retour Hub et smoke 600 frames ([preuves du plan 01](plans/01-deplacements.md#8-prototype-de-mobilité--22-septembre-2026)).
- [x] Mesure de mobilité en combat dense : code courant sans/avec dash, 120 ennemis actifs proches, dix cas GL à 720p/1080p ; moyennes, percentiles, pics et mémoire conservés. La cible 60 FPS constants n’est pas atteinte ; cette mesure ne clôt pas la recette D ni le travail de performance ([résultats du plan 01](plans/01-deplacements.md#9-mesure-de-mobilité-en-combat-dense--22-septembre-2026)).
- [ ] Recette du lot D : comparer les variantes clavier/manette en combat, retenir timings et protection, vérifier le coût en combat dense et recalibrer le début de run (plan 03).
- [x] Validation par Raphaël du dash commun livré (22 septembre 2026).
- [ ] Refonte et validation d’au moins cinq à six personnages et de tous leurs sprites avant les mobilités spécifiques (06/08).
- [ ] Mobilités de personnages : prototypes et profils après casting et nouveaux sprites validés (plan 01 E).
- [x] Micro-événements v1 (plan 12) : directeur calé autour des Résurgences, cinq événements (Souverain, Harde, Vestige tombé, Veille, Averse d'éclats), repères au sol, bandeau et flèche de bord, données JSON ; captures en vraie run (24 septembre 2026).
- [x] Variantes renforcées data-driven (plan 12) : élites naturelles à affixe, Souverains, Aberrations et affixes de phase dans `_variants.json`.
- [x] Recette humaine des micro-événements et des élites : cadence validée en l'état, élites bien dosées, événements appréciés (Raphaël, 25 septembre 2026). Détails à revoir plus tard ; le soin, peut-être trop rare, est reporté aux plans 13/03 (plan 12 §7).
- [ ] Ajuster le tempo : runs de 15-25 min en gameplay normal.
- [ ] **PLAYTEST : est-ce que c'est fun ? Est-ce que l'Effacement crée de la tension ? Est-ce que le mouvement permanent fonctionne ?**

### Phase B — Autels et montée en puissance (2-3 semaines)

- [x] Implémenter AltarSystem (spawn, interaction, upgrades d'armes) ; refondu en Mémoriaux (plan 17 lot 3B, 26 septembre 2026).
- [x] Raretés Commun → Légendaire, portées par les améliorations d'arme et de passif plutôt que par l'arme (décision 4.3 du [plan 17](plans/17-armes-coffres-modificateurs.md), vague 1, 26 septembre 2026).
- [x] Risque choisi : Péril, Failles et Oublis, à la place de l'Appel du Vide et des malédictions ([plan 17](plans/17-armes-coffres-modificateurs.md) lots 3A et 3C, 26 septembre 2026).
- [x] ~~Rendre les armes lootables dans les coffres dès le début de run.~~ Retiré le 1er octobre 2026 : les armes viennent du level-up ([DECISIONS §40](plans/DECISIONS.md), plan 24 L1).
- [x] Implémenter EssenceTracker (remplace l'inventaire).
- [ ] Ajuster l'économie d'Essence (drop rates, coûts d'upgrade aux Autels).
- [ ] Comparaison d'armes au loot.
- [ ] **PLAYTEST : est-ce que la montée en puissance est satisfaisante ? L'économie d'Essence est-elle équilibrée ?**

### Phase C — Late game et endgame (2 semaines)

- [x] Implémenter EndgameManager (détection du late game, transition endgame).
- [ ] Implémenter le boss / événement majeur de late game (tester les 3 options, en choisir une).
- [ ] Implémenter le scaling infini d'endgame.
- [ ] Ajuster le score pour refléter le nouveau flow.
- [ ] **PLAYTEST : est-ce que le late game / endgame donne envie de rejouer ? Le "just one more run" fonctionne ?**

### Phase D — Lisibilité et UX (2-3 semaines)

- [x] Écrans de choix : réouverture et entrée interrompue sans éléments masqués ni sélection prématurée ; 24 contrôles et captures Main vérifiés (plan 04 R7, 1er octobre 2026).

- [x] Radar et carte : détails de terrain par biome, routes/eau, phases et légende ; 17 contrôles et captures Main vérifiés (plan 04 R6). Recette artistique humaine encore ouverte.

- [x] Pression des tirs, première correction ciblée : recharge du Hurleur ×2,5 en données, cri conservé ; banc fixe, quatre runs et captures vérifiés (plan 07 R3b). Ressenti humain encore ouvert.

- [x] Barre d'XP fullwidth en bas de l'écran (déplacée le 24 septembre 2026 dans la plaque de vie du HUD refait, [plan 04](plans/04-interfaces-et-hub.md#retour-de-raphaël-et-hud-de-run--24-septembre-2026)).
- [x] HUD de run lisible : plaques contrastées, jauge de PV sous le héros, police Saira Semi Condensed, boussole retirée ; captures 1080p/4K vérifiées (plan 04, 24 septembre 2026).
- [x] Plaque PV/niveau retravaillée et compactée après retour de Raphaël (190 × 38) : cadre de métal patiné, jauge en relief, métriques de police et alignements corrigés ; huit états capturés en 1080p/720p, build sans warning et smoke verts (plan 04 R8, 7 octobre 2026).
- [x] Recette humaine du HUD refait et de la police : « HUD bien mieux » (Raphaël, 25 septembre 2026).
- [x] Armes dans le menu pause avec stats effectives et dégâts infligés, passifs et fiche du personnage (plan 17 lot 1C ; la rareté n'est plus portée par l'arme).
- [x] Menu pause compact : navigation à gauche, équipement élargi, textes retirés et survol/focus simplifiés ; captures 100/130 %, navigation clavier et événements A/B vérifiés (plan 24 R1, plan 04, 1er octobre 2026).
- [x] Icônes de nature du butin : Essence, XP, objets, Souvenirs et treize statistiques ; badge de rareté séparé, roulette et résultats capturés (plan 24 R2, plan 04, 1er octobre 2026).
- [ ] Onboarding implicite (les 5 premières minutes doivent être auto-explicatives).
- [x] Indicateurs visuels de l'Effacement (phases, transitions de couleur) : sol qui oublie et lisière de l'Effacé, plan 16 O1/O3 (26 septembre 2026) ; les décors suivront en O2.
- [x] Signaux précurseurs des Résurgences.
- [ ] Sound design cleanup (retirer sons répétitifs, ajuster XP, musique adaptative).
- [x] Intégration des 50 choix audio A–B6 applicables et nettoyage des fichiers inutilisés ; banque JSON, crédits et archives d’écoute hors dépôt (plan 15).
- [x] Neuf sons d'armes choisis le 7 octobre intégrés à l'identique, gains et cadence bornés, salves et galerie moteur vérifiées ; quinze timbres en deuxième écoute (plan 15, A3b1).
- [x] Treize choix d'armes R2 intégrés à l'identique, son du Transistor au départ du cône, tests et galerie moteur vérifiés ; 22 timbres retenus, Cloche et Boîte à musique en reprise ciblée (plan 15, A3b3).
- [x] Cloche B et Boîte à musique C intégrées : 24 armes sonorisées, notes aléatoires au contact orbital sans répétition immédiate, cadence et voix communes ; régressions et galerie moteur vérifiées (plan 15, A3b4).
- [x] Plan 15 A3a : priorités/plafonds des voix en JSON, limitation UI en pause, fondus sûrs, arrêt des ambiances au Hub et gains de boucle/musique ; banque, régressions, smoke et enregistrements avant/après vérifiés (7 octobre 2026). Recette artistique du mix encore ouverte.
- [x] Écran de mort reworké (transition visuelle + score détaillé + stats) : bilan en trois zones, première passe du plan 02 lot D (27 septembre 2026) ; séquence de mort dans le monde, lot M1 ; distance, éliminations par arme et frise relevées, lot M2 ; bilan en une page dense, lot M3 ; gains animés, lot M4 (28 septembre 2026). Recette en jeu de Raphaël attendue.

### Refonte objets et spécialisations — plan 05, 28 septembre 2026

- [x] B0 : catalogue de neuf spécialisations extensible, lecture/validation des données, contrats de combat/soin et provenance explicite du modèle XP ; build sans avertissement, contrats/armes/ennemis/modèle et smoke vérifiés, sans activation d’effets inachevés ([compte rendu](plans/05-perks-specialisations.md#11-compte-rendu-b0--socle-livré-et-vérifié-le-28-septembre-2026)).
- [x] B1 : acquisition de quatre perks uniques aux paliers 2/6/12/20 dans la file de niveaux (report, cascade, relance, bannissement protégé, première offre composée, éligibilité selon l'arsenal), carte et pause ; en sommeil en run normale tant qu'aucun effet n'est branché, vérifiée par banc et capture en aperçu ([compte rendu](plans/05-perks-specialisations.md#12-compte-rendu-b1--acquisition-livrée-et-vérifiée-le-29-septembre-2026)).
- [x] B2 : Prévoyance, Reprise, Débordement, Convergence et Propagation actifs en run avec leurs retours visuels (barre de PV, case d'arme, chiffre renforcé, repère, trait), état inactif, pause chiffrée et avertissement d'échange ; banc d'effets, captures et banc dense A/B vérifiés ([compte rendu](plans/05-perks-specialisations.md#13-compte-rendu-b2--effets-de-combat-et-de-survie-livrés-le-29-septembre-2026)).
- [x] B3 (partie sans objets) : Sillage (couloir de collecte, orbes endormies rappelées) et Seconde lecture (carte reportée) actifs en run, bancs et captures vérifiés ([compte rendu](plans/05-perks-specialisations.md#14-compte-rendu-b3-partie-sans-objets--sillage-et-seconde-lecture-29-septembre-2026)).
- [x] Plan 21 lot G1 : fragments offerts après chaque Résurgence survécue (fin des paliers de niveau) ; bannissements gratuits puis coût croissant en Péril ([compte rendu](plans/21-historique.md#17-compte-rendu-g1--fragments-après-les-résurgences-bannir-coûte-du-péril)).
- [x] Plan 28 P1–P3 : Péril sans plafond et recentré sur les créatures, affiché au HUD ; stèles du Péril sur la carte ; Sifflet d'arbitre ([plan 28](plans/28-peril-et-difficulte.md), 7 octobre 2026).
- [x] Plan 29 F1 : créatures avancées par une seule boucle C# au lieu d'un rappel moteur chacune, mode `Floating`, banc dense corrigé (terrain dégagé, découpage de l'image) ; 400 créatures 37 → 57 FPS, 32/32 suites ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 29 A : index de la foule (grille C#) à la place des 30 parcours de toutes les créatures par l'interop ; 32/32 suites ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 29 F2 : séparation entre créatures et arrêt au contact du joueur, réglages dans `data/scaling/crowd.json` ; planche gardée par Raphaël ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 29 B : créatures sans corps physique (grille des décors bloquants, touches des projectiles et des orbes par l'index de la foule) ; 32/32 suites, six contrôles ajoutés ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 29 C1 : plus aucun rappel moteur par créature ni par projectile (registres C#), séparation calculée en une passe ; banc dense 1 000 créatures 3,7 → 71 FPS ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 29 C2–C3 : ombres en lots (créatures et tirs), tirs ennemis hors du moteur physique, orbes en registre, séparation un tick sur deux ; 1 000 créatures 60 → 87 FPS, 1 500 → 25 → 46 ([plan 29](plans/29-performance-des-foules.md), 9 octobre 2026).
- [x] Plan 21 lot G2a : les passifs deviennent des objets (6 emplacements, 50 niveaux par formule, effets multiples, 1 à 5 niveaux selon la rareté, 12 objets de propriété) ([compte rendu](plans/21-historique.md)).
- [x] Plan 21 lot G2a-2, étape 1 : paliers d'objets (données, activation au franchissement, annoncés seulement s'ils sont codés, cartes et pause), Papier carbone (copies d'attaque à dégâts réduits, +1 copie aux niveaux 25 et 50) et Pince à linge (Durée des statuts, renouvellement au palier 25) ([compte rendu](plans/21-historique.md#21-compte-rendu-g2a-2-étape-1--socle-des-paliers-papier-carbone-pince-à-linge)).
- [x] Plan 21 lot G2a-2, étape 2 : paliers du niveau 25 des douze autres objets de propriété, chacun avec son retour en jeu ([compte rendu](plans/21-historique.md#22-compte-rendu-g2a-2-étape-2--les-douze-paliers)).
- [x] Plan 21 lot G2b, étape 1 : anciens Dons retirés des coffres et des fouilles (niveaux d'objet à la place), avec leurs six synergies et les effets qui n'existaient que par eux ([compte rendu](plans/21-historique.md#24-compte-rendu-g2b-étape-1--retrait-des-anciens-dons)).
- [x] Plan 21 lot G2b, étape 2 : statut Fragilité, coefficient de déclenchement par arme, Allumette humide, Glaçon, Thermomètre et Épingle à nourrice avec leurs paliers ([compte rendu](plans/21-historique.md#25-compte-rendu-g2b-étape-2--socle-des-déclencheurs-et-quatre-objets-dimpact)).
- [x] Plan 21 lot G2b, étape 3 : Pétard mouillé, Dé à coudre, Semelle usée et Boîte de pansements avec leurs paliers ([compte rendu](plans/21-historique.md#26-compte-rendu-g2b-étape-3--élimination-marche-niveau)).
- [x] Plan 22 lot C0 : mesure de départ de la carte (lieux croisés et visités par minute, événements, Essence gagnée et dépensée ; bot qui ratisse) ([compte rendu](plans/22-carte-a-explorer.md#12-compte-rendu-c0--mesure-de-départ-30-septembre)).
- [x] Plan 21 lot G0 : propriétés de la grammaire commune nommées sur les cartes d'armes et d'objets, armes concernées par un objet ([compte rendu](plans/21-historique.md#27-lot-g0--les-propriétés-nommées-sur-les-cartes--découpage-30-septembre)).
- [x] Plan 21 lot G3, étape 1 : ascensions au niveau 50, deux voies au choix et définitives ; Arc du gymnase, Faucille, Cloche d'école, Boîte à musique ([compte rendu](plans/21-historique.md#29-compte-rendu-g3-étape-1--mécanique-et-quatre-armes)).
- [x] Plan 22 lot C1 : Puits, Veine de cristal et Épouvantail sur les décors déjà générés, avec signe discret, usage unique et perte au Néant ([compte rendu](plans/22-carte-a-explorer.md#14-compte-rendu-c1--trois-petits-lieux-30-septembre)).
- [x] Plan 23 R0 : mesure de référence de la puissance du joueur face aux ennemis.
- [x] Plan 23 R1 : bouclier de départ retiré, invulnérabilité après un coup réduite.
- [x] Plan 23 R2 : cartes de niveau à la Megabonk, inventaire et stats affichés pendant le choix.
- [x] Plan 23 R3 : objets à 30 niveaux et gains francs, projectiles en plus au lieu des copies, Reflet brisé réactivé, paliers au niveau 15.
- [x] Plan 21 G6a : les 24 armes montent leur nombre (tirs, frappes, ondes, orbes, cibles) à un poids franc ; Papier carbone favorisé dans les offres (2 octobre 2026).
- [x] Plan 21 G6b : vol de vie, objet Paille tordue et bonus de coffre, soin plafonné par seconde (2 octobre 2026).
- [x] Plan 21 G6c : objets de Chance, d'XP et d'aimant renforcés, coffres et Repères alignés (2 octobre 2026).
- [x] Plan 21 G6e : projectiles, notes et coups de mêlée grandissent avec la stat de taille, plafond de lisibilité (2 octobre 2026).
- [x] Plan 22 C2a–b : l'Atelier, 4 par carte : Trempe offerte, forge d'arme (quitte le Mémorial) et Retrempe contre de l'Essence ; Repère, carte, stabilisation (2 octobre 2026).
- [x] Plan 23 R4 : stats entières fractionnaires et pas d'armes relevés.
- [x] Plan 23 R5 : difficulté réglée sur la nouvelle puissance, mesurée.
- [x] Plan 23 R6 : huit objets de déclencheur restants (G2c).
- [x] Plan 23 R7 : six autres petits lieux, carte agrandie en hauteur et minimap (plan 22 C4, C6).
- [x] Plan 23 R8 (révisé, DECISIONS §38) : chaque coffre donne en plus du butin un bonus d'une stat au hasard.
- [x] Plan 23 R9 : Essence rendue par le Porte-monnaie hors de la quête d'accumulation ; Repères (plan 22 §3 B), un peu de Chance au premier usage de chaque type de lieu.
- [x] Plan 21 lot G3, étape 2 : voies des 20 autres armes (validées DECISIONS §40, en jeu au plan 24 L12).
- [ ] Délestage et Habitude, puis B4 : catalogue d'objets, récompense à choix, inventaire cumulable, migration des anciennes sources, intégration UI et validation en run ([prérequis](plans/05-perks-specialisations.md#14-compte-rendu-b3-partie-sans-objets--sillage-et-seconde-lecture-29-septembre-2026)).

### Phase E — Quêtes et personnages (2-3 semaines)

- [x] Reprise visuelle de la Forgeuse : lunettes, tablier et gants précisés, 152 frames déterministes et captures Main vérifiées (plan 08 R5c). Approbation artistique humaine encore ouverte.

- [x] Reprise visuelle du Vagabond : écharpe et sac identifiables, 152 frames déterministes et captures Main vérifiées (plan 08 R5b). Approbation artistique humaine encore ouverte.

- [x] Reprise visuelle du Traqueur : 152 frames déterministes, silhouettes comparées sur trois sols et animations capturées dans Main (plan 08 R5a). Approbation artistique humaine encore ouverte.

- [x] Implémenter QuestManager + QuestDataLoader.
- [ ] Quêtes de run (3-5 par run, générées dynamiquement).
- [ ] Quêtes de progression (déblocages de personnages et d'armes).
- [ ] Rendre le système de déblocage fonctionnel.
- [ ] S'assurer que 4 personnages sont jouables et équilibrés.
- [x] Menu quêtes dans le Hub ("Chroniques").
- [x] Notifications in-game de complétion de quête.

### Phase F — Art et polish (4+ semaines)

- [ ] Mettre en place le pipeline d'assets (IA + post-processing + Aseprite).
- [ ] Remplacer les Polygon2D priorité 1 (Player, ennemis principaux, projectiles).
- [x] Remplacer les Polygon2D priorité 2 (coffres, armes, orbes).
- [x] Tiles d'Effacement (phases visuelles des zones : Ancrée → Effacée) : shader du sol, plan 16 O1 (26 septembre 2026).
- [x] Sprites des Autels : Mémorial (endormi, ravivé), éclat et Faille, procéduraux (plan 17 lot 3B, 26 septembre 2026).
- [ ] Sprites des Résurgents (ennemis de Résurgence).
- [x] Plan 25 S4 : sprites des cinq familles de tirs ennemis, apparitions dans la visée existante, ombres 2:1 et impacts recyclés au sol ; œil du Présage sur sa frappe de zone, captures et régressions vérifiées. Coût S4+S6 mesuré avant/après à 720p/1080p, une passe par version (voir compte rendu).
- [x] Plan 25 S6 : XP dorée, crâne, minimap et sceaux de quêtes branchés ; progression, bris et complétion capturés dans une vraie quête.
- [x] Plan 25 S5 : cinq bonus 16 px branchés au plan 24 C4, flottement, lueur 2:1 et disparition ; cinq ramassages capturés, effets et réutilisation du pool vérifiés.
- [x] Plan 25 S1 : cinq raretés, cadres et reflets légendaires, sauts de Chance et trèfle branchés ; glyphes retirés, écrans de choix capturés.
- [x] Plan 25 S2/S3 : 34 icônes d'objets natives 32/16 px ; 31 objets actifs raccordés aux écrans, HUD, pause et bilan, trois objets du monde visibles « À venir » en Collection. Résolveur d'anciennes icônes supprimé.
- [x] Plan 25 S7 : quatre fonds animés natifs, neuf habillages de menus et cinq sols de chargement branchés ; rendu nearest vérifié par captures. Correctif I7 : rotation continue des rayons à 8°/s, poussières scintillantes ; fond fixe I6 remplacé conformément à DECISIONS §43.
- [x] Plan 25 S8 : 14 icônes de Réminiscences en Collection, neuf définitions raccordées aux interfaces ; les effets non implémentés restent « À venir ». Choix et inventaire capturés.
- [x] Hub visuel (camp du Foyer vivant, validé par Raphaël le 26 septembre 2026, plan 04).
- [ ] Musiques adaptatives (5-6 tracks).
- [ ] Sound design complet.

### Phase G — Early Access prep

- [x] Mode dev explicite : toggle mémorisé dans le Hub au lancement F5, contenu existant débloqué, profil/progression/records/analytics séparés, Steam désactivé ; bascules normal ↔ dev testées et activation exclue des exports Debug/Release ([guide](DEV-MODE.md)).
- [x] Plan 26 Q0 : validations strictes et profils isolés, bancs cône/UI remis à jour, global séquentiel 21/21 avec build/import partagés et verrou de checkout ; pannes injectées, mesures réelles et contrôles GL vérifiés ([preuves](audits/qualite-2026-10-02/q0/README.md), [guide](VALIDATION.md)).
- [x] Plan 26 Q7c-4a : catalogue des cinq biomes validé en entier avant publication, définitions en lecture seule, refus récupérable sans génération partielle ; 590 valeurs avant/après identiques, 190 contrôles de catalogues, validations 9/9 puis 5/5 et vraie fiche invalide vérifiés ([preuves](audits/catalogues-biomes-2026-10-06/README.md), 6 octobre 2026). Les autres catalogues de génération restent ouverts.
- [x] Plan 27 V0 : shader des entités et des projectiles du joueur appliquant de nouveau Modulate (flash des coups, annonces, Rampant terré, éclair et clignotement du joueur, liseré des projectiles), modes `--capture-statuses` et `--capture-player-hit` ; planches avant/après et banc A/B vérifiés (6 octobre 2026).
- [x] Plan 27 V1a : figé, ralenti, brûlure et Fragile lisibles sur le sprite des créatures (teinte de priorité, givre, fêlures, bord chaud, animation figée ou ralentie), réglages en données contrôlés ; planche, 13 contrôles et banc A/B vérifiés (7 octobre 2026).
- [x] Plan 27 V1b : étoiles, larmes et braises autour des créatures désorientées, saignantes ou brûlantes, à la hauteur réelle du sprite, jamais coupées par les réglages d'effets ; planche et 6 contrôles vérifiés (7 octobre 2026).
- [x] Plan 27 V1c : statuts vérifiés sur trois sols, en foule de soixante et au banc A/B avec armes à statut (Cloche, Lampe, Berceuse, Polaroïd) ; stries du ralenti retirées pour leur coût (7 octobre 2026).
- [x] Plan 27 V2a : chiffres des brûlures et saignements regroupés toutes les 0,5 s par créature, à la couleur du statut, reliquat à la mort ; galerie, 6 contrôles et banc A/B vérifiés (7 octobre 2026).
- [x] Plan 27 V2b : formes des Craies dessinées à la taille réelle de leur zone, écho des Gants à son rayon réel, gerbes à la couleur de l'arme ou de l'objet sur les coups secondaires et les dégâts d'objets ; galeries et banc A/B vérifiés (7 octobre 2026).
- [x] Plan 27 V2c : champ du Chronomètre rempli et à sa taille réelle, premier maillon du Trousseau, éclat des projectiles en bout de course ; galerie, 2 contrôles et banc A/B vérifiés (7 octobre 2026).
- [x] Plan 27 V2d : icônes des paliers d'objets quand ils agissent, trait de la brûlure transmise, poussière du recul ; galerie, contrôle et banc A/B vérifiés (7 octobre 2026).
- [x] Plan 27 V3a : éclair rouge du joueur blessé, vignette tournée vers le coup, chiffre des dégâts reçus, secousse lisible réglée en données ; planche et 2 contrôles vérifiés (7 octobre 2026).
- [x] Plan 27 V3b : toile annulée avec le coup, toile visible sur le joueur et près de sa jauge, joueur pâli quand l'Effacement le ralentit ; planche, 3 contrôles et contre-épreuve vérifiés (7 octobre 2026).
- [x] Plan 27 V3c : bouclier qui casse ou revient plein, armure qui pare, coup ignoré, soins visibles, icône de vol de vie seulement quand des PV sont rendus ; planche, contrôle et contre-épreuve vérifiés (7 octobre 2026).
- [x] Plan 27 V3d : le Néant consume sans le paquet d'une blessure (vignette pâle, pas de secousse ni de son de coup, dash libre) ; planche et contrôle vérifiés (7 octobre 2026).
- [x] Plan 27 V4 : anneau de détonation de l'Instable blessé, cause de mort juste pour une explosion ou un événement, couloirs de tir allongés et épaissis ; planches et 4 contrôles vérifiés (7 octobre 2026).

- [ ] Scope final : 3-4 biomes, 4+ personnages, 8+ types d'ennemis, 30+ perks, 10+ armes, 15+ Souvenirs, quêtes.
- [ ] Bug fix et performance (60 FPS, 100+ ennemis en endgame).
- [ ] Accessibilité (remapping, taille texte, screenshake toggle, colorblind — déjà partiellement en place).
- [ ] Steam (page, screenshots, description, trailer).
- [ ] Launch.

---

# PARTIE XI — DOCUMENTS À METTRE À JOUR

## 26. Impact sur la documentation existante

| Document | Statut | Action nécessaire |
|----------|--------|------------------|
| **VESTIGES-GDD.md** | Partiellement obsolète | Sections à réécrire : Core Loop (§3), Craft (§4.6), Construction (§4.7), Nuit (§3 "La Nuit"). Sections à ajouter : Effacement, Autels, Endgame. Le reste (combat, perks, coffres, personnages, score) reste valide avec des ajustements. |
| **VESTIGES-BIBLE.md** | Partiellement obsolète | Le lore (Partie I) est intact. La direction artistique (Partie IV) est intacte — les palettes "vivante" et "corrompue" deviennent les palettes "Ancrée" et "Effacée" au lieu de "Jour" et "Nuit". L'audio (Partie V) doit être adaptée au nouveau flow (pas de phases jour/nuit distinctes). Les mentions du Foyer in-run et du cycle jour/nuit doivent être mises à jour. |
| **VESTIGES-ARCHITECTURE.md** | Partiellement obsolète | Le système Base & Craft disparaît. Le système World perd le cycle jour/nuit et gagne l'Effacement. Nouveaux systèmes à documenter (Erasure, Crisis, Altar, Quest). Les principes fondamentaux et l'architecture en couches restent valides. |
| **VESTIGES-ROADMAP.md** | Obsolète | Remplacé par le plan d'exécution de ce document (Partie X). Les phases 0-6 restent comme historique. |
| **CHARTE-GRAPHIQUE.md** | Valide | Les palettes s'appliquent désormais aux phases d'Effacement au lieu du cycle jour/nuit. Ajout nécessaire : palette "Effacée" (blanc-bleuté) et palette "Néant" (vide pur). |
| **ASSET-LIST.md** | À réviser | Retirer les assets de craft/base (structures, tourelles, pièges). Ajouter : Autels, Résurgents, indicateurs d'Effacement, UI de quêtes. |
| **STRATEGIE-BIOMES-ET-WORKFLOW.md** | À réviser | La stratégie de biomes change (contigus, pas concentriques). Le workflow asset doit intégrer le nouveau pipeline IA. |
| **Ce document (VESTIGES-STRATEGIE-V2.md)** | Actif | Fait autorité sur le gameplay et la direction du projet jusqu'à ce que le GDD soit mis à jour. |

---

# PARTIE XII — RISQUES ET QUESTIONS OUVERTES

## 27. Risques identifiés

| Risque | Probabilité | Impact | Mitigation |
|--------|------------|--------|-----------|
| L'Effacement progressif n'est pas fun | Moyenne | Critique | Phase A se termine par un playtest. Si pas fun → itérer avant de continuer. Le concept est testable très rapidement (juste un timer qui assombrit les zones + plus d'ennemis). |
| Le jeu devient un "fuis toujours dans la même direction" | Moyenne | Haute | Les Autels, coffres, et POIs créent des raisons de dévier. Le joueur choisit OÙ aller, pas juste "en avant". L'Effacement n'est pas directionnel — il est temporel (les zones anciennes s'effacent, pas un mur qui avance). |
| Sans zone safe, le joueur ne respire jamais | Moyenne | Haute | Les accalmies post-Résurgence sont des moments de respiration. Les zones à haute mémoire sont plus calmes. Les Autels offrent un mini-répit (soin). Calibrer la pression pour qu'il y ait un rythme tension-release. |
| Le pipeline d'assets ne produit pas assez de qualité | Haute | Haute | Commencer par les assets les plus critiques. Tester le pipeline tôt (Phase A). Si l'IA + post-processing ne suffit pas, investir dans un freelance pour les sprites clés. |
| L'endgame infini devient monotone | Moyenne | Moyenne | Varier l'endgame : ennemis exclusifs, événements aléatoires, palettes visuelles changeantes. Le score croissant et le leaderboard maintiennent la motivation extrinsèque. |
| Trop de systèmes retirés → joueurs V1 déçus | Basse (pas encore de joueurs) | Basse | Le jeu n'est pas encore sorti. Le pivot se fait maintenant, pas après l'Early Access. |

## 28. Questions ouvertes

1. **Le boss de late game** : Option A (L'Indicible), B (La Convergence), ou C (Le Choix) ? → À tester en Phase C.
2. **Le nombre exact de personnages au launch** : 4 minimum. Lesquels ? Vagabond, Forgeuse, Traqueur + L'Éveillée ou Le Colosse ?
3. **Les Souvenirs dans le nouveau flow** : comment les intégrer sans ralentir le joueur ? Pickups au sol ? Drops d'ennemis spéciaux ? POIs rapides ?
4. **Le Hub** : quel est son état actuel exact et combien de travail pour le rendre fonctionnel ?
5. **Budget freelance** : est-ce envisageable pour les sprites critiques ?
6. **Playtest externe** : est-ce que tu peux faire tester à 2-3 personnes après la Phase A ?
7. **Le mode coop V2** : est-ce que la nouvelle direction (joueur nomade sans base) change la vision du coop ? (Un nomade seul vs deux nomades ensemble — ça pourrait être encore plus fun.)

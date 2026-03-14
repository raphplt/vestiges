# VESTIGES — Stratégie V2 : Plan Complet

> **Version :** 1.0
> **Date :** 12 mars 2026
> **Statut :** Document stratégique — remplace la roadmap V1 et les sections obsolètes du GDD/Bible
> **Auteur :** Raphaël + Claude (design partner)

---

# PARTIE I — LE PIVOT

## 1. Pourquoi pivoter

Le prototype V1 (Phases 0-6 complétées) a mis en lumière trois frictions fondamentales :

**Le confinement nocturne tue le flow.** Le cœur d'un roguelite c'est le mouvement perpétuel — explorer, tuer, looter, monter en puissance. Forcer le joueur à défendre un point fixe pendant 5-8 minutes casse ce flow. La nuit devenait un simulateur d'esquive de projectiles sans profondeur tactique.

**Le craft n'a pas prouvé sa valeur.** L'UI n'était pas assez fluide, les assets pas au niveau (Polygon2D), le rapport effort/récompense illisible. Le joueur pouvait survivre sans crafter. Un système que le joueur ignore est un système mort.

**Les runs étaient trop longues.** 15 min/cycle × 3-4 cycles = 45min-1h. Pour un roguelite où la mort est permanente, un investissement de 45 min avant de mourir est frustrant, pas motivant. Les références (Megabonk, Vampire Survivors) visent 15-30 min par run.

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
│  → C'est le "Vampire Survivors after boss" / "Megabonk endgame".
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
- "Pilleur" — Les coffres ont +30% de chance de contenir une arme de rareté supérieure.
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
| 1-2 min | Premier coffre | Coffre visible, ouverture, arme ou bonus |
| 3-5 min | L'Effacement existe | Zone derrière lui qui se décolore visiblement |
| 4-6 min | Premier Autel | L'Autel pulse quand le joueur approche, menu simple |
| 4-5 min | Première Résurgence | Signal sonore + visuel 30 sec avant, vague intense |

### Affichage des armes

- **En jeu :** l'arme équipée est visible sur le sprite. Nom + rareté affichés brièvement au changement.
- **Menu pause :** armes possédées avec stats, rareté (bordure colorée), effets, tier.
- **Comparaison au loot :** quand le joueur trouve une arme, comparaison côte à côte (vert = mieux, rouge = pire).

### Le menu pause

- **Compact.** Ne prend pas toute la hauteur de l'écran.
- **Affiche :** armes équipées + stats, perks actifs, quêtes de run en cours, score actuel, record à battre.
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
  - Début de run : ambient minimal (la "mélancolie merveilleuse" de la Bible).
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

- [ ] Retirer/désactiver les systèmes obsolètes (craft, base, Foyer in-run, cycle jour/nuit).
- [x] Implémenter ErasureManager (mémoire par zone, rythme global, phases visuelles en placeholder — même juste un changement de teinte sur les tiles).
- [x] Implémenter CrisisManager (Résurgences toutes les 3-5 min, spawns en burst).
- [x] Adapter SpawnManager (spawns liés à l'Effacement, plus proches, plus denses).
- [ ] Agrandir la map (doubler la taille pour tester, objectif final 4-5x).
- [x] Revoir la génération : biomes contigus, pas concentriques.
- [ ] Ajuster le tempo : runs de 15-25 min en gameplay normal.
- [ ] **PLAYTEST : est-ce que c'est fun ? Est-ce que l'Effacement crée de la tension ? Est-ce que le mouvement permanent fonctionne ?**

### Phase B — Autels et montée en puissance (2-3 semaines)

- [ ] Implémenter AltarSystem (spawn, interaction, upgrades d'armes).
- [ ] Implémenter WeaponRaritySystem (Commun → Légendaire, effets, génération).
- [ ] Rendre les armes lootables dans les coffres dès le début de run.
- [ ] Implémenter EssenceTracker (remplace l'inventaire).
- [ ] Ajuster l'économie d'Essence (drop rates, coûts d'upgrade aux Autels).
- [ ] Comparaison d'armes au loot.
- [ ] **PLAYTEST : est-ce que la montée en puissance est satisfaisante ? L'économie d'Essence est-elle équilibrée ?**

### Phase C — Late game et endgame (2 semaines)

- [ ] Implémenter EndgameManager (détection du late game, transition endgame).
- [ ] Implémenter le boss / événement majeur de late game (tester les 3 options, en choisir une).
- [ ] Implémenter le scaling infini d'endgame.
- [ ] Ajuster le score pour refléter le nouveau flow.
- [ ] **PLAYTEST : est-ce que le late game / endgame donne envie de rejouer ? Le "just one more run" fonctionne ?**

### Phase D — Lisibilité et UX (2-3 semaines)

- [ ] Barre d'XP fullwidth en bas de l'écran.
- [ ] Armes dans le menu pause avec stats et rareté.
- [ ] Menu pause compact.
- [ ] Onboarding implicite (les 5 premières minutes doivent être auto-explicatives).
- [ ] Indicateurs visuels de l'Effacement (phases, transitions de couleur).
- [ ] Signaux précurseurs des Résurgences.
- [ ] Sound design cleanup (retirer sons répétitifs, ajuster XP, musique adaptative).
- [ ] Écran de mort reworké (transition visuelle + score détaillé + stats).

### Phase E — Quêtes et personnages (2-3 semaines)

- [x] Implémenter QuestManager + QuestDataLoader.
- [ ] Quêtes de run (3-5 par run, générées dynamiquement).
- [ ] Quêtes de progression (déblocages de personnages et d'armes).
- [ ] Rendre le système de déblocage fonctionnel.
- [ ] S'assurer que 4 personnages sont jouables et équilibrés.
- [x] Menu quêtes dans le Hub ("Chroniques").
- [ ] Notifications in-game de complétion de quête.

### Phase F — Art et polish (4+ semaines)

- [ ] Mettre en place le pipeline d'assets (IA + post-processing + Aseprite).
- [ ] Remplacer les Polygon2D priorité 1 (Player, ennemis principaux, projectiles).
- [ ] Remplacer les Polygon2D priorité 2 (coffres, armes, orbes).
- [ ] Tiles d'Effacement (phases visuelles des zones : Ancrée → Effacée).
- [ ] Sprites des Autels.
- [ ] Sprites des Résurgents (ennemis de Résurgence).
- [ ] Hub visuel.
- [ ] Musiques adaptatives (5-6 tracks).
- [ ] Sound design complet.

### Phase G — Early Access prep

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

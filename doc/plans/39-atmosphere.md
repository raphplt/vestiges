# Plan 39 — Atmosphère et détail du monde

Demande de Raphaël, 11 octobre 2026 : « Comment rajouter plus d'atmosphère et de détail au jeu ? (pense large/vue d'ensemble) », puis « crée le plan ». Questions du §7 tranchées le même soir ([DECISIONS §119 bis](DECISIONS.md)) ; **A1 livré** (§8).

Unité d'estimation : **1 j = une journée de travail d'agent**, implémentation et vérification par capture comprises. Ordres de grandeur.

## 1. Constat

Capture de référence : ville et lisière des champs, début de run (`/tmp/vestiges-low-health-rounded/health-1-low.png`).

- **Lumière plate.** Un seul `CanvasModulate` à teinte chaude fixe (`scenes/Main.tscn`, `Color(1, 0.95, 0.85)`), modifié seulement par l'Indicible. Aucune ombre portée : immeubles, silos et arbres semblent posés sur le sol. Aucune lumière ponctuelle en run, pas d'étalonnage par biome.
- **Monde immobile.** Seules les canopées se balancent (`sway`) et les herbes plient (`prop_trample`). Pas de faune, pas de vent commun, pas de météo hors du boss. Rues sans trace de l'exode.
- **Silence.** Seule la forêt a une ambiance (boucle et oiseaux). Ville, marais, carrière et champs n'ont rien ; aucune réverbération (`AudioManager` : 5 bus panoramiques, pas de reverb).
- **Du travail dormant :**
  - les ambiances météo sont mappées dans `AudioManager.cs` (l. 1015–1018 : `thick_fog`, `storm`, `ash_rain`, `forgotten_wind`) mais aucun de ces événements n'existe dans `data/events/run_events.json` ;
  - les 11 anomalies de `scripts/World/Lore/` et les POI sont coupés (`world_gen.json` : `pois_enabled` et `lore_elements_enabled` à `false`, plan 17 lot 0B), avec un art provisoire en `Polygon2D` ;
  - le lore du plan 19 est écrit et la planche L1 validée (§78), rien n'est branché ;
  - la brume par biome est codée en dur dans `BiomeAtmosphere.GetFogDensity`.

La Bible demande l'inverse de cet état : « tragiquement beau », lumière dorée d'éternel début d'automne, ambiances distinctes par biome (§7.2, §7.3, §8, §11.4), et l'Effacement entendu comme des notes manquantes (§8.2).

## 2. Garde-fous communs

- Rien de global par image : ~10 000 décors par carte. Faune, particules, lumières et traces vivent autour du joueur, en pool, par index spatial (modèle `PropOcclusion`).
- Préférer les uniformes globaux dans les shaders existants (`ground`, `prop_forget`, `entity`) à une passe plein écran supplémentaire.
- Tout nouveau shader va dans `ShaderWarmup`.
- Réglages en JSON (`data/biomes/*.json`, nouveau `data/world/atmosphere.json`), jamais en dur.
- Effets au sol `ZIndex = -1`, couches de `AGENTS.md` respectées.
- Lots qui peuvent coûter (A3, B1, B2, B4, B5) : `/bench` avant/après au même commit de base, machine calme.
- Aucun retour du cycle jour/nuit (pivot V2) : une lumière par run (C6) reste fixe pendant toute la run.

## 3. Phase A — Lumière et son de base (≈ 6 à 8 j)

Le lot qui transforme le plus la capture de référence, pour un risque technique faible.

### A1 — Étalonnage par biome (1 j)

- Bloc `atmosphere` dans chaque `data/biomes/*.json` : teinte, saturation, contraste, densité et couleur de brume. `BiomeAtmosphere` lit ces valeurs à la place de `GetFogDensity`.
- Uniforme global d'étalonnage lu par `ground`, `prop_forget` et `entity` ; fondu de 1 à 2 s sur `PlayerBiomeChanged`. Le `CanvasModulate` garde la teinte de base.
- Intention, d'après la Bible : ville gris-beige froide, forêt verte et dorée, marais bleu-vert délavé, carrière brun-rouge, champs chauds et saturés.
- Vérification : captures par biome à la même graine, avant/après.

### A2 — Ombres de nuages (0,5 à 1 j)

- Bruit tileable échantillonné en coordonnées monde dans `ground` et dans les décors, qui défile dans la direction du vent (B1). Couverture, vitesse et opacité dans `atmosphere.json`, par biome (nulles au marais, couvert par la brume).
- Ne touche ni l'UI ni le Néant.
- Vérification : capture fixe et courte séquence ; banc si les décors en profitent.

### A3 — Ombres portées (2 à 3 j)

- Soleil bas d'automne, direction unique dans `atmosphere.json`. Les décors hauts (immeubles, arbres, silos, poteaux, éoliennes) projettent une ombre biaisée de leur silhouette, aplatie au sol à `ZIndex = -1`.
- Deux options : réutiliser la texture du décor avec un shader d'ombre (aucun nouvel asset, batching conservé), ou générer l'ombre dans le pipeline à partir de l'emprise du manifeste. Recommandation : la première, en étendant `PropShadow.cs`.
- Les petits décors gardent leur ombre de contact. Les ombres suivent l'oubli (délavées puis absentes en zone Effacée).
- Vérification : captures ville, forêt, ferme ; `/bench` (nombre de sprites dessinés).

### A4 — Ambiances sonores par biome et réverbération (2 à 2,5 j)

- Bloc `ambience` par biome : boucle de fond, sons ponctuels tirés au hasard avec intervalle, gain. `AudioManager` généralise le code de la forêt.
- Intentions de la Bible :
  - ville : vent entre les immeubles, craquements, tôle qui bat, aucun oiseau ;
  - marais : respiration, clapotis, bulles, murmures lointains ;
  - carrière : goutte-à-goutte, pierre qui roule, écho ;
  - champs : insectes, blé au vent, corbeaux lointains ;
  - Hub : feu, vent léger.
- Réverbération sur le bus World, dosage par biome en fondu (forte en carrière, étouffée au marais).
- Sources : planche d'écoute de candidats comme au plan 15 M4, choix de Raphaël.
- Vérification : `tools/record_run_audio.sh` avec `--audio-stems`, régression du mix, audit de banque.

### A5 — Météo en micro-événements (2 j)

- Quatre entrées dans `run_events.json`, dont le son est déjà branché :
  - brouillard épais : brume ×3 et portée de vue réduite ;
  - pluie de cendres : particules grises qui tombent et sol qui se ternit ;
  - vent de l'oubli : rafales visibles, feuilles et papiers, balancement accentué ;
  - orage : assombrissement, éclairs lointains réutilisant `IndicibleStorm`.
- Tirage par biome (pas de pluie de cendres en forêt profonde, par exemple).
- Vérification : `--event <id>` en capture, enregistrement audio.

## 4. Phase B — Un monde vivant et raconté (≈ 12 à 16 j)

### B1 — Vent commun et particules par biome (2 à 3 j)

- Vent global (direction, force, rafales) publié en uniforme : balancement étendu aux buissons, herbes hautes, linge, panneaux ; particules et nuages suivent la même direction.
- Particules manquantes, autour de la caméra seulement :
  - ville : cendres, papiers et journaux portés par le vent ;
  - forêt : feuilles qui tombent, rayons dorés entre les arbres ;
  - champs : pollen, graines ;
  - carrière : poussière qui tombe des parois.
- Données dans les blocs `atmosphere` (A1).
- Vérification : captures par biome ; `/bench`.

### B2 — Faune qui fuit (3 à 4 j)

- Bêtes non hostiles en pool de 30 à 40, apparues hors écran autour du joueur :
  - ville et champs : corbeaux et pigeons qui s'envolent à l'approche, rats qui filent le long des murs ;
  - forêt : papillons, lucioles dans les sous-bois ;
  - marais : grenouilles qui plongent, hérons.
- Signal de jeu : les bêtes fuient à l'opposé de l'Effacement, et une zone effacée est vide et silencieuse.
- Sprites par le pipeline `tools/sprites/creatures/`, animation courte (envol, course).
- Pas de collision, pas de `_Process` par bête : un seul gestionnaire (règle des rappels C#, plan 29).
- Vérification : capture, `/bench`, `tools/measure_run.sh` pour le nombre moyen à l'écran.

### B3 — Les rues racontent l'exode (3 à 5 j)

- Embouteillages figés sur les routes (files de voitures portières ouvertes), valises, poussettes, barricades, panneaux « ÉVACUATION » et flèches peintes, bus renversé, caddies.
- Placement par groupes cohérents le long des routes (comme les scènes-récits des champs), pas en semis uniforme.
- Sprites par `tools/generate_props.py urban`, manifeste d'emprise.
- Vérification : `--capture-props`, `--capture-map`, collisions.

### B4 — Fausses lumières (2 à 3 j)

- Sprites additifs plutôt que `PointLight2D` (coût en GL Compatibility) :
  - lampadaires qui grésillent encore ;
  - lampes orange qui clignotent à la carrière ;
  - une fenêtre éclairée de loin en loin ;
  - feux de camp éteints qui fument ;
  - lueurs violettes du marais.
- Elles s'éteignent avec l'oubli de leur cellule.
- Vérification : captures ; `/bench`.

### B5 — Traces persistantes du combat (1 à 2 j)

- Brûlures, impacts, éclats et taches irisées qui restent au sol en pool plafonné (les plus anciennes disparaissent d'abord), puis sont effacées par l'Effacement. Pas de cadavres (Bible).
- Vérification : capture après un combat dense ; `/bench`.

### B6 — Petits décors réactifs (2 à 3 j)

- Canettes qui roulent, feuilles soulevées par le dash, cartons qui s'écroulent, vitres qui éclatent sous un tir, sans collision bloquante ni impact sur le jeu.
- Vérification : capture ; banc si le nombre d'objets réactifs est élevé.

## 5. Phase C — L'identité de Vestiges (≈ 3 à 5 semaines, en partie hors code)

### C1 — Radios et téléphones abandonnés (2 j)

- Radio posée sur un capot ou un rebord : message d'évacuation en boucle, placé à l'écran, qui se dégrade mot à mot avec l'oubli de la zone.
- Téléphone qui sonne dans une cabine (le petit lieu existe) et se tait à l'approche.
- Voix : synthèse traitée ou enregistrement de Raphaël ; textes en français dans les traductions.

### C2 — Le son de l'oubli (3 à 4 j, plus les pistes de C3)

- Selon la phase de la zone et la distance au front : passe-bas, sons ponctuels qui sautent, ambiance qui s'éteint, voix incomplètes.
- Avec C3 : pistes musicales qui se coupent, notes manquantes (Bible §8.2).
- Vérification : enregistrement audible en traversant une zone effacée.

### C3 — Musique en couches (1 à 2 semaines, avec un compositeur ou une banque)

- Pistes séparées synchrones par morceau (piano, cordes pincées, bois, percussions), ouvertes selon l'intensité, le biome et l'Effacement. Remplace les fondus entre morceaux de `music.json`.

### C4 — Lore branché (3 à 5 j)

- Intégration de la planche L1 (plan 19) : fragments près des scènes-récits, plaques de rue et inscriptions dont les lettres s'effacent une à une dans l'Oubli.
- Recoupe le plan 19 et le point « fragment de lore près des scènes » du tableau de bord.

### C5 — Hub qui évolue (3 à 4 j)

- Objets rapportés au camp selon les Souvenirs obtenus, feu plus grand, silhouettes : plan 08 lot D, Bible §10.

### C6 — Lumière tirée par run (retiré)

- Proposé : aube froide, après-midi dorée, crépuscule cuivre ou ciel couvert, tiré au départ et fixe pour toute la run.
- **Refusé par Raphaël le 11 octobre** : une lumière au hasard par run n'est « pas le mieux ». Il reprendra peut-être un cycle de lumière, sans nuit trop noire ([DECISIONS §119 bis](DECISIONS.md)).

### C7 — Anomalies et lieux étranges (1 à 2 semaines)

- Plan 14 (Lieu qui se souvient, Écho de ta dernière run, Oubli de soi) et réactivation des anomalies de `scripts/World/Lore/` redessinées en pixel art, avec leurs sons (carillon, tic-tac).
- **Plan 14 non arbitré : rien sans l'accord explicite de Raphaël.**

## 6. Ordre recommandé

1. A1 → A2 → A3 → A4 → A5. A1 pose les données utilisées par tout le reste.
2. B1, puis B2, B3 et C1, qui rendent le monde habité puis perdu.
3. B4, B5, B6 selon le temps.
4. C3 et C2 dès que la question de la musique est tranchée ; C4 et C5 en parallèle du plan 19 et du plan 08.

## 7. Réponses de Raphaël (11 octobre)

Détail : [DECISIONS §119 bis](DECISIONS.md).

1. **Ordre :** phase A dans l'ordre, A1 → A5.
2. **Sources des ambiances (A4) :** candidats de banques libres dans une planche d'écoute, son choix.
3. **Météo (A5) :** ambiance, plus la portée de vue réduite par le brouillard épais.
4. **Lumière par run (C6) :** refusée ; C6 retiré. Un cycle de lumière sans vraie nuit reste une idée de Raphaël.
5. **Musique en couches (C3) :** plus tard, morceaux actuels gardés.
6. **Anomalies (C7) :** à réactiver après les phases A et B ; le plan 14 s'arbitre lot par lot.

## 8. A1 livré — 11 octobre 2026

**Fait :**

- `assets/shaders/world_grade.gdshaderinc` : teinte, saturation et contraste lus dans deux uniformes globaux (`world_grade_tint`, `world_grade_tone`, neutres par défaut dans `project.godot`). Inclus par `ground`, `path`, `prop_forget` (décors et herbes qui plient), `sway` (canopées) et `entity` (joueur et créatures).
- Étalonnage appliqué **avant** l'oubli : le blanc de l'Effacement reste identique dans tous les biomes.
- Entités : la moitié de l'étalonnage seulement (`entity_share`), et les yeux vert acide ne sont jamais touchés.
- Bloc `atmosphere` dans chaque `data/biomes/*.json` : `tint`, `tint_strength`, `exposure`, `saturation`, `contrast`, `fog_density`, `fog_color`. La teinte est ramenée à une luminance de 1 : elle colore sans assombrir. Bloc facultatif (neutre, sans brume), bornes contrôlées au chargement. `BiomeAtmosphere.GetFogDensity` et `GetFogColor` sont supprimés, la brume lit ces valeurs (mêmes réglages qu'avant).
- `data/world/atmosphere.json` (lu par `AtmosphereConfig`) : fondu de 1,5 s au changement de biome, part des entités. Fichier invalide : run sans étalonnage, avec un message.
- `BiomeAtmosphere` : premier biome appliqué d'un coup, les suivants en fondu depuis l'étalonnage affiché (un aller-retour sur une lisière ne saute pas). Uniformes publiés seulement pendant un fondu ; remis au neutre en quittant la run.

**Réglages initiaux** (proposés par l'agent d'après la Bible, à juger en jouant) :

| Biome | Intention | Teinte (force) | Saturation | Contraste |
|---|---|---|---|---|
| Ville | gris-beige froid | bleutée (0,5) | 0,62 | 1,06 |
| Forêt | verte et dorée | dorée (0,45), exposition 1,06 | 0,9 | 1,04 |
| Marais | bleu-vert délavé | bleu-vert (0,45) | 0,62 | 0,88 |
| Carrière | brun-rouge | rouille (0,38) | 0,9 | 1,08 |
| Champs | chauds | dorée (0,35) | 1,0 | 1,03 |

**Vérifié :**

- Captures `--capture-props --hide-collisions`, seed 1002, neutre puis étalonné, regardées : ville plus froide et grise (toits de brique ternis), forêt plus dorée, marais délavé bleu-vert, carrière qui tire vers la rouille sans perdre ses pierres, champs plus dorés et moins acides. Joueur lisible dans les cinq biomes. Planche : `doc/plans/planches/39-A1-etalonnage.jpg` (non versionnée).
- `tools/test_catalogs.sh` : 213 contrôles, dont les refus du bloc `atmosphere` et de `atmosphere.json`.
- `dotnet build` 0 warning, `tools/smoke_test.sh` vert (Hub neutre).
- `--capture-props` attend désormais 2,2 s par biome au lieu de 20 images : le fondu n'était pas terminé, et la première vue tombait sur l'écran de chargement.

**Non mesuré :** coût GPU. Une dizaine d'opérations par pixel sur le sol, les décors et les entités, sans lecture de texture ; A1 n'est pas dans la liste des lots à banc (§2). À recouper au banc d'A3.

**Points ouverts :**

- L'étalonnage suit le biome du joueur, pas celui de chaque pixel : sur une lisière, le biome voisin prend la couleur du biome où l'on se tient. Un étalonnage par pixel est possible pour le sol (le shader connaît le biome de chaque case), pas pour les décors sans coût.
- Coffres, Mémoriaux, projectiles et effets ne sont pas étalonnés : ils ressortent un peu plus, ce qui sert la lisibilité.

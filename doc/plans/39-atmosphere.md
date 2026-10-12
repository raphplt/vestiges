# Plan 39 — Atmosphère et détail du monde

Demande de Raphaël, 11 octobre 2026 : « Comment rajouter plus d'atmosphère et de détail au jeu ? (pense large/vue d'ensemble) », puis « crée le plan ». Questions du §7 tranchées le même soir ([DECISIONS §119 bis](DECISIONS.md)) ; **phase A livrée** (§8 à §13).

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

**Ajusté le même soir** (retour de Raphaël, [DECISIONS §119 ter](DECISIONS.md)) : passage d'un biome à l'autre jugé un peu fort. Tous les étalonnages sont dosés à 65 % par `grade.strength` (écart au neutre ; les réglages de chaque biome restent la référence) et le fondu passe à 2,5 s.

## 9. A2 livré — 11 octobre 2026

**Fait :**

- `cloud_shade` dans `world_grade.gdshaderinc` : bruit de valeur à deux octaves lu en coordonnées monde, repère au sol (un nuage rond fait une ombre deux fois plus large que haute), seuil par la couverture du biome. Quatre paliers tramés au pixel (Bayer 2×2) : la lisière reste du pixel art. L'ombre baisse plus le rouge et le vert que le bleu.
- Appliqué au sol, aux routes, aux décors, aux herbes et aux canopées, au point du pixel dessiné ; aux entités à la même part que l'étalonnage. Avant l'oubli : le blanc de l'Effacement n'est pas ombré. Aucun coût sans nuages (Hub, marais) : test sur un uniforme.
- Réglages : sens, vitesse et taille dans `data/world/atmosphere.json` (`clouds` : 20°, 12 px/s, 420 px) ; couverture et noirceur par biome (`cloud_cover`, `cloud_opacity`, au plus 0,6) dans le bloc `atmosphere`, fondues avec l'étalonnage. Marais sans nuages (la brume les couvre).
- `BiomeAtmosphere` cumule le défilement et publie `world_clouds` à chaque image tant que le biome a des nuages (un `Vector4`, sans allocation).

| Biome | Couverture | Noirceur |
|---|---|---|
| Ville | 0,5 | 0,32 |
| Forêt | 0,45 | 0,26 |
| Marais | 0 | 0 |
| Carrière | 0,45 | 0,28 |
| Champs | 0,55 | 0,34 |

Le sens du vent (20°) est provisoire : B1 le reprendra comme vent commun.

**Vérifié :**

- Captures `--capture-props --hide-collisions`, seed 1002, regardées : dans les champs, deux grandes ombres traversent le blé, les haies et les clôtures sans couture entre sol et décors ; plus discrètes en ville et en forêt. Premier jet trop doux (taille 560 px, bord large, noirceur 0,22–0,3) : ombres à peine visibles, resserrées.
- `tools/test_catalogs.sh` : nouveaux refus (noirceur, couverture absente, force, taille, vitesse, section `clouds` absente) verts.
- `dotnet build` 0 warning, `tools/smoke_test.sh` vert, aucune erreur de shader au journal de run.

**Non vérifié :** le défilement en mouvement (captures fixes seulement).

**Banc du 12 octobre** (`tools/bench_ab.sh`, base = HEAD sans A2, 2 passes valides) : 1080p 439 → 424 FPS (−0,08 ms), 720p 470 → 479 FPS. Les passes varient de ±10 % entre elles : pas de coût mesurable.

## 10. A3 livré — 12 octobre 2026

**Fait :**

- `PropShadow.CreateCast` : un second sprite par grand décor (et un par canopée), même texture, retourné et cisaillé par sa transformation. Un point à `h` px au-dessus du pied du décor tombe au sol à `h × (dx, dy)`. Le découpage à l'écran suit l'ombre réelle, sans shader de sommet ni nouvel asset.
- `prop_cast_shadow.gdshader`, un seul matériau partagé : couleur d'ombre unie, opacité du biome (fondue avec l'étalonnage), effacée sous un nuage (lumière diffuse) et dans les zones oubliées. Ajouté à `ShaderWarmup`.
- Réglages : `cast_shadows` dans `data/world/atmosphere.json` (direction au sol −35°, longueur 1,1 px par px de hauteur, hauteur minimale 40 px, couleur bleu nuit) ; `shadow_opacity` par biome (0,2 au marais, 0,45 à 0,5 ailleurs). Les petits décors gardent leur ombre de contact.

**Écart au plan :** l'ombre ne tombe pas « de côté » sous un soleil rasant. Un sprite vu de face, projeté de côté, ne donne qu'un trait (premier essai à 25° : liserés de 5 px au pied des décors). L'ombre part donc vers l'arrière et la droite, comme dans la plupart des jeux 2D en vue de dessus.

**Vérifié :**

- Captures `--capture-props --hide-collisions`, seed 1002, regardées : longues ombres d'arbres vers la droite en forêt, poteau, tracteur et clôtures dans les champs, réverbères de la carrière, immeubles et panneaux en ville. Sous un nuage, les ombres s'effacent. Planche : `doc/plans/planches/39-A3-ombres.jpg` (non versionnée).
- `tools/test_catalogs.sh` vert (opacité, longueur, hauteur, couleur refusées hors bornes), `dotnet build` 0 warning, `tools/smoke_test.sh` vert, aucune erreur de shader en run.

**Coût** (`tools/bench_ab.sh`, base HEAD, 3 passes, 1080p) : 4 943 sprites d'ombre sur la carte du banc (72 961 → 77 904 nœuds). Appels de dessin 304 → 306 : les ombres partagent le matériau et suivent les décors en lots. Passe 1, machine calme : 2,33 → 2,40 ms par image (+0,07 ms, +3 %), rendu CPU 1,05 → 1,11 ms, GPU inchangé (1,11 ms). La charge est montée à 8,5 pendant les passes 2 et 3 : médianes faussées (−14 %), non retenues. À remesurer machine calme.

**Points ouverts :**

- Un décor rendu transparent par `PropOcclusion` laisse voir son ombre au travers.
- Deux ombres qui se chevauchent foncent un peu plus (tronc sous canopée, immeubles voisins).
- 5 000 nœuds de plus : si le banc calme confirme un coût, regrouper les ombres par îlot ou les cuire dans une couche au sol.

## 11. A4 — découpage et A4a livré — 12 octobre 2026

**Découpage :**

- **A4a — socle (livré).** Ambiance par biome en données, réverbération du bus des sons du monde. Ne dépend pas du choix des sons.
- **A4b — planche d'écoute.** Candidats CC0 (Kenney, BigSoundBank, comme au plan 15 M3) pour chaque biome : fond de ville, marais, carrière, champs, Hub ; sons ponctuels (tôle, craquements, clapotis, bulles, pierre qui roule, goutte, insectes, corbeaux). Page d'écoute hors dépôt, export des choix. Rien n'est intégré avant le choix de Raphaël.
- **A4c — intégration.** Sons retenus dans la banque et les blocs `ambience`, fondu enchaîné entre deux fonds (aujourd'hui, un changement de fond coupe net : un seul fond existait), enregistrement audible en traversant les cinq biomes.

**A4a fait :**

- Bloc `ambience` dans `data/biomes/*.json`, lu par `BiomeDataLoader` (`BiomeAmbience`) : `loop`, `accents`, `first_accent_s`, `accent_interval_s`, `accent_volume_db`, `reverb` (`wet` ≤ 0,5, `room_size`, `damping`). Clés de son contrôlées dans la banque au chargement. Sans bloc : silence et son sec.
- `AudioManager` ne connaît plus la forêt : le fond et les sons ponctuels viennent du biome courant. La forêt garde exactement ses réglages (fond à −2 dB, oiseaux à −4 dB, premier entre 5 et 15 s puis toutes les 20 à 45 s). Le champ `biome` de `sounds.json`, seul lien précédent, est retiré.
- `AudioEffectReverb` sur le bus World, en fondu de 2 s au changement de biome, coupée une fois revenue au sec et remise à zéro en fin de run. Réglages initiaux, **non écoutés par l'agent** : carrière `wet` 0,15, ville 0,08, autres biomes secs. Le battement de cœur de santé basse et l'alerte de bord passent aussi par ce bus : ils prennent la même réverbération.

**Vérifié :** `tools/test_audio.sh` vert, avec 4 nouveaux contrôles de réverbération (entrée en fondu, niveau complet, retour au sec et effet coupé, rien au Hub) ; les contrôles existants de la forêt passent sans changement de comportement (`_birdTimer` renommé `_accentTimer`, régression mise à jour). `tools/test_catalogs.sh` vert (4 refus du bloc `ambience`), `dotnet build` 0 warning, `tools/smoke_test.sh` vert.

**À écouter :** la réverbération de la carrière et de la ville, sur les coups et les cris.

### A4b livré — planche d'écoute, 12 octobre 2026

`~/.local/share/vestiges-audio/2026-10-12/ambiances/index.html`, hors dépôt : 13 besoins, 39 candidats, choix gardés dans le navigateur et export `choix-ambiances-2026-10-12.json`.

- **Fonds en boucle** (24 s, fondu enchaîné de 1,5 s à la reprise) : ville (vent entre les immeubles), marais (tourbières, mare, grenouilles), carrière (grottes, vent en volume clos), champs (vent dans les cultures, insectes et chaleur), Hub (feu de cheminée ou de branches, pour A4c).
- **Sons ponctuels** : métal qui grince ou bat et chute lointaine (ville), bulles et grenouille isolée (marais), goutte-à-goutte et pierre qui roule (carrière), corbeaux et insecte (champs).
- Sources : BigSoundBank, Joseph SARDIN, CC0. La page de chaque prise est archivée dans `sources/` avec l'empreinte du fichier. `prepare.py` régénère tout ; `manifest.json` garde source, licence, point de coupe et empreintes par candidat.
- Découpes faites **par mesure, sans écoute** : la fenêtre de fond la plus régulière (crêtes pénalisées), l'événement ponctuel le plus net, prolongé tant qu'il reprend dans les 0,3 s. Une voix ou un choc peut s'y être glissé ; la page invite à le signaler.
- Niveaux : fonds à −26 dB RMS (grottes à −28 et −30, deux feux à −33 : crépitements tout en crêtes), sons ponctuels à −20 dB RMS au plus, crête ≤ −6 dBFS partout. Un doublon voulu : la même chute de pierres sert à la ville et à la carrière.

**Suite :** Raphaël écoute et exporte ses choix ; A4c les intègre (banque, blocs `ambience`, fondu enchaîné entre fonds, ambiance du Hub).

## 12. A5 livré — 12 octobre 2026

**Fait :**

- `World/WeatherDirector` : une météo à la fois, tirée selon le biome où se trouve le joueur, avec ses poids dans `data/world/weather.json` (pas de cendres en forêt, brouillard surtout au marais). Première entre 2:30 et 4:00, puis une pause de 2:30 à 4:00 après chaque fin. Flux de tirage à part (`RunRandom.Create("weather")`). Suspendue pendant un boss : l'Indicible garde sa nuit. Signal `WeatherChanged`.
- **À part des micro-événements** (écart au plan, qui parlait d'entrées dans `run_events.json`) : la météo ne prend pas le créneau d'un événement de jeu et peut s'y superposer. Dans `run_events.json`, elle aurait raréfié les Souverains, la Harde, la Veille.
- Effets sur les couches A1–A3, en fondu propre à chaque météo : facteurs d'étalonnage, minimum de nuages et vitesse de leur défilement, opacité des ombres portées. `BiomeAtmosphere` compose météo et biome, si bien qu'un changement de biome pendant l'orage garde l'orage.
- `World/WeatherVisuals` :
  - brouillard épais : un voile à l'écran (`weather_view.gdshader`, couche 4 sous le HUD), net jusqu'à 170 px au sol autour du joueur et plein à 360 px, opacité 0,82 ; lisière qui dérive, tramée au pixel. C'est la **portée de vue réduite** demandée : silhouettes devinables, créatures lointaines cachées.
  - particules autour de la caméra, émises dans le monde : flocons de cendre, traits de débris au vent, pluie inclinée.
- Orage : ciel assombri (exposition × 0,74), nuages lourds, pluie, éclairs lointains toutes les 6 à 14 s par sursaut d'exposition. On ne réutilise pas `IndicibleStorm` : il tient le `CanvasModulate` et sa nuit appartient au boss.
- Son : `AudioManager` joue la boucle de la météo (`sfx_brouillard`, `sfx_pluie_legere`, `sfx_foret_rafales`, `sfx_orage_proche`, déjà dans la banque) sur `WeatherChanged`. L'ancien branchement sur `RandomEventTriggered` ne pouvait pas se déclencher : aucun micro-événement ne porte ces identifiants.
- `RunObservation --weather <id>` : météo imposée à 3 s, pour les captures.

**Vérifié :**

- Captures des quatre météos (seed 1002), regardées : voile de brouillard (premier essai invisible, le rectangle restait à taille nulle ; corrigé), cendres et palette ternie, débris au vent, pluie sous un ciel sombre. Planche : `doc/plans/planches/39-A5-meteo.jpg` (non versionnée).
- Tirage naturel (`tools/measure_run.sh`, 2 graines × 600 s) : quatre météos, chacune dans un biome qui l'autorise.
- `tools/test_catalogs.sh` vert (biomes de la météo existants, six refus), `tools/test_audio.sh` vert (régression passée de `RandomEventTriggered` à `WeatherChanged`), `dotnet build` 0 warning, `tools/smoke_test.sh` vert.

**Relecture (`godot-reviewer`), corrigée :** un émetteur de particules pouvait n'être jamais libéré quand deux météos se suivaient dans le même fondu, et l'enchaînement des fondus doublait l'arrivée des nouvelles particules (un tween par émetteur désormais) ; paramètres du voile en `StringName` en cache ; dernière publication sans sursaut quand un éclair retombe ; particules ramenées en couche 15, sous les plaques d'ennemis (20) et les chiffres de dégâts (30). Orage recapturé après correction.

**Non vérifié :** les éclairs et le défilement en mouvement (captures fixes), le son des météos en jeu, le coût au banc (voile plein écran seulement pendant le brouillard, 110 à 220 particules).

**Constat (corrigé au §13 à la demande de Raphaël) :** le voile du marais (`BiomeAtmosphere.CreateSwampOverlay`, `swamp_atmosphere.gdshader` : brume laiteuse, reflets spectraux) pose son ancrage comme le premier voile de brouillard. Mesuré le 12 octobre : rectangle de taille (0, 0) avec une intensité de 1 en plein marais. **Il n'a jamais été visible.** Le corriger (`SetAnchorsAndOffsetsPreset`) changerait d'un coup l'allure du marais et ajouterait une passe plein écran : non fait sans son avis.

## 13. A4c livré et voile du marais réaffiché — 12 octobre 2026

**Choix de Raphaël** ([DECISIONS §124](DECISIONS.md)) intégrés par `~/.local/share/vestiges-audio/2026-10-12/ambiances/integrate.py` (hors dépôt, reproductible) :

| Biome | Fond | Sons ponctuels |
|---|---|---|
| Ville | vent fort dans un village (second tour) | bâche qui claque, barre de fer qui tombe, chute de pierres |
| Marais | mare dans un champ | bulles (deux fois plus fréquentes), grenouille |
| Carrière | grotte ou vent en volume clos, l'un ou l'autre (ajusté, voir plus bas) | goutte (deux fois plus fréquente), chute de pierres |
| Champs | soleil et chaleur | corbeau, grillon |
| Forêt | inchangée | oiseaux |
| Hub | grand feu de branches | — |

- Fichiers dans `assets/audio/sfx/ambiance/` (44,1 kHz, boucles de 24 s stéréo, ponctuels mono, silence de tête coupé pour l'audit de banque), crédits CC0 par fichier dans `assets/audio/CREDITS.json`.
- Niveaux calés sur l'existant : fonds à −14 dB dans la banque (≈ −40 dB effectifs, comme la forêt), sons ponctuels à −10 dB dans les blocs `ambience` (≈ −30, comme les oiseaux).
- **Fondu enchaîné** entre deux fonds : l'ancien passe à un lecteur sortant (`AmbianceOut`), à sa position et à son niveau, et s'éteint en 2 s pendant que le nouveau monte. Avant, un changement de fond coupait net.
- **Hub** : `HubScreen` lance le feu sous la musique (`AudioManager.PlayHubAmbiance`) ; il s'éteint en 1,5 s au départ en run.
- **Fond de ville, second tour** : `ville-r2.html` à côté de la planche. A = l'ancien A passé au passe-haut à 250 Hz (circulation retirée), B à D = trois nouvelles prises de vent (sifflement 2, vent d'intérieur 3, vent fort dans un village).

**Voile du marais** : ancrage corrigé (`SetAnchorsAndOffsetsPreset`). Réaffiché, il relisait l'écran sur la grille de l'ancienne résolution interne (480 × 270) et pixelisait tout le marais deux fois trop gros ; la grille suit maintenant la vue réelle (960 × 540 px monde). Il s'ajoutait aussi à l'étalonnage d'A1 : force ramenée à 0,6 (`swamp_veil.strength`, `data/world/atmosphere.json`). Capture avant/après regardée : brume laiteuse légère, détails nets, léger reflet sur le joueur qui reste lisible.

**Vérifié :** `tools/test_audio.sh` vert. La régression d'ambiance est réécrite : elle supposait qu'un fond qui joue était la forêt ; elle lit maintenant la clé du fond. S'y ajoutent les contrôles du fondu enchaîné forêt → marais, de la ville sans fond, du Hub et de son extinction au départ ; l'audit de banque passe après la coupe du silence de tête. `tools/test_catalogs.sh`, `dotnet build` 0 warning, `tools/smoke_test.sh` vert (imports des nouveaux WAV).

**Non vérifié :** l'écoute en jeu (rien n'a été écouté par l'agent), le coût du voile du marais au banc (passe plein écran avec lecture de l'écran, seulement dans le marais).

**Ajusté le même jour** ([DECISIONS §125](DECISIONS.md)) : la carrière ne superpose plus ses deux fonds. Le bloc `ambience` prend une liste `loops` ; un seul fond joue, tiré à chaque entrée dans le biome (grotte ou vent en volume clos). La ville prend le vent fort dans un village (second tour, D). Contrôles ajoutés : un seul fond de carrière à la fois, le vent de la ville remplace la carrière en fondu. `tools/test_audio.sh` et `tools/test_catalogs.sh` verts, smoke vert.

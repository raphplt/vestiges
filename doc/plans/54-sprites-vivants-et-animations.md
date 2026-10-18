# Plan 54 — Sprites vivants, refonte du pipeline SDF 2.0 et dynamique d'animation en jeu

17 octobre 2026 · Demandé par Raphaël : améliorer le niveau de détail, le caractère et les animations des personnages jouables et des ennemis, en combinant une refonte ciblée du générateur procédural Python (`tools/sprites/`) et des améliorations d'animation dynamique côté Godot, validées pas à pas en commençant par un duo pilote (**Le Vagabond** + **Le Rôdeur**).

---

## 1. Diagnostic technique de l'existant

L'audit de [`tools/sprites/`](../../tools/sprites/) et du rendu en jeu ([`Player.cs`](../../scripts/Core/Player.cs), [`Enemy.cs`](../../scripts/Combat/Enemy.cs)) met en évidence quatre verrous côté génération et trois manques côté moteur :

### 1.1 Côté générateur Python (`tools/sprites/`)
1. **Surfaces trop lisses (« effet plastique »)** :
   - [`render.py`](../../tools/sprites/render.py) quantifie l'éclairage diffus sur 4 paliers fixes (`SHADE_THRESHOLDS`), puis `_clean_orphans()` supprime systématiquement les pixels isolés.
   - Tous les matériaux ([`palette.py`](../../tools/sprites/palette.py)) partagent la même loi de réflexion mate : la laine rapiécée, le cuir, la pierre, la chair et l'acier ont la même texture d'aplat. Aucun dithering de transition ([`CHARTE-GRAPHIQUE.md` §5](../CHARTE-GRAPHIQUE.md#L284-L289)) ni éclat spéculaire n'est produit.
2. **Volumes en « capsules empilées » manquant de caractère** :
   - [`sdf.py`](../../tools/sprites/sdf.py) n'offre que 5 primitives rondes (`sphere`, `capsule`, `ellipsoid`, `rounded_box`, `cylinder`) combinées par union dure (`d < best`).
   - Il manque une fusion organique (`smin`) pour les épaules, muscles et bosses des créatures, des primitives biseautées/pincées (`tapered_box`, `cone_arch`, plis de tissu) pour créer des angles francs lisibles à 30 px de haut, et une courbure des membres dans [`_body.py`](../../tools/sprites/characters/_body.py).
3. **Animations trigonométriques uniformes (« effet automate »)** :
   - Dans [`poses.py`](../../tools/sprites/poses.py), `playable_animations()` pilote la marche par un `np.sin(phase)` symétrique.
   - Il n'y a ni *squash & stretch* sur le squelette ([`rig.py`](../../tools/sprites/rig.py)), ni asymétrie de timing (impact lourd vs passage rapide), ni *smear* (traînée d'attaque/dash), et un seul scalaire `drape` pilote tous les éléments souples en même temps.
4. **Postures trop génériques par personnage et ennemi** :
   - Les personnages balancent les deux bras dans le vide pendant la marche au lieu d'interagir avec leur équipement signature (bâton/sangle du Vagabond, manche du marteau de la Forgeuse, arc du Traqueur, sacoche du Facteur, lanterne/Essence de l'Éveillée, scaphandre lourd).

### 1.2 Côté Godot (Rendu et animation en temps réel)
1. **Glissement rigide du sprite joueur** : [`Player.cs`](../../scripts/Core/Player.cs) joue l'`AnimatedSprite2D` sans inclinaison dans les virages (*lean*), ni rebond d'échelle aux pas ou à la réception du dash.
2. **Déconnexion entre le corps du joueur et ses attaques** : [`PlayerAttackFx.cs`](../../scripts/Combat/PlayerAttackFx.cs) dessine les arcs et projectiles, mais le sprite du personnage ne donne aucune impulsion (*lunge* / *kickback*) quand une arme frappe ou tire.
3. **Traînées de dash génériques et télégraphes ennemis discrets** : [`MobilityFeedback.cs`](../../scripts/Combat/MobilityFeedback.cs) affiche une forme générique au lieu de la silhouette exacte de la frame courante du personnage teintée à sa couleur d'accent.

---

## 2. Architecture des solutions

### Volet A — Pipeline Python SDF 2.0 (`tools/sprites/`)

1. **Matière, micro-relief 3D, spéculaire et dithering ([`palette.py`](../../tools/sprites/palette.py) & [`render.py`](../../tools/sprites/render.py))** :
   - Enrichir `Material` avec des paramètres optionnels rétrocompatibles :
     - `roughness: float = 0.0` : bruit 3D triplanaire **ancré dans l'espace local de chaque `Part` ou du modèle** (pour qu'il suive le volume sans clignoter d'une frame à l'autre), donnant du grain au cuir, à la pierre, à la mousse et aux vêtements.
     - `specular: float = 0.0` : reflet spéculaire serré (`half_vector @ normal`) qui force le ton 3 (highlight) sur les arêtes du métal, du verre, des optiques et de la chitine.
     - `dither: float = 0.0` : tramage damier `2×2` (`(x + y) & 1`) uniquement sur la bande de transition entre deux seuils de `SHADE_THRESHOLDS` (conforme à la charte §5 : jamais sur les aplats unis).
     - `rim: float = 0.0` : léger rehaussement de ton sur les normales rasantes opposées à la lumière pour découper les silhouettes sombres.
   - Assouplir `_clean_orphans()` pour préserver les pixels de dithering et les éclats spéculaires volontaires.

2. **Nouvelles primitives SDF et opérateurs de sculpture ([`sdf.py`](../../tools/sprites/sdf.py) & [`_body.py`](../../tools/sprites/characters/_body.py))** :
   - Opérateurs :
     - `smin(d1, d2, k)` : union organique lisse (épaules, jonction cou/torse, masses musculaires et bosses du bestiaire).
     - `smax(d1, -d2, k)` : soustraction douce (creux d'orbites, déchirures de cape, cicatrices, gueules).
     - ` cloth_folds(p, axis, freq, amp)` : ondulation spatiale pour sculpter de vrais pans de manteau, tabliers et écharpes.
   - Primitives :
     - `tapered_box(p, a, b, width_a, width_b, depth_a, depth_b, rounding)` : volume trapézoïdal orienté le long d'un segment (idéal pour bottes à revers, avant-bras gantés, pans de veste, lames, plaques rocheuses).
     - `arc_smear(p, center, normal, radius, thickness, angle_start, angle_end)` : croissant de mouvement pour les frames d'attaque ennemie et de dash.
   - Dans [`_body.py`](../../tools/sprites/characters/_body.py) : remplacer les cylindres uniformes par des membres galbés (épaule fondue, revers de manche/gant marqué, pli de genou, botte structurée avec semelle et coup-de-pied).

3. **Squelette expressif : *Squash & Stretch* et inertie multi-axes ([`rig.py`](../../tools/sprites/rig.py))** :
   - Ajouter à `Pose` et `Skeleton` :
     - `stretch: float = 0.0` : facteur de *squash & stretch* conservant le volume (`scale_y = 1 + stretch`, `scale_xz = 1 / sqrt(1 + stretch)`) appliqué autour du pivot des pieds.
     - `drape_lift: float = 0.0` et `drape_sway: float = 0.0` en complément de `drape` : permettent aux éléments souples (sac, écharpe, capuche, breloques, marteau) de se soulever quand le corps tombe et de fouetter latéralement avec 1 à 2 frames de retard.
     - `shoulder_shrug_l / r` et orientation des mains pour permettre de tenir un accessoire ou une arme de route.

4. **Refonte des animations et postures de caractère ([`poses.py`](../../tools/sprites/poses.py), personnages & créatures)** :
   - **Cycle de marche asymétrique (8 frames)** :
     - Frame 1 (Contact lourd) : `stretch = -0.07` (écrasement d'absorption), `bob` bas, `drape_lift` encore haut.
     - Frame 2 (Écrasement / transfert) : flexion maximale du genou avant, retard maximal des tissus.
     - Frame 3 (Poussée / Propulsion) : `stretch = +0.06`, extension franche de la jambe arrière, buste engagé.
     - Frame 4 (Suspension haute) : sommet du rebond avant le contact opposé (Frames 5–8 miroir).
   - **Cycle de Dash (4 frames)** :
     - Frame 1 : compression forte (`stretch = -0.12`, recul d'élan) ;
     - Frame 2 : détente explosive (`stretch = +0.10`, buste très projeté, tissus à l'horizontale) ;
     - Frame 3 : vol rasant groupé ;
     - Frame 4 : réception amortie.
   - **Cycles ennemis (Idle nerveux, Marche prédatrice, Attaque en 4 temps)** :
     - Frame 1 d'attaque : vraie **anticipation** (recul + compression + torsion) ;
     - Frame 2 : **frappe avec *smear*** (détente maximale + croissant d'attaque/griffes étirées) ;
     - Frame 3 : **impact / dépassement** (*follow-through*) ;
     - Frame 4 : **récupération** lourde.

### Volet B — Dynamique et Juice côté Godot (`scripts/Core/`, `scripts/Combat/`, `assets/shaders/`)

1. **Dynamique corporelle du joueur ([`Player.cs`](../../scripts/Core/Player.cs))** :
   - **Inclinaison directionnelle (*Velocity Lean*)** : légère rotation visuelle du `_sprite` (±3,5° max) proportionnelle au changement de cap et à la vitesse latérale, lissée sans allocation.
   - **Micro-rebond de foulée et réception de dash** : modulation douce de `_sprite.Scale` autour de `Vector2.One` (±5 %) synchronisée sur les pas et l'atterrissage du dash.
   - **Impulsion d'attaque (*Attack Lunge / Kickback*)** dans [`PlayerAttackFx.cs`](../../scripts/Combat/PlayerAttackFx.cs) : décalage élastique court (2–3 px pendant 80 ms) du `_sprite` vers la cible sur une frappe de mêlée ou en léger recul sur un tir lourd, sans déplacer la collision physique.
2. **Silhouettes rémanentes fidèles au personnage ([`MobilityFeedback.cs`](../../scripts/Combat/MobilityFeedback.cs))** :
   - Lors du dash ou de l'Élan, copier la `Texture2D` de la frame exacte courante de l'`AnimatedSprite2D` du joueur sur les sprites de traînée `_trails[i]`, teintée avec la couleur d'accent identitaire du personnage.
3. **Télégraphes vivants et réactions d'impact des ennemis ([`Enemy.cs`](../../scripts/Combat/Enemy.cs), [`HitFeedback.cs`](../../scripts/Combat/HitFeedback.cs))** :
   - Pendant la fenêtre d'annonce d'attaque (`SetWindupPose`), ajouter une micro-vibration horizontale (`±1 px`) et un écrasement préparatoire avant la détente.
   - Dans [`HitFeedback.cs`](../../scripts/Combat/HitFeedback.cs), orienter l'axe du *squash* selon la direction du coup reçu.

---

## 3. Découpage en lots (Validation progressive)

Conformément aux règles du projet, nous avançons **un lot à la fois**, en commençant par valider visuellement le moteur sur **un seul personnage (Le Vagabond)** et **un seul ennemi (Le Rôdeur)** avant de décliner sur le reste du casting.

- [x] **Lot S0 — Moteur SDF 2.0 + Prototype Avant/Après sur Le Vagabond et Le Rôdeur (`--dry-run`)**
  - Implémenter les évolutions de [`palette.py`](../../tools/sprites/palette.py), [`render.py`](../../tools/sprites/render.py), [`sdf.py`](../../tools/sprites/sdf.py), [`rig.py`](../../tools/sprites/rig.py), [`poses.py`](../../tools/sprites/poses.py) et [`_body.py`](../../tools/sprites/characters/_body.py).
  - Enrichir le modèle et les animations du **Vagabond** ([`vagabond.py`](../../tools/sprites/characters/vagabond.py) : plis du manteau, bottes et gants sculptés, grain du cuir/tissu, bâton de marche / tenue de bretelle, cycle de marche et dash avec *squash & stretch* et inertie du sac/écharpe).
  - Enrichir le modèle et les animations du **Rôdeur** ([`rodeur.py`](../../tools/sprites/creatures/rodeur.py) : fusion organique `smin` de la bosse et des épaules, grain minéral/mousse, côtes/griffes affûtées, démarche boiteuse asymétrique, attaque avec anticipation recroquevillée + *smear* de griffes).
  - Générer en `--dry-run` (sans modifier `assets/`) une planche comparative **Avant / Après** (taille réelle ×1 et zoom ×4, sur sols de biomes) dans `doc/plans/planches/plan54_lot_S0_vagabond_rodeur.png`.
  - **Arrêt pour validation visuelle avec Raphaël.**

- [x] **Lot S1 — Intégration en jeu du duo pilote (Vagabond + Rôdeur) et règles de netteté à 30 px**
  - Correction de la perte de netteté observée sur le premier jet du Lot S0 :
    1. **Primitives symétriques en rotation (`capsule` / `sphere`)** sur les membres orientables plutôt que `tapered_box` aligné sur les axes monde (qui créait un crénelage en escalier sur les 8 angles de lacet).
    2. **Pas de micro-accessoires de 1 px sur le torse de 6×6 px** (ceinture, boucle, fente sombre, côtes 1 px) qui fragmentaient les aplats en bruit et déclenchaient des lignes internes (`inner_line`) parasites.
    3. **Pas de matériau `emissive` hors yeux** (une lanterne `emissive` court-circuite `inner_line` et bave sur les matériaux voisins dès 25 % de couverture).
    4. **`stretch = 0.0` dans `poses.py`** pour éviter tout décalage sous-pixel des yeux et du bord du chapeau d'une frame à l'autre ; le *squash & stretch* passe par l'articulation franche des poses-clés et par `AnimatedSprite2D.Scale` en jeu (`PlayerAttackFx`).
  - Export des PNG finaux de `vagabond` et `rodeur` dans `assets/characters/vagabond/` et `assets/enemies/rodeur/` et validation `tools/smoke_test.sh`.

- [x] **Lot G1 — Améliorations Godot Runtime sur le Joueur (Lean, Rebond, Impulsion d'attaque, Pas synchronisés)**
  - Implémenté dans [`Player.cs`](../../scripts/Core/Player.cs) et [`PlayerAttackFx.cs`](../../scripts/Combat/PlayerAttackFx.cs) : inclinaison dynamique selon la vélocité (`±3.1°`), `PlayStepSquash` à la réception de dash et à l'arrêt de course, `LungeOffset` directionnel sur les frappes mêlée/tir, et synchronisation exacte des poussières/sons de pas sur les frames d'impact (`0` et `4`) du cycle `Walk` à 8 frames.

- [x] **Lot G2 — Améliorations Godot Runtime sur les Ennemis (Désynchronisation d'Idle et Tempo de marche lié à la vitesse)**
  - Implémenté dans [`Enemy.cs`](../../scripts/Combat/Enemy.cs) : phase initiale aléatoire sur `Idle` pour éviter que les meutes respirent à l'unisson, et modulation du `SpeedScale` de `Walk` par `Velocity.Length() / _baseSpeed` (`[0.72, 1.45]`).

- [x] **Lot S2 — Déclinaison SDF 2.0 aux 5 autres personnages jouables + retouches Aseprite optionnelles**
  - Appliquer les nouveaux matériaux, membres sculptés, accessoires tenus et allures propres à **La Forgeuse**, **Le Traqueur**, **Le Facteur**, **L'Éveillée** et **La Scaphandrière** (`tools/sprites/characters/*.py`).
  - Planche comparative de contrôle pour validation, puis export et test avec `tools/capture_run.sh`.

- [x] **Lot G3 — Traînées fidèles, télégraphes vivants et coups orientés (Godot)**
  - [`MobilityFeedback.cs`](../../scripts/Combat/MobilityFeedback.cs) : chaque traînée du dash (et du saut instantané) reprend la frame exacte du sprite joueur (animation, image, retournement, hauteur), en silhouette pleine à la couleur d'accent du personnage (`accent_color` dans `data/characters/characters.json`) par un petit shader préchauffé au chargement ; repli sur les formes génériques pour un personnage sans sprite.
  - [`Enemy.cs`](../../scripts/Combat/Enemy.cs) + nouveau `WindupFeedback` : pendant l'annonce d'attaque, vibration horizontale d'un pixel en plus de la posture accroupie ; à la détente (bond, charge, onde), étirement bref vers le haut puis retour élastique. Une annulation n'étire pas. Le retour de coup reçu reste prioritaire sur la position et l'échelle.
  - [`HitFeedback.cs`](../../scripts/Combat/HitFeedback.cs) : l'écrasement suit l'axe dominant du coup (coup de côté : écrasé en largeur ; coup du haut ou du bas : écrasé en hauteur).
  - Vérification : build, smoke test, `tools/test_enemy_abilities.sh`, capture en run, banc A/B si le coût bouge.

- [x] **Lot S3 — Déclinaison SDF 2.0 au reste du bestiaire**
  - Appliquer les fusions organiques `smin`, textures de surface, démarches et attaques avec anticipation/*smear* aux autres créatures de `tools/sprites/creatures/*.py`.
  - Planche bestiaire comparative, export et validation complète (`tools/validate.sh`).

---

## 4. Lot S2 livré — 17 octobre 2026

Duo pilote validé en jeu par Raphaël, échelle gardée (DECISIONS §188). Les cinq autres jouables sont régénérés avec le moteur du pilote.

**Fait :**
- Le cycle commun (`playable_animations` : marche asymétrique en huit poses, dash en quatre temps, coup reçu, mort) s'applique maintenant aux six jouables ; seule la Scaphandrière garde son dash propre.
- **Forgeuse** : la main droite tient le manche du marteau devant la poitrine (`hold_arm` dans `poses.py` : pose fixe, 20 % du balancier commun gardé, appliquée à l'attente, la marche et le dash) ; le manche passe dans le poing et dépasse dessous ; la tête du marteau reste en l'air quand le corps retombe (`drape_lift`) ; épaules fondues dans le buste (`smin`) ; reflet sur l'acier.
- **Traqueur** : pans de cape, pointe de capuche, écharpe et empennages sur les trois axes d'inertie (`drape`, `drape_lift`, `drape_sway`).
- **Facteur** : sacoche qui rebondit, lettres envolées sur les trois axes ; reflet sur les roues et l'insigne.
- **Éveillée** : ourlet, mèches et cordons sur les trois axes ; reflet sur le laiton.
- **Scaphandrière** : tuyau d'air et filin sur les trois axes ; reflet sur le cuivre et le laiton.
- Règles du §188 tenues : pas de détail d'un pixel ajouté, pas de nouvel `emissive`, pas de `stretch` dans le squelette.

**Retouches de la Scaphandrière :** ses 40 retouches Aseprite verrouillaient tous ses PNG. Les 216 PNG étaient identiques pixel pour pixel au rendu brut du générateur : aucune retouche faite à la main n'avait été exportée. Avec l'accord de Raphaël, les anciennes sont retirées et 40 retouches neuves sont recréées depuis le nouveau rendu (`--editable`).

**Vérifié :**
- Planche avant/après : [`planches/plan54_lot_S2_jouables.png`](planches/plan54_lot_S2_jouables.png) (marche SE, dash E, attente S, marche N, taille réelle sur sol).
- Régénération reproductible (deux passes de la Forgeuse, PNG identiques octet pour octet).
- `tools/smoke_test.sh` vert ; aucun nouveau PNG (mêmes noms et nombres de frames), donc aucun `.import` nouveau.
- Captures en vraie run (Forgeuse, Scaphandrière, 16 s, seed 221092026) regardées : marteau tenu lisible, pas de bavure sur le sol.
- Pas de banc : mêmes cadres, mêmes nombres de frames, aucun code du jeu modifié.

**Reste au plan :** S3 (bestiaire), et la partie Godot non découpée du volet B (traînée du dash à la silhouette exacte, vibration et écrasement des créatures avant l'attaque, écrasement orienté selon le coup reçu), à proposer en lot G3.

---

## 5. Lot G3 livré — 17 octobre 2026

**Fait :**
- **Traînées du dash** ([`MobilityFeedback.cs`](../../scripts/Combat/MobilityFeedback.cs)) : chaque traînée reprend la frame affichée du sprite joueur (animation, image, hauteur, inclinaison, échelle), en silhouette pleine à la couleur d'accent du personnage, puis se trame au pixel (Bayer 4×4) en s'effaçant en 0,18 s. Nouveau shader [`dash_silhouette.gdshader`](../../assets/shaders/dash_silhouette.gdshader), préchauffé au chargement. Couleur dans `data/characters/characters.json` (`accent_color`, la couleur du corps à défaut) : Vagabond orange écharpe, Forgeuse rouge forge, Traqueur vert, Facteur or, Éveillée cyan, Scaphandrière vert-de-gris. Personnage sans sprite : formes génériques inchangées.
- **Annonce des créatures** (nouveau [`WindupFeedback.cs`](../../scripts/Combat/WindupFeedback.cs)) : pendant l'annonce (bond du Rôdeur, charge et onde des gardiens), le visuel tremble d'un pixel en plus de la posture accroupie ; à la détente (`Enemy.ReleaseWindup`), il s'étire vers le haut (0,84 × 1,2) et revient au repos en 0,14 s. Une annulation n'étire pas. Un coup reçu garde la priorité sur la position et l'échelle.
- **Coups orientés** ([`HitFeedback.cs`](../../scripts/Combat/HitFeedback.cs)) : coup venu du côté, la créature s'écrase en largeur (0,8 × 1,2) ; du haut ou du bas de l'écran, en hauteur (1,25 × 0,75, comme avant). L'axe dominant tranche : un mélange annulait l'écrasement en diagonale.

**Régressions des lots G1 et G2 corrigées (commités sans relancer ces suites) :**
- G2 : le tempo de marche divisait la vitesse par la vitesse de base, ralenti compris ; le ralenti, déjà appliqué à la cadence par le statut, comptait deux fois (0,29 au lieu de 0,4). La référence inclut maintenant le facteur de ralenti, et un écart de moins de 8 % garde la cadence à 1 (pas de réécriture à chaque bousculade).
- G1 : l'élan d'attaque pouvait laisser le sprite décalé à la mort ou à la suspension (échec « Saut : mort en l'air repose immédiatement la silhouette ») ; il est remis à zéro dans les deux cas.

**Vérifié :** build 0 avertissement ; `tools/smoke_test.sh` ; `tools/test_enemy_abilities.sh` 0 échec, dont quatre contrôles nouveaux (tremblement d'un pixel, détente étirée, retour au repos, annulation sans étirement) et le contrôle d'écrasement mis à jour pour un coup de côté ; `tools/test_movement.sh`, `tools/test_weapons.sh`, `tools/test_dev_mode.sh` verts ; `tools/test_shader_warmup.sh` valide (premier passage expiré sous charge machine, second passage à 360 s valide). Relecture `godot-reviewer` : trois remarques traitées (remise au repos à l'arrêt, recul d'un coup préservé à la détente, même visuel ciblé que le retour de coup) et l'échelle copiée sur la traînée. Captures de la galerie d'armes avec dash (Vagabond, Forgeuse) regardées.

**Non mesuré :** pas de banc ; quatre sprites recyclés par dash, une créature à la fois en annonce, aucun nœud créé. Le tremblement et la détente ne se jugent qu'en mouvement : à regarder en jeu.

---

## 6. Lot S3 livré — 17 octobre 2026

Raphaël a choisi ce lot en autonomie (export direct en jeu, jugement à la fin). Les onze créatures restantes passent au moteur du pilote, avec les règles du §188 : pas de détail d'un pixel ajouté, pas de nouvel `emissive`, pas de `stretch` dans le squelette. Mêmes cadres, mêmes pivots, quatre images par action : aucun PNG ajouté ni retiré, donc aucun `.import` nouveau.

**Méthode commune :**
- **Matières** : contraste relevé (≈ 1,1) sur les chairs, cuirs et pierres ; reflet (`specular`) sur l'os, les crocs, les mandibules, la chitine, l'ardoise, la rouille, la fonte et la poche de bile ; bord éclairé (`rim`) sur les silhouettes sombres ou pâles (Ombre, Présage, Rampant d'Ombre, Hurleur, Tisseuse, Sentinelle, nœuds du Tréant).
- **Fusions `smin`** : seulement entre volumes d'un même matériau, avec un rayon court (0,6 à 1,2 K). Un rayon plus grand gonfle la masse et avale les détails voisins : sur l'Ombre, la flaque couvrait ses reflets et ses filaments ; sur le Charognard, les côtes et la crête d'échine. Le Rampant reste en anneaux séparés : sa segmentation est sa lecture.
- **Animations** : poses clés écrites à la main au lieu du cycle commun symétrique. Marche en quatre temps inégaux (appui lourd, rebond, reprise), attaque en quatre temps (anticipation, détente, impact, retour). L'attaque se joue au moment où le jeu l'appelle (`PlayAttackAnim`) : pour le bond du Charognard et la charge de la Brute, c'est au départ du saut, l'annonce étant déjà la posture accroupie du jeu (G3). Leur première image est donc la détente, pas une anticipation.

**Par créature :**
- **Ombre** : buste et flaque d'un seul tenant ; reptation en quatre temps (la main s'agrippe, le buste se hisse, la flaque se rattrape) ; se ramasse dans l'encre avant de jaillir.
- **Charognard** : garrot, taille, croupe et cou fondus ; trot heurté, tête en retard, queue à contretemps ; la gueule claque à la réception du bond.
- **Présage** : bosse fondue dans le voile ; dérive par à-coups, fil à plomb en retard sur le corps ; se recueille (manches closes, capuche baissée) avant l'incantation.
- **Rampant d'Ombre** : cou fondu dans le tronc, masque de porcelaine ; détale par saccades ; se cabre cou rentré avant de frapper.
- **Cracheur Pâli** : traîne la jambe droite (épaule qui plonge côté boiteux, poche qui ballotte) ; se cambre plus loin en arrière avant de cracher (son attaque se joue dès l'annonce).
- **Rampant** : reptation par vagues de contraction des anneaux (`squeeze`), la tête tâte le sol ; se ramasse avant de surgir.
- **Brute du Vide** : bosse d'épaules fondue ; marche boiteuse, le pilier de béton écrase le sol et le corps roule de son côté ; charge partie d'une poussée brutale.
- **Hurleur** : colonne d'un seul tenant ; tangue sur ses racines ; se ramasse plus bas avant de hurler.
- **Tisseuse** : à-coups d'insecte (deux pas vifs, un arrêt où une patte tressaille) ; se tasse au ras du sol avant de se cabrer.
- **Sentinelle** : bourrelets de chair fondus en une coulée ; cligne en veillant ; plisse les paupières et avance la tête pour viser.
- **Tréant Corrompu** : tronc d'un seul tenant ; se déracine une souche après l'autre ; coup de branche en arc complet, de l'armé haut à l'impact au sol.

**Outil :** [`tools/creature_compare.py`](../../tools/creature_compare.py) compose une planche avant/après d'une ou plusieurs créatures, sans rien écrire dans `assets/` ; `--before <ref>` lit les PNG « avant » dans un commit.

**Vérifié :**
- Planche avant/après des onze créatures (direction SE, toutes les actions, taille réelle sur sol ancré et effacé) : [`planches/plan54_lot_S3_bestiaire.png`](planches/plan54_lot_S3_bestiaire.png). Chaque créature regardée aussi en E, N et W pendant le réglage.
- Régénération reproductible : Présage régénéré deux fois, PNG identiques octet pour octet.
- 1 408 PNG réécrits (11 × 128), aucun fichier ajouté ni supprimé ; Rôdeur inchangé.
- `tools/validate.sh` : smoke, capacités ennemies, parties de boss, déplacements, 4/4 vertes, sources inchangées pendant les tests.
- Captures en vraie run (`--capture-bestiary`, onze créatures en deux groupes) regardées.

**Non mesuré :** pas de banc ; mêmes cadres, mêmes nombres d'images, aucun code du jeu modifié.

**À juger en jeu par Raphaël :** les marches à contretemps (Charognard, Rampant d'Ombre, Tisseuse) et la boiterie du Cracheur et de la Brute, qui ne se lisent qu'en mouvement ; le bord éclairé de l'Ombre et du Rampant d'Ombre sur sol sombre.


---
paths:
  - "tools/**/*.py"
  - "scripts/*.py"
  - "assets/**/*.png"
---

# Pipeline pixel art

- `doc/CHARTE-GRAPHIQUE.md` fait autorité sur le visuel ; `doc/plans/08-direction-artistique.md` décrit le pipeline actuel et ses décisions (échelle, orientations, décors).
- Production **procédurale** dans `tools/sprites/` : un modèle SDF (capsules, boîtes, ellipsoïdes) rendu par lancer de rayons à 30° (projection 2:1), puis passes pixel art (4 tons par matériau, lumière haut-gauche, contour sel-out, lignes internes). Pas de dessin isolé par script ni de redimensionnement Lanczos.
- Échelle commune : `MODEL_SCALE` = 0,62 (créatures, décors), `CHARACTER_MODEL_SCALE` = 0,53 (personnages, ~30 px). Décors : 1 m ≈ 27,5 unités (`tools/sprites/props/_kit.py`).
- Points d'entrée : `tools/generate_character.py <id>` et `tools/generate_enemy.py <id>` (8 directions, nommage `char_<id>_<DIR>_<action>_<NN>.png`, `enemy_<id>_<DIR>_<action>_<NN>.png`, directions E/SE/S/SW/W/NW/N/NE) ; `tools/generate_props.py <biome> [--sheet]` (un fichier par décor + `props_manifest.json`).
- Tout tirage aléatoire passe par une graine (`Weathering`) : régénérer doit donner des PNG identiques octet pour octet ; vérifier avec `git status` qu'un rendu non modifié ne change rien.
- Toujours produire une planche (`--sheet`) sur le sol réel du biome avec un personnage de référence, la regarder, puis capturer en jeu (`/capture`) avant de valider.
- Retouches Aseprite : `art/retouches/**/*.aseprite` + `.json` (cibles). Leurs PNG ne sont plus réécrits par les générateurs (`tools/sprites/retouch.py`) ; `--editable` crée une retouche, `tools/export_retouches.py` l'exporte. Ne jamais supprimer une retouche sans accord de Raphaël.
- Ne jamais supprimer un `.import` existant (uid référencé). Les nouveaux PNG reçoivent leur `.import` via `tools/smoke_test.sh` : les committer ensemble.
- Aperçus et planches : dans le scratchpad ou un dossier ignoré, jamais chargés par le jeu.
- Détails de surface (`tools/sprites/props/_surface.py`) : un détail se peint sur un volume (`painted`) au lieu d'être sculpté ; un détail plus fin qu'un pixel (gravier, crépi) se déclare `Part(..., relief=False)`, sinon ses marches cassent les normales et noircissent la surface.
- Grandes surfaces planes (toits, plateaux) : `PropModel(smooth_slopes=True)`, sinon chaque pixel d'un plan horizontal vu à 30° devient une ligne interne sombre. Désactivé par défaut pour ne pas changer en silence les sprites déjà validés.
- Borne rapide d'une pièce coûteuse (lierre, gravats) : renvoyer la distance à la borne seulement loin d'elle ; près du bord, évaluer la vraie forme, sinon la borne nulle sur son bord crée une fausse surface.
- Aperçu en jeu par décor : `CAPTURE_EXTRA_ARGS="--capture-prop-gallery <biome>" tools/capture_run.sh <dossier> …` (sous `xvfb-run` sans écran).
- Formes végétales et bois communs (`tools/sprites/props/_flora.py`) : feuillage en grappes (`_leaf_mass`) plutôt que des boules lisses, branches torses, rainures, polypores, lierre en feuilles plaquées ; un biome n'importe pas les aides d'un autre biome. `tree` (forêt) sert aussi aux champs et à la ferme : le modifier oblige à régénérer leurs arbres.
- Personnages : un œil rond d'un pixel tombe entre deux pixels du visage et disparaît de face ; dessiner les yeux en amande verticale (ellipsoïde ~1 × 1,6 unité de tête, posé sur la surface du visage), comme les héros du plan 32. Une coiffe ou une visière vue d'en haut à 30° couvre les yeux : la remonter, ou redresser la tête d'un personnage penché (repère de tête tourné, voir `facteur.py`).

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
- Ne jamais supprimer un `.import` existant (uid référencé). Les nouveaux PNG reçoivent leur `.import` via `tools/smoke_test.sh` : les committer ensemble.
- Aperçus et planches : dans le scratchpad ou un dossier ignoré, jamais chargés par le jeu.

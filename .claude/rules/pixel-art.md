---
paths:
  - "tools/**/*.py"
  - "scripts/*.py"
  - "assets/**/*.png"
---

# Pipeline pixel art

- `doc/CHARTE-GRAPHIQUE.md` fait autorité : palettes master/biomes, tailles, contours, nombre de frames, nommage (`char_<id>_<DIR>_<action>_<NN>.png`, `enemy_<id>_<DIR>_<action>_<NN>.png`).
- Sprites générés procéduralement en Python/Pillow. Un générateur par entité, idempotent, qui écrit directement dans `assets/<feature>/<id>/`.
- Couleurs uniquement issues des palettes de la charte (voir `tools/process_generated_sprite.py` pour la quantification).
- Les chargeurs (`Combat/CharacterSpriteLoader.cs`, `Combat/EnemySpriteLoader.cs`) trouvent les frames par convention de nom : respecter directions (NE, NW, SE, SW...) et actions attendues, sinon le fallback Polygon2D s'affiche.
- Aperçus (`*_preview.png`, `preview/`) : outils de validation visuelle, jamais chargés par le jeu.
- Après génération : lancer un import Godot (`tools/smoke_test.sh`) pour créer les `.import`, et les committer avec les PNG.

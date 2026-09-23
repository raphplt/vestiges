# Sprites des personnages

Générés par `python3 tools/generate_character.py <id>` (pipeline procédural `tools/sprites/`, un modèle par
personnage dans `tools/sprites/characters/<id>.py`). Une régénération écrase les PNG : toute retouche doit être
reportée dans le modèle.

- Cadre 32×48, pieds au pixel (16, 45), environ 35 px de haut ; `sprite_feet_offset` = 21 dans `data/characters/characters.json`.
- Taille calée sur le monde (retour « géant » du 23 septembre) : `FRAME_SIZE`, `FRAME_PIVOT`, `MODEL_SCALE` dans `tools/sprites/render.py`.
- Huit directions : E, SE, S, SW, W, NW, N, NE. Actions : idle 4, walk 4, dash 3, hurt 2, death 4 frames.
- Lumière haut-gauche, contour sel-out teinté, rampes de couleurs dérivées des couleurs identitaires (charte §4).
- Planche de casting (silhouettes, sol ancré et effacé) : `python3 tools/character_lineup.py sortie.png`.

# Sprites des personnages

Générés par `python3 tools/generate_character.py <id>` (pipeline procédural `tools/sprites/`, un modèle par
personnage dans `tools/sprites/characters/<id>.py`). Une régénération écrase les PNG et retire ceux d'un ancien
nombre de frames : toute retouche doit être reportée dans le modèle.

- Cadre 32×40, pieds au pixel (16, 36), environ 30 px de haut (33-34 px avec le sac du Vagabond ou l'arc du Traqueur) ;
  `sprite_feet_offset` = 16 dans `data/characters/characters.json` (pivot − demi-hauteur du cadre).
- Échelle propre aux personnages (`CHARACTER_MODEL_SCALE` = 0,53, avec `CHARACTER_FRAME_SIZE` et `CHARACTER_FRAME_PIVOT`
  dans `tools/sprites/render.py`), 15 % sous celle des créatures (`MODEL_SCALE` = 0,62) : retours « géant » du
  23 septembre puis « encore un peu trop grand » du 24.
- Huit directions : E, SE, S, SW, W, NW, N, NE. Actions : idle 6, walk 4, dash 3, hurt 2, death 4 frames.
- Idle vivant (`living_idle` dans `tools/sprites/poses.py`, joué à 5 fps, cycle de 1,2 s) : souffle qui lève épaules et
  tête d'un à deux pixels, bras qui s'ouvrent, balancement latéral du poids, et un élément souple propre au modèle
  (`Pose.drape`) : écharpe, pointe de capuche et manche d'outil du Vagabond, pointe de capuche et empennages du
  Traqueur, marteau qui se cale et braise qui palpite chez la Forgeuse.
- Lumière haut-gauche, contour sel-out teinté, rampes de couleurs dérivées des couleurs identitaires (charte §4).
- Planche de casting (silhouettes, sol ancré et effacé) : `python3 tools/character_lineup.py sortie.png`.

# Sprites ennemis

Chargés par `scripts/Combat/EnemySpriteLoader.cs` depuis le dossier `visual.sprite_folder` du JSON ennemi.

## Convention

`enemy_<id>_<DIR>_<action>_<NN>.png`
- Directions : E, SE, S, SW, W, NW, N, NE. Les jeux à quatre diagonales (NE, NW, SE, SW) restent acceptés ; un fichier sans direction est dupliqué sur toutes.
- Actions : idle, walk, attack, death (`move` accepté comme alias de `walk`).
- Frames numérotées à partir de 00 ou 01, contiguës.

## Pipeline procédural (densité commune avec les personnages)

Généré par `python3 tools/generate_enemy.py <id> [--sheet planche.png]` à partir de `tools/sprites/creatures/<id>.py`. Le script vide le dossier de ses PNG avant d'écrire. Lancer ensuite un import Godot pour créer les `.import`.

| Ennemi | Cadre | Pieds (px) | `sprite_feet_offset` |
|---|---|---|---|
| rodeur | 40×48 | (20, 45) | 21 |
| charognard | 32×32 | (16, 24) | 8 |
| presage | 32×48 | (16, 45) | 21 |

`sprite_feet_offset` = distance du centre du cadre aux pieds. Présent dans le JSON, il ancre les pieds au sol à l'échelle 1.

## Anciens sprites

Seule la Tisseuse vient encore d'un ancien générateur Pillow (`scripts/generate_tisseuse.py`, 4 directions, 64×64), à remplacer par un modèle du pipeline ; ce script sera alors retiré. Sans `sprite_feet_offset`, elle est centrée et remontée de 35 % de sa hauteur. Les anciens générateurs du Cracheur, du Rampant, de l'Ombre rampante et du Tréant sont retirés (10 octobre) : ces créatures viennent du pipeline.

# Retoucher un sprite dans Aseprite

Les sprites du jeu sont générés par des scripts Python (`tools/`). Pour retoucher l'un d'eux à la main sans que la prochaine génération efface ton travail, on passe par un fichier Aseprite dans `art/retouches/`.

## 1. Créer le fichier à retoucher

Relancer le générateur du sprite avec `--editable` :

| Sprite | Commande |
|---|---|
| Décor | `python3 tools/generate_props.py forest --only prop_stump --editable` |
| Projectile | `python3 tools/generate_projectiles.py arrow --editable` |
| Personnage | `python3 tools/generate_character.py vagabond --editable` |
| Créature | `python3 tools/generate_enemy.py rodeur --editable` |

Le fichier apparaît au même chemin que le PNG, sous `art/retouches/`. Exemple : `art/retouches/assets/props/forest/prop_stump.aseprite`.
- **Personnage ou créature** : un fichier par direction et par action (`char_vagabond_SE_walk.aseprite`), une frame par image du jeu.
- **Projectile** : la planche entière (16 directions en colonnes, frames en lignes).

Chaque fichier contient :
- trois calques : **couleurs**, **lignes internes**, **contour** ;
- une palette : les couleurs du sprite, du plus sombre au plus clair, puis la palette master de la charte.

## 2. Retoucher

Ouvrir le `.aseprite`, retoucher, enregistrer (Ctrl+S).
- **Garder la taille du sprite.** Le point au sol, la collision des décors et l'ancrage des pieds en dépendent. Une taille différente est refusée à l'export.
- **Rester en couleur RGBA** (Sprite › Color Mode › RGB Color) et en mode de fusion normal.
- **Calques libres.** Tu peux en ajouter, en masquer ou en grouper : seuls les calques visibles sont exportés.
- **Animations.** Ne pas ajouter ni retirer de frames : chacune correspond à un PNG précis.

## 3. Exporter vers le jeu

```bash
python3 tools/export_retouches.py           # toutes les retouches
python3 tools/export_retouches.py stump     # seulement celles dont le chemin contient « stump »
```

Le script aplatit les calques visibles et réécrit les PNG du jeu ; Godot les réimporte seul. Il n'a pas besoin de l'exécutable Aseprite.

## 4. Et ensuite ?

- **Régénérer ne casse rien.** Tant que la retouche existe, les générateurs ne réécrivent plus ses PNG : ils affichent « retouche conservée ». Le reste du catalogue est régénéré normalement.
- **Rendre le sprite au générateur** : supprimer le `.aseprite` et son `.json` voisin, puis relancer le générateur.
- **Recommencer depuis un rendu frais** : même chose, puis relancer avec `--editable`. Une retouche existante n'est jamais écrasée.
- **Committer** le `.aseprite`, son `.json` et le PNG exporté ensemble.

Détails techniques : `tools/sprites/retouch.py` (protection), `tools/sprites/aseprite.py` (format), `tools/export_retouches.py` (export).

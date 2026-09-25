---
name: capture
description: Capture l'écran d'une vraie run de Vestiges (bot invincible, 1080p) pour vérifier un changement visible — HUD, micro-événement, carte des biomes, décors et collisions, bestiaire — puis regarde les images. À utiliser après toute modification de rendu, de génération du monde, de décors, de VFX ou d'UI en run, et pour montrer un avant/après.
argument-hint: "[run|event <id>|map|props|props-clean|bestiary] [seed]"
allowed-tools: Bash(tools/capture_run.sh *) Bash(python3 *) Bash(ls *) Bash(grep *) Read
---

# Captures en vraie run

Le smoke test ne boote que le Hub ; ce skill lance `res://tools/tests/RunObservation.tscn` dans la vraie scène de run (`tools/capture_run.sh`, profil dev temporaire, XDG isolé).

## Choisir le mode

| Argument | `CAPTURE_EXTRA_ARGS` | Produit |
|---|---|---|
| `run` (défaut) | — | Une capture toutes les N secondes d'une run naturelle (`<dossier> 60 10`) |
| `event <id>` | `--event <id>` | Micro-événement forcé à 3 s (`hunt` Souverain, `stampede` Harde, `fallen_relic` Vestige tombé, `vigil` Veille, `shard_rain` Averse ; ids dans `data/events/run_events.json`) |
| `map` | `--capture-map [--map-seeds 40]` | Statistiques de biomes autour du spawn (ligne `RESULT map`), `biomes-<seed>.png`, vues dézoomées |
| `props` | `--capture-props` | Zone la plus chargée en décors de chaque biome, formes de collision affichées, et vue « derrière » le plus haut décor |
| `props-clean` | `--capture-props --hide-collisions` | Idem, rendu propre (pour juger la beauté) |
| `bestiary` | `--capture-bestiary` | Gros plans des créatures du pilote autour du joueur |

## Procédure

1. Dossier de sortie dans le scratchpad de la session (jamais dans le dépôt), un dossier neuf par capture.
2. Lancer, par exemple : `CAPTURE_EXTRA_ARGS="--capture-props --hide-collisions" tools/capture_run.sh <dossier> 10 0 1920x1080 1002`. Seed par défaut du banc : 221092026 ; 1002 donne une ville dense proche du spawn. Garder la même seed pour un avant/après.
3. Vérifier le journal : `grep -E "RunObservation|ERROR: \[|Exception" <dossier>/run.log`.
4. **Ouvrir les PNG avec Read et les regarder.** Les captures font 3840×2160 sur écran haute densité : pour juger un détail, recadrer autour du joueur avec Pillow (`Image.crop`, puis `resize(..., Image.NEAREST)`) plutôt que d'agrandir toute l'image.
5. Rendre compte de ce qui est vu (et de ce qui cloche), avec le chemin des images pour Raphaël. Ne pas versionner les captures.

## Pièges

- Les ~90 premières frames montrent l'écran de chargement : les modes `map`, `props` et `bestiary` l'attendent déjà ; pour une capture ad hoc, attendre aussi.
- Le compteur FPS affiché dans une capture n'est pas une mesure (rendu 4K, formes de debug, téléportation) : utiliser `/bench`.

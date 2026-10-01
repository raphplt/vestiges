# Reprise des personnages R5 — 1er octobre 2026

Production SDF existante, échelle 0,53, cadre 32×40, pivot (16,36), kits et
vitesses d’animation conservés. Planches de comparaison sur les tuiles réelles
forêt/ruines/carrière avec `tools/review_character.py`, huit directions et cinq
actions. Les rapports JSON conservent les 152 empreintes SHA-256 par personnage.
Les captures Main emploient `--capture-character --character <id>`, seed 221092026,
ViewSonic uniquement (X11, écran 1, position 3840,0 ; sortie réelle 3840×2160).

## Traqueur — R5a

Épaules, torse et capuche légèrement élargis ; col, rebord de capuche et poignets
beiges plus présents. Vert forêt et silhouette élancée conservés, arc au dos.
152/152 images changées, cinq actions × huit directions chargées par Godot.
Cadre/alpha/séquences contrôlés, quatre images touchent le bas comme avant ;
le rendu dans un cadre étendu confirme **zéro frame coupée**. Deux générations
complètes donnent **152 PNG identiques octet pour octet**.

Planches regardées : `/tmp/vestiges-r5-traqueur-review/comparison.png`,
`/tmp/vestiges-traqueur-complete.png`. Captures de référence puis nouveau :
`/tmp/vestiges-r5-traqueur-reference`, `...-verified`, `...-final`.
Marche, dash, dégât et mort capturés ; le journal confirme S_dash, S_hurt, S_death.
Build sans avertissement et smoke 600 frames verts. Aucun coût de rendu ajouté
au runtime : mêmes tailles et nombres de textures ; pas de mesure FPS sous charge.

Le premier scénario capturait trop tôt, sous le chargement, et ne produisait
pas de ligne RESULT ; images écartées, attente de l’overlay et huit directions
ajoutées. L’image d’impact initiale précédait la pose hurt : attente de deux
frames physiques ajoutée, nouvelle capture vérifiée. Ce sont des corrections
du protocole, pas des défauts masqués du jeu. Validation artistique par Raphaël
encore ouverte ; pas de changement audio.

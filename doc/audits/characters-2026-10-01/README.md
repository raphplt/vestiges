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

## Vagabond — R5b

Écharpe orange de charte, capuche/manteau légèrement étoffés, poche latérale et
attaches orange sur le sac. Visage ombré, couchage et outils conservés.
152/152 PNG changés et reproduits identiquement par une seconde génération
séparée. Alpha/séquences/cadres vérifiés : les quatre frames de bord restent
les mêmes, aucun débordement au rendu étendu. 40 animations chargées dans Main.

Planches sur trois sols et cinq actions regardées dans
`/tmp/vestiges-r5-vagabond-review/comparison.png` et `/tmp/vestiges-vagabond-complete.png`.
Captures avant/après dans `/tmp/vestiges-r5-vagabond-reference` et `...-final` :
marche, S_dash, SE_hurt et S_death inspectés. Build sans warning, smoke 600 vert.
Pas de changement de contrat de texture ou de coût du code runtime ; recette
artistique humaine toujours ouverte.

## Forgeuse — R5c

Lunettes rouges renforcées, attaches et poche de tablier, manchettes de gants,
masse légèrement plus lisible. Stature, palette acier, squelette et kit conservés.
152/152 PNG changés ; deuxième génération séparée identique octet pour octet.
Alpha/séquences/cadres vérifiés ; **15 poses de bord contre 14 auparavant**,
dont E_death_04 nouvellement au bord. Le rendu étendu confirme qu’aucune n’est
coupée. Format et pivot restent communs, 40 animations chargées dans Main.

Planches regardées : `/tmp/vestiges-r5-forgeuse-review/comparison.png`,
`/tmp/vestiges-forgeuse-complete.png`. Référence et finale en run :
`/tmp/vestiges-r5-forgeuse-reference`, `...-final`. Marche, S_dash, S_hurt et
S_death capturés sur ViewSonic. Build sans warning et smoke 600 frames verts.
Pas de changement audio, validation artistique humaine encore ouverte.

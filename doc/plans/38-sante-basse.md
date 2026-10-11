# Plan 38 — Santé basse perceptible

Demande de Raphaël, 11 octobre 2026 : vignette rouge sang, battements de cœur, éventuellement d'autres détails.

## Lot S1 — Alerte de survie

- Une vignette persistante sous le HUD, rouge sang `#C4432B`, tramée comme la blessure. Centre dégagé ; intensité et épaisseur croissantes avec le danger.
- Entrée à 30 % des PV, sortie à 35 % pour éviter les allers-retours pendant la régénération ; intensité maximale à 8 %. Valeurs de départ ajustables dans `data/fx/player_feedback.json`.
- Un vrai double battement sourd, synthétisé et reproductible, de 72 à 120 pulsations/minute. Remplace la boucle de santé basse existante, respecte le volume des effets et s'interrompt pendant les menus en pause.
- Pulsation de la vignette calée sur la boucle sonore ; fondu après soin ; arrêt immédiat à la mort ou au Hub. La jauge de vie possède déjà une alerte rouge.
- Le flash directionnel de blessure reste au-dessus. Pas de secousses supplémentaires, ni de brouillage au centre du combat.

## Vérification

Build sans warning, smoke du Hub, régression du mix audio, scénario réel : santé pleine / 30 % / critique / soin encore sous le seuil de sortie / soin à 35 % / pause / mort. Captures plein écran et contrôles du lecteur audio. Aucun chiffre de performance revendiqué sans banc comparatif.

État : **implémenté et vérifié** le 11 octobre 2026.

- Build : zéro warning, zéro erreur.
- `tools/validate.sh /tmp/vestiges-low-health-validation smoke audio` : 2/2 suites ; banque de 126 sons sans échec et régression du mix verte. Deux avertissements préexistants d'écrêtage des sources orage/coffre restent signalés par l'audit.
- `CAPTURE_EXTRA_ARGS=--capture-low-health tools/capture_run.sh /tmp/vestiges-low-health-captures-v2 15 1 1920x1080 221092026` : dix contrôles réussis (pleine vie, entrée à 30 %, 72 BPM, critique à 8 % et 120 BPM, suspension, reprise, soin à 32 %, sortie à 35 %, absence de redémarrage à 32 % depuis le haut, mort). La régénération est neutralisée dans ce scénario pour maintenir les valeurs exactes.
- Captures regardées : `health-0-full.png`, `health-1-low.png`, `health-2-critical.png`, `health-3-healed.png`. Centre et HUD dégagés ; rouge retiré après le soin.
- Son mono synthétisé par `tools/generate_heartbeat.py`, sans échantillon externe ; banque raccordée à `heartbeat.wav`. Sonie momentanée maximale mesurée : −16,2 LUFS avant le gain dynamique, sans écrêtage ni problème de phase. L'écoute artistique en situation reste à apprécier par Raphaël ; les contrôles automatiques ne la remplacent pas.

## Implémentation

`AudioManager` conserve la responsabilité de la boucle existante et expose son intensité et sa contraction courante. Un lecteur réutilisé, aucun lecteur créé à chaque battement. `LowHealthVignette`, sous la blessure directionnelle et le HUD, utilise désormais le shader elliptique dédié `low_health_vignette.gdshader` (lot S2) ; aucune lecture de la texture d'écran. Réglages et ordre des seuils validés par `PlayerFeedbackConfig`. Aucun changement aux PV ni au combat.


## Lot S2 — Dégradé arrondi et disparition en pixels

Retour de Raphaël : le premier rendu ne convient pas ; demander davantage de dégradé et d'arrondis, avec éventuellement des pixels qui disparaissent comme l'Effacement.

Préparation avant code : remplacer le matériau de santé basse par un shader dédié, en halo elliptique et dégradé continu. Dissoudre localement la matière rouge en petits amas carrés dont l'opacité évolue lentement ; centre transparent. Horloge animée pilotée par le nœud pour figer l'effet en pause. Vérification : build strict, smoke, captures du scénario santé basse aux mêmes seed et résolution. Le son et ses seuils ne changent pas.

État : **implémenté et vérifié**. Shader dédié ajouté au préchauffage. Halo elliptique, dégradé continu, coins rouge sombre, dissolution lente et discrète par amas de 3 × 3 pixels natifs, quelques fragments sur la frange intérieure. Centre transparent, animation figée avec la run en pause.

- `tools/validate.sh /tmp/vestiges-low-health-rounded-validation smoke` : build sans warning, smoke vert.
- `CAPTURE_EXTRA_ARGS=--capture-low-health tools/capture_run.sh /tmp/vestiges-low-health-rounded 15 1 1920x1080 221092026` : dix contrôles réussis ; captures critique et après soin regardées. Aucun échec shader en GL Compatibility.
- Validation artistique par Raphaël encore ouverte.

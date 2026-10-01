# Fiabilisation de la recette — 1er octobre 2026

Suite du plan 10, après 5B1 ; aucun code de gameplay modifié.

`test_movement.sh --run-integration` abandonnait après 1 500 frames headless,
avant la fin du travail de génération sur son thread. Échec reproduit sur la
référence et après le correctif des shaders ([logs](../shader-warmup-2026-10-01/README.md)).
La fixture attend désormais au plus 120 secondes murales ; elle cède 1 ms par
frame pendant ce seul chargement. Le lancement garde un timeout externe de
180 secondes. Le pas fixe 60 Hz et toutes les assertions restent identiques.

Même instrument sur les deux versions :

| Version | Fin de chargement | Résultat |
|---|---|---|
| Référence 63492145 | 2 728 frames / 7 255 ms | 26 assertions, zéro échec |
| Après 5B1 | 2 727 frames / 7 243 ms | 26 assertions, zéro échec |

Ces chronométrages vérifient seulement l'achèvement avant le délai ; machine
chargée, aucune comparaison de performance. La première tentative utilisait
`Engine.MaxFps`, ignoré en mode `--fixed-fps` : remplacé par le délai explicite.
Le checkout de référence a reçu uniquement les deux fichiers du banc le temps
de ce contrôle, puis a été restauré à son état propre.

Le lanceur `capture_run.sh` refuse maintenant un journal comportant une erreur
moteur inattendue. Son contrat de sortie a été vérifié par rejeu de deux logs
réels avec un exécutable témoin, sans lancer de fenêtre supplémentaire :

- résultat présent **et** ennemi inconnu : code de sortie **1** ;
- résultat valide et seul avertissement de ressources à la fermeture : **0**.

Les exceptions documentées Steam/audio Dummy/ressources restent filtrées.
Syntaxe des trois scripts shell vérifiée. Builds C# des deux références : zéro
avertissement et erreur. Profils temporaires, aucune sauvegarde joueur touchée.

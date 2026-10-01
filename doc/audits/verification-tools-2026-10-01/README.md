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


## Vérification après intégration sur `main`

Les trois lots de cette continuation sont réunis sur `main` :
`fe46a0c8` (écrans de choix), `88f307a2` (préchauffage rendu),
`b46ae135` (outils de recette). Vérification finale sur ce checkout :

- écrans de choix : **24 contrôles, zéro échec** ;
- Main réelle headless : **26 contrôles, zéro échec**, initialisation en
  8 266 ms, sous la limite murale de 120 s ;
- compilation : **zéro avertissement et erreur** ; smoke de **600 frames vert** ;
- Main avec rendu : **12 captures** des capacités Hurleur/Cracheur, seed
  221092026, inspectées en planche et une image ouverte séparément. Journal
  sans erreur inattendue, préchauffage à **17 dessins pour 16 shaders**.
  Fenêtre constatée sur ViewSonic, écran 1, position (3840, 0), taille réelle
  3840 × 2160 malgré la demande de 1920 × 1080.

Journaux finaux `final-main-*.log.gz` et empreintes des captures dans
[final-main-captures.json](final-main-captures.json). Aucun chiffre de FPS
retenu ; ni le cache pilote froid/chaud ni le ressenti humain ne sont validés.

Les dix fichiers de travail indépendants sur la révélation Mémorial/Faille
ont été préservés hors de ces commits (contrôle des empreintes, fusion des
seuls hunks de `GameBootstrap`). Leur code présent pendant cette dernière
recette ne constitue pas une livraison de ce lot. À sa clôture, son nouveau
shader `landmark_reveal` devra rejoindre le catalogue de préchauffage et
passer le contrôle de couverture ; les 16 shaders vérifiés ici correspondent
au code déjà committé. Aucun fichier audio n'est modifié par cette continuation.

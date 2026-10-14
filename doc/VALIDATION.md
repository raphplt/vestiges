# Valider un lot

La commande commune construit avec les warnings traités comme des erreurs, vérifie l'import Godot, puis exécute les suites une par une avec des profils temporaires :

```sh
tools/validate.sh /tmp/vestiges-validation-mon-lot
```

Choisir un dossier neuf. Les suites par défaut couvrent le Hub, les déplacements et l'intégration de Main, les armes et raretés, les objets, les perks, les capacités ennemies, les petits lieux, les bonus, le cône, les effets temporels, l'Effacement, la carte, les choix, les parcours de menus (`menu_navigation`), les assets UI, les intentions musicales, la banque et les priorités du mix audio (`audio`), le mode dev, ses actions et l’exclusion des exports, les sauvegardes, le chargement récupérable, le remapping des touches, la reproductibilité des tirages, les phases de la run, les contrats des catalogues de réglages, l’Indicible, les parties de boss, la file des classements Steam (faux service), ainsi que les modèles de progression et de lanceurs.

Pour une vérification ciblée, donner les noms après le dossier :

```sh
tools/validate.sh /tmp/vestiges-validation-combat weapons cone movement-integration
python3 tools/tests/test_validation_launchers.py
```

Pour les signatures du Vagabond et de la Forgeuse uniquement : `tools/validate.sh <dossier neuf> smoke route_forge`. La suite `route_forge` évite de rejouer les autres perks. Capture commune : `CAPTURE_EXTRA_ARGS="--capture-route-forge --character vagabond" tools/capture_run.sh <dossier>`.

Le dossier contient `build.log`, `import.log`, un journal par suite, `results.tsv`, `validation.json` et les manifestes de sources avant/après. La commande renvoie un échec si une suite échoue ou si les sources changent pendant les tests. Une erreur de build ou d'import arrête la commande avant les scènes. En cas d'échec, les profils et journaux temporaires des lanceurs sont conservés ; `VALIDATION_KEEP_LOGS=1` les conserve aussi en cas de succès.

Chaque lancement de moteur doit finir avec le code 0 et un seul résultat correspondant au scénario. Un résultat suivi d'un crash, un journal vide, un résultat absent ou répété et un timeout sont des échecs. Les lignes d'erreur sont contrôlées sans filtrage par mots tels que `steam_api`. Les avertissements Steam, MixRate et ObjectDB de fermeture peuvent subsister ; seule la ligne exacte de ressources Godot encore utilisées à la fermeture est tolérée parmi les erreurs.

Un verrou par checkout empêche deux lanceurs de validation ou de mesure de se superposer. Le lanceur global partage son build/import avec ses suites. Le verrou ne coordonne pas l'éditeur, un `dotnet build` manuel ou les scripts de capture historiques : fermer toute session qui écrit dans le même checkout, ou utiliser un checkout isolé. Le lanceur global contrôle les changements de sources, sans immobiliser les autres chats.

Les mesures demandent également un dossier neuf, des seeds distinctes, un processus réussi et un CSV non vide couvrant la durée annoncée :

```sh
MEASURE_JOBS=2 tools/measure_run.sh /tmp/vestiges-mesure 30 "7 42"
SECONDS_PER_RUN=30 tools/measure_density.sh /tmp/vestiges-densite 7 42
BENCH_REPEATS=1 BENCH_SECONDS=15 tools/benchmark_movement.sh /tmp/vestiges-banc
tools/bench_ab.sh HEAD /tmp/vestiges-ab 2
```

Le contrôle des exports (`tools/test_dev_release.sh`) fait désormais partie du global : assemblies Debug/Release, archive de ressources Godot native sans outils, preset temporaire restauré. Le préchauffage GPU (`tools/test_shader_warmup.sh <dossier neuf>`), les captures, l'écoute et les bancs FPS se lancent séparément. Les tests headless ne prouvent ni le rendu, ni les performances GPU, ni le fonctionnement réel de Steam. Une comparaison A/B partielle ou sans passe valide échoue ; mesurer les FPS sur une machine calme.

Sur un checkout neuf sans cache d'import, Godot peut signaler que la police du thème n'est pas encore importée avant son premier scan. Ce premier lancement reste un échec explicite, même s'il produit les imports. Conserver son journal puis relancer avec un autre dossier de résultats après avoir vérifié le diagnostic ; ne pas ignorer des erreurs d'import répétées.

Le scénario `CAPTURE_EXTRA_ARGS="--capture-pause --capture-size 1280x720" tools/capture_run.sh <dossier> 8 4 1280x720` vérifie aussi les paramètres par événements manette, l’annulation de l’abandon et le défilement d’une fiche chargée d’Oublis. `--capture-death --restart-from-bilan` vérifie le bilan, le retour au choix du personnage et le départ d’une nouvelle run. `--capture-size WxH` force la taille de fenêtre après le démarrage de Main ; les captures restent à contrôler visuellement.

### Refonte des menus — complément du 11 octobre 2026

`choice_screen` couvre aussi le butin (révélation, attente de lecture, confirmation unique) et le journal (profil neuf sans spoiler, fragment retrouvé, J/B, retour au contexte et sélection conservée). `--capture-cascade` contrôle la relance, le bannissement, son annulation, les niveaux en attente et la reprise.

Captures supplémentaires : `CAPTURE_EXTRA_ARGS="--capture-journal --capture-size 1280x720" tools/capture_run.sh <dossier>` ; `CAPTURE_DEV=" "` désactive le profil dev du banc pour contrôler un profil neuf. `--menu-font-scale 1.3` agrandit les rôles typographiques dans le processus de test seulement : il ne crée pas un réglage utilisateur et ne modifie aucune sauvegarde.

`--capture-death --bilan-rich --restart-from-bilan` ajoute neuf annonces de quête fictives **au seul affichage du bilan**, quatre Réminiscences, puis vérifie Équipement → Expédition, le défilement des gains et la relance avec un autre personnage. Cette variante éprouve la mise en page ; elle ne valide pas que ces neuf quêtes ont été réellement accomplies. Les stats de combat et la mort viennent de Main. [Preuves de la recette](audits/menus-2026-10-11/README.md).

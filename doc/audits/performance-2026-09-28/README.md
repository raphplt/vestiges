# Preuves — audit du 28 septembre 2026

[Rapport](../../AUDIT-PERFORMANCES-2026-09-28.md). Aucun fichier de production modifié pour ces expériences.

## Référence et provenance

- Commit de départ : `0383e05379cc148496c24c4ef01ea7a3e7769fb6`.
- `initial-worktree.json` conserve le statut et les empreintes des fichiers modifiés au départ. Leur contenu et leur diff sont volontairement exclus du commit de l’audit.
- Copie de mesure figée : `/tmp/vestiges-audit-20260928`, HEAD détaché, modifications initiales appliquées avant instrumentation. Le travail concurrent dans l’arbre principal ne change pas cette copie. Exception documentée : les quatre passes finales de densité utilisent le build déjà construit de l’arbre principal, sans recompilation ; ses empreintes de production sont toutes identiques à celles de la copie, et son SHA d’assembly distinct est conservé dans le manifeste de densité.
- Les manifestes donnent la commande, le SHA-256 de l’assembly, les empreintes des sources, les codes de sortie, les erreurs et les relevés de charge. Même seed `221092026`, même build dans les variantes d’une comparaison. Profil utilisateur Godot temporaire et isolé.
- Godot 4.7.2 Mono, .NET 10, build Debug ; GL Compatibility pour le rendu, driver Dummy pour l’audio. Le son demandé est exécuté, mais ce driver ne mesure pas le coût audio d’une sortie réelle.

## Contenu

| Fichier / dossier | Interprétation |
|---|---|
| `systems-a.json`, `systems-b.json` | Deux processus : recensement réel des décors, appels isolés d’Effacement, foule, statuts et cône ; intervalles par appel conservés |
| `physics.json` | Mémoire native de 10 000 Node2D / StaticBody2D sans forme ; première séquence de chauffe conservée ; seconde séquence A/B/B/A stabilisée |
| `shaders.json` | Compteurs du viewport, huit shaders identiques, hors champ / visibles / visibles / hors champ ; cinq images par étape |
| `shader-coverage.json` | Inventaire de 19 shaders, appartenance à la liste de préchauffage, références littérales dans les sources |
| `cycles.json` | Hub initial puis vingt retours au Hub, compteurs après stabilisation et GC |
| `manifest-*.json` | Provenance et charge des essais ; `manifest-crowd.json` documente la première tentative refusée à charge 4,65 |
| `crowd/`, `density/` | Huit passes observateur puis quatre passes densité au calme ; JSON, intervalles CSV gzip et manifestes de charge ; douze séries valides |
| `profile/` | Quatre profils EventPipe, ordre 60/240/240/60 ; piles Speedscope et intervalles d’images compressés sans perte, logs et métadonnées |
| `summary.json` | Synthèse dérivée par `summarize.py`, y compris le profil du seul thread exécutant les ennemis |
| `*.log.gz`, `build.log` | Journaux bruts compressés et build de la copie figée |

**Les FPS des fichiers `profile/*.json` sont instrumentés et relevés sous charge : ils ne sont pas des résultats de débit exploitables.** Les temps inclusifs des piles ne s’additionnent pas ; les appels natifs restent dans leurs appelants et les autres threads en attente ne doivent pas être comptés comme du CPU de gameplay. Les événements GC du `.nettrace` de travail n’ont pas servi aux conclusions : les piles exportées sont archivées, pas une analyse des pauses GC.

Les premières fixtures de développement ont été exclues : recensement limité par erreur aux enfants directs des tronçons ; corps désactivés qui invalidaient `MoveAndSlide` ; mémoire sans attendre les libérations différées ; première erreur de signature du script de rendu. Les JSON officiels proviennent des scripts corrigés, sans erreur inattendue. Le premier import de la copie a aussi quitté sur une erreur native ; ne pas le confondre avec la sortie des diagnostics officiels.

## Scripts de diagnostic

L’instrumentation exécutable vit dans les emplacements autorisés du projet :

- [`tools/tests/PerformanceAudit20260928.cs`](../../../tools/tests/PerformanceAudit20260928.cs) et sa scène : données de carte, horloges, appels isolés, cône, option `--cycles`.
- [`tools/tests/PhysicsBodyAudit.gd`](../../../tools/tests/PhysicsBodyAudit.gd) et sa scène : isoler la mémoire native des corps sans formes, sans scène Main ni wrapper C# par corps.
- [`tools/tests/ShaderWarmupAudit.gd`](../../../tools/tests/ShaderWarmupAudit.gd) et sa scène : vérifier la soumission réelle au rendu des huit matériaux du bootstrap.
- [`tools/tests/MovementDenseBenchmark.cs`](../../../tools/tests/MovementDenseBenchmark.cs) : option `--audit-observer-period N`, coût total de l’inspection et nombre d’inspections. Défaut 1, comportement antérieur préservé ; 60 signifie une inspection toutes les 60 **images**, pas toutes les secondes.
- [`tools/audit_performance_20260928.sh`](../../../tools/audit_performance_20260928.sh) : orchestration, profils temporaires, garde de charge, enregistrement des commandes et rejets des erreurs.
- [`tools/profile_crowd_20260928.sh`](../../../tools/profile_crowd_20260928.sh) : attache EventPipe après chauffe, quatre passes alternées, profil explicitement séparé des FPS.
- [`summarize.py`](summarize.py) : synthèse depuis les mesures et piles brutes, sans conversion des chronométrages sous charge en FPS.

Les liens sont relatifs au dépôt pour rester utilisables depuis une autre machine.

## Reproduction

Depuis une copie isolée de l’état à comparer, compiler et importer une seule fois. Ne pas lancer build, import, capture ou second banc pendant une mesure de débit. `GODOT_BIN` surcharge l’exécutable. Les fichiers de production en cours au moment de l’audit ne sont pas reconstituables depuis le seul commit de documentation : comparer leurs empreintes aux manifestes, ou employer la copie de mesure conservée localement.

```bash
dotnet build --nologo
godot-mono --headless --editor --import --path .
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau systems
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau physics
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau shaders
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau cycles
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau crowd
AUDIT_SKIP_BUILD=1 tools/audit_performance_20260928.sh /tmp/vestiges-audit-nouveau crowd-density
python3 doc/audits/performance-2026-09-28/summarize.py /tmp/vestiges-audit-nouveau
```

Sans `AUDIT_SKIP_BUILD=1`, le lanceur construit et importe avant son mode. Le dossier peut servir à plusieurs modes ; chaque mode refuse d’écraser son manifeste existant. Le mode `crowd` compare 60 puis 240 ennemis, observateur 1/60 puis 60/1, sans profilage. `crowd-density` compare 60/240/240/60 ennemis avec observateur décimé. Pour synthétiser les bancs comme dans cette archive, ranger leurs résultats sous `crowd/` et `density/`. Les maxima des douze passes sont à l’index 1, au moment du travail d’inventaire initial du banc ; aucune image n’est supprimée des fichiers bruts. Il suit la charge chaque seconde ; seul `fps_eligible=true` avec `valid=true` permet d’exploiter une passe pour le débit. Un seuil dépassé avant départ bloque la suite et l’inscrit dans le manifeste. Une hausse pendant la passe l’invalide pour le débit même si le gameplay du banc reste valide.

Pour le profil managé, `dotnet-trace` 10.0.745401 a été installé dans un dossier temporaire, sans dépendance ajoutée au jeu :

```bash
dotnet tool install --tool-path /tmp/vestiges-audit-profiler dotnet-trace --version 10.0.745401
DOTNET_TRACE=/tmp/vestiges-audit-profiler/dotnet-trace tools/profile_crowd_20260928.sh /tmp/vestiges-profil-nouveau
```

Ouvrir un fichier `.speedscope.json.gz` après décompression avec un visualiseur Speedscope, ou relancer `summarize.py` (lecture directe du gzip). La synthèse choisit le thread comportant `Enemy._PhysicsProcess`, puis calcule les temps inclusifs et exclusifs à partir des intervalles de pile. Cette conversion ne transforme pas un profil de temps mural en profil natif CPU.

## Limites des fixtures

- **Foule isolée** : mêmes méthodes de production par délégués, 100 échantillons par méthode ; toutes les positions sont remises en place avant chaque mesure. Sous-méthodes non exclusives, Ombres seules, ni frame complète ni solveur global.
- **Cône** : 240 ennemis toujours enregistrés, 0 ou 50 placés dans le cône ; trois activations, la première exclue comme chauffe, 240 ticks mesurés. Pas de mort, pas de choix de build. Allocations comptées seulement à l’intérieur des appels au cône.
- **Effacement** : simulation sans combat ni obstacles ; trajectoire synthétique finie. Compteur d’allocations du parcours nomade **incluant l’observateur**, contrairement au test séparé des cellules à zéro. `zero_visits_after_update` ne compte pas exactement le travail évitable.
- **Statuts** : distance réimposée avant chacun des 600 ticks. Comparaison de l’expiration, pas d’un vrai parcours.
- **Mémoire des corps** : moniteur natif Godot, pas RSS, pas coût CPU de broadphase ; les instances GDScript évitent les wrappers C#.
- **Cycles** : instanciation/destruction des vraies scènes avec une seconde de run simulée ; ni actions UI complètes, ni bilan, ni longue run, ni mémoire GPU.
- **Shaders** : rendu réel, caches pilote habituels ; les huit compteurs ne mesurent pas la compilation à froid.

## Validation finale

Le build de référence est à zéro warning/erreur (`build.log`). Les manifestes officiels des systèmes, cycles, physique et shaders ont un code de sortie nul et aucune erreur inattendue. Les avertissements audio Dummy, absence de Steam et ressources à la fermeture restent distincts des erreurs de script. Le bilan du build et du smoke test finaux est enregistré dans `validation.json`.

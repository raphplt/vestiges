# Temps forts — mesures du 9 octobre 2026

Données du [plan 30](../../plans/30-temps-forts.md). Code `d7789e9f` plus la sonde `--timeline` (`tools/tests/RunTimelineProbe.cs`). Graines 42, 1002, 7, 123 ; Péril 0 ; mesure sans rendu en temps accéléré (`tools/measure_run.sh`).

| Dossier | Profil | Durée |
|---|---|---|
| `build/` | `--nomad --visit --mortal --prefer souffle_du_neant,reflet_brise,memoire_vive,photo_de_classe,jeton_de_fete,oeil_critique` (protocole P0 du plan 28) | 900 s |
| `nomad/` | `--nomad --visit` | 900 s |
| `wander/` | aucun argument (bot qui erre autour du départ) | 900 s |
| `cap300/` | comme `build/`, plus `--scaling max_enemies_on_screen=300` (surcharge de mesure, `data/` inchangé) | 660 s |
| `t1-build/`, `t1-nomad/`, `t1-wander/` | mêmes profils après le lot T1 (`crisis_cap_multiplier` 1,4 dans les données) ; seulement `timeline`, `summary.txt` et `chronologie.md` | 900 s |

Par graine : `density-<graine>.csv` (par seconde : visibles, proches à 600 px, en vie, apparues, tuées, niveau, dégâts reçus, Essence…), `beats-<graine>.csv` (par seconde : phase, Résurgence, événement en cours, élites et Souverains, créatures à 300 px, PV autour du joueur), `timeline-<graine>.csv` (heure de chaque temps fort). `summary.txt` : lignes `RESULT` de la mesure. `chronologie.md` : sortie de `python3 tools/summarize_timeline.py <dossier>` (tranches de 30 s, temps forts par graine, creux, contrastes avant/pendant/après).

Lot T2 (Souverains à heure fixe) : `t2-build/`, `t2-nomad/` (9 min, avant la relecture), `t2-final/` (build, 10 min, après les corrections de la relecture) ; mêmes fichiers que les dossiers `t1-*`.

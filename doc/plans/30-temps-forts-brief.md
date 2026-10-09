# Brief — étude des temps forts : la chronologie réelle des 15 premières minutes

À donner tel quel à l'agent qui fait l'étude. Écrit le 9 octobre 2026 (DECISIONS §83). **C'est une étude : rien n'est à implémenter avant que Raphaël ait choisi.**

---

Tu fais une étude de game design chiffrée sur Vestiges, roguelite de survie en vue isométrique (Godot 4.7, C#). Raphaël trouve que le jeu manque de **temps forts** par rapport à ce à quoi il joue en ce moment :

> « vampire survivor a plus de temps fort que nous (j'y joue en ce moment). megabonk aussi avec sur les premieres 10 minutes 2 mini boss qui spawnent, 2 vagues d'ennemis intenses, un boss au bout des 10 min, et des "élites" (qu'on a aussi pour le coup). donc ptet concretement que sur le jeu on pourrait étudier des pistes pour dynamiser un peu plus notre jeu. »

## Ce qu'on attend

1. **La chronologie réelle** des 15 premières minutes d'une run de Vestiges, **mesurée** (pas déduite des fichiers) : densité de créatures autour du joueur, apparitions, Résurgences (annonce, début, fin), micro-événements, élites et Souverains (mini-boss), montées de niveau, coffres et lieux, dégâts reçus, éliminations, Essence. Par tranche de 15 ou 30 s, sur plusieurs graines.
2. **Une lecture de cette chronologie** : où sont les pics, les creux, les moments où rien ne change ; combien de « moments » marquants en 10 et en 15 minutes.
3. **La comparaison** avec Megabonk (ce qu'en dit Raphaël, ci-dessus) et Vampire Survivors (vagues minute par minute propres à chaque carte, événements de carte comme les nuées de chauves-souris ou les murs de fleurs, boss à heure fixe, arrêt de l'apparition vers 300 créatures, 500 au plus). Les faits sur les autres jeux viennent de sources citées (wikis, notes de version), sinon ils sont marqués comme une impression de Raphaël.
4. **Trois à cinq pistes concrètes et chiffrées** pour dynamiser les 10–15 premières minutes, chacune avec : le moment visé, ce que voit le joueur, les réglages existants qu'elle touche (ou ce qu'il faudrait ajouter), ses risques (difficulté, lisibilité, performance : plafond de 500 créatures), et une question à poser à Raphaël. Exemples à évaluer, pas à reprendre tels quels : un Souverain à heure fixe (5 et 9 min ?), une vague intense entre deux Résurgences, un boss ou un gardien vers 10 min, une table de vagues par biome, des événements de carte qui traversent l'écran.
5. Le tout dans un plan `doc/plans/30-temps-forts.md` (planche de chronologie en image si elle aide), une ligne au tableau de bord, et les questions pour Raphaël posées avec `AskUserQuestion` à la fin.

## À lire avant tout

- `CLAUDE.md`, `AGENTS.md` : règles du projet, reprise de session.
- `doc/VESTIGES-STRATEGIE-V2.md` : fait autorité sur le gameplay. Notamment §7 (tempo et durée des runs), « Les Résurgences (crises) », les phases (boss vers 20–25 min, endgame). Ne pas proposer de réintroduire ce que le pivot V2 a retiré (jour/nuit, Foyer, craft, construction).
- `doc/plans/DECISIONS.md` (§74, §78 : Péril et difficulté ; §83 : cette demande), `doc/plans/TABLEAU-DE-BORD.md`, `doc/plans/28-peril-et-difficulte.md` (le Péril pèse sur le nombre de créatures).
- Réglages : `data/scaling/spawn_flow.json` (flux continu : cible de 110 créatures + 4 par minute, densité locale, grappes, phases), `data/scaling/crises.json` (première Résurgence à 240 s, puis toutes les 240 s ± 45, annonce 20 s, durée 70 s, rafales, Indicible vers 22 min), `data/events/run_events.json` (chasse, harde, relique tombée, veille, pluie d'éclats), `data/enemies/_variants.json` (élites, Souverains), `data/scaling/peril.json`.
- Code : `scripts/Spawn/SpawnManager.cs`, `scripts/Events/` (`CrisisManager`, `RunEventDirector`).

## Mesurer

- `tools/measure_run.sh <dossier> 900 "42 1002 7 123"` : run sans rendu, temps de jeu accéléré, bot invincible ; écrit `density-<seed>.csv` (par seconde : visibles, proches, en vie, apparues, tuées, niveau, dégâts, Essence…) et une ligne `RESULT` (événements, coffres, lieux, niveaux et leur heure). `MEASURE_EXTRA_ARGS="--nomad --visit"` pour un bot qui avance et visite les lieux ; le protocole du plan 28 (P0) donne un build réaliste avec `--prefer` et `--mortal`.
- Ce qui manque à l'observation sera ajouté à `tools/tests/RunObservation*.cs` (heure de début et de fin de chaque Résurgence et micro-événement, apparition d'élites et de Souverains), comme les autres sondes : en lecture seule, sans changer le jeu.
- `tools/capture_run.sh <dossier> 900 30` en temps réel pour voir à quoi ressemblent les moments clés (regarder les images).
- Lancer les commandes longues par `systemd-run --user --scope --quiet --collect …` (Claude Code tourne dans un scope bridé à 2 cœurs) et vérifier la charge (`uptime`) : Raphaël joue ou compile souvent en parallèle.

## Règles

Étude et propositions uniquement ; aucune modification de réglage ou de système de jeu sans accord. Docs en français, phrases courtes, chiffres. Commit en français (`docs:` ou `test:` pour la sonde), sans trailer `Co-Authored-By`.

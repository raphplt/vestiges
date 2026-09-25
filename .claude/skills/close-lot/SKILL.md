---
name: close-lot
description: Clôt un lot de travail de Vestiges — vérifications, compte rendu dans le plan concerné, retours de Raphaël dans DECISIONS.md, cases de la roadmap V2 §25, puis commit en français. À utiliser quand un lot d'un plan (doc/plans/) est implémenté et qu'il faut le livrer proprement.
argument-hint: "<plan et lot, ex. 08 P2>"
---

# Clôture d'un lot

## Contexte

Diff en cours :
!`git status --short`

Derniers commits :
!`git log --oneline -8`

## 1. Vérifier (et noter les résultats exacts)

- `dotnet build` sans warning (le hook de fin de tour le refait).
- `tools/smoke_test.sh` si scènes, shaders, `project.godot`, initialisation ou assets importés ont changé.
- Régressions pertinentes : `tools/test_movement.sh`, `tools/test_enemy_abilities.sh`, `tools/test_dev_mode.sh`.
- Changement visible : `/capture` avant/après, images regardées.
- Changement de coût : `/bench` (ou la raison pour laquelle la mesure est reportée).
- Diff d'ampleur sur `scripts/`, `scenes/` ou `data/` : sous-agent `godot-reviewer`, remarques traitées ou justifiées.

Un échec n'est jamais passé sous silence : il figure dans le compte rendu.

## 2. Documenter

- **Plan concerné** (`doc/plans/NN-*.md`) : section « <lot> livré — <date> » avec ce qui a été fait, les écarts au plan, les mesures (avant/après, matériel, réserves), les points ouverts. Garder le style des sections voisines : phrases courtes, chiffres, pas de superlatifs.
- **`doc/plans/DECISIONS.md`** : uniquement les décisions et retours explicites de Raphaël, avec leur date. Ne jamais y présenter une proposition comme validée.
- **`doc/plans/README.md`** : une ligne dans la mise à jour du jour si le lot change l'état du dossier.
- **Roadmap V2** (`doc/VESTIGES-STRATEGIE-V2.md` §25) : cocher seulement ce qui est implémenté **et** vérifié. Une validation de design ou un critère de playtest (« fun », « tension ») ne se coche pas sans Raphaël.

## 3. Committer

- Un commit par lot (le code et sa doc peuvent être séparés si la doc précède le code).
- Message en français : `type: sujet` (`feat`, `fix`, `perf`, `docs`, `chore`), puis un corps de 2 à 6 lignes sur le pourquoi et les chiffres clés. **Pas de trailer `Co-Authored-By`.**
- Committer les nouveaux `.cs.uid` et `.import` avec leurs fichiers ; jamais `.godot/`, captures ou planches.
- Pousser seulement si Raphaël l'a demandé dans la session.

## 4. Rendre compte

Ce qui est livré (hashes), ce qui est vérifié et comment, ce qui ne l'est pas, les décisions attendues de Raphaël, le lot suivant proposé.

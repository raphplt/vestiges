---
name: roadmap-sync
description: Réaligne la roadmap V2 (doc/VESTIGES-STRATEGIE-V2.md §25) sur l'état réel du code et signale la dérive entre docs et implémentation. À utiliser en reprise de projet, en fin de lot, ou quand on demande « où en est-on ».
disable-model-invocation: true
---

# Synchronisation roadmap V2

## Contexte injecté

Cases de la roadmap V2 :
!`sed -n '/^## 25\./,/^## 26\./p' doc/VESTIGES-STRATEGIE-V2.md | grep -nE '^###|- \['`

Derniers commits :
!`git log --oneline -15`

## Procédure

1. Pour chaque case non cochée, chercher la preuve d'implémentation dans `scripts/`, `data/` et `scenes/` (classe, JSON, branchement dans `World/GameBootstrap.cs` ou le HUD). Une classe qui existe mais n'est instanciée nulle part ne compte pas.
2. Classer chaque case : **fait** (preuve file:ligne), **partiel** (ce qui manque), **non commencé**.
3. Cocher (`- [x]`) uniquement les cases **faites**. Ne jamais cocher un critère de playtest (« est-ce fun », « tension ») : il exige une validation humaine.
4. Relever la dérive doc/code : sections du GDD, de l'Architecture ou d'AGENTS.md qui décrivent un système supprimé ou absent.
5. Rendre un rapport court : tableau par phase (fait / partiel / restant), prochain item recommandé, liste de dérives. Ne pas réécrire les autres docs sans accord.

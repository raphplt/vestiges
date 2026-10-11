# Menus — Pierre et Effacement

Assets originaux générés le 10 octobre 2026 avec l’outil intégré **imagegen**, puis copiés dans ce dossier. Le rendu du jeu conserve le filtrage nearest ; `MenuSkin` partage les textures et prépare le panneau en 384 × 384 pour ses neuf tranches. Le shader du portrait ne modifie pas les illustrations existantes.

| Fichier final | Source générée |
|---|---|
| `interior.png` | `exec-d430c28e-1b70-4646-93ff-97239db8bbf1.png` |
| `panel.png` | `exec-67531229-3226-4319-947f-77f520270a12.png` |

Les originaux sont conservés sous `~/.codex/generated_images/01a12780-1d08-7001-a77e-c23ec590435d/`.

## Consignes de génération — synthèse

**Fond intérieur.** Décor de menu 16:9 pour VESTIGES, jeu d’exploration en pixel art dans une ville côtière française oubliée. Pierre urbaine usée, lierre dans le coin supérieur gauche, grandes plages indigo/charbon calmes au centre pour les contrôles, bord droit rongé par l’Effacement ivoire. Volumes dessinés, arêtes de pixels, lumière chaude discrète. Aucun texte ni faux bouton, aucune rune ou ornement médiéval, aucun flou de verre. Palette issue de la charte du jeu.

**Panneau.** Surface de lecture carrée et opaque en charbon, bord de pierre ordinaire irrégulier, ébréché, avec ombre et tranche éclairée. Centre presque uni pour le texte, quelques grains près des bords. Extérieur transparent. Aucun texte, icône, bouton, symbole magique ou effet de verre. Prévu pour être découpé en neuf tranches dans Godot.

Les boutons, onglets et curseurs restent des textures déterministes créées une seule fois par `MenuSkin.cs` ; ils ne sont pas des images générées. Les deux PNG de ce dossier sont des assets de production ; la planche `doc/plans/planches/04-menus-personnages-pierre-v2.png` reste une étude.

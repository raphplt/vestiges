# 15 — Audio

Mis à jour le 26 septembre 2026.

## État

51 candidats retenus par Raphaël ; 50 branchés dans le jeu. Le son de frappe du Colosse reste archivé : le déclencheur n’existe plus dans le code actuel. Trois sons actuels conservés : révélation du coffre, sortie du level-up (−3 dB), pas dans l’eau.

L’ancien catalogue compte encore **59 besoins sans choix final** : 53 sans propositions, quatre à reprendre (coup majeur, explosion, Hurleur, échec) et deux en attente (XP, flash). Il comprend musiques et ambiances, des partages possibles et des systèmes à réauditer ; ce n’est pas un engagement à produire 59 fichiers supplémentaires.

## Intégration du 26 septembre

- Banque, volumes et intervalles minimaux dans [sounds.json](../../data/audio/sounds.json), chargés au démarrage. Les sons utilisent les pools existants.
- Choix A à B6 branchés : coups, progression, menus, pas, esquive, armes, POI, relique, éclats, actions ennemies et boss. Son d’arme une fois par geste, avec limitation et gain réduit.
- Clés séparées pour l’esquive et les pas, la relique et la dissolution, les phases du Présage, l’ouverture du coffre et sa mélodie de révélation.
- Dissolution complète sans coupe ; fin d’esquive B5 B avec son atténuation conservée ; cloche B4 B sans filtre. Feuilles sur terrain forêt, gravier sur sol sec de carrière, eau inchangée.
- Interaction refusée : retour sur un POI verrouillé ; l’ancien Autel n’est pas recréé.
- 24 entrées de banque inutilisées et 46 anciens fichiers audio retirés après sauvegarde et comparaison d’empreintes. 83 fichiers audio référencés restent dans la banque, dont les musiques et ambiances provisoires encore utilisées.

[Crédits et provenance](../../assets/audio/CREDITS.json). Le level-up choisi dérive d’un fichier historique dont la provenance reste à confirmer ; cette incertitude est conservée dans les crédits.

## Validation

- Build C# : zéro avertissement et zéro erreur.
- Smoke test : import et démarrage du Hub réussis.
- `godot-mono --headless --path . --script res://tools/tests/AudioBankSmoke.gd` : 76 effets vérifiés (chargement, gains, limitation, UI en pause), plus les sept musiques chargées.
- Captures de vraies runs dans `/tmp/vestiges-audio-integration-run` et `/tmp/vestiges-audio-integration-combat`, seed 221092026 ; seconde run : 35 s, sept ennemis tués, niveau 2 atteint. Ce contrôle technique utilise le driver audio Dummy ; il ne valide pas l’équilibre sonore à l’oreille.

Le contrôle statique ne trouve aucune nouvelle clé manquante. `sfx_rare_fragment` était déjà appelée sans fichier ni registre ; elle reste sans sélection, dans les besoins à rechercher.

## Prochaine étape

1. Écouter une run avec ces branchements et ajuster volumes/cadences avant de reprendre les recherches.
2. Réauditer les 59 entrées restantes contre le gameplay courant et regrouper les sons partageables.
3. Reprendre coup majeur (trois nouvelles pistes, anciennes contraintes abandonnées), explosion courte identifiable et Hurleur plus impactant. Échec : référence artistique à préciser ; XP et flash restent en attente.
4. Musiques : mélodie, rythme et progression ; inspiration Outer Wilds/Celeste/Megabonk. Budget envisagé 0–100 €, aucune dépense autorisée. Options et notes dans l’archive.

## Archive hors dépôt

À la demande de Raphaël, les planches A à B6, 246 propositions, exports de choix, preuves de licence, inventaire historique, ancien plan détaillé et outils de planches sont dans :
`/Users/raph/Documents/Travail/Personnel/Archives/vestiges-audio/2026-09-26/`.

Tous les fichiers déplacés ont été comparés par SHA-256 avant retrait du dépôt. Préparer les prochaines planches dans cette archive, avec le gabarit épuré : titre, déclenchement, lecteurs et choix. Ne plus ajouter les fichiers d’écoute et les documents intermédiaires au repo. Les décisions originales restent intactes dans l’archive ; le repo conserve ce plan, les décisions utiles, la banque et les crédits.

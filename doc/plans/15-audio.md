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

## Correctif du 27 septembre — son des coffres

Retour de Raphaël : « il y a un son d'ouverture du coffre et le son de défilement jusqu'à avoir le résultat ; le premier son dure beaucoup trop longtemps, il ne doit pas se jouer quand le défilement se fait. »

Déroulé constaté dans `ChestLootScreen` :
- le clic d'ouverture (`chest_open`, 0,9 s) part à l'ouverture ;
- la mélodie de révélation (`chest_opening.wav`, 5 s, forte pendant 4 s) partait dès que la **première** ligne s'arrêtait, alors que les suivantes défilaient encore ;
- elle continuait environ 2,5 s après la fermeture de l'écran, sur la reprise du jeu.

Correctif :
- la mélodie ne part qu'une fois **toutes** les lignes arrêtées ;
- elle s'éteint en fondu de 0,5 s à la fermeture de l'écran (`AudioManager.PlayUI` rend désormais son lecteur, `AudioManager.FadeOutUI` l'éteint s'il joue encore ce son).

Le clic d'ouverture est inchangé. **À réécouter en jeu** : si c'est lui que Raphaël désignait comme « premier son », le couper dès le début du défilement.

## Correctif du 28 septembre — son des coffres, ordre rétabli

Retour de Raphaël : « il y a deux sons, l'un avec une mélodie qui doit se jouer quand on a lancé l'ouverture du coffre pendant que les items défilent, et l'autre qui est un bruit qui doit se jouer en premier […] le bruit pendant le défilement se joue trop tard. »

Le correctif du 27 septembre avait mal lu le premier retour : en attendant l'arrêt de toutes les lignes, il faisait partir la mélodie 2,5 à 3,3 s après l'ouverture, une fois le défilement fini.

Ordre désormais :
1. le clic d'ouverture (`chest_open`, attaque dans les 0,3 premières secondes) ;
2. la mélodie (`chest_opening.wav`) 0,3 s plus tard, **pendant** le défilement ;
3. son fondu de 0,5 s à la fermeture de l'écran, inchangé.

Garde : un minuteur en retard ne relance jamais la mélodie sur un coffre suivant. **À réécouter en jeu.**

## Bouclier — 28 septembre

Raphaël fournit deux sons pour le bouclier du lot 8B (plan 03 §8) : `shield-block.mp3` et `shield-break.mp3`. Intégrés en WAV 48 kHz dans `assets/audio/sfx/joueur/` :
- `sfx_shield_block` : le bouclier encaisse un coup et tient. L'original durait 1,8 s dont 1,5 s de silence, pic à −12,7 dB : coupé à 0,45 s avec un fondu, remonté à −4 dB ;
- `sfx_shield_break` : le bouclier tombe à zéro. Coupé à 0,5 s, niveau d'origine (−1,4 dB).

Déclenchés par `AudioManager` sur `PlayerShieldChanged` quand le bouclier baisse. La recharge reste muette, et un coup encaissé ne joue pas le son de blessure. **À écouter en jeu**, niveau compris.

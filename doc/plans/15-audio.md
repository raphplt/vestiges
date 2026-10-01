# 15 — Audio

Mis à jour le 1er octobre 2026. **Nouvelle priorité : chantier audio d'ensemble et identité des Résurgences**, détaillés en fin de plan (DECISIONS §44).

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

## Étapes identifiées le 26 septembre

Ces besoins restent à reprendre dans le chantier élargi du 1er octobre, décrit en fin de plan.

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

## Reprise audio et identité des Résurgences — 1er octobre 2026

[DECISIONS §44](DECISIONS.md) : **l'audio est le prochain gros chantier souhaité**, musique comme effets sonores. Les Résurgences constituent un problème concret : on ne comprend pas assez qu'elles ont lieu, malgré les signes visuels existants. Le chantier dépasse les quelques sons encore à choisir. Coordination : [plan 24 §12, R4](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026).

**Ordre de travail proposé, à entreprendre :**

1. **Écoute de référence en vraie run.** Enregistrer avec une sortie audio réelle l'exploration, le combat dense, une Résurgence complète et les interfaces. Relever ce qui manque, se masque, se répète ou intervient trop tard. Les captures avec le driver Dummy ne constituent pas une écoute.
2. **Premier lot : un cycle de Résurgence.** Travailler les signes annonciateurs, le déclenchement, une identité sonore pendant la phase active et une transition sensible vers l'accalmie. Coordonner musique, ambiances et effets avec les signes visuels du plan 08. Ne pas réintroduire par défaut les bandeaux textuels retirés au plan 24 A3, ni augmenter indistinctement le volume de tout le mix.
3. **Musique et ambiances.** Revoir leur identité, leurs transitions et leur place dans la run ; réauditer les besoins contre le gameplay courant avant de rechercher des pistes.
4. **Effets et mixage.** Reprendre impact des coups, attaques ennemies, retours du joueur, récompenses et UI ; vérifier les priorités sonores, répétitions, volumes et sons simultanés en combat dense. Réintégrer les choix encore ouverts dans cet audit.

Les 50 choix déjà branchés restent la base de travail ; le retour n'annule pas toutes les sélections antérieures. Pour chaque lot, livrer une séquence audible avant/après et décrire ce qui a changé. L'acceptation vise une Résurgence reconnaissable, un combat lisible à l'oreille et une écoute soutenable sur une run complète. Aucun fichier sonore ni réglage n'est modifié par cette mise à jour du plan.

### R4 — préparation livrée, écoute à entreprendre

Reprise demandée en [DECISIONS §45](DECISIONS.md). **Aucun son, gain ou comportement audio n'est modifié dans ce lot de préparation.** Les constats ci-dessous viennent du code et des fichiers ; ils ne constituent pas une écoute. Les 50 choix intégrés restent la base. Les captures UI réalisées pour R1/R2 sont muettes et ne valident rien ici.

#### Diagnostic du cycle actuel

| Moment | Déclenchement actuel | Point à traiter |
|---|---|---|
| Annonce, 20 s avant la crise | `OnCrisisWarning` lance `mus_crepuscule` (fondu 2 s) et `sfx_danger_building` (−5 dB supplémentaires) | L'état reste `Exploration` : `_Process` appelle `RefreshExplorationMusic` toutes les 120 images et remplace le morceau d'annonce. À 60 images/s, cela peut intervenir en moins de 2 s, avant la fin du fondu. Le délai dépend du framerate. |
| Début et phase active, 70 s | `RunPhaseChanged(Crisis)` puis `CrisisStarted` demandent tous deux `mus_nuit_vagues` et la baisse d'ambiance | La seconde demande musicale est ignorée car la clé est déjà courante ; le fondu réellement retenu est celui de 3 s. Aucun effet de début distinct dans ces handlers. Écouter l'attaque du morceau avant de choisir un nouveau signal. |
| Fin et accalmie de récompense, 30 s | `CrisisEnded` est émis **avant** le retour de phase ; `OnCrisisEnded` relit encore l'ancienne phase. Le changement de phase suivant relance l'exploration adaptative | Pas d'état musical d'accalmie ; le combat peut revenir immédiatement. Ne pas augmenter les volumes pour compenser l'absence de contraste. |
| Exploration/combat | Seuil de trois ennemis sur `_activeEnemyCount`, incrémenté aux apparitions, décrémenté aux morts | Les retraits lointains par `CullFarDayEnemies` → `EnemyPool.Return` ne sont pas des morts et ne décrémentent pas ce compteur. Il peut rester supérieur à la population réelle ; ce n'est pas un indicateur de combat local. |
| Résurgences tardives | L'annonce audio ne traite que `Exploration` | Vérifier explicitement LateGame/Endgame et le retour au Hub, pas seulement la première crise. |

Sources : [AudioManager](../../scripts/Infrastructure/AudioManager.cs), [CrisisManager](../../scripts/Events/CrisisManager.cs), [SpawnManager](../../scripts/Spawn/SpawnManager.cs), [EnemyPool](../../scripts/Spawn/EnemyPool.cs), [réglages des crises](../../data/scaling/crises.json).

La banque contient bien les clés utilisées. Durées lues dans les fichiers avec `ffprobe` : exploration 238,72 s, combat 136,96 s, annonce 174,68 s, Résurgence 290,96 s ; danger 2,60 s. Ces durées ne disent rien de leur qualité musicale ni de leur audibilité. Le signal de danger utilise le pool SFX commun, qui coupe la voix la plus ancienne s'il est plein : masquage ou interruption restent **à écouter**, pas constatés à l'oreille.

Le visuel `CrisisOmen` monte déjà de 0 à 0,8 pendant l'annonce, passe à 0,9 en 1,2 s au début et s'efface en 2,5 s à la fin ; les créatures accélèrent leur animation pendant l'annonce. Conserver ces repères pour synchroniser l'audio, puis décider sur une capture si un complément visuel est nécessaire. Aucun bandeau textuel réintroduit par défaut.

#### A0 — référence audible avant correction

1. Enregistrer une vraie `Main` pendant **360 s de jeu**, seed 221092026, Traqueur, profil isolé, volumes consignés. Exploration avant 220 s, annonce 220–240, crise 240–310, accalmie 310–340 ; les horodatages des signaux font foi si un événement ou une pause décale l'horloge.
2. Utiliser une sortie réelle. `tools/capture_run.sh` impose actuellement `--audio-driver Dummy` : prévoir un paramètre explicite pour le driver et une capture du flux propre au jeu. Linux dispose de `ffmpeg`, `pactl` et PulseAudio via PipeWire ; préférer une sortie temporaire dédiée au seul processus Vestiges, sans changer la sortie par défaut ni enregistrer les autres applications. Fenêtre uniquement avec `VESTIGES_SCREEN=1`, après vérification que l'index désigne toujours le ViewSonic. Écouter ensuite le fichier, avec les images de la run.
3. Journaliser les signaux Warning/Started/Ended, la phase et les changements de clé musicale. Cette trace permettra de reproduire le remplacement de l'annonce ; elle ne remplace pas l'écoute.
4. Noter, avec horodatages : moment où l'événement devient reconnaissable, sons masqués, répétition fatigante, clarté des attaques et du retour au calme. Faire une passe mix complet, puis une passe musique/ambiances seules si le diagnostic reste ambigu. Capturer aussi une ouverture de coffre et un level-up pour vérifier les transitions UI.

Livrable A0 : enregistrement audible complet, trace des événements et courte fiche d'écoute. Les médias et candidats restent **hors dépôt**. Le chemin d'archive historique ci-dessus est celui du Mac ; pour l'écoute Linux, utiliser un dossier local d'audition, par exemple `/home/raphael/.local/share/vestiges-audio/2026-10-01/`, puis archiver avec la même traçabilité. Aucun enregistrement A0 n'a été produit dans cette préparation.

#### A1 — stabiliser le pilotage avec la banque actuelle

- Séparer l'intention musicale (exploration, combat, annonce, Résurgence, accalmie) de la phase gameplay. Une annonce conserve sa priorité jusqu'au début réel, une annulation ou une transition supérieure (mort, sortie de run, boss selon le contexte).
- Centraliser la résolution de cette intention pour éviter les demandes concurrentes de `RunPhaseChanged` et `CrisisStarted`. Traiter la fin après résolution de la nouvelle phase ; préserver LateGame/Endgame.
- Remplacer le rythme de 120 images par du temps de jeu et une hystérésis configurable. Calculer la présence ennemie utile depuis un cache à fréquence bornée, en tenant compte des retraits ; aucune recherche de groupe par frame, aucun faux `EnemyKilled` envoyé pour compenser un retrait.
- Prendre les durées d'annonce et d'accalmie dans les données de crise ou les signaux existants, sans dupliquer des constantes. Les écrans en pause ne doivent pas consommer ces fenêtres.
- Conserver les pistes déjà branchées pour entendre d'abord la différence de transitions. Vérifier 30/60/144 images/s, pause pendant annonce et crise, retour au Hub, mort, deuxième crise et crises tardives.

Livrable A1 : correction technique isolée, régression des priorités/transitions et écoute avant/après de la même séquence A0. `AudioBankSmoke` vérifie chargement et gains ; build/smoke restent requis. **Aucun de ces tests ne valide seul le mix.**

#### A2 — identité de la Résurgence, premier lot artistique

| Phase | Résultat recherché | Proposition à écouter |
|---|---|---|
| Annonce | Reconnaissance immédiate, tension croissante sur les 20 s | Motif court identifiable puis arrangement qui se densifie ; signal existant gardé comme référence |
| Début | Rupture perceptible sans masquer une attaque | Accent d'entrée synchronisé au début réel, intégré musicalement |
| Active | Énergie tenue, distincte de l'exploration en combat | Mélodie, rythme et variations sur 70 s, selon le brief Résurgence du guide audio ; espace laissé aux ennemis |
| Accalmie | Soulagement audible et envie de repartir | Retrait de couches et reprise d'un motif d'exploration ; écouter le contraste avec les ennemis encore présents |

Choix à l'oreille en contexte, par petits groupes de propositions comparables en niveau. Les préférences et licences suivent le [guide audio](../AUDIO-GUIDE.md) ; aucune dépense ou substitution générale des 50 choix n'est engagée. Essayer le cycle complet avant d'étendre à toutes les musiques. Si les sources sont indépendantes, vérifier raccords et cohérence musicale plutôt que supposer qu'elles partagent un thème.

#### A3 — lisibilité du mix puis extension

Après le cycle : priorités des signaux de danger, voix simultanées en foule, répétitions d'armes/impacts, retours joueur, gains et UI. Préserver l'ordre sonore établi du coffre (clic, mélodie à +0,3 s, fondu à la fermeture), ainsi que sa rotation visuelle fluide. Refaire une run complète au mix final, puis réauditer les 59 besoins historiques avant de chercher de nouveaux fichiers.

Recette : une Résurgence se reconnaît sans lire d'annonce, l'entrée et la sortie s'entendent, les attaques restent lisibles et la répétition est soutenable. Noter les limites d'écoute ; ne cocher les cases audio de la roadmap qu'après intégration **et** écoute réelle. **État au terme de R4 : préparation prête ; A0 à A3 non réalisés.**

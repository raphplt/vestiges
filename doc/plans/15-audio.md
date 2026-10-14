# 15 — Audio

Mis à jour le 10 octobre 2026. **24 armes sonorisées ; quinze choix de sons manquants intégrés (M4a/M4b), quatre besoins refusés à reprendre.** Fond de forêt conservé à la demande de Raphaël : boucle entière de 8 s (M5a), gain −2 dB et lecture seulement en Forêt Reconquise (M6). Huit sons de Barrière validés pour l'instant. La recette artistique du mix reste ouverte ; suite détaillée en fin de plan.

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

Recette : une Résurgence se reconnaît sans lire d'annonce, l'entrée et la sortie s'entendent, les attaques restent lisibles et la répétition est soutenable. Noter les limites d'écoute ; ne cocher les cases audio de la roadmap qu'après intégration **et** écoute réelle. **État au terme de R4 : préparation prête ; A0 à A3 non réalisés.** Suite : A0 et A1 livrés ci-dessous.

### A0 et A1 — livrés le 1er octobre (soir)

Travail mené seul sur demande de Raphaël (« avance le plus que tu peux sur la partie audio, en autonomie, en suivant les plans »). **Aucun son n'a été écouté par l'agent** : les constats ci-dessous viennent des enregistrements mesurés, de la trace et du code. L'écoute reste à faire par Raphaël, sur les fichiers indiqués.

#### Outil d'enregistrement

Plutôt qu'une sortie PulseAudio dédiée, `tools/record_run_audio.sh` utilise le **Movie Maker de Godot** : chaque image et le mixage du moteur sont écrits ensemble, à 30 i/s, sans passer par la sortie son du système ni enregistrer d'autre application. C'est exactement ce que le jeu envoie à la carte son ; la fenêtre s'ouvre sur le ViewSonic (`VESTIGES_SCREEN=1`, position vérifiée dans le log). Le rendu tourne plus lentement que le temps réel (≈ 0,45×), d'où deux ajustements :

- `AudioManager.NowMsec` : les intervalles minimaux des sons et la chaîne de ramassage d'XP suivent les images rendues sous Movie Maker, sinon la limitation aurait été 2,2 fois trop faible dans l'enregistrement. Hors Movie Maker, l'horloge reste `Time.GetTicksMsec()`.
- Le bot lit chaque écran qui fige la run pendant 2 s (`--choice-delay`) au lieu de choisir à l'image suivante, pour entendre l'entrée du level-up.

Sorties hors dépôt : `run.mp4` (images + son), `audio.flac`, trace (`audio-events.csv` : signaux, phases, musiques, chaque son et son issue ; `audio-states.csv` : état par seconde ; `audio-sounds.csv` : bilan par clé) et `rapport.md` (`tools/audio_report.py` : sonie EBU R128, spectrogramme de chaque Résurgence, sons par moment). Options du banc : `--audio-trace`, `--choice-delay`, `--mute-buses` (passe d'une famille de sons), `--music-config` (variante de réglages), `--crisis-at` (cycle court). `tools/audio_planche.py` monte une planche d'écoute locale. Les mesures situent un moment et comparent deux fichiers ; elles ne jugent pas le son.

Volumes consignés : profil neuf, bus Master 0 dB, Music −6 dB, SFX 0 dB, Ambiance −8 dB (valeurs par défaut, aucun réglage utilisateur).

#### A0 — référence avant correction

Dossier : `/home/raphael/.local/share/vestiges-audio/2026-10-01/a0-avant/`. Seed 221092026, Traqueur, 360 s de jeu (438 s d'enregistrement avec chargement et écrans), bot nomade qui visite les lieux. Annonce à 265,6 s d'enregistrement, début à 290,7 s, fin à 374,0 s.

| Constat (trace et mesures) | Conséquence |
|---|---|
| **Aucune musique de 0 à 290 s.** À la première run d'une session, la phase vaut déjà `Exploration` : aucun `RunPhaseChanged`, donc ni musique d'exploration, ni ambiance de forêt, ni oiseaux. En jeu réel, la musique du Hub continue à sa place. | La première Résurgence arrive sans contraste musical possible. |
| **Annonce muette** : `OnCrisisWarning` exigeait la phase `Exploration`, inconnue de l'AudioManager ; ni `mus_crepuscule` ni `sfx_danger_building`. | Rien ne prévient à l'oreille pendant les 20 s d'annonce. |
| Début : `mus_nuit_vagues` démarre bien avec la crise. Fin : passage direct à `mus_jour_combat`. | Pas d'accalmie musicale. |
| Compteur d'ennemis de l'AudioManager : jusqu'à **1 022** au-dessus des créatures actives (1 684 apparitions, 584 morts). | La musique de combat aurait été permanente dès que la phase était connue. |
| Sonie court terme stable autour de −15/−20 LUFS du début à la fin, sans marche à l'annonce ni au début. | La Résurgence ne se distingue pas par le niveau ; les effets portent le mix. |

Mesures A3 tirées de la même trace : **12,3 sons/s** en moyenne dans le pool de 12 voix, **64 voix coupées par minute**, 12 voix ou plus demandées 8 % du temps. Les plus fréquents : `sfx_hit_ennemi` 2,7/s (et 1 992 demandes limitées), lancer 1,6/s, `xp_gain` 1,2/s, arc 1,2/s, lame 1,0/s, tirs ennemis 0,7/s.

Mesures des fichiers (sonie momentanée, avant le bus Music à −6 dB) :
- `mus_crepuscule` frappe à 0–2 s (−11 LUFS) puis retombe vers −36/−38 LUFS de 9 à 18 s ; la frappe suivante tombe à 19–20 s, au moment où la crise remplace le morceau. L'annonce de 20 s fait donc surtout entendre une retombée vers le quasi-silence.
- `mus_nuit_vagues` ouvre par une introduction avec un creux à −31/−39 LUFS entre 6 et 10 s ; le morceau ne devient dense (−10 à −20) qu'après ~18 s.
- `mus_jour_exploration` commence par 5 s presque muettes (−63 à −41 LUFS).
- Ancien fondu enchaîné : les décibels passaient linéairement de −80 à 0. À mi-parcours, les deux morceaux étaient vers −40 dB, d'où un creux au milieu de chaque transition.

#### A1 — pilotage stabilisé, banque inchangée

- `MusicDirector` (composant extrait de l'AudioManager) résout une **intention** : Hub, exploration, combat, accalmie, LateGame, endgame, annonce, Résurgence, mort, de la plus faible à la plus forte. Il lit l'état de `GameManager` et les signaux de crise, pas la phase seule. L'annonce garde la main jusqu'au début réel ; les signaux d'une même image (fin de crise, accalmie, nouvelle phase) ne donnent qu'une résolution, en fin d'image, donc un seul fondu.
- Réglages dans `data/audio/music.json` : morceau, fondu et point d'entrée (`start_sec`, 0 partout) par intention ; seuils du combat.
- Combat : créatures actives à moins de 600 px, relevées toutes les 0,5 s de **temps de jeu**, entrée à 4 tenues 1 s, sortie à 1 ou moins tenue 6 s. Un retrait au pool compte. Plus de rythme à 120 images ni de compteur d'apparitions et de morts.
- Accalmie : nouveau signal `CrisisCalmChanged`, émis par `CrisisAftermath`, qui tient déjà sa durée (`calm_essence_seconds`, 30 s) et se fige avec la pause. Pendant l'accalmie, la musique d'exploration revient (fondu de 4 s) même en foule ; LateGame et endgame gardent leur fond.
- Endgame : la musique sort désormais de la crise (elle restait sur `mus_nuit_vagues`, la phase ne changeant pas).
- Première run : exploration, ambiance et oiseaux démarrent sans changement de phase ; `sfx_danger_building` sonne à chaque annonce, quelle que soit la phase.
- Fondu enchaîné à puissance constante en amplitude (sinus/cosinus) ; un fondu interrompu repart du niveau atteint ; le ralenti de la mort n'étire plus le fondu.
- Tirages aléatoires de l'audio (hauteur, oiseaux, dissolution) sur un générateur propre : ils ne consomment plus celui du gameplay. Contrôle : deux runs headless de même seed, l'une sans bus SFX, restent identiques 76 s ; la divergence qui suit vient d'ailleurs (minuteries en temps réel, physique), pas de l'audio.
- Nettoyage : `PlayDeathStinger` (sans appelant) retiré. Plus aucun ennemi `colosse_*` dans `data/enemies` : branche d'ambiance et entrée `sfx_colosse_lointain` retirées de la banque ; le fichier reste dans `assets/audio/sfx/ambiance/` pour l'audit A3.

Vérification : `tools/test_music.sh`, 22 contrôles verts. Ils couvrent le Hub au démarrage, la première run, l'entrée et la sortie du combat par présence (retrait au pool), l'annonce tenue 18 s à 30, 60 et 144 i/s en foule, les pauses pendant l'annonce et la crise, la fin vers l'accalmie, le retour du combat, une seconde crise, LateGame, endgame, la mort pendant une annonce, le Hub et une seconde run. Build sans avertissement, smoke vert. `AudioBankSmoke` : 77 effets OK. Ce contrôle était cassé depuis le 27 septembre, car l'appel GDScript n'avait pas suivi le paramètre `basePitch` de `PlaySfx` ; il est corrigé. Relecture `godot-reviewer` traitée : fondus, configuration tolérante aux clés manquantes, horloge. Banc : `--choice-delay` compte désormais des pas d'image ; en temps réel, un cycle headless accéléré restait figé sur l'écran de niveau.

**Après**, même seed et même banc : `/home/raphael/.local/share/vestiges-audio/2026-10-01/a1-apres/`. Exploration à 0 s, combat à 9 s, annonce `mus_crepuscule` au signal (261,8 s) tenue jusqu'au début (287,0 s), `mus_nuit_vagues`, puis exploration en accalmie à la fin (371,2 s), et retour du combat 44 s plus tard. Les deux runs ont divergé en combat (812 contre 1 092 coups joués) : à ce moment, les tirages aléatoires de l'audio consommaient encore le générateur global du gameplay (corrigé depuis, voir ci-dessus).

**Constat de design pour Raphaël.** Dans cette run, le bot a en médiane 31 créatures à moins de 600 px dès la première minute (jamais une ou moins après 60 s). La bascule exploration/combat au nombre d'ennemis donne donc la musique de combat presque toute la run : 9 s d'exploration avant la première Résurgence. L'ancien compteur aboutissait au même résultat. Rehausser le seuil ne ferait qu'alterner les deux morceaux au gré des vagues. Les seuils actuels ne sont pas une décision : il faut choisir le rôle de chaque morceau (fond de run unique, combat réservé aux pics, superposition de couches…) à l'écoute.

#### A2 — premières propositions, à choisir à l'oreille

Sans nouveau fichier ni choix artistique automatique, une seule dimension est proposée : **le point d'entrée** dans les morceaux d'annonce et de Résurgence (`start_sec` de `music.json`), d'après les mesures de A0. Trois cycles courts, même seed, crise avancée à 70 s de jeu (`--crisis-at 70`), seule la variante change (`--music-config`) :

| Variante | Annonce (`mus_crepuscule`) | Crise (`mus_nuit_vagues`) | Intention |
|---|---|---|---|
| V0 | début du morceau | début du morceau | référence A1 |
| V1 | 38 s : frappe à −12 LUFS, puis retombée plus courte | 18 s : entrée sur la montée vers −11 LUFS | rupture audible au début de crise |
| V2 | 52 s : passage continu, −16 à −26 LUFS | 34 s : partie dense du morceau | annonce tenue sur 20 s, crise pleine dès l'entrée |

Sonie du mix complet au début de crise (5 premières secondes) : −17,6 (V0), −16,4 (V1), −16,0 LUFS (V2). Les effets du combat dominent le mix, ces écarts ne départagent rien.

**Planche à écouter** : `/home/raphael/.local/share/vestiges-audio/2026-10-01/planche-resurgence.html`, à ouvrir dans un navigateur. Elle donne cinq extraits avec images, de 30 s avant l'annonce à 30 s après la fin : A0, A1, V0, V1, V2. Rapports, sonie et spectrogrammes sont à côté, dans chaque dossier. **Production par défaut inchangée** (`start_sec` à 0) tant que Raphaël n'a pas choisi.

Questions pour Raphaël, dans l'ordre :
1. A1 contre A0 : l'annonce et le retour au calme se reconnaissent-ils désormais ? Les fondus sont-ils propres ?
2. V0, V1 ou V2 : quelle entrée pour l'annonce et pour la crise, ou aucune ?
3. Exploration et combat : quel rôle pour chaque morceau avec une foule quasi permanente (voir A1) ?
4. Accent d'entrée au début de crise : faut-il un son dédié ? Il manque à la banque ; une recherche de candidats sourcés suivrait le [guide audio](../AUDIO-GUIDE.md) §4.

Les morceaux actuels restent provisoires. Les nouvelles pistes (briefs C et D du guide) et le choix d'un accent restent à faire par Raphaël. A3 (mix) attend ces choix ; ses premiers chiffres sont donnés en A0.

## Retours du 4 octobre (soir)

Voir [DECISIONS §66](DECISIONS.md). Faits : cri du Hurleur retiré ; sons d'attaque des armes baissés de 6 dB (13 clés, −8 → −14 dB). À faire, dans l'ordre proposé :
1. **Un son par arme**, plus discret que l'impact. Aujourd'hui, 24 armes se partagent 13 sons et 6 sont muettes. Dans le genre, le tir reste bas et court, ce sont l'impact et la mort qui portent le retour ; une arme très rapide joue un son sur deux ou plus bas.
2. **Mixage** (A3) : 12 sons par seconde et 64 voix coupées par minute relevés en A0. Priorités entre familles (joueur, créatures, monde, interface).
3. **Sons manquants** : relevé par écoute d'une run enregistrée (`tools/record_run_audio.sh`).
4. **Musiques et ambiance** : plus tard, à la demande de Raphaël.

**Cri du Hurleur, essai de synthèse (4 octobre, soir).** `tools/generate_hurleur_cry.py <dossier>` produit trois variantes, sans rien brancher dans le jeu. Toutes sont une voix synthétique (voyelle « ou » vers « o », vibrato, souffle, réverbération), filtrées sous 3,5 kHz, crête à −6 dBFS :
- A, plainte : 2,5 s, −12,8 LUFS ;
- B, appel en deux élans : 2,5 s, −14,7 LUFS ;
- C, chœur grave désaccordé : 2,9 s, −15,6 LUFS.

Les fichiers sont dans `~/Téléchargements/vestiges-hurleur-cri/`. Spectres vérifiés (rien d'aigu), aucune écoute faite par Claude. Si Raphaël en retient une : la copier dans `assets/audio/sfx/creatures/`, l'ajouter à la banque avec un volume bas, la remettre dans `cry_audio` du Hurleur, puis l'écouter en run.

**Retenu : C** (DECISIONS §66). `sfx_hurleur_cri.wav` remplacé par la variante C, banque à −6 dB, cri rétabli sur la fiche du Hurleur. Smoke, capacités ennemies et musique verts ; écoute en run à faire par Raphaël.

## Reprise du 7 octobre — lots proposés avant implémentation

Demande : reprendre en autonomie mixage, un son par arme, ambiances, menus et musiques (DECISIONS §76). Les choix déjà retenus restent la matière de départ. Les médias d'écoute vont dans `~/.local/share/vestiges-audio/2026-10-07/`. Le fichier `ChestLootScreen.cs` comporte une modification préexistante : la préserver.

1. **A3a — mixage et continuité.** Référence audible de 125 s, seed 221092026, Traqueur, crise avancée à 45 s. Limiter les voix simultanées par son, protéger les alertes et blessures par priorité configurée en JSON ; appliquer la limitation aussi aux menus, annuler un ancien fondu avant réemploi d'un lecteur UI. Corriger les ambiances qui persistent au Hub, les fondus concurrents et les gains ignorés par les boucles/la musique. Régressions ciblées, build, smoke, musique, puis même enregistrement après.
2. **A3b — une identité par arme.** Réauditer les 24 armes actuelles (18 sonorisées par 13 clés, six muettes). Préparer des variantes courtes et discrètes à partir des sources existantes, avec provenance et comparaison hors dépôt. Intégration des nouveaux timbres selon la préférence demandée à Raphaël ; aucune dépense. Vérifier un déclenchement par geste, y compris les armes spéciales.
3. **A3c — menus, ambiance et musique.** Étendre le retour de navigation au clavier/manette ; tester le coffre sans modifier sa séquence. Préparer une écoute séparée des fonds musicaux et ambiances, puis en contexte avec le mix corrigé. Conserver les points d'entrée actuels des musiques en l'absence d'un choix d'écoute, et distinguer les corrections techniques de la sélection artistique.

Un lot est vérifié avant le suivant. Les mesures ne valent pas écoute : le bilan précise ce qui reste à écouter et ne coche pas les objectifs artistiques sur la seule base des tests.

### Livré — 7 octobre

**A3a, protections techniques intégrées.** `AudioVoicePool` conserve les 12 voix monde et les trois voix UI préallouées. `sounds.json` définit `priority` et `max_voices` : une demande ne coupe qu'une voix moins prioritaire ; à égalité elle attend, et le plafond par son empêche l'empilement. Les blessures, boucliers et annonces importantes sont prioritaires. Les demandes refusées sont distinguées des demandes limitées par intervalle dans la trace et le rapport.

- UI : l'intervalle minimal fonctionne également en pause, avec un historique de déclenchement indépendant des demandes monde. Un lecteur réutilisé annule son ancien fondu, qui ne peut plus l'arrêter après coup.
- Ambiance : oiseaux sur le bus Ambiance ; arrêt des fonds et alertes de run au Hub ; annulation d'un fondu d'ambiance lors du retour à l'exploration ; gain de banque appliqué aux boucles. L'ambiance ne redémarre plus sur une fin de crise différée après sortie de run.
- Musique : le gain de chaque piste de `sounds.json` est désormais respecté pendant le fondu. Aucun morceau, point d'entrée ou gain musical n'a été changé.
- Menus (A3c, partie technique) : les boutons communs sonnent aussi au focus clavier/manette ; les onglets des paramètres sont branchés. Survol et focus simultanés sont limités par la même cadence. La séquence du coffre et les modifications préexistantes de son écran sont préservées.

**Mesure avant/après.** `mix-avant/` et `mix-apres/` dans le dossier d'écoute : même outil `record_run_audio.sh`, seed 221092026, Traqueur, 125 s de jeu, `--crisis-at 45`, profil neuf et volumes par défaut. Le Movie Maker enregistre le mix moteur, indépendamment de la charge de la machine. La référence a été lancée avant les modifications ; l'après utilise un checkout isolé de la même base (`0eefb459`) avec les seuls changements audio, pour ne pas embarquer le chantier Péril mené en parallèle.

| Mesure | Avant | Après |
|---|---:|---:|
| Durée enregistrée, pauses comprises | 146,6 s | 153,2 s |
| Interruptions de voix monde | 27 | 4 |
| Demandes refusées par le plafond/priorité | 0 | 209 |
| Sons joués, UI comprise | 1 188 | 1 070 |
| Sonie moyenne mesurée | −16,7 LUFS | −16,6 LUFS |

Les combats et choix du bot divergent (Boussole avant, Faucille après) : ces valeurs sont une observation de deux runs comparables, **pas une mesure causale de gain en pourcentage**. Parmi les 209 refus : 102 pas d'herbe, 34 XP, 29 attaques d'Ombre, 26 dissolutions. Le niveau global reste proche : ce lot protège les voix, il ne constitue pas une validation du mix à l'oreille. Les tests déterministes prouvent séparément priorité, plafond et absence de coupure tardive.

**Vérification.** `tools/validate.sh /tmp/vestiges-audio-validation-complete-20261007 smoke music audio` passe dans le dossier principal : 3/3 suites, sources stables, build à zéro avertissement/erreur. Suite `audio` ajoutée au lanceur global : 77 effets chargés, puis 13 contrôles de mix (saturation, priorité, plafond, fondu UI, pause, gains, retour au Hub). Les régressions musicales existantes passent. Huit tests des lanceurs passent aussi. Un premier build avait rencontré la migration concurrente de `PerilDataLoader` ; l'isolation a permis d'avancer, puis la validation du dossier partagé est devenue verte. Aucun changement de gameplay du chantier Péril n'a été retouché pour l'audio.

**A3b, préparation artistique achevée ; intégration en attente d'écoute.** `tools/prepare_weapon_audio.py <dossier hors dépôt>` produit 72 candidats (A court, B matière, C feutré), trois par arme, avec aperçu à cadence répétée au gain proposé de −14 dB, référence actuelle ajustée au même niveau d'écoute et export JSON des choix. Quatre petits lots de six armes. Chaque candidat conserve les crédits CC0 des sources déjà retenues, leurs empreintes, la recette et l'empreinte du résultat ; les lamelles de Boîte à musique et de Baguette ajoutent une synthèse originale. Contrôles : 72 empreintes distinctes, WAV mono 48 kHz, absence d'écrêtage et extrémités fondues. Cela ne constitue pas une écoute artistique.

Six armes restent muettes en production : Boîte à musique, Polaroïd, Baguette de sourcier, Gants de boxe, Craies, Transistor. À leur intégration, la Boîte doit sonner au contact d'une orbite (cadence bornée) et le Transistor au départ du cône ; ces deux chemins ne passent pas par `PlayAttackFeedback`. Les 22 autres passent par le retour d'attaque ponctuel. Ne pas ajouter un son par projectile d'une salve ni une boucle permanente.

**Page unique d'écoute :** `~/.local/share/vestiges-audio/2026-10-07/index.html` : deux vidéos de mix, planches des armes, puis 20 extraits de musique/ambiance séparés (avec les points d'entrée déjà proposés pour la Résurgence). Aucune nouvelle musique composée, aucun nouveau timbre branché. La question du degré d'autonomie artistique a été posée ; en l'absence de réponse, maintien des choix à l'écoute prévus au guide audio. Aucun son n'a été écouté par l'agent.

**Suite :** retour d'écoute du mix, choix des timbres par petits lots et branchement des six armes manquantes ; puis choix du rôle de la musique d'exploration/combat et remplacement des fonds provisoires. Le signal de fragment rare (`sfx_rare_fragment`) reste un besoin sans fichier : ne pas le compter comme couvert.

### Retour d’écoute — frottement dans le mix après

Raphaël signale « une sorte de bruit de fond constant comme un frottement » dans `mix-apres`. Source non identifiée : horodatage demandé, fonds et bruitages isolés dans `~/.local/share/vestiges-audio/2026-10-07/frottement/index.html`. Les mesures et les tests techniques ne permettent pas de conclure à sa place. Ne pas considérer le mix comme recetté ; aucun son supprimé ou atténué au hasard.

## A3b — choix des armes et deuxième recherche, 7 octobre

Export de Raphaël conservé octet pour octet hors dépôt : `~/.local/share/vestiges-audio/2026-10-07/retours/choix-armes-2026-10-07.json`.

Lots proposés avant modification :
1. **A3b1 : intégrer les neuf choix explicites**, avec clés propres aux armes, fichiers identiques aux candidats écoutés, gain proposé de −14 dB, une voix et intervalle de 180 ms ; conserver leurs sources/crédits. Contrôler les attaques ponctuelles et les salves, puis enregistrer une galerie audible des neuf armes.
2. **A3b2 : nouvelle recherche pour les quinze refus.** Les variations des mêmes anciens sons ne suffisent pas. Chercher des matières nouvelles adaptées aux objets (déclic/bille, armature de parapluie, pelle, câble, vaisselle, dents de râteau, cloche, obturateur) ; notes distinctes pour la Boîte à musique et signatures propres aux effets abstraits. Préparer de petits lots d'écoute, sans intégrer les candidats refusés ni transformer « aucun » en demande de silence.

Choix retenus : Faucille B, Parcmètre A, Arc du gymnase B, Cloueuse C, Scalpel C, Trousseau C, Boussole B, Lampe à pétrole C, Gants de boxe A. Les Gants de boxe obtiennent ainsi leur premier son ; les cinq autres armes muettes attendent une sélection.

Le frottement signalé dans le mix reste à identifier ; ce lot ne prétend pas le corriger.

### Livré — choix intégrés et deuxième écoute

**A3b1 terminé.** Les neuf WAV sélectionnés sont copiés sans transformation dans `assets/audio/sfx/weapons/`, avec neuf clés propres et leurs crédits : égalité octet pour octet avec l'audition approuvée. Banque à −14 dB, priorité 10, une voix par son, intervalle minimal de 180 ms. Les quinze refus conservent leur état de production antérieur ; cinq armes restent sans son d'attaque.

Validation : `tools/validate.sh /tmp/vestiges-audio-selections-20261007 smoke audio weapons music`, 4/4 suites vertes, sources stables, build sans avertissement ni erreur. La régression de l'Arc vérifie une seule demande sonore pour une attaque à un projectile comme à trois projectiles. La galerie `selection-integree/run.mp4` enregistre les neuf armes dans le moteur, seed 221092026, avec musique et ambiance coupées ; les neuf nouvelles clés sont jouées et aucune de leurs voix n'est interrompue ou refusée. `RunObservation.Weapons` accepte désormais la trace audio et les bus muets, avec repères de début de chaque arme. Les impacts et les créatures restent audibles : cette galerie n'isole pas le seul départ d'attaque. Pas de validation artistique supplémentaire par l'agent.

**A3b2 prêt à écouter.** `~/.local/share/vestiges-audio/2026-10-07/armes-2/index.html` : 45 nouveaux candidats, trois lots de cinq armes, choix persistants dans le navigateur et export `choix-armes-2026-10-07-r2.json` identifié par sa série. La page principale donne aussi accès à la galerie intégrée et à l'identification du frottement. La première audition et l'export de Raphaël restent archivés.

Nouvelles matières : billes de verre et interrupteurs, mécanisme de parapluie, pelle et tôle, câble fouetté, assiettes, râteau et dents pincées, cloches, verre, notes réelles de boîte à musique (do, do–sol, do–fa–sol), obturateurs, baguettes de bois, gomme, craie, contacts radio brefs et chronomètre. Les trois candidats d'une arme sont des prises, gestes ou assemblages différents ; ils ne sont plus des changements de hauteur d'un ancien bruitage. Les matières analogues sont explicites dans les sources (par exemple peigne pour certaines dents du Râteau). Pas de boucle radio ni de fond permanent ajouté.

Provenance : sources CC0 de Joseph SARDIN / BigSoundBank, ValentinPetiteau, Xemptful, RavenWolfProds, sechavez3499 et sseletskyy / Freesound. Pages de licence, originaux téléchargés, empreintes et découpes archivés dans `recherche-2/` ; liens et auteurs détaillés par candidat dans la page et `manifest.json`. Les cinq sources Freesound sont leurs aperçus publics MP3 HQ, indiqués comme tels ; convertir ces aperçus en WAV ne restaure pas la qualité du WAV original. Aucun candidat de cette deuxième série n'est intégré au jeu.

Reproduction : `python3 ~/.local/share/vestiges-audio/2026-10-07/recherche-2/prepare_round2.py` (NumPy et FFmpeg), avec `sources.json` et les sources archivées. Contrôles : 45 empreintes distinctes, PCM mono 48 kHz/16 bits, crêtes au plus −8 dBFS, extrémités à zéro, références locales valides ; page vérifiée dans Chromium. Les répétitions sont illustratives et espacées pour conserver les queues des cloches et notes ; elles ne simulent pas toutes les cadences ni la concurrence des voix du moteur. **À valider : les timbres à l'écoute, puis les cinq chemins encore muets après sélection.**

## A3b3 — deuxième sélection et deux reprises ciblées

Export `choix-armes-2026-10-07-r2.json` reçu et archivé dans `retours/`. Lots avant code :
1. Intégrer les treize choix R2 octet pour octet, avec provenance et mêmes protections de banque ; brancher le Transistor au début effectif du cône seulement. Vérifier le déclenchement unique, le maintien, la reprise et le retrait de l’arme, puis galerie moteur des treize sons.
2. Cloche : proposer des frappes beaucoup plus simples et courtes. Boîte à musique : essai hors jeu d’une note aléatoire par déclenchement, avec petites gammes cohérentes, sans phrase imposée. Ces deux choix `aucun` ne valident aucun candidat R2 ; garder leur état de production jusqu’au retour d’écoute.

Le lot vise 22 armes choisies sur 24 ; le frottement du mix reste un sujet distinct non résolu.

### Intégration et essais R3

Les treize WAV R2 sont intégrés sans modification sous `assets/audio/sfx/weapons/`, avec clés propres, crédits des sources et recettes conservés, banque à −14 dB, intervalle 180 ms, priorité 10 et plafond d’une voix. Les 22 choix R1/R2 sont vérifiés octet pour octet par rapport aux auditions. Polaroïd, Baguette de sourcier, Craies et Transistor obtiennent leur premier son ; seule la Boîte à musique reste muette. La Cloche conserve son son antérieur, pas un candidat refusé.

Le cône du Transistor appelle le retour sonore commun à son activation ; il ne joue rien de plus pendant le maintien, les dégâts continus ou le retrait. Les attaques ponctuelles gardent le même retour sonore, sans ajout de recul ou de flash au Transistor. Régression dédiée : départ, vingt mises à jour et demandes d’attaque pendant le maintien, expiration/reprise, retrait. Les quatre contrôles passent, comme les salves de l’Arc.

Validation : `tools/validate.sh /tmp/vestiges-audio-r2-integration-20261007 smoke audio weapons music` : 4/4 suites, sources stables, build zéro avertissement/erreur. Banque : 99 effets chargés, gains et limitation vérifiés. Les modifications concurrentes du Péril et du HUD ne sont pas retouchées par ce lot.

**R3 hors jeu :** `~/.local/share/vestiges-audio/2026-10-07/armes-3/index.html`. Cloche : A sinus bref de 180 ms, B fondamentale et octave douce de 300 ms, C frappe réelle raccourcie à 190 ms. Boîte : A lamelles réelles, B notes synthétiques rondes, C notes synthétiques pincées ; chaque timbre propose do/ré/mi/sol/la, tirage uniforme sans répétition immédiate. Un clic = une note ; les séquences enregistrées sont des exemples au gain proposé, pas des phrases à jouer automatiquement. Le choix porte sur le timbre ; la suggestion de hasard n’est pas interprétée comme une sélection d’un fichier R2 refusé.

Rendus, script `prepare.py`, empreintes, recette de synthèse et provenance sont archivés dans `armes-3/`. Les matières enregistrées viennent des sources CC0 de Joseph SARDIN déjà archivées en R2 ; les autres sons sont des synthèses originales. Contrôles : 18 fichiers de timbre distincts, WAV mono 48 kHz, absence d’écrêtage et fondus aux extrémités ; Chromium vérifie 60 tirages sans répétition immédiate, décodage audio, export JSON R3 avec remarques et persistance après rechargement. Ce contrôle ne constitue pas une écoute artistique. Export attendu : `choix-armes-2026-10-07-r3.json`.

**Galerie R2 enregistrée :** `selection-r2-integree/index.html` et `run.mp4`, même dossier d’écoute, seed 221092026, treize armes équipées une par une, musique et ambiance coupées. La trace confirme les treize nouvelles clés jouées, dont deux départs du Transistor. Les accès directs de la page utilisent les repères réels du moteur. Les ennemis et impacts restent présents. Les pages principales et R2 renvoient maintenant vers les deux reprises R3.

## A3b4 — derniers choix : Cloche B et Boîte C

Raphaël valide « cloche b et boite a musique c ». Lot avant code : copier les six WAV R3 à l’identique (Cloche B et cinq notes C) avec leur recette de synthèse ; intégrer à la banque un groupe de variantes préchargées, tirage sonore indépendant du gameplay sans répétition immédiate après une lecture admise. Une seule clé partage intervalle et plafond entre toutes les notes ; aucun désaccordage de la gamme. Au contact d’une orbe vivante, utiliser la clé de l’arme orbitale, même si une autre arme vient de tirer ; pas de son au minuteur, à l’équipement ou après retrait. Régressions des contacts, du choix des notes et de la saturation, build/smoke et galerie audible avant de cocher 24/24.

**Intégration.** Cloche : `sfx_weapon_teachers_bell`, WAV B de 300 ms. Boîte : `sfx_weapon_music_box`, cinq WAV C de 250 ms. Fichiers identiques aux six rendus sélectionnés, recettes et empreintes dans les crédits ; synthèses originales VESTIGES. Les 24 armes disposent désormais d’une clé propre et aucune n’est muette.

La banque accepte une liste optionnelle `variants` de chemins, préchargée à l’initialisation. Après admission par l’intervalle et le pool, `AudioManager` choisit uniformément parmi les autres variantes avec son RNG audio ; une demande refusée ne change pas la dernière note ni le tirage. Les groupes de variantes gardent une hauteur de lecture exacte (1), un gain de −14 dB, un intervalle commun de 180 ms et une seule voix commune. Pas d’allocation ou de chargement à chaque note. Le fondu UI reconnaît également les variantes.

Le contact de l’orbe demande le son de l’arme orbitale ; l’arme courante peut être différente. Les contrôles couvrent l’absence de note à l’équipement et au minuteur, le contact avec une autre arme courante, le retrait, quarante lectures admises couvrant les cinq notes sans répétition immédiate, la hauteur exacte, les refus par cadence et plafond sans changement du RNG, et le fondu UI. Les contrôles moteur existants des dégâts orbitaux, des salves et du Transistor restent verts.

**Validation :** `tools/validate.sh /tmp/vestiges-audio-r3-integration-20261007 smoke audio weapons music`, 4/4 suites, sources stables ; build zéro avertissement/erreur. Banque : 101 entrées d’effets, toutes leurs variantes chargeables. Les pages d’écoute indiquent les choix R3 retenus, archivés depuis le message de Raphaël dans `retours/choix-armes-2026-10-07-r3.json`.

**Galerie finale :** `selection-r3-integree/index.html` et `run.mp4`, seed 221092026, musique et ambiance coupées, impacts et ennemis conservés. Trace : quatre frappes de Cloche et neuf notes de Boîte effectivement jouées. Les six WAV correspondent exactement aux empreintes R3 sélectionnées. Les 24 armes et leurs 24 clés propres sont vérifiées. Le lot « un son par arme » est intégré ; le frottement signalé dans le mix global et la suite musique/ambiances restent à traiter séparément.

## Reprise du 10 octobre — mix, sons manquants, puis musique

Réponses de Raphaël au [DECISIONS §88](DECISIONS.md) : combat aux pics, Résurgence V2 et accent d'entrée, page d'isolement du frottement, ordre mix → sons manquants → musique. Le travail se fait désormais sur le Mac : les dossiers d'écoute Linux (`/home/raphael/.local/share/vestiges-audio/`) n'y sont pas, les nouvelles écoutes vont dans `~/.local/share/vestiges-audio/2026-10-10/` (hors dépôt).

Lots, un à la fois, commit par lot :
1. **M1 — musique aux pics et entrée de la Résurgence.** La musique d'exploration devient le fond de la run. Le combat entre sur une créature notable proche (variante : élite, champion, Souverain, Aberration ; ou rang élite, mini-boss, boss) ou sur une foule très dense, et sort quand il n'y a plus de notable et que la foule redescend sous un second seuil (hystérésis), tenu quelques secondes. Seuils dans `music.json`, fixés d'après la distribution mesurée des créatures proches. Points d'entrée V2 : annonce à 52 s, crise à 34 s. Mesure avant/après de la part du temps en combat sur les mêmes seeds ; `test_music` étendu (foule moyenne sans notable : exploration ; notable : combat ; sortie).
2. **M2 — page d'isolement du frottement.** Run de référence enregistrée après M1, puis la même run avec un seul bus audible à la fois (musique, ambiance, effets), et chaque boucle d'ambiance seule. Analyse spectrale pour désigner les suspects (bruit large bande constant), sans rien atténuer avant le choix de Raphaël.
3. **M3 — planche des sons manquants.** Accent d'entrée de la Résurgence, soin, bouclier, brûlure du Néant, explosion de l'Instable, réveil du Mémorial et de la Faille (`sfx_souvenir_trouve` provisoire, §49), fragment rare, Barrière et Souverains, et les impacts par matière (plan 27 V5 : lame, choc, verre, feu, papier, électrique). Trois candidats par besoin, sources CC0 ou synthèse originale, provenance archivée, page d'écoute avec export des choix. Rien n'est intégré avant le choix.
4. **M4 — intégration des choix de M3**, à son retour.
5. **Musiques** : recherche de nouvelles pistes (exploration, combat, annonce, Résurgence, Hub), plus tard.

### M1 livré — 10 octobre

- **Combat aux pics.** `MusicDirector` ne déclenche plus le combat sur 4 créatures proches mais sur un pic : une créature d'une variante listée dans `peak_variants` (`champion`, les Souverains), un mini-boss ou un boss, ou 85 créatures ou plus dans 600 px, tenus 1 s. Il retombe sans pic et sous 60 créatures, tenus 6 s. Les élites sont exclues : l'une d'elles est à moins de 600 px 33 à 43 % du temps d'exploration. `Enemy` expose `VariantId` et `Tier` pour cette lecture, à 2 Hz.
- **Points d'entrée V2** : annonce à 52 s de `mus_crepuscule`, Résurgence à 34 s de `mus_nuit_vagues`. L'accent d'entrée viendra de la planche M3.
- **Trace d'écoute** : `audio-states.csv` compte les élites, Souverains, aberrations et boss proches, et relève une ligne par seconde de jeu même en temps accéléré.

Mesure, même outil des deux côtés (`tools/measure_run.sh`, `--nomad --visit --audio-trace`, 420 s, seeds 221092026, 1002, 42 ; bot invincible, profil dev). Distribution en exploration : médiane 36 à 46 créatures à moins de 600 px, 90e centile 68 à 83, Souverain proche 15 à 17 % du temps.

| Part du temps d'exploration | Avant | Après |
|---|---:|---:|
| Musique de combat | 83 % (les trois seeds) | 22 à 33 % |
| Musique d'exploration | 2 % | 52 à 63 % |
| Changements de musique sur la run | 6 | 9 à 12 |

Les seuils sont fixés sur ce bot, qui ne joue pas comme un humain. **À écouter en jeu** : un combat qui part trop rarement ou trop souvent se règle dans `music.json`. `test_music` passe 27 contrôles (foule moyenne avec une élite : exploration ; Souverain : combat ; hystérésis après son départ ; points d'entrée). `validate.sh smoke audio music` vert, build sans avertissement.

### M2 livré — page d'isolement du frottement

- **Pistes séparées d'une même run.** `RunObservation --audio-stems` (`AudioStemRecorder`) pose un effet d'enregistrement en fin des bus Music, Ambiance et SFX ; les pas (`sfx_pas_*`) passent le temps de la mesure par un bus à eux, au volume des effets, sans changer le mixage. Les quatre WAV sont synchrones du mixage du Movie Maker ; `stems.json` donne leur début sur l'horloge sonore et le volume de chaque bus (l'effet passe avant le fader). Une seule run suffit : la rejouer avec des bus coupés divergerait.
- **`tools/audio_stems.py <dossier>`** remet chaque piste à sa place et à son volume, l'encode, mesure niveau par seconde, plancher (10e centile), planéité spectrale et spectrogramme, puis écrit `index.html` : vidéo muette, mixage ou pistes à couper ou écouter seules, repères cliquables (annonce, crise, niveaux, coffres, changements de musique). `record_run_audio.sh` accepte `PYTHON` pour le rapport (NumPy et Pillow absents du Python du Mac : environnement isolé).
- **Référence** : `~/.local/share/vestiges-audio/2026-10-10/frottement/`, seed 221092026, Traqueur, 240 s de jeu, `--crisis-at 70`, après M1.

| Piste (au volume du bus) | Plancher | Médiane | Planéité | Audible |
|---|---:|---:|---:|---:|
| Mixage complet | −24,5 | −17,8 | 0,46 | 100 % |
| Effets sans les pas | −38,6 | −19,0 | 0,46 | 91 % |
| Pas | — | −35,9 | 0,23 | 79 % |
| Musique | −70,4 | −31,1 | 0,11 | 89 % |
| Ambiance (boucle de forêt) | −55,8 | −47,9 | 0,77 | 91 % |

Suspects, **sans conclusion avant l'écoute de Raphaël** : (1) la nappe des effets de combat, bruitée et quasi continue dès la première minute (`sfx_hit_ennemi` 490 fois en 4 min 46, arc 340, tirs ennemis 299) ; (2) les pas, à 0 dB dans la banque, plus forts que la médiane de la musique et seuls à remplir les moments calmes (sur un essai de 25 s sans combat, ils forment le plancher du mixage, audibles 92 % du temps) ; (3) la boucle de forêt, bruit pur de 8 s, constante mais 30 dB sous le mixage. Rien n'est atténué tant qu'il n'a pas désigné la piste.

### M3 livré — planche des sons manquants

`~/.local/share/vestiges-audio/2026-10-10/sons-manquants/index.html` : 19 besoins, 57 candidats, choix gardés dans le navigateur et export `choix-sons-2026-10-10.json`. Page d'accueil des deux écoutes : `~/.local/share/vestiges-audio/2026-10-10/index.html`.

- **Résurgence** : accent d'entrée. **Joueur** : soin, bouclier rechargé, coup paré ou ignoré, brûlure du Néant. **Créatures** : explosion de l'Instable, annonce du tir visé, surgissement du Rampant, tentacule de l'Indicible. **Monde** : réveil du Mémorial, réveil de la Faille (son actuel en référence), fragment rare. **Impacts par matière (plan 27 V5)** : lame, choc, verre, feu, papier, électrique, avec une répartition proposée des 24 armes et le son actuel en référence. **Mix** : pas dans l'herbe à −6 dB, à −10 dB, ou aigus coupés, au cas où le frottement viendrait d'eux.
- Candidats au même niveau d'écoute (RMS maximal −20 dBFS, crête ≤ −8 dBFS), sauf les pas à leur niveau réel ; bouton « en rafale » à la cadence de jeu pour les impacts, la brûlure et les pas.
- Sources : paquets Kenney CC0 (Impact, Sci-fi, RPG, Interface, Digital Audio) et BigSoundBank de Joseph SARDIN, CC0 (allumettes, essence, briquet, papier, soudure, terre), archivés avec leurs pages et empreintes dans `sources/` ; 17 synthèses originales (NumPy, graine fixe). `prepare.py` régénère les 57 fichiers à l'identique (vérifié : empreintes égales sur deux passes), `manifest.json` garde sources, licences et empreintes par candidat.
- Contrôles par la mesure, pas par l'oreille : 57 empreintes distinctes, aucun écrêtage, attaque dans les 15 premières millisecondes sauf l'accent A, qui monte volontairement pendant 1 s ; les prises longues (allumettes, essence) sont coupées sur l'attaque voulue, situées par l'enveloppe. **Aucun son n'a été écouté par l'agent.**

**Suite :** Raphaël désigne le frottement et exporte ses choix ; M4 intègre ensuite les sons retenus (clés, gains en contexte, déclencheurs : soin, recharge du bouclier, armure, tranche du Néant, Instable, visée, Rampant, tentacule, Mémorial, Faille, fragment rare, accent de crise, son d'impact par famille d'arme en données).

### M4 — retour et découpage du 10 octobre

Export reçu : `~/Downloads/choix-sons-2026-10-10.json`, série `sons-manquants-2026-10-10`, quinze choix et quatre refus. Archiver l'original à l'identique dans le dossier d'écoute, sans prendre les textes des médias pour de nouvelles instructions.

- **M4a — événements et pas.** Intégrer à l'identique accent de Résurgence A, parade A, brûlure du Néant A, explosion de l'Instable A, surgissement du Rampant A, tentacule de l'Indicible A, réveil du Mémorial B, fragment rare A et pas d'herbe A (−6 dB). Conserver les choix et crédits existants ; ne pas remplacer les sons des quatre besoins refusés (soin, bouclier plein, annonce du tir visé, réveil de la Faille). Le Néant ne joue que sur une perte de PV effectivement résolue, les retours de défense restent indépendants du réglage des particules. Régressions des déclencheurs, banque et musique, build/smoke puis enregistrement audible.
- **M4b — impacts par matière (plan 27 V5).** Intégrer lame A, choc A, verre C, feu B, papier B et électrique A. Clé d'impact dans les données des 24 armes, résolue depuis la provenance du coup ; garder le critique distinct, le retour générique pour les dommages sans arme et la cadence des impacts continus. Vérifier les six familles, le silence des dégâts sans retour d'impact et les chemins d'armes existants ; galerie moteur et mêmes contrôles.
- **Diagnostic d'ambiance, à côté de M4a.** Raphaël : « je crois que ca vient de l'ambiance ». Hypothèse de boucle de forêt, sans certitude acquise. Préparer une comparaison de la même prise avec et sans cette piste, puis traiter la boucle selon son retour. Les pas A sont un choix explicite indépendant de cette hypothèse. Ne pas annoncer le frottement résolu sur la seule base de mesures.

M4a est livré et vérifié avant M4b. Les autres travaux locaux sur les boss sont préservés ; leurs sons dédiés en cours ne constituent pas des choix de cette planche.

### M4a/M4b livrés — 10 octobre

Les quinze WAV retenus sont copiés à l'identique dans `assets/audio/sfx/selected/` : empreintes conformes au manifeste d'audition, export original archivé sans modification sous `retours/`, sources et crédits conservés. Pour les pas A, la baisse de 6 dB est déjà dans le WAV ; la banque n'ajoute pas une seconde atténuation. Les soins, le bouclier plein et la visée restent sans nouveau son ; la Faille conserve son son actuel, aucun des candidats refusés n'est intégré.

- **M4a :** l'accent joue au début réel de la Résurgence, la parade sur l'armure qui absorbe une part notable et sur un coup ignoré, l'Instable à son explosion proche ; nouveaux timbres au surgissement du Rampant et à la levée des mains de l'Indicible. Le Mémorial joue son réveil une fois, y compris si sa révélation est passée ; la Faille garde sa référence. La clé de fragment rare, déjà appelée par les choix, a désormais son fichier. La brûlure suit `PlayerDamageResolved` : seulement les PV réellement perdus au Néant, intervalle de 500 ms, aucun bruit de blessure ou accent critique associé. Les blessures de combat suivent aussi ce contrat, y compris le premier coup de la run ; un soin ou un bouclier ne déclenche pas une blessure.
- **M4b / plan 27 V5 :** `impact_audio` dans les données de chacune des 24 armes, référence contrôlée au chargement, champ conservé par l'arme d'origine dans `AttackContext`. Six familles (lame, choc, verre, feu, papier, électrique) ; la Cloche et les Gants vont dans choc, la Lampe dans feu. Les critiques gardent leur son seul, les objets et dégâts sans arme leur impact générique ; la cadence visuelle des impacts continus borne toujours leur retour sonore. Les priorités et plafonds restent dans la banque JSON, sans chargement ni allocation ajoutés par impact.

**Vérification technique :** `tools/validate.sh /tmp/vestiges-audio-m4a-20261010-02 smoke audio music enemy_abilities indicible` : 5/5 ; puis `tools/validate.sh /tmp/vestiges-audio-m4b-20261010-01 smoke audio music weapons enemy_abilities indicible` : 6/6, sources stables, build zéro avertissement/erreur. Banque : 121 effets chargeables (inclut les sons de Barrière des travaux locaux). Contrôles ajoutés : premier coup, bouclier/non-application, brûlure distincte et cadence réelle, accent absent de l'annonce puis présent au début, parade indépendante des particules ; 24 provenances dans six familles, critique, coup secondaire, objet passif, dommage sans arme, absence de son entre deux retours continus, référence d'impact invalide refusée.

**Écoutes :** `~/.local/share/vestiges-audio/2026-10-10/integration-m4/index.html`. Run moteur de 85 s, seed 221092026, Traqueur, `--crisis-at 20 --audio-stems` ; les pauses font partie de l'enregistrement. La trace confirme un accent de crise, deux surgissements et 132 impacts de choc effectivement joués. Galerie distincte des six matières, musique et ambiance coupées, sons d'attaque conservés. Les sons non rencontrés dans cette courte run restent à écouter sur leur audition et dans leur situation de jeu ; ce banc ne vaut pas une recette de run complète.

**Ambiance :** `comparaison-ambiance/index.html` fournit 45 s de la référence M2 recomposées depuis les pistes synchrones, avec ou sans la seule famille Ambiance. Musique, effets, pas et gains sont identiques, sans normalisation ; cette référence précède M4. Raphaël suspecte l'ambiance ; couper cette famille doit encore confirmer ou infirmer le frottement. Aucun remplacement artistique ni suppression de boucle n'est décidé sur la seule mesure. Aucun son n'a été écouté par l'agent.

**Suite :** écouter le mix et confirmer la piste du frottement ; rechercher de nouveaux candidats pour les quatre refus (soin, recharge du bouclier, visée, Faille), puis reprendre les ambiances et nouvelles musiques. Les objectifs généraux « sound design cleanup », « musiques adaptatives » et « sound design complet » restent ouverts jusqu'à la recette artistique.

### Sons de la Barrière validés pour l'instant — 10 octobre

Raphaël valide les huit sons de la page `~/.local/share/vestiges-audio/2026-10-10/barriere/index.html` ([DECISIONS §92](DECISIONS.md)) : levée, verrou, battant brisé, chute de la grille, annonce et impact du poing, annonce et balayage de la chaîne. Tous sont déjà branchés dans la banque et les données de la Barrière (plan 07). Validation d'écoute provisoire, avec retouches possibles plus tard ; le frottement d'ambiance, les quatre refus et la recette du mix restent ouverts.

### M5 — frottement confirmé, correction à comparer — 10 octobre

Raphaël confirme que couper le seul bus Ambiance fait disparaître le frottement ([DECISIONS §93](DECISIONS.md)). Le bus est identifié ; la boucle précise reste à distinguer des oiseaux et des overlays météo avant traitement.

- **M5a — durée de boucle, correction technique.** Diagnostic : les WAV importés en QOA sont traités comme du PCM16 pour calculer `LoopEnd` depuis la taille compressée. Forêt : 310 208 octets donnent 77 552 échantillons (1,616 s) au lieu des 384 000 échantillons du fichier de 8 s ; la référence M2 répète exactement ces 77 552 échantillons. Calculer la borne depuis la durée décodée et la fréquence du stream, préserver l'original partagé ; vérifier PCM8/16, QOA réel et boucle contexte/retour au Hub. Build, smoke, audio, musique et nouvel enregistrement avec pistes séparées ; comparer la période mesurée avant/après.
- **M5b — audition du fond.** Retrouver un passage d'exploration sans oiseaux ni météo dans le nouvel enregistrement M5a et conserver les autres pistes ; préparer une version au bruit de fond filtré et une version sans cette seule boucle. Comparaison synchrone avec l'original, gains explicites, aucun ajustement automatique de volume du mix ; mesurer la réduction du bruit et vérifier que le reste est identique. Les oiseaux et overlays restent hors du traitement proposé. Les fichiers et le script de préparation restent hors dépôt, les assets de jeu attendent le choix à l'écoute. Suivi mis à jour à la livraison ; pas de case d'intégration cochée pour une audition.

### M5a livré, M5b prêt à écouter — 10 octobre

**Durée corrigée :** `CreateLoopableStream` utilise la durée décodée du WAV multipliée par sa fréquence ; il ne déduit plus le nombre d'échantillons des octets compressés. L'original préchargé reste intact. Les formats PCM8/16 mono/stéréo, le WAV QOA réel et les lecteurs forêt/tonnerre sont vérifiés ; arrêt au Hub et annulation des fondus restent verts.

**Preuves :** `tools/validate.sh /tmp/vestiges-audio-m5a-20261010-05 smoke audio music` en copie isolée de HEAD avec les modifications audio : 3/3, zéro source modifiée, build sans avertissement/erreur. Les fichiers audio et de régression sont identiques à ceux du checkout principal. Le premier build principal rencontrait le chantier des quêtes en cours ; le build principal a ensuite réussi sans avertissement. Les trois suites du contrôle principal `...-06` passent aussi ; le lanceur signale toutefois une modification concurrente hors lot (`tools/sprites/props/indicible_forms.py`). La référence stable reste donc `...-05`.

**Mesure moteur avant/après :** même seed 221092026 et même outil `record_run_audio.sh`, Traqueur. Référence M2 : période exacte de 77 552 échantillons (1,6157 s), corrélation 1 ; nouvelle prise de 45 s : 384 000 échantillons (8 s), corrélation 1. L'ancienne période ne se retrouve plus (corrélation 0,0117). Cette correction technique ne suffit pas à déclarer le frottement artistiquement résolu.

**Écoute :** `~/.local/share/vestiges-audio/2026-10-10/correction-ambiance/index.html`. Même passage de 14,84 s à partir de 19,48 s de la nouvelle prise ; trace sans oiseaux, météo ni menus dans cet intervalle. **A** : fond actuel, durée corrigée ; **B** : passe-haut 80 Hz, passe-bas 900 Hz, baisse de 6 dB ; **C** : cette seule nappe de forêt absente, oiseaux et météo conservés dans le traitement envisagé en jeu. Musique, effets et pas identiques ; aucun ajustement de volume du mix. Boutons de comparaison à position constante, mode mix ou ambiance seule, boucle source et candidat de 8 s disponibles. Fond A : RMS −48,19 dBFS ; B : −68,50 dBFS (−20,31 dB au total ; bande ≥2 kHz : −29,09 dB). Script, empreintes, retour de diagnostic et manifeste hors dépôt. Aucun son n'a été écouté par l'agent.

**Retour acquis :** Raphaël conserve le fond original avec une baisse de 1 à 2 dB, uniquement en forêt ; intégré en M6 ci-dessous. B/C restent des auditions archivées. Les quatre besoins refusés et la recherche musicale restent ouverts.

### M6 — fond de forêt local et baisse légère — 10 octobre

Retour acquis ([DECISIONS §94](DECISIONS.md)) : boucle complète conservée, baisse de 1 à 2 dB au plus, lecture seulement en forêt. Lot unique : gain −2 dB dans la banque, biome d'éligibilité en données ; notification du biome depuis la détection existante de `BiomeAtmosphere`, abonnement de l'audio via EventBus. Pas de nouveau parcours du monde par frame. Le fond et les oiseaux sont réservés à la Forêt Reconquise ; fondus de frontière annulables si le joueur revient rapidement, Résurgences et météo conservées. Biome inconnu au démarrage : silence du fond jusqu'à sa détection ; état local remis à zéro entre runs.

Vérifier entrée/sortie des cinq biomes, retour rapide pendant le fondu, phase de crise et retour au Hub, gain et durée de huit secondes ; trace du biome et de l'état du lecteur dans un enregistrement moteur. Build sans avertissement, smoke/audio/musique et vérification en run, puis case de roadmap après livraison. Les WAV de M5b ne sont pas intégrés.

### M6 livré — forêt seule, fond original à −2 dB — 10 octobre

La banque réserve `sfx_ambiance_foret` à `forest_reclaimed` et fixe son gain à −2 dB. Le WAV source est inchangé (empreinte identique à celle du retour), la boucle entière corrigée en M5a est conservée. `BiomeAtmosphere` annonce les changements déjà détectés via EventBus ; le lecteur audio suit ce signal. À la sortie, fondu de 0,5 s ; un retour rapide annule l'arrêt en attente sans relancer le même stream. Traverser plusieurs biomes hors forêt ne prolonge pas ce fondu. Les oiseaux ne sont déclenchés qu'en forêt ; les overlays météo gardent leur fonctionnement. Le biome inconnu reste silencieux et l'état est remis à zéro au Hub et au début de chaque run.

**Contrôles :** `tools/validate.sh /tmp/vestiges-audio-m6-20261010-02 smoke audio music` : 3/3, aucune source modifiée pendant la validation, build sans avertissement/erreur. Régressions : quatre autres biomes et biome inconnu silencieux, gain forêt −2 dB, oiseaux locaux, notifications répétées sans redémarrage, fondu non prolongé par des frontières successives, retour rapide sans coupure tardive, météo encore active hors forêt, cycle Hub/run et boucle de 8 s.

**Enregistrement moteur :** `~/.local/share/vestiges-audio/2026-10-10/foret-biome/index.html` ; 85 s de jeu, seed 221092026, Traqueur, cinq biomes traversés. Les 169 observations confirment 10 états en forêt à −2 dB et 159 hors forêt avec lecteur arrêté. Entrée à 61,90 s de vidéo, sortie à 66,40 s ; le passage en forêt dure 4,5 s. La durée entière de boucle est vérifiée par la régression et la mesure M5a, pas par ce bref passage. Cette prise précède seulement le garde-fou contre le prolongement du fondu lors de frontières successives, vérifié ensuite dans la suite finale. Pistes synchrones et trace disponibles sur la page ; aucun son n'a été écouté par l'agent.

**À valider ensuite :** confort du mix en jouant, nouveaux candidats pour soin, bouclier rechargé, annonce du tir visé et réveil de la Faille ; puis ambiances et nouvelles musiques. Le choix du fond de forêt et la validation provisoire des sons de Barrière sont acquis.

### M7 — arrêt des sons de run à la mort — 10 octobre

Retour acquis : Raphaël signale que les sons continuent après la mort, surtout ceux des ennemis et des armes, et demande de tous les arrêter ([DECISIONS §104](DECISIONS.md)). La remise à zéro actuelle n'intervient qu'au Hub ; la séquence de mort laisse le monde avancer au ralenti puis le fige, tandis que plusieurs boucles audio traitent toujours la pause.

Lot unique : arrêter dès l'état Death les voix d'armes/ennemis, les sons d'interface déjà engagés, les ambiances et toutes les boucles de run ; annuler leurs fondus. Refuser les nouvelles demandes du monde jusqu'au lancement d'une autre run, y compris les événements tardifs de météo/crise. Les sons dédiés à la mort et au bilan gardent le canal d'interface et la musique prévue. Vérifier l'arrêt immédiat, la pause, les événements tardifs, le retour au Hub et la deuxième run avec la même régression avant/après ; capture d'une mort réelle, build sans avertissement et suites smoke/audio/musique avant de cocher la roadmap.

### M7 livré — 10 octobre

`AudioManager` arrête les lecteurs et leurs fondus dès l'état Death, y compris les boucles de choix, de santé basse et de proximité de l'Effacement. Les sons UI déjà lancés sont coupés aussi. Les nouvelles demandes du monde, les événements d'équipement et les relances d'ambiance restent bloqués pendant la mort et au Hub ; une nouvelle run réactive les lecteurs avec un contexte et des cadences remis à zéro. Le canal UI reste disponible pour la dissolution, le bilan et les menus ; le pilotage musical de mort est conservé.

**Avant/après, même scénario `AudioMixRegression`, seed 221092026 :** sept voix de monde préparées (arme, ennemi, forêt, météo, choix et deux alertes), plus un son UI en cours. Avant : **7 → 7** voix à la mort, quatre contrôles en échec. Après : **7 → 0**, ancien son UI arrêté ; événements tardifs, bilan en pause, musique de mort et deuxième run verts. `tools/validate.sh /tmp/vestiges-audio-death-after-20261010-01 smoke audio music` : **3/3**, aucune source modifiée pendant la validation, build sans avertissement ni erreur. Build final après ajout du contrôle de capture : zéro avertissement/erreur.

**Mort réelle :** `tools/capture_run.sh`, seed 221092026, Traqueur, 25 s avec `--capture-death --play-bot --nomad --skip-reveal`, dans `/tmp/vestiges-audio-death-combat-20261010-01`. Douze ennemis apparus, attaques de l'Arc et une élimination avant la mort ; dix contrôles `DEATH_AUDIO` confirment zéro voix de run active, immédiatement puis pendant la séquence et le bilan en pause jusqu'à huit secondes. Bilan capturé et inspecté. La première prise de huit secondes, avant les premiers ennemis, reste dans `/tmp/vestiges-audio-death-capture-20261010-01`. Ces captures utilisent le driver audio factice : l'arrêt des lecteurs est vérifié, sans prétendre à une écoute du mix.

## Revue de mix — 10 octobre (soir)

Demande de Raphaël : « baisse le volume des armes du joueur, il est sur-mixé », puis « une grosse passe pour identifier ce qui va et ce qui ne va pas ». Sons d'attaque des armes passés de −14 à −20 dB et impacts de −3 dB avant la revue (committé avec `6d2f4e86`) ; la Sacoche de lettres, arrivée ensuite, alignée à −20 dB. **La revue montre que ce n'était probablement pas le bon coupable** (ci-dessous).

### Méthode

- **Banque entière** (142 fichiers) : sonie momentanée maximale (EBU R128, 400 ms) pour les sons courts, intégrée pour les musiques et nappes, crête vraie, échantillons saturés, silence de tête et de queue, centroïde spectral, part des basses, corrélation gauche/droite et perte en mono. Le « niveau effectif » ajoute le `volume_db` de la banque et le décalage passé à l'appel.
- **Run enregistrée** : `tools/record_run_audio.sh` avec `--audio-stems`, 300 s, seed 221092026, Traqueur, bot invincible, jusqu'au début de la première Résurgence ; 5 275 événements tracés.
- **Resynthèse par famille** : chaque son tracé rejoué à son gain sur une piste par famille. Elle retrouve la sonie de la piste d'effets réelle à 0,2 dB près (−15,9 contre −15,7 LUFS) et celle d'un passage de combat à 0,0 dB près ; on peut donc lui faire dire qui pèse quoi.
- **Code** : bus, pools, appels et décalages relus (rapport d'agent, lecture seule).
- Scripts et venv (NumPy, SciPy, pyloudnorm, soundfile) hors dépôt, dans le dossier temporaire de la session ; aucun son écouté par l'agent.

### Ce que pèse chaque famille dans la run

| Famille | Intégrée | Court terme p50 / p95 |
|---|---|---|
| Effets, total | −15,7 LUFS | −19,6 / −12,8 |
| dont `sfx_ombre_attaque` seul | **−15,6** | −24,0 / −16,5 |
| dont autres sons d'ennemis | −22 à −33 | — |
| dont impacts et critiques des armes | −34,2 | −40,0 / −30,6 |
| dont attaques des armes | **−49,1** | −51,1 / −47,9 |
| Pas | −34,2 | −39,8 / −29,9 |
| XP | −40,9 | — |
| Musique (après bus −6 dB) | ≈ −22,5 | — |
| Ambiance (après bus −8 dB) | ≈ −33 | audible 21 % du temps |
| Mixage complet | −16,2 LUFS | crête **+0,2 dBFS, 140 échantillons saturés** |

### Ce qui ne va pas, par gravité

1. **`sfx_ombre_attaque` fait à lui seul presque tout le volume des effets.** C'est le coup porté au joueur par l'Ombre, le Charognard et le Rampant d'ombre (`attack_audio`, appel à −7 dB) : 167 fois en 5 min, fichier saturé (28 échantillons à 0 dBFS, crête vraie +0,3), joué à 0 dB de banque. C'est un « wouch » de coup : il est probablement ce qui sonne comme « les armes trop fortes ». Le bot invincible encaisse plus qu'un joueur, mais l'écart reste énorme : 33 dB au-dessus des attaques d'armes.
2. **Les attaques des armes du joueur sont devenues presque inaudibles** : −49 LUFS, 15 dB sous les impacts et 27 dB sous la musique. Les fichiers sont eux-mêmes inégaux (16 dB d'écart momentané entre la Lanterne ou la Masse et la Fronde ou l'Arc).
3. **Le coup critique saute de 11 à 20 dB** : `sfx_hit_critique` (0 dB, crête −3 dBFS) remplace l'impact par matière (−13/−15 dB).
4. **La révélation de coffre est le son le plus fort de la run** (−6,6 LUFS momentanés, 3,5 dB au-dessus du combat le plus dense) **et sature la sortie à chaque coffre** : fichier écrêté (82 échantillons), 0 dB, aucun limiteur. Les 140 échantillons saturés du mixage tombent tous sur les cinq coffres.
5. **Aucun limiteur ni compresseur** sur aucun bus. Fichiers écrêtés à la source : `sfx_ombre_attaque`, `sfx_orage_proche` (93), `sfx_chest_reveal` (82).
6. **Pas incohérents selon le sol** : 22 dB d'écart (eau −17, béton −29, gravier −29, bois −34, herbe −39). `sfx_pas_herbe` concentre 98 % de son énergie sous 120 Hz (centroïde 22 Hz) : inaudible sur haut-parleurs de portable. Ses 0,73 s de silence de queue avec une seule voix bloquent les pas suivants (136 refus sur 250 demandes).
7. **Silences de tête** : 50 à 90 ms sur sept sons d'armes (fronde, tuyau, couperet, fouet, bâton, lame du Néant, dessin), donc une attaque qui sonne en retard. 140 à 675 ms sur des annonces : `sfx_resurgence_accent` 675 ms, vague et éclair de l'Indicible, chaîne de la Barrière. Une annonce doit partir à l'instant.
8. **Musiques à niveaux non maîtrisés** : intégrées de −13,9 (Résurgence) à −18,6 LUFS (annonce), soit +4 dB en entrant en Résurgence sans que ce soit réglé. `mus_jour_exploration` commence par 4,2 s de silence et `mus_hub` par 2,2 s, ce qui laisse un trou après chaque fondu de 3 s. Combat, Résurgence et nuit ont 82 à 91 % de leur énergie sous 120 Hz : elles disparaissent sur petits haut-parleurs et masquent les impacts graves.
9. **Phase** : quatre fichiers stéréo en opposition partielle (corrélation négative) perdent 4 à 6 dB en mono (téléphone, Steam Deck, un seul haut-parleur) : `sfx_weapon_drop`, `sfx_artefact_trouve`, `sfx_bord_effacement_proche`, `sfx_sante_basse`.
10. **Récompenses déséquilibrées** : `sfx_artefact_trouve` −9 contre `sfx_souvenir_trouve` −33, soit 24 dB entre deux trouvailles. `sfx_level_up_after` et `sfx_perk_choix` pèsent 18 % de l'énergie des effets à eux deux.
11. **Code** : le filtre d'étouffement (mort, Mémorial) s'applique aussi à l'interface, qui joue sur le bus SFX. Il n'y a ni spatialisation ni atténuation à distance (porte nette à 650 px pour les actions ennemies). Les boucles démarrent et s'arrêtent sans fondu. `sfx_danger_building` est joué deux fois au même événement. 16 sons de la banque ne sont jamais appelés (13 attaques génériques, `sfx_boss_enrage`, `sfx_boss_death`, `sfx_projectile_vol`).

### Ce qui va

- **Architecture saine** : priorités, voix par clé et cadence tiennent la densité. 463 impacts contondants refusés sans trou audible, et 7 dB seulement entre médiane et 95e centile des effets : la foule ne fait pas exploser le volume.
- **Mixage global dans la norme PC** : −16,2 LUFS intégrés.
- **Sons de l'Indicible et de la Barrière cohérents** : −20 à −33 effectifs, propres, compatibles mono.
- **Musique bien pilotée** : fondu à puissance constante, points d'entrée, combat réservé aux pics. Musique et effets restent séparables.
- **Ambiance de forêt** stable à −25 LUFS, cantonnée à son biome.
- **Outillage rare pour un jeu indépendant** (trace, pistes séparées, enregistrement moteur) : il a rendu cette revue possible sans écouter.

### Écoute à faire

Page locale hors dépôt, régénérable par les scripts de la revue : même passage de début de Résurgence (22 s) en **réel**, **A** actuel, **B** Ombre −8 dB, **C** B + attaques d'armes à −14, **D** B + attaques d'armes à −8. On y trouve aussi les sons suspects isolés à leur niveau en jeu. Question : quelle version, et l'Ombre est-elle bien le son « trop fort » entendu ?

### Lots proposés (rien n'est codé)

- **X1 — sécurité, sans décision artistique** : limiteur sur Master (plafond −1 dB) ; trois fichiers écrêtés ramenés à −1 dBTP ; silences de tête retirés (sons d'armes, annonces) ; quatre fichiers en opposition de phase rendus compatibles mono ; silence d'entrée des musiques sauté par un point d'entrée. Vérification : nouvelle prise même seed, zéro échantillon saturé, sonie par famille avant/après.
- **X2 — hiérarchie, selon l'écoute** : gain de l'Ombre ; attaques d'armes remontées et égalisées entre elles (cible commune momentanée) ; critique ramené à +4/+6 dB au-dessus de l'impact ; coffre −6 dB ; pas égalisés (cible commune, herbe à remplacer ou à filtrer) ; trouvailles rapprochées.
- **X3 — musique** : niveaux des sept morceaux alignés (cible −16 LUFS ±1 ou écarts voulus écrits dans `music.json`), éventuellement un passe-haut léger sur les morceaux les plus graves.
- **X4 — bus** : bus UI séparé, hors étouffement ; compresseur doux sur SFX ; ducking léger de la musique sur les gros événements ; atténuation progressive à distance au lieu de la porte de 650 px.
- **X5 — ménage** : 16 sons jamais appelés retirés ou branchés ; doublon de `sfx_danger_building`.

### X1–X5 livrés — 10 octobre (soir)

Raphaël valide toutes les propositions et remarques de la revue, sans écoute intermédiaire ([DECISIONS §107](DECISIONS.md)). Un seul lot, une seule passe finale.

**Fichiers** (sha256 et traitement notés dans `assets/audio/CREDITS.json`) :
- silences de tête retirés (seuil −50 dBFS, 3 ms gardés, 2 ms de fondu) : six attaques d'armes (47 à 83 ms), dissolution, dash, tir ennemi, quatre annonces de boss, accent de Résurgence (441 ms) ;
- polarité du canal droit inversée sur quatre fichiers en opposition de phase ;
- pas d'herbe filtré sous 60 Hz et ramené à −6 dBFS de crête, 0,6 s de silence de queue retiré ; pas de bois +6 dB ; souvenir trouvé +14 dB ;
- musiques : silence de tête d'exploration (1,35 s) et du Hub (0,61 s), de queue du Hub et de l'annonce (2,15 s), retirés et réencodés en Vorbis q6 ; introduction quasi muette restante sautée par `start_sec` (hub 1,6 s, exploration et accalmie 2,8 s, combat 1,0 s), la boucle repart du début du fichier ;
- trois sons morts supprimés (`boss_enrage`, `boss_death`, `sfx_projectile_vol`). Les douze attaques génériques sortent de la banque, mais leurs fichiers restent : `tools/prepare_weapon_audio.py` s'en sert comme matière des sons d'armes.

**Banque** : gains calculés sur la sonie momentanée maximale de chaque fichier. Effectif = sonie + banque + décalage d'appel.

| Famille | Cible effective |
|---|---|
| Attaques des 25 armes | −40 (banque −1 à −17,5) |
| Impacts par matière | −38 |
| Critique | −33 |
| Coup générique | −38 |
| Pas, tous sols | −35 |
| Musiques (intégrée) | −16 LUFS ; Résurgence et nuit −15 |

Réglages fixes : Ombre −10, tir de Sentinelle −7, coffre révélé −8, boucle et suite du niveau −6 et −8, choix −4, niveau −3, artefact −6. Ducking déclaré dans la banque (`duck_music_db`, `duck_sec`) pour neuf grands moments : coffre, niveau, trouvailles, accent de Résurgence, fragment rare, défaites de la Barrière et de l'Indicible, réveil du Mémorial.

**Code** :
- `AudioManager` : bus `World`, sous-bus de SFX réglé par le même curseur, qui porte tous les sons du monde. Seul ce bus est étouffé à la mort et au Mémorial : l'interface reste nette. Il est aussi compressé doucement (−18 dB, 3:1).
- Limiteur sur Master (plafond −1 dB).
- Ducking de la musique par un amplificateur sur Music : attaque 0,08 s, tenue, retour en 0,8 s, remis à zéro à la mort.
- Boucles (choix, santé basse, bord, météo) en fondu d'entrée de 0,35 s et de sortie de 0,5 s ; fond de forêt en fondu d'entrée de 1,2 s ; arrêt net conservé à la mort.
- Branche morte `resurgence`/`the_call` retirée : c'est elle qui portait le doublon de `sfx_danger_building`.
- `Enemy.PlayNearbyAudio` : plein niveau jusqu'à 250 px, puis −14 dB en courbe douce jusqu'au rayon muet de 650 px, au lieu de la porte nette.
- `AudioStemRecorder` isole les pas sur le bus `World`.
- Relecture `godot-reviewer` : deux constats corrigés. Une boucle redemandée pendant son fondu de sortie remonte depuis le niveau atteint au lieu de repartir du début. L'amplificateur de ducking est remis à 0 dB quand l'autoload quitte l'arbre.

**Mesures**, même seed 221092026, Traqueur, 300 s, bot invincible.

| | Avant | Après |
|---|---|---|
| Mixage complet | −16,2 LUFS, crête +0,2 dBFS, **140 échantillons saturés** | −20,5 LUFS, crête −2,1 dBFS, **0 saturé** |
| Piste d'effets | −15,7 | −23,8 (pas compris) |
| Musique (avant bus) | −16,5 | −16,0 |
| Pas d'herbe refusés faute de voix | 136 sur 250 | 3 sur 195 |

Par famille, nouvelle banque sur la même trace qu'avant (resynthèse, sans compresseur ni atténuation à distance) :

| Famille | Avant | Après |
|---|---|---|
| Ombre | −15,6 | −25,6 |
| Attaques des armes | −49,1 | −37,2 |
| Impacts et critiques | −34,2 | −35,7 |
| Autres ennemis | −24,9 | −25,1 |
| Pas | −34,3 | −35,5 |
| Écrans et récompenses | −13,8 | −20,0 |

Les armes passent de 33 dB sous le coup de l'Ombre à 12 dB au-dessus. La prise « après » vient du worktree de la branche, sans les réglages d'équilibrage non committés de la prise « avant » : sa trace diffère (4 714 événements contre 5 275). Seule la comparaison par resynthèse est à trace égale.

**Contrôles** : build sans avertissement ; `tools/validate.sh … smoke audio music` 3/3, avec les nouveaux contrôles (limiteur, bus World compressé, étouffement du monde seul, ducking et retour, fondus d'entrée et de sortie des boucles et de la forêt). Aucun son écouté par l'agent.

**À juger en jouant** : le mixage est plus calme de 4 dB. La musique est maintenant légèrement au-dessus des effets (−22 contre −23,8 LUFS dans le mix) : à remonter par la banque si le combat manque de corps.

### X6 — garde-fous et finitions, livré le 11 octobre

Demande de Raphaël après X1–X5 : s'attaquer aux axes restants. Lot sans décision artistique.

- **Garde-fous du mix** : `tools/audio_bank_audit.py`, avec FFmpeg et la bibliothèque standard de Python seulement, mesure les 127 fichiers de la banque. Il applique `data/audio/mix_targets.json` :
  - cibles par famille : attaques d'armes −40, impacts −38, critique −33, coup générique −38, pas −35, musiques −15,5 LUFS ±1,5 ;
  - plafond à −10 pour tout son ponctuel ;
  - silence de tête de 30 ms au plus, montées voulues exemptées ;
  - corrélation gauche/droite positive.

  Un écart fait échouer `tools/test_audio.sh` et indique la correction de `volume_db` à faire. Vérifié par l'absurde : coffre remis à 0 dB et Fronde à −14, deux échecs nets. Les trois fichiers écrêtés à la source (Ombre, orage, coffre) restent signalés en avertissement.
- **Variation de volume** : `volume_variance_db` dans la banque, tirée à chaque lecture admise. 44 sons répétés : attaques et impacts ±1,5 dB, pas ±2, XP, critique, Ombre, Rôdeur, terrier et bond ±1, dissolution et coup générique ±1,5. Contrôle de régression ajouté ; le contrôle de banque GDScript tolère l'écart déclaré.
- **Réglages** : un fichier de réglages sans clé Musique ou Ambiance reprend désormais le niveau d'un profil neuf (−6 et −8 dB), au lieu de 100 % et 70 %.
- **Outil des sons d'armes** : `tools/prepare_weapon_audio.py` propose pour chaque candidat le gain qui l'amène à la cible des attaques, au lieu d'un −20 dB fixe.

**Contrôles** : build sans avertissement, `tools/validate.sh … smoke audio music` 3/3 (contrôle du mix : 127 fichiers, 0 échec, 3 avertissements).

**Encore ouvert, à décider ou écouter par Raphaël** :
1. Curseurs Musique et Ambiance affichés à 50 % et 40 % dans un profil neuf : passer la baisse dans le mix interne pour qu'ils s'affichent à 100 %, avec migration des réglages enregistrés.
2. Remplacer les trois fichiers écrêtés à la source par des versions propres : la distorsion est dans le fichier, le gain ne l'enlève pas.
3. Spatialisation stéréo légère des coups et des ennemis selon leur position à l'écran.
4. Basses très présentes dans les musiques de combat et de nuit (82 à 91 % de l'énergie sous 120 Hz).
5. Quatre sons manquants : soin, bouclier rechargé, annonce du tir visé, réveil de la Faille.

### X7 — curseurs à 100 % et placement à l'écran, livré le 11 octobre

Demande de Raphaël ([DECISIONS §111](DECISIONS.md)).

- **Curseurs** : Master, Musique, Effets et Ambiance valent 100 % sur un profil neuf. L'équilibre du mix vit hors des curseurs, dans un amplificateur sur chaque bus : musique −6 dB (le même amplificateur porte le ducking), ambiance −8 dB.
  - Les réglages enregistrés avant (sans `mix_version`) sont convertis en gardant le niveau entendu : l'ancien défaut de 50 % devient 100 %, une ambiance réglée à 20 % passe à 50 %. La version 2 est écrite à la sauvegarde.
- **Placement gauche/droite** : `AudioManager.PlayAt(clé, position)` choisit l'un de cinq bus orientés selon la position de la source à l'écran. Ils sont envoyés au bus du monde, donc étouffés et compressés avec lui.
  - Positions : panoramique ±0,25 au-delà de 20 % de la demi-largeur, ±0,5 au-delà de 60 %.
  - Sons placés : impacts et critiques sur les créatures, coups au contact, actions des créatures (tirs, cris, terrier, crachat), bond, Présage, dissolution à la mort, poings de la Barrière, éclairs et mains de l'Indicible.
  - Les sons du joueur, les annonces globales et l'interface restent au centre.
- **Contrôles** : `tools/validate.sh … smoke audio music` 3/3, puis `enemy_abilities indicible` 2/2. Nouveaux contrôles : curseurs à 100 % en profil neuf, équilibre de l'ambiance hors curseur, conversion des anciens réglages sans double conversion, choix du bus selon la position, bus orientés envoyés au monde, impact à droite joué à droite, ducking relatif à l'équilibre.

### X8 — planche des sons manquants et de la blessure, 11 octobre (à écouter)

Demande de Raphaël ([DECISIONS §111](DECISIONS.md)) : chercher ou créer les quatre sons manquants, et remplacer la blessure du joueur, qu'il n'aime pas.

`~/.local/share/vestiges-audio/2026-10-11/propositions/index.html` (hors dépôt) propose 39 candidats sur cinq besoins :

| Besoin | Synthèses originales | Sources CC0 |
|---|---|---|
| Blessure | 4 | 5 |
| Soin | 3 | 4 |
| Bouclier rechargé | 3 | 4 |
| Tir visé | 3 | 4 |
| Faille | 3 | 4 |

- **Sources CC0** : BigSoundBank (Joseph Sardin), Kenney (RPG Audio, Impact Sounds), OpenGameArt (Still North Media, Ogrebane, qubodup, Independent.nu, Spring Spring), plus quelques superpositions. Les sources refusées le 10 octobre sont écartées.
- Tous les candidats sont amenés à la même sonie (−20 LUFS momentanés), pour comparer le timbre et non le volume.
- La blessure se réécoute aussi dans une scène de combat de 6 s au niveau du jeu : Résurgence, arc, impacts, coups d'Ombre. Le son actuel est donné en référence, comme celui de la Faille.
- Sources archivées avec leurs pages et licences (`sources/`, `License.txt` de Kenney), synthèses reproductibles (`synth.py`, graine fixe), découpes et empreintes dans `manifest.json`, choix gardés dans le navigateur avec export `choix-sons-2026-10-11.json`.
- Aucun son n'a été écouté par l'agent ; rien n'est intégré.

**Après le choix :** intégrer le son retenu au gain cible de sa famille (contrôlé par `tools/audio_bank_audit.py`), puis brancher les déclencheurs manquants : soin notable avec cadence bornée, bouclier revenu au plein, visée des créatures à distance (placée à l'écran), réveil de la Faille.

**Retour du 11 octobre sur X8 ([DECISIONS §112](DECISIONS.md))** : le « son de blessure » métallique entendu en jeu était le coup de la créature, joué en même temps que la blessure. Retiré : un coup au contact ne joue plus que la blessure, et les trois sons de contact, devenus inutiles, sont supprimés. La planche ajoute un diagnostic (blessure seule, coup d'Ombre seul, les deux) ; ses scènes de combat, où la blessure était encore couverte, sont refaites sans le coup de créature.

**Choix du 11 octobre ([DECISIONS §113](DECISIONS.md)), intégrés :**
- **Blessure :** son actuel gardé.
- **Soin et bouclier rechargé :** sans son, besoins clos. Les trois créations de soin d'après la référence de Raphaël restent archivées hors dépôt (`2026-10-11/soin/`).
- **Tir visé :** C (`sfx_aim_warning`), lu par la capacité `aimed_shot` (`aim_audio` dans le contrat, valeur par défaut). `AudioManager.PlayAt(…, endsAfter)` le fait partir à la longueur du fichier moins la visée, pour qu'il culmine au tir (0,35 s pour la Tisseuse, 0,4 et 0,8 s pour le Hurleur). Gain visé −24 effectif, comme les autres annonces de créatures.
- **Réveil de la Faille :** G (`sfx_rift_wake`), joué par `RiftDirector` au réveil, ducking −4 dB pendant 2 s, gain visé −20 effectif.
- Le contrôle de la banque a refusé un premier fichier du tir visé (117 ms de silence de tête), retiré.
- Contrôles : `smoke audio music enemy_abilities` 4/4, dont un nouveau contrôle du départ calé du tir visé.

## Correctif du 12 octobre — son des petits lieux

Retour de Raphaël : pas de son en validant la cabine téléphonique, le wagonnet de la mine, etc. Les petits lieux (plan 22) passaient par `SmallPlaceUsed`, que l'`AudioManager` n'écoutait pas. Ils jouent désormais `sfx_poi_activate`, la même confirmation que les POI explorés ; aucune nouvelle entrée de banque. Contrôle automatique dans `SmallPlacesRegression` (une demande à l'usage, aucune sur un lieu épuisé). À écouter en jeu ; un son propre par lieu reste un besoin possible.

## U1 — sons de menu faits des matières du jeu — 14 octobre 2026

Demande de Raphaël ([DECISIONS §178](DECISIONS.md)) : les sons de menu ne sont pas dans l'ambiance du jeu. Survol, clic et confirmation sortent de Kenney Interface Sounds ; le survol (20 ms, centroïde 10,7 kHz) est de loin le plus brillant de la banque.

**Planche :** `~/.local/share/vestiges-audio/2026-10-14/menus/index.html` (hors dépôt, autonome : sons intégrés à la page). Trois synthèses originales par rôle, plus le son actuel en référence :

| Rôle (clé) | A | B | C |
|---|---|---|---|
| Survol et focus (`sfx_menu_survol`), 4 variantes chacun | pierre frôlée | papier | bois effleuré |
| Clic (`sfx_menu_clic`) | pierre | bois | bronze étouffé |
| Confirmer (`sfx_menu_confirmer`) | piano patiné | corde pincée | petite cloche |
| Refus (`sfx_perk_refuse`, aussi joué au level-up) | pierre sourde | bois mat, deux coups | corde étouffée |

- Confirmation accordée en fa mineur, tonalité mesurée du thème du Hub (chroma : fa, do, ré bémol) : fa puis do, suivis d'échos qui s'assourdissent, baissent un peu et se trouent (signature de l'Effacement).
- Survols entre 1,2 et 2 kHz de centroïde, contre 10,7 kHz aujourd'hui ; variantes tirées sans répétition immédiate en jeu (`variants` de `sounds.json`).
- Chaque candidat est au niveau du son actuel qu'il remplacerait (même sonie momentanée maximale), pour comparer le timbre au niveau du jeu.
- Scène « en situation » : la sélection cochée jouée sur le thème du Hub à −6 dB (`MusicMixDb`), en navigation (survols, clic, refus, confirmation) et en défilement rapide (12 survols à 130 ms, au-dessus de la cadence de 120 ms).
- `synth.py` (NumPy, graine fixe) régénère les fichiers à l'identique (empreintes vérifiées sur deux passes) ; `manifest.json` garde empreintes et niveaux. Contrôles par mesure et spectrogramme : pas de silence de tête, pas de composante continue, fins en fondu. Aucun son n'a été écouté par l'agent.

**Après le choix :** copier les fichiers retenus dans `assets/audio/sfx/selected/`, mettre à jour `sounds.json` (variantes du survol) et `CREDITS.json` (synthèse originale), passer `tools/audio_bank_audit.py`, `tools/test_audio.sh` et le smoke test. Un mélange entre candidats (survol A, clic B…) est possible ; une même matière pour survol, clic et refus donnera la famille la plus cohérente.

### U1 livré — choix du 14 octobre

Choix de Raphaël (export de la planche) : **survol B** (papier), **clic A** (pierre), **confirmer C** (petite cloche), **refus C** (corde étouffée). Remarque sur le survol : « B mais aucun des 3 proposés ne me plait vraiment ».

- `tools/generate_menu_sounds.py` régénère les sept fichiers retenus dans `assets/audio/sfx/ui/` (`menu_hover_1` à `4`, `menu_click`, `menu_confirm`, `menu_refuse`), identiques octet pour octet à ceux de la planche (mêmes graines, mêmes niveaux que les sons Kenney remplacés).
- `sounds.json` : mêmes clés, nouveaux fichiers ; le survol tire ses quatre variantes sans répétition immédiate. `sfx_perk_refuse` change aussi au level-up et aux écrans de choix en run.
- Anciens `ui_hover`, `ui_confirm` et `perk_refuse` retirés avec leurs crédits ; `ui_click` reste, source de cinq sons d'armes (`prepare_weapon_audio.py`).
- Contrôles : `audio_bank_audit.py` 0 échec, smoke test, `test_audio.sh` (0 échec). Aucun son écouté par l'agent ; à juger en jeu.

**Reste :** le survol B est un pis-aller ; une seconde planche de survol est à proposer selon ce qui gêne Raphaël dans les trois premières.

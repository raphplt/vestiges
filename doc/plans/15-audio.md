# 15 — Audio

Mis à jour le 7 octobre 2026. **Reprise autonome : mixage, identité des 24 armes, ambiances, menus et musique**, détaillée en fin de plan.

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

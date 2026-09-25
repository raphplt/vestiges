# Lot A — Sources des 30 candidats audio

25 septembre 2026. **Dix effets, trois propositions distinctes chacun. Aucun son retenu ni intégré.**

Cette présélection repose sur les descriptions des auteurs, les noms des fichiers et des mesures techniques. Aucune écoute artistique n’a été effectuée par l’agent. Les intitulés proposent une direction à comparer : ils ne certifient pas la qualité, le timbre perçu ni l’adéquation au jeu. Raphaël choisit ou refuse les trois candidats.

## Ce qui est livré

- `candidates.json` : briefs, chemins, descriptions, durée, auteurs, sources, preuves datées et SHA256 de chaque original.
- `media/*_original.*` : 30 fichiers sources complets, copiés sans transformation depuis les téléchargements publics. Les noms locaux permettent de les relier aux fiches ; le nom dans l’archive est conservé dans `source_file`.
- `media/*_preview.wav` : 30 aperçus WAV PCM 16 bits à 44,1 kHz, avec seulement un gain constant. Ni montage, ni synthèse, ni génération musicale ou sonore par IA.
- `sources/*.json` : pages officielles consultées le 25 septembre 2026, liens publics de téléchargement, liens de licence observés, noms des fichiers choisis et SHA256 de la réponse HTML reçue. Le hash constate la réponse consultée ; ce n’est pas une archive complète de la page.
- `sources/*-License.txt` : textes de licence inclus dans les six packs Kenney utilisés, conservés tels quels.

## Provenance et droits

Tous les candidats sont proposés sous **CC0 1.0** par leur source. Le lien de licence a été vérifié sur chaque page d’origine, pas déduit du mot « gratuit ». Les textes Kenney confirment aussi l’usage commercial. Les téléchargements publics ont fonctionné sans compte ni paiement. Les fichiers du dépôt viennent des téléchargements d’assets, pas des montages de démonstration des pages.

Les noms d’auteurs et liens sont conservés par courtoisie même si CC0 ne demande pas de crédit. Les candidats peuvent être copiés et transformés, y compris dans un jeu commercial et un dépôt public, selon [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/). La sélection ne prétend pas auditer toute la chaîne de création au-delà des déclarations et licences publiées par les auteurs.

| Source | Auteur | Fichiers retenus | Preuve locale |
|---|---|---:|---|
| [Swishes Sound Pack](https://opengameart.org/content/swishes-sound-pack) | artisticdude | 1 | [artisticdude-swishes.json](sources/artisticdude-swishes.json) |
| [Digital Audio](https://kenney.nl/assets/digital-audio) | Kenney | 4 | [digital-audio.json](sources/digital-audio.json) |
| [Impact Sounds](https://kenney.nl/assets/impact-sounds) | Kenney | 4 | [impact-sounds.json](sources/impact-sounds.json) |
| [37 hits/punches](https://opengameart.org/content/37-hitspunches) | Independent.nu (soumis par qubodup) | 3 | [independent-hits.json](sources/independent-hits.json) |
| [Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney | 4 | [interface-sounds.json](sources/interface-sounds.json) |
| [7 Assorted Sound Effects (Menu, Level Up)](https://opengameart.org/content/7-assorted-sound-effects-menu-level-up) | Joth | 3 | [joth-ui.json](sources/joth-ui.json) |
| [Music Jingles](https://kenney.nl/assets/music-jingles) | Kenney | 1 | [music-jingles.json](sources/music-jingles.json) |
| [RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney | 3 | [rpg-audio.json](sources/rpg-audio.json) |
| [80 CC0 RPG SFX](https://opengameart.org/content/80-cc0-rpg-sfx) | rubberduck | 5 | [rubberduck-rpg.json](sources/rubberduck-rpg.json) |
| [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | Kenney | 1 | [sci-fi-sounds.json](sources/sci-fi-sounds.json) |
| [Short alarm](https://opengameart.org/content/short-alarm) | yd | 1 | [yd-alarm.json](sources/yd-alarm.json) |

## Comparaison de niveau

FFmpeg mesure la moyenne RMS et la crête de chaque fichier entier, silences inclus. L’aperçu reçoit le plus petit des gains suivants : atteindre −22 dBFS de moyenne, rester à −3 dBFS de crête, ou amplifier de +12 dB maximum. Le gain exact figure dans chaque fiche. La crête est contrôlée après conversion : si le rééchantillonnage dépasse −3 dBFS, le gain est réduit avec une marge de 0,1 dB puis l’aperçu est reconstruit depuis l’original. Les 30 crêtes après conversion respectent ce plafond ; leurs valeurs figurent dans les mesures. La conversion ne contient ni limiteur, ni compresseur, ni changement de hauteur, ni découpage.

Cela limite les écarts extrêmes mais **n’égalise pas le volume perçu** : un impact court, une alarme et un jingle peuvent rester très différents. Le mixage final se fera après sélection, dans le jeu. Les originaux restent disponibles pour contrôler la préparation.

## Limites utiles pour le choix

- Le lot comporte six auteurs et onze packs ; plusieurs effets opposent des sources physiques, des confirmations synthétiques et des matières surnaturelles. Trois variantes numérotées voisines d’un même fichier ne constituent pas le panier.
- Impact critique : juger la différence avec l’impact normal. Le nom d’un son ne prouve pas qu’il sera ressenti comme plus puissant.
- Dégât joueur : une des propositions est un signal d’interface, volontairement distinct d’un coup corporel ; aucune voix n’est imposée au personnage.
- Dissolution : ces sources peuvent demander un travail de texture après choix. Un candidat glitch initial a été écarté après mesure, car ses 10 ms ne constituent pas une dissolution convaincante sur le seul plan temporel.
- Level-up / perk : les ponctuations de Joth durent plusieurs secondes. Vérifier qu’elles n’empiètent pas l’une sur l’autre et restent agréables à force de répétition.
- Coffre : les sources sont une ouverture de porte, un verrou et des pièces. La dernière illustre la récompense mais ne sonorise pas tout le mouvement du couvercle ; il faudra éventuellement assembler deux éléments.
- Dash : le départ utilise actuellement le son de pas sur gravier (`data/movement/mobility.json`, `start_audio`). Ce son de référence est disponible dans la page ; les trois propositions ne sont pas encore branchées en jeu.
- Danger : la cloche, l’impulsion grave et l’alarme représentent trois options. L’alarme risque une couleur trop technologique ; l’impulsion peut être confondue avec une explosion. L’écoute décidera.

Ce lot ne clôt ni la recherche exhaustive des autres effets ni la recette des dix effets présentés. Les sources non retenues restent des références ; aucun choix n’est présumé.

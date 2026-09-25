# Plan 12 — Micro-événements et variantes renforcées

Statut : **v1 implémentée le 24 septembre 2026, recettée et validée le 25 septembre** · Priorité : P0 (retour de Raphaël) · Dépendances : 03 (rythme), 07 (bestiaire).
Références : V2 §8–§10 (Résurgences, accalmie), [plan 03](03-boucle-et-rythme.md), [plan 07](07-bestiaire-et-rencontres.md).

## 1. Le retour

Raphaël, le 24 septembre, après une partie : « le jeu manque d'événements, il faut des micro-événements toutes les 2/3 minutes (et pas faire ça à l'arrache, mais vraiment y réfléchir et bien le faire), et pourquoi pas des "alternatives" des mobs de base en version boss ou plus forte ».

Constat avant l'intervention :
- Entre le début de run et la première Résurgence (240 s), puis entre deux Résurgences (cycle nominal de 310 s), il ne se passait rien d'autre que le flux continu d'ennemis.
- `data/events/events.json` datait de la V1 (jour/nuit, marchand, tempête) : aucun loader ne le lisait.
- `Enemy.Aberrate()` et les modificateurs de vague existaient, mais en dur dans le code et seulement pendant les Résurgences.

## 2. Principes de conception

1. **Un temps fort toutes les deux à trois minutes, sans jamais concurrencer la Résurgence.** Aucun événement pendant une Résurgence, ni pendant son annonce, ni dans les 40 s d'accalmie qui suivent. Juste avant une Résurgence, seuls les événements qui ont le temps de se terminer sont tirés : les courts (Harde, Averse) remplissent ce créneau.
2. **Une opportunité optionnelle, pas une corvée.** Ignorer un événement coûte sa récompense, rien de plus, sauf quand le danger vient à soi (harde, averse, impact).
3. **Lisible en une seconde.** Un bandeau (titre, objectif, temps restant, progression), un repère au sol visible de loin (colonne de lumière), une flèche au bord de l'écran si la cible est hors champ, un son d'annonce, un bilan de fin avec la récompense reçue.
4. **Chaque événement sollicite une compétence différente**, pour que la succession ne se répète pas :

| Événement | Ce qu'il demande | Risque | Récompense |
|---|---|---|---|
| **Un Souverain approche** | Puissance sur une cible unique résistante | Mini-boss escorté de sa meute | Coffre rare, Essence, arme possible ; coffre épique après 12 min |
| **La Harde** | Dégâts de zone, placement de biais | Une file de créatures traverse la zone en piétinant | XP ×1,5 par créature ; si 60 % abattues : coffre et Essence |
| **Un vestige tombe** | Avidité contre prudence | Impact annoncé (blesse aussi le joueur), puis gardiens | XP ≈ 70 % d'un niveau, Essence, soins |
| **La Veille** | Tenir une position quelques secondes | Les créatures convergent vers le cercle | Soins 30 %, coffre, Essence |
| **Averse d'éclats** | Esquive et placement : attirer les créatures sous les impacts | Impacts annoncés, aussi sur le joueur | Essence selon les créatures fauchées |

5. **Pas de base.** La Veille dure 14 s cumulées au plus, le cercle disparaît aussitôt tenu. Aucun Foyer ne revient.
6. **Récompenses branchées sur l'économie existante** : coffres, orbes d'XP (proportionnelles au niveau en cours, pour garder leur valeur toute la run), Essence, soins. La Veille et le vestige sont les seules vraies sources de soin.
7. **Variété sans hasard pur** : tirage pondéré, jamais deux fois de suite le même, poids réduit (×0,35) pour les deux derniers. Harde et vestige dès 1 min, Souverain à partir de 2 min, Veille à partir de 2 min 30, Averse à partir de 3 min 20 : les mécaniques arrivent progressivement.

**Calendrier (valeurs de départ) :** premier événement entre 70 et 95 s, puis 60 à 90 s de calme après la fin de chacun. Mesuré sur une run de 7 min (seed 777) : événements à 80 s et 163 s, Résurgence à 240 s, événement suivant à 350 s après l'accalmie.

## 3. Variantes renforcées des créatures de base

Trois variantes, définies dans `data/enemies/_variants.json` :
- **Élite** : PV visés 120 (multiplicateur borné entre ×2 et ×5), taille ×1,3, contour or, un affixe. Plaque au-dessus de la tête (nom accordé + PV), par exemple « Rôdeur Enragé ». Récompenses : XP ×4, +3 Essence, 8 % d'arme, 15 % de coffre commun. Elles apparaissent **naturellement** à partir de 90 s, une toutes les 35 à 55 s, deux vivantes au plus.
- **Souverain** : la version « boss » d'une créature de base. PV visés 300 (multiplicateur borné entre ×2,5 et ×12), taille ×1,6, contour orange, deux affixes. Il mène l'événement du même nom et lâche un coffre rare garanti.
- **Aberration** : l'ancienne variante des Résurgences, désormais pilotée par les données (mêmes valeurs qu'avant).

Les affixes sont des règles de combat, pas seulement des chiffres :

| Affixe | Effet |
|---|---|
| Enragé | Dégâts ×1,35, vitesse ×1,22 |
| Cuirassé | Dégâts reçus ×0,6, vitesse ×0,88 |
| Tenace | Régénère 3 % des PV par seconde, seulement après 2,5 s sans coup reçu |
| Instable | Explose à la mort (rayon 64) |
| Véloce | Vitesse ×1,45, PV ×0,8 |

Les PV ne sont pas un simple multiplicateur : les créatures de base vont de 10 PV (Ombre) à 120 (Tréant Corrompu). Un Souverain Tréant à ×11 aurait eu 1 320 PV, contre 198 pour un Souverain Charognard. Viser un budget de PV, avec des bornes, garde l'écart perceptible sans créer de sac à PV.

Les modificateurs aléatoires des Résurgences et du late game, jusque-là codés en dur, passent par les mêmes affixes (`phase_modifiers`). Les titres s'accordent en genre grâce à `grammatical_gender` dans le JSON des créatures (« Tisseuse Enragée », « Ombre Souveraine »).

## 4. Implémentation

- `scripts/Events/RunEventDirector.cs` : calendrier, exclusion des Résurgences, tirage, relais EventBus (`RunEventStarted`, `RunEventProgress` à 10 Hz, `RunEventEnded`). `ForceStart(id)` sert au banc de capture.
- `scripts/Events/RunEvents/` : un fichier par événement, une base `RunEvent`, un contexte (`RunEventContext` : point devant le joueur, apparitions, récompenses) et un repère au sol (`RunEventMarker`). Les impacts réutilisent `GroundTelegraph` du Présage.
- `data/events/run_events.json` : calendrier et réglages de chaque événement, lus par `RunEventDataLoader`. L'ancien `events.json` de la V1 est supprimé.
- `scripts/Combat/EnemyModifiers.cs` : état ajouté à une créature (variante, affixes, traversée de harde, jeton d'événement, exemption du retrait à distance), remis à zéro au retour au pool. `EnemyNameplate` dessine la plaque, sans redessin tant que les PV ne changent pas.
- `SpawnManager` : `SpawnEventEnemy`, `MakeVariant`, `PickLocalEnemyId`, élites naturelles.
- HUD : `RunEventHud` (bandeau, flèche de bord, bilan, annonce « Vaincu : Rôdeur Enragé »).
- Textes : clés `EVENT_*` dans `assets/translations/translations.csv` (fr/en).

## 5. Vérifications du 24 septembre

- `dotnet build` sans avertissement, smoke test vert.
- `tools/capture_run.sh` (nouveau) : vraie run rendue à 1080p, bot nomade invincible qui suit la cible de l'événement comme le ferait un joueur. L'option `CAPTURE_EXTRA_ARGS="--event <id>"` force un événement à 3 s.
- Les cinq événements démarrent, s'affichent et se terminent. Vestige et Veille réussis par le bot ; Harde échouée (arme de départ seule) ; Averse réussie (Essence obtenue).
- Corrections issues des captures :
  - Souverain trop coriace (25 % de PV perdus en 18 s) : budget de PV, durée portée à 70 s, apparition à partir de 2 min.
  - Un Souverain Tenace se régénérait plus vite que l'arme de départ ne le blessait : la régénération est suspendue 2,5 s après chaque coup.
  - Le directeur bloquait tout événement 85 s avant chaque Résurgence (une seule occurrence en 260 s) : tirage limité aux événements qui tiennent dans la fenêtre, et écart ramené à 60–90 s.
  - Une Harde partie d'un lac échouait à vide : plusieurs directions sont essayées, et un événement impossible à mettre en place est abandonné en silence puis retenté.
- Pire cas, Souverain forcé à 3 s contre un bot de niveau 1 avec l'arme de départ : un abattu en 24 s, un effacé. En run normale, il n'arrive qu'après 2 min.
- Élites naturelles observées : Charognard Cuirassé à 90 s, Tenace à 142 s, Instable à 190 s.
- Relecture `godot-reviewer` : aucun bug bloquant. Deux remarques appliquées (cumul Aberration + affixe rétabli en Résurgence, membres de la Harde protégés du retrait à distance pendant l'événement).

**Limites :** le bot n'esquive pas et ne choisit pas ses améliorations. Aucune de ces mesures ne dit si c'est amusant.

## 6. Recette demandée à Raphaël

- Cadence : un temps fort toutes les 2 à 3 minutes est-il ressenti, sans marcher sur les Résurgences ?
- Lisibilité : bandeau, flèche de bord et repère au sol suffisent-ils à comprendre quoi faire ?
- Intérêt : lequel des cinq donne envie de faire un détour ? Lequel est ignoré ? Lequel est frustrant ?
- Élites : fréquence (une toutes les 35 à 55 s après 90 s), difficulté, lisibilité des affixes.
- Récompenses : trop généreuses ou trop maigres par rapport aux coffres du monde ?

**Pistes suivantes, non engagées :** marchand ambulant (Essence contre arme), Écho d'un survivant à escorter, fragment de lore lié à l'événement, événements propres à un biome, Souverains de famille à comportement enrichi (plan 07).

## 7. Recette de Raphaël — 25 septembre 2026

- Cadence validée en l'état. Quelques détails resteront à revoir plus tard ; priorité aux autres aspects du jeu.
- Élites bien dosées.
- Micro-événements : « bien ».
- Le soin est peut-être trop difficile à obtenir : reporté au [plan 13 §8](13-butin.md#8-retour-du-25-septembre--le-soin) et au plan 03.

Les pistes du §6 restent non engagées.

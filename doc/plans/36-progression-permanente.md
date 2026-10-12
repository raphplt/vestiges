# Plan 36 — La progression permanente : des améliorations au Hub

## Reprise visuelle M5 — 12 octobre 2026

Raphaël trouve le Campement trop dense, retire le mot « Rang » devant `0/X`
et envisage des icônes pixel art lisibles et une largeur mieux adaptée. Il
signale aussi le fond partagé trop détaillé (plan 24 B5d).

Un seul lot M5, sans modifier l'économie :
- Retirer « Rang » du compteur FR/EN.
- Préparer quatorze icônes natives 32×32, silhouettes et détails francs,
  affichées à une échelle entière (64 px dans la liste).
- Préférer une liste à gauche et une fiche d'achat à droite : chaque ligne
  garde le nom, l'explication courte et `0/X` ; la sélection rassemble l'effet,
  le prix et le remboursement. Alternative proposée à Raphaël : deux colonnes
  aérées avec le remboursement uniquement sur la sélection.
- Garder les catégories visibles, les remboursements libres, le solde et
  la navigation clavier/manette ; tester achats, refus et focus après remboursement.
- Vérifier build zéro warning, smoke, camp, navigation et UI art, puis captures
  de profils neuf/dev à 720p/1080p.

La préférence de disposition a été demandée ; sans réponse durant le travail,
l'agent retient la première option recommandée et l'annonce : liste à gauche,
fiche à droite. C'est un choix d'implémentation de l'agent, pas une réponse de
Raphaël.

État : **livré et vérifié le 12 octobre 2026**.
- Liste de 780 px et fiche de 640 px, séparées de 40 px ; lignes de 80 px,
  nom, explication courte et `0/X`. La liste défile et suit le focus.
- 14 icônes natives 32×32 dans `assets/ui/camp/`, générateur déterministe
  `python3 -m tools.sprites.ui.camp` ; affichage ×2 dans la liste, ×4 dans la fiche.
- La ligne sélectionne ; **Acheter** et **Rendre** dans la fiche font les
  transactions. Plus de prix, gain et bouton de remboursement répétés sur
  chaque ligne. Le compteur et la confirmation d'achat omettent « Rang ».
- Effet actuel puis suivant et prix visibles avant l'achat ; solde et compteurs
  actualisés après transaction. Dernier rang rendu ou remboursement global :
  le focus revient à la sélection, même si Godot libère celui du bouton désactivé.
- Achats, refus, remboursements, montants et progression restent ceux du
  catalogue ; aucune valeur d'économie ou de gameplay modifiée.

Vérifications : build zéro warning et **4/4 suites vertes** (`smoke`, `camp`,
`menu_navigation`, `ui_art`), sources stables pendant le passage. Neuf contrôles
d'interface ajoutés au parcours réel : sélection sans achat, accès à la fiche,
achat et compteur, changement de fiche, remboursement, focus et remboursement
global. Captures profils normal/dev en 1080p/720p, transaction et défilement
jusqu'aux statistiques inspectés ; fond partagé vu en Collection.

Preuves hors dépôt : `/tmp/vestiges-menus-camp-m5-validation/`,
`/tmp/vestiges-camp-m5-normal1080/`, `/tmp/vestiges-camp-m5-normal720/`,
`/tmp/vestiges-camp-m5-1080/`, `/tmp/vestiges-menus-m5-collection/` et
`/tmp/vestiges-camp-icons.png`.

11 octobre 2026. Parenthèse de Raphaël au [§109](DECISIONS.md) : « il y a des améliorations globales que tu peux faire, par exemple j'améliore le nombre d'armes que je peux porter, j'améliore des statistiques […] là ça manque et ça serait crucial de rajouter ça rapidement ». **Étude et proposition, tranchées le 11 octobre (§8) : l'écran s'appelle le Campement.**

## 0. En bref

- **Aujourd'hui, rien ne s'achète entre deux runs.**
  - Les quêtes débloquent des personnages, des armes et des objets (plan 06), mais ne rendent pas plus fort.
  - Les **Vestiges** (score ÷ 10 à la fin de la run, `ScoreManager.cs`) s'accumulent et s'affichent au Hub, mais ne servent à rien : `SpendVestiges` n'a aucun appelant.
- **Les docs disaient l'inverse.** Le GDD (§5) veut des Vestiges pour des kits, des cosmétiques et des mutateurs, « jamais de puissance brute ». Le §109 tranche autrement : des **améliorations de puissance** permanentes, comme les PowerUps de Vampire Survivors.
- **Proposition : l'Établi**, un écran du Hub où l'on dépense les Vestiges. Il y a trois rayons :
  1. **Capacités** : emplacements d'armes, places dans le sac d'objets (plan 35), cartes par offre, relances, bannissements ;
  2. **Stats** : PV, dégâts, cadence, vitesse, régénération, armure, attraction, XP, Chance… en quelques rangs chacun, gains modestes ;
  3. **Confort** : Essence de départ, révélation partielle de la carte.

  Les rangs sont remboursables à tout moment, pour essayer sans risque.
- **Conséquence sur l'équilibrage** : la difficulté se règle désormais sur deux profils, neuf et plein. Un profil plein ne doit pas rendre le début trivial.

## 1. Aujourd'hui

- **Ce qui reste entre les runs** (`meta_save.json`) :
  - Vestiges, statistiques et 19 fragments de lore (Souvenirs, sans effet de jeu) ;
  - quêtes accomplies et progression des quêtes.
- **Le Hub** propose Partir, Personnages, Quêtes, Collection, Paramètres et Quitter (`HubScreen.cs`). Il ne contient aucune boutique.
- **Bouchons sans effet** : kits et mutateurs (`MetaSaveManager.cs`), squelette de mutateurs (plan 18 §5).
- **En run, l'Essence** sert au Mémorial et à l'Atelier, et se perd à la fin.

## 2. Principes

- **Petits pas, nombreux** : chaque achat se sent un peu, la somme compte. Pas de bond qui casse le début de run.
- **Les capacités avant les stats** : un emplacement ou une relance de plus change la façon de jouer ; +5 % de PV non.
- **Remboursable** : le joueur essaie, regrette, recommence.
- **Les quêtes débloquent, l'Établi renforce.** Une quête ouvre une pièce du contenu ; l'Établi n'en ouvre aucune.
- **Lisible** : chaque rang dit son effet chiffré et son prix.

## 3. Proposition : l'Établi

Nom repris du GDD. On peut aussi l'appeler l'Atelier du Hub, mais l'Atelier existe déjà en run.

### Rayon 1 — Capacités

| Amélioration | Effet | Rangs | Remarque |
|---|---|---|---|
| **Emplacement d'arme** | 3 → 4 (option : → 5) | 1 (ou 2) | Raphaël garde 4 comme référence. Commencer à 3 rend le premier achat marquant, sans rendre la fin de run plus forte qu'aujourd'hui. À trancher. |
| **Poches** | Sac d'objets 10 → 25 | 5 à 8 (+2 ou +3 places par rang) | Plan 35 |
| Carte de plus par offre | 3 → 4 | 1 | Cher |
| Relances | 3 → 6 par run | 3 | |
| Bannissements gratuits | 3 → 6 | 3 | Lien avec le Péril (plan 28) |

### Rayon 2 — Stats

Une dizaine de lignes, 5 rangs chacune, gains par rang de l'ordre de :
- PV max +5 % ;
- dégâts +4 % ;
- cadence +3 % ;
- vitesse +2 % ;
- régénération +0,1 PV/s ;
- armure +1 ;
- attraction +10 % ;
- XP +4 % ;
- Chance +3 % ;
- Essence gagnée +5 %.

Au maximum, le profil plein a environ +25 % de PV, +20 % de dégâts et +15 % de cadence. C'est comparable à quelques niveaux d'Habitude (plan 35), et bien moins qu'un build de fin de run.

### Rayon 3 — Confort

- Essence de départ (+20 par rang) ;
- carte du biome de départ révélée ;
- premier coffre de la run garanti rare.

### Économie

- **Gain** : aujourd'hui, score ÷ 10. Il faut vérifier, sur les mesures du plan 33, ce que rapporte une run de débutant (5 à 10 min), une run moyenne (15 à 25 min) et une bonne run (30 min ou plus). Bonus possibles : Barrière, Indicible, quêtes accomplies.
- **Prix** : croissants par rang ; le profil plein doit s'obtenir en 30 à 50 runs moyennes. À régler par un tableur (`tools/`) avant de coder.

## 4. Équilibrage

- Chaque mesure de difficulté se fait désormais **à deux profils** : neuf (réserve de départ, rien d'acheté) et plein (tout acheté). Le plan 33 a réglé le profil neuf.
- La difficulté peut s'appuyer sur le **Péril**, que le joueur choisit (Failles, stèles) : un profil plein joue naturellement plus haut, et le score suit.

## 5. Ce que ça touche

- **Données** : `data/progression/workbench.json` (rangs, effets, prix), contrôlé au chargement comme les autres contrats.
- **Sauvegarde** : rangs achetés dans `meta_save.json`, avec migration du profil existant (les Vestiges déjà gagnés restent).
- **Code** :
  - application au début de la run (stats de base, emplacements, relances, bannissements) ;
  - emplacements d'armes et sac en données au lieu de constantes (`Player.MaxWeaponSlots`, `MaxPassiveSlots`).
- **Interface** : écran de l'Établi au Hub (plan 04, menus en cours de refonte, §105–106) ; solde visible ; gain de Vestiges au bilan de fin de run.

## 6. Lots proposés

| Lot | Contenu |
|---|---|
| **M0** | Table des rangs et prix, simulation de l'économie sur les mesures existantes, maquette de l'écran (avec le plan 04) |
| **M1** | Données, sauvegarde et application en run ; emplacements en données ; rayon Stats et rayon Capacités sans le sac |
| **M2** | Écran de l'Établi au Hub, remboursement, bilan de fin de run |
| **M3** | Mesure à deux profils, réglage des prix |
| **M4** | Poches (sac d'objets), une fois le plan 35 I2 livré — livré (§14) |

## 7. Questions pour Raphaël

1. **Emplacements d'armes** : commencer à 3 et acheter le 4ᵉ, ou commencer à 4 et pouvoir acheter un 5ᵉ ?
2. **Remboursement** libre et gratuit : d'accord ?
3. **Nom** : l'Établi (nom du GDD), un autre ?
4. **Ordre** : l'Établi avant la refonte de l'inventaire (M1–M2 d'abord, rapide), ou après ?

## 8. Décisions du 11 octobre ([§110](DECISIONS.md))

- **Nom** : **le Campement**. « Établi » est déjà pris en jeu.
- **Emplacements d'armes** : un profil neuf commence à **2**, le Campement monte jusqu'à **4** (deux rangs). Le début de run d'un profil neuf devient donc plus dur qu'au plan 33 : à remesurer en M3, à deux profils.
- **Remboursement** libre et gratuit.
- **Ordre** : juste après le Péril visible (plan 34 G0), avant la refonte de l'inventaire. Les Poches (sac d'objets) arrivent avec le plan 35.

Dans tout ce plan, « l'Établi » se lit désormais « le Campement ».

## 9. Lot M0 livré : prix et économie — 11 octobre 2026

**La table des prix est dans les données** (`data/progression/camp_upgrades.json`), et `tools/camp_economy.py` la lit comme le jeu. Ce sont les prix proposés au §110, gardés tels quels :

| Rayon | Amélioration | Effet par rang | Rangs | Prix par rang | Total |
|---|---|---|---:|---|---:|
| Capacités | Râtelier | +1 emplacement d'arme (2 → 4) | 2 | 400 / 2 500 | 2 900 |
| Capacités | Jeu de cartes | +1 relance par run | 3 | 300 / 800 / 2 000 | 3 100 |
| Capacités | Boîte d'allumettes | +1 bannissement gratuit par run | 3 | 300 / 800 / 2 000 | 3 100 |
| Stats | Sac de couchage | PV max +5 % | 5 | 250 / 600 / 1 200 / 2 400 / 4 000 | 8 450 |
| Stats | Pierre à aiguiser | dégâts +4 % | 5 | idem | 8 450 |
| Stats | Métronome | cadence +3 % | 5 | idem | 8 450 |
| Stats | Bonnes chaussures | vitesse +2 % | 5 | idem | 8 450 |
| Stats | Marmite | régénération +0,1 PV/s | 5 | idem | 8 450 |
| Stats | Rembourrage | armure +1 | 5 | idem | 8 450 |
| Stats | Filet à papillons | attraction de l'XP +10 % | 5 | idem | 8 450 |
| Stats | Carnet de notes | XP +4 % | 5 | idem | 8 450 |
| Stats | Patte de lapin | Chance +3 % | 5 | idem | 8 450 |
| Confort | Bocal d'Essence | +20 Essence au départ | 5 | 150 / 350 / 700 / 1 200 / 2 000 | 4 400 |
| | **Campement plein** | | | | **89 550** |

Les noms sont des objets du camp, dans l'esprit des Écussons (§110) : à valider. Quatre ont été changés en M3 (Sac de couchage, Marmite, Filet à papillons, Patte de lapin) : les premiers recoupaient l'objet « Aimant de frigo » et les bonus lâchés « Fer à cheval », « Gourde » et « Couverture de survie ». Un rang multiplicatif vaut 1 + pas × rang, sans composer. Au maximum : PV ×1,25, dégâts ×1,2, cadence ×1,15, vitesse ×1,1.

**Ce que rapporte une run, mesuré.**
- Méthode : Vagabond en profil neuf, bot mortel qui va vers les lieux (`--nomad --visit --mortal`), 4 graines (42, 1002, 7, 123), 30 min.
- Le bot ne meurt pas : un coup fatal le remet à fond. Ce qu'il a gagné à la minute *d* est donc ce que gagne un joueur mort à cette minute.
- La mesure est faite avant le M1 : quatre emplacements d'armes. Le score vient d'une nouvelle colonne `score` des CSV de `RunObservation`.

| Minute | 5 | 10 | 15 | 20 | 25 | 30 |
|---|---:|---:|---:|---:|---:|---:|
| Vestiges gagnés, moyenne | 267 | 792 | 1 460 | 2 185 | 3 018 | 4 042 |
| Écart entre graines | 202–340 | 638–965 | 943–2 153 | 1 365–3 177 | 1 824–4 047 | 2 668–5 255 |

C'est l'ordre de grandeur annoncé au §110 : environ une élimination pour un Vestige, 500 à 1 000 pour une run courte, 3 000 à 5 000 pour 30 min.

**Nombre de runs pour tout acheter** (`python3 tools/camp_economy.py <mesures>`). Deux façons d'acheter sont simulées : le moins cher d'abord, ou les emplacements d'armes d'abord.

| Run type | Vestiges par run | Runs pour le Campement plein | 3ᵉ arme, après la run… | 4ᵉ arme, après la run… |
|---|---:|---:|---|---|
| Débutant (mort entre 5 et 10 min) | 530 | 170 | 1 (armes d'abord) ou 8 (moins cher d'abord) | 6 ou 102 |
| Moyenne (15 à 25 min) | 2 221 | **41** | 1 ou 2 | 2 ou 25 |
| Bonne (30 min) | 4 042 | 23 | 1 ou 1 | 1 ou 14 |

**Conclusion : les prix tiennent la cible** (30 à 50 runs moyennes ; 41 mesurées). Rien n'est changé. Deux remarques pour Raphaël :
- Le 4ᵉ emplacement (2 500) est le rang le plus utile et l'un des plus chers. Un joueur qui achète au moins cher l'aura tard (25 runs moyennes) ; s'il économise pour lui, il l'a très tôt. L'écran le montre en tête du rayon Capacités.
- Un débutant (530 par run) met 170 runs à tout acheter. C'est voulu : il progresse en jouant, et ses runs s'allongent à mesure qu'il achète. Ce chiffre sera revu en M3 avec les gains d'un profil neuf à deux armes.

## 10. Lot M1 livré : données, sauvegarde, application en run — 11 octobre 2026

**Données.** `CampUpgradeDataLoader` lit `camp_upgrades.json` et le contrôle en entier au chargement, comme les autres contrats. Un fichier invalide est refusé avec sa raison, et la run ne se charge pas (`GameBootstrap`). Sont refusés :
- un effet ou un rayon inconnu, une clé inconnue, un identifiant en double ;
- une stat que le joueur ne lit pas avec ce modificateur ;
- plus de 4 emplacements d'armes au total ;
- un prix nul, ou plus bas que celui du rang précédent ;
- une traduction absente.

Le contrat des objets n'admet les PV qu'en additif ; le Campement les donne en pourcentage. Il garde donc sa propre liste des couples stat / modificateur que `Player.ApplyPerkModifier` applique.

**Sauvegarde** (`meta_save.json`, sans changer de version) :
- `camp_ranks` donne le rang acheté par amélioration ;
- `camp_paid` garde les Vestiges versés pour chacune. Un remboursement rend donc ce qui a été payé, même si les prix ont changé depuis.
- Achat, remboursement d'un rang et « tout rendre » passent par `MetaSaveManager`, en une seule écriture.
- À la normalisation :
  - une amélioration retirée du jeu disparaît et ses Vestiges sont rendus ;
  - un rang est borné au maximum ;
  - un montant négatif est ramené à 0.
- Les profils existants gardent leurs Vestiges, et rien n'est acheté pour eux.

**Emplacements d'armes.** `Player.MaxWeaponSlots` (4) reste le plafond absolu et la taille du HUD. La run fixe sa limite par `Player.WeaponSlotLimit` : 2 pour un profil neuf, +1 par rang du Râtelier. Un joueur construit hors run, comme dans les tests, garde 4. Tous les points d'ajout lisent la limite : `Player.AddWeapon`, l'arme au sol (`WeaponPickup`), le wagonnet (`SmallPlaceDirector`) et l'offre de niveau (`FragmentManager`). La fiche du joueur affiche « n/limite ».

**Application** (`scripts/Meta/CampUpgrades.cs`). Elle se fait une fois, dans `GameBootstrap.InitializeCharacterAndRun`, juste après `InitializeCharacter` :
- limite d'armes ;
- relances et bannissements gratuits (`FragmentManager`, prêt à ce moment) ;
- Essence de départ (`EssenceTracker.AddEssence`, sans compter comme Essence gagnée) ;
- stats, par `ApplyPerkModifier`.

**Mode dev : toujours 4 emplacements d'armes.** Les bancs, les captures et les builds de référence portent souvent quatre armes. Les autres rangs suivent la sauvegarde dev (`user://dev/`), qu'on peut remplir et vider au Campement.

**Tests.** Nouvelle suite `camp` (`tools/test_camp.sh`, branchée dans `tools/validate.sh`) en trois passages, chacun sur un profil neuf :
1. 57 contrôles :
   - catalogue, et 13 fichiers refusés ;
   - achat, refus faute de Vestiges, rang maximal, remboursement d'un rang et de tout, rechargement ;
   - profil abîmé à la main, puis normalisé ;
   - totaux et application au joueur : deux armes au plus en profil neuf, et avec tous les rangs PV ×1,25, dégâts ×1,2, cadence ×1,15, vitesse ×1,1, régénération +0,5, armure +5, Chance +0,15, attraction ×1,5, XP ×1,2, +3 relances, +3 bannissements, 100 Essence.
2. Une vraie run (`Main`) en profil neuf : 2 emplacements, rien d'autre.
3. Une vraie run avec le Campement plein acheté par la boutique : 4 emplacements, toutes les capacités et les dégâts.

`RunObservation --camp-full` achète tout le Campement avant la run, pour les mesures à deux profils (M3).

## 11. Lot M2 livré : page Campement, HUD verrouillé, bilan — 11 octobre 2026

**Page du Hub.** L'entrée « Campement » se place entre Personnages et Quêtes (`HubCampUpgradesPanel`, sur le modèle des autres pages). L'écran se lit ainsi :
- en haut, le solde de Vestiges, une ligne d'état et « Tout rendre » ;
- à gauche, les Capacités puis le Confort ; à droite, les neuf Statistiques ;
- chaque ligne donne le nom, l'effet acquis et le suivant (« +8 % → +12 % », « 2 → 3 emplacements d'armes »), le rang (« Rang 2/5 ») et le prix du rang suivant, éteint s'il est hors de portée ;
- le bouton « Rendre » de la ligne rembourse le dernier rang ;
- en bas, le détail de la ligne active : effet par rang et prix suivant.

Navigation :
- Valider sur une ligne achète le rang suivant. Un refus dit ce qui manque (« Il manque 400 Vestiges. ») avec le son de refus.
- Gauche et droite passent de la ligne à son « Rendre ». Retour revient à l'accueil, dont le solde se met à jour.
- Un « Rendre » éteint par un remboursement rend le focus à sa ligne.

Si le catalogue est refusé, la ligne d'état le dit. En mode dev, elle rappelle que les armes restent à quatre. Le solde de l'accueil s'écrit désormais avec séparateur de milliers.

**HUD.** Les cases d'armes au-delà de la limite de la run sont éteintes et portent un cadenas dessiné en pixels (`HudSlotLock`, dessiné une fois). Un profil neuf voit donc une arme, une case libre et deux cadenas. Capture en profil neuf regardée : `CAPTURE_PROFILE=normal tools/capture_run.sh` (nouvelle option, le profil dev reste le défaut).

**Bilan.** La carte « +N Vestiges » existait déjà. Une carte la suit : « N Vestiges à dépenser au Campement », masquée si l'écriture de fin de run a échoué.

**Vérifications.**
- Build sans avertissement.
- Suites : `smoke`, `camp`, `weapons`, `small_places`, `objects`, `movement`, `choice_screen`, `quests`, `catalogs`, `run_trace`, `dev_mode`, `saves`, `launchers`. Toutes vertes, `objects` en locale française (voir plus bas).
- Captures de la page sous `xvfb-run` (`tools/capture_hub.sh`) : mise en page, focus d'une ligne à son « Rendre », refus faute de Vestiges.
- Relecture `godot-reviewer`. Corrigés : « 4 → 5 emplacements » affiché en mode dev, focus laissé sur un bouton éteint, espaceur en trop à la réouverture, catalogue refusé sans message, solde sans séparateur, carte de solde après un échec d'écriture, commentaire sur l'ordre d'application.

**Défauts d'environnement trouvés en route, sans lien avec le Campement :**
- `quests` échouait dans `validate.sh` : le dossier du profil n'existait pas encore au premier `find`. Le lanceur le crée maintenant ; `test_camp.sh` fait de même.
- `objects` échoue dans un conteneur en anglais : deux contrôles lisent des textes de cartes en français. La suite passe avec `LANG=fr_FR.UTF-8`. Rien n'est changé.

**Une quête dépend maintenant du Campement.** « Quatre airs » (`four_tunes` : quatre armes au niveau 3) débloque la Boîte à musique. Avec deux emplacements au départ, elle demande les deux rangs du Râtelier (2 900 Vestiges, soit 2 runs moyennes, ou 6 de débutant en économisant). Question à Raphaël au §13.

## 12. Lot M3 livré : mesure à deux profils — 11 octobre 2026

**Protocole.** `MEASURE_PROFILE=normal MEASURE_JOBS=2 MEASURE_EXTRA_ARGS="--nomad --visit --mortal --character vagabond" tools/measure_run.sh <dossier> 600 "42 1002 7 123"`. Trois profils, au même commit pour les deux derniers :

| | 4 armes, rien d'acheté (avant M1) | **Profil neuf** (2 armes) | **Campement plein** (`--camp-full`) |
|---|---:|---:|---:|
| Éliminations à 5 min, moyenne | 267 | 202 (76 %) | 377 (141 %) |
| Éliminations à 10 min, moyenne | 867 | **672 (78 %)** | 1 198 (138 %) |
| Graines (10 min) | 682–1 058 | 490–802 | 940–1 337 |
| Niveau à 10 min, moyenne | 23,2 | 21,5 | 30,2 |
| « Morts » du bot de 0 à 10 min (remis à fond) | 45 | 57,5 (+28 %) | 16,8 (−63 %) |
| Dégâts reçus cumulés à 10 min (bot qui n'esquive pas) | 45 500 | 80 800 | 27 300 |
| Vestiges gagnés à 10 min | 792 | 587 | 1 100 |

Les éliminations du plan 33 §7 (926 à 10 min) venaient d'autres graines. Rapporté à elles, le profil neuf en fait 73 %.

**Lecture.**
- **Le profil neuf reste au-dessus du seuil.** À 10 min, il fait 78 % des éliminations d'un départ à quatre armes, pour un seuil d'alerte fixé à 60 %. Le début est plus dur, sans être cassé. Le bot reçoit 78 % de dégâts en plus et « meurt » 28 % plus souvent : c'est là que la différence se sent.
- **Le Campement plein ne rend pas le début trivial,** mais il le rend nettement plus confortable : le bot y atteint le niveau 30 à 10 min, la cible du §103 pour une bonne run humaine. Les « morts » chutent des deux tiers. Ce n'est pas l'emplacement d'arme qui pèse le plus, mais la défense : armure +5 (réduction des dégâts de 12 à 32 % dès le départ), PV ×1,25 et régénération +0,5/s.
- **Gains d'un débutant à deux armes** : 386 Vestiges par run courte (185 à 5 min, 587 à 10 min). La 3ᵉ arme s'achète après la 2ᵉ run, la 4ᵉ après la 8ᵉ, s'il économise pour elles (`tools/camp_economy.py`).

**Rien n'est appliqué.** Le profil neuf ne passe pas sous les 60 %. Deux pistes si le début paraît trop dur en jouant, ou le profil plein trop sûr :
1. **Profil neuf** : 3ᵉ emplacement à 250 au lieu de 400, pour qu'il tombe dès la première run courte ; ou bien, en run, le deuxième emplacement s'ouvre au niveau 5 au lieu d'être là dès le départ, ce qui laisse le joueur apprendre une arme d'abord.
2. **Profil plein** : armure +0,5 par rang (2,5 au maximum, au lieu de 5), qui reste la stat la plus forte du rayon ; ou laisser faire le Péril, que le joueur relève lui-même (§4).

## 14. Lot M4 livré : Poches — 11 octobre 2026

- **Poches** (`camp_pockets`, rayon Capacités, effet `bag_slots`) : +3 places dans le sac d'objets par rang, 5 rangs, de 10 à 25 places (plan 35 §3.3).
- Prix : 300, 700, 1 400, 2 500, 4 000 Vestiges, soit 8 900 pour les cinq rangs. Le Campement plein passe de 89 550 à 98 450 Vestiges : environ 45 runs moyennes au rythme mesuré en M3, dans la cible de 30 à 50.
- Appliqué au début de la run par `CampUpgrades.Apply` (`Player.SetBagCapacity`) ; la barre du bas ajoute ses cases et se recentre (plan 35 I1).
- La page du Campement dit « Sac de 13 → 16 places », comme le Râtelier dit ses emplacements.
- Vérifications : suite `camp` (14 améliorations, profil neuf à 10 places, Campement plein à 25, appliqué au joueur), `inventory`, `catalogs`, `saves`, `smoke` ; capture de la page en profil neuf regardée (ligne Poches sous le Râtelier, « Sac de 10 → 13 places »).

## 13. Questions pour Raphaël (Campement)

1. **Noms des améliorations** (§9) : Râtelier, Jeu de cartes, Boîte d'allumettes, Sac de couchage, Pierre à aiguiser, Métronome, Bonnes chaussures, Marmite, Rembourrage, Filet à papillons, Carnet de notes, Patte de lapin, Bocal d'Essence. Te vont-ils ?
2. **La quête « Quatre airs »** (★, quatre armes au niveau 3, débloque la Boîte à musique) demande maintenant d'avoir acheté les deux rangs du Râtelier. On la garde ainsi (elle devient une quête de milieu de progression, à passer en ★★), ou on la ramène à trois armes ?
3. **Début à deux armes** : à juger en jouant. Faut-il une des pistes du §12 ?
4. **Armure** : +1 par rang est la stat la plus forte du Campement plein. On la garde, ou on la passe à +0,5 ?
5. **4ᵉ emplacement à 2 500** : bien placé ? Un joueur qui achète au moins cher l'aura vers la 25ᵉ run moyenne.

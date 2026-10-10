# Plan 36 — La progression permanente : des améliorations au Hub

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
| M4 | Poches (sac d'objets), une fois le plan 35 I2 livré |

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

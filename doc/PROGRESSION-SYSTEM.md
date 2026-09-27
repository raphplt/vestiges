# VESTIGES — Système de progression en run

> **Version :** 2.0
> **Date :** 26 septembre 2026 (réécriture ; version 1.0 du 1er mars 2026)
> **Statut :** état du système en V2, et évolutions prévues par le [plan 17](plans/17-armes-coffres-modificateurs.md)
> **Docs liés :** [Stratégie V2](VESTIGES-STRATEGIE-V2.md) §10 et §13, `data/weapons/weapons.json`, `data/progression/passive_souvenirs.json`, `data/perks/perks.json`

---

## Le problème

Le level-up est le moment fort de la run. Il doit être rapide, lisible et offrir un vrai choix. Si tout y passe (armes, stats, effets, améliorations), le choix devient confus et la montée en puissance illisible.

Réponse de Vestiges : le level-up ne traite que ce que le joueur porte (armes et Souvenirs passifs), et les gains de stats globaux viennent du monde qu'il arrache à l'oubli.

---

## Trois sources qui ne se mélangent pas

### 1. Le level-up : Fragments de mémoire

À chaque niveau, le joueur choisit **un Fragment parmi trois** : une **arme** ou un **Souvenir passif**.

- **Armes** : elles fixent le motif d'attaque automatique. **Quatre emplacements.** Reprendre une arme possédée l'améliore.
- **Souvenirs passifs** : effets permanents (dégâts, cadence, PV, vitesse, zone…). **Quatre emplacements.** Reprendre un passif possédé l'améliore.
- **Emplacements pleins** : seules les améliorations sont proposées. Début de run, on découvre ; fin de run, on affine.
- Un choix devenu impossible pendant l'écran (arme ramassée entre-temps) relance une offre à jour.

### 2. Le monde : perks et butin

Les perks de stats (« +15 % dégâts », « +20 PV »…) ne passent pas par le level-up. Ils viennent du monde :

- **Coffres** : 23 par carte, sur toute la carte, signalés par une colonne de lumière à la couleur de leur rareté. Ils donnent Essence, XP, armes (avec rareté), perks et Souvenirs.
- **Micro-événements et élites** : coffres de récompense.
- **Mémoriaux** (cinq par carte) : trois éclats à rassembler les raniment ; bénédiction à rareté, puis services contre de l'Essence (raviver une arme au choix, Rare au moins ; soin ; lever un Oubli).
- **Failles** : une amélioration Épique ou Légendaire contre un **Oubli** (malus durable) et un point de **Péril**. Toujours refusable. D'autres s'ouvrent là où le monde s'efface.

Les perks s'empilent sans emplacement. Explorer et ouvrir des coffres rend objectivement plus fort, sans alourdir l'écran de niveau. Le tirage exclut les perks liés à des systèmes absents et les passifs d'autres personnages.

### 3. Les évolutions

Les **fusions** (arme et passif au maximum → arme évoluée) sont détectées mais jamais appliquées. Le plan 17 propose de les remplacer par l'**Éveil** : à un certain niveau, une condition liée à l'histoire de l'objet transforme l'arme en une version unique (vague 5).

---

## Pourquoi ça tient dans l'univers

- **Fragments de mémoire** : à chaque niveau, le joueur se souvient d'une arme, d'un geste, d'une aptitude. Le monde s'efface ; lui se renforce parce qu'il se souvient.
- **Souvenirs passifs** : se souvenir qu'on peut courir vite, c'est courir vite.
- **Butin du monde** : un coffre intact dans une ruine contient un fragment de ce que le monde était. L'ouvrir, c'est l'arracher à l'oubli.

---

## Emplacements et sources

| Système | Emplacements | Source | Améliorable |
|---|---|---|---|
| Armes | 4 | Level-up, coffres, élites | Oui, en reprenant la même arme |
| Souvenirs passifs | 4 | Level-up | Oui, en reprenant le même passif |
| Perks | Sans limite | Coffres, événements | Non ; plusieurs exemplaires se cumulent |
| Bénédictions | Sans limite | Mémoriaux | Non |
| Oublis | Sans limite | Failles | Levés au Mémorial |

---

## Évolutions prévues (plan 17)

- **Level-up à raretés** : chaque amélioration tire une rareté (Commun à Légendaire) qui fixe l'ampleur du gain et le nombre de stats touchées ; la carte montre « avant → après ».
- **Stats montables par arme** en données, au lieu d'une table commune ; plafond de niveau en données.
- **Chance et Péril** (livrés, vague 3) : la Chance, l'oubli de la zone et le Péril font monter la rareté de tous les tirages (level-up, Mémorial, Faille). Le Péril renforce aussi les créatures et majore XP et score (`data/scaling/peril.json`).
- **L'oubli comme monnaie du risque** (livré) : plus la zone est oubliée, plus les raretés montent.
- La rareté ne vit que sur les améliorations, pas sur l'arme (décision du 26 septembre, plan 17 §4.3).

---

## Ce qu'on ne fait pas

- Pas de marchand pour acheter armes ou passifs.
- Pas de suppression d'arme ou de passif en cours de run. Seule exception : une arme trouvée au sol peut s'échanger contre celle du premier emplacement.
- Pas de réorganisation des emplacements : l'ordre ne compte pas.

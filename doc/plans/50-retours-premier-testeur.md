# Plan 50 — Retours du premier testeur

15 octobre 2026. Un joueur a découvert le jeu pendant une demi-heure, en vocal avec Raphaël. Retours et réponses au [registre §184](DECISIONS.md). Ordre de travail demandé : « tout dans l'ordre », un lot à la fois.

## 1. Relevé du code avant de trancher

| Retour | Ce que faisait le jeu |
|---|---|
| Coffres impossibles dans une foule | Jauge courte (0,5 à 1,2 s), mais **tout déplacement l'annulait** : dans une foule, il faut bouger. |
| Créatures nées « dans la gueule » | Juste hors du cadre, 40 à 140 px du bord (20 à 70 px en Résurgence), une fois sur deux devant le joueur ; l'écran étant large, le bord haut et bas est proche. La vitesse n'était pas prise en compte. |
| Vestiges trop rares | 0,4 par élimination depuis le §168 (divisé par 3 la veille). |
| Brouillard trop fort | Météo « brouillard épais » : net jusqu'à 170 px, plein à 360 px, voile à 82 %. |
| Joueur qui ne bouge pas | Rien ne pousse avant le sol Fragile (vers 7 min) ; la présence du joueur ralentit même l'Effacement autour de lui (`player_decay_reduction` 0,72). Repères de coffres au bord de l'écran seulement avec l'Étiquette perdue. |
| Dash sans intérêt | Aucune invulnérabilité (0 ms, §211 du registre), recharge 2,5 s. |
| Essence = éliminations | 1 par créature ordinaire, 4 par élite, 8 par boss, valeurs en dur dans `EssenceTracker`. |
| Prix des coffres | +8 % par minute de run (`item_loot.json`). |

## 2. Lots

| Lot | Contenu | État |
|---|---|---|
| **T1** Réglages | Coffres à l'appui, collés (40 px) ; autres lieux ÷2 et fouille qui suit le joueur ; anneau d'apparition ; Vestiges 0,7 ; brouillard ; début de run ; dash invulnérable | **Livré le 15 octobre** |
| T2 Campement | Retrait PV, dégâts, cadence, vitesse, régénération, armure (sans remboursement, §152) ; nouvelle amélioration « gain de Vestiges » | **Livré le 15 octobre** |
| T3 Niveaux de difficulté | Simple et Normal ouverts, Difficile et Impossible à gagner en battant l'Indicible ; nombre, PV, dégâts, boss, multiplicateur de Vestiges ; conception au plan 47 L4 et 46 | À concevoir puis faire |
| T4 Essence et coffres | Essence par type de créature dans son JSON ; prix des coffres qui monte par coffre ouvert et plus au temps ; coffres de boss et d'élites gratuits | À faire |
| T5 Lisibilité | Repères au bord de l'écran pour tous (l'Étiquette perdue à renforcer ou retirer) ; première run guidée ; dash montré dès le début | À concevoir puis faire |

## 3. T1, livré le 15 octobre

- **Coffres** : `open_time` remplacé par `open_range_px` (40 px, collision du coffre 12 px) dans `data/chests/chests.json` ; `Chest.HoldTime` vaut 0, l'ouverture se fait à l'appui. `IInteractable.InteractRadius` borne la recherche du lieu le plus proche (0 = portée du personnage).
- **Autres lieux** : durées divisées par deux dans `small_places.json` (voiture 0,4 s, boîte aux lettres 0,3 s…), `landmarks.json` (Mémorial 0,3 s, Faille et stèle 0,4 s) et `pois.json` (bâtiment fouillable 0,75 s). Bouger, dash compris, n'annule plus une fouille ; seul l'éloignement (×1,5 de la portée, ×2 pour les anciens POI) l'interrompt.
- **Apparition** (`spawn_flow.json`) : devant un joueur qui court, l'anneau recule de sa vitesse × 0,6 s (`spawn_speed_lead_seconds`) ; jamais à moins de 320 px (`spawn_min_distance_px`), Résurgence et fin de run comprises.
- **Vestiges** : `kills_per_vestige` 2,5 → 1,43.
- **Brouillard épais** : net 260 px, plein 480 px, opacité 0,6.
- **Début de run** : densité ×0,6 → ×0,57 ; XP des créatures nées dans la même fenêtre ×1,05 (`early_xp_multiplier`), qui revient à 1 sur la rampe de 10 à 14 min.
- **Dash** : invulnérable 0,15 s, soit tout le dash commun ; le début de la glissade et du saut (profils plus longs) aussi.

Vérifié : build sans avertissement, smoke, déplacements (tests réécrits : coffre ouvert à l'appui, fouille qui continue en bougeant, dash protégé, mort en dash par la variante de 60 ms), inventaire. Pas de mesure de densité : réglages tranchés par Raphaël. **À juger en jouant** : ouverture au contact, fouille en mouvement, distance d'apparition en courant, brouillard.

## 4. T2, livré le 15 octobre

- **Retirés** de `camp_upgrades.json` : Sac de couchage (PV), Pierre à aiguiser (dégâts), Métronome (cadence), Bonnes chaussures (vitesse), Marmite (régénération), Rembourrage (armure), avec leurs textes, leurs icônes et leurs fabriques dans `tools/sprites/ui/camp.py`. Gardés : Filet à papillons (aimant), Carnet de notes (XP), Patte de lapin (Chance).
- **Remboursement** : rien à construire. La normalisation du profil existante retire une amélioration inconnue et **rend ce qui avait été versé** pour elle, à la première lecture du profil (déjà testée par `CampRegression`).
- **Boîte en fer** (`camp_vestige_gain`, rayon Confort) : +10 % de Vestiges de fin de run par rang, cinq rangs à 300, 700, 1 400, 2 500 et 4 000. Effet `vestige_gain` (`CampEffect.VestigeGain`), total porté par `CampUpgrades.Totals.VestigeGain`, posé par `GameBootstrap` dans `ScoreManager.VestigeMultiplier`, appliqué par `ScoreConfig.VestigesFor(kills, multiplier)`. Le multiplicateur des niveaux de difficulté (T3) passera par le même endroit.
- **Campement plein** : 98 450 → 56 650 Vestiges, neuf améliorations au lieu de quatorze. À 0,7 Vestige par élimination, il se remplit environ 2,3 fois plus vite qu'avant T1, et plus encore une fois la Boîte en fer achetée. D'autres usages des Vestiges restent à trouver (§184).

Vérifié : build sans avertissement, smoke, Campement (catalogue, achats, remboursements, normalisation, application en run, multiplicateur en vraie run), mode dev ; capture de la page du Campement (rayons et fiche de la Boîte en fer) regardée. Icône de la Boîte en fer provisoire : boîte rouge ouverte, éclats dedans.

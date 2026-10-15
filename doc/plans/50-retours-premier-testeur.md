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
| T3 Niveaux de difficulté | Simple et Normal ouverts, Difficile et Impossible à gagner en battant l'Indicible ; nombre, PV, dégâts, boss, élites, Effacement, Péril de départ, Vestiges | **Livré le 15 octobre** |
| T4 Essence et coffres | Essence par type de créature dans son JSON ; prix des coffres qui monte par coffre ouvert et plus au temps ; coffres de boss et d'élites gratuits | **Livré le 15 octobre** |
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

## 5. T3, livré le 15 octobre

**Données** : `data/scaling/difficulty.json`, lu et contrôlé par `DifficultyDataLoader` (refus au chargement de la run, comme le Péril).

| Niveau | Ouvert | Créatures | PV | Dégâts | Élites | Effacement | Péril de départ | Vestiges |
|---|---|---|---|---|---|---|---|---|
| Simple | d'office | ×0,75 | ×0,8 | ×0,7 | ×1 | ×1 | 0 | ×1 |
| Normal | d'office | ×1 | ×1 | ×1 | ×1 | ×1 | 0 | ×1 |
| Difficile | Indicible battu en Normal | ×1,3 | ×1,35 | ×1,3 | ×1,3 | ×1,15 | 3 | ×1,25 |
| Impossible | Indicible battu en Difficile | ×1,6 | ×1,8 | ×1,7 | ×1,6 | ×1,3 | 6 | ×1,5 |

Avec le Péril de départ, l'effet au départ vaut environ ×1,7 créatures, ×1,55 PV et ×1,5 dégâts en Difficile ; ×2,5, ×2,3 et ×2,2 en Impossible (le nombre reste sous le plafond technique de 500 créatures).

**Où ça s'applique** :
- `SpawnManager.Difficulty`, lu au `_Ready` depuis `GameManager.DifficultyId` : cible locale et plafond de créatures, PV et dégâts à l'apparition (`ComputeScaling`) et des variantes calées sur le niveau (élites, Souverains), intervalle des élites naturelles ;
- la Barrière (PV des battants, coups de poing et chaîne) et l'Indicible (`EndgameManager`) lisent le niveau par le `SpawnManager` ; les gardiens des anciens POI aussi ;
- `ErasureManager` : vitesse de l'Effacement ;
- `GameBootstrap` : Péril de départ (visible au HUD) et multiplicateur de Vestiges, après celui de la Boîte en fer.

**Méta** : `difficulties_beaten` (niveaux où l'Indicible est tombé, inscrits au règlement de la run) et `last_difficulty` dans le profil ; mode dev : tout ouvert. Un relevé ancien sans niveau se lit comme Normal. Le `RunRecord` garde le niveau ; le bilan l'affiche à côté de la graine. Un seul classement, inchangé.

**Hub** : `DifficultySelector` sous la fiche du personnage, au-dessus de « Partir » : gauche et droite au clavier ou à la manette, un clic passe au suivant ; un niveau fermé dit ce qui l'ouvre et « Partir » devient « Difficulté verrouillée ».

**Bancs** : `--difficulty <id>` pour `RunObservation` (`MEASURE_EXTRA_ARGS`).

**Mesure** (5 min, graine 42, bot du profil dev, une seule graine : ordre de grandeur seulement) :

| Niveau | PV apparus | Éliminations | Niveau du joueur à 5 min | Créatures visibles (3–5 min) |
|---|---|---|---|---|
| Simple | 6 836 | 82 | 5 | 25 |
| Normal | 10 562 | 93 | 7 | 20 |
| Difficile | 17 352 | 20 | 2 | 47 |
| Impossible | 23 054 | 15 | 2 | 68 |

Le bot ne part pas avec la même arme d'un niveau à l'autre, les éliminations ne se comparent donc pas finement. Mais en Difficile, l'ouverture devient une boule de neige : moins d'éliminations, moins d'XP, des créatures qui s'entassent. **À juger en jouant** avec la méta d'un joueur qui a battu l'Indicible ; si l'ouverture écrase, réduire d'abord le Péril de départ.

Vérifié : build sans avertissement, smoke, sauvegardes (ouverture des niveaux, dernier choix, rechargement), Campement, Barrière, Indicible, mode dev ; capture du sélecteur en profil neuf (Normal, puis Difficile fermé) regardée. `test_menu_navigation` échoue pour une raison antérieure (il attend une Scaphandrière sans portrait, alors que `portrait.webp` existe) : tâche à part.

## 6. T4, livré le 15 octobre

- **Essence par créature** : champ `essence` obligatoire dans `stats` de chaque fiche (`data/enemies/_contract.json`), lu par `EnemyStats.Essence` ; `EssenceTracker` n'a plus de valeurs en dur (1 / 4 / 8 selon le rang). Valeurs d'essai : 1 pour Ombre, Charognard, Rampeur d'ombre et Présage ; 2 pour Cracheur, Rampant, Tisseuse, Rôdeur et Sentinelle ; 3 pour Brute, Hurleur et Tréant ; 8 pour les parties de boss et l'Indicible (inchangé). Bonus des variantes et chance de la zone effacée inchangés.
- **Mesure** (10 min, profil dev, graines 42 et 7) : 632 éliminations pour 947 Essence (×1,5) et 610 pour 759 (×1,24) ; avant, le rapport restait vers ×1,1.
- **Prix des coffres** : `chest_cost_growth_per_open` 0,2 remplace `chest_cost_growth_per_minute` dans `item_loot.json` : prix de base × (1 + 0,2 × coffres payés déjà ouverts), soit 20, 24, 28… 40 au sixième coffre commun. Le compteur est un état de run tenu par `WorldInteraction` ; le prix est recalculé en direct (il n'est plus figé au premier devis). Arrondi au supérieur avec une tolérance (45 × 1,2 = 54, pas 55).
- **Coffres gratuits** : `Chest.AlwaysFree` pour les coffres lâchés par une créature à variante (élites, Souverains) et par la Barrière ; invite « Ouvrir — gratuit » ; ils ne font pas monter le prix des suivants.

Vérifié : build sans avertissement, smoke, butin (prix par ouverture, invite refaite quand le prix monte, coffre gratuit), catalogues (contrat des créatures), quêtes, objets. Le bot n'ouvre pas de coffres payants : la courbe de prix se juge en jouant.

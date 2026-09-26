# Couverture audio — VESTIGES

Catalogue exhaustif **des correspondances des données et systèmes repérés**, pas validation artistique ou recette en jeu. Les besoins supplémentaires restent à arbitrer. Aucun son n’est retenu automatiquement.

[Écouter le lot A](lot-a/index.html) · [Écouter le lot A2](lot-a2/index.html) · [Retours A](lot-a/RETOURS.md) · [Retours A2](lot-a2/RETOURS.md) · [Catalogue JSON](catalogue.json) · [Plan audio](../plans/15-audio.md) · [Inventaire des fichiers et appels](../plans/15-audio-inventaire.md)

**113 besoins actuels**, 11 avec candidats (51 propositions), 6 choisis, 0 intégrés, 0 recettés. 16 entrées hors dénominateur (plans futurs, héritage V1, anciens mappings météo).

## Statuts

| Statut | Besoins actuels |
|---|---:|
| actuel conservé | 1 |
| candidats prêts | 1 |
| retenu | 6 |
| à rechercher | 102 |
| à retravailler | 3 |

## Retours et nouvelle recherche

Candidats retenus : `critical_hit_a`, `dissolution_a2_b`, `perk_select_a2_a`, `chest_open_a`, `dash_start_a`, `danger_warning_a2_a`.

Besoins à retravailler : `enemy_hit`, `player_hit`, `level_up`.

Sons actuels explicitement conservés : `chest_reveal`.

Les décisions et notes exportées restent intactes dans `raphael_choice` et `review_history`. Le statut « actuel conservé » consigne une instruction explicite dans les notes sans fabriquer un candidat choisi. Les préparations spécifiques figurent dans `integration.preparation` ; elles ne valent pas intégration ni recette en jeu.

Lot A2 : `enemy_hit`, `player_hit`, `dissolution`, `level_up`, `perk_select`, `danger_warning`, `chest_reveal`.

## Premier panier

`enemy_hit`, `critical_hit`, `player_hit`, `dissolution`, `xp_pickup`, `level_up`, `perk_select`, `chest_open`, `dash_start`, `danger_warning`.

## Registre par besoin

| ID | Famille | Déclencheur | État actuel | Suivi |
|---|---|---|---|---|
| `enemy_hit` | combat | Impact sur ennemi | appel existant, audibilité non recettée | à retravailler |
| `critical_hit` | combat | Impact critique | appel existant, audibilité non recettée | retenu |
| `player_hit` | joueur | Dégât reçu | appel existant, audibilité non recettée | à retravailler |
| `dissolution` | combat | Mort ennemi / dissolution ; proxy chute de relique | appel existant, audibilité non recettée | retenu |
| `xp_pickup` | progression | Collecte orbe XP | appel existant, audibilité non recettée | candidats prêts |
| `level_up` | progression | Entrée montée de niveau | appel existant, audibilité non recettée | à retravailler |
| `perk_select` | progression | Choix perk, fragment ou butin | appel existant, audibilité non recettée | retenu |
| `chest_open` | monde | Ouverture physique du coffre | appel existant, audibilité non recettée | retenu |
| `dash_start` | joueur | Départ esquive | appel existant, audibilité non recettée | retenu |
| `danger_warning` | événements | Annonce Résurgence, boss/colosse et micro-événement | appel existant, audibilité non recettée | retenu |
| `chest_reveal` | progression | Mélodie après ouverture du coffre | Actuellement un seul fichier accompagne ouverture et présentation ; aucune couche de révélation distincte. | actuel conservé |
| `dash_end` | joueur | Fin esquive | appel existant, audibilité non recettée | à rechercher |
| `player_heavy_hit` | joueur | Coup majeur reçu | appel existant, audibilité non recettée | à rechercher |
| `player_heal` | joueur | Soin effectif | branchement audio dédié non relevé | à rechercher |
| `player_death` | joueur | Mort joueur | branchement audio dédié non relevé | à rechercher |
| `level_up_exit` | progression | Sortie choix level-up | appel existant, audibilité non recettée | à rechercher |
| `perk_refuse` | progression | Refus de choix | appel existant, audibilité non recettée | à rechercher |
| `rare_fragment` | progression | Révélation fragment rare | clé appelée mais absente du registre ; aucun son chargé pour cet appel | à rechercher |
| `essence_gain` | progression | Gain d’Essence | branchement audio dédié non relevé | à rechercher |
| `weapon_equip` | progression | Équiper / échanger arme | branchement audio dédié non relevé | à rechercher |
| `weapon_upgrade` | progression | Améliorer arme | branchement audio dédié non relevé | à rechercher |
| `weapon_drop` | progression | Déposer arme | branchement audio dédié non relevé | à rechercher |
| `passive_gain` | progression | Obtenir / améliorer passif | branchement audio dédié non relevé | à rechercher |
| `synergy_activate` | progression | Synergie activée | branchement audio dédié non relevé | à rechercher |
| `fusion_ready` | progression | Fusion disponible | branchement audio dédié non relevé | à rechercher |
| `fusion_complete` | progression | Fusion terminée | branchement audio dédié non relevé | à rechercher |
| `curse_accept` | progression | Accepter objet maudit | appel existant, audibilité non recettée | à rechercher |
| `quest_complete` | progression | Quête accomplie | branchement audio dédié non relevé | à rechercher |
| `souvenir_found` | méta | Découverte souvenir | appel existant, audibilité non recettée | à rechercher |
| `artifact_found` | monde | Découverte lore / sanctuaire / relique | appel existant, audibilité non recettée | à rechercher |
| `world_reveal` | monde | Zone découverte | Fichier enregistré ; OnZoneDiscovered est vide, aucune lecture de la clé relevée. | à rechercher |
| `world_erase` | monde | Zone qui s’efface | branchement audio dédié non relevé | à rechercher |
| `altar_upgrade` | monde | Autel amélioration | branchement audio dédié non relevé | à rechercher |
| `altar_reforge` | monde | Autel reforge | branchement audio dédié non relevé | à rechercher |
| `altar_heal` | monde | Autel soin | branchement audio dédié non relevé | à rechercher |
| `interaction_unavailable` | monde | Interaction impossible / recharge | branchement audio dédié non relevé | à rechercher |
| `poi_search` | monde | Fouille POI | branchement audio dédié non relevé | à rechercher |
| `poi_activate` | monde | Activer POI | branchement audio dédié non relevé | à rechercher |
| `ui_hover` | interface | Survol | appel existant, audibilité non recettée | à rechercher |
| `ui_click` | interface | Clic | appel existant, audibilité non recettée | à rechercher |
| `ui_confirm` | interface | Confirmer | appel existant, audibilité non recettée | à rechercher |
| `journal_open` | interface | Ouvrir journal | appel existant, audibilité non recettée | à rechercher |
| `journal_close` | interface | Fermer journal | appel existant, audibilité non recettée | à rechercher |
| `meta_unlock` | méta | Déblocage personnage / collection | branchement audio dédié non relevé | à rechercher |
| `memorial_activate` | méta | Activer mémorial / constellation | branchement audio dédié non relevé | à rechercher |
| `run_departure` | interface | Départ run / transition | branchement audio dédié non relevé | à rechercher |
| `event_success` | événements | Objectif micro-événement réussi | appel existant, audibilité non recettée | à rechercher |
| `event_fail` | événements | Objectif expiré / perdu | branchement audio dédié non relevé | à rechercher |
| `crisis_start` | événements | Début Résurgence | branchement audio dédié non relevé | à rechercher |
| `crisis_end` | événements | Fin Résurgence | branchement audio dédié non relevé | à rechercher |
| `relic_fall` | événements | Relique en chute / atterrissage | appel existant, audibilité non recettée | à rechercher |
| `shard_strike` | événements | Chute d’éclat et impact | branchement audio dédié non relevé | à rechercher |
| `stampede_pass` | événements | Passage de harde | branchement audio dédié non relevé | à rechercher |
| `vigil_hold` | événements | Veille : présence / progression | branchement audio dédié non relevé | à rechercher |
| `low_health` | ambiances | Santé basse | appel existant, audibilité non recettée | à rechercher |
| `erasure_near` | ambiances | Proximité Effacement / bord de carte | appel existant, audibilité non recettée | à rechercher |
| `level_up_wait` | ambiances | Attente choix niveau | appel existant, audibilité non recettée | à rechercher |
| `colossus_presence` | ambiances | Colosse présent | appel existant, audibilité non recettée | à rechercher |
| `step_grass` | joueur | Pas sur grass | appel existant, audibilité non recettée | à rechercher |
| `step_forest` | joueur | Pas sur forest | appel existant, audibilité non recettée | à rechercher |
| `step_concrete` | joueur | Pas sur concrete | appel existant, audibilité non recettée | à rechercher |
| `step_water` | joueur | Pas sur water | appel existant, audibilité non recettée | à rechercher |
| `step_gravel` | joueur | Pas sur gravier / carrière | Fichier utilisé pour esquive ; surface gravier dédiée non relevée. | à rechercher |
| `step_wood` | joueur | Pas sur bois | Fichier enregistré, non sélectionné par les pas actuels. | à rechercher |
| `blade_swing` | armes | Lame légère | branchement audio dédié non relevé | à rechercher |
| `heavy_swing` | armes | Frappe lourde | branchement audio dédié non relevé | à rechercher |
| `bow_release` | armes | Corde et flèche | branchement audio dédié non relevé | à rechercher |
| `crossbow_release` | armes | Mécanisme arbalète | branchement audio dédié non relevé | à rechercher |
| `throw_release` | armes | Lancer de pierre / hache | branchement audio dédié non relevé | à rechercher |
| `whip_snap` | armes | Fouet de câbles | branchement audio dédié non relevé | à rechercher |
| `bell_pulse` | armes | Cloche et onde | branchement audio dédié non relevé | à rechercher |
| `light_shot` | armes | Projection de lumière | branchement audio dédié non relevé | à rechercher |
| `music_box_note` | armes | Notes orbitales | branchement audio dédié non relevé | à rechercher |
| `chain_jump` | armes | Chaîne et rebonds | branchement audio dédié non relevé | à rechercher |
| `needle_shot` | armes | Aiguille à tête chercheuse | branchement audio dédié non relevé | à rechercher |
| `camera_flash` | armes | Flash photographique | branchement audio dédié non relevé | à rechercher |
| `essence_shot` | armes | Orbe d’Essence | branchement audio dédié non relevé | à rechercher |
| `void_slash` | armes | Tranchant du Vide | branchement audio dédié non relevé | à rechercher |
| `lantern_fire` | armes | Flamme mémorielle / zone | branchement audio dédié non relevé | à rechercher |
| `echo_strike` | armes | Frappe puis écho retardé | branchement audio dédié non relevé | à rechercher |
| `drawing_launch` | armes | Dessin animé / impact de forme | branchement audio dédié non relevé | à rechercher |
| `radio_wave` | armes | Émission radio soutenue | branchement audio dédié non relevé | à rechercher |
| `clock_strike` | armes | Horloge / temps ralenti | branchement audio dédié non relevé | à rechercher |
| `pounce_windup` | ennemis | Préparation bond | clé appelée mais absente du registre ; aucun son chargé pour cet appel | à rechercher |
| `pounce_leap` | ennemis | Bond | clé appelée mais absente du registre ; aucun son chargé pour cet appel | à rechercher |
| `omen_cast` | ennemis | Annonce frappe Présage | clé appelée mais absente du registre ; aucun son chargé pour cet appel | à rechercher |
| `omen_impact` | ennemis | Impact frappe Présage | clé appelée mais absente du registre ; aucun son chargé pour cet appel | à rechercher |
| `enemy_ranged_shot` | ennemis | Projectile ennemi | branchement audio dédié non relevé | à rechercher |
| `screamer_call` | ennemis | Cri qui invoque des renforts | branchement audio dédié non relevé | à rechercher |
| `burrow_transition` | ennemis | Enfouissement / surgissement | branchement audio dédié non relevé | à rechercher |
| `enemy_charge` | ennemis | Charge brute / colosse | branchement audio dédié non relevé | à rechercher |
| `colossus_slam` | ennemis | Frappe de sol colosse | branchement audio dédié non relevé | à rechercher |
| `enemy_explode` | ennemis | Explosion affixe Instable | branchement audio dédié non relevé | à rechercher |
| `boss_tentacle` | ennemis | Tentacule : annonce et frappe | branchement audio dédié non relevé | à rechercher |
| `boss_enrage` | ennemis | Passage phase enragée | branchement audio dédié non relevé | à rechercher |
| `boss_death` | ennemis | Mort Indicible | branchement audio dédié non relevé | à rechercher |
| `ambience_collapsed_quarry` | ambiances | Ambiance Carrière Effondrée | Exploration force actuellement forêt ; sélection selon biome à intégrer. | à rechercher |
| `ambience_forest_reclaimed` | ambiances | Ambiance Forêt Reconquise | Exploration force actuellement forêt ; sélection selon biome à intégrer. | à rechercher |
| `ambience_swamp` | ambiances | Ambiance Marécages | Exploration force actuellement forêt ; sélection selon biome à intégrer. | à rechercher |
| `ambience_urban_ruins` | ambiances | Ambiance Ruines Urbaines | Exploration force actuellement forêt ; sélection selon biome à intégrer. | à rechercher |
| `ambience_wild_fields` | ambiances | Ambiance Champs Sauvages | Exploration force actuellement forêt ; sélection selon biome à intégrer. | à rechercher |
| `ambient_details` | ambiances | Oiseaux, eau, vent ponctuels | Oiseaux joués en Exploration ; autres détails non attestés sur biomes actuels. | à rechercher |
| `crisis_thunder` | ambiances | Tonnerre en Résurgence / LateGame | appel existant, audibilité non recettée | à rechercher |
| `weather_legacy_mapping` | ambiances | Anciens mappings météo | IDs météo historiques absents des 5 micro-événements actuels. | hors périmètre actuel |
| `music_exploration` | musique | Exploration | Sélection existante ; transitions à recetter. | à rechercher |
| `music_combat` | musique | Combat | Sélection existante ; transitions à recetter. | à rechercher |
| `music_warning` | musique | Annonce Résurgence | Sélection existante ; transitions à recetter. | à rechercher |
| `music_crisis` | musique | Résurgence | Sélection existante ; transitions à recetter. | à rechercher |
| `music_aftermath` | musique | Accalmie après Résurgence | Fichier enregistré sans appel relevé. | à rechercher |
| `music_late_game` | musique | LateGame / Indicible | Sélection existante ; transitions à recetter. | à rechercher |
| `music_endgame` | musique | Endgame | Pas de case Endgame audio. | à rechercher |
| `music_hub` | musique | Hub | Sélection existante ; transitions à recetter. | à rechercher |
| `music_death` | musique | Mort / résultat | Sélection existante ; transitions à recetter. | à rechercher |
| `lore_detail` | monde | Rencontre / interaction élément de lore | branchement audio dédié non relevé | à rechercher |
| `future_frozen_relic` | futur | Vestige figé / rareté | branchement audio dédié non relevé | hors périmètre actuel |
| `future_triptych` | futur | Triptyque / perte des choix | branchement audio dédié non relevé | hors périmètre actuel |
| `future_oblivion_pact` | futur | Pacte / sacrifice | branchement audio dédié non relevé | hors périmètre actuel |
| `future_threatened_loot` | futur | Butin menacé | branchement audio dédié non relevé | hors périmètre actuel |
| `future_collapse` | futur | Effondrement | branchement audio dédié non relevé | hors périmètre actuel |
| `future_run_echo` | futur | Écho de dernière run | branchement audio dédié non relevé | hors périmètre actuel |
| `future_self_forgetting` | futur | Oubli de soi | branchement audio dédié non relevé | hors périmètre actuel |
| `future_remembering_place` | futur | Lieu qui se souvient | branchement audio dédié non relevé | hors périmètre actuel |
| `future_double` | futur | Double | branchement audio dédié non relevé | hors périmètre actuel |
| `future_mirage` | futur | Mirage | branchement audio dédié non relevé | hors périmètre actuel |
| `legacy_harvest` | héritage V1 | Ancien système harvest | Fichiers historiques enregistrés, système supprimé. | héritage V1 |
| `legacy_craft` | héritage V1 | Ancien système craft | Fichiers historiques enregistrés, système supprimé. | héritage V1 |
| `legacy_construction` | héritage V1 | Ancien système construction | Fichiers historiques enregistrés, système supprimé. | héritage V1 |
| `legacy_hearth` | héritage V1 | Ancien système hearth | Fichiers historiques enregistrés, système supprimé. | héritage V1 |
| `legacy_day_night` | héritage V1 | Ancien système day_night | Fichiers historiques enregistrés, système supprimé. | héritage V1 |

## Correspondances complètes

Chaque ligne référence un effet partagé ou propre proposé. La liste ne demande pas de déclencher tous ces sons simultanément. Les références précises, mécaniques et réserves figurent dans le JSON.

### weapons — 24

| Entrée | Effets associés |
|---|---|
| `chipped_blade` | `blade_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `heavy_hammer` | `heavy_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `makeshift_bow` | `bow_release`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `sling` | `throw_release`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `sharpened_pipe` | `blade_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `crossbow` | `crossbow_release`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `cleaver` | `heavy_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `whip` | `whip_snap`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `throwing_axes` | `throw_release`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `nail_mace` | `heavy_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `teachers_bell` | `bell_pulse`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `surgeons_scalpel` | `blade_swing`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop`, `player_heal` |
| `lighthouse_shard` | `light_shot`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `music_box` | `music_box_note`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `chain_of_names` | `chain_jump`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `compass_needle` | `needle_shot`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `photographers_flash` | `camera_flash`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `essence_staff` | `essence_shot`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `void_edge` | `void_slash`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `memory_lantern` | `lantern_fire`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `echo_gauntlets` | `echo_strike`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `childs_drawing` | `drawing_launch`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `last_broadcast` | `radio_wave`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |
| `clock_hand` | `clock_strike`, `enemy_hit`, `critical_hit`, `weapon_equip`, `weapon_upgrade`, `weapon_drop` |

### enemies — 16

| Entrée | Effets associés |
|---|---|
| `charognard` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `pounce_windup`, `pounce_leap` |
| `colosse_forest` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `colossus_presence`, `enemy_charge`, `colossus_slam`, `danger_warning` |
| `colosse_swamp` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `colossus_presence`, `enemy_charge`, `colossus_slam`, `danger_warning` |
| `colosse_urban` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `colossus_presence`, `enemy_charge`, `colossus_slam`, `danger_warning` |
| `fading_spitter` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `enemy_ranged_shot` |
| `hurleur` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `screamer_call`, `enemy_ranged_shot` |
| `indicible` | `enemy_hit`, `boss_tentacle`, `boss_enrage`, `boss_death`, `dissolution`, `danger_warning`, `player_hit` |
| `presage` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `omen_cast`, `omen_impact` |
| `rampant` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `burrow_transition` |
| `rodeur` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit` |
| `shade` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit` |
| `shadow_crawler` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit` |
| `tisseuse` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `enemy_ranged_shot` |
| `treant_corrompu` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit` |
| `void_brute` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `enemy_charge` |
| `wailing_sentinel` | `enemy_hit`, `critical_hit`, `dissolution`, `player_hit`, `enemy_ranged_shot` |

### enemy_variants — 3

| Entrée | Effets associés |
|---|---|
| `elite` | `enemy_hit`, `critical_hit`, `dissolution` |
| `champion` | `enemy_hit`, `critical_hit`, `dissolution` |
| `aberration` | `enemy_hit`, `critical_hit`, `dissolution` |

### enemy_affixes — 5

| Entrée | Effets associés |
|---|---|
| `enraged` | `enemy_hit`, `critical_hit`, `dissolution` |
| `armored` | `enemy_hit`, `critical_hit`, `dissolution` |
| `regenerant` | `enemy_hit`, `critical_hit`, `dissolution` |
| `explosive` | `enemy_hit`, `critical_hit`, `dissolution`, `enemy_explode` |
| `swift` | `enemy_hit`, `critical_hit`, `dissolution` |

### biomes — 5

| Entrée | Effets associés |
|---|---|
| `collapsed_quarry` | `ambience_collapsed_quarry`, `step_grass`, `step_concrete`, `step_water`, `step_forest`, `step_gravel` |
| `forest_reclaimed` | `ambience_forest_reclaimed`, `step_grass`, `step_concrete`, `step_water`, `step_forest` |
| `swamp` | `ambience_swamp`, `step_grass`, `step_concrete`, `step_water`, `step_forest` |
| `urban_ruins` | `ambience_urban_ruins`, `step_grass`, `step_concrete`, `step_water`, `step_forest` |
| `wild_fields` | `ambience_wild_fields`, `step_grass`, `step_concrete`, `step_water`, `step_forest` |

### events — 5

| Entrée | Effets associés |
|---|---|
| `hunt` | `danger_warning`, `event_success`, `event_fail`, `enemy_hit`, `dissolution`, `chest_open` |
| `stampede` | `danger_warning`, `event_success`, `event_fail`, `stampede_pass`, `xp_pickup` |
| `fallen_relic` | `danger_warning`, `event_success`, `event_fail`, `relic_fall`, `artifact_found` |
| `vigil` | `danger_warning`, `event_success`, `event_fail`, `vigil_hold`, `souvenir_found`, `player_heal` |
| `shard_rain` | `danger_warning`, `event_success`, `event_fail`, `shard_strike`, `xp_pickup`, `player_hit` |

### screens — 23

| Entrée | Effets associés |
|---|---|
| `ChestLootScreen` | `chest_open`, `chest_reveal`, `perk_select`, `ui_hover`, `ui_click` |
| `DebugActionPanel` | `ui_click` |
| `DebugOverlay` | `ui_click` |
| `DevelopmentBadge` | `ui_click` |
| `GameLoadingOverlay` | `run_departure` |
| `GameOverScreen` | `music_death`, `ui_confirm`, `ui_hover`, `ui_click` |
| `HUD` | `xp_pickup`, `level_up`, `low_health`, `erasure_near` |
| `HubBackdrop` | `music_hub` |
| `HubCamp` | `ui_hover`, `ui_click` |
| `HubChroniquesPanel` | `ui_hover`, `ui_click` |
| `HubMenuButton` | `ui_hover`, `ui_click` |
| `HubScreen` | `music_hub`, `ui_hover`, `ui_click`, `ui_confirm`, `meta_unlock`, `memorial_activate`, `run_departure` |
| `JournalScreen` | `journal_open`, `journal_close`, `ui_hover`, `ui_click` |
| `LevelUpScreen` | `level_up`, `level_up_wait`, `level_up_exit`, `perk_select`, `perk_refuse`, `rare_fragment`, `ui_hover`, `ui_click` |
| `Minimap` | `world_reveal` |
| `PauseMenu` | `ui_confirm`, `ui_hover`, `ui_click` |
| `PerkIconResolver` | `perk_select` |
| `PlayerHealthGauge` | `player_hit`, `low_health`, `player_heal` |
| `RunEventHud` | `danger_warning`, `event_success`, `event_fail` |
| `SettingsScreen` | `ui_hover`, `ui_click` |
| `SouvenirPopup` | `souvenir_found` |
| `UITheme` | `ui_hover`, `ui_click` |
| `VoidTransition` | `run_departure` |

### characters — 3

| Entrée | Effets associés |
|---|---|
| `traqueur` | `ui_confirm`, `meta_unlock`, `player_hit`, `player_heal`, `player_death`, `dash_start`, `dash_end` |
| `vagabond` | `ui_confirm`, `meta_unlock`, `player_hit`, `player_heal`, `player_death`, `dash_start`, `dash_end` |
| `forgeuse` | `ui_confirm`, `meta_unlock`, `player_hit`, `player_heal`, `player_death`, `dash_start`, `dash_end` |

### chests — 4

| Entrée | Effets associés |
|---|---|
| `chest_common` | `chest_open`, `chest_reveal`, `perk_select` |
| `chest_rare` | `chest_open`, `chest_reveal`, `perk_select` |
| `chest_epic` | `chest_open`, `chest_reveal`, `perk_select` |
| `chest_lore` | `chest_open`, `chest_reveal`, `perk_select` |

### pois — 7

| Entrée | Effets associés |
|---|---|
| `searchable_building` | `poi_search`, `poi_activate`, `artifact_found` |
| `resource_cache` | `poi_search`, `poi_activate`, `artifact_found` |
| `guarded_chest` | `poi_search`, `poi_activate`, `artifact_found` |
| `lore_ruin` | `poi_search`, `poi_activate`, `artifact_found` |
| `merchant_npc` | `poi_search`, `poi_activate`, `artifact_found` |
| `anomaly` | `poi_search`, `poi_activate`, `artifact_found` |
| `sanctuary` | `poi_search`, `poi_activate`, `artifact_found` |

### perks — 64

| Entrée | Effets associés |
|---|---|
| `damage_up` | `perk_select`, `synergy_activate` |
| `speed_up` | `perk_select`, `synergy_activate` |
| `hp_up` | `perk_select`, `synergy_activate` |
| `attack_speed_up` | `perk_select`, `synergy_activate` |
| `extra_projectile` | `perk_select`, `synergy_activate` |
| `aoe_up` | `perk_select`, `synergy_activate` |
| `armor_up` | `perk_select`, `synergy_activate` |
| `regen_up` | `perk_select`, `synergy_activate` |
| `range_up` | `perk_select`, `synergy_activate` |
| `xp_magnet` | `perk_select`, `synergy_activate` |
| `lucky` | `perk_select`, `synergy_activate` |
| `vampirism` | `perk_select`, `synergy_activate` |
| `berserker` | `perk_select`, `synergy_activate` |
| `piercing_shot` | `perk_select`, `synergy_activate` |
| `ricochet` | `perk_select`, `synergy_activate` |
| `crit_chance` | `perk_select`, `synergy_activate` |
| `crit_damage` | `perk_select`, `synergy_activate` |
| `ignite` | `perk_select`, `synergy_activate` |
| `thorns` | `perk_select`, `synergy_activate` |
| `execution` | `perk_select`, `synergy_activate` |
| `kill_speed` | `perk_select`, `synergy_activate` |
| `architect` | `perk_select`, `synergy_activate` |
| `salvager` | `perk_select`, `synergy_activate` |
| `torch_bearer` | `perk_select`, `synergy_activate` |
| `quick_fix` | `perk_select`, `synergy_activate` |
| `harvest_bounty` | `perk_select`, `synergy_activate` |
| `night_vision` | `perk_select`, `synergy_activate` |
| `channeling` | `perk_select`, `synergy_activate` |
| `siphon` | `perk_select`, `synergy_activate` |
| `instability` | `perk_select`, `synergy_activate` |
| `essence_regen` | `perk_select`, `synergy_activate` |
| `second_wind` | `perk_select`, `synergy_activate` |
| `time_master` | `perk_select`, `synergy_activate` |
| `awakened_sight` | `perk_select`, `synergy_activate` |
| `glass_cannon` | `perk_select`, `synergy_activate` |
| `last_stand` | `perk_select`, `synergy_activate` |
| `memory_anchor` | `perk_select`, `synergy_activate` |
| `appel_du_vide` | `perk_select`, `synergy_activate` |
| `synergy_blood_rage` | `perk_select`, `synergy_activate` |
| `synergy_crit_master` | `perk_select`, `synergy_activate` |
| `synergy_essence_storm` | `perk_select`, `synergy_activate` |
| `synergy_fortress` | `perk_select`, `synergy_activate` |
| `synergy_glass_berserker` | `perk_select`, `synergy_activate` |
| `synergy_torchfire` | `perk_select`, `synergy_activate` |
| `synergy_ricochet_crit` | `perk_select`, `synergy_activate` |
| `synergy_executioner` | `perk_select`, `synergy_activate` |
| `vagabond_harvest` | `perk_select`, `synergy_activate` |
| `vagabond_adaptability` | `perk_select`, `synergy_activate` |
| `vagabond_survivalist` | `perk_select`, `synergy_activate` |
| `vagabond_jack_of_all` | `perk_select`, `synergy_activate` |
| `vagabond_nomad` | `perk_select`, `synergy_activate` |
| `vagabond_scrounger` | `perk_select`, `synergy_activate` |
| `forgeuse_fortify` | `perk_select`, `synergy_activate` |
| `forgeuse_quick_craft` | `perk_select`, `synergy_activate` |
| `forgeuse_reinforce` | `perk_select`, `synergy_activate` |
| `forgeuse_overcharge` | `perk_select`, `synergy_activate` |
| `forgeuse_recycler` | `perk_select`, `synergy_activate` |
| `forgeuse_last_wall` | `perk_select`, `synergy_activate` |
| `traqueur_precision` | `perk_select`, `synergy_activate` |
| `traqueur_piercing` | `perk_select`, `synergy_activate` |
| `traqueur_swiftness` | `perk_select`, `synergy_activate` |
| `traqueur_ambush` | `perk_select`, `synergy_activate` |
| `traqueur_evasion` | `perk_select`, `synergy_activate` |
| `traqueur_marked` | `perk_select`, `synergy_activate` |

### passives — 13

| Entrée | Effets associés |
|---|---|
| `flamme_interieure` | `perk_select`, `passive_gain` |
| `memoire_vive` | `perk_select`, `passive_gain` |
| `ancrage` | `perk_select`, `passive_gain` |
| `instinct` | `perk_select`, `passive_gain` |
| `resonance` | `perk_select`, `passive_gain` |
| `siphon_essence` | `perk_select`, `passive_gain` |
| `peau_dure` | `perk_select`, `passive_gain` |
| `oeil_critique` | `perk_select`, `passive_gain` |
| `regeneration` | `perk_select`, `passive_gain` |
| `portee_etendue` | `perk_select`, `passive_gain` |
| `souffle_du_neant` | `perk_select`, `passive_gain` |
| `fragment_deternite` | `perk_select`, `passive_gain` |
| `reflet_brise` | `perk_select`, `passive_gain` |

### souvenirs — 19

| Entrée | Effets associés |
|---|---|
| `billet_de_train` | `souvenir_found` |
| `liste_de_courses` | `souvenir_found` |
| `photo_decoloree` | `souvenir_found` |
| `journal_voisine` | `souvenir_found` |
| `rapport_cartographie` | `souvenir_found` |
| `graffiti_mur` | `souvenir_found` |
| `note_ponts` | `souvenir_found` |
| `radio_statique` | `souvenir_found` |
| `journal_dernier` | `souvenir_found` |
| `notes_terrain` | `souvenir_found` |
| `dessin_enfant` | `souvenir_found` |
| `carnet_recherche_foyer` | `souvenir_found` |
| `temoignage_irene` | `souvenir_found` |
| `photo_laboratoire` | `souvenir_found` |
| `inscription_gravee` | `souvenir_found` |
| `memory_flame` | `souvenir_found` |
| `void_knowledge` | `souvenir_found` |
| `lantern_memory` | `souvenir_found` |
| `echo_fragment` | `souvenir_found` |

### constellations — 6

| Entrée | Effets associés |
|---|---|
| `avant` | `memorial_activate`, `meta_unlock` |
| `signes` | `memorial_activate`, `meta_unlock` |
| `effacement` | `memorial_activate`, `meta_unlock` |
| `creatures` | `memorial_activate`, `meta_unlock` |
| `foyer` | `memorial_activate`, `meta_unlock` |
| `joueur` | `memorial_activate`, `meta_unlock` |

### quests — 12

| Entrée | Effets associés |
|---|---|
| `remember_the_flame` | `quest_complete`, `meta_unlock` |
| `cut_through_the_void` | `quest_complete`, `meta_unlock` |
| `count_the_crises` | `quest_complete`, `meta_unlock` |
| `name_the_unspeakable` | `quest_complete`, `meta_unlock` |
| `after_the_climax` | `quest_complete`, `meta_unlock` |
| `run_kill_swarm` | `quest_complete`, `meta_unlock` |
| `run_survive_crises` | `quest_complete`, `meta_unlock` |
| `run_search_the_map` | `quest_complete`, `meta_unlock` |
| `run_open_cache` | `quest_complete`, `meta_unlock` |
| `run_reach_level` | `quest_complete`, `meta_unlock` |
| `run_collect_essence` | `quest_complete`, `meta_unlock` |
| `run_survive_time` | `quest_complete`, `meta_unlock` |

### cursed_items — 3

| Entrée | Effets associés |
|---|---|
| `curse_oblivion` | `curse_accept` |
| `curse_swarm` | `curse_accept` |
| `curse_fury` | `curse_accept` |

### fusions — 5

| Entrée | Effets associés |
|---|---|
| `vestige_lame_souvenir` | `fusion_ready`, `fusion_complete`, `blade_swing`, `enemy_hit`, `critical_hit` |
| `vestige_arc_chasseur` | `fusion_ready`, `fusion_complete`, `bow_release`, `enemy_hit`, `critical_hit` |
| `vestige_fouet_noms` | `fusion_ready`, `fusion_complete`, `whip_snap`, `enemy_hit`, `critical_hit` |
| `vestige_berceuse` | `fusion_ready`, `fusion_complete`, `music_box_note`, `enemy_hit`, `critical_hit` |
| `vestige_trait_oubli` | `fusion_ready`, `fusion_complete`, `crossbow_release`, `enemy_hit`, `critical_hit` |

### lore — 11

| Entrée | Effets associés |
|---|---|
| `BioluminescentMushrooms` | `lore_detail`, `artifact_found` |
| `CrazyClock` | `lore_detail`, `artifact_found` |
| `DoorWithoutWall` | `lore_detail`, `artifact_found` |
| `EmptyShelves` | `lore_detail`, `artifact_found` |
| `GhostChime` | `lore_detail`, `artifact_found` |
| `GhostSwing` | `lore_detail`, `artifact_found` |
| `InterruptedFootsteps` | `lore_detail`, `artifact_found` |
| `RootLetters` | `lore_detail`, `artifact_found` |
| `SurvivingGraffiti` | `lore_detail`, `artifact_found` |
| `SwallowedSign` | `lore_detail`, `artifact_found` |
| `WaterMirror` | `lore_detail`, `artifact_found` |

### systems — 7

| Entrée | Effets associés |
|---|---|
| `altars` | `altar_upgrade`, `altar_reforge`, `altar_heal`, `interaction_unavailable` |
| `erasure` | `erasure_near`, `world_erase`, `player_hit` |
| `crises` | `danger_warning`, `crisis_start`, `crisis_end`, `music_warning`, `music_crisis`, `music_aftermath`, `crisis_thunder` |
| `endgame` | `danger_warning`, `music_late_game`, `music_endgame`, `boss_death` |
| `essence` | `essence_gain` |
| `run_music` | `music_exploration`, `music_combat`, `music_hub`, `music_death`, `ambient_details` |
| `weapon_pickup` | `weapon_equip`, `weapon_drop`, `interaction_unavailable` |

## Preuves et limites

- Audit statique : présence dans les données/code ne prouve ni accessibilité en run, ni qualité sonore.
- Besoins proposés = effets utiles à rechercher ou arbitrer ; aucun silence décidé pour un manque.
- Durées et variantes sont des briefs proposés, pas des propriétés mesurées.
- La Stratégie V2 §21 exclut explicitement les idle/alertes répétitifs ennemis ; leurs fichiers ne créent pas de nouveaux besoins.
- Candidats, choix et intégration sont trois étapes distinctes. Les anciens fichiers ne sont pas des candidats validés.
- Plans 13/14 et héritage V1 exclus du dénominateur actuel.
- L’inventaire 15-audio-inventaire.md conserve les 79 fichiers historiques et tous les sites d’appels ; ce catalogue compte les besoins, pas les assets.

- Armes : 24 définitions, 19 familles proposées ; impacts partagés. Les effets de statut ne créent pas un son à chaque tick.
- Ennemis : 16 définitions, 3 variantes et 5 affixes. Les 2 capacités data-driven ont 4 clés appelées mais non enregistrées ; `rare_fragment` constitue la cinquième clé manquante.
- Indicible utilise une classe dédiée : ne pas lui attribuer à tort l’impact générique de `Enemy.TakeDamage`.
- Biomes : 5 définitions. L’ambiance forêt est forcée aujourd’hui ; béton/eau ont leurs pas, herbe et forêt partagent un fichier. Bois/gravier restent à arbitrer comme surfaces.
- Interface : écrans, widgets et outils techniques sont distingués dans le JSON ; une association de contexte ne prouve pas un appel depuis le widget.
- Fusions : les cinq données sont suivies, mais `cable_whip` n’est pas une arme actuelle et `kill_restore_day` est historique ; cela ne valide pas leur accessibilité.
- Les sons ennemis historiques non utilisés ne sont pas ajoutés comme idle/alertes ; la direction V2 les exclut. Aucun nouveau silence artistique n’est décidé.

## Format des choix

`raphael_choice` vaut `null` tant que le retour n’est pas consigné. Sinon : `{"decision":"candidate","candidate_id":"enemy_hit_a","notes":"retour de Raphaël"}`. Les autres décisions sont `pending` (attente), `none` (chercher autre chose) et `silence` ; leur `candidate_id` vaut `null`. `notes` est facultatif. Le format reprend celui des décisions exportées par la page d’écoute, à reporter explicitement dans le catalogue. Aucun import des choix navigateur n’est automatique.

Seuls les candidats effectivement sélectionnés sont comptés comme choisis. Un refus ou un silence ne compte pas comme son retenu. Si un candidat choisi disparaît lors de la synchronisation, la génération échoue explicitement avant toute écriture ; rétablir ce candidat ou faire réviser le choix par Raphaël.

## Régénération et contrôle

`python3 tools/audio/build_catalogue.py` régénère ces deux fichiers en préservant choix et recette. `python3 tools/audio/build_catalogue.py --check` compare les sorties et contrôle les identifiants/liaisons. Les candidats de tous les lots sont agrégés depuis `lot-*/candidates.json`, en conservant notamment ceux du lot A, avec contrôle des médias et preuves de licence présents (sans valider leur qualité artistique). Les catégories de données sont parcourues intégralement ; une nouvelle arme non classée ou une nouvelle interface non mappée fait échouer la génération.

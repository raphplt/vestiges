# Inventaire audio VESTIGES — 25 septembre 2026

Relevé statique du 25 septembre 2026, avant intégration de nouveaux sons. Aucun fichier audio écouté. Les numéros de ligne du code correspondent à cet état du dépôt.

Voir le [plan de production](15-audio.md) et le [guide audio actuel](../AUDIO-GUIDE.md).

## Comptages vérifiés

- 79 fichiers réels : 8 OGG musique, 71 WAV effets/ambiances.
- 62 clés AudioManager, toutes pointent vers un fichier existant.
- 17 fichiers non enregistrés : 15 créatures, 2 projectiles.
- 19 clés enregistrées sans référence hors déclaration/limitation de répétition (ne signifie pas vérification en exécution).
- 5 clés utilisées mais absentes du registre : 4 créatures (fichiers présents) et sfx_rare_fragment (fichier absent).

## Registre exhaustif des fichiers

| Fichier | Clé enregistrée | Références littérales hors registre et throttle |
|---|---|---|
| `assets/audio/musique/mus_aube.ogg` | `mus_aube` | Aucune |
| `assets/audio/musique/mus_crepuscule.ogg` | `mus_crepuscule` | scripts/Infrastructure/AudioManager.cs:654 |
| `assets/audio/musique/mus_hub.ogg` | `mus_hub` | scripts/Infrastructure/AudioManager.cs:445 |
| `assets/audio/musique/mus_jour_combat.ogg` | `mus_jour_combat` | scripts/Infrastructure/AudioManager.cs:573 |
| `assets/audio/musique/mus_jour_exploration.ogg` | `mus_jour_exploration` | scripts/Infrastructure/AudioManager.cs:574 |
| `assets/audio/musique/mus_mort.ogg` | `mus_mort` | scripts/Infrastructure/AudioManager.cs:453<br>scripts/Infrastructure/AudioManager.cs:642 |
| `assets/audio/musique/mus_nuit_chaos.ogg` | `mus_nuit_chaos` | scripts/Infrastructure/AudioManager.cs:638 |
| `assets/audio/musique/mus_nuit_vagues.ogg` | `mus_nuit_vagues` | scripts/Infrastructure/AudioManager.cs:634<br>scripts/Infrastructure/AudioManager.cs:661 |
| `assets/audio/sfx/ambiance/sfx_ambiance_bulle.wav` | `sfx_ambiance_bulle` | Aucune |
| `assets/audio/sfx/ambiance/sfx_ambiance_foret.wav` | `sfx_ambiance_foret` | scripts/Infrastructure/AudioManager.cs:630 |
| `assets/audio/sfx/ambiance/sfx_ambiance_marecages.wav` | `sfx_ambiance_marecages` | Aucune |
| `assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_1.wav` | `sfx_ambiance_oiseaux_1` | scripts/Infrastructure/AudioManager.cs:561 |
| `assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_2.wav` | `sfx_ambiance_oiseaux_2` | scripts/Infrastructure/AudioManager.cs:561 |
| `assets/audio/sfx/ambiance/sfx_ambiance_ruines.wav` | `sfx_ambiance_ruines` | Aucune |
| `assets/audio/sfx/ambiance/sfx_bord_effacement_proche.wav` | `sfx_bord_effacement_proche` | scripts/Infrastructure/AudioManager.cs:843 |
| `assets/audio/sfx/ambiance/sfx_brouillard.wav` | `sfx_brouillard` | scripts/Infrastructure/AudioManager.cs:790<br>scripts/Infrastructure/AudioManager.cs:805 |
| `assets/audio/sfx/ambiance/sfx_colosse_lointain.wav` | `sfx_colosse_lointain` | scripts/Infrastructure/AudioManager.cs:786<br>scripts/Infrastructure/AudioManager.cs:801 |
| `assets/audio/sfx/ambiance/sfx_foret_rafales.wav` | `sfx_foret_rafales` | scripts/Infrastructure/AudioManager.cs:789<br>scripts/Infrastructure/AudioManager.cs:808 |
| `assets/audio/sfx/ambiance/sfx_orage_proche.wav` | `sfx_orage_proche` | scripts/Infrastructure/AudioManager.cs:787<br>scripts/Infrastructure/AudioManager.cs:806 |
| `assets/audio/sfx/ambiance/sfx_pluie_legere.wav` | `sfx_pluie_legere` | scripts/Infrastructure/AudioManager.cs:788<br>scripts/Infrastructure/AudioManager.cs:807 |
| `assets/audio/sfx/ambiance/sfx_tonnerre_lointain.wav` | `sfx_tonnerre_lointain` | scripts/Infrastructure/AudioManager.cs:791<br>scripts/Infrastructure/AudioManager.cs:809 |
| `assets/audio/sfx/combat/sfx_degat_critique_recu.wav` | `sfx_degat_critique_recu` | scripts/Infrastructure/AudioManager.cs:701 |
| `assets/audio/sfx/combat/sfx_hit_critique.wav` | `sfx_hit_critique` | scripts/Combat/Enemy.cs:1001 |
| `assets/audio/sfx/combat/sfx_hit_ennemi.wav` | `sfx_hit_ennemi` | scripts/Combat/Enemy.cs:1001 |
| `assets/audio/sfx/combat/sfx_hit_joueur.wav` | `sfx_hit_joueur` | scripts/Infrastructure/AudioManager.cs:699 |
| `assets/audio/sfx/combat/sfx_projectile_impact.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/combat/sfx_projectile_vol.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_brute_charge.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_brute_pas.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_charognard_idle.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_charognard_meute.wav` | Non enregistrée | data/enemies/charognard.json:28 |
| `assets/audio/sfx/creatures/sfx_hurleur_cri.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_indicible_presence.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_ombre_attaque.wav` | Non enregistrée | data/enemies/charognard.json:29 |
| `assets/audio/sfx/creatures/sfx_ombre_idle.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_rampant_deplacement.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_rampant_surgissement.wav` | Non enregistrée | data/enemies/presage.json:26 |
| `assets/audio/sfx/creatures/sfx_rodeur_attaque.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_rodeur_idle.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_sentinelle_activation.wav` | Non enregistrée | data/enemies/presage.json:25 |
| `assets/audio/sfx/creatures/sfx_sentinelle_tir.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/creatures/sfx_tisseuse_idle.wav` | Non enregistrée | Aucune |
| `assets/audio/sfx/foyer/sfx_foyer_aura.wav` | `sfx_foyer_aura` | Aucune |
| `assets/audio/sfx/foyer/sfx_foyer_crepitement.wav` | `sfx_foyer_crepitement` | Aucune |
| `assets/audio/sfx/foyer/sfx_foyer_upgrade.wav` | `sfx_foyer_upgrade` | Aucune |
| `assets/audio/sfx/gameplay/chest_opening.wav` | `sfx_chest_opening` | scripts/UI/ChestLootScreen.cs:241 |
| `assets/audio/sfx/gameplay/level_up.wav` | `sfx_level_up` | scripts/UI/LevelUpScreen.cs:890 |
| `assets/audio/sfx/gameplay/level_up_after.wav` | `sfx_level_up_after` | scripts/UI/LevelUpScreen.cs:912 |
| `assets/audio/sfx/gameplay/level_up_loop.wav` | `sfx_level_up_loop` | scripts/UI/LevelUpScreen.cs:901 |
| `assets/audio/sfx/gameplay/sfx_artefact_trouve.wav` | `sfx_artefact_trouve` | scripts/Events/RunEvents/FallenRelicEvent.cs:99<br>scripts/Infrastructure/AudioManager.cs:728<br>scripts/Infrastructure/AudioManager.cs:734 |
| `assets/audio/sfx/gameplay/sfx_craft_impossible.wav` | `sfx_craft_impossible` | Aucune |
| `assets/audio/sfx/gameplay/sfx_craft_termine.wav` | `sfx_craft_termine` | Aucune |
| `assets/audio/sfx/gameplay/sfx_danger_building.wav` | `sfx_danger_building` | scripts/Events/RunEventDirector.cs:189<br>scripts/Infrastructure/AudioManager.cs:655<br>scripts/Infrastructure/AudioManager.cs:678<br>scripts/Infrastructure/AudioManager.cs:741 |
| `assets/audio/sfx/gameplay/sfx_malediction_acceptee.wav` | `sfx_malediction_acceptee` | scripts/Progression/CursedItemManager.cs:70 |
| `assets/audio/sfx/gameplay/sfx_monde_aube.wav` | `sfx_monde_aube` | Aucune |
| `assets/audio/sfx/gameplay/sfx_monde_bord_map.wav` | `sfx_monde_bord_map` | Aucune |
| `assets/audio/sfx/gameplay/sfx_monde_crepuscule.wav` | `sfx_monde_crepuscule` | Aucune |
| `assets/audio/sfx/gameplay/sfx_monde_dissolution.wav` | `sfx_monde_dissolution` | scripts/Events/RunEvents/FallenRelicEvent.cs:75<br>scripts/Infrastructure/AudioManager.cs:688 |
| `assets/audio/sfx/gameplay/sfx_monde_tuile_apparait.wav` | `sfx_monde_tuile_apparait` | Aucune |
| `assets/audio/sfx/gameplay/sfx_perk_choix.wav` | `sfx_perk_choix` | scripts/Infrastructure/AudioManager.cs:717<br>scripts/Infrastructure/AudioManager.cs:722<br>scripts/UI/ChestLootScreen.cs:420 |
| `assets/audio/sfx/gameplay/sfx_perk_refuse.wav` | `sfx_perk_refuse` | scripts/UI/LevelUpScreen.cs:742 |
| `assets/audio/sfx/gameplay/sfx_recolte_hache.wav` | `sfx_recolte_hache` | Aucune |
| `assets/audio/sfx/gameplay/sfx_recolte_obtenu.wav` | `sfx_recolte_obtenu` | Aucune |
| `assets/audio/sfx/gameplay/sfx_recolte_pioche.wav` | `sfx_recolte_pioche` | Aucune |
| `assets/audio/sfx/gameplay/sfx_sante_basse.wav` | `sfx_sante_basse` | scripts/Infrastructure/AudioManager.cs:830 |
| `assets/audio/sfx/gameplay/sfx_souvenir_trouve.wav` | `sfx_souvenir_trouve` | scripts/Events/RunEvents/VigilEvent.cs:77<br>scripts/Infrastructure/AudioManager.cs:708<br>scripts/UI/RunEventHud.cs:218 |
| `assets/audio/sfx/gameplay/sfx_structure_impossible.wav` | `sfx_structure_impossible` | Aucune |
| `assets/audio/sfx/gameplay/sfx_structure_pose.wav` | `sfx_structure_pose` | Aucune |
| `assets/audio/sfx/gameplay/xp_gain.wav` | `xp_gain` | scripts/Combat/XpOrb.cs:123 |
| `assets/audio/sfx/joueur/pas/sfx_pas_beton.wav` | `sfx_pas_beton` | scripts/Core/Player.cs:1544<br>data/movement/mobility.json:14 |
| `assets/audio/sfx/joueur/pas/sfx_pas_bois.wav` | `sfx_pas_bois` | Aucune |
| `assets/audio/sfx/joueur/pas/sfx_pas_eau.wav` | `sfx_pas_eau` | scripts/Core/Player.cs:1543 |
| `assets/audio/sfx/joueur/pas/sfx_pas_gravier.wav` | `sfx_pas_gravier` | data/movement/mobility.json:13 |
| `assets/audio/sfx/joueur/pas/sfx_pas_herbe.wav` | `sfx_pas_herbe` | scripts/Core/Player.cs:1545 |
| `assets/audio/sfx/ui/sfx_inventaire_fermer.wav` | `sfx_inventaire_fermer` | scripts/UI/JournalScreen.cs:55 |
| `assets/audio/sfx/ui/sfx_inventaire_ouvrir.wav` | `sfx_inventaire_ouvrir` | scripts/UI/JournalScreen.cs:47 |
| `assets/audio/sfx/ui/sfx_menu_clic.wav` | `sfx_menu_clic` | scripts/UI/UITheme.cs:175 |
| `assets/audio/sfx/ui/sfx_menu_confirmer.wav` | `sfx_menu_confirmer` | scripts/UI/GameOverScreen.cs:302<br>scripts/UI/GameOverScreen.cs:309<br>scripts/UI/HubScreen.cs:446<br>scripts/UI/HubScreen.cs:509<br>scripts/UI/HubScreen.cs:613<br>scripts/UI/HubScreen.cs:1240<br>scripts/UI/PauseMenu.cs:94<br>scripts/UI/PauseMenu.cs:99<br>scripts/UI/PauseMenu.cs:105<br>scripts/UI/PauseMenu.cs:114 |
| `assets/audio/sfx/ui/sfx_menu_survol.wav` | `sfx_menu_survol` | scripts/UI/UITheme.cs:168 |

## Sites d’appels exhaustifs et signatures

Les méthodes internes de registre et les signatures sont incluses. Les sélecteurs dynamiques sont détaillés ci-dessous.

```text
scripts/Combat/Abilities/OmenStrikeAbility.cs:118: AudioManager.Play(_castAudio, 0.08f, -6f);
scripts/Combat/Abilities/OmenStrikeAbility.cs:131: AudioManager.Play(_impactAudio, 0.08f, -4f);
scripts/Combat/Abilities/PounceAbility.cs:134: AudioManager.Play(_windupAudio, 0.1f, -6f);
scripts/Combat/Abilities/PounceAbility.cs:146: AudioManager.Play(_leapAudio, 0.1f, -4f);
scripts/Combat/Enemy.cs:1001: Infrastructure.AudioManager.Play(isCrit ? "sfx_hit_critique" : "sfx_hit_ennemi", 0.07f);
scripts/Combat/MobilityFeedback.cs:47: Infrastructure.AudioManager.Play(mobility.Config.StartAudio, 0.05f, -3f);
scripts/Combat/MobilityFeedback.cs:51: Infrastructure.AudioManager.Play(mobility.Config.EndAudio, 0.05f, -7f);
scripts/Combat/XpOrb.cs:123: AudioManager.Play("xp_gain", 0.03f, -1.5f);
scripts/Core/Player.cs:1547: Infrastructure.AudioManager.Play(key, 0.05f, -4f);
scripts/Events/RunEventDirector.cs:189: Infrastructure.AudioManager.Play("sfx_danger_building", 0f, -6f);
scripts/Events/RunEvents/FallenRelicEvent.cs:75: Infrastructure.AudioManager.Play("sfx_monde_dissolution", 0.05f, -2f);
scripts/Events/RunEvents/FallenRelicEvent.cs:99: Infrastructure.AudioManager.Play("sfx_artefact_trouve", 0f, -2f);
scripts/Events/RunEvents/VigilEvent.cs:77: Infrastructure.AudioManager.Play("sfx_souvenir_trouve", 0f, -2f);
scripts/Infrastructure/AudioManager.cs:337: Instance?.PlaySfx(key, pitchVariance, volumeDb);
scripts/Infrastructure/AudioManager.cs:340: public void PlaySfx(string key, float pitchVariance = 0.05f, float volumeDb = 0f)
scripts/Infrastructure/AudioManager.cs:445: PlayMusic("mus_hub", loop: true, fadeDuration: 2f);
scripts/Infrastructure/AudioManager.cs:453: PlaySfx("mus_mort", 0f);
scripts/Infrastructure/AudioManager.cs:460: private void PlayMusic(string key, float fadeDuration = 2.5f, bool loop = true)
scripts/Infrastructure/AudioManager.cs:505: private void PlayAmbiance(string key)
scripts/Infrastructure/AudioManager.cs:562: PlaySfx(birdKey, 0.05f, -4f);
scripts/Infrastructure/AudioManager.cs:575: PlayMusic(target, fadeDuration: 3f);
scripts/Infrastructure/AudioManager.cs:630: PlayAmbiance("sfx_ambiance_foret");
scripts/Infrastructure/AudioManager.cs:634: PlayMusic("mus_nuit_vagues", fadeDuration: 3f);
scripts/Infrastructure/AudioManager.cs:638: PlayMusic("mus_nuit_chaos", fadeDuration: 3f);
scripts/Infrastructure/AudioManager.cs:642: PlayMusic("mus_mort", fadeDuration: 2f, loop: false);
scripts/Infrastructure/AudioManager.cs:654: PlayMusic("mus_crepuscule", fadeDuration: 2f);
scripts/Infrastructure/AudioManager.cs:655: PlaySfx("sfx_danger_building", 0f, -5f);
scripts/Infrastructure/AudioManager.cs:661: PlayMusic("mus_nuit_vagues", fadeDuration: 2f);
scripts/Infrastructure/AudioManager.cs:678: PlaySfx("sfx_danger_building", 0f, -4f);
scripts/Infrastructure/AudioManager.cs:688: PlaySfx("sfx_monde_dissolution", 0.08f, -6f);
scripts/Infrastructure/AudioManager.cs:699: PlaySfx("sfx_hit_joueur");
scripts/Infrastructure/AudioManager.cs:701: PlaySfx("sfx_degat_critique_recu", 0f, -1f);
scripts/Infrastructure/AudioManager.cs:708: PlaySfx("sfx_souvenir_trouve", 0f);
scripts/Infrastructure/AudioManager.cs:717: PlaySfx("sfx_perk_choix", 0f);
scripts/Infrastructure/AudioManager.cs:722: PlaySfx("sfx_perk_choix", 0f);
scripts/Infrastructure/AudioManager.cs:728: PlaySfx("sfx_artefact_trouve", 0f, -3f);
scripts/Infrastructure/AudioManager.cs:734: PlaySfx("sfx_artefact_trouve", 0f, -3f);
scripts/Infrastructure/AudioManager.cs:741: PlaySfx("sfx_danger_building", 0f, -5f);
scripts/Infrastructure/AudioManager.cs:795: PlayLoopOnPlayer(_ambianceOverlayPlayer, ref _ambianceOverlayKey, targetKey, volumeDb);
scripts/Infrastructure/AudioManager.cs:830: PlayLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey, "sfx_sante_basse", -10f);
scripts/Infrastructure/AudioManager.cs:843: PlayLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey, "sfx_bord_effacement_proche", -11f);
scripts/Infrastructure/AudioManager.cs:882: private void PlayLoopOnPlayer(AudioStreamPlayer player, ref string currentKey, string key, float volumeDb)
scripts/Progression/CursedItemManager.cs:70: AudioManager.Play("sfx_malediction_acceptee", 0f, -2f);
scripts/UI/ChestLootScreen.cs:241: AudioManager.PlayUI("sfx_chest_opening", 0f);
scripts/UI/ChestLootScreen.cs:420: AudioManager.PlayUI("sfx_perk_choix", 0.05f);
scripts/UI/GameOverScreen.cs:302: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/GameOverScreen.cs:309: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/HubScreen.cs:446: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/HubScreen.cs:509: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/HubScreen.cs:613: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/HubScreen.cs:1240: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/JournalScreen.cs:47: AudioManager.PlayUI("sfx_inventaire_ouvrir");
scripts/UI/JournalScreen.cs:55: AudioManager.PlayUI("sfx_inventaire_fermer");
scripts/UI/LevelUpScreen.cs:449: Infrastructure.AudioManager.PlayUI("sfx_rare_fragment");
scripts/UI/LevelUpScreen.cs:742: Infrastructure.AudioManager.PlayUI("sfx_perk_refuse", 0f);
scripts/UI/LevelUpScreen.cs:890: Infrastructure.AudioManager.PlayUI("sfx_level_up");
scripts/UI/LevelUpScreen.cs:901: Infrastructure.AudioManager.PlayLoop("sfx_level_up_loop", -4f);
scripts/UI/LevelUpScreen.cs:911: Infrastructure.AudioManager.StopLoop();
scripts/UI/LevelUpScreen.cs:912: Infrastructure.AudioManager.PlayUI("sfx_level_up_after");
scripts/UI/PauseMenu.cs:94: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/PauseMenu.cs:99: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/PauseMenu.cs:105: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/PauseMenu.cs:114: AudioManager.PlayUI("sfx_menu_confirmer");
scripts/UI/RunEventHud.cs:218: Infrastructure.AudioManager.Play("sfx_souvenir_trouve", 0f, -4f);
scripts/UI/UITheme.cs:168: AudioManager.PlayUI("sfx_menu_survol");
scripts/UI/UITheme.cs:175: AudioManager.PlayUI("sfx_menu_clic");
```

## Résolution dynamique vérifiée

- Player.cs:1539-1547 : eau → sfx_pas_eau, béton → sfx_pas_beton, tous les autres terrains → sfx_pas_herbe. Bois jamais sélectionné. Gravier utilisé pour mobilité via JSON.
- MobilityFeedback.cs:47,51 et data/movement/mobility.json:13-14 : StartAudio=sfx_pas_gravier, EndAudio=sfx_pas_beton.
- PounceAbility.Configure, scripts/Combat/Abilities/PounceAbility.cs:59-60, puis StartWindup:134/StartLeap:146 : data/enemies/charognard.json:28-29 → sfx_charognard_meute / sfx_ombre_attaque. Clés absentes du registre.
- OmenStrikeAbility BeginCast:118 / Resolve:131 : data/enemies/presage.json:25-26 → sfx_sentinelle_activation / sfx_rampant_surgissement. Clés absentes du registre.
- AudioManager.cs:561-562 : oiseaux aléatoires 1/2 en phase Exploration.
- AudioManager.cs:570-575 : exploration/combat selon compte global d’ennemis >= 3 ; pas mesure de proximité du joueur.
- AudioManager.cs:784-811 : ambiance overlay, priorité colosse ; anciens IDs thick_fog/storm/ash_rain/forgotten_wind ; tonnerre en Crisis/LateGame. Les cinq IDs run_events.json actuels sont hunt/stampede/fallen_relic/vigil/shard_rain, donc les mappings météo ne sont pas atteints par ces événements actuels.

## Constats techniques concrets

- AudioManager.cs:335 : `public static void Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f)` ; clé inconnue ignorée sans avertissement dans PlaySfx:342-343.
- AudioManager.cs:364 : `public static void PlayUI(string key, float pitchVariance = 0f, float volumeDb = 0f)` ; clé inconnue ignorée sans avertissement dans PlayUiSfx:371-372.
- AudioManager.cs:460 : `private void PlayMusic(string key, float fadeDuration = 2.5f, bool loop = true)` ; A/B crossfade, pas de couches synchronisées ni tempo configuré.
- AudioManager.cs:186-214 : lecteurs AudioStreamPlayer non spatialisés pour tout ; pool effets 12, pool UI 3. Aucun AudioStreamPlayer2D trouvé dans scripts/scenes/data.
- AudioManager.cs:340-360 : un fichier par clé et variation de pitch ; aucune banque de variantes aléatoires par effet.
- AudioManager.cs:622-644 : cas Exploration, Crisis, LateGame, Death ; aucun cas Endgame. EndgameManager.cs:137 déclenche pourtant RunPhase.Endgame.
- AudioManager.cs:629-630 : ambiance forêt forcée en exploration ; ruines et marécage chargées mais jamais sélectionnées. Cinq définitions biomes existent (forêt, ruines, marécages, champs, carrière).
- AudioManager.cs:649-655 : avertissement Résurgence lance mus_crepuscule mais laisse phase Exploration ; _Process:552-553 peut réévaluer et remplacer la piste au prochain tick de 120 frames. Risque déduit du code, non observé en jeu.
- AudioManager.cs:665-669 : fin Résurgence retourne à la musique de phase ; mus_aube n’est jamais appelée.
- AudioManager.cs:711-713 : OnZoneDiscovered vide, donc pas de son de matérialisation branché ici.
- AudioManager.cs:582-599 : hooks EventBus existants. Pas de hook audio EssenceChanged, WeaponEquipped/Upgraded/Dropped, FusionAvailable/Completed, SynergyActivated ou ZonePhaseChanged.
- UI/LevelUpScreen.cs:449 appelle sfx_rare_fragment, absent.
- Aucun chemin audio direct dans scenes/data ; data contient six clés audio dynamiques documentées ci-dessus.

## Reliquats V1 à ne pas confondre avec des besoins V2

- Récolte : sfx_recolte_hache, sfx_recolte_pioche, sfx_recolte_obtenu.
- Craft/construction : sfx_craft_termine, sfx_structure_pose, sfx_craft_impossible, sfx_structure_impossible (ce dernier et recolte_obtenu n’ont que le throttle hors registre).
- Foyer : sfx_foyer_crepitement, sfx_foyer_aura, sfx_foyer_upgrade.
- Monde jour/nuit : sfx_monde_crepuscule, sfx_monde_aube.
- Musique : noms historiques jour/nuit/crépuscule réemployés pour V2 ; ne pas jeter les fonctions actuelles avec les noms. mus_aube n’est pas utilisée.

## Matrice des besoins V2 à compléter avant de chercher des candidats

Liste de couverture de systèmes, pas affirmation que chaque action doit obligatoirement produire un son.

| Famille | Couverture / manque à examiner |
|---|---|
| Musique | Hub, exploration, combat, signal Résurgence, Résurgence, accalmie, boss/late game, endgame, mort ; variations cohérentes et transitions |
| Joueur | Pas par surface et variantes ; esquive départ/fin aujourd’hui proxy gravier/béton ; dégât, coup majeur, santé basse, soin, mort |
| Combat / armes | 24 définitions armes, tous les coups de base partagent impacts ennemi/critique ; chercher familles lancement, swing, vol, impact, fin, mécaniques spéciales plutôt que 24 copies isolées |
| Ennemis | 16 définitions individuelles dont boss et 3 colosses ; présence, déplacement pertinent, annonce attaque, attaque, impact, mort/dissolution ; 15 WAV existants tous non enregistrés |
| Effacement | proximité actuellement son dédié ; distinguer transition phases, dégâts du néant, danger imminent, disparition de zone sans spam |
| Résurgences | annonce, démarrage, intensification, fin/accalmie ; thème et stingers |
| Autels | AltarManager.cs:257,276,297 : upgrade/reforge/soin, indisponibilité, cooldown, approche/présence ; aucune lecture audio directe |
| Progression | XP, level-up entrée/boucle/sortie, choix/refus, rareté, Essence, arme équipée/upgrade/drop, passif, synergie, fusion, malédiction, quête complète |
| Monde | 5 biomes ; points d’intérêt, coffres/raretés, pickup, lore, découverte ; 11 scripts Lore et pas de lecture directe audio |
| Micro-événements | hunt/Souverain, stampede/Harde, fallen_relic/Vestige tombé, vigil/Veille, shard_rain/Averse d’éclats ; annonce commune et succès générique, quelques sons réutilisés, pas de signatures complètes |
| Hub/meta/UI | hover/clic/confirmation, ouvrir/fermer journal ; sélection personnage, déblocage, constellation/mémorial, départ run, résultat/mort à arbitrer |

### Armes à couvrir

- `chipped_blade` — Lame Ébréchée (melee)
- `heavy_hammer` — Marteau Lourd (melee)
- `makeshift_bow` — Arc de Fortune (ranged)
- `sling` — Fronde Rouillée (ranged)
- `sharpened_pipe` — Tuyau Affûté (melee)
- `crossbow` — Arbalète Artisanale (ranged)
- `cleaver` — Couperet du Boucher (melee)
- `whip` — Fouet de Câbles (melee)
- `throwing_axes` — Haches de Jet (ranged)
- `nail_mace` — Masse Cloutée (melee)
- `teachers_bell` — La Cloche de l'Institutrice (melee)
- `surgeons_scalpel` — Le Scalpel du Chirurgien (melee)
- `lighthouse_shard` — Éclat de Phare (ranged)
- `music_box` — La Boîte à Musique (special)
- `chain_of_names` — La Chaîne des Noms (melee)
- `compass_needle` — L'Aiguille de Boussole (ranged)
- `photographers_flash` — Le Flash du Photographe (ranged)
- `essence_staff` — Bâton d'Essence (ranged)
- `void_edge` — Tranchant du Vide (melee)
- `memory_lantern` — Lanterne Mémorielle (ranged)
- `echo_gauntlets` — Gantelets d'Écho (melee)
- `childs_drawing` — Le Dessin d'Enfant (special)
- `last_broadcast` — La Dernière Émission (ranged)
- `clock_hand` — L'Aiguille de l'Horloge (melee)

### Ennemis à couvrir

- `charognard` — Charognard (melee)
- `colosse_forest` — Colosse Sylvestre (melee)
- `colosse_swamp` — Colosse des Profondeurs (melee)
- `colosse_urban` — Colosse de Béton (melee)
- `fading_spitter` — Cracheur Pâli (ranged)
- `hurleur` — Hurleur (ranged)
- `indicible` — L'Indicible (boss)
- `presage` — Présage (ranged)
- `rampant` — Rampant (melee)
- `rodeur` — Rôdeur (melee)
- `shade` — Ombre (melee)
- `shadow_crawler` — Rampant d'Ombre (melee)
- `tisseuse` — Tisseuse (ranged)
- `treant_corrompu` — Tréant Corrompu (melee)
- `void_brute` — Brute du Vide (melee)
- `wailing_sentinel` — Sentinelle Hurlante (ranged)

## Sources lues et modèles réutilisables

- AGENTS.md complet : pivot V2, hiérarchie et règles qualité. RTK.md absent du dépôt et des parents examinés ; prévenir parent.
- doc/VESTIGES-STRATEGIE-V2.md:197-207 (annonce/crise/accalmie), :633-638 (musique V2), :752 et :774 (roadmap son/musique encore ouverte).
- scripts/Infrastructure/AudioManager.cs complet : registre :92-176, APIs :335-460, branchements :582-599, musique/contextes :622-846. Modèles directement réutilisables pour jouer, limiter et relier un effet.
- scripts/Core/EventBus.cs:12-99 : événements disponibles pour une intégration découplée.
- scripts/Combat/Abilities/PounceAbility.cs:44-60,134,146 et data/enemies/charognard.json:28-29 : modèle data-driven pour sélection audio.
- scripts/Combat/Abilities/OmenStrikeAbility.cs:118,131 + data/enemies/presage.json:25-26 : deuxième exemple data-driven.
- scripts/Combat/MobilityFeedback.cs:47,51 + data/movement/mobility.json:13-14 : paire départ/fin.
- scripts/UI/UITheme.cs:159-175 : branchement central clic/survol.
- scripts/Events/RunEventDirector.cs:187-189,219 ; scripts/Events/RunEvents/FallenRelicEvent.cs:75,99 ; VigilEvent.cs:77 ; UI/RunEventHud.cs:218 : exemples run events.
- scripts/Events/EndgameManager.cs:129-139 : événement transition Endgame effectif.
- Guide audio et Bible §8 : anciennes versions consultées pendant le relevé ; direction et prompts remplacés depuis par la révision du 25 septembre conforme à la demande de Raphaël.
- data/weapons/weapons.json complet (24 entrées), data/enemies/*.json (16 ennemis individuels + table variantes), data/biomes/*.json (5 + template), data/events/run_events.json complet (5 micro-événements).

## Confiance et limites

- Haute pour comptages, chemins, clés et appels statiques. Exhaustivité du dépôt vérifiée par recherche extensions hors .godot/.git et recherche audio dans scripts/scenes/data.
- Aucun jugement de qualité sonore : pas d’écoute, pas de comparaison subjective, pas d’exécution du jeu. Les références ne prouvent pas qu’un branchement est atteint à chaque run.
- Aucun inventaire de durée/loudness/clipping ni contrôle des boucles ; à faire pendant audit technique.
- Aucun justificatif licence audio détecté via noms LICENSE/CREDITS usuels (seuls licence racine et police). Ne signifie pas preuve d’absence de droits : provenance encore à documenter.
- Les manques proposés sont des points à arbitrer, pas une exigence d’ajouter un bruit à chaque action.
- Ce document fige un relevé de code ; les remplacements de sons et corrections techniques restent à réaliser.

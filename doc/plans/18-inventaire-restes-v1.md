# Plan 18 — Inventaire des restes V1 et des visuels hors DA

Statut : **inventaire du 26 septembre 2026, décisions à prendre** · Priorité : P0 (lots 0B et 0C du [plan 17](17-armes-coffres-modificateurs.md)) · Dépendances : [08](08-direction-artistique.md), [13](13-butin.md), [05](05-armes-objets-builds.md).

**26 septembre, soir :** lots 0A à 0C livrés ([plan 17](17-armes-coffres-modificateurs.md#lot-0a-livré--26-septembre)). Restent de cet inventaire : les lignes 2, 14 à 17, 19, 22 à 26 du §3 (plans 08, 15, vague 2 ou 3), les quatre décors urbains à confirmer, les sons jamais joués (plan 15).

## 1. Le retour

Raphaël, le 26 septembre : « il y a plusieurs mécaniques anciennes qui sont encore présentes dans le jeu (souvent visibles sous la forme de sprites dans le jeu, sprites qui sont d'ailleurs totalement en dissonance avec le reste du jeu car pas du tout pixelisés) fais-en moi l'inventaire complet ».

## 2. Ce qu'il faut retenir

- **Les visuels « lisses » ne viennent presque pas des PNG.** Les 2 812 PNG de `assets/` (hors polices) ont été mesurés : taille, couleurs uniques, part d'alpha partiel, date. Ceux qui sont chargés en jeu sont tous du pixel art ; le plus chargé en couleurs, `prop_hanging_moss.png`, en a 99. Les rares PNG lisses (icônes d'application, anciens fonds du Hub, brouillard `tile_fog_f*`) ne sont jamais chargés.
- **La dissonance vient du rendu procédural en résolution écran.** On la retrouve dans :
  - des formes `Polygon2D` (POI, éléments de lore, Autels, auras) ;
  - des dessins `_Draw` et des dégradés radiaux (brume, particules douces) ;
  - des lumières dont la texture est **le logo de Godot** (`res://icon.svg`) ;
  - des textes posés dans le monde.

  La caméra est en zoom 2 : ces formes sont deux fois plus fines que la grille des pixels du jeu. Le plan 08 l'interdit déjà (« pas de forme vectorielle en résolution écran »).
- **Méthode.** « Vérifié » signifie que la chaîne d'appel a été suivie dans le code jusqu'à l'instanciation en run réelle. Le rendu à l'écran n'a pas été capturé pour cet inventaire : les descriptions de rendu sont déduites du code. Les lots 0B et 0C commenceront par des captures avant/après.

## 3. Ce que le joueur voit en run

Classé du plus fréquent au plus rare.

| # | Élément | Nature | Fréquence en run | Action proposée | Lot |
|---|---|---|---|---|---|
| 1 | Flash de montée de niveau : `PointLight2D` texturée par le logo Godot (`VfxFactory.CreateFlashLight`, `VfxFactory.cs:128`) | Visuel lisse | À chaque level-up, et aux explosions d'élites | Flash en pixel art, ou lumière supprimée | 0B |
| 2 | Particules ambiantes (`AmbientParticles`), brume de biome en dégradé radial (`BiomeAtmosphere`), halo des orbes d'XP : textures douces agrandies | Visuel lisse | Permanent en forêt et au marais | Refaire en tramé sur la grille des texels (`pixel_fx`) | 08 |
| 3 | **POI V1** : 7 types en formes `Polygon2D` (bâtiment à fouiller, cache de ressources, coffre gardé, ruine de lore, marchand, anomalie, sanctuaire), avec losange pulsant et anneau rouge des gardés (`PoiManager`, `PointOfInterest`, `data/pois/pois.json`) | Mécanique V1 + visuel | 10 à 20 par carte | Refondre, voir le §5 et le plan 17 §4.5 | 0B puis 3B/3C |
| 4 | **Marchand** (`merchant_npc`) : interaction « échange » sans table de butin ni code ; il ne donne rien | Mécanique V1 morte mais visible | Selon biome | Retirer | 0B |
| 5 | **Éléments de lore** : 11 types (`scripts/World/Lore/*.cs`, 8 mars), dont horloge folle, porte sans mur, miroir d'eau, champignons | Polygones, lumières au logo Godot | 25 à 40 par carte | Désactiver ou refaire en pixel art, voir le §5 | 0B |
| 6 | Faux Souvenirs émis par trois éléments de lore (`souvenir_les_signes`, `souvenir_avant`, `souvenir_eau`) : aucun n'existe dans `souvenirs.json` ; la popup s'affiche, rien n'est enregistré | Bug | Au contact | Retirer | 0B |
| 7 | Buff `warmth` (« −10 % dégâts la nuit », `DoorWithoutWall.cs:169`) : aucun écouteur, il ne fait rien | Mécanique V1 morte | Au contact | Retirer | 0B |
| 8 | **Perks V1 donnés par les coffres et POI** : `Player.ResolvePerkLoot` (`Player.cs:1939`) tire dans tous les perks, sans filtre. On peut recevoir Architecte, Récupérateur, Réparation express, Récolte abondante, Vision nocturne, Maître du temps (« le jour dure… »), Dernier rempart, Ancrage mémoriel (Foyer), Torche vivante, Méditation (« de jour »), ou le passif d'un autre personnage | Mécanique V1 (bug) | ≈ 20 % par tirage de coffre | Filtrer tout de suite, puis purger `perks.json` | 0B |
| 9 | **Malédictions imposées** (`CursedItemManager`, 3 entrées) : tombent de `chest_rare` (8 %) et `chest_epic` (15 %), seul le malus s'applique | Mécanique héritée | Coffres rares et épiques | Retirer des tables ; remplacées par la Faille (plan 17 §4.5) | 0A / 3C |
| 10 | Libellé brut « Souvenir: random_souvenir » (`ChestLootScreen.cs:314`, `Player.cs:1749, 1918`) et roulette qui s'arrête sur « Arme inconnue » | Bug de texte | Coffres | Corriger | 0A |
| 11 | Auras d'interaction (`InteractableAura`) : losanges translucides pulsants sous coffres, POI et Autels. Particules d'ouverture de coffre en losanges `Polygon2D` | Visuel lisse | Chaque coffre, POI, Autel | Refondre avec la colonne de lumière | 0A |
| 12 | Barre de « récolte » (`_harvestBar`, `Player.cs:1597`) : `ProgressBar` à coins arrondis au-dessus du joueur, pour fouiller et ouvrir | Nom V1 + visuel lisse | Chaque ouverture | Jauge pixel, renommée | 0A |
| 13 | Popups de butin en `Label` de 12 px posés dans le monde (`Player.cs:2012-2047`) | Visuel lisse | Chaque butin | Texte au style des chiffres de dégâts, ou HUD | 0A |
| 14 | **Autels** : losange `Polygon2D`, halo, `Label` ; touches Maj et Ctrl codées en dur ; agit sur le slot 0 seulement | Mécanique V2, visuel provisoire | 4 par carte | Refondre en Mémorial (plan 17 §4.5) | 3B |
| 15 | ~~**Arme au sol** (`WeaponPickup`) : halo en losange, deux `Label` de 10 à 12 px sans accents, icône à ×0,5 (densité de pixels mélangée)~~ **Refaite le 27 septembre** : anneau pixel, icône 32×32 à l'échelle 1, plus de texte au sol ; l'échange passe par l'invite commune des coffres | Visuel lisse | Chaque arme lâchée | Fait | 2B |
| 16 | **Icônes d'armes** 64×64 de mars, agrandies par facteurs non entiers, affichées à 22 et 48 px | PNG, écart d'échelle | Chaque arme | Refaire (plan 17 §4.7) | 2B |
| 17 | ~~Aura d'affixe des élites : ellipse `Polygon2D`~~ **Refaite le 27 septembre** en anneau pixel ([08](08-direction-artistique.md)) | Visuel lisse | Chaque élite | Fait | 08 |
| 18 | **Coffres** 16×12 de mars, trop petits face aux décors refaits | PNG, échelle | 12 à 14 par carte | Refaire (plan 17 §4.9) | 0A |
| 19 | **Props du marais** : 26 fichiers de l'ancien pipeline, alpha en dégradé (`prop_vine_curtain` à 87 % d'alpha partiel, `prop_hanging_moss`) | PNG, ancien pipeline | Biome marais | Régénérer comme forêt, ville et champs | 08 |
| 20 | **Tisseuse en triangle** : `data/enemies/tisseuse.json:18` utilise `"sprite"` au lieu de `sprite_folder`. L'ennemi s'affiche en `Polygon2D` ; ses 128 PNG ne servent pas | Bug de données | Résurgences au marais | Corriger la clé, vérifier l'ancrage des pieds | 0B |
| 21 | Marqueurs de micro-événements (`RunEventMarker`) : ellipses et colonne en `_Draw`. Plaque des ennemis et jauge de PV en `_Draw` avec police Saira | V2, vectoriel | Toutes les 2 à 3 min | Colonne sur la grille des texels, commune avec les coffres ; plaques : choix du plan 04 à confirmer | 0A |
| 22 | Anciens VFX encore chargés : orbe d'XP, explosion, dissolution (`dissolution_f1` à 100 % d'alpha partiel), éclaboussure | PNG anciens | Fréquent | Refaire (plan 08) | 08 |
| 23 | ~~Ennemis de mars non régénérés : cracheur, rampant, shadow_crawler, shade, sentinelle, hurleur, void_brute, tréant~~ **Régénérés le 27 septembre** dans le pipeline procédural ([08](08-direction-artistique.md)) | PNG, ancien pipeline | Selon biome et phase | Fait | 08 |
| 24 | **Indicible** : entièrement en `Polygon2D` (`Indicible.cs`) | Visuel lisse | Fin de run | Sprites | 08 |
| 25 | **Appel du Vide** : perk de coffre qui ajoute un bouton ON/OFF dans la pause ; malus pur ; l'activer efface les malédictions | Mécanique héritée | Si obtenu | Remplacé par le Péril (plan 17 §4.5) | 3A |
| 26 | Musique « jour », « crépuscule », « nuit » (`AudioManager.cs:103-108`) réaffectée aux phases V2 | Noms V1 | Toute la run | Renommer ou recomposer (plan 15) | 15 |

## 4. Ce qui ne se voit pas (code et données morts)

Vérifié par l'absence d'appel. Tout part au lot 0C, sauf mention contraire.

- **Chargeurs et données sans consommateur** :
  - `ResourceDataLoader.cs` et `data/resources/resources.json` ;
  - gabarits `data/recipes/_template.json`, `data/biomes/_template.json` (`resource_pool`), `data/loot_tables/_template.json` (`wood`) ;
  - `data/simulation/default_batch.json` ;
  - champs `resource_bias`, `ambient_color_day` et `ambient_color_dusk` (`BiomeDataLoader.cs`).
- **Classes mortes** : `UI/Minimap.cs` (jamais instanciée, contient `ColorFoyer`), `Events/EventSpriteFactory.cs` (marchand ambulant).
- **Signaux EventBus** :
  - écouté, jamais émis : `MemorialActivated` ;
  - ni émis ni écoutés : `FogRevealBurst`, `XpMultiplierChanged` ;
  - émis, jamais écoutés : `PlayerBuffApplied`, `FusionAvailable`, `FusionCompleted`, `WeaponDropped`, `RandomEventEnded`.
- **Mémorial de perks** : chemin mort dans `PerkManager` et `LevelUpScreen` (mode « MÉMORIAL »), effet `bonus_resource`.
- **Fusions** : détection active, jamais appliquées ; `cable_whip` invalide, `kill_restore_day` V1. À retirer si l'Éveil les remplace (plan 17 vague 5).
- **Synergies** : 8 entrées, simple notification, 4 exigent des perks désactivés.
- **Passif `fragment_deternite`** (`cooldown_reduction`) : proposé au level-up, sans aucun effet. **Visible et trompeur** : à retirer du tirage dès 0B.
- **Stats sans effet** : `harvest_speed`, `structure_hp`, `repair_speed`, `essence_cost`, `essence_drop_multiplier`, `salvage_return`, `vision_radius`, `day_duration` ; `IsPositionVisible` renvoie toujours vrai ; coût en Essence des armes (`_essenceDamagePenalty` toujours 1).
- **Colosses** : données, 257 PNG, comportement, barème de score, succès Steam, mais aucune source d'apparition. Réintégrer au plan 07 (boss de famille) ou supprimer.
- **Mutateurs** : `WorldSetup.PoisDisabled`, `GameManager.ActiveMutators` toujours vidé, `ScoreManager._mutatorMultiplier` toujours 1. À garder si les mutateurs de la V2 §18 restent prévus.
- **Nuits dans le score et l'historique** :
  - `ScoreManager` : `NoDamageNights`, `NightsSurvived`, `DeathNight`, `ResourcesCollected`, `StructuresPlaced`, `GetSurvivalPoints(nightNumber)` ;
  - `RunTracker` : structures, ressources, `CurrentNight` ;
  - `RunHistoryManager` : `nights_survived`, `death_night` ;
  - `GameOverScreen` : libellés vides.
  - La migration de `max_nights_survived` (`MetaSaveManager`) est à garder pour les anciennes sauvegardes.
- **Déblocage `survive_3_nights`** (Vagabond) : il correspond désormais à 12 minutes. Renommer.
- **Pools d'ennemis `day_enemy_pool` et `night_enemy_pool`** (5 biomes, `SpawnManager`) : comportement V2, noms V1. Renommer en pool d'exploration et pool de Résurgence.
- **Sons jamais joués** : `sfx_recolte_*`, `sfx_craft_*`, `sfx_structure_*`, `sfx_foyer_*`, `mus_aube`, `sfx_monde_crepuscule`, `sfx_monde_aube`.
- **Traductions mortes** : clés `CRAFT_*` ; clés `LEVELUP_*`, `WEAPON_*`, `RARITY_*`, `PAUSE_*` jamais utilisées. Ces dernières seront à reprendre dans la vague 1, pas à supprimer.
- **`.uid` orphelins suivis par git** : `scripts/Base/*.cs.uid` (CraftManager…), `scripts/UI/CraftPanel.cs.uid`, `scripts/World/Foyer.cs.uid`, `scripts/Infrastructure/RecipeDataLoader.cs.uid`.
- **Texte V1 dans les données** : `memory_lantern` « éclaire la nuit », `emits_light` ; `last_broadcast` « boss nuit 10+ » ; commentaire « CraftManager » et type `recipe` dans `SouvenirManager` ; couleur `PalGoldFoyer` du HUD.
- **Steam** :
  - succès `ACH_SURVIVE_NIGHT_*` branchés sur les Résurgences, et `STAT_MAX_NIGHTS` ;
  - `ACH_NO_DAMAGE_NIGHT` et `ACH_ALL_STRUCTURES_SURVIVE` sont impossibles à débloquer ;
  - classement `Vestiges_Nights`.
  - **Décision de Raphaël** : si ces identifiants sont déjà publiés côté Steam, ils se renomment dans Steamworks, pas seulement dans le code.

### PNG jamais affichés

À supprimer en 0C, avec leur `.import`, après une dernière recherche de référence (`rg` sur le nom et le chemin, chargeurs par convention compris). Les uid d'un `.import` supprimé ne doivent être référencés nulle part.

- **V1** : `resources/` (16), `structures/` (23 : murs, portes, pièges, établi, four), `tools/` (12), `foyer/foyer_level1.png`, `items/` (19).
- **HUD et fonds** : `hud_icon_wood/stone/metal/sun/void/heart/score/level`, `preview_hud_sprites`, `ui_foyer_glow`, `ui_hub_background` et son aperçu, `bg_menu_3840x2160`.
- **Tuiles et ennemis** : `tile_fog_f1-3`, `tiles/sanctuaire` (3), `enemies/ombre` (21), aperçus d'ennemis. Les `enemies/colosse_*` (257) suivent la décision sur les Colosses.
- **Armes et effets** : 13 variantes d'armes par matériau (`*_metal`, `*_pierre`, `*_essence`), `status_*.png`, VFX d'attaque hérités (plan 08), planches urbaines.
- **À confirmer** : `prop_supermarket_shelves`, `prop_graffiti_wall`, `prop_collapsed_stairs`, `prop_concrete_wall_v2`.

## 5. Décisions demandées à Raphaël

1. **Éléments de lore** (25 à 40 par carte, polygones et lumières au logo Godot, gain d'XP faible) :
   - **A (recommandé)** : les désactiver dès 0B, puis les refaire en pixel art plus tard (plan 08, lot « récits »), en moins grand nombre ;
   - **B** : les garder tels quels jusqu'à leur refonte.
2. **POI V1**. Correspondance proposée avec le plan 17 :

   | POI actuel | Devient |
   |---|---|
   | sanctuaire | Mémorial |
   | anomalie | Faille |
   | coffre gardé | coffre épique gardé (système des coffres) |
   | ruine de lore | élément de lore |
   | bâtiment à fouiller | conservé, en pixel art |
   | cache de ressources | retirée |
   | marchand | retiré |

   Jusqu'à la vague 3, les garder fonctionnels (la quête « Explorer 3 points d'intérêt » en dépend) ou les masquer ?
3. **Colosses** : réintégrer comme boss de famille (plan 07) ou supprimer ?
4. **Steam** : les identifiants « nuits » sont-ils publiés dans Steamworks ? Si non, renommage libre.
5. **Mutateurs** : garder le squelette pour la V2 §18, ou supprimer en attendant ?

### Réponses de Raphaël — 26 septembre 2026

| # | Réponse | Application |
|---|---|---|
| 1 | Désactiver les éléments de lore en attendant leur refonte | Option A, lot 0B |
| 2 | Pareil pour les points d'intérêt d'ici la vague 3 | POI désactivés en 0B. La quête de run « Explorer 3 points d'intérêt » et la stabilisation de la mémoire par exploration en dépendent : retirer la quête du tirage et vérifier l'Effacement sans POI dans le même lot |
| 3 | Colosses : « supprimer peut-être ? » | Suppression retenue au lot 0C, sauf objection de Raphaël d'ici là |
| 4 | Le jeu n'est pas sur Steam : aucun identifiant n'est publié | Succès, statistiques et classement « nuits » renommés ou supprimés librement en 0C |
| 5 | Pas de réponse | Squelette des mutateurs conservé (V2 §18), rien de nouveau |

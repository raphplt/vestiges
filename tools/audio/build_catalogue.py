#!/usr/bin/env python3
"""Construit le catalogue documentaire audio et contrôle sa couverture des données.

Aucun fichier du jeu n'est modifié. Les choix/recettes existants sont conservés.
--check compare les sorties sans écrire ; les numéros de lignes sont recalculés.
"""
import argparse
import collections
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'doc/audio'
AUDIO = 'scripts/Infrastructure/AudioManager.cs'
SECOND = ['enemy_hit', 'player_hit', 'dissolution', 'level_up', 'perk_select', 'danger_warning', 'chest_reveal']
FIRST = ['enemy_hit', 'critical_hit', 'player_hit', 'dissolution', 'xp_pickup', 'level_up', 'perk_select', 'chest_open', 'dash_start', 'danger_warning']

def read(path):
    return (ROOT / path).read_text()

def data(path):
    return json.loads(read(path))

def ref(path, token=None):
    content = read(path)
    if token is not None:
        assert token in content, (path, token)
        return f'{path}:{content[:content.index(token)].count(chr(10)) + 1}'
    return path

def validate_choice(effect):
    """Valide le choix après synchronisation ; renvoie uniquement un candidat retenu."""
    choice = effect['raphael_choice']
    if choice is None:
        return False
    prefix = f"Choix invalide pour {effect['effect_id']}"
    if not isinstance(choice, dict) or choice.get('decision') not in ('pending', 'candidate', 'none', 'silence'):
        raise ValueError(prefix + ' : objet decision/candidate_id attendu ; aucune sortie écrite.')
    if not isinstance(choice.get('notes', ''), str):
        raise ValueError(prefix + ' : notes doit être un texte ; aucune sortie écrite.')
    candidate_id = choice.get('candidate_id')
    if choice['decision'] != 'candidate':
        if candidate_id is not None:
            raise ValueError(prefix + ' : candidate_id doit être null hors sélection ; aucune sortie écrite.')
        return False
    available = {candidate['candidate_id'] for candidate in effect['candidates']}
    if not isinstance(candidate_id, str) or candidate_id not in available:
        raise ValueError(prefix + f' : candidat retenu {candidate_id!r} absent de la nouvelle liste ; '
                         'rétablir le candidat ou faire réviser ce choix par Raphaël. Aucune sortie écrite.')
    return True

registry = dict(re.findall(r'\["([^"\n]+)"\]\s*=\s*"res://([^"\n]+)"', read(AUDIO)))
effects = []
mappings = {}

def effect(id, family, trigger, intent, path, token=None, keys='', state=None, duration=(0.1, 1.2), loop=False, decision=False, scope='actuel'):
    current = []
    for key in keys.split(','):
        if not key:
            continue
        files = [registry[key]] if key in registry else [str(p.relative_to(ROOT)) for p in sorted((ROOT/'assets/audio').rglob(key+'.wav'))]
        current.append({'key': key, 'registered': key in registry, 'files': files})
    effects.append({'effect_id': id, 'family': family, 'trigger': trigger, 'references': [ref(path, token)], 'current_sound': current,
        'current_state': state or ('clé appelée mais absente du registre ; aucun son chargé pour cet appel' if any(not c['registered'] for c in current) else 'appel existant, audibilité non recettée' if current else 'branchement audio dédié non relevé'),
        'coverage_status': 'à rechercher' if scope == 'actuel' else ('héritage V1' if scope == 'héritage V1' else 'hors périmètre actuel'),
        'intent': intent, 'target_duration_seconds': list(duration), 'playback': 'boucle' if loop else 'ponctuel',
        'desired_variants': 4 if family in ('joueur', 'armes', 'ennemis', 'combat') else 3,
        'priority': 'P0' if id in FIRST else 'P2' if decision or scope != 'actuel' else 'P1',
        'decision_needed': 'son dédié ou partage à arbitrer ; aucun silence décidé' if decision else None,
        'scope': scope, 'candidates': [], 'raphael_choice': None, 'review_history': [],
        'integration': {'status': 'non intégré dans la refonte', 'notes': 'Le branchement éventuel décrit ci-dessus est celui des anciens fichiers.'},
        'validation': {'status': 'non recetté', 'notes': 'Aucune écoute ni capture de run dans ce catalogue.'}})

# Premier panier : les identifiants sont également utilisés par la page d'écoute.
for id, family, label, key, path, token, intent in [
 ('enemy_hit','combat','Impact sur ennemi','sfx_hit_ennemi','scripts/Combat/Enemy.cs','isCrit ?','Court, physique et lisible en rafale.'),
 ('critical_hit','combat','Impact critique','sfx_hit_critique','scripts/Combat/Enemy.cs','isCrit ?','Accent plus net que l’impact ordinaire sans masquer les dangers.'),
 ('player_hit','joueur','Dégât reçu','sfx_hit_joueur',AUDIO,'private void OnPlayerDamaged','Distinguer immédiatement le joueur blessé des ennemis touchés.'),
 ('dissolution','combat','Mort ennemi / dissolution ; proxy chute de relique','sfx_monde_dissolution',AUDIO,'private void OnEnemyKilled','Matière qui se défait, bref et compatible avec les morts multiples.'),
 ('xp_pickup','progression','Collecte orbe XP','xp_gain','scripts/Combat/XpOrb.cs','AudioManager.Play','Petite note gratifiante qui supporte les rafales.'),
 ('level_up','progression','Entrée montée de niveau','sfx_level_up','scripts/UI/LevelUpScreen.cs','PlayUI("sfx_level_up")','Ascension mélodique claire, récompense.'),
 ('perk_select','progression','Choix perk, fragment ou butin','sfx_perk_choix',AUDIO,'private void OnPerkChosen','Confirmation satisfaisante, brève.'),
 ('chest_open','monde','Ouverture physique du coffre','sfx_chest_opening','scripts/UI/ChestLootScreen.cs','PlayUI("sfx_chest_opening"','Mécanisme tangible d’ouverture ; révélation musicale traitée séparément.'),
 ('dash_start','joueur','Départ esquive','sfx_pas_gravier','scripts/Combat/MobilityFeedback.cs','mobility.Config.StartAudio','Impulsion brève, frottement ou souffle, direction à choisir.'),
 ('danger_warning','événements','Annonce Résurgence, boss/colosse et micro-événement','sfx_danger_building',AUDIO,'private void OnCrisisWarning','Danger identifiable, bref, sans confusion avec une récompense.')]:
    effect(id,family,label,intent,path,token,key)

effect('chest_reveal', 'progression', 'Mélodie après ouverture du coffre',
       'Quelques secondes de mélodie gratifiante après le mécanisme physique ; référence actuelle appréciée.',
       'scripts/UI/ChestLootScreen.cs', 'PlayUI("sfx_chest_opening"', 'sfx_chest_opening',
       state='Actuellement un seul fichier accompagne ouverture et présentation ; aucune couche de révélation distincte.',
       duration=(2,5))
effects[-1]['priority'] = 'P0'
effects[-1]['references'] += ['doc/audio/lot-a/choix-raphael-2026-09-25.json']

# Les durées sont des briefs proposés, jamais des mesures des sources.
rows = [
 ('dash_end','joueur','Fin esquive','Frottement léger, articulé au départ.','scripts/Combat/MobilityFeedback.cs','mobility.Config.EndAudio','sfx_pas_beton'),
 ('player_heavy_hit','joueur','Coup majeur reçu','Accent de gravité ponctuel.',AUDIO,'CriticalDamageThresholdRatio','sfx_degat_critique_recu'),
 ('player_heal','joueur','Soin effectif','Retour de vitalité discret.','scripts/Core/Player.cs','public void Heal',''),
 ('player_death','joueur','Mort joueur','Ponctuation distincte du thème de mort.','scripts/Core/Player.cs','private void Die',''),
 ('level_up_exit','progression','Sortie choix level-up','Raccord bref vers la run.','scripts/UI/LevelUpScreen.cs','PlayUI("sfx_level_up_after")','sfx_level_up_after'),
 ('perk_refuse','progression','Refus de choix','Refus doux et compréhensible.','scripts/UI/LevelUpScreen.cs','PlayUI("sfx_perk_refuse"','sfx_perk_refuse'),
 ('rare_fragment','progression','Révélation fragment rare','Accent de rareté.','scripts/UI/LevelUpScreen.cs','PlayUI("sfx_rare_fragment")','sfx_rare_fragment'),
 ('essence_gain','progression','Gain d’Essence','Signal partagé pour récompenses et collectes, à limiter.','scripts/Progression/EssenceTracker.cs',None,''),
 ('weapon_equip','progression','Équiper / échanger arme','Confirmation matérielle partagée.','scripts/Core/EventBus.cs','WeaponEquippedEventHandler',''),
 ('weapon_upgrade','progression','Améliorer arme','Gain de puissance.','scripts/Core/EventBus.cs','WeaponUpgradedEventHandler',''),
 ('weapon_drop','progression','Déposer arme','Petit dépôt matériel.','scripts/Core/EventBus.cs','WeaponDroppedEventHandler',''),
 ('passive_gain','progression','Obtenir / améliorer passif','Peut partager le choix de fragment.','scripts/Core/EventBus.cs','PassiveSouvenirAddedEventHandler',''),
 ('synergy_activate','progression','Synergie activée','Récompense de combinaison.','scripts/Core/EventBus.cs','SynergyActivatedEventHandler',''),
 ('fusion_ready','progression','Fusion disponible','Annonce rare et reconnaissable.','scripts/Core/EventBus.cs','FusionAvailableEventHandler',''),
 ('fusion_complete','progression','Fusion terminée','Transformation marquante.','scripts/Core/EventBus.cs','FusionCompletedEventHandler',''),
 ('curse_accept','progression','Accepter objet maudit','Récompense mêlée de menace.','scripts/Progression/CursedItemManager.cs','AudioManager.Play','sfx_malediction_acceptee'),
 ('quest_complete','progression','Quête accomplie','Récompense partagée.','scripts/Progression/QuestManager.cs',None,''),
 ('souvenir_found','méta','Découverte souvenir','Émotion, motif identifiable.',AUDIO,'private void OnSouvenirDiscovered','sfx_souvenir_trouve'),
 ('artifact_found','monde','Découverte lore / sanctuaire / relique','Révélation rare.',AUDIO,'private void OnPoiExplored','sfx_artefact_trouve'),
 ('world_reveal','monde','Zone découverte','Matérialisation parcimonieuse.',AUDIO,'private void OnZoneDiscovered','sfx_monde_tuile_apparait'),
 ('world_erase','monde','Zone qui s’efface','Dissolution environnementale à limiter.','scripts/World/ErasureManager.cs',None,''),
 ('altar_upgrade','monde','Autel amélioration','Transformation pierre/métal/Essence.','scripts/World/AltarManager.cs','private void TryUpgrade',''),
 ('altar_reforge','monde','Autel reforge','Transformation plus marquée.','scripts/World/AltarManager.cs','private void TryReforge',''),
 ('altar_heal','monde','Autel soin','Peut partager le soin joueur.','scripts/World/AltarManager.cs','private void TryHeal',''),
 ('interaction_unavailable','monde','Interaction impossible / recharge','Signal court, discret, partagé.','scripts/World/AltarManager.cs','private void SetFeedback',''),
 ('poi_search','monde','Fouille POI','Frottement/fouille tangible.','scripts/World/PointOfInterest.cs',None,''),
 ('poi_activate','monde','Activer POI','Signature de reconnaissance partagée.','scripts/World/PointOfInterest.cs',None,''),
 ('ui_hover','interface','Survol','Infime déplacement de matière.','scripts/UI/UITheme.cs','sfx_menu_survol','sfx_menu_survol'),
 ('ui_click','interface','Clic','Clic tactile bref.','scripts/UI/UITheme.cs','sfx_menu_clic','sfx_menu_clic'),
 ('ui_confirm','interface','Confirmer','Confirmation nette.','scripts/UI/HubScreen.cs','sfx_menu_confirmer','sfx_menu_confirmer'),
 ('journal_open','interface','Ouvrir journal','Page/objet retrouvé.','scripts/UI/JournalScreen.cs','sfx_inventaire_ouvrir','sfx_inventaire_ouvrir'),
 ('journal_close','interface','Fermer journal','Fermeture brève.','scripts/UI/JournalScreen.cs','sfx_inventaire_fermer','sfx_inventaire_fermer'),
 ('meta_unlock','méta','Déblocage personnage / collection','Récompense rare, partage envisageable.','scripts/UI/HubScreen.cs',None,''),
 ('memorial_activate','méta','Activer mémorial / constellation','Émotion et reconnaissance.','scripts/Core/EventBus.cs','MemorialActivatedEventHandler',''),
 ('run_departure','interface','Départ run / transition','Passage du Hub au monde.','scripts/UI/VoidTransition.cs',None,''),
 ('event_success','événements','Objectif micro-événement réussi','Confirmation partagée.','scripts/UI/RunEventHud.cs','sfx_souvenir_trouve','sfx_souvenir_trouve'),
 ('event_fail','événements','Objectif expiré / perdu','Information discrète.','scripts/Events/RunEvents/RunEvent.cs','protected void Fail',''),
 ('crisis_start','événements','Début Résurgence','Ponctuation articulée à la musique.','scripts/Events/CrisisManager.cs',None,''),
 ('crisis_end','événements','Fin Résurgence','Relâchement clair.','scripts/Events/CrisisManager.cs',None,''),
 ('relic_fall','événements','Relique en chute / atterrissage','Chute et impact du vestige.','scripts/Events/RunEvents/FallenRelicEvent.cs','private void Land','sfx_monde_dissolution'),
 ('shard_strike','événements','Chute d’éclat et impact','Annonce/impact lisibles, variantes.','scripts/Events/RunEvents/ShardRainEvent.cs','private void LaunchStrike',''),
 ('stampede_pass','événements','Passage de harde','Masse de pas dosée selon proximité.','scripts/Events/RunEvents/StampedeEvent.cs',None,''),
 ('vigil_hold','événements','Veille : présence / progression','Repère doux, succès partagé.','scripts/Events/RunEvents/VigilEvent.cs',None,''),
]
for id,fam,label,intent,path,token,key in rows:
    effect(id,fam,label,intent,path,token,key,decision=not bool(key))

for id,trigger,key,path,token in [
 ('low_health','Santé basse','sfx_sante_basse',AUDIO,'private void UpdatePlayerWarnings'),
 ('erasure_near','Proximité Effacement / bord de carte','sfx_bord_effacement_proche',AUDIO,'sfx_bord_effacement_proche", -11f'),
 ('level_up_wait','Attente choix niveau','sfx_level_up_loop','scripts/UI/LevelUpScreen.cs','PlayLoop("sfx_level_up_loop"'),
 ('colossus_presence','Colosse présent','sfx_colosse_lointain',AUDIO,'private void UpdateContextAmbiance')]:
    effect(id,'ambiances',trigger,'Boucle qui reste lisible et ne fatigue pas.',path,token,key,duration=(4,20),loop=True)

surface_keys = {'grass':'herbe','forest':'herbe','concrete':'beton','water':'eau'}
for surface,key in surface_keys.items():
    effect('step_'+surface,'joueur','Pas sur '+surface,'Matière concrète, variations naturelles.','scripts/Core/Player.cs','sfx_pas_eau','sfx_pas_'+key,decision=surface=='forest',duration=(0.08,0.5))
effect('step_gravel','joueur','Pas sur gravier / carrière','Distinguer cailloux du couvert végétal.','data/biomes/collapsed_quarry.json',keys='sfx_pas_gravier',state='Fichier utilisé pour esquive ; surface gravier dédiée non relevée.',decision=True)
effect('step_wood','joueur','Pas sur bois','À arbitrer selon surfaces accessibles.','scripts/Core/Player.cs','sfx_pas_eau',keys='sfx_pas_bois',state='Fichier enregistré, non sélectionné par les pas actuels.',decision=True)

# Familles de lancement : un seul besoin peut couvrir plusieurs armes.
weapon_groups = {
 'blade_swing':('Lame légère','chipped_blade,sharpened_pipe,surgeons_scalpel'),
 'heavy_swing':('Frappe lourde','heavy_hammer,cleaver,nail_mace'),
 'bow_release':('Corde et flèche','makeshift_bow'), 'crossbow_release':('Mécanisme arbalète','crossbow'),
 'throw_release':('Lancer de pierre / hache','sling,throwing_axes'), 'whip_snap':('Fouet de câbles','whip'),
 'bell_pulse':('Cloche et onde','teachers_bell'), 'light_shot':('Projection de lumière','lighthouse_shard'),
 'music_box_note':('Notes orbitales','music_box'), 'chain_jump':('Chaîne et rebonds','chain_of_names'),
 'needle_shot':('Aiguille à tête chercheuse','compass_needle'), 'camera_flash':('Flash photographique','photographers_flash'),
 'essence_shot':('Orbe d’Essence','essence_staff'), 'void_slash':('Tranchant du Vide','void_edge'),
 'lantern_fire':('Flamme mémorielle / zone','memory_lantern'), 'echo_strike':('Frappe puis écho retardé','echo_gauntlets'),
 'drawing_launch':('Dessin animé / impact de forme','childs_drawing'), 'radio_wave':('Émission radio soutenue','last_broadcast'),
 'clock_strike':('Horloge / temps ralenti','clock_hand')}
weapon_lookup={}
for group,(label,ids) in weapon_groups.items():
    effect(group,'armes',label,'Signature courte et variations ; ne pas ajouter un son par projectile ou tick.','data/weapons/weapons.json',f'"id": "{ids.split(",")[0]}"',decision=True,duration=(0.1,2 if group=='radio_wave' else 0.8))
    for id in ids.split(','):weapon_lookup[id]=group
mappings['weapons']=[]
for w in data('data/weapons/weapons.json'):
    ids=[weapon_lookup[w['id']],'enemy_hit','critical_hit','weapon_equip','weapon_upgrade','weapon_drop']
    if w['id']=='surgeons_scalpel':ids+=['player_heal']
    mappings['weapons'].append({'id':w['id'],'name':w['name'],'references':[ref('data/weapons/weapons.json',f'"id": "{w["id"]}"')], 'effect_ids':ids, 'mechanics':{k:w[k] for k in ('attack_pattern','special_effect','on_hit_effect') if k in w},'note':'Famille proposée ; impacts partagés. Audio des mécaniques spéciales à confirmer en run, pas de son par tick.'})

# Capacités réellement présentes dans le code, sans cris d’idle à réintroduire.
abilities = [
 ('pounce_windup','Préparation bond','scripts/Combat/Abilities/PounceAbility.cs','_windupAudio','sfx_charognard_meute'),
 ('pounce_leap','Bond','scripts/Combat/Abilities/PounceAbility.cs','_leapAudio','sfx_ombre_attaque'),
 ('omen_cast','Annonce frappe Présage','scripts/Combat/Abilities/OmenStrikeAbility.cs','_castAudio','sfx_sentinelle_activation'),
 ('omen_impact','Impact frappe Présage','scripts/Combat/Abilities/OmenStrikeAbility.cs','_impactAudio','sfx_rampant_surgissement'),
 ('enemy_ranged_shot','Projectile ennemi','scripts/Combat/Enemy.cs','private void ShootProjectile',''),
 ('screamer_call','Cri qui invoque des renforts','scripts/Combat/Enemy.cs','private void ProcessScreamerCry',''),
 ('burrow_transition','Enfouissement / surgissement','scripts/Combat/Enemy.cs','private void ProcessBurrowerPhase',''),
 ('enemy_charge','Charge brute / colosse','scripts/Combat/Enemy.cs','private void ProcessChargerAbilities',''),
 ('colossus_slam','Frappe de sol colosse','scripts/Combat/Enemy.cs','private void PerformGroundSlam',''),
 ('enemy_explode','Explosion affixe Instable','scripts/Combat/Enemy.cs','private void Die',''),
 ('boss_tentacle','Tentacule : annonce et frappe','scripts/Combat/Indicible.cs','private void SpawnTentacleAttack',''),
 ('boss_enrage','Passage phase enragée','scripts/Combat/Indicible.cs','private void EnterEnragedPhase',''),
 ('boss_death','Mort Indicible','scripts/Combat/Indicible.cs','private void Die','')]
for id,label,path,token,key in abilities:
    effect(id,'ennemis',label,'Annonce de mécanique utile, localisation/limitation à prévoir ; aucune boucle idle.',path,token,key,decision=not bool(key))
behaviors={'pack':['pounce_windup','pounce_leap'],'screamer':['screamer_call','enemy_ranged_shot'],'burrower':['burrow_transition'],'colosse':['colossus_presence','enemy_charge','colossus_slam','danger_warning'],'charger':['enemy_charge'],'sentinel':['enemy_ranged_shot'],'weaver':['enemy_ranged_shot'],'indicible':['boss_tentacle','boss_enrage','boss_death','danger_warning']}
mappings['enemies']=[]
for p in sorted((ROOT/'data/enemies').glob('[!_]*.json')):
    d=json.loads(p.read_text());ids=['enemy_hit','critical_hit','dissolution','player_hit']
    ids+=behaviors.get(d.get('behavior'),['enemy_ranged_shot'] if d['type']=='ranged' else [])
    if 'omen_strike' in d.get('abilities',{}): ids=[i for i in ids if i!='enemy_ranged_shot']+['omen_cast','omen_impact']
    if d['id']=='indicible': ids=['enemy_hit','boss_tentacle','boss_enrage','boss_death','dissolution','danger_warning','player_hit']
    mappings['enemies'].append({'id':d['id'],'name':d['name'],'references':[str(p.relative_to(ROOT))], 'effect_ids':ids,'behavior':d.get('behavior'),'abilities':list(d.get('abilities',{})),'note':'Impacts/actions partagés ; aucune voix propre imposée. Idle/alertes répétitives exclus par Stratégie V2 §21. Indicible utilise sa classe dédiée : impact ennemi proposé à ajouter, dissolution déjà via EnemyKilled.'})
variants=data('data/enemies/_variants.json')
for group,source in [('enemy_variants','variants'),('enemy_affixes','affixes')]:
    mappings[group]=[{'id':id,'references':[ref('data/enemies/_variants.json',f'"{id}"')], 'effect_ids':['enemy_hit','critical_hit','dissolution']+(['enemy_explode'] if id=='explosive' else []),'note':'Hérite des capacités de l’espèce ; aucun cri répétitif ni signature par multiplicateur statistique.'} for id in variants[source]]

mappings['biomes']=[]
biome_sounds={'forest_reclaimed':'sfx_ambiance_foret','urban_ruins':'sfx_ambiance_ruines','swamp':'sfx_ambiance_marecages','wild_fields':'','collapsed_quarry':''}
for p in sorted((ROOT/'data/biomes').glob('[!_]*.json')):
    d=json.loads(p.read_text()); id='ambience_'+d['id']
    effect(id,'ambiances','Ambiance '+d['name'],'Boucle naturelle, légère, compatible musique mélodique.',str(p.relative_to(ROOT)),keys=biome_sounds[d['id']],state='Exploration force actuellement forêt ; sélection selon biome à intégrer.',duration=(30,120),loop=True)
    mappings['biomes'].append({'id':d['id'],'name':d['name'],'references':[str(p.relative_to(ROOT))],'effect_ids':[id]+['step_'+s for s in d['terrain_weights']]+(['step_gravel'] if d['id']=='collapsed_quarry' else []),'terrain_weights':d['terrain_weights'],'note':'Surface forest partage actuellement herbe. Aucun terrain gravier dédié dans les données.'})
effect('ambient_details','ambiances','Oiseaux, eau, vent ponctuels','Détails occasionnels, contextualisés au biome.',AUDIO,'birdKey',keys='sfx_ambiance_oiseaux_1,sfx_ambiance_oiseaux_2,sfx_ambiance_bulle,sfx_foret_rafales',state='Oiseaux joués en Exploration ; autres détails non attestés sur biomes actuels.',decision=True,duration=(0.5,8))
effect('crisis_thunder','ambiances','Tonnerre en Résurgence / LateGame','Tension environnementale ponctuelle.',AUDIO,'private void UpdateContextAmbiance',keys='sfx_tonnerre_lointain',duration=(10,40),loop=True)
effect('weather_legacy_mapping','ambiances','Anciens mappings météo','Vérifier utilité avant toute recherche.',AUDIO,'private void UpdateContextAmbiance',keys='sfx_pluie_legere,sfx_orage_proche,sfx_brouillard',state='IDs météo historiques absents des 5 micro-événements actuels.',scope='hors périmètre actuel',loop=True,duration=(20,90))

music=[('exploration','Exploration','mus_jour_exploration'),('combat','Combat','mus_jour_combat'),('warning','Annonce Résurgence','mus_crepuscule'),('crisis','Résurgence','mus_nuit_vagues'),('aftermath','Accalmie après Résurgence','mus_aube'),('late_game','LateGame / Indicible','mus_nuit_chaos'),('endgame','Endgame',''),('hub','Hub','mus_hub'),('death','Mort / résultat','mus_mort')]
for id,label,key in music:
    effect('music_'+id,'musique',label,'Musique de jeu vidéo : mélodie, rythme, progression ; distorsion dosée.',AUDIO,keys=key,state='Pas de case Endgame audio.' if id=='endgame' else 'Fichier enregistré sans appel relevé.' if id=='aftermath' else 'Sélection existante ; transitions à recetter.',duration=(90,180) if id!='death' else (4,20),loop=id!='death')

mappings['events']=[]
event_extra={'hunt':['enemy_hit','dissolution','chest_open'],'stampede':['stampede_pass','xp_pickup'],'fallen_relic':['relic_fall','artifact_found'],'vigil':['vigil_hold','souvenir_found','player_heal'],'shard_rain':['shard_strike','xp_pickup','player_hit']}
for d in data('data/events/run_events.json')['events']:
    mappings['events'].append({'id':d['id'],'references':[ref('data/events/run_events.json',f'"id": "{d["id"]}"')],'effect_ids':['danger_warning','event_success','event_fail']+event_extra[d['id']],'note':'Annonce/résultat partagés ; signatures proposées à arbitrer.'})

# Couverture des écrans et widgets présents, y compris ceux qui ne nécessitent pas de son propre.
screens={
 'HubScreen':['music_hub','ui_hover','ui_click','ui_confirm','meta_unlock','memorial_activate','run_departure'],
 'HubBackdrop':['music_hub'],
 'HubCamp':['ui_hover','ui_click'],
 'HubChroniquesPanel':['ui_hover','ui_click'],
 'HubMenuButton':['ui_hover','ui_click'],
 'LevelUpScreen':['level_up','level_up_wait','level_up_exit','perk_select','perk_refuse','rare_fragment','ui_hover','ui_click'],
 'ChestLootScreen':['chest_open','chest_reveal','perk_select','ui_hover','ui_click'],
 'GameOverScreen':['music_death','ui_confirm','ui_hover','ui_click'],
 'PauseMenu':['ui_confirm','ui_hover','ui_click'], 'SettingsScreen':['ui_hover','ui_click'],
 'JournalScreen':['journal_open','journal_close','ui_hover','ui_click'],
 'SouvenirPopup':['souvenir_found'], 'RunEventHud':['danger_warning','event_success','event_fail'],
 'HUD':['xp_pickup','level_up','low_health','erasure_near'], 'PlayerHealthGauge':['player_hit','low_health','player_heal'],
 'VoidTransition':['run_departure'], 'GameLoadingOverlay':['run_departure'],
 'Minimap':['world_reveal'], 'DebugActionPanel':['ui_click'], 'DebugOverlay':['ui_click'],
 'DevelopmentBadge':['ui_click'], 'UITheme':['ui_hover','ui_click'], 'PerkIconResolver':['perk_select']}
mappings['screens']=[]
for p in sorted((ROOT/'scripts/UI').glob('*.cs')):
    name=p.stem
    scope='outil / composant technique' if name in ['DebugActionPanel','DebugOverlay','DevelopmentBadge','UITheme','PerkIconResolver','HubBackdrop'] else 'actuel'
    mappings['screens'].append({'id':name,'references':[str(p.relative_to(ROOT))],'effect_ids':screens[name],'scope':scope,'note':'Correspondance de contexte ou signal partagé, pas preuve d’appel depuis ce widget. Minimap désactivée selon plan 13 ; composants techniques hors recherche de sons propres.'})

for group,path,ids in [
 ('characters','data/characters/characters.json',['ui_confirm','meta_unlock','player_hit','player_heal','player_death','dash_start','dash_end']),
 ('chests','data/chests/chests.json',['chest_open','chest_reveal','perk_select']),
 ('pois','data/pois/pois.json',['poi_search','poi_activate','artifact_found']),
 ('perks','data/perks/perks.json',['perk_select','synergy_activate']),
 ('passives','data/progression/passive_souvenirs.json',['perk_select','passive_gain']),
 ('souvenirs','data/souvenirs/souvenirs.json',['souvenir_found']),
 ('constellations','data/souvenirs/constellations.json',['memorial_activate','meta_unlock']),
 ('quests','data/quests/quests.json',['quest_complete','meta_unlock']),
 ('cursed_items','data/cursed_items/cursed_items.json',['curse_accept'])]:
    mappings[group]=[{'id':d['id'],'name':d.get('name',d['id']),'references':[ref(path,f'"id": "{d["id"]}"')],'effect_ids':ids,'note':'Retour partagé ; pas de son unique imposé à chaque entrée.'} for d in data(path) if isinstance(d, dict) and "id" in d]

mappings['fusions']=[]
for d in data('data/progression/fusions.json'):
    group=weapon_lookup.get(d['weapon_id'],'whip_snap')
    mappings['fusions'].append({'id':d['id'],'name':d['name'],'references':[ref('data/progression/fusions.json',f'"id": "{d["id"]}"')],'effect_ids':['fusion_ready','fusion_complete',group,'enemy_hit','critical_hit'],'note':'Hérite de la famille source. Données historiques à vérifier : cable_whip ne correspond à aucune arme ; kill_restore_day contient une mécanique V1, ne pas réintroduire le jour.'})

# Les objets de lore ont un seul besoin partagé à arbitrer, pas onze sons imposés.
effect('lore_detail','monde','Rencontre / interaction élément de lore','Signe diégétique discret, sous-variantes uniquement si utiles.','scripts/World/Lore/GhostChime.cs',decision=True,duration=(0.3,4))
mappings['lore']=[{'id':p.stem,'references':[str(p.relative_to(ROOT))],'effect_ids':['lore_detail','artifact_found'],'note':'Aucun appel audio direct relevé ; timbre propre à arbitrer, objets sonores (carillon/horloge/pas) prioritaires.'} for p in sorted((ROOT/'scripts/World/Lore').glob('*.cs'))]
mappings['systems']=[{'id':id,'references':[ref(path)],'effect_ids':ids,'note':'Mutualisation proposée, aucune obligation de son par changement de valeur.'} for id,path,ids in [
 ('altars','scripts/World/AltarManager.cs',['altar_upgrade','altar_reforge','altar_heal','interaction_unavailable']),
 ('erasure','scripts/World/ErasureManager.cs',['erasure_near','world_erase','player_hit']),
 ('crises','scripts/Events/CrisisManager.cs',['danger_warning','crisis_start','crisis_end','music_warning','music_crisis','music_aftermath','crisis_thunder']),
 ('endgame','scripts/Events/EndgameManager.cs',['danger_warning','music_late_game','music_endgame','boss_death']),
 ('essence','scripts/Progression/EssenceTracker.cs',['essence_gain']),
 ('run_music','scripts/Infrastructure/AudioManager.cs',['music_exploration','music_combat','music_hub','music_death','ambient_details']),
 ('weapon_pickup','scripts/Combat/WeaponPickup.cs',['weapon_equip','weapon_drop','interaction_unavailable'])]]

for id,label in [('frozen_relic','Vestige figé / rareté'),('triptych','Triptyque / perte des choix'),('oblivion_pact','Pacte / sacrifice'),('threatened_loot','Butin menacé')]:
    effect('future_'+id,'futur',label,'Brief différé jusqu’à validation et implémentation.','doc/plans/13-butin.md',scope='plan 13 non implémenté',decision=True)
for id,label in [('collapse','Effondrement'),('run_echo','Écho de dernière run'),('self_forgetting','Oubli de soi'),('remembering_place','Lieu qui se souvient'),('double','Double'),('mirage','Mirage')]:
    effect('future_'+id,'futur',label,'Brief différé jusqu’à validation et implémentation.','doc/plans/14-anomalies-du-monde.md',scope='plan 14 non implémenté',decision=True)
for id,keys in [('harvest','sfx_recolte_hache,sfx_recolte_pioche,sfx_recolte_obtenu'),('craft','sfx_craft_termine,sfx_craft_impossible'),('construction','sfx_structure_pose,sfx_structure_impossible'),('hearth','sfx_foyer_crepitement,sfx_foyer_aura,sfx_foyer_upgrade'),('day_night','sfx_monde_crepuscule,sfx_monde_aube')]:
    effect('legacy_'+id,'héritage V1','Ancien système '+id,'Exclu : ne pas réintroduire le système.',AUDIO,keys=keys,state='Fichiers historiques enregistrés, système supprimé.',scope='héritage V1')

# Exceptions constatées : enregistrer une clé ne prouve pas qu'elle est appelée.
for e in effects:
    if e['effect_id'] == 'world_reveal':
        e['current_state'] = 'Fichier enregistré ; OnZoneDiscovered est vide, aucune lecture de la clé relevée.'
    if e['effect_id'] == 'dissolution':
        e['references'] += [ref('scripts/Events/RunEvents/FallenRelicEvent.cs', 'private void Land')]
    if e['effect_id'] == 'danger_warning':
        e['references'] += [ref('scripts/Events/RunEventDirector.cs', 'sfx_danger_building')]
    if e['effect_id'] in ('pounce_windup', 'pounce_leap'):
        e['references'] += [ref('data/enemies/charognard.json', '"abilities"')]
    if e['effect_id'] in ('omen_cast', 'omen_impact'):
        e['references'] += [ref('data/enemies/presage.json', '"abilities"')]

# Conserver uniquement les champs de suivi éditoriaux ; les mappings restent issus du dépôt.
old_path=DEST/'catalogue.json'
if old_path.exists():
    old={e['effect_id']:e for e in json.loads(old_path.read_text())['effects']}
    for e in effects:
        if e['effect_id'] in old:
            for field in ['candidates','raphael_choice','review_history','integration','validation','coverage_status']:
                if field in old[e['effect_id']]:
                    e[field]=old[e['effect_id']][field]

# Le lot de candidats est la source des métadonnées et des médias ; le registre
# ne confond pas proposition documentaire, décision de Raphaël et intégration.
candidate_map = {}
for candidate_path in sorted(DEST.glob('lot-*/candidates.json')):
    batch = candidate_path.parent.name
    candidate_groups = json.loads(candidate_path.read_text())['effects']
    group_ids = {g['id'] for g in candidate_groups}
    assert len(group_ids) == len(candidate_groups), f'Doublons de besoins dans {batch}'
    if batch == 'lot-a':
        assert group_ids == set(FIRST), 'Lot A incomplet ou identifiants inattendus'
    if batch == 'lot-a2':
        assert group_ids == set(SECOND), 'Lot A2 incomplet ou identifiants inattendus'
    for group in candidate_groups:
        candidates = group['candidates']
        assert len({c['id'] for c in candidates}) == len(candidates)
        for c in candidates:
            for key in ['author','source_url','download_url','license','license_evidence','license_verified_at','attribution']:
                assert c.get(key), (c['id'],key)
            for key in ['original_path','preview_path','license_evidence']:
                assert (candidate_path.parent/c[key]).is_file(), (c['id'], key)
        candidate_map.setdefault(group['id'], []).extend(
            {'candidate_id':c['id'], 'batch':batch,
             'metadata_file':f'doc/audio/{batch}/candidates.json',
             'preview_file':f'doc/audio/{batch}/'+c['preview_path']} for c in candidates)
for e in effects:
    if e['effect_id'] not in candidate_map:
        continue
    e['candidates'] = candidate_map[e['effect_id']]
    assert len({c['candidate_id'] for c in e['candidates']}) == len(e['candidates']), 'ID candidat partagé entre lots'
    # Les retours explicites ne redeviennent pas « prêts » par simple présence
    # des propositions déjà refusées, ni à la réception d'un nouveau panier.
    if e['coverage_status'] in ('à rechercher','sans résultat','candidats prêts'):
        e['coverage_status'] = 'candidats prêts' if len(e['candidates']) >= 3 else 'à rechercher'

ids={e['effect_id'] for e in effects}
assert len(ids)==len(effects)
assert set(candidate_map) <= ids, 'Candidats pour un besoin absent du catalogue'
for group,items in mappings.items():
    assert len({x['id'] for x in items}) == len(items),group
    for x in items:
        assert set(x['effect_ids']) <= ids,(group,x)

# Tous les choix sont validés avant la moindre écriture, y compris hors périmètre.
selected = {e['effect_id']: validate_choice(e) for e in effects}
current=[e for e in effects if e['scope']=='actuel']
counts={'current_needs':len(current),'first_batch_needs':len(FIRST),'candidate_proposals':sum(len(e['candidates']) for e in current),'needs_with_candidates':sum(bool(e['candidates']) for e in current),'chosen':sum(selected[e['effect_id']] for e in current),'integrated':sum(e['integration']['status']=='intégré' for e in current),'validated':sum(e['validation']['status']=='recetté' for e in current),'coverage_status':dict(sorted(collections.Counter(e['coverage_status'] for e in current).items())),'excluded_needs':len(effects)-len(current),'entities_by_group':{k:len(v) for k,v in mappings.items()}}
result={'schema_version':1,'generated_from':'Dépôt courant ; python3 tools/audio/build_catalogue.py ; références recalculées à chaque génération.','first_batch_effect_ids':FIRST,'second_batch_effect_ids':SECOND,'raphael_choice_schema':{'unset':None,'object':{'decision':'pending | candidate | none | silence','candidate_id':'ID présent dans candidates si decision=candidate ; null sinon','notes':'Texte facultatif'},'chosen_count':'Uniquement decision=candidate avec ID valide ; refus, silence et attente exclus.','orphan_policy':'La génération échoue avant toute écriture si un candidat choisi a disparu.'},'limitations':['Audit statique : présence dans les données/code ne prouve ni accessibilité en run, ni qualité sonore.','Besoins proposés = effets utiles à rechercher ou arbitrer ; aucun silence décidé pour un manque.','Durées et variantes sont des briefs proposés, pas des propriétés mesurées.','La Stratégie V2 §21 exclut explicitement les idle/alertes répétitifs ennemis ; leurs fichiers ne créent pas de nouveaux besoins.','Candidats, choix et intégration sont trois étapes distinctes. Les anciens fichiers ne sont pas des candidats validés.','Plans 13/14 et héritage V1 exclus du dénominateur actuel.','L’inventaire 15-audio-inventaire.md conserve les 79 fichiers historiques et tous les sites d’appels ; ce catalogue compte les besoins, pas les assets.'],'counts':counts,'effects':effects,'mappings':mappings}
text=json.dumps(result,ensure_ascii=False,indent=2)+'\n'
lines=['# Couverture audio — VESTIGES','', 'Catalogue exhaustif **des correspondances des données et systèmes repérés**, pas validation artistique ou recette en jeu. Les besoins supplémentaires restent à arbitrer. Aucun son n’est retenu automatiquement.','', '[Écouter le lot A](lot-a/index.html) · [Écouter le lot A2](lot-a2/index.html) · [Retours A](lot-a/RETOURS.md) · [Retours A2](lot-a2/RETOURS.md) · [Catalogue JSON](catalogue.json) · [Plan audio](../plans/15-audio.md) · [Inventaire des fichiers et appels](../plans/15-audio-inventaire.md)','',f"**{counts['current_needs']} besoins actuels**, {counts['needs_with_candidates']} avec candidats ({counts['candidate_proposals']} propositions), {counts['chosen']} choisis, {counts['integrated']} intégrés, {counts['validated']} recettés. {counts['excluded_needs']} entrées hors dénominateur (plans futurs, héritage V1, anciens mappings météo).",'', '## Statuts','', '| Statut | Besoins actuels |','|---|---:|']
for k,v in counts['coverage_status'].items(): lines.append(f'| {k} | {v} |')
chosen_ids = [e['raphael_choice']['candidate_id'] for e in current if selected[e['effect_id']]]
rework_ids = [e['effect_id'] for e in current if e['coverage_status'] == 'à retravailler']
kept_ids = [e['effect_id'] for e in current if e['coverage_status'] == 'actuel conservé']
lines += ['', '## Retours et nouvelle recherche', '',
          'Candidats retenus : ' + ', '.join('`'+x+'`' for x in chosen_ids) + '.', '',
          'Besoins à retravailler : ' + ', '.join('`'+x+'`' for x in rework_ids) + '.', '',
          'Sons actuels explicitement conservés : ' + (', '.join('`'+x+'`' for x in kept_ids) or 'aucun') + '.', '',
          'Les décisions et notes exportées restent intactes dans `raphael_choice` et `review_history`. '
          'Le statut « actuel conservé » consigne une instruction explicite dans les notes sans fabriquer un candidat choisi. '
          'Les préparations spécifiques figurent dans `integration.preparation` ; elles ne valent pas intégration ni recette en jeu.', '',
          'Lot A2 : ' + ', '.join('`'+x+'`' for x in SECOND) + '.']
lines+=['','## Premier panier','',', '.join('`'+x+'`' for x in FIRST)+'.','', '## Registre par besoin','', '| ID | Famille | Déclencheur | État actuel | Suivi |','|---|---|---|---|---|']
for e in effects:
    lines.append(f"| `{e['effect_id']}` | {e['family']} | {e['trigger']} | {e['current_state']} | {e['coverage_status']} |")
lines+=['','## Correspondances complètes','', 'Chaque ligne référence un effet partagé ou propre proposé. La liste ne demande pas de déclencher tous ces sons simultanément. Les références précises, mécaniques et réserves figurent dans le JSON.']
for group,items in mappings.items():
    lines+=['',f'### {group} — {len(items)}','', '| Entrée | Effets associés |','|---|---|']
    for x in items:lines.append(f"| `{x['id']}` | {', '.join('`'+i+'`' for i in x['effect_ids'])} |")
lines+=['','## Preuves et limites','']+['- '+x for x in result['limitations']]+['','- Armes : 24 définitions, 19 familles proposées ; impacts partagés. Les effets de statut ne créent pas un son à chaque tick.','- Ennemis : 16 définitions, 3 variantes et 5 affixes. Les 2 capacités data-driven ont 4 clés appelées mais non enregistrées ; `rare_fragment` constitue la cinquième clé manquante.','- Indicible utilise une classe dédiée : ne pas lui attribuer à tort l’impact générique de `Enemy.TakeDamage`.','- Biomes : 5 définitions. L’ambiance forêt est forcée aujourd’hui ; béton/eau ont leurs pas, herbe et forêt partagent un fichier. Bois/gravier restent à arbitrer comme surfaces.','- Interface : écrans, widgets et outils techniques sont distingués dans le JSON ; une association de contexte ne prouve pas un appel depuis le widget.','- Fusions : les cinq données sont suivies, mais `cable_whip` n’est pas une arme actuelle et `kill_restore_day` est historique ; cela ne valide pas leur accessibilité.','- Les sons ennemis historiques non utilisés ne sont pas ajoutés comme idle/alertes ; la direction V2 les exclut. Aucun nouveau silence artistique n’est décidé.','', '## Format des choix','', '`raphael_choice` vaut `null` tant que le retour n’est pas consigné. Sinon : `{"decision":"candidate","candidate_id":"enemy_hit_a","notes":"retour de Raphaël"}`. Les autres décisions sont `pending` (attente), `none` (chercher autre chose) et `silence` ; leur `candidate_id` vaut `null`. `notes` est facultatif. Le format reprend celui des décisions exportées par la page d’écoute, à reporter explicitement dans le catalogue. Aucun import des choix navigateur n’est automatique.','', 'Seuls les candidats effectivement sélectionnés sont comptés comme choisis. Un refus ou un silence ne compte pas comme son retenu. Si un candidat choisi disparaît lors de la synchronisation, la génération échoue explicitement avant toute écriture ; rétablir ce candidat ou faire réviser le choix par Raphaël.','', '## Régénération et contrôle','', '`python3 tools/audio/build_catalogue.py` régénère ces deux fichiers en préservant choix et recette. `python3 tools/audio/build_catalogue.py --check` compare les sorties et contrôle les identifiants/liaisons. Les candidats de tous les lots sont agrégés depuis `lot-*/candidates.json`, en conservant notamment ceux du lot A, avec contrôle des médias et preuves de licence présents (sans valider leur qualité artistique). Les catégories de données sont parcourues intégralement ; une nouvelle arme non classée ou une nouvelle interface non mappée fait échouer la génération.','']
outputs={'catalogue.json':text,'COUVERTURE.md':'\n'.join(lines)}
check=argparse.ArgumentParser();check.add_argument('--check',action='store_true');args=check.parse_args()
for name,content in outputs.items():
    path=DEST/name
    if args.check:
        assert path.exists() and path.read_text()==content, f'{name} doit être régénéré'
    else:
        path.write_text(content)
print(json.dumps(counts,ensure_ascii=False))

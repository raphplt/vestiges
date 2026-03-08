# VESTIGES — Audios Supplémentaires Recommandés

> **Contexte :** Lore du jeu = monde d'oubli progressif. Lumière = mémoire. Créatures = remplissage du vide.
> **Statut :** Propositions basées sur la narrative et l'architecture de gameplay

---

## Musiques additionnelles (Suno Pro)

| Type | Vision | Fichier |
|------|--------|---------|
| Menu principal — attente | Interface flottante, patient, pas pressant. Le joueur lit ses options. | `assets/audio/musique/mus_menu_principal.ogg` |
| Game Over — défaite | Pas de tragédie épique. Juste... l'absence. Proche de "Mort" mais moins définitif. | `assets/audio/musique/mus_game_over.ogg` |
| Victoire / Fin run | Moment rare où le joueur a survécu jusqu'à l'aube finale. Bittersweet, léger soulagement. | `assets/audio/musique/mus_victoire.ogg` |
| Boss alerté | Intensité soudaine. Quelque chose d'ancien se réveille. Le monde change. | `assets/audio/musique/mus_boss_alerté.ogg` |

**Prompts Suno suggérés :**

```
Menu Principal:
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, glacial dissonant sustains, video game OST. 
Waiting in an interface, floating between worlds. No urgency. Patient breath. One minimal piano tone with extreme reverb, 
one barely present synth pad, as if listening. Empty space as the music itself, 4-5 min, seamless loop possibility.

Game Over:
abstract ambient drone, minimal textural sound design, no melody, video game OST. 
The character's consciousness detaches. Not sad, not dramatic — just separation. One dying tone fades in slow motion, 
a final echo of something, then silence becomes the music. Bittersweet, 2 min, dissolves into void.

Victoire / Fin Run:
abstract dark ambient, minimal textural, very slow, video game OST. 
The world stabilizes. A long quiet victory — not triumph, but rest. One clear piano note sustained impossibly long, 
barely any overtones, almost transparency. The absence of chaos is the reward. 3-4 min ending in silence.

Boss Alerté:
abstract dark ambient drone, textural, dissonant, video game OST. 
Something ancient wakes at the edge of reality. The world's memory fractures. Pressure builds. 
Bass rumble climbing, scattered dissonant tones, sense of presence, of wrongness arriving. No percussion, building dread, 2-3 min.
```

---

## Actions — Interface & Interactions

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Menu — clic | `soft subtle touch tone, like a very light select beacon, warm and responsive, 0.15 seconds` | `assets/audio/sfx/ui/sfx_menu_clic.wav` |
| Menu — survol option | `tiny ascending tone, almost at the edge of perception, 0.1 seconds` | `assets/audio/sfx/ui/sfx_menu_survol.wav` |
| Menu — confirmer sélection | `two tones in harmony, brief and affirming, 0.3 seconds` | `assets/audio/sfx/ui/sfx_menu_confirmer.wav` |
| Inventaire — ouverture | `soft swish, like pages opening, brief and smooth, 0.4 seconds` | `assets/audio/sfx/ui/sfx_inventaire_ouvrir.wav` |
| Inventaire — fermeture | `soft swish reverse, like pages closing, 0.4 seconds` | `assets/audio/sfx/ui/sfx_inventaire_fermer.wav` |
| Item sélectionné | `tiny resonant chime, delicate, 0.2 seconds` | `assets/audio/sfx/ui/sfx_item_selectionne.wav` |
| Arme changée / équipée | `metallic click-clink, solid and confident, 0.4 seconds` | `assets/audio/sfx/ui/sfx_arme_equipee.wav` |
| Potion / consommable utilisé | `light swallow-like sound with subtle whoosh, warm sensation, 0.5 seconds` | `assets/audio/sfx/gameplay/sfx_potion_utilisee.wav` |

---

## Progression & Déverrouillages

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Perk refusé | `short dry click, slightly disappointed, 0.2 seconds` | `assets/audio/sfx/gameplay/sfx_perk_refuse.wav` |
| Malédiction acceptée | `dark single low tone with subtle distortion, ominous but inevitable, 0.6 seconds` | `assets/audio/sfx/gameplay/sfx_malediction_acceptee.wav` |
| Artefact trouvé | `ethereal ascending shimmer, like something transcendent, longer reverb tail, 2 seconds` | `assets/audio/sfx/gameplay/sfx_artefact_trouve.wav` |
| Progression bar — tick | `extremely subtle tiny click, barely conscious, 0.05 seconds, subtle` | `assets/audio/sfx/ui/sfx_progress_tick.wav` |

---

## Navigation & Monde

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Créature détectée / spotted | `sharp alert beep, sudden, like a warning system activating, 0.3 seconds` | `assets/audio/sfx/gameplay/sfx_creature_spotted.wav` |
| Aura de danger (mounting tension) | `low rising tone building pressure, like a distant alarm, 2 seconds crescendo` | `assets/audio/sfx/gameplay/sfx_danger_building.wav` |
| Projétile joueur — charge/prêt | `subtle electric hum building, weapon powering ready, 1 second` | `assets/audio/sfx/combat/sfx_projectile_charge.wav` |
| Projétile joueur — tire | `sharp release with twang, like a bowstring or sling, 0.3 seconds` | `assets/audio/sfx/combat/sfx_projectile_tire.wav` |
| Explosion / zone damage | `heavy bass thump with scattered debris clink, brief and powerful, 0.6 seconds` | `assets/audio/sfx/combat/sfx_explosion.wav` |
| Coffre — ouverture | `wooden lock mechanism giving way, slight creak and hinges moving, satisfying, 0.8 seconds` | `assets/audio/sfx/gameplay/sfx_coffre_ouverture.wav` |
| Coffre — vide / déçu | `sad muffled thud, like empty wood echo, 0.4 seconds` | `assets/audio/sfx/gameplay/sfx_coffre_vide.wav` |

---

## Dégâts & Système de santé

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Dégât critique reçu | `hard impact with sharp crack, more visceral than normal hit, 0.4 seconds` | `assets/audio/sfx/combat/sfx_degat_critique_recu.wav` |
| Santé basse — alerte (loop) | `subtle pulsing low tone, heartbeat-like and anxious, loopable 2 seconds` | `assets/audio/sfx/gameplay/sfx_sante_basse.wav` |
| Mort du joueur (stinger, court) | `electronic fade-out with brief reverb, less dramatic than "Mort", 1 second` | `assets/audio/sfx/gameplay/sfx_mort_joueur.wav` |
| Résurrection / second chance | `magical awakening tone, like a blessing, ascending and clear, 1.5 seconds` | `assets/audio/sfx/gameplay/sfx_resurrection.wav` |

---

## Construction & Foyer avancé

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Structure impossibilité placement | `soft buzzer or error tone, negative feedback, 0.2 seconds` | `assets/audio/sfx/gameplay/sfx_structure_impossible.wav` |
| Structure endommagée — alerte | `brief warning alert, not catastrophic but concerning, 0.5 seconds` | `assets/audio/sfx/gameplay/sfx_structure_endommagee.wav` |
| Foyer dégâts critiques | `low heavy impact, alarm tone, the safe zone is threatened, 1 second` | `assets/audio/sfx/foyer/sfx_foyer_degats.wav` |
| Foyer — purification (upgrade final) | `grand swelling tone with multiple layers, triumphant in a quiet way, 3 seconds` | `assets/audio/sfx/foyer/sfx_foyer_purification.wav` |

---

## Événements & Atmosphère dynamique

| Ambiance | Prompt ElevenLabs | Fichier |
|----------|-------------------|---------|
| Tonnerre — distance (loop) | `distant rumble rolling through sky, occasionally with light crack, loopable 6 seconds` | `assets/audio/sfx/ambiance/sfx_tonnerre_lointain.wav` |
| Pluie légère (loop) | `soft rain pattering on surfaces, peaceful and gentle, loopable 8 seconds` | `assets/audio/sfx/ambiance/sfx_pluie_legere.wav` |
| Brouillard / occultation (loop) | `deeply muffled and dampened ambient sounds, like being inside fog, loopable` | `assets/audio/sfx/ambiance/sfx_brouillard.wav` |
| Créatures nocturnes — ambiance (loop) | `distant inhuman calls and unsettling animal-like sounds from far away, loopable 8 seconds` | `assets/audio/sfx/ambiance/sfx_creatures_nocturnes.wav` |
| Catastrophe / omen warning | `building dread, low frequencies with subtle discord, something very wrong is coming, 3 seconds` | `assets/audio/sfx/gameplay/sfx_catastrophe_avertissement.wav` |

---

## Résumé

**Total nouveau :** ~35 sons supplémentaires

| Catégorie | Count | Notes |
|-----------|-------|-------|
| Musiques | 4 | Menu, Game Over, Victoire, Boss |
| UI & Inventaire | 8 | Clics, survols, confirmations |
| Progression | 4 | Perks, artefacts |
| Navigation & Combat | 7 | Spotting, projectiles, coffres |
| Santé | 4 | Dégâts, mort, résurrection |
| Construction | 4 | Structures, Foyer |
| Ambiance | 5 | Météo, créatures, événements |
| **TOTAL** | **36** | |

**Priorité recommandée :**
1. **Tier 1 (essentiels)** : Musiques (4), Menu UI (3), Foyer purification, Creature spotted, Coffres
2. **Tier 2 (gameplay)** : Projection bar, Santé basse, Dégâts critiques, Artefacts
3. **Tier 3 (ambiance)** : Tonnerre, pluie, brouillard, créatures nocturnes


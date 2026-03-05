# VESTIGES — Guide de Production Audio

> **Référence :** Bible Artistique & Narrative section 8
> **Budget total :** ~10$ (1 mois Suno Pro)
> **Statut :** Phase 7, Lot 7.4

---

## Stratégie

| Catégorie | Source | Pourquoi |
|-----------|--------|----------|
| Musique (8 tracks) | Suno Pro | Le style "piano désaccordé + field recordings" n'existe pas en libre de droits |
| SFX abstraits (13) | ElevenLabs Free | Sons impossibles à trouver : Foyer, dissolution, créatures |
| SFX concrets (15) | Freesound.org CC0 | Sons du quotidien que tu transformes dans Audacity |
| Traitement audio | Audacity (gratuit) | Transformer sons quotidiens en sons de créatures |

**Droits commerciaux Suno :** Les tracks générées pendant un abonnement Pro conservent leurs droits commerciaux même après annulation. 1 mois suffit. Vérifier les CGU à jour sur suno.com avant lancement EA.

---

## OUTIL 1 — Suno Pro (10$/mois)

**Paramétrage :** Mode **Simple**. Cocher **Instrumental**. Weirdness à **80%** minimum.

**Préfixe commun à tous les prompts :**
```
abstract dark ambient, experimental drone, no melody no musical phrases no hooks no resolution, textural sound design not composition, glacial pace, dissonant sustained tones only,
```

**Exclude styles (champ More Options) :** `melody, melodic, piano piece, composition, song, musical phrases, arpeggios`

---

### Track 1 — Jour / Exploration

> *Le joueur explore des ruines envahies par la végétation. Beau mais fragile. Un monde en train de s'effacer.*
> **Fichier :** `assets/audio/musique/mus_jour_exploration.ogg`

```
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, glacial dissonant sustains, video game OST. Explore ruins reclaimed by nature: haunted beauty, fragile wonder. One detuned piano note slowly decays, one barely touched guitar string, distant wind through trees and far birds. More silence than sound, no drums/percussion/bass, 4 min.
```

---

### Track 2 — Jour / Combat

> *Le même monde beau, mais des créatures surgissent. La beauté ne disparaît pas — elle devient dangereuse.*
> **Fichier :** `assets/audio/musique/mus_jour_combat.ogg`

```
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, glacial dissonant sustains, video game OST. Same haunted world turning dangerous: unstable detuned piano tone, isolated metallic hits with no rhythm, low resonant bass like earth vibration, tension without pulse, no melody only rising pressure, beauty rotting underneath, 3 min.
```

---

### Track 3 — Crépuscule

> *La transition. Les couleurs s'évaporent. La nuit vient. Quelque chose d'ancien se réveille.*
> **Fichier :** `assets/audio/musique/mus_crepuscule.ogg`

```
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, glacial dissonant sustains, video game OST. Twilight transition: the world starts forgetting itself. One piano tone fragments into dissonance, a very low drone slowly appears, distant metallic decay, something ancient and wrong wakes below, warmth dissolves, quiet but deeply wrong, 2 min.
```

---

### Track 4 — Nuit / Premières vagues

> *La nuit tombe pour de vrai. Le noir est absolu. Les créatures approchent.*
> **Fichier :** `assets/audio/musique/mus_nuit_vagues.ogg`

```
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, video game OST. Surviving in absolute darkness: dry isolated metallic impacts with no rhythm, deep bass impacts like massive breathing, cold string scrapes, buried subsonic heartbeat, raw physical threat, nothing resolved, nothing safe, 3 min.
```

---

### Track 5 — Nuit / Chaos

> *Le pic de la nuit. Les vagues submergent. Adrénaline de presque mourir.*
> **Fichier :** `assets/audio/musique/mus_nuit_chaos.ogg`

```
abstract dark ambient experimental drone, no melody/hooks, textural not compositional, video game OST. Night peak chaos: overwhelming survival in a dissolving world, all textures collapsing into crushing density, heavy low-end impacts, saturated physically vibrating drones, aggressive dissonant string scrapes, distant unintelligible synthetic voices, no melody only chaos and raw power, relentless, 3 min.
```

---

### Track 6 — Aube

> *Les créatures reculent. Le monde se souvient de lui-même. Le silence comme récompense.*
> **Fichier :** `assets/audio/musique/mus_aube.ogg`

```
abstract ambient, no melody no phrases, textural sound design, video game OST. Dawn after survival: the world remembers itself. One detuned piano tone hangs in vast silence, one sustained note slowly clarifies, long reverb fades into nothing, almost no sound; absence of threat is the music. Bittersweet, not triumphant, 90 sec ending in silence.
```

---

### Track 7 — Hub

> *Entre deux mondes. La conscience pure du joueur, sans monde autour. Flottement.*
> **Fichier :** `assets/audio/musique/mus_hub.ogg`

```
abstract dark ambient experimental drone, no melody/hooks/resolution, textural not compositional, glacial pace, video game OST. Hub state: floating inside pure consciousness without a world. Synthetic pads with extreme reverb suggest infinite empty space, one piano tone with extreme tape delay, buried reversed textures, weightless and anchorless, no drums/pulse/rhythm, seamless 4 min.
```

---

### Track 8 — Mort (stinger, pas un loop)

> *Le monde se défait. Pas de tragédie — juste l'absence.*
> **Fichier :** `assets/audio/musique/mus_mort.ogg`

```
abstract dark ambient experimental drone, no melody, textural dissolution, video game OST. Death stinger: the world comes apart. One piano tone slows and distorts like a dying tape machine, then falls into silence; sound forgets how to exist. 30 sec ending in complete void, not sad or dramatic, only erasure, the sound of being forgotten.
```

---

## OUTIL 2 — ElevenLabs Sound Effects (gratuit, 10 000 crédits/mois)

Accès via elevenlabs.io > Sound Effects. Droits commerciaux inclus en free. Tous les SFX du jeu sont générés ici.

---

### Pas du joueur

| Surface | Prompt ElevenLabs | Fichier |
|---------|-------------------|---------|
| Béton | `single dry footstep on cracked concrete, worn boot sole, no reverb, realistic, 0.3 seconds` | `assets/audio/sfx/joueur/pas/sfx_pas_beton.wav` |
| Herbe | `single footstep on dense wet grass, soft muffled crunch, outdoor, 0.3 seconds` | `assets/audio/sfx/joueur/pas/sfx_pas_herbe.wav` |
| Eau peu profonde | `single footstep splashing in shallow water, 3-4 cm deep, light splash and drip, 0.4 seconds` | `assets/audio/sfx/joueur/pas/sfx_pas_eau.wav` |
| Plancher bois | `single footstep on old wooden floor, slight creak, hollow resonance beneath, 0.3 seconds` | `assets/audio/sfx/joueur/pas/sfx_pas_bois.wav` |
| Gravier | `single footstep on loose gravel and small stones, dry scrape and settle, 0.3 seconds` | `assets/audio/sfx/joueur/pas/sfx_pas_gravier.wav` |

---

### Actions — Récolte

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Hache dans bois | `sharp axe blade hitting dense wood, solid thud with deep resonance, wood fibers splitting, single impact, 0.4 seconds` | `assets/audio/sfx/gameplay/sfx_recolte_hache.wav` |
| Pioche sur pierre | `metal pickaxe striking hard stone, sharp metallic clang with stone chip scatter, single strike, 0.5 seconds` | `assets/audio/sfx/gameplay/sfx_recolte_pioche.wav` |
| Récolte terminée — ressource obtenue | `brief satisfying rustle and clink, like picking up a handful of small objects, short and clean, 0.5 seconds` | `assets/audio/sfx/gameplay/sfx_recolte_obtenu.wav` |

---

### Actions — Craft et construction

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Craft terminé | `solid mechanical click-clunk of two heavy objects locking together perfectly, short definitive completion sound, 0.6 seconds` | `assets/audio/sfx/gameplay/sfx_craft_termine.wav` |
| Placement structure | `dense thud of a heavy wooden or stone object being set firmly on the ground, grounded and final, 0.5 seconds` | `assets/audio/sfx/gameplay/sfx_structure_pose.wav` |
| Craft impossible — ressources manquantes | `short dry thunk, dull and unrewarding, low pitch, 0.2 seconds` | `assets/audio/sfx/gameplay/sfx_craft_impossible.wav` |

---

### Actions — Combat

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Hit ennemi (impact générique) | `sharp blunt impact on dense organic matter, brief energy burst, no blood, dry and physical, 0.2 seconds` | `assets/audio/sfx/combat/sfx_hit_ennemi.wav` |
| Hit critique | `same blunt impact but harder, with a brief cracking resonance, 0.3 seconds` | `assets/audio/sfx/combat/sfx_hit_critique.wav` |
| Hit sur joueur | `heavy blunt strike landing on body, low dull thud with brief pain resonance, 0.3 seconds` | `assets/audio/sfx/combat/sfx_hit_joueur.wav` |
| Projectile en vol | `fast low whistling rush of air, brief, 0.3 seconds` | `assets/audio/sfx/combat/sfx_projectile_vol.wav` |
| Projectile impact | `sharp crack on impact, brief, dry, 0.2 seconds` | `assets/audio/sfx/combat/sfx_projectile_impact.wav` |

---

### Progression

| Action | Prompt ElevenLabs | Fichier |
|--------|-------------------|---------|
| Level up | `ascending crystalline chime sequence, 3 tones rising, clear and resonant, like glass tones or bells, 1.5 seconds, hopeful and bright` | `assets/audio/sfx/gameplay/sfx_level_up.wav` |
| Orbes XP attirées (loop court) | `small cluster of delicate golden tones, warm soft chimes overlapping briefly, 0.8 seconds, loopable` | `assets/audio/sfx/gameplay/sfx_xp_orbe.wav` |
| Perk sélectionné | `single clear resonant tone with warm shimmer tail, like a tuning fork, 1 second` | `assets/audio/sfx/gameplay/sfx_perk_choix.wav` |
| Souvenir (lore) trouvé | `soft distant tone with long reverb, like a memory surfacing, ethereal and fragile, 1.5 seconds` | `assets/audio/sfx/gameplay/sfx_souvenir_trouve.wav` |

---

### Environnement et ambiance

| SFX | Prompt ElevenLabs | Fichier |
|-----|-------------------|---------|
| Vent ambiance — Forêt (loop) | `gentle wind moving through dense tree canopy, leaves rustling softly, occasional branch creak, peaceful but slightly unsettling, loopable 8 seconds` | `assets/audio/sfx/ambiance/sfx_ambiance_foret.wav` |
| Vent ambiance — Ruines (loop) | `wind whistling through gaps in concrete and broken metal structures, hollow and lonely, occasional distant metallic resonance, loopable 8 seconds` | `assets/audio/sfx/ambiance/sfx_ambiance_ruines.wav` |
| Vent ambiance — Marécages (loop) | `low wind over still water, soft wet reeds moving, distant water drip, oppressive stillness, loopable 8 seconds` | `assets/audio/sfx/ambiance/sfx_ambiance_marecages.wav` |
| Oiseaux lointains | `2-3 bird calls in a dense forest, far away, muffled by foliage, 3 seconds, natural and slightly eerie` | `assets/audio/sfx/ambiance/sfx_ambiance_oiseaux.wav` |
| Eau de marécage — bulle | `single slow bubble rising and popping at the surface of murky water, 0.5 seconds` | `assets/audio/sfx/ambiance/sfx_ambiance_bulle.wav` |

---

### Foyer

| SFX | Prompt ElevenLabs | Fichier |
|-----|-------------------|---------|
| Foyer — crépitement (loop) | `small contained fire crackling softly, tonal and almost musical, warm orange-gold quality, no smoke sound, loopable 6 seconds` | `assets/audio/sfx/foyer/sfx_foyer_crepitement.wav` |
| Foyer — aura zone safe (loop) | `very low barely audible warm subsonic hum, like a room-filling resonance, golden and safe, felt more than heard, loopable 8 seconds` | `assets/audio/sfx/foyer/sfx_foyer_aura.wav` |
| Foyer — intensification (upgrade) | `fire flaring up briefly, crackle intensifying then settling, brighter and warmer, 2 seconds` | `assets/audio/sfx/foyer/sfx_foyer_upgrade.wav` |

---

### Monde et effets visuels

| SFX | Prompt ElevenLabs | Fichier |
|-----|-------------------|---------|
| Fog of war — tuile qui apparaît | `delicate crystalline shimmer with a tiny reverb tail, like something materializing from nothing, 0.4 seconds, subtle` | `assets/audio/sfx/gameplay/sfx_monde_tuile_apparait.wav` |
| Mort créature — dissolution | `brief soft hiss and scatter, like sand or dust dispersing quickly, dark and quiet, 1 second` | `assets/audio/sfx/gameplay/sfx_monde_dissolution.wav` |
| Bord de map — dissolution ambiante (loop) | `very quiet white noise texture with subtle crackle, like reality fraying at the edges, unsettling and dry, loopable` | `assets/audio/sfx/gameplay/sfx_monde_bord_map.wav` |
| Transition crépuscule (stinger) | `low resonant tone swelling briefly then fading, like the air pressure changing, 3 seconds, ominous` | `assets/audio/sfx/gameplay/sfx_monde_crepuscule.wav` |
| Transition aube (stinger) | `single soft piano tone with long reverb, clear and clean, like light returning, 2 seconds` | `assets/audio/sfx/gameplay/sfx_monde_aube.wav` |

---

### Créatures

| Créature | Prompt ElevenLabs | Fichier |
|----------|-------------------|---------|
| Rôdeur — idle | `a low inhuman groan, like a human moan slowed and stretched, quiet and wrong, 1.5 seconds` | `assets/audio/sfx/creatures/sfx_rodeur_idle.wav` |
| Rôdeur — attaque | `a short guttural burst, wet and hollow, like a broken voice trying to shout, 0.5 seconds` | `assets/audio/sfx/creatures/sfx_rodeur_attaque.wav` |
| Charognard — idle | `rapid irregular clicking and wet chittering, like an insect but larger and wrong, 1 second` | `assets/audio/sfx/creatures/sfx_charognard_idle.wav` |
| Charognard — meute (loop) | `multiple overlapping wet clicking sounds, swarm-like, anxious and surrounding, loopable 3 seconds` | `assets/audio/sfx/creatures/sfx_charognard_meute.wav` |
| Sentinelle — activation | `mechanical click followed by a high-pitched electrical charge building, like a weapon powering up, 1.5 seconds` | `assets/audio/sfx/creatures/sfx_sentinelle_activation.wav` |
| Sentinelle — tir | `sharp electronic pulse release, like an electrical discharge, short and precise, 0.3 seconds` | `assets/audio/sfx/creatures/sfx_sentinelle_tir.wav` |
| Ombre — idle | `a distant sound like a child humming a single note, barely audible, pitch slightly wrong, 2 seconds` | `assets/audio/sfx/creatures/sfx_ombre_idle.wav` |
| Ombre — attaque | `a sudden sharp whisper-hiss, like breath exhaled too fast and too close, 0.3 seconds` | `assets/audio/sfx/creatures/sfx_ombre_attaque.wav` |
| Brute — pas | `extremely heavy impact on ground, stone and metal resonance, slow and massive, 0.6 seconds` | `assets/audio/sfx/creatures/sfx_brute_pas.wav` |
| Brute — charge | `massive low rumble building to a heavy stone-and-metal collision impact, 2 seconds, physically overwhelming` | `assets/audio/sfx/creatures/sfx_brute_charge.wav` |
| Tisseuse — idle | `a slow wet fibrous sound, like silk tearing in slow motion, unsettling and organic, 1.5 seconds` | `assets/audio/sfx/creatures/sfx_tisseuse_idle.wav` |
| Hurleur — cri | `a piercing sustained shriek, starts organic then becomes electronic and wrong, like a siren made of flesh, 2 seconds` | `assets/audio/sfx/creatures/sfx_hurleur_cri.wav` |
| Rampant — déplacement (loop) | `low subterranean rumble and scrape, like something heavy moving just below a surface, loopable 3 seconds` | `assets/audio/sfx/creatures/sfx_rampant_deplacement.wav` |
| Rampant — surgissement | `sudden burst of soil and stone, sharp explosive crack, 0.5 seconds` | `assets/audio/sfx/creatures/sfx_rampant_surgissement.wav` |
| L'Indicible — présence (loop) | `extremely low sub-bass drone, below 60hz, with distant unintelligible voices layered under, felt in the chest more than heard, deeply wrong, loopable 10 seconds` | `assets/audio/sfx/creatures/sfx_indicible_presence.wav` |

---

## Pipeline de production

```
1. Suno Pro (1 mois) → générer 8 tracks musique → exporter WAV
2. ElevenLabs → générer tous les SFX (pas, actions, créatures, ambiance)
3. Audacity → créer des loops seamless sur les musiques si besoin
4. Intégrer dans Godot par catégorie (AudioStreamPlayer + AudioBus)
```

---

## Intégration Godot (rappels)

- Musiques en **OGG Vorbis** (`.ogg`) — meilleur ratio taille/qualité, loop natif Godot
- SFX en **WAV** — latence minimale pour les sons de gameplay
- Loops seamless : activer `Loop` dans l'import Godot + vérifier les points de loop dans Audacity
- Musique adaptative (section 8.4 de la Bible) : `AudioStreamPlayer` par layer + contrôle des volumes via `EventBus` selon le nombre d'ennemis et les HP

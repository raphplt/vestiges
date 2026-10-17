# Plan 53 — Performance macOS, shaders du monde et paramètres vidéo

17 octobre 2026 · Demandé par Raphaël : « je serai d'avis de travailler sur ces quatre leviers !! Et en profiter d'ailleurs pour dans les menus du jeu pouvoir régler la résolution, les fps max, ect... »

## 1. Constat technique

Sur un PC Windows/Linux (Ryzen 7 5700X + RX 6950 XT), le banc de combat dense à 120 créatures tient **~150 à 170 FPS** en 1080p ([audit du 27 septembre](../audits/performance-2026-09-27/mesures.json)). Sur Mac Apple Silicon (M1 Pro 16 Go, M5 Pro 18 Go), le jeu tombait à **30–40 FPS** en plein écran Retina (~58,5 FPS en fenêtré 1080p avec 57 % des frames au-dessus de 16,67 ms).

Cet écart ne venait pas du matériel Apple Silicon (les scripts C# + la physique Jolt ne prennent que **1,2 à 1,5 ms** par frame sur M1 Pro, contre **15,54 ms** dans `render_and_rest`), mais de quatre causes cumulées :

1. **Résolution Retina × `canvas_items` (Fillrate ×3 à ×4)** : `allow_hidpi` est actif par défaut sous macOS et `stretch/mode="canvas_items"` rendait le jeu à la résolution native du framebuffer Retina (~3024×1964 à 3840×2160, soit 6 à 8,3 Mpx contre 2 Mpx en 1080p), alors que la caméra `zoom = Vector2(2, 2)` ne montre que 960×540 pixels monde.
2. **Coût ALU par fragment dans `world_grade.gdshaderinc`** : exécuté sur chaque pixel du sol, des routes et de tous les décors. `cloud_shadow` (2 octaves), `world_grime_at` (1 hash + 1 value noise) et `world_stains_at` (5 octaves de FBM + 1 value noise) totalisaient **8 évaluations de bruit 2D + 2 hashes par fragment dessiné**.
3. **Sondage des frontières dans `ground.gdshader` sur >90 % de la carte** : les 5 biomes ayant `"blend_terrains": true`, l'identifiant de mélange change à chaque variante de matière de tuile, si bien que `own_data.a > 0.5` était vrai sur la quasi-totalité de l'écran. Chaque fragment du sol exécutait jusqu'à **48 recherches de cellules (`6 anneaux × 8 directions`)** avec appels `cos`/`sin`, conversion isométrique `cell_of`, `texelFetch` dans `cell_map` et 5 évaluations de `value_noise`, avant même de vérifier si le bruit de lisière (`shape`) avançait du côté du voisin.
4. **Absence de réglages vidéo** : l'onglet **Paramètres › Graphismes** ne contenait que *Plein écran*, *Filtre daltonien* et *Langue* : aucun réglage de résolution, d'échelle de rendu (Retina), de V-Sync ni de FPS max.

---

## 2. Lots implémentés et vérifiés

- [x] **Lot V1 — Paramètres vidéo (Résolution, Échelle de rendu / Retina, FPS max, V-Sync)** :
  - Service [`DisplaySettings`](../../scripts/Infrastructure/DisplaySettings.cs) persisté dans `user://display_settings.cfg` et branché dans [`GameManager`](../../scripts/Core/GameManager.cs) ainsi que dans [`SettingsScreen`](../../scripts/UI/SettingsScreen.cs).
  - Réglages exposés : **Plein écran**, **Résolution (fenêtré)** (`1280×720`, `1600×900`, `1920×1080`, `2560×1440`), **Échelle de rendu** (`1080p (Opti.)` par défaut pour plafonner le fillrate Retina/4K, `Native (Retina)`, `720p (Perf.)`), **Synchronisation V-Sync** et **FPS max** (`Illimité`, `60`, `120`, `144`, `240`).
  - Lanceurs macOS ([`tools/lib/portable.sh`](../../tools/lib/portable.sh), [`tools/run_dev.sh`](../../tools/run_dev.sh)) mis à jour pour détecter automatiquement `~/Applications/Godot_mono.app/Contents/MacOS/Godot`.
  - Régression vérifiée dans [`MenuNavigationRegression.cs`](../../tools/tests/MenuNavigationRegression.cs) (`60/60 checks OK`).
- [x] **Lot V2 — Pré-calcul en texture des bruits de `world_grade.gdshaderinc` (Nuages, Grain, Taches)** :
  - Texture globale répétable `world_noise_lut` (`256×256` `Rgba8`, `filter_linear, repeat_enable`) générée une fois par processus dans [`BiomeAtmosphere.EnsureNoiseTexture()`](../../scripts/World/BiomeAtmosphere.cs) (et dès [`ShaderWarmup`](../../scripts/Infrastructure/ShaderWarmup.cs)) :
    - `R` : bruit de nuages bi-octave périodique ;
    - `G` : bruit de taches 3 octaves périodique ;
    - `B` : bruit de moisissure 2 octaves périodique ;
    - `A` : bruit de valeur de base (filtré en bilinéaire matériel).
  - Dans [`world_grade.gdshaderinc`](../../assets/shaders/world_grade.gdshaderinc), `cloud_shadow`, `world_grime_at` et `world_stains_at` lisent directement `world_noise_lut` et n'évaluent la rupture d'auréole (`rim`) que sur les pixels de tache (`inside > 0.0`).
- [x] **Lot V3 — Polarité des voisins et sondage mono-anneau dans `GroundMaterial` / `ground.gdshader`** :
  - Dans [`GroundMaterial.MarkBorderCells`](../../scripts/World/GroundMaterial.cs), le canal `A` de `cell_map` encode désormais la polarité des voisins présents dans le rayon maximal (`0` = aucun, `85` = supérieur seul, `170` = inférieur seul, `255` = les deux).
  - Dans [`ground.gdshader`](../../assets/shaders/ground.gdshader), `shape` et `raw_advance` sont évalués en premier (via `cloud_noise` matériel) : plus de 65 % des fragments de bordure sont écartés sans aucun sondage ni calcul de `warped`, et les fragments restants ne sondent qu'**un seul anneau (`target_ring`)** avec table constante `RING_DIRS[8]` (zéro `cos`/`sin`, au plus 8 lectures au lieu de 48).
- [x] **Lot V4 — Comparaison `gl_compatibility` vs Metal natif (`mobile`) et banc A/B sur Apple M1 Pro** :
  - Les 29 shaders de `ShaderWarmup` compilent sans erreur ni écart visuel sous `gl_compatibility` comme sous `mobile` (`--rendering-driver metal`).
  - Une fois les shaders V2/V3 optimisés, `gl_compatibility` atteint **124,8 FPS** en 1080p (chargement `8,4 s`) contre **118,8 FPS** en `mobile`/`metal` (chargement `13,4 s` dû à la compilation SPIR-V → MSL) : `gl_compatibility` est donc conservé par défaut.

---

## 3. Résultats mesurés sur Apple M1 Pro (`tools/benchmark_movement.sh`, seed `221092026`, 120 ennemis)

| Résolution | Variante | Avant (Baseline) | Après (Lots V2 + V3, `gl_compatibility`) | Après (`mobile` / Metal) | Gain FPS | Frames > 16,67 ms (Avant → Après) |
|---|---|---:|---:|---:|---:|---:|
| **1280×720** | Sans dash | 77,8 FPS (12,86 ms) | **162,9 FPS** (6,14 ms) | — | **+109 %** | 4,62 % → **0,08 %** |
| **1280×720** | Dash | 87,9 FPS (11,37 ms) | **164,2 FPS** (6,09 ms) | — | **+87 %** | 1,74 % → **0,12 %** |
| **1920×1080** | Sans dash | 58,5 FPS (17,10 ms) | **124,8 FPS** (8,01 ms) | 118,8 FPS (8,41 ms) | **+113 %** | 56,72 % → **0,16 %** |
| **1920×1080** | Dash | 58,4 FPS (17,11 ms) | **123,2 FPS** (8,12 ms) | — | **+111 %** | 57,81 % → **0,16 %** |

Décomposition de la frame 1080p (`frame_split_ms`, sans dash) :
- `render_and_rest` : **15,54 ms → 7,13 ms** (**−54 %**)
- `process_scripts` : `0,58 ms`
- `physics_scripts` + `physics_step` : `0,31 ms`
- En combinant le plafond d'échelle de rendu `1080p (Opti.)` sur écran Retina (lot V1) et l'optimisation des shaders du monde (lots V2 + V3), le jeu passe sur M1 Pro de **~35 FPS** en plein écran Retina à **~125 FPS** constants.

# Cadence du Hurleur — 1er octobre 2026

Suite de R3 : les runs naturelles seules ne permettaient pas d'isoler un réglage.
Le tir du Hurleur passe à un multiplicateur de cooldown de **2,5**, en données.
Son intervalle de base devient environ **3,15 s** (2,75 s + annonce de 0,4 s),
contre 1,50 s. L'agressivité continue à s'appliquer à la recharge. Les trois
autres tireurs, la durée/vitesse des projectiles, le cri et les pools d'apparition
restent identiques.

## Comparaison contrôlée

`tools/benchmark_projectile_cadence.sh <dossier>` : 90 s, six Hurleurs et deux
Cracheurs fixes, premiers délais déterministes, joueur invincible sur une
ellipse périodique. Capacités et projectiles du jeu. Pas de morts ni de build.
Le cri se résout et est compté, mais aucun EnemyPool ne crée ses renforts dans
ce banc, afin de garder la composition fixe. Le vrai appel des renforts est
couvert par les régressions existantes et les captures Main.

| Agressivité | Multiplicateur | Tirs Hurleurs | Tirs Cracheurs | Cris | Projectiles actifs moyens / pic |
|---|---:|---:|---:|---:|---:|
| 1 | 1 | 354 | 126 | 60 | 18,561 / 23 |
| 1 | 2,5 | 168 | 126 | 60 | 11,375 / 18 |
| 1,6 | 1 | 480 | 179 | 60 | 25,370 / 31 |
| 1,6 | 2,5 | 246 | 179 | 60 | 16,512 / 21 |

Le levier réduit de 52,5 % / 48,8 % les tirs du Hurleur et de 38,7 % / 34,9 %
les projectiles actifs, sans modifier les autres comptes. Ce cas extrême reste
occupé par au moins un tir pendant 99,56 % du temps : alléger la couche de tirs
ne signifie pas qu'un écran entier devient régulièrement vide de projectiles.
Le banc ne mesure pas l'esquive ou le plaisir du combat.

## Retour aux runs naturelles

Même assembly (`build.sha256`), 320 s, seeds 221092026/1002, Traqueur nomade,
Péril 0, sonde à 10 Hz. Référence forcée par `--howler-cooldown 1`, variante
avec le JSON livré ; `--nomad --measure-projectiles` dans les deux cas.

| Seed | Cooldown | Tirs visibles en crise : moyenne / P95 / pic | Foule visible moyenne (run) | Tireurs visibles moyens (crise) | Dégâts bruts/min (crise) |
|---|---:|---:|---:|---:|---:|
| 221092026 | ×1 | 8,468 / 43 / 71 | 20,71 | 9,67 | 1 950 |
| 221092026 | ×2,5 | 5,020 / 14 / 26 | 22,51 | 10,27 | 1 157 |
| 1002 | ×1 | 2,835 / 11 / 23 | 18,06 | 5,22 | 710 |
| 1002 | ×2,5 | 2,906 / 9 / 14 | 19,30 | 6,61 | 607 |

La foule reste présente et les pointes observées diminuent. La divergence des
builds/compositions interdit d'attribuer tous ces écarts au réglage : les
pourcentages causaux proviennent du banc fixe, pas de cette table. Les dégâts
sont les coups bruts sur le bot invincible, pas des PV effectivement perdus.
Données complètes : `natural.json`, `controlled.json`, CSV/journaux compressés.

## Vérifications et limites

- Build et smoke Hub 600 frames verts : zéro avertissement, zéro erreur C# ; 61 contrôles de capacités ennemies
  verts, dont la réinitialisation Hurleur → Cracheur d'une capacité poolée.
- Le premier essai de cette nouvelle régression comptait deux fois la physique
  après `Initialize` (5 tirs au lieu de 2). Le banc a été corrigé pour reprendre
  son pilotage manuel ; seconde passe entièrement verte.
- 24 captures en vraie Main, deux Hurleurs et un Cracheur, avant/après,
  ViewSonic écran 1 confirmé. Images inspectées et planche comparée :
  `/tmp/vestiges-r3b-capture-before`, `/tmp/vestiges-r3b-capture-after`,
  `/tmp/vestiges-r3b-comparison.png`. Visée, projectile et cri conservés.
- Une référence déjà obsolète à `choice_backdrop.gdshader` dans le préchargement
  provoque une erreur au boot Main, indépendante du tir ; correction courte
  prévue immédiatement après ce lot, sans toucher à la rotation du fond actuel.
- Recette humaine encore ouverte, notamment en espace étroit et en Résurgences
  tardives. Aucun audio édité ; sons existants déclenchés par les mêmes capacités.

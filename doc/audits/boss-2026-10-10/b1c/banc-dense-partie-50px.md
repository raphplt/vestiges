# Banc dense avec et sans partie de boss de 50 px — 10 octobre 2026 (plan 07 B1c)

`BENCH_REPEATS=1 BENCH_SECONDS=15 tools/benchmark_movement.sh`, quatre passes par variante, ordre alterné ; variante « avec » : `BENCH_EXTRA_ARGS="--boss-part 50"` (une partie de 50 px posée à 1 500 px du combat, qui relève `Enemy.LargestBodyRadius` de 14 à 50 px pour tous les tirs). Même code des deux côtés. Machine chargée (charge 3,9 à 7,1 sur 8 cœurs) : les FPS ne valent que comme ordre de grandeur, les deux variantes ont subi la même charge.

Médianes des quatre passes [min–max] :

| Cas | Variante | Scripts physiques (ms) | Scripts process (ms) | Image moyenne (ms) | Allocations (Mo) |
|---|---|---|---|---|---|
| 1280×720 | sans | 0,284 [0,275–0,309] | 0,894 | 7,16 [7,13–7,75] | 17,8 |
| 1280×720 | avec | 0,281 [0,271–0,300] | 0,892 | 7,21 [7,06–7,67] | 18,4 |
| 1280×720 dash | sans | 0,278 [0,269–0,322] | 0,896 | 7,16 [7,03–8,11] | 17,8 |
| 1280×720 dash | avec | 0,289 [0,284–0,319] | 0,907 | 7,22 [7,10–8,05] | 19,0 |
| 1920×1080 | sans | 0,408 [0,399–0,417] | 0,953 | 9,94 [9,91–10,07] | 17,4 |
| 1920×1080 | avec | 0,397 [0,395–0,405] | 0,943 | 9,92 [9,83–10,09] | 17,5 |
| 1920×1080 dash | sans | 0,406 [0,391–0,423] | 0,949 | 9,95 [9,80–10,40] | 17,4 |
| 1920×1080 dash | avec | 0,395 [0,386–0,436] | 0,943 | 9,92 [9,79–10,48] | 18,7 |

Aucun écart au-delà de la dispersion des passes : la marge élargie ne coûte rien de mesurable avec 120 créatures et l'arme de départ.

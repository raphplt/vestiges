# L'Indicible mesuré — 4 octobre 2026

Demandé par Raphaël après Q6b ([DECISIONS §64](../../plans/DECISIONS.md)) : « mesurer après Q6c » pourquoi l'Indicible ne se combat pas. Commit de base 3259df2e.

## Commande

```sh
godot-mono --headless --fixed-fps 60 --audio-driver Dummy --path . res://tools/tests/RunObservation.tscn -- \
    --dev --measure-indicible --seconds 20 --seed 7 --output <dossier>
```

Le boss est forcé sur le joueur, avec le build de la capture de fin de partie : arme de départ + arbalète, haches de lancer, fronde. Le bot invincible prend trois postures, 20 s de jeu chacune à 60 images par seconde :
- **cercle** : il tourne en rond, comme dans la capture ;
- **centre** : il reste au point d'apparition ;
- **dehors** : il se poste à 540 px du centre, hors de l'anneau des bords.

Journaux bruts : `seed-7.txt`, `seed-42.txt`.

## Résultats (seeds 7 et 42)

| Posture | PV du boss | Tirs | Visent le centre | Atteignent un bord | Vagues de tentacules | Coups reçus |
|---|---|---|---|---|---|---|
| cercle | 6 400 → 6 400 | 51 / 52 | 6 / 7 | **0** | 8 | **0** |
| centre | 6 400 → 6 400 | 249 | 10 | **0** | 8 | 8 / 12 |
| dehors (540 px) | 6 400 → 6 400 | 9 | 9 | **0** | 8 | 11 / 8 |

## Causes

1. **Ses zones touchables sont hors de portée.** Les quatre bords, seuls à recevoir les projectiles, sont à 310–390 px du centre (`spread` 350, épaisseur 40). Les armes à distance du build portent à 120–180 px (fronde 120, haches 140, arbalète 180). Le boss apparaît sur le joueur : le joueur est au centre de l'anneau, à plus de 130 px du bord le plus proche.
2. **Les armes visent son centre, où il n'y a rien à toucher.** Le ciblage prend la position du nœud, c'est-à-dire le centre de l'arène. Au centre, cette cible est à 0 px et les tirs partent dans la direction du regard. Hors de l'anneau, le centre est hors de portée et le joueur ne tire presque plus (9 tirs en 20 s, puis rien).
3. **Les autres attaques ne le voient pas.** Mêlée, orbite, cône et chaîne ne retiennent que les `Enemy`. L'Indicible est un `Node2D` à part, que seule l'`Area2D` de ses bords relie aux projectiles.
4. **Ses coups touchent un joueur immobile, jamais un joueur qui bouge.** Chaque tentacule vise la position du joueur à ±60 px et frappe 0,8 s plus tard, sur une largeur de 30 px. Immobile, le joueur prend environ un coup par vague (8 à 12 pour 8 vagues de 2 tentacules). En mouvement continu, aucun. La capture du 4 octobre (0 coup) venait du bot qui tournait en rond.

## Ce que la mesure ne dit pas

- Avec une arme de plus longue portée (au-delà de 310 px), un projectile parti vers le centre depuis l'extérieur de l'anneau croiserait un bord. Aucune arme du jeu n'a été essayée une à une.
- Le ressenti (lisibilité des annonces, visuel de rectangles sombres) n'est pas mesurable ainsi : à juger en jeu.

# Lot 6A — horloges et statuts à distance, 28 septembre 2026

Premier correctif autorisé après l’audit. Les résultats ci-dessous sont **reproduits isolément sur les vrais composants Godot**, avec une horloge pilotée ; ce ne sont pas des mesures de FPS. Seed `221092026`, Godot 4.7.2 Mono, build sans avertissement.

## Résultats

Le même banc compte 26 assertions : **13 échecs avant, zéro après**. [Avant](before.json), [après, lot 6A seul](after.json), [dans l’arbre partagé](integration.json), [provenance](validation.json).

| Scénario | Avant | Après |
|---|---|---|
| Ralentissement et désorientation de 2 s, après 10 s à 1 500 px | 2 s restantes pour chacun | Expirés, comme à 100 px |
| Régénération à distance, fixture à 10 000 PV max, départ à 5 000 | 5 000 PV contre 5 900 à proximité | 5 900 dans les deux cas |
| Recul au retour à proximité | Vecteur conservé au loin | Décroissance et expiration au loin |
| Mort par DOT | Suite du tick encore exécutée | Arrêt immédiat, aucun déplacement après mort |
| Effacement après 600 s, appels à 1 Hz | 15,4037 % contre 30,8032 % à 60 Hz | 30,8037 % à 1, 30, 60 et 144 Hz |
| Néant pendant 2 s, appels à 1 Hz | Deux pas de dégâts au lieu de quatre | Quatre impacts, 12 % des PV max comme à 2 Hz |
| Image bloquée 10 s | Un seul pas, reste perdu | Dette conservée, au plus quatre pas par image, résultat identique après rattrapage |
| Alternance de deltas 0,13 / 0,07 s | Reste perdu | Même intégrale que des pas réguliers |

La pause, la réutilisation des ennemis, les deltas réduits de moitié et les quatre transitions de phase successives sont également vérifiés. Le ralenti est testé en fournissant des deltas déjà réduits ; ce n’est pas un test de la mise en scène complète du hitstop.

## Contrat et limites

Les statuts utilisent le temps de simulation, même hors de l’IA complète. La régénération continue aussi après abandon de la poursuite. Au loin, pas de nouveau tirage aléatoire de désorientation, ni de déplacement physique pour le recul : seul son état expire. La cadence des DOT reste inchangée.

L’Effacement garde le reste temporel en double précision et rejoue ses pas logiques de 0,5 s, avec un budget de quatre pas par image. Chaque pas publie ses transitions et ses dégâts ; la texture finale est envoyée une seule fois. La dette subsiste après le budget et s’arrête sur une pause ou une mort. Les pas rattrapés utilisent la position et les multiplicateurs disponibles : aucun historique de trajet n’est reconstruit pendant une image non simulée. Après un long blocage, les dégâts dus peuvent donc arriver rapprochés sur plusieurs images.

Ce lot ne réduit pas le nombre de cellules visitées par pas (lot 6C). Le plafond de quatre borne les parcours par image, sans prouver qu’une très grande carte tiendra son budget CPU. Aucun gain de FPS revendiqué ; pas de chronométrage comparatif sous charge.

## Reproduction et validations

```bash
tools/test_temporal.sh /tmp/vestiges-temporal
tools/test_enemy_abilities.sh
tools/test_movement.sh
tools/smoke_test.sh
CAPTURE_EXTRA_ARGS="--nomad" tools/capture_run.sh /tmp/vestiges-temporal-capture 12 6 1280x720 221092026
```

Le script temporel isole les sauvegardes, compile, importe et écrit `result.json` et les journaux. Pour reproduire l’avant, prendre `66dd704f`, copier le banc `TemporalRegression.cs`, sa scène et son script de lancement, puis lancer le même script : code de sortie 1 attendu. La comparaison archivée a aussi repris les changements non commités présents au démarrage dans une copie isolée ; ils ne sont pas incorporés à ce lot. L’après isolé ne contient que les deux correctifs de production, sans les changements de capacités ennemies arrivés pendant la vérification.

- Build : zéro erreur, zéro avertissement. Capacités et déplacements : zéro échec. Smoke Hub : vert. Journaux bruts compressés conservés ici.
- Run rendue de 12 s : terminée sans erreur runtime ; [capture à 6 s](run-006s.png) ouverte et inspectée (terrain, joueur et HUD visibles). Elle vérifie le chargement réel, pas l’Effacement de fin de run.
- **Limite préexistante reproduite** : `tools/test_movement.sh --run-integration` expire après 1 500 frames d’attente de Main, avant comme après (`movement-reference.log.gz`, `movement.log.gz`). Ce contrôle n’est pas déclaré vert. Le scénario temporel dédié et la capture réelle passent.
- L’ancien banc d’audit compte désormais les publications de mémoire au lieu de supposer que le compteur privé revient exactement à zéro. Dans son scénario stationnaire de dix minutes, chaque pas fait encore varier la mémoire ; cette adaptation accepte plusieurs pas par image et le reste conservé.

Le suivi de livraison est au [plan 10 §11](../../../plans/10-terrain-et-tiles.md#11-audit-de-performances-du-27-septembre--lots). Prochain lot : 6B, coût des impacts continus du cône.

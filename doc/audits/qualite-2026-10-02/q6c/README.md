# Q6c — preuves (4 octobre 2026)

- `valeurs-effectives-avant.txt` : valeurs que le combat lit pour chaque créature, secours compris, au commit adb1e586. Pour chaque créature : type, comportement, rang, réglages de meute, stats de base. Pour chaque capacité : tous ses champs simples après `Configure`, sauf les minuteurs tirés au hasard. S'y ajoutent les deux groupes d'apparition de secours. Le fichier est produit par `EnemyEffectiveDump.cs.txt`, une scène temporaire non versionnée dans `tools/tests/`.
- `valeurs-effectives-apres.txt` : même relevé après Q6c.
- Différence : 12 lignes `_packBonusDamage=0.15` retirées, 12 lignes `_packFamily` ajoutées. `_packBonusDamage` était lu mais jamais appliqué : le champ mort est retiré, et le réglage reste déclaré « non lu » au contrat, question pour Raphaël. `_packFamily` est la famille de meute, nouvelle : `charognard` pour le Charognard, `null` pour les autres. Les 263 autres lignes sont identiques, groupes de secours compris (lus désormais dans `spawn_flow.json`).
- Hors relevé : le ralentissement de la Tisseuse (0,4 pendant 2 s, autrefois en dur) est vérifié par le banc des capacités ennemies.

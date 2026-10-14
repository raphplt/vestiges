# Plan 48 — Almanac : ménage du dépôt et migration

14 octobre 2026 · Demandé par Raphaël ([DECISIONS §173](DECISIONS.md)). Proposition à valider. Rien n'est fait.

L'outil lui-même est un projet à part, public : **Almanac**, plan dans `~/dev/perso/almanac/PLAN.md`. Ce plan-ci ne couvre que **ce que Vestiges doit faire** pour en profiter : ranger, décrire, migrer.

## 1. Constat

- 125 documents Markdown (environ 32 000 lignes), dont 66 plans ; plusieurs catalogues se recouvrent (05, 05 V1, 35), des sections V1 obsolètes restent lisibles.
- Une reprise de session lit `README.md` (79 Ko), `DECISIONS.md` (263 Ko) et `TABLEAU-DE-BORD.md` (42 Ko) : de l'ordre de 100 000 tokens avant tout travail.
- 98 JSON de données dans 19 dossiers ; trois seulement ont un contrat (`enemies/_contract.json`, `weapons/weapon_contract.json`, `progression/objects_contract.json`). Les stats ne se consultent qu'en ouvrant les fichiers.
- 5 676 PNG et 162 sons, sans vue d'ensemble ni liste des orphelins.
- 102 scripts à la racine de `tools/` (générateurs, tests, mesures, synthèses), sans rangement par usage.
- Le tableau de bord et le README sont tenus à la main, en double des plans.

## 2. Principe

Le dépôt de Vestiges reste la seule source de vérité. Almanac lit, montre et vérifie ; il n'emporte aucun contenu. « Migrer vers Almanac », c'est donc **rendre le dépôt lisible par Almanac** et remplacer les lectures manuelles par ses requêtes, pas déplacer les fichiers.

## 3. Lots proposés

À démarrer au jalon 1 d'Almanac (fin de sa phase B), pas avant.

| Lot | Contenu | Dépend de |
|---|---|---|
| V0 | Inventaire : docs remplacées ou obsolètes, catalogues en double, scripts jamais appelés, assets orphelins. **Liste soumise à Raphaël, aucune suppression sans son accord.** | Almanac B (puis C4 pour les orphelins) |
| V1 | `almanac.config.ts` : docs, registre de décisions, plans, toutes les collections de `data/`, assets, commandes de mesure ; un JSON Schema pour chacune des 16 collections sans contrat | Almanac B, A3 |
| V2 | Front-matter des plans : statut, lots, prochaine action, « attend Raphaël », `replaced_by` | Almanac C2 |
| V3 | Ménage des docs selon V0 : sections V1 sorties, catalogues fusionnés, plans clos marqués | V0 validé |
| V4 | Ménage de `tools/` : rangement par usage (génération, tests, mesure, synthèse), scripts morts retirés après accord | V0 validé |
| V5 | Références vivantes dans les docs de référence (`21-systeme-de-jeu.md`, catalogues) à la place des chiffres recopiés | Almanac C3 |
| V6 | Reprise de session par `almanac brief` dans `CLAUDE.md` et `AGENTS.md` ; MCP Almanac déclaré pour Claude Code et Codex | Almanac B4 |
| V7 | Tableau de bord généré par Almanac, l'actuel archivé | Almanac H1 |
| V8 | Résultats de `measure_run.sh` et des bancs au format `almanac-result.json` | Almanac G1 |

## 4. Questions tranchées (14 octobre 2026)

1. **Contrats manquants** : un schéma pour **toutes** les collections de `data/` pendant la migration (lot V1), pas seulement pour celles qu'on édite. Les trois contrats existants restent lus tels quels par l'adaptateur.
2. **Mises à jour datées du README** : **gardées écrites à la main** ; Almanac les affiche et `brief` en reprend les dernières.

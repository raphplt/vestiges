# Dégâts ennemis dans l’ouverture — 12 octobre 2026

Lot R2 du [plan 33](../../plans/33-equilibrage-ouverture.md). Demande : mesurer la menace des coups avec les cinq personnages et proposer des réglages. **Aucun dégât de gameplay modifié.** Les propositions ci-dessous sont des projections, sans partie jouée avec leurs valeurs.

## Protocole

20 runs de 10 min : cinq personnages × graines 42, 1002, 7 et 123. Profil neuf, Campement vide, Péril 0 ; armes de départ et passifs de personnage réels, puis choix de niveau et objets du monde. Bot `--nomad --visit --mortal`, régénération R1 (−10 % de base, suspension de 2 s après perte de PV). Scène Main réelle, moteur headless à 60 images par seconde de simulation. Deux graines en parallèle ; aucune mesure de FPS.

```sh
MEASURE_PROFILE=normal MEASURE_JOBS=2 \
MEASURE_EXTRA_ARGS="--nomad --visit --mortal --damage-probe --character vagabond" \
tools/measure_run.sh <dossier neuf> 600 "42 1002 7 123"
```

Remplacer `vagabond` par chacun des quatre autres identifiants. Les CSV `damage.csv` et `healing.csv` sont dans chaque dossier de graine. Les 20 résultats, journaux, CSV de densité et contrôles des processus sont dans `/tmp/vestiges-damage-r2-20261012-valid/baseline/`. Agrégats durables : [results.json](results.json).

**Mesure de la force d’un coup.** On garde les blessures de combat réellement acceptées et non fatales. Le nombre de coups pour vider la vie vaut `1 / moyenne(PV perdus / PV max au moment du coup)`, hors soins. Les coups fatals sont exclus de cette moyenne parce que `HpLost` est tronqué aux PV restants. Les coups absorbés par le bouclier et les dégâts du Néant sont séparés. Les soins ne comptent que les PV effectivement rendus, par origine.

**Limites.** Le bot ne joue ni esquive active ni recul tactique et ne poursuit pas les tireurs. Après un coup fatal, le banc lui rend toute sa vie pour continuer à mesurer ; ce retour n’est pas un soin de gameplay. Les décès mesurent la pression subie par ce trajet, pas la survie d’un joueur. Une même graine donne le même monde, mais les personnages et leurs builds parcourent des trajets différents. Les moyennes décrivent donc leur exposition réelle, pas les dégâts d’une créature identique contre chacun. Quatre graines ne suffisent pas à arbitrer le ressenti.

## Résultats des runs

| Personnage | PV perdus par coup, 0–2 min | Coups pour vider la vie, 0–2 min | Plage entre graines | Coups, 2–5 min | Coups, 5–10 min |
|---|---:|---:|---:|---:|---:|
| Vagabond | 7.0 | 15.3 | 12.3–19.7 | 12.1 | 14.5 |
| Traqueur | 5.9 | 12.8 | 10.6–15.4 | 8.5 | 9.2 |
| Forgeuse | 5.2 | 28.9 | 26.9–33.4 | 19.3 | 16.8 |
| Facteur | 5.8 | 15.5 | 14.3–17.4 | 11.5 | 12.2 |
| Éveillée | 6.1 | 14.3 | 11.4–15.8 | 10.3 | 11.4 |

Le constat de 10–15 coups est cohérent avec plusieurs personnages. La Forgeuse est nettement plus résistante : 120 PV de base + 25 du passif, soit **145 PV et 2 d’armure** au départ (11,8 % de réduction). Le Traqueur a 70 PV. Le Vagabond commence à 100 PV et son passif conserve +0,2 PV/s, soit 1,1 PV/s après R1.

## Qui fait mal

| Personnage | Part des PV perdus à l’Ombre, au Charognard et au Rampant d’Ombre, 0–2 min | Première blessure fatale du bot (plage) |
|---|---:|---:|
| Vagabond | 71.3 % | 70–90 s |
| Traqueur | 91.5 % | 35–119 s |
| Forgeuse | 92.5 % | 65–110 s |
| Facteur | 91.2 % | 43–88 s |
| Éveillée | 90.5 % | 36–108 s |

Les petites créatures dominent les blessures du départ. Leur faible dégât unitaire explique qu’une touche isolée soit peu menaçante. Au Vagabond, leurs coups non fatals annoncés pendant 0–2 min tournent autour de 4,2 PV pour l’Ombre, 5,8 PV pour le Charognard et 7,0 PV pour le Rampant d’Ombre, avant armure ; leurs stats de base sont 3, 4 et 5. Les multiplicateurs d’apparition et les capacités expliquent cet écart.

La marche suivante est déjà forte : au Vagabond, le coup moyen passe de 7 à 10,5 PV après 2 min. Sur les cinq premières minutes, le Rôdeur représente à lui seul environ un tiers des PV perdus au Vagabond ; sa base de 12 dégâts ne nécessite pas le même renforcement que les petites créatures.

## Proposition à tester en premier

| Créature | Dégâts de base actuels | Proposition | Hausse |
|---|---:|---:|---:|
| Charognard | 4 | 5 | +25 % |
| Ombre | 3 | 4 | +33 % |
| Rampant d’Ombre | 5 | 6 | +20 % |

Conserver d’abord les dégâts du Rôdeur (12), de la Brute (12), du Tréant (18) et des attaques à distance. Les trois petites créatures reviennent aussi plus tard et existent en variantes : modifier leur stat de base renforce également leurs élites et Souverains. Ce n’est donc pas un bonus exclusivement limité aux deux premières minutes.

**Projection à exposition et défense constantes**, en multipliant chaque blessure observée des trois espèces par le rapport proposé. Elle ne simule ni les nouvelles morts, ni les effets déclenchés par les touches, ni les choix et parcours qui en découleraient :

| Personnage | Coups actuels, 0–2 min | Avec la hausse ciblée (projection) | Avec +20 % sur tous les dégâts (projection) |
|---|---:|---:|---:|
| Vagabond | 15.3 | 13.1 | 12.7 |
| Traqueur | 12.8 | 10.4 | 10.6 |
| Forgeuse | 28.9 | 23.6 | 24.1 |
| Facteur | 15.5 | 12.6 | 12.9 |
| Éveillée | 14.3 | 11.8 | 11.9 |

La hausse ciblée réduit la tolérance aux touches courantes tout en conservant la différence entre héros. Le Traqueur passerait, après 2 min, de 8,5 à 7,8 coups moyens ; +20 % partout le ramènerait à 7,1 et renforcerait aussi les coups lourds. **Préférence : hausse ciblée, puis partie humaine de vérification avec le Traqueur et le Vagabond.**

Variante prudente si la suspension de régénération R1 durcit déjà assez la partie : Charognard 4 → 4,5 ; Ombre 3 → 3,5 ; Rampant d’Ombre 5 → 5,5. Ces valeurs restent des propositions, non intégrées.

## Soins et pression mesurés

| Personnage | PV perdus/min, 0–2 min | Soins/min | Dont régénération/min | Décès du bot/run, 0–2 min |
|---|---:|---:|---:|---:|
| Vagabond | 237 | 44 | 30 | 3.00 |
| Traqueur | 178 | 27 | 19 | 3.75 |
| Forgeuse | 188 | 37 | 26 | 1.75 |
| Facteur | 317 | 25 | 15 | 6.00 |
| Éveillée | 218 | 38 | 25 | 3.75 |

Le renouvellement des PV après décès permet au bot de subir plusieurs barres de dégâts par minute. Ces taux ne représentent donc pas la perte nette d’un joueur survivant. Après 5 min, les soins directs et les restitutions de perks peuvent dépasser la régénération : il ne faut pas attribuer tout le maintien en vie à cette seule stat.

## Vérification

Build avec avertissements traités comme erreurs : **0 warning, 0 erreur**. Les 20 processus terminent avec code 0, un RESULT unique et 600 s couvertes dans leurs CSV ; les cinq `validation.json` sont verts. Les avertissements connus de ressources à la fermeture restent admis par le validateur commun.

Sonde : 17697 résultats de dégâts, 0 sources inconnues, 0 résultats du Néant. Les chiffres de combat excluent ce dernier.

Le premier passage, dans `/tmp/vestiges-damage-r2-20261012`, a échoué : le bot ne confirmait plus le coffre depuis la refonte de l’écran. Ces quatre passages sont exclus. `AutoPickLevelUp` confirme maintenant la révélation puis la reprise via l’entrée de l’écran ; les 20 runs complètes valident ce parcours.

Seuls les outils de mesure et la documentation changent dans R2. Aucun réglage de dégâts ni comportement de combat modifié.

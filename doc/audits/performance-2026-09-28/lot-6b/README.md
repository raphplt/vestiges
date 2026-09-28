# Lot 6B — impacts continus du Transistor, 28 septembre 2026

**Livré : −47 % d’allocations directes et −83 % de déclenchements du feedback sur 50 cibles**, avec les mêmes dégâts, signaux et attribution à l’arme. Aucun gain FPS revendiqué.

## Contrat conservé

Le cône intègre toujours les dégâts à chaque tick. `EntityDamaged`, les statistiques par arme, le vampirisme, les probabilités de proc, l’exécution et les effets « tous les N coups » gardent leur cadence. La surcharge `ReadOnlySpan<Variant>` d’`EmitSignal` évite le tableau `params` sans modifier le signal Godot ni ses abonnés synchrones. Un test d’émission imbriquée vérifie que les arguments extérieurs restent intacts.

Seuls les flashs, le recul visuel, les demandes audio et les gerbes d’impact passent par un budget par cible : dix impulsions par seconde d’exposition à 60 Hz. Premier impact immédiat ; le coup fatal force son feedback. Une sortie du cône suspend ce budget, sans dégât reporté ; le recyclage le réinitialise. Les attaques ordinaires et leurs critiques conservent leur feedback habituel. Le cône ignore aussi les références inactives encore présentes dans le cache de groupe.

Les chiffres gardent chaque fraction de dégâts et leur animation. Leur texte et leur style sont réassignés seulement si la valeur affichée ou le style change. Cela évite des appels natifs redondants ; aucun gain mémoire n’est attribué isolément à cette modification dans le petit domaine d’entiers du banc.

## Résultats reproduits isolément

Vrais `Player`, `Enemy` et `CombatPools`, seed `221092026`, Godot 4.7.2 Mono, compilation Debug identique des deux versions. Pour chaque nombre de cibles : une émission de chauffe puis deux émissions de 120 ticks, soit quatre secondes actives. Ordre des densités : 0/1/10/50/100/100/50/10/1/0. Passes de versions **B2/A2/B3/A3**, ancien code dans une copie séparée de `57c759f5`, instrumentation finale identique. Les changements de feu présents au démarrage sont repris des deux côtés ; ils ne sont pas inclus dans le correctif.

| Cibles | Impacts avant = après | Allocations avant | Allocations après | Feedback avant → après |
|---|---:|---:|---:|---:|
| 0 | 0 | 63 360 octets | 63 360 octets | 0 → 0 |
| 1 | 240 | 115 200–115 232 | 90 240–90 272 | 240 → 40 |
| 10 | 2 400 | 581 760 | 332 160 | 2 400 → 400 |
| 50 | 12 000 | 2 655 360 | 1 407 360 | 12 000 → 2 000 |
| 100 | 24 000 | 5 247 360 | 2 751 360 | 24 000 → 4 000 |

À 50 cibles : **1 248 000 octets évités, soit 104 octets par impact**. Les dégâts cumulés restent exactement `4823.997946083546` dans le compteur double du banc. L’attribution float conserve exactement les valeurs de chaque passe correspondante, avec son arrondi préexistant. La baisse du feedback seule ne réduisait pas ces allocations ; le passage à la surcharge Span explique le gain mesuré de 104 octets par impact.

Le compteur de feedback observe la remise à zéro du vrai `HitFeedback` après chaque appel, hors de la fenêtre de mesure. **Déduit du code** : les demandes de flash et d’audio suivent le même garde ; `PlayerAttackFx.PlayHit` est également évité lors des ticks intermédiaires. Les chiffres restent alimentés à chaque tick.

Les intervalles bruts et allocations directes sont dans [A2](a2.json), [B2](b2.json), [A3](a3.json), [B3](b3.json). [Provenance](validation.json), builds et journaux bruts compressés à côté. Les nombres absolus ne se comparent pas aux 3,05–3,18 Mo de l’audit initial : ce banc isolé ne charge pas Main ni tous ses abonnés, dont RunTracker.

## Validation et limites

- **38 assertions, zéro échec** après correction ; 28 assertions de comportement communes à l’ancien code et dix contrôles supplémentaires du budget de feedback. Même séquence de 120 tirages d’embrasement (38 succès) dans le test déterministe, même vampirisme ; soin tous les cinq coups, ralentissement à chaque impact et attribution après changement de l’arme courante vérifiés. Sortie du cône, mort et réutilisation vérifiées.
- Régressions armes et capacités : zéro échec ; fusion des chiffres et critiques séparés vérifiés par le banc existant. Build sans avertissement ; smoke Hub vert.
- Captures de Main avec **Transistor + Trousseau + Boussole** simultanés, puis cibles fragiles : images ouvertes et inspectées, aucun message d’erreur runtime. [Combat](capture.png), [impacts sur cibles fragiles](capture-lethal.png). C’est une recette courte, pas une longue run avec renouvellement soutenu des ennemis.
- Les shaders de particules et le pitch audio consomment déjà le RNG global. Pour comparer exactement les procs, le scénario dédié coupe les particules et neutralise les sons d’impact ; les mesures d’allocations gardent les particules. Les probabilités et le nombre de tirages des perks ne changent pas, mais **la trajectoire aléatoire d’une vraie run avec effets actifs n’est pas garantie identique**. Séparer les RNG cosmétiques reste une piste distincte.
- Allocations mesurées sur le thread appelant, uniquement pendant `ProcessSustainedCone`. Elles excluent le travail animé entre les appels, les allocations natives et les autres threads ; pas de mesure des pauses GC ni du gain FPS global. Charge observée 4,23 à un moment de la passe, au-dessus du seuil 4 de `bench_ab.sh` : les chronométrages bruts ne servent pas à conclure sur les performances en temps réel.
- Les 12 000 signaux de dégâts restent intentionnels. Les diminuer demanderait un contrat d’agrégation différent pour les abonnés et les procs ; ce lot n’en a pas besoin pour obtenir son gain.

## Reproduction

```bash
tools/test_cone.sh /tmp/cone-apres
# Ancien code avec la même instrumentation (ignore seulement les dix assertions du nouveau budget) :
tools/test_cone.sh /tmp/cone-avant --baseline
tools/test_weapons.sh
tools/test_enemy_abilities.sh
CAPTURE_EXTRA_ARGS="--capture-weapons --weapons last_broadcast+chain_of_names+compass_needle" tools/capture_run.sh /tmp/cone-capture 12 6 1280x720 221092026
```

Ajouter `--lethal` dans `CAPTURE_EXTRA_ARGS` pour les cibles fragiles. La galerie accepte désormais `+` pour équiper plusieurs armes ensemble et garde la virgule pour des configurations successives. Suivi : [plan 10 §11](../../../plans/10-terrain-et-tiles.md#11-audit-de-performances-du-27-septembre--lots).

# Plan 52 — L'Élan

16 octobre 2026. Mécanique centrale retenue au [§185](DECISIONS.md). Elle doit être validée en bac à sable avant de construire les terres du [plan 51](51-objectifs-de-run.md).

Raphaël, en annulant la décision du 13 octobre (§151) : « il faut un système de version "maîtrise", avec puissance, mais qui puisse prendre en compte aussi la vitesse. Ce truc de gestes maîtrisés est très intéressant et donne un vrai côté stimulant ».

## 1. La promesse

**Bien bouger rend plus fort.** L'Élan est une ressource qui se gagne en courant et surtout par des gestes maîtrisés : esquive parfaite, glissade, saut du vide, percussion. Il se perd à l'arrêt et sous les coups. À la clé : de la vitesse, de la puissance, une percussion qui projette les créatures dans le Néant, et un saut qui franchit le vide.

**La maîtrise visée :**

| Moment | Ce que fait le joueur |
|---|---|
| Première run | Il bouge, les armes tirent seules, il dashe quand il est touché |
| Dixième run | Il ne s'arrête jamais et réussit ses premières esquives parfaites |
| Cinquantième run | Il enchaîne glissades et sauts au-dessus des trous, et percute la foule dans le vide |
| Centième run | Il suit le front pour prendre les coffres du bord à la dernière seconde et traverse une terre presque sans retomber |

**Garde-fous :**
- **Le déplacement de base reste précis.** L'Élan se gagne par des actions, pas par l'inertie : jamais de personnage qui glisse sans le vouloir.
- **Une seule ressource**, et chaque geste la nourrit.
- **Courir ne suffit pas pour monter haut.** La vitesse seule plafonne au premier palier ; au-dessus, il faut entretenir l'Élan par des gestes.

## 2. Les règles (valeurs de départ, à régler en jouant)

L'Élan va de 0 à 1, en **trois paliers** : I à 0,34, II à 0,67, III à 1. Tous les réglages sont dans `data/movement/elan.json`.

**Gains :**

| Source | Gain | Lot |
|---|---|---|
| Courir à au moins 85 % de sa vitesse | +0,04 par seconde, **sans dépasser le palier I** | E1 |
| Esquive parfaite (un coup évité pendant le dash) | +0,25 et une partie de la recharge rendue | E2 |
| Saut au-dessus du vide | +0,15 | E3 |
| Percussion | +0,03 par créature, 0,15 au plus par dash | E4 |
| Glissade rapide | +0,08 par seconde | E5 |

**Pertes :**
- **À l'arrêt**, après 0,4 s de tolérance : −0,5 par seconde.
- **Au-dessus du palier I**, l'Élan s'érode de 0,03 par seconde. Un Élan haut s'entretient.
- **Sous un coup** : −0,33, soit un palier.

**Effets par palier :**

| Palier | Vitesse | Dégâts | En plus |
|---|---|---|---|
| I | +8 % | +10 % | — |
| II | +16 % | +22 % | Percussion (E4) |
| III | +24 % | +36 % | Saut plus long (E3) |

**Lisibilité :** la traînée du joueur s'intensifie à chaque palier. Une jauge discrète apparaît sous les pieds, à côté de la recharge du dash qui existe déjà (`MobilityFeedback`). Chaque changement de palier se signale par un retour sonore et visuel.

**Le Facteur** a déjà un petit élan, la Tournée (`RoundMomentum`). Il est gardé tel quel pendant le bac à sable. Après validation, il deviendra la variante de l'Élan propre au Facteur.

## 3. Le bac à sable

Une arène jouable par Raphaël en fenêtre, lancée par `tools/elan_sandbox.sh`, sur la vraie scène `Main`. Elle reprend le gabarit du banc dense, mais sans IA ni immortalité :
- apparitions, Effacement et Résurgences coupés ;
- gardes retirés, terrain dégagé ;
- **foule en vagues** : Ombres et Cracheurs autour du joueur, réglables, relancées en continu ;
- **trous de vide** de plusieurs largeurs (mémoire forcée à zéro), à partir de E3 ;
- **sol qui s'effrite** : une bande qui passe au vide puis revient, en boucle ;
- **affichage de réglage** : valeur de l'Élan, palier, dernières sources de gain et de perte ;
- **touches** : R recommence la vague, G active l'immortalité.

## 4. Points d'accroche dans le code (étude du 16 octobre)

- **Vitesse** : `Player._PhysicsProcess`, l'expression de vitesse passée à `Mobility.Step` (`Speed * _speedMultiplier * _erasurePenalty.Speed`).
- **Effets par palier** : `ApplyPerkModifier("speed" / "damage", rapport, "multiplicative")`, appliqué seulement au changement de palier, comme la Tournée.
- **Coups** :
  - `PlayerDamageResolved` pour les pertes ;
  - `Player.TakeDamage`, branche `Mobility.IsInvulnerable`, pour l'esquive parfaite : tous les coups y passent.
- **Saut** :
  - `PlayerMobility.VisualHeight` existe déjà ;
  - `ClampMobilityTravel` bloque le Néant ;
  - `ErasureManager.HurtPlayerInVoid` blesse dans le vide ;
  - la mémoire d'une cellule se force par `OverrideMemory`, accès de test.
- **Percussion** :
  - `PlayerDashMoved` donne le trajet du dash ;
  - `CrowdIndex.Near` et `Iso.GroundDistanceToSegment` trouvent les créatures sur ce trajet ;
  - `Enemy.ApplyKnockback` les repousse, avec un plafond de 80 px et aucun effet sur les boss.
- **Chute des créatures** : aujourd'hui, elles ignorent le Néant (`Enemy.MoveBody`). Il faudra qu'elles évitent le vide en marchant et y tombent quand elles sont projetées (`Vanish`, ou une mort comptée). Ce sera mesuré au banc dense.
- **Retours** : `ScreenShake` est disponible. `Hitstop` est neutralisé (plan 02), à ne pas réactiver sans le retester.

## 5. Lots

| Lot | Contenu | Vérification |
|---|---|---|
| **E1 La jauge et l'arène** | `PlayerElan`, classe pure comme `PlayerMobility`, et `elan.json` ; gain par la vitesse, pertes, paliers ; effets de vitesse et de dégâts ; jauge sous les pieds ; arène jouable | Régressions de mouvement, nouvelle suite de l'Élan, capture, Raphaël joue |
| E2 L'esquive parfaite | Événement « coup esquivé » dans `TakeDamage`, gain et recharge rendue, retour visuel et sonore | Suite de l'Élan (projectile et contact esquivés), capture |
| E3 Le saut du vide | Le dash franchit le vide tant qu'il est en l'air ; longueur selon le palier ; atterrir dans le vide = chute (dégâts, retour au dernier sol sûr) ; ombre qui rétrécit ; trous dans l'arène | Suite de mouvement (franchir, chuter, bord du monde toujours bloqué) |
| E4 La percussion | Au palier II et plus, le dash projette les créatures traversées ; elles tombent dans le vide ; elles l'évitent en marchant | Suite de l'Élan, banc dense avant/après |
| E5 La glissade | Maintenir la touche après le dash fait glisser ; virage borné ; gain en glissade rapide | Suite de mouvement |
| E6 Retours et réglages | Traînée par palier, sons, réglages après les séances de Raphaël, bot de mesure qui joue l'Élan | Séance de jeu, mesure |

L'Élan reste actif pour tous les personnages dès E1. S'il gêne en run normale, un réglage de `elan.json` le coupe.

## 6. Questions ouvertes (sans blocage, valeurs de départ prises)

- **Une jauge visible, ou seulement la traînée ?** Je pars de la jauge discrète, à retirer si la traînée suffit.
- **L'Élan reste-t-il d'une terre à l'autre ?** À trancher avec le plan 51.
- **Une créature tombée dans le vide** compte-t-elle comme éliminée, avec XP et score ? Je propose oui, pour récompenser le geste.

## 7. Compte rendu E1 — 16 octobre 2026

**Livré :**
- **`PlayerElan`** (`scripts/Core/PlayerElan.cs`) : un état pur, comme `PlayerMobility`. La course le nourrit jusqu'au plafond, les gestes au-delà. Il fond à l'arrêt après la tolérance, s'érode au-dessus du plafond, et un coup lui retire un palier.
- **Branchement au joueur** (`Player.Elan.cs`) : l'Élan avance à chaque pas physique, sur la vitesse réellement parcourue rapportée à la vitesse de course. Le bonus de vitesse et de dégâts ne passe par `ApplyPerkModifier` qu'au changement de palier.
- **Réglages** dans `data/movement/elan.json`, contrôlés en entier par `ElanConfig` : seuils croissants, clés connues.
- **Éteint en run normale** (`enabled_in_runs` à faux) : l'équilibre, les mesures et les autres suites ne changent pas avant validation.
- **Jauge** sous la recharge du dash : sa couleur se réchauffe à chaque palier, les seuils sont marqués.
- **Bac à sable** `tools/elan_sandbox.sh` :
  - vraie carte ; apparitions, Effacement, Résurgences, événements, Barrière et Indicible coupés ;
  - 40 créatures entretenues autour du joueur (`--enemies N`) ;
  - personnage au choix (`--character <id>`) ;
  - un coup fatal remet les PV au maximum ;
  - affichage de réglage : valeur, palier, dernière source, multiplicateurs.

  `--capture <dossier> --seconds S` fait courir le joueur seul et garde une image.
- La recherche de terrain dégagé du banc dense est mise en commun (`tools/tests/TestArena.cs`).

**Vérifié :**
- build sans avertissement, smoke test vert ;
- `tools/test_movement.sh` vert, avec 17 contrôles de l'Élan :
  - réglages refusés s'ils sont invalides ;
  - course trop lente sans gain, plafond de la course, geste au-delà ;
  - érosion, tolérance et fonte à l'arrêt, perte d'un palier au coup ;
  - sur le vrai joueur, premier palier après 9,5 s de course, course ×1,080 mesurée, palier perdu au coup, vitesse rendue à l'extinction ;
- capture du bac à sable après 14 s : Élan 0,34, palier I, vitesse ×1,08, dégâts ×1,10, jauge visible.

**Relecture (`godot-reviewer`)** : rien de bloquant. Corrigé :
- la jauge restait figée quand on éteignait l'Élan ;
- un plafond de course sous le premier seuil est désormais refusé ;
- un `elan.json` fautif éteint l'Élan avec un message, au lieu d'empêcher la run de démarrer.

Documenté : un dash compte comme de la course, et courir freiné (eau, ralentissement) ne nourrit pas l'Élan.

**Réglage corrigé en route** : une perte de 0,34 au coup faisait tomber de deux paliers à Élan plein (1 → 0,66). Elle passe à 0,33.

**Ce que E1 ne donne pas encore** : en E1, seule la course nourrit l'Élan, donc on ne dépasse pas le palier I en jouant. Les gestes (esquive parfaite, saut, percussion, glissade) arrivent en E2 à E5. E1 sert à sentir la jauge, la perte au coup et le premier palier.

**Suite proposée : E2, l'esquive parfaite.**


## 8. Compte rendu E2 — 16 octobre 2026 (soir)

**Livré :**
- **Esquive parfaite** : le premier coup évité pendant un dash donne +0,25 d'Élan et rend la moitié de la recharge du dash. Elle compte une fois par dash, même sous plusieurs coups.
- **Une seule entrée pour tous les coups** : `Player.DodgesHit()`, appelée par `TakeDamage`, la Barrière et l'Indicible (qui testaient l'invulnérabilité eux-mêmes). L'esquive compte donc quelle que soit la source : contact, projectile, zone, boss.
- **Retour** : son clair (la parade d'armure, plus aiguë, provisoire), éclair blanc chaud du joueur et onde qui s'ouvre au sol en quatre rayons, cuits au chargement.
- **Réglages** dans `elan.json` (`perfect_dodge` : gain, part de recharge rendue, son).
- **Élan éteint** (run normale) : le dash protège comme avant, sans gain ni recharge rendue. L'équilibre ne change pas.
- **Bac à sable** : compteur d'esquives parfaites à l'affichage. En mode capture, le bot fonce vers la créature la plus proche et dashe au contact.

**Vérifié :** build sans avertissement ; `tools/test_movement.sh` vert, avec 5 contrôles de plus (gain, recharge rendue, une seule fois par dash, coup qui porte hors du dash, Élan éteint).

**Pas encore vérifié** : la capture de l'onde. La dernière capture du bac à sable a gardé l'écran de chargement au lieu du jeu, à comprendre avant de conclure sur le rendu.

**Suite : E3, le saut du vide.** Proposé : l'Élan « porte » le dash au-dessus du vide. Un dash fait environ 70 px et une cellule de Néant 128 px, donc un dash seul ne franchit jamais un trou. Au-dessus du vide, le dash se prolonge d'une distance qui grandit avec le palier (par exemple 0, 64, 128 puis 192 px). Franchir un trou et retomber sur le sol donne +0,15. Atterrir dans le vide, c'est y marcher, et payer (plan 51 §11).

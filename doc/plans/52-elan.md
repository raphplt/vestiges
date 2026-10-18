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

## 9. Découpage de E3 et E4 — 18 octobre 2026

Revue de direction du 18 octobre ([registre §189](DECISIONS.md), [LE-JEU.md](../LE-JEU.md)) : **le vide comme arme, jauge gardée**, une semaine de bac à sable avec critère d'arrêt. L'Élan reste éteint en run normale ; rien de ce qui suit ne change une run normale.

| Lot | Contenu | Vérification |
|---|---|---|
| **E0 Outillage sur la terre** (préalable) | Depuis que les terres sont la règle (17 octobre), le bac à sable et le banc dense posent l'arène hors de la terre et coupent l'Effacement avant la fin de la recousure : sol noir, joueur bloqué, **banc dense invalide**. L'arène est cherchée sur le sol de la terre ; la recousure est terminée d'office ; le bac à sable garde l'Effacement actif mais figé (le Néant draine, rien ne tombe) | Capture du bac à sable, banc dense valide |
| **E3 Le saut du vide** | Dès le palier I, le dash est porté au-dessus du Néant et s'y prolonge de 64, 128 puis 192 px selon le palier (`void_leap` dans `elan.json`). Au palier 0 et Élan éteint, rien ne change : le dash s'arrête au bord. Atterrir dans le vide revient à y marcher (Essence, puis PV). En l'air, le Néant ne prend rien. Un saut qui franchit au moins 32 px de vide et retombe sur le sol donne +0,15. Le bord du monde reste infranchissable. Hauteur visible en l'air, son et poussière à la retombée. Bac à sable : bandes de vide de 1, 2 et 3 cellules près du départ, compteur de sauts | Suite de mouvement (franchir, retomber dans le vide, palier 0 arrêté au bord, bord du monde, pas de drain en l'air, gain une fois), capture d'un saut |
| **E4 La percussion** | Le dash projette les créatures qu'il traverse (`percussion` dans `elan.json`) : recul selon le palier, en avant et vers l'extérieur du trajet, +0,03 par créature, 0,15 au plus par dash. **Palier minimum réglable** : 0 par défaut (recommandation de l'agent), 2 pour la version d'origine, basculable au lancement du bac à sable (`--percussion-tier`). Boss et mini-boss immunisés. Le vide comme arme allumé (Élan allumé) : une créature ordinaire projetée dans le Néant y tombe, éliminée (XP au bord, score) ; en marchant, toutes évitent le vide ; élites et Souverains ne tombent pas | Suite de l'Élan (projection, gain plafonné, chute comptée, boss immunisé, évitement en marchant, rien quand l'Élan est éteint), banc dense A/B (Élan éteint : aucun coût ; allumé : coût de l'évitement), capture d'une chute |

Choix pris pour le prototype, à juger en jouant :
- **Le saut demande le palier I** : un débutant à l'arrêt ne tombe pas par mégarde, et le premier palier vient en 10 s de course.
- **Les armes qui repoussent font aussi tomber** : tout recul vers le vide compte, pas seulement la percussion. C'est le vide comme arme pour tous les builds de recul.
- **L'XP d'une chute tombe au bord** d'où la créature est partie, pas dans le vide.

## 10. Compte rendu E0, outillage sur la terre — 18 octobre 2026

**Constat** : depuis le 17 octobre, les terres sont la règle et l'arène des bancs tombait hors de la terre.
- `TestArena.FindOpen` cherchait seulement un cercle sans décor. Le premier cercle libre était dans le vide autour de la terre : arène en (−268, −4087) au banc dense.
- Le joueur ne peut pas marcher hors du sol : il restait bloqué, et `_distance > 100` échouait. **Le banc dense rendait `valid=False` depuis le 17 octobre.**
- Le banc et le bac à sable coupaient l'Effacement avant la fin de la recousure : sol noir à l'écran.

**Corrigé :**
- `TestArena.FindOpen(probe, world)` exige du sol sous le centre et sous huit points du cercle. Sur une terre dense, le rayon se réduit à 75 % puis à 50 %.
- `ErasureManager.SettleStitchForTests` (outils seulement) termine la recousure et publie le sol.
- Le bac à sable garde l'Effacement actif sur une terre, mais figé (`FrontFrozen`) : rien ne tombe, le Néant draine, le sol suit le joueur.

**Vérifié :**
- build sans avertissement ;
- capture du bac à sable : terre de forêt, joueur sur le sol ;
- banc dense court (5 s) : `valid=True`, arène en (−5008, −1064) sur une terre de carrière, sol et décors à l'image ; le mode dash demande la durée normale (15 s) pour ses cinq dashs.

## 11. Compte rendu E3, le saut du vide — 18 octobre 2026

**Livré :**
- **Le dash porté** (`PlayerMobility`) : au-dessus du Néant, un dash qui arrive au bout de sa durée continue, pas à pas, tant qu'il reste de l'allonge. L'allonge est lue au départ du dash : `void_leap.extension_px`, une valeur par palier, palier 0 compris (0, 64, 128, 192 px). Au palier 0 et Élan éteint, l'allonge est nulle : le dash s'arrête au bord, comme avant.
- **La règle du saut** (`Player.VoidLeap.cs`, nouveau fichier partiel pour ne pas faire grossir `Player.cs`) :
  - la règle est fixée au départ du dash, pour toute sa durée ;
  - le vide franchi est échantillonné tous les 4 px ;
  - retomber sur le sol après au moins 32 px de vide donne +0,15 et compte un saut ;
  - retomber dans le vide revient à y marcher (Essence, puis PV) ;
  - le bord du monde reste bloqué par le générateur et le mur.
- **En l'air, rien n'est pris** : l'`ErasureManager` ne publie pas la phase du joueur et ne draine pas tant que `Player.IsLeaping`.
- **Retours** :
  - le sprite monte de 10 px au-dessus du vide (200 px/s à la montée et à la descente), l'ombre reste au sol ;
  - à la retombée réussie : un choc sourd (`sfx_impact_blunt`, provisoire) et une onde de poussière au sol, sur le modèle de l'esquive parfaite.
- **Réglages** : section `void_leap` d'`elan.json`, lue strictement (une allonge par palier, croissante).
- **Bac à sable** :
  - trois bandes de Néant à l'est du départ (1, 2 et 3 cellules de large, 5 de haut, 3 cellules de sol entre elles) ;
  - compteur de sauts ;
  - `--scenario leap` : le bot, au palier II, saute la première bande et l'image est gardée en l'air.

**Vérifié :**
- build sans avertissement, smoke test vert ;
- `tools/test_movement.sh` et `tools/test_movement.sh --run-integration` verts. Contrôles ajoutés :
  - allonges du fichier : palier 0 sans allonge, longueurs refusées si le compte ou l'ordre est faux ;
  - palier 0 : le dash s'arrête au bord (x = 128) ;
  - palier II : une cellule franchie, retombée à x = 267, Élan 0,70 → 0,83 (gain moins l'érosion) ;
  - palier I parti 40 px avant le bord : retombée dans le vide à x = 250, sans gain ;
  - vraie carte : une mise à jour complète de l'Effacement en plein saut ne prend rien, et au palier III le dash ne passe pas le bord du monde ;
- capture du bac à sable (`--scenario leap`), regardée : le joueur en l'air au milieu de la bande, en pose de dash, avec la barre de recharge restée au sol.

**Relecture (`godot-reviewer`)** : rien de bloquant ; l'Élan éteint et le palier 0 ne changent rien. Corrigé :
- le saut instantané (Plan de métro) passait le vide dès le palier I, sans allonge : il n'est plus porté, il s'arrête au bord ;
- l'allonge était convertie en secondes avec la vitesse du départ, si bien qu'un ralentissement en l'air (eau) la doublait. Elle se compte maintenant en pixels ;
- une mort en plein saut laissait le corps en l'air ;
- **une pause ou une montée de niveau en plein saut laissait tomber le joueur dans le vide** : le saut reste suspendu en l'air et reprend ensuite (choix de l'agent, pour qu'une interruption ne coûte jamais une chute) ;
- un dash repris à l'image même où le saut finit perdait sa retombée ;
- l'onde de retombée continuait pendant une pause ;
- `Land()` renommé `LandFromLeap()`, pour ne pas le confondre avec le changement de terre.

Gardé tel quel et documenté :
- un dash parti du Néant qui en ressort compte comme un saut, puisque le séjour dans le vide a été payé ;
- la glissade continue de tourner pendant l'allonge : on peut viser sa retombée.

Contrôles ajoutés après relecture : Élan éteint (cas des runs normales), saut instantané au palier I, pause en plein saut, allonge de 64 px gardée malgré une vitesse divisée par deux en l'air. La mort en l'air n'a pas de contrôle : le test demanderait un joueur jetable ; la correction tient en une ligne (`Die`).

**Pas mesuré au banc** : le coût ajouté se limite au joueur (une consultation de la phase par pas, seulement pendant un dash porté). Le banc A/B est fait avec E4, dont l'évitement du vide touche toute la foule.

**À juger en jouant :**
- **la lisibilité du bord** : le Néant se dessine en dégradé tramé sur environ une demi-cellule, mais la règle bascule net à la frontière de la cellule. Savoir où sauter peut être difficile. Un bord plus net est une piste pour E6 ;
- les longueurs (64, 128, 192 px) : au palier I, il faut partir à moins de 8 px du bord pour franchir une cellule ;
- le son de retombée.

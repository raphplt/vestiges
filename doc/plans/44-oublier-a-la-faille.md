# Plan 44 — Oublier au Puits

Statut : **validé le 13 octobre 2026** ([DECISIONS §157](DECISIONS.md)). **Le soir même, l'oubli quitte la Faille pour un Puits dédié ([§158](DECISIONS.md), §9 ci-dessous)** : les sections 1 à 8 décrivent la première version, à la Faille : `rift.forget`, `RiftForgetting` et la carte « Oublier » n'existent plus. Les règles d'oubli (§3) et les retraits (§4) restent valables, au Puits.

## 1. La demande

Raphaël veut plus de mécaniques liées à l'oubli, thème central du jeu. Il veut d'abord pouvoir **oublier une arme, un Écusson ou un objet** pris pendant la run, pour corriger un mauvais choix ou changer de build en cours de route.

Il a retenu la recommandation de l'agent :
- **à la Faille**, contre de l'**Essence** ;
- l'oubli **laisse une trace** : la prochaine arme, ou le prochain Écusson, repart d'une part des niveaux oubliés ;
- **tout passe par la Faille**. L'échange d'arme au sol et l'écran d'échange du sac plein disparaissent.

## 2. Ce qui existe

- **Armes** (4 emplacements) : une arme posée au sol (Wagonnet) s'échange, si les emplacements sont pleins, contre l'arme du premier emplacement (`WeaponPickup`). Le Wagonnet ne pose une arme que si un emplacement est libre.
- **Écussons** (4 emplacements) : aucun moyen de les retirer.
- **Objets du sac** : quand le sac est plein, un objet au sol, ou acheté à l'Atelier, ouvre l'écran d'échange (`ItemExchange`). Cet écran oublie une pile entière pour prendre le nouvel objet, gratuitement (plan 35 I3).
- **Faille** : elle offre trois améliorations épiques ou légendaires, chacune payée d'un Oubli et d'un point de Péril. Elle se referme quand on en accepte une.
- **Level-up** : un emplacement libre fait offrir une arme ou un Écusson neufs.

## 3. Règles

- **Où :** toute Faille ouverte. L'écran de la Faille gagne une carte « Oublier ». Celle-ci ouvre la liste de ce qu'on porte : armes, Écussons, puis piles du sac.
- **Prix :** 100 Essence, +50 % à chaque oubli de la run, toutes Failles confondues. Réglages dans la section `rift.forget` de `data/world/landmarks.json`.
- **La Faille reste ouverte** après un oubli : on peut encore y prendre une amélioration, qui la referme. L'écran revient aux offres. Celles qui visaient la chose oubliée disparaissent.
- Une Faille dont aucune amélioration n'est possible s'ouvre quand même, si l'on peut y oublier quelque chose.
- **Arme :** on ne peut pas oublier sa dernière arme. La carte prévient si un perk deviendrait inactif.
- **Trace :**
  - la prochaine arme prise repart de la moitié du niveau de l'arme oubliée (40 → 20), en améliorations communes. Ascension et surplus sont perdus ;
  - le prochain Écusson repart de la moitié du niveau de l'Écusson oublié, en cartes communes ;
  - plusieurs traces attendent dans l'ordre des oublis ;
  - les objets n'ont pas de niveau, donc ils ne laissent pas de trace. Oublier une pile la retire en entier.
- **Écusson oublié :** son apport, ses paliers et son fil d'usage sont retirés.
- **Sifflet d'arbitre :** le Péril pris ne se rend pas (inchangé).

## 4. Ce qui disparaît

- **Échange d'arme au sol :** une arme au sol se prend seulement s'il reste un emplacement. Sinon, l'invite renvoie à la Faille et l'arme reste au sol.
- **Écran d'échange du sac :** l'objet au sol reste au sol quand le sac est plein, et l'invite renvoie à la Faille. À l'Atelier, l'objet ne s'achète plus à sac plein.
- `ItemExchange`, ses clés de traduction et le mode `--capture-exchange` sont retirés.

## 5. Lot F1

| Contenu | Vérification |
|---|---|
| `Player.ForgetBadge` (apport, paliers, fil d'usage), `RiftForgetting` (écran, prix, traces), carte « Oublier » de la Faille ; retrait de l'échange d'arme et de l'écran d'échange ; réglages contrôlés au chargement | Suite `inventory` (oubli d'Écusson et d'arme, traces, objet au sol à sac plein, Atelier), `weapons`, `catalogs`, `smoke` ; capture `--capture-rift` étendue à l'écran d'oubli, regardée |

## 6. Ouvert

- Prix, part des niveaux gardée : à juger en jeu.
- Pistes d'oubli suivantes évoquées le 13 octobre, non retenues pour l'instant : arme négligée qui perd des niveaux, Écussons peu sollicités qui s'érodent, objet oublié qui revient altéré plus loin.

## 7. Lot F1 livré — 13 octobre 2026

**Code.**
- `RiftForgetting` (World) : écran d'oubli, prix, traces. Les traces s'appliquent à l'arrivée de l'arme (`WeaponEquipped`) ou de l'Écusson (`PassiveSouvenirAdded`), sur un tirage à part (`rift_traces`).
- `RiftDirector` : carte « Oublier » après les offres. Retour aux offres après un oubli, sans celles qui visaient ce qui a été oublié. Une Faille sans amélioration possible s'ouvre encore pour oublier.
- `Player.ForgetBadge` : l'apport revient au neutre, les paliers se débranchent (`ObjectMilestones.Deactivate`), le fil d'usage s'arrête (`BadgeUsage.Untrack`).
- `PerkSpecializationOffers.DeactivatedByForget` remplace l'avertissement de l'échange.
- Retirés : `ItemExchange`, l'échange d'arme de `WeaponPickup`, l'achat à sac plein de l'Atelier, le mode `--capture-exchange`. Les invites au sol disent « Prendre » ou renvoient à la Faille.

**Vérifications** (copie isolée : la matrice M1 d'une autre session tenait le verrou de validation du dépôt).
- Build sans avertissement.
- Suite `inventory` : 86 contrôles, dont l'oubli à la Faille :
  - prix 100 puis 150 ;
  - fouet niveau 40 oublié, boîte à musique arrivée au niveau 20 ;
  - Instinct niveau 30 oublié, vitesse et dash rendus, fil arrêté ; Peau dure arrivée au niveau 15 ;
  - pile du Glaçon oubliée en entier ;
  - dernière arme gardée, Essence insuffisante.
- Suites vertes : smoke, perk_effects, objects, weapons, catalogs, item_loot, item_combat, small_places, choice_screen, quests, run_trace, dev_mode (12/12).
- Capture `--capture-rift` regardée :
  - les offres de la Faille, avec la carte « Oublier » en bas (à faire défiler) ;
  - l'écran d'oubli : armes, Écusson niveau 24 (« trace : niveau 12 »), objets ;
  - le retour aux offres, sans celle qui visait l'Écusson oublié, et l'oubli suivant à 150 ;
  - la Faille refermée à l'offre prise.

**À juger en jeu :**
- la place de la carte « Oublier » : elle est en dernier, sous les trois offres, et il faut faire défiler pour la voir ;
- le prix ;
- la part de niveaux gardée.

**Relecture `godot-reviewer`, corrigé :**
- **grave** : oublier Ancrage (PV max) à PV bas rendait les PV négatifs. Le maximum baisse désormais sans toucher aux PV portés, sauf pour les ramener sous le nouveau maximum (deux contrôles ajoutés) ;
- un objet qui n'entre pas dans le sac n'expire plus tant qu'il ne peut pas entrer, puisqu'il faut aller à une Faille pour lui faire une place. Le Néant l'efface toujours ;
- invites justes : « déjà au maximum » pour un objet, « déjà portée » pour une arme ;
- une Faille sans amélioration possible ne s'ouvre que si l'oubli est payable ;
- commentaires de l'ancien échange réalignés, capture `--capture-weapon-pickup` adaptée (arme refusée, emplacements pleins).

**Après corrections :**
- suite `inventory` : 88 contrôles, 0 échec ;
- 12 suites sur 13 vertes ;
- `weapons` : 0 contrôle en échec, mais une forme de collision fuit à la fermeture, une fois sur deux environ :
  - mesurée en copies isolées : HEAD, 0 sur 11 passages ; HEAD + le seul CSV de traductions, 3 sur 4 ; HEAD + le seul `landmarks.json`, 2 sur 4 ;
  - un simple changement de données suffit à la déclencher : elle ne vient pas du code de l'oubli ;
  - tâche à part proposée pour la trouver.

## 8. Souverains sans limite de temps (DECISIONS §157)

- La chasse (`HuntEvent`) n'a plus de fin au temps : le Souverain reste jusqu'à sa mort. Sa durée de 60 s ne sert plus qu'à placer le rendez-vous avant la Résurgence suivante.
- Tant qu'il vit, aucun autre micro-événement ne démarre, comme pendant toute chasse. **Point pour Raphaël :** un Souverain laissé derrière soi suspend donc les événements suivants (chasses de 9:30, ruées, reliques…) jusqu'à sa mort. Il suit le joueur et n'est jamais retiré par la distance. Si c'est gênant, deux options : libérer le calendrier au bout de 60 s, le Souverain restant en vie, ou le faire suivre plus vite.
- S'il est retiré sans être abattu (englouti par le Néant), l'événement échoue proprement.
- L'objectif devient « Abattez le … ».
- **Vérification** : nouveau mode `--capture-sovereign` (joueur désarmé, immobile, invincible). Le Souverain est toujours là au bout de 75 s : même créature, PV pleins, barre en haut de l'écran (capture regardée).

## 9. Lot F2 — le Puits (DECISIONS §158)

Raphaël trouve confus qu'un même lieu, la Faille, fasse deux choses. L'oubli part dans un lieu à lui, un **Puits**. Les puits à boire (petits lieux, soin de 15 %) disparaissent pour ne pas s'y confondre.

**Fait :**
- **Puits** (`Well`, `WellDirector`) :
  - 6 par carte, couronne 0,15 à 0,9 ;
  - placé après tous les autres lieux, pour ne déplacer ni coffres ni Mémoriaux, Failles, Ateliers ou stèles d'une graine ;
  - section `well` de `landmarks.json`, avec les mêmes règles d'oubli qu'au §3 (100 Essence, +50 % par oubli, moitié des niveaux en trace) ;
  - englouti par le Néant, il s'éteint.
- **Écran** : l'écran du Puits est directement la liste à oublier. Après un oubli, elle se rouvre au nouveau prix ; « Partir » ferme.
- **Sprite** `forget_well` (`tools/sprites/props/landmarks.py`) : margelle de moellons à demi écroulée, montants brisés sans toit ni seau, eau pâle de l'Effacement qui luit, lueurs qui montent. Plus grand que le puits de décor des champs. Colonne de lumière bleu pâle (`well` dans `rarities.json`), icône et légende de minimap.
- **Repère « Puits »** (+10 PV max) : il se gagne au premier oubli à un Puits. Le nombre de Repères reste 13.
- **Faille** : rendue telle qu'avant le §157.
- **Puits à boire** retiré de `small_places.json` : le décor des champs reste, sans interaction. Clé `PLACE_WELL_PROMPT` retirée.
- **Bot de mesure** : il compte les Puits vus mais ne les vise pas. Oublier reste un choix de build.
- **Mode de capture** `--capture-well`.

**Point pour Raphaël :** les puits à boire donnaient jusqu'à 10 soins de 15 % par carte. Restent les soins de la voiture abandonnée, de la table de pique-nique et du Mémorial.

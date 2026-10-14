# Plan 47 — Une fin de run qui menace

14 octobre 2026. **Découpage validé (L1–L2 lancés, L4 en partie tranché, [§168](DECISIONS.md)).** Réponse à [DECISIONS §165 et §167](DECISIONS.md), sur les constats de la matrice M1 ([plan 42 §5](42-signature.md#5-campagne-de-mesures)).

## 1. Le constat

Raphaël le voit en jouant et la mesure le confirme. Avec la méta complète, passé 15 min, le bot ne perd presque plus de PV (Traqueur moyen : 10 PV/min). Les cinq builds de référence rendent tous la fin de run sans danger (0 à 2 PV/min pour l'arme poussée, le critique et la zone).

Causes relevées :

- **La puissance du joueur croît plus vite que les créatures.** Ses dégâts par seconde suivent son niveau (≈ 0,38 × niveau^2,28, corrélation 0,97) : environ ×22 entre 10 et 30 min, contre ×15 pour les PV des créatures (`hp_scaling_segments` : ×1,13 par minute après 8 min, ×1,35 en fin de run). Les créatures meurent avant d'arriver au contact.
- **Presque rien ne contourne ce mur de dégâts.** Dans la run de la Pelle, après 7 min, les PV perdus viennent à 75–90 % des Présages, qui tirent à distance.
- **Les défenses s'empilent sans frein** (`data/characters/defense.json`, `passive_souvenirs.json`) :

  | Défense | Source | Valeur |
  |---|---|---|
  | Vol de vie | Plafond | 8 % des PV max par seconde, ×2 sous la moitié des PV avec le palier 15 de l'Écusson du barbier |
  | Régénération | Écusson de la couturière | 0,6 PV/s par niveau sur 99 niveaux (niveau 30 : 18 PV/s), ×2 après une blessure au palier 15 |
  | Armure | Écusson du rugbyman | 2 par niveau ; 40 d'armure retirent 73 % des dégâts (plafond 75 %, demi-valeur 15) |

- **L'Effacement ne presse pas** : 3 à 7 % du temps passé Effacé ou dans le Néant.

## 2. La cible (§167)

- **Joueur moyen, méta complète : il meurt souvent entre 20 et 30 min.** Atteindre l'Indicible demande un bon build et de bien jouer ; qu'un joueur y arrive du premier coup est un échec.
- Les déblocages (armes, objets, Écussons, améliorations permanentes) servent cette progression : un profil neuf ne doit pas pouvoir tenir jusqu'à 30 min.
- Le facteur principal reste la montée des créatures : PV et dégâts au fil du temps, plus de créatures, un monde plus instable.

## 3. Lots proposés

### L1 — Défenses freinées (données seules)

Demandé par Raphaël : vol de vie trop fort, régénération et probablement armure à baisser. Premières valeurs, à vérifier par la mesure de L2 :

| Réglage | Aujourd'hui | Proposé |
|---|---|---|
| Plafond du vol de vie | 8 % des PV max/s | 2 % |
| Vol de vie par niveau de l'Écusson du barbier | 0,5 % | 0,3 % |
| Régénération par niveau de l'Écusson de la couturière | 0,6 PV/s | 0,2 PV/s |
| Régénération par exemplaire de la Pastille mentholée | 0,15 PV/s | 0,08 PV/s |
| Armure : demi-valeur | 15 | 30 (40 d'armure → 57 % au lieu de 73 %) |
| Armure : plafond de réduction | 75 % | 60 % |

Les rangs de Campement en régénération et en armure partent avec la puissance brute (§152) ; ils ne sont pas recalés ici.

### L2 — Créatures qui suivent le joueur après 15 min (données, mesure)

- Relever la croissance des PV et des dégâts après 15 min, et le nombre de créatures (plafond de run), pour viser la cible du §2 avec les défenses de L1.
- Le « Péril du temps » est déjà câblé (`time_peril_per_minute`, aujourd'hui 0) : l'activer ajoute des créatures avec le temps, sans code.
- Mesure : matrice ciblée (méta neuve, milieu et complète ; profils moyen et fort ; trois graines ; 30 min). La cible se lit en coups fatals par tranche de 5 min et en minute de la première mort.
- Avant L2 : le bot doit aller chercher les Souverains et son profil fort esquiver moins loin (plan 42), sinon la mesure sous-estime ou surestime le danger.

### L3 — Un monde plus instable (conception, plan 42)

L'Effacement qui accélère en fin de run et fait traverser la foule, les stats qui baissent au hasard dans le Néant (déjà retenu au plan 42 §3). C'est le levier « signature » : il se conçoit au plan 42 §6 avant tout code.

### L4 — Niveaux de difficulté au départ (conception, avec le plan 46)

Proposé par Raphaël le 14 octobre : choisir avant la run un niveau, de simple à impossible, qui change la fréquence et la puissance des créatures, les boss, etc. Le plan 46 (autre session) a déjà un accord de principe sur des « paliers de départ » : les deux se conçoivent ensemble.

Tranché ([§168](DECISIONS.md)) :
- **un seul classement** mondial aux éliminations, tous niveaux confondus ;
- **chaque niveau s'ouvre** en battant l'Indicible au niveau d'avant ;
- **Vestiges** : 0,2 à 0,4 × éliminations au niveau simple, davantage au-dessus, pour que les nouveaux joueurs aient intérêt à monter le temps de tout débloquer. Le taux commun est passé à 0,4 le 14 octobre.

Reste ouvert : déblocages réservés aux niveaux élevés, marques de victoire, nombre de niveaux et ce que chacun change. Le niveau « normal » est celui calé en L2 ; les autres s'en déduisent.

## 4. Ordre proposé

1. L1 (petit, données seules), avec L2 et sa mesure : défenses et créatures se règlent ensemble.
2. L4, conception avec Raphaël, en même temps que le plan 46.
3. L3 dans la conception du plan 42.

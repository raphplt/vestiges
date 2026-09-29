# Plan 05 — Catalogue d'objets V1, proposition révisée

29 septembre 2026 · **Proposition à valider par Raphaël avant toute création.** Révise la liste de 24 objets du [catalogue commun](05-catalogue-objets-perks.md#4-les-objets--24-effets-cumulables) (28 septembre), non adoptée. Complète le [catalogue des perks](05-perks-specialisations.md), dont sept règles sur neuf sont actives. Aucun code ni donnée de jeu modifiés par ce document.

Demande de Raphaël ([DECISIONS §28](DECISIONS.md)) : réviser la liste et créer les objets ; la récompense à choix d'objets se tient **hors des coffres, dans une source à part**, forme à déterminer.

## 1. Ce que les objets doivent faire, et ne pas faire

Trois couches, trois rôles distincts :

- **Armes** : la manière d'attaquer, et leurs signatures (feu, écho, rebond, onde sonore, temps, chaîne, perforation, contrôles).
- **Perks** (quatre, sans niveaux) : changent une règle de jeu.
- **Objets** (sans limite de nombre ni d'exemplaires) : portent la **puissance cumulée** et quelques interactions complémentaires. Ils reprennent les statistiques des quatorze passifs actuels (plan 05 §9), qui quittent le level-up.

Critères appliqués à chaque objet :

1. **Aucune signature d'arme ni de perk.** L'ancienne liste en reprenait cinq : l'Allumette imitait le feu de la Lampe à pétrole, le Morceau de miroir l'écho des Gants, la Bille fêlée les rebonds du Lance-billes et du Trousseau, la Cassette l'onde du Transistor et de la Cloche, les deux montres le Chronomètre. Ces cinq objets sortent, avec leurs mécaniques.
2. **Nom et silhouette distincts des 24 armes.** Pas de clé, d'aiguille, de montre ni d'appareil sonore. « Ressort de réveil » devient « Ressort de sommier », « Aimant de haut-parleur » devient « Aimant de frigo ».
3. **Chaque exemplaire compte.** Une pile ne doit jamais devenir inutile. Les plafonds (armure, critique) ont une règle d'excédent écrite.
4. **Cumul additif dans une même statistique**, jamais `1,03^n`. Les déclencheurs gagnent en puissance avec les copies, pas en probabilité ni en nombre de minuteurs ; aucun traitement par exemplaire.
5. **Objets du monde d'avant**, ordinaires, cohérents avec le registre des armes (plan 17) et le lore (plan 19). Un texte de souvenir court par objet viendra après validation.

## 2. Le catalogue proposé : 23 objets

Rareté fixe par objet (DECISIONS §7). `n` = exemplaires possédés. Coefficients = valeurs de départ, à mesurer.

### Communs — neuf renforts simples

| ID | Objet | Effet avec n exemplaires | Reprend |
|---|---|---|---|
| `wooden_wedge` | Cale en bois | +4 % × n dégâts | Flamme intérieure |
| `bed_spring` | Ressort de sommier | +3 % × n cadence | Mémoire vive |
| `coat_button` | Bouton de manteau | +6 × n PV max (PV courants augmentés d'autant) | Ancrage |
| `thread_spool` | Bobine de fil | +0,15 × n PV/s (fonctionne à PV pleins, utile à Prévoyance) | Régénération |
| `red_shoelace` | Lacet rouge | +2 % × n vitesse de déplacement | Instinct |
| `folding_ruler` | Mètre pliant | +4 % × n portée | Portée étendue |
| `copper_washer` | Rondelle de cuivre | +4 % × n taille des zones ; au-delà de la limite d'angle d'une arme, l'excédent devient des dégâts sur cette arme (catalogue commun §6) | Résonance |
| `fridge_magnet` | Aimant de frigo | +8 % × n rayon d'attraction de l'XP (élargit aussi le couloir de Sillage) | Siphon |
| `class_photo` | Photo de classe | +4 % × n XP reçue, appliquée une seule fois au gain | nouveau (axe XP) |

### Inhabituels — six orientations

| ID | Objet | Effet avec n exemplaires | Reprend |
|---|---|---|---|
| `fair_token` | Jeton de fête foraine | +0,05 × n Chance (qualité des améliorations et du butin, pas les procs) | Dons « lucky » |
| `reading_glasses` | Lunettes de lecture | +2 % × n chance de critique ; au-delà de 100 %, chaque tranche entière garantit un degré de critique supplémentaire (catalogue commun §6) | Œil critique |
| `stamp_loupe` | Loupe de philatéliste | +10 % × n dégâts critiques | Don « crit_damage » |
| `firefighter_patch` | Écusson de pompier | +5 × n bouclier max, chargé à l'acquisition | Carapace |
| `knee_pad` | Genouillère | +2 × n armure ; au-delà du plafond de 75 % (45 points), chaque point devient +1 PV max | Peau dure |
| `piggy_bank_shard` | Éclat de tirelire | +5 % × n Essence gagnée | nouveau (axe Essence) |

### Rares — six règles de combat, de soin ou de risque

| ID | Objet | Effet avec n exemplaires | Rôle |
|---|---|---|---|
| `sewing_thimble` | Dé à coudre | Chaque élimination attribuée rend 0,4 × n PV | Première source de soin en combat (plan 13 constate qu'il n'y en a aucune) ; nourrit Prévoyance |
| `plaster_tin` | Boîte de pansements | Chaque niveau gagné rend 2 % × n des PV max | Soin lié au rythme d'XP ; fort en cascade, faible en surplus lent |
| `dented_medal` | Médaille cabossée | Sous 35 % des PV : +12 % × n dégâts | Risque assumé ; se combine à Reprise |
| `worn_sole` | Semelle usée | Après 2 s de déplacement continu, la prochaine attaque d'une arme fait +15 % × n dégâts ; un arrêt de plus de 0,5 s l'annule | Récompense le nomade, sans toucher à la collecte (Sillage) |
| `faded_id_photo` | Photo d'identité effacée | Jusqu'à +8 % × n XP et Essence des créatures tuées en zone oubliée, pondéré par l'oubli du lieu de mort | Axe de l'oubli |
| `hole_punch` | Perforatrice | +1 × n perforation des projectiles ; la Cloueuse gagne +1 perforation de plus par exemplaire, pour garder son avance | Reflet brisé (voir question 4) |

### Épiques — deux effets structurants

| ID | Objet | Effet avec n exemplaires | Rôle |
|---|---|---|---|
| `carbon_paper` | Papier carbone | +1 × n copie d'attaque selon le motif de chaque arme (projectile, frappe, élément orbital, saut de chaîne, répétition agrégée pour l'onde et le cône ; table du catalogue commun §6) | Souffle du Néant ; à borner après banc de performance |
| `glass_paperweight` | Presse-papier en verre | En zone oubliée, jusqu'à +10 % × n dégâts et +4 % × n vitesse, selon l'oubli du lieu où se tient le joueur | Oser la lisière de l'Effacement, sans le neutraliser |

**Retirés de l'ancienne liste :**

- Allumette humide, Morceau de miroir, Bille fêlée, Cassette sans étiquette, Montre sans aiguilles, Chronomètre arrêté : signatures d'armes (§1).
- Badge d'atelier : sa riposte relève plutôt d'un futur perk ou d'une arme.
- Loupe fendue (taille des projectiles) : faible décision, et Calibre est absent du catalogue B.
- Verre de lunette : remplacé par Lunettes de lecture (chance de critique) et Loupe de philatéliste (dégâts critiques).

**Couverture vérifiée :** les treize passifs actifs ont une destination. Chance de critique, dégâts critiques, Chance, XP, attraction, Essence, soin, bouclier, armure, portée, zone et vitesse ont une source. Seul Fragment d'Éternité (désactivé) reste sans objet.

## 3. Synergies avec les perks

| Perk | Objets qui le nourrissent | Pourquoi |
|---|---|---|
| Prévoyance | Bobine de fil, Dé à coudre, Boîte de pansements, Bouton de manteau | Excédents de soin et capacité |
| Reprise | Dé à coudre, Médaille cabossée | Survivre blessé, tuer vite |
| Débordement | Cale en bois, Lunettes, Loupe de philatéliste | Surkills plus fréquents et plus gros |
| Convergence | Mètre pliant, Semelle usée | Tenir l'élite à portée, frapper fort en arrivant |
| Propagation | Rondelle de cuivre, Papier carbone | Plus de cibles contrôlées à la fois |
| Sillage | Aimant de frigo, Lacet rouge, Photo de classe | Couloir plus large, plus long, plus rentable |
| Seconde lecture | Jeton de fête foraine | Cartes plus rares à reporter |
| Délestage | Photo de classe (le bonus d'XP s'applique une fois à la conversion) | Arbitrer objet contre croissance |
| Habitude | Tous les communs | Concentrer une pile choisie |

Aucun bonus de set : ces liens naissent des effets eux-mêmes.

## 4. La source à part : la récompense à choix

Raphaël veut la récompense à choix **hors des coffres**. Proposition : reprendre le **Triptyque** du [plan 13](13-butin.md#b-le-triptyque--choisir) (non arbitré), sous le nom de **Reliquaire**.

- Un petit autel de mémoire à trois alcôves, silhouette distincte des coffres, des Autels, des Mémoriaux et des Failles.
- En approchant, les trois objets se révèlent. Le joueur en prend un ; les deux autres s'effacent (dissolution, son de perte). Choisir, c'est laisser oublier.
- C'est ici, et seulement ici, que s'appliquent Délestage (« Prendre » ou « Convertir : X XP ») et Habitude (alternative déjà possédée de même rareté, par carte).
- La rareté de chaque alcôve suit la Chance, et l'oubli de la zone.

Trois façons de le faire apparaître, cumulables. Recommandation : **A et B**.

| Option | Déclencheur | Intérêt | Limite |
|---|---|---|---|
| A — Posé par la génération | Reliquaires répartis sur la carte, plus riches près du front d'Effacement | Choix de trajet, cohérent avec l'idée B du plan 11 | Densité à régler avec les coffres et les POI |
| B — Laissé par un Souverain | À sa mort, un Reliquaire à sa place (le coffre rare garanti devient un Reliquaire) | Récompense lisible d'un combat difficile | Dépend de la fréquence des Souverains |
| C — Fin de Résurgence | Un Reliquaire au centre de l'arène | Respiration après la tension | Rythme dicté par les crises |

Les coffres gardent Essence, XP et armes. Ils perdent les anciens Dons appliqués d'office : ce circuit parallèle ferme quand les objets le remplacent.

Économie : cible de départ **un Reliquaire toutes les 60 à 90 s** d'exploration, soit 30 à 45 objets dans une run de 45 min. Dix Cales en bois donnent +40 % de dégâts, contre +22 % pour Flamme intérieure au niveau 5 aujourd'hui. À mesurer avec `tools/measure_run.sh` (objets par minute, raretés, puissance) avant d'affirmer quoi que ce soit.

## 5. Présentation

- **Carte d'objet** (Reliquaire) : icône, nom, rareté, effet de l'exemplaire, puis « possédés : n → n+1 » et l'effet total avant → après.
- **Pause** : inventaire d'objets défilant, quantité par objet, effet total.
- **HUD** : rien en plus, sauf l'éventuel compteur de Semelle usée à juger en jeu.
- **Bilan et Collection** : onglet « Objets ».
- **Icônes** : 16×16 comme les icônes actuelles de passifs, ou 32×32 comme les armes (question 3). Planche de proposition avant production, comme pour les perks.

## 6. Lots de création après validation

| Lot | Contenu | Vérification |
|---|---|---|
| O1 — Socle | `data/items/items.json` et loader ; inventaire par joueur agrégé par identifiant et par statistique ; contributions additives ; cumul de 1 000 exemplaires sans coût par pile | Banc de cumul, build, contrats |
| O2 — Effets | Les 23 objets branchés ; règles d'excédent (armure, critique, zone) ; migration des treize passifs hors du level-up, droits du profil conservés | Banc d'effets par objet, régressions, captures |
| O3 — Reliquaire | Source à part (options retenues), écran de choix, Délestage et Habitude activés | Captures, banc de choix, `measure_run` |
| O4 — Présentation et équilibre | Pause, bilan, Collection, icônes ; coffres sans Dons ; runs même graine avant/après | Mesures d'économie, banc dense machine calme |

## 7. Questions pour Raphaël

1. **Liste** : les 23 objets et les retraits te conviennent-ils ? Noms à changer ?
2. **Source** : Reliquaire (Triptyque du plan 13) avec les options A + B, ou une autre forme ?
3. **Icônes d'objets** : 16×16 comme les passifs, ou 32×32 comme les armes ?
4. **Perforation** : garder la Perforatrice (avec la compensation de la Cloueuse) ou laisser la perforation aux armes ?
5. **Papier carbone** : +1 copie par exemplaire sans limite, sous réserve du banc, ou une copie tous les deux exemplaires ?

## 8. Icônes de perks : planche de proposition

[Planche](planches/perks-icones-proposition-1.png), générée par `python3 tools/generate_perk_icons.py --sheet <fichier>` (modèles dans `tools/sprites/perks/icons.py`, pipeline SDF des icônes d'armes). Chaque perk est un **pin's émaillé** du monde d'avant : un cerclage de laiton, un émail, un motif en relief. C'est une catégorie visuelle distincte des armes (objets détournés en diagonale) et des futurs objets (objets du quotidien).

| Perk | Motif |
|---|---|
| Convergence | Réticule, couronne au centre |
| Débordement | Verre trop plein, l'or qui déborde |
| Propagation | Deux silhouettes reliées par une étincelle (le motif le plus faible, à retravailler si la direction plaît) |
| Prévoyance | Bocal fermé, rempli à mi-hauteur |
| Reprise | Cœur entouré d'une flèche qui revient |
| Sillage | Empreinte de chaussure suivie d'orbes |
| Délestage | Balance : un objet d'un côté, une orbe de l'autre |
| Seconde lecture | Carte gardée derrière celle qu'on joue, avec un signet |
| Habitude | Deux boutons identiques |

Deux variantes d'émail :
- **par famille** : rouge combat, vert survie, bleu collecte, violet récompenses. Risque : ces couleurs peuvent évoquer les raretés (vert inhabituel, bleu rare, violet épique) ;
- **commun** : bleu nuit pour tous.

Taille 32×32, lisible à ×1 et ×2 sur la planche. Rien n'est écrit dans `assets/` ni branché sur les cartes avant validation. Questions : direction pin's retenue ? Quelle variante d'émail ? Motifs à changer ?

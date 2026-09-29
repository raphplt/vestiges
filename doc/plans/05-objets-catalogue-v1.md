# Plan 05 — Catalogue d'objets V1, proposition 2

29 septembre 2026 · **Proposition à valider par Raphaël avant toute création.** Remplace la proposition 1 du même jour, jugée trop faite de multiplicateurs ([DECISIONS §29](DECISIONS.md)). Complète le [catalogue des perks](05-perks-specialisations.md), dont sept règles sur neuf sont actives. Aucun code ni donnée de jeu modifiés par ce document.

Demandes de Raphaël :
- réviser la liste et créer les objets (§28) ;
- plus de diversité et d'originalité, en gardant quelques multiplicateurs et les raretés ;
- s'inspirer de Megabonk sans copier ;
- armes et objets **pas tous débloqués par défaut** ;
- récompense à choix hors des coffres, forme à déterminer (§28, §29).

## 1. Rôle des objets et garde-fous

- **Armes** : la manière d'attaquer et leurs signatures (feu, écho, rebond, onde sonore, temps, chaîne, perforation, contrôles).
- **Perks** : quatre, changent une règle.
- **Objets** : s'accumulent sans limite. Ils portent la **puissance cumulée**, mais surtout des **manières de jouer** : où se tenir, quand bouger, quoi garder, quel risque prendre, comment lire la carte.

Megabonk, étudié ([liste d'objets](https://commonsensegamer.com/megabonk-items/), [classement](https://www.dexerto.com/wikis/megabonk/item-tier-list)), n'est pas intéressant pour ses objets mais pour leurs **ressorts** :

- conditions de posture (immobile, en mouvement, PV hauts ou bas, ennemis proches) ;
- contreparties (gain contre perte) ;
- effets indexés sur une autre ressource ;
- sauvetage unique ;
- objets qui agissent sur le monde (sanctuaires, coffres) ;
- déblocages par défis.

Ici, ces ressorts s'appliquent aux systèmes propres à Vestiges (Effacement, Essence, zones oubliées, POI, coffres, choix de niveau, dash). Aucun objet n'en reprend un de Megabonk, ni un nom, ni un effet.

Garde-fous :
- aucune signature d'arme ou de perk ;
- noms distincts des 24 armes ;
- chaque exemplaire compte, avec une règle écrite pour tout plafond ;
- cumul additif ;
- aucun traitement ni minuteur par exemplaire.

Retirés de la proposition 1 :
- la Perforatrice : la perforation reste aux armes, la Cloueuse garde son identité ;
- l'Éclat de tirelire et la Genouillère : multiplicateurs sans décision ;
- la Loupe de philatéliste : les dégâts critiques restent à l'arme et aux autres sources.

## 2. Le catalogue : 30 objets, quatre raretés

`n` = exemplaires possédés. **Accès** : **D** = disponible dès le départ, **Q** = débloqué par une quête du profil, **V** = acheté avec des Vestiges (monnaie méta déjà gagnée au bilan, mais qui ne se dépense encore nulle part). Coefficients, conditions et prix = valeurs de départ, à mesurer.

### Communs — le socle chiffré (9)

| ID | Objet | Effet avec n exemplaires | Accès |
|---|---|---|---|
| `wooden_wedge` | Cale en bois | +4 % × n dégâts | D |
| `bed_spring` | Ressort de sommier | +3 % × n cadence | D |
| `coat_button` | Bouton de manteau | +6 × n PV max | D |
| `thread_spool` | Bobine de fil | +0,15 × n PV/s, même à PV pleins | D |
| `red_shoelace` | Lacet rouge | +2 % × n vitesse | D |
| `folding_ruler` | Mètre pliant | +3 % × n portée et taille des zones | D |
| `fridge_magnet` | Aimant de frigo | +8 % × n rayon d'attraction de l'XP (et couloir de Sillage) | D |
| `class_photo` | Photo de classe | +4 % × n XP reçue | D |
| `chewing_gum` | Chewing-gum sous la table | Dash rechargé 6 % × n plus vite, sans descendre sous 0,6 s ; au-delà, l'excédent allonge le dash | D |

Ce sont les seuls multiplicateurs purs, avec le Jeton et les Lunettes : 11 objets sur 30.

### Inhabituels — une posture à tenir (8)

| ID | Objet | Effet avec n exemplaires | Accès |
|---|---|---|---|
| `fair_token` | Jeton de fête foraine | +0,05 × n Chance | D |
| `reading_glasses` | Lunettes de lecture | +2 % × n chance de critique ; au-delà de 100 %, degrés de critique (catalogue commun §6) | Q : 500 coups critiques en une run |
| `worn_sole` | Semelle usée | Après 2 s de déplacement continu, la prochaine attaque d'une arme fait +15 % × n dégâts ; un arrêt de 0,5 s l'annule | D |
| `camping_stool` | Tabouret de camping | Immobile depuis 1 s : +20 % × n cadence, qui retombe en 0,5 s dès qu'on bouge | Q : 300 éliminations sans bouger |
| `hi_vis_vest` | Gilet réfléchissant | +2 % × n dégâts par ennemi à moins de 120 px, jusqu'à 10 ennemis | Q : survivre 30 s entouré de 40 ennemis |
| `thermos` | Thermos | Au-dessus de 90 % des PV : +10 % × n dégâts | V |
| `dented_medal` | Médaille cabossée | Sous 35 % des PV : +12 % × n dégâts | Q : tuer un Souverain sous 20 % des PV |
| `spiral_notebook` | Carnet à spirale | Fouille des POI 20 % × n plus rapide ; chaque POI fouillé donne +2 × n Essence | V |

Le Thermos et la Médaille tirent en sens opposés : garder ses PV hauts, ou vivre dangereusement.

### Rares — un arbitrage à faire (9)

| ID | Objet | Effet avec n exemplaires | Accès |
|---|---|---|---|
| `sewing_thimble` | Dé à coudre | Chaque élimination attribuée rend 0,4 × n PV | D |
| `plaster_tin` | Boîte de pansements | Chaque niveau gagné rend 2 % × n des PV max | V |
| `worn_wallet` | Porte-monnaie usé | +1 % × n dégâts par tranche de 10 Essence **gardées** (jusqu'à +30 % × n). Dépenser aux Autels et Mémoriaux fait baisser le bonus | Q : garder 200 Essence d'un coup |
| `movers_belt` | Ceinture de déménageur | +1 % × n dégâts par tranche de 10 PV max au-delà de 100 | Q : atteindre 250 PV max |
| `torn_calendar` | Calendrier arraché | L'Effacement avance 5 % × n plus vite ; en échange, +8 % × n XP et Essence | Q : finir une run au Péril 3 |
| `road_map` | Carte routière pliée | Coffres, Reliquaires, Mémoriaux et Failles se révèlent dans le brouillard dans un rayon agrandi de 25 % × n | V |
| `crowbar` | Pied-de-biche | À l'ouverture d'un coffre, 15 % × n de chance d'une ligne de butin en plus ; chaque tranche de 100 % en garantit une | Q : ouvrir 100 coffres |
| `faded_id_photo` | Photo d'identité effacée | Jusqu'à +8 % × n XP et Essence des créatures tuées en zone oubliée, selon l'oubli du lieu | Q : 500 éliminations en zone oubliée |
| `scrap_notebook` | Cahier de brouillon | +1 × n relance de niveau à l'acquisition, puis +1 × n relance tous les 20 niveaux | V |

Le Porte-monnaie crée une vraie tension avec les Autels et les Mémoriaux. Le Calendrier accélère la fuite, c'est-à-dire la boucle principale du jeu, en échange de croissance. Le Cahier et le Pied-de-biche agissent sur les choix et le butin, pas sur les dégâts.

### Épiques — un effet qui structure la run (4)

| ID | Objet | Effet avec n exemplaires | Accès |
|---|---|---|---|
| `carbon_paper` | Papier carbone | +1 copie d'attaque tous les 2 exemplaires, selon le motif de chaque arme (projectile, frappe, élément orbital, saut de chaîne ; répétition agrégée pour l'onde et le cône) | Q : 50 000 éliminations cumulées |
| `glass_paperweight` | Presse-papier en verre | En zone oubliée, jusqu'à +10 % × n dégâts et +4 % × n vitesse, selon l'oubli du lieu où tu te tiens | Q : 5 min cumulées en zone très oubliée |
| `opening_locket` | Médaillon ouvrant | Un coup qui devrait te tuer efface un médaillon à ta place : tu restes à 1 PV, 1,5 s d'invulnérabilité, les ennemis proches sont repoussés. Un exemplaire consommé par sauvetage ; le Néant n'est pas concerné | V |
| `shoebox` | Boîte à chaussures | +2 % × n dégâts par objet **différent** possédé | Q : posséder 15 objets différents dans une run |

La Boîte à chaussures pousse à diversifier, l'inverse d'Habitude, qui pousse à concentrer. Le Médaillon est le seul objet qui se consomme : l'objet s'oublie à la place du joueur.

**Bilan :**
- 11 multiplicateurs purs, 19 objets à condition, contrepartie ou effet sur le monde ;
- 13 disponibles au départ, 11 par quête, 6 par achat.

Les treize passifs actuels ont chacun une destination : Flamme → Cale, Mémoire vive → Ressort, Ancrage → Bouton, Instinct → Lacet, Résonance et Portée étendue → Mètre, Siphon → Aimant, Régénération → Bobine, Œil critique → Lunettes, Carapace → bouclier des personnages (plan 03 §8), Souffle du Néant → Papier carbone. Deux restent sans objet : Peau dure (l'armure) et Reflet brisé (la perforation) ; l'armure reste une statistique de personnage, la perforation reste aux armes.

## 3. Déblocages : une réserve de départ, le reste mérité

Raphaël : armes et objets ne sont pas tous disponibles au départ ; le reste s'obtient par quêtes ou achats.

- **Objets :** 13 au départ (les communs, le Jeton, la Semelle, le Dé à coudre).
  - Les **quêtes** débloquent les objets qui récompensent un style : chaque condition entraîne ce que l'objet rend plus fort.
  - Les **achats** en Vestiges débloquent les objets de confort ou d'exploration. Prix de départ proposés : inhabituel 150, rare 400, épique 900 Vestiges. À caler sur les Vestiges gagnés par run, pas encore mesurés.
- **Lieu d'achat :** un comptoir du Hub (nom à trouver). La monnaie existe (`MetaSaveManager.Vestiges`, gagnée au bilan) mais ne se dépense nulle part.
- **Armes :** aujourd'hui 20 sur 24 sont disponibles d'emblée ; 4 dépendent d'un Souvenir de lore. Le même principe (réserve limitée, puis quêtes et achats) demande de choisir les armes de départ par personnage (plans 06 et 17). Ce n'est pas tranché ici.
- **Mode dev :** tout reste ouvert ; les droits déjà acquis d'un profil sont conservés.
- **Ce qu'on ne débloque que par une quête** est montré dans la Collection avec sa condition : le joueur sait quoi viser.

## 4. La source à part : récompense à choix

Toujours à déterminer (§29). Proposition inchangée : le **Reliquaire**, Triptyque du [plan 13](13-butin.md#b-le-triptyque--choisir) (non arbitré).
- Trois alcôves, un objet pris, les deux autres s'effacent.
- Il apparaît posé par la génération, plus riche près de l'Effacement, et laissé par chaque Souverain.
- Délestage et Habitude s'y appliquent.
- Les coffres gardent Essence, XP et armes, et perdent les anciens Dons.
- Cible de départ : un Reliquaire toutes les 60 à 90 s, à mesurer.

## 5. Présentation et création

- **Carte d'objet** : icône, nom, rareté, effet, « possédés : n → n+1 », effet total avant → après. Pour un objet à condition, la condition tient en une ligne.
- **Pause** : inventaire, quantités, effets totaux. Pour les objets à condition, leur état courant (actif ou non, bonus du moment).
- **Collection** : les objets verrouillés avec leur quête ou leur prix.
- **Icônes d'objets** : planche de proposition après validation de la liste.

Lots après validation :

| Lot | Contenu |
|---|---|
| O1 | Données, inventaire et agrégation, banc de 1 000 exemplaires |
| O2 | Multiplicateurs et objets à posture |
| O3 | Objets d'arbitrage et épiques, sauvetage du Médaillon |
| O4 | Reliquaire, Délestage, Habitude |
| O5 | Déblocages (quêtes, comptoir du Hub), Collection, bilan, icônes |

Chaque lot se termine par un banc, des captures et des mesures d'économie.

## 6. Questions pour Raphaël

1. Ces 30 objets, leur répartition (11 multiplicateurs, 19 mécaniques) et les retraits te conviennent-ils ? Des objets à changer ou des idées à ajouter ?
2. Le Calendrier arraché, qui accélère l'Effacement contre de la croissance, te semble-t-il dans l'esprit du jeu ?
3. Déblocages : 13 objets au départ, puis quêtes et achats en Vestiges au Hub. Le principe te va-t-il, et veux-tu le même pour les armes ?
4. Source à choix : Reliquaire, ou une autre idée ?

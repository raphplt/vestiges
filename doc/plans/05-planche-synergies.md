# Planche des synergies de règles (forme C)

4 octobre 2026 · Demandée aux §65 et §66 · **Proposition, rien n'est codé.**

## Ce qu'est la forme C

Une synergie de règles, ce n'est **pas** un bonus caché qui s'allume quand deux objets sont réunis, ni une annonce à l'écran (les objets ne disent pas avec quoi ils vont, §39). C'est une règle qui **produit** quelque chose (un statut, une élimination, un soin, un critique, un déplacement) et une autre qui **le lit**. Chaque règle tient seule et s'explique en une phrase ; le joueur découvre en jouant que l'une nourrit l'autre.

Le travail consiste donc à **raccorder** : vérifier que ce qu'une règle produit est bien lu par celles qui devraient le lire, et qu'aucune règle ne promet ce que le jeu ne fait pas.

Relevé du code au 4 octobre (`main`, e7130af6), avec fichiers et lignes : 24 armes, 34 objets, 9 spécialisations, 5 bonus lâchés. Les chiffres et emplacements sont dans le compte rendu de relevé ; cette planche n'en garde que les conclusions.

---

## 1. Ce qui se répond déjà

Ces liens marchent aujourd'hui, sans rien ajouter :

| Producteur | Lecteur | Exemple de jeu |
|---|---|---|
| Ralentissement d'arme (Cloche, Glas, Congère, champ du Chronomètre, Glaçon) | Épingle à nourrice (+dégâts aux ralentis), Propagation (passe au voisin à la mort), Pince à linge (renouvelle à l'expiration) | Cloche + Épingle + Propagation : la foule ralentie prend plus, et le ralentissement court de mort en mort |
| Désorientation d'arme (Polaroïd, Court-circuit, Tocsin, Surexposition, Fréquence pirate) | Propagation, Pince à linge, IA des créatures (capacités bloquées) | Polaroïd + Propagation : un flash désoriente une file entière |
| Brûlure (Allumette humide) | Thermomètre (+dégâts aux brûlants), palier de l'Allumette (la brûlure passe au voisin) | Allumette + Thermomètre |
| Critique (Lunettes de lecture) | Loupe (rend Fragile), Stylo à quatre couleurs (rebond) | Lunettes + Loupe : chaque critique prépare les coups suivants de toutes les armes |
| Soin, régénération comprise, même à PV pleins | Prévoyance (garde l'excédent) | Scalpel, Bobine de fil ou Paille tordue + Prévoyance |
| Blessure | Reprise (les éliminations rendent une part) | Reprise + armes de foule |
| Coup fatal surpuissant | Débordement (reporte l'excédent) | Parcmètre Contravention ou Lentille Foyer + Convergence (vise les élites) + Débordement |
| Marche continue | Semelle usée (charge), Sillage (collecte derrière) | un jeu nomade |
| Immobilité | Tabouret de camping (cadence) | Craies, feu de la Lampe, Boîte à musique : un jeu posté |
| Zone de l'écho des Gants et des formes des Craies | Rondelle de cuivre (plus grand) | Gants + Rondelle |

**Des règles qui se contredisent, et c'est voulu :** le recul de la Pelle à neige (Déblayer) éloigne la foule dont se nourrit le Gilet réfléchissant ; la Médaille cabossée veut peu de PV, Prévoyance et Reprise les remontent ; Semelle usée et Tabouret de camping ne vont pas ensemble. Ces tensions font les choix de build. Elles restent.

---

## 2. Ce qui ne se répond pas, et devrait

Chaque ligne est un raccord, pas un effet nouveau. Elle corrige une règle pour qu'elle fasse ce que son texte laisse attendre.

| # | Constat | Raccord proposé | Ce que ça relie |
|---|---|---|---|
| **R1** | Le feu au sol de la Lampe n'est pas une brûlure : le Thermomètre ne le lit pas, il ne se renouvelle pas, il ne passe pas au voisin | Une créature qui se tient dans le feu **brûle** (le statut ordinaire, sa durée habituelle) | Lampe ↔ Thermomètre, palier de l'Allumette, Pince à linge |
| **R2** | Fragile (« subit plus de dégâts de toutes tes armes ») ne touche pas les tics de brûlure et de saignement | Fragile s'applique aussi aux brûlures et saignements posés par le joueur | Loupe ↔ Râteau, Incision (Scalpel), Allumette, Lampe (avec R1) |
| **R3** | Le saignement n'a aucun lecteur ; la désorientation et le sommeil n'en ont aucun côté dégâts | L'Épingle à nourrice lit les ennemis **entravés** : ralentis, désorientés ou endormis. Nouveau texte : « Tes coups font plus de dégâts aux ennemis entravés. » | Épingle ↔ Polaroïd, Rallonge, Cloche Tocsin, Transistor pirate, Boîte à musique Berceuse |
| **R4** | Le sommeil de la Berceuse et l'arrêt sur image du Chronomètre ralentissent, mais n'arrêtent ni les coups au corps à corps ni la marche des tireurs | Un ennemi **figé** ne bouge pas et n'attaque pas, pour sa durée | rend vrais Berceuse et Arrêt sur image ; Épingle les lisait déjà |
| **R5** | « Coup d'arme » veut dire seulement le coup principal : l'écho des Gants et la forme des Craies ne déclenchent ni l'Allumette, ni le Glaçon, ni la Paille | **Tout ce qu'une arme fait est un coup de cette arme** (écho, forme, saut de chaîne), avec le coefficient de déclenchement de l'arme. **Les objets ne se déclenchent jamais entre eux** : le rebond du Stylo, l'explosion du Pétard, l'écho de la Rondelle et l'éclat du Mètre restent inertes | Gants ↔ Allumette, Glaçon, Paille ; Craies ↔ idem. La seconde moitié empêche les chaînes infinies |
| **R6** | Une élimination par l'écho, la forme, le feu, la brûlure ou le saignement ne compte ni pour le Dé à coudre ni pour le Pétard mouillé | **Une élimination par ce qu'une arme a fait est une élimination par cette arme** (même règle que R5). L'explosion du Pétard et le rebond du Stylo restent des objets : leurs morts ne réexplosent pas | Râteau, Lampe ↔ Dé à coudre, Pétard mouillé |
| **R7** | La Propagation dit transmettre ralentissement et désorientation ; son offre ignore le Glaçon (qui ralentit) et compte le Chronomètre en Arrêt sur image (qui ne fait que figer) | L'offre suit ce que l'équipement produit réellement | Propagation ↔ Glaçon |

**Ce qui reste sans lecteur, volontairement :** le recul. Le lire voudrait un effet nouveau (une créature projetée qui blesse ses voisines) : ce serait un objet de plus au catalogue, pas un raccord. À garder pour une prochaine vague d'objets, si tu le veux.

---

## 3. Combinaisons que ces raccords ouvrent

Rien n'est annoncé dans le jeu : ce sont des exemples de ce que le joueur pourra découvrir.

| Combinaison | Ce qui se passe | Raccords |
|---|---|---|
| **Lampe à pétrole + Thermomètre + Allumette humide** | Le sol en feu fait brûler ; les brûlants prennent plus ; une mort dans le feu passe la brûlure au voisin | R1 |
| **Râteau + Lunettes + Loupe** | Les critiques rendent Fragile ; les saignements en cours font plus mal | R2 |
| **Râteau Herse + Dé à coudre** | Les saignements longs finissent les créatures, et chaque mort soigne | R6 |
| **Lampe Nappe + Pétard mouillé** | Les morts dans le feu explosent ; l'explosion ne réexplose pas | R1, R6 |
| **Polaroïd + Épingle à nourrice + Propagation** | Le flash désoriente, l'Épingle punit les désorientés, la désorientation court de mort en mort | R3 |
| **Boîte à musique Berceuse + Chronomètre Arrêt sur image + Épingle** | Une couronne de créatures figées qui n'attaquent plus, et prennent plus | R3, R4 |
| **Gants de boxe + Glaçon + Épingle** | Chaque écho peut ralentir ; les ralentis prennent plus, échos compris | R5 |
| **Craies + Allumette + Rondelle de cuivre** | Les formes, plus larges, peuvent enflammer | R5 |
| **Rallonge Court-circuit + Épingle + Gilet réfléchissant** | Le fouet désoriente la foule autour de toi, que le Gilet transforme en dégâts | R3 |

---

## 4. Corrections relevées en passant

Ce sont des défauts, pas des choix de design ; ils seraient corrigés dans le même lot, sans question :
- le **cône du Transistor** soigne par la Paille tordue à chaque image physique, au lieu de suivre le rythme de ses impacts (10 par seconde et par ennemi) ;
- avec **Fréquence pirate**, le cône redonne une direction au hasard à chaque image aux créatures désorientées : elles tremblent sur place au lieu d'errer ;
- le **palier de l'Épingle** (prolonger le ralentissement des voisins) ne fait rien sur un voisin seulement figé ;
- le compteur du **Scalpel** n'est jamais remis à zéro quand l'arme est retirée.

---

## 5. Une découverte : les pouvoirs des personnages n'existent pas

Les fiches du plan 06 décrivent l'onde de la Forgeuse au quatrième coup, la marque du Traqueur qui passe à la mort de sa cible, et le Carnet de route du Vagabond. **Aucun n'est codé.** Chaque personnage n'a qu'une signature de stats au départ (Vagabond : vitesse et régénération ; Forgeuse : PV et armure ; Traqueur : portée et cadence). Le plan 05 (Convergence, Propagation) en tient compte comme s'ils existaient.

C'est là que les synergies de règles auraient le plus de sens : un pouvoir de personnage est une règle qui produit (une onde, une marque, une charge de biome) et que les objets peuvent lire.

---

## 6. Questions pour Raphaël

| # | Question | Options | Recommandation |
|---|---|---|---|
| 1 | **Les raccords R1 à R7** | A. Tous. B. Au choix, ligne par ligne. C. Aucun pour l'instant. | **A** : chacun rend une règle conforme à son texte, et aucun n'ajoute d'effet nouveau. |
| 2 | **Une règle commune pour « coup d'arme » et « élimination par une arme »** (R5, R6) | A. Tout ce que fait une arme compte pour elle ; les objets ne se déclenchent jamais entre eux. B. Seul le coup principal compte, comme aujourd'hui. | **A** : une seule règle, lisible, sans boucle possible. |
| 3 | **L'Épingle à nourrice lit les entravés** (R3) | A. Oui, ralentis, désorientés et endormis. B. Non ; un nouvel objet pour les désorientés, plus tard. | **A** : un raccord plutôt qu'un objet de plus. |
| 4 | **Les pouvoirs des personnages** (§5) | A. Les coder, dans un lot du plan 06 qui suit les raccords. B. Garder les signatures de stats et réécrire les fiches. C. Les repenser d'abord avec toi. | **A**, en commençant par la Forgeuse (le quatrième coup est le plus simple et le plus lisible). |
| 5 | **Le recul sans lecteur** | A. Rien pour l'instant. B. Un objet qui le lit, dans une prochaine vague d'objets. | **A**. |

## 7. Lot proposé après validation

**S1 — Raccords** (plan 05) : R1 à R7 selon tes réponses, et les corrections du §4.
- Vérification : un banc par raccord, avec chaque fois la combinaison qui le montre et sa contre-épreuve sans l'objet lecteur.
- Fixtures : un objet ne déclenche jamais un objet ; une explosion de Pétard ne réexplose pas.
- Suites objets, armes, perks et choix vertes ; captures du feu, du gel et du flash.
- Mesure au banc dense : R5 et R6 ajoutent des déclenchements.

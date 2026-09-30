# Plan 21 — Inventaire du jeu au 30 septembre 2026

Photographie de ce qui est en jeu au commit `71a90474`, demandée par Raphaël ([DECISIONS §36](DECISIONS.md)). Tirée des données (`data/`), pas de mémoire. La [référence](21-systeme-de-jeu.md) reste le document qui fait foi ; ce fichier ne décide rien.

## 1. Stats du joueur

### De départ, par personnage (`data/characters/characters.json`)

| Stat | Traqueur | Vagabond | Forgeuse |
|---|---|---|---|
| PV max | 70 | 100 | 120 |
| Régénération | 0,3 PV/s | 0,5 PV/s | 0,6 PV/s |
| Bouclier | 12 | 20 | 30 |
| Vitesse | 240 | 200 | 180 |
| Dégâts (facteur sur l'arme, 10 = × 1) | 12 | 10 | 9 |
| Cadence (facteur sur l'arme) | 1,2 | 1,0 | 0,9 |
| Portée (300 = × 1) | 380 | 300 | 250 |
| Portée d'interaction | 55 | 60 | 70 |
| Arme de départ | Arc du gymnase | Faucille | Parcmètre |
| Signature | +portée, +cadence | +8 % vitesse, +0,2 régén | +PV, +armure |

Défense commune (`data/characters/defense.json`) :
- Le bouclier encaisse un coup entier, puis se recharge en 2 s après 5 s sans coup.
- Chaque coup reçu donne 0,5 s d'invulnérabilité.
- Armure : réduction = armure / (armure + 15), plafonnée à 75 %.

### Liste des stats, montées par les objets

La fiche de la pause affiche aujourd'hui : PV, Régénération, Bouclier, Armure (réduction), Vitesse, Dégâts, Cadence, Critique (chance et multiplicateur), Portée, Zone, Durée, Aimant, Chance, Copies d'attaque, Perforation, Essence, Péril.

| Stat | Propriété | Montée par | Visible dans la pause |
|---|---|---|---|
| PV max | — | Bouton de manteau | oui |
| Régénération | — | Bobine de fil | oui |
| Bouclier | — | Écusson de pompier | oui |
| Armure | — | Genouillère | oui |
| Vitesse | Élan | Lacet rouge | oui |
| Recharge du dash | Élan | Lacet rouge | non |
| Dégâts (multiplicateur global) | Force | aucun objet (seulement les armes) | oui |
| Cadence | Fréquence | Ressort de sommier | oui |
| Chance de critique, dégâts critiques | Précision | Lunettes de lecture | oui |
| Portée | Portée | Mètre pliant | oui |
| Zone | Taille | Rondelle de cuivre | oui |
| Durée des statuts | Durée | Pince à linge | oui |
| Copies d'attaque et leur part des dégâts | Nombre | Papier carbone | oui, si > 0 |
| Rayon d'aimant | — | Aimant de frigo | oui |
| XP gagnée | — | Photo de classe | non |
| Chance | — | Jeton de fête foraine | oui |
| Chances de déclenchement et bonus contre une cible (Brûlure, ralentissement, explosion, soin…) | — | objets de déclencheur | non |

## 2. Armes (24)

- **Montée :** une amélioration = un niveau (50 au plus), qui tire au hasard une ou plusieurs stats parmi celles que l'arme peut monter.
- **Pas d'une commune :** dégâts +12 %, cadence +8 %, portée +6 %, zone +8 %, recul +15 %, vitesse de projectile ou d'orbite +10 %.
- **Gain selon la rareté :** inhabituelle × 1,5 ; rare : 2 stats × 1,25 ; épique × 1,75 ; légendaire : 3 stats × 2.
- **Stats entières :** « +1 projectile / perforation / saut / orbe » ne tombe **qu'en épique ou légendaire** (4 % des tirages), et seulement pour les armes de la colonne « Entier ».

| Arme | Motif | Dégâts | Cadence /s | Portée | Particularité | Entier possible | Ascension |
|---|---|---|---|---|---|---|---|
| Faucille | Mêlée, arc 90° | 10 | 1,0 | 60 | — | — | Moisson / Estoc |
| Parcmètre | Mêlée, arc 120° | 16 | 0,65 | 55 | Gros recul | — | — |
| Arc du gymnase | Tir | 8 | 1,2 | 150 | — | perforation | Volée / Transpercer |
| Lance-billes | Salve de 3 | 4 | 1,8 | 120 | — | projectile | — |
| Parapluie | Estoc | 7 | 1,5 | 45 | — | — | — |
| Cloueuse | Tir | 22 | 0,6 | 180 | Perfore 3 | perforation | — |
| Pelle à neige | Mêlée, arc 160° | 18 | 0,8 | 65 | — | — | — |
| Rallonge | Cercle | 9 | 0,9 | 80 | — | — | — |
| Assiettes | Salve de 2 | 11 | 1,1 | 140 | — | projectile | — |
| Râteau | Mêlée, arc 100° | 25 | 0,5 | 50 | Saignement 2/s, 4 s | — | — |
| Cloche d'école | Cercle | 12 | 0,7 | 100 | Ralentit de 50 %, 2 s | — | Glas / Tocsin |
| Scalpel | Estoc | 6 | 2,5 | 35 | Soigne tous les 5 coups | — | — |
| Lentille de phare | Tir | 30 | 0,4 | 250 | Perforation illimitée | perforation | — |
| Boîte à musique | 3 orbes | 7 | — | 90 | Orbite permanente | orbe | Ronde / Berceuse |
| Trousseau | Chaîne, 2 sauts | 14 | 0,8 | 55 | — | saut | — |
| Boussole | Tir guidé | 13 | 1,0 | 180 | — | projectile | — |
| Polaroïd | Salve de 5 | 18 | 0,5 | 130 | Désoriente 1,5 s | projectile | — |
| Baguette de sourcier | Tir guidé | 20 | 0,8 | 200 | Coûte 0,5 Essence par tir | projectile | — |
| Gomme | Mêlée, arc 70° | 15 | 1,8 | 50 | Efface la victime | — | — |
| Lampe à pétrole | Tir | 10 | 0,6 | 170 | Feu au sol, 0,3 Essence par tir | perforation | — |
| Gants de boxe | Mêlée, arc 90° | 11 | 1,4 | 45 | Écho de la frappe, 0,2 Essence par coup | — | — |
| Craies | 2 tirs guidés | 25 | 0,6 | 200 | Formes au hasard, éclat de zone | projectile | — |
| Transistor | Cône continu | 8 | 0,3 | 250 | Dégâts qui montent pendant l'émission | — | — |
| Chronomètre | Mêlée, arc 140° | 35 | 0,35 | 70 | Ralentit le temps autour de l'impact | — | — |

## 3. Objets (22 proposés, 6 emplacements, niveau 1 à 50)

Une amélioration d'objet donne 1 à 5 niveaux selon la rareté (commune 1, légendaire 5). La valeur d'un effet ne dépend que du niveau.

### Objets de propriété (14)

| Objet | Effet au niveau 1 | Niv 10 | Niv 25 | Niv 50 | Palier 25 |
|---|---|---|---|---|---|
| Ressort de sommier | Cadence +1,2 % | +12 % | +30 % | +60 % | Chaque 10ᵉ attaque part deux fois |
| Rondelle de cuivre | Zone +1,2 % | +12 % | +30 % | +60 % | Les zones refrappent à 30 % |
| Mètre pliant | Portée +1 % | +10 % | +25 % | +50 % | Un projectile en bout de course éclate |
| Lunettes de lecture | Critique +0,6 %, dégâts critiques +1 % | +6 %, +10 % | +15 %, +25 % | +30 %, +50 % | Critique sur PV pleins × 2 |
| Bouton de manteau | PV max +4 | +40 | +100 | +200 | Coups sous 3 % des PV max ignorés |
| Bobine de fil | Régénération +0,06 PV/s | +0,6 | +1,5 | +3 | Régénération × 2 pendant 3 s après une blessure |
| Genouillère | Armure +0,8 | +8 | +20 | +40 | Armure × 2 pendant le dash et 1 s après |
| Écusson de pompier | Bouclier +1,5 | +15 | +37,5 | +75 | Bouclier cassé : onde de recul |
| Lacet rouge | Vitesse +0,6 %, recharge du dash +1 % | +6 %, +10 % | +15 %, +25 % | +30 %, +50 % | Dash 30 % plus long |
| Aimant de frigo | Aimant +3 % | +30 % | +75 % | +150 % | 0,2 PV par orbe |
| Photo de classe | XP +1 % | +10 % | +25 % | +50 % | 3 Essence par niveau gagné |
| Jeton de fête foraine | Chance +1 % | +10 % | +25 % | +50 % | +1 relance tous les 15 niveaux |
| Papier carbone | 1 copie à 31 % des dégâts | 1 à 44 % | 2 à 65 % | 3 à 100 % | +1 copie (et au niveau 50) |
| Pince à linge | Durée +1,5 % | +15 % | +37,5 % | +75 % | Un statut expiré se renouvelle à 25 % |

### Objets de déclencheur (8 sur 19)

| Objet | Effet au niveau 1 | Niv 25 | Niv 50 | Palier 25 |
|---|---|---|---|---|
| Allumette humide | 6,4 % d'enflammer (25 % du coup par seconde, 3 s) | 16 % | 26 % | La Brûlure passe au voisin à la mort |
| Glaçon dans un mouchoir | 6,4 % de ralentir de 40 %, 1,5 s | 16 % | 26 % | Ralenti deux fois : figé 0,5 s |
| Thermomètre | +10,8 % contre une cible brûlée | +30 % | +50 % | Tes Brûlures ralentissent de 15 % |
| Épingle à nourrice | +10,8 % contre une cible ralentie | +30 % | +50 % | Un ralenti tué prolonge de 1 s ses voisins |
| Pétard mouillé | La victime explose : 21,6 % du coup fatal | 60 % | 100 % | L'explosion se répète |
| Dé à coudre | +0,12 PV par élimination | +0,6 | +1,1 | Une élite tuée rend 5 % des PV max |
| Semelle usée | Après 2 s de marche, prochaine attaque +16 % | +40 % | +65 % | Vaut pour deux attaques |
| Boîte de pansements | Chaque niveau soigne 1,06 % des PV max | 2,5 % | 4 % | Cascade de 3 niveaux : 1 s d'invulnérabilité |

Les chances de l'Allumette et du Glaçon sont multipliées par le coefficient de l'arme : de 0,3 (Transistor) à 1,4 (Chronomètre).

Retirés des offres, identifiants gardés : Flamme intérieure (+dégâts), Reflet brisé (+1 perforation), Fragment d'Éternité.

## 4. Coffres (`data/loot_tables/`)

| Coffre | Tirages | Contenu possible (poids) |
|---|---|---|
| Commun | 2 | Essence 4–9 (40), XP 15–35 (20), 1 niveau d'objet (20), une arme (20) |
| Rare | 2 | Essence 8–16 (34), XP 28–60 (18), 1–2 niveaux d'objet (18), une arme (22) |
| Épique | 3 | Arme (35), 2–3 niveaux d'objet (20), Essence 14–28 (20), XP 50–100 (10) |
| Mémoire | 2 | Souvenir (45), Essence 10–18 (35), XP 25–45 (20) |

Une arme trouvée alors que les quatre emplacements sont pris tombe au sol, à échanger.

# Plan 22 — La carte à explorer

30 septembre 2026 · **Direction validée par Raphaël ([DECISIONS §35](DECISIONS.md)) : feu vert, avec les arbitrages du §0.** Ouvert à sa demande ([DECISIONS §34](DECISIONS.md)). Conçu avec les [douze principes](../PRINCIPES-BUILD.md) et le [plan 21](21-systeme-de-jeu.md). Aucun code modifié par ce document.

**Ce plan regroupe au lieu d'ajouter.** Il absorbe le plan 13 (butin), l'idée B du plan 11 (butin à sauver de l'Effacement) et la remise en service des POI prévue au plan 18. Ces documents restent comme historique ; les décisions à prendre sont ici.

## 0. Arbitrages du 30 septembre

- **Feu vert** sur la direction : les décors deviennent des lieux.
- **Carte agrandie, surtout en hauteur.** Elle mesure aujourd'hui environ 25 600 × 6 400 px, soit quatre fois plus large que haute. Cible de départ : doubler la hauteur (12 800 px), largeur inchangée. À mesurer : coût de génération, nombre de décors, temps de traversée.
- **Un peu plus de lieux** que proposé : viser **85 à 90** au lieu de 70. Avec la carte agrandie, l'espacement moyen reste voisin de celui d'aujourd'hui.
- **Le Mémorial reste la pièce centrale** des lieux de choix : un autel à raviver, qui protège sa zone de l'Effacement et donne un bonus. Il est déjà en jeu (plan 17, vague 3).
- **Minimap : oui**, au lieu d'un simple écran de carte en dernier lot.
- Les ex-perks s'appellent les **Réminiscences** ; le système de build est fixé dans la [référence unique](21-systeme-de-jeu.md).

## 1. La demande et le constat

Raphaël veut une carte qui donne envie d'explorer et de ratisser, avec plus de lieux et de récompenses qu'aujourd'hui, mais **moins dense que Megabonk** pour garder l'immersion, et des lieux intégrés au lore. Les objets sont limités à 6 : l'intérêt d'explorer sur la durée doit donc venir **d'autre chose que l'accumulation d'objets** (Essence, améliorations d'armes, ou un système nouveau).

État mesuré dans le code et les données :

| Élément | Aujourd'hui |
|---|---|
| Carte | Environ 25 600 × 6 400 px ; à 200 px/s, 128 s d'est en ouest |
| Lieux fixes | 31 : 23 coffres, 5 Mémoriaux, 3 Failles |
| Événements | 5 micro-événements, un toutes les 60 à 90 s |
| POI | 6 types, désactivés (formes V1) |
| Essence | Sources : éliminations, coffres, événements, quêtes. **Un seul usage : les quatre services du Mémorial** |
| Décors reconnaissables | Fermes (puits, épouvantail, tracteur, pique-nique, corde à linge), carrières (wagonnets, veine de cristal, baraque), bâtiments urbains : **tous décoratifs** |
| Carte du joueur | Aucune minimap ni écran de carte ; pointeur vers 3 coffres au plus |

Deux manques : il y a peu de raisons de faire un détour, et l'Essence ne mène presque nulle part.

## 2. Principes du plan

1. **Le monde montre déjà ses lieux.** Un lieu est un décor du monde d'avant qui se souvient encore : un puits, une boîte aux lettres, un wagonnet. Pas d'icône flottante partout, pas de sanctuaire abstrait.
2. **Trois tailles, trois rythmes.** Des petits lieux fréquents (un geste, une petite récompense), des lieux de choix plus rares (une décision de build), des épreuves (un combat, une grosse récompense).
3. **Moins dense que Megabonk.** Cible de départ : un petit lieu en vue toutes les 20 à 30 s de marche, un lieu de choix toutes les 2 à 3 min. À mesurer et à baisser si la carte paraît encombrée.
4. **L'Effacement choisit ce qu'on perd.** Un lieu est plus riche près du front, et perdu quand sa zone s'efface. On ne peut pas tout prendre.
5. **Explorer nourrit les armes, pas un inventaire.** Voir §5.

## 3. Petits lieux : donner un rôle aux décors

Un geste court (maintien de 0,5 à 1 s), une récompense immédiate, aucune interface. Chaque lieu n'agit qu'une fois. Non visité, il porte un signe discret à moins de 400 px environ (une lueur, quelques particules d'Essence).

| Lieu | Décor | Ce qu'il donne | Lien au lore |
|---|---|---|---|
| Boîte aux lettres | Urbain, fermes | XP (une lettre jamais lue) | L'obsession du Facteur |
| Puits | Fermes (déjà généré) | Soin de 15 % | L'eau se souvient de la soif |
| Veine de cristal | Carrières (déjà générée) | Essence, à briser | Les « veines cristallines » de la Stratégie V2 §12 |
| Wagonnet | Carrières (déjà généré) | Essence, ou une arme au sol | Ce qu'on n'a pas eu le temps de remonter |
| Épouvantail | Fermes (déjà généré) | Petite embuscade, puis Essence | Il garde encore le champ |
| Voiture abandonnée | Routes | Fouille : Essence ou soin | Un départ interrompu |
| Cabine téléphonique | Urbain | Révèle les lieux proches (pointeurs de bord d'écran) | Une voix qui donne encore une adresse |
| Abribus | Routes | 20 s de vitesse | On y attendait ; on repart |
| Table de pique-nique | Fermes (déjà générée) | Soin léger et une ligne de lore | Un repas jamais fini |

Six sont des décors déjà générés ou faciles à placer sur les routes existantes. Cible : environ 25 par carte.

## 4. Lieux de choix et épreuves

| Lieu | Statut | Rôle |
|---|---|---|
| Coffre | Existe (23) | Essence, XP, arme. Les anciens Dons en sortent (plan 21) |
| Mémorial | Existe (5) | **La mémoire** : bénédiction, soin, lever un Oubli |
| Faille | Existe (3, plus celles des zones effacées) | Amélioration forte contre un Oubli et du Péril |
| **Reliquaire** | Nouveau (6 à 8) | **Les objets** : trois alcôves, une prise, deux effacées. Une fois les 6 objets pris, il propose des niveaux d'objets ou un remplacement (plan 21 §18) |
| **Atelier** | Nouveau (4) | **Les armes** : voir §5 |
| Épreuves | Existent (5 micro-événements, Souverains) | Combat, puis coffre, Essence, parfois une Réminiscence (nom provisoire des ex-perks) |

Total visé : environ 70 lieux au lieu de 31, dont 25 petits. La carte fait 164 millions de px² : cela laisse environ 1 500 px entre deux lieux en moyenne, soit 7 à 8 s de marche. Un écran en montre rarement plus de deux.

## 5. Ce qui garde l'exploration intéressante jusqu'au bout

Trois moteurs, tous tournés vers les armes et l'Essence plutôt que vers les objets.

### A. L'Atelier : l'Essence devient de la puissance d'arme

Un établi ou une forge abandonnée, dans un garage, une grange, une baraque de carrière. C'est le seul lieu qui travaille les armes :

- **Niveau d'arme** contre de l'Essence : le service quitte le Mémorial pour l'Atelier.
- **Retrempe** : relancer les stats de la dernière amélioration d'une arme, à rareté égale ou supérieure.
- **Trempe**, gratuite à la première visite : les 5 prochaines améliorations d'armes touchent **une stat de plus**. C'est l'idée du marteau de Megabonk, mais portée par un lieu et limitée dans le temps, pas par un objet permanent.

Les armes ont 196 niveaux à gagner sur une run. L'Essence et la Trempe restent donc utiles jusqu'à la fin, là où les objets plafonnent à 6. Explorer maintenant améliore les niveaux à venir (principe 6).

### B. Les Repères : la variété améliore tout le reste

Chaque **type** de lieu reconnu pour la première fois dans la run donne un Repère : un peu de Chance, donc de meilleures raretés sur toutes les améliorations. Douze types, douze Repères. Ce qui est récompensé, c'est de varier, pas d'enchaîner le même lieu.

### C. L'Atlas : ce qu'on découvre reste, d'une run à l'autre

Un onglet de la Collection. Chaque type de lieu, puis chaque lieu remarquable, y inscrit une ligne de lore la première fois. Compléter une page débloque une arme ou un objet : c'est une des voies de déblocage de la référence (plan 21 §10) ([DECISIONS §29](DECISIONS.md)). Une partie ratée laisse quand même une page avancée (principe 12).

## 6. Lien avec l'Effacement

- **Plus riche près du front.** La quantité et la rareté d'un lieu montent avec l'oubli de sa zone, comme pour les améliorations aujourd'hui.
- **Perdu avec sa zone.** Un lieu englouti disparaît. Il laisse une **trace** pâle, visible un moment : le joueur sait ce qu'il a laissé oublier (principe 12).
- **Stabiliser.** Ouvrir un coffre stabilise déjà sa zone ; les lieux de choix font de même. Visiter, c'est ralentir un peu l'oubli là où l'on passe.

## 7. Lisibilité sans casser l'immersion

- Les lieux sont des décors. Leur signe d'activité ne se voit que de près.
- De loin, seuls les lieux de choix ont un repère : la colonne de lumière des coffres, à la couleur de leur famille.
- Pointeurs de bord d'écran limités à 3, comme aujourd'hui ; la Cabine téléphonique en ajoute pour un temps.
- **Écran de carte** à la demande, montrant les lieux découverts et l'avancée de l'Effacement : proposé en dernier lot, à juger après essai.

## 8. Vérification par les principes

| Principe | Où il est tenu |
|---|---|
| 1. Fonction claire | Mémorial = mémoire, Atelier = armes, Reliquaire = objets, petits lieux = récolte |
| 2 et 4. Valeur selon le build, renforcement | La Trempe vaut plus pour une arme à nombreuses stats ; l'Essence vaut plus avec un Porte-monnaie |
| 5. Régime | Atelier et Reliquaire changent la suite de la run |
| 6. Immédiat contre futur | Détour vers un Atelier contre fuite ; Trempe pour plus tard contre soin maintenant |
| 7. Hasard orienté | Placement aléatoire, types reconnaissables, Cabine qui révèle |
| 8. Contraintes | L'Effacement : on ne peut pas tout visiter |
| 9. Conséquences visibles | Récompense immédiate, stat en plus visible sur les cartes d'amélioration |
| 10. Croissance sous tension | Lieux riches près du front |
| 11. Plusieurs chemins | Builds d'Essence, de Chance (Repères), d'armes (Trempe), de survie (puits, Mémorial) |
| 12. Hypothèses | Traces des lieux perdus, pages de l'Atlas |

## 9. Lots

| Lot | Contenu | Vérification |
|---|---|---|
| C0 | **Mesure de départ** : lieux rencontrés et visités par minute, Essence gagnée et dépensée, avec `tools/measure_run.sh` étendu | Chiffres de référence avant tout changement |
| C1 | Trois petits lieux sur des décors déjà générés (Puits, Veine de cristal, Épouvantail) ; socle commun des petits lieux | Captures, banc, mesure de densité |
| C2 | Atelier : niveau d'arme, Retrempe, Trempe | Banc, captures, mesure de l'Essence dépensée |
| C3 | Reliquaire, avec les objets du plan 21 (lot G2) | Dépend du catalogue d'objets |
| C4 | Les six autres petits lieux, Repères | Mesure : types visités par run |
| C5 | Traces des lieux effacés, Atlas | Captures, essai de Raphaël |
| C6 | Carte agrandie en hauteur ; **minimap** (lieux découverts, front de l'Effacement) | Mesure du coût de génération, banc, captures |

## 10. Idées reprises d'autres plans, à placer

Pour qu'elles ne se perdent pas ([tableau de bord §4](TABLEAU-DE-BORD.md)) :

- **Vestige figé** (plan 13 §4 A) : un objet flottant à ramasser sans menu. Peut devenir la forme d'un niveau d'objet trouvé.
- **Pacte d'oubli** (plan 13 §4 C) : oublier un objet contre un meilleur. Peut devenir le « remplacement » du Reliquaire.
- **Source de soin** (plan 13 §8) : couverte par le Puits, la Table de pique-nique et le Mémorial.
- **Marchand ambulant, escorte, événements de biome** (plan 12 §6) : épreuves à ajouter après C4.
- **Fragment de lore près des scènes-récits** (plan 08) : c'est la Table de pique-nique et l'Atlas.
- **Établi de grand-père** (plan 21 §16) : remplacé par la Trempe de l'Atelier, si Raphaël valide.

## 11. Questions pour Raphaël

1. Donner un rôle aux décors existants plutôt que créer des sanctuaires : est-ce la bonne direction pour l'immersion ?
2. L'Atelier et la Trempe (une stat de plus sur les 5 prochaines améliorations) comme moteur d'exploration tourné vers les armes : ça te convient ?
3. Déplacer le service « niveau d'arme » du Mémorial vers l'Atelier ?
4. Environ 70 lieux au lieu de 31 : la bonne échelle, ou plus prudent pour commencer ?
5. Les Repères et l'Atlas : on les garde, ou on simplifie ?

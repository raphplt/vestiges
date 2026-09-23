# Plan 06 A — Fiches du casting proposé

Version 0.2 · 23 septembre 2026 · Statut : **validé par Raphaël** (six identités ; Vagabond initial). Aucun chiffre ci-dessous n'est un équilibrage validé : ce sont des valeurs d'essai.
Références : [plan 06 §3](06-personnages-quetes-defis.md#3-casting-proposé), [plan 08](08-direction-artistique.md), [Bible §6.1](../VESTIGES-BIBLE.md), [plan 01 lot E](01-deplacements.md#lot-e--mobilités-de-personnages-saut-et-glissade), [plan 11](11-mecaniques-originales.md).

## 1. Principes communs

- **Six identités, six manières de se souvenir.** Chaque personnage réagit à l'oubli d'une façon qui se voit dans sa silhouette, son arme, sa mobilité et sa mécanique. Le décalage vient du métier ou de l'obsession, jamais d'une parodie.
- **Même bouton de mobilité pour tous** (Espace / X). Le dash commun validé reste la base ; chaque personnage le **module** ou le remplace par une variante lisible au même bouton (plan 01 E).
- **Une différence mécanique observable** au-delà des statistiques, perceptible dans la première minute.
- **Répartition des rôles :** deux mêlée, trois distance, un hybride. Cela suit ta demande d'augmenter la place du combat à distance, sans supprimer la mêlée.
- **Silhouettes distinctes à taille de jeu.** Si deux silhouettes se confondent en aplat noir à 100 % et 50 % de zoom, l'une est redessinée (Bible §11.4).
- **Pas de retour du craft.** Les perks exclusifs hérités de la Forgeuse (`forgeuse_quick_craft`, `forgeuse_reinforce`, `forgeuse_last_wall`, `forgeuse_recycler`) décrivent encore de la construction V1 ; ils sont à remplacer dans la refonte.

## 2. Vue d'ensemble

| # | Personnage | Rôle | Arme de départ | Mobilité | Mécanique signature | Accès proposé |
|---|---|---|---|---|---|---|
| 1 | Le Vagabond | Polyvalent, mêlée à allonge | Lame Ébréchée | Dash commun | Carnet de route : chaque biome traversé renforce | **Initial (recommandé)** |
| 2 | Le Traqueur | Distance, précision, fragile | Arc de Fortune | Pas de côté : deux charges courtes | Empreinte : marque qui se transmet | 200 éliminations en une run |
| 3 | La Forgeuse | Mêlée lourde, tenace | Marteau Lourd | Bond lourd : impact à l'atterrissage | Redresser : chaque 4ᵉ coup est une onde | 15 min actives en une run |
| 4 | L'Éveillée | Distance, Essence, contrôle | Bâton d'Essence | Déphasage : laisse une rémanence | Double : la rémanence répète l'attaque suivante | 2 Résurgences + 2 Autels en une run |
| 5 | Le Facteur sans destination | Distance mobile, rebonds | Sacoche de lettres (nouvelle) | Glissade sur patins | Tournée : élan qui monte tant qu'il avance | Caches ouvertes dans deux biomes |
| 6 | La Scaphandrière sans mer | Hybride, résistante, zone | Fusil-harpon (nouveau) | Saut flottant court | Réserve d'air : tient dans l'Effacement | Vaincre un boss de famille + défi de terrain |

Colosse et Ombre (V2/Bible) restent des alternatives si un concept échoue au test de silhouette ou de fun.

## 3. Fiches

### 1. Le Vagabond — « Tant que je marche, le chemin existe. »

- **Silhouette :** moyenne, équilibrée, sac à dos volumineux qui déborde (piquets, gamelle, carte roulée), capuche. Lecture immédiate : le sac dépasse de la tête.
- **Personnalité :** tenace, sans passé revendiqué. Recoud sa veste avec des bouts ramassés sur la route.
- **Motif sonore :** cliquetis du sac à chaque pas, gamelle qui tinte au dash.
- **Arme de départ :** Lame Ébréchée, avec l'allonge revue par le plan 05.
- **Passif chiffré — Carnet de route :** à la première entrée dans chaque nouveau biome de la run, +6 % dégâts et +4 % vitesse de déplacement, cumulables. Valeurs d'essai.
- **Mobilité :** dash commun validé, sans modification. C'est la référence.
- **Contrepartie :** aucun bonus au départ ; sa force vient du voyage.
- **Synergies :** objets de vitesse (le bonus nourrit l'élan) ; Autels (plus de zones traversées, plus d'occasions).
- **Faiblesse :** moins fort s'il reste dans un seul biome ; aucune défense particulière.
- **Différence observable :** la jauge de carnet se remplit visiblement à chaque frontière de biome. Elle récompense le « toujours avancer » de la V2.

### 2. Le Traqueur — « L'auteur des empreintes n'existe plus. Je les suis quand même. »

- **Silhouette :** fine, allongée, capuche pointue, arc plus haut que lui dans le dos, posture penchée en avant. La plus verticale du casting.
- **Personnalité :** nerveux, obsessionnel, parle aux traces.
- **Motif sonore :** corde qui vibre comme une voix (lore de l'Arc de Fortune).
- **Arme de départ :** Arc de Fortune.
- **Passif chiffré — Empreinte :** la première cible touchée est marquée et subit +20 % de dégâts. À sa mort, la marque passe à l'ennemi le plus proche dans un rayon de 200 px. Remplace Œil d'Aigle ; la portée +20 % passe dans les stats de base.
- **Mobilité — Pas de côté :** deux charges d'un dash plus court (60 % de la distance) et plus vif (100 ms), recharge par charge de 1,6 s. Valeurs d'essai.
- **Contrepartie :** 70 PV, la vie la plus basse ; aucune protection au contact.
- **Synergies :** perforation (la marque se propage plus vite) ; critiques.
- **Faiblesse :** encerclement au contact ; peu de contrôle de foule.
- **Différence observable :** la marque visible et sa transmission guident le choix des cibles ; les deux charges permettent un zigzag.

### 3. La Forgeuse — « Tout ce qui est tordu peut se redresser. Même le monde. »

- **Silhouette :** trapue et large, tablier de cuir, lunettes de soudure sur le front, marteau trop gros porté sur l'épaule. La plus large du casting.
- **Personnalité :** têtue, chaleureuse, persuadée que l'Effacement est un défaut de fabrication.
- **Motif sonore :** enclume assourdie, souffle de forge à l'impact.
- **Arme de départ :** Marteau Lourd.
- **Passif chiffré — Redresser :** chaque 4ᵉ coup de mêlée déclenche une onde circulaire de 70 px, à 60 % des dégâts, avec un léger recul. Remplace `forgeuse_fortify` et les perks de construction.
- **Mobilité — Bond lourd :** saut court (plan 01 E), de 0,35 s. À l'atterrissage, impact de 50 px, à 50 % des dégâts. Ne franchit ni mur ni Néant, et n'accorde aucune immunité générale.
- **Contrepartie :** vitesse 180, la plus lente ; recharge du bond de 3,5 s.
- **Synergies :** bonus de zone et de portée (plan 05) ; recul et étourdissement.
- **Faiblesse :** tireurs et menaces à distance, lenteur face aux charges.
- **Différence observable :** le rythme « trois coups puis onde » et l'impact d'atterrissage transforment la mobilité en attaque.

### 4. L'Éveillée — « J'entends ce lieu tel qu'il était, tel qu'il est et tel qu'il aurait pu être. »

- **Silhouette :** éthérée, vêtements amples qui flottent dans un vent inexistant, lueur faible aux mains. Seule silhouette au contour « double » : un léger décalage visible en permanence.
- **Personnalité :** calme, distraite, répond parfois à des questions qu'on ne lui a pas posées.
- **Motif sonore :** accord de verre, léger écho décalé sur ses propres sons.
- **Arme de départ :** Bâton d'Essence, dont l'accès devient direct pour elle (plan 06 B).
- **Passif chiffré — Double :** après une mobilité, une rémanence reste 1,5 s au point de départ et répète l'attaque suivante à 50 % des dégâts. C'est le prototype de rémanence offensive proposé au plan 11.
- **Mobilité — Déphasage :** déplacement bref, identique au dash commun en distance, sans traversée de mur. La rémanence reste au point de départ.
- **Contrepartie :** 80 PV, dégâts de base modérés ; sa puissance dépend du bon usage de la mobilité.
- **Synergies :** multi-projectiles (la rémanence les répète) ; réduction de recharge de mobilité.
- **Faiblesse :** peu efficace si le joueur n'utilise pas la mobilité ; Essence en tête-à-tête contre les ennemis résistants.
- **Différence observable :** chaque esquive laisse une tourelle éphémère, ce qui pousse à esquiver de façon offensive.

### 5. Le Facteur sans destination — « Il y a forcément quelqu'un qui attend ces lettres. »

- **Silhouette :** casquette de facteur, sacoche en bandoulière qui déborde de lettres, patins bricolés aux pieds (planches et roulements). Posture inclinée vers l'avant, comme un patineur.
- **Personnalité :** obstiné, tendre, salue les ruines. Refuse d'admettre que les adresses n'existent plus.
- **Motif sonore :** roulement des patins, froissement de papier, sonnette de vélo déformée.
- **Arme de départ — Sacoche de lettres (nouvelle arme, plan 05) :** lance une enveloppe qui rebondit jusqu'à trois ennemis proches. Chaque rebond « cherche une adresse » : il vise l'ennemi le plus éloigné dans un rayon de 150 px.
- **Passif chiffré — Tournée :** tant qu'il se déplace sans s'arrêter, l'élan monte jusqu'à +25 % de vitesse et +15 % de cadence en 4 s. Un arrêt de plus de 0,5 s ou une touche remet l'élan à zéro.
- **Mobilité — Glissade sur patins :** engagement plus long (0,4 s), direction ajustable mais virage moins vif, sortie toujours contrôlable (plan 01 E). La glissade conserve l'élan.
- **Contrepartie :** virages larges pendant la glissade ; dégâts unitaires faibles.
- **Synergies :** objets de rebond et de perforation ; bonus de vitesse.
- **Faiblesse :** espaces étroits, terrain lent (l'eau casse l'élan), obligation de rester en mouvement.
- **Différence observable :** jouer à l'arrêt est nettement moins efficace, et la jauge d'élan se voit sur le personnage (lettres qui s'envolent).

### 6. La Scaphandrière sans mer — « Ma mer a été oubliée. Moi, non. »

- **Silhouette :** casque de plongée rond à hublot, bottes lestées, tuyau d'air qui pend jusqu'à une bouteille dans le dos. Seule silhouette ronde en haut ; lecture immédiate grâce au casque.
- **Personnalité :** silencieuse, méthodique, respire fort ; cherche la côte qui manque sur toutes les cartes.
- **Motif sonore :** bulles, respiration dans le casque, sonar lointain.
- **Arme de départ — Fusil-harpon (nouvelle arme, plan 05) :** tir perforant lent, à longue portée. Le harpon ramène vers elle le premier ennemi léger touché, ou tire la Scaphandrière vers un ennemi lourd.
- **Passif chiffré — Réserve d'air :** la jauge d'air permet de rester 6 s dans les zones effilochées ou effacées sans subir leurs dégâts. Elle se recharge hors de ces zones, en 10 s. Lien direct avec le prototype « butin à sauver de l'Effacement » (plan 11).
- **Mobilité — Saut flottant :** saut court de 0,5 s, avec contrôle aérien limité et ombre au sol lisible (plan 01 E). Franchit l'eau peu profonde, pas l'eau profonde, les murs ni le Néant.
- **Contrepartie :** vitesse 190, cadence lente.
- **Synergies :** objets de perforation ; exploration près du front d'Effacement.
- **Faiblesse :** masses rapides au contact ; cadence lente contre les groupes.
- **Différence observable :** seule personnage qui peut s'attarder dans l'Effacement, ce qui change le trajet et les détours.

## 4. Planche de silhouettes : contrat proposé avec 08

Présenter les six silhouettes côte à côte à taille de jeu : aplat noir, puis couleur, sur sol ancré et sur sol effacé.

- **Hauteur relative proposée :**
  - Forgeuse 0,9 (plus large) ;
  - Vagabond 1,0 ;
  - Traqueur 1,1 ;
  - Éveillée 1,0 avec contour flottant ;
  - Facteur 0,95 incliné ;
  - Scaphandrière 1,0, casque plus large.
- **Couleur d'accent unique par personnage :** Vagabond ocre, Traqueur vert mousse, Forgeuse orange braise, Éveillée cyan Essence, Facteur bleu postal délavé, Scaphandrière laiton. Le vert-acide reste réservé aux créatures.
- La planche n'est produite qu'après validation des identités ci-dessus, pour éviter de dessiner des personnages refusés.

## 5. Décisions demandées à Raphaël

1. Valider, modifier ou remplacer chacune des six identités.
2. Choisir le personnage initial. Recommandation : le Vagabond, conformément à la V2 ; le Traqueur actuel reste débloqué pour les profils existants.
3. Accepter deux nouvelles armes de départ (Sacoche de lettres, Fusil-harpon), à intégrer au plan 05.
4. Valider le principe des mobilités modulées au même bouton, avant que 01 E ne les prototype.

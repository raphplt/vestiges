# Plan 32 — Sprites des héros à venir

Version 0.1 · 10 octobre 2026 · Demande de Raphaël : « j'aimerai bien que tu prépares les sprites des prochaines héros. Base toi sur le lore et les éléments écrits » ([DECISIONS §85](DECISIONS.md)).

Références : [lore v1.1 §5](../VESTIGES-LORE.md#5-les-personnages-jouables) (qui ils sont, ce qu'ils ont fait la nuit du 14), [fiches du casting](06-fiches-casting.md) (silhouettes, accents), [plan 25 §8](25-sprites-et-design.md#8-personnages-joués-et-projectiles-du-joueur--3-octobre-2026-decisions-60) (proportions et animations des trois personnages joués), [charte §4](../CHARTE-GRAPHIQUE.md).

## 1. État

- Jouables, au niveau du plan 25 (proportions stylisées, visage, signature surdimensionnée, animations à 8 images de marche) : Vagabond, Traqueur, Forgeuse.
- Prototypes anciens, jamais exportés (proportions réalistes, pas de visage, animations communes) : Éveillée, Facteur, Scaphandrière (`tools/sprites/characters/`). Le tableau de bord les liste dans « validé, pas encore fait » avec leurs sprites.
- Sans modèle : les quatre ajouts du lore (Sonneur, Écolière, Photographe, Veilleuse) et l'Enfant du Bas-Port, variante du Vagabond.

Aucun de ces héros n'est dans `characters.json` : les sprites sont préparés, rien n'est branché. Mécaniques, armes de départ et accès restent aux plans 06, 01 E et 05.

## 2. Principes

- **Même gabarit que les trois joués** : tête ×1,15 par rapport au « stylisé », corps raccourci, visage lisible (yeux, barbe ou coiffe), cadre élargi seulement pour la signature, pieds au même point (pivot à 4 px du bas). Animations des personnages joués (`playable_animations`), allure propre à chacun.
- **Une signature surdimensionnée par héros, tirée du lore**, lisible en aplat noir à l'échelle du jeu, sans se confondre avec les trois autres.
- **Couleur dominante et accent uniques** (fiches §4, charte §4) ; le vert-acide reste aux créatures.
- Leur nom n'apparaît nulle part : seulement leur fonction (lore §5).

## 3. Designs

| Héros | Ce que dit le lore | Signature | Dominante / accent |
|---|---|---|---|
| **L'Éveillée** (Mireille Aymard) | Standardiste de la mairie ; a reçu les appels et ne les a pas passés ; répète les voix | Casque d'opératrice à cornet devant la bouche, cordons du standard qui flottent autour d'elle, fiches de laiton au bout ; robe longue et gilet qui flottent ; un écho décalé de sa silhouette | Blanc-bleu / cyan Essence |
| **Le Facteur** (Aimé Ribot) | Distribue le courrier du Bas-Port à des adresses rayées ; une lettre qu'il ne lance jamais | Casquette des postes à visière et insigne, moustache, sacoche de cuir énorme qui déborde de lettres et de liasses ficelées, lettres qui s'envolent derrière lui, patins bricolés, penché en avant | Bleu postal délavé / jaune de sacoche |
| **La Scaphandrière** (Solange Delmas) | Scaphandrière du chantier naval ; a remonté vingt-neuf corps ; plonge encore là où il n'y a plus d'eau | Casque de cuivre rond à hublot grillagé et vert-de-gris, pèlerine boulonnée, bouteille et tuyau, ceinture de plombs, semelles de plomb, filin enroulé | Toile grise / laiton et cuivre |
| **Le Sonneur** (Baptiste Roux) | Empêché de sonner le tocsin ; l'a sonné ensuite chaque 14 novembre | Corde de cloche enroulée en bandoulière, bout coupé effiloché, cloche de bronze dans le dos, béret, long manteau sombre | Noir bleuté / bronze |
| **L'Écolière** (Lise Garnier) | Neuf ans ; attendait à l'école ; ses parents ne sont pas venus | La même fillette que l'écho de l'écolière : nattes, blouse d'école, col blanc, gros cartable, craie rose à la main ; taille d'enfant | Bleu marine / rose craie |
| **La Photographe** (Claire Morel) | Trois photos prises du clocher, jamais développées | Appareil à soufflet tenu devant elle, flash à réflecteur rond, béret, imperméable, bandoulière à pellicules | Bordeaux / argent du flash |
| **La Veilleuse** (Jeanne Oriol) | La seule porte ouverte ; très âgée, lente, ne combat bien qu'à l'arrêt | Veuve en noir, châle vert (la couleur de sa porte) sur la tête et les épaules, dos voûté, lampe à pétrole allumée | Noir / vert de porte, flamme |
| **L'Enfant du Bas-Port** (Élie, six ans) | La nuit du 14 : il pleut, la mer est là | Ciré jaune de pêcheur et suroît, bottes de caoutchouc, écharpe orange du Vagabond trop grande pour lui | Jaune ciré / orange |

## 4. Lots

| Lot | Contenu | État |
|---|---|---|
| **H1** | Éveillée, Facteur, Scaphandrière : reprise des prototypes au niveau des trois joués | Livré, à valider |
| **H2** | Sonneur, Écolière, Photographe : nouveaux modèles | Livré, à valider |
| **H3** | Veilleuse, Enfant du Bas-Port : nouveaux modèles | Livré, à valider |

Chaque lot : modèles, aperçus quatre vues et animations, planche avant/après à l'échelle du jeu à côté des trois joués et d'un Rôdeur, aplats noirs, puis sprites écrits dans `assets/characters/<id>/` (8 directions, idle, marche, dash, coup, mort) avec leurs `.import`.

## 5. Vérification

- Aperçus ×4 en cinq vues, regardés ; animations (marche, dash, mort) regardées en planche.
- Échelle du jeu : à côté des trois joués et d'un Rôdeur, sur sol clair et sombre ; aplats noirs distincts.
- Génération deux fois de suite : PNG identiques octet pour octet.
- Smoke test : nouveaux PNG importés, aucun supprimé.

## 6. Compte rendu H1 — Éveillée, Facteur, Scaphandrière (10 octobre)

[Planche avant/après](planches/32-h1-heros.png) (cinq vues ×4, échelle du jeu ×2 à côté des trois joués et d'un Rôdeur, aplats noirs), [animations](planches/32-h1-animations.png) (SE et NW, toutes les actions).

| Héros | Avant (prototype) | Proposé |
|---|---|---|
| **Éveillée** | cloche blanche sans visage, pans « écho » | standardiste : écouteur et serre-tête, cornet de laiton sur la poitrine ; deux cordons du standard qui s'envolent des mains, fiche de laiton et lueur cyan au bout ; cheveux longs qui flottent vers le haut, emportés du même côté que l'ourlet de la robe plissée ; gilet, col blanc, mains lumineuses |
| **Facteur** | homme bleu à casquette, sacoche petite | casquette des postes à insigne doré, grosse moustache grise, tête droite malgré la posture de patineur ; sacoche de cuir jaune énorme dont le bouquet de lettres s'évase vers l'extérieur, liasse ficelée de rouge, lettres envolées ; patins à roues de métal qui le surélèvent |
| **Scaphandrière** | casque rond, toile olive | casque de cuivre à grand hublot cerclé de laiton et reflet, hublots latéraux, robinet, taches de vert-de-gris à l'arrière ; pèlerine boulonnée ; toile sable ; ceinture et pains de plomb, semelles de plomb ; bouteille dans le dos et tuyau qui pend en boucle ; filin enroulé |

- **Gabarit** : cadres 42×44, pieds au même point que le Vagabond (pivot 40, décalage de pied 18 au branchement). Animations des personnages joués (`playable_animations`), allure propre : l'Éveillée flotte (peu de rebond, bras écartés), le Facteur se penche, la Scaphandrière pèse.
- **Yeux** : un œil rond d'un pixel se perd entre deux pixels du visage ; ceux des nouveaux héros sont des amandes verticales, qui se voient de face. Les trois joués ont encore l'œil rond (yeux peu lisibles de face) : à reprendre si Raphaël le souhaite.
- **Contour « double » de l'Éveillée** : essayé dans le modèle (même silhouette décalée, cyan pâle), abandonné : l'écho passe devant elle dès qu'elle tourne le dos. À faire en jeu au branchement : un double transparent qui la suit en retard, ce qui est aussi sa mécanique (rémanence).

## 7. Compte rendu H2 — Sonneur, Écolière, Photographe (10 octobre)

[Planche](planches/32-h2-heros.png), [animations](planches/32-h2-animations.png).

- **Sonneur** (cadre 42×46, un peu plus grand) : béret penché à queue, cheveux poivre et sel, long manteau de bedeau bleu-noir, col blanc ; corde de cloche en deux tours de bandoulière, bout coupé effiloché qui pend au genou ; cloche de bronze sanglée haut dans le dos, au-dessus de l'épaule gauche, penchée vers l'extérieur : de face, son profil (dôme, flanc évasé, lèvre) dépasse à côté de la tête ; de dos, sa bouche sombre et son battant.
- **Écolière** (cadre 36×40, taille d'enfant) : la même fillette que l'écho de l'écolière (blouse bleu marine, col blanc, cartable rouge, cheveux bruns) avec les proportions des joués ; cartable plus large que ses épaules à rabat sombre et fermoirs dorés ; nattes écartées nouées de rubans roses ; craie rose dans la main droite.
- **Photographe** (cadre 42×44) : imperméable bordeaux ceinturé à col relevé, carré court ; appareil à deux objectifs sur la poitrine ; flash sur potence dont le grand réflecteur rond et l'ampoule dépassent au-dessus de l'épaule droite (la cloche du Sonneur est de l'autre côté) ; sacoche à pellicules, boîtes jaunes.

## 8. Compte rendu H3 — Veilleuse, Enfant du Bas-Port (10 octobre)

[Planche](planches/32-h3-heros.png), [animations](planches/32-h3-animations.png), [casting complet](planches/32-casting.png) (les dix, cinq vues, échelle du jeu, aplats).

- **Veilleuse** (cadre 40×42) : voûtée, tête relevée ; robe noire de veuve qui tombe d'aplomb, tablier gris ; châle de laine vert bouteille (la couleur de sa porte, plus bleu que le vert du Traqueur) en capuche ronde et sur les épaules, franges ; mèches blanches ; lampe à pétrole qui pend d'aplomb sous la main gauche et suit le balancement du bras, flamme lumineuse ; canne dans la droite.
- **Enfant du Bas-Port** (cadre 34×38, plus petit que l'Écolière) : ciré jaune de pêcheur trop grand, suroît à bord rabattu sur la nuque, bottes de caoutchouc ; écharpe orange du Vagabond, trop grande, dont le bout traîne ; le bonnet rouge à pompon de sa sœur dans la main gauche.

## 9. Vérifications H1 à H3

- Aperçus ×8 en cinq vues de chaque héros, regardés à chaque passe ; planches d'animations (SE et NW, idle, marche, dash, coup, mort) regardées.
- Échelle du jeu à côté des trois joués et d'un Rôdeur ; aplats noirs : dix silhouettes distinctes (coiffes, cloche, réflecteur, casque, mèches, tailles d'enfant).
- Sprites écrits : 216 PNG par héros (8 directions × 27 images), 1 728 au total ; smoke test vert, 1 728 `.import` créés, aucun fichier supprimé.
- Régénération de l'Écolière et du Facteur comparée octet par octet : identique.
- Non vérifié en jeu : aucun de ces héros n'est jouable. Au branchement : `sprite_feet_offset` = pivot moins la demi-hauteur du cadre (18 pour les cadres de 44, 19 pour le Sonneur, 17 pour la Veilleuse, 16 pour l'Écolière, 15 pour l'Enfant), captures en run.


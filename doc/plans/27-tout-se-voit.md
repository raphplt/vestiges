# Plan 27 — Tout se voit : impacts, statuts, coups reçus

6 octobre 2026 · Demandé par Raphaël ([DECISIONS §71](DECISIONS.md)) · **Validé le 6 octobre ([DECISIONS §72](DECISIONS.md)) : ordre V0 → V4, questions tranchées au §4.** **V0 livré le 6 octobre** ([§6](#6-v0-livré--6-octobre-2026)) ; V1 en cours, découpé au [§7](#7-v1-découpage--7-octobre-2026) ; **V1a livré** ([§8](#8-v1a-livré--7-octobre-2026)), **V1b livré** ([§9](#9-v1b-livré--7-octobre-2026)), **V1c livré** ([§10](#10-v1c-livré--7-octobre-2026)) : V1 terminé ; V2 en cours, découpé au [§11](#11-v2-découpage--7-octobre-2026) ; **V2a livré** ([§12](#12-v2a-livré--7-octobre-2026)), **V2b livré** ([§13](#13-v2b-livré--7-octobre-2026)), **V2c livré** ([§14](#14-v2c-livré--7-octobre-2026)), **V2d livré** ([§15](#15-v2d-livré--7-octobre-2026)) : V2 terminé ; V3 en cours, découpé au [§16](#16-v3-découpage--7-octobre-2026) ; **V3a livré** ([§17](#17-v3a-livré--7-octobre-2026)).

> « fais en sorte que tout se voit. […] qu'on puisse voir concrètement en jeu tous les impacts bien comme il faut et les effets sur les ennemis (et sur le joueur quand on se fait attaquer) »

Le plan part d'un inventaire du code (trois relevés, 6 octobre, `main` 86626a57) : ce que le joueur inflige, ce qu'il subit, et les outils disponibles. Chaque constat porte son fichier ; la colonne « visible » dit ce qu'on voit sur une créature à sprite, pas ce que le code croit afficher.

---

## 1. Constat

### 1.1 Une panne qui cache déjà beaucoup

Le shader commun des créatures et du joueur (`assets/shaders/entity.gdshader:95`) écrit sa couleur finale **sans la multiplier par `COLOR`**, qui porte `Modulate` et `SelfModulate`. Depuis le commit 6a30b79c (« Améliorations textures »), tout ce qui passe par ces deux canaux est ignoré sur un sprite :

- le **flash blanc de chaque coup** (`HitFeedback.cs:114`, `SelfModulate` ×3) : le plan 02 J1 l'a validé par l'état du code, pas par l'image ;
- l'**estompage du Rampant terré** (`Enemy.cs:1683`), le **fondu d'une créature d'événement qui s'en va** (`Enemy.cs:414`), la **teinte d'annonce** des attaques (`EnemyAttackFx.cs:54`) ;
- côté joueur, l'**éclair de blessure** règle `flash_amount`, que le shader déclare mais ne lit jamais (`Player.cs:1715-1734`).

Ce qui marche dans ce shader : contour d'élite, teinte d'Aberration, dissolution à la mort. L'écrasement et le petit recul du coup (`HitFeedback`) se voient aussi.

### 1.2 Statuts posés sur les ennemis

Les teintes de statut colorent un polygone de secours (`Enemy._visual`), **masqué dès qu'une créature a un sprite** (`Enemy.cs:1527`) : les douze en ont un.

| Statut | Posé par | Ce qu'on voit aujourd'hui | Visible |
|---|---|---|---|
| Brûlure | Lampe (feu au sol), Allumette, transmission, Pince | 2 braises toutes les 0,3 s, à hauteur fixe ; coupées avec le réglage « Effets d'attaque » ; aucun chiffre sur les tics | faible |
| Saignement | Râteau, Scalpel Incision, Pince | rien | **non** |
| Ralentissement | Cloche, Glas, Congère, Chronomètre, Glaçon, Thermomètre, Chewing-gum, Propagation, Pince | la créature avance moins vite ; son animation garde sa cadence | **non** |
| Figé | Berceuse, Arrêt sur image, Glaçon au palier | la créature s'arrête et **continue son animation d'attente** | **non** |
| Désorientation | Polaroïd, Court-circuit, Tocsin, Fréquence pirate, Propagation, Pince | elle erre ; rien sur elle | **non** |
| Fragile | Loupe, Chewing-gum au palier, Pince | 3 étincelles à la pose, rien pendant la durée | **non** |
| Terré | Rampant | estompage prévu, perdu (§1.1) | probablement non |
| Élite, champion, Aberration, affixe, cible prioritaire | variantes, Convergence | contour, teinte, anneau au sol, plaque, coins | oui |

### 1.3 Impacts et effets d'armes

Un coup direct donne, en plus du flash perdu, un écrasement, une étoile générique, un chiffre et un son unique pour les 24 armes ; `PlayHit` (`PlayerAttackFx.cs:224`) y ajoute 7 étincelles et une étoile aux couleurs de l'arme (14 et un anneau sur un critique). Les trous :

| Effet | Constat |
|---|---|
| Forme des Craies | **aucun visuel** de la zone ; `shape_visual: crayon_drawn` n'est lu nulle part (`Player.cs:990-1008`) |
| Écho des Gants | anneau fixe de 18 px pour un rayon réel d'environ 40 px × Taille, sans zone ni gerbe (`PlayerAttackFx.cs:256`) |
| Coups secondaires (écho, forme) et dégâts d'objets (Rondelle, Mètre, Pétard, Stylo) | retour générique seulement, pas de `PlayHit` à la couleur de l'arme |
| Tics de Brûlure et de saignement | ni chiffre, ni éclat, ni son ; le feu de la Lampe a perdu ses chiffres avec le lot S1b |
| Champ du Chronomètre | un simple contour ; rien sur les créatures prises dedans |
| Trousseau | aucun trait du joueur vers la première cible |
| Projectile en fin de course | disparaît sans éclat (sauf avec le Mètre) ; `ProjectileImpact` ne sert qu'aux tirs ennemis |
| Icônes d'objets | absentes pour la Rondelle, le Mètre, l'Écusson, la Pince à linge, les paliers de l'Allumette et de l'Épingle ; la Brûlure transmise n'est reliée par aucun trait |
| Recul | aucun effet propre (poussière, traînée) |

### 1.4 Ce que subit le joueur

Toute blessure passe par un même paquet (`Player.LoseHp`, `Player.cs:1338-1363`) : éclair presque blanc (perdu, §1.1), secousse très faible (environ 1 px à l'écran), animation de blessure, sons, barres. Le gel d'image est désactivé volontairement (`ScreenShake.cs:57-60`). Les trous :

| Ce qui arrive | Constat |
|---|---|
| **Toile de la Tisseuse** (−60 % de vitesse, 2 s) | **invisible** : ni teinte, ni icône, ni son. Et elle s'applique **même quand le coup est annulé** par le bouclier, l'invulnérabilité ou le dash (`EnemyProjectile.cs:133-135`) : défaut de règle |
| Blessure | éclair proche du blanc des ennemis touchés ; ni vignette, ni direction du coup ; secousse imperceptible |
| Bouclier | la casse ressemble à un blocage (même éclair) ; la recharge et le retour au plein sont muets |
| Armure, coup ignoré | aucun retour pour l'armure ; le coup ignoré n'a qu'un éclair gris sans son |
| Soins | aucun son de soin, aucun chiffre, aucune surbrillance ; régénération invisible ; l'icône du vol de vie s'affiche même à vie pleine (0 PV rendu) |
| Néant (Effacement) | le paquet blessure complet repart toutes les 0,5 s : deux secousses par seconde, son de coup en boucle, dash bloqué 0,2 s à chaque tranche |
| Pénalité de vitesse et de dégâts de l'Effacement | seulement le voile, jamais le lien avec la vitesse |
| Explosion d'un Instable | rayon jamais montré avant la détonation, pas de son propre ; la cause de mort n'est pas transmise à l'écran de mort (comme les événements) |
| Annonces | mêlée sans annonce (attendu), tir visé, Rampant et tentacule muets ; couloirs de tir courts et fins |

---

## 2. Principes

1. **Un état de jeu se lit par une forme ou un mouvement, puis par une teinte.** Jamais par la seule couleur : le filtre daltonien existe, et vingt créatures teintées en foule font un arc-en-ciel illisible. Exemple : figé = animation arrêtée + givre ; désorienté = étoiles qui tournent.
2. **Le danger passe d'abord.** Rien de ce qui est ajouté ne doit couvrir une annonce ennemie ni un projectile ennemi ; les effets de statut restent sous les plaques (z 20) et sous les tirs.
3. **Ce qui renseigne n'est pas décoratif.** Un statut est une information de jeu (l'Épingle frappe les entravés) : sa marque minimale reste visible avec « Effets d'attaque » coupé et en particules réduites ; seules les fioritures (étincelles en plus) suivent le budget.
4. **Charte graphique.** Poses clés sans interpolation, tramage en damier pour les transitions, contours jamais noirs, yeux des créatures `#7FFF00` jamais recouverts, palette de `PixelPalette` (familles Fire, Blood, Glass, Pale, Void, Silk ; Hostile réservée à l'ennemi).
5. **Coût borné.** Le shader des créatures a déjà un matériau par créature (`Enemy.cs:1535`) : un paramètre de statut ne coûte qu'un appel au changement d'état, jamais par image. Effets suivis par pool (`PixelFx` en boucle avec `follow`), aucune allocation par coup, budget `FxBudget` respecté ; chaque lot passe au banc dense avec des armes à statut.
6. **Réglages en données.** Couleurs, durées, tailles et seuils dans un fichier de `data/` contrôlé au chargement (comme les contrats du plan 26).
7. **Vérifié à l'image.** Chaque lot visible est livré avec une planche de captures avant/après, regardée avant intégration ; un mode de capture dédié est construit en V0.

---

## 3. Lots proposés

Ordre recommandé : V0, puis V1 (le cœur de la demande), V2, V3, V4. V5 dépend du chantier audio.

### V0 — Réparer la base et outiller la vérification

- `entity.gdshader` multiplie sa couleur par `COLOR` : flash blanc des coups, estompage du Rampant, fondu de disparition et teinte d'annonce reviennent ; le shader lit `flash_amount`/`flash_color`, pour l'éclair du joueur.
- **Nouveau mode de capture `--capture-statuses`** : une rangée de créatures, chacune sous un statut posé directement (brûlure, saignement, ralenti, figé, désorienté, Fragile, terré, plus une créature saine), en gros plan à plusieurs instants ; et `--capture-player-hit` : le joueur touché, bouclier qui encaisse puis casse, toile, soin.
- **Vérification :** planche avant/après sur les deux modes ; contrôle du pool (le matériau rendu au pool revient à l'état neutre, `EnemyAbilityRegression`) ; banc dense A/B (le shader touche toutes les créatures).

### V1 — Les statuts se voient sur les créatures

Un composant dédié (`EnemyStatusVisual`, hors de `Enemy.cs`, qui ne doit plus grossir) lit les états de la créature **au changement d'état** et pilote trois canaux : un paramètre de statut du shader (teinte et trame), le rythme de l'animation, et un effet suivi par pool.

| Statut | Proposition (à valider sur planche) |
|---|---|
| Figé | animation arrêtée sur la pose en cours ; givre en trame claire (famille Glass) sur le haut du sprite ; deux éclats de glace à la pose |
| Ralenti | animation ralentie au même facteur que la marche ; teinte froide légère ; petites stries derrière la créature quand elle marche |
| Désorienté | 3 étoiles qui tournent au-dessus de la tête, à la hauteur réelle du sprite (taille des élites comprise) |
| Brûlure | braises à la hauteur réelle, plus nombreuses selon l'intensité ; bord chaud tramé en bas du sprite ; marque minimale hors budget |
| Saignement | gouttes rouges qui tombent (famille Blood), comme les braises |
| Fragile | fêlures blanches tramées sur le sprite pendant la durée, éclat de verre à la pose |
| Plusieurs statuts | priorité d'affichage fixe pour la teinte (figé > Fragile > brûlure > ralenti) ; les effets de forme (étoiles, gouttes, braises) se cumulent, plafonnés |

- **Vérification :** planche `--capture-statuses` sur trois sols (forêt, ville, carrière) et en foule (60 créatures sous Cloche + Berceuse) ; contrôles : chaque état allume puis éteint son canal, le pool rend une créature neutre, aucun paramètre réécrit à chaque image ; banc dense A/B avec Cloche, Lampe, Berceuse, Polaroïd.

### V2 — Chaque impact a sa forme

- **Forme des Craies dessinée** : étoile, cercle, maison, soleil (les formes déjà nommées dans les données), au trait crayonné, à la taille réelle de la zone.
- **Écho des Gants à sa vraie taille**, avec zone et gerbe ; **coups secondaires et dégâts d'objets** avec `PlayHit` à la couleur de l'arme (ou de l'objet).
- **Tics de dégâts sur la durée** : petit chiffre regroupé toutes les 0,5 s par créature, à la couleur du statut (§72) ; le feu de la Lampe retrouve un retour à chaque tic.
- **Champ du Chronomètre** rempli en trame pendant sa durée ; **trait du Trousseau** vers sa première cible ; **éclat de fin de course** des projectiles (pool `ProjectileImpact`).
- **Icônes d'objets manquantes** (Rondelle, Mètre, Écusson, Pince à linge, paliers de l'Allumette et de l'Épingle) et **trait de Brûlure transmise** d'une créature à l'autre.
- **Recul** : petite poussière au sol au départ du recul.
- **Vérification :** galerie `--capture-weapons` des 24 armes, avec les ascensions concernées et les objets ; banc dense A/B.

### V3 — Le joueur sent les coups

- **Blessure** : éclair rouge propre au joueur (distinct du blanc des ennemis), vignette rouge tramée aux bords, plus marquée du côté d'où vient le coup, **chiffre des dégâts reçus** au-dessus du joueur (§72) ; secousse lisible (réglée en données, toujours sous le réglage de secousse du joueur). Pas de gel d'image (§72).
- **Toile** : la créature-joueur porte des fils (trame Silk) et ralentit son animation ; icône de statut près de la jauge sous les pieds ; **correction de règle** : la toile ne s'applique plus si le coup est annulé.
- **Bouclier** : la casse fait voler des éclats de verre, distincts d'un blocage ; un tintement court et un éclat quand il est de nouveau plein.
- **Armure, coup ignoré** : petit éclat « paré » quand l'armure réduit un coup notable.
- **Soins** : étincelles vertes et surbrillance de la barre ; son de soin discret (à produire, plan 15) ; plus d'icône de vol de vie quand rien n'est rendu.
- **Néant** : un retour propre à l'Effacement (vignette blanche qui pulse avec les tranches, son de brûlure du Néant) au lieu du paquet blessure complet toutes les 0,5 s ; le dash n'est plus bloqué par ces tranches.
- **Pénalité de l'Effacement** : le joueur ralenti par l'Effacement le montre (traînée pâle, comme la toile mais en Pale).
- **Vérification :** `--capture-player-hit` avant/après ; contrôles de la toile annulée, du dash sous le Néant ; recette en jeu par Raphaël.

### V4 — Annonces ennemies complètes

- **Instable** : son rayon s'affiche en anneau qui se remplit quand la créature est blessée à mort ; son d'explosion propre ; cause transmise à l'écran de mort (et pour les événements).
- **Annonces muettes** : son court pour le tir visé, le surgissement du Rampant et le tentacule de l'Indicible (à produire, plan 15) ; couloir de tir un peu plus long et plus épais si la planche le justifie.
- **Vérification :** `--capture-abilities` avant/après ; contrôles de la cause de mort.

### V5 — Le son de l'impact (après la reprise audio, plan 15)

Un son d'impact par famille de matière (lame, choc, verre, feu, papier, électrique) au lieu d'un son unique pour 24 armes. Lié au mixage A3 du plan 15 : pas avant ton écoute.

---

## 4. Questions pour Raphaël

**Tranchées le 6 octobre ([DECISIONS §72](DECISIONS.md)) :** 1 A (marque minimale toujours visible), 2 B (chiffre regroupé toutes les 0,5 s), 3 **B** (chiffre des dégâts reçus, contre la recommandation), 4 A (gel d'image coupé), 5 A (V0 → V4).

| # | Question | Options | Recommandation |
|---|---|---|---|
| 1 | **Les statuts restent-ils visibles avec « Effets d'attaque » coupé ?** | A. Oui, la marque minimale (teinte, animation, étoiles) reste ; seules les fioritures disparaissent. B. Non, tout suit le réglage. | **A** : c'est une information de jeu, pas une décoration. |
| 2 | **Chiffres sur les tics de brûlure et de saignement** | A. Aucun. B. Un petit chiffre agrégé toutes les 0,5 s, à la couleur du statut. C. Un chiffre par tic. | **B** : on voit que ça brûle et combien, sans pluie de chiffres. |
| 3 | **Chiffre des dégâts reçus par le joueur** | A. Non : éclair rouge, vignette et barre suffisent. B. Oui, petit chiffre rouge au-dessus du joueur. | **A** pour commencer ; B se teste en un réglage si la blessure reste mal lue. |
| 4 | **Gel d'image (hitstop) sur un coup reçu ou un critique** | A. Laisser désactivé, comme aujourd'hui. B. Le réactiver, très court, sur coup reçu seulement. | **A** : il a été coupé volontairement ; la vignette et l'éclair portent le message. |
| 5 | **Ordre** | A. V0 → V1 → V2 → V3 → V4. B. Le joueur d'abord (V3 avant V1). | **A** : V0 rend d'un coup le flash de tous les impacts ; V1 est le cœur de ta demande. |

## 5. Ce qui n'est pas dans ce plan

- Les sons à produire (soin, annonces, impacts par matière) : listés ici, produits au plan 15.
- Le gel d'image et l'arme en main (abandonnée, §65) : pas réintroduits.
- Les décors et le terrain : hors sujet.

## 6. V0 livré — 6 octobre 2026

**Cause mesurée.** Une sonde de rendu (Godot 4.7.2, GL Compatibility) montre que, dans `fragment()`, `COLOR` vaut déjà texture × Modulate : un gris 0,5 rendu par `COLOR` seul donne 0,498, et texture × `COLOR` donne 0,247. L'ancien shader des entités multipliait donc la texture par elle-même (sprites assombris) ; le commit 6a30b79c a retiré la multiplication, et Modulate avec elle. `COLOR` lu dans `vertex()` porte Modulate seul (0,498 × modulate rouge = 0,498, 0, 0).

**Fait.**
- `entity.gdshader` : Modulate capté dans `vertex()` par un `varying`, appliqué à toutes les passes (contour, dissolution, rendu) ; `flash_amount`/`flash_color` relus. Reviennent : flash des coups, teinte d'annonce, estompage du Rampant terré, fondu de disparition, éclair du joueur, et le **clignotement d'invulnérabilité du joueur** (`Player.cs:1375`), perdu lui aussi et absent de l'inventaire.
- `player_projectile.gdshader`, **même défaut, hors inventaire** : le liseré et le halo du lot F5 (§53), posés sur des pixels transparents, étaient multipliés par un alpha nul et ne se sont jamais affichés ; les sprites de projectiles étaient assombris. Réparé à la demande de Raphaël ([DECISIONS §73](DECISIONS.md)).
- `--capture-statuses [--status-enemy id]` : dix créatures sur deux lignes (saine, coup répété, annonce, brûlure, saignement, ralenti, figé, désorienté, Fragile, Rampant terré), étiquetées, six instants de 1 à 90 images. `--capture-player-hit` : repos, blessure, bouclier qui encaisse puis casse, toile en marche, soin, et une vue plein écran pour les barres. Les deux cherchent d'abord un terrain sans décor haut dans le cadre (rectangle visible des décors, rangés par tronçons) : sinon la rangée passait derrière un immeuble.
- `tools/bench_ab.sh` : import de préchauffage du worktree de base. La première passe de base échouait à chaque fois (police du thème pas encore importée), ce qui laissait la base à 1/2 passe valide.

**Vérifié.**
- Planches avant/après sur le ViewSonic, regardées : avant, les dix créatures ne se distinguent que par leur pose et le joueur ne change pas quand il est touché ; après, flash du coup, annonce verte, Rampant estompé, éclair de blessure, éclair bleu pâle du bouclier, clignotement d'invulnérabilité. Projectiles (arc, aiguille) plus clairs et lisérés. Brûlure, saignement, ralenti, figé, désorienté, Fragile, toile et soin restent invisibles : c'est V1 et V3.
- `dotnet build` 0 avertissement ; smoke vert ; `test_enemy_abilities` (remise à neutre du pool : Modulate, SelfModulate, `flash_amount`), `test_movement`, `test_weapons`, `test_dev_mode` : 0 échec.
- Banc A/B contre `0bb4d122`, 2 passes valides de chaque côté, charge 2,1 à 2,3 sur 16 fils : 720p 188,6 → 189,1 FPS, 1080p 178,8 → 175,3 FPS, p99 10,6 et 10,9 ms identiques, 2 nœuds créés/s des deux côtés. Pas de coût mesurable.

**Points ouverts.**
- Le flash des coups (SelfModulate ×3) blanchit les tons clairs et donne un ton chair sur les bruns : lisible, mais pas une silhouette blanche. À juger en jeu ; V1 peut passer par `flash_amount` si Raphaël veut un blanc franc.
- Le Rampant terré à 35 % d'opacité se perd presque sur l'asphalte ; à reprendre avec les marques de V1.
- L'éclair de blessure du joueur est presque blanc, comme celui des ennemis : V3 le passe au rouge.

## 7. V1, découpage — 7 octobre 2026

V1 se livre en trois sous-lots, chacun avec sa planche `--capture-statuses` avant/après.

- **V1a — le sprite lui-même.** Un composant `EnemyStatusVisual` (classe simple possédée par `Enemy`, comme `HitFeedback`) compare à chaque tick un masque d'états (quelques comparaisons, aucune écriture) et n'écrit le matériau qu'au changement. Canaux : teinte de priorité (figé > Fragile > brûlure > ralenti) ; givre tramé sur les bords hauts (figé) ; fêlures claires (Fragile) ; bord chaud tramé sur les bords bas (brûlure) ; yeux `#7FFF00` jamais recouverts. Animation : arrêtée sur la pose en cours si figée (mini-boss et boss exceptés, ils agissent encore), ralentie au facteur de marche si ralentie. Réglages dans `data/fx/status_visuals.json`, contrôlés au chargement.
- **V1b — les marques autour.** Étoiles qui tournent au-dessus de la tête (désorienté), gouttes (saignement), braises (brûlure) à la hauteur réelle du sprite, élites comprises. Dessinées par un nœud enfant de la créature, poses clés à cadence fixe, redessin au changement de pose seulement ; marque minimale hors réglage « Effets d'attaque », fioritures au budget.
- **V1c — vérification d'ensemble.** Planche sur trois sols (forêt, ville, carrière) et en foule (60 créatures sous Cloche et Berceuse) ; contrôles : chaque état allume puis éteint son canal, une créature rendue au pool revient neutre ; banc A/B avec Cloche, Lampe, Berceuse, Polaroïd.

## 8. V1a livré — 7 octobre 2026

**Fait.**
- `EnemyStatusVisual` (classe possédée par `Enemy`, +12 lignes dans `Enemy.cs`) : masque d'états comparé à chaque tick, matériau écrit au changement seulement, et seulement pour les états qui le touchent (figé, ralenti, brûlure, Fragile). Teinte de priorité figé > Fragile > brûlure > ralenti, couleur moyenne de la rampe (la claire tire au blanc et se confondait avec le flash d'un coup, constaté sur la première planche).
- `entity.gdshader` : givre clair sur les bords hauts (1er pixel plein, suivants en damier), fêlures diagonales claires, bord chaud tramé sur les bords bas ; yeux `#7FFF00` épargnés ; tout le bloc est gardé par des uniformes, une créature sans statut ne le paie pas.
- Animation : figée sur la pose en cours (mini-boss et boss exceptés), ralentie au facteur de marche (plancher 0,25). Une créature tuée figée meurt à cadence normale ; givre et chaleur se dissolvent avec elle.
- `data/fx/status_visuals.json`, lu par `StatusVisualConfig` (familles, forces, profondeurs, espacement), contrôlé en entier ; refusé, il est signalé une fois et les créatures restent sans marque.

**Écarts.** Les stries derrière une créature ralentie sont reportées à V1b (marques autour du sprite). La relecture (`godot-reviewer`) a trouvé deux défauts corrigés avant livraison : animation de mort arrêtée sur une créature tuée figée, cadence non remise à 1 au retour au pool.

**Vérifié.**
- Planche `--capture-statuses` sur le ViewSonic, regardée : figé (pose tenue sur les six instants, bleu verre, givre), ralenti (teinte froide, pas plus lents), brûlure (orange, bords bas chauds), Fragile (fêlures). Saignement et désorienté restent sans marque : V1b.
- `EnemyAbilityRegression` : 13 contrôles ajoutés (réglages lus, famille inconnue et fêlures trop serrées refusées ; chaque canal allumé puis éteint à son terme ; priorité ; pose tenue ; matériau non réécrit sans changement ; mort à cadence normale ; pool neutre et cadence remise dès le retour). Contre-épreuve : sans la correction de la mort, le contrôle échoue (cadence 0, image figée). 0 échec ; `test_movement`, `test_weapons`, smoke verts.
- **Coût.** Première version : 1080p −4,9 à −7 % de FPS sur deux bancs, écart qui croissait avec la résolution. Cause : boucles de bord bornées à 16, déroulées par le compilateur. Borne ramenée à 4 (`MaxEdgeDepthPx`) : banc A/B contre `9b8c6b7e`, 3 passes valides de chaque côté, charge 2,5 au départ et 4,3 à la fin : 720p 195,9 → 201,5 FPS, 1080p 183,2 → 184,6, p99 10,2/10,4 et 10,7/10,7 ms, 2 nœuds créés/s. Pas de coût mesurable. Le banc n'a pas de créature sous statut : le coût d'une foule marquée se mesure en V1c.

**Points ouverts.** Ralenti et figé partagent la famille Glass : ils se distinguent par la force de la teinte, le givre et l'animation ; à juger en jeu. Les cavités internes (entre les jambes) prennent aussi givre et chaleur.

## 9. V1b livré — 7 octobre 2026

**Fait.**
- `EnemyStatusMarks`, nœud enfant de chaque créature à sprite : étoiles en croix qui tournent sur une ellipse au-dessus de la tête (désorienté, famille Crit), larmes de sang qui tombent du milieu du corps aux pieds puis éclaboussent (saignement, Blood), braises 2×2 qui montent au-dessus de la tête (brûlure, Fire ; 2 à 4 selon la part des PV max brûlée par seconde). Des stries derrière une créature ralentie qui marche ont été livrées ici puis retirées en V1c (coût). Poses clés à 8 par seconde, redessin au changement de pose ou d'état seulement, mise à jour dans la portée de traitement complet (600 px) seulement ; figée, la créature garde aussi ses marques sur leur pose.
- Hauteur réelle : corps mesuré une fois par espèce sur la pose de repos, mesure désormais partagée avec l'ombre (`SpriteFeetY`, même valeur qu'avant) ; contre-mis à l'échelle, le nœud reste au pixel sur une élite.
- Information de jeu : jamais coupées par « Effets d'attaque » ni par le budget. Les étincelles de brûlure existantes restent la fioriture, soumise aux deux, et partent maintenant du milieu du corps au lieu de 12 px fixes.
- Réglages dans la section `marks` de `data/fx/status_visuals.json`, contrôlés (cadence de 2 à 24, comptes de 1 à 6, minimum ≤ maximum).

**Écarts.** Première planche : braises et gouttes d'un pixel, perdues sur le sol ; agrandies (braise 2×2, larme 2×3). Pas d'éclats de glace à la pose du figé : le givre de V1a suffit à la planche, à rediscuter si le figé se lit mal en jeu. Relecture `godot-reviewer` : créature sans sprite réutilisant un nœud de pool (marques de l'ancienne espèce) corrigé, marques figées avec la créature, double décodage de l'image supprimé.

**Vérifié.** Planche `--capture-statuses` sur le ViewSonic, agrandie et regardée sur trois instants : étoiles, larmes, braises et stries visibles et distinctes. 6 contrôles ajoutés (marques avec effets d'attaque coupés, éteintes à leur terme, stries seulement en marche, éteintes à la mort et au pool) ; `test_enemy_abilities`, `test_movement`, `test_weapons` et smoke verts. Coût : le banc dense n'a pas de créature sous statut, mesure en V1c.

## 10. V1c livré — 7 octobre 2026

**Fait.**
- `--status-biome id` : les planches de statuts se tiennent sur un terrain dégagé du biome demandé (recherche jusqu'à 12 000 px, biome testé avant les décors).
- `--capture-status-crowd` : soixante Rôdeurs autour du joueur armé de la Cloche et de la Berceuse.
- Banc dense : `--ascend arme:ascension` (Berceuse), pour mesurer une foule réellement sous statut.
- **Stries du ralenti retirées.** Avec la Cloche, presque toute la foule est ralentie en permanence : chaque créature marquée coûtait un appel de dessin (+87 appels, +706 objets rendus, GPU +0,11 ms à 720p ; −7,6 % de FPS à 720p et −5,3 % à 1080p, contre `9b8c6b7e`, 3 passes). Le ralenti se lit déjà par sa teinte et ses pas ralentis (V1a). Les marques restent pour désorienté, saignement et brûlure, qui n'ont pas d'autre signe.

**Vérifié.**
- Planches regardées sur le ViewSonic : forêt, ville, carrière (mêmes lectures ; figé ressort mieux sur les sols sombres) ; foule à 0,5, 1, 2, 3 et 5 s : créatures saines, ralenties (teinte froide) et figées (plus pâles, givrées) se distinguent.
- Banc A/B avec Cloche, Lampe, Berceuse et Polaroïd contre `9b8c6b7e` (avant V1a), 3 passes valides de chaque côté, charge 2,3 au départ et 2,75 à la fin : 720p 193,3 → 202,2 FPS, p99 9,7 → 9,1 ms ; 1080p 192,5 → 188,1 FPS, p99 9,0 → 10,6 ms ; appels de dessin 348 → 351 et 422 → 424, GPU 0,42 → 0,43 et 0,70 → 0,71 ms, nœuds créés/s 3 à 4 des deux côtés. Les FPS restent dans le bruit ; le p99 à 1080p n'est corroboré ni par le GPU ni par les appels de dessin.
- `test_enemy_abilities` (contrôle « ralenti sans marque » remplaçant celui des stries), `test_movement`, `test_weapons`, smoke : verts.

**Points ouverts.**
- **Flash collectif :** une impulsion de la Cloche touche toute la foule ; pendant 0,2 s, soixante créatures sont des silhouettes blanches (flash ×3 rendu en V0, plus la pâleur du figé). C'est un éclair, pas un état, mais il couvre la foule entière. À juger en jeu ; si c'est trop, plafonner le flash sur une créature figée ou baisser son intensité quand beaucoup de coups tombent dans la même image.
- Rampant terré presque invisible sur l'asphalte et la forêt (déjà noté en V0).
- `tools/bench_ab.sh` : malgré le préchauffage, un import de la base peut encore échouer sur la police du thème (une passe sur neuf) ; la passe est écartée, pas faussée.

## 11. V2, découpage — 7 octobre 2026

V2 se livre en quatre sous-lots, chacun avec sa planche (galerie `--capture-weapons` sur les armes concernées) et, quand il touche le combat dense, son banc.

- **V2a — dégâts sur la durée.** Par créature, brûlure et saignement s'additionnent et sortent en un petit chiffre toutes les 0,5 s (§72), à la couleur du statut, sans se fondre dans les chiffres des coups ; le dernier reliquat sort à la mort. Le feu de la Lampe, qui pose une brûlure, retrouve ainsi un retour à chaque tic. Réglages dans `data/fx/status_visuals.json`.
- **V2b — formes d'armes.** Formes des Craies dessinées à la taille réelle de la zone (étoile, cercle, maison, soleil) ; écho des Gants à sa vraie taille ; coups secondaires (écho, forme) et dégâts d'objets avec le retour aux couleurs de l'arme ou de l'objet.
- **V2c — trajectoires.** Champ du Chronomètre rempli en trame ; trait du Trousseau vers sa première cible ; éclat de fin de course des projectiles du joueur, par pool.
- **V2d — objets et recul.** Icônes manquantes (Rondelle, Mètre, Écusson, Pince à linge, paliers de l'Allumette et de l'Épingle), trait de la brûlure transmise, poussière au départ d'un recul.

## 12. V2a livré — 7 octobre 2026

**Fait.** `DamageOverTimeNumbers`, classe possédée par chaque créature : brûlure et saignement s'additionnent et sortent en un chiffre par statut toutes les 0,5 s (§72), plus petit (12 contre 14), à la couleur moyenne de la famille (brûlure orange flamme, saignement rouge sang), tenu 0,08 s puis monté de 16 px ; jamais fondu avec les chiffres des coups. La part fractionnaire attend le chiffre suivant (tolérance d'arrondi : trente tics de 1/6 font 5) ; le reliquat sort à la mort, tic fatal compris. Chiffres au budget des chiffres de dégâts, comme les autres. Le feu de la Lampe, qui pose une brûlure depuis S1b, a ainsi un chiffre à chaque intervalle. Intervalle et couleurs dans la section `numbers` de `data/fx/status_visuals.json`.

**Vérifié.**
- Galerie Lampe + Râteau (`--capture-weapons --weapons memory_lantern+nail_mace`) sur le ViewSonic, regardée : petits chiffres orange et rouges au-dessus des créatures, distincts des chiffres de coups (30, 12) ; couleurs mesurées au pixel (E07B39 et C4432B).
- 6 contrôles ajoutés (`EnemyAbilityRegression.DotNumbers`) : rien avant l'intervalle, 6 après 0,5 s à 12/s, chiffre du coup à part, demi-unité reportée, 1 après 1 s à 1/s, reliquat à la mort. 0 échec ; `test_weapons` et smoke verts.
- Banc A/B avec Cloche, Lampe, Berceuse, Polaroïd et Râteau contre `5057912d`, charge 2,3 : passes valides 720p base 174 et 191 FPS, courant 201 à 212 ; 1080p base 172 et 191, courant 154 à 188 ; appels de dessin +13 et +11 (les libellés), GPU 0,44 → 0,43 et 0,71 → 0,72 ms ; le budget écarte environ 990 chiffres des deux côtés. Pas de coût mesurable. Une passe de base s'est déclarée invalide (399 FPS, autre défaut intermittent de l'outil que l'import de police).

**Point ouvert.** Orange et rouge sont proches ; les braises et les larmes autour du sprite disent lequel. À juger en jeu.

## 13. V2b livré — 7 octobre 2026

**Fait.**
- **Formes des Craies** (`ChalkDrawing`, recyclé par `CombatPools`) : étoile, cercle, maison ou soleil tiré au hasard, tracé au pixel avec des trous de craie (15 %), en trois poses sur 0,12 s, tenu 0,4 s, effacé en deux poses de tramage. Taille réelle : la zone des Craies se contrôle en cercle à l'écran (`DistanceTo`), la forme est donc dessinée sans écrasement au sol. Familles par forme (étoile Brass, cercle Glass, maison Rust, soleil Fire) et rythme dans `data/fx/chalk_shapes.json`, lu par `ChalkShapeConfig` (forme sans tracé ou famille inconnue refusées). La liste `shapes` de la fiche d'arme reste une annotation : les noms sont les mêmes.
- **Écho des Gants** : l'anneau fixe de 18 px devient zone tramée, anneau et gerbe au rayon réel de l'écho (cercle à l'écran, comme son contrôle), à la couleur de l'arme.
- **Coups secondaires** (écho, forme) et **dégâts d'objets** (Mètre, Pétard, Rondelle, écho critique du Stylo) : chaque cible reçoit la gerbe et l'éclat à la couleur de l'arme ou de l'objet, partie du centre de la zone. Mêmes plafonds que les coups directs (rafales par image, budget, réglage « Effets d'attaque »).

**Vérifié.**
- Galeries sur le ViewSonic, regardées : Craies + Fronde (étoile dorée et cercle tracés à la craie autour des cibles touchées), Craies + Gants (zones d'écho bleu-vert au rayon réel, formes à la craie, gerbes sur les cibles secondaires).
- `test_enemy_abilities`, `test_weapons`, `test_movement`, smoke : verts.
- Banc A/B avec Craies, Gants et Cloche contre `c50d0931`, 3 passes valides, charge 2,2 puis 1,6 : 720p 213,0 → 211,1 FPS, p99 9,0 → 9,2 ms ; 1080p 199,2 → 200,0 FPS, p99 9,1 → 9,1 ms ; GPU identique (0,42 et 0,70 ms), appels de dessin +10 et +6, nœuds créés/s 2,9 → 2,2 et 1,9 → 1,5. Pas de coût mesurable. Le banc n'équipe pas d'objet : les gerbes d'objets n'y sont pas mesurées, elles passent par les mêmes plafonds.

**Relecture `godot-reviewer`, corrigé avant livraison.** Pixels tracés plafonnés à 600 par forme (un sur deux ou trois au-delà, zone très agrandie) ; segments du cercle selon le rayon (un tous les 4 px) au lieu de 32 fixes ; dernier pixel des tracés ouverts ; plafond de gerbes à part pour les coups secondaires et objets (10 par image), pour qu'une explosion en foule ne prive pas le coup direct de la sienne, et rien compté quand les effets d'attaque sont coupés ; réglages des Craies chargés avant de consommer le budget.

**Point ouvert.** Les zones de l'écho et des Craies sont des cercles à l'écran alors que les objets (Mètre, Pétard) mesurent sur le sol écrasé : deux géométries de zone coexistent dans le code, chacune dessinée comme elle touche. À unifier un jour, hors de ce plan.

## 14. V2c livré — 7 octobre 2026

**Fait.**
- **Champ du Chronomètre** rempli en trame (densité 0,3) au lieu d'un simple contour, et rond à l'écran : il se contrôle en cercle (`DistanceTo`), il était dessiné écrasé au sol.
- **Trousseau** : le premier maillon part du joueur vers la première cible ; les rebonds suivants étaient déjà tracés.
- **Fin de course** : un projectile du joueur arrêté par sa portée joue une petite étoile et quatre étincelles à sa couleur, qui poursuivent sa route (`PlayerAttackFx.PlayFizzle`), sauf quand le Mètre pliant le fait déjà éclater. Effet d'attaque : suit le réglage et le budget. Écart au plan : pas le pool `ProjectileImpact`, dont les poses sont celles des tirs ennemis ; les pools de formes et d'étincelles suffisent.
- Galerie : `--weapon-frames a,b,…` choisit les images capturées après l'attaque.

**Vérifié.**
- Galeries sur le ViewSonic, regardées : Chronomètre + Trousseau (champ tramé autour des cibles, maillon du joueur à la première cible). La fin de course ne s'est pas laissé capturer (les trois pierres de la Fronde touchent leur cible, `--lethal` ajoute une élite qui explose) : 2 contrôles la vérifient (`EnemyAbilityRegression.ImpactFx` : un effet joué, aucun avec les effets d'attaque coupés).
- `test_enemy_abilities`, `test_weapons`, `test_movement`, `ObjectsRegression` (appel du Mètre mis à jour), smoke : verts.
- Banc A/B avec Chronomètre, Trousseau et Fronde contre `94e0d57b`, 3 passes valides, charge 1,9 puis 3,2 : 720p 196,0 → 210,5 FPS, 1080p 156,9 → 191,6 (base bruitée : 120,6 à 188,5) ; GPU 0,40 → 0,39 et 0,70 → 0,68 ms ; appels de dessin 291 → 291 et 354 → 356 ; nœuds créés/s 5 à 6. Pas de coût mesurable.

**Point ouvert.** Le champ ne dure que 0,5 s (`slow_duration`) et sa trame reste pâle sur la ville ; à juger en jeu, la densité est un réglage.

## 15. V2d livré — 7 octobre 2026

**Fait.**
- **Icônes des paliers** : l'icône de l'objet s'élève quand son palier 15 agit : Rondelle (zone qui refrappe), Mètre (éclat en bout de course), Écusson (onde du bouclier cassé), Pince à linge (statut renouvelé), Épingle (ralentis prolongés à une élimination), Allumette (brûlure transmise). Chaque stat visée n'appartient qu'à un objet (vérifié dans le catalogue).
- **Brûlure transmise** : un trait de feu va de la victime à la voisine qui reçoit la brûlure.
- **Recul** : un peu de poussière (famille Stone) part du sol à l'opposé du coup, au budget des étincelles et au réglage « Effets d'attaque », plafonnée à 6 créatures par image.

**Vérifié.**
- Galerie Lampe + Cloche, six objets au palier 15, `--lethal` sur le ViewSonic : icônes au-dessus du joueur, anneaux de la Rondelle, zone de feu. Le trait de brûlure transmise ne se distingue pas sur la planche (foule en dissolution au ralenti).
- `ObjectsRegression` : contrôle ajouté (icônes du Mètre puis de l'Écusson à leur action), 0 échec ; `test_enemy_abilities`, `test_weapons`, `test_movement`, smoke : verts.
- Banc A/B avec Cloche, Marteau et Lampe contre `910efe6a`, 3 passes, charge 2,3 puis 3,85 : 720p 213,9 → 214,7 FPS ; 1080p base 196,6 et 192,8 (une passe invalide), courant 198,7 ; GPU 0,41 → 0,42 et 0,72 → 0,69 ms ; appels de dessin inchangés. Mais la poussière, sans plafond, faisait écarter 664 étincelles par passe (0 avant) : plafonnée à 6 par image, une passe de contrôle redonne 0 étincelle écartée.

**Point ouvert.** Le trait de brûlure transmise n'a été vu que dans le code ; à regarder en jeu.

## 16. V3, découpage — 7 octobre 2026

- **V3a — la blessure.** Éclair rouge propre au joueur ; vignette rouge tramée aux bords, plus marquée du côté d'où vient le coup ; chiffre des dégâts reçus au-dessus du joueur (§72) ; secousse lisible réglée en données, toujours sous le réglage du joueur ; pas de gel d'image (§72).
- **V3b — ce qui ralentit le joueur.** Toile : fils tramés (Silk) sur le sprite, animation ralentie, icône près de la jauge ; **correction de règle** : la toile ne s'applique plus si le coup est annulé (bouclier, invulnérabilité, dash). Pénalité de l'Effacement : traînée pâle quand elle ralentit le joueur.
- **V3c — défense et soins.** Bouclier qui casse (éclats de verre, distincts d'un blocage), recharge pleine (éclat bref ; tintement à produire au plan 15) ; armure qui réduit un coup notable (éclat « paré ») ; coup ignoré ; soins (étincelles vertes, surbrillance de la barre ; son à produire) ; plus d'icône de vol de vie sans PV rendu.
- **V3d — le Néant.** Retour propre à l'Effacement (vignette blanche qui pulse avec les tranches) au lieu du paquet blessure toutes les 0,5 s ; le dash n'est plus bloqué par ces tranches.

Chaque sous-lot : planche `--capture-player-hit` avant/après, contrôles ; recette en jeu par Raphaël à la fin de V3.

## 17. V3a livré — 7 octobre 2026

**Fait.**
- **Éclair rouge** propre au joueur (famille Blood, 0,22 s), distinct du blanc des créatures.
- **Chiffre des dégâts reçus** (§72) : « −12 » en rouge au-dessus du joueur, plus gros qu'un chiffre de coup ; jamais écarté par le budget.
- **Vignette** (`HurtVignette`, calque plein écran sous le HUD, shader `hurt_vignette`) : liseré rouge tramé sur trois niveaux aux bords de l'écran, plus épais du côté du coup, effacé en trois paliers en 0,45 s ; seulement pour un coup de combat qui retire des PV (le Néant aura le sien en V3d). Matériau écrit à chaque palier seulement ; shader préchauffé au chargement.
- **Provenance du coup** : `Player.TakeDamage(damage, from)` et `PlayerDamageResult.FromDirection`, renseignés par les quatre sources (corps à corps et capacités via `Enemy.HitPlayer`, projectiles ennemis, explosion d'un Instable, tentacule de l'Indicible) ; les dégâts d'événement restent sans direction.
- **Secousse** : trauma 0,45 plus 1,5 fois la part des PV max perdus, toujours multiplié par le réglage du joueur. Avant : 0,3, soit environ 0,5 px de décalage de caméra ; un coup de 12 sur 70 PV donne maintenant environ 3 px.
- Réglages dans `data/fx/player_feedback.json` (`PlayerFeedbackConfig`), contrôlés ; refusés, le jeu garde l'éclair clair et la secousse d'avant.

**Écart.** La première vignette (épaisseur 0,16, opacité 0,55) couvrait près d'un tiers de l'écran du côté du coup : ramenée à 0,06 et 0,45 sur la planche.

**Vérifié.**
- `--capture-player-hit` sur le ViewSonic, avec une vue plein écran du coup (venu de la droite), regardée : liseré plus épais à droite, silhouette rouge, « −12 », puis clignotement d'invulnérabilité.
- 2 contrôles (`EnemyAbilityRegression.ImpactFx` : direction du coup, chiffre « −5 ») ; `test_movement`, `test_weapons`, `test_enemy_abilities`, `test_objects`, `test_dev_mode`, smoke : verts.
- Pas de banc : le joueur du banc dense est invincible, aucun de ces effets n'y joue ; ils ne coûtent qu'au moment d'un coup reçu.

**À juger en jeu.** Force de la secousse et du liseré ; Raphaël fera la recette de V3 en entier.

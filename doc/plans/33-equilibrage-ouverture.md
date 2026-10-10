# Plan 33 — Équilibrage de l'ouverture, personnage de mêlée

10 octobre 2026, soir. Retour de Raphaël après des parties au Vagabond (Faucille), consigné au [§101](DECISIONS.md). Diagnostic, leviers tranchés le soir même (§6), **lot Q1 livré et mesuré** (§7). À juger en jouant.

## 0. En bref

- **Le Cracheur pèse trop pour ce qu'il rapporte.** Il fait 15 % des apparitions, dès la première seconde et dans les cinq biomes, mais le Vagabond n'en tue que 4 %. Il cause 14 % des dégâts reçus, le Présage 9 % de plus. Il est entré dans les cinq pools d'exploration le 2 octobre (plan 21 E4), quand le retour du jour demandait « plus de tirs ».
- **Une partie des créatures est trop solide pour la mêlée du début.** Le Rôdeur cause 30 % des dégâts reçus et le Vagabond n'en abat que 8 %. À la minute 0, il a 90 PV (9 coups de Faucille) et frappe à environ 26 dégâts par seconde au contact. Le Tréant en a 180 et la Brute 120. Sur 10 minutes, 80 % de ce qui apparaît ne meurt pas : les éliminations viennent presque toutes des Charognards et des Ombres.
- **La difficulté a été réglée sur un build à distance.** Toutes les mesures des plans 21 (E4, H1–H3) et 30 suivent le protocole « build de Raphaël » : projectiles, perforation, cadence, avec tout débloqué. La mêlée en profil neuf, qui est l'entrée du jeu depuis la réserve de départ (plan 06 Q1), n'a jamais servi de référence.
- **Les niveaux arrivent plus lentement que la cible validée.** Au Vagabond, on atteint le niveau 15 à 22 à 10 minutes, contre 24 à 37 visés au §51. Le niveau 2 arrive vers 80 s, le 5 vers 2:45. Les deux courbes sont liées : peu d'éliminations, donc peu d'XP.
- **Les dix premières minutes enchaînent beaucoup de choses.** Micro-événement à 1:15, élite toutes les 35 à 55 s dès 1:30, Souverain à 2:45, Résurgence à 4:00, Souverain à 7:00, Résurgence à 8:30, Barrière à 10:00. Chaque ajout des 9 et 10 octobre (Résurgence qui déborde, Souverains ×4 PV, foule qui fait front) a durci un début déjà réglé sur le build fort.
- **Le soin n'est pas le premier problème.** Une régénération de 1 PV/s au Vagabond est généreuse pour le genre. Le soin est l'idée de Raphaël et il ne compte qu'après les autres causes (§3.5).

## 1. Méthode

`tools/measure_run.sh`, profil neuf (`MEASURE_PROFILE=normal` : Vagabond et réserve de départ), 10 min, graines 42, 1002, 7 et 123, Péril 0. Bot `--nomad --visit --mortal` : il avance, visite les lieux, prend de vrais coups (un coup fatal remet ses PV au maximum et se compte). Même protocole avec `--character traqueur` pour comparer.

**Limites.** Le bot n'esquive pas, ne recule pas et ne poursuit pas les tireurs. Les PV perdus et le nombre de « morts » mesurent donc la pression subie, pas la survie d'un joueur. Seules les **proportions** et les **comparaisons** sont fiables. Pour les dégâts, on retient la colonne « avec invulnérabilité » (`hit_gated`) : elle ne compte un coup que s'il passe les 0,25 s d'invulnérabilité. Les cartes de niveau sont tirées au hasard, comme dans une run de découverte.

```sh
MEASURE_PROFILE=normal MEASURE_JOBS=2 MEASURE_EXTRA_ARGS="--nomad --visit --mortal --character vagabond" tools/measure_run.sh <dossier neuf> 600 "42 1002 7 123"
```

## 2. Ce que montrent les mesures

### Qui fait mal, qui meurt (4 graines cumulées, 10 min)

| Créature | Apparues (Vagabond) | Tuées / apparues, Vagabond | Tuées / apparues, Traqueur | Part des dégâts reçus, Vagabond | Traqueur |
|---|---:|---:|---:|---:|---:|
| Charognard | 16 % | 58 % | 67 % | 16 % | 16 % |
| **Cracheur Pâli** (tir) | **15 %** | **4 %** | 11 % | **14 %** | **19 %** |
| **Rôdeur** | 12 % | **8 %** | 12 % | **30 %** | 26 % |
| Ombre | 12 % | 41 % | 48 % | 4 % | 4 % |
| Ombre rampante | 10 % | 24 % | 26 % | 9 % | 7 % |
| **Présage** (frappe au sol) | 9 % | **1 %** | 6 % | 9 % | 10 % |
| Rampant | 7 % | 12 % | 25 % | 5 % | 4 % |
| Brute du Vide | 7 % | 1 % | 4 % | 6 % | 6 % |
| Tréant corrompu | 3 % | 3 % | 2 % | 6 % | 6 % |

Le bot ne poursuit pas les tireurs, ce qui gonfle leur survie. Le constat tient quand même : au contact, la Faucille ne vient à bout que des petites créatures, et le reste s'accumule.

### Rythme et niveaux

| Graine | Éliminations, Vagabond / Traqueur | Niveau à 10 min, Vagabond / Traqueur | Niveau 2 | Niveau 5 | Niveau 10 |
|---|---:|---:|---:|---:|---:|
| 42 | 635 / 768 | 18 / 21 | 82 s | 2:47 | 5:16 |
| 1002 | 393 / 683 | 15 / 24 | 84 s | 2:37 | 4:51 |
| 7 | 629 / 1 088 | 22 / 24 | 87 s | 2:44 | 4:10 |
| 123 | 441 / 519 | 15 / 17 | 73 s | 2:48 | 4:25 |

Il apparaît 2 300 à 2 900 créatures en 10 minutes, et le Vagabond en tue 400 à 640. Le Traqueur fait 30 à 70 % de mieux, mais reste lui aussi sous la cible de niveaux du §51. Elle avait été mesurée avant les durcissements H3 et T1–T2.

### Les PV des créatures, minute par minute

Formule actuelle : PV × 1,5 d'emblée, ×1,07 par minute jusqu'à 4 min, **×1,33 par minute de 4 à 8 min**, ×1,17 ensuite (`spawn_flow.json`, plan 21 H3). Les dégâts sont ×1,3 d'emblée.

| Minute | Multiplicateur | Ombre | Cracheur | Rôdeur | Brute | Tréant | Coups de Faucille de base (10) pour un Rôdeur |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | ×1,5 | 15 | 22 | 90 | 120 | 180 | 9 |
| 4 | ×2,0 | 20 | 29 | 118 | 157 | 236 | 12 |
| 6 | ×3,5 | 35 | 52 | 209 | 278 | 417 | 21 |
| 8 | ×6,2 | 62 | 92 | 369 | 492 | 738 | 37 |
| 10 | ×8,4 | 84 | 126 | 505 | 674 | 1 011 | 51 |

La pente de 4 à 8 min suppose que la puissance du joueur décolle à ce moment-là. C'est vrai pour le build à distance de référence, beaucoup moins pour une mêlée avec une ou deux armes.

## 3. Causes, une par une

### 3.1 Les tireurs

- **Le Cracheur** (`fading_spitter`) apparaît dès la minute 0 et pèse 1 sur 4 à 6 dans chaque pool d'exploration. Une fois sur trois, il arrive en grappe de 2 à 4. Il s'arrête à 275 px et tire toutes les 1,4 s environ (1,1 s de recharge en dur dans `Enemy.cs`, plus 0,3 s d'annonce). Son projectile vole 4 s, environ 740 px. Contre une mêlée qui frappe à 80–110 px, il faut aller le chercher à travers la foule ou esquiver sans fin.
- **Historique** : au 1er octobre, Raphaël trouvait déjà l'esquive permanente gênante (§44, audit [pression des projectiles](../audits/projectile-pressure-2026-10-01/)). Le lendemain, il trouvait les ennemis trop faibles et demandait des tirs (§50) : E4 a alors mis le Cracheur dans les cinq biomes. Les deux demandes se contredisent ; le retour du jour tranche pour la première.
- **Le Présage** (frappe au sol annoncée, 1 s, deux zones au plus) fait 9 à 10 % des dégâts. C'est une zone à éviter par le placement, pas un projectile qui traverse l'écran : il correspond mieux au genre.
- En Résurgence s'ajoutent le Hurleur, la Sentinelle et la Tisseuse, qui pèsent peu ici (moins de 1 % chacun sur 10 min).

### 3.2 Les créatures solides

Rôdeur (60 PV de base, 15 dégâts), Tréant (120, 18) et Brute (80, 12) figurent dans les pools d'exploration **dès la minute 0**, sans `spawn_from_minute`. Avec ×1,5 de PV et ×1,3 de dégâts, un Rôdeur au contact inflige environ 26 dégâts par seconde à un Vagabond de 100 PV, qui met 9 s à le tuer.

### 3.3 La mêlée elle-même

- La Faucille fait 10 dégâts par coup, environ un coup par seconde, en arc.
- Le dernier commit (plan 24 C2c, §99) a ramené la touche à l'ellipse du coup dessiné. L'allonge verticale est divisée par 1,35 : c'est une baisse pour la mêlée, même si elle est justifiée visuellement.
- La foule qui fait front (plan 29 F2) garde les créatures au contact au lieu de les empiler. Une mêlée reste donc au contact plus souvent qu'une arme à distance.
- Aucun avantage propre à la mêlée ne compense le risque du contact : ni vol de vie, ni réduction de dégâts, ni bonus de dégâts.

### 3.4 L'XP

Courbe : 22 × n^1,42, majorée de ×1,65 à ×1,2 sur les niveaux 1 à 5. Il faut 36 XP pour le niveau 2, 483 pour le 5 et 2 870 pour le 10. Le niveau 2 vers 80 s est lent pour le genre : le premier choix arrive en général dans les 20 à 30 premières secondes. Surtout, l'XP suit les éliminations, et la mêlée en fait peu (§2). La courbe n'est qu'une partie du problème.

### 3.5 Le soin

Le Vagabond régénère 1 PV/s, soit ses 100 PV en 100 s. Le bot récupère environ 100 PV par minute, toutes sources confondues (régénération, micro-événements, Mémoriaux, bonus de soin au sol). C'est peu face à la pression, mais la pression est la vraie cause. À revoir seulement après les §3.1 et §3.2.

### 3.6 Le tempo des dix minutes

Calendrier actuel, du [plan 30](30-temps-forts.md) complété par T1–T2 et B2 : répit de 0:00 à 0:08, montée jusqu'à 1:23, micro-événement à 1:15, élites dès 1:30 (au plus deux en vie, une toutes les 35 à 55 s), micro-événement vers 2:30, **Souverain à 2:45** (×4 PV depuis T2), Résurgence de 4:00 à 5:10 (plafond ×1,4), micro-événement à 5:50, Souverain à 7:00, micro-événement à 7:05, Résurgence de 8:30 à 9:40, Barrière à 10:00. Les trois premières minutes empilent déjà élites, micro-événements et Souverain sur une ouverture sans répit, alors que le joueur a une seule arme.

## 4. Leviers proposés (rien d'appliqué)

Chaque levier passe par les données (`data/enemies`, `data/biomes`, `data/scaling`), sauf mention contraire. Après chaque lot, on refait la même mesure avec les mêmes graines, au Vagabond et au Traqueur.

### A. Les tireurs

| Option | Effet | Coût |
|---|---|---|
| **A1. Repousser et raréfier le Cracheur** : `spawn_from_minute` 3, `spawn_weight` 0,5, retiré des pools d'exploration de la forêt, des champs et de la carrière (retour à l'avant-E4) | Plus de tir pendant l'apprentissage, des tireurs rares ensuite | Données seules |
| **A2. Changer son comportement : crachat en cloche** qui laisse une flaque de bile au sol (annonce d'1 s comme le Présage, flaque de 2 à 3 s qui ralentit ou blesse) | On contourne la flaque en se plaçant dans la foule au lieu d'esquiver un projectile ; le Cracheur garde son identité de zone de contrôle | Une capacité à écrire, proche d'`omen_strike` |
| A3. Brider tous les tirs ennemis : durée de vol ramenée à la portée + 100 px, recharge sortie du code vers les données et allongée (1,1 s → 2 à 2,5 s) | Moins de projectiles qui traversent l'écran | Code (constante en dur), utile de toute façon |
| A4. Supprimer le Cracheur | Plus simple | Perte d'une créature déjà dessinée et sonorisée |

**Recommandation : A1 tout de suite, A2 ensuite.** A3 en complément, puisque la recharge en dur enfreint de toute façon la règle data-driven.

### B. Les créatures solides au début

| Option | Effet |
|---|---|
| **B1. Entrées échelonnées** : Rôdeur dès 1:30, Brute dès 3:00, Tréant dès 4:00 (`spawn_from_minute`) | Les premières minutes se jouent contre des créatures qu'on tue en 2 à 4 coups |
| B2. Rôdeur allégé : 60 → 45 PV, 15 → 11 dégâts | Le premier tank reste présent mais tenable |
| B3. Rampe des PV en ouverture : ×1,5 d'emblée → ×1,2 qui rejoint ×1,5 vers 3 min | Adoucit tout le début sans toucher aux fiches |
| B4. Revoir la pente de 4 à 8 min (×1,33/min) en mesurant au Vagabond **et** au build de référence | Le milieu de partie suit la puissance moyenne, pas la meilleure |

**Recommandation : B1 + B2**, puis B4 mesuré. B3 seulement si B1 ne suffit pas.

### C. La mêlée

| Option | Effet |
|---|---|
| C1. Ne rien faire de propre à la mêlée tant que A et B ne sont pas mesurés | Évite de corriger deux fois |
| C2. Dégâts de base des armes de mêlée ×1,2 à ×1,3 (Faucille 10 → 12 ou 13) | La mêlée tue plus vite ce qui la touche |
| C3. Une petite défense en mêlée, propre au Vagabond ou commune aux armes de mêlée : vol de vie au coup (plafonné comme l'objet) ou réduction de dégâts au contact | Récompense le risque du contact |

**Recommandation : C1, puis C2 si la mesure montre encore un écart de plus de 30 % d'éliminations avec le Traqueur.**

### D. Les niveaux

| Option | Effet |
|---|---|
| D1. Ouverture plus généreuse : majoration des niveaux 1 à 5 de ×1,65 → ×1,0 | Niveau 2 vers 30 à 40 s, premier choix plus tôt |
| D2. Courbe d'ensemble : `base_xp` 22 → 19 | Environ +10 % de niveaux sur toute la run |
| D3. Attendre A et B, qui rendent des éliminations, puis remesurer | Évite de pousser deux fois l'XP |

**Recommandation : D1 tout de suite** (il ne touche que les cinq premiers niveaux et ne crée pas de cascade), D2 seulement après la mesure de A et B.

### E. Le tempo

| Option | Effet |
|---|---|
| **E1. Ouverture plus aérée** : élites naturelles dès 2:30 au lieu de 1:30, une toutes les 50 à 75 s ; montée d'ouverture de 75 à 120 s | Les trois premières minutes servent à apprendre et à se construire |
| E2. Premier Souverain à 3:15 au lieu de 2:45 | Un peu plus de temps pour la deuxième arme |
| E3. Résurgence 1 de 4:00 à 4:30 | Tout le calendrier se décale ; la Barrière reste à 10:00 |

**Recommandation : E1**, puis E2 si le Souverain tombe encore avec une seule arme.

### F. Le soin

Rien dans l'immédiat. Si la mesure après A et B montre encore moins de 15 % de PV rendus face aux PV perdus, on pourra relever la part de bonus de soin au sol lâchés par les élites.

## 5. Découpage proposé (après arbitrage)

| Lot | Contenu | Vérification |
|---|---|---|
| **Q0** | Mesure de référence au Vagabond et au Traqueur ajoutée au protocole (profil neuf), à côté du build de Raphaël | Ce plan, §2 |
| **Q1** | A1 + B1 + B2 + D1 + D2 + E1, données seules | Même mesure, 4 graines ; cibles : Cracheur sous 8 % des dégâts, Rôdeur sous 20 %, niveau 5 avant 2:00, Vagabond à au moins 70 % des éliminations du Traqueur |
| Q2 | A2 : crachat en cloche et flaque (capacité, contrat, sons, captures) | Régressions des capacités ennemies, `/capture` |
| Q3 | A3 : recharge et durée de vol des tirs en données | Régressions, mesure |
| Q4 | B4, C, D2 selon la mesure de Q1 | Mesure aux deux profils |

Raphaël juge la sensation en jouant après Q1 : ce lot est conçu pour être annulé ou ajusté en une ligne de JSON.

## 6. Décisions du 10 octobre au soir

Réponses de Raphaël ([§101](DECISIONS.md)) : **A1 puis A2** pour le Cracheur, **B1 + B2** pour les créatures solides, **D1 + D2** pour l'XP (ouverture et courbe entière : `base_xp` 22 → 19), **E1** pour le tempo. C3 et F restent en attente de la mesure. Lot Q1 prêt à engager, en données seules : `fading_spitter.json`, les trois biomes, `rodeur.json`, `void_brute.json`, `treant_corrompu.json`, `progression.json`, `_variants.json` (élites naturelles) et `spawn_flow.json` (ouverture). Il sera mesuré avec le protocole du §1.

## 7. Q1 livré — 10 octobre 2026, soir

Raphaël : « ça me va que tu fasses les modifications dont on a parlé […] Si tu vois d'autres choses, n'hésite pas à les implémenter aussi », avec une cible d'XP : « plus viser à peu près le niveau 30 […] si on fait une plutôt bonne run. Voire […] un peu plus si on fait une excellente run » ([§103](DECISIONS.md)). Tout a été fait d'un bloc, puis mesuré une fois.

**Ce qui change :**
- **Cracheur (A1 + A2).** Il apparaît dès 3 min, avec un poids de 0,5, et sort des pools d'exploration de la forêt, des champs et de la carrière. Il ne tire plus : sa capacité `bile_lob` ([`BileLobAbility`](../../scripts/Combat/Abilities/BileLobAbility.cs), contrat dans `_contract.json`) fonctionne ainsi :
  - le Cracheur s'arrête 0,35 s ;
  - une goutte vole 0,9 s en cloche vers la place du joueur au moment du geste, avec un cercle au sol qui se remplit ;
  - qui est resté dessus est touché ;
  - une flaque reste 3 s et ralentit à ×0,65 ;
  - au plus 4 crachats en vol à la fois, toutes créatures confondues ;
  - la flaque disparaît avec le Cracheur, et un crachat en vol tombe si le Cracheur perd la trace du joueur.
- **Créatures solides (B1 + B2).** Rôdeur dès 1:30, ramené à 45 PV et 11 dégâts. Brute dès 3:00, Tréant dès 4:00. Pour que l'ouverture reste variée, les pools d'exploration gagnent l'Ombre (forêt, champs, carrière) et le Rampant d'Ombre (carrière).
- **XP (D1 + D2).** Courbe 15 × n^1,4 au lieu de 22 × n^1,42, sans majoration des cinq premiers niveaux. Le niveau 2 coûte 15 XP (36 avant) et le 5 en coûte 229 (483 avant).
- **Tempo (E1).** Élites naturelles dès 2:30, une toutes les 50 à 75 s. Montée d'ouverture sur 120 s au lieu de 75.
- **En plus (« d'autres choses ») :**
  - la pente des PV de 4 à 8 min passe de ×1,33 à ×1,27 par minute (B4), car elle suivait le build à distance ;
  - la cadence des tirs ennemis passe en données (`ranged_cooldown`, 1,5 s au lieu de 1,1 s en dur) : Tisseuse, Sentinelle et Hurleur tirent environ un quart moins souvent ;
  - un tir ennemi s'éteint à sa portée + 120 px (`projectile_reach_margin`) au lieu de voler 4 s, environ 740 px (A3).

**Mesure** (protocole du §1, mêmes graines ; la fusion du Facteur, arrivée pendant le lot, ne touche ni le Vagabond ni le Traqueur) :

| | Vagabond avant | Vagabond après | Traqueur avant | Traqueur après |
|---|---:|---:|---:|---:|
| Niveau à 10 min (4 graines) | 15–22, moy. 17,5 | **23–28, moy. 25,3** | 17–24, moy. 21,5 | **24–36, moy. 28,3** |
| Niveau à 5 min | 8–13 | 13–19 | 7–15 | 16–23 |
| Niveau 5 atteint | 2:37–2:48 | 1:21–2:09 | 1:39–3:58 | 1:16–1:48 |
| Éliminations en 10 min, moyenne | 524 | **926** | 764 | 1 208 |
| Cracheur, part des dégâts reçus | 14 % | **2 %** | 19 % | 3 % |
| Rôdeur, part des dégâts reçus | 30 % | **18 %** | 26 % | 19 % |
| « Morts » du bot de 0 à 5 min (il n'esquive pas) | 26–41 | 15–37 | 20–52 | 18–31 |

Le Vagabond fait 77 % des éliminations du Traqueur (69 % avant). Le bot prend ses cartes au hasard et ne s'écarte de rien : une bonne run humaine devrait se placer au-dessus de lui, vers 30 à 10 min, comme demandé. Le Présage devient la première source de dégâts à distance (12 à 13 %) ; c'est une frappe au sol annoncée, déjà dans l'esprit du genre.

**Vérifications.**
- Build sans avertissement.
- Validation globale : 32 suites sur 34 au premier passage, puis les deux restantes repassées.
  - `perk_effects` échouait parce que la fusion de la PR du Facteur a changé ce test pendant la validation.
  - `run_trace` attend 10 apparitions : avec l'ouverture plus lente et le niveau 2 plus tôt, l'écran de niveau mettait la partie en pause avant. Le test retire désormais l'arme du joueur et observe 120 s.
- 11 contrôles du crachat et du contrat ajoutés à `EnemyAbilityRegression` : arrêt, goutte sans projectile, touche sur place, flaque qui ralentit puis s'efface, esquive en avançant, annulation quand le joueur est perdu, disparition avec l'auteur. Les tests du tir annoncé passent à la Tisseuse.
- Relecture `godot-reviewer` : le crachat restait figé quand le Cracheur perdait la trace du joueur ; c'est corrigé et testé.
- Capture `CAPTURE_EXTRA_ARGS="--capture-bile" tools/capture_run.sh <dossier>` regardée. Les gouttes se voient en vol au-dessus des corps, le cercle se remplit sous le joueur, puis restent une flaque rouille tramée et le joueur grisé par le ralentissement.

**Reste à juger en jouant :**
- la lisibilité du crachat et de la flaque en pleine foule ;
- l'impact muet (aucun son d'éclaboussure dans la banque ; à chercher au plan 15) ;
- le rythme des niveaux, qui pourrait s'emballer après 15 min : la courbe d'ensemble descend d'environ un tiers ;
- le Souverain de 2:45, désormais affronté avec plus de niveaux.

C2 et C3 (avantage propre à la mêlée) restent en attente : l'écart avec le Traqueur est passé sous le seuil de 30 % fixé au §4.

## 8. Calendrier de 30 min jusqu'à l'Indicible — 10 octobre 2026, nuit

Raphaël, après Q1 : « remettre 50 hp de base pour le rodeur et 12 dégâts. aussi les 2 miniboss prévus dans les 10 min arrivent trop tôt […] il faut revoir le calendrier de la run. un truc qui tient en 30 min (moment de l'indicible) : après on sera dans le late game. dans ces 30 min il faut caler environ 4 résurgences, le boss intermédiaire (la barrière), 3/4 "miniboss", des élites » ; calendrier à 4 Souverains choisi ; « pour moi le late game c'est ce qui se passe après l'indicible […] à partir de 20 min on est en mid-game et à 30 on est déjà au début du late game » ([§108](DECISIONS.md)). Le late game a son étude propre : [plan 34](34-late-game.md).

**Calendrier retenu et mesuré** (build de référence, graine 42 ; les autres graines à quelques secondes près) :

| Heure visée | Mesuré | Événement |
|---|---|---|
| 0:00–2:30 | | Ouverture, élites dès 2:30 puis toutes les 50 à 75 s |
| 4:00 | 4:00 | Souverain 1 |
| 6:00 | 6:00 | Résurgence 1 (70 s) |
| 9:30 | 9:30 | Souverain 2 |
| 12:00 | 12:06 | Résurgence 2 |
| 15:00 | 15:00 | **Barrière** |
| 18:00 | 17:59 | Résurgence 3 (intensité 2) |
| 21:00 | 20:58 | Souverain 3 |
| 24:00 | 23:53 | Résurgence 4 (intensité 2) |
| 27:00 | 26:57 | Souverain 4 |
| 30:00 | 29:46 | Résurgence 5 et **Indicible** ; le late game commence |

**Réglages (données) :**
- `crises.json` : première Résurgence à 360 s, puis 290 ± 15 s après la fin de la précédente ; Indicible à la 5ᵉ ou à 1 800 s au plus tard ; late game à la 5ᵉ Résurgence ou à 90 % d'Effacement global (au lieu de 4 et 68 %), donc avec l'Indicible.
- `run_events.json` : Souverains à 240, 570, 1 260 et 1 620 s (au lieu de 150 et 420).
- `barrier.json` : levée à 900 s (au lieu de 600).
- `erasure.json` : vitesse de base ×22/30 et accélération ×(22/30)² ; seuil de late game à 0,9.
- `spawn_flow.json` : PV +13 % par minute après 8 min (au lieu de +17 %). À 1,17, les PV valaient ×46 à 22 min et auraient valu ×160 à 30 min ; à 1,13, ils valent ×74 à 30 min.
- `rodeur.json` : 50 PV et 12 dégâts, comme demandé.
- Quêtes de niveau remises à leur intention d'origine (plan 06 §9), que la nouvelle courbe d'XP rendait triviales : **Grandir** au niveau 30 (au lieu de 20), **Contre la montre** au niveau 30 avant 10 min (au lieu de 20). Au bot, la seconde n'est atteinte qu'une fois sur 8, comme le calibrage d'origine (1 sur 7).

**Mesures sur 30 min** (Vagabond en profil neuf, bot mortel ; build de référence, bot invincible, jusqu'à 35 min ; 4 graines chacun) :

| | Vagabond | Build de référence |
|---|---|---|
| Niveau à 10 min | 21–25 | 26–53 |
| Niveau à 20 min | 34–46 | 42–103 |
| Niveau à 30 min | 41–60 | 56–181 |

| Minute | 5 | 10 | 15 | 20 | 25 | 30 |
|---|---:|---:|---:|---:|---:|---:|
| Mémoire du sol sous le joueur | 0,82 | 0,72 | 0,60 | 0,48 | 0,34 | 0,19 |
| Effacement global | 0,10 | 0,23 | 0,37 | 0,53 | 0,69 | 0,86 |

L'Effacement suit l'étirement voulu : sol Fragile vers 10 min, Effiloché vers 20, Effacé vers 27 (au lieu de 7, 14–15 et 20).

**Boss :**
- **Barrière à 15 min.** Avec l'ancienne réserve (5 000 + 15 000 par battant, réglée à 10 min), le battant tenait **2 à 9 min** au Vagabond (127, 183, 426 et 538 s) et 27 à 315 s au build. À 15 min, 70 à 200 créatures entourent le bot devant la grille, et la plupart des coups vont à la foule : environ 90 dégâts par seconde médians sur le battant. Réserve ramenée à **2 000 + 6 000 par battant** : 8 000 PV pour un battant, 20 000 pour trois, 32 000 pour cinq. Remesuré au Vagabond (20 min, mêmes graines) : battant brisé en **67, 68, 75 et 116 s**.
- **Indicible à 30 min**, réserve inchangée (288 000 PV) : vaincu en 13 s et en 140 s dans deux graines du build ; dans les deux autres, il n'a pas perdu un tiers de sa réserve en 5 min. À 22 min, on mesurait 22, 56, 141 et plus de 240 s. Le bot ne va pas chercher les mains : la mesure ne départage pas, et le réglage se fera à la partie de Raphaël.

**Vérifications.**
- Build sans avertissement.
- Suites relancées après le calendrier : smoke, boss_parts, indicible, quests, run_phase, catalogs, erasure_active, run_trace et enemy_abilities, toutes vertes.
- Deux tests suivaient l'ancien rythme :
  - le parcours d'`ErasureActiveRegression` passe de 30 à 45 min, la durée où l'Effacement redevient complet ;
  - le scénario de date limite de `QuestRegression` simule le niveau 30.

**À juger en jouant :**
- la place des Souverains et de la Barrière dans la partie ;
- la tenue du milieu de partie, de 15 à 30 min ;
- la longueur du combat contre l'Indicible à 30 min ;
- le rythme des niveaux sur 30 min (au bot : 41 à 60 au Vagabond, 56 à 181 avec le build de référence).

# Plan 35 — Catalogue d’objets V2 : 56 objets

12 octobre 2026. **Icônes propres des 34 nouveaux objets livrées le même jour (§7).** Complément du [plan de rareté et d’équilibrage](35-raretes-des-objets.md), après les deux séries de retours de Raphaël (DECISIONS §128 et §132). **Plan validé et implémenté (DECISIONS §133), hors nouveaux sprites et animations.** Les 22 objets existants et les 34 nouveaux ont leurs raccordements et contrôles par lot ; recette globale et équilibre des builds se distinguent de ces preuves techniques. Les [quinze quêtes Q sont intégrées](35-quetes-objets-v2.md), activation vérifiée (DECISIONS §138). Le plan principal fait autorité sur les 22 effets révisés.

## 1. Inspiration et identité

La [liste Fandom fournie par Raphaël](https://megabonk.fandom.com/wiki/Items) a été consultée via son contenu indexé : l’ouverture directe était bloquée. Le [guide de mécanique d’IndieSnek](https://www.megabonkinfo.org/) a aussi été consulté le 12 octobre ; il indique une base de tests en jeu et de données extraites. Les versions/nombres divergent entre sites : ils ne servent pas de référence chiffrée pour Vestiges.

Ce qu’on retient de Megabonk : objets de stats immédiatement compréhensibles, effets déclenchés par les actions du joueur, cumuls explicites, compromis et changements de règles aux rangs élevés. Son catalogue illustre aussi la nécessité de distinguer probabilité, puissance et délai interne. On ne reprend ni ses valeurs ni ses objets transformés en simples synonymes.

Pour Vestiges, la fonction du petit objet quotidien explique l’effet : une étiquette aide à retrouver, une cassette rembobine, un carnet donne des adresses, un tampon annule. Les effets d’exploration suivent la route et l’Effacement. Référence locale : `VESTIGES-BIBLE.md` §1 et §3.2, mémoire collective et quotidien humain ; aucune réintroduction du craft, de la construction ou du Foyer en run.

## 2. Règles communes

- `n` : copies portées. Chaque copie renforce le même effet, sans changement de comportement au doublon. Cumul additif sauf formule explicite. Copies illimitées pour tous les objets ordinaires ; seules les limites des légendaires et objets uniques restent applicables. Les plafonds de sécurité des effets sont distincts des limites de copies.
- **D** : disponible dans le tirage d’un profil neuf, sans être équipé au départ. **Q** : effet implémenté, accessible en dev, verrouillé par `initial_unlock: false` en profil normal jusqu’à activation de sa [quête préparée](35-quetes-objets-v2.md). Aucun de ces quinze nouveaux objectifs n’est encore suivi en run.
- Un bonus simple de +8 % × n donne +8/+16/+24 %. Appliquer l’écart exact lors de l’ajout/retrait ; pas une multiplication répétée. Le plafond d’armure et les immunités des boss restent valables ; le vol de vie suit le nouveau réglage du plan principal §3.1.
- Les valeurs sont des premiers réglages. Un effet ne devient pas équilibré par sa seule rareté ; les mesures peuvent conduire à modifier valeur, plafond ou rang.

**Noms :** privilégier des noms courts et reconnaissables, varier les constructions plutôt que multiplier « objet de métier ». Les nouveaux IDs ci-dessous ont été introduits dans ce lot : aucune migration d’ancien identifiant nécessaire. Les IDs des 22 objets déjà livrés restent stables.

## 3. Les 34 nouveaux objets

### 3.1 Statistiques globales — 18 objets

| Rareté | Objet proposé (`id`) | Effet par copie | Accès |
|---|---|---|---|
| Commun | Dossard froissé (`dossard_froisse`, ex-Lacets noués) | **+8 % vitesse de déplacement**. Le numéro d'une course qu'on n'a jamais finie. | D |
| Commun | Pastille mentholée (`pastille_mentholee`) | **+9 PV/min de régénération par copie**, rendus progressivement ; respecte les 2 s sans régénération après blessure. Débit théorique : 9/18/27 PV/min avec 1/2/3 copies, pas un soin groupé toutes les minutes. | D |
| Commun | Bouillotte (`bouillotte`, ex-Gros bouton) | **+12 PV max** ; ne soigne pas les PV manquants lors d’un échange. | D |
| Commun | Agrafe rouillée (`agrafe_rouillee`) | **+1 armure**, selon la formule et le plafond du joueur. | D |
| Commun | Bons points (`bons_points`) | **+6 % XP gagnée**. | D |
| Commun | Capsule porte-bonheur (`capsule_porte_bonheur`) | **+3 points de Chance** (valeur interne +0,03). Améliore aussi les probabilités des raretés élevées dans les coffres, via la formule du plan principal §5.1 ; ne change jamais la rareté fixe d’un objet. | Q |
| Commun | Bande magnétique (`bande_magnetique`) | **+15 % rayon d’attraction de l’XP**. Un morceau de cassette qui attire encore les souvenirs épars. | D |
| Inhabituel | Petite clé (`petite_cle`) | **+6 % cadence d’attaque**. | D |
| Inhabituel | Compas (`compas`) | **+10 % taille des zones d’attaque** selon la grammaire de chaque arme. | D |
| Inhabituel | Longue-vue (`longue_vue`, ex-Ruban gradué) | **+10 % portée des attaques** compatibles. | D |
| Inhabituel | Verre fêlé (`verre_fele`) | **+3 points de chance critique**. Disponible dès le profil neuf conformément à DECISIONS §131 ; pas de critique de base ajouté. | D |
| Inhabituel | Yo-yo (`yoyo`, ex-Ressort) | Dash qui se **recharge 8 % plus vite** par copie : durée de recharge divisée par `1 + 0,08n`. Ne donne pas de dash gratuit ni d’invulnérabilité. | D |
| Inhabituel | Bocal de conserve (`bocal_de_conserve`, ex-Ficelle cirée) | **+12 % durée des statuts**, dans les plafonds anti-contrôle continu des objets. | Q |
| Rare | Peigne cassé (`peigne_casse`) | **+1 perforation** aux attaques compatibles. | Q |
| Rare | Ampoule rouge (`ampoule_rouge`) | **+0,5 point de vol de vie**, sur les dégâts éligibles réellement infligés (surplus au-delà des PV restants exclu), avec plafond partagé entre les armes proposé à **8 % des PV max/s**. Pas de plafond commun avec la régénération ; une copie ne garantit jamais ce débit. | Q |
| Rare | Bille irisée (`bille_irisee`) | **+25 points au multiplicateur critique** (×1,5 devient ×1,75 avec une copie). | Q |
| Épique | Boucle cabossée (`boucle_cabossee`) | **+15 bouclier maximum**, selon la défense existante ; ne pas confondre points de bouclier et nombre de coups annulés. | D |
| Épique | Diapositive en double (`diapositive_en_double`) | **+1 projectile**, y compris **+1 satellite aux armes orbitales**, via une même statistique de quantité. Une seule application du bonus à chaque arme. Un souvenir photographié deux fois. | D |

Ces objets de stats sont volontairement simples. Leur fonction est de compléter les quatre Écussons sans imposer une condition artificielle à chaque petite trouvaille. Ne pas réutiliser les IDs des anciens objets devenus Écussons.

### 3.2 Propriétés propres aux armes — 6 objets

Ce lot exige de nouveaux raccordements : être affichée dans `data/ui/stats.json` ne rend pas une statistique automatiquement applicable par un objet. Montrer les armes concernées sur la fiche ; ne pas laisser croire qu’un bonus agit sur toutes les armes.

| Rareté | Objet proposé (`id`) | Effet par copie | Accès |
|---|---|---|---|
| Commun | Tampon élastique (`tampon_elastique`) | **+15 % recul** des attaques compatibles ; respecte les résistances des boss. | Q |
| Inhabituel | Fléchette mousse (`flechette_mousse`) | **+15 % vitesse des projectiles** ; ne change ni leur nombre ni la cadence. | D |
| Inhabituel | Roulement lisse (`roulement_lisse`) | **+12 % vitesse des attaques orbitales** ; conserver leur limite de touches par cible. | Q |
| Inhabituel | Cordon spiralé (`cordon_spirale`) | **+15 % distance de rebond/liaison** des armes compatibles. | Q |
| Inhabituel | Fer à cheval miniature (`fer_a_cheval_miniature`) | **+15 % de capacité à tourner vers la cible** pour un projectile déjà autoguidé : il corrige sa trajectoire plus vite. Ni chance de toucher, ni portée, ni vitesse de déplacement ; aucun effet sur un projectile droit. Afficher les armes concernées. | Q |
| Rare | Domino ébréché (`domino_ebreche`) | **+1 cible de chaîne/rebond** pour les armes compatibles. | Q |

Les paramètres de zone propres aux armes (`arc_angle`, `cone_angle_end`) passent par le Compas ; confirmer leur raccordement avec les formes touchées, pas seulement le dessin. **Quantité commune :** l’Anneau de trousseau est retiré. Les orbitales reçoivent les mêmes bonus de quantité que les autres projectiles, donc la Diapositive. Faire converger le nombre de base et la progression `orbital_count` vers `projectile_count`, puis ajouter `projectile_bonus` une seule fois ; migrer les gains de niveau/améliorations existants sans perte ni double cumul. `chain_targets` garde son rôle distinct de nombre de rebonds. Cette unification est implémentée ; les tests vérifient quantités réelles, gains, ascensions et retrait sans double cumul.

### 3.3 Objets qui changent la façon de jouer — 10 objets

| Rareté | Objet proposé (`id`) | Effet et cumul du même effet | Accès |
|---|---|---|---|
| Commun | Étiquette perdue (`etiquette_perdue`) | **Conditionne les flèches de direction des coffres** : absentes sans l’objet ; une copie indique le coffre fermé hors écran le plus proche, n copies les n plus proches, au plus trois flèches (copies illimitées). Portée de repérage existante inchangée ; copies = davantage de directions, pas davantage de portée. Ne révèle pas le contenu et ne masque pas les flèches d’événements/lieux révélés. L’Oubli des repères garde priorité. | D |
| Inhabituel | Ticket de vestiaire (`ticket_de_vestiaire`) | Les objets au sol restent **45 s × n plus longtemps** avant leur disparition naturelle. L’Effacement continue à les détruire ; ne ressuscite pas un objet déjà disparu. | Q |
| Rare | Pied-de-biche (`pied_de_biche`) | **15 % × n de chance d’ouvrir un coffre sans payer l’Essence**, probabilité plafonnée à 60 %, sans limite de copies. Un seul jet mémorisé par coffre ; aucune relance en annulant, revenant ou changeant le sac. Peut réussir même sans assez d’Essence. La Chance agit sur le butin, pas sur ce jet ; transaction détaillée au plan principal §5.2. | Q |
| Rare | Sonnette (`sonnette`) | À la fermeture du butin d’un coffre nouvellement ouvert, attire l’XP déjà présente dans **150 px × n** autour du coffre. Une seule activation par coffre, aucun gain d’XP créé. | D |
| Épique | Registre biffé (`registre_biffe`) | Toucher une créature avec **deux armes différentes en moins de 4 s** lui inflige un impact additionnel de **25 % × n de la somme des deux coups de référence**. Déclenchement au plus toutes les 2 s, global au joueur ; les dégâts d’objets ne comptent pas comme une troisième arme. | Q |
| Épique | Pelote rouge (`pelote_rouge`) | Un dash relie les **deux dernières créatures distinctes touchées**, encore vivantes et à moins de 250 px l’une de l’autre. Pendant 3 s, **min(15 % × n, 60 %)** des dégâts d’arme infligés à l’une sont aussi infligés à l’autre. Sans limite de copies ; délai de 6 s, un seul lien ; pas de transfert récursif, ni de lien entre deux parties d’un même boss. | Q |
| Épique | Tampon ANNULÉ (`tampon_annule`) | Pendant un dash, efface les **n premiers projectiles ennemis rencontrés**. Ne protège pas d’un corps-à-corps, d’un rayon, d’une zone au sol ou du Néant. Le saut instantané utilise le trajet balayé ; chaque projectile ne disparaît qu’une fois. | D |
| Légendaire | Carnet d’adresses (`carnet_dadresses`) | Chaque récompense d’objet d’un coffre devient **un choix parmi 1+n objets du rang tiré**, au lieu d’un objet imposé. Trois copies au plus ; on reçoit toujours un seul objet par récompense, pas toute la sélection. Pas de nouveau tirage de rareté en ouvrant l’écran. | D |
| Légendaire | Cassette rembobinée (`cassette_rembobinee`) | À la fin d’un dash, rend **25 % × n des PV de combat perdus dans les 3 dernières secondes**. Trois copies au plus, 12 s entre activations. Ne ressuscite pas, ne rend pas les pertes du Néant/auto-infligées et ne restaure ni position, ni Essence, ni inventaire. Les pertes déjà remboursées sont consommées du relevé. | D |
| Légendaire | Portrait effacé (`portrait_efface`) | Après un dash, laisse au point de départ une **silhouette-souvenir pendant 2 s × n**, maximum 3 copies (6 s), recharge 12 s. Les créatures ordinaires à moins de 220 px ciblent ce leurre tant qu’il existe et reste une cible valide ; les attaques déjà engagées continuent. Aucun dégât, aucune collision, aucun blocage des tirs ; élites et boss ignorent le leurre. Un seul leurre, détruit par l’Effacement ; pas de recharge contournée par rééquipement. | Q |

**Pourquoi ces trois légendaires :** le Carnet fait mieux choisir les trouvailles ; la Cassette rend une partie des blessures récentes ; le Portrait détourne les créatures vers le souvenir d’une présence. Le Plan cadastral et son exception locale à l’Effacement sont abandonnés. Le Portrait exige un vrai raccordement au ciblage des ennemis ; un dessin seul ne suffit pas.

## 4. Couverture des statistiques

Les **19 stats globales existantes** sont toutes couvertes ; aucune nouvelle statistique générique n’est nécessaire pour ce premier groupe.

| Stat réelle | Objet(s) proposé(s) |
|---|---|
| `damage` | Semelle, Tabouret, Thermos, Médaille, Thermomètre, Épingle, Gilet, Porte-monnaie |
| `speed` | Dossard froissé |
| `max_hp` | Bouillotte |
| `attack_speed` | Petite clé |
| `aoe_radius` | Compas |
| `attack_range` | Longue-vue |
| `regen_rate` | Pastille mentholée |
| `lifesteal` | Ampoule rouge |
| `armor` | Agrafe rouillée |
| `shield` | Boucle cabossée |
| `crit_chance` | Verre fêlé |
| `crit_multiplier` | Bille irisée |
| `projectile_pierce` | Peigne cassé |
| `xp_magnet_radius` | Bande magnétique |
| `luck` | Capsule porte-bonheur |
| `xp_gain` | Bons points |
| `dash_recharge` | Yo-yo |
| `projectile_bonus` | Diapositive en double |
| `status_duration` | Bocal de conserve |

Propriétés d’armes supplémentaires : `knockback`, `projectile_speed`, `orbital_speed`, `chain_range`, `homing_strength`, `chain_targets` via §3.2 ; quantité commune `projectile_count` et `projectile_bonus` via Diapositive, orbitales incluses ; angles et taille via Compas. `peril` reste représenté par le Sifflet. Évasion, épines et sursoin ne sont pas ajoutés simplement parce qu’ils existent dans Megabonk.

## 5. Lots de production

Lire d’abord I10a–I10e du [plan principal](35-raretes-des-objets.md#6-lots-dimplémentation--un-à-la-fois). Leur modèle de cumul et leurs budgets doivent être livrés avant d’étendre le catalogue. Ces lots sont implémentés ; leurs preuves techniques sont consignées au [plan principal I11](35-raretes-des-objets.md#i11--extension-du-catalogue). Les icônes existantes sont réutilisées conformément à l’exclusion des nouveaux sprites/animations.

| Lot | Contenu | Patterns à lire et vérifications |
|---|---|---|
| I11a | 18 objets de statistiques globales, en deux fournées de 9 | `Player.ApplyPerkModifier`, contrat `player_stats`, oubli dans `Player.Objects`. Vérifier tous les gains/retraits, plafonds de défense, absence de soin gratuit, paliers d’Écussons intacts, icônes et quêtes. |
| I11b | 6 objets propres aux armes et unification des quantités orbitales | Contrat `data/weapons/weapon_contract.json`, `Player.GetWeaponStat`, consommateurs de chaque arme. Ajouter un raccordement explicite si absent ; tests d’armes compatibles/incompatibles, valeurs réelles et rendu identiques, bornes de nombre et banc dense pour les orbitales. |
| I11c | 7 utilitaires/épiques du §3.3, après les coffres payants I10e | Interactions `ChestOpened`, inventaire du monde, provenance `AttackContext`/`DamageKind.Passive`, `PlayerMobility.DashesStarted`. Les nouveaux événements/APIs sont à créer, pas à supposer. Tester ouverture une seule fois, expiration, changement de biome, salves, disparition d’une cible, parties de boss, pools et coût par image. |
| I11d1 | Carnet d’adresses | `ChoiceScreen`, `LootRewards`, exemplaires promis, `ItemExchange`. Tirer le rang une seule fois, candidats distincts accessibles ; si moins de candidats disponibles, montrer seulement ceux-là. Réserver uniquement l’objet choisi entre les récompenses d’un même coffre. Sac plein et annulation sans relance ; captures et manette. |
| I11d2 | Cassette rembobinée | Relevé borné des pertes de PV de combat et événements de dash. Pas de soins comptés comme dégâts, pas de résurrection ; éliminer les pertes remboursées et les garder consommées après oubli/rééquipement. Aucun cercle soin→dégâts→soin. Tests perte fractionnaire, bouclier, soin intermédiaire, pause et mort. |
| I11d3 | Portrait effacé | Raccorder réellement la priorité de cible des ennemis ordinaires, puis leur retour au joueur à la disparition du leurre. Conserver les attaques déjà engagées, résistances des élites/boss, collision et Effacement. Pool, tests de cible expirée, dash répété, oubli et banc dense ; capture montrant une vraie déviation des ennemis. |

**Pour chaque fournée :** IDs, données, contrat, effets réellement branchés, icônes, traduction, accès et descriptions ; build sans warning, suites utiles et capture réelle avant la suivante. Pas de fiche active sans implémentation complète.

**Déblocages :** les 14 quêtes d’objets existantes sont conservées ; les 34 nouveaux se répartissent en 19 D et 15 Q. Les [quinze quêtes Q intégrées](35-quetes-objets-v2.md) utilisent des faits déjà suivis, sans dépendance à l’objet récompensé. Elles sont actives ; le Pied-de-biche exige 50 coffres cumulés. Profil neuf : 27 objets accessibles, dont Boucle cabossée, Diapositive et Tampon ANNULÉ (épiques), Carnet et Cassette (légendaires). Ces hauts rangs restent soumis au tirage, jamais garantis.

**Recette globale :** I12 du plan principal, avec contrôle automatique de cette matrice contre les vraies clés du contrat. Mesurer les objets ignorés, le remplissage du sac et la dilution des synergies ; 56 objets ne justifient pas à eux seuls d’augmenter la capacité du sac.

## 6. Contrat de livraison : effet réel, statistiques et retour visuel

Exigence explicite de Raphaël : aucun objet ne peut être déclaré livré sur la seule base de sa fiche JSON ou de son texte.

- **Application réelle :** tester chaque effet avec l’objet absent, une copie, plusieurs copies, puis retrait ; mesurer le résultat (PV, distance, nombre de projectiles/satellites, durée, ciblage, Essence débitée), pas seulement le modificateur stocké. Couvrir les armes compatibles et celles qui ne le sont pas.
- **Pause :** toutes les 19 statistiques globales, même nulles, plus les valeurs effectives propres à chaque arme. Inclure les stats auparavant absentes ou cachées (`xp_gain`, recharge du dash, vol de vie, bouclier, quantités…), en indiquant les unités. Régénération en **PV/min**, y compris les fiches d’objets et la Collection ; le moteur peut garder les PV/s en interne. Afficher base, total et contributions consultables. Bonus conditionnels : condition, valeur potentielle et état actuel ; pas de bonus affiché comme permanent.
- **Limites lisibles :** taux de statut, plafond partagé entre armes, recharge, nombre de copies et prochain cumul dans la fiche. Vol de vie : taux prélevé et plafond en PV/s distincts du débit passif de régénération. Quantité orbitale visible dans la même famille « projectiles », pas un compteur de bonus concurrent.
- **En jeu :** garder brûlure/ralentissement/poison lisibles ; signal bref quand le Pied-de-biche économise l’Essence, flèches uniquement avec l’Étiquette, nombre de satellites réellement augmenté, leurre qui attire vraiment. Retour visuel quand il aide à comprendre, sans ajouter une particule par bonus statistique ni un encombrement permanent.
- **Preuves avant activation :** assertions de résultat en run/banc, captures pause et effets à 720p/1080p, clavier et manette ; aucun texte trompeur si une arme est incompatible. Les effets fréquents utilisent les pools existants. L’objet reste hors tirage tant que ces raccordements ne sont pas complets.

## 7. Icônes des 34 nouveaux objets — préparation du 12 octobre 2026

Demande de Raphaël : « il y a des nouveaux objets, il faudrait créer leurs sprites ». **S10a–S10c livrés le 12 octobre (§7.6) ; S10d reporté.** Aujourd'hui chacun des 34 objets du §3 prête l'icône d'un Écusson ou d'un autre objet (`icon` de `data/progression/passive_souvenirs.json`) : Capsule et Écusson du forain partagent le jeton, Verre fêlé et Bille irisée les lunettes, quatre objets le Plan de métro. Cible : une icône 32 × 32 et sa version 16 × 16 par objet, aucune icône partagée.

### 7.1 Règles de dessin

- Même pipeline et même vue que les objets du plan 25 (S2/S3) et les armes validées : modèles SDF dans `tools/sprites/items/`, lumière haut-gauche, quatre tons, contour sel-out, rendu natif en 32 et en 16 px, sans tirage aléatoire. `tools/generate_item_icons.py` reçoit deux lots, `v2a` et `v2b`.
- **Silhouette avant détail** : chaque icône se reconnaît à 16 px parmi les 56 objets et les 16 Écussons. Pas deux papiers rectangulaires de la même couleur, pas deux spirales, pas deux disques métalliques.
- **Conflits avec les Écussons**, qui gardent les dessins des anciens objets de stats : Lacets noués / Lacet rouge, Gros bouton / Bouton de manteau, Ressort / Ressort de sommier, Ruban gradué / Mètre pliant, Ficelle cirée / Bobine de fil. Raphaël : « il faudrait repenser ces objets (même fonctionnement mais identité différente) ». Nouvelles identités proposées au §7.2, à juger sur la planche S10a ; noms et IDs renommés seulement après validation (références : fiches, `data/quests/quests.json`, traductions, `tools/tests/`).
- Rareté : **cadre seul** (Raphaël, 12 octobre). L'icône reste neutre, pas de doré ni d'effilochure liés au rang.
- Un dessin sert l'effet quand c'est possible : le crayon dans la cassette rembobine, la pelote laisse filer un fil, le visage du portrait s'efface.

### 7.2 Sujets proposés

| Objet | Sujet de l'icône | Couleur dominante | Écart à surveiller |
|---|---|---|---|
| Lacets noués → **Dossard froissé** | Dossard de course en tissu blanc, gros numéro, coins épinglés, un coin replié | Blanc, rouge | Photo de classe, bons points |
| Pastille mentholée | Boîte ronde en fer entrouverte, pastilles blanches, couvercle vert menthe | Vert, acier | Jeton, rondelle |
| Gros bouton → **Bouillotte** | Bouillotte en caoutchouc rouge striée, bouchon vissé, un peu affaissée | Rouge | Thermos |
| Agrafe rouillée | Barrette d'agrafes entamée, une agrafe dépliée devant, rouille orangée | Rouille | Épingle à nourrice |
| Bons points | Deux cartons « bon point » décalés, bords bleu et rouge, grosse étoile dorée (trois cartons devenaient illisibles à 16 px) | Papier, rouge, bleu | Photo de classe |
| Capsule porte-bonheur | Capsule de bouteille dentelée, cabossée, trèfle peint dessus | Rouge, acier | Jeton de fête (Écusson du forain) |
| Bande magnétique | Petite bobine ouverte à flasques blanches, bande brune enroulée dont la queue s'échappe (le ruban seul se lisait comme une chaîne) | Blanc, brun | Cassette rembobinée, capsule |
| Petite clé | Petite clé de journal intime en laiton, anneau, panneton simple | Laiton | Épingle |
| Compas | Compas d'écolier ouvert en V, pointe et mine, molette | Acier, bleu | Ruban gradué |
| Ruban gradué → **Longue-vue** | Petite longue-vue de laiton dépliée, trois tubes, objectif de verre (l'antenne télescopique essayée d'abord se lisait comme un bâton) | Laiton | Stylo, thermomètre |
| Verre fêlé | Verre de cantine strié, fêlure en éclair, un éclat détaché | Verre | Lunettes, presse-papier |
| Ressort → **Yo-yo** | Yo-yo de bois peint vu de trois quarts, ficelle qui se déroule avec sa boucle | Bleu, bois | Roulement, capsule |
| Ficelle cirée → **Bocal de conserve** | Bocal à joint orange et fermeture métallique, fruits au sirop, étiquette manuscrite | Verre, orange | Flacon de pharmacie, ampoule |
| Peigne cassé | Peigne de poche noir cassé en deux, dents manquantes | Sombre | Domino |
| Ampoule rouge | Ampoule à vis de chambre noire, verre rouge, filament visible | Rouge, laiton | Flacon de pharmacie |
| Bille irisée | Grosse bille de verre, torsade colorée à l'intérieur, reflet | Verre, violet | Presse-papier |
| Boucle cabossée | Boucle de ceinture carrée en métal, cabossée, ardillon tordu | Acier, laiton | Médaille |
| Diapositive en double | Deux diapositives 35 mm à cadre blanc superposées et décalées, image bleutée | Papier, bleu | Photo de classe |
| Tampon élastique | Butoir de porte conique en caoutchouc noir, vis dépassant (le « tampon » qui fait rebondir) | Sombre | Tampon ANNULÉ |
| Fléchette mousse | Fléchette en mousse orange à ventouse | Orange, bleu | Pied-de-biche |
| Roulement lisse | Roulement à billes vu de trois quarts, billes visibles, graisse | Acier | Rondelle de cuivre |
| Cordon spiralé | Cordon de téléphone beige en spirale serrée, fiche au bout | Beige | Ressort |
| Fer à cheval miniature | Petit fer à cheval porte-bonheur, clous, ruban rouge noué | Acier, rouge | Trèfle plastifié |
| Domino ébréché | Domino ivoire debout, points noirs, coin ébréché | Ivoire | Peigne, bons points |
| Étiquette perdue | Étiquette de bagage en kraft, œillet, ficelle, adresse griffonnée | Kraft | Ticket de consigne |
| Ticket de vestiaire | Jeton numéroté en plastique rouge suspendu à un crochet de vestiaire | Rouge, acier | Tickets papier |
| Pied-de-biche | Pied-de-biche rouge en diagonale, pied fendu, éclats de peinture | Rouge, acier | Fléchette |
| Sonnette | Sonnette de vélo chromée, levier, coque bombée | Acier | Boîte de pastilles |
| Registre biffé | Gros registre relié toile ouvert, lignes barrées à l'encre rouge | Toile verte, papier | Carnet, plan de métro |
| Pelote rouge | Pelote de laine rouge, un fil qui part et serpente | Rouge | Bobine, gros bouton |
| Tampon ANNULÉ | Tampon encreur à manche bois sur un papier marqué d'un rectangle rouge « ANNULÉ » | Bois, rouge | Butoir |
| Carnet d'adresses | Petit carnet à onglets alphabétiques, cuir sombre, tranche dorée | Cuir, doré | Registre |
| Cassette rembobinée | Cassette audio, crayon engagé dans une bobine, bande qui pend | Sombre, doré | Bande magnétique |
| Portrait effacé | Portrait ovale dans un cadre doré, visage qui s'efface en pixels violets | Doré, violet | Photo de classe |

### 7.3 Lots

| Lot | Contenu | Preuves |
|---|---|---|
| **S10a** | 18 objets de statistiques (§3.1), module `tools/sprites/items/catalog_v2.py`, `--lot v2a` | Planche 32/16 sur fond sombre avec les 22 objets existants et les 16 Écussons en référence, inspectée ; régénération identique octet pour octet. **Validation de Raphaël avant export.** |
| **S10b** | 16 objets d'arme et de jeu (§3.2, §3.3), `--lot v2b` | Mêmes preuves. |
| **S10c** | Export validé : PNG + `.import` (smoke test), champs `icon`/`icon_small` des 34 fiches, `items_manifest.json`, `icon_color` relu | Build 0 warning, smoke, `tools/test_objects.sh`, captures du choix de coffre, de la pause et de la Collection à 720p et 1080p regardées. |
| S10d (à décider) | Visuels en jeu exclus au §133 : silhouette du Portrait, fil de la Pelote, éclat du Tampon ANNULÉ, signal du Pied-de-biche, onde de la Sonnette | Lot séparé, pools existants ; non lancé sans accord. |

### 7.4 Questions à Raphaël

1. ~~Conflits avec les Écussons~~ : cinq objets repensés, identités validées « à 100 % » et renommées (§7.6).
2. ~~Rareté sur l'icône~~ : cadre seul.
3. S10d (visuels en jeu) : reporté par Raphaël le 12 octobre (DECISIONS §140) ; non lancé.

### 7.5 S10a — planche proposée, 12 octobre 2026

`python3 tools/generate_item_icons.py --lot v2a --sheet doc/plans/planches/35-s10a-objets.png` produit la [planche S10a](planches/35-s10a-objets.png) : les 18 objets de statistiques, modèles dans `tools/sprites/items/catalog_v2.py`, rendu natif 32 et 16 px, sur fond sombre et sur le cadre de carte. Références sur la même planche : armes validées, les six Écussons dont un objet pouvait se rapprocher, jeton, thermos et flacon. Cinq couleurs ajoutées à `tools/sprites/items/kit.py`, toutes de la charte (menthe, laiton, orange, bande, écaille) ; les modèles existants n'en utilisent aucune, leurs PNG ne changent pas.

Planche inspectée ; quatre sujets repris après une première passe (bons points, bande magnétique, antenne remplacée par la longue-vue, ampoule trop ronde qui se lisait comme un gant). Deux générations identiques : SHA-256 `b6757f7e669da804e9015913c27e89148c5e7a5f5c6c454b8201084367eac7cc`.

**Soumise à Raphaël : sujets, lecture à 16 px et cinq nouvelles identités (Dossard froissé, Bouillotte, Yo-yo, Longue-vue, Bocal de conserve).** Rien n'est exporté dans `assets/`, aucune fiche ni aucun ID n'est modifié. Suite : S10b.

Planche S10a validée par Raphaël, ainsi que les cinq identités (« je valide à 100 % ces nouvelles identités et la planche aussi »).

### 7.6 S10b et S10c — livrés, 12 octobre 2026

**S10b.** `--lot v2b` produit la [planche S10b](planches/35-s10b-objets.png) : les 16 objets d'arme et de jeu. Première passe inspectée ; repris le cordon (spirale fondue en boudin), le pied-de-biche (lu comme un sucre d'orge : acier noirci, peinture rouge sur la tige), la sonnette (de profil, un casque ; de dessus, une poêle tant que le collier faisait manche), le jeton de vestiaire (rond, lu comme une bombe : oblong), le carnet (onglets invisibles : couleurs), la cassette (noire sur fond sombre) et le butoir (tassé). Deux couleurs de la charte ajoutées au kit : caoutchouc `#6B6161`, kraft `#B4A080`. La planche S10b est passée sans validation séparée : Raphaël a demandé de « faire le reste » dans la foulée.

**S10c.** Renommage des cinq identités : `lacets_noues` → `dossard_froisse`, `gros_bouton` → `bouillotte`, `ressort` → `yoyo`, `ruban_gradue` → `longue_vue`, `ficelle_ciree` → `bocal_de_conserve`, dans les fiches, la récompense de la quête « Garder le fil » (ID de quête inchangé) et trois scènes de test. Aucune migration : les déblocages se déduisent des quêtes accomplies (`MetaSaveManager.IsUnlocked`), jamais d'un ID d'objet sauvegardé. Effets, valeurs et raretés inchangés ; trois descriptions reçoivent une phrase d'identité.

68 PNG exportés dans `assets/items/icons/` avec leurs `.import` (smoke test), `items_manifest.json` complété (les objets V2 n'y sont pas marqués « monde »), `icon`/`icon_small` des 34 fiches. Contrôle : 56 objets et 16 Écussons, aucune icône partagée, aucun fichier manquant. Aucun PNG existant modifié par l'export. `icon_color` laissé tel quel : il ne sert qu'en l'absence d'icône.

Vérifications : smoke vert ; `test_objects.sh`, `test_quests.sh`, `test_catalogs.sh`, `test_inventory.sh`, `test_item_loot.sh` à zéro échec. Captures 4K regardées : pause avec six nouveaux objets dans le sac et HUD en 16 px (`--capture-pause --capture-new-items`), galerie de butin de tous les objets en profil dev (`--capture-loot --capture-loot-gallery`). Nouveaux noms et icônes visibles, lisibles sur les cartes de rareté. Planches S10a et S10b régénérées identiques octet pour octet.

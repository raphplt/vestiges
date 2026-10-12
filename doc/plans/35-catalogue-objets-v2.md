# Plan 35 — Catalogue d’objets V2 : 57 propositions

12 octobre 2026. Complément du [plan de rareté et d’équilibrage](35-raretes-des-objets.md), après les retours de Raphaël. **Conception uniquement : les 35 nouveautés ne sont pas implémentées.** Les 22 objets existants et leurs corrections sont décrits dans le plan principal, qui fait autorité sur leurs nouveaux effets proposés.

## 1. Inspiration et identité

La [liste Fandom fournie par Raphaël](https://megabonk.fandom.com/wiki/Items) a été consultée via son contenu indexé : l’ouverture directe était bloquée. Le [guide de mécanique d’IndieSnek](https://www.megabonkinfo.org/) a aussi été consulté le 12 octobre ; il indique une base de tests en jeu et de données extraites. Les versions/nombres divergent entre sites : ils ne servent pas de référence chiffrée pour Vestiges.

Ce qu’on retient de Megabonk : objets de stats immédiatement compréhensibles, effets déclenchés par les actions du joueur, cumuls explicites, compromis et changements de règles aux rangs élevés. Son catalogue illustre aussi la nécessité de distinguer probabilité, puissance et délai interne. On ne reprend ni ses valeurs ni ses objets transformés en simples synonymes.

Pour Vestiges, la fonction du petit objet quotidien explique l’effet : une étiquette aide à retrouver, une cassette rembobine, un carnet donne des adresses, un tampon annule. Les effets d’exploration suivent la route et l’Effacement. Référence locale : `VESTIGES-BIBLE.md` §1 et §3.2, mémoire collective et quotidien humain ; aucune réintroduction du craft, de la construction ou du Foyer en run.

## 2. Règles communes de ces propositions

- `n` : copies portées. Chaque copie renforce le même effet, sans changement de comportement au doublon. Cumul additif sauf formule explicite. Six copies au plus par défaut ; limites différentes indiquées.
- **D** : proposé dans le tirage d’un profil neuf, sans être équipé au départ. **Q** : derrière une nouvelle quête, à définir avant activation. Les quêtes et noms proposés ne sont pas déjà livrés.
- Un bonus simple de +8 % × n donne +8/+16/+24 %. Appliquer l’écart exact lors de l’ajout/retrait ; pas une multiplication répétée. Les plafonds existants d’armure, de vol de vie et les immunités des boss restent valables.
- Les valeurs sont des premiers réglages. Un effet ne devient pas équilibré par sa seule rareté ; les mesures peuvent conduire à modifier valeur, plafond ou rang.

## 3. Les 35 nouveaux objets

### 3.1 Statistiques globales — 18 objets

| Rareté | Objet proposé (`id`) | Effet par copie | Accès |
|---|---|---|---|
| Commun | Lacet de randonnée (`lacet_de_randonnee`) | **+8 % vitesse de déplacement**. Le lacet qui tient encore quand tout lâche. | D |
| Commun | Pastille de menthe (`pastille_de_menthe`) | **+0,15 PV/s de régénération** ; respecte les 2 s sans régénération après blessure. | D |
| Commun | Bouton de duffle-coat (`bouton_de_duffle_coat`) | **+12 PV max** ; ne soigne pas les PV manquants lors d’un échange. | D |
| Commun | Agrafe de cartable (`agrafe_de_cartable`) | **+1 armure**, selon la formule et le plafond du joueur. | D |
| Commun | Carnet de bons points (`carnet_de_bons_points`) | **+6 % XP gagnée**. | D |
| Commun | Capsule porte-bonheur (`capsule_porte_bonheur`) | **+3 points de Chance** (valeur interne +0,03). Pas de promesse d’améliorer les coffres tant que Chance n’agit pas sur leurs tirages. | Q |
| Commun | Bande magnétique (`bande_magnetique`) | **+15 % rayon d’attraction de l’XP**. Un morceau de cassette qui attire encore les souvenirs épars. | D |
| Inhabituel | Clé de réveil (`cle_de_reveil`) | **+6 % cadence d’attaque**. | D |
| Inhabituel | Compas d’écolier (`compas_decolier`) | **+10 % taille des zones d’attaque** selon la grammaire de chaque arme. | D |
| Inhabituel | Ruban de couturière (`ruban_de_couturiere`) | **+10 % portée des attaques** compatibles. | D |
| Inhabituel | Verre de lorgnette (`verre_de_lorgnette`) | **+3 points de chance critique**. | Q |
| Inhabituel | Ressort de stylo (`ressort_de_stylo`) | Dash qui se **recharge 8 % plus vite** par copie : durée de recharge divisée par `1 + 0,08n`. Ne donne pas de dash gratuit ni d’invulnérabilité. | D |
| Inhabituel | Ficelle cirée (`ficelle_ciree`) | **+12 % durée des statuts**, dans les plafonds anti-contrôle continu des objets. | Q |
| Rare | Dent de peigne (`dent_de_peigne`) | **+1 perforation** aux attaques compatibles. | Q |
| Rare | Tube de prélèvement (`tube_de_prelevement`) | **+0,5 point de vol de vie**, avec le plafond existant de 3 % des PV max/s partagé par toutes ses sources. | Q |
| Rare | Bille de verre (`bille_de_verre`) | **+25 points au multiplicateur critique** (×1,5 devient ×1,75 avec une copie). | Q |
| Épique | Boucle de ceinture (`boucle_de_ceinture`) | **+15 bouclier maximum**, selon la défense existante ; ne pas confondre points de bouclier et nombre de coups annulés. | D |
| Épique | Diapositive en double (`diapositive_en_double`) | **+1 projectile** via `projectile_bonus`, pour les armes compatibles. Un souvenir photographié deux fois. | D |

Ces objets de stats sont volontairement simples. Leur fonction est de compléter les quatre Écussons sans imposer une condition artificielle à chaque petite trouvaille. Ne pas réutiliser les IDs des anciens objets devenus Écussons.

### 3.2 Propriétés propres aux armes — 7 objets

Ce lot exige de nouveaux raccordements : être affichée dans `data/ui/stats.json` ne rend pas une statistique automatiquement applicable par un objet. Montrer les armes concernées sur la fiche ; ne pas laisser croire qu’un bonus agit sur toutes les armes.

| Rareté | Objet proposé (`id`) | Effet par copie | Accès |
|---|---|---|---|
| Commun | Tampon en caoutchouc (`tampon_en_caoutchouc`) | **+15 % recul** des attaques compatibles ; respecte les résistances des boss. | Q |
| Inhabituel | Fléchette en mousse (`flechette_en_mousse`) | **+15 % vitesse des projectiles** ; ne change ni leur nombre ni la cadence. | D |
| Inhabituel | Bille de roulement (`bille_de_roulement`) | **+12 % vitesse des attaques orbitales** ; conserver leur limite de touches par cible. | Q |
| Inhabituel | Fil de téléphone (`fil_de_telephone`) | **+15 % distance de rebond/liaison** des armes compatibles. | Q |
| Inhabituel | Fer à cheval miniature (`fer_a_cheval_miniature`) | **+15 % guidage** d’un projectile déjà guidé. Ne transforme pas un projectile droit en projectile guidé. | Q |
| Rare | Domino ébréché (`domino_ebreche`) | **+1 cible de chaîne/rebond** pour les armes compatibles. | Q |
| Épique | Anneau de trousseau (`anneau_de_trousseau`) | **+1 élément orbital** pour les armes compatibles ; maximum de copies à éprouver au banc dense. | Q |

Les paramètres de zone propres aux armes (`arc_angle`, `cone_angle_end`) passent par le Compas ; confirmer leur raccordement avec les formes touchées, pas seulement le dessin. Les nombres `projectile_count`, `chain_targets` et `orbital_count` restent distincts : pas de double application de la Diapositive et de l’Anneau au même paramètre.

### 3.3 Objets qui changent la façon de jouer — 10 objets

| Rareté | Objet proposé (`id`) | Effet et cumul du même effet | Accès |
|---|---|---|---|
| Commun | Étiquette de bagage (`etiquette_de_bagage`) | Repère de bord d’écran pour les **objets laissés au sol**, dans un rayon de **150 px × n**. Retrouver un objet refusé devient un choix possible avant l’Effacement. Ne révèle pas le contenu d’un coffre fermé. | D |
| Inhabituel | Ticket de vestiaire (`ticket_de_vestiaire`) | Les objets au sol restent **45 s × n plus longtemps** avant leur disparition naturelle. L’Effacement continue à les détruire ; ne ressuscite pas un objet déjà disparu. | Q |
| Rare | Clé sans serrure (`cle_sans_serrure`) | Ouvre les coffres **25 % plus vite par copie** : maintien divisé par `1 + 0,25n`. Aucun coffre gratuit, ils le sont déjà ; aucun effet sur les stèles ou les combats de boss. | Q |
| Rare | Sonnette de comptoir (`sonnette_de_comptoir`) | À la fermeture du butin d’un coffre nouvellement ouvert, attire l’XP déjà présente dans **150 px × n** autour du coffre. Une seule activation par coffre, aucun gain d’XP créé. | D |
| Épique | Carnet d’émargement (`carnet_demargement`) | Toucher une créature avec **deux armes différentes en moins de 4 s** lui inflige un impact additionnel de **25 % × n de la somme des deux coups de référence**. Déclenchement au plus toutes les 2 s, global au joueur ; les dégâts d’objets ne comptent pas comme une troisième arme. | Q |
| Épique | Pelote de laine (`pelote_de_laine`) | Un dash relie les **deux dernières créatures distinctes touchées**, encore vivantes et à moins de 250 px l’une de l’autre. Pendant 3 s, **15 % × n** des dégâts d’arme infligés à l’une sont aussi infligés à l’autre. Quatre copies au plus ; délai de 6 s, un seul lien ; pas de transfert récursif, ni de lien entre deux parties d’un même boss. | Q |
| Épique | Tampon ANNULÉ (`tampon_annule`) | Pendant un dash, efface les **n premiers projectiles ennemis rencontrés**. Ne protège pas d’un corps-à-corps, d’un rayon, d’une zone au sol ou du Néant. Le saut instantané utilise le trajet balayé ; chaque projectile ne disparaît qu’une fois. | D |
| Légendaire | Carnet d’adresses (`carnet_dadresses`) | Chaque récompense d’objet d’un coffre devient **un choix parmi 1+n objets du rang tiré**, au lieu d’un objet imposé. Trois copies au plus ; on reçoit toujours un seul objet par récompense, pas toute la sélection. Pas de nouveau tirage de rareté en ouvrant l’écran. | D |
| Légendaire | Cassette rembobinée (`cassette_rembobinee`) | À la fin d’un dash, rend **25 % × n des PV de combat perdus dans les 3 dernières secondes**. Trois copies au plus, 12 s entre activations. Ne ressuscite pas, ne rend pas les pertes du Néant/auto-infligées et ne restaure ni position, ni Essence, ni inventaire. Les pertes déjà remboursées sont consommées du relevé. | D |
| Légendaire | Plan cadastral (`plan_cadastral`) | Ouvrir un coffre maintient **pendant 5 s × n** un petit îlot de **80 px de rayon** autour de lui lorsque l’Effacement l’atteint. Trois copies au plus, 45 s entre activations, un seul îlot ; ne recrée pas le Néant, ne ralentit pas le front mondial, ne protège pas des créatures. Un délai pour finir son choix et repartir. | Q |

**Pourquoi ces trois légendaires :** le Carnet fait mieux choisir les trouvailles ; la Cassette récompense le dash après une blessure ; le Plan fait temporairement tenir un lieu parce qu’il existe encore sur le papier. Trois façons différentes d’habiter un monde qui s’oublie.

Le Plan cadastral est le plus coûteux et le plus risqué pour le core loop : dernier sous-lot, avec parcours d’Effacement obligatoires. Ne pas changer globalement le front ni rendre les objets protégés permanents. Cette proposition doit pouvoir être retirée sans bloquer les autres nouveautés.

## 4. Couverture des statistiques

Les **19 stats globales existantes** sont toutes couvertes ; aucune nouvelle statistique générique n’est nécessaire pour ce premier groupe.

| Stat réelle | Objet(s) proposé(s) |
|---|---|
| `damage` | Semelle, Tabouret, Thermos, Médaille, Thermomètre, Épingle, Gilet, Porte-monnaie |
| `speed` | Lacet de randonnée |
| `max_hp` | Bouton de duffle-coat |
| `attack_speed` | Clé de réveil |
| `aoe_radius` | Compas d’écolier |
| `attack_range` | Ruban de couturière |
| `regen_rate` | Pastille de menthe |
| `lifesteal` | Tube de prélèvement |
| `armor` | Agrafe de cartable |
| `shield` | Boucle de ceinture |
| `crit_chance` | Verre de lorgnette |
| `crit_multiplier` | Bille de verre |
| `projectile_pierce` | Dent de peigne |
| `xp_magnet_radius` | Bande magnétique |
| `luck` | Capsule porte-bonheur |
| `xp_gain` | Carnet de bons points |
| `dash_recharge` | Ressort de stylo |
| `projectile_bonus` | Diapositive en double |
| `status_duration` | Ficelle cirée |

Propriétés d’armes supplémentaires : `knockback`, `projectile_speed`, `orbital_speed`, `chain_range`, `homing_strength`, `chain_targets`, `orbital_count` via §3.2 ; `projectile_count` via Diapositive ; angles et taille via Compas. `peril` reste représenté par le Sifflet. Évasion, épines et sursoin ne sont pas ajoutés simplement parce qu’ils existent dans Megabonk.

## 5. Lots de production

Lire d’abord I10a–I10c du [plan principal](35-raretes-des-objets.md#6-lots-dimplémentation--un-à-la-fois). Leur modèle de cumul et leurs budgets doivent être livrés avant d’étendre le catalogue. Ces lots sont proposés, aucune case de réalisation n’est cochée.

| Lot | Contenu | Patterns à lire et vérifications |
|---|---|---|
| I11a | 18 objets de statistiques globales, en deux fournées de 9 | `Player.ApplyPerkModifier`, contrat `player_stats`, oubli dans `Player.Objects`. Vérifier tous les gains/retraits, plafonds de défense, absence de soin gratuit, paliers d’Écussons intacts, icônes et quêtes. |
| I11b | 7 objets propres aux armes | Contrat `data/weapons/weapon_contract.json`, `Player.GetWeaponStat`, consommateurs de chaque arme. Ajouter un raccordement explicite si absent ; tests d’armes compatibles/incompatibles, valeurs réelles et rendu identiques, bornes de nombre et banc dense pour les orbitales. |
| I11c | 7 utilitaires/épiques du §3.3 | Interactions `ChestOpened`, inventaire du monde, provenance `AttackContext`/`DamageKind.Passive`, `PlayerMobility.DashesStarted`. Les nouveaux événements/APIs sont à créer, pas à supposer. Tester ouverture une seule fois, expiration, changement de biome, salves, disparition d’une cible, parties de boss, pools et coût par image. |
| I11d1 | Carnet d’adresses | `ChoiceScreen`, `LootRewards`, exemplaires promis, `ItemExchange`. Tirer le rang une seule fois, candidats distincts accessibles ; si moins de candidats disponibles, montrer seulement ceux-là. Réserver uniquement l’objet choisi entre les récompenses d’un même coffre. Sac plein et annulation sans relance ; captures et manette. |
| I11d2 | Cassette rembobinée | Relevé borné des pertes de PV de combat et événements de dash. Pas de soins comptés comme dégâts, pas de résurrection ; éliminer les pertes remboursées et les garder consommées après oubli/rééquipement. Aucun cercle soin→dégâts→soin. Tests perte fractionnaire, bouclier, soin intermédiaire, pause et mort. |
| I11d3 | Plan cadastral | Lire d’abord `ErasureManager` et les tests `erasure_active` ; définir une exception locale temporaire sur terrain encore réel. Un seul îlot, expiration déterministe, aucun front global modifié. Pas de création de nouveau monde, de coffre ni de butin supplémentaire ; captures et banc. |

**Pour chaque fournée :** IDs, données, contrat, effets réellement branchés, icônes, traduction, accès et descriptions ; build sans warning, suites utiles et capture réelle avant la suivante. Pas de fiche active sans implémentation complète.

**Déblocages :** conserver les quêtes des 22 objets existants. Les 35 nouveaux ont ici une proposition D/Q explicite ; les objectifs des Q sont à écrire dans leur lot (une action déjà traçable, sans dépendance à l’objet qu’elle doit débloquer). Le profil neuf doit avoir au moins les trois épiques et deux légendaires désignés au §4 du plan principal. Les hauts rangs sont accessibles, jamais garantis.

**Recette globale :** I12 du plan principal, avec contrôle automatique de cette matrice contre les vraies clés du contrat. Mesurer les objets ignorés, le remplissage du sac et la dilution des synergies ; 57 objets ne justifient pas à eux seuls d’augmenter la capacité du sac.

# Plan 21 — Le système de build, reconçu depuis les principes

29 septembre 2026 · **Version 2 après les arbitrages de Raphaël ([DECISIONS §32](DECISIONS.md)) : la [§16](#16-version-2--arbitrages-du-29-septembre) fait foi là où elle contredit les sections précédentes.** Demande ([DECISIONS §31](DECISIONS.md)) : repartir d'un point de vue neutre, sans présupposé, et construire la manière dont le build se fait en respectant les [douze principes](../PRINCIPES-BUILD.md), quitte à remettre en cause ce qui était prévu ou déjà implémenté. Aucun code modifié par ce document.

Méthode : partir de ce que Vestiges possède déjà en propre (l'Effacement, les zones oubliées, l'Essence, les Mémoriaux, les Failles et le Péril, les Résurgences, l'exploration nomade, les personnages du plan 06) ; poser une structure ; la vérifier principe par principe ; puis seulement comparer avec l'existant.

## 1. Constat sur l'existant, vu par les principes

| Élément actuel | Ce qui fonctionne | Ce qui heurte un principe |
|---|---|---|
| Armes, 4 emplacements, améliorations à rareté (plan 17) | Actions claires, rareté = surprise (P7) | Gains tirés au hasard sur des stats : la valeur d'une amélioration dépend peu du build (P2), peu de changements de régime (P5) |
| Passifs de niveau, 4 × 5 niveaux | Contrainte lisible (P8) | Surtout des « +x % » uniformes : Flamme vaut la même chose pour tous (P2), peu visibles (P9) |
| Fragments B, 4 règles aux niveaux 2/6/12/20 | Règles qualitatives, vrais changements (P5) | Rôle proche de celui des objets (P1) ; acquis au rythme de l'XP, sans lien avec les moments du monde (P9, P10) |
| Objets (propositions 1 et 2), illimités ou 8 | Mécaniques variées | Troisième couche d'interactions sans règle commune (P3) ; illimités, ils ne contraignent rien (P8) |
| Anciens Dons des coffres | — | Circuit parallèle, appliqué d'office, sans choix (P7, P9) |
| Courbe XP visant 300 à 400 niveaux en 45 min (plan 20) | Rythme soutenu | Des centaines de choix imposent des gains minuscules, donc peu perceptibles (P9), ou une longue phase de surplus |

Conclusion : les pièces existent, mais **elles ne parlent pas la même langue**. Il manque une grammaire commune qui relie armes, statistiques, objets et règles (P3). Et il faut une fonction nette par famille (P1).

## 2. La structure : cinq familles, cinq questions

Chaque famille répond à une question, s'acquiert par un canal différent et a une contrainte propre.

| Famille | Question | Emplacements | Canal d'acquisition | Rôle |
|---|---|---|---|---|
| **Armes** | *Quoi* : comment j'attaque | 4 | Niveau (nouvelle arme, niveaux, voies) | Définissent les actions et portent des étiquettes (projectile, mêlée, zone, orbite, continu, chaîne) |
| **Traits** | *Combien* : l'efficacité de mes actions | 4, niveaux 1 à 5 | Niveau | Modifient les **propriétés communes** : fréquence, nombre, taille, portée, durée, précision… |
| **Objets** | *Quand* : ce qui se déclenche | 6, niveaux 1 à 3 par doublon | **Monde** : Reliquaires, Souverains, coffres | Ajoutent des **interactions** sur des déclencheurs communs (impact, élimination, critique, blessure, dash, niveau, zone oubliée) |
| **Fragments** | *Comment* : une règle réécrite | 4 | **Moments** : après chaque Résurgence, sur un Souverain | Changent une règle commune : **changements de régime** |
| **Personnage** | *Pourquoi* : l'orientation | 1 | Choix au Hub | Signature, arme de départ, mobilité, affinités d'offre |

Ce que le joueur apprend dans l'ordre (P1) : une arme, puis ce qu'un trait fait à cette arme, puis qu'un objet se déclenche plus souvent quand l'arme tape plus vite, puis qu'un fragment change la règle. Chaque famille se comprend seule avant ses combinaisons.

Les trois canaux distincts ont un sens :

- **le niveau récompense le combat** ;
- **le monde récompense l'exploration** ;
- **les moments récompensent la survie à la pression**.

C'est la boucle de Vestiges : se battre, avancer, tenir (P10).

## 3. La grammaire commune (P3, P4)

Tout le système repose sur trois listes courtes, partagées par toutes les familles.

**Propriétés d'attaque**, portées par chaque arme selon son motif :

| Propriété | Ce qu'elle change | Armes concernées |
|---|---|---|
| Fréquence | Attaques par seconde | Toutes |
| Nombre | Projectiles, frappes, éléments orbitaux, sauts de chaîne | Selon le motif |
| Taille | Zone, arc, cône, rayon d'onde | Mêlée, zone, cône |
| Portée | Distance d'acquisition et de trajet | Distance, chaîne |
| Durée | Statuts, zones au sol, orbites | Celles qui en ont |
| Force | Dégâts de base | Toutes (niveaux d'arme seulement) |
| Précision | Chance et puissance de critique | Toutes |

**Déclencheurs** : impact, élimination, critique, blessure subie, soin, dash, niveau gagné, entrée en zone oubliée, fin de Résurgence.

**Statuts** : Brûlure, Saignement, Ralentissement, Désorientation, Fragilité. Chacun a une règle unique de cumul et de rafraîchissement, et une provenance (contrat B0 déjà livré).

Deux règles de garde :

- **Coefficient de déclenchement par arme.** Un impact d'arme rapide ou multiple compte moins pour les objets qu'un impact lent et lourd. La fréquence augmente les occasions (P4) sans que les armes à multi-impact écrasent tout (P11).
- **Pas de récursion.** Un effet déclenché ne redéclenche pas les objets (contrat B0).

Chaînes de renforcement typiques (P4) :

- Fréquence → plus d'impacts → plus d'objets « à l'impact » → plus de statuts → objets « contre les brûlés ».
- Taille → plus de cibles par attaque → plus d'éliminations → objets « à l'élimination » → plus d'XP → plus de niveaux.

## 4. Armes : quoi

- **Emplacements :** 4. L'arme de départ vient du personnage.
- **Niveaux 1 à 10.** Chaque carte d'amélioration annonce la propriété qu'elle monte (« Arc : Nombre +1 »). La rareté fixe l'ampleur, comme aujourd'hui (plan 17). Le joueur voit donc ce qu'une amélioration renforce dans son build (P2).
- **Deux voies par arme, au niveau 5.** Au niveau 5, le joueur choisit l'une des deux ; c'est un changement de régime visible (P5, P9).

Exemples de voies :

| Arme | Voie A | Voie B |
|---|---|---|
| Arc du gymnase | **Volée** : les flèches partent en éventail ; le Nombre compte double | **Transpercer** : les flèches traversent tout, mais une seule part |
| Faucille | **Moisson** : l'arc devient un cercle complet, plus lent | **Estoc** : frappe droite deux fois plus longue, qui profite de la Portée |
| Cloche d'école | **Glas** : l'onde ralentit deux fois plus longtemps, sans recul | **Tocsin** : l'onde repousse fort et désoriente |
| Boîte à musique | **Ronde** : les orbes s'éloignent et reviennent | **Berceuse** : les orbes restent proches, et les ennemis touchés s'endorment un instant |

- **Niveau 10 :** effet de maîtrise propre à chaque voie.

Le cas de la Cloche relevé au plan 05 §14 (ralentissement expiré avant le coup fatal) se règle naturellement par la voie Glas, sans règle spéciale.

## 5. Traits : combien

- **Emplacements :** 4. Niveaux 1 à 5, par le level-up.
- Chaque trait monte une propriété commune. **Sa valeur dépend donc des armes** (P2) : Nombre ne vaut rien pour une arme sans projectile ni frappe multiple, Taille vaut beaucoup pour la Cloche et rien pour l'Arc en voie Transpercer.

| Trait | Effet par niveau (valeurs d'essai) | Surtout utile à |
|---|---|---|
| Cadence | +8 % Fréquence | Tous ; nourrit les objets à l'impact |
| Démultiplication | +1 Nombre aux niveaux 1, 3, 5 | Projectiles, orbites, chaînes |
| Envergure | +10 % Taille | Mêlée, zones, cônes |
| Allonge | +8 % Portée | Distance, chaînes |
| Persistance | +15 % Durée | Statuts, zones au sol, orbites |
| Précision | +5 % chance et +10 % dégâts de critique | Objets « au critique » |
| Vigueur | +15 PV max et +0,3 PV/s | Contact, soins, fragments de survie |
| Élan | +5 % vitesse et +6 % recharge du dash | Déplacement, objets « au dash » |
| Mémoire | +8 % XP | Investissement (P6), fort tôt, nul pour la survie immédiate |
| Fortune | +0,05 Chance et +6 % Essence | Rareté des cartes, Mémoriaux |

- **Pas de trait « +dégâts » universel.** Sa valeur ne dépendrait d'aucun choix (P2) ; la Force vient des niveaux d'arme.
- **Mémoire et Fortune sont des investissements.** Ils concurrencent un trait de puissance pour une place limitée (P6, P8) et ne sont indispensables à aucun build (P11).

Les treize passifs actuels deviennent ces traits : Flamme, que P2 ne justifie pas, disparaît ; Mémoire vive devient Cadence, Souffle du Néant Démultiplication, Résonance Envergure, Portée étendue Allonge, Siphon rejoint Mémoire, et ainsi de suite.

## 6. Objets : quand

- **Emplacements :** 6. Un doublon monte l'objet d'un niveau, jusqu'à 3.
- **Acquisition dans le monde seulement :**
  - le **Reliquaire** : trois alcôves ; on en prend une, les autres s'effacent ;
  - les **Souverains** ;
  - une ligne possible des **coffres**.
- Explorer améliore donc le build. Un objet trouvé avec les six emplacements pleins se remplace ou se convertit en Essence.
- **Chaque objet a un déclencheur et un effet qui utilise les propriétés communes.** Il profite de lui-même de la Fréquence, de la Taille, de la Durée ou du Nombre du build (P3, P4). Aucun n'a besoin d'une règle écrite pour une autre pièce.

| Déclencheur | Objet | Effet (niveau 1) | Se renforce par |
|---|---|---|---|
| Impact | Allumette humide | 10 % d'enflammer (Brûlure) | Fréquence, Durée |
| Impact | Glaçon dans un mouchoir | 10 % de ralentir | Fréquence, Durée |
| Impact | Aimant cassé | Chaque 8ᵉ impact attire les ennemis proches vers la cible | Fréquence, Taille |
| Élimination | Pétard mouillé | La victime explose en petite zone | Taille, Nombre de cibles |
| Élimination | Bocal de billes | La victime lâche 2 billes qui ricochent une fois | Nombre, Portée |
| Élimination | Dé à coudre | Rend 0,5 PV | Rythme d'éliminations |
| Critique | Loupe de philatéliste | Un critique rend la cible Fragile 2 s | Précision, Durée |
| Critique | Stylo à quatre couleurs | Un critique relance l'attaque sur une autre cible | Précision, Fréquence |
| Blessure | Écusson de pompier | Onde qui repousse les ennemis proches | Taille |
| Blessure | Médaillon ouvrant | Sauve d'un coup fatal, puis se brise | — (se consomme) |
| Dash | Chewing-gum | Le dash laisse une traînée qui ralentit | Durée, Élan |
| Dash | Semelle usée | Le premier impact après un dash compte double pour les objets | Élan, objets à l'impact |
| Niveau | Boîte de pansements | Soigne 3 % des PV max | Mémoire (plus de niveaux) |
| Zone oubliée | Presse-papier en verre | +15 % dégâts en zone oubliée | Chemin près de l'Effacement |
| Contre un statut | Thermomètre | +20 % dégâts contre les ennemis brûlés | Tout ce qui enflamme |
| Contre un statut | Épingle à nourrice | +20 % dégâts contre les ennemis ralentis | Tout ce qui ralentit |
| Économie | Porte-monnaie usé | +1 % dégâts par tranche de 10 Essence gardées | Choix de ne pas dépenser (P6) |
| Économie | Calendrier arraché | L'Effacement avance plus vite ; +15 % XP et Essence | Pari sur la fuite (P6, P10) |
| Exploration | Carte routière pliée | Révèle Reliquaires et Mémoriaux plus loin | Plus d'objets (P6) |

Les statuts deviennent des **ponts** : l'Allumette et la Lampe à pétrole enflamment toutes les deux, et le Thermomètre récompense les deux. La Lampe garde sa forme propre (flamme au sol) ; la Brûlure est une règle commune, pas une signature volée.

## 7. Fragments : comment

- **Emplacements :** 4. Un fragment se cristallise **à la fin de chaque Résurgence survécue** (≈ 4, 8, 13 et 18 min). Un Souverain en offre parfois un.
- Choix parmi trois.
- **Chaque fragment réécrit une règle commune.** C'est la source principale des changements de régime (P5). On reconnaît sa présence dans la manière de jouer, pas dans un chiffre (P9).

| Fragment | Règle réécrite | Régime |
|---|---|---|
| Convergence *(livré)* | Le ciblage préfère élites et Souverains | Chasse aux menaces |
| Débordement *(livré)* | Le surplus d'un coup fatal passe au coup suivant | Armes lourdes : chaque tir devient rentable |
| Propagation *(livré)* | Les contrôles passent au voisin à la mort | Contrôle en chaîne |
| Prévoyance *(livré)* | Les soins en trop se gardent en réserve | La défense permet le risque |
| Reprise *(livré)* | Tuer vite après une blessure rend des PV | Agression blessée |
| Sillage *(livré)* | Le trajet récent ramasse l'XP | Nomade pur |
| Seconde lecture *(livré)* | Une carte laissée revient | Orientation du hasard (P7) |
| Contrecoup | Le dash déclenche les objets « à l'impact » autour du joueur | Le dash devient une attaque |
| Braise | La Brûlure se transmet au voisin quand elle s'épuise | Feu qui se propage |
| Mémoire vive | Chaque 10ᵉ impact déclenche deux fois les objets | Un effet occasionnel devient fiable |
| Lisière | En zone oubliée, les statuts durent deux fois plus | Vivre près de l'Effacement |
| Poids mort | Les ennemis ralentis encaissent 25 % de plus | Contrôle offensif |

- **Délestage et Habitude** deviennent des fragments du Reliquaire : convertir un objet en XP, ou préférer un niveau d'objet possédé. Leur place dépend d'un premier test.

Le moment d'acquisition relie la règle au cœur du jeu. On survit à la vague, et le monde laisse un fragment de sa mémoire.

## 8. Personnages : pourquoi

Chaque personnage du plan 06 garde sa signature, son arme de départ et sa mobilité. Il reçoit en plus **deux affinités**, des étiquettes favorisées dans les offres :

| Personnage | Affinités | Hypothèse d'ouverture |
|---|---|---|
| Vagabond | Élan, Taille | Traverser et frapper large |
| Traqueur | Précision, Portée | Critiques et marques |
| Forgeuse | Taille, Force | Lent et lourd, ondes |
| Éveillée | Nombre, Durée | Rémanences démultipliées |
| Facteur | Fréquence, Élan | Élan, objets à l'impact |
| Scaphandrière | Durée, zone oubliée | Vivre dans l'Effacement |

Le personnage oriente sans enfermer (P1, P11). Ses affinités pèsent sur les offres, sans les imposer.

## 9. Hasard et orientation (P7)

- **Level-up :** 3 cartes, 4 avec une Chance élevée.
- **Relancer** a un coût fixe par run. **Bannir, c'est oublier** (idée de Raphaël, §30) : aucune limite de nombre, mais chaque bannissement ajoute un tiers de point de Péril. Le monde se durcit et récompense mieux (P10).
- **Mettre de côté** une carte pour la prochaine offre : c'est Seconde lecture, réservée à qui choisit ce fragment.
- **Reliquaire :** trois objets visibles, un pris, les deux autres s'effacent. C'est la voie inattendue du monde, que le joueur choisit quand même.

Résultat recherché : le build semble découvert (offres, Reliquaires, fragments tirés au sort) et construit (voies, emplacements, bannissements).

## 10. Boucle de croissance sous tension (P6, P10)

- **Combat → XP → niveaux → armes et traits → combat.**
- **Exploration → Reliquaires, Essence → objets, Mémoriaux → combat.**
- **Survie aux Résurgences → fragments → changement de régime.**
- **Pression :** Effacement, densité, Résurgences, Péril choisi (Failles, bannissements).

**Courbe de niveaux : recommandation de révision.** Le build ci-dessus contient environ 70 choix de niveau porteurs de sens :

- 3 nouvelles armes et 4 × 9 niveaux d'arme, soit 39 ;
- 4 choix de voie ;
- 4 traits × 5 niveaux, soit 20 ;
- plus quelques cartes spéciales.

Viser 300 à 400 niveaux oblige soit à des gains trop petits pour être perçus (P9), soit à une longue phase de surplus. Recommandation : viser **80 à 110 niveaux** pour une excellente run de 45 min, soit un choix toutes les 25 à 35 s au cœur de la run. Au-delà, les **niveaux de surplus** donnent du concret (soin, Essence, relances, score) plutôt que des pourcents. C'est un changement par rapport aux paliers validés du plan 20, à trancher.

**Investir contre survivre, à chaque offre (P6) :**

- une nouvelle arme (faible au niveau 1) contre un niveau de l'arme principale ;
- Mémoire contre Vigueur ;
- garder de l'Essence (Porte-monnaie) contre la dépenser au Mémorial ;
- le Calendrier arraché ;
- un Reliquaire lointain contre la sécurité.

## 11. Plusieurs chemins (P11)

| Archétype | Pièces clés | Force | Besoin | Moment de faiblesse |
|---|---|---|---|---|
| Nuée | Arc en Volée, Démultiplication, objets à l'impact | Couvre l'écran | Fréquence et Nombre | Élites résistantes avant Précision |
| Brasier | Lampe, Allumette, Thermomètre, Braise | Dégâts qui se propagent | Durée | Début de run, avant que les statuts ne prennent |
| Colosse | Forgeuse, Envergure, Débordement, Pétard | Chaque coup nettoie une zone | Taille et Force | Tireurs à distance |
| Contrôleur | Cloche en Glas, Propagation, Poids mort, Épingle | La foule ne touche jamais | Durée | Faibles dégâts bruts |
| Nomade | Vagabond, Élan, Sillage, Chewing-gum, Contrecoup | Se bat en courant | Élan | Espaces étroits, encerclement |
| Lisière | Scaphandrière, Presse-papier, Lisière, Calendrier | Croissance rapide près du Néant | Survie en zone oubliée | Résurgences loin du front |

**Garde-fous :**

- aucune pièce dans tous les archétypes ;
- Mémoire et Fortune n'apparaissent dans aucun archétype comme indispensables.

À vérifier par mesure sur des runs de bot orientées par archétype, avec `measure_run` étendu.

## 12. Lisibilité et hypothèses (P9, P12)

- **Cartes :**
  - une amélioration montre la propriété touchée et son effet réel sur les armes concernées ;
  - un trait montre ses armes compatibles ;
  - un objet montre ce qui le renforce déjà dans le build.
- **Combat :** chaque déclencheur d'objet et chaque règle de fragment a un retour visuel propre, comme en B2 : repère, liseré, chiffre renforcé, trait.
- **Bilan :**
  - dégâts par source (armes, objets, fragments) ;
  - la **chaîne la plus rentable** de la run (par exemple : Cadence → Allumette → Thermomètre, 38 % des dégâts) ;
  - l'objet ou le fragment le plus sous-exploité.
- **Collection :** pièces verrouillées avec leur quête, qui est souvent la description d'un build à essayer (« 300 éliminations de brûlés »), ou leur prix en Vestiges.
- **Déblocage :** une réserve de départ par famille, puis le reste par quêtes et achats (§29).

## 13. Ce que ça change dans l'existant

| Existant | Devient | Coût |
|---|---|---|
| Armes à 4 emplacements, rareté des améliorations | Gardés ; niveau max 10 au lieu de 50, voies au niveau 5, cartes qui nomment la propriété | Données de voies par arme (24 × 2), UI de carte |
| Passifs 4 × 5 | Traits 4 × 5, réécrits sur les propriétés | Données, migration simple |
| Fragments B (7 actifs) | Gardés comme fragments, acquis après les Résurgences au lieu des niveaux 2/6/12/20 | Changer la source d'offre ; le code des effets reste |
| Objets proposés | Refaits en 6 emplacements, déclencheurs communs | Nouveau domaine (O1–O4) |
| Anciens Dons | Retirés ; leurs effets utiles deviennent objets (enflammer, ricochet, exécution…) | Fermeture du circuit |
| Courbe XP 300–400 niveaux | 80–110 niveaux puis surplus concret | Révision du plan 20 |
| Bannissement 3 par run | Illimité, nourrit le Péril | Petit |

**Lots, dans l'ordre :**

1. **G0** : grammaire commune (propriétés, déclencheurs, statuts, coefficients) dans le code, sans changer le jeu.
2. **G1** : traits.
3. **G2** : voies d'armes (d'abord 6 armes).
4. **G3** : fragments après les Résurgences.
5. **G4** : objets et Reliquaire.
6. **G5** : courbe de niveaux, bilan, déblocages.

Chaque lot se mesure par des runs de bot orientées par archétype.

## 14. Vérification principe par principe

| Principe | Où il est tenu |
|---|---|
| 1. Fonction claire | Cinq familles, cinq questions, trois canaux (§2) |
| 2. Valeur selon le build | Traits sur propriétés, pas de +dégâts universel ; cartes qui nomment ce qu'elles renforcent (§4, §5) |
| 3. Règles communes | Propriétés, déclencheurs, statuts partagés (§3) |
| 4. Renforcement mutuel | Chaînes fréquence → impacts → statuts → bonus, bornées par les coefficients (§3) |
| 5. Changements de régime | Voies d'armes, fragments (§4, §7) |
| 6. Immédiat contre futur | Mémoire, Fortune, Calendrier, nouvelle arme, Essence gardée (§10) |
| 7. Hasard orienté | Trois cartes, relances, bannir = oublier, Reliquaire, mise de côté (§9) |
| 8. Contraintes | 4 / 4 / 6 / 4 emplacements, voies exclusives (§2, §4) |
| 9. Conséquences visibles | Propriétés visibles (Nombre, Taille), retours par déclencheur, moins de niveaux mais plus lourds (§10, §12) |
| 10. Croissance sous tension | Trois boucles et la pression de l'Effacement et du Péril (§10) |
| 11. Plusieurs chemins | Six archétypes, garde-fous sur l'investissement (§11) |
| 12. Hypothèses | Bilan par chaîne, quêtes-builds, Collection (§12) |

## 15. Questions pour Raphaël

1. Cette structure (cinq familles, trois canaux) te convient-elle comme base ?
2. Fragments après chaque Résurgence plutôt qu'aux niveaux 2/6/12/20 ?
3. Voies d'armes au niveau 5 et niveau max 10 : acceptes-tu de revoir le plan 17 dans ce sens ?
4. Courbe de 80 à 110 niveaux au lieu de 300 à 400 : acceptes-tu de revoir le plan 20 ?
5. Pas de trait « +dégâts » universel : d'accord ?
6. Par quel lot commencer ? Recommandation : G0 puis G1, car ils touchent peu de code et rendent tout le reste possible.

## 16. Version 2 — arbitrages du 29 septembre

Raphaël tranche la plupart des questions ([DECISIONS §32](DECISIONS.md)). Cette section remplace les passages contraires des §4, §5, §6, §9, §10 et §11.

### Armes : quoi, inchangé dans le cœur, avec une ascension

- Niveau max **50**. Amélioration par **tirage aléatoire de stats** au level-up, avec rareté : la Chance favorise les rares, et une amélioration peut toucher plusieurs stats. C'est le système actuel du plan 17. Le plan 21 garde sa grammaire : la carte nomme les propriétés touchées (Fréquence, Nombre, Taille…), ce qui répond au principe 2 sans supprimer le hasard.
- **Ascension au niveau max :** deux voies finales au choix. Les voies de la §4 (Volée ou Transpercer, Glas ou Tocsin…) deviennent ces ascensions. C'est le changement de régime (P5), qui récompense d'avoir mené une arme au bout.
- **Objet légendaire proposé :** une amélioration d'arme touche une stat de plus. Inspiré du marteau de Megabonk ; son nom, sa forme et son effet exact restent à rendre propres à Vestiges. Par exemple, l'**Établi de grand-père** : chaque amélioration d'arme gagne +1 stat, et +1 de plus tous les 25 niveaux de l'objet.

### Objets : quand, en 6 emplacements qui montent loin

- **6 emplacements**, niveaux **jusqu'à 50**.
- **Proposition d'acquisition :** un objet **neuf** vient du monde (Reliquaire, Souverain, ligne de coffre) ; ses **niveaux** viennent du level-up, comme ceux des armes, et des doublons trouvés dans le monde.
  - Avec 4 armes et 6 objets à 50 niveaux, le level-up a environ 490 améliorations à offrir : de quoi porter les 300 à 400 niveaux visés sans surplus précoce.
  - Chaque niveau d'objet monte son effet ; les niveaux 10, 25 et 50 sont des paliers marquants (seuil franchi, effet supplémentaire, régime). Exemples : l'Allumette enflamme plus souvent, puis la Brûlure se propage au palier 25 ; le Pétard explose plus grand, puis en deux temps au palier 50.

### Traits : recommandation, les fondre dans les objets

Question de Raphaël : comment obtient-on les traits, et sont-ils nécessaires ?

- **Obtention prévue en version 1 :** par le level-up, comme les passifs actuels. Une carte « nouveau trait » quand un emplacement est libre, puis des cartes de niveau.
- **Sont-ils nécessaires ? Plus maintenant.** Les objets montent désormais par le level-up jusqu'au niveau 50 : ils peuvent porter à la fois les interactions (quand) et les propriétés communes (combien).
  - Un objet comme **Démultiplication** (+1 Nombre à certains paliers) ou **Cadence** remplit le rôle d'un trait. Il occupe un des 6 emplacements, donc il se paie en place (P8).
  - Garder une cinquième famille avec 4 emplacements de plus ajouterait une couche à apprendre sans fonction distincte (P1).

**Recommandation :** pas de famille « traits ». Les objets se répartissent entre objets de **propriété** (Cadence, Démultiplication, Envergure, Allonge, Persistance, Précision, Vigueur, Élan, Mémoire, Fortune) et objets de **déclencheur** (Allumette, Pétard, Dé à coudre…). Les 6 emplacements obligent à doser les deux.

Les passifs actuels migrent vers ces objets de propriété, sauf Flamme (« +dégâts » uniforme, P2). La structure devient **4 armes, 6 objets, 4 fragments, 1 personnage**.

### Fragments : comment, source changée

Un fragment se cristallise **après chaque Résurgence survécue**, au choix parmi trois ; parfois aussi sur un Souverain. Quatre au maximum. C'est validé et prêt à implémenter : les effets livrés ne changent pas.

### Hasard et orientation

- **Relances :** nombre limité par run (3 aujourd'hui).
- **Bannissements :** les 3 premiers sont gratuits. Ensuite chacun coûte du Péril, de plus en plus : +⅓, puis +⅔, puis +1, +1⅓… Bannir reste possible sans limite, mais oublier se paie de plus en plus cher.

### Courbe et surplus

300 à 400 niveaux sont conservés : les gains restent visibles en début de partie, et les cascades de niveaux de milieu et fin de partie (réserve automatique, plan 20) sont voulues. Le §10 du plan 21 est annulé sur ce point.

### Chemins

La §11 n'est qu'un **aperçu de six directions parmi des dizaines ou des centaines**. La grammaire commune est justement faite pour que des combinaisons non prévues fonctionnent sans règle spéciale. Aucune pièce n'est conçue « pour » un archétype.

### Lots révisés

| Lot | Contenu | Statut |
|---|---|---|
| G1 | Fragments offerts après chaque Résurgence (source d'offre), bannissements gratuits puis Péril croissant | **Livré le 29 septembre** (§17) |
| G0 | Grammaire commune dans le code : propriétés nommées sur les cartes d'armes, déclencheurs et statuts partagés | À faire avant les objets |
| G2 | Objets : 6 emplacements, 50 niveaux, neuf par le monde (Reliquaire), niveaux par le level-up ; migration des passifs | Après validation du catalogue et de la question des traits |
| G3 | Ascensions d'armes au niveau max (deux voies) | Après G0 |
| G4 | Personnages : affinités d'offre | Avec le plan 06 |

## 17. Compte rendu G1 — fragments après les Résurgences, bannir coûte du Péril

- **Source des fragments :** chaque Résurgence survécue (`EventBus.CrisisEnded`) ouvre un droit, dans la limite des emplacements libres. Les paliers de niveau 2/6/12/20 sont retirés des données et du code.
- **Moment de l'offre :** si aucun choix n'est ouvert, l'offre de trois fragments s'ouvre aussitôt. Sinon elle passe juste après le choix en cours, avant les niveaux en file.
- **Droit reporté :** un fragment passé, ou sans candidat, attend le niveau gagné suivant (servi avant ce niveau) ou la Résurgence suivante.
- **Contenu de l'offre :** première offre composée, relance, bannissement protégé et éligibilité inchangés.
- **Bannissements :** 3 gratuits par run, puis le n-ième payant coûte n × ⅓ de Péril. La dette est cumulée et réglée par points entiers. Valeurs dans `data/scaling/peril.json` (`banish.free`, `banish.peril_step`).
  - Coût des bannissements 4 à 6 : 0,33, puis 0,67, puis 1. Le Péril passe à 1 puis à 2.
  - Le bouton affiche le prochain coût : « Bannir (+0,3 Péril) ».
- **Libellés :** « FRAGMENT · EMPLACEMENT n/4 », familles « FRAGMENT · COMBAT »… ; la section de la pause s'appelle « Fragments ». Le titre ordinaire du level-up reste « Fragment de mémoire » : conflit de nom à trancher (plan 05 objets §7).

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_perk_acquisition.sh`, réécrit autour des Résurgences : 22 assertions, zéro échec.
  - Niveaux seuls sans fragment ; offre à la Résurgence.
  - Place dans la file ; report au niveau suivant ; droit sans candidat gardé puis servi.
  - Première offre ; offres courtes ; coût croissant en Péril ; limite du catalogue ; éligibilité ; quatre emplacements.
- Effets (66), contrats, armes, capacités ennemies : zéro échec. Smoke vert.
- Capture `--perk-scene resurgence` : offre après la Résurgence, puis deux cartes et « Bannir (+0,3 Péril) » après trois bannissements gratuits.

Relecture par sous-agent après le commit : aucun bug bloquant. Corrections apportées ensuite :
- **Coût exact** : il est tenu en fractions entières (`banish.peril_divisor`, 3), donc sans dérive d'arrondi. Il est affiché en fractions (« +⅓ », « +⅔ », « +1⅓ Péril »), et l'aide précise que les fractions s'additionnent.
- **Dette conservée** : elle n'est plus effacée sans paiement si le gestionnaire de Péril est absent. Au Péril maximal, plus rien ne se paie.
- **Données** : le chargement du bloc `banish` signale une erreur s'il manque.
- **Écran** : il se referme après une relance ou un bannissement si l'offre ne peut pas être renouvelée.
- **Report** : un fragment passé revient aussi juste après un niveau déjà en file.

Non fait : l'offre ponctuelle d'un fragment par un Souverain (§7), qui reste à régler (fréquence).

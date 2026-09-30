# Plan 21 — Historique de la conception (29 et 30 septembre 2026)

> **Document d'archive.** La référence unique du système de jeu est [21-systeme-de-jeu.md](21-systeme-de-jeu.md). Ce fichier garde le raisonnement, les versions successives et le compte rendu du lot G1 ; en cas de contradiction, la référence fait foi.

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
| G2 | Objets : 6 emplacements, 50 niveaux, neuf par le monde (Reliquaire), niveaux par le level-up ; migration des passifs ; retrait des anciens Dons et de leurs six synergies inopérantes | Traits supprimés et 6 × 50 validés ([DECISIONS §33](DECISIONS.md)) ; reste le catalogue à réécrire |
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

## 18. Megabonk comme référence : ce qu'on prend, ce qu'on ne clone pas — 30 septembre

Raphaël valide la suppression des traits et les 6 objets jusqu'au niveau 50, demande un autre nom pour les ex-perks, et pose sa vraie difficulté ([DECISIONS §33](DECISIONS.md)) : Megabonk lui paraît parfait, il veut s'en approcher sans le cloner, et il constate que Vestiges manque de choses à trouver sur la carte.

### Les deux structures côte à côte

| | Megabonk | Vestiges (plan 21 version 2) |
|---|---|---|
| Armes | 4, 40 niveaux, améliorations à rareté sur stats aléatoires | 4, 50 niveaux, même principe (déjà en jeu, plan 17), plus une ascension à deux voies au niveau max |
| Statistiques | 4 tomes montés par le level-up | Portées par les objets |
| Objets | Illimités, trouvés sur la carte, empilés toute la partie | 6 emplacements, 50 niveaux ; neufs par le monde, niveaux par le level-up et le monde |
| Règles | — | 4 (ex-perks), une par Résurgence survécue |
| Carte | Fixe ; coffres payants, statues à choix gratuites, marchands, huit sanctuaires, pots, micro-ondes | Qui s'efface ; coffres, 5 Mémoriaux, 3 Failles, 5 micro-événements ; POI désactivés (`pois_enabled: false`) |

Sources : [objets](https://commonsensegamer.com/megabonk-items/), [sanctuaires](https://deltiasgaming.com/?p=355916), [conseils et lieux de la carte](https://www.gfinityesports.com/article/megabonk-beginners-guide-7-essential-tips-to-survive-the-hordes-in-style).

### Ce qui fait la qualité de Megabonk, lu par les douze principes

- **Trois horloges de récompense :** le niveau (souvent, petit), l'objet trouvé (parfois, surprenant), le sanctuaire ou le boss (rare, fort). Il se passe toujours quelque chose (P10).
- **La carte est une liste de courses :** de nombreux lieux de types différents, visibles de loin, chacun avec une règle simple. Ratisser est la manière normale de jouer (P6, P10).
- **Les objets illimités gardent chaque coffre désirable jusqu'à la fin** (P7).
- **Les tomes limités donnent l'identité du build** (P8).

Rien de cela n'est une invention de Megabonk. Le squelette (armes à emplacements, statistiques au level-up, objets trouvés) est une convention du genre : Vampire Survivors, Risk of Rain, Brotato. Le reprendre n'est pas cloner. Ce qui ferait de Vestiges un clone, ce serait de reprendre en plus ses objets, ses lieux et sa boucle de carte fixe.

### Où Vestiges est déjà autre chose

1. **Le monde s'efface.** Megabonk se joue sur une carte fixe qu'on nettoie. Dans Vestiges, ce qu'on n'a pas visité disparaît. Explorer, c'est choisir ce qu'on sauve et ce qu'on laisse oublier.
2. **Choisir, c'est oublier.** Reliquaire (une alcôve prise, deux effacées), bannissement payé en Péril, Failles.
3. **Les Résurgences** rythment la run et donnent les règles.
4. **Les personnages** orientent le build (plan 06).

Conclusion de l'agent : le risque n'est pas dans la structure du build. Il est dans la carte. Tant que Vestiges n'a pas ses propres lieux, liés à l'Effacement, le joueur ne sentira pas la différence.

### Tension à surveiller : 6 objets et l'envie de ratisser

Dans Megabonk, l'objet illimité fait de chaque coffre une bonne nouvelle jusqu'à la fin. Avec 6 emplacements, le monde n'a plus d'objet neuf à offrir une fois les 6 pris. Pour que la carte reste désirable, trois garde-fous :

- le monde donne surtout des **niveaux** d'objets et d'armes (300 niveaux d'objets à gagner sur une run) ;
- un Reliquaire peut toujours proposer de **remplacer** un objet, le nouveau héritant d'une partie des niveaux ;
- les petites récompenses sont variées : Essence, soin, XP, relances, bénédiction courte.

Si l'essai montre que la carte cesse d'attirer après les 6 objets, le repli est connu : des objets trouvés sans limite d'emplacements, à côté des objets de niveau. Ce serait un choix de conception assumé, pas un échec.

### Nom des ex-perks

« Fragment de mémoire » désigne déjà chaque choix de niveau. Propositions pour les quatre règles gagnées après les Résurgences :

| Nom | Pourquoi |
|---|---|
| **Réminiscences** (recommandé) | Ce qui revient après une Résurgence : le monde pulse, un souvenir de règle reste. Le couple Résurgence et Réminiscence se retient |
| Certitudes | Dans un monde qui s'oublie, quatre choses restent vraies pour toi |
| Ancres | Reprend l'état « ancré » des zones : ce qui tient quand tout s'efface |

Écartés, parce que déjà pris dans le jeu : Échos (Gants de boxe, plan 16), Empreintes (Traqueur), Rémanences (Éveillée), Éclats (Mémoriaux), Souvenirs (lore et méta).

### Chantier qui manque : la carte à ratisser

À ouvrir comme plan à part. Principe proposé : beaucoup de lieux petits et lisibles de loin, chacun avec une règle simple et une récompense immédiate, **plus riches près du front d'Effacement et perdus quand leur zone s'efface**. C'est la version Vestiges de la liste de courses : on ne peut pas tout prendre, et la carte elle-même choisit ce qu'on perd.

## 19. Compte rendu G2a — les passifs deviennent des objets (30 septembre)

Première application de la [référence unique](21-systeme-de-jeu.md) §4.

- **Emplacements et niveaux :** 6 emplacements au lieu de 4 ; niveau 1 à 50 au lieu de 5.
- **Effet par formule :** au niveau n, un effet vaut `1 + per_level × n` (multiplicatif) ou `per_level × n` (additif). Plus de table de cinq valeurs ni de gain multiplié par la rareté : l'effet d'un objet ne dépend que de son niveau.
- **Effets multiples :** un objet peut porter plusieurs effets (Lunettes de lecture : chance et dégâts de critique ; Lacet rouge : vitesse et recharge du dash).
- **Rareté :** une amélioration d'objet donne 1, 2, 3, 4 ou 5 niveaux de la commune à la légendaire (`object_levels` dans `upgrade_rarities.json`).
- **Catalogue :** 12 objets de propriété proposés, avec leurs nouveaux noms. Les identifiants des anciens passifs sont conservés.
- **Retirés des offres :** Flamme intérieure (+dégâts universel), Reflet brisé (perforation), et Papier carbone en attendant ses copies à dégâts réduits.
- **Nouvelles stats :** XP gagnée (appliquée une fois au gain) et recharge du dash.
- **Interface :** cartes « NOUVEL OBJET », niveau « 1 → 6 » et effet avant → après ; pause « Objets » avec tous les effets ; HUD à 6 cases. Les pourcentages gardent une décimale quand elle compte (« +1,2 % »). Les ex-perks s'affichent « Réminiscence ».

**Non fait, annoncé :** les paliers du niveau 25, Papier carbone, Pince à linge (lot G2a-2) ; les objets de déclencheur (G2b). Les classes internes gardent leur nom (`PassiveSouvenir…`, fichier `passive_souvenirs.json`) : renommage technique à prévoir, sans effet en jeu.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` (nouveau) : 15 assertions, zéro échec (emplacements, formule jusqu'au niveau 50, effets multiples, XP, dash, Chance, niveaux par rareté, offres).
- Effets des Réminiscences (66), acquisition (22), contrats, armes, capacités ennemies, déplacements : zéro échec. Smoke vert.
- Captures `--capture-levelup` et `--capture-pause`, regardées : carte légendaire « Ressort de sommier, Niv 1 → 6, Cadence +1,2 % → +7,2 % », section « Objets » de la pause, six cases au HUD.

## 20. Lot G2a-2 — paliers du niveau 25, Papier carbone, Pince à linge : découpage (30 septembre)

Application de la [référence](21-systeme-de-jeu.md) §4. Deux étapes, un commit chacune.

### Étape 1 : le socle des paliers et les deux objets manquants

- **Données :** un objet porte une liste `milestones` (niveau, effet, texte, paramètres). Un effet d'objet vaut `base + per_level × n` au niveau n ; `base` est facultative (Papier carbone : 30 % + 1,4 % × n).
- **Activation :** un palier s'active quand une amélioration franchit son niveau, une seule fois. Un composant `ObjectMilestones`, enfant du joueur comme celui des Réminiscences, porte l'état et les paramètres des paliers atteints.
- **Annoncé seulement si codé :** un registre des effets branchés (sur le modèle de `PerkSpecializationEffects`). Un palier absent du registre n'apparaît ni sur les cartes ni dans la pause.
- **Lisibilité :**
  - carte d'objet neuf ou d'amélioration : « Palier 25 : … » tant qu'il n'est pas atteint ;
  - amélioration qui le franchit : « Palier 25 atteint : … » en couleur de gain ;
  - pause : le palier sous chaque objet, grisé tant qu'il n'est pas atteint.
- **Papier carbone** (`souffle_du_neant`, réactivé) :
  - stats `attack_copies` (+1) et `copy_damage` (30 % + 1,4 % × n) ;
  - les copies s'ajoutent aux tirs, aux salves et aux frappes de mêlée, à dégâts réduits ; les projectiles copiés sont teintés ;
  - paliers 25 et 50 : +1 copie chacun.
- **Pince à linge** (`persistance`, nouveau) :
  - stat `status_duration` (+1,5 % × n), appliquée là où une durée est posée : effets à l'impact des armes (Saignement, Ralentissement, Désorientation), embrasement, feu au sol, champ de l'Aiguille ;
  - les orbites sont permanentes aujourd'hui : la Durée ne s'y applique pas encore ; la carte ne les annonce pas ;
  - palier 25 : un statut infligé par le joueur qui expire a 25 % de chance de se renouveler. L'ennemi publie l'expiration sur l'EventBus ; le composant des paliers décide.
- **Code :** les méthodes d'objets quittent `Player.cs` pour une partie `Player.Objects.cs` ; `ActivePassiveSouvenir` prend son propre fichier.
- **Vérification :** `tools/test_objects.sh` étendu (formule avec base, activation au franchissement, registre, copies, Durée, renouvellement) ; captures `--capture-levelup` et `--capture-pause`.

### Étape 2 : les douze autres paliers

Valeurs de départ, en données (`params` du palier) :

| Objet | Palier 25 | Branchement |
|---|---|---|
| Ressort de sommier | Chaque 10ᵉ attaque d'une arme repart 0,12 s après | Minuteur d'attaque de l'arme ; tirs, frappes et chaînes (ni cône continu ni orbite) |
| Rondelle de cuivre | Les zones refrappent 0,25 s après, à 30 % | Frappes de mêlée (arcs, cercles), échos et éclats d'armes spéciales. Un effet déclenché ne refrappe pas (§7, pas de récursion) |
| Mètre pliant | Un projectile en bout de course éclate : 50 % de ses dégâts dans 36 px × Taille | Fin de course d'un projectile d'arme, pas d'un ricochet |
| Lunettes de lecture | Critique sur une cible à PV pleins : dégâts × 2 | Au moment de l'impact (projectile, mêlée, chaîne) |
| Bouton de manteau | Coup inférieur à 3 % des PV max ignoré | Coup reçu, avant l'armure ; éclair pâle, limité en fréquence |
| Bobine de fil | Régénération × 2 pendant 3 s après une blessure | Blessure publiée sur l'EventBus |
| Genouillère | Armure × 2 pendant le dash et 1 s après | Temps depuis le dernier dash, tenu par la mobilité |
| Écusson de pompier | Bouclier cassé : onde qui repousse de 90 px dans 110 px | Coup qui casse le bouclier |
| Lacet rouge | Dash 30 % plus long | Vitesse du dash, même durée |
| Aimant de frigo | Chaque orbe ramassée rend 0,2 PV | Ramassage d'orbe (soin ordinaire : Prévoyance le stocke) |
| Photo de classe | Chaque niveau gagné donne 3 Essence | Niveau gagné |
| Jeton de fête foraine | +1 relance à chaque niveau de joueur multiple de 15 | Gestionnaire des choix de niveau, avant l'ouverture de l'écran |

- **Retour visuel :** repli visible de l'attaque, arc rejoué, éclat au sol, étincelle au renouvellement, anneau de l'onde, vol d'Essence. Bobine et Genouillère n'ont pas d'effet visible propre à ce lot.
- **Vérification :** `tools/test_objects.sh` (un contrôle par palier) ; capture en run avec des objets au-delà du niveau 25.

**Hors lot :** renommage technique (`PassiveSouvenir…`) ; dégâts par source dans le bilan ; Durée des orbites (quand une orbite aura une durée, G3).

## 21. Compte rendu G2a-2, étape 1 : socle des paliers, Papier carbone, Pince à linge (30 septembre)

- **Paliers :** chaque objet porte ses paliers dans `passive_souvenirs.json` (niveau, effet, texte, paramètres). Les douze paliers des objets déjà en jeu y sont écrits, mais ne sont ni annoncés ni activés tant que leur effet n'est pas codé (`ObjectMilestoneEffects`).
  - Un palier s'active une seule fois, quand une amélioration franchit son niveau ; une amélioration peut en franchir deux.
  - Retour : un texte doré au-dessus du joueur (« Papier carbone : palier 25 »).
  - Le composant `ObjectMilestones`, enfant du joueur, naît au premier palier atteint et n'écoute que les événements des paliers actifs.
- **Cartes :** « Palier 25 : … » grisé tant qu'il est à venir, « Palier 25 atteint : … » doré quand l'amélioration le franchit. **Pause :** les paliers sous chaque objet, dorés une fois atteints.
- **Papier carbone** (proposé à nouveau) :
  - effets `attack_copies` et `copy_damage` (30 % + 1,4 % × n) ;
  - les copies sont des crans de la formule (`step_levels` 25 et 50), donc la carte montre « Copies +1 → +2 » ;
  - tirs : les copies visent les cibles suivantes ; salves et frappes de mêlée : les coups pleins tiennent le centre de l'éventail, les copies les deux bords ;
  - projectiles copiés teintés de bleu et plus pâles.
- **Pince à linge** (`persistance`, nouveau) :
  - Durée +1,5 % × n sur Saignement, Ralentissement, Désorientation des armes, embrasement, feu au sol et champ de l'Aiguille ;
  - palier 25 : à l'expiration d'un statut infligé par le joueur (événement `EnemyStatusExpired`), 25 % de chance de le reposer avec la même intensité et la même durée, avec une étincelle de la couleur du statut ;
  - un contrôle reçu par Propagation reste non transmissible une fois renouvelé.
- **Code :** les méthodes d'objets sont passées dans `Player.Objects.cs` et `ActivePassiveSouvenir` dans son fichier (`Player.cs` : 2 381 → 2 306 lignes). Format d'affichage `share` pour une part sans signe (« 31,4 % »).
- **Capture :** `--capture-weapons` accepte `--objects id:niveau,…`.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` : 21 assertions, zéro échec. Parmi elles :
  - formule avec base et crans ;
  - paliers franchis une fois, deux d'un coup ;
  - cartes : palier à venir, atteint, non codé absent ;
  - part des copies en mêlée et en tir ;
  - Cloche : 2,30 s de ralentissement au niveau 10 ;
  - 23 % de renouvellements sur 400 expirations, rien pour un statut sans joueur, contrôle propagé non retransmis.
- Effets des Réminiscences, acquisition, contrats, armes, capacités ennemies, déplacements : zéro échec.
- Captures regardées :
  - `--capture-levelup` : Papier carbone du niveau 22, « Palier 25 : +1 copie » aux raretés commune et inhabituelle, « Palier 25 atteint » et « Copies +1 → +2 » dès la rare ; carte « Nouvel objet » de la Pince à linge avec son palier ;
  - `--capture-pause` : paliers dorés et grisés sous les objets ; niveaux 30 et 12 lisibles dans les cases du HUD, le second touche à peine le bord de sa case ;
  - `--capture-weapons --objects souffle_du_neant:50` : l'arc touche quatre cibles (un tir et trois copies).

Relecture par sous-agent avant le commit. Corrigé : origine d'un contrôle conservée au renouvellement ; copies réparties sur les deux bords de l'éventail ; nombre de frappes jamais nul ; texte du palier traduit. Deux textes de palier se superposaient au-dessus du joueur ; ils s'empilent désormais.

## 22. Compte rendu G2a-2, étape 2 : les douze paliers (30 septembre)

Les 14 objets de propriété ont maintenant leur palier en jeu, avec les valeurs de départ du §20.

| Objet | Palier 25 en jeu | Retour visible |
|---|---|---|
| Ressort de sommier | La 10ᵉ attaque d'une arme repart 0,12 s après. Compte par arme, gardé quand l'arme monte de niveau ; une attaque répétée ne se compte pas | Deuxième attaque |
| Rondelle de cuivre | Chaque frappe de mêlée refrappe 0,25 s après dans sa direction, à 30 % de sa part (copies comprises) ; échos des Gantelets et éclats du Dessin aussi. Un écho suit une frappe qui a touché ; il ne déclenche aucun effet à l'impact | Arc rejoué, anneau au sol |
| Mètre pliant | Un projectile d'arme en bout de course éclate : 50 % de ses dégâts dans 36 px × Taille. Ni ricochet ni effet à l'impact | Éclat au sol, six au plus par frame |
| Lunettes de lecture | Critique sur une cible à PV pleins : dégâts × 2 (projectile, mêlée, chaîne) | Chiffre critique |
| Bouton de manteau | Coup sous 3 % des PV max ignoré | Éclair gris, quatre fois par seconde au plus |
| Bobine de fil | Régénération × 2 pendant 3 s après une blessure | Aucun propre |
| Genouillère | Armure × 2 pendant le dash et 1 s après | Aucun propre |
| Écusson de pompier | Bouclier cassé : recul de 90 px dans 110 px | Double anneau cuivré |
| Lacet rouge | Dash 30 % plus long (vitesse, même durée) | Le dash lui-même |
| Aimant de frigo | 0,2 PV par orbe ramassée (soin ordinaire : Prévoyance stocke l'excédent) | Barre de PV |
| Photo de classe | 3 Essence par niveau gagné | Vol d'Essence |
| Jeton de fête foraine | +1 relance à chaque niveau multiple de 15, avant l'ouverture de l'écran | Compteur de relances |

- **Code :** l'état des paliers vit dans `ObjectMilestones` (horloge par frame seulement pendant une attente) et `ZoneEchoes`. Les branchements dans le joueur tiennent en une ligne chacun ; `Player.cs` passe de 2 306 à 2 324 lignes.
- **Dash :** `PlayerMobility` tient la longueur du dash et le temps écoulé depuis le dernier.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` : 36 assertions, zéro échec, dont une par palier et le compte du Ressort gardé après une montée de niveau de l'arme.
- Effets des Réminiscences, acquisition, contrats, armes, capacités ennemies, déplacements : zéro échec. Smoke vert.
- Capture `--capture-weapons --weapons chipped_blade+chipped_blade --objects resonance:25,souffle_du_neant:50`, regardée : l'arc du Couteau ébréché est rejoué 0,25 s après, et chaque cible reprend 30 % de son coup (48 → 62, 36 → 46).
- L'éclat du Mètre pliant n'apparaît pas dans le cadre serré de la galerie, où tous les tirs touchent : il n'est vérifié que par le banc.
- Le banc de combat dense n'a pas été relancé : sans objet au palier, rien ne change par frame ; l'expiration d'un statut coûte un test de propriétaire.

Relecture par sous-agent. Corrigé : plus d'écho ni d'attaque répétée après la mort ; compte du Ressort gardé quand une arme monte de niveau (le signal d'inventaire part aussi alors) ; éclats dessinés limités à six par frame ; plus d'identifiant d'objet écrit en dur.

**Reste à régler en jeu :** toutes les valeurs de palier sont des valeurs de départ. Bobine et Genouillère n'ont pas de retour visuel propre.

## 23. Lot G2b — objets de déclencheur : découpage (30 septembre)

Application de la [référence](21-systeme-de-jeu.md) §4 (objets de déclencheur) et §7 (Fragilité, coefficient, pas de récursion). Trois étapes, un commit chacune.

### Étape 1 : retrait des anciens Dons

- **Coffres et fouilles :** l'entrée « perk » des tables de butin devient `object_level`, soit des niveaux pour un objet possédé tiré au hasard (1 dans un coffre commun, 1 à 2 dans un rare, 2 à 3 dans un épique). Sans objet à monter, elle donne de l'Essence. La référence prévoit que les coffres montent les objets (§4).
- **PerkManager :** il ne garde que les signatures de départ des personnages (`is_passive`), en attendant G4. Sortent : le tirage de butin, les offres, les six synergies (code, données, notification du level-up), les Dons dans `perks.json`.
- **Joueur :** sortent les effets qui n'existaient que par les Dons : vol de vie, berserker, épines, exécution, esquive, second souffle, embrasement, ricochet, cadence par élimination, projectiles pleins en plus. Leurs lignes quittent la fiche de la pause. La Brûlure revient à l'étape 2 par l'Allumette humide ; la survie au coup fatal reviendra avec le Médaillon ouvrant.
- **Historique de run et analytics :** ils retiennent les Réminiscences acquises à la place des Dons. Le signal `PerkChosen` disparaît.

### Étape 2 : socle des déclencheurs et quatre objets d'impact

- **Statut Fragilité** sur les créatures : dégâts subis augmentés, même règle de cumul que les autres statuts (§7).
- **Coefficient de déclenchement** par arme, en données (`trigger_coefficient` dans `weapons.json`) : les chances d'impact et d'élimination sont multipliées par lui. Le cône continu déclenche à la cadence de ses impacts visibles.
- **Pas de récursion :** seuls les coups directs d'arme déclenchent les objets.
- **Objets :** Allumette humide (Brûlure), Glaçon dans un mouchoir (ralentissement), Thermomètre (dégâts contre une cible brûlée), Épingle à nourrice (dégâts contre une cible ralentie), avec leurs paliers 25.

### Étape 3 : élimination, mouvement, niveau

- **Objets :** Pétard mouillé (explosion de la victime), Dé à coudre (soin à l'élimination), Semelle usée (déplacement continu), Boîte de pansements (soin au niveau gagné), avec leurs paliers 25.
- Les onze autres objets de déclencheur suivent dans un lot G2c (Loupe, Stylo, Tabouret, Chewing-gum, Gilet, Thermos, Médaille, Porte-monnaie, puis les trois objets « monde » avec le Reliquaire).

**Vérification à chaque étape :** bancs d'objets, d'effets, de contrats, d'armes ; smoke ; capture des cartes et d'un combat. Étape 1 : `--loot-draws` pour la répartition du butin des coffres.

## 24. Compte rendu G2b, étape 1 : retrait des anciens Dons (30 septembre)

- **Coffres et fouilles :** l'entrée « perk » des huit tables de butin devient `object_level`. Elle donne des niveaux à un objet possédé tiré au hasard : 1 dans un coffre commun, 1 à 2 dans un rare, 2 à 3 dans un épique.
  - La carte du butin dit ce qui arrive vraiment : un objet proche du niveau 50 ne reçoit que les niveaux qui lui manquent, et deux tirages d'un même coffre ne dépassent pas son maximum.
  - Sans objet à monter, l'entrée donne de l'Essence (`fallback_essence` dans la table : 6, 10 ou 15).
- **Retirés :**
  - le tirage de Dons, les offres et les six synergies (code, données, notification au level-up) ;
  - `perks.json` ne garde que les trois signatures de départ des personnages ;
  - dans le joueur : vol de vie, berserker, épines, exécution, esquive, second souffle, embrasement, ricochet, cadence par élimination, projectiles pleins en plus, et leurs lignes dans la pause ;
  - `Enemy.Execute`, `DamageKind.Execution`, `HealingKind.Revival`, le drapeau de ricochet des projectiles, les signaux `PerkChosen` et `SynergyActivated`.
- **Pause :** la fiche montre la Durée et, s'il y en a, les copies d'attaque avec leur part des dégâts.
- **Historique de run et analytics :** ils retiennent les Réminiscences acquises. Les champs gardent leur nom (`PerkIds`) pour lire les sauvegardes existantes.
- **Code :** `Player.cs` passe de 2 324 à 2 064 lignes.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `--loot-draws 1000`, avec un objet à monter et un objet au niveau 50 : aucun Don, aucun niveau pour l'objet au maximum, zéro échec. Niveaux d'objet tirés : commun 407, rare 399, épique 715 pour 1 000 coffres.
- Objets, effets et contrats des Réminiscences, acquisition, armes : zéro échec. Smoke vert.
  - Le contrat « coup fatal » ne teste plus le second souffle, retiré avec les Dons. La survie au coup fatal reviendra avec le Médaillon ouvrant.

Relecture par sous-agent. Corrigé : Essence de repli passée en données ; niveaux annoncés bornés au maximum, y compris quand deux tirages visent le même objet ; drapeau de ricochet et alias devenus inutiles supprimés.

## 25. Compte rendu G2b, étape 2 : socle des déclencheurs et quatre objets d'impact (30 septembre)

- **Socle :** un composant `ObjectTriggers`, enfant du joueur, porte les objets de déclencheur. Leurs effets de niveau passent par lui, leurs réglages sont en données (`params`).
  - Seul un coup direct d'arme déclenche. Le cône continu déclenche au rythme de ses impacts visibles.
  - La chance est multipliée par le coefficient de l'arme (`trigger_coefficient`), et combinée sur les frappes qui touchent ensemble. Au-delà de 100 %, l'excédent renforce l'effet.
- **Coefficient par arme :** de 0,3 (Dernière émission) et 0,35 (Boîte à musique) à 1,4 (Aiguille de l'Horloge). Valeurs de départ tirées de la cadence, du nombre de coups et de la zone ; réglage en jeu.
- **Fragilité :** dégâts subis augmentés, même règle de cumul que les autres statuts. La Pince à linge la renouvelle. Aucun objet ne la pose encore : Loupe et Chewing-gum viendront en G2c.
- **Règle de cumul (§7) :** Brûlure et Saignement gardent désormais la plus forte intensité et la plus longue durée restante.
- **Objets et paliers :**

| Objet | Niveau n | Palier 25 |
|---|---|---|
| Allumette humide | 6 % + 0,4 % × n d'enflammer : 25 % du coup de base de l'arme par seconde, 3 s × Durée | La Brûlure d'un ennemi tué passe à son plus proche voisin (120 px) |
| Glaçon dans un mouchoir | 6 % + 0,4 % × n de ralentir de 40 % pendant 1,5 s × Durée. Le ralentissement compte comme celui de l'arme (Propagation, Pince à linge) | Un ennemi déjà ralenti est figé 0,5 s, sans toucher à son ralentissement |
| Thermomètre | +10 % + 0,8 % × n contre une cible brûlée | Tes Brûlures ralentissent aussi de 15 %. Texte précisé, la référence disait « les ennemis brûlés » |
| Épingle à nourrice | +10 % + 0,8 % × n contre une cible ralentie ou figée | Un ennemi ralenti tué prolonge de 1 s le ralentissement de ses voisins (120 px), 4 s restantes au plus |

- **Retour visuel :** étincelles à l'allumage, au ralentissement et à la transmission. Une créature qui brûle lâche deux braises toutes les 0,3 s : sur un sprite, la teinte du polygone de repli ne se voyait pas.
- **Dégâts contre une cible :** orbites, cône et maillons de chaîne passent aussi par le calcul à l'impact (Lunettes, Thermomètre, Épingle).

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` : 47 assertions, zéro échec, dont :
  - Allumette au niveau 50 : 26 % avec le Marteau (coefficient 1), 12 % avec la Fronde (0,45), 0 % sur un effet déclenché ;
  - dégâts et durée de la Brûlure, gel de 0,5 s sans toucher au ralentissement, bonus contre cible brûlée et ralentie ;
  - transmission de la Brûlure et prolongation plafonnée des ralentissements à l'élimination ;
  - Fragilité (+20 %, intensité la plus forte, expiration).
- Effets et contrats des Réminiscences, acquisition, armes, capacités ennemies, déplacements : zéro échec. Smoke vert.
- Capture `--capture-weapons --objects allumette_humide:50,thermometre:25` (Marteau), regardée : frappes et chiffres normaux, mais aucune cible ne brûle dans les six images, faute de chance. Les braises ne sont donc pas vérifiées à l'écran ; à regarder en vraie run.

Relecture par sous-agent. Corrigé :
- la Brûlure d'un cône continu partait des dégâts d'une frame, donc environ 60 fois trop faible : elle part maintenant du coup de base de l'arme ;
- le gel prenait la durée du ralentissement en cours : c'est désormais un état à part ;
- la prolongation de l'Épingle est plafonnée en données ;
- le palier du Thermomètre vaut aussi pour une Brûlure transmise.

Laissé tel quel : le crédit d'un DOT va à la dernière application, même plus faible.

## 26. Compte rendu G2b, étape 3 : élimination, marche, niveau (30 septembre)

| Objet | Niveau n | Palier 25 |
|---|---|---|
| Pétard mouillé | Une victime d'un coup direct d'arme explose : 20 % + 1,6 % × n des dégâts du coup fatal, dans 50 px × Taille | L'explosion se répète 0,25 s après |
| Dé à coudre | Une élimination par un coup direct d'arme rend 0,1 + 0,02 × n PV | Une élite, un champion, un miniboss ou un boss tué rend en plus 5 % des PV max |
| Semelle usée | Après 2 s de marche sans arrêt, la prochaine attaque fait +15 % + 1 % × n ; la marche repart de zéro après l'attaque | Deux attaques chargées |
| Boîte de pansements | Chaque niveau gagné soigne 1 % + 0,06 % × n des PV max | Trois niveaux gagnés dans la même frame : 1 s d'invulnérabilité |

- **Pas de récursion :** l'explosion frappe comme un effet déclenché, donc une victime de l'explosion ne réexplose pas et ne soigne pas.
- **Soins ordinaires :** le Dé et la Boîte soignent normalement. Prévoyance stocke leur excédent, et une Reprise en cours les compte comme une récupération : c'est voulu (P4).
- **Semelle :** la charge est prise par l'attaque qui part (tir, frappe, chaîne après avoir trouvé une cible, cône à son allumage), jamais par l'affichage ni par une orbite. Retour : bouffée cuivrée aux pieds quand elle est prête.
- **Catalogue :** 22 objets proposés, 14 de propriété et 8 de déclencheur. Les accès « Q » et « V » de la référence ne sont pas encore appliqués : tout est proposé, en attendant les déblocages.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` : 51 assertions, zéro échec. Parmi elles :
  - double explosion à 60 % du coup fatal au niveau 25 ;
  - soin par élimination, bonus d'élite, rien pour une élimination par effet ;
  - charge de marche, remise à zéro par l'arrêt et après l'attaque, deux charges au palier ;
  - soin par niveau et invulnérabilité de cascade.
- Effets et contrats des Réminiscences, acquisition, armes, capacités ennemies, déplacements : zéro échec. Smoke vert.
- Capture `--capture-weapons --lethal --weapons heavy_hammer --objects petard_mouille:25`, regardée : chaque cible tuée par le Marteau laisse une zone de feu tramée à sa place.

Relecture par sous-agent. Corrigé : la Semelle se rechargeait aussitôt après une attaque tant que le joueur marchait.

**À mesurer :** chaque explosion parcourt la liste des ennemis. Une arme de zone qui tue 20 ennemis par frame fait 20 parcours, 40 au palier. Ce coût n'est pas encore passé au banc dense (`/bench` avec le Pétard).

## 27. Lot G0 — les propriétés nommées sur les cartes : découpage (30 septembre)

Application de la [référence](21-systeme-de-jeu.md) §3 (« la carte nomme la propriété touchée ») et §11 (« la propriété touchée, la valeur avant → après, les armes concernées »). Un commit.

- **Données :** chaque stat affichée (`data/ui/stats.json`) porte sa propriété de la grammaire §7 : Force, Fréquence, Nombre, Taille, Portée, Durée, Précision, Élan. Les stats de survie et de collecte n'en ont pas. Les stats d'arme hors grammaire sont rangées ainsi :
  - vitesse de projectile : Portée ;
  - vitesse d'orbite : Fréquence ;
  - guidage : Précision ;
  - recul : Force ;
  - perforation : Nombre.
- **Cartes d'arme :** « Fréquence · Cadence 0,8 /s → 0,9 /s +16 % ». Le préfixe tombe quand la stat porte déjà le nom de la propriété (« Portée +12 % »).
- **Cartes d'objet :** la même règle pour chaque effet, puis une ligne « Pour : Faucille, Arc du gymnase » qui nomme les armes portées que l'objet renforce. Elle dit « Aucune de tes armes » si l'objet ne sert à rien aujourd'hui (P2 : que renforce-t-il dans ma construction ?).
- **Armes concernées par propriété :** une seule règle, dans le code des armes, lue par la carte (et plus tard par les affinités G4).
  - Fréquence, Précision : toute arme qui attaque par coups (ni orbite ni cône continu) ;
  - Nombre : tirs, salves, frappes de mêlée ;
  - Taille : mêlée en arc ou en cercle, cône, orbes, zones des armes spéciales ;
  - Portée : toutes ;
  - Durée : armes à statut, feu au sol, champ de l'Aiguille ; toute arme qui frappe si un objet pose des statuts (Allumette, Glaçon).
- **Vérification :** `tools/test_objects.sh` (propriétés et armes concernées), capture `--capture-levelup`.

### Compte rendu (30 septembre)

Livré comme découpé. `WeaponProperties.Concerns` porte la règle des armes concernées ; `StatCatalog` lit la propriété de chaque stat.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_objects.sh` : 54 assertions, zéro échec, dont les noms de propriété, la règle des armes concernées (y compris orbes et Durée par objet) et la ligne « Pour » d'une carte.
- Effets des Réminiscences, acquisition, armes : zéro échec. Smoke vert.
- Capture `--capture-levelup`, regardée, sur la carte légendaire :
  - Cloueuse : « Fréquence · Cadence 0,8 /s → 0,9 /s +16 % », « Portée +12 % », « Force · Dégâts », « Nombre · Perçage 3 → 4 » ;
  - Papier carbone : « Pour : Arc du gymnase, Cloueuse » ;
  - Pince à linge : « Aucune de tes armes » en rouge, car ni l'arc ni la cloueuse ne posent de statut.

Relecture par sous-agent. Corrigé : Durée comptée pour toute arme quand un objet pose des statuts ; Taille pour les orbes ; motif et type d'arme lus sans tenir compte de la casse.

Laissé tel quel : la Perforation reste rangée en Nombre alors qu'elle ne vaut que pour les tirs. Aucun objet proposé ne la porte (Reflet brisé est retiré), donc aucune carte ne peut la montrer fausse. À revoir si un objet de perforation revient.

## 28. Lot G3 — ascensions d'armes : découpage et proposition pour 20 armes (30 septembre)

Application de la [référence](21-systeme-de-jeu.md) §3 : au niveau 50, la carte suivante de l'arme propose deux voies, au choix et pour de bon.

### Mécanique (étape 1)

- **Données :** une arme porte deux `ascensions` dans `weapons.json`. Chaque voie se décrit par les mêmes leviers, sans code propre à l'arme :
  - motif d'attaque remplacé (`attack_pattern`) ;
  - stats multipliées (`stat_multipliers`) ou fixées (`stat_overrides`), après les gains de niveau ;
  - effet à l'impact remplacé (`on_hit_effect`) : ralentir, désorienter, faire saigner, et désormais figer ;
  - réglages de l'effet spécial remplacés (`special_overrides`) ;
  - part des copies du Papier carbone (`copies_multiplier`) ;
  - drapeaux de comportement (`flags`), pour ce que les stats ne disent pas (orbite qui s'éloigne et revient).
- **Offre :** tant qu'une arme au niveau 50 n'a pas choisi, chaque offre de niveau montre ses deux voies et une troisième carte. Passer la laisse en attente. La bannir écarte l'arme de la run, comme pour une amélioration.
- **Lisibilité :** carte « ASCENSION » (nom, règle, « Voie définitive ») ; la pause affiche « Arc du gymnase · Volée ».
- **Voies de l'étape 1 :** les huit de la référence (Arc du gymnase, Faucille, Cloche d'école, Boîte à musique). Valeurs de départ :

| Arme | Voie | Leviers |
|---|---|---|
| Arc du gymnase | Volée | Salve en éventail, projectiles × 2, copies × 2 |
| Arc du gymnase | Transpercer | Un seul trait, perforation illimitée, dégâts × 1,5, aucune copie |
| Faucille | Moisson | Cercle complet, cadence × 0,75 |
| Faucille | Estoc | Frappe droite, portée × 2 |
| Cloche d'école | Glas | Ralentissement deux fois plus long, sans recul |
| Cloche d'école | Tocsin | Recul × 2,5, désoriente 1 s au lieu de ralentir |
| Boîte à musique | Ronde | Les orbes s'éloignent et reviennent (portée de 0,6 à 1,6 × en 2 s) |
| Boîte à musique | Berceuse | Orbes à 0,6 × la portée, qui figent 0,6 s au contact |

### Proposition pour les 20 autres armes (étape 2, à valider par Raphaël)

Chaque arme reçoit une voie « plus large » et une voie « plus concentrée », pour que le choix change la façon de jouer (P5) et tire vers des objets différents (P2).

| Arme | Voie A | Voie B |
|---|---|---|
| Parcmètre | **Séisme** : onde tout autour, cadence × 0,7 | **Contravention** : coup droit, dégâts × 1,8, recul × 2 |
| Lance-billes | **Grêle** : billes × 2, dégâts × 0,7 | **Bille d'acier** : une bille, perforation 5, dégâts × 2,5 |
| Parapluie | **Rafale** : cadence × 1,6, portée × 0,8 | **Ouvert** : frappe en arc de 120°, recul |
| Cloueuse | **Agrafeuse** : salve de 3, dégâts × 0,5 | **Clou de charpente** : dégâts × 2, perforation illimitée, cadence × 0,7 |
| Pelle à neige | **Congère** : ralentit 2 s | **Déblayer** : cercle complet, recul × 2 |
| Rallonge | **Court-circuit** : désoriente 0,8 s | **Enrouleur** : portée × 1,5, cadence × 0,8 |
| Assiettes | **Service complet** : assiettes × 2 | **Vaisselle cassée** : perforation 3 |
| Râteau | **Herse** : Saignement deux fois plus long | **Ratisser** : cercle complet |
| Scalpel | **Suture** : soigne tous les 3 coups au lieu de 5 | **Incision** : fait saigner |
| Lentille de phare | **Balayage** : trois rayons, dégâts × 0,6 | **Foyer** : dégâts × 2, cadence × 0,6 |
| Trousseau | **Passe-partout** : sauts de chaîne × 2 | **Clé unique** : un seul saut, dégâts × 2,2 |
| Boussole | **Rose des vents** : tirs × 3, dégâts × 0,5 | **Nord** : perforation 3, guidage fort |
| Polaroïd | **Rafale de flashs** : cadence × 1,5 | **Surexposition** : désorientation deux fois plus longue |
| Baguette de sourcier | **Fourche** : tirs × 2 | **Source** : dégâts × 1,8 |
| Gomme | **Mie de pain** : cercle complet | **Encre** : dégâts × 1,6, portée × 1,3 |
| Lampe à pétrole | **Nappe** : feu au sol deux fois plus long et plus large | **Mèche courte** : tirs × 2, feu plus petit |
| Gants de boxe | **Enchaînement** : l'écho frappe deux fois | **Crochet** : dégâts × 1,7, recul × 2 |
| Craies | **Marelle** : formes plus grandes | **Dessin appliqué** : tirs × 2 |
| Transistor | **Grandes ondes** : cône plus large | **Fréquence pirate** : le cône désoriente |
| Chronomètre | **Arrêt sur image** : fige au lieu de ralentir | **Compte à rebours** : cadence × 1,5 |

**Hors lot :** les traits d'arme lus par les Réminiscences (cible cherchée, contrôle natif) restent ceux de l'arme de base. Une Faucille en Moisson (cercle) garde donc son éligibilité à Convergence. À revoir si l'écart gêne.

## 29. Compte rendu G3, étape 1 : mécanique et quatre armes (30 septembre)

- **Données :** `ascensions` dans `weapons.json` pour l'Arc du gymnase, la Faucille, la Cloche d'école et la Boîte à musique, avec les leviers et valeurs du §28. Le levier `special_overrides` (réglages d'un effet spécial) n'est pas encore codé : aucune des huit voies n'en a besoin, il viendra avec l'étape 2.
- **Arme :** la voie choisie remplace le motif et l'effet à l'impact, multiplie ou fixe des stats après les gains de niveau, et règle la part des copies du Papier carbone.
  - Nouvel effet à l'impact, « figer », par l'état de gel des créatures.
  - Nouveau drapeau « orbite qui va et vient » : le rayon passe de 0,6 à 1,6 × la portée en 2 s.
- **Offre :** tant qu'une arme au niveau 50 n'a pas choisi, chaque offre montre ses deux voies et une troisième carte.
  - La carte de survie ne remplace jamais une voie.
  - Une voie ne se bannit pas : on la choisit ou on passe.
  - Une relance ne change que la troisième carte.
- **Partout où l'arme compte :** propriétés concernées (cartes d'objets), traits lus par les Réminiscences, pause (« Arc du gymnase · Volée »), perforation illimitée affichée « ∞ ».
- **Code :** `Player.Ascensions.cs` (choix de la voie, orbite), `WeaponAscensionData`, `FragmentOption.AscensionType`.

**Vérifications :**
- `dotnet build` : zéro avertissement.
- `tools/test_weapons.sh` : zéro échec, dont huit contrôles d'ascension :
  - deux voies par arme et choix définitif ;
  - Volée (éventail, flèches et copies × 2) et Transpercer (une flèche, × 1,5, perforation illimitée, aucune copie) ;
  - offre des deux voies ensemble ; Berceuse qui fige ; Ronde de 0,60 à 1,60 ;
  - la voie comptée par les règles de propriétés et de traits.
- Objets, effets et contrats des Réminiscences, acquisition, capacités ennemies, déplacements : zéro échec. Smoke vert.
  - Un passage du banc d'effets a fini sur une exception à la fermeture, après « RESULT failures=0 ». Elle ne s'est pas reproduite aux passages suivants.
- Captures regardées :
  - `--capture-levelup` : « ASCENSION · NIVEAU 50 », Volée et Transpercer côte à côte avec « Voie définitive : l'autre est oubliée », puis une troisième carte ;
  - `--capture-weapons --ascensions makeshift_bow:volley,chipped_blade:harvest,music_box:round` : l'Arc tire en éventail, la Faucille touche les cinq cibles tout autour, les notes de la Ronde s'éloignent puis reviennent.

Relecture par sous-agent. Corrigé :
- règles de propriétés et de traits lues sur l'arme portée, voie comprise ;
- bannissement d'une voie refusé ;
- perforation illimitée affichée « ∞ ».

**Reste :**
- la Faucille en Moisson frappe en cercle mais dessine encore l'arc de la faucille : le visuel d'onde circulaire est à reprendre avec la direction artistique ;
- l'historique de run ne retient pas la voie choisie ;
- les voies des 20 autres armes attendent l'avis de Raphaël (§28).

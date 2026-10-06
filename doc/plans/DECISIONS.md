# VESTIGES — Décisions de Raphaël et arbitrages restants

Version 0.9 · 26 septembre 2026 · Référence de validation du dossier.

Ce registre intègre les retours structurants après la première lecture des plans et la validation des déplacements de base du 22 septembre. Les validations ci-dessous sont acquises. Le socle de 01 est implémenté et validé ; le dash du prototype D demandé est livré et validé par Raphaël ([compte rendu](01-deplacements.md#8-prototype-de-mobilité--22-septembre-2026)). Les variantes alternatives F1 restent des essais ; les mobilités spécifiques attendent le casting et les sprites refaits.

## 1. Décisions acquises

| Sujet | Décision de Raphaël | Application |
|---|---|---|
| Direction audio (25 septembre) | Musiques de jeu vidéo avec rythme et mélodie ; distorsion et Effacement possibles. Les essais de drones abstraits n'ont pas convaincu et leurs consignes sont abandonnées | [Guide audio V2](../AUDIO-GUIDE.md), Bible §8, Stratégie V2 §21 et [plan 15](15-audio.md) |
| Production audio (25 septembre) | Aucun budget de prestation ni d'achat ; abonnement Google et crédits ElevenLabs déjà disponibles. Privilégier une approche mixte avec sons existants et collaboration bénévole éventuelle | Plan 15 ; aucune dépense supplémentaire prévue |
| Sélection audio (25 septembre) | Les agents cherchent plusieurs candidats par effet ; Raphaël fait les comparaisons et choisit. Couverture exhaustive des sons et effets du jeu demandée | Inventaire croisant fichiers, appels, données et besoins V2 ; choix suivis par identifiant, lots d'écoute limités |
| Contrôle | Axes écran, diagonales normalisées, amplitude du stick préservée, contrôle clavier/manette ; déplacements de base refaits explicitement validés le 22 septembre | 01, socle livré validé ; recette exhaustive distincte |
| Mobilité | Dash commun livré validé (« Ok top je valide ») ; mobilités spécifiques après nouveau casting et sprites | 01 D validé ; 01 E dépend de 06/08 |
| Mode dev | Tout le contenu existant débloqué pour les essais | Profil dev séparé ; [utilisation](../DEV-MODE.md) |
| Score | Visible et vivant pendant la run ; aucune progression vers le record ni annonce de dépassement pendant la partie | 02/04, record uniquement au bilan final |
| Juiciness | Trois intensités validées ; ambition très élevée sur mouvement, combat, collecte et récompenses | 02, intensité et qualité à tester |
| Mort | Refonte majeure du bilan ; inspiration Megabonk adaptée à Vestiges | 02/04 |
| Début de run | Actuellement trop facile, niveaux trop rapides, menace insuffisante ; le 23 septembre, toujours niveau 1→5 en moins de 30 s après correctif | 03, essai menace avant essai XP ; mesure horodatée ajoutée |
| Bestiaire à distance | Plus d'ennemis à distance, attaques diversifiées et originales, pas seulement des projectiles simples (23 septembre) | 07, menaces à distance proposées ; priorité P1 pour le début de run |
| Performance | Ressenti « beaucoup plus fluide » après le correctif de la flèche du 23 septembre | 01 §10, cible 1080p encore ouverte |
| Mécaniques existantes | Peuvent être revues lorsqu'une amélioration est démontrée | Tous les plans, essais avant/après |
| Hub | Clarté maximale, peu de texte, détails à la demande | 04 |
| Collection | Page armes/objets accessible directement depuis le menu principal | 04/05 |
| Build | Quatre armes et quatre passifs conservés | 05 |
| Corps à corps | Portée actuelle trop faible ; augmenter l’allonge utile dès les armes de base | 05, ciblage/dégâts/visuels à vérifier ensemble ; valeurs à calibrer |
| Armes à distance | En augmenter la proportion | 05, catalogue et disponibilité réelle ; ratio final à proposer |
| Bonus de portée et de zone | Prévoir des objets/bonus augmentant la portée et le diamètre d’impact | 05, effets distincts, cumul et compatibilité par famille |
| Objets | Système distinct, capacité sans limite de design, exemplaires cumulables, raretés, déblocage partiel initial | 05 |
| Quêtes | Variées et indépendantes des Souvenirs ; déblocages directs | 06 et migration 05 |
| Casting | Refonte d’au moins cinq à six personnages et de tous leurs sprites, validation avant mobilités spécifiques | 06/08 prioritaires avant 01 E |
| Bestiaire | Ajouter quelques nouvelles créatures ; étudier des boss de familles | 07 |
| Effets d'attaque | Tous les sprites d'attaque, du joueur comme des ennemis, dans la DA (pixellisés, couleurs du jeu) ; attaques du joueur « juicy » sans perte de fluidité ; réglage pour activer ou désactiver animations et projectiles et en régler l'opacité (25 septembre) | 08, lots V0–V3 livrés, recette attendue |
| Terrain | Examiner et planifier les améliorations utiles des tiles et des systèmes voisins | Nouveau plan 10 |
| Innovation | Proposer des mécaniques originales précises | Nouveau plan 11, propositions non approuvées |
| Art | Pixel art accepté, suffisamment détaillé et uniforme entre les sprites ; choisir une méthode de production | 08, recommandation explicite |
| Personnage initial | Vagabond pour les nouveaux profils (23 septembre) ; les profils existants conservent leurs personnages | 06 B, migration à écrire |
| Format des sprites joueur | Huit orientations ; 48×64 abandonné le même jour car « géant » en jeu : cadre 32×48, ~35 px de haut (23 septembre) | 08 : `MODEL_SCALE`, chargeur 8 directions livré |
| Production des sprites | Procédurale par scripts, « qualité maximale », retouches éventuelles de Raphaël ; le procédural doit suffire (23 septembre) | 08 : pipeline de génération à la place de la recommandation « dessin puis retouche » |
| Casting | Six personnages validés le 23 septembre ; personnages « atypiques » encouragés | 06 ; sprites pilotes 08 |
| Densité d'ennemis | Manque ressenti ; mesures objectives et comparaisons demandées (23 septembre) | 03 : banc de densité, anneau d'apparition calé sur l'écran, densité croissante |
| Premier essai de menace | Présage et bond annoncé du Charognard (23 septembre) | 07/03 A2 : [implémentation](07-bestiaire-et-rencontres.md#premier-essai-menace--23-septembre-2026), recette en jeu à faire |
| Retours du 24 septembre | Nouveaux ennemis validés ; joueur encore trop grand, idle à animer (refonte du dessin reportée) ; flèche vers le centre inutile ; PV et infos peu visibles ; police trop pixelisée ; micro-événements toutes les 2–3 min « vraiment réfléchis » ; variantes fortes des mobs de base | 08 (−15 %, idle 6 frames) ; 04 (HUD, Saira Semi Condensed) ; [12](12-micro-evenements.md) (5 événements, élites, Souverains) ; recettés le 25 septembre |
| Butin et anomalies (24 septembre, soir) | Loot aléatoire à raretés, composante du jeu, présentation différente de Megabonk, sources identifiables sur la carte ; événements aléatoires rares, liés au lore et si possible au joueur et à l'oubli, pénalisants sans tuer la run ; « pas obligé de tout traiter maintenant » | Propositions : [13](13-butin.md), [14](14-anomalies-du-monde.md) ; décisions §8 et §7 à trancher |
| Recette du 25 septembre | Cadence des micro-événements validée en l'état (détails à revoir plus tard) ; élites bien dosées ; micro-événements « bien » ; HUD « bien mieux » ; le soin est peut-être trop difficile à obtenir | 12 et 04 recettés ; soin noté en [13](13-butin.md) et [03](03-boucle-et-rythme.md), pas traité maintenant sauf correctif trivial |
| Priorité du 25 septembre | Nouveau gros chantier prioritaire : visuel et juiciness de tout le jeu (« dix fois plus joli et plus juicy »), dans l'ordre : régression des biomes, audit des décors (sprites invisibles, hitbox frustrantes), refonte procédurale des décors biome par biome en commençant par l'urbain, juiciness et particules ; découpage en lots dans les plans avant de coder, un lot à la fois | [10 §6–7](10-terrain-et-tiles.md), [08](08-direction-artistique.md), [02 §7](02-juiciness-score.md) ; 13 et 14 restent non arbitrés et non implémentés |
| Biomes | « Avant les biomes étaient mélangés, et c'était mieux » : apparaître sur un seul terrain est une régression | Mesurée puis corrigée : mosaïque de régions, [10 §6](10-terrain-et-tiles.md#6-régression-un-seul-biome-autour-du-départ--25-septembre-2026) ; recette en jeu à faire |
| Retours du 26 septembre (jeu) | Champs « un peu vides », petits sprites éparpillés qui « ne racontent pas grand-chose » ; les attaques ennemies devraient avoir des sons ; tiles améliorables ; jonctions entre environnements trop brutes ; l'oubli « se voit assez peu », à exploiter visuellement ou autrement ; question : tous les mobs doivent-ils converger ? ; initiatives perf appréciées, investigation plus poussée souhaitée ; début de run trop agressif (« se faire sauter dessus à la seconde 1 », « il faut avoir le temps de respirer »), sans tomber dans l'extrême inverse | Livré : répit et montée d'ouverture ([03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)), sons d'attaque provisoires branchés ([15](15-audio.md)), décors en tronçons, ×2 FPS ([10 §10](10-terrain-et-tiles.md#10-investigation-performance--26-septembre-2026)). Proposé : composition des champs ([08 P4b](08-direction-artistique.md)), tiles et jonctions ([10 §9](10-terrain-et-tiles.md#9-retours-du-26-septembre--tiles-et-jonctions)), oubli ([16](16-oubli-sensible.md)), perception des créatures ([07 §7](07-bestiaire-et-rencontres.md#7-question-du-26-septembre--toutes-les-créatures-doivent-elles-converger-)) |
| Écran d'accueil (26 septembre) | Trop classique ; ne plus afficher les statistiques du personnage (PV, attaque…) mais **montrer son sprite** ; affichage de jeu fini, dans la DA ; « surprends-moi » | 04 : [accueil refait](04-interfaces-et-hub.md#accueil-refait--26-septembre-2026), recette attendue |
| Recette du 26 septembre (après-midi) | Accueil refait **validé** ; effets d'attaque en pixel art (V0–V3) **validés** ; fluidité « ok » ; ouverture de run « ok, peut-être un tout petit trop peu adoucie » ; sons : traités par un autre agent. Nouvelle machine : Mac Apple Silicon (M1 Pro) | 04 et 08 V0–V3 recettés ; ouverture adoucie un cran de plus ([03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)) ; plan 15 hors de ce fil ; Steam et outils rendus compatibles Mac |
| Retours du 26 septembre (soir) | Nouvelles tuiles de la forêt « validées à 100 %, mille fois mieux » ; les sorts ennemis apparaissent « en vertical » alors que la carte est en 2.5D, et le respect de la vue isométrique mérite une étude plus large ; le joueur peut se promener dans le Néant du bord de carte sans limite | Forêt : référence de style pour les autres biomes ([10 T1](10-terrain-et-tiles.md)) ; audit et lots I1–I4 ([08](08-direction-artistique.md#respect-de-la-vue-isométrique--chantier-du-26-septembre-2026)) ; bord du monde infranchissable à la marche comme au dash |
| Armes, coffres, modificateurs (26 septembre) | Tous les sprites d'armes à revoir (hors DA) ; noms d'armes « trop clichés » ; ne plus montrer portée/dégâts au choix d'une arme, au plus une description courte ; au level-up, montrer la ou les stats augmentées, avec des raretés d'amélioration (plus rare = plus fort ou plusieurs stats) ; armes et stats visibles en pause ; stats à virgule envisagées ; coffres « disparus » à réintégrer et rendre visibles ; modificateurs en jeu (chance, difficulté), mécanique liée à l'Effacement, sanctuaires de mémoire à bonus et mécanique miroir à malus, « sans devenir lourdingue » ; objets importants mais peut-être plus tard ; moins de mentions du jeu de référence dans la doc ; inventaire complet des restes V1 ; traiter vague par vague avec soin | Audit et plan : [17](17-armes-coffres-modificateurs.md), [18](18-inventaire-restes-v1.md) ; rien d'implémenté, décisions 17 §6 et 18 §5 attendues |
| Arbitrages des plans 17 et 18 (26 septembre) | Ordre des vagues laissé au plus logique ; noms d'armes « beaucoup mieux » ; icônes 32×32 ; arme en main « à tester », désactivée par défaut ; Mémoriaux et Failles validés ; plafond de niveau d'arme à déterminer (« 50, 100 ? ») ; éléments de lore et POI désactivés jusqu'à leur refonte (POI : vague 3) ; Colosses « supprimer peut-être » ; jeu absent de Steam. Rareté sur l'arme ou seulement sur les améliorations : **sans réponse** | [17 §6](17-armes-coffres-modificateurs.md#réponses-de-raphaël--26-septembre-2026), [18 §5](18-inventaire-restes-v1.md#réponses-de-raphaël--26-septembre-2026) ; rareté à confirmer avant le lot 1A |
| Lot 0A, coffres (26 septembre) | Lancement du plan 17 par le lot 0A, un seul lot puis compte rendu pour la recette. En cours de lot : « essaie de voir si tu peux pas faire certains bench en headless […] je veux bien que tu l'implémentes » ; puis « hésite pas à avancer à fond en parallèle sur d'autres sujets » | [17, lot 0A livré](17-armes-coffres-modificateurs.md#lot-0a-livré--26-septembre), recette attendue ; mesure headless `tools/measure_run.sh` |
| Plan 17, arbitrages du soir (26 septembre) | Rareté « seulement sur les améliorations » ; « le mot don me va » ; décors urbains jamais placés : « tu peux les virer » ; style des icônes et fréquence des coffres à juger en recette | [17 §6](17-armes-coffres-modificateurs.md#réponses-de-raphaël--26-septembre-soir) ; vague 1 débloquée |
| Recette des planches du plan 17 (26 septembre) | « Tu peux faire mieux pour les armes » ; coffres « un peu plus détaillés / avoir des motifs » ; « pour le reste c'est parfait. Go » | Coffres et icônes v2 ([17 §6](17-armes-coffres-modificateurs.md#réponses-de-raphaël--26-septembre-soir)) ; vague 1 lancée |
| Vague 3 du plan 17, retours (26 septembre, nuit) | Pause des écrans Mémorial et Faille : « garder la pause, pas de soucis ». Malus des Failles : « je préfère des malus sur la map que des malus de stats », liste à revoir avant validation | [17, vague 3](17-armes-coffres-modificateurs.md#vague-3-détaillée--26-septembre-soir) ; Stratégie V2 §11 amendée ; Oublis à refaire en effets de carte après son choix |
| Recette des noms, icônes et Oublis (26 septembre, nuit) | Noms du catalogue 4.6 validés sauf « Dessin d'enfant » et « Aiguille d'horloge » (« j'aime pas trop le nom ») ; les trois icônes v2 validées ; les neuf Oublis de carte validés, leurs noms « un détail » | [17 §6](17-armes-coffres-modificateurs.md#réponses-de-raphaël--26-septembre-nuit) ; vague 2 (2A, 2B) et Oublis de carte lancés ; noms provisoires Craies et Chronomètre |
| Ordre des chantiers (26 septembre) | « Oui je valide l'ordre » : relecture en suspens, puis sol (T2 jonctions → T1 → T3) et oubli (O1 → O3 → O4/O5 → O2/O6). Raphaël, absent, délègue les autres arbitrages : « prends les décisions les plus rationnelles […] implémente-les » | Arbitrages délégués en §7, **provisoires jusqu'à sa relecture** |
| Lore (27 septembre) | L'histoire se raconte par le jeu, fine, jamais imposée ; beaucoup de textes actuels sont « trop directs » et à reprendre ; poésie, drame, histoires cachées entremêlées ; éléments présents dès le début compris longtemps après ; puzzle de fond qui ne gêne pas le jeu ; interruptions de run possibles si ni frustrantes ni répétitives ; pas flou ni métaphorique en permanence, « une vraie histoire, tangible » ; une révélation forte bienvenue, sans trope facile ; le lore des docs n'est pas une référence absolue | [19](19-lore.md) : étape A livrée ; rien d'écrit ni de modifié en jeu |
| Lore, réponses à l'étape A (27 septembre) | Vérité centrale aux bords ouverts ; cause entre « oubli choisi » et « jamais expliquée » ; personnages = des gens que le monde a oubliés, **une dizaine** avec personnages cachés ou versions alternatives ; une vraie logique de boucle « à penser en profondeur » ; Hub hors de la fiction, son intérêt à revoir ; créatures faites de ce qui a été oublié ; Essence = mémoire des autres, XP simple système de jeu ; monde presque-nôtre ou lieu inventé, **sans noms de lieux liés à la mémoire** ; documents bruts sans narrateur ; décor et mécaniques d'abord ; interruptions rares ; révélation et fin non tranchées | [19 §7](19-lore.md#7-réponses-de-raphaël--27-septembre) ; architectures à comparer en [§8](19-lore.md#8-architectures-à-comparer) |
| Lore, deuxième tour (27 septembre) | La marée « un peu limitée narrativement et dans la portée et la puissance de son message » : explorer de nouvelles pistes ; le personnage sait qu'il recommence (« plutôt oui sur le principe », conséquences à étudier) ; des histoires personnelles qui se croisent ; Q14 délégué | [19 §9](19-lore.md#9-deuxième-tour--27-septembre) : journal par personne avec une case « sans nom » (provisoire) ; pistes N1 à N3 |
| Lore, piste retenue (27 septembre) | « N1 me semble le plus intéressant. C'est encore à peaufiner par contre mais bonne base pour avancer » | [19 §10](19-lore.md#10-n1-retenue-comme-base--à-peaufiner) : N1 « La demande » comme base ; points P1 à P10 à trancher avant l'étape B |
| Lore, réponses partielles et pause (27 septembre) | P1 : lettre puis guichet ? « il y a un truc qui cloche » ; P2 : oui, crainte que les documents expliquent tout dès le début ; P3 et P4 délégués ; P5 : A ? ; P6 incertain (y compris savoir si le personnage sait qu'il recommence) ; pause : « j'ai un peu de mal à me projeter […] on reprendra ça plus tard » | [19 §11](19-lore.md#11-réponses-partielles-à-10--27-septembre-pause) : P3 B et P4 C provisoires ; reprise sur des exemples concrets |
| Partie longue du 27 septembre (retour du 28) | L'effacement « commence très tôt » : vers 4 min, en moins d'une minute, des zones entières deviennent blanches, « à revoir » (l'effet visuel est « très réussi ») ; difficulté qui monte vite, dure dès 4 min ; « le personnage est une chips » : peu de PV, aucun moyen perçu d'en gagner, ni bouclier, réduction de dégâts douteuse, régénération lente et sans bonus ; tirs (ennemis et joueur) peu visibles, les « balles » surtout ; son du coffre toujours faux (la mélodie du défilement part trop tard) ; contenu des coffres à revoir ; « en dehors de ça le jeu est très bien » | Tempo de l'oubli mesuré et lots 8A–8C proposés ([03 §8](03-boucle-et-rythme.md#8-tempo-de-loubli-et-survie--28-septembre-2026)) ; son du coffre corrigé ([15](15-audio.md)) ; contenu des coffres rattaché au plan 13 |
| Lots 8A–8C validés (28 septembre) | « Ok ça me va » ; bouclier : « j'aime bien ce que fait [le jeu de référence] avec ses personnages : ils ont tous un bouclier de base plus ou moins grand, il tank un coup (ou plusieurs si petits dégâts) sans limite de dégâts, comme ça il évite un one shot. À voir si on l'implémente telle quelle ou si on l'adapte à Vestiges » ; « je te laisse faire 8A et la suite » | [03 §8](03-boucle-et-rythme.md#8-tempo-de-loubli-et-survie--28-septembre-2026) : 8A puis 8B (bouclier par personnage) |
| Retours du 28 septembre, après 8A–8C | Équilibrage « mieux » : jeu assez dur, réussite liée au build et aux déplacements, pics de difficulté bienvenus ; aucun lieu où dépenser l'Essence vu en 10 min ; coffres décevants, armes lâchées alors que les emplacements sont pleins, bonus non expliqués ; un pouvoir par personnage ; +1 projectile partiel, stat de taille des projectiles souhaitée ; rareté des améliorations peu attrayante ; chance à la manière du jeu de référence sans le copier ; autels de mémoire qui préservent une zone contre de l'Essence, avec bonus local ; **point capital** : risque pas assez récompensé, manque de dopamine, montée de niveaux en cascade, pas de tomes, beaucoup d'ennemis. « Ne les traite pas, mets-les dans un plan » | [20](20-recompense-et-puissance.md) : consignés et vérifiés, rien de traité |
| Lore, reprise concrète (29 septembre) | Correction « l'oubli ne marche qu'à plusieurs » : « mieux, à la fois plus poétique et plausible » ; ton des documents « top » ; fil du Facteur « bien ». Histoire mémorable, qu'on peut manquer, mais parfaitement ficelée, touchante, profonde, à plusieurs niveaux de lecture ; version finale rapide, en script global et détaillé, écrit en auteur ; relecture et retours ensuite | [VESTIGES-LORE.md](../VESTIGES-LORE.md) v1.0 à relire ; [19 §13](19-lore.md#13-script-global--29-septembre) |
| Relecture du script du lore v1.0 (29 septembre) | « C'est pas mal du tout dans l'ensemble, bravo » ; le jeu qui change légèrement après la fin, « très cool », avec un objet débloqué en plus du personnage (objets débloqués petit à petit) ; interruptions à placer peut-être en fin de run (« pas sûr ») ; « rien ne justifie correctement que la map change à chaque run, c'est un vrai défaut dans l'histoire, il ne faut aucun défaut de ce type » | [VESTIGES-LORE.md](../VESTIGES-LORE.md) v1.1 : §7 bis (carte et conventions de run justifiées), Clé verte et objets de fin, scènes en fin de run |
| Réponses du 28 septembre (plan 20 §4) | Plaisir : cascade, choix enchaînés et puissance visible, à des degrés différents ; cascade non permanente, exponentiel réservé aux très bonnes runs, gain de niveaux à remonter un peu pour tous ; « avoir des métriques et des calculs pour ça est quelque chose d'important ». XP issue du risque, de la gestion de la masse et des boss ; boss intermédiaire difficile à mi-parcours dont le joueur règle la difficulté ; Résurgences plus payantes ; builds autour de l'XP, de la chance et d'un troisième axe ; ne pas recopier le contrôle de difficulté du jeu de référence ; pas de tomes. Ennemis : « le plus possible », plus de 1 000. Dégâts à distance peut-être trop forts | [20 §6–§9](20-recompense-et-puissance.md) : modèle, leviers, étude et lots proposés ; cibles à valider |
| Réponses du 28 septembre, soir (plan 20 §9) | Excellente run : « niveau 300/400 (voire même 1000 si on cape les armes, genre au bout d'un moment les niveaux ne servent plus) » en 45 min, difficulté des créatures qui suit ; montée graduelle et « par vague », selon les événements, le build et le talent. Boss intermédiaire : option A. Axe de build de l'oubli : oui. Cascades : « un truc intelligent qui stack les niveaux gagnés pour éviter que ça s'ouvre et se referme en boucle ». Dégâts à distance : « à toi de voir », pistes à concrétiser. Créatures : « vraiment beaucoup », peut-être 500, pour le très late game et les Résurgences les plus dures ; optimisation par un autre agent, rendu à juger | [20 §6.6, §7.1, §8](20-recompense-et-puissance.md) : courbe plafonnée et cibles révisées, lot D1 fait, lots R1-G, R1-H, R1-T ajoutés |
| Réponses du 28 septembre, nuit (plan 20 §9) | « Les paliers me vont. » Au-delà du build complet, les niveaux ne servent plus à rien ou à « quelque chose de relativement trivial », ouvert aux idées. Réserve de niveaux : « un cache assez rapide », qui se déclenche au bout de quelques secondes et seulement quand il y a beaucoup de niveaux ou de créatures, « toujours automatique, jamais manuel ». Montée plus forte des créatures en endgame. XP et chance : pas de tomes, des objets dédiés (pas encore refondus ni branchés) et des perks (les quatre passifs sous les armes, pas encore refondus non plus), qui se cumulent | [20 §6.7](20-recompense-et-puissance.md) : règle de réserve, idées A–E pour le surplus, lots R1-F et R1-E précisés |
| Réponses du 28 septembre, fin de nuit (plan 20 §9) | Niveaux de surplus : A (souffle) et D (onde tous les 10 niveaux), E (score) éventuellement, « mais ça ne doit pas devenir overkill ». Refonte des perks : « à faire en même temps » que les leviers d'XP. R1-F (PV ×1,07 par minute après 22 min) : oui. Puis « chill, on verra ça plus tard », mise en pause | [20 §9](20-recompense-et-puissance.md) : consigné, rien d'engagé |

« Les mobs avancent successivement » reste ambigu au moment de cette révision. Une clarification a été demandée : arrivée en file jugée problématique, introduction progressive des types, ou les deux. Le plan 07 sépare ces deux sujets ; aucun comportement n'est présenté comme une préférence confirmée.

## 2. Spécifications encore à examiner

| Décision | Recommandation dans les plans | Statut |
|---|---|---|
| Mobilité active | Une action de mobilité commune, dash de base ; sauts/glissades spécifiques à certains personnages | Dash livré validé ; variantes F1 restent des essais, E après casting et sprites |
| Invulnérabilité du dash | Comparer dash sans invulnérabilité et fenêtre courte, puis choisir avec le danger de début de run | Deux variantes disponibles dans F1 : 0 ms par défaut / 60 ms d’essai ; choix ouvert |
| Objets | Rareté fixe par définition, compteur par ID, chaque exemplaire renforce un effet sans plafond d'exemplaires | Proposition technique 05 compatible avec la demande |
| Casting cible | Au moins cinq à six personnages entièrement revus, tous leurs sprites refaits avant 01 E | Six [fiches](06-fiches-casting.md) validées ; sprites pilotes à valider en jeu |
| Boss | Une variante à comportement enrichi par famille retenue ; deux prototypes avant généralisation | Proposition 07 |
| Pixel art | Densité commune, tuiles 64×32 et joueur 48×64 validé en huit orientations | Joueur validé ; densité ennemis à fixer en 08 A |
| Fabrication des sprites | Tranchée le 23 septembre : procédural en qualité maximale (voir §1) | Remplace la recommandation initiale de 08 |
| Innovation prioritaire | Rémanence offensive du déplacement, puis butin à sauver de l'Effacement | Prototypes proposés 11 |

Ni les seuils d'XP, ni les timings de mobilité, ni les chiffres des objets ne sont approuvés en tant qu'équilibrage final. Les choix antérieurs non explicitement arbitrés (personnage initial, kit de l'Éveillée, règles du classement) ne deviennent pas validés par défaut.

## 3. Contradictions retirées de la version 0.1

- Le record n'est plus un objectif affiché ni célébré pendant la run, y compris en pause.
- Les objets ne sont plus limités à un exemplaire ou trois stacks : les plafonds d'affichage/particules limitent uniquement la présentation.
- Les quêtes ne distribuent plus des Souvenirs comme étape obligatoire vers une arme.
- Les nouveaux personnages et ennemis font partie du périmètre ; l'audit de l'existant sert leur conception.
- Le quatrième personnage n'est plus conditionné à dix fragments de lore dans la cible proposée.
- Le terrain et les innovations possèdent des plans séparés pour être évalués et validés.

## 4. Validation et traçabilité

Les plans 01 (socle), 02 (avec correction record et extension bilan) et 03 (avec priorité à la difficulté initiale) disposent d'une direction validée. Le 22 septembre, Raphaël demande : « continue le travail sur le plan 1 déplacements » et précise : « les déplacements de base ont été refaits pour aller dans le sens du plan, je valide ces changements ». Le socle livré est donc validé, et le prototype D poursuit le plan sans redemander cet accord. Cette validation ne choisit pas l'inertie, l'invulnérabilité ou l'équilibrage du dash, et ne vaut pas recette exhaustive manette/captures/autres joueurs. Les autres extensions détaillées restent proposées ; les décisions déjà acquises ne seront pas redemandées.

Les cases de la roadmap signifient « implémenté et vérifié ». Une validation de design n'en coche aucune.

## 5. Retours audio du 25 septembre — lot A

Trois sélections explicites : `critical_hit_a`, `chest_open_a` (mécanisme d’ouverture seulement), `dash_start_a`. Aucune intégration effectuée. Les sept autres décisions restent `none`/`pending` telles qu’exportées. XP actuelle appréciée sans sélection définitive. Ajout d’un besoin `chest_reveal` : mélodie de quelques secondes après l’ouverture. Lot A2 de sept besoins détaillé dans [le plan 15](15-audio.md#3-bis-lot-a2--rechercher-après-le-retour-du-25-septembre).

Source probante : [export original](../audio/README.md), identique octet pour octet au fichier transmis ; [synthèse et explication impact/arme](../audio/README.md). Verbatim des notes (chaînes JSON pour conserver aussi les espaces finaux) :

### enemy_hit — none

Candidat : `None`.

```json
"Le B est le mieux des trois mais est trop long. Apres le truc c'est que je sais pas quand ce son pop réellement. normalement les sons d'attaques se font par armes."
```

### critical_hit — candidate

Candidat : `critical_hit_a`.

```json
""
```

### player_hit — pending

Candidat : `None`.

```json
"Aucun ne le fait. rechercher autre cose. le signal absrait pouvait etre une bonne idée mais pas fan de celui là. mais chercher aussi des plus classiques"
```

### dissolution — pending

Candidat : `None`.

```json
"La C est la mieux mais je suis pas convaincu."
```

### xp_pickup — pending

Candidat : `None`.

```json
"L'actuelle est plutot bien"
```

### level_up — pending

Candidat : `None`.

```json
"L'actuelle est mieux que les trois mais j'aimerai quand meme trouver autre chose"
```

### perk_select — pending

Candidat : `None`.

```json
"essayer de trouver une seule note avec un echo ptet. "
```

### chest_open — candidate

Candidat : `chest_open_a`.

```json
"Le A est parfait pour le moment où on ouvre le coffre mais apres il faut une sorte de mélodie sur quelques secondes. (l'actuelle fait a peu pres le job)"
```

### dash_start — candidate

Candidat : `dash_start_a`.

```json
""
```

### danger_warning — none

Candidat : `None`.

```json
""
```


## 6. Retours audio du 26 septembre — lot A2

[Export original archivé](../audio/README.md) et [verbatim, décisions et préparation](../audio/README.md). Trois nouveaux choix : `dissolution_a2_b`, `perk_select_a2_a`, `danger_warning_a2_a`. Dissolution : « le B mais ne pas le cropper à un seconde » ; conserver les 1,395828 s de l’original. Impacts ennemi et joueur, montée de niveau : `none`, recherche à reprendre. Révélation du coffre : « garder l'actuel » ; suivi **actuel conservé**, sans transformer le `pending` exporté en sélection de candidat. XP inchangée. Six candidats retenus au total, aucun intégré ni recetté.

## 7. Arbitrages délégués du 26 septembre

Raphaël, absent, demande de trancher au mieux et d'implémenter. Chaque choix ci-dessous est **provisoire** : il suffit qu'il le conteste pour le revoir. Raison donnée en une ligne.

| Question | Choix | Raison |
|---|---|---|
| Plan 10 §9, ordre | T2 jonctions, puis T1 sol, puis T3 chemins (validé par Raphaël) | Les jonctions sont le défaut le plus visible de la carte en mosaïque |
| Plan 16, ordre | O1 → O3 → O4/O5 → O2/O6 (validé par Raphaël) | Voir l'oubli avant d'en payer le prix |
| Plan 16, coût de l'oubli | Débuffs de la V2 **adoucis** et en JSON, avec un signal clair ; calibrage au lot O4 | La V2 fait autorité ; le retour du jour demande de respirer, pas de punir |
| Plan 16, Néant (0 %) | Traversable avec dégâts continus (V2) | Un mur pourrait enfermer le joueur ; la V2 fait autorité |
| 08 P4b, composition des champs | Oui, après T2/T1 : la ferme se lit mieux sur un sol refait | Retour « champs un peu vides » ; dépend du sol |
| 07 §7, convergence | Proposition retenue : chasseurs inchangés, habitants avec perception et laisse, créatures distancées qui perdent la trace ; mesure de densité avant/après | Rencontres contournables sans baisser la pression du flux |
| Dash, invulnérabilité | 0 ms conservé (réglage validé avec le dash) | Pas de retour contraire ; le début de run vient d'être adouci |
| Plans 13 et 14 | Recommandations des plans retenues (trois formes de butin, rareté fixe, coffres en conteneurs ; anomalies Écho, Oubli de soi, Effondrement, plafond 25 %, une ligne de texte) mais **pas d'implémentation avant les lots ci-dessus** | Gros chantiers ; le socle d'objets du plan 05 reste leur prérequis |
| Boss de famille | Deux prototypes, plus tard (07) | Hors de l'ordre validé |
| Colosses (07, 27 septembre, session cloud) | Un Colosse par crise à partir de la deuxième, proposé puis **retiré** à la fusion de main : le plan 17 (lot 0C) a supprimé les Colosses | La décision 0C prime ; plus rien à faire apparaître |

## 8. Retour audio A3 — 26 septembre 2026

[Export et notes exactes](../audio/README.md) : impact ennemi B (`enemy_hit_a3_b`) retenu ; aucun dégât joueur satisfaisant ; level-up jugé hors thème (champ `pending` conservé). Sept candidats retenus au total, sans intégration. Ne pas déduire un nouveau style musical du refus. Le son de level-up actuel reste la référence précédemment préférée ; la correction de méthode proposée par l’agent est détaillée au plan 15.

## 9. Étude de BO et planche A4 — 26 septembre 2026

Raphaël demande la prochaine planche et une étude des moyens de créer une BO cohérente, soignée, avec Outer Wilds, Celeste et Megabonk comme inspirations. Budget à comparer : 0–100 €, davantage si nécessaire ; aucun achat demandé. Niveau grand débutant, quelques essais FL Studio et Ableton, apprentissage de plusieurs mois non souhaité. Les titres précis et l’équilibre entre ces inspirations restent à préciser ; ils ne remplacent pas automatiquement la direction V2.

## 10. Choix A4 et outils de composition — 26 septembre 2026

Choix exprimés directement dans le chat : montée de niveau **B, Plus doux** (`level_up_a4_b`) et dégât joueur **A, Coup plus grave** (`player_hit_a4_a`). Neuf candidats retenus au total, sans nouvelle intégration ni recette. [Transcription du retour](../audio/README.md).

Raphaël attend des planches couvrant dix effets plutôt que seulement deux. A4 avait été limité par l’agent aux deux refus restants ; viser dix besoins dans les prochains lots lorsque possible. Pour la BO, priorité aux outils permettant de composer et s’amuser soi-même (mélodie, timbres et variantes), locaux/open source de préférence ; l’assistance est possible mais la génération de morceaux complets n’est pas le seul objectif.

## 11. Retour B1 — 26 septembre 2026

[Export et décisions](../audio/README.md) : neuf choix retenus, 18 au total. `step_wood` reste en attente et à retravailler : « non ca va pas il faut un bruit plus genre marcher sur des feuilles ». Prochaine planche de dix effets demandée ; correction feuilles en priorité. Aucun remplacement runtime implicitement validé par la préparation de cette planche.

## 12. Retour B2 — 26 septembre 2026

[Export original et notes exactes](../audio/README.md). Huit candidats retenus : feuilles B, gravier C, survol B, clic A, confirmation A, refus de perk B, interaction indisponible B et fouille A. Total : 26 candidats retenus, sans nouvelle intégration.

Sortie du level-up : « garder l'actuel imo mais baisser un peu le gain ». Champ exporté `pending` préservé, suivi **actuel conservé** ; essai local −3 dB préparé comme proposition de réglage. Échec d’événement : « aucun des trois là c'est juste des sons ultra aigus » ; `pending` préservé, suivi **à retravailler**. Éviter ces trois sons pour la prochaine recherche, explorer une matière moins aiguë sans déduire une nouvelle DA globale.

## 13. Retour B3 — 26 septembre 2026

[Export et notes exactes](../audio/README.md) : cloche A, chaîne A, lanterne B, horloge C, relique A et éclat B retenus ; **32 candidats retenus au total**. Cloche : « 1 mais aigus trop attenués » ; essai séparé avec passe-bas moins restrictif proposé, sans modifier le candidat ni le runtime. Échec d’événement et flash refusés. Lumière : « aucun ne va ils sont trop electroniques » ; aiguille : « aucun ne va pas ils sont trop electroniques ». Conserver les deux `pending` exportés et suivre ces besoins comme à retravailler. Recherche moins électronique pour ces deux effets, sans généraliser ce refus à toute la direction audio.

## 14. Retour B4 — 26 septembre 2026

[Choix et verbatim](../audio/README.md) : cloche B sans filtre, lumière B, aiguille B, arc B, arbalète B et Vide A retenus. 37 besoins avec candidat choisi. Flash en attente, sans décision de silence. Échec : « aucun n'est dans la da du jeu ». Fin d’esquive : « aucun chercher pus discret ». Coup majeur : « chercher un son qui fasse plus impact sur un corps mou / semi rigide ». Aucun choix de son ne vaut validation de sa cadence en combat ; l’arbitrage des armes silencieuses reste à faire en run.

## 15. Audio — retours B5, 26 septembre 2026

Raphaël retient la fin d’esquive B, la préparation et le départ du bond C, la charge C, le surgissement C, l’impact du Présage C et l’activation de POI A. Pour le coup majeur : « aucune ne va cherche autre chose ». Pour les pas dans l’eau : « garder l’actuel ». Ces deux entrées gardent leur champ `pending` original ; le suivi applique les notes explicites. Explosion : aucun candidat. Export complet : [retours B5](../audio/README.md). Aucun choix de silence supplémentaire ni intégration demandée.

## 16. Audio — retour B6 et textes des planches

Raphaël demande de retirer les slogans, introductions et consignes génériques des prochaines planches. Afficher les informations utiles à l’écoute et aux choix, avec la provenance accessible.

Sept choix B6 retenus : Présage B, tir ennemi A, Colosse C, tentacule B, rage C, mort du boss C, fouet A. Coup majeur : abandonner les anciennes contraintes et proposer trois nouveaux sons. Explosion : courte mais identifiable, B6 trop sec. Hurleur : plus impactant. Notes exactes et décisions exportées conservées dans [les retours B6](../audio/README.md).

## 17. Audio — intégration avant fin des sélections et nettoyage

Raphaël demande de brancher les choix déjà faits avant de finir les recherches, de retirer les sons inutilisés et de sortir les documents intermédiaires du repo. Les futurs lots restent dans l’archive externe indiquée au [plan audio](15-audio.md). Cinquante choix ont un usage actuel ; le coup du Colosse reste en archive sans réintroduire son ancien système. Les anciens sons et toutes les décisions sont sauvegardés avant retrait.


## 18. Corrections après le second audit — 28 septembre 2026

Raphaël : « okok. je te laisse commencer à travailler pour résoudre ces problemes en commencant par les plus critiques stp ». Début par le lot 6A du plan 10 (horloges et statuts à distance), puis les impacts continus et le préchauffage selon la priorité de l’audit. Autorisation d’implémentation, sans reprendre les changements de feu et d’équilibrage déjà en cours dans l’arbre.

## 19. Impacts continus — 28 septembre 2026

Après l’explication du cône, Raphaël : « okok. continue sur ca alors ». Lot 6B ouvert : réduire le travail de feedback du Transistor sans changer les dégâts, l’attribution ni la cadence des procs.

## 20. Priorité conjointe objets et perks — 28 septembre 2026

Raphaël refuse la verticale limitée à trois objets pour les coffres : « Pour moi le plus important à traiter et urgent c'est les objets et les perks (objets : stackables à l'infini , perks = 4 max dans l'inventaire) ».

Il valide ensuite (« oui ok parfait ») le recadrage : refonte conjointe du catalogue et des effets, quatre perks de spécialisation, objets indépendants cumulables, axes XP/chance/oubli, acquisition et présentation complètes. La première étape convenue est de présenter **la répartition concrète des effets et le catalogue avant implémentation**.

Le [catalogue commun du plan 05](05-catalogue-objets-perks.md) est préparé dans ce cadre. Ses 16 perks, dix niveaux proposés, 24 objets, noms, coefficients, conditions d'accès et retraits d'anciens effets ne sont pas implicitement validés par l'accord sur la préparation. Aucun changement de gameplay ni case de roadmap cochée à cette étape. Le chantier XP/réserve déjà en cours au plan 20 reste coordonné séparément.

## 21. Identité des objets/perks et proximité avec Megabonk — 28 septembre 2026

Raphaël : « certaines perks ou objets sont trop proches dans leur identité des armes du jeu » ; l'ensemble « 4 armes / 4 tomes (ici \"perks\") et objets illimité » lui paraît très proche de Megabonk. Il demande si Megabonk a inventé cette formule, si sa reprise est appropriée et s'il faut envisager les choses autrement.

Le [catalogue §11](05-catalogue-objets-perks.md#11-réexamen-de-lidentité-et-de-la-structure--28-septembre) consigne les chevauchements, les antériorités recherchées et trois directions à comparer. **Le catalogue reste non validé.** La préférence de l'agent pour des perks qui changent les règles de jeu plutôt que des statistiques répétées est une recommandation, pas une décision de Raphaël. Ni suppression des quatre slots ni nouvelle formule acquise ; aucune implémentation engagée.

## 22. Refonte de la mort — 28 septembre 2026

Raphaël : « la mort est à revoir. actuellement c'est un simple écran de fin game over il faudrait quelque chose de plus spectaculaire et qui fasse "vrai jeu". par exemple je pense à la mort dans megabonk avec une animation de mort et ensuite un écran de résumé avec toutes les states sur la run. »

Sur la proposition (séquence de mort en trois actes, bilan, données à ajouter, lots M1 à M5), il répond : « "la route s'efface" j'éviterai un truc aussi direct , le jeu est déjà assez explicite en soit. d'ailleurs c'est un truc que je changerai aussi au chargement initial de la partie (sur les textes). Du reste ca me va. et oui les deux dans le ton de la mort ca me va. pour les pages une seule plus dense je dirai. et ok pour la distance pq pas ».

Acquis :
- ton de la mort : impact brutal puis effacement, enchaînés ;
- pas de titre explicite au bilan ni de carte « type YOU DIED » ;
- bilan sur une seule page plus dense, sans page de détails ;
- distance parcourue comme chiffre mis en avant ;
- découpage [plan 02 lot D, seconde passe](02-juiciness-score.md) : M1 séquence, M2 données, M3 bilan dense, M4 gains animés.

Même retenue voulue pour les textes du chargement de run (`GameLoadingOverlay`, fragments « Le monde oublie ce qu'il était… ») : direction notée, réécriture non faite. Le rendu de M1 reste à juger en jeu.

Après la livraison de M1, Raphaël : « continue m2 ». M2 (données de run) est lancé ; l'échelle des distances (16 px par mètre) reste une proposition.

Après M2, Raphaël : « go m3 ». M3 (bilan dense) est lancé ; sa composition reste à juger en jeu.

Après M3, Raphaël : « go m4 ». M4 (gains animés) est lancé.

## 23. Approfondissement des trois formules objets/perks — 28 septembre 2026

Raphaël : « ok ces trois réflexions peuvent être intéressantes essaie de les pousser et d'aller au bout de ton idée stp ».

Demande acquise : développer les trois directions jusqu'à leurs conséquences concrètes, avant implémentation. Le [catalogue commun](05-catalogue-objets-perks.md), §12, compare acquisition, run, XP/Chance, identité des armes/personnages, saturation et coût de production. A conserve quatre perks de statistiques améliorables ; B propose quatre règles qualitatives sans niveaux ; C retire les perks et combine branches d'armes et choix de butin. Sept exemples de règles détaillent B, avec limites et critères de validation.

La recommandation de l'agent est B ; **Raphaël n'a encore choisi aucune formule**. Ni les paliers d'acquisition proposés, ni les sept exemples, ni la suppression des niveaux ou des emplacements ne sont validés. L'étude explicite le surplus plus précoce de B/C et les dépendances aux récompenses du monde. Aucun code de gameplay changé pour cette étude ; aucune case de roadmap cochée.

## 24. Direction B retenue et catalogue abouti — 28 septembre 2026

Raphaël : « la B est définitivement la plus intéressante. on essaie de faire une version définitive des nouvelles perks ? Pour le sujet des niveaux peut etre qu'il faut réhausser le niveau max des armes (à 99? ou 70?) ou accepter de retravailler la courbe des niveaux ».

**Acquis : direction B**, quatre perks qualitatifs sans niveaux, distincts des armes et des objets cumulables. Demande : définir le catalogue complet et étudier les plafonds d'armes 70/99 ainsi que la courbe. Les constats de non-choix aux §21–23 sont désormais historiques.

Le [catalogue de spécialisation](05-perks-specialisations.md) propose neuf règles avec acquisition, valeurs initiales, limites, interactions et lots. Seconde lecture devient le report d'une carte non choisie ; Traversée sort de cette version ; Propagation, Reprise et Habitude complètent les autres fiches. L'agent recommande d'essayer un plafond d'arme de 70 en conservant d'abord la courbe actuelle, puis d'ajuster d'après mesure ; comparaison chiffrée avec 99 et coûts XP plafonnés à 3 500/4 000/4 200.

**Non acquis :** les neuf effets, leurs noms/coefficient/paliers, le plafond 70 ou 99, une nouvelle courbe, l'implémentation. Aucun changement de gameplay ni case de roadmap cochée pour cette demande de conception. Le catalogue d'objets reste à réviser conjointement avant migration définitive.


## 25. Catalogue V1 validé, début de l’implémentation — 28 septembre 2026

Raphaël : « ok ca me va le cataloque pour une v1 en n'excluant pas de rajouter des perks plus tar. commence limplementation ».

**Acquis :** le catalogue V1 de neuf perks du [plan 05](05-perks-specialisations.md), extensible ultérieurement, et le démarrage de l’implémentation. Quatre perks qualitatifs équipés au maximum, une acquisition chacun, sans niveaux ni raretés. Les coefficients et le calendrier d’acquisition sont des valeurs initiales d’essai, pas un équilibrage validé par le jeu.

**Premier lot engagé : B0**, données, contrats, provenance du modèle XP et préparation de migration des anciens passifs vers les objets. Garder les anciennes sources jouables jusqu’à un remplacement effectif ; aucun perk inactif présenté au joueur. L’accord sur le catalogue ne tranche pas explicitement entre armes à 70, à 99 ou révision de courbe : B0 conserve le plafond et la courbe actuels.


Livraison de B0 : données et loader extensibles, contrats de dégâts/soins/statuts intégrés, modèle XP corrigé par provenance, migration des anciennes statistiques documentée. Build sans avertissement ; régressions contrats (32), armes (13), capacités ennemies (55), modèle Python (6) et smoke 600 frames verts. Seule la case B0 est cochée ; les neuf nouveaux effets, leur acquisition et la migration d’objets restent à intégrer. Aucun arbitrage de plafond d’armes ni changement de courbe déduit de cette livraison. [Compte rendu](05-perks-specialisations.md#11-compte-rendu-b0--socle-livré-et-vérifié-le-28-septembre-2026).


## 26. B1 validé, passage à B2 — 29 septembre 2026

Raphaël, après la livraison de B1 (acquisition en sommeil tant qu'aucun effet n'est branché) : « je valide et passe a B2 ».

**Acquis :** B1 tel que livré, y compris le report d'un perk passé à chaque niveau suivant jusqu'à ce qu'un perk soit pris (§2 du plan 05). **Engagé :** lot B2, effets de combat et de survie (Prévoyance, Reprise, Débordement, Convergence, Propagation) et leurs retours visuels, découpés au [plan 05 §8.3](05-perks-specialisations.md). Les coefficients restent des valeurs d'essai.

Livraison de B2 : les cinq effets sont actifs en run et donc proposés aux paliers. Leurs coefficients restent les valeurs d'essai des fiches. **À trancher par Raphaël :** avec la Cloche seule, Propagation ne se déclenche presque jamais (le ralentissement de 2 s expire avant le coup fatal), voir [plan 05 §13](05-perks-specialisations.md#13-compte-rendu-b2--effets-de-combat-et-de-survie-livrés-le-29-septembre-2026). Aucun coefficient n'a été modifié.


## 27. B2 validé, Propagation laissée à l'agent, perks manquants — 29 septembre 2026

Raphaël, après B2 : « Ok top. et tu as créé les sprites de ces perks. pour propagation jsp moi fais comme tu veux apres la cloche est juste une arme sur les 24. ca dit quoi pour les autres. et fais les perks manquantes ».

**Acquis :** B2 tel que livré. Le réglage de Propagation est laissé au jugement de l'agent, à éclairer par les autres armes de contrôle et pas par la seule Cloche. **Demandé :** implémenter les perks restants (Sillage, Seconde lecture, Délestage, Habitude). Question sur les sprites des perks : aucun n'a été créé en B1/B2, les cartes de perk n'ont pas d'icône.

Suite donnée : la règle de Propagation est conservée après mesure sur Cloche, Polaroïd et Chronomètre (0, 9 et 10 éliminations transmissibles sur 14, 9 et 13) ; la Cloche seule échoue, c'est un réglage d'arme. Sillage et Seconde lecture sont livrés. Délestage et Habitude ne le sont pas, faute d'objets et de récompense à choix ; leurs prérequis sont listés au [plan 05 §14](05-perks-specialisations.md#14-compte-rendu-b3-partie-sans-objets--sillage-et-seconde-lecture-29-septembre-2026). Aucune icône de perk n'a été créée : direction à valider.


## 28. Objets à réviser et à créer, récompense à choix hors coffres, planche d'icônes — 29 septembre 2026

Raphaël : « ouais en effet pour le catalogue d'objets il va falloir réviser la liste et les créer. une proposition récente a été fait mais elle reste à retravailler. c'est le moment de faire ca. récompense à choix : à déterminer, pour moi en dehors des coffres dans un truc à part. à déterminer.. 3 oui il me faut une planche de proposition ».

**Acquis :** le catalogue d'objets est le chantier courant. La proposition récente est à retravailler, puis les objets seront créés. La récompense à choix d'objets est **distincte des coffres**, dans une source à part dont la forme reste à déterminer. Une planche de proposition d'icônes de perks est demandée. **Non acquis :** la forme de cette source, le contenu du catalogue révisé, le style des icônes.


## 29. Retours sur le catalogue d'objets V1 et la planche de perks — 29 septembre 2026

Raphaël : « pour les icons de perks je prefer e par famille juste je trouve que certains comme la propagation, la prévoyange le sillage - en vrai presque tous - ne sont pas assez "reconnaissables". Pour les objets c'est pas mal apres le truc qui me gene c'est que les objets sont quasi tous des multiplicateurs. il faudrait plus de diversité et d'originalité. ca peut pas etre que des multiplicateurs meme si c'est bien d'en avoir. avoir mis plusieurs difficultés c'est bien. je te laisse etre créatif sur ce que pourraient faire les objets regarde du coté de megabonk notamment (à ne pas copier telle quelle). aussi pour info ca c'est important mais les objets et armes ne sont pas tous débloqués par défaut il y aura un nombre limité de dispos et le reste accessible via des quetes / achats (si on a un systeme de monnaie persistante). Ah et en vrai les perks sous forme de pins j'aime pas trop, je verrai une autre forme du type des fragments par exemple ».

**Acquis :**
- Icônes de perks : couleur **par famille** ; pas de pin's, une autre forme, par exemple des **fragments** ; motifs à rendre bien plus reconnaissables.
- Objets : garder quelques multiplicateurs et les **raretés** (« plusieurs difficultés »), mais la majorité doit apporter des effets **divers et originaux**. S'inspirer de Megabonk sans copier.
- **Armes et objets ne sont pas tous débloqués par défaut** : une réserve de départ limitée, le reste par quêtes ou achats (monnaie persistante, si elle existe).

**Non acquis :** la liste d'objets, la source à choix, la forme exacte des fragments.


## 30. Bannir = oublier, icônes 2, objets peut-être limités et intégrés au level-up, perks renommés « fragments » — 29 septembre 2026

Raphaël : « idée : le fait de bannir des armes / perks / items dans les choix est un "oubli" et contribue à ce que l'oubli général augmente. La proposition deux des icons des perks : mieux. en vrai à la réflexion je me demande s'il ne faudrait pas limiter les objets aussi. tu en penses quoi ? et limite les intégrer avec le level up... oui je sais ca bouscule tout mais en y réflechissant les anciennes perks étaient super bien par exemple les projectiles en plus ect... donc ouais faudrait peut etre revoir comment les objets fonctionnet et les limiter (soit à 4 ou bien à genre 8 ou 12). je me demande vraiment car sinon ca sera plus dur de les stacker. et par contre limiter les "perks" qu'on peut appeler fragments maintenents toujours à 4. »

**Acquis :**
- icônes de perks, proposition 2 (fragments teintés par famille) : meilleure direction ;
- les perks restent **limités à quatre** et peuvent s'appeler **« fragments »**.

**En réflexion, rien de décidé :**
- objets limités (4, 8 ou 12 emplacements) et intégrés au level-up, sur le modèle des anciens passifs (projectile supplémentaire, etc.) ;
- bannir une arme, un fragment ou un objet compterait comme un oubli qui nourrit l'oubli général.

L'avis de l'agent est consigné au [catalogue d'objets §7](05-objets-catalogue-v1.md#7-remise-en-question-du-29-septembre--objets-limités-et-intégrés-au-level-up).


## 31. Refonte du build depuis un point de vue neutre, douze principes — 29 septembre 2026

Raphaël : « ok juste avant de toute valider essaie de prendre un point de vue neutre et en dehors de toutes mes indications et meme de tes proprees pensées. dit toi qu'on te drop sur vestiges, avec le lore ect mais c'est à toi de construire la manière dont le build se fait. tu peux implémenter ca comme tu veux tu es libre (armes, perks, objets, autre chose). A partir de maintenant au lieu de se baser sur le hasard ou des intuitions on va s'efforcer de respecter les règles suivantes meme si ca implique des réflexions et chantiers sur ce qui était prévu initialement voir déjà implémenté. il faut savoir etre flexible le jeu est encore dans une phase relativement jeune de son développement », suivi des douze principes consignés dans [PRINCIPES-BUILD](../PRINCIPES-BUILD.md).

**Acquis :**
- les douze principes font désormais référence pour toute conception de build ;
- le système de build est à reconcevoir sans présupposé, en remettant en cause si besoin ce qui était prévu ou déjà implémenté (fragments B, objets, passifs, raretés d'amélioration) ;
- l'agent est libre sur la forme (armes, fragments, objets, autre chose).

**Non acquis :** la proposition qui en découlera, à présenter avant validation.


## 32. Arbitrages sur le plan 21 — 29 septembre 2026

Raphaël : « je garderai le niveau 50 pour les armes (ou en tout cas, plus que 10). et je garderai aussi le fait qu'augmenter une arme se fasse par le level up aléatoire de stats. c'est plus stimulant. (en sachant qu'avec plus de chance ca augmente les augments rares et qu'une amélioration d'arme peut contenir plusieurs stats. il faut ptet un item qui augmente le nombre de stat augmenté par amélioration (item légendaire mais copie du marteau de megabonkà). par contre pourquoi pas rajouter un systeme quand on passe level max d'une arme qui propose deux "voies" ou améliorations finales, ca c'est pas mal du tout. Les passifs/traits ok mais je ne comprend pas trop comment on les obtient ni s'ils sont réellement nécessaires. Pour les fragments ca me va de changer la source d'offre tres bonne idée. pour les objets ca me va d'en avoir 6 par contre je mettrai pas level 3 max mais genre level 30 ou 50. Top sinon les propositions pour les personnages ca me va et ca manquait clairemenet. Limiter les relances ca me va aussi. et pour les banissements ce que je ferai c'est en avoir des gratuits et au fur et à mesure ca commence à couter du péril et de plus en plus. Viser 300 à 400 niveaux n'est pas grave du tout et ca ne diluera pas la perception des gains. déjà car ca restera visible en early game et en mid / late game il y aura des mécaniques qui feront que l'xp pourra s'accumuler en gros on pourra monter de plusiuers niveaux à la fois. pour ta liste de "plusiuers chemins" ok si c'est juste un petit apercu mais attention à ne pas limiter le jeu selon une vision trop réduite de chemin possible il y en aura des dizaines ou des centaines. »

**Acquis :**
- **Armes :** niveau max 50 (au moins bien plus que 10). Amélioration par tirage aléatoire de stats au level-up, avec rareté (la Chance favorise les rares) et plusieurs stats possibles par amélioration.
- **Voies finales :** au niveau max, deux voies ou améliorations finales au choix.
- **Idée d'objet légendaire** qui augmente le nombre de stats par amélioration d'arme (inspiré du marteau de Megabonk, à rendre propre à Vestiges).
- **Fragments :** offerts après les Résurgences au lieu des niveaux 2/6/12/20.
- **Objets :** 6 emplacements, niveaux jusqu'à 30 ou 50.
- **Personnages :** affinités et orientation du plan 21 §8.
- **Relances :** limitées.
- **Bannissements :** quelques-uns gratuits, puis un coût en Péril croissant.
- **Courbe :** 300 à 400 niveaux conservés ; la montée de plusieurs niveaux à la fois en mi-partie et en fin de partie est voulue.
- **Chemins :** la liste d'archétypes n'est qu'un aperçu ; le jeu doit permettre des dizaines ou des centaines de chemins.

**Question ouverte :** les traits (comment on les obtient, sont-ils nécessaires ?).


## 33. Traits supprimés, objets 6 × 50, nom des fragments à trouver, Megabonk comme référence — 30 septembre 2026

Raphaël : « oui pour les fragments ceux du plan 21 il faudrait trouver un autre nom je te laise me proposer qq chose. Ca me va de supprimer les traits et de ne garder que les objets comme ca ca sera plus clair. Et c'est bien d'avoir 6 objets seulement (à affiner au besoin de chiffre) et jusqu'au niveau 50. bon ok avec du recul ma problématique est la suivante : mégabonk est une grosse inspiration et je trouve ses mécaniques et sa construction parfait. je voudrais un jeu qui s'approche de cette "perfection", mais j'ai trop tendance à vouloir le cloner. si je dis ca c'est car mégabonk est construit de la manière suivante : 4 armes avec 40 niveaux chacune. les armes ont la meme rareté. les améliorations d'armes ont des raretés et se font sur une state aléatoire de l'arme (ou plusieurs) et plus la rareté est importante, plus les stats de l'amélioration. apres il y a les 4 tomes jusqu'au niveu 99 de mémoire et les objets sans limite et qu'on peut stacker tout du long de la partie. bref c'est vraiment parfait quoi. et en plus il y a plein de trucs partout sur la map différents pois ou micro rewards bref ca favorise vraiment l'exploration et de ratisser la map ce qui nous manque encore. »

**Acquis :**
- **Traits supprimés :** les objets portent aussi les statistiques.
- **Objets :** 6 emplacements (chiffre à affiner), jusqu'au niveau 50.
- **Nom :** les ex-perks du plan 21 doivent prendre un autre nom que « fragments » ; proposition demandée à l'agent.

**Constat de Raphaël, à traiter :** Megabonk est la référence de qualité, avec le risque de le cloner. Ce qui manque le plus à Vestiges par rapport à lui : une carte pleine de points d'intérêt et de petites récompenses, qui pousse à explorer et à ratisser.


## 34. Carte à ratisser : plan à ouvrir, densité mesurée, intérêt long terme hors objets — 30 septembre 2026

Raphaël : « alors la carte à ratisser c'est bien et ouais ca me va d'ouvrir un plan (seulement ca fait 22 plans déjà... ca commence à faire beaucoup et j'ai peur que des idées de perdent de l'un à l'autre ou que le retard s'accumule. mais bon en l'occurence c'est important là). je pense qu'en terme de quantité on ne doit pas etre au meme niveau que megabonk car si la map fourmille trop de POIs partout on perd en immersion et on se rapproche trop de megabonk dans sa dynamique mais il faut quand meme revoir à la hausse le nombre de lieux et récompenses tout en veillant à bien les intégrer au lore quand cest possibles. Les 6 objets max moi ca me va. et ducoup peut etre que pour se distinguer de megabonk on peut garder un intéret sur le long terme à explorer et continuer de joueur par autre chose que l'accumulation d'objets. ca pourrait etre lié aux essences , aux améliorations d'armes ou à autre chose de pas encore créé. »

**Acquis :**
- ouvrir le plan de la carte à explorer ;
- plus de lieux et de récompenses qu'aujourd'hui, mais **moins dense que Megabonk**, pour préserver l'immersion ;
- lieux intégrés au lore quand c'est possible ;
- **6 objets maximum**, confirmé ;
- l'intérêt d'explorer sur la durée doit venir **d'autre chose que l'accumulation d'objets** : Essence, améliorations d'armes, ou un système à créer.

**Inquiétude à traiter :** trop de plans (22), risque d'idées perdues et de retard accumulé.
**En attente :** le nom des ex-perks (Réminiscences, Certitudes ou Ancres).


## 35. Carte agrandie, plus de lieux, Mémorial confirmé, minimap, Réminiscences, et une référence unique du système de jeu — 30 septembre 2026

Raphaël : « je pense que de 1 la map peut etre un peu agrandie surtout verticalement et de deux il faut augmenter un peu la proportions de poi visés. aussi j'aime bien le principe d'hotel (pas le vrai nom) à activer qui protège la zone de l'effacement et donne un bonus. j'espère qu'il est consigné quelque part. la minimap bonne idée. Pour les ex perks ca me va réminiscences on garde ca. Allez go. Et est ce que pour de bon on peut enfin fixer le systeme du jeu (armes perks objects ect ect avec les détails) et garder une seule version de ca qq part dans un des plans et commencer à l'appliquer rapidement ?? c'est limite le plus important du jeu enfait. »

**Acquis :**
- **Carte :** à agrandir un peu, surtout en hauteur.
- **Lieux :** viser un peu plus de lieux que les 70 proposés au plan 22.
- **Mémorial :** le principe de l'autel à activer, qui protège sa zone de l'Effacement et donne un bonus, plaît. Il existe déjà en jeu (plan 17, vague 3) et reste la pièce centrale des lieux de choix.
- **Minimap :** oui.
- **Nom :** les ex-perks s'appellent les **Réminiscences**.
- **Plan 22 :** feu vert.
- **Référence unique :** fixer le système de jeu (armes, Réminiscences, objets, détails) en **une seule version**, dans un seul plan, et commencer à l'appliquer vite. C'est la priorité.

Suite donnée : le plan 21 devient la référence unique du système de build ; son historique part en annexe.


## 36. Level-up pas assez attrayant, cartes illisibles, coffres à revoir — 30 septembre 2026

Raphaël : « le level up est pas assez fun/attrayant car les valeurs de level up sont trop petites (0.05 de regen ect), avant il y avait des trucs genre +1 projectile (par arme), +1 transpercer... il faut reprendre plutot ce genre de valeurs (apres oui la regen est lente ok mais à ce point ?) Bref fait moi un inventaire des objets et arme. et aussi le menu de level up et chiant et dur à comprendre il peut y avoir 5 /6 lignes en tout avec le statut (nouveau) le nom de l'arme les stats ect bref genre peu lisible. [capture de l'écran de level-up de Megabonk] regarde ce que fait megabonk en comparaison. et memes les coffres sont chiant on choppe des essences et des armes le loot est à revoir imo. Et redit moi aussi la liste des stats du joueur au fait. »

**Acquis :**
- Les gains d'un niveau sont trop petits pour être sentis (Bobine de fil : +0,06 PV/s par niveau). Il faut revenir à des gains francs du type « +1 projectile par arme », « +1 perforation ».
- L'écran de level-up est trop chargé : jusqu'à 5 ou 6 lignes par carte (statut, nom, stats, propriété, armes concernées, palier). Référence : Megabonk, une carte = rareté, nom, niveau, une ligne de gain, avec l'inventaire et les stats du joueur affichés à côté.
- Le butin des coffres (Essence, armes) est ennuyeux, à revoir.
- Demandé : l'inventaire des objets et des armes, la liste des stats du joueur ([21-inventaire.md](21-inventaire.md)).

**Non acquis :** la nouvelle échelle des objets (nombre de niveaux, valeur d'un niveau), la forme des cartes, le nouveau butin des coffres. Propositions de l'agent à valider.


## 37. Projectiles en plus au lieu des copies, plus de bouclier de départ, gros gains, objets à 30 niveaux, cartes validées — 30 septembre 2026

Raphaël : « copie d'attaque j'aime pas remet juste projectile supplémentaire. ah et aussi retire le bouclier par défaut sur tous les personnages. et réduit la durée d'invicibilité stp. Ah et pour les gains trop petit c'est pas grave que le total au niveau 50 te paraisse déraisonnable. le jeu et la difficulté / puissance des ennemis devra augmenter en conséqunence. Ah et pour les entiers en gros un truc : c'est bien d'avoir des décimaux mais 1 n'est pas le max. par exemple pour les projectiles ca peut aller de 0.5 (par exemple pas valeur finale) à 3 environ (augment légendaire). Je valide ta proposition des cartes. Pour les niveaux des objets non pas 10 niveaux je veux plus. ok si tu veux pas 50 on essaie 30 d'abord. Encore une fois c'est ok si les stats atteingnent des valeurs qui te paraissent énorme. Et ducoup si je comprends il manque aussi des objets et autres à créer donc il va falloir le faire. et la map fait encore vide c'était un autre plan mais il est lié à celui ci. Bref consigne mes retours et prépare moi un plan je vais faire tourner un agent en cloud. »

**Acquis :**
- **Copies d'attaque supprimées :** l'objet donne des projectiles supplémentaires, à pleins dégâts.
- **Bouclier de départ retiré** sur les trois personnages. Le bouclier ne vient plus que des objets (Écusson de pompier).
- **Invulnérabilité après un coup réduite.** Elle vaut 0,5 s aujourd'hui ; valeur de départ proposée 0,25 s.
- **Gains francs :** les totaux peuvent devenir très grands (façon Megabonk). La difficulté et la puissance des ennemis montent en conséquence.
- **Stats entières fractionnaires :** projectiles, perforation, sauts, orbes montent par fractions. Exemple : de +0,5 projectile (commune) à environ +3 (légendaire). 1 n'est pas le maximum d'une amélioration.
- **Cartes de niveau :** proposition validée. Forme Megabonk (rareté, nom, niveau, une ligne de gain en valeur, deux au plus), sans propriété, sans « Pour : », sans texte de palier ; inventaire et stats affichés à côté pendant le choix.
- **Objets à 30 niveaux** pour commencer (ni 10 ni 50), chaque niveau étant un gain franc.
- **Contenu manquant à créer :** objets restants, et une carte qui fait encore vide (plan 22, lié au plan 21).
- **Plan pour un agent en cloud** demandé : [23-plan-agent.md](23-plan-agent.md).

**Non acquis :**
- la nouvelle forme des coffres (proposition §36 : un choix d'une amélioration parmi trois, de rareté garantie) ;
- la valeur exacte de l'invulnérabilité ;
- les chiffres des objets à 30 niveaux, qui sont des valeurs de départ ;
- l'Atelier et les voies d'ascension des 20 autres armes, toujours en attente.


## 38. Coffres : Essence gardée, bonus d'une stat au hasard ; Porte-monnaie et Repères laissés à l'agent ; travail mis sur main — 1er octobre 2026

Raphaël, après le compte rendu des lots R0 à R7 du plan 23 : « Un coffre peut donner de l'essence ca me va. il doit aussi donner un bonus d'une stat au hasard. Porte monnai usé : fais comme tu veux. reperes : fait comme tu veux. Et ps met ce que tu as fait sur main stp »

**Acquis :**
- **Coffres :** l'Essence reste dans le butin. Chaque coffre donne **en plus un bonus d'une stat au hasard**. La proposition du §36 (choix d'une amélioration parmi trois) n'est pas retenue sous cette forme ; R8 du plan 23 devient ce bonus.
- **Porte-monnaie usé** (Essence rendue et quête « Accumuler de l'Essence ») : laissé au choix de l'agent.
- **Repères** (plan 22 §3 B, question 5 du §11) : laissés au choix de l'agent.
- Le travail des lots R0 à R7 va sur `main`.

**Non acquis :** la taille du bonus de stat et la liste des stats tirées, valeurs de départ de l'agent.


## 39. Partie du 1er octobre : écran trop chargé, début de run trop dur, écrans pas assez vivants, objets sans icônes — 1er octobre 2026

Raphaël, après une partie jouée jusqu'un peu après la première Résurgence (retours dictés, extraits) : « les armes sont mieux présentées comme ça » ; « l'écran de chargement […] c'est juste un écran noir qui affiche des textes » ; « il commence à y avoir trop de choses sur l'écran » ; quêtes de run « toujours visibles […] soit ajouter une option pour les afficher ou les masquer […] ça fait pas vraiment jeu » ; barre du haut : « je ne pense pas qu'il y ait besoin de montrer une barre de progression globale. Par contre, afficher le temps, ça, c'est une bonne idée » ; « le score […] augmente en permanence, même si je ne tue personne […] il faudrait afficher le nombre de kills » ; « le jeu est peut-être devenu trop compliqué […] pas assez de régénération, en particulier pendant la première des résurgences […] j'avais une seule arme à peine […] les monstres étaient vraiment très puissants » ; « quand on monte de niveau, on n'obtient pas assez facilement d'autres armes » ; Mémorial : « c'est une bonne mécanique » mais l'écran « apparaît comme ça d'un coup […] il faut que ça soit vraiment beaucoup plus juicy », « la zone se souvient, je ne sais pas s'il faut le garder » ; « il faut qu'un peu tout suive la direction artistique du jeu […] pixelisé, un peu en mode oubli » ; le fond qui tourne à l'ouverture de coffre « n'est pas pixelisé » ; raretés : « c'est pas des vraies icônes dédiées, c'est des sortes de caractères ASCII » ; « les coffres, il faut pas qu'ils droppent des armes » ; flèches des coffres « visibles de trop loin » ; minimap : des carrés de couleurs « qui semblent correspondre à rien », « elle devrait être un zoom sur la map » ; Repères « pourraient être un peu plus intéressants » ; annonces de Résurgence, horde, micro-événements : « on pourrait peut-être s'en passer » ; Chance : « j'ai un peu de mal à voir si elle s'applique vraiment » ; stats au level-up : « juste un tiret […] pour la vitesse, pour les dégâts, pour la zone et pour la durée » ; armes de mêlée : « une portée un peu trop petite, surtout certaines » ; « on manque d'outils […] pour avoir des boucliers temporaires, soit avoir de la régénération […] les ennemis puissants ou certains petits loot pourraient donner des petits bonus : un bonus qui attire l'XP, un bonus qui redonne de la vie » ; « le design final des armes a été validé […] pour les objets, ils n'ont pas du tout été redesignés » ; « il faut pas que les objets indiquent d'eux-mêmes dans quelle synergie ils offrent avec quoi ». Demandé : un plan, et où en sont les mécaniques principales.

**Acquis :**
- **Coffres :** plus d'armes dans leur butin.
- **Écran de jeu à alléger :** quêtes de run masquables ou plus discrètes, et dans une forme « de jeu » ; plus de barre de progression globale en haut ; **le temps reste affiché** ; annonces d'événements à supprimer ou presque.
- **Kills affichés** en run.
- **Écrans de choix** (Mémorial, coffre) plus animés et dans la DA pixel « oubli » ; **icônes de rareté dédiées**.
- **Début de run plus tenable :** plus d'armes au level-up, plus de moyens de survie.
- **Idée retenue sur le principe :** des bonus temporaires lâchés par les ennemis puissants ou de petits butins (aimant d'XP, soin…).
- **Objets** à redessiner, comme les armes.
- **Pas de synergies explicitées** par les objets.

**Non acquis (propositions du [plan 24](24-retours-du-1er-octobre.md), questions Q1 à Q9) :** forme des quêtes repliées, score de distance au lieu du temps, liste et taux des bonus lâchés, gain des Repères, portées de mêlée et régénération chiffrées, ordre des lots.


## 40. Plan 24 validé, score aux seules éliminations, ascensions de toutes les armes, barre d'XP en bas — 1er octobre 2026

Raphaël, après le plan 24 : « je valide les recommendations. et pour le score il ne devrait etre compté qu'en kills. par besoin d'un autre score. Reperes : à toi de voir. Armes des ennemis : ne pas en garder le probleme étant qu'on obtient rapidement les 4 armes donc apres ca devient useless. Pour le reste des armes fait en sorte qu'elles aient toutes leur ascension stp. Ah et la barre d'XP doit prendre toute une largeur d'écran place la en bas de l'écran stp. et fais en sorte quelle soit vraiment plus joli. A part ca je valide toutes tes propositions. Prépare un prompt/plan global pour avancer sur les sprites (faire tous les objets, revoir certains projectiles ennemes pour les rendres plus joli/impactants; et les petits détails comme les raretés et les menus. »

**Acquis :**
- **Toutes les recommandations du plan 24 §8 et toutes ses propositions** sont validées : bonus lâchés (cinq bonus, élites à 35 %), une ligne d'aide la première fois de chaque micro-événement, surbrillance des synergies retirée, quêtes repliées par défaut, objets : icônes d'abord, ordre des lots du §7.
- **Score : les éliminations seulement.** Ni temps, ni distance, ni lieux, ni coffres, ni Résurgences. Pas d'autre score.
- **Repères :** laissés à l'agent. Choix retenu : un petit gain permanent lié au lieu (plan 24 §5 D3).
- **Armes lâchées par les ennemis : supprimées**, élites comme Souverain. Les quatre emplacements se remplissent vite ; une arme au sol devient alors inutile.
- **Ascensions : toutes les armes** en reçoivent une. Les voies proposées pour les 20 autres armes ([21-historique §28](21-historique.md)) sont validées par cette demande.
- **Barre d'XP :** sur toute la largeur de l'écran, en bas, et beaucoup plus jolie.
- **Sprites et design :** un plan global est demandé pour un agent (tous les objets, projectiles ennemis plus beaux et plus percutants, raretés, menus) : [plan 25](25-sprites-et-design.md). L'agent courant travaille sur le reste.

## 41. Brancher toutes les images et l'UI du plan 25 sur main — 1er octobre 2026

Après fusion du plan 25 et explication des branchements encore en attente,
Raphaël : « bah branche tout alors stp (retourne sur main et fais les changements pour tout brancher) ».

**Acquis :** intégrer les planches proposées, les images et les éléments d'UI
du plan 25 directement sur `main`, sans nouvel arrêt de validation graphique.
Les composants du plan 24 maintenant disponibles sont réunis avec les sprites
pour brancher également bonus, sceaux, écrans animés et chargement.

## 42. Fond d'ouverture de coffre saccadé — 1er octobre 2026

Raphaël : « l'animation quand un coffre s'ouvre en fond là elle est a 5 fps
elle donne la gerbe c'est pas possible ». Corriger le mouvement du fond ;
ce retour invalide la cadence et les sauts de rotation livrés au plan 25 I4.

## 43. Garder la rotation du fond de coffre — 1er octobre 2026

Raphaël : « non pas fixe ca doit tourner mais a une bonne vitesse c'est trop
dur sérieux ». Le correctif I6 a mal interprété le retour : conserver une
rotation fluide, à vitesse modérée, au lieu de figer les rayons.

## 44. Recette après intégration des sprites : combat, interfaces et audio — 1er octobre 2026

Raphaël juge le résultat global très bon, notamment le travail sur les sprites. Il demande de **mettre ses retours à l'écrit, mettre à jour les plans et préparer un court prompt de continuation**, sans lancer leur implémentation dans cette session.

**Retours reformulés :**

- **Personnages :** améliorer le sprite du Traqueur en priorité, ainsi que ceux des deux autres personnages déjà jouables, le Vagabond et la Forgeuse.
- **Tirs ennemis :** trop d'ennemis semblent lancer des projectiles qui traversent l'écran ou la carte. Raphaël en perçoit deux ou trois types ; ce nombre n'est pas un inventaire vérifié. L'esquive permanente gêne le combat contre une foule et prend trop de place. L'esquive reste souhaitée, d'autres attaques la font déjà bien fonctionner.
- **Pause :** retirer les descriptions des armes dans l'équipement, « Le monde se fige, mais ta mémoire reste éveillée. », « HALTE DANS LE VIDE » et l'explication sous Chance (« Monte la rareté des améliorations et des bénédictions. »). Revoir plus largement la présentation du menu, jugée insuffisamment travaillée.
- **Survol des boutons :** les lignes et couches de contours superposées déplaisent ; revoir cet effet.
- **Coffres :** les récompenses comme l'Essence, les dégâts critiques ou les PV supplémentaires semblent partager la même icône. Leur nature doit être identifiable par des icônes distinctes.
- **Résurgences :** leur présence se comprend mal malgré les effets existants. Renforcer leur identité visuelle et/ou sonore, notamment par la bande-son.
- **Audio :** effets sonores et musique demandent un travail important ; Raphaël y voit le prochain gros chantier.
- **Carte :** jugée trop pixellisée, trop grise et peu soignée. Le retour ne distingue pas explicitement minimap, carte agrandie et terrain du monde ; ce périmètre reste à identifier avant une refonte du terrain.

**Rattachement :** synthèse, lots proposés et prompt au [plan 24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026) ; interfaces au plan 04, pression des ennemis au 07, personnages au 08, audio et Résurgences au 15, compléments graphiques au 25. Les solutions et l'ordre des lots sont des propositions de travail, pas des choix détaillés validés par ce retour.

**Précisions sur les décisions antérieures :** la demande retire l'aide de Chance prévue au plan 24 D2 et validée en §40. Le souhait de menaces à distance variées du 23 septembre reste un historique, à rééquilibrer au vu de la saturation ressentie aujourd'hui. Le retour sur les Résurgences ne demande pas le rétablissement des annonces textuelles retirées au plan 24 A3. La rotation fluide des fonds (§43) reste acquise.

## 45. Reprise seul et écran des captures — 1er octobre 2026

Raphaël demande d'exécuter seul les retours, un lot à la fois : pause et survols,
puis icônes de coffre ; examiner ensuite les tirs ennemis et préparer l'audio,
en commençant par les Résurgences. Les trois personnages jouables et la carte
restent au programme. Préserver la rotation fluide, vérifier le visuel par
captures et tout changement audio par écoute réelle, puis actualiser les plans.

Pendant ce travail : « évite de lancer le jeu ou alors fait le sur l'écran
viewsonic […] pas sur l'écran msi ». Captures et fenêtres de test uniquement
sur le ViewSonic (VX2758, DP-1 ; second écran dans la configuration constatée),
avec `VESTIGES_SCREEN=1`. Les contrôles sans rendu restent headless.

## 46. Avancer seul sur l'audio — 1er octobre 2026

Raphaël : « Avance le plus que tu peux sur la partie audio du projet, en autonomie, en suivant les plans. » Travail mené sur le plan 15 : A0 (référence enregistrée), A1 (pilotage de la musique) et propositions A2 préparées. Aucun choix artistique n'est tranché par l'agent : points d'entrée des morceaux, rôle exploration/combat et accent de début de crise attendent l'écoute de Raphaël ([plan 15](15-audio.md#a2--premières-propositions-à-choisir-à-loreille)).

## 47. Continuation autonome hors audio — 1er octobre 2026

Raphaël autorise à continuer en autonomie et à avancer autant que possible en
conservant une qualité maximale. Il exclut explicitement l'audio de cette
continuation. Les travaux audio déjà présents dans son checkout sont préservés.
Le travail reste seul, par lots vérifiés ; les fenêtres restent sur ViewSonic.

## 48. Projectiles trop rares, vol de vie, level-up rapide, builds XP/Chance, sprites d'armes adaptatifs — 1er octobre 2026

Raphaël, après une partie : « globalement multiplier le nombre de projectiles est trop dur (il faudrait que toutes les armes aient une option pour level up cette stat et elle apparait trop lentement tout comme l'objet de projectiles. il manque une stat de vol de vie. le level up est peut etre un peu trop rapide surtout quand on le build pas l'XP / la luck. il peut etre dur de faire un build autour de l'XP/la luck car il me semble qu'il manque d'objets explicitement luck/xp (ou alors je ne les ai pas vu sur ma run c'est probable). il faut que les sprites des armes soient adaptatifs (si la taille de l'arme ou le nombre de projectile augmentent) ». Il demande de consigner seulement, sans implémenter.

**Retours reformulés :**

- **Projectiles :** en multiplier le nombre est trop difficile. Toutes les armes doivent pouvoir gagner des projectiles en montant de niveau. Cette stat apparaît trop lentement dans les offres, comme l'objet qui donne des projectiles.
- **Vol de vie :** la stat manque.
- **Level-up :** peut-être un peu trop rapide, surtout sans build tourné vers l'XP ou la Chance. Formulé comme un doute : à mesurer avant de toucher la courbe.
- **Builds XP/Chance :** difficiles à construire ; il semble manquer d'objets explicitement orientés Chance ou XP. Raphaël admet qu'il ne les a peut-être simplement pas vus dans sa run : vérifier le catalogue et leur fréquence d'apparition avant d'en ajouter.
- **Sprites d'armes :** ils doivent s'adapter quand la taille de l'arme ou son nombre de projectiles augmente.

**Rattachement :** projectiles, vol de vie, level-up et builds XP/Chance au [plan 21](21-systeme-de-jeu.md), courbe d'XP en appui du [plan 20](20-recompense-et-puissance.md) ; sprites d'armes adaptatifs aux plans [08](08-direction-artistique.md) et [25](25-sprites-et-design.md). Rien n'est encore décidé sur les solutions : le prochain agent mesure, propose des lots et pose les questions du [tableau de bord §2](TABLEAU-DE-BORD.md#2-décisions-attendues-de-raphaël).

## 49. Réponses aux questions de reprise — 2 octobre 2026

Questions posées à la reprise (tableau de bord §2 et §48), avec les constats mesurés dans les données : seules 6 armes sur 24 peuvent tirer « projectiles » au niveau, à environ 6 % des tirages de stat ; un seul objet de Chance (Jeton de fête, +0,03), un seul d'XP (Photo de classe, +5 %) ; aucun vol de vie ; les 31 objets proposés ont tous le même poids d'offre.

- **Projectiles :** les 24 armes peuvent gagner leur « nombre » au niveau (projectiles, frappes en mêlée, orbes, cibles de chaîne), à un poids relevé (environ 13 % des tirages), et le Papier carbone (`souffle_du_neant`) est favorisé dans les offres.
- **Vol de vie :** un nouvel objet, et la stat entre au tirage des bonus de coffre. Soin plafonné par seconde pour éviter l'immortalité en foule.
- **Builds XP/Chance :** renforcer les objets existants (pas plus forts), sans en ajouter.
- **Level-up :** mesurer d'abord (temps entre deux niveaux, avec et sans build XP), puis proposer un réglage chiffré avant de toucher la courbe.
- **Sprites d'armes adaptatifs :** les deux : la taille suit la stat de taille, et des variantes visuelles quand le nombre de projectiles monte.
- **Réveil du Mémorial et de la Faille (24 L6b) :** durée gardée ; **son à changer** (`sfx_souvenir_trouve` provisoire), suivi au plan 15.
- **Carte à explorer (22 §11) :** tout validé : Atelier et Trempe, service « niveau d'arme » déplacé du Mémorial vers l'Atelier, Atlas gardé. C2 suit les lots du §48.
- **Catalogue d'objets (21 §4) :** validé en l'état, réglages au fil des retours.
- **Recettes en jeu faites et validées :** invulnérabilité 0,25 s (23 R1), déplacements (01), bestiaire et cadence du Hurleur (07). La recette artistique des trois personnages (08) reste ouverte.
- **Anomalies (plan 14) :** arbitrées : Écho de ta dernière run, Oubli de soi, Effondrement ; pénalité plafonnée à 25 % des PV max, rien de détruit ; une ligne de texte à l'écran.
- **Le reste** (audio et son des Résurgences, mort et bilan 02, lore 19, récompense 20, classement 09) : en attente, **à reprendre juste après** les lots du §48.
- **Ordre de travail :** lots du §48 (gameplay) puis C2 (Atelier) ; sprites adaptatifs ensuite.

## 50. XP trop rapide, raretés trop fréquentes et peu visibles, ennemis trop faibles — 2 octobre 2026

Raphaël, après une partie sur les lots G6 et C2 : « l'xp scale trop vite. et les boost d'xp et de chance sont trop élevés. de plus les raretés ne sont pas du tout assez visible. il faut que les couleurs soient limites partout sur la card. j'étais niveau 60 en 10 minute avec que des améliorations légendaires/rare dans les drops […] et en plus il y a un bug visuel. Les ennemis sont un peu trop faible et il manque un peu de projectiles à distance. effectuer des mesures pour travailler l'équilibrage stp. »

**Retours reformulés :**
- **XP :** monte trop vite (niveau 60 à 10 min). Les bonus d'XP et de Chance, relevés en G6c, sont trop forts : à revenir en arrière.
- **Raretés :** beaucoup trop de rares et de légendaires en milieu de partie.
- **Lisibilité des raretés :** la couleur de rareté doit se voir partout sur la carte, pas seulement dans le petit bandeau.
- **Bug visuel :** une ligne verticale en pointillés traverse les cartes de rareté (capture du 2 octobre, 11 h 11).
- **Ennemis :** un peu trop faibles ; il manque des tirs ennemis à distance.
- **Méthode :** mesurer avant d'équilibrer.

**Suite :** lots E0 à E5 au [plan 21 §15](21-systeme-de-jeu.md), mesures d'abord.

## 51. Niveaux trop ralentis : un entre-deux — 2 octobre 2026

Raphaël, après E2–E4 : « euh tu as trop nerfé les niveaux. essaie de trouver un entre deux stp ». Réglage mesuré et retenu au [plan 21, E5](21-systeme-de-jeu.md) : courbe 20 / 1,4 / plafond 20 000, XP par victime toujours +2 % par minute.

## 52. Offres de niveau : tout au hasard — 2 octobre 2026

Raphaël, après avoir surtout vu la Faucille et le Lance-billes : « retire les points suivants : le truc qui rajoute un délai avant d'avoir la carte de level up qui s'ouvre. retire aussi le truc avec les paliers des armes et le fait qu'on ait une arme garantie. tout doit etre random ».

- **Réserve de niveaux retirée** (plan 20 §6.7) : l'écran s'ouvre aussitôt ; plusieurs niveaux gagnés d'un coup s'enchaînent toujours dans le même écran.
- **Paliers d'armes retirés** : plus d'ouverture par niveau du joueur (palier 2 au niveau 5, etc.), plus de chance d'un palier de plus, plus de poids des paliers hauts par la Chance. Toutes les armes débloquées pèsent pareil dès le niveau 1.
- **Arme garantie retirée** : plus de carte « nouvelle arme » forcée sous 3 armes, ni une fois sur deux ensuite.
- **Restent** : au moins une nouveauté et une amélioration par offre quand c'est possible, l'objet de survie proposé tant qu'on n'en a aucun, le poids des améliorations (×1,15) et celui du Papier carbone (×2), et les armes de palier 4 qui demandent un Souvenir découvert (déblocage méta, pas un palier).

## 53. Encore trop rapide et trop facile, raretés hautes dès le début, objets flous, projectiles peu visibles — 2 octobre 2026

Raphaël, après une partie sur les réglages E5 et §52 : « alors je trouve que le level up est encore un peu trop rapide et les ennemis trop faciles (au niveau de leurs HP/dégats). et la luck est trop favorable dès le début avec les améliorations épique / légendaire. ces niveaux là doivent etre rares et s'obtenir de plus en plus en fonction de notre niveau de chance. aussi un autre soucis que j'ai qui concerne les objets est que pour beaucoup leur effet n'est pas clair (par exemple les trucs avec le bord ect...) ou alors leur effet n'est pas ressenti (manque d'effets). d'ailleurs beaucoup des armes du joueur (en tout cas les projectiles sont trop peu visible comme les notes de musiques par exemple. et enfin de manière générale il serait bien d'avoir plus d'effets à l'impact »

**Retours reformulés :**
- **Niveaux :** encore un peu trop rapides (réglage E5).
- **Ennemis :** trop faciles, en PV comme en dégâts.
- **Raretés :** épique et légendaire trop fréquentes dès le début. Elles doivent être rares, et devenir plus fréquentes à mesure que la Chance monte.
- **Objets :** beaucoup d'effets ne sont pas clairs (exemple : « Au bord de la chute » de la Médaille cabossée), ou ne se ressentent pas faute de retour visuel.
- **Projectiles du joueur :** trop peu visibles (exemple : les notes de la Boîte à musique).
- **Impacts :** plus d'effets au moment où un coup touche.

**Suite :** lots F1 à F6 au [plan 21 §16](21-systeme-de-jeu.md).

## 54. Préparer la consolidation de la qualité et retirer les règles en dur — 2 octobre 2026

Après la revue de qualité, Raphaël demande : « ok prépare un plan pour améliorer ca. un autre truc que j'ai vu c'est que le code est pas propre il y a en dur le nom de certaines attaques ou autre ».

**Demande consignée :** préparer un plan de remboursement de la dette et traiter explicitement les noms d'attaques et autres règles codées en dur. Le [plan 26](26-qualite-et-dette-technique.md) relie les 18 constats de la revue à des lots vérifiables et ajoute un inventaire de chaînes de mécanismes, paramètres de gameplay et relations de contenu présents dans le code.

**Proposition technique :** clés et réglages dans les données, libellés traduits, mécanismes convertis en types C# au chargement, références et paramètres validés. Les algorithmes restent dans leurs modules. Ordre recommandé : validations, protection des acquis et de la distribution, nettoyage des contrats, corrections de run, extractions mesurées et préparation de livraison.

**État :** plan préparé, implémentation non commencée. Aucun changement d'équilibrage ni de règle de butin n'est décidé par cette demande.

## 55. Exécuter Q0 — 2 octobre 2026

Raphaël : « go Q0 ».

Le lot Q0 du [plan 26](26-qualite-et-dette-technique.md) est engagé : fiabiliser les validations, réparer les bancs obsolètes, contrôler les échecs moteur/import/timeout et fournir un lanceur global séquentiel avec build/import partagés. Les autres lots restent ouverts.

**Résultat technique : Q0 livré et vérifié.** Global 21/21, build sans warning, huit tests de lanceurs/pannes, mesures réelles headless et GL, exports Debug/Release et préchauffage GPU verts. Le contrôle strict a exposé un diagnostic du shader des projectiles : calcul équivalent rendu compatible headless, images GL identiques avant/après. Le contrat de raretés 21 F2 reste intact. Une mesure concurrente occupant le checkout, la validation a utilisé un checkout isolé des changements de gameplay en cours. [Compte rendu](../audits/qualite-2026-10-02/q0/README.md), [guide](../VALIDATION.md). Aucun autre lot n'est implémenté par cette demande.

## 54. Coffres trop généreux, jeu trop simple après le niveau 10 à 20 — 2 octobre 2026

Raphaël : « les coffres sont trop op il faut diminuer le nb de recompenses. le jeu est trop simple une fois qu'on passe le level 10/20 cest impossible de mourir et meme de prendre des dégats. et j'ai meme pas particulirereent bien joué. j'ai juste build des projectiles et récupéré tous les coffres ect bref j'ai joué normalement et j'étais intuable fais des mesures stp »

- **Coffres :** trop de récompenses par coffre.
- **Difficulté :** après le niveau 10 à 20, on ne meurt plus et on ne prend presque plus de dégâts, avec un jeu normal (build projectiles, tous les coffres).
- **Méthode :** mesurer d'abord. Mode `--mortal` ajouté à la mesure (bot non invincible, un coup fatal le remet à fond et se compte), relevé du butin des coffres par tranche.

**Suite :** lots H au [plan 21 §17](21-systeme-de-jeu.md).


## 56. Exécuter Q1 — 2 octobre 2026

Raphaël : « fait q1 ».

Le lot Q1 du [plan 26](26-qualite-et-dette-technique.md) est engagé : réserver F1/F4 au profil dev, retirer outils et hooks de test des exports, identifier les runs d’essai et protéger les envois Steam. Les contrôles locaux et la séparation des acquis doivent rester fonctionnels. Aucun changement d’équilibrage n’est inclus.

## 57. Réponses aux questions de reprise — 3 octobre 2026

- **Difficulté (précise §54) :** Raphaël : « le jeu est encore trop simple […] j'ai fait une run de 25 min. je n'ai meme pas cherché à optimiser au max j'ai juste ratissé la map comme le ferai un joueur normal, level up au fur et à mesure. au bout de 5 minutes je ne me sentais plus trop en danger et a partir de 10-15 minutes jétais littéralement intuable. […] j'avais certes l'objet xp et celui de luck mais ca ne justifie pas ca et c'est pas non plus pour ca quil faut les nerfs ». Puis : « enfait le pb c'est pas trop de projectiles et dégats c'est ennemis qui suivent pas ». Cette run date d'**avant** les réglages H1-H2 du commit c5fac34e, encore jamais joués.
  - **Décision :** mesurer H1-H2 contre l'état précédent (bot mortel qui ratisse, 25 min), puis régler la menace des ennemis pour qu'elle suive la puissance du joueur. Ne pas affaiblir les objets d'XP et de Chance, ni d'abord la puissance du joueur. Suite au plan 21 §17.
- **Q1 :** vérifier et clore.
- **Sprites d'armes G6f :** Raphaël regarde d'abord la planche ; question maintenue.
- **Ateliers (C2c) :** prix ×5 (forge 150, Trempe 100) et 8 Ateliers par carte.
- **Mémorial réduit au soin :** à juger en jeu.
- **Après H et Q1 :** reprendre l'audio (plan 15).
- **Bestiaire :** la Tisseuse apparaît hors des Marécages, avec un poids faible. Les « mobs successifs » restent ouverts.
- **Plan 20 :** ses idées restantes (essai d'XP, niveaux de surplus, réserve automatique) sont absorbées par le plan 21 ; le plan 20 est marqué remplacé.

## 58. Questions de reprise du soir — 3 octobre 2026

- **H3 :** pas encore joué ; l'équilibrage reste tel quel.
- **Q1 :** Raphaël demande de vérifier et de clore. Fait (plan 26).
- **Chantier de la session :** « Décidés + Q5 » : Ateliers à prix ×5 et 8 par carte, Tisseuse hors des Marécages avec un poids faible (§57), puis Q5 (motifs d'attaque typés, plan 26).
- **Planche G6f :** pas encore regardée ; Raphaël demande où la voir : `doc/plans/planches/21-g6f-variantes.png`. Question maintenue.

## 59. Le nombre de projectiles doit se voir, pas de paliers — 3 octobre 2026

Raphaël, après avoir vu la planche G6f : « je comprends pas pour pour le nombre de projectiles pourquoi tu mets pas pls de projectiles sur l'écran ?? je comprends pas ta logique de paliers là jaime pas trop meme si visuellement cest réussi ».

- **Paliers G6f abandonnés** (liseré, rémanence) : un projectile décoré selon le nombre ne dit rien de plus que le nombre de projectiles visibles.
- **Règle : un projectile de plus = un projectile de plus à l'écran.** Constat dans le code : ce n'est pas toujours le cas. Les tirs visés partent vers des cibles distinctes, mais quand il y a moins d'ennemis à portée que de projectiles, ils partent ensemble sur la même ligne et se superposent ; les frappes d'une onde circulaire (Fouet, Cloche, voies en cercle) redessinent le même cercle ; les ondes en plus du Transistor ne font que multiplier les dégâts du cône.
- **Tirs visés : rafale.** Les projectiles qui partagent une cible partent l'un après l'autre, quelques centièmes de seconde d'écart, sur la même trajectoire ; dégâts inchangés. Préférée à l'éventail (qui change le jeu) et au côte à côte.
- **Ondes en cercle et Transistor : ondes successives**, une par frappe ou par onde, sans changer les dégâts.

**Suite :** lot G6g au [plan 21](21-systeme-de-jeu.md).

## 60. Réglages validés en jeu, sprites un cran en dessous — 3 octobre 2026

Raphaël, après une partie : « la difficulté est mieux réglée maintenant. les coffres cest mieux aussi. la rareté cest mieux assi. l'cran c'est mieux également et idem pour les projectiles. objets ok mieux aussi . mes retours maintenant ca va etre sur les sprites j'aime pas trop ceux des persos et les sprites des projetiles des alliés ils se ressemblent trop pour certains , et de manière générals ils ne sont pas au niveau je trouve , ils sont un cran en dessous en terme de design ».

- **Validés en jeu :** difficulté (H3 : PV d'avant H au début, ×2 dès 8 min), coffres (H1), raretés liées à la Chance (F2), écran de niveau (sans délai, cartes teintées), projectiles (lisérés, nombre visible), objets (règle chiffrée, icône quand ils agissent).
- **Sprites des personnages :** Raphaël ne les aime pas, malgré la reprise R5 du 1er octobre.
- **Projectiles du joueur :** certains se ressemblent trop. Constat : l'arc, l'arbalète, la Boussole et l'Éclat de phare sont quatre bâtonnets horizontaux de même forme ; l'Éclat de phare et le Flash photo partagent un sprite, comme le Bâton d'essence et les Craies.
- **En général :** les sprites sont « un cran en dessous » en design.

**Réponses aux questions de méthode :**
- **Périmètre :** « c'est les personnages joués qui sont un cran en dessous du reste (et leurs animations d'ailleurs) ». Ennemis, décors, icônes et effets ne sont pas visés.
- **Personnages : pousser le générateur procédural** (plutôt qu'un pixel artiste, une retouche Aseprite ou l'IA). Ce qui gêne : **la silhouette et le design lui-même** (tenue, allure), pas d'abord les couleurs ou la taille. Les animations sont à reprendre aussi.
- **Projectiles du joueur : planche d'abord**, un sprite propre à chaque arme, silhouettes différentes, un peu plus grands ; intégration après validation.

**Suite :** lots S au [plan 25](25-sprites-et-design.md).

## 61. Planches S1 et S2 — 3 octobre 2026

Raphaël : « stylisé ou grosse tete j'aime bcp il y a qq chose à faire. pour les projectiles les nouvelles versions proposées sont bcp mieux pour les quelques projectilmes refait. tu peux avancer ».

- **Personnages :** direction retenue entre B (stylisé) et C (grosse tête) de la [planche du Traqueur](planches/25-s8-traqueur-directions.png) ; à appliquer au Vagabond et à la Forgeuse, puis aux animations.
- **Projectiles du joueur :** planche S1 validée ; intégration en jeu (S1b).


## 62. Questions de reprise — 4 octobre 2026

- **Personnages joués (plan 25 S2–S4) :** « je valide pour l'instant mais pas définitif. c'est mieux maintenant on va dire ». Validés provisoirement ; des retouches restent possibles.
- **G6g (rafale, ondes successives) :** validé en jeu.
- **Mémorial réduit au soin et à la levée d'Oubli, Ateliers à prix ×5 et 8 par carte (C2c) :** validés en jeu.
- **Mort et bilan (plan 02) :** « validé pour l'instant il y aura peut-être des retouches plus tard pour peaufiner l'affichage ».
- **Audio :** la planche de la Résurgence n'est pas encore écoutée (Raphaël demandait comment l'ouvrir : `xdg-open ~/.local/share/vestiges-audio/2026-10-01/planche-resurgence.html`). Questions du plan 15 maintenues.
- **Bestiaire, « les mobs avancent successivement » :** plus d'actualité, sujet fermé.
- **Lore, à traiter en priorité :**
  - Le script v1.1 ([VESTIGES-LORE.md](../VESTIGES-LORE.md)) est **validé comme base** : il devient la référence du lore et remplace la partie I de la Bible.
  - Les quatre personnages ajoutés (le Sonneur, l'Écolière, la Photographe, la Veilleuse cachée) et l'Enfant du Bas-Port sont **gardés**.
  - Fin personnelle : **à la prochaine mort**, comme le dit la v1.1 (le personnage nommé peut finir ; sa prochaine mort clôt son histoire, écran de mort d'une ligne).
  - **Objets du lore gardés tous les deux :** la Clé verte (vraie fin) et un objet par fin personnelle.
  - **La Montée** existe dans chaque carte, discrète : une rue en pente, pavée, une barrière au bout, jamais signalée.
  - **La Barrière**, boss intermédiaire, est gardée, à concevoir (plan 07).
  - **Failles (P7) :** le profil compte les Failles acceptées et quelques détails changent (une ligne de document, un murmure). Jamais punitif, sans effet sur les fins.
- **Chantier de la session :** Q2a puis Q2b (sauvegardes protégées, fin de run attribuée une seule fois), plan 26.

## 63. Suite du plan 26 — 4 octobre 2026

Après Q2a, Q2b, Q6a et Q6b, Raphaël : « oui ca me va Q3 et Q6 ». Lots suivants validés : **Q3** (opérations Steam) et **Q6c** (relations de contenu et capacités ennemies), à mener dans une nouvelle conversation. Les questions de l'Indicible (« ne se combat pas vraiment ») et de `essence_cost_per_attack` restent ouvertes au tableau de bord.

## 64. Questions de reprise — 4 octobre 2026, après-midi

- **L'Indicible ne se combat pas vraiment :** à **mesurer après Q6c** (où vont les tirs, combien touchent, pourquoi il ne frappe pas) ; la reprise de son design vient ensuite, sur ces chiffres.
- **La Barrière :** fiche à concevoir **avec la reprise de l'Indicible**, au plan 07.
- **`essence_cost_per_attack` :** **retiré** des trois armes et du contrat ; il n'était lu par aucun mécanisme, rien ne change en jeu. Une idée de coût en Essence reviendrait par le plan 21.
- **Classement Weekly (Q3) :** **gardé pour toutes les runs**, sans seed fixée ; un tableau par semaine (`Vestiges_Weekly_<année>-W<semaine ISO>`), créé par le jeu au premier envoi, au lieu d'un tableau unique qu'on supposait remis à zéro par Steam. Un futur défi à seed fixée aurait son propre classement (plan 09).
- **Steam réel :** Q3 est prouvé par un faux Steam, limite écrite ; la session réelle se fera avec l'App ID du jeu (ou le 480) quand la bibliothèque native sera posée, au plus tard à Q12. Raphaël demande s'il peut déjà mettre le jeu sur Steam : réponse donnée (compte Steamworks, Steam Direct, App ID attribué au paiement, page « Bientôt disponible »). Démarche à son initiative ; le changement d'App ID tient en une constante et `steam_appid.txt`.
- **Audio :** reste ouvert, Raphaël écoute la planche de la Résurgence quand il veut.
- **Lore :** après Q6c, proposer au plan 19 les lots de production (textes, objets, fins, Montée) avec le nom de Vaulme, les trente et un noms et le déclenchement des fins personnelles en options.
- **A-VERIFIER.md :** Raphaël coche les points en jouant.

## 65. Décisions du soir — 4 octobre 2026

Raphaël, sur la liste des points en attente :
- **L'Indicible :** « le refaire entièrement » (piste C de la mesure, plan 07), à concevoir avec la Barrière.
- **Bonus de dégâts de la meute** (`pack_bonus_damage`, jamais appliqué) : **retiré**.
- **Lore :** les lots L1 à L7 du plan 19 §15 sont **validés**, ainsi que les trois points ouverts tels que recommandés : **Vaulme gardé**, **liste des trente et un validée**, **fin personnelle déclenchée par un fil complet et un geste en run** (option A).
- **Synergies de perks :** « les implémenter », mais **après réexplication** de ce dont il s'agit et de ce qui est envisagé. Point de départ : les six synergies d'origine appartenaient aux anciens Dons, retirés au plan 21 G2b ; DECISIONS §39 demande que les objets n'annoncent pas leurs synergies. À reformuler avant tout code.
- **Arme en main** (option d'essai des Graphismes) : **abandonnée**.
- **Barème des points d'intérêt** (`pois.json`, 25 à 300 points selon le type, 50 partout en jeu) : **à brancher**.
- **Taille du texte** (115 et 130 %) : **retirée**.
- **Ornières du tracteur embourbé :** laissées au choix de Claude, gardées telles quelles.
- **Steam :** Raphaël demande la marche à suivre pour obtenir l'App ID et faire le test réel.
- **Audio :** la commande d'ouverture de la planche avait pris le point final de la phrase ; la planche existe bien.

## 66. Retours audio — 4 octobre 2026, soir

Raphaël, après la planche de la Résurgence : « pour l'audio j'ai du mal à distinguer. mais par contre clairement il y a de gros problèmes de mixage ».
- **Cri du Hurleur :** « une horreur absolue, retire-le immédiatement ». Son retiré (`cry_audio` ôté de la fiche) ; le cri reste une capacité (annonce et renforts), désormais muet.
- **Sons des tirs du joueur :** « plutôt les retirer, ou alors les baisser un peu en volume et trouver de la diversité (en avoir un par arme) ». Premier pas : les treize sons d'attaque des armes baissés de 6 dB (−8 → −14). Un son propre à chaque arme reste à produire (plan 15).
- **Musiques, ambiance, sons manquants :** « à revoir plus tard » ; des sons sont à améliorer ou à ajouter.
- **Résurgence :** peu de différence perçue entre les extraits ; Raphaël demande les sons bruts. Copiés dans `~/Téléchargements/vestiges-resurgence-sons-bruts/` (annonce `mus_crepuscule`, Résurgence `mus_nuit_vagues`, signal de danger, et pour comparer exploration et combat).
- **Synergies (suite du §65) :** forme **C** retenue, des synergies de règles (des règles qui se répondent, sans effet ajouté ni annonce). Planche de combinaisons à proposer avant code.

- **Cri du Hurleur, suite :** Raphaël retient la variante **C** (chœur grave désaccordé). Elle remplace l'ancien fichier, au volume −6 dB dans la banque (−7 dB de plus quand la créature est proche, par `PlayNearbyAudio`), et `cry_audio` est rétabli. À écouter en run.

## 67. Questions de reprise — 4 octobre 2026, nuit

- **Cri du Hurleur :** variante **C** gardée (déjà branchée au §66).
- **Lore L1 :** **planche d'abord**. Tous les textes réécrits (FR et EN) sont soumis à Raphaël dans une page à relire ; rien n'entre dans le jeu avant son accord.
- **Plan 26 Q4 :** gardé en cinquième position, après la fiche commune de l'Indicible et de la Barrière.
- **Steam :** pas encore d'App ID ; Raphaël redemande la marche à suivre, écrite cette fois dans [STEAM-MISE-EN-PLACE.md](../STEAM-MISE-EN-PLACE.md).
- **Barème des lieux :** la question de `A-VERIFIER.md` était périmée (score aux seules éliminations depuis le §40, lieux désactivés, `score_points` lu par aucun code). Raphaël choisit de **retirer** `score_points` des lieux et des coffres.
- **Ordre de travail confirmé :** nettoyage (§65), puis lore L1, planche des synergies de règles, fiche Indicible et Barrière, puis Q4.

## 68. Avancer sans validation — 5 octobre 2026

Raphaël, après les planches L1, synergies et Barrière/Indicible : « continue d'avancer stp sur d'autres sujets je peux pas valider là car jai pas le temps ».
- Les trois planches restent en attente de sa relecture ; rien n'en est codé.
- Claude enchaîne les lots **sans décision de design** : corrections de run et validations de catalogues du plan 26 (Q8, Q7), qui ne changent ni règle ni équilibrage. Chaque lot reste découpé dans le plan, vérifié et clos par un commit.

## 69. Reprise sur une priorité au choix — 6 octobre 2026

Raphaël : « Reprend le projet sur le sujet de ton choix parmi les plus importants à faire qui sont décrits dans les docs ».
- Lot choisi : **plan 26 Q7c-4a, biomes**, suite des validations des catalogues du monde. Découpage écrit avant implémentation, lot livré et vérifié ; les paramètres du monde, fermes et carrières restent aux sous-lots suivants.
- Cette reprise ne tranche pas les planches de lore, de synergies ou de boss en attente ; aucune nouvelle règle de gameplay n'est activée.

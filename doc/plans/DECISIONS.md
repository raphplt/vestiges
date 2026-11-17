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
| Recette des noms, icônes et Oublis (26 septembre, nuit) | Noms du catalogue 4.6 validés sauf « Craies » et « Aiguille d'horloge » (« j'aime pas trop le nom ») ; les trois icônes v2 validées ; les neuf Oublis de carte validés, leurs noms « un détail » | [17 §6](17-armes-coffres-modificateurs.md#réponses-de-raphaël--26-septembre-nuit) ; vague 2 (2A, 2B) et Oublis de carte lancés ; noms provisoires Craies et Chronomètre |
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

## 70. Questions de reprise — 6 octobre 2026, soir

Raphaël valide en bloc les recommandations des trois planches en attente :
- **Synergies de règles** ([planche](05-planche-synergies.md)) : les sept raccords **R1 à R7** ; **une règle commune** : tout ce qu'une arme fait (écho, forme, saut de chaîne, feu, brûlure, saignement) compte comme un coup et une élimination de cette arme, et les objets ne se déclenchent jamais entre eux ; l'**Épingle à nourrice lit les entravés** (ralentis, désorientés, endormis) ; les **pouvoirs des personnages seront codés** dans un lot du plan 06 qui suit les raccords, en commençant par la Forgeuse ; **rien pour le recul** pour l'instant.
- **Barrière et Indicible** ([plan 07](07-bestiaire-et-rencontres.md#fiche-commune--la-barrière-et-lindicible--4-octobre-2026-proposition)) : la Barrière se lève vers 12 min, après la troisième Résurgence, en travers de la route ; les Mémoriaux ravivés changent le **nombre de battants** ; l'Indicible se frappe **à ses mains**, avec une fenêtre où il est découvert après chaque vague ; **trois phases** (tempête, marée, vague) ; durées visées de **60–90 s** pour la Barrière et de **2–3 min** pour l'Indicible avec un build de référence. Lots B1 à B4 ouverts.
- **Lore L1** ([planche](19-planche-L1.md)) : les quatre questions d'intégration comme recommandé : textes anglais dans `translations.csv` (les JSON portent la clé) ; journal en une seule liste, dans l'ordre de découverte, en attendant L2 ; une **seconde planche courte** pour les autres armes dont la dernière phrase commente ; devise de l'accueil gardée. **Les textes de la planche restent à relire** avant d'entrer dans le jeu.
- **Ordre :** Claude prend d'abord le lot **S1 (raccords)**, puis les lots B. La suite du plan 26 (Q7c-4b/c, Q7d) reste le chantier de repli quand un lot attend une relecture.

## 71. Tout doit se voir — 6 octobre 2026, nuit

Après le lot S1, Raphaël demande si les effets se voient en jeu et si les sprites des ennemis sont touchés. Constat : seule la Brûlure se voit pendant sa durée (braises). Ralentissement, pause figée, désorientation, saignement et Fragile teintent un polygone de secours masqué dès qu'une créature a un sprite, ce qui est le cas des douze. Une créature figée continue son animation d'attente.
Raphaël : « bon bah ca sera ton prochain chantier de faire en sorte que tout se voit. prépare un plan pour qu'on puisse voir concretement en jeu tous les impacts bien comme il faut et les effets sur les ennemis (et sur le joueur quand on se fait attaquer, s'il y a des effets en particulier) ».
- **Prochain chantier de Claude :** la lisibilité des impacts et des effets, sur les ennemis et sur le joueur ([plan 27](27-tout-se-voit.md)). Il passe avant les pouvoirs des personnages (plan 06) et les lots B.
- Le plan part d'un inventaire du code, puis propose les lots ; une planche de captures avant/après précède chaque intégration visible.

## 72. Plan 27, questions tranchées — 6 octobre 2026, nuit

Raphaël, sur les cinq questions du [plan 27](27-tout-se-voit.md#4-questions-pour-raphaël) : « 1 oui / 2 oui comme tu dis / 3 oui il faut un chiffre je crois / 4 laisse coupé / 5 l'ordre que tu viens de dire ».
- **Statuts avec « Effets d'attaque » coupé :** leur marque minimale (teinte, animation, étoiles) **reste visible** ; seules les fioritures suivent le réglage et le budget.
- **Tics de brûlure et de saignement :** un **petit chiffre regroupé toutes les 0,5 s** par créature, à la couleur du statut.
- **Dégâts reçus par le joueur :** **un chiffre s'affiche** au-dessus du joueur (en plus de l'éclair rouge et de la vignette), à intégrer au lot V3.
- **Gel d'image (hitstop) :** **reste coupé**.
- **Ordre :** V0 → V1 → V2 → V3 → V4 ; V5 (sons d'impact) après la reprise audio du plan 15.

## 73. Projectiles du joueur réparés — 6 octobre 2026, nuit

Pendant le lot V0 du [plan 27](27-tout-se-voit.md#6-v0-livré--6-octobre-2026), le shader des projectiles du joueur montre le même défaut que celui des créatures : depuis le lot F5 (§53), le liseré et le halo ne s'affichaient pas et les sprites étaient assombris. Raphaël les avait validés en jeu dans cet état le 3 octobre. Question posée avec la planche avant/après ; réponse : « Réparer tout ».
- **Projectiles du joueur :** couleurs d'origine, liseré et halo visibles, comme décidé au §53.

## 74. Difficulté : trop facile après 10 minutes — 6 octobre 2026, nuit

Raphaël, après le lot V0 du plan 27 (« jai rien à redire ») : « le jeu est toujours bcp trop facile à partir d'un moment (10 min) en faisant le bon build (je prend juste 4 armes nimporte lesquelles et projectiles / chance / xp / crit. il devient hyper prioritaire de tester l'équilibrage du jeu plus, et surout de régler la difficulté. pour moi : il ne faut pas (trop) baisser les stats du joueur mais surtout augmenter celles des ennemis (et pour ca le vecteur principal que je vois serait que le joueur ait des moyens tres concrets d'augmenter la difficulté de la run - il faut une stat dédié à cela, mais pas sur qu'on l'appelle 'difficulté' peut etre qq chose lié à l'oubli. et en gros ca augmente le nombre d'ennemis, leur dégats, leur résistance...) c'est parfait car ca rend pas le jeu impossible et ca permet au joueur de 'controler' sa difficulité lui meme et ca devient un skill dans le jeu. et sinon je ferai malgré tout, en dehors de ce que je viens de dire, un équilibrage sur les ennemis pour les rendre plus forts , résistants et nombreux au fur et à mesure de la partie (c'est déjà le cas mais augmenter les curseurs) et rendre les moments de crise plus durs. »
- **Constat :** à partir de 10 min environ, un build de quatre armes quelconques avec projectiles, Chance, XP et critique rend la run trop facile.
- **Priorité :** tester l'équilibrage davantage et régler la difficulté. C'est **le chantier suivant, une fois le plan 27 entièrement terminé** (V0 à V4).
- **Direction :** ne pas (trop) baisser les stats du joueur ; renforcer surtout les ennemis.
- **Levier principal :** une stat dédiée que le joueur augmente concrètement pour durcir sa run : plus d'ennemis, plus de dégâts, plus de résistance. Nom à trouver, plutôt lié à l'oubli qu'à « difficulté ». La difficulté devient un choix et une compétence du joueur. À examiner au moment du plan : le Péril existant (0 à 10, Failles et bannissements, plan 21 §8) va déjà dans ce sens.
- **En plus :** ennemis plus forts, plus résistants et plus nombreux au fil de la run (curseurs existants relevés) ; Résurgences plus dures.

Précision de Raphaël, même soir : « pour le péril il faut par exemple un objet dédié au péril, et des sortes de totems ou moyens de l'augmenter dispersés sur la map. il faut aussi que ca donne plus d'essence par exemple en contrepartie »
- **Le Péril est le levier retenu** pour la stat de difficulté choisie par le joueur.
- **Sources à ajouter :** un objet dédié au Péril ; des totems (ou autres moyens) dispersés sur la carte pour l'augmenter.
- **Contrepartie :** plus d'Essence, entre autres (s'ajoute à l'XP, au score et aux raretés que le Péril majore déjà).

## 75. Liseré des projectiles du joueur allégé — 7 octobre 2026

Raphaël : « diminuer le halo/contours autours des projectiles du heros je le trouve trop prononcé ca ne me plait pas ».
- **Liseré** crème d'un pixel ramené de 0,9 à 0,45 d'opacité, **halo** de 0,28 à 0,1 (`player_projectile.gdshader`). Les projectiles restent lisibles sur sols gris, sable et herbe (rendu comparé hors jeu sur les neuf planches du héros) et se distinguent toujours des tirs ennemis.
- **Fenêtres de test** sur le second écran par défaut sous Linux (`godot_screen_args`, `tools/lib/portable.sh`), à sa demande : « lance toujours le jeu sur lecran viewsonic stp ».

## 76. Reprise autonome du chantier audio — 7 octobre 2026

Raphaël demande de reprendre « le sujet audio (mixage, un son par armes, armes d'ambiances, sons des menus, musiques) » et d'avancer en autonomie ; questions et validations possibles au fil du travail. « Armes d'ambiances » est interprété comme les ambiances sonores, dans la continuité du plan 15. Le report des musiques et ambiances du 4 octobre prend donc fin. Lots A3a à A3c proposés dans le [plan 15](15-audio.md). Aucune dépense demandée.

## 77. Plaque PV et niveau — 7 octobre 2026

Raphaël demande, capture à l'appui, de rendre le bloc de PV et de niveau
« plus jolie travaillée » et de corriger les soucis de positionnement en
restant dans la direction artistique du jeu. Reprise ciblée au plan 04, R8 :
plaque sombre, métal patiné, niveau cyan, jauge verte et textes correctement
alignés. Les règles de vie, de progression et de bouclier restent inchangées.

## 78. Péril : sans plafond, stèles, objet ; planche lore L1 validée — 7 octobre 2026

Réponses de Raphaël au début du chantier difficulté (§74).
- **Planche lore L1** : « valide les textes de la planche de lore ». Textes validés tels quels ; intégration à planifier au plan 19.
- **Steam** : il demande comment obtenir l'App ID ; marche à suivre redonnée ([STEAM-MISE-EN-PLACE](../STEAM-MISE-EN-PLACE.md) §1). Reste à faire de son côté.
- **Échelle** : « Sans plafond ». Le Péril n'a plus de maximum.
- **Totems** : stèle à activer (maintenir la touche comme à une Faille) pour +1 Péril définitif, une dizaine par carte.
- **Objet du Péril** : « monte le péril, ne paie pas mieux imo. et en fait je pense que les bénéfices pour le joueur du péril c'est juste plus d'ennemis donc plus d'xp et d'essences car plus de morts ». L'objet ne donne que du Péril ; il occupe un emplacement.
- **Principe qui en découle** : le Péril ne paie pas directement. Sa seule contrepartie est d'avoir plus d'ennemis, donc plus d'XP et d'Essence par les éliminations. Les bonus directs d'XP et de rareté par point sont retirés, la stèle ne donne pas d'Essence. Le multiplicateur de score est gardé : il ne rend pas le joueur plus fort et il départage les runs au classement (à revoir si Raphaël le refuse).
- « je te demande de faire le chantier dédié au péril ok ?? » : pas d'autre question avant de livrer ; les choix restants suivent les recommandations du [plan 28](28-peril-et-difficulte.md).

## 79. Choix des sons d'armes — 7 octobre 2026

Raphaël transmet `choix-armes-2026-10-07.json` et demande « voici mes choix. au boulot ». Neuf choix explicites : Faucille B, Parcmètre A, Arc du gymnase B, Cloueuse C, Scalpel C, Trousseau C, Boussole B, Lampe à pétrole C, Gants de boxe A. Quinze refus (`aucun`) : rechercher des sons nouveaux et propres à chaque arme, plutôt que d'autres déclinaisons du même son. Lance-billes : « aucun n'est un son de déclic / bille ». Boîte à musique : « genre des notes de musiques ». Export original, y compris toutes les notes, archivé hors dépôt ; intégration et reprise de recherche au plan 15 A3b.

## 80. Sons d’armes, deuxième sélection — 7 octobre 2026

Export R2 de Raphaël : Lance-billes B, Parapluie C, Pelle à neige A, Rallonge B, Assiettes A, Râteau A, Lentille de phare C, Polaroïd C, Baguette de sourcier A, Gomme C, Craies B, Transistor A, Chronomètre C. Cloche refusée : « aucune il faut un son plus simple ». Boîte à musique refusée : « enfait jaime pas... ptettre mettre une note au hasard à chaque fois ? ». Préparer l’essai de notes aléatoires et une Cloche simplifiée ; ces commentaires ne valident pas un fichier précis. Export complet conservé hors dépôt, suite au plan 15 A3b3.

## 81. Plaque PV/niveau plus compacte — 7 octobre 2026

Raphaël trouve la reprise meilleure, mais le cadre trop gros. Conserver son
style et réduire son encombrement, surtout les marges et la hauteur (plan 04 R8).

## 82. Cloche B et Boîte à musique C — 7 octobre 2026

Raphaël valide explicitement « cloche b et boite a musique c » dans la série R3. Cloche B : frappe ronde synthétique de 300 ms. Boîte C : notes pincées de 250 ms, do/ré/mi/sol/la, une note aléatoire à chaque contact audible, sans répétition immédiate. Les deux timbres sont désormais choisis ; terminer leur intégration et leur vérification au plan 15 A3b4.

## 83. Performance des foules — 9 octobre 2026

- Demande : « met toi sur le chantier Performance des foules, essaie de comprendre ce qui fait qu'on est si limité sur la taille de la foule […] alors que tous les autres jeux (megabonk, vampire survivor) le gèrent tres bien. le but sera de comprendre les failles de notre approche et les corriger. » Chantier ouvert au [plan 29](29-performance-des-foules.md).
- En cours de route : « à vérifier si les problemes de fps relevés ne sont pas dû au fait que tu test avec un environnement limité ». Vérifié : Claude Code hérite du quota CPU de Zed (2 cœurs), mais Godot n'en utilise qu'un ; mêmes FPS hors quota.
- Mesure hors quota : Raphaël choisit que l'agent lance les bancs par `systemd-run --user --scope` (scope sans quota), sans toucher à la configuration de Zed.
- Après F1, sur le plafond : « alors enfait c'est bien d'avoir corrigé le banc mais ca ne règle pas le probleme. le jeu ne devrait pas galérer à afficher des centaines de créatures (potentiellemnt des milliers). je me répète mais tous les autres jeux du genre bullet heaven y arrivent tres bien... » Objectif du plan 29 relevé : des milliers de créatures, pas un plafond grappillé. Le plafond reste à 200 tant que l'architecture n'a pas changé.
- F2 (séparation, arrêt au contact) : « Oui, avec planche ». À implémenter, mesurer, puis montrer des captures avant/après avant de garder.
- Planche F2 (séparation, arrêt au contact) : « Garder tel quel ». Gardé avec les réglages de `data/scaling/crowd.json`.
- Plafond : « releve le plafond. pour l'instant met le à 500 en gardant à l'esprit qu'on voudra l'augmenter à plusieurs milliers dès que possible. » `active_enemies_ceiling` passe à 500 (la cible de run reste 110 + 4 par minute, Péril compris).
- « il faut essayer de penser que ça fonctionne même sur l'entrée de gamme » ; tester en export Release ; tester en run réelle avec un plafond au-delà de 1 000.
- Cible de foule : « on va se calquer sur vampire survivor imo. et donc ne pas aller au delà pour l'instant » (≈ 300 avant l'arrêt de l'apparition normale, 500 au plus selon les wikis communautaires). Plafond gardé à 500.
- Lot D : « je pense que on va passer en noeudless car de ce que tu me dis ca a vraiment l'air meilleur ». Décidé ; repris par un autre agent sur le [brief du lot D](29-lot-D-brief.md). Guide d'export demandé : [EXPORT.md](../EXPORT.md).
- Lot D revu : « avec ce que tu me dit on va garder les noeuds pour l'instant ». Lot D reporté ; le brief reste prêt si le besoin revient (entrée de gamme, foule plus grande). « on s'arrete là pour les optis je pense c'est déjà du bon travail » : chantier performance des foules clos pour l'instant.
- Temps forts : « vampire survivor a plus de temps fort que nous (j'y joue en ce moment). megabonk aussi avec sur les premieres 10 minutes 2 mini boss qui spawnent, 2 vagues d'ennemis intenses, un boss au bout des 10 min, et des "élites" (qu'on a aussi pour le coup). donc ptet concretement que sur le jeu on pourrait étudier des pistes pour dynamiser un peu plus notre jeu. » Piste à étudier, rien de décidé.

## 84. Temps forts : réponses à l'étude — 9 octobre 2026

Étude livrée au [plan 30](30-temps-forts.md) (chronologie mesurée des 15 premières minutes). Réponses de Raphaël aux quatre questions :
- **Résurgence : « Pic sans reflux ».** Pendant la crise, le plafond de run monte (≈ ×1,6) pour que la vague s'ajoute à la foule au lieu de la remplacer ; à la fin, pas de retrait forcé : le plafond redescend et la foule en trop s'use d'elle-même (éliminations, créatures semées). La crise devient une marche plus haute, voulue.
- **Souverains : deux rendez-vous fixes**, vers 2:45 et 7:00, entre les Résurgences ; annoncés, nommés, barre de PV, combat de 20 à 40 s avec le build de référence, coffre rare. Retirés du tirage des micro-événements.
- **Barrière : à 10 min, à l'heure, en priorité.** Déclenchée vers 10:00 (après la deuxième Résurgence et son accalmie), et non plus après la troisième Résurgence (qui commence en fait entre 13:36 et 14:50). Remplace le déclenchement du §70. Les lots B1–B2 du plan 07 passent avant les pouvoirs des personnages.
- **Nuées sans objectif (D) et respiration par minute (E) : après A, B et C**, si la run manque encore de contraste une fois ceux-ci joués.
- Après l'étude : « ok ca me va go ». Lots engagés dans l'ordre proposé : T1 (Résurgence), T2 (Souverains), puis B1–B2 (Barrière).

## 85. Barrière : questions de reprise — 9 octobre 2026, nuit

Reprise des lots B1–B2 du plan 07 (§84). Réponses de Raphaël, toutes conformes à la recommandation :
- **Contournement : ailes de chaînes.** Deux ailes de chaînes tendues, intouchables, prolongent la grille bien au-delà de l'écran ; on ne passe qu'en brisant un battant. L'Effacement continue d'avancer derrière : rester bloqué coûte.
- **Orientation : selon le cap.** Horizontale à l'écran si le joueur va vers le haut ou le bas, verticale s'il va à gauche ou à droite (les deux trois-quarts des décors). Deux jeux de sprites.
- **Récompense : un coffre par battant**, le dernier rare (comme le Souverain). Choix propre à la Barrière ; le plan 13 reste non arbitré.
- **Barre de boss en haut de l'écran**, avec le nom, pour la Barrière, l'Indicible et aussi les deux Souverains ; un cran par battant pour la Barrière.
- **Flux de créatures pendant le combat : densité visée ×0,5**, réglage en données.
- **Tableau de bord §2 :** App ID Steam pas encore obtenu ; l'audio est « à faire bientôt » (à reprendre après B2). Le reste inchangé.

## 86. Décors un cran au-dessus — 9 octobre 2026

- Demande : « Je voudrais que tu améliore les sprites de certains décors. Fais d'abord un tour sur le repo pour lister et évaluer la qualité des décors (détails, cohérence globale ect). Je compléterai par mes propres retours. » Audit des 186 sprites présenté ([plan 31 §1](31-decors.md)).
- Réponse à l'audit : « Je valide tes retours. j'ai rien à dire de plus honnetement. » Ordre : « commencer par le marais et ensuite la partie urbaine ok », « fais vraiment chaque décor avec une attention maximale et produit le meilleur résultat possible ». Chantier ouvert au [plan 31](31-decors.md) ; les questions du tableau de bord §2 sont toutes reportées par Raphaël ou de son ressort (App ID Steam, écoutes, recette en jeu) : rien à lui reposer avant ce chantier.
- Après les lots marais et urbain (9 octobre, soir) : « c'est magnifique […] continue avec les prochaines décors. Vraiment j'aime bcp ce que tu as fait. je crois que sur les autres il y a moins de choses à revoir. » Marais et urbain validés tels quels ; suite au plan 31 : forêt, champs (ferme comprise), carrière, avec une reprise plus légère. La question des trois murs orphelins (U4) reste ouverte.
- « ok continue avec la foret stp. go » : lot F1 (forêt) livré ; ses arbres sont partagés avec l'arbre isolé des champs et le verger, régénérés avec lui. Suivants : C1 (champs et ferme), Q1 (carrière).
- Question U4 (10 octobre) : « oui supprime les. Et passe au champ stp » : `prop_brick_wall`, `prop_concrete_wall` et `prop_concrete_wall_v3` supprimés avec leurs `.import` ; lot C1 (champs et ferme) livré dans la foulée : maisons de ferme régénérées avec les façades urbaines, grange endommagée vraiment abîmée, haies, verger, petits décors des champs. Suivant : Q1 (carrière).
- « go carrière » (10 octobre) : lot Q1 livré ; les cinq biomes du plan 31 sont repris.
- Planche orpheline des champs (10 octobre) : « Oui supprime la planche orpheline stp » : `_wild_fields_hero_props.png` supprimée avec son `.import`. L'ancien générateur qui la produisait (`scripts/generate_wild_fields_props.py`) réécrirait sept décors repris en C1 : question posée au tableau de bord.

## 87. Sprites des héros à venir — 10 octobre 2026

- Demande : « j'aimerai bien que tu prépares les sprites des prochaines héros. Base toi sur le lore et les éléments écrits ». Lu comme : les héros du lore (§5) qui n'ont pas de sprites au niveau des trois joués, sept plus une variante : l'Éveillée, le Facteur et la Scaphandrière (casting validé, prototypes anciens), le Sonneur, l'Écolière et la Photographe (ajouts du 4 octobre), la Veilleuse (cachée) et l'Enfant du Bas-Port (variante du Vagabond).
- Plan [32](32-heros.md), lots H1 (Éveillée, Facteur, Scaphandrière), H2 (Sonneur, Écolière, Photographe), H3 (Veilleuse, Enfant) livrés : huit modèles procéduraux au gabarit des joués, signature tirée du lore pour chacun, sprites écrits dans `assets/characters/<id>/`. Rien n'est branché (aucun n'est dans `characters.json`) ; planches à valider par Raphaël.
- Réponse aux questions du lot (10 octobre) : « oui stp reprend les 3 jouables et améliore les si tu peux pendant que tu y es. et supprime generate_wild_fields_props (et tout autre script maintenant inutile) ». Lot H4 : yeux en amande du Vagabond, du Traqueur et de la Forgeuse, barbe du Vagabond raccourcie, lunettes de la Forgeuse remontées sur le crâne à monture de laiton, marteau d'acier plus sombre ; sprites régénérés. Scripts supprimés : 13 générateurs historiques de `scripts/` remplacés par le pipeline (Cracheur, Rampant, Ombre rampante, Tréant et leurs aperçus, Traqueur et son aperçu, décors urbains et des champs, tuiles de la forêt et des champs) et 6 prototypes du Traqueur dans `tools/` ; gardés : `scripts/generate_blood.py` et `scripts/generate_tisseuse.py` (seules sources de sprites encore utilisés), `tools/generate_tiles.py` (fond des planches de décors). 27 tuiles orphelines laissées par ces générateurs : question au tableau de bord.
- Tuiles orphelines (10 octobre) : « oui supprime les tuiles orphelines stp » : les 27 images que plus rien ne chargeait (20 dans `assets/tiles/foret/`, 7 dans `assets/tiles/champs/`) supprimées avec leurs `.import`, après avoir revérifié qu'aucune scène, aucun JSON ni aucun code ne cite leur nom ou leur uid. Les tuiles Wang (`_wNN`) des mêmes matières restent.

## 88. Reprise du chantier audio — 10 octobre 2026

Raphaël : « je voudrais continuer le chantier audio ; go ». L'audio passe donc avant B2 (le §85 le plaçait après). Réponses aux questions de reprise, toutes conformes à la recommandation :
- **Frottement du mix :** refaire une page d'isolement. Nouvelle run de référence sur le Mac, puis musique, ambiance, effets et chaque boucle écoutés séparément ; Raphaël désigne celui qui frotte.
- **Rôle exploration/combat : fond unique, combat aux pics.** La musique d'exploration est le fond de la run ; celle de combat ne part que sur un vrai pic (élite, mini-boss ou boss proche, foule très dense), seuils en données.
- **Résurgence : V2 et accent d'entrée.** Annonce prise à 52 s de `mus_crepuscule`, crise à 34 s de `mus_nuit_vagues` ; un son d'accent court au début réel de la crise, à choisir sur planche.
- **Ordre : mix → sons manquants → musique.** Le mix et les réglages de musique d'abord, puis les sons manquants et les impacts par matière (plan 27 V5) sur planches, enfin la recherche de nouvelles musiques.
- **Tableau de bord §2 :** inchangé depuis le §85, pas reposé.

## 89. Les deux boss en autonomie — 10 octobre 2026

Raphaël, après la livraison des lots audio M1–M3 : « est-ce que tu peux avancer en autonomie sur un autre sujet stp comme les deux boss (l'Indicible et celui à 10 min) ». Les questions de design étaient toutes tranchées (§70, §84, §85) ; aucune question posée. Lots enchaînés : B1a, B1b, B1c, puis B2 (plan 07).

Choix faits par Claude faute de validation de la planche B2a, **tous révisables** (sprites régénérables par `tools/generate_props.py barrier`, valeurs en données) :
- **Planche B2a intégrée telle que proposée** : grille de fer forgé, piliers de pierre, ailes de bornes et de chaînes, diagonale en vertical (lacet 72°) ; **poing en gantelet de fer** (cohérent avec la chaîne de la Forgeuse ; la main de noyé reste générée, non utilisée).
- **Distance de levée : 150 px** devant le joueur (et non ~350) : à 350, la grille naissait hors de l'écran (la vue fait 540 px de monde en hauteur).
- **Ennemis qui traversent la grille** : la grille n'arrête que le joueur (les créatures, sans corps, passent entre les barreaux).
- **Mêlée contre un boss** : une partie de boss se frappe à son bord (B1c) ; l'allonge contre les créatures ne change pas.
- **Sons provisoires** pris dans la banque (levée, battant brisé, chute, poings, chaîne), en données, à remplacer au chantier audio.
- **L'Indicible (B3)** :
  - mains en **main grise de noyé** (le modèle écarté pour la Barrière) ;
  - vent, éclairs et prises qui visent la position où le joueur sera ;
  - **marée** par bandes depuis un bord ;
  - **vague** à une ou deux brèches ;
  - fenêtre « découvert » rendue par le statut **Fragile** existant (+100 %).
- **Premier réglage (B4)**, mesuré en vraie run longue, bot invincible : réserve de la Barrière **20 000 + 15 000 par battant**, Indicible **90 000 × 3,2**. L'écart entre builds est très grand : réglage fin à la partie de Raphaël.
- **Finition de la Barrière** (« fait les points 2, 4 […] 5 : fait une vraie animation, prends le temps ») :
  - levée en trois temps (fissure, surgissement en vague, verrouillage) ;
  - huit sons propres (montages CC0 et synthèses) ;
  - **préférence de ciblage des parties de boss** (100 px), sans laquelle la foule passait avant le battant ;
  - réserve 5 000 + 15 000 par battant : un battant suffit pour passer, les autres sont la récompense.
- **Fusion du 10 octobre** : un `git pull` est resté en conflit avec la PR #2 (décors, héros). Résolue à la demande de Raphaël (« règle les conflits stp ») en gardant les deux côtés. Les sections du 9 et 10 octobre se sont trouvées en double ; les sections distantes gardent leurs numéros (§86 décors, §87 héros), la reprise audio devient §88 et ce chantier des boss §89.

## 90. Personnages à intégrer et quêtes de déblocage — 10 octobre 2026

Raphaël : « il faudrait implémenter ces personnages dans le jeu […] et aussi faire ce système de quêtes qui permet de débloquer des objets, des armes, des personnages de manière permanente ». Inventaire fait (plan 06, plan 21 §10, fiches du casting) ; réponses aux questions :

**Acquis :**
- **Personnages à rendre jouables : l'Éveillée, le Facteur et la Scaphandrière** (sprites du plan 32 H1). Les autres héros du plan 32 attendent.
- **Kit complet des fiches** ([06-fiches-casting](06-fiches-casting.md)) : passif, mobilité propre, arme de départ, y compris les nouvelles armes (Sacoche de lettres, Fusil-harpon).
- **Quêtes d'abord, personnages ensuite** : le système de déblocages permanents (personnages, armes, objets) et ses quêtes précèdent le branchement des trois personnages, qui en seront des récompenses.

**Non acquis (réglé plus bas) :** ce qui est verrouillé au départ pour un profil neuf.

**Suite, même jour.** Raphaël : « il faut avoir une seule instance de quêtes : celles pour débloquer des armes, personnages, objets […] variées, de différents niveaux de difficultés, certaines imbriquées […] il ne faut pas que les quêtes soient affichées en permanence in game. par contre, il faut avoir la notif qu'on a complété une quête dès que c'est fait sans attendre la fin de la run ». Puis, sur la liste du [plan 06 §9](06-personnages-quetes-defis.md) : « J'ai tout lu, ça me va, très bonne base ».

**Acquis :**
- **Une seule famille de quêtes**, qui débloquent personnages, armes et objets de façon permanente. Plus de quêtes de run, plus de Souvenirs comme clé.
- **Rien d'affiché en permanence en jeu** ; **notification dès qu'une quête est accomplie**, en pleine run.
- **La liste des 36 quêtes et la réserve de départ du plan 06 §9 sont la base** ; les seuils restent à mesurer.
- ~~Des quêtes en révèlent d'autres~~ : mal compris, corrigé par la précision ci-dessous.
- **Profils** : seul existe le profil dev de Raphaël, qui a tout ; aucune préservation des anciens profils à prévoir. Exigence : **les déblocages et la progression des quêtes sont bien persistants**.
- **Backlog** : prévoir des quêtes d'exception pour des objets légendaires ou des armes « pétées » (exemple de Raphaël : ouvrir tous les coffres du jeu). Rattachées au plan 06 §9.9, pas dans le premier lot.

**Précision, même jour.** Raphaël : « pour moi, toutes les quêtes doivent apparaître par défaut. Il ne faut pas qu'il y ait des quêtes cachées. C'est juste que il ne faut pas que les quêtes soient marquées bloquées parce que conditionnées par une autre […] il faut qu'il y ait dans les menus une page dédiée pour justement suivre les quêtes, l'avancée des quêtes […] intégrer une page qui liste les personnages, avec les détails, etc., et avec un affichage du sprite agrandi, ça je vais me débrouiller pour faire des images plus sympas […] il y a des menus qui servent à rien, genre qui suivent les quêtes, etc., il faudrait les revoir ».

**Acquis (remplace le point « des quêtes en révèlent d'autres » ci-dessus) :**
- **Toutes les quêtes sont visibles et ouvertes dès le départ** ; aucune quête cachée ni verrouillée par une autre. L'emboîtement vient du jeu lui-même (une récompense aide la quête suivante), jamais d'un verrou.
- **Page Quêtes dédiée** dans les menus du Hub pour suivre l'avancée des quêtes.
- **Page Personnages** : liste, fiche détaillée, sprite agrandi ; Raphaël fournira des images plus soignées.
- **Revoir les menus devenus inutiles** (ceux qui suivent les anciennes quêtes, etc.).

## 91. Choix des sons manquants et piste d'ambiance — 10 octobre 2026

Raphaël transmet `choix-sons-2026-10-10.json` et précise : « je crois que ca vient de l'ambiance ». La source du frottement reste une hypothèse à confirmer par la piste isolée ; ce retour ne valide ni un remplacement d'ambiance précis ni tout le mix.

Quinze choix explicites : accent de Résurgence A, parade A, brûlure du Néant A, explosion de l'Instable A, surgissement du Rampant A, tentacule de l'Indicible A, Mémorial B, fragment rare A ; impacts lame A, choc A, verre C, feu B, papier B, électrique A ; pas d'herbe A (même son, −6 dB). Quatre refus (`aucun`) : soin, bouclier rechargé, annonce du tir visé, réveil de la Faille. Ils appellent une reprise de recherche, sans sélection automatique ni décision de supprimer un son actuel. Export original archivé hors dépôt ; découpage et intégration au plan 15 M4a/M4b.

## 92. Sons de la Barrière validés pour l'instant — 10 octobre 2026

Raphaël, dans le suivi audio et sur la page d'écoute de la Barrière : « pour l'instant je valide tout de la barrière ». Les huit sons sont validés pour l'instant : levée, verrou, battant brisé, chute de la grille, annonce et impact du poing, annonce et balayage de la chaîne. Les sons sont déjà intégrés ; cette validation d'écoute est consignée aux plans 15 et 07. Retouches possibles ultérieurement ; la recette du combat et du mix en run reste au suivi existant.

## 93. Frottement confirmé sur le bus Ambiance — 10 octobre 2026

À la question « Sur la page d’isolement, le frottement disparaît-il quand tu coupes uniquement “Ambiance” en laissant les autres pistes actives ? », Raphaël répond : « Oui, il disparaît ». Le diagnostic de famille est acquis : le frottement signalé vient du bus Ambiance. Préparer une correction comparable à l'original (plan 15 M5), en distinguant la boucle de fond des oiseaux et des effets météo. Ce retour ne choisit pas encore un nouveau timbre ni la suppression de toute l'ambiance.

## 94. Fond de forêt conservé, réservé à son biome — 10 octobre 2026

Raphaël : « le fond de la forêt, il est pas très dérangeant tant que c'est la boucle complète qui est jouée […] il faut que ça se joue que dans la forêt […] peut-être baisser un tout petit peu de 1 ou 2 décibels, pas plus ». Conserver le fichier et les huit secondes corrigées de M5a, sans le filtrage ni la suppression proposés en M5b. Baisser le gain de banque de 2 dB et réserver le fond au biome Forêt Reconquise ; les oiseaux qui l'accompagnent suivent ce biome. Météo et contexte de Résurgence gardent leur pilotage existant. Intégration et recette des passages de frontière au plan 15 M6.

## 95. L'Indicible au niveau du jeu — 10 octobre 2026, soir

Raphaël : « mais au delà des sons, le design, les animations, c'est fini ? c'est au niveau du jeu ? ». Constat : mécaniques finies, rendu au stade de maquette (Indicible jamais visible, arrivée sans mise en scène, tempête sans pluie ni éclair visible, marée et vague en aplats, mains sans animation, sons empruntés). Sur la proposition d'un lot B5 (planche de la forme découverte, puis arrivée et tempête, marée, vague et mort, dix sons) : « ouais ok ca me va. go planche ».
- **Lot B5 ouvert** ([plan 07](07-bestiaire-et-rencontres.md#lot-b5--lindicible-au-niveau-du-jeu--découpage-proposé-10-octobre-2026-soir)), planche B5a d'abord.
- **Seule la forme découverte attend sa validation** ; arrivée, tempête, marée, vague, mort et sons avancent sans attendre.
- **Forme découverte : C, l'œil** (planche B5a), choisi par Raphaël comme recommandé : un œil immense ouvert dans le sol inondé, cils en bras de noyés, la lanterne en reflet dans l'iris, qui est la cible.

## 96. Illustrations des personnages — 10 octobre 2026

Raphaël fournit douze illustrations (`~/Downloads/personnages`, compressées en WebP) : « fait en sorte que les sprites en grand dans l'écran de déblocage soient ceux là. pour les persos pas en jeu, ne les affiche pas ».

**Acquis :** le grand portrait de la page Personnages est l'illustration du personnage ; la page ne montre que les personnages en jeu. Deux illustrations du Facteur existent (à roulettes, solitaire) : celle qui servira reste à choisir quand il deviendra jouable.

## 97. Écran des quêtes — 10 octobre 2026

Raphaël demande de revoir la page du menu : des cases en rangées de quatre ou cinq plutôt qu'une liste verticale ; ne plus présenter les quêtes en cours et accomplies dans deux tableaux séparés, tout en permettant de passer clairement des unes aux autres. Il signale des textes superflus : « En une seule run » répété et parfois inapproprié, « Meilleure run », les explications à côté des étoiles et le compteur « 0 / 36 », à déplacer ou supprimer. Il pense d'abord que les personnages manquent, puis constate leur présence après avoir joué : ce n'est pas une absence du catalogue.

**Application au [plan 06 Q3b](06-personnages-quetes-defis.md#q3b--grille-de-quêtes-et-lecture-allégée--10-octobre-2026) :** grille commune de quatre cartes par rangée, toutes visibles par défaut, filtres de statut et distinction des cartes accomplies ; personnages repérables dès l'ouverture ; suppression des libellés répétitifs et du compteur global. Les restrictions utiles restent dans la description de chaque objectif.

## 98. Portée des armes de mêlée, second relèvement — 10 octobre 2026

Raphaël : « Je pense que la portée des armes corps à corps devrait être rehaussée. » Le premier relèvement (plan 24 L3, validé au §40) n'a pas suffi. Option retenue parmi trois : **+20 px, plancher 80** pour les arcs et les estocs, armes en cercle plus modérées (Rallonge 95, Cloche 110) ; le visuel du coup recalé sur l'allonge réelle (il n'en montrait que 70 %).

**Acquis :** valeurs et visuel du [plan 24 C2b](24-retours-du-1er-octobre.md#c2b-second-relèvement--10-octobre-2026). Sensation à confirmer en jouant.

## 99 bis. Chroniques retirées et icône de complétion — 10 octobre 2026

Raphaël : « l'écran chronique il sert à rien également […] le retirer totalement » ; il refuse la coche typographique des quêtes et demande « une icône custom en pixel art dans la DA du jeu à la place ».

**Acquis :** supprimer entièrement la page Chroniques du Hub et son entrée de menu. Remplacer les coches visibles par une texture originale dans la direction artistique du jeu, sur les cartes accomplies et dans leur détail. Lot [06 Q3c](06-personnages-quetes-defis.md#q3c--retrait-des-chroniques-et-icône-de-complétion--10-octobre-2026).

## 99. La mêlée ne touche pas plus loin que son coup dessiné — 10 octobre 2026

Raphaël : « je ne veux pas que la portée d'une arme soit supérieure à ce que le sprite affiche (ou pas trop quoi) ». Précisé : le problème concerne le corps à corps, pas les projectiles ; aucune arme n'est dessinée en main, le « sprite » d'un coup est sa traînée (arc, estoc, anneaux). Option retenue parmi trois : **la touche suit la traînée** (plutôt que dessiner les coups en cercle sans perspective, ou relever encore les portées).

**Acquis :** la touche d'un coup de mêlée se mesure sur l'ellipse de sa traînée, dont le bord passe à l'allonge, sans plafond ; allonge horizontale inchangée, verticale ÷ 1,35 ; détail au [plan 24 C2c](24-retours-du-1er-octobre.md#c2c-la-touche-sarrête-au-bord-du-coup-dessiné--10-octobre-2026). Sensation à confirmer en jouant.

## 100. Icône de complétion à simplifier — 10 octobre 2026

Raphaël : « pas très joli / lisible le sceau je trouve ».

**Application :** reprendre l'icône de complétion dans le [lot 06 Q3d](06-personnages-quetes-defis.md#q3d--lisibilité-de-licône-de-complétion--10-octobre-2026) : silhouette de médaille à deux rubans, moins de détails internes, contraste renforcé et captures à taille réelle. La demande d'une icône originale en pixel art, sans coche typographique, reste applicable. Nouvelle médaille ensuite validée par Raphaël : « top », avec demande de push sur `main`.


## 101. Ouverture trop dure en mêlée — 10 octobre 2026, soir

Raphaël, après des parties au Vagabond (retour dicté, extraits) : « avec le personnage de base […] qui a une arme de corps à corps, c'est vrai que le jeu est peut-être trop dur » ; « les ennemis qui tirent à distance sont trop embêtants […] ça oblige à faire des esquives tout le temps. Et c'est pas forcément le but d'un jeu bullet heaven […] le but de ce genre de jeu c'est de savoir contrôler la foule » ; « soit je supprimerai totalement l'ennemi ou je le modifierai pour qu'il ait un comportement différent » ; « certains ennemis ont trop de points de vie […] au tout début de partie, dans les premières minutes » ; régénération lente « normal », « pas le plus gros problème » ; « le format du jeu est peut-être un tout petit peu trop compressé avec un peu trop de choses qui se passent en 10 minutes » ; « la montée de niveau aussi se fait peut-être un peu lentement […] pas non plus que ça soit trop abusé » ; « prendre tout ça avec des pincettes […] pas direct appliquer des correctifs […] déjà cartographier ».

**Suite :** diagnostic mesuré et leviers proposés au [plan 33](33-equilibrage-ouverture.md), rien d'appliqué. Ce retour l'emporte sur la demande de tirs en exploration du §50 (plan 21 E4), dans le sens du §44.

**Arbitrage du même soir (questions du plan 33) :**
- **Cracheur :** raréfié tout de suite (dès 3 min, poids 0,5, retiré des pools d'exploration de la forêt, des champs et de la carrière), puis transformé en crachat en cloche qui laisse une flaque au sol (A1 puis A2).
- **Créatures solides :** entrées échelonnées (Rôdeur dès 1:30, Brute dès 3:00, Tréant dès 4:00) et Rôdeur allégé, 45 PV et 11 dégâts de base (B1 + B2).
- **XP :** ouverture et courbe entière : majoration des niveaux 1 à 5 retirée, et `base_xp` 22 → 19 (D1 + D2), plutôt que l'ouverture seule recommandée.
- **Tempo :** ouverture aérée, élites naturelles dès 2:30 et une toutes les 50 à 75 s, montée d'ouverture sur 120 s ; Souverains, Résurgences et Barrière gardent leur heure (E1).

## 102. Largeur des coups de mêlée — 10 octobre 2026

Raphaël, après §99 (portée de la mêlée) : « test la larguer dcp » (traiter aussi la largeur du coup). Appliqué sans autre arbitrage, dans l'esprit de §99 : la largeur de touche est celle du dessin. La forme suit l'attaque (trait pour l'attaque droite, traînée pour l'arc et l'onde), l'ouverture se mesure sur l'ellipse et se dessine jusqu'à 360°, l'estoc touche ce que son trait recouvre.

**Acquis :** détail au [plan 24 C2d](24-retours-du-1er-octobre.md#c2d-largeur-du-coup--10-octobre-2026). Conséquence à juger en jouant : les estocs touchent en ligne, plus en cône de 60°.

## 102 bis. Les héros un par un, le Facteur d'abord — 10 octobre 2026

Raphaël : « Rajoute-les dans characters.json, rajoute-leur un passif raccord avec leur lore / type de caractère (tous doivent avoir un passif unique) et pareil une arme de départ (prends des armes qui existent quand c'est possible sinon il faudra créer celles qui manquent). Le moyen de les débloquer se fera principalement par des quêtes. La mobilité propre : oui bien sûr. Donc pour ne pas rusher on va les faire un par un en mode vertical. Commence par le Facteur. »

**Acquis :**
- Chaque héros du plan 32 entre en jeu par un lot vertical complet : fiche, portrait, passif unique, arme de départ, quête, mobilité propre, tests et captures ([plan 06 §11](06-personnages-quetes-defis.md#11-lot-p2--le-facteur-personnage-complet--10-octobre-2026)).
- Le Facteur est livré (P2a à P2d). Portrait retenu : l'illustration « solitaire » (moustache grise, manteau usé), plus proche du sprite que celle à roulettes, qui reste disponible (point laissé ouvert au §96).
- Aucune arme existante ne portait le geste du Facteur : la Sacoche de lettres est créée, avec un nouveau motif d'attaque (rebond vers l'ennemi le plus éloigné).
- Les mobilités propres passent par des profils en données (plan 01 E) ; la glissade du Facteur est le premier.

**À juger en jouant :** valeurs d'essai (stats du Facteur, 2 rebonds à −15 %, élan +25 %/+15 % en 4 s, glissade 0,4 s à ×1,5 et 3 s de recharge) ; son provisoire de la Sacoche à auditionner.

**Suite, l'Éveillée (même soir) :** Raphaël : « ok top. personnage suivant, stp ». L'Éveillée est livrée (plan 06 P3). Sa rémanence répète le prochain **tir** seulement : rejouer une mêlée loin du corps demanderait de refaire ses traînées, et l'Éveillée est un personnage de distance. À confirmer en jouant.

**Intégration du 10 octobre :** Raphaël demande de débloquer la PR #4, dont GitHub laisse la vérification de fusion affichée. Trois conflits confirmés par API (traductions générées et décisions) ; lot de synchronisation P2e, sans nouvel arbitrage de gameplay.

## 103. Équilibrage de l'ouverture : tout appliquer, viser le niveau 30 — 10 octobre 2026, soir

Raphaël : « ça me va que tu fasses les modifications dont on a parlé. Que ce soit pour le crasher, pour la solidité des ennemis, pour l'XP, pour le rythme. Si tu vois d'autres choses, d'ailleurs, n'hésite pas à les implémenter aussi » ; XP : « être au niveau 15-20 à 10 minutes, c'est légèrement bas. Faudrait plus viser à peu près le niveau 30 […] si on fait une plutôt bonne run. Voire potentiellement même un peu plus si on fait une excellente run. Avec un bon build » ; méthode : « essaye de tout faire d'un bloc […] Fais tous les changements et lance éventuellement des tests à la fin ».

**Appliqué, à valider en jouant :** lot Q1 du [plan 33](33-equilibrage-ouverture.md#7-q1-livré--10-octobre-2026-soir), avec trois ajouts de l'agent : pente des PV de 4 à 8 min adoucie, cadence des tirs ennemis en données et ralentie, portée de vol des tirs bornée. Cible d'XP retenue : environ 30 à 10 min pour une bonne run, davantage pour une excellente.

## 104. Arrêter les sons à la mort — 10 octobre 2026

Raphaël : « il y a un bug chiant quand la partie est fini (mort) les sons ne s'arretent pas il faudrait les stopper tous (surtout ceux des ennemis/armes) ».

**Appliqué et vérifié :** arrêt immédiat des sons de la run et blocage de leur relance pendant la mort et le bilan, puis réactivation à la prochaine run ; lot M7 du [plan 15](15-audio.md). L'audio dédié à la séquence de mort et à l'interface de bilan reste disponible. Régression avant/après, smoke/audio/musique et mort réelle après combat verts.

## 105. Menus : organisation et direction visuelle à reprendre — 10 octobre 2026

Raphaël demande de réfléchir à une amélioration globale des menus : possibles doublons, moins d'allers-retours, rôles clairs et séparés, finition d'un jeu professionnel. Il juge plusieurs écrans trop basiques et trop proches d'interfaces web, avec des fonds semi-transparents ou flous ; il souhaite davantage de profondeur et de direction visuelle dans les boutons, les encadrés et les effets. Il demande d'examiner aussi le fond du menu principal et inclut explicitement le menu pause.

**Acquis :** préparer le diagnostic et un plan de correction UX/UI et artistique. Le retrait des Chroniques (§99 bis), la page Quêtes et la page Personnages dédiées (§90/97) restent le cadre courant.

**Propositions de l'agent, à examiner :** navigation directe entre Personnages, Quêtes et Collection ; choix confirmé et départ depuis la fiche de héros ; liens ciblés entre récompense et quête ; pause avec build compact et détail sélectionné ; matière de métal patiné, comparée à une piste pierre/Effacement. Fond actuel conservé dans la première piste ; monnaie sans dépense et graine sortiraient de la hiérarchie principale. Diagnostic rendu, contrats de parcours et lots M0–M5 au [plan 04 §7](04-interfaces-et-hub.md#7-refonte-des-menus--étude-du-10-octobre-2026).

**Non acquis :** une piste graphique retenue, le remplacement du fond, la suppression du solde/graine, les détails des nouveaux parcours et leur implémentation. Cette demande de conception ne coche aucune case de roadmap.

## 106. Menus intérieurs : esquisses rejetées, accueil conservé — 10 octobre 2026

Raphaël préfère, entre les deux pistes, « le truc avec la pierre et l'effacement », mais rejette nettement **les deux esquisses** : « ça fait beaucoup trop web ». Il demande de retrouver les codes visuels des menus de jeux, une patte forte, le pixel art et le thème de l'oubli ; il invite à relire le lore. Il précise ne pas avoir lu tout le plan proposé.

**Acquis :** conserver pour l'instant le premier menu au lancement, qu'il apprécie ; réorienter la recherche vers l'intérieur de Personnages, Quêtes, Collection et Paramètres. La pause reste incluse par sa demande précédente. Regrouper Paramètres à côté de Quitter en bas lui plaît, à distance des entrées principales.

**Préférence, sans validation du rendu :** explorer Pierre et Effacement. Le métal patiné n'est plus la recommandation. Aucune des anciennes esquisses ne devient une référence approuvée ; aucun accord global sur les parcours ou le découpage précédent ne se déduit de ce retour.

**Étude révisée :** [plan 04 §7](04-interfaces-et-hub.md#7-refonte-des-menus--étude-du-10-octobre-2026), lecture du lore v1.1 validé, références de menus de jeux et [nouvelle planche Personnages](planches/04-menus-personnages-pierre-v2.png). Pierre de la ville, contours effacés, illustration dominante et contrôles dessinés ; lots centrés sur un écran intérieur de référence avant déclinaison. Le déplacement de la graine, le retrait du solde, le changement du fond et la refonte du bilan/Journal/choix sont sortis de cette passe. Cette planche et les propositions UX restent à examiner ; aucun code de jeu modifié ni item de roadmap coché.

## 107. Passe de mix : tout appliquer d'un coup — 10 octobre 2026, soir

Raphaël demande d'abord de baisser les armes du joueur (« sur-mixé »), puis une grande revue du mix. La revue mesure la banque entière et une run enregistrée par familles ([plan 15](15-audio.md#revue-de-mix--10-octobre-soir)). Résultat : le « trop fort » venait surtout du coup de l'Ombre (presque tout le volume des effets), du critique et du coffre, pas des attaques d'armes, devenues inaudibles. Réponse de Raphaël : « tu as déjà bien identifié la plupart des problèmes que j'ai moi-même remarqués. Je valide 100 % de tes propositions et remarques. Fais les corrections directement […], juste éventuellement une passe finale. »

**Acquis :** lots X1–X5 appliqués sans écoute intermédiaire. Limiteur Master ; bus World étouffé et compressé, hors interface ; ducking des grands moments déclaré dans la banque ; boucles en fondu ; atténuation à distance des sons d'ennemis. Gains recalés par famille sur la sonie mesurée : Ombre −10 dB, armes égalisées et remontées, critique +5 dB au-dessus de l'impact, pas homogènes, musiques à −16 LUFS (Résurgence et nuit à −15). Fichiers corrigés : silences de tête, phases, pas d'herbe ; sons morts retirés. Mesure : 140 → 0 échantillons saturés. **À juger en jouant :** mix plus calme de 4 dB, musique légèrement au-dessus des effets.

## 108. Calendrier de 30 min jusqu'à l'Indicible, late game à étudier — 10 octobre 2026, nuit

Raphaël, après Q1 : « remettre 50 hp de base pour le rodeur et 12 dégâts. aussi les 2 miniboss prévus dans les 10 min arrivent trop tôt […] il faut revoir le calendrier de la run. un truc qui tient en 30 min (moment de l'indicible) : après on sera dans le late game. dans ces 30 min il faut caler environ 4 résurgences, le boss intermédiaire (la barrière), 3/4 "miniboss", des élites. Avant tout ça tenait en 22 min je crois. Et le late game, que se passe-t-il ? » Calendrier à quatre Souverains choisi parmi trois. Phases : « pour moi le late game c'est ce qui se passe après l'indicible. après à partir de 20 min on est en mid-game et à 30 on est déjà au début du late game. mais je pensais plus au late game genre qu'est-ce qu'on prévoit quoi ? » ; étude du late game demandée tout de suite.

**Appliqué, à jouer :** Rôdeur 50 PV et 12 dégâts. Calendrier : Souverains à 4:00, 9:30, 21:00, 27:00 ; Résurgences à 6:00, 12:00, 18:00, 24:00 ; Barrière à 15:00 ; Indicible avec la 5ᵉ Résurgence vers 30:00 ; late game à partir de lui. Effacement étiré d'autant, montée des PV adoucie après 8 min, réserve de la Barrière recalée sur 15 min, quêtes de niveau remises à 30. Détail et mesures : [plan 33 §8](33-equilibrage-ouverture.md#8-calendrier-de-30-min-jusquà-lindicible--10-octobre-2026-nuit).

**Proposé, à trancher :** contenu du late game au [plan 34](34-late-game.md) (Profondeur, Marées, monde d'après, butin, sortie volontaire).

## 109. Late game validé dans l'ensemble, inventaire et progression permanente à repenser — 11 octobre 2026

Raphaël, après le plan 34 (retour dicté, extraits) :
- **Late game** : « ta proposition globale pour le late game tient » ; sur Megabonk, « il y a bien un late game "infini" avec juste des vagues d'ennemis en boucle. je pense qu'on peut faire un peu mieux. mais […] avoir des vagues de plus en plus dures, ça me va » ; « la profondeur aussi me va, mais […] je ne crois pas qu'il faille en faire […] une sorte de compteur en plus. Je pense que sur le jeu, il manque le fait de montrer in-game sans faire pause le péril de la partie […] l'avoir tout le temps […] en late game il faut qu'il augmente tout seul, continuellement » ; « c'est pas mal d'avoir des niveaux de profondeur genre dire ouais profondeur 14 […] faut voir » ; Marées : « ça me va, c'est cool justement, ça change des résurgences classiques […] vu que l'eau est revenue dans le lore du jeu » ; « avoir une sorte de boss continuel, ça peut être bien » ; monde d'après : « hyper bien », mais « un gros, gros coup visuel, ça peut pas se faire à la va-vite […] peut-être pas la plus grande priorité ».
- **Butin et inventaire** : « il y aura un moment où toutes les armes, elles seront maxées. Tous les objets aussi. […] ce qui aurait été bien, ce serait que comme Megabonk, qu'on ait un système avec des objets à récolter à l'infini » ; « qu'on repense encore une dernière fois à l'inventaire du joueur » ; « un nombre limité d'armes on va dire 4, un nombre limité de tomes — là j'appelle ça des tomes mais […] ça sera autre chose — pareil ça pourrait être 4, un nombre potentiellement illimité d'objets » ; « si on veut être différent de Megabonk […] un inventaire de 10 objets […] qui peut être augmenté » ; « globalement 10, 20, 25 objets […] mais pas non plus infini » ; « des objets uniques […] meilleures améliorations, […] te téléporter […] du tout court de distance, […] un revive, […] des dégâts de poison […] le but c'est pas d'en avoir 30 ou 40 du même » ; « j'ai peur qu'avec 4 armes et 4 tomes […] ça soit pareil que Megabonk ». Les objets actuels « ont été pensés comme les tomes de Megabonk » : leurs concepts passent dans la nouvelle entité.
- **Progression permanente** : « il y a des améliorations globales que tu peux faire, par exemple j'améliore le nombre d'armes que je peux porter, j'améliore des statistiques […] ça manque et ça serait crucial de rajouter ça rapidement ».

**Acquis :**
- Late game : vagues de plus en plus dures (Marées) et boss qui revient ; Péril visible en permanence en run et montant seul en late game, sans second compteur ; monde d'après à faire plus tard, sérieusement. Lots révisés au [plan 34 §8](34-late-game.md).
- Inventaire : 4 armes ; une étagère d'améliorations répétables, une stat chacune, au nom à trouver (pas « tomes ») ; des objets à effet unique, en nombre plus grand que les armes mais limité, sac agrandissable. Ce choix **remplace §33–§34** (6 objets à niveaux) et rouvre §21 en assumant la proximité avec Megabonk, avec des différences à trouver.
- Progression permanente à ajouter, prioritaire : elle contredit le GDD §5 (« jamais de puissance brute »), que ce choix remplace.

**Proposé, à trancher :** [plan 35](35-inventaire-du-joueur.md) (Habitudes, sac, sources des objets) et [plan 36](36-progression-permanente.md) (l'Établi, ses rayons, son économie).

## 110. Écussons, sac de 10, Campement, armes de 2 à 4 — 11 octobre 2026

Réponses de Raphaël aux questions des plans 35 et 36 :
- Nom de la deuxième étagère : Habitudes, Gestes, Ancrages, Cicatrices refusés, « tes premiers concepts sont trop flous il faudrait plus des objets concrets ». Retenu parmi quatre familles concrètes : **les Écussons**, cousus sur la veste, un par stat (« Écusson du boxeur » ×7 pour les dégâts…).
- Montée des Écussons : **au level-up et par l'usage** (option H2).
- Objets à effet unique : **trouvés dans le monde** (coffres, élites, Souverains, Ateliers, Marées), plus au level-up ; **sac de 10**, agrandissable ; doublons permis ; sac plein, on en oublie un.
- Emplacements d'armes : « commencer à 2 et pouvoir monter jusqu'à 4 », achetés en progression permanente.
- Remboursement des achats permanents : **libre et gratuit**.
- Nom de l'écran : « établi est aussi un truc in game donc ça va pas trop » ; retenu **le Campement** parmi quatre.
- Ordre : « péril visible, établi puis refonte de l'inventaire » : Péril toujours visible (plan 34 G0), puis le Campement (plan 36), puis l'inventaire (plan 35).

## 111. Curseurs à 100 %, sons placés à l'écran, nouveaux sons à proposer — 11 octobre 2026

Raphaël, après X6 : « les curseurs musique et ambiance, comme tous les autres, doivent être à 100 % par défaut. C'est ce que font tous les jeux. » Le placement gauche/droite des coups et des ennemis, « c'est génial ». Pour les quatre sons manquants : chercher sur le web ou les créer, puis proposer. Il n'aime pas le son de blessure du joueur : en trouver un plus approprié.

**Acquis :** tous les curseurs audio à 100 % sur un profil neuf ; l'équilibre interne (musique −6 dB, ambiance −8 dB) passe hors curseur. Les réglages enregistrés avant gardent leur niveau entendu. Sons du monde placés à gauche ou à droite selon leur source à l'écran (plan 15 X7). **En attente de son écoute :** planche de candidats pour le soin, le bouclier rechargé, l'annonce du tir visé, le réveil de la Faille et la blessure du joueur.

## 112. Pas de son de créature quand son coup touche le joueur — 11 octobre 2026

Raphaël remarque que la blessure du joueur sonne très différemment en jeu (« beaucoup plus métallique et fort ») que le fichier seul. La cause : à chaque coup au contact, le jeu jouait aussi le son d'attaque de la créature. Celui de l'Ombre, du Charognard et du Rampant d'ombre est un frottement métallique écrêté, deux fois plus long que la blessure, et il la couvrait. Trois options lui sont présentées : retirer ce son, le remplacer ou le baisser. Il choisit de le retirer : « ne pas avoir de son de créature qui nous touchent ».

**Acquis :** un coup au contact ne joue que la blessure du joueur. Les sons des créatures restent sur leurs actions (bond, cri, tir, terrier, crachat). Le tir de la Sentinelle garde son son. Les trois sons de contact (`sfx_ombre_attaque`, `sfx_rodeur_attaque`, `sfx_brute_charge`) et le champ `attack_audio` des sept créatures de corps à corps sont retirés.

## 113. Choix de la planche du 11 octobre — 11 octobre 2026

Raphaël, après écoute :
- **Blessure du joueur :** il garde le son actuel. Il est désormais seul à jouer au contact (§112).
- **Soin reçu :** une création d'après sa référence est d'abord demandée, puis il renonce : « aucun son pour les soins reçus, ça me va, sinon ça va être la galère à gérer ».
- **Bouclier rechargé :** aucun son, pas nécessaire.
- **Annonce du tir visé :** C, bois qui grince sous la tension (synthèse originale).
- **Réveil de la Faille :** G, gong inversé et déchirure (BigSoundBank, CC0).

**Acquis :** le tir visé fait partir son grincement en cours de route, pour qu'il culmine au tir quelle que soit la durée de visée de la créature. Il est placé à l'écran et s'atténue au loin. La Faille joue son réveil au moment où on la réveille, et la musique baisse brièvement. Le soin et le bouclier restent muets, besoins clos.

## 114. Carte blanche pour la refonte des menus sur une branche séparée — 10 octobre 2026

Raphaël autorise l'implémentation : « refais tous les menus comme tu l'entends », sur une branche autre que main. La nouvelle image est jugée « pas mal » ; il préfère un nom de personnage discret et moins de statistiques d'armes et de mobilité. Il propose, sans l'imposer, Partir / Recommencer → choix du personnage → lancement ; le camp pourrait montrer les héros débloqués, ce qui évite d'y sélectionner un casting de dix personnages. Il demande à l'agent de garder son jugement et de travailler.

Choix de l'agent dans ce mandat : adopter cette étape de sélection, conserver l'écran titre et son camp d'ambiance, privilégier portrait / style de jeu / départ, et réserver les règles détaillées à une fiche facultative. Production en lots P1–P3 au plan 04 §8 dans un worktree local, branche `codex/refonte-menus`. Le transfert de cette conversation vers le cloud n'est pas disponible via l'outil de transfert ; aucun travail parallèle cloud créé.

**Livraison de ce mandat :** lots P1–P3 implémentés et vérifiés sur cette branche, sans fusion : nouveaux menus intérieurs, liens de consultation, pause et paramètres ; build sans warning, suites 7/7 puis 2/2, captures 720p/1080p et relance réelle avec changement de héros. Compte rendu et limites au [plan 04 §8.2](04-interfaces-et-hub.md#82-validation-et-captures--p1p3-livrés). La livraison ne vaut pas approbation artistique de Raphaël.

## 115. Terminer la refonte de tous les menus — 11 octobre 2026

Raphaël : « termine tout alors, y compris montée de niveau, butin, bilan et journal, dans la même direction graphique et sur la même branche. Va jusqu’au bout et teste le tout avant de me dire que c’est fini. »

Mandat : achever les écrans restants dans la direction pierre/Effacement de P1–P3 sur `codex/refonte-menus`, avec vérification fonctionnelle et visuelle. Lots P4–P7 au [plan 04 §9](04-interfaces-et-hub.md#9-achèvement-de-tous-les-menus--11-octobre-2026). L'accueil et le camp restent conservés. Aucune fusion dans `main` demandée.

**Livré et vérifié :** P4–P7 terminés, dans la continuité de P1–P3 : montée de niveau, rencontres, butin, journal accessible depuis la pause et bilan. Build zéro warning, 15/15 suites générales puis 5/5 finales, captures réelles 720p/1080p avec texte agrandi et relance avec un autre personnage. [Preuves et limites](../audits/menus-2026-10-11/README.md). Aucun accord artistique de Raphaël ni fusion vers main présumés.

## 116. Questions du Campement et de l'inventaire acceptées, refonte lancée — 11 octobre 2026

Raphaël, après le compte rendu du Campement et du lot I0 : « Ok, ces points me vont. continue avec le deuxieme chantier sur la meme branche stp. »

**Lecture retenue** : les propositions et recommandations des plans 36 §13 et 35 §9.5 sont acceptées. Là où deux options étaient posées sans recommandation, l'agent prend la première, et le dit ici pour que Raphaël puisse la corriger :
- **Campement** : noms des 13 améliorations gardés ; « Quatre airs » garde quatre armes et passe en ★★ ; armure +1 par rang gardée ; 4ᵉ emplacement à 2 500 gardé.
- **Écussons** : noms de la table 35 §9.1 gardés ; l'**arbitre redevient un objet** du sac (option A, le Péril reste un choix) ; un seul palier, au niveau 15 (première option) ; l'usage donne environ un quart des niveaux en 30 min ; un fil sous chaque Écusson au HUD, sans bandeau.
- **Sac** : sac plein, l'objet reste au sol avec une invite à maintenir, qui ouvre l'écran d'échange en pause ; 3 à 5 nouveaux objets rejoignent la réserve de départ ; un objet sans niveaux vaut d'abord son niveau 10 d'aujourd'hui, et son palier du niveau 15 devient l'effet d'un doublon.

**Suite** : refonte de l'inventaire (plan 35, lots I1 à I7, découpés au §10), puis les Poches du Campement (plan 36 M4).

## 117. Retours sur les livraisons du 9 au 11 octobre — 11 octobre 2026

Raphaël, après avoir joué : la refonte des menus « dans la globalité, c'est cool », le menu de level-up « très bien fait, c'est assez stylé », « le son c'est beaucoup mieux », « la courbe de difficulté elle est mieux ». Ses demandes, dans ses mots quand ils comptent :
- **Partir** : la sélection des personnages seule, sans onglet vers Quêtes et Collection. Bouton Partir « pas assez visible ». La liste des personnages « se détache pas assez de l'arrière-plan », pas de cadre, survol en « fond semi-transparent sans contour, sans habillage ».
- **Pages séparées** : depuis Campement, ne pas pouvoir aller vers Quêtes et Collection, « autant garder des vraies pages séparées ».
- **Quêtes** : « je t'ai dit que je voulais pas un truc avec Tout, En cours et Accompli, il faut juste un seul affichage », accomplies distinguées par la couleur ; cartes trop grandes.
- **Collection** : pas de bouton pour afficher valeurs et règles, souvent une seule ligne.
- **Réminiscences** : toutes débloquées par défaut, « elles devraient n'avoir que quelques-unes de dispo de base, et toutes les autres via des quêtes ». Icône de Mémoire vive à refaire.
- **Campement** : « on comprend pas ce que ça fait », il faut un sous-texte qui explique.
- **Curseur** custom, de base et au survol.
- **Chargement** : l'animation « lag beaucoup ».
- **Minimap** : trop basique, brouillard trop simple, un peu trop dézoomée.
- **Menus en run** (level-up, coffre, pause) : garder les panneaux, mais revoir le jeu derrière, « un peu transparent », « un petit effet pour qu'il soit pas full visible ».
- **HUD** : Péril pas à côté du chrono, plutôt à droite près du score ; score et niveaux sans encadrés, éliminations écrites trop petit.
- **Barre du bas** : Écussons plus grands, objets ailleurs (« à gauche de l'écran en vertical »), pas tout au même endroit.
- **Routes** de la ville trop en ligne droite, « pas en vue isométrique », surtout le bord du haut.
- **Cercle** de la Veillée pas en pixel art ; certains pixel art n'ont pas la même qualité.
- **Transitions** entre zones : « des pixels dans tous les sens », à rendre plus doux.
- **Ennemis** : seuls sprites non repris récemment, en retravailler certains.

**Choix du jour (questions posées) :**
- Réminiscences : 3 de base, 4 par quêtes, et « préparer les autres » (quêtes des 7 à venir).
- Quêtes en lignes compactes, accomplies en fin de liste. Curseur flèche pixel art et main au survol. Collection : tout affiché, défilement si besoin.
- Menus de run : jeu flouté et assombri. HUD : texte posé avec contour. Objets en colonne à gauche sans cadre. Minimap **ronde, façon boussole** (contre la recommandation carrée).
- Routes : mêmes axes, bords en perspective iso. Transitions : lisière organique en amas. Ennemis : Rôdeur, Charognard, Présage et Tisseuse d'abord.
- Ordre : Hub, puis run, puis monde.

Lots au [plan 37](37-retours-du-11-octobre.md).

## 118. Retour de santé basse — 11 octobre 2026

Raphaël demande d'adapter l'affichage quand le joueur est bas en vie : une vignette « rouge sang », un bruit de cœur qui bat, et éventuellement d'autres détails ; « Prépare ça ».

Lot S1 au [plan 38](38-sante-basse.md). Les seuils, le tempo et l'intensité sont des réglages initiaux proposés par l'agent, à éprouver en jeu ; aucune validation artistique présumée.

### 118 bis. Vignette plus douce et arrondie

Raphaël n'aime pas le premier rendu : « un truc plus dégradé avec plus d'arrondis et éventuellement des effets pixels/disparition en lien avec le thème du jeu ». Reprise visuelle au lot S2 du [plan 38](38-sante-basse.md) : halo elliptique, dégradé doux et matière qui se dissout par amas de pixels.

## 119. Atmosphère et détail du monde — 11 octobre 2026

Raphaël demande : « Comment rajouter plus d'atmosphère et de détail au jeu ? (pense large/vue d'ensemble stp). sois créatif. sors moi une liste de recommendations par priorités et estimation de temps nécessaire. » Sur la liste proposée : « oui crée le plan ».

[Plan 39](39-atmosphere.md). Rien n'est validé au-delà de la création du plan : ordre, sources sonores, météo, lumière par run, musique et anomalies restent des questions (§7).


### 119 bis. Réponses aux six questions du plan 39

Posées le 11 octobre avant le lot A1 :

1. **Ordre :** phase A dans l'ordre du plan (A1 → A5).
2. **Sources des ambiances (A4) :** candidats issus de banques libres, présentés dans une planche d'écoute comme au plan 15 M4 ; Raphaël choisit.
3. **Météo (A5) :** ambiance, plus la portée de vue réduite par le brouillard épais. Pas d'autre effet de jeu.
4. **Lumière par run (C6) :** refusée sous la forme proposée : « c'est pas le mieux d'avoir une lumière au hasard par run. Je remettrai peut-être un cycle (sans nuit trop noire) ». C6 est retiré ; un cycle de lumière sans vraie nuit reste une idée de Raphaël, à reprendre par lui, en exception assumée au pivot V2.
5. **Musique en couches (C3) :** plus tard, on garde les morceaux actuels.
6. **Anomalies (C7) :** à réactiver, après les phases A et B. Accord de principe sur le plan 14 et sur le retour des anomalies de `scripts/World/Lore/` redessinées ; le détail du plan 14 reste à arbitrer lot par lot.

### 119 ter. Étalonnage adouci, A2 lancé

Sur A1 (11 octobre, soir) : « l'étalonnage est peut-être un peu fort quand on passe d'un biome à l'autre », puis « continue un dernier coup ». L'agent dose tous les étalonnages à 65 % (`grade.strength` de `data/world/atmosphere.json`) et allonge le fondu de 1,5 à 2,5 s ; ces deux valeurs sont des propositions, à juger en jouant. Lot A2 (ombres de nuages) réalisé dans la foulée.

## 120. Coffres réservés aux objets du sac — 11 octobre 2026

Raphaël : « dans les coffres on ne peut plus obtenir que des objets. retire tous les autres types de loots possibles. » Il demande aussi les raretés des objets, certains lui paraissant plus puissants que d’autres.

Acquis : tous les coffres donnent seulement des objets du sac, y compris l’ancien coffre de lore ; suppression des gains d’Essence, XP, niveaux d’Écussons, Souvenirs et statistiques. Un sac plein laisse l’objet au sol pour l’échanger, sans conversion en Essence. Lot I9 au plan 35. Les objets n’ont actuellement aucune rareté propre ; la question ne vaut pas validation d’un classement ou de probabilités nouvelles.

## 121. Rareté fixe des objets et fréquence liée à leur puissance — 11 octobre 2026

Raphaël : « il faudrait que les objets aient des raretés par défaut. Rareté fixe par objet. proba de l’avoir game lié à sa rareté. Puissance de l’objet liée à sa rareté aussi (les classer dans les bonnes raretés). prépare ca. »

**Acquis :** chaque objet a une rareté de catalogue fixe ; sa fréquence dépend de cette rareté ; classer les effets existants selon leur force réelle. **Préparation livrée :** [plan 35 I10](35-raretes-des-objets.md), 22 objets, cinq rangs, tables par source et lots d’implémentation. Le classement précis et les taux sont proposés, pas encore mesurés. La puissance justifie le rang, sans multiplicateur automatique ajouté. Aucune modification du gameplay dans cette préparation.

## 122. Direction « crade » — 12 octobre 2026

Raphaël, sur les tuiles du plan 37 : « même s'il y a du mieux je reste pas 100 % convaincu […] ça rend mieux sur le marais que la forêt et la forêt rend mieux que la mine », puis : « je suis à la recherche d'une certaine esthétique qui va au-delà du pixel art […] ça vient des BD / images que j'ai pu lire plus jeune. Un truc un peu mystérieux, crade, indescriptible. » Après les pistes proposées : « en JV Rain World est très sympa et en BD tout le Métal Hurlant […] Beksiński aussi top. Je suis d'avis qu'il faudrait rendre la DA un peu "crade" » par une palette salie, du grain, des hachures dans les ombres et des taches. Mises en garde : « ça peut vite devenir grossier ou forcé dans le trait », « le jeu a déjà beaucoup d'assets ». Sur la maquette hors jeu : « ça me va avec un dosage léger pour l'instant ».

**Acquis :** direction crade au dosage léger, par un traitement commun des shaders du monde, sans redessiner les assets ; lots G1 à G3 au [plan 40](40-direction-crade.md). **Non tranché :** la mise à jour des références de la Bible §4.

Précision le même jour, sur des pistes proposées ensuite (récitatifs, cartouches, Effacement en dessin qu'on gomme) : « tu vas trop en mode BD, c'est pas le format BD qui m'intéressait, c'était plus l'esthétique ». **Acquis :** la direction emprunte l'ambiance et la matière de ces images (mystérieux, crade, indescriptible), jamais le support BD.

## 123. Textes de l'écran de chargement — 12 octobre 2026

Raphaël demande des textes plus éloignés de l'animation, « plus mystérieux et
variés », plus nombreux, ainsi que des conseils de jeu. Le placement peut être
plus haut ou plus bas.

Après un début d'intégration prématuré, il précise : « je voulais que tu me
fasse des propositions avant de tout écrire. » **Acquis :** présenter et
faire choisir la direction avant de rédiger le catalogue complet et de coder.
Les changements de code et de textes commencés ont été retirés. Aucun choix
précis de placement, de quantité, de ton ou de rythme n'est validé.
Propositions à discuter au [plan 24 B5b](24-retours-du-1er-octobre.md).

### 123 bis. Placement accepté, textes indirects et artefacts

Raphaël accepte le placement **en dessous**. Il refuse les exemples proposés,
encore trop directs sur l'oubli, et renvoie aux règles d'écriture déjà posées
au plan 19. Il demande des formes variées et immersives : poèmes, transmissions,
annonces, etc. Les conseils concrets sont acceptés, mais **ne doivent pas être
identifiables visuellement des autres textes** : aucun préfixe « Conseil ».

Il souhaite des artefacts donnant l'impression de ne lire que des fragments,
« sans que ça devienne cliché ». Le dosage et les exemples restent à discuter ;
pas de rédaction du catalogue complet ni d'intégration avant ce retour.

### 123 ter. Ton accepté et intégration autorisée

Raphaël accepte les seconds exemples (poème, transmission, annonce coupée,
note et conseil). Il demande « légèrement plus » d'artefacts, adaptés au type
de phrase, puis : « fait le reste go et implémente le ». Lot B5b autorisé dans
cette direction, sous l'animation, avec les conseils mêlés aux autres textes
sans titre ni présentation distincte.

### 123 quater. Fond de chargement moins détaillé

Raphaël : « jaime pas l'image de fond elle est trop détaillée », puis demande
une version plus pixellisée, jolie et dans la DA, en regardant le fond du menu
principal. Il demande également si les fonds et bordures des différents menus
sont des PNG ou des dessins du moteur. Lot B5c : fond dédié au chargement avec
le grain de l'accueil ; réponse sur les techniques réellement utilisées.

## 124. Ambiances choisies, licence et voile du marais — 12 octobre 2026

Choix de Raphaël sur la planche A4b ([plan 39 §11](39-atmosphere.md)) : marais B (mare), bulles A, grenouille C ; carrière gouttes B, pierre A, et pour le fond « A et C » ; champs B (soleil et chaleur), corbeau A, grillon A ; Hub B (grand feu de branches) ; ville tôle C (bâche qui claque) et, pour la chute, « 1 et 3 » (barre de fer et pierres). Fond de ville : aucun, « j'hésite entre le A et le C car le A a des bruits de fond type voiture et le C fait très "montagne" / altitude… soit l'un des deux soit chercher un nouveau ».

Question de Raphaël : ces sons sont-ils utilisables en licence commerciale ? Réponse : oui, tous sont CC0 (BigSoundBank, Joseph SARDIN), usage commercial libre sans attribution obligatoire ; crédits tenus dans `assets/audio/CREDITS.json`.

« Corrige le voile du marais stp » : voile réaffiché (il ne l'avait jamais été), à une force de 0,6 proposée par l'agent, à juger en jeu.

## 125. Carrière à deux fonds, vent de la ville — 12 octobre 2026

Sur l'intégration de A4c : « juste un détail sur la carrière j'aime les deux A et C mais ne les mets pas en même temps. Pour le vent le dernier me va bien : vent fort dans un village. »

**Acquis :** la carrière a deux fonds (grotte, vent en volume clos), l'un ou l'autre ; l'agent tire celui qui joue à chaque entrée dans le biome. La ville prend le vent fort dans un village (second tour, D).

## 126 bis. Régénération réduite et suspendue après blessure — 12 octobre 2026

Raphaël demande environ −10 % de régénération de base pour tous les personnages et un délai de 2 s après un coup. Lot R1 au [plan 33](33-equilibrage-ouverture.md) : cinq taux multipliés par 0,9, régénération suspendue après chaque perte de PV et délai relancé par toute nouvelle blessure.

Après le rééquilibrage récent de l'ouverture, les ennemis lui semblent faire trop peu mal : 10 à 15 coups pour mourir. Il envisage de relever un peu leurs dégâts en début de partie ; ce retour ne fixe pas de hausse et les dégâts ennemis restent inchangés dans ce lot.

### 126 ter. Mesurer les dégâts avec les cinq personnages

Raphaël : « mesure le truc des dégats ennemis et fait moi des suggestions en fonction de ce que tu trouves (teste avec les différents persos en jeu) ». Mesure demandée, sans autorisation d'appliquer les propositions.

Lot R2 au [plan 33](33-equilibrage-ouverture.md), [rapport](../audits/damage-opening-2026-10-12/README.md) : cinq personnages × quatre graines × 10 min en vraie Main, profil neuf, Péril 0, bot nomade mortel qui visite. Le ressenti de 10–15 coups est cohérent avec l'ouverture ; la Forgeuse résiste à environ 29 coups moyens, grâce à ses PV et son armure. Proposition de l'agent : +1 dégât de base au Charognard, à l'Ombre et au Rampant d'Ombre, qui dominent les blessures des deux premières minutes ; garder d'abord les dégâts des autres créatures. Variante plus douce : +0,5. Projections chiffrées au rapport, aucun dégât de gameplay modifié.

## 126. Ombres portées pixelisées, réglage de qualité — 12 octobre 2026

Raphaël, sur les ombres portées d'A3 : « les ombres ne me plaisent pas trop : elles ne sont pas pixellisées donc ça dénote du reste. Aussi pas besoin qu'elles soient en HD, elles peuvent être en relativement low res. Il faut un réglage pour la qualité des ombres au passage. »

**Acquis :** ombres portées en gros pixels alignés sur le monde, plus basse résolution que le dessin ; réglage de qualité dans les paramètres. Valeurs proposées par l'agent : Hautes (pixel du monde), Moyennes (2 px, par défaut), Basses (4 px), Désactivées.

## 127. Fonds de menus et Campement trop dense — 12 octobre 2026

Raphaël signale le même fond trop détaillé dans les autres menus. Sur le
Campement : « il est trop dense […] on peut s'y perdre rapidement », retirer
« Rang » devant `0/X`, envisager des icônes pixel art visibles (« pas une
bouillie de pixel ») et réadapter la largeur. Fond partagé au plan 24 B5d ;
reprise de lisibilité au plan 36 M5. Préférence de disposition demandée : liste
et fiche séparées, ou deux colonnes aérées.

Sans réponse sur la disposition pendant le travail, l'agent retient et annonce
l'option recommandée : liste et fiche séparées, commandes d'achat et de
remboursement dans la fiche. Les noms et leurs explications courtes restent
sur chaque ligne. Ce choix reste un choix de l'agent, sans validation artistique
humaine présumée.

## 128. Objets : cumul simple, déclenchements bornés et catalogue élargi — 12 octobre 2026

Raphaël corrige la proposition de raretés : pansement accepté à 3 % des PV max par niveau et par copie, sans invulnérabilité ; Semelle à +20 % par exemplaire après le dash ; Montre remplacée par une chance de ralentir un élite ; Allumette, Glaçon et Flacon à faible probabilité, en évitant l’emballement des salves ; Thermomètre et Épingle à +35 % ; Tabouret en dégâts progressifs à l’immobilité jusqu’à +100 % ; Dé jugé trop fort, à limiter par critiques ou par débit. Les autres effets explicitement acceptés sont conservés hors bonus de doublon.

**Règle globale acquise : aucun effet supplémentaire au doublon pour les objets du sac**, cumul du même effet seulement. Les paliers des Écussons ne sont pas visés. Raphaël demande aussi des objets pour toutes les statistiques, des effets variés et originaux à chaque rareté, davantage d’épiques/légendaires et une partie d’entre eux accessibles dès le profil neuf. Inspiration demandée : https://megabonk.fandom.com/wiki/Items, adaptée à Vestiges et à son lore.

**Préparation révisée :** [plan 35 I10](35-raretes-des-objets.md) et [catalogue V2](35-catalogue-objets-v2.md). 22 objets révisés, 35 nouveautés proposées, 19 statistiques globales couvertes. Le choix de plafonner le Dé sans imposer les critiques, les taux de déclenchement, durées, plafonds, nouveaux effets et accès précis sont des propositions de l’agent, pas des valeurs validées par Raphaël. Aucun effet de gameplay implémenté pendant cette révision.

## 129. Retours de partie sur l'atmosphère et l'interface — 12 octobre 2026

Raphaël, après une partie (retours dictés, extraits) :

- **Marais :** « un effet qui donne envie de vomir. Il faudrait vraiment l'enlever, immédiatement… tout l'écran qui bouge ». C'est le voile du marais réaffiché le matin même (§124) : coupé aussitôt.
- **Ombres :** « bien réussi » ; ombres des nuages dans les champs « bien gérées » ; mais les ombres des arbres « trop pixelisées par défaut ». Défaut repassé au pixel du monde.
- **Particules :** « cool, mais peut-être un peu trop simples » ; en ville « juste des petits carrés qui bougent… je vois pas trop l'effet qui est censé y avoir pour la ville ».
- **Taches de la direction crade** (plan 40) : « bien ».
- **Colonnes de lumière** au-dessus des coffres et de certains lieux : « pas ouf… ça se voit qu'elle n'a pas été refaite récemment », à améliorer.
- **Carrière :** autour d'un lieu à briser, « un espèce de rectangle semi-transparent au sol, pas du tout dans la DA du jeu », à revoir.
- **HUD** (niveau, PV, score, éliminations, Essence, Péril), refait la veille : mieux sans fond, mais « mal aligné », éliminations trop petites, « trop de couleurs différentes » en haut à droite (score jaune, Essence bleue, Péril rouge, morts en blanc) ; « essaye de faire un truc différent ».
- **Personnage par défaut :** « peut-être que je ferai en sorte que ce soit le Traqueur… à voir, mais je pense que ça serait bien… si c'est le Vagabond, c'est bizarre que le Traqueur apparaisse en premier dans la liste ».

**Acquis :** voile du marais coupé ; ombres portées au pixel du monde par défaut. Le reste est découpé au [plan 41](41-retours-du-12-octobre.md).

### 129 bis. Réponses sur le plan 41

- Personnage par défaut : **le Traqueur**, sélectionné sur un profil neuf et premier de la liste.
- Particules de la ville : « tester 1 et 2 » — poussière qui flotte et scintille dans la lumière (avec des papiers rares dessinés), et papiers dessinés seuls ; Raphaël choisit en jeu.
- Colonnes de lumière : planche de trois propositions avant intégration.
- Ordre : HUD, rectangle de la carrière, particules, colonnes de lumière.

### 129 ter. Traqueur de départ, Vagabond gagné par la chasse

Le Traqueur et son arc étaient gardés par « Mener la chasse » (vaincre un Souverain) ; d'où un Traqueur verrouillé en tête de liste et un Vagabond sélectionné. Choix de Raphaël : **échange** — Traqueur et arc libres dès le départ, « Mener la chasse » débloque le Vagabond et sa lame.

## 130. Les projectiles d'une attaque partent à la chaîne — 12 octobre 2026

Raphaël : « quand on a plusieurs munitions d'une même arme […] les projectiles tirent tous en même temps. Au lieu de se tirer à la chaîne, ou les uns à la suite des autres. […] dans Megabonk, si on a deux projectiles d'une arme, ils vont se tirer l'un après l'autre et pas les deux en même temps, puis plus rien, puis les deux en même temps. »

Constat dans le code : la rafale de G6g (§59) ne retardait que les tirs qui retombaient sur une cible déjà visée. Dès qu'il y a plusieurs créatures à portée, cas presque constant, tous les tirs partaient dans la même image ; les projectiles en plus de l'éventail (sans Papier carbone) aussi ; un seul son par attaque.

- **Portée :** les tirs propres de l'arme et les projectiles en plus (éventail sans Papier carbone, ou une cible chacun au palier) partent l'un après l'autre. Les armes en salve (Lance-billes, Assiettes, Polaroïd) gardent leur éventail d'un coup, c'est leur personnalité ; leurs projectiles en plus s'enchaînent ensuite. Choisi après reformulation : la première question n'était pas claire.
- **Cadence :** écart fixe (0,08 s), resserré pour que la chaîne tienne dans 60 % du temps de recharge de l'arme.
- **Ciblage :** répartition gardée, une créature par tir quand c'est possible ; si la sienne meurt pendant l'attente, le tir prend la plus proche à son départ.
- **Son :** un par projectile, le premier normal, les suivants plus doux et variés en hauteur.

**Suite :** lot G6h au [plan 21](21-systeme-de-jeu.md), livré le même jour, à juger en jeu.

## 131. Questions de reprise — 12 octobre 2026

- **Critiques (plan 06 §10, quêtes de critique) :** « il faut pas qu'il y ait du critique de base par défaut pour tous les personnages. mais par contre, il faut qu'il y ait au moins un ou deux objets qui permettent d'avoir des chances de coups critiques, obtenables dès le début sans avoir de quête. il faut aussi peut-être qu'il y ait un personnage qui a un passif lié aux critiques et il faut aussi peut-être qu'il y ait des armes qui augmentent le critique […] il faut qu'il y ait plus d'armes qui intègrent des coups critiques dans leurs statistiques de level up. » **Acquis :** pas de critique de base ; un ou deux objets de critique dans la réserve de départ ; davantage d'armes avec le critique dans leurs gains de niveau. **Pistes à proposer :** un personnage au passif de critique, des armes qui augmentent le critique. Lot à découper plus tard : Raphaël demande le 12 octobre de **ne rien toucher pour l'instant** (objets, stats, critiques), seulement de le consigner. Le tirage du critique pour les armes en arc reste ouvert.
- **Campement (plan 36 §13) et Inventaire (plan 35 §16) :** pas encore joué, questions reportées.
- **Reste du tableau de bord :** rien de neuf.

## 132. Objets : Chance, coffres payants, soins et lisibilité — 12 octobre 2026

Deuxième retour de Raphaël sur le catalogue proposé (§128) :

- Pastille : compter et afficher les soins par minute ; remplacer le nom « duffle-coat », varier les noms au lieu d’accumuler « machin de quelque chose ».
- La Chance **doit influencer les coffres**. Aujourd’hui le choix d’objet est uniforme et ne la lit pas.
- Le plafond à 3 % des PV max/s est jugé trop bas : le relever ou le supprimer, avec une forte récupération qui reste le résultat d’un build et de bons objets, pas un acquis du début de run. Le code actuel limite le vol de vie, pas tous les soins.
- Exigence de livraison : chaque effet réellement appliqué en jeu, **toutes les stats visibles en pause**, effets montrés en action quand cela aide à les comprendre. Une fiche ou un raccordement théorique ne suffit pas.
- Explication demandée sur le guidage : correction de trajectoire d’un projectile autoguidé vers sa cible, pas chance de toucher.
- Anneau refusé : **les armes orbitales suivent la même quantité de projectiles** que les autres armes.
- Étiquette : préférer conditionner les flèches des coffres à cet objet à une simple extension de portée.
- Clé remplacée par un **Pied-de-biche donnant une chance d’ouverture gratuite** ; les **coffres deviennent payants en Essence**, accélérer leur animation n’a pas d’intérêt puisqu’elle se saute déjà.
- Plan cadastral refusé ; proposer une autre mécanique.

**Préparation, pas livraison :** [plan](35-raretes-des-objets.md) et [catalogue](35-catalogue-objets-v2.md) révisés. Propositions de l’agent : 9 PV/min par Pastille (même débit que 0,15 PV/s), plafond du vol de vie à 8 % des PV max/s, Dé limité à n PV/s sans second plafond proportionnel ; poids de rareté multipliés par `(1 + Chance)^rang` ; prix initiaux de coffres 20/45/90 Essence à mesurer ; Pied-de-biche 15 % par copie, maximum 60 %, jet unique mémorisé ; Étiquette une flèche par copie, maximum trois ; Portrait effacé légendaire, leurre après dash. Aucune de ces valeurs n’est présumée validée par Raphaël. Catalogue à 56 objets (34 nouveaux) après suppression de l’Anneau ; noms révisés, Verre fêlé accessible dès le départ conformément au §131. Pas de changement du code de gameplay pendant cette préparation.


## 133. Implémentation complète des objets, copies ordinaires illimitées — 12 octobre 2026

Raphaël valide le plan et demande « implémente toutes ces objets. fait tout, à part les sprites et les animations ». Il retire la limite générale à six exemplaires : **aucune limite de copies sur les objets ordinaires**, seules les limites des légendaires et objets uniques restent. Les anciennes exceptions ordinaires à deux, trois, quatre ou dix copies sont donc également levées. Les places du sac et les plafonds de sécurité des effets restent des mécanismes distincts.

Exécution par lots du [plan I10](35-raretes-des-objets.md) et du [catalogue V2](35-catalogue-objets-v2.md) : fondations et copies directes ; effets existants ; raretés, sources et coffres payants ; nouveaux objets et raccordements ; affichage intégral des stats et validations. Les sprites et animations nouveaux sont exclus ; les effets doivent fonctionner et être lisibles avec les moyens existants. Les valeurs de §132 sont validées comme réglages initiaux à mesurer, sans prétendre leur équilibre déjà démontré.


## 134. Améliorations d'arme : stat au plafond, Rallonge, Épique à deux stats — 12 octobre 2026

Raphaël signale une carte « Rallonge, Zone +0 % » au level-up. Cause : les armes circulaires (Rallonge, Cloche d'école) frappent toujours à 360° et ignorent l'arc, mais `arc_angle` restait dans leur `growth`, hérité du gabarit de mêlée.

- **Tirage :** une stat d'arme déjà à son plafond (arc à 360°, cône à 180°) n'est plus tirée (`WeaponInstance.IsStatCapped`) ; `arc_angle` retiré du `growth` des deux armes circulaires.
- **Rallonge :** « la portée de cette arme doit aussi augmenter et là par défaut la portée est environ 2 fois trop grande ». Rayon de base 95 → 48 px ; poids de la portée au tirage 1,5 → 2,5 (elle reprend celui de l'arc). La Cloche d'école (110 px) n'est pas touchée.
- **Épique :** passait une seule stat ×1,75 (total 1,75), moins que la Rare (2 × 1,25). Raphaël : « corrige ». L'Épique améliore **deux stats ×1,75**, comme le prévoyait le plan 17 : gains totaux 1 / 1,5 / 2,5 / 3,5 / 6.

### 129 quater. Colonnes de lumière

Planche de trois propositions (A faisceau effiloché, B poussière de mémoire, C rai oblique) : « là comme ça aucun me plaît. En fait le A était pas mal mais pas assez animé. » A repris en plus animé (vagues qui remontent, cœur qui respire, éclats au sommet, anneau au sol), vidéo : « parfait comme ça on laisse ainsi ». **Acquis** pour toutes les colonnes de lumière.

## 135. Campement : cohérence des icônes et matières des menus — 12 octobre 2026

Raphaël : « les icons pixel art c'est bien mais il n'y a pas la meme densité de pixel ni style qu'ailleurs (par exemple les objets ect) donc à revoir » ; « Grands panneaux et leurs bordures, fond : ca ne devrait pas etre des png mais plutot des textures. et attention à ce qu'elles soient de bonne qualité ».

**Direction demandée :** reprendre la densité et le style des objets ; remplacer les grandes illustrations étirées des surfaces intérieures par des matières de qualité. **Proposition de l'agent, à juger :** même pipeline de volumes et lumière que les objets pour les icônes, petite pierre raccordable, grain discret, bordures modulaires à échelle constante. Trois icônes et une planche de matières préparées au [lot M6a](36-progression-permanente.md#reprise-visuelle-m6--étude-du-12-octobre-2026). Aucun remplacement en jeu pour l'instant ; la préparation des planches n'est pas une approbation de cette direction précise.

### 135 bis. Direction M6 approuvée

Après les planches des trois icônes et des matières : « Bcp mieux. ca me va fait le reste ». **Acquis :** même rendu que les objets, pierre mate raccordable, grain discret, bordures à échelle constante ; décliner toutes les icônes du Campement et intégrer les matières aux menus. L'illustration du menu principal reste la référence, les fonds intérieurs et les grands panneaux deviennent procéduraux.

M6b livré et vérifié le même jour : quatorze icônes, matière raccordable créée et partagée par le jeu, neuf régions répétées ; captures aux deux résolutions et six suites vertes sur une copie isolée du lot. Build du dossier partagé également sans warning. Compte rendu au [plan 36](36-progression-permanente.md#reprise-visuelle-m6--étude-du-12-octobre-2026) ; les changements de faune et d'objets en cours ne font pas partie de cette validation isolée.

### 135 ter. Fond illustré conservé, matière dans les panneaux, bordures à enrichir

Raphaël : « je me suis mal exprimé le fond peut etre l'image que tu avais créé (la dernière c'était top) par contre il faut que les cards et menus aient des bordures plus travailées. et le fond que tu as créé irait bien pour ces menus je pense ».

Clarification de M6 : remettre l'image simplifiée B5c/B5d derrière les menus ; employer la matière procédurale dans les cartes et panneaux. Travailler davantage leurs bordures. Proposition d'implémentation au [lot M7](36-progression-permanente.md#reprise-visuelle-m7--clarification-du-12-octobre-2026) : relief de pierre, biseaux, joints et cassures irréguliers, coins taillés, sans changer les icônes ou la disposition. Le détail de cette nouvelle bordure reste à juger sur les captures.

Raphaël précise ensuite : « en fait c'est aussi que les bords rectangulaires brutaux que j'aime pas ». **Demande :** revoir la silhouette elle-même. Reprise M7 : coins largement cassés et différents selon leur position, contours avec décrochements, sans imposer une nouvelle disposition aux contenus.

M7 intégré et vérifié : fond illustré restauré, matériau dans les panneaux, coins taillés et contours ébréchés ; relief teinté par la sélection. Six suites vertes sur copie isolée, build du dossier partagé zéro warning, captures aux deux résolutions et dans la pause. **Le détail du rendu reste à juger par Raphaël**, les vérifications techniques ne valent pas approbation visuelle.

### 135 quater. Campement : icônes encadrées, hauteur et typographie

Raphaël : « ok c'est sympa mais ptet sur le campement met les icons de la liste à gauche dans des cadres […] le menu peux prendre plus de hauteur » ; supprimer le trait sous « Tout rendre » sur toute la largeur. Il apprécie le décor mais s'interroge sur son effet sur la lecture ; « je crois que c'est surtout la font utilisée qui dénote mais négativement ».

Lot [M8](36-progression-permanente.md#reprise-visuelle-m8--lisibilité-du-campement-12-octobre-2026) : cadres des icônes, hauteur disponible et suppression du trait. Proposition de l'agent pour la lecture : assombrir le décor sur le Campement et essayer Pixel Operator sur cette page, avec de plus grands textes et des marges adaptées. La police et la luminosité exactes ne sont pas présumées approuvées avant la capture du résultat ; aucune refonte typographique globale implicite.

M8 intégré et vérifié : cadres, 144 px supplémentaires en hauteur de corps, trait supprimé, décor à 52 % de luminosité, Pixel Operator limitée au Campement. Captures 720p/1080p et libellés anglais inspectés, quatre suites vertes sur copie isolée et build du dossier partagé zéro warning. **Police et luminosité à juger par Raphaël** sur ces captures.

### 135 quinquies. Alignements et place du retour

M8 apprécié (« top »), commit et push de tout demandés. Pendant cette publication : « sur le campement, et les autres menus […] certains éléments […] le bouton retour ne devrait pas être en bas à gauche […] des éléments qui sont mal alignés ». Lot [M9](36-progression-permanente.md#reprise-visuelle-m9--alignements-et-retour-12-octobre-2026) : en-tête commun, marges communes et Campement aligné sur sa liste. Préférence demandée pour Retour : haut à droite (proposition) ou haut à gauche près du titre ; sans réponse durant la préparation, haut à droite retenu par l'agent, sans le présenter comme une approbation de Raphaël.

M9 intégré et vérifié : captures des quatre pages en 720p et du Campement en 1080p inspectées, build zéro warning et cinq suites vertes. Retour placé dans l'en-tête, aligné au bord droit du contenu ; son action et les transactions sont contrôlées. Publication sur `main` dans le mandat précédent.

### 135 sexies. Généraliser la police du Campement

Raphaël : « la font que tu as mis sur campement elle me plait met la partout dans le jeu stp ». Pixel Operator approuvée comme police globale. Lot [M10](36-progression-permanente.md#reprise-visuelle-m10--pixel-operator-dans-tout-le-jeu-12-octobre-2026) : thème commun, menus, HUD et textes du monde, avec les variantes native 8 px et grasse de la même famille pour les petites tailles. Le choix de Saira du 24 septembre est remplacé par ce retour ; tailles et mises en page à vérifier sur captures.

M10 livré : police centralisée, essai isolé du Campement retiré, petits textes et dégâts migrés, colonnes de stats adaptées. Captures des menus et de vraies runs inspectées en 720p/1080p, français et anglais ; build zéro warning, dix suites puis trois suites ciblées vertes. [Preuves et limites](36-progression-permanente.md#reprise-visuelle-m10--pixel-operator-dans-tout-le-jeu-12-octobre-2026).

### 135 septies. Simplifier les gains et confirmer « Tout rendre »

Raphaël : retirer « Effet actuel → suivant », ne pas mettre le gain en bleu,
écrire « +1 truc » plutôt que `x → y`, remplacer les `x/y` de la liste par
autant de points colorés que de niveaux possibles, confirmer « Tout rendre »
et l'éloigner de Retour, ajouter une icône de Vestiges. Lot [M11](36-progression-permanente.md#reprise-visuelle-m11--gains-niveaux-et-remboursement-12-octobre-2026).
Choix de l'agent : action globale au bas de la liste, gain du prochain
achat (bonus total au maximum), confirmation avec Annuler focalisé et
montant versé réellement remboursable ; icône dorée partagée par le solde
et les prix.

M11 livré : gains sans comparaison ni cyan, points de niveau dans la
liste, action globale au pied gauche et confirmation avec montant payé
annoncé. Annuler, Échap/B et focus vérifiés sans mutation ; confirmation
unique, remboursement, points et sauvegarde contrôlés. Icône native des
Vestiges exportée par le pipeline des objets. Captures normal/dev,
français/anglais en 720p/1080p, build zéro warning et quatre suites vertes.


## 136. Préparer les quêtes après les objets — 12 octobre 2026

Raphaël demande de continuer l’implémentation validée, puis ajoute : « une fois fait tu préparera les quetes ». Après les raccordements des 34 nouveautés, préparation de [quinze quêtes d’objets](35-quetes-objets-v2.md), avec fiches JSON alors conservées hors données actives (intégrées et brouillon supprimé ensuite, §138). Tous les objectifs utilisent des faits déjà suivis, chaque dépendance est explicitée et aucune récompense n’est requise pour son propre objectif. Seuils proposés à mesurer ; aucune nouvelle quête activée ni modification de `data/quests/quests.json`.

Les 19 nouveautés disponibles au départ le restent ; les quinze Q sont implémentés et accessibles en dev, mais `initial_unlock: false` les garde fermés en profil normal jusqu’à leur quête. Le mode dev a priorité ; une quête accomplie ouvre explicitement son objet. Les 49 quêtes existantes et les 14 anciens objets qu’elles gardent sont conservés.

## 137. Exode dans les rues : densité égale, scènes rares — 12 octobre 2026

Raphaël, avant le [lot B3 du plan 39](39-atmosphere.md) : « sur la phase B j'ai très hâte. j'ai un peu peur que ça surcharge le monde car il y a déjà pas mal de props. il faudra y faire attention et faire des choix. »

**Acquis pour B3 :**
- **Densité égale.** Les scènes de l'exode remplacent une partie des voitures et gravats semés au hasard dans les rues. Le nombre de décors en ville ne doit pas monter : il est mesuré avant et après.
- **Rares et fortes.** Environ 5 à 8 scènes par ville, espacées.
- **Ville seulement.** Ni les chemins des champs ni les autres biomes, qui ont déjà leurs scènes-récits.
- **Première vague :**
  - embouteillage figé, qui laisse toujours un passage ;
  - barrage d'évacuation contournable ;
  - petits restes de fuite (valise, poussette, caddie, carton), posés uniquement dans les scènes.

  Le bus renversé n'est pas retenu.

Règle générale pour la suite de la phase B (B4 à B6) : vérifier ce que chaque ajout coûte en encombrement, et retirer quelque chose quand on ajoute.


## 138. Activer les quinze quêtes d’objets — 12 octobre 2026

Raphaël valide les quinze quêtes préparées et demande leur implémentation dans le jeu. Seule correction : le Pied-de-biche exige **50 coffres ouverts, toutes parties confondues**, au lieu des 12 proposés. Les quatorze autres objectifs restent validés sans modification.

Lot unique au [plan des quêtes d’objets](35-quetes-objets-v2.md#5-lot-dactivation-livré--12-octobre-2026) : fusion de quinze fiches dans le catalogue actif (49 → 64), conservation des quêtes et sauvegardes historiques, suivi et notifications existants, accès Collection/Quêtes et déblocage du butin. Le verrou initial des quinze objets reste en données ; leur quête accomplie a priorité. Aucun nouveau sprite ni animation. Validation par événements réels, seuils/contre-exemples, sauvegarde, normal/dev et captures réelles ; la roadmap ne sera cochée qu’après preuves.

Activation livrée le même jour : 64 fiches, dont 57 quêtes actives et les 7 Réminiscences historiquement dormantes. Les 49 fiches anciennes restent identiques. Huit suites vertes, captures Quêtes/Collection à 720p/1080p et déblocage du Pied-de-biche au cinquantième coffre vérifiés ; [preuves](35-quetes-objets-v2.md#6-preuves-dactivation). Les seuils validés restent sujets au retour de partie humaine.

## 139. Icônes des 34 nouveaux objets — 12 octobre 2026

Raphaël : « il y a des nouveaux objets, il faudrait créer leurs sprites ». Découpage au [catalogue V2 §7](35-catalogue-objets-v2.md#7-icônes-des-34-nouveaux-objets--préparation-du-12-octobre-2026) : S10a (18 stats), S10b (16 armes et jeu), S10c (export et branchement), S10d (visuels en jeu, à décider).

- **Conflits avec les Écussons** (Lacets noués, Gros bouton, Ressort, Ruban gradué, Ficelle cirée reprenaient le sujet d'un Écusson) : « en effet ça c'est pas ouf. il faudrait repenser ces objets (même fonctionnement mais identité différente) ». Proposition de l'agent, à juger sur la planche S10a : Dossard froissé, Bouillotte, Yo-yo, Longue-vue (l'antenne télescopique essayée d'abord ne se lisait pas), Bocal de conserve. Noms et IDs inchangés avant validation.
- **Rareté :** cadre seul, aucune marque de rang sur l'icône.
- **S10a :** planche d'abord, rien d'exporté ni branché avant validation.
- **Visuels en jeu (S10d) :** Raphaël a demandé ce que c'était ; explication donnée, décision en attente.

Après la planche S10a : « je valide à 100 % ces nouvelles identités et la planche aussi. Go, fais le reste ». **Acquis :** Dossard froissé, Bouillotte, Yo-yo, Longue-vue et Bocal de conserve remplacent les cinq objets, IDs compris ; planche S10a validée. S10b et S10c livrés le même jour ([§7.6](35-catalogue-objets-v2.md#76-s10b-et-s10c--livrés-12-octobre-2026)) ; la planche S10b n'a pas été validée séparément. S10d (visuels en jeu) reste en attente de décision.

## 140. Visuels des objets reportés, son des petits lieux — 12 octobre 2026

Sur S10d (silhouette du Portrait, fil de la Pelote, éclat du Tampon ANNULÉ…) : « non t'inquiète pas besoin des effets sonores des objets pour l'instant ». Lu comme le report de S10d ; ni visuels ni sons d'objets à faire pour l'instant.

« Il manque un effet sonore quand on valide un POI sur le jeu (la cabine téléphonique, le wagon de la mine, etc.) ». Les neuf petits lieux du plan 22 (puits, veine, épouvantail, boîte aux lettres, wagonnet, voiture, cabine, abribus, pique-nique) émettaient `SmallPlaceUsed` sans aucun son, contrairement aux POI explorés. Correctif : `AudioManager` joue à l'usage la confirmation des POI (`sfx_poi_activate`), une fois, jamais sur un lieu déjà utilisé. Vérifié par `tools/test_small_places.sh` (demande de son comptée) ; **à écouter en jeu**, un son propre par lieu reste possible plus tard.

## 141. Couleur de rareté plus visible — 12 octobre 2026

Raphaël demande de mettre davantage en avant la couleur de rareté des objets dans les menus et probablement en jeu. La comparaison fournie avec Megabonk montre une rareté lisible par le fond entier de la case ; il demande de s'en inspirer sans recopier. Lot I13 du [plan des raretés](35-raretes-des-objets.md) : fonds teintés, cadres colorés, focus qui conserve la rareté et rappel explicite sur la fiche, puis raccordement HUD/butin/sol.

## 142. Trier les objets — 12 octobre 2026

Raphaël : « il faudrait que les objets soient triés par raretés et qu'il y ait plusieurs options de tri disponibles ». Lot I14 du [plan des raretés](35-raretes-des-objets.md) : rareté croissante par défaut, décroissante, nom et ordre initial ; commandes dans la Collection et le sac en pause, préférence de session commune au HUD et au bilan. Choix d'implémentation : regrouper les raretés même sur profil neuf, disponibilité seulement à rang égal ; conserver l'objet examiné quand le tri change.

## 143. Radios et téléphones abandonnés : son seul — 12 octobre 2026

Raphaël, avant le [lot C1 du plan 39](39-atmosphere.md) : « ok continue avec la suite des modifications stp », puis ses réponses aux quatre questions du lot (toutes sur la recommandation).

**Acquis pour C1 :**
- **Voix synthétisée et traitée.** espeak-ng, seul moteur disponible sur la machine, passé dans un poste de radio (bande étroite, saturation, flottement) : un message d'alerte automatique. Un fichier par mot ou groupe de mots, remplaçable plus tard par un enregistrement de Raphaël sous les mêmes noms.
- **Son seul.** Rien à l'écran, ni bulle ni sous-titre : on entend la radio en s'approchant.
- **Radios dans les embouteillages.** Sur le capot d'une voiture d'environ un embouteillage sur trois (5 à 6 par carte), sans nouveau décor ailleurs.
- **Une cabine sur deux sonne.** Parmi les cabines activables (petits lieux), elle se tait quand le joueur arrive et garde son usage (révéler les lieux proches).

## 144. Capacité du sac par types distincts — 12 octobre 2026

Raphaël : le sac compte actuellement les doublons dans ses dix places, alors qu'il attend dix types d'objets distincts avec les doublons empilés. Précision : « le 10 c'est pas une valeur absolue c'est la valeur par défaut sans les améliorations ». La capacité reste calculée à partir d'`inventory.json` et du Campement ; seuls les types distincts occupent une place, les doublons rejoignent leur pile à sac plein. Un nouveau type demande de remplacer une pile entière si la capacité est atteinte. Il demande aussi de voir les objets dans les coffres et dans le HUD à gauche. Lot I15 du [plan des raretés](35-raretes-des-objets.md).

## 145. Fausses lumières : décors existants, fenêtres, feux de camp — 12 octobre 2026

Raphaël, après C1 : « je vais écouter je te ferai les retours ultérieurement en attendant continue avec d'autres choses du plan ». Réponses aux questions du [lot B4 du plan 39](39-atmosphere.md) :
- **Les trois sortes de lumières** : lueurs posées sur les décors qui en portent déjà (feux tricolores, lampadaires, lampes de carrière, lanternes du marais), fenêtres éclairées de loin en loin, feux de camp éteints qui fument.
- **Une lampe sur quatre environ** reste allumée ; les autres sont mortes.
- **Discrètes en plein jour, plus fortes dans le sombre** : orage, brouillard, nuit de l'Indicible.
- **Avec l'oubli** : une lampe d'une zone qui s'oublie vacille de plus en plus, puis meurt pour de bon.

Découpage proposé par l'agent : B4a (lueurs sur les décors existants), B4b (fenêtres), B4c (feux de camp), un lot à la fois.

## 146. Alkhemikal pour les titres, Pixel Operator pour le reste — 12 octobre 2026

Raphaël demande une police plus adaptée sur dafont, catégories Bitmap ou Gothique. Recherche de l'agent : polices à accents et licence compatible avec un jeu vendu, puis comparaison en jeu de Pixel Operator, Alkhemikal (jeti, CC BY 4.0) et Ithaca (GGBotNet, OFL) sur sept écrans en 1080p. Raphaël : « un mélange de Alkhemikal et Pixel Operator je dirai ».

**Acquis :** Alkhemikal pour les titres, en-têtes de section, entrées du menu d'accueil, bouton Partir, nom de l'élément détaillé, onglets de Collection, nom du biome et chuchotements ; Pixel Operator pour le texte courant, les boutons d'action et tous les chiffres. Le « pas de gothique » de la Bible §9.1 est levé. Crédit obligatoire : « Police Alkhemikal par jeti, CC BY 4.0 », à faire figurer dans les crédits du jeu dès qu'ils existent. Lot [M12](36-progression-permanente.md#reprise-visuelle-m12--alkhemikal-pour-les-titres-12-octobre-2026).

## 147. Détail des stats en infobulle — 12 octobre 2026

Raphaël, pendant M12 : « il n'y a pas besoin d'une ligne de texte sous le détail de chaque stat. à la limite on peut l'afficher au hover mais en mode pop up. » La ligne grise sous chaque stat (base, bonus du build) disparaît de la pause et de la colonne de stats du level-up, qui partagent la même fiche ; elle s'ouvre en infobulle au survol de la ligne, une information par ligne. Les infobulles du jeu reçoivent un style commun (fond sombre opaque, liseré doré, 24 px). Limite : l'infobulle ne s'ouvre qu'à la souris, pas à la manette. Lot [M13](36-progression-permanente.md#reprise-visuelle-m13--détail-des-stats-en-infobulle-12-octobre-2026).


## 148. Coffres sans description et cartes d'amélioration mieux agencées — 12 octobre 2026

Raphaël : « dans les coffres il ne faut pas afficher la description de l'arme » ; les récompenses actuelles étant des objets, leur effet disparaît du résultat du coffre. Il demande aussi des cartes d'amélioration mieux agencées, inspirées de la clarté de Megabonk sans recopier son interface : icône, nom, rareté, niveau et gains distincts, matières et typographie de Vestiges conservées. Lot [I16](35-raretes-des-objets.md#i16--butin-compact-et-hiérarchie-des-cartes-damélioration-12-octobre-2026).

Précision pendant I16 : « il ne faut surtout pas les probas du coffre affichés » ; retirer leur affichage du résultat et du choix de coffre. « il faut le niveau de l'amélioration aussi pour les carts de level up » : afficher le niveau actuel et le niveau obtenu à droite du nom, et Nouveau pour une acquisition.


## 149. Description des objets rétablie dans les coffres — 12 octobre 2026

Raphaël revient sur la suppression d'I16 : « pour les coffres je pense qu'il faut quand meme la description de l'objet sous le titre ». Remettre l'effet de l'objet directement sous son nom, avec un texte courant clair et replié, conserver la rareté séparée et les probabilités masquées. Reprise [I17](35-raretes-des-objets.md#i17--description-de-lobjet-sous-le-titre-du-butin-12-octobre-2026).

## 150. Retours du 12 octobre au soir : menus, bilan, monde — 12 et 13 octobre 2026

Retours dictés par Raphaël après une partie, puis tranchés par questions. Lots à découper dans un plan de retours avant de coder ; priorité proposée : corrections rapides, puis bilan et Recommencer, puis densité du monde.

- **Pause** : cases d'armes, d'Écussons et du sac mal centrées (niveau et icône qui débordent). **Pas de tri dans la pause** : le sac s'y montre en rareté croissante fixe ; la Collection garde son tri (revient sur §142 pour la pause seulement). **Bouton Recommencer** : même personnage, nouvelle graine, avec confirmation ; la run compte comme abandonnée.
- **Pluie** : il ne doit pas pleuvoir avant l'Indicible. **Orage sec** : éclairs, vent et nuages, sans pluie ; la pluie de cendres reste.
- **Bilan** : la courbe « Distance au départ au fil de la run » ne se comprend pas ; à la place, **score détaillé et build (armes, Écussons, objets) visibles sans clic**. « CE QUI DEMEURE » : plus de titre, une **rangée compacte de gains avec icônes**.
- **Sous-titre du menu principal** : jugé trop explicite, propositions faites, puis « on va laisser telle quelle finalement ».
- **Choix du personnage** : retirer le cadre jaune autour du portrait ; ne plus afficher PV et vitesse dans les détails ; le survol des boutons « Détails » du personnage et de l'arme (rectangle semi-transparent) est hors direction, à refaire.
- **Voix humaines en ville** (plan 39 C1) : « c'est pas bien », **retirer les voix** ; les cabines qui sonnent restent.
- **Repère au sol des petits lieux** (coins de losange dorés, plan 41 R4) : incompris, **retiré** ; l'invite « [E] » suffit.
- **Faune** : un peu trop, **−30 %**, pigeons qui s'envolent gardés.
- **Barrières** : beaucoup depuis peu, même en ville ; mesurer la source, puis **la diviser par deux**.
- **Notification de quête accomplie** : montrer l'**icône de la récompense**.
- **HUD des objets** (ajouté le 13 octobre pendant L1) : « ça ne me plaît pas l'affichage des objets. N'affiche pas 4/10 types. Aussi pas besoin de l'icône de rareté, et pas besoin de mettre dans des cadres carrés », puis « afficher les objets par ordre de récupération ».
- **Après L1** : « c'est mieux les cadres des personnages mais ils sont pas bien affichés… aussi on ne distingue pas assez bien la différence entre un personnage jouable et pas encore débloqué » ; « sur le menu pause il y a un trait jaune à côté de l'option choisie, c'est moche, faut faire mieux » ; contour des icônes du sac : « oui, on peut même faire un contour coloré en fonction de la rareté ». « Sur tout le reste de ce que tu as fait ça me va. »
- **Cadre du portrait** : « soit tu n'en mets pas, c'est peut-être le mieux » : retiré. **Pluie de cendres** : la recommandation (son sec à la place du bruit de pluie) est acceptée.
- **Pour plus tard** : rues de la ville un peu étroites ; routes toutes perpendiculaires alors que les bâtiments sont décalés. Pas assez d'Écussons comparé au reste (retour global, voir plan 42).

## 151. La signature de Vestiges — 13 octobre 2026

Raphaël veut un jeu aussi complet et poussé que Vampire Survivors, Megabonk et Risk of Rain 2, **avec des mécaniques uniques**, une boucle très satisfaisante et addictive. Megabonk l'a le plus happé : « un mélange du côté gambling, des scores explosifs, des quêtes et déblocages et qu'on s'ennuie jamais en run ». Méta-réflexion avec l'agent, consignée au [plan 42](42-signature.md).

**Acquis :**
- **Atmosphère en pause** (plans 39 et 40) tant que la boucle n'a pas sa signature.
- Gestes de base gardés : « le tir automatique me plaît ».
- **Effacement** : règles actuelles bonnes mais peu visibles, surtout en début de partie. Il doit **prendre quelque chose** : piste retenue, dans le Néant, **des stats tirées au hasard diminuées temporairement**, plus nombreuses au fil de la run (« plus marrant », aléatoire). Écartés : l'Essence non dépensée (des objets comptent l'Essence ramassée), les objets du sac ; préféré aux niveaux d'arme oubliés. **Instabilité croissante** au fil de la partie.
- **Butin à sauver** de l'Effacement : « excellent » ; coffres et bonus qui s'effacent forcent à explorer vite et rendent le jeu « plus vif ».
- **Contrer** : ni halo de couleur au sol (« va rendre mal ») ni geste supplémentaire ; l'ancrage passe par l'ouverture des coffres, les points d'intérêt et la marche, à rendre lisible.
- **Emballement de puissance** : « 1000 % d'accord », c'est ce qui rend Megabonk addictif ; il ne doit pas arriver à chaque run (connaître le jeu, build en synergie). Se joue dans l'équilibrage, surtout la montée en puissance des ennemis face à celle du joueur.
- **Systèmes** : pas plus nombreux que dans Megabonk ; plutôt mal expliqués, organisés et équilibrés.
- **Mouvement** : à renforcer, à la manière d'un jeu 2D isométrique.
- Retenus de l'analyse : « la pression » et « la gourmandise ». Non retenus : « l'élan », la formule « la distance comme moteur », l'idée A du plan 11 (trace offensive du mouvement).
- **Méthode** : mesurer d'abord, **par scripts de simulation** plutôt qu'en lançant le jeu, avec les différents personnages, des configurations de build, plusieurs états de progression (peu d'armes et d'objets en début de jeu) et des profils de joueur (débutant, fort) ; écrire la promesse en une phrase ; **tout concevoir avant de coder**.

**Doute ouvert** : les améliorations permanentes de puissance brute du Campement (vitesse, dégâts…) : « quand on a joué 20 heures au jeu, le début de partie n'est plus le même ». Taille du sac et gain de Vestiges lui conviennent. À trancher (plan 42 §4).

## 152. Campement sans puissance brute, ordre de travail, builds de référence — 13 octobre 2026

- **Campement** : « ça me va de retirer les bonus de puissance bruts permanents » (PV max, dégâts, cadence, vitesse, régénération, armure ; XP et Chance à trancher avec le gambling, plan 42 §4). **Aucune logique de remboursement** des Vestiges dépensés : « personne n'a de copie du jeu ». Les améliorations d'options et d'ampleur restent. Lot à découper au plan 36.
- **Ordre** : lot 1 des retours du 12 octobre au soir (§150), puis l'outillage de mesure du plan 42 (M0a–M0c).
- **Builds de référence** : les quatre proposés (une arme poussée, synergie d'objets, zone, critique), plus **un build tank** ; « de toute façon on en découvrira au fur et à mesure ».

## 153. Bot de mesure qui esquive, sans séance de jeu — 13 octobre 2026

- **Constat de Raphaël** : « si le bot est invincible ça gâche la mesure, et s'il meurt parce qu'il est bête ça ne va pas aussi ».
- **Retenu** :
  - le bot esquive ce qu'un joueur voit : tirs ennemis en vol, annonces de coup au sol ;
  - chaque profil a un temps de réaction, une part de menaces remarquées et un usage du dash ;
  - les deux bornes restent, invincible (pression brute) et mortel (survie, la run continue après une mort comptée). Plan 42 M0d.
- **Calage** : pas d'heure de jeu dédiée (« c'était pas automatisable justement ? »).
  - Les profils partent d'ordres de grandeur humains de réaction et d'attention.
  - Un enregistreur en mode dev suit les parties de test habituelles de Raphaël. On compare au profil fort quand quelques runs se sont accumulées, et on ne recale que si l'écart est grand. Plan 42 M0e.
  - La matrice n'attend pas ce contrôle.

## 154. Retours de jeu du 13 octobre après-midi — 13 octobre 2026

Cinq runs jouées par Raphaël (Traqueur ×2, Facteur ×2, Vagabond), toutes tracées par l'enregistreur (plan 42 M0e).

- **Build cassé** : une run « totalement broken », intuable en moins de 10 min, à partir d'une seule arme (la Pelle à neige, corps à corps). Il s'est laissé tuer pour finir. Les autres morts viennent en partie de la difficulté et en partie d'un jeu sans forcer : les traces ne mesurent donc pas un joueur qui « try hard ».
- **Départ trop rapide** : la montée en niveau va trop vite (niveau 10 vers 2 min ; traces : niveau 50 à 10 min, 66 à 14 min). Les cinq premières minutes sont trop nerveuses : des ennemis dangereux arrivent vite, et l'XP compense. Dans les autres bullet heaven, les premiers niveaux sont plus lents, ce qui rend la montée graduelle et évite de se sentir trop fort dès le début.
  - **Tranché** : équilibrer le départ **avant** la grande matrice du plan 42, pour qu'elle mesure le jeu corrigé. Cible : **niveau 10 vers 4–5 min**, avec une pression ennemie qui baisse d'autant au début.
- **Corps à corps** : les personnages de corps à corps semblent moins forts que ceux à distance. À mesurer (matrice, bloc personnages) puis équilibrer.
- **Difficulté** : il manque des moyens de l'augmenter (monter son Péril, etc.). Va dans la conception du plan 42 (pression, gambling).
- **Méthode des essais** : à chaque run d'essai, varier les personnages et les builds. C'est « hyper important » pour détecter les failles et les builds trop forts, et donc pour équilibrer, même si ce n'était pas le but premier de l'outillage.
- **Petits points** :
  - un badge « Nouveau » dans la Collection sur un objet qu'on vient de débloquer ;
  - un tic-tac ou un compte à rebours quand on ramasse les trois éclats d'un Mémorial.
- **Machine** : la grande mesure peut tourner cette nuit (4 cœurs, 7 à 8 h).
- **Choix du personnage** (ajout du même jour) : la sélection ne doit pas changer au simple survol de la carte ; il faut cliquer. Fait : survol et focus ne font que surligner, seul l'appui choisit.
- **Livré le 13 octobre** :
  - badge « Nouveau » sur les cases de la Collection débloquées par une quête, tant qu'on ne les a pas regardées, et point d'or sur l'onglet ;
  - compte à rebours des éclats au-dessus du joueur, à la place de celui du Mémorial, qui sortait de l'écran pendant le ramassage ; tic-tac à chaque seconde, pressé (deux par seconde) et en rouge dans les 5 dernières (`shard_urgent_time`) ;
  - la trace de survie note la vraie graine de la run.

## 155. Quêtes trop faciles, classement aux éliminations — 13 octobre 2026, fin d'après-midi

**Quêtes.** Constat : en une seule run, sans y prêter attention, Raphaël en a accompli 5 à 10, ce qui leur ôte leur intérêt.
- Une partie des déblocages (la moitié des pièces ou un peu moins, toutes familles confondues : personnages, armes, objets…) vient de quêtes qui se font d'elles-mêmes. Elles demandent 5 à 20 parties moyennes ou longues, soit environ 1 h à 4–5 h de jeu sans rien faire de spécial.
- Les autres demandent de **le vouloir** : on regarde comment faire la quête, et elle se fait difficilement par hasard, sans être impossible. Formes possibles :
  - jouer une arme précise ;
  - faire quelque chose de spécial sur la carte ;
  - survivre d'une certaine manière, par exemple sans prendre de dégâts pendant un temps ;
  - relever des défis : aller loin sans tel objet, ou avec une seule arme.
- Les quêtes de compteur qui restent (tant de dégâts, tant de coffres) voient leurs paliers « pas mal » relevés.
- **Ordre** : après la mesure de nuit, pour caler les paliers sur les chiffres par run. Le catalogue revu est proposé à Raphaël avant de coder.

**Classement.**
- Il revient avec un affichage refait : records personnels (meilleur au global et par personnage) et surtout un **classement global de tous les joueurs, remis à zéro chaque semaine**. Le classement de tous les temps est gardé.
- **Une seule mesure, les éliminations, partout** : classements, records et bilan. Le score composé disparaît de l'affichage.
- Graines aléatoires (aucune graine imposée pour concourir), tous personnages confondus.
- **Vestiges gagnés = éliminations ÷ un taux**, calé pour qu'une run moyenne rapporte autant qu'aujourd'hui. Le Péril paie toujours, par plus d'ennemis donc plus d'éliminations.
- **Steam** : Raphaël lance la démarche maintenant (Steam Direct, App ID) et demande d'être guidé.
- **Ordre** : le classement se code tout de suite, à part de la mesure de nuit ; les quêtes viennent après la mesure.

## 156. Conformité avant Steam : nom, IA, crédits — 13 octobre 2026, soir

Audit des droits et des contenus (plan 31 §9 et §9 bis).
- **Nom** : « Vestiges » est une marque INPI pour les jeux vidéo (n° 5181093, septembre 2025). Raphaël change de nom, à trouver « au moins aussi bien ». Le dépôt INPI se fera avant que le nom devienne public ; les dépôts plus chers (EUIPO, autres pays) selon les wishlists.
- **Musiques** : générées sous abonnement Suno payant, mais **remplacées** quoi qu'il arrive.
- **Sons** : ceux d'ElevenLabs sont remplacés, avec `shield_block`, `shield_break` et `level_up.wav`, dont l'origine est oubliée.
- **IA générative** : le public y est très hostile. Les assets produits par IA générative sont remplacés (pixel art procédural, dessinateur et compositeur ami de Raphaël, UX/UI), et leurs traces retirées de la documentation. Le code écrit avec des agents gêne moins. Ce qui reste d'IA dans le jeu au moment de la page Steam se déclare.
- **Commits de mars sous `raphael@tkorp.com`** : adresse liée au même compte GitHub personnel ; le projet appartient à 100 % à Raphaël.
- **Fait le jour même** : page Crédits dans le Hub, arme Polaroïd renommée Instantané, `LICENSE` complété, logo Godot (`icon.svg`) retiré et remplacé par le halo de lumière dans les trois décors de lore qui l'utilisaient.
- **Étude de marché** ([rapport](../reports/Étude%20de%20marché%20Vestiges.md)) : Raphaël valide le plan de validation en quatre étapes (clips sous nom de code, page Coming Soon, démo, Next Fest), sans s'interdire d'être ambitieux. Les tags post-apocalyptique et pixel isométrique peu porteurs ne condamnent pas le jeu, mais il doit être très propre et peaufiné. La mélancolie n'est pas le ressort principal : les parties doivent être nerveuses, stimulantes, avec du *juice*.
- **Direction du nouveau nom** : un mot inventé, ou l'oubli.
- **Piste en tête, non tranchée : *Unnamable*** (Raphaël s'en laisse la nuit, pas convaincu) (l'Innommable, l'Indicible du lore ; on finit le jeu en nommant). Vérifié le 13 octobre : libre sur Steam, aucun jeu de ce nom ; à l'INPI (bases française, européenne et internationale), seules des marques « UN-NAMABLE » et « UNNAME » de mode et de cosmétique (classes 3, 14, 18, 25, 35, 39, 42), aucune en 9 ni 41 ; `unnamablegame.com` et `playunnamable.com` libres. Reste à vérifier les États-Unis (USPTO). « Vestiges » reste le nom de la monnaie. Le titre en jeu ne change qu'au moment où le nom devient public.

## 157. Oublier une arme, un Écusson ou un objet à la Faille — 13 octobre 2026

Raphaël : « il faudrait avoir plus de mécaniques liées à l'oubli dans le jeu […] un point d'intérêt sur la map qui permette en échange d'essence […] d'oublier un objet comme ça on peut si on s'est trompé ou qu'on ne le veut plus on peut changer et prendre autre chose », puis, sur la recommandation de l'agent : « ouais ok ca me va ta recommandation. par contre je supprimerai les mécanismes suivants : [l'échange d'arme au sol et l'oubli du sac plein]. tout passera par ca. sinon le faire dans la faille ca me va. »

**Acquis :**
- oublier une arme, un Écusson ou une pile d'objets se fait **à la Faille, contre de l'Essence** ;
- l'oubli **laisse une trace** : la prochaine arme ou le prochain Écusson repart d'une part des niveaux oubliés ;
- l'échange d'arme au sol et l'écran d'échange du sac plein **disparaissent** : tout passe par la Faille.

**Choisi par l'agent, à juger en jeu :** 100 Essence puis +50 % par oubli, moitié des niveaux gardée, la Faille reste ouverte après un oubli. Détail au [plan 44](44-oublier-a-la-faille.md).

**Hors sujet, même jour :** les mini-boss à barre de vie en haut de l'écran ne doivent **jamais disparaître** s'ils ne sont pas tués.

## 158. L'oubli quitte la Faille pour un Puits ; les puits à boire disparaissent — 13 octobre 2026

Raphaël, découvrant que la Faille est le lieu des améliorations contre Péril : « je trouve ca confusant d'avoir un POI qui fait 2 choses apres réflexion ca ne me plait pas il va falloir trouver autre chose. » Il choisit un **nouveau lieu dédié en forme de puits**. Prévenu que des puits à boire existent déjà (petits lieux, soin de 15 %) : « retirer le POI puits existant (ou remplacer son effet par le nouveau). »

**Acquis :**
- la Faille redevient ce qu'elle était : améliorations contre Oubli et Péril, rien d'autre ;
- l'oubli (règles du §157 inchangées) se fait à un **Puits**, lieu à part entière présent dans tous les biomes ;
- le puits à boire n'est plus un petit lieu : le décor des champs reste, sans interaction.

**Choisi par l'agent :** 6 Puits par carte ; le Repère « Puits » (+10 PV max) passe au premier oubli à un Puits, ce qui garde 13 types de Repères.

## 159. Projectiles du joueur : plus petits, bordure allégée, même finition — 13 octobre 2026

Raphaël : « je trouve que c'est trop (je parle des projectiles des armes à distance comme l'arc et les billes). en gros on ne dirait plus des projectiles mais des armes en soit […] ils sont trop gros, ont une trop grosse bordure... d'autres sont trop peu visibles comme l'instantané. il faudrait dans l'ensemble que tous les sprites des projectiles soient au meme "niveau de finition". »

Réponses sur la [planche avant/après](planches/25-s1c-projectiles-joueur.png) : « arc ok, arbalete ok, boussole ok, éclat de phare ok, lance bille ok, assiette ca ne ressemble pas à des assietes. instantané je garderai le meme, mais je le réduirai. [Bâton d'essence], lanterne, craies, le reste, ok. »

**Acquis :**
- chaque projectile est dessiné à sa taille finale, sans agrandissement en jeu (flèche ~16 px pour un héros de 28 px, bille ~8 px) ;
- bordure : contour du sprite et liseré crème léger (0,25), sans halo ;
- traînée de vitesse dessinée dans le sprite derrière les tirs physiques ;
- Assiettes : une vraie assiette au lieu de la hache ;
- Instantané : même étoile, plus petite, cœur chaud et branches dorées pour ne plus se perdre dans les éclairs d'impact blancs.

**Livré :** [plan 25 S1c](25-sprites-et-design.md#s1c--projectiles-au-même-niveau-de-finition-13-octobre-decisions-159). À juger en jeu.

## 161. Préparer un nouveau personnage et ses sprites — 13 octobre 2026

Raphaël : « est ce que tu pourrais préparer l'implémentation d'un nouveau personnage en jeu ? il y a tous les sprites à créer ».

**Acquis :** préparer l'implémentation et couvrir l'ensemble des besoins visuels. L'identité n'est pas précisée dans cette demande.

**Préparation de l'agent :** [plan 45](45-nouveau-personnage.md), rattaché au plan 06. Scaphandrière recommandée, car dernière des trois intégrations déjà demandées au §90 ; son choix pour cette demande reste à préciser. Audit : 216 images de corps déjà présentes, portrait et harpon absents, mobilité et effets à compléter. Le passif historique ne correspond plus aux malus actuels de l'Effacement ; une adaptation est proposée, sans la présenter comme validée. Aucun gameplay ni sprite modifié, aucune case de roadmap cochée.

**Suite du 13 octobre — implémentation autorisée :** après la recommandation de Scaphandrière et l'adaptation de sa réserve d'air, Raphaël répond « okok go alors ». Le plan 45 passe en exécution N0–N5. Le choix et le lancement sont acquis ; l'équilibrage et le rendu final restent à éprouver.

**Réglages d'essai choisis par l'agent :** air 6 s neutralisant les malus de vitesse/dégâts Effiloché/Effacé, recharge 10 s Ancré/Fragile, aucun effet protecteur/recharge dans le Néant ; 115 PV, vitesse 190, dégâts 10, cadence 0,9, portée 340, régénération 1,035, bouclier 0, interaction 65. Harpon : léger ramené, lourd attirant le joueur, boss ancrés ; saut flottant de 0,5 s avec ombre au sol, contrôle limité et collisions actives. Ces chiffres ne sont pas des demandes explicites de Raphaël.

**Correction de Raphaël pendant N1 :** « je t'ai pas demandé de recréer le sprite détaillé du personnage son........ ». Le portrait détaillé IA créé par l'agent dépasse l'intention ; il est retiré du jeu ainsi que son prompt de production du dépôt. **Respecter le design détaillé existant ; ne pas inventer un nouveau portrait.** Utiliser le fallback du sprite en menu tant que la source correcte n'est pas localisée. Les corrections de poses et de cadrage requises pour le gameplay continuent, sans refonte de la silhouette ni des matériaux.

## 160. Souverains calés sur le niveau du joueur, petits retours d'interface — 13 octobre 2026, soir

**Souverains.** Raphaël : « les souverains doivent avoir des stats dépendantes du niveau du joueur. là notamment premier souverain qui pop (vers 4-5 min) a bcp trop d'HP dans la plupart de mes runs qui scalent pas trop. »
- Constat : leurs PV ne suivent que le temps (cible 1 200 × montée des créatures), soit environ 2 400 PV à 4:00 et 7 000 à 9:30. Le départ ralenti (plan 42 D1) a amené le joueur au premier Souverain vers le niveau 9–10 au lieu de 13–15, sans que le Souverain baisse.
- **Tranché** (lot S1, plan 42) : PV et, plus modérément, dégâts selon le **niveau du joueur** à l'apparition, courbe en données, calée pour une mort en 20 à 40 s face à un build moyen de ce niveau (cible du §84). La mesure ajoute les dégâts par seconde du joueur par minute et le temps pour tuer chaque Souverain ; vérification sur la matrice (5 personnages × 3 profils), Facteur compris.

**Interface** (livré avec le classement, plan 43) :
- carte de niveau d'une arme commune : voile gris transparent sur la pierre, plus de plaque claire ;
- compteur d'éliminations du HUD : le crâne seul, sans le mot.

## 162. Difficulté choisie : stèles et défi volontaire — 13 octobre 2026

Raphaël, sur le [plan 46](46-difficulte-choisie.md) : « steles ca me va, défi volontaire aussi. il faut un objet ou un écusson qui permette d'augmenter le péril si cest pas déjà le cas. Le reste ca me pariat pas mal ».

**Acquis de conception :**
- stèles à engagement choisi et défi volontaire retenus comme moyens d'augmenter la difficulté ;
- un objet ou un Écusson doit aussi permettre de monter le Péril ; **le Sifflet d'arbitre existant couvre déjà ce besoin** : +1 par exemplaire pris, copies ordinaires illimitées, Péril conservé même après oubli au Puits ; pas de doublon à créer pour satisfaire cette demande ;
- accord de principe sur les autres pistes proposées : paliers de Péril avant la run et pression d'Effacement réglable, à détailler avec les mesures et le plan 42.

**Limites de la validation :** Raphaël ne précise pas séparément les montants +1/+3/+5, les paliers 0/3/6/10, ni les chiffres du défi (60 s, 60 créatures, 150 m) ; ils restent les réglages d'essai de l'agent. L'ordre des lots et une récompense de quête précise ne sont pas tranchés par ce retour. Le principe de gains par les créatures (§78) et le classement en éliminations brutes (§155) restent en vigueur.

**État :** plan et suivi mis à jour ; aucune implémentation de gameplay du plan 46, aucune case de roadmap cochée. Référence D0 préparée à trois graines minimum par configuration, non exécutée.

## 163. Une seule police, Alkhemikal ; niveau dans un sceau — 13 octobre 2026, soir

- **Alkhemikal partout dans le jeu** (demande de Raphaël) : textes, chiffres, titres, HUD, chiffres de dégâts, plaques d'élites, thème global. Pixel Operator n'est plus chargé. `UITheme` garde ses accès (`BodyFont`, `StrongFont`…) qui renvoient tous Alkhemikal ; la graisse ne change plus la police (Alkhemikal n'a pas de gras).
- Métriques presque identiques à Pixel Operator à taille égale (226 px contre 228 pour la même phrase en 16) : les mises en page tiennent. Aucun texte sous 16 px (grille d'Alkhemikal) : les textes en 8 (Pixel Operator 8) passent en 16, même largeur, plus hauts.
- **Vitals** : niveau dans un petit sceau (coins coupés, liseré de métal de la jauge, bandeau « NIV. », rivets d'or), chiffre en blanc papier, en 24 ; PV et jauge centrés sur la hauteur du sceau.

## 164. Zones au sol au niveau du reste — 13 octobre 2026, soir

Raphaël : « les armes qui mettent des zones au sol me posent probleme (que ce soit du joueur ou des ennemis). souvent c'est un cercle parfait avec une texture légère. c'est clairement pas au niveau du reste ».

Réponses sur la [planche](planches/25-s10-zones-au-sol.png) : annonces des créatures « bord irrégulier aussi » (plutôt que bord exact mieux fini ou annonce à thème) ; matières « oui, comme la planche » ; Chronomètre en cadran couché ; tout ce qui est au sol dans ce lot.

**Acquis :**
- chaque zone a une matière : annonce, liquide, feu, cadran, éclat ;
- toutes ont un bord irrégulier propre à chaque zone, annonces comprises ;
- les zones s'animent pendant toute leur durée ;
- le Chronomètre est un cadran couché au sol, avec une touche au sol et la même surface (rayon 80 → 113) ;
- l'aura d'affixe et le trou des mains de l'Indicible ont le même rendu ;
- le dessin d'une flaque s'écarte d'environ 10 % de son cercle de touche, accepté.

**Choisi par l'agent :** famille `Gum` rose pour le Chewing-gum, translucide ; Écho des Gants gardé en cercle à l'écran, comme sa touche.

**Livré :** [plan 25 S10](25-sprites-et-design.md). À juger en jeu.

**Retour du soir, après livraison :** « je me suis trompé ptet que les ennemis peuvent avoir des formes plus régulières. le pb n'était pas tant la forme en vrai le cercle ca allait globalement mais plutot le rendu et le manque d'animation/ de détail ou de coté rugueux ect. » À reprendre le 14 octobre : les annonces des créatures peuvent revenir à une forme régulière (cercle ou ellipse) en gardant le nouveau rendu (sol assombri, contour, front, animation, grain rugueux). Il faut décider s'il en va de même pour les matières (flaques, feu).

## 165. Un Souverain jamais tué ne bloque plus la suite ; la fin de run sans danger est prioritaire — 14 octobre 2026

**Souverain.** Raphaël : « il ne doit pas bloquer la suite. ce n'est pas grave s'il reste sur la map ca fait une difficulté en plus (le truc c'est que sinon on peut juste l'ignorer). »
- La chasse dure 90 s (`duration_sec`). À son terme, l'événement se clôt (« Le … rôde toujours ») et laisse place aux suivants ; le Souverain reste sur la carte et continue de poursuivre le joueur (§157 tient : il ne disparaît pas).
- Tué plus tard, il rapporte encore le coffre et l'Essence de sa variante, pas la récompense de la chasse.
- Vérifié : `--capture-sovereign`, Souverain présent après 100 s, chasse close, événement suivant lancé.

**Fin de run sans danger.** Raphaël est « 100 % d'accord » avec deux constats de la matrice M1 (plan 42 §5), qu'il a lui-même faits en jouant : avec la méta complète, passé 15 min, le bot ne perd presque plus de PV ; aucun build de référence ne casse le jeu plus qu'un autre, mais tous rendent la fin de run sans danger. « c'est un sujet super méga important. » Pistes proposées le jour même, à trancher.

## 166. Un passif unique et plus identitaire par personnage — 14 octobre 2026

**Suite P6 :** Raphaël accepte Endurci par la route (terrain inédit → trois charges réduisant les blessures) pour le Vagabond et Redresser (chaque quatrième attaque réussie → onde avec recul au point touché) pour la Forgeuse : « OK les deux me vont implémente les. ne déploie pas plein dagents ect et ne fait pas 100 tests plzz ». Implémentation directe sans sous-agents, build/smoke obligatoires, une régression ciblée et une capture commune ; paramètres d’essai au plan 06 P6.

**Livraison P6 :** deux modules composés et paramètres JSON, anciennes signatures remplacées ; trois charges à −50 % sur les blessures de combat, onde toutes les quatre émissions réussies à 60 % de référence, 90 px et recul de 25 px. Build zéro warning, smoke et régression limitée verts ; capture commune relue dans Main. Aucun sous-agent ni mesure de densité/performance. Coefficients d’essai, équilibrage humain ouvert.

Raphaël veut un passif propre à chaque personnage, en plus de sa mobilité et de son arme de départ. Après examen des signatures existantes : « je verrai bien un passif plus marqué et identitaire que ceux déjà présents qui sont peut etre à revoir (en vrai ca peut aller mais je mettrai pas 2 bonus, un seul par personnage suffit). » Il demande de regarder [les personnages de Megabonk](https://megabonk.fandom.com/wiki/Characters).

**Acquis :**
- un passif identitaire par personnage ; les signatures existantes peuvent être revues, sans obligation de toutes les remplacer ; plusieurs effets sont autorisés (précision ci-dessous) ;
- le passif doit porter une identité forte et influencer la façon de jouer ou de construire son build ;
- les quatre points d'intégration proposés sont acceptés : paramètres JSON, module composé par mécanique, événements existants, présentation explicite Passif / Mobilité / Arme de départ et feedback utile en run.

**Propositions de l'agent, non validées :** [plan 06 §14](06-personnages-quetes-defis.md#14-révision-des-passifs--14-octobre-2026). Une progression de statistique peut être identitaire si son ampleur et ses synergies sont significatives ; aucune règle de Megabonk ni aucun coefficient par niveau n'est repris automatiquement. Les six effets, leurs coefficients et leur ordre d'implémentation restent à arbitrer. Aucun changement de gameplay ni case de roadmap cochée sur cet accord de conception.

**Précision de Raphaël :** « cumuler des effets me dérange pas en fait » ; « je calquerai pas les effets comme tu l'as fait avec la chance sur vagabond car c'est aussi ce que fait le premier personnage de megabonk […] il faut etre différent ». La restriction à un seul effet est levée. L'association Vagabond / chance est rejetée comme trop proche de la référence ; la proposition Fortune de route est retirée. S'inspirer de la lisibilité et de l'impact des passifs, en construisant leurs effets à partir du lore et des systèmes de Vestiges. La simplification du Facteur à la cadence seule n'est plus une nécessité. Aucun des autres effets proposés n'est validé par cette correction.

**Retour suivant de Raphaël :**
- Traqueur : « un truc + simple pour les crits car les tirs sont automatiques ». Remplacer la proposition Acharnement, qui dépend d'une continuité de cible ; le nouveau mécanisme reste à choisir.
- Facteur : le passif Tournée est accepté dans son principe ; un effet sur la vitesse est envisagé (« ptet laisser un truc sur la vitesse aussi ? a voir »), sans arbitrage de son maintien ni de son montant.
- Éveillée : Double accepté dans son principe ; les coefficients et l'extension au-delà des tirs actuels restent à préciser.
- Scaphandrière : Pression, bonus de dégâts en zones effilochées et effacées, accepté dans son principe ; valeurs à calibrer. Cette validation concerne la proposition offensive, pas une nouvelle validation de la réserve d'air actuelle.

**Proposition de remplacement pour le Traqueur, non validée :** Œil d'aigle, critique garanti sur le premier impact direct contre une cible indemne. Le ciblage automatique suffit ; aucun maintien manuel d'une proie. Vagabond et Forgeuse restent à discuter. Ces retours valident des directions de conception ; aucun lancement d'implémentation ni équilibrage vérifié n'en découle.

**Dernier retour — lancement autorisé :** « trop fort en vrai, on peut faire ca mais dire qu'il y a une proba de crit sur le premier coup et qui monte avec les niveaux. ok go du reste, implémente moi ca. » Œil d’aigle devient une probabilité croissante avec le niveau, appliquée au premier impact direct contre une cible indemne, sans garantie. Implémenter le lot P5 du plan 06 : conserver vitesse + cadence de Tournée ; Double accepté, extension aux émissions de mêlée/chaîne/cône ; Pression remplace la réserve d’air, dégâts renforcés en Effiloché/Effacé, aucun effet dans le Néant. Vagabond et Forgeuse restent à concevoir, exclus du lot. Valeurs d’essai de l’agent : Traqueur 10 % au niveau 1, +0,25 point par niveau, plafond 50 % ; Pression +25/+50 %, multiplicatif après les malus de zone ; valeurs actuelles Facteur et Double conservées. Validation technique et captures requises ; ces chiffres ne constituent pas un équilibrage validé par Raphaël.

**P5 livré et vérifié :** Traqueur probabiliste (+10 points, +0,25 point/niveau, plafond +50 points), Facteur vitesse + cadence conservées, Double étendu hors orbitales continues, Pression +25/+50 % après malus. Snapshot isolé : 14/14 suites, build zéro warning ; captures des quatre personnages et fiches du Hub. Détails et limites au plan 06 §14 ; Vagabond/Forgeuse et équilibrage humain restent ouverts.

**Consigne de Raphaël pour les vérifications suivantes :** éviter trop de mesures et de vérifications, coûteuses en tokens. Préférer un build, les tests ciblés nécessaires et une capture utile ; réduire répétitions, matrices et mesures. Les contrôles obligatoires d’AGENTS.md restent applicables.

## 167. Fin de run : des créatures plus fortes, des défenses freinées, une cible de difficulté — 14 octobre 2026

Réponses de Raphaël aux pistes du §165 :
- **Ce qui doit menacer** : « les ennemis doivent devenir encore plus forts (PV/dégâts au fil du temps c'est le facteur principal et ils peuvent être plus nombreux. le monde peut devenir plus "instable". Pour survivre jusqu'au boss de 30 min il faut le vouloir (avoir un bon build et bien jouer. si n'importe qui peut y arriver du premier coup = échec. les armes, objets, écussons et améliorations permanentes à débloquer doivent aussi servir la progression). »
- **Cible** : un joueur moyen avec la méta complète **meurt souvent entre 20 et 30 min**.
- **Défenses** : « le vol de vie est trop fort actuellement et il faut aussi baisser la régen et probablement aussi l'armure ».
- **Niveaux de difficulté** (nouvelle idée) : « plusieurs niveaux de difficulté à choisir dès le début avant de commencer la run, de simple à impossible. cela aura un impact sur les ennemis leur fréquence puissance les boss etc. mais ça a aussi pas mal d'impact sur le jeu qu'il faut penser en amont (classement global par exemple, et sinon juste le pourquoi un joueur irait sur des niveaux plus élevés à part pour le défi personnel) ». À concevoir avec les paliers de départ du plan 46.

Découpage proposé au [plan 47](47-fin-de-run-menacante.md) : L1 défenses freinées, L2 créatures qui suivent après 15 min (avec mesure), L3 monde instable (plan 42), L4 niveaux de difficulté (avec le plan 46).

## 168. Niveaux de difficulté : un classement, des niveaux à gagner ; Vestiges divisés par 3 — 14 octobre 2026

Réponses de Raphaël sur le plan 47 :
- **Classement** : un seul classement mondial aux éliminations, tous niveaux confondus ; un niveau plus dur donne plus de créatures, donc plus d'éliminations possibles.
- **Accès** : chaque niveau s'ouvre en battant l'Indicible au niveau d'avant.
- **Pourquoi monter** : « les vestiges auront peu d'intérêt une fois tout débloqué. donc je dirais peut-être quand même le choix + de vestiges pour les nouveaux joueurs le temps de tout débloquer (il faut aussi diminuer drastiquement le gain de vestiges au moins d'un facteur 2 ou 3, et en gros prendre ce nouveau facteur qui sera entre 0.2 et 0.4 x le nombre de kills et ça sera pour les runs "simples" et ça monte avec les runs plus dures ». Les autres motifs (déblocages réservés, marques de victoire) restent ouverts.
- **Fait le jour même** : 0,4 Vestige par élimination (`kills_per_vestige` 0,85 → 2,5), soit un gain divisé par environ 3. La matrice M1 donnait le Campement plein en 11 runs moyennes (cible 30 à 50). Le multiplicateur par niveau viendra avec le lot L4.
- **L1 et L2** : feu vert. D'abord recaler le bot (aller chercher les Souverains, esquiver moins loin), puis défenses et créatures réglées ensemble sur la cible du §167, avec mesure.

## 169. Vol de vie : l'ancien plafond, une montée plus lente ; plus d'élites, vraiment plus fortes — 14 octobre 2026

Retours de Raphaël sur le premier réglage du plan 47 :
- **Vol de vie** : « je ne plafonnerais pas le vol de vie [à 2 %] mais par contre le scaling doit être plus long. » Choix : l'ancien plafond de 8 % des PV max par seconde revient, et le vol de vie monte trois fois plus lentement (0,15 % par niveau de l'Écusson du barbier ou par Ampoule, au lieu de 0,5 %).
- **PV et dégâts des créatures** : le ×1,375 du Péril du temps lui semblait « très léger ». Mis au clair : il s'ajoute à la croissance existante ; à 30 min, les PV valent déjà environ ×130 à ×180 du début, les dégâts seulement ×5 à ×6. Choix : dégâts ×12 à ×15 à 30 min (1,09 par minute après 15 min, `damage_scaling_segments`), « mais ce qui fait la diff c'est d'avoir plus d'élites qui elles sont vraiment plus fortes ».
- **Élites de fin de run** : de 15 à 30 min, elles apparaissent toutes les 12 à 20 s au lieu de 50 à 75, jusqu'à 8 en même temps au lieu de 2, avec PV ×3 et dégâts ×1,8 en plus de la variante, et un affixe de plus passé 22 min 30.
- **Traqueur** : « ok le traqueur on va le régler aussi ». Son attaque passe de 12 à 10, comme le Vagabond et le Facteur ; son passif (portée +20 %, cadence +10 %) reste son identité.

## 170. Vol de vie plafonné à 5 % ; les boules de neige gênent — 14 octobre 2026

- Raphaël : « vol de vie met 5 % pas 8 en plafond ». Plafond à 5 % des PV max par seconde, montée lente inchangée (0,15 % par niveau ou par Ampoule).
- « ça me gêne qu'il y ait encore des boules de neige. je vais tester ça » : une graine sur trois emmène encore le Traqueur au niveau 155 à 30 min, intouchable (plan 47 §4 bis). Raphaël teste en jeu avant la suite.

## 171. Début de run moins saturé, projectiles entiers, XP plus lente — 14 octobre 2026

Trois runs de Raphaël (profil dev, tout débloqué, quatre armes, mode actuel « relativement simple ») après le plan 47 :
- **Équilibrage** : « beaucoup mieux » ; aucune run où il s'est senti trop fort et intouchable.
- **Trop de créatures** les dix premières minutes, « peut-être deux fois trop » : « spammé d'ennemis en permanence », elles foncent toutes dessus, on est « sur-stimulé » ; Megabonk en a beaucoup sans que ce soit ingérable. Peut-être aussi la cause d'une montée de niveau encore trop rapide.
- **Projectiles** : plus il montait l'Écusson du jongleur, moins un niveau en donnait (+21,5 → +21,88 au niveau 38, rendement décroissant après 30). « On n'est pas censé recevoir moins d'un projectile par niveau » : 1, 1,x ou 2,x, jamais 0,5. L'amélioration de projectiles est très forte et sortait presque à chaque offre (poids d'offre doublé, et l'Écusson monte aussi tout seul en touchant).

**Choix :**
- débit −40 % (cible de créatures proches et intervalle d'apparition) jusqu'à 10 min, retour progressif au réglage normal à 14 min ;
- stats entières : au moins 1 par carte d'arme (1 / 1,5 / 2 / 2,5 / 3 selon la rareté) et par niveau d'Écusson. Règle commune à tous les Écussons : un niveau ne donne jamais moins d'une unité sur une stat qui se compte, rendement décroissant compris. Pas de plafond propre au jongleur (« c'est bizarre d'avoir des règles différentes selon les écussons »). Le jongleur et le Reflet brisé passent à 1 par niveau ; le jongleur n'est plus favorisé (poids 1) ; leurs seuils d'usage sont relevés (2 500 touches, 1 000 perforations) ;
- XP : courbe ralentie (exposant 1,4 → 1,5). Les bonus d'XP de la méta ne baissent pas, ils pourraient même monter un peu : monter vite doit demander de construire pour l'XP, ce qui fait aussi une marche pour les débutants.

**Mesure** (bot moyen, 15 min, trois graines), avant → après : créatures visibles en moyenne de 1 à 10 min, Traqueur méta complète 28 → 12, Vagabond 31 → 20, Facteur 31 → 16, Traqueur méta milieu 34 → 18 ; niveau à 10 min 47 → 40, 42 → 26, 42 → 37, 37 → 34 ; à 14 min 70 → 59, 64 → 43, 58 → 52, 58 → 49.

## 172. Textes d'objets lisibles, aimant d'XP plus court, zones au sol rondes mais travaillées — 14 octobre 2026

Retours du 13 au soir : « bcp d'effets d'objets ou d'écussons sont durs à lire (par exemple alumette humide) mais en réalité ca en touche bcp » ; « on ramasse l'xp de bcp trop loin par défaut ce qui facilite bcp le level up ».

**Constat de l'agent :** 32 objets avaient deux textes, celui du JSON jamais affiché et un texte écrit en dur dans `ItemPresentation.cs`, chargé de mécanique interne (« coefficient de l'arme (au plus 1) », « applications/s », « répit », « sans récursion ») ; 40 répétaient leur ligne de stat en phrase. L'aimant attire à 150 px (56 % de la demi-hauteur d'écran, plus que la portée des armes de mêlée) et fait dériver les orbes depuis 250 px quand on s'arrête.

**Réponses de Raphaël (14 octobre) :**
- textes : une phrase courte, dans les mots du joueur ; les limites techniques à part, **dans la Collection seulement** ;
- objets de stat : la ligne de stat, puis une phrase d'ambiance ;
- aimant d'XP : **80 px, sans dérive** ; bonus d'aimant (Campement, Bande magnétique, Écusson, bénédiction, chariot) gardés en pourcentage ; bonus Aimant au sol inchangé (« rien en fait ») ;
- zones au sol (suite du §164) : « il faut que les contours soient plus réguliers en cercle, mais que ca soit bcp plus travaillé et créatif en terme d'effet (je parle de tout ce qui fait des zones au sol) » ;
- ordre : les textes d'abord ; la revue du tableau de bord attendra.

**Livré :** textes ([plan 35 I18](35-raretes-des-objets.md#i18--textes-lisibles-14-octobre-2026)) ; aimant à 80 px sans dérive, réglé dans `data/scaling/progression.json` (`attraction_radius`), à juger en jouant. Raphaël ne veut pas de mesure pour ce réglage (« pas besoin de mesurer pour ca »). Les zones au sol restent à faire.

**Retour sur la Collection, le soir même :** trop de lignes sous un objet ; la règle courte est « correcte », mais l'ambiance avait la même taille, la même couleur et la même police qu'elle, alors qu'elles « n'ont pas la même importance » ; la ligne grise (« Doublons sans limite… ») n'a « pas du tout besoin » d'être montrée. Pour les détails (limites techniques), Raphaël hésite, puis choisit de **les supprimer partout**.

**Appliqué :** ambiance en plus petit et en gris sous la règle ; ligne grise retirée pour les objets et les Écussons (plafond de copies, « Cousu en souvenir de… ») ; le champ `details` et ses textes disparaissent. Un Écusson montre sa ligne de stat au niveau 1, puis sa règle s'il en a une.

## 173. Almanac : un outil maison et public pour suivre le jeu — 14 octobre 2026

Demande : un outil de gestion de projet de jeu vidéo, open source et installé sur son serveur, qui regroupe moodboards, listes et documentation, parce que « plein de systèmes s'entremêlent », que les docs partent « vraiment dans tous les sens » et que « les données du jeu ne sont pas accessibles facilement alors que ça devrait l'être ».

**Réponses de Raphaël :**
- **fait maison**, dans un **dépôt public** à part, réutilisé ensuite en interne ; nom : **Almanac** ;
- suivre **toutes les stats** (objets, personnages, effets, variables), **toute la documentation** ; si possible gestionnaire de **sprites et de sons**, **moodboards**, **outil de mesure** de runs et d'équilibrage ;
- **agentique** : MCP, faciliter la vie des agents, **minimiser les tokens** ; **une seule source de vérité**, « un gros danger » sur un projet de cette taille ;
- le contenu **reste dans le dépôt du jeu**, l'outil le lit et y écrit ;
- stack « la plus rapide et la plus légère », TypeScript ;
- hébergement : Proxmox, Docker Compose dans une VM Debian 13, convention `~/apps/<projet>/` avec `docker-compose.yml` et `.env` ;
- ensuite, **grand ménage** du dépôt (docs, scripts) et migration.

**Suite :** plan de l'outil dans `~/dev/perso/almanac/PLAN.md` ; ménage et migration de Vestiges au [plan 48](48-almanac.md), à démarrer au premier jalon d'Almanac. Plan à valider.

## 174. Projectiles du joueur plus lents — 14 octobre 2026

Retour de jeu : « le bullet time des projectile est trop rapide en général ». Précisé : il s'agit de la vitesse de vol, pas de la cadence ; projectiles ennemis inchangés.

**Réglage :** `projectile_speed` −30 % sur les armes à distance du joueur (Arc du gymnase 280, Lance-billes 210, Cloueuse 350, Assiettes 245, Lentille de phare 560, Instantané 420, Sacoche de lettres 240, Fusil-harpon 180). Les armes déjà lentes baissent de 15 % seulement, pour garder le virage des têtes chercheuses : Boussole 170, Baguette de sourcier 210, Lampe à pétrole 155, Craies 130. La portée ne change pas : le trajet dure plus longtemps. `tools/test_weapons.sh` vert.

**Questions tranchées le même jour :** dépôt d'Almanac en anglais avec interface bilingue français/anglais ; licence MIT ; dépôt **privé jusqu'à la fin de la phase B**, puis public ; contrats de Vestiges lus par adaptateur, sans conversion ; instance du NAS en lecture seule en V1 ; Godot sur le NAS plus tard, si besoin ; un schéma pour **toutes** les collections de `data/` pendant la migration ; mises à jour datées du README gardées écrites à la main.

## 175. Tirs étalés sur la recharge, cadence plus lente, stats entières sous l'unité en commune, plus aucun rendement décroissant — 14 octobre 2026

Retours de jeu de Raphaël après le §174 :
- **Cadence** : « la cadence de tir elle-même est trop forte » ; surtout « les délais entre les tirs successifs : si on a trois projectiles, il ne faut pas les tirer en rafale mais les écarter dans le délai prévu avant le prochain tir ».
- **Stats entières** : il revient sur le §171 : une amélioration commune peut donner moins d'un projectile entier ; plus d'un est réservé aux rangs supérieurs. Même règle pour les cartes d'arme et l'Écusson de l'archer.
- **Rendement décroissant** : « il ne faut pas de rendement décroissant du tout dans le jeu dans les améliorations d'armes / d'écussons. Ça doit même être l'inverse : le rendement doit être stable (ou croissant avec la luck, car la luck améliore le niveau des améliorations). Je sais que ça peut “péter” les runs, donc il faut améliorer les courbes de difficulté en fonction de ça. Mais j'y tiens beaucoup, car c'est très frustrant d'avoir un rendement décroissant dans un jeu incrémental. »
- **Affichage** : la carte d'amélioration d'une arme additionnait ses projectiles propres et ceux des Écussons (« 4 → 5 » pour un arc niveau 1 et un Jongleur à +3) ; le nombre de l'arme doit s'afficher à part.

**Choix :**
- les tirs visés d'une attaque se répartissent à intervalles égaux sur toute la recharge (`shot_cooldown_share` 1,0 dans `weapon_visuals.json`, à la place de `shot_interval_s` 0,08 s et `shot_max_cooldown_share`) : 3 tirs et 1 s de recharge, un tir toutes les 0,33 s. L'éventail propre des armes en salve (Lance-billes, Assiettes, Instantané) part toujours d'un coup ;
- `attack_speed` de base −20 % pour les armes à distance et les Craies, −10 % pour la mêlée (Arc du gymnase 0,83 → 1,04 s entre deux attaques, Lance-billes 0,56 → 0,69 s, Lame ébréchée 1,00 → 1,11 s) ;
- stats entières : 0,5 / 0,75 / 1 / 1,5 / 2 de la commune à la légendaire, pour les cartes d'arme (projectiles, perforation, rebonds) et pour le Jongleur et l'Écusson de l'archer. `integer_gain` devient un multiple du pas (1 / 1,5 / 2 / 3 / 4) ; ces stats portent un pas de 0,5, et les effets d'Écusson concernés sont marqués `integer`. Les seuils d'usage relevés au §171 (2 500 touches, 1 000 perforations) ne bougent pas ;
- plus aucun rendement décroissant des Écussons : `badge_soft_cap_level` et `badge_soft_cap_factor` sont retirés, et chaque niveau vaut son pas entier jusqu'au niveau 99. Le plancher d'une unité du §171 disparaît avec lui ;
- la carte d'amélioration d'arme montre le nombre propre de l'arme pour une stat qui se compte ; la fiche de pause sépare base de l'arme, niveaux de l'arme et build.

**À faire :** les courbes de difficulté doivent suivre une puissance qui ne plafonne plus (plan 47, fin de run sans danger). Une commune sur la Boîte à musique donne +0,5 orbe, visible seulement à la carte suivante, car les orbes ne comptent que la partie entière : à revoir si cela gêne.

**Vérification :** build sans avertissement ; suites smoke, objets, armes, inventaire, combat d'objets, effets de perks, écran de choix, catalogues, quêtes et navigation des menus vertes (validation headless sur une copie propre). Relecture `godot-reviewer` : rien de bloquant.

## 176. Arme prête : un coup gardé jusqu'à ce qu'une créature arrive à portée — 14 octobre 2026

Retour de jeu après le §175, avec la Rallonge : une créature arrive à portée d'une arme de mêlée, et le coup ne part que plus tard ; « pour les armes corps à corps, c'est pas du tout logique », « hyper désagréable comme sensation ».

**Cause :** le minuteur de chaque arme tournait en continu. Une recharge finie sans cible à portée perdait son coup, et l'arme attendait une recharge entière de plus. Une créature qui arrivait au contact attendait donc entre 0 et une recharge complète avant le premier coup. Le défaut existait déjà ; la cadence plus lente du §175 l'a rendu flagrant (Rallonge : jusqu'à 1,23 s).

**Correction :** une recharge finie sans cible met l'arme **prête** : son minuteur s'arrête, et chaque image elle vérifie, sans allocation, si une créature est à portée, mesurée comme son attaque la mesure (ellipse du coup de mêlée, cercle agrandi par la taille). Elle frappe dès qu'une créature entre à portée, et sa recharge repart de ce coup. La règle vaut pour toutes les armes à coups (mêlée, tirs, chaîne), pas pour l'orbite ni le cône continu. Le Ressort de sommier ne compte plus que les attaques qui partent. Code : `Player.WeaponReadiness.cs`.

**Vérification :** build sans avertissement ; suites smoke, armes (nouveau contrôle : Rallonge prête, frappe aussitôt à l'arrivée d'une créature, recharge relancée), objets, effets de perks et combat d'objets vertes.

## 177. Formes des Craies et écho des Gants couchés au sol — 14 octobre 2026

Retour de jeu : les dessins au sol des Craies « ne semblent pas être en vue isométrique comme le jeu » ; « corrige ça en les aplatissant et aplatis aussi la zone ».

**Correction :** la forme (étoile, cercle, maison, soleil) se calcule au sol puis se projette (`Iso.ToScreen`) : une ellipse deux fois plus large que haute, de même largeur qu'avant. La zone de dégâts se mesure sur cette même ellipse (`Iso.GroundDistanceSquared`), comme le Chronomètre, le Mètre ou les échos de zone d'objets. Cela lève le point ouvert du plan 27 pour les Craies. Conséquence : à rayon égal, la surface touchée est divisée par deux (même largeur, moitié de hauteur).

**Au passage :** la craie en vol tournait dressée vers l'écran, une pose sur deux ; elle tourne maintenant couchée autour de la verticale, en 8 frames à 20 images/s (`tools/sprites/projectiles.py`, planche régénérée).

**Écho des Gants de boxe** (demandé ensuite) : même correction, la zone et son dessin (zone tramée, anneau) sont couchés au sol, mesurés par `Iso.GroundDistanceSquared` ; même division par deux de la surface à rayon égal (`echo_radius`).

**Vérification :** aucun test lancé, Raphaël vérifie en jeu.

## 178. Sons de menu dans l'ambiance du jeu — 14 octobre 2026

Raphaël : « les sons de sélection des menus ne sont pas assez dans l'ambiance du jeu, comment remédier à ça ».

**Diagnostic (mesuré, non écouté) :** survol, clic et confirmation viennent du paquet Kenney Interface Sounds, choisis le 7 octobre parmi des candidats du même genre. Le survol dure 20 ms et centre sa brillance à 10,7 kHz, le clic à 4,8 kHz, alors que les sons du monde sont coupés vers 3,5–4,5 kHz et faits de matières (bois, craie, papier, cloche, piano patiné). Le survol, joué à chaque déplacement du focus, est le plus en cause.

**Acquis :** Raphaël retient la piste proposée, une famille de sons faits des matières du jeu, en synthèse originale, plutôt qu'un simple adoucissement des fichiers actuels. Lot U1 au [plan 15](15-audio.md#u1--sons-de-menu-faits-des-matières-du-jeu--14-octobre-2026) : planche d'écoute hors dépôt, rien n'est intégré avant son choix.

**Choix du même jour :** survol B (papier), clic A (pierre), confirmer C (petite cloche de bronze), refus C (corde étouffée), intégrés ([plan 15 U1](15-audio.md#u1-livré--choix-du-14-octobre)). Sur le survol : « B mais aucun des 3 proposés ne me plait vraiment » ; B est gardé en attendant une seconde planche.

**Survol, même jour :** ce qui gêne dans les trois premiers, « trop présent » et « trop de souffle » ; directions retenues pour la seconde planche : bronze effleuré et cran mécanique ([plan 15 U1b](15-audio.md#u1b--seconde-planche-du-survol-14-octobre-à-écouter)).

**Retours en jeu, même jour :** survol retenu sur la seconde planche, cran mécanique simple. Le retour et le clic sonnaient pareil : ouvrir une page et revenir jouaient tous deux la cloche, par-dessus le clic. **Acquis :** la cloche est réservée aux vraies validations, ouvrir une page ne joue que le clic, le retour a son propre son joué seul (cran inversé, choix provisoire), sans clic d'appui dessous. Le clic de pierre « n'est pas assez reconnaissable ni percutant » : seconde planche U1d au [plan 15](15-audio.md#u1d--clic-de-bouton-14-octobre-à-écouter).

**Survol dans le Campement et dans Partir :** « quand on hover un élément […] il faut l'afficher ». Raphaël choisit l'aperçu : survoler une amélioration ou un personnage affiche sa fiche le temps du survol (et du focus dans Partir), puis la fiche revient à l'élément choisi. Le choix ne change toujours qu'au clic : cela précise le §154, sans l'annuler.

**Mix et doublons, même jour :** survol baissé (−3 dB), clic monté (+4 dB). Les sons « en double » venaient du survol joué à chaque focus donné par le jeu ou par le clic : le focus ne sonne plus qu'au déplacement du joueur.

## 179. Paramètres : curseurs dans la matière des menus, fermer carré — 14 octobre 2026

Raphaël : « améliore après les sliders des paramètres, donne-leur un aspect plus beau et dans la DA du jeu, et le bouton fermer n'est pas carré… mets une vraie croix au lieu d'un x ».

**Fait :** rainure de pierre, incrustation et poignée de bronze, bouton fermer carré à croix dessinée ([plan 04 §10](04-interfaces-et-hub.md#10-paramètres--curseurs-et-bouton-fermer--14-octobre-2026)). À juger en jeu.

**§178, clic retenu, même jour :** loquet (C de la planche U1d), au niveau de clic demandé ([plan 15](15-audio.md#u1d-livré--clic-en-loquet-14-octobre)).

**§178, famille finale du même jour :** plus de son au survol ; un même clic pour les boutons et le retour (caillou sur planche, +1 dB) ; un son de sélection à part (lamelle) pour choisir un personnage, un objet, une quête ou une amélioration dans une liste ; cloche pour les validations, corde étouffée pour les refus ([plan 15 U1f](15-audio.md#u1f-livré--survol-muet-clic-partagé-avec-le-retour-son-de-sélection-14-octobre)).

## 180. Arc du gymnase et Traqueur relevés à mi-chemin — 14 octobre 2026

Raphaël : « l'arc a été trop nerfé ainsi que le traqueur […] augmente le légèrement ». L'arc avait pris trois baisses le même jour : attaque du Traqueur 12 → 10 (§169), vitesse de vol 400 → 280 (§174), cadence 1,2 → 0,96 (§175).

**Réglage :** attaque du Traqueur 10 → 11 ; Arc du gymnase dégâts 8 → 9 et cadence 0,96 → 1,08 (0,93 s entre deux attaques au lieu de 1,04). La vitesse de vol reste à 280 : le §174 vaut pour tous les projectiles. `tools/test_weapons.sh` vert.

## 181. Atelier de test du mode dev — 14 octobre 2026

Raphaël : « Le mode admin / dev mode de test manque cruellement d'outil : pouvoir toggle un god mod, pouvoir choisir à sa guise les niveaux, les armes, le temps… ça pourrait être très pratique même pour les agents qui débuggent et qui font ça à la main avec des scripts ».

**Lot D1 :** panneau F4 enrichi et visible en run, commandes communes à l'interface et aux lancements automatisés ; invincibilité, progression exacte, composition d'armes et contrôle du temps. Détail et validation au [plan 49](49-outils-dev.md). Aucun changement de progression du profil normal ni de règle du gameplay distribué.

**D1 livré et vérifié :** build sans avertissement, smoke, profils et armes, commandes en vraie Main et exclusion des exports Debug/Release ; captures 720p/1080p. F4 et bouton en run, fermeture par F4/Échap, commandes répétables par `--dev-command`. L'horloge de difficulté règle les futurs spawns ; accélérer fait avancer la simulation entière. Fixer le niveau du joueur ne distribue pas de cartes et reconstruire une arme remplace ses gains.

## 182. Level-up « au fil », pour ne plus ressembler à Megabonk — 14 octobre 2026

Raphaël, captures de Megabonk et de Vestiges côte à côte : « à la réflexion je trouve que notre level up ressemble trop à celui de megabonk tu en dis quoi ? ca passe ou ca fait copie ». Avis de l'agent : trop proche, la même composition en trois colonnes (inventaire, offres, stats) et la même carte élément par élément ; légal, mais lu « clone » sur une vignette Steam. Raphaël : « au lieu des rectangles horizontaux ptet les mettre en vertical ? et disposer autrement les menus de gauche et de droite. franchement soit créatif surprend moi, ca doit etre différent de mégabonk ». Trois maquettes proposées (A le fil, B la ronde, C les stèles). Raphaël : « la A me plait beaucoup. implémente la. »

**Tranché :**
- Trois fragments de papier verticaux suspendus par des pinces à un fil tendu entre deux clous ; papier jauni teinté par la rareté, photo de l'objet scotchée, « NOUVEAU » et « Niv 7 → 8 » en coups de tampon.
- Plus de colonnes d'inventaire et de stats : l'inventaire devient une ceinture de cuir en bas de l'écran (armes, Écussons, sac, Réminiscences), la poche que touche le fragment survolé s'allume ; une étiquette pend sous le fragment survolé et dit ce qui change ; toutes les stats restent à une touche.
- Relancer, Bannir et Passer sont des étiquettes pendues aux clous. Bannir fait tomber le fragment, qui s'efface (l'oubli du §30).
- Remplace la forme Megabonk du §36 pour l'écran de level-up seulement ; la règle « une ligne de gain » reste. Lot au [plan 04 §11](04-interfaces-et-hub.md#11-level-up-au-fil--14-octobre-2026).

**Retour du soir, après la première version :** « alors c'est bien mais rajoute plus de détails ! et en particuliers les fonds derriere les icons des armes/objets pourraient etre bcp plus travaillés mais aussi le reste ! » Lot F2 : tirages photo détaillés et papiers, pinces, fil et ceinture plus fouillés ([plan 04 §11](04-interfaces-et-hub.md#11-level-up-au-fil--14-octobre-2026)).

**Retour suivant :** « c'est plus galere maintenant les actions annexes l'une est à gauche les autres à droite. soit rajouter des claviers soit (ca peut etre les deux) corriger ca ». Les deux, lot F3 : Relancer, Bannir et Passer pendent en colonne au clou de droite, dans l'ordre de la navigation ; « Toutes les stats » pend au clou de gauche ; raccourcis R / B / P (Y / X / LB à la manette), touche dessinée sur chaque étiquette.

## 183. Rallonge et Pelle à neige : animations trop semblables — 15 octobre 2026

Raphaël : « rallonge et pelle a neige ont quasiment le meme sprite, ca ne me plait pas (je parle du sprite en jeu - de l'animation) ». Reprise ciblée des effets d'attaque au [plan 25 S11](25-sprites-et-design.md#s11--distinguer-la-rallonge-de-la-pelle-à-neige-en-attaque-15-octobre-2026).

**Livré et vérifié :** la Rallonge dessine un câble fin et souple en S, relié au buste, avec sa prise au bout, qui se déroule puis fait un tour. La Pelle conserve son balayage large. Build sans avertissement, smoke et armes verts, captures des deux armes et de Déblayer/Enrouleur inspectées. Ressenti à juger en jeu par Raphaël.

## 184. Retours d'un premier testeur (30 min de jeu) — 15 octobre 2026

Raphaël, en vocal avec un joueur qui découvrait le jeu, plus ses propres remarques. Relevé du code et réponses le jour même ; lots au [plan 50](50-retours-premier-testeur.md).

**Constats du testeur** : coffres « quasiment impossibles » à ouvrir dans une foule (la jauge s'annule au moindre déplacement) ; autres lieux un peu lents ; créatures qui apparaissent « dans la gueule » en courant ; Vestiges trop durs à gagner depuis le nerf du §168 ; brouillard trop fort ; dix premières minutes « légèrement » trop dures ; il ne se déplace pas sur la carte et tourne en rond au même endroit ; il ne comprend pas l'intérêt du dash.

**Tranché :**
- **Coffres** : ouverture instantanée à l'appui sur E, collé au coffre (plus de jauge). **Autres lieux** : durées divisées par deux, et bouger n'annule plus la jauge, seul l'éloignement.
- **Apparition** : distance minimale au joueur, marge qui grandit avec sa vitesse dans sa direction (décision technique de l'agent).
- **Vestiges** : +75 %, 0,7 par élimination. **Campement** : retrait des six stats brutes (§152), Aimant, XP et Chance gardés ; ajout d'une amélioration « gain de Vestiges ». Trouver plus tard d'autres usages aux Vestiges.
- **Brouillard épais** (météo) : vue nette 170 → 260 px, voile complet 360 → 480 px, opacité 0,82 → 0,6.
- **Début de run** : XP requise −5 % sur les niveaux d'avant 10 min, créatures −5 % jusqu'à 10 min. « Vraiment un tout petit peu. »
- **Exploration** : repères au bord de l'écran pour tous (aujourd'hui seulement par l'Étiquette perdue : « il faudrait le retirer ou le booster ») et première run guidée.
- **Dash** : invulnérable pendant le dash, et le montrer dès le début.
- **Difficulté** (« important et urgent ») : quatre niveaux, Simple et Normal ouverts d'office, Difficile puis Impossible s'ouvrent en battant l'Indicible au niveau d'avant. Chaque niveau règle nombre, PV et dégâts des créatures, boss et multiplicateur de Vestiges.
- **Essence** (retour ajouté) : « le nombre de kills c'est le même que le nombre d'essence ». Essence selon la créature, dans son JSON. « Mais alors il faut que les coffres deviennent de plus en plus chers » : **le prix monte par coffre ouvert, plus avec le temps de jeu**. Les coffres lâchés par les boss et les élites sont gratuits.
- **Ordre** : « tout dans l'ordre » : réglages, Campement, difficulté, Essence et coffres, repères et première run guidée.
- Plus tard : mouvement trop basique (« juste haut, bas, gauche, droite et dash »), à reprendre avec le plan 42 §6.

**Niveaux de difficulté, le même jour** (plan 50 T3) : « écart moyen » retenu (Simple ×0,75 créatures, ×0,8 PV, ×0,7 dégâts ; Difficile ×1,3 / ×1,35 / ×1,3, Vestiges ×1,25 ; Impossible ×1,6 / ×1,8 / ×1,7, Vestiges ×1,5), les boss suivant PV et dégâts. En plus : Effacement plus rapide, élites plus fréquentes **et** Péril de départ (3 en Difficile, 6 en Impossible), qui s'ajoute aux multiplicateurs. Choix sur l'écran du personnage, sous la fiche, dernier niveau retenu.

**Lisibilité, le même jour** (plan 50 T5) : deux flèches de coffres pour tous, **Étiquette perdue retirée** ; invite du dash au premier danger, une fois par profil. Première run guidée : « j'aurais tendance à dire trois étapes », mais Raphaël pointe un manque plus large : **pas d'objectif dans la partie**. « Le but de Megabonk, c'est trouver le portail et activer le portail pour passer au niveau d'après. Risk of Rain 2, c'est pareil » ; « quand la partie dure depuis 15-20 minutes, ça peut être perdu » ; « rendre le jeu moins linéaire […] plus structurer le jeu, plus rendre la boucle de gameplay plus complexe ou plus intéressante », avec un champ de liberté plus restreint (carte qui s'efface, boss à heure fixe). « Il faudrait y mettre un certain investissement de recherche et développement. » Chantier de conception à ouvrir ; la première run guidée attend son résultat, ses étapes devant mener à l'objectif de la run.

## 185. Objectifs dans la run : boss invoqué, terres successives à étudier — 15 octobre 2026

Brainstorm sur le manque relevé au §184 (pas d'objectif, joueur perdu vers 15–20 min). Quatre pistes proposées : A, Mémoriaux à raviver pour appeler l'Indicible ; B, piste narrative ; C, terres successives ; D, contrats au choix. Étude au [plan 51](51-objectifs-de-run.md).

**Tranché :**
- **A presque acquise, C à étudier** : « A et C me parlent beaucoup (que ce soit l'un l'autre ou les deux. mais A je suis presque sur qu'il le faut) il faut creuser dans ces directions sur la faisabilité et les implications ».
- **Boss plus à heure fixe** : « on peut l'invoquer quand on veut […] si on rassemble assez de mémoriaux ou qu'on active un portail on peut lancer le boss », à tordre selon le lore. Ce qui pousse à le faire : une difficulté qui monte sans cesse, « au bout d'un moment, ça sera intenable. Donc on sera obligé de faire le boss, soit pour passer au late game, soit pour aller sur la map suivante ».
- **Ampleur** : plusieurs terres successives le tentent, avec des implications majeures (quatre ou cinq biomes aujourd'hui). Le lore peut être retouché « même en profondeur », en gardant une cohérence globale.
- **Ressenti visé** : course contre l'Effacement, pression et choix serrés, « limite une difficulté croissante comme Risk of Rain 2 et Megabonk ».
- **Écartées** : la piste narrative (B) et les contrats (D).

**Le même jour, après le plan 51 (vocal)** : Raphaël écarte le lore comme contrainte de départ. « Le lore n'a pas été écrit forcément en pensant au gameplay », il ne se prête pas à plusieurs cartes. Le reprendre ou le réécrire en partie est permis, « mais il ne faut pas juste tout chambouler de manière bête », en gardant une cohérence globale.
- **Plusieurs cartes plutôt qu'une grande** : il trouve le concept de Megabonk et de Risk of Rain 2 « vraiment très bien », car on change de décor à chaque carte. « Quand on se balade dans une seule map et qu'à chaque fois c'est la même, on finit par s'ennuyer, moi je le ressens en jouant depuis plusieurs semaines. » Chaque niveau serait une carte plus travaillée, plus détaillée, « un peu spéciale ». « Le jeu partirait vraiment dans une autre direction, c'est un choix assez fort », mais il ressentait déjà que le jeu était trop linéaire.
- **Les boss** : Raphaël ne les a pas encore testés. Il n'aime pas trop leur construction : « c'est pas un gros personnage fort, c'est des sortes d'entités un peu spatiales ». À garder peut-être, sans certitude.
- **L'ambition** : être original comme Megabonk, qui mélange Risk of Rain 2 et Vampire Survivors et crée quelque chose de neuf. Tirer le meilleur de ces jeux avec une identité propre, qui n'est pas là pour être différente : un jeu aussi amusant à la première heure qu'à la deux-centième.
- **Durées** : une run réussie dure 20 à 30 min, une très grosse performance une heure ou un peu plus. La tension doit monter progressivement, pas rester constante.

**Suite du vocal, même jour : la run en terres retenue comme direction.**
- **Trois terres par run**, tirées parmi une sélection : cinq aujourd'hui, « on pourrait même étendre », ce qui permettrait d'ajouter des cartes. **Cartes plus petites** (« aujourd'hui la map est quand même très grande, pour moi c'est un problème »). **Environ 10 min par terre**, à confirmer.
- **Passage** : un portail est le trope classique ; Raphaël reste ouvert à quelque chose de plus créatif.
- **Un boss à la fin de chaque terre** lui va, avec « un nouveau bestiaire dédié aux boss ».
- **L'Effacement comme signature** : la terre qui se réduit de ses bords vers son cœur « c'est fort comme mécanique ». « Il faut vraiment penser ça à fond », pas trop frustrant, mais avec le sentiment de devoir se dépêcher. Image : les mini-jeux Minecraft où le sol de l'arène disparaît. À explorer : un îlot isolé devient-il inaccessible ? Peut-on traverser le vide en payant (Essence ou autre) ?
- **Route entre deux terres** (choisir la suivante entre deux) : intéressant, à condition que **chaque terre ait sa propre identité de jeu**. Aujourd'hui marais, forêt et champs ne diffèrent que visuellement.
- **La boucle de jeu d'abord, le lore ensuite** : « jusqu'à présent la boucle de gameplay n'avait pas été spécialement étudiée, elle s'était plus adaptée en fonction du lore, alors que normalement ça aurait dû être l'inverse ». Le but est de donner envie de rejouer, « même avant le lore ».

**16 octobre, sur l'innovation** (après une critique de l'agent : structure empruntée, terre qui rétrécit pas inédite, mouvement trop pauvre) :
- **La séparation en plusieurs cartes est la bonne direction**, mais la structure n'est pas forcément définitive : « j'ai peur que ça ressemble trop aussi à Megabonk ».
- **Il manque une mécanique centrale** : à la fois proche du genre et différenciante, « vraiment accrochante, qui a un vrai but, un vrai intérêt, qui est intelligente […] pas un truc débile ». Le vide comme arme « pourrait éventuellement être intéressant », sans accord à 100 %.
- **Mouvement** : d'accord, c'est la force de Megabonk (sauts, glissades, vitesse, sensation de puissance), et le jeu ne l'a pas. Doute qu'on y arrive « juste avec des tirs automatiques » en 2,5D, mais il en faut plus.
- **Pas de capharnaüm** : un ensemble structuré et logique.
- **Maîtrise** : entre la première run et la centième, « il faut qu'on ait l'impression d'être devenu meilleur, de mieux maîtriser les mécaniques, en plus du fait de mieux comprendre le jeu ». Ça manque aujourd'hui.

**16 octobre, mécanique et structure :**
- **L'Élan retenu comme mécanique centrale.** Proposé par l'agent : une ressource qui se gagne par le mouvement bien joué (avancer sans s'arrêter, glisser, esquive parfaite, sauter le vide) et se perd à l'arrêt ou sous les coups ; elle donne vitesse et puissance, la percussion qui projette dans le Néant, un saut au-dessus du vide. Raphaël : « franchement, très bonne idée […] il faut rapidement commencer à implémenter ça […] et mettre les bons efforts dedans ». **À valider d'abord**, avant la structure : « le plus important, c'est d'abord valider la nouvelle mécanique ».
- **Structure à acter** pour ne pas rester dans le flou : des niveaux à temps limité, mais « une map précise, un temps limité, un boss à activer et on passe au niveau suivant, ça ressemble trop à Megabonk […] malheureusement, ça marche très bien ». Il faut une variante.
- Trois variantes proposées : la terre qui se défait, la Traversée, le Mangeur (un gardien qui erre, mange la terre et grossit). Le Mangeur « me semble être la meilleure idée des 3 », mais sans conviction : « surtout une galère à coder et à designer ».
- **Boss** : ils doivent être **agressifs** et avoir de **vrais designs de boss**. « La Barrière, avec le recul, c'est peut-être pas le mieux. »
- **Structure, réponse du 16 octobre** : « il faut être vraiment plus libre dans la pensée, éventuellement là on essaie encore trop de se rattacher à l'existant ». **La terre qui tombe (option 1) est préférée ; la Traversée est écartée** ; les détails restent à étudier. « Pas de timer » : « ça serait quand même bien », ou à défaut une **jauge** (difficulté, effacement, à voir). Les Mémoriaux en ancres et en puissance lui vont. Idée à discuter : une mécanique de **zone à tenir** (« capture the flag, du genre rester dans une zone délimitée »).
- **Bac à sable de l'Élan : feu vert, maintenant** (plan 52).
- **Décision du 13 octobre sur l'élan annulée** (§151 : « l'élan, la vitesse rend plus fort, n'est pas retenu ») : « j'annule ma décision du 13. Je pense qu'il faut un système de version "maîtrise" avec puissance, mais qui puisse prendre en compte aussi la vitesse. Ce truc de gestes maîtrisés est très intéressant et donne un vrai côté stimulant. Après, à voir dans l'implémentation. » Élan = gestes maîtrisés (esquive parfaite, glissade, saut du vide, percussion) **et** vitesse, avec de la puissance à la clé. Plan 52.

## 186. La boucle de jeu arrêtée — 16 octobre 2026 (soir)

Reprise du plan 51 pour formaliser la boucle avant de l'implémenter. Raphaël revient sur trois points du §185 : « Mémoriaux : des ancres et de la puissance, pas des clés, je ne suis pas sûr de ça au final » ; le choix de la terre suivante, « je sais pas si c'est nécessaire » ; et « trois terres d'environ 10 min », qui « reprend la durée d'une run de Megabonk avec le même format […] pas très original ». Il demande à l'agent d'être force de proposition et de penser en game designer : un jeu compréhensible par n'importe quel joueur, aux objectifs clairs, auquel on veut rejouer. Détail au [plan 51 §11](51-objectifs-de-run.md).

**Tranché :**
- **Mémoriaux : clés, ancres et puissance.** Il en faut trois pour réveiller le gardien ; chacun ancre sa zone contre la chute et donne une puissance. Chaque Mémorial de plus rend le gardien plus fort et plus généreux.
- **Raviver un Mémorial = tenir son cercle environ 20 s**, assez large pour y courir (la zone à tenir du §185).
- **Pas de choix de la terre suivante** : les terres sont tirées.
- **Le passage** : la terre suivante se recoud autour du joueur, « ou [un portail] si [la recousure est] trop dur[e] ».
- **Le format** : trois formats plus audacieux proposés (l'île qui dérive, l'archipel, des terres de plus en plus courtes avec la Montée en finale fixe). Une première version accélérée avec la Montée : « c'est Megabonk… en vrai peut-être qu'il faut penser le format différemment… c'est une prise de tête ». Puis : « pour l'instant on va faire plus simple, en gros l'idée principale juste avant celle-ci : le 3×10 min environ, avec montée en difficulté constante et late game infini (avec difficulté qui continue de monter) ». Les formats audacieux sont mis en réserve (plan 51 §11.5).
- **Le late game** : « une 4e map dédiée au late game qui reste à l'infini ». Sa conception reste à faire.
- **Le vide** : les petits trous se sautent avec l'Élan, et le traverser draine l'Essence puis les PV.
- **Le gardien** : au réveil, le sol non ancré s'effondre, et l'arène est ce qu'on a sauvé. Pendant le combat, le front se fige et les coups du gardien brisent le sol.
- **La jauge** : un anneau de terre restante autour de la minimap, avec un repère qui marque quand le gardien se lève seul. Aucun chiffre.
- **La charte des gardiens** (plan 51 §10) est validée telle quelle.
- **Ordre** : « valider ça, puis ensuite, quand ça sera bon, on passera sur l'Élan » ; le prototype des terres vient après les lots E2 et E3.

**17 octobre, après essai du bac à sable** : l'esquive parfaite (lot E2) « c'est un peu difficile mais c'est une bonne chose ». **Ordre changé** : « je voudrais d'abord bosser sur la boucle de jeu et le cycle plutôt ». Le prototype des terres (plan 51, P0 à P3) passe avant les lots E3 à E6 de l'Élan ; le vide s'y traverse d'abord à pied, en payant, et le saut d'Élan viendra avec E3.

**17 octobre, terres par défaut** : « je voudrais que tu branches le nouveau système sur le jeu principal, par défaut ». Toutes les runs passent par les terres (plan 51 §15), la grande carte reste disponible aux mesures (`--no-lands`). Idée du même jour, non tranchée : « faire quelque chose avec les ennemis qu'on tue ? avoir une mécanique liée à ça. Je ne suis pas sûr de cette idée. » Trois pistes au plan 51 §15, la recommandée : les morts dans le cercle d'un Mémorial le raniment plus vite.

**17 octobre, ce qui manque pour une run complète** : « ça serait bien de traiter le late game alors. Et les boss, ils ont été faits ? Bref, sur ce qui manque, fais-le. Le 10 min par terre devrait être timé avec un chronomètre, ou alors un système de difficulté croissante comme RoR2, enfin à voir. » Découpage et choix de l'agent au plan 51 §16, à juger en jouant :
- **Durée** : pas de chronomètre affiché ; l'anneau (12 min au plus) et la difficulté qui monte avec le temps restent les deux bornes (modèle de RoR2).
- **Victoire** : le troisième gardien vaincu, la run est gagnée (boss battu, difficulté suivante débloquée, phase endgame). Un gardien vaut la Barrière pour les quêtes, le troisième vaut l'Indicible.
- **4e terre** : sans repaire ni passage, le cœur ne tombe jamais ; un écho de gardien se lève toutes les 2 min 30, plus fort à chaque fois, avec un coffre et un point de Péril. La run finit à la mort.
- **Gardien de référence** : charge annoncée, frappe qui ouvre le vide (brèches de 20 s, cœur compris), onde à traverser en dash, trois phases. Corps du Souverain grossi en attendant l'art.

## 187. Optimisation macOS, shaders du monde et paramètres vidéo — 17 octobre 2026

Constat de Raphaël : le jeu tourne à +150 FPS sur PC Windows/Linux, mais tombe à 30–40 FPS sur Mac (M5 Pro 18 Go et M1 Pro 16 Go). Diagnostic : (1) résolution Retina × `canvas_items` (6 à 8 Mpx de fragments), (2) bruits procéduraux recalculés par fragment dans `world_grade.gdshaderinc`, (3) 48 recherches de cellules par fragment aux frontières dans `ground.gdshader`, (4) couche OpenGL dépréciée sur macOS + verrouillage V-Sync et absence de réglages vidéo.

Raphaël : « ouais ok ca me va totalement. je serai d'avis de travailler sur ces quatre leviers !! Et en profiter d'ailleurs pour dans les menus du jeu pouvoir régler la résolution, les fps max, ect... »

**Découpé au [plan 53](53-optimisation-macos-et-parametres-video.md)** en quatre lots :
- **V1** : Paramètres vidéo dans **Paramètres › Graphismes** (Plein écran persisté, Résolution, Échelle de rendu / Retina, V-Sync, FPS max).
- **V2** : Pré-calcul en texture répétable des bruits de `world_grade.gdshaderinc` (nuages, grain, taches).
- **V3** : Pré-calcul CPU des jonctions de biomes dans `GroundMaterial` / `ground.gdshader`.
- **V4** : Moteur de rendu Metal / macOS et validation globale A/B sur Mac.

## 188. Sprites vivants et netteté à 30 px (Plan 54) — 17 octobre 2026

Premier essai du Lot S0 sur **Le Vagabond** et **Le Rôdeur** ([plan 54](54-sprites-vivants-et-animations.md)). Retours de Raphaël sur la planche comparative :
- « c'est un peu mieux apres a voir en jeu.. fait en sorte que je puisse le tester en jeu stp. »
- « aussi je me demande si ils ne pourraient pas etre plus détaillés mais apres j'ai peur que ca dénote avecle reste du jeu. »
- « apres c'est moins net maintenant egalement »

**Diagnostic et règles retenues pour la netteté à ~30 px (`CHARACTER_MODEL_SCALE = 0.53`) :**
- **Primitives symétriques en rotation (`capsule`, `sphere`)** sur les membres orientés : `tapered_box` est aligné sur les axes monde et crée un crénelage en escalier sur des bras/bottes de 2–3 px quand le personnage tourne sur 8 directions.
- **Pas de micro-détails de 1 px sur un torse de 6×6 px** (boucle de ceinture, fente de manteau, côtes isolées) : à cette échelle, la richesse visuelle vient de la **silhouette**, des **poses-clés franches**, du **mouvement secondaire 3D** (écharpe, paquetage, pans du manteau) et de la **réactivité en jeu** (pas synchronisés, inclinaison, *lunge* d'attaque), pas de bruit de surface qui casse les aplats et dénote avec les décors.
- **Pas d'`emissive` hors yeux** (court-circuite `inner_line` et bave sur les matériaux voisins) et **`stretch = 0.0` dans le squelette SDF** pour éviter le scintillement sous-pixel du chapeau et des yeux d'une frame à l'autre.


**Validation du duo pilote (17 octobre 2026, au soir)** : Raphaël a joué le Vagabond et le Rôdeur refaits avec l'inclinaison, l'amorti, l'élan d'attaque et le tempo de marche des créatures (lots S1, G1, G2) : **validé**. **Échelle gardée** (`CHARACTER_MODEL_SCALE = 0.53`, pas de cran de détail en plus) : la richesse passe par la silhouette, les poses et le mouvement secondaire. Suite choisie : **lot S2**, les cinq autres personnages jouables, avant la partie Godot restante (traînées fidèles, télégraphes).

**Lot S2 (17 octobre 2026, au soir)** : sur la planche des cinq autres jouables, Raphaël garde **la Forgeuse qui tient le manche de son marteau** (« Oui, garder »). Pour les retouches Aseprite de la Scaphandrière, qui bloquaient sa régénération sans contenir de retouche faite à la main : « pourquoi ça bloque ? refait les a neuf sil faut » → anciennes retouches retirées, retouches neuves recréées depuis le nouveau rendu.

**Lot S3, choisi en autonomie (17 octobre 2026, au soir)** : Raphaël demande un plan ouvert « sur lequel tu pourras avancer un bon moment (en faire le plus possible sans retour de ma part) », jusqu'à épuiser son quota de la semaine. Proposition retenue : plan 54 S3. Réponses :
- chantier : **S3, le reste du bestiaire** ;
- livraison : **export direct en jeu** par sous-lots, jugement de l'ensemble à la fin (git permet de revenir) ;
- s'il reste du quota : **Zones au sol** (plan 25, suite des §164 et §172) ;
- tour des questions du tableau de bord (§48) : **sauté cette fois**, les lignes du §2 restent telles quelles.


## 189. Revue de direction : gel des nouveautés, tranche verticale, le vide comme arme — 18 octobre 2026

Raphaël demande une revue de mentor, pas d'implémentation : « dégrossir, simplifier, acter ». Constat de départ, ses mots : le jeu « manque de dynamisme et de clarté sur le déroulement d'une partie » ; les terres, affichées une par une, « sont très linéaires, plats et vides » (« la forêt fait très vide, la ville fait hyper répétitive ») ; « l'IA a tendance à valider chaque idée que je lui soumets et à enchaîner les plans […] je m'y perds sur les priorités, le jeu ne fait que changer ». Il doute de la boucle actuelle : « est-ce que ça ne manque pas d'innovation ? ».

**Diagnostic de l'agent** (détail dans [LE-JEU.md](../LE-JEU.md)) :
- la production dépasse la validation : environ 700 commits en quatre semaines, 26 lignes « à juger en jeu » au tableau de bord, 63 cases d'`A-VERIFIER.md` ; l'Élan, dit « mécanique centrale », est éteint en run normale (`enabled_in_runs: false`) ;
- rythme mesuré au bot (profil neuf, joueur moyen, boucle des terres, graines 7 et 42, 7 min) : 3,5 créatures à l'écran en moyenne la première minute (17 à 25 % d'images vides), premier niveau à 23 s et à 3 min 05, 130 et 48 éliminations. Cause : les ralentissements des 13 et 14 octobre (premiers niveaux plus chers, début moins chargé, aimant à 80 px, projectiles −30 %, cadence plus lente) ;
- terres : bruit saupoudré de décors, sans repères ni pièces ; les champs tiennent parce qu'ils ont des pièces (ferme, grange, silo) ;
- le front met 12 min à atteindre le cœur quand le bot boucle une terre en moins de 4 min 30 : la terre qui tombe est un chronomètre, pas une mécanique ;
- plus de quinze notions à comprendre dans la première terre.

**Tranché :**
- **Structure de run : « explorer encore »**, pas de verrouillage des trois terres pour l'instant. Cadrage proposé : exploration bornée sur papier (deux formats au plus, comparés à la boucle actuelle), en parallèle du bac à sable, sans code.
- **Signature : le vide comme arme, avec la jauge d'Élan.** Prototype d'une semaine en bac à sable, avec critère d'arrêt : percussion qui pousse la horde dans le vide (plan 52 E4), saut des trous (E3), esquive parfaite (E2, livrée), jauge à trois paliers gardée. L'agent recommandait les gestes sans jauge ; Raphaël garde la jauge.
- **Gel des nouveautés et tranche verticale de 4 à 6 semaines** : le Vagabond, deux terres travaillées, un vrai gardien, environ 8 armes et 20 objets, coffres et Mémoriaux, départ rapide. Le reste est désactivé dans les données, pas supprimé : Ateliers, Puits, stèles, Failles, micro-événements, Résurgences, Souverains, Barrière, autres personnages, quêtes, classement.
- **Terres : « lieux de Vaulme, biomes mieux générés également »**. Les terres deviennent des lieux du lore (le Bas-Port, la Haute-Ville…) avec 3 à 5 pièces faites main chacune et des chemins procéduraux entre elles (« on se souvient des lieux, pas des chemins », lore §1), **et** la génération gagne de la structure : clairières et fourrés, places, repères visibles de loin, alternance d'espaces ouverts et serrés.

**Proposé, sans réponse explicite** : trois piliers (le vide, menace et arme ; la horde et la puissance ; Vaulme), une page [LE-JEU.md](../LE-JEU.md) qui fait autorité, la méthode de travail (une chose à la fois, rien par défaut sans séance de jeu, test extérieur toutes les deux semaines, l'IA donne deux options au plus et dit ce qu'on retire), le jalon « boucle verrouillée et fun pour cinq testeurs extérieurs » fin novembre.

**Tour des questions du tableau de bord (§48)** : sauté. Les deux premières lignes du §2 (Élan, run en terres) sont traitées ici ; les autres attendent la fin du gel.

## 190. La forme du jeu verrouillée, la Série, la Montée en finale — 18 octobre 2026

Raphaël, après la revue du §189, puis E3 et E4 livrés : « pas sûr pour la mécanique d'élan, j'aurais plus vu un truc de combo, mais genre en vrai c'est dur à faire dans un jeu à tir automatique » ; « honnêtement je suis perdu et découragé, j'évolue dans le brouillard sans certitudes. J'ai besoin de concret et de propositions fortes de ta part pour réduire les doutes et avancer vers une version du jeu jouable… il va falloir prioriser. » Concepts clés qu'il rappelle : de l'innovation qui suscite l'intérêt, des dizaines d'heures avec une progression qu'on sent, frustrant mais très plaisant une fois maîtrisé, de l'aléatoire et du gambling, une histoire forte et un lore présent, un sentiment de puissance.

Il avait écarté la veille l'île qui dérive : « le jeu proposé s'éloigne trop du genre (Megabonk etc.) et semble trop complexe à comprendre, à coder ». L'agent a retiré sa recommandation : il avait fait d'un geste (pousser dans le vide) la structure du jeu. L'île est rangée définitivement.

**Tranché :**
- **La forme : un survivors-like**, trois terres avec un gardien chacune, puis une fin sans fin. Raphaël : « l'idée de combo avec la série, ça me va ».
- **La Série remplace la jauge d'Élan nourrie par la course.** C'est un combo d'éliminations : chaque élimination la nourrit, elle retombe sans élimination, un coup fait perdre un palier, une chute dans le vide compte plus, et ses paliers donnent dégâts, vitesse et Chance. Le saut du vide et la percussion restent.
- **Le vide comme arme reste à tester** : « tomber dans le vide, ça reste à tester. Pourquoi pas, mais je ne l'ai jamais encore testé. »
- **Les trois terres sont tirées au sort** : « soit il faut 2-3 maps possibles par niveau, soit tout mélanger. Et dans tous les cas il y aura un gros chantier pour les rendre plus complètes et suffisantes à elles-mêmes. » Choix par défaut de l'agent : tout mélanger d'abord (les cinq terres actuelles, le danger venant du rang) ; des réserves par rang quand il y aura plus de terres.
- **La Montée et l'Indicible en finale** : « terres tirées […] et montée et indicible à la fin quand même ». Lecture de l'agent : après les trois terres tirées, la Montée est une étape fixe, avec l'Indicible au bout. Le vaincre gagne la run, et la fin sans fin continue de là. Elle remplace la 4e terre actuelle.
- **Les priorités se disent en effort, jamais en durée** : « ça n'a aucun intérêt de parler en termes de durée (6 semaines etc.) car ce sont des métriques totalement biaisées et impossibles à calculer. Plutôt parler en effort. » L'ordre est au [LE-JEU.md](../LE-JEU.md).
- **Aucun point en suspens** : « et pendant qu'on y est, ne plus avoir de points en suspens ». Le §2 du tableau de bord reste vide. Toute question reçoit aussitôt une décision par défaut de l'agent, consignée ici et réversible, ou une place dans l'ordre de [LE-JEU.md](../LE-JEU.md). Triage du jour des 26 lignes qui attendaient au §2 du [tableau de bord](TABLEAU-DE-BORD.md). Défauts notables :
  - la percussion dès le palier 0 ;
  - une chute compte comme une élimination, quêtes comprises ;
  - les bornes `RandiRange(min, max + 1)` corrigées sans changer le jeu ;
  - la sortie commerciale tranchée en une séance après les testeurs, le nom en premier ;
  - `A-VERIFIER.md` n'est plus suivi.
- **Défauts de l'agent au lot S1 (la Série), réversibles** : seules les éliminations du joueur nourrissent la Série (pas les explosions de créatures, les dégâts d'événements ni les parties de boss) ; le compteur « ×N » ne rebondit qu'à chaque dizaine et au palier gagné ; trois paliers au plus.

## 191. Retours sur la Série, boucler de bout en bout — 18 octobre 2026

Raphaël, après avoir joué la Série dans le bac à sable ([plan 52 §15](52-elan.md)) :
- « Il faut vraiment qu'on essaie de boucler les trucs de bout en bout, au lieu de passer d'un sujet à l'autre. »
- Le compteur : « il ne faut pas que ce soit au-dessus de la tête du joueur, mais sur un coin de l'écran, en haut à gauche, en haut à droite ou en haut au milieu, et que ça reste toujours à l'écran », « affiché plus gros », « plus attractif, que ça saute plus aux yeux ».
- « C'était sympa mais ça manquait de retour visuel, ça manquait de retour tout court. » Il n'a pas dépassé 20.
- « Dans l'idée c'est bien, je pense que c'est une très bonne chose. Il faudra juste la peaufiner au fur et à mesure. Même peut-être maintenant. »
- La barre : « si c'était un jeu nommé aux Game Awards […] comment faudrait-il que la mécanique soit ? Pas juste un POC, mais une version vraiment finie, prête à être distribuée sur Steam. »
- « Le truc qui fait que dès qu'on est touché on perd tout, je ne sais pas si c'est bien ou si c'est un peu compliqué. »
- « Il faut que la mécanique soit simple à comprendre, pas trop compliquée à commencer à utiliser, mais que pour la maîtriser il faille être très fort au jeu », un principe qu'il veut « appliquer partout dans le jeu ».
- Le vide : pas de retour.

**Tranché :**
- **Boucler de bout en bout** : une mécanique est finie quand elle est au niveau d'une sortie. Règles lisibles, retours visuels et sonores à chaque changement, place au HUD, réglage mesuré, jouée en vraie run. Pas de sujet suivant avant ([LE-JEU.md](../LE-JEU.md), méthode).
- **Simple à comprendre, dur à maîtriser**, pour chaque mécanique du jeu.
- **La Série est gardée et doit être finie maintenant**, avant tout autre chantier.
- **Ensuite, une revue de la boucle de run** (terres 1 à 3, finale, fin sans fin), aussi appuyée que celle de la Série : « voir si c'est toujours ça qu'on prend ». La forme du §190 peut y changer. Rangée à l'étape 2 du [LE-JEU.md](../LE-JEU.md).

**Proposé par l'agent, en attente d'un mot de Raphaël** (pause demandée avant sa réponse ; détail au [plan 52 §15](52-elan.md)) :
- trois règles affichées : six rangs de ×10 à ×320, un coup qui coupe en deux, une jauge vide qui encaisse la Série en Essence ;
- les leviers de maîtrise, les retours, le HUD en haut au centre ;
- les lots S2 à S5.

**Décision par défaut sur la seule question posée : la Série s'encaisse** quand sa jauge se vide. L'autre choix, « elle perd un rang, sans récompense », reste possible d'un mot.

**Défauts de l'agent**, réversibles :
- le compteur « EN RAFALE » du HUD disparaît au profit de la Série ;
- un coup porté à un gardien ou un mini-boss relance la fenêtre ;
- le vide reste branché sur la Série et se juge avec elle en vraie run.

## 192. La Série finie, lots S2 à S5 — 18 octobre 2026

Raphaël, à la reprise : « on finit la Série de bout en bout (lots S2 à S5 du §15), au niveau d'une sortie Steam : simple à comprendre, dur à maîtriser », « enchaîne S2 à S5 avec un commit par lot, puis allume la Série en vraie run pour que je la joue ». Le code de S2 était déjà arrivé entre-temps (`dd9ffc0d`) ; sa réponse à la question du §191 n'a pas été écrite.

**Tranché par défaut, réversible :**
- **La Série s'encaisse en Essence** quand sa jauge se vide (défaut du §191, déjà codé). « Elle perd un rang, sans récompense » reste possible d'un mot.
- **HUD de la Série** (S3) : bloc en haut au centre, sous le temps ; le nom du biome, l'aide, la barre de boss et le bandeau de quête passent dessous. Visible dès ×2. « EN RAFALE » est retiré.
- **Retours de la Série** (S4) : sons pris parmi ceux du jeu (cloche au rang, verre au coup, chaînes à l'encaissement, tic du compte à rebours) en attendant des sons composés (étape « clarté ») ; contour dès FURIE, images rémanentes à MONSTRE, musique de combat dès CARNAGE ; l'Essence encaissée part du HUD, pas du joueur.
- **La Série et le vide sont allumés en run normale** (S5), pour la séance de Raphaël. Fenêtre du rang 0 portée à 5 s. Meilleure Série au bilan et record par personnage.
- **Le premier rang tarde (après 3 min au bot)** : ce n'est pas un réglage de la Série mais le rythme du départ, étape 3 du [LE-JEU.md](../LE-JEU.md).


## 193. Le rythme du départ : écran plein tôt, premier niveau à la première élimination, aimant à 100 px — 18 octobre 2026

Raphaël : « traite le sujet : Le rythme du départ. fait juste les modifs ne teste pas pour l'instant. » Étape 3 du [LE-JEU.md](../LE-JEU.md), lot R1 du [plan 55](55-rythme-du-depart.md).

**Constat** (§189, plan 52 S5) : 3,5 créatures à l'écran la première minute, premier niveau entre 23 s et 3 min 05, premier rang de la Série après 2 min 36. L'ouverture du §154 (cible de départ 4, rampe de 5 min) se cumulait à la densité du début du §171 (×0,57) : 4 créatures proches à 1 min au lieu de 20.

**Tranché par défaut, réversible :**
- **Écran plein tôt** : l'ouverture part au-dessus de la courbe normale (cible 26, répit 3 s, rampe 3 min, plus d'intervalle doublé), soit environ 15 créatures proches dès le départ. La densité du début (×0,57, §171 et §184) et tout ce qui suit 3 min ne changent pas : la plainte « spammé d'ennemis » portait sur les minutes 1 à 10, où le nombre ne bouge pas.
- **Premier niveau à la première élimination** : facteur des dix premiers niveaux de 0,4 à 1,15 (au lieu de 1,8 à 1,0). 6 XP pour le premier niveau, 149 cumulés pour le niveau 5 (410 avant). La courbe rejoint l'ancienne vers le niveau 9 : la visée du §154, niveau 10 vers 4–5 min, est gardée.
- **Aimant à 100 px** au lieu de 80, toujours sans dérive. Le §172 avait réglé 80 px sans mesure ; à 150 px, on ramassait plus loin que la portée des armes de mêlée.

**Mesuré le 19 octobre** (R2 et R3, [plan 55 §4 et §5](55-rythme-du-depart.md)) : environ 12 créatures visibles la première minute au lieu de 3 ; niveau 10 entre 4 min 43 et 5 min 30 ; ×10 entre 32 et 54 s à la graine 7, au lieu de 126 à 250 s. Le premier niveau au bot (29 à 83 s) ne juge pas la visée, car le bot ne va pas chercher les orbes. **Aucun cran de réglage.** Reste la séance de Raphaël (R4).

## 194. Revue de la boucle de run : le 2e gardien est un mur, une seule horloge, l'arène fermée — 20 octobre 2026

Raphaël, après la livraison du rythme du départ : « ok continue avec ca stp » (étape 2 du [LE-JEU.md](../LE-JEU.md), revue demandée au §191). Détail au [plan 56](56-boucle-de-run.md).

**Constat mesuré** (bot des terres, profil neuf, Vagabond 45 min, Traqueur et Forgeuse 30 min, graines 7 et 42) :
- le combat de gardien dure 2 min 30 à 6 min à la 1re terre, puis 6 à 30 min ou ne finit pas à la 2e, ou contre un gardien enragé. Aucune des huit runs n'est gagnée. Pendant le combat, la foule passe d'environ 60 à plus de 500 créatures, et les Résurgences tombent en plein combat ;
- ce n'est pas une régression : le commit du plan 51 donne le même mur. Ses 110 à 138 s venaient du profil dev ;
- Résurgences, chasses de Souverains, micro-événements, Ateliers, Puits et stèles tournent toujours en run, malgré la réserve du §189 ;
- la Montée n'existe pas dans le code, et l'Indicible n'apparaît pas sur les terres.

**Proposé par l'agent, défauts réversibles** :
- **la forme du §190 est gardée** : trois terres, la Montée, l'Indicible. Les Mémoriaux restent les clés du gardien (option A du plan 56 §5, contre « le gardien quand tu veux ») ;
- **le combat de gardien devient une arène fermée** : l'horloge de difficulté s'arrête, rien d'autre ne se déclenche, la foule est plafonnée. Visée : 1 à 2 min au profil neuf ;
- **une seule horloge, celle de la terre** : les systèmes en réserve sont éteints en run ;
- **la run se lit** : un lieu de Vaulme par terre, un carton d'arrivée, une piste de run sous le chrono ;
- **la Montée est définie** : chemin en lacets bordé de vide, barrière à mi-pente tenue comme un Mémorial, l'Indicible en haut. L'Indicible vaincu, on choisit de redescendre (victoire) ou de rester dans la nuit (fin sans fin au sommet, échos des gardiens). La 4e terre disparaît ;
- **les lots B1 à B3** (arène, une seule horloge, lisibilité) passent avant la séance de Raphaël. B4, la Montée, garde sa place à l'étape 6.
- **Les mesures de durée se font en `MEASURE_PROFILE=normal`.**

## 195. L'arène du gardien : difficulté arrêtée, foule plafonnée, gardien visé en premier — 23 octobre 2026

Raphaël : « Allez go B1 » (lot B1 du [plan 56](56-boucle-de-run.md), étape 2 du [LE-JEU.md](../LE-JEU.md)). Détail et mesures au plan 56 §10.

**Tranché par défaut, réversible :**
- **Arène fermée** : du réveil d'un gardien ou d'un écho jusqu'à sa mort, le temps de difficulté s'arrête. La Résurgence et le micro-événement en cours s'arrêtent, et la foule est plafonnée à 40 créatures. Le chrono affiché, le bilan et les quêtes gardent le temps joué.
- **Les renforts d'une créature (cri du Hurleur) tiennent le plafond de créatures actives**, en run comme en arène. Avant, ils n'en avaient aucun.
- **Le gardien passe devant la foule dans le choix des cibles** (100 px, comme les parties de boss) et naît sans affixe tiré au sort.
- **PV du gardien** : plancher 1 500 (au lieu de 2 500), ×1,35 par terre (au lieu de ×1,8), plafond 1,3 fois le plancher (au lieu de 1,6), enragé ×1,25 (au lieu de ×1,5).
- **Plafond de l'arène à 40 plutôt qu'à 25** : à 25, la médiane n'est pas meilleure, les combats enragés s'allongent, et le combat serait plus calme.

**Mesuré** (profil neuf, trois personnages, graines 7 et 42) : 7 combats sur 10 en 73 à 112 s hors gardien enragé, contre 6 à 30 min avant. Deux runs gagnées vers 19 min, contre aucune avant. Les autres butent sur une terre où le bot ne rejoint pas les Mémoriaux, faute de contourner l'eau et les immeubles. Reste la séance de Raphaël, après B2 et B3.

## 196. Une seule horloge : la réserve éteinte en run — 23 octobre 2026

Raphaël : « go b2 » (lot B2 du [plan 56](56-boucle-de-run.md)). Détail au plan 56 §11.

**Tranché par défaut, réversible :**
- **Interrupteurs de données** : `enabled` dans `crises.json`, `run_events.json` (schedule) et chaque section de `landmarks.json`. Remettre `true` rallume un système.
- **`--reserve`**, outils seulement, rallume tout pour les bancs et régressions.
- **Cinq quêtes en réserve** : Développer, Compter les Résurgences, Dernière émission, Les gestes connus et Une trace de moins. Elles sont cachées et non suivies, et **leurs pièces restent verrouillées** : pas de pièce gratuite qu'une réserve rallumée reprendrait.
- **Quêtes de Souverains ouvertes** : un gardien compte comme Souverain.
- **Failles gardées** en run : un pari, pas une horloge.
- **Bilan** : ligne des Résurgences masquée quand elle vaut 0.

## 197. La run se lit : un lieu par terre, un carton d'arrivée, une piste de run — 24 octobre 2026

Raphaël : « allez go » (lot B3 du [plan 56](56-boucle-de-run.md)). Détail au plan 56 §12 et §13.

**Tranché par défaut, réversible :**
- **Le lieu vient du biome** (`place_key`), selon la table du lore : bois de la colline, fermes du plateau, Haute-Ville, Bas-Port, carrière des Aubiers.
- **En anglais**, Haute-Ville et Bas-Port restent des noms propres. Les trois autres se traduisent : The Hill Woods, The Plateau Farms, The Aubiers Quarry.
- **Le carton** paraît à chaque terre, départ compris, entre la Série et les bandeaux de quête. Le son est celui du réveil d'un Mémorial, joué plus grave : un lieu dont on se souvient.
- **Tant que la Montée n'existe pas** (B4), la terre du late game allume le sommet de la piste, en orange, et son carton dit « Dernière terre ».
- **Sur les terres, le nom du biome ne passe plus sous la Série** : le carton le remplace.

## 198. Le chemin du gardien doit se lire, et chaque terre a son gardien — 25 octobre 2026

Raphaël, après une run : « les boss à la fin de chaque niveau ont été faits ? je ne les ai pas vus ». Le chemin (trois Mémoriaux, le repaire, le gardien) n'était dit que par une ligne de 10 px au-dessus de la minimap. Raphaël : « ça c'est un problème majeur […] il faut vraiment corriger ça », et « il faut que les boss soient des entités à part, pas des versions grossies de mobs existants ». Détail au [plan 57](57-chemin-et-gardiens.md).

**Acté :**
- **le chemin du gardien passe avant la suite** : l'objectif en haut au centre, une flèche d'objectif, des cartons à l'arrivée, à la 3e lumière et au gardien qui se lève seul (lot C1), puis la minimap et l'alerte de rage (C2) ;
- **un gardien par terre, entité à part**, que rien d'autre ne fait naître : son nom, son corps, deux coups de référence et un coup à lui. Les Souverains ne servent plus de gardiens. Ce chantier remonte avant les terres complètes.

**Tranché par défaut, réversible :**
- **les cinq gardiens** : le Grand Cerf (bois de la colline), l'Épouvantail (fermes du plateau), le Tocsin (Haute-Ville), le Pilleur (Bas-Port), le Carrier (carrière des Aubiers), chacun avec son coup (plan 57 §3) ;
- **le Grand Cerf d'abord**, gardien de référence, qui sert à toutes les terres tant que les autres manquent ;
- **corps procéduraux d'abord**, que l'ami artiste reprendra ;
- **la flèche montre les Mémoriaux même dans le brouillard** ; une seule flèche d'objectif, aucune pendant le combat.

## 199. Le gardien devient une créature à part — 25 octobre 2026

Raphaël : « go g0 » (lot G0 du [plan 57](57-chemin-et-gardiens.md)). Détail au plan 57 §7.

**Tranché par défaut, réversible :**
- **Le gardien est une fiche de créature de rang `guardian`**, nommée par le champ `guardian` de chaque biome, refusée dans toute liste de créatures : rien d'autre ne le fait naître. Il naît sans variante ni affixe.
- **Ses coups de référence se choisissent dans sa fiche** (`moves`) ; leurs réglages restent communs (`data/world/guardian.json`). Le Grand Cerf prend la charge et la frappe, sans onde ; son coup à lui vient avec G1.
- **Ses PV sont ceux de sa fiche** (1 950 sur la première terre), × 1,35 par terre franchie, sans le temps, le Péril ni la difficulté. Ce sont les PV que tous les gardiens mesurés atteignaient déjà : le plafond du plan 56 B1 jouait à chaque fois.
- **Un rang lourd**, comme les boss : ni recul, ni percussion, ni gel qui coupe ses coups, ni poussée de la foule. Le Souverain qui servait de gardien les subissait.
- **Il prend la suite du Souverain qu'il était** : son coffre rare s'ajoute à l'épique (`chests`), sa mort compte pour les quêtes de Souverains (§196), lâche un objet et un bonus au sol, et compte au bilan.
- **Les échos du late game reprennent les gardiens vaincus**, chacun son tour.
- **Jusqu'à G1, toutes les terres prennent le Grand Cerf**, avec un corps provisoire : celui du Tréant, à l'échelle 2. Le nom, la barre de boss et les coups sont les siens ; le corps vient avec G1.

## 200. Le Grand Cerf, gardien de référence fini — 27 octobre 2026

Raphaël : « ok go » (lot G1 du [plan 57](57-chemin-et-gardiens.md)). Détail au plan 57 §8.

**Tranché par défaut, réversible :**
- **Le gardien est dessiné à sa taille**, plus jamais agrandi : sa fiche donne le rayon de son corps (`body_radius`).
- **Son corps** : cerf d'écorce sombre au crâne blanc, ramure de bois mort, un chiffon rouge noué à un bois, yeux violets. Procédural, l'ami artiste le reprend.
- **Ses charges en chaîne** viennent à sa deuxième phase : trois charges, chacune annoncée et visée sur le joueur. La dernière montre son disque et y ouvre le vide 12 s. La chaîne passe avant ses autres coups quand elle est prête.
- **Sa mort se voit** : arrêt sur image, corps couché 1,1 s, dissolution lente, et un son de chute propre à chaque gardien (`death_audio`). Les durées de cette mise en scène restent dans le code (présentation, pas équilibrage).
- **Sons de la banque**, sans nouveau fichier : la charge de créature annonce la chaîne, le poing de la Barrière frappe, la défaite de la Barrière fait sa chute. Un son propre au Cerf viendra avec l'audio des gardiens.
- **Le repaire montre le gardien couché**, vu de profil, à la place de la stèle violette.
- **La 3e terre se mesurera après C2** : le bot, invincible, reste bloqué hors du cœur réduit et n'atteint pas le gardien. Le corriger rejoint C2, qui traite la rage.

## 201. Ce qui restait flou sur le chemin du gardien, et l'étape 5 ensuite — 27 octobre 2026

Raphaël : « Ok go 3 et après on fera un gros focus sur le 5 c'est très important ». L'étape 3 (le rythme du départ) n'attend que sa séance : « go 3 » est lu comme la fin de l'étape 3 bis, C2 puis G2 à G5. **Ensuite, l'étape 5, les terres complètes, est le prochain gros chantier.** Détail de C2 au plan 57 §9.

**Tranché par défaut, réversible :**
- **Les Mémoriaux d'une terre se voient sur la minimap dès l'arrivée**, à raviver en pierre pâle, ravivés en or ; les lointains et le repaire s'épinglent au bord du radar. Hors des terres, rien ne change.
- **Le repaire prêt bat** au rythme de la minimap (quatre images par seconde), sans repeinte par image.
- **L'alerte compte la dernière minute** au rythme de l'oubli du moment, en rouge à la place de l'objectif ; un son à son entrée, une fois par terre. Le compte n'est pas lissé : une crise qui accélère l'oubli le fait sauter.
- **L'arène s'ouvre au plus à `fight_crowd_cap` créatures** : au réveil, les plus loin du joueur s'en vont sans butin. Remplace « la foule déjà là au-delà du plafond s'use d'elle-même » (plan 56 B1) : avec l'alerte, le joueur amène sa horde au cœur, et elle protégeait le gardien.
- **Le bot qui reste figé au bord du vide** est le premier sujet du lot G2.


## 202. Visiter les cinq terres avant le chantier des terres — 27 octobre 2026

Raphaël : « le truc c'est qu'il y a déjà bcp de props et d'assets. je te propose avant de faire quoi que ce soit de me créer un outil pour que je puisse tester toutes les maps existantes car pour l'instant je n'ai fait que la zone urbaine ». Avant de découper l'étape 5, il visite ce qui existe.

**Tranché par défaut, réversible :**
- **`tools/play_lands.sh`** lance le profil dev avec les cinq terres dans un ordre fixe : bois, fermes, Haute-Ville, Bas-Port, carrière. La run part du Hub comme d'habitude. `--god` rend invincible, et une liste de biomes choisit l'ordre. L'argument de jeu `--land-order` n'existe qu'en compilation locale.
- **F4 › Terres** refait la terre en cours dans le biome choisi, au même rang (commande `land <biome>`).
- **Une terre refaite au même rang ne durcit pas la run** : le Péril de rang ne s'ajoute qu'en allant plus loin qu'avant. Hors outils de dev, rien ne change : on n'y revient jamais en arrière.
- Le lot G2 (l'Épouvantail) continue en parallèle ; l'étape 5 se découpe après la visite et ses retours.

## 203. Le HUD se tait, la flèche part, raviver tient en haleine — 27 octobre 2026

Retour de Raphaël après la visite des cinq terres (cité au plan 57 §10) : l'objectif permanent prend trop de place, la flèche est moche et inutile, raviver un Mémorial n'a ni suspense ni règle lisible, les cartes sont vides et linéaires.

**Tranché par défaut, réversible :**
- **Lot C3, le HUD se tait** : le nom du lieu remplace « Terre 1 », au carton d'arrivée seulement. Le compteur passe en trois icônes collées à la minimap. Le haut de l'écran ne parle plus qu'aux changements et pendant l'alerte de rage. **La flèche d'objectif est retirée** (elle remplace le choix de C1, §198) : la minimap suffit.
- **Lot C4, raviver tient en haleine** : anneau-horloge et secondes restantes, deux vagues annoncées, cercle qui se resserre au dernier tiers, onde de lumière qui repousse la foule au bout. La durée de 20 s reste, à remesurer.
- **Ordre** : G2 (l'Épouvantail, presque fini) se termine, puis C3 et C4, puis G3 à G5. Les cartes vides et linéaires ouvrent l'étape 5.

## 204. L'Épouvantail, gardien des champs — 27 octobre 2026

Lot G2 du [plan 57](57-chemin-et-gardiens.md), dans l'ordre fixé au §203. Détail au plan 57 §13.

**Tranché par défaut, réversible :**
- **Son corps** : épouvantail sur un pieu, manteau bleu délavé rapiécé de rouge, tête en sac où brûlent deux fentes violettes, faux rouillée et deux corbeaux. Procédural ; l'ami artiste le reprend.
- **Sa mort est un affaissement** le long du pieu : le chapeau tombe et les corbeaux s'envolent. La chute sur le côté sortait du cadre.
- **Le fauchage dès la première phase** : c'est son coup à lui, il doit se voir tôt.
  - Disque de 230 px, brèche de 90° (les trois quarts du tour fauchés, comme prévu au §3) ;
  - la brèche s'ouvre à 120° du joueur, d'un côté puis de l'autre, pour le faire tourner autour du gardien plutôt que reculer ;
  - un dash traverse la faux.
- **Sons de la banque** (chaîne et défaite de la Barrière), en attendant l'audio des gardiens.
- **Les autres terres gardent le Cerf** jusqu'à leur lot (G3 à G5).

## 205. Les terres complètes avant les derniers gardiens — 27 octobre 2026

Raphaël, après G2 : « Ok continue stp d'abord le chantier des terres. Mes précisions sont (mais ça dépend de chaque terre) : au global ça manque de variations / POI, ou props vraiment particulières (ça se remarquait pas trop quand les biomes étaient mixés mais là ça ressort plus) ». Détail au [plan 58](58-terres-completes.md).

**Tranché par défaut, réversible :**
- **L'étape 5 passe avant G3 à G5.** Les gardiens restants prendront ensuite la pièce du cœur de leur terre.
- **Une terre, c'est un cœur, trois à cinq pièces maîtresses et un terrain rythmé entre elles** (forme du §189). Chaque pièce est unique sur sa terre, faite main en JSON, avec des variantes internes, un sol à elle et une place de Mémorial. La pièce du cœur porte le repaire.
- **Les Mémoriaux se posent dans les pièces d'abord** : on ravive le Mémorial du belvédère, pas un Mémorial quelconque.
- **Des décors remarquables garantis sur chaque terre**, comme les cinq grands décors des champs.
- **Ordre des lots** : la grammaire des pièces (T0), puis les bois, le Bas-Port, la Haute-Ville, la carrière, et les champs en dernier.
- **Les éléments de lore et les anciens POI restent éteints** ; une pièce peut reprendre leurs idées en décor.

## 206. La grammaire des pièces — 27 octobre 2026

Lot T0 du [plan 58](58-terres-completes.md), dans l'ordre du §205. Détail au plan 58 §7.

**Tranché par défaut, réversible :**
- **Une pièce est un lieu composé** (`data/world/land_pieces.json`), au format des fermes et des chantiers, avec en plus :
  - une place de Mémorial ;
  - un sol imposé dans l'ellipse de son emprise : une clairière, une place, jamais un rectangle ;
  - des groupes de variantes.
- **Le cœur entoure le repaire**, à sa place d'aujourd'hui, et impose toujours son sol pour que le repaire ne tombe jamais dans l'eau. Le côté ouest, d'où arrive le joueur, reste ouvert.
- **Trois ou quatre pièces par terre** en plus du cœur, à 1 400 px les unes des autres au moins, dans la bande des Mémoriaux. Elles se posent avant tout autre lieu.
- **Les Mémoriaux se logent d'abord dans les pièces**, les autres se posent comme avant : deux sur cinq dans une pièce aujourd'hui.
- **Décors remarquables garantis** sur les bois, le marais et la carrière, un exemplaire chacun, à 18 colonnes les uns des autres. Les champs gardent leurs cinq grands décors ; la Haute-Ville attend T3.
- **La Haute-Ville n'a ni pièce ni remarquable en T0** : sa grille d'immeubles remplit la terre, T3 la refait.
- **Les pièces de démonstration n'emploient que des décors existants** : elles prouvent la grammaire, pas le « vraiment particulier », qui viendra des décors neufs de T1 à T5.

## 207. Le découpage des bois de la colline — 10 novembre 2026

Lot T1 du [plan 58](58-terres-completes.md), dans l'ordre du §205. Détail au plan 58 §8.

**Tranché par défaut, réversible :**
- **Quatre sous-lots** : le terrain rythmé (T1a), la clairière du Cerf et le belvédère (T1b), l'aire de pique-nique et la maison engloutie (T1c), le viaduc effondré et la mesure (T1d).
- **Le terrain rythmé est une couche de données** (`rhythm` dans le fichier de décors du biome), réutilisée par les autres terres ; les fourrés laissent toujours des trouées.
- **Le belvédère borde un petit pan de vide** creusé dans la terre, où pousser la horde. Il part si l'essai du vide (étape 4) retire le vide.

Raphaël, le 10 novembre, sur ce découpage : « Oui, lance T1a ».

## 208. Le terrain rythmé des bois — 10 novembre 2026

Lot T1a du [plan 58](58-terres-completes.md), dans le découpage du §207. Détail au plan 58 §9.

**Tranché par défaut, réversible :**
- **Trois zones aux bois** : clairières (environ un cinquième de la terre), sous-bois, fourrés (un autre cinquième). Réglage dans `data/props/forest_reclaimed.json`.
- **Les clairières restent presque nues** en attendant les pièces : des fleurs, une souche, un bouleau seul.
- **Les fourrés sont plus pleins, pas infranchissables** : densité ×1,8 et des trouées sans décor bloquant. Le nombre de décors de la terre monte de 11 %.

## 209. La clairière du Cerf et le belvédère — 10 novembre 2026

Lot T1b du [plan 58](58-terres-completes.md), dans le découpage du §207. Détail au plan 58 §10.

Raphaël, le 10 novembre, sur ce lot : « Oui, lance T1b ».

**Tranché par défaut, réversible :**
- **Le pan de vide est du Néant du front**, creusé à l'arrivée sur la terre : même rendu (blanc qui grésille), mêmes règles (les créatures y tombent, le joueur s'y brûle et le saute). Pas de second système de vide.
- **Le vide du belvédère est au nord de la pièce**, la rambarde et le Mémorial au sud : on arrive par le raccord sud, on voit le vide devant soi.
- **Garde-fous du lecteur** : le vide fait au moins une cellule d'Effacement, reste dans l'emprise, laisse 240 px au Mémorial, et n'existe jamais au cœur. Aucun chemin du réseau ne le traverse.
- **Les cellules creusées sortent de la terre à perdre** : la part debout reste à 1 à l'arrivée.
- **Les deux pièces de démonstration restent** (vieille route, cercle de pierres) jusqu'à ce que T1c et T1d les remplacent.
- **Le belvédère part avec le vide** si l'essai du vide (étape 4) le retire : il garde alors sa rambarde au bord d'une pente.

## 210. L'aire de pique-nique et la maison engloutie — 10 novembre 2026

Lot T1c du [plan 58](58-terres-completes.md), dans le découpage du §207. Détail au plan 58 §11.

Raphaël, le 10 novembre, sur ce lot : « Oui, lance T1c ».

**Tranché par défaut, réversible :**
- **La maison est un pavillon de plain-pied** tiré de la ferme : il se voit de loin sans masquer ce qu'il y a derrière lui.
- **« Engloutie » veut dire mangée par la forêt**, pas enfoncée dans le sol : lierre, mousse, un frêne contre le pignon, le jardin en sous-bois.
- **La voiture-jardinière reste la voiture envahie** existante, déclinée dans l'autre orientation ; la balançoire n'a pas d'interaction (l'idée de la balançoire fantôme reste en réserve).
- **Les pièces de démonstration des bois partent** : la vieille route et le cercle de pierres. Leurs décors restent au semis.

## 211. Le viaduc effondré et la mesure des pièces vues — 10 novembre 2026

Lot T1d du [plan 58](58-terres-completes.md), dans le découpage du §207. Détail au plan 58 §12.

Raphaël, le 10 novembre, sur ce lot : « Oui, lance T1d ».

**Tranché par défaut, réversible :**
- **Le viaduc est tombé, pas debout** : des piles, des tronçons de tablier au sol, rien sous quoi passer. Un tablier debout demanderait une canopée qui ne se balance pas au vent, et un passage dessous ; c'est en réserve si Raphaël le veut.
- **Les bois posent trois ou quatre pièces parmi quatre**, comme le réglage commun : on ne voit pas tout à chaque run.
- **Une pièce est « vue »** quand le cadre de la caméra recouvre la moitié centrale de son emprise ; le cœur ne compte pas, on y va toujours.

## 212. Le Bas-Port, découpage de T2 — 11 novembre 2026

Lot T2 du [plan 58](58-terres-completes.md), découpage au §13 du plan. Raphaël, le 11 novembre : « Oui, découpe T2 ».

**Proposition de l'agent, réversible :**
- **Quatre sous-lots**, dans cet ordre :
  - T2a, le terrain : chenaux, passerelles, roselières, vasières, prés salés ;
  - T2b, la cale du chantier naval (cœur) et les quais ;
  - T2c, les marais salants et la digue ;
  - T2d, les maisons du port et la mesure.
- **La mer oubliée est le vide** : les quais bordent un pan de Néant, comme le belvédère.
- **Les chenaux ralentissent sans arrêter**, avec des gués et des passerelles là où passent les chemins.
- **Le Cerf reste le gardien provisoire** jusqu'au Pilleur (plan 57 G4).
- **Les pièces de démonstration partent** : le ponton en T2b, l'autel noyé en T2c.
- **L'identifiant `swamp` ne change pas.**

## 213. Le terrain du Bas-Port — 11 novembre 2026

Lot T2a du [plan 58](58-terres-completes.md), dans le découpage du §212. Détail au plan 58 §14.

Raphaël, le 11 novembre, sur ce lot : « Oui, lance T2a ».

**Tranché par défaut, réversible :**
- **`SwampPropPlacer` est retiré** : le Bas-Port se règle comme les bois, par son rythme et ses chenaux en JSON. Les placements en dur ne reviennent pas ; ce qui doit être fait main ira dans les pièces (T2b à T2d).
- **Sous un chemin, un chenal redevient du sol**, et une passerelle s'y pose. Le chemin reste praticable à pleine vitesse ; hors des chemins, l'eau ralentit sans bloquer.
- **Quatre sens de passerelle**, dont deux diagonales, et un sens par traversée.
- **Moins de taches d'eau** au marais (poids 0,10 au lieu de 0,20), pour que les chenaux se lisent.

## 214. La cale du chantier naval et les quais — 11 novembre 2026

Lot T2b du [plan 58](58-terres-completes.md), dans le découpage du §212. Détail au plan 58 §15.

Raphaël, le 11 novembre : « continue d'avancer en autonomie le plus possible ».

**Tranché par défaut, réversible :**
- **Le chalutier est à terre, sur sa cale**, pas à flot : le chantier s'est arrêté en plein carénage. La cale descend vers une eau qu'il n'y a plus.
- **Les quais n'ont pas de garde-corps** : la bordure est basse et ne bloque pas. Le vide se longe à découvert, et la horde peut y tomber.
- **Un pan de vide se cale sur la grille d'Effacement** : l'ancre de la pièce glisse un peu pour que le bord du pan tombe au même endroit à chaque run. Le belvédère des bois en profite aussi.
- **L'île du gardien et le ponton des pêcheurs partent.** Leurs décors restent au semis.

## 215. Les marais salants et la digue rompue — 11 novembre 2026

Lot T2c du [plan 58](58-terres-completes.md), dans le découpage du §212. Détail au plan 58 §16. Fait dans la suite du « continue d'avancer en autonomie le plus possible » du §214.

**Tranché par défaut, réversible :**
- **Une pièce pose de l'eau par bassins rectangulaires** (`water`), pas par ellipses : une saline, un bassin de chasse, une mer derrière une digue ont des bords droits. Les bassins se remplissent une fois toutes les pièces posées, et un chemin qui les traverse reste à sec.
- **La forme d'une saline vient d'un décor plaqué**, l'eau du terrain dessous ne fait que ralentir. Les tuiles d'eau, avec leur bordure de vase, fondent des bassins voisins en une seule mare.
- **La mer est revenue derrière la digue** : une bande d'eau au nord du talus, et une langue d'eau par la brèche. C'est de l'eau, pas du vide : la mer oubliée reste aux quais.
- **L'autel noyé part.** Ses décors restent au semis du marais.

## 216. Les maisons du port, et le Bas-Port fait — 11 novembre 2026

Lot T2d du [plan 58](58-terres-completes.md), dans le découpage du §212. Détail au plan 58 §17. Fait dans la suite du « continue d'avancer en autonomie le plus possible » du §214.

**Tranché par défaut, réversible :**
- **Les maisons du port sont une pièce, pas un semis** : trois maisons de pêcheurs en rangée, des filets et des casiers devant. Le Bas-Port a quatre pièces et en pose trois ou quatre par run, comme les bois.
- **Pas de rue pavée devant les maisons** : la bande de pavés se lisait comme un muret. Le fouillis du port tient lieu de rue.
- **Les décors remarquables du Bas-Port deviennent ceux du port** : le feu de port (neuf), le ponton effondré, la barque submergée, la charrette noyée. L'autel noyé et l'arbre ligaturé restent au semis, sans garantie.

## 217. La Haute-Ville, découpage de T3 — 11 novembre 2026

Lot T3 du [plan 58](58-terres-completes.md), découpage au §18 du plan. Raphaël, le 11 novembre : « continue d'avancer en autonomie le plus possible ».

**Proposition de l'agent, réversible :**
- **Quatre sous-lots**, dans cet ordre :
  - T3a, la grille qui ondule et casse : avenues, ruelles, décrochements, rues effondrées, places, et des pièces qui trouent la grille ;
  - T3b, le parvis de Saint-Aubin (cœur) et la place de la mairie ;
  - T3c, l'école et la poste ;
  - T3d, l'arrêt de la ligne 7 et la mesure.
- **Les rues restent sur les axes de l'écran** : onduler, c'est décrocher, élargir et espacer autrement.
- **Une rue effondrée est un cratère, pas un vide.**
- **Une seule église**, Saint-Aubin.
- **Ni barrière, ni Montée, ni porte verte** dans la Haute-Ville.
- **Le Cerf reste le gardien provisoire** jusqu'au Tocsin (plan 57 G3).
- **L'identifiant `urban_ruins` ne change pas.**

## 218. La grille de la Haute-Ville qui ondule et casse — 11 novembre 2026

Lot T3a du [plan 58](58-terres-completes.md), compte rendu au §19 du plan. Raphaël, le 11 novembre : « continue d'avancer en autonomie le plus possible ».

**Tranché par défaut par l'agent, réversible :**
- **Les avenues sont des rues plantées, pas des chaussées doubles.** `RoadTileGenerator` dessine des chaussées d'une cellule. Deux rangs de route côte à côte y laisseraient des trous entre les bandes. Une avenue a donc des trottoirs plus larges et une rangée de tilleuls de chaque côté.
- **Le réglage de la grille est en JSON** (`city` dans `data/props/urban_ruins.json`). Ses anciennes constantes en dur, ondulation et effondrement à zéro, sont retirées.
- **Une pièce pavée prend le dallage des places.** Sous une pièce sans sol imposé, ou à sol de béton, la grille laisse le dallage des places plutôt que le béton générique, qui faisait une tache sombre.
- **Le square est une pièce de démonstration** : il donne à la ville sa première pièce et son premier Mémorial logé. T3b à T3d posent ensuite les vraies pièces. Le square restera ou partira selon ce qu'elles laisseront de place.
- **Les tilleuls roussis disent la sécheresse** (« sans la mer, il ne pleut plus ») : un sur quatre dans les places et les avenues.

## 219. La Montée avant la carrière et les champs — 12 novembre 2026

Raphaël, le 12 novembre, sur la proposition de l'agent : « ok, passe la Montée avant la carrière et les champs ».

**Acté :**
- **Nouvel ordre** : T3b à T3d (la Haute-Ville), puis la Montée et l'Indicible en finale ([LE-JEU](../LE-JEU.md) étape 6), puis T4 (la carrière), T5 (les champs), puis les gardiens G3 à G5.
- **Pourquoi** : sans la Montée, une run n'a pas la fin que fixe la forme verrouillée, et les testeurs extérieurs ont besoin d'une run qui va de bout en bout. Les champs sont déjà la terre la plus riche : T5 est le lot qui rapporte le moins.

**Tranché par défaut par l'agent, réversible :**
- **La Haute-Ville se finit d'abord** : T3a est livré, et laisser la terre à moitié faite obligerait à y revenir à froid.
- **Les gardiens G3 à G5 gardent leur place**, après T5, chacun avec la pièce du cœur de sa terre. Le Cerf reste le gardien provisoire des terres qui n'ont pas encore le leur. La Montée compte un fragment de lore par gardien, quel qu'il soit.

## 220. Le parvis de Saint-Aubin et la place de la mairie, découpage de T3b — 12 novembre 2026

Lot T3b du [plan 58](58-terres-completes.md), découpage au §20 du plan. Raphaël, le 12 novembre : « vas-y, découpe le parvis et la mairie ».

**Proposition de l'agent, réversible :**
- **Deux sous-lots**, dans cet ordre :
  - T3b1 : Saint-Aubin et son parvis, le cœur de la ville. Les deux églises du semis partent ;
  - T3b2 : la place de la mairie, avec sa fontaine, son kiosque et son panneau de faire-part d'oubli.
- **Le clocher fait environ 350 px**, deux fois et demie un immeuble. Il dépasse la hauteur permise parce qu'il est dans le cœur dégagé.
- **La nef et le clocher sont un seul sprite**, trié à sa base.
- **« Visible de loin » s'arrête à la caméra** : environ 600 px au nord de la base. Le repère de la terre reste la minimap.
- **L'horloge de la mairie est arrêtée sur 23 h 03**, l'heure de l'ordre de fermer la barrière.
- **Rien ne se lit ni ne sonne** : la cloche revient au Tocsin (plan 57 G3).
- **Le square reste** jusqu'à T3d.

## 221. Saint-Aubin et son parvis — 12 novembre 2026

Lot T3b1 du [plan 58](58-terres-completes.md), dans le découpage du §220. Compte rendu au §21 du plan. Raphaël, le 12 novembre : « vas-y, attaque T3b1 ».

**Tranché par défaut par l'agent, réversible :**
- **L'horloge du clocher est arrêtée sur 23 h 10**, l'heure où l'on a empêché Baptiste de sonner le tocsin. Celle de la mairie (T3b2) gardera 23 h 03.
- **L'église a deux variantes, intacte et ruinée**, tirées au hasard à chaque run. La ruinée montre la nef par son toit crevé.
- **Une église est un décor à part, pas un module d'îlot** : `prop_saint_aubin` ne porte pas le préfixe `prop_bld_`, pour que le placeur d'immeubles ne la pose jamais dans une rangée.
- **Les sprites des églises de quartier restent** dans le dossier : plus rien ne les pose, mais leurs uid sont référencés.

## 222. Les rues de la ville trop linéaires — 12 novembre 2026

Raphaël, le 12 novembre, en regardant la Haute-Ville de T3a : « pour la map ville ça serait quand même très bien qu'il y ait plus de variations dans les rues, là c'est très linéaire. Il faut plus de patterns, des diagonales, des parcs, etc. »

**Acté :**
- **Le défaut du §217 « les rues restent sur les axes de l'écran » est levé.** La ville aura des rues en diagonale.
- **Un lot neuf, T3e, « la ville sort de sa grille »**, passe après T3b1 et avant T3b2 : les diagonales, des parcs, des motifs de rues variés.

**Tranché par défaut par l'agent, réversible :**
- **Le lot se découpe au début de son travail**, après l'étude de `RoadTileGenerator` et de `UrbanLayoutGenerator`. La difficulté connue est que les chaussées ne se dessinent que sur une cellule de large et sur les axes. Sur la grille « stacked », la diagonale naturelle est celle des losanges, à 2 pour 1 à l'écran.
- **Contenu visé**, à préciser au découpage :
  - des avenues en diagonale qui traversent la grille ;
  - des parcs, îlots verts avec pelouse, allées, arbres et bassin ;
  - des places en étoile où plusieurs rues convergent ;
  - des îlots coupés en biais par les diagonales.
- **La mairie (T3b2) attend** : sa place se pose mieux dans une ville qui a déjà ses diagonales et ses parcs.

## 223. 15 novembre 2026 — Suite du chantier : finir la Haute-Ville puis la Montée

Raphaël choisit l'ordre proposé pour avancer vers une version distribuable et jouable : **finir la Haute-Ville** (d'abord T3e du §222, puis T3b2, T3c et T3d), enchaîner sur **la Montée et l'Indicible en finale** ([plan 56 lot B4](56-boucle-de-run.md), [LE-JEU.md](../LE-JEU.md) étape 6) pour boucler une vraie run de bout en bout, puis un 3e gardien propre ([plan 57 G3/G4](57-chemin-et-gardiens.md)) et la passe de clarté.

**Acté :**
- **Rien n'est abandonné** : les lots laissés de côté pour l'instant (carrière T4, champs T5, gardiens restants, réserves, conformité Steam) restent au programme pour la suite sans couper sur la qualité.
- **Prévenir Raphaël dès qu'un jalon se prête à un vrai test en main**, puisqu'il n'a pas rejoué depuis la refonte : d'abord dès la Haute-Ville posée (`tools/play_lands.sh urban_ruins`), puis pour une vraie run complète une fois la Montée B4 branchée.



## 224. Les terres font vide : essai de la verticalité ; raviver plus court ; l'objectif en cases à cocher — 15 novembre 2026

Raphaël, après avoir joué les terres d'un seul biome : « comparé à avant où tous les biomes se mélangeaient, ça fait très vide […] je ne pense pas que rajouter des props à l'infini règle le problème. J'ai une solution, et elle est radicale : rajouter de la verticalité au jeu. Comme dans Megabonk, avoir des étages, et donc aussi pouvoir sauter […] c'est à essayer au moins. Et on va se concentrer sur ça. » Puis : « 20 secondes pour raviver un mémorial au début c'est peut-être trop long », et « comme il n'y a pas d'objectif affiché à l'écran, le nouveau joueur sera perdu. Ne serait-ce qu'une case à cocher avec marqué : raviver les 3 mémoriaux pour réveiller le boss. » Détail au [plan 59](59-verticalite.md).

**Acté :**
- **La verticalité devient le chantier en cours**, comme un essai avec critère d'arrêt (plan 59 §6). La forme du 18 octobre est rouverte sur ce seul point. La Montée (étape 6) et la carrière (T4) attendent.

**Proposé par l'agent, en attente du mot de Raphaël sur l'option :**
- **relief en 2D isométrique plutôt que passage en 3D**, d'abord en bac à sable (lots V0 à V2), puis séance de Raphaël ;
- **le dash devient le bond**, sur la même touche ; contact au même étage seulement ; créatures qui grimpent lentement ; falaise qui compte comme un vide (plan 59 §5) ;
- **repli si l'essai ne prend pas** : des terres plus petites et plus denses.

**Tranché par défaut par l'agent, réversible, et fait le jour même :**
- **Raviver un Mémorial prend 12 s au lieu de 20** (`circle.seconds` de `landmarks.json`), sur toutes les terres. Les deux vagues et le resserrement suivent, puisqu'ils sont en parts du temps. Une durée qui monte avec le rang reste possible d'un mot.
- **L'objectif de la terre revient en cases à cocher**, en haut à gauche sous les PV (`LandChecklist`) : « Raviver les Mémoriaux 0/3 », puis « Réveiller le gardien » (« …, au cœur » quand il attend), puis « Vaincre le gardien ». Une case cochée passe en or ; la terre franchie, la liste s'efface. Elle est cachée sur la terre du late game. Cela revient sur le « HUD se tait » du §203 : la ligne du haut au centre reste muette, la liste tient en deux lignes courtes sans fond. Les icônes collées à la minimap restent.

## 225. Étudier les deux voies en bac à sable : relief 2D et 3D — 15 novembre 2026

Raphaël, sur l'option du §224 : « étudier les deux dans un sandbox, relief 2D et 3D ». Détail au [plan 59 §8](59-verticalite.md).

**Tranché par défaut par l'agent, réversible :**
- **Un seul bac à sable, une seule simulation, trois vues** qu'on bascule avec Tab sur la même partie : relief 2D (isométrique orthographique sans lumière, pixel pour pixel), 3D de haut (perspective, lumière, ombres, qu'on tourne), 3D en poursuite (caméra derrière le joueur, à la souris). On compare ainsi le rendu et la caméra, pas deux jeux. La poursuite est ajoutée parce que la 3D de haut ressemblait trop au relief 2D pour juger « comme dans Megabonk ».
- **Le bac à sable est autonome** (`tools/tests/Relief*.cs`, `data/sandbox/relief.json`, `tools/relief_sandbox.sh`) : il n'emploie que les tuiles et sprites du jeu, pas `Player` ni `Enemy`. Rien n'entre dans le jeu avant la séance de Raphaël.
- **Le bond sur Espace**, comme proposé au §224. Les créatures grimpent, longent le vide, et ne se blessent en tombant que poussées.
- **Les touches du bac à sable sont déclarées à son lancement**, pas dans `project.godot`, pour ne pas encombrer la carte d'entrées du jeu.

**Appris en construisant** : la vue relief 2D est rendue par le moteur 3D, ce qui règle le tri et l'occlusion des falaises, le plus gros chantier d'un relief en 2D pure. Si le relief est gardé avec l'aspect 2D, c'est la voie recommandée (plan 59 §8).

**Retour de Raphaël après un premier essai** : « la horde est bcp trop grande. aussi essaye avec les vrais biomes et pas les tuiles moches ».
- **La horde passe de 90 à 35 créatures**, et +/- la change par 10.
- **Le décor est un vrai biome** : le bac à sable génère la vraie run sur la terre demandée (`--land-order`, la forêt par défaut), photographie son sol tel que le jeu le dessine et relève ses décors debout, puis la libère. Le sol devient le dessus des étages, les décors sont replacés et bloquent selon leur emprise. `--tiles` garde les tuiles simples. Le relief reste celui de l'arène.
- **Vu en le faisant** : en 3D, un décor reste une carte plate. Ça passe pour un arbre, pas pour un immeuble. La vraie 3D demanderait des volumes pour les bâtiments ; le relief 2D garde les sprites tels quels.

## 226. Le relief prend, les deux vues plaisent : bâtir sur un monde 3D, la caméra viendra après — 15 novembre 2026

Raphaël, après la séance du bac à sable : « c'est dingue le jeu change totalement de dimension (que ce soit avec la 2D relief ou la 3D). c'est formidable. par contre j'arrive pas à me décider, les deux me plaisent énormément. » Puis : « il y a un bug sur les rebords dans les 2, en gros on reste bloqué (quand on essaie de monter ou de descendre) il faut sauter pour s'extraire », et « les marches c'est bien mais j'aurai peut etre essayé aussi des paliers plus hauts qu'on ne peut pas sauter ».

**Acquis** : le relief est gardé. Les critères 1 et 2 du plan 59 §6 sont pris.

**Tranché par défaut par l'agent, réversible :**
- **Ne pas choisir la vue maintenant.** Les trois vues du bac à sable sont un même monde en 3D sous trois caméras. Le relief du jeu se bâtit sur ce monde 3D, rendu d'abord en relief 2D (pixel art, sprites et décors gardés). La 3D en poursuite reste ouverte : changement de caméra et volumes pour les bâtiments. Le choix se fera quand le relief sera dans une vraie terre, les deux vues sous la main.
- **Rebords** : le joueur tient sur le plus haut sol sous son corps et ne tombe qu'une fois sorti du rebord tout entier. Contrôle `--check-edges` : 105 rebords collants sur 107 avant, 0 sur 113 après.
- **Hauts paliers** : un palier à 2 étages et une tour à 4, chacun par un escalier de 7 marches, que le bond (1,17 étage au plus) ne prend pas. Vérifié par le même contrôle et en capture.

## 227. Le relief entre dans le jeu par un pont de rendu : la logique reste en 2D — 15 novembre 2026

Raphaël, sur « Les trois vues sont un même monde en 3D, filmé par trois caméras » : « parfait meme. je te laisse commit et me suire ensuite sur quoi on avance concretement. »

**Tranché par défaut par l'agent, réversible** ([plan 59 §9](59-verticalite.md)) :
- **La logique reste en 2D**, sur le plan du sol, avec une hauteur par entité. **Seul le rendu passe en 3D**, par un pont qui montre la run. Le jeu fait 92 000 lignes, dont 139 fichiers qui dessinent en 2D : les réécrire d'un coup serait la refonte que le §224 écartait.
- **Ordre** : R0 le pont de rendu (la vraie run, à plat, vue par la caméra 3D, jouable, mesurée et avec l'inventaire de ce qui manque), R1 les étages dans les bois, R2 le portage des effets, R3 le relief dans la génération, la carrière d'abord. Le choix de la caméra se fait quand R1 se joue.
- **Arrêt** : si R0 ne tient pas 60 images par seconde avec 120 créatures, ou si la moitié du jeu est à réécrire, le repli est un relief en 2D pure, sans la poursuite.

## 228. Tout le jeu passe en 3D, la logique comprise — 15 novembre 2026

Raphaël, après les pour et contre du pont de rendu (§227) : « pour moi il faut revoir tout en 3D au lieu de faire un pont meme si ca coute encore plus. et on est d'accord que avec le passage en 3D je serai encore libre de choisir le "type" de 3D comme lors des tests ? »

**Acquis** : le §227 est remplacé. La logique et le rendu passent en 3D, sur un sol carré. Le choix de la caméra reste libre : relief 2D au pixel près, 3D de haut, poursuite.

**Tranché par défaut par l'agent, réversible** ([plan 59 §10](59-verticalite.md)) :
- **La run 3D se bâtit à côté de la run actuelle**, qui reste jouable et testée jusqu'à ce que la 3D l'égale.
- **Un plan à part, le plan 60**, découpe le portage d'après un inventaire du code avant toute ligne.
- **Trois choix de caméra pèsent sur l'amont** : rotation ou non, vue au pixel près ou non, portée de vue. Ils se font avant de produire beaucoup d'art.

## 229. Caméra tournante, low/mid poly à essayer, la caméra validée d'abord — 15 novembre 2026

Raphaël, sur les trois choix de caméra du §228 : « 1. je pense qu'il faut ca , oui. 2. la vue au pixel pres c'est quoi exactement 3. si on refait le jeu pour le passer en 3D je serai chaud pour changer la vue "pixel art" en une vue "low/mid poly". La portée de vue dépend de la caméra je suis d'accord. il faudra assez rapidement valider une caméra »

**Acquis** :
- **La caméra tourne** ; les bâtiments ont des volumes.
- **La vue au pixel près est abandonnée** : elle ne sert qu'au pixel art, que la rotation et les modèles 3D rendent sans objet.
- **La caméra se valide vite**, en premier.

**Tranché par défaut par l'agent, réversible** :
- **Low/mid poly à essayer**, pas encore adopté. La charte graphique reste en vigueur pour la run actuelle jusqu'à l'essai. Piste par défaut pour l'essai : le low poly « crade » (textures basse résolution, palette réduite, brouillard, tramage), qui garde l'esprit de la DA.
- **Premier lot du plan 60 : le bac à sable de caméra et de style**, avant tout portage.
- **Le moteur de rendu sera reconsidéré** dans le plan 60 : GL Compatibility a été choisi pour la 2D, et la 3D éclairée, les ombres et le brouillard le poussent dans ses limites.

## 230. Le plan 60 découpe le passage en 3D — 15 novembre 2026

Raphaël : « Oui, écris-le » (le plan 60). Détail au [plan 60](60-vestiges-en-3d.md).

**Tranché par défaut par l'agent, réversible :**
- **Neuf lots**, de 60.0 (caméra et style) à 60.8 (la bascule). La run 3D devient jouable à plat à la fin de 60.3, et les terres passent en relief en 60.4.
- **Gel des nouveautés sur la run 2D** pendant le portage : seulement des corrections. La Montée, la carrière T4 et les gardiens G3 à G5 se feront directement en 3D.
- **La horde reste sans corps physiques** (grille de voisinage, `TickRoster`) ; l'unité est le mètre, sur un sol carré ; l'interface reste en 2D par-dessus.
- **Les modèles naissent du pipeline de sprites**, qui modélise déjà personnages, créatures et décors en volumes 3D sur un squelette : une pièce low poly par forme, attachée à son os.
- **60.0 se découpe en cinq sous-lots** : les corps en low poly, l'arène, les caméras, les styles, le moteur de rendu ; puis la séance de Raphaël, qui choisit la caméra, le style, le moteur et l'échelle.
- **Arrêt** : si ni la caméra ni le style ne convainquent, ou si la horde ne tient pas 60 images par seconde, on le dit avant 60.1 ; le repli est le relief en 2D pure.

## 231. Les corps en low poly : un maillage par pose — 15 novembre 2026

Raphaël : « Oui, lance 60.0a ». Détail au [plan 60 §8](60-vestiges-en-3d.md).

**Tranché par défaut par l'agent, réversible :**
- **Un maillage par pose**, joué comme une planche de sprites (8 images par seconde), au lieu d'une pièce par forme attachée à son os : une forme du pipeline est une fonction de distance qu'on ne sait pas relire. Toutes les poses existantes servent telles quelles.
- **Ombrage par faces, une couleur par face**, le ton de base de la rampe du matériau ; l'émissif à part.
- **Couleurs de sommet écrites en sRGB** : Godot en GL Compatibility les relit ainsi. À revoir si Forward+ est choisi.
- **Le pas de maillage** : 1,6 unité pour un personnage (le visage reste lisible), 2,0 pour une créature, un quarantième de la taille pour un décor. Les pièces plus fines laissent des éclats, acceptés pour l'essai.

## 232. L'arène low poly : le bac à sable passe en facettes — 15 novembre 2026

Raphaël : « Oui, lance 60.0b ». Détail au [plan 60 §9](60-vestiges-en-3d.md).

**Tranché par défaut par l'agent, réversible :**
- **Le low poly devient le rendu par défaut du bac à sable** ; l'ancien (sprites, sol photographié d'un biome) reste derrière `--sprites` pour comparer.
- **L'échelle est celle des sprites** : 2,65 m par cellule, un étage de 1,86 m, le facteur haut de 0,6 cellule. Elle se règle dans `data/sandbox/relief.json` et se tranche à la fin de 60.0.
- **Le relief** : une couleur par étage, de légers écarts de teinte et de hauteur, des falaises en strates avec une lèvre de la couleur du dessus. La simulation garde son sol plat par cellule.
- **Les décors** sont semés par graine à l'écart du tour du bot, plus une maison posée à la main ; leurs emprises bloquent la simulation.
- **En low poly, la vue « relief 2D » devient une isométrique orthographique éclairée**, premier essai de la caméra isométrique haute de 60.0c.

## 233. Le low poly est trop « low » : essayer plusieurs styles, avec des contours noirs — 15 novembre 2026

Raphaël, après 60.0b : « je suis pas fan du tout des low poly des sols, le reste c'est un peu moins pire », puis « je voudrais qu'il y ait des bordures noires sur les bords », et enfin : « tester plusieurs effets/styles. en vrai le low poly moi j'aime bien mais là c'est trop low ».

**Tranché par défaut par l'agent, réversible :**
- **Les styles (60.0d) passent avant les caméras (60.0c)**, puisque c'est le style qui bloque.
- **Chaque effet se bascule en jeu d'une touche, indépendamment des autres**, pour qu'il compare les combinaisons lui-même : F1 le sol (facettes, lisse, pixel art), F2 les corps (low poly, mid poly), F3 les contours noirs, F4 la pixelisation, F5 l'ombrage (normal ou toon).
- **Le mid poly** : les mêmes volumes maillés deux fois plus fin, ombrage lissé. Ses modèles sont générés à la demande et ne sont pas versionnés (trop lourds pour un essai).
- **Les contours** sont détectés à l'écran sur la profondeur : ils dessinent les silhouettes, les arêtes des falaises et des toits, sur tout ce qui est en 3D.

## 234. Les contours restent, la pixelisation et la 3D de haut partent, la caméra libre en tête — 15 novembre 2026

Raphaël, après avoir joué les styles (§233) :
- « La pixelisation : on oublie définitivement. »
- Contours : « j'aime beaucoup le rendu », à peaufiner sur le joueur et parfois sur les ennemis : « c'est du détail ».
- « La vue 3D de haut sans mouvement de caméra, on peut la tuer définitivement. » Le choix se joue entre le relief 2D (isométrique orthographique) et la 3D à caméra libre (poursuite).
- Il préfère la caméra libre : « beaucoup plus fun et plaisante à jouer (c'est peut-être le fait que je sois un joueur de Megabonk) ». Mais « il manque le contrôle vertical de la caméra ».

**Tranché par défaut par l'agent, réversible :**
- **À la reprise** : retirer la vue 3D de haut et la bascule de pixelisation du bac à sable ; donner à la caméra libre le contrôle vertical (souris haut et bas, entre deux bornes réglées en JSON) ; faire de la caméra libre la vue de départ. Puis peaufiner les contours du joueur et des créatures.
- **Le sol** : « le sol lisse je n'aime pas, ça manque de texture » ; les facettes non plus (§233). À la reprise, regarder comment Megabonk et Minecraft texturent leur sol (texture basse résolution posée sur des blocs, filtrée au plus proche) avant d'en proposer un.
- **Le détail des corps reste ouvert** (low ou mid poly) : on garde sa bascule jusqu'à son choix.


## 235. Caméra libre réglable en hauteur et sol texturé à la Minecraft — 16 novembre 2026

Suite de §234. Raphaël : « le sol lisse je n'aime pas, ça manque de texture […] on regardera comment Megabonk / Minecraft font ».

**Tranché par défaut par l'agent, réversible :**
- **La caméra libre est la vue de départ.**
  - La souris l'incline entre 4° et 72° ; la molette règle le recul, de 3,5 à 14 cellules (`camera_chase`).
  - Elle ne traverse plus le relief : elle s'arrête devant la première falaise qui coupe le rayon du joueur.
  - Monter la souris abaisse la caméra (convention des jeux à la troisième personne), sans option d'inversion pour l'instant.
- **La vue 3D de haut et la pixelisation sont retirées** du bac à sable (§234). Il reste deux vues, caméra libre et relief 2D, et quatre bascules de style (F1 à F4).
- **Le sol, d'après Minecraft et Megabonk** : des textures pixel art basse résolution, lues au plus proche, posées en coordonnées du monde sur la géométrie du sol lisse.
  - 32 px par cellule, la densité des tuiles du jeu.
  - Les motifs sont ceux des sols du jeu.
  - Une frange d'herbe pend au haut des falaises.
  - C'est le style de départ ; les trois autres sols restent sous F1 pour comparer.
- **Les textures sont générées par script** (`tools/generate_relief_textures.py`), comme les sprites, pour rester retouchables et reproductibles. Le relief les lit sans passer par l'import de Godot, pour calculer lui-même leurs mipmaps.
- **À juger par Raphaël** : le sol texturé, puis l'accord entre ce sol et les corps lisses (low ou mid poly, ou corps texturés à leur tour).

## 236. Le mouvement trop rapide, la caméra à distance fixe, un sol plus simple avec de l'herbe en relief — 16 novembre 2026

Raphaël, en jouant le bac à sable après §235 :
- Sol : « il est peut-être un peu trop détaillé ; c'était peut-être mieux qu'il soit un peu plus low poly ». Et, de Megabonk : « les textures du sol sont hyper moches, même celles du reste du jeu, mais ça compense parce qu'il y a plein de détails, par exemple au sol il y a des herbes : du relief rajouté ».
- Caméra libre : « il ne faut pas qu'on puisse régler la profondeur en tant que joueur ; elle doit être fixe ».
- Mouvement, son retour principal : « trop rapide », « saut de puce » ; le saut en direction « va beaucoup trop loin, beaucoup trop haut » ; « on peut hyper facilement faire des sauts en diagonale hyper abusés ».
- « Chaque personnage a sa propre mobilité, ses propres habiletés de déplacement. »
- Ordre de travail : d'abord les mouvements, puis à nouveau les textures.

**Tranché par défaut par l'agent, réversible :**
- **Le rythme 3D** : les vitesses du jeu 2D sont ramenées par un facteur `pace` de 0,67, car des corps à l'échelle trahissent ses 12 m/s. Les créatures suivent le même facteur. La vitesse prend une inertie d'environ un dixième de seconde.
- **Un saut commun à tous** (Espace), à hauteur d'homme (1,64 m, 0,7 s en l'air), qui garde l'élan de la course sans pousser. La portée vient de la course, plus du saut.
- **Les étages passent à 1,33 m** (0,5 cellule au lieu de 0,7). Un saut qui franchit un étage doit dépasser l'étage ; avec un étage plus haut que le facteur, il était forcément plus haut que le corps.
- **Chaque personnage garde sa capacité du jeu** (Maj ou clic droit), tirée de son profil de mobilité : dash, glissade sur patins, déphasage, saut flottant. Le saut reste commun parce que la verticalité en a besoin pour tous.
- **La caméra libre est à distance fixe.** Plus de molette.
- **Le sol** : textures à 16 px par cellule, avec moins de motifs. Des touffes d'herbe en relief sur les cellules d'herbe, invisibles des contours.
- **Mesuré en mètres** par le contrôle headless (plan 60 §12) : c'est la règle pour comparer les réglages de mouvement sans image.

## 237. Des corps plus grands, l'herbe plus rare — 16 novembre 2026

Raphaël, après §236, avec une capture de Megabonk : « je pense que le personnage pourrait (et devrait) être plus grand, et pareil pour les ennemis ; là il est trop petit. Aussi les herbes, il y en a beaucoup trop. Megabonk gère ça mieux. »

**Tranché par défaut par l'agent, réversible :**
- **Les corps passent à ×1,35**, joueur comme créatures, avec leurs emprises et leurs portées de contact. Les décors ne bougent pas. L'échelle commune (`metres_per_cell`) reste à trancher en fin de 60.0.
- **La caméra libre se rapproche** : 4 cellules de recul au lieu de 7,5, 16° d'inclinaison au départ au lieu de 24°.
- **L'herbe pousse en bouquets épars**, avec des plaques nues entre eux : 1 500 touffes au lieu de 16 000, plus larges et moins contrastées.

## 238. Un seul sol, des falaises taillées, des contours lissés ; la crainte d'un air de Megabonk — 16 novembre 2026

Raphaël, sur les sols du bac à sable :
- tuiles du jeu : « tu peux les retirer, elles ne me plaisent pas du tout » ;
- facettes : « juste des carrés coupés en deux, ça n'est pas esthétique du tout » ;
- lisse : « je n'aime pas du tout » ;
- texturé : « mon préféré jusqu'à présent, à voir si on peut pas faire quelque chose de mieux avec des polys ».

Et : « les contours créent trop d'escaliers, on voit les pixels ».

Puis une crainte : « le mode caméra libre, que je trouve beaucoup plus fun que le relief 2D, plus les polys, plus le concept même du jeu font qu'on se rapproche dangereusement d'un Megabonk ; j'ai peur qu'on puisse parler de plagiat ou de copie ».

**Tranché par défaut par l'agent, réversible :**
- **Seul le sol texturé reste**, avec deux variantes de falaises sous F1 : taillées en facettes de roche (au départ) ou droites (plan 60 §14).
- **Les contours se rendent à la définition de l'écran, et le trait est anticrénelé** dans son shader. Le FXAA n'existe pas dans le renderer du jeu.
- **Sur Megabonk**, analyse de l'agent, sans valeur de conseil juridique :
  - Une mécanique ou un genre ne se protège pas, en France comme ailleurs : les idées sont de libre parcours. La caméra à la troisième personne, le low poly aux textures pixelisées et la horde à survivre sont des communs de genre. Megabonk lui-même est un survivors-like en 3D, après Vampire Survivors.
  - Ce qui expose, c'est de reprendre son expression : personnages, noms, interface, présentation des coffres, des autels et des améliorations, palette, ton, musique. Le droit français ajoute le parasitisme : s'approprier pas à pas l'identité d'un concurrent pour profiter de sa notoriété.
  - Le vrai risque est d'image : des joueurs qui écrivent « clone de Megabonk ».
- **Règle de conduite qui en découle :**
  - Megabonk ne sert que de leçon d'ergonomie : échelle des corps à l'écran, densité du détail. Jamais de modèle d'aspect, de ton ou d'interface. Pas de ciel bleu ni d'herbe vive à sa manière.
  - Le sol et le monde vont vers l'identité de Vestiges : crade, éteint, mystérieux, un monde qui s'efface derrière le joueur.
  - Les différences de fond se tiennent au premier plan : l'Effacement qui pousse toujours en avant, sans arène ni minuterie ; la verticalité pour fuir ; les personnages, le bestiaire et le lore du jeu.
  - La caméra libre est gardée.

## 239. Des falaises qu'on ne saute pas, des rampes ; l'identité validée ; la mécanique forte ; convertir les assets — 16 novembre 2026

Raphaël :
- « Il faut que les falaises soient beaucoup plus hautes, genre la plupart, on ne peut pas les sauter. Et donc il faut des cases de montée. »
- « Le sprite du joueur (le facteur) est assez moche », pas prioritaire.
- Il valide la ligne du §238 : « crade, éteint, mystérieux, un monde qui s'efface derrière toi », l'Effacement, la verticalité pour fuir, les personnages, le bestiaire et le lore. « Je me demande s'il ne faut pas une nouvelle mécanique forte et unique, ou si on a déjà tous les ingrédients. »
- « Penser à des systèmes pour convertir les assets dans le nouveau format, de manière automatisée ou hybride. »

**Correction de l'agent sur le §238** : il y écrivait « l'Effacement qui pousse toujours en avant, sans arène ni minuterie ». C'est la Stratégie V2 de mars. La forme verrouillée le 18 octobre (§190, [LE-JEU](../LE-JEU.md)) a trois terres, chacune avec son gardien et son arène, et une horloge de run. Ses deux signatures sont la Série et le vide. L'Effacement y ronge la terre. Ce qui distingue le jeu de Megabonk tient donc à la Série, au vide et à l'Effacement, pas à l'absence d'arène.

**Tranché par défaut par l'agent, réversible :**
- **Des falaises de deux étages et plus**, au-delà du saut, et **des rampes** pour y monter (plan 60 §15). Seuls quelques gradins d'un étage restent sautables. Les plateaux jusqu'à trois étages sont herbus.
- **Pas de nouvelle mécanique : les ingrédients y sont, il faut les nouer.** Le relief 3D fait se rejoindre ce qui était séparé :
  - l'Effacement crée le vide ;
  - le vide est l'arme : pousser la horde du haut des falaises, la chute tue, et elle compte plus dans la Série (§190) ;
  - la hauteur est le refuge : la horde escalade lentement, on tient le plateau et on la repousse en bas ;
  - la Série récompense ce jeu-là.

  En une phrase : « la ville s'effondre dans l'oubli ; tiens les hauteurs, jette la horde dans le vide ».
  - **Avocat du diable** : si, en jouant, on ne pousse pas la horde sans qu'on le dise (critère d'arrêt de LE-JEU), le vide n'est pas la mécanique forte. Il faudra alors en chercher une autre, partie de la mémoire et de l'oubli (§190 : jamais une signature reprise d'un autre jeu).
  - Ce nœud s'essaie dans le bac à sable avant le portage : falaises, poussée et chute y sont déjà.
- **Le facteur** se reprend dans son modèle SDF au détail des corps, après l'arrêt du style (plan 60 §16).
- **La conversion des assets** : presque tout a une source SDF et il ne reste aucune retouche à la main, donc la voie est surtout automatique. Cinq outils sont à écrire, dans l'ordre du plan 60 §16 : export de masse, emprises, poids, planches, sols.

## 240. Toutes les falaises hors de portée du saut ; la conversion des assets commence — 16 novembre 2026

Raphaël : « En fait, je ne ferai pas de falaises qu'on peut grimper en sautant. Je les ferai toutes plus hautes, et même plus que 2 étages : je dirais 3 à 5, à tester. Bref, je veux qu'on avance maintenant, et qu'on commence à préparer la conversion des assets. »

**Tranché par défaut par l'agent, réversible :**
- **Plus aucun gradin sautable** : toutes les falaises de l'arène font 3, 4 ou 5 étages (4 à 6,6 m), et chaque plateau a sa rampe (plan 60 §17). Les plateaux de 3 étages restent herbus ; ceux de 4 et 5 sont en roche nue (seuil à 3,5 étages, inchangé).
- **La chute de ces hauteurs est sévère** : une vingtaine de PV pour 3 étages, près de 50 pour 5 (`safe_drop_levels` 1,2). On ne le corrige pas avant de l'avoir joué : c'est aussi ce qui fait du vide une arme contre la horde (§239).
- **La conversion des assets commence par l'export de masse** (lot C1 du plan 60 §17), sans attendre l'arrêt du style : l'outil ne fige rien, il se relance en deux minutes quand le style ou les modèles changent.
  - **La bibliothèque n'est pas versionnée** : `build/models/`, ignorée de git et de Godot. Plusieurs centaines de Mo de .glb régénérables n'ont rien à faire dans le dépôt ; seuls les modèles que lit le bac à sable restent dans `assets/models/`.
  - **Les variantes orientées sont réduites à la première**, et le tronc et la canopée d'un arbre fondus en un modèle : la caméra tourne, le jeu tourne le modèle. Les variantes écartées sont notées dans l'index, pour les reprendre si l'une diffère vraiment.
  - **Les décors plats** (flaques, rails, marelle, flèches au sol, salines) sortent presque sans volume : ils deviendront des décalques au sol, avec les textures du lot C5, pas des maillages.

## 241. Le poids et la finesse des modèles : découpage du lot C3 — 16 novembre 2026

Raphaël : reprendre la conversion par le lot C3 (poids et finesse), découpage d'abord.

**Tranché par défaut par l'agent, réversible** (plan 60 §18) :
- **Trois sous-lots, dans cet ordre** : le format compact (C3a), les pièces minces (C3b), les budgets de triangles (C3c).
- **Glb de base seulement** : le Godot installé ne déclare pas `KHR_mesh_quantization`. On gagne par le partage des sommets, des couleurs sur 8 bits, des indices sur 16 bits, et la normale de facette calculée dans le shader.
- **Pas de niveaux de détail à l'export** : l'import de Godot les génère (C4).
- **Pas de décimation par effondrement d'arêtes** : un modèle trop lourd reprend un pas plus grossier, ce qui garde le low poly uniforme.
- **Le maillage par pose reste** ; le squelette glTF reste plus tard.
- **Les normales de sommet ne sont pas écrites dans le fichier, elles sont reconstruites au chargement** (lissées), pour le seul décalage des ombres. Les écrire coûterait environ 75 % de plus sur les sommets.

**C3b livré, tranché par défaut par l'agent, réversible** (plan 60 §18) :
- **Pas fin = la moitié du pas du décor**, plancher 4 cm.
- **Le pas des décors reste calé sur leur boîte de cadrage**, qui surestime leur taille réelle d'environ 1,9 en médiane. La corriger rendrait tous les décors plus fins partout : c'est une décision de style, laissée à Raphaël.
- **L'orientation des faces par le gradient du volume s'applique à tous les modèles**, personnages compris.
- **Un affinage local de la grille remplace le pas tiré de l'épaisseur** prévu au découpage, parce qu'une pièce mince n'est souvent pas une pièce SDF à elle seule ; la variante au pas fin calé sur la taille réelle de la matière est écartée (570 000 à 1,66 million de triangles, 10 à 28 Mo, gain non visible sur les planches).

**C3c livré, tranché par défaut par l'agent, réversible** (plan 60 §18) :
- **Budgets dans `tools/sprites/lowpoly_budgets.json`** : personnage 5 000, créature 4 000 (16 000 à partir de 4 m), décor 2 500 (6 000 à partir de 2 m, 10 000 à partir de 5 m), grossissement maximal ×2,5, mid poly à quatre fois le budget, `--step` les ignore.
- **Personnage à 5 000 au lieu de 3 000** : à 3 000, 7 personnages sur 14 auraient grossi, alors que le pas de 1,6 est réglé pour la lisibilité du visage. Garde-fou, aucun ne dépasse (le plus lourd : 4 528).
- **Grand gardien à 16 000 au lieu de 8 000** : à 8 000, le Grand Cerf perdait les andouillers et l'épouvantail son corbeau et le ruban de son chapeau. Un gardien est seul à l'écran.
- **Paliers de décor par taille** au lieu des budgets « décor » et « bâtiment » : un budget unique écrasait les grands décors.
- **Les décors ne se grossissent pas** (`coarsen` faux), leur dépassement est noté dans l'index. Deux essais mesurés l'ont montré : pas fin grossi, 51 modèles grossis et 48,3 → 41,3 Mo mais pièces minces défaites ; pas fin figé, 21 grossis, 29 dépassements, décors −5 %, le bâti se dégrade dès ×1,2. La distance relève des niveaux de détail de C4.
- **Résultat** : 2 modèles grossis (épouvantail ×1,03, brute du vide ×1,15), 48 décors au-dessus de leur budget et signalés, bibliothèque 48,3 → 47,9 Mo. La cible de moins de 40 Mo n'est pas tenue ; le levier est le squelette glTF, toujours laissé à plus tard.
- **Suite au choix de Raphaël** : C2 ou C4.


## 242. L'import des modèles dans Godot : découpage du lot C4 — 17 novembre 2026

Raphaël : continuer par C4 (l'import dans Godot), plutôt que par C2.

**Tranché par défaut par l'agent, réversible** (plan 60 §19) :
- **Trois sous-lots, dans cet ordre** : le convertisseur et les niveaux de détail (C4a), le budget sur les planches (C4b), une terre dans le bac à sable (C4c).
- **Un convertisseur headless plutôt que l'importeur de l'éditeur** : un `.res` par modèle, avec normales lissées et niveaux de détail, sans 312 `.import` non versionnés ni long réimport à chaque export.
- **Pas de `MeshLibrary`** : faite pour le `GridMap`, sans poses. Les décors seront posés en instances groupées, et l'index par terre sert de catalogue.
- **Les niveaux de détail de Godot d'abord, mesurés en Compatibility** ; repli : un second export au pas ×2, montré au-delà d'une distance.

**C4a livré, tranché par défaut par l'agent, réversible** (plan 60 §19, « C4a livré ») :
- **`.scn` plutôt que `.res`** : une PackedScene compressée garde les poses en enfants. Écrit à côté du .glb ; seuls les .glb plus récents repassent.
- **Les triangles de chaque niveau vont dans `lods.json`**, pas dans l'index de l'export : un seul outil écrit chaque fichier.
- **Le repli par `visibility_range` est abandonné** : les niveaux de détail de Godot s'appliquent en Compatibility (mesuré, primitives dessinées par image selon la distance et le seuil d'erreur). Le second export au pas ×2 n'est pas nécessaire.
- **Les `.scn` et `lods.json` de `assets/models` sont ignorés de git** ; `tools/relief_sandbox.sh` les reconvertit au lancement, de façon incrémentale.
- **Le seuil d'erreur des niveaux de détail reste au défaut (1 px)** ; il se réglera à l'œil en C4c.
- Mesures : 312 modèles convertis en 4,4 s, 0 échec ; chargement 0,16 s en `.scn` contre 0,42 s en .glb ; 55,8 Mo de .scn contre 47,9 Mo de .glb. Planches identiques ou à quelques pixels près.

**C4b livré, tranché par défaut par l'agent, réversible** (plan 60 §19, « C4b livré ») :
- **Triangles et budget sous chaque nom, en rouge au-delà du budget**, avec le facteur de grossissement s'il y en a un. Une étiquette sur deux est décalée plus bas pour éviter les chevauchements.

**C4c livré, tranché par défaut par l'agent, réversible** (plan 60 §19, « C4c livré ») :
- **`--biome <terre>`** sème dans le bac à sable les décors de `data/props/<terre>.json`, tirés de la bibliothèque convertie. Densité et poids globaux seulement, sans zones rythmées. Emprise bloquante : la boîte réduite à 60 %, jusqu'à C2.
- **Un nœud par décor plutôt qu'une instance groupée par modèle** : niveaux de détail et élimination hors champ se règlent par nœud.
- **Erreur tolérée des niveaux de détail : 4 px** (`lod_threshold`). En forêt, −45 % de triangles dessinés et environ +5 % d'images par seconde, pour des décors identiques à l'œil.
- **La synchronisation verticale est coupée pendant les captures** du bac à sable, qui mesurent les images par seconde.

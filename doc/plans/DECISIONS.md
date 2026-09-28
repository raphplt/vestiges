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

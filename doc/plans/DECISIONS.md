# VESTIGES — Décisions de Raphaël et arbitrages restants

Version 0.8 · 25 septembre 2026 · Référence de validation du dossier.

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
| Écran d'accueil (26 septembre) | Trop classique ; ne plus afficher les statistiques du personnage (PV, attaque…) mais **montrer son sprite** ; affichage de jeu fini, dans la DA ; « surprends-moi » | 04 : [accueil refait](04-interfaces-et-hub.md#accueil-refait--26-septembre-2026), recette attendue |

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

Source probante : [export original](../audio/lot-a/choix-raphael-2026-09-25.json), identique octet pour octet au fichier transmis ; [synthèse et explication impact/arme](../audio/lot-a/RETOURS.md). Verbatim des notes (chaînes JSON pour conserver aussi les espaces finaux) :

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

[Export original archivé](../audio/lot-a2/choix-raphael-2026-09-26.json) et [verbatim, décisions et préparation](../audio/lot-a2/RETOURS.md). Trois nouveaux choix : `dissolution_a2_b`, `perk_select_a2_a`, `danger_warning_a2_a`. Dissolution : « le B mais ne pas le cropper à un seconde » ; conserver les 1,395828 s de l’original. Impacts ennemi et joueur, montée de niveau : `none`, recherche à reprendre. Révélation du coffre : « garder l'actuel » ; suivi **actuel conservé**, sans transformer le `pending` exporté en sélection de candidat. XP inchangée. Six candidats retenus au total, aucun intégré ni recetté.

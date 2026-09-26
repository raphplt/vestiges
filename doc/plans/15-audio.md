# 15 — Refonte audio : musique, bruitages et sélection

Mis à jour le 26 septembre 2026 · **Choix A/A2 consignés : six candidats retenus, un son actuel conservé ; recherche restante et intégration à faire.**

## 1. Décisions acquises

- Musiques de jeu vidéo avec rythme, mélodie et progression. La distorsion et l'Effacement peuvent colorer ces compositions.
- Aucun budget de prestation ou d'achat supplémentaire. Utiliser les sources gratuites, l'abonnement Google et les crédits ElevenLabs existants.
- Les agents trouvent les candidats et préparent la comparaison ; Raphaël écoute et choisit.
- Plusieurs candidats par effet, couverture exhaustive. Une absence de résultat doit être documentée et suivie.
- Une collaboration bénévole est une possibilité, pas une dépendance du chantier.

Références : [décisions](DECISIONS.md), [Stratégie V2 §21](../VESTIGES-STRATEGIE-V2.md#21-sound-design-v2), [Bible §8](../VESTIGES-BIBLE.md#8-direction-audio), [guide et prompts Google](../AUDIO-GUIDE.md).

## 2. Lot 0 — Découverte et inventaire

**Livré :** [inventaire statique](15-audio-inventaire.md), croisement des fichiers, du registre, des appels C#, des clés JSON et des systèmes V2. Ce relevé n'est pas une écoute et ne certifie pas les droits des fichiers actuels.

79 fichiers (8 musiques, 71 effets/ambiances), 62 clés enregistrées, 17 fichiers non enregistrés. Cinq clés appelées manquent au registre. Le périmètre fonctionnel comprend notamment 24 armes, 16 ennemis individuels et leurs variantes, cinq biomes et cinq micro-événements.

**Modèles existants à lire avant toute intégration :**

| Responsabilité | API / modèle vérifié | Référence |
|---|---|---|
| Jouer un effet | `AudioManager.Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f)` | `scripts/Infrastructure/AudioManager.cs:335` |
| Jouer en interface | `AudioManager.PlayUI(string key, float pitchVariance = 0f, float volumeDb = 0f)` | Même fichier, ligne 364 |
| Musique actuelle | Méthode privée `PlayMusic(string key, float fadeDuration = 2.5f, bool loop = true)` ; fondu A/B | Même fichier, ligne 460 |
| Sons pilotés par données | `windup_audio`, `leap_audio` ; lecture dans `PounceAbility.Configure` | `data/enemies/charognard.json:28`, `scripts/Combat/Abilities/PounceAbility.cs:59` |
| Signaux inter-systèmes | Contrats existants EventBus, abonnements AudioManager | `scripts/Core/EventBus.cs:12`, `scripts/Infrastructure/AudioManager.cs:582` |
| Clic et survol centralisés | Branchement UI existant | `scripts/UI/UITheme.cs:159` |

Ces signatures décrivent l'existant, pas des API nouvelles. Vérifier leur état au moment d'implémenter. Le code actuel ignore certaines clés inconnues ; présence d'un appel ne signifie pas son audible.

**Vérifications :** rapprocher chaque fichier de sa clé, chaque appel de son registre, inspecter les sélecteurs dynamiques et les données. Séparer reliquats V1, usages V2 et besoins proposés. Aucune case de roadmap cochée pour cet audit.

## 3. Lot A — Catalogue des besoins et premier lot de candidats

### Catalogue de couverture

Décliner la matrice de l'inventaire en une ligne par **effet utile au gameplay**, avec un identifiant stable. Chaque arme, ennemi, biome, événement et écran doit renvoyer à un effet partagé, un effet propre ou une décision explicite de rester silencieux. Un fichier actuel peut couvrir plusieurs besoins ; un effet peut nécessiter plusieurs variantes.

Familles : déplacements et joueur ; armes et impacts ; annonces et actions ennemies ; progression et récompenses ; monde et biomes ; Effacement et Résurgences ; micro-événements ; interface, Hub et méta ; musique et ponctuations.

Colonnes du registre à créer : `effect_id`, famille, action/déclencheur, références code/données, son actuel, statut de couverture, intention, durée cible, boucle ou ponctuel, variantes souhaitées, priorité, candidats, choix de Raphaël, état d'intégration et recette.

Statuts de couverture : `à rechercher`, `candidats prêts`, `retenu`, `à retravailler`, `sans résultat`, `silence décidé`, `actuel conservé` (consigne explicite, comptée séparément des candidats choisis), `héritage V1`, `hors périmètre actuel`. Ne jamais transformer un manque de résultat en effet terminé.

### Recherche agentique

1. Rechercher des enregistrements ou packs gratuits dont l'usage commercial est documenté, en commençant par les sons concrets. Les crédits ElevenLabs complètent les manques ; ils ne remplacent pas cette recherche.
2. Viser **trois candidats distincts par effet**. Si une recherche fournit moins de bons candidats documentés, montrer le résultat incomplet et poursuivre par une autre source ou une transformation possible.
3. Relever pour chaque candidat : ID stable, titre, auteur, page d'origine, URL d'écoute/téléchargement, licence exacte et sa preuve datée, attribution, format/durée si vérifiables, variantes du pack, traitement éventuel et adéquation supposée au brief. Distinguer métadonnées vérifiées et appréciation proposée.
4. Télécharger les sources seulement par les moyens autorisés. Un accès nécessitant connexion reste signalé ; ne pas contourner l'accès ni prétendre posséder un fichier dont seule la page est disponible.
5. Présenter une petite page d'écoute locale lorsque les fichiers sont disponibles, sinon des liens directs clairement identifiés. Boutons A/B/C, possibilité « aucun », notes et source pour chaque extrait. Préparer un volume de comparaison cohérent sans écraser les originaux.
6. Raphaël choisit. Conserver les refus et leur raison pour éviter de reproposer les mêmes sons.

Sources techniques : [licences Freesound](https://freesound.org/help/faq/), [API Freesound](https://freesound.org/docs/api/overview.html). Ne pas supposer qu'une clé API ou une connexion est disponible ; la recherche publique peut commencer sans automatiser les téléchargements. Privilégier CC0 et CC-BY ; vérifier les conditions particulières de chaque source et la redistribution d'assets dans le dépôt public, distincte de leur inclusion dans le jeu.

### Premier panier proposé

Dix effets pour fixer le vocabulaire sans submerger l'écoute : impact ennemi, critique, dégât joueur, dissolution, collecte XP, montée de niveau, choix de perk, ouverture de coffre, départ de dash et annonce de danger. Cible : 30 candidats, pas 30 sons déjà trouvés. Les variantes de pas et les armes suivent dans le lot suivant.

**Critère de sortie :** dix fiches avec leurs sources et statut explicite, choix consignés par Raphaël ou recherche encore ouverte. Le catalogue global reste suivi pendant tous les lots ; livrer dix effets n'achève pas la couverture.

**Garde-fous :** aucun son choisi automatiquement, aucun achat, aucun retour de craft/Foyer in-run, aucune licence déduite du seul mot « gratuit ». Ne pas multiplier les sons simultanés pour satisfaire artificiellement l'exhaustivité.

## 3 bis. Lot A2 — Rechercher après le retour du 25 septembre

**Découpage consigné avant modification du catalogue et des outils.** [Export original archivé](../audio/lot-a/choix-raphael-2026-09-25.json) et [retours détaillés](../audio/lot-a/RETOURS.md).

1. Importer les dix décisions et notes à l’identique, sans convertir les `pending` en refus ou en choix. Retenus : `critical_hit_a`, `chest_open_a` (ouverture physique seulement), `dash_start_a`. Aucun remplacement dans le jeu à cette étape.
2. Distinguer `chest_open` (mécanisme d’ouverture) et **`chest_reveal`** (mélodie de révélation sur quelques secondes, nouveau besoin). Les quatre coffres et l’écran de butin référencent les deux besoins. L’ancien fichier reste une référence de révélation, pas un candidat sélectionné.
3. Chercher trois nouveaux candidats par besoin pour **sept effets** : `enemy_hit`, `player_hit`, `dissolution`, `level_up`, `perk_select`, `danger_warning`, `chest_reveal`. Impact ennemi court au contact ; dégâts joueur avec pistes classiques et abstraites ; dissolution plus convaincante ; level-up mélodique ; choix de perk une note avec écho ; nouvelle annonce de danger ; mélodie de coffre après ouverture. XP actuelle appréciée provisoirement : ne pas rechercher ni valider implicitement son remplacement.
4. Conserver intégralement le lot A, ses sources et refus, puis ajouter A2 sous des identifiants distincts. Le registre suit les candidats de plusieurs lots sans invalider les trois choix retenus ; les besoins redemandés restent **à retravailler**, même si leur ancien lot contient trois fichiers.
5. Livrer une page A2 avec références actuelles et retours ; Raphaël choisit. Critères : décisions fidèles à l’export, candidats distincts non refusés, provenance et médias vérifiés, régénération stable, aucun choix orphelin ni son intégré implicitement.

**Déclencheur vérifié :** `Enemy.TakeDamage` (`scripts/Combat/Enemy.cs:989–1001`) joue `sfx_hit_ennemi` ou `sfx_hit_critique` après application d’un dégât. Ce son confirme le **contact sur la cible** ; il ne représente pas le mouvement de l’arme. Les attaques/projections aboutissent à ce point (notamment `Projectile.cs:221` et `Player.cs:2367`). Aucun son d’attaque spécifique aux 24 armes n’est configuré dans `weapons.json` ni déclenché par `PlayerAttackFx` ; les familles par arme du catalogue restent à chercher/intégrer. Ne pas remplacer ce retour d’impact par un simple whoosh de swing.

## 4. Lot B — Recherche par familles jusqu'à couverture complète

Après le premier panier, traiter un lot d'écoute de 8 à 12 effets à la fois : joueur/surfaces, familles d'armes, ennemis par capacités, progression, monde/événements, UI/méta. Partir des listes exactes de l'inventaire, notamment des armes spéciales et des variantes ennemies ; vérifier les nouveaux contenus apparus depuis le relevé.

À chaque livraison, publier les comptes : besoins totaux, couverts par candidat, choisis, restant à chercher, sans résultat, silences décidés, intégrés et recettés. La couverture se compte par besoin, pas par nombre de fichiers téléchargés. Les sons récurrents nécessitent des variantes après le choix de la direction.

**Vérification :** aucune arme, capacité ennemie, surface, interaction, phase ou écran de la matrice sans correspondance. Les contenus seulement proposés dans les plans 13/14 restent séparés des besoins réellement jouables.

## 5. Lot M — Essais musicaux Google

Les six [prompts du guide](../AUDIO-GUIDE.md#3-essais-google--mode-demploi) sont prêts : exploration, combat, Résurgence, après Résurgence, endgame, Hub. Commencer par A et B, deux versions chacun ; Raphaël compare. Développer les autres après retour, puis les ponctuations/mort.

Conserver prompt, outil/modèle, date, formule utilisée, fichier exporté et retour d'écoute. Les droits d'exploitation sont à vérifier pour le produit exact. L'accès dans Gemini ne garantit ni API comprise dans l'abonnement, ni pistes séparées, ni synchronisation de générations indépendantes.

**Vérification :** mélodie identifiable, pulsation appropriée au gameplay, plaisir d'écoute répété, espace pour les effets, distorsion dosée. Ne pas traiter la durée, le tempo ou le raccord demandés dans le prompt comme des propriétés mesurées du résultat.

## 6. Lot C — Préparation et intégration après sélection

Nettoyer et préparer les sons choisis, conserver les originaux et la traçabilité, constituer les banques de variantes. Pour les branches existantes, reprendre les modèles du lot 0. Avant de modifier le gestionnaire, détailler un sous-lot technique couvrant les problèmes constatés : clés absentes, surfaces/biomes, annonces, Endgame et transitions.

Limiter les répétitions, réserver les avertissements, préserver les réglages de volume et la pause. Une éventuelle spatialisation ou musique par couches doit être conçue et mesurée, pas ajoutée implicitement au remplacement des fichiers. Si le coût change, comparer avec le même banc et la même seed sur machine calme.

**Vérification :** build sans warning ; smoke test si initialisation/scènes touchées ; capture de vraie run pour les comportements en run, plus enregistrement audio ou écoute réelle (des captures d'images seules ne valident pas le son). Tester combat dense, événement forcé, retour Hub, pause, mort et transitions. Aucune suppression d'un son ancien sur la seule base de son nom.

## 7. Lot D — Recette finale et roadmap

Reprendre tout le registre et vérifier les choix en jeu, les licences/attributions, les boucles, la répétition, l'équilibre musique/effets, les silences voulus et les signaux utiles. Raphaël arbitre le rendu artistique. Documenter les limitations et les recherches infructueuses.

Cocher les items audio de la roadmap V2 §25 uniquement après implémentation et vérification correspondantes. Le présent plan et les prompts ne les valident pas.

## 8. Collaboration bénévole éventuelle

Chercher une collaboration limitée (un thème et une variation, ou une petite famille d'effets), avec une vidéo de gameplay et un brief clair. Annoncer dès le départ l'absence de rémunération, la destination commerciale envisagée et l'utilisation parallèle d'outils IA. Créditer le contributeur et convenir par écrit du droit d'utiliser/modifier les livrables et de leur présentation en portfolio. Ne pas promettre de revenus ou de parts non décidés.

Point d'entrée vérifié le 25 septembre : [Game Audio Learning Portal — communautés](https://www.gameaudiolearning.com/communities), qui référence notamment [Aftertouch Audio sur Discord](https://discord.com/invite/HydEUYXmEu) et Airwiggles. Vérifier les règles du salon avant de poster : l'existence d'une communauté ne prouve pas qu'elle accepte les annonces bénévoles. Aucun contact envoyé par l'agent.

Texte de départ à adapter :

> Bonjour ! Je développe VESTIGES, un roguelite isométrique jouable dans un monde post-apocalyptique qui s'efface. Je cherche une collaboration bénévole, sans rémunération ni promesse de revenus, pour un petit périmètre : un thème instrumental et une variation de combat, ou une famille de bruitages. Direction : mélodie, rythme, mélancolie et distorsion légère. Le jeu vise une sortie commerciale. J'utilise aussi des outils IA pour des essais musicaux et certains sons. Les contributions seraient créditées, avec un accord explicite sur l'utilisation des fichiers. Je peux fournir une vidéo de gameplay et un brief précis. Si ce format vous intéresse, échangeons d'abord sur le périmètre et vos disponibilités.

## 9. État de livraison

### 25 septembre 2026 — Livraison initiale, avant réception des choix

- Direction, décisions et prompts Google : documentés ; musiques avec rythme et mélodie, distorsion possible. Aucun essai Google généré par l’agent.
- Inventaire statique : livré. La provenance et l’écoute artistique des 79 fichiers historiques restent à auditer ; les vérifications de droits ci-dessous concernent uniquement les nouveaux candidats.
- [Catalogue de couverture](../audio/COUVERTURE.md) et [registre JSON](../audio/catalogue.json) : **112 besoins actuels**, dont **10 avec candidats** et **102 encore à rechercher ou arbitrer**. Seize entrées futures ou historiques sont séparées du dénominateur. Les correspondances des données et systèmes repérés sont suivies ; ce relevé ne constitue pas une validation artistique exhaustive.
- [Page d’écoute du lot A](../audio/lot-a/index.html) : **30 candidats**, trois pour chaque effet du premier panier ; choix A/B/C, refus, silence souhaité, notes et export/import JSON. Les dix sons actuels servent aussi de références, dont le pas sur gravier utilisé au départ du dash : 40 lecteurs au total.
- [Sources et préparation](../audio/lot-a/README-sources.md) : six auteurs, onze packs CC0 vérifiés, 30 originaux distincts conservés avec SHA256 et 30 aperçus préparés par gain constant. Les durées et le décodage ont été contrôlés ; les niveaux perçus et l’adéquation restent à écouter.
- **Aucun effet choisi, intégré ou recetté.** Les choix de Raphaël sont attendus. La page conserve les décisions dans le navigateur quand le stockage le permet ; l’export JSON constitue la sauvegarde à transmettre pour consigner les décisions dans le registre.

Contrôles disponibles : `python3 tools/audio/build_catalogue.py --check` vérifie les correspondances et la synchronisation des sorties ; `python3 tools/audio/build_review.py` reconstruit la page et contrôle les fichiers candidats. La recette navigateur porte sur lecture locale, lecteur unique, sauvegarde, export/import, filtres et affichage mobile ; elle ne remplace pas l’écoute. Les aperçus restent hors des assets chargés par le jeu.

**Suite prévue lors de la livraison initiale :** écoute du lot A, puis familles du lot B et intégration lot C. Les retours reçus ensuite donnent priorité au lot A2 ci-dessus. Aucun item de roadmap coché pour cette livraison documentaire et d’écoute.


### 25 septembre 2026 — Retour reçu et lot A2 engagé

Les dix décisions de l’[export archivé](../audio/lot-a/choix-raphael-2026-09-25.json) sont consignées fidèlement, notes comprises. **Trois candidats retenus** (`critical_hit_a`, `chest_open_a`, `dash_start_a`), **aucun intégré ou recetté**. Les autres décisions demeurent `none`/`pending` telles qu’exportées. Le nouveau besoin `chest_reveal` porte le catalogue à **113 besoins actuels** ; les 16 exclusions restent séparées.

Les six besoins existants redemandés sont marqués **à retravailler**, même à l’arrivée de nouveaux candidats. Les propositions A restent archivées et le registre agrège A et A2 ; il refuse toujours d’effacer un candidat déjà choisi. Le lot A2 et ses sept besoins sont détaillés en §3 bis. L’XP actuelle reste une préférence provisoire, sans sélection artificielle.


**Panier A2 livré :** [page d’écoute](../audio/lot-a2/index.html), [candidats et provenance](../audio/lot-a2/candidates.json). **21 propositions supplémentaires pour sept besoins**, avec les anciens sons en référence et les retours pris en compte. Le catalogue agrège désormais **51 propositions sur 11 besoins**, conserve **3 choix retenus**, **6 besoins à retravailler**, **102 à rechercher**, et deux besoins avec candidats sans choix (`xp_pickup` en attente et `chest_reveal`). Aucun choix artistique n’a été ajouté par l’agent, aucun son n’a été intégré.


### 26 septembre 2026 — Traitement des choix A2

Sous-lot documentaire et préparation, consigné avant modification : archiver l’export original, reporter les sept décisions et leurs notes sans altération, conserver l’historique du lot A, puis préparer la dissolution B sur toute sa durée. Mettre à jour le catalogue et vérifier sa régénération. Aucun remplacement runtime dans ce sous-lot.

Trois nouveaux candidats retenus : `dissolution_a2_b` **sans coupe à une seconde**, `perk_select_a2_a`, `danger_warning_a2_a`. `enemy_hit`, `player_hit` et `level_up` sont refusés et restent à rechercher. Pour `chest_reveal`, « garder l'actuel » fait autorité : conserver le fichier existant ; le champ exporté `pending` reste intact et le suivi distingue **actuel conservé** d’un candidat sélectionné. XP inchangée, toujours en attente avec préférence provisoire pour l’actuel.

Suite : rechercher de nouveaux candidats pour les trois refus, puis poursuivre les familles non couvertes. Les six candidats retenus depuis A restent à intégrer et à écouter en jeu ; aucune case de roadmap cochée.

[Export A2 archivé](../audio/lot-a2/choix-raphael-2026-09-26.json), [synthèse et verbatim](../audio/lot-a2/RETOURS.md), [préparation complète de la dissolution](../audio/lot-a2/preparations/dissolution_a2_b_complete.wav). Les aperçus et sources du panier A2 restent inchangés pour la traçabilité.

## 10. Sons d'attaque des ennemis — 26 septembre 2026

**Retour de Raphaël :** « les attaques des ennemis devraient avoir des sons je crois ».

**Constat :**
- Aucune attaque de base ennemie ne jouait de son.
- Les sons de créatures existants (`assets/audio/sfx/creatures/`), ainsi que `sfx_projectile_vol` et `sfx_projectile_impact`, n'étaient pas enregistrés dans `AudioManager.Paths`. Les sons prévus pour le Présage et le Charognard (`presage.json`, `charognard.json`) échouaient donc en silence : `PlaySfx` ignore toute clé inconnue.

**Branchement livré (plomberie, pas sélection) :**
- Les 12 fichiers sont enregistrés.
- Champ `attack_audio` par créature (`EnemyDataLoader`) : joué quand un coup porte (−7 dB) ou qu'un projectile part à moins de 650 px du joueur (−9 dB). Débit limité à une voix toutes les 90 ms par son.
- Sons **provisoires** attribués à 11 créatures : Ombre, Rampant d'ombre et Charognard → `sfx_ombre_attaque` ; Rôdeur → `sfx_rodeur_attaque` ; Brute et Tréant → `sfx_brute_charge` ; Hurleur → `sfx_hurleur_cri` ; Sentinelle → `sfx_sentinelle_tir` ; Rampant → `sfx_rampant_surgissement` ; Cracheur et Tisseuse → `sfx_projectile_vol`.

Ces choix n'engagent pas la sélection : les besoins d'attaque par famille (`enemy_ranged_shot` et les attaques au contact) restent à rechercher au lot B, et l'écoute en jeu reste à faire. Aucun son de tir n'est non plus joué pour les armes du joueur (§3 bis).

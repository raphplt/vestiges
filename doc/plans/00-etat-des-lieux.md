# Phase 0 — État des lieux, références et écarts

Date : 21 septembre 2026 · Audit statique ciblé du dépôt courant.
Statut : **faits de code/données et hypothèses à tester**, aucune validation gameplay.

## 1. Méthode et limites

Lecture de la V2 et des parties pertinentes du GDD, de la Bible, de la Charte et de l'architecture ; recherches ciblées et extraction de données ; trois audits indépendants sur contrôle/score/rythme, progression/contenu, UI/art/Steam.

Le dépôt contenait déjà une modification de `project.godot`. Elle a été lue dans son état courant et laissée intacte. Les références de ligne ci-dessous sont des repères de cette révision ; rechercher les symboles après évolution des fichiers.

Un build `dotnet build --nologo` a réussi avec zéro warning et zéro erreur pendant la préparation. Aucun playtest visuel, essai de sauvegarde, mesure de performance ou appel Steam n'a été exécuté. Les scripts de simulation historiques ne constituent pas une preuve qu'un runner complet fonctionne aujourd'hui.

Instructions appliquées : AGENTS.md et RTK retrouvé dans `/home/raphael/.claude/RTK.md`. La compétence make-plan a servi à séparer découverte factuelle, propositions, lots et preuves attendues.

## 2. Documents d'autorité lus

| Document | Sections utilisées | Précaution |
|---|---|---|
| [Stratégie V2](../VESTIGES-STRATEGIE-V2.md) | §1–18, 19–21, 25–28 | Gameplay et roadmap prioritaires |
| [GDD](../VESTIGES-GDD.md) | Identités personnages et bestiaire | Exclure jour/nuit/base/craft |
| [Architecture](../VESTIGES-ARCHITECTURE.md) | Couches, responsabilités, flux | Les modules retirés ne sont pas des modèles |
| [Bible](../VESTIGES-BIBLE.md) | Amendement V2, lore, identité, créatures, VFX/audio/UI | Réinterpréter palettes anciennes via Effacement |
| [Charte](../CHARTE-GRAPHIQUE.md) | Grille, palettes, silhouettes, animation, UI | Autorité visuelle ; écart avec résolution du projet |
| [Progression](../PROGRESSION-SYSTEM.md) | Sources de puissance, slots, fusions | Note marquée à valider, conflit avec V2 §13 |

## 3. Inventaire et niveau de preuve

| Élément | Données présentes | Ce que l'audit établit |
|---|---:|---|
| Personnages | 3 | Traqueur, Vagabond, Forgeuse avec accès méta |
| Armes | 24 | Trois sources default, onze world, sept loot_epic, trois loot_legendary |
| Perks | 64 définitions | 74 éléments JSON dont dix chaînes de commentaire ; huit synergies incluses |
| Passifs de run | 13 | Slots et amélioration en code |
| Fusions | 5 | Détection présente, parcours de choix incomplet dans les appels trouvés |
| Malédictions | 3 | Certains modificateurs appliqués, certains bénéfices sans consommateur trouvé |
| Quêtes | 7 run + 5 progression | Tirage, suivi et attribution présents |
| Lore | 19 Souvenirs, 6 constellations | Découverte et organisation présentes ; récompenses legacy |
| Ennemis | 15 fichiers | Onze ordinaires, trois Colosses, un Indicible ; pas quinze rôles |
| Effets | Nombreuses fabriques | Impacts, XP, traînées, dissolution et secousses déjà présents |

**Correction du premier tri :** les quêtes ne déclarent pas directement de récompense arme, mais quatre récompenses Souvenirs débloquent indirectement Bâton d'Essence, Tranchant du Vide, Lanterne Mémorielle et Gantelets d'Écho. Ce chemin doit être préservé ou migré, pas ignoré.

## 4. Écarts établis et plans responsables

| Constat | Repère source | Traitement |
|---|---|---|
| Conversion réelle de commande vers diagonale ; amplitude stick normalisée | Player.cs 633/2757 | 01 |
| Score existant très compact et sans animation | HUD.cs 576/878 | 02/04 |
| Score de survie calculé sans notification à chaque évolution temporelle | ScoreManager.cs 42–60 et émissions | 02 |
| Durée basée sur temps mural, incluant la pause si non corrigée ailleurs | RunTracker.cs 61 | 02/03/06/09 |
| Feedback du corps appliqué au Polygon2D masqué avec sprites | Player.cs 282/2537 | 02/08, vérification visuelle |
| Hitstop neutralisé intentionnellement | ScreenShake.cs 57 | 02, pas de réactivation automatique |
| Intervalle crise compté après sa fin | CrisisManager.cs 103 ; crises.json | 03 |
| Accalmie récompensée/accélération Effacement non branchées dans périmètre trouvé | CrisisEnded et ErasureManager | 03 |
| Level-up armes/passifs en code vs perks dans V2 | FragmentManager 92 ; PerkManager 67 ; V2 §13 | 05, arbitrage explicite |
| Sources/tier/conditions de loot filtrés différemment | Player.cs 1877/1894 ; FragmentManager 192 | 05 |
| Multiplicateurs de rareté ignorés au niveau 1 ; modèle sans effets par rareté | WeaponInstance.cs 69–76 ; WeaponRarityDataLoader.cs 6 | 05 |
| Référence cable_whip absente ; kill_restore_day obsolète | fusions.json et passive_souvenirs.json | 05 |
| ApplyFusion sans appel trouvé ; offre coffre uniquement log | FragmentManager 328/335 | 05 |
| Accès personnages différents de V2 | characters.json ; MetaSaveManager 185 | 06 |
| Attribution quête et complétion sauvegardées séparément | QuestManager 96/410 | 06 |
| Cartes personnages textuelles, navigation souris dominante | HubScreen.cs 779–899 | 04 |
| Résolution charte/projet/HUD non unifiée | Charte §1 ; project.godot 33 ; HUD 43 | 08/04 |
| Police HUD effective non identifiée ; PixelOperator seulement prouvée pour logo généré | generate_hub_title.py 25, recherche code/scènes | 04, diagnostic préalable |
| Weekly fixe, pas de période autonome démontrée | SteamLeaderboards.cs 21/74 | 09 |
| État asynchrone Steam partagé entre opérations successives | SteamLeaderboards.cs 60/134/185 | 09, risque à reproduire |

Présence de mots « nuit », « recipe » ou « Foyer » ne prouve pas qu'un système retiré est encore actif. Distinguer alias de compatibilité, description obsolète, branche active et donnée inutilisée.

## 5. Contrats existants autorisés comme points d'appui

Les symboles suivants ont été trouvés ; leurs détails doivent être relus avant modification. Les nouveaux services/événements proposés dans les plans ne sont pas supposés disponibles.

| Domaine | API / pattern | Source |
|---|---|---|
| Contrôle | Input.GetVector, Velocity, MoveAndSlide dans Player._PhysicsProcess | [Player](../../scripts/Core/Player.cs) |
| Événements | ScoreChanged(int), EnemyKilled(string, Vector2), CrisisEnded(int), LootReceived(string, string, int) | [EventBus](../../scripts/Core/EventBus.cs) |
| Score | CurrentScore, BestScore, SaveEndOfRun(), BuildRunRecord() | [ScoreManager](../../scripts/Score/ScoreManager.cs) |
| VFX | CreateProjectileImpact, CreateXpCollectBurst, CreateLevelUpBurst | [VfxFactory](../../scripts/Combat/VfxFactory.cs) |
| Audio | Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f), pools et throttling | [AudioManager](../../scripts/Infrastructure/AudioManager.cs) |
| Rythme | EnableEndgameTempo(), TimeUntilNextCrisis, CrisisTimeRemaining | [CrisisManager](../../scripts/Events/CrisisManager.cs) |
| Données armes | Get(string), GetAll(), GetDefaultForCharacter(string) | [WeaponDataLoader](../../scripts/Infrastructure/WeaponDataLoader.cs) |
| Build | AddWeapon, AddOrUpgradePassive, SelectFragment, ApplyFusion | [Player](../../scripts/Core/Player.cs), [FragmentManager](../../scripts/Progression/FragmentManager.cs) |
| Quêtes | GetProgressionSnapshots, ResolvePendingProgressionQuests | [QuestManager](../../scripts/Progression/QuestManager.cs) |
| Persistance | UnlockCharacter, CompleteQuest, UpdateStats, version de sauvegarde | [MetaSaveManager](../../scripts/Infrastructure/MetaSaveManager.cs) |
| UI | ApplyButtonStyle, ApplyTabStyle, WireButtonAudio | [UITheme](../../scripts/UI/UITheme.cs) |
| Sprites | LoadOrGet(string charId, string folder), cache SpriteFrames | [CharacterSpriteLoader](../../scripts/Combat/CharacterSpriteLoader.cs) |
| Lore | DiscoverSouvenir(string), SouvenirDiscovered | [SouvenirManager](../../scripts/Meta/SouvenirManager.cs) |
| Spawn | Enemy.Initialize(EnemyData, float, float), EnemyPool | [Enemy](../../scripts/Combat/Enemy.cs), [EnemyPool](../../scripts/Spawn/EnemyPool.cs) |
| Steam | UploadScore, LoadEntries, EntriesLoaded, ScoreUploaded | [SteamLeaderboards](../../scripts/Infrastructure/Steam/SteamLeaderboards.cs) |

## 6. Vérification des futurs lots

Copier les patterns d'entrée, de cache, de signal et de loader réellement présents ; adapter seulement le périmètre approuvé. Ne pas ajouter un singleton global pour chaque effet ni une nouvelle implémentation concurrente d'un système déjà présent.

Valider les références JSON et les effets exécutés, puis les parcours complets. Les tests prioritaires concernent les invariants réels : temps actif, absence de double attribution, disponibilités de loot, contrats de pool, migrations et périodes de classement.

Pour les changements purement visuels, privilégier captures comparables et essais de lisibilité. Un test qui répète les constantes du code ne remplace pas une recette.

Voir le [protocole commun](README.md#6-vérifications-communes). Les détails et sources externes spécifiques figurent dans les plans 05/06/09. Aucune case gameplay de la roadmap n'est cochée à partir de cet audit.

## 7. Complément d’audit de la révision 0.2

Les constats ci-dessus restent l’état historique du code ; ils ne décrivent pas les nouvelles règles cibles. Les décisions actuelles figurent dans [DECISIONS](DECISIONS.md).

- Mobilité : dash reconnu par CharacterSpriteLoader, pas de mécanique/input dash ou saut ni profil de mobilité dans CharacterData ; caméra déjà lissée. Plan 01.
- XP : CalculateXpForLevel en C#, premier seuil 33 XP ; quatre Rampants d’Ombre à 10 XP suffisent arithmétiquement sans autre modificateur. Le ressenti de début trop facile est rapporté par Raphaël, pas mesuré dans cet audit. Plan 03.
- Spawns : maintenance de densité locale toutes les 0,25 s en plus de l’intervalle régulier ; poursuite mêlée directe et pas de collision ennemis-ennemis dans le masque actuel. Le problème de file et la progression des familles restent deux interprétations à clarifier. Plan 07.
- Objets : aucun inventaire indépendant à piles illimitées trouvé ; PerkManager et les passifs ont des plafonds. Déblocages arme/objet explicites et nouveaux types de récompense à créer. Plans 05/06.
- Mort : panneau 360×420 textuel, snapshot limité à une arme finale ; IsNewRecord est consulté après mise à jour du record dans SaveEndOfRun. Plan 02.
- Migration : MetaSaveManager applique une migration legacy aux anciennes versions ; RunHistoryManager peut archiver/réinitialiser un historique ancien. Une évolution V2 doit avoir sa propre migration conservant quêtes, droits et runs. Plans 02/06.
- Terrain : tuiles runtime 64×32, hash de variantes sans seed de run, biome central aléatoire, transitions génériques non identifiées ; berges/routes fournissent des patterns utiles. Aucun validateur complet de connectivité après props trouvé. Plan 10.

Audits en lecture seule ; aucune nouvelle API fictive ni qualité visuelle présumée validée. La révision ajoute des spécifications et ne lance aucun lot de code.

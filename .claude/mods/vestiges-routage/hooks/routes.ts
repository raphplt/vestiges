import type { Table } from '../types'

// Table du routage. effort : low, medium, high, xhigh, max. model : alias (sonnet, opus, haiku)
// appliqué au premier tour de la session seulement ; absent = le modèle de la session.
// Les mots-clés sont des expressions régulières insensibles à la casse ; la catégorie qui en
// reconnaît le plus l'emporte (à égalité, la première) ; sans aucune, un petit modèle classe la
// demande d'après les descriptions, sauf une réponse courte qui garde la catégorie précédente.
export const ROUTES: Table = {
  "shortPromptChars": 30,
  "categories": [
    {
      "id": "perf",
      "effort": "xhigh",
      "description": "performance, FPS, banc, optimisation, régression de coût, profilage",
      "keywords": [
        "\\bperf",
        "\\bfps\\b",
        "\\bbanc\\b",
        "\\bbench",
        "optimis",
        "profil(eur|age)",
        "alloc",
        "r[ée]gression",
        "lent(e|eur)?\\b",
        "\\bp99\\b"
      ]
    },
    {
      "id": "csharp",
      "effort": "high",
      "description": "code C# de gameplay ou de systèmes Godot, scènes, shaders, bug en jeu",
      "keywords": [
        "\\.cs\\b",
        "c#",
        "\\bgodot\\b",
        "\\bsc[eè]ne",
        "shader",
        "\\bbug\\b",
        "plantage",
        "crash",
        "exception",
        "\\bnode2d\\b",
        "gameplay",
        "\\bennemi",
        "\\barme",
        "\\bperk",
        "\\bhud\\b",
        "\\bimpl[ée]ment"
      ]
    },
    {
      "id": "python",
      "effort": "high",
      "description": "scripts Python de génération de sprites, de modèles 3D low poly, de textures ou d'audio",
      "keywords": [
        "\\.py\\b",
        "python",
        "sprite",
        "g[ée]n[ée]rateur",
        "export_lowpoly",
        "low ?poly",
        "\\.glb\\b",
        "mod[eè]les? 3d",
        "texture",
        "\\bsdf\\b",
        "pixel art"
      ]
    },
    {
      "id": "json",
      "effort": "low",
      "model": "sonnet",
      "description": "réglage de valeurs dans les JSON de data/ (stats, dégâts, vitesses, tables)",
      "keywords": [
        "\\.json\\b",
        "\\bdata/",
        "r[ée]glage",
        "\\br[ée]gle(r)?\\b",
        "\\bvaleur",
        "\\bd[ée]g[aâ]ts\\b",
        "\\bvitesse\\b",
        "\\bpoids\\b",
        "[ée]quilibr",
        "\\bbaisse(r)?\\b",
        "\\bmonte(r)?\\b"
      ]
    },
    {
      "id": "docs",
      "effort": "medium",
      "model": "sonnet",
      "description": "rédaction ou mise à jour de documentation, plans de lots, DECISIONS.md, GDD, roadmap",
      "keywords": [
        "\\bdoc(s|umentation)?\\b",
        "\\bplan\\b",
        "decisions\\.md",
        "\\bgdd\\b",
        "roadmap",
        "r[ée]dige",
        "r[ée]daction",
        "\\bbible\\b",
        "\\bcharte\\b",
        "\\.md\\b"
      ]
    },
    {
      "id": "question",
      "effort": "medium",
      "description": "question, explication ou lecture sans modification de fichier",
      "keywords": [
        "^(pourquoi|comment|qu'est-ce|est-ce que|c'est quoi|explique)",
        "\\?\\s*$"
      ]
    }
  ],
  "commands": {
    "bench": "perf",
    "close-lot": "docs",
    "roadmap-sync": "docs",
    "smoke-test": "question",
    "capture": "csharp"
  }
}

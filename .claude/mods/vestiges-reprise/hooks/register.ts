import type { EngineInterface, Register } from 'claude-code'

const STATUS_LINES = 25
const COMMITS = 12
const README_CHARS = 1500

// La reprise de session de CLAUDE.md (git status, git log, dernière mise à jour des plans) faite
// d'office : trois appels d'outil de moins à chaque ouverture, et l'avertissement sur les
// modifications de Raphaël toujours présent.
export const register: Register = on => {
  on('classic.SessionStart', async ($, e, next) => {
    const result = await next(e)
    // Une reprise garde son contexte : l'état y est déjà, et l'ajouter tromperait sur sa date.
    if (e.source === 'resume') return result
    const context = await resumeContext($).catch(() => undefined)
    if (context === undefined) return result

    return { ...result, additionalContext: [...(result.additionalContext ?? []), context] }
  })
}

async function resumeContext($: EngineInterface): Promise<string | undefined> {
  const [branch, status, log] = await Promise.all([
    $.process.run(['git', 'branch', '--show-current']),
    $.process.run(['git', 'status', '--short']),
    $.process.run(['git', 'log', '--oneline', `-${COMMITS}`]),
  ])
  if (log.exitCode !== 0) return undefined

  const changes = status.stdout.split('\n').filter(l => l.length > 0)
  const shown = changes.slice(0, STATUS_LINES).join('\n')
  const more = changes.length > STATUS_LINES ? `\n… et ${changes.length - STATUS_LINES} autres` : ''
  const readme = await $.fs.read('doc/plans/README.md').catch(() => '')

  return [
    `Reprise de session (mod vestiges-reprise), branche ${branch.stdout.trim() || '?'} :`,
    changes.length === 0
      ? 'Arbre propre.'
      : `${changes.length} fichier(s) modifié(s). Ceux que Raphaël a touchés pendant ses tests, ou qu'une session parallèle travaille, sont volontaires : ne pas les annuler, ne committer que ses propres fichiers.\n${shown}${more}`,
    `Derniers commits :\n${log.stdout.trim()}`,
    lastUpdate(readme),
    'Reste à lire selon la tâche (CLAUDE.md, reprise de session) : doc/LE-JEU.md et doc/plans/DECISIONS.md.',
  ].filter(s => s.length > 0).join('\n\n')
}

// Le premier paragraphe daté en gras du dossier de plans : la dernière mise à jour.
function lastUpdate(readme: string): string {
  const start = readme.search(/^\*\*\d/m)
  if (start < 0) return ''
  const rest = readme.slice(start)
  const end = rest.slice(2).search(/\n\*\*\d/)
  const block = (end < 0 ? rest : rest.slice(0, end + 2)).trim()

  return `Dernière mise à jour du dossier de plans (doc/plans/README.md) :\n${block.length > README_CHARS ? `${block.slice(0, README_CHARS)}…` : block}`
}

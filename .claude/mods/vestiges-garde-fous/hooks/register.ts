import type { EngineInterface, Register } from 'claude-code'

// Seuils repris de tools/bench_ab.sh : un banc de FPS n'a de sens que sous un
// quart des cœurs de charge ; sous 3 Gio disponibles, une tâche lourde risque
// d'être tuée par le noyau (et d'emporter les builds de Raphaël avec elle).
const LOAD_FRACTION = 0.25
const MIN_AVAILABLE_GIB = 3
// Un ouvrier de l'export de masse a été tué par earlyoom à 4,6 puis 5,6 Gio de RSS.
const EXPORT_JOB_GIB = 5

// Préfixe qui lève les garde-fous de charge et de mémoire pour une commande,
// quand Raphaël décide de mesurer quand même.
const OVERRIDE = 'GARDE_FOUS=off'

const FPS_SENSITIVE =
  /\b(benchmark_movement|benchmark_projectile_cadence|bench_ab|profile_crowd\w*|audit_performance\w*)\.sh\b/
const MEMORY_HEAVY =
  /\b(benchmark_\w+|bench_ab|measure_\w+|validate|capture_\w+|record_\w+|model_sheet|profile_crowd\w*|audit_performance\w*)\.sh\b|export_lowpoly\.py\b[^\n;&|]*--(all|catalog|jobs)\b/

type Machine = { load1: number; cpus: number; availableGib: number }

export const register: Register = on => {
  on('tool.call', { tool: 'Bash' }, async ($, e, next) => {
    const command = e.command
    const refusal = refuseShell(command) ?? (await refuseCommit($, command)) ?? (await refuseUnderPressure($, command))

    return refusal === undefined ? next(e) : { deny: `${$.plugin.name} : ${refusal}` }
  })
}

function refuseShell(command: string): string | undefined {
  for (const segment of segments(command)) {
    const words = tokenize(segment)
    const start = words[0] === 'rtk' || words[0] === 'sudo' ? 1 : 0
    const tool = words[start]

    if (tool === 'pkill' || tool === 'pgrep') {
      const refusal = refuseSelfKill(tool, words.slice(start + 1), command)
      if (refusal !== undefined) return refusal
    }

    if (tool === 'git') {
      const refusal = refuseGit(gitArgs(words.slice(start + 1)))
      if (refusal !== undefined) return refusal
    }
  }

  return undefined
}

// Le shell qui exécute la commande porte tout son texte dans sa ligne de
// commande : `pkill -f motif` le trouve, le tue, et l'appel finit en code 144.
function refuseSelfKill(tool: string, args: string[], command: string): string | undefined {
  const isFull = args.some(a => a === '--full' || /^-[a-zA-Z]*f[a-zA-Z]*$/.test(a))
  if (!isFull) return undefined

  const pattern = firstOperand(args)
  if (pattern === undefined || pattern.includes('[')) return undefined

  const kills = tool === 'pkill' || /\bkill\b|\bxargs\b/.test(command.replace(/\bpkill\b/g, ''))
  if (!kills) return undefined

  const bracketed = pattern.replace(/[A-Za-z0-9]/, c => `[${c}]`)

  return (
    `\`${tool} -f ${pattern}\` trouverait aussi le shell qui l'exécute (son texte contient le motif) ` +
    `et le tuerait (code 144). Écrire le motif avec une classe de caractères, qui ne se reconnaît pas ` +
    `elle-même : \`${tool} -f '${bracketed}'\`.`
  )
}

function refuseGit(args: string[]): string | undefined {
  const [verb, ...rest] = args
  const parallel = "des sessions parallèles travaillent sur l'arbre principal (CLAUDE.md, mémoire « sessions parallèles »)"

  if (verb === 'add') {
    const wide = rest.find(a => ['-A', '--all', '.', '-u', '--update', ':/', '*', '--no-ignore-removal'].includes(a))
    if (wide !== undefined) {
      return `\`git add ${wide}\` ramasserait le travail des autres : ${parallel}. Ajouter ses propres fichiers par leur nom (\`git add -p\` pour ne prendre que ses lignes).`
    }
  }

  if (verb === 'commit' && rest.some(a => a === '--all' || /^-[a-zA-Z]*a[a-zA-Z]*$/.test(a))) {
    return `\`git commit -a\` committerait les modifications des autres : ${parallel}. Indexer ses fichiers par leur nom, puis \`git commit\`.`
  }

  if (verb === 'stash' && !['list', 'show'].includes(rest[0] ?? '')) {
    return `\`git stash\` retirerait de l'arbre le travail en cours des autres sessions et de Raphaël. Ne pas remiser : committer ses propres fichiers ou demander.`
  }

  if (verb === 'reset' && rest.includes('--hard')) {
    return `\`git reset --hard\` effacerait des modifications qui ne sont pas les siennes (sessions parallèles, tests de Raphaël). Demander à Raphaël.`
  }

  if ((verb === 'checkout' || verb === 'restore') && rest.some(a => a === '.' || a === ':/')) {
    return `\`git ${verb} .\` annulerait toutes les modifications de l'arbre, y compris celles des autres. Restaurer ses fichiers par leur nom.`
  }

  if (verb === 'clean' && rest.some(a => /^-[a-zA-Z]*f/.test(a) || a === '--force')) {
    return `\`git clean -f\` supprimerait des fichiers non suivis qui peuvent appartenir à d'autres sessions (PNG neufs, .import). Supprimer ses fichiers par leur nom.`
  }

  return undefined
}

// Ce que le dépôt exige d'un commit et que seule la mémoire de l'agent gardait : pas de trailer
// Co-Authored-By (règle de Raphaël, que le harnais contredit à chaque session), chaque .cs avec son
// .cs.uid, chaque PNG neuf d'assets/ avec son .import, rien de .godot/.
async function refuseCommit($: EngineInterface, command: string): Promise<string | undefined> {
  const git = commitPrefix(command)
  if (git === undefined) return undefined

  if (/co-authored-by/i.test(command)) {
    return "le message porte un trailer `Co-Authored-By` : les commits de Vestiges n'en ont jamais (AGENTS.md, règle de Raphaël). Le retirer."
  }

  const staged = await $.process.run(['git', ...git, 'diff', '--cached', '--name-status', '--no-renames'])
  if (staged.exitCode !== 0) return undefined
  const added: string[] = []
  const deleted: string[] = []
  const all: string[] = []
  for (const line of staged.stdout.split('\n')) {
    const [status = '', path = ''] = line.split('\t')
    if (path.length === 0) continue
    all.push(path)
    if (status === 'A') added.push(path)
    if (status === 'D') deleted.push(path)
  }

  const godot = all.filter(p => p.startsWith('.godot/'))
  if (godot.length > 0) return `fichiers de \`.godot/\` indexés (${list(godot)}) : ils ne se versionnent jamais. \`git restore --staged\` sur eux.`

  const needed = [
    ...added.filter(p => p.endsWith('.cs')).map(p => `${p}.uid`),
    ...added.filter(p => /^assets\/.*\.png$/i.test(p)).map(p => `${p}.import`),
  ]
  const orphans = deleted.filter(p => p.endsWith('.cs')).map(p => `${p}.uid`)
  if (needed.length === 0 && orphans.length === 0) return undefined

  const index = await $.process.run(['git', ...git, 'ls-files', '--cached', '--', ...needed, ...orphans])
  const present = new Set(index.stdout.split('\n').filter(l => l.length > 0))
  const missing = needed.filter(p => !present.has(p))
  const left = orphans.filter(p => present.has(p))

  if (missing.length > 0) {
    return (
      `compagnons absents de l'index : ${list(missing)}. Un .cs se committe avec son .cs.uid, un PNG neuf avec son .import ` +
      `(CLAUDE.md, pièges). \`tools/smoke_test.sh\` les fait créer par l'import Godot ; les indexer par leur nom.`
    )
  }
  if (left.length > 0) {
    return `.cs supprimé mais son .cs.uid reste versionné : ${list(left)}. \`git rm\` le .cs.uid avec lui.`
  }

  return undefined
}

function commitPrefix(command: string): string[] | undefined {
  for (const segment of segments(command)) {
    const words = tokenize(segment)
    const start = words[0] === 'rtk' ? 1 : 0
    if (words[start] !== 'git') continue
    const args = words.slice(start + 1)
    if (gitArgs(args)[0] !== 'commit') continue
    // `-C` avant le verbe change de dépôt ; après `commit`, il désigne un commit dont reprendre le message.
    const globals = args.slice(0, args.length - gitArgs(args).length)
    const c = globals.indexOf('-C')

    return c >= 0 && globals[c + 1] !== undefined ? ['-C', globals[c + 1] ?? '.'] : []
  }

  return undefined
}

function list(paths: string[]): string {
  return paths.length <= 4 ? paths.join(', ') : `${paths.slice(0, 4).join(', ')} et ${paths.length - 4} autres`
}

async function refuseUnderPressure($: EngineInterface, command: string): Promise<string | undefined> {
  if (command.includes(OVERRIDE)) return undefined

  const isFpsSensitive = FPS_SENSITIVE.test(command)
  const isHeavy = MEMORY_HEAVY.test(command)
  if (!isFpsSensitive && !isHeavy) return undefined

  const machine = await readMachine($)
  const limit = Math.max(1, Math.floor(machine.cpus * LOAD_FRACTION))

  if (isFpsSensitive && machine.load1 > limit) {
    return (
      `charge ${machine.load1.toFixed(2)} pour ${machine.cpus} cœurs (limite ${limit}) : une mesure de FPS ne vaudrait rien. ` +
      `Voir \`ps -eo pcpu,comm --sort=-pcpu | head\`, attendre que la machine se calme, ou s'appuyer sur des métriques ` +
      `insensibles à la charge (nœuds créés/s, allocations). Pour passer outre sur décision de Raphaël : préfixer la commande par \`${OVERRIDE}\`.`
    )
  }

  const jobs = Number(/export_lowpoly\.py\b[^\n;&|]*--jobs[=\s]+(\d+)/.exec(command)?.[1] ?? 1)
  const exportNeedGib = jobs * EXPORT_JOB_GIB
  if (jobs > 1 && exportNeedGib > machine.availableGib) {
    const fits = Math.max(1, Math.floor(machine.availableGib / EXPORT_JOB_GIB))
    return (
      `\`export_lowpoly.py --jobs ${jobs}\` demanderait jusqu'à ${exportNeedGib} Gio (≈ ${EXPORT_JOB_GIB} Gio par ouvrier, ` +
      `d'après les ouvriers tués par earlyoom) pour ${machine.availableGib.toFixed(1)} Gio disponibles. Relancer avec ` +
      `\`--jobs ${fits}\`, ou préfixer par \`${OVERRIDE}\` sur décision de Raphaël.`
    )
  }

  if (isHeavy && machine.availableGib < MIN_AVAILABLE_GIB) {
    return (
      `${machine.availableGib.toFixed(1)} Gio de mémoire disponibles (minimum ${MIN_AVAILABLE_GIB}) : la tâche risque d'être tuée ` +
      `par le noyau. Voir \`ps -eo rss,comm --sort=-rss | head\`, réduire le parallélisme (\`--jobs\`, \`MEASURE_JOBS\`) ` +
      `ou attendre. Pour passer outre sur décision de Raphaël : préfixer la commande par \`${OVERRIDE}\`.`
    )
  }

  return undefined
}

async function readMachine($: EngineInterface): Promise<Machine> {
  const [loadavg, meminfo, cpuinfo] = await Promise.all([
    $.fs.read('/proc/loadavg'),
    $.fs.read('/proc/meminfo'),
    $.fs.read('/proc/cpuinfo'),
  ])
  const availableKib = Number(/^MemAvailable:\s+(\d+)/m.exec(meminfo)?.[1] ?? Number.NaN)

  return {
    load1: Number(loadavg.split(' ')[0]),
    cpus: Math.max(1, (cpuinfo.match(/^processor\s*:/gm) ?? []).length),
    availableGib: Number.isNaN(availableKib) ? Number.POSITIVE_INFINITY : availableKib / 1048576,
  }
}

// Découpe en commandes simples hors guillemets : un message de commit ou un corps de heredoc qui
// cite une commande ne la lance pas. Une substitution `$(...)` dans des guillemets doubles
// s'exécute, elle : son contenu redevient du code.
function segments(command: string): string[] {
  const text = command.replace(/<<-?\s*(['"]?)(\w+)\1[^\n]*\n[\s\S]*?\n\s*\2[ \t]*(?=\n|$)/g, '<<heredoc')
  const parts: string[] = []
  let current = ''
  let quote: string | undefined
  for (let i = 0; i < text.length; i++) {
    const c = text[i] ?? ''
    if (quote !== undefined) {
      if (c === quote) quote = undefined
      else if (quote === '"' && c === '$' && text[i + 1] === '(') {
        parts.push(current)
        current = ''
        i++
        continue
      }
      current += c
      continue
    }
    if (c === "'" || c === '"') {
      quote = c
      current += c
      continue
    }
    if (';|&\n`()'.includes(c) || (c === '$' && text[i + 1] === '(')) {
      parts.push(current)
      current = ''
      continue
    }
    current += c
  }
  parts.push(current)

  return parts.map(s => s.trim()).filter(s => s.length > 0)
}

function tokenize(segment: string): string[] {
  const words: string[] = []
  const re = /'([^']*)'|"((?:\\.|[^"\\])*)"|(\S+)/g
  for (let m = re.exec(segment); m !== null; m = re.exec(segment)) {
    words.push(m[1] ?? m[2] ?? m[3] ?? '')
  }

  // Les affectations de variables en tête (`FOO=1 pkill ...`) ne sont pas la commande.
  while (words.length > 0 && /^[A-Za-z_][A-Za-z0-9_]*=/.test(words[0] ?? '')) words.shift()

  return words
}

// Retire les options globales de git (`-C chemin`, `-c clé=valeur`, `--no-pager`).
function gitArgs(args: string[]): string[] {
  let i = 0
  while (i < args.length && (args[i] ?? '').startsWith('-')) {
    i += args[i] === '-C' || args[i] === '-c' ? 2 : 1
  }

  return args.slice(i)
}

const OPTIONS_WITH_VALUE = new Set(['-u', '-U', '-g', '-G', '-P', '-s', '-t', '-F', '--signal', '--euid', '--uid', '--pgroup', '--group', '--parent', '--session', '--terminal', '--pidfile'])

function firstOperand(args: string[]): string | undefined {
  for (let i = 0; i < args.length; i++) {
    const a = args[i] ?? ''
    if (OPTIONS_WITH_VALUE.has(a)) {
      i++
      continue
    }
    if (!a.startsWith('-')) return a
  }

  return undefined
}

import type { EngineInterface, Register } from 'claude-code'

// Commandes qui produisent des images à juger : captures en run, Hub, planches
// de modèles, galeries, bacs à sable en mode capture, générateurs de sprites.
const PRODUCER =
  /\b(capture_\w+|model_sheet|record_biome_tour)\.sh\b|\b(elan|relief)_sandbox\.sh\b[^\n;&|]*--capture\b|\b(generate_\w+|character_lineup|review_character|export_lowpoly|audio_planche)\.py\b/
// Scripts dont le premier argument est le dossier de sortie.
const DIR_FIRST = /\b(capture_\w+|model_sheet|record_biome_tour)\.sh\s+(?:'([^']+)'|"([^"]+)"|([^\s;&|-][^\s;&|]*))/
const CAPTURE_OPTION = /--capture\s+(?:'([^']+)'|"([^"]+)"|([^\s;&|]+))/
const IMAGE = /\.(png|jpe?g|webp)$/i
// Au-delà, une commande en fond qui n'a rien produit est oubliée.
const PENDING_MAX_MS = 3 * 60 * 60 * 1000
const LISTED = 8

type Pending = { label: string; since: number; dirs: string[] }

export const register: Register = on => {
  let pending: Pending[] = []
  const seen = new Set<string>()

  on('tool.call', { tool: 'Bash' }, async ($, e, next) => {
    const match = PRODUCER.exec(e.command)
    if (match !== null) {
      // Une seconde de marge : -newermt compare à la seconde près.
      const since = (await $.clock.now()) - 1000
      pending.push({ label: match[0], since, dirs: outputDirs(e.command) })
    }

    return next(e)
  })

  on('tool.call', { tool: 'Read' }, ($, e, next) => {
    if (IMAGE.test(e.file_path)) seen.add(e.file_path)

    return next(e)
  })

  on('classic.Stop', async ($, e, next) => {
    if (pending.length === 0) return next(e)

    const now = await $.clock.now()
    const images = await newImages($, pending)
    const unread = images.filter(path => !seen.has(path))

    if (images.length === 0) {
      // Rien encore : la commande tourne peut-être en fond, on attend la fin d'un tour suivant.
      pending = pending.filter(p => now - p.since < PENDING_MAX_MS)
      return next(e)
    }

    const labels = [...new Set(pending.map(p => p.label))].join(', ')
    pending = []
    const isAnyRead = unread.length < images.length
    if (isAnyRead || e.stop_hook_active) return next(e)

    const shown = unread.slice(0, LISTED).map(p => `- ${p}`).join('\n')
    const more = unread.length > LISTED ? `\n… et ${unread.length - LISTED} autres.` : ''

    return {
      block:
        `${$.plugin.name} : ${labels} a produit ${images.length} image(s) et aucune n'a été ouverte. ` +
        `Avant de conclure, ouvrir avec Read les images représentatives et les regarder (CLAUDE.md : « ouvrir les PNG ` +
        `avec Read et les regarder avant de conclure »). Au-delà de deux ou trois, les confier au sous-agent \`regard\`.\n${shown}${more}`,
    }
  })
}

function outputDirs(command: string): string[] {
  const dirs: string[] = []
  const first = DIR_FIRST.exec(command)
  if (first !== null) dirs.push(first[2] ?? first[3] ?? first[4] ?? '')
  const option = CAPTURE_OPTION.exec(command)
  if (option !== null) dirs.push(option[1] ?? option[2] ?? option[3] ?? '')

  // `S=/tmp/x; tools/capture_run.sh $S` : les variables posées dans la commande même se résolvent ici ;
  // une variable venue d'ailleurs laisse un dossier inconnu, et l'on fouille alors le dépôt et /tmp.
  const vars = new Map<string, string>()
  for (const m of command.matchAll(/(?:^|[\s;&(])([A-Za-z_]\w*)=(?:'([^']*)'|"([^"]*)"|([^\s;&|]*))/g)) {
    vars.set(m[1] ?? '', m[2] ?? m[3] ?? m[4] ?? '')
  }
  const expanded = dirs.map(d => d.replace(/\$\{?([A-Za-z_]\w*)\}?/g, (all, name: string) => vars.get(name) ?? all))
  const known = expanded.filter(d => d.length > 0 && !d.includes('$'))

  return known.length < expanded.length ? [...known, '.', '/tmp'] : known
}

// Les dossiers nommés sont fouillés seuls ; un générateur sans dossier connu
// écrit dans le dépôt (assets/), fouillé hors .godot, build et .git.
async function newImages($: EngineInterface, list: Pending[]): Promise<string[]> {
  const since = Math.floor(Math.min(...list.map(p => p.since)) / 1000)
  const dirs = list.some(p => p.dirs.length === 0) ? ['.', ...list.flatMap(p => p.dirs)] : list.flatMap(p => p.dirs)
  const found = new Set<string>()
  const cwd = (await $.process.run(['pwd'])).stdout.trim()
  const home = (await $.env.get('HOME')) ?? ''

  for (const dir of [...new Set(dirs.map(d => d.replace(/^~(?=\/|$)/, home)))]) {
    const { stdout } = await $.process.run(
      [
        'find', dir,
        '(', '-name', '.godot', '-o', '-name', '.git', '-o', '-name', 'build', '-o', '-name', 'node_modules', ')', '-prune',
        '-o', '-type', 'f', '(', '-iname', '*.png', '-o', '-iname', '*.jpg', '-o', '-iname', '*.webp', ')',
        '-newermt', `@${since}`, '-print',
      ],
      { timeoutMs: 20000 },
    ).catch(() => ({ stdout: '' }))
    for (const line of stdout.split('\n')) {
      if (line.length === 0) continue
      found.add(line.startsWith('/') ? line : `${cwd}/${line.replace(/^\.\//, '')}`)
    }
  }

  return [...found].sort()
}

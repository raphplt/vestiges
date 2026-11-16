import type { EngineInterface, Register } from 'claude-code'

const PERIOD_MS = 15000
// Même seuil que tools/bench_ab.sh : au-delà d'un quart des cœurs, pas de FPS crédibles.
const LOAD_FRACTION = 0.25
// Tâches longues du projet, reconnues à leur ligne de commande.
const LONG_TASK = /\btools\/([\w-]+\.(?:sh|py))\b|\b(dotnet build)\b|\b(godot(?:-mono)?)\b.*--headless/
// Tueurs de cette machine : earlyoom (le plus fréquent) et systemd-oomd ; le noyau en dernier recours.
const KILL_LINE = /sending SIG\w+ to process (\d+) uid \d+ "([^"]+)".*?VmRSS (\d+) MiB(?:, cmdline "([^"]{0,160}))?|Killed (\S+) due to|Killed process (\d+) \(([^)]+)\)/

type Machine = { load1: number; cpus: number; availableGib: number; swapUsedGib: number }
type Task = { label: string; seconds: number }
type Kill = { at: number; what: string }

let lastCheckS = 0
let kills: Kill[] = []
let unread: string[] = []

async function refresh($: EngineInterface): Promise<void> {
  const [machine, tasks, fresh] = await Promise.all([readMachine($), longTasks($), newKills($, lastCheckS)])
  lastCheckS = Math.floor((await $.clock.now()) / 1000)
  $.ui.status(statusLine(machine, tasks))

  for (const kill of fresh) {
    kills = [...kills, kill].slice(-20)
    const text = `Processus tué faute de mémoire à ${clock(kill.at)} : ${kill.what}`
    unread.push(text)
    $.ui.toast(text)
    $.ui.log(`${$.plugin.name} : ${text}`)
  }
}

function takeUnread(): string[] {
  const list = unread
  unread = []

  return list.length === 0
    ? []
    : [
        `${list.join('\n')}\nUne tâche lancée par cette session a pu en mourir (export, capture, banc) : vérifier son ` +
          `résultat avant de conclure, et réduire le parallélisme avant de relancer.`,
      ]
}

export const register: Register = on => {
  on('session.start', async ($, e, next) => {
    const started = await next(e)
    lastCheckS = Math.floor((await $.clock.now()) / 1000)
    await $.command.register({
      name: 'moniteur',
      description: 'Charge, mémoire, tâches longues en cours et processus tués faute de mémoire',
    })
    void refresh($).catch(() => undefined)
    $.clock.every(PERIOD_MS, () => void refresh($).catch(() => undefined))

    return started
  })

  on('command.run', { command: 'moniteur' }, async $ => {
    const [machine, tasks] = await Promise.all([readMachine($), longTasks($)])
    const top = await $.process.run(['ps', '-eo', 'pcpu,rss,comm', '--sort=-pcpu', '--no-headers'])
    const lines = top.stdout.split('\n').filter(l => l.trim().length > 0).slice(0, 6).map(l => {
      const [cpu, rss, ...comm] = l.trim().split(/\s+/)
      return `  ${(cpu ?? '').padStart(5)} %  ${(Number(rss) / 1048576).toFixed(1).padStart(5)} Gio  ${comm.join(' ')}`
    })

    return {
      text: [
        statusLine(machine, tasks),
        `Swap utilisé : ${machine.swapUsedGib.toFixed(1)} Gio`,
        'Plus gros consommateurs de CPU :',
        ...lines,
        kills.length === 0
          ? 'Aucun processus tué faute de mémoire depuis le début de la session.'
          : `Tués faute de mémoire :\n${kills.map(k => `  ${clock(k.at)}  ${k.what}`).join('\n')}`,
      ].join('\n'),
    }
  })

  on('classic.PostToolUse', async ($, e, next) => {
    const result = await next(e)
    const notes = takeUnread()

    return notes.length === 0 ? result : { ...result, additionalContext: [...(result.additionalContext ?? []), ...notes] }
  })

  on('classic.UserPromptSubmit', async ($, e, next) => {
    const result = await next(e)
    const notes = takeUnread()

    return notes.length === 0 ? result : { ...result, additionalContext: [...(result.additionalContext ?? []), ...notes] }
  })
}

function statusLine(m: Machine, tasks: Task[]): string {
  const limit = Math.max(1, Math.floor(m.cpus * LOAD_FRACTION))
  const load = `charge ${m.load1.toFixed(1).replace('.', ',')}/${m.cpus}${m.load1 > limit ? ' (trop pour un banc)' : ''}`
  const memory = `${m.availableGib.toFixed(0)} Gio dispo${m.availableGib < 3 ? ' (bas)' : ''}`
  const background = tasks.length === 0 ? '' : ` · en cours : ${tasks.map(t => `${t.label} ${duration(t.seconds)}`).join(', ')}`

  return `${load} · ${memory}${background}`
}

async function readMachine($: EngineInterface): Promise<Machine> {
  const [loadavg, meminfo, cpuinfo] = await Promise.all([
    $.fs.read('/proc/loadavg'),
    $.fs.read('/proc/meminfo'),
    $.fs.read('/proc/cpuinfo'),
  ])
  const kib = (key: string): number => Number(new RegExp(`^${key}:\\s+(\\d+)`, 'm').exec(meminfo)?.[1] ?? 0)

  return {
    load1: Number(loadavg.split(' ')[0]),
    cpus: Math.max(1, (cpuinfo.match(/^processor\s*:/gm) ?? []).length),
    availableGib: kib('MemAvailable') / 1048576,
    swapUsedGib: (kib('SwapTotal') - kib('SwapFree')) / 1048576,
  }
}

async function longTasks($: EngineInterface): Promise<Task[]> {
  const { stdout } = await $.process.run(['ps', 'x', '-o', 'etimes=,args='])
  const byLabel = new Map<string, number>()
  for (const line of stdout.split('\n')) {
    const m = /^\s*(\d+)\s+(.*)$/.exec(line)
    if (m === null) continue
    const task = LONG_TASK.exec(m[2] ?? '')
    if (task === null) continue
    const label = (task[1] ?? task[2] ?? task[3] ?? '').replace(/\.(sh|py)$/, '')
    byLabel.set(label, Math.max(byLabel.get(label) ?? 0, Number(m[1])))
  }

  return [...byLabel].map(([label, seconds]) => ({ label, seconds })).sort((a, b) => b.seconds - a.seconds).slice(0, 4)
}

async function newKills($: EngineInterface, sinceS: number): Promise<Kill[]> {
  if (sinceS === 0) return []
  const { stdout } = await $.process.run([
    'journalctl', '-u', 'earlyoom', '-u', 'systemd-oomd', '--since', `@${sinceS}`,
    '-q', '--no-pager', '-o', 'short-unix', '-g', 'sending SIG|Killed',
  ]).catch(() => ({ stdout: '' }))
  const kernel = (await $.fs.read('/proc/vmstat').catch(() => '')).includes('oom_kill 0')
    ? ''
    : (await $.process.run(['journalctl', '-k', '--since', `@${sinceS}`, '-q', '--no-pager', '-o', 'short-unix', '-g', 'Killed process']).catch(() => ({ stdout: '' }))).stdout

  const kills: Kill[] = []
  for (const line of `${stdout}\n${kernel}`.split('\n')) {
    const m = KILL_LINE.exec(line)
    if (m === null) continue
    const at = Number(line.split(' ')[0]) * 1000
    const what =
      m[2] !== undefined
        ? `${m[2]} (pid ${m[1]}, ${(Number(m[3]) / 1024).toFixed(1)} Gio)${describeCmdline(m[4])}`
        : m[5] !== undefined
          ? `cgroup ${m[5]} (systemd-oomd)`
          : `${m[7]} (pid ${m[6]}, noyau)`
    kills.push({ at, what })
  }

  return kills
}

// Un ouvrier de multiprocessing ne dit rien de sa tâche : on garde ce qui la nomme.
function describeCmdline(cmdline: string | undefined): string {
  if (cmdline === undefined) return ''
  const tool = /tools\/[\w-]+\.\w+/.exec(cmdline)?.[0]
  if (tool !== undefined) return `, ${tool}`
  if (cmdline.includes('multiprocessing')) return ', ouvrier multiprocessing Python (export_lowpoly --jobs ?)'

  return `, ${cmdline.slice(0, 80)}`
}

function duration(seconds: number): string {
  return seconds < 60 ? `${seconds} s` : seconds < 3600 ? `${Math.floor(seconds / 60)} min` : `${Math.floor(seconds / 3600)} h ${Math.floor((seconds % 3600) / 60)}`
}

function clock(ms: number): string {
  const d = new Date(ms)
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

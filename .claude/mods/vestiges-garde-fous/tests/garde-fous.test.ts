import { expect, test } from 'claude-code/testing'

const LOADAVG = (load: number): string => `${load} 1.00 1.00 2/2851 214089`
const MEMINFO = (gib: number): string => `MemTotal: 32000000 kB\nMemAvailable: ${gib * 1048576} kB\n`
const CPUINFO = Array.from({ length: 16 }, (_, i) => `processor\t: ${i}`).join('\n')

const DENIED = [
  'pkill -f godot',
  'pkill -9 -f "tools/capture_run.sh"',
  'sleep 1; pkill -f export_lowpoly || true',
  'kill $(pgrep -f relief_sandbox)',
  'git add -A',
  'rtk git add .',
  'git -C /home/raphael/dev/perso/vestiges add --all',
  'git commit -am "feat: x"',
  'git commit -a -m "x"',
  'git stash',
  'git reset --hard HEAD',
  'git checkout -- .',
  'git clean -fd',
]

const ALLOWED = [
  "pkill -f '[g]odot'",
  'pkill godot-mono',
  'pgrep -f godot',
  'git add scripts/Core/Player.cs doc/plans/DECISIONS.md',
  'git commit -m "fix: tout -a dans le texte"',
  'git commit --amend --no-edit',
  'git stash list',
  'git reset HEAD scripts/Core/Player.cs',
  'git checkout -- scripts/Core/Player.cs',
  'dotnet build',
]

for (const command of DENIED) {
  test(`refuse : ${command}`, async ($, on) => {
    on('tool.call', () => ({ result: {} }))
    const r = await $.tool.call({ tool: 'Bash', command })
    expect(r.deny).toBeDefined()
  })
}

for (const command of ALLOWED) {
  test(`laisse passer : ${command}`, async ($, on) => {
    on('tool.call', () => ({ result: {} }))
    const r = await $.tool.call({ tool: 'Bash', command })
    expect(r.deny).toBeUndefined()
  })
}

function machine(on: Parameters<Extract<Parameters<typeof test>[1], (...args: never[]) => unknown>>[1], load: number, gib: number): void {
  on('fs.read', (_, e) => ({
    value: e.path === '/proc/loadavg' ? LOADAVG(load) : e.path === '/proc/meminfo' ? MEMINFO(gib) : CPUINFO,
  }))
  on('tool.call', () => ({ result: {} }))
}

test('banc refusé sous charge, accepté au calme ou forcé', async ($, on) => {
  machine(on, 9.5, 20)
  const loaded = await $.tool.call({ tool: 'Bash', command: 'BENCH_REPEATS=1 tools/benchmark_movement.sh /tmp/b' })
  expect(loaded.deny).toContain('charge 9.50')
  const forced = await $.tool.call({ tool: 'Bash', command: 'GARDE_FOUS=off tools/bench_ab.sh HEAD /tmp/b' })
  expect(forced.deny).toBeUndefined()
})

test('banc accepté machine calme', async ($, on) => {
  machine(on, 1.2, 20)
  const calm = await $.tool.call({ tool: 'Bash', command: 'tools/bench_ab.sh HEAD /tmp/b' })
  expect(calm.deny).toBeUndefined()
})

test('export de masse : --jobs borné par la mémoire', async ($, on) => {
  machine(on, 1, 17)
  const tooMany = await $.tool.call({ tool: 'Bash', command: 'python3 tools/export_lowpoly.py --all --jobs 6' })
  expect(tooMany.deny).toContain('--jobs 3')
  const fits = await $.tool.call({ tool: 'Bash', command: 'python3 tools/export_lowpoly.py --all --jobs 3' })
  expect(fits.deny).toBeUndefined()
})

test('tâche lourde refusée mémoire basse', async ($, on) => {
  machine(on, 1, 2)
  const r = await $.tool.call({ tool: 'Bash', command: 'tools/capture_run.sh /tmp/c 20' })
  expect(r.deny).toContain('Gio de mémoire disponibles')
})

type Git = { staged: string; index: string[] }

function repo(on: Parameters<Extract<Parameters<typeof test>[1], (...args: never[]) => unknown>>[1], git: Git): void {
  on('process.run', (_, e) => {
    const isDiff = e.argv.includes('diff')
    const asked = e.argv.slice(e.argv.indexOf('--') + 1)
    const stdout = isDiff ? git.staged : git.index.filter(p => asked.includes(p)).join('\n')
    return { value: { exitCode: 0, stdout, stderr: '', isStdoutTruncated: false, isStderrTruncated: false } }
  })
  on('tool.call', () => ({ result: {} }))
}

const COMMIT = 'git commit -m "feat: x"'

test('commit : trailer Co-Authored-By refusé', async ($, on) => {
  repo(on, { staged: 'M\tCLAUDE.md', index: [] })
  const r = await $.tool.call({
    tool: 'Bash',
    command: `git commit -m "$(cat <<'EOF'\nfeat: x\n\nCo-Authored-By: Claude <noreply@anthropic.com>\nEOF\n)"`,
  })
  expect(r.deny).toContain('Co-Authored-By')
})

test('commit : .cs neuf sans .cs.uid refusé, avec accepté', async ($, on) => {
  repo(on, { staged: 'A\tscripts/Core/Foo.cs', index: [] })
  expect((await $.tool.call({ tool: 'Bash', command: COMMIT })).deny).toContain('Foo.cs.uid')
})

test('commit : .cs neuf avec son .cs.uid accepté', async ($, on) => {
  repo(on, { staged: 'A\tscripts/Core/Foo.cs\nA\tscripts/Core/Foo.cs.uid', index: ['scripts/Core/Foo.cs.uid'] })
  expect((await $.tool.call({ tool: 'Bash', command: COMMIT })).deny).toBeUndefined()
})

test('commit : PNG neuf d\'assets sans .import refusé', async ($, on) => {
  repo(on, { staged: 'A\tassets/props/forest/tree_01.png', index: [] })
  expect((await $.tool.call({ tool: 'Bash', command: COMMIT })).deny).toContain('tree_01.png.import')
})

test('commit : PNG hors assets (doc) accepté sans .import', async ($, on) => {
  repo(on, { staged: 'A\tdoc/images/planche.png', index: [] })
  expect((await $.tool.call({ tool: 'Bash', command: COMMIT })).deny).toBeUndefined()
})

test('commit : .cs supprimé dont le .cs.uid reste refusé', async ($, on) => {
  repo(on, { staged: 'D\tscripts/Core/Old.cs', index: ['scripts/Core/Old.cs.uid'] })
  expect((await $.tool.call({ tool: 'Bash', command: COMMIT })).deny).toContain('Old.cs.uid')
})

test('commit : fichier de .godot indexé refusé', async ($, on) => {
  repo(on, { staged: 'A\t.godot/imported/x.ctex', index: [] })
  expect((await $.tool.call({ tool: 'Bash', command: 'rtk git commit -m "x"' })).deny).toContain('.godot/')
})

// Un message ou un heredoc cite des commandes sans les lancer.
const QUOTED = [
  `git commit -q -F - <<'EOF'\nchore: garde-fous\n\ngarde-fous (pkill -f qui se tue, git add -A)\nEOF`,
  'git commit -m "fix: plus de pkill -f godot ni de git add -A dans le shell"',
  `git commit -m "$(cat <<'EOF'\nfeat: x\n\npkill -f godot, git stash\nEOF\n)"`,
  "echo 'git reset --hard' > note.txt",
]

for (const command of QUOTED) {
  test(`cité, pas lancé : ${command.slice(0, 40)}`, async ($, on) => {
    repo(on, { staged: 'M\tCLAUDE.md', index: [] })
    expect((await $.tool.call({ tool: 'Bash', command })).deny).toBeUndefined()
  })
}

test('substitution dans des guillemets doubles : son contenu est du code', async ($, on) => {
  on('tool.call', () => ({ result: {} }))
  const r = await $.tool.call({ tool: 'Bash', command: 'echo "$(pkill -f godot)"' })
  expect(r.deny).toBeDefined()
})

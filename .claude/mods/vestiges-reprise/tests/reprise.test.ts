import { expect, test } from 'claude-code/testing'

const README = `# Dossier\n\n**16 novembre — dernière chose ([plan 60](60.md)) :**\n- un point\n\n**15 novembre — avant :**\n- ancien\n`

function engine(on: Parameters<Extract<Parameters<typeof test>[1], (...args: never[]) => unknown>>[1], status: string): void {
  on('process.run', (_, e) => {
    const stdout = e.argv.includes('branch') ? 'main\n' : e.argv.includes('status') ? status : 'abc123 feat: x\ndef456 fix: y\n'
    return { value: { exitCode: 0, stdout, stderr: '', isStdoutTruncated: false, isStderrTruncated: false } }
  })
  on('fs.read', () => ({ value: README }))
  on('classic.SessionStart', () => ({}))
}

const START = { session_id: 's', transcript_path: '/t', cwd: '/r', hook_event_name: 'SessionStart', source: 'startup' } as const

test('au démarrage : état, commits et dernière mise à jour', async ($, on) => {
  engine(on, ' M CLAUDE.md\n?? .claude/mods/\n')
  const r = await $.classic.SessionStart(START)
  const text = (r.additionalContext ?? []).join('\n')
  expect(text).toContain('branche main')
  expect(text).toContain('2 fichier(s) modifié(s)')
  expect(text).toContain('abc123 feat: x')
  expect(text).toContain('16 novembre — dernière chose')
  expect(text).not.toContain('15 novembre')
})

test('arbre propre', async ($, on) => {
  engine(on, '')
  const r = await $.classic.SessionStart(START)
  expect((r.additionalContext ?? []).join('\n')).toContain('Arbre propre.')
})

test('reprise (resume) : rien d\'ajouté', async ($, on) => {
  engine(on, ' M x\n')
  const r = await $.classic.SessionStart({ ...START, source: 'resume' })
  expect(r.additionalContext).toBeUndefined()
})

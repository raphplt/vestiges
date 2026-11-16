import { expect, test } from 'claude-code/testing'

type On = Parameters<Extract<Parameters<typeof test>[1], (...args: never[]) => unknown>>[1]

const SESSION_MODEL = 'claude-opus-5-5'

// Fait passer une requête du tour et rend le modèle et l'effort que le mod a demandés.
async function step(
  $: Parameters<Extract<Parameters<typeof test>[1], (...args: never[]) => unknown>>[0],
  turnId: string,
  index: number,
  messageCount: number,
): Promise<{ model: string; effort: unknown }> {
  const stream = $.turn.step({ turnId, index, messageCount, model: SESSION_MODEL, effort: 'high' })
  return JSON.parse((await drain(stream)).answer) as { model: string; effort: unknown }
}

async function drain<R>(stream: AsyncGenerator<unknown, R>): Promise<R> {
  let it = await stream.next()
  while (it.done !== true) it = await stream.next()
  return it.value
}

function engine(on: On): void {
  on('ui.status', () => ({ value: undefined }))
  on('prompt.submit', (_, e) => ({ text: e.text }))
  on('model.classify', () => ({ value: undefined }))
  // L'engine renvoie dans la réponse ce qu'on lui a demandé, pour l'inspecter.
  on('turn.step', async function* (_, e) {
    return {
      turnId: e.turnId,
      index: e.index,
      answer: JSON.stringify({ model: e.model, effort: e.effort }),
      toolUses: [],
      stopReason: 'end_turn',
      usage: null,
    }
  })
}

test('C# : effort high, modèle de la session', async ($, on) => {
  engine(on)
  await $.prompt.submit({ text: 'Corrige le bug du shader de la scène Main.tscn' })
  const r = await step($, 't1', 0, 1)
  expect(r).toEqual({ model: SESSION_MODEL, effort: 'high' })
})

test('docs au premier tour : sonnet, qui reste, puis monte pour du C#', async ($, on) => {
  engine(on)
  await $.prompt.submit({ text: 'Rédige la doc du plan 60 dans DECISIONS.md' })
  expect(await step($, 't1', 0, 1)).toEqual({ model: 'sonnet', effort: 'medium' })
  expect(await step($, 't1', 1, 3)).toEqual({ model: 'sonnet', effort: 'medium' })

  await $.prompt.submit({ text: 'Mets à jour la roadmap' })
  expect(await step($, 't2', 0, 5)).toEqual({ model: 'sonnet', effort: 'medium' })

  await $.prompt.submit({ text: 'Implémente le shader de la scène en C#' })
  expect(await step($, 't3', 0, 7)).toEqual({ model: SESSION_MODEL, effort: 'high' })

  // Redescendre viderait le cache : la doc suivante reste sur le modèle de la session.
  await $.prompt.submit({ text: 'Rédige la doc du plan' })
  expect(await step($, 't4', 0, 9)).toEqual({ model: SESSION_MODEL, effort: 'medium' })
})

test('réglage JSON en cours de session : effort low sans changer de modèle', async ($, on) => {
  engine(on)
  await $.prompt.submit({ text: 'Implémente le shader en C#' })
  await step($, 't1', 0, 1)
  await $.prompt.submit({ text: 'Baisse les dégâts du rôdeur dans data/enemies.json' })
  expect(await step($, 't2', 0, 5)).toEqual({ model: SESSION_MODEL, effort: 'low' })
})

test('réponse courte : garde la catégorie précédente', async ($, on) => {
  engine(on)
  await $.prompt.submit({ text: 'Optimise le banc de FPS' })
  await $.prompt.submit({ text: 'oui vas-y' })
  expect(await step($, 't1', 0, 1)).toEqual({ model: SESSION_MODEL, effort: 'xhigh' })
})

test('/route off coupe tout, /route auto rétablit', async ($, on) => {
  engine(on)
  on('command.register', () => ({ value: { command: 'route' } }))
  await $.prompt.submit({ text: 'Optimise le banc de FPS' })
  const off = await $.command.run({ command: 'route', args: 'off' })
  expect(off.text).toContain('Mode : coupé')
  expect(await step($, 't1', 0, 1)).toEqual({ model: SESSION_MODEL, effort: 'high' })
  await $.command.run({ command: 'route', args: 'auto' })
  expect(await step($, 't2', 0, 3)).toEqual({ model: SESSION_MODEL, effort: 'xhigh' })
})

test('sous-agent : jamais touché', async ($, on) => {
  engine(on)
  await $.prompt.submit({ text: 'Rédige la doc du plan' })
  const stream = $.turn.step({ turnId: 'a', index: 0, messageCount: 1, model: SESSION_MODEL, effort: 'high', agentId: 'x' })
  expect(JSON.parse((await drain(stream)).answer)).toEqual({ model: SESSION_MODEL, effort: 'high' })
})

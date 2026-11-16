import { atom, read, update } from 'claude-code'
import type { EngineInterface, Register } from 'claude-code'

import type { CacheStats, Category, Effort, Forced, Route, Table } from '../types'
import { ROUTES } from './routes'

const EFFORTS: readonly Effort[] = ['low', 'medium', 'high', 'xhigh', 'max']
// Changer de modèle vide le cache de prompt : tout le contexte se repaie au prix d'écriture.
// Le modèle d'une catégorie ne s'applique donc qu'au premier tour ; ensuite on ne fait que monter.
const MODEL_RANK: Record<string, number> = { haiku: 0, sonnet: 1, opus: 2, fable: 3 }

const routeAtom = atom({ plugin: 'vestiges-routage', key: 'route' } as const, null)
const stickyAtom = atom({ plugin: 'vestiges-routage', key: 'stickyModel' } as const, null)
const forcedAtom = atom({ plugin: 'vestiges-routage', key: 'forced' } as const, {})
const cacheAtom = atom({ plugin: 'vestiges-routage', key: 'lastCache' } as const, null)

// Tour dont la première requête ouvrait la session : le seul où le modèle peut descendre.
let openingTurnId: string | undefined

export const register: Register = on => {
  on('session.start', async ($, e, next) => {
    const started = await next(e)
    await $.command.register({
      name: 'route',
      description: 'Routage modèle/effort : sans argument l\'état ; auto, off, <catégorie>, effort <niveau>, modele <alias>',
    })
    await showStatus($)

    return started
  })

  on('prompt.submit', async ($, e, next) => {
    if (e.isMeta) return next(e)
    const forced = await read($, forcedAtom)
    const previous = await read($, routeAtom)
    const route = await classify($, ROUTES, e.text, e.command?.name, previous, forced)
    await update($, routeAtom, () => route)
    await showStatus($)

    return next(e)
  })

  on('turn.step', async function* ($, e, next) {
    if (e.agentId !== undefined) return yield* next(e)

    const forced = await read($, forcedAtom)
    const route = await read($, routeAtom)
    if (forced.isOff === true || route === null) return yield* next(e)

    if (e.index === 0 && e.messageCount <= 1) openingTurnId = e.turnId
    const model = await chooseModel($, e.model, route, e.turnId === openingTurnId)
    const effort = e.effort === undefined ? undefined : route.effort ?? e.effort
    const result = yield* next({ ...e, ...(model === undefined ? {} : { model }), ...(effort === undefined ? {} : { effort }) })

    if (result.usage !== null) {
      const stats: CacheStats = {
        read: result.usage.cache_read_input_tokens,
        written: result.usage.cache_creation_input_tokens,
        uncached: result.usage.input_tokens,
        model: result.usage.model,
      }
      await update($, cacheAtom, () => stats)
      $.ui.log(`${$.plugin.name} : étape ${e.index} ${stats.model} effort ${effort ?? '-'} cache lu ${stats.read} écrit ${stats.written} hors cache ${stats.uncached}`, { to: 'debug' })
    }

    return result
  })

  on('command.run', { command: 'route' }, async ($, e) => {
    const t = ROUTES
    const [verb = '', value = ''] = e.args.trim().split(/\s+/)
    const ids = t.categories.map(c => c.id)

    if (verb === 'auto') await update($, forcedAtom, () => ({}))
    else if (verb === 'off') await update($, forcedAtom, () => ({ isOff: true }))
    else if (ids.includes(verb)) await update($, forcedAtom, f => ({ ...f, isOff: undefined, category: verb }))
    else if (verb === 'effort' && (EFFORTS as readonly string[]).includes(value)) {
      await update($, forcedAtom, f => ({ ...f, isOff: undefined, effort: value as Effort }))
    } else if ((verb === 'modele' || verb === 'model') && value.length > 0) {
      await update($, forcedAtom, f => ({ ...f, isOff: undefined, model: value }))
      await update($, stickyAtom, () => value)
    } else if (verb !== '') {
      return { text: `Usage : /route [auto | off | ${ids.join(' | ')} | effort <${EFFORTS.join('|')}> | modele <alias>]` }
    }

    await showStatus($)

    return { text: await describe($) }
  })
}

async function classify(
  $: EngineInterface,
  t: Table,
  text: string,
  command: string | undefined,
  previous: Route | null,
  forced: Forced,
): Promise<Route> {
  const byId = (id: string): Category | undefined => t.categories.find(c => c.id === id)
  const make = (c: Category, reason: string): Route => ({
    category: c.id,
    effort: forced.effort ?? c.effort,
    model: forced.model ?? c.model,
    reason,
  })

  const forcedCategory = forced.category === undefined ? undefined : byId(forced.category)
  if (forcedCategory !== undefined) return make(forcedCategory, 'forcée par /route')

  const fromCommand = command === undefined ? undefined : byId(t.commands[command] ?? '')
  if (fromCommand !== undefined) return make(fromCommand, `commande /${command}`)
  if (command !== undefined && previous !== null) return { ...previous, reason: `commande /${command}, catégorie conservée` }

  const scored = t.categories
    .map(c => ({ c, score: c.keywords.filter(k => new RegExp(k, 'im').test(text)).length }))
    .filter(s => s.score > 0)
    .sort((a, b) => b.score - a.score)
  const best = scored[0]
  if (best !== undefined) return make(best.c, `mots-clés (${best.score})`)

  // « oui », « continue », « vas-y » : la suite du travail en cours.
  if (text.trim().length < t.shortPromptChars && previous !== null) return { ...previous, reason: 'réponse courte, catégorie conservée' }

  const labels = t.categories.map(c => c.description)
  const picked = await $.model.classify(text.slice(0, 4000), labels, { model: 'haiku' }).catch(() => undefined)
  const chosen = t.categories.find(c => c.description === picked)
  if (chosen !== undefined) return make(chosen, 'classée par haiku')

  return previous ?? { category: 'défaut', reason: 'non classée', ...(forced.effort === undefined ? {} : { effort: forced.effort }) }
}

async function chooseModel($: EngineInterface, current: string, route: Route, isOpening: boolean): Promise<string | undefined> {
  const sticky = await read($, stickyAtom)
  const forced = await read($, forcedAtom)
  if (forced.model !== undefined) return forced.model

  if (isOpening) {
    await update($, stickyAtom, () => route.model ?? null)
    return route.model
  }

  if (sticky === null) return undefined
  // Une catégorie qui veut le modèle de la session (ou plus fort) fait monter : on lâche la descente, pour de bon.
  if (rank(route.model ?? current) > rank(sticky)) {
    await update($, stickyAtom, () => null)
    return route.model
  }

  return sticky
}

function rank(model: string): number {
  const name = Object.keys(MODEL_RANK).find(alias => model.toLowerCase().includes(alias))
  return name === undefined ? 1 : MODEL_RANK[name] ?? 1
}

async function showStatus($: EngineInterface): Promise<void> {
  const forced = await read($, forcedAtom)
  const route = await read($, routeAtom)
  const sticky = await read($, stickyAtom)
  if (forced.isOff === true) {
    $.ui.status('route : coupée')
    return
  }
  if (route === null) {
    $.ui.status('route : auto, en attente d\'une demande')
    return
  }
  const model = forced.model ?? sticky ?? 'modèle de session'
  $.ui.status(`route : ${route.category} · effort ${route.effort ?? 'session'} · ${model}${forced.category !== undefined ? ' (forcée)' : ''}`)
}

async function describe($: EngineInterface): Promise<string> {
  const forced = await read($, forcedAtom)
  const route = await read($, routeAtom)
  const sticky = await read($, stickyAtom)
  const cache = await read($, cacheAtom)
  const mode = forced.isOff === true ? 'coupé' : forced.category !== undefined || forced.effort !== undefined || forced.model !== undefined ? `forcé ${JSON.stringify(forced)}` : 'auto'

  return [
    `Mode : ${mode}`,
    route === null ? 'Aucune demande classée encore.' : `Dernière demande : ${route.category} (${route.reason}), effort ${route.effort ?? 'de la session'}`,
    `Modèle : ${forced.model ?? sticky ?? 'celui de la session'}${sticky !== null && forced.model === undefined ? ' (choisi au premier tour, ne descend plus)' : ''}`,
    cache === null
      ? 'Cache : aucune requête mesurée.'
      : `Dernière requête (${cache.model}) : cache lu ${cache.read}, écrit ${cache.written}, hors cache ${cache.uncached}`,
  ].join('\n')
}

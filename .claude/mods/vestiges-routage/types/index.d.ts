export type Effort = 'low' | 'medium' | 'high' | 'xhigh' | 'max'

export type Route = {
  category: string
  effort?: Effort
  model?: string
  reason: string
}

export type Forced = { category?: string; effort?: Effort; model?: string; isOff?: boolean }

export type CacheStats = { read: number; written: number; uncached: number; model: string }

declare module 'claude-code' {
  interface PluginState {
    'vestiges-routage': {
      route: Route | null
      stickyModel: string | null
      forced: Forced
      lastCache: CacheStats | null
    }
  }
}

export type Category = { id: string; effort?: Effort; model?: string; description: string; keywords: string[] }

export type Table = { shortPromptChars: number; categories: Category[]; commands: Record<string, string> }

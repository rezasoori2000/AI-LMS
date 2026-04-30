import { useState, useEffect, useCallback } from 'react'

type Theme = 'light' | 'dark'

const STORAGE_KEY = 'ai-lms-theme'

function readStoredTheme(): Theme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored === 'dark' || stored === 'light') return stored
  } catch {
    // localStorage may be unavailable in some environments (e.g. SSR, sandboxed iframes)
  }
  // Respect OS preference as the default
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(theme: Theme): void {
  if (theme === 'dark') {
    document.documentElement.dataset.theme = 'dark'
  } else {
    delete document.documentElement.dataset.theme
  }
}

/**
 * Dark / light mode toggle.
 *
 * On mount, reads from localStorage and falls back to the OS colour-scheme
 * preference. Writes `data-theme="dark"` on <html> which activates the
 * `[data-theme='dark']` block in index.css — no component re-renders needed
 * for the visual switch; the CSS layer handles everything.
 *
 * Phase 2 usage:
 *   Add a <ThemeToggleButton /> to the Topbar action area:
 *     const { isDark, toggleTheme } = useTheme()
 *     <button onClick={toggleTheme}>{isDark ? '☀️' : '🌙'}</button>
 *
 * Tenant note:
 *   If a tenant wants to force a specific theme (e.g. always light),
 *   inject `data-theme` as a server-side attribute on <html> before the app
 *   loads to prevent a flash of the wrong theme. The hook's `applyTheme`
 *   call on mount will then be a no-op since the attribute is already set.
 */
export function useTheme() {
  const [theme, setTheme] = useState<Theme>(() => readStoredTheme())

  // Apply on mount and whenever theme changes
  useEffect(() => {
    applyTheme(theme)
    try {
      localStorage.setItem(STORAGE_KEY, theme)
    } catch {
      // Ignore storage errors
    }
  }, [theme])

  const toggleTheme = useCallback(() => {
    setTheme((prev) => (prev === 'dark' ? 'light' : 'dark'))
  }, [])

  const setLightTheme = useCallback(() => setTheme('light'), [])
  const setDarkTheme  = useCallback(() => setTheme('dark'),  [])

  return {
    theme,
    isDark: theme === 'dark',
    isLight: theme === 'light',
    toggleTheme,
    setLightTheme,
    setDarkTheme,
  }
}

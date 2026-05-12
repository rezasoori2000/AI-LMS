/**
 * tokenStorage — thin helpers for persisting the JWT access token.
 *
 * Storage choice: localStorage
 * - Simple, synchronous, works across page reloads.
 * - XSS risk: any injected script can read it. Acceptable for Phase 1 MVP.
 * - Phase 3 migration path: replace with a short-lived httpOnly cookie issued
 *   by the backend (requires a /auth/refresh endpoint). The rest of the app
 *   only calls these three helpers, so the migration is a one-file change.
 *
 * The key is a constant so it is never mistyped in callsites.
 * It must stay in sync with the existing read in api-client.ts:
 *   `localStorage.getItem('auth_token')`
 */

const TOKEN_KEY = 'auth_token'

export function persistToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function retrieveToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from 'react'
import { persistToken, retrieveToken, clearToken } from '@/utils/tokenStorage'
import type { AuthApiResponse, BackendUserRoleValue } from '@/types'

// ── Session user shape ────────────────────────────────────────────────────────

/**
 * AuthUser — the minimum user data the frontend cares about at the session level.
 *
 * Sourced from the AuthApiResponse on login/register.
 *
 * Future expansion points (do NOT add until needed):
 * - `tenantId`: Guid — wired in when TenantProvider is introduced (Phase 3)
 * - `displayName`: string — fetched from a /me profile endpoint (Phase 2)
 * - `permissions`: string[] — computed from role + tenant policy (Phase 3)
 */
export interface AuthUser {
  id:       string
  email:    string
  role:     BackendUserRoleValue
  tenantId: string | null
}

// ── Context shape ─────────────────────────────────────────────────────────────

interface AuthContextValue {
  /** Currently signed-in user, or null if unauthenticated. */
  user:            AuthUser | null
  /** Raw JWT access token, or null. Used by api-client.ts interceptor. */
  token:           string | null
  /** True when a valid token is present in state (not verified server-side). */
  isAuthenticated: boolean
  /**
   * True during the initial synchronous session-restore on mount.
   * Render a full-page spinner while this is true to avoid a flash of
   * unauthenticated UI before the stored token is read.
   */
  isRestoring:     boolean
  /**
   * Called after a successful login or register API response.
   * Persists the token, sets user state, triggers re-renders.
   */
  signIn:  (response: AuthApiResponse) => void
  /** Clears token from storage and resets all auth state. */
  signOut: () => void
}

// ── Context ───────────────────────────────────────────────────────────────────

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

// ── Provider ──────────────────────────────────────────────────────────────────

interface AuthProviderProps {
  children: ReactNode
}

/**
 * AuthProvider — manages auth state for the entire application.
 *
 * Session restoration strategy:
 * 1. On mount, `retrieveToken()` reads localStorage synchronously.
 * 2. If a token is found, it is set in state so api-client.ts interceptors
 *    can attach it immediately on the first request.
 * 3. User metadata (id, email, role) from the LAST successful auth response
 *    is stored alongside the token in a separate localStorage key so we can
 *    reconstruct `user` without a network round-trip on reload.
 * 4. `isRestoring` is set to false after the synchronous read — consumers
 *    gate rendering on this flag to avoid a flash of unauthenticated content.
 *
 * Deferred:
 * - Verifying token expiry on restore (check `exp` claim in Phase 2)
 * - Fetching a fresh /me profile on restore (Phase 2)
 * - Refresh token flow (Phase 3)
 * - Multi-tab session sync via storage events (Phase 3)
 */
export function AuthProvider({ children }: AuthProviderProps) {
  const [token,      setToken]      = useState<string | null>(null)
  const [user,       setUser]       = useState<AuthUser | null>(null)
  const [isRestoring, setRestoring] = useState(true)

  // ── Session restore on mount ──────────────────────────────────────────────
  useEffect(() => {
    const storedToken = retrieveToken()
    const storedUser  = readStoredUser()

    if (storedToken && storedUser) {
      setToken(storedToken)
      setUser(storedUser)
    }
    setRestoring(false)
  }, [])

  // ── Session expiry listener ───────────────────────────────────────────────
  // api-client.ts dispatches 'auth:session-expired' whenever a 401 response
  // is received. This resets React state synchronously so ProtectedRoute
  // redirects on the next render — without requiring a page reload.
  useEffect(() => {
    function handleSessionExpired() {
      clearToken()
      clearStoredUser()
      setToken(null)
      setUser(null)
    }
    window.addEventListener('auth:session-expired', handleSessionExpired)
    return () => window.removeEventListener('auth:session-expired', handleSessionExpired)
  }, []) // setToken/setUser are stable React dispatch functions

  // ── signIn ────────────────────────────────────────────────────────────────
  const signIn = useCallback((response: AuthApiResponse) => {
    const authUser: AuthUser = {
      id:       response.userId,
      email:    response.email,
      role:     response.role,
      tenantId: response.tenantId,
    }
    persistToken(response.accessToken)
    writeStoredUser(authUser)
    setToken(response.accessToken)
    setUser(authUser)
  }, [])

  // ── signOut ───────────────────────────────────────────────────────────────
  const signOut = useCallback(() => {
    clearToken()
    clearStoredUser()
    setToken(null)
    setUser(null)
    // Navigation to /auth/login is handled by the ProtectedRoute consumer
    // or the caller — keeping this function pure of routing concerns.
  }, [])

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: token !== null,
        isRestoring,
        signIn,
        signOut,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

// ── Hook ──────────────────────────────────────────────────────────────────────

/**
 * useAuth — consume auth state anywhere in the component tree.
 *
 * Throws if used outside AuthProvider so mistakes surface immediately
 * at development time rather than producing silent undefined behaviour.
 */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (ctx === undefined) {
    throw new Error('useAuth must be used inside <AuthProvider>')
  }
  return ctx
}

// ── Stored user helpers ───────────────────────────────────────────────────────

const USER_KEY = 'auth_user'

function writeStoredUser(user: AuthUser): void {
  try {
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  } catch {
    // Storage quota exceeded — non-fatal; session is still valid via token.
  }
}

function readStoredUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

function clearStoredUser(): void {
  localStorage.removeItem(USER_KEY)
}

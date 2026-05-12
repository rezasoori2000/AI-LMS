import axios from 'axios'
import type { TFunction } from 'i18next'

/**
 * mapAuthError — centralized Axios error → user-facing string translator.
 *
 * The backend returns RFC 7807 Problem Details:
 *   { type, title, status, traceId }
 *
 * ASP.NET model validation (400) returns the extended shape:
 *   { type, title, status, errors: { FieldName: ["message"] } }
 *
 * Context-specific status mapping:
 * - 'login'    + 401 → "Invalid email or password."    (don't echo server details)
 * - 'register' + 409 → "An account with this email address already exists."
 * - 400 with field errors → first field validation message from server
 * - No response (network/timeout) → "Unable to reach the server."
 * - Anything else → generic error message
 *
 * Deliberately does NOT pass raw server error strings to the UI to avoid
 * leaking internal implementation details to end users.
 */

export type AuthErrorContext = 'login' | 'register'

export function mapAuthError(
  err:     unknown,
  context: AuthErrorContext,
  t:       TFunction,
): string {
  // Non-Axios error (e.g. programming error, cancelled promise)
  if (!axios.isAxiosError(err)) {
    return t('errors.generic')
  }

  // No response = network error or request timed out
  if (!err.response) {
    return t('errors.networkError')
  }

  const { status, data } = err.response

  // ── Context-specific status codes ─────────────────────────────────────────

  if (context === 'login' && status === 401) {
    return t('auth.loginFailed')
  }

  if (context === 'register' && status === 409) {
    return t('auth.emailAlreadyRegistered')
  }

  // ── ASP.NET Core model validation (400) ───────────────────────────────────
  // Shape: { errors: { FieldName: ["message", ...] } }
  if (status === 400 && data?.errors && typeof data.errors === 'object') {
    const fieldErrors = Object.values(data.errors as Record<string, string[]>)
    const first = fieldErrors[0]?.[0]
    if (first) return first
  }

  // ── Problem Details title fallback ────────────────────────────────────────
  // Only use the server title for 4xx client errors, not 5xx server faults.
  if (status < 500 && typeof data?.title === 'string' && data.title) {
    return data.title as string
  }

  return t('errors.generic')
}

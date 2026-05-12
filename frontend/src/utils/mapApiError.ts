import axios from 'axios'

/**
 * mapApiError — generic Axios error → user-facing string for content mutations.
 *
 * The backend returns RFC 7807 Problem Details:
 *   { type, title, status, traceId }
 *
 * ASP.NET model validation (400) may also return:
 *   { type, title, status, errors: { FieldName: ["message"] } }
 *
 * Strategy:
 * - No response (network/timeout) → generic network error string
 * - 400 with field validation errors → first field message from server
 * - 400/404/409 with a title → return server title (content errors are readable)
 * - 5xx or unknown → generic error string
 *
 * Returns a plain string — callers render it directly or pass it to InlineFeedback.
 */
export function mapApiError(err: unknown): string {
  if (!axios.isAxiosError(err)) {
    return 'An unexpected error occurred. Please try again.'
  }

  if (!err.response) {
    return 'Unable to reach the server. Please check your connection.'
  }

  const { status, data } = err.response

  // ASP.NET model validation errors object
  if (status === 400 && data?.errors && typeof data.errors === 'object') {
    const firstField = Object.values(data.errors as Record<string, string[]>)[0]
    if (Array.isArray(firstField) && firstField.length > 0) {
      return firstField[0]
    }
  }

  // RFC 7807 title (covers 400, 404, 409 with readable messages)
  if (data?.title && typeof data.title === 'string') {
    return data.title
  }

  if (status >= 500) {
    return 'A server error occurred. Please try again later.'
  }

  return 'Something went wrong. Please try again.'
}

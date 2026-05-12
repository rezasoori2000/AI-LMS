import type { LoginPayload, RegisterPayload, AuthApiResponse } from '@/types'
import apiClient from '@/services/api-client'

/**
 * Auth service — thin wrappers around the backend auth endpoints.
 *
 * Endpoints (defined in AuthController):
 *   POST /api/auth/login     → 200 AuthApiResponse
 *   POST /api/auth/register  → 201 AuthApiResponse
 *
 * Both methods throw an AxiosError on non-2xx responses so callers can
 * inspect error.response.status and error.response.data for server messages.
 *
 * Session persistence (storing the returned token) is handled in the
 * AuthContext added in Section 3 Part 4 — not here.
 */

export async function loginUser(payload: LoginPayload): Promise<AuthApiResponse> {
  const { data } = await apiClient.post<AuthApiResponse>('/auth/login', payload)
  return data
}

export async function registerUser(payload: RegisterPayload): Promise<AuthApiResponse> {
  const { data } = await apiClient.post<AuthApiResponse>('/auth/register', payload)
  return data
}

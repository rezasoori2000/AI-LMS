import axios, { type AxiosInstance, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios'
import { retrieveToken, clearToken } from '@/utils/tokenStorage'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

/**
 * Shared Axios instance for all API calls.
 *
 * - Base URL and timeout set here once
 * - Auth token injection via request interceptor
 * - 401 handling via response interceptor
 *
 * Do not use axios directly in features — always import from here.
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30_000,
})

// ── Request interceptor ──────────────────────────────────────
// Attaches Bearer token from storage on every request.
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = retrieveToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// ── Response interceptor ─────────────────────────────────────
// - 401: clears stale token from storage; dispatches a CustomEvent so
//   AuthContext can synchronously reset React state without a circular import.
//   ProtectedRoute then redirects to /auth/login on the next render.
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error) => {
    if (error.response?.status === 401) {
      clearToken()
      window.dispatchEvent(new CustomEvent('auth:session-expired'))
    }
    return Promise.reject(error)
  }
)

export default apiClient

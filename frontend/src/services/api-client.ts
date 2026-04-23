import axios, { type AxiosInstance, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios'

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
// Auth token storage strategy will be finalized in Section 2.
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// ── Response interceptor ─────────────────────────────────────
// Handles global error cases:
// - 401: clears stale token; full redirect will be handled by AuthProvider (Section 2)
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('auth_token')
      // AuthProvider will detect missing token and redirect to /auth/login
    }
    return Promise.reject(error)
  }
)

export default apiClient

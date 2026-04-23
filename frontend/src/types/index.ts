// ============================================================
// Shared TypeScript types for AI-LMS frontend
// Domain-specific types live in their feature folders
// ============================================================

// ── User / Auth ──────────────────────────────────────────────

export type UserRole =
  | 'super_admin'
  | 'tenant_admin'
  | 'content_editor'
  | 'teacher'
  | 'parent'
  | 'student'

export interface User {
  id: string
  email: string
  displayName: string
  role: UserRole
  tenantId: string
  locale: string
}

// ── Localization ─────────────────────────────────────────────

export type Locale = string         // ISO 639-1 code: 'en', 'fa', 'ar', etc.
export type Direction = 'ltr' | 'rtl'

// ── API response shapes ──────────────────────────────────────

export interface ApiResponse<T> {
  data: T
  message?: string
  success: boolean
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ApiError {
  message: string
  code?: string
  errors?: Record<string, string[]>
}

// ── Utility types ────────────────────────────────────────────

export type Nullable<T> = T | null
export type Optional<T> = T | undefined
export type AsyncState<T> = {
  data: Nullable<T>
  isLoading: boolean
  error: Nullable<ApiError>
}

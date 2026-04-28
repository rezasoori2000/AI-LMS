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
// ── Navigation ──────────────────────────────────────────────

/**
 * A single sidebar navigation item.
 * Driven by config arrays so role-based nav is added in Section 2
 * by filtering NAV_ITEMS_BY_ROLE[currentUser.role].
 */
export interface NavItem {
  /** Stable key — used as React list key */
  key: string
  /** i18n translation key, e.g. 'nav.dashboard' */
  labelKey: string
  /** Destination href */
  href: string
  /**
   * Optional icon node.
   * Phase 2: replace with lucide-react icons.
   */
  icon?: React.ReactNode
  /**
   * Roles that may see this item in the sidebar.
   * Informational only in Phase 1 — not enforced until auth guards are added.
   * Phase 2: AppLayout will filter NAV_ITEMS_BY_ROLE[user.role], so this acts
   * as documentation for the intended visibility per item.
   */
  allowedRoles?: UserRole[]
  /**
   * When set, renders a labelled section separator above this nav item.
   * i18n key, e.g. 'nav.groups.management'. Only the first item in each
   * visual group should carry this field; subsequent items in the same
   * group leave it undefined.
   */
  sectionTitleKey?: string
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

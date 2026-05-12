import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import type { BackendUserRoleValue } from '@/types'

// ── Props ─────────────────────────────────────────────────────────────────────

interface ProtectedRouteProps {
  children: React.ReactElement
  /**
   * Optional role whitelist.
   *
   * When provided the authenticated user's role must be in the array,
   * otherwise they are redirected to /403.
   *
   * Leave undefined to allow any authenticated user (most common in Phase 1).
   *
   * Phase 3 expansion: replace `BackendUserRoleValue[]` with a richer
   * `AccessPolicy` type that can also include tenant and permission checks.
   */
  allowedRoles?: BackendUserRoleValue[]
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * ProtectedRoute — gate for routes that require authentication.
 *
 * Behaviour:
 * 1. While session is being restored (`isRestoring`) renders nothing —
 *    the AuthProvider is still reading localStorage; avoid a redirect flash.
 * 2. If unauthenticated → redirect to /auth/login, preserving the intended
 *    destination in location state so LoginPage can navigate back after sign-in.
 * 3. If `allowedRoles` is set and the user's role is not in the list →
 *    redirect to /403.
 * 4. Otherwise renders the child element.
 *
 * Usage:
 *   // Any authenticated user
 *   <Route path="dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
 *
 *   // Admin-only
 *   <Route path="admin" element={
 *     <ProtectedRoute allowedRoles={[BackendUserRole.SuperAdmin, BackendUserRole.TenantAdmin]}>
 *       <AdminPage />
 *     </ProtectedRoute>
 *   } />
 */
export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, isRestoring, user } = useAuth()
  const location = useLocation()

  // Still reading localStorage — render nothing to prevent a redirect flash
  if (isRestoring) return null

  // Not logged in → send to login, preserve the intended destination
  if (!isAuthenticated) {
    return (
      <Navigate
        to="/auth/login"
        replace
        state={{ from: location.pathname }}
      />
    )
  }

  // Role check — only runs when allowedRoles is explicitly provided
  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return <Navigate to="/403" replace />
  }

  return children
}

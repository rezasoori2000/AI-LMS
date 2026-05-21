import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppShell } from '@/components/layout/AppShell'
import { DEFAULT_NAV_ITEMS, NAV_ITEMS_BY_ROLE } from '@/config/nav'
import { useAuth } from '@/context/AuthContext'
import { toFrontendRole } from '@/types'

/**
 * AppLayout — authenticated application shell.
 *
 * Wraps all authenticated routes with AppShell (Topbar + Sidebar + main).
 *
 * Nav items:
 * - Resolved from NAV_ITEMS_BY_ROLE using the authenticated user's role.
 * - Falls back to DEFAULT_NAV_ITEMS when no user is present (should not happen
 *   in practice because AppLayout is always nested inside ProtectedRoute).
 *
 * Skip-link:
 * - Visible only on keyboard focus (sr-only focus:not-sr-only pattern).
 * - Sends focus to <main id="main-content"> inside AppShell, bypassing the
 *   Topbar and Sidebar for keyboard and screen-reader users.
 * - Must be the first focusable element in the document.
 *
 * The <Outlet /> renders the matched child route (dashboard page, etc.).
 */
export default function AppLayout() {
  const { t }    = useTranslation()
  const { user } = useAuth()
  const navItems = user
    ? (NAV_ITEMS_BY_ROLE[toFrontendRole(user.role)] ?? DEFAULT_NAV_ITEMS)
    : DEFAULT_NAV_ITEMS

  return (
    <>
      {/* Skip navigation link — first focusable element in the page */}
      <a
        href="#main-content"
        className={[
          'sr-only focus:not-sr-only',
          'fixed start-2 top-2 z-50',
          'rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-brand-600',
        ].join(' ')}
      >
        {t('nav.skipToContent')}
      </a>

      <AppShell navItems={navItems}>
        <Outlet />
      </AppShell>
    </>
  )
}

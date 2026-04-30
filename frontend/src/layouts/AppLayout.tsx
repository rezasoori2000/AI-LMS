import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppShell } from '@/components/layout/AppShell'
import { DEFAULT_NAV_ITEMS } from '@/config/nav'

/**
 * AppLayout — authenticated application shell.
 *
 * Wraps all authenticated routes with AppShell (Topbar + Sidebar + main).
 *
 * Nav items:
 * - Currently passes DEFAULT_NAV_ITEMS (all dashboards) since there is no auth yet.
 * - Section 2: replace with `NAV_ITEMS_BY_ROLE[currentUser.role]` once the
 *   auth context is available.
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
  const { t } = useTranslation()

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

      <AppShell navItems={DEFAULT_NAV_ITEMS}>
        <Outlet />
      </AppShell>
    </>
  )
}

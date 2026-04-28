import { Outlet } from 'react-router-dom'
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
 * The <Outlet /> renders the matched child route (dashboard page, etc.).
 */
export default function AppLayout() {
  return (
    <AppShell navItems={DEFAULT_NAV_ITEMS}>
      <Outlet />
    </AppShell>
  )
}

import type { RouteObject } from 'react-router-dom'
import AdminDashboardPage from '@/pages/dashboards/AdminDashboardPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Admin section routes — all nested under the /admin path segment.
 *
 * Spread into AppLayout's children in routes/index.tsx.
 *
 * The parent `path: 'admin'` route has no element intentionally.
 * This means children render directly in AppLayout's <Outlet />.
 *
 * Phase 2 upgrade path (zero child-route changes required):
 *   Add `element: <AdminLayout />` to the parent route when admin sections
 *   need a secondary sub-navigation bar. The child routes remain identical.
 */
export const adminRoutes: RouteObject[] = [
  {
    path: 'admin',
    // No element — path segment only. Children render in AppLayout <Outlet />.
    children: [
      {
        index: true,
        element: <AdminDashboardPage />,
        handle: {
          titleKey: 'dashboards.admin.title',
          access: ['super_admin', 'tenant_admin'],
        },
      },
      {
        path: 'tenants',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.tenants', access: ['super_admin'] },
      },
      {
        path: 'users',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.users', access: ['super_admin', 'tenant_admin'] },
      },
      {
        path: 'teachers',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.teachers', access: ['tenant_admin'] },
      },
      {
        path: 'students',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.students', access: ['tenant_admin'] },
      },
      {
        path: 'courses',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.courses', access: ['super_admin', 'tenant_admin', 'content_editor'] },
      },
      {
        path: 'settings',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.settings', access: ['super_admin', 'tenant_admin'] },
      },
    ],
  },
]

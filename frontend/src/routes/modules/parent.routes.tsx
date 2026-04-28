import type { RouteObject } from 'react-router-dom'
import ParentDashboardPage from '@/pages/dashboards/ParentDashboardPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Parent section routes — all nested under the /parent path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 */
export const parentRoutes: RouteObject[] = [
  {
    path: 'parent',
    children: [
      {
        index: true,
        element: <ParentDashboardPage />,
        handle: {
          titleKey: 'dashboards.parent.title',
          access: ['parent'],
        },
      },
      {
        path: 'children',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.children', access: ['parent'] },
      },
      {
        path: 'progress',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.progress', access: ['parent'] },
      },
    ],
  },
]

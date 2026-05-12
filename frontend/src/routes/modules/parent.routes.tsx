import type { RouteObject } from 'react-router-dom'
import ParentDashboardPage from '@/pages/dashboards/ParentDashboardPage'
import ChildrenPage        from '@/pages/parent/ChildrenPage'
import ChildDetailPage     from '@/pages/parent/ChildDetailPage'
import ComingSoonPage      from '@/pages/placeholders/ComingSoonPage'

/**
 * Parent section routes — all nested under the /parent path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 *
 * Phase 1 (Section 6):
 *   /parent                  → ParentDashboardPage (live children overview)
 *   /parent/children         → ChildrenPage        (full linked-student list)
 *   /parent/children/:id     → ChildDetailPage     (per-student enrollment detail)
 *   /parent/progress         → ComingSoonPage      (deferred to Phase 2)
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
        element: <ChildrenPage />,
        handle: { titleKey: 'parent.children.title', access: ['parent'] },
      },
      {
        path: 'children/:studentId',
        element: <ChildDetailPage />,
        handle: { titleKey: 'parent.childDetail.title', access: ['parent'] },
      },
      {
        path: 'progress',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.progress', access: ['parent'] },
      },
    ],
  },
]

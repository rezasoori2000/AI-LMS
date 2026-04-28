import type { RouteObject } from 'react-router-dom'
import StudentDashboardPage from '@/pages/dashboards/StudentDashboardPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Student section routes — all nested under the /student path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 */
export const studentRoutes: RouteObject[] = [
  {
    path: 'student',
    children: [
      {
        index: true,
        element: <StudentDashboardPage />,
        handle: {
          titleKey: 'dashboards.student.title',
          access: ['student'],
        },
      },
      {
        path: 'lessons',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.lessons', access: ['student'] },
      },
      {
        path: 'progress',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.progress', access: ['student'] },
      },
    ],
  },
]

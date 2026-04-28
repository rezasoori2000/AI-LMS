import type { RouteObject } from 'react-router-dom'
import TeacherDashboardPage from '@/pages/dashboards/TeacherDashboardPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Teacher section routes — all nested under the /teacher path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 */
export const teacherRoutes: RouteObject[] = [
  {
    path: 'teacher',
    children: [
      {
        index: true,
        element: <TeacherDashboardPage />,
        handle: {
          titleKey: 'dashboards.teacher.title',
          access: ['teacher', 'content_editor'],
        },
      },
      {
        path: 'students',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.students', access: ['teacher'] },
      },
      {
        path: 'lessons',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.lessons', access: ['teacher', 'content_editor'] },
      },
      {
        path: 'courses',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.courses', access: ['teacher', 'content_editor'] },
      },
      {
        path: 'progress',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.progress', access: ['teacher'] },
      },
    ],
  },
]

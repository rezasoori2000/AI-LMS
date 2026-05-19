import type { RouteObject } from 'react-router-dom'
import TeacherDashboardPage from '@/pages/dashboards/TeacherDashboardPage'
import StudentsPage         from '@/pages/teacher/StudentsPage'
import StudentMonitorPage   from '@/pages/teacher/StudentMonitorPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Teacher section routes — all nested under the /teacher path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 *
 * Phase 1 (Section 8):
 *   /teacher                          → TeacherDashboardPage (live summary + student overview)
 *   /teacher/students                 → StudentsPage         (full assigned-student list)
 *   /teacher/students/:studentId      → StudentMonitorPage   (enrollment + progress detail)
 *   /teacher/progress                 → ComingSoonPage       (deferred to Phase 2)
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
          access: ['teacher'],
        },
      },
      {
        path: 'students',
        element: <StudentsPage />,
        handle: { titleKey: 'teacher.students.title', access: ['teacher'] },
      },
      {
        path: 'students/:studentId',
        element: <StudentMonitorPage />,
        handle: { titleKey: 'teacher.studentDetail.title', access: ['teacher'] },
      },
      {
        path: 'progress',
        element: <ComingSoonPage />,
        handle: { titleKey: 'nav.progress', access: ['teacher'] },
      },
    ],
  },
]

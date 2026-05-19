import type { RouteObject } from 'react-router-dom'
import StudentDashboardPage from '@/pages/dashboards/StudentDashboardPage'
import SubjectsPage from '@/pages/student/SubjectsPage'
import SubjectDetailPage from '@/pages/student/SubjectDetailPage'
import LessonPlayerPage from '@/pages/student/LessonPlayerPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'

/**
 * Student section routes — all nested under the /student path segment.
 * Spread into AppLayout's children in routes/index.tsx.
 *
 * Phase 1 (Section 7):
 *   /student                                         → StudentDashboardPage (live stats in Part 6)
 *   /student/subjects                                → SubjectsPage    (Part 4)
 *   /student/subjects/:subjectId                     → SubjectDetailPage (Part 4)
 *   /student/subjects/:subjectId/lessons/:lessonId   → LessonViewPage (Part 5)
 *   /student/progress                                → ProgressPage   (Part 6)
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
        path: 'subjects',
        element: <SubjectsPage />,
        handle: { titleKey: 'nav.mySubjects', access: ['student'] },
      },
      {
        path: 'subjects/:subjectId',
        element: <SubjectDetailPage />,
        handle: { titleKey: 'nav.mySubjects', access: ['student'] },
      },
      {
        path: 'subjects/:subjectId/lessons/:lessonId',
        element: <LessonPlayerPage />,
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

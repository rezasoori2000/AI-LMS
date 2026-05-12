import type { RouteObject } from 'react-router-dom'
import AdminDashboardPage from '@/pages/dashboards/AdminDashboardPage'
import ComingSoonPage from '@/pages/placeholders/ComingSoonPage'
import StudentsLinkPage from '@/pages/admin/students/StudentsLinkPage'
import GradesPage         from '@/pages/admin/content/grades/GradesPage'
import GradeFormPage      from '@/pages/admin/content/grades/GradeFormPage'
import SubjectsPage       from '@/pages/admin/content/subjects/SubjectsPage'
import SubjectFormPage    from '@/pages/admin/content/subjects/SubjectFormPage'
import ChaptersPage       from '@/pages/admin/content/chapters/ChaptersPage'
import ChapterFormPage    from '@/pages/admin/content/chapters/ChapterFormPage'
import LessonsPage        from '@/pages/admin/content/lessons/LessonsPage'
import LessonFormPage     from '@/pages/admin/content/lessons/LessonFormPage'
import QuestionsPage      from '@/pages/admin/content/questions/QuestionsPage'
import QuestionFormPage   from '@/pages/admin/content/questions/QuestionFormPage'

/**
 * Admin section routes — all nested under the /admin path segment.
 *
 * Spread into AppLayout's children in routes/index.tsx.
 *
 * Phase 2 upgrade path (zero child-route changes required):
 *   Add `element: <AdminLayout />` to the parent route when admin sections
 *   need a secondary sub-navigation bar.
 */
export const adminRoutes: RouteObject[] = [
  {
    path: 'admin',
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
        element: <StudentsLinkPage />,
        handle: { titleKey: 'nav.students', access: ['super_admin', 'tenant_admin'] },
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

      // ── Content management ─────────────────────────────────────────────────
      {
        path: 'content',
        handle: { access: ['super_admin', 'tenant_admin', 'content_editor'] },
        children: [
          // Grades
          {
            path: 'grades',
            element: <GradesPage />,
            handle: { titleKey: 'admin.content.grades.title' },
          },
          {
            path: 'grades/new',
            element: <GradeFormPage />,
            handle: { titleKey: 'admin.content.grades.createTitle' },
          },
          {
            path: 'grades/:id/edit',
            element: <GradeFormPage />,
            handle: { titleKey: 'admin.content.grades.editTitle' },
          },

          // Subjects
          {
            path: 'subjects',
            element: <SubjectsPage />,
            handle: { titleKey: 'admin.content.subjects.title' },
          },
          {
            path: 'subjects/new',
            element: <SubjectFormPage />,
            handle: { titleKey: 'admin.content.subjects.createTitle' },
          },
          {
            path: 'subjects/:id/edit',
            element: <SubjectFormPage />,
            handle: { titleKey: 'admin.content.subjects.editTitle' },
          },

          // Chapters
          {
            path: 'chapters',
            element: <ChaptersPage />,
            handle: { titleKey: 'admin.content.chapters.title' },
          },
          {
            path: 'chapters/new',
            element: <ChapterFormPage />,
            handle: { titleKey: 'admin.content.chapters.createTitle' },
          },
          {
            path: 'chapters/:id/edit',
            element: <ChapterFormPage />,
            handle: { titleKey: 'admin.content.chapters.editTitle' },
          },

          // Lessons
          {
            path: 'lessons',
            element: <LessonsPage />,
            handle: { titleKey: 'admin.content.lessons.title' },
          },
          {
            path: 'lessons/new',
            element: <LessonFormPage />,
            handle: { titleKey: 'admin.content.lessons.createTitle' },
          },
          {
            path: 'lessons/:id/edit',
            element: <LessonFormPage />,
            handle: { titleKey: 'admin.content.lessons.editTitle' },
          },

          // Questions
          {
            path: 'questions',
            element: <QuestionsPage />,
            handle: { titleKey: 'admin.content.questions.title' },
          },
          {
            path: 'questions/new',
            element: <QuestionFormPage />,
            handle: { titleKey: 'admin.content.questions.createTitle' },
          },
          {
            path: 'questions/:id/edit',
            element: <QuestionFormPage />,
            handle: { titleKey: 'admin.content.questions.editTitle' },
          },
        ],
      },
    ],
  },
]

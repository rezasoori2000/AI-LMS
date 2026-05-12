import { createBrowserRouter } from 'react-router-dom'
import AppLayout   from '@/layouts/AppLayout'
import AuthLayout  from '@/layouts/AuthLayout'
import HomePage    from '@/pages/HomePage'
import NotFoundPage    from '@/pages/NotFoundPage'
import ForbiddenPage   from '@/pages/ForbiddenPage'
import LoginPage       from '@/pages/auth/LoginPage'
import RegisterPage    from '@/pages/auth/RegisterPage'
import { ProtectedRoute }  from '@/components/auth/ProtectedRoute'
import { adminRoutes }   from '@/routes/modules/admin.routes'
import { teacherRoutes } from '@/routes/modules/teacher.routes'
import { parentRoutes }  from '@/routes/modules/parent.routes'
import { studentRoutes } from '@/routes/modules/student.routes'

/**
 * Central router for AI-LMS.
 *
 * Route tree (Phase 1 Section 2 Part 2 — route structure):
 *
 *  /                     → AppLayout
 *    index               → HomePage
 *    admin/*             → adminRoutes   (AdminDashboardPage + sub-route stubs)
 *    teacher/*           → teacherRoutes (TeacherDashboardPage + sub-route stubs)
 *    parent/*            → parentRoutes  (ParentDashboardPage + sub-route stubs)
 *    student/*           → studentRoutes (StudentDashboardPage + sub-route stubs)
 *  /auth                 → AuthLayout
 *    (login/register added in Section 2 — auth feature module)
 *  *                     → NotFoundPage
 *
 * Each domain module is a RouteObject[] spread into AppLayout's children.
 * Sub-routes currently render ComingSoonPage; swap the element when the real
 * page is ready — the path, handle, and access metadata stay unchanged.
 *
 * Phase 2 auth guard integration:
 *   Add a `loader` to the AppLayout route that reads
 *   `matchRoutes(router.routes, url).at(-1)?.route.handle?.access`
 *   and redirects to /auth/login or /403 as appropriate.
 *   Route handle metadata (access, titleKey) is already in place in each module.
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <HomePage />, handle: { titleKey: 'nav.home', access: 'public' } },
      ...adminRoutes,
      ...teacherRoutes,
      ...parentRoutes,
      ...studentRoutes,
    ],
  },
  {
    path: '/auth',
    element: <AuthLayout />,
    children: [
      { path: 'login',    element: <LoginPage />,    handle: { titleKey: 'auth.login',    access: 'public' } },
      { path: 'register', element: <RegisterPage />, handle: { titleKey: 'auth.register', access: 'public' } },
    ],
  },
  {
    // Role guard redirect target.
    // Phase 2: auth loader throws redirect('/403') when the user's role
    // is not in the matched route's handle.access array.
    path: '/403',
    element: <ForbiddenPage />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
])

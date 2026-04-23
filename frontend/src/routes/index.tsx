import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/layouts/AppLayout'
import AuthLayout from '@/layouts/AuthLayout'
import HomePage from '@/pages/HomePage'
import NotFoundPage from '@/pages/NotFoundPage'

/**
 * Central route definitions for AI-LMS.
 *
 * Structure:
 * - '/'            → AppLayout (authenticated shell) — placeholder for now
 * - '/auth/*'      → AuthLayout (login, register) — added in Section 2
 * - '*'            → NotFoundPage
 *
 * Role-based route guards will be added in Section 2 after auth is implemented.
 */
export const router = createBrowserRouter([
  {
    // Main authenticated app shell
    path: '/',
    element: <AppLayout />,
    children: [
      {
        index: true,
        element: <HomePage />,
      },
      // Placeholder: dashboard, lessons, progress routes added in later sections
    ],
  },
  {
    // Auth shell — login/register pages (Section 2)
    path: '/auth',
    element: <AuthLayout />,
    children: [
      // Placeholder: /auth/login, /auth/register
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
])

import { Outlet, Navigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'

/**
 * AuthLayout — centered layout for login and register pages.
 *
 * Redirect behaviour:
 * - While session is restoring, renders the layout normally (avoids a flash).
 * - Once restoration is complete, an authenticated user is sent to '/'
 *   so they never see the login/register forms when already signed in.
 */
export default function AuthLayout() {
  const { isAuthenticated, isRestoring } = useAuth()

  if (!isRestoring && isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return (
    <div className="min-h-screen bg-surface-raised flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-brand-600">AI-LMS</h1>
          <p className="mt-1 text-sm text-content-secondary">AI-powered educational platform</p>
        </div>
        <div className="bg-surface rounded-lg shadow-card border border-stroke p-8">
          <Outlet />
        </div>
      </div>
    </div>
  )
}

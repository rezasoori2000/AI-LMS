import { Outlet } from 'react-router-dom'

/**
 * AuthLayout — centered layout for login, register, forgot password pages.
 * Added to routes in Section 2 when auth is implemented.
 */
export default function AuthLayout() {
  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-brand-600">AI-LMS</h1>
          <p className="mt-1 text-sm text-gray-500">AI-powered educational platform</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8">
          <Outlet />
        </div>
      </div>
    </div>
  )
}

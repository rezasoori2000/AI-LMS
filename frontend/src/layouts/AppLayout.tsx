import { Outlet } from 'react-router-dom'

/**
 * AppLayout — main authenticated application shell.
 *
 * Currently a minimal skeleton.
 * In Section 2+:
 * - Header will receive nav links based on user role
 * - Sidebar added for admin/teacher dashboards
 * - Breadcrumb added for lesson navigation
 */
export default function AppLayout() {
  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <span className="text-lg font-semibold text-brand-600">AI-LMS</span>
          {/* Role-based navigation will be added in Section 2 */}
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-gray-200 bg-white mt-auto">
        <div className="max-w-7xl mx-auto px-6 py-4 text-center text-sm text-gray-500">
          AI-LMS &copy; {new Date().getFullYear()}
        </div>
      </footer>
    </div>
  )
}
